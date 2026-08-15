using UnityEngine;

// 主人公の まわりだけ、**手前に ある ものに 穴を あける**。
//
// 家の 入口ぎわで カーブミラーや 電柱が カメラに かぶり、主人公が 見えなく なって いた。
// 木は これまで「カメラに 近い ものは まるごと 消す」で 逃げて いたが、
// 物が ぱっと 現れたり 消えたり して 落ちつかない。
// **丸く 抜く**ほうが 見ていて 静かだし、何が そこに あるかも 分かる。
//
// しくみは かんたんで、画面での 主人公の 位置と 半径と 深さを
// シェーダの みんなが 見る 値に 入れるだけ。抜くかどうかは シェーダが 決める
// （Natsuyasumi/PixelSprite の ClipHole）。
[ExecuteAlways]
public class SeeThrough : MonoBehaviour {
    public Transform target;                 // 主人公
    [Tooltip("画面の 高さに たいする 穴の 大きさ")]
    public float radius = 0.13f;
    [Tooltip("主人公の 足もとから どれだけ 上を まん中に するか")]
    public float lift = 0.75f;

    static readonly int HoleId = Shader.PropertyToID("_HoleParams");
    Camera cam;

    void OnEnable() {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (target == null) {
            var pm = FindFirstObjectByType<PlayerMove>();
            if (pm != null) target = pm.transform;
        }
    }

    void OnDisable() { Shader.SetGlobalVector(HoleId, Vector4.zero); }

    void LateUpdate() {
        if (cam == null) cam = Camera.main;
        if (cam == null || target == null) { Shader.SetGlobalVector(HoleId, Vector4.zero); return; }

        var at = target.position + Vector3.up * lift;
        var vp = cam.WorldToViewportPoint(at);
        if (vp.z <= 0f) { Shader.SetGlobalVector(HoleId, Vector4.zero); return; }   // うしろは 抜かない

        Shader.SetGlobalVector(HoleId, new Vector4(vp.x, vp.y, radius, vp.z));
    }
}
