using UnityEngine;
using System.IO;

// 箱の村（R2）を あるく ための 足。寸法と 速さは 本編と 同じ（D-100）。
// ★走りが 基本（本人 2026-08-17）。Shift を おして いる あいだだけ 歩き。
// ★移動は カメラ基準＋押しはじめラッチ：
//   押した 瞬間の カメラの 向きで 方向を 決め、押して いる あいだは カメラが 回っても
//   進む 向きを 変えない。離して 押し直すと そのときの カメラ基準に 取り直す
//   （bokunatsu-design スキルの 入力ラッチ。「次の 操作で 向きが 合わない」への 答え）。
// 起動引数 -tour で 見せ場を 順に まわって スクショを 撮り、閉じる。
public class MuraMove : MonoBehaviour {
    public float walk = 2.6f, run = 4.4f;
    public Transform cam;
    public Transform[] tour;
    CharacterController cc; float vy;
    float baseYaw; bool held;

    void Start() {
        cc = GetComponent<CharacterController>();
        foreach (var a in System.Environment.GetCommandLineArgs())
            if (a == "-tour") { StartCoroutine(Tour()); break; }
    }

    void Update() {
        float h = Input.GetAxisRaw("Horizontal"), v = Input.GetAxisRaw("Vertical");
        bool any = Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f;
        if (any && !held && cam != null) baseYaw = cam.eulerAngles.y;   // 押しはじめに 決める
        held = any;
        var dir = Quaternion.Euler(0f, baseYaw, 0f) * new Vector3(h, 0f, v);
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        float spd = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? walk : run;
        vy = cc.isGrounded ? -0.5f : vy - 9.8f * Time.deltaTime;
        cc.Move((dir * spd + Vector3.up * vy) * Time.deltaTime);
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
