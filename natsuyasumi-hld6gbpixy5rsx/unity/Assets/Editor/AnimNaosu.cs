using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// もらった モーションの **筋値を 直して** 別の クリップに 焼く（2026-09-06）。
///
/// WHY: 本人「肩を上げて、肘をまげて、カクカク動いてるだけ」「まるでジョジョ」。
///      Quaternius は **ずんぐりした 汎用リグ**むけ。Unity の ヒューマノイドは
///      **筋肉の 角度**を 保って 移すので、脚が 長く きゃしゃな VRoid の 体型に 移すと
///      肩が 上がり 肘が 曲がった 見た目に なる。**再生の しかたでは なく 体型の ちがい。**
///
/// ★ここが 効く 点：本人の 言った「肩を 上げて」「肘を まげて」は
///   Unity の ヒューマノイドでは **そのまま パラメータの 名まえ**
///   （`Shoulder Down-Up` / `Forearm Stretch`）。だから 直接 叩ける。
///
/// 取りこんだ クリップは 読みだし専用 なので、**曲線を うつして 別の .anim を 作る**。
///
/// run: powershell -File tools/rebuild.ps1 -Only AnimNaosu.Naosu
/// </summary>
public static class AnimNaosu {
    const string FBX = "Assets/Art/Models/anim/AnimationLibrary_Unity_Standard.fbx";
    const string DIR = "Assets/Art/Models/anim/naoshi";

    /// <summary>直す クリップ（もとの 名まえ）</summary>
    static readonly string[] TAISHO = { "Idle_Loop", "Walk_Loop", "Walk_Formal_Loop", "Jog_Fwd_Loop", "Sprint_Loop" };

    /// <summary>
    /// 直しかた：曲線の 名まえ に この 文字が 入って いたら、値に これを 足す／かける。
    /// ・肩を 下げる（+ が 上げる 向き）
    /// ・肘の のばし を 増やす（+ が のばす 向き）
    /// ・またを せまく（In-Out の 振れ幅を 小さく）
    /// ・腕の 振りを 小さく（女の子は 体の 前で 小さく 振る）
    /// </summary>
    struct Naoshi {
        public string fukumu;   // 曲線の 名まえに ふくまれる 文字
        public float tasu;      // 足す
        public float kakeru;    // かける（振れ幅）
        public Naoshi(string f, float t, float k) { fukumu = f; tasu = t; kakeru = k; }
    }

    static readonly Naoshi[] NAOSHI = {
        new Naoshi("Shoulder Down-Up",   -0.45f, 0.55f),   // 肩を 下げる・上下の 振れを 半分に
        new Naoshi("Arm Down-Up",        -0.10f, 0.80f),   // 上腕を 少し 下げる
        new Naoshi("Forearm Stretch",    +0.30f, 0.70f),   // 肘を のばす
        new Naoshi("Arm Front-Back",      0.00f, 0.72f),   // 腕の 前後の 振りを 小さく
        new Naoshi("Upper Leg In-Out",    0.00f, 0.55f),   // またを せまく
        new Naoshi("Leg In-Out",          0.00f, 0.60f),
    };

    /// <summary>BOOTH（fumi2kick）の 直しかた。あちらは **VRで 人形を 操る** 用途 なので
    /// **腕を 横に ひらいた まま**（`Arm Down-Up` の 平均が -0.245。自然に 下ろすと -0.7〜-0.85）。
    /// 下ろして 少しだけ 振る（2026-09-06 実測）</summary>
    static readonly Naoshi[] NAOSHI_F2K = {
        new Naoshi("Arm Down-Up",        -0.50f, 1.00f),   // 腕を 下ろす
        new Naoshi("Shoulder Down-Up",   -0.12f, 0.80f),   // 肩も 少し 下げる
        new Naoshi("Arm Front-Back",      0.00f, 1.15f),   // 前後の 振りは 少し 増やす
        new Naoshi("Upper Leg In-Out",    0.00f, 0.85f),
    };

    static readonly string[] TAISHO_F2K = { "0002_Walk", "0005_Sit", "0006_LieBack", "0007_LieDown" };

    public static void Naosu() {
        if (!AssetDatabase.IsValidFolder(DIR)) {
            AssetDatabase.CreateFolder("Assets/Art/Models/anim", "naoshi");
        }
        var moto = new Dictionary<string, AnimationClip>();
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(FBX)) {
            if (a is AnimationClip c && !c.name.StartsWith("__preview__")) {
                var n = c.name;
                int bar = n.IndexOf('|');
                if (bar >= 0) n = n.Substring(bar + 1);
                moto[n] = c;
            }
        }
        Debug.Log("[Probe] AnimNaosu: もとの クリップ " + moto.Count + " 本");

        foreach (var na in TAISHO) {
            AnimationClip src;
            if (!moto.TryGetValue(na, out src)) {
                Debug.LogWarning("[Probe] AnimNaosu: 無い " + na);
                continue;
            }
            var dst = new AnimationClip { name = na, frameRate = src.frameRate };
            var st = AnimationUtility.GetAnimationClipSettings(src);
            AnimationUtility.SetAnimationClipSettings(dst, st);

            int naota = 0, zenbu = 0;
            foreach (var b in AnimationUtility.GetCurveBindings(src)) {
                var cv = AnimationUtility.GetEditorCurve(src, b);
                if (cv == null) continue;
                zenbu++;
                float tasu = 0f, kake = 1f;
                foreach (var n2 in NAOSHI) {
                    if (b.propertyName.Contains(n2.fukumu)) { tasu = n2.tasu; kake = n2.kakeru; naota++; break; }
                }
                if (tasu != 0f || kake != 1f) {
                    var ks = cv.keys;
                    // 平均の まわりで 振れ幅を 縮め、そこへ 足す
                    float hei = 0f;
                    foreach (var k in ks) hei += k.value;
                    hei /= Mathf.Max(1, ks.Length);
                    for (int i = 0; i < ks.Length; i++) {
                        ks[i].value = hei + (ks[i].value - hei) * kake + tasu;
                        ks[i].inTangent *= kake;
                        ks[i].outTangent *= kake;
                    }
                    cv.keys = ks;
                }
                AnimationUtility.SetEditorCurve(dst, b, cv);
            }
            var p = DIR + "/" + na + ".anim";
            AssetDatabase.DeleteAsset(p);
            AssetDatabase.CreateAsset(dst, p);
            Debug.Log("[Probe] AnimNaosu: " + na + " 曲線 " + zenbu + " 本（直した " + naota + " 本）→ " + p);
        }
        // ---- BOOTH（fumi2kick）の ぶん
        if (!AssetDatabase.IsValidFolder("Assets/Art/Models/anim/f2k_naoshi")) {
            AssetDatabase.CreateFolder("Assets/Art/Models/anim", "f2k_naoshi");
        }
        foreach (var na in TAISHO_F2K) {
            var src = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/Models/anim/f2k/" + na + ".anim");
            if (src == null) { Debug.LogWarning("[Probe] AnimNaosu: f2k が 無い " + na); continue; }
            var dst = new AnimationClip { name = na, frameRate = src.frameRate };
            AnimationUtility.SetAnimationClipSettings(dst, AnimationUtility.GetAnimationClipSettings(src));
            int naota = 0, zenbu = 0;
            foreach (var b2 in AnimationUtility.GetCurveBindings(src)) {
                var cv = AnimationUtility.GetEditorCurve(src, b2);
                if (cv == null) continue;
                zenbu++;
                float tasu = 0f, kake = 1f;
                foreach (var n2 in NAOSHI_F2K) {
                    if (b2.propertyName.Contains(n2.fukumu)) { tasu = n2.tasu; kake = n2.kakeru; naota++; break; }
                }
                if (tasu != 0f || kake != 1f) {
                    var ks = cv.keys;
                    float hei = 0f;
                    foreach (var k in ks) hei += k.value;
                    hei /= Mathf.Max(1, ks.Length);
                    for (int i = 0; i < ks.Length; i++) {
                        ks[i].value = hei + (ks[i].value - hei) * kake + tasu;
                        ks[i].inTangent *= kake; ks[i].outTangent *= kake;
                    }
                    cv.keys = ks;
                }
                AnimationUtility.SetEditorCurve(dst, b2, cv);
            }
            var p2 = "Assets/Art/Models/anim/f2k_naoshi/" + na + ".anim";
            AssetDatabase.DeleteAsset(p2);
            AssetDatabase.CreateAsset(dst, p2);
            Debug.Log("[Probe] AnimNaosu(f2k): " + na + " 曲線 " + zenbu + "（直した " + naota + "）→ " + p2);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // どんな 名まえの 曲線が あるかを 一度 出す（直す 名まえの 手がかり）
        AnimationClip mihon;
        if (moto.TryGetValue("Jog_Fwd_Loop", out mihon)) {
            var sb = new StringBuilder();
            int n3 = 0;
            foreach (var b in AnimationUtility.GetCurveBindings(mihon)) {
                if (n3++ < 40) sb.Append(b.propertyName).Append(" / ");
            }
            Debug.Log("[Probe] AnimNaosu: 曲線の 名まえ（先頭40）: " + sb);
        }
    }
}
