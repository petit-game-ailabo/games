using System;
using UnityEngine;
using UnityEngine.Rendering;

// 時間帯で 光を まるごと 切りかえる（あさ／ひる／ゆうがた／よる）。
// 太陽の むき・色・強さ、まわりの 明るさ、もや、行灯の 火 までを ひとまとめに 変える。
// ゲーム中は これを 時計に つなぐ。絵の たしかめは exeの 引数 -tod yoru などで。
[ExecuteAlways]
public class TimeOfDay : MonoBehaviour {
    public enum Tod { Asa, Hiru, Yugata, Yoru }

    [Header("いまの 時間帯")]
    public Tod tod = Tod.Asa;

    [Header("つなぐ もの（自動で 割りあて）")]
    public Light sun;
    public Light fill;
    public Light andon;
    public Renderer shojiPaper;    // 障子紙。よるは 光らせない
    public Camera cam;
    public Weather weather;        // 天気は この 上に かぶせる（順番を 固定するため ここから 呼ぶ）

    [Header("そとの 場面か")]
    // 屋内は「屋根を はずした 切りぬき」なので まわりは 暗いのが 正しい。
    // そとの 場面で 同じ ことを すると **地平線から さきが まっ黒**に なるので、空の 色を 出す
    public bool outdoor = false;

    void OnEnable()   { AutoWire(); ApplyFromArgs(); Apply(); }
    void OnValidate() { AutoWire(); Apply(); }

    void AutoWire() {
        if (sun == null)   { var g = GameObject.Find("Sun");   if (g) sun = g.GetComponent<Light>(); }
        if (fill == null)  { var g = GameObject.Find("Fill");  if (g) fill = g.GetComponent<Light>(); }
        if (andon == null) { var g = GameObject.Find("Andon_Light"); if (g) andon = g.GetComponent<Light>(); }
        if (cam == null)   cam = Camera.main;
        if (weather == null) weather = FindFirstObjectByType<Weather>();
    }

    void ApplyFromArgs() {
        var a = Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++) {
            if (a[i] != "-tod") continue;
            switch (a[i + 1].ToLower()) {
                case "asa":    tod = Tod.Asa;    break;
                case "hiru":   tod = Tod.Hiru;   break;
                case "yugata": tod = Tod.Yugata; break;
                case "yoru":   tod = Tod.Yoru;   break;
            }
        }
    }

    public void Apply() {
        // 太陽：むき（x=高さ, y=方角）／色／強さ
        // ★方角(y)は **90〜180 に そろえる**。こうすると 光は「手前・左」から 差し、
        //   影が **奥へ** 落ちる。横から あてると 板の 草木が「立てた 板」だと ばれる。
        //   90〜180 の あいだなら 左の 障子ごしにも 光が 入る（格子の 影が 畳に のびる）
        Vector3 sunRot; Color sunCol; float sunI;
        Color sky, equator, ground, fogCol;
        float fogD, andonI, fillI;
        Color fillCol;

        switch (tod) {
            case Tod.Hiru:      // ひる：高く、白っぽく、影は みじかい
                sunRot = new Vector3(62f, 168f, 0f);
                sunCol = new Color(1.00f, 0.97f, 0.90f); sunI = 3.0f;
                sky = new Color(0.62f, 0.63f, 0.60f); equator = new Color(0.50f, 0.47f, 0.40f);
                ground = new Color(0.26f, 0.23f, 0.19f);
                fogCol = new Color(0.76f, 0.75f, 0.68f); fogD = 0.018f;
                andonI = 0f; fillCol = new Color(0.82f, 0.87f, 1.00f); fillI = 0.55f;
                break;
            case Tod.Yugata:    // ゆうがた：低く、だいだい色。影が ながく のびる
                sunRot = new Vector3(14f, 138f, 0f);
                sunCol = new Color(1.00f, 0.68f, 0.42f); sunI = 2.6f;
                sky = new Color(0.55f, 0.44f, 0.40f); equator = new Color(0.52f, 0.36f, 0.28f);
                ground = new Color(0.24f, 0.17f, 0.14f);
                fogCol = new Color(0.82f, 0.60f, 0.44f); fogD = 0.030f;
                andonI = 2.2f; fillCol = new Color(0.60f, 0.62f, 0.85f); fillI = 0.35f;
                break;
            case Tod.Yoru:      // よる：日は 落ちきって、行灯だけが たより
                sunRot = new Vector3(16f, 160f, 0f);
                sunCol = new Color(0.55f, 0.62f, 0.90f); sunI = 0.18f;   // 月あかり ぶん
                sky = new Color(0.16f, 0.19f, 0.28f); equator = new Color(0.12f, 0.13f, 0.20f);
                ground = new Color(0.06f, 0.07f, 0.10f);
                fogCol = new Color(0.20f, 0.24f, 0.36f); fogD = 0.040f;
                andonI = 4.6f; fillCol = new Color(0.45f, 0.55f, 0.90f); fillI = 0.16f;
                break;
            default:            // あさ：ひくい 光が 障子ごしに ながく さしこむ
                sunRot = new Vector3(38f, 150f, 0f);
                sunCol = new Color(1.00f, 0.95f, 0.83f); sunI = 2.6f;
                sky = new Color(0.58f, 0.58f, 0.54f); equator = new Color(0.46f, 0.42f, 0.35f);
                ground = new Color(0.24f, 0.21f, 0.17f);
                fogCol = new Color(0.72f, 0.70f, 0.62f); fogD = 0.022f;
                andonI = 0f; fillCol = new Color(0.80f, 0.84f, 0.95f); fillI = 0.55f;
                break;
        }

        if (sun != null) {
            sun.transform.rotation = Quaternion.Euler(sunRot);
            sun.color = sunCol; sun.intensity = sunI;
        }
        if (fill != null) { fill.color = fillCol; fill.intensity = fillI; }
        if (andon != null) { andon.intensity = andonI; andon.enabled = andonI > 0.01f; }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = sky;
        RenderSettings.ambientEquatorColor = equator;
        RenderSettings.ambientGroundColor = ground;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogCol;
        RenderSettings.fogDensity = fogD;
        DynamicGI.UpdateEnvironment();

        // 障子紙の にじみ：外が 明るい ときだけ 光らせる。よるは 逆に 行灯が 内から 照らす
        if (shojiPaper != null && shojiPaper.sharedMaterial != null) {
            float g = (tod == Tod.Yoru) ? 0.10f : (tod == Tod.Yugata ? 0.55f : 0.85f);
            var c = (tod == Tod.Yugata) ? new Color(1.00f, 0.78f, 0.55f) : new Color(1.00f, 0.94f, 0.80f);
            shojiPaper.sharedMaterial.SetColor("_EmissionColor", c * g);
        }
        // 背景の 色。**屋内は「屋根を はずした 切りぬき」**なので、まわりは 暗いのが 正しい。
        // 明るい 空に すると 部屋が 宙に 浮いて 見えた
        if (cam != null) {
            if (outdoor) {
                // 空の 色。**遠くは この 色に かすんで いく**ように 霧も そろえるので、
                // 地面の 切れめが 黒い 崖に 見えなく なる
                Color sky2;
                switch (tod) {
                    case Tod.Hiru:   sky2 = new Color(0.55f, 0.74f, 0.93f); break;
                    case Tod.Yugata: sky2 = new Color(0.85f, 0.56f, 0.38f); break;
                    case Tod.Yoru:   sky2 = new Color(0.07f, 0.09f, 0.17f); break;
                    default:         sky2 = new Color(0.69f, 0.81f, 0.93f); break;   // あさ
                }
                cam.backgroundColor = sky2;
                RenderSettings.fogColor = sky2;
                // そとは 遠くを かすませる。ただし **かけすぎると 山が 消える**。
                // 木立ちだけで 地平線を ふさいで いた ころは 1.5倍に して いたが、
                // それだと 40m先で 半分 白く なり、山が あるのに 平地に 見えた
                RenderSettings.fogDensity *= 0.85f;
                // そとは 空からの まわりこみが 強い。屋内の 値のままだと 沈んで 見える
                RenderSettings.ambientSkyColor = sky * 1.35f;
                DynamicGI.UpdateEnvironment();
            } else {
                switch (tod) {
                    case Tod.Yugata: cam.backgroundColor = new Color(0.075f, 0.050f, 0.045f); break;
                    case Tod.Yoru:   cam.backgroundColor = new Color(0.022f, 0.026f, 0.040f); break;
                    default:         cam.backgroundColor = new Color(0.055f, 0.048f, 0.044f); break;
                }
            }
        }

        // **天気は いちばん 最後に かぶせる。** ここまでで 時間帯の 値が そろっているので、
        // 天気は それに かけ算する だけで すむ（朝の 雨・夕方の 雨が それぞれ 正しく なる）
        if (weather != null) weather.ApplyOn(sun, fill, cam);
    }
}
