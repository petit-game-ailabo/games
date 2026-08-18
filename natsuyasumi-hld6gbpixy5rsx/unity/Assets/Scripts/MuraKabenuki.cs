using UnityEngine;
using System.Collections.Generic;

// カメラと 主人公の あいだに はさまった 物を いったん 消す（箱の村の 保険）。
// **本命は 構図**＝ゾーンカメラを「建物が はさまらない 向き」に 設計する ことで、
// これは その 取りこぼしを ひろう 側（本編では SeeThrough の ディザ抜きが これに あたる）。
// 地めん・丘（G_ / Oka_）は 消さない。
public class MuraKabenuki : MonoBehaviour {
    public Transform target;
    readonly List<Renderer> hidden = new List<Renderer>();

    void LateUpdate() {
        foreach (var r in hidden) if (r != null) r.enabled = true;
        hidden.Clear();
        if (target == null) return;
        var to = target.position + Vector3.up * 0.6f - transform.position;
        var hits = Physics.RaycastAll(transform.position, to.normalized,
                                      Mathf.Max(0f, to.magnitude - 0.6f),
                                      ~0, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits) {
            var n = hit.collider.name;
            if (n.StartsWith("G_") || n.StartsWith("Oka_") || n.StartsWith("BLK_")) continue;
            var r = hit.collider.GetComponent<Renderer>();
            if (r == null) r = hit.collider.GetComponentInChildren<Renderer>();
            if (r != null && r.enabled) { r.enabled = false; hidden.Add(r); }
        }
    }
}
