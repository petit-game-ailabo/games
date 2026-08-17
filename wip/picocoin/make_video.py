"""
PicoCoin Miner - short promo video generator
Japanese narration via gTTS, frames via Pillow, combined with moviepy
"""
import os, sys, textwrap
from PIL import Image, ImageDraw, ImageFont
from gtts import gTTS
from moviepy import AudioFileClip, ImageClip, concatenate_videoclips

OUT_DIR = os.path.join(os.path.dirname(__file__), "_video_tmp")
os.makedirs(OUT_DIR, exist_ok=True)

W, H = 1080, 1920   # 9:16 shorts

# ---------- colour palette ----------
BG      = (10, 14, 39)
ACCENT  = (74, 106, 255)
GOLD    = (255, 215, 0)
GREEN   = (68, 255, 136)
PURPLE  = (153, 68, 221)
WHITE   = (255, 255, 255)
GRAY    = (140, 150, 180)
RED     = (255, 80, 80)

# ---------- font helpers ----------
def _font(size, bold=False):
    candidates = [
        r"C:\Windows\Fonts\YuGothB.ttc",   # Yu Gothic Bold
        r"C:\Windows\Fonts\YuGothM.ttc",   # Yu Gothic Medium
        r"C:\Windows\Fonts\meiryo.ttc",
        r"C:\Windows\Fonts\msgothic.ttc",
        r"C:\Windows\Fonts\arial.ttf",
    ]
    for p in candidates:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                continue
    return ImageFont.load_default()

F_LARGE  = _font(90,  bold=True)
F_MED    = _font(60)
F_SMALL  = _font(44)
F_TINY   = _font(36)

def draw_text_center(draw, text, y, font, color=WHITE, max_width=980):
    """Word-wrap and centre text block."""
    # rough wrap
    dummy_img = Image.new("RGB", (1, 1))
    dd = ImageDraw.Draw(dummy_img)
    lines = []
    words = text.split()
    line = ""
    for w in words:
        test = (line + w).strip()
        bb = dd.textbbox((0, 0), test, font=font)
        if bb[2] - bb[0] > max_width and line:
            lines.append(line.strip())
            line = w + " "
        else:
            line = test + " "
    if line.strip():
        lines.append(line.strip())

    total_h = sum((draw.textbbox((0,0), l, font=font)[3] - draw.textbbox((0,0), l, font=font)[1]) + 12
                  for l in lines)
    cur_y = y - total_h // 2
    for l in lines:
        bb = draw.textbbox((0, 0), l, font=font)
        lw = bb[2] - bb[0]
        draw.text(((W - lw) // 2, cur_y), l, font=font, fill=color)
        cur_y += (bb[3] - bb[1]) + 14

def draw_node(draw, x, y, w, h, emoji, name, title_color, radius=14):
    # shadow
    draw.rounded_rectangle([x+4, y+4, x+w+4, y+h+4], radius=radius, fill=(0,0,0,120))
    # body
    draw.rounded_rectangle([x, y, x+w, y+h], radius=radius, fill=(20,25,55))
    draw.rounded_rectangle([x, y, x+w, y+int(h*0.38)], radius=radius, fill=title_color)
    # title cover bottom corners
    draw.rectangle([x, y+int(h*0.25), x+w, y+int(h*0.38)], fill=title_color)
    # emoji
    emo_f = _font(40)
    draw.text((x+12, y+8), emoji, font=emo_f, fill=WHITE)
    name_f = _font(28, bold=True)
    draw.text((x+60, y+14), name, font=name_f, fill=WHITE)

def draw_arrow(draw, x1, y1, x2, y2, color=ACCENT, width=4):
    draw.line([(x1,y1),(x2,y2)], fill=color, width=width)
    # arrowhead
    import math
    angle = math.atan2(y2-y1, x2-x1)
    size = 18
    for a in (angle+2.5, angle-2.5):
        ax = x2 - size*math.cos(a)
        ay = y2 - size*math.sin(a)
        draw.line([(x2,y2),(int(ax),int(ay))], fill=color, width=width)

def new_frame(bg=BG):
    img = Image.new("RGB", (W, H), bg)
    d = ImageDraw.Draw(img)
    return img, d

def gradient_bg():
    img = Image.new("RGB", (W, H))
    for y in range(H):
        t = y / H
        r = int(10  + (20-10)*t)
        g = int(14  + (8-14)*t)
        b = int(39  + (60-39)*t)
        for x in range(W):
            img.putpixel((x, y), (r, g, b))
    return img

# =====================================================================
# SCENES
# =====================================================================

def scene_title():
    img = gradient_bg()
    d = ImageDraw.Draw(img)
    # glow circle
    for r in range(220, 0, -20):
        alpha = int(30 * (220-r)/220)
        d.ellipse([W//2-r, H//2-320-r, W//2+r, H//2-320+r],
                  fill=(74, 106, 255))
    d.text((W//2 - 60, H//2 - 400), "🪙", font=_font(120), fill=GOLD)
    draw_text_center(d, "PicoCoin Miner", H//2 - 160, F_LARGE, GOLD)
    draw_text_center(d, "仮想通貨マイニング", H//2 - 40, F_MED, ACCENT)
    draw_text_center(d, "シミュレーター", H//2 + 60, F_MED, ACCENT)
    draw_text_center(d, "ノードを繋いで\nピココインを稼ごう！", H//2 + 250, F_SMALL, GRAY)
    return img

def scene_mining():
    img = gradient_bg()
    d = ImageDraw.Draw(img)
    draw_text_center(d, "⛏ マイニング", 180, F_LARGE, GREEN)
    draw_text_center(d, "マイニングPCに\nCPU・電源・冷却を接続", 340, F_MED, WHITE)

    nw, nh = 280, 100
    cx = W//2
    # PC node
    draw_node(d, cx-nw//2, 460, nw, nh, "🖥", "マイニングPC", (34,68,140))
    # Parts
    draw_node(d, cx+180, 370, 200, 80, "🧠", "CPU Mk2",    (34,90,140))
    draw_node(d, cx+180, 470, 200, 80, "💧", "水冷",        (34,85,204))
    draw_node(d, cx+180, 570, 200, 80, "🔌", "電源 Mk2",   (180,40,40))
    # arrows
    draw_arrow(d, cx+380, 410, cx+nw//2+280, 490, ACCENT)
    draw_arrow(d, cx+380, 510, cx+nw//2+280, 505, ACCENT)
    draw_arrow(d, cx+380, 610, cx+nw//2+280, 520, ACCENT)

    draw_text_center(d, "パーツが増えるほど\nマイニング速度アップ！", 780, F_MED, GOLD)

    # hash rate badge
    d.rounded_rectangle([cx-160, 860, cx+160, 940], radius=20, fill=(20,200,80))
    draw_text_center(d, "⚡ 9.0 H/s", 900, F_MED, WHITE)
    return img

def scene_heat():
    img = gradient_bg()
    d = ImageDraw.Draw(img)
    draw_text_center(d, "🌡 熱管理", 180, F_LARGE, RED)
    draw_text_center(d, "パーツを増やすと\n熱が上がる！", 340, F_MED, WHITE)

    cx = W//2
    # temp gauge
    gauge_x, gauge_y, gauge_w, gauge_h = cx-250, 470, 500, 50
    d.rounded_rectangle([gauge_x, gauge_y, gauge_x+gauge_w, gauge_y+gauge_h],
                         radius=10, fill=(40,40,60))
    fill_w = int(gauge_w * 0.82)
    d.rounded_rectangle([gauge_x, gauge_y, gauge_x+fill_w, gauge_y+gauge_h],
                         radius=10, fill=(220,80,20))
    draw_text_center(d, "🔥 82% 過熱危険！", 498, F_SMALL, RED)

    draw_text_center(d, "冷却ファン・水冷・\nカスタム水冷で対策", 650, F_MED, ACCENT)

    draw_node(d, cx-320, 750, 200, 80, "🌀", "空冷ファン",    (40,120,140))
    draw_node(d, cx- 80, 750, 180, 80, "💧", "水冷",          (34,85,204))
    draw_node(d, cx+120, 750, 210, 80, "🔵", "カスタム水冷",  (20,60,180))

    draw_text_center(d, "熱をコントロールして\n安定稼働を維持！", 1000, F_MED, GREEN)
    return img

def scene_network():
    img = gradient_bg()
    d = ImageDraw.Draw(img)
    draw_text_center(d, "⚡ Lightning Network", 160, F_LARGE, GOLD)
    draw_text_center(d, "入金してルーティング手数料を稼ごう", 300, F_SMALL, WHITE)

    cx = W//2
    # mini LN graph suggestion
    import math
    pts = [(cx + int(90*math.cos(a*math.pi/5)), 560 + int(70*math.sin(a*math.pi/5)))
           for a in range(10)]
    for i in range(len(pts)):
        for j in range(i+1, len(pts)):
            if abs(i-j) in (1, 2, 5):
                d.line([pts[i], pts[j]], fill=(30,50,100), width=2)
    for i, p in enumerate(pts):
        col = GOLD if i == 4 else (60,80,130)
        r = 14 if i == 4 else 9
        d.ellipse([p[0]-r, p[1]-r, p[0]+r, p[1]+r], fill=col)
    draw_text_center(d, "YOU", 610, _font(30), GOLD)

    draw_text_center(d, "1000₱ 入金\n→ 1₱/秒 の手数料収入", 760, F_MED, GOLD)
    draw_text_center(d, "サーバーを複数接続で\n冗長化・ウイルス対策も！", 960, F_SMALL, GRAY)

    draw_node(d, 60,  1100, 220, 80, "🖥", "サーバー",         (34,68,100))
    draw_node(d, 310, 1100, 220, 80, "⚡", "Lightning",        (180,100,0))
    draw_node(d, 560, 1100, 220, 80, "🖥", "サーバー(冗長)",   (40,80,80))
    draw_arrow(d, 280, 1140, 310, 1140, ACCENT)
    draw_arrow(d, 530, 1140, 560, 1140, PURPLE)
    return img

def scene_research():
    img = gradient_bg()
    d = ImageDraw.Draw(img)
    draw_text_center(d, "🔬 研究ツリー", 180, F_LARGE, PURPLE)
    draw_text_center(d, "研究ポイントを使って\n新技術を解放！", 340, F_MED, WHITE)

    items = [
        ("⛏", "マイニング基礎",  (40,80,40),   200, 520),
        ("🧠", "高性能CPU",      (40,80,120),  200, 640),
        ("💧", "水冷技術",       (30,70,180),  200, 760),
        ("☀", "太陽光発電",     (140,100,20), 200, 880),
        ("🖥", "高性能マイニングPC",(30,100,50),200,1000),
        ("⚡", "LN基礎",         (140,70,20),  580, 520),
        ("📡", "チャネル最適化",  (60,80,120),  580, 640),
        ("🌐", "分散型ルーティング",(40,60,140), 580, 760),
        ("🛡", "ウイルスバスター", (30,100,60),  580, 880),
    ]
    for emoji, name, col, x, y in items:
        nw = 380 if len(name) > 7 else 340
        draw_node(d, x, y, nw, 72, emoji, name, col)

    draw_text_center(d, "解放するほど\n収益が雪だるま式に！", 1200, F_MED, GOLD)
    return img

def scene_virus():
    img = gradient_bg()
    d = ImageDraw.Draw(img)
    draw_text_center(d, "🦠 ウイルス脅威", 180, F_LARGE, RED)
    draw_text_center(d, "サーバーがウイルスに感染！", 320, F_MED, WHITE)

    cx = W//2
    # infected server
    d.rounded_rectangle([cx-160, 430, cx+160, 600], radius=14, fill=(60,10,10))
    d.rounded_rectangle([cx-160, 430, cx+160, 480], radius=14, fill=(140,20,20))
    d.text((cx-120, 438), "🖥 サーバー", font=_font(38), fill=WHITE)
    d.text((cx-50, 510), "💀", font=_font(60), fill=RED)

    draw_text_center(d, "感染中はLN残高の\n10%が15秒ごとに盗まれる！", 700, F_MED, RED)

    draw_text_center(d, "対策オプション", 880, F_SMALL, GOLD)

    ops = [
        ("🛡", "ウイルスバスター\n自動駆除 60→20秒"),
        ("🌐", "冗長サーバー構成\n感染中も稼働継続"),
    ]
    for i, (em, txt) in enumerate(ops):
        bx = 80 + i*500
        d.rounded_rectangle([bx, 930, bx+420, 1130], radius=16, fill=(20,40,80))
        d.text((bx+20, 950), em, font=_font(60), fill=WHITE)
        draw_text_center(d, txt, 1060, F_TINY, WHITE)
    return img

def scene_cta():
    img = gradient_bg()
    d = ImageDraw.Draw(img)
    # big glow
    for r in range(400, 0, -30):
        d.ellipse([W//2-r, H//2-r-200, W//2+r, H//2+r-200],
                  fill=(74, 106, int(255 * r / 400)))
    d.text((W//2 - 70, H//2 - 400), "🪙", font=_font(140), fill=GOLD)
    draw_text_center(d, "PicoCoin Miner", H//2 - 170, F_LARGE, GOLD)
    draw_text_center(d, "ノードを繋いで", H//2 + 30, F_MED, WHITE)
    draw_text_center(d, "マイニング帝国を築け！", H//2 + 120, F_MED, WHITE)
    draw_text_center(d, "🎮 今すぐプレイ", H//2 + 320, F_MED, GREEN)
    # hashtags
    draw_text_center(d, "#ゲーム  #マイニング  #シミュレーション",
                     H//2 + 500, F_TINY, GRAY)
    return img

# =====================================================================
# NARRATION
# =====================================================================
NARRATIONS = [
    ("ピココインマイナー。ノードを繋いでマイニングを始めよう。", 3.5),
    ("マイニングPCにCPU、電源、冷却を接続。パーツが増えるほどハッシュレートが上がります。", 5.0),
    ("でも注意！パーツを増やすと熱が上昇。冷却システムで温度管理が鍵です。", 4.5),
    ("ライトニングネットワークに入金してルーティング手数料を稼ごう。冗長構成でウイルス対策も完璧に。", 5.5),
    ("研究ツリーで新技術を解放。水冷、高性能CPU、分散型ルーティングなど豊富な研究が待っています。", 5.5),
    ("ウイルスに感染したらLN残高が盗まれる！ウイルスバスターか冗長サーバーで守ろう。", 5.0),
    ("PicoCoin Miner。ノードを繋いでマイニング帝国を築け！", 4.0),
]

SCENES = [
    scene_title,
    scene_mining,
    scene_heat,
    scene_network,
    scene_research,
    scene_virus,
    scene_cta,
]

def main():
    print("Generating narration audio...")
    audio_paths = []
    for i, (text, _) in enumerate(NARRATIONS):
        ap = os.path.join(OUT_DIR, f"audio_{i:02d}.mp3")
        tts = gTTS(text=text, lang="ja", slow=False)
        tts.save(ap)
        audio_paths.append(ap)
        print(f"  [{i+1}/{len(NARRATIONS)}] {text[:30]}...")

    print("Generating video frames...")
    clips = []
    for i, (scene_fn, audio_path) in enumerate(zip(SCENES, audio_paths)):
        print(f"  Scene {i+1}/{len(SCENES)}: {scene_fn.__name__}")
        img = scene_fn()
        img_path = os.path.join(OUT_DIR, f"frame_{i:02d}.png")
        img.save(img_path)

        audio_clip = AudioFileClip(audio_path)
        duration = audio_clip.duration + 0.3
        img_clip = ImageClip(img_path, duration=duration)
        img_clip = img_clip.with_audio(audio_clip)
        clips.append(img_clip)

    print("Combining into final video...")
    final = concatenate_videoclips(clips, method="compose")
    out_path = os.path.join(os.path.dirname(__file__), "picocoin_promo.mp4")
    final.write_videofile(out_path, fps=24, codec="libx264", audio_codec="aac",
                          logger=None)
    print(f"\nDone! Saved to: {out_path}")

if __name__ == "__main__":
    main()
