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
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("[Probe] BuildMuraPlayer.Web result=" + report.summary.result +
                  " size=" + (report.summary.totalSize / (1024 * 1024)) + "MB");
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
