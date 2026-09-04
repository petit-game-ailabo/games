using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 納屋の 屋内カメラ（2026-09-05）。**屋内の 見せかたの 1つめ**。
//
// ★本人「納屋の中に入ったら、外の景色は一気に暗くして視点が納屋の中だけになるのがいいかな。
//   そのうえで、納屋の手前の壁とかが消えて、納屋の中だけ見えるとかどう？」
//
// やって いる ことは 3つ。どれか 1つでは 屋内に ならない。
//   1. **カメラを 引きとる**（`MuraCamFixed.Suspended`）。追従カメラは 主人公の 15m 南に つく ので、
//      小屋に 入ると **カメラが 壁の 中**に 入る。壁は ニアクリップの 向こうで 描かれず、
//      画面が 家の 中の べつの 面で うまる（natsuyasumi スキル「カメラが 物に じゃまされる とき」）。
//      → 小屋の 外・南に 台を 決めうちして、そこから 中を のぞく。
//   2. **手前の 壁と 屋根を 消す**（`kesu`）。1 だけだと 南の 壁で 何も 見えない。
//   3. **外を 落とす**（露出を 下げる ＋ ふちを 暗く ＋ 中に あかりを つける）。
//      2 だけだと「屋根を はずした 模型」に 見える。外が 沈んで はじめて 中に 入った ことに なる。
//      露出は 画面ぜんぶに かかる ので、**中の あかりで 小屋だけ 押しもどす**のが 肝。
//
// 出入りは 0.3秒で 混ぜる。切りかえを 一瞬に すると 戸口を かすめる たびに 画面が 明滅する。
public class NiwaNayaNaka : MonoBehaviour {
    public Transform target;             // 主人公
    public Transform cam;                // Main Camera
    public MuraCamFixed fix;
    public Volume vol;
    public Light akari;                  // 中の あかり（外に いる あいだは 0）
    public Renderer[] kesu;              // 中に いる あいだ 消す（南の 壁・屋根・妻・棟）

    [Header("中の 台")]
    public Bounds naka;                  // ここに 主人公が 入ったら 屋内
    public Vector3 camPos, camLook;
    public float camFov = 33f;

    [Header("外の 落としかた")]
    public float kurasa = -1.05f;        // 足す 露出（EV。-1.05 ≒ 明るさ 0.48ばい）
    public float vinet = 0.54f;          // ふちの 暗さ
    public float akarusa = 4.6f;         // 中の あかりの 強さ
    public float magari = 0.30f;         // 出入りに かける 秒

    float t;                             // 0=外 1=中
    bool osaeta, keshita;
    Vignette vig; ColorAdjustments ca;
    float vig0, exp0; bool fxAru;
    Camera camc;

    void Start() {
        camc = cam != null ? cam.GetComponent<Camera>() : null;
        if (vol != null && vol.sharedProfile != null) {
            // FocusOnPlayer と 同じく sharedProfile を さわる（片方だけ instance に すると 食いちがう）
            vol.sharedProfile.TryGet(out vig);
            vol.sharedProfile.TryGet(out ca);
        }
        if (vig != null && ca != null) {
            vig0 = vig.intensity.value; exp0 = ca.postExposure.value; fxAru = true;
        }
        Miseru(true);
        if (akari != null) akari.intensity = 0f;
        Debug.Log("[Probe] NiwaNayaNaka vol=" + (vol != null)
                  + " prof=" + (vol != null && vol.sharedProfile != null)
                  + " vig=" + (vig != null) + " ca=" + (ca != null)
                  + " kesu=" + (kesu != null ? kesu.Length : -1));
    }

    void OnDisable() { Modosu(); }

    void Modosu() {
        if (fxAru) { vig.intensity.value = vig0; ca.postExposure.value = exp0; }
        if (osaeta) { MuraCamFixed.Suspended = false; osaeta = false; }
        Miseru(true);
        if (akari != null) akari.intensity = 0f;
    }

    void Miseru(bool v) {
        if (keshita == !v || kesu == null) return;
        keshita = !v;
        foreach (var r in kesu) if (r != null) r.enabled = v;
    }

    void LateUpdate() {
        if (target == null || cam == null) return;
        // 俯瞰（-fukan）は カメラを 正射影に して 自分で 動かす。じゃま しない
        if (camc != null && camc.orthographic) return;

        bool uchi = naka.Contains(target.position);
        t = Mathf.MoveTowards(t, uchi ? 1f : 0f, Time.deltaTime / Mathf.Max(0.05f, magari));

        // 壁は **早めに 消す**（カメラが 寄りきる 前に 消えて いないと 一瞬 壁で うまる）
        Miseru(t < 0.10f);
        if (akari != null) akari.intensity = akarusa * t;
        if (fxAru) {
            vig.intensity.value = Mathf.Lerp(vig0, vinet, t);
            ca.postExposure.value = Mathf.Lerp(exp0, exp0 + kurasa, t);
        }

        if (t > 0f) {
            if (!osaeta) { MuraCamFixed.Suspended = true; osaeta = true; }
            float k = 1f - Mathf.Exp(-8f * Time.deltaTime);
            cam.position = Vector3.Lerp(cam.position, camPos, k);
            cam.rotation = Quaternion.Slerp(cam.rotation,
                                            Quaternion.LookRotation(camLook - camPos), k);
            if (camc != null) camc.fieldOfView = Mathf.Lerp(camc.fieldOfView, camFov, k);
        } else if (osaeta) {
            // t が 0 に なった＝外。追従カメラに 返す（あちらが 6/秒で もどす）
            MuraCamFixed.Suspended = false; osaeta = false;
        }
    }
}
