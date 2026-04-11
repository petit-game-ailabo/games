# -*- coding: utf-8 -*-
"""
CHAIN BLAST Day25 - Short Promo Video
* VOICEVOX (speaker_id=8) / Kasukabe Tsumugi
* OBS録画素材を使用
* 30~45秒ショート動画
"""
import json
from pathlib import Path
import numpy as np
import requests
from PIL import Image, ImageDraw, ImageFont
from moviepy import (AudioFileClip, VideoFileClip, VideoClip,
                     concatenate_videoclips)

BASE = Path(__file__).parent
TMP  = BASE / "_video_tmp"
TMP.mkdir(exist_ok=True)

SPEAKER_ID = 8
VW, VH     = 1080, 1920
CONTENT_W  = 752   # 有効幅 (右側の黒帯を除く)

# OBS録画ファイルをここに設定
VIDEOS = [
    BASE / "rec_01.mp4",  # タイトル画面〜プレイ開始
    BASE / "rec_02.mp4",  # 連鎖爆発シーン
    BASE / "rec_03.mp4",  # 高スコア・リザルト画面
]

# (ナレーション, 使用動画インデックス, 開始秒, 終了秒)
SCENES = [
    (
        "今日はCHAIN BLAST！数字ボールをタップして連鎖爆発を狙う60秒スコアアタックだよ！",
        0, 0, 10,
    ),
    (
        "大きい数字をタップすると小さい数字に連鎖する！うまく繋げればFEVERやMAX BLASTが炸裂するよ！",
        1, 0, 12,
    ),
    (
        "60秒でどこまでスコアを伸ばせるか挑戦してみて！概要欄から遊べるよ！",
        2, 0, 10,
    ),
]

def load_font(size):
    for p in [
        r"C:\Windows\Fonts\YuGothB.ttc",
        r"C:\Windows\Fonts\YuGothM.ttc",
        r"C:\Windows\Fonts\meiryo.ttc",
        r"C:\Windows\Fonts\msgothic.ttc",
    ]:
        if Path(p).exists():
            try: return ImageFont.truetype(p, size)
            except: pass
    return ImageFont.load_default()

F_SUB = load_font(54)

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
    print(f"  [tts] {Path(path).name} ({len(r2.content)//1024}KB)")

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

def process_frame(frame_arr, voice_text, progress=1.0):
    h, w = frame_arr.shape[:2]
    crop_w = min(CONTENT_W, w)
    cropped = frame_arr[:, :crop_w]

    ci_h, ci_w = cropped.shape[:2]
    scale = VH / ci_h
    new_w = int(ci_w * scale)
    img = Image.fromarray(cropped).resize((new_w, VH), Image.LANCZOS)
    x_off = (new_w - VW) // 2
    if new_w >= VW:
        img = img.crop((x_off, 0, x_off + VW, VH))
    else:
        canvas = Image.new("RGB", (VW, VH), (0, 0, 0))
        canvas.paste(img, ((VW - new_w) // 2, 0))
        img = canvas

    img = img.convert("RGBA")
    ov  = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d   = ImageDraw.Draw(ov)

    sub_max_w = VW - 80
    lines = wrap_text(voice_text, F_SUB, sub_max_w)[-3:]
    line_h = 64
    total_h = len(lines) * line_h + 40
    bar_y = VH - total_h - 60

    slide = int((1.0 - min(progress, 1.0)) * (total_h + 80))
    bar_y_anim = bar_y + slide
    d.rounded_rectangle([20, bar_y_anim, VW - 20, bar_y_anim + total_h],
                         radius=16, fill=(0, 0, 0, 180))

    ty = bar_y_anim + 20
    for line in lines:
        bb = d.textbbox((0, 0), line, font=F_SUB)
        lw = bb[2] - bb[0]
        tx = (VW - lw) // 2
        for dx, dy in [(-2,0),(2,0),(0,-2),(0,2),(-2,-2),(2,2),(-2,2),(2,-2)]:
            d.text((tx+dx, ty+dy), line, font=F_SUB, fill=(0,0,0,255))
        d.text((tx, ty), line, font=F_SUB, fill=(255,255,255,255))
        ty += line_h

    return np.array(Image.alpha_composite(img, ov).convert("RGB"))

def build_scene(voice_text, vid_idx, t_start, t_end, idx):
    audio_path = TMP / f"audio_{idx:02d}.wav"
    tts(voice_text, audio_path)
    audio = AudioFileClip(str(audio_path))
    dur = audio.duration

    video_path = VIDEOS[vid_idx]
    avail = min(t_end, t_start + dur + 0.5) - t_start
    src = VideoFileClip(str(video_path)).subclipped(t_start, t_start + avail)
    if src.duration < dur:
        from moviepy import vfx
        src = src.with_effects([vfx.Loop(duration=dur)])
    src = src.with_duration(dur)

    fps = 24
    def make_frame(t):
        vt = min(max(t, 0), src.duration - 1.0/fps)
        frame = src.get_frame(vt)
        progress = min(1.0, t / 0.3) if t > 0.02 else 0.0
        return process_frame(frame, voice_text, progress)

    clip = VideoClip(make_frame, duration=dur).with_fps(fps)
    clip = clip.with_audio(audio.with_duration(dur))
    src.close()
    return clip

def self_check():
    print("\n[self-check] サンプルフレームを確認中...")
    missing = [v for v in VIDEOS if not v.exists()]
    if missing:
        print(f"  [ERROR] 以下の録画ファイルが見つかりません:")
        for m in missing:
            print(f"    {m}")
        print("  OBSで録画してからファイル名を上記に合わせてください")
        return False
    for vi, vp in enumerate(VIDEOS):
        src = VideoFileClip(str(vp))
        frame = src.get_frame(min(3, src.duration - 0.1))
        result = process_frame(frame, "テスト字幕：画面確認中", 1.0)
        Image.fromarray(result).save(TMP / f"check_v{vi}.png")
        print(f"  Saved check_v{vi}.png  (duration={src.duration:.1f}s, size={src.size})")
        src.close()
    return True

def main():
    print("=== CHAIN BLAST Promo Video ===")
    if not self_check():
        return

    print("\n[1/2] TTS + building clips...")
    clips = []
    for i, (voice, vi, ts, te) in enumerate(SCENES):
        print(f"  Scene {i+1}/{len(SCENES)}: {voice[:30]}...")
        clips.append(build_scene(voice, vi, ts, te, i))

    total = sum(c.duration for c in clips)
    print(f"\n  Total duration: {total:.1f}s")

    print("\n[2/2] Rendering...")
    final = concatenate_videoclips(clips, method="compose")
    out = BASE / "chain_blast_day25.mp4"
    final.write_videofile(
        str(out), fps=24,
        codec="libx264", audio_codec="aac",
        ffmpeg_params=["-crf", "20", "-preset", "fast"],
        logger="bar"
    )
    mb = out.stat().st_size // 1024 // 1024
    print(f"\nDone! -> {out}  ({mb}MB)")
    for c in clips:
        c.close()
    final.close()

if __name__ == "__main__":
    main()
