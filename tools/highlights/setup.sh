#!/usr/bin/env bash
# تهيئة بيئة عمل أداة استخراج أفضل المقاطع
# Usage: bash tools/highlights/setup.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
VENV="${VIDEO_VENV:-$HOME/.venv-video}"

echo "==> إنشاء البيئة الافتراضية: $VENV"
python3 -m venv "$VENV"
"$VENV/bin/pip" install -q --upgrade pip
"$VENV/bin/pip" install -q -r "$ROOT/tools/highlights/requirements.txt"

FFMPEG_BIN="$("$VENV/bin/python" -c "import imageio_ffmpeg;print(imageio_ffmpeg.get_ffmpeg_exe())")"

echo "==> ffmpeg: $FFMPEG_BIN"
"$FFMPEG_BIN" -hide_banner -version | head -1

echo
echo "==> جاهز. مثال تشغيل:"
echo "    $VENV/bin/python $ROOT/tools/highlights/highlights.py --input \"$ROOT/media/in\" --out \"$ROOT/media/out\" --top 5 --vertical crop"
