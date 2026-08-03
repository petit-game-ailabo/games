# -*- coding: utf-8 -*-
# 蛍（D8）：晩ごはんの あと 夜の あぜみちへ 行くと ほたるが とび、はじめては 情景シーン。
#   ・夜の あぜ … ほたるが いる／入場で 一度だけ 場面
#   ・昼は ほたるは いない
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
    for _ in range(60):
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

    # 昼の あぜ：ほたるは いない
    pg.evaluate("window._ctrl.setYoru(false); window._ctrl.goto('aze')"); pg.wait_for_timeout(300)
    drain(pg); pg.evaluate("window._ctrl.free()")
    day = pg.evaluate("hotaru.length")
    print("昼の あぜ ほたる:", day)
    if day != 0: fails.append("昼なのに ほたるが いる")

    # 夜に する → ちがう画面へ 出てから あぜへ 戻ると 入場で 情景シーン
    pg.evaluate("window._ctrl.setYoru(true)")
    pg.evaluate("window._ctrl.goto('iemae')"); pg.wait_for_timeout(300); drain(pg)
    pg.evaluate("window._ctrl.free(); window._ctrl.goto('aze')"); pg.wait_for_timeout(500)
    sc = pg.evaluate("window._ctrl.scene()")
    say = sc.get("say") if sc else None
    line = say[1] if say else ""
    print("夜あぜ 入場の 場面:", line)
    # 場面が 出て、見た しるしが 立てば ほたるの 情景（hotaru_deru 以外に 夜あぜ入場の 場面は ない）
    if not say: fails.append("夜の あぜで ほたるの 場面が 出ない")
    mita = pg.evaluate("() => hasFlag('mita_hotaru')")
    drain(pg)
    nh = pg.evaluate("hotaru.length")
    print("夜あぜ ほたるの 数:", nh, " 見たしるし:", mita)
    if nh < 1: fails.append("夜の あぜに ほたるが いない")
    if not mita: fails.append("ほたるを 見た しるしが 立たない")

    # 一度きり：もう一度 出入りしても 場面は 出ない
    pg.evaluate("window._ctrl.goto('iemae')"); pg.wait_for_timeout(300); drain(pg)
    pg.evaluate("window._ctrl.free(); window._ctrl.goto('aze')"); pg.wait_for_timeout(500)
    again = pg.evaluate("window._ctrl.scene()")
    print("二度目の 入場 場面:", again and again.get("say"))
    if again is not None: fails.append("ほたるの 場面が 二度 出る")
    drain(pg)

    print("errors:", errs[:3])
    if errs: fails.append("エラー " + str(errs[:2]))
    b.close()
srv.shutdown()
if fails:
    print("=== NG ==="); [print("  -", f) for f in fails]; sys.exit(1)
print("=== すべて OK ===")
