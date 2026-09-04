using UnityEngine;

// ヒマワリの 育ち（2026-09-05）。
//
// ★本人「家の右側に外の水道蛇口と、ホースが欲しい。そしてヒマワリか何かを育てるような
//   プランター？鉢？あるいは地面直？の場所が欲しい。そこで毎日水を上げて、育てたい」
//
// いまは **日づけで 育つ**（8月1日 0.28 → 8月20日 満開）。水やりの 遊びを 入れる ときに
// Mizu（水を やった 日かず）を 立てれば、そちらが 優先に なる。
//   ・株ごとに 満開の 高さが ちがう（そろって いると 造花に 見える）
//   ・つぼみ → 花 は 0.72 で 入れかえる。花の ほうが 重いので 首も 少し 傾ける
public class NiwaHimawari : MonoBehaviour {
    public Transform[] kabu;          // 株の 根もと（scale で 伸ばす）
    public Renderer[] hana;           // 咲いた 花
    public Renderer[] tsubomi;        // つぼみ
    /// <summary>水を やった 日かず。-1 なら 日づけから 決める</summary>
    public static int Mizu = -1;

    int mae = -999;

    void OnEnable() { mae = -999; }

    void Update() {
        int d = (Mizu >= 0) ? Mizu : MuraDay.Day;
        if (d == mae) return;
        mae = d;
        float sodachi = Mathf.Clamp01((d + 6f) / 26f);
        bool saku = sodachi > 0.72f;
        if (kabu != null)
            for (int i = 0; i < kabu.Length; i++) {
                if (kabu[i] == null) continue;
                // 株ごとに 少し ずらす（そろって 伸びると 造花に 見える）
                float k = Mathf.Clamp01(sodachi * (0.86f + 0.28f * ((i * 37) % 11) / 10f));
                kabu[i].localScale = new Vector3(0.55f + 0.45f * k, k, 0.55f + 0.45f * k);
            }
        if (hana != null) foreach (var r in hana) if (r != null) r.enabled = saku;
        if (tsubomi != null) foreach (var r in tsubomi) if (r != null) r.enabled = !saku;
    }
}
