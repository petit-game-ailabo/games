# -*- coding: utf-8 -*-
# うごかしかた:  python tests/test_mukae.py
#   pip install playwright && playwright install chromium  が いる。
#   絵は tests/_out/ に出る（git には 入れない）。
# 日がくれたら けーねが むかえに来るか。どの画面に居ても なりたつか
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
SCREENS = ["mori", "aze", "iemae", "rouka", "doma", "zashiki"]
fails = []

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

    for si, s in enumerate(SCREENS):
        pg.evaluate("window._ctrl.setSteps(0)"); pg.evaluate("window._ctrl.setMukae(false)")
        # 晩ごはんは とめておく。ここで 見たいのは むかえ だけ（晩ごはんは test_yoru.py）
        pg.evaluate("window._ctrl.setYoru(true)")
        pg.evaluate(f"window._ctrl.goto('{s}')"); pg.wait_for_timeout(500)
        # 会話が はじまっていたら 離しておく
        pg.evaluate("window._ctrl.setSteps(24)"); pg.wait_for_timeout(400)
        began = pg.evaluate("window._ctrl.dbg()")["scene"]
        if not began:
            pg.wait_for_timeout(4000)
            began = pg.evaluate("window._ctrl.dbg()")["scene"]
        says, shot = [], False
        for _ in range(400):
            pg.wait_for_timeout(150)
            sc = pg.evaluate("window._ctrl.scene()")
            if sc is None: break
            if sc["say"] and (not says or says[-1] != sc["say"][1]):
                says.append(sc["say"][1])
                if not shot and len(says) == 2:
                    shot = True
                    pg.screenshot(path=os.path.join(OUT, f"k_{s}.png"))
        d = pg.evaluate("window._ctrl.dbg()")
        ok = began and d["state"] == "play" and d["cur"] == "zashiki"
        print(f"  {s:8s} むかえ={began} → {d['cur']} ({d['state']})  セリフ{len(says)} {'OK' if ok else 'NG'}")
        if not began: fails.append(f"{s}: 日がくれても むかえに来ない")
        elif d["cur"] != "zashiki": fails.append(f"{s}: ざしきに かえっていない（{d['cur']}）")
        # ふとんの そばに 立っているか
        dd = ((d["x"]-180)**2 + (d["y"]-456)**2) ** 0.5
        if si == 0: print(f"     かえったあとの立ち位置: {d['x']},{d['y']}  ふとんまで {dd:.0f}px")

    # 1日に 1回だけ
    pg.evaluate("window._ctrl.setYoru(true)")
    pg.evaluate("window._ctrl.setSteps(30)"); pg.wait_for_timeout(2500)
    d = pg.evaluate("window._ctrl.dbg()")
    print(f"\n  もう一度 日がくれても: scene={d['scene']} mukae={d['mukae']}")
    if d["scene"]: fails.append("むかえが 1日に なんども 来る")

    # ねると つぎの日は また来る
    pg.evaluate("window._ctrl.sleep()")
    for _ in range(700):
        pg.wait_for_timeout(200)
        if pg.evaluate("window._ctrl.scene()") is None: break
    st = pg.evaluate("window._ctrl.steps()")
    d0 = pg.evaluate("window._ctrl.dbg()")
    print(f"  2日目のはじめ: steps={st['steps']} mukae={st['mukae']} yoru={d0['yoru']}")
    if st["mukae"]: fails.append("日が変わっても むかえの しるしが 残っている")
    if d0["yoru"]: fails.append("日が変わっても 晩ごはんの しるしが 残っている")
    pg.evaluate("window._ctrl.setYoru(true)")
    pg.evaluate("window._ctrl.setSteps(24)"); pg.wait_for_timeout(3000)
    d2 = pg.evaluate("window._ctrl.dbg()")
    print(f"  2日目の日ぐれ: scene={d2['scene']}")
    if not d2["scene"]: fails.append("2日目に むかえが 来ない")

    print("\nerrors:", errs[:5])
    if errs: fails.append("エラー " + str(errs[:3]))
    b.close()
srv.shutdown()
print("\n=== 失敗 ===" if fails else "\n=== すべて OK ===")
for f in fails: print(" -", f)

sys.exit(1 if fails else 0)
