using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// 庭の 母屋（2026-09-01・4版目）。**1つの かたまりの 左下を 長方形に くりぬいた 形**の 2階建て。
//
// ★本人「二階建てにした方がいいかな。玄関の部分だけ一階しかなくて他が二階あるイメージ。
//   後は、箱が2つくっつくような形にしてるけどそうじゃなくて…家に対して、その左下を
//   長方形型にくり抜いた状態みたいな形だね」
//
//   → 前の 版は **箱を 2つ 貼りあわせて** いた（本屋＋突出部が それぞれ 別の 屋根を もつ）。
//     そうでは なく **1つの 四角から 南西の かどを 切りかいた** 形に する。
//     ちがいが 出る ところ：
//       ・**東の 壁が z=-5.4 から 3.6 まで 通しで 立つ**（切りかきの 反対がわ）
//       ・玄関の 屋根は 独立した 屋根では なく、母屋の 壁に とりつく **下屋**
//     これで 2つの 箱では なく「1つの 家に くぼみが ある」ように 読める。
//
// ★階数：**母屋は 2階建て、玄関（下屋）だけ 平屋**。
//   昭和の 家の 2階は 天井が 低い（2.1〜2.2m）。1階2.35m＋2階2.15m で 軒げた 5.10m、
//   棟は 地面から 6.35m。画面に 入る 高さは h < 3.30 + 0.114*d なので d>26.8m 要る＝
//   主人公が 庭に いる とき 棟が すこし 切れる。2階建ては そういう もの
//
// ★調べた こと（3版目から 引きつぐ）
//   ・縁側（廊下）＝**3尺 0.91m**。1.2m以上は「広縁」で 格上に なる
//     suumo.jp/article/oyakudachi/oyaku/ms_shinchiku/ms_knowhow/ken/ ／
//     gaiheki-katorihome.com/engawasunpouicheikijunnokanzengaido.html
//   ・農家の 平面は 広間型と 田の字型。4室の 連絡に 廊下を つかわず 連続配置
//     kominkai.net/nouka-madori/
//   ・L字の 家は 実在（曲り家・中門造）。ja.wikipedia.org/wiki/曲り家 ／
//     ugomachi.jp/tyumondukuri/
//   ・南面の 開口は **部屋ごとに 分節**する（全長を 建具に しない）
//   ・外から **ガラスの 引き戸 → 廊下 → 障子 → 座敷**（障子は 紙。雨に さらせない）
public static class NiwaIe {
    const string TEX = "Assets/Art/Textures/";
    const string DIR = "Assets/Art/Materials/Niwa";

    // ---- 寸法（尺モジュール。1間＝1.8m）
    // 外まわりの 四角： x -5.4..5.4（10.8m）× z -5.4..3.6（9.0m）
    // そこから **南西の かど（x -5.4..0.9, z -5.4..-1.8）を 切りかく**
    public const float X0 = -5.4f, X1 = 5.4f;
    public const float ZN = 3.6f;          // いちばん 北（うしろの 壁）
    public const float ZM = -1.8f;         // 母屋の 南の 壁＝切りかきの 線
    public const float ZS = -5.4f;         // 玄関（下屋）の 南の はし
    public const float KX = 0.9f;          // 切りかきの 東の はし＝玄関の 西の 壁

    public const float YUKA = 0.45f;       // 1階の 床
    public const float H1 = 2.35f;         // 1階の 天井高
    public const float DOSHI = YUKA + H1;  // 胴差（2階の 床）2.80
    public const float H2 = 2.15f;         // 2階の 天井高（昭和の 2階は 低い）
    public const float NOKI = DOSHI + H2;  // 2階の 軒げた 4.95
    public const float GNOKI = DOSHI - 0.1f; // 下屋の 軒げた（1階の 上に とりつく）
    public const float ROUKA = 0.91f;      // 廊下＝3尺
    const float HASHIRA = 0.135f;
    const float KEN = 1.8f;
    /// <summary>玄関の 面（庭から 入る ところ）。とびいしを ここまで つなぐ</summary>
    public const float MINAMI = ZS;
    public const float GENKAN_X = (KX + X1) * 0.5f;

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
    // 貼りかたが 1つだと 10mの 壁と 0.6mの 柱で 絵の こまかさが 15倍 ちがう）
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
    static Material mKiM, mIshi, mGarasu;
    static Color koshiIro;

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

    /// <summary>土壁＋腰の 下見板の 面（真壁づくり）。
    /// 腰板は **柱より ぐっと 暗く**（柿渋や すすで 黒に 近い 焦茶）。同じ 明るさだと
    /// 板の すじが 見えて いても 暗い かたまりに しか 読めない</summary>
    static void Menkabe(string nm, float x0, float x1, float z0, float z1,
                        float yBottom, float yTop) {
        float koshi = Mathf.Min(yBottom + 0.9f, yTop);
        Kabe(nm + "_Koshi", x0, x1, z0, z1, yBottom, koshi, "wood_beam.png", 0.88f,
             koshiIro, "Koshi");
        if (yTop > koshi + 0.01f)
            Kabe(nm + "_Kabe", x0, x1, z0, z1, koshi, yTop, "plaster_wall.png", 0.96f,
                 Color.white, "Kabe");
    }

    /// <summary>ガラスの 引き戸 1くぎり（腰板・ガラス・木の 桟・鴨居・敷居・小壁）</summary>
    static void GarasuDo(string nm, float x0, float x1, float z, float yFloor, float yTop) {
        Kabe(nm + "_Koshi", x0, x1, z - 0.06f, z + 0.06f, yFloor - 0.06f, yFloor + 0.32f,
             "wood_beam.png", 0.88f, koshiIro, "Koshi");
        for (float x = x0; x < x1 - 0.01f; x += 0.9f) {
            float xe = Mathf.Min(x + 0.9f, x1);
            Box(nm + "_Garasu", new Vector3((x + xe) * 0.5f, yFloor + 1.10f, z),
                new Vector3(xe - x - 0.07f, 1.50f, 0.05f), mGarasu);
            Box(nm + "_SashTate", new Vector3(xe, yFloor + 1.10f, z - 0.05f),
                new Vector3(0.06f, 1.58f, 0.07f), mKiM);
        }
        Box(nm + "_Kamoi", new Vector3((x0 + x1) * 0.5f, yFloor + 1.87f, z - 0.05f),
            new Vector3(x1 - x0, 0.10f, 0.09f), mKiM);
        Box(nm + "_Shikii", new Vector3((x0 + x1) * 0.5f, yFloor + 0.34f, z - 0.05f),
            new Vector3(x1 - x0, 0.09f, 0.09f), mKiM);
        if (yTop > yFloor + 1.92f)
            Kabe(nm + "_Kokabe", x0, x1, z - 0.06f, z + 0.06f, yFloor + 1.92f, yTop,
                 "plaster_wall.png", 0.96f, Color.white, "Kabe");
    }

    /// <summary>2階の まど（腰高。ガラス＋木の 桟）</summary>
    static void Mado2(string nm, float x0, float x1, float z) {
        float y0 = DOSHI + 0.75f, y1 = DOSHI + 1.95f;
        Box(nm + "_Garasu", new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, z),
            new Vector3(x1 - x0, y1 - y0, 0.05f), mGarasu);
        Box(nm + "_WakuU", new Vector3((x0 + x1) * 0.5f, y1 + 0.05f, z - 0.05f),
            new Vector3(x1 - x0 + 0.16f, 0.10f, 0.08f), mKiM);
        Box(nm + "_WakuD", new Vector3((x0 + x1) * 0.5f, y0 - 0.05f, z - 0.05f),
            new Vector3(x1 - x0 + 0.16f, 0.10f, 0.08f), mKiM);
        for (float x = x0; x <= x1 + 0.01f; x += (x1 - x0) * 0.5f)
            Box(nm + "_WakuT", new Vector3(x, (y0 + y1) * 0.5f, z - 0.05f),
                new Vector3(0.07f, y1 - y0, 0.08f), mKiM);
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

        // ========== 土台と 床
        Kabe("Ie_Ishiba", X0 - 0.1f, X1 + 0.1f, ZM - 0.1f, ZN + 0.1f, 0f, YUKA - 0.06f,
             "stone.png", 0.95f, Color.white, "Ishi");
        Kabe("Ie_IshibaG", KX - 0.1f, X1 + 0.1f, ZS - 0.1f, ZM, 0f, 0.16f,
             "stone.png", 0.95f, Color.white, "Ishi");
        // 母屋 1階：南の 3尺が 廊下、その おくが 座敷
        Kabe("Ie_Rouka", X0, X1, ZM, ZM + ROUKA, YUKA - 0.05f, YUKA,
             "wood_floor.png", 0.75f, Color.white, "Yuka");
        Kabe("Ie_Tatami", X0, X1, ZM + ROUKA, ZN, YUKA - 0.06f, YUKA, "tatami.png", 0.95f,
             Color.white, "Tatami");
        // 玄関＝土間（床を 上げない）
        Kabe("Ie_Doma", KX, X1, ZS, ZM, 0.02f, 0.12f, "ji_tsuchi.jpg", 1f, Color.white, "Doma");
        // 2階の 床
        Kabe("Ie_Yuka2", X0, X1, ZM, ZN, DOSHI - 0.14f, DOSHI, "wood_floor.png", 0.75f,
             Color.white, "Yuka");

        // ========== 壁
        // ★**東の 壁は 切りかきの 反対がわ なので z=ZS から ZN まで 通しで 立つ**。
        //   ここが 通って いる ことで「箱が 2つ」では なく「1つの 家の くぼみ」に 見える
        Menkabe("Ie_Higashi1", X1 - 0.08f, X1 + 0.08f, ZS, ZN, 0.06f, DOSHI);
        Menkabe("Ie_Higashi2", X1 - 0.08f, X1 + 0.08f, ZM, ZN, DOSHI, NOKI);
        Menkabe("Ie_Kita1", X0, X1, ZN - 0.08f, ZN + 0.08f, YUKA - 0.06f, DOSHI);
        Menkabe("Ie_Kita2", X0, X1, ZN - 0.08f, ZN + 0.08f, DOSHI, NOKI);
        Menkabe("Ie_Nishi1", X0 - 0.08f, X0 + 0.08f, ZM, ZN, YUKA - 0.06f, DOSHI);
        Menkabe("Ie_Nishi2", X0 - 0.08f, X0 + 0.08f, ZM, ZN, DOSHI, NOKI);
        // 切りかきの 内がわの 壁（玄関の 西）
        Menkabe("Ie_Kirikaki", KX - 0.08f, KX + 0.08f, ZS, ZM, 0.06f, GNOKI);

        // 母屋の 南面（切りかきに 面する ところ）＝部屋ごとに 開口を 分節
        //   オモテ(座敷) 3.6m ／ あいだの 壁 0.9m ／ デイ(居間) 4.5m は 玄関の うしろ
        GarasuDo("Ie_Omote", X0, -0.9f, ZM, YUKA, DOSHI);
        Menkabe("Ie_Nakakabe", -0.9f, 0f, ZM - 0.08f, ZM + 0.08f, YUKA - 0.06f, DOSHI);
        GarasuDo("Ie_Dei", 0f, KX, ZM, YUKA, DOSHI);
        Menkabe("Ie_MinamiOku", KX, X1, ZM - 0.08f, ZM + 0.08f, 0.06f, DOSHI);
        // 2階の 南面：まどを 2つ（下屋の 屋根の 上に 出る）
        Menkabe("Ie_Minami2", X0, X1, ZM - 0.08f, ZM + 0.08f, DOSHI, NOKI);
        Mado2("Ie_Mado2a", X0 + 0.9f, X0 + 2.7f, ZM - 0.10f);
        Mado2("Ie_Mado2b", 1.2f, 3.0f, ZM - 0.10f);

        // 障子は 廊下の おく（内がわの しきり）。ガラス戸ごしに 見える
        for (float x = X0; x < KX - 0.01f; x += 0.9f) {
            float xe = Mathf.Min(x + 0.9f, KX);
            Kabe("Ie_Shoji", x + 0.03f, xe - 0.03f, ZM + ROUKA - 0.04f, ZM + ROUKA + 0.04f,
                 YUKA, YUKA + 1.80f, "shoji_paper.png", 0.90f, Color.white, "Shoji");
            Box("Ie_ShojiSan", new Vector3(xe, YUKA + 0.90f, ZM + ROUKA - 0.05f),
                new Vector3(0.05f, 1.80f, 0.05f), mKiM);
        }

        // ========== 玄関（下屋。**平屋**。ここだけ 1階しか ない）
        Menkabe("Ie_GenkanKabe", KX, X1, ZS - 0.07f, ZS + 0.07f, 2.20f, GNOKI);
        Kabe("Ie_GenkanTo", KX + 0.15f, KX + 1.85f, ZS - 0.05f, ZS + 0.05f, 0.12f, 2.20f,
             "wood_beam.png", 0.84f, new Color(0.55f, 0.47f, 0.38f), "Koshi");
        Menkabe("Ie_GenkanWaki", KX + 1.85f, X1, ZS - 0.07f, ZS + 0.07f, 0.06f, 2.20f);
        foreach (float x in new[] { KX, X1 })
            Box("Ie_GenkanHashira", new Vector3(x, 1.25f, ZS),
                new Vector3(0.16f, 2.5f, 0.16f), mKiM);
        // 作業場の 道具（おじちゃんは 陶芸家）
        Box("Ie_Rokuro", new Vector3(KX + 0.9f, 0.45f, ZS + 2.4f),
            new Vector3(0.60f, 0.68f, 0.60f), mKiM);
        Box("Ie_Tsubo", new Vector3(X1 - 0.7f, 0.36f, ZS + 1.2f),
            new Vector3(0.42f, 0.50f, 0.42f), mIshi);

        // 見せる 柱（1間ごと）と 軒げた
        for (float x = X0; x <= X1 + 0.01f; x += KEN) {
            Box("Ie_Hashira_S", new Vector3(x, (YUKA + DOSHI) * 0.5f, ZM - 0.09f),
                new Vector3(HASHIRA, DOSHI - YUKA, HASHIRA), mKiM);
            Box("Ie_Hashira_N", new Vector3(x, (YUKA + DOSHI) * 0.5f, ZN + 0.09f),
                new Vector3(HASHIRA, DOSHI - YUKA, HASHIRA), mKiM);
        }
        Box("Ie_Doshi_S", new Vector3(0f, DOSHI - 0.05f, ZM - 0.10f),
            new Vector3(X1 - X0 + 0.4f, 0.16f, 0.14f), mKiM);
        Box("Ie_Nokigeta_N", new Vector3(0f, NOKI + 0.09f, ZN + 0.09f),
            new Vector3(X1 - X0 + 0.5f, 0.18f, 0.18f), mKiM);

        // ========== 屋根
        const float honyaEave = 0.85f;
        // 母屋＝寄棟。棟は 地面から 6.35m
        var honya = new HouseRoof.Opt {
            ax = (X1 - X0) * 0.5f, az = (ZN - ZM) * 0.5f,
            eave = honyaEave,
            yEave = NOKI + 0.16f,
            rise = 1.25f,
            // 反り(sori)と 軒先の はね上げ(tipLift)は 寺社や 地主の 家の 意匠。ふつうの 家は まっすぐ
            hipRun = 2.0f, tHip = 0.97f, sori = 1.0f, tipLift = 0.02f,
            thick = 0.16f, texM = 1.2f, nx = 12, nz = 8, rings = 11,
        };
        var honyaT = new GameObject("Ie_Honya").transform;
        honyaT.SetParent(ie, false);
        honyaT.localPosition = new Vector3(0f, 0f, (ZM + ZN) * 0.5f);
        HouseRoof.Build(honyaT, honya, mKawaraM, mKiM, null);

        // 玄関の 屋根＝**母屋の 壁に とりつく 下屋**（独立した 屋根に しない）。
        // ★HouseRoof.Shed は zIn < zOut（zが ふえる 向き）で 呼ぶ ことが 前提。
        //   南が -Z の この 家で そのまま 呼ぶと 面が 裏返り、瓦の はずが 軒天の 板に 見える。
        //   180°まわした 子の 中で 組んで 向きを そろえる
        var muki = new GameObject("Ie_Muki").transform;
        muki.SetParent(ie, false);
        muki.localRotation = Quaternion.Euler(0f, 180f, 0f);
        HouseRoof.Shed(muki, "Ie_Geya", -X1 - 0.85f, -KX + 0.85f,
                       -ZM + 0.05f, -ZS + 0.85f,
                       GNOKI + 0.55f, GNOKI + 0.05f, 1.2f, mKawaraM, mKiM);

        // ========== 軒まわりの 造作
        // ★遠くから 家を「家」に 見せるのは 壁の 絵より **軒の 線**。
        //   25m先だと 画面は 73px/m なので、10cmの 樋でも 7px＝ちゃんと 見える。
        //   雨樋・鼻隠し・垂木の 木口は どれも 細いが、**横に 通る 線**として 効く
        {
            float exZ = (ZM + ZN) * 0.5f;                     // 母屋の 屋根の まん中
            float eaveS = exZ - (ZN - ZM) * 0.5f - honyaEave; // 南の 軒先
            float eaveN = exZ + (ZN - ZM) * 0.5f + honyaEave;
            float eaveX = (X1 - X0) * 0.5f + honyaEave;
            float yG = NOKI + 0.16f - 0.24f;                  // 樋の 高さ（屋根の 下）
            // 雨樋（南・北）
            Box("Ie_Toi_S", new Vector3(0f, yG, eaveS + 0.05f),
                new Vector3(eaveX * 2f, 0.11f, 0.12f), mKiM);
            Box("Ie_Toi_N", new Vector3(0f, yG, eaveN - 0.05f),
                new Vector3(eaveX * 2f, 0.11f, 0.12f), mKiM);
            // 竪樋（四すみ。地面まで おろす）
            foreach (float x in new[] { -eaveX + 0.2f, eaveX - 0.2f })
                Box("Ie_Tatedoi", new Vector3(x, yG * 0.5f, eaveS + 0.05f),
                    new Vector3(0.09f, yG, 0.09f), mKiM);
            // 垂木の 木口（南の 軒の 下。45cm ごとの こまかい 影の リズム）
            for (float x = -eaveX + 0.25f; x <= eaveX - 0.24f; x += 0.45f)
                Box("Ie_Taruki", new Vector3(x, NOKI + 0.16f - 0.13f, eaveS + 0.16f),
                    new Vector3(0.07f, 0.09f, 0.34f), mKiM);
        }

        // ========== 雨戸と 戸袋（ガラス戸の 西の はし）。昭和の 家の 顔
        {
            float y0 = YUKA + 0.30f, y1 = YUKA + 1.95f;
            Box("Ie_Tobukuro", new Vector3(X0 - 0.42f, (y0 + y1) * 0.5f, ZM - 0.16f),
                new Vector3(0.84f, y1 - y0, 0.26f),
                Fit("Koshi", "wood_beam.png", 0.84f, y1 - y0, 0.88f, koshiIro));
            Box("Ie_TobukuroYa", new Vector3(X0 - 0.42f, y1 + 0.07f, ZM - 0.16f),
                new Vector3(0.96f, 0.10f, 0.34f), mKiM);
        }

        // ========== くつぬぎ石（玄関の 前）
        Box("Ie_Kutsunugi", new Vector3(KX + 1.0f, 0.14f, ZS - 0.55f),
            new Vector3(1.2f, 0.28f, 0.75f), mIshi);

        Debug.Log(string.Format(
            "[Probe] NiwaIe 2階建て {0}x{1}m の 四角から 南西{2}x{3}m を 切りかき／"
            + "玄関だけ 平屋／軒げた{4:F2}m 棟{5:F2}m",
            X1 - X0, ZN - ZS, KX - X0, ZM - ZS, NOKI, honya.yEave + honya.rise));
    }
}
