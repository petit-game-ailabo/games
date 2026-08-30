# -*- coding: utf-8 -*-
# 本人が 用意した 歩き差分（8コマ x 7方向）を、CharSprite が そのまま 読める
# **8方向(列) x 8コマ(行)** の アトラスに 組みなおす。
#
# もらった 絵の ならび（機械で 検出＋目視で 確認）
#   行 = 正面 / 左ななめ前 / 左 / 左ななめ奥 / 奥 / 右ななめ奥 / 右ななめ前
#   列 = 歩きの 8コマ
#   ★「右」だけ 無いので **「左」を 鏡に して** 埋める（7方向 → 8方向）
#
# 出力の ならび（CharSprite の きまり）
#   列 = 0:正面 1:左ななめ前 2:左 3:左ななめ奥 4:奥 5:右ななめ奥 6:右 7:右ななめ前
#   行 = 歩きの 8コマ（0が 立ちも かねる）
#
# 走らせかた: python unity/ArtSource/make_marisa_walk.py
import os
from collections import deque
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "ref", "photos", "ChatGPT Image 2026年8月30日 14_32_43.png")
OUT = os.path.join(HERE, "..", "Assets", "Art", "Sprites", "marisa_walk.png")

COLS_IN, ROWS_IN = 8, 7
CELL_W, CELL_H = 128, 160          # 出力 1コマ（背たけ 1.3m ＝ 1mあたり 約123ドット）


def is_bg(p):
    """市松の 地か（灰色に 近く 明るい）"""
    r, g, b = p[0], p[1], p[2]
    if min(r, g, b) < 224:
        return False
    return abs(r - g) < 12 and abs(g - b) < 12 and abs(r - b) < 12


def cut_bg(im):
    """ふちから たどれる 地だけ 抜く（エプロンの 白は 囲まれて いるので 残る）"""
    im = im.convert("RGBA")
    w, h = im.size
    px = im.load()
    seen = bytearray(w * h)
    q = deque()

    def push(x, y):
        i = y * w + x
        if seen[i] or not is_bg(px[x, y]):
            return
        seen[i] = 1
        q.append((x, y))

    for x in range(w):
        push(x, 0); push(x, h - 1)
    for y in range(h):
        push(0, y); push(w - 1, y)
    while q:
        cx, cy = q.popleft()
        px[cx, cy] = (0, 0, 0, 0)
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = cx + dx, cy + dy
            if 0 <= nx < w and 0 <= ny < h:
                push(nx, ny)
    return im


def bands(f, thr):
    out = []
    s = None
    for i, v in enumerate(f):
        if v > thr and s is None:
            s = i
        elif v <= thr and s is not None:
            out.append((s, i - 1)); s = None
    if s is not None:
        out.append((s, len(f) - 1))
    return out


def main():
    src = Image.open(SRC).convert("RGB")
    w, h = src.size
    px = src.load()

    colf = [sum(0 if is_bg(px[x, y]) else 1 for y in range(0, h, 3)) for x in range(w)]
    cb = bands(colf, 1)
    assert len(cb) == COLS_IN, "列が %d 個（8のはず）" % len(cb)

    rowf = [sum(0 if is_bg(px[x, y]) else 1 for x in range(0, w, 2)) for y in range(h)]
    rb = bands(rowf, 3)
    # くっついた 帯（正面ななめ前の 2行）を 半分に わる
    fixed = []
    for a, z in rb:
        if z - a > 220:
            m = (a + z) // 2
            fixed.append((a, m - 3)); fixed.append((m + 3, z))
        else:
            fixed.append((a, z))
    assert len(fixed) == ROWS_IN, "行が %d 個（7のはず）" % len(fixed)

    cut = cut_bg(src)

    def cell(ri, ci):
        """1コマを 切って、**足もとの まん中**で そろえて 詰める"""
        x0, x1 = cb[ci]
        y0, y1 = fixed[ri]
        im = cut.crop((x0 - 6, y0 - 6, x1 + 7, y1 + 7))
        bb = im.getbbox()
        if bb is None:
            return Image.new("RGBA", (CELL_W, CELL_H), (0, 0, 0, 0))
        im = im.crop(bb)
        sc = min((CELL_W - 8) / im.width, (CELL_H - 6) / im.height)
        im = im.resize((max(1, int(im.width * sc)), max(1, int(im.height * sc))), Image.LANCZOS)
        out = Image.new("RGBA", (CELL_W, CELL_H), (0, 0, 0, 0))
        out.alpha_composite(im, ((CELL_W - im.width) // 2, CELL_H - im.height - 2))
        return out

    # もらった 行 → 出力の 列（向き）
    #   0:正面=行0 / 1:左ななめ前=行1 / 2:左=行2 / 3:左ななめ奥=行3 / 4:奥=行4
    #   5:右ななめ奥=行5 / 6:右=行2の鏡 / 7:右ななめ前=行6
    MAP = [(0, False), (1, False), (2, False), (3, False),
           (4, False), (5, False), (2, True), (6, False)]

    atlas = Image.new("RGBA", (CELL_W * 8, CELL_H * 8), (0, 0, 0, 0))
    for out_col, (src_row, mirror) in enumerate(MAP):
        for frame in range(8):
            c = cell(src_row, frame)
            if mirror:
                c = c.transpose(Image.FLIP_LEFT_RIGHT)
            atlas.alpha_composite(c, (out_col * CELL_W, frame * CELL_H))

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    atlas.save(OUT)
    print("wrote", os.path.abspath(OUT), atlas.size, "cell", (CELL_W, CELL_H))


if __name__ == "__main__":
    main()
