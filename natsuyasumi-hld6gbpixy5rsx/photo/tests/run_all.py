# -*- coding: utf-8 -*-
# 検査を ぜんぶ うごかす。
#   python tests/run_all.py            ぜんぶ
#   python tests/run_all.py screens    名まえに screens を ふくむ ものだけ
#
# 1本ずつ ブラウザを 立ちあげるので、ぜんぶで 5〜8分 かかる。
import os, subprocess, sys, time

# Windows の コンソールは 既定が cp932 で、日本語の記号（—）を 出すと 落ちる。
# 環境に たよらず utf-8 で 出す。
try: sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception: pass

HERE = os.path.dirname(os.path.abspath(__file__))
TESTS = [
    ("test_screens.py",    "6画面：出発点・歩ける範囲・つながり・会話・近づけるか"),
    ("test_talk_range.py", "会話の近さを 地面のうえの距離で はかっているか"),
    ("test_morning.py",    "朝のながれ：起床→たいそう→ごはん→自由行動／日づけ"),
    ("test_poses.py",      "キャラのすがた・転送しない・場面あけに 会話が はじまらない"),
    ("test_day.py",        "切りかわりの幕の色・夕方・ふとんで日が変わる"),
    ("test_mukae.py",      "日ぐれに けーねが むかえに来る"),
    ("test_yoru.py",       "日ぐれに かえったら 晩ごはん→縁側→ねられる"),
    ("test_yoyaku.py",     "きょう行った ところが ごはんの 話に なる（よやく）"),
    ("test_oto.py",        "時間帯で 鳴くものが 変わる（耳でも 時間が わかる）"),
    ("test_field.py",      "画面の中の 虫とり：蝶を あみで その場で とる（P4a）"),
    ("test_gate.py",       "塞がれた道：見てから 道具で 自分で どかす（P6）"),
]

want = sys.argv[1] if len(sys.argv) > 1 else ""
runs = [t for t in TESTS if want in t[0]]
if not runs:
    print("あてはまる検査が ない:", want); sys.exit(2)

bad = []
for name, what in runs:
    print("\n" + "="*66)
    print("  " + name + "  —  " + what)
    print("="*66)
    t0 = time.time()
    r = subprocess.run([sys.executable, os.path.join(HERE, name)],
                       capture_output=True, text=True, encoding="utf-8", errors="replace")
    # http.server の アクセスログは じゃまなので 消す
    for ln in (r.stdout or "").split("\n"):
        if '"GET /' not in ln: print(ln)
    if r.returncode != 0:
        bad.append(name)
        tail = (r.stderr or "").strip().split("\n")[-6:]
        if tail and tail[0]: print("  stderr:", "\n  ".join(tail))
    print(f"  （{time.time()-t0:.0f}秒）")

print("\n" + "="*66)
if bad:
    print("  しっぱい:", ", ".join(bad))
    sys.exit(1)
print("  ぜんぶ とおった（" + str(len(runs)) + "本）")
