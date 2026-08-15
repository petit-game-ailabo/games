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
    // 端が 見えない ように 広く とる。遠くは 霧で 消える
    public const float Size = 196f;
    public const int Cells = 196;                                  // 1マス 1m
    public static readonly Vector2 Center = new Vector2(-14f, 2f);

    // ★2026-08-15：**遊べる ところを 四角く 決めた。**
    // カメラを 正面に 固定した ので、家の うしろのような 死角に 入れると
    // 何も 見えなく なる。通れる ところは 画面に 映る 範囲に とどめ、
    // まわりは 山・川・生垣で ふさぐ。
    //   おく(-Z)＝山の 斜面／手前(+Z)＝川／左右(X)＝生垣と 木立ち
    public const float PlayMinX = -26f, PlayMaxX = 26f;
    public const float PlayMinZ = -10f, PlayMaxZ = 27f;

    // ★2026-08-15：**高台（みはらし台）。**
    // ここだけは 四角の そとへ 出られる。山の 肩まで 登ると カメラが 裏へ まわりこみ、
    // それまで 背中がわで 見えなかった 谷ぜんたいが 見える＝のぼる ごほうび。
    // 通り道は 細い 一本道に して、他の 死角へは 行けない ままに する
    public static readonly Vector2 Lookout = new Vector2(-20f, -18f);
    public const float LookoutHalfX = 5.0f, LookoutHalfZ = 4.2f;
    public const float TrailX = -20f;          // 登り道の 中心
    public const float TrailHalf = 2.0f;       // 通れる はば の 半分

    public const float Flat = -0.52f;          // 谷そこ の 高さ

    // ★2026-08-15：**谷そこを 円から 四角に 広げた。**
    //   半径11mの 円だと 家の 前の 本道(z=7)に 届かず、道が 山ぎわの 高さに
    //   引っぱられて **家の 前が 3.0m、左手が 3.8m 持ちあがって いた**。
    //   玄関が 二階の 高さに なり、家に 入ったら 上がれなかった（本人の 指摘）。
    //   家・道・畑・納屋・井戸が のる ところは まとめて 平ら に する。
    //   山が 立ちあがるのは その そとがわ
    static readonly Vector2 FlatCenter = new Vector2(0f, 8f);
    static readonly Vector2 FlatHalf = new Vector2(20f, 17f);   // この 中は まっ平ら
    const float FlatBlend = 13f;                                 // ここから 山へ 上がる

    /// <summary>0＝谷そこで まっ平ら、1＝もとの 起伏のまま</summary>
    static float FlatWeight(float x, float z) {
        float dx = Mathf.Max(0f, Mathf.Abs(x - FlatCenter.x) - FlatHalf.x);
        float dz = Mathf.Max(0f, Mathf.Abs(z - FlatCenter.y) - FlatHalf.y);
        return SmoothBand(0f, FlatBlend, Mathf.Sqrt(dx * dx + dz * dz));
    }

    // 山。すぐ そばに そびえる
    static readonly Vector2 MountA = new Vector2(-34f, -30f);
    const float MountAR = 58f, MountAH = 27f;
    static readonly Vector2 MountB = new Vector2(10f, -38f);
    const float MountBR = 44f, MountBH = 16f;

    // ---- 踏み分け道。**たてよこに そろえる。**
    // 斜めに くねると、どこを 歩けるのかが 画面から 読みとれない。
    // 道すじは まっすぐ／直角に して、**ふちの ぎざぎざは 絵の がわで 出す**
    //（Ground シェーダで しきいを ゆらす。歩ける ところは まっすぐの まま）
    public static readonly Vector2[][] Paths = {
        new[] { new Vector2(-25f, 7f), new Vector2(25f, 7f) },        // 本道（画面の よこ）
        new[] { new Vector2(0f, 7f),   new Vector2(0f, 3.4f) },       // 家の 玄関へ
        new[] { new Vector2(-13f, 7f), new Vector2(-13f, 20f) },      // 左：畑・井戸へ
        new[] { new Vector2(-13f, 20f),new Vector2(-4f, 20f) },
        new[] { new Vector2(12f, 7f),  new Vector2(12f, -6f) },       // 右：納屋・祠へ
        new[] { new Vector2(12f, -6f), new Vector2(18f, -6f) },
        new[] { new Vector2(3f, 7f),   new Vector2(3f, 24f) },        // 川べりへ
        // 山への 登り口 → 高台。**勾配の うわぎりが かかる ので、この 長さが 要る**
        new[] { new Vector2(-20f, 7f),  new Vector2(-20f, -4f),
                new Vector2(-20f, -11f), new Vector2(-20f, -18f) },
    };
    // ---- 沢（小川）と 川。**地形に みぞを 掘り、そこへ 水を 流す。**
    // 水は 高い ほうから 低い ほうへ しか 流れないので、道と 同じく
    // 上流から 下流へ 高さを ならして 決める
    public struct Stream {
        public Vector2[] line;
        public float half;      // みぞの 半分の 幅
        public float depth;     // 掘る 深さ
    }
    public static readonly Stream[] Streams = {
        // 小川：山から おりて 家の 左を たてに ながれ、川へ そそぐ。笹船を ながす
        new Stream {
            half = 1.1f, depth = 0.8f,
            line = new[] {
                new Vector2(-22f, -9f), new Vector2(-22f, 0f), new Vector2(-22f, 10f),
                new Vector2(-22f, 20f), new Vector2(-22f, 29f),
            },
        },
        // 大きめの 川：手前を よこに 貫く。**ここが 手前の さかい**。水きり・釣り
        new Stream {
            half = 4.4f, depth = 1.8f,
            line = new[] {
                new Vector2(-52f, 31f), new Vector2(-20f, 31f), new Vector2(0f, 31f),
                new Vector2(20f, 31f),  new Vector2(52f, 31f),
            },
        },
    };
    static float[][] streamProf;
    const float StreamGrade = 0.055f;      // 川は ほとんど 平ら（水は 急には 落ちない）

    const float PathHalf = 0.75f;
    const float PathFade = 1.55f;

    // ★2026-08-15：**勾配の うわぎりは 道ごと。**
    //   谷そこの 道は 人が 荷を かついで 歩く ところ なので ほぼ 平ら。
    //   きつい 角度が 許されるのは 山道だけ。本人の 言うとおり
    //   「歩く 道に 極たんな 角度は ない」
    static readonly float[] PathGrade = {
        0.10f,   // 0 本道（車が 通る）
        0.10f,   // 1 玄関へ
        0.12f,   // 2 畑・井戸へ
        0.12f,   // 3
        0.12f,   // 4 納屋・祠へ
        0.12f,   // 5
        0.12f,   // 6 川べりへ
        0.26f,   // 7 山への 登り（ここだけ 急でよい＝約15度）
    };
    const float DenseStep = 1.0f;      // 道を 1mごとに 刻んで 高さを 決める
    // これだけ 近い 点は **同じ 辻**と みなす。
    // ★広く とりすぎると（1.6m）、辻でも ない 1m となりの 点まで 結ばれて、
    //   本道が 山道に 引っぱり上げられ、本道の 上に 20度の 段が できた。
    //   道の 起点は もとから きっちり 重なって いるので せまくて よい
    const float JoinR = 0.4f;

    // ★2026-08-15：**削る はばは 道ごとに 変える。**
    //   地面を せまく しか 削らないと、山道では 両がわの 斜面が そのまま 残って
    //   **溝の そこを 歩く**ことに なる。実さい 撮ったら 左右が 草の 壁で、
    //   外が まったく 見えず ただの 通路に なって いた。
    //   本物の 山道も 斜面を 広く 削って 棚に する。だから 山道だけ 大きく とる。
    //   ※土の 色が つく はばは PathFade の まま＝道すじは 細い 一本道に 見える
    //   ※測って みたら、削り幅 1.55m のままだと **道の 1.5m よこが 最大 4.7m も 高い**＝
    //     道が 溝の そこに なって いた。路肩を なだらかに 削る はばを とる
    static readonly float[] PathCut = {
        9f,    // 0 本道（車道。路肩を 広く とる）
        5f,    // 1 玄関へ
        5f,    // 2 畑・井戸へ
        5f,    // 3
        5f,    // 4 納屋・祠へ
        5f,    // 5
        5f,    // 6 川べりへ
        12f,   // 7 山道
    };

    // ★土の 色が つく はば（半分）。**本道だけ 車道の 幅に する。**
    //   田舎の 車道は 舗装されて いない ことが 多く、**車 1台ぶん**しか ない。
    //   2台は すれちがえず、対向車が 来たら どちらかが 待避所まで 下がる。
    //   ＝ 3m ほど。ほかの 道は 人が 踏み分けた だけ なので 細い まま
    //   ※1.55 まで 広げたら 家の 前が 一面 土に なった。**土の 帯は ふちの ぼかしぶん
    //     さらに 広がる**（+0.8m）ので、見た目の 幅は これの 倍＋1.6m ある
    static readonly float[] PathHalfPer = { 1.10f, 0.75f, 0.75f, 0.75f, 0.75f, 0.75f, 0.75f, 0.75f };

    // ---- 道の 高さの すじ道。
    //
    // ★2026-08-15 作りなおし。前は **道ごとに ばらばらに** 高さを 決めて いた。
    //   枝道の 起点は「本道の 削った 高さ」では なく **素の 地形の 高さ**から
    //   はじまって いたので、辻で 両者が 食いちがい、そこに 崖が できて いた。
    //   実さい 測ったら 玄関へ 曲がる ところが **72度**、川べりへ 曲がる ところが 69度で、
    //   まったく 登れなかった（本人の「歩けない道がある」）。
    //
    //   直しかた：道ぜんたいを **1つの 網として いっしょに 解く。**
    //     1) 1mごとに 刻む
    //     2) 近い 点どうし（＝辻）の 高さを そろえる
    //     3) なめらかに する
    //     4) 勾配の うわぎりを かける
    //   これを 何回か くりかえすと、辻で つながった まま 全体が なだらかに なる。
    static Vector2[][] dense;      // 道ごとの こまかい 点
    static float[][] denseH;       // その 高さ
    static float[][] denseCum;     // 起点からの 長さ

    static void EnsureProfiles() {
        if (dense != null) return;
        EnsureStreams();
        int n = Paths.Length;
        dense = new Vector2[n][]; denseH = new float[n][]; denseCum = new float[n][];

        for (int p = 0; p < n; p++) {
            var line = Paths[p];
            var pts = new List<Vector2>();
            var cum = new List<float>();
            float run = 0f;
            pts.Add(line[0]); cum.Add(0f);
            for (int i = 0; i < line.Length - 1; i++) {
                float seg = Vector2.Distance(line[i], line[i + 1]);
                int k = Mathf.Max(1, Mathf.CeilToInt(seg / DenseStep));
                for (int j = 1; j <= k; j++) {
                    pts.Add(Vector2.Lerp(line[i], line[i + 1], j / (float)k));
                    run += seg / k;
                    cum.Add(run);
                }
            }
            dense[p] = pts.ToArray(); denseCum[p] = cum.ToArray();
            var h = new float[pts.Count];
            for (int i = 0; i < pts.Count; i++) h[i] = RawHeight(pts[i].x, pts[i].y);
            denseH[p] = h;
        }

        // 素の 地形（引きもどす さきに つかう）
        var raw = new float[n][];
        for (int p = 0; p < n; p++) {
            raw[p] = new float[dense[p].Length];
            for (int i = 0; i < dense[p].Length; i++)
                raw[p][i] = RawHeight(dense[p][i].x, dense[p][i].y);
        }

        // ★くりかえしの 順番と 強さが 肝。
        //   引きもどしを 強く（0.5）すると 辻の そろえと 引っぱりあって 収束せず、
        //   最後に かけた 処理しだいで 辻に また 崖が 出た（実さい 66度に もどった）。
        //   弱く 何度も かけ、**辻の そろえを いちばん 最後に する**
        for (int pass = 0; pass < 200; pass++) {
            // 1) 素の 地形へ そっと 引きもどす。
            //    道は 地形に そって 敷く もの。勾配で 無理な ところ だけ 掘る／盛る
            for (int p = 0; p < n; p++) {
                var h = denseH[p]; var r0 = raw[p];
                for (int i = 0; i < h.Length; i++) h[i] = Mathf.Lerp(h[i], r0[i], 0.12f);
            }
            // 2) すこしだけ ならす（強く かけると 端の 高さが 全体に 広がる）
            for (int p = 0; p < n; p++) {
                var h = denseH[p];
                for (int i = 1; i < h.Length - 1; i++)
                    h[i] = (h[i - 1] + h[i] * 6f + h[i + 1]) * 0.125f;
            }
            // 3) 勾配の うわぎり。行きと 帰りの 両方から かける
            for (int p = 0; p < n; p++) {
                var h = denseH[p]; var c = denseCum[p];
                float g = PathGrade[Mathf.Min(p, PathGrade.Length - 1)];
                for (int i = 1; i < h.Length; i++) {
                    float d = Mathf.Max(c[i] - c[i - 1], 1e-4f);
                    h[i] = Mathf.Clamp(h[i], h[i - 1] - g * d, h[i - 1] + g * d);
                }
                for (int i = h.Length - 2; i >= 0; i--) {
                    float d = Mathf.Max(c[i + 1] - c[i], 1e-4f);
                    h[i] = Mathf.Clamp(h[i], h[i + 1] - g * d, h[i + 1] + g * d);
                }
            }
            // 4) 辻を そろえる。**必ず 最後**。ここが 抜けると 辻が 崖に なる
            for (int a = 0; a < n; a++)
                for (int b = a + 1; b < n; b++)
                    for (int i = 0; i < dense[a].Length; i++)
                        for (int j = 0; j < dense[b].Length; j++) {
                            if ((dense[a][i] - dense[b][j]).sqrMagnitude > JoinR * JoinR) continue;
                            float m = (denseH[a][i] + denseH[b][j]) * 0.5f;
                            denseH[a][i] = m; denseH[b][j] = m;
                        }
        }

        // 仕上げ：辻の そろえと 勾配の うわぎりを 交ごに 何度か。
        // どちらも 差を ちぢめる 向きの 処理なので、交ごに かけると 両方 成りたつ
        for (int fin = 0; fin < 6; fin++) {
            for (int p = 0; p < n; p++) {
                var h = denseH[p]; var c = denseCum[p];
                float g = PathGrade[Mathf.Min(p, PathGrade.Length - 1)];
                for (int i = 1; i < h.Length; i++) {
                    float d = Mathf.Max(c[i] - c[i - 1], 1e-4f);
                    h[i] = Mathf.Clamp(h[i], h[i - 1] - g * d, h[i - 1] + g * d);
                }
                for (int i = h.Length - 2; i >= 0; i--) {
                    float d = Mathf.Max(c[i + 1] - c[i], 1e-4f);
                    h[i] = Mathf.Clamp(h[i], h[i + 1] - g * d, h[i + 1] + g * d);
                }
            }
            for (int a = 0; a < n; a++)
                for (int b = a + 1; b < n; b++)
                    for (int i = 0; i < dense[a].Length; i++)
                        for (int j = 0; j < dense[b].Length; j++) {
                            if ((dense[a][i] - dense[b][j]).sqrMagnitude > JoinR * JoinR) continue;
                            float m = (denseH[a][i] + denseH[b][j]) * 0.5f;
                            denseH[a][i] = m; denseH[b][j] = m;
                        }
        }
    }

    // 川の 水面の 高さ。上流から 下流へ ゆるやかに 下げる
    static void EnsureStreams() {
        if (streamProf != null) return;
        streamProf = new float[Streams.Length][];
        for (int si = 0; si < Streams.Length; si++) {
            var line = Streams[si].line;
            var h = new float[line.Length];
            h[0] = BareHeight(line[0].x, line[0].y);
            for (int i = 1; i < line.Length; i++) {
                float seg = Vector2.Distance(line[i - 1], line[i]);
                float want = BareHeight(line[i].x, line[i].y);
                // **下る ばかり。** のぼる 川は ない
                h[i] = Mathf.Min(want, h[i - 1] - 0.02f);
                h[i] = Mathf.Max(h[i], h[i - 1] - StreamGrade * seg);
            }
            streamProf[si] = h;
        }
    }

    /// <summary>川も 道も 考えない、いちばん 素の 地形</summary>
    static float BareHeight(float x, float z) {
        float h = Flat;
        h += Bump(x, z, MountA, MountAR, MountAH);
        h += Bump(x, z, MountB, MountBR, MountBH);
        h += Mathf.Sin(x * 0.071f + 1.3f) * Mathf.Cos(z * 0.089f - 0.7f) * 2.3f
           + Mathf.Sin(x * 0.213f) * Mathf.Cos(z * 0.171f) * 0.7f
           + Mathf.Sin((x + z) * 0.041f) * 1.6f;
        return Mathf.Lerp(Flat, h, FlatWeight(x, z));
    }

    /// <summary>いちばん 近い 川。みぞの ふかさと 水面の 高さ</summary>
    public static void NearestStream(float x, float z, out int index, out float across, out float waterY) {
        EnsureStreams();
        index = -1; across = float.MaxValue; waterY = 0f;
        var p = new Vector2(x, z);
        for (int si = 0; si < Streams.Length; si++) {
            var line = Streams[si].line;
            for (int i = 0; i < line.Length - 1; i++) {
                float t;
                float d = DistToSegment(p, line[i], line[i + 1], out t);
                if (d >= across) continue;
                across = d; index = si;
                waterY = Mathf.Lerp(streamProf[si][i], streamProf[si][i + 1], t);
            }
        }
    }

    /// <summary>道を 考えない 素の 地形（川の みぞは 掘って ある）</summary>
    public static float RawHeight(float x, float z) {
        float h = BareHeight(x, z);
        // 川の みぞを 掘る。岸から まん中へ なだらかに 下げる
        int si; float across, waterY;
        NearestStream(x, z, out si, out across, out waterY);
        if (si >= 0) {
            var st = Streams[si];
            float bank = st.half * 2.2f;                       // ここから 岸が 下がりはじめる
            float t = 1f - SmoothBand(st.half * 0.55f, bank, across);
            if (t > 0f) {
                float bed = waterY - st.depth;                 // 川底
                h = Mathf.Lerp(h, Mathf.Min(h, bed), t);
            }
        }
        return h;
    }

    /// <summary>その 場所の 地めんの 高さ（道を 削った あと）</summary>
    public static float Height(float x, float z) {
        EnsureProfiles();
        int pi; float dist, ph;
        NearestPathEx(x, z, out pi, out dist, out ph);
        // **削る はばは 道ごと。** 山道は 広い 棚に 削る（せまいと 溝に なる）
        float cut = pi >= 0 ? PathCut[pi] : PathFade;
        float w = 1f - SmoothBand(PathHalf, cut, dist);
        // **道の まん中は ほぼ そのまま 道の 高さに する。**
        // 8割しか 寄せないと 地形の でこぼこが 2割 のこり、歩く 面が ざらつく。
        // ふちは w が 落ちるので、まわりへは なだらかに つながる
        float h = w <= 0f ? RawHeight(x, z)
                          : Mathf.Lerp(RawHeight(x, z), ph, w * 0.96f);
        // 高台は 平らに ならす。**道の 幅だけでは 立って 見わたせない**。
        // ただし **道の 上では 道の 高さを 優先する。**
        // 棚の ふちが 道を 横ぎる ところで 地面を 引き上げて しまい、
        // そこだけ 32度の 坂に なって いた（棚の 高さと 道の 高さが 引っぱりあう）
        float lt = LookoutFlat(x, z) * (1f - w * 0.92f);
        if (lt > 0f) h = Mathf.Lerp(h, LookoutY, lt);
        return h;
    }

    static float lookoutY; static bool lookoutReady;
    /// <summary>高台の 地めんの 高さ。登り道の すじ道から とる</summary>
    public static float LookoutY {
        get {
            if (!lookoutReady) {
                EnsureProfiles();
                int pi; float d, ph;
                NearestPathEx(Lookout.x, Lookout.y, out pi, out d, out ph);
                lookoutY = ph; lookoutReady = true;
            }
            return lookoutY;
        }
    }

    /// <summary>高台らしさ 0〜1（1＝まっ平ら）</summary>
    public static float LookoutFlat(float x, float z) {
        float dx = Mathf.Abs(x - Lookout.x), dz = Mathf.Abs(z - Lookout.y);
        // ふちの ぼかしは 広めに とる。**棚は 斜面を 8m ほど 削って 作る**ので、
        // 急に 切ると 石切り場の ような 崖に なる
        float tx = 1f - SmoothBand(LookoutHalfX, LookoutHalfX + 5.5f, dx);
        float tz = 1f - SmoothBand(LookoutHalfZ, LookoutHalfZ + 5.5f, dz);
        return Mathf.Clamp01(Mathf.Min(tx, tz));
    }

    /// <summary>人が 立ち入れる ところ（四角＋山への 一本道＋高台）</summary>
    public static bool Walkable(float x, float z, float margin = 0f) {
        if (x > PlayMinX - margin && x < PlayMaxX + margin
         && z > PlayMinZ - margin && z < PlayMaxZ + margin) return true;
        // 登り道の 帯
        if (Mathf.Abs(x - TrailX) < TrailHalf + margin
         && z < PlayMinZ && z > Lookout.y - LookoutHalfZ) return true;
        // 高台
        return Mathf.Abs(x - Lookout.x) < LookoutHalfX + margin
            && Mathf.Abs(z - Lookout.y) < LookoutHalfZ + margin;
    }

    /// <summary>道らしさ 0〜1（1＝まるっきり 土）</summary>
    public static float PathWeight(float x, float z) {
        EnsureProfiles();
        float w, ph;
        NearestPath(x, z, out w, out ph);
        return w;
    }

    static void NearestPath(float x, float z, out float weight, out float height) {
        int pi; float dist;
        NearestPathEx(x, z, out pi, out dist, out height);
        float half = pi >= 0 ? PathHalfPer[pi] : PathHalf;
        weight = 1f - SmoothBand(half, half + (PathFade - PathHalf), dist);
    }

    /// <summary>いちばん 近い 道。どの 道か・どれだけ 離れて いるか・その 高さ</summary>
    static void NearestPathEx(float x, float z, out int index, out float dist, out float height) {
        var p = new Vector2(x, z);
        dist = float.MaxValue; height = Flat; index = -1;
        for (int li = 0; li < dense.Length; li++) {
            var pts = dense[li]; var hs = denseH[li];
            for (int i = 0; i < pts.Length - 1; i++) {
                float t;
                float d = DistToSegment(p, pts[i], pts[i + 1], out t);
                if (d >= dist) continue;
                dist = d; index = li;
                height = Mathf.Lerp(hs[i], hs[i + 1], t);
            }
        }
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

    /// <summary>水面の 帯を 作る。川ごとに 1枚の 細長い 面</summary>
    public static GameObject BuildWater(Transform parent, Material mat) {
        EnsureProfiles();
        var root = new GameObject("Water");
        root.transform.SetParent(parent, false);

        for (int si = 0; si < Streams.Length; si++) {
            var st = Streams[si];
            var line = st.line;
            const int Sub = 6;                                  // 1区間を いくつに 割るか
            int steps = (line.Length - 1) * Sub + 1;
            var verts = new Vector3[steps * 3];                 // 左岸・まん中・右岸
            var cols = new Color32[steps * 3];
            var uvs = new Vector2[steps * 3];
            var tris = new int[(steps - 1) * 4 * 3];

            float run = 0f;
            for (int k = 0; k < steps; k++) {
                int seg = Mathf.Min(k / Sub, line.Length - 2);
                float t = (k - seg * Sub) / (float)Sub;
                Vector2 c = Vector2.Lerp(line[seg], line[seg + 1], t);
                Vector2 dir = (line[seg + 1] - line[seg]).normalized;
                Vector2 nrm = new Vector2(-dir.y, dir.x);
                float y = Mathf.Lerp(streamProf[si][seg], streamProf[si][seg + 1], t);
                if (k > 0) run += Vector2.Distance(c, new Vector2(verts[(k - 1) * 3 + 1].x, verts[(k - 1) * 3 + 1].z));

                for (int e = 0; e < 3; e++) {
                    float off = (e - 1) * st.half;
                    var p = c + nrm * off;
                    // 岸は 水面と 同じ 高さ、まん中は ほんの すこし 下げて 面を 作る
                    verts[k * 3 + e] = new Vector3(p.x, y - (e == 1 ? 0.02f : 0f), p.y);
                    cols[k * 3 + e] = new Color32((byte)(e == 1 ? 255 : 40), 0, 0, 255);
                    uvs[k * 3 + e] = new Vector2(off, run);
                }
            }
            int ti = 0;
            for (int k = 0; k < steps - 1; k++) {
                for (int e = 0; e < 2; e++) {
                    int a = k * 3 + e, b = a + 1, c2 = a + 3, d = c2 + 1;
                    tris[ti++] = a; tris[ti++] = c2; tris[ti++] = b;
                    tris[ti++] = b; tris[ti++] = c2; tris[ti++] = d;
                }
            }

            var mesh = new Mesh { name = "Water" + si };
            mesh.vertices = verts; mesh.colors32 = cols; mesh.uv = uvs; mesh.triangles = tris;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();

            var go = new GameObject(si == 0 ? "Ogawa" : "Kawa");
            go.transform.SetParent(root.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            UnityEditor.AssetDatabase.CreateAsset(mesh, "Assets/Art/Materials/WaterMesh" + si + ".asset");
        }
        return root;
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
            int si2; float across2, wy2;
            NearestStream(x, z, out si2, out across2, out wy2);
            if (si2 >= 0 && across2 < Streams[si2].half * 2.0f) continue;   // 川の 中には 生えない
            float sl = Slope(x, z);
            if (sl > 0.85f) continue;                               // 岩はだには 生えない
            if (slopeOnly) {
                // **遊べる 四角の 中には 木を 生やさない。**
                // 中に 立てると 見とおしを ふさぎ、2Dで 見せる 意味が なくなる。
                // 木は そとがわに ならべて「そこから 先は 森」と 見せる 壁に する
                if (Walkable(x, z, 1.5f)) continue;
                float rise = Height(x, z) - Flat;
                if (sl < 0.16f && rise < 2.2f && z < PlayMaxZ) continue;
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
