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
        bool naosu = mi.animationType != ModelImporterAnimationType.Human || !mi.importAnimation
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
        Debug.Log("[Probe] AnimClip " + cl.Count + " 本  avatar=" +
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
