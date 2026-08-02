# -*- coding: utf-8 -*-
# うごかしかた:  python tests/test_yoru.py
#   pip install playwright && playwright install chromium  が いる。
#   絵は tests/_out/ に出る（git には 入れない）。
# 日がくれて うちに かえったら 晩ごはん → 縁側 → ねられる状態、に なるか
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


def run_scene(pg, limit=700, shot=None):
    """場面が おわるまで 見まもる。通った画面と セリフを かえす"""
    screens, says = [], []
    for i in range(limit):
        pg.wait_for_timeout(150)
        sc = pg.evaluate("window._ctrl.scene()")
        if sc is None:
            if screens or says: break
            continue
        cur = pg.evaluate("window._ctrl.dbg()")["cur"]
        if not screens or screens[-1] != cur: screens.append(cur)
        if sc["say"] and (not says or says[-1] != sc["say"][1]): says.append(sc["say"][1])
        if shot and len(says) == 3:
            pg.screenshot(path=os.path.join(OUT, shot)); shot = None
    return screens, says


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

    # 1) むかえを とめて、ざしきで 日ぐれを むかえたら 晩ごはんが はじまるか
    pg.evaluate("window._ctrl.setMukae(true)")
    pg.evaluate("window._ctrl.goto('zashiki','mae')"); pg.wait_for_timeout(500)
    pg.evaluate("window._ctrl.setSteps(24)")
    # **はなしの とちゅうでは 割りこまない**ので、ざしきの 大妖精との 会話（1日目は9行・
    # だいたい25秒）が おわるのを 待つ ぶんも 見こむ。むかえと おなじ考え方
    began = False
    for _ in range(220):
        pg.wait_for_timeout(200)
        if pg.evaluate("window._ctrl.dbg()")["scene"]: began = True; break
    print(f"  日ぐれに ざしきに 居ると はじまる: {began}")
    if not began: fails.append("日がくれて うちに 居ても 晩ごはんが はじまらない")

    screens, says = run_scene(pg, shot="y_dinner.png")
    d = pg.evaluate("window._ctrl.dbg()")
    print(f"  通った画面: {' → '.join(screens)}")
    print(f"  セリフ{len(says)}こ  さいご='{says[-1] if says else ''}'")
    print(f"  おわったところ: {d['cur']} ({d['state']})")
    # 晩ごはんは どま、縁側は ろうか、さいごは ざしきに もどる
    for want in ("doma", "rouka"):
        if want not in screens: fails.append(f"晩ごはんの ながれで {want} を 通っていない")
    if screens and screens[-1] != "zashiki": fails.append(f"ざしきに もどっていない（{screens[-1]}）")
    if d["state"] != "play": fails.append(f"自由行動に なっていない（{d['state']}）")
    if says and says[-1] != "おやすみ":
        fails.append(f"さいごが「おやすみ」で おわっていない（{says[-1] if says else 'なし'}）")
    pg.screenshot(path=os.path.join(OUT, "y_after.png"))

    # 2) ふとんまで 歩ける ところに 立っているか（ねる のは じぶんで きめること）
    dd = ((d["x"]-180)**2 + (d["y"]-456)**2) ** 0.5
    print(f"  かえったあとの立ち位置: {d['x']},{d['y']}  ふとんまで {dd:.0f}px")
    if dd < 66: fails.append("かえった しゅんかん ふとんの上に 立っている（かってに ねてしまう）")

    # 3) 1日に 1回だけ
    pg.evaluate("window._ctrl.setSteps(30)"); pg.wait_for_timeout(2500)
    d2 = pg.evaluate("window._ctrl.dbg()")
    print(f"\n  もう一度 日がくれても: scene={d2['scene']} yoru={d2['yoru']}")
    if d2["scene"]: fails.append("晩ごはんが 1日に なんども はじまる")

    # 4) ねた つぎの日は また ある
    pg.evaluate("window._ctrl.sleep()")
    for _ in range(900):
        pg.wait_for_timeout(200)
        if pg.evaluate("window._ctrl.scene()") is None: break
    d3 = pg.evaluate("window._ctrl.dbg()")
    print(f"  2日目のはじめ: day={d3['day']} yoru={d3['yoru']}")
    if d3["yoru"]: fails.append("日が変わっても 晩ごはんの しるしが 残っている")

    pg.evaluate("window._ctrl.setMukae(true)")
    pg.evaluate("window._ctrl.goto('zashiki','mae')"); pg.wait_for_timeout(400)
    pg.evaluate("window._ctrl.setSteps(24)"); pg.wait_for_timeout(800)
    began2 = pg.evaluate("window._ctrl.dbg()")["scene"]
    print(f"  2日目の日ぐれ: scene={began2}")
    if not began2: fails.append("2日目に 晩ごはんが はじまらない")
    s2, says2 = run_scene(pg)
    print(f"  2日目は 別のセリフか: {says2[0] if says2 else 'なし'}")
    if says and says2 and says[0] == says2[0]:
        fails.append("2日目も 1日目と 同じ セリフ")

    print("\nerrors:", errs[:5])
    if errs: fails.append("エラー " + str(errs[:3]))
    b.close()
srv.shutdown()
print("\n=== 失敗 ===" if fails else "\n=== すべて OK ===")
for f in fails: print(" -", f)

sys.exit(1 if fails else 0)
