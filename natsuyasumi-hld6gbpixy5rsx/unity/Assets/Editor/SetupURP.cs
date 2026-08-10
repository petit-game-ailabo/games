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

        // 世界の テクスチャは ミップを 切る。遠くの 面が 黒く なる 症状の 切りわけ用でもあり、
        // ドット絵に よせる うえでも ぼけない ほうが よい
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Textures" })) {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            ti.mipmapEnabled = false;
            ti.streamingMipmaps = false;
            ti.SaveAndReimport();
            Debug.Log("[SetupURP] mipmap off: " + path);
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
