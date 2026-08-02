# -*- coding: utf-8 -*-
# うごかしかた:  python tests/test_day.py
#   pip install playwright && playwright install chromium  が いる。
#   絵は tests/_out/ に出る（git には 入れない）。
# ・画面の切りかわりが 白くないか（目に いたくないか）
# ・歩いて ふとんに たどりつけば ちゃんと日が変わるか（キー操作だけで）
# ・移動するほど 夕方になり、日が変わると 朝にもどるか
import sys
import http.server, socketserver, threading, functools, os
from playwright.sync_api import sync_playwright

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "_out")
os.makedirs(OUT, exist_ok=True)
PORT = 0
h = functools.partial(http.server.SimpleHTTPRequestHandler, directory=ROOT)
socketserver.TCPServer.allow_reuse_address = True
srv = socketserver.TCPServer(("127.0.0.1", PORT), h)
PORT = srv.server_address[1]
threading.Thread(target=srv.serve_forever, daemon=True).start()
fails = []

# 画面のまんなかの明るさ（切りかわりのとき 白くなっていないか見る）
LUM = """() => {
  const d = document.getElementById('c').getContext('2d').getImageData(470,260,4,4).data;
  let s=0; for (let i=0;i<d.length;i+=4) s += (d[i]+d[i+1]+d[i+2])/3;
  return Math.round(s/(d.length/4));
}"""

with sync_playwright() as pw:
    b = pw.chromium.launch(args=["--autoplay-policy=no-user-gesture-required"])
    pg = b.new_page(viewport={"width": 960, "height": 540})
    errs = []
    pg.on("pageerror", lambda e: errs.append("PAGEERROR " + str(e)))
    pg.on("console", lambda m: errs.append("CONSOLE " + m.text) if m.type == "error" else None)
    pg.goto(f"http://127.0.0.1:{PORT}/index.html?record=1")
    pg.wait_for_timeout(3600)
    pg.evaluate("window._ctrl.start()"); pg.wait_for_timeout(400)
    pg.evaluate("window._ctrl.free()"); pg.wait_for_timeout(700)

    # 1) 切りかわりの あいだ 画面が 白くならないこと
    pg.evaluate("window._ctrl.goto('zashiki','mae')"); pg.wait_for_timeout(500)
    base = pg.evaluate(LUM)
    pg.keyboard.down("ArrowDown")
    mid = []      # 幕が いちばん濃いときの明るさ
    for _ in range(40):
        pg.wait_for_timeout(35)
        f = pg.evaluate("fade")
        if f > 0.95: mid.append(pg.evaluate(LUM))   # 幕が ほぼ 全部かかっている ところだけ見る
    pg.keyboard.up("ArrowDown"); pg.wait_for_timeout(700)
    print(f"  ふだんの明るさ {base} / 幕が いちばん濃いとき {mid[:6]}")
    if not mid: fails.append("切りかわりを つかまえられなかった")
    elif min(mid) > 40: fails.append(f"切りかわりが 明るすぎる（{min(mid)}）。白い光は 目に いたい")

    # 2) 何度 出入りしても 日は変わらない（＝時間だけ すすむ）ことの かくにん
    pg.evaluate("window._ctrl.goto('zashiki','mae')"); pg.wait_for_timeout(400)
    d0 = pg.evaluate("window._ctrl.dbg()")
    for i in range(12):
        pg.keyboard.down("ArrowDown"); pg.wait_for_timeout(700); pg.keyboard.up("ArrowDown")
        pg.wait_for_timeout(250)
        pg.keyboard.down("ArrowDown"); pg.wait_for_timeout(1400); pg.keyboard.up("ArrowDown")
        pg.wait_for_timeout(250)
    d1 = pg.evaluate("window._ctrl.dbg()")
    st = pg.evaluate("window._ctrl.steps()")
    print(f"  たくさん 出入りしたあと: day={d1['day']} steps={st['steps']} dayT={st['dayT']} ({d1['cur']})")
    if st["steps"] < 6: fails.append("画面を移っても 時間が すすんでいない")
    if d1["day"] != d0["day"]: fails.append("歩いただけで 日が変わってしまった")
    pg.screenshot(path=os.path.join(OUT, "d_evening.png"))

    # 日がくれて けーねが むかえに来ていたら、それが おわるのを 待つ
    for _ in range(700):
        if pg.evaluate("window._ctrl.scene()") is None: break
        pg.wait_for_timeout(200)
    # 晩ごはんと 縁側は とめておく（そっちは test_yoru.py）。
    # ここで 見たいのは「歩いて ふとんに立てば 日が変わる」だけ
    pg.evaluate("window._ctrl.setYoru(true)"); pg.wait_for_timeout(200)
    for _ in range(700):
        if pg.evaluate("window._ctrl.scene()") is None: break
        pg.wait_for_timeout(200)

    # 3) キー操作だけで ふとんまで行って ねられるか
    pg.evaluate("window._ctrl.goto('zashiki','mae')"); pg.wait_for_timeout(500)
    before = pg.evaluate("window._ctrl.dbg()")
    pg.keyboard.down("ArrowLeft")
    reached = False
    for _ in range(40):
        pg.wait_for_timeout(120)
        d = pg.evaluate("window._ctrl.dbg()")
        if d["x"] < 200: reached = True; break
        if d["cur"] != "zashiki": break
    pg.keyboard.up("ArrowLeft")
    d = pg.evaluate("window._ctrl.dbg()")
    print(f"  左へ歩いて ふとんへ: 着いた={reached} {d['x']},{d['y']} ({d['cur']})")
    pg.screenshot(path=os.path.join(OUT, "d_onfuton.png"))
    pg.wait_for_timeout(2200)
    d2 = pg.evaluate("window._ctrl.dbg()")
    print(f"  じっとして 2.2秒後: day={d2['day']} scene={d2['scene']}")
    if d2["day"] != before["day"] + 1:
        fails.append(f"歩いて ふとんに立っても 日が変わらない（{before['day']}→{d2['day']} at {d['x']},{d['y']}）")

    # 4) 日が変わったら 朝にもどる（steps が 0 に）
    for _ in range(700):
        pg.wait_for_timeout(200)
        if pg.evaluate("window._ctrl.scene()") is None: break
    st2 = pg.evaluate("window._ctrl.steps()")
    print(f"  2日目のはじめ: steps={st2['steps']} dayT={st2['dayT']}")
    if st2["steps"] != 0: fails.append("日が変わっても 夕方のまま")
    pg.screenshot(path=os.path.join(OUT, "d_day2.png"))

    print("\nerrors:", errs[:5])
    if errs: fails.append("エラー " + str(errs[:3]))
    b.close()
srv.shutdown()
print("\n=== 失敗 ===" if fails else "\n=== すべて OK ===")
for f in fails: print(" -", f)

sys.exit(1 if fails else 0)
