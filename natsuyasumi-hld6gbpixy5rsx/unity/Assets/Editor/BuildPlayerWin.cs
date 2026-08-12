// Windows用に ビルドする（絵の たしかめ用。出荷と 同じ 描画経路を とおす）。
// Unity -batchmode -quit -executeMethod BuildPlayerWin.Build
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildPlayerWin {
    public static void Build() {
        var opts = new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Zashiki.unity" },
            locationPathName = "Builds/win/natsuyasumi.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,   // 開発ビルドの すかし文字を 出さない
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("[BuildPlayerWin] result=" + report.summary.result +
                  " errors=" + report.summary.totalErrors +
                  " size=" + (report.summary.totalSize / (1024 * 1024)) + "MB");
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
