using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// 庭シーン（Niwa.unity）の プレイヤー。mura とは 別の 出力さき。
public static class BuildNiwaPlayer {
    public static void Win() {
        var opts = new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Niwa.unity" },
            locationPathName = "Builds/niwa-win/niwa.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("[Probe] BuildNiwaPlayer.Win result=" + report.summary.result);
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }

    public static void Web() {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        PlayerSettings.WebGL.memorySize = 512;
        PlayerSettings.runInBackground = true;
        PlayerSettings.companyName = "petit-game-ailabo";
        PlayerSettings.productName = "niwa";
        // WebGL では FSR の シェーダが 死ぬ → Linear（mura で 学んだ 型）
        var urp = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(
            "Assets/Settings/URP_Asset.asset");
        if (urp != null && urp.upscalingFilter != UnityEngine.Rendering.Universal.UpscalingFilterSelection.Linear) {
            urp.upscalingFilter = UnityEngine.Rendering.Universal.UpscalingFilterSelection.Linear;
            EditorUtility.SetDirty(urp);
            AssetDatabase.SaveAssets();
        }
        // ★圧縮は **コードで 指定する**（2026-08-31）。ProjectSettings.asset を 直に 書きかえても
        //   Unity が 終わる ときに 自分の 設定を 書きもどして 消える。
        //   公開まえの 素の 大きさは .data 61.6MB＋.wasm 42MB＝103MB あった。
        //   GitHub Pages は Content-Encoding を つけられない ので ブラウザ側で 展開する
        //   （gzip。Brotli の ほうが 小さいが JS での 展開が おそい）
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        var opts = new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Niwa.unity" },
            locationPathName = "../niwa-web",
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("[Probe] BuildNiwaPlayer.Web result=" + report.summary.result +
                  " size=" + (report.summary.totalSize / (1024 * 1024)) + "MB");
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        // キャッシュずれ よけ（Pages の 反映ちゅうの wasm/data 混在で 何も 出なくなる）
        var htmlPath = "../niwa-web/index.html";
        if (System.IO.File.Exists(htmlPath)) {
            string v = "?v=" + System.DateTime.Now.ToString("yyyyMMddHHmm");
            var html = System.IO.File.ReadAllText(htmlPath);
            // ★圧縮を 入れると 名まえが .unityweb に なる。版数を つける さきも そちら
            foreach (var e in new[] { ".loader.js", ".data.unityweb",
                                      ".framework.js.unityweb", ".wasm.unityweb",
                                      ".data", ".framework.js", ".wasm" })
                if (html.Contains(e + "\"")) html = html.Replace(e + "\"", e + v + "\"");
            System.IO.File.WriteAllText(htmlPath, html);
        }
    }
}
