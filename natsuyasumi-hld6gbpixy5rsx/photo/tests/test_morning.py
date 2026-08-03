# -*- coding: utf-8 -*-
# うごかしかた:  python tests/test_morning.py
#   pip install playwright && playwright install chromium  が いる。
#   絵は tests/_out/ に出る（git には 入れない）。
# 朝のながれの検証：ざしきで起きる → 庭でラジオたいそう → いろり端でごはん → 自由行動
# そのあと ふとんで じっとしていると つぎの日の朝になるか
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

with sync_playwright() as pw:
    b = pw.chromium.launch(args=["--autoplay-policy=no-user-gesture-required"])
    pg = b.new_page(viewport={"width": 960, "height": 540})
    errs = []
    pg.on("pageerror", lambda e: errs.append("PAGEERROR " + str(e)))
    pg.on("console", lambda m: errs.append("CONSOLE " + m.text) if m.type == "error" else None)
    pg.goto(f"http://127.0.0.1:{PORT}/index.html?record=1")
    pg.wait_for_timeout(3600)
    if errs:
        print("読み込みでエラー:", errs); b.close(); srv.shutdown(); raise SystemExit(1)
    print("state:", pg.evaluate("state"))

    pg.evaluate("window._ctrl.start()")
    shots, seen_sc, seen_say, taiso_seen = [], [], [], False
    prev = None
    for i in range(700):                       # 最大 68 秒ぶん
        pg.wait_for_timeout(200)
        s = pg.evaluate("window._ctrl.scene()")
        d = pg.evaluate("window._ctrl.dbg()")
        if d["cur"] not in seen_sc: seen_sc.append(d["cur"])
        if s is None:
            print(f"  自由行動になった（{i*0.2:.1f}秒）")
            break
        if s["k"] == "taiso" and not taiso_seen:
            taiso_seen = True
            pg.screenshot(path=os.path.join(OUT, "m_taiso.png"))
        if s["say"] and s["say"] != prev:
            prev = s["say"]; seen_say.append(s["say"][0] + "：" + s["say"][1])
            if len(seen_say) in (1, 6, 10):
                pg.screenshot(path=os.path.join(OUT, f"m_say{len(seen_say)}.png"))
    else:
        fails.append("朝のながれが 自由行動まで たどりつかない")

    d = pg.evaluate("window._ctrl.dbg()")
    print("  通った画面:", seen_sc)
    print("  ラジオたいそう:", taiso_seen)
    print("  セリフ", len(seen_say), "個")
    for s in seen_say: print("     ", s)
    print("  おわりの状態:", d)
    for want in ["zashiki", "iemae", "doma"]:
        if want not in seen_sc: fails.append(f"朝のながれに {want} が出てこない")
    if not taiso_seen: fails.append("ラジオたいそうが 出てこない")
    if d["state"] != "play": fails.append("自由行動になっていない")
    if d["day"] != 1: fails.append("1日目になっていない")

    # 自由行動中に あるける／会話できる
    pg.keyboard.down("ArrowLeft"); pg.wait_for_timeout(900); pg.keyboard.up("ArrowLeft")
    d2 = pg.evaluate("window._ctrl.dbg()")
    print("\n  自由行動であるけた:", d2["x"] != d["x"] or d2["cur"] != d["cur"], d2)
    if d2["x"] == d["x"] and d2["cur"] == d["cur"]: fails.append("自由行動であるけない")

    # ふとんで じっとしていると つぎの日
    pg.evaluate("window._ctrl.goto('zashiki','mae')"); pg.wait_for_timeout(400)
    pg.evaluate("window._ctrl.put(186,452)"); pg.wait_for_timeout(2600)
    d3 = pg.evaluate("window._ctrl.dbg()")
    print("  ふとんに じっとして:", d3)
    if d3["day"] != 2 or not d3["scene"]:
        fails.append("ふとんで 1日が おわらない: " + str(d3))
    pg.screenshot(path=os.path.join(OUT, "m_night.png"))

    # 2日目の朝も 自由行動まで行くか
    for i in range(700):
        pg.wait_for_timeout(200)
        if pg.evaluate("window._ctrl.scene()") is None: break
    d4 = pg.evaluate("window._ctrl.dbg()")
    print("  2日目:", d4)
    if d4["state"] != "play" or d4["day"] != 2: fails.append("2日目が 自由行動にならない")

    # 2日目の会話は 1日目とちがう
    pg.evaluate("window._ctrl.goto('aze','temae')"); pg.wait_for_timeout(400)
    pg.evaluate("window._ctrl.put(400,436)"); pg.wait_for_timeout(500)
    pg.evaluate("window._ctrl.act()"); pg.wait_for_timeout(200)   # P1：キーで 話しかける
    t = pg.evaluate("window._ctrl.talk()")
    print("  あぜみち 2日目のはなし:", t)
    if not t: fails.append("2日目に あぜみちで はなさない")
    elif "よるに ここ" not in t["line"][1]: fails.append("2日目なのに 1日目のセリフ: " + str(t))

    # 3日目は しずか（会話なし）
    pg.evaluate("window._ctrl.sleep()"); pg.wait_for_timeout(400)
    for i in range(700):
        pg.wait_for_timeout(200)
        if pg.evaluate("window._ctrl.scene()") is None: break
    pg.evaluate("window._ctrl.goto('aze','temae')"); pg.wait_for_timeout(300)
    pg.evaluate("window._ctrl.put(400,436)"); pg.wait_for_timeout(500)
    pg.evaluate("window._ctrl.act()"); pg.wait_for_timeout(200)   # キーを 押しても 3日目は しずか
    t3 = pg.evaluate("window._ctrl.talk()")
    d5 = pg.evaluate("window._ctrl.dbg()")
    print("  3日目:", d5, "はなし:", t3)
    if t3 is not None: fails.append("3日目は しずかなはずが はなしている")

    fps = pg.evaluate("""() => new Promise(r=>{let n=0,t0=performance.now();
        (function f(){n++; if(performance.now()-t0<2000) requestAnimationFrame(f);
         else r(Math.round(n/((performance.now()-t0)/1000)));})();})""")
    print("\nfps:", fps)
    print("errors:", errs[:6])
    if errs: fails.append("エラー " + str(errs[:3]))
    b.close()
srv.shutdown()
print("\n=== 失敗 ===" if fails else "\n=== すべて OK ===")
for f in fails: print(" -", f)

sys.exit(1 if fails else 0)
