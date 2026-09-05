using UnityEngine;

/// <summary>
/// VRoid の モデルを **いまの キャラの すぐ 横**に 立てて、動きを 見くらべる（2026-09-06）。
///
/// 主人公（板の 絵）に ついて 動き、速さに 合わせて 待機／歩き／走りを 出す。
/// **見くらべ の ためだけ** の もの。採用が 決まったら 作りなおす。
///
/// ★向きは **カメラから 見た 向き**では なく **世界の 向き**で 回す。
///   板の 絵は カメラ基準だが、3Dの モデルは そのまま 世界に 立って いる。
/// </summary>
public class NiwaVroid : MonoBehaviour {
    public Transform target;               // 主人公
    public Animator anim;
    public Vector3 zure = new Vector3(0.9f, 0f, 0f);   // 右へ どれだけ
    public string tomaru = "Idle", aruku = "Walk", hashiru = "Run";
    public float arukuIjou = 0.15f, hashiruIjou = 3.4f;

    Vector3 mae;
    string ima = "";

    void Awake() {
        // ★`-vroid` を つけた ときだけ 出す。ふだんの 絵づくりの じゃまを しない
        bool dasu = false;
        foreach (var a in System.Environment.GetCommandLineArgs())
            if (a == "-vroid") dasu = true;
        foreach (var a in System.Environment.GetCommandLineArgs())
            if (a == "-vrun") zutto = true;
        if (!dasu) { gameObject.SetActive(false); return; }
        Debug.Log("[NiwaVroid] 見くらべ用の モデルを 出す");
    }

    void Start() {
        if (target != null) mae = target.position;
        // ★はじめは **カメラの ほうを 向く**。板の 絵は 止まると こちらを 向く ので そろえる。
        //   置いた ままだと 背中を 見せて いた（2026-09-06）
        transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        if (anim != null) {
            anim.CrossFade(tomaru, 0f, 0);
            anim.Update(0f);
            var st = anim.GetCurrentAnimatorStateInfo(0);
            Debug.Log("[NiwaVroid] avatar=" + (anim.avatar == null ? "なし"
                      : (anim.avatar.isHuman ? "Humanoid○" : "Humanoid×"))
                      + " ctrl=" + (anim.runtimeAnimatorController == null ? "なし"
                      : anim.runtimeAnimatorController.name)
                      + " state=" + st.shortNameHash + " len=" + st.length.ToString("F2"));
        }
        Utsuru();
    }

    bool zutto;      // -vrun ： ずっと 走らせる（動いて いるか たしかめる ため）

    void LateUpdate() {
        // ★ここで Kake を 呼んで、下でも 速さで Kake を 呼ぶと **毎フレーム 交互に なって
        //   クロスフェードが 再開し つづけ、時間が 進まない**（2026-09-06 実際に そうなった）。
        //   呼ぶのは 1フレームに 1回だけ に する
        if (anim != null && zutto) {
            if (Time.frameCount % 60 == 0) {
                var st = anim.GetCurrentAnimatorStateInfo(0);
                var hip = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                Debug.Log("[NiwaVroid] t=" + st.normalizedTime.ToString("F2")
                          + " Run?=" + st.IsName("Run")
                          + " anim.enabled=" + anim.enabled + " speed=" + anim.speed
                          + " timeScale=" + Time.timeScale + " dt=" + Time.deltaTime.ToString("F3")
                          + " updateMode=" + anim.updateMode + " culling=" + anim.cullingMode
                          + " 左もも=" + (hip == null ? "なし" : hip.localRotation.eulerAngles.ToString("F1")));
            }
        }
        if (target == null) return;
        var now = target.position;
        var v = (now - mae) / Mathf.Max(Time.deltaTime, 1e-4f);
        v.y = 0f;
        mae = now;
        float spd = v.magnitude;

        transform.position = now + zure;
        if (spd > arukuIjou) {
            // 進む 向きへ 体を 向ける（3Dなので 世界の 向き）
            var q = Quaternion.LookRotation(v.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, q, 720f * Time.deltaTime);
        }
        Kake(zutto ? hashiru : (spd <= arukuIjou ? tomaru : (spd >= hashiruIjou ? hashiru : aruku)));
        Utsuru();
    }

    void Kake(string na) {
        if (anim == null || na == ima) return;
        ima = na;
        anim.CrossFade(na, 0.15f, 0);
    }

    /// <summary>板の 絵と 同じ 明るさで 見える ように、影は 落とさない</summary>
    void Utsuru() { }
}
