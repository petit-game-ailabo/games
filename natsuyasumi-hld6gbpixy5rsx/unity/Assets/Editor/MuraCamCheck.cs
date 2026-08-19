using UnityEngine;
using UnityEditor;

// S1-1（D-107）：カメラの 担当範囲を 機械検証する。
// **担当範囲の どの 点に 立っても、その 台から 枠内（Edge<0.9）で 遮蔽なし** が 合格。
// これが 通って いれば「切替先の カメラの そとに キャラが いる」は 起きない。
//   rebuild.ps1 -Only MuraCamCheck.Check
public static class MuraCamCheck {

    /// <summary>担当範囲から カメラの 置き場所を **逆算**する（違反3300を 手で 直すのは 無理筋）。
    /// 向きは いまの 意図を 保ち、範囲が 画角に 収まる 距離と 高さを 計算。
    /// 遮蔽が 出る 向きは 30度きざみで 回して 遮蔽の いちばん 少ない 向きを えらぶ。
    /// 出力は BuildMura.cs に そのまま 貼れる S(...) 行。
    ///   rebuild.ps1 -Only MuraCamCheck.Fit</summary>
    public static void Fit() {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            "Assets/Scenes/Mura.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
        var fix = Object.FindFirstObjectByType<MuraCamFixed>();
        if (fix == null) { Debug.LogError("[Probe] MuraCamFixed が 見つからない"); return; }
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[Probe] Fit の 提案（BuildMura の spots に 貼る）：");

        foreach (var s in fix.spots) {
            if (s == null) continue;
            var a = s.area;
            float cx = a.center.x, cz = a.center.z;
            float tanV = Mathf.Tan(s.fov * 0.5f * Mathf.Deg2Rad);
            float tanH = tanV * 1.78f;
            float halfDiag = Mathf.Sqrt(a.extents.x * a.extents.x + a.extents.z * a.extents.z);
            float D = Mathf.Max(7f, halfDiag / (0.72f * tanH));   // 範囲が 枠の 72% に 収まる 距離
            const float pitch = 26f * Mathf.Deg2Rad;

            // もとの 向き（方位）を 出発点に、遮蔽の 少ない 方位を さがす
            var az0 = s.pos - new Vector3(cx, s.pos.y, cz); az0.y = 0f;
            float baseDeg = az0.sqrMagnitude > 0.1f ? Mathf.Atan2(az0.x, az0.z) * Mathf.Rad2Deg : 180f;
            float bestDeg = baseDeg; int bestBad = int.MaxValue; Vector3 bestPos = s.pos;
            for (int i = 0; i < 12; i++) {
                // 0, +30, -30, +60, -60 … の 順（もとの 向きを ひいきに）
                float deg = baseDeg + (i % 2 == 0 ? 1 : -1) * ((i + 1) / 2) * 30f;
                var az = new Vector3(Mathf.Sin(deg * Mathf.Deg2Rad), 0f, Mathf.Cos(deg * Mathf.Deg2Rad));
                var pos = new Vector3(cx, 0f, cz) + az * (D * Mathf.Cos(pitch));
                pos.y = 0.8f + D * Mathf.Sin(pitch);
                // 地めんに めり込むなら 持ち上げる
                RaycastHit gh;
                if (Physics.Raycast(new Vector3(pos.x, 80f, pos.z), Vector3.down, out gh, 300f,
                                    ~(1 << 2), QueryTriggerInteraction.Ignore) && pos.y < gh.point.y + 1.6f)
                    pos.y = gh.point.y + 1.6f;
                // 範囲の 5点（中心と 四すみ）で 遮蔽を 数える
                int bad = 0;
                var probe = new MuraCamFixed.Spot { name = s.name, pos = pos,
                    lookAt = new Vector3(cx, 0.8f, cz), fov = s.fov };
                foreach (var q in new[] {
                    new Vector2(cx, cz),
                    new Vector2(a.min.x + 1f, a.min.z + 1f), new Vector2(a.max.x - 1f, a.min.z + 1f),
                    new Vector2(a.min.x + 1f, a.max.z - 1f), new Vector2(a.max.x - 1f, a.max.z - 1f) }) {
                    RaycastHit hit;
                    if (!Physics.Raycast(new Vector3(q.x, 80f, q.y), Vector3.down, out hit, 300f,
                                         ~(1 << 2), QueryTriggerInteraction.Ignore)) continue;
                    var eye = hit.point + Vector3.up * 0.6f;
                    if (MuraCamFixed.BlockedFrom(probe, eye, null)) bad++;
                    if (MuraCamFixed.EdgeFrom(probe, eye, 1.78f) > 0.9f) bad++;
                }
                if (bad < bestBad) { bestBad = bad; bestDeg = deg; bestPos = pos; }
                if (bad == 0) break;
            }
            sb.AppendFormat(
                "            S(\"{0}\", {1:F0}f, {2:F0}f, {3:F0}f, {4:F0}f,   {5:F1}f, {6:F1}f, {7:F1}f,   {8:F0}f, 0.8f, {9:F0}f, {10:F0}f),  // 残る問題点={11}\n",
                s.name, cx, cz, a.size.x, a.size.z,
                bestPos.x, bestPos.y, bestPos.z, cx, cz, s.fov, bestBad);
        }
        Debug.Log(sb.ToString());
    }

    public static void Check() {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            "Assets/Scenes/Mura.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
        var fix = Object.FindFirstObjectByType<MuraCamFixed>();
        if (fix == null) { Debug.LogError("[Probe] MuraCamFixed が 見つからない"); return; }
        var player = GameObject.Find("Player");
        var sb = new System.Text.StringBuilder();
        int totalBad = 0;

        foreach (var s in fix.spots) {
            if (s == null) continue;
            int n = 0, badEdge = 0, badBlock = 0;
            float worstE = 0f; Vector3 worstEp = Vector3.zero, worstBp = Vector3.zero;
            var a = s.area;
            for (float x = a.min.x + 0.5f; x <= a.max.x; x += 1f)
                for (float z = a.min.z + 0.5f; z <= a.max.z; z += 1f) {
                    RaycastHit hit;
                    if (!Physics.Raycast(new Vector3(x, 80f, z), Vector3.down, out hit, 300f,
                                         ~(1 << 2), QueryTriggerInteraction.Ignore)) continue;
                    // 立てない ところ（かべの 中・水の そこ）は 範囲の 検証から 外す
                    var eye = hit.point + Vector3.up * 0.6f;
                    if (Physics.CheckSphere(eye, 0.22f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    n++;
                    float e = MuraCamFixed.EdgeFrom(s, eye, 1.78f);
                    if (e > 0.9f) { badEdge++; if (e > worstE) { worstE = e; worstEp = eye; } }
                    else if (MuraCamFixed.BlockedFrom(s, eye,
                             player != null ? player.transform : null)) { badBlock++; worstBp = eye; }
                }
            totalBad += badEdge + badBlock;
            sb.AppendFormat("[Probe] {0,-8} 点={1,4}  枠のそと={2,3}  遮蔽={3,3}{4}{5}\n",
                s.name, n, badEdge, badBlock,
                badEdge > 0 ? "  最悪E=" + worstE.ToString("F2") + " at(" + worstEp.x.ToString("F0") + "," + worstEp.z.ToString("F0") + ")" : "",
                badBlock > 0 ? "  遮蔽at(" + worstBp.x.ToString("F0") + "," + worstBp.z.ToString("F0") + ")" : "");
        }

        // 参考：遊べる 四角の うち、どの 範囲にも 入って いない 点（みちくさ帯）の 割合
        int free = 0, orphan = 0;
        for (float x = TerrainGen.PlayMinX; x <= TerrainGen.PlayMaxX; x += 2f)
            for (float z = TerrainGen.PlayMinZ; z <= TerrainGen.PlayMaxZ; z += 2f) {
                RaycastHit hit;
                if (!Physics.Raycast(new Vector3(x, 80f, z), Vector3.down, out hit, 300f,
                                     ~(1 << 2), QueryTriggerInteraction.Ignore)) continue;
                var eye = hit.point + Vector3.up * 0.6f;
                if (Physics.CheckSphere(eye, 0.22f, ~0, QueryTriggerInteraction.Ignore)) continue;
                free++;
                bool inAny = false;
                foreach (var s in fix.spots) { if (s != null && s.area.Contains(hit.point + Vector3.up * 0.5f)) { inAny = true; break; } }
                if (!inAny) orphan++;
            }
        sb.AppendFormat("[Probe] まとめ 違反={0}（0が合格）  未割当の帯={1}/{2}点（参考）\n",
                        totalBad, orphan, free);
        Debug.Log(sb.ToString());
    }
}
