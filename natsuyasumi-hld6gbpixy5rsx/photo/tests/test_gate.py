# -*- coding: utf-8 -*-
# 塞がれた道は「見てから／道具で 自分で」（P6）。倒木の 例：
#   ・倒木に 近づくと わけを 言い、**見た しるし**（saw_taiboku）が 立つ
#   ・道具（なた）が ないと きれない
#   ・見た つぎの 朝ごはんで 魔理沙が なたを くれる（見る 前は くれない）
#   ・なたが あれば その場で きって みちが ひらく（taiboku_nai）
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
        pg.keyboard.press("Space"); pg.wait_for_timeout(110)

with sync_playwright() as pw:
    b = pw.chromium.launch(args=["--autoplay-policy=no-user-gesture-required"])
    pg = b.new_page(viewport={"width": 960, "height": 540})
    errs = []
    pg.on("pageerror", lambda e: errs.append("PAGEERROR " + str(e)))
    pg.goto(f"http://127.0.0.1:{PORT}/index.html?record=1")
    pg.wait_for_function("() => window._ctrl && state === 'title'", timeout=15000)
    pg.evaluate("window._ctrl.start()"); pg.wait_for_timeout(400)
    drain(pg); pg.evaluate("window._ctrl.free()")

    # 見る 前は 朝ごはんで なたを くれない
    pre = pg.evaluate("""() => {
      delete WORLD.flags['saw_taiboku']; delete WORLD.items['nata'];
      delete WORLD.fired['nata_kubaru'];
      const steps = mealSteps('breakfast', { day: WORLD.day, at:'breakfast' });
      return { nata: hasItem('nata'), n: steps.length };
    }""")
    print("見る前の 朝ごはん: なた=", pre["nata"])
    if pre["nata"]: fails.append("倒木を 見ていないのに なたを くれた")

    # 倒木に 近づく → わけ＋見た しるし
    pg.evaluate("window._ctrl.goto('aze')"); pg.wait_for_timeout(300)
    drain(pg); pg.evaluate("window._ctrl.free()")
    pg.evaluate("window._ctrl.put(473,312)"); pg.wait_for_timeout(400)
    drain(pg)
    saw = pg.evaluate("() => hasFlag('saw_taiboku')")
    print("倒木に 近づいた: 見たしるし=", saw)
    if not saw: fails.append("倒木に 近づいても 見た しるしが 立たない")

    # なたが ないと きれない（gate は そのまま）
    pg.evaluate("window._ctrl.free(); window._ctrl.put(473,312)"); pg.wait_for_timeout(200)
    pg.evaluate("window._ctrl.act()"); pg.wait_for_timeout(200); drain(pg)
    cut_nonata = pg.evaluate("() => hasFlag('taiboku_nai')")
    print("なた なしで キー: きれた=", cut_nonata)
    if cut_nonata: fails.append("なたが ないのに きれた")

    # 見た つぎの 朝ごはんで なたを くれる（フラグは 立っている）
    got = pg.evaluate("""() => {
      delete WORLD.fired['nata_kubaru'];
      const steps = mealSteps('breakfast', { day: WORLD.day, at:'breakfast' });
      return { nata: hasItem('nata'), lines: steps.map(s=>s.text||'').filter(Boolean) };
    }""")
    print("見た後の 朝ごはん: なた=", got["nata"], " 台詞数=", len(got["lines"]))
    if not got["nata"]: fails.append("倒木を 見たのに 朝ごはんで なたを くれない")

    # なたが あれば その場で きって みちが ひらく
    pg.evaluate("window._ctrl.free(); window._ctrl.put(473,312)"); pg.wait_for_timeout(200)
    pg.evaluate("window._ctrl.act()"); pg.wait_for_timeout(250)
    cut = pg.evaluate("() => hasFlag('taiboku_nai')")
    say = pg.evaluate("window._ctrl.scene()")
    print("なたで キー: きれた=", cut, " 台詞=", say and say.get('say'))
    if not cut: fails.append("なたを もっても きれない")
    drain(pg)
    # みちが ひらいたか（gate が あかない と walkable が とおらない）
    through = pg.evaluate("() => { const s=cur; cur='aze'; const w=walkable(473,240); cur=s; return w; }")
    print("倒木の あった ところ 通れる:", through)
    if not through: fails.append("きったのに みちが ひらかない")

    # === ハチの す（mori）も 同じ型：見る→朝ごはんで かとりせんこう→けむりで 自分で ===
    pg.evaluate("window._ctrl.goto('mori')"); pg.wait_for_timeout(300)
    drain(pg); pg.evaluate("window._ctrl.free()")
    pg.evaluate("window._ctrl.put(420,352)"); pg.wait_for_timeout(400); drain(pg)
    saw_h = pg.evaluate("() => hasFlag('saw_hachi')")
    print("ハチに 近づいた: 見たしるし=", saw_h)
    if not saw_h: fails.append("ハチに 近づいても 見た しるしが 立たない")

    pg.evaluate("window._ctrl.free(); window._ctrl.put(420,352)"); pg.wait_for_timeout(200)
    pg.evaluate("window._ctrl.act()"); pg.wait_for_timeout(200); drain(pg)
    if pg.evaluate("() => hasFlag('hachi_nai')"): fails.append("けむりが ないのに はちが どいた")

    got_h = pg.evaluate("""() => {
      delete WORLD.fired['kemuri_kubaru'];
      const steps = mealSteps('breakfast', { day: WORLD.day, at:'breakfast' });
      return { kemuri: hasItem('kemuri'), n: steps.length };
    }""")
    print("見た後の 朝ごはん: かとりせんこう=", got_h["kemuri"])
    if not got_h["kemuri"]: fails.append("ハチを 見たのに 朝ごはんで かとりせんこうを くれない")

    pg.evaluate("window._ctrl.free(); window._ctrl.put(420,352)"); pg.wait_for_timeout(200)
    pg.evaluate("window._ctrl.act()"); pg.wait_for_timeout(250)
    cut_h = pg.evaluate("() => hasFlag('hachi_nai')")
    print("けむりで キー: どいた=", cut_h)
    if not cut_h: fails.append("かとりせんこうを もっても はちが どかない")
    drain(pg)

    print("errors:", errs[:3])
    if errs: fails.append("エラー " + str(errs[:2]))
    b.close()
srv.shutdown()
if fails:
    print("=== NG ==="); [print("  -", f) for f in fails]; sys.exit(1)
print("=== すべて OK ===")
