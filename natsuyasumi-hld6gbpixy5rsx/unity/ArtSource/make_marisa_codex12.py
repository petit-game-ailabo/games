# Codex の 12コマ（4列x3行）を シートに 入れる（2026-09-06・二版）。
#
# 一版（8コマ）との ちがい：
#  ★**沈みが 無い**ので、大きさは **黒い 帽子の つばの はば**で コマごとに そろえる。
#    一版は 上下動を 残す ために 共通倍率しか つかえず、2歩めが 3.5% 大きい まま 入った（D-235）。
#  ★縦は **足もとでは なく 帽子で そろえる。**
#    足で そろえると、ひざを 抱えた 浮きの コマで **体ぜんぶが 沈む**（実測 44px）。
#  ★**手足が 大きく 動く コマは 使わない。**本人「手が大きく動きすぎたり、
#    足が大きく上がりすぎてる。そこまで手足が大きく動いてない数枚に絞って」。
#    実測：浮きの コマ(5,11)は 足が **47〜49px＝身長の 15%** 上がる＝跳んで いる。
#    TSUKAU で えらぶ。となりの コマとの 差の むらも これで 3.81 → 1.31 に なる。
#
# run: python unity/ArtSource/make_marisa_codex12.py
import os
import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
SPR = os.path.join(HERE, "..", "Assets", "Art", "Sprites")
MOTO = os.path.join(HERE, "ref", "codex", "hashiri_shomen_12_raw.png")
CW, CH = 224, 336
TAKE, JIMEN = 312.0, 332
UKI = {5, 11}          # 両足が 地から はなれる コマ
UKI_PX = 6             # 浮かせる 量（身長の 2%）。もとの 絵の 48px は 跳びすぎ

# ★つかう コマ。1歩め(1..6) と 2歩め(7..12) から **同じ ところを 取る**。
#   かたっぽの 歩だけ コマが 多いと **びっこを ひいて 見える**。
#   本人「膝もっと低いやつでいいよ。9番って奴かな、これが要らないかな」
#   → 9（通過L）と、その 左右の 相手 3（通過R）を 外す。**この 2枚だけが ひざを 上げる。**
#   のこりは 接地(1,7) と 支持(2,8)。となりの コマとの 差の むらは **1.30**（試した 中で 最小）
TSUKAU = [1, 2, 7, 8]


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
    # ★帽子は 黒。髪が まざらない ように 色で 拾う。**帽子は かたい＝ものさしに なる**
    kuro = (R < 95) & (G < 95) & (B < 95) & fg
    rows, cols = kugiri(fg.any(1)), kugiri(fg.any(0))
    if len(rows) != 3 or len(cols) != 4:
        print("!! 4x3 に ならない: 行", len(rows), "列", len(cols))
        return
    src = Image.fromarray(np.dstack([a, np.where(fg, 255, 0)]).astype(np.uint8))

    K = []
    for ri, (y0, y1) in enumerate(rows):
        for ci, (x0, x1) in enumerate(cols):
            s = fg[y0:y1 + 1, x0:x1 + 1]
            k = kuro[y0:y1 + 1, x0:x1 + 1]
            ys, xs = np.nonzero(s)
            t, b = int(ys.min()), int(ys.max())
            kt = k[t:t + int((b - t) * 0.42), :]
            w = kt.sum(1)
            ai = int(np.argmax(w))
            kx = np.nonzero(kt[ai])[0]
            K.append(dict(n=ri * 4 + ci + 1, x0=x0, y0=y0, t=t, b=b, ty=t + ai, tw=int(w.max()),
                          cx=float(kx.min() + kx.max()) / 2.0, l=int(xs.min()), r=int(xs.max())))
    tw = np.array([d['tw'] for d in K], float)
    hosei = tw.mean() / tw
    print("つばの はばの ブレ %.1f%%（コマごとに そろえる）" % ((tw.max() / tw.min() - 1) * 100))
    tb = np.array([(d['b'] - d['ty']) for d in K], float) * hosei
    jiku = float(np.median([tb[i] for i in range(12) if K[i]['n'] not in UKI]))
    bai = (TAKE * 0.855) / jiku
    tsuba_y = JIMEN - jiku * bai
    print("倍率 %.4f  つばの 基準 y=%.0f" % (bai, tsuba_y))

    sheet = Image.open(os.path.join(SPR, "marisa_codex.png")).convert("RGBA")
    for out_i, n in enumerate(TSUKAU):
        d = K[n - 1]
        b = bai * hosei[n - 1]
        piece = src.crop((d['x0'] + d['l'], d['y0'] + d['t'], d['x0'] + d['r'] + 1, d['y0'] + d['b'] + 1))
        nw = max(1, int(round((d['r'] - d['l'] + 1) * b)))
        nh = max(1, int(round((d['b'] - d['t'] + 1) * b)))
        piece = piece.resize((nw, nh), Image.LANCZOS)
        ty = tsuba_y - (UKI_PX if n in UKI else 0)
        oy = int(round(ty - (d['ty'] - d['t']) * b))
        # ★足が 地面より 下に なったら 押しあげる。**コマの 高さは 336 しか 無い**ので、
        #   はみ出すと **足が 切れる**（実測 koma8 は 341＝5px 切れた）。
        #   帽子で そろえるのが 本すじ だが、切れる くらいなら 頭を 数px 動かす 方が まし
        if oy + nh > JIMEN:
            print("    ! 足が %dpx 下がるので 押しあげる" % (oy + nh - JIMEN))
            oy = JIMEN - nh
        ox = int(round(CW * 0.5 - (d['cx'] - d['l']) * b))
        sheet.paste(Image.new("RGBA", (CW, CH), (0, 0, 0, 0)), (0, out_i * CH))
        sheet.alpha_composite(piece, (ox, out_i * CH + oy))
        print("  行%d ← コマ%2d  たけ%3d 足もと y=%d" % (out_i, n, nh, oy + nh))
    p = os.path.join(SPR, "marisa_codex.png")
    sheet.save(p)
    print("かいた:", p, "（col0 の 行0..%d）" % (len(TSUKAU) - 1))


if __name__ == "__main__":
    main()
