# 山の 木を 描き足す。
#
# 調べた こと：**日本の 山が まだらに 見えるのは、戦後に 植えた スギ・ヒノキの
# 人工林（針葉樹・整然・暗い 青みどり）と、もとからの 広葉樹の 天然林（明るく 色が ばらつく）が
# 入りまじって いる から。** 人工林は 針葉樹が 9割以上、天然林は 広葉樹が 8割以上。
# → 針葉樹の 絵が 無いと この まだらは 作れない ので、ここで 描く。
#
# 1コマ 144x144 の 4列 x 2行（草木の アトラスと 同じ 決まり。32px＝1m）。
# 走らせかた: python unity/ArtSource/make_trees.py
import os
import random

from PIL import Image, ImageDraw

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Art", "Sprites", "nature2.png")

DARK  = (0x25, 0x22, 0x2e)
G_DK  = (0x11, 0x4d, 0x3c)     # 針葉樹の 暗い 青みどり
G_GY  = (0x24, 0x50, 0x22)
G_MD  = (0x36, 0x65, 0x12)
G_LT  = (0x55, 0x86, 0x19)
G_BR  = (0x6d, 0xb9, 0x27)
BR_DK = (0x40, 0x24, 0x18)
BR_LT = (0xab, 0x7a, 0x1e)
OLIVE = (0x9a, 0x92, 0x3a)
GREY  = (0x3d, 0x3f, 0x37)

CELL = 144
COLS, ROWS = 4, 2
rng = random.Random(20260815)


def mix(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))


def cell():
    return Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))


def blob(d, cx, cy, rx, ry, col):
    d.ellipse((cx - rx, cy - ry, cx + rx, cy + ry), fill=col + (255,))


# ---- 針葉樹（スギ）。細く 高く、下から 上へ すぼまる。人工林の 主役
def conifer(height_px, width_px, base, lite, seed):
    im = cell()
    d = ImageDraw.Draw(im)
    r = random.Random(seed)
    top = CELL - height_px
    trunk = mix(BR_DK, DARK, 0.35)
    # みき
    d.rectangle((CELL // 2 - 3, CELL - height_px // 5, CELL // 2 + 2, CELL - 1),
                fill=trunk + (255,))
    # 枝の 段。上ほど 短く
    layers = 13
    for i in range(layers):
        t = i / (layers - 1.0)                     # 0=下 1=上
        y = int(CELL - 6 - t * (height_px - 10))
        half = int(width_px * 0.5 * (1.0 - t) ** 0.78) + 2
        if half < 2:
            continue
        c = base if (i % 2 == 0) else mix(base, lite, 0.45)
        # 段は ぎざぎざに（まっすぐな 三角は 木に 見えない）
        pts = []
        n = max(3, half // 3)
        for k in range(n + 1):
            xx = CELL // 2 - half + int(2 * half * k / n)
            yy = y + (0 if k % 2 == 0 else -3) + r.randint(-1, 1)
            pts.append((xx, yy))
        pts.append((CELL // 2, y - 11))
        d.polygon(pts, fill=c + (255,))
    # てっぺん
    d.polygon([(CELL // 2 - 5, top + 16), (CELL // 2 + 5, top + 16), (CELL // 2, top)],
              fill=mix(base, lite, 0.3) + (255,))
    # 影ぎわ を すこし こくする
    for i in range(60):
        x = r.randint(CELL // 2 - width_px // 2, CELL // 2 + width_px // 2)
        y = r.randint(top, CELL - 1)
        if im.getpixel((x, y))[3] > 0 and r.random() < 0.5:
            im.putpixel((x, y), mix(base, DARK, 0.35) + (255,))
    return im


# ---- 枯れ木（山には かならず ある）
def deadtree():
    im = cell()
    d = ImageDraw.Draw(im)
    c = mix(BR_DK, GREY, 0.45)
    d.rectangle((CELL // 2 - 4, CELL - 96, CELL // 2 + 3, CELL - 1), fill=c + (255,))
    for (x0, y0, x1, y1) in ((0, 30, -26, 8), (0, 44, 24, 22), (0, 62, -20, 46), (0, 20, 16, 2)):
        d.line((CELL // 2 + x0, CELL - 96 + y0, CELL // 2 + x1, CELL - 96 + y1),
               fill=c + (255,), width=3)
    return im


# ---- ささやぶ（山の 下ばえ。ここを かき分けて 進む）
def sasa():
    im = cell()
    d = ImageDraw.Draw(im)
    for i in range(90):
        x = CELL // 2 + rng.randint(-46, 46)
        h = rng.randint(24, 62)
        lean = rng.randint(-10, 10)
        c = [G_MD, G_LT, G_GY][rng.randrange(3)]
        d.line((x, CELL - 2, x + lean, CELL - 2 - h), fill=c + (255,), width=2)
        d.line((x + lean, CELL - 2 - h, x + lean + (3 if lean > 0 else -3), CELL - 2 - h - 5),
               fill=mix(c, G_BR, 0.4) + (255,), width=2)
    return im


# ---- しだ（沢ぞい・木かげ）
def fern():
    im = cell()
    d = ImageDraw.Draw(im)
    for i in range(9):
        a = -1.35 + i * 0.34
        x0, y0 = CELL // 2, CELL - 4
        for k in range(11):
            t = k / 10.0
            x = int(x0 + a * 46 * t)
            y = int(y0 - 52 * t + 14 * t * t)
            c = mix(G_DK, G_LT, 0.25 + 0.5 * t)
            d.line((x, y, x + int(a * 5), y - 6), fill=c + (255,), width=3)
    return im


# ---- 岩（山はだに 出て いる）
def rock():
    im = cell()
    d = ImageDraw.Draw(im)
    base = mix(GREY, (255, 255, 255), 0.18)
    d.polygon([(CELL // 2 - 40, CELL - 6), (CELL // 2 - 30, CELL - 44),
               (CELL // 2 - 4, CELL - 56), (CELL // 2 + 26, CELL - 40),
               (CELL // 2 + 38, CELL - 6)], fill=base + (255,), outline=DARK + (255,))
    d.polygon([(CELL // 2 - 30, CELL - 44), (CELL // 2 - 4, CELL - 56),
               (CELL // 2 + 4, CELL - 30), (CELL // 2 - 18, CELL - 22)],
              fill=mix(base, (255, 255, 255), 0.22) + (255,))
    for _ in range(40):
        x = rng.randint(CELL // 2 - 38, CELL // 2 + 36)
        y = rng.randint(CELL - 54, CELL - 8)
        if im.getpixel((x, y))[3] > 0:
            im.putpixel((x, y), mix(base, DARK, 0.30) + (255,))
    # 岩の 上の こけ
    for _ in range(24):
        x = rng.randint(CELL // 2 - 26, CELL // 2 + 20)
        y = rng.randint(CELL - 54, CELL - 40)
        if im.getpixel((x, y))[3] > 0:
            im.putpixel((x, y), mix(G_DK, G_MD, 0.5) + (255,))
    return im


# ---- 倒木（山道の わき）
def log():
    im = cell()
    d = ImageDraw.Draw(im)
    c = mix(BR_DK, BR_LT, 0.35)
    d.rounded_rectangle((CELL // 2 - 52, CELL - 30, CELL // 2 + 48, CELL - 8),
                        radius=10, fill=c + (255,), outline=DARK + (255,))
    d.ellipse((CELL // 2 + 38, CELL - 30, CELL // 2 + 52, CELL - 8),
              fill=mix(c, BR_LT, 0.45) + (255,), outline=DARK + (255,))
    for i in range(-48, 44, 7):
        d.line((CELL // 2 + i, CELL - 27, CELL // 2 + i + 4, CELL - 11),
               fill=mix(c, DARK, 0.30) + (255,))
    for _ in range(18):   # こけ
        x = rng.randint(CELL // 2 - 50, CELL // 2 + 40)
        y = rng.randint(CELL - 30, CELL - 24)
        if im.getpixel((x, y))[3] > 0:
            im.putpixel((x, y), mix(G_DK, G_MD, 0.4) + (255,))
    return im


if __name__ == "__main__":
    parts = [
        # 0,1: スギ（人工林。高く 細い）。1本ずつ すこし ちがう
        conifer(132, 46, G_DK, G_MD, 11),
        conifer(112, 40, mix(G_DK, G_GY, 0.4), G_MD, 29),
        # 2: ヒノキふう（すこし 明るく ずんぐり）
        conifer(96, 52, mix(G_DK, G_MD, 0.35), G_LT, 47),
        # 3: 枯れ木
        deadtree(),
        # 4..7: 下ばえ と 地物
        sasa(), fern(), rock(), log(),
    ]
    sheet = Image.new("RGBA", (CELL * COLS, CELL * ROWS), (0, 0, 0, 0))
    for i, p in enumerate(parts):
        sheet.paste(p, ((i % COLS) * CELL, (i // COLS) * CELL))
    sheet.save(OUT)
    print("->", OUT, sheet.size)
