# 8方向の 下敷きを 作る（2026-09-06・D-246）。
#
# WHY: 本人「別の向きの絵を作るときには、元の絵のサイズ感と変わらないことを
#   プロンプトで指定して。今だと左右に向いた瞬間に顔のサイズが変わってるのがおかしい」。
#   実測：**帽子の つばの 差し渡しが 向きに よって 110〜129px＝17.3% ちがう。**
#   頭の てっぺんの 位置も 21〜32px と 11px ずれて いる。
#
# 言葉で「同じ 大きさで」と 書くだけでは そろわない（一版で 実証ずみ）。
#   → **いまの 正面の 立ち絵を うすく 敷いた わく**を 8つ ならべて 渡す。
#     その 上から 描いて もらえば、大きさは 自動で そろう。
#   → 目じるしの 横線を 5本 引く：帽子の てっぺん／つばの 線／肩／腰／地めん。
#
# run: python unity/ArtSource/make_muki_guide.py
import os
import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
SPR = os.path.join(HERE, "..", "Assets", "Art", "Sprites")
CW, CH = 224, 336
MUKI = ["1 shomen", "2 hidari-mae", "3 hidari", "4 hidari-oku",
        "5 oku", "6 migi-oku", "7 migi", "8 migi-mae"]


def main():
    im = Image.open(os.path.join(SPR, "marisa_walk.png")).convert("RGBA")
    tachi = im.crop((0, 8 * CH, CW, 9 * CH))          # ★正面の 本物の 立ち絵
    a = np.asarray(tachi)
    al = a[..., 3] > 96
    ys, xs = np.nonzero(al)
    atama, ashi = int(ys.min()), int(ys.max())
    # 帽子の つばの 線＝上から 130px の 中で 黒が いちばん 横に 広い 行
    R, G, B = a[..., 0].astype(int), a[..., 1].astype(int), a[..., 2].astype(int)
    kuro = (R < 95) & (G < 95) & (B < 95) & al
    besty, bestw = atama, 0
    for y in range(atama, atama + 130):
        x = np.nonzero(kuro[y])[0]
        if len(x) > 3 and int(x.max() - x.min() + 1) > bestw:
            bestw, besty = int(x.max() - x.min() + 1), y
    kata = atama + int((ashi - atama) * 0.37)
    koshi = atama + int((ashi - atama) * 0.55)
    print("あたま y=%d / つば y=%d はば %d / 肩 y=%d / 腰 y=%d / 地めん y=%d"
          % (atama, besty, bestw, kata, koshi, ashi))

    # うすく する
    usui = tachi.copy()
    d = np.asarray(usui).copy()
    d[..., 3] = (d[..., 3] * 0.22).astype(np.uint8)
    usui = Image.fromarray(d)

    out = Image.new("RGB", (CW * 4, CH * 2), (252, 252, 250))
    for i, na in enumerate(MUKI):
        sub = Image.new("RGB", (CW, CH), (252, 252, 250))
        sd = ImageDraw.Draw(sub)
        for y, iro, w in ((atama, (200, 80, 80), 2), (besty, (60, 110, 200), 2),
                          (kata, (210, 190, 120), 1), (koshi, (210, 190, 120), 1),
                          (ashi, (90, 150, 90), 3)):
            sd.line([(0, y), (CW, y)], fill=iro, width=w)
        # つばの 差し渡しを 縦線で 示す
        sd.line([(CW // 2 - bestw // 2, besty - 8), (CW // 2 - bestw // 2, besty + 8)],
                fill=(60, 110, 200), width=3)
        sd.line([(CW // 2 + bestw // 2, besty - 8), (CW // 2 + bestw // 2, besty + 8)],
                fill=(60, 110, 200), width=3)
        sub.paste(usui, (0, 0), usui)
        sd.text((6, 6), na, fill=(30, 30, 30))
        sd.text((6, CH - 14), "tsuba haba %d / takasa %d" % (bestw, ashi - atama + 1),
                fill=(120, 120, 128))
        sd.rectangle([0, 0, CW - 1, CH - 1], outline=(205, 208, 212))
        out.paste(sub, ((i % 4) * CW, (i // 4) * CH))
    p = os.path.join(HERE, "muki_guide.png")
    out.save(p)
    print("かいた:", p, out.size)


if __name__ == "__main__":
    main()
