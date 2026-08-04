# -*- coding: utf-8 -*-
# 日づけの 範囲キー（D0）：talks に "3-7" の ような 範囲で 会話を 書ける。
#   ・日4は "3-7" の 会話が 出る（あぜみち）
#   ・日8は まだ 中身が ない ので しずか
#   ・日2は これまでどおり（範囲を 足しても 壊れない）
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
        pg.keyboard.press("Space"); pg.wait_for_timeout(100)

def npc_lines_on(pg, day, place):
    # その日・その画面の NPC が 話せる 行数と 1行目を かえす
    return pg.evaluate(f"""() => {{
      newDay({day}); resetDay();
      const sc = SC['{place}']; const n = (sc.npc||[])[0];
      const L = linesOf(n, '{place}');
      return {{ n: L ? L.length : 0, first: L && L[0] ? L[0][1] : null }};
    }}""")

with sync_playwright() as pw:
    b = pw.chromium.launch(args=["--autoplay-policy=no-user-gesture-required"])
    pg = b.new_page(viewport={"width": 960, "height": 540})
    errs = []
    pg.on("pageerror", lambda e: errs.append("PAGEERROR " + str(e)))
    pg.goto(f"http://127.0.0.1:{PORT}/index.html?record=1")
    pg.wait_for_function("() => window._ctrl && state === 'title'", timeout=15000)
    pg.evaluate("window._ctrl.start()"); pg.wait_for_timeout(400)
    drain(pg); pg.evaluate("window._ctrl.free()")

    d4  = npc_lines_on(pg, 4, "aze")    # "3-7"
    d5  = npc_lines_on(pg, 5, "aze")    # おなじ 範囲
    d10 = npc_lines_on(pg, 10, "aze")   # "8-14"
    d15 = npc_lines_on(pg, 15, "aze")   # まだ ない
    d2  = npc_lines_on(pg, 2, "aze")
    print("あぜ 日4:", d4)
    print("あぜ 日10:", d10)
    print("あぜ 日15:", d15)
    if d4["n"] < 1 or "むし" not in (d4["first"] or ""):
        fails.append("日4に 範囲(3-7)の 会話が 出ない: " + str(d4))
    if d5["first"] != d4["first"]:
        fails.append("日5が 日4と ちがう（範囲なのに）")
    if d10["n"] < 1 or "とんぼ" not in (d10["first"] or ""):
        fails.append("日10に 範囲(8-14)の 会話が 出ない: " + str(d10))
    if d10["first"] == d4["first"]:
        fails.append("日10が 日4と 同じ（範囲が 切りかわっていない）")
    if d15["n"] != 0:
        fails.append("日15は まだ しずかな はず: " + str(d15))
    if d2["n"] < 1:
        fails.append("日2の 会話が 壊れた: " + str(d2))

    # 6画面 すべてに 日4・日10の 会話が 入ったか
    for scr in ["zashiki", "doma", "rouka", "iemae", "mori"]:
        r4 = npc_lines_on(pg, 4, scr)
        r10 = npc_lines_on(pg, 10, scr)
        print(f"  {scr} 日4:{r4['n']} 日10:{r10['n']}")
        if r4["n"] < 1: fails.append(f"{scr}: 日4の 会話が ない")
        if r10["n"] < 1: fails.append(f"{scr}: 日10の 会話が ない")

    print("errors:", errs[:3])
    if errs: fails.append("エラー " + str(errs[:2]))
    b.close()
srv.shutdown()
if fails:
    print("=== NG ==="); [print("  -", f) for f in fails]; sys.exit(1)
print("=== すべて OK ===")
