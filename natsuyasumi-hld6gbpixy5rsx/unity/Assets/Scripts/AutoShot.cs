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

        // -walk "x,y" が あれば、その むきに しばらく あるかせてから 撮る（当たりの たしかめ）
        string walk = Arg("-walk", null);
        if (!string.IsNullOrEmpty(walk)) {
            var pm = FindFirstObjectByType<PlayerMove>();
            if (pm != null) {
                var pp = walk.Split(',');
                pm.useAutoInput = true;
                pm.autoInput = new Vector2(float.Parse(pp[0]), float.Parse(pp[1]));
                var start = pm.transform.position;
                float t = float.Parse(Arg("-walksec", "2.0"));
                for (float e = 0f; e < t; e += Time.deltaTime) yield return null;
                pm.useAutoInput = false; pm.autoInput = Vector2.zero;
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
                for (int i = 0; i < sumoPress && sumo.Busy; i++) {
                    sumo.DebugPush();
                    for (int w = 0; w < 3; w++) yield return null;
                }
                Debug.Log("[AutoShot] むしずもう けっか=" + sumo.DebugState);
                for (int w = 0; w < 30; w++) yield return null;
            }
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
