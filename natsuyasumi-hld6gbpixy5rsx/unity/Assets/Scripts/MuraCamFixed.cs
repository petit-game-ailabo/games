using UnityEngine;

// ぼくなつ式の 固定カメラ（S0-2 v1.4）。
// ★**完全固定**：位置も 向きも 動かない（v1.3の「向きだけ 追う」は 気持ちわるい と
//   本人評価 → 廃止）。構図は「領域の 見どころを 向けて 置く」で 作る。
// ★**保持**：主人公が どの 領域にも いない あいだは **今の カメラを 保ちつづける**。
//   v1.3は 領域の すきまで 即「引きの 俯瞰」に 落ちて、走ると カメラが コロコロ
//   切り替わって いた。超俯瞰は 山頂など 意図した 場面だけに する。
// 切替の 位置・タイミングの 細かい 調整は 絵と 配置が 決まる フェーズの 課題（台帳に 記録）。
public class MuraCamFixed : MonoBehaviour {
    [System.Serializable]
    public class Spot {
        public string name;
        public Bounds area;          // 主人公が ここに 入ったら この カメラ
        public Vector3 pos;          // カメラの 置き場所（固定）
        public Vector3 lookAt;       // 見る さき（固定。構図は これで 決める）
        public float fov = 46f;
        [HideInInspector] public bool wasIn;
    }
    public Spot[] spots;
    public Transform target;
    public Spot fallback;            // まだ どの 領域にも 入って いない 起動直後だけ

    Camera cam; Spot cur;

    void Start() { cam = GetComponent<Camera>(); }

    void LateUpdate() {
        if (target == null) return;
        Spot best = null; float bestV = float.MaxValue;
        if (spots != null)
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
        // 領域の そとでは 切り替えない（今のを 保つ）
        if (best == null) { if (cur == null) best = fallback; else return; }
        if (best == cur || best == null) return;
        cur = best;
        transform.position = cur.pos;
        transform.rotation = Quaternion.LookRotation(cur.lookAt - cur.pos);
        if (cam != null) cam.fieldOfView = cur.fov;
    }
}
