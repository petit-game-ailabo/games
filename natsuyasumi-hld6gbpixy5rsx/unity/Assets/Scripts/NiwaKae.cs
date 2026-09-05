using UnityEngine;

/// <summary>
/// 主人公の 絵を えらぶ。**既定は 手描きの 2D**（marisa_walk）。
/// <c>-3d</c> を つけると 3Dの 体に 2Dの 頭を のせた もの（marisa_hybrid）に なる。
///
/// ★2026-09-06：3Dは やめて 2Dに もどした。本人「歩くと 3Dと 2D崩れちゃうね。
///   2Dに戻して、できるだけ2Dの絵をキレイに使えるように作っていこうか」。
///   3Dの 体に 2Dの 頭を のせる やりかたは、止まって いる あいだは 成りたつが
///   **歩くと 頭と 体の 動きが 合わず 崩れる**（頭は 2Dの コマ・体は 3Dの 焼き）。
///
/// ★引数を つけない で 起ちあげた ときに 切りかわって いないと 意味が ない。
///   はじめ 逆（既定＝2D・<c>-meshy</c> で 3D）に して いて、本人が
///   ローカルの exe を そのまま 起ちあげたら 前のままで 出た（2026-09-05）。
///
/// 絵の 大きさ（1792x3360・8列x10行）は どちらも 同じ なので
/// <see cref="CharSprite"/> の 計算は さわらなくて よい。
/// </summary>
public class NiwaKae : MonoBehaviour {
    public Renderer target;
    public Texture2D futsu;             // 手描き 2D（marisa_walk）＝既定
    public Texture2D meshy;             // 3Dの 体＋2Dの 頭（marisa_hybrid）＝見くらべ用

    void Awake() {
        bool sanD = false;
        foreach (var a in System.Environment.GetCommandLineArgs())
            if (a == "-3d") sanD = true;
        var t = sanD ? meshy : futsu;
        if (t == null) t = futsu;
        if (t == null || target == null || target.sharedMaterial == null) return;
        target.sharedMaterial.mainTexture = t;
        Debug.Log("[NiwaKae] キャラ絵 = " + t.name + " (" + t.width + "x" + t.height + ")");
    }
}
