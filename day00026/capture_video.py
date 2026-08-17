# -*- coding: utf-8 -*-
"""
DRIFT ISLAND - 完全自動動画生成スクリプト
Playwright でブラウザを操作しながらcanvasをキャプチャ → moviepy + VOICEVOX で動画合成
"""
import json
import time
import base64
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
GAME_URL = "http://localhost:3003/day00026"
FFMPEG_PATH = r"C:\Users\talashi\AppData\Local\Programs\Python\Python312\Lib\site-packages\imageio_ffmpeg\binaries\ffmpeg-win-x86_64-v7.1.exe"

NARRATIONS = [
    "今日のゲームはDRIFT ISLAND！漂流島からの脱出サバイバルだよ！",
    "昼はガンガン採集して、夜はベッドで安全に過ごそう！空腹と水分管理が大事！",
    "素材を集めてクラフトして、船を作って島から脱出しよう！概要欄から遊べるよ！",
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

def capture_frames_playwright():
    RECORDER_JS = """
    window._recorder = {
        frames: [],
        recording: false,
        intervalId: null,
        _prev: null,

        start(fps) {
            this.frames = [];
            this.recording = true;
            this._prev = null;
            const c = document.getElementById('gameCanvas');
            this.intervalId = setInterval(() => {
                if (!this.recording) return;
                const d = c.toDataURL('image/jpeg', 0.88);
                if (d !== this._prev) {
                    this.frames.push(d);
                    this._prev = d;
                }
            }, 1000 / (fps || 60));
        },

        stop() {
            this.recording = false;
            if (this.intervalId) { clearInterval(this.intervalId); this.intervalId = null; }
            return this.frames.length;
        },

        flush() {
            const f = this.frames;
            this.frames = [];
            this._prev = null;
            return f;
        }
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

    scenes = {}
    FPS = 40

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
        page = browser.new_page(viewport={"width": 800, "height": 660})
        page.goto(GAME_URL + '?record=1')
        time.sleep(2.0)

        # レコーダー注入
        page.evaluate(RECORDER_JS)

        # ---- Scene 1: タイトル画面 (3秒 静止画) ----
        print(f"  Capturing scene 1: title (3s static)...")
        time.sleep(1.0)
        ss = page.screenshot(type="jpeg", quality=90)
        title_img = Image.open(BytesIO(ss)).resize((VW, VH), Image.LANCZOS)
        scenes[0] = [title_img] * int(FPS * 3)
        print(f"    {len(scenes[0])} frames (static)")

        # ---- ゲーム開始 ----
        page.evaluate("""
            deleteSave();
            hideTitleScreen();
            restartGame();
        """)
        time.sleep(0.5)

        # ---- Scene 2: 採集→移動→クラフトを自動操作 (12秒) ----
        print(f"  Capturing scene 2: gameplay (12s @ {FPS}fps)...")
        # ページ全体スクリーンショット方式（UIも映る）
        scene2_frames = []

        # 自動プレイ: 採集を数回→移動→採集→クラフト
        page.evaluate("""
        window._autoActions = [];
        // 採集3回
        for(let i=0; i<3; i++) window._autoActions.push(() => doGather());
        // 隣接マスへ移動
        window._autoActions.push(() => {
            const adj = getAdjacentCells(G.pr, G.pc);
            if(adj.length > 0) {
                const t = adj.find(a => MAP[a.r][a.c] === 3) || adj[0]; // 森優先
                doMove(t.r, t.c);
            }
        });
        // 採集3回
        for(let i=0; i<3; i++) window._autoActions.push(() => doGather());
        // 戻る
        window._autoActions.push(() => {
            const adj = getAdjacentCells(G.pr, G.pc);
            if(adj.length > 0) {
                const t = adj.find(a => MAP[a.r][a.c] === 6) || adj[0]; // ホーム優先
                doMove(t.r, t.c);
            }
        });
        // 採集2回
        for(let i=0; i<2; i++) window._autoActions.push(() => doGather());
        // クラフト試行
        window._autoActions.push(() => {
            const r = RECIPES.find(r => canCraft(r) === true);
            if(r) doCraft(r);
        });

        let _ai = 0;
        window._autoplay = setInterval(() => {
            if(_ai < window._autoActions.length) {
                try { window._autoActions[_ai](); } catch(e) {}
                _ai++;
            }
        }, 1200);
        """)
        # 12秒間ページスクリーンショットを撮り続ける
        end_time = time.time() + 12.0
        while time.time() < end_time:
            ss = page.screenshot(type="jpeg", quality=85)
            img = Image.open(BytesIO(ss)).resize((VW, VH), Image.LANCZOS)
            scene2_frames.append(img)
            time.sleep(1.0 / FPS)
        page.evaluate("clearInterval(window._autoplay)")
        print(f"    {len(scene2_frames)} frames captured")
        scenes[1] = scene2_frames

        # ---- Scene 3: クラフト画面 (4秒 静止画) ----
        print(f"  Capturing scene 3: craft menu (4s static)...")
        page.evaluate("openCraft()")
        time.sleep(0.5)
        ss3 = page.screenshot(type="jpeg", quality=90)
        craft_img = Image.open(BytesIO(ss3)).resize((VW, VH), Image.LANCZOS)
        scenes[2] = [craft_img] * int(FPS * 4)
        print(f"    {len(scenes[2])} frames (static)")

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
    print("=== DRIFT ISLAND Video Generator ===")

    try:
        requests.get("http://localhost:50021/version", timeout=2)
        print("[OK] VOICEVOX running")
    except:
        print("[ERROR] VOICEVOX not running. Start VOICEVOX first.")
        return

    print("\n[1/3] Generating narration...")
    audio_paths = []
    for i, text in enumerate(NARRATIONS):
        p = TMP / f"narr_{i:02d}.wav"
        tts(text, p)
        audio_paths.append(p)

    print("\n[2/3] Capturing game footage...")
    if DRIVER == 'playwright':
        scenes = capture_frames_playwright()
    else:
        print("[ERROR] Playwright not installed.")
        return

    print("\n[3/3] Compositing video...")
    clips = []
    for i, (scene_frames, narr_path, narr_text) in enumerate(zip(
        [scenes[0], scenes[1], scenes[2]], audio_paths, NARRATIONS
    )):
        audio = AudioFileClip(str(narr_path))
        dur = audio.duration

        sub_frames = [add_subtitle(f, narr_text) for f in scene_frames]
        fps = 20
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
    out = BASE / "drift_island_day26.mp4"

    import os
    os.environ['IMAGEIO_FFMPEG_EXE'] = FFMPEG_PATH

    final.write_videofile(
        str(out), fps=40,
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
