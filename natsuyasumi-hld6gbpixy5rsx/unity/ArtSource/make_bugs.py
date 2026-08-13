# 虫の ドット絵を 描き起こす。
#
# 木立ちと 同じ 色の 系統で そろえる（羽の 半とうめいだけ 灰青を 足した）。
# 1コマ 16x16 の 4列 x 2行 → 64x32。世界では 1匹 0.34m で 出す。
#   ＝16px / 0.34m ≒ 47px/m で、キャラ(48x64を1.35m)と 同じ 目の こまかさに なる。
#   実物の 虫は もっと 小さいが、それだと 1〜2px に なって 見えない
#
# 絵は 見て すぐ 直せる ように **文字の 地図**で 持つ。
# 走らせかた: python unity/ArtSource/make_bugs.py
import os

from PIL import Image

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Art", "Sprites", "bugs.png")

PAL = {
    ".": None,                      # すける
    "K": (0x25, 0x22, 0x2e),        # 輪郭
    "d": (0x40, 0x24, 0x18),        # こい 茶
    "b": (0x6b, 0x45, 0x22),        # 茶
    "B": (0xab, 0x7a, 0x1e),        # あかるい 茶
    "g": (0x11, 0x4d, 0x3c),        # こい 緑
    "G": (0x36, 0x65, 0x12),        # 緑
    "L": (0x6d, 0xb9, 0x27),        # あかるい 緑
    "w": (0xe8, 0xe6, 0xd0),        # 生なり
    "y": (0xfa, 0xeb, 0x54),        # きいろ
    "o": (0xd3, 0x8f, 0x10),        # だいだい
    "c": (0xb9, 0xc9, 0xc2),        # 羽（うすい）
    "C": (0x8f, 0xa3, 0x9c),        # 羽（こい）
}

# ---- セミ（木に とまる。頭を 上に して たて向き）
SEMI = [
    "................",
    ".......KK.......",
    "......KddK......",
    "......KbbK......",
    ".....KKddKK.....",
    "....KCcKddKcCK..",
    "...KCccKddKccCK.",
    "...KCccKdbKccCK.",
    "...KCccKdbKccCK.",
    "....KCcKdbKcCK..",
    "....KCcKdbKcCK..",
    ".....KKKddKKK...",
    "......KdbdK.....",
    ".......KdK......",
    "........K.......",
    "................",
]

# ---- カブトムシ（角が ある。たて向き）
KABUTO = [
    "................",
    ".......KK.......",
    "......KBBK......",
    ".....KBddBK.....",
    "......KddK......",
    ".....KKddKK.....",
    "....KdbbbbdK....",
    "...KdbBBBBbdK...",
    "..KKdbBBBBbdKK..",
    "..KdddbBBbdddK..",
    "..KddddbbddddK..",
    "...KKdddddKK....",
    "..K..KdddK...K..",
    ".K....KKK.....K.",
    "................",
    "................",
]

# ---- クワガタ（大あご）
KUWAGATA = [
    "................",
    "...K........K...",
    "...KK......KK...",
    "....Kd....dK....",
    ".....Kd..dK.....",
    "......KddK......",
    ".....KKddKK.....",
    "....KdKddKdK....",
    "...KddbbbbddK...",
    "..KKdbbbbbbdKK..",
    "..KdddbbbbdddK..",
    "..KdddddddddK...",
    "...K.KdddK..K...",
    "..K...KKK....K..",
    "................",
    "................",
]

# ---- トンボ（よこ向きに とぶ）
TONBO = [
    "................",
    "..CccC....CccC..",
    ".CcccccKKcccccC.",
    "..CccC.KK.CccC..",
    "......KKKK......",
    ".....KGGGGK.....",
    "....KGyyyyGK....",
    "...KKGGGGGGKK...",
    "..CccC.KK.CccC..",
    ".CcccccKKcccccC.",
    "..CccC.KK.CccC..",
    "........KK......",
    "........KK......",
    "........KK......",
    ".........K......",
    "................",
]

# ---- オニヤンマ（大きい。黒に きいろの しま）
ONIYANMA = [
    "................",
    ".CccC......CccC.",
    "CccccccKKccccccC",
    ".CccC..KK..CccC.",
    "......KKKK......",
    ".....KKyyKK.....",
    "....KKKyyKKK....",
    "...KKKKKKKKKK...",
    "..CccC.KK.CccC..",
    ".CcccccKKcccccC.",
    "..CccC.KK.CccC..",
    "........KK......",
    "........Ky......",
    "........KK......",
    "........Ky......",
    ".........K......",
]

# ---- チョウ（はねを ひろげた ところ）
CHOU = [
    "................",
    "...KK......KK...",
    "..KyyKK..KKyyK..",
    ".KyyyyK..KyyyyK.",
    ".KyKyyKKKKyyKyK.",
    "KyyKyyKddKyyKyyK",
    "KyyyyyKddKyyyyyK",
    "KyyyKKKddKKKyyyK",
    ".KyyKK.KK.KKyyK.",
    "..KyyK.KK.KyyK..",
    "...KKKKddKKKK...",
    "......KddK......",
    ".......KK.......",
    "................",
    "................",
    "................",
]

# ---- バッタ（草に いる。うしろ足が 大きい）
BATTA = [
    "................",
    "................",
    "..........KK....",
    ".........KGGK...",
    "....KKKKKGGGK...",
    "...KGGGGGGGGK...",
    "..KGLLLLLGGGK...",
    "..KGLLLLLLGGK...",
    "...KGGGGGGGK....",
    "..KK.KKKKKK.....",
    ".KGGK...K.K.....",
    "KGGK....K.K.....",
    "KGK.....KK......",
    ".K..............",
    "................",
    "................",
]

# ---- ホタル（よるだけ。おしりが 光る）
HOTARU = [
    "................",
    "................",
    "......KKK.......",
    ".....KdddK......",
    "....KCcKdK......",
    "...KCcckddK.....",
    "..KCcccKddK.....",
    "..KCcccKdoK.....",
    "...KCcKdooK.....",
    "....KKKoyoK.....",
    ".....KoyyyoK....",
    "......KoyoK.....",
    ".......KoK......",
    "........K.......",
    "................",
    "................",
]

BUGS = [
    ("semi", SEMI), ("kabuto", KABUTO), ("kuwagata", KUWAGATA), ("tonbo", TONBO),
    ("oniyanma", ONIYANMA), ("chou", CHOU), ("batta", BATTA), ("hotaru", HOTARU),
]

CELL = 16
COLS, ROWS = 4, 2


def draw(art):
    im = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
    px = im.load()
    for y, row in enumerate(art[:CELL]):
        for x, ch in enumerate(row[:CELL]):
            c = PAL.get(ch)
            if c:
                px[x, y] = (c[0], c[1], c[2], 255)
    return im


if __name__ == "__main__":
    sheet = Image.new("RGBA", (CELL * COLS, CELL * ROWS), (0, 0, 0, 0))
    for i, (name, art) in enumerate(BUGS):
        bad = [(y, len(r)) for y, r in enumerate(art) if len(r) != CELL]
        if bad:
            raise SystemExit("%s: 行の 長さが %d でない: %s" % (name, CELL, bad))
        if len(art) != CELL:
            raise SystemExit("%s: 行数が %d でない (%d)" % (name, CELL, len(art)))
        sheet.paste(draw(art), ((i % COLS) * CELL, (i // COLS) * CELL))
        print("%2d %s" % (i, name))
    sheet.save(OUT)
    print("->", OUT, sheet.size)
