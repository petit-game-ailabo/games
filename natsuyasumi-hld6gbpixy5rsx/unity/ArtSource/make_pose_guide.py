# 走りの コマの「下敷き」を 作る（2026-09-06）。
#
# WHY: いまの 正面の 走り 8コマは **上半身が ぜんぶ 同じで 脚だけ ちがう**。
#      高さも 全コマ y22..332 で 固定＝**上下の はずみが ゼロ**。
#      腕も 髪も 動いて いない。つまり「走って いる 絵が 8枚」では なく
#      「立ち絵の 脚だけ 差しかえた ものが 8枚」だった。
#
#      絵を たのむ ときに「走って いる 感じで」と 言うと また 同じ ものが 来る。
#      走りの コマは **決まった 構造**が ある ので、それを 絵にして 渡す。
#
# 走り 1歩＝4コマ。2歩で 8コマ（左右で 役目が 入れかわる だけ・鏡では ない）
#   1 コンタクト … 足が 地に つく。体は 中くらいの 高さ
#   2 ダウン     … 体が **いちばん 低い**。ささえる ひざが 深く 曲がる
#   3 パッシング … 体が 上がる とちゅう。うしろの 脚が よこを 通る
#   4 アップ     … 体が **いちばん 高い**。**両足とも 地から はなれる**
#
# 正面の 走りは 横と ちがう：
#   ・脚の 前後は **奥ゆき**に なる → 前の 足は **大きく 低く**、うしろの 足は **小さく 高く**
#   ・腕は 体の 前を よこぎる。前に 出た 腕は **大きく** 見える
#   ・肩の 線と 腰の 線は **逆に かたむく**
#
# run: python unity/ArtSource/make_pose_guide.py
import os
import math
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
CW, CH = 224, 336
COLS, ROWS = 4, 2
JIMEN = 332                    # 地めんの 線
SEN = (40, 40, 48)
USUI = (150, 160, 175)
AKA = (200, 60, 60)
AO = (60, 110, 200)

# コマごと：(名まえ, 頭の てっぺんの y, 手前の脚, おくの脚, 手前の腕, おくの腕)
#   脚・腕の 値は -1..+1（+1＝いちばん 手前／前）
KOMA = [
    ("1 contact R", 22, +0.9, -0.9, -0.8, +0.8),
    ("2 down R",    27, +0.4, -0.7, -0.4, +0.4),
    ("3 passing R", 18, -0.1, +0.1, +0.0, +0.0),
    ("4 up R",      13, -0.8, +0.9, +0.7, -0.7),
    ("5 contact L", 22, -0.9, +0.9, +0.8, -0.8),
    ("6 down L",    27, -0.7, +0.4, +0.4, -0.4),
    ("7 passing L", 18, +0.1, -0.1, +0.0, +0.0),
    ("8 up L",      13, +0.9, -0.8, -0.7, +0.7),
]


def hito(d, cx, atama_y, ashiA, ashiB, udeA, udeB, uku):
    """棒人間 1体。ashi/ude は -1..+1（+1＝手前へ 出て いる）"""
    # ★実寸に 合わせる。立ち絵は 頭 y22..足 y332＝たけ 310px。
    #   ここを 合わせないと 「下敷き」に ならない（2026-09-06）
    atama_r = 27
    kubi = atama_y + atama_r * 2 + 2
    kata = kubi + 12
    koshi = atama_y + 178
    hiza_len, sune_len = 66, 66

    # 肩と 腰は **逆に かたむく**
    katak = (udeA - udeB) * 5.0
    koshik = (ashiA - ashiB) * -5.0

    # 頭・胴
    d.ellipse([cx - atama_r, atama_y, cx + atama_r, atama_y + atama_r * 2], outline=SEN, width=4)
    d.line([(cx, kubi), (cx, koshi)], fill=SEN, width=6)
    d.line([(cx - 30, kata - katak), (cx + 30, kata + katak)], fill=SEN, width=5)
    d.line([(cx - 22, koshi - koshik), (cx + 22, koshi + koshik)], fill=SEN, width=5)

    def ashi(t, iro, futo):
        """t=+1 手前(大きく 低く) / t=-1 おく(小さく 高く)"""
        sg = 1.0 if iro is AKA else -1.0
        hx = cx + sg * 16
        hy = koshi + koshik * sg
        # 手前ほど 大きく 見える＝長く 描く
        bai = 1.0 + 0.22 * t
        # ひざの 位置：前に 出す ほど 手前(下)へ
        kx = hx + sg * 6 + t * 10
        ky = hy + hiza_len * bai * (1.0 - 0.18 * abs(t))
        fx = kx + t * 16
        fy = ky + sune_len * bai
        fy = min(fy, JIMEN - uku)
        d.line([(hx, hy), (kx, ky)], fill=iro, width=int(futo * bai))
        d.line([(kx, ky), (fx, fy)], fill=iro, width=int(futo * bai))
        d.ellipse([fx - 11 * bai, fy - 6, fx + 11 * bai, fy + 6], fill=iro)
        return fy

    def ude(t, iro, sg):
        sx = cx + sg * 30
        sy = kata + katak * sg
        bai = 1.0 + 0.22 * t
        ex = sx + sg * 4 + t * 14
        ey = sy + 52 * bai
        wx = ex - sg * 10 + t * 12
        wy = ey + 46 * bai * (1.0 - 0.25 * abs(t))
        d.line([(sx, sy), (ex, ey)], fill=iro, width=int(7 * bai))
        d.line([(ex, ey), (wx, wy)], fill=iro, width=int(7 * bai))
        d.ellipse([wx - 7, wy - 7, wx + 7, wy + 7], fill=iro)

    ude(udeA, AO, -1)
    ude(udeB, AO, +1)
    ashi(ashiA, AKA, 11)
    ashi(ashiB, USUI, 10)


def main():
    W, H = CW * COLS, CH * ROWS
    im = Image.new("RGB", (W, H), (252, 252, 250))
    d = ImageDraw.Draw(im)
    for i, (nm, ay, aA, aB, uA, uB) in enumerate(KOMA):
        ox = (i % COLS) * CW
        oy = (i // COLS) * CH
        c = ImageDraw.Draw(im)
        # わく
        c.rectangle([ox, oy, ox + CW - 1, oy + CH - 1], outline=(210, 212, 216))
        # 地めん
        c.line([(ox, oy + JIMEN), (ox + CW, oy + JIMEN)], fill=(190, 190, 195), width=2)
        # 立ち の 高さ（くらべる ため）
        c.line([(ox, oy + 22), (ox + CW, oy + 22)], fill=(225, 228, 232), width=1)
        uku = (22 - ay) if ay < 22 else 0          # アップの コマは 足が 浮く
        sub = Image.new("RGB", (CW, CH), (252, 252, 250))
        sd = ImageDraw.Draw(sub)
        sd.line([(0, JIMEN), (CW, JIMEN)], fill=(120, 170, 120), width=2)
        sd.line([(0, 22), (CW, 22)], fill=(228, 230, 234), width=1)
        hito(sd, CW // 2, ay, aA, aB, uA, uB, uku)
        sd.text((6, CH - 26), nm, fill=(30, 30, 30))
        sd.text((6, CH - 14), "atama y=%d / jimen y=332 / uki=%dpx" % (ay, uku), fill=(120, 120, 128))
        im.paste(sub, (ox, oy))
    p = os.path.join(HERE, "hashiri_guide.png")
    im.save(p)
    print("かいた:", p, im.size)


if __name__ == "__main__":
    main()
