using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 被写界深度の ピントを **いつも 主人公に あわせる**。
//
// この 見た目の 肝は「ミニチュアを 覗いている」感じで、それは
// 「ピントの 合う 帯が せまく、その 手前と 奥が とける」ことで 出る。
// ピントを 決めうちの 距離に すると、歩いて 近づいた ときに 主人公まで ぼけてしまう。
[ExecuteAlways]
public class FocusOnPlayer : MonoBehaviour {
    public Volume volume;
    public Transform target;
    public float lag = 8f;              // 追いつく はやさ
    public float bias = 0.15f;          // すこし 奥に ピントを ずらす（足もとより 体の 中心）

    DepthOfField dof;
    float cur = -1f;

    void OnEnable() {
        if (volume == null) volume = FindFirstObjectByType<Volume>();
        if (target == null) {
            var pm = FindFirstObjectByType<PlayerMove>();
            if (pm != null) target = pm.transform;
        }
        if (volume != null && volume.sharedProfile != null)
            volume.sharedProfile.TryGet(out dof);
    }

    void LateUpdate() {
        if (dof == null || target == null) return;
        var cam = Camera.main;
        if (cam == null) return;

        float want = Vector3.Distance(cam.transform.position, target.position + Vector3.up * 0.6f) + bias;
        cur = cur < 0f ? want
            : Mathf.Lerp(cur, want, 1f - Mathf.Exp(-lag * Mathf.Max(Time.deltaTime, 0.0001f)));
        dof.focusDistance.overrideState = true;
        dof.focusDistance.value = cur;
    }
}
