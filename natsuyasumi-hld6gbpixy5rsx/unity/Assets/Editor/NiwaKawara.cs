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

    /// <summary>**軒先の 一列だけ** ならべる（2026-09-01・2版目）。
    /// ★本人「瓦は、1300枚にしてる？全然立体感がない。…手前側の雨樋あたりが、波打ってるのが
    ///   瓦の立体感出してるかも。この波打ちを一階の瓦の手前にも適用。逆に瓦を一枚一枚
    ///   設置する意味ないから、ここは前のに戻そう」
    ///   1300まい ならべても 面の 上では 25m先で つぶれて 見えなかった。効いて いたのは
    ///   **軒先の 波うつ 輪郭**だけ。だから そこだけ 形で 作り、面は 絵に もどす。
    ///   （労力と 効果が 合わない ところを 落とす。左右の はしの 抜けも これで 消える）</summary>
    static void Nokinami(List<CombineInstance> cis, Mesh noki,
                         Vector3 hidari, Vector3 migi, Quaternion rot) {
        float haba = Vector3.Distance(hidari, migi);
        int kazu = Mathf.Max(1, Mathf.RoundToInt(haba / HABA));
        for (int k = 0; k < kazu; k++) {
            float u = (k + 0.5f) / kazu;
            cis.Add(new CombineInstance {
                mesh = noki,
                transform = Matrix4x4.TRS(Vector3.Lerp(hidari, migi, u), rot, Vector3.one),
            });
        }
    }

    /// <summary>下屋（一方ながれの 平らな 面）に ふく。HouseRoof.Shed と 同じ 引数で 呼ぶ</summary>
    public static int Geya(Transform parent, string name, float x0, float x1,
                           float zIn, float zOut, float yIn, float yOut, Material mat) {
        var noki = Ichimai(true);
        var cis = new List<CombineInstance>();
        var a = new Vector3(0f, yIn, zIn);
        var b = new Vector3(0f, yOut, zOut);
        var nobori = (a - b).normalized;                 // 軒（外）から 棟（内）へ のぼる
        var hosen = Vector3.Cross(nobori, Vector3.right).normalized;
        if (hosen.y < 0f) hosen = -hosen;
        var rot = Quaternion.LookRotation(nobori, hosen);
        // 軒先に すこし かかる 位置へ
        var moto = b + nobori * (NOBE * 0.45f);
        Nokinami(cis, noki, new Vector3(x0, moto.y, moto.z), new Vector3(x1, moto.y, moto.z), rot);
        int n = cis.Count;
        var mesh = new Mesh();
        mesh.CombineMeshes(cis.ToArray(), true, true);
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        Object.DestroyImmediate(noki);
        return n;
    }

    /// <summary>屋根の **軒先だけ** ならべる。parent は HouseRoof.Build と 同じ 入れもの</summary>
    public static int Fuku(Transform parent, HouseRoof.Opt o, Material mat, string name) {
        var noki = Ichimai(true);
        var cis = new List<CombineInstance>();
        float t0 = -1f, t1 = -0.90f;
        foreach (int sz in new[] { -1, 1 }) {
            var a = new Vector3(0f, HouseRoof.Y(o, t0), sz * HouseRoof.HalfZ(o, t0));
            var b = new Vector3(0f, HouseRoof.Y(o, t1), sz * HouseRoof.HalfZ(o, t1));
            var nobori = (b - a).normalized;
            var hosen = Vector3.Cross(nobori, Vector3.right).normalized;
            if (hosen.y < 0f) hosen = -hosen;
            var rot = Quaternion.LookRotation(nobori, hosen);
            float hx = HouseRoof.HalfX(o, t0);
            var moto = a + nobori * (NOBE * 0.45f);
            Nokinami(cis, noki, new Vector3(-hx, moto.y, moto.z),
                     new Vector3(hx, moto.y, moto.z), rot);
        }
        int n = cis.Count;
        var mesh = new Mesh();
        mesh.CombineMeshes(cis.ToArray(), true, true);
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        Object.DestroyImmediate(noki);
        return n;
    }
}
