using UnityEngine;

/// <summary>
/// 主人公の 絵を えらぶ。**既定は Meshy の 3Dを 焼いた もの**（2026-09-05・D-221）。
/// <c>-kyu2d</c> を つけると 前の 手描き 2D（marisa_walk）に もどる。
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
    public Texture2D futsu;             // 前の 手描き 2D（marisa_walk）
    public Texture2D meshy;             // Meshy の 3Dを 焼いた もの（marisa_meshy）

    void Awake() {
        bool modoru = false;
        foreach (var a in System.Environment.GetCommandLineArgs())
            if (a == "-kyu2d") modoru = true;
        var t = modoru ? futsu : meshy;
        if (t == null) t = futsu;       // 焼いた 絵が 無い ときは 前のに 落ちる
        if (t == null || target == null || target.sharedMaterial == null) return;
        target.sharedMaterial.mainTexture = t;
        Debug.Log("[NiwaKae] キャラ絵 = " + t.name + " (" + t.width + "x" + t.height + ")");
    }
}
