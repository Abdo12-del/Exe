/*
 * test_core.c — اختبارات منطق الأعمال (تُبنى وتُشغَّل على لينكس)
 */
#include "core.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>

static int fails = 0;

/* AppData كبير (≈9MB) — يجب أن يكون في النطاق العام لا على المكدس */
static AppData d, d2, d3, d4, d5, d6, d7;

#define CHECK(cond)                                                     \
    do {                                                                \
        if (!(cond)) {                                                  \
            printf("FAIL %s:%d: %s\n", __FILE__, __LINE__, #cond);      \
            fails++;                                                    \
        }                                                               \
    } while (0)

static int close_d(double a, double b) { return fabs(a - b) < 0.01; }

int main(void)
{
    app_init(&d);

    /* المنتجات */
    int a = product_add(&d, "قمر الدين", 50, 80, 20);
    int b = product_add(&d, "زيت الزيتون", 900, 1200, 10);
    CHECK(a == 1 && b == 2);
    CHECK(product_add(&d, "قمر الدين", 1, 2, 3) == -2); /* اسم مكرر */
    CHECK(product_add(&d, "", 1, 2, 3) == -1);
    CHECK(product_add(&d, "خ", -1, 2, 3) == -1);
    CHECK(product_find(&d, 1) != NULL);
    CHECK(product_find(&d, 1)->qty == 20);
    CHECK(product_find(&d, 99) == NULL);
    CHECK(product_find_by_name(&d, "زيت الزيتون") == product_find(&d, 2));

    /* بيع ناجح */
    double total = 0;
    int ids[2] = {1, 2}, qtys[2] = {3, 2};
    CHECK(sale_create(&d, ids, qtys, 2, &total) == 0);
    CHECK(close_d(total, 80.0 * 3 + 1200.0 * 2));
    CHECK(product_find(&d, 1)->qty == 17);
    CHECK(product_find(&d, 2)->qty == 8);
    CHECK(d.n_sales == 1);
    CHECK(d.next_sale_id == 2);
    CHECK(close_d(d.sales[0].profit, (80 - 50) * 3 + (1200 - 900) * 2));

    /* مخزون غير كافٍ — يجب أن يفشل دون خصم */
    int ids2[1] = {2}, qtys2[1] = {999};
    CHECK(sale_create(&d, ids2, qtys2, 1, &total) == -2);
    CHECK(product_find(&d, 2)->qty == 8);
    CHECK(d.n_sales == 1);

    /* منتج غير موجود */
    int ids3[1] = {42}, qtys3[1] = {1};
    CHECK(sale_create(&d, ids3, qtys3, 1, &total) == -1);

    /* تقارير */
    CHECK(close_d(rep_total_sales(&d), 80.0 * 3 + 1200.0 * 2));
    CHECK(close_d(rep_total_profit(&d), 90.0 + 600.0));
    CHECK(close_d(rep_sales_today(&d), 2640.0));

    int tids[5], tqs[5];
    int tn = rep_top_products(&d, tids, tqs, 5);
    CHECK(tn == 2);
    CHECK(tids[0] == 1 && tqs[0] == 3);
    CHECK(tids[1] == 2 && tqs[1] == 2);

    int lids[16];
    CHECK(rep_low_stock(&d, lids, 16) == 0);
    int c = product_add(&d, "مكسرات", 100, 150, 3);
    CHECK(c == 3);
    CHECK(rep_low_stock(&d, lids, 16) == 1);
    CHECK(lids[0] == 3);

    /* JSON: حفظ ثم تحميل ومقارنة */
    const char *path = "/tmp/taseer_test_data.json";
    CHECK(json_save(&d, path) == 0);
    app_init(&d2);
    CHECK(json_load(&d2, path) == 1);
    CHECK(d2.n_products == d.n_products);
    CHECK(d2.n_sales == d.n_sales);
    CHECK(d2.next_product_id == d.next_product_id);
    CHECK(d2.next_sale_id == d.next_sale_id);
    for (int i = 0; i < d.n_products; i++) {
        CHECK(strcmp(d2.products[i].name, d.products[i].name) == 0);
        CHECK(d2.products[i].id == d.products[i].id);
        CHECK(d2.products[i].qty == d.products[i].qty);
        CHECK(close_d(d2.products[i].buy_price, d.products[i].buy_price));
        CHECK(close_d(d2.products[i].sell_price, d.products[i].sell_price));
    }
    CHECK(d2.sales[0].n_items == d.sales[0].n_items);
    CHECK(d2.sales[0].id == d.sales[0].id);
    CHECK(close_d(d2.sales[0].total, d.sales[0].total));
    CHECK(close_d(d2.sales[0].profit, d.sales[0].profit));
    CHECK(d2.sales[0].items[0].product_id == d.sales[0].items[0].product_id);
    CHECK(strcmp(d2.sales[0].items[0].name, d.sales[0].items[0].name) == 0);
    CHECK(close_d(d2.sales[0].items[0].sell_price, d.sales[0].items[0].sell_price));
    CHECK(d2.sales[0].items[0].qty == d.sales[0].items[0].qty);

    /* حذف منتج بعد بيعه — يبقى في الفواتير السابقة */
    CHECK(product_remove(&d, 3) == 0);
    CHECK(json_save(&d, path) == 0);
    app_init(&d3);
    CHECK(json_load(&d3, path) == 1);
    CHECK(d3.n_products == 2);
    CHECK(product_find(&d3, 3) == NULL);

    /* ملف غير موجود */
    app_init(&d4);
    CHECK(json_load(&d4, "/tmp/taseer_no_such_file_xyz.json") == 0);

    /* ملف تالف */
    FILE *f = fopen("/tmp/taseer_bad_data.json", "wb");
    CHECK(f != NULL);
    if (f) { fputs("{broken json here", f); fclose(f); }
    app_init(&d5);
    CHECK(json_load(&d5, "/tmp/taseer_bad_data.json") == -1);

    /* سلاسل معقدة (حرف اقتباس وشرطة مائلة + ترميز) */
    app_init(&d6);
    CHECK(product_add(&d6, "معجون \"فواكه\" طبيعي", 10, 20, 5) == 1);
    CHECK(json_save(&d6, path) == 0);
    app_init(&d7);
    CHECK(json_load(&d7, path) == 1);
    CHECK(strcmp(d7.products[0].name, "معجون \"فواكه\" طبيعي") == 0);

    /* نُبقي نسخة أخيرة كي يتحقق منها سكربت البناء بمحلل JSON مستقل */
    CHECK(json_save(&d, "/tmp/taseer_last.json") == 0);

    remove(path);
    remove("/tmp/taseer_bad_data.json");

    if (fails) {
        printf("TESTS FAILED: %d\n", fails);
        return 1;
    }
    printf("ALL TESTS PASSED\n");
    return 0;
}
