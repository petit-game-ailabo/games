using UnityEngine;
using UnityEditor;

// 庭の 西がわの 納屋（物置小屋・2026-09-05）。
//
// ★本人「庭の左側に納屋を設置したい。大きいものじゃなくていいけど、釣り竿、虫取り網、虫かご、
//   みたいなアクションアイテムがこの中に格納されるイメージ。…この家の人が使うような掃除用具や、
//   生け垣の整理用具とか、バケツや木の選定はさみとか。様々なものがあるイメージ」
//
// 調べた こと（昭和の 田舎の 物置）
//   ・母屋とは 別むね。1〜2坪（1.8〜3.6m四方）の 板張り、切妻の 瓦か トタン
//   ・入口は **妻がわ**（三角の 見える 面）に 引き戸 2枚。開けっぱなしで つかう
//   ・床は 張らない **土間**。地面の 湿気を 切る ため 石の 布基礎に 乗る
//   ・戸の 上には 小さな 庇。妻入りは 軒の 出が 少なく、戸が 雨に さらされる ため
//   ・道具は 立てかける（柄の 長い ものは 壁ぎわ）／棚に 置く（かご・小道具）
//
// 置きかた：庭の 段の 西の はし。生垣（x=-9.7・厚み0.9＝内がわの 面が -9.25）の すぐ 内。
//   戸は **東**（庭の まん中がわ）を 向く＝妻入り。
//
// ★戸を 南（カメラの 正面）へ 向けた 版は 取り消した（2026-09-05・本人「納屋、扉は右側でいいかも。
//   全ての扉が正面向きも気持ち悪い」）。母屋の 玄関が すでに 南を 向いて いる ので、納屋まで
//   正面だと 建てものが そろって こちらを 向き、書き割りに 見える。
//   **中の 道具は 屋内カメラ（`NiwaNayaNaka`）が 手前の 壁と 屋根を 消して 見せる**ので、
//   戸の 面が 画面に 対して 真横でも かまわない。
//
// ★中に **入れる**（2026-09-05）。壁 1枚ずつに 当たりを つけ、戸口だけ あける。
//   入ると `NiwaNayaNaka` が カメラを 引きとり、南の 壁と 屋根を 消して 外を 落とす。
public static class NiwaNaya {
    // ---- 世界での 置き場所（BuildNiwa から 参照する）
    // ★x は 生垣から 離す（2026-09-05・本人「生け垣も少し壁をすり抜けて、納屋の中に入ってる」）。
    //   生垣は 株の 芯が 半分 0.44m ＋ 毛の シェル 0.24m ＋ 横ゆらぎ 0.12m ＝ **線から 0.80m** ふくらむ。
    //   線を -10.05 へ 寄せ（BuildNiwa）、小屋を 東へ 0.30 動かして けらばとの すきまを 0.34m とった
    public const float CX = -7.45f, CZ = 2.30f;      // 小屋の まん中
    public const float HX = 1.30f, HZ = 1.65f;       // 壁しんの 半分（東西 2.6m x 南北 3.3m）
    /// <summary>戸口の 前の 立ち位置（地面の 絵に 踏み跡を 描く）</summary>
    public static Vector3 Guchi { get { return new Vector3(CX + HX + 0.9f, 0f, CZ - 0.15f); } }

    const float ATSU = 0.09f;      // 板壁の 厚み
    const float DODAI = 0.24f;     // 石の 布基礎の 天
    // ★軒げたの 天は **屋根の 裏より 下**。壁しん(z=HZ)での 屋根裏は
    //   MUNE - (MUNE-YNOKI)*HZ/(HZ+DE) - 屋根の厚み0.15 = 2.221m。
    //   ここを 外すと 妻がわの 壁が 軒の すみで 屋根を つきぬける
    const float NOKI = 2.15f;      // 壁の 天（軒げた の 上ば）
    const float MUNE = 3.20f;      // 棟
    const float YNOKI = 2.16f;     // 軒先（z=±(HZ+DE)）
    const float DE = 0.42f;        // 軒の 出（南北）
    // ★戸口は **開いた ときに 0.86m** 通れる 広さ（2026-09-05）。0.72m だと 主人公の
    //   当たり（半径0.26）が すりぬけるのに 気を つかい、戸口で つっかえた
    const float TO_H = 0.86f;      // 戸口の 半分（1.72m）
    const float TO_W = 0.88f;      // 引き戸 1枚の はば
    const float TO_Y = 1.98f;      // 戸の 高さ
    const float TAI = 0.50f;       // 中を 歩ける 帯の 半分（z 方向）

    const float TM_KAWARA = 2.8f, TM_ITA = 1.0f, TM_KI = 0.55f;
    const float TM_ISHI = 1.6f, TM_TO = 2.5f, TM_TAKE = 1.25f;

    static Material mKi, mKiTate, mKawara, mTo, mIshi, mTake, mTetsu, mAo, mNuno, mTsuchi;

    static Material MatIro(string name, Color c, float tsuya) {
        return NiwaBuhin.Mat(name, null, Vector2.one, c, false, false, tsuya);
    }

    public static Transform Build(Transform root) {
        var g = new GameObject("Naya");
        g.transform.SetParent(root, false);
        g.transform.position = new Vector3(CX, NiwaJimenE.Takasa(CX, CZ), CZ);
        var t = g.transform;

        // ---- 材質（絵は 母屋と 同じ ものを つかう。同じ 家の 持ちものに 見える）
        // ★瓦の 色は **母屋と そろえる**（IeKawaraMesh は 0.80,0.80,0.82）。
        //   0.94 で 焼いたら 納屋の 屋根だけ 白っぽく 浮いた（2026-09-05）
        mKawara = NiwaBuhin.Mat("NayaKawara", "shashin/ie_kawara.jpg", Vector2.one,
                                new Color(0.80f, 0.80f, 0.82f), false, true);
        mKi = NiwaBuhin.Mat("NayaKiYane", "shashin/ie_ki.jpg", Vector2.one, Color.white);
        mKiTate = NiwaBuhin.Fit("NayaHashira", "shashin/ie_ki.jpg", 0.13f, 2.2f, TM_KI,
                                new Color(1.00f, 0.96f, 0.88f));
        mTake = NiwaBuhin.Mat("NayaTake", "shashin/take_kawa_ki.jpg", new Vector2(1f, 3f), Color.white);
        mTetsu = MatIro("NayaTetsu", new Color(0.46f, 0.47f, 0.49f), 0.35f);
        mAo = MatIro("NayaAo", new Color(0.30f, 0.42f, 0.55f), 0.25f);
        // ★網は **すける**。不とうめいの 円すいだと 電がさに 見える（2026-09-05）
        mNuno = NiwaBuhin.Mat("NayaAmi", null, Vector2.one,
                              new Color(0.74f, 0.79f, 0.70f), false, false, 0.06f, 0.30f);
        mTsuchi = MatIro("NayaDoma", new Color(0.30f, 0.26f, 0.21f), 0.02f);

        // ---- 石の 布基礎（土間の 湿気を 切る）
        mIshi = NiwaBuhin.Fit("NayaIshi", "shashin/ishigaki.jpg", 2.9f, 0.36f, TM_ISHI,
                              new Color(0.98f, 0.97f, 0.94f));
        // ★基礎に **当たりを つける**（土間の 上に 立たせる。段差 0.24m は
        //   CharacterController の stepOffset 0.35m の 内なので 一歩で 上がれる）
        NiwaBuhin.Hako(t, "Naya_Dodai", new Vector3(0f, 0.05f, 0f),
                       new Vector3(HX * 2f + 0.16f, 0.34f, HZ * 2f + 0.16f), mIshi, true);
        // 土間（中の 地めん）。★基礎の 天と **同じ 高さに しない**。0.24 で そろえたら
        //   2つの 面が 取りあって 床が 石と 土の まだらに なった（2026-09-05）
        NiwaBuhin.Hako(t, "Naya_Doma", new Vector3(0f, 0.24f, 0f),
                       new Vector3(HX * 2f - ATSU, 0.10f, HZ * 2f - ATSU), mTsuchi, true);

        // ---- 板壁（下見板。母屋の 腰板と 同じ 絵）
        Kabe(t, "Naya_KabeW", -HX, -HX, -HZ, HZ, DODAI, NOKI);            // 西（妻がわ）
        Kabe(t, "Naya_KabeS", -HX, HX, -HZ, -HZ, DODAI, NOKI);            // 南（軒がわ・屋内カメラで 消える）
        Kabe(t, "Naya_KabeN", -HX, HX, HZ, HZ, DODAI, NOKI);              // 北（軒がわ）
        Kabe(t, "Naya_KabeE1", HX, HX, -HZ, -TO_H, DODAI, NOKI);          // 東の 袖壁（戸の 両わき）
        Kabe(t, "Naya_KabeE2", HX, HX, TO_H, HZ, DODAI, NOKI);
        Kabe(t, "Naya_KabeE3", HX, HX, -TO_H, TO_H, TO_Y, NOKI);          // 戸の 上の 小壁

        // ---- 隅の 柱と 軒げた（凸凹が 影を 作る＝立体に 見える。板だけだと 箱に 見える）
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                NiwaBuhin.Hako(t, "Naya_Hashira",
                    new Vector3(sx * (HX + 0.015f), (DODAI + NOKI) * 0.5f, sz * (HZ + 0.015f)),
                    new Vector3(0.13f, NOKI - DODAI, 0.13f), mKiTate);
        foreach (float sz in new[] { -1f, 1f })
            NiwaBuhin.Hako(t, "Naya_Nokigeta", new Vector3(0f, NOKI - 0.07f, sz * (HZ + 0.02f)),
                new Vector3(HX * 2f + 0.20f, 0.15f, 0.13f),
                NiwaBuhin.Fit("NayaGeta", "shashin/ie_ki.jpg", 2.8f, 0.15f, TM_KI,
                              new Color(1.00f, 0.96f, 0.88f)));

        // ---- 屋根＝切妻。棟は 東西（x）に 走り、南北へ 流れる＝**妻入り**（戸が 三角の 面に つく）
        HouseRoof.Shed(t, "Naya_YaneN", -HX, HX, 0f, HZ + DE, MUNE, YNOKI, TM_KAWARA, mKawara, mKi);
        var muki = new GameObject("Naya_Muki").transform;      // ★Shed は zIn<zOut が 前提。
        muki.SetParent(t, false);                              //   南の 流れは 180°まわした 子の 中で 組む
        muki.localRotation = Quaternion.Euler(0f, 180f, 0f);
        HouseRoof.Shed(muki, "Naya_YaneS", -HX, HX, 0f, HZ + DE, MUNE, YNOKI, TM_KAWARA, mKawara, mKi);
        // 棟包み
        NiwaBuhin.Hako(t, "Naya_Mune", new Vector3(0f, MUNE - 0.06f, 0f),
                       new Vector3(HX * 2f + 0.34f, 0.20f, 0.30f), mKawara);

        // ---- 妻壁（三角）。**下ばは 壁の 天より 下**（すきまが 空くと 光が すける）。
        //   両はしで 屋根の 裏より 下なら、直線どうしなので あいだも ぜんぶ 下に なる
        foreach (float sx in new[] { -1f, 1f }) {
            var tm = NiwaBuhin.Tsuma("NayaTsuma", HZ, NOKI - 0.06f, MUNE - 0.16f, 0.10f, TM_ITA);
            var go = NiwaBuhin.Mesh1(t, "Naya_Tsuma", tm,
                NiwaBuhin.Mat("NayaTsumaIta", "shashin/ie_itakabe.jpg", Vector2.one,
                              new Color(1.02f, 0.98f, 0.90f), false, true));
            go.transform.localPosition = new Vector3(sx * (HX + 0.02f), 0f, 0f);
        }
        // 破風板（妻の へりの 板。これが 無いと 屋根が 紙に 見える）
        {
            float th = Mathf.Atan2(MUNE - YNOKI, HZ + DE) * Mathf.Rad2Deg;
            float len = Mathf.Sqrt((HZ + DE) * (HZ + DE) + (MUNE - YNOKI) * (MUNE - YNOKI));
            var mHafu = NiwaBuhin.Fit("NayaHafu", "shashin/ie_ki.jpg", len, 0.20f, TM_KI,
                                      new Color(0.94f, 0.89f, 0.80f));
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                    NiwaBuhin.HakoR(t, "Naya_Hafu",
                        new Vector3(sx * (HX + 0.155f), (YNOKI + MUNE) * 0.5f - 0.11f, sz * (HZ + DE) * 0.5f),
                        new Vector3(0.06f, 0.20f, len), new Vector3(sz * th, 0f, 0f), mHafu);
        }

        // ---- 戸口（引き戸 2枚。1枚を 開けはなして 中の 道具を 見せる）
        Do(t);
        // 戸の 上の 庇（妻入りは 軒の 出が 少なく、戸が 雨に さらされる）
        {
            var h = new GameObject("Naya_Hisashi").transform;
            h.SetParent(t, false);
            h.localPosition = new Vector3(HX, 0f, 0f);
            h.localRotation = Quaternion.Euler(0f, 90f, 0f);   // 子の +z が 世界の +x
            HouseRoof.Shed(h, "Naya_Kabetsuki_Hisashi", -0.86f, 0.86f, 0.02f, 0.62f,
                           2.28f, 2.12f, TM_KAWARA, mKawara, mKi);
        }

        // ---- 中の 道具
        Dougu(t);

        // ---- 中は **左右だけ 歩ける 帯**（2026-09-05・本人「納屋の中当たり判定を細かくつけるのは
        //      難しいと思うから、ほぼ左右移動だけで、上下はほとんど動けないぐらいの当たり判定でいいや」）。
        //      道具 1つずつに 当たりを つけるのは 続かない ので、**通り道のほうを 決めて しまう**。
        //      帯は 戸口の まん中（z=CZ）を 通る ので、東から そのまま 入れる
        //      ★帯は **戸口の 手前で 切る**。戸口いっぱいまで のばすと、入った とたん 横の かべに
        //      あたって 足が 止まる。東の 0.7m は 戸口の 土間＝自由に 動ける
        foreach (float sz in new[] { -1f, 1f }) {
            var w = NiwaBuhin.Hako(t, "BLK_NayaOku",
                new Vector3(-0.35f, (DODAI + 1.7f) * 0.5f, sz * (TAI + 0.07f)),
                new Vector3(HX * 2f - 0.70f, 1.7f, 0.14f), null, true);
            w.GetComponent<Renderer>().enabled = false;
        }

        // ---- 屋内の 見せかた（`NiwaNayaNaka`）。カメラ・ポストFX・主人公は
        //      場面の あとの ほうで できる ので **BuildNiwa が つなぐ**
        Okunai(g, t);

        return t;
    }

    /// <summary>取れる 道具の 台帳。主人公・虫・書体は BuildNiwa が つなぐ。
    /// ★**あみと かごで ひとそろい**（1回 取れば 両方）。手に 出るのは あみだけ</summary>
    static void Daicho(Transform ami) {
        var g = new GameObject("Dougu");
        g.transform.SetParent(ami.root, false);
        var dd = g.AddComponent<NiwaDougu>();
        dd.mono = new[] {
            new NiwaDougu.Mono {
                id = "mushitori", namae = "むしとりあみ",
                totta = "むしとりあみと かごを てに いれた", mi = ami,
                oki = ami.position, okiKaiten = ami.eulerAngles,
                // 右手。柄の もとを 腰に、先が 頭より 上へ 出る。
                // ★傾きは **外へ**。内へ 倒すと 柄が 顔を 横切る（2026-09-05）
                mochiOff = new Vector3(0.34f, 0.05f, 0.14f),
                mochiKaiten = new Vector3(22f, -10f, 24f),
            },
        };
    }

    /// <summary>中に 入った ときの しかけ。消す 物は **名まえの 頭で 拾う**
    /// （BuildNiwa が 1つずつ 名ざしする ことに なると 部品を 足す たびに 忘れる）</summary>
    static void Okunai(GameObject g, Transform t) {
        // 中の あかり。**外に いる あいだは 0**（外から 小屋だけ 光って 見えたら 嘘に なる）
        var akariGO = new GameObject("Naya_Kabetsuki_Akari");
        akariGO.transform.SetParent(t, false);
        // ★主人公の **真上に 置かない**。0.3m 上に 5の 点光源を 置いたら
        //   帽子の 白い リボンが ブルームで 白い 塊に なった（2026-09-05）。
        //   戸口がわの 高い ところから 斜めに 入れる＝戸から 差す 光にも 見える
        akariGO.transform.localPosition = new Vector3(0.62f, 2.04f, -0.50f);
        var pl = akariGO.AddComponent<Light>();
        pl.type = LightType.Point; pl.range = 5.6f; pl.intensity = 0f;
        pl.color = new Color(1f, 0.94f, 0.82f);
        pl.shadows = LightShadows.None;

        var naka = g.AddComponent<NiwaNayaNaka>();
        naka.akari = pl;
        float gy = t.position.y;
        naka.naka = new Bounds(new Vector3(CX, gy + 1.0f, CZ),
                               new Vector3(HX * 2f - 0.25f, 2.6f, HZ * 2f - 0.25f));
        // 台は 小屋の 南の 外。19°の 見おろしで 土間の おくまで 入る
        naka.camPos = new Vector3(CX - 0.20f, gy + 2.55f, CZ - 4.70f);
        naka.camLook = new Vector3(CX + 0.15f, gy + 0.95f, CZ + 0.20f);
        naka.camFov = 33f;
        // 戸：ToB が 南へ 0.72 引かれて 北がわが 開く。外の 立ち位置は 開く ほうの 正面
        naka.toB = toBT;
        naka.toAke = -TO_W * 0.5f + 0.02f;      // 南の 1枚に ほぼ 重ねる＝北がわが 0.86m 開く
        naka.toKabe = toKabeC;
        naka.soto = new Vector3(CX + HX + 0.95f, gy + 0.10f, CZ + 0.22f);

        // 消す：南の 壁・屋根の 2面・棟・破風・妻・軒げた・戸の 上の 庇
        var kesu = new System.Collections.Generic.List<Renderer>();
        string[] atama = { "Naya_KabeS", "Naya_YaneN", "Naya_YaneS", "Naya_Mune",
                           "Naya_Hafu", "Naya_Tsuma", "Naya_Nokigeta",
                           "Naya_Kabetsuki_Hisashi", "Naya_Soto_" };
        foreach (var r in t.GetComponentsInChildren<Renderer>())
            foreach (var k in atama)
                if (r.name.StartsWith(k)) { kesu.Add(r); break; }
        naka.kesu = kesu.ToArray();
        Debug.Log("[Probe] NiwaNaya 屋内で 消す " + kesu.Count + " 個");
    }

    /// <summary>板壁 1面（x か z の どちらかが 同じ 2点で 面を 決める）。
    /// ★当たりは **壁 1枚ずつ**に つける。まるごと 1個の 箱で かこむと 中に 入れなく なる
    /// （natsuyasumi スキル「戸口は 壁を 4枚 置いて まん中を あける」）</summary>
    static void Kabe(Transform t, string name, float x0, float x1, float z0, float z1,
                     float y0, float y1) {
        float w = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(z1 - z0));
        var c = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f);
        var s = new Vector3(Mathf.Abs(x1 - x0) < 0.01f ? ATSU : Mathf.Abs(x1 - x0), y1 - y0,
                            Mathf.Abs(z1 - z0) < 0.01f ? ATSU : Mathf.Abs(z1 - z0));
        // ★壁は **色あせた たて板**（母屋の 2階と 同じ 絵）。はじめ 下見板（母屋の 腰板）で
        //   焼いたら **小屋 ぜんたいが まっ黒**に なった（2026-09-05）。腰板の 絵は
        //   柿渋・すすで 黒に 近く、腰の 帯 1本だから 効く もので、1棟 ぜんぶには 使えない
        float k = 1.00f + 0.10f * Mathf.Abs(Mathf.Sin(name.Length * 2.399f));
        NiwaBuhin.Hako(t, name, c, s,
            NiwaBuhin.Fit("NayaIta", "shashin/ie_itakabe.jpg", w, y1 - y0, TM_ITA,
                          new Color(k, k * 0.97f, k * 0.92f), true), true);
    }

    /// <summary>引き戸 2枚。手前(東)の 1枚を 北へ 引いて あり、南がわが 開いて いる</summary>
    static void Do(Transform t) {
        mTo = NiwaBuhin.Fit("NayaTo", "shashin/ie_tobukuro.jpg", 0.76f, 1.66f, TM_TO,
                            new Color(0.98f, 0.93f, 0.85f));
        var mWaku = NiwaBuhin.Fit("NayaWaku", "shashin/ie_ki.jpg", 1.6f, 0.09f, TM_KI,
                                  new Color(0.92f, 0.86f, 0.78f));
        // 敷居と 鴨居
        NiwaBuhin.Hako(t, "Naya_Shikii", new Vector3(HX - 0.02f, DODAI + 0.03f, 0f),
                       new Vector3(0.16f, 0.07f, TO_H * 2f + 0.14f), mWaku);
        NiwaBuhin.Hako(t, "Naya_Kamoi", new Vector3(HX - 0.02f, TO_Y - 0.04f, 0f),
                       new Vector3(0.16f, 0.09f, TO_H * 2f + 0.14f), mWaku);
        // 戸 2枚。★**閉じた 位置**で 組む（開けるのは `NiwaNayaNaka`）。
        //   南の 1枚（ToA）は 動かず、北の 1枚（ToB）が 南へ 引かれて 北がわが 開く
        float y0 = DODAI + 0.06f, y1 = TO_Y - 0.07f;
        toAT = new GameObject("Naya_ToA").transform;
        toAT.SetParent(t, false); toAT.localPosition = new Vector3(0f, 0f, -TO_W * 0.5f);
        Ita1(toAT, "ToA", HX + 0.015f, 0f, TO_W, y0, y1);
        toBT = new GameObject("Naya_ToB").transform;
        toBT.SetParent(t, false); toBT.localPosition = new Vector3(0f, 0f, TO_W * 0.5f);
        Ita1(toBT, "ToB", HX - 0.045f, 0f, TO_W, y0, y1);
        // 引き手（黒い 小さな くぼみ）
        NiwaBuhin.Hako(toBT, "Hikite", new Vector3(HX + 0.03f, 1.05f, -0.34f),
                       new Vector3(0.02f, 0.13f, 0.05f), mTetsu);
        // 閉じて いる あいだ 通れなく する かべ（開いたら `NiwaNayaNaka` が 切る）
        var kb = NiwaBuhin.Hako(t, "BLK_NayaTo",
                                new Vector3(HX, (DODAI + TO_Y) * 0.5f, 0f),
                                new Vector3(0.14f, TO_Y - DODAI, TO_H * 2f), null, true);
        kb.GetComponent<Renderer>().enabled = false;
        toKabeC = kb.GetComponent<Collider>();
    }

    static Transform toAT, toBT;
    static Collider toKabeC;

    static void Ita1(Transform t, string name, float x, float zc, float haba, float y0, float y1) {
        NiwaBuhin.Hako(t, name, new Vector3(x, (y0 + y1) * 0.5f, zc),
                       new Vector3(0.035f, y1 - y0, haba), mTo);
        // 桟（框と 中桟）。板だけだと のっぺりした 1枚の 板に 見える
        var mSan = NiwaBuhin.Fit("NayaSan", "shashin/ie_ki.jpg", haba, 0.08f, TM_KI,
                                 new Color(0.86f, 0.80f, 0.71f));
        foreach (float y in new[] { y0 + 0.05f, (y0 + y1) * 0.5f, y1 - 0.05f })
            NiwaBuhin.Hako(t, name + "_San", new Vector3(x + 0.026f, y, zc),
                           new Vector3(0.02f, 0.08f, haba), mSan);
        foreach (float s in new[] { -1f, 1f })
            NiwaBuhin.Hako(t, name + "_Kabetsuki_Kamachi",
                           new Vector3(x + 0.026f, (y0 + y1) * 0.5f, zc + s * (haba * 0.5f - 0.04f)),
                           new Vector3(0.02f, y1 - y0, 0.08f), mSan);
    }

    // ---------------------------------------------------------------- 道具
    static void Dougu(Transform t) {
        var d = new GameObject("Naya_Dougu").transform;
        d.SetParent(t, false);
        var mBou = NiwaBuhin.Mat("NayaBou", "shashin/ie_ki.jpg", new Vector2(1f, 6f),
                                 new Color(0.90f, 0.83f, 0.70f));
        var mIta2 = NiwaBuhin.Fit("NayaTana", "shashin/ie_ki.jpg", 1.35f, 0.75f, TM_KI,
                                  new Color(0.88f, 0.82f, 0.72f));

        // ---- 棚（北の 壁ぎわ・2段）
        float tx = -0.55f, tz = 1.18f;
        foreach (float y in new[] { 0.80f, 1.34f })
            NiwaBuhin.Hako(d, "Naya_Tana", new Vector3(tx, y, tz), new Vector3(1.38f, 0.045f, 0.78f), mIta2);
        foreach (float sx in new[] { -0.62f, 0.62f })
            NiwaBuhin.Hako(d, "Naya_TanaAshi", new Vector3(tx + sx, 0.95f, tz),
                           new Vector3(0.07f, 1.42f, 0.07f), mIta2);

        // ★虫かごは 置かない（2026-09-05・本人「納屋に虫かごはおかないかな。
        //   網と籠はワンセットのアイテムとして拾えるようにしよう」）。かごは 物として 作らず、
        //   中みは メニュー（`NiwaMenu`）で 見る
        // ---- 剪定ばさみ と 軍手（下の 棚）
        NiwaBuhin.HakoR(d, "Naya_Sentei", new Vector3(0.02f, 0.845f, 1.02f),
                        new Vector3(0.05f, 0.03f, 0.22f), new Vector3(0f, 24f, 0f), mTetsu);
        NiwaBuhin.HakoR(d, "Naya_SenteiE", new Vector3(0.02f, 0.835f, 0.88f),
                        new Vector3(0.04f, 0.02f, 0.14f), new Vector3(0f, 12f, 0f),
                        MatIro("NayaAkaE", new Color(0.52f, 0.24f, 0.18f), 0.15f));
        NiwaBuhin.Hako(d, "Naya_Gunte", new Vector3(-0.95f, 0.845f, 1.30f),
                       new Vector3(0.18f, 0.05f, 0.13f),
                       MatIro("NayaGunte", new Color(0.78f, 0.76f, 0.70f), 0.05f));

        // ---- 釣り竿 2本（西の 壁に 立てかける。長い 柄の ものは 壁ぎわ が 決まり）
        for (int i = 0; i < 2; i++) {
            float z = -0.45f - i * 0.26f;
            var a = new Vector3(-1.02f, DODAI, z);
            var b = new Vector3(-1.17f, 2.05f, z - 0.08f);
            NiwaBuhin.Bou(d, "Naya_Sao", a, Vector3.Lerp(a, b, 0.45f), 0.016f, mBou);
            NiwaBuhin.Bou(d, "Naya_SaoSaki", Vector3.Lerp(a, b, 0.45f), b, 0.008f, mBou);
            NiwaBuhin.Bou(d, "Naya_SaoMoto", a, Vector3.Lerp(a, b, 0.16f), 0.021f,
                          MatIro("NayaSaoMoto", new Color(0.34f, 0.30f, 0.26f), 0.20f));
        }

        // ---- 虫取り網。**柄の もとが 原点**の かたまり（+y が 柄の 向き）。
        //      輪は 柄に 直角、網は 輪から 下へ 垂れる（柄の 向きに のばすと 電がさに 見えた）
        var amiT = new GameObject("Naya_Mono_Ami").transform;
        amiT.SetParent(d, false);
        {
            var a = new Vector3(0.86f, DODAI, -1.38f);
            var b = new Vector3(1.02f, 1.76f, -1.02f);
            var dir = (b - a).normalized;
            amiT.localPosition = a;
            amiT.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
            float len = (b - a).magnitude;
            NiwaBuhin.Bou(amiT, "Ami_E", Vector3.zero, new Vector3(0f, len, 0f), 0.014f, mTake);
            var wa = NiwaBuhin.Mesh1(amiT, "Ami_Wa", NiwaBuhin.Wa("NayaAmiWa", 0.155f, 0.010f), mTetsu);
            wa.transform.localPosition = new Vector3(0f, len + 0.02f, 0f);
            var ami = NiwaBuhin.Mesh1(amiT, "Ami_Nuno",
                                      NiwaBuhin.Tsutsu("NayaAmiNuno", 0.150f, 0.030f, 0.30f, false, 0.2f, 12),
                                      mNuno);
            ami.transform.localPosition = new Vector3(0f, len + 0.02f, 0f);
            ami.transform.localRotation = Quaternion.Euler(178f, 0f, 10f);
        }
        Daicho(amiT);

        // ---- 竹ぼうき（戸口の 内がわに 立てかける）
        Houki(d, new Vector3(1.08f, DODAI, 1.34f), new Vector3(1.16f, 1.52f, 1.02f));
        // ---- 刈込ばさみ（生垣の 整理用。南の 壁ぎわ）
        {
            var a = new Vector3(-0.40f, DODAI, -1.48f);
            var b = new Vector3(-0.30f, 1.06f, -1.34f);
            foreach (float s in new[] { -0.035f, 0.035f }) {
                var o = new Vector3(s, 0f, 0f);
                NiwaBuhin.Bou(d, "Naya_Karikomi_E", a + o, b + o, 0.017f, mBou);
                NiwaBuhin.Bou(d, "Naya_Karikomi_Ha", b + o, b + o + (b - a).normalized * 0.34f,
                              0.010f, mTetsu);
            }
        }

        // ---- バケツ（中に 1つ・外に 1つ）と じょうろ
        Baketsu(d, new Vector3(0.52f, DODAI, 1.42f), mTetsu, 0.145f);
        Baketsu(d, new Vector3(1.86f, NiwaJimenE.Takasa(CX + 1.86f, CZ - 0.42f) - NiwaJimenE.Takasa(CX, CZ),
                               -0.42f), mAo, 0.135f);
        Jouro(d, new Vector3(0.62f, DODAI, -0.72f));

        // ---- 木箱と 肥料の 袋（奥に 積む。すきまが 埋まると「使って いる 小屋」に 見える）
        NiwaBuhin.Hako(d, "Naya_Hako1", new Vector3(-0.86f, DODAI + 0.17f, -1.18f),
                       new Vector3(0.46f, 0.34f, 0.30f), mIta2);
        NiwaBuhin.HakoR(d, "Naya_Hako2", new Vector3(-0.88f, DODAI + 0.46f, -1.14f),
                        new Vector3(0.40f, 0.24f, 0.28f), new Vector3(0f, 9f, 0f), mIta2);
        NiwaBuhin.HakoR(d, "Naya_Fukuro", new Vector3(-1.02f, DODAI + 0.13f, 0.52f),
                        new Vector3(0.34f, 0.26f, 0.22f), new Vector3(0f, 0f, 6f),
                        MatIro("NayaFukuro", new Color(0.62f, 0.58f, 0.46f), 0.05f));

        // ---- 外に 立てかけた もの（南の 壁）：脚立と 竹ぼうき。
        //      ★名まえを `Naya_Soto_` に して、屋内カメラの ときは 一緒に 消す
        //        （立てかけて いる 壁が 消えるので、のこすと 宙に 浮いた 棒に なる）
        {
            float gy = NiwaJimenE.Takasa(CX + 0.2f, CZ - HZ - 0.3f) - NiwaJimenE.Takasa(CX, CZ);
            soto = "Naya_Soto_";
            Kyatatsu(d, new Vector3(0.10f, gy, -HZ - 0.24f));
            Houki(d, new Vector3(-0.85f, gy, -HZ - 0.30f), new Vector3(-0.80f, 1.44f, -HZ - 0.04f));
            soto = "";
        }
    }

    static string soto = "";
    static void Houki(Transform d, Vector3 a, Vector3 b) {
        NiwaBuhin.Bou(d, soto + "Naya_Houki_E", a + (b - a).normalized * 0.32f, b, 0.017f, mTake);
        var dir = (b - a).normalized;
        for (int i = 0; i < 11; i++) {                 // 穂＝細い 竹を ひろげる
            float w = (i / 10f - 0.5f) * 0.26f;
            var yoko = Vector3.Cross(dir, Vector3.forward).normalized;
            NiwaBuhin.Bou(d, soto + "Naya_Houki_Ho", a + yoko * w + dir * 0.02f,
                          a + dir * 0.36f + yoko * w * 0.35f, 0.005f, mTake);
        }
    }

    static void Baketsu(Transform d, Vector3 at, Material m, float r) {
        var g = NiwaBuhin.Mesh1(d, "Naya_Baketsu",
                                NiwaBuhin.Tsutsu("NayaBaketsu" + m.name, r * 0.86f, r, 0.28f, true, 0.35f),
                                m);
        g.transform.localPosition = at;
        // つる（針金の 取っ手）。3本の 短い 棒で 弧に する
        var p = new Vector3[4];
        for (int i = 0; i < 4; i++) {
            float a = Mathf.PI * i / 3f;
            p[i] = at + new Vector3(-Mathf.Cos(a) * r, 0.28f + Mathf.Sin(a) * r * 0.9f, 0f);
        }
        for (int i = 0; i < 3; i++) NiwaBuhin.Bou(d, "Naya_Baketsu_Tsuru", p[i], p[i + 1], 0.007f, mTetsu);
    }

    static void Jouro(Transform d, Vector3 at) {
        var g = NiwaBuhin.Mesh1(d, "Naya_Jouro",
                                NiwaBuhin.Tsutsu("NayaJouro", 0.105f, 0.095f, 0.24f, true, 0.3f), mTetsu);
        g.transform.localPosition = at;
        // 注ぎ口（前へ 上がる）と ハス口
        var a = at + new Vector3(0.06f, 0.07f, 0f);
        var b = at + new Vector3(0.36f, 0.24f, 0f);
        NiwaBuhin.Bou(d, "Naya_Jouro_Kuchi", a, b, 0.022f, mTetsu);
        var hasu = NiwaBuhin.Mesh1(d, "Naya_Jouro_Hasu",
                                   NiwaBuhin.Tsutsu("NayaHasu", 0.026f, 0.055f, 0.05f, true, 0.2f), mTetsu);
        hasu.transform.localPosition = b;
        hasu.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
        // 取っ手
        NiwaBuhin.Bou(d, "Naya_Jouro_Te", at + new Vector3(-0.08f, 0.24f, 0f),
                      at + new Vector3(-0.13f, 0.34f, 0f), 0.010f, mTetsu);
        NiwaBuhin.Bou(d, "Naya_Jouro_Te", at + new Vector3(-0.13f, 0.34f, 0f),
                      at + new Vector3(0.02f, 0.36f, 0f), 0.010f, mTetsu);
    }

    /// <summary>脚立（生垣の 上を 刈る ときの 台）。壁に 立てかけて たたんだ まま</summary>
    static void Kyatatsu(Transform d, Vector3 at) {
        var mAshi = NiwaBuhin.Fit("NayaKyatatsu", "shashin/ie_ki.jpg", 1.5f, 0.07f, TM_KI,
                                  new Color(0.92f, 0.86f, 0.75f));
        for (int s = -1; s <= 1; s += 2) {
            var a = at + new Vector3(s * 0.17f, 0f, 0f);
            var b = at + new Vector3(s * 0.17f, 1.46f, 0.30f);
            NiwaBuhin.Bou(d, soto + "Naya_Kyatatsu_Ashi", a, b, 0.026f, mAshi);
        }
        for (int i = 1; i <= 3; i++) {
            float k = i / 4f;
            var y = Vector3.Lerp(at, at + new Vector3(0f, 1.46f, 0.30f), k);
            NiwaBuhin.Hako(d, soto + "Naya_Kyatatsu_Dan", y, new Vector3(0.36f, 0.035f, 0.13f), mAshi);
        }
    }
}
