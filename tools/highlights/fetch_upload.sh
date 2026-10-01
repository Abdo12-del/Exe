#!/usr/bin/env bash
# fetch_upload.sh — ينزّل الفيديوهات التي رفعها المستخدم على GitHub، يعيد تجميع الأجزاء
# المجزّأة (7z / zip / split) ويضعها جاهزة في مجلد media/in لتحليلها.
#
# الاستخدام:
#   bash tools/highlights/fetch_upload.sh https://github.com/USER/REPO [branch] [dest]
#   bash tools/highlights/fetch_upload.sh --tarball /مسار/ملف.tar.gz [dest]
#
# ملاحظة: يعمل مع المستودعات العامة والخاصة (يستخدم gh auth token إن وُجد).

set -euo pipefail

VID_EXT_RE='\.(mp4|mov|mkv|avi|webm|m4v|mpg|mpeg|ts|flv|wmv)$'
PY="${PYTHON:-python3}"

log() { printf "\033[1;36m%s\033[0m\n" "$*"; }
err() { printf "\033[1;31m%s\033[0m\n" "$*" >&2; }

# ---------------------------------------------------------------- arguments
TARBALL=""
REPO_URL=""
BRANCH="main"
DEST=""

if [ "${1:-}" = "--tarball" ]; then
  TARBALL="${2:-}"
  DEST="${3:-media/in}"
else
  REPO_URL="${1:-}"
  BRANCH="${2:-main}"
  DEST="${3:-media/in}"
fi

[ -n "$TARBALL" ] || [ -n "$REPO_URL" ] || {
  echo "الاستخدام:"
  echo "  bash $0 https://github.com/USER/REPO [branch] [dest]"
  echo "  bash $0 --tarball ملف.tar.gz [dest]"
  exit 1
}

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
mkdir -p "$WORK/in" "$WORK/out" "$DEST"

# ---------------------------------------------------------------- download
if [ -n "$TARBALL" ]; then
  log "› استخدام أرشيف محلي: $TARBALL"
  cp "$TARBALL" "$WORK/repo.tar.gz"
else
  # استخراج USER/REPO من الرابط
  SLUG="$(echo "$REPO_URL" | sed -E 's#https?://(www\.)?github\.com/##; s#\.git$##; s#/$##')"
  OWNER="${SLUG%%/*}"
  REPO="${SLUG#*/}"
  [ -n "$OWNER" ] && [ -n "$REPO" ] && [ "$OWNER" != "$SLUG" ] || { err "رابط مستودع غير صالح: $REPO_URL"; exit 1; }

  TOKEN=""
  if command -v gh >/dev/null 2>&1; then TOKEN="$(gh auth token 2>/dev/null || true)"; fi

  log "› تنزيل $OWNER/$REPO@$BRANCH …"
  ok=0
  for REF in "$BRANCH" master; do
    ARGS=(-sL --max-time 1800 -o "$WORK/repo.tar.gz"
          -H "Accept: application/vnd.github+json")
    [ -n "$TOKEN" ] && ARGS+=(-H "Authorization: token $TOKEN")
    ARGS+=("https://api.github.com/repos/$OWNER/$REPO/tarball/$REF")
    code="$(curl "${ARGS[@]}" -w "%{http_code}" || echo 000)"
    if [ "$code" = "200" ] && [ -s "$WORK/repo.tar.gz" ]; then ok=1; break; fi
  done
  [ "$ok" = "1" ] || { err "تعذّر التنزيل (تأكد من اسم المستودع والفرع والصلاحيات)."; exit 1; }
  log "› حجم الأرشيف: $(du -h "$WORK/repo.tar.gz" | cut -f1)"
fi

# ---------------------------------------------------------------- extract
tar -xzf "$WORK/repo.tar.gz" -C "$WORK/in"
log "› تم فك الأرشيف."

# ---------------------------------------------------------------- reassemble
reassemble() { # $1 = مجلد البحث (بحث في كل المجلدات الفرعية)
  local dir="$1"
  # 1) أجزاء 7-Zip:  name.7z.001 / .002 …  (أو .000 …)
  find "$dir" -type f -name '*.7z.*' | sed -E 's/\.7z\.[0-9]+$//' | sort -u | while read -r base; do
    log "› تجميع أجزاء 7z: $(basename "$base")"
    cat "$base".7z.* > "$WORK/out/$(basename "$base").7z"
  done
  # 2) أجزاء ZIP: name.zip + name.z01 ...
  find "$dir" -type f -name '*.z[0-9][0-9]' | sed -E 's/\.z[0-9][0-9]$//' | sort -u | while read -r base; do
    [ -f "$base.zip" ] || continue
    log "› تجميع أجزاء ZIP: $(basename "$base")"
    cat "$base.zip" "$base".z[0-9][0-9] > "$WORK/out/$(basename "$base").zip"
  done
  # 3) أجزاء split:  name.mp4.part00 / .part01 …  أو  name.mp4.001 …
  find "$dir" -type f -name '*.part*' | sed -E 's/\.part[0-9]+$//' | sort -u | while read -r base; do
    log "› تجميع أجزاء split: $(basename "$base")"
    cat "$base".part* > "$WORK/out/$(basename "$base")"
  done
  find "$dir" -type f -name '*.[0-9][0-9][0-9]' -not -name '*.7z.*' | sed -E 's/\.[0-9]{3}$//' | sort -u | while read -r base; do
    log "› تجميع أجزاء مرقّمة: $(basename "$base")"
    cat "$base".[0-9][0-9][0-9] > "$WORK/out/$(basename "$base")"
  done
}
reassemble "$WORK/in"
# نسخ بقية الملفات (غير المجزّأة)
find "$WORK/in" -type f | while read -r f; do
  case "$(basename "$f")" in
    *.7z.*|*.z[0-9][0-9]|*.part[0-9]*|*.[0-9][0-9][0-9]|*.tar.gz|*.tgz) ;;
    *) cp -n "$f" "$WORK/out/" 2>/dev/null || true ;;
  esac
done

# ---------------------------------------------------------------- extract archives
for a in "$WORK/out"/*.7z; do
  [ -e "$a" ] || continue
  log "› فك ضغط 7z: $(basename "$a")"
  if command -v 7z >/dev/null 2>&1; then
    7z x -y -o"$WORK/out" "$a" >/dev/null
  elif command -v 7za >/dev/null 2>&1; then
    7za x -y -o"$WORK/out" "$a" >/dev/null
  else
    if ! "$PY" -c "import py7zr" >/dev/null 2>&1; then
      log "› تثبيت py7zr لفك ضغط 7z …"
      "$PY" -m venv /tmp/_7zvenv >/dev/null 2>&1 || true
      if [ -x /tmp/_7zvenv/bin/python ]; then
        /tmp/_7zvenv/bin/pip install -q py7zr >/dev/null 2>&1 || true
        PY=/tmp/_7zvenv/bin/python
      else
        pip install --quiet --break-system-packages py7zr >/dev/null 2>&1 || true
      fi
    fi
    "$PY" - "$a" "$WORK/out" <<'PYEOF'
import sys, py7zr
with py7zr.SevenZipFile(sys.argv[1], 'r') as z:
    z.extractall(sys.argv[2])
PYEOF
  fi
  rm -f "$a"
done

for a in "$WORK/out"/*.zip; do
  [ -e "$a" ] || continue
  log "› فك ضغط ZIP: $(basename "$a")"
  unzip -o -q "$a" -d "$WORK/out" || true
  rm -f "$a"
done

# ---------------------------------------------------------------- collect videos
count=0
while IFS= read -r f; do
  name="$(basename "$f")"
  cp -f "$f" "$DEST/$name"
  count=$((count + 1))
  size="$(du -h "$DEST/$name" | cut -f1)"
  log "✔ $name  ($size)"
done < <(find "$WORK/out" -type f | grep -iE "$VID_EXT_RE" | sort)

if [ "$count" = "0" ]; then
  err "لم يتم العثور على أي ملف فيديو بعد فك الضغط. الملفات الموجودة:"
  find "$WORK/out" -type f | head -20
  exit 2
fi

log ""
log "✅ جاهز: $count ملف فيديو في مجلد $DEST"
log "   شغّل الآن التحليل، مثلاً:"
log "   python tools/highlights/highlights.py -i $DEST -o media/out --top 7 --len 20 --min-len 15 --max-len 30 --aspect 9:16"
