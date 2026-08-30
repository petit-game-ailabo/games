# -*- coding: utf-8 -*-
# 本人が 用意した 立ち絵・目とじ・走り6コマ（ぜんぶ 正面）を、
# CharSprite が 読む **8列 x 8行** の アトラスに 組む。
#
#   列 = 向き（いまは 絵が 正面しか 無いので **どの 列も 正面**。
#        左右は 鏡に して おく＝向きの 絵が そろったら ここを 差し替える）
#   行 = 0..5:走りの 6コマ / 6:立ち / 7:目とじ
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


def main():
    tachi = one("15_27_01")          # 立ち（目あき）
    metsu = one("15_27_20")          # 目とじ
    runs = [one("15_37_06 (1)"), one("15_37_06 (2)"), one("15_37_07 (3)"),
            one("15_37_07 (4)"), one("15_37_08 (5)"), one("15_37_08 (6)")]

    # ★ぜんぶ **同じ 枠**で 切る。コマごとに 詰めると 足もとが 上下に はねる
    box = None
    for im in [tachi, metsu] + runs:
        b = im.getbbox()
        box = b if box is None else (min(box[0], b[0]), min(box[1], b[1]),
                                     max(box[2], b[2]), max(box[3], b[3]))

    def fit(im, flip=False):
        c = im.crop(box)
        if flip:
            c = c.transpose(Image.FLIP_LEFT_RIGHT)
        sc = min((CELL_W - 12) / c.width, (CELL_H - 10) / c.height)
        c = c.resize((max(1, int(c.width * sc)), max(1, int(c.height * sc))), Image.LANCZOS)
        out = Image.new("RGBA", (CELL_W, CELL_H), (0, 0, 0, 0))
        out.alpha_composite(c, ((CELL_W - c.width) // 2, CELL_H - c.height - 3))
        return out

    rows = runs + [tachi, metsu]          # 行 0..5=走り / 6=立ち / 7=目とじ
    atlas = Image.new("RGBA", (CELL_W * 8, CELL_H * 8), (0, 0, 0, 0))
    for col in range(8):
        flip = col in (5, 6, 7)           # 右がわの 列は 鏡（絵が そろうまでの つなぎ）
        for row, src in enumerate(rows):
            atlas.alpha_composite(fit(src, flip), (col * CELL_W, row * CELL_H))
    atlas.save(OUT)
    print("wrote", os.path.abspath(OUT), atlas.size, "cell", (CELL_W, CELL_H))

    # 会話用の 立ち絵（大きいまま・目あき／目とじ）
    os.makedirs(TACHIE_DIR, exist_ok=True)
    tachi.crop(box).save(os.path.join(TACHIE_DIR, "marisa_tachie.png"))
    metsu.crop(box).save(os.path.join(TACHIE_DIR, "marisa_tachie_me.png"))
    print("tachie", tachi.crop(box).size)


if __name__ == "__main__":
    main()
