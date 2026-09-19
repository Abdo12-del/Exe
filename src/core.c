/*
 * core.c — منطق الأعمال: المنتجات، المبيعات، التقارير، وحفظ/تحميل JSON
 * (قابلة للاختبار على أي نظام — لا تعتمد على واجهة)
 */
#include "core.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>

/* ------------------------------------------------------------------ */
/* تهيئة وإدارة المنتجات                                                */
/* ------------------------------------------------------------------ */

void app_init(AppData *d)
{
    memset(d, 0, sizeof *d);
    d->next_product_id = 1;
    d->next_sale_id = 1;
}

/* 0..n: معرف المنتج الجديد، -1 مدخلات غير صالحة، -2 الاسم موجود */
int product_add(AppData *d, const char *name, double buy, double sell, int qty)
{
    if (!name || !*name) return -1;
    if (buy < 0 || sell < 0 || qty < 0) return -1;
    if (!isfinite(buy) || !isfinite(sell)) return -1;
    if (d->n_products >= MAX_PRODUCTS) return -1;
    if (product_find_by_name(d, name)) return -2;

    Product *p = &d->products[d->n_products];
    d->n_products++;
    p->id = d->next_product_id++;
    snprintf(p->name, sizeof p->name, "%s", name);
    p->buy_price = buy;
    p->sell_price = sell;
    p->qty = qty;
    return p->id;
}

Product *product_find(AppData *d, int id)
{
    for (int i = 0; i < d->n_products; i++)
        if (d->products[i].id == id) return &d->products[i];
    return NULL;
}

Product *product_find_by_name(AppData *d, const char *name)
{
    for (int i = 0; i < d->n_products; i++)
        if (strcmp(d->products[i].name, name) == 0) return &d->products[i];
    return NULL;
}

int product_remove(AppData *d, int id)
{
    for (int i = 0; i < d->n_products; i++) {
        if (d->products[i].id == id) {
            memmove(&d->products[i], &d->products[i + 1],
                    (size_t)(d->n_products - i - 1) * sizeof(Product));
            d->n_products--;
            return 0;
        }
    }
    return -1;
}

/* ------------------------------------------------------------------ */
/* المبيعات                                                            */
/* ------------------------------------------------------------------ */

int sale_create(AppData *d, const int *ids, const int *qtys, int n, double *total_out)
{
    if (!ids || !qtys || n <= 0 || n > MAX_ITEMS) return -1;
    if (d->n_sales >= MAX_SALES) return -3;

    /* فحص المخزون لكل العناصر أولاً حتى لا تُخصم كميات جزئياً عند الفشل */
    for (int i = 0; i < n; i++) {
        Product *p = product_find(d, ids[i]);
        if (!p) return -1;
        if (qtys[i] <= 0 || qtys[i] > p->qty) return -2;
    }

    Sale s;
    memset(&s, 0, sizeof s);
    s.id = d->next_sale_id++;
    s.ts = time(NULL);
    now_str(s.date_str, sizeof s.date_str, s.ts);

    double total = 0.0, profit = 0.0;
    for (int i = 0; i < n; i++) {
        Product *p = product_find(d, ids[i]);
        SaleItem *it = &s.items[s.n_items];
        it->product_id = p->id;
        snprintf(it->name, sizeof it->name, "%s", p->name);
        it->sell_price = p->sell_price;
        it->buy_price = p->buy_price;
        it->qty = qtys[i];
        s.n_items++;

        total += p->sell_price * qtys[i];
        profit += (p->sell_price - p->buy_price) * qtys[i];
        p->qty -= qtys[i];
    }
    s.total = total;
    s.profit = profit;

    d->sales[d->n_sales] = s;
    d->n_sales++;
    if (total_out) *total_out = total;
    return 0;
}

Sale *sale_find(AppData *d, int id)
{
    for (int i = 0; i < d->n_sales; i++)
        if (d->sales[i].id == id) return &d->sales[i];
    return NULL;
}

/* ------------------------------------------------------------------ */
/* أدوات زمنية                                                        */
/* ------------------------------------------------------------------ */

void now_str(char *buf, size_t n, time_t ts)
{
    if (n == 0) return;
    struct tm *t = localtime(&ts);
    if (!t || strftime(buf, n, "%Y-%m-%d %H:%M", t) == 0) {
        buf[0] = '?';
        buf[1] = 0;
    }
}

int same_day(time_t a, time_t b)
{
    struct tm *ta = localtime(&a);
    struct tm *tb = localtime(&b);
    if (!ta || !tb) return 0;
    return ta->tm_year == tb->tm_year && ta->tm_yday == tb->tm_yday;
}

/* ------------------------------------------------------------------ */
/* التقارير                                                            */
/* ------------------------------------------------------------------ */

double rep_total_sales(const AppData *d)
{
    double t = 0;
    for (int i = 0; i < d->n_sales; i++) t += d->sales[i].total;
    return t;
}

double rep_total_profit(const AppData *d)
{
    double t = 0;
    for (int i = 0; i < d->n_sales; i++) t += d->sales[i].profit;
    return t;
}

double rep_sales_today(const AppData *d)
{
    time_t now = time(NULL);
    double t = 0;
    for (int i = 0; i < d->n_sales; i++)
        if (same_day(d->sales[i].ts, now)) t += d->sales[i].total;
    return t;
}

int rep_top_products(const AppData *d, int *ids_out, int *qtys_out, int max_n)
{
    if (!ids_out || !qtys_out || max_n <= 0) return 0;
    int ids[MAX_PRODUCTS], qs[MAX_PRODUCTS];
    for (int i = 0; i < d->n_products; i++) { ids[i] = d->products[i].id; qs[i] = 0; }

    for (int i = 0; i < d->n_sales; i++)
        for (int k = 0; k < d->sales[i].n_items; k++) {
            int pid = d->sales[i].items[k].product_id;
            for (int j = 0; j < d->n_products; j++)
                if (ids[j] == pid) qs[j] += d->sales[i].items[k].qty;
        }

    int n = 0;
    for (int i = 0; i < max_n && i < d->n_products; i++) {
        int best = -1;
        for (int j = 0; j < d->n_products; j++)
            if (qs[j] > 0 && (best < 0 || qs[j] > qs[best])) best = j;
        if (best < 0) break;
        ids_out[n] = ids[best];
        qtys_out[n] = qs[best];
        qs[best] = 0;
        n++;
    }
    return n;
}

int rep_low_stock(const AppData *d, int *ids_out, int max_n)
{
    if (!ids_out || max_n <= 0) return 0;
    int n = 0;
    for (int i = 0; i < d->n_products && n < max_n; i++)
        if (d->products[i].qty <= LOW_STOCK) ids_out[n++] = d->products[i].id;
    return n;
}

/* ================================================================== */
/* JSON — مولّد بسيط                                                   */
/* ================================================================== */

static void jesc(FILE *f, const char *s)
{
    fputc('"', f);
    for (const unsigned char *c = (const unsigned char *)s; *c; c++) {
        switch (*c) {
        case '"':  fputs("\\\"", f); break;
        case '\\': fputs("\\\\", f); break;
        case '\n': fputs("\\n", f);  break;
        case '\r': fputs("\\r", f);  break;
        case '\t': fputs("\\t", f);  break;
        default:
            if (*c < 0x20) fprintf(f, "\\u%04x", *c);
            else fputc(*c, f);
        }
    }
    fputc('"', f);
}

static void emit_items(FILE *f, const Sale *s)
{
    for (int i = 0; i < s->n_items; i++) {
        const SaleItem *it = &s->items[i];
        fprintf(f, "%s\n        {\"id\": %d, \"name\": ", i ? ", " : "", it->product_id);
        jesc(f, it->name);
        fprintf(f, ", \"sell\": %.2f, \"buy\": %.2f, \"qty\": %d}",
                it->sell_price, it->buy_price, it->qty);
    }
}

int json_save(const AppData *d, const char *path)
{
    FILE *f = fopen(path, "wb");
    if (!f) return -1;

    fprintf(f, "{\n  \"next_product_id\": %d,\n  \"next_sale_id\": %d,\n",
            d->next_product_id, d->next_sale_id);

    fprintf(f, "  \"products\": [");
    for (int i = 0; i < d->n_products; i++) {
        const Product *p = &d->products[i];
        fprintf(f, "%s\n    {\"id\": %d, \"name\": ", i ? "," : "", p->id);
        jesc(f, p->name);
        fprintf(f, ", \"buy\": %.2f, \"sell\": %.2f, \"qty\": %d}",
                p->buy_price, p->sell_price, p->qty);
    }
    fprintf(f, "%s],\n", d->n_products ? "\n  " : "");

    fprintf(f, "  \"sales\": [");
    for (int i = 0; i < d->n_sales; i++) {
        const Sale *s = &d->sales[i];
        fprintf(f, "%s\n    {\"id\": %d, \"ts\": %lld, \"total\": %.2f, \"profit\": %.2f, \"items\": [",
                i ? "," : "", s->id, (long long)s->ts, s->total, s->profit);
        emit_items(f, s);
        fprintf(f, "%s]}", d->n_sales ? "\n    " : "");
    }
    fprintf(f, "%s]\n}\n", d->n_sales ? "\n  " : "");

    if (fclose(f) != 0) return -1;
    return 0;
}

/* ================================================================== */
/* JSON — محلل (شجرة عامة ثم استخرج وفق المخطط)                          */
/* ================================================================== */

typedef enum { JT_NULL, JT_NUM, JT_STR, JT_OBJ, JT_ARR } JType;

typedef struct JNode {
    JType type;
    double num;
    char *str;            /* JT_STR */
    struct JNode **kids;  /* عناصر المصفوفة / قيم الكائن */
    char **keys;          /* JT_OBJ فقط */
    int nk;
} JNode;

typedef struct {
    const char *s;
    size_t i, n;
    int depth;
    int err;
} JP;

static void skip_ws(JP *p)
{
    while (p->i < p->n) {
        char c = p->s[p->i];
        if (c == ' ' || c == '\t' || c == '\n' || c == '\r') p->i++;
        else break;
    }
}

static int parse_hex4(JP *p, unsigned *out)
{
    if (p->i + 4 > p->n) return -1;
    unsigned v = 0;
    for (int k = 0; k < 4; k++) {
        char c = p->s[p->i++];
        v <<= 4;
        if (c >= '0' && c <= '9') v |= (unsigned)(c - '0');
        else if (c >= 'a' && c <= 'f') v |= (unsigned)(c - 'a' + 10);
        else if (c >= 'A' && c <= 'F') v |= (unsigned)(c - 'A' + 10);
        else return -1;
    }
    *out = v;
    return 0;
}

static char *parse_string(JP *p)
{
    /* s[i] == '"' */
    p->i++;
    size_t cap = 16, len = 0;
    char *buf = (char *)malloc(cap);
    if (!buf) { p->err = 1; return NULL; }

    while (p->i < p->n) {
        unsigned char c = (unsigned char)p->s[p->i++];
        if (c == '"') { buf[len] = 0; return buf; }
        if (c == '\\') {
            if (p->i >= p->n) { free(buf); p->err = 1; return NULL; }
            char e = p->s[p->i++];
            switch (e) {
            case '"':  c = '"';  break;
            case '\\': c = '\\'; break;
            case '/':  c = '/';  break;
            case 'b':  c = '\b'; break;
            case 'f':  c = '\f'; break;
            case 'n':  c = '\n'; break;
            case 'r':  c = '\r'; break;
            case 't':  c = '\t'; break;
            case 'u': {
                unsigned cp = 0;
                if (parse_hex4(p, &cp)) { free(buf); p->err = 1; return NULL; }
                if (cp >= 0xD800 && cp <= 0xDBFF) {
                    if (p->i + 1 < p->n && p->s[p->i] == '\\' && p->s[p->i + 1] == 'u') {
                        p->i += 2;
                        unsigned lo = 0;
                        if (parse_hex4(p, &lo) || lo < 0xDC00 || lo > 0xDFFF) {
                            free(buf); p->err = 1; return NULL;
                        }
                        cp = 0x10000u + ((cp - 0xD800) << 10) + (lo - 0xDC00);
                    } else {
                        free(buf); p->err = 1; return NULL;
                    }
                }
                if (len + 5 > cap) {
                    cap *= 2;
                    char *t = (char *)realloc(buf, cap);
                    if (!t) { free(buf); p->err = 1; return NULL; }
                    buf = t;
                }
                if (cp < 0x80) {
                    buf[len++] = (char)cp;
                } else if (cp < 0x800) {
                    buf[len++] = (char)(0xC0 | (cp >> 6));
                    buf[len++] = (char)(0x80 | (cp & 0x3F));
                } else if (cp < 0x10000) {
                    buf[len++] = (char)(0xE0 | (cp >> 12));
                    buf[len++] = (char)(0x80 | ((cp >> 6) & 0x3F));
                    buf[len++] = (char)(0x80 | (cp & 0x3F));
                } else {
                    buf[len++] = (char)(0xF0 | (cp >> 18));
                    buf[len++] = (char)(0x80 | ((cp >> 12) & 0x3F));
                    buf[len++] = (char)(0x80 | ((cp >> 6) & 0x3F));
                    buf[len++] = (char)(0x80 | (cp & 0x3F));
                }
                continue;
            }
            default:
                free(buf); p->err = 1; return NULL;
            }
        }
        if (len + 2 > cap) {
            cap *= 2;
            char *t = (char *)realloc(buf, cap);
            if (!t) { free(buf); p->err = 1; return NULL; }
            buf = t;
        }
        buf[len++] = (char)c;
    }
    free(buf);
    p->err = 1;
    return NULL;
}

static JNode *new_node(JP *p, JType t)
{
    JNode *n = (JNode *)calloc(1, sizeof *n);
    if (!n) p->err = 1;
    else n->type = t;
    return n;
}

static JNode *pv_inner(JP *p);

static JNode *parse_value(JP *p)
{
    if (++p->depth > 64) { p->err = 1; return NULL; }
    JNode *r = pv_inner(p);
    p->depth--;
    return r;
}

static JNode *pv_inner(JP *p)
{
    skip_ws(p);
    if (p->i >= p->n) { p->err = 1; return NULL; }
    char c = p->s[p->i];

    if (c == '"') {
        char *str = parse_string(p);
        if (p->err) return NULL;
        JNode *n = new_node(p, JT_STR);
        if (!n) return NULL;
        n->str = str;
        return n;
    }

    if (c == '{') {
        p->i++;
        JNode *n = new_node(p, JT_OBJ);
        if (!n) return NULL;
        skip_ws(p);
        if (p->i < p->n && p->s[p->i] == '}') { p->i++; return n; }
        for (;;) {
            skip_ws(p);
            if (p->i >= p->n || p->s[p->i] != '"') { p->err = 1; return n; }
            char *key = parse_string(p);
            if (p->err) return n;
            skip_ws(p);
            if (p->i >= p->n || p->s[p->i] != ':') { free(key); p->err = 1; return n; }
            p->i++;
            JNode *v = parse_value(p);
            if (p->err) return n;
            n->nk++;
            n->keys = (char **)realloc(n->keys, sizeof(char *) * (size_t)n->nk);
            n->kids = (JNode **)realloc(n->kids, sizeof(JNode *) * (size_t)n->nk);
            if (!n->keys || !n->kids) { p->err = 1; return n; }
            n->keys[n->nk - 1] = key;
            n->kids[n->nk - 1] = v;
            skip_ws(p);
            if (p->i < p->n && p->s[p->i] == ',') { p->i++; continue; }
            if (p->i < p->n && p->s[p->i] == '}') { p->i++; return n; }
            p->err = 1;
            return n;
        }
    }

    if (c == '[') {
        p->i++;
        JNode *n = new_node(p, JT_ARR);
        if (!n) return NULL;
        skip_ws(p);
        if (p->i < p->n && p->s[p->i] == ']') { p->i++; return n; }
        for (;;) {
            JNode *v = parse_value(p);
            if (p->err) return n;
            n->nk++;
            n->kids = (JNode **)realloc(n->kids, sizeof(JNode *) * (size_t)n->nk);
            if (!n->kids) { p->err = 1; return n; }
            n->kids[n->nk - 1] = v;
            skip_ws(p);
            if (p->i < p->n && p->s[p->i] == ',') { p->i++; continue; }
            if (p->i < p->n && p->s[p->i] == ']') { p->i++; return n; }
            p->err = 1;
            return n;
        }
    }

    if (c == 't') {
        if (p->i + 4 <= p->n && strncmp(p->s + p->i, "true", 4) == 0) {
            p->i += 4;
            JNode *n = new_node(p, JT_NUM);
            if (n) n->num = 1;
            return n;
        }
        p->err = 1; return NULL;
    }
    if (c == 'f') {
        if (p->i + 5 <= p->n && strncmp(p->s + p->i, "false", 5) == 0) {
            p->i += 5;
            JNode *n = new_node(p, JT_NUM);
            if (n) n->num = 0;
            return n;
        }
        p->err = 1; return NULL;
    }
    if (c == 'n') {
        if (p->i + 4 <= p->n && strncmp(p->s + p->i, "null", 4) == 0) {
            p->i += 4;
            return new_node(p, JT_NULL);
        }
        p->err = 1; return NULL;
    }

    /* رقم */
    char *end = NULL;
    double v = strtod(p->s + p->i, &end);
    if (end == p->s + p->i) { p->err = 1; return NULL; }
    p->i += (size_t)(end - (p->s + p->i));
    JNode *n = new_node(p, JT_NUM);
    if (!n) return NULL;
    n->num = v;
    return n;
}

static void free_tree(JNode *n)
{
    if (!n) return;
    for (int i = 0; i < n->nk; i++) {
        free_tree(n->kids[i]);
        free(n->keys ? n->keys[i] : NULL);
    }
    free(n->kids);
    free(n->keys);
    free(n->str);
    free(n);
}

static JNode *jfind_obj(JNode *o, const char *key)
{
    if (!o || o->type != JT_OBJ) return NULL;
    for (int i = 0; i < o->nk; i++)
        if (o->keys[i] && strcmp(o->keys[i], key) == 0) return o->kids[i];
    return NULL;
}

int json_load(AppData *d, const char *path)
{
    FILE *f = fopen(path, "rb");
    if (!f) return 0;

    if (fseek(f, 0, SEEK_END) != 0) { fclose(f); return -1; }
    long sz = ftell(f);
    if (sz <= 0 || sz > 16 * 1024 * 1024) { fclose(f); return -1; }
    if (fseek(f, 0, SEEK_SET) != 0) { fclose(f); return -1; }

    char *buf = (char *)malloc((size_t)sz + 1);
    if (!buf) { fclose(f); return -1; }
    size_t rd = fread(buf, 1, (size_t)sz, f);
    fclose(f);
    buf[rd] = 0;

    JP p;
    p.s = buf; p.i = 0; p.n = rd; p.depth = 0; p.err = 0;
    JNode *root = parse_value(&p);

    int rc = -1;
    if (root && !p.err && root->type == JT_OBJ) {
        app_init(d);
        JNode *v;

        if ((v = jfind_obj(root, "next_product_id")) && v->type == JT_NUM)
            d->next_product_id = (int)v->num;
        if ((v = jfind_obj(root, "next_sale_id")) && v->type == JT_NUM)
            d->next_sale_id = (int)v->num;

        if ((v = jfind_obj(root, "products")) && v->type == JT_ARR) {
            for (int i = 0; i < v->nk && d->n_products < MAX_PRODUCTS; i++) {
                JNode *o = v->kids[i];
                JNode *a = jfind_obj(o, "id"), *nm = jfind_obj(o, "name"),
                      *b = jfind_obj(o, "buy"), *s = jfind_obj(o, "sell"),
                      *q = jfind_obj(o, "qty");
                if (!a || !nm || nm->type != JT_STR || !b || !s || !q) continue;
                Product *pr = &d->products[d->n_products];
                d->n_products++;
                pr->id = (int)a->num;
                snprintf(pr->name, sizeof pr->name, "%s", nm->str);
                pr->buy_price = b->num;
                pr->sell_price = s->num;
                pr->qty = (int)q->num;
            }
        }

        if ((v = jfind_obj(root, "sales")) && v->type == JT_ARR) {
            for (int i = 0; i < v->nk && d->n_sales < MAX_SALES; i++) {
                JNode *o = v->kids[i];
                JNode *a = jfind_obj(o, "id"), *ts = jfind_obj(o, "ts"),
                      *tt = jfind_obj(o, "total"), *pf = jfind_obj(o, "profit"),
                      *items = jfind_obj(o, "items");
                if (!a || !ts) continue;
                Sale *sl = &d->sales[d->n_sales];
                d->n_sales++;
                sl->id = (int)a->num;
                sl->ts = (time_t)(ts->num);
                sl->total = tt ? tt->num : 0.0;
                sl->profit = pf ? pf->num : 0.0;
                now_str(sl->date_str, sizeof sl->date_str, sl->ts);
                if (items && items->type == JT_ARR) {
                    for (int k = 0; k < items->nk && sl->n_items < MAX_ITEMS; k++) {
                        JNode *io = items->kids[k];
                        JNode *iid = jfind_obj(io, "id"), *nm = jfind_obj(io, "name"),
                              *sp = jfind_obj(io, "sell"), *bp = jfind_obj(io, "buy"),
                              *q = jfind_obj(io, "qty");
                        if (!iid || !nm || nm->type != JT_STR || !sp || !q) continue;
                        SaleItem *it = &sl->items[sl->n_items];
                        sl->n_items++;
                        it->product_id = (int)iid->num;
                        snprintf(it->name, sizeof it->name, "%s", nm->str);
                        it->sell_price = sp->num;
                        it->buy_price = bp ? bp->num : 0.0;
                        it->qty = (int)q->num;
                    }
                }
            }
        }

        if (d->next_product_id <= 0) d->next_product_id = 1;
        if (d->next_sale_id <= 0) d->next_sale_id = 1;
        rc = 1;
    }

    free_tree(root);
    free(buf);
    return rc;
}
