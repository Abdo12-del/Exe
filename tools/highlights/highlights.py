#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
highlights.py — أداة مونتاج آلية: تحلّل الفيديو وتستخرج أفضل المقاطع الجاهزة للنشر.

الفكرة:
    1) تحليل كل فيديو: حركة، صوت، حدة/وضوح، وجوه، تغيّر المشاهد، صمت، إضاءة.
    2) ترشيح نوافذ زمنية مرشحة وتقييمها بدرجة مركّبة (تُطبَّع داخل كل فيديو).
    3) اختيار أفضل K مقاطع بدون تكرار (منع التداخل + منع التشابه البصري).
    4) ضبط حدود القصّ على تغيّر مشهد/صمت حتى لا يُقطع الكلام في منتصفه.
    5) التصدير: قصّ + تحويل لأي نسبة أبعاد (9:16 عمودي/1:1/16:9) + تطبيع الصوت + تلاشٍ.

أمثلة:
    # أفضل 5 مقاطع عمودية 9:16 من مجلد، بطول 20 ثانية
    python highlights.py --input media/in --out media/out --top 5 --len 20 --aspect 9:16

    # تحليل سريع بدون تصدير (لمعرفة ما سيختاره)
    python highlights.py --input clip.mp4 --out media/out --top 3 --dry-run

    # فيديو أفقي بطول 60 ثانية + فيديو ملخّص (showreel) يجمع المختارات
    python highlights.py --input media/in --out media/out --top 6 --len 60 --aspect keep --reel
"""

from __future__ import annotations

import argparse
import json
import math
import os
import re
import shutil
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Dict, List, Optional, Sequence, Tuple

import numpy as np
from PIL import Image, ImageDraw

# --------------------------------------------------------------------------------------
# إعدادات عامة
# --------------------------------------------------------------------------------------

VIDEO_EXT = {".mp4", ".mov", ".mkv", ".avi", ".webm", ".m4v", ".mpg", ".mpeg", ".ts", ".flv", ".wmv"}

AR_REASONS = {
    "motion": "حركة عالية",
    "audio": "صوت/كلام نشِط",
    "face": "وجوه واضحة",
    "sharp": "صورة واضحة غير مهزوزة",
    "scene": "إيقاع مونتاج جيد",
    "penalty_silence": "عقوبة: صمت",
    "penalty_blur": "عقوبة: ضبابية/اهتزاز",
    "penalty_dark": "عقوبة: إضاءة سيئة",
    "penalty_static": "عقوبة: لقطة ثابتة مملة",
}


# --------------------------------------------------------------------------------------
# أدوات ffmpeg
# --------------------------------------------------------------------------------------

def find_ffmpeg(explicit: Optional[str] = None) -> str:
    """إيجاد ملف ffmpeg التنفيذي (من المتغيرات/المسار/حزمة imageio-ffmpeg)."""
    candidates = []
    if explicit:
        candidates.append(explicit)
    if os.environ.get("FFMPEG"):
        candidates.append(os.environ["FFMPEG"])
    which = shutil.which("ffmpeg")
    if which:
        candidates.append(which)
    try:
        import imageio_ffmpeg  # type: ignore

        candidates.append(imageio_ffmpeg.get_ffmpeg_exe())
    except Exception:
        pass
    for c in candidates:
        if c and Path(c).exists() and os.access(c, os.X_OK):
            return c
    raise SystemExit(
        "لم يتم العثور على ffmpeg. ثبّت المتطلبات عبر: bash tools/highlights/setup.sh"
    )


FFMPEG: str = ""


def run_ffmpeg(args: Sequence[str], quiet: bool = True) -> subprocess.CompletedProcess:
    return subprocess.run(
        [FFMPEG, "-hide_banner", "-loglevel", "error" if quiet else "info", "-y", *args],
        capture_output=True,
        text=True,
    )


def probe(path: Path) -> dict:
    """قراءة بيانات الفيديو من مخرجات ffmpeg (بدون ffprobe)."""
    p = subprocess.run(
        [FFMPEG, "-hide_banner", "-i", str(path)], capture_output=True, text=True
    )
    err = p.stderr
    info: dict = {"path": str(path), "duration": 0.0, "width": 0, "height": 0, "fps": 0.0,
                  "has_audio": False, "has_video": False, "rotation": 0}

    m = re.search(r"Duration:\s*(\d+):(\d+):(\d+\.?\d*)", err)
    if m:
        info["duration"] = int(m.group(1)) * 3600 + int(m.group(2)) * 60 + float(m.group(3))

    for line in err.splitlines():
        s = line.strip()
        if s.startswith("Stream #") and ": Video:" in s:
            info["has_video"] = True
            m2 = re.search(r",\s*(\d{2,5})x(\d{2,5})", s)
            if m2:
                info["width"], info["height"] = int(m2.group(1)), int(m2.group(2))
            m3 = re.search(r"(\d+(?:\.\d+)?)\s*fps", s)
            if m3:
                info["fps"] = float(m3.group(1))
            m4 = re.search(r"rotate\s*:\s*(-?\d+)", s)
            if m4:
                info["rotation"] = int(m4.group(1))
        elif s.startswith("Stream #") and ": Audio:" in s:
            info["has_audio"] = True
    return info


# --------------------------------------------------------------------------------------
# التحليل
# --------------------------------------------------------------------------------------

@dataclass
class VideoAnalysis:
    path: Path
    duration: float
    width: int
    height: int
    fps: float
    has_audio: bool
    t: np.ndarray = field(default_factory=lambda: np.zeros(0))          # أزمنة العيّنات المرئية
    motion: np.ndarray = field(default_factory=lambda: np.zeros(0))      # مقدار الحركة (0..255)
    sharp: np.ndarray = field(default_factory=lambda: np.zeros(0))       # تباين لابلاس (حدة)
    bright: np.ndarray = field(default_factory=lambda: np.zeros(0))      # متوسط السطوع
    face: np.ndarray = field(default_factory=lambda: np.zeros(0))        # نسبة مساحة الوجه (0..1)
    face_x: np.ndarray = field(default_factory=lambda: np.zeros(0))      # مركز الوجه أفقياً (0..1)
    audio_db: Optional[np.ndarray] = None                                # dBFS لكل نافذة صوتية
    audio_t: Optional[np.ndarray] = None
    scenes: List[float] = field(default_factory=list)
    silence: List[Tuple[float, float]] = field(default_factory=list)
    noise_floor: float = -60.0
    thumbs: Dict[int, np.ndarray] = field(default_factory=dict)          # إطارات مصغّرة للوحات الاتصال


def _scaled_h(w: int, h: int, new_w: int) -> int:
    return max(2, (int(round(new_w * h / max(1, w))) // 2) * 2)


def analyze_video(path: Path, sample_fps: float = 3.0, work_w: int = 192,
                  detect_faces: bool = True, detect_scenes: bool = True,
                  verbose: bool = True) -> VideoAnalysis:
    info = probe(path)
    if not info["has_video"]:
        raise SystemExit(f"الملف لا يحتوي على مسار فيديو: {path}")
    dur = info["duration"]
    if dur <= 0:
        raise SystemExit(f"تعذّر تحديد مدة الفيديو: {path}")

    W, H = info["width"], info["height"]
    # ffmpeg يطبّق دوران الكاميرا تلقائياً عند الفك، لذا نبدّل الأبعاد إن لزم
    if abs(info.get("rotation", 0)) in (90, 270):
        W, H = H, W
    h2 = _scaled_h(W, H, work_w)
    frame_bytes = work_w * h2

    if verbose:
        print(f"   ▸ تحليل الصورة ({work_w}x{h2} @ {sample_fps}fps) …", flush=True)

    cmd = [FFMPEG, "-hide_banner", "-loglevel", "error", "-i", str(path), "-an", "-sn",
           "-vf", f"fps={sample_fps},scale={work_w}:{h2},format=gray",
           "-f", "rawvideo", "-pix_fmt", "gray", "-"]
    proc = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL)

    import cv2  # داخل الدالة لتسريع الإقلاع

    motions, sharps, brights, thumbs = [], [], [], []
    prev: Optional[np.ndarray] = None
    idx = 0
    try:
        while True:
            buf = proc.stdout.read(frame_bytes)
            if not buf or len(buf) < frame_bytes:
                break
            g = np.frombuffer(buf, dtype=np.uint8).reshape(h2, work_w)
            if prev is not None:
                motions.append(float(cv2.absdiff(g, prev).mean()))
                sharps.append(float(cv2.Laplacian(g, cv2.CV_64F).var()))
                brights.append(float(g.mean()))
                if idx % 5 == 0:
                    thumbs.append((idx, cv2.resize(g, (48, 27), interpolation=cv2.INTER_AREA)))
                idx += 1
            prev = g
    finally:
        try:
            proc.stdout.close()
        except Exception:
            pass
        proc.wait()

    n = len(motions)
    if n == 0:
        raise SystemExit(f"تعذّر فك ترميز الفيديو: {path}")
    # أول عيّنة تُقابل زمن العيّنة الثانية تقريباً
    t = (np.arange(n) + 1) / sample_fps
    va = VideoAnalysis(
        path=path, duration=dur, width=W, height=H, fps=info["fps"],
        has_audio=info["has_audio"], t=t,
        motion=np.asarray(motions, dtype=np.float32),
        sharp=np.asarray(sharps, dtype=np.float32),
        bright=np.asarray(brights, dtype=np.float32),
        face=np.zeros(n, dtype=np.float32),
        face_x=np.full(n, 0.5, dtype=np.float32),
    )
    for i, th in thumbs:
        va.thumbs[i] = th

    # ---------------- الوجوه ----------------
    if detect_faces:
        if verbose:
            print("   ▸ كشف الوجوه …", flush=True)
        casc = Path(cv2.data.haarcascades) / "haarcascade_frontalface_default.xml"
        alt = Path(cv2.data.haarcascades) / "haarcascade_frontalface_alt2.xml"
        detector = cv2.CascadeClassifier(str(casc if casc.exists() else alt))
        fw = 320
        fh = _scaled_h(W, H, fw)
        fcmd = [FFMPEG, "-hide_banner", "-loglevel", "error", "-i", str(path), "-an", "-sn",
                "-vf", f"fps=1,scale={fw}:{fh}", "-f", "rawvideo", "-pix_fmt", "rgb24", "-"]
        p2 = subprocess.Popen(fcmd, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL)
        fb = fw * fh * 3
        k = 0
        try:
            while True:
                buf = p2.stdout.read(fb)
                if not buf or len(buf) < fb:
                    break
                img = np.frombuffer(buf, dtype=np.uint8).reshape(fh, fw, 3)
                gray = cv2.cvtColor(img, cv2.COLOR_RGB2GRAY)
                gray = cv2.equalizeHist(gray)
                faces = detector.detectMultiScale(gray, scaleFactor=1.12, minNeighbors=5,
                                                  minSize=(int(fw * 0.05), int(fw * 0.05)))
                if len(faces):
                    x, y, w_, h_ = max(faces, key=lambda f: f[2] * f[3])
                    va.face[:] = 0  # placeholder replaced below (per-time assignment)
                    tt = k + 0.5
                    j0, j1 = int(tt * sample_fps), int((tt + 1.0) * sample_fps)
                    j0 = max(0, min(n - 1, j0))
                    j1 = max(j0 + 1, min(n, j1))
                    va.face[j0:j1] = (w_ * h_) / float(fw * fh)
                    va.face_x[j0:j1] = (x + w_ / 2.0) / float(fw)
                k += 1
        finally:
            try:
                p2.stdout.close()
            except Exception:
                pass
            p2.wait()

    # ---------------- الصوت ----------------
    if va.has_audio:
        if verbose:
            print("   ▸ تحليل الصوت …", flush=True)
        acmd = [FFMPEG, "-hide_banner", "-loglevel", "error", "-i", str(path),
                "-vn", "-sn", "-ac", "1", "-ar", "16000", "-f", "s16le", "-"]
        p3 = subprocess.Popen(acmd, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL)
        win = 4000  # 0.25 ثانية
        dbs: List[float] = []
        try:
            while True:
                buf = p3.stdout.read(win * 2 * 40)
                if not buf:
                    break
                a = np.frombuffer(buf, dtype="<i2").astype(np.float32) / 32768.0
                if len(a) < win:
                    break
                a = a[: (len(a) // win) * win].reshape(-1, win)
                rms = np.sqrt((a * a).mean(axis=1))
                dbs.extend((20.0 * np.log10(np.maximum(rms, 1e-6))).tolist())
        finally:
            try:
                p3.stdout.close()
            except Exception:
                pass
            p3.wait()
        if dbs:
            va.audio_db = np.asarray(dbs, dtype=np.float32)
            va.audio_t = np.arange(len(dbs)) * 0.25
            va.noise_floor = float(np.percentile(va.audio_db, 10))
            thr = max(-45.0, va.noise_floor + 8.0)
            silent = va.audio_db < thr
            # تحويل أقنعة الصمت إلى فترات
            intervals: List[Tuple[float, float]] = []
            start = None
            for i, s in enumerate(silent):
                tt = float(va.audio_t[i])
                if s and start is None:
                    start = tt
                elif not s and start is not None:
                    if tt - start >= 0.5:
                        intervals.append((start, tt))
                    start = None
            if start is not None and len(va.audio_t) and va.audio_t[-1] - start >= 0.5:
                intervals.append((start, float(va.audio_t[-1]) + 0.25))
            va.silence = intervals

    # ---------------- تغيّر المشاهد ----------------
    if detect_scenes:
        if verbose:
            print("   ▸ كشف تغيّرات المشاهد …", flush=True)
        scmd = [FFMPEG, "-hide_banner", "-i", str(path), "-an", "-sn",
                "-vf", "fps=10,select='gt(scene,0.30)',showinfo", "-f", "null", "-"]
        p4 = subprocess.run(scmd, capture_output=True, text=True)
        va.scenes = [float(m) for m in re.findall(r"pts_time:(\d+\.?\d*)", p4.stderr)]
    return va


# --------------------------------------------------------------------------------------
# التقييم
# --------------------------------------------------------------------------------------

@dataclass
class Norm:
    """مُطبِّع: يحوّل قيمة خام إلى رتبة مئوية (0..1) داخل توزيع مرشحي الفيديو."""
    sorted_vals: np.ndarray

    def __call__(self, v: float) -> float:
        if self.sorted_vals.size == 0:
            return 0.0
        return float(np.searchsorted(self.sorted_vals, v) / self.sorted_vals.size)


def _slice_mask(t: np.ndarray, a: float, b: float) -> np.ndarray:
    return (t >= a) & (t < b)


def window_features(va: VideoAnalysis, a: float, b: float) -> Dict[str, float]:
    m = _slice_mask(va.t, a, b)
    if m.sum() < 3:
        return {}
    mot = va.motion[m]
    shp = va.sharp[m]
    bri = va.bright[m]
    fac = va.face[m]

    f: Dict[str, float] = {
        "motion_mean": float(mot.mean()),
        "motion_p90": float(np.percentile(mot, 90)),
        "sharp_med": float(np.median(shp)),
        "dark_frac": float(((bri < 32) | (bri > 244)).mean()),
        "face_frac": float((fac > 0.002).mean()),
        "face_size": float(np.percentile(fac, 90)),
    }
    if va.audio_db is not None and va.audio_t is not None:
        am = (va.audio_t >= a) & (va.audio_t < b)
        if am.sum() >= 2:
            db = va.audio_db[am]
            loud_ref = float(np.percentile(va.audio_db, 95))
            f["audio_mean_db"] = float(db.mean())
            f["speech_frac"] = float((db > loud_ref - 22.0).mean())
            f["silence_frac"] = float((db < max(-45.0, va.noise_floor + 8.0)).mean())
        else:
            f.update({"audio_mean_db": -60.0, "speech_frac": 0.0, "silence_frac": 1.0})
    sd = sum(1 for s in va.scenes if a <= s < b)
    f["scene_rate"] = sd / max(1e-6, (b - a))  # تغيّر مشهد في الثانية
    return f


def score_features(f: Dict[str, float], nrm: Dict[str, Norm], w: Dict[str, float]) -> Tuple[float, Dict[str, float]]:
    if not f:
        return -1e9, {}
    motion = nrm["motion_p90"](f["motion_p90"])
    audio = nrm["audio"](f.get("audio_mean_db", -60.0))
    speech = f.get("speech_frac", 0.0)
    sharp = nrm["sharp_med"](f["sharp_med"])
    face = 0.6 * f["face_frac"] + 0.4 * nrm["face_size"](f["face_size"])
    # إيقاع مونتاج: الأفضل بين 0.05 و0.25 تغيّر مشهد/ثانية
    sr = f["scene_rate"]
    pace = 1.0 - min(1.0, abs(sr - 0.12) / 0.35)
    pace = 0.35 + 0.65 * max(0.0, pace)  # لا نعاقب بشدة اللقطات الطويلة الهادئة

    pos = (w["motion"] * motion + w["audio"] * (0.5 * audio + 0.5 * speech)
           + w["face"] * face + w["sharp"] * sharp + w["scene"] * pace)
    pen = (w["silence"] * f.get("silence_frac", 0.0)
           + w["blur"] * (1.0 - sharp) * 0.5
           + w["dark"] * f["dark_frac"]
           + w["static"] * (1.0 - motion) * (0.0 if f["face_frac"] > 0.4 else 0.6))
    total = pos - pen
    parts = {
        "motion": motion, "audio": audio, "speech": speech, "sharp": sharp,
        "face": face, "pace": pace,
        "penalty_silence": f.get("silence_frac", 0.0),
        "penalty_blur": (1.0 - sharp) * 0.5,
        "penalty_dark": f["dark_frac"],
        "penalty_static": (1.0 - motion) * (0.0 if f["face_frac"] > 0.4 else 0.6),
        "score_raw": total,
    }
    return total, parts


def build_candidates(va: VideoAnalysis, length: float, step: float,
                     min_len: float, max_len: float) -> List[Tuple[float, float]]:
    out: List[Tuple[float, float]] = []
    dur = va.duration
    lens = sorted({round(length, 3), round(min_len, 3), round(max_len, 3)})
    lens = [L for L in lens if L > 1]
    tt = 0.0
    while tt + min(lens) <= dur + 0.01:
        for L in lens:
            if tt + L <= dur + 0.01:
                out.append((round(tt, 2), round(min(tt + L, dur), 2)))
        tt += step
    return out


def make_norms(feats: List[Dict[str, float]]) -> Dict[str, Norm]:
    keys = ["motion_p90", "sharp_med", "face_size", "audio_mean_db"]
    out: Dict[str, Norm] = {}
    for k in keys:
        vals = np.sort(np.asarray([f.get(k, 0.0) for f in feats], dtype=np.float32))
        out[{"motion_p90": "motion_p90", "sharp_med": "sharp_med",
             "face_size": "face_size", "audio_mean_db": "audio"}[k]] = Norm(vals)
    return out


def signature(va: VideoAnalysis, a: float, b: float) -> Optional[np.ndarray]:
    """بصمة بصرية مخفّفة لتمثيل محتوى النافذة (لمقارنة التشابه)."""
    idxs = [i * 5 for i in va.thumbs.keys()]
    if not idxs:
        return None
    arr = np.asarray([va.thumbs[i] for i in sorted(va.thumbs.keys())], dtype=np.float32)
    times = np.asarray(sorted(va.thumbs.keys()), dtype=np.float32) / max(1e-6, (len(va.t) / va.duration))
    m = (times >= a) & (times < b)
    if m.sum() == 0:
        return None
    sel = arr[m]
    sig = sel.mean(axis=0).reshape(-1)
    sig = sig - sig.mean()
    n = float(np.linalg.norm(sig))
    return sig / n if n > 1e-6 else None


@dataclass
class Clip:
    video: Path
    start: float
    end: float
    score: float
    parts: Dict[str, float]
    rank: int = 0
    out: Optional[Path] = None
    thumb: Optional[Path] = None


def pick_clips(va: VideoAnalysis, top: int, length: float, step: float,
               min_len: float, max_len: float, weights: Dict[str, float],
               min_gap: float, verbose: bool = True) -> List[Clip]:
    cands = build_candidates(va, length, step, min_len, max_len)
    if verbose:
        print(f"   ▸ تقييم {len(cands)} نافذة مرشحة …", flush=True)
    feats = [window_features(va, a, b) for a, b in cands]
    feats = [f for f in feats if f]
    norms = make_norms(feats)
    scored: List[Tuple[float, int, Dict[str, float]]] = []
    for i, f in enumerate(feats):
        s, parts = score_features(f, norms, weights)
        scored.append((s, i, parts))
    scored.sort(key=lambda x: -x[0])

    selected: List[Clip] = []
    sigs: List[np.ndarray] = []
    for s, i, parts in scored:
        if len(selected) >= top:
            break
        a, b = cands[i]
        # منع التداخل والقرب الشديد
        if any(not (b + min_gap <= c.start or a >= c.end + min_gap) for c in selected):
            continue
        # منع التكرار البصري
        sig = signature(va, a, b)
        if sig is not None and sigs:
            sims = [float(np.dot(sig, s2)) for s2 in sigs]
            if sims and max(sims) > 0.94:
                continue
        # ضبط الحدود داخل النافذة
        a2, b2, s2, parts2 = refine_window(va, a, b, min_len, max_len, norms, weights)
        if sig is not None:
            sigs.append(sig)
        selected.append(Clip(va.path, a2, b2, s2, parts2))
    # ترتيب زمني للإخراج
    selected.sort(key=lambda c: c.start)
    for i, c in enumerate(selected, 1):
        c.rank = i
    return selected


def refine_window(va: VideoAnalysis, a: float, b: float, min_len: float, max_len: float,
                  norms: Dict[str, Norm], weights: Dict[str, float]) -> Tuple[float, float, float, Dict[str, float]]:
    """تحريك/تمديد حدود النافذة قليلاً للحصول على أفضل قصّة، ثم المحاذاة على مشهد/صمت."""
    best = (a, b, None, None)
    base_len = b - a
    for dlen in (0.0, -0.15 * base_len, 0.15 * base_len):
        L = float(np.clip(base_len + dlen, min_len, max_len))
        for off in np.arange(-4.0, 4.01, 0.5):
            a2 = a + float(off)
            b2 = a2 + L
            if a2 < 0 or b2 > va.duration:
                continue
            f = window_features(va, a2, b2)
            if not f:
                continue
            s, parts = score_features(f, norms, weights)
            if best[2] is None or s > best[2]:
                best = (a2, b2, s, parts)
    a2, b2, s, parts = best
    if s is None:
        f = window_features(va, a, b)
        s, parts = score_features(f, norms, weights)
        a2, b2 = a, b

    # محاذاة البداية/النهاية على أقرب تغيّر مشهد أو صمت (±1.5 ثانية)
    a2 = snap_start(va, a2)
    b2 = snap_end(va, b2)
    if b2 - a2 < min_len:
        b2 = min(va.duration, a2 + min_len)
    return round(max(0.0, a2), 2), round(min(va.duration, b2), 2), float(s), parts


def snap_start(va: VideoAnalysis, t: float, tol: float = 1.5) -> float:
    cand = [s for s in va.scenes if abs(s - t) <= tol]
    if cand:
        return min(cand, key=lambda s: abs(s - t))
    for s0, s1 in va.silence:
        if abs(s1 - t) <= tol:
            return s1 - 0.05
        if s0 <= t <= s1:
            return s1 - 0.05
    return t


def snap_end(va: VideoAnalysis, t: float, tol: float = 1.5) -> float:
    cand = [s for s in va.scenes if abs(s - t) <= tol]
    if cand:
        return min(cand, key=lambda s: abs(s - t))
    for s0, s1 in va.silence:
        if abs(s0 - t) <= tol:
            return s0 + 0.05
        if s0 <= t <= s1:
            return s0 + 0.05
    return t


# --------------------------------------------------------------------------------------
# التصدير
# --------------------------------------------------------------------------------------

def aspect_target(aspect: str, height: int) -> Tuple[int, int]:
    if aspect in ("keep", "source", ""):
        return 0, height
    if aspect == "9:16":
        return int(round(height * 9 / 16 / 2) * 2), height
    if aspect == "1:1":
        return height, height
    if aspect == "16:9":
        return int(round(height * 16 / 9 / 2) * 2), height
    raise SystemExit(f"نسبة أبعاد غير مدعومة: {aspect}")


def build_vf(va: VideoAnalysis, aspect: str, height: int, fill: str,
             face_x: float, fps_cap: float, fade: float, subs: Optional[Path],
             dur: float) -> Tuple[List[str], int, int]:
    src_w, src_h = va.width, va.height
    ow, oh = aspect_target(aspect, height)
    chains: List[str] = []

    if ow and oh:
        src_ar, tgt_ar = src_w / max(1, src_h), ow / oh
        if fill == "crop" and src_ar > tgt_ar + 1e-3:
            # المصدر أعرض من المطلوب ⇒ نكبّر ثم نقصّ جانبياً (مع تمركز على الوجه)
            tmp_w = max(ow + 2, (int(round(oh * src_w / max(1, src_h))) // 2) * 2)
            x = int(np.clip(round(face_x * tmp_w - ow / 2.0), 0, max(0, tmp_w - ow)))
            chains.append(f"scale={tmp_w}:{oh}")
            chains.append(f"crop={ow}:{oh}:{x}:0")
        elif fill == "crop" and src_ar < tgt_ar - 1e-3:
            # المصدر أضيق ⇒ نقصّ عمودياً (نحافظ على ثلث الإطار العلوي حيث الوجوه عادةً)
            tmp_h = max(oh + 2, (int(round(ow * src_h / max(1, src_w))) // 2) * 2)
            y = int(np.clip(round(tmp_h * 0.35 - oh / 2.0), 0, max(0, tmp_h - oh)))
            chains.append(f"scale={ow}:{tmp_h}")
            chains.append(f"crop={ow}:{oh}:0:{y}")
        elif fill == "blur":
            chains.append(
                f"split=2[b][f];[b]scale={ow}:{oh}:force_original_aspect_ratio=increase,"
                f"crop={ow}:{oh},gblur=sigma=28[bg];[f]scale={ow}:{oh}:force_original_aspect_ratio=decrease[fg];"
                f"[bg][fg]overlay=(W-w)/2:(H-h)/2"
            )
        else:  # pad (أشرطة سوداء)
            chains.append(f"scale={ow}:{oh}:force_original_aspect_ratio=decrease")
            chains.append(f"pad={ow}:{oh}:(ow-iw)/2:(oh-ih)/2:color=black")
        out_w, out_h = ow, oh
    else:
        h = min(height, src_h) if src_h else height
        h = h if h % 2 == 0 else max(2, h - 1)
        chains.append(f"scale=-2:{h}")
        out_w, out_h = -2, h

    if fps_cap and va.fps and va.fps > fps_cap + 0.1:
        chains.append(f"fps={fps_cap}")
    if fade and fade > 0 and dur > 2 * fade + 0.2:
        chains.append(f"fade=t=in:st=0:d={fade}")
        chains.append(f"fade=t=out:st={max(0.0, dur - fade):.3f}:d={fade}")
    if subs:
        esc = str(subs).replace("\\", "/").replace(":", r"\:").replace("'", r"\'")
        chains.append(f"subtitles=filename='{esc}'")
    chains.append("setsar=1")
    chains.append("format=yuv420p")
    return chains, out_w, out_h


def face_center_in_clip(va: VideoAnalysis, a: float, b: float) -> float:
    m = _slice_mask(va.t, a, b)
    if m.sum() == 0 or not np.any(va.face[m] > 0.002):
        return 0.5
    xs = va.face_x[m][va.face[m] > 0.002]
    return float(np.median(xs))


def best_thumb_time(va: VideoAnalysis, a: float, b: float) -> float:
    m = _slice_mask(va.t, a, b)
    if m.sum() == 0:
        return a
    idxs = np.where(m)[0]
    sc = va.motion[m] + va.face[m] * 400.0
    return float(va.t[idxs[int(np.argmax(sc))]])


def export_clip(va: VideoAnalysis, clip: Clip, out_dir: Path, aspect: str, height: int,
                fill: str, fps_cap: float, fade: float, crf: int, preset: str,
                normalize: bool, subs: Optional[Path], prefix: str) -> Path:
    out_dir.mkdir(parents=True, exist_ok=True)
    dur = clip.end - clip.start
    name = f"{prefix}{clip.rank:02d}_{int(clip.start // 60):02d}m{int(clip.start % 60):02d}s-{dur:.0f}s.mp4"
    dst = out_dir / name

    fx = face_center_in_clip(va, clip.start, clip.end)
    chains, ow, oh = build_vf(va, aspect, height, fill, fx, fps_cap, fade, subs, dur)
    vf = ",".join(chains)

    cmd = [FFMPEG, "-hide_banner", "-loglevel", "error", "-y",
           "-ss", f"{clip.start:.3f}", "-i", str(va.path), "-t", f"{dur:.3f}",
           "-vf", vf]
    af: List[str] = []
    if va.has_audio:
        if normalize:
            af.append("loudnorm=I=-16:TP=-1.5:LRA=11")
        if fade and fade > 0:
            af.append(f"afade=t=in:st=0:d={fade}")
            af.append(f"afade=t=out:st={max(0.0, dur - fade):.3f}:d={fade}")
        if af:
            cmd += ["-af", ",".join(af)]
        cmd += ["-c:a", "aac", "-b:a", "160k", "-ar", "48000", "-ac", "2"]
    else:
        cmd += ["-an"]
    cmd += ["-c:v", "libx264", "-preset", preset, "-crf", str(crf),
            "-movflags", "+faststart", str(dst)]
    r = subprocess.run(cmd, capture_output=True, text=True)
    if r.returncode != 0 or not dst.exists():
        raise SystemExit(f"فشل تصدير المقطع {dst.name}:\n{r.stderr[-1500:]}")
    clip.out = dst
    return dst


def make_thumb(va: VideoAnalysis, clip: Clip, out_dir: Path) -> Optional[Path]:
    t = best_thumb_time(va, clip.start, clip.end)
    dst = out_dir / f"thumb_{clip.rank:02d}.jpg"
    cmd = [FFMPEG, "-hide_banner", "-loglevel", "error", "-y", "-ss", f"{t:.3f}",
           "-i", str(va.path), "-frames:v", "1", "-vf", "scale=480:-2", str(dst)]
    r = subprocess.run(cmd, capture_output=True, text=True)
    if r.returncode == 0 and dst.exists():
        clip.thumb = dst
        return dst
    return None


def make_contact_sheet(clips: List[Clip], out_dir: Path, cols: int = 3) -> Optional[Path]:
    imgs = [Image.open(c.thumb).convert("RGB") for c in clips if c.thumb and c.thumb.exists()]
    if not imgs:
        return None
    tw, th = 320, 180
    imgs = [im.resize((tw, th)) for im in imgs]
    rows = math.ceil(len(imgs) / cols)
    pad, label = 12, 26
    sheet = Image.new("RGB", (cols * tw + (cols + 1) * pad, rows * (th + label) + (rows + 1) * pad), (18, 18, 20))
    d = ImageDraw.Draw(sheet)
    for i, im in enumerate(imgs):
        r, c = divmod(i, cols)
        x = pad + c * (tw + pad)
        y = pad + r * (th + label + pad)
        sheet.paste(im, (x, y))
        c0 = clips[i]
        d.text((x + 4, y + th + 6),
               f"#{c0.rank}  {c0.start:.1f}s-{c0.end:.1f}s  score {c0.score:.2f}  {Path(c0.video).name[:18]}",
               fill=(235, 235, 240))
    dst = out_dir / "contact_sheet.jpg"
    sheet.save(dst, quality=88)
    return dst


def make_reel(clips: List[Clip], out_dir: Path) -> Optional[Path]:
    clips = [c for c in clips if c.out and c.out.exists()]
    if len(clips) < 2:
        return None
    lst = out_dir / "_reel_list.txt"
    with open(lst, "w", encoding="utf-8") as f:
        for c in clips:
            f.write(f"file '{c.out.resolve()}'\n")
    dst = out_dir / "reel_all_clips.mp4"
    cmd = [FFMPEG, "-hide_banner", "-loglevel", "error", "-y", "-f", "concat", "-safe", "0",
           "-i", str(lst), "-c", "copy", "-movflags", "+faststart", str(dst)]
    r = subprocess.run(cmd, capture_output=True, text=True)
    if r.returncode == 0 and dst.exists():
        try:
            lst.unlink()
        except Exception:
            pass
        return dst
    print("   ! تعذّر دمج المقاطع بنسخ مباشر، يتم إعادة الترميز…")
    cmd = [FFMPEG, "-hide_banner", "-loglevel", "error", "-y", "-f", "concat", "-safe", "0",
           "-i", str(lst), "-c:v", "libx264", "-preset", "veryfast", "-crf", "20",
           "-c:a", "aac", "-b:a", "160k", "-movflags", "+faststart", str(dst)]
    r = subprocess.run(cmd, capture_output=True, text=True)
    try:
        lst.unlink()
    except Exception:
        pass
    return dst if dst.exists() else None


# --------------------------------------------------------------------------------------
# التقارير
# --------------------------------------------------------------------------------------

def emit_cut_scripts(js: Path, args: argparse.Namespace) -> None:
    """يولّد سكربت قصّ (Windows/Linux) ليقصّ المستخدم نفس اللحظات من الفيديو الأصلي كامل الجودة."""
    try:
        sys.path.insert(0, str(Path(__file__).resolve().parent))
        import make_cut_script as mcs  # type: ignore
        clips = mcs.load_clips(js)
        if not clips:
            return
        vf = mcs.build_vf(args.aspect, args.height, args.fill, args.fps, 0.0)
        mcs.write_bat(js.parent / "cut_clips.bat", clips, vf, args.fade, args.crf,
                      args.preset, not args.no_normalize, "clips")
        mcs.write_sh(js.parent / "cut_clips.sh", clips, vf, args.fade, args.crf,
                     args.preset, not args.no_normalize, "clips")
        try:
            (js.parent / "cut_clips.sh").chmod(0o755)
        except Exception:
            pass
        print("   ✔ cut_clips.bat + cut_clips.sh  (لقصّ نفس اللحظات من الفيديو الأصلي بجودته الكاملة)")
    except Exception as e:  # لا نُفشل التشغيل بسبب السكربت الإضافي
        print(f"   ! تعذّر توليد سكربت القصّ: {e}")


def fmt_ts(x: float) -> str:
    return f"{int(x // 60):02d}:{x % 60:05.2f}"


def write_reports(clips: List[Clip], analyses: Dict[str, VideoAnalysis], out_dir: Path,
                  args: argparse.Namespace) -> Tuple[Path, Path]:
    out_dir.mkdir(parents=True, exist_ok=True)
    md = out_dir / "report.md"
    js = out_dir / "report.json"

    rows = []
    for c in clips:
        va = analyses[str(c.video)]
        rows.append({
            "rank": c.rank,
            "source": str(c.video),
            "start": round(c.start, 2),
            "end": round(c.end, 2),
            "duration": round(c.end - c.start, 2),
            "score": round(c.score, 4),
            "parts": {k: round(v, 4) for k, v in c.parts.items()},
            "output": str(c.out) if c.out else None,
            "thumb": str(c.thumb) if c.thumb else None,
        })

    with open(js, "w", encoding="utf-8") as f:
        json.dump({"settings": {k: (str(v) if isinstance(v, Path) else v)
                                for k, v in vars(args).items()}, "clips": rows},
                  f, ensure_ascii=False, indent=2)

    lines = ["# تقرير أفضل المقاطع", "",
             f"- عدد المقاطع المختارة: **{len(clips)}**",
             f"- الطول المستهدف: {args.len} ثانية ({args.min_len}–{args.max_len})",
             f"- نسبة الأبعاد: {args.aspect} | الارتفاع: {args.height} | التعبئة: {args.fill}",
             f"- الأوزان: {json.dumps(args.weights, ensure_ascii=False)}", ""]
    lines.append("| # | المصدر | البداية | النهاية | المدة | الدرجة | أسباب الاختيار |")
    lines.append("|---|--------|---------|---------|-------|--------|----------------|")
    for c in clips:
        reasons = []
        p = c.parts
        if p.get("motion", 0) > 0.6:
            reasons.append(AR_REASONS["motion"])
        if p.get("speech", 0) > 0.55 or p.get("audio", 0) > 0.6:
            reasons.append(AR_REASONS["audio"])
        if p.get("face", 0) > 0.25:
            reasons.append(AR_REASONS["face"])
        if p.get("sharp", 0) > 0.6:
            reasons.append(AR_REASONS["sharp"])
        if p.get("pace", 0) > 0.7:
            reasons.append(AR_REASONS["scene"])
        for k, lab in (("penalty_silence", AR_REASONS["penalty_silence"]),
                       ("penalty_blur", AR_REASONS["penalty_blur"]),
                       ("penalty_dark", AR_REASONS["penalty_dark"]),
                       ("penalty_static", AR_REASONS["penalty_static"])):
            if p.get(k, 0) > 0.35:
                reasons.append(lab)
        lines.append(f"| {c.rank} | {Path(c.video).name} | {fmt_ts(c.start)} | {fmt_ts(c.end)} | "
                     f"{c.end - c.start:.1f}s | {c.score:.3f} | {'، '.join(reasons) or '—'} |")
    lines += ["", "## الملفات الناتجة", ""]
    for c in clips:
        if c.out:
            lines.append(f"- `{c.out.name}` — من {fmt_ts(c.start)} إلى {fmt_ts(c.end)}")
    md.write_text("\n".join(lines), encoding="utf-8")
    return md, js


# --------------------------------------------------------------------------------------
# الواجهة
# --------------------------------------------------------------------------------------

def collect_inputs(inp: Path) -> List[Path]:
    if inp.is_file():
        return [inp]
    files = [p for p in sorted(inp.iterdir()) if p.suffix.lower() in VIDEO_EXT and not p.name.startswith(".")]
    return files


def default_weights() -> Dict[str, float]:
    return {"motion": 1.0, "audio": 0.9, "face": 0.7, "sharp": 0.6, "scene": 0.4,
            "silence": 1.2, "blur": 0.8, "dark": 1.0, "static": 0.8}


def parse_weights(s: Optional[str]) -> Dict[str, float]:
    w = default_weights()
    if not s:
        return w
    for part in s.split(","):
        if "=" in part:
            k, v = part.split("=", 1)
            k = k.strip()
            if k in w:
                w[k] = float(v)
    return w


def main(argv: Optional[List[str]] = None) -> int:
    global FFMPEG
    ap = argparse.ArgumentParser(
        description="استخراج أفضل المقاطع من فيديوهاتك تلقائياً وجاهزة للنشر",
        formatter_class=argparse.ArgumentDefaultsHelpFormatter,
    )
    ap.add_argument("--input", "-i", type=Path, default=Path("media/in"), help="ملف فيديو أو مجلد يحتوي فيديوهات")
    ap.add_argument("--out", "-o", type=Path, default=Path("media/out"), help="مجلد الإخراج")
    ap.add_argument("--top", type=int, default=5, help="عدد المقاطع المختارة (لكل فيديو)")
    ap.add_argument("--len", type=float, default=20.0, help="الطول المستهدف للمقطع بالثواني")
    ap.add_argument("--min-len", type=float, default=10.0, help="أدنى طول مسموح")
    ap.add_argument("--max-len", type=float, default=35.0, help="أقصى طول مسموح")
    ap.add_argument("--step", type=float, default=1.0, help="خطوة انزلاق نافذة البحث بالثواني")
    ap.add_argument("--min-gap", type=float, default=4.0, help="أدنى فاصل زمني بين مقطعين مختارين")
    ap.add_argument("--aspect", default="keep", choices=["keep", "9:16", "1:1", "16:9"], help="نسبة الأبعاد")
    ap.add_argument("--height", type=int, default=1920, help="ارتفاع الإخراج (يُستخدم مع 9:16/1:1/16:9)")
    ap.add_argument("--fill", default="crop", choices=["crop", "blur", "pad"], help="طريقة تعبئة الإطار")
    ap.add_argument("--fps", type=float, default=30.0, help="حد أقصى للإطارات في الثانية (0 = بدون تحديد)")
    ap.add_argument("--fade", type=float, default=0.3, help="مدة التلاشي في البداية والنهاية")
    ap.add_argument("--crf", type=int, default=20, help="جودة الترميز (أقل = أفضل)")
    ap.add_argument("--preset", default="veryfast", help="سرعة ترميز x264")
    ap.add_argument("--weights", type=str, default=None,
                    help="أوزان مخصّصة مثل: motion=1.2,face=1.0,silence=2.0")
    ap.add_argument("--subs", type=Path, default=None, help="ملف ترجمة .srt يُدمج في الفيديو")
    ap.add_argument("--reel", action="store_true", help="دمج المختارات في فيديو ملخّص واحد")
    ap.add_argument("--dry-run", action="store_true", help="تحليل واختيار فقط بدون تصدير")
    ap.add_argument("--cut-script", action="store_true",
                    help="توليد cut_clips.bat/.sh لقصّ نفس اللحظات من الفيديو الأصلي على جهازك (تلقائي مع --dry-run)")
    ap.add_argument("--no-faces", action="store_true", help="تعطيل كشف الوجوه (أسرع)")
    ap.add_argument("--no-scenes", action="store_true", help="تعطيل كشف تغيّر المشاهد (أسرع)")
    ap.add_argument("--sample-fps", type=float, default=3.0, help="معدّل أخذ العينات أثناء التحليل")
    ap.add_argument("--no-normalize", action="store_true", help="عدم تطبيع مستوى الصوت")
    ap.add_argument("--ffmpeg", type=str, default=None, help="مسار ffmpeg يدوياً")
    ap.add_argument("--quiet", action="store_true", help="إخراج مختصر")
    args = ap.parse_args(argv)

    FFMPEG = find_ffmpeg(args.ffmpeg)
    args.weights = parse_weights(args.weights)

    inputs = collect_inputs(args.input)
    if not inputs:
        print(f"لا توجد فيديوهات في: {args.input}")
        print("الممتدات المدعومة:", ", ".join(sorted(VIDEO_EXT)))
        return 1

    out_dir: Path = args.out
    (out_dir / "thumbs").mkdir(parents=True, exist_ok=True)

    verbose = not args.quiet
    analyses: Dict[str, VideoAnalysis] = {}
    all_clips: List[Clip] = []

    for p in inputs:
        print(f"\n🎞  {p.name}", flush=True)
        va = analyze_video(p, sample_fps=args.sample_fps, detect_faces=not args.no_faces,
                           detect_scenes=not args.no_scenes, verbose=verbose)
        analyses[str(p)] = va
        if verbose:
            print(f"   ▸ المدة {fmt_ts(va.duration)} | {va.width}x{va.height} | "
                  f"صوت: {'نعم' if va.has_audio else 'لا'} | مشاهد: {len(va.scenes)}", flush=True)
        clips = pick_clips(va, args.top, args.len, args.step, args.min_len, args.max_len,
                           args.weights, args.min_gap, verbose=verbose)
        all_clips.extend(clips)
        for c in clips:
            print(f"   ✔ #{c.rank}  {fmt_ts(c.start)} → {fmt_ts(c.end)}  ({c.end - c.start:.1f}s)  "
                  f"درجة {c.score:.3f}", flush=True)

    if not all_clips:
        print("\nلم يتم اختيار أي مقطع.")
        return 1

    if args.dry_run:
        md, js = write_reports(all_clips, analyses, out_dir, args)
        print(f"\n(وضع التحليل فقط) تم كتابة: {md.name}, {js.name}", flush=True)
        emit_cut_scripts(js, args)
        return 0

    print("\n⚙  جارٍ التصدير …", flush=True)
    for c in all_clips:
        va = analyses[str(c.video)]
        prefix = f"{Path(c.video).stem[:18]}_clip"
        dst = export_clip(va, c, out_dir, args.aspect, args.height, args.fill, args.fps,
                          args.fade, args.crf, args.preset, not args.no_normalize,
                          args.subs, prefix)
        make_thumb(va, c, out_dir / "thumbs")
        size = dst.stat().st_size / (1024 * 1024)
        print(f"   ✔ {dst.name}  ({size:.1f} م.ب)", flush=True)

    sheet = make_contact_sheet(all_clips, out_dir / "thumbs")
    if sheet:
        print(f"   ✔ {sheet.relative_to(out_dir)}", flush=True)
    if args.reel:
        reel = make_reel(all_clips, out_dir)
        if reel:
            print(f"   ✔ {reel.name}  ({(reel.stat().st_size / (1024 * 1024)):.1f} م.ب)", flush=True)

    md, js = write_reports(all_clips, analyses, out_dir, args)
    if args.cut_script:
        emit_cut_scripts(js, args)
    print(f"\n✅ تم. المخرجات في: {out_dir}")
    print(f"   تقرير: {md.name} | بيانات: {js.name}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
