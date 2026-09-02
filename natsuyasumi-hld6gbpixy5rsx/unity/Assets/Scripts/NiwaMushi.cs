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
// ★大きさは 実物の **2.5倍**。カブト 78mm → 0.2m。画面 98px/m で 20px＝読める。
// ★向き：
//   ・幹に とまる 虫（セミ・カブト・クワガタ）は **幹の 面に 貼りつける**（ビルボードに しない）。
//     カメラは 正面固定 なので、幹の 手前がわ ±60° の どこかに 置けば 見える。
//   ・飛ぶ 虫・草の 虫は ビルボード（MuraBillboard）。上から 見た 絵を 出す
//   ・ホタルは 夜だけ。絵は 小さく、光の 点で 見せる
// ★近づいたら 寄りカード（画面の 右下に 大きな 絵と 名まえ）。カメラは 寄れない ので ここで 見せる
public class NiwaMushi : MonoBehaviour {
    public enum Perch { Miki, Sora, Kusa, Shigemi }

    [System.Serializable]
    public class Shu {
        public string id;          // ファイル名の 頭（semi/kabuto/…）
        public string name;        // ひらがな
        public Perch perch;
        public float haba;         // 絵の 板の 大きさ(m)。実物の 2.5倍で 絵の 余白こみ
        public bool hiru, yoru;    // 出る 時間帯
        public Texture2D yoko, ue, naname;
        // ★材質は **組み立て時に BuildNiwa が 作って わたす**（主人公と 同じ やりかた）。
        //   実行時に Shader.Find("Natsuyasumi/PixelSprite") すると、そのシェーダが ビルドに
        //   入って いない 場面では null → 黙って Lit（不透明）に 落ち、透明の ところが
        //   **黒い 四角**に なった（2026-09-02）
        public Material zairyo;
        public Texture2D Sekai { get { return perch == Perch.Miki || perch == Perch.Kusa ? (yoko ?? ue) : (ue ?? yoko); } }
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
            // ★大きさは 実測で 決めた（2026-09-02）。2.5倍（カブト 0.26m）だと 幹の 上で 12px＝
            //   樹皮の 傷と 見わけが つかなかった。チョウ（0.32m）は 30〜60px で 読めた。
            //   幹に とまる 暗い 虫は 4倍、飛ぶ 虫は 3倍 を めやすに
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
        public Vector3 home; public float phase; public Light hikari;
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
        foreach (var h in ikiteru) Ugoku(h);
        Card();
    }

    // ---------------------------------------------------------------- 湧かせる
    bool Deru(Shu s) { return MuraDay.Night ? s.yoru : s.hiru; }

    void Waku() {
        if (ikiteru.Count >= kazu) return;
        var cand = shu.FindAll(s => Deru(s) && s.Sekai != null);
        if (cand.Count == 0) return;
        var s = cand[Random.Range(0, cand.Count)];
        Vector3 at; Quaternion rot;
        if (!Basho(s, out at, out rot)) return;
        Oku(s, at, rot);
    }

    /// <summary>時間帯が 変わって 出ない 虫に なったら 消す（朝に ホタルは いない）</summary>
    void Kataduke() {
        foreach (var h in ikiteru) if (!Deru(h.shu)) Destroy(h.go);
    }

    Vector3 Origin { get { return player != null ? player.position : transform.position; } }

    bool Basho(Shu s, out Vector3 at, out Quaternion rot) {
        at = Vector3.zero; rot = Quaternion.identity;
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
                    // 手前がわ ±40°（正面固定の カメラから 見える 面）
                    if (!MikiNi(m, Random.Range(-40f, 40f), Random.Range(0.9f, 1.9f), out at, out rot)) continue;
                    return true;
                }
                case Perch.Sora: {
                    var c = Random.insideUnitCircle * kouho * 0.7f;
                    at = new Vector3(o.x + c.x, 0f, o.z + c.y);
                    if (!Jimen(ref at)) continue;
                    at.y += Random.Range(1.0f, 2.0f);
                    return true;
                }
                case Perch.Kusa: {
                    var c = Random.insideUnitCircle * kouho * 0.6f;
                    at = new Vector3(o.x + c.x, 0f, o.z + c.y);
                    if (!Jimen(ref at)) continue;
                    at.y += s.haba * 0.45f;
                    return true;
                }
                default: {   // Shigemi＝夜の しげみ。木の 根もとの まわり
                    if (miki.Count == 0) return false;
                    var m = miki[Random.Range(0, miki.Count)];
                    var ne = m.pts[0];
                    if (new Vector2(ne.x - o.x, ne.z - o.z).magnitude > kouho) continue;
                    var c = Random.insideUnitCircle.normalized * Random.Range(0.8f, 2.2f);
                    at = new Vector3(ne.x + c.x, ne.y + 0.2f + Random.Range(0.3f, 1.2f), ne.z + c.y);
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>幹の 面に 貼る 位置と 向き。角度は 手前(-Z)を 0° として 左右へ。
    /// 背骨の 輪を その 高さで 補間して **実際の 芯と 半径**を つかう</summary>
    bool MikiNi(Miki m, float kakudo, float takasa, out Vector3 at, out Quaternion rot) {
        at = Vector3.zero; rot = Quaternion.identity;
        if (m.pts == null || m.pts.Length < 2) return false;
        float y = m.pts[0].y + 0.2f + takasa;          // pts[0] は 地めんの 0.2m 下
        int i = 0;
        while (i < m.pts.Length - 2 && m.pts[i + 1].y < y) i++;
        float t = Mathf.InverseLerp(m.pts[i].y, m.pts[i + 1].y, y);
        var shin = Vector3.Lerp(m.pts[i], m.pts[i + 1], t);
        float r = Mathf.Lerp(m.rad[i], m.rad[i + 1], t) + 0.02f;
        float a = kakudo * Mathf.Deg2Rad;
        var soto = new Vector3(Mathf.Sin(a), 0f, -Mathf.Cos(a));   // 幹から 外へ
        at = shin + soto * r;
        // 板の 見える 面（local -Z）を 外へ 向ける＝forward は 幹の 中へ。少し 上を 向かせて 日を うける
        rot = Quaternion.LookRotation(-soto, Vector3.up) * Quaternion.Euler(-12f, 0f, 0f);
        return true;
    }

    static bool Jimen(ref Vector3 p) {
        RaycastHit hit;
        if (!Physics.Raycast(p + Vector3.up * 12f, Vector3.down, out hit, 30f)) return false;
        p.y = hit.point.y;
        return true;
    }

    void Oku(Shu s, Vector3 at, Quaternion rot) {
        var go = new GameObject("Mushi_" + s.id);
        go.transform.SetParent(transform, false);
        go.transform.position = at;
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Ita";
        Destroy(quad.GetComponent<Collider>());
        quad.transform.SetParent(go.transform, false);
        quad.transform.localScale = new Vector3(s.haba, s.haba, 1f);
        var r = quad.GetComponent<Renderer>();
        r.sharedMaterial = Zairyo(s);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.layer = 2;
        if (s.perch == Perch.Miki) go.transform.rotation = rot;
        else go.AddComponent<MuraBillboard>();
        var h = new Hiki { shu = s, go = go, ita = quad.transform, home = at, phase = Random.value * 10f };
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
        var sh = Shader.Find("Natsuyasumi/PixelSprite") ?? Shader.Find("Universal Render Pipeline/Lit");
        m = new Material(sh);
        var t = s.Sekai;
        m.SetTexture("_BaseMap", t); m.mainTexture = t;
        if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", 0.45f);
        if (m.HasProperty("_BreatheAmp")) m.SetFloat("_BreatheAmp", 0f);
        if (m.HasProperty("_SwayAmp")) m.SetFloat("_SwayAmp", 0f);
        if (m.HasProperty("_HoleIgnore")) m.SetFloat("_HoleIgnore", 1f);   // 主人公の まわりの 穴で 消さない
        if (m.HasProperty("_Wrap")) m.SetFloat("_Wrap", 0.7f);
        zairyo[s.id] = m;
        return m;
    }

    // ---------------------------------------------------------------- 動き（最小）
    void Ugoku(Hiki h) {
        float t = Time.time + h.phase;
        switch (h.shu.perch) {
            case Perch.Miki:
                // とまって いる。ときどき ほんの 少し ずれる（生きて いる 気配だけ）
                h.go.transform.position = h.home + h.go.transform.right * Mathf.Sin(t * 0.6f) * 0.01f
                                                 + Vector3.up * Mathf.Sin(t * 0.9f) * 0.008f;
                break;
            case Perch.Sora:
                // ただよう。トンボは すっと 止まって また 動く
                h.go.transform.position = h.home + new Vector3(Mathf.Sin(t * 0.8f) * 1.2f,
                                                               Mathf.Sin(t * 1.7f) * 0.25f,
                                                               Mathf.Cos(t * 0.6f) * 0.9f);
                break;
            case Perch.Kusa:
                // ときどき はねる
                float hop = Mathf.Max(0f, Mathf.Sin(t * 2.2f)) * Mathf.Max(0f, Mathf.Sin(t * 0.37f));
                h.go.transform.position = h.home + Vector3.up * hop * 0.25f;
                break;
            default:
                h.go.transform.position = h.home + new Vector3(Mathf.Sin(t * 0.5f) * 0.6f,
                                                               Mathf.Sin(t * 0.9f) * 0.3f,
                                                               Mathf.Cos(t * 0.4f) * 0.6f);
                if (h.hikari != null) {
                    float k = Mathf.Pow(Mathf.Clamp01(Mathf.Sin(t * 1.6f) * 0.5f + 0.5f), 3f);
                    h.hikari.intensity = 0.2f + k * 2.0f;
                }
                break;
        }
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
    /// <summary>決まった 場所に 決まった 虫を 置く。撮って たしかめる ため</summary>
    void DebugOki() {
        Shu S(string id) { return shu.Find(x => x.id == id); }
        Vector3 at; Quaternion rot;
        // 主人公に 近く、カメラに 入って いる 幹を 近い 順に
        var cam = Camera.main; var o = Origin;
        var mieru = new List<Miki>();
        foreach (var m in miki) {
            var p = m.pts[0] + Vector3.up * 1.6f;
            if (cam != null) { var v = cam.WorldToViewportPoint(p); if (v.z < 0f || v.x < 0.05f || v.x > 0.95f || v.y < 0.1f || v.y > 0.95f) continue; }
            mieru.Add(m);
        }
        mieru.Sort((p, q) => Vector3.Distance(p.pts[0], o).CompareTo(Vector3.Distance(q.pts[0], o)));
        Debug.Log("[Probe] NiwaMushi mieru miki " + mieru.Count);
        if (mieru.Count > 0 && S("semi") != null && MikiNi(mieru[0], -15f, 1.5f, out at, out rot)) Oku(S("semi"), at, rot);
        if (mieru.Count > 0 && S("kabuto") != null && MikiNi(mieru[0], 20f, 1.0f, out at, out rot)) Oku(S("kabuto"), at, rot);
        if (mieru.Count > 1 && S("kuwagata") != null && MikiNi(mieru[1], 0f, 1.3f, out at, out rot)) Oku(S("kuwagata"), at, rot);
        if (S("tonbo") != null) { var p = new Vector3(-3f, 0f, 2f); if (Jimen(ref p)) Oku(S("tonbo"), p + Vector3.up * 1.5f, Quaternion.identity); }
        if (S("chou") != null) { var p = new Vector3(3.5f, 0f, 1f); if (Jimen(ref p)) Oku(S("chou"), p + Vector3.up * 1.2f, Quaternion.identity); }
        if (S("batta") != null) { var p = new Vector3(-1.5f, 0f, -1f); if (Jimen(ref p)) Oku(S("batta"), p + Vector3.up * 0.08f, Quaternion.identity); }
        Debug.Log("[Probe] NiwaMushi debug oki " + ikiteru.Count + " miki=" + miki.Count);
        logGamen = 3;
    }
    int logGamen;
    bool debugOita;
    void LateUpdate() {
        if (debugMushi && !debugOita && Time.time >= 2.0f) { debugOita = true; DebugOki(); logGamen = 0; }
        if (debugMushi && debugOita && logGamen == 0 && Time.time >= 2.85f) logGamen = 1;
        if (logGamen <= 0) return;
        if (--logGamen > 0) return;
        var c = Camera.main; if (c == null) return;
        var sb = new System.Text.StringBuilder();
        foreach (var h in ikiteru) {
            var p = h.go.transform.position; var sp = c.WorldToScreenPoint(p);
            sb.Append(h.shu.id).Append(" w").Append(p.ToString("F1")).Append(" s(").Append((int)sp.x).Append(",").Append((int)(Screen.height - sp.y)).Append(",").Append(sp.z.ToString("F0")).Append(") ");
        }
        Debug.Log("[Probe] NiwaMushi gamen t=" + Time.time.ToString("F2") + " " + MuraCamFixed.PlaceName + " " + sb);
        logGamen = -1;
    }
}
