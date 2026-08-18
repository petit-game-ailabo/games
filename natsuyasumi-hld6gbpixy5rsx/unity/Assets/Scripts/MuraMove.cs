using UnityEngine;
using System.IO;

// 箱の村（R2）を あるく ための かんたんな 足。寸法と 速さは 本編と 同じ（D-100）。
// 起動引数 -tour で 見せ場を 順に まわって スクショを 撮り、閉じる。
public class MuraMove : MonoBehaviour {
    public float walk = 2.6f, run = 4.4f;
    public Transform[] tour;
    CharacterController cc; float vy;

    void Start() {
        cc = GetComponent<CharacterController>();
        foreach (var a in System.Environment.GetCommandLineArgs())
            if (a == "-tour") { StartCoroutine(Tour()); break; }
    }

    void Update() {
        float h = Input.GetAxisRaw("Horizontal"), v = Input.GetAxisRaw("Vertical");
        var dir = new Vector3(h, 0f, v);
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        float spd = Input.GetKey(KeyCode.LeftShift) ? run : walk;
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
            yield return new WaitForSeconds(1.4f);   // カメラの 追従と ゾーンの 移り待ち
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, "mise" + i.ToString("00") + ".png"));
            yield return new WaitForSeconds(0.5f);
        }
        yield return new WaitForSeconds(0.8f);
        Application.Quit();
    }
}
