# 画面まわりの 絵を 描き起こす。
#
# Kenney「UI Pack」(CC0)も 入れて あるが、あれは 西洋の 木わく なので、
# いまの 木立ち・家の 色みとは そろわない。**枠だけは ここで 描く。**
# （Kenney は 丸や 矢印などの 記号として 残す）
#
# 9スライス（かどを のこして まん中だけ のばす）で 使うので、
# かど 6px・まん中 1px の 24x24 に する。
# 走らせかた: python unity/ArtSource/make_ui.py
import os

from PIL import Image

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Art", "UI")

DARK = (0x25, 0x22, 0x2e)
BR_DK = (0x40, 0x24, 0x18)
BR_LT = (0xab, 0x7a, 0x1e)
OLIVE = (0x9a, 0x92, 0x3a)
PALE = (0xd3, 0xc6, 0x3b)


def mix(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))


def panel(name, body, border, alpha=246, size=24, edge=6):
    """すみ 6px を のこして のばす 枠"""
    im = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    px = im.load()
    inner = mix(body, (0, 0, 0), 0.0)
    for y in range(size):
        for x in range(size):
            d = min(x, y, size - 1 - x, size - 1 - y)
            if d == 0:
                c, a = DARK, 255                       # いちばん そとは こい 輪郭
            elif d == 1:
                c, a = border, 255                     # 明るい ふち
            elif d == 2:
                c, a = mix(border, body, 0.55), 245
            else:
                c, a = inner, alpha                    # 中は うっすら すける
            px[x, y] = (c[0], c[1], c[2], a)
    im.save(os.path.join(OUT, name))
    return name, im.size


# 虫あみの 記号（画面の 説明に そえる）
NET_ICON = [
    "................",
    "......KKKK......",
    ".....KwwwwK.....",
    "....KwKKKKwK....",
    "....KwK..KwK....",
    "....KwK..KwK....",
    ".....KwKKwK.....",
    "......KwwK......",
    "......KBBK......",
    ".......KBK......",
    ".......KBK......",
    "........KBK.....",
    "........KBK.....",
    ".........KK.....",
    "................",
    "................",
]

PAL = {".": None, "K": DARK, "w": (0xe8, 0xe6, 0xd0), "B": BR_LT}


def icon(name, art):
    im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    px = im.load()
    for y, row in enumerate(art):
        for x, ch in enumerate(row):
            c = PAL.get(ch)
            if c:
                px[x, y] = (c[0], c[1], c[2], 255)
    im.save(os.path.join(OUT, name))
    return name, im.size


if __name__ == "__main__":
    print(*panel("panel.png", mix(BR_DK, DARK, 0.45), mix(OLIVE, PALE, 0.35)))
    print(*panel("panel_light.png", mix(OLIVE, (255, 255, 255), 0.55), mix(BR_DK, BR_LT, 0.5), alpha=240))
    print(*icon("icon_net.png", NET_ICON))
