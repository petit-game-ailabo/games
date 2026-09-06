# 8方向の 立ち絵と まばたきを シートに 入れる（2026-09-06・D-249）。
#
# ★これで `make_marisa_tachi.py`（走りの コマから いちばん 立ちに 近い ものを えらぶ
#   応急処置・D-240）は **お役ごめん**。本物の 立ち絵が 来た。
#
# もらう 絵：マゼンタの 上に 4列x4行＝16コマ。
#   上の 8つ＝目を あけた 立ち（1 正面／2 左ななめ前／3 真左／4 左ななめ奥／
#              5 真後ろ／6 右ななめ奥／7 真右／8 右ななめ前）
#   下の 8つ＝同じ 絵で 目だけ とじた もの
#
# ★大きさは **全身の たけ**で そろえる。
#   帽子の つばの 横はばは **向きを またぐと つかえない**（真横では つばが
#   すぼまって 描かれる。実測 88〜133px）。
#   いっぽう **全身を 312 に そろえた あとの 帽子の たては 8方向とも 140px**
#   （ブレ 0.5%）だった＝頭の 大きさは もともと そろって いる。
#
# ★横は **足もとの まん中**で そろえる。立ち絵は 足が 動かない ので これが 素直。
#   走りの 8コマの 足もとの まん中は 108〜113（ブレ 5px）なので ほぼ そろう。
#
# run: python unity/ArtSource/make_marisa_tachi8.py
import os
import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
SPR = os.path.join(HERE, "..", "Assets", "Art", "Sprites")
MOTO = os.path.join(HERE, "ref", "codex", "tachi_8muki_raw.png")
CW, CH = 224, 336
TAKE, JIMEN = 312.0, 332
TACHI, MABATAKI = 8, 9
# ★まばたきの 絵が 立ちと 合って いない 向き（実測の ちがいが 大きい）。
#   そこは まばたきを あきらめて 立ちを そのまま 入れる。
#   4・5・6（左ななめ奥／真後ろ／右ななめ奥）は **うしろ姿で 目が 見えない**。
#   そこで まばたきを 出すと、目 いがいの ちがい（髪など）だけが ちらついて 見える。
#   実測：真後ろの 立ちと まばたきの ちがいは 669画素 あった（目は 写って いないのに）
MABATAKI_NASHI = {4, 5, 6}


def kugiri(v, mn=40):
    out, s = [], None
    for i, x in enumerate(v):
        if x and s is None:
            s = i
        if not x and s is not None:
            if i - s >= mn:
                out.append((s, i - 1))
            s = None
    if s is not None and len(v) - s >= mn:
        out.append((s, len(v) - 1))
    return out


def main():
    im = Image.open(MOTO).convert("RGB")
    a = np.asarray(im).astype(int)
    R, G, B = a[..., 0], a[..., 1], a[..., 2]
    fg = ~((R > 150) & (B > 150) & (G < 120) & ((R - G) > 60) & ((B - G) > 60))
    fi = Image.fromarray((fg * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(3))
    fg = np.asarray(fi.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MaxFilter(3))) > 128
    rows, cols = kugiri(fg.any(1)), kugiri(fg.any(0))
    if len(rows) != 4 or len(cols) != 4:
        print("!! 4x4 に ならない: 行", len(rows), "列", len(cols))
        return
    src = Image.fromarray(np.dstack([a, np.where(fg, 255, 0)]).astype(np.uint8))
    box = [(x0, y0, x1, y1) for (y0, y1) in rows for (x0, x1) in cols]

    sheet = Image.open(os.path.join(SPR, "marisa_codex.png")).convert("RGBA")

    def ireru(i, gyo):
        x0, y0, x1, y1 = box[i]
        s = fg[y0:y1 + 1, x0:x1 + 1]
        ys, xs = np.nonzero(s)
        t, b = int(ys.min()), int(ys.max())
        bai = TAKE / float(b - t + 1)
        piece = src.crop((x0 + int(xs.min()), y0 + t, x0 + int(xs.max()) + 1, y0 + b + 1))
        nw = max(1, int(round((xs.max() - xs.min() + 1) * bai)))
        nh = max(1, int(round((b - t + 1) * bai)))
        piece = piece.resize((nw, nh), Image.LANCZOS)
        # 横は 足もとの まん中
        obi = s[b - int((b - t) * 0.10):b + 1, :]
        fx = np.nonzero(obi.any(0))[0]
        cx = ((fx.min() + fx.max()) / 2.0 - xs.min()) * bai
        col = i % 8
        sheet.paste(Image.new("RGBA", (CW, CH), (0, 0, 0, 0)), (col * CW, gyo * CH))
        sheet.alpha_composite(piece, (col * CW + int(round(CW * 0.5 - cx)),
                                      gyo * CH + JIMEN - nh))
        return nh

    for i in range(8):
        nh = ireru(i, TACHI)
        j = i if (i + 1) in MABATAKI_NASHI else i + 8
        ireru(j, MABATAKI)
        print("  col%d ← コマ%2d（まばたき コマ%2d） たけ%d" % (i, i + 1, j + 1, nh))
    p = os.path.join(SPR, "marisa_codex.png")
    sheet.save(p)
    print("かいた:", p)

    # ---- 検査
    al = np.asarray(sheet)[..., 3] > 96
    at, tk = [], []
    for c in range(8):
        x = al[TACHI * CH:(TACHI + 1) * CH, c * CW:(c + 1) * CW]
        ys, _ = np.nonzero(x)
        at.append(int(ys.min())); tk.append(int(ys.max() - ys.min() + 1))
    print("頭の てっぺん:", at, " ブレ %dpx（めやす 5px）" % (max(at) - min(at)))
    print("全身の たけ  :", tk, " ブレ %.1f%%" % ((max(tk) / min(tk) - 1) * 100))


if __name__ == "__main__":
    main()
