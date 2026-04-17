# -*- coding: utf-8 -*-
"""
HANAMI STACK - 完全自動動画生成スクリプト
Playwright でブラウザを操作しながらcanvasをキャプチャ -> moviepy + VOICEVOX で動画合成
"""
import json
import time
import base64
import subprocess
import sys
import requests
from pathlib import Path
from io import BytesIO
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from moviepy import AudioFileClip, ImageClip, VideoClip, concatenate_videoclips

try:
    from playwright.sync_api import sync_playwright
    DRIVER = 'playwright'
except ImportError:
    DRIVER = None

BASE = Path(__file__).parent
TMP  = BASE / "_video_tmp"
TMP.mkdir(exist_ok=True)

SPEAKER_ID = 8
VW, VH = 1080, 1920
GAME_URL = "http://localhost:5173"
FFMPEG_PATH = r"C:\Users\talashi\AppData\Local\Programs\Python\Python312\Lib\site-packages\imageio_ffmpeg\binaries\ffmpeg-win-x86_64-v7.1.exe"

NARRATIONS = [
    "HANAMI STACK！春のお花見弁当箱に、おにぎりや唐揚げを積み重ねるゲームだよ！",
    "本物の物理シミュレーションで、積んだアイテムが次のアイテムに押されてリアルに動くのがポイント！",
    "どこまで高く積めるかな？概要欄から無料で遊べるよ！",
]

def load_font(size):
    for p in [
        r"C:\Windows\Fonts\YuGothB.ttc",
        r"C:\Windows\Fonts\YuGothM.ttc",
        r"C:\Windows\Fonts\meiryo.ttc",
    ]:
        if Path(p).exists():
            try: return ImageFont.truetype(p, size)
            except: pass
    return ImageFont.load_default()

F_SUB = load_font(52)

def tts(text, path, speed=1.1):
    if Path(path).exists():
        print(f"  [cache] {Path(path).name}")
        return
    r1 = requests.post("http://localhost:50021/audio_query",
                       params={"text": text, "speaker": SPEAKER_ID})
    r1.raise_for_status()
    query = r1.json()
    query["speedScale"] = speed
    r2 = requests.post("http://localhost:50021/synthesis",
                       params={"speaker": SPEAKER_ID},
                       headers={"Content-Type": "application/json"},
                       data=json.dumps(query))
    r2.raise_for_status()
    Path(path).write_bytes(r2.content)
    print(f"  [tts] {Path(path).name}")

def wrap_text(text, font, max_w):
    dummy = ImageDraw.Draw(Image.new("RGB", (1, 1)))
    lines, line = [], ""
    for ch in text:
        test = line + ch
        bb = dummy.textbbox((0, 0), test, font=font)
        if bb[2] - bb[0] > max_w and line:
            lines.append(line)
            line = ch
        else:
            line = test
    if line:
        lines.append(line)
    return lines

def add_subtitle(frame_img, text):
    img = frame_img.convert("RGBA")
    ov  = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d   = ImageDraw.Draw(ov)
    lines = wrap_text(text, F_SUB, VW - 80)[-3:]
    line_h = 64
    total_h = len(lines) * line_h + 40
    bar_y = VH - total_h - 60
    d.rounded_rectangle([20, bar_y, VW - 20, bar_y + total_h],
                        radius=16, fill=(0, 0, 0, 180))
    ty = bar_y + 20
    for line in lines:
        bb = d.textbbox((0, 0), line, font=F_SUB)
        lw = bb[2] - bb[0]
        tx = (VW - lw) // 2
        for dx, dy in [(-2,0),(2,0),(0,-2),(0,2)]:
            d.text((tx+dx, ty+dy), line, font=F_SUB, fill=(0,0,0,255))
        d.text((tx, ty), line, font=F_SUB, fill=(255,255,255,255))
        ty += line_h
    return Image.alpha_composite(img, ov).convert("RGB")

RECORDER_JS = """
window._recorder = {
    frames: [], recording: false, intervalId: null, _prev: null,
    start(fps) {
        this.frames = []; this.recording = true; this._prev = null;
        const c = document.getElementById('c');
        this.intervalId = setInterval(() => {
            if (!this.recording) return;
            const d = c.toDataURL('image/jpeg', 0.88);
            if (d !== this._prev) { this.frames.push(d); this._prev = d; }
        }, 1000 / (fps || 40));
    },
    stop() {
        this.recording = false;
        if (this.intervalId) { clearInterval(this.intervalId); this.intervalId = null; }
        return this.frames.length;
    },
    flush() { const f = this.frames; this.frames = []; this._prev = null; return f; }
};
'recorder_ready'
"""

def decode_frames(raw_list, target_w, target_h):
    result = []
    for data in raw_list:
        img = Image.open(BytesIO(base64.b64decode(data.split(',')[1])))
        img = img.resize((target_w, target_h), Image.LANCZOS)
        result.append(img)
    return result

def fire_click(page, cx, cy, canvas_w=360, canvas_h=640, vp_w=390, vp_h=844):
    """canvas座標でマウスイベントを発火"""
    sx = cx * vp_w / canvas_w
    sy = cy * vp_h / canvas_h
    page.mouse.move(sx, sy)
    page.mouse.down()
    time.sleep(0.12)
    page.mouse.up()

def capture_frames():
    scenes = {}
    FPS = 40
    DROP_XS = [180, 165, 195, 175, 185, 160, 200]  # 7回分のドロップX座標

    with sync_playwright() as pw:
        browser = pw.chromium.launch(
            headless=True,
            args=[
                '--disable-background-timer-throttling',
                '--disable-renderer-backgrounding',
                '--disable-backgrounding-occluded-windows',
                '--disable-background-media-suspend',
                '--force-device-scale-factor=1',
            ]
        )
        page = browser.new_page(viewport={"width": 390, "height": 844})
        page.goto(GAME_URL + '?record=1')
        time.sleep(2.0)
        page.evaluate(RECORDER_JS)

        # ---- Scene 1: タイトル画面 (3秒) ----
        print(f"  Scene 1: title (3s @ {FPS}fps)...")
        page.evaluate(f"window._recorder.start({FPS})")
        time.sleep(3.0)
        page.evaluate("window._recorder.stop()")
        raw1 = page.evaluate("window._recorder.flush()")
        print(f"    {len(raw1)} frames")
        scenes[0] = decode_frames(raw1, VW, VH)

        # ---- ゲーム開始 ----
        fire_click(page, 180, 430)
        time.sleep(0.5)

        # ---- Scene 2: ゲームプレイ (15秒: 7個積む) ----
        print(f"  Scene 2: gameplay (15s @ {FPS}fps)...")
        page.evaluate(f"window._recorder.start({FPS})")

        for i, x in enumerate(DROP_XS):
            fire_click(page, x, 300)
            time.sleep(4.5)  # 着地・安定待ち

        # 余った録画時間を埋める（スタックの揺れを見せる）
        time.sleep(1.5)
        page.evaluate("window._recorder.stop()")
        raw2 = page.evaluate("window._recorder.flush()")
        print(f"    {len(raw2)} frames")
        scenes[1] = decode_frames(raw2, VW, VH)

        # ---- Scene 3: ハイスコア静止画 (4秒) ----
        print(f"  Scene 3: result snapshot (4s)...")
        raw3_single = page.evaluate("document.getElementById('c').toDataURL('image/jpeg', 0.88)")
        frame3 = decode_frames([raw3_single], VW, VH)[0]
        n3 = int(FPS * 4)
        scenes[2] = [frame3] * n3
        print(f"    {n3} frames (static)")

        browser.close()

    return scenes

def frames_to_clip(frames, fps=40):
    arr = [np.array(f) for f in frames]
    def make_frame(t):
        idx = min(int(t * fps), len(arr) - 1)
        return arr[idx]
    dur = len(frames) / fps
    return VideoClip(make_frame, duration=dur).with_fps(fps)

def build_video():
    print("=== HANAMI STACK Video Generator ===")

    try:
        requests.get("http://localhost:50021/version", timeout=2)
        print("[OK] VOICEVOX running")
    except:
        print("[ERROR] VOICEVOX not running.")
        return

    print("\n[1/3] Generating narration...")
    audio_paths = []
    for i, text in enumerate(NARRATIONS):
        p = TMP / f"narr_{i:02d}.wav"
        tts(text, p)
        audio_paths.append(p)

    print("\n[2/3] Capturing game footage...")
    if DRIVER != 'playwright':
        print("[ERROR] Playwright not installed.")
        return
    scenes = capture_frames()

    print("\n[3/3] Compositing video...")
    clips = []
    fps = 40
    for i, (scene_frames, narr_path, narr_text) in enumerate(zip(
        [scenes[0], scenes[1], scenes[2]], audio_paths, NARRATIONS
    )):
        audio = AudioFileClip(str(narr_path))
        dur = audio.duration

        sub_frames = [add_subtitle(f, narr_text) for f in scene_frames]
        needed = int(dur * fps) + 1
        if len(sub_frames) < needed:
            sub_frames = sub_frames + [sub_frames[-1]] * (needed - len(sub_frames))
        else:
            sub_frames = sub_frames[:needed]

        clip = frames_to_clip(sub_frames, fps).with_audio(audio.with_duration(dur))
        clips.append(clip)
        print(f"  Scene {i+1}: {dur:.1f}s")

    total = sum(c.duration for c in clips)
    print(f"\n  Total: {total:.1f}s")

    final = concatenate_videoclips(clips, method="compose")
    out = BASE / "hanami_stack_day27.mp4"

    import os
    os.environ['IMAGEIO_FFMPEG_EXE'] = FFMPEG_PATH

    final.write_videofile(
        str(out), fps=fps,
        codec="libx264", audio_codec="aac",
        ffmpeg_params=["-crf", "22", "-preset", "fast"],
        logger="bar"
    )
    print(f"\nDone! -> {out}")
    for c in clips:
        c.close()
    final.close()

if __name__ == "__main__":
    build_video()
