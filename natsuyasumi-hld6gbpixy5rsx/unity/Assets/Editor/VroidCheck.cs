using UnityEditor;
using UnityEngine;

/// <summary>
/// VRM が ちゃんと 読めて いるかを **数で** たしかめる（2026-09-06）。
/// 絵を 見ても 「読めて いる つもり」は 分からない ので、
/// Humanoid の アバターに なって いるか・ボーンが 何本 かを 出す。
/// </summary>
public static class VroidCheck {
    public static void Check() {
        const string dir = "Assets/Art/Models/vroid";
        var guids = AssetDatabase.FindAssets("t:GameObject", new[] { dir });
        Debug.Log("[Probe] Vroid さがした " + guids.Length + " 件 in " + dir);
        foreach (var g in guids) {
            var p = AssetDatabase.GUIDToAssetPath(g);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go == null) { Debug.Log("[Probe]   よめない " + p); continue; }
            var an = go.GetComponentInChildren<Animator>();
            var sk = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            int tri = 0;
            foreach (var s in sk) if (s.sharedMesh != null) tri += s.sharedMesh.triangles.Length / 3;
            string av = an == null ? "Animator なし"
                : (an.avatar == null ? "avatar なし"
                   : (an.avatar.isHuman ? "Humanoid ○ ボーン" + an.avatar.humanDescription.human.Length
                                        : "Humanoid ×"));
            Debug.Log("[Probe]   " + p + "  " + av
                      + "  SkinnedMesh=" + sk.Length + " 三角形=" + tri
                      + "  子=" + go.GetComponentsInChildren<Transform>(true).Length);
        }
        // どんな 型の アセットが できたか
        foreach (var p in AssetDatabase.FindAssets("", new[] { dir })) {
            var path = AssetDatabase.GUIDToAssetPath(p);
            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            var kinds = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var a in all) {
                if (a == null) continue;
                var t = a.GetType().Name;
                kinds[t] = kinds.TryGetValue(t, out var n) ? n + 1 : 1;
            }
            var sb = new System.Text.StringBuilder();
            foreach (var kv in kinds) sb.Append(kv.Key).Append("x").Append(kv.Value).Append(" ");
            Debug.Log("[Probe]   なかみ " + path + " : " + sb);
        }
    }
}
