# -*- coding: utf-8 -*-
# ごはんの絵が 毎日ちがう（D13）：
#   ・`gohanFor(at, day)` が 日ごとに ちがう おかずを 返す
#   ・`{k:'gohan'}` の 場面ステップで ちゃぶ台の 中身（gohanShow）が 立ち、場面が おわると 消える
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
        pg.keyboard.press("Space"); pg.wait_for_timeout(90)

with sync_playwright() as pw:
    b = pw.chromium.launch(args=["--autoplay-policy=no-user-gesture-required"])
    pg = b.new_page(viewport={"width": 960, "height": 540})
    errs = []
    pg.on("pageerror", lambda e: errs.append("PAGEERROR " + str(e)))
    pg.goto(f"http://127.0.0.1:{PORT}/index.html?record=1")
    pg.wait_for_function("() => window._ctrl && state === 'title'", timeout=15000)
    pg.evaluate("window._ctrl.start()"); pg.wait_for_timeout(400)
    drain(pg); pg.evaluate("window._ctrl.free()")

    # 日ごとに ちがう
    b = [pg.evaluate(f"gohanFor('breakfast',{d}).name") for d in (1, 2, 3)]
    d = [pg.evaluate(f"gohanFor('dinner',{d}).name") for d in (1, 2, 3)]
    print("朝ごはん:", b)
    print("晩ごはん:", d)
    if len(set(b)) < 2: fails.append("朝ごはんが 毎日 同じ")
    if len(set(d)) < 2: fails.append("晩ごはんが 毎日 同じ")

    # 場面ステップで ごはんの絵が 立つ／消える
    pg.evaluate("window._ctrl.goto('doma')"); pg.wait_for_timeout(300)
    drain(pg); pg.evaluate("window._ctrl.free()")
    pg.evaluate("runScene([{k:'gohan',at:'dinner'},{k:'wait',s:5}]); state='scene';")
    pg.wait_for_timeout(300)
    show = pg.evaluate("gohanShow && gohanShow.name")
    want = pg.evaluate("gohanFor('dinner', WORLD.day).name")
    print("場面で 出た ごはん:", show, " / きょうの 晩ごはん:", want)
    if show != want: fails.append("gohan ステップで きょうの ごはんが 出ない")
    # 場面が おわると 消える
    pg.evaluate("window._ctrl.free()"); pg.wait_for_timeout(150)
    if pg.evaluate("gohanShow") is not None: fails.append("場面が おわっても ごはんの絵が のこる")

    print("errors:", errs[:3])
    if errs: fails.append("エラー " + str(errs[:2]))
    b_ = pg  # keep ref
    pg.context.browser.close()
srv.shutdown()
if fails:
    print("=== NG ==="); [print("  -", f) for f in fails]; sys.exit(1)
print("=== すべて OK ===")
