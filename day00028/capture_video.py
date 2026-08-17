# -*- coding: utf-8 -*-
"""
とびきりケーキ屋さん - 完全自動動画生成スクリプト
"""
import json
import time
import base64
import requests
from pathlib import Path
from io import BytesIO
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from moviepy import AudioFileClip, VideoClip, concatenate_videoclips

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
GAME_URL = "http://localhost:5173/day00028"
FFMPEG_PATH = r"C:\Users\talashi\AppData\Local\Programs\Python\Python312\Lib\site-packages\imageio_ffmpeg\binaries\ffmpeg-win-x86_64-v7.1.exe"

NARRATIONS = [
    "とびきりケーキ屋さん！落ちてくるフルーツやスイーツをタップして、お客様好みのケーキを作ろう！",
    "甘さゲージとにぎやかさゲージを見ながら、いちごやチョコで甘くしたり、レモンやミントでさっぱりさせたり！30秒でケーキが完成したら、お客様から感想が届くよ！",
    "毎日新作ミニゲームを投稿してるよ！概要欄のリンクから今すぐ無料で遊んでみてね！",
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
    frames: [], recording: false, intervalId: null,
    start(fps) {
        this.frames = []; this.recording = true;
        const c = document.getElementById('gameCanvas');
        this.intervalId = setInterval(() => {
            if (!this.recording) return;
            this.frames.push(c.toDataURL('image/jpeg', 0.88));
        }, 1000 / (fps || 40));
    },
    stop() {
        this.recording = false;
        if (this.intervalId) { clearInterval(this.intervalId); this.intervalId = null; }
        return this.frames.length;
    },
    flush() { const f = this.frames; this.frames = []; return f; },
    snapshot() { return document.getElementById('gameCanvas').toDataURL('image/jpeg', 0.92); }
};
'ok'
"""

def decode_frames(raw_list, target_w, target_h):
    result = []
    for data in raw_list:
        img = Image.open(BytesIO(base64.b64decode(data.split(',')[1])))
        img = img.resize((target_w, target_h), Image.LANCZOS)
        result.append(img)
    return result

def capture_frames():
    scenes = {}
    FPS = 40

    # アイテムをタップするX座標パターン（ケーキ上の広い範囲）
    TAP_POSITIONS = [
        (120, 350), (200, 400), (280, 350), (160, 450),
        (240, 380), (195, 320), (140, 420), (260, 440),
        (195, 360), (170, 390), (220, 340), (195, 410),
        (150, 370), (250, 360), (195, 430), (130, 340),
        (270, 390), (195, 380), (180, 350), (210, 420),
    ]

    with sync_playwright() as pw:
        browser = pw.chromium.launch(
            headless=True,
            args=[
                '--disable-background-timer-throttling',
                '--disable-renderer-backgrounding',
                '--disable-backgrounding-occluded-windows',
                '--disable-background-media-suspend',
            ]
        )
        page = browser.new_page(viewport={"width": 390, "height": 844})
        page.goto(GAME_URL + '?record=1')
        time.sleep(2.5)
        page.evaluate(RECORDER_JS)

        # ---- Scene 1: タイトル画面 ----
        print(f"  Scene 1: title (static 2s)...")
        raw_title = page.evaluate("window._recorder.snapshot()")
        frame_title = decode_frames([raw_title], VW, VH)[0]
        scenes[0] = [frame_title] * (FPS * 2)
        print(f"    {len(scenes[0])} frames")

        # ---- ゲーム開始 → カスタマー画面へ ----
        page.evaluate("window._recordCtrl.start()")
        time.sleep(1.0)

        # カスタマー画面の「ケーキを作る！」ボタンをクリック
        page.mouse.click(195, 638)
        time.sleep(0.5)

        # ---- Scene 2: ゲームプレイ録画（30秒 + 演出） ----
        print(f"  Scene 2: gameplay (@ {FPS}fps)...")
        page.evaluate(f"window._recorder.start({FPS})")

        # 30秒分のタップシミュレーション（ビートに合わせて約0.47秒間隔）
        tap_interval = 0.47
        for i, (tx, ty) in enumerate(TAP_POSITIONS):
            page.mouse.click(tx, ty)
            print(f"    tap {i+1}: ({tx}, {ty})")
            time.sleep(tap_interval)

        # 残り時間を待つ（30秒 - タップ時間）
        elapsed = len(TAP_POSITIONS) * tap_interval
        remaining = max(0, 32 - elapsed)  # 2秒余裕
        time.sleep(remaining)

        # 完成演出 + 採点画面
        time.sleep(4.0)

        page.evaluate("window._recorder.stop()")
        raw2 = page.evaluate("window._recorder.flush()")
        print(f"    {len(raw2)} frames")
        scenes[1] = decode_frames(raw2, VW, VH)

        # ---- Scene 3: スコア画面スナップショット ----
        print(f"  Scene 3: score screen (3s static)...")
        raw3 = page.evaluate("window._recorder.snapshot()")
        frame3 = decode_frames([raw3], VW, VH)[0]
        scenes[2] = [frame3] * (FPS * 3)
        print(f"    {len(scenes[2])} frames")

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
    print("=== とびきりケーキ屋さん Video Generator ===")

    try:
        requests.get("http://localhost:50021/version", timeout=2)
        print("[OK] VOICEVOX running")
    except:
        print("[ERROR] VOICEVOX not running. Start VOICEVOX first!")
        return

    print("\n[1/3] Narration TTS...")
    audio_paths = []
    for i, text in enumerate(NARRATIONS):
        p = TMP / f"narr_{i:02d}.wav"
        tts(text, p)
        audio_paths.append(p)

    print("\n[2/3] Capturing gameplay...")
    if DRIVER != 'playwright':
        print("[ERROR] Playwright not installed.")
        return
    scenes = capture_frames()

    print("\n[3/3] Compositing video...")
    fps = 40
    clips = []
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
    print(f"  Total: {total:.1f}s")
    if total > 60:
        print("  WARNING: over 60s! Consider trimming narration.")

    final = concatenate_videoclips(clips, method="compose")
    out = BASE / "cake_day28.mp4"

    import os
    os.environ['IMAGEIO_FFMPEG_EXE'] = FFMPEG_PATH
    final.write_videofile(
        str(out), fps=fps,
        codec="libx264", audio_codec="aac",
        ffmpeg_params=["-crf", "22", "-preset", "fast"],
        logger="bar"
    )
    print(f"\nDone! -> {out}")
    for c in clips: c.close()
    final.close()

if __name__ == "__main__":
    build_video()
