# 田舎の 遊びの 小物を 描き起こす。
#
# 色は 木立ちの 20色に そろえる（ほかの 絵と 同じ たな卸し）。
# **props.png とは 別の 紙**に する。あちらの 番号を ずらすと 場面の 組み立てが 狂う。
#
# 1コマ 32x32 の 6列 x 1行
#   0=ささぶね  1=うき  2=水きりの 石  3=はな  4=さかな  5=えだ
# 走らせかた: python unity/ArtSource/make_play.py
import os

from PIL import Image, ImageDraw

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Art", "Sprites", "play.png")

DARK  = (0x25, 0x22, 0x2e)
BR_DK = (0x40, 0x24, 0x18)
BR_MD = (0x6b, 0x45, 0x22)
BR_LT = (0xab, 0x7a, 0x1e)
G_DK  = (0x11, 0x4d, 0x3c)
G_MD  = (0x36, 0x65, 0x12)
G_LT  = (0x6d, 0xb9, 0x27)
OLIVE = (0x9a, 0x92, 0x3a)
PALE  = (0xd3, 0xc6, 0x3b)
CREAM = (0xe8, 0xe6, 0xd0)
RED   = (0xb4, 0x3a, 0x2e)
YELL  = (0xfa, 0xeb, 0x54)
BLUE  = (0x3a, 0x6b, 0x9a)
SLATE = (0x5a, 0x5f, 0x66)

CELL = 32


def mix(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))


def cell():
    return Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))


def ell(d, box, fill, outline=DARK):
    d.ellipse(box, fill=(fill + (255,)) if fill else None,
              outline=(outline + (255,)) if outline else None)


def poly(d, pts, fill, outline=DARK):
    d.polygon(pts, fill=fill + (255,), outline=(outline + (255,)) if outline else None)


# 0 ささぶね（笹の 葉を 裂いて 舟に した もの。ま横から）
def sasabune():
    im = cell(); d = ImageDraw.Draw(im)
    # 葉を そらせた 舟ばら。両はしが つんと 上がるのが ささぶねらしさ
    poly(d, [(3, 20), (10, 16), (22, 16), (29, 20), (22, 23), (10, 23)], mix(G_MD, G_LT, 0.45))
    # 葉すじ
    d.line((6, 20, 26, 20), fill=mix(G_DK, G_LT, 0.30) + (255,))
    for x in range(9, 25, 4):
        d.line((x, 17, x + 2, 22), fill=mix(G_DK, G_MD, 0.5) + (255,))
    # 帆に なる ように 裂いて 立てた ところ
    poly(d, [(15, 6), (20, 15), (14, 15)], mix(G_LT, PALE, 0.35))
    d.line((15, 6, 15, 16), fill=G_DK + (255,))
    return im


# 1 うき（釣りの うき。赤白）
def uki():
    im = cell(); d = ImageDraw.Draw(im)
    d.line((16, 2, 16, 10), fill=DARK + (255,))               # アンテナ
    ell(d, (11, 9, 21, 24), CREAM)                            # 胴
    d.rectangle((12, 10, 20, 15), fill=RED + (255,))          # 赤い ところ
    ell(d, (11, 9, 21, 24), None, DARK)
    d.line((16, 24, 16, 29), fill=DARK + (255,))              # 糸
    return im


# 2 水きりの 石（ひらたい ほど よく はねる）
def ishi():
    im = cell(); d = ImageDraw.Draw(im)
    ell(d, (4, 13, 28, 21), mix(SLATE, CREAM, 0.22))
    ell(d, (8, 14, 18, 18), mix(SLATE, CREAM, 0.48), outline=None)   # 照り
    d.line((7, 19, 24, 19), fill=mix(SLATE, DARK, 0.35) + (255,))
    return im


# 3 はな（つゆくさ・あかまんま のような 野の 花。色水と 押し花に つかう）
def hana():
    im = cell(); d = ImageDraw.Draw(im)
    d.line((16, 16, 15, 29), fill=G_MD + (255,))              # くき
    poly(d, [(10, 18), (14, 15), (13, 21)], G_DK)             # は
    poly(d, [(22, 20), (18, 17), (19, 23)], G_DK)
    for (x, y) in ((16, 8), (11, 11), (21, 11), (13, 15), (19, 15)):
        ell(d, (x - 3, y - 3, x + 3, y + 3), mix(BLUE, CREAM, 0.30))
    ell(d, (14, 10, 18, 14), YELL, outline=None)              # しべ
    return im


# 4 さかな（おいかわ・はやの たぐい。川の 小もの）
def sakana():
    im = cell(); d = ImageDraw.Draw(im)
    poly(d, [(6, 16), (14, 11), (23, 13), (26, 16), (23, 20), (14, 22)], mix(SLATE, CREAM, 0.40))
    poly(d, [(3, 11), (7, 16), (3, 21)], mix(SLATE, CREAM, 0.20))   # お
    poly(d, [(15, 12), (20, 8), (21, 13)], mix(SLATE, CREAM, 0.15)) # せびれ
    poly(d, [(14, 20), (18, 24), (19, 20)], mix(SLATE, CREAM, 0.15))# はらびれ
    d.line((8, 17, 24, 17), fill=mix(BLUE, CREAM, 0.35) + (255,))   # わき腹の すじ
    d.point((24, 15), fill=DARK + (255,))                     # め
    return im


# 5 えだ（秘密基地の ざいりょう）
def eda():
    im = cell(); d = ImageDraw.Draw(im)
    d.line((3, 26, 27, 8), fill=BR_MD + (255,), width=3)
    d.line((3, 26, 27, 8), fill=mix(BR_MD, DARK, 0.35) + (255,))
    d.line((14, 17, 20, 20), fill=BR_DK + (255,), width=2)    # 小えだ
    d.line((19, 12, 24, 15), fill=BR_DK + (255,), width=2)
    poly(d, [(20, 20), (25, 22), (21, 24)], G_DK)             # のこった 葉
    return im


if __name__ == "__main__":
    parts = [sasabune(), uki(), ishi(), hana(), sakana(), eda()]
    sheet = Image.new("RGBA", (CELL * len(parts), CELL), (0, 0, 0, 0))
    for i, p in enumerate(parts):
        sheet.paste(p, (i * CELL, 0))
    sheet.save(OUT)
    print("->", OUT, sheet.size)
