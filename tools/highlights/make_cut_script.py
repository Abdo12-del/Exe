#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
make_cut_script.py — يولّد سكربت قصّ جاهز (Windows .bat + Linux/macOS .sh) من report.json.

الاستخدام:
    python tools/highlights/make_cut_script.py --report media/out/report.json --aspect 9:16

الفكرة: قد تضطر لرفع نسخة مضغوطة (بروكسي) من فيديوك لتحليلها هنا. بعد التحليل نحصل على
أزمنة أفضل اللحظات — وهذا السكربت يقصّ **نفس اللحظات من الفيديو الأصلي عالي الجودة**
على جهازك، فتخرج المقاطع بجودتها الكاملة دون رفع الفيديو الأصلي.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import List, Dict


def target_dims(aspect: str, height: int) -> str:
    """فلتر تكبير/قصّ يعتمد على أبعاد الفيديو تلقائياً (بدون معرفة مسبقة بالأبعاد)."""
    if aspect == "keep":
        return f"scale=-2:'min({height},ih)'"
    if aspect == "9:16":
        w = int(round(height * 9 / 16 / 2) * 2)
        return f"scale={w}:{height}:force_original_aspect_ratio=increase,crop={w}:{height}"
    if aspect == "1:1":
        return f"scale={height}:{height}:force_original_aspect_ratio=increase,crop={height}:{height}"
    if aspect == "16:9":
        w = int(round(height * 16 / 9 / 2) * 2)
        return f"scale={w}:{height}:force_original_aspect_ratio=increase,crop={w}:{height}"
    return f"scale=-2:'min({height},ih)'"


def blur_dims(aspect: str, height: int) -> str:
    if aspect == "keep":
        return f"scale=-2:'min({height},ih)'"
    if aspect == "9:16":
        w = int(round(height * 9 / 16 / 2) * 2)
    elif aspect == "1:1":
        w = height
    elif aspect == "16:9":
        w = int(round(height * 16 / 9 / 2) * 2)
    else:
        w = height
    return (f"split=2[b][f];[b]scale={w}:{height}:force_original_aspect_ratio=increase,"
            f"crop={w}:{height},gblur=sigma=28[bg];[f]scale={w}:{height}:force_original_aspect_ratio=decrease[fg];"
            f"[bg][fg]overlay=(W-w)/2:(H-h)/2")


def pad_dims(aspect: str, height: int) -> str:
    if aspect == "keep":
        return f"scale=-2:'min({height},ih)'"
    if aspect == "9:16":
        w = int(round(height * 9 / 16 / 2) * 2)
    elif aspect == "1:1":
        w = height
    elif aspect == "16:9":
        w = int(round(height * 16 / 9 / 2) * 2)
    else:
        w = height
    return (f"scale={w}:{height}:force_original_aspect_ratio=decrease,"
            f"pad={w}:{height}:(ow-iw)/2:(oh-ih)/2:color=black")


def build_vf(aspect: str, height: int, fill: str, fps: float, fade: float) -> str:
    if fill == "blur":
        base = blur_dims(aspect, height)
    elif fill == "pad":
        base = pad_dims(aspect, height)
    else:
        base = target_dims(aspect, height)
    parts = [base]
    if fps and fps > 0:
        parts.append(f"fps={fps}")
    parts.append("setsar=1,format=yuv420p")
    return ",".join(parts)


def ffmpeg_cmd(video: str, start: float, end: float, dst: str, vf: str, fade: float,
               crf: int, preset: str, normalize: bool) -> str:
    dur = max(0.1, end - start)
    parts = ["ffmpeg", "-hide_banner", "-y",
             "-ss", f"{start:.2f}", "-i", f'"{video}"', "-t", f"{dur:.2f}",
             "-vf", f'"{vf}"']
    af = []
    if normalize:
        af.append("loudnorm=I=-16:TP=-1.5:LRA=11")
    if fade and fade > 0 and dur > 2 * fade + 0.2:
        af.append(f"afade=t=in:st=0:d={fade}")
        af.append(f"afade=t=out:st={max(0.0, dur - fade):.2f}:d={fade}")
    if af:
        parts += ["-af", '"' + ",".join(af) + '"']
    parts += ["-c:v", "libx264", "-preset", preset, "-crf", str(crf),
              "-c:a", "aac", "-b:a", "160k", "-ar", "48000",
              "-movflags", "+faststart", f'"{dst}"']
    return " ".join(parts)


def load_clips(report: Path) -> List[Dict]:
    data = json.loads(report.read_text(encoding="utf-8"))
    return data.get("clips", [])


def write_bat(path: Path, clips: List[Dict], vf: str, fade: float, crf: int,
              preset: str, normalize: bool, outdir: str) -> None:
    lines = [
        "@echo off",
        "chcp 65001 >nul",
        "setlocal EnableDelayedExpansion",
        "REM ============================================================",
        "REM  سكربت قصّ تلقائي — ولّدته أداة اختيار أفضل اللحظات",
        "REM  عدّل السطر التالي ليشير إلى الفيديو الأصلي (كامل الجودة) على جهازك",
        "REM ============================================================",
        'set "ORIGINAL=video.mp4"',
        "",
        'if not exist "%ORIGINAL%" (',
        "  echo لم يتم العثور على الفيديو: %ORIGINAL%",
        "  echo عدّل قيمة ORIGINAL أعلى هذا الملف.",
        "  pause",
        "  exit /b 1",
        ")",
        "",
        'set "FF=ffmpeg"',
        "where ffmpeg >nul 2>nul",
        'if errorlevel 1 (',
        '  if exist "%~dp0ffmpeg.exe" ( set "FF=%~dp0ffmpeg.exe" ) else (',
        "    echo ffmpeg غير مثبت. نزّل نسخة وفك ضغطها بجانب هذا الملف باسم ffmpeg.exe",
        "    echo https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip",
        "    pause",
        "    exit /b 1",
        "  )",
        ")",
        "",
        f'if not exist "{outdir}" mkdir "{outdir}"',
        "echo ===========================================",
        f"echo  عدد المقاطع: {len(clips)}",
        "echo ===========================================",
        "",
    ]
    for i, c in enumerate(clips, 1):
        src = c.get("source", "video.mp4")
        dst = f"{outdir}\\clip{i:02d}_{int(c['start'] // 60):02d}m{int(c['start'] % 60):02d}s.mp4"
        lines.append(f"REM  مقطع {i}: من {c['start']:.2f}s إلى {c['end']:.2f}s  (الدرجة {c.get('score', 0):.2f})")
        lines.append(f'echo [{i}/{len(clips)}] قصّ {c["start"]:.2f}s -> {c["end"]:.2f}s')
        cmd = ffmpeg_cmd("%ORIGINAL%", c["start"], c["end"], dst, vf, fade, crf, preset, normalize)
        lines.append("%FF% " + " ".join(cmd.split(" ")[1:]))
        lines.append("")
    lines += [
        "echo.",
        "echo ✅ تم الانتهاء. المقاطع في مجلد: " + outdir,
        "explorer \"" + outdir + "\"",
        "pause",
    ]
    path.write_text("\n".join(lines), encoding="utf-8")


def write_sh(path: Path, clips: List[Dict], vf: str, fade: float, crf: int,
             preset: str, normalize: bool, outdir: str) -> None:
    lines = [
        "#!/usr/bin/env bash",
        "# سكربت قصّ تلقائي — ولّدته أداة اختيار أفضل اللحظات",
        "set -euo pipefail",
        '# عدّل السطر التالي ليشير إلى الفيديو الأصلي على جهازك',
        'ORIGINAL="${1:-video.mp4}"',
        'OUT="' + outdir + '"',
        'FF="${FFMPEG:-ffmpeg}"',
        'command -v "$FF" >/dev/null 2>&1 || { echo "ffmpeg غير موجود. ثبّته: sudo apt install ffmpeg"; exit 1; }',
        'mkdir -p "$OUT"',
        "",
    ]
    for i, c in enumerate(clips, 1):
        dst = f'"$OUT"/clip{i:02d}_{int(c["start"] // 60):02d}m{int(c["start"] % 60):02d}s.mp4'
        lines.append(f"# مقطع {i}: {c['start']:.2f}s -> {c['end']:.2f}s (الدرجة {c.get('score', 0):.2f})")
        lines.append(f'echo "[{i}/{len(clips)}] {c["start"]:.2f}s -> {c["end"]:.2f}s"')
        cmd = ffmpeg_cmd('"$ORIGINAL"', c["start"], c["end"], dst, vf, fade, crf, preset, normalize)
        lines.append('"$FF" ' + " ".join(cmd.split(" ")[1:]))
        lines.append("")
    lines += ['echo "✅ تم. المقاطع في: $OUT"', ""]
    path.write_text("\n".join(lines), encoding="utf-8")


def main() -> int:
    ap = argparse.ArgumentParser(description="توليد سكربت قصّ من تقرير report.json")
    ap.add_argument("--report", type=Path, required=True, help="مسار report.json")
    ap.add_argument("--out", type=Path, default=None, help="مجلد حفظ السكربت (افتراضياً مجلد التقرير)")
    ap.add_argument("--aspect", default="keep", choices=["keep", "9:16", "1:1", "16:9"])
    ap.add_argument("--height", type=int, default=1080)
    ap.add_argument("--fill", default="crop", choices=["crop", "blur", "pad"])
    ap.add_argument("--fps", type=float, default=0.0, help="0 = بدون تحديد")
    ap.add_argument("--fade", type=float, default=0.3)
    ap.add_argument("--crf", type=int, default=20)
    ap.add_argument("--preset", default="veryfast")
    ap.add_argument("--no-normalize", action="store_true")
    ap.add_argument("--outdir", default="clips", help="اسم مجلد المخرجات على جهاز المستخدم")
    args = ap.parse_args()

    clips = load_clips(args.report)
    if not clips:
        print("لا توجد مقاطع في التقرير.")
        return 1
    out = args.out or args.report.parent
    out.mkdir(parents=True, exist_ok=True)
    vf = build_vf(args.aspect, args.height, args.fill, args.fps, 0.0)  # التلاشي يُطبّق صوتياً فقط
    bat = out / "cut_clips.bat"
    sh = out / "cut_clips.sh"
    write_bat(bat, clips, vf, args.fade, args.crf, args.preset, not args.no_normalize, args.outdir)
    write_sh(sh, clips, vf, args.fade, args.crf, args.preset, not args.no_normalize, args.outdir)
    try:
        sh.chmod(0o755)
    except Exception:
        pass
    print(f"✔ {bat}")
    print(f"✔ {sh}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
