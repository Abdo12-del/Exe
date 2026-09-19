#!/bin/sh
# ============================================================
#  سكربت بناء برنامج تسيير المبيعات
#
#  يبني:
#   1) اختبارات المنطق (تُشغَّل على لينكس)
#   2) نسخة لينكس للتجربة المحلية
#   3) dist/taseer.exe — نسخة ويندوز 64-bit (لا تحتاج أي أدوات إضافية)
#
#  المتطلبات: Zig (يمكن تثبيته عبر pip install ziglang)
# ============================================================
set -e
cd "$(dirname "$0")"

if command -v zig >/dev/null 2>&1; then
    ZIG="zig"
elif [ -x /home/user/.zigenv/bin/python ]; then
    ZIG="/home/user/.zigenv/bin/python -m ziglang"
else
    echo "خطأ: لم يتم العثور على Zig — ثبّته أولاً: pip install ziglang" >&2
    exit 1
fi

mkdir -p dist

echo "==> تشغيل اختبارات المنطق..."
$ZIG cc src/test_core.c src/core.c -o dist/taseer_test -O2
dist/taseer_test

echo "==> التحقق من صلاحية JSON بمحلل مستقل..."
python3 -m json.tool /tmp/taseer_last.json > /dev/null

echo "==> بناء نسخة لينكس (للتجربة)..."
$ZIG cc src/main.c src/core.c src/io.c -o dist/taseer -O2

echo "==> بناء نسخة ويندوز taseer.exe..."
$ZIG cc -target x86_64-windows src/main.c src/core.c src/io.c -O2 -s -o dist/taseer.exe

echo ""
echo "تم البناء بنجاح: dist/taseer.exe"
ls -la dist/
