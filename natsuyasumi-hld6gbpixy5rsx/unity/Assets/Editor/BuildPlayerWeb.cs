// ブラウザ版（WebGL）を つくる。人に わたすのは これが いちばん らく＝URLひとつ。
// Unity -batchmode -quit -executeMethod BuildPlayerWeb.Build
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildPlayerWeb {
    public static void Build() {
        // **圧縮を 切る。** GitHub Pages は gzip/brotli の ヘッダを 返せないので、
        // 圧縮したままだと 読みこみに 失敗する
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        // APPLICATION: ＝Unity同梱の ひな型。PROJECT: だと 自作テンプレを さがして 失敗する
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        PlayerSettings.WebGL.memorySize = 512;
        PlayerSettings.runInBackground = true;
        PlayerSettings.companyName = "petit-game-ailabo";
        PlayerSettings.productName = "natsuyasumi-okuyuki";
        PlayerSettings.defaultWebScreenWidth = 960;
        PlayerSettings.defaultWebScreenHeight = 540;

        var opts = new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Zashiki.unity" },
            locationPathName = "../unity-web",       // リポジトリ直下に 出す（Pagesで 配る）
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("[BuildPlayerWeb] result=" + report.summary.result +
                  " errors=" + report.summary.totalErrors +
                  " size=" + (report.summary.totalSize / (1024 * 1024)) + "MB");
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
