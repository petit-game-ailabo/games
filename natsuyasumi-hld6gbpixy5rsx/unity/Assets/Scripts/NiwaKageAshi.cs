using UnityEngine;

// 主人公の 足もとの 影（2026-08-30）。
// HD-2Dは 2Dの 絵を 3Dの 地面に 立てる ので、足もとに 影が ないと 浮いて 見える。
// 絵の 板は 影を 落とせない（落とすと 板の 形の 影が 出る）ので、
// **地面を 下に さがして そこへ 敷く**。坂（高台の 坂道）でも ついてくる。
public class NiwaKageAshi : MonoBehaviour {
    public Transform target;
    public float ukiKesu = 1.2f;      // これだけ 浮いたら 消える（跳んだ とき）
    Renderer rend;

    void Start() { rend = GetComponent<Renderer>(); }

    void LateUpdate() {
        if (target == null) return;
        // 自分の あたり判定に ぶつかる ので、当たった ものを えらんで 拾う
        var hits = Physics.RaycastAll(target.position + Vector3.up * 1.2f, Vector3.down,
                                      8f, ~0, QueryTriggerInteraction.Ignore);
        float bestY = float.NegativeInfinity;
        foreach (var h in hits) {
            var t = h.collider.transform;
            if (t == target || t.IsChildOf(target)) continue;
            if (h.point.y > target.position.y + 0.3f) continue;   // 頭の 上の 屋根など
            if (h.point.y > bestY) bestY = h.point.y;
        }
        // 地面が 見つからない ときも 消さない（足もとに そのまま 置く）。
        // ★カメラの ふせ角が 10°と 浅い ので 地面は ひどく つぶれて 映る。
        //   50cm四方の 影でも 画面では たて 6px ほど。小さいと 見えない
        //   （はじめ 0.42x0.30・濃さ0.40で 出して、まったく 見えなかった。
        //    地面さがしは 正常だった＝実測 あたり0.00 足0.06 あたり2件）
        if (float.IsNegativeInfinity(bestY)) bestY = target.position.y;
        transform.position = new Vector3(target.position.x, bestY + 0.045f, target.position.z);
        if (rend != null) {
            rend.enabled = true;
            // 浮くほど 薄く・大きく（跳ぶ しくみが 入った ときの ため）
            float uki = Mathf.Clamp01((target.position.y - bestY) / ukiKesu);
            float s = 1f + uki * 0.6f;
            transform.localScale = new Vector3(s, 1f, s);
        }
    }
}
