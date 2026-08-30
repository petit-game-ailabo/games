# -*- coding: utf-8 -*-
# 本人が 用意した 絵から、CharSprite が 読む アトラスを 組む（2026-08-30）。
#
# もらった もの
#   立ち絵（目あき／目とじ）… 1枚ずつ・正面
#   走り 8方向 x 8コマ … 1方向が 4列x2行の シート 1枚。ぜんぶで 8枚
#
# 出力の わりつけ（8列 x 10行）
#   列 = 0:正面 1:左ななめ前 2:左 3:左ななめ奥 4:奥 5:右ななめ奥 6:右 7:右ななめ前
#   行 = 0..7:走りの 8コマ / 8:立ち / 9:目とじ
#
# ★出どころが ちがう 絵は **座標系も ちがう**（立ち絵は 1024x1536、走りは 214x491）。
#   同じ 枠で 切ると 片方だけ 極端に 小さく なる → 出どころ ごとに 枠を とって
#   **背たけを そろえて から** コマに 詰める。
# ★取りこみは 点フィルタに しない・ミップも 切る（SetupURP が marisa_walk を 別あつかい）
#
# 走らせかた: python unity/ArtSource/make_marisa_walk.py
import os, glob
from collections import deque
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
REF = os.path.join(HERE, "ref", "photos")
OUT = os.path.join(HERE, "..", "Assets", "Art", "Sprites", "marisa_walk.png")
TACHIE_DIR = os.path.join(HERE, "..", "Assets", "Art", "Sprites", "tachie")

CELL_W, CELL_H = 192, 336
COLS, ROWS = 8, 10

# 向きの ならび（列の 順）。タグは もらった ファイル名の 一部
DIRS = ["16_09_44",                          # 0 正面
        "16_27_04 (1)", "16_27_05 (2)", "16_27_05 (3)", "16_27_06 (4)",
        "16_27_07 (5)", "16_27_07 (6)", "16_27_07 (7)"]


def is_bg(p):
    r, g, b = p[0], p[1], p[2]
    return min(r, g, b) >= 222 and abs(r - g) < 14 and abs(g - b) < 14 and abs(r - b) < 14


def cut(path):
    """ふちから たどれる 市松の 地だけ 抜く（囲まれた 白い エプロンは 残す）"""
    im = Image.open(path).convert("RGBA")
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


def one(tag):
    f = [x for x in glob.glob(os.path.join(REF, "ChatGPT*.png")) if tag in os.path.basename(x)]
    assert f, "見つからない: " + tag
    return cut(sorted(f)[0])


def bands(f, thr=1):
    out = []; s = None
    for i, v in enumerate(f):
        if v > thr and s is None:
            s = i
        elif v <= thr and s is not None:
            out.append((s, i - 1)); s = None
    if s is not None:
        out.append((s, len(f) - 1))
    return out


def sheet_cells(tag, cols=4, rows=2):
    """4列x2行の シートを 8コマに 割る"""
    im = one(tag)
    w, h = im.size
    px = im.load()
    colf = [sum(0 if px[x, y][3] == 0 else 1 for y in range(0, h, 3)) for x in range(w)]
    rowf = [sum(0 if px[x, y][3] == 0 else 1 for x in range(0, w, 3)) for y in range(h)]
    cb, rb = bands(colf), bands(rowf)
    assert len(cb) == cols and len(rb) == rows, \
        "%s の ならびが ちがう %d x %d" % (tag, len(cb), len(rb))
    return [im.crop((ca - 8, ra - 8, cz + 9, rz + 9)) for (ra, rz) in rb for (ca, cz) in cb]


def union(ims):
    b = None
    for im in ims:
        t = im.getbbox()
        if t is None:
            continue
        b = t if b is None else (min(b[0], t[0]), min(b[1], t[1]),
                                 max(b[2], t[2]), max(b[3], t[3]))
    return b


def main():
    tachi = one("15_27_01")
    metsu = one("15_27_20")
    box_tac = union([tachi, metsu])
    h_tac = box_tac[3] - box_tac[1]

    runs = {}       # 列 → 8コマ
    boxes = {}      # 列 → その 方向の 枠
    for col, tag in enumerate(DIRS):
        cells = sheet_cells(tag)
        runs[col] = cells
        boxes[col] = union(cells)

    # 背たけの 基準＝いちばん 高い 方向（これに みんな そろえる）
    h_base = max(boxes[c][3] - boxes[c][1] for c in runs)

    def fit(im, box, h_src):
        c = im.crop(box)
        sc = (CELL_H - 10) / float(h_base) * (h_base / float(h_src)) * (h_src / float(h_src))
        sc = (CELL_H - 10) / float(h_base)          # 全部 同じ 縮尺（背たけが そろう）
        w2 = max(1, int(c.width * sc)); h2 = max(1, int(c.height * sc))
        if w2 > CELL_W - 8:                          # はみ出す ときだけ 横で 抑える
            k = (CELL_W - 8) / float(w2); w2 = CELL_W - 8; h2 = max(1, int(h2 * k))
        c = c.resize((w2, h2), Image.LANCZOS)
        out = Image.new("RGBA", (CELL_W, CELL_H), (0, 0, 0, 0))
        out.alpha_composite(c, ((CELL_W - c.width) // 2, CELL_H - c.height - 3))
        return out

    atlas = Image.new("RGBA", (CELL_W * COLS, CELL_H * ROWS), (0, 0, 0, 0))
    for col in range(COLS):
        box = boxes[col]
        hh = box[3] - box[1]
        for row in range(8):
            atlas.alpha_composite(fit(runs[col][row], box, hh), (col * CELL_W, row * CELL_H))
        # 行8＝立ち。正面は 立ち絵、ほかは **足の そろった コマ**（横はばが いちばん せまい）
        if col == 0:
            stand = fit(tachi, box_tac, h_tac)
            blink = fit(metsu, box_tac, h_tac)
        else:
            k = min(range(8), key=lambda i: (runs[col][i].getbbox()[2] - runs[col][i].getbbox()[0]))
            stand = blink = fit(runs[col][k], box, hh)
        atlas.alpha_composite(stand, (col * CELL_W, 8 * CELL_H))
        atlas.alpha_composite(blink, (col * CELL_W, 9 * CELL_H))

    atlas.save(OUT)
    print("wrote", os.path.abspath(OUT), atlas.size, "cell", (CELL_W, CELL_H))

    os.makedirs(TACHIE_DIR, exist_ok=True)
    tachi.crop(box_tac).save(os.path.join(TACHIE_DIR, "marisa_tachie.png"))
    metsu.crop(box_tac).save(os.path.join(TACHIE_DIR, "marisa_tachie_me.png"))
    print("tachie", tachi.crop(box_tac).size)


if __name__ == "__main__":
    main()
