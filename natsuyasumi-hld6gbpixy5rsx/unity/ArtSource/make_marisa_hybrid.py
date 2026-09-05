# 主人公の シート＝**3Dの 体 ＋ 2Dの 頭と 髪**（2026-09-06）
#
# WHY: 本人「3Dだと 可愛さが 失われてる」「ギルティの 良さを 取り入れて
#      キャラクターの アニメ調(もともとの画像の良さ)を 残しながら 3Dで 動かせないか」。
#
# ギルティギアXrd の GDC2015（本村）で 何度も 言われて いるのは
#   **顔は 3Dの 形で 作らない。目・まゆ・口は 描いた 絵**。
# こちらも 同じ 結論に なった。ただし
#   ・私が Python で 描いた 顔（円と 円弧）は 60pxに 縮むと 「へのへのもへじ」に なる
#   ・`marisa_walk.png` には **8方向ぶんの よく 描けた 顔が すでに ある**
# ので、**その 頭を そのまま 3Dの 体に のせる**。
#
# 首だけで 切ると **髪の 色と 描きかたが 首の 線で 変わって 見える** ので、
# 2Dからは **頭 ＋ 髪ぜんぶ** を 取る（髪は 金色で 見わける。実測 G-B: 髪85／肌11）。
#
# もと：
#   Assets/Art/Sprites/marisa_walk.png    手描き 2D（8列x10行）
#   Assets/Art/Sprites/marisa_meshy.png   Meshy の 3Dを 焼いた もの（同じ ならび）
# 出し先：
#   Assets/Art/Sprites/marisa_hybrid.png
#
# run: python unity/ArtSource/make_marisa_hybrid.py
import os
import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
SPR = os.path.join(HERE, "..", "Assets", "Art", "Sprites")
CW, CH, COLS, ROWS = 224, 336, 8, 10

# 首の 高さ。目盛りを 引いて 読んだ 値（2Dも 3Dも ここが 首）
KUBI = 116
# 髪の 見わけ（実測）。肌は G-B が 11、髪は 85。あいだの 58 で 切る
KAMI_GB, KAMI_RB = 58, 95


def cell(im, c, r):
    return im.crop((c * CW, r * CH, (c + 1) * CW, (r + 1) * CH))


def joho(a):
    """絵の てっぺんと、頭の 横の まん中"""
    A = np.asarray(a)[..., 3] > 128
    ys = np.nonzero(A.any(1))[0]
    if len(ys) == 0:
        return 0, CW * 0.5
    top = ys.min()
    xs = np.nonzero(A[top:KUBI].any(0))[0]
    if len(xs) == 0:
        return top, CW * 0.5
    return top, (xs.min() + xs.max()) * 0.5


def tsunagari(m, y):
    """m の うち、y より 上の ところと つながって いる ぶん だけ 残す。
    scipy が 無い ので 塗りつぶしを 自分で 書く（広げては 重ねる の くり返し）。"""
    seed = m.copy()
    seed[y:, :] = False
    if not seed.any():
        return m
    cur = seed
    while True:
        nxt = cur.copy()
        nxt[1:, :] |= cur[:-1, :]
        nxt[:-1, :] |= cur[1:, :]
        nxt[:, 1:] |= cur[:, :-1]
        nxt[:, :-1] |= cur[:, 1:]
        nxt &= m
        if nxt.sum() == cur.sum():
            return nxt
        cur = nxt


def noseru(a2, a3):
    """a3（3Dの 体）に a2（2Dの 頭と 髪）を のせる"""
    t2, c2 = joho(a2)
    t3, c3 = joho(a3)
    s = (KUBI - t3) / float(max(KUBI - t2, 1))

    A = np.asarray(a2).astype(np.float64)
    R, G, B, AL = A[..., 0], A[..., 1], A[..., 2], A[..., 3]
    kami = (AL > 128) & (R > 170) & ((G - B) > KAMI_GB) & ((R - B) > KAMI_RB)
    yy = np.arange(CH)[:, None]
    m = (AL > 128) & ((yy < KUBI - 6) | kami)
    # ★頭と つながって いない ところは すてる。
    #   髪の 色に 近い 点（三つ編みの ひも・フリルの つや）が 足もとに 飛んで、
    #   金色の 破片が 宙に 浮いて いた（2026-09-06）
    m = tsunagari(m, KUBI - 6)

    # ふちを すこし 広げて ぼかす（3Dの 体との あいだに すき間を 作らない）
    mm = Image.fromarray((m * 255).astype(np.uint8))
    mm = mm.filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.GaussianBlur(1.2))
    al = np.minimum(np.asarray(mm, dtype=np.float64), AL)

    src = a2.copy()
    src.putalpha(Image.fromarray(al.astype(np.uint8)))
    src = src.resize((int(round(CW * s)), int(round(CH * s))), Image.LANCZOS)

    ox = int(round(c3 - c2 * s))
    oy = int(round(t3 - t2 * s))
    out = a3.copy()
    tmp = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
    tmp.paste(src, (ox, oy), src)
    out.alpha_composite(tmp)
    return out


def main():
    w2 = Image.open(os.path.join(SPR, "marisa_walk.png")).convert("RGBA")
    m3 = Image.open(os.path.join(SPR, "marisa_meshy.png")).convert("RGBA")
    if w2.size != (CW * COLS, CH * ROWS) or m3.size != (CW * COLS, CH * ROWS):
        raise SystemExit("シートの 大きさが ちがう: %s / %s" % (w2.size, m3.size))

    sheet = Image.new("RGBA", (CW * COLS, CH * ROWS), (0, 0, 0, 0))
    for r in range(ROWS):
        for c in range(COLS):
            # 行も そろえる＝走りの コマは 走りの 頭が のる（首の かたむきが 合う）
            sheet.paste(noseru(cell(w2, c, r), cell(m3, c, r)), (c * CW, r * CH))
        print("row", r, "ok", flush=True)

    p = os.path.join(SPR, "marisa_hybrid.png")
    sheet.save(p)
    A = np.asarray(sheet)[..., 3]
    ys, _ = np.nonzero(A[8 * CH:9 * CH, 0:CW] > 128)
    print("かいた:", p, sheet.size, " row8col0 y %d..%d" % (ys.min(), ys.max()))


if __name__ == "__main__":
    main()
