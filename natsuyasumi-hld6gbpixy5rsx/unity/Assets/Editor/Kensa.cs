using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// 広い 3Dマップの あたり判定を **機械で 総なめ** する 道具。
// 絵を 見て「たぶん 通れる」と 当てに いくと 外す（T1_L の 前例）。
// 使いかた: rebuild.ps1 -Only Kensa.Aruku   … 歩ける ところの 総なめ＋到達できるか
//           rebuild.ps1 -Only Kensa.Butsu   … 見た目と あたりの 棚おろし
// くわしい 結果は %TEMP%\natsuyasumi\kensa_*.txt に 書く（ログの 60行制限に 収まらない）。
//
// ★v2：**1マスに 高さを 複数 もたせた。** 上からの レイ 1本だと、家の 中は
//   2階の 床を 拾って 1階が 消え、主人公（座敷 スタート）が 家から 出られない
//   ことに なって いた（初回の 実測：行ける261マス／行けない17414マス）。
public static class Kensa {

    // 主人公の CharacterController と 同じ 寸法（BuildZashiki と そろえる）
    const float R = 0.26f, H = 1.0f;
    const float Step = 0.5f;          // 検査の マス目（m）
    // となりへ 移れる 高低差。slopeLimit50度 × 0.5m ＝ 0.60、階段は 実測 0.56
    //（0.5m マスが 段の 位相を またぐ ため 0.50 に ならない）。0.55 だと 階段が 全部 切れた
    const float StepUp = 0.62f;
    static readonly int GroundMask = ~(1 << 2);   // 層2 は「真下レイに 出ない もの」

    struct Lv { public float h; public bool free; public bool reach; }

    static void Open() {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            "Assets/Scenes/Zashiki.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
    }

    static string OutPath(string name) {
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "natsuyasumi");
        System.IO.Directory.CreateDirectory(dir);
        return System.IO.Path.Combine(dir, name);
    }

    /// <summary>遊べる 四角を 0.5mごとに 主人公の カプセルで 総なめし、
    /// 「立てるか」→「主人公の 位置から 歩いて 行けるか」を 塗りつぶしで 調べる。
    /// 1マスに つき 立てる 高さを ぜんぶ もつ（1階と 2階、縁側と 地めん、を 別あつかい）。
    /// 行けるはずなのに 行けない 飛び地（pocket）が あれば、何に ふさがれて いるかも 出す。</summary>
    // 総なめの 結果（Aruku と Suji で 共有）
    static List<Lv>[,] cells; static int nx, nz; static float x0, z0;
    static readonly int[] dx = { 1, -1, 0, 0 }, dk = { 0, 0, 1, -1 };

    static void Sweep() {
        Open();
        x0 = TerrainGen.PlayMinX; z0 = TerrainGen.PlayMinZ;
        float x1 = TerrainGen.PlayMaxX, z1 = TerrainGen.PlayMaxZ;
        nx = Mathf.CeilToInt((x1 - x0) / Step) + 1;
        nz = Mathf.CeilToInt((z1 - z0) / Step) + 1;

        cells = new List<Lv>[nx, nz];
        for (int i = 0; i < nx; i++)
            for (int k = 0; k < nz; k++) {
                float x = x0 + i * Step, z = z0 + k * Step;
                var lvs = new List<Lv>();
                // 上から ぜんぶ 拾う。近い 面（0.4m 未満）は 1つに まとめ、
                // 60度より 急な 面は「立てる 床」と 見なさない
                var hits = Physics.RaycastAll(new Vector3(x, 80f, z), Vector3.down, 300f,
                                              GroundMask, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => b.point.y.CompareTo(a.point.y));
                float last = float.MaxValue;
                foreach (var hit in hits) {
                    if (hit.normal.y < 0.5f) continue;
                    float h = hit.point.y;
                    if (last - h < 0.4f) continue;
                    last = h;
                    // ★カプセルの 下は stepOffset(0.35) より 上から。床すれすれから 立てると
                    //   階段の つぎの 段(+0.28)に 必ず 当たり、階段が ぜんぶ「ふさがり」に なる。
                    //   またげる 高さの 物は 実機でも 止まらない ので、見ないのが 正しい
                    var p0 = new Vector3(x, h + 0.42f + R * 0.9f, z);
                    var p1 = new Vector3(x, h + H - R + 0.06f, z);
                    bool blocked = Physics.CheckCapsule(p0, p1, R * 0.9f, ~0, QueryTriggerInteraction.Ignore);
                    lvs.Add(new Lv { h = h, free = !blocked, reach = false });
                }
                cells[i, k] = lvs;
            }

        // 主人公の いる ところから 塗りつぶし
        var player = GameObject.Find("Player");
        Vector3 sp = player != null ? player.transform.position : Vector3.zero;
        int si = Mathf.Clamp(Mathf.RoundToInt((sp.x - x0) / Step), 0, nx - 1);
        int sk = Mathf.Clamp(Mathf.RoundToInt((sp.z - z0) / Step), 0, nz - 1);
        var q = new Queue<Vector3Int>();       // (i, k, 何番目の 高さか)
        {
            var lvs = cells[si, sk]; int best = -1; float bd = float.MaxValue;
            for (int l = 0; l < lvs.Count; l++) {
                float d = Mathf.Abs(lvs[l].h - sp.y);
                if (lvs[l].free && d < bd) { bd = d; best = l; }
            }
            if (best >= 0) { var v = lvs[best]; v.reach = true; lvs[best] = v; q.Enqueue(new Vector3Int(si, sk, best)); }
        }
        while (q.Count > 0) {
            var c = q.Dequeue();
            float h0 = cells[c.x, c.y][c.z].h;
            for (int d = 0; d < 4; d++) {
                int i = c.x + dx[d], k = c.y + dk[d];
                if (i < 0 || k < 0 || i >= nx || k >= nz) continue;
                var lvs = cells[i, k];
                for (int l = 0; l < lvs.Count; l++) {
                    if (!lvs[l].free || lvs[l].reach) continue;
                    if (Mathf.Abs(lvs[l].h - h0) > StepUp) continue;
                    var v = lvs[l]; v.reach = true; lvs[l] = v;
                    q.Enqueue(new Vector3Int(i, k, l));
                }
            }
        }
    }

    public static void Aruku() {
        Sweep();

        // 立てるのに 行けない ところを、つながりごとに 飛び地として まとめる
        var seen = new HashSet<int>();
        var pockets = new List<(int n, float cx, float cz, float ch)>();
        System.Func<int, int, int, int> key = (i, k, l) => (i * nz + k) * 8 + l;
        for (int i = 0; i < nx; i++)
            for (int k = 0; k < nz; k++) {
                var lvs = cells[i, k];
                for (int l = 0; l < lvs.Count; l++) {
                    if (!lvs[l].free || lvs[l].reach || seen.Contains(key(i, k, l))) continue;
                    int n = 0; float sx = 0, sz = 0, sh = 0;
                    var q2 = new Queue<Vector3Int>();
                    q2.Enqueue(new Vector3Int(i, k, l)); seen.Add(key(i, k, l));
                    while (q2.Count > 0) {
                        var c = q2.Dequeue(); n++;
                        var ch = cells[c.x, c.y][c.z].h;
                        sx += x0 + c.x * Step; sz += z0 + c.y * Step; sh += ch;
                        for (int d = 0; d < 4; d++) {
                            int i2 = c.x + dx[d], k2 = c.y + dk[d];
                            if (i2 < 0 || k2 < 0 || i2 >= nx || k2 >= nz) continue;
                            var lv2 = cells[i2, k2];
                            for (int l2 = 0; l2 < lv2.Count; l2++) {
                                if (!lv2[l2].free || lv2[l2].reach || seen.Contains(key(i2, k2, l2))) continue;
                                if (Mathf.Abs(lv2[l2].h - ch) > StepUp * 2f) continue;
                                seen.Add(key(i2, k2, l2)); q2.Enqueue(new Vector3Int(i2, k2, l2));
                            }
                        }
                    }
                    pockets.Add((n, sx / n, sz / n, sh / n));
                }
            }
        pockets.Sort((a, b) => b.n.CompareTo(a.n));

        var sb = new System.Text.StringBuilder();
        int cFree = 0, cIsle = 0;
        foreach (var lvs in cells)
            foreach (var lv in lvs) { if (lv.reach) cFree++; else if (lv.free) cIsle++; }
        sb.AppendFormat("[Probe] Kensa.Aruku {0}x{1}マス(0.5m)  行ける={2}  立てるが行けない={3}  飛び地={4}件\n",
                        nx, nz, cFree, cIsle, pockets.Count);
        int show = 0;
        foreach (var pk in pockets) {
            if (show >= 12) break;
            float area = pk.n * Step * Step;
            if (area < 1.0f) break;                       // 1m² 未満は ふちの まるめ。ノイズ
            show++;
            var names = new Dictionary<string, int>();
            var hits = Physics.OverlapSphere(new Vector3(pk.cx, pk.ch + 0.6f, pk.cz), 2.5f,
                                             ~0, QueryTriggerInteraction.Ignore);
            foreach (var hcol in hits) {
                if (hcol == null) continue;
                var t = hcol.transform; string nm = t.name;
                if (t.parent != null) nm = t.parent.name + "/" + nm;
                int v; names.TryGetValue(nm, out v); names[nm] = v + 1;
            }
            string who = "";
            foreach (var kv in names) { who += " [" + kv.Key + "]"; if (who.Length > 160) break; }
            sb.AppendFormat("[Probe]   飛び地{0}: {1:F1}m²  中心({2:F1},{3:F1}) 高さ{4:F1} 近くの もの:{5}\n",
                            show, area, pk.cx, pk.cz, pk.ch, who);
        }
        if (show == 0) sb.Append("[Probe]   飛び地なし（1m²以上）\n");

        // 地図を ファイルへ。 #=ふさがり  .=行ける  x=立てるが行けない  ~=床なし
        var map = new System.Text.StringBuilder();
        map.AppendLine("上が奥(-Z) 右が+X  #=ふさがり .=行ける x=立てるが行けない ~=床なし");
        for (int k = 0; k < nz; k++) {
            for (int i = 0; i < nx; i++) {
                var lvs = cells[i, k];
                bool anyR = false, anyF = false;
                foreach (var lv in lvs) { if (lv.reach) anyR = true; else if (lv.free) anyF = true; }
                map.Append(lvs.Count == 0 ? '~' : anyR ? '.' : anyF ? 'x' : '#');
            }
            map.AppendLine("  z=" + (z0 + k * Step).ToString("F1"));
        }
        var path = OutPath("kensa_map.txt");
        System.IO.File.WriteAllText(path, sb.ToString() + map);
        sb.Append("[Probe] 地図: " + path + "\n");
        Debug.Log(sb.ToString());
    }

    /// <summary>1本の すじを 塗りつぶしの 結果ごと 見る（原因さがし用）。
    /// .=行ける  o=立てるが 行けない  x=ふさがり</summary>
    public static void Suji() {
        Sweep();
        var sb = new System.Text.StringBuilder();
        LineDump(sb, "階段 x=5.5 (z=-2→-6)", 5.5f, -2f, 5.5f, -6f, 8);
        LineDump(sb, "階段 x=5.0 (z=-2→-6)", 5.0f, -2f, 5.0f, -6f, 8);
        LineDump(sb, "てっぺんの 継ぎ目 z=-5.5 (x=3.5→6)", 3.5f, -5.5f, 6f, -5.5f, 5);
        LineDump(sb, "てっぺんの 継ぎ目 z=-5.0 (x=3.5→6)", 3.5f, -5f, 6f, -5f, 5);
        // ふさがりの 犯人を 出す（(4.5,-5.5) の 2階の へり）
        {
            float bx = 4.5f, bz = -5.5f, bh = 2.61f;
            var cols = Physics.OverlapCapsule(
                new Vector3(bx, bh + 0.42f + R * 0.9f, bz),
                new Vector3(bx, bh + H - R + 0.06f, bz), R * 0.9f, ~0, QueryTriggerInteraction.Ignore);
            foreach (var c in cols) sb.AppendLine("[Probe] はんにん(4.5,-5.5): " + c.name);
        }
        Debug.Log(sb.ToString());
    }

    static void LineDump(System.Text.StringBuilder sb, string name,
                         float xa, float za, float xb, float zb, int n) {
        sb.AppendLine("[Probe] == " + name);
        for (int i = 0; i <= n; i++) {
            float x = Mathf.Lerp(xa, xb, i / (float)n);
            float z = Mathf.Lerp(za, zb, i / (float)n);
            int gi = Mathf.Clamp(Mathf.RoundToInt((x - x0) / Step), 0, nx - 1);
            int gk = Mathf.Clamp(Mathf.RoundToInt((z - z0) / Step), 0, nz - 1);
            string line = "";
            foreach (var lv in cells[gi, gk])
                line += string.Format("  {0,5:F2} {1}", lv.h, lv.reach ? "." : lv.free ? "o" : "x");
            sb.AppendFormat("[Probe]  x={0,5:F2} z={1,5:F2} |{2}\n",
                            x0 + gi * Step, z0 + gk * Step, line);
        }
    }

    /// <summary>見た目と あたりの 棚おろし。
    /// ①コライダーの 無い 見た目（すり抜ける おそれ）を 大きい 順に
    /// ②見た目の 無い コライダー（見えない かべ。意図した もの以外は 事故）
    /// **わざと あたりを 外して いる もの**（屋根・化粧板・うね 等）は 名まえで 除く。</summary>
    public static void Butsu() {
        Open();
        var sb = new System.Text.StringBuilder();

        // わざと あたりの 無い 見た目（このリポジトリの 決めごと）：
        //   屋根一族（レイで 地めんと 誤認させない ため 層2/あたり無し）＝ Yane/Roof/Mune/Tsuma/Taruki/Hisashi/Geya
        //   化粧板（壁より 12cm 出るので あたりを つけると 家が ふくらむ）＝ Koshi/Hashira/Waku/Nuki/Mizukiri
        //   地めんの 起伏あつかい ＝ Une
        string[] okNoHit = { "Yane", "Roof", "Mune", "Tsuma", "Taruki", "Hisashi", "Geya",
                             "Koshi", "Hashira", "Waku", "Nuki", "Mizukiri", "Une", "Shell" };
        var noHit = new List<(float size, string path)>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) {
            if (r is ParticleSystemRenderer) continue;
            var b = r.bounds;
            float big = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (big < 0.6f) continue;                       // 小物は 通れて よい
            if (b.size.y < 0.35f) continue;                 // ひくい もの（地めんの 絵など）は またげる
            bool ok = false;
            foreach (var w in okNoHit) if (r.name.Contains(w)) { ok = true; break; }
            if (ok) continue;
            bool has = r.GetComponentInParent<Collider>() != null
                    || r.GetComponentInChildren<Collider>() != null;
            if (has) continue;
            var t = r.transform; string p = t.name;
            for (var pt = t.parent; pt != null; pt = pt.parent) p = pt.name + "/" + p;
            noHit.Add((big, p));
        }
        noHit.Sort((a, b2) => b2.size.CompareTo(a.size));
        sb.AppendFormat("[Probe] Kensa.Butsu すり抜ける見た目(0.6m以上・わざとを除く)={0}\n", noHit.Count);
        for (int i = 0; i < Mathf.Min(40, noHit.Count); i++)
            sb.AppendFormat("[Probe]   {0,5:F1}m  {1}\n", noHit[i].size, noHit[i].path);

        // ② コライダーは あるのに 見た目が ない（見えない かべ）。
        //    設計した かべ・手置きの あたり箱は 名まえで 除く
        string[] okGhost = { "Bound_", "T1_", "T2_", "T3_", "Look_", "PlayArea", "Kit_Hit", "Hit" };
        var ghost = new List<string>();
        foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)) {
            if (c.isTrigger) continue;
            bool drawn = c.GetComponentInParent<Renderer>() != null
                      || c.GetComponentInChildren<Renderer>() != null;
            if (drawn) continue;
            bool ok = false;
            foreach (var w in okGhost) if (c.name.StartsWith(w)) { ok = true; break; }
            if (ok) continue;
            var t = c.transform; string p = t.name;
            for (var pt = t.parent; pt != null; pt = pt.parent) p = pt.name + "/" + p;
            ghost.Add(p + "  at(" + c.bounds.center.x.ToString("F1") + "," + c.bounds.center.z.ToString("F1") + ")");
        }
        sb.AppendFormat("[Probe] 見た目のない あたり（設計した かべを 除く）={0}\n", ghost.Count);
        for (int i = 0; i < Mathf.Min(40, ghost.Count); i++)
            sb.AppendFormat("[Probe]   {0}\n", ghost[i]);

        // ③ convex でない MeshCollider（重い・動く物と 衝突しない）。名まえも 出す
        var mc = new Dictionary<string, int>();
        int meshCol = 0;
        foreach (var c in Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))
            if (!c.convex) {
                meshCol++;
                int v; mc.TryGetValue(c.name, out v); mc[c.name] = v + 1;
            }
        sb.AppendFormat("[Probe] convexでない MeshCollider={0}\n", meshCol);
        int shown = 0;
        foreach (var kv in mc) {
            if (shown++ >= 15) break;
            sb.AppendFormat("[Probe]   x{0,-4} {1}\n", kv.Value, kv.Key);
        }

        var path2 = OutPath("kensa_butsu.txt");
        System.IO.File.WriteAllText(path2, sb.ToString());
        sb.Append("[Probe] くわしくは: " + path2 + "\n");
        Debug.Log(sb.ToString());
    }
}
