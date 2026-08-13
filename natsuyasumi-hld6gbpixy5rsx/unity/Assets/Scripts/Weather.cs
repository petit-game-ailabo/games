using System;
using UnityEngine;
using UnityEngine.Rendering;

// 天気を まるごと 切りかえる（はれ／くもり／あめ／ゆうだち）。
// 時間帯（TimeOfDay）が 決めた 光の 上に **かぶせる** かたちで 効かせる。
//   TimeOfDay.Apply() の 最後から よばれる → 二重に かけない ように 順番を 固定して ある。
// つぶ（雨・もや）は BuildZashiki が 作って ここに つないでいる。
// たしかめは exeの 引数 -weather ame など。
[ExecuteAlways]
public class Weather : MonoBehaviour {
    public enum Mode { Hare, Kumori, Ame, Yudachi }

    [Header("いまの 天気")]
    public Mode mode = Mode.Hare;

    [Header("つなぐ もの（BuildZashiki が 割りあて）")]
    public ParticleSystem rain;     // 雨つぶ（庭の うえ）
    public ParticleSystem mist;     // もや・雲の 影
    public TimeOfDay timeOfDay;

    void OnEnable() {
        ApplyFromArgs();
        if (timeOfDay == null) timeOfDay = FindFirstObjectByType<TimeOfDay>();
        if (timeOfDay != null) timeOfDay.Apply();   // 光を 引きなおすと この Weather も かかる
    }

    void OnValidate() {
        if (timeOfDay != null) timeOfDay.Apply();
    }

    void ApplyFromArgs() {
        var a = Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++) {
            if (a[i] != "-weather") continue;
            switch (a[i + 1].ToLower()) {
                case "hare":    mode = Mode.Hare;    break;
                case "kumori":  mode = Mode.Kumori;  break;
                case "ame":     mode = Mode.Ame;     break;
                case "yudachi": mode = Mode.Yudachi; break;
            }
        }
    }

    // TimeOfDay が 光を おいた **あと**に よばれる。
    // sunScale などは「時間帯の 値に かける」ので、朝の あめ・夕方の あめが それぞれ 正しく なる
    public void ApplyOn(Light sun, Light fill, Camera cam) {
        float sunScale, fogScale, fillScale, rainRate, mistRate;
        Color fogTint;      // もやの 色を この 色へ 寄せる
        float tintAmount;

        switch (mode) {
            case Mode.Kumori:   // くもり：日ざしが 弱く、影が うすい。空気は すこし 白い
                sunScale = 0.55f; fillScale = 1.30f; fogScale = 1.25f;
                fogTint = new Color(0.74f, 0.76f, 0.78f); tintAmount = 0.35f;
                rainRate = 0f; mistRate = 2.5f;
                break;
            case Mode.Ame:      // あめ：しとしと。影は ほぼ 出ない
                sunScale = 0.34f; fillScale = 1.35f; fogScale = 1.55f;
                fogTint = new Color(0.62f, 0.67f, 0.72f); tintAmount = 0.5f;
                rainRate = 320f; mistRate = 4f;
                break;
            case Mode.Yudachi: // ゆうだち：夏の 夕立。暗く、つぶが 太くて 多い
                sunScale = 0.22f; fillScale = 1.20f; fogScale = 1.9f;
                fogTint = new Color(0.46f, 0.50f, 0.58f); tintAmount = 0.65f;
                rainRate = 800f; mistRate = 7f;
                break;
            default:            // はれ：さわらない
                sunScale = 1f; fillScale = 1f; fogScale = 1f;
                fogTint = Color.white; tintAmount = 0f;
                rainRate = 0f; mistRate = 0f;
                break;
        }

        if (sun != null) {
            sun.intensity *= sunScale;
            // 雨の 日は 影の ふちを ぼかす。くっきり 出ると 晴れに 見える
            sun.shadowStrength *= Mathf.Lerp(1f, 0.35f, 1f - sunScale);
        }
        if (fill != null) fill.intensity *= fillScale;

        // **もやには 上限を おく。** かけ算だけだと 夕方(0.030)×夕立 で 一面 まっ白に なった
        RenderSettings.fogDensity = Mathf.Min(RenderSettings.fogDensity * fogScale, 0.048f);
        if (tintAmount > 0f)
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, fogTint, tintAmount);
        // 背景（切りぬきの まわり）も 天気に つれて 寄せる。雨の 日だけ 暗いままだと 浮く
        if (cam != null && tintAmount > 0f)
            cam.backgroundColor = Color.Lerp(cam.backgroundColor, fogTint * 0.12f, tintAmount);

        SetRate(rain, rainRate);
        SetRate(mist, mistRate);

        // 夕立は つぶを 太く・速く する
        if (rain != null) {
            var main = rain.main;
            main.startSpeed = mode == Mode.Yudachi ? 16f : 9f;
            main.startSize  = mode == Mode.Yudachi ? 0.075f : 0.05f;
            main.startColor = mode == Mode.Yudachi
                ? new Color(0.78f, 0.84f, 0.92f, 0.60f)
                : new Color(0.80f, 0.86f, 0.94f, 0.42f);
        }
        DynamicGI.UpdateEnvironment();
    }

    static void SetRate(ParticleSystem ps, float rate) {
        if (ps == null) return;
        var em = ps.emission;
        em.rateOverTime = rate;
        // 0 のまま 走らせておくと 前の つぶが 残るので、止めるときは 消す
        if (rate <= 0.01f) { ps.Clear(true); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
        else if (!ps.isPlaying) ps.Play(true);
    }
}
