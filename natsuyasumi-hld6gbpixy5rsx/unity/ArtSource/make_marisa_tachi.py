# 正面いがいの「立ち」を、走りの コマの 中から **いちばん 立ちに 近い もの**に 差しかえる
# （2026-09-06・D-240）。
#
# WHY: 本人「奥向きの棒立ちがない。左右も。」——調べたら そのとおり だった。
#   `marisa_walk.png` は 8列x10行で、行8＝立ち・行9＝まばたき の はずが、
#   **col1〜col7 は 行8が 走りの コマの 完全な コピー**（違い 0画素）で、
#   **行9は 行8と これも 完全に 同じ**。つまり
#   **正面いがいに 立ち絵も まばたきも 無い。** 止まると 走りの 途中で 固まる。
#
# ここで やる こと（絵は 増やせない ので できる 範囲の 改善）：
#   走り 8コマの うち **足が いちばん そろって いて 地に ついて いる** コマを えらび、
#   それを 行8・行9 に 入れる。棒立ちには ならないが、
#   **足が 大きく 開いた まま 止まる** のは 直る。
#
# ★col0（正面）は 触らない。**あそこだけ 本物の 立ち絵と まばたきが ある**
#   （行8と 行9の 違いが 70画素＝目を とじた ぶん）。上書きすると 悪く なる。
#
# ★本当の 直しは **8方向ぶんの 立ち絵を 描く こと。**走りより ずっと やさしい 依頼。
#
# run: python unity/ArtSource/make_marisa_tachi.py
import os
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
SPR = os.path.join(HERE, "..", "Assets", "Art", "Sprites")
CW, CH = 224, 336
JIMEN = 332
TACHI, MABATAKI = 8, 9


def erabu(al, c):
    """走り 8コマから いちばん 立ちに 近い 行を えらぶ"""
    ichiban, ten = 0, None
    for r in range(8):
        x = al[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW]
        obi = x[JIMEN - 36:JIMEN + 4, :]              # 足もとの 帯
        xs = np.nonzero(obi.any(0))[0]
        haba = (xs.max() - xs.min() + 1) if len(xs) else 999
        soko = int(np.nonzero(x.any(1))[0].max())
        # 足の はば＋**浮いて いる ぶんは 2倍で 罰する**（浮いた コマで 止まると 変）
        t = haba + (JIMEN - soko) * 2
        if ten is None or t < ten:
            ten, ichiban = t, r
    return ichiban, ten


def main():
    p = os.path.join(SPR, "marisa_codex.png")
    im = Image.open(p).convert("RGBA")
    al = np.asarray(im)[..., 3] > 96
    for c in range(1, 8):                              # ★col0 は 触らない
        r, t = erabu(al, c)
        koma = im.crop((c * CW, r * CH, (c + 1) * CW, (r + 1) * CH))
        for gyo in (TACHI, MABATAKI):
            im.paste(Image.new("RGBA", (CW, CH), (0, 0, 0, 0)), (c * CW, gyo * CH))
            im.paste(koma, (c * CW, gyo * CH), koma)
        print("col%d: 行%d を 立ちに した（点 %d）" % (c, r, t))
    im.save(p)
    print("かいた:", p)


if __name__ == "__main__":
    main()
