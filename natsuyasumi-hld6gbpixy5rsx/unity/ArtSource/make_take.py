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


# ---- rock surface (iwa_hada.jpg): grey-brown, blotches, cracks, a little moss. Spherical UV on a blob mesh.
def iwa(name, seed):
    rnd = random.Random(seed)
    im = Image.new("RGB", (512, 512), (122, 118, 108))
    d = ImageDraw.Draw(im)
    for _ in range(900):
        x = rnd.randrange(512); y = rnd.randrange(512); r = rnd.randint(4, 40)
        k = rnd.uniform(0.78, 1.16)
        col = tuple(min(255, int(c * k)) for c in (122, 118, 108))
        d.ellipse([x - r, y - r // 2, x + r, y + r // 2], fill=col)
    im = im.filter(ImageFilter.GaussianBlur(3))
    d = ImageDraw.Draw(im)
    for _ in range(40):                                        # cracks
        x = rnd.randrange(512); y = rnd.randrange(512)
        for _ in range(rnd.randint(3, 9)):
            x2 = x + rnd.randint(-30, 30); y2 = y + rnd.randint(-30, 30)
            d.line([(x, y), (x2, y2)], fill=(70, 66, 60), width=rnd.randint(1, 2)); x, y = x2, y2
    moss = Image.new("RGB", (512, 512), (96, 118, 62))
    m = Image.new("L", (512, 512), 0); dm = ImageDraw.Draw(m)
    for _ in range(14):
        x = rnd.randrange(512); y = rnd.randrange(512); r = rnd.randint(20, 70)
        dm.ellipse([x - r, y - r, x + r, y + r], fill=rnd.randint(90, 170))
    m = m.filter(ImageFilter.GaussianBlur(14))
    im = Image.composite(moss, im, m)
    im = im.filter(ImageFilter.GaussianBlur(0.6))
    im.save(os.path.join(OUT, name), quality=92)
    print(name)

iwa("iwa_hada.jpg", 7)


# ---- dry stone wall (ishigaki.jpg): field stones packed tight (nozura-zumi), thin dark joints, grainy faces.
#   Tiles horizontally and vertically. UV: 1 repeat = 1.6 m
def ishigaki(name, seed):
    import math
    rnd = random.Random(seed)
    W2, H2 = 512, 512
    im = Image.new("RGB", (W2, H2), (82, 76, 66))
    d = ImageDraw.Draw(im)
    y = -20
    while y < H2 + 40:
        h = rnd.randint(64, 110)
        x = -rnd.randint(0, 70)
        while x < W2 + 60:
            w = rnd.randint(70, 150)
            base = rnd.choice([(126, 121, 112), (116, 112, 105), (134, 127, 116), (108, 106, 100), (122, 115, 104), (140, 134, 122)])
            pts = []
            cx, cy, rx, ry = x + w / 2, y + h / 2, w / 2 - 2, h / 2 - 2
            n = rnd.randint(9, 14)
            for k in range(n):
                a = k / n * 2 * math.pi + rnd.uniform(-0.12, 0.12)
                jr = rnd.uniform(0.90, 1.0)
                pts.append((cx + math.cos(a) * rx * jr, cy + math.sin(a) * ry * jr))
            d.polygon(pts, fill=base)
            # subtle relief: a soft dark crescent at the bottom-right, light at the top-left
            dk = tuple(int(c * 0.86) for c in base); lt = tuple(min(255, int(c * 1.07)) for c in base)
            lo = [p for p in pts if p[1] > cy + ry * 0.35]
            if len(lo) > 1: d.line(lo, fill=dk, width=5)
            hi = [p for p in pts if p[1] < cy - ry * 0.35]
            if len(hi) > 1: d.line(hi, fill=lt, width=3)
            x += w + rnd.randint(2, 6)
        y += h + rnd.randint(2, 6)
    # grain on the faces
    px = im.load()
    for _ in range(90000):
        x = rnd.randrange(W2); y2 = rnd.randrange(H2)
        c = px[x, y2]; k = rnd.uniform(0.9, 1.1)
        px[x, y2] = tuple(min(255, int(v * k)) for v in c)
    im = im.filter(ImageFilter.GaussianBlur(0.7))
    moss = Image.new("RGB", (W2, H2), (96, 116, 62))
    m = Image.new("L", (W2, H2), 0); dm = ImageDraw.Draw(m)
    for _ in range(18):
        x = rnd.randrange(W2); y2 = rnd.randrange(H2 // 2, H2); r = rnd.randint(12, 34)
        dm.ellipse([x - r, y2 - r, x + r, y2 + r], fill=rnd.randint(50, 110))
    m = m.filter(ImageFilter.GaussianBlur(9))
    im = Image.composite(moss, im, m)
    im.save(os.path.join(OUT, name), quality=92)
    print(name)

ishigaki("ishigaki.jpg", 11)
