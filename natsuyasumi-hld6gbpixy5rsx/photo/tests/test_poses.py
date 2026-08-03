# -*- coding: utf-8 -*-
# うごかしかた:  python tests/test_poses.py
#   pip install playwright && playwright install chromium  が いる。
#   絵は tests/_out/ に出る（git には 入れない）。
# キャラの すがた（pose）が ちゃんと切りかわるか／急な転送になっていないか
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
        print("読み込みエラー:", errs); b.close(); srv.shutdown(); raise SystemExit(1)

    pg.evaluate("window._ctrl.start()")
    log = []          # (t, step, poses, veil, cur)
    taiso_end_i = None
    walked = {}       # 場面ごとに キャラが動いたか
    prev_pos = {}
    for i in range(700):
        pg.wait_for_timeout(150)
        s = pg.evaluate("window._ctrl.scene()")
        p = pg.evaluate("window._ctrl.poses()")
        d = pg.evaluate("window._ctrl.dbg()")
        if s is None:
            log.append((i*0.15, "FREE", p, 0, d["cur"]))
            break
        log.append((i*0.15, s["k"], p, s["veil"], d["cur"]))
        for c in p["cast"]:
            key = (d["cur"], c["k"])
            if key in prev_pos and prev_pos[key] != (c["x"], c["y"]):
                walked[key] = True
            prev_pos[key] = (c["x"], c["y"])
    else:
        fails.append("自由行動まで たどりつかない")

    # 1) たいそうが おわったら 誰も taiso のままで いないこと
    seen_taiso = [r for r in log if any(c["pose"] == "taiso" for c in r[2]["cast"]) or r[2]["me"] == "taiso"]
    after = [r for r in log if r[1] not in ("taiso",) and log.index(r) > (log.index(seen_taiso[-1]) if seen_taiso else 0)]
    stuck = [r for r in after if any(c["pose"] == "taiso" for c in r[2]["cast"]) or r[2]["me"] == "taiso"]
    print(f"  たいそう中のフレーム: {len(seen_taiso)}  そのあと taiso のまま: {len(stuck)}")
    if not seen_taiso: fails.append("たいそうが 一度も 起きていない")
    if stuck: fails.append(f"たいそうが おわっても すがたが もどらない（{len(stuck)}フレーム）")

    # 2) 場面の切りかわりで まっくらの幕が かかること
    veils = [r[3] for r in log if r[1] == "to"]
    print(f"  切りかわりの幕: 最大 {max(veils) if veils else 0}  ({len(veils)}フレーム)")
    if not veils or max(veils) < 0.9: fails.append("場面の切りかわりで まっくらにならない")

    # 3) 'move' の場面で キャラが ほんとうに 歩いたか
    moves = [r for r in log if r[1] == "move"]
    print(f"  move の場面: {len(moves)}フレーム  歩いたキャラ: {sorted(set(k[1] for k in walked))}")
    if len(moves) < 6: fails.append("move の場面が ほとんどない（歩かずに 転送している）")
    if len(walked) < 3: fails.append("歩いたキャラが すくない: " + str(walked))

    # 4) 自由行動になった しゅんかん 会話が はじまっていないこと
    d = pg.evaluate("window._ctrl.dbg()")
    p = pg.evaluate("window._ctrl.poses()")
    print(f"  自由行動の しゅんかん: {d}  talkLock={p['lock']}")
    if d["talking"]: fails.append("自由行動に なった とたん 会話が はじまっている")
    pg.screenshot(path=os.path.join(OUT, "p_free.png"))

    # 5) しばらく待っても はじまらない（となりに 居るだけでは だめ）
    pg.wait_for_timeout(2500)
    d2 = pg.evaluate("window._ctrl.dbg()")
    print(f"  2.5秒後: talking={d2['talking']}")
    if d2["talking"]: fails.append("その場に いるだけで 会話が はじまってしまう")

    # 6) いちど はなれて 近づき、キーを 押せば ちゃんと はじまる（P1）
    pg.evaluate("window._ctrl.put(180,470)"); pg.wait_for_timeout(600)
    pg.evaluate("window._ctrl.put(500,505)"); pg.wait_for_timeout(400)
    pg.evaluate("window._ctrl.act()"); pg.wait_for_timeout(300)
    d3 = pg.evaluate("window._ctrl.dbg()")
    t3 = pg.evaluate("window._ctrl.talk()")
    print(f"  はなれて 戻ったら: talking={d3['talking']} {t3 and t3['line']}")
    if not d3["talking"]: fails.append("はなれて 近づいても 会話が はじまらない")

    # ながれの ながさ
    print(f"\n  朝の ながさ: {log[-1][0]:.1f} 秒")
    ks = []
    for r in log:
        if not ks or ks[-1] != r[1]: ks.append(r[1])
    print("  ながれ:", " → ".join(ks))

    print("\nerrors:", errs[:5])
    if errs: fails.append("エラー " + str(errs[:3]))
    b.close()
srv.shutdown()
print("\n=== 失敗 ===" if fails else "\n=== すべて OK ===")
for f in fails: print(" -", f)

sys.exit(1 if fails else 0)
