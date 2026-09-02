# 虫の 絵を **OpenAI の 画像API（gpt-image-1）で 直接 作る**（2026-09-03）。
#
# ★本人「君から直接codexにプロンプト投げて、画像を受け取る方法ってないかな？」
#   Codex（ChatGPT）そのものには 外から 投げる 口が 無い ので、同じ OpenAI の **画像API**を つかう。
#   ・鍵は evn/OpenAI.env に  OPENAI_API_KEY=sk-...  と 1行（evn/ は リポジトリに 入らない）
#   ・API は ChatGPT の 定額とは 別の **従量課金**（1024px 1まい 数円〜十数円。品質 medium）
#   ・背景は API の transparent を つかう ので マゼンタ抜きは 要らない（そのまま make_mushi.py へ）
#
# 走らせかた:
#   python unity/ArtSource/make_mushi_gen.py               # mushi_list.txt の ぜんぶ（できて いる ものは とばす）
#   python unity/ArtSource/make_mushi_gen.py kamakiri_yoko  # 1まいだけ
#   pip install openai   （はじめて の とき）
#
# 一覧（unity/ArtSource/mushi_list.txt）：1行 1まい。  ファイル名 | 英語の プロンプト
import os, sys, base64

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(HERE, "mushi")
LIST = os.path.join(HERE, "mushi_list.txt")
ENV = os.path.join(ROOT, "..", "evn", "OpenAI.env")

KYOTSU = ("Photorealistic macro photograph of ONE Japanese insect, whole body visible, centered, "
          "filling about 80% of the frame. Transparent background. No ground, no leaves, no cast shadow, "
          "no glow, no other objects, no text or watermark. Sharp focus, even soft studio lighting, high detail. "
          "For a top view the head points to the top of the frame; for a side view the head points to the left.")


def kagi():
    k = os.environ.get("OPENAI_API_KEY")
    if k: return k
    if os.path.exists(ENV):
        for line in open(ENV, encoding="utf-8"):
            line = line.strip()
            if line.startswith("OPENAI_API_KEY="): return line.split("=", 1)[1].strip().strip('"')
    sys.exit("鍵が ない: evn/OpenAI.env に OPENAI_API_KEY=... を 書く")


def yomu():
    rows = []
    for line in open(LIST, encoding="utf-8"):
        line = line.strip()
        if not line or line.startswith("#"): continue
        name, prompt = [x.strip() for x in line.split("|", 1)]
        rows.append((name, prompt))
    return rows


def main():
    try:
        from openai import OpenAI
    except ImportError:
        sys.exit("pip install openai")
    client = OpenAI(api_key=kagi())
    os.makedirs(OUT, exist_ok=True)
    dake = set(sys.argv[1:])
    for name, prompt in yomu():
        if dake and name not in dake: continue
        path = os.path.join(OUT, name + ".png")
        if not dake and os.path.exists(path): continue
        print("作る", name)
        r = client.images.generate(model="gpt-image-1", prompt=prompt + " " + KYOTSU,
                                   size="1024x1024", quality="medium", background="transparent",
                                   output_format="png", n=1)
        open(path, "wb").write(base64.b64decode(r.data[0].b64_json))
    print("できた。次: python unity/ArtSource/make_mushi.py")


if __name__ == "__main__":
    main()
