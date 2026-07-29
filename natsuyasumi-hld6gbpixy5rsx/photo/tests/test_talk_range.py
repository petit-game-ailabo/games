# -*- coding: utf-8 -*-
# うごかしかた:  python tests/test_talk_range.py
#   pip install playwright && playwright install chromium  が いる。
#   絵は tests/_out/ に出る（git には 入れない）。
# 会話の近さ判定：横だけでなく 奥ゆきでも ちゃんと離れられるか
# 注意：離れた先が画面のはしだと 別の画面へ移ってしまうので、毎回 画面を入り直して測る
import sys
import http.server, socketserver, threading, functools, os
from playwright.sync_api import sync_playwright

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PORT = 0
h = functools.partial(http.server.SimpleHTTPRequestHandler, directory=ROOT)
socketserver.TCPServer.allow_reuse_address = True
srv = socketserver.TCPServer(("127.0.0.1", PORT), h)
PORT = srv.server_address[1]
threading.Thread(target=srv.serve_forever, daemon=True).start()

NPC = {"aze": (396, 424), "mori": (372, 402), "iemae": (250, 480),
       "zashiki": (300, 470), "rouka": (790, 330), "doma": (470, 480)}
OFF = [("そば", 6, 10), ("奥へ70", 0, -70), ("手前へ70", 0, 70),
       ("横へ70", 70, 0), ("横へ190", 190, 0)]
fails = []

with sync_playwright() as pw:
    b = pw.chromium.launch()
    pg = b.new_page(viewport={"width": 960, "height": 540})
    errs = []
    pg.on("pageerror", lambda e: errs.append(str(e)))
    pg.goto(f"http://127.0.0.1:{PORT}/index.html?record=1")
    pg.wait_for_timeout(3500)
    pg.evaluate("window._ctrl.start()"); pg.wait_for_timeout(400); pg.evaluate("window._ctrl.free()"); pg.wait_for_timeout(900)
    R = pg.evaluate("window._ctrl.R")
    print("TALK_R =", R, "（単位は 背の高さ ぶん）\n")

    for s, (nx, ny) in NPC.items():
        print(f"  {s}")
        for tag, dx, dy in OFF:
            pg.evaluate(f"window._ctrl.goto('{s}')"); pg.wait_for_timeout(260)
            pg.evaluate(f"window._ctrl.put({nx+dx},{ny+dy})"); pg.wait_for_timeout(300)
            d = pg.evaluate("window._ctrl.dbg()")
            if d["cur"] != s:
                print(f"     {tag:9s} （画面のはしを越えて {d['cur']} へ。判定できず）")
                continue
            gd = pg.evaluate("window._ctrl.gdist()")
            mind = min(min(row) for row in gd) if gd else None
            talk = pg.evaluate("window._ctrl.talk()") is not None
            print(f"     {tag:9s} はなす={str(talk):5s} 地面のうえの距離={mind}")
            if tag == "そば" and not talk: fails.append(f"{s}: そばなのに はなさない")
            if tag.startswith("奥へ") and talk: fails.append(f"{s}: 奥へ離れても はなしている")
            if tag == "横へ190" and talk and mind > R:
                fails.append(f"{s}: 距離{mind} なのに はなしている")
        print()

    print("errors:", errs[:5])
    if errs: fails.append("エラー " + str(errs[:2]))
    b.close()
srv.shutdown()
print("=== 失敗 ===" if fails else "=== すべて OK ===")
for f in fails: print(" -", f)

sys.exit(1 if fails else 0)
