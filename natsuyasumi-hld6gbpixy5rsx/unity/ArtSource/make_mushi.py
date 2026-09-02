# 虫の 絵を、本人が Codex で 作った リアルな 画像（マゼンタ背景）から こしらえる（2026-09-02）。
#
#   もと: unity/ArtSource/mushi/<名前>_<向き>.png（1024x1024・背景 #FF00FF・リポジトリに 入れない）
#   出す: Assets/Art/Sprites/mushi/<名前>_<向き>.png（512x512 RGBA・切りぬきずみ）
#
# ★キー抜きは「マゼンタとの 距離 → 透明度」では 終わらない。トンボの 羽の ように **すける ところは
#   もとの 色と マゼンタの 混色**に なって いる ので、p = a*f + (1-a)*bg を f について 解いて
#   （unmix）もとの 色に もどす。これを しないと 羽が 紫に にごる。
# ★色数を 落として 魔理沙（絵）の 調子に 寄せる。写真の まま だと 木の 写真に 溶けて 消える
#
# 走らせかた: python unity/ArtSource/make_mushi.py
import os
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "mushi")
OUT = os.path.join(HERE, "..", "Assets", "Art", "Sprites", "mushi")
BG = (255, 0, 255)
OUTPX = 512      # 出す 大きさ（寄りカードで 1/2画面、世界では 縮めて つかう）
IRO = 24         # 色数


def key(im):
    """マゼンタを 抜いて RGBA に。混色を 解く"""
    im = im.convert("RGB")
    w, h = im.size
    px = im.load()
    out = Image.new("RGBA", (w, h))
    op = out.load()
    T = 230.0
    for y in range(h):
        for x in range(w):
            r, g, b = px[x, y]
            # マゼンタとの 距離（G が 上がる／R,B が 下がる ほど 遠い）
            d = ((r - 255) ** 2 + g ** 2 + (b - 255) ** 2) ** 0.5
            a = min(1.0, d / T)
            if a <= 0.02:
                op[x, y] = (0, 0, 0, 0); continue
            # unmix: f = (p - (1-a)*bg) / a
            fr = (r - (1 - a) * 255) / a
            fg = g / a
            fb = (b - (1 - a) * 255) / a
            fr, fg, fb = [max(0, min(255, int(v))) for v in (fr, fg, fb)]
            # のこった 紫かぶり（R,B が G より ずっと 高い）を おさえる
            if fr > fg + 60 and fb > fg + 60:
                m = (fr + fb) // 2
                fr, fb = (fr + fg) // 2, (fb + fg) // 2
            op[x, y] = (fr, fg, fb, int(a * 255))
    return out


def trim(im, pad=8):
    bb = im.getchannel("A").point(lambda v: 255 if v > 8 else 0).getbbox()
    if bb is None: return im
    x0, y0, x1, y1 = bb
    x0 = max(0, x0 - pad); y0 = max(0, y0 - pad); x1 = min(im.width, x1 + pad); y1 = min(im.height, y1 + pad)
    return im.crop((x0, y0, x1, y1))


def square(im, size):
    """正方形の 中に 収める（縦横比は 保つ）"""
    w, h = im.size
    s = size / max(w, h)
    im = im.resize((max(1, int(w * s)), max(1, int(h * s))), Image.LANCZOS)
    o = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    o.paste(im, ((size - im.width) // 2, (size - im.height) // 2))
    return o


def posterize(im, n):
    """色数を 落とす（透明は 別に あつかう）"""
    a = im.getchannel("A")
    rgb = im.convert("RGB").quantize(colors=n, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGB")
    rgb.putalpha(a)
    return rgb


def outline(im):
    """ふち：内がわ 2px の 暗い 線 ＋ 外がわ 1px の 明るい 縁取り。
    ★暗い 虫（カブト・クワガタ・セミ）が 暗い 樹皮に 乗ると、画面 12〜30px では
      樹皮の 傷と 見わけが つかない（2026-09-02 実測）。明るい 縁取りが 虫を 幹から 浮かせる。
      魔理沙も「線の ある 絵」なので 絵柄としても そろう"""
    a = im.getchannel("A")
    inner = Image.eval(a, lambda v: 255 if v > 40 else 0)
    g1 = inner.filter(ImageFilter.MaxFilter(5))     # +2px
    g2 = g1.filter(ImageFilter.MaxFilter(3))        # +3px
    ring_dark = Image.composite(Image.new("L", im.size, 0), g1, inner)   # 内がわ 2px
    ring_light = Image.composite(Image.new("L", im.size, 0), g2, g1)     # その 外 1px
    light = Image.new("RGBA", im.size, (236, 228, 200, 0))
    light.putalpha(ring_light.point(lambda v: 170 if v else 0))
    dark = Image.new("RGBA", im.size, (28, 22, 24, 0))
    dark.putalpha(ring_dark.point(lambda v: 210 if v else 0))
    return Image.alpha_composite(Image.alpha_composite(light, dark), im)


os.makedirs(OUT, exist_ok=True)
names = sorted(f for f in os.listdir(SRC) if f.lower().endswith(".png"))
for f in names:
    im = Image.open(os.path.join(SRC, f))
    im = key(im)
    im = trim(im)
    im = square(im, OUTPX)
    im = posterize(im, IRO)
    im = outline(im)
    im.save(os.path.join(OUT, f))
    print(f, im.size)
print("ok", len(names))
