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
    // 高台に のぼれるかの 機械検査（本人 2026-08-31「高台が歩いていけない」）。
    // 目で 見て 気づく まえに 数字で 落とす
    public Vector3 noboruKara;
    public float noboruMade;
    public CharSprite sprite;            // 8方向スプライト（S0-3。空なら 何も しない）
    CharacterController cc; float vy;
    float baseYaw; float prevH, prevV;

    Vector3 simDir;                      // 再現あるき（-repro）が 入力の かわりに 入れる

    void Start() {
        cc = GetComponent<CharacterController>();
        foreach (var a in System.Environment.GetCommandLineArgs()) {
            if (a == "-tour") { StartCoroutine(Tour()); break; }
            if (a == "-noboru") { StartCoroutine(Noboru()); break; }
            if (a == "-repro") { StartCoroutine(Repro()); break; }
        }
        // ★-at "x,z[,y]" で 主人公を そこへ 飛ばす（2026-09-05）。
        //   AutoShot の -at は PlayerMove しか 見て いない ので、庭（MuraMove）では
        //   だまって 効かず、**どこを 撮っても 同じ 絵**に なって いた
        var av = System.Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < av.Length; i++)
            if (av[i] == "-at") { StartCoroutine(Tobu(av[i + 1])); break; }
        // ★-fukan [size] で 俯瞰図を 開いた まま 撮る（2026-09-05）。
        //   追従カメラは いつも 南から 北を 向く ので、**家の 裏は どうやっても 撮れない**。
        //   囲い（生垣・石垣）や 物の 置き場所の 確かめは 俯瞰で やる
        for (int i = 0; i < av.Length; i++)
            if (av[i] == "-fukan") {
                StartCoroutine(Fukan(i + 1 < av.Length ? av[i + 1] : null));
                break;
            }
    }

    System.Collections.IEnumerator Fukan(string ookisa) {
        for (int i = 0; i < 20; i++) yield return null;      // -at の 移動を 先に 効かせる
        if (cam == null) yield break;
        float sz = 30f;
        if (!string.IsNullOrEmpty(ookisa)) float.TryParse(ookisa, out sz);
        sz = Mathf.Clamp(sz, 4f, 200f);
        var fk = cam.GetComponent<MuraFukan>();
        if (fk != null) {                                    // 村には 俯瞰エディタが ある
            fk.size = sz; fk.height = Mathf.Max(sz + 12f, 40f);
            fk.Set(true);
        } else {
            // 庭には MuraFukan が 無い ので、撮影の あいだだけ カメラを 真上から の
            // 正射影に する。**囲いや 置き場所は これでしか 確かめられない**
            //   （追従カメラは いつも 南から 北。家の 裏は 一生 写らない）
            MuraCamFixed.Suspended = true;
            var c = cam.GetComponent<Camera>();
            if (c != null) {
                c.orthographic = true;
                c.orthographicSize = sz;
                c.farClipPlane = Mathf.Max(c.farClipPlane, 300f);
                var uac = c.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if (uac != null) uac.renderPostProcessing = false;   // 深度ぼかしで 全面 ぼける
            }
            RenderSettings.fog = false;
            cam.position = new Vector3(0f, 90f, 4f);
            cam.rotation = Quaternion.Euler(90f, 0f, 0f);    // 画面の 上が 北
        }
        Debug.Log("[MuraMove] -fukan size=" + sz + (fk != null ? " (MuraFukan)" : " (ortho)"));
    }

    /// <summary>撮影用の テレポート。地めんの 高さは 上から レイを 落として 拾う。
    /// **層2（屋根・見えない かべ）は 拾わない**（屋根の 上に 立った 絵に なる）</summary>
    System.Collections.IEnumerator Tobu(string at) {
        yield return null;
        var q = at.Split(',');
        if (q.Length < 2) yield break;
        float ax, az;
        if (!float.TryParse(q[0], out ax) || !float.TryParse(q[1], out az)) yield break;
        var to = new Vector3(ax, 0f, az);
        float y;
        if (q.Length > 2 && float.TryParse(q[2], out y)) to.y = y;
        else {
            RaycastHit gh;
            to.y = Physics.Raycast(new Vector3(ax, 80f, az), Vector3.down, out gh, 200f,
                                   ~(1 << 2), QueryTriggerInteraction.Ignore)
                 ? gh.point.y + 0.1f : 1f;
        }
        cc.enabled = false;
        transform.position = to;
        Physics.SyncTransforms();
        cc.enabled = true;
        Debug.Log("[MuraMove] -at " + to.ToString("F2"));
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
                if (tick > 0.2f) {
                    tick = 0f;
                    log.AppendLine(tAll.ToString("F1") + "s  cam=" + MuraCamFixed.CurName +
                                   "  ばしょ=" + MuraCamFixed.PlaceName +
                                   "  pos=" + transform.position.ToString("F1"));
                }
                yield return null;
            }
            simDir = Vector3.zero;
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, "repro" + (shot++).ToString("00") + ".png"));
            yield return new WaitForSeconds(0.4f);
        }
        File.WriteAllText(Path.Combine(dir, "camlog.txt"), log.ToString());
        // さいごに 俯瞰エディタを 開いて 1枚（見取り図の 確認用）
        var fk = cam != null ? cam.GetComponent<MuraFukan>() : null;
        if (fk != null) {
            fk.Set(true);
            yield return new WaitForSeconds(1.0f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, "fukan.png"));
            yield return new WaitForSeconds(0.5f);
        }
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

    System.Collections.IEnumerator Noboru() {
        string dir = Path.Combine(Path.GetTempPath(), "natsuyasumi", "mura");
        Directory.CreateDirectory(dir);
        yield return new WaitForSeconds(1.2f);
        cc.enabled = false;
        transform.position = noboruKara;
        Physics.SyncTransforms();
        cc.enabled = true;
        yield return new WaitForSeconds(0.4f);
        float t = 0f, best = transform.position.y;
        while (t < 12f) {
            simDir = Vector3.forward;                    // 北へ まっすぐ
            t += Time.deltaTime;
            if (transform.position.y > best) best = transform.position.y;
            yield return null;
        }
        simDir = Vector3.zero;
        yield return new WaitForSeconds(0.4f);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir, "noboru.png"));
        yield return new WaitForSeconds(0.6f);
        string kekka = (best >= noboruMade - 0.35f) ? "OK" : "NG";
        File.WriteAllText(Path.Combine(dir, "noboru.txt"),
            "takadai=" + noboruMade.ToString("F2") + " todatta=" + best.ToString("F2")
            + " ima=" + transform.position.ToString("F1") + " " + kekka);
        Application.Quit();
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
