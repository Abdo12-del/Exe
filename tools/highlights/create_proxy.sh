#!/usr/bin/env bash
# create_proxy.sh — يصنع نسخة مضغوطة (بروكسي) من فيديو طويل لرفعها
# الاستخدام:  bash create_proxy.sh /مسار/الفيديو.mp4   [ارتفاع]  [crf]
set -euo pipefail

IN="${1:-}"
H="${2:-360}"
CRF="${3:-32}"
[ -n "$IN" ] || { echo "الاستخدام: bash $0 الفيديو.mp4 [360] [32]"; exit 1; }
[ -f "$IN" ] || { echo "الملف غير موجود: $IN"; exit 1; }

FF="${FFMPEG:-$(command -v ffmpeg || true)}"
[ -n "$FF" ] || { echo "ffmpeg غير موجود. ثبّته: sudo apt install ffmpeg  /  brew install ffmpeg"; exit 1; }

OUT="${IN%.*}_proxy_${H}p.mp4"
echo "المدخل : $IN"
echo "المخرج : $OUT"
"$FF" -hide_banner -loglevel error -stats -i "$IN" \
  -vf "scale=-2:$H" \
  -c:v libx264 -preset veryfast -crf "$CRF" -pix_fmt yuv420p \
  -c:a aac -b:a 48k -ac 1 -ar 44100 \
  -movflags +faststart "$OUT"

SIZE=$(wc -c < "$OUT")
echo "الحجم: $SIZE بايت"
if [ "$SIZE" -gt 99000000 ]; then
  echo "أكبر من 95 ميجابايت — جارٍ التجزئة إلى أجزاء 90 ميجابايت..."
  split -d -a 2 -b 90M "$OUT" "${OUT}.part"
  echo "تم: ${OUT}.part00 , ${OUT}.part01 , ..."
  echo "لإعادة التجميع لاحقاً: cat ${OUT}.part* > $OUT"
fi
echo "✅ انتهى"
