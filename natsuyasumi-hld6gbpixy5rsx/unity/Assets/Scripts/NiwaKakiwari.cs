using UnityEngine;

// 描き割りの 遠景を カメラに 連動させる（2026-08-30）。
// 追従カメラでは 置きっぱなしの 遠景は 画角の 都合で 映らない／ずれる。
// カメラから 一定の ずれに 浮かせて おけば、どこを 歩いても 画面の 上の 帯に
// 同じ 山なみが 出る（横スクロールの 遠景パララックスと 同じ 考えかた）。
public class NiwaKakiwari : MonoBehaviour {
    public Vector3 zurashi;          // カメラ位置からの ずれ（world）

    void LateUpdate() {
        var cam = Camera.main;
        if (cam == null) return;
        transform.position = cam.transform.position + zurashi;
    }
}
