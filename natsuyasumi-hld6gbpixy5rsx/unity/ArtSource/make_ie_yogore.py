# 家の 汚し（2026-09-02）。本人の 写真(shashin/ie_*.jpg)から、**古い 家の 材質**を こしらえる。
#
# ★本人「引き続ききれいでおしゃれすぎる…家自体が古いので黒しみとか、色あせた感じになるはず」
#   参考写真（2階建ての 空き家）：1階は 黄ばんだ 漆喰に 黒い 雨だれ、2階は 色あせた 茶の たて板、
#   戸袋は 下見板では なく **平らな 板**（さび色に あせて いる）、樋は 銅の 茶。
#
#   ie_kabe_yogore.jpg … 1階の 漆喰。黄ばみ＋たての 黒しみ＋しみの かたまり
#   ie_itakabe.jpg     … 2階の たて板。写真の 木(ie_ki)を 明るく して 板の すじを 入れる
#   ie_tobukuro.jpg    … 戸袋。木目を うすく、さび色に あせた 平らな 板
#   *_n.png は もとの 法線を そのまま わたす（模様の 位置は 変えて いない）
#
# 走らせかた: python unity/ArtSource/make_ie_yogore.py
import os, random
from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter, ImageOps

HERE = os.path.dirname(os.path.abspath(__file__))
T = os.path.join(HERE, "..", "Assets", "Art", "Textures", "shashin")


def suji(w, h, kazu, seed, koi, haba=(6, 40), bokashi=9):
    """たての 雨だれ。白い 絵に 暗い すじを 描いて ぼかす → あとで 乗算"""
    rnd = random.Random(seed)
    m = Image.new("L", (w, h), 255)
    d = ImageDraw.Draw(m)
    for _ in range(kazu):
        x = rnd.randrange(w); ww = rnd.randint(*haba)
        y0 = rnd.randrange(-h // 4, h // 3); y1 = rnd.randrange(h // 2, h + h // 4)
        v = int(255 * (1 - koi * rnd.uniform(0.3, 1.0)))
        d.rectangle([x - ww // 2, y0, x + ww // 2, y1], fill=v)
    return m.filter(ImageFilter.GaussianBlur(bokashi))


def shimi(w, h, kazu, seed, koi, r=(30, 140), bokashi=25):
    """しみの かたまり（楕円を ぼかす）"""
    rnd = random.Random(seed)
    m = Image.new("L", (w, h), 255)
    d = ImageDraw.Draw(m)
    for _ in range(kazu):
        x = rnd.randrange(w); y = rnd.randrange(h); rr = rnd.randint(*r)
        v = int(255 * (1 - koi * rnd.uniform(0.3, 1.0)))
        d.ellipse([x - rr, y - rr // 2, x + rr, y + rr // 2], fill=v)
    return m.filter(ImageFilter.GaussianBlur(bokashi))


def kake(img, mask):
    return ImageChops.multiply(img, Image.merge("RGB", [mask] * 3))


def iro(img, rgb):
    """色を かける（1より 大きくても よい）"""
    ch = img.split()
    out = [c.point(lambda v, k=k: min(255, int(v * k))) for c, k in zip(ch, rgb)]
    return Image.merge("RGB", out)


def yoko_narabe(img, n):
    w, h = img.size
    o = Image.new(img.mode, (w * n, h))
    for i in range(n): o.paste(img, (i * w, 0))
    return o


# ---- 1階の 漆喰：黄ばみ＋黒しみ
kabe = Image.open(os.path.join(T, "ie_kabe.jpg")).convert("RGB")
w, h = kabe.size
y = iro(kabe, (0.97, 0.94, 0.86))
y = kake(y, suji(w, h, 22, 1, 0.42))
y = kake(y, shimi(w, h, 9, 2, 0.30))
y.save(os.path.join(T, "ie_kabe_yogore.jpg"), quality=90)
Image.open(os.path.join(T, "ie_kabe_n.png")).save(os.path.join(T, "ie_kabe_yogore_n.png"))

# ---- 2階の たて板：暗い 木を 色あせた 茶に。板の すじを 12.5cm ごと
ki = Image.open(os.path.join(T, "ie_ki.jpg")).convert("RGB")
kin = Image.open(os.path.join(T, "ie_ki_n.png"))
n = 2 if ki.size[0] < ki.size[1] else 1
ita = yoko_narabe(ki, n)
w, h = ita.size
g = ImageOps.autocontrast(ImageOps.grayscale(ita), cutoff=2)
g = ImageEnhance.Contrast(g).enhance(0.55)
ita = ImageOps.colorize(g, black=(88, 72, 56), white=(178, 158, 130))   # 日に 焼けた 杉板
d = ImageDraw.Draw(ita)
step = w // 8                                                             # 1m に 8まい
for i in range(8):
    x = i * step
    d.line([(x, 0), (x, h)], fill=(70, 56, 44), width=3)
    d.line([(x + 3, 0), (x + 3, h)], fill=(200, 182, 156), width=1)       # 板の ふちの ハイライト
ita = kake(ita, suji(w, h, 16, 3, 0.35, haba=(8, 60), bokashi=12))
ita = kake(ita, shimi(w, h, 6, 4, 0.22))
ita.save(os.path.join(T, "ie_itakabe.jpg"), quality=90)
yoko_narabe(kin, n).save(os.path.join(T, "ie_itakabe_n.png"))

# ---- 戸袋：木目を うすく のこした さび色の 平らな 板
w, h = ki.size
g = ImageOps.autocontrast(ImageOps.grayscale(ki), cutoff=2)
g = ImageEnhance.Contrast(g).enhance(0.45)
tb = ImageOps.colorize(g, black=(72, 54, 46), white=(138, 112, 98))   # あせた 茶。赤を 強く すると ペンキに 見える
tb = kake(tb, shimi(w, h, 7, 5, 0.45, r=(40, 200), bokashi=35))
tb = kake(tb, suji(w, h, 6, 6, 0.30, haba=(20, 90), bokashi=20))
tb.save(os.path.join(T, "ie_tobukuro.jpg"), quality=90)
kin.save(os.path.join(T, "ie_tobukuro_n.png"))
print("ok", ita.size, tb.size)
