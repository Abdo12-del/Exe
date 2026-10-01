#!/usr/bin/env bash
# يولّد فيديو اختبار صناعي (~125 ثانية) يحتوي مقاطع مملة/صامتة/مظلمة ومقاطع حيوية،
# لاختبار أداة استخراج أفضل اللحظات بدون الحاجة إلى مقاطع حقيقية.
#
# الاستخدام:  FFMPEG=/مسار/ffmpeg  bash tools/highlights/make_test_footage.sh [مسار_الإخراج]
set -euo pipefail
OUT="${1:-test_footage.mp4}"
FF="${FFMPEG:-$(command -v ffmpeg || true)}"
if [ -z "$FF" ]; then
  FF="$(python3 -c "import imageio_ffmpeg;print(imageio_ffmpeg.get_ffmpeg_exe())" 2>/dev/null || true)"
fi
[ -n "$FF" ] || { echo "لم يتم العثور على ffmpeg (ثبّت المتطلبات: bash tools/highlights/setup.sh)"; exit 1; }

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

gen () { # name vsrc asrc dur
  "$FF" -hide_banner -loglevel error -y \
    -f lavfi -i "$2" -f lavfi -i "$3" -t "$4" \
    -vf "scale=640:360,fps=25" -af "aformat=sample_rates=44100:channel_layouts=stereo" \
    -c:v libx264 -preset ultrafast -pix_fmt yuv420p -c:a aac -shortest "$TMP/$1_raw.mp4"
}
post () { # name vf af
  "$FF" -hide_banner -loglevel error -y -i "$TMP/$1_raw.mp4" -vf "$2" -af "$3" \
      -c:v libx264 -preset ultrafast -pix_fmt yuv420p -c:a aac "$TMP/$1.mp4"
}

# 0-20s: ثابت + شبه صامت (يجب أن تتجنبه الأداة)
gen 01 "smptehdbars=size=640x360:rate=25" "sine=frequency=200:sample_rate=44100" 20
post 01 "null" "volume=0.01"
# 20-45s: حركة عالية + صوت مرتفع
gen 02 "testsrc2=size=640x360:rate=25" "sine=frequency=440:sample_rate=44100" 25
post 02 "null" "volume='0.35+0.25*sin(2*PI*0.7*t)':eval=frame"
# 45-60s: مظلم + ضجيج (يجب أن تتجنبه الأداة)
gen 03 "testsrc2=size=640x360:rate=25" "sine=frequency=150:sample_rate=44100" 15
post 03 "eq=brightness=-0.35:contrast=0.55,noise=alls=60:allf=t+u" "volume=0.05"
# 60-80s: حركة + صوت متوسط
gen 04 "mandelbrot=size=640x360:rate=25" "sine=frequency=520:sample_rate=44100" 20
post 04 "null" "volume='0.3+0.2*sin(2*PI*1.3*t)':eval=frame"
# 80-100s: ثابت + صامت
gen 05 "smptebars=size=640x360:rate=25" "sine=frequency=180:sample_rate=44100" 20
post 05 "null" "volume=0.008"
# 100-125s: حركة عالية + صوت
gen 06 "life=size=640x360:rate=25:mold=10:life_color=#00ff00" "sine=frequency=700:sample_rate=44100" 25
post 06 "null" "volume='0.4+0.2*sin(2*PI*0.9*t)':eval=frame"

: > "$TMP/list.txt"
for n in 01 02 03 04 05 06; do echo "file '$TMP/$n.mp4'" >> "$TMP/list.txt"; done
"$FF" -hide_banner -loglevel error -y -f concat -safe 0 -i "$TMP/list.txt" -c copy "$OUT"
echo "✔ تم إنشاء: $OUT"
"$FF" -hide_banner -i "$OUT" 2>&1 | grep -E "Duration|Stream"
