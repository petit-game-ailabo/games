using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// 庭の 母屋（2026-09-01）。**ぼくなつ1の 空野家**に 合わせた ふつうの 民家。
//
// ★本人「とんでもない大豪邸になってて、ぼくなつみたいな夏休みの田舎って感じがしない。
//   ぼくなつを調べてみて。1がいいな」
//
// ★調べた こと（ja.wikipedia.org/wiki/ぼくのなつやすみ）
//   ・預けられる さきは **空野家**。おじちゃん（空野優作）は **陶芸家**で 家に **作業場**が ある
//   ・建物は **明治なかごろの 金沢の 民家を 移築した もの**。庄屋の 大農家では ない
//   ・**縁側**が 舞台装置（従妹が 夜に クラリネットを 練習する 場所）
//   ・1975年（昭和50年）。モデルは 山梨県 道志村 月夜野
//   ・森と 山に かこまれ、家の 前に **小さな 木の 橋**が かかる 清流
//
// ★前の 版は BuildHouse（箱の村むけの 大農家 24x12m＝288平米・2階建て・中廊下・
//   土間だけで 90平米）を そのまま つないで いた。桁行13間は **庄屋クラス**で、
//   9歳が 夏を すごす 親戚の 家では ない。**作り直した**。
//   BuildHouse は 寸法が const で BuildZashiki（凍結ずみ）と 共有な ので さわらない。
//   価値の ある `HouseRoof`（入母屋の 屋根。軒の出・反り・隅棟が 数値で 決まる）だけ 借りて、
//   胴は ここで 組む。
//
// ★寸法：桁行 10.8m（6間）x 梁間 7.2m（4間）＝78平米の 平屋。
//   明治期の 地方の 民家の ふつうの 大きさ。棟は 地面から 4.9m。
//   木や 前の 家で つかった 式（画面に 入る 高さ h < 3.30 + 0.114*d）から、
//   縁側を z=4 に 置けば 主人公が 庭に いる とき d=25m で 棟まで 入る
public static class NiwaIe {
    const string TEX = "Assets/Art/Textures/";
    const string DIR = "Assets/Art/Materials/Niwa";

    // ---- 寸法（家の まん中を 原点に、南＝-Z）
    public const float AX = 5.4f;          // 桁行の 半分（10.8m＝6間）
    public const float AZ = 3.6f;          // 梁間の 半分（7.2m＝4間）
    public const float YUKA = 0.45f;       // 床の 高さ（民家は 床を 上げる）
    public const float NOKI = 3.15f;       // 軒げたの 高さ
    public const float ENGAWA = 1.25f;     // 縁側の 出
    public const float DOMA_X = 2.0f;      // これより 東（+X）が 土間
    const float KOSHI = 0.95f;             // 腰壁（下見板）の 天
    const float HASHIRA = 0.135f;          // 見せる 柱の ふとさ
    const float KEN = 1.8f;                // 柱の 間かく＝1間
    /// <summary>縁側の いちばん 南の はし（庭がわ）。とびいしを ここまで つなぐ</summary>
    public const float MINAMI = -AZ - ENGAWA;

    static Material Mat(string name, string tex, Vector2 tiling, float rough, Color tint) {
        System.IO.Directory.CreateDirectory(DIR);
        string path = DIR + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        // 建物は ディザで 抜ける Lit（家の 裏に まわっても 画面が 壁 1色に ならない）
        var sh = Shader.Find("Natsuyasumi/DitherLit");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
        m.shader = sh;
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX + tex);
        if (t == null) Debug.LogError("[NiwaIe] 絵が ない: " + tex);
        else {
            m.SetTexture("_BaseMap", t); m.SetTextureScale("_BaseMap", tiling);
            m.mainTexture = t; m.mainTextureScale = tiling;
        }
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 1f - rough);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
        return m;
    }

    // ★**大きさの ちがう 面に 同じ 貼りかたを つかっては いけない。**
    //   箱の UVは どの 面も 0〜1 なので、10.8mの 壁と 0.6mの 柱で 絵の こまかさが 18倍 ちがう。
    //   面の 大きさ(m)を わたして 1.5m/まい に そろえる（BuildZashiki で 踏んだ 落とし穴）
    static Dictionary<string, Material> fitCache = new Dictionary<string, Material>();
    static Material Fit(string prefix, string tex, float w, float h, float rough, Color tint) {
        string k = prefix + "_" + Mathf.RoundToInt(w * 20) + "_" + Mathf.RoundToInt(h * 20);
        Material got;
        if (fitCache.TryGetValue(k, out got)) return got;
        got = Mat("IeFit_" + k, tex, new Vector2(w / 1.5f, h / 1.5f), rough, tint);
        fitCache[k] = got;
        return got;
    }

    static Transform ROOT;
    static GameObject Box(string name, Vector3 c, Vector3 s, Material m) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name; go.transform.SetParent(ROOT, false);
        go.transform.localPosition = c; go.transform.localScale = s;
        if (m != null) go.GetComponent<Renderer>().sharedMaterial = m;
        return go;
    }

    /// <summary>xz の 範囲と 上下の y から 板を 置く（面の 大きさで 貼りかたを 決める）</summary>
    static GameObject Kabe(string name, float x0, float x1, float z0, float z1,
                           float y0, float y1, string tex, float rough, Color tint,
                           string fitKey) {
        var c = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f);
        var s = new Vector3(x1 - x0, y1 - y0, z1 - z0);
        float w = Mathf.Max(s.x, s.z);
        return Box(name, c, s, Fit(fitKey, tex, w, s.y, rough, tint));
    }

    public static void Build(Transform ie) {
        ROOT = ie;
        fitCache.Clear();
        var mKawara = Mat("IeKawara", "roof_tile.png", new Vector2(6f, 3f), 0.86f, Color.white);
        var mKawaraM = Mat("IeKawaraMesh", "roof_tile.png", Vector2.one, 0.86f, Color.white);
        var mKiM = Mat("IeKiMesh", "wood_beam.png", Vector2.one, 0.80f, Color.white);
        var mYuka = Mat("IeYuka", "wood_floor.png", new Vector2(6f, 4f), 0.75f, Color.white);
        var mTatami = Mat("IeTatami", "tatami.png", new Vector2(6f, 4f), 0.95f, Color.white);
        var mDoma = Mat("IeDoma", "ji_tsuchi.jpg", new Vector2(3f, 2f), 1f, Color.white);
        var mIshi = Mat("IeIshi", "stone.png", new Vector2(3f, 1.4f), 0.95f, Color.white);
        var mShoji = Mat("IeShoji", "shoji_paper.png", new Vector2(2f, 2f), 0.90f, Color.white);
        // 腰の 下見板は **柱より ぐっと 暗く**（柿渋や すすで 黒に 近い 焦茶に なる）。
        // 同じ 明るさだと 板の すじが 見えて いても 暗い かたまりに しか 読めない
        var koshiIro = new Color(0.46f, 0.40f, 0.34f);

        // ---------- 石場だて（床下の 石と 土台）
        Kabe("Ie_Ishiba", -AX - 0.1f, AX + 0.1f, -AZ - 0.1f, AZ + 0.1f, 0f, YUKA - 0.06f,
             "stone.png", 0.95f, Color.white, "Ishi");

        // ---------- 床（座敷＝畳／土間＝土。土間は 床を 上げない）
        Kabe("Ie_Tatami", -AX, DOMA_X, -AZ, AZ, YUKA - 0.06f, YUKA, "tatami.png", 0.95f,
             Color.white, "Tatami");
        Kabe("Ie_Doma", DOMA_X, AX, -AZ, AZ, 0.02f, 0.10f, "ji_tsuchi.jpg", 1f,
             Color.white, "Doma");
        // 縁側（南の そとがわ。ぼくなつ1で 従妹が クラリネットを 吹く ところ）
        Kabe("Ie_Engawa", -AX, DOMA_X, -AZ - ENGAWA, -AZ, YUKA - 0.05f, YUKA,
             "wood_floor.png", 0.75f, Color.white, "Yuka");
        Box("Ie_EngawaHari", new Vector3((-AX + DOMA_X) * 0.5f, YUKA - 0.12f, -AZ - ENGAWA + 0.06f),
            new Vector3(DOMA_X + AX, 0.14f, 0.12f), mKiM);

        // ---------- 壁（真壁づくり＝柱と 貫が 外に 見える）
        // おく（北）と 東西は 土壁＋腰の 下見板。南は 障子＋土間の 入口
        void Menkabe(string nm, float x0, float x1, float z0, float z1) {
            Kabe(nm + "_Koshi", x0, x1, z0, z1, YUKA - 0.06f, KOSHI, "wood_beam.png", 0.88f,
                 koshiIro, "Koshi");
            Kabe(nm + "_Kabe", x0, x1, z0, z1, KOSHI, NOKI, "plaster_wall.png", 0.96f,
                 Color.white, "Kabe");
        }
        Menkabe("Ie_Kita", -AX, AX, AZ - 0.08f, AZ + 0.08f);
        Menkabe("Ie_Nishi", -AX - 0.08f, -AX + 0.08f, -AZ, AZ);
        Menkabe("Ie_Higashi", AX - 0.08f, AX + 0.08f, -AZ, AZ);

        // 南＝**障子の ならび**（座敷がわ）。腰板の 上に 障子、その 上に 小壁
        Kabe("Ie_MinamiKoshi", -AX, DOMA_X, -AZ - 0.06f, -AZ + 0.06f, YUKA - 0.06f, YUKA + 0.28f,
             "wood_beam.png", 0.88f, koshiIro, "Koshi");
        for (float x = -AX; x < DOMA_X - 0.01f; x += 0.9f) {
            float x1 = Mathf.Min(x + 0.9f, DOMA_X);
            Kabe("Ie_Shoji", x + 0.03f, x1 - 0.03f, -AZ - 0.05f, -AZ + 0.05f,
                 YUKA + 0.28f, YUKA + 2.08f, "shoji_paper.png", 0.90f, Color.white, "Shoji");
            // 障子の 桟（たて）
            Box("Ie_ShojiSan", new Vector3(x1, YUKA + 1.18f, -AZ - 0.07f),
                new Vector3(0.05f, 1.80f, 0.05f), mKiM);
        }
        Kabe("Ie_MinamiKokabe", -AX, DOMA_X, -AZ - 0.06f, -AZ + 0.06f, YUKA + 2.08f, NOKI,
             "plaster_wall.png", 0.96f, Color.white, "Kabe");
        // 土間の 入口（引き戸を 半分 あける）
        Kabe("Ie_DomaKabe", DOMA_X, AX, -AZ - 0.06f, -AZ + 0.06f, 2.10f, NOKI,
             "plaster_wall.png", 0.96f, Color.white, "Kabe");
        Kabe("Ie_DomaTo", DOMA_X + 0.05f, DOMA_X + 1.7f, -AZ - 0.05f, -AZ + 0.05f, 0.10f, 2.10f,
             "wood_beam.png", 0.84f, new Color(0.55f, 0.47f, 0.38f), "Koshi");
        Box("Ie_DomaHashira", new Vector3(AX - 0.09f, 1.6f, -AZ),
            new Vector3(0.16f, 3.0f, 0.16f), mKiM);

        // 見せる 柱（1間ごと）と 軒げた
        for (float x = -AX; x <= AX + 0.01f; x += KEN) {
            Box("Ie_Hashira_S", new Vector3(x, (YUKA + NOKI) * 0.5f, -AZ - 0.09f),
                new Vector3(HASHIRA, NOKI - YUKA + 0.4f, HASHIRA), mKiM);
            Box("Ie_Hashira_N", new Vector3(x, (YUKA + NOKI) * 0.5f, AZ + 0.09f),
                new Vector3(HASHIRA, NOKI - YUKA + 0.4f, HASHIRA), mKiM);
        }
        Box("Ie_Nokigeta_S", new Vector3(0f, NOKI + 0.09f, -AZ - 0.09f),
            new Vector3(AX * 2f + 0.5f, 0.18f, 0.18f), mKiM);
        Box("Ie_Nokigeta_N", new Vector3(0f, NOKI + 0.09f, AZ + 0.09f),
            new Vector3(AX * 2f + 0.5f, 0.18f, 0.18f), mKiM);

        // ---------- 屋根（入母屋。HouseRoof が 軒の出・反り・隅棟を 面倒 みる）
        var opt = new HouseRoof.Opt {
            ax = AX, az = AZ,
            eave = 0.95f,          // 軒の 出。日本家屋の 見えかたは ここの 影で 決まる
            yEave = NOKI + 0.18f,
            rise = 1.55f,          // 棟は 地面から 4.88m
            hipRun = 1.30f, tHip = 0.46f, sori = 1.30f, tipLift = 0.15f,
            thick = 0.18f, texM = 1.2f,
            nx = 12, nz = 8, rings = 11,
        };
        HouseRoof.Build(ie, opt, mKawaraM, mKiM, null);

        // 縁側の 上の 庇（下屋）。深い 軒が 縁側に 影を 落とす
        // ★HouseRoof.Shed は **zIn < zOut（z が ふえる 向き）**で 呼ぶ ことを 前提に
        //   面の まわりを 決めて いる（BuildHouse は 縁側が +Z がわ）。
        //   この 家は 南が -Z なので そのまま 呼ぶと **面が 裏返り、瓦の はずが
        //   軒天の 板に 見える**（HouseRoof.cs の 警告どおりの 失敗を した）。
        //   180°まわした 子の 中で 組んで、向きを そろえる
        var muki = new GameObject("Ie_Muki").transform;
        muki.SetParent(ie, false);
        muki.localRotation = Quaternion.Euler(0f, 180f, 0f);
        HouseRoof.Shed(muki, "Ie_Hisashi", -DOMA_X - 0.2f, AX + 0.2f,
                       AZ + 0.05f, AZ + ENGAWA + 0.55f,
                       YUKA + 2.32f, YUKA + 1.92f, 1.2f, mKawaraM, mKiM);

        // ---------- 作業場（おじちゃんは 陶芸家。ぼくなつ1の 設定）
        {
            const float SX0 = -AX - 3.4f, SX1 = -AX - 0.15f, SZ0 = -1.6f, SZ1 = 2.2f;
            const float SY = 2.35f;
            Kabe("Sagyo_Yuka", SX0, SX1, SZ0, SZ1, 0.02f, 0.12f, "ji_tsuchi.jpg", 1f,
                 Color.white, "Doma");
            Kabe("Sagyo_Kita", SX0, SX1, SZ1 - 0.07f, SZ1 + 0.07f, 0.1f, SY,
                 "wood_beam.png", 0.88f, koshiIro, "Koshi");
            Kabe("Sagyo_Nishi", SX0 - 0.07f, SX0 + 0.07f, SZ0, SZ1, 0.1f, SY,
                 "wood_beam.png", 0.88f, koshiIro, "Koshi");
            Kabe("Sagyo_Minami", SX0, SX1 - 1.5f, SZ0 - 0.07f, SZ0 + 0.07f, 0.1f, SY,
                 "wood_beam.png", 0.88f, koshiIro, "Koshi");
            for (float x = SX0; x <= SX1 + 0.01f; x += 1.6f)
                Box("Sagyo_Hashira", new Vector3(x, SY * 0.5f, SZ0),
                    new Vector3(0.13f, SY, 0.13f), mKiM);
            HouseRoof.Shed(muki, "Sagyo_Yane", -SX1 - 0.2f, -SX0 + 0.2f, -SZ1 - 0.1f, -SZ0 + 0.75f,
                           SY + 0.75f, SY + 0.05f, 1.2f, mKawaraM, mKiM);
            // ろくろ と 素焼きの つぼ
            Box("Sagyo_Rokuro", new Vector3(SX0 + 1.1f, 0.45f, SZ0 + 1.0f),
                new Vector3(0.62f, 0.7f, 0.62f), mKiM);
            Box("Sagyo_Tsubo", new Vector3(SX1 - 0.7f, 0.36f, SZ1 - 0.6f),
                new Vector3(0.44f, 0.52f, 0.44f), mIshi);
        }

        // ---------- くつぬぎ石（縁側の 前）
        Box("Ie_Kutsunugi", new Vector3(-1.2f, 0.14f, -AZ - ENGAWA - 0.35f),
            new Vector3(1.1f, 0.28f, 0.7f), mIshi);

        if (mKawara == null || mYuka == null || mTatami == null || mDoma == null
            || mShoji == null) Debug.LogWarning("[NiwaIe] 材質の どれかが ない");
        Debug.Log("[Probe] NiwaIe 桁行" + (AX * 2f) + "m x 梁間" + (AZ * 2f) + "m ＝"
                  + (AX * 2f * AZ * 2f).ToString("F0") + "平米の 平屋（棟 "
                  + (opt.yEave + opt.rise).ToString("F2") + "m）");
    }
}
