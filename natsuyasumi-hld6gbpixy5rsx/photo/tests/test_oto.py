# -*- coding: utf-8 -*-
# うごかしかた:  python tests/test_oto.py
#   pip install playwright && playwright install chromium  が いる。
# 時計は 出さないので、**耳でも 時間が わかる**ようになっているか。
#   あさ ちゅんちゅん／ひる セミ／ゆうがた ひぐらしと鈴虫／よる カラスと鈴虫
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
fails = []

# steps → こう 鳴いてほしい
WANT = [(0, "asa"), (2, "asa"), (6, "hiru"), (13, "hiru"), (16, "yugata"), (23, "yugata")]

with sync_playwright() as pw:
    b = pw.chromium.launch(args=["--autoplay-policy=no-user-gesture-required"])
    pg = b.new_page(viewport={"width": 960, "height": 540})
    errs = []
    pg.on("pageerror", lambda e: errs.append("PAGEERROR " + str(e)))
    pg.on("console", lambda m: errs.append("CONSOLE " + m.text) if m.type == "error" else None)
    pg.goto(f"http://127.0.0.1:{PORT}/index.html?record=1")
    pg.wait_for_timeout(3500)
    pg.evaluate("window._ctrl.start()"); pg.wait_for_timeout(400)
    pg.evaluate("window._ctrl.free()"); pg.wait_for_timeout(400)
    # 音は キーを おさないと はじまらない（ブラウザの きまり）
    pg.keyboard.press("Space"); pg.wait_for_timeout(600)
    pg.evaluate("window._ctrl.setMukae(true); window._ctrl.setYoru(true);")

    on = pg.evaluate("window._ctrl.amb()")["on"]
    print(f"  音が つかえるか: {on}")
    if not on: fails.append("AudioContext が つくれていない。この検査は 音つきで 走らせること")

    # 時間帯ごとに、ほんとうに その音が 鳴るまで 待つ
    for steps, want in WANT:
        pg.evaluate(f"window._ctrl.setYoru(false); window._ctrl.setSteps({steps});")
        pg.evaluate("window._ctrl.setYoru(false)")
        got = None
        for _ in range(60):
            pg.wait_for_timeout(200)
            a = pg.evaluate("window._ctrl.amb()")
            if a["last"]:
                got = a["last"]
                if got == want: break
            pg.evaluate(f"window._ctrl.setSteps({steps})")   # よやくや 日ぐれで ずれないように
        a = pg.evaluate("window._ctrl.amb()")
        ok = (got == want)
        print(f"  steps={steps:2d} dayT={a['dayT']}  ほしい={want:7s} 鳴った={got} {'OK' if ok else 'NG'}")
        if not ok: fails.append(f"steps={steps}: {want} が 鳴らない（{got}）")

    # よる（晩ごはんの あと）
    pg.evaluate("window._ctrl.setSteps(24); window._ctrl.setYoru(true);")
    got = None
    for _ in range(60):
        pg.wait_for_timeout(200)
        a = pg.evaluate("window._ctrl.amb()")
        if a["last"] == "yoru": got = "yoru"; break
        pg.evaluate("window._ctrl.setYoru(true)")
    print(f"  晩ごはんの あと: 鳴った={got}")
    if got != "yoru": fails.append("晩ごはんの あとに よるの音に ならない")

    # --- 場所で 風と 水が 変わるか
    print()
    WANT_WIND = {"zashiki":"in", "doma":"in", "rouka":"in",
                 "iemae":"out", "aze":"ki", "mori":"ki"}
    seen = {}
    for s, amb in WANT_WIND.items():
        pg.evaluate(f"window._ctrl.setSteps(0); window._ctrl.setYoru(true); window._ctrl.goto('{s}')")
        pg.wait_for_timeout(1300)
        p = pg.evaluate("window._ctrl.place()")
        seen[s] = p
        print(f"  {s:8s} amb={p['amb']:4s} 風={p['wind']:.3f} こもり={p['windF']:4d}Hz 水={p['water']:.3f}")
        if p["amb"] != amb: fails.append(f"{s}: amb が {p['amb']}（{amb} のはず）")
    if not (seen["zashiki"]["wind"] < seen["iemae"]["wind"] < seen["mori"]["wind"]):
        fails.append("家のなか < いえのまえ < そとの みち の じゅんに 風が 強く なっていない")
    if not (seen["zashiki"]["windF"] < seen["mori"]["windF"]):
        fails.append("家のなかの 風が こもっていない")
    if not (seen["aze"]["water"] > 0.01): fails.append("あぜみちで 水の音が しない")
    if seen["mori"]["water"] > 0.01: fails.append("水の ない画面で 水の音が する")

    # 家のなかでは 風鈴、そとの みちでは 木の葉ずれ
    for s, want in [("zashiki", "fuurin"), ("mori", "kaze")]:
        pg.evaluate(f"window._ctrl.goto('{s}')"); pg.wait_for_timeout(400)
        got = set()
        for _ in range(160):
            pg.wait_for_timeout(200)
            pg.evaluate("window._ctrl.setSteps(0); window._ctrl.setYoru(true);")
            v = pg.evaluate("window._ctrl.place()")["lastPlace2"]
            if v: got.add(v)
            if want in got: break
        print(f"  {s:8s} で 鳴った: {sorted(got)}")
        if want not in got: fails.append(f"{s}: {want} が 鳴らない")
        if s == "mori" and "fuurin" in got: fails.append("そとなのに 風鈴が 鳴る")

    # --- 足音が 画面ごとに 変わるか（キーで じっさいに 歩かせる）
    print()
    WANT_ASHI = {"zashiki":"tatami", "doma":"tsuchi", "rouka":"ita",
                 "iemae":"jari", "aze":"kusa", "mori":"ishi"}
    for s, want in WANT_ASHI.items():
        pg.evaluate(f"window._ctrl.setSteps(0); window._ctrl.setYoru(true); window._ctrl.goto('{s}')")
        pg.wait_for_timeout(400)
        pg.evaluate("window._ctrl.free()")
        d0 = pg.evaluate("window._ctrl.dbg()")
        # **画面から 出ないように 引きもどしながら 歩かせる。**
        # どまの 左は ざしきなので、ふつうに 歩かせると となりの 足音を 聞いてしまう
        pg.keyboard.down("ArrowLeft")
        for _ in range(6):
            pg.wait_for_timeout(150)
            pg.evaluate(f"window._ctrl.put({d0['x']},{d0['y']})")
        pg.keyboard.up("ArrowLeft")
        f = pg.evaluate("window._ctrl.foot()")
        now = pg.evaluate("window._ctrl.dbg()")["cur"]
        ok = (f["last"] == want and now == s)
        print(f"  {s:8s} ほしい={want:7s} 鳴った={f['last']:7s} {'OK' if ok else 'NG'}"
              f"{'' if now == s else '  （'+now+' へ 出てしまった）'}")
        if not ok: fails.append(f"{s}: 足音が {want} で ない（{f['last']}）")

    # 歩いた ぶんだけ 鳴る。とまっているときは 鳴らない
    pg.evaluate("window._ctrl.setSteps(0); window._ctrl.goto('iemae')"); pg.wait_for_timeout(400)
    pg.evaluate("window._ctrl.free()")
    n0 = pg.evaluate("window._ctrl.foot()")["n"]
    pg.keyboard.down("ArrowLeft"); pg.wait_for_timeout(1500); pg.keyboard.up("ArrowLeft")
    n1 = pg.evaluate("window._ctrl.foot()")["n"]
    pg.wait_for_timeout(2500)
    n2 = pg.evaluate("window._ctrl.foot()")["n"]
    print(f"  1.5秒 歩いて {n1-n0}歩 → そのあと 2.5秒 じっとして {n2-n1}歩")
    if n1 - n0 < 2: fails.append(f"歩いても 足音が 鳴らない（{n1-n0}歩）")
    if n2 != n1: fails.append(f"とまっているのに 足音が 鳴る（{n2-n1}歩）")

    # 音を 足しても こまが 落ちていないか
    fps = pg.evaluate("""() => new Promise(r => { let n=0; const t0=performance.now();
      const s=()=>{n++; if(performance.now()-t0<1000) requestAnimationFrame(s); else r(n);};
      requestAnimationFrame(s); })""")
    print(f"  fps: {fps}")
    if fps < 50: fails.append(f"音を 足したら こまが 落ちた（{fps}fps）")

    print("\nerrors:", errs[:5])
    if errs: fails.append("エラー " + str(errs[:3]))
    b.close()
srv.shutdown()
print("\n=== 失敗 ===" if fails else "\n=== すべて OK ===")
for f in fails: print(" -", f)

sys.exit(1 if fails else 0)
