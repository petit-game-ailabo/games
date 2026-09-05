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
    public string tomaru = "Idle";

    /// <summary>歩き／走りの 組み合わせ。**Bキーで 切りかえて 見くらべる**。
    /// 汎用リグの 既定（Walk/Sprint）は 男性的で 重い ので、ほかも 並べる</summary>
    static readonly string[,] KUMI = {
        { "WalkF2K",    "Jog"  },   // BOOTH の 歩き（VRM むけ）＋ 軽い 駆け足
        { "WalkFormal", "Jog"  },   // Quaternius の やわらかい ほう
        { "Walk",       "Run"  },   // Quaternius の もとの まま（重い）
        { "WalkF2K",    "Run"  },
    };
    int kumi = 0;                   // 既定は BOOTH の 歩き
    string aruku { get { return KUMI[kumi, 0]; } }
    string hashiru { get { return KUMI[kumi, 1]; } }
    public float arukuIjou = 0.15f, hashiruIjou = 3.4f;

    Vector3 mae;
    string ima = "";

    void Awake() {
        // ★**既定で 出す。Vキーで 消せる。**
        //   はじめ `-vroid` を つけた ときだけ に して いたが、
        //   exe を そのまま 起ちあげると 引数が 無い ので **出て こなかった**（2026-09-06）。
        //   見せる ものは 既定に する（D-224 と 同じ しくじり）
        bool kesu = false;
        foreach (var a in System.Environment.GetCommandLineArgs()) {
            if (a == "-novroid") kesu = true;
            if (a == "-vrun") zutto = true;
            if (a == "-vwalk") zuttoAruku = true;
        }
        if (kesu) { gameObject.SetActive(false); return; }
        Debug.Log("[NiwaVroid] 見くらべ用の モデルを 出す（Vキーで 消せる）");
    }

    /// <summary>Vキーで 出したり 消したり</summary>
    void Update() {
        if (Input.GetKeyDown(KeyCode.B)) {
            kumi = (kumi + 1) % (KUMI.GetLength(0));
            ima = "";                       // つぎの コマで かけ直す
        }
        if (!Input.GetKeyDown(KeyCode.V)) return;
        miseru = !miseru;
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = miseru;
    }

    void OnGUI() {
        if (!miseru) return;
        GUI.Label(new Rect(10, 46, 700, 22),
                  "B=3Dの うごきを かえる（いま " + aruku + " / " + hashiru + "）");
    }

    bool miseru = true;

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

    bool zutto;        // -vrun  ： ずっと 走らせる
    bool zuttoAruku;   // -vwalk ： ずっと 歩かせる（見くらべの ため）

    void LateUpdate() {
        // ★ここで Kake を 呼んで、下でも 速さで Kake を 呼ぶと **毎フレーム 交互に なって
        //   クロスフェードが 再開し つづけ、時間が 進まない**（2026-09-06 実際に そうなった）。
        //   呼ぶのは 1フレームに 1回だけ に する
        if (anim != null) {
            if (Time.frameCount % 45 == 0) {
                var st = anim.GetCurrentAnimatorStateInfo(0);
                var hip = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                Debug.Log("[NiwaVroid] ima=" + ima + " t=" + st.normalizedTime.ToString("F2")
                          + " len=" + st.length.ToString("F2")
                          + " WalkF2K?=" + st.IsName("WalkF2K")
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
        Kake(zutto ? hashiru : zuttoAruku ? aruku
             : (spd <= arukuIjou ? tomaru : (spd >= hashiruIjou ? hashiru : aruku)));
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
