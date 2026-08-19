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

    Vector3 simDir;                      // 再現あるき（-repro）が 入力の かわりに 入れる

    void Start() {
        cc = GetComponent<CharacterController>();
        foreach (var a in System.Environment.GetCommandLineArgs()) {
            if (a == "-tour") { StartCoroutine(Tour()); break; }
            if (a == "-repro") { StartCoroutine(Repro()); break; }
        }
    }

    /// <summary>本人の 報告を **歩いて** 再現する（テレポートの ツアーでは 出ない ものが ある）。
    /// 庭 → 家の 東わき → 家の 裏を 西へ → 北へ 戻る。カメラ名を 0.2秒ごとに ログ</summary>
    System.Collections.IEnumerator Repro() {
        string dir = Path.Combine(Path.GetTempPath(), "natsuyasumi", "mura");
        Directory.CreateDirectory(dir);
        var log = new System.Text.StringBuilder();
        yield return new WaitForSeconds(1.0f);
        var steps = new[] {                                   // (向きx, 向きz, 秒)
            new Vector3(0, -1, 3.5f),   // 庭から 南へ（家の 東わき）
            new Vector3(-1, -1, 2.0f),  // 南西へ（家の 裏の 口）
            new Vector3(-1, 0, 6.5f),   // 家の 裏すじを 西へ
            new Vector3(0, 1, 3.0f),    // 北へ 戻る
            new Vector3(-1, 0, 4.0f),   // 道を 西へ（引きに 捕まらないか）
        };
        int shot = 0; float tAll = 0f, tick = 0f;
        foreach (var s in steps) {
            float t = 0f;
            while (t < s.z) {
                simDir = new Vector3(s.x, 0f, s.y).normalized;
                t += Time.deltaTime; tAll += Time.deltaTime; tick += Time.deltaTime;
                if (tick > 0.4f) {
                    tick = 0f;
                    var fx = cam != null ? cam.GetComponent<MuraCamFixed>() : null;
                    log.AppendLine(tAll.ToString("F1") + "s  cam=" + MuraCamFixed.CurName +
                                   "  pos=" + transform.position.ToString("F1") +
                                   "  | " + (fx != null ? fx.DebugEdges() : ""));
                }
                yield return null;
            }
            simDir = Vector3.zero;
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, "repro" + (shot++).ToString("00") + ".png"));
            yield return new WaitForSeconds(0.4f);
        }
        File.WriteAllText(Path.Combine(dir, "camlog.txt"), log.ToString());
        yield return new WaitForSeconds(0.5f);
        Application.Quit();
    }

    void Update() {
        float h = Input.GetAxisRaw("Horizontal"), v = Input.GetAxisRaw("Vertical");
        bool any = Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f;
        // 入力の 組み合わせが 変わったら、いまの カメラで 基準を 取り直す
        if (any && (h != prevH || v != prevV) && cam != null) baseYaw = cam.eulerAngles.y;
        prevH = h; prevV = v;
        var dir = Quaternion.Euler(0f, baseYaw, 0f) * new Vector3(h, 0f, v);
        if (simDir != Vector3.zero) dir = simDir;   // 再現あるきは 世界の 向きで まっすぐ
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
