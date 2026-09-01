// 入母屋(いりもや)の 屋根を **メッシュで 起こす**。
//
// ★なぜ 箱では だめか（2026-08-16・本人の 指摘「自作3Dに 色を 塗って いるだけで おかしい」）
//   これまでの 屋根は **傾けた 箱 2まい**だった。すると
//     - 棟(むね)が 無い。2まいの あいだが すきまに なって 空が 見える
//     - 妻(つま)がわが 開けっぱなし＝家の 中を 横から のぞける
//     - 面が まっすぐ＝反りが 無い。日本の 屋根に 見えない
//     - 軒(のき)が 板 1まいの 厚みしか なく、下から 見ると 紙のよう
//   絵(テクスチャ)を いくら 貼っても、**形が 箱の ままだと 箱に しか 見えない**。
//
// ★入母屋とは
//   下半分が 四方に 流れる 寄棟(よせむね)、上半分が 妻を 見せる 切妻(きりづま)。
//   農家・寺・城で いちばん よく 見る 形。**寄棟と 切妻の あいだの 段**が 特徴。
//
// ★どう 組むか — 「輪(リング)を 積む」
//   高さ t ごとに **平面の 四角い 輪**を 作り、輪と 輪を 面で つなぐ。
//     HalfZ(t) … 棟に 向かって 0 まで 縮む → 前後の 流れが できる
//     HalfX(t) … 隅棟の ところまで 縮み、そこから **止まる** → 止まった 先は
//                x が 変わらず y だけ 上がる＝**そのまま 妻壁(垂直)に なる**
//   つまり 入母屋の「寄棟＋切妻」は、**縮みかたを 途中で 止めるだけ**で 出る。
//   反りは Y(t) を まっすぐ(t)では なく t^sori に する＝棟がわが 急・軒がわが ゆるい。
//
// ★t < 0 は 軒の 出。ここだけ 外へ ふくらませ、先を すこし はね上げる(軒反り)。
using System.Collections.Generic;
using UnityEngine;

public static class HouseRoof {

    public class Opt {
        public float ax = 5.4f;      // 壁しんの 半分（横）
        public float az = 3.9f;      // 壁しんの 半分（奥ゆき）
        public float eave = 1.15f;   // 軒の 出
        public float yEave = 4.62f;  // 軒げたの 高さ
        public float rise = 1.90f;   // 軒から 棟までの 立ち上がり
        public float hipRun = 1.50f; // 隅棟が 内へ 入る 長さ＝妻の 位置
        public float tHip = 0.49f;   // 隅棟の 天(0..1)。ここから 上が 切妻
        public float sori = 1.34f;   // 反り。1で まっすぐ、大きいほど 軒がわが ゆるむ
        public float tipLift = 0.17f;// 軒先の はね上げ（t*t なので **曲がる**）
        // ★軒先を **直線で** 下げる（2026-09-02）。tipLift を 負に すると t*t の
        //   放物線に なり、そこから 直線の 流れへ つながる ので **屋根が 波うつ**。
        //   流れと 同じ 勾配で まっすぐ 下げたい ときは こちらを つかう
        public float eaveDrop = 0f;  // 軒先の 下がり（t に 比例＝直線）
        public float thick = 0.20f;  // 屋根の 厚み（軒先の 小口）
        public float texM = 1.5f;    // 絵の 1くりかえし ＝ 何m（48px ÷ 32px/m）
        public int nx = 14, nz = 10; // 輪を 何点で きざむか
        public int rings = 13;       // t>0 を 何段に 分けるか（反りの なめらかさ）
    }

    // ---------------------------------------------------------------- 形の 式
    public static float HalfX(Opt o, float t) {
        if (t <= 0f) return o.ax + o.eave * (-t);
        return o.ax - o.hipRun * Mathf.Min(t, o.tHip) / o.tHip;
    }
    public static float HalfZ(Opt o, float t) {
        if (t <= 0f) return o.az + o.eave * (-t);
        return o.az * (1f - t);
    }
    public static float Y(Opt o, float t) {
        if (t <= 0f) return o.yEave + o.tipLift * t * t + o.eaveDrop * t;   // 軒先の 上下
        return o.yEave + o.rise * Mathf.Pow(t, o.sori);
    }

    // ---------------------------------------------------------------- メッシュの 器
    class MB {
        public readonly List<Vector3> v = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<int>[] tri;
        public MB(int sub) { tri = new List<int>[sub]; for (int i = 0; i < sub; i++) tri[i] = new List<int>(); }
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
                         Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, int s) {
            // つぶれた 面は 入れない（法線が NaN に なって 屋根が 真っ黒に なる）
            if ((b - a).sqrMagnitude < 1e-8f && (d - c).sqrMagnitude < 1e-8f) return;
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            uv.Add(ua); uv.Add(ub); uv.Add(uc); uv.Add(ud);
            tri[s].Add(i); tri[s].Add(i + 1); tri[s].Add(i + 2);
            tri[s].Add(i); tri[s].Add(i + 2); tri[s].Add(i + 3);
        }
        public Mesh Make(string name) {
            var m = new Mesh { name = name };
            m.indexFormat = v.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32
                                            : UnityEngine.Rendering.IndexFormat.UInt16;
            m.SetVertices(v); m.SetUVs(0, uv);
            m.subMeshCount = tri.Length;
            for (int i = 0; i < tri.Length; i++) m.SetTriangles(tri[i], i);
            m.RecalculateNormals();
            // ★接線が 無いと 法線マップが でたらめに なる（2026-09-01）
            m.RecalculateTangents();
            m.RecalculateBounds();
            return m;
        }
        public bool Empty { get { return v.Count == 0; } }
    }

    /// <summary>輪を 1周ぶん。上から 見て 時計まわり（+Z面を -X→+X）＝外向きの 法線に なる</summary>
    static Vector3[] Ring(Opt o, float t) {
        float hx = HalfX(o, t), hz = HalfZ(o, t), y = Y(o, t);
        var p = new Vector3[2 * (o.nx + o.nz)];
        int k = 0;
        for (int i = 0; i < o.nx; i++) p[k++] = new Vector3(Mathf.Lerp(-hx, hx, i / (float)o.nx), y, hz);
        for (int i = 0; i < o.nz; i++) p[k++] = new Vector3(hx, y, Mathf.Lerp(hz, -hz, i / (float)o.nz));
        for (int i = 0; i < o.nx; i++) p[k++] = new Vector3(Mathf.Lerp(hx, -hx, i / (float)o.nx), y, -hz);
        for (int i = 0; i < o.nz; i++) p[k++] = new Vector3(-hx, y, Mathf.Lerp(-hz, hz, i / (float)o.nz));
        return p;
    }

    // ---------------------------------------------------------------- 本体
    /// <summary>屋根一式を 建てる。返すのは「中に 入ったら 消す」renderer の ならび</summary>
    public static Renderer[] Build(Transform parent, Opt o, Material tile, Material wood, Material plaster) {
        var made = new List<Renderer>();
        int N = 2 * (o.nx + o.nz);

        // t の 刻み。-1..0 は 軒の 出（3段で じゅうぶん）、0..1 は 屋根の 流れ
        var ts = new List<float> { -1f, -0.62f, -0.28f, 0f };
        // てっぺんは 0 に せず わずかに 手前で 止める。HalfZ=0 に すると 面が つぶれる
        for (int i = 1; i <= o.rings; i++) ts.Add(Mathf.Lerp(0f, 0.985f, i / (float)o.rings));

        var mb = new MB(2);           // 0=瓦 1=板（妻壁・小口）

        // u は **いちばん 外の 輪の 弧長**で 決める。輪ごとに 測ると 棟へ 行くほど
        // 絵が 詰まって「かわらの 大きさが 変わる」ので、列を そろえる
        var outer = Ring(o, ts[0]);
        var u = new float[N + 1];
        for (int j = 0; j < N; j++) u[j + 1] = u[j] + Vector3.Distance(outer[j], outer[(j + 1) % N]);
        for (int j = 0; j <= N; j++) u[j] /= o.texM;

        var vAcc = new float[N + 1];  // 流れに そった 距離＝v
        var prev = outer;
        for (int r = 1; r < ts.Count; r++) {
            var cur = Ring(o, ts[r]);
            var vNext = new float[N + 1];
            for (int j = 0; j <= N; j++) {
                int jj = j % N;
                vNext[j] = vAcc[j] + Vector3.Distance(prev[jj], cur[jj]) / o.texM;
            }
            for (int j = 0; j < N; j++) {
                int j2 = (j + 1) % N;
                Vector3 a = prev[j], b = prev[j2], c = cur[j2], d = cur[j];
                // 垂直な 面＝妻壁。それ以外＝瓦
                Vector3 nrm = Vector3.Cross(b - a, c - a);
                int sub = (nrm.sqrMagnitude > 1e-10f && Mathf.Abs(nrm.normalized.y) < 0.34f) ? 1 : 0;
                mb.Quad(a, b, c, d,
                        new Vector2(u[j], vAcc[j]), new Vector2(u[j + 1], vAcc[j + 1]),
                        new Vector2(u[j + 1], vNext[j + 1]), new Vector2(u[j], vNext[j]), sub);
            }
            prev = cur; vAcc = vNext;
        }

        // --- 軒先の 小口（厚み）。板 1まいに 見えないよう 下へ 落とす
        for (int j = 0; j < N; j++) {
            int j2 = (j + 1) % N;
            Vector3 a = outer[j], b = outer[j2];
            Vector3 aD = a + Vector3.down * o.thick, bD = b + Vector3.down * o.thick;
            mb.Quad(b, a, aD, bD,
                    new Vector2(u[j + 1], 0f), new Vector2(u[j], 0f),
                    new Vector2(u[j], o.thick / o.texM), new Vector2(u[j + 1], o.thick / o.texM), 1);
        }
        // --- 軒天（軒の 裏がわ）。下から 見上げた とき すけない ように
        {
            var pv = outer; var pvD = new Vector3[N];
            for (int j = 0; j < N; j++) pvD[j] = pv[j] + Vector3.down * o.thick;
            float acc = 0f;
            for (int r = 1; r < 4; r++) {                    // t=-1 → 0 の 3段ぶんだけ
                var cu = Ring(o, ts[r]);
                var cuD = new Vector3[N];
                for (int j = 0; j < N; j++) cuD[j] = cu[j] + Vector3.down * o.thick;
                float step = Vector3.Distance(pvD[0], cuD[0]) / o.texM;
                for (int j = 0; j < N; j++) {
                    int j2 = (j + 1) % N;
                    // 上の 面と 逆まわり＝法線が 下を 向く
                    mb.Quad(cuD[j], cuD[j2], pvD[j2], pvD[j],
                            new Vector2(u[j], acc + step), new Vector2(u[j + 1], acc + step),
                            new Vector2(u[j + 1], acc), new Vector2(u[j], acc), 1);
                }
                pvD = cuD; acc += step;
            }
        }

        made.Add(MakeGO(parent, "H_RoofShell", mb.Make("irimoya"), new[] { tile, wood }));

        // --- 棟(むね)。屋根の てっぺんを 太い 帯で ふさぐ。**これが 無いと 空が すける**
        {
            float gx = o.ax - o.hipRun;
            float yTop = Y(o, 1f);
            var mm = new MB(1);
            BoxTo(mm, new Vector3(-gx - 0.22f, yTop - 0.26f, -0.30f),
                      new Vector3(gx + 0.22f, yTop + 0.16f, 0.30f), o.texM, 0);
            // 鬼がわら。棟の 両はしを すこし 立てる
            for (int s = -1; s <= 1; s += 2) {
                float x = s * (gx + 0.20f);
                BoxTo(mm, new Vector3(x - 0.16f, yTop + 0.10f, -0.22f),
                          new Vector3(x + 0.16f, yTop + 0.46f, 0.22f), o.texM, 0);
            }
            made.Add(MakeGO(parent, "H_Mune", mm.Make("mune"), new[] { tile }));
        }

        // --- 隅棟(すみむね)と 破風板(はふいた)。
        // 4すみを 軒先から 棟まで たどる **1本の 線**。t<tHip は 瓦の 隅棟、
        // それより 上は x が 動かない＝妻の へり＝木の 破風板 に なる
        {
            var mm = new MB(2);
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2) {
                    var line = new List<Vector3>();
                    var sub = new List<int>();
                    for (int i = 0; i <= 26; i++) {
                        float t = Mathf.Lerp(-1f, 0.985f, i / 26f);
                        line.Add(new Vector3(sx * HalfX(o, t), Y(o, t), sz * HalfZ(o, t)));
                        sub.Add(t < o.tHip ? 0 : 1);
                    }
                    Prism(mm, line, sub, 0.155f, 0.135f, 0.03f, o.texM);
                }
            made.Add(MakeGO(parent, "H_Sumimune", mm.Make("sumimune"), new[] { tile, wood }));
        }

        // --- 垂木(たるき)。軒の 裏に ならぶ 木。**日本の 屋根らしさの 半分は これ**
        {
            var mm = new MB(1);
            float yIn = Y(o, 0f) - o.thick - 0.055f;
            float yOut = Y(o, -1f) - o.thick - 0.055f;
            // 前後(±Z)の 軒。妻がわは 破風の 内なので 出さない
            float gx = o.ax - o.hipRun;
            for (int sz = -1; sz <= 1; sz += 2)
                for (float x = -gx + 0.18f; x <= gx - 0.18f; x += 0.42f) {
                    Beam(mm, new Vector3(x, yIn, sz * (o.az - 0.15f)),
                             new Vector3(x, yOut, sz * (o.az + o.eave - 0.06f)), 0.055f, 0.075f, o.texM);
                }
            // 左右(±X)の 軒＝寄棟がわ
            for (int sx = -1; sx <= 1; sx += 2)
                for (float z = -o.az + 0.3f; z <= o.az - 0.3f; z += 0.42f) {
                    Beam(mm, new Vector3(sx * (o.ax - 0.15f), yIn, z),
                             new Vector3(sx * (o.ax + o.eave - 0.06f), yOut, z), 0.055f, 0.075f, o.texM);
                }
            made.Add(MakeGO(parent, "H_Taruki", mm.Make("taruki"), new[] { wood }));
        }

        // --- 軒げた（軒の 付け根を 1本の 太い 木で 締める）。
        // これが 無いと 壁と 屋根が 直に つながって「箱に 板を のせた」に 見える
        {
            var mm = new MB(1);
            float y0 = o.yEave - 0.30f, y1 = o.yEave - 0.02f;
            BoxTo(mm, new Vector3(-o.ax - 0.10f, y0, o.az - 0.09f),
                      new Vector3(o.ax + 0.10f, y1, o.az + 0.09f), o.texM, 0);
            BoxTo(mm, new Vector3(-o.ax - 0.10f, y0, -o.az - 0.09f),
                      new Vector3(o.ax + 0.10f, y1, -o.az + 0.09f), o.texM, 0);
            BoxTo(mm, new Vector3(-o.ax - 0.09f, y0, -o.az - 0.09f),
                      new Vector3(-o.ax + 0.09f, y1, o.az + 0.09f), o.texM, 0);
            BoxTo(mm, new Vector3(o.ax - 0.09f, y0, -o.az - 0.09f),
                      new Vector3(o.ax + 0.09f, y1, o.az + 0.09f), o.texM, 0);
            made.Add(MakeGO(parent, "H_Nokigeta", mm.Make("nokigeta"), new[] { wood }));
        }

        // --- 妻の かざり（破風の 内がわの 板壁と 換気の 小窓）。
        // 入母屋の 妻は 農家では たいてい 板が 縦に はって ある
        {
            var mm = new MB(1);
            float gx = o.ax - o.hipRun;
            float yh = Y(o, o.tHip);
            for (int sx = -1; sx <= 1; sx += 2) {
                float x = sx * (gx + 0.055f);
                // 縦の 板の すじ
                for (float t = o.tHip + 0.06f; t < 0.94f; t += 0.11f) {
                    float hz = HalfZ(o, t);
                    BoxTo(mm, new Vector3(x - 0.05f, Y(o, t) - 0.06f, -hz + 0.06f),
                              new Vector3(x + 0.05f, Y(o, t) + 0.05f, hz - 0.06f), o.texM, 0);
                }
                // 妻の 下ばし＝母屋の 見えがかり
                BoxTo(mm, new Vector3(x - 0.07f, yh - 0.12f, -HalfZ(o, o.tHip)),
                          new Vector3(x + 0.07f, yh + 0.02f, HalfZ(o, o.tHip)), o.texM, 0);
            }
            made.Add(MakeGO(parent, "H_Tsuma", mm.Make("tsuma"), new[] { wood }));
        }
        return made.ToArray();
    }

    // ---------------------------------------------------------------- 下屋(げや)
    /// <summary>縁側や 玄関に かける 一方流れの 小さな 屋根。厚み・小口・垂木つき</summary>
    public static Renderer[] Shed(Transform parent, string name,
                                  float x0, float x1, float zIn, float zOut,
                                  float yIn, float yOut, float texM,
                                  Material tile, Material wood) {
        var made = new List<Renderer>();
        float th = 0.15f, side = 0.16f;   // 厚み／けらば(横の 出)
        var mm = new MB(2);               // 0=瓦 1=板
        float ax0 = x0 - side, ax1 = x1 + side;
        Vector3 A = new Vector3(ax0, yIn, zIn), B = new Vector3(ax1, yIn, zIn);
        Vector3 C = new Vector3(ax1, yOut, zOut), D = new Vector3(ax0, yOut, zOut);
        float run = Vector3.Distance(new Vector3(0, yIn, zIn), new Vector3(0, yOut, zOut)) / texM;
        float wid = (ax1 - ax0) / texM;
        float tv = th / texM;
        Vector3 dn = Vector3.down * th;
        // ★まわす 向きを まちがえると 面が 裏返る。裏返った 屋根は
        //   **上から 見ると 軒天(板)が 見えて、瓦のはずが 木の 板に なる**。
        //   じっさい それで 庇と 下屋だけ 茶色の 板に 見えた（2026-08-16）
        // 上の 面（法線が 上を 向く ならび）
        mm.Quad(A, D, C, B, new Vector2(0, 0), new Vector2(0, run), new Vector2(wid, run), new Vector2(wid, 0), 0);
        // 裏（軒天）＝上と 逆まわり
        mm.Quad(A + dn, B + dn, C + dn, D + dn,
                new Vector2(0, 0), new Vector2(wid, 0), new Vector2(wid, run), new Vector2(0, run), 1);
        // 小口 4方
        mm.Quad(C, D, D + dn, C + dn, new Vector2(0, 0), new Vector2(wid, 0), new Vector2(wid, tv), new Vector2(0, tv), 1); // 軒先
        mm.Quad(A, B, B + dn, A + dn, new Vector2(0, 0), new Vector2(wid, 0), new Vector2(wid, tv), new Vector2(0, tv), 1); // 壁がわ
        mm.Quad(B, C, C + dn, B + dn, new Vector2(0, 0), new Vector2(run, 0), new Vector2(run, tv), new Vector2(0, tv), 1); // けらば(+X)
        mm.Quad(D, A, A + dn, D + dn, new Vector2(0, 0), new Vector2(run, 0), new Vector2(run, tv), new Vector2(0, tv), 1); // けらば(-X)
        made.Add(MakeGO(parent, name, mm.Make(name), new[] { tile, wood }));

        // 垂木
        var tk = new MB(1);
        for (float x = x0 + 0.2f; x <= x1 - 0.2f; x += 0.40f)
            Beam(tk, new Vector3(x, yIn - th - 0.05f, zIn), new Vector3(x, yOut - th - 0.05f, zOut),
                 0.05f, 0.07f, texM);
        made.Add(MakeGO(parent, name + "_Taruki", tk.Make(name + "_taruki"), new[] { wood }));
        return made.ToArray();
    }

    // ---------------------------------------------------------------- 部品
    static Renderer MakeGO(Transform parent, string name, Mesh mesh, Material[] mats) {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        // **当たりは つけない。**屋根に 当たりが あると 真下への レイが 屋根を 地めんと
        // 見なして、虫が 空中に わく（Invisible の 層2 と 同じ 事故）
        go.layer = 2;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterials = mats;
        return mr;
    }

    /// <summary>軸に そった 箱を メッシュに 足す（UV は m を texM で わった もの）</summary>
    static void BoxTo(MB mb, Vector3 lo, Vector3 hi, float texM, int sub) {
        float sx = (hi.x - lo.x) / texM, sy = (hi.y - lo.y) / texM, sz = (hi.z - lo.z) / texM;
        Vector3 a = lo, g = hi;
        Vector3 p000 = new Vector3(a.x, a.y, a.z), p100 = new Vector3(g.x, a.y, a.z);
        Vector3 p110 = new Vector3(g.x, g.y, a.z), p010 = new Vector3(a.x, g.y, a.z);
        Vector3 p001 = new Vector3(a.x, a.y, g.z), p101 = new Vector3(g.x, a.y, g.z);
        Vector3 p111 = new Vector3(g.x, g.y, g.z), p011 = new Vector3(a.x, g.y, g.z);
        Vector2 z0 = Vector2.zero;
        mb.Quad(p001, p101, p111, p011, z0, new Vector2(sx, 0), new Vector2(sx, sy), new Vector2(0, sy), sub); // +Z
        mb.Quad(p100, p000, p010, p110, z0, new Vector2(sx, 0), new Vector2(sx, sy), new Vector2(0, sy), sub); // -Z
        mb.Quad(p101, p100, p110, p111, z0, new Vector2(sz, 0), new Vector2(sz, sy), new Vector2(0, sy), sub); // +X
        mb.Quad(p000, p001, p011, p010, z0, new Vector2(sz, 0), new Vector2(sz, sy), new Vector2(0, sy), sub); // -X
        mb.Quad(p011, p111, p110, p010, z0, new Vector2(sx, 0), new Vector2(sx, sz), new Vector2(0, sz), sub); // +Y
        mb.Quad(p000, p100, p101, p001, z0, new Vector2(sx, 0), new Vector2(sx, sz), new Vector2(0, sz), sub); // -Y
    }

    /// <summary>2点を むすぶ 角材（傾いて いても よい）</summary>
    static void Beam(MB mb, Vector3 a, Vector3 b, float halfW, float halfH, float texM) {
        var line = new List<Vector3> { a, b };
        var sub = new List<int> { 0, 0 };
        Prism(mb, line, sub, halfW, halfH, halfH, texM);
    }

    /// <summary>折れ線に そって 角の 棒を のばす。sub は 点ごとの サブメッシュ番号</summary>
    static void Prism(MB mb, List<Vector3> pts, List<int> sub,
                      float halfW, float up, float down, float texM) {
        int n = pts.Count;
        if (n < 2) return;
        var L = new Vector3[n]; var R = new Vector3[n];   // 左右へ ふる 向き
        for (int i = 0; i < n; i++) {
            Vector3 d = (i == 0) ? pts[1] - pts[0] : (i == n - 1) ? pts[n - 1] - pts[n - 2] : pts[i + 1] - pts[i - 1];
            Vector3 side = Vector3.Cross(Vector3.up, d);
            if (side.sqrMagnitude < 1e-8f) side = Vector3.right;   // 真上を 向く 段は 横を 決められない
            side = side.normalized * halfW;
            L[i] = side; R[i] = -side;
        }
        float acc = 0f;
        for (int i = 0; i < n - 1; i++) {
            float step = Vector3.Distance(pts[i], pts[i + 1]) / texM;
            int s = sub[i];
            Vector3 uUp = Vector3.up * up, uDn = Vector3.down * down;
            // 4すみ（手前が i、おくが i+1）
            Vector3 a1 = pts[i] + L[i] + uUp, a2 = pts[i] + R[i] + uUp;
            Vector3 a3 = pts[i] + R[i] + uDn, a4 = pts[i] + L[i] + uDn;
            Vector3 b1 = pts[i + 1] + L[i + 1] + uUp, b2 = pts[i + 1] + R[i + 1] + uUp;
            Vector3 b3 = pts[i + 1] + R[i + 1] + uDn, b4 = pts[i + 1] + L[i + 1] + uDn;
            float w = halfW * 2f / texM, h = (up + down) / texM;
            mb.Quad(a1, a2, b2, b1, new Vector2(0, acc), new Vector2(w, acc), new Vector2(w, acc + step), new Vector2(0, acc + step), s);       // 上
            // ★左の 側面は 巻きが 逆で **裏返って いた**（2026-09-02）。カメラ側が 左面に なる
            //   南西の 隅棟だけ 側面が 消え、帯の 幅ぶん 屋根が すけて 木が 見えた
            //   （本人「透明で奥の木が見える空間がある」）。右面と 同じ 向きに そろえる
            mb.Quad(a1, b1, b4, a4, new Vector2(h, acc), new Vector2(h, acc + step), new Vector2(0, acc + step), new Vector2(0, acc), s);       // 左
            mb.Quad(a2, a3, b3, b2, new Vector2(0, acc), new Vector2(h, acc), new Vector2(h, acc + step), new Vector2(0, acc + step), s);       // 右
            mb.Quad(a3, a4, b4, b3, new Vector2(0, acc), new Vector2(w, acc), new Vector2(w, acc + step), new Vector2(0, acc + step), s);       // 下
            acc += step;
        }
        // 両はしの ふた
        {
            Vector3 uUp = Vector3.up * up, uDn = Vector3.down * down;
            float w = halfW * 2f / texM, h = (up + down) / texM;
            Vector3 a1 = pts[0] + L[0] + uUp, a2 = pts[0] + R[0] + uUp;
            Vector3 a3 = pts[0] + R[0] + uDn, a4 = pts[0] + L[0] + uDn;
            mb.Quad(a1, a4, a3, a2, new Vector2(0, 0), new Vector2(0, h), new Vector2(w, h), new Vector2(w, 0), sub[0]);
            int e = n - 1;
            Vector3 b1 = pts[e] + L[e] + uUp, b2 = pts[e] + R[e] + uUp;
            Vector3 b3 = pts[e] + R[e] + uDn, b4 = pts[e] + L[e] + uDn;
            mb.Quad(b2, b3, b4, b1, new Vector2(0, 0), new Vector2(0, h), new Vector2(w, h), new Vector2(w, 0), sub[e]);
        }
    }
}
