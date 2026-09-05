# 主人公の「歩き」を 作る（2026-09-06）。
#
# WHY: PLAN の 未了「主人公の 動きの 見直し」に こう 書いて あった——
#      **8コマが 1枚ずつ 生成されて いて 手足の 通り道が 一貫して いない
#      （並べ替えでは 埋まらない）**／**歩きの コマが 無い**（Shiftの 歩きは
#      走りの スロー再生）。本人の 不満は ぜんぶ ここに 帰着する。
#      絵の きれいさの 話では ない ので、生成を やりなおしても 直らない。
#
# WHAT: 描いて ある 立ち絵を **腰で 切って 紙人形に 組み**、回転で 中割りを 作る。
#       中割りが 補間で 生まれる ので、手足の 通り道は 必ず つながる。
#
#       ★はじめ 腕も 切ろうと したが、**色では 腕と 髪が 分けられない**
#         （実測：素肌の R-B は 18〜66、髪は 64。重なる）。
#         スカートの すそ から 下は 脚しか 無い ので、**そこだけ 切る**。
#         腕は 立ち絵の まま（歩きなら 60pxで ほぼ わからない）。
#
#       ★既存の 走り（row0..7）は さわらない。**無かった 歩きを 足す** だけに する。
#         行を 増やす と CharSprite の Rows も 変える 必要が ある ので、
#         まず 絵を 作って 見てから 配線する。
#
# run: python unity/ArtSource/make_marisa_ugoki.py            8方向の 歩き 8コマを 焼く
#      python unity/ArtSource/make_marisa_ugoki.py --miru     正面だけ 並べて 見せる
import os
import sys
import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
SPR = os.path.join(HERE, "..", "Assets", "Art", "Sprites")
MOTO = os.path.join(SPR, "marisa_walk.png")
CW, CH, COLS = 224, 336, 8
KOMA = 8                       # 歩きの コマ数

# 絵に 目盛りを 引いて 読んだ 値（正面 col0 row8）
SUSO = 262                     # スカートの すそ。ここから 下は 脚だけ
KOSHI_Y = 232                  # 脚を 振る 軸の 高さ（スカートの 中）
NOBASU = SUSO - KOSHI_Y + 8    # 脚の 上を ここまで のばす（回して すき間を 作らない）

FURI = 13.0                    # 歩きの 振り幅（度）。走りは もっと 大きい
HAZUMI = 2.6                   # 上下の はずみ（px）
AGE = 7.0                      # 正面のとき 足を 上げる 量（px）
HIRAKI = 4.0                   # 正面のとき 左右に ひらく 量（px）


def cell(im, c, r):
    return im.crop((c * CW, r * CH, (c + 1) * CW, (r + 1) * CH))


def wakeru(a):
    """立ち絵を 「腰から 上」と 「左右の 脚」に 分ける"""
    A = np.asarray(a).astype(np.uint8)
    m = A[..., 3] > 128
    yy = np.arange(CH)[:, None] * np.ones((1, CW), dtype=int)

    sita = m & (yy > SUSO)
    if not sita.any():
        return None
    xs = np.nonzero(sita.any(0))[0]
    mannaka = (xs.min() + xs.max()) * 0.5

    xx = np.ones((CH, 1), dtype=int) * np.arange(CW)[None, :]

    def kiru(msk):
        o = np.zeros((CH, CW, 4), dtype=np.uint8)
        o[msk] = A[msk]
        return o

    ue = kiru(m & (yy <= SUSO))
    # ★横むきは 両脚が 重なって いる ので、まん中で 切ると **靴の 一部が
    #   切りはなされて 取り残される**（実測：x80,y315 に 黒い 破片）。
    #   すその ところ（＝かならず 脚）と つながって いる ぶん だけ 残す
    aL = kiru(hitotsuduki(sita & (xx < mannaka)))
    aR = kiru(hitotsuduki(sita & (xx >= mannaka)))
    return ue, nobasu(aL), nobasu(aR), mannaka


def hitotsuduki(m):
    """いちばん 上の 行と つながって いる ぶん だけ 残す"""
    ys = np.nonzero(m.any(1))[0]
    if len(ys) == 0:
        return m
    seed = np.zeros_like(m)
    seed[ys.min():ys.min() + 3] = m[ys.min():ys.min() + 3]
    cur = seed
    while True:
        nxt = cur.copy()
        nxt[1:, :] |= cur[:-1, :]
        nxt[:-1, :] |= cur[1:, :]
        nxt[:, 1:] |= cur[:, :-1]
        nxt[:, :-1] |= cur[:, 1:]
        nxt &= m
        if nxt.sum() == cur.sum():
            return nxt
        cur = nxt


def nobasu(p):
    """脚の 上を のばす。軸が スカートの 中に ある ので、
    見えて いる ぶん だけ だと 回した とき すき間が 出る。
    のばした ぶんは スカート（上の 層）が 隠す"""
    o = p.copy()
    ys = np.nonzero((o[..., 3] > 128).any(1))[0]
    if len(ys) == 0:
        return o
    top = ys.min()
    row = o[top].copy()
    # ★のばすのは **脚の 太さぶんだけ**。行を まるごと のばすと
    #   すそに かかった スカートの 黒も 一緒に のびて、回した とき
    #   スカートの 外に はみ出し **黒い 帯**に なる（2026-09-06）
    xs = np.nonzero(row[:, 3] > 128)[0]
    if len(xs) == 0:
        return o
    cx = (xs.min() + xs.max()) * 0.5
    haba = min(xs.max() - xs.min(), 24)
    keep = np.zeros(CW, dtype=bool)
    keep[int(cx - haba * 0.5):int(cx + haba * 0.5) + 1] = True
    row[~keep] = 0
    for y in range(max(0, top - NOBASU), top):
        o[y] = row
    return o


def mawasu(p, kaku, jiku):
    """p を jiku(x,y) のまわりに kaku度 回す"""
    if abs(kaku) < 0.01:
        return Image.fromarray(p)
    im = Image.fromarray(p)
    return im.rotate(-kaku, resample=Image.BICUBIC, center=jiku)


def komawari(ue, aL, aR, mannaka, t, muki):
    """t = 0..1 の 1コマ。muki = 0..7（0＝手前むき・2＝画面左・4＝奥・6＝画面右）

    ★向きで 動きが ちがう。ここを 1つに すると まちがう（2026-09-06）。
      ・横むき … 脚は **前後に 振る**＝画面では 回転に 見える
      ・正面／うしろ … 前後の 動きは 画面では **奥ゆき**なので 回転しない。
        足が **上がって 下りる**＋左右に すこし ひらく
      ・ななめ … その あいだ
    """
    ph = t * 2.0 * np.pi
    kaku = np.radians(muki * 45.0)
    yoko = abs(np.sin(kaku))          # 横むきぐあい
    mae = 1.0 - yoko                  # 正面／うしろぐあい

    out = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
    bob = -HAZUMI * abs(np.cos(ph))   # 脚が そろう ところで 体が 上がる
    ashi = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))

    for p, pph, sg in ((aL, ph, -1.0), (aR, ph + np.pi, +1.0)):
        s = np.sin(pph)
        rot = FURI * s * yoko                       # 横むき＝回す
        age = -AGE * max(0.0, s) * mae              # 正面＝上げる
        hiraki = HIRAKI * s * mae * sg * -1.0       # 正面＝すこし ひらく
        jiku = (mannaka + sg * 8, KOSHI_Y)
        im = mawasu(p, rot, jiku)
        im = im.transform(im.size, Image.AFFINE,
                          (1, 0, -hiraki, 0, 1, -(bob + age)), resample=Image.BICUBIC)
        ashi.alpha_composite(im)

    # ★すその 上は 描かない。のばした ぶんが 回って スカートの 外に はみ出すと
    #   **黒い 帯**に なる（2026-09-06）。のばすのは すき間よけ なので、
    #   はみ出しは ここで 落とす
    kiri = int(SUSO - 1 + bob)
    an = np.asarray(ashi).copy()
    an[:max(0, kiri), :, 3] = 0
    out.alpha_composite(Image.fromarray(an))

    ui = Image.fromarray(ue).transform(
        (CW, CH), Image.AFFINE, (1, 0, 0, 0, 1, -bob), resample=Image.BICUBIC)
    out.alpha_composite(ui)
    return out


def main():
    im = Image.open(MOTO).convert("RGBA")
    miru = "--miru" in sys.argv

    if miru:
        w = wakeru(cell(im, 0, 8))
        if w is None:
            raise SystemExit("脚が 見つからない")
        ue, aL, aR, mn = w
        out = Image.new("RGB", (CW * KOMA, (CH + 22) * 2), (245, 244, 240))
        d = ImageDraw.Draw(out)
        for j, (nm, col) in enumerate((("shomen (col0)", 0), ("yoko (col2)", 2))):
            w2 = wakeru(cell(im, col, 8))
            u2, l2, r2, m2 = w2
            d.text((4, 4 + j * (CH + 22)), "aruki 8 koma — " + nm, fill=(20, 20, 20))
            for i in range(KOMA):
                f = komawari(u2, l2, r2, m2, i / float(KOMA), col)
                bg = Image.new("RGB", (CW, CH), (245, 244, 240))
                bg.paste(f, (0, 0), f)
                out.paste(bg, (CW * i, 22 + j * (CH + 22)))
        p = os.path.abspath(os.path.join(HERE, "..", "..", "..", "marisa_aruki.png"))
        out.save(p)
        print("かいた:", p)
        return

    sheet = Image.new("RGBA", (CW * COLS, CH * KOMA), (0, 0, 0, 0))
    for c in range(COLS):
        w = wakeru(cell(im, c, 8))
        if w is None:
            print("col", c, "脚が 見つからない → 立ち絵の まま")
            for i in range(KOMA):
                sheet.paste(cell(im, c, 8), (c * CW, i * CH))
            continue
        ue, aL, aR, mn = w
        for i in range(KOMA):
            sheet.paste(komawari(ue, aL, aR, mn, i / float(KOMA), c), (c * CW, i * CH))
        print("col", c, "ok", flush=True)
    p = os.path.join(SPR, "marisa_aruki.png")
    sheet.save(p)
    print("かいた:", p, sheet.size)


if __name__ == "__main__":
    main()
