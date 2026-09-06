using UnityEngine;

/// <summary>
/// 主人公の 絵を えらぶ。**既定は 描きなおした 走り**（marisa_codex・D-228）。
/// <c>-kyu</c> … いままでの 絵（marisa_walk）
/// <c>-3d</c>  … 3Dの 体に 2Dの 頭を のせた もの（marisa_hybrid・見くらべ用に 残して ある）
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
    /// <summary>★N キーで **その場で 見くらべ**（2026-09-06・D-245）。
    /// 数えたら **古い 正面（8枚・14fps・1コマの 変化 1775）は、本人が いいと 言った
    /// 奥むき（3024）より さらに 落ちついて いる**。
    /// 64枚 たのむ まえに、**もう 手もとに ある 絵**を 見て もらう。
    /// ★コマ数も いっしょに 変える。古い 絵は 8コマ・新しい 絵は 6コマ。
    ///   絵だけ 変えると **使って いない 行を 出して しまう**</summary>
    public CharSprite chars;
    public Renderer target;
    public Texture2D futsu;             // 手描き 2D（marisa_walk）＝既定
    public Texture2D meshy;             // 3Dの 体＋2Dの 頭（marisa_hybrid）＝見くらべ用
    public Texture2D shin;              // 描きなおした 走り（marisa_codex）

    void Awake() {
        Texture2D t = shin != null ? shin : futsu;
        foreach (var a in System.Environment.GetCommandLineArgs()) {
            if (a == "-3d" && meshy != null) t = meshy;
            if (a == "-kyu" && futsu != null) t = futsu;
        }
        if (t == null) t = futsu;
        if (t == null || target == null || target.sharedMaterial == null) return;
        Kiru(t);
    }

    void Kiru(Texture2D t) {
        if (t == null || target == null || target.sharedMaterial == null) return;
        ima = t;
        target.sharedMaterial.mainTexture = t;
        if (chars != null && chars.cycleFramesCol != null && chars.cycleFramesCol.Length > 0)
            chars.cycleFramesCol[0] = 8;   // 四版は 古い 絵と 同じ 8コマ（D-248）
        Debug.Log("[NiwaKae] キャラ絵 = " + t.name + " (" + t.width + "x" + t.height
                  + ") 正面の コマ数 " + (chars != null && chars.cycleFramesCol != null
                     && chars.cycleFramesCol.Length > 0 ? chars.cycleFramesCol[0] : -1));
    }

    Texture2D ima;

    void Update() {
        if (!Input.GetKeyDown(KeyCode.N)) return;
        Kiru(ima == shin && futsu != null ? futsu : (shin != null ? shin : futsu));
    }
}
