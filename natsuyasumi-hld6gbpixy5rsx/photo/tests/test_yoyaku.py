# -*- coding: utf-8 -*-
# うごかしかた:  python tests/test_yoyaku.py
#   pip install playwright && playwright install chromium  が いる。
#   絵は tests/_out/ に出る（git には 入れない）。
# きょう 行った ところが、その日の 晩ごはん や つぎの日の 朝ごはん で 話に なるか
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

NO_DUSK = ("window._ctrl.setSteps(0);"
           "window._ctrl.setMukae(true); window._ctrl.setYoru(true);")


def wait_scene_end(pg, limit=900):
    """場面が おわるまで 待って、出た セリフを かえす"""
    says = []
    for _ in range(limit):
        pg.wait_for_timeout(150)
        sc = pg.evaluate("window._ctrl.scene()")
        if sc is None:
            if says: break
            continue
        if sc["say"] and (not says or says[-1] != sc["say"][1]): says.append(sc["say"][1])
    return says


def visit(pg, place):
    """その画面へ 行く。ひきがねの 場面が 出たら おわるまで 待つ"""
    pg.evaluate(NO_DUSK)
    pg.evaluate(f"window._ctrl.goto('{place}')"); pg.wait_for_timeout(700)
    if pg.evaluate("window._ctrl.scene()"): wait_scene_end(pg)


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

    # --- 1日目：もり と あぜみち へ 行く
    visit(pg, "mori")
    visit(pg, "aze")
    q = pg.evaluate("window._ctrl.queue()")
    print("  行ったあとの よやく:", [f"{x['at']}(八月{x['day']}日)" for x in q])
    if not any(x["at"] == "dinner" for x in q):
        fails.append("もりへ 行っても 晩ごはんの よやくが つかない")
    if not any(x["at"] == "breakfast" for x in q):
        fails.append("あぜみちへ 行っても 朝ごはんの よやくが つかない")

    # --- その日の 晩ごはん。もりの 話が 出るか
    pg.evaluate("window._ctrl.setMukae(true); window._ctrl.setYoru(false);")
    pg.evaluate("window._ctrl.goto('zashiki','mae')"); pg.wait_for_timeout(400)
    pg.evaluate("window._ctrl.free()")
    pg.evaluate("window._ctrl.setSteps(24)")
    for _ in range(220):
        pg.wait_for_timeout(200)
        if pg.evaluate("window._ctrl.dbg()")["scene"]: break
    dinner = wait_scene_end(pg)
    hit = [s for s in dinner if "もりの ほうへ" in s]
    print(f"  晩ごはんの セリフ{len(dinner)}こ  もりの話={'あり' if hit else 'なし'}")
    if not hit: fails.append("きょう もりへ 行ったのに 晩ごはんで 話に ならない")
    pg.screenshot(path=os.path.join(OUT, "q_dinner.png"))

    left = pg.evaluate("window._ctrl.queue()")
    print("  晩ごはんの あと のこる よやく:", [f"{x['at']}(八月{x['day']}日)" for x in left])
    if any(x["at"] == "dinner" for x in left):
        fails.append("出したはずの 晩ごはんの よやくが のこっている")
    if not any(x["at"] == "breakfast" for x in left):
        fails.append("あしたの 朝ごはんの よやくまで 消えている")

    # --- ねて つぎの日の 朝ごはん。あぜみちの 話が 出るか
    pg.evaluate("window._ctrl.sleep()")
    morning = wait_scene_end(pg, 1200)
    hit2 = [s for s in morning if "あぜみちに いた" in s]
    print(f"  2日目の朝の セリフ{len(morning)}こ  あぜみちの話={'あり' if hit2 else 'なし'}")
    if not hit2: fails.append("きのう あぜみちへ 行ったのに 朝ごはんで 話に ならない")
    if pg.evaluate("window._ctrl.queue()"):
        fails.append("出したはずの よやくが のこっている")
    pg.screenshot(path=os.path.join(OUT, "q_breakfast.png"))

    # --- 行かなければ 話に ならない（2日目は どこへも 行かずに 晩ごはん）
    pg.evaluate("window._ctrl.free()")
    pg.evaluate("window._ctrl.setMukae(true); window._ctrl.setYoru(false);")
    pg.evaluate("window._ctrl.goto('zashiki','mae')"); pg.wait_for_timeout(400)
    pg.evaluate("window._ctrl.free()")
    pg.evaluate("window._ctrl.setSteps(24)")
    for _ in range(220):
        pg.wait_for_timeout(200)
        if pg.evaluate("window._ctrl.dbg()")["scene"]: break
    dinner2 = wait_scene_end(pg)
    hit3 = [s for s in dinner2 if "もりの ほうへ" in s]
    print(f"  行かなかった日の 晩ごはん: もりの話={'あり' if hit3 else 'なし'}")
    if hit3: fails.append("行っていないのに もりの 話に なる")

    print("\nerrors:", errs[:5])
    if errs: fails.append("エラー " + str(errs[:3]))
    b.close()
srv.shutdown()
print("\n=== 失敗 ===" if fails else "\n=== すべて OK ===")
for f in fails: print(" -", f)

sys.exit(1 if fails else 0)
