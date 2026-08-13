# 部屋の 小物を 描き起こす。
#
# 畳・板・壁・草木・虫は ぜんぶ 木立ちの 20色に そろえ なおしたのに、
# ここだけ 前の 32px の まま 浮いていた。同じ 色で 描き直す。
#
# 1コマ 32x32 の 6列 x 1行（ならびは 前と 同じ。番号を 変えると 場面の 組み立てが 狂う）
#   0=むぎわらぼうし  1=すいか  2=ざぶとん  3=かびん  4=かとりせんこう  5=うちわ
# 走らせかた: python unity/ArtSource/make_props.py
import os

from PIL import Image, ImageDraw

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Art", "Sprites", "props.png")

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
RED   = (0xb4, 0x3a, 0x2e)      # すいかの 実・ざぶとんの 布
YELL  = (0xfa, 0xeb, 0x54)

CELL = 32
N = 6


def mix(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))


def cell():
    return Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))


def ell(d, box, fill, outline=DARK):
    d.ellipse(box, fill=fill + (255,), outline=(outline + (255,)) if outline else None)


def rect(d, box, fill, outline=DARK):
    d.rectangle(box, fill=fill + (255,), outline=outline + (255,))


# 0 むぎわらぼうし（畳に 置いてある）
def boushi():
    im = cell(); d = ImageDraw.Draw(im)
    ell(d, (2, 11, 29, 27), mix(OLIVE, PALE, 0.45))          # つば
    ell(d, (9, 6, 22, 20), mix(OLIVE, PALE, 0.20))           # 山
    d.line((10, 17, 21, 17), fill=RED + (255,))              # リボン
    d.line((10, 18, 21, 18), fill=mix(RED, DARK, 0.35) + (255,))
    for x in range(4, 28, 3):                                 # 編み目
        d.point((x, 22), fill=mix(OLIVE, DARK, 0.30) + (255,))
    return im


# 1 すいか（切ったの）
def suika():
    im = cell(); d = ImageDraw.Draw(im)
    d.polygon([(4, 25), (28, 25), (16, 7)], fill=RED + (255,), outline=DARK + (255,))
    d.polygon([(4, 25), (28, 25), (16, 21)], fill=CREAM + (255,), outline=DARK + (255,))
    d.line((4, 26, 28, 26), fill=G_DK + (255,))              # 皮
    d.line((4, 27, 28, 27), fill=G_MD + (255,))
    for p in ((13, 15), (18, 16), (15, 12), (20, 19)):        # たね
        d.point(p, fill=DARK + (255,))
    return im


# 2 ざぶとん（ゆかに 寝かせる）
def zabuton():
    im = cell(); d = ImageDraw.Draw(im)
    rect(d, (3, 7, 28, 24), RED)
    rect(d, (5, 9, 26, 22), mix(RED, (255, 255, 255), 0.16), outline=mix(RED, DARK, 0.4))
    for p in ((3, 7), (28, 7), (3, 24), (28, 24)):            # かどの ふさ
        d.point(p, fill=PALE + (255,))
    d.point((16, 15), fill=PALE + (255,))                     # まん中の とじ
    return im


# 3 かびん（花を いけて ある）
def kabin():
    im = cell(); d = ImageDraw.Draw(im)
    # 花
    for (x, y, c) in ((11, 6, CREAM), (19, 4, YELL), (15, 8, CREAM)):
        ell(d, (x - 3, y - 3, x + 3, y + 3), c)
    d.line((13, 9, 15, 17), fill=G_MD + (255,))
    d.line((19, 7, 16, 17), fill=G_MD + (255,))
    # うつわ
    d.polygon([(12, 17), (20, 17), (22, 27), (10, 27)], fill=mix(G_DK, CREAM, 0.35) + (255,),
              outline=DARK + (255,))
    d.line((12, 20, 20, 20), fill=mix(G_DK, DARK, 0.3) + (255,))
    return im


# 4 かとりせんこう（皿に のせた うずまき。けむりは 別に 出す）
def senko():
    im = cell(); d = ImageDraw.Draw(im)
    ell(d, (6, 18, 26, 28), mix(G_DK, CREAM, 0.25))           # 皿
    # うずまき
    d.arc((10, 14, 23, 24), 0, 360, fill=mix(G_DK, DARK, 0.2) + (255,))
    d.arc((13, 16, 20, 22), 0, 360, fill=mix(G_DK, DARK, 0.2) + (255,))
    d.point((17, 19), fill=RED + (255,))                      # 火
    return im


# 5 うちわ（ゆかに 寝かせる）
def uchiwa():
    im = cell(); d = ImageDraw.Draw(im)
    ell(d, (4, 3, 27, 22), CREAM)
    for x in range(7, 25, 3):                                 # 骨
        d.line((15, 21, x, 6), fill=mix(BR_LT, CREAM, 0.35) + (255,))
    ell(d, (11, 8, 20, 16), mix(G_DK, G_LT, 0.35), outline=None)   # 絵がら
    rect(d, (14, 21, 17, 29), BR_MD)                          # 柄
    return im


if __name__ == "__main__":
    parts = [boushi(), suika(), zabuton(), kabin(), senko(), uchiwa()]
    sheet = Image.new("RGBA", (CELL * N, CELL), (0, 0, 0, 0))
    for i, p in enumerate(parts):
        sheet.paste(p, (i * CELL, 0))
    sheet.save(OUT)
    print("->", OUT, sheet.size)
