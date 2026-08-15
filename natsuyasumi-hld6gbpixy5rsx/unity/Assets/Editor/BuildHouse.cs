// 田舎の 家を 建てる。
//
// ★調べた こと（2026-08-15）
//  - 農家の 代表的な 間取りは **「田の字型（四間取り）」**。土間を のぞく 床上を
//    4部屋に 割り、ふだんは 襖で 仕切り、**要れば 建具を 外して 大広間に できる**のが 特徴。
//    関西以西に 多く、日本の 農家で いちばん 広く 見られる 形。
//  - もう ひとつが **「広間型」**。土間の となりに 広い 板の間（台所）を とり、
//    その おくに 客間と 寝間を 置く。室町の 末から ある 古い 形。
//  - **土間** は 玄関から そのまま 入る 土のままの 床。かまど・農作業の 場。
//  - **縁側** は 座敷の 外がわに つく 板の えん。庭と 部屋の あいだ。
//
// この 家は 田の字型を とり、土間・縁側・二階を つける。
// **手前の 壁と 屋根は「中に 入ったら 消える」**（RoomCutaway が 面倒を みる）。
using System.Collections.Generic;
using UnityEngine;

public static class BuildHouse {

    public struct Mats {
        public Material tatami, wood, floor, plaster, roof, paper, stone, soil;
    }

    // 間取り（m）。原点は 家の まんなか
    public const float X0 = -5.4f, X1 = 5.4f;     // 横 10.8m
    public const float Z0 = -3.9f, Z1 = 3.9f;     // 奥ゆき 7.8m
    public const float DomaX = 1.8f;              // これより 右が 土間
    public const float MidZ = 0.0f;               // 田の字の 前後の 仕切り
    public const float MidX = -1.8f;              // 田の字の 左右の 仕切り
    public const float F1 = 0f;                   // 1階の 床
    public const float H1 = 2.35f;                // 1階の 高さ
    public const float F2 = H1 + 0.18f;           // 2階の 床
    public const float H2 = 2.05f;                // 2階の 高さ
    public const float EngawaZ = Z1 + 0.9f;       // 縁側の そと ばし
    public const float DoorX = 3.6f;              // 玄関の まん中
    // **玄関は 広く とる。** 1.6m で 引き戸を 半分 立てたら、あいた すきまが
    // 体の 幅(0.52m)と ほぼ 同じに なり、戸の はしに 引っかかって 入れなかった
    public const float DoorW = 2.4f;
    public const float DoorLeaf = 0.9f;          // 立って いる 戸の 幅
    /// <summary>人が 通れる ところの まん中</summary>
    public static float DoorOpenX { get { return DoorX + (DoorW * 0.5f - (DoorW * 0.5f - DoorLeaf)) * 0.5f; } }

    static readonly List<Renderer> front = new List<Renderer>();   // 手前の 壁（入ったら 消す）
    static readonly List<Renderer> roofs = new List<Renderer>();   // 屋根（入ったら 消す）
    static readonly List<Renderer> upper = new List<Renderer>();   // 2階の 床（おくへ 行くと 消す）
    static readonly List<Renderer> midWall = new List<Renderer>(); // まん中の 仕切り（おくへ 行くと 消す）
    // 中の 建具（左右の ふすま）。**家に 入ったら 外す。**
    // 田の字型は もともと「建具を 外すと 大広間に なる」間取り。
    // 立てた ままだと 天井まで ある 白い 壁が 画の まん中を ふさぎ、
    // せっかく 屋根を 抜いても 半分しか 見えなかった
    static readonly List<Renderer> inner = new List<Renderer>();

    public static RoomCutaway.Piece[] Build(Transform root, Mats m,
                                            System.Func<string, Transform, Vector3, Vector3, Material, GameObject> box) {
        front.Clear(); roofs.Clear(); upper.Clear(); midWall.Clear(); inner.Clear();
        var t = root;

        // ---------- 床
        // 田の字の 4部屋＝畳
        box("H_Tatami", t, new Vector3((X0 + DomaX) * 0.5f, F1 - 0.05f, 0f),
            new Vector3(DomaX - X0, 0.1f, Z1 - Z0), m.tatami);
        // 土間（右がわ）。床より 低い 土のまま
        box("H_Doma", t, new Vector3((DomaX + X1) * 0.5f, F1 - 0.30f, 0f),
            new Vector3(X1 - DomaX, 0.1f, Z1 - Z0), m.soil);
        // 縁側
        box("H_Engawa", t, new Vector3((X0 + DomaX) * 0.5f, F1 - 0.04f, (Z1 + EngawaZ) * 0.5f),
            new Vector3(DomaX - X0, 0.12f, EngawaZ - Z1), m.floor);
        // ゆか下
        box("H_Under", t, new Vector3(0f, -0.31f, 0f), new Vector3(X1 - X0 + 0.2f, 0.5f, EngawaZ - Z0 + 0.2f), m.wood);

        // ---------- 外がわの 壁
        // おく（-Z）と 左右は 残す。**手前(+Z)は 玄関の ぶんを あけ、あとは 切りぬきで 消す**
        box("H_WallBack", t, new Vector3(0f, F1 + H1 * 0.5f, Z0), new Vector3(X1 - X0, H1, 0.16f), m.plaster);
        box("H_WallL", t, new Vector3(X0, F1 + H1 * 0.5f, 0f), new Vector3(0.16f, H1, Z1 - Z0), m.plaster);
        box("H_WallR", t, new Vector3(X1, F1 + H1 * 0.5f, 0f), new Vector3(0.16f, H1, Z1 - Z0), m.plaster);

        // 手前の 壁（玄関の 左右）。入ったら 消える
        float lw = (DoorX - DoorW * 0.5f) - DomaX;
        Front(box("H_WallF_L", t, new Vector3((DomaX + DoorX - DoorW * 0.5f) * 0.5f, F1 + H1 * 0.5f, Z1),
                  new Vector3(lw, H1, 0.16f), m.plaster));
        float rw = X1 - (DoorX + DoorW * 0.5f);
        Front(box("H_WallF_R", t, new Vector3((DoorX + DoorW * 0.5f + X1) * 0.5f, F1 + H1 * 0.5f, Z1),
                  new Vector3(rw, H1, 0.16f), m.plaster));
        // 玄関の 上の かもい
        Front(box("H_DoorTop", t, new Vector3(DoorX, F1 + H1 - 0.25f, Z1), new Vector3(DoorW, 0.5f, 0.16f), m.wood));
        // 玄関の 引き戸（半分 あいて いる）
        // 引き戸は 左に よせて 立てる。右がわが まるごと あく
        Front(box("H_Door", t, new Vector3(DoorX - DoorW * 0.5f + DoorLeaf * 0.5f, F1 + 0.9f, Z1 + 0.05f),
                  new Vector3(DoorLeaf, 1.8f, 0.06f), m.paper));
        // 座敷がわの 手前は 障子（縁側に 面する）
        for (int i = 0; i < 4; i++) {
            float w = (DomaX - X0) / 4f;
            Front(box("H_Shoji" + i, t, new Vector3(X0 + w * (i + 0.5f), F1 + 0.95f, Z1),
                      new Vector3(w * 0.96f, 1.9f, 0.06f), m.paper));
        }
        Front(box("H_ShojiTop", t, new Vector3((X0 + DomaX) * 0.5f, F1 + H1 - 0.22f, Z1),
                  new Vector3(DomaX - X0, 0.44f, 0.14f), m.wood));

        // ---------- 中の 仕切り（田の字）
        // まん中の 仕切りは、**おくの 部屋へ 行くと 消える**
        Mid(box("H_Fusuma_Z", t, new Vector3((X0 + DomaX) * 0.5f, F1 + H1 * 0.5f, MidZ),
                new Vector3(DomaX - X0, H1, 0.1f), m.paper));
        Inner(box("H_Fusuma_X", t, new Vector3(MidX, F1 + H1 * 0.5f, (Z0 + Z1) * 0.5f),
                  new Vector3(0.1f, H1, Z1 - Z0), m.paper));
        // 土間と 床上の さかいの 柱
        for (int i = -1; i <= 1; i += 2)
            box("H_PostDoma" + i, t, new Vector3(DomaX, F1 + H1 * 0.5f, i * (Z1 - 0.4f)),
                new Vector3(0.16f, H1, 0.16f), m.wood);

        // ---------- かまど（土間）と ながし（台所）
        box("H_Kamado", t, new Vector3(X1 - 1.1f, F1 - 0.05f, Z0 + 1.2f), new Vector3(1.5f, 0.7f, 1.1f), m.stone);
        box("H_Nagashi", t, new Vector3(X1 - 1.1f, F1 - 0.05f, Z0 + 2.8f), new Vector3(1.4f, 0.7f, 0.8f), m.wood);

        // ---------- 二階への 階段（土間の おく）
        for (int i = 0; i < 9; i++) {
            float y = F1 + 0.13f + i * (F2 - F1) / 9f;
            float z = Z0 + 0.5f + i * 0.28f;
            box("H_Kaidan" + i, t, new Vector3(DomaX + 0.9f, y, z), new Vector3(1.1f, 0.12f, 0.3f), m.floor);
        }

        // ---------- 二階
        // **2階の 床は「1階の おくに いる ときだけ」消す。** そうしないと
        // 1階の おくが 天井で ふさがって 見えない
        Upper(box("H_Floor2", t, new Vector3((X0 + DomaX) * 0.5f, F2, 0f),
                  new Vector3(DomaX - X0, 0.16f, Z1 - Z0), m.floor));
        Upper(box("H_Tatami2", t, new Vector3((X0 + DomaX) * 0.5f, F2 + 0.1f, 0f),
                  new Vector3(DomaX - X0 - 0.2f, 0.06f, Z1 - Z0 - 0.2f), m.tatami));
        // 2階の 壁（おく・左右）と 仕切り。**床と いっしょに 消す**（残すと 宙に 浮く）
        Upper(box("H_W2Back", t, new Vector3((X0 + DomaX) * 0.5f, F2 + H2 * 0.5f, Z0), new Vector3(DomaX - X0, H2, 0.14f), m.plaster));
        Upper(box("H_W2L", t, new Vector3(X0, F2 + H2 * 0.5f, 0f), new Vector3(0.14f, H2, Z1 - Z0), m.plaster));
        Upper(box("H_W2Mid", t, new Vector3(MidX, F2 + H2 * 0.5f, 0f), new Vector3(0.1f, H2, Z1 - Z0), m.paper));
        Front(box("H_W2Front", t, new Vector3((X0 + DomaX) * 0.5f, F2 + H2 * 0.5f, Z1),
                  new Vector3(DomaX - X0, H2, 0.12f), m.paper));

        // ---------- 屋根。入ったら 消える
        float roofY = F2 + H2 + 0.5f;
        for (int i = -1; i <= 1; i += 2) {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = "H_Roof" + i; g.transform.SetParent(t, false);
            g.transform.localPosition = new Vector3(0f, roofY, i * (Z1 - Z0) * 0.27f);
            g.transform.localScale = new Vector3(X1 - X0 + 1.6f, 0.3f, (Z1 - Z0) * 0.62f);
            g.transform.localRotation = Quaternion.Euler(i * 26f, 0f, 0f);
            g.GetComponent<Renderer>().sharedMaterial = m.roof;
            roofs.Add(g.GetComponent<Renderer>());
        }
        // 縁側の ひさし（これも 屋根あつかい）
        var eave = GameObject.CreatePrimitive(PrimitiveType.Cube);
        eave.name = "H_Eave"; eave.transform.SetParent(t, false);
        eave.transform.localPosition = new Vector3((X0 + DomaX) * 0.5f, F1 + H1 + 0.35f, EngawaZ - 0.2f);
        eave.transform.localScale = new Vector3(DomaX - X0 + 0.4f, 0.18f, 1.9f);
        eave.transform.localRotation = Quaternion.Euler(-16f, 0f, 0f);
        eave.GetComponent<Renderer>().sharedMaterial = m.roof;
        roofs.Add(eave.GetComponent<Renderer>());
        for (int i = -1; i <= 1; i += 2)
            box("H_EavePost" + i, t, new Vector3(i < 0 ? X0 + 0.2f : DomaX - 0.2f, F1 + H1 * 0.5f + 0.2f, EngawaZ - 0.15f),
                new Vector3(0.13f, H1 + 0.4f, 0.13f), m.wood);

        Rooms(t, m, box);

        // ---------- 沓ぬぎ石（玄関の 前）。
        // **床は 地めんより 0.52m 高い。** 1段だと 0.43m の 段差に なり、
        // またげる 高さ(0.35m)を こえて 家に 入れなかった。2段に する
        box("H_Fumiishi1", t, new Vector3(DoorOpenX, F1 - 0.44f, Z1 + 1.15f), new Vector3(1.5f, 0.30f, 0.8f), m.stone);
        box("H_Fumiishi2", t, new Vector3(DoorOpenX, F1 - 0.26f, Z1 + 0.55f), new Vector3(1.5f, 0.55f, 0.7f), m.stone);
        // 縁側からも 上がれる ように（庭あそびの 出入り）
        box("H_EngawaIshi", t, new Vector3(-3.6f, F1 - 0.40f, EngawaZ + 0.45f), new Vector3(1.2f, 0.34f, 0.7f), m.stone);
        box("H_EngawaIshi2", t, new Vector3(-3.6f, F1 - 0.22f, EngawaZ - 0.05f), new Vector3(1.2f, 0.6f, 0.6f), m.stone);

        return new[] {
            // 家の 中に 入ったら 手前の 壁と 屋根を 消す
            new RoomCutaway.Piece { parts = front.ToArray(), hideBeyondZ = Z1 + 0.4f },
            new RoomCutaway.Piece { parts = roofs.ToArray(), hideBeyondZ = Z1 + 0.4f },
            // 中の ふすまも 外す＝大広間に なる（田の字型 本来の つかいかた）
            new RoomCutaway.Piece { parts = inner.ToArray(), hideBeyondZ = Z1 + 0.4f },
            // **おくの 部屋へ 行ったら**、まん中の 仕切りも 消す
            new RoomCutaway.Piece { parts = midWall.ToArray(), hideBeyondZ = MidZ + 0.6f },
            // 2階は「1階の おくに いる とき」だけ 消す。
            // 高さの 条件を 入れないと、2階に 上がった とたん 足もとが 消える
            new RoomCutaway.Piece { parts = upper.ToArray(),
                                    hideBeyondZ = MidZ + 0.6f, hideBelowY = F2 - 0.3f },
        };
    }

    // ---------- 部屋ごとの 中みを 入れる。
    //
    // 田の字の 4間は、家具を 入れないと **どれも 同じ 畳の 四角**で、
    // 「台所」「ねま」「じぶんの 部屋」と 言われても どこの ことか 分からない。
    // 何が 置いて あるかで 部屋の 役目を 見せる。
    //
    // 田の字の 割りかた（本もの の 農家の ならび）
    //   手前・土間より = 茶の間（家じゅうが 集まる ところ。ちゃぶ台と テレビ）
    //   手前・おく     = 座敷（庭に 面した よそいき の 部屋。床の間）
    //   おく・土間より = ねま（おじさん おばさん。たんすと 押入れ）
    //   おく・おく     = 仏間
    //   2階 左 = いとこの 部屋／2階 右 = じぶんの 部屋
    static void Rooms(Transform t, Mats m,
                      System.Func<string, Transform, Vector3, Vector3, Material, GameObject> box) {
        // ===== 茶の間（居間）
        {
            float cx = 0f, cz = 2.0f;
            // ちゃぶ台（丸い 低い つくえ）。脚は 4本
            box("R_Chabudai", t, new Vector3(cx, F1 + 0.32f, cz), new Vector3(1.5f, 0.08f, 1.5f), m.wood);
            for (int i = 0; i < 4; i++) {
                float sx = (i % 2 == 0 ? -1f : 1f) * 0.55f, sz = (i < 2 ? -1f : 1f) * 0.55f;
                box("R_ChabuAshi" + i, t, new Vector3(cx + sx, F1 + 0.15f, cz + sz),
                    new Vector3(0.08f, 0.30f, 0.08f), m.wood);
            }
            // 座ぶとん
            box("R_Zabu1", t, new Vector3(cx - 1.1f, F1 + 0.04f, cz), new Vector3(0.62f, 0.08f, 0.62f), m.paper);
            box("R_Zabu2", t, new Vector3(cx + 1.1f, F1 + 0.04f, cz), new Vector3(0.62f, 0.08f, 0.62f), m.paper);
            // ブラウン管テレビ（台に のせる）。田舎の 茶の間の 主
            box("R_TvDai", t, new Vector3(cx + 1.35f, F1 + 0.28f, cz + 1.5f), new Vector3(1.0f, 0.56f, 0.5f), m.wood);
            box("R_Tv",    t, new Vector3(cx + 1.35f, F1 + 0.83f, cz + 1.5f), new Vector3(0.86f, 0.54f, 0.46f), m.plaster);
            box("R_TvGamen",t, new Vector3(cx + 1.35f, F1 + 0.83f, cz + 1.26f), new Vector3(0.66f, 0.40f, 0.04f), m.stone);
        }

        // ===== 座敷（床の間）
        {
            float cx = -3.6f;
            box("R_Tokonoma",  t, new Vector3(cx, F1 + 0.09f, 2.9f), new Vector3(1.9f, 0.18f, 0.8f), m.wood);
            box("R_Kakejiku",  t, new Vector3(cx, F1 + 1.35f, 3.24f), new Vector3(0.6f, 1.5f, 0.04f), m.paper);
            box("R_Kabin",     t, new Vector3(cx + 0.62f, F1 + 0.38f, 2.9f), new Vector3(0.22f, 0.42f, 0.22f), m.stone);
            box("R_TokoBashira",t, new Vector3(cx + 0.98f, F1 + H1 * 0.5f, 2.9f), new Vector3(0.14f, H1, 0.14f), m.wood);
        }

        // ===== ねま（おじさん・おばさん）
        {
            float cx = 0f, cz = -2.2f;
            // たんす（引き出しの すじを 入れる）
            box("R_Tansu", t, new Vector3(cx + 1.1f, F1 + 0.62f, cz - 1.2f), new Vector3(1.5f, 1.25f, 0.55f), m.wood);
            for (int i = 0; i < 3; i++)
                box("R_TansuHiki" + i, t, new Vector3(cx + 1.1f, F1 + 0.28f + i * 0.36f, cz - 0.93f),
                    new Vector3(1.32f, 0.06f, 0.04f), m.floor);
            // 押入れ。**まっ白な 板 1枚では ただの 空白に 見える。**
            // 木の わくを 組んで、その中に ふすま 2枚を はめる
            box("R_OshiireHako", t, new Vector3(cx - 1.3f, F1 + 0.90f, Z0 + 0.35f), new Vector3(1.9f, 1.85f, 0.6f), m.wood);
            for (int i = -1; i <= 1; i += 2)
                box("R_OshiireFusuma" + i, t, new Vector3(cx - 1.3f + i * 0.45f, F1 + 0.90f, Z0 + 0.66f),
                    new Vector3(0.84f, 1.68f, 0.04f), m.paper);
            box("R_OshiireNaka", t, new Vector3(cx - 1.3f, F1 + 0.92f, Z0 + 0.35f), new Vector3(1.92f, 0.07f, 0.62f), m.wood);
            box("R_Futon1",   t, new Vector3(cx - 1.3f, F1 + 0.13f, cz + 0.4f), new Vector3(1.4f, 0.22f, 0.9f), m.floor);
            box("R_Futon2",   t, new Vector3(cx - 1.3f, F1 + 0.33f, cz + 0.4f), new Vector3(1.3f, 0.20f, 0.85f), m.paper);
        }

        // ===== 仏間
        {
            float cx = -3.6f, cz = -2.6f;
            box("R_Butsudan",  t, new Vector3(cx, F1 + 0.85f, Z0 + 0.4f), new Vector3(1.1f, 1.7f, 0.55f), m.wood);
            box("R_ButsuOku",  t, new Vector3(cx, F1 + 1.0f, Z0 + 0.18f), new Vector3(0.86f, 1.1f, 0.06f), m.stone);
            box("R_Rin",       t, new Vector3(cx + 0.62f, F1 + 0.12f, cz + 0.5f), new Vector3(0.2f, 0.16f, 0.2f), m.stone);
        }

        // ===== 2階。
        // ★**2階の 中みは 2階の 床と いっしょに 消す（Upper に 入れる）。**
        //   1階の おくに いると 2階の 床が 消えるので、床だけ 消して 家具を 残すと
        //   **布団や 窓が 宙に 浮いた 白い 板**に なり、何が 起きて いるのか 分からなく なった
        //   （実さい それで 屋内が 白い 板だらけに 見えた）
        // ひだり＝いとこの 部屋（机と 本だな）
        {
            float cx = -3.6f;
            Upper(box("R2_Tsukue",   t, new Vector3(cx, F2 + 0.68f, Z0 + 0.6f), new Vector3(1.2f, 0.06f, 0.6f), m.wood));
            for (int i = -1; i <= 1; i += 2)
                Upper(box("R2_TsukueAshi" + i, t, new Vector3(cx + i * 0.52f, F2 + 0.35f, Z0 + 0.6f),
                          new Vector3(0.07f, 0.66f, 0.5f), m.wood));
            Upper(box("R2_Isu",      t, new Vector3(cx, F2 + 0.22f, Z0 + 1.3f), new Vector3(0.42f, 0.42f, 0.42f), m.wood));
            Upper(box("R2_Hondana",  t, new Vector3(X0 + 0.4f, F2 + 0.75f, 1.4f), new Vector3(0.4f, 1.5f, 1.4f), m.wood));
            for (int i = 0; i < 3; i++)
                Upper(box("R2_Hon" + i, t, new Vector3(X0 + 0.4f, F2 + 0.35f + i * 0.45f, 1.4f),
                          new Vector3(0.34f, 0.30f, 1.24f), m.paper));
        }

        // みぎ＝じぶんの 部屋（しきっぱなしの 布団と 夏休みの しゅくだい）
        {
            float cx = 0f;
            Upper(box("R2_Futon",    t, new Vector3(cx, F2 + 0.24f, -1.6f), new Vector3(1.4f, 0.22f, 2.0f), m.floor));
            Upper(box("R2_Kakebuton",t, new Vector3(cx, F2 + 0.40f, -1.4f), new Vector3(1.32f, 0.18f, 1.5f), m.paper));
            Upper(box("R2_Makura",   t, new Vector3(cx, F2 + 0.36f, -2.5f), new Vector3(0.7f, 0.16f, 0.34f), m.paper));
            Upper(box("R2_Chabu",    t, new Vector3(cx + 0.9f, F2 + 0.36f, 1.9f), new Vector3(1.1f, 0.07f, 0.8f), m.wood));
            Upper(box("R2_Shukudai", t, new Vector3(cx + 0.9f, F2 + 0.41f, 1.9f), new Vector3(0.42f, 0.03f, 0.3f), m.paper));
            Upper(box("R2_Mado",     t, new Vector3(cx + 0.9f, F2 + 1.15f, Z1 - 0.02f), new Vector3(1.4f, 1.2f, 0.05f), m.paper));
        }

        // ===== 台所（土間の 流しの となり）＝食器だな
        box("R_Shokki", t, new Vector3(X1 - 1.05f, F1 + 0.35f, Z0 + 4.2f), new Vector3(1.2f, 1.5f, 0.5f), m.wood);
    }

    static void Front(GameObject g) { Collect(g, front); }
    static void Upper(GameObject g) { Collect(g, upper); }
    static void Mid(GameObject g)   { Collect(g, midWall); }
    static void Inner(GameObject g) { Collect(g, inner); }
    static void Collect(GameObject g, List<Renderer> into) {
        if (g == null) return;
        var r = g.GetComponent<Renderer>();
        if (r != null) into.Add(r);
    }
}
