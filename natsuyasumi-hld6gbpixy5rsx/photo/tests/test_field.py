# -*- coding: utf-8 -*-
# 画面の中の 虫とり（P4a）：蝶が 世界に とび、あみで その場で とれるか。
#   ・昼は 蝶が とぶ／夜は とばない
#   ・あみが ないと ふれない、あると ふって とれる
#   ・とると 虫かご+1・図鑑に 種類・その日の のこりが へる（有限）
import sys, os, http.server, socketserver, threading, functools
from playwright.sync_api import sync_playwright

try: sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception: pass

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
h = functools.partial(http.server.SimpleHTTPRequestHandler, directory=ROOT)
socketserver.TCPServer.allow_reuse_address = True
srv = socketserver.TCPServer(("127.0.0.1", 0), h)
PORT = srv.server_address[1]
threading.Thread(target=srv.serve_forever, daemon=True).start()
fails = []

def drain(pg):
    for _ in range(50):
        if not pg.evaluate("window._ctrl.scene()"): return
        pg.keyboard.press("Space"); pg.wait_for_timeout(110)

with sync_playwright() as pw:
    b = pw.chromium.launch(args=["--autoplay-policy=no-user-gesture-required"])
    pg = b.new_page(viewport={"width": 960, "height": 540})
    errs = []
    pg.on("pageerror", lambda e: errs.append("PAGEERROR " + str(e)))
    pg.goto(f"http://127.0.0.1:{PORT}/index.html?record=1")
    pg.wait_for_function("() => window._ctrl && state === 'title'", timeout=15000)
    pg.evaluate("window._ctrl.start()"); pg.wait_for_timeout(400)
    drain(pg); pg.evaluate("window._ctrl.free()")
    pg.evaluate("window._ctrl.goto('aze')"); pg.wait_for_timeout(300)
    drain(pg); pg.evaluate("window._ctrl.free()")

    # 昼：蝶が とぶ
    pg.wait_for_timeout(400)
    n_day = pg.evaluate("bugs.length")
    active = pg.evaluate("bugsActive()")
    print("昼の 蝶:", n_day, " active:", active)
    if n_day < 1 or not active: fails.append("昼なのに 蝶が とばない")

    # あみが ないと ふっても とれない
    pg.evaluate("delete WORLD.items['ami']")
    kago0 = pg.evaluate("numOf('mushikago')")
    # 蝶を あみの 先へ 置いてから ふる（あみ なし）
    pg.evaluate("window._ctrl.put(540, 490)"); pg.wait_for_timeout(200)
    # 蝶を あみの 先へ 置いて、**同じ フレームで** ふる（あいだに 蝶が 動かないように）
    pg.evaluate("""() => { const np = netPoint(); if (bugs[0]) { bugs[0].gx = np.x; bugs[0].gy = player.y; } advance = true; }""")
    pg.wait_for_timeout(200)
    kago_noami = pg.evaluate("numOf('mushikago')")
    print("あみ なしで ふった 後の 虫かご:", kago_noami)
    if kago_noami != kago0: fails.append("あみが ないのに とれてしまった")

    # あみを 持たせて ふる → とれる
    pg.evaluate("WORLD.items['ami'] = 1")
    pool_before = pg.evaluate("leftToday('mushi:aze', 18)")
    pg.evaluate("""() => { const np = netPoint(); if (bugs[0]) { bugs[0].gx = np.x; bugs[0].gy = player.y; } advance = true; }""")
    pg.wait_for_timeout(250)
    kago1 = pg.evaluate("numOf('mushikago')")
    pool_after = pg.evaluate("leftToday('mushi:aze', 18)")
    zukan = pg.evaluate("Object.keys(WORLD.flags).filter(k=>k.startsWith('zukan:'))")
    print(f"あみで ふった 後: 虫かご {kago0}->{kago1}  のこり {pool_before}->{pool_after}  図鑑 {zukan}")
    if kago1 != kago0 + 1: fails.append("あみで ふっても 虫かごが ふえない")
    if pool_after != pool_before - 1: fails.append("とったのに その日の のこりが へらない")
    if not zukan: fails.append("とったのに 図鑑に 種類が のらない")

    # 夜は とばない
    pg.evaluate("window._ctrl.setYoru(true)"); pg.wait_for_timeout(300)
    n_night = pg.evaluate("bugs.length")
    print("夜の 蝶:", n_night)
    if n_night != 0: fails.append("夜なのに 蝶が とんでいる")

    print("errors:", errs[:3])
    if errs: fails.append("エラー " + str(errs[:2]))
    b.close()
srv.shutdown()
if fails:
    print("=== NG ===")
    for f in fails: print("  -", f)
    sys.exit(1)
print("=== すべて OK ===")
