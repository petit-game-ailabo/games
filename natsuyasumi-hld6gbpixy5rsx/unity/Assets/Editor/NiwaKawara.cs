using System.Collections.Generic;
using UnityEngine;

// 瓦を **1まいずつ 置く**（2026-09-01）。
//
// ★本人「後は瓦がまだイマイチ。3Dでやるなら、瓦一枚ずつ配置してみるしかないんじゃない？」
//   → その とおり。法線マップは **面の かたむきを だます だけ**なので、
//     軒先の 輪郭は まっすぐな 線の まま。本ものの 瓦屋根は 軒先が 波うって いて、
//     それが 屋根らしさの 大半を つくる。輪郭は 形でしか 出せない。
//
// ★寸法は 桟瓦の 働き寸法（実物）：**よこ 305mm x たて 235mm**。
//   本屋の 南北で 35列 x 15段 x 2面 ＝ 1050まい、下屋を 入れて 約1400まい。
//   1まい 15頂点なので 2万頂点ほど。メッシュを 結合すれば 描画は 1回で すむ
//   （木の 葉カードが 1本 150まい x 80本 なのを 思えば 軽い）。
//
// ★屋根の 面は HouseRoof の 式を そのまま つかう（HalfX/HalfZ/Y が public）。
//   t=-1 が 軒先、t=1 が 棟。段は **弧長**で きざむ（t で きざむと 段の 間かくが ずれる）
public static class NiwaKawara {
    public const float HABA = 0.305f;   // 桟瓦の よこの 働き
    public const float NOBE = 0.235f;   // 　　　　たての 働き

    /// <summary>瓦 1まい。よこ断面は 山（左のはし）と 谷。
    /// 手前（軒がわ）が すこし 下がって 前の 段に かぶさる＝段ごとの 影が 出る</summary>
    static Mesh Ichimai(bool nokigawara) {
        // よこ 5点の 断面（山→谷）。y は 面からの 浮き
        float[] px = { -0.5f, -0.34f, -0.10f, 0.18f, 0.5f };
        float[] py = { 0.012f, 0.055f, 0.020f, 0.004f, 0.010f };
        // たて 3段：おく（次の 段に かくれる）→まん中→手前（軒がわ・すこし 出る）
        float[] pz = { 0.62f, -0.10f, -0.72f };
        float[] pl = { -0.010f, 0f, 0.004f };      // 段ごとの 浮きの 足し
        int nx = px.Length, nz = pz.Length;
        var v = new List<Vector3>();
        var uv = new List<Vector2>();
        for (int j = 0; j < nz; j++)
            for (int i = 0; i < nx; i++) {
                v.Add(new Vector3(px[i] * HABA, py[i] + pl[j], pz[j] * NOBE));
                uv.Add(new Vector2(px[i] + 0.5f, pz[j] * 0.5f + 0.5f));
            }
        var tri = new List<int>();
        for (int j = 0; j < nz - 1; j++)
            for (int i = 0; i < nx - 1; i++) {
                int a = j * nx + i, b = a + 1, c = a + nx, d = c + 1;
                tri.Add(a); tri.Add(c); tri.Add(d);
                tri.Add(a); tri.Add(d); tri.Add(b);
            }
        if (nokigawara) {
            // 軒瓦の 垂れ。ここが **軒先の 輪郭を 波うたせる**＝屋根らしさの もと
            int b0 = v.Count;
            for (int i = 0; i < nx; i++) {
                v.Add(new Vector3(px[i] * HABA, py[i] + pl[nz - 1], pz[nz - 1] * NOBE));
                uv.Add(new Vector2(px[i] + 0.5f, 0.02f));
            }
            for (int i = 0; i < nx; i++) {
                v.Add(new Vector3(px[i] * HABA, py[i] - 0.055f, pz[nz - 1] * NOBE - 0.03f));
                uv.Add(new Vector2(px[i] + 0.5f, 0f));
            }
            for (int i = 0; i < nx - 1; i++) {
                int a = b0 + i, b = a + 1, c = a + nx, d = c + 1;
                tri.Add(a); tri.Add(c); tri.Add(d);
                tri.Add(a); tri.Add(d); tri.Add(b);
            }
        }
        var m = new Mesh { name = nokigawara ? "Nokigawara" : "Kawara" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
        m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
        return m;
    }

    /// <summary>屋根の 面を なぞって 瓦を ならべる。sz=-1 が 南の 面、+1 が 北の 面</summary>
    static void Men(List<CombineInstance> cis, HouseRoof.Opt o, int sz,
                    Mesh hira, Mesh noki) {
        // t を こまかく サンプルして **弧長**で 段を きる
        const int N = 600;
        var ts = new float[N + 1];
        var p = new Vector2[N + 1];               // (z, y)
        for (int i = 0; i <= N; i++) {
            float t = Mathf.Lerp(-1f, 0.985f, i / (float)N);
            ts[i] = t;
            p[i] = new Vector2(sz * HouseRoof.HalfZ(o, t), HouseRoof.Y(o, t));
        }
        float acc = 0f, next = NOBE * 0.5f;
        bool first = true;
        for (int i = 1; i <= N; i++) {
            acc += Vector2.Distance(p[i], p[i - 1]);
            if (acc < next) continue;
            next += NOBE;
            float t = ts[i];
            float hx = HouseRoof.HalfX(o, t) - HABA * 0.55f;
            if (hx <= HABA) continue;                       // 棟の ちかくは 隅棟に ゆずる
            // 面の 向き：のぼりの むきと 面の 法線
            var a3 = new Vector3(0f, p[i - 1].y, p[i - 1].x);
            var b3 = new Vector3(0f, p[i].y, p[i].x);
            var nobori = (b3 - a3).normalized;
            var hosen = Vector3.Cross(nobori, Vector3.right).normalized;
            if (hosen.y < 0f) hosen = -hosen;
            var rot = Quaternion.LookRotation(nobori, hosen);
            int kazu = Mathf.FloorToInt(hx * 2f / HABA);
            float x0 = -kazu * HABA * 0.5f + HABA * 0.5f;
            for (int k = 0; k < kazu; k++) {
                var pos = new Vector3(x0 + k * HABA, p[i].y, p[i].x);
                cis.Add(new CombineInstance {
                    mesh = first ? noki : hira,
                    transform = Matrix4x4.TRS(pos, rot, Vector3.one),
                });
            }
            first = false;
        }
    }

    /// <summary>下屋（一方ながれの 平らな 面）に ふく。HouseRoof.Shed と 同じ 引数で 呼ぶ</summary>
    public static int Geya(Transform parent, string name, float x0, float x1,
                           float zIn, float zOut, float yIn, float yOut, Material mat) {
        var hira = Ichimai(false);
        var noki = Ichimai(true);
        var cis = new List<CombineInstance>();
        var a = new Vector3(0f, yIn, zIn);
        var b = new Vector3(0f, yOut, zOut);
        float len = Vector3.Distance(a, b);
        var nobori = (a - b).normalized;                 // 軒（外）から 棟（内）へ のぼる
        var hosen = Vector3.Cross(nobori, Vector3.right).normalized;
        if (hosen.y < 0f) hosen = -hosen;
        var rot = Quaternion.LookRotation(nobori, hosen);
        int dan = Mathf.Max(1, Mathf.FloorToInt(len / NOBE));
        int kazu = Mathf.Max(1, Mathf.FloorToInt((x1 - x0) / HABA));
        float xs = (x0 + x1) * 0.5f - kazu * HABA * 0.5f + HABA * 0.5f;
        for (int j = 0; j < dan; j++) {
            float u = (j * NOBE + NOBE * 0.5f) / len;     // 軒がわ から
            var pos = Vector3.Lerp(b, a, u);
            for (int k = 0; k < kazu; k++)
                cis.Add(new CombineInstance {
                    mesh = j == 0 ? noki : hira,
                    transform = Matrix4x4.TRS(new Vector3(xs + k * HABA, pos.y, pos.z),
                                              rot, Vector3.one),
                });
        }
        var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.CombineMeshes(cis.ToArray(), true, true);
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        Object.DestroyImmediate(hira); Object.DestroyImmediate(noki);
        return cis.Count;
    }

    /// <summary>屋根に 瓦を ふく。parent は HouseRoof.Build に わたした のと 同じ 入れもの</summary>
    public static int Fuku(Transform parent, HouseRoof.Opt o, Material mat, string name) {
        var hira = Ichimai(false);
        var noki = Ichimai(true);
        var cis = new List<CombineInstance>();
        Men(cis, o, -1, hira, noki);
        Men(cis, o, +1, hira, noki);
        if (cis.Count == 0) return 0;
        var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.CombineMeshes(cis.ToArray(), true, true);
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        Object.DestroyImmediate(hira); Object.DestroyImmediate(noki);
        return cis.Count;
    }
}
