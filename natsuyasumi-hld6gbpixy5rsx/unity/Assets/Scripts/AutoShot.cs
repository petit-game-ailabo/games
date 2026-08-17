using System;
using System.Collections;
using UnityEngine;

// ビルドした ゲームを 起動して、数フレーム 待ってから 絵を 書きだして 終わる。
// エディタの batchmode 描画は あてに ならないので、**実際に 動く ゲームの 画** で 確かめる。
// 使いかた: game.exe -shot C:\path\out.png [-shotframes 60]
public class AutoShot : MonoBehaviour {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() {
        var go = new GameObject("~AutoShot");
        DontDestroyOnLoad(go);
        go.AddComponent<AutoShot>();
    }

    static string Arg(string key, string def) {
        var a = Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++) if (a[i] == key) return a[i + 1];
        return def;
    }

    IEnumerator Start() {
        string path = Arg("-shot", null);
        if (string.IsNullOrEmpty(path)) yield break;
        int frames = int.Parse(Arg("-shotframes", "90"));

        // **絵を 書きだす あいだは カメラを 手で 動かせなく する。**
        // 窓もちの まま 走らせると ホイールや 右ドラッグが まぎれこんで 寄りが 変わり、
        // 撮るたびに 画角が ちがって しまう（実際 見くらべが できなく なった）
        var orbit = FindFirstObjectByType<CamOrbit>();
        if (orbit != null) {
            orbit.allowMouse = false;
            // -cam "distance,pitch,yaw" で 画角を 決めうちに できる（庭まで 入れて 見たい ときなど）
            string cam = Arg("-cam", null);
            if (!string.IsNullOrEmpty(cam)) {
                var c = cam.Split(',');
                if (c.Length > 0) orbit.distance = float.Parse(c[0]);
                if (c.Length > 1) orbit.pitch = float.Parse(c[1]);
                if (c.Length > 2) orbit.yaw = float.Parse(c[2]);
                Debug.Log($"[AutoShot] cam distance={orbit.distance} pitch={orbit.pitch} yaw={orbit.yaw}");
            }
        }

        // -at "x,z" で 主人公を そこへ 移す。**遠くの 見せ場を 撮る ための 近道。**
        // 高台まで 歩かせると 何十秒も かかり、たしかめが 回らない。
        // "x,z,y" と 3つ 書くと 高さも 決めうちに する。
        // ※これが 無いと **屋内を 撮れない。** 上から レイを 落とすと いちばん 上の
        //   当たり＝屋根に 乗って しまい、家の 上に 立った 絵に なった
        string at = Arg("-at", null);
        if (!string.IsNullOrEmpty(at)) {
            var pm = FindFirstObjectByType<PlayerMove>();
            if (pm != null) {
                var q = at.Split(',');
                float ax = float.Parse(q[0]), az = float.Parse(q[1]);
                var cc0 = pm.GetComponent<CharacterController>();
                if (q.Length > 2) {
                    if (cc0 != null) cc0.enabled = false;
                    pm.transform.position = new Vector3(ax, float.Parse(q[2]), az);
                    if (cc0 != null) cc0.enabled = true;
                } else {
                    // 空から 落として 地めんに のせる（地形の 高さを ここでは 知らない）
                    if (cc0 != null) cc0.enabled = false;
                    pm.transform.position = new Vector3(ax, 60f, az);
                    if (cc0 != null) cc0.enabled = true;
                    RaycastHit gh;
                    if (Physics.Raycast(new Vector3(ax, 80f, az), Vector3.down, out gh, 200f, ~0,
                                        QueryTriggerInteraction.Ignore)) {
                        if (cc0 != null) cc0.enabled = false;
                        pm.transform.position = gh.point + Vector3.up * 0.1f;
                        if (cc0 != null) cc0.enabled = true;
                    }
                }
                // カメラが 寄せきるまで 待つ（見せ場の 切りかえは ゆっくり 効く）
                for (int w = 0; w < 180; w++) yield return null;
                Debug.Log("[AutoShot] 移した -> " + pm.transform.position);
            }
        }

        // -walk "x,y" が あれば、その むきに しばらく あるかせてから 撮る（当たりの たしかめ）
        string walk = Arg("-walk", null);
        if (!string.IsNullOrEmpty(walk)) {
            var pm = FindFirstObjectByType<PlayerMove>();
            if (pm != null) {
                var pp = walk.Split(',');
                pm.useAutoInput = true;
                pm.autoRun = Arg("-run", null) != null;      // 走りの 絵の たしかめ
                pm.autoInput = new Vector2(float.Parse(pp[0]), float.Parse(pp[1]));
                var start = pm.transform.position;
                float t = float.Parse(Arg("-walksec", "2.0"));
                for (float e = 0f; e < t; e += Time.deltaTime) yield return null;
                // -walkhold を つけると **歩いた まま 撮る**。
                // 歩き・走りの 絵は 止まって しまうと 見られない
                if (Arg("-walkhold", null) == null) { pm.useAutoInput = false; pm.autoInput = Vector2.zero; }
                var moved = pm.transform.position - start;
                Debug.Log($"[AutoShot] walked {moved.magnitude:F2}m  from {start} to {pm.transform.position}");
            } else Debug.LogWarning("[AutoShot] PlayerMove が 見つからない");
        }

        // -bugs N を つけると、いちばん 近い 虫の そばへ 移って あみを ふる。
        // **つかまえられる ことを 人手なしで たしかめる** ための 自動運転
        int bugTries = int.Parse(Arg("-bugs", "0"));
        if (bugTries > 0) {
            var catcher = FindFirstObjectByType<BugCatcher>();
            var book = FindFirstObjectByType<BugBook>();
            var cc = catcher != null ? catcher.GetComponent<CharacterController>() : null;
            var cam = Camera.main;
            // **毎回 まっさらから 数える。** 前の たしかめの 記録が 残っていると
            // 「取れた のか 前のぶん なのか」が 読めない
            if (book != null) book.Clear();
            for (int i = 0; i < bugTries && catcher != null; i++) {
                // 虫が 湧くのを 待つ
                Bug target = null;
                for (int w = 0; w < 240 && target == null; w++) { target = catcher.Nearest(); yield return null; }
                if (target == null) { Debug.Log("[AutoShot] 虫が いない"); break; }

                // 虫の 手前(あみの とどく ところ)へ 立つ
                Vector3 fwd = cam != null ? cam.transform.forward : Vector3.forward;
                fwd.y = 0f; fwd.Normalize();
                var stand = target.transform.position - fwd * catcher.reach;
                stand.y = catcher.transform.position.y;
                if (cc != null) cc.enabled = false;
                catcher.transform.position = stand;
                if (cc != null) cc.enabled = true;

                // ★**ふる 前に 虫の ほうを 向く。**（2026-08-17）
                //   立ち位置だけ 合わせて いた ころは 8回 ふって 0回しか 取れず、
                //   「虫の 顔ぶれは 変わったが 取れない」と 読めて しまった。
                //   あみの 判定は **向きで 前後・左右に ずれる**ので、
                //   向きを 合わせない たしかめは 当たらなくて あたりまえ だった
                var pm2 = catcher.GetComponent<PlayerMove>();
                if (pm2 != null) {
                    var to = target.transform.position - catcher.transform.position; to.y = 0f;
                    if (to.sqrMagnitude > 1e-4f) {
                        to.Normalize();
                        pm2.useAutoInput = true;
                        pm2.autoInput = new Vector2(Vector3.Dot(to, cam != null ? cam.transform.right : Vector3.right),
                                                    Vector3.Dot(to, fwd));
                        for (int w = 0; w < 20; w++) yield return null;
                        pm2.autoInput = Vector2.zero;
                        pm2.useAutoInput = false;
                    }
                }
                int before = book != null ? book.Total : 0;
                catcher.TrySwing();
                // ふり切るまでに 虫は 動く。人が 遊ぶ ときも 同じ なので、
                // **止めては いけない**（止めると 当たり判定だけを 見て 手ざわりを 見のがす）
                for (int w = 0; w < 60; w++) yield return null;
                int after = book != null ? book.Total : 0;
                Debug.Log(string.Format("[AutoShot] ふった {0}: {1} → 取った={2} (しゅるい {3})",
                          i, target.kind.name, after > before, book != null ? book.Kinds : -1));
            }
            if (book != null) Debug.Log("[AutoShot] むし ごうけい=" + book.Total + " しゅるい=" + book.Kinds);
        }

        // -sumo N を つけると むしずもうを N回 おして みる（勝ち負けが 動くか の たしかめ）
        int sumoPress = int.Parse(Arg("-sumo", "0"));
        if (sumoPress > 0) {
            var sumo = FindFirstObjectByType<BugSumo>();
            if (sumo == null) Debug.Log("[AutoShot] むしずもうが 無い");
            else if (!sumo.CanStart()) Debug.Log("[AutoShot] かごが 空で いどめない");
            else {
                Debug.Log("[AutoShot] むしずもう はじめ=" + sumo.Begin());
                // やじるしで つつく（0=まえ 1=うしろ 2=ひだり 3=みぎ）と 声えんを まぜる
                for (int i = 0; i < sumoPress && sumo.Busy; i++) {
                    if (i % 3 == 0) sumo.DebugCheer();
                    else sumo.DebugPoke(i % 4);
                    for (int w = 0; w < 22; w++) yield return null;   // つつく 間かくを あける
                }
                Debug.Log("[AutoShot] むしずもう けっか=" + sumo.DebugState);
                for (int w = 0; w < 30; w++) yield return null;
            }
        }

        // -face N（0〜7）で 向きを 決めうちに して 撮る。**8方向の 絵の たしかめ**。
        // -pose N（0〜7）で 状態も 決めうち（0立ち 1歩き 2走り 3喜 4怒 5哀 6楽 7目とじ）
        string faceArg = Arg("-face", null), poseArg = Arg("-pose", null);
        if (!string.IsNullOrEmpty(faceArg) || !string.IsNullOrEmpty(poseArg)) {
            var cs = FindFirstObjectByType<CharSprite>();
            if (cs == null) Debug.Log("[AutoShot] CharSprite が ない");
            else {
                cs.debugCell = true;
                cs.debugDir = string.IsNullOrEmpty(faceArg) ? 0 : int.Parse(faceArg);
                cs.debugRow = string.IsNullOrEmpty(poseArg) ? 0 : int.Parse(poseArg);
                Debug.Log("[AutoShot] むき=" + cs.debugDir + " すがた=" + cs.debugRow);
            }
        }

        // -play sasabune|mizukiri|tsuri|hana|irozu|oshibana|himitsu で 遊びを ためす。
        // **押す 間かくを あけて 通す。** 水きりは 2回、つりは あたりを 待って 1回
        string playName = Arg("-play", null);
        if (!string.IsNullOrEmpty(playName)) {
            var ph = FindFirstObjectByType<PlayHost>();
            if (ph == null) Debug.Log("[AutoShot] PlayHost が ない");
            else {
                PlayKind pk;
                switch (playName.ToLower()) {
                    case "sasabune": pk = PlayKind.Sasabune; break;
                    case "mizukiri": pk = PlayKind.Mizukiri; break;
                    case "tsuri":    pk = PlayKind.Tsuri;    break;
                    case "hana":     pk = PlayKind.Hanatsumi;break;
                    case "irozu":    pk = PlayKind.Irozu;    break;
                    case "oshibana": pk = PlayKind.Oshibana; break;
                    case "shukudai": pk = PlayKind.Shukudai; break;
                    case "kingyo":   pk = PlayKind.Kingyo;   break;
                    case "hanabi":   pk = PlayKind.Hanabi;   break;
                    case "himitsu":  pk = PlayKind.Himitsu;  break;
                    case "dagashi": pk = PlayKind.Dagashi;  break;
                    case "shateki": pk = PlayKind.Shateki;  break;
                    case "kuji":    pk = PlayKind.Kuji;     break;
                    case "odori":   pk = PlayKind.Bonodori; break;
                    case "toro":    pk = PlayKind.Toronagashi; break;
                    case "hoshi":   pk = PlayKind.Hoshi;    break;
                    // ★**知らない 名は だまって ひみつきちに しない。**（2026-08-17）
                    //   -play hanabi と 打って ひみつきちが 走り、それに 気づかず
                    //   「線こう花火が 動いた」と 思いこむ ところだった
                    default:
                        Debug.LogError("[AutoShot] -play の 名が わからない: " + playName);
                        pk = PlayKind.Himitsu;  break;
                }
                // **自動運転は ねらい所で 押す。** 一定間かくで 押させて いた ころは
                // 水きりが いつも 0段、つりは いつも 逃げられ、うまく いった ときの
                // 画が 一度も 撮れて いなかった
                ph.debugAuto = true;
                ph.debugShina = int.Parse(Arg("-shina", "0"));
                Debug.Log("[AutoShot] あそび はじめ=" + ph.DebugBegin(pk));
                // -playwait 0 に すると 遊びの **さいちゅうを 撮る**（終わるのを 待たない）
                if (Arg("-playwait", "1") != "0") {
                    for (int w = 0; w < 1200 && ph.Busy; w++) yield return null;
                    Debug.Log("[AutoShot] あそび けっか=" + ph.DebugState);
                }
            }
        }

        // -diary を つけると 日記帳を ひらく。-diary enikki で 絵日記の がわ
        {
            string dv = Arg("-diary", null);
            if (dv != null) {
                var dh = FindFirstObjectByType<DayHost>();
                if (dh != null) dh.DebugOpenDiary(dv == "enikki");
                else Debug.LogError("[AutoShot] DayHost が ない");
            }
        }

        // -hyohon N で かごの 虫を N ひき 標本に する（部屋が 育つ ことの たしかめ）
        {
            int hy = int.Parse(Arg("-hyohon", "0"));
            if (hy > 0) {
                var bk = FindFirstObjectByType<BugBook>();
                // かごに N しゅるい 入れてから 標本に する（Add → MakeSpecimen の 道を そのまま 通す）
                for (int i = 0; i < hy && bk != null; i++) {
                    var kk = BugKind.All[i % BugKind.All.Length];
                    bk.Add(kk.id, BugBook.RollSize(kk));
                    bk.MakeSpecimen();
                }
                if (bk != null) Debug.Log("[AutoShot] ひょうほん しゅるい=" + bk.SpecimenKinds);
                for (int w = 0; w < 70; w++) yield return null;   // Sodatsu が 見なおすまで 待つ
            }
        }

        // ★-neru N で **N日 寝る**。1日の わっか（寝る→日記→朝）を そのまま 通す。
        //   かごの 虫が 弱って 逃げる ことは、日を またがないと 確かめられない。
        //   （2026-08-17：BugBook.Asa() を 書いた つもりで どこからも 呼んで おらず、
        //     出るはずの ない「よわって いる！」を「出ました」と 報告して しまった。
        //     日を またぐ たしかめが 無かったのが 大もとの 原因）
        {
            int neru = int.Parse(Arg("-neru", "0"));
            if (neru > 0) {
                var dh = FindFirstObjectByType<DayHost>();
                var bk = FindFirstObjectByType<BugBook>();
                var nk = FindFirstObjectByType<Nikki>();
                if (dh == null) Debug.LogError("[AutoShot] DayHost が ない");
                else for (int n = 0; n < neru; n++) {
                    int mae = bk != null ? bk.Recent.Count : 0;
                    dh.DebugNeru();
                    for (int w = 0; w < 900 && dh.DebugOkuri; w++) {
                        // 日記の 画面で スペース待ちに なる ので 押して やる
                        if (w % 40 == 39) dh.DebugSusumeru();
                        yield return null;
                    }
                    Debug.Log(string.Format("[AutoShot] {0}日め あさ: かご {1} → {2}　にがした={3}",
                              nk != null ? nk.day : -1, mae,
                              bk != null ? bk.Recent.Count : -1,
                              bk != null ? bk.Freed : -1));
                }
            }
        }

        // ★-tosi で **31日ぶんの 品しなを 数える。**（2026-08-17）
        //   遊ぶ 人：「9巡ぶん、検証は すべて『その 機能が 動く 画面を 1枚』でした。
        //     個々の 部品は 全部 正しい。でも **31日を 頭から 通した 人が、まだ 1人も いません**。
        //     42分 × 31日 ＝ 約22時間。その 22時間ぶんの 中みが、本当に ありますか」
        //
        //   1日ずつ **その日に できる ことを 数えて 出す**。
        //   「何も する ことが なかった 日」が 何日 あるかは、これで はっきり する
        if (Arg("-tosi", null) != null) {
            var nk = FindFirstObjectByType<Nikki>();
            var wx2 = FindFirstObjectByType<Weather>();
            // ★**数えるときは 天気を 決めうちに しない。**-weather は 撮影の ための 引数で、
            //   これが 立った ままだと RollForDay が 素通りして **31日 ぜんぶ 晴れ**に なる
            //  （最初の 集計は それで 20日の 台風まで 晴れに 見えて いた）
            if (wx2 != null) wx2.forced = false;
            var td2 = FindFirstObjectByType<TimeOfDay>();
            var spots = FindObjectsByType<PlaySpot>(FindObjectsSortMode.None);
            var npcs2 = FindObjectsByType<Npc>(FindObjectsSortMode.None);
            Debug.Log("[Tosi] 日 | できごと | 天気 | 昼の遊び | 夜の遊び | 話せる人(昼/夜) | 出る虫");
            for (int d = 1; d <= Nikki.LastDay; d++) {
                if (nk != null) nk.day = d;
                if (wx2 != null) wx2.RollForDay(d);
                var g2 = FindFirstObjectByType<Gyoji>();
                if (g2 != null) g2.Apply(d);

                int hiru = 0, yoru = 0;
                foreach (var sp in spots) {
                    if (sp == null) continue;
                    if (td2 != null) td2.hour = 13f;
                    if (sp.Ima) hiru++;
                    if (td2 != null) td2.hour = 20f;
                    if (sp.Ima) yoru++;
                }
                int nh = 0, ny = 0;
                foreach (var n in npcs2) {
                    if (n == null) continue;
                    if (td2 != null) td2.hour = 13f;
                    if (n.Iru) nh++;
                    if (td2 != null) td2.hour = 20f;
                    if (n.Iru) ny++;
                }
                // その日 出やすい 虫（かかりが 0.4 いじょう）
                var mushi = new System.Text.StringBuilder();
                foreach (var k in BugKind.All)
                    if (k.Koro(d) >= 0.4f) { if (mushi.Length > 0) mushi.Append("/"); mushi.Append(k.name); }

                Debug.Log(string.Format("[Tosi] {0,2} | {1,-13} | {2,-8} | {3,2} | {4,2} | {5}/{6} | {7}",
                          d, Nikki.OnDay(d), wx2 != null ? wx2.mode.ToString() : "?",
                          hiru, yoru, nh, ny, mushi));
            }
            if (nk != null) nk.day = 1;
        }

        // -book を つけると ずかんを ひらいた ところを 撮る
        if (Arg("-book", null) != null) {
            var hud = FindFirstObjectByType<BugHud>();
            if (hud != null) hud.OpenBook();
        }

        // 光・影・ポストFXが おちつくまで 待つ
        for (int i = 0; i < frames; i++) yield return null;

        // -shots N をつけると N枚を -shotgap 秒おきに 撮る。
        // **息づかいのような 動く 効きめは 1枚では たしかめられない**ので、
        // 時間を ずらして 撮って 見くらべる（out_0.png, out_1.png ...）
        int shots = int.Parse(Arg("-shots", "1"));
        float gap = float.Parse(Arg("-shotgap", "1.0"));
        for (int s = 0; s < shots; s++) {
            string p = shots == 1
                ? path
                : System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path),
                      System.IO.Path.GetFileNameWithoutExtension(path) + "_" + s +
                      System.IO.Path.GetExtension(path));
            ScreenCapture.CaptureScreenshot(p);
            Debug.Log("[AutoShot] captured -> " + p);
            for (int i = 0; i < 20; i++) yield return new WaitForEndOfFrame();
            if (s < shots - 1) yield return new WaitForSeconds(gap);
        }
        yield return new WaitForSeconds(1.0f);
        Application.Quit();
    }
}
