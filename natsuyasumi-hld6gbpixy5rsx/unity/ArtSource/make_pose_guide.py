# 走りの コマの「下敷き」を 作る（2026-09-06 二版）。
#
# ★一版（沈み ありの 8コマ）は **やめた。**
#   本人「沈みはいらない、そんな走り方してる人間はいないよ。人の走り方はそんなに
#   上がったり下がったりしない」。そのとおり で、走りの 重心の 上下は 身長の 3%ほど。
#   しかも **正面からは ほとんど 見えない**。一版に 書いた 98/96/101/104 は
#   **横から見た 走りの 教科書図**で、正面には 効かない うえに 漫画的に なる。
#
# 二版の 考えかた：**上下は ほぼ 動かさない。動きは 脚・腕・ひねりで 出す。**
#   正面の 走りで 実際に 目に 見える のは この 4つ：
#     ① どちらの ひざが 上がって いるか と その 高さ
#     ② どちらの 腕が 前か（脚と 逆）・肘は 直角ちかく
#     ③ 肩の線と 腰の線の ひねり（逆向き）
#     ④ 髪と スカートの 遅れ
#   ★奥ゆき：前に 出た 脚は **手前＝大きく 低く**、後ろの 脚は **奥＝小さく 高く**。
#     正面の 走りは これが 主役。左右の 開きでは ない。
#
# 1歩＝6コマ。2歩で 12コマ（左右で 役目が 入れかわる だけ・鏡では ない）
#   1 接地   … 足の 裏が 地に つく
#   2 支持   … 体重が その 足の 真上に 乗る
#   3 通過   … ふり脚の ひざが ささえ脚の 横を 通る（左右の 差が いちばん 小さい）
#   4 蹴り   … かかとが 浮き、ささえ脚が 後ろへ 伸びきる。ふり脚の ひざが 前で 高い
#   5 浮き   … **両足とも 地から はなれる。頭が いちばん 高い（といっても 6px）**
#   6 降り   … ふり足が 前で 下へ おりて くる。次の 接地の 直前
#
# ★8コマ(1歩4コマ)から 12コマ(1歩6コマ)へ 増やす 理由：
#   沈みを 無くすと **上下動が 動きを 運ばなく なる**。脚と 腕だけで つなぐ には
#   1歩4コマでは 足りない（実測：となりの コマとの 差が 最大÷最小 2.66倍＝D-235）。
#
# run: python unity/ArtSource/make_pose_guide.py
import os
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
CW, CH = 224, 336
COLS, ROWS = 4, 3
JIMEN = 332                    # 地めんの 線
ATAMA = 22                     # 立ち絵の 頭の てっぺん。**ここより 下がらない**
KOSHI = 200                    # 腰の 線（そろえる ための ものさし）
SEN = (40, 40, 48)
USUI = (150, 160, 175)
AKA = (200, 60, 60)            # 右脚
AO = (60, 110, 200)            # 腕

# (名まえ, 頭の y, 右脚, 左脚, 左腕, 右腕)
#   脚 = (ひざの 高さ h, 地に ついて いるか)
#     h=+1.0 … ひざが 胸の 高さまで 上がる（正面からは **手前に 出て 大きく 見える**）
#     h= 0.0 … まっすぐ 立った 状態
#     h=-0.4 … うしろへ 伸びきる（正面からは **奥＝小さく 見える**）
#   腕 = -1..+1（+1＝前へ ふり出す）。**脚と 逆。**肘は つねに 直角ちかく
#
# ★正面の 走りで いちばん 目に つくのは **ひざの 高さの 差**。
#   左右に 開くのでは なく、**片方の ひざが 上がって 手前に 出る**。
KOMA = [
    #  名まえ        頭   右脚(h, oku, 接地)      左脚(h, oku, 接地)      左腕   右腕
    ("1 setchi R",   22, (+0.10, +0.55, True),  (+0.15, -0.85, False), +0.85, -0.85),
    ("2 shiji R",    21, ( 0.00, +0.10, True),  (+0.35, -0.45, False), +0.55, -0.55),
    ("3 tsuuka R",   20, (-0.10, -0.15, True),  (+0.60, +0.05, False), +0.15, -0.15),
    ("4 keri R",     19, (-0.35, -0.60, True),  (+0.85, +0.55, False), -0.35, +0.35),
    ("5 uki R",      16, (-0.20, -0.85, False), (+0.65, +0.85, False), -0.75, +0.75),
    ("6 ori R",      18, (+0.30, -0.55, False), (+0.20, +0.90, False), -0.55, +0.55),
    ("7 setchi L",   22, (+0.15, -0.85, False), (+0.10, +0.55, True),  -0.85, +0.85),
    ("8 shiji L",    21, (+0.35, -0.45, False), ( 0.00, +0.10, True),  -0.55, +0.55),
    ("9 tsuuka L",   20, (+0.60, +0.05, False), (-0.10, -0.15, True),  -0.15, +0.15),
    ("10 keri L",    19, (+0.85, +0.55, False), (-0.35, -0.60, True),  +0.35, -0.35),
    ("11 uki L",     16, (+0.65, +0.85, False), (-0.20, -0.85, False), +0.75, -0.75),
    ("12 ori L",     18, (+0.20, +0.90, False), (+0.30, -0.55, False), +0.55, -0.55),
]


def hito(d, cx, atama_y, ashiR, ashiL, udeL, udeR, uku):
    atama_r = 27
    kubi = atama_y + atama_r * 2 + 2
    kata = kubi + 12
    koshi = atama_y + (KOSHI - ATAMA)
    momo, sune = 66, 66

    katak = (udeL - udeR) * -5.0
    koshik = (ashiR[1] - ashiL[1]) * -5.0

    d.ellipse([cx - atama_r, atama_y, cx + atama_r, atama_y + atama_r * 2], outline=SEN, width=4)
    d.line([(cx, kubi), (cx, koshi)], fill=SEN, width=6)
    d.line([(cx - 30, kata - katak), (cx + 30, kata + katak)], fill=SEN, width=5)
    d.line([(cx - 22, koshi - koshik), (cx + 22, koshi + koshik)], fill=SEN, width=5)

    def ashi(a, iro, sg, futo):
        h, oku, setti = a
        hx = cx + sg * 18
        hy = koshi + koshik * sg
        # ★2つを 分けて 持つ：**h＝ひざの 高さ**、**oku＝手前か 奥か**。
        #   分けないと「ひざは 上がって いるが 体の うしろ」（＝降りの コマの 後ろ脚）が 描けず、
        #   接地・支持・降り が 同じ 絵に なる（一版は 実際 そう なった）
        bai = 1.0 + 0.45 * oku
        kx = hx - sg * 12 * max(h, 0.0) + oku * 6
        ky = hy + momo * bai * (1.0 - 0.80 * max(h, 0.0)) + 10 * max(-h, 0.0)
        fx = kx + sg * 2 + 16 * max(h, 0.0) + oku * 6
        fy = ky + sune * bai * (1.0 - 0.35 * max(h, 0.0)) + 8 * max(-h, 0.0)
        if setti:
            fy = JIMEN
            fx = kx + sg * 2
        else:
            fy = min(fy, JIMEN - max(uku, 6))
        w = max(4, int(futo * bai))
        d.line([(hx, hy), (kx, ky)], fill=iro, width=w)
        d.line([(kx, ky), (fx, fy)], fill=iro, width=max(3, w - 2))
        kr = 5 + 10 * max(h, 0.0) * bai
        d.ellipse([kx - kr, ky - kr, kx + kr, ky + kr], fill=iro)
        r = 10 * bai
        d.ellipse([fx - r, fy - 6 * bai, fx + r, fy + 6 * bai], fill=iro)
        if setti:
            d.line([(fx - 20, JIMEN + 4), (fx + 20, JIMEN + 4)], fill=iro, width=4)

    def ude(t, sg):
        sx = cx + sg * 30
        sy = kata + katak * sg
        bai = 1.0 + 0.22 * t
        ex = sx + sg * 3
        ey = sy + 52
        wx = ex - sg * 16 + t * 20
        wy = ey - 8 - t * 22
        d.line([(sx, sy), (ex, ey)], fill=AO, width=int(7 * bai))
        d.line([(ex, ey), (wx, wy)], fill=AO, width=int(7 * bai))
        d.ellipse([wx - 7, wy - 7, wx + 7, wy + 7], fill=AO)

    ude(udeL, -1)
    ude(udeR, +1)
    # ★奥の 脚から 先に 描く。手前の 脚が 上に 重なる
    if ashiR[1] < ashiL[1]:
        ashi(ashiR, USUI, +1, 11); ashi(ashiL, AKA, -1, 11)
    else:
        ashi(ashiL, USUI, -1, 11); ashi(ashiR, AKA, +1, 11)


def main():
    im = Image.new("RGB", (CW * COLS, CH * ROWS), (252, 252, 250))
    for i, (nm, ay, aR, aL, uL, uR) in enumerate(KOMA):
        uku = ATAMA - ay if ay < ATAMA else 0     # 頭が 上がった ぶん 足も 浮く
        sub = Image.new("RGB", (CW, CH), (252, 252, 250))
        sd = ImageDraw.Draw(sub)
        # ★ものさしの 線。**12コマ ぜんぶ この 線に 合わせる**
        sd.line([(0, JIMEN), (CW, JIMEN)], fill=(90, 150, 90), width=3)     # 地めん
        sd.line([(0, ATAMA), (CW, ATAMA)], fill=(200, 80, 80), width=2)     # 頭（これより 下がらない）
        sd.line([(0, KOSHI), (CW, KOSHI)], fill=(210, 190, 120), width=2)   # 腰
        hito(sd, CW // 2, ay, aR, aL, uL, uR, uku)
        sd.text((6, CH - 26), nm, fill=(30, 30, 30))
        setti = ("R" if aR[2] else "") + ("L" if aL[2] else "")
        sd.text((6, CH - 14), "hiza R%+.2f L%+.2f / oku R%+.2f L%+.2f / jimen=%s"
                % (aR[0], aL[0], aR[1], aL[1], setti if setti else "nashi"),
                fill=(120, 120, 128))
        sd.rectangle([0, 0, CW - 1, CH - 1], outline=(205, 208, 212))
        im.paste(sub, ((i % COLS) * CW, (i // COLS) * CH))
    p = os.path.join(HERE, "hashiri_guide.png")
    im.save(p)
    print("かいた:", p, im.size, "（1歩6コマ x 2歩＝12コマ）")


if __name__ == "__main__":
    main()
