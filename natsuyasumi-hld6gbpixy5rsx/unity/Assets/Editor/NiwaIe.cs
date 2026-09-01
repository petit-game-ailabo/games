using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// 庭の 母屋（2026-09-01・3版目）。**L字の 民家**（曲り家・中門造の 形）。
//
// ★調べなおした こと（本人「敵対的レビューするつもりで日本の田舎のおじさんの家、
//   あるいは僕夏の要素で家を調べ直して欲しい。長方形である必要はないと思ってる」）
//
//   ・**縁側の はば**：在来工法の 基本グリッドは 910mm＝3尺＝半間。
//     廊下・通路として つかう 縁側の 奥ゆきは **90cm（3尺）が ふつう**で、
//     **120cm（4尺）以上は「広縁」**という 格上の あつかいに なる。
//     → 前の 版は 1.25m ＝ **広縁**に して いた。豪華に 見えて 当然だった。
//     出典 suumo.jp/article/oyakudachi/oyaku/ms_shinchiku/ms_knowhow/ken/ ／
//          gaiheki-katorihome.com/engawasunpouicheikijunnokanzengaido.html
//
//   ・**農家の 平面**：代表は **広間型**と **田の字型（四間取り）**。
//     田の字型は「日本の 住宅の 原点」。部屋は ニワ(土間)・オモテ(座敷)・デイ(居間)・
//     ダイドコ・ナンド。**4室の 連絡に 廊下を つかわず 連続して 配置**する。
//     出典 kominkai.net/nouka-madori/
//
//   ・**長方形で ない 家は 実在する**（本人の 指摘）：
//     **曲り家**＝「長方形平面の 直屋に 対して **L字形平面**の 家屋」。母屋と 厩が L字に
//     一体化。岩手県 南部領 全域、とくに 盛岡・紫波・遠野。18世紀中期まで さかのぼる。
//     屋根は **本屋が 寄棟、突出部が 入母屋 もしくは 寄棟**。
//     出典 ja.wikipedia.org/wiki/曲り家
//     **中門造**も 似た L字だが、**突出部の 先端に 入口（中門口）が ある**のが 決定的な ちがい。
//     出典 ugomachi.jp/tyumondukuri/
//
// ★直した ところ（前の 版の しくじり）
//   1. 廊下 1.25m → **0.91m（3尺）**。1.2m以上は 広縁＝格上
//   2. 南面が 10.8m ぜんぶ ガラス戸 → **部屋ごとに 開口を 分節**し、あいだに 壁を 入れる
//   3. 単純な 長方形 → **L字**（本屋＋南へ 突きだす 土間・作業場。先端に 中門口）
//   4. 入母屋 → **本屋は 寄棟**（曲り家の 本屋の 形）
//
// ★外から **ガラスの 引き戸 → 廊下 → 障子 → 座敷**（障子は 紙。雨に さらせない）
public static class NiwaIe {
    const string TEX = "Assets/Art/Textures/";
    const string DIR = "Assets/Art/Materials/Niwa";

    // ---- 寸法（尺モジュール。1間＝1.8m、半間＝0.9m）
    // 本屋：桁行 9.0m（5間）x 梁間 6.3m（3.5間）
    public const float HX = 4.5f, HZ = 3.15f;
    // 突出部（土間＋作業場）：はば 3.6m（2間）。東がわを 南へ 突きだす
    public const float TX0 = 4.5f, TX1 = 8.1f, TZ0 = -6.3f, TZ1 = 0.6f;
    public const float YUKA = 0.45f;       // 床の 高さ
    public const float NOKI = 2.95f;       // 軒げたの 高さ（床から 2.5m）
    public const float ROUKA = 0.91f;      // 廊下＝**3尺**。1.2m以上は 広縁に なる
    const float HASHIRA = 0.135f;
    const float KEN = 1.8f;                // 柱の 間かく＝1間
    /// <summary>南の 外がわの 面（ガラス戸の 線）。とびいしを ここまで つなぐ</summary>
    public const float MINAMI = -HZ;

    static Material Mat(string name, string tex, Vector2 tiling, float rough, Color tint) {
        System.IO.Directory.CreateDirectory(DIR);
        string path = DIR + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
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

    // 面の 大きさ(m)から 貼りかたを 決める（箱の UVは どの 面も 0〜1 なので、
    // 貼りかたが 1つだと 9mの 壁と 0.6mの 柱で 絵の こまかさが 15倍 ちがう）
    static readonly Dictionary<string, Material> fitCache = new Dictionary<string, Material>();
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

    static GameObject Kabe(string name, float x0, float x1, float z0, float z1,
                           float y0, float y1, string tex, float rough, Color tint, string fitKey) {
        var c = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f);
        var s = new Vector3(x1 - x0, y1 - y0, z1 - z0);
        return Box(name, c, s, Fit(fitKey, tex, Mathf.Max(s.x, s.z), s.y, rough, tint));
    }

    static Material mKiM, mIshi, mGarasu;
    static Color koshiIro;

    /// <summary>土壁＋腰の 下見板の 面（真壁づくり）。
    /// 腰板は **柱より ぐっと 暗く**（柿渋や すすで 黒に 近い 焦茶）。同じ 明るさだと
    /// 板の すじが 見えて いても 暗い かたまりに しか 読めない</summary>
    static void Menkabe(string nm, float x0, float x1, float z0, float z1, float yTop) {
        // 腰板は 土間がわ（床 0.1m）でも すき間が 出ない よう 下まで のばす
        Kabe(nm + "_Koshi", x0, x1, z0, z1, 0.06f, 0.95f, "wood_beam.png", 0.88f,
             koshiIro, "Koshi");
        Kabe(nm + "_Kabe", x0, x1, z0, z1, 0.95f, yTop, "plaster_wall.png", 0.96f,
             Color.white, "Kabe");
    }

    /// <summary>ガラスの 引き戸 1くぎり（腰板・ガラス・木の 桟・鴨居・敷居・小壁）</summary>
    static void GarasuDo(string nm, float x0, float x1, float z, float yTop) {
        Kabe(nm + "_Koshi", x0, x1, z - 0.06f, z + 0.06f, YUKA - 0.06f, YUKA + 0.32f,
             "wood_beam.png", 0.88f, koshiIro, "Koshi");
        for (float x = x0; x < x1 - 0.01f; x += 0.9f) {
            float xe = Mathf.Min(x + 0.9f, x1);
            Box(nm + "_Garasu", new Vector3((x + xe) * 0.5f, YUKA + 1.16f, z),
                new Vector3(xe - x - 0.07f, 1.62f, 0.05f), mGarasu);
            Box(nm + "_SashTate", new Vector3(xe, YUKA + 1.16f, z - 0.05f),
                new Vector3(0.06f, 1.70f, 0.07f), mKiM);
        }
        Box(nm + "_Kamoi", new Vector3((x0 + x1) * 0.5f, YUKA + 1.99f, z - 0.05f),
            new Vector3(x1 - x0, 0.10f, 0.09f), mKiM);
        Box(nm + "_Shikii", new Vector3((x0 + x1) * 0.5f, YUKA + 0.34f, z - 0.05f),
            new Vector3(x1 - x0, 0.09f, 0.09f), mKiM);
        Kabe(nm + "_Kokabe", x0, x1, z - 0.06f, z + 0.06f, YUKA + 2.04f, yTop,
             "plaster_wall.png", 0.96f, Color.white, "Kabe");
    }

    public static void Build(Transform ie) {
        ROOT = ie;
        fitCache.Clear();
        var mKawaraM = Mat("IeKawaraMesh", "roof_tile.png", Vector2.one, 0.86f, Color.white);
        mKiM = Mat("IeKiMesh", "wood_beam.png", Vector2.one, 0.80f, Color.white);
        mIshi = Mat("IeIshi", "stone.png", new Vector2(3f, 1.4f), 0.95f, Color.white);
        koshiIro = new Color(0.46f, 0.40f, 0.34f);
        mGarasu = Mat("IeGarasu", "shoji_paper.png", Vector2.one, 0.25f,
                      new Color(0.78f, 0.86f, 0.88f, 0.42f));
        mGarasu.SetFloat("_Surface", 1f); mGarasu.SetFloat("_Blend", 0f);
        mGarasu.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mGarasu.SetOverrideTag("RenderType", "Transparent");
        mGarasu.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mGarasu.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mGarasu.SetInt("_ZWrite", 0);
        mGarasu.renderQueue = 3000;

        // ========== 本屋（9.0 x 6.3m）
        Kabe("Ie_Ishiba", -HX - 0.1f, HX + 0.1f, -HZ - 0.1f, HZ + 0.1f, 0f, YUKA - 0.06f,
             "stone.png", 0.95f, Color.white, "Ishi");
        // 床：南の 3尺が 廊下、その おくが 座敷（田の字＝部屋の あいだに 廊下を 通さない）
        Kabe("Ie_Rouka", -HX, HX, -HZ, -HZ + ROUKA, YUKA - 0.05f, YUKA,
             "wood_floor.png", 0.75f, Color.white, "Yuka");
        Kabe("Ie_Tatami", -HX, HX, -HZ + ROUKA, HZ, YUKA - 0.06f, YUKA, "tatami.png", 0.95f,
             Color.white, "Tatami");
        Menkabe("Ie_Kita", -HX, HX, HZ - 0.08f, HZ + 0.08f, NOKI);
        Menkabe("Ie_Nishi", -HX - 0.08f, -HX + 0.08f, -HZ, HZ, NOKI);
        Menkabe("Ie_HigashiN", HX - 0.08f, HX + 0.08f, TZ1, HZ, NOKI);

        // 南面＝**部屋ごとに 開口を 分節**する。9mを ぜんぶ 建具に しない
        //   オモテ(座敷) 3.6m ／ あいだの 壁 0.9m ／ デイ(居間) 4.5m
        GarasuDo("Ie_Omote", -HX, -0.9f, -HZ, NOKI);
        Menkabe("Ie_Nakakabe", -0.9f, 0f, -HZ - 0.08f, -HZ + 0.08f, NOKI);
        GarasuDo("Ie_Dei", 0f, HX, -HZ, NOKI);
        // 障子は **廊下の おく**（内がわの しきり）。ガラス戸ごしに 見える
        for (float x = -HX; x < HX - 0.01f; x += 0.9f) {
            float xe = Mathf.Min(x + 0.9f, HX);
            Kabe("Ie_Shoji", x + 0.03f, xe - 0.03f, -HZ + ROUKA - 0.04f, -HZ + ROUKA + 0.04f,
                 YUKA, YUKA + 1.80f, "shoji_paper.png", 0.90f, Color.white, "Shoji");
            Box("Ie_ShojiSan", new Vector3(xe, YUKA + 0.90f, -HZ + ROUKA - 0.05f),
                new Vector3(0.05f, 1.80f, 0.05f), mKiM);
        }
        // 見せる 柱（1間ごと）と 軒げた
        for (float x = -HX; x <= HX + 0.01f; x += KEN) {
            Box("Ie_Hashira_S", new Vector3(x, (YUKA + NOKI) * 0.5f, -HZ - 0.09f),
                new Vector3(HASHIRA, NOKI - YUKA + 0.4f, HASHIRA), mKiM);
            Box("Ie_Hashira_N", new Vector3(x, (YUKA + NOKI) * 0.5f, HZ + 0.09f),
                new Vector3(HASHIRA, NOKI - YUKA + 0.4f, HASHIRA), mKiM);
        }
        Box("Ie_Nokigeta_S", new Vector3(0f, NOKI + 0.09f, -HZ - 0.09f),
            new Vector3(HX * 2f + 0.5f, 0.18f, 0.18f), mKiM);
        Box("Ie_Nokigeta_N", new Vector3(0f, NOKI + 0.09f, HZ + 0.09f),
            new Vector3(HX * 2f + 0.5f, 0.18f, 0.18f), mKiM);

        // ========== 突出部（土間＋作業場）。**先端に 入口＝中門口**（中門造の 形）
        const float TNOKI = 2.55f;
        Kabe("To_Doma", TX0, TX1, TZ0, TZ1, 0.02f, 0.10f, "ji_tsuchi.jpg", 1f, Color.white, "Doma");
        Menkabe("To_Higashi", TX1 - 0.08f, TX1 + 0.08f, TZ0, TZ1, TNOKI);
        Menkabe("To_Nishi", TX0 - 0.08f, TX0 + 0.08f, TZ0, -HZ, TNOKI);
        // 先端（南）の 中門口：引き戸を 半分 あける
        Kabe("To_MonKabe", TX0, TX1, TZ0 - 0.07f, TZ0 + 0.07f, 2.15f, TNOKI,
             "plaster_wall.png", 0.96f, Color.white, "Kabe");
        Kabe("To_MonTo", TX0 + 0.06f, TX0 + 1.75f, TZ0 - 0.05f, TZ0 + 0.05f, 0.10f, 2.15f,
             "wood_beam.png", 0.84f, new Color(0.55f, 0.47f, 0.38f), "Koshi");
        // 引き戸の わきは 壁。ここを 作りわすれて **先端に 2mの 穴が あいて いた**
        Menkabe("To_MonWaki", TX0 + 1.75f, TX1, TZ0 - 0.07f, TZ0 + 0.07f, 2.15f);
        foreach (float x in new[] { TX0, TX1 })
            Box("To_MonHashira", new Vector3(x, 1.3f, TZ0), new Vector3(0.16f, 2.6f, 0.16f), mKiM);
        // 作業場の 道具（おじちゃんは 陶芸家）
        Box("To_Rokuro", new Vector3(TX0 + 1.0f, 0.45f, TZ0 + 2.2f),
            new Vector3(0.62f, 0.70f, 0.62f), mKiM);
        Box("To_Tsubo", new Vector3(TX1 - 0.8f, 0.36f, TZ0 + 3.4f),
            new Vector3(0.44f, 0.52f, 0.44f), mIshi);
        Box("To_Tsubo2", new Vector3(TX1 - 0.75f, 0.30f, TZ0 + 1.2f),
            new Vector3(0.36f, 0.44f, 0.36f), mIshi);

        // ========== 屋根。**本屋は 寄棟**（曲り家の 本屋の 形）／突出部は 寄棟の 小屋根
        var honya = new HouseRoof.Opt {
            ax = HX, az = HZ,
            eave = 0.90f,           // 軒の 出。日本家屋の 見えかたは ここの 影で 決まる
            yEave = NOKI + 0.18f,
            rise = 1.35f,           // 棟は 地面から 4.48m
            // ★反り(sori)と 軒先の はね上げ(tipLift)は 寺社や 地主の 家の 意匠。ふつうの 家は まっすぐ
            hipRun = 2.1f, tHip = 0.97f, sori = 1.0f, tipLift = 0.02f,
            thick = 0.17f, texM = 1.2f, nx = 12, nz = 8, rings = 11,
        };
        HouseRoof.Build(ie, honya, mKawaraM, mKiM, null);

        // 突出部の 屋根＝棟が 南北に 走る ので 90°まわした 子の 中で 組む
        var wing = new GameObject("Ie_Tsukidashi").transform;
        wing.SetParent(ie, false);
        wing.localPosition = new Vector3((TX0 + TX1) * 0.5f, 0f, (TZ0 + TZ1) * 0.5f);
        wing.localRotation = Quaternion.Euler(0f, 90f, 0f);
        var tsuki = new HouseRoof.Opt {
            ax = (TZ1 - TZ0) * 0.5f, az = (TX1 - TX0) * 0.5f,
            eave = 0.80f, yEave = TNOKI + 0.16f, rise = 1.05f,
            hipRun = 1.2f, tHip = 0.97f, sori = 1.0f, tipLift = 0.02f,
            thick = 0.15f, texM = 1.2f, nx = 10, nz = 7, rings = 9,
        };
        HouseRoof.Build(wing, tsuki, mKawaraM, mKiM, null);

        // ========== くつぬぎ石（ガラス戸の 前）
        Box("Ie_Kutsunugi", new Vector3(-2.2f, 0.14f, -HZ - 0.55f),
            new Vector3(1.1f, 0.28f, 0.7f), mIshi);

        Debug.Log(string.Format(
            "[Probe] NiwaIe L字 本屋{0}x{1}m＋突出{2}x{3}m 廊下{4}m 棟{5:F2}m",
            HX * 2f, HZ * 2f, TX1 - TX0, -HZ - TZ0, ROUKA, honya.yEave + honya.rise));
    }
}
