// 山ぎわの 地めんを 組み立てる。
//
// ★調べた こと（2026-08-15）
//  - 日本の 山が **まだらに 見える**のは、戦後の 拡大造林で 植えた スギ・ヒノキの
//    人工林（針葉樹・そろって いて 暗い 青みどり）と、もとからの 広葉樹の 天然林
//    （明るく 色が ばらつく）が 入りまじって いる から。人工林は 針葉樹が 9割以上、
//    天然林は 広葉樹が 8割以上。→ **かたまりごとに 樹種を 分けて 植える**と あの 斑に なる。
//  - 山の 斜面で 木の 生えて いない ところは ほとんど 無い。岩はだと 沢すじ ぐらい。
//  - 踏み分け道は 斜面を まっすぐ 登らない。**九十九折り**で のぼり、
//    道じたいは ほぼ 一定の ゆるい 勾配に 削られて いる（人が 歩いて ならした ため）。
//
// 高さの 式は BuildZashiki からも 使う（木や 虫を 地めんに 置くため）。
// ※ 名前は TerrainGen。Unity に UnityEngine.Terrain が あるので ぶつからない ように
using System.Collections.Generic;
using UnityEngine;

public static class TerrainGen {
    public const float Size = 148f;
    public const int Cells = 148;                                  // 1マス 1m
    public static readonly Vector2 Center = new Vector2(-14f, 2f);

    public const float Flat = -0.52f;          // 家の まわりの 高さ
    const float FlatRadius = 11f;
    const float FlatBlend = 8f;

    // 山。すぐ そばに そびえる
    static readonly Vector2 MountA = new Vector2(-34f, -30f);
    const float MountAR = 58f, MountAH = 27f;
    static readonly Vector2 MountB = new Vector2(10f, -38f);
    const float MountBR = 44f, MountBH = 16f;

    // ---- 踏み分け道。**九十九折り**で 斜面を のぼる
    public static readonly Vector2[][] Paths = {
        new[] {   // 家の 前 → 沢ぞい → 九十九折りで 山へ
            new Vector2(  1.5f,  4.6f), new Vector2( -3.5f,  6.0f), new Vector2( -9.0f,  6.4f),
            new Vector2(-14.5f,  5.0f), new Vector2(-19.0f,  1.5f),
            // ここから 九十九折り
            new Vector2(-24.0f, -1.0f), new Vector2(-30.0f,  1.5f), new Vector2(-34.5f, -2.0f),
            new Vector2(-30.0f, -7.0f), new Vector2(-24.5f, -6.0f), new Vector2(-22.0f,-11.0f),
            new Vector2(-27.0f,-15.0f), new Vector2(-33.0f,-14.0f), new Vector2(-36.0f,-19.0f),
            new Vector2(-31.5f,-23.0f), new Vector2(-26.0f,-24.0f),
        },
        new[] {   // 枝道：原っぱ・畑の ほうへ（平ら）
            new Vector2(-9.0f, 6.4f), new Vector2(-8.0f, 12f), new Vector2(-4.5f, 17f),
            new Vector2( 1.0f, 20f),  new Vector2( 8.0f, 21f),
        },
        new[] {   // 枝道：家の 右てから 林・川へ
            new Vector2( 1.5f, 4.6f), new Vector2( 8f, 6f), new Vector2(14f, 9f), new Vector2(19f, 14f),
        },
    };
    const float PathHalf = 0.75f;
    const float PathFade = 1.55f;
    const float MaxGrade = 0.26f;      // 道の 勾配の うわぎり（＝約15度）

    // ---- 道の 高さの すじ道（勾配を ならした もの）
    static float[][] profiles;
    static float[][] cumLen;

    static void EnsureProfiles() {
        if (profiles != null) return;
        profiles = new float[Paths.Length][];
        cumLen = new float[Paths.Length][];
        for (int p = 0; p < Paths.Length; p++) {
            var line = Paths[p];
            var h = new float[line.Length];
            var c = new float[line.Length];
            h[0] = RawHeight(line[0].x, line[0].y);
            c[0] = 0f;
            for (int i = 1; i < line.Length; i++) {
                float seg = Vector2.Distance(line[i - 1], line[i]);
                c[i] = c[i - 1] + seg;
                float want = RawHeight(line[i].x, line[i].y);
                // **のぼりも 下りも 勾配を 抑える。** 人が 歩いて ならした 道は
                // 急に 角度が 変わらない（本人の 指摘どおり）
                float maxD = MaxGrade * seg;
                h[i] = Mathf.Clamp(want, h[i - 1] - maxD, h[i - 1] + maxD);
            }
            // 行きと 帰りで ならして、折り返しの 段差を 消す
            for (int pass = 0; pass < 3; pass++)
                for (int i = 1; i < line.Length - 1; i++)
                    h[i] = (h[i - 1] + h[i] * 2f + h[i + 1]) * 0.25f;
            profiles[p] = h; cumLen[p] = c;
        }
    }

    /// <summary>道を 考えない 素の 地形</summary>
    public static float RawHeight(float x, float z) {
        float h = Flat;
        h += Bump(x, z, MountA, MountAR, MountAH);
        h += Bump(x, z, MountB, MountBR, MountBH);
        // 尾根と 沢。式だけで 作る（毎回 同じ 地形に なる）
        h += Mathf.Sin(x * 0.071f + 1.3f) * Mathf.Cos(z * 0.089f - 0.7f) * 2.3f
           + Mathf.Sin(x * 0.213f) * Mathf.Cos(z * 0.171f) * 0.7f
           + Mathf.Sin((x + z) * 0.041f) * 1.6f;
        float d = Vector2.Distance(new Vector2(x, z), new Vector2(0f, 0.45f));
        float flatT = SmoothBand(FlatRadius, FlatRadius + FlatBlend, d);
        return Mathf.Lerp(Flat, h, flatT);
    }

    /// <summary>その 場所の 地めんの 高さ（道を 削った あと）</summary>
    public static float Height(float x, float z) {
        EnsureProfiles();
        float w, ph;
        NearestPath(x, z, out w, out ph);
        if (w <= 0f) return RawHeight(x, z);
        // まるごと 道の 高さに すると 溝に なる。8割ほど 寄せて「削った 道」に する
        return Mathf.Lerp(RawHeight(x, z), ph, w * 0.82f);
    }

    /// <summary>道らしさ 0〜1（1＝まるっきり 土）</summary>
    public static float PathWeight(float x, float z) {
        EnsureProfiles();
        float w, ph;
        NearestPath(x, z, out w, out ph);
        return w;
    }

    static void NearestPath(float x, float z, out float weight, out float height) {
        var p = new Vector2(x, z);
        float best = float.MaxValue; height = Flat;
        for (int li = 0; li < Paths.Length; li++) {
            var line = Paths[li];
            for (int i = 0; i < line.Length - 1; i++) {
                float t;
                float d = DistToSegment(p, line[i], line[i + 1], out t);
                if (d >= best) continue;
                best = d;
                height = Mathf.Lerp(profiles[li][i], profiles[li][i + 1], t);
            }
        }
        weight = 1f - SmoothBand(PathHalf, PathFade, best);
    }

    // ★**Unity の Mathf.SmoothStep は GLSL の smoothstep では ない。**
    //   Mathf.SmoothStep(a,b,t) は「a と b の あいだを t で 補間」する もので、
    //   「t を a〜b で 正規化」しない。取りちがえて 道が まるごと 消えた ことが ある
    static float SmoothBand(float edge0, float edge1, float x) {
        float t = Mathf.Clamp01((x - edge0) / Mathf.Max(edge1 - edge0, 1e-5f));
        return t * t * (3f - 2f * t);
    }

    static float Bump(float x, float z, Vector2 c, float r, float hgt) {
        float d = Vector2.Distance(new Vector2(x, z), c);
        float t = Mathf.Clamp01(1f - d / r);
        return hgt * t * t * (3f - 2f * t);
    }

    static float DistToSegment(Vector2 p, Vector2 a, Vector2 b, out float t) {
        var ab = b - a;
        t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-5f));
        return Vector2.Distance(p, a + ab * t);
    }

    /// <summary>斜面の きつさ（0＝平ら、1＝立っている）</summary>
    public static float Slope(float x, float z) {
        const float e = 1.0f;
        float dx = Height(x + e, z) - Height(x - e, z);
        float dz = Height(x, z + e) - Height(x, z - e);
        return Mathf.Clamp01(new Vector2(dx, dz).magnitude / (2f * e) / 1.2f);
    }

    // ---- 樹種の かたまり。**これが「まだら」の 正体**
    public enum Cover { Broadleaf, Conifer }

    /// <summary>そこが 人工林(針葉樹)か 天然林(広葉樹)か</summary>
    public static Cover CoverAt(float x, float z) {
        // 大きめの うねりで 25〜40m ぐらいの かたまりを 作る
        float n = Mathf.Sin(x * 0.048f + 2.1f) * Mathf.Cos(z * 0.039f - 1.1f)
                + Mathf.Sin(x * 0.021f - 0.6f) * Mathf.Cos(z * 0.027f + 0.4f) * 0.8f;
        // 人工林は 手の 届く ところ＝低い ところ・ゆるい ところに 多い
        float h = RawHeight(x, z);
        float low = Mathf.Clamp01(1f - (h - Flat) / 20f);
        return (n * 0.5f + 0.5f) + low * 0.35f > 0.72f ? Cover.Conifer : Cover.Broadleaf;
    }

    /// <summary>地めんを 作って 場面に 置く</summary>
    public static GameObject Build(Transform parent, Material mat) {
        EnsureProfiles();
        var go = new GameObject("Ground");
        go.transform.SetParent(parent, false);

        int n = Cells + 1;
        float step = Size / Cells;
        float x0 = Center.x - Size * 0.5f, z0 = Center.y - Size * 0.5f;

        var verts = new Vector3[n * n];
        var cols = new Color32[n * n];
        var tris = new int[Cells * Cells * 6];

        for (int j = 0; j < n; j++) {
            for (int i = 0; i < n; i++) {
                float x = x0 + i * step, z = z0 + j * step;
                float h = Height(x, z);
                float edge = Mathf.Min(Mathf.Min(i, n - 1 - i), Mathf.Min(j, n - 1 - j)) / 5f;
                if (edge < 1f) h -= (1f - edge) * 10f;      // 端は そとへ 落とす
                verts[j * n + i] = new Vector3(x, h, z);
                byte dirt = (byte)Mathf.RoundToInt(Mathf.Clamp01(PathWeight(x, z)) * 255f);
                cols[j * n + i] = new Color32(dirt, 0, 0, 255);
            }
        }
        int t = 0;
        for (int j = 0; j < Cells; j++) {
            for (int i = 0; i < Cells; i++) {
                int a = j * n + i, b = a + 1, c = a + n, d = c + 1;
                tris[t++] = a; tris[t++] = c; tris[t++] = b;
                tris[t++] = b; tris[t++] = c; tris[t++] = d;
            }
        }

        var mesh = new Mesh { name = "GroundMesh", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.vertices = verts;
        mesh.colors32 = cols;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = mesh;

        UnityEditor.AssetDatabase.CreateAsset(mesh, "Assets/Art/Materials/GroundMesh.asset");
        return go;
    }

    /// <summary>木を 生やす 場所ひとつぶん</summary>
    public struct Spot {
        public Vector3 pos;
        public Cover cover;
        public float slope;
        public float size;      // 0.7〜1.25。おなじ 種類でも 大小が ある
    }

    /// <summary>山に 木を 生やす。**斜面は ほとんど 木で おおわれて いる**ので こませる</summary>
    /// <param name="slopeOnly">true なら **斜面・高い ところにだけ** 生やす。
    /// 実際の 山ぎわでは 谷そこの 平らな ところは 田畑や 庭で、木が 生えるのは 斜面。
    /// これを しないと 家が 森に うもれる</param>
    public static List<Spot> Scatter(int tries, float minR, float maxR, System.Random rng,
                                     float minSep, bool avoidHouse = true, bool slopeOnly = false) {
        EnsureProfiles();
        var list = new List<Spot>();
        // 場所の 早びき（ざっくり 格子に 入れて 近さを 見る）
        var grid = new Dictionary<int, List<Vector2>>();
        float cellSize = Mathf.Max(minSep, 1f);
        System.Func<float, float, int> key = (fx, fz) =>
            (Mathf.FloorToInt(fx / cellSize) + 4096) * 8192 + (Mathf.FloorToInt(fz / cellSize) + 4096);

        for (int n = 0; n < tries; n++) {
            float a = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            float r = Mathf.Lerp(minR, maxR, Mathf.Sqrt((float)rng.NextDouble()));
            float x = Center.x + Mathf.Cos(a) * r, z = Center.y + Mathf.Sin(a) * r;

            if (PathWeight(x, z) > 0.10f) continue;                 // 道の 上には 生えない
            float sl = Slope(x, z);
            if (sl > 0.85f) continue;                               // 岩はだには 生えない
            if (slopeOnly) {
                // 平らで 低い ところ＝谷そこ。ここは あけて おく（田畑・庭に なる）
                float rise = Height(x, z) - Flat;
                if (sl < 0.16f && rise < 2.2f) continue;
            }
            if (avoidHouse) {
                var d = new Vector2(x, z) - new Vector2(0f, 0.45f);
                if (d.magnitude < 12f) continue;
            }
            // 近すぎる 木は 置かない
            bool close = false;
            for (int gx = -1; gx <= 1 && !close; gx++)
                for (int gz = -1; gz <= 1 && !close; gz++) {
                    List<Vector2> bucket;
                    if (!grid.TryGetValue(key(x + gx * cellSize, z + gz * cellSize), out bucket)) continue;
                    foreach (var q in bucket)
                        if ((q.x - x) * (q.x - x) + (q.y - z) * (q.y - z) < minSep * minSep) { close = true; break; }
                }
            if (close) continue;

            int k = key(x, z);
            if (!grid.ContainsKey(k)) grid[k] = new List<Vector2>();
            grid[k].Add(new Vector2(x, z));

            list.Add(new Spot {
                pos = new Vector3(x, Height(x, z), z),
                cover = CoverAt(x, z),
                slope = sl,
                size = 0.72f + (float)rng.NextDouble() * 0.55f,
            });
        }
        return list;
    }
}
