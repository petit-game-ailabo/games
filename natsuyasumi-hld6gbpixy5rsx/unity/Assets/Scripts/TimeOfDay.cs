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

    void OnEnable()   { AutoWire(); ApplyFromArgs(); Apply(); }
    void OnValidate() { AutoWire(); Apply(); }

    void AutoWire() {
        if (sun == null)   { var g = GameObject.Find("Sun");   if (g) sun = g.GetComponent<Light>(); }
        if (fill == null)  { var g = GameObject.Find("Fill");  if (g) fill = g.GetComponent<Light>(); }
        if (andon == null) { var g = GameObject.Find("Andon_Light"); if (g) andon = g.GetComponent<Light>(); }
        if (cam == null)   cam = Camera.main;
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
        Vector3 sunRot; Color sunCol; float sunI;
        Color sky, equator, ground, fogCol;
        float fogD, andonI, fillI;
        Color fillCol;

        switch (tod) {
            case Tod.Hiru:      // ひる：高く、白っぽく、影は みじかい
                sunRot = new Vector3(62f, 52f, 0f);
                sunCol = new Color(1.00f, 0.97f, 0.90f); sunI = 3.0f;
                sky = new Color(0.62f, 0.63f, 0.60f); equator = new Color(0.50f, 0.47f, 0.40f);
                ground = new Color(0.26f, 0.23f, 0.19f);
                fogCol = new Color(0.76f, 0.75f, 0.68f); fogD = 0.018f;
                andonI = 0f; fillCol = new Color(0.82f, 0.87f, 1.00f); fillI = 0.55f;
                break;
            case Tod.Yugata:    // ゆうがた：低く、だいだい色。影が ながく のびる
                sunRot = new Vector3(12f, 78f, 0f);
                sunCol = new Color(1.00f, 0.68f, 0.42f); sunI = 2.6f;
                sky = new Color(0.55f, 0.44f, 0.40f); equator = new Color(0.52f, 0.36f, 0.28f);
                ground = new Color(0.24f, 0.17f, 0.14f);
                fogCol = new Color(0.82f, 0.60f, 0.44f); fogD = 0.030f;
                andonI = 2.2f; fillCol = new Color(0.60f, 0.62f, 0.85f); fillI = 0.35f;
                break;
            case Tod.Yoru:      // よる：日は 落ちきって、行灯だけが たより
                sunRot = new Vector3(-8f, 200f, 0f);
                sunCol = new Color(0.55f, 0.62f, 0.90f); sunI = 0.18f;   // 月あかり ぶん
                sky = new Color(0.16f, 0.19f, 0.28f); equator = new Color(0.12f, 0.13f, 0.20f);
                ground = new Color(0.06f, 0.07f, 0.10f);
                fogCol = new Color(0.20f, 0.24f, 0.36f); fogD = 0.040f;
                andonI = 4.6f; fillCol = new Color(0.45f, 0.55f, 0.90f); fillI = 0.16f;
                break;
            default:            // あさ：ひくい 光が 障子ごしに ながく さしこむ
                sunRot = new Vector3(34f, 66f, 0f);
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
        if (cam != null)
            cam.backgroundColor = (tod == Tod.Yoru) ? new Color(0.020f, 0.024f, 0.038f)
                                                    : new Color(0.055f, 0.045f, 0.040f);
    }
}
