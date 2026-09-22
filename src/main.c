/*
 * main.c — واجهة سطر الأوامر لبرنامج تسيير المبيعات
 *
 * مزايا:
 *   - البيع (فواتير) مع سلة وحساب الإجمالي تلقائياً وخصم المخزون
 *   - إدارة المنتجات (إضافة/عرض/تعديل/حذف)
 *   - سجل المبيعات مع عرض تفاصيل أي فاتورة
 *   - تقارير: إجمالي المبيعات والأرباح، مبيعات اليوم، الأكثر مبيعاً،
 *     وتنبيه المخزون المنخفض
 *   - حفظ تلقائي في ملف JSON بجانب الملف التنفيذي
 *   - دعم كامل للغة العربية في وحدة تحكم ويندوز
 */
#include "core.h"
#include "io.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>

#if defined(_WIN32)
#include <windows.h>
#endif

static AppData g;
static char g_data_path[1024];

#define PRINT(s)   io_out(s)
#define PRINTLN(s) io_outln(s)

/* ------------------------------------------------------------------ */
/* أدوات عرض وإدخال                                                    */
/* ------------------------------------------------------------------ */

static void line(void)
{
    PRINTLN("════════════════════════════════════════════════");
}

static void banner(void)
{
    PRINTLN("════════════════════════════════════════════════");
    PRINTLN("              برنامج تسيير المبيعات");
    PRINTLN("════════════════════════════════════════════════");
}

static void warn(const char *msg)
{
    PRINT("  ⚠ ");
    PRINTLN(msg);
}

/* إدخال رقم: 1 نجاح، 0 إلغاء (EOF أو إدخال فارغ) */
static int ask_int(const char *prompt, int *out, int lo, int hi)
{
    for (;;) {
        char buf[64];
        PRINT(prompt);
        if (!io_read_line(buf, sizeof buf)) return 0;
        if (!*buf) return 0;
        char *end = NULL;
        long v = strtol(buf, &end, 10);
        if (*end || v < lo || v > hi) {
            warn("أدخل رقماً صحيحاً من فضلك");
            continue;
        }
        *out = (int)v;
        return 1;
    }
}

static int ask_double(const char *prompt, double *out, double lo, double hi)
{
    for (;;) {
        char buf[64];
        PRINT(prompt);
        if (!io_read_line(buf, sizeof buf)) return 0;
        if (!*buf) return 0;
        char *end = NULL;
        double v = strtod(buf, &end);
        if (*end || !isfinite(v) || v < lo || v > hi) {
            warn("أدخل قيمة رقمية صحيحة من فضلك");
            continue;
        }
        *out = v;
        return 1;
    }
}

static int ask_text(const char *prompt, char *buf, size_t cap)
{
    for (;;) {
        PRINT(prompt);
        if (!io_read_line(buf, cap)) return 0;
        if (*buf) return 1;
        warn("لا يمكن أن يكون الحقل فارغاً");
    }
}

/* نعم/لا: 1 نعم، 0 لا، -1 EOF */
static int ask_yes_no(const char *prompt)
{
    for (;;) {
        char buf[16];
        PRINT(prompt);
        if (!io_read_line(buf, sizeof buf)) return -1;
        if (buf[0] == '\xd9' && buf[1] == '\x86') return 1; /* ن */
        if (buf[0] == 'y' || buf[0] == 'Y') return 1;
        if (buf[0] == '\xd9' && buf[1] == '\x84') return 0; /* ل */
        if (buf[0] == 'n' || buf[0] == 'N') return 0;
        warn("أجب بنعم أو لا");
    }
}

/* حقل اختياري: إدخال فارغ = الإبقاء على القيمة الحالية.
 * 1 قيمة جديدة، 0 إبقاء، -1 EOF */
static int ask_keep_text(const char *prompt, const char *def, char *buf, size_t cap)
{
    char p[320];
    for (;;) {
        snprintf(p, sizeof p, "%s [%s] ", prompt, def);
        PRINT(p);
        if (!io_read_line(buf, cap)) return -1;
        if (*buf) return 1;
        return 0;
    }
}

static int ask_keep_double(const char *prompt, double def, double *out)
{
    char p[96], buf[64];
    for (;;) {
        snprintf(p, sizeof p, "%s [%.2f] ", prompt, def);
        PRINT(p);
        if (!io_read_line(buf, sizeof buf)) return -1;
        if (!*buf) { *out = def; return 0; }
        char *end = NULL;
        double v = strtod(buf, &end);
        if (*end || !isfinite(v) || v < 0) {
            warn("أدخل قيمة رقمية صحيحة من فضلك");
            continue;
        }
        *out = v;
        return 1;
    }
}

static int ask_keep_int(const char *prompt, int def, int *out)
{
    char p[96], buf[64];
    for (;;) {
        snprintf(p, sizeof p, "%s [%d] ", prompt, def);
        PRINT(p);
        if (!io_read_line(buf, sizeof buf)) return -1;
        if (!*buf) { *out = def; return 0; }
        char *end = NULL;
        long v = strtol(buf, &end, 10);
        if (*end || v < 0) {
            warn("أدخل رقماً صحيحاً من فضلك");
            continue;
        }
        *out = (int)v;
        return 1;
    }
}

/* ------------------------------------------------------------------ */
/* حفظ البيانات                                                        */
/* ------------------------------------------------------------------ */

static void make_data_path(void)
{
#if defined(_WIN32)
    char exe[1024] = "";
    GetModuleFileNameA(NULL, exe, sizeof(exe) - 1);
    char *slash = strrchr(exe, '\\');
    if (slash) *slash = 0;
    else strcpy(exe, ".");
    snprintf(g_data_path, sizeof g_data_path, "%s\\taseer_data.json", exe);
#else
    snprintf(g_data_path, sizeof g_data_path, "./taseer_data.json");
#endif
}

static void save(void)
{
    if (json_save(&g, g_data_path) != 0)
        warn("تعذر حفظ ملف البيانات — تحقق من صلاحية الكتابة في المجلد");
}

/* ------------------------------------------------------------------ */
/* البيانات التجريبية                                                   */
/* ------------------------------------------------------------------ */

static void load_sample(void)
{
    struct { const char *name; double buy, sell; int qty; } sample[] = {
        { "قمر الدين",          50.0,   80.0,  20 },
        { "زيت الزيتون",       900.0, 1200.0,  10 },
        { "عسل البربر",       1500.0, 2200.0,   8 },
        { "سمن بلدي",         1100.0, 1600.0,  15 },
        { "تمر دقلة نور",      300.0,  500.0,  30 },
        { "خبز بار",            20.0,   35.0,  40 },
    };
    for (size_t i = 0; i < sizeof(sample) / sizeof(sample[0]); i++)
        product_add(&g, sample[i].name, sample[i].buy, sample[i].sell, sample[i].qty);
    save();
    PRINTLN("  ✔ تم تحميل البيانات التجريبية.");
}

/* ------------------------------------------------------------------ */
/* المنتجات                                                            */
/* ------------------------------------------------------------------ */

static void print_products(void)
{
    if (g.n_products == 0) {
        PRINTLN("  لا توجد منتجات بعد. أضف منتجاً من قسم «إدارة المنتجات».");
        return;
    }
    PRINTLN("  ─────────────────────────────────────────────────────────");
    PRINTLN("  #   | المنتج                  | سعر البيع    | المخزون");
    PRINTLN("  ─────────────────────────────────────────────────────────");
    for (int i = 0; i < g.n_products; i++) {
        const Product *p = &g.products[i];
        char row[256];
        snprintf(row, sizeof row, "  %3d | %-26s | %10.2f دج | %7d",
                 p->id, p->name, p->sell_price, p->qty);
        PRINTLN(row);
        if (p->qty <= LOW_STOCK)
            PRINTLN("      ⚠ مخزون منخفض!");
    }
    PRINTLN("  ─────────────────────────────────────────────────────────");
}

static void add_product(void)
{
    char name[MAX_NAME];
    if (!ask_text("  اسم المنتج: ", name, sizeof name)) return;
    if (product_find_by_name(&g, name)) {
        warn("يوجد منتج بنفس الاسم بالفعل");
        return;
    }
    double buy, sell;
    int qty;
    if (!ask_double("  سعر الشراء (دج): ", &buy, 0, 1e9)) return;
    if (!ask_double("  سعر البيع (دج): ", &sell, 0, 1e9)) return;
    if (!ask_int("  الكمية: ", &qty, 0, 10000000)) return;

    int id = product_add(&g, name, buy, sell, qty);
    if (id < 0) {
        warn("فشل إضافة المنتج");
        return;
    }
    if (sell < buy)
        warn("تنبيه: سعر البيع أقل من سعر الشراء");
    save();
    char msg[96];
    snprintf(msg, sizeof msg, "تمت إضافة المنتج (رقمه %d)", id);
    PRINTLN("  ✔ ");
    PRINTLN(msg);
}

static void edit_product(void)
{
    int id = 0;
    if (!ask_int("  رقم المنتج: ", &id, 0, 1000000)) return;
    Product *p = product_find(&g, id);
    if (!p) {
        warn("لا يوجد منتج بهذا الرقم");
        return;
    }
    PRINTLN("  (اضغط Enter للاحتفاظ بالقيمة الحالية)");
    PRINTLN("");

    char buf[MAX_NAME];
    int rc = ask_keep_text("  الاسم: ", p->name, buf, sizeof buf);
    if (rc == -1) return;
    if (rc == 1) {
        Product *dup = product_find_by_name(&g, buf);
        if (dup && dup != p) {
            warn("يوجد منتج آخر بنفس الاسم — لم يُعدَّل الاسم");
        } else {
            snprintf(p->name, sizeof p->name, "%s", buf);
        }
    }
    double v;
    rc = ask_keep_double("  سعر الشراء (دج): ", p->buy_price, &v);
    if (rc == -1) return;
    if (rc == 1) p->buy_price = v;
    rc = ask_keep_double("  سعر البيع (دج): ", p->sell_price, &v);
    if (rc == -1) return;
    if (rc == 1) p->sell_price = v;
    int iv;
    rc = ask_keep_int("  الكمية: ", p->qty, &iv);
    if (rc == -1) return;
    if (rc == 1) p->qty = iv;

    save();
    PRINTLN("  ✔ تم حفظ التعديلات.");
}

static void delete_product(void)
{
    int id = 0;
    if (!ask_int("  رقم المنتج: ", &id, 0, 1000000)) return;
    Product *p = product_find(&g, id);
    if (!p) {
        warn("لا يوجد منتج بهذا الرقم");
        return;
    }
    char msg[192];
    snprintf(msg, sizeof msg, "حذف «%s» نهائياً؟", p->name);
    if (ask_yes_no("  ") == 0) {
        PRINTLN("  لم يُحذف.");
        return;
    }
    product_remove(&g, id);
    save();
    PRINTLN("  ✔ تم الحذف.");
}

static void products_menu(void)
{
    for (;;) {
        PRINTLN("");
        line();
        PRINTLN("                    إدارة المنتجات");
        line();
        PRINTLN("   (1) إضافة منتج");
        PRINTLN("   (2) عرض المنتجات");
        PRINTLN("   (3) تعديل منتج");
        PRINTLN("   (4) حذف منتج");
        PRINTLN("   (5) رجوع");
        line();
        int ch = 0;
        if (!ask_int("  اختر: ", &ch, 1, 5)) return;
        if (ch == 1) add_product();
        else if (ch == 2) print_products();
        else if (ch == 3) edit_product();
        else if (ch == 4) delete_product();
        else return;
    }
}

/* ------------------------------------------------------------------ */
/* البيع                                                               */
/* ------------------------------------------------------------------ */

static void print_receipt(const Sale *s)
{
    line();
    char h[160];
    snprintf(h, sizeof h, "          فاتورة بيع  #%d   —   %s", s->id, s->date_str);
    PRINTLN(h);
    line();
    for (int i = 0; i < s->n_items; i++) {
        const SaleItem *it = &s->items[i];
        char row[256];
        snprintf(row, sizeof row, "  %-24s   %3d × %8.2f = %10.2f دج",
                 it->name, it->qty, it->sell_price, it->sell_price * it->qty);
        PRINTLN(row);
    }
    line();
    char t[96];
    snprintf(t, sizeof t, "  الإجمالي: %.2f دج", s->total);
    PRINTLN(t);
    PRINTLN("");
}

static void new_sale(void)
{
    if (g.n_products == 0) {
        warn("لا توجد منتجات. أضف منتجات أولاً من قسم «إدارة المنتجات».");
        return;
    }
    print_products();
    PRINTLN("");

    int ids[MAX_ITEMS], qtys[MAX_ITEMS], n = 0;
    for (;;) {
        int id = 0;
        if (!ask_int("  رقم المنتج (أو 0 لإنهاء): ", &id, 0, 1000000)) return;
        if (id == 0) break;
        Product *p = product_find(&g, id);
        if (!p) {
            warn("رقم منتج غير موجود");
            continue;
        }
        if (p->qty <= 0) {
            warn("نفد مخزون هذا المنتج");
            continue;
        }
        int q = 0;
        if (!ask_int("  الكمية: ", &q, 1, 10000000)) return;

        int in_cart = 0, idx = -1;
        for (int i = 0; i < n; i++)
            if (ids[i] == id) { idx = i; in_cart = qtys[i]; }
        if (q + in_cart > p->qty) {
            char msg[96];
            snprintf(msg, sizeof msg, "الكمية أكبر من المتاح (المتبقي: %d)", p->qty - in_cart);
            warn(msg);
            continue;
        }
        if (idx >= 0) qtys[idx] += q;
        else { ids[n] = id; qtys[n] = q; n++; }
    }

    if (n == 0) {
        PRINTLN("  تم إلغاء البيع.");
        return;
    }

    PRINTLN("  ────────────────────────────");
    PRINTLN("  السلة:");
    double total = 0;
    for (int i = 0; i < n; i++) {
        Product *p = product_find(&g, ids[i]);
        if (!p) continue;
        double lv = p->sell_price * qtys[i];
        total += lv;
        char row[256];
        snprintf(row, sizeof row, "  %-24s   %3d × %8.2f = %10.2f دج",
                 p->name, qtys[i], p->sell_price, lv);
        PRINTLN(row);
    }
    PRINTLN("  ────────────────────────────");
    char t[96];
    snprintf(t, sizeof t, "  الإجمالي: %.2f دج", total);
    PRINTLN(t);
    PRINTLN("");

    if (ask_yes_no("  تأكيد البيع؟ (ن/لا) ") == 0) {
        PRINTLN("  تم إلغاء البيع.");
        return;
    }

    double tv = 0;
    int rc = sale_create(&g, ids, qtys, n, &tv);
    if (rc == -2) {
        warn("المخزون غير كافٍ لإتمام البيع");
        return;
    }
    if (rc != 0) {
        warn("فشل إنشاء الفاتورة");
        return;
    }
    save();
    Sale *s = sale_find(&g, g.next_sale_id - 1);
    if (s) print_receipt(s);
}

/* ------------------------------------------------------------------ */
/* سجل المبيعات                                                        */
/* ------------------------------------------------------------------ */

static void sales_history(void)
{
    if (g.n_sales == 0) {
        PRINTLN("  لا توجد مبيعات بعد.");
        return;
    }
    PRINTLN("  ─────────────────────────────────────────────────────────");
    char row[160];
    for (int i = g.n_sales - 1; i >= 0; i--) {
        const Sale *s = &g.sales[i];
        snprintf(row, sizeof row, "  #%d  %s   عناصر: %d   الإجمالي: %.2f دج",
                 s->id, s->date_str, s->n_items, s->total);
        PRINTLN(row);
    }
    PRINTLN("  ─────────────────────────────────────────────────────────");
    int id = 0;
    if (!ask_int("  رقم الفاتورة لعرض التفاصيل (أو 0 للرجوع): ", &id, 0, 1000000)) return;
    if (!id) return;
    Sale *s = sale_find(&g, id);
    if (!s) {
        warn("لا توجد فاتورة بهذا الرقم");
        return;
    }
    print_receipt(s);
}

/* ------------------------------------------------------------------ */
/* التقارير                                                            */
/* ------------------------------------------------------------------ */

static void reports(void)
{
    PRINTLN("");
    line();
    PRINTLN("                  التقارير والإحصائيات");
    line();
    char r[128];
    snprintf(r, sizeof r, "  إجمالي المبيعات:   %14.2f دج", rep_total_sales(&g));
    PRINTLN(r);
    snprintf(r, sizeof r, "  إجمالي الأرباح:    %14.2f دج", rep_total_profit(&g));
    PRINTLN(r);
    snprintf(r, sizeof r, "  عدد الفواتير:      %14d", g.n_sales);
    PRINTLN(r);
    snprintf(r, sizeof r, "  مبيعات اليوم:       %14.2f دج", rep_sales_today(&g));
    PRINTLN(r);
    line();

    PRINTLN("  الأكثر مبيعاً:");
    int ids[5], qs[5];
    int n = rep_top_products(&g, ids, qs, 5);
    if (n == 0)
        PRINTLN("    لا توجد بيانات بعد.");
    for (int i = 0; i < n; i++) {
        Product *p = product_find(&g, ids[i]);
        snprintf(r, sizeof r, "    %d. %s — %d وحدة", i + 1, p ? p->name : "؟", qs[i]);
        PRINTLN(r);
    }

    PRINTLN("");
    PRINTLN("  تنبيه المخزون (5 أو أقل):");
    int lids[32];
    int m = rep_low_stock(&g, lids, 32);
    if (m == 0)
        PRINTLN("    كل شيء على ما يرام ✔");
    for (int i = 0; i < m; i++) {
        Product *p = product_find(&g, lids[i]);
        if (!p) continue;
        snprintf(r, sizeof r, "    ⚠ %s — المتبقي: %d", p->name, p->qty);
        PRINTLN(r);
    }
    line();
}

/* ------------------------------------------------------------------ */
/* القائمة الرئيسية                                                    */
/* ------------------------------------------------------------------ */

static void main_menu(void)
{
    PRINTLN("");
    line();
    PRINTLN("   (1) البيع — فاتورة جديدة");
    PRINTLN("   (2) إدارة المنتجات");
    PRINTLN("   (3) سجل المبيعات");
    PRINTLN("   (4) التقارير والإحصائيات");
    PRINTLN("   (5) حفظ ومغادرة");
    line();
}

static int main_choice(int *out)
{
    for (;;) {
        char buf[16];
        PRINT("  اختر: ");
        if (!io_read_line(buf, sizeof buf)) return 0;
        if (!*buf) continue; /* Enter فارغ = إعادة عرض القائمة */
        char *end = NULL;
        long v = strtol(buf, &end, 10);
        if (*end || v < 1 || v > 5) {
            warn("اختر رقماً من 1 إلى 5");
            continue;
        }
        *out = (int)v;
        return 1;
    }
}

int main(void)
{
    io_init();
    make_data_path();
    banner();

    app_init(&g); /* التهيئة قبل أي قراءة (global يبدأ صفراً) */
    int rc = json_load(&g, g_data_path);
    if (rc == 1) {
        PRINTLN("  تم تحميل البيانات المحفوظة.");
    } else if (rc == 0) {
        PRINTLN("  أول تشغيل — لا توجد بيانات سابقة.");
        PRINTLN("");
        if (ask_yes_no("  هل تريد تحميل بيانات تجريبية؟ (ن/لا) ") > 0)
            load_sample();
    } else {
        warn("تعذر قراءة ملف البيانات — سأبدأ من جديد");
        app_init(&g);
    }

    for (;;) {
        main_menu();
        int ch = 0;
        if (!main_choice(&ch)) break;
        if (ch == 1) new_sale();
        else if (ch == 2) products_menu();
        else if (ch == 3) sales_history();
        else if (ch == 4) reports();
        else if (ch == 5) break;
    }

    save();
    PRINTLN("");
    PRINTLN("  ✔ تم حفظ البيانات. إلى اللقاء!");
    PRINTLN("");
    PRINT("  اضغط Enter للخروج...");
    char buf[16];
    io_read_line(buf, sizeof buf);
    return 0;
}
