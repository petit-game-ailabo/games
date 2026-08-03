# -*- coding: utf-8 -*-
# 画面の中の 虫とり（P4a）：蝶が 世界に とび、あみで その場で とれるか。
#   ・昼は 蝶が とぶ／夜は とばない
#   ・あみが ないと ふれない、あると ふって とれる
#   ・とると 虫かご+1・図鑑に 種類・その日の のこりが へる（有限）
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
    pg.evaluate("window._ctrl.goto('aze')"); pg.wait_for_timeout(300)
    drain(pg); pg.evaluate("window._ctrl.free()")

    # 昼：蝶が とぶ
    pg.wait_for_timeout(400)
    n_day = pg.evaluate("bugs.length")
    active = pg.evaluate("bugsActive()")
    print("昼の 蝶:", n_day, " active:", active)
    if n_day < 1 or not active: fails.append("昼なのに 蝶が とばない")

    # あみが ないと ふっても とれない
    pg.evaluate("delete WORLD.items['ami']")
    kago0 = pg.evaluate("numOf('mushikago')")
    # 蝶を あみの 先へ 置いてから ふる（あみ なし）
    pg.evaluate("window._ctrl.put(540, 490)"); pg.wait_for_timeout(200)
    # 蝶を あみの 先へ 置いて、**同じ フレームで** ふる（あいだに 蝶が 動かないように）
    pg.evaluate("""() => { const np = netPoint(); if (bugs[0]) { bugs[0].gx = np.x; bugs[0].gy = player.y; } advance = true; }""")
    pg.wait_for_timeout(200)
    kago_noami = pg.evaluate("numOf('mushikago')")
    print("あみ なしで ふった 後の 虫かご:", kago_noami)
    if kago_noami != kago0: fails.append("あみが ないのに とれてしまった")

    # あみを 持たせ、手に 持って ふる → とれる（P8：装備していないと ふれない）
    pg.evaluate("WORLD.items['ami'] = 1; WORLD.hold = 'ami'")
    pool_before = pg.evaluate("leftToday('mushi:aze', 18)")
    pg.evaluate("""() => { const np = netPoint(); if (bugs[0]) { bugs[0].gx = np.x; bugs[0].gy = player.y; } advance = true; }""")
    pg.wait_for_timeout(250)
    kago1 = pg.evaluate("numOf('mushikago')")
    pool_after = pg.evaluate("leftToday('mushi:aze', 18)")
    zukan = pg.evaluate("Object.keys(WORLD.flags).filter(k=>k.startsWith('zukan:'))")
    print(f"あみで ふった 後: 虫かご {kago0}->{kago1}  のこり {pool_before}->{pool_after}  図鑑 {zukan}")
    if kago1 != kago0 + 1: fails.append("あみで ふっても 虫かごが ふえない")
    if pool_after != pool_before - 1: fails.append("とったのに その日の のこりが へらない")
    if not zukan: fails.append("とったのに 図鑑に 種類が のらない")

    # 夜は とばない
    pg.evaluate("window._ctrl.setYoru(true)"); pg.wait_for_timeout(300)
    n_night = pg.evaluate("bugs.length")
    print("夜の 蝶:", n_night)
    if n_night != 0: fails.append("夜なのに 蝶が とんでいる")

    # === 釣り（P4b）：水べで 竿を ふる → その場で 釣り（別画面に とばない） ===
    pg.evaluate("window._ctrl.setYoru(false)")
    pg.evaluate("delete WORLD.items['sao']")
    # 水べ aze_mizu2(700,470) の そばへ。まず 竿 なし＝釣れない
    pg.evaluate("window._ctrl.free(); window._ctrl.put(700, 500)"); pg.wait_for_timeout(200)
    near_water = pg.evaluate("window._ctrl.near()")
    pg.evaluate("window._ctrl.act()"); pg.wait_for_timeout(150)
    f_nosao = pg.evaluate("window._ctrl.fishing()")
    st_nosao = pg.evaluate("window._ctrl.dbg()")["state"]
    print("竿なしで キー:", near_water, "釣り=", f_nosao, "state=", st_nosao)
    if f_nosao is not None: fails.append("竿が ないのに 釣りが はじまった")

    # 竿を 持たせ 手に 持って ふる → その場で 釣りが はじまる（state は play のまま＝別画面でない）
    pg.evaluate("WORLD.items['sao'] = 1; WORLD.hold = 'sao'")
    pg.evaluate("window._ctrl.act()"); pg.wait_for_timeout(150)
    f_start = pg.evaluate("window._ctrl.fishing()")
    st_start = pg.evaluate("window._ctrl.dbg()")["state"]
    print("竿ありで キー: 釣り=", f_start, "state=", st_start)
    if not f_start: fails.append("竿を もっても 釣りが はじまらない")
    if st_start != "play": fails.append("釣りで 別画面（state）に とんでいる: " + str(st_start))

    # あたりが 来るまで 待って、あたりの あいだに 押す → つれる
    tsuri0 = pg.evaluate("numOf('tsuri')")
    got = False
    for _ in range(80):
        pg.wait_for_timeout(100)
        fs = pg.evaluate("window._ctrl.fishing()")
        if fs and fs["phase"] == "atari":
            pg.evaluate("window._ctrl.act()"); pg.wait_for_timeout(100)
            got = True; break
        if fs is None: break
    pg.wait_for_timeout(1500)   # owari の 見せ が おわるまで
    tsuri1 = pg.evaluate("numOf('tsuri')")
    tsutta = pg.evaluate("() => hasFlag('tsutta')")
    print(f"あたりで 押した: {got}  つり {tsuri0}->{tsuri1}  つったフラグ={tsutta}")
    if not got: fails.append("あたりが 来ない（釣りが 進まない）")
    if tsuri1 != tsuri0 + 1: fails.append("あたりで 押しても 釣れていない")
    fs_end = pg.evaluate("window._ctrl.fishing()")
    if fs_end is not None: fails.append("釣りが おわらない（play に もどらない）")
    # 釣りの あと ふつうに 歩ける（足どめが 解けている）
    st_after = pg.evaluate("window._ctrl.dbg()")["state"]
    if st_after != "play": fails.append("釣りの あと play に もどっていない")

    # === P8：道具の 持ちかえ（数字キー）と 虫かごビュー（C） ===
    pg.evaluate("window._ctrl.setYoru(false); window._ctrl.free()")
    pg.evaluate("window._ctrl.goto('aze')"); pg.wait_for_timeout(300)
    drain(pg); pg.evaluate("window._ctrl.free()")
    pg.evaluate("WORLD.items = {'ami':1, 'sao':1}; WORLD.hold = 'ami'")
    tools = pg.evaluate("toolsHeld()")
    pg.keyboard.press("Digit2"); pg.wait_for_timeout(80)
    hold2 = pg.evaluate("WORLD.hold")
    pg.keyboard.press("Digit1"); pg.wait_for_timeout(80)
    hold1 = pg.evaluate("WORLD.hold")
    print("持ちかえ:", tools, " 2キー->", hold2, " 1キー->", hold1)
    if len(tools) < 2: fails.append("道具が 2つ 持てていない")
    elif hold2 != tools[1] or hold1 != tools[0]: fails.append("数字キーで 持ちかえられない")
    # C で 虫かご → Esc で もどる（開いて すぐは とじない ガードが あるので 少し 待つ）
    pg.keyboard.press("KeyC"); pg.wait_for_timeout(400)
    st_view = pg.evaluate("state")
    pg.keyboard.press("Escape"); pg.wait_for_timeout(300)
    st_back = pg.evaluate("state")
    print("Cで虫かご:", st_view, " Escで:", st_back)
    if st_view != "view": fails.append("C で 虫かごが 開かない")
    if st_back != "play": fails.append("虫かごを とじても play に もどらない")

    print("errors:", errs[:3])
    if errs: fails.append("エラー " + str(errs[:2]))
    b.close()
srv.shutdown()
if fails:
    print("=== NG ===")
    for f in fails: print("  -", f)
    sys.exit(1)
print("=== すべて OK ===")
