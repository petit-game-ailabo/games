using UnityEngine;
using UnityEditor;

// 地形の 高さを 数字で 見る ための 道具。
// **絵を 見ても 高さは 読めない**ので、置き場所を 決める まえに ここで 測る。
// 使いかた: Unity.exe -batchmode -executeMethod TerrainProbe.Dump / .Paths
public static class TerrainProbe {

    /// <summary>**組んだ 場面で「そこに 何が あるか」を 測る。**
    ///
    /// 地形の 高さだけ 見ても「歩けない」の 半分しか 分からない。
    /// 見えない かべ・木の みき・小屋の 柱など **物の あたり**で 止まる ことも 多い。
    /// 通れない ところが 出たら、まず これで **地形か 物か**を 切り分ける。
    /// 使いかた: rebuild.ps1 -Only TerrainProbe.What</summary>
    public static void What() {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            "Assets/Scenes/Zashiki.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
        var sb = new System.Text.StringBuilder();
        // 高台への 道（本道から わかれて 山へ）を よこに ずらしながら 見る
        Line(sb, "高台への道(z=-6)", -18f, -32f, -6f, true);
        Line(sb, "小川わたり(z=-6)", -19f, -26f, -6f, true);
        Line(sb, "山道の たて(x=-30)", -6f, -18f, -30f, false);
        Debug.Log("[Probe]\n" + sb);
    }

    static void Line(System.Text.StringBuilder sb, string name, float a, float b, float fixedV, bool alongX) {
        sb.AppendLine("== " + name);
        int n = Mathf.CeilToInt(Mathf.Abs(b - a)) * 2;
        for (int i = 0; i <= n; i++) {
            float v = Mathf.Lerp(a, b, i / (float)n);
            float x = alongX ? v : fixedV, z = alongX ? fixedV : v;
            float h = TerrainGen.Height(x, z);
            // 人の 高さで 立って いる ところに 何か あるか（半径 0.3m は 主人公の 太さ）
            var hits = Physics.OverlapSphere(new Vector3(x, h + 0.6f, z), 0.30f);
            string what = "";
            foreach (var c in hits) {
                if (c == null) continue;
                var t = c.transform; string p = t.name;
                for (var q = t.parent; q != null; q = q.parent) p = q.name + "/" + p;
                what += " [" + p + "]";
            }
            sb.AppendFormat("  {0,6:0.0} h={1,6:0.00}{2}\n", v, h, what);
        }
    }

    /// <summary>**歩く 道を 1mごとに 測る。** ここが でこぼこだと 歩けない</summary>
    public static void Paths() {
        var sb = new System.Text.StringBuilder();
        // 人が 歩ける 角度の 目やす。CharacterController の slopeLimit は 50度
        const float Walk = 0.36f;   // ≒20度：ここを こえたら「急」
        const float Stop = 1.19f;   // ≒50度：ここを こえたら **登れない**
        int bad = 0, steep = 0;

        for (int p = 0; p < TerrainGen.Paths.Length; p++) {
            var line = TerrainGen.Paths[p];
            float worst = 0f; Vector2 worstAt = Vector2.zero;
            sb.AppendFormat("[道{0}] ", p);
            for (int i = 0; i < line.Length - 1; i++) {
                var a = line[i]; var b = line[i + 1];
                float len = Vector2.Distance(a, b);
                int n = Mathf.Max(1, Mathf.CeilToInt(len));
                float prev = TerrainGen.Height(a.x, a.y);
                for (int k = 1; k <= n; k++) {
                    var q = Vector2.Lerp(a, b, k / (float)n);
                    float hgt = TerrainGen.Height(q.x, q.y);
                    float d = len / n;
                    float g = Mathf.Abs(hgt - prev) / Mathf.Max(d, 0.01f);
                    if (g > worst) { worst = g; worstAt = q; }
                    if (g > Stop) bad++;
                    else if (g > Walk) steep++;
                    prev = hgt;
                }
            }
            sb.AppendFormat("いちばん 急な ところ {0:F2}（{1:F0}度）at ({2:F1},{3:F1})\n",
                            worst, Mathf.Atan(worst) * Mathf.Rad2Deg, worstAt.x, worstAt.y);
        }
        sb.AppendFormat("[まとめ] 登れない ところ={0} 急な ところ={1}\n", bad, steep);

        // 道の よこ（そこから 1.5m 外）も 見る。道はばの 中で 段が あると つまずく
        sb.AppendLine("[道はばの 中の 段] 道の まん中から よこ 1.5m の 高さの ちがい");
        for (int p = 0; p < TerrainGen.Paths.Length; p++) {
            var line = TerrainGen.Paths[p];
            float worst = 0f;
            for (int i = 0; i < line.Length - 1; i++) {
                var a = line[i]; var b = line[i + 1];
                var dir = (b - a).normalized;
                var nrm = new Vector2(-dir.y, dir.x);
                int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b)));
                for (int k = 0; k <= n; k++) {
                    var q = Vector2.Lerp(a, b, k / (float)n);
                    float c = TerrainGen.Height(q.x, q.y);
                    float l = TerrainGen.Height(q.x - nrm.x * 1.5f, q.y - nrm.y * 1.5f);
                    float r = TerrainGen.Height(q.x + nrm.x * 1.5f, q.y + nrm.y * 1.5f);
                    worst = Mathf.Max(worst, Mathf.Max(Mathf.Abs(c - l), Mathf.Abs(c - r)));
                }
            }
            sb.AppendFormat("  道{0}: {1:F2}m\n", p, worst);
        }
        // ★家の まわりは 平ら で なければ ならない。
        //   家の 床は 0m。道が それより 高いと **玄関が 二階の 高さ**に なり、
        //   入った あと 上がれなく なる（本人の 指摘）
        sb.AppendLine("[家の まわり] 床=0.00m に たいする 地めんの 高さ");
        foreach (var q in new[] {
            new Vector2(0f, 7f), new Vector2(0f, 5f), new Vector2(0f, 4.2f),
            new Vector2(3.6f, 4.5f), new Vector2(-5f, 4.5f), new Vector2(6f, 4.5f),
            new Vector2(-8f, 7f), new Vector2(8f, 7f), new Vector2(0f, 10f),
        }) sb.AppendFormat("  ({0,5:F1},{1,5:F1})  h={2,6:F2}\n", q.x, q.y, TerrainGen.Height(q.x, q.y));

        // 家の まわり 14m 四方の いちばん 急な ところ
        float wm = 0f; Vector2 wat = Vector2.zero;
        for (float x = -12f; x <= 12f; x += 0.5f)
            for (float z = -6f; z <= 12f; z += 0.5f) {
                float g = Mathf.Abs(TerrainGen.Height(x + 0.5f, z) - TerrainGen.Height(x, z)) / 0.5f;
                float g2 = Mathf.Abs(TerrainGen.Height(x, z + 0.5f) - TerrainGen.Height(x, z)) / 0.5f;
                float m = Mathf.Max(g, g2);
                if (m > wm) { wm = m; wat = new Vector2(x, z); }
            }
        sb.AppendFormat("  家まわりの いちばん 急な ところ {0:F2}（{1:F0}度）at ({2:F1},{3:F1})\n",
                        wm, Mathf.Atan(wm) * Mathf.Rad2Deg, wat.x, wat.y);

        Debug.Log(sb.ToString());
    }

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
