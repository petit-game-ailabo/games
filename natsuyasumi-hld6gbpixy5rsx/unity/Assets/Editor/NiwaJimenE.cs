using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// 庭の 地面を **1場面 1枚の 絵**に 焼く（2026-08-31・D-119）。
//
// ★なぜ タイルを やめるか
//   いままでは 48x48px・6色の 手描きタイルを 2m角で 65x55回 くりかえして いた＝目が 見える。
//   ぼくなつ1/2は 背景が プリレンダの 一枚絵で、カメラが 画面ごとに 固定。
//   1平方メートルずつ 全部 個別に 描いて あるので、くりかえしと いう 考えが そもそも ない。
//   絵はがき方式（カメラ固定・1画面1構図）は これと 同じ 構造なので 同じ 作りに できる。
//
// ★どこまで 焼くか（**こだわる 範囲を しぼる**）
//   画面での 地面の こまかさは 主人公の あたりで よこ 1mあたり 約121px 要る。
//   広い 範囲を 1枚に すると こまかさが 足りなく なる ので、
//   **庭と 門の外の 道の 32m四方だけ**を 4096²で 焼く（1mあたり 128px）。
//   画面は 主人公の あたりで よこ 15.5mを 1920pxで 映す＝124px/m なので ちょうど 足りる。
//   その 外は 同じ 草の 写真の タイル。さかい目は 塀の ところなので 見えない。
//
// ★素材は 本人が 用意した 真上からの 写真（ji_kusa/ji_tsuchi/ji_jari/ji_koke）。
//   光の むらが 0〜2%＝方向の ある 光が 焼きこまれて いない ので、
//   こちらの 朝夕夜の 光を 当てられる
public static class NiwaJimenE {
    public const float HABA = 32f;                                 // 焼く 範囲（m四方）
    public static readonly Vector2 NAKA = new Vector2(0f, 2f);     // その まん中（world xz）
    const int N = 4096;                                            // 焼いた 絵の こまかさ
    const int MN = 512;                                            // マスクの こまかさ（4.7cm/px）
    // ★JPEGで 書く（2026-08-31）。4096²の PNGは 32MB あり、塗りかたを 変える たびに
//   リポジトリに 積む ことに なる。写真の 地面なので 透明度は 要らない
    const string OUT = "Assets/Art/Textures/niwa_jimen.jpg";
    const string SIG = "Assets/Art/Textures/niwa_jimen.sig.txt";
    const string ME = "Assets/Editor/NiwaJimenE.cs";   // 署名に 自分の 更新時こくを 入れる

    struct Moto { public Color32[] px; public int n; public float m; }

    /// <summary>素材を 読む。焼く 絵の こまかさに 合わせて **先に 縮める**
    /// （そのまま 点で 拾うと ざらつく）</summary>
    static Moto Yomu(string name, float tileM) {
        // ★取りこみずみの Texture2D から GetPixels32 しては いけない（2026-08-31）。
        //   圧縮された 状態で 読むと **まっピンク (255,0,255) が 返る**。
        //   実際に 地面が ぜんぶ ピンクに なった（素材の PNG じたいは 無事だった）。
        //   PNGの バイト列を じかに 読んで 展開すれば 取りこみ設定に 左右されない
        string path = "Assets/Art/Textures/" + name + ".jpg";
        if (!System.IO.File.Exists(path)) {
            Debug.LogError("[Probe] JimenE 素材が ない: " + name); return default;
        }
        var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!t.LoadImage(System.IO.File.ReadAllBytes(path))) {
            Debug.LogError("[Probe] JimenE 素材が ひらけない: " + name);
            Object.DestroyImmediate(t); return default;
        }
        int n = Mathf.Max(8, Mathf.RoundToInt(tileM * N / HABA));
        var src = t.GetPixels32();
        int sw = t.width, sh = t.height;
        Object.DestroyImmediate(t);
        var dst = new Color32[n * n];
        for (int y = 0; y < n; y++) {
            int y0 = y * sh / n, y1 = Mathf.Max(y0 + 1, (y + 1) * sh / n);
            for (int x = 0; x < n; x++) {
                int x0 = x * sw / n, x1 = Mathf.Max(x0 + 1, (x + 1) * sw / n);
                int r = 0, g = 0, b = 0, c = 0;
                for (int sy = y0; sy < y1; sy++)
                    for (int sx = x0; sx < x1; sx++) {
                        var p = src[sy * sw + sx];
                        r += p.r; g += p.g; b += p.b; c++;
                    }
                dst[y * n + x] = new Color32((byte)(r / c), (byte)(g / c), (byte)(b / c), 255);
            }
        }
        return new Moto { px = dst, n = n, m = n * HABA / N };
    }

    static Color32 Hiku(Moto s, float wx, float wz) {
        float fu = wx / s.m, fv = wz / s.m;
        int x = Mathf.FloorToInt((fu - Mathf.Floor(fu)) * s.n);
        int y = Mathf.FloorToInt((fv - Mathf.Floor(fv)) * s.n);
        return s.px[Mathf.Clamp(y, 0, s.n - 1) * s.n + Mathf.Clamp(x, 0, s.n - 1)];
    }

    // ---- ゆらぎ（タイルの 目を こわす・さかい目を いびつに する）
    static float Hash(int x, int y, int s) {
        int n = x * 374761393 + y * 668265263 + s * 1442695040;
        n = (n ^ (n >> 13)) * 1274126177;
        return ((n ^ (n >> 16)) & 0x7fffffff) / (float)0x7fffffff;
    }
    static float Noise(float x, float y, int s) {
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
        float xf = x - xi, yf = y - yi;
        float u = xf * xf * (3f - 2f * xf), v = yf * yf * (3f - 2f * yf);
        return Mathf.Lerp(Mathf.Lerp(Hash(xi, yi, s), Hash(xi + 1, yi, s), u),
                          Mathf.Lerp(Hash(xi, yi + 1, s), Hash(xi + 1, yi + 1, s), u), v);
    }
    static float Fbm(float x, float y, int s) {
        float r = 0f, a = 0.5f;
        for (int i = 0; i < 4; i++) { r += a * Noise(x, y, s + i * 17); x *= 2f; y *= 2f; a *= 0.5f; }
        return r;
    }

    /// <summary>ふち関数（0→1に なめらかに 立ちあがる）。
    /// ★Unityの SmoothStep(from,to,t) は **これでは ない**（2026-08-31）。
    ///   あれは from から to へ なめらかに 補間する もので、返り値は from〜to。
    ///   ふち関数の つもりで 1f から 引いたら、t が 1に 飽和した 全域で -6.65 が 返り、
    ///   マスクが **7.65** に なった。土を 7.65倍で 混ぜて 地面が まっピンク (255,0,255) に なった</summary>
    static float Fuchi(float a, float b, float x) {
        float t = Mathf.Clamp01((x - a) / Mathf.Max(1e-6f, b - a));
        return t * t * (3f - 2f * t);
    }

    // ================= 地ばんの 凸凹 =================
    // 本人 2026-08-31「地面自体を多少凸凹させてテクスチャ貼り付ければ解決しないかな。
    // 人が上下しないレベルの凸凹か、あるいは特定の場所だけ」
    //
    // ★カメラの ふせ角が 10°＝**高さだけ 6倍に 拡大されて 映る**。
    //   奥ゆき 1m は 画面で 21px しか ないが、高さ 1m は 121px。
    //   だから 20cmの 起伏でも 画面では 24px 動く＝平らな 面が 平らに 見える 弱点に 直に 効く。
    // ★歩く ところは 平ら（本人の 指示）。庭の 踏み跡・家の 敷地・道・高台は うねらせない。
    //   塀の そとは 誰も 歩かない ので 大きく うねらせる

    /// <summary>箱の 中なら 1、そとへ yawa で 0</summary>
    static float Hako(float wx, float wz, float x0, float x1, float z0, float z1, float yawa) {
        float d = Mathf.Max(Mathf.Max(x0 - wx, wx - x1), Mathf.Max(z0 - wz, wz - z1));
        return 1f - Fuchi(0f, yawa, d);
    }

    /// <summary>その 場所の 起伏の 大きさ（0＝平ら）</summary>
    static float Haba(float wx, float wz) {
        float hira = 0f;
        // ★平らに する ところを 取りすぎない こと。はじめ 家の 敷地を |x|<7.5・z0.5〜18.5、
        //   踏み跡を |x|<3.3 に したら **見えて いる 芝生の ほぼ 全部が 平ら**に なり、
        //   凸凹が まったく 出なかった
        hira = Mathf.Max(hira, Hako(wx, wz, -1.3f, 1.3f, -7.5f, 8.5f, 1.0f));   // 庭の 踏み跡
        hira = Mathf.Max(hira, Hako(wx, wz, -2.2f, 2.2f, -10.5f, -5.5f, 0.8f));  // 門の 坂（段の ふちも）
        hira = Mathf.Max(hira, Hako(wx, wz, -6.5f, 6.5f, 1.5f, 13f, 1.2f));     // 家の 敷地（2階建て・南西を 切りかき）
        hira = Mathf.Max(hira, Hako(wx, wz, -44f, 44f, -12.6f, -6.6f, 1.2f) * 0.85f); // 道
        hira = Mathf.Max(hira, Hako(wx, wz, 10f, 32f, -8f, 12f, 2.5f));         // 高台
        // ★塀の 線は ほぼ 平ら（0.8）。塀は 1枚ずつ 固い ので、支柱の 間で 段が つくと
        //   垣根が こわれて 見える。2.5m間かくで 振幅20cm・波長7.7mだと 段が 19cm＝
        //   塀の 高さ87cmの 2割。0.8 平らに すれば 段は 6cmに おさまる
        hira = Mathf.Max(hira, Hako(wx, wz, -10.2f, 10.2f, -6.7f, -5.3f, 1.0f) * 0.8f);
        hira = Mathf.Max(hira, Hako(wx, wz, -10.2f, 10.2f, 13.7f, 15.1f, 1.0f) * 0.8f);
        // 建てものと 花壇の 足もと（2026-09-05）。凸凹の 上に 置くと 角が 浮く／沈む
        hira = Mathf.Max(hira, Hako(wx, wz, -9.4f, -6.1f, 0.0f, 4.7f, 1.0f));    // 納屋
        hira = Mathf.Max(hira, Hako(wx, wz, 6.75f, 9.25f, 0.25f, 3.15f, 1.0f));  // 花壇
        hira = Mathf.Max(hira, Hako(wx, wz, 5.85f, 6.80f, 2.45f, 3.75f, 0.8f));  // 立水栓
        hira = Mathf.Max(hira, Hako(wx, wz, -10.4f, -9.0f, -7f, 14f, 1.0f) * 0.8f);
        hira = Mathf.Max(hira, Hako(wx, wz, 9.0f, 10.4f, -7f, 14f, 1.0f) * 0.8f);
        // 塀の そと（誰も 歩かない）は 大きく
        float soto = 1f + Fuchi(12f, 26f, Mathf.Max(Mathf.Abs(wx), wz - 16f)) * 0.9f;
        return 0.30f * soto * (1f - hira);
    }

    // ---- 高台（本人 2026-08-31「先に高台の箱を直しておいて」）。
    //   箱だと **垂直な 壁と まっすぐな 天**＝画面で いちばん 目立つ 直線に なる。
    //   地ばんの 高さの 一部に して しまえば、地面の あみも 物の すわりも 影も 影の 形も
    //   ぜんぶ 自動で ついてくる。坂も 別に 作らなくて よい（ふち全体が 21°の 土手）。
    //   ★道（z -12.4〜-7）に かからない よう 北へ ずらす（道が 土手を 登って しまう）
    // ★のぼれる 傾斜に する（本人 2026-08-31「高台が歩いていけない」）。
    //   なめらかな 落としかた（Fuchi＝smoothstep）は **平均の 1.5倍**の 傾斜が まん中に 出る。
    //   はじめ 半径(7.5, 6.0)・高さ3.4m に したら
    //     東西 1.5*3.4/(0.58*7.5)=1.17 → 49.5°
    //     南北 1.5*3.4/(0.58*6.0)=1.47 → 55.7°
    //   CharacterController の 上限は 50°なので **南北からは のぼれなかった**。
    //   いちばん 急な ところ＝1.5*TH/(0.58*R) を 40°(0.839) 以下に おさめる
    public const float TX = 21f, TZ = 2f, TH = 2.8f;   // 塀は x=9.7
    const float TRX = 11f, TRZ = 9f;      // 東西33.4° / 南北38.8°
    static float Takadai(float wx, float wz) {
        float dx = (wx - TX) / TRX, dz = (wz - TZ) / TRZ;
        return TH * (1f - Fuchi(0.42f, 1f, Mathf.Sqrt(dx * dx + dz * dz)));
    }

    /// <summary>いちばん 急な ところの 傾斜（度）。組み立ての ときに 出して 確かめる</summary>
    public static string TakadaiKeisha() {
        float ax = 1.5f * TH / (0.58f * TRX), az = 1.5f * TH / (0.58f * TRZ);
        return "東西" + (Mathf.Atan(ax) * Mathf.Rad2Deg).ToString("F1")
             + "° 南北" + (Mathf.Atan(az) * Mathf.Rad2Deg).ToString("F1") + "°";
    }

    /// <summary>地ばんの 高さ</summary>
    // ---- 庭の 段（2026-09-04・本人「家の周りってぼくなつみたいに石垣じゃない？…入口がちょっと斜めの坂で、
    //   石垣に囲まれつつ、入り口あたりは大きく開いてる」）。
    //   山あいの 家は 道より 一段 高い 平場に 建ち、段差を 石垣が 支え、道からは 斜めの 坂で 上がる。
    //   庭 ぜんぶを NH だけ 上げ、南の ふち（z=-6）は 石垣の ぶん 鋭く、東西北は 生垣の 中で ゆるく 落とす。
    //   坂は |x|<1.6 で z -9.5→-6 を 直線で 上がる：0.6m/3.5m ＝ 9.7°（歩く 道の 上限 10°の 内）
    public const float NH = 0.6f;                       // 段の 高さ
    public const float SAKA_Z0 = -9.5f, SAKA_Z1 = -6.0f, SAKA_HABA = 1.6f;
    public static float Dan(float wx, float wz) {
        // 平場：x -10.4..10.4、z -6..14.5。南は 0.25m で 立ちあがる（石垣）、ほかは 2m で なだらかに
        float sx = 1f - Fuchi(0f, 2.0f, Mathf.Max(-10.4f - wx, wx - 10.4f));
        // ★北の ふちは 生垣を 家の うしろ（z=14.4）へ まわした ぶん 広げた（2026-09-05）。
        //   14.5 の ままだと 北の 生垣が 段の 斜面の 上に 立って 傾いて 見える
        float sn = 1f - Fuchi(0f, 2.0f, wz - 15.4f);
        float ss = Fuchi(SAKA_Z1 - 0.25f, SAKA_Z1, wz);
        float hira = Mathf.Min(sx, Mathf.Min(sn, ss));
        // 坂：まん中の 帯だけ、直線で 上がる。横は 0.4m で 消す（坂の 両わきも 石垣）
        float saka = Mathf.Clamp01((wz - SAKA_Z0) / (SAKA_Z1 - SAKA_Z0)) * (1f - Fuchi(SAKA_HABA - 0.1f, SAKA_HABA + 0.3f, Mathf.Abs(wx)));
        if (wz > SAKA_Z1) saka = 0f;
        return NH * Mathf.Max(hira, saka);
    }

    public static float Takasa(float wx, float wz) {
        float taka = Takadai(wx, wz) + Dan(wx, wz);
        float a = Haba(wx, wz);
        if (a < 0.001f) return taka;
        // ★Fbm を そのまま つかっては いけない（2026-08-31）。
        //   4段の 値ノイズは 理屈のうえでは 0〜0.94 だが、**実際の ばらつきは 標準偏差0.11**。
        //   (Fbm-0.5) を そのまま つかったら 高低差が 18cmしか 出なかった（狙いの 半分以下）。
        //   平均0.47 を 引いて 3.0倍する と ふつうに ±0.10、たまに ±0.30 に なる
        return taka + ((Fbm(wx * 0.13f, wz * 0.13f, 401) - 0.47f) * 3.0f
                       + (Fbm(wx * 0.42f, wz * 0.42f, 733) - 0.47f) * 1.05f) * a;
    }

    // ---- 門の 外の 道の ふち。**焼いた 絵と 形の 両方で 同じ 式を つかう**ので
    //      絵の ところ（±16m）と そとの 形が つながる
    public static float MichiKita(float wx) {
        return -7.0f + (Fbm(wx * 0.25f, 3.7f, 53) - 0.5f) * 1.2f;
    }
    public static float MichiMinami(float wx) {
        return -12.4f + (Fbm(wx * 0.21f, 11.3f, 97) - 0.5f) * 1.3f;
    }

    /// <summary>凸凹に そった 地面の あみ。uvTile>0 なら 世界の 長さで タイル、
    /// 0以下なら 0..1（焼いた 一枚絵むけ）</summary>
    public static Mesh Ami(string name, float habaX, float habaZ, Vector2 naka,
                           float masu, float uvTile) {
        int nx = Mathf.Max(1, Mathf.RoundToInt(habaX / masu));
        int nz = Mathf.Max(1, Mathf.RoundToInt(habaZ / masu));
        var v = new Vector3[(nx + 1) * (nz + 1)];
        var uv = new Vector2[v.Length];
        for (int j = 0; j <= nz; j++)
            for (int i = 0; i <= nx; i++) {
                float lx = -habaX * 0.5f + habaX * i / nx;
                float lz = -habaZ * 0.5f + habaZ * j / nz;
                float wx = naka.x + lx, wz = naka.y + lz;
                int k = j * (nx + 1) + i;
                v[k] = new Vector3(lx, Takasa(wx, wz), lz);
                uv[k] = uvTile > 0f ? new Vector2(wx / uvTile, wz / uvTile)
                                    : new Vector2(i / (float)nx, j / (float)nz);
            }
        var tri = new int[nx * nz * 6];
        int t = 0;
        for (int j = 0; j < nz; j++)
            for (int i = 0; i < nx; i++) {
                int a = j * (nx + 1) + i, b = a + 1, c = a + nx + 1, d = c + 1;
                tri[t++] = a; tri[t++] = c; tri[t++] = d;
                tri[t++] = a; tri[t++] = d; tri[t++] = b;
            }
        var m = new Mesh { name = name };
        if (v.Length > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.vertices = v; m.uv = uv; m.triangles = tri;
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    /// <summary>門の 外の 道。**うねった ふち**の 帯（箱だと まっすぐな 線に なる）</summary>
    public static Mesh MichiAmi(float x0, float x1, float masu, float uvTile) {
        int nx = Mathf.Max(2, Mathf.RoundToInt((x1 - x0) / masu));
        const int NZ = 6;
        var v = new Vector3[(nx + 1) * (NZ + 1)];
        var uv = new Vector2[v.Length];
        float cx = (x0 + x1) * 0.5f;
        for (int i = 0; i <= nx; i++) {
            float wx = Mathf.Lerp(x0, x1, i / (float)nx);
            float zk = MichiKita(wx), zm = MichiMinami(wx);
            for (int j = 0; j <= NZ; j++) {
                float wz = Mathf.Lerp(zm, zk, j / (float)NZ);
                int k = j * (nx + 1) + i;
                v[k] = new Vector3(wx - cx, Takasa(wx, wz) + 0.02f, wz);
                uv[k] = new Vector2(wx / uvTile, wz / uvTile);
            }
        }
        var tri = new int[nx * NZ * 6];
        int t = 0;
        for (int j = 0; j < NZ; j++)
            for (int i = 0; i < nx; i++) {
                int a = j * (nx + 1) + i, b = a + 1, c = a + nx + 1, d = c + 1;
                tri[t++] = a; tri[t++] = c; tri[t++] = d;
                tri[t++] = a; tri[t++] = d; tri[t++] = b;
            }
        var m = new Mesh { name = "NiwaMichi" };
        m.vertices = v; m.uv = uv; m.triangles = tri;
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    // ---- マスク（世界座標で 描く）
    static int Mx(float wx) { return Mathf.RoundToInt((wx - (NAKA.x - HABA * 0.5f)) / HABA * MN); }
    static int Mz(float wz) { return Mathf.RoundToInt((wz - (NAKA.y - HABA * 0.5f)) / HABA * MN); }
    static float M2W { get { return HABA / MN; } }

    static void Maru(float[] m, Vector2 c, float r, float yawa, float koi) {
        int x0 = Mathf.Max(0, Mx(c.x - r - yawa)), x1 = Mathf.Min(MN - 1, Mx(c.x + r + yawa));
        int z0 = Mathf.Max(0, Mz(c.y - r - yawa)), z1 = Mathf.Min(MN - 1, Mz(c.y + r + yawa));
        for (int z = z0; z <= z1; z++)
            for (int x = x0; x <= x1; x++) {
                float wx = NAKA.x - HABA * 0.5f + x * M2W, wz = NAKA.y - HABA * 0.5f + z * M2W;
                float d = Vector2.Distance(new Vector2(wx, wz), c);
                float a = koi * (1f - Fuchi(r, r + yawa, d));
                if (a > m[z * MN + x]) m[z * MN + x] = a;
            }
    }

    static void Sen(float[] m, Vector2 a, Vector2 b, float haba, float yawa, float koi) {
        var d = b - a; float len = d.magnitude;
        if (len < 0.001f) { Maru(m, a, haba, yawa, koi); return; }
        int n = Mathf.CeilToInt(len / (haba * 0.4f)) + 1;
        for (int i = 0; i <= n; i++) Maru(m, a + d * (i / (float)n), haba, yawa, koi);
    }

    /// <summary>マスクを なめらかに 読む</summary>
    static float Toru(float[] m, float u, float v) {
        float fx = Mathf.Clamp(u * MN - 0.5f, 0f, MN - 1.001f);
        float fz = Mathf.Clamp(v * MN - 0.5f, 0f, MN - 1.001f);
        int x = (int)fx, z = (int)fz; float tx = fx - x, tz = fz - z;
        float a = m[z * MN + x], b = m[z * MN + x + 1];
        float c = m[(z + 1) * MN + x], e = m[(z + 1) * MN + x + 1];
        return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, e, tx), tz);
    }

    /// <summary>絵を 敷く 板。**UVは 自分で 持つ**（2026-08-31）。
    /// Unityの Plane の UVの 向きに 頼ったら 奥と 手前が 逆に なり、
    /// 門の外の 道が 家の うしろに 出た。v=0 が z の 小さい ほう＝焼いた 絵の 並びと 同じ</summary>
    /// <summary>一枚絵の 板。**ふちは 地ばんの 下へ もぐらせる**（2026-08-31）。
    /// 本人「家の周囲と、その先とで 緑の線が 断絶されるタイミングがある」＝
    /// 板を 5cm 浮かせて いた ので、その ふちが 段差の 線に なって いた。
    /// もぐらせれば 段が 出ず、見える さかいは 絵と タイルが 一致する ところに なる</summary>
    public static Mesh Ita() {
        const float masu = 0.35f;
        int n = Mathf.RoundToInt(HABA / masu);
        var v = new Vector3[(n + 1) * (n + 1)];
        var uv = new Vector2[v.Length];
        for (int j = 0; j <= n; j++)
            for (int i = 0; i <= n; i++) {
                float lx = -HABA * 0.5f + HABA * i / n, lz = -HABA * 0.5f + HABA * j / n;
                float wx = NAKA.x + lx, wz = NAKA.y + lz;
                float d = Mathf.Min(HABA * 0.5f - Mathf.Abs(lx), HABA * 0.5f - Mathf.Abs(lz));
                float uki = Mathf.Lerp(-0.15f, 0.05f, Fuchi(0f, 0.8f, d));
                int k = j * (n + 1) + i;
                v[k] = new Vector3(lx, Takasa(wx, wz) + uki, lz);
                uv[k] = new Vector2(i / (float)n, j / (float)n);
            }
        var tri = new int[n * n * 6];
        int t = 0;
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++) {
                int a = j * (n + 1) + i, b = a + 1, c = a + n + 1, e = c + 1;
                tri[t++] = a; tri[t++] = c; tri[t++] = e;
                tri[t++] = a; tri[t++] = e; tri[t++] = b;
            }
        var m = new Mesh { name = "JimenEIta" };
        if (v.Length > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.vertices = v; m.uv = uv; m.triangles = tri;
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    /// <summary>庭の 地面の 絵を 焼いて、その テクスチャを 返す</summary>
    public static Texture2D Yaku(Transform root) {
        var t0 = System.DateTime.Now;

        // ---- 場面から 位置を 拾う（BuildNiwa と 数字を 二重に 持たない）
        var ishi = new List<Vector3>();   // とびいし
        var ki = new List<Bounds>();      // 木
        var hei = new List<Bounds>();     // 塀（線に する ため 範囲で 持つ）
        Bounds ie = default; bool ieAru = false;
        foreach (var r in root.GetComponentsInChildren<Renderer>()) {
            if (r == null || !r.enabled) continue;
            string n = r.transform.name;
            if (n.StartsWith("Kage")) continue;
            if (n.Contains("path_stone")) ishi.Add(r.bounds.center);
            else if (n.Contains("tree")) ki.Add(r.bounds);
            else if (n.Contains("fence")) hei.Add(r.bounds);
        }
        var ieT = root.Find("Ie");
        if (ieT != null)
            foreach (var r in ieT.GetComponentsInChildren<Renderer>()) {
                if (!ieAru) { ie = r.bounds; ieAru = true; } else ie.Encapsulate(r.bounds);
            }
        ishi.Sort((a, b) => a.z.CompareTo(b.z));

        // ---- マスクを 描く
        var mDoro = new float[MN * MN];   // 踏みかためた 土
        var mJari = new float[MN * MN];   // 砂利
        var mKoke = new float[MN * MN];   // 苔

        // 踏み跡：門から 玄関までの とびいしの 連なりに そって
        for (int i = 0; i + 1 < ishi.Count; i++) {
            var a = new Vector2(ishi[i].x, ishi[i].z);
            var b = new Vector2(ishi[i + 1].x, ishi[i + 1].z);
            if (Vector2.Distance(a, b) > 3f) continue;      // 別の 並びへの 飛びは つながない
            Sen(mDoro, a, b, 0.38f, 0.40f, 1f);
        }
        if (ishi.Count > 0) {
            var saki = new Vector2(ishi[0].x, ishi[0].z);
            var oku = new Vector2(ishi[ishi.Count - 1].x, ishi[ishi.Count - 1].z);
            Sen(mDoro, saki, saki + new Vector2(0f, -1.6f), 0.45f, 0.55f, 1f);  // 門の 手前へ
            Maru(mDoro, oku + new Vector2(0f, 0.9f), 0.9f, 1.0f, 1f);         // 玄関前の すりへり
            Maru(mJari, saki + new Vector2(0f, -0.7f), 1.1f, 1.0f, 0.9f);     // 門の あたりは 砂利
        }
        // 門の 外の 道（z=-9.5・はば5m）。北の ふちは **まっすぐでは ない**
        for (int z = 0; z < MN; z++) {
            float wz = NAKA.y - HABA * 0.5f + z * M2W;
            for (int x = 0; x < MN; x++) {
                float wx = NAKA.x - HABA * 0.5f + x * M2W;
                float zk = MichiKita(wx), zm = MichiMinami(wx);
                float a = (1f - Fuchi(zk - 0.35f, zk + 0.35f, wz))
                          * Fuchi(zm - 0.35f, zm + 0.35f, wz);
                if (a > mDoro[z * MN + x]) mDoro[z * MN + x] = a;
            }
        }
        // 木の 根もと：日かげで 草が はげ、苔が つく
        foreach (var b in ki) {
            var c = new Vector2(b.center.x, b.center.z);
            float r = Mathf.Max(0.6f, Mathf.Min(b.size.x, b.size.z) * 0.30f);
            Maru(mDoro, c, r * 0.7f, r * 0.9f, 0.75f);
            Maru(mKoke, c, r * 1.2f, r * 1.3f, 0.8f);
        }
        // 塀ぎわ：草刈りが とどかず 湿る
        // 1枚ずつ 丸を 置くと 2.5mおきの 点に なる。板の 長いほうを 線に して つなぐ
        foreach (var b in hei) {
            Vector2 a, e;
            if (b.size.x >= b.size.z) {
                a = new Vector2(b.min.x, b.center.z); e = new Vector2(b.max.x, b.center.z);
            } else {
                a = new Vector2(b.center.x, b.min.z); e = new Vector2(b.center.x, b.max.z);
            }
            Sen(mKoke, a, e, 0.22f, 0.45f, 0.55f);
        }
        // 家の 足もと：雨だれの 落ちる 線
        if (ieAru) {
            var e = ie; e.Expand(new Vector3(0.5f, 0f, 0.5f));
            var p0 = new Vector2(e.min.x, e.min.z); var p1 = new Vector2(e.max.x, e.min.z);
            var p2 = new Vector2(e.max.x, e.max.z); var p3 = new Vector2(e.min.x, e.max.z);
            Sen(mDoro, p0, p1, 0.30f, 0.45f, 0.7f);
            Sen(mKoke, p1, p2, 0.35f, 0.50f, 0.7f);
            Sen(mKoke, p2, p3, 0.40f, 0.60f, 0.85f);   // 北がわは いちばん 日かげ
            Sen(mKoke, p3, p0, 0.35f, 0.50f, 0.7f);
        }

        // 納屋・立水栓・花壇の まわり（2026-09-05）。**位置は 場面から 拾う**（数字を 二重に 持たない）
        var tatemono = new List<Vector3>();
        foreach (var nm in new[] { "Naya", "Suido", "Hanadan" }) {
            var o = root.Find(nm);
            if (o == null) continue;
            Bounds b2 = default; bool ar2 = false;
            foreach (var r in o.GetComponentsInChildren<Renderer>()) {
                if (r == null || !r.enabled) continue;
                if (!ar2) { b2 = r.bounds; ar2 = true; } else b2.Encapsulate(r.bounds);
            }
            if (!ar2) continue;
            tatemono.Add(b2.center);
            var c2 = new Vector2(b2.center.x, b2.center.z);
            if (nm == "Naya") {
                // 戸口の 前は 踏み かたまり、とびいしの 並びへ 通いみちが つく
                var kuchi = new Vector2(NiwaNaya.Guchi.x, NiwaNaya.Guchi.z);
                Maru(mDoro, kuchi, 0.85f, 0.9f, 0.85f);
                Sen(mDoro, kuchi, new Vector2(1.7f, 0.1f), 0.32f, 0.50f, 0.62f);
                Sen(mKoke, new Vector2(c2.x, c2.y + 1.9f), new Vector2(c2.x, c2.y - 1.9f), 0.35f, 0.5f, 0.5f);
            } else if (nm == "Suido") {
                Maru(mJari, c2, 0.72f, 0.70f, 0.90f);   // 水受けの まわりは 砂利
                Maru(mKoke, c2, 1.05f, 0.90f, 0.55f);   // いつも 濡れて いる ので 苔
            } else {
                Maru(mDoro, c2, 1.75f, 0.70f, 0.80f);   // 花壇の まわりは 踏んだ 土
            }
        }

        // ★地ばんの 高さを 絵に 焼きこむ（2026-08-31）。
        //   なめらかな 起伏は **明るさでは ほとんど 見えない**。傾斜 5〜8%＝3〜5°なので
        //   真昼だと 明るさの ちがいは 0.3%。ふせ角10°では 自分を 隠しも しない。
        //   実際の 地面は 高い ところが 乾いて 黄みどり、低い ところが 湿って 苔っぽい。
        //   色に すれば 時こくに よらず 見える
        var mTaka = new float[MN * MN];
        float takaMin = 9f, takaMax = -9f;
        for (int z = 0; z < MN; z++) {
            float wz = NAKA.y - HABA * 0.5f + z * M2W;
            for (int x = 0; x < MN; x++) {
                float wx = NAKA.x - HABA * 0.5f + x * M2W;
                float h = Takasa(wx, wz);
                mTaka[z * MN + x] = h;
                if (h < takaMin) takaMin = h;
                if (h > takaMax) takaMax = h;
            }
        }
        Debug.Log("[Probe] JimenE takasa " + takaMin.ToString("F2") + " .. " + takaMax.ToString("F2"));

        // ---- 中身が 前と 同じ なら 焼き直さない（1回 28.6秒 かかる・2026-08-31）
        var sig = new System.Text.StringBuilder();
        // ★版数を 手で 上げる 方式は やめた（2026-08-31）。
        //   塗りかたを 変えたのに 上げ わすれて、まっピンクの 絵を そのまま 使い回した。
        //   **この スクリプト じしんの 更新時こく**を 入れれば、直したら かならず 焼き直る
        sig.Append(System.IO.File.GetLastWriteTimeUtc(ME).Ticks)
           .Append(",").Append(N).Append(",").Append(MN).Append(",")
           .Append(HABA).Append(",").Append(NAKA);
        foreach (var q in ishi) sig.Append("|i").Append(q.ToString("F2"));
        foreach (var q in ki) sig.Append("|k").Append(q.center.ToString("F2"))
                                 .Append(q.size.ToString("F2"));
        foreach (var q in hei) sig.Append("|h").Append(q.center.ToString("F2"));
        sig.Append("|e").Append(ieAru ? ie.center.ToString("F2") + ie.size.ToString("F2") : "-");
        foreach (var q in tatemono) sig.Append("|t").Append(q.ToString("F2"));
        foreach (var nm in new[] { "ji_kusa", "ji_tsuchi", "ji_jari", "ji_koke" }) {
            var fi = new System.IO.FileInfo("Assets/Art/Textures/" + nm + ".jpg");
            sig.Append("|m").Append(fi.Exists ? fi.Length : 0);
        }
        string sigNow = sig.ToString();
        if (System.IO.File.Exists(OUT) && System.IO.File.Exists(SIG)
            && System.IO.File.ReadAllText(SIG) == sigNow) {
            Debug.Log("[Probe] JimenE すえおき（中身が 同じ）");
            return AssetDatabase.LoadAssetAtPath<Texture2D>(OUT);
        }

        // ---- 焼く
        var kusa = Yomu("ji_kusa", 3.0f);
        var doro = Yomu("ji_tsuchi", 2.25f);
        var jari = Yomu("ji_jari", 1.5f);
        var koke = Yomu("ji_koke", 1.875f);
        if (kusa.px == null || doro.px == null || jari.px == null || koke.px == null) return null;

        var buf = new Color32[N * N];
        float hidariX = NAKA.x - HABA * 0.5f, teZ = NAKA.y - HABA * 0.5f;
        float w2 = HABA / N;
        for (int y = 0; y < N; y++) {
            float wz = teZ + (y + 0.5f) * w2;
            float v = (y + 0.5f) / N;
            for (int x = 0; x < N; x++) {
                float wx = hidariX + (x + 0.5f) * w2;
                float u = (x + 0.5f) / N;
                // さかい目を いびつに する（まっすぐな ふちを なくす）
                float nx = (Fbm(wx * 0.55f, wz * 0.55f, 91) - 0.5f) * 0.055f;
                float nz = (Fbm(wx * 0.55f + 31f, wz * 0.55f + 17f, 137) - 0.5f) * 0.055f;

                var c = Hiku(kusa, wx, wz);
                float r = c.r, g = c.g, b = c.b;

                float taka = Toru(mTaka, u, v);
                float mk = Mathf.Max(Toru(mKoke, u + nx, v + nz),
                                     Mathf.Clamp01((-taka - 0.015f) * 7f) * 0.40f);  // くぼみは 湿る
                if (mk > 0.004f) {
                    var s = Hiku(koke, wx, wz);
                    r += (s.r - r) * mk; g += (s.g - g) * mk; b += (s.b - b) * mk;
                }
                float md = Toru(mDoro, u + nx, v + nz);
                if (md > 0.004f) {
                    var s = Hiku(doro, wx, wz);
                    r += (s.r - r) * md; g += (s.g - g) * md; b += (s.b - b) * md;
                }
                float mj = Toru(mJari, u + nx, v + nz);
                if (mj > 0.004f) {
                    var s = Hiku(jari, wx, wz);
                    r += (s.r - r) * mj; g += (s.g - g) * mj; b += (s.b - b) * mj;
                }

                // ★ふちの 3mは むらを 消す。そとは むらの 無い タイルの 草なので、
                //   消して おかないと 明暗の 差が 線に 見える
                // 大きな むら（10mほどの ゆるい 明暗）。これが タイルの 目を いちばん こわす
                float fchi = Fuchi(0f, 3.0f,
                    Mathf.Min(HABA * 0.5f - Mathf.Abs(wx - NAKA.x),
                              HABA * 0.5f - Mathf.Abs(wz - NAKA.y)));
                // 夏の 焼け（日なたの 草は 黄みどりに なる）。一様な 緑の 面を こわす
                float kare = Mathf.Max(
                                 Mathf.Clamp01((Fbm(wx * 0.075f, wz * 0.075f, 211) - 0.50f) * 3.0f),
                                 Mathf.Clamp01((taka - 0.015f) * 7f))            // 高みは 乾く
                             * 0.62f * (1f - md) * fchi;     // 土の 上では やらない
                if (kare > 0.004f) {
                    r += (r * 1.16f - r) * kare;
                    g += (g * 1.02f - g) * kare;
                    b += (b * 0.68f - b) * kare;
                }
                float mura = 1f + (Fbm(wx * 0.11f, wz * 0.11f, 7) - 0.47f) * 0.62f * fchi;
                buf[y * N + x] = new Color32(
                    (byte)Mathf.Clamp(r * mura, 0f, 255f),
                    (byte)Mathf.Clamp(g * mura, 0f, 255f),
                    (byte)Mathf.Clamp(b * mura, 0f, 255f), 255);
            }
        }

        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        tex.SetPixels32(buf); tex.Apply();
        System.IO.File.WriteAllBytes(OUT, tex.EncodeToJPG(94));
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(OUT, ImportAssetOptions.ForceUpdate);
        var ti = AssetImporter.GetAtPath(OUT) as TextureImporter;
        if (ti != null) {
            ti.textureType = TextureImporterType.Default;
            ti.filterMode = FilterMode.Bilinear;
            ti.mipmapEnabled = true;      // 奥は ちいさく 映る。ミップが ないと ちらつく
            ti.anisoLevel = 8;            // ふせ角10°＝ほぼ 真横。異方性は 必須
            ti.isReadable = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.maxTextureSize = 4096;
            ti.SaveAndReimport();
        }
        System.IO.File.WriteAllText(SIG, sigNow);
        var sb = new System.Text.StringBuilder();
        sb.Append("[Probe] JimenE ").Append(N).Append("px/").Append(HABA).Append("m ")
          .Append((System.DateTime.Now - t0).TotalSeconds.ToString("F1")).Append("s").Append(System.Environment.NewLine);
        sb.Append("[Probe] JimenE ishi=").Append(ishi.Count);
        foreach (var q in ishi) sb.Append(" (").Append(q.x.ToString("F1")).Append(",")
                                  .Append(q.z.ToString("F1")).Append(")");
        sb.Append(System.Environment.NewLine).Append("[Probe] JimenE ki=").Append(ki.Count);
        foreach (var q in ki) sb.Append(" (").Append(q.center.x.ToString("F1")).Append(",")
                                 .Append(q.center.z.ToString("F1")).Append(")");
        sb.Append(System.Environment.NewLine).Append("[Probe] JimenE hei=").Append(hei.Count)
          .Append(" ie=").Append(ieAru ? ie.center.ToString("F1") + " " + ie.size.ToString("F1")
                                       : "なし");
        Debug.Log(sb.ToString());
        return AssetDatabase.LoadAssetAtPath<Texture2D>(OUT);
    }
}
