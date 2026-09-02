using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 庭の 虫——**見せかたの 段**（2026-09-02）。つかまえる・図鑑・かご・ずもうは 旧村の
// 仕組み（Bug/BugCatcher/BugBook…）を あとで 移す。ここでは「写真の 木と 3Dの 中で
// 虫が 虫に 見える」ことだけを 決める。
//
// ★きめごと（D-171）：**生きものは 絵、世界は 写真**。虫を 木と 同じ 写真の 質感に すると
//   5cmの 虫は 画面で 5px＝見えない うえ、幹に 溶けて 消える。魔理沙と 同じ「絵」の 側に 置く。
//   絵は 本人が Codex で 作った リアルな 画像を 切りぬいて 色数を 落とした もの
//   （make_mushi.py・Assets/Art/Sprites/mushi/<種>_<向き>.png）。
// ★大きさ（D-173）：幹の 暗い 虫は 実物の 4倍、飛ぶ 虫は 3倍。2.5倍では 樹皮の 傷と 見わけが つかなかった。
// ★向きと 動き（2026-09-02・本人「クワガタ？がカメラに対して斜めで トンボが正面って意味わからん。
//   動きも、もっと虫ごとに特徴あるよね」）：
//   ・幹の 虫は **上から 見た 絵（背中）を 頭を 上に して 樹皮に 貼る**。横向きの 絵を 幹の 面に
//     貼ると、正面の カメラからは ななめに ゆがんで 見えた。カブト・クワガタは ゆっくり 這う、
//     セミは とまりっぱなしで ときどき 飛びさる
//   ・飛ぶ 虫は 板を **うしろに 寝かせて 上前方から 見た 形**に し、頭を 進む 向きに あわせる。
//     上から 見た 絵を カメラ正面に 向けると 壁に 貼った 標本に なる。
//     トンボ＝すっと 飛んで 空中で 止まる／オニヤンマ＝一直線に 往復／チョウ＝羽ばたき＋ふらつき
//   ・バッタは じっと して いて ときどき 放物線で 跳ぶ
// ★近づいたら 寄りカード（画面の 右下に 大きな 絵と 名まえ）。カメラは 寄れない ので ここで 見せる
public class NiwaMushi : MonoBehaviour {
    public enum Perch { Miki, Sora, Kusa, Shigemi }

    [System.Serializable]
    public class Shu {
        public string id;          // ファイル名の 頭（semi/kabuto/…）
        public string name;        // ひらがな
        public Perch perch;
        public float haba;         // 絵の 板の 大きさ(m)
        public bool hiru, yoru;    // 出る 時間帯
        public Texture2D yoko, ue, naname;
        // ★材質は **組み立て時に BuildNiwa が 作って わたす**（主人公と 同じ やりかた・D-172）。
        //   実行時の Shader.Find は ビルドに 無い シェーダで null → 黙って Lit に 落ちて 黒い 四角に なった
        public Material zairyo;        // 世界で 出す 絵（Sekai）の 材質
        public Material zairyoYoko;    // 飛ぶ 虫が 真横に 進む ときの 横の 絵（あれば）
        // 世界で 出す 絵：幹と 空は 上から（背中）、草は 横
        public Texture2D Sekai { get { return perch == Perch.Kusa ? (yoko ?? ue) : (ue ?? yoko); } }
        public Texture2D Card { get { return naname ?? yoko ?? ue; } }
    }

    [System.Serializable]
    public class Miki { public Vector3[] pts; public float[] rad; }   // KiV5 の 背骨（輪の 座標と 半径）

    public List<Shu> shu = new List<Shu>();
    public List<Miki> miki = new List<Miki>();
    public Font font;
    public int kazu = 12;                              // 同時に 居る 数
    public float kouho = 18f;                          // 主人公から この 半径の 中に 湧かせる

    /// <summary>種の 台帳。絵は 組み立て時に BuildNiwa が 入れる</summary>
    public static List<Shu> Shurui() {
        return new List<Shu> {
            new Shu { id = "semi",     name = "あぶらぜみ",     perch = Perch.Miki,    haba = 0.34f, hiru = true },
            new Shu { id = "kabuto",   name = "かぶとむし",     perch = Perch.Miki,    haba = 0.42f, hiru = true, yoru = true },
            new Shu { id = "kuwagata", name = "のこぎりくわがた", perch = Perch.Miki,  haba = 0.38f, hiru = true, yoru = true },
            new Shu { id = "tonbo",    name = "しおからとんぼ", perch = Perch.Sora,    haba = 0.36f, hiru = true },
            new Shu { id = "oniyanma", name = "おにやんま",     perch = Perch.Sora,    haba = 0.50f, hiru = true },
            new Shu { id = "chou",     name = "あげはちょう",   perch = Perch.Sora,    haba = 0.36f, hiru = true },
            new Shu { id = "batta",    name = "しょうりょうばった", perch = Perch.Kusa, haba = 0.30f, hiru = true },
            new Shu { id = "hotaru",   name = "ほたる",         perch = Perch.Shigemi, haba = 0.10f, yoru = true },
        };
    }

    class Hiki {
        public Shu shu; public GameObject go; public Transform ita;
        public Vector3 home, pos, target; public float phase, wait, t;
        public Light hikari;
        // 幹の 虫
        public Miki miki; public float kakudo, takasa, kakudoV, takasaV; public bool sakasa;
        // 飛ぶ 虫・跳ぶ 虫
        public Vector3 heading = Vector3.forward; public int mode; public Vector3 lineA, lineB;
        public bool tobisaru; public float tobiT;
    }
    readonly List<Hiki> ikiteru = new List<Hiki>();
    readonly Dictionary<string, Material> zairyo = new Dictionary<string, Material>();
    Transform player;
    float timer;
    bool debugMushi;

    // 寄りカード
    RawImage cardImg; Text cardTxt; CanvasGroup cardGrp; float cardAlpha;

    void Start() {
        var mv = FindFirstObjectByType<MuraMove>();
        if (mv != null) player = mv.transform;
        foreach (var a in System.Environment.GetCommandLineArgs()) if (a == "-mushi") debugMushi = true;
        MakeCard();
        if (!debugMushi) for (int i = 0; i < 6; i++) Waku();
    }

    void Update() {
        ikiteru.RemoveAll(h => h.go == null);
        timer -= Time.deltaTime;
        if (timer <= 0f) { timer = 1.2f; Waku(); Kataduke(); }
        var cam = Camera.main;
        foreach (var h in ikiteru) if (h.go != null) Ugoku(h, cam);
        Card();
    }

    // ---------------------------------------------------------------- 湧かせる
    bool Deru(Shu s) { return MuraDay.Night ? s.yoru : s.hiru; }

    void Waku() {
        if (ikiteru.Count >= kazu) return;
        var cand = shu.FindAll(s => Deru(s) && s.Sekai != null);
        if (cand.Count == 0) return;
        var s = cand[Random.Range(0, cand.Count)];
        Hiki h;
        if (!Basho(s, out h)) return;
        Oku(h);
    }

    /// <summary>時間帯が 変わって 出ない 虫に なったら 消す（朝に ホタルは いない）</summary>
    void Kataduke() {
        foreach (var h in ikiteru) if (!Deru(h.shu)) Destroy(h.go);
    }

    Vector3 Origin { get { return player != null ? player.position : transform.position; } }

    bool Basho(Shu s, out Hiki h) {
        h = new Hiki { shu = s, phase = Random.value * 10f };
        var o = Origin;
        for (int i = 0; i < 10; i++) {
            switch (s.perch) {
                case Perch.Miki: {
                    if (miki.Count == 0) return false;
                    var m = miki[Random.Range(0, miki.Count)];
                    // ★近い 幹ほど えらぶ（遠い 幹の 虫は 小さすぎて 読めない）。半径の 6割より 外は 落とす
                    float dm = new Vector2(m.pts[0].x - o.x, m.pts[0].z - o.z).magnitude;
                    if (dm > kouho * 0.6f) continue;
                    if (Random.value < dm / (kouho * 0.6f) * 0.7f) continue;
                    h.miki = m; h.kakudo = Random.Range(-25f, 25f); h.takasa = Random.Range(0.9f, 1.9f);
                    h.sakasa = Random.value < 0.35f;
                    Quaternion rot;
                    if (!MikiNi(h, out h.pos, out rot)) continue;
                    h.home = h.pos;
                    return true;
                }
                case Perch.Sora: {
                    var c = Random.insideUnitCircle * kouho * 0.7f;
                    var at = new Vector3(o.x + c.x, 0f, o.z + c.y);
                    if (!Jimen(ref at)) continue;
                    at.y += Random.Range(1.0f, 2.0f);
                    h.home = h.pos = h.target = at;
                    return true;
                }
                case Perch.Kusa: {
                    var c = Random.insideUnitCircle * kouho * 0.6f;
                    var at = new Vector3(o.x + c.x, 0f, o.z + c.y);
                    if (!Jimen(ref at)) continue;
                    at.y += s.haba * 0.40f;
                    h.home = h.pos = at;
                    return true;
                }
                default: {   // Shigemi＝夜の しげみ。木の 根もとの まわり
                    if (miki.Count == 0) return false;
                    var m = miki[Random.Range(0, miki.Count)];
                    var ne = m.pts[0];
                    if (new Vector2(ne.x - o.x, ne.z - o.z).magnitude > kouho) continue;
                    var c = Random.insideUnitCircle.normalized * Random.Range(0.8f, 2.2f);
                    h.home = h.pos = new Vector3(ne.x + c.x, ne.y + 0.2f + Random.Range(0.3f, 1.2f), ne.z + c.y);
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>幹の 面に 貼る 位置と 向き。角度は 手前(-Z)を 0° として 左右へ。
    /// 背骨の 輪を その 高さで 補間して **実際の 芯と 半径**を つかう。
    /// 板の 見える 面（local -Z）を 外へ 向け、絵の 上（頭）を 幹の 上へ（sakasa なら 下へ）</summary>
    bool MikiNi(Hiki h, out Vector3 at, out Quaternion rot) {
        at = Vector3.zero; rot = Quaternion.identity;
        var m = h.miki;
        if (m == null || m.pts == null || m.pts.Length < 2) return false;
        float y = m.pts[0].y + 0.2f + h.takasa;          // pts[0] は 地めんの 0.2m 下
        int i = 0;
        while (i < m.pts.Length - 2 && m.pts[i + 1].y < y) i++;
        float t = Mathf.InverseLerp(m.pts[i].y, m.pts[i + 1].y, y);
        var shin = Vector3.Lerp(m.pts[i], m.pts[i + 1], t);
        float r = Mathf.Lerp(m.rad[i], m.rad[i + 1], t) + 0.02f;
        float a = h.kakudo * Mathf.Deg2Rad;
        var soto = new Vector3(Mathf.Sin(a), 0f, -Mathf.Cos(a));   // 幹から 外へ
        at = shin + soto * r;
        rot = Quaternion.LookRotation(-soto, Vector3.up);
        if (h.sakasa) rot = rot * Quaternion.Euler(0f, 0f, 180f);
        return true;
    }

    static bool Jimen(ref Vector3 p) {
        RaycastHit hit;
        if (!Physics.Raycast(p + Vector3.up * 12f, Vector3.down, out hit, 30f)) return false;
        p.y = hit.point.y;
        return true;
    }

    void Oku(Hiki h) {
        var s = h.shu;
        var go = new GameObject("Mushi_" + s.id);
        go.transform.SetParent(transform, false);
        go.transform.position = h.pos;
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Ita";
        Destroy(quad.GetComponent<Collider>());
        quad.transform.SetParent(go.transform, false);
        quad.transform.localScale = new Vector3(s.haba, s.haba, 1f);
        var r = quad.GetComponent<Renderer>();
        r.sharedMaterial = Zairyo(s);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.layer = 2;
        h.go = go; h.ita = quad.transform;
        if (s.perch == Perch.Miki) { Quaternion rot; if (MikiNi(h, out h.pos, out rot)) { go.transform.position = h.pos; go.transform.rotation = rot; } }
        if (s.perch == Perch.Sora) {
            h.mode = 0; if (h.wait <= 0f) h.wait = Random.Range(0.5f, 2f);
            if (h.heading.sqrMagnitude < 0.01f) { var d0 = Random.insideUnitCircle.normalized; h.heading = new Vector3(d0.x, 0f, d0.y); }
            if (s.id == "oniyanma") { var d = new Vector2(h.heading.x, h.heading.z).normalized; h.lineA = h.home + new Vector3(d.x, 0f, d.y) * 4f; h.lineB = h.home - new Vector3(d.x, 0f, d.y) * 4f; h.target = h.lineA; }
        }
        if (s.perch == Perch.Kusa) { if (h.wait <= 0f) h.wait = Random.Range(2f, 8f); if (h.heading.sqrMagnitude < 0.01f) h.heading = Random.value < 0.5f ? Vector3.left : Vector3.right; }
        if (s.id == "hotaru") {
            var lg = new GameObject("Hikari");
            lg.transform.SetParent(go.transform, false);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(0.85f, 1f, 0.45f);
            l.range = 2.0f; l.intensity = 1.4f; l.shadows = LightShadows.None;
            h.hikari = l;
        }
        ikiteru.Add(h);
    }

    Material Zairyo(Shu s) {
        if (s.zairyo != null) return s.zairyo;
        Material m;
        if (zairyo.TryGetValue(s.id, out m) && m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.mainTexture = s.Sekai;
        m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.45f); m.EnableKeyword("_ALPHATEST_ON");
        zairyo[s.id] = m;
        return m;
    }

    // ---------------------------------------------------------------- 動き（虫ごと）
    void Ugoku(Hiki h, Camera cam) {
        float dt = Time.deltaTime, t = Time.time + h.phase;
        switch (h.shu.perch) {
            case Perch.Miki: MikiUgoki(h, dt, t); break;
            case Perch.Sora: SoraUgoki(h, cam, dt, t); break;
            case Perch.Kusa: KusaUgoki(h, cam, dt, t); break;
            default:
                h.pos = h.home + new Vector3(Mathf.Sin(t * 0.5f) * 0.6f, Mathf.Sin(t * 0.9f) * 0.3f, Mathf.Cos(t * 0.4f) * 0.6f);
                h.go.transform.position = h.pos;
                if (cam != null) h.go.transform.rotation = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
                if (h.hikari != null) {
                    float k = Mathf.Pow(Mathf.Clamp01(Mathf.Sin(t * 1.6f) * 0.5f + 0.5f), 3f);
                    h.hikari.intensity = 0.2f + k * 2.0f;
                }
                break;
        }
    }

    /// <summary>幹の 虫。セミ＝とまりっぱなし、ときどき 飛びさる。カブト・クワガタ＝ゆっくり 這う</summary>
    void MikiUgoki(Hiki h, float dt, float t) {
        if (h.tobisaru) {
            // セミが 飛びさる：ななめ 上へ 速く。1.2秒で 消す
            h.tobiT += dt;
            h.pos += (Vector3.up * 2.5f + h.heading * 4f) * dt;
            h.go.transform.position = h.pos;
            h.go.transform.rotation = Quaternion.Euler(0f, h.go.transform.eulerAngles.y, Mathf.Sin(t * 40f) * 25f);
            if (h.tobiT > 1.2f) Destroy(h.go);
            return;
        }
        Vector3 at; Quaternion rot;
        if (h.shu.id == "semi") {
            h.wait -= dt;
            if (h.wait <= 0f) {
                if (Random.value < 0.15f) { h.tobisaru = true; h.heading = -h.go.transform.forward; h.heading.y = 0f; h.heading.Normalize(); return; }
                h.wait = Random.Range(6f, 14f);
            }
            // 気配だけ：ほんの 少し ふるえる
            if (!MikiNi(h, out at, out rot)) return;
            h.pos = at;
            h.go.transform.position = at + h.go.transform.right * Mathf.Sin(t * 0.7f) * 0.004f;
            h.go.transform.rotation = rot;
            return;
        }
        // カブト・クワガタ：這う → 止まる → 這う。上下 0.8〜2.1m の あいだを ゆっくり
        h.wait -= dt;
        if (h.wait <= 0f) {
            if (h.takasaV == 0f && h.kakudoV == 0f) {
                if (Random.value < 0.25f) h.sakasa = !h.sakasa;     // たまに 向きを 変える
                h.takasaV = (h.sakasa ? -1f : 1f) * Random.Range(0.03f, 0.07f);
                h.kakudoV = Random.Range(-4f, 4f);
                h.wait = Random.Range(3f, 7f);
            } else { h.takasaV = 0f; h.kakudoV = 0f; h.wait = Random.Range(2f, 6f); }
        }
        h.takasa += h.takasaV * dt; h.kakudo += h.kakudoV * dt;
        if (h.takasa < 0.8f) { h.takasa = 0.8f; h.sakasa = false; h.takasaV = Mathf.Abs(h.takasaV); }
        if (h.takasa > 2.1f) { h.takasa = 2.1f; h.sakasa = true; h.takasaV = -Mathf.Abs(h.takasaV); }
        h.kakudo = Mathf.Clamp(h.kakudo, -35f, 35f);
        if (!MikiNi(h, out at, out rot)) return;
        float yure = h.takasaV != 0f ? Mathf.Sin(t * 9f) * 3f : 0f;   // 這って いる あいだ わずかに ゆれる
        h.pos = at;
        h.go.transform.position = at;
        h.go.transform.rotation = rot * Quaternion.Euler(0f, 0f, yure);
    }

    /// <summary>飛ぶ 虫。板は うしろに 寝かせ、頭を 進む 向きへ</summary>
    void SoraUgoki(Hiki h, Camera cam, float dt, float t) {
        var s = h.shu;
        if (s.id == "tonbo") {
            // すっと 飛んで、空中で ぴたっと 止まる（ホバリング）
            if (h.mode == 0) {
                h.wait -= dt;
                h.pos = h.target + new Vector3(Mathf.Sin(t * 7f) * 0.02f, Mathf.Sin(t * 11f) * 0.03f, 0f);
                if (h.wait <= 0f) {
                    var c = Random.insideUnitCircle * Random.Range(1.5f, 4f);
                    h.target = h.home + new Vector3(c.x, Random.Range(-0.4f, 0.6f), c.y);
                    h.mode = 1;
                }
            } else {
                var d = h.target - h.pos; float dist = d.magnitude;
                float sp = Mathf.Clamp(dist * 4f, 0.8f, 5f);
                if (dist > 0.001f) h.heading = Vector3.Lerp(h.heading, d.normalized, 8f * dt).normalized;
                h.pos += (dist > 0.001f ? d.normalized : Vector3.zero) * Mathf.Min(sp * dt, dist);
                if (dist < 0.05f) { h.mode = 0; h.wait = Random.Range(1f, 3.5f); h.target = h.pos; }
            }
        } else if (s.id == "oniyanma") {
            // 一直線に 往復（同じ 道を 見まわる）
            var d = h.target - h.pos; float dist = d.magnitude;
            if (dist > 0.001f) h.heading = Vector3.Lerp(h.heading, d.normalized, 4f * dt).normalized;
            h.pos += (dist > 0.001f ? d.normalized : Vector3.zero) * Mathf.Min(3.2f * dt, dist);
            h.pos.y = Mathf.Lerp(h.pos.y, h.home.y + 0.6f + Mathf.Sin(t * 1.3f) * 0.15f, 2f * dt);
            if (dist < 0.1f) h.target = (h.target == h.lineA) ? h.lineB : h.lineA;
        } else {
            // チョウ：ふらふら。向きを ちょくちょく 変え、上下に ひらひら
            h.wait -= dt;
            if (h.wait <= 0f) {
                h.wait = Random.Range(0.4f, 1.2f);
                var c = Random.insideUnitCircle.normalized;
                var kibou = new Vector3(c.x, 0f, c.y);
                var modori = h.home - h.pos; modori.y = 0f;
                if (modori.magnitude > 4f) kibou = (kibou + modori.normalized * 1.5f).normalized;
                h.target = kibou;
            }
            if (h.target.sqrMagnitude > 0.01f) h.heading = Vector3.Lerp(h.heading, h.target, 3f * dt).normalized;
            h.pos += h.heading * 0.8f * dt;
            h.pos.y = h.home.y + Mathf.Sin(t * 3.1f) * 0.25f + Mathf.Sin(t * 0.7f) * 0.3f;
            float hane = 0.30f + 0.70f * Mathf.Abs(Mathf.Sin(t * 14f));       // 羽ばたき＝板の 横幅の 伸び縮み
            h.ita.localScale = new Vector3(s.haba * hane, s.haba, 1f);
        }
        h.go.transform.position = h.pos;
        h.go.transform.rotation = SoraMuki(h, cam);
    }

    /// <summary>飛ぶ 虫の 絵の 切りかえ：進む 向きが 画面の 左右に 近い ときは **横の 絵**を
    /// 立てた 板に（進む 向きへ 裏がえす）、奥・手前に 近い ときは 上から 見た 絵を 寝かせて</summary>
    bool SoraYoko(Hiki h, Camera cam, out float side) {
        side = 1f;
        var s = h.shu;
        if (s.zairyoYoko == null) return false;
        float yaw = cam != null ? cam.transform.eulerAngles.y : 0f;
        var camRot = Quaternion.Euler(0f, yaw, 0f);
        float a = Vector3.Dot(h.heading, camRot * Vector3.right), b = Vector3.Dot(h.heading, camRot * Vector3.forward);
        bool yoko = Mathf.Abs(a) > Mathf.Abs(b) * 1.2f;     // 横に 進む ほうが はっきり 大きい とき
        side = a < 0f ? 1f : -1f;                            // 絵は 頭が 左。右へ 進む なら 裏がえす
        var r = h.ita.GetComponent<Renderer>();
        var want = yoko ? s.zairyoYoko : s.zairyo;
        if (r.sharedMaterial != want) r.sharedMaterial = want;
        return yoko;
    }

    /// <summary>飛ぶ 虫の 板の 向き：カメラに 向けた 板を うしろに 寝かせ（上前方から 見た 形）、
    /// 絵の 上（頭）が 進む 向きに なるよう 板の 面内で まわす</summary>
    Quaternion SoraMuki(Hiki h, Camera cam) {
        float yaw = cam != null ? cam.transform.eulerAngles.y : 0f;
        var camRot = Quaternion.Euler(0f, yaw, 0f);
        float side;
        if (SoraYoko(h, cam, out side)) {
            // 横の 絵：立てた 板（ビルボード）。少し 前のめりに
            var sc = h.ita.localScale; sc.x = Mathf.Abs(sc.x) * side; h.ita.localScale = sc;
            return camRot * Quaternion.Euler(-8f, 0f, 0f);
        } else {
            var sc = h.ita.localScale; sc.x = Mathf.Abs(sc.x); h.ita.localScale = sc;
        }
        var R = camRot * Vector3.right; var F = camRot * Vector3.forward;
        float a = Vector3.Dot(h.heading, R), b = Vector3.Dot(h.heading, F);
        // 板を 寝かせると 絵の 上（頭）は F（画面の おく）を 向く。進む 向き (a,b) に あわせて 面内で まわす
        float roll = -Mathf.Atan2(a, b) * Mathf.Rad2Deg;
        float lean = h.shu.id == "chou" ? 50f : 58f;
        return camRot * Quaternion.Euler(-lean, 0f, roll);
    }

    /// <summary>草の 虫（バッタ）：じっと して、ときどき 放物線で 跳ぶ</summary>
    void KusaUgoki(Hiki h, Camera cam, float dt, float t) {
        if (h.mode == 0) {
            h.wait -= dt;
            if (h.wait <= 0f) {
                if (Random.value < 0.4f) h.heading = -h.heading;
                var kib = h.pos + h.heading * Random.Range(0.4f, 0.9f);
                if (Jimen(ref kib)) { kib.y += h.shu.haba * 0.40f; h.target = kib; h.lineA = h.pos; h.mode = 1; h.t = 0f; }
                else h.wait = 2f;
            }
        } else {
            h.t += dt / 0.55f;
            float k = Mathf.Clamp01(h.t);
            h.pos = Vector3.Lerp(h.lineA, h.target, k) + Vector3.up * (4f * k * (1f - k)) * 0.45f;
            if (k >= 1f) { h.mode = 0; h.wait = Random.Range(3f, 9f); h.home = h.pos; }
        }
        h.go.transform.position = h.pos;
        float yaw = cam != null ? cam.transform.eulerAngles.y : 0f;
        var camRot = Quaternion.Euler(0f, yaw, 0f);
        // 横向きの 絵。進む 向きに 左右を あわせる（板を 裏がえす）
        float side = Vector3.Dot(h.heading, camRot * Vector3.right) < 0f ? -1f : 1f;
        h.ita.localScale = new Vector3(h.shu.haba * side, h.shu.haba, 1f);
        float pitch = h.mode == 1 ? -Mathf.Sin(Mathf.Clamp01(h.t) * Mathf.PI) * 25f * side : 0f;   // 跳ぶ あいだ 前へ かたむく
        h.go.transform.rotation = camRot * Quaternion.Euler(0f, 0f, pitch);
    }

    // ---------------------------------------------------------------- 寄りカード
    void MakeCard() {
        var cg = new GameObject("MushiCard");
        cg.transform.SetParent(transform, false);
        var canvas = cg.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 40;
        var scaler = cg.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        cardGrp = cg.AddComponent<CanvasGroup>(); cardGrp.alpha = 0f;

        var bg = new GameObject("Bg").AddComponent<Image>();
        bg.transform.SetParent(cg.transform, false);
        bg.color = new Color(0.08f, 0.07f, 0.06f, 0.72f);
        var rb = bg.rectTransform; rb.anchorMin = rb.anchorMax = new Vector2(1f, 0f);
        rb.pivot = new Vector2(1f, 0f); rb.anchoredPosition = new Vector2(-40f, 60f); rb.sizeDelta = new Vector2(300f, 340f);

        cardImg = new GameObject("E").AddComponent<RawImage>();
        cardImg.transform.SetParent(bg.transform, false);
        var ri = cardImg.rectTransform; ri.anchorMin = ri.anchorMax = new Vector2(0.5f, 1f);
        ri.pivot = new Vector2(0.5f, 1f); ri.anchoredPosition = new Vector2(0f, -14f); ri.sizeDelta = new Vector2(260f, 260f);

        cardTxt = new GameObject("Na").AddComponent<Text>();
        cardTxt.transform.SetParent(bg.transform, false);
        cardTxt.font = font; cardTxt.fontSize = 30; cardTxt.alignment = TextAnchor.MiddleCenter;
        cardTxt.color = new Color(0.98f, 0.95f, 0.85f);
        var rt = cardTxt.rectTransform; rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f); rt.anchoredPosition = new Vector2(0f, 12f); rt.sizeDelta = new Vector2(0f, 44f);
    }

    void Card() {
        if (cardGrp == null || player == null) return;
        Hiki chikai = null; float best = 1.7f;
        foreach (var h in ikiteru) {
            if (h.go == null) continue;
            float d = Vector3.Distance(h.go.transform.position, player.position + Vector3.up * 0.9f);
            if (d < best) { best = d; chikai = h; }
        }
        float want = chikai != null ? 1f : 0f;
        if (chikai != null) {
            var tex = chikai.shu.Card;
            if (cardImg.texture != tex) cardImg.texture = tex;
            cardTxt.text = chikai.shu.name;
        }
        cardAlpha = Mathf.MoveTowards(cardAlpha, want, Time.deltaTime * 4f);
        cardGrp.alpha = cardAlpha;
    }

    // ---------------------------------------------------------------- 撮影用（-mushi）
    // tour の テレポート(1.5s)の あと 2.0s で 置き、撮影(2.9s)の 直前 2.85s で 画面座標を ログ
    int logGamen; bool debugOita;
    void LateUpdate() {
        if (debugMushi && !debugOita && Time.time >= 2.0f) { debugOita = true; DebugOki(); }
        if (debugMushi && debugOita && logGamen == 0 && Time.time >= 2.85f) logGamen = 1;
        if (logGamen <= 0) return;
        var c = Camera.main; if (c == null) return;
        var sb = new System.Text.StringBuilder();
        foreach (var h in ikiteru) {
            if (h.go == null) continue;
            var p = h.go.transform.position; var sp = c.WorldToScreenPoint(p);
            sb.Append(h.shu.id).Append(" w").Append(p.ToString("F1")).Append(" s(").Append((int)sp.x).Append(",").Append((int)(Screen.height - sp.y)).Append(",").Append(sp.z.ToString("F0")).Append(") ");
        }
        Debug.Log("[Probe] NiwaMushi gamen t=" + Time.time.ToString("F2") + " " + MuraCamFixed.PlaceName + " " + sb);
        logGamen = -1;
    }

    /// <summary>決まった 場所に 決まった 虫を 置く。撮って たしかめる ため</summary>
    void DebugOki() {
        Shu S(string id) { return shu.Find(x => x.id == id); }
        var cam = Camera.main; var o = Origin;
        var mieru = new List<Miki>();
        foreach (var m in miki) {
            var p = m.pts[0] + Vector3.up * 1.6f;
            if (cam != null) { var v = cam.WorldToViewportPoint(p); if (v.z < 0f || v.x < 0.05f || v.x > 0.95f || v.y < 0.1f || v.y > 0.95f) continue; }
            mieru.Add(m);
        }
        mieru.Sort((p, q) => Vector3.Distance(p.pts[0], o).CompareTo(Vector3.Distance(q.pts[0], o)));
        void MikiOki(string id, Miki m, float kakudo, float takasa, bool sakasa) {
            var s = S(id); if (s == null || m == null) return;
            var h = new Hiki { shu = s, miki = m, kakudo = kakudo, takasa = takasa, sakasa = sakasa, phase = Random.value * 10f, wait = 5f };
            Quaternion rot;
            if (!MikiNi(h, out h.pos, out rot)) return;
            h.home = h.pos; Oku(h);
        }
        void SoraOki(string id, Vector3 p, Vector3 heading) {
            var s = S(id); if (s == null) return;
            if (!Jimen(ref p)) return;
            var h = new Hiki { shu = s, phase = Random.value * 10f, heading = heading.normalized, wait = 9f };
            h.home = h.pos = h.target = p + Vector3.up * 1.5f;
            Oku(h);
        }
        if (mieru.Count > 0) { MikiOki("semi", mieru[0], -15f, 1.5f, false); MikiOki("kabuto", mieru[0], 18f, 1.0f, false); }
        if (mieru.Count > 1) MikiOki("kuwagata", mieru[1], 0f, 1.3f, true);
        SoraOki("tonbo", new Vector3(-3f, 0f, 2f), new Vector3(1f, 0f, 0.3f));
        SoraOki("oniyanma", new Vector3(0.5f, 0f, 3.5f), new Vector3(-1f, 0f, 0f));
        SoraOki("chou", new Vector3(3.5f, 0f, 1f), new Vector3(0.3f, 0f, -1f));
        {
            var s = S("batta");
            if (s != null) {
                var p = new Vector3(-1.5f, 0f, -1f);
                if (Jimen(ref p)) { var h = new Hiki { shu = s, phase = 1f, wait = 9f, heading = Vector3.right }; h.home = h.pos = p + Vector3.up * s.haba * 0.4f; Oku(h); }
            }
        }
        Debug.Log("[Probe] NiwaMushi debug oki " + ikiteru.Count + " miki=" + miki.Count + " mieru=" + mieru.Count);
    }
}
