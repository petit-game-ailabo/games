# -*- coding: utf-8 -*-
"""
PicoCoin Miner - Short Promo Video v3
* ElevenLabs Jessica voice (young/playful) in Japanese via multilingual_v2
* Actual gameplay video clips (1280x720) cropped to 9:16 (1080x1920)
* Animated caption overlays
"""
import os, sys, time, requests
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
from moviepy import (AudioFileClip, VideoFileClip, VideoClip,
                     concatenate_videoclips)

BASE = Path(__file__).parent
TMP  = BASE / "_video_tmp3"
TMP.mkdir(exist_ok=True)

ELEVEN_KEY = "sk_ea8a78676916649d1de990b98b8286ab34b02e365a1163a6"
VOICE_ID   = "cgSgspJ2msm6clMCkdW9"   # Jessica - young/playful
MODEL      = "eleven_multilingual_v2"
VW, VH     = 1080, 1920

# ── video files ──────────────────────────────────────────────────────────
V1 = BASE / "2026-04-07 02-28-32.mp4"   # 107s
V2 = BASE / "2026-04-07 02-30-41.mp4"   # 55s

# ── scenes ───────────────────────────────────────────────────────────────
# (voice_text, caption_line1, caption_line2, video_file, t_start, t_end)
# t_start/t_end = time range to use from source video
SCENES = [
    (
        "ねえねえ、私が作ったゲーム見てほしいんだけど！ピコ、コイン、マイナー！仮想通貨マイニングのシミュレーターだよ！",
        "🪙 PicoCoin Miner",
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
        "⛏ CPU・冷却・電源を接続！",
        "熱管理が鍵！",
        V1, 45, 75,
    ),
    (
        "ライトニングネットワークっていうのがあって、お金を入金するとルーティング手数料が自動で貯まるの！かわいくない？",
        "⚡ Lightning Network",
        "入金するだけで手数料収入！",
        V1, 75, 107,
    ),
    (
        "でもね、サーバーがウイルスに感染することがあって、ドクロが出てきたらLN残高の10%が盗まれちゃう！ドキドキするでしょ！",
        "🦠 ウイルス感染！",
        "LN残高の10%が盗まれる危機！",
        V2, 0, 20,
    ),
    (
        "研究ツリーで水冷とかAIマイニングとか解放していくと、どんどん強くなれるよ！どれを先に研究するか悩むのが楽しいんだよね！",
        "🔬 研究ツリーで強化！",
        "解放するほど収益アップ",
        V2, 20, 45,
    ),
    (
        "ぜひ遊んでみてね！めっちゃ楽しいから！コメントで感想教えてくれると嬉しいな！",
        "🎮 今すぐプレイしてみて！",
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

F_BIG   = load_font(70)
F_MED   = load_font(52)

# ── ElevenLabs TTS ────────────────────────────────────────────────────────
def tts(text, path):
    if Path(path).exists():
        print(f"  [cache] {Path(path).name}")
        return
    url = f"https://api.elevenlabs.io/v1/text-to-speech/{VOICE_ID}"
    payload = {
        "text": text,
        "model_id": MODEL,
        "voice_settings": {
            "stability": 0.35,
            "similarity_boost": 0.85,
            "style": 0.55,
            "use_speaker_boost": True
        }
    }
    headers = {"xi-api-key": ELEVEN_KEY, "Content-Type": "application/json"}
    r = requests.post(url, json=payload, headers=headers, timeout=30)
    r.raise_for_status()
    Path(path).write_bytes(r.content)
    print(f"  [tts] {Path(path).name}  ({len(r.content)//1024}KB)")

# ── text overlay ──────────────────────────────────────────────────────────
def draw_caption(frame_arr, line1, line2, progress=1.0):
    """Draw caption bar at bottom of frame (numpy array -> numpy array)."""
    img = Image.fromarray(frame_arr).convert("RGBA")
    bar_h = 220
    slide = int((1.0 - min(progress, 1.0)) * bar_h)
    by = VH - bar_h - 40 + slide

    ov = Image.new("RGBA", img.size, (0,0,0,0))
    d  = ImageDraw.Draw(ov)
    d.rounded_rectangle([30, by, VW-30, by+bar_h], radius=18, fill=(0,0,0,195))

    d2 = ImageDraw.Draw(ov)
    # line1
    bb1 = d2.textbbox((0,0), line1, font=F_BIG)
    x1 = (VW - (bb1[2]-bb1[0])) // 2
    d2.text((x1, by+28), line1, font=F_BIG, fill=(255,220,60,255))
    # line2
    bb2 = d2.textbbox((0,0), line2, font=F_MED)
    x2 = (VW - (bb2[2]-bb2[0])) // 2
    d2.text((x2, by+28+75), line2, font=F_MED, fill=(200,220,255,255))

    out = Image.alpha_composite(img, ov).convert("RGB")
    return np.array(out)

# ── crop landscape to portrait ────────────────────────────────────────────
def crop_to_portrait(frame_arr):
    """Crop 1280x720 (or any) frame to 9:16 center crop, scaled to 1080x1920."""
    h, w = frame_arr.shape[:2]
    # target aspect: 9/16
    target_w = h * 9 // 16
    if target_w > w:
        target_w = w
    x0 = (w - target_w) // 2
    cropped = frame_arr[:, x0:x0+target_w]
    # Scale up to 1080x1920
    img = Image.fromarray(cropped).resize((VW, VH), Image.LANCZOS)
    return np.array(img)

# ── build one scene ───────────────────────────────────────────────────────
def build_scene(voice_text, line1, line2, video_path, t_start, t_end, scene_idx):
    audio_path = TMP / f"audio_{scene_idx:02d}.mp3"
    tts(voice_text, audio_path)
    audio = AudioFileClip(str(audio_path))
    voice_dur = audio.duration

    # Load video segment
    src = VideoFileClip(str(video_path)).subclipped(t_start, min(t_end, t_start + voice_dur + 1.0))
    src_dur = src.duration
    # If source shorter than audio, loop it
    if src_dur < voice_dur + 0.5:
        from moviepy import vfx
        src = src.with_effects([vfx.Loop(duration=voice_dur + 0.5)])
    src = src.with_duration(voice_dur)

    fps = 24

    def make_frame(t):
        # Get video frame
        vt = min(t, src.duration - 1/fps)
        frame = src.get_frame(vt)
        # Crop to portrait
        frame = crop_to_portrait(frame)
        # Caption slide-in: first 0.4s
        progress = min(1.0, t / 0.4) if t > 0.05 else 0.0
        frame = draw_caption(frame, line1, line2, progress)
        return frame

    clip = VideoClip(make_frame, duration=voice_dur)
    safe_audio = audio.with_duration(voice_dur)
    clip = clip.with_audio(safe_audio)
    clip = clip.with_fps(fps)
    src.close()
    return clip

# ── main ──────────────────────────────────────────────────────────────────
def main():
    print("=== PicoCoin Promo Video v3 ===")

    print("\n[1/2] Generating ElevenLabs audio + building clips...")
    clips = []
    for i, (voice, l1, l2, vid, ts, te) in enumerate(SCENES):
        print(f"  Scene {i+1}/{len(SCENES)}")
        clip = build_scene(voice, l1, l2, vid, ts, te, i)
        clips.append(clip)
        time.sleep(0.2)

    print("\n[2/2] Rendering final video...")
    final = concatenate_videoclips(clips, method="compose")
    out = BASE / "picocoin_promo_v3.mp4"
    final.write_videofile(
        str(out), fps=24,
        codec="libx264", audio_codec="aac",
        ffmpeg_params=["-crf", "20", "-preset", "fast"],
        logger="bar"
    )
    print(f"\nDone! -> {out}  ({out.stat().st_size//1024//1024}MB)")
    for c in clips:
        c.close()
    final.close()

if __name__ == "__main__":
    main()
