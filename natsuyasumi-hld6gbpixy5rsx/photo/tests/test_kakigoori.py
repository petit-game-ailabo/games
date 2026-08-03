# -*- coding: utf-8 -*-
# かき氷（D9）：チルノは 氷の妖精。どまの すいかを 調べると
#   「ひやす／かきごおりを つくる／やめておく」の 3つから えらべ、
#   かきごおりを えらぶと その場で つくって `kakigoori` の しるしが 立つ。
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
        pg.keyboard.press("Space"); pg.wait_for_timeout(100)

with sync_playwright() as pw:
    b = pw.chromium.launch(args=["--autoplay-policy=no-user-gesture-required"])
    pg = b.new_page(viewport={"width": 960, "height": 540})
    errs = []
    pg.on("pageerror", lambda e: errs.append("PAGEERROR " + str(e)))
    pg.goto(f"http://127.0.0.1:{PORT}/index.html?record=1")
    pg.wait_for_function("() => window._ctrl && state === 'title'", timeout=15000)
    pg.evaluate("window._ctrl.start()"); pg.wait_for_timeout(400)
    drain(pg); pg.evaluate("window._ctrl.free()")

    # どまの すいかに 近づいて 調べる
    pg.evaluate("window._ctrl.goto('doma')"); pg.wait_for_timeout(300)
    drain(pg); pg.evaluate("window._ctrl.free()")
    # すいかは (880,430)。そばの 床へ
    pg.evaluate("window._ctrl.put(858, 458)"); pg.wait_for_timeout(300)
    near = pg.evaluate("window._ctrl.near()")
    print("すいかの そば:", near)
    if near.get("spot") != "suika": fails.append("すいかに 近づけていない: " + str(near))
    pg.evaluate("window._ctrl.act()"); pg.wait_for_timeout(300)   # キーで 調べる

    # えらぶ が 出るまで すすめる
    picked = False
    for _ in range(50):
        pg.wait_for_timeout(150)
        sel = pg.evaluate("window._ctrl.sel()")
        if sel:
            opts = sel["opts"]
            print("えらぶ:", opts)
            idx = next((i for i, o in enumerate(opts) if "かきごおり" in o), 1)
            pg.evaluate(f"window._ctrl.pick({idx})")
            picked = True; break
        if pg.evaluate("window._ctrl.scene()") is None: break
    if not picked: fails.append("かきごおりの えらびが 出ない")
    drain(pg)

    made = pg.evaluate("() => hasFlag('kakigoori')")
    print("かきごおり つくった:", made)
    if not made: fails.append("かきごおりを えらんでも しるしが 立たない")

    # ひやす（D7）の 予約は 立っていない（別の えらび だから）
    hiyashita = pg.evaluate("() => hasFlag('suika_hiyashita')")
    if hiyashita: fails.append("かきごおりを えらんだのに すいかを ひやした ことに なっている")

    print("errors:", errs[:3])
    if errs: fails.append("エラー " + str(errs[:2]))
    b.close()
srv.shutdown()
if fails:
    print("=== NG ==="); [print("  -", f) for f in fails]; sys.exit(1)
print("=== すべて OK ===")
