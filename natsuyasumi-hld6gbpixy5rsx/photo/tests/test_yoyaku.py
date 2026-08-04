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

    # --- 晩ごはんの けっか、どまに つりざおが 置かれたか。そばに 行けば ひろえるか
    oki = pg.evaluate("window._ctrl.items()")
    print(f"  晩ごはんの あと: もちもの={oki['mochi']} 置かれた={list(oki['oki'].keys())}")
    if not oki["oki"].get("doma"):
        fails.append("晩ごはんで つりざおが どまに 置かれない")
    else:
        pg.evaluate(NO_DUSK)
        o = oki["oki"]["doma"][0]
        # どまの 入り口は つりざおから 0.8人ぶんしか ないので、**着いた しゅんかんに
        # ひろってしまう**。おなじ tick のうちに 遠くへ どけて、それから 近づく
        pg.evaluate("window._ctrl.goto('doma'); window._ctrl.free();"
                    " window._ctrl.put(760,500);")
        pg.wait_for_timeout(600)
        pg.screenshot(path=os.path.join(OUT, "q_item.png"))
        if "sao" in pg.evaluate("window._ctrl.items()")["mochi"]:
            fails.append("はなれているのに ひろってしまう")
        pg.evaluate(f"window._ctrl.put({o['x']},{o['y']})"); pg.wait_for_timeout(300)
        pg.evaluate("window._ctrl.act()"); pg.wait_for_timeout(400)   # P1：キーで ひろう
        got = pg.evaluate("window._ctrl.items()")
        sc = pg.evaluate("window._ctrl.scene()")
        line = sc["say"][1] if sc and sc["say"] else ""
        print(f"  そばへ行ったら: もちもの={got['mochi']}  「{line}」")
        if "sao" not in got["mochi"]: fails.append("そばへ行っても つりざおを ひろえない")
        if got["oki"].get("doma"): fails.append("ひろったのに まだ 置かれたまま")
        if not line: fails.append("ひろっても なにも 言わない")
        if pg.evaluate("window._ctrl.scene()"): wait_scene_end(pg)

    # --- ねて つぎの日の 朝ごはん。あぜみちの 話が 出るか
    pg.evaluate("window._ctrl.sleep()")
    morning = wait_scene_end(pg, 1200)
    hit2 = [s for s in morning if "あぜみちに いた" in s]
    print(f"  2日目の朝の セリフ{len(morning)}こ  あぜみちの話={'あり' if hit2 else 'なし'}")
    if not hit2: fails.append("きのう あぜみちへ 行ったのに 朝ごはんで 話に ならない")
    # 出した ぶんだけ 消える。**ほかの よやくは のこる**
    # （さおを ひろった ときの 晩ごはんの ぶんが ここに いる。D-025 で 取りこぼさない）
    rest = pg.evaluate("window._ctrl.queue()")
    print("  朝ごはんの あと のこる よやく:", [f"{x['at']}(八月{x['day']}日)" for x in rest])
    if any(x["at"] == "breakfast" for x in rest):
        fails.append("出したはずの 朝ごはんの よやくが のこっている")
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

    # --- 数（K2）。ずっと のこる ものと、その日だけの ものを 分けている
    print()
    pg.evaluate("""() => {
      addNum('stamp'); addNum('stamp'); addNum('zukan', 5);
      leftToday('mushi:aze', 18); useToday('mushi:aze', 18); useToday('mushi:aze', 18);
    }""")
    n = pg.evaluate("window._ctrl.num()")
    print(f"  つけたところ: ずっと={n['zutto']} きょう={n['kyou']}")
    if n["zutto"].get("stamp") != 2: fails.append("ずっと のこる 数が つかない")
    if n["kyou"].get("mushi:aze") != 16: fails.append("その日だけの 数が 減らない")

    # ねると **その日だけの 数は 朝に もどり、ずっと のこる ものは 残る**
    pg.evaluate("window._ctrl.sleep()")
    wait_scene_end(pg, 1200)
    n2 = pg.evaluate("window._ctrl.num()")
    print(f"  ねた あと: ずっと={n2['zutto']} きょう={n2['kyou']}")
    if n2["zutto"].get("stamp") != 2: fails.append("ねたら ずっと のこる 数まで 消えた")
    if n2["kyou"].get("mushi:aze") is not None:
        fails.append("ねても その日だけの 数が もどらない（DESIGN §6：その日その場所の 虫は 有限）")

    # セーブして 読みなおしても のこるか
    n3 = pg.evaluate("() => { saveWorld(); return JSON.parse(localStorage.getItem(SAVE_KEY)).num; }")
    print(f"  セーブの中: {n3}")
    if (n3 or {}).get("stamp") != 2: fails.append("数が セーブに のっていない")

    # --- 時間帯の じょうけん（K5a）。**耳で きこえる 音と 同じ 区切り**で ないと
    # 手がかりに ならない（時計を 出さない ゲームなので）
    print()
    r = pg.evaluate("""() => {
      resetWorld(); state = 'play';
      const out = [];
      for (const s of [0, 6, 16]) { WORLD.steps = s;
        out.push([s, ambKind(), matchWhen({toki:ambKind()}, {}),
                  matchWhen({toki:'asa'}, {})]); }
      WORLD.steps = 24; WORLD.yoruDone = true;
      out.push([24, ambKind(), matchWhen({toki:'yoru'}, {}), matchWhen({toki:'asa'}, {})]);
      WORLD.yoruDone = false; WORLD.steps = 8;   // DAY_STEPS=16 で dayT=0.5＝ひる（D11）
      return { rows: out,
               list: [matchWhen({toki:['hiru','yoru']}, {}), matchWhen({toki:['asa']}, {})],
               fine: [matchWhen({tokiFrom:0.4}, {}), matchWhen({tokiFrom:0.6}, {})],
               plain: [matchWhen({}, {}), matchWhen(null, {})] };
    }""")
    for steps, kind, same, isAsa in r["rows"]:
        ok = same and (isAsa == (kind == "asa"))
        print(f"  steps={steps:2d} きこえる={kind:7s} じょうけんと 一致={same} {'OK' if ok else 'NG'}")
        if not ok: fails.append(f"steps={steps}: 時間帯の じょうけんが 耳と 合わない（{kind}）")
    if r["list"] != [True, False]: fails.append("toki を ならびで 書けない")
    if r["fine"] != [True, False]: fails.append("tokiFrom が きいていない")
    if r["plain"] != [True, True]: fails.append("じょうけんを 書かないと 通らなくなった")

    print("\nerrors:", errs[:5])
    if errs: fails.append("エラー " + str(errs[:3]))
    b.close()
srv.shutdown()
print("\n=== 失敗 ===" if fails else "\n=== すべて OK ===")
for f in fails: print(" -", f)

sys.exit(1 if fails else 0)
