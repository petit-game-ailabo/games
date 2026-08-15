using UnityEngine;
using UnityEditor;

// 地形の 高さを 数字で 見る ための 道具。
// **絵を 見ても 高さは 読めない**ので、置き場所を 決める まえに ここで 測る。
// 使いかた: Unity.exe -batchmode -executeMethod TerrainProbe.Dump
public static class TerrainProbe {
    public static void Dump() {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[Probe] 山への 一本道の すじ（x=-20）：z / 道の 高さ / まわりの 素の 高さ");
        for (float z = 8f; z >= -30f; z -= 2f) {
            sb.AppendFormat("  z={0,6:F1}  みち={1,7:F2}  そで={2,7:F2}  よこ6m={3,7:F2}\n",
                z, TerrainGen.Height(-20f, z), TerrainGen.RawHeight(-20f, z),
                TerrainGen.RawHeight(-26f, z));
        }
        sb.AppendLine("[Probe] 遊べる 四角の おくの へり（z=-10）：x / 高さ");
        for (float x = -26f; x <= 26f; x += 4f)
            sb.AppendFormat("  x={0,6:F1}  h={1,7:F2}\n", x, TerrainGen.Height(x, -10f));
        sb.AppendLine("[Probe] 高台 LookoutY=" + TerrainGen.LookoutY.ToString("F2"));
        Debug.Log(sb.ToString());
    }
}
