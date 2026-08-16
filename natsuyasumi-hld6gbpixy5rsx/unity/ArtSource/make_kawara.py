# 瓦(かわら)と 漆喰(しっくい)を **手で 描く**。
#
# ★なぜ 写真から 作るのを やめたか（2026-08-17）
#   これまでの roof_tile.png は ambientCG の RoofingTiles001 を 48px に 落とした もの。
#   拡大して 見たら **暗くて 模様の 読めない 泥**に なって いた。だから 屋根は
#   画面で いちばん 大きい 面 なのに 灰色の かたまりに しか 見えなかった
#  （本人「屋根が大きい割にのっぺり」）。
#   **本ものの 桟瓦(さんがわら)は 流れに そって たての すじが 通る。**
#   写真を 縮めると この すじが まっさきに 消える。だから **すじを 手で 描く**。
#
#   漆喰も 同じ。(196,192,141)＝黄土色で、緑の 環境光の 下だと 苔むした 土壁に 見えた
#  （本人「漆喰が黄土色で苔っぽい」）。**本ものの 漆喰は 白に 近い 生成り**。
#   柱の 焦茶と つよく 対比させると 民家に なる。
#
# ★大きさの きまりは これまでどおり **1mあたり 32ドット**。
#   瓦は 48px＝1.5m。桟瓦 1まいの 見えがかりは 30cm ほど なので 5列 x 5段。
#
# 走らせかた: python unity/ArtSource/make_kawara.py
import os
import random

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "Assets", "Art", "Textures")
SIZE = 48                      # 1.5m
COLS, ROWS = 5, 5              # 桟瓦の わりつけ（1まい 30cm）


def kawara():
    """いぶし銀の 桟瓦。**流れに そった たてすじ**が 命"""
    rnd = random.Random(20260817)
    im = Image.new("RGB", (SIZE, SIZE))
    px = im.load()
    # いぶし銀＝青みの ある 濃い ねずみ色。木立ちの 色に すこし 寄せる
    base = (92, 99, 106)
    dark = (56, 62, 70)          # 谷（となりの 瓦との 重なり）
    lite = (132, 139, 145)       # 山の てっぺんに あたる 光
    edge = (40, 44, 51)          # 段の 影
    cw = SIZE / COLS             # 1まいの 幅 9.6px
    ch = SIZE / ROWS
    for y in range(SIZE):
        # 段の 中の 位置。0＝段の 上ばし
        fy = (y % ch) / ch
        for x in range(SIZE):
            fx = (x % cw) / cw
            # たての かまぼこ。まん中が 高く、はしが 谷
            hump = 1.0 - abs(fx - 0.5) * 2.0
            c = [base[i] + (lite[i] - base[i]) * (hump ** 2.2) for i in range(3)]
            # 瓦と 瓦の あいだの 谷
            if fx < 0.13 or fx > 0.87:
                c = [dark[i] + (c[i] - dark[i]) * 0.35 for i in range(3)]
            # 段の 重なり（上の 段が かぶさる 影）
            if fy < 0.17:
                k = 1.0 - fy / 0.17
                c = [edge[i] + (c[i] - edge[i]) * (1.0 - 0.72 * k) for i in range(3)]
            # 焼きむら。1まいごとに すこし 明るさを 変える
            tile = (int(x / cw), int(y / ch))
            rnd.seed(tile[0] * 31 + tile[1] * 17 + 7)
            mul = 0.92 + rnd.random() * 0.16
            n = rnd.random() * 6 - 3
            px[x, y] = tuple(max(0, min(255, int(v * mul + n))) for v in c)
    return im


def shikkui():
    """漆喰。**白に 近い 生成り。** のっぺりしない ように こまかい むらを 入れる"""
    rnd = random.Random(882026)
    im = Image.new("RGB", (32, 32))
    px = im.load()
    base = (222, 216, 199)
    for y in range(32):
        for x in range(32):
            # こての あと（ゆるい ななめの うねり）
            import math
            w = math.sin(x * 0.55 + y * 0.31) * 3.0 + math.sin(x * 0.21 - y * 0.47) * 2.2
            n = rnd.random() * 7 - 3.5
            # ところどころ 下地が すける（古い 壁）
            spot = 0.0
            if rnd.random() < 0.03:
                spot = -14.0
            px[x, y] = tuple(max(0, min(255, int(base[i] + w + n + spot))) for i in range(3))
    return im


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, im in (("roof_tile", kawara()), ("plaster_wall", shikkui())):
        p = os.path.join(OUT, name + ".png")
        im.save(p)
        print("->", p, im.size)


if __name__ == "__main__":
    main()
