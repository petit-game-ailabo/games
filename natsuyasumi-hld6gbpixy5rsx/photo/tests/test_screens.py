# -*- coding: utf-8 -*-
# うごかしかた:  python tests/test_screens.py
#   pip install playwright && playwright install chromium  が いる。
#   絵は tests/_out/ に出る（git には 入れない）。
# 6画面版の検証：全画面つながるか／道の外に出ないか／会話が進み、尽きたら二度と開かないか
import sys
import http.server, socketserver, threading, functools, os, random
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

SCREENS = ["zashiki", "doma", "rouka", "iemae", "aze", "mori"]
NPC_AT = {  # 画面ごとの NPC のいる場所（近づく先）
    "zashiki": (300, 470), "doma": (560, 496), "rouka": (790, 330),
    "iemae": (250, 480), "aze": (396, 424), "mori": (372, 402),
}
fails = []

# この検査は 走りまわるので、ほうっておくと steps が DAY_STEPS を こえて
# 日ぐれ → むかえ → 晩ごはん → 縁側 の 長い場面が はじまり、
# そのあとの 移動も 会話も ぜんぶ できなくなる。
# ここで 見たいのは 画面と 会話なので、日ぐれの ぶんは とめておく
# （日ぐれは test_mukae.py / test_yoru.py が 見ている）
NO_DUSK = ("window._ctrl.setSteps(0);"
           "window._ctrl.setMukae(true); window._ctrl.setYoru(true);")

with sync_playwright() as pw:
    b = pw.chromium.launch()
    pg = b.new_page(viewport={"width": 960, "height": 540})
    errs = []
    pg.on("pageerror", lambda e: errs.append(str(e)))
    pg.on("console", lambda m: errs.append("CONSOLE " + m.text) if m.type == "error" else None)
    pg.goto(f"http://127.0.0.1:{PORT}/index.html?record=1")
    pg.wait_for_timeout(3500)
    print("loaded state:", pg.evaluate("state"))
    pg.evaluate("window._ctrl.start()"); pg.wait_for_timeout(400); pg.evaluate("window._ctrl.free()"); pg.wait_for_timeout(1200)
    pg.evaluate(NO_DUSK)


    # 0) どの NPC にも「じゅうぶん近づける床」があるか
    print("\n-- 近づけるか --")
    rep = pg.evaluate("""() => {
      const out = [];
      for (const k in SC) {
        for (const n of (SC[k].npc||[])) for (const w of n.who) {
          let best = 1e9, bx=0, by=0;
          const save = cur; cur = k;
          for (let y=SC[k].yTop; y<=540; y+=4) for (let x=0; x<960; x+=6) {
            if (!walkable(x,y)) continue;
            const d = groundDist(x,y,w[1],w[2]);
            if (d < best) { best=d; bx=x; by=y; }
          }
          cur = save;
          out.push({sc:k, who:w[0], best:+best.toFixed(2), at:[bx,by]});
        }
      }
      return out;
    }""")
    for r in rep:
        ok = r["best"] < 1.6
        print(f"  {r['sc']:8s} {r['who']:8s} いちばん近づける距離={r['best']} {r['at']} {'OK' if ok else 'NG'}")
        if not ok: fails.append(f"{r['sc']}/{r['who']}: 近づける床がない（最短 {r['best']}）")

    # 1) 各画面：出発点が床の上か／スクリーンショット
    print("\n-- 各画面 --")
    for s in SCREENS:
        pg.evaluate(f"window._ctrl.goto('{s}')"); pg.wait_for_timeout(500)
        d = pg.evaluate("window._ctrl.dbg()")
        print(f"  {s:8s} start on-floor={d['on']} h={d['h']}")
        if not d["on"]: fails.append(f"{s}: 出発点が床の外")
        pg.screenshot(path=os.path.join(OUT, f"n_{s}.png"))

    # 2) 各画面でランダムに歩き回って 床の外に出ないか
    print("\n-- 歩きまわり --")
    random.seed(11)
    K = ["ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight"]
    for s in SCREENS:
        pg.evaluate(NO_DUSK)
        pg.evaluate(f"window._ctrl.goto('{s}')"); pg.wait_for_timeout(300)
        off = 0; visited = set()
        for _ in range(26):
            ks = random.sample(K, random.choice([1, 1, 2]))
            for k in ks: pg.keyboard.down(k)
            pg.wait_for_timeout(random.randint(90, 260))
            for k in ks: pg.keyboard.up(k)
            d = pg.evaluate("window._ctrl.dbg()")
            visited.add(d["cur"])
            if not d["on"]: off += 1
        print(f"  {s:8s} 床の外 {off}/26  通った画面 {sorted(visited)}")
        if off: fails.append(f"{s}: 床の外に出た {off} 回")

    # 3) 出入り口：各画面から全方向に歩いて どこへ行けるか
    print("\n-- つながり --")
    reach = {}
    for s in SCREENS:
        got = set()
        for key in ["ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight"]:
            pg.evaluate(NO_DUSK)
            pg.evaluate(f"window._ctrl.goto('{s}')"); pg.wait_for_timeout(300)
            pg.keyboard.down("ShiftLeft"); pg.keyboard.down(key)
            for _ in range(22):
                pg.wait_for_timeout(200)
                c = pg.evaluate("window._ctrl.dbg()")["cur"]
                if c != s: got.add(c)
            pg.keyboard.up(key); pg.keyboard.up("ShiftLeft")
        reach[s] = sorted(got)
        print(f"  {s:8s} -> {reach[s]}")
        if not got: fails.append(f"{s}: どこへも行けない")

    # 4) 会話：近づく → 進む → 尽きる → 二度と開かない
    print("\n-- はなし --")
    for s in SCREENS:
        pg.evaluate(NO_DUSK)
        pg.evaluate(f"window._ctrl.goto('{s}')"); pg.wait_for_timeout(300)
        pg.evaluate("window._ctrl.free()")   # 場面あけの talkLock を はずす
        x, y = NPC_AT[s]
        pg.evaluate(f"window._ctrl.put({x+30},{y+18})"); pg.wait_for_timeout(500)
        t0 = pg.evaluate("window._ctrl.talk()")
        if not t0:
            fails.append(f"{s}: 近づいてもはなしが始まらない"); print(f"  {s:8s} NG 始まらない"); continue
        total = t0["n"]
        # ぜんぶ終わるまで待つ（1行あたり最大 4.5 秒 + 余裕）
        seen = []
        for _ in range(int(total * 26) + 40):
            pg.wait_for_timeout(200)
            t = pg.evaluate("window._ctrl.talk()")
            if t is None: break
            if not seen or seen[-1] != t["idx"]: seen.append(t["idx"])
        st = pg.evaluate("window._ctrl.npcState()")
        ok_done = all(n["done"] for n in st)
        # 尽きたあと もう一度近づく
        pg.evaluate(f"window._ctrl.put(200,520)"); pg.wait_for_timeout(400)
        pg.evaluate(f"window._ctrl.put({x+30},{y+18})"); pg.wait_for_timeout(900)
        again = pg.evaluate("window._ctrl.talk()")
        print(f"  {s:8s} {total}行 通過{len(seen)} done={ok_done} 再訪で開く={again is not None}")
        if not ok_done: fails.append(f"{s}: 会話が終わらない")
        if again is not None: fails.append(f"{s}: 尽きたのに また開く")

    # 5) 会話の途中で離れる → 止まる → 戻ると続く
    print("\n-- 離れて戻る --")
    pg.reload(); pg.wait_for_timeout(3200)
    pg.evaluate("window._ctrl.start()"); pg.wait_for_timeout(400); pg.evaluate("window._ctrl.free()"); pg.wait_for_timeout(800)
    pg.evaluate("window._ctrl.goto('aze')"); pg.wait_for_timeout(300)
    pg.evaluate("window._ctrl.put(426,442)"); pg.wait_for_timeout(3000)
    a = pg.evaluate("window._ctrl.talk()")
    pg.evaluate("window._ctrl.put(560,530)"); pg.wait_for_timeout(2500)
    away = pg.evaluate("window._ctrl.talk()")
    pg.evaluate("window._ctrl.put(426,442)"); pg.wait_for_timeout(600)
    back = pg.evaluate("window._ctrl.talk()")
    print(f"  そば idx={a and a['idx']} / 離れた={away} / 戻り idx={back and back['idx']}")
    if away is not None: fails.append("離れてもはなしが続いている")
    if back is None or back["idx"] != a["idx"]: fails.append("戻ったのに続きから始まらない")
    pg.screenshot(path=os.path.join(OUT, "n_talk.png"))

    fps = pg.evaluate("""() => new Promise(r=>{let n=0,t0=performance.now();
        (function f(){n++; if(performance.now()-t0<2000) requestAnimationFrame(f);
         else r(Math.round(n/((performance.now()-t0)/1000)));})();})""")
    print("\nfps:", fps)
    print("errors:", errs[:6])
    if errs: fails.append("コンソール/ページエラー: " + str(errs[:3]))
    b.close()
srv.shutdown()
print("\n=== 失敗 ===" if fails else "\n=== すべて OK ===")
for f in fails: print(" -", f)

sys.exit(1 if fails else 0)
