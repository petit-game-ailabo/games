using UnityEngine;

// 遠景の 描き割りの 色を 1日に あわせて 掛ける（2026-08-30）。
// 描き割りは Unlit（絵の 色を そのまま 出す）なので、昼夜は ここで つける。
// Lit で 光を 受けさせると 写真の 色が 飛んで 白い もやに 見えた。
public class NiwaHaikeiIro : MonoBehaviour {
    public Material[] mats;
    public Light sun;

    void LateUpdate() {
        if (mats == null || sun == null) return;
        // 太陽の つよさ 0.035(月)〜1.25(真昼) を 0..1 に
        float k = Mathf.InverseLerp(0.035f, 1.25f, sun.intensity);
        // 夜は 青くらく、昼は そのまま、夕は 太陽の 色に すこし 寄せる
        var yoru = new Color(0.16f, 0.20f, 0.34f);
        var hiru = Color.white;
        var c = Color.Lerp(yoru, hiru, Mathf.Pow(k, 0.7f));
        if (k > 0.02f && k < 0.55f) c = Color.Lerp(c, sun.color * 0.9f, 0.35f);   // 朝夕の 焼け
        foreach (var m in mats) if (m != null) m.color = c;
    }
}
