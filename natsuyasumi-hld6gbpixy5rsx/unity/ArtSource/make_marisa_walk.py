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
import os, glob, math, itertools
from collections import deque
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
REF = os.path.join(HERE, "ref", "photos")
OUT = os.path.join(HERE, "..", "Assets", "Art", "Sprites", "marisa_walk.png")
TACHIE_DIR = os.path.join(HERE, "..", "Assets", "Art", "Sprites", "tachie")

CELL_W, CELL_H = 224, 336
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


def tate(im):
    """bbox の 背たけ"""
    b = im.getbbox()
    return b[3] - b[1]


def sotto(ims):
    """その 方向の **いちばん 直立した コマ**の 背たけ。
    走りは 前かがみに なる ので、全コマの 平均や 面積で そろえると 方向ごとに ずれる
    （実測：面積ぞろえでも 止まりの 背たけが 10%ちがった）。
    走りの 山＝直立の しゅんかんは どの 方向でも 同じ 背たけの はず なので、
    そこを 基準に すれば **方向を またいで ぴったり** そろう"""
    return max(tate(c) for c in ims)


def torso_cx(im):
    """胴（たて 35〜60%）の よこ 重心。腕や 足の ふりに 引っぱられない"""
    b = im.getbbox()
    a = im.crop(b).getchannel("A")
    px = a.load(); w, h = a.size
    tot = 0; sx = 0
    for y in range(int(h * 0.35), int(h * 0.60)):
        for x in range(w):
            if px[x, y] > 128:
                tot += 1; sx += x
    return (sx / float(tot)) if tot else w / 2.0


def place(im, scale):
    """1コマを セルに 置く。**胴の よこ位置を まん中・足もとを 下の 線**に そろえる。
    もらった 絵は コマごとに 位置が ばらついて いて（実測：足もとが 最大 48px＝背たけの
    10% も 上下）、そのまま だと がくがく 跳ねて 見える"""
    b = im.getbbox()
    c = im.crop(b)
    w2 = max(1, int(round(c.width * scale)))
    h2 = max(1, int(round(c.height * scale)))
    c = c.resize((w2, h2), Image.LANCZOS)
    cx = torso_cx(im) * scale                      # 胴の よこ位置（切りぬきの 中での 座標）
    out = Image.new("RGBA", (CELL_W, CELL_H), (0, 0, 0, 0))
    x = int(round(CELL_W * 0.5 - cx))
    y = CELL_H - h2 - 3
    out.alpha_composite(c, (max(-w2 + 1, min(CELL_W - 1, x)), y))
    return out


def frame_diff(a, b):
    """2コマの ちがい（そろえた あとの 画素差）。なめらかな 並び順を さがす ため"""
    pa, pb = a.load(), b.load()
    d = 0
    for y in range(0, CELL_H, 4):
        for x in range(0, CELL_W, 4):
            ca, cb = pa[x, y], pb[x, y]
            d += abs(ca[3] - cb[3])
            if ca[3] > 128 and cb[3] > 128:
                d += (abs(ca[0]-cb[0]) + abs(ca[1]-cb[1]) + abs(ca[2]-cb[2])) // 3
    return d


def smooth_order(frames):
    """8コマを **いちばん なめらかな わっか**に ならべ直す。
    もらった コマは 1枚ずつ 別に 描かれて いる ので、ならびが 手足の 動きとして
    つながって いない（実測：足の ひらきが 262→118→292 と 飛ぶ 方向が あった）。
    0番を 起点に、となりどうしの ちがいの 合計が いちばん 小さい 順を さがす"""
    n = len(frames)
    d = [[0] * n for _ in range(n)]
    for i in range(n):
        for j in range(i + 1, n):
            d[i][j] = d[j][i] = frame_diff(frames[i], frames[j])
    best = None; best_cost = None
    for perm in itertools.permutations(range(1, n)):
        order = (0,) + perm
        cost = sum(d[order[k]][order[(k + 1) % n]] for k in range(n))
        if best_cost is None or cost < best_cost:
            best_cost = cost; best = order
    return list(best), best_cost


def main():
    tachi = one("15_27_01")
    metsu = one("15_27_20")

    runs = {}
    for col, tag in enumerate(DIRS):
        runs[col] = sheet_cells(tag)

    # ① 大きさを そろえる（直立コマの 背たけ を 基準に）
    TARGET = CELL_H - 24                       # セルの 中の 背たけ（ふちに 少し 余白）
    scales = {col: TARGET / float(sotto(runs[col])) for col in runs}

    atlas = Image.new("RGBA", (CELL_W * COLS, CELL_H * ROWS), (0, 0, 0, 0))
    for col in range(COLS):
        placed = [place(c, scales[col]) for c in runs[col]]
        order, cost = smooth_order(placed)
        n = len(placed)
        raw = sum(frame_diff(placed[k], placed[(k + 1) % n]) for k in range(n))
        print("列%d %-10s 縮尺%.3f  順 %s  つなぎの ちがい %d→%d (%.0f%%減)"
              % (col, DIRS[col][-6:], scales[col], order, raw, cost, 100.0 * (raw - cost) / raw))
        for row in range(8):
            atlas.alpha_composite(placed[order[row]], (col * CELL_W, row * CELL_H))
        # 行8＝立ち／行9＝目とじ
        if col == 0:
            # 立ち絵は 走りと **背たけ**を そろえる（姿勢が ちがうので 面積では 合わない）
            s_tac = TARGET / float(tate(tachi))
            stand, blink = place(tachi, s_tac), place(metsu, s_tac)
        else:
            # いちばん 背の 高い コマ＝いちばん 直立して いる（前かがみの コマを 立ちに すると
            # 方向に よって 背たけが ちがって 見える）
            k = max(range(8), key=lambda i: tate(runs[col][i]))
            stand = blink = place(runs[col][k], scales[col])
        atlas.alpha_composite(stand, (col * CELL_W, 8 * CELL_H))
        atlas.alpha_composite(blink, (col * CELL_W, 9 * CELL_H))

    atlas.save(OUT)
    print("wrote", os.path.abspath(OUT), atlas.size, "cell", (CELL_W, CELL_H))

    # 検算：方向ごとの 背たけが そろって いるか
    hs = []
    for col in range(COLS):
        b = atlas.crop((col * CELL_W, 8 * CELL_H, (col + 1) * CELL_W, 9 * CELL_H)).getbbox()
        hs.append(b[3] - b[1])
    print("止まりの 背たけ:", hs, " ばらつき %.1f%%" % (100.0 * (max(hs) - min(hs)) / max(hs)))
    ph = []
    for col in range(COLS):
        v = []
        for row in range(8):
            b = atlas.crop((col * CELL_W, row * CELL_H,
                            (col + 1) * CELL_W, (row + 1) * CELL_H)).getbbox()
            v.append(b[3] - b[1])
        ph.append(max(v))
    print("走りの 山の 背たけ:", ph, " ばらつき %.1f%%" % (100.0 * (max(ph) - min(ph)) / max(ph)))

    os.makedirs(TACHIE_DIR, exist_ok=True)
    bt = union([tachi, metsu])
    tachi.crop(bt).save(os.path.join(TACHIE_DIR, "marisa_tachie.png"))
    metsu.crop(bt).save(os.path.join(TACHIE_DIR, "marisa_tachie_me.png"))


if __name__ == "__main__":
    main()
