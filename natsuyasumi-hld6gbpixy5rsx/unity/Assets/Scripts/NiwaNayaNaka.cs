using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 納屋の 出入りと 屋内の 見せかた（2026-09-05）。**屋内の 型の 1つめ**。
//
// ★本人「納屋の中に入ったら、外の景色は一気に暗くして視点が納屋の中だけになるのがいいかな。
//   そのうえで、納屋の手前の壁とかが消えて、納屋の中だけ見えるとかどう？」
// ★本人「納屋の扉が閉まっている状態で、近くでスペースキー押したら扉が開いて、
//   操作しなくてもそのまま自動で納屋の中へ」
//
// 見せかたは 3つで 1組。どれか 1つでは 屋内に ならない。
//   1. **カメラを 引きとる**（`MuraCamFixed.Suspended`）。追従カメラは 主人公の 15m 南に つく ので、
//      小屋に 入ると **カメラが 壁の 中**に 入り、壁は ニアクリップの 向こうで 描かれず 画面が こわれる
//      （natsuyasumi スキル「カメラが 物に じゃまされる とき」）。小屋の 南の 外に 台を 決めうち。
//   2. **手前の 壁と 屋根を 消す**（`kesu`）。1 だけだと 南の 壁で 何も 見えない。
//   3. **外を 落とす**（露出 ＋ ヴィネット ＋ 中の あかり）。2 だけだと「屋根を はずした 模型」。
//      露出は 画面ぜんぶに かかる ので、**中の あかりで 小屋だけ 押しもどす**のが 肝。
//
// 戸は **近づいたら ひとりでに 開く**（2026-09-05・本人「納屋の入り口でスペースキー押して
// 扉開けるのめんどくさいから無くして」）。スペースを 出入りに 使うと、道具を 取る／虫を つかまえる
// と ならんで しまい「いま 何が 起きるか」が 読めなく なる。
// **開いた 戸口は 0.86m** ある ので 自分で 歩いて 通れる（0.72m だと 引っかかった）。
// 中の 帯（BLK_NayaOku）も 戸口の 手前で 切って あり、まっすぐ 入れる。
// 見た目の 混ぜは 0.30秒（一瞬に すると 戸口を かすめる たびに 画面が 明滅する）。
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

    [Header("戸")]
    public Transform toB;                // 引く ほうの 戸（もう 1枚は 動かない）
    public float toAke = -0.72f;         // 開けきった ときの localPosition.z
    public Collider toKabe;              // 閉じて いる あいだ 通れなく する かべ
    public Vector3 soto;                 // 戸口の 外の 立ち位置（world）
    public float sotoTodoku = 1.9f;      // ここまで 近づいたら 戸が 開く

    [Header("外の 落としかた")]
    public float kurasa = -1.05f;        // 足す 露出（EV）
    public float vinet = 0.54f;          // ふちの 暗さ
    public float akarusa = 4.6f;         // 中の あかりの 強さ
    public float magari = 0.30f;         // 出入りに かける 秒

    float t;                             // 0=外 1=中
    float aki;                           // 戸の 開きぐあい 0..1
    float toShimeZ;
    bool osaeta, keshita;
    Vignette vig; ColorAdjustments ca;
    float vig0, exp0; bool fxAru;
    Camera camc;

    /// <summary>いま 中に いる か（道具を 取れるかの 判定に つかう）</summary>
    public bool Uchi { get { return target != null && naka.Contains(target.position); } }

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
        if (toB != null) toShimeZ = toB.localPosition.z;
        Miseru(true);
        if (akari != null) akari.intensity = 0f;
        Debug.Log("[Probe] NiwaNayaNaka vig=" + (vig != null) + " ca=" + (ca != null)
                  + " kesu=" + (kesu != null ? kesu.Length : -1)
                  + " to=" + (toB != null) + " toKabe=" + (toKabe != null));
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

    void Update() {
        if (target == null) return;
        // ---- 戸：中に いる か、戸口の 前に 来たら 開く。離れたら 閉まる
        bool akeru = Uchi || Vector3.Distance(target.position, soto) < sotoTodoku;
        aki = Mathf.MoveTowards(aki, akeru ? 1f : 0f, Time.deltaTime / 0.32f);
        if (toB != null) {
            var lp = toB.localPosition;
            lp.z = Mathf.Lerp(toShimeZ, toAke, aki);
            toB.localPosition = lp;
        }
        // ★かべを 切るのは **開ききる 前**に する（歩きながら 近づくので、
        //   開ききってから 切ると 一度 戸口に ぶつかって 足が 止まる）
        if (toKabe != null) toKabe.enabled = aki < 0.35f;
    }

    void LateUpdate() {
        if (target == null || cam == null) return;
        // 俯瞰（-fukan）は カメラを 正射影に して 自分で 動かす。じゃま しない
        if (camc != null && camc.orthographic) return;

        t = Mathf.MoveTowards(t, Uchi ? 1f : 0f, Time.deltaTime / Mathf.Max(0.05f, magari));

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
