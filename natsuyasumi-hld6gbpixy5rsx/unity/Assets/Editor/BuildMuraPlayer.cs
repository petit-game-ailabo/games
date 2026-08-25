using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// 箱の村（Mura.unity）の プレイヤーを つくる。本編（Zashiki）とは 別の 出力さき。
public static class BuildMuraPlayer {
    public static void Win() {
        var opts = new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Mura.unity" },
            locationPathName = "Builds/mura-win/mura.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("[Probe] BuildMuraPlayer.Win result=" + report.summary.result);
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }

    public static void Web() {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        PlayerSettings.WebGL.memorySize = 512;
        PlayerSettings.runInBackground = true;
        PlayerSettings.companyName = "petit-game-ailabo";
        PlayerSettings.productName = "mura-hakonomura";
        var opts = new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Mura.unity" },
            locationPathName = "../mura-web",
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };
        // ★WebGL では FSRアップスケーリングの シェーダが 非対応で「PostProcessing render
        //   passes will not execute」に なる（DoF/Bloom が 全部 死ぬ）→ Linear に
        var urp = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(
            "Assets/Settings/URP_Asset.asset");
        if (urp != null && urp.upscalingFilter != UnityEngine.Rendering.Universal.UpscalingFilterSelection.Linear) {
            urp.upscalingFilter = UnityEngine.Rendering.Universal.UpscalingFilterSelection.Linear;
            EditorUtility.SetDirty(urp);
            AssetDatabase.SaveAssets();
            Debug.Log("[Probe] URP upscalingFilter -> Linear");
        }
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("[Probe] BuildMuraPlayer.Web result=" + report.summary.result +
                  " size=" + (report.summary.totalSize / (1024 * 1024)) + "MB");
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        // ★キャッシュずれ よけ：Pages の 反映ちゅうに 古い wasm＋新しい data が まざると
        //   何も 出ずに 止まる（本人 2026-08-26「何も表示されなくなった」）。
        //   ファイルURLに ビルドごとの 版数を つけて、混在を 起こさせない
        var htmlPath = "../mura-web/index.html";
        if (System.IO.File.Exists(htmlPath)) {
            string v = "?v=" + System.DateTime.Now.ToString("yyyyMMddHHmm");
            var html = System.IO.File.ReadAllText(htmlPath);
            html = html.Replace(".loader.js\"", ".loader.js" + v + "\"")
                       .Replace(".data\"", ".data" + v + "\"")
                       .Replace(".framework.js\"", ".framework.js" + v + "\"")
                       .Replace(".wasm\"", ".wasm" + v + "\"");
            System.IO.File.WriteAllText(htmlPath, html);
            Debug.Log("[Probe] index.html cache-bust " + v);
        }
    }
}
