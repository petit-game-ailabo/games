# -*- coding: utf-8 -*-
# 絵日記で 1日を 終える（D5）：
#   ・ねる まえに 絵日記が 出て、その日の 記録（行った ところ など）が のる
#   ・**スペースを 押さなくても** すこし 見せてから ひとりでに とじて 翌日へ すすむ
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
    drain(pg); pg.evaluate("window._ctrl.free()")

    # きょうは あぜみちへ 行っておく（日記に のる はず）
    pg.evaluate("window._ctrl.goto('aze')"); pg.wait_for_timeout(300)
    drain(pg); pg.evaluate("window._ctrl.free()")

    # ねる。**スペースは 押さない**（自動で とじる ことの 確認）
    pg.evaluate("window._ctrl.sleep()")
    lines = None
    for _ in range(70):
        pg.wait_for_timeout(200)
        v = pg.evaluate("window._ctrl.view()")
        if v and v["name"] == "nikki":
            lines = pg.evaluate("view.d.lines")
            break
    print("絵日記の 中身:", lines)
    if not lines: fails.append("ねる まえに 絵日記が 出ない")
    elif not any("あぜみち" in x for x in lines):
        fails.append("行った ところ（あぜみち）が 日記に のらない")

    # スペースを 押さずに、絵日記が ひとりでに とじる（hold）
    closed = False
    for _ in range(50):                       # 最大 10 秒
        pg.wait_for_timeout(200)
        v = pg.evaluate("window._ctrl.view()")
        if not v or v["name"] != "nikki": closed = True; break
    print("スペース 押さずに 絵日記が とじた:", closed)
    if not closed: fails.append("絵日記が ひとりでに とじない")

    # そのあと 場面は とまらず 翌日（day=2）へ 入る（絵日記を 越えて すすんだ 証拠）
    got_day2 = False
    for _ in range(220):                      # 朝の ながれは 長い（taiso など 尺が ある）
        pg.wait_for_timeout(200)
        if pg.evaluate("window._ctrl.scene()"): pg.keyboard.press("Space")
        if pg.evaluate("window._ctrl.dbg()")["day"] == 2: got_day2 = True; break
    print("翌日(day=2)へ 入った:", got_day2)
    if not got_day2: fails.append("絵日記の あと 翌日に すすめない")

    print("errors:", errs[:3])
    if errs: fails.append("エラー " + str(errs[:2]))
    b.close()
srv.shutdown()
if fails:
    print("=== NG ==="); [print("  -", f) for f in fails]; sys.exit(1)
print("=== すべて OK ===")
