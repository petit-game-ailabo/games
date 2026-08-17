# -*- coding: utf-8 -*-
"""
PicoCoin Miner - Short Promo Video v5
* VOICEVOX 春日部つむぎ (speaker_id=8)
* ゲーム画面(左722px)を全画面9:16に拡大
* ショート動画スタイル: 話してる内容をそのまま下部に大きく字幕表示
"""
import json, re
from pathlib import Path
import numpy as np
import requests
from PIL import Image, ImageDraw, ImageFont
from moviepy import (AudioFileClip, VideoFileClip, VideoClip,
                     concatenate_videoclips)

BASE = Path(__file__).parent
TMP  = BASE / "_video_tmp5"
TMP.mkdir(exist_ok=True)

SPEAKER_ID   = 8      # 春日部つむぎ ノーマル
VW, VH       = 1080, 1920
CONTENT_W    = 722    # ゲーム画面の有効幅 (右側は黒)

V1 = BASE / "2026-04-07 02-28-32.mp4"
V2 = BASE / "2026-04-07 02-30-41.mp4"

# voice_text = 実際に喋るテキスト (字幕にもそのまま使う)
SCENES = [
    (
        "ねえねえ、私が作ったゲーム見てほしいんだけど！ピコ、コイン、マイナー！仮想通貨マイニングのシミュレーターだよ！",
        V1, 0, 18,
    ),
    (
        "こんな感じでノードをドラッグして配置するんだけど、繋ぎ方で全然変わってくるのがめちゃくちゃ面白い！",
        V1, 18, 42,
    ),
    (
        "マイニングPCにCPUとか水冷とか電源を繋いでいくんだけど、パーツ増やすと熱が上がっちゃって！冷却管理が超大事！",
        V1, 42, 72,
    ),
    (
        "ライトニングネットワークっていうのがあって、お金を入金するとルーティング手数料が自動で貯まるの！",
        V1, 72, 107,
    ),
    (
        "サーバーがウイルスに感染することがあって、LN残高の10パーセントが盗まれちゃう！ドキドキするでしょ！",
        V2, 0, 22,
    ),
    (
        "研究ツリーで水冷とかAIマイニングとか解放していくと、どんどん強くなれるよ！どれ先に研究するか悩むのが楽しい！",
        V2, 22, 46,
    ),
    (
        "ぜひ遊んでみてね！めっちゃ楽しいから！概要欄のリンクからすぐ遊べます！",
        V2, 46, 55,
    ),
]

# ── font ─────────────────────────────────────────────────────────────────
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

F_SUB = load_font(54)   # 字幕メイン

# ── VOICEVOX TTS ──────────────────────────────────────────────────────────
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

# ── text wrap ────────────────────────────────────────────────────────────
def wrap_text(text, font, max_w):
    """日本語対応: 文字単位で折り返し"""
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

# ── frame processing ──────────────────────────────────────────────────────
def process_frame(frame_arr, voice_text, progress=1.0):
    h, w = frame_arr.shape[:2]

    # 1) 有効コンテンツ領域(左CONTENT_W px)を切り取り
    crop_w = min(CONTENT_W, w)
    cropped = frame_arr[:, :crop_w]   # 722x720

    # 2) 全画面9:16に拡大 (縦に引き伸ばし)
    #    縦を1920に合わせ、横は1080に合わせてクロップ
    ci_h, ci_w = cropped.shape[:2]
    # まず縦1920にスケール
    scale = VH / ci_h
    new_w = int(ci_w * scale)   # 722 * (1920/720) = ~1924
    new_h = VH                   # 1920
    img = Image.fromarray(cropped).resize((new_w, new_h), Image.LANCZOS)
    # 横方向: センタークロップして1080に
    x_off = (new_w - VW) // 2
    if new_w >= VW:
        img = img.crop((x_off, 0, x_off + VW, VH))
    else:
        # 足りない場合はパディング
        canvas = Image.new("RGB", (VW, VH), (8, 12, 32))
        canvas.paste(img, ((VW - new_w) // 2, 0))
        img = canvas

    # 3) 字幕オーバーレイ (ショート動画スタイル)
    img = img.convert("RGBA")
    ov  = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d   = ImageDraw.Draw(ov)

    sub_max_w = VW - 80
    lines = wrap_text(voice_text, F_SUB, sub_max_w)

    # 最大3行表示
    lines = lines[-3:]
    line_h = 64
    total_h = len(lines) * line_h + 40
    bar_y = VH - total_h - 60

    # 半透明背景
    slide = int((1.0 - min(progress, 1.0)) * (total_h + 80))
    bar_y_anim = bar_y + slide
    d.rounded_rectangle([20, bar_y_anim, VW - 20, bar_y_anim + total_h],
                         radius=16, fill=(0, 0, 0, 180))

    # テキスト描画 (黒縁+白テキスト でくっきり)
    ty = bar_y_anim + 20
    for line in lines:
        bb = d.textbbox((0, 0), line, font=F_SUB)
        lw = bb[2] - bb[0]
        tx = (VW - lw) // 2
        # 黒縁
        for dx, dy in [(-2,0),(2,0),(0,-2),(0,2),(-2,-2),(2,2),(-2,2),(2,-2)]:
            d.text((tx + dx, ty + dy), line, font=F_SUB, fill=(0, 0, 0, 255))
        # 白テキスト
        d.text((tx, ty), line, font=F_SUB, fill=(255, 255, 255, 255))
        ty += line_h

    result = Image.alpha_composite(img, ov).convert("RGB")
    return np.array(result)


# ── build one scene ───────────────────────────────────────────────────────
def build_scene(voice_text, video_path, t_start, t_end, idx):
    audio_path = TMP / f"audio_{idx:02d}.wav"
    tts(voice_text, audio_path)
    audio = AudioFileClip(str(audio_path))
    dur = audio.duration

    avail = min(t_end, t_start + dur + 0.5) - t_start
    src = VideoFileClip(str(video_path)).subclipped(t_start, t_start + avail)
    if src.duration < dur:
        from moviepy import vfx
        src = src.with_effects([vfx.Loop(duration=dur)])
    src = src.with_duration(dur)

    fps = 24

    def make_frame(t):
        vt = min(max(t, 0), src.duration - 1.0 / fps)
        frame = src.get_frame(vt)
        progress = min(1.0, t / 0.3) if t > 0.02 else 0.0
        return process_frame(frame, voice_text, progress)

    clip = VideoClip(make_frame, duration=dur).with_fps(fps)
    clip = clip.with_audio(audio.with_duration(dur))
    src.close()
    return clip


# ── self-check: save sample frames ───────────────────────────────────────
def self_check():
    print("\n[self-check] Saving sample frames to _video_tmp5/check_*.png ...")
    from moviepy import VideoFileClip
    for vid, t, name in [(V1, 10, "v1_t10"), (V1, 50, "v1_t50"), (V2, 25, "v2_t25")]:
        src = VideoFileClip(str(vid))
        frame = src.get_frame(t)
        result = process_frame(frame, "テスト字幕：ゲーム画面が全体に表示されているか確認中", 1.0)
        Image.fromarray(result).save(TMP / f"check_{name}.png")
        print(f"  Saved check_{name}.png  ({VW}x{VH})")
        src.close()


# ── main ─────────────────────────────────────────────────────────────────
def main():
    print("=== PicoCoin Promo Video v5 ===")

    # まずフレームチェック
    self_check()

    print("\n[1/2] TTS + building clips...")
    clips = []
    for i, (voice, vid, ts, te) in enumerate(SCENES):
        print(f"  Scene {i+1}/{len(SCENES)}")
        clips.append(build_scene(voice, vid, ts, te, i))

    print("\n[2/2] Rendering...")
    final = concatenate_videoclips(clips, method="compose")
    out = BASE / "picocoin_promo_v5.mp4"
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
