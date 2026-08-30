using UnityEngine;

// 雲（2026-08-30 本人指示「雲は単体で流れるように」「山より奥・もっと高いところ」）。
// カメラに ついてくる 高さ・奥ゆきは そのままに、横だけ ゆっくり ながれて 折りかえす。
public class NiwaKumo : MonoBehaviour {
    public Vector3 zurashi;          // カメラからの ずれ（x は ながれの 起点）
    public float hayasa = 0.35f;     // 横に ながれる 速さ（m/秒）
    public float haba = 260f;        // 一周する はば
    float t;

    void LateUpdate() {
        var cam = Camera.main;
        if (cam == null) return;
        t += Time.deltaTime * hayasa;
        float x = Mathf.Repeat(zurashi.x + t + haba * 0.5f, haba) - haba * 0.5f;
        transform.position = cam.transform.position + new Vector3(x, zurashi.y, zurashi.z);
    }
}
