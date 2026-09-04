# Himawari (sunflower) cards, drawn (2026-09-05).
#   himawari_hana.png   flower head, seen face on (alpha cut)
#   himawari_tsubomi.png  bud / half open head
#   himawari_ha.png     one big heart shaped leaf (alpha cut)
# The head is 0.30 m across in game -> 192 px is ~640 px/m, sharper than the
# world (32 px/m) but a card this small reads badly if it is any coarser.
# run: python unity/ArtSource/make_himawari.py
import os, math, random
from PIL import Image, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "Assets", "Art", "Textures")

S = 4                     # supersampling


def poly_rot(cx, cy, pts, ang):
    c, s = math.cos(ang), math.sin(ang)
    return [(cx + x * c - y * s, cy + x * s + y * c) for (x, y) in pts]


def petal(length, width):
    """one petal pointing +x, with the notched tip real sunflowers have"""
    pts = []
    n = 14
    for i in range(n + 1):                      # upper edge, base -> tip
        t = i / n
        pts.append((t * length, width * math.sin(math.pi * t) ** 0.75 * (1 - 0.35 * t)))
    pts.append((length * 1.03, width * 0.10))   # notch
    pts.append((length * 0.93, 0.0))
    pts.append((length * 1.03, -width * 0.10))
    for i in range(n, -1, -1):                  # lower edge, tip -> base
        t = i / n
        pts.append((t * length, -width * math.sin(math.pi * t) ** 0.75 * (1 - 0.35 * t)))
    return pts


def head(name, size, open_amount, seed):
    rnd = random.Random(seed)
    W = size * S
    im = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    c = W / 2.0
    disc = W * 0.185 * (0.55 + 0.45 * open_amount)
    plen = W * 0.30 * open_amount
    pwid = W * 0.072

    # --- petals: a dark back ring, then the lit front ring offset by half a step
    for ring, (col, edge, lo, scale) in enumerate([
            ((214, 150, 30), (176, 116, 20), 0.5, 1.02),
            ((252, 200, 54), (222, 158, 28), 0.0, 0.92)]):
        n = 17
        for i in range(n):
            a = (i + lo) * 2 * math.pi / n + rnd.uniform(-0.05, 0.05)
            L = plen * scale * rnd.uniform(0.90, 1.08)
            pts = poly_rot(c, c, petal(disc * 0.72 + L, pwid * rnd.uniform(0.85, 1.1)), a)
            d.polygon(pts, fill=col, outline=edge)

    # --- disc: dark seeds, a ring of open florets, shading to the lower right
    d.ellipse([c - disc, c - disc, c + disc, c + disc], fill=(74, 52, 30))
    for _ in range(int(1500 * (disc / (W * 0.185)) ** 2)):
        a = rnd.uniform(0, 2 * math.pi)
        r = disc * math.sqrt(rnd.random())
        k = 0.55 + 0.45 * (1 - r / disc)
        col = (int(96 * k + 40), int(70 * k + 26), int(44 * k + 14))
        rr = disc * 0.045
        d.ellipse([c + r * math.cos(a) - rr, c + r * math.sin(a) - rr,
                   c + r * math.cos(a) + rr, c + r * math.sin(a) + rr], fill=col)
    # the outer ring of the disc is still yellow florets
    d.ellipse([c - disc, c - disc, c + disc, c + disc],
              outline=(190, 148, 46), width=int(disc * 0.16))
    # shadow of the petals falling on the disc, from the upper left
    sh = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    ImageDraw.Draw(sh).ellipse([c - disc * 1.02 + disc * 0.22, c - disc * 1.02 + disc * 0.26,
                                c + disc * 1.02 + disc * 0.22, c + disc * 1.02 + disc * 0.26],
                               fill=(0, 0, 0, 60))
    mask = Image.new("L", (W, W), 0)
    ImageDraw.Draw(mask).ellipse([c - disc, c - disc, c + disc, c + disc], fill=255)
    im.paste(Image.alpha_composite(im.crop((0, 0, W, W)), sh), (0, 0), mask)

    im = im.resize((size, size), Image.LANCZOS)
    # alpha cut needs a hard edge: push half transparent pixels either way
    a = im.split()[3].point(lambda v: 0 if v < 110 else 255)
    im.putalpha(a)
    im.save(os.path.join(OUT, name))
    print(name, size)


def leaf(name, size, seed):
    rnd = random.Random(seed)
    W = size * S
    im = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    # heart shaped blade, stalk at the bottom, tip at the top
    pts = []
    n = 40
    for i in range(n + 1):                       # right edge, base -> tip
        t = i / n
        y = W * (0.94 - 0.86 * t)
        w = W * 0.40 * math.sin(math.pi * min(1.0, t * 1.12)) ** 0.62 * (1 - 0.15 * t)
        w *= 1 + 0.06 * math.sin(t * 34)         # coarse teeth
        pts.append((W * 0.5 + w, y))
    for i in range(n, -1, -1):                   # left edge, tip -> base
        t = i / n
        y = W * (0.94 - 0.86 * t)
        w = W * 0.40 * math.sin(math.pi * min(1.0, t * 1.12)) ** 0.62 * (1 - 0.15 * t)
        w *= 1 + 0.06 * math.sin(t * 34 + 1.7)
        pts.append((W * 0.5 - w, y))
    d.polygon(pts, fill=(66, 116, 48))
    # blotchy light, then the veins
    for _ in range(90):
        x = rnd.randrange(W); y = rnd.randrange(W); r = rnd.randint(W // 26, W // 12)
        k = rnd.uniform(0.88, 1.16)
        d.ellipse([x - r, y - r, x + r, y + r], fill=(int(66 * k), int(116 * k), int(48 * k)))
    im.putalpha(Image.new("L", (W, W), 0))
    m = Image.new("L", (W, W), 0)
    ImageDraw.Draw(m).polygon(pts, fill=255)
    im.putalpha(m)
    d = ImageDraw.Draw(im)
    d.line([(W * 0.5, W * 0.94), (W * 0.5, W * 0.09)], fill=(126, 168, 88), width=max(2, W // 130))
    for i in range(1, 9):
        t = i / 9.0
        y = W * (0.86 - 0.74 * t)
        w = W * 0.36 * math.sin(math.pi * min(1.0, t * 1.12)) ** 0.62
        for s in (-1, 1):
            d.line([(W * 0.5, y), (W * 0.5 + s * w * 0.86, y - W * 0.12)],
                   fill=(112, 152, 78), width=max(1, W // 200))
    d.line([(W * 0.5, W * 0.99), (W * 0.5, W * 0.90)], fill=(120, 150, 74), width=max(3, W // 90))
    # the midrib and the veins are drawn over the alpha, so clip them back to the
    # blade (plus the stalk) or they poke out of the tip as loose lines
    ImageDraw.Draw(m).rectangle([W * 0.5 - W // 90, W * 0.90, W * 0.5 + W // 90, W * 0.99], fill=255)
    im.putalpha(m)
    im = im.filter(ImageFilter.GaussianBlur(W / 400.0))
    im = im.resize((size, size), Image.LANCZOS)
    a = im.split()[3].point(lambda v: 0 if v < 110 else 255)
    im.putalpha(a)
    im.save(os.path.join(OUT, name))
    print(name, size)


head("himawari_hana.png", 192, 1.0, 7)
head("himawari_tsubomi.png", 128, 0.28, 11)
leaf("himawari_ha.png", 160, 3)
