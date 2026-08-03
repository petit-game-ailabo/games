# -*- coding: utf-8 -*-
# 自由研究の 観察日記（D9）：あぜみちの いねを 調べると
#   「かんさつ する？」→ する で `kansatsu:いね` が 立ち、観察日記（VIEW.kansatsu）が 開く。
#   「やめておく」を えらんでも しるしは 立たず、**また 調べられる**（once は 観察するまで）。
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
        st = pg.evaluate("state")
        if st not in ("scene", "view"): return
        pg.keyboard.press("Space"); pg.wait_for_timeout(100)

def pick_when_ready(pg, want):
    for _ in range(50):
        pg.wait_for_timeout(150)
        sel = pg.evaluate("window._ctrl.sel()")
        if sel:
            opts = sel["opts"]
            idx = next((i for i, o in enumerate(opts) if want in o), 0)
            pg.evaluate(f"window._ctrl.pick({idx})")
            return opts
        if pg.evaluate("window._ctrl.scene()") is None: return None
    return None

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

    # いね(648,392) の そばへ
    pg.evaluate("window._ctrl.put(648, 415)"); pg.wait_for_timeout(300)
    near = pg.evaluate("window._ctrl.near()")
    print("いねの そば:", near)
    if near.get("spot") != "ine": fails.append("いねに 近づけていない: " + str(near))

    # 1回目：やめておく → しるしは 立たない
    pg.evaluate("window._ctrl.act()"); pg.wait_for_timeout(250)
    opts = pick_when_ready(pg, "やめておく")
    print("えらぶ:", opts)
    drain(pg)
    if pg.evaluate("() => hasFlag('kansatsu:いね')"):
        fails.append("やめておいたのに かんさつ したことに なっている")

    # 2回目：くりかえし 調べられる → する → しるし＋観察日記が 開く
    pg.evaluate("window._ctrl.free(); window._ctrl.put(648, 415)"); pg.wait_for_timeout(250)
    pg.evaluate("window._ctrl.act()"); pg.wait_for_timeout(250)
    opts2 = pick_when_ready(pg, "する")
    print("もう一度 えらべた:", opts2)
    if not opts2: fails.append("やめた あと もう一度 調べられない（くりかえせない）")
    # 観察日記が 開くまで
    opened = False
    for _ in range(30):
        pg.wait_for_timeout(120)
        v = pg.evaluate("window._ctrl.view()")
        if v and v["name"] == "kansatsu": opened = True; break
        if pg.evaluate("window._ctrl.scene()"): pg.keyboard.press("Space")
    made = pg.evaluate("() => hasFlag('kansatsu:いね')")
    print("かんさつ した:", made, " 観察日記が 開いた:", opened)
    if not made: fails.append("する を えらんでも しるしが 立たない")
    if not opened: fails.append("観察日記（VIEW.kansatsu）が 開かない")
    pg.wait_for_timeout(300); pg.keyboard.press("Escape"); pg.wait_for_timeout(300)

    print("errors:", errs[:3])
    if errs: fails.append("エラー " + str(errs[:2]))
    b.close()
srv.shutdown()
if fails:
    print("=== NG ==="); [print("  -", f) for f in fails]; sys.exit(1)
print("=== すべて OK ===")
