// 山ぎわの 地めんを 組み立てる。
//
// ねらい：**夏休みの 田舎＝山が すぐ そばに あって、山の 中を 駆けまわる。**
// 平らな 板を やめて、
//  - 家の まわりだけ 平ら（建てられる ように）
//  - そこから 山へ 向かって 上がって いく 斜面
//  - **人が 通る ところは 草が はげて 土が 出る**（踏み分け道）
//  - 道を はずれると けもの道＝草の まま、木が こんで いる
// を 1枚の 起伏の ある 面で 作る。
//
// 高さの 式は BuildZashiki からも 使う（木や 虫を 地めんに 置くため）。
// ※ 名前は TerrainGen。Unity に UnityEngine.Terrain が あるので ぶつからない ように
using System.Collections.Generic;
using UnityEngine;

public static class TerrainGen {
    // 広さと こまかさ。1マス 1m
    public const float Size = 116f;
    public const int Cells = 116;
    public static readonly Vector2 Center = new Vector2(-9.2f, 0.9f);   // 庭の あたりを まん中に

    public const float Flat = -0.52f;          // 家の まわりの 高さ（＝もとの 地めん）
    const float FlatRadius = 10f;              // ここまでは 平ら
    const float FlatBlend = 7f;                // ここまでで 斜面に なじませる

    // 山。まん中から 見て 奥・左に そびえる
    // ※ はじめ 遠くに 置きすぎて、家の あたりでは 1m ほどしか 上がらず
    //   ただの 平地に 見えた。**すぐ そばに そびえる**ように 寄せた
    static readonly Vector2 MountA = new Vector2(-30f, -28f);
    const float MountAR = 54f, MountAH = 24f;
    static readonly Vector2 MountB = new Vector2(6f, -34f);
    const float MountBR = 40f, MountBH = 14f;

    // ---- 踏み分け道。家の 前から 山へ 上がって いく 1本と、原っぱへ 抜ける 枝
    public static readonly Vector2[][] Paths = {
        new[] {   // 家 → 庭 → 山道
            new Vector2( 1.2f,  4.6f), new Vector2(-3.0f,  6.2f), new Vector2(-8.5f,  6.8f),
            new Vector2(-14f,   4.5f), new Vector2(-19f,   0.5f), new Vector2(-23f,  -6f),
            new Vector2(-27f, -13f),   new Vector2(-30f, -21f),   new Vector2(-33f, -30f),
        },
        new[] {   // 枝道：原っぱの ほうへ
            new Vector2(-8.5f, 6.8f), new Vector2(-7.5f, 12f), new Vector2(-4f, 17f), new Vector2(1f, 20f),
        },
        new[] {   // 枝道：家の 右てから 林へ
            new Vector2( 1.2f, 4.6f), new Vector2( 7f, 5.5f), new Vector2(12f, 8f), new Vector2(16f, 13f),
        },
    };
    // ※ はじめ 5m幅に して いたら、踏み分け道では なく 川に 見えた。
    //   人が 1人 とおる ぶん＝1.4m ほど＋ふちの ぼけ で 3m 弱に する
    const float PathHalf = 0.7f;      // まん中から この 幅までは まるっきり 土
    const float PathFade = 1.45f;     // ここまでで 草に もどる

    /// <summary>その 場所の 地めんの 高さ</summary>
    public static float Height(float x, float z) {
        float h = Flat;

        // 山ふたつ。なだらかに 立ちあがる
        h += Bump(x, z, MountA, MountAR, MountAH);
        h += Bump(x, z, MountB, MountBR, MountBH);

        // うねり。式だけで 作る（毎回 同じ 地形に なる）
        float n = Mathf.Sin(x * 0.071f + 1.3f) * Mathf.Cos(z * 0.089f - 0.7f) * 2.1f
                + Mathf.Sin(x * 0.213f) * Mathf.Cos(z * 0.171f) * 0.62f
                + Mathf.Sin((x + z) * 0.041f) * 1.4f;

        // 道の ちかくは うねりを 抑える（歩きやすく、道らしく 見える）
        float p = PathWeight(x, z);
        n *= 1f - p * 0.85f;

        // 家の まわりは 平ら
        float d = Vector2.Distance(new Vector2(x, z), new Vector2(0f, 0.45f));
        float flatT = SmoothBand(FlatRadius, FlatRadius + FlatBlend, d);
        return Mathf.Lerp(Flat, h + n, flatT);
    }

    // ★**Unity の Mathf.SmoothStep は GLSL の smoothstep では ない。**
    //   Mathf.SmoothStep(a,b,t) は「a と b の あいだを t で 補間」する もので、
    //   「t を a〜b で 正規化」しない。取りちがえて 道の 重みが つねに 負に なり、
    //   道が まるごと 消えて いた
    static float SmoothBand(float edge0, float edge1, float x) {
        float t = Mathf.Clamp01((x - edge0) / Mathf.Max(edge1 - edge0, 1e-5f));
        return t * t * (3f - 2f * t);
    }

    static float Bump(float x, float z, Vector2 c, float r, float hgt) {
        float d = Vector2.Distance(new Vector2(x, z), c);
        float t = Mathf.Clamp01(1f - d / r);
        return hgt * t * t * (3f - 2f * t);
    }

    /// <summary>道らしさ 0〜1（1＝まるっきり 土）</summary>
    public static float PathWeight(float x, float z) {
        float best = float.MaxValue;
        var p = new Vector2(x, z);
        foreach (var line in Paths)
            for (int i = 0; i < line.Length - 1; i++)
                best = Mathf.Min(best, DistToSegment(p, line[i], line[i + 1]));
        return 1f - SmoothBand(PathHalf, PathFade, best);
    }

    static float DistToSegment(Vector2 p, Vector2 a, Vector2 b) {
        var ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-5f));
        return Vector2.Distance(p, a + ab * t);
    }

    /// <summary>斜面の きつさ（0＝平ら、1＝立っている）</summary>
    public static float Slope(float x, float z) {
        const float e = 1.0f;
        float dx = Height(x + e, z) - Height(x - e, z);
        float dz = Height(x, z + e) - Height(x, z - e);
        return Mathf.Clamp01(new Vector2(dx, dz).magnitude / (2f * e) / 1.2f);
    }

    /// <summary>地めんを 作って 場面に 置く</summary>
    public static GameObject Build(Transform parent, Material mat) {
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
                // 端は そとへ 下げて おく。地めんの 切れめが 崖に 見えない ように
                float edge = Mathf.Min(Mathf.Min(i, n - 1 - i), Mathf.Min(j, n - 1 - j)) / 6f;
                if (edge < 1f) h -= (1f - edge) * 6f;
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

    /// <summary>木を 置く 場所を えらぶ。道の 上と 家の まわりは 避ける</summary>
    public static List<Vector3> ScatterTrees(int count, float minR, float maxR, System.Random rng) {
        var list = new List<Vector3>();
        int guard = 0;
        while (list.Count < count && guard++ < count * 60) {
            float a = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            float r = Mathf.Lerp(minR, maxR, Mathf.Sqrt((float)rng.NextDouble()));
            float x = Center.x + Mathf.Cos(a) * r, z = Center.y + Mathf.Sin(a) * r;

            if (PathWeight(x, z) > 0.12f) continue;                 // 道の 上には 生えない
            if (Slope(x, z) > 0.72f) continue;                      // 崖には 生えない
            var d = new Vector2(x, z) - new Vector2(0f, 0.45f);
            if (d.magnitude < 11f) continue;                        // 家の まわりは あける

            // 家から 遠いほど こませる（近くは あかるい 原っぱ、奥は 森）
            float far = Mathf.InverseLerp(minR, maxR, r);
            if (rng.NextDouble() > 0.35f + far * 0.65f) continue;

            // 近すぎる 木は 置かない
            float sep = Mathf.Lerp(3.4f, 2.1f, far);
            bool tooClose = false;
            foreach (var p in list) {
                if ((p.x - x) * (p.x - x) + (p.z - z) * (p.z - z) < sep * sep) { tooClose = true; break; }
            }
            if (tooClose) continue;

            list.Add(new Vector3(x, Height(x, z), z));
        }
        return list;
    }
}
