using UnityEngine;

// 描き割りの 遠景を カメラに 連動させる（2026-08-30）。
// 追従カメラでは 置きっぱなしの 遠景は 画角の 都合で 映らない／ずれる。
// カメラから 一定の ずれに 浮かせて おけば、どこを 歩いても 画面の 上の 帯に
// 同じ 山なみが 出る（横スクロールの 遠景パララックスと 同じ 考えかた）。
public class NiwaKakiwari : MonoBehaviour {
    public Vector3 zurashi;          // カメラ位置からの ずれ（world）
    // ★この 場所に いる ときだけ 見せる（空きなら いつでも）。
    //   例：遠くの 青い 峰は **高台に 登った ときだけ** 見える（平地から 見えるのは おかしい・本人 2026-08-30）
    public string onlyZone = "";
    Renderer[] rs;

    void Start() { rs = GetComponentsInChildren<Renderer>(); }

    void LateUpdate() {
        var cam = Camera.main;
        if (cam == null) return;
        transform.position = cam.transform.position + zurashi;
        if (!string.IsNullOrEmpty(onlyZone) && rs != null) {
            bool mieru = MuraCamFixed.PlaceName == onlyZone;
            foreach (var r in rs) if (r != null) r.enabled = mieru;
        }
    }
}
