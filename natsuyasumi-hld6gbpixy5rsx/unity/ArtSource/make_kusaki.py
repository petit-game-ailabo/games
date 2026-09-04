# Grass clump and bamboo leaf cutouts (2026-09-03). Source: the user's Codex images on magenta
# (unity/ArtSource/kusaki/, not in the repo). Output: Assets/Art/Textures/shashin/<name>.png
# Same chroma key as make_mushi.py (unmix the magenta blend), but NO posterize/outline:
# foliage stays photo-like, same as the tree leaves (ki_ha.png).
# run: python unity/ArtSource/make_kusaki.py
import os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "kusaki")
OUT = os.path.join(HERE, "..", "Assets", "Art", "Textures", "shashin")

def key(im):
    im = im.convert("RGB"); w, h = im.size; px = im.load()
    out = Image.new("RGBA", (w, h)); op = out.load()
    for y in range(h):
        for x in range(w):
            r, g, b = px[x, y]
            d = ((r - 255) ** 2 + g ** 2 + (b - 255) ** 2) ** 0.5
            a = min(1.0, d / 230.0)
            if a <= 0.02: op[x, y] = (0, 0, 0, 0); continue
            fr = (r - (1 - a) * 255) / a; fg = g / a; fb = (b - (1 - a) * 255) / a
            fr, fg, fb = [max(0, min(255, int(v))) for v in (fr, fg, fb)]
            if fr > fg + 60 and fb > fg + 60: fr, fb = (fr + fg) // 2, (fb + fg) // 2
            op[x, y] = (fr, fg, fb, int(a * 255))
    return out

def trim(im, pad=4):
    bb = im.getchannel("A").point(lambda v: 255 if v > 8 else 0).getbbox()
    if bb is None: return im
    x0, y0, x1, y1 = bb
    return im.crop((max(0, x0 - pad), max(0, y0 - pad), min(im.width, x1 + pad), min(im.height, y1 + pad)))

def square(im, size):
    w, h = im.size; s = size / max(w, h)
    im = im.resize((max(1, int(w * s)), max(1, int(h * s))), Image.LANCZOS)
    o = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    o.paste(im, ((size - im.width) // 2, size - im.height))   # bottom-aligned (base of the clump at the bottom)
    return o

for f in ("kusa_kabu.png", "take_ha.png"):
    im = key(Image.open(os.path.join(SRC, f)))
    im = square(trim(im), 1024)
    im.save(os.path.join(OUT, f))
    print(f, im.size)
