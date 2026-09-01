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

    // ★★2026-09-02・いちばん 大事な おぼえがき★★
    //   面の 巻きが **逆**だった ので、軒瓦は 背面カリングされて **画面に 0ピクセル**。
    //   その 状態で 垂れの ふかさ・UVの ものさし・法線の 向きを 6かい いじって いた。
    //   目印の いろ(マゼンタ)を つけて 撮り、0ピクセル → 両面に して 27631ピクセル で 確定。
    //   **見えない ときは まず 「描かれて いるか」を 目印の いろで たしかめる**。
    //   形の 中みを 疑うのは その あと。
    /// <summary>瓦 1まい。よこ断面は 山（左のはし）と 谷。
    /// 手前（軒がわ）が すこし 下がって 前の 段に かぶさる＝段ごとの 影が 出る</summary>
    /// <summary>HouseRoof と おなじ ものさしで UV を つくる。
    /// ★HouseRoof の きまりは **UV ＝ メートル ÷ texM**（texM=2.8 なら 絵 1まいが 2.8m）。
    ///   材質がわの タイリングは 1。ここを 「材質が 2.8ばい」と 読みちがえて わり算を 逆に かけ、
    ///   瓦 1まい(0.305m)に 0.357UV＝**3.3ばい こまかい 絵**を のせて まだらに した
    ///   （2026-09-02 の しくじり。0.305/2.8＝0.109UV が 正しい）</summary>
    static Vector2 U(float xm, float zm, float texM) =>
        new Vector2(0.30f + xm / texM, 0.30f + zm / texM);

    /// <param name="uvWari">屋根板と おなじ texM（絵 1まいが 何m か）。ここを あわせないと
    /// 瓦だけ 絵の きめが ちがって 浮く</param>
    static Mesh Ichimai(bool nokigawara, float uvWari) {
        // よこ 5点の 断面（山→谷）。y は 面からの 浮き
        // 断面は 9点。5点だと 軒先の 歯が **かくかくの のこぎり**に 見えた
        float[] px = { -0.50f, -0.42f, -0.34f, -0.26f, -0.14f, 0.00f, 0.16f, 0.32f, 0.50f };
        float[] py = { 0.012f, 0.038f, 0.055f, 0.044f, 0.022f, 0.010f, 0.004f, 0.004f, 0.010f };
        // ★軒先の 波は **日の あたる 屋根の 面の 上**で 出す（2026-09-02・3度目）。
        //   垂れを 深く して 下へ 出しても、軒下は 影で 一様に くらく（かがやき 42..50）
        //   牙が 背景に とけて 見えなかった。本人が 見て いた 「波」は
        //   当時 雨樋が 屋根を つきぬけて 面の 上に つくって いた 凹凸。
        //   だから 軒の 一列だけ 山を 高く して、面の 上に 光と 影の さざ波を つくる
        if (nokigawara)
            for (int i = 0; i < py.Length; i++) py[i] *= 1.4f;
        // たて 3段：おく（次の 段に かくれる）→まん中→手前（軒がわ・すこし 出る）
        float[] pz = { 0.62f, -0.10f, -0.72f };
        float[] pl = { -0.010f, 0f, 0.004f };      // 段ごとの 浮きの 足し
        int nx = px.Length, nz = pz.Length;
        var v = new List<Vector3>();
        var uv = new List<Vector2>();
        for (int j = 0; j < nz; j++)
            for (int i = 0; i < nx; i++) {
                v.Add(new Vector3(px[i] * HABA, py[i] + pl[j], pz[j] * NOBE));
                uv.Add(U(px[i] * HABA, pz[j] * NOBE, uvWari));
            }
        var tri = new List<int>();
        for (int j = 0; j < nz - 1; j++)
            for (int i = 0; i < nx - 1; i++) {
                int a = j * nx + i, b = a + 1, c = a + nx, d = c + 1;
                tri.Add(a); tri.Add(d); tri.Add(c);
                tri.Add(a); tri.Add(b); tri.Add(d);
            }
        if (nokigawara) {
            // 軒瓦の 垂れ。ここが **軒先の 輪郭を 波うたせる**＝屋根らしさの もと。
            // ★深さは **屋根の 板の 厚み(0.16)より 深く**する（2026-09-02）。
            //   0.055 で 作って いたら 板の 小口の 陰に かくれて 輪郭に 出なかった
            //   （本人「瓦の手前側の波が立体感出してたはずなのにそれが消えてる」）
            // ★垂れを **一様に 深く しても だめ**（2026-09-02・2度目の 直し）。
            //   0.24 の 平らな 面に したら、ただの 暗い 帯に なって 波に 見えなかった。
            //   実物の 桟瓦は **山の ところだけ 深く 垂れて 前へ 出る**。だから
            //   断面の 点ごとに 垂れの ふかさ(tare)と 前への 出(tz)を 変える。
            //   谷は 屋根板の 厚み(0.16)より 浅いので 板の 小口に かくれ、
            //   山だけが 板の 下に 牙のように 出る＝これが 軒先の 波
            // ★画面では 軒先の 垂れは たった 8px。そこに 写真の 細部を のせようと して
            //   3かい しくじった（まだらな 茶いろの ふち に なった）。
            //   この 大きさで きくのは **輪郭と 明るさ**だけ。だから
            //   (1) UVは 一点に して 平らな いろに する
            //   (2) 山と 谷で 垂れの ふかさを 大きく 変えて 輪郭を 波うたせる
            //   (3) 法線を 外＋上に 向けて 日を うけさせ、まっくらな 軒天(かがやき46)から 引きはなす
            //   瓦の 山の ピッチ 305mm は 画面で 30px＝じゅうぶん 読める（2026-09-02）
            // ★巻きを 直したら 見えるように なった とたん、0.26mの 垂れは **実物(約50mm)の 5ばい**で
            //   のこぎりの 歯に 見えた。ふり幅 0.08m（画面 8px）に おさえる（2026-09-02）
            float[] tare = { 0.10f, 0.12f, 0.13f, 0.12f, 0.09f, 0.07f, 0.05f, 0.05f, 0.09f };
            float[] tz   = { 0.030f, 0.035f, 0.040f, 0.035f, 0.028f, 0.022f, 0.015f, 0.015f, 0.028f };
            int b0 = v.Count;
            for (int i = 0; i < nx; i++) {
                v.Add(new Vector3(px[i] * HABA, py[i] + pl[nz - 1], pz[nz - 1] * NOBE));
                uv.Add(U(px[i] * HABA, pz[nz - 1] * NOBE, uvWari));
            }
            for (int i = 0; i < nx; i++) {
                v.Add(new Vector3(px[i] * HABA, py[i] - tare[i], pz[nz - 1] * NOBE - tz[i]));
                uv.Add(U(px[i] * HABA, pz[nz - 1] * NOBE - tare[i], uvWari));
            }
            for (int i = 0; i < nx - 1; i++) {
                int a = b0 + i, b = a + 1, c = a + nx, d = c + 1;
                tri.Add(a); tri.Add(d); tri.Add(c);
                tri.Add(a); tri.Add(b); tri.Add(d);
            }
        }
        var m = new Mesh { name = nokigawara ? "Nokigawara" : "Kawara" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
        m.RecalculateNormals();
        if (nokigawara) {
            // 垂れの 面の 法線を **外＋上**へ 向けなおす。ほんとうの 向き(外＋下)だと
            // 日が あたらず、まっくらな 軒天と 見わけが つかない
            var nr = m.normals;
            for (int i = 0; i < nx * 2; i++) {
                int k = nr.Length - nx * 2 + i;
                float sx = px[Mathf.Min(nx - 1, i % nx)] < 0f ? -0.45f : 0.30f;
                nr[k] = new Vector3(sx, 0.30f, -0.95f).normalized;
            }
            m.normals = nr;
        }
        m.RecalculateTangents(); m.RecalculateBounds();
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
                           float zIn, float zOut, float yIn, float yOut, Material mat,
                           float uvWari) {
        var noki = Ichimai(true, uvWari);
        var cis = new List<CombineInstance>();
        var a = new Vector3(0f, yIn, zIn);
        var b = new Vector3(0f, yOut, zOut);
        var nobori = (a - b).normalized;                 // 軒（外）から 棟（内）へ のぼる
        var hosen = Vector3.Cross(nobori, Vector3.right).normalized;
        if (hosen.y < 0f) hosen = -hosen;
        var rot = Quaternion.LookRotation(nobori, hosen);
        // 軒先に すこし かかる 位置へ
        var moto = b - nobori * 0.05f;      // 軒先より すこし 外へ
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
    public static int Fuku(Transform parent, HouseRoof.Opt o, Material mat, string name,
                           float uvWari) {
        var noki = Ichimai(true, uvWari);
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
            var moto = a - nobori * 0.05f;  // 軒先より すこし 外へ
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
