# 家に はる テクスチャを、外の 素材から こしらえる。
#
# もと素材：ambientCG（https://ambientcg.com）。**ぜんぶ CC0**＝商用可・
# クレジット不要・再配布可。公開リポジトリに 同梱しても よい。アカウントも 要らない。
#
# ★そのまま はると 写真の 質感が ドット絵の キャラや 草木と 食いちがう ので、
#   **小さく して 色を 減らして** から つかう（本人の 判断）。
#     1) 1K の JPG を 256px に 落とす＝1ドットが 大きくなる
#     2) 色を 16色に まとめる＝階調が 段に なる
#     3) 木立ちの 20色に すこし 寄せる＝ほかの 絵と 色みが そろう
#
# 走らせかた: python unity/ArtSource/make_house_tex.py
import os
import zipfile

from PIL import Image

HERE = os.path.dirname(__file__)
# zip を ここに 置く。**リポジトリには 入れない**（1本 4〜9MB あり、成果物の PNG だけで 足りる）。
# 取りなおすときは https://ambientcg.com/get?file=<assetId>_1K-JPG.zip
SRC = os.path.join(HERE, "_acg")
OUT = os.path.join(HERE, "..", "Assets", "Art", "Textures")

# 木立ち(ansimuz CC0)から 吸いだした 色。ここへ すこし 寄せる
FAMILY = [
    (0x25, 0x22, 0x2e), (0x11, 0x4d, 0x3c), (0x36, 0x65, 0x12), (0x55, 0x86, 0x19),
    (0x6d, 0xb9, 0x27), (0x24, 0x50, 0x22), (0x40, 0x24, 0x18), (0xab, 0x7a, 0x1e),
    (0x9a, 0x92, 0x3a), (0x5d, 0x60, 0x2c), (0xd3, 0xc6, 0x3b), (0x3d, 0x3f, 0x37),
    (0xe8, 0xe6, 0xd0), (0x6b, 0x45, 0x22), (0xb4, 0x3a, 0x2e), (0x5a, 0x5f, 0x66),
]

# どの 素材を 何に つかうか。(zip名, 出す名, 何色に まとめるか, 寄せる ぐあい)
# ★どれを えらぶかが 9割。**小さく しても 形が のこる もの**しか つかえない。
#   はじめ RoofingTiles013A（平たい スレート）を とったら、256pxに 落とした とたん
#   ただの 黒い ざらざらに なって 屋根に 見えなかった。
#   うろこ状の 凹凸が はっきり した RoofingTiles001 なら 小さくても 瓦に 見える。
#   PaintedPlaster017（真っ白で のっぺり）と Ground054（平たい 砂）も 同じ 理由で 落選。
#   漆喰と 道の 土は **手で 描いた ものを 残す**（草の 絵と 色が そろって いる）
# (zip名, 出す名, 何色に まとめるか, 寄せる ぐあい, 明るさの かけ算 rgb)
JOBS = [
    ("RoofingTiles001",  "roof_tile",    14, 0.42, (1.00, 1.00, 1.00)),   # 瓦（うろこ状）
    # 板は もとが 白木で 明るすぎた。**古い 農家の 板は 日に 焼けて 茶色い**ので 落とす
    ("WoodSiding009",    "wood_beam",    14, 0.45, (0.62, 0.50, 0.34)),
    ("Rock030",          "stone",        12, 0.40, (1.05, 1.05, 1.02)),   # 石
    ("ThatchedRoof001A", "thatch",       14, 0.45, (1.15, 1.02, 0.72)),   # わら（黄みを 出す）
]

SIZE = 256


def color_map(im, keep, pull):
    """色を まとめて、木立ちの 色に すこし 寄せる"""
    q = im.convert("RGB").quantize(colors=keep, method=Image.MEDIANCUT, dither=Image.NONE)
    pal = q.getpalette()[: keep * 3]
    out = []
    for i in range(keep):
        c = tuple(pal[i * 3: i * 3 + 3])
        # いちばん 近い 身内の 色を さがして、そちらへ pull だけ 寄せる
        near = min(FAMILY, key=lambda f: sum((f[j] - c[j]) ** 2 for j in range(3)))
        out += [int(round(c[j] + (near[j] - c[j]) * pull)) for j in range(3)]
    q.putpalette(out + [0] * (768 - len(out)))
    return q.convert("RGB")


def pick_color_member(zf):
    """zip の 中から 色の 絵を さがす（法線や あらさの 絵は つかわない）"""
    for n in zf.namelist():
        low = n.lower()
        if low.endswith((".jpg", ".png")) and "color" in low:
            return n
    return None


def main():
    os.makedirs(OUT, exist_ok=True)
    for zip_name, out_name, keep, pull, mul in JOBS:
        path = os.path.join(SRC, zip_name + ".zip")
        if not os.path.exists(path):
            print("ない:", path)
            continue
        with zipfile.ZipFile(path) as zf:
            member = pick_color_member(zf)
            if member is None:
                print("色の 絵が ない:", zip_name)
                continue
            with zf.open(member) as fp:
                im = Image.open(fp).convert("RGB")
        # 正方形に 切って 小さく する。**繰りかえして 貼るので 継ぎめが 出ない ように 端は 切らない**
        s = min(im.size)
        im = im.crop((0, 0, s, s)).resize((SIZE, SIZE), Image.LANCZOS)
        if mul != (1.0, 1.0, 1.0):
            px = im.load()
            for y in range(SIZE):
                for x in range(SIZE):
                    r, g, b = px[x, y]
                    px[x, y] = (min(255, int(r * mul[0])),
                                min(255, int(g * mul[1])),
                                min(255, int(b * mul[2])))
        im = color_map(im, keep, pull)
        dst = os.path.join(OUT, out_name + ".png")
        im.save(dst)
        print("->", dst, im.size, "色", keep)


if __name__ == "__main__":
    main()
