using UnityEngine;

// ドット絵の 板は いつも カメラを 向く。ただし **たてには 傾けない**
// （傾けると 寝ころんで 見えるので、y軸まわりだけ 回す）
[ExecuteAlways]
public class Billboard : MonoBehaviour {
    void LateUpdate() {
        var cam = Camera.main;
        if (cam == null) return;
        var f = cam.transform.forward; f.y = 0f;
        if (f.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(f);
    }
}
