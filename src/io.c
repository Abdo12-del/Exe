/*
 * io.c — تنفيذ طبقة الإدخال/الإخراج لكل نظام
 */
#include "io.h"

#include <stdio.h>
#include <string.h>

#if defined(_WIN32)
#include <windows.h>

static HANDLE g_out = NULL;
static HANDLE g_in = NULL;
static int g_out_console = 0;
static int g_in_console = 0;

static int utf8_to_utf16(const char *s, wchar_t *w, size_t cap)
{
    size_t len = strlen(s), i = 0, o = 0;
    while (i < len && o < cap) {
        unsigned c = (unsigned char)s[i];
        unsigned cp;
        if (c < 0x80) {
            cp = c; i += 1;
        } else if ((c & 0xE0) == 0xC0) {
            if (i + 1 >= len) break;
            cp = ((unsigned)(c & 0x1F) << 6) | ((unsigned char)s[i + 1] & 0x3F);
            i += 2;
        } else if ((c & 0xF0) == 0xE0) {
            if (i + 2 >= len) break;
            cp = ((unsigned)(c & 0x0F) << 12) |
                 (((unsigned char)s[i + 1] & 0x3F) << 6) |
                 ((unsigned char)s[i + 2] & 0x3F);
            i += 3;
        } else if ((c & 0xF8) == 0xF0) {
            if (i + 3 >= len) break;
            cp = ((unsigned)(c & 0x07) << 18) |
                 (((unsigned char)s[i + 1] & 0x3F) << 12) |
                 (((unsigned char)s[i + 2] & 0x3F) << 6) |
                 ((unsigned char)s[i + 3] & 0x3F);
            i += 4;
        } else {
            w[o++] = (wchar_t)'?';
            i += 1;
            continue;
        }
        if (cp > 0xFFFF) {
            cp -= 0x10000;
            if (o + 1 >= cap) break;
            w[o++] = (wchar_t)(0xD800 + ((cp >> 10) & 0x3FF));
            w[o++] = (wchar_t)(0xDC00 + (cp & 0x3FF));
        } else {
            w[o++] = (wchar_t)cp;
        }
    }
    return (int)o;
}

static int utf16_to_utf8(const wchar_t *w, int n, char *buf, size_t cap)
{
    int o = 0;
    for (int i = 0; i < n && (size_t)o + 5 < cap; i++) {
        unsigned cp = (unsigned)w[i];
        if (cp >= 0xD800 && cp <= 0xDBFF && i + 1 < n) {
            unsigned lo = (unsigned)w[i + 1];
            if (lo >= 0xDC00 && lo <= 0xDFFF) {
                cp = 0x10000u + ((cp - 0xD800) << 10) + (lo - 0xDC00);
                i++;
            } else {
                cp = '?';
            }
        }
        if (cp < 0x80) {
            buf[o++] = (char)cp;
        } else if (cp < 0x800) {
            buf[o++] = (char)(0xC0 | (cp >> 6));
            buf[o++] = (char)(0x80 | (cp & 0x3F));
        } else if (cp < 0x10000) {
            buf[o++] = (char)(0xE0 | (cp >> 12));
            buf[o++] = (char)(0x80 | ((cp >> 6) & 0x3F));
            buf[o++] = (char)(0x80 | (cp & 0x3F));
        } else {
            buf[o++] = (char)(0xF0 | (cp >> 18));
            buf[o++] = (char)(0x80 | ((cp >> 12) & 0x3F));
            buf[o++] = (char)(0x80 | ((cp >> 6) & 0x3F));
            buf[o++] = (char)(0x80 | (cp & 0x3F));
        }
    }
    buf[o] = 0;
    return o;
}

void io_init(void)
{
    g_out = GetStdHandle(STD_OUTPUT_HANDLE);
    g_in = GetStdHandle(STD_INPUT_HANDLE);
    if (g_out) g_out_console = (GetFileType(g_out) == FILE_TYPE_CHAR);
    if (g_in) g_in_console = (GetFileType(g_in) == FILE_TYPE_CHAR);
}

void io_out(const char *utf8)
{
    if (g_out_console) {
        wchar_t w[4096];
        int n = utf8_to_utf16(utf8, w, 4095);
        DWORD wr = 0;
        WriteConsoleW(g_out, w, (DWORD)n, &wr, NULL);
    } else {
        fputs(utf8, stdout);
        fflush(stdout);
    }
}

void io_outln(const char *utf8)
{
    io_out(utf8);
    io_out("\n");
}

int io_read_line(char *buf, size_t cap)
{
    if (cap == 0) return 0;
    buf[0] = 0;
    if (g_in_console) {
        wchar_t w[2048];
        DWORD rd = 0;
        if (!ReadConsoleW(g_in, w, 2047, &rd, NULL) || rd == 0) return 0;
        utf16_to_utf8(w, (int)rd, buf, cap);
        /* Echo: ReadConsoleW لا يعرض ما يكتبه المستخدم */
        io_out(buf);
        io_out("\n");
    } else {
        if (!fgets(buf, (int)cap, stdin)) return 0;
    }
    size_t l = strlen(buf);
    while (l > 0 && (buf[l - 1] == '\n' || buf[l - 1] == '\r' ||
                     buf[l - 1] == ' ' || buf[l - 1] == '\t'))
        buf[--l] = 0;
    return 1;
}

#else /* غير ويندوز: UTF-8 مباشرة */

void io_init(void) { /* لا شيء */ }

void io_out(const char *utf8)
{
    fputs(utf8, stdout);
    fflush(stdout);
}

void io_outln(const char *utf8)
{
    io_out(utf8);
    io_out("\n");
}

int io_read_line(char *buf, size_t cap)
{
    if (cap == 0) return 0;
    buf[0] = 0;
    if (!fgets(buf, (int)cap, stdin)) return 0;
    size_t l = strlen(buf);
    while (l > 0 && (buf[l - 1] == '\n' || buf[l - 1] == '\r' ||
                     buf[l - 1] == ' ' || buf[l - 1] == '\t'))
        buf[--l] = 0;
    return 1;
}

#endif /* _WIN32 */
