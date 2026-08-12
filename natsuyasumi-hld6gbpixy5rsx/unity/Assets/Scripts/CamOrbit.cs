using UnityEngine;

// 手ざわりを 見るための カメラ。実行中に 角度と 寄りを かえられる。
// **数値は インスペクタに 出してある**ので、GUIで さわって 好みを 決められる。
public class CamOrbit : MonoBehaviour {
    [Header("見る 中心")]
    public Vector3 target = new Vector3(0f, 0.9f, 0.4f);
    [Tooltip("これを 入れると その人を 追いかける")]
    public Transform follow;
    public Vector3 followOffset = new Vector3(0f, 0.7f, 0f);
    public float followLag = 6f;        // 大きいほど きびきび ついていく
    [Header("角度（度）")]
    [Range(8f, 70f)]  public float pitch = 28f;    // 見おろす 角度。30度前後が それらしい
    // yaw=180 で「部屋の 手前がわ（+Z）から 見る」。0 だと 裏に まわりこむ
    [Range(0f, 360f)] public float yaw = 180f;
    [Header("よりかた")]
    [Range(3f, 20f)] public float distance = 8.2f;
    public bool allowMouse = true;

    void OnEnable()  { Apply(); }
    void OnValidate(){ Apply(); }

    void Update() {
        if (allowMouse && Application.isPlaying) {
            if (Input.GetMouseButton(1)) {                    // 右ドラッグで まわす
                yaw   += Input.GetAxis("Mouse X") * 2.2f;
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 1.6f, 8f, 70f);
            }
            float w = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(w) > 0.0001f) distance = Mathf.Clamp(distance - w * 6f, 3f, 20f);
        }
        Apply();
    }

    void Apply() {
        if (follow != null) {
            var want = follow.position + followOffset;
            target = Application.isPlaying
                ? Vector3.Lerp(target, want, 1f - Mathf.Exp(-followLag * Time.deltaTime))
                : want;
        }
        var rot = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = target - (rot * Vector3.forward) * distance;
        transform.rotation = rot;
    }
}
