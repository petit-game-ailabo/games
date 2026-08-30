# -*- coding: utf-8 -*-
# 本人が 用意した 立ち絵・目とじ・走り6コマ（ぜんぶ 正面）を、
# CharSprite が 読む **8列 x 8行** の アトラスに 組む。
#
#   列0..6 = 向き（いまは 絵が 正面しか 無いので どれも 正面。左右は 鏡）
#   列7    = 立ち（行0）と 目とじ（行1）＝止まって いる ときに つかう
#   行     = 走りの 8コマ
#
# ★1コマは 192x336（キャラの 比に あわせる）。取りこみは 上限4096・ミップなし・なめらか
# ★取りこみは **点フィルタに しない**（SetupURP が marisa_walk を 対象外に して いる）
#
# 走らせかた: python unity/ArtSource/make_marisa_walk.py
import os, glob
from collections import deque
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
REF = os.path.join(HERE, "ref", "photos")
OUT = os.path.join(HERE, "..", "Assets", "Art", "Sprites", "marisa_walk.png")
TACHIE_DIR = os.path.join(HERE, "..", "Assets", "Art", "Sprites", "tachie")

# キャラは たて長（およそ 1:2.1）。四角い コマだと 横が あまって 絵が 小さく なる
CELL_W, CELL_H = 192, 336


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


def sheet_cells(tag, cols, rows):
    """1枚に ならんだ コマを 切りだす（中みの ある 帯を さがして 割る）"""
    im = one(tag)
    w, h = im.size
    px = im.load()

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

    colf = [sum(0 if px[x, y][3] == 0 else 1 for y in range(0, h, 3)) for x in range(w)]
    rowf = [sum(0 if px[x, y][3] == 0 else 1 for x in range(0, w, 3)) for y in range(h)]
    cb, rb = bands(colf), bands(rowf)
    assert len(cb) == cols and len(rb) == rows, "コマの ならびが ちがう %d x %d" % (len(cb), len(rb))
    out = []
    for (ra, rz) in rb:
        for (ca, cz) in cb:
            out.append(im.crop((ca - 8, ra - 8, cz + 9, rz + 9)))
    return out


def main():
    tachi = one("15_27_01")          # 立ち（目あき）
    metsu = one("15_27_20")          # 目とじ
    # ★走りは 8コマ（4列 x 2行の 1枚）。前の 6コマは 動きが いまいち だった（本人 2026-08-30）
    runs = sheet_cells("16_09_44", 4, 2)

    def union(ims):
        b = None
        for im in ims:
            t = im.getbbox()
            if t is None:
                continue
            b = t if b is None else (min(b[0], t[0]), min(b[1], t[1]),
                                     max(b[2], t[2]), max(b[3], t[3]))
        return b

    # ★出どころ ごとに そろえる（走りは 走りの 枠、立ちは 立ちの 枠）。
    #   そのうえで **背たけが そろう ように** 縮尺を あわせる
    box_run = union(runs)
    box_tac = union([tachi, metsu])
    h_run = box_run[3] - box_run[1]
    h_tac = box_tac[3] - box_tac[1]

    def fit(im, group, flip=False):
        box = box_run if group == "run" else box_tac
        c = im.crop(box)
        if flip:
            c = c.transpose(Image.FLIP_LEFT_RIGHT)
        # 高さ（走りの 枠）を そろえて から コマに 収める
        base = h_run if group == "run" else h_tac
        sc = min((CELL_W - 12) / (c.width * (h_run / float(base))), (CELL_H - 10) / base)
        c = c.resize((max(1, int(c.width * sc)), max(1, int(c.height * sc))), Image.LANCZOS)
        out = Image.new("RGBA", (CELL_W, CELL_H), (0, 0, 0, 0))
        out.alpha_composite(c, ((CELL_W - c.width) // 2, CELL_H - c.height - 3))
        return out

    # 行 0..7 = 走りの 8コマ。立ち／目とじは **列7**に 入れる（列7は 向きに つかわない）
    atlas = Image.new("RGBA", (CELL_W * 8, CELL_H * 8), (0, 0, 0, 0))
    for col in range(7):
        flip = col in (5, 6)              # 右がわの 列は 鏡（向きの 絵が そろうまでの つなぎ）
        for row in range(8):
            atlas.alpha_composite(fit(runs[row], "run", flip), (col * CELL_W, row * CELL_H))
    for row in range(8):
        atlas.alpha_composite(fit(metsu if row == 1 else tachi, "tac"), (7 * CELL_W, row * CELL_H))
    atlas.save(OUT)
    print("wrote", os.path.abspath(OUT), atlas.size, "cell", (CELL_W, CELL_H))

    # 会話用の 立ち絵（大きいまま・目あき／目とじ）
    os.makedirs(TACHIE_DIR, exist_ok=True)
    tachi.crop(box_tac).save(os.path.join(TACHIE_DIR, "marisa_tachie.png"))
    metsu.crop(box_tac).save(os.path.join(TACHIE_DIR, "marisa_tachie_me.png"))
    print("tachie", tachi.crop(box_tac).size)


if __name__ == "__main__":
    main()
