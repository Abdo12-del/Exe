/*
 * core.h — نموذج البيانات والمنطق الأساسي لبرنامج تسيير المبيعات
 * (نموذج المنتجات والمبيعات + حفظ/تحميل JSON + التقارير)
 */
#ifndef CORE_H
#define CORE_H

#include <stddef.h>
#include <time.h>

#define MAX_NAME     120
#define MAX_ITEMS    64
#define MAX_PRODUCTS 256
#define MAX_SALES    1024
#define LOW_STOCK    5   /* حد تنبيه المخزون المنخفض */

typedef struct {
    int id;
    char name[MAX_NAME];
    double buy_price;   /* سعر الشراء */
    double sell_price;  /* سعر البيع */
    int qty;            /* الكمية المتوفرة */
} Product;

typedef struct {
    int product_id;
    char name[MAX_NAME];
    double sell_price;
    double buy_price;
    int qty;
} SaleItem;

typedef struct {
    int id;
    time_t ts;
    char date_str[32];
    SaleItem items[MAX_ITEMS];
    int n_items;
    double total;
    double profit;
} Sale;

typedef struct {
    Product products[MAX_PRODUCTS];
    int n_products;
    int next_product_id;
    Sale sales[MAX_SALES];
    int n_sales;
    int next_sale_id;
} AppData;

void app_init(AppData *d);

/* المنتجات */
int       product_add(AppData *d, const char *name, double buy, double sell, int qty);
Product  *product_find(AppData *d, int id);
Product  *product_find_by_name(AppData *d, const char *name);
int       product_remove(AppData *d, int id);

/* المبيعات: 0 نجاح، -1 مدخلات غير صالحة، -2 مخزون غير كافٍ، -3 السجل ممتلئ */
int  sale_create(AppData *d, const int *ids, const int *qtys, int n, double *total_out);
Sale *sale_find(AppData *d, int id);

/* JSON: save → 0 نجاح / -1 خطأ. load → 1 تم / 0 لا يوجد ملف / -1 خطأ قراءة */
int json_save(const AppData *d, const char *path);
int json_load(AppData *d, const char *path);

/* التقارير */
double rep_total_sales(const AppData *d);
double rep_total_profit(const AppData *d);
double rep_sales_today(const AppData *d);
int    rep_top_products(const AppData *d, int *ids_out, int *qtys_out, int max_n);
int    rep_low_stock(const AppData *d, int *ids_out, int max_n);

/* أدوات زمنية */
void now_str(char *buf, size_t n, time_t ts);
int  same_day(time_t a, time_t b);

#endif /* CORE_H */
