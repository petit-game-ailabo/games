# Marisa's face and hair, drawn (2026-09-05).
#
# WHY: the first 3D pass built the face and hair out of spheres and it looked like a toy.
# Anime 3D almost never models the face -- eyes, brows and mouth are a PAINTED TEXTURE on a
# smooth head (Guilty Gear Xrd, VRoid, MMD all do this), and hair is drawn on cards.
# So the parts that carry the character get drawn here, not modelled.
#
#   marisa_kao.png        face: eyes, brows, mouth, blush (alpha cut)
#   marisa_maegami.png    bangs card
#   marisa_yokogami.png   one side lock
#   marisa_ushirogami.png back hair mass
#
# run: python unity/ArtSource/make_marisa_tex.py
import os, math
from PIL import Image, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "Assets", "Art", "Textures", "marisa")
os.makedirs(OUT, exist_ok=True)

S = 4                                   # supersample

KAMI      = (238, 202,  92)             # hair
KAMI_KAGE = (206, 166,  60)
KAMI_SEN  = (150, 112,  34)
ME_SHIRO  = (252, 250, 246)
ME_IRO    = (232, 176,  54)             # golden eyes
ME_KURO   = ( 58,  40,  28)
SEN       = ( 46,  32,  26)             # line
HOO       = (246, 168, 150, 90)         # blush


def save(im, name, size):
    im = im.resize((size, size), Image.LANCZOS)
    a = im.split()[3].point(lambda v: 0 if v < 110 else 255)
    im.putalpha(a)
    im.save(os.path.join(OUT, name))
    print(name, size)


# ---------------------------------------------------------------- 顔
def kao(size=256):
    W = size * S
    im = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    u = W / 256.0                        # 256 を もとに 描く

    def me(cx, cy, mukai):
        """anime eye. mukai = -1 left / +1 right (inner corner leans that way)"""
        w, h = 40 * u, 46 * u
        # 白目
        d.ellipse([cx - w, cy - h, cx + w, cy + h], fill=ME_SHIRO)
        # 虹彩（下へ すこし ずらす＝見おろされる ふせ角の カメラで 目が 合う）
        r = 30 * u
        d.ellipse([cx - r, cy - r + 4 * u, cx + r, cy + r + 4 * u], fill=ME_IRO)
        d.ellipse([cx - r, cy - r + 4 * u, cx + r, cy + r + 4 * u],
                  outline=(180, 126, 30), width=int(2.5 * u))
        # ひとみ
        pr = 13 * u
        d.ellipse([cx - pr, cy - pr + 5 * u, cx + pr, cy + pr + 5 * u], fill=ME_KURO)
        # 光（左上に 大きく・右下に 小さく）
        hr = 11 * u
        d.ellipse([cx - hr - 6 * u, cy - hr - 6 * u, cx + hr - 6 * u, cy + hr - 6 * u],
                  fill=(255, 255, 255))
        hr2 = 4 * u
        d.ellipse([cx - hr2 + 10 * u, cy - hr2 + 12 * u, cx + hr2 + 10 * u, cy + hr2 + 12 * u],
                  fill=(255, 255, 255, 190))
        # まぶた（上の 太い 線。目の 印象は ほぼ これで 決まる）
        d.arc([cx - w - 3 * u, cy - h - 6 * u, cx + w + 3 * u, cy + h - 2 * u],
              start=195, end=345, fill=SEN, width=int(9 * u))
        # まつ毛（外がわの はねあげ）
        d.line([(cx + mukai * (w + 2 * u), cy - h * 0.45),
                (cx + mukai * (w + 13 * u), cy - h * 0.95)], fill=SEN, width=int(6 * u))
        # 下まぶた（細く）
        d.arc([cx - w, cy - h + 8 * u, cx + w, cy + h + 4 * u],
              start=20, end=160, fill=SEN, width=int(3 * u))

    me(76 * u, 112 * u, -1)
    me(180 * u, 112 * u, +1)

    # まゆ
    for cx in (76 * u, 180 * u):
        d.arc([cx - 32 * u, 34 * u, cx + 32 * u, 86 * u],
              start=200, end=340, fill=KAMI_SEN, width=int(7 * u))

    # 口（小さく 開いた 形）
    d.arc([114 * u, 176 * u, 142 * u, 196 * u], start=10, end=170, fill=SEN, width=int(5 * u))

    # ★ほおの ぼかしは やめた。アルファを 0/1 に 切る ので、ふちが 点々の 汚れに なった
    save(im, "marisa_kao.png", size)


def kao_tojiru(size=256):
    """まばたき。目を 弧 1本に する（テクスチャの 差しかえで やる＝形は さわらない）"""
    W = size * S
    im = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    u = W / 256.0
    for cx, sg in ((76 * u, -1), (180 * u, +1)):
        d.arc([cx - 40 * u, 86 * u, cx + 40 * u, 146 * u],
              start=200, end=340, fill=SEN, width=int(10 * u))
        d.line([(cx + sg * 42 * u, 106 * u), (cx + sg * 56 * u, 92 * u)],
               fill=SEN, width=int(7 * u))
        d.arc([cx - 32 * u, 34 * u, cx + 32 * u, 86 * u],
              start=200, end=340, fill=KAMI_SEN, width=int(7 * u))
    d.arc([114 * u, 176 * u, 142 * u, 196 * u], start=10, end=170, fill=SEN, width=int(5 * u))
    save(im, "marisa_kao_tojiru.png", size)


# ---------------------------------------------------------------- 髪
def suji(d, x0, y0, x1, y1, w, col):
    d.line([(x0, y0), (x1, y1)], fill=col, width=int(w))


def maegami(size=256):
    """前髪。下の へりを とがらせる（アニメの 髪は 直線の 束）"""
    W = size * S
    im = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    u = W / 256.0
    # 上は 頭に そって 丸く、下は 5つの 束に わかれる
    pts = [(6 * u, 40 * u)]
    for i in range(41):
        t = i / 40.0
        pts.append((6 * u + t * 244 * u, 40 * u - math.sin(t * math.pi) * 34 * u))
    saki = [(250 * u, 128 * u), (214 * u, 176 * u), (196 * u, 120 * u),
            (160 * u, 190 * u), (140 * u, 124 * u), (108 * u, 196 * u),
            (86 * u, 122 * u), (52 * u, 180 * u), (34 * u, 126 * u), (6 * u, 150 * u)]
    d.polygon(pts + saki, fill=KAMI)
    # 束の すじ
    for x in (40, 74, 110, 148, 186, 220):
        suji(d, x * u, 24 * u, (x + 6) * u, 132 * u, 3.5 * u, KAMI_KAGE)
    # 上の つや（アニメの 天使の わ）
    d.arc([26 * u, 6 * u, 230 * u, 78 * u], start=200, end=340,
          fill=(255, 238, 176), width=int(9 * u))
    save(im, "marisa_maegami.png", size)


def yokogami(size=256):
    """横の 髪。上が 太く 下が 細い 1本の 束"""
    W = size * S
    im = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    u = W / 256.0
    mig, hid = [], []
    for i in range(41):
        t = i / 40.0
        y = 8 * u + t * 240 * u
        w = (52 * u) * (1 - t * 0.72) * (1 + 0.12 * math.sin(t * 7))
        cx = 128 * u + math.sin(t * 2.2) * 16 * u
        mig.append((cx + w, y)); hid.append((cx - w, y))
    d.polygon(mig + hid[::-1], fill=KAMI)
    for k in (-0.45, 0.0, 0.45):
        p = [(128 * u + math.sin(i / 40.0 * 2.2) * 16 * u + k * 52 * u * (1 - i / 40.0 * 0.72),
              8 * u + i / 40.0 * 240 * u) for i in range(41)]
        d.line(p, fill=KAMI_KAGE, width=int(3.5 * u))
    save(im, "marisa_yokogami.png", size)


def ushirogami(size=256):
    """うしろの 髪。ひろい 面に すじ、すそは 波"""
    W = size * S
    im = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    u = W / 256.0
    top, bot = [], []
    for i in range(41):
        t = i / 40.0
        x = 4 * u + t * 248 * u
        top.append((x, 10 * u + math.sin(t * math.pi) * -6 * u))
        bot.append((x, 214 * u + math.sin(t * 9) * 22 * u))
    d.polygon(top + bot[::-1], fill=KAMI)
    for x in range(20, 250, 26):
        suji(d, x * u, 12 * u, (x + 10) * u, 210 * u, 4 * u, KAMI_KAGE)
    d.arc([20 * u, -30 * u, 236 * u, 70 * u], start=200, end=340,
          fill=(255, 238, 176), width=int(10 * u))
    save(im, "marisa_ushirogami.png", size)


kao()
kao_tojiru()
maegami()
yokogami()
ushirogami()
