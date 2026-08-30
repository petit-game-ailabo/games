using UnityEditor;
using UnityEngine;

// ★1回の Unity起動で「テクスチャ取りこみ → 場面を 組む → プレイヤーを 建てる」まで やる。
//   前は -Only を 3回 呼んで いて、そのたびに Unityの 起動（読みこみ＋ドメイン再読み）に
//   1分ちかく かかって いた（本人 2026-08-30「時間かかりすぎじゃない？」）。
//   rebuild.ps1 -Only NiwaAll.Win  /  -Only NiwaAll.WinWeb
public static class NiwaAll {

    static void Steps(bool web) {
        var t0 = System.DateTime.Now;
        SetupURP.FixPixelArt();
        var t1 = System.DateTime.Now;
        BuildNiwa.Build();
        var t2 = System.DateTime.Now;
        BuildNiwaPlayer.Win();
        var t3 = System.DateTime.Now;
        if (web) BuildNiwaPlayer.Web();
        var t4 = System.DateTime.Now;
        Debug.Log(string.Format("[Probe] NiwaAll 取りこみ{0:F0}秒 場面{1:F0}秒 Win{2:F0}秒 Web{3:F0}秒",
            (t1-t0).TotalSeconds, (t2-t1).TotalSeconds, (t3-t2).TotalSeconds, (t4-t3).TotalSeconds));
    }

    public static void Win()    { Steps(false); }
    public static void WinWeb() { Steps(true); }
}
