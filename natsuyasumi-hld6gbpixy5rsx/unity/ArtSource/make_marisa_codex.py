# Codex が 描いた 走りの コマを シートに 組む（2026-09-06）。
#
# もらう 絵：マゼンタ(#FF00FF)の 上に 4列x2行の 8コマ。大きさも 位置も バラバラで よい。
#            **こちらで そろえる**（そう 約束して 依頼して いる）。
#
# ここで やる こと：
#   1 マゼンタを 抜く（**ふちの にじみも 取る**。1px 削らないと 紫の 縁が 残る）
#   2 8コマに 切る（決め打ちの 枠では なく **中身の かたまりで** 切る）
#   3 **8コマ ぜんぶ 同じ 倍率**で 縮める（コマごとに 合わせると 大小の 差＝はずみが 消える）
#   4 横は まん中を x=112 に、縦は **その 行で いちばん 下の コマ**を y=332 に。
#     こうすると **浮きの コマが 浮いた まま 入る**
#
# run: python unity/ArtSource/make_marisa_codex.py [--miru]
import os
import sys
import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
SPR = os.path.join(HERE, "..", "Assets", "Art", "Sprites")
MOTO = os.path.join(HERE, "ref", "codex", "hashiri_shomen_raw.png")
CW, CH = 224, 336
TAKE = 312.0        # いまの シートの 身長
JIMEN = 332         # 足もとの 行


def haikei(a):
    """マゼンタの ところ True"""
    R, G, B = a[..., 0], a[..., 1], a[..., 2]
    return (R > 150) & (B > 150) & (G < 120) & ((R - G) > 60) & ((B - G) > 60)


def kugiri(v, saitei=60):
    """★せまい かたまりは すてる。1pxの ごみが 残って いると
    列の 区切りが 6つに なって 崩れる（2026-09-06 実際に なった）"""
    out, s = [], None
    for i, x in enumerate(v):
        if x and s is None:
            s = i
        if not x and s is not None:
            if i - s >= saitei:
                out.append((s, i - 1))
            s = None
    if s is not None and len(v) - s >= saitei:
        out.append((s, len(v) - 1))
    return out


def main():
    im = Image.open(MOTO).convert("RGB")
    a = np.asarray(im).astype(int)
    bg = haikei(a)
    fg = ~bg
    # ★ふちを 1px 削る。マゼンタと まざった 画素が 紫の 縁に なる
    fi = Image.fromarray((fg * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(3))
    # ★ごみ取り：MinFilter で 削った あと MaxFilter で もどす（開く）。
    #   これで 数画素の 点だけ 消えて、本体は 元の 大きさに もどる
    fi = fi.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MaxFilter(3))
    fg = np.asarray(fi) > 128

    rows = kugiri(fg.any(1))
    cols_all = kugiri(fg.any(0))
    if len(rows) != 2 or len(cols_all) != 4:
        print("!! かたまりが 4x2 に ならない: 行", len(rows), "列", len(cols_all))
    print("行", rows)
    print("列", cols_all)

    # 8コマの わくを 取る
    waku = []
    for (y0, y1) in rows:
        for (x0, x1) in cols_all:
            sub = fg[y0:y1 + 1, x0:x1 + 1]
            ys, xs = np.nonzero(sub)
            if len(ys) == 0:
                waku.append(None)
                continue
            waku.append((xs.min() + x0, ys.min() + y0, xs.max() + x0, ys.max() + y0))
    for i, w in enumerate(waku):
        print("コマ%d" % (i + 1), w, "たけ", (w[3] - w[1] + 1) if w else "-")

    # ★倍率は 8コマ 共通。たけの 中央値を 312 に する
    takasa = [w[3] - w[1] + 1 for w in waku if w]
    bai = TAKE / float(np.median(takasa))
    print("倍率 %.4f（たけの 中央値 %.0f → %.0f）" % (bai, np.median(takasa), TAKE))

    rgba = np.dstack([a, np.where(fg, 255, 0)]).astype(np.uint8)
    src = Image.fromarray(rgba)

    out = Image.new("RGBA", (CW * 8, CH), (0, 0, 0, 0))
    for ri, (ry0, ry1) in enumerate(rows):
        # その 行の いちばん 下 ＝ 地めん
        jimen = max(w[3] for w in waku[ri * 4:(ri + 1) * 4] if w)
        for ci in range(4):
            i = ri * 4 + ci
            w = waku[i]
            if w is None:
                continue
            x0, y0, x1, y1 = w
            piece = src.crop((x0, y0, x1 + 1, y1 + 1))
            nw = max(1, int(round((x1 - x0 + 1) * bai)))
            nh = max(1, int(round((y1 - y0 + 1) * bai)))
            piece = piece.resize((nw, nh), Image.LANCZOS)
            # 縦：地めんからの ずれを そのまま 持ちこむ＝**はずみが 残る**
            sita = JIMEN - int(round((jimen - y1) * bai))
            ox = int(round(CW * 0.5 - nw * 0.5))
            oy = sita - nh
            out.alpha_composite(piece, (i * CW + ox, oy))
            print("  コマ%d → たけ%d 足もと y=%d" % (i + 1, nh, sita))

    p = os.path.join(HERE, "ref", "codex", "hashiri_shomen_8.png")
    out.save(p)
    print("かいた:", p, out.size)

    if "--miru" in sys.argv:
        return

    # いまの シートの row0..7 col0 を 差しかえた ものを 作る
    base = Image.open(os.path.join(SPR, "marisa_walk.png")).convert("RGBA")
    sheet = base.copy()
    for i in range(8):
        koma = out.crop((i * CW, 0, (i + 1) * CW, CH))
        sheet.paste(Image.new("RGBA", (CW, CH), (0, 0, 0, 0)), (0, i * CH))
        sheet.paste(koma, (0, i * CH), koma)
    q = os.path.join(SPR, "marisa_codex.png")
    sheet.save(q)
    print("かいた:", q, sheet.size, "（col0 の row0..7 だけ 差しかえ）")


if __name__ == "__main__":
    main()
