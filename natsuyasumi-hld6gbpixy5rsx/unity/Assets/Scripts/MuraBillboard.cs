using UnityEngine;

// スプライトの 板を カメラへ 向ける（yawだけ。倒しては いけない＝足もとが 浮く）
public class MuraBillboard : MonoBehaviour {
    void LateUpdate() {
        var c = Camera.main;
        if (c == null) return;
        transform.rotation = Quaternion.Euler(0f, c.transform.eulerAngles.y, 0f);
    }
}
