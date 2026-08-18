using UnityEngine;

// ぼくなつ式の 固定カメラ（S0-2・本人 2026-08-17「一旦カメラ固定を試せないか」）。
// 手で 置いた カメラ位置の どれかに 立ち、主人公が 領域を 出たら **カットで** 切り替わる。
// 位置は 固定・向きだけ 主人公を ゆるやかに 追う（見失い＝破綻を 防ぐ。完全固定に したい
// 見せ場は track=false）。追従や 自動よけは しない＝パッと 動く 気持ちわるさを 消す。
public class MuraCamFixed : MonoBehaviour {
    [System.Serializable]
    public class Spot {
        public string name;
        public Bounds area;          // 主人公が ここに いる あいだ この カメラ
        public Vector3 pos;          // カメラの 置き場所（固定）
        public bool track = true;    // 主人公を 向きで 追うか
        public Vector3 lookOffset;   // 見る 中心の ずらし（構図の 調整しろ）
        public float fov = 46f;
        [HideInInspector] public bool wasIn;
    }
    public Spot[] spots;
    public Transform target;
    public Spot fallback;            // どの 領域にも いない ときの 引きの 絵

    Camera cam; Spot cur; Quaternion wantRot;

    void Start() { cam = GetComponent<Camera>(); }

    Spot Pick() {
        Spot best = null; float bestV = float.MaxValue;
        if (spots != null && target != null)
            foreach (var s in spots) {
                if (s == null) continue;
                var a = s.area;
                if (s.wasIn) a.Expand(new Vector3(1.6f, 0f, 1.6f));   // ふちで ぱたぱた しない
                bool inside = a.Contains(target.position);
                s.wasIn = inside;
                if (!inside) continue;
                float v = s.area.size.x * s.area.size.z;              // せまい ほうが 勝つ
                if (v < bestV) { bestV = v; best = s; }
            }
        return best ?? fallback;
    }

    void LateUpdate() {
        if (target == null) return;
        var s = Pick();
        if (s == null) return;
        bool cut = s != cur;
        cur = s;
        transform.position = s.pos;                                    // 位置は いつも 固定
        if (cam != null) cam.fieldOfView = s.fov;
        var look = target.position + Vector3.up * 0.7f + s.lookOffset - s.pos;
        if (look.sqrMagnitude < 0.01f) return;
        var rot = Quaternion.LookRotation(look);
        if (cut || !s.track) transform.rotation = rot;                 // カットの 瞬間は 一発で
        else transform.rotation = Quaternion.Slerp(transform.rotation, rot,
                                        1f - Mathf.Exp(-3.5f * Time.deltaTime));
        wantRot = rot;
    }
}
