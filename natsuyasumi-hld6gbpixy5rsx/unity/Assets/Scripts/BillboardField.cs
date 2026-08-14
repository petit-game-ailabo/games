using UnityEngine;

// 山ぜんぶの 草木を **まとめて** カメラの ほうへ 向ける。
//
// 1本ずつに Billboard を つけると、木が 数千本に なった とたん
// 毎フレーム 数千回の 呼び出しに なる。
// しかも この ゲームの カメラは ふだん 向きが 変わらない（yaw は 固定）ので、
// **向きが 変わった ときだけ 回せば よい。** ふだんの 手間は ほぼ ゼロに なる。
//
// 人や 虫のように 走って いる あいだに 生まれる ものは、これまでどおり
// 1つずつの Billboard を つかう。
public class BillboardField : MonoBehaviour {
    public Transform[] items;

    [Header("カメラの 手前の 木を どける")]
    // **カメラと 主人公の あいだに 木が 立つと 何も 見えなく なる。**
    // 近すぎる 木は 引っこめる。ふつうの ゲームでは すけさせるが、
    // ドット絵の 切りぬきは すけさせると 汚くなるので、消す ほうが きれい
    public float hideWithin = 3.4f;
    public Transform follow;                 // 主人公
    public float checkInterval = 0.1f;

    readonly System.Collections.Generic.List<Renderer> hidden = new System.Collections.Generic.List<Renderer>();
    float checkLeft;

    Quaternion last;
    bool ready;

    void Update() {
        checkLeft -= Time.deltaTime;
        if (checkLeft > 0f) return;
        checkLeft = checkInterval;
        HideNearCamera();
    }

    // カメラの ごく 近くに ある 板を 引っこめる
    void HideNearCamera() {
        var cam = Camera.main;
        if (cam == null || items == null) return;
        for (int i = 0; i < hidden.Count; i++) if (hidden[i] != null) hidden[i].enabled = true;
        hidden.Clear();

        Vector3 c = cam.transform.position;
        float r2 = hideWithin * hideWithin;
        for (int i = 0; i < items.Length; i++) {
            var t = items[i];
            if (t == null) continue;
            var d = t.position - c;
            if (d.sqrMagnitude > r2) continue;
            var r = t.GetComponentInChildren<Renderer>();
            if (r != null && r.enabled) { r.enabled = false; hidden.Add(r); }
        }
    }

    void LateUpdate() {
        var cam = Camera.main;
        if (cam == null || items == null) return;
        var f = cam.transform.forward; f.y = 0f;
        if (f.sqrMagnitude < 0.0001f) return;
        var q = Quaternion.LookRotation(f, Vector3.up);
        // 向きが ほとんど 変わって いないなら 何も しない
        if (ready && Quaternion.Angle(q, last) < 0.05f) return;
        last = q; ready = true;
        for (int i = 0; i < items.Length; i++) {
            var t = items[i];
            if (t != null) t.rotation = q;
        }
    }
}
