# -*- coding: utf-8 -*-
# Kenney Nature Kit (CC0) から 庭シーンに つかう ぶんだけ Assets へ うつす。
# 手本置き場: ArtSource/ref/kenney-nature-kit/ （gitignore・Unityの外）
# つかいかた: python make_kenney.py
import shutil, os

SRC = os.path.join(os.path.dirname(__file__), "ref", "kenney-nature-kit", "Models", "FBX format")
DST = os.path.join(os.path.dirname(__file__), "..", "Assets", "Art", "Models", "kenney")

FILES = [
    # 木（庭の シンボルツリー と 塀ぞい）
    "tree_oak", "tree_oak_dark", "tree_default", "tree_default_dark",
    "tree_detailed", "tree_fat",
    # 草・花
    "grass", "grass_large", "grass_leafs", "grass_leafsLarge",
    "flower_redA", "flower_yellowA", "flower_purpleA",
    # 竹（和の 気配）
    "crops_bambooStageA", "crops_bambooStageB",
    # 塀と 門
    "fence_simple", "fence_simpleCenter", "fence_simpleHigh",
    "fence_corner", "fence_gate", "fence_planks",
    # 飛び石・小道
    "path_stone", "path_stoneCircle", "path_stoneEnd", "path_wood",
    # 石・小物
    "rock_smallA", "rock_smallB", "stone_smallFlatA", "stone_smallFlatB",
    "pot_large", "pot_small", "log", "mushroom_red",
]

os.makedirs(DST, exist_ok=True)
n = 0
for f in FILES:
    src = os.path.join(SRC, f + ".fbx")
    if not os.path.exists(src):
        print("MISSING:", f)
        continue
    shutil.copy2(src, os.path.join(DST, f + ".fbx"))
    n += 1
print("copied", n, "->", os.path.abspath(DST))
