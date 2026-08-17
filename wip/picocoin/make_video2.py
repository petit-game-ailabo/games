# -*- coding: utf-8 -*-
"""
PicoCoin Miner - Short Promo Video v2
* ElevenLabs Jessica voice (young/playful) in Japanese via multilingual_v2
* Actual gameplay screenshots with Ken Burns zoom/pan
* Animated text overlays
* 9:16 vertical (1080x1920)
"""
import os, sys, io, time, base64, requests, json, textwrap, math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageEnhance
from moviepy import (AudioFileClip, ImageClip, TextClip,
                     concatenate_videoclips, CompositeVideoClip)

BASE     = Path(__file__).parent
TMP      = BASE / "_video_tmp2"
TMP.mkdir(exist_ok=True)

ELEVEN_KEY   = "sk_ea8a78676916649d1de990b98b8286ab34b02e365a1163a6"
VOICE_ID     = "cgSgspJ2msm6clMCkdW9"   # Jessica - Playful, Bright, Warm, young
MODEL        = "eleven_multilingual_v2"
VW, VH       = 1080, 1920

# ── font ───────────────────────────────────────────────────────────────
def font(size, bold=False):
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

F_HUGE  = font(96, bold=True)
F_BIG   = font(72, bold=True)
F_MED   = font(52)
F_SMALL = font(40)
F_TINY  = font(32)

# ── ElevenLabs TTS ─────────────────────────────────────────────────────
def tts(text, path):
    if Path(path).exists():
        print(f"  [cache] {path.name}")
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
    print(f"  [tts] {path.name}  ({len(r.content)//1024}KB)")

# ── image helpers ───────────────────────────────────────────────────────
def draw_text_wrap(draw, text, cx, y, fnt, color, max_w=VW-120):
    dummy = ImageDraw.Draw(Image.new("RGB",(1,1)))
    words = list(text)  # char-level for Japanese
    # rough word-wrap by pixel width
    lines, line = [], ""
    for ch in text:
        test = line + ch
        bb = dummy.textbbox((0,0), test, font=fnt)
        if bb[2]-bb[0] > max_w and line:
            lines.append(line); line = ch
        else:
            line = test
    if line: lines.append(line)
    total_h = sum((draw.textbbox((0,0),l,font=fnt)[3]-draw.textbbox((0,0),l,font=fnt)[1])+8 for l in lines)
    cy = y - total_h//2
    for l in lines:
        bb = draw.textbbox((0,0),l,font=fnt)
        lw = bb[2]-bb[0]
        draw.text(((VW-lw)//2, cy), l, font=fnt, fill=color)
        cy += (bb[3]-bb[1]) + 10

def rounded_rect(draw, x,y,w,h, r, fill):
    draw.rounded_rectangle([x,y,x+w,y+h], radius=r, fill=fill)

def glow_text(img, text, cx, y, fnt, color, glow_color, glow_r=6):
    """Draw text with glow effect."""
    glow_layer = Image.new("RGBA", img.size, (0,0,0,0))
    gd = ImageDraw.Draw(glow_layer)
    bb = gd.textbbox((0,0), text, font=fnt)
    tw = bb[2]-bb[0]
    tx = cx - tw//2
    for dx in range(-glow_r, glow_r+1, 2):
        for dy in range(-glow_r, glow_r+1, 2):
            gd.text((tx+dx, y+dy), text, font=fnt, fill=glow_color)
    glow_layer = glow_layer.filter(ImageFilter.GaussianBlur(glow_r))
    img = img.convert("RGBA")
    img = Image.alpha_composite(img, glow_layer)
    d = ImageDraw.Draw(img)
    d.text((tx, y), text, font=fnt, fill=color)
    return img.convert("RGB")

# ── screenshot loading ──────────────────────────────────────────────────
# Screenshots captured from the live game (passed as base64 blobs via JS eval)
# We'll save them directly from Pillow by reconstructing from the MCP outputs

SCREENSHOT_DATA = {}  # populated below by base64 strings from MCP

def load_game_shot(name):
    p = TMP / f"shot_{name}.png"
    if p.exists():
        return Image.open(p).convert("RGB")
    return None

# ── Ken Burns effect ────────────────────────────────────────────────────
def make_kenburns_frames(img, duration, fps, from_scale, to_scale,
                          from_xy=(0.5,0.5), to_xy=(0.5,0.5)):
    """Return list of PIL images with slow zoom/pan."""
    n = int(duration * fps)
    frames = []
    iw, ih = img.size
    # target output size
    out_w, out_h = VW, VH
    for i in range(n):
        t = i / max(n-1, 1)
        sc  = from_scale + (to_scale - from_scale) * t
        cx  = from_xy[0] + (to_xy[0] - from_xy[0]) * t
        cy  = from_xy[1] + (to_xy[1] - from_xy[1]) * t
        # crop box in original image
        crop_w = min(iw, int(out_w / sc * (iw/out_w)))
        crop_h = min(ih, int(out_h / sc * (ih/out_h)))
        crop_w = max(crop_w, 100)
        crop_h = max(crop_h, 100)
        left = int(cx * iw - crop_w//2)
        top  = int(cy * ih - crop_h//2)
        left = max(0, min(left, iw - crop_w))
        top  = max(0, min(top,  ih - crop_h))
        crop = img.crop((left, top, left+crop_w, top+crop_h))
        crop = crop.resize((out_w, out_h), Image.LANCZOS)
        frames.append(crop)
    return frames

def frames_to_clip(frames, fps, audio_clip=None):
    """Convert list of PIL images to moviepy clip."""
    import numpy as np
    from moviepy import VideoClip
    frame_arrays = [np.array(f.convert("RGB")) for f in frames]
    total = len(frame_arrays)
    def make_frame(t):
        idx = min(int(t * fps), total-1)
        return frame_arrays[idx]
    dur = total / fps
    clip = VideoClip(make_frame, duration=dur)
    if audio_clip:
        audio_clip = audio_clip.with_duration(dur)
        clip = clip.with_audio(audio_clip)
    return clip

# ── overlay helpers ─────────────────────────────────────────────────────
def overlay_title_card(frame_img, title, sub=None, y_title=None):
    """Add semi-transparent title overlay on bottom of frame."""
    img = frame_img.copy().convert("RGBA")
    overlay = Image.new("RGBA", img.size, (0,0,0,0))
    d = ImageDraw.Draw(overlay)
    bar_h = 280 if sub else 200
    bar_y = VH - bar_h - 60
    d.rounded_rectangle([40, bar_y, VW-40, bar_y+bar_h], radius=20,
                         fill=(10,14,39,210))
    overlay = Image.alpha_composite(img, overlay)
    d2 = ImageDraw.Draw(overlay)
    ty = bar_y + 40
    draw_text_wrap(d2, title, VW//2, ty + (F_BIG.size//2), F_BIG, (255,220,60))
    if sub:
        draw_text_wrap(d2, sub, VW//2, ty + F_BIG.size + 60, F_MED, (180,200,255))
    return overlay.convert("RGB")

def overlay_caption(frame_img, text, progress=1.0):
    """Slide-up caption at bottom."""
    img = frame_img.copy().convert("RGBA")
    ov  = Image.new("RGBA", img.size, (0,0,0,0))
    d   = ImageDraw.Draw(ov)
    bar_h = 180
    slide = int((1.0 - progress) * bar_h)
    by = VH - bar_h - 30 + slide
    d.rounded_rectangle([30, by, VW-30, by+bar_h], radius=16, fill=(0,0,0,190))
    draw_text_wrap(d, text, VW//2, by + bar_h//2, F_MED, (240,240,255))
    return Image.alpha_composite(img, ov).convert("RGB")

# ── build screenshot-based frame sequences ──────────────────────────────
def build_scene(shot_name, caption, audio_path, fps=24,
                kb_from=1.0, kb_to=1.12,
                kb_from_xy=(0.5,0.5), kb_to_xy=(0.5,0.5),
                title=None, title_sub=None, dark_overlay=0.0):
    audio = AudioFileClip(str(audio_path))
    dur   = audio.duration + 0.3

    shot = load_game_shot(shot_name)
    if shot is None:
        # fallback: dark frame
        shot = Image.new("RGB", (VW, VH), (10,14,39))

    # fit shot to 9:16
    sw, sh = shot.size
    scale = max(VW/sw, VH/sh)
    new_w, new_h = int(sw*scale)+2, int(sh*scale)+2
    shot = shot.resize((new_w, new_h), Image.LANCZOS)
    # crop center
    ox = (new_w - VW)//2; oy = (new_h - VH)//2
    shot = shot.crop((ox, oy, ox+VW, oy+VH))

    if dark_overlay > 0:
        dark = Image.new("RGB", shot.size, (0,0,0))
        shot = Image.blend(shot, dark, dark_overlay)

    frames = make_kenburns_frames(shot, dur, fps,
                                   kb_from, kb_to,
                                   kb_from_xy, kb_to_xy)

    n = len(frames)
    result = []
    for i, f in enumerate(frames):
        t = i / max(n-1, 1)
        # caption slide-in during first 0.4s
        cap_prog = min(1.0, t / 0.15) if i > 0 else 0.0

        f2 = f.copy()
        if title:
            f2 = overlay_title_card(f2, title, title_sub)
        if caption:
            f2 = overlay_caption(f2, caption, cap_prog)
        result.append(f2)

    return frames_to_clip(result, fps, audio)

# ── MAIN ────────────────────────────────────────────────────────────────
LINES = [
    # (voice_text, caption, shot_name, kb settings)
    (
        "ねえねえ、私が作ったゲーム見てほしいんだけど！ピコ、コイン、マイナー！仮想通貨マイニングのシミュレーターだよ！",
        "🪙 PicoCoin Miner\n仮想通貨マイニングシミュレーター",
        "title",
        dict(kb_from=1.0, kb_to=1.08, kb_from_xy=(0.5,0.5), kb_to_xy=(0.5,0.52), dark_overlay=0.1)
    ),
    (
        "こんな感じでノードをドラッグして配置するんだけど、繋ぎ方で全然変わってくるのがめちゃくちゃ面白い！",
        "ノードをドラッグ＆ドロップで配置！\n繋ぎ方が超重要",
        "overview",
        dict(kb_from=0.95, kb_to=1.1, kb_from_xy=(0.3,0.5), kb_to_xy=(0.6,0.5))
    ),
    (
        "マイニングPCにCPUとか水冷とか電源を繋いでいくんだけど、パーツ増やすと熱が上がっちゃって！冷却管理が超大事なんだよね！",
        "⛏ CPU・冷却・電源を接続してスペックアップ\n熱管理が鍵！",
        "overview",
        dict(kb_from=1.1, kb_to=1.2, kb_from_xy=(0.15,0.45), kb_to_xy=(0.15,0.5))
    ),
    (
        "ライトニングネットワークっていうのがあって、お金を入金するとルーティング手数料が自動で貯まるの！見て、YOUって書いてある私のノードが光ってる！かわいくない？",
        "⚡ Lightning Network\n入金するだけで手数料収入！",
        "ln_zoom",
        dict(kb_from=1.0, kb_to=1.06, kb_from_xy=(0.5,0.45), kb_to_xy=(0.5,0.5))
    ),
    (
        "でもね、サーバーがウイルスに感染することがあって、ドクロが出てきたらLN残高の10%が盗まれちゃう！ドキドキするでしょ！",
        "🦠 ウイルス感染！\nLN残高の10%が盗まれる危機！",
        "virus",
        dict(kb_from=1.0, kb_to=1.1, kb_from_xy=(0.5,0.4), kb_to_xy=(0.5,0.5), dark_overlay=0.05)
    ),
    (
        "研究ツリーで水冷とかAIマイニングとか解放していくと、どんどん強くなれるよ！どれを先に研究するか悩むのが楽しいんだよね！",
        "🔬 研究ツリーで強化！\n解放するほど収益アップ",
        "research",
        dict(kb_from=0.98, kb_to=1.08, kb_from_xy=(0.4,0.45), kb_to_xy=(0.55,0.5))
    ),
    (
        "ぜひ遊んでみてね！めっちゃ楽しいから！コメントで感想教えてくれると嬉しいな！",
        "🎮 今すぐプレイしてみて！\nコメントお待ちしてます！",
        "overview",
        dict(kb_from=1.05, kb_to=1.0, kb_from_xy=(0.5,0.5), kb_to_xy=(0.5,0.5))
    ),
]

def main():
    print("=== PicoCoin Promo Video v2 ===")

    # 1. Generate all TTS audio
    print("\n[1/3] Generating ElevenLabs audio...")
    audio_paths = []
    for i, (voice_text, caption, shot, _) in enumerate(LINES):
        ap = TMP / f"v2_audio_{i:02d}.mp3"
        tts(voice_text, ap)
        audio_paths.append(ap)
        time.sleep(0.3)  # rate limit

    # 2. Build video clips
    print("\n[2/3] Building video clips...")
    clips = []
    for i, (voice_text, caption, shot, kw) in enumerate(LINES):
        print(f"  Scene {i+1}/{len(LINES)}: {shot}")
        clip = build_scene(
            shot_name=shot,
            caption=caption,
            audio_path=audio_paths[i],
            fps=24,
            **kw
        )
        clips.append(clip)

    # 3. Render
    print("\n[3/3] Rendering final video...")
    final = concatenate_videoclips(clips, method="compose")
    out = BASE / "picocoin_promo_v2.mp4"
    final.write_videofile(
        str(out), fps=24,
        codec="libx264", audio_codec="aac",
        ffmpeg_params=["-crf","18","-preset","fast"],
        logger=None
    )
    print(f"\nDone! -> {out}  ({out.stat().st_size//1024}KB)")

if __name__ == "__main__":
    main()
