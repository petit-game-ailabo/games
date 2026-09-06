# 四版の 走り 8コマ（4列x2行）を シートに 入れる（2026-09-06・D-248）。
#
# 二版（12コマ）との ちがい：
#  ★五版から **浮きの コマが 2枚 入った**（実測 15〜17px）。
#    足もとで そろえると **浮きが 消えて、しかも 体が 15px 沈む**。
#    → 縦は **帽子の つば**で そろえ、**地に ついて いる コマの 足が 332 に 来る**ように 置く。
#    浮きの コマの 足は そのぶん 上に 残る。
#  ★大きさは **黒い 帽子の つばの 差し渡し**で そろえる。
#    つばは 円 なので どの 向きから 見ても 横はばが 変わらない＝**向きを またいでも つかえる**。
#    実測：もらった 8コマの つばは 217〜218px（ブレ 0.5%）。すでに そろって いる。
#
# run: python unity/ArtSource/make_marisa_hashiri8.py
import os
import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
SPR = os.path.join(HERE, "..", "Assets", "Art", "Sprites")
MOTO = os.path.join(HERE, "ref", "codex", "hashiri_shomen_8q_raw.png")
CW, CH = 224, 336
TAKE, JIMEN = 312.0, 332
RETSU = 0            # 入れる 列（0＝正面）


def kugiri(v, mn=60):
    out, s = [], None
    for i, x in enumerate(v):
        if x and s is None:
            s = i
        if not x and s is not None:
            if i - s >= mn:
                out.append((s, i - 1))
            s = None
    if s is not None and len(v) - s >= mn:
        out.append((s, len(v) - 1))
    return out


def main():
    im = Image.open(MOTO).convert("RGB")
    a = np.asarray(im).astype(int)
    R, G, B = a[..., 0], a[..., 1], a[..., 2]
    fg = ~((R > 150) & (B > 150) & (G < 120) & ((R - G) > 60) & ((B - G) > 60))
    fi = Image.fromarray((fg * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(3))
    fg = np.asarray(fi.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MaxFilter(3))) > 128
    kuro = (R < 95) & (G < 95) & (B < 95) & fg
    rows, cols = kugiri(fg.any(1)), kugiri(fg.any(0))
    if len(rows) != 2 or len(cols) != 4:
        print("!! 4x2 に ならない: 行", len(rows), "列", len(cols))
        return
    src = Image.fromarray(np.dstack([a, np.where(fg, 255, 0)]).astype(np.uint8))

    K = []
    for ri, (y0, y1) in enumerate(rows):
        for ci, (x0, x1) in enumerate(cols):
            s = fg[y0:y1 + 1, x0:x1 + 1]
            k = kuro[y0:y1 + 1, x0:x1 + 1]
            ys, xs = np.nonzero(s)
            t, b = int(ys.min()), int(ys.max())
            haba, tsuba, sx = 0, t, (0, 0)
            for y in range(t, t + int((b - t) * 0.45)):
                x = np.nonzero(k[y])[0]
                if len(x) > 3 and int(x.max() - x.min() + 1) > haba:
                    haba, tsuba, sx = int(x.max() - x.min() + 1), y, (int(x.min()), int(x.max()))
            K.append(dict(n=ri * 4 + ci + 1, x0=x0, y0=y0, t=t, b=b, tw=haba, ty=tsuba,
                          cx=(sx[0] + sx[1]) / 2.0, l=int(xs.min()), r=int(xs.max())))
    tw = np.array([d['tw'] for d in K], float)
    hosei = tw.mean() / tw
    print("つばの はば %d〜%d（ブレ %.1f%%）" % (tw.min(), tw.max(), (tw.max() / tw.min() - 1) * 100))
    # ★地に ついて いる コマ＝その 行の いちばん 下から 5px 以内
    jimen = {}
    for d in K:
        jimen[d['y0']] = max(jimen.get(d['y0'], 0), d['b'])
    tsuku = [d['b'] >= jimen[d['y0']] - 5 for d in K]
    print("地に ついて いる コマ:", [i + 1 for i, t in enumerate(tsuku) if t])
    take = np.array([(d['b'] - d['t'] + 1) for i, d in enumerate(K) if tsuku[i]], float)            * np.array([hosei[i] for i in range(len(K)) if tsuku[i]])
    bai = TAKE / float(np.median(take))
    # ★つばから 足もとまで（地に ついて いる コマの 中央値）で つばの 基準を 決める
    tb = np.median([(K[i]['b'] - K[i]['ty']) * hosei[i] * bai for i in range(len(K)) if tsuku[i]])
    TSUBA_Y = JIMEN - tb
    print("倍率 %.4f  つばの 基準 y=%.0f" % (bai, TSUBA_Y))

    sheet = Image.open(os.path.join(SPR, "marisa_codex.png")).convert("RGBA")
    for i, d in enumerate(K):
        b = bai * hosei[i]
        piece = src.crop((d['x0'] + d['l'], d['y0'] + d['t'], d['x0'] + d['r'] + 1, d['y0'] + d['b'] + 1))
        nw = max(1, int(round((d['r'] - d['l'] + 1) * b)))
        nh = max(1, int(round((d['b'] - d['t'] + 1) * b)))
        piece = piece.resize((nw, nh), Image.LANCZOS)
        oy = int(round(TSUBA_Y - (d['ty'] - d['t']) * b))   # ★帽子の つばで そろえる
        # ★横は **帽子の つばの まん中**で そろえる。体ぜんぶの まん中だと
        #   足を 前に 出した コマで 体が 横に ずれる
        ox = int(round(CW * 0.5 - (d['cx'] - d['l']) * b))
        sheet.paste(Image.new("RGBA", (CW, CH), (0, 0, 0, 0)), (RETSU * CW, i * CH))
        sheet.alpha_composite(piece, (RETSU * CW + ox, i * CH + oy))
        print("  行%d ← コマ%d  たけ%3d 足もと y=%d" % (i, d['n'], nh, oy + nh))
    p = os.path.join(SPR, "marisa_codex.png")
    sheet.save(p)
    print("かいた:", p)

    # ---- 検査
    al = np.asarray(sheet)[..., 3] > 96
    def cel(r):
        return al[r * CH:(r + 1) * CH, RETSU * CW:(RETSU + 1) * CW]
    sa, ue, sh = [], [], []
    for i in range(8):
        x, y = cel(i), cel((i + 1) % 8)
        sa.append(int(np.logical_xor(x, y).sum()))
        ue.append(int(np.logical_xor(x[:236], y[:236]).sum()))
        sh.append(int(np.logical_xor(x[236:], y[236:]).sum()))
    print("\n1コマあたりの 変化 %.0f（上半身 %.0f ／ 脚 %.0f）  めやす 1800〜3000・上<下"
          % (sum(sa) / 8, sum(ue) / 8, sum(sh) / 8))
    print("となりとの 差:", sa, " むら %.2f" % (max(sa) / min(sa)))
    atama = [int(np.nonzero(cel(i).any(1))[0].min()) for i in range(8)]
    print("頭の てっぺん:", atama, " ブレ %dpx（めやす 5px）" % (max(atama) - min(atama)))


if __name__ == "__main__":
    main()
