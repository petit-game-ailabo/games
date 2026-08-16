// Quaternius「Medieval Village MegaKit」(CC0) を つかう ための したく。
//
// ★これは **試し**（本人 2026-08-16）：「アセットを つかったら どれだけ 質の 高い ものが
//   できるのか テストしたい。風景に 合わなくて いい」。納屋を 消して ここへ 建てる。
//
// ★アセットの 組み立ては 手さぐりで やっては いけない。
//   部品の 大きさと **軸(pivot)が どこに あるか** が 分からないと、置いた とたん
//   半分 めりこむ。地形と 同じで **まず 数字を 出す**（MegaKit.Dump）。
//
// つかいかた（rebuild.ps1 -Only MegaKitSetup / -Only MegaKitDump）
//   Setup … マテリアルを 作り、FBX と 絵の 取りこみ かたを ととのえる
//   Dump  … 部品ごとの 大きさ・軸の 位置を Data/megakit_bounds.txt に 出す
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class MegaKit {

    public const string ModelDir = "Assets/Art/Models/megakit/";
    public const string TexDir   = "Assets/Art/Textures/megakit/";
    public const string MatDir   = "Assets/Art/Materials/megakit/";

    // (マテリアル名, 色の絵, 法線の絵, なめらかさ, 金っぽさ)
    // ★名まえは **FBX の 中の 名まえと 1字も ちがえない**。Unity は
    //   materialName=BasedOnMaterialName / materialSearch=Everywhere で
    //   この 名まえを たよりに ひもづける。ずれると 全部 まっ白に なる
    static readonly (string name, string baseTex, string nrmTex, float smooth, float metal)[] MATS = {
        ("MI_Plaster",       "T_Plaster_BaseColor",     "T_Plaster_Normal",     0.14f, 0f),
        ("MI_WoodTrim",      "T_WoodTrim_BaseColor",    "T_WoodTrim_Normal",    0.20f, 0f),
        ("MI_WoodTrim_Wear", "T_WoodTrim_BaseColor",    "T_WoodTrim_Normal",    0.12f, 0f),
        ("MI_UnevenBrick",   "T_UnevenBrick_BaseColor", "T_UnevenBrick_Normal", 0.12f, 0f),
        ("MI_Brick",         "T_Brick_BaseColor",       "T_Brick_Normal",       0.12f, 0f),
        ("MI_RedBrick",      "T_RedBrick_BaseColor",    "T_Brick_Normal",       0.15f, 0f),
        ("MI_RockTrim",      "T_RockTrim_BaseColor",    "T_RockTrim_Normal",    0.16f, 0f),
        ("MI_RoundTiles",    "T_RoundTiles_BaseColor",  "T_RoundTiles_Normal",  0.28f, 0f),
    };

    [MenuItem("なつやすみ/MegaKit の したく")]
    public static void Setup() {
        Directory.CreateDirectory(MatDir);

        // ---------- 絵の 取りこみ かた
        // 法線は NormalMap に しないと ただの 青い 絵として 貼られて 凹凸が 出ない。
        // あらさ・ORM は **sRGB を 切る**（明るさの 曲線が かかると 値が ずれる）
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TexDir.TrimEnd('/') })) {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            var ti = AssetImporter.GetAtPath(p) as TextureImporter;
            if (ti == null) continue;
            bool isNrm = p.Contains("_Normal");
            bool isData = isNrm || p.Contains("_ORM") || p.Contains("_Roughness");
            var want = isNrm ? TextureImporterType.NormalMap : TextureImporterType.Default;
            bool srgb = !isData;
            if (ti.textureType == want && ti.sRGBTexture == srgb && ti.maxTextureSize >= 1024) continue;
            ti.textureType = want;
            ti.sRGBTexture = srgb;
            ti.maxTextureSize = 1024;
            ti.mipmapEnabled = true;
            // ★**この アセットだけは 点フィルタに しない。** ほかの 絵は ドット絵なので
            //   SetupURP.FixPixelArt が 点フィルタ＋圧縮なしに するが、こちらは
            //   1024px の 描きこんだ 絵。点フィルタに すると ざらざらに なる
            ti.filterMode = FilterMode.Bilinear;
            ti.anisoLevel = 4;
            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();
        }

        // ---------- マテリアル
        foreach (var d in MATS) {
            string path = MatDir + d.name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            var bt = Tex(d.baseTex);
            var nt = Tex(d.nrmTex);
            if (bt != null) { m.SetTexture("_BaseMap", bt); m.mainTexture = bt; }
            if (nt != null) { m.SetTexture("_BumpMap", nt); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 1f); }
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", d.smooth);
            m.SetFloat("_Metallic", d.metal);
            EditorUtility.SetDirty(m);
        }
        // ツタ＝抜き。板 1まいなので 両面に する
        {
            var m = Ensure("MI_Vine");
            var bt = Tex("T_VineLeaf");
            if (bt != null) { m.SetTexture("_BaseMap", bt); m.mainTexture = bt; }
            m.SetFloat("_Surface", 0f);                 // Opaque のまま 抜く
            m.SetFloat("_AlphaClip", 1f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cutoff", 0.5f);
            m.SetFloat("_Cull", 0f);
            m.doubleSidedGI = true;
            m.SetFloat("_Smoothness", 0.18f);
            EditorUtility.SetDirty(m);
        }
        // 窓ガラス＝すこし すける。中から あかりが もれて いる ように 光らせる
        {
            var m = Ensure("MI_WindowGlass");
            m.SetFloat("_Surface", 1f);                 // Transparent
            m.SetFloat("_Blend", 0f);
            m.renderQueue = 3000;
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetColor("_BaseColor", new Color(0.90f, 0.74f, 0.42f, 0.72f));
            m.SetFloat("_Smoothness", 0.85f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(1.00f, 0.80f, 0.48f) * 0.55f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(m);
        }
        // 鉄の かざり
        {
            var m = Ensure("MI_MetalOrnaments");
            m.SetColor("_BaseColor", new Color(0.16f, 0.16f, 0.18f));
            m.SetFloat("_Metallic", 0.85f);
            m.SetFloat("_Smoothness", 0.45f);
            EditorUtility.SetDirty(m);
        }
        AssetDatabase.SaveAssets();

        // ---------- FBX の 取りこみ かた
        // **外の マテリアルを 名まえで さがさせる。** そうしないと FBX の 中の
        // マテリアルが そのまま 入って、こちらで 作った URP の マテリアルが 効かない
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ModelDir.TrimEnd('/') })) {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            var mi = AssetImporter.GetAtPath(p) as ModelImporter;
            if (mi == null) continue;
            bool ok = mi.materialImportMode == ModelImporterMaterialImportMode.ImportStandard
                   && mi.materialLocation == ModelImporterMaterialLocation.External
                   && mi.materialName == ModelImporterMaterialName.BasedOnMaterialName
                   && mi.materialSearch == ModelImporterMaterialSearch.Everywhere
                   && mi.useFileScale == false && mi.globalScale == 1f
                   && mi.bakeAxisConversion
                   && mi.importCameras == false;
            if (ok) continue;
            // ★**縮尺と 軸を そろえる。** そのまま 入れると 壁 1まいが
            //   0.02m x 0.03m に なり、しかも **高さが Z軸**に なる（Blender の 出しかた）。
            //   useFileScale を 切って 1倍に すると 壁は 2m x 3m＝ちゃんとした 部品に なる
            mi.useFileScale = false;
            mi.globalScale = 1f;
            mi.bakeAxisConversion = true;     // Z上がり → Y上がり に 焼きこむ
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            mi.materialLocation = ModelImporterMaterialLocation.External;
            mi.materialName = ModelImporterMaterialName.BasedOnMaterialName;
            mi.materialSearch = ModelImporterMaterialSearch.Everywhere;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importAnimation = false;
            mi.addCollider = false;
            EditorUtility.SetDirty(mi);
            mi.SaveAndReimport();
            n++;
        }
        AssetDatabase.Refresh();
        Debug.Log("[MegaKit] したく できた。マテリアル " + (MATS.Length + 3) + " / FBX 直した " + n + "点");
    }

    static Material Ensure(string name) {
        string path = MatDir + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        return m;
    }

    static Texture2D Tex(string name) {
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + name + ".png");
        if (t == null) Debug.LogWarning("[MegaKit] 絵が ない: " + name);
        return t;
    }

    /// <summary>部品の 大きさと 軸の 位置を 出す。**置き場所を 決める 前に 必ず これ**</summary>
    [MenuItem("なつやすみ/MegaKit の 寸法を 出す")]
    public static void Dump() {
        var sb = new StringBuilder();
        sb.AppendLine("# MegaKit の 部品の 寸法（m）");
        sb.AppendLine("# size = 大きさ / center = 軸から 見た まん中 / min-max = 軸から の はば");
        sb.AppendLine("# 軸が (0,0,0) で min.y=0 なら 「床に 置ける」部品");
        var names = new List<string>();
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ModelDir.TrimEnd('/') }))
            names.Add(AssetDatabase.GUIDToAssetPath(guid));
        names.Sort();
        // ★**「絵を 見て 高さは 読めない」の アセット版。**
        //   mesh.bounds は もとの ままの 向き（この キットは Z上がり）なので、
        //   それを 読んでも **置いた ときの 向きに ならない**。
        //   実さいに 置いて Renderer.bounds を 測る。これだけが 本当の 数字
        foreach (var p in names) {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (src == null) continue;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            bool first = true;
            var b = new Bounds();
            var mats = new List<string>();
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) {
                if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
                foreach (var m in r.sharedMaterials)
                    if (m != null && !mats.Contains(m.name)) mats.Add(m.name);
            }
            Object.DestroyImmediate(go);
            if (first) continue;
            sb.AppendFormat("{0,-46} size=({1,6:0.00},{2,6:0.00},{3,6:0.00})  x[{4,6:0.00}..{5,5:0.00}] y[{6,5:0.00}..{7,5:0.00}] z[{8,6:0.00}..{9,5:0.00}]  {10}\n",
                Path.GetFileNameWithoutExtension(p), b.size.x, b.size.y, b.size.z,
                b.min.x, b.max.x, b.min.y, b.max.y, b.min.z, b.max.z,
                string.Join(",", mats));
        }
        string outDir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Data");
        Directory.CreateDirectory(outDir);
        string outPath = Path.Combine(outDir, "megakit_bounds.txt");
        File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(false));
        Debug.Log("[MegaKit] 寸法を 出した: " + outPath + "  " + names.Count + "点");
    }

    /// <summary>組んだ 場面の なかで **大きすぎる もの**を さがす。
    /// 「画面ぜんぶが 1色に なった」ときは たいてい 巨大な 面が カメラを 包んで いる</summary>
    [MenuItem("なつやすみ/場面の でかい ものを さがす")]
    public static void Inspect() {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            "Assets/Scenes/Zashiki.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
        var list = new List<(float d, string path, Vector3 size, Vector3 c)>();
        foreach (var go in scene.GetRootGameObjects())
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) {
                var b = r.bounds;
                float d = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                if (d < 14f) continue;
                string path = r.name;
                for (var t = r.transform.parent; t != null; t = t.parent) path = t.name + "/" + path;
                list.Add((d, path, b.size, b.center));
            }
        list.Sort((a, b) => b.d.CompareTo(a.d));
        for (int i = 0; i < Mathf.Min(14, list.Count); i++) {
            var e = list[i];
            Debug.Log(string.Format("[でかい] {0,7:0.0}m  {1}  size=({2:0.0},{3:0.0},{4:0.0}) center=({5:0.0},{6:0.0},{7:0.0})",
                e.d, e.path, e.size.x, e.size.y, e.size.z, e.c.x, e.c.y, e.c.z));
        }
        Debug.Log("[でかい] 14m を こえる もの " + list.Count + " 個");
    }

    // ---------------------------------------------------------------- 組み立ての 道具
    /// <summary>部品を 1つ 置く。**このキットを 置くときは 必ず これを 通す。**
    ///
    /// 2つ しかけが ある。どちらも 抜かすと 事故に なる。
    ///  1) **X まわりに +90度 まわす。** 部品は ねて いて、高さが -Z・厚みが Y。
    ///     まわすと 高さが +Y、外がわが -Z に なる（手前を 見せる 面は yaw=180）
    ///  2) **大きさを 1 に もどす。** prefab の 根っこに 100倍が 入って いて、
    ///     そのままだと 壁 1まいが 200m x 312m に なる
    /// </summary>
    public static GameObject Put(Transform parent, string piece, Vector3 pos, float yaw = 0f) {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + piece + ".fbx");
        if (src == null) { Debug.LogWarning("[MegaKit] 部品が ない: " + piece); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        go.name = piece;
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = Vector3.one;
        return go;
    }

    /// <summary>壁を ならべる ときの 書きやすい かたち</summary>
    public static GameObject Put(Transform parent, string piece, float x, float y, float z, float yaw) {
        return Put(parent, piece, new Vector3(x, y, z), yaw);
    }

    /// <summary>あたりの 箱。FBX には あたりが 無い ので 手で 置く。
    /// **まるごと 1個で かこうと 戸口ごと ふさがって 中に 入れなく なる**</summary>
    public static void Hit(Transform parent, Vector3 pos, Vector3 size) {
        var go = new GameObject("Kit_Hit");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.layer = 2;                       // 真下への レイの じゃまを しない
        go.AddComponent<BoxCollider>().size = size;
    }

    /// <summary>その 場の マテリアルを とる（土台などの 箱に 貼る）</summary>
    public static Material Mat(string name) {
        return AssetDatabase.LoadAssetAtPath<Material>(MatDir + name + ".mat");
    }
}
