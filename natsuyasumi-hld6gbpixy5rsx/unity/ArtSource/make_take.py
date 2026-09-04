# Bamboo culm textures (2026-09-03). Procedural: the surface is smooth, so a drawn one is enough.
#   take_kawa_midori.png  green (1-3 year culms)
#   take_kawa_ki.png      yellowing (4-5 year)
#   take_kawa_cha.png     dead brown-grey
# Tube UV: v = length * 0.8  ->  one repeat per 1.25 m. Nodes every 0.31 m -> 4 bands per repeat.
# run: python unity/ArtSource/make_take.py
import os, random
from PIL import Image, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "Assets", "Art", "Textures", "shashin")
W, H = 256, 512

def make(name, base, dark, seed):
    rnd = random.Random(seed)
    im = Image.new("RGB", (W, H), base)
    d = ImageDraw.Draw(im)
    # vertical shading: darker at the sides (u=0 and u=1 are the same seam), lighter in the middle
    for x in range(W):
        k = 0.82 + 0.18 * (1 - abs(x / W - 0.5) * 2) ** 0.7
        col = tuple(int(c * k) for c in base)
        d.line([(x, 0), (x, H)], fill=col)
    # faint vertical streaks
    for _ in range(140):
        x = rnd.randrange(W); ln = rnd.randint(30, 200); y0 = rnd.randrange(H)
        k = rnd.uniform(0.9, 1.08)
        col = tuple(min(255, int(c * k)) for c in base)
        d.line([(x, y0), (x, y0 + ln)], fill=col, width=1)
    im = im.filter(ImageFilter.GaussianBlur(0.6))
    d = ImageDraw.Draw(im)
    # 4 node bands per repeat: a light ridge with a dark line under it
    for i in range(4):
        y = int(H * (i + 0.5) / 4)
        light = tuple(min(255, int(c * 1.18)) for c in base)
        d.rectangle([0, y - 4, W, y + 1], fill=light)
        d.rectangle([0, y + 2, W, y + 5], fill=dark)
        d.rectangle([0, y - 7, W, y - 5], fill=dark)
    im.save(os.path.join(OUT, name), quality=92)
    print(name)

make("take_kawa_midori.jpg", (112, 138, 74), (58, 72, 40), 1)
make("take_kawa_ki.jpg",     (152, 142, 78), (86, 80, 44), 2)
make("take_kawa_cha.jpg",    (128, 112, 88), (70, 62, 50), 3)
