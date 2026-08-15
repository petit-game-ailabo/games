using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 田舎の 遊び を まとめて 引きうける ところ。
//
// ★手ざわりの きまり（虫ずもうで しくじった ところから）
//   - **連打は させない。** どの 遊びも 押すのは 1回か 2回。
//     ねらう のは 間あいと 度あいで あって、指の はやさでは ない
//   - **技の キーと 送りの キーを 分ける。** 結果の 画が 連打で 飛ばない ように
//   - 待つ 遊び（つり）は **待って いる ことが 絵で 分かる**ように する
//
// 遊びの 場所は PlaySpot が もつ。ここは その 中身だけ。
public class PlayHost : MonoBehaviour {
    public Texture2D atlas;          // play.png（0=ささぶね 1=うき 2=石 3=はな 4=さかな 5=えだ）
    public Font font;
    public Sprite panel;

    const int Cols = 6;
    public const int IconBoat = 0, IconFloat = 1, IconStone = 2, IconFlower = 3, IconFish = 4, IconBranch = 5;

    BugHud hud;
    PlayerMove move;
    PlaySpot near;
    Coroutine running;

    // ---- 画面（ゲージ 1本と ひとこと。ちいさく すませる）
    RectTransform ui;
    Image gaugeBack, gaugeFill, gaugeMark;
    Text uiLine;

    /// <summary>遊びの さいちゅう。あみや ずもうは 手を 出さない</summary>
    public bool Busy { get { return running != null; } }

    // ---- 記録
    const string KeyBoat  = "natsuyasumi.play.boats.v1";
    const string KeySkip  = "natsuyasumi.play.skip.v1";     // 水きりの さいこう記録
    const string KeyFish  = "natsuyasumi.play.fish.v1";
    const string KeyFlow  = "natsuyasumi.play.flowers.v1";  // つんだ 花（色水・押し花の もと）
    const string KeyOshi  = "natsuyasumi.play.oshibana.v1";
    const string KeyIrozu = "natsuyasumi.play.irozu.v1";
    const string KeyBase  = "natsuyasumi.play.himitsu.v1";  // ひみつきちの できぐあい 0〜4

    public int Boats   { get { return PlayerPrefs.GetInt(KeyBoat, 0); } }
    public int BestSkip{ get { return PlayerPrefs.GetInt(KeySkip, 0); } }
    public int Fish    { get { return PlayerPrefs.GetInt(KeyFish, 0); } }
    public int Flowers { get { return PlayerPrefs.GetInt(KeyFlow, 0); } }
    public int Oshibana{ get { return PlayerPrefs.GetInt(KeyOshi, 0); } }
    public int Irozu   { get { return PlayerPrefs.GetInt(KeyIrozu, 0); } }
    public int BaseStep{ get { return PlayerPrefs.GetInt(KeyBase, 0); } }

    static void Bump(string key, int by) {
        PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key, 0) + by);
        PlayerPrefs.Save();
    }

    void Start() {
        hud = FindFirstObjectByType<BugHud>();
        move = GetComponent<PlayerMove>();
        BuildUI();
    }

    /// <summary>いま そばに ある 遊び場（足もとの ひとことは BugCatcher が まとめて 出す）</summary>
    public PlaySpot NearSpot { get { return Busy ? null : near; } }

    float scanLeft;
    PlaySpot[] all;

    void Update() {
        if (Busy) return;
        // **場面ぜんぶを 毎フレーム さがさない。** 遊び場は 動かないので 1度 集めて おき、
        // 近さだけを ときどき 見る
        if (all == null) all = FindObjectsByType<PlaySpot>(FindObjectsSortMode.None);
        scanLeft -= Time.deltaTime;
        if (scanLeft > 0f) return;
        scanLeft = 0.15f;
        near = Nearest();
    }

    PlaySpot Nearest() {
        if (all == null) return null;
        PlaySpot best = null; float bd = float.MaxValue;
        foreach (var s in all) {
            if (s == null || !s.Near(transform)) continue;
            float d = (s.transform.position - transform.position).sqrMagnitude;
            if (d < bd) { bd = d; best = s; }
        }
        return best;
    }

    /// <summary>そばに 遊び場が あれば 始める。始めたら true</summary>
    public bool TryBegin() {
        if (Busy) return false;
        var s = Nearest();
        if (s == null) return false;
        running = StartCoroutine(Play(s));
        return true;
    }

    /// <summary>たしかめの 自動運転から。どの 遊びを 何回 押すか</summary>
    public bool DebugBegin(PlayKind kind) {
        if (Busy) return false;
        foreach (var s in FindObjectsByType<PlaySpot>(FindObjectsSortMode.None)) {
            if (s.kind != kind) continue;
            running = StartCoroutine(Play(s));
            return true;
        }
        Debug.Log("[PlayHost] 遊び場が ない: " + kind);
        return false;
    }
    bool debugPress;
    public void DebugPress() { debugPress = true; }
    // ★**自動運転は 上手に 遊ぶ。**
    //   ただ 一定間かくで 押させると、まぐれで 0点に なった のか
    //   しくみが 壊れて いるのか 区別が つかない。
    //   ねらい所で 押す ようにして、**うまく いった ときの 画**を 確かめる
    [HideInInspector] public bool debugAuto;
    public string DebugState {
        get {
            return string.Format("ふね={0} 水きり最高={1} さかな={2} 花={3} 色水={4} おし花={5} きち={6}",
                                 Boats, BestSkip, Fish, Flowers, Irozu, Oshibana, BaseStep);
        }
    }

    bool Pressed() {
        if (debugPress) { debugPress = false; return true; }
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.J)
            || Input.GetMouseButtonDown(0);
    }

    IEnumerator Play(PlaySpot s) {
        if (move != null) move.locked = true;
        if (hud != null) hud.SetPrompt(null);
        switch (s.kind) {
            case PlayKind.Sasabune:  yield return Sasabune(s); break;
            case PlayKind.Mizukiri:  yield return Mizukiri(s); break;
            case PlayKind.Tsuri:     yield return Tsuri(s); break;
            case PlayKind.Hanatsumi: yield return Hanatsumi(s); break;
            case PlayKind.Irozu:     yield return Irozu2(s); break;
            case PlayKind.Oshibana:  yield return Oshibana2(s); break;
            default:                 yield return Himitsu(s); break;
        }
        ShowGauge(false);
        if (move != null) move.locked = false;
        running = null;
    }

    // ================= ささぶね =================
    // 作って ながす だけ。**むずかしさは 要らない。**
    // 見て いる あいだ 舟が 遠ざかって いく、それが この 遊びの ぜんぶ
    IEnumerator Sasabune(PlaySpot s) {
        Line("ささの 葉を 裂いて…");
        yield return new WaitForSeconds(0.9f);

        var boat = MakeIcon(IconBoat, 0.34f);
        boat.transform.position = s.water + Vector3.up * 0.04f;
        var dir = s.flow.sqrMagnitude > 0.01f ? s.flow.normalized : Vector3.forward;

        Bump(KeyBoat, 1);
        Line("ながれて いった　（つうさん " + Boats + " そう）");

        // 下って いく。ゆらぎながら 小さく なって 消える
        float t = 0f;
        while (t < 9f) {
            t += Time.deltaTime;
            boat.transform.position += dir * (0.9f + Mathf.Sin(t * 1.7f) * 0.12f) * Time.deltaTime
                                     + Vector3.right * Mathf.Sin(t * 2.3f) * 0.06f * Time.deltaTime;
            // 舟は そのまま 流す。人は とちゅうで 歩きだせる ように 早めに 手を はなす
            if (t > 1.6f) break;
            yield return null;
        }
        // **あとは 舟に まかせる。**川下に 見えなく なるまで ひとりでに 流れる
        var d = boat.AddComponent<Drifter>();
        d.dir = dir; d.speed = 0.95f; d.life = 14f;
        yield return new WaitForSeconds(0.3f);
    }

    // ================= 水きり =================
    // **2回 おす。** 1回めで 力、2回めで 角度。連打では どうにも ならない。
    // 角度は「ねらいの 帯」に 入れる。ひらたい 石を 低く 投げるのが こつ、
    // という 本ものの 感じを ゲージの あたりの 位置で 表す
    IEnumerator Mizukiri(PlaySpot s) {
        float power = 0f, angle = 0f;

        Line("ちからを ためる…　スペースで きめる");
        ShowGauge(true, 0f);
        float t = 0f;
        while (true) {
            t += Time.deltaTime * 1.15f;
            power = Mathf.PingPong(t, 1f);
            SetGauge(power, -1f);
            if (debugAuto ? power > 0.92f : Pressed()) break;
            yield return null;
        }
        yield return new WaitForSeconds(0.25f);

        // **ねらいの 帯は まん中より 下。** 石は 低く 投げる ほうが よく はねる
        const float Sweet = 0.34f, Width = 0.12f;
        Line("なげる 角ど　　ひくいほど よく はねる");
        t = 0f;
        while (true) {
            t += Time.deltaTime * 1.45f;
            angle = Mathf.PingPong(t, 1f);
            SetGauge(angle, Sweet);
            if (debugAuto ? Mathf.Abs(angle - Sweet) < 0.02f : Pressed()) break;
            yield return null;
        }
        ShowGauge(false);

        float miss = Mathf.Abs(angle - Sweet);
        float acc = Mathf.Clamp01(1f - miss / (Width * 3f));        // 1＝どんぴしゃ
        int skips = Mathf.RoundToInt(power * 7f * acc * acc);
        if (miss > Width * 3.2f) skips = 0;

        // 石を とばす。はねる ごとに 水しぶき
        var stone = MakeIcon(IconStone, 0.26f);
        var dir = s.flow.sqrMagnitude > 0.01f ? Vector3.Cross(Vector3.up, s.flow.normalized) : Vector3.forward;
        if (Vector3.Dot(dir, s.water - transform.position) < 0f) dir = -dir;
        Vector3 from = s.water + Vector3.up * 0.5f - dir * 0.6f;
        stone.transform.position = from;

        if (skips <= 0) {
            // ぼちゃん
            var to = from + dir * 1.6f;
            for (float k = 0f; k < 1f; k += Time.deltaTime * 2.6f) {
                stone.transform.position = Vector3.Lerp(from, to, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 0.5f - k * 0.5f);
                yield return null;
            }
            Splash(to, 0.9f);
            Line("ぼちゃん…　石が ひらたく なかった");
        } else {
            float hop = (s.span * 0.9f) / skips;
            Vector3 at = from;
            for (int i = 0; i < skips; i++) {
                var to = at + dir * hop;
                to.y = s.water.y;
                float h = Mathf.Lerp(0.55f, 0.12f, i / (float)Mathf.Max(1, skips - 1));
                for (float k = 0f; k < 1f; k += Time.deltaTime * 4.2f) {
                    stone.transform.position = Vector3.Lerp(at, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * h;
                    yield return null;
                }
                Splash(to, 0.5f);
                Line((i + 1) + " だん！");
                at = to;
            }
            if (skips > BestSkip) {
                PlayerPrefs.SetInt(KeySkip, skips); PlayerPrefs.Save();
                Line(skips + " だん！　★じこ さいこう きろく");
            } else Line(skips + " だん　（さいこう " + BestSkip + "）");
        }
        Destroy(stone, 0.6f);
        yield return new WaitForSeconds(1.1f);
    }

    // ================= つり =================
    // **待つ 遊び。** うきが しずんだ 合図の あとの ひと呼吸で あわせる。
    // 早おしは 空ぶり＝連打では 取れない
    IEnumerator Tsuri(PlaySpot s) {
        Line("いとを たらした…");
        var uki = MakeIcon(IconFloat, 0.30f);
        uki.transform.position = s.water + Vector3.up * 0.12f;

        float wait = Random.Range(2.2f, 6.5f);
        float t = 0f;
        bool early = false;
        while (t < wait) {
            t += Time.deltaTime;
            var p = s.water + Vector3.up * (0.12f + Mathf.Sin(t * 2.4f) * 0.03f);
            uki.transform.position = p;
            // ときどき 前ぶれ（さそいの あたり）。ここで 押すと 空ぶり
            if (!debugAuto && Pressed()) { early = true; break; }
            yield return null;
        }
        if (early) {
            Line("はやい！　まだ かかって いない");
            Destroy(uki); yield return new WaitForSeconds(1.0f); yield break;
        }

        // ★あたり。うきが しずむ
        Line("！");
        Splash(s.water, 0.4f);
        float window = 0.85f;
        bool hooked = false;
        for (float k = 0f; k < window; k += Time.deltaTime) {
            uki.transform.position = s.water + Vector3.up * (0.12f - k * 0.5f);
            if (debugAuto ? k > 0.15f : Pressed()) { hooked = true; break; }
            yield return null;
        }
        Destroy(uki);
        if (!hooked) { Line("にげられた…"); yield return new WaitForSeconds(1.1f); yield break; }

        // かかった。引きあげる
        var fishName = FishNames[Random.Range(0, FishNames.Length)];
        int mm = Random.Range(70, 240);
        var fish = MakeIcon(IconFish, 0.34f + mm / 900f);
        Vector3 a = s.water, b = transform.position + Vector3.up * 1.1f;
        for (float k = 0f; k < 1f; k += Time.deltaTime * 1.8f) {
            fish.transform.position = Vector3.Lerp(a, b, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.6f;
            yield return null;
        }
        Bump(KeyFish, 1);
        Line(fishName + "　" + mm + "mm　が つれた！　（つうさん " + Fish + " ひき）");
        Destroy(fish, 1.2f);
        yield return new WaitForSeconds(1.3f);
    }

    static readonly string[] FishNames = { "おいかわ", "かわむつ", "うぐい", "どじょう", "ふな", "やまめ" };

    // ================= 花つみ =================
    IEnumerator Hanatsumi(PlaySpot s) {
        Line("しゃがんで 花を つんだ");
        var f = MakeIcon(IconFlower, 0.30f);
        f.transform.position = transform.position + Vector3.up * 0.5f;
        for (float k = 0f; k < 1f; k += Time.deltaTime * 1.6f) {
            f.transform.position = transform.position + Vector3.up * (0.5f + k * 0.7f);
            yield return null;
        }
        Destroy(f);
        int n = Random.Range(1, 4);
        Bump(KeyFlow, n);
        Line("はなを " + n + "本 つんだ　（もちもの " + Flowers + "本）");
        yield return new WaitForSeconds(1.0f);
    }

    // ================= 色水 =================
    IEnumerator Irozu2(PlaySpot s) {
        if (Flowers < 3) { Line("花が たりない（3本 いる）"); yield return new WaitForSeconds(1.2f); yield break; }
        Line("花を もんで…");
        yield return new WaitForSeconds(1.0f);
        PlayerPrefs.SetInt(KeyFlow, Flowers - 3);
        Bump(KeyIrozu, 1);
        var col = IrozuNames[Random.Range(0, IrozuNames.Length)];
        Line(col + "の 色水が できた　（つうさん " + Irozu + " ぱい）");
        yield return new WaitForSeconds(1.4f);
    }
    static readonly string[] IrozuNames = { "むらさき", "うすあか", "あお", "きいろ", "ももいろ" };

    // ================= おし花 =================
    IEnumerator Oshibana2(PlaySpot s) {
        if (Flowers < 1) { Line("花を もって いない"); yield return new WaitForSeconds(1.2f); yield break; }
        Line("本に はさんだ。かわくまで しばらく…");
        yield return new WaitForSeconds(1.3f);
        PlayerPrefs.SetInt(KeyFlow, Flowers - 1);
        Bump(KeyOshi, 1);
        Line("おし花が " + Oshibana + " まいに なった");
        yield return new WaitForSeconds(1.2f);
    }

    // ================= ひみつきち =================
    // 一気には できない。**来る たびに すこしずつ 建つ**のが この 遊びの 芯
    static readonly string[] BaseSteps = {
        "やぶを かき分けて 場所を 作った",
        "えだを ならべて かべに した",
        "板きれを わたして 屋根に した",
        "木の 箱を もちこんで つくえに した",
        "はたを 立てた。もう だれにも 見つからない",
    };
    IEnumerator Himitsu(PlaySpot s) {
        int step = BaseStep;
        if (step >= BaseSteps.Length) {
            Line("ひみつきちは できあがって いる");
            yield return new WaitForSeconds(1.2f); yield break;
        }
        Line("…");
        var e = MakeIcon(IconBranch, 0.40f);
        e.transform.position = s.transform.position + Vector3.up * 1.2f;
        for (float k = 0f; k < 1f; k += Time.deltaTime * 1.2f) {
            e.transform.position = s.transform.position + Vector3.up * (1.2f - k * 0.9f);
            yield return null;
        }
        Destroy(e);
        PlayerPrefs.SetInt(KeyBase, step + 1); PlayerPrefs.Save();
        Line(BaseSteps[step] + "　（" + (step + 1) + "/" + BaseSteps.Length + "）");
        yield return new WaitForSeconds(1.6f);
    }

    // ---- 見た目の 部品
    GameObject MakeIcon(int index, float size) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "PlayIcon";
        Destroy(go.GetComponent<Collider>());
        go.transform.localScale = new Vector3(size, size, 1f);
        go.AddComponent<Billboard>();
        var r = go.GetComponent<Renderer>();
        var sh = Shader.Find("Natsuyasumi/PixelSprite") ?? Shader.Find("Sprites/Default");
        var m = new Material(sh);
        if (atlas != null) {
            m.SetTexture("_BaseMap", atlas);
            m.mainTexture = atlas;
            var st = new Vector4(1f / Cols, 1f, index / (float)Cols, 0f);
            m.SetVector("_BaseMap_ST", st);
            m.SetTextureScale("_BaseMap", new Vector2(st.x, st.y));
            m.SetTextureOffset("_BaseMap", new Vector2(st.z, st.w));
        }
        if (m.HasProperty("_BreatheAmp")) m.SetFloat("_BreatheAmp", 0f);
        if (m.HasProperty("_SwayAmp")) m.SetFloat("_SwayAmp", 0f);
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    void Splash(Vector3 at, float scale) {
        var go = new GameObject("Splash");
        go.transform.position = at;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.5f * scale + 0.2f; main.startSpeed = 1.8f * scale;
        main.startSize = 0.07f * scale + 0.03f;
        main.maxParticles = 30; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 1.6f;
        main.startColor = new Color(0.85f, 0.94f, 1f, 0.95f);
        var em = ps.emission; em.enabled = true; em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.RoundToInt(10 * scale + 4)) });
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.08f;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")
                                  ?? Shader.Find("Sprites/Default"));
        r.material.SetFloat("_Surface", 1);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Destroy(go, 1.4f);
    }

    void Line(string s) { if (hud != null) hud.Say(s); if (uiLine != null) uiLine.text = s; }

    void ShowGauge(bool on, float v = 0f) {
        if (ui == null) return;
        ui.gameObject.SetActive(on);
        if (on) SetGauge(v, -1f);
    }

    void SetGauge(float v, float mark) {
        if (gaugeFill == null) return;
        gaugeFill.rectTransform.sizeDelta = new Vector2(Mathf.Clamp01(v) * 220f, 10f);
        if (gaugeMark != null) {
            bool on = mark >= 0f;
            if (gaugeMark.gameObject.activeSelf != on) gaugeMark.gameObject.SetActive(on);
            if (on) gaugeMark.rectTransform.anchoredPosition = new Vector2(mark * 220f, 0f);
        }
    }

    void BuildUI() {
        var host = hud != null && hud.CanvasRoot != null ? hud.CanvasRoot : null;
        if (host == null) return;

        var go = new GameObject("PlayUI", typeof(Image));
        go.transform.SetParent(host, false);
        ui = go.GetComponent<RectTransform>();
        ui.anchorMin = ui.anchorMax = ui.pivot = new Vector2(0.5f, 0f);
        ui.anchoredPosition = new Vector2(0f, 96f);
        ui.sizeDelta = new Vector2(250f, 46f);
        var bg = go.GetComponent<Image>();
        bg.sprite = panel; bg.type = Image.Type.Sliced; bg.raycastTarget = false;
        if (panel == null) bg.color = new Color(0.12f, 0.10f, 0.09f, 0.88f);

        var back = new GameObject("Back", typeof(Image));
        back.transform.SetParent(ui, false);
        var brt = back.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(220f, 10f);
        brt.anchoredPosition = new Vector2(0f, -6f);
        gaugeBack = back.GetComponent<Image>();
        gaugeBack.color = new Color(1f, 1f, 1f, 0.16f); gaugeBack.raycastTarget = false;

        var fill = new GameObject("Fill", typeof(Image));
        fill.transform.SetParent(back.transform, false);
        var frt = fill.GetComponent<RectTransform>();
        frt.anchorMin = frt.anchorMax = new Vector2(0f, 0.5f);
        frt.pivot = new Vector2(0f, 0.5f);
        frt.anchoredPosition = Vector2.zero; frt.sizeDelta = new Vector2(0f, 10f);
        gaugeFill = fill.GetComponent<Image>();
        gaugeFill.color = new Color(0.56f, 0.86f, 0.35f); gaugeFill.raycastTarget = false;

        // ねらいの 帯（水きりの 角ど）
        var mk = new GameObject("Mark", typeof(Image));
        mk.transform.SetParent(back.transform, false);
        var mrt = mk.GetComponent<RectTransform>();
        mrt.anchorMin = mrt.anchorMax = new Vector2(0f, 0.5f);
        mrt.pivot = new Vector2(0.5f, 0.5f);
        mrt.sizeDelta = new Vector2(26f, 16f);
        gaugeMark = mk.GetComponent<Image>();
        gaugeMark.color = new Color(1f, 0.85f, 0.35f, 0.45f); gaugeMark.raycastTarget = false;
        mk.SetActive(false);

        var tx = new GameObject("Line", typeof(Text));
        tx.transform.SetParent(ui, false);
        var trt = tx.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(8f, 16f); trt.offsetMax = new Vector2(-8f, -6f);
        uiLine = tx.GetComponent<Text>();
        uiLine.font = font; uiLine.fontSize = 12;
        uiLine.alignment = TextAnchor.MiddleCenter;
        uiLine.color = new Color(0.96f, 0.95f, 0.88f);
        uiLine.horizontalOverflow = HorizontalWrapMode.Overflow;
        uiLine.verticalOverflow = VerticalWrapMode.Overflow;
        uiLine.raycastTarget = false; uiLine.supportRichText = false;

        ui.gameObject.SetActive(false);
    }
}
