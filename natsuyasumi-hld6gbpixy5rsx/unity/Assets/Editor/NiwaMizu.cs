using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// 家の 東がわの 水まわり（2026-09-05）。
//
// ★本人「家の右側に外の水道蛇口と、ホースが欲しい。そしてヒマワリか何かを育てるような
//   プランター？鉢？あるいは地面直？の場所が欲しい。そこで毎日水を上げて、育てたい」
//
// 調べた こと
//   ・田舎の 家の 外の 水道は **立水栓**（コンクリの 柱＋真ちゅうの 蛇口）に
//     水受け（コンクリの パン）。柱の わきに ホース掛け
//   ・ヒマワリは 鉢では 育たない（根が 深い）。地面直の 畝か、板で 囲った 花壇。
//     支柱を そえて 麻ひもで 結ぶ。8月なかばが 満開、下葉から 枯れて くる
//   ・花は 東〜南を 向いて そろう（若い うちは 日を 追い、咲くと 東で 止まる）
//
// 置きかた：母屋の 東の 壁（x=5.4）と 東の 生垣（内がわの 面が x=9.25）の あいだ。
//   立水栓と 花壇を **東西に ならべる**（南北に ならべると 手前の ヒマワリが 蛇口を 隠す）。
public static class NiwaMizu {
    // ---- 世界での 置き場所（BuildNiwa と NiwaJimenE から 参照する）
    // ★立水栓は 花壇の **西どなり**（2026-09-05）。はじめ 花壇の 北（z=5.3）に 置いたら
    //   カメラは いつも 南から 北を 見る ので **ヒマワリの うしろに 完全に 隠れた**。
    //   南北に ならべる ものは 手前から 背の 低い 順に する
    public static readonly Vector3 SUI = new Vector3(6.30f, 0f, 3.10f);      // 立水栓
    public const float DAN_X0 = 6.95f, DAN_X1 = 9.05f;                       // 花壇
    public const float DAN_Z0 = 0.45f, DAN_Z1 = 2.95f;
    public const float DAN_H = 0.19f;                                        // 板の 高さ
    static readonly float[] UNE_Z = { -0.66f, 0.44f };                       // 畝の まん中

    static Material mCon, mShin, mHose, mKuki, mHa, mHana, mGaku, mTsubomi, mTake, mTsuchi;

    static Material Iro(string name, Color c, float tsuya) {
        return NiwaBuhin.Mat(name, null, Vector2.one, c, false, false, tsuya);
    }

    public static void Build(Transform root) {
        mCon = NiwaBuhin.Fit("MizuCon", "shashin/ie_kabe_yogore.jpg", 0.16f, 0.95f, 0.7f,
                             new Color(0.84f, 0.83f, 0.80f));
        mShin = Iro("MizuShinchu", new Color(0.66f, 0.55f, 0.28f), 0.55f);
        mHose = Iro("MizuHose", new Color(0.20f, 0.34f, 0.22f), 0.38f);
        mKuki = Iro("MizuKuki", new Color(0.38f, 0.50f, 0.24f), 0.10f);
        mTake = NiwaBuhin.Mat("MizuTake", "shashin/take_kawa_ki.jpg", new Vector2(1f, 4f), Color.white);
        // ★1くりかえし 2.25m だと 花壇の 中が **のっぺりした 板の 床**に 見えた（2026-09-05）。
        //   耕した 土は 手のひら ほどの 粒が 見える ので 0.7m まで つめる
        mTsuchi = NiwaBuhin.Fit("MizuTsuchi", "ji_tsuchi.jpg", DAN_X1 - DAN_X0, DAN_Z1 - DAN_Z0, 0.70f,
                                new Color(0.60f, 0.52f, 0.42f));
        mHa = NiwaBuhin.Mat("MizuHimawariHa", "himawari_ha.png", Vector2.one, Color.white, true);
        mHana = NiwaBuhin.Mat("MizuHimawariHana", "himawari_hana.png", Vector2.one, Color.white, true);
        mTsubomi = NiwaBuhin.Mat("MizuHimawariTsubomi", "himawari_tsubomi.png", Vector2.one, Color.white, true);
        mGaku = NiwaBuhin.Mat("MizuHimawariGaku", "himawari_hana.png", Vector2.one,
                              new Color(0.30f, 0.42f, 0.20f), true);

        Suido(root);
        Hanadan(root);
    }

    // ---------------------------------------------------------------- 立水栓と ホース
    static void Suido(Transform root) {
        var g = new GameObject("Suido");
        g.transform.SetParent(root, false);
        float gy = NiwaJimenE.Takasa(SUI.x, SUI.z);
        g.transform.position = new Vector3(SUI.x, gy, SUI.z);
        var t = g.transform;

        const float H = 0.94f;
        NiwaBuhin.Hako(t, "Suido_Hashira", new Vector3(0f, H * 0.5f - 0.05f, 0f),
                       new Vector3(0.16f, H + 0.10f, 0.16f), mCon, true);
        NiwaBuhin.Hako(t, "Suido_Kasa", new Vector3(0f, H + 0.02f, 0f),
                       new Vector3(0.21f, 0.05f, 0.21f), mCon);
        // 蛇口（南を 向く。庭がわ から つかう）
        NiwaBuhin.Bou(t, "Suido_Jaguchi", new Vector3(0f, 0.78f, -0.06f), new Vector3(0f, 0.78f, -0.17f),
                      0.018f, mShin);
        NiwaBuhin.Bou(t, "Suido_Hakidashi", new Vector3(0f, 0.79f, -0.17f), new Vector3(0f, 0.70f, -0.17f),
                      0.016f, mShin);
        NiwaBuhin.Hako(t, "Suido_Handoru", new Vector3(0f, 0.86f, -0.06f),
                       new Vector3(0.11f, 0.022f, 0.022f), mShin);
        NiwaBuhin.Hako(t, "Suido_Handoru", new Vector3(0f, 0.86f, -0.06f),
                       new Vector3(0.022f, 0.022f, 0.11f), mShin);
        // 水受け（コンクリの パン）と 中の 水たまり
        NiwaBuhin.Hako(t, "Suido_Pan", new Vector3(0f, 0.06f, -0.20f),
                       new Vector3(0.66f, 0.16f, 0.50f), mCon, true);
        NiwaBuhin.Hako(t, "Suido_Mizu", new Vector3(0f, 0.135f, -0.20f),
                       new Vector3(0.54f, 0.02f, 0.38f),
                       Iro("MizuTamari", new Color(0.24f, 0.30f, 0.28f), 0.85f));
        // ホース掛け（柱の 東がわ）
        NiwaBuhin.Bou(t, "Suido_Kake", new Vector3(0.07f, 0.62f, 0f), new Vector3(0.20f, 0.62f, 0f),
                      0.014f, mShin);
        NiwaBuhin.Bou(t, "Suido_Kake", new Vector3(0.20f, 0.62f, 0f), new Vector3(0.20f, 0.70f, 0f),
                      0.014f, mShin);

        // ---- ホース。掛けに 3巻き ＋ 先を 地めんへ 垂らして 花壇まで はわせる
        {
            var maki = new List<Vector3>();
            for (int i = 0; i <= 46; i++) {
                float a = i * Mathf.PI * 2f * 3f / 46f;            // 3巻き
                float r = 0.155f - 0.012f * (i / 46f);
                maki.Add(new Vector3(0.245f + 0.030f * (i / 46f), 0.62f - 0.155f + Mathf.Cos(a) * r,
                                     Mathf.Sin(a) * r));
            }
            NiwaBuhin.Mesh1(t, "Suido_HoseMaki", NiwaBuhin.Kan("SuidoHoseMaki", maki, 0.016f), mHose);
            // 蛇口へ つなぐ 短い ぶん
            NiwaBuhin.Mesh1(t, "Suido_HoseMoto", NiwaBuhin.Kan("SuidoHoseMoto", new List<Vector3> {
                new Vector3(0.02f, 0.72f, -0.16f), new Vector3(0.12f, 0.66f, -0.10f),
                new Vector3(0.22f, 0.62f, -0.02f), new Vector3(0.26f, 0.60f, 0.04f),
            }, 0.016f), mHose);
        }
        // 地めんを はう ぶん。花壇の **手前(南)**を まわして 東の はしまで。
        // ★北がわを 通すと ヒマワリの うしろに 入って 1本も 見えない（2026-09-05）
        {
            var kado = new[] {
                new Vector2(SUI.x + 0.26f, SUI.z - 0.16f), new Vector2(SUI.x + 0.42f, SUI.z - 1.35f),
                new Vector2(DAN_X0 - 0.30f, DAN_Z0 - 0.55f), new Vector2(DAN_X0 + 0.90f, DAN_Z0 - 0.72f),
                new Vector2(DAN_X1 - 0.25f, DAN_Z0 - 0.35f),
            };
            var michi = new List<Vector3>();
            for (int k = 0; k + 1 < kado.Length; k++)
                for (int i = 0; i < 6; i++) {
                    float u = i / 6f;
                    var q = Vector2.Lerp(kado[k], kado[k + 1], u);
                    q.x += Mathf.Sin((k + u) * 3.1f) * 0.11f;      // くねらせる（まっすぐな 管は 嘘くさい）
                    michi.Add(new Vector3(q.x - SUI.x, Jimen(q) - gy, q.y - SUI.z));
                }
            {
                var q = kado[kado.Length - 1];
                michi.Add(new Vector3(q.x - SUI.x, Jimen(q) - gy, q.y - SUI.z));
            }
            NiwaBuhin.Mesh1(t, "Suido_HoseJi", NiwaBuhin.Kan("SuidoHoseJi", michi, 0.016f), mHose);
            // 先の ノズル
            var saki = michi[michi.Count - 1];
            var muki = (saki - michi[michi.Count - 2]).normalized;
            var nz = NiwaBuhin.Mesh1(t, "Suido_Nozuru",
                                     NiwaBuhin.Tsutsu("SuidoNozuru", 0.024f, 0.036f, 0.15f, false, 0.2f, 10),
                                     mShin);
            nz.transform.localPosition = saki;
            nz.transform.localRotation = Quaternion.FromToRotation(Vector3.up, muki);
        }
    }

    /// <summary>地めんに 置く ものの 高さ。★**見えて いる 地面は 当たりより 0.05m 上**
    /// （JimenE の 一枚絵の 板）。当たりの 高さ（Takasa）に 置くと 板の 下に もぐって
    /// **まるごと 見えなく なる**。はじめ ホースを +0.025 で 置いて 1本も 出なかった</summary>
    static float Jimen(Vector2 xz) { return NiwaJimenE.Takasa(xz.x, xz.y) + 0.05f + 0.025f; }

    // ---------------------------------------------------------------- 花壇と ヒマワリ
    static void Hanadan(Transform root) {
        var g = new GameObject("Hanadan");
        g.transform.SetParent(root, false);
        float cx = (DAN_X0 + DAN_X1) * 0.5f, cz = (DAN_Z0 + DAN_Z1) * 0.5f;
        float gy = NiwaJimenE.Takasa(cx, cz);
        g.transform.position = new Vector3(cx, gy, cz);
        var t = g.transform;

        float hx = (DAN_X1 - DAN_X0) * 0.5f, hz = (DAN_Z1 - DAN_Z0) * 0.5f;
        // 土（へりの 板より 少し 低い＝水が たまる）
        NiwaBuhin.Hako(t, "Hanadan_Tsuchi", new Vector3(0f, 0.01f, 0f),
                       new Vector3(hx * 2f - 0.10f, 0.22f, hz * 2f - 0.10f), mTsuchi);
        // ★畝（2026-09-05）。平らな 土の 面は 上から 見ると **板の 床**に しか 見えない。
        //   耕した ところは 畝の 天が 光を うけ、あいだが 影に なる ので 段が 出る
        var mUne = NiwaBuhin.Mat("MizuUne", "ji_tsuchi.jpg", new Vector2(hx * 2f / 0.7f, 0.9f),
                                 new Color(0.74f, 0.65f, 0.52f));
        foreach (float uz in UNE_Z)
            NiwaBuhin.Hako(t, "Hanadan_Une", new Vector3(0f, 0.155f, uz),
                           new Vector3(hx * 2f - 0.22f, 0.09f, 0.62f), mUne);
        // へりの 板（4枚。角に 杭）
        var mIta = NiwaBuhin.Fit("HanadanIta", "shashin/ie_ki.jpg", hx * 2f, DAN_H, 0.55f,
                                 new Color(1.20f, 1.10f, 0.92f));
        foreach (float sz in new[] { -1f, 1f })
            NiwaBuhin.Hako(t, "Hanadan_Ita", new Vector3(0f, DAN_H * 0.5f, sz * hz),
                           new Vector3(hx * 2f + 0.06f, DAN_H, 0.06f), mIta);
        foreach (float sx in new[] { -1f, 1f })
            NiwaBuhin.Hako(t, "Hanadan_Ita", new Vector3(sx * hx, DAN_H * 0.5f, 0f),
                           new Vector3(0.06f, DAN_H, hz * 2f + 0.06f), mIta);
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                NiwaBuhin.Hako(t, "Hanadan_Kui", new Vector3(sx * hx, DAN_H * 0.5f + 0.03f, sz * hz),
                               new Vector3(0.08f, DAN_H + 0.10f, 0.08f), mIta);

        // ---- ヒマワリ（株ごとに 高さ・向きを ばらす。そろえると 造花に 見える）
        var kabu = new List<Transform>();
        var hana = new List<Renderer>();
        var tsubomi = new List<Renderer>();
        var st = Random.state;
        Random.InitState(20260905);
        //  (x, z, 満開の 高さ, 首の 向き)
        // ★向きは **0が 南**（素の 板の 法線が -Z）。はじめ 180前後で 書いたら
        //   花が ぜんぶ 裏を 向き、画面には 萼の 緑の 円ばかりが 出た（2026-09-05）
        // ★z は **畝の 上**（UNE_Z）に そろえる。畝の あいだに 生えて いると 畑に 見えない
        var oki = new[] {
            new[] { -0.72f, -0.68f, 1.62f,   6f }, new[] { -0.14f, -0.62f, 1.86f,  20f },
            new[] {  0.46f, -0.70f, 1.70f,  -8f }, new[] {  0.80f, -0.64f, 1.52f,  16f },
            new[] { -0.62f,  0.42f, 2.02f,   0f }, new[] {  0.06f,  0.48f, 1.94f,  12f },
            new[] {  0.72f,  0.40f, 1.78f, -12f },
        };
        foreach (var o in oki) {
            var k = new GameObject("Hanadan_Himawari").transform;
            k.SetParent(t, false);
            k.localPosition = new Vector3(o[0], 0.15f, o[1]);      // 畝の 天（0.20）より 少し 下
            kabu.Add(k);
            Kabu1(k, o[2], o[3], hana, tsubomi);
        }
        Random.state = st;

        var hw = g.AddComponent<NiwaHimawari>();
        hw.kabu = kabu.ToArray();
        hw.hana = hana.ToArray();
        hw.tsubomi = tsubomi.ToArray();
    }

    /// <summary>ヒマワリ 1株。茎・葉・花（と つぼみ）・支柱</summary>
    static void Kabu1(Transform k, float h, float yaw, List<Renderer> hana, List<Renderer> tsubomi) {
        var saki = new Vector3(Random.Range(-0.06f, 0.06f), h, Random.Range(-0.05f, 0.05f));
        NiwaBuhin.Bou(k, "Kuki", Vector3.zero, saki * 0.5f, 0.022f, mKuki);
        NiwaBuhin.Bou(k, "Kuki", saki * 0.5f, saki, 0.017f, mKuki);
        // 支柱（竹）と 結び（麻ひも）
        var shi = new Vector3(0.075f, 0f, 0.04f);
        NiwaBuhin.Bou(k, "Shichu", shi - Vector3.up * 0.05f, shi + Vector3.up * (h * 0.72f), 0.012f, mTake);
        foreach (float u in new[] { 0.34f, 0.62f })
            NiwaBuhin.Hako(k, "Musubi", Vector3.Lerp(Vector3.zero, saki, u) + shi * 0.5f + Vector3.up * 0.01f,
                           new Vector3(0.10f, 0.014f, 0.05f),
                           Iro("MizuHimo", new Color(0.66f, 0.60f, 0.44f), 0.05f));
        // 葉（法線は ぜんぶ 上。板の 法線を 横向きに すると 片面が まっ黒に なる）
        for (int i = 0; i < 4; i++) {
            float u = 0.26f + i * 0.17f;
            float w = Mathf.Lerp(0.50f, 0.30f, i / 3f);   // ヒマワリの 葉は 大きい（実物 20〜30cm）
            var go = NiwaBuhin.Mesh1(k, "Ha", HaMesh("HimawariHa", w, w * 1.05f), mHa, false);
            go.transform.localPosition = Vector3.Lerp(Vector3.zero, saki, u);
            go.transform.localRotation = Quaternion.Euler(Random.Range(24f, 44f), i * 97f + Random.Range(-20f, 20f), 0f);
        }
        // 花（咲く まえは つぼみ）。首は 東〜南を 向いて 少し うつむく
        var kubi = Quaternion.Euler(Random.Range(-24f, -9f), yaw, 0f);
        var hn = NiwaBuhin.Mesh1(k, "Hana", HaMesh("HimawariHana", 0.34f, 0.34f, true), mHana, false);
        hn.transform.localPosition = saki + Vector3.up * 0.03f;
        hn.transform.localRotation = kubi;
        hana.Add(hn.GetComponent<Renderer>());
        // 萼（うしろから 見た とき 花の 裏が 見えない よう 緑の 板を 重ねる）
        var gk = NiwaBuhin.Mesh1(k, "Gaku", HaMesh("HimawariGaku", 0.30f, 0.30f, true), mGaku, false);
        gk.transform.localPosition = saki + Vector3.up * 0.03f + kubi * new Vector3(0f, 0f, 0.014f);
        gk.transform.localRotation = kubi;
        hana.Add(gk.GetComponent<Renderer>());
        var tb = NiwaBuhin.Mesh1(k, "Tsubomi", HaMesh("HimawariTsubomi", 0.16f, 0.16f, true), mTsubomi, false);
        tb.transform.localPosition = saki + Vector3.up * 0.02f;
        tb.transform.localRotation = Quaternion.Euler(-6f, yaw, 0f);
        tsubomi.Add(tb.GetComponent<Renderer>());
    }

    /// <summary>板 1枚。naka=true なら まん中が 原点（花）、false なら 下ばが 原点（葉）。
    /// **法線は ぜんぶ 上**（横向きの 法線だと 斜めの 日ざしで 片面が まっ黒に なる）</summary>
    static readonly Dictionary<string, Mesh> haCache = new Dictionary<string, Mesh>();
    static Mesh HaMesh(string name, float w, float h, bool naka = false) {
        string key = name + w.ToString("F3") + h.ToString("F3");
        Mesh got;
        if (haCache.TryGetValue(key, out got)) return got;
        float y0 = naka ? -h * 0.5f : 0f, y1 = naka ? h * 0.5f : h;
        var m = new Mesh { name = name };
        m.vertices = new[] { new Vector3(-w * 0.5f, y0, 0f), new Vector3(w * 0.5f, y0, 0f),
                             new Vector3(w * 0.5f, y1, 0f), new Vector3(-w * 0.5f, y1, 0f) };
        m.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
        m.triangles = new[] { 0, 2, 1, 0, 3, 2 };     // 素の Quad と 同じく -Z を 向く
        var up = naka ? new Vector3(0f, 0.55f, -0.84f).normalized : Vector3.up;
        m.normals = new[] { up, up, up, up };
        m.RecalculateTangents(); m.RecalculateBounds();
        haCache[key] = m;
        return m;
    }
}
