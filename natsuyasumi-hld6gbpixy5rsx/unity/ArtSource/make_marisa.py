# 魔理沙の 8方向 x 8状態の 絵を 取りこむ。
#
# ★もらった 画像は **透過して いない。** RGB のまま で、
#   「透明に 見える 市松もよう」が **絵として 塗ってある**（PIL で mode=RGB）。
#   しかも 魔理沙の エプロンと 帽子の リボンは **本物の 白**なので、
#   「白っぽい 色を 消す」と 体に 穴が あく。
#   → 画の ふちから **塗りつぶしで たどれる ところ だけ** を 抜く。
#     エプロンの 白は 黒い 輪郭に 囲まれて いて ふちから 届かないので 残る。
#
# ならび（本人の 指定）
#   列 = 正面 / 左ななめ前 / 左 / 左ななめ奥 / 奥 / 右ななめ奥 / 右 / 右ななめ前
#   行 = 立ち / 歩き / 走り / 喜 / 怒 / 哀 / 楽 / 目を とじた
#
# 走らせかた: python unity/ArtSource/make_marisa.py
import os
from collections import deque

from PIL import Image

HERE = os.path.dirname(__file__)
# もらった 原画は ArtSource に 置く（Assets に 入れると 2.4MB の 素材が ビルドに 入る）
SRC = os.path.join(HERE, "marisa_source_8x8.png")
OUT = os.path.join(HERE, "..", "Assets", "Art", "Sprites", "marisa_8x8.png")

COLS, ROWS = 8, 8


def is_bg(p):
    """市松の 地か。**灰色に 近く、明るい**もの だけ"""
    r, g, b = p[0], p[1], p[2]
    if min(r, g, b) < 224:
        return False
    return abs(r - g) < 10 and abs(g - b) < 10 and abs(r - b) < 10


def cut_background(im):
    """ふちから 届く 地だけ 抜く（囲まれた 白は 残す）"""
    w, h = im.size
    px = im.load()
    seen = bytearray(w * h)
    q = deque()

    def push(x, y):
        i = y * w + x
        if seen[i]:
            return
        if not is_bg(px[x, y]):
            return
        seen[i] = 1
        q.append((x, y))

    for x in range(w):
        push(x, 0); push(x, h - 1)
    for y in range(h):
        push(0, y); push(w - 1, y)

    while q:
        x, y = q.popleft()
        if x > 0: push(x - 1, y)
        if x < w - 1: push(x + 1, y)
        if y > 0: push(x, y - 1)
        if y < h - 1: push(x, y + 1)

    out = im.convert("RGBA")
    op = out.load()
    for y in range(h):
        base = y * w
        for x in range(w):
            if seen[base + x]:
                op[x, y] = (0, 0, 0, 0)
    return out, seen


def bands(flags):
    """つづいて いる ところを 拾う。**升目は そろって いない**ので
    128x192 で 割るのでは なく、中みの ある 帯を 見つけて 割る
    （そのまま 割ったら 上下の コマが つながって 見えた）"""
    out, s = [], None
    for i, v in enumerate(flags):
        if v and s is None:
            s = i
        if not v and s is not None:
            out.append((s, i)); s = None
    if s is not None:
        out.append((s, len(flags)))
    return out


def foot_x(im, box):
    """足もとの まん中。**絵ぜんたいの まん中では ずれる。**
    帽子の つばは 横に 広く、向きに よって 出っぱりが 変わるので、
    そこで そろえると 向きを 変える たびに 体が 横に とぶ"""
    x0, y0, x1, y1 = box
    px = im.load()
    h = y1 - y0
    lo = y1 - max(3, int(h * 0.12))
    tot = n = 0
    for y in range(lo, y1):
        for x in range(x0, x1):
            if px[x, y][3] > 8:
                tot += x; n += 1
    if n == 0:
        return (x0 + x1) * 0.5
    return tot / n


def tight(im, x0, y0, x1, y1):
    """その はんいの 中で 中みが ある ところ だけに 詰める（画ぜんたいの 座標）"""
    px = im.load()
    minx, miny, maxx, maxy = x1, y1, -1, -1
    for y in range(y0, y1):
        for x in range(x0, x1):
            if px[x, y][3] > 8:
                if x < minx: minx = x
                if x > maxx: maxx = x
                if y < miny: miny = y
                if y > maxy: maxy = y
    if maxx < 0:
        return None
    return (minx, miny, maxx + 1, maxy + 1)


def pixel_scale(im, seen):
    """もとの ドットが 何ばいに 引きのばされて いるか を 当てる"""
    w, h = im.size
    px = im.load()
    best, bestscore = 1, -1
    for s in (1, 2, 3, 4, 6, 8):
        hit = tot = 0
        for y in range(0, h - s, s * 3):
            for x in range(0, w - s, s * 3):
                if seen[y * w + x]:
                    continue
                a = px[x, y]
                if a[3] == 0:
                    continue
                tot += 1
                if px[x + s - 1, y][:3] == a[:3] and px[x, y + s - 1][:3] == a[:3]:
                    hit += 1
        if tot < 50:
            continue
        score = (hit / tot) * s          # 大きい ばい率ほど うれしい
        if score > bestscore:
            bestscore, best = score, s
    return best


def main():
    im = Image.open(SRC).convert("RGB")
    w, h = im.size
    cw, ch = w // COLS, h // ROWS
    print("もと", im.size, "コマ", (cw, ch))

    rgba, seen = cut_background(im)
    print("ばい率の 見あて =", pixel_scale(rgba, seen))

    # 中みの ある 帯から 升目を 割りだす
    px = rgba.load()
    rowf = [any(px[x, y][3] > 8 for x in range(0, w, 2)) for y in range(h)]
    colf = [any(px[x, y][3] > 8 for y in range(0, h, 2)) for x in range(w)]
    rb, cb = bands(rowf), bands(colf)
    print("帯 たて", len(rb), "よこ", len(cb))
    if len(rb) != ROWS or len(cb) != COLS:
        raise SystemExit("帯の 数が 合わない: %d x %d" % (len(cb), len(rb)))

    # 1コマずつ 中みの はこを 出す
    cells = []
    for r0, r1 in rb:
        line = []
        for c0, c1 in cb:
            b = tight(rgba, c0, r0, c1, r1)
            line.append(b)
        cells.append(line)

    # そろえる 大きさ。**足もとを 下ばしに、足の まん中を よこの まん中に**
    maxw = max(b[2] - b[0] for line in cells for b in line if b)
    maxh = max(b[3] - b[1] for line in cells for b in line if b)
    nw = maxw + 4                                    # ふちに すこし ゆとり
    nh = maxh + 2
    print("1コマ", (nw, nh))

    sheet = Image.new("RGBA", (nw * COLS, nh * ROWS), (0, 0, 0, 0))
    for r in range(ROWS):
        for c in range(COLS):
            b = cells[r][c]
            if b is None:
                continue
            fx = foot_x(rgba, b)
            cell = rgba.crop(b)
            # 足の まん中を コマの まん中に、そこを 下ばしに あわせる
            dx = int(round(c * nw + nw * 0.5 - (fx - b[0])))
            dy = int(round(r * nh + nh - (b[3] - b[1])))
            sheet.paste(cell, (dx, dy), cell)

    sheet.save(OUT)
    print("->", OUT, sheet.size, "1コマ", (nw, nh))


if __name__ == "__main__":
    main()
