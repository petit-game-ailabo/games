// Kenney「Nature Kit」(CC0) を つかう ための したく。megakit と 同じ 型：
//   **手さぐりで 組まない。まず 数字を 出す**（KenneyKit.Dump）。
//   rebuild.ps1 -Only KenneyKit.Setup / -Only KenneyKit.Dump
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class KenneyKit {

    public const string ModelDir = "Assets/Art/Models/kenney/";
    public const string MatDir   = "Assets/Art/Materials/kenney";

    // ★Kenney の 素の 配色は ミント/パステル調（葉も 草も 青みどり）。僕夏の 空気に
    //   合わない ので、**自然の 色に 塗りかえた 同名材質**を 作って 結びなおす（CC0・改変OK）
    static readonly (string name, float r, float g, float b)[] IRO = {
        ("grass",        0.36f, 0.55f, 0.28f),
        ("leafsGreen",   0.33f, 0.52f, 0.27f),
        ("leafsDark",    0.24f, 0.42f, 0.22f),
        ("leafsFall",    0.80f, 0.52f, 0.24f),
        ("woodBark",     0.45f, 0.35f, 0.26f),
        ("woodBarkDark", 0.38f, 0.29f, 0.22f),
        ("wood",         0.58f, 0.44f, 0.31f),
        ("woodDark",     0.42f, 0.32f, 0.24f),
        ("woodInner",    0.80f, 0.70f, 0.55f),
        ("woodBirch",    0.88f, 0.84f, 0.76f),
        ("stone",        0.62f, 0.63f, 0.60f),
        ("stoneDark",    0.48f, 0.49f, 0.47f),
        ("dirt",         0.55f, 0.42f, 0.30f),
        ("dirtDark",     0.45f, 0.34f, 0.25f),
        ("colorRed",     0.82f, 0.30f, 0.28f),
        ("colorYellow",  0.93f, 0.72f, 0.30f),
        ("colorPurple",  0.62f, 0.54f, 0.85f),
        ("_defaultMat",  0.85f, 0.85f, 0.82f),
    };
    static Color Iro(string name) {
        foreach (var (n, r, g, b) in IRO) if (n == name) return new Color(r, g, b);
        return Color.white;
    }

    public static void Setup() {
        Directory.CreateDirectory(MatDir);
        int mats = 0, models = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Art/Models/kenney" })) {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path)) {
                var m = obj as Material;
                if (m == null) continue;
                string mp = MatDir + "/" + m.name + ".mat";
                var um = AssetDatabase.LoadAssetAtPath<Material>(mp);
                if (um == null) {
                    um = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(um, mp);
                    mats++;
                }
                um.color = Iro(m.name);                 // 毎回 塗りなおす（表を 直したら 反映）
                um.SetFloat("_Smoothness", 0.05f);      // つや消し（夜の 白うき よけ）
                EditorUtility.SetDirty(um);
            }
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            if (imp != null) {
                imp.addCollider = false;
                // ★materialName/materialSearch を 置くだけでは 新しい 取り込みでは 効かない。
                //   SearchAndRemapMaterials で ExternalObjects に 登録して はじめて
                //   こちらの 塗りかえた 材質に つながる（v1 は ミントの まま だった）
                imp.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName,
                                            ModelImporterMaterialSearch.Everywhere);
                imp.SaveAndReimport();
                models++;
            }
        }
        Debug.Log("[Probe] KenneyKit.Setup models=" + models + " newMats=" + mats);
    }

    // 部品ごとの 大きさ・軸の 位置・材質を Data/kenney_bounds.txt へ
    public static void Dump() {
        var sb = new StringBuilder();
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Art/Models/kenney" })) {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            var g = Object.Instantiate(prefab);
            g.transform.localScale = Vector3.one;      // ★測るときと 置くときで 条件を そろえる
            var rs = g.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) { Object.DestroyImmediate(g); continue; }
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            var matNames = new HashSet<string>();
            foreach (var r in rs) foreach (var m in r.sharedMaterials) if (m != null) matNames.Add(m.name + "(" + m.shader.name + ")");
            sb.AppendLine(Path.GetFileNameWithoutExtension(path)
                + "  size=" + b.size.ToString("F2")
                + "  min=" + b.min.ToString("F2") + " max=" + b.max.ToString("F2")
                + "  rootScale=" + prefab.transform.localScale.ToString("F2")
                + "  mats=" + string.Join(",", matNames));
            Object.DestroyImmediate(g);
        }
        Directory.CreateDirectory("Data");
        File.WriteAllText("Data/kenney_bounds.txt", sb.ToString());
        Debug.Log("[Probe] KenneyKit.Dump -> Data/kenney_bounds.txt (" + sb.Length + "字)");
    }

    // 置くヘルパ。root100倍などの くせが あれば ここで 吸収する（Dumpを 見てから 決める）
    public static GameObject Put(Transform parent, string name, Vector3 pos, float yaw = 0f, float scale = 1f) {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + name + ".fbx");
        if (prefab == null) { Debug.LogError("[KenneyKit] ない: " + name); return null; }
        var g = (GameObject)Object.Instantiate(prefab);
        g.name = "K_" + name;
        g.transform.SetParent(parent, false);
        g.transform.localScale = Vector3.one * scale;   // ★rootの 拡大を 打ちけす
        g.transform.position = pos;
        g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        return g;
    }
}
