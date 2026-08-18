using UnityEngine;

// ぼくなつ式の 固定カメラ（S0-2 v1.5）。位置も 向きも 完全に 固定。
// ★切替の 条件は「主人公が **今の カメラの 画面から 出た とき**」（D-105）。
//   領域の 出入りで 切ると、余白で どこに いるか 分からなく なり、
//   いつ 切り替わるかも 読めない（本人の 指摘）。画面から 出た 瞬間なら 必ず 予測できる。
// 新しい カメラは ①主人公が 領域に 入って いる もの（せまい 勝ち）
// ②無ければ 主人公が いちばん 画面の まん中に 来る もの。
// 出口を 塀で 絞る・家の 裏の 死角で 変える などは 配置フェーズの 課題（D-105）。
public class MuraCamFixed : MonoBehaviour {
    [System.Serializable]
    public class Spot {
        public string name;
        public Bounds area;          // この 領域に いる あいだは この カメラが 第一候補
        public Vector3 pos;          // カメラの 置き場所（固定）
        public Vector3 lookAt;       // 見る さき（固定。構図は これで 決める）
        public float fov = 46f;
    }
    public Spot[] spots;
    public Transform target;
    public Spot fallback;            // 起動直後だけ

    Camera cam; Spot cur;

    void Start() { cam = GetComponent<Camera>(); }

    // その カメラから 見た 主人公の「画面の はしからの 余裕」。0=まん中、1=ふち
    float Edge(Spot s) {
        var rot = Quaternion.LookRotation(s.lookAt - s.pos);
        var local = Quaternion.Inverse(rot) * (target.position + Vector3.up * 0.5f - s.pos);
        if (local.z <= 0.5f) return 99f;                       // うしろ・近すぎ
        float tanV = Mathf.Tan(s.fov * 0.5f * Mathf.Deg2Rad);
        float tanH = tanV * (cam != null ? cam.aspect : 1.78f);
        return Mathf.Max(Mathf.Abs(local.x / (local.z * tanH)),
                         Mathf.Abs(local.y / (local.z * tanV)));
    }

    void LateUpdate() {
        if (target == null) return;
        // 見えて いる あいだは ぜったいに 切り替えない
        if (cur != null && Edge(cur) < 0.98f) return;

        Spot best = null; float bestV = float.MaxValue;
        if (spots != null)
            foreach (var s in spots) {
                if (s == null || s == cur) continue;
                if (!s.area.Contains(target.position)) continue;
                if (Edge(s) > 0.85f) continue;                 // 入っても ふちなら 選ばない
                float v = s.area.size.x * s.area.size.z;
                if (v < bestV) { bestV = v; best = s; }
            }
        if (best == null && spots != null) {                   // 領域の そと＝一番よく 見える 台
            float bestE = 0.85f;
            foreach (var s in spots) {
                if (s == null || s == cur) continue;
                float e = Edge(s);
                if (e < bestE) { bestE = e; best = s; }
            }
        }
        if (best == null) { if (cur == null) best = fallback; else return; }
        if (best == null || best == cur) return;
        cur = best;
        transform.position = cur.pos;
        transform.rotation = Quaternion.LookRotation(cur.lookAt - cur.pos);
        if (cam != null) cam.fieldOfView = cur.fov;
    }
}
