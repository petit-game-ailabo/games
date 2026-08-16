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
// ★形を ととのえた（2026-08-16・本人の 指摘「自作3Dに 色を 塗って いるだけで おかしい」）
//  絵は 貼った のに 箱に 見えて いたのは、**形が 箱の ままだった**から。3つ 直した。
//   1) 屋根 … 傾けた 箱2まい → **入母屋を メッシュで 起こす**（HouseRoof.cs）。
//      棟・隅棟・破風・反り・軒の 厚み・垂木。空が すける すきまも 消えた
//   2) 壁 … 天井まで まっ白 → **腰は 下見板(よこ板)、上は 漆喰、あいだに 水切り**。
//      さらに **真壁づくり＝柱と 貫を 外に 見せる**。日本の 民家は「白い 面」では なく
//      「木の 枠に はめた 白い 面」。枠が 無いと 発泡スチロールに 見える
//   3) 障子 … 白い 板 → **桟(さん)を 組む**。7.2m の 白い 面が いちばん 目に 痛かった
//
// この 家は 田の字型を とり、土間・縁側・二階を つける。
// **手前の 壁と 屋根は「中に 入ったら 消える」**（RoomCutaway が 面倒を みる）。
using System.Collections.Generic;
using UnityEngine;

public static class BuildHouse {

    public struct Mats {
        public Material tatami, wood, floor, plaster, roof, paper, stone, soil;
        // メッシュで 起こす 屋根は **UV に m を 焼きこむ**ので、貼りかた(tiling)が
        // 1,1 の 別マテリアルが 要る。箱用の(7.2,1.6)を わたすと 絵が 7倍に 伸びる
        public Material roofM, woodM;
        // ★**大きさの ちがう 面に 同じ マテリアルを 貼っては いけない。**
        //   箱の UV は どの 面も 0〜1 なので、貼りかたが 1つだと
        //   10.8m の 壁と 0.6m の 壁で 絵の こまかさが 18倍 ちがう。
        //   下見板を 10.8m の 腰壁に 貼ったら **板の すじが 1本も 出なかった**（絵が
        //   10.8m に 引きのばされた）。面の 大きさ(m)を わたすと 1.5m/まい に そろえて くれる
        public System.Func<float, float, Material> plasterFit, woodFit;
        // 腰の 下見板だけ 暗い（柱と 同じ 明るさだと 板の すじが 読めない）
        public System.Func<float, float, Material> koshiFit;
    }

    // 間取り（m）。原点は 家の まんなか
    //
    // ★2026-08-17：**大農家の 実寸まで 広げ、中廊下を 通した。**
    //   本人「母屋は乗数が少ない。仕切りがない状態で1部屋でいい。しかも廊下とかもない。
    //         田舎の家の広さが分かってないからちゃんと調べてね」
    //
    //   調べた ところ（民家園などの 公開されて いる 実寸の 幅）：
    //    - 中〜大規模の 農家の 母屋は **桁行(間口) 8〜13間＝14.5〜23.6m、
    //      梁間(奥ゆき) 4〜6.5間＝7.3〜11.8m**。前の 18x9.6m は 中規模の 下のほう
    //    - **土間は 平面の 3分の1 を しめる ことも ある**（かまど・作業・農具）
    //    - 座敷は 10〜12畳が ふつうだが、**大農家の 広間(でい)は 20畳を こえる**
    //    - 明治いこうの 民家には **中廊下**が 通る。外の 縁側と 内の 廊下の 2本立て
    //
    //   → 24.0 x 12.0m（桁行 13間・梁間 6.6間）。土間 7.5m ぶん＝90平米。
    //     床上は 3列 x 2行 の 6部屋で、1部屋 27〜32平米＝**16〜19畳**。
    //     そのあいだに **中廊下 1.5m** を 東西に 通す。
    //   ★屋根は 原点を まん中と して 組む ので 左右対称は くずさない
    public const float X0 = -12.0f, X1 = 12.0f;   // 横 24.0m（桁行 13間）
    public const float Z0 = -6.0f, Z1 = 6.0f;     // 奥ゆき 12.0m（梁間 6.6間）
    public const float DomaX = 4.5f;              // これより 右が 土間（7.5m）
    // **中廊下**。床上を 手前と おくに 分ける（ここが 通り道に なる）
    public const float RoukaZ0 = -0.6f, RoukaZ1 = 0.9f;
    public const float MidZ = 0.15f;              // 廊下の まん中（部屋わけの しきい）
    // たての 仕切り 2本＝床上が 3列
    public const float MidX = -6.75f;
    public const float MidX2 = -1.5f;
    public const float F1 = 0f;                   // 1階の 床
    public const float H1 = 2.35f;                // 1階の 高さ
    public const float F2 = H1 + 0.18f;           // 2階の 床
    public const float H2 = 2.05f;                // 2階の 高さ
    public const float WallTop = F2 + H2;         // 軒げたが のる 高さ（4.58）
    public const float EngawaZ = Z1 + 1.2f;       // 縁側の そと ばし
    public const float DoorX = 8.25f;             // 玄関の まん中（土間 4.5〜12.0 の まん中）
    // **玄関は 広く とる。** 1.6m で 引き戸を 半分 立てたら、あいた すきまが
    // 体の 幅(0.52m)と ほぼ 同じに なり、戸の はしに 引っかかって 入れなかった
    public const float DoorW = 3.0f;
    public const float DoorLeaf = 1.2f;          // 立って いる 戸の 幅
    /// <summary>人が 通れる ところの まん中</summary>
    public static float DoorOpenX { get { return DoorX + (DoorW * 0.5f - (DoorW * 0.5f - DoorLeaf)) * 0.5f; } }

    // 腰壁（下見板）の 高さ と 出っぱり
    const float KoshiY = 0.98f;      // 腰壁の 天
    const float WallHalf = 0.08f;    // 外壁の 厚みの 半分
    const float Bite = 0.02f;        // 壁に どれだけ くいこませるか（すきまを 作らない ため）
    const float PostW = 0.145f;      // 見せる 柱の 太さ
    const float Ken = 1.80f;         // 柱の 間かく＝1間

    /// <summary>壁の 外がわに はる 物の 中心。壁の 面から 出っぱらせる</summary>
    static float Face(float wall, float sign, float depth) {
        return wall + sign * (WallHalf + depth * 0.5f - Bite);
    }

    static readonly List<Renderer> front = new List<Renderer>();   // 手前の 壁（入ったら 消す）
    static readonly List<Renderer> roofs = new List<Renderer>();   // 屋根（入ったら 消す）
    static readonly List<Renderer> upper = new List<Renderer>();   // 2階の 床（おくへ 行くと 消す）
    static readonly List<Renderer> midWall = new List<Renderer>(); // まん中の 仕切り（おくへ 行くと 消す）
    // 中の 建具（左右の ふすま）。**家に 入ったら 外す。**
    // 田の字型は もともと「建具を 外すと 大広間に なる」間取り。
    // 立てた ままだと 天井まで ある 白い 壁が 画の まん中を ふさぎ、
    // せっかく 屋根を 抜いても 半分しか 見えなかった
    static readonly List<Renderer> inner = new List<Renderer>();

    // box を 使いまわす ための 置き場（Rooms から も 触る）
    static System.Func<string, Transform, Vector3, Vector3, Material, GameObject> B;

    public static RoomCutaway.Piece[] Build(Transform root, Mats m,
                                            System.Func<string, Transform, Vector3, Vector3, Material, GameObject> box) {
        front.Clear(); roofs.Clear(); upper.Clear(); midWall.Clear(); inner.Clear();
        B = box;
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
        //
        // ★土間の 上は 吹きぬけ。むかしの 農家の 土間は 屋根の 裏まで 抜けて いる。
        //   前は 壁が H1(2.35m)で 止まって いて、屋根を 軒げたの 高さ(4.58m)に 上げた とたん
        //   **土間の 上が 2.2m ぶん 素どおしに なった**ので、外の 壁は 軒げたまで 立てる
        box("H_WallBack", t, new Vector3(0f, F1 + WallTop * 0.5f, Z0), new Vector3(X1 - X0, WallTop, 0.16f),
            m.plasterFit(X1 - X0, WallTop));
        box("H_WallL", t, new Vector3(X0, F1 + WallTop * 0.5f, 0f), new Vector3(0.16f, WallTop, Z1 - Z0),
            m.plasterFit(Z1 - Z0, WallTop));
        box("H_WallR", t, new Vector3(X1, F1 + WallTop * 0.5f, 0f), new Vector3(0.16f, WallTop, Z1 - Z0),
            m.plasterFit(Z1 - Z0, WallTop));

        // 手前の 壁（玄関の 左右）。入ったら 消える
        float lw = (DoorX - DoorW * 0.5f) - DomaX;
        Front(box("H_WallF_L", t, new Vector3((DomaX + DoorX - DoorW * 0.5f) * 0.5f, F1 + WallTop * 0.5f, Z1),
                  new Vector3(lw, WallTop, 0.16f), m.plasterFit(lw, WallTop)));
        float rw = X1 - (DoorX + DoorW * 0.5f);
        Front(box("H_WallF_R", t, new Vector3((DoorX + DoorW * 0.5f + X1) * 0.5f, F1 + WallTop * 0.5f, Z1),
                  new Vector3(rw, WallTop, 0.16f), m.plasterFit(rw, WallTop)));
        // 玄関の 上。かもい から 軒げたまでを ふさぐ（吹きぬけの 口を あけない）
        Front(box("H_DoorTop", t, new Vector3(DoorX, F1 + H1 - 0.25f, Z1), new Vector3(DoorW, 0.5f, 0.20f),
                  m.woodFit(DoorW, 0.5f)));
        Front(box("H_DoorUp", t, new Vector3(DoorX, F1 + (H1 + WallTop) * 0.5f, Z1),
                  new Vector3(DoorW, WallTop - H1, 0.16f), m.plasterFit(DoorW, WallTop - H1)));
        // 玄関の 引き戸（半分 あいて いる）
        // 引き戸は 左に よせて 立てる。右がわが まるごと あく
        Front(box("H_Door", t, new Vector3(DoorX - DoorW * 0.5f + DoorLeaf * 0.5f, F1 + 0.9f, Z1 + 0.05f),
                  new Vector3(DoorLeaf, 1.8f, 0.06f), m.paper));
        Front(Sun(box, "H_DoorSan", t, new Vector3(DoorX - DoorW * 0.5f + DoorLeaf * 0.5f, F1 + 0.9f, Z1 + 0.09f),
                  DoorLeaf, 1.8f, 2, 3, m.wood));
        // 玄関の わく（柱と まぐさ）。木で 縁どると 穴が「戸口」に 見える
        for (int i = -1; i <= 1; i += 2)
            Front(box("H_DoorPost" + i, t, new Vector3(DoorX + i * DoorW * 0.5f, F1 + H1 * 0.5f, Z1 + 0.03f),
                      new Vector3(0.17f, H1, 0.22f), m.wood));

        // 座敷がわの 手前は 障子（縁側に 面する）。**桟を 組む。**
        // 桟の 無い 障子は 7.2m の まっ白な 板で、家の 顔が のっぺりする いちばんの 原因だった
        // 間口が 12.6m に なった ので 障子は 7まい（1まい 1.8m ＝ 1間）
        for (int i = 0; i < 9; i++) {
            float w = (DomaX - X0) / 9f;
            float cx = X0 + w * (i + 0.5f);
            Front(box("H_Shoji" + i, t, new Vector3(cx, F1 + 0.95f, Z1),
                      new Vector3(w * 0.96f, 1.9f, 0.06f), m.paper));
            Front(Sun(box, "H_ShojiSan" + i, t, new Vector3(cx, F1 + 0.95f, Z1 + 0.045f),
                      w * 0.96f, 1.9f, 3, 4, m.wood));
        }
        Front(box("H_ShojiTop", t, new Vector3((X0 + DomaX) * 0.5f, F1 + H1 - 0.22f, Z1),
                  new Vector3(DomaX - X0, 0.44f, 0.18f), m.wood));
        // 障子の 下ばし（敷居）
        Front(box("H_Shikii", t, new Vector3((X0 + DomaX) * 0.5f, F1 + 0.04f, Z1),
                  new Vector3(DomaX - X0, 0.14f, 0.22f), m.wood));
        // 2階の 手前は 板と 窓（漆喰の のっぺりを 切る）
        Front(box("H_W2Front", t, new Vector3((X0 + DomaX) * 0.5f, F2 + H2 * 0.5f, Z1),
                  new Vector3(DomaX - X0, H2, 0.12f), m.plasterFit(DomaX - X0, H2)));

        Skin(t, m);          // 腰壁・柱・貫（外がわの 化粧）
        Nageshi(t, m);       // 屋内の 長押（天井が 消えた ときの 4.6m の 壁を 割る）

        // ---------- 中の 仕切り（田の字）
        // まん中の 仕切りは、**おくの 部屋へ 行くと 消える**
        // ★**中廊下**。手前の 部屋と おくの 部屋の あいだを 1.5m の 廊下が 通る。
        //   ふすまは 廊下の 両がわに 立つ（おくへ 行くと 消える）
        Mid(box("H_Fusuma_Z", t, new Vector3((X0 + DomaX) * 0.5f, F1 + H1 * 0.5f, RoukaZ1),
                new Vector3(DomaX - X0, H1, 0.1f), m.paper));
        Mid(box("H_Fusuma_Z2", t, new Vector3((X0 + DomaX) * 0.5f, F1 + H1 * 0.5f, RoukaZ0),
                new Vector3(DomaX - X0, H1, 0.1f), m.paper));
        // 廊下の 床は 板（畳では ない）
        box("H_Rouka", t, new Vector3((X0 + DomaX) * 0.5f, F1 + 0.005f, (RoukaZ0 + RoukaZ1) * 0.5f),
            new Vector3(DomaX - X0, 0.09f, RoukaZ1 - RoukaZ0), m.floor);
        // たての 仕切りは **廊下で 切る**（廊下を ふさがない）
        foreach (float mx in new[] { MidX, MidX2 }) {
            Inner(box("H_FusumaXF" + (int)(mx * 10), t,
                      new Vector3(mx, F1 + H1 * 0.5f, (RoukaZ1 + Z1) * 0.5f),
                      new Vector3(0.1f, H1, Z1 - RoukaZ1), m.paper));
            Inner(box("H_FusumaXB" + (int)(mx * 10), t,
                      new Vector3(mx, F1 + H1 * 0.5f, (Z0 + RoukaZ0) * 0.5f),
                      new Vector3(0.1f, H1, RoukaZ0 - Z0), m.paper));
        }
        // 土間と 床上の さかいの 柱。**吹きぬけの 天まで 通す（通し柱）**
        for (int i = -1; i <= 1; i += 2)
            box("H_PostDoma" + i, t, new Vector3(DomaX, F1 + WallTop * 0.5f, i * (Z1 - 0.4f)),
                new Vector3(0.19f, WallTop, 0.19f), m.wood);
        // 土間の 上の 梁（吹きぬけを 横に わたす 太い 木）。**農家の 土間の 顔**
        for (int i = 0; i < 5; i++) {
            float z = Z0 + 1.4f + i * 2.5f;
            box("H_Hari" + i, t, new Vector3((DomaX + X1) * 0.5f, F1 + WallTop - 0.55f, z),
                new Vector3(X1 - DomaX, 0.30f, 0.24f), m.wood);
        }

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
        // 2階の 仕切り。**床と いっしょに 消す**（残すと 宙に 浮く）。
        // 外がわの 壁は 1階から 軒げたまで 1まいで 通す ように した ので ここには 立てない
        Upper(box("H_W2Mid", t, new Vector3(MidX, F2 + H2 * 0.5f, 0f), new Vector3(0.1f, H2, Z1 - Z0), m.paper));
        Upper(box("H_W2Mid2", t, new Vector3(MidX2, F2 + H2 * 0.5f, 0f), new Vector3(0.1f, H2, Z1 - Z0), m.paper));

        // ---------- 屋根。入母屋を メッシュで 起こす。入ったら 消える
        var opt = new HouseRoof.Opt {
            ax = X1, az = Z1, eave = 1.35f, yEave = F1 + WallTop + 0.04f,
            rise = 2.90f, hipRun = 3.40f, tHip = 0.49f, sori = 1.34f,
            tipLift = 0.17f, thick = 0.20f, texM = 1.5f,
        };
        roofs.AddRange(HouseRoof.Build(t, opt, m.roofM, m.woodM, null));

        // 縁側の 下屋（げや）。**母屋の 軒は 4.7m と 高すぎて 縁側に 影が かからない。**
        // 一段 低い 屋根を かけると、縁側が「軒の 下」に なって 日本の 家に 見える
        roofs.AddRange(HouseRoof.Shed(t, "H_Geya", X0 - 0.20f, DomaX + 0.10f,
                                      Z1 - 0.05f, EngawaZ + 0.55f,
                                      F1 + H1 + 0.42f, F1 + H1 - 0.10f, 1.5f, m.roofM, m.woodM));
        // 下屋を ささえる 柱。**縁側の 上に 立てる**（縁側の そとは 地めんが 0.5m 低いので
        // そこに 立てると 足もとが 宙に 浮く）
        for (int i = -1; i <= 1; i += 2)
            box("H_EavePost" + i, t,
                new Vector3(i < 0 ? X0 + 0.30f : DomaX - 0.30f, F1 + 1.13f, EngawaZ - 0.12f),
                new Vector3(0.15f, 2.36f, 0.15f), m.wood);
        // 玄関の 庇（ひさし）
        roofs.AddRange(HouseRoof.Shed(t, "H_Hisashi", DoorX - 1.9f, DoorX + 1.9f,
                                      Z1 - 0.05f, Z1 + 1.25f,
                                      F1 + H1 + 0.38f, F1 + H1 - 0.02f, 1.5f, m.roofM, m.woodM));

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

    // ---------- 外がわの 化粧（腰壁・柱・貫）
    //
    // ★日本の 民家の 壁は「白い 面」では なく **「木の 枠に はめた 白い 面」**。
    //   柱を 塗りこめる 大壁づくり は 蔵や 町家の 形で、農家は **真壁づくり**＝
    //   柱・貫が 外に 見える。この 木の すじが 無いと、どれだけ 絵を 貼っても
    //   「白い 箱に 色を 塗った もの」に しか 見えない。
    // ★腰壁（こしかべ）＝下から 1m ほどに 板を 横に はる。雨の はねを よける ためで、
    //   実さいの 農家は ほぼ 例外なく こうなって いる。**白と 焦茶に 分かれる**ので
    //   面が 半分の 高さに 割れ、のっぺりが 消える。
    static void Skin(Transform t, Mats m) {
        float yb = F1 - 0.36f;                       // 板の 下ばし（床下まで まわす）
        float kc = (yb + KoshiY) * 0.5f, kh = KoshiY - yb;
        float lw = (DoorX - DoorW * 0.5f) - DomaX, rw = X1 - (DoorX + DoorW * 0.5f);
        float flx = (DomaX + DoorX - DoorW * 0.5f) * 0.5f, frx = (DoorX + DoorW * 0.5f + X1) * 0.5f;
        const float KD = 0.14f, MD = 0.22f, ND = 0.15f;   // 腰板／水切り／貫 の 厚み

        // --- 腰の 下見板。おく・左右・手前（玄関の 左右）
        // ★**板の 大きさが 命。** 10.8m の 面に 貼りかた(1,2)の マテリアルを あてたら
        //   絵 1まいが 10.8m に のびて、下見板の すじが 1本も 出なかった。woodFit で そろえる
        Yoko("H_Koshi_B", t, 0f, X1 - X0 + 0.02f, kc, kh, Z0, -1f, KD, m.koshiFit(X1 - X0, kh));
        Tate("H_Koshi_L", t, 0f, Z1 - Z0 + 0.02f, kc, kh, X0, -1f, KD, m.koshiFit(Z1 - Z0, kh));
        Tate("H_Koshi_R", t, 0f, Z1 - Z0 + 0.02f, kc, kh, X1, +1f, KD, m.koshiFit(Z1 - Z0, kh));
        Front(Yoko("H_Koshi_FL", t, flx, lw, kc, kh, Z1, +1f, KD, m.koshiFit(lw, kh)));
        Front(Yoko("H_Koshi_FR", t, frx, rw, kc, kh, Z1, +1f, KD, m.koshiFit(rw, kh)));

        // --- 水切り（腰板の 天に かぶせる 小さな 庇）。板と 漆喰の さかいを はっきり させる
        float my = KoshiY + 0.05f;
        float mbx = X1 - X0 + 0.12f, mbz = Z1 - Z0 + 0.12f;
        Yoko("H_Mizukiri_B", t, 0f, mbx, my, 0.10f, Z0, -1f, MD, m.woodFit(mbx, 0.10f));
        Tate("H_Mizukiri_L", t, 0f, mbz, my, 0.10f, X0, -1f, MD, m.woodFit(mbz, 0.10f));
        Tate("H_Mizukiri_R", t, 0f, mbz, my, 0.10f, X1, +1f, MD, m.woodFit(mbz, 0.10f));
        Front(Yoko("H_Mizukiri_FL", t, flx, lw, my, 0.10f, Z1, +1f, MD, m.woodFit(lw, 0.10f)));
        Front(Yoko("H_Mizukiri_FR", t, frx, rw, my, 0.10f, Z1, +1f, MD, m.woodFit(rw, 0.10f)));

        // --- 柱（1間ごと）。腰の 上から 軒げたまで
        float py = (KoshiY + WallTop) * 0.5f, ph = WallTop - KoshiY;
        const float PD = 0.13f;
        var pm = m.woodFit(PostW, ph);
        for (float x = X0 + Ken; x < X1 - 0.1f; x += Ken)
            NoHit(B("H_Hashira_B" + Mathf.RoundToInt(x * 10), t,
                    new Vector3(x, py, Face(Z0, -1f, PD)), new Vector3(PostW, ph, PD), pm));
        for (float z = Z0 + Ken; z < Z1 - 0.1f; z += Ken) {
            NoHit(B("H_Hashira_L" + Mathf.RoundToInt(z * 10), t,
                    new Vector3(Face(X0, -1f, PD), py, z), new Vector3(PD, ph, PostW), pm));
            NoHit(B("H_Hashira_R" + Mathf.RoundToInt(z * 10), t,
                    new Vector3(Face(X1, +1f, PD), py, z), new Vector3(PD, ph, PostW), pm));
        }
        // 四すみの 柱は 太く（隅柱）
        var sm = m.woodFit(0.27f, WallTop - yb);
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2) {
                var g = NoHit(B("H_Sumibashira" + (sx > 0 ? "R" : "L") + (sz > 0 ? "F" : "B"), t,
                          new Vector3(sx * (X1 + 0.055f), (yb + WallTop) * 0.5f, sz * (Z1 + 0.055f)),
                          new Vector3(0.27f, WallTop - yb, 0.27f), sm));
                if (sz > 0) Front(g);
            }
        // --- 貫(ぬき)。2階の 床の 高さを 横に 1本 通す＝面が 上下に 割れる
        float ny = F2 + 0.02f;
        Yoko("H_Nuki_B", t, 0f, X1 - X0, ny, 0.16f, Z0, -1f, ND, m.woodFit(X1 - X0, 0.16f));
        Tate("H_Nuki_L", t, 0f, Z1 - Z0, ny, 0.16f, X0, -1f, ND, m.woodFit(Z1 - Z0, 0.16f));
        Tate("H_Nuki_R", t, 0f, Z1 - Z0, ny, 0.16f, X1, +1f, ND, m.woodFit(Z1 - Z0, 0.16f));
        Front(Yoko("H_Nuki_FL", t, flx, lw, ny, 0.16f, Z1, +1f, ND, m.woodFit(lw, 0.16f)));
        Front(Yoko("H_Nuki_FR", t, frx, rw, ny, 0.16f, Z1, +1f, ND, m.woodFit(rw, 0.16f)));

        // --- 2階の 窓（手前）。**明かりとりが 無いと 家に 見えない**
        for (int i = 0; i < 4; i++) {
            float x = X0 + 2.6f + i * 4.4f;
            Front(NoHit(B("H_Mado2Waku" + i, t, new Vector3(x, F2 + 1.05f, Z1 + 0.10f),
                    new Vector3(1.62f, 1.32f, 0.05f), m.wood)));
            Front(NoHit(B("H_Mado2_" + i, t, new Vector3(x, F2 + 1.05f, Z1 + 0.13f),
                    new Vector3(1.45f, 1.15f, 0.05f), m.paper)));
            Front(Sun(B, "H_Mado2San" + i, t, new Vector3(x, F2 + 1.05f, Z1 + 0.17f),
                      1.45f, 1.15f, 3, 3, m.wood));
        }
        // おくの 壁の 小さな 窓（虫籠窓ふう）
        for (int i = 0; i < 5; i++) {
            float x = X0 + 2.4f + i * 4.8f;
            NoHit(B("H_MadoB" + i, t, new Vector3(x, F2 + 1.0f, Face(Z0, -1f, 0.08f)),
                    new Vector3(1.1f, 0.85f, 0.08f), m.wood));
        }
        // --- 左右の 壁（妻がわ）の 窓。**ここが いちばん 大きな 空白だった。**
        // 柱と 貫の 格子だけが 並んで「窓の 無い 蔵」に 見えていた
        // ★**重ねる 順を まちがえると 窓が「黒い 穴」に なる。**
        //   わく を 紙より 外に 置いたら、わくの 板が 紙を まるごと ふさいで
        //   壁に 開いた 暗い 四角に しか 見えなかった。**おく から わく→紙→桟**の 順
        for (int s = -1; s <= 1; s += 2) {
            string k = s < 0 ? "L" : "R";
            float wx = s < 0 ? X0 : X1;
            float xW = wx + s * 0.10f, xP = wx + s * 0.13f, xS = wx + s * 0.17f;
            // 2階
            NoHit(B("H_MadoS" + k + "Waku", t, new Vector3(xW, F2 + 1.05f, -0.9f),
                    new Vector3(0.05f, 1.32f, 1.62f), m.woodFit(1.62f, 1.32f)));
            NoHit(B("H_MadoS" + k, t, new Vector3(xP, F2 + 1.05f, -0.9f),
                    new Vector3(0.05f, 1.15f, 1.45f), m.paper));
            Sun2(B, "H_MadoS" + k + "San", t, new Vector3(xS, F2 + 1.05f, -0.9f),
                 1.45f, 1.15f, 3, 3, m.wood);
            // 1階は 明かりとりの 小窓（たて格子）
            NoHit(B("H_MadoS1" + k + "Waku", t, new Vector3(xW, F1 + 1.62f, 1.5f),
                    new Vector3(0.05f, 0.94f, 1.44f), m.woodFit(1.44f, 0.94f)));
            NoHit(B("H_MadoS1" + k, t, new Vector3(xP, F1 + 1.62f, 1.5f),
                    new Vector3(0.05f, 0.78f, 1.28f), m.paper));
            Sun2(B, "H_MadoS1" + k + "San", t, new Vector3(xS, F1 + 1.62f, 1.5f),
                 1.28f, 0.78f, 6, 1, m.wood);
        }
    }

    // ---------- 屋内の 長押(なげし)
    //
    // 2階の 床は「1階の おくに いる ときだけ」消える＝そのとき **天井が 無くなり、
    // 軒げたまでの 4.6m の 壁が まるごと 見える**。何も 無いと ただの 高い 板に なる。
    // 本ものの 座敷は 鴨居の 上に 長押が まわって いて、そこで 面が 割れる
    static void Nageshi(Transform t, Mats m) {
        const float y = 1.86f, h = 0.15f, d = 0.09f;
        float wid = DomaX - X0;
        NoHit(B("H_Nageshi_B", t, new Vector3((X0 + DomaX) * 0.5f, y, Z0 + WallHalf + d * 0.5f),
                new Vector3(wid, h, d), m.woodFit(wid, h)));
        NoHit(B("H_Nageshi_L", t, new Vector3(X0 + WallHalf + d * 0.5f, y, (Z0 + Z1) * 0.5f),
                new Vector3(d, h, Z1 - Z0), m.woodFit(Z1 - Z0, h)));
        // 土間がわ（通し柱の 内がわ）
        NoHit(B("H_Nageshi_D", t, new Vector3(DomaX - 0.14f, y, (Z0 + Z1) * 0.5f),
                new Vector3(d, h, Z1 - Z0), m.woodFit(Z1 - Z0, h)));
    }

    /// <summary>壁の 外がわに はる、X に のびる 板</summary>
    static GameObject Yoko(string name, Transform t, float cx, float len, float cy, float h,
                           float wallZ, float sign, float depth, Material m) {
        return NoHit(B(name, t, new Vector3(cx, cy, Face(wallZ, sign, depth)),
                       new Vector3(len, h, depth), m));
    }

    /// <summary>壁の 外がわに はる、Z に のびる 板</summary>
    static GameObject Tate(string name, Transform t, float cz, float len, float cy, float h,
                           float wallX, float sign, float depth, Material m) {
        return NoHit(B(name, t, new Vector3(Face(wallX, sign, depth), cy, cz),
                       new Vector3(depth, h, len), m));
    }

    /// <summary>あたりを 外す。化粧の 板は 壁より 外に 出るので、当たりを のこすと
    /// 家の まわりが 12cm ずつ ふくらんで 沓ぬぎ石に 乗れなく なる</summary>
    static GameObject NoHit(GameObject g) {
        if (g == null) return null;
        var c = g.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);
        return g;
    }

    /// <summary>障子・窓の 桟(さん)を 組む。cols/rows は マスの 数。
    /// 桟の 無い 障子は ただの まっ白な 板で、家の 顔が のっぺりする 最大の 原因だった</summary>
    static GameObject Sun(System.Func<string, Transform, Vector3, Vector3, Material, GameObject> box,
                          string name, Transform t, Vector3 c, float w, float h,
                          int cols, int rows, Material m) {
        const float S = 0.045f, D = 0.045f;    // 桟の 太さ／出
        var g = new GameObject(name);
        g.transform.SetParent(t, false);
        System.Action<string, Vector3, Vector3> put = (n, p, s) =>
            NoHit(box(name + n, g.transform, p, s, m));
        put("_T", new Vector3(c.x, c.y + h * 0.5f, c.z), new Vector3(w, S * 1.8f, D));
        put("_B", new Vector3(c.x, c.y - h * 0.5f, c.z), new Vector3(w, S * 1.8f, D));
        put("_L", new Vector3(c.x - w * 0.5f, c.y, c.z), new Vector3(S * 1.8f, h, D));
        put("_R", new Vector3(c.x + w * 0.5f, c.y, c.z), new Vector3(S * 1.8f, h, D));
        for (int i = 1; i < cols; i++)
            put("_V" + i, new Vector3(c.x - w * 0.5f + w * i / cols, c.y, c.z), new Vector3(S, h, D * 0.9f));
        for (int i = 1; i < rows; i++)
            put("_H" + i, new Vector3(c.x, c.y - h * 0.5f + h * i / rows, c.z), new Vector3(w, S, D * 0.9f));
        return g;
    }

    /// <summary>Sun の 左右の 壁むけ。桟の 幅が X では なく Z に のびる</summary>
    static GameObject Sun2(System.Func<string, Transform, Vector3, Vector3, Material, GameObject> box,
                           string name, Transform t, Vector3 c, float w, float h,
                           int cols, int rows, Material m) {
        const float S = 0.045f, D = 0.045f;
        var g = new GameObject(name);
        g.transform.SetParent(t, false);
        System.Action<string, Vector3, Vector3> put = (n, p, s) =>
            NoHit(box(name + n, g.transform, p, s, m));
        put("_T", new Vector3(c.x, c.y + h * 0.5f, c.z), new Vector3(D, S * 1.8f, w));
        put("_B", new Vector3(c.x, c.y - h * 0.5f, c.z), new Vector3(D, S * 1.8f, w));
        put("_L", new Vector3(c.x, c.y, c.z - w * 0.5f), new Vector3(D, h, S * 1.8f));
        put("_R", new Vector3(c.x, c.y, c.z + w * 0.5f), new Vector3(D, h, S * 1.8f));
        for (int i = 1; i < cols; i++)
            put("_V" + i, new Vector3(c.x, c.y, c.z - w * 0.5f + w * i / cols), new Vector3(D * 0.9f, h, S));
        for (int i = 1; i < rows; i++)
            put("_H" + i, new Vector3(c.x, c.y - h * 0.5f + h * i / rows, c.z), new Vector3(D * 0.9f, S, w));
        return g;
    }

    // ---------- 部屋ごとの 中みを 入れる。
    //
    // 部屋は 家具を 入れないと **どれも 同じ 畳の 四角**で、
    // 「台所」「ねま」「じぶんの 部屋」と 言われても どこの ことか 分からない。
    //
    // ★2026-08-17：六間取り（3列 x 2行）に なった ので 割りなおした。
    //   列の まん中： 左 -6.9 ／ 中 -2.7 ／ 右 1.5
    //   行の まん中： 手前(+Z) 2.4 ／ おく(-Z) -2.4
    //   手前左=座敷 手前中=茶の間 手前右=おばあちゃんの へや
    //   おく左=仏間 おく中=ねま おく右=なんど
    //   2階 左=いとこの 部屋／中=じぶんの 部屋／右=屋根うら
    static void Rooms(Transform t, Mats m,
                      System.Func<string, Transform, Vector3, Vector3, Material, GameObject> box) {
        // 列の まん中：(-12.0,-6.75) (-6.75,-1.5) (-1.5,4.5)
        const float CL = -9.375f, CM = -4.125f, CR = 1.5f;
        // 行の まん中：手前(0.9〜6.0) おく(-6.0〜-0.6)
        const float RF = 3.45f, RB = -3.3f;

        // ===== 手前・左：座敷（床の間）
        box("R_Tokonoma",   t, new Vector3(CL, F1 + 0.09f, Z1 - 0.5f), new Vector3(2.4f, 0.18f, 0.9f), m.wood);
        box("R_Kakejiku",   t, new Vector3(CL, F1 + 1.45f, Z1 - 0.12f), new Vector3(0.7f, 1.7f, 0.04f), m.paper);
        box("R_Kabin",      t, new Vector3(CL + 0.8f, F1 + 0.40f, Z1 - 0.5f), new Vector3(0.24f, 0.44f, 0.24f), m.stone);
        box("R_TokoBashira",t, new Vector3(CL + 1.25f, F1 + H1 * 0.5f, Z1 - 0.5f), new Vector3(0.15f, H1, 0.15f), m.wood);
        box("R_Zabu0",      t, new Vector3(CL, F1 + 0.04f, RF - 0.6f), new Vector3(0.62f, 0.08f, 0.62f), m.paper);

        // ===== 手前・中：茶の間
        box("R_Chabudai", t, new Vector3(CM, F1 + 0.32f, RF), new Vector3(1.6f, 0.08f, 1.6f), m.wood);
        for (int i = 0; i < 4; i++) {
            float sx = (i % 2 == 0 ? -1f : 1f) * 0.58f, sz = (i < 2 ? -1f : 1f) * 0.58f;
            box("R_ChabuAshi" + i, t, new Vector3(CM + sx, F1 + 0.15f, RF + sz),
                new Vector3(0.08f, 0.30f, 0.08f), m.wood);
        }
        box("R_Zabu1", t, new Vector3(CM - 1.3f, F1 + 0.04f, RF), new Vector3(0.62f, 0.08f, 0.62f), m.paper);
        box("R_Zabu2", t, new Vector3(CM + 1.3f, F1 + 0.04f, RF), new Vector3(0.62f, 0.08f, 0.62f), m.paper);
        // ブラウン管テレビ。田舎の 茶の間の 主
        box("R_TvDai",  t, new Vector3(CM, F1 + 0.28f, Z1 - 0.6f), new Vector3(1.0f, 0.56f, 0.5f), m.wood);
        box("R_Tv",     t, new Vector3(CM, F1 + 0.83f, Z1 - 0.6f), new Vector3(0.86f, 0.54f, 0.46f), m.plaster);
        box("R_TvGamen",t, new Vector3(CM, F1 + 0.83f, Z1 - 0.84f), new Vector3(0.66f, 0.40f, 0.04f), m.stone);

        // ===== 手前・右：おばあちゃんの へや
        box("R_Kyodai",      t, new Vector3(CR, F1 + 0.34f, Z1 - 0.6f), new Vector3(1.0f, 0.62f, 0.45f), m.wood);
        box("R_KyodaiKagami",t, new Vector3(CR, F1 + 1.05f, Z1 - 0.7f), new Vector3(0.5f, 0.72f, 0.05f), m.stone);
        box("R_Tansu2",      t, new Vector3(CR + 1.5f, F1 + 0.62f, RF + 0.6f), new Vector3(1.3f, 1.25f, 0.55f), m.wood);
        for (int i = 0; i < 3; i++)
            box("R_Tansu2H" + i, t, new Vector3(CR + 1.5f, F1 + 0.28f + i * 0.36f, RF + 0.33f),
                new Vector3(1.14f, 0.06f, 0.04f), m.floor);
        box("R_Zabu3", t, new Vector3(CR - 0.9f, F1 + 0.04f, RF - 0.4f), new Vector3(0.62f, 0.08f, 0.62f), m.paper);

        // ===== おく・左：仏間
        box("R_Butsudan", t, new Vector3(CL, F1 + 0.90f, Z0 + 0.45f), new Vector3(1.2f, 1.8f, 0.6f), m.wood);
        box("R_ButsuOku", t, new Vector3(CL, F1 + 1.05f, Z0 + 0.18f), new Vector3(0.94f, 1.2f, 0.06f), m.stone);
        box("R_Rin",      t, new Vector3(CL + 0.7f, F1 + 0.12f, RB + 0.6f), new Vector3(0.2f, 0.16f, 0.2f), m.stone);
        box("R_Zabu4",    t, new Vector3(CL, F1 + 0.04f, RB + 0.3f), new Vector3(0.62f, 0.08f, 0.62f), m.paper);

        // ===== おく・中：ねま
        // 押入れ。**まっ白な 板 1枚では ただの 空白に 見える。**わくを 組んで ふすまを はめる
        box("R_OshiireHako", t, new Vector3(CM, F1 + 0.90f, Z0 + 0.35f), new Vector3(2.4f, 1.85f, 0.6f), m.wood);
        for (int i = -1; i <= 1; i += 2)
            box("R_OshiireFusuma" + i, t, new Vector3(CM + i * 0.58f, F1 + 0.90f, Z0 + 0.66f),
                new Vector3(1.08f, 1.68f, 0.04f), m.paper);
        box("R_OshiireNaka", t, new Vector3(CM, F1 + 0.92f, Z0 + 0.35f), new Vector3(2.42f, 0.07f, 0.62f), m.wood);
        box("R_Futon1", t, new Vector3(CM, F1 + 0.13f, RB + 0.5f), new Vector3(1.4f, 0.22f, 2.0f), m.floor);
        box("R_Futon2", t, new Vector3(CM, F1 + 0.33f, RB + 0.7f), new Vector3(1.3f, 0.20f, 1.5f), m.paper);
        box("R_Makura1",t, new Vector3(CM, F1 + 0.30f, RB - 0.6f), new Vector3(0.7f, 0.16f, 0.34f), m.paper);

        // ===== おく・右：なんど（物置）
        box("R_Tansu", t, new Vector3(CR + 1.4f, F1 + 0.62f, RB), new Vector3(1.4f, 1.25f, 0.55f), m.wood);
        for (int i = 0; i < 3; i++)
            box("R_TansuHiki" + i, t, new Vector3(CR + 1.4f, F1 + 0.28f + i * 0.36f, RB - 0.27f),
                new Vector3(1.22f, 0.06f, 0.04f), m.floor);
        for (int i = 0; i < 5; i++) {
            float x = CR - 1.5f + (i % 3) * 0.62f, y = F1 + 0.24f + (i / 3) * 0.46f;
            box("R_Tawara" + i, t, new Vector3(x, y, Z0 + 0.7f), new Vector3(0.56f, 0.44f, 0.8f), m.floor);
        }
        box("R_Kuwa", t, new Vector3(CR - 0.2f, F1 + 0.75f, Z0 + 0.2f), new Vector3(0.08f, 1.5f, 0.08f), m.wood);

        // ===== 2階。
        // ★**2階の 中みは 2階の 床と いっしょに 消す（Upper に 入れる）。**
        //   床だけ 消して 家具を 残すと 布団や 窓が 宙に 浮いた 白い 板に なる
        // ひだり＝いとこの 部屋（机と 本だな）
        Upper(box("R2_Tsukue", t, new Vector3(CL, F2 + 0.68f, Z0 + 0.7f), new Vector3(1.3f, 0.06f, 0.6f), m.wood));
        for (int i = -1; i <= 1; i += 2)
            Upper(box("R2_TsukueAshi" + i, t, new Vector3(CL + i * 0.57f, F2 + 0.35f, Z0 + 0.7f),
                      new Vector3(0.07f, 0.66f, 0.5f), m.wood));
        Upper(box("R2_Isu",     t, new Vector3(CL, F2 + 0.22f, Z0 + 1.5f), new Vector3(0.42f, 0.42f, 0.42f), m.wood));
        Upper(box("R2_Hondana", t, new Vector3(X0 + 0.45f, F2 + 0.75f, RF), new Vector3(0.45f, 1.5f, 1.6f), m.wood));
        for (int i = 0; i < 3; i++)
            Upper(box("R2_Hon" + i, t, new Vector3(X0 + 0.45f, F2 + 0.35f + i * 0.45f, RF),
                      new Vector3(0.38f, 0.30f, 1.44f), m.paper));

        // まんなか＝じぶんの 部屋（しきっぱなしの 布団と 夏休みの しゅくだい）
        Upper(box("R2_Futon",     t, new Vector3(CM, F2 + 0.24f, RB), new Vector3(1.4f, 0.22f, 2.0f), m.floor));
        Upper(box("R2_Kakebuton", t, new Vector3(CM, F2 + 0.40f, RB + 0.2f), new Vector3(1.32f, 0.18f, 1.5f), m.paper));
        Upper(box("R2_Makura",    t, new Vector3(CM, F2 + 0.36f, RB - 0.9f), new Vector3(0.7f, 0.16f, 0.34f), m.paper));
        Upper(box("R2_Chabu",     t, new Vector3(CM + 1.0f, F2 + 0.36f, RF), new Vector3(1.1f, 0.07f, 0.8f), m.wood));
        Upper(box("R2_Shukudai",  t, new Vector3(CM + 1.0f, F2 + 0.41f, RF), new Vector3(0.42f, 0.03f, 0.3f), m.paper));
        Upper(box("R2_Mushikago", t, new Vector3(CM - 1.2f, F2 + 0.24f, RF + 0.6f), new Vector3(0.34f, 0.4f, 0.34f), m.wood));

        // みぎ＝屋根うら（つかって いない。行李と むしろ）
        for (int i = 0; i < 3; i++)
            Upper(box("R2_Kori" + i, t, new Vector3(CR - 0.6f + i * 0.1f, F2 + 0.22f + i * 0.4f, RB + 0.3f),
                      new Vector3(1.1f, 0.38f, 0.7f), m.floor));
        Upper(box("R2_Mushiro", t, new Vector3(CR + 1.2f, F2 + 0.06f, RF), new Vector3(1.2f, 0.08f, 1.8f), m.tatami));

        // ===== 台所（土間の 流しの となり）＝食器だな
        box("R_Shokki", t, new Vector3(X1 - 1.05f, F1 + 0.35f, Z0 + 5.4f), new Vector3(1.2f, 1.5f, 0.5f), m.wood);
    }


    static void Front(GameObject g) { Collect(g, front); }
    static void Upper(GameObject g) { Collect(g, upper); }
    static void Mid(GameObject g)   { Collect(g, midWall); }
    static void Inner(GameObject g) { Collect(g, inner); }
    static void Collect(GameObject g, List<Renderer> into) {
        if (g == null) return;
        var r = g.GetComponent<Renderer>();
        if (r != null) { into.Add(r); return; }
        // 桟の ように 子を まとめた 入れものは、中の renderer を ぜんぶ 入れる
        into.AddRange(g.GetComponentsInChildren<Renderer>());
    }
}
