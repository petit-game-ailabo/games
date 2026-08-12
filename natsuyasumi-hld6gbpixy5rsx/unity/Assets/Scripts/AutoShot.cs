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
