# 家の テクスチャを ドット絵で 起こす。
#
# ねらい：**木立ち(ansimuz「Trees & Bushes」CC0)と 同じ 絵の 出どころに 見せる。**
# 前は 写真から 起こした テクスチャを 貼っていたので、ドット絵の 草木と ならぶと
# 材質が ちぐはぐ だった。そこで 木の 絵から 実際に 使われている 色を 吸いだし、
# その 20色の 系統だけで 家の 材質を 描く。
#
# 尺は **32px＝1m**（草木の アトラスと 同じ）。
# 走らせかた: python unity/ArtSource/make_textures.py
import os
import random

from PIL import Image

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Art", "Textures")

# --- 木立ちの 絵から 拾った 色（これ以外は 使わない）
DARK   = (0x25, 0x22, 0x2e)   # いちばん こい 輪郭
G_DK   = (0x11, 0x4d, 0x3c)
G_MD   = (0x36, 0x65, 0x12)
G_LT   = (0x55, 0x86, 0x19)
G_BR   = (0x6d, 0xb9, 0x27)
G_GY   = (0x24, 0x50, 0x22)
BR_DK  = (0x40, 0x24, 0x18)   # みきの かげ
BR_LT  = (0xab, 0x7a, 0x1e)   # みきの ひかり
OLIVE  = (0x9a, 0x92, 0x3a)
OL_DK  = (0x5d, 0x60, 0x2c)
PALE   = (0xd3, 0xc6, 0x3b)
GREY   = (0x3d, 0x3f, 0x37)


def mix(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))


def newimg(w, h, c):
    return Image.new("RGB", (w, h), c)


# 決めうちの 乱数。**毎回 同じ 絵に なる**ように 種を 固定する
rng = random.Random(20260814)


def speckle(im, colors, n):
    """ぽつぽつ 打つ。端を またいでも つながる ように そのまま 置く（画像の 端＝繰りかえしの 境）"""
    px = im.load()
    w, h = im.size
    for _ in range(n):
        x, y = rng.randrange(w), rng.randrange(h)
        px[x, y] = colors[rng.randrange(len(colors))]


# ---------------------------------------------------------------- 畳
# 1まい 1.8m x 0.9m。32px/m なので 57.6 x 28.8 → 56 x 28 に まるめる（3%の ちがいは 見えない）
def tatami():
    w, h = 56, 28
    base = mix(OLIVE, (255, 255, 255), 0.30)      # 日に やけた 藺草
    lite = mix(OLIVE, (255, 255, 255), 0.44)
    dark = mix(OLIVE, DARK, 0.30)
    im = newimg(w, h, base)
    px = im.load()
    # 目（よこ方向に ながれる 藺草）。2pxおきに 明暗を つける
    for y in range(h):
        if y % 4 == 0:
            for x in range(w):
                px[x, y] = lite
        elif y % 4 == 2:
            for x in range(w):
                px[x, y] = dark
    # たての ほつれ。びっしり 同じだと 布に 見えるので すこし 崩す
    for _ in range(90):
        x, y = rng.randrange(w), rng.randrange(h)
        px[x, y] = lite if rng.random() < 0.5 else dark
    # へり（縁）。長い 辺に 黒い 布を まわす
    edge = mix(BR_DK, DARK, 0.35)
    edge2 = mix(BR_DK, (255, 255, 255), 0.12)
    for x in range(w):
        for y in (0, 1, h - 2, h - 1):
            px[x, y] = edge
        px[x, 2] = edge2 if x % 5 else edge
        px[x, h - 3] = edge2 if x % 5 else edge
    # **短い 辺にも 継ぎ目の 線を 入れる。** これが 無いと、よこに ならべた とき
    # 1まいずつの 切れめが 見えず、部屋ぜんたいが ただの 縞に なった
    seam = mix(OLIVE, DARK, 0.55)
    for y in range(h):
        px[0, y] = seam
        px[w - 1, y] = seam
    im.save(os.path.join(OUT, "tatami.png"))
    return "tatami.png", im.size


# ---------------------------------------------------------------- 板の間・縁がわ
def wood_floor():
    w = h = 32                                    # 1m ぶん
    im = newimg(w, h, mix(BR_DK, BR_LT, 0.45))
    px = im.load()
    plank = 8                                     # 板 1まいの はば（25cm）
    for y in range(h):
        row = y // plank
        t = 0.38 + 0.10 * ((row * 7) % 3)         # 板ごとに 色を すこし 変える
        c = mix(BR_DK, BR_LT, t)
        for x in range(w):
            px[x, y] = c
        if y % plank == 0:                        # 板の つぎ目
            for x in range(w):
                px[x, y] = mix(BR_DK, DARK, 0.45)
    # 木目。よこに ながれる すじ
    for _ in range(70):
        y = rng.randrange(h)
        if y % plank == 0:
            continue
        x0 = rng.randrange(w)
        c = mix(BR_DK, BR_LT, 0.25 if rng.random() < 0.5 else 0.62)
        for i in range(rng.randrange(3, 11)):
            px[(x0 + i) % w, y] = c
    im.save(os.path.join(OUT, "wood_floor.png"))
    return "wood_floor.png", im.size


# ---------------------------------------------------------------- 柱・桟（たての 木）
def wood_beam():
    w = h = 32
    im = newimg(w, h, mix(BR_DK, BR_LT, 0.40))
    px = im.load()
    for x in range(w):
        t = 0.34 + 0.16 * (((x * 5) % 7) / 6.0)
        c = mix(BR_DK, BR_LT, t)
        for y in range(h):
            px[x, y] = c
    # たての 木目
    for _ in range(60):
        x = rng.randrange(w)
        y0 = rng.randrange(h)
        c = mix(BR_DK, BR_LT, 0.20 if rng.random() < 0.5 else 0.60)
        for i in range(rng.randrange(4, 14)):
            px[x, (y0 + i) % h] = c
    im.save(os.path.join(OUT, "wood_beam.png"))
    return "wood_beam.png", im.size


# ---------------------------------------------------------------- 土壁
def plaster_wall():
    w = h = 32
    base = mix(OLIVE, (255, 255, 255), 0.42)
    im = newimg(w, h, base)
    px = im.load()
    # むらを 大きめに 置く。のっぺりだと 紙に 見える
    for _ in range(26):
        cx, cy = rng.randrange(w), rng.randrange(h)
        r = rng.randrange(2, 6)
        c = mix(base, OL_DK, 0.10 + 0.10 * rng.random())
        for dy in range(-r, r + 1):
            for dx in range(-r, r + 1):
                if dx * dx + dy * dy <= r * r:
                    px[(cx + dx) % w, (cy + dy) % h] = c
    # 藁すさ（土に まぜた わら）。これが あると ぐっと 土壁に なる
    for _ in range(46):
        x, y = rng.randrange(w), rng.randrange(h)
        c = mix(OLIVE, PALE, 0.35)
        for i in range(rng.randrange(2, 5)):
            px[(x + i) % w, (y + (i // 2)) % h] = c
    speckle(im, [mix(base, OL_DK, 0.22), mix(base, (255, 255, 255), 0.18)], 60)
    im.save(os.path.join(OUT, "plaster_wall.png"))
    return "plaster_wall.png", im.size


# ---------------------------------------------------------------- 障子紙
def shoji_paper():
    w = h = 32
    base = mix(PALE, (255, 255, 255), 0.72)       # 生なりの 白
    im = newimg(w, h, base)
    px = im.load()
    # 楮の せんい。ごく うっすら
    for _ in range(40):
        x, y = rng.randrange(w), rng.randrange(h)
        c = mix(base, OLIVE, 0.13)
        for i in range(rng.randrange(2, 7)):
            px[(x + i) % w, y] = c
    im.save(os.path.join(OUT, "shoji_paper.png"))
    return "shoji_paper.png", im.size


# ---------------------------------------------------------------- かわら（屋根）
def roof_tile():
    w, h = 32, 32
    im = newimg(w, h, mix(G_GY, GREY, 0.5))
    px = im.load()
    base = mix(GREY, (255, 255, 255), 0.10)
    for y in range(h):
        for x in range(w):
            px[x, y] = base
    # なみ形の かわら。8px ごとに 山と 谷
    for x in range(w):
        phase = (x % 8)
        if phase in (0,):
            c = mix(GREY, DARK, 0.55)             # 谷＝かげ
        elif phase in (1, 7):
            c = mix(GREY, DARK, 0.25)
        elif phase == 4:
            c = mix(GREY, (255, 255, 255), 0.28)  # 山＝ひかり
        else:
            c = base
        for y in range(h):
            px[x, y] = c
    # よこの つぎ目
    for y in range(0, h, 16):
        for x in range(w):
            px[x, y] = mix(GREY, DARK, 0.65)
    speckle(im, [mix(GREY, DARK, 0.35), mix(GREY, (255, 255, 255), 0.16)], 70)
    im.save(os.path.join(OUT, "roof_tile.png"))
    return "roof_tile.png", im.size


# ---------------------------------------------------------------- 草地
# 木立ちと 同じ 緑で 敷きなおす。前のは 木より 彩度が 高くて 浮いていた
def grass_ground():
    w = h = 48
    im = newimg(w, h, G_MD)
    px = im.load()
    for y in range(h):
        for x in range(w):
            r = rng.random()
            if r < 0.10:
                px[x, y] = G_LT
            elif r < 0.16:
                px[x, y] = G_GY
            elif r < 0.19:
                px[x, y] = mix(G_MD, G_DK, 0.5)
    # 草の は。たてに 2〜3px
    for _ in range(150):
        x, y = rng.randrange(w), rng.randrange(h)
        c = G_BR if rng.random() < 0.25 else G_LT
        for i in range(rng.randrange(2, 4)):
            px[x, (y + i) % h] = c
    # 土が すこし 見える ところ
    for _ in range(14):
        cx, cy = rng.randrange(w), rng.randrange(h)
        for dy in range(-1, 2):
            for dx in range(-2, 3):
                if rng.random() < 0.55:
                    px[(cx + dx) % w, (cy + dy) % h] = mix(BR_DK, OLIVE, 0.45)
    im.save(os.path.join(OUT, "grass_ground.png"))
    return "grass_ground.png", im.size


# ---------------------------------------------------------------- けもの道・踏み分け道の 土
# 人が とおる ところは 草が はげて 土が 出る。草地と 同じ 48px＝1.5m
def dirt_path():
    w = h = 48
    base = mix(BR_DK, OLIVE, 0.42)
    im = newimg(w, h, base)
    px = im.load()
    for y in range(h):
        for x in range(w):
            r = rng.random()
            if r < 0.14:
                px[x, y] = mix(base, BR_LT, 0.30)
            elif r < 0.24:
                px[x, y] = mix(base, DARK, 0.28)
    # 小石
    for _ in range(26):
        cx, cy = rng.randrange(w), rng.randrange(h)
        c = mix(GREY, (255, 255, 255), 0.22)
        px[cx, cy] = c
        if rng.random() < 0.5:
            px[(cx + 1) % w, cy] = mix(GREY, DARK, 0.3)
    # わだち（踏まれて へこんだ すじ）
    for _ in range(8):
        y0 = rng.randrange(h)
        c = mix(base, DARK, 0.20)
        for x in range(w):
            px[x, (y0 + (x // 9)) % h] = c
    # はしに のこった 草
    for _ in range(30):
        x, y = rng.randrange(w), rng.randrange(h)
        px[x, y] = mix(G_MD, base, 0.35)
    im.save(os.path.join(OUT, "dirt_path.png"))
    return "dirt_path.png", im.size


if __name__ == "__main__":
    for f in (tatami, wood_floor, wood_beam, plaster_wall, shoji_paper, roof_tile, grass_ground, dirt_path):
        name, size = f()
        print("%-18s %s" % (name, size))
