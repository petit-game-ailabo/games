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
        // ★法線マップ（本人 2026-09-01「瓦がのっぺりなのも嫌だね」）。
        //   DitherLit は もともと _BumpMap に 対応して いた のに つかって いなかった。
        //   平らな 面に 凹凸の 影が つく＝日の むきで 表情が 変わる。のっぺりの 根本の 直し
        string np = TEX + tex.Substring(0, tex.LastIndexOf('.')) + "_n.png";
        var nt = AssetDatabase.LoadAssetAtPath<Texture2D>(np);
        if (nt != null && m.HasProperty("_BumpMap")) {
            m.SetTexture("_BumpMap", nt);
            m.SetTextureScale("_BumpMap", tiling);
            if (m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", 1.0f);
            m.EnableKeyword("_NORMALMAP");
        } else {
            m.DisableKeyword("_NORMALMAP");
        }
        return m;
    }

    // 面の 大きさ(m)から 貼りかたを 決める（箱の UVは どの 面も 0〜1 なので、
    // 貼りかたが 1つだと 10mの 壁と 0.6mの 柱で 絵の こまかさが 15倍 ちがう）
    static readonly Dictionary<string, Material> fitCache = new Dictionary<string, Material>();
    static Material Fit(string prefix, string tex, float w, float h, float rough, Color tint,
                        float tileM) {
        string k = prefix + "_" + Mathf.RoundToInt(w * 20) + "_" + Mathf.RoundToInt(h * 20);
        Material got;
        if (fitCache.TryGetValue(k, out got)) return got;
        got = Mat("IeFit_" + k, tex, new Vector2(w / tileM, h / tileM), rough, tint);
        fitCache[k] = got;
        return got;
    }

    // ---- 絵と タイルの 大きさ（本人の 写真。実寸から 決めた）
    //   瓦   … 1タイルに 12段。桟瓦の 働き寸法 0.235m → 2.8m
    //   下見板 … 1タイルに 約7まい。板の 働き 0.20m → 1.4m
    //   土壁・障子紙 … 模様が ない ので 大きさは 自由
    const string TX_KAWARA = "shashin/ie_kawara.jpg", TX_KABE = "shashin/ie_kabe.jpg";
    const string TX_SHITAMI = "shashin/ie_shitami.jpg", TX_SHOJI = "shashin/ie_shoji.jpg";
    const string TX_KI = "shashin/ie_ki.jpg";
    // ★古い 家の 材質（2026-09-02・本人の 参考写真）。make_ie_yogore.py で 写真から こしらえる。
    //   本人「引き続ききれいでおしゃれすぎる…家自体が古いので黒しみとか、色あせた感じになるはず」
    const string TX_KABE_Y = "shashin/ie_kabe_yogore.jpg";   // 1階の 漆喰＝黄ばみ＋黒しみ
    const string TX_ITAKABE = "shashin/ie_itakabe.jpg";      // 2階の 色あせた たて板
    const string TX_TOBUKURO = "shashin/ie_tobukuro.jpg";    // 戸袋の 平らな 板（下見板では ない）
    const float TM_ITAKABE = 1.0f, TM_TOBUKURO = 2.5f;
    const float TM_KAWARA = 2.8f, TM_KABE = 2.0f, TM_SHITAMI = 1.4f, TM_SHOJI = 1.5f;
    const float TM_KI = 0.55f;   // 柱・枠の 木（たて目。板 1まいを 切って 90°まわした もの）

    static Transform ROOT;
    static Material mKiM, mIshi, mGarasu, mToi;
    static Color koshiIro;

    static GameObject Box(string name, Vector3 c, Vector3 s, Material m) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name; go.transform.SetParent(ROOT, false);
        go.transform.localPosition = c; go.transform.localScale = s;
        if (m != null) go.GetComponent<Renderer>().sharedMaterial = m;
        return go;
    }

    static GameObject Kabe(string name, float x0, float x1, float z0, float z1,
                           float y0, float y1, string tex, float rough, Color tint,
                           string fitKey, float tileM) {
        var c = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f);
        var s = new Vector3(x1 - x0, y1 - y0, z1 - z0);
        return Box(name, c, s, Fit(fitKey, tex, Mathf.Max(s.x, s.z), s.y, rough, tint, tileM));
    }

    /// <summary>土壁＋腰の 下見板の 面（真壁づくり）。
    /// 腰板は **柱より ぐっと 暗く**（柿渋や すすで 黒に 近い 焦茶）。同じ 明るさだと
    /// 板の すじが 見えて いても 暗い かたまりに しか 読めない</summary>
    static int menKazu;
    /// <summary>土壁＋腰の 下見板の 面（真壁づくり）。
    /// ★汚し（本人 2026-09-01「小綺麗すぎるかも、もっと汚いイメージ」）
    ///   ・**面ごとに 大きな しみ**：絵の 中の たてすじは 2mの タイルに おさまる ので、
    ///     壁 1面を またぐ 汚れに ならない。面ごとに 明るさを ずらして 単調さを こわす
    ///   ・**軒下の 黒ずみ**：雨だれが たまる ところ。上の 0.35mを 暗く する
    ///   ・**2階は 下見板を 高く**（1.45m）。白い 面積が へって 昭和の 家に 近づく</summary>
    /// <param name="koshiH">腰の 下見板の 高さ。**2階は 0**（参考写真の 2階は 腰板が 無く
    /// 全面 モルタル）。1階も 0.55mの 低い 腰だけ。0.9mを 両階に まわして いたら
    /// 濃い 木の 枠と あわさって 家ぜんたいが 暗く「シックで今風」に なって いた</param>
    static void Menkabe(string nm, float x0, float x1, float z0, float z1,
                        float yBottom, float yTop, float koshiH = 0.55f) {
        float koshi = Mathf.Min(yBottom + koshiH, yTop);
        // 面ごとの しみ（0.86〜1.0 の あいだで ばらす）
        float shimi = 0.86f + 0.14f * Mathf.Abs(Mathf.Sin(menKazu * 2.399f));
        menKazu++;
        var iro = new Color(shimi, shimi, shimi);
        Kabe(nm + "_Koshi", x0, x1, z0, z1, yBottom, koshi, TX_SHITAMI, 0.88f,
             iro, "Koshi", TM_SHITAMI);
        if (yTop > koshi + 0.01f) {
            // ★2階（腰板なし）は **色あせた たて板**、1階は 黄ばんだ 漆喰に 黒しみ
            //   （本人の 参考写真 2026-09-02。D-153「2階は 全面モルタル」は この 写真で 上書き）
            bool ita = koshiH <= 0f;
            string tx = ita ? TX_ITAKABE : TX_KABE_Y;
            float tm = ita ? TM_ITAKABE : TM_KABE;
            string fk = ita ? "Ita" : "KabeY";
            // 2階の 板は 軒の 影に 入る うえ 雨だれの 帯も かかる ので、絵より 明るく かけて つりあわせる
            var kabeIro = ita ? new Color(shimi * 1.22f, shimi * 1.18f, shimi * 1.10f)
                              : new Color(shimi * 0.96f, shimi * 0.92f, shimi * 0.84f);
            float sumi = Mathf.Max(koshi, yTop - 0.35f);      // 軒下の 黒ずみ
            Kabe(nm + "_Kabe", x0, x1, z0, z1, koshi, sumi, tx, 0.96f, kabeIro, fk, tm);
            if (sumi < yTop - 0.01f)
                Kabe(nm + "_Amadare", x0, x1, z0, z1, sumi, yTop, tx, 0.96f,
                     new Color(kabeIro.r * 0.62f, kabeIro.g * 0.60f, kabeIro.b * 0.56f), fk, tm);
        }
    }

    /// <summary>ガラスの 引き戸 1くぎり（腰板・ガラス・木の 桟・鴨居・敷居・小壁）</summary>
    static void GarasuDo(string nm, float x0, float x1, float z, float yFloor, float yTop) {
        Kabe(nm + "_Koshi", x0, x1, z - 0.06f, z + 0.06f, yFloor - 0.06f, yFloor + 0.32f,
             TX_SHITAMI, 0.88f, koshiIro, "Koshi", TM_SHITAMI);
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
                 TX_KABE_Y, 0.96f, new Color(0.96f, 0.92f, 0.84f), "KabeY", TM_KABE);
    }

    /// <summary>2階の まど（腰高。ガラス＋木の 桟）</summary>
    /// <param name="tobukuroGawa">戸袋を つける がわ（-1 西・+1 東）</param>
    static void Mado2(string nm, float x0, float x1, float z, float tobukuroGawa) {
        // ★軒に かくれすぎて いた ので 0.3m 下げる（本人 2026-09-02）。上に 壁を 0.6m のこす
        float y0 = DOSHI + 0.45f, y1 = DOSHI + 1.55f;
        Box(nm + "_Garasu", new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, z),
            new Vector3(x1 - x0, y1 - y0, 0.05f), mGarasu);
        Box(nm + "_WakuU", new Vector3((x0 + x1) * 0.5f, y1 + 0.05f, z - 0.05f),
            new Vector3(x1 - x0 + 0.16f, 0.10f, 0.08f), mKiM);
        Box(nm + "_WakuD", new Vector3((x0 + x1) * 0.5f, y0 - 0.05f, z - 0.05f),
            new Vector3(x1 - x0 + 0.16f, 0.10f, 0.08f), mKiM);
        for (float x = x0; x <= x1 + 0.01f; x += (x1 - x0) * 0.5f)
            Box(nm + "_WakuT", new Vector3(x, (y0 + y1) * 0.5f, z - 0.05f),
                new Vector3(0.07f, y1 - y0, 0.08f), mKiM);
        // 雨戸の 戸袋（まどの 外がわ。本人「この辺りに雨戸を格納してるはずでは」）。
        // 壁に とりつく ものなので 浮き検査(NiwaJimen.Uki)は 名まえの Kabetsuki で よける
        float tx0 = tobukuroGawa < 0f ? x0 - 0.88f : x1 + 0.08f;
        float tx1 = tx0 + 0.80f, th = y1 - y0 + 0.16f;
        Box("Ie_Kabetsuki_Tobukuro2", new Vector3((tx0 + tx1) * 0.5f, (y0 + y1) * 0.5f, z - 0.08f),
            new Vector3(0.80f, th, 0.20f),
            Fit("Tobukuro", TX_TOBUKURO, 0.80f, th, 0.80f, Color.white, TM_TOBUKURO));
        Box("Ie_Kabetsuki_Tobukuro2Ya", new Vector3((tx0 + tx1) * 0.5f, y1 + 0.12f, z - 0.08f),
            new Vector3(0.92f, 0.08f, 0.28f), mKiM);
    }

    /// <summary>呼び樋（軒の 樋から 壁の 竪樋へ ななめに もどす 管）</summary>
    static void Yobitoi(string nm, Vector3 a, Vector3 b) {
        var go = Box(nm, (a + b) * 0.5f, new Vector3(0.09f, 0.09f, (b - a).magnitude + 0.06f), mToi);
        go.transform.localRotation = Quaternion.LookRotation(b - a, Vector3.up);
    }

    /// <summary>下屋の 屋根の 面の 高さ（Shed に わたした 数と 同じ 式）</summary>
    static float GeyaY(float z) =>
        Mathf.Lerp(GNOKI + 0.55f, GNOKI + 0.05f, Mathf.InverseLerp(ZM - 0.05f, ZS - 0.85f, z));

    public static void Build(Transform ie) {
        ROOT = ie;
        fitCache.Clear();
        menKazu = 0;
        // ★瓦は 参考写真の とおり **黒っぽい 灰**に（2026-09-02）。灰みどりだと 新しく 見える
        var mKawaraM = Mat("IeKawaraMesh", TX_KAWARA, Vector2.one, 0.86f, new Color(0.80f, 0.80f, 0.82f));
        // 雨樋＝銅の 茶（参考写真）。木の 絵に 茶を 強く かける
        mToi = Mat("IeToi", TX_KI, Vector2.one, 0.60f, new Color(1.45f, 1.12f, 0.90f));   // くすんだ 銅
        // ★柱・枠は **たて目の 木**。下見板を そのまま 貼ると 木目が よこに 走る ので、
        //   板 1まいを 切りだして 90°まわした 絵を つかう。柱は 木より 黒い（本人）
        mKiM = Mat("IeKi", TX_KI, new Vector2(1f / TM_KI, 1f / TM_KI), 0.85f, Color.white);
        // 屋根の メッシュの 木口・軒天は 大きな 面なので 別の 貼りかた
        var mKiYane = Mat("IeKiYane", TX_KI, Vector2.one, 0.85f, Color.white);
        mIshi = Mat("IeIshi", "stone.png", new Vector2(3f, 1.4f), 0.95f, Color.white);
        // ★写真の 下見板は もともと 明るさ42（かなり 暗い）。前の ドット絵むけの
        //   暗い 色補正(0.46,0.40,0.34)を のこすと 18＝まっ黒に なる
        koshiIro = Color.white;
        mGarasu = Mat("IeGarasu", TX_SHOJI, Vector2.one, 0.25f,
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
             "stone.png", 0.95f, Color.white, "Ishi", 1.5f);
        Kabe("Ie_IshibaG", KX - 0.1f, X1 + 0.1f, ZS - 0.1f, ZM, 0f, 0.16f,
             "stone.png", 0.95f, Color.white, "Ishi", 1.5f);
        // 母屋 1階：南の 3尺が 廊下、その おくが 座敷
        Kabe("Ie_Rouka", X0, X1, ZM, ZM + ROUKA, YUKA - 0.05f, YUKA,
             "wood_floor.png", 0.75f, Color.white, "Yuka", 1.5f);
        Kabe("Ie_Tatami", X0, X1, ZM + ROUKA, ZN, YUKA - 0.06f, YUKA, "tatami.png", 0.95f,
             Color.white, "Tatami", 1.5f);
        // 玄関＝土間（床を 上げない）
        Kabe("Ie_Doma", KX, X1, ZS, ZM, 0.02f, 0.12f, "ji_tsuchi.jpg", 1f, Color.white, "Doma", 2.25f);
        // 2階の 床
        Kabe("Ie_Yuka2", X0, X1, ZM, ZN, DOSHI - 0.14f, DOSHI, "wood_floor.png", 0.75f,
             Color.white, "Yuka", 1.5f);

        // ========== 壁
        // ★**東の 壁は 切りかきの 反対がわ なので z=ZS から ZN まで 通しで 立つ**。
        //   ここが 通って いる ことで「箱が 2つ」では なく「1つの 家の くぼみ」に 見える
        Menkabe("Ie_Higashi1", X1 - 0.08f, X1 + 0.08f, ZS, ZN, 0.06f, DOSHI);
        Menkabe("Ie_Higashi2", X1 - 0.08f, X1 + 0.08f, ZM, ZN, DOSHI, NOKI, 0f);
        Menkabe("Ie_Kita1", X0, X1, ZN - 0.08f, ZN + 0.08f, YUKA - 0.06f, DOSHI);
        Menkabe("Ie_Kita2", X0, X1, ZN - 0.08f, ZN + 0.08f, DOSHI, NOKI, 0f);
        Menkabe("Ie_Nishi1", X0 - 0.08f, X0 + 0.08f, ZM, ZN, YUKA - 0.06f, DOSHI);
        Menkabe("Ie_Nishi2", X0 - 0.08f, X0 + 0.08f, ZM, ZN, DOSHI, NOKI, 0f);
        // 切りかきの 内がわの 壁（玄関の 西）
        Menkabe("Ie_Kirikaki", KX - 0.08f, KX + 0.08f, ZS, ZM, 0.06f, GNOKI);

        // 母屋の 南面（切りかきに 面する ところ）＝部屋ごとに 開口を 分節
        //   オモテ(座敷) 3.6m ／ あいだの 壁 0.9m ／ デイ(居間) 4.5m は 玄関の うしろ
        GarasuDo("Ie_Omote", X0 + 0.85f, -0.9f, ZM, YUKA, DOSHI);   // 西の はしは 戸袋
        Menkabe("Ie_Nakakabe", -0.9f, 0f, ZM - 0.08f, ZM + 0.08f, YUKA - 0.06f, DOSHI);
        GarasuDo("Ie_Dei", 0f, KX, ZM, YUKA, DOSHI);
        Menkabe("Ie_MinamiOku", KX, X1, ZM - 0.08f, ZM + 0.08f, 0.06f, DOSHI);
        // 2階の 南面：まどを 2つ（下屋の 屋根の 上に 出る）
        Menkabe("Ie_Minami2", X0, X1, ZM - 0.08f, ZM + 0.08f, DOSHI, NOKI, 0f);
        Mado2("Ie_Mado2a", X0 + 0.9f, X0 + 2.7f, ZM - 0.10f, -1f);
        Mado2("Ie_Mado2b", 1.2f, 3.0f, ZM - 0.10f, +1f);

        // 障子は 廊下の おく（内がわの しきり）。ガラス戸ごしに 見える
        for (float x = X0; x < KX - 0.01f; x += 0.9f) {
            float xe = Mathf.Min(x + 0.9f, KX);
            Kabe("Ie_Shoji", x + 0.03f, xe - 0.03f, ZM + ROUKA - 0.04f, ZM + ROUKA + 0.04f,
                 YUKA, YUKA + 1.80f, TX_SHOJI, 0.90f, Color.white, "Shoji", TM_SHOJI);
            Box("Ie_ShojiSan", new Vector3(xe, YUKA + 0.90f, ZM + ROUKA - 0.05f),
                new Vector3(0.05f, 1.80f, 0.05f), mKiM);
        }

        // ========== 玄関（下屋。**平屋**。ここだけ 1階しか ない）
        Menkabe("Ie_GenkanKabe", KX, X1, ZS - 0.07f, ZS + 0.07f, 2.20f, GNOKI);
        // ★玄関は **ガラスの 引き戸**（本人 2026-09-02「田舎の扉は引き戸のイメージ」）。
        //   板戸を 1まい 立てて いた のを、縁がわと 同じ 建具に そろえる
        GarasuDo("Ie_GenkanTo", KX + 0.15f, KX + 1.85f, ZS, 0.12f, 2.20f);
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
            // ★軒の 出が **水平**だと 軒が 反って 見える（寺社の 反りと 同じ 形）。
            //   本人「二階の屋根、なんか丸み帯びてる」。軒の 出 0.90m x 勾配(1.35/2.7)
            //   ＝0.45m 下げて、流れと 同じ 勾配で つづける
            //   ★tipLift は t*t なので 負に すると **放物線に 垂れて 屋根が 波うつ**
            //     （本人 2026-09-02「屋根がなんかウェーブしてる」）。直線で 下げる eaveDrop を つかう
            hipRun = 2.0f, tHip = 0.97f, sori = 1.0f, tipLift = 0f, eaveDrop = 0.45f,
            thick = 0.16f, texM = TM_KAWARA, nx = 12, nz = 8, rings = 11,
        };
        var honyaT = new GameObject("Ie_Honya").transform;
        honyaT.SetParent(ie, false);
        honyaT.localPosition = new Vector3(0f, 0f, (ZM + ZN) * 0.5f);
        HouseRoof.Build(honyaT, honya, mKawaraM, mKiYane, null);
        // ★瓦は **1まいずつ 置く**（本人 2026-09-01「3Dでやるなら、瓦一枚ずつ
        //   配置してみるしかないんじゃない？」）。法線マップは 面の かたむきを だます だけで、
        //   **軒先の 輪郭は まっすぐな 線の まま**。屋根らしさの 大半は 波うつ 軒先が つくる
        int nKawara = NiwaKawara.Fuku(honyaT, honya, mKawaraM, "Ie_KawaraFuki", TM_KAWARA);


        // 玄関の 屋根＝**母屋の 壁に とりつく 下屋**（独立した 屋根に しない）。
        // ★HouseRoof.Shed は zIn < zOut（zが ふえる 向き）で 呼ぶ ことが 前提。
        //   南が -Z の この 家で そのまま 呼ぶと 面が 裏返り、瓦の はずが 軒天の 板に 見える。
        //   180°まわした 子の 中で 組んで 向きを そろえる
        var muki = new GameObject("Ie_Muki").transform;
        muki.SetParent(ie, false);
        muki.localRotation = Quaternion.Euler(0f, 180f, 0f);
        HouseRoof.Shed(muki, "Ie_Geya", -X1 - 0.85f, -KX + 0.85f,
                       -ZM + 0.05f, -ZS + 0.85f,
                       GNOKI + 0.55f, GNOKI + 0.05f, TM_KAWARA, mKawaraM, mKiYane);
        // 下屋にも 1まいずつ ふく（いちばん 手前に あって 目に つく）
        nKawara += NiwaKawara.Geya(muki, "Ie_GeyaKawara", -X1 - 0.85f, -KX + 0.85f,
                                   -ZM + 0.05f, -ZS + 0.85f,
                                   GNOKI + 0.55f + 0.02f, GNOKI + 0.05f + 0.02f, mKawaraM, TM_KAWARA);
        {   // 軒瓦が 屋根板より 下・外に 出て いるかを **数で** 見はる
            var rg = muki.Find("Ie_Geya").GetComponentInChildren<MeshRenderer>().bounds;
            var rk = muki.Find("Ie_GeyaKawara").GetComponent<MeshRenderer>().bounds;
            Debug.Log("[Probe] nokigawara shita e " + (rg.min.y - rk.min.y).ToString("F3") +
                      "m soto e " + (rg.min.z - rk.min.z).ToString("F3") + "m");
        }

        // ========== 棟瓦と 隅棟
        // ★ここに 置いて いた まっすぐな 箱の 隅棟は **消した**（2026-09-02）。
        //   HouseRoof が 屋根の 式なりに 隅棟(H_Sumimune)と 棟(H_Mune)を 作って いる のに
        //   二重に 置き、しかも 軒先を 0.45m 下げた あとも 箱は 昔の 角の 高さ(yEave+0.10)を
        //   目ざして いた ので、**角で 屋根の 上に 浮いた 棒**に なって いた
        //   （本人「まだ梁が飛び出してる」）

        // ========== 軒まわりの 造作（雨樋・竪樋・垂木）
        // ★遠くから 家を「家」に 見せるのは 壁の 絵より **軒の 線**。
        {
            float exZ = (ZM + ZN) * 0.5f;                     // 母屋の 屋根の まん中
            float eaveS = exZ - (ZN - ZM) * 0.5f - honyaEave; // 南の 軒先
            float eaveN = exZ + (ZN - ZM) * 0.5f + honyaEave;
            float eaveX = (X1 - X0) * 0.5f + honyaEave;
            float yNoki = HouseRoof.Y(honya, -1f);            // 軒先の 高さ（屋根の 式から）
            // ★雨樋は **軒瓦の 先の 下**に つるす（2026-09-02）。前は 軒先の 内がわ z+0.05 に
            //   置いて いた ので、軒瓦を 0.27m 外へ 出した とたん 瓦の うしろに かくれた。
            //   樋・呼び樋は 軒から つるす ものなので 浮き検査は Kabetsuki で よける
            float yG = yNoki - 0.18f;
            Box("Ie_Kabetsuki_Toi_S", new Vector3(0f, yG, eaveS + 0.22f),
                new Vector3(eaveX * 2f + 0.1f, 0.11f, 0.12f), mToi);
            Box("Ie_Kabetsuki_Toi_N", new Vector3(0f, yG, eaveN - 0.22f),
                new Vector3(eaveX * 2f + 0.1f, 0.11f, 0.12f), mToi);
            // 竪樋（西の 角）：呼び樋で 壁まで もどして から 下ろす。
            // ★前に 軒先（壁から 0.9m 外）に 立てて 浮いた（本人「謎の棒」）。壁ぎわに 立てる
            var wa = new Vector3(-eaveX + 0.06f, yG, eaveS + 0.22f);
            var wb = new Vector3(X0 - 0.14f, yG - 0.20f, ZM - 0.14f);
            Yobitoi("Ie_Kabetsuki_Yobitoi_W", wa, wb);
            Box("Ie_Tatetoi_W", new Vector3(wb.x, (wb.y + 0.06f) * 0.5f, wb.z),
                new Vector3(0.09f, wb.y - 0.06f, 0.09f), mToi);
            // 東の はしは 下屋の 屋根の 上へ 落とす（実際の 家でも そう する）
            float xE = eaveX - 0.30f, zE = eaveS + 0.22f;
            float yGe = GeyaY(zE) + 0.04f;
            Box("Ie_Tatetoi_E", new Vector3(xE, (yG + yGe) * 0.5f, zE),
                new Vector3(0.09f, yG - yGe, 0.09f), mToi);
            // 垂木の 木口（南の 軒の 下。45cm ごとの こまかい 影の リズム）。瓦の 先より 内に おさめる
            for (float x = -eaveX + 0.25f; x <= eaveX - 0.24f; x += 0.45f)
                Box("Ie_Taruki", new Vector3(x, yNoki - 0.05f, eaveS + 0.02f),
                    new Vector3(0.07f, 0.09f, 0.34f), mKiM);

            // 下屋の 雨樋と 竪樋（玄関の 角の 柱に そわせて 下ろす）
            float gz = ZS - 0.85f - 0.22f;                    // 下屋の 軒先の 外
            float gy = GNOKI + 0.05f - 0.30f;                 // 軒瓦の 先の 下
            Box("Ie_Kabetsuki_Toi_G", new Vector3(((KX - 0.85f) + (X1 + 0.85f)) * 0.5f, gy, gz),
                new Vector3(X1 - KX + 1.7f + 0.1f, 0.11f, 0.12f), mToi);
            var ga = new Vector3(X1 + 0.80f, gy, gz);
            var gb = new Vector3(X1 + 0.14f, gy - 0.15f, ZS - 0.14f);
            Yobitoi("Ie_Kabetsuki_Yobitoi_G", ga, gb);
            Box("Ie_Tatetoi_G", new Vector3(gb.x, (gb.y + 0.06f) * 0.5f, gb.z),
                new Vector3(0.09f, gb.y - 0.06f, 0.09f), mToi);
        }

        // ========== 雨戸と 戸袋（ガラス戸の 西の はし）。昭和の 家の 顔
        {
            // ★戸袋は **石場まで おろす**。0.75mから 始めて いたら 下に すきまが のこり、
            //   壁より 前へ 出て いる ぶん 下から のぞけて 浮いて 見えた
            float y0 = YUKA - 0.06f, y1 = YUKA + 1.95f;
            // ★浮きの 直し 2回目（2026-09-02）。1回目は X0-0.42＝**壁より 外**に 置いて いた。
            //   内がわ（X0+0.42）へ 移した が、こんどは **戸袋の 下に 何も 無かった**
            //   （ガラス戸の 腰板を X0+0.85 から 始めた ので、その 西の 0.85mが 空洞）。
            //   機械検査（NiwaJimen.Uki）が Ie_Tobukuro を 拾って 分かった。
            //   下の 腰板と 上の 小壁を 足して 壁として つなぐ
            Kabe("Ie_TobukuroShita", X0, X0 + 0.85f, ZM - 0.06f, ZM + 0.06f,
                 YUKA - 0.06f, y0 + 0.02f, TX_SHITAMI, 0.88f, koshiIro, "Koshi", TM_SHITAMI);
            Kabe("Ie_TobukuroUe", X0, X0 + 0.85f, ZM - 0.06f, ZM + 0.06f,
                 y1 + 0.12f, DOSHI, TX_KABE_Y, 0.96f, new Color(0.96f, 0.92f, 0.84f), "KabeY", TM_KABE);
            Box("Ie_Tobukuro", new Vector3(X0 + 0.42f, (y0 + y1) * 0.5f, ZM - 0.13f),
                new Vector3(0.84f, y1 - y0, 0.22f),
                // ★戸袋は 下見板では なく **平らな 板**（本人「雨戸なので、木じゃなくて、もっと違う材質」）
                Fit("Tobukuro", TX_TOBUKURO, 0.84f, y1 - y0, 0.80f, Color.white, TM_TOBUKURO));
            Box("Ie_TobukuroYa", new Vector3(X0 + 0.42f, y1 + 0.07f, ZM - 0.13f),
                new Vector3(0.96f, 0.10f, 0.30f), mKiM);
        }

        // ========== くつぬぎ石（玄関の 前）
        Box("Ie_Kutsunugi", new Vector3(KX + 1.0f, 0.14f, ZS - 0.55f),
            new Vector3(1.2f, 0.28f, 0.75f), mIshi);

        Debug.Log("[Probe] NiwaIe 瓦 " + nKawara + "まい");
        Debug.Log(string.Format(
            "[Probe] NiwaIe 2階建て {0}x{1}m の 四角から 南西{2}x{3}m を 切りかき／"
            + "玄関だけ 平屋／軒げた{4:F2}m 棟{5:F2}m",
            X1 - X0, ZN - ZS, KX - X0, ZM - ZS, NOKI, honya.yEave + honya.rise));
    }
}
