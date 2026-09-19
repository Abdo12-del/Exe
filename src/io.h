/*
 * io.h — طبقة إدخال/إخراج نصية (UTF-8 داخلياً)
 * على ويندوز تستخدم دوال وحدة التحكم الواسعة (ReadConsoleW/WriteConsoleW)
 * فتظهر العربية بشكل صحيح دون الحاجة لتغيير صفحة الترميز.
 */
#ifndef IO_H
#define IO_H

#include <stddef.h>

void io_init(void);

void  io_out(const char *utf8);              /* بدون نهاية سطر */
void  io_outln(const char *utf8);            /* مع نهاية سطر */
int   io_read_line(char *buf, size_t cap);   /* سطر مقصوص: 1 نجاح، 0 EOF */

#endif /* IO_H */
