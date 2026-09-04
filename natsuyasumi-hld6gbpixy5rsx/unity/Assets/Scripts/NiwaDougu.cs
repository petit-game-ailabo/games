using System.Collections.Generic;
using UnityEngine;

// 納屋の 道具を **取って 外で つかう**（2026-09-05・第1弾＝虫とり網と 虫かご）。
//
// ★本人「実際に物を取って外で使えるようにしよう。外に虫がいるから、まずは虫かごと虫取り網かな」
//
// つくりの きまり
//   ・**道具は 場面の 物 そのもの**を 持ちあげる（HUDの アイコンだけ、には しない）。
//     納屋の 中で 見えて いた 網が そのまま 手に 移る ので「取った」が 絵で わかる。
//   ・持って いる あいだは **カメラ基準で 主人公の わきに 置く**（親子づけ しない）。
//     主人公は カメラを 向く 板なので、世界の 向きで つけると 裏に まわりこむ。
//   ・スペースは **1つの キー**。近くの できごとの 中から 1つだけ えらんで 案内を 出す。
//     （取る／しまう／あみを ふる。同時に 2つ 出さない）
//   ・あみは 幹や 草の 虫は ほぼ 取れて、飛ぶ 虫は にげやすい（NiwaMushi.AmiWoFuru）。
public class NiwaDougu : MonoBehaviour {

    [System.Serializable]
    public class Mono {
        public string id;                 // "ami" / "kago"
        public string namae;              // ひらがなの 名まえ
        public Transform mi;              // 物ぜんたい（納屋の 中に 置いて ある）
        public Vector3 oki;               // もとの 置き場所（world）
        public Vector3 okiKaiten;         // もとの 向き
        public Vector3 mochiOff;          // 持った ときの ずらし（右・上・前）
        public Vector3 mochiKaiten;       // 持った ときの 向き（カメラ基準）
        [HideInInspector] public bool motteru;
    }

    public Mono[] mono;
    public Transform player;
    public NiwaMushi mushi;
    public Font font;

    public float todoku = 1.7f;        // 取る／しまう の 近さ
    public float amiHaba = 1.6f;       // あみの とどく 長さ
    public float furuByou = 0.40f;     // ふり切るまで

    readonly List<string> kago = new List<string>();   // つかまえた 虫（名まえ）
    float furu = -1f; bool sabaita;
    string fuki; float fukiT;

    // ---- 撮影用（-motsu で はじめから 持つ／-furu で ときどき ふる）。
    //      **自動運転は「上手に 遊ぶ」ように する**：一定間かくで 押させると
    //      虫が いない ときに ばかり ふって、つかまえた 画が 一度も 撮れない
    bool autoFuru; float autoT;

    void Start() {
        foreach (var a in System.Environment.GetCommandLineArgs()) {
            if (a == "-motsu") foreach (var m in mono) m.motteru = true;
            if (a == "-furu") autoFuru = true;
        }
    }

    Mono Motteru(string id) {
        foreach (var m in mono) if (m.id == id) return m.motteru ? m : null;
        return null;
    }

    /// <summary>床の 上での 近さ。★高さを 入れない こと。かごは 棚の 上（1.1m 高い）に ある ので、
    /// 素の 距離で 見ると 目の 前に 立って いても とどかない</summary>
    float Yoko(Vector3 a, Vector3 b) {
        return new Vector2(a.x - b.x, a.z - b.z).magnitude;
    }

    Mono ChikaiOki() {                  // 近くに 置いて ある 道具
        Mono best = null; float bd = todoku;
        foreach (var m in mono) {
            if (m == null || m.mi == null || m.motteru) continue;
            float d = Yoko(player.position, m.oki);
            if (d < bd) { bd = d; best = m; }
        }
        return best;
    }

    Mono ChikaiModosu() {               // 持って いて、もとの 場所の 近く
        Mono best = null; float bd = todoku;
        foreach (var m in mono) {
            if (m == null || m.mi == null || !m.motteru) continue;
            float d = Yoko(player.position, m.oki);
            if (d < bd) { bd = d; best = m; }
        }
        return best;
    }

    Vector3 Te { get { return player.position + Vector3.up * 0.55f; } }

    /// <summary>いま スペースで できる こと。null なら 何も 出さない</summary>
    string Dekiru(out int shurui, out Mono taisho) {
        shurui = 0; taisho = null;
        var ami = Motteru("ami");
        if (ami != null && mushi != null && furu < 0f) {
            string na = mushi.ChikaiNa(Te, amiHaba);
            if (na != null) { shurui = 3; return na + "を つかまえる"; }
        }
        var o = ChikaiOki();
        if (o != null) { shurui = 1; taisho = o; return o.namae + "を とる"; }
        var b = ChikaiModosu();
        if (b != null) { shurui = 2; taisho = b; return b.namae + "を しまう"; }
        if (ami != null) { shurui = 4; return "あみを ふる"; }
        return null;
    }

    void Update() {
        if (player == null || mono == null) return;
        if (fukiT > 0f) fukiT -= Time.deltaTime;

        if (furu >= 0f) {
            furu += Time.deltaTime;
            if (!sabaita && furu >= furuByou * 0.55f) { sabaita = true; Sabaku(); }
            if (furu >= furuByou) furu = -1f;
        }

        // 撮影の 自動運転：**虫が とどく ところに 来たら** ふる
        if (autoFuru && furu < 0f && mushi != null && Time.time > autoT) {
            if (Motteru("ami") != null && mushi.ChikaiNa(Te, amiHaba) != null) {
                furu = 0f; sabaita = false; autoT = Time.time + 0.9f;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            int sh; Mono m;
            Dekiru(out sh, out m);
            if (sh == 1) { m.motteru = true; Iu(m.namae + "を もった"); }
            else if (sh == 2) { m.motteru = false; Modosu(m); Iu(m.namae + "を しまった"); }
            else if (sh == 3 || sh == 4) { furu = 0f; sabaita = false; }
        }
        Oku();
    }

    void Sabaku() {
        if (mushi == null) return;
        string na = mushi.AmiWoFuru(Te, amiHaba);
        if (na == null) return;                       // そもそも 居ない：空ぶり
        if (na == "") { Iu("にげられた！"); return; }
        if (Motteru("kago") == null) { Iu("かごが ないと 入れられない"); return; }
        kago.Add(na);
        Iu(na + "を つかまえた！");
    }

    void Iu(string s) { fuki = s; fukiT = 2.4f; }

    void Modosu(Mono m) {
        m.mi.position = m.oki;
        m.mi.rotation = Quaternion.Euler(m.okiKaiten);
    }

    /// <summary>持って いる 物を カメラ基準で わきに 置く。ふって いる あいだは 弧を えがく</summary>
    void Oku() {
        var cam = Camera.main;
        if (cam == null) return;
        var migi = cam.transform.right; migi.y = 0f;
        if (migi.sqrMagnitude < 1e-4f) migi = Vector3.right;
        migi.Normalize();
        var mae = Vector3.Cross(Vector3.up, migi);    // カメラの 前（地面ぞい）
        foreach (var m in mono) {
            if (m == null || m.mi == null || !m.motteru) continue;
            float yaw = 0f, pitch = 0f, sayuu = 0f;
            if (m.id == "ami" && furu >= 0f) {
                // ふり：うしろ → 前へ 弧。行きは 速く、もどりは ゆっくり
                float k = Mathf.Clamp01(furu / furuByou);
                float e = k < 0.55f ? Mathf.Sin(k / 0.55f * Mathf.PI * 0.5f)
                                    : 1f - (k - 0.55f) / 0.45f * 0.35f;
                yaw = Mathf.Lerp(-40f, 55f, e);
                pitch = Mathf.Lerp(-25f, 40f, e);
                sayuu = Mathf.Lerp(-0.15f, 0.45f, e);
            }
            var off = m.mochiOff;
            m.mi.position = player.position
                          + migi * (off.x + sayuu) + Vector3.up * off.y + mae * off.z;
            m.mi.rotation = Quaternion.LookRotation(mae, Vector3.up)
                          * Quaternion.Euler(m.mochiKaiten + new Vector3(pitch, yaw, 0f));
        }
    }

    void OnGUI() {
        if (font != null) GUI.skin.font = font;
        int sh; Mono m;
        string d = Dekiru(out sh, out m);
        if (d != null && furu < 0f) {
            var r = new Rect(Screen.width / 2 - 230, Screen.height - 92, 460, 28);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), "スペース：" + d);
            var c = GUI.color; GUI.color = new Color(1f, 0.97f, 0.86f);
            GUI.Label(r, "スペース：" + d);
            GUI.color = c;
        }
        if (fukiT > 0f) {
            var r = new Rect(Screen.width / 2 - 240, Screen.height / 2 - 120, 480, 28);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), "『" + fuki + "』");
            var c = GUI.color; GUI.color = new Color(1f, 0.97f, 0.86f);
            GUI.Label(r, "『" + fuki + "』");
            GUI.color = c;
        }
        // もちもの と かごの 中み
        var sb = new System.Text.StringBuilder();
        foreach (var mm in mono) if (mm != null && mm.motteru) sb.Append(sb.Length > 0 ? "・" : "").Append(mm.namae);
        if (sb.Length > 0) GUI.Label(new Rect(14, 54, 460, 24), "もちもの：" + sb);
        if (kago.Count > 0) {
            var kinds = new List<string>();
            foreach (var k in kago) if (!kinds.Contains(k)) kinds.Add(k);
            GUI.Label(new Rect(14, 78, 620, 24),
                      "かご " + kago.Count + "ひき（" + string.Join("・", kinds.ToArray()) + "）");
        }
    }
}
