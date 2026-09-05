using System.Collections.Generic;
using UnityEngine;

// 道具（2026-09-05・第1弾＝虫とりの ひとそろい）。スペース 1つで
// 「納屋に はいる」「道具を とる」「虫を つかまえる」を まかなう。
//
// ★本人「網と籠はワンセットのアイテムとして拾えるようにしよう。そのうえで、かごは表示しないように。
//   網だけ表示しよう」
// ★本人「虫取り網だけど、網と釣りざおを持ち帰るのってめんどくさいんだよね」
//   → **持ち帰る 概念を なくす。** 一度 見つけたら ずっと 持って いる（`motta`）。
//     手に 出るのは **その 場で つかう ものだけ**（外＝あみ／屋内＝手ぶら）。
//     竿を 足す ときも 同じ：水べに 立った ときだけ 竿が 出る。
//     どうぶつの森は 道具を 1つずつ 出して 手に 持ち、ぼくなつ2は 家の 中で あみが 消える。
//     どちらも「入れものは 見せない・いま つかう ものだけ 見せる」で 共通して いる。
// ★かご は 物として 作らない（持ち歩く 絵に しない）。中みは メニュー（`NiwaMenu`）で 見る。
public class NiwaDougu : MonoBehaviour {

    [System.Serializable]
    public class Mono {
        public string id;                 // "mushitori"
        public string namae;              // 拾う ときの 名まえ
        public string totta = "";         // 取った ときの ことば（空なら 名まえ＋「を てに いれた」）
        public Transform mi;              // 手に 出す 見た目（あみ）
        public Vector3 oki;               // 置いて ある ところ（world）
        public Vector3 okiKaiten;
        public Vector3 mochiOff;          // 持った ときの ずらし（右・上・前）
        public Vector3 mochiKaiten;
        [HideInInspector] public bool motta;      // 見つけた（ずっと 持って いる）
    }

    public Mono[] mono;
    public Transform player;
    public NiwaMushi mushi;
    public NiwaNayaNaka naya;
    public NiwaMenu menu;
    public Font font;

    public float todoku = 1.7f;        // 取る の 近さ
    public float amiHaba = 1.6f;       // あみの とどく 長さ
    public float furuByou = 0.40f;     // ふり切るまで

    /// <summary>かごの 中み。メニューが 読む</summary>
    public readonly List<string> Kago = new List<string>();

    float furu = -1f; bool sabaita;
    string fuki; float fukiT;
    bool autoFuru; float autoT;

    void Start() {
        foreach (var a in System.Environment.GetCommandLineArgs()) {
            if (a == "-motsu") foreach (var m in mono) m.motta = true;
            if (a == "-furu") autoFuru = true;
        }
    }

    Mono Motta(string id) {
        if (mono == null) return null;
        foreach (var m in mono) if (m.id == id) return m.motta ? m : null;
        return null;
    }

    /// <summary>床の 上での 近さ。★高さを 入れない（棚の 上の 物が とどかなく なる）</summary>
    static float Yoko(Vector3 a, Vector3 b) { return new Vector2(a.x - b.x, a.z - b.z).magnitude; }

    /// <summary>まだ 見つけて いなくて 目の 前に ある 道具。**その 部屋に 居る ことが 要る**
    /// （2026-09-05・本人「納屋の外から虫かごがとれちゃう」。壁ごしに 取れて いた）</summary>
    Mono ChikaiOki() {
        if (naya == null || !naya.Uchi) return null;
        Mono best = null; float bd = todoku;
        foreach (var m in mono) {
            if (m == null || m.mi == null || m.motta) continue;
            float d = Yoko(player.position, m.oki);
            if (d < bd) { bd = d; best = m; }
        }
        return best;
    }

    Vector3 Te { get { return player.position + Vector3.up * 0.55f; } }

    /// <summary>いま スペースで できる こと。null なら 何も 出さない</summary>
    string Dekiru(out int shurui, out Mono taisho) {
        shurui = 0; taisho = null;
        if (menu != null && menu.Hiraiteru) return null;
        bool soto = naya == null || !naya.Uchi;
        var ami = Motta("mushitori");
        if (ami != null && mushi != null && furu < 0f && soto) {
            string na = mushi.ChikaiNa(Te, amiHaba);
            if (na != null) { shurui = 3; return na + "を つかまえる"; }
        }
        var o = ChikaiOki();
        if (o != null) { shurui = 1; taisho = o; return o.namae + "を とる"; }
        if (ami != null && soto) { shurui = 4; return "あみを ふる"; }
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
        if (autoFuru && furu < 0f && mushi != null && Time.time > autoT
            && Motta("mushitori") != null && mushi.ChikaiNa(Te, amiHaba) != null) {
            furu = 0f; sabaita = false; autoT = Time.time + 0.9f;
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            int sh; Mono m;
            Dekiru(out sh, out m);
            if (sh == 1) {
                m.motta = true;
                Iu(string.IsNullOrEmpty(m.totta) ? m.namae + "を てに いれた" : m.totta);
            }
            else if (sh == 3 || sh == 4) { furu = 0f; sabaita = false; }
        }
        Oku();
    }

    void Sabaku() {
        if (mushi == null) return;
        string na = mushi.AmiWoFuru(Te, amiHaba);
        if (na == null) return;                       // そもそも 居ない：空ぶり
        if (na == "") { Iu("にげられた！"); return; }
        Kago.Add(na);
        Iu(na + "を つかまえた！");
    }

    void Iu(string s) { fuki = s; fukiT = 2.4f; }

    /// <summary>手に 出す 物を カメラ基準で わきに 置く。★親子づけ しない
    /// （主人公は カメラを 向く 板。世界の 向きで つけると 裏に まわりこむ）</summary>
    void Oku() {
        var cam = Camera.main;
        if (cam == null) return;
        var migi = cam.transform.right; migi.y = 0f;
        if (migi.sqrMagnitude < 1e-4f) migi = Vector3.right;
        migi.Normalize();
        var mae = Vector3.Cross(Vector3.up, migi);    // カメラの 前（地面ぞい）
        // ★屋内では 手ぶら（ぼくなつ2 も 家の 中では あみが 消える）。
        //   寄った 屋内カメラの 画に 1.6mの 柄が 入ると 画面の 半分が 棒に なる
        bool dasu = naya == null || !naya.Uchi;
        foreach (var m in mono) {
            if (m == null || m.mi == null || !m.motta) continue;
            if (!dasu) {                              // 置き場所へ 戻して おく（見えない ところ）
                m.mi.position = m.oki; m.mi.rotation = Quaternion.Euler(m.okiKaiten);
                continue;
            }
            float yaw = 0f, pitch = 0f, sayuu = 0f;
            if (furu >= 0f) {
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

    /// <summary>影つきの 1行。★かげは **暗い 色で** 描く。同じ 色で 2回 描くと
    /// 「字が 2重に 出て いる」に 見える（2026-09-05・本人の 指摘）</summary>
    public static void Ichigyou(Rect r, string s) {
        var c = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), s);
        GUI.color = new Color(1f, 0.97f, 0.86f);
        GUI.Label(r, s);
        GUI.color = c;
    }

    void OnGUI() {
        if (font != null) GUI.skin.font = font;
        if (menu != null && menu.Hiraiteru) return;
        int sh; Mono m;
        string d = Dekiru(out sh, out m);
        if (d != null && furu < 0f)
            Ichigyou(new Rect(Screen.width / 2 - 230, Screen.height - 92, 460, 28), "スペース：" + d);
        if (fukiT > 0f)
            Ichigyou(new Rect(Screen.width / 2 - 240, Screen.height / 2 - 120, 480, 28), "『" + fuki + "』");
        if (Motta("mushitori") != null)
            Ichigyou(new Rect(14, 54, 460, 24),
                     (Kago.Count > 0 ? "かご " + Kago.Count + "ひき　" : "") + "Ｍ：めにゅー");
    }
}
