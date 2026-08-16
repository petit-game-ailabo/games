# Quaternius「Medieval Village MegaKit」(CC0) を Unity に 取りこめる 形に する。
#
# ★これは **試し**（本人 2026-08-16）。
#   「アセットを つかったら どれだけ 質の 高い ものが できるのか テストしたい。
#     風景に 合わなくて いい」。納屋を いったん 消して、ここへ 建てる。
#   さらに その 作りを 手本に、じぶんで 起こした モデルを この 世界へ 入れていく。
#
# 元は unity/ArtSource/ref/quaternius-medieval-village/（.gitignore 済み・手もとだけ）。
# ここから **要る ぶんだけ** Assets/ へ うつす。
#
#  - FBX は 176点で 7.7MB しか ない ので まるごと うつす（選びなおしが きく）
#  - テクスチャは 2048px。**そのままだと 20枚で 60MB**。GitHub Pages が
#    リポジトリを そのまま 配信する ので、色と 法線は 1024、あらさ(ORM/Roughness)は
#    512 に 落とす。この 引きの 画角なら 見わけは つかない
#
# 走らせかた: python unity/ArtSource/make_megakit.py
import os
import shutil

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "ref", "quaternius-medieval-village")
UNITY = os.path.join(HERE, "..", "Assets", "Art")
OUT_FBX = os.path.join(UNITY, "Models", "megakit")
OUT_TEX = os.path.join(UNITY, "Textures", "megakit")

# (もとの ファイル, 出す名, 何pxに するか)
# **法線は「Normals Godot-Unity」の ほうを つかう。** 素の Normal は Y が 逆で、
# Unity で つかうと 凹凸が 裏返る（へこみが 出っぱりに 見える）
N = "Normals Godot-Unity"
JOBS = [
    ("Textures/T_Plaster_BaseColor.png",        "T_Plaster_BaseColor",     1024),
    (f"Textures/{N}/T_Plaster_Normal.png",      "T_Plaster_Normal",        1024),
    ("Textures/T_Plaster_ORM.png",              "T_Plaster_ORM",            512),
    ("Textures/T_WoodTrim_BaseColor.png",       "T_WoodTrim_BaseColor",    1024),
    (f"Textures/{N}/T_WoodTrim_Normal.png",     "T_WoodTrim_Normal",       1024),
    ("Textures/T_WoodTrim_ORM.png",             "T_WoodTrim_ORM",           512),
    ("Textures/T_UnevenBrick_BaseColor.png",    "T_UnevenBrick_BaseColor", 1024),
    (f"Textures/{N}/T_UnevenBrick_Normal.png",  "T_UnevenBrick_Normal",    1024),
    ("Textures/T_UnevenBrick_Roughness.png",    "T_UnevenBrick_Roughness",  512),
    ("Textures/T_Brick_BaseColor.png",          "T_Brick_BaseColor",       1024),
    (f"Textures/{N}/T_Brick_Normal.png",        "T_Brick_Normal",          1024),
    ("Textures/T_Brick_Roughness.png",          "T_Brick_Roughness",        512),
    ("Textures/T_RedBrick_BaseColor.png",       "T_RedBrick_BaseColor",    1024),
    ("Textures/T_RockTrim_BaseColor.png",       "T_RockTrim_BaseColor",    1024),
    (f"Textures/{N}/T_RockTrim_Normal.png",     "T_RockTrim_Normal",       1024),
    ("Textures/T_RockTrim_ORM.png",             "T_RockTrim_ORM",           512),
    ("Textures/T_RoundTiles_BaseColor.png",     "T_RoundTiles_BaseColor",  1024),
    (f"Textures/{N}/T_RoundTiles_Normal.png",   "T_RoundTiles_Normal",     1024),
    ("Textures/T_RoundTiles_Roughness.png",     "T_RoundTiles_Roughness",   512),
    ("Textures/T_VineLeaf.png",                 "T_VineLeaf",               512),
    ("Textures/T_WindowGradient.png",           "T_WindowGradient",         256),
]


def main():
    if not os.path.isdir(SRC):
        print("もと素材が ない:", SRC)
        print("https://quaternius.com から 取りなおして ref/ に 置く")
        return

    os.makedirs(OUT_FBX, exist_ok=True)
    os.makedirs(OUT_TEX, exist_ok=True)

    n = 0
    for f in sorted(os.listdir(os.path.join(SRC, "FBX"))):
        if not f.lower().endswith(".fbx"):
            continue
        shutil.copy2(os.path.join(SRC, "FBX", f), os.path.join(OUT_FBX, f))
        n += 1
    print(f"FBX -> {OUT_FBX}  {n}点")

    total = 0
    for src, name, size in JOBS:
        path = os.path.join(SRC, src)
        if not os.path.exists(path):
            print("ない:", path)
            continue
        im = Image.open(path)
        # あらさの 絵は 1チャンネル。RGB に そろえないと Unity で 扱いが ぶれる
        if im.mode == "L":
            im = im.convert("RGB")
        if im.size != (size, size):
            im = im.resize((size, size), Image.LANCZOS)
        dst = os.path.join(OUT_TEX, name + ".png")
        im.save(dst, optimize=True)
        total += os.path.getsize(dst)
        print(f"  {name:28s} {size}px  {os.path.getsize(dst)/1048576:.2f}MB")
    print(f"テクスチャ 合計 {total/1048576:.1f}MB -> {OUT_TEX}")

    # ライセンスを いっしょに 置く（CC0 だが 出どころは 追える ように する きまり）
    shutil.copy2(os.path.join(SRC, "License_Standard.txt"),
                 os.path.join(OUT_FBX, "License_Quaternius_CC0.txt"))


if __name__ == "__main__":
    main()
