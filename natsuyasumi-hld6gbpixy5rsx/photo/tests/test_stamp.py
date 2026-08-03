# -*- coding: utf-8 -*-
# ラジオ体操の スタンプカード（D4）：
#   ・朝 おきると その日の 判こが 押される（stamp:{day}）
#   ・つぎの日も おされる
#   ・どまの 引き出しで 31枡の カードを 見られる（VIEW.stamp）
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
    for _ in range(120):
        if not pg.evaluate("window._ctrl.scene()"): return
        pg.keyboard.press("Space"); pg.wait_for_timeout(90)

with sync_playwright() as pw:
    b = pw.chromium.launch(args=["--autoplay-policy=no-user-gesture-required"])
    pg = b.new_page(viewport={"width": 960, "height": 540})
    errs = []
    pg.on("pageerror", lambda e: errs.append("PAGEERROR " + str(e)))
    pg.goto(f"http://127.0.0.1:{PORT}/index.html?record=1")
    pg.wait_for_function("() => window._ctrl && state === 'title'", timeout=15000)
    pg.evaluate("window._ctrl.start()"); pg.wait_for_timeout(500)

    # 1日目：おきると 判こ（wake で 押される）
    s1 = pg.evaluate("() => hasFlag('stamp:1')")
    print("1日目 判こ stamp:1 =", s1)
    if not s1: fails.append("1日目に 判こが 押されない")
    drain(pg); pg.evaluate("window._ctrl.free()")

    # 2日目：ねて 起きると 2日目の 判こ
    pg.evaluate("window._ctrl.sleep()"); pg.wait_for_timeout(400)
    drain(pg); pg.evaluate("window._ctrl.free()")
    s2 = pg.evaluate("() => hasFlag('stamp:2')")
    day = pg.evaluate("() => WORLD.day")
    print("2日目(day=%s) 判こ stamp:2 =" % day, s2)
    if not s2: fails.append("2日目に 判こが 押されない")

    # どまの 引き出しで カードを 見る（VIEW.stamp）
    pg.evaluate("window._ctrl.goto('doma')"); pg.wait_for_timeout(300)
    drain(pg); pg.evaluate("window._ctrl.free()")
    pg.evaluate("window._ctrl.put(215, 448)"); pg.wait_for_timeout(300)
    near = pg.evaluate("window._ctrl.near()")
    pg.evaluate("window._ctrl.act()"); pg.wait_for_timeout(200)   # キーで 調べる
    # 場面（引き出しの セリフ）を すすめると 最後に カードが 開く
    st = None
    for _ in range(30):
        st = pg.evaluate("state")
        if st == "view": break
        pg.keyboard.press("Space"); pg.wait_for_timeout(120)
    print("引き出し near=", near, " → state=", st)
    if st != "view": fails.append("引き出しで スタンプカードが 開かない")
    # とじる
    pg.wait_for_timeout(400); pg.keyboard.press("Escape"); pg.wait_for_timeout(300)
    print("とじたあと state=", pg.evaluate("state"))

    print("errors:", errs[:3])
    if errs: fails.append("エラー " + str(errs[:2]))
    b.close()
srv.shutdown()
if fails:
    print("=== NG ==="); [print("  -", f) for f in fails]; sys.exit(1)
print("=== すべて OK ===")
