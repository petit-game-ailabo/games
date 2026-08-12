// URP（描画の しくみ）を 有効に して、ドット絵の 取りこみかたを そろえる。
// Unity -batchmode -quit -executeMethod SetupURP.Run
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class SetupURP {
    const string Dir = "Assets/Settings/";

    public static void Run() {
        Directory.CreateDirectory(Dir);

        // 色の あつかいは Linear。ブルームや 光の 出かたが 自然に なる
        PlayerSettings.colorSpace = ColorSpace.Linear;

        var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
        // **これが 無いと ポストFXは まるごと 効かない。**
        // コードで 作った Rendererは 後処理用の シェーダ束(PostProcessData)が 空のままに なる。
        // 版によって 取りかたが ちがうので、決め打ちの みち → 検索 の順で さがす
        var ppd = AssetDatabase.LoadAssetAtPath<PostProcessData>(
            "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
        if (ppd == null) {
            foreach (var g in AssetDatabase.FindAssets("t:PostProcessData")) {
                ppd = AssetDatabase.LoadAssetAtPath<PostProcessData>(AssetDatabase.GUIDToAssetPath(g));
                if (ppd != null) break;
            }
        }
        rendererData.postProcessData = ppd;
        Debug.Log("[SetupURP] postProcessData = " + (ppd != null ? AssetDatabase.GetAssetPath(ppd) : "NULL"));
        AssetDatabase.CreateAsset(rendererData, Dir + "URP_Renderer.asset");

        var urp = UniversalRenderPipelineAsset.Create(rendererData);
        AssetDatabase.CreateAsset(urp, Dir + "URP_Asset.asset");

        urp.supportsHDR = true;                 // 明るい ところが とぶ＝ブルームが 効く
        urp.shadowDistance = 40f;
        urp.msaaSampleCount = 1;                // ドット絵を にじませない
        urp.renderScale = 1f;

        GraphicsSettings.defaultRenderPipeline = urp;
        QualitySettings.renderPipeline = urp;

        // **ミップマップの 読みこみを 切る。** 入れたままだと、絵を 書きだす とき（batchmode）に
        // 遠くの 面の ミップが 読まれず まっ黒に なる（実際 床が 黒く なった）
        QualitySettings.streamingMipmapsActive = false;
        QualitySettings.globalTextureMipmapLimit = 0;

        FixPixelArt("Assets/Art/Sprites/chars.png");
        FixPixelArt("Assets/Art/Sprites/props.png");

        // 世界の テクスチャは ミップを 切る。遠くの 面が 黒く なる 症状の 切りわけ用でもあり、
        // ドット絵に よせる うえでも ぼけない ほうが よい
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Textures" })) {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            // 世界の テクスチャも **点フィルタ**。ドット絵の キャラと 同じ 肌ざわりに そろえる
            // （調べた ところ、この 見た目の 肝は「3Dの 面に ドット絵の テクスチャ」だった）
            bool ok = !ti.mipmapEnabled && !ti.streamingMipmaps
                      && ti.filterMode == FilterMode.Point
                      && ti.textureCompression == TextureImporterCompression.Uncompressed;
            if (ok) continue;                                   // 済みなら 触らない（毎回 やると 遅い）
            ti.mipmapEnabled = false;
            ti.streamingMipmaps = false;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
            Debug.Log("[SetupURP] pixel texture: " + path);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SetupURP] done. colorSpace=" + PlayerSettings.colorSpace);
    }

    // ドット絵は **点フィルタ・圧縮なし**。ぼかすと ドットの 良さが 消える
    static void FixPixelArt(string path) {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning("[SetupURP] not found: " + path); return; }
        ti.textureType = TextureImporterType.Default;
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();
    }
}
