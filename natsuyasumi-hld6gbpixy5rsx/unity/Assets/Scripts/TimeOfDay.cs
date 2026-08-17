using System;
using UnityEngine;
using UnityEngine.Rendering;

// 時間帯で 光を まるごと 切りかえる（あさ／ひる／ゆうがた／よる）。
// 太陽の むき・色・強さ、まわりの 明るさ、もや、行灯の 火 までを ひとまとめに 変える。
//
// ★2026-08-15：**時計を つないだ。**
//   遊んで いる あいだ 時間が 進み、あさ→ひる→ゆうがた→よる と めぐる。
//   4つの 決めうちを **つなぎめ なく 混ぜる**ように 作りなおした。
//   前は 段で 切りかわって いたので、混ぜられる ように 値を ひとまとめ(Preset)に した。
//   絵の たしかめは exeの 引数 -tod yoru（時計を 止めて 決めうち）／-clock 17.5（時こく 指定）
[ExecuteAlways]
public class TimeOfDay : MonoBehaviour {
    public enum Tod { Asa, Hiru, Yugata, Yoru }

    [Header("いまの 時間帯")]
    public Tod tod = Tod.Asa;

    [Header("時計")]
    [Tooltip("時間が 進む")]
    public bool runClock = true;
    // ★**「時間が 進む」と「時こくで 光を 決める」は 別もの。**
    //   いっしょに して いた ため、-clock 20 で 撮っても -tod の あさの ままだった
    //  （時計を 止めた 時点で hour を 見なく なって いた）。
    //   絵を 見くらべる ときは「時こくは 決めうち、でも その 時こくの 光」が 要る
    [Tooltip("時こく(hour)で 光を 決める。off なら tod の 決めうち")]
    public bool useHour = true;
    [Tooltip("いまの 時こく（0〜24）")]
    public float hour = 6.5f;
    [Tooltip("ひと日が 何分で めぐるか。**夏休みの 一日を 遊びきれる 長さ**に する")]
    public float realMinutesPerDay = 42f;
    [Tooltip("光を 塗りなおす 間かく（秒）。毎フレームは もったいない")]
    public float refreshEvery = 0.35f;
    float refreshLeft;

    [Header("つなぐ もの（自動で 割りあて）")]
    public Light sun;
    public Light fill;
    public Light andon;
    // ★**あかりは 家に 1つでは 足りない。**（2026-08-17）
    //   2階の つくえで 絵日記を 書く のに、部屋が まっ暗だった。
    //   同じ 明るさで つく あかりを ここに ならべる（母屋の 2階・離れ など）
    public Light[] andonHoka;

    // ★**8月の あいだに 日が みじかく なる。**（2026-08-17・遊ぶ 人の 指摘）
    //   「TimeOfDay の 日の入りを、31日かけて 30分ずつ 早める。
    //     『8月末は 5時には もう 暗い』——これだけで 終わりが 近い ことが 体で わかります」
    //   時こく そのものは いじらず、**時こく → 見た目 の あてはめを ずらす**。
    //   1日で hiZure 分だけ 夕方が 早く 来る＝月末には 40分 早い
    [Tooltip("ひと月で どれだけ 日の入りが 早く なるか（時間）")]
    public float mijikaku = 0.67f;
    [HideInInspector] public Nikki nikki;

    /// <summary>見た目を 決める ときの「みかけの 時こく」。日づけで うしろへ ずれる</summary>
    public float MikakeHour {
        get {
            if (nikki == null) return hour;
            float t = Mathf.Clamp01((nikki.day - 1) / 30f);
            // 昼の あいだは ずらさない。**夕方から 先だけ** 早める
            if (hour < 12f) return hour;
            return hour + mijikaku * t;
        }
    }
    public Renderer shojiPaper;    // 障子紙。よるは 光らせない
    public Camera cam;
    public Weather weather;        // 天気は この 上に かぶせる（順番を 固定するため ここから 呼ぶ）
    public CamOrbit orbit;         // 見せ場では もやを 薄く する（遠くを 見せる ため）

    [Header("空")]
    // 手続きで 描く 空（Natsuyasumi/Sky）。時間帯ごとに 色を 入れかえる
    public Material skybox;

    [Header("そとの 場面か")]
    // 屋内は「屋根を はずした 切りぬき」なので まわりは 暗いのが 正しい。
    // そとの 場面で 同じ ことを すると **地平線から さきが まっ黒**に なるので、空の 色を 出す
    public bool outdoor = false;

    /// <summary>いまの 時こくの 文字（HUDに 出す）</summary>
    public string ClockText {
        get {
            int h = Mathf.FloorToInt(hour) % 24;
            int m = Mathf.FloorToInt((hour - Mathf.Floor(hour)) * 60f);
            return string.Format("{0}:{1:00}", h, m);
        }
    }

    void OnEnable()   { AutoWire(); ApplyFromArgs(); Apply(); }
    void OnValidate() { AutoWire(); Apply(); }

    void Update() {
        if (!Application.isPlaying) return;
        if (runClock) {
            float perSec = 24f / Mathf.Max(0.5f, realMinutesPerDay * 60f);
            hour += perSec * Time.deltaTime;
            if (hour >= 24f) hour -= 24f;
        }
        // **時計を 止めて いても 塗りなおす。** 見せ場の もやの こさが 変わるので、
        // ここで 回さないと 高台に のぼっても もやが 薄く ならない
        refreshLeft -= Time.deltaTime;
        if (refreshLeft <= 0f) { refreshLeft = refreshEvery; Apply(); }
    }

    void AutoWire() {
        if (sun == null)   { var g = GameObject.Find("Sun");   if (g) sun = g.GetComponent<Light>(); }
        if (fill == null)  { var g = GameObject.Find("Fill");  if (g) fill = g.GetComponent<Light>(); }
        if (andon == null) { var g = GameObject.Find("Andon_Light"); if (g) andon = g.GetComponent<Light>(); }
        if (cam == null)   cam = Camera.main;
        if (weather == null) weather = FindFirstObjectByType<Weather>();
        if (orbit == null) orbit = FindFirstObjectByType<CamOrbit>();
    }

    void ApplyFromArgs() {
        var a = Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++) {
            if (a[i] == "-clock") {
                // 時こくを 決めうちに する（時計は 止めるが、光は その 時こくの もの）
                float h;
                if (float.TryParse(a[i + 1], out h)) {
                    hour = Mathf.Repeat(h, 24f); runClock = false; useHour = true;
                }
                continue;
            }
            if (a[i] != "-tod") continue;
            // **-tod は 時計を 止めて 決めうちに する。**
            // 絵を 見くらべる ときに 進んで いては 比べられない
            runClock = false; useHour = false;
            switch (a[i + 1].ToLower()) {
                case "asa":    tod = Tod.Asa;    hour = 6.5f;  break;
                case "hiru":   tod = Tod.Hiru;   hour = 12f;   break;
                case "yugata": tod = Tod.Yugata; hour = 18f;   break;
                case "yoru":   tod = Tod.Yoru;   hour = 21.5f; break;
            }
        }
    }

    // ---- 時間帯ごとの 値を ひとまとめに する。
    // **混ぜられる ように した**のが 肝。前は switch の 中に 直に 書いて いたので
    // つなぎめで かくっと 切りかわって いた
    struct Preset {
        public Vector3 sunRot; public Color sunCol; public float sunI;
        public Color sky, equator, ground, fogCol; public float fogD;
        public float andonI; public Color fillCol; public float fillI;
        public Color bg, zen, hor, ridge, cloud; public float cloudAmt, haze;
        public float shojiGlow; public Color shojiCol;
    }

    static Preset Of(Tod t) {
        switch (t) {
            case Tod.Hiru:      // ひる：高く、白っぽく、影は みじかい
                return new Preset {
                    sunRot = new Vector3(62f, 168f, 0f),
                    sunCol = new Color(1.00f, 0.97f, 0.90f), sunI = 3.0f,
                    sky = new Color(0.62f, 0.63f, 0.60f), equator = new Color(0.50f, 0.47f, 0.40f),
                    ground = new Color(0.26f, 0.23f, 0.19f),
                    fogCol = new Color(0.76f, 0.75f, 0.68f), fogD = 0.018f,
                    andonI = 0f, fillCol = new Color(0.82f, 0.87f, 1.00f), fillI = 0.55f,
                    bg = new Color(0.55f, 0.74f, 0.93f),
                    zen = new Color(0.26f, 0.52f, 0.88f), hor = new Color(0.78f, 0.88f, 0.96f),
                    ridge = new Color(0.34f, 0.44f, 0.40f), cloud = Color.white,
                    cloudAmt = 0.34f, haze = 0.68f,
                    shojiGlow = 0.85f, shojiCol = new Color(1.00f, 0.94f, 0.80f),
                };
            case Tod.Yugata:    // ゆうがた：低く、だいだい色。影が ながく のびる
                return new Preset {
                    sunRot = new Vector3(14f, 138f, 0f),
                    sunCol = new Color(1.00f, 0.68f, 0.42f), sunI = 2.6f,
                    sky = new Color(0.55f, 0.44f, 0.40f), equator = new Color(0.52f, 0.36f, 0.28f),
                    ground = new Color(0.24f, 0.17f, 0.14f),
                    fogCol = new Color(0.82f, 0.60f, 0.44f), fogD = 0.030f,
                    andonI = 2.2f, fillCol = new Color(0.60f, 0.62f, 0.85f), fillI = 0.35f,
                    bg = new Color(0.85f, 0.56f, 0.38f),
                    zen = new Color(0.36f, 0.32f, 0.52f), hor = new Color(0.98f, 0.62f, 0.34f),
                    ridge = new Color(0.30f, 0.24f, 0.28f), cloud = new Color(1f, 0.78f, 0.55f),
                    cloudAmt = 0.46f, haze = 0.74f,
                    shojiGlow = 0.55f, shojiCol = new Color(1.00f, 0.78f, 0.55f),
                };
            case Tod.Yoru:      // よる：日は 落ちきって、行灯だけが たより
                return new Preset {
                    sunRot = new Vector3(16f, 160f, 0f),
                    sunCol = new Color(0.55f, 0.62f, 0.90f), sunI = 0.18f,   // 月あかり ぶん
                    sky = new Color(0.16f, 0.19f, 0.28f), equator = new Color(0.12f, 0.13f, 0.20f),
                    ground = new Color(0.06f, 0.07f, 0.10f),
                    fogCol = new Color(0.20f, 0.24f, 0.36f), fogD = 0.040f,
                    andonI = 4.6f, fillCol = new Color(0.45f, 0.55f, 0.90f), fillI = 0.16f,
                    bg = new Color(0.07f, 0.09f, 0.17f),
                    zen = new Color(0.04f, 0.06f, 0.14f), hor = new Color(0.12f, 0.16f, 0.28f),
                    ridge = new Color(0.06f, 0.08f, 0.13f), cloud = new Color(0.22f, 0.26f, 0.36f),
                    cloudAmt = 0.22f, haze = 0.55f,
                    shojiGlow = 0.10f, shojiCol = new Color(1.00f, 0.94f, 0.80f),
                };
            default:            // あさ：ひくい 光が 障子ごしに ながく さしこむ
                return new Preset {
                    sunRot = new Vector3(38f, 150f, 0f),
                    sunCol = new Color(1.00f, 0.95f, 0.83f), sunI = 2.6f,
                    sky = new Color(0.58f, 0.58f, 0.54f), equator = new Color(0.46f, 0.42f, 0.35f),
                    ground = new Color(0.24f, 0.21f, 0.17f),
                    fogCol = new Color(0.72f, 0.70f, 0.62f), fogD = 0.022f,
                    andonI = 0f, fillCol = new Color(0.80f, 0.84f, 0.95f), fillI = 0.55f,
                    bg = new Color(0.69f, 0.81f, 0.93f),
                    zen = new Color(0.34f, 0.56f, 0.84f), hor = new Color(0.94f, 0.86f, 0.74f),
                    ridge = new Color(0.32f, 0.40f, 0.40f), cloud = new Color(1f, 0.94f, 0.86f),
                    cloudAmt = 0.40f, haze = 0.72f,
                    shojiGlow = 0.85f, shojiCol = new Color(1.00f, 0.94f, 0.80f),
                };
        }
    }

    static Preset Mix(Preset a, Preset b, float t) {
        return new Preset {
            sunRot = Vector3.Lerp(a.sunRot, b.sunRot, t),
            sunCol = Color.Lerp(a.sunCol, b.sunCol, t), sunI = Mathf.Lerp(a.sunI, b.sunI, t),
            sky = Color.Lerp(a.sky, b.sky, t), equator = Color.Lerp(a.equator, b.equator, t),
            ground = Color.Lerp(a.ground, b.ground, t), fogCol = Color.Lerp(a.fogCol, b.fogCol, t),
            fogD = Mathf.Lerp(a.fogD, b.fogD, t),
            andonI = Mathf.Lerp(a.andonI, b.andonI, t),
            fillCol = Color.Lerp(a.fillCol, b.fillCol, t), fillI = Mathf.Lerp(a.fillI, b.fillI, t),
            bg = Color.Lerp(a.bg, b.bg, t), zen = Color.Lerp(a.zen, b.zen, t),
            hor = Color.Lerp(a.hor, b.hor, t), ridge = Color.Lerp(a.ridge, b.ridge, t),
            cloud = Color.Lerp(a.cloud, b.cloud, t),
            cloudAmt = Mathf.Lerp(a.cloudAmt, b.cloudAmt, t), haze = Mathf.Lerp(a.haze, b.haze, t),
            shojiGlow = Mathf.Lerp(a.shojiGlow, b.shojiGlow, t),
            shojiCol = Color.Lerp(a.shojiCol, b.shojiCol, t),
        };
    }

    // 時こく → どの 時間帯か。**夏の 日は 長い**。日の出 5時ごろ、日の入り 19時ごろ
    static readonly float[] KeyHour = { 0f, 4.2f, 6.5f, 10f, 15.5f, 18.3f, 19.6f, 21f, 24f };
    static readonly Tod[]   KeyTod  = { Tod.Yoru, Tod.Yoru, Tod.Asa, Tod.Hiru, Tod.Hiru,
                                        Tod.Yugata, Tod.Yugata, Tod.Yoru, Tod.Yoru };

    /// <summary>その 時こくに いちばん 近い 時間帯（虫の 湧きかたなどが 見る）</summary>
    public static Tod TodAt(float h) {
        h = Mathf.Repeat(h, 24f);
        for (int i = 0; i < KeyHour.Length - 1; i++) {
            if (h < KeyHour[i] || h > KeyHour[i + 1]) continue;
            float t = (h - KeyHour[i]) / Mathf.Max(KeyHour[i + 1] - KeyHour[i], 1e-4f);
            return t < 0.5f ? KeyTod[i] : KeyTod[i + 1];
        }
        return Tod.Hiru;
    }

    Preset Current() {
        if (!useHour) return Of(tod);
        // **みかけの 時こく**で 見た目を 決める（月末ほど 夕方が 早く 来る）
        float h = Mathf.Repeat(MikakeHour, 24f);
        for (int i = 0; i < KeyHour.Length - 1; i++) {
            if (h < KeyHour[i] || h > KeyHour[i + 1]) continue;
            float t = Mathf.Clamp01((h - KeyHour[i]) / Mathf.Max(KeyHour[i + 1] - KeyHour[i], 1e-4f));
            t = t * t * (3f - 2f * t);                       // なめらかに 移る
            tod = t < 0.5f ? KeyTod[i] : KeyTod[i + 1];      // 見る がわの ための 目やす
            return Mix(Of(KeyTod[i]), Of(KeyTod[i + 1]), t);
        }
        return Of(tod);
    }

    public void Apply() {
        var p = Current();

        // 太陽：むき（x=高さ, y=方角）／色／強さ
        // ★方角(y)は **90〜180 に そろえる**。こうすると 光は「手前・左」から 差し、
        //   影が **奥へ** 落ちる。横から あてると 板の 草木が「立てた 板」だと ばれる。
        //   90〜180 の あいだなら 左の 障子ごしにも 光が 入る（格子の 影が 畳に のびる）
        if (sun != null) {
            sun.transform.rotation = Quaternion.Euler(p.sunRot);
            sun.color = p.sunCol; sun.intensity = p.sunI;
        }
        if (fill != null) { fill.color = p.fillCol; fill.intensity = p.fillI; }
        if (andon != null) { andon.intensity = p.andonI; andon.enabled = p.andonI > 0.01f; }
        if (andonHoka != null)
            foreach (var a in andonHoka)
                if (a != null) { a.intensity = p.andonI; a.enabled = p.andonI > 0.01f; }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = p.sky;
        RenderSettings.ambientEquatorColor = p.equator;
        RenderSettings.ambientGroundColor = p.ground;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = p.fogCol;
        RenderSettings.fogDensity = p.fogD;
        DynamicGI.UpdateEnvironment();

        // 障子紙の にじみ：外が 明るい ときだけ 光らせる。よるは 逆に 行灯が 内から 照らす
        if (shojiPaper != null && shojiPaper.sharedMaterial != null)
            shojiPaper.sharedMaterial.SetColor("_EmissionColor", p.shojiCol * p.shojiGlow);

        // 背景の 色。**屋内は「屋根を はずした 切りぬき」**なので、まわりは 暗いのが 正しい。
        // 明るい 空に すると 部屋が 宙に 浮いて 見えた
        if (cam != null) {
            if (outdoor) {
                // 空の 色。**遠くは この 色に かすんで いく**ように 霧も そろえるので、
                // 地面の 切れめが 黒い 崖に 見えなく なる
                cam.backgroundColor = p.bg;
                RenderSettings.fogColor = p.bg;

                // **空を 手続きで 描く。** 単色だと 地平線から さきが「何も ない ところ」に
                // 見えて、山ぎわの 場面が 宙に 浮いた。遠くの 山なみも ここで 出す
                if (skybox != null) {
                    skybox.SetColor("_Zenith", p.zen);
                    skybox.SetColor("_Horizon", p.hor);
                    skybox.SetColor("_Ridge", p.ridge);
                    skybox.SetColor("_CloudColor", p.cloud);
                    skybox.SetFloat("_CloudAmount", p.cloudAmt);
                    skybox.SetFloat("_Haze", p.haze);
                    // ★低いと ただの 帯に 見える。奥から 順に 描く ように 直した ので 高くできる
                    skybox.SetFloat("_RidgeHeight", 0.165f);
                    RenderSettings.skybox = skybox;
                    cam.clearFlags = CameraClearFlags.Skybox;
                    // 遠くは 地平線の 色に かすませる＝空と 地めんが つながる
                    RenderSettings.fogColor = p.hor;
                }
                // そとは 遠くを かすませる。ただし **かけすぎると 山が 消える**。
                // 木立ちだけで 地平線を ふさいで いた ころは 1.5倍に して いたが、
                // それだと 40m先で 半分 白く なり、山が あるのに 平地に 見えた
                RenderSettings.fogDensity *= 0.85f;
                // そとは 空からの まわりこみが 強い。屋内の 値のままだと 沈んで 見える
                RenderSettings.ambientSkyColor = p.sky * 1.35f;
                DynamicGI.UpdateEnvironment();
            } else {
                cam.backgroundColor = Color.Lerp(new Color(0.055f, 0.048f, 0.044f),
                                                 new Color(0.022f, 0.026f, 0.040f),
                                                 Mathf.Clamp01(1f - p.sunI / 2.6f));
            }
        }

        // **見せ場では もやを 薄く する。**
        // 高台に のぼって 谷を 見わたす ところで ふだんの こさの ままだと、
        // 遠くが 灰みどりに 溶けて「見わたせた」感じが まったく 出なかった
        if (orbit != null && orbit.FogScale != 1f)
            RenderSettings.fogDensity *= Mathf.Max(0f, orbit.FogScale);

        // **天気は いちばん 最後に かぶせる。** ここまでで 時間帯の 値が そろっているので、
        // 天気は それに かけ算する だけで すむ（朝の 雨・夕方の 雨が それぞれ 正しく なる）
        if (weather != null) weather.ApplyOn(sun, fill, cam);
    }
}
