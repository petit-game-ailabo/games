# -*- coding: utf-8 -*-
"""
PicoCoin Miner - Short Promo Video v4
* VOICEVOX 春日部つむぎ (speaker_id=8)
* 実際のプレイ動画をレターボックスで9:16に (中身が見える)
* 字幕はemoji除去・文字化け対策済み
"""
import json, time
from pathlib import Path
import numpy as np
import requests
from PIL import Image, ImageDraw, ImageFont
from moviepy import (AudioFileClip, VideoFileClip, VideoClip,
                     concatenate_videoclips)

BASE = Path(__file__).parent
TMP  = BASE / "_video_tmp4"
TMP.mkdir(exist_ok=True)

SPEAKER_ID = 8   # 春日部つむぎ ノーマル
VW, VH     = 1080, 1920

V1 = BASE / "2026-04-07 02-28-32.mp4"   # 107s
V2 = BASE / "2026-04-07 02-30-41.mp4"   # 55s

SCENES = [
    (
        "ねえねえ、私が作ったゲーム見てほしいんだけど！ピコ、コイン、マイナー！仮想通貨マイニングのシミュレーターだよ！",
        "PicoCoin Miner",
        "仮想通貨マイニングシミュレーター",
        V1, 0, 20,
    ),
    (
        "こんな感じでノードをドラッグして配置するんだけど、繋ぎ方で全然変わってくるのがめちゃくちゃ面白い！",
        "ノードをドラッグ＆ドロップで配置！",
        "繋ぎ方が超重要",
        V1, 20, 45,
    ),
    (
        "マイニングPCにCPUとか水冷とか電源を繋いでいくんだけど、パーツ増やすと熱が上がっちゃって！冷却管理が超大事なんだよね！",
        "CPU・冷却・電源を接続！",
        "熱管理が鍵！",
        V1, 45, 75,
    ),
    (
        "ライトニングネットワークっていうのがあって、お金を入金するとルーティング手数料が自動で貯まるの！かわいくない？",
        "Lightning Network",
        "入金するだけで手数料収入！",
        V1, 75, 107,
    ),
    (
        "でもね、サーバーがウイルスに感染することがあって、ドクロが出てきたらLN残高の10パーセントが盗まれちゃう！ドキドキするでしょ！",
        "ウイルス感染！",
        "LN残高の10%が盗まれる危機！",
        V2, 0, 20,
    ),
    (
        "研究ツリーで水冷とかAIマイニングとか解放していくと、どんどん強くなれるよ！どれを先に研究するか悩むのが楽しいんだよね！",
        "研究ツリーで強化！",
        "解放するほど収益アップ",
        V2, 20, 45,
    ),
    (
        "ぜひ遊んでみてね！めっちゃ楽しいから！コメントで感想教えてくれると嬉しいな！",
        "今すぐプレイしてみて！",
        "コメントお待ちしてます！",
        V2, 45, 55,
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

F_BIG = load_font(64)
F_MED = load_font(48)

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
    print(f"  [tts] {Path(path).name}  ({len(r2.content)//1024}KB)")

# ── frame helpers ─────────────────────────────────────────────────────────
def fit_to_portrait(frame_arr):
    """
    ゲーム画面(横長)を縦9:16に収める。
    上部にゲーム画面、下部は濃い背景でキャプション領域。
    """
    h, w = frame_arr.shape[:2]
    # ゲーム画面エリア: 上60%
    game_h = int(VH * 0.60)
    # アスペクト維持でgame_hに合わせてスケール
    scale = game_h / h
    new_w = int(w * scale)
    new_h = game_h
    img_game = Image.fromarray(frame_arr).resize((new_w, new_h), Image.LANCZOS)
    # 横方向: VWに足りない分は黒でパディング、はみ出す分はクロップ
    canvas = Image.new("RGB", (VW, VH), (8, 12, 32))
    x_off = (VW - min(new_w, VW)) // 2
    if new_w > VW:
        # センタークロップ
        crop_x = (new_w - VW) // 2
        img_game = img_game.crop((crop_x, 0, crop_x + VW, new_h))
        x_off = 0
    canvas.paste(img_game, (x_off, 0))
    return np.array(canvas)


def draw_caption(frame_arr, line1, line2, progress=1.0):
    img = Image.fromarray(frame_arr).convert("RGBA")
    bar_h = 260
    slide = int((1.0 - min(progress, 1.0)) * (bar_h + 60))
    by = VH - bar_h - 60 + slide

    ov = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d  = ImageDraw.Draw(ov)
    d.rounded_rectangle([40, by, VW - 40, by + bar_h], radius=20, fill=(10, 14, 50, 220))

    # line1 (大きい・黄色)
    bb1 = d.textbbox((0, 0), line1, font=F_BIG)
    x1 = (VW - (bb1[2] - bb1[0])) // 2
    d.text((x1, by + 36), line1, font=F_BIG, fill=(255, 220, 60, 255))
    # line2 (中・白っぽい)
    bb2 = d.textbbox((0, 0), line2, font=F_MED)
    x2 = (VW - (bb2[2] - bb2[0])) // 2
    d.text((x2, by + 36 + 78), line2, font=F_MED, fill=(200, 220, 255, 255))

    return np.array(Image.alpha_composite(img, ov).convert("RGB"))


# ── build one scene ───────────────────────────────────────────────────────
def build_scene(voice_text, line1, line2, video_path, t_start, t_end, idx):
    audio_path = TMP / f"audio_{idx:02d}.wav"
    tts(voice_text, audio_path)
    audio = AudioFileClip(str(audio_path))
    dur = audio.duration

    src = VideoFileClip(str(video_path)).subclipped(t_start, min(t_end, t_start + dur + 1.0))
    if src.duration < dur:
        from moviepy import vfx
        src = src.with_effects([vfx.Loop(duration=dur)])
    src = src.with_duration(dur)

    fps = 24

    def make_frame(t):
        vt = min(t, src.duration - 1.0 / fps)
        frame = src.get_frame(max(vt, 0))
        frame = fit_to_portrait(frame)
        progress = min(1.0, t / 0.35) if t > 0.03 else 0.0
        frame = draw_caption(frame, line1, line2, progress)
        return frame

    clip = VideoClip(make_frame, duration=dur).with_fps(fps)
    clip = clip.with_audio(audio.with_duration(dur))
    src.close()
    return clip


# ── main ──────────────────────────────────────────────────────────────────
def main():
    print("=== PicoCoin Promo Video v4 (VOICEVOX tsumugu) ===")

    print("\n[1/2] Generating VOICEVOX audio + building clips...")
    clips = []
    for i, (voice, l1, l2, vid, ts, te) in enumerate(SCENES):
        print(f"  Scene {i+1}/{len(SCENES)}")
        clip = build_scene(voice, l1, l2, vid, ts, te, i)
        clips.append(clip)

    print("\n[2/2] Rendering final video...")
    final = concatenate_videoclips(clips, method="compose")
    out = BASE / "picocoin_promo_v4.mp4"
    final.write_videofile(
        str(out), fps=24,
        codec="libx264", audio_codec="aac",
        ffmpeg_params=["-crf", "20", "-preset", "fast"],
        logger="bar"
    )
    size_mb = out.stat().st_size // 1024 // 1024
    print(f"\nDone! -> {out}  ({size_mb}MB)")
    for c in clips:
        c.close()
    final.close()


if __name__ == "__main__":
    main()
