using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Quaternius の アニメーション（CC0）を **Humanoid として** 取りこむ（2026-09-06）。
///
/// ★GUI で 設定しない（この企画の 決まり）。ModelImporter を コードで 直して 読みなおす。
/// ★Humanoid に して おけば、VRM（これも Humanoid）に **そのまま 着せられる**（リターゲット）。
/// </summary>
public static class AnimSetup {
    const string FBX = "Assets/Art/Models/anim/AnimationLibrary_Unity_Standard.fbx";

    public static void Setup() {
        var mi = AssetImporter.GetAtPath(FBX) as ModelImporter;
        if (mi == null) { Debug.LogError("[Probe] AnimSetup: FBX が ない " + FBX); return; }
        // ★**`_Loop` の クリップに ループを 立てる。**
        //   FBX から 取りこんだ だけでは `loopTime` は 立たない ので、
        //   **1周 したら 最後の コマで 固まる**。本人「走りっぱなしだと 途中から モーションが なくなる」。
        //   `normalizedTime` は 1.0 を こえて 増えつづける ので 「動いて いる」ように 見えて
        //   気づきにくい（角度を 出して 初めて 分かる・2026-09-06）
        var cl = mi.defaultClipAnimations;
        bool loopIru = false;
        for (int i = 0; i < cl.Length; i++) {
            bool wa = cl[i].name.EndsWith("_Loop") || cl[i].name.Contains("Idle");
            if (cl[i].loopTime != wa) { cl[i].loopTime = wa; loopIru = true; }
        }
        if (loopIru) mi.clipAnimations = cl;

        bool naosu = loopIru
                     || mi.animationType != ModelImporterAnimationType.Human || !mi.importAnimation
                     || mi.materialImportMode != ModelImporterMaterialImportMode.None;
        if (naosu) {
            mi.animationType = ModelImporterAnimationType.Human;
            mi.importAnimation = true;
            // 絵は 要らない。動きだけ つかう（新しい Unity は importMaterials では なく こちら）
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.resampleCurves = true;
            mi.SaveAndReimport();
            Debug.Log("[Probe] AnimSetup: Humanoid に 直して 読みなおした");
        } else {
            Debug.Log("[Probe] AnimSetup: すでに Humanoid");
        }
        Ichiran();
    }

    public static void Ichiran() {
        var all = AssetDatabase.LoadAllAssetsAtPath(FBX);
        var cl = new List<AnimationClip>();
        Avatar av = null;
        foreach (var a in all) {
            if (a is AnimationClip c && !c.name.StartsWith("__preview__")) cl.Add(c);
            if (a is Avatar v) av = v;
        }
        int wa2 = 0;
        foreach (var c in cl) if (c.isLooping) wa2++;
        Debug.Log("[Probe] AnimClip " + cl.Count + " 本（うち ループ " + wa2 + " 本）  avatar=" +
                  (av == null ? "なし" : (av.isHuman ? "Humanoid ○" : "Humanoid ×")));
        cl.Sort((x, y) => string.CompareOrdinal(x.name, y.name));
        var sb = new StringBuilder();
        foreach (var c in cl) sb.Append(c.name).Append("(").Append(c.length.ToString("F2")).Append("s) ");
        Debug.Log("[Probe] clips: " + sb);
        // 歩き・走り らしき ものだけ もう一度
        var sb2 = new StringBuilder();
        foreach (var c in cl) {
            var n = c.name.ToLower();
            if (n.Contains("walk") || n.Contains("run") || n.Contains("idle") || n.Contains("jog"))
                sb2.Append(c.name).Append("(").Append(c.length.ToString("F2")).Append("s) ");
        }
        Debug.Log("[Probe] ★ほしいもの: " + sb2);
    }
}
