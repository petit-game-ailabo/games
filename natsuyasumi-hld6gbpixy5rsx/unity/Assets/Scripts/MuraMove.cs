using UnityEngine;
using System.IO;

// 箱の村（S0）を あるく ための 足。寸法と 速さは 本編と 同じ（D-100）。
// 走りが 基本・Shift で 歩き（本人 2026-08-17）。
// ★移動は **押しはじめラッチ**（カット切替の 固定カメラ用＝バイオHD式・調査で 確認済み）：
//   キーの 組み合わせが 変わった 瞬間に そのときの カメラ基準で 方向を 取り直す。
//   押しっぱなしの あいだは カメラが カットで 変わっても 進む 向きを 変えない。
//   （W→W+A の ように 足した 瞬間も「新しい 意図」なので 取り直す）
public class MuraMove : MonoBehaviour {
    public float walk = 2.6f, run = 4.4f;
    public Transform cam;
    public Transform[] tour;
    public CharSprite sprite;            // 8方向スプライト（S0-3。空なら 何も しない）
    CharacterController cc; float vy;
    float baseYaw; float prevH, prevV;

    void Start() {
        cc = GetComponent<CharacterController>();
        foreach (var a in System.Environment.GetCommandLineArgs())
            if (a == "-tour") { StartCoroutine(Tour()); break; }
    }

    void Update() {
        float h = Input.GetAxisRaw("Horizontal"), v = Input.GetAxisRaw("Vertical");
        bool any = Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f;
        // 入力の 組み合わせが 変わったら、いまの カメラで 基準を 取り直す
        if (any && (h != prevH || v != prevV) && cam != null) baseYaw = cam.eulerAngles.y;
        prevH = h; prevV = v;
        var dir = Quaternion.Euler(0f, baseYaw, 0f) * new Vector3(h, 0f, v);
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        float spd = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? walk : run;
        vy = cc.isGrounded ? -0.5f : vy - 9.8f * Time.deltaTime;
        cc.Move((dir * spd + Vector3.up * vy) * Time.deltaTime);
        if (sprite != null) sprite.Drive(dir, dir.magnitude * spd, false);
    }

    System.Collections.IEnumerator Tour() {
        string dir = Path.Combine(Path.GetTempPath(), "natsuyasumi", "mura");
        Directory.CreateDirectory(dir);
        yield return new WaitForSeconds(1.5f);
        for (int i = 0; i < tour.Length; i++) {
            cc.enabled = false;
            transform.position = tour[i].position + Vector3.up * 0.1f;
            Physics.SyncTransforms();
            cc.enabled = true;
            yield return new WaitForSeconds(1.4f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, "mise" + i.ToString("00") + ".png"));
            yield return new WaitForSeconds(0.5f);
        }
        yield return new WaitForSeconds(0.8f);
        Application.Quit();
    }
}
