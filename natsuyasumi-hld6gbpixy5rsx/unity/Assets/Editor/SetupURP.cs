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

        FixPixelArt();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SetupURP] done. colorSpace=" + PlayerSettings.colorSpace);
    }

    // ドット絵は **点フィルタ・圧縮なし・ミップなし**。ぼかすと ドットの 良さが 消える。
    // 素材を 足すたびに 手で 直すのは 必ず 漏れるので、**フォルダごと まとめて** そろえる。
    // Unity -batchmode -quit -executeMethod SetupURP.FixPixelArt でも 単体で 呼べる
    [MenuItem("なつやすみ/ドット絵の 取りこみを そろえる")]
    public static void FixPixelArt() {
        // スプライト（板の 絵）は 端を くり返さない＝Clamp。
        // 世界の テクスチャは **敷きつめる**ので Repeat のまま さわらない（畳や 草地が 1枚に なってしまう）
        Sweep("Assets/Art/Sprites",  TextureWrapMode.Clamp);
        Sweep("Assets/Art/Textures", null);
        FixUI();
    }

    // 画面まわり：枠は 9スライス（かどを のこして まん中だけ のばす）、字は 点で 出す
    [MenuItem("なつやすみ/画面まわりの 取りこみを そろえる")]
    public static void FixUI() {
        Slice("Assets/Art/UI/panel.png", 6);
        Slice("Assets/Art/UI/panel_light.png", 6);
        Slice("Assets/Art/UI/icon_net.png", 0);

        // 書体。**にじませない。** ふつうに 取りこむと きれいに ぼかされて、
        // せっかくの 点で 描かれた 書体が だいなしに なる
        var fi = AssetImporter.GetAtPath("Assets/Art/Fonts/PixelMplus12-Regular.ttf") as TrueTypeFontImporter;
        if (fi == null) { Debug.LogWarning("[SetupURP] 書体が 見つからない"); return; }
        if (fi.fontRenderingMode != FontRenderingMode.HintedRaster || fi.fontSize != 12) {
            fi.fontRenderingMode = FontRenderingMode.HintedRaster;   // 点の まま 出す
            fi.fontSize = 12;                                        // もとの 絵と 同じ 大きさ
            fi.includeFontData = true;                               // ビルドに 同梱（M+の 許諾内）
            fi.SaveAndReimport();
            Debug.Log("[SetupURP] font: HintedRaster 12px");
        }
    }

    static void Slice(string path, int border) {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning("[SetupURP] not found: " + path); return; }
        var want = new Vector4(border, border, border, border);
        bool ok = ti.textureType == TextureImporterType.Sprite
                  && ti.filterMode == FilterMode.Point
                  && !ti.mipmapEnabled
                  && ti.spriteBorder == want
                  && ti.textureCompression == TextureImporterCompression.Uncompressed;
        if (ok) return;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 16;
        ti.spriteBorder = want;
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();
        Debug.Log("[SetupURP] ui sprite: " + path + " border=" + border);
    }

    static void Sweep(string dir, TextureWrapMode? wrap) {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { dir })) {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            // ★**借りものの アセットは ここの 対象外。**（2026-08-16）
            //   megakit は 1024px の 描きこんだ 絵で、点フィルタ＋圧縮なしに すると
            //   ざらざらに なった うえに ビルドが 何十MBも ふくらむ。
            //   取りこみかたは MegaKit.Setup が べつに 面倒を みる
            if (path.Contains("/megakit/")) continue;
            // ★描き おこしの キャラ絵も 対象外（2026-08-30）。点フィルタだと 縮小で ぎざぎざに なる
            if (path.Contains("marisa_walk") || path.Contains("/tachie/")) {
                var ti3 = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti3 == null) continue;
                // ★ここを まちがえると 絵が ぼける（2026-08-30・本人「まだ解像度が下がってる」）
                //   ・maxTextureSize 既定2048 → 2048x2560 の アトラスが 0.8倍に 縮んで いた
                //   ・ミップマップ → 画面の 3倍の こまかさが ある のに ぼけた 段を えらぶ
                //     キャラは 画面での 大きさが ほぼ 一定 なので **ミップは 要らない**
                bool ok3 = ti3.filterMode == FilterMode.Bilinear
                           && !ti3.mipmapEnabled
                           && ti3.maxTextureSize >= 4096
                           && ti3.textureCompression == TextureImporterCompression.Uncompressed;
                if (!ok3) {
                    ti3.textureType = TextureImporterType.Default;
                    ti3.filterMode = FilterMode.Bilinear;
                    ti3.mipmapEnabled = false;
                    ti3.alphaIsTransparency = true;
                    ti3.textureCompression = TextureImporterCompression.Uncompressed;
                    ti3.maxTextureSize = 4096;
                    ti3.SaveAndReimport();
                    Debug.Log("[SetupURP] char art: " + path + " (max4096/mipなし/なめらか)");
                }
                continue;
            }
            // ★地めんの 写真素材（ji_*）と 焼いた 一枚絵（niwa_jimen）も 対象外（2026-08-31）。
            //   ・**ミップと 異方性フィルタが 要る**：カメラの ふせ角が 10°＝地面を
            //     ほぼ 真横から 見る ので、ミップ無しだと 奥が じゃりじゃり ちらつく。
            //     異方性は 斜めに 引きのばして 読む ための もので、地面には 必須
            //   ・**読みだし可**：一枚絵を 焼く ときに GetPixels32 で 中身を 読む
            {
                string fn0 = System.IO.Path.GetFileNameWithoutExtension(path);
                if (fn0.StartsWith("ji_") || fn0.StartsWith("niwa_jimen")) {
                    var tig = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (tig == null) continue;
                    bool moto = fn0.StartsWith("ji_");    // 素材は 焼く ために 読みだす
                    bool okg = tig.filterMode == FilterMode.Bilinear && tig.mipmapEnabled
                               && tig.anisoLevel >= 8 && tig.isReadable == moto
                               && tig.maxTextureSize >= 4096;
                    if (!okg) {
                        tig.textureType = TextureImporterType.Default;
                        tig.filterMode = FilterMode.Bilinear;
                        tig.mipmapEnabled = true;
                        tig.anisoLevel = 8;
                        tig.isReadable = moto;
                        tig.wrapMode = TextureWrapMode.Repeat;
                        tig.maxTextureSize = 4096;
                        tig.SaveAndReimport();
                        Debug.Log("[SetupURP] jimen: " + path);
                    }
                    continue;
                }
            }
            // ★写真から 起こした 遠景の 描き割りも 対象外（2026-08-30）。
            //   点フィルタ＋ミップ無しだと 縮小で ちらつき、色の ノイズに 見える
            string fn = System.IO.Path.GetFileNameWithoutExtension(path);
            // kage_＝接地の影の ぼかし。点フィルタだと 輪が 階段に なる
            if (fn.StartsWith("satoyama") || fn.StartsWith("yama_") || fn.StartsWith("kumo")
                || fn.StartsWith("kage_")) {
                var ti2 = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti2 != null && (ti2.filterMode != FilterMode.Bilinear || !ti2.mipmapEnabled)) {
                    ti2.textureType = TextureImporterType.Default;
                    ti2.filterMode = FilterMode.Bilinear;
                    ti2.mipmapEnabled = true;
                    ti2.alphaIsTransparency = true;
                    ti2.textureCompression = TextureImporterCompression.Uncompressed;
                    ti2.SaveAndReimport();
                    Debug.Log("[SetupURP] photo backdrop: " + path);
                }
                continue;
            }
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            bool ok = !ti.mipmapEnabled && !ti.streamingMipmaps
                      && ti.filterMode == FilterMode.Point
                      && ti.alphaIsTransparency
                      && (wrap == null || ti.wrapMode == wrap.Value)
                      && ti.textureCompression == TextureImporterCompression.Uncompressed;
            if (ok) continue;                                   // 済みなら 触らない（毎回 やると 遅い）
            ti.textureType = TextureImporterType.Default;
            ti.mipmapEnabled = false;
            ti.streamingMipmaps = false;
            ti.filterMode = FilterMode.Point;
            ti.alphaIsTransparency = true;
            if (wrap != null) ti.wrapMode = wrap.Value;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
            Debug.Log("[SetupURP] pixel texture: " + path);
        }
    }
}
