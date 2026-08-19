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

    // ★T で HD-2D追従（オクトパス型＝yaw固定・回転なし・並進だけで 追う）と 切替できる。
    //   カメラの 方向性が まだ 確定して いない ので、両方を 残して 比べる（本人 2026-08-18）
    [Header("HD-2D追従（Tで切替）")]
    public bool hd2d;
    public float hdPitch = 26f, hdDist = 8.5f;

    Camera cam; Spot cur;

    void Start() { cam = GetComponent<Camera>(); }

    void Update() {
        if (Input.GetKeyDown(KeyCode.T)) { hd2d = !hd2d; cur = null; }
    }

    void OnGUI() {
        GUI.Label(new Rect(10, 10, 900, 24),
            "T=カメラ切替 → いま【" + (hd2d ? "HD-2D追従（回転なし）" : "固定カメラのカット割り") + "】"
            + (hd2d || cur == null ? "" : "  カメラ: " + cur.name));
    }

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
        if (hd2d) {
            // HD-2D追従：向きは 固定（yaw=0・北を 見る）。位置だけ なめらかに ついて いく
            var rot = Quaternion.Euler(hdPitch, 0f, 0f);
            var eye = target.position + Vector3.up * 0.7f;
            var back = -(rot * Vector3.forward);
            // ★距離は いつも 一定。挟まったら 寄る（カメラ衝突）は「急に 離れる/寄る」が
            //   嫌だ と 本人評価 → 廃止。めり込みで 裏が 見えるのは、配置フェーズで
            //   「大きすぎる 物を 置かない・裏を 歩かせない」で 解決する（PLAN 配置フェーズ送り）
            var want = eye + back * hdDist;
            transform.position = Vector3.Lerp(transform.position, want,
                                              1f - Mathf.Exp(-6f * Time.deltaTime));
            transform.rotation = rot;
            if (cam != null) cam.fieldOfView = 46f;
            return;
        }
        // ★「舞台（領域）の 中に いて、画面にも 見えて いる」あいだだけ 保つ。
        //   見えて いる だけで 保つと、広く 見える 台では 主人公が 豆つぶに なっても
        //   永遠に 切り替わらない（v1.6の 実測：ツアーの 全スポットが 1台に 張りついた）
        // ★ひき（fallback）は 保持しない。広い 台は どこからでも「見えて いる」ので、
        //   一度 入ると 張りついて めちゃくちゃ 引きの 絵の まま に なる（本人の 報告）
        if (cur != null && cur != fallback) {
            var ca = cur.area; ca.Expand(new Vector3(3f, 0f, 3f));
            if (ca.Contains(target.position) && Edge(cur) < 0.98f) return;
            // 領域の そとでも、まだ 画面の まん中よりに いる うちは 保つ（余白の 救済）
            if (Edge(cur) < 0.55f) return;
        }

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
        if (best == null && spots != null) {
            // ★受け皿：だれも ちゃんと 見えて いなくても、**一番マシに 見える 台**を 必ず えらぶ。
            //   これが 無いと、今の カメラに 張りついた まま 主人公が 画面の そとに 居つづける
            float bestE = float.MaxValue;
            foreach (var s in spots) {
                if (s == null) continue;
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
