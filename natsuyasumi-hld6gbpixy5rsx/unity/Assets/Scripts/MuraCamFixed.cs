using UnityEngine;

// ぼくなつ式の 固定カメラ（S1-1・D-107：**範囲駆動**）。
// ・村を「担当範囲」で 区切り、主人公が いる 範囲の 台が 映す。
// ・範囲に いる あいだは ぜったいに 切り替えない（＋1.5mの ヒステリシス）。
// ・範囲を 移ったら カットで 切替。どの 範囲にも いない 帯は 今の 台を 保つ。
// ・0.35秒などの ヒューリスティックは 保険に 格下げ（枠を 大きく 越えた／隠れつづけた とき
//   だけ 見える 台へ 逃がす）。
// ・**担当範囲⊆画角∧遮蔽なし は MuraCamCheck が 機械検証する**。切替先で キャラが
//   画面外、は 検証で 落とす（起きて いたら 配置の 直し漏れ）。
public class MuraCamFixed : MonoBehaviour {
    [System.Serializable]
    public class Spot {
        public string name;
        public Bounds area;          // 担当範囲（この 中に いる あいだ この 台）
        public Vector3 pos;          // カメラの 置き場所（固定）
        public Vector3 lookAt;       // 見る さき（固定）
        public float fov = 46f;
        // ★屋内の 型（S0-4）：この 台の あいだだけ 消す 物の 名前の 頭（例 "IeKabeN"）。
        //   遮蔽の 判定too 同じ 物を 透かして 数える（Check と 実機が 同じ 答えに なる）
        public string sukashi = "";
    }
    public Spot[] spots;
    public Transform target;
    public Spot fallback;            // 起動直後だけ

    [Header("HD-2D追従（Tで切替・比較用）")]
    public bool hd2d;
    public float hdPitch = 26f, hdDist = 8.5f;

    Camera cam; Spot cur; float lastCut = -9f; float hiddenT;
    public static string CurName = "-";
    public static string PlaceName = "-";

    void Start() { cam = GetComponent<Camera>(); }

    void Update() {
        if (Input.GetKeyDown(KeyCode.T)) { hd2d = !hd2d; cur = null; }
    }

    void OnGUI() {
        GUI.Label(new Rect(10, 8, 1200, 26),
            "ばしょ【" + PlaceName + "】  カメラ【" + (hd2d ? "HD-2D追従" : CurName) + "】" +
            "   T=カメラ方式の切替（いま " + (hd2d ? "HD-2D追従" : "固定カット割り") + "）");
    }

    // ---- 判定（MuraCamCheck からも つかう ので static）----

    /// <summary>その 台から 見た 点の「画面の はしからの 余裕」。0=まん中、1=ふち、>1=そと</summary>
    public static float EdgeFrom(Spot s, Vector3 eye, float aspect) {
        var rot = Quaternion.LookRotation(s.lookAt - s.pos);
        var local = Quaternion.Inverse(rot) * (eye - s.pos);
        if (local.z <= 0.5f) return 99f;
        float tanV = Mathf.Tan(s.fov * 0.5f * Mathf.Deg2Rad);
        float tanH = tanV * aspect;
        return Mathf.Max(Mathf.Abs(local.x / (local.z * tanH)),
                         Mathf.Abs(local.y / (local.z * tanV)));
    }

    /// <summary>台と 点の あいだに 物が はさまって いるか（ignore＝主人公じしん。
    /// s.sukashi の 物は 透かして 数える）</summary>
    public static bool BlockedFrom(Spot s, Vector3 eye, Transform ignore) {
        var d = eye - s.pos;
        var hits = Physics.RaycastAll(s.pos, d.normalized, d.magnitude,
                                      ~0, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits) {
            var t = hit.collider.transform;
            if (ignore != null && (t == ignore || t.IsChildOf(ignore))) continue;
            if (!string.IsNullOrEmpty(s.sukashi) && t.name.StartsWith(s.sukashi)) continue;
            return true;
        }
        return false;
    }

    float Edge(Spot s) {
        return EdgeFrom(s, target.position + Vector3.up * 0.5f, cam != null ? cam.aspect : 1.78f);
    }
    bool Blocked(Spot s) {
        return BlockedFrom(s, target.position + Vector3.up * 0.6f, target);
    }

    Spot ZoneOf(Vector3 p, float expand) {
        Spot best = null; float bestV = float.MaxValue;
        if (spots == null) return null;
        foreach (var s in spots) {
            if (s == null) continue;
            var a = s.area; a.Expand(new Vector3(expand, 0f, expand));
            if (!a.Contains(p)) continue;
            float v = s.area.size.x * s.area.size.z;
            if (v < bestV) { bestV = v; best = s; }
        }
        return best;
    }

    readonly System.Collections.Generic.List<Renderer> sukashiNow =
        new System.Collections.Generic.List<Renderer>();

    void Cut(Spot s) {
        lastCut = Time.time; hiddenT = 0f;
        cur = s;
        CurName = cur.name;
        transform.position = cur.pos;
        transform.rotation = Quaternion.LookRotation(cur.lookAt - cur.pos);
        if (cam != null) cam.fieldOfView = cur.fov;
        // すかし（屋内の 型）：前の 台の ぶんを 戻し、この 台の ぶんを 消す
        foreach (var r in sukashiNow) if (r != null) r.enabled = true;
        sukashiNow.Clear();
        if (!string.IsNullOrEmpty(cur.sukashi))
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if (r.name.StartsWith(cur.sukashi)) { r.enabled = false; sukashiNow.Add(r); }
    }

    void LateUpdate() {
        if (target == null) return;
        var zone = ZoneOf(target.position, 0f);
        PlaceName = zone != null ? zone.name : "（みちくさ）";

        if (hd2d) {
            var rot = Quaternion.Euler(hdPitch, 0f, 0f);
            var eye = target.position + Vector3.up * 0.7f;
            var want = eye - rot * Vector3.forward * hdDist;
            transform.position = Vector3.Lerp(transform.position, want,
                                              1f - Mathf.Exp(-6f * Time.deltaTime));
            transform.rotation = rot;
            if (cam != null) cam.fieldOfView = 46f;
            CurName = "HD-2D";
            return;
        }

        // ---- 範囲駆動（D-107）----
        if (cur == null) { Cut(zone ?? fallback ?? (spots != null && spots.Length > 0 ? spots[0] : null)); return; }

        // いまの 台の 範囲に いる（＋1.5m）＝ぜったいに 保つ
        var ca = cur.area; ca.Expand(new Vector3(1.5f, 0f, 1.5f));
        bool inCur = ca.Contains(target.position);

        // 範囲を 移った ＝ カット（すこし 間を おく。さかい目の 連打よけ）
        if (!inCur && zone != null && zone != cur && Time.time - lastCut > 0.6f) { Cut(zone); return; }

        // 保険（検証ずみなら まず 効かない）：大きく 枠を 越えた／隠れつづけた
        if (Blocked(cur)) hiddenT += Time.deltaTime; else hiddenT = 0f;
        if (Edge(cur) > 1.05f || hiddenT > 0.5f) {
            Spot best = null; float bestD = float.MaxValue;
            foreach (var s in spots) {
                if (s == null) continue;
                if (Edge(s) > 0.9f || Blocked(s)) continue;
                float d = (s.pos - target.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = s; }
            }
            if (best == null) {
                foreach (var s in spots) {
                    if (s == null) continue;
                    float d = (s.pos - target.position).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = s; }
                }
            }
            if (best != null && best != cur) Cut(best);
        }
    }
}
