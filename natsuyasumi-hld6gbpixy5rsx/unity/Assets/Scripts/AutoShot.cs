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

        // 光・影・ポストFXが おちつくまで 待つ
        for (int i = 0; i < frames; i++) yield return null;

        ScreenCapture.CaptureScreenshot(path);
        Debug.Log("[AutoShot] captured -> " + path);
        // 書きこみが 終わるまで すこし 待ってから おわる
        for (int i = 0; i < 20; i++) yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(1.0f);
        Application.Quit();
    }
}
