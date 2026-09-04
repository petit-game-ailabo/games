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
//   ・入口は 引き戸 2枚。開けっぱなしで つかう
//   ・床は 張らない **土間**。地面の 湿気を 切る ため 石の 布基礎に 乗る
//   ・道具は 立てかける（柄の 長い ものは 壁ぎわ）／棚に 置く（かご・小道具）
//
// 置きかた：庭の 段の 西の はし。生垣（x=-9.7・厚み0.9＝内がわの 面が -9.25）の すぐ 内。
//
// ★戸は **南**（＝カメラがわ）を 向ける＝平入り。はじめ 妻入り（戸を 東に）で 組んだら、
//   この 企画の カメラは **いつも 南から 北を 向く**ので 戸の 面が どこからも 真横にしか
//   見えず、中の 道具が 1つも 映らなかった（2026-09-05）。軒（0.42m）が 戸に かかる ので
//   庇も 要らなく なった。
//
// ★中には まだ 入れない（戸口に 見えない かべ）。カメラ が 小屋の 中に 入ると
//   ニアクリップの 向こうで 壁が 描かれず 画面が こわれる（natsuyasumi スキルの
//   「カメラが 物に じゃまされる とき」の ★3だけでは 足りない）。道具を 取る 遊びを
//   入れる ときに 屋内カメラごと 作る。
public static class NiwaNaya {
    // ---- 世界での 置き場所（BuildNiwa から 参照する）
    public const float CX = -7.75f, CZ = 2.30f;      // 小屋の まん中
    public const float HX = 1.30f, HZ = 1.65f;       // 壁しんの 半分（東西 2.6m x 南北 3.3m）
    /// <summary>戸口の 前の 立ち位置（地面の 絵に 踏み跡を 描く）</summary>
    public static Vector3 Guchi { get { return new Vector3(CX + 0.05f, 0f, CZ - HZ - 0.85f); } }

    const float ATSU = 0.09f;      // 板壁の 厚み
    const float DODAI = 0.24f;     // 石の 布基礎の 天
    // ★軒げたの 天は **屋根の 裏より 下**。壁しん(z=HZ)での 屋根裏は
    //   MUNE - (MUNE-YNOKI)*HZ/(HZ+DE) - 屋根の厚み0.15 = 2.221m。
    //   ここを 外すと 妻がわの 壁が 軒の すみで 屋根を つきぬける
    const float NOKI = 2.15f;      // 壁の 天（軒げた の 上ば）
    const float MUNE = 3.20f;      // 棟
    const float YNOKI = 2.16f;     // 軒先（z=±(HZ+DE)）
    const float DE = 0.42f;        // 軒の 出（南北）
    const float TO_H = 0.72f;      // 戸口の 半分（1.44m）
    const float TO_Y = 1.98f;      // 戸の 高さ

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
        mNuno = MatIro("NayaAmi", new Color(0.86f, 0.87f, 0.82f), 0.10f);
        mTsuchi = MatIro("NayaDoma", new Color(0.30f, 0.26f, 0.21f), 0.02f);

        // ---- 石の 布基礎（土間の 湿気を 切る）
        mIshi = NiwaBuhin.Fit("NayaIshi", "shashin/ishigaki.jpg", 2.9f, 0.36f, TM_ISHI,
                              new Color(0.98f, 0.97f, 0.94f));
        NiwaBuhin.Hako(t, "Naya_Dodai", new Vector3(0f, DODAI * 0.5f - 0.06f, 0f),
                       new Vector3(HX * 2f + 0.16f, DODAI + 0.12f, HZ * 2f + 0.16f), mIshi);
        // 土間（中の 地めん。板を 敷かない ぶん 影で 暗い）
        NiwaBuhin.Hako(t, "Naya_Doma", new Vector3(0f, DODAI - 0.02f, 0f),
                       new Vector3(HX * 2f - ATSU, 0.04f, HZ * 2f - ATSU), mTsuchi);

        // ---- 板壁（下見板。母屋の 腰板と 同じ 絵）
        Kabe(t, "Naya_KabeW", -HX, -HX, -HZ, HZ, DODAI, NOKI);            // 西（妻がわ）
        Kabe(t, "Naya_KabeE", HX, HX, -HZ, HZ, DODAI, NOKI);              // 東（妻がわ）
        Kabe(t, "Naya_KabeN", -HX, HX, HZ, HZ, DODAI, NOKI);              // 北（軒がわ）
        Kabe(t, "Naya_KabeS1", -HX, -TO_H, -HZ, -HZ, DODAI, NOKI);        // 南の 袖壁（戸の 両わき）
        Kabe(t, "Naya_KabeS2", TO_H, HX, -HZ, -HZ, DODAI, NOKI);
        Kabe(t, "Naya_KabeS3", -TO_H, TO_H, -HZ, -HZ, TO_Y, NOKI);        // 戸の 上の 小壁

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

        // ---- 中の 道具
        Dougu(t);

        // ---- 当たりは **小屋 まるごと 1つの 箱**（壁を 1枚ずつ 当たりに すると
        //      戸口の すきまに はさまる。中に 入れる ように する のは 屋内カメラを 作る とき）
        var blk = NiwaBuhin.Hako(t, "BLK_Naya", new Vector3(0f, (NOKI + 0.1f) * 0.5f, 0f),
                                 new Vector3(HX * 2f + 0.2f, NOKI + 0.1f, HZ * 2f + 0.2f), null, true);
        blk.GetComponent<Renderer>().enabled = false;

        return t;
    }

    /// <summary>下見板の 壁 1面（x か z の どちらかが 同じ 2点で 面を 決める）</summary>
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
                          new Color(k, k * 0.97f, k * 0.92f), true));
    }

    /// <summary>引き戸 2枚。手前(東)の 1枚を 北へ 引いて あり、南がわが 開いて いる</summary>
    static void Do(Transform t) {
        mTo = NiwaBuhin.Fit("NayaTo", "shashin/ie_tobukuro.jpg", 0.76f, 1.66f, TM_TO,
                            new Color(0.98f, 0.93f, 0.85f));
        var mWaku = NiwaBuhin.Fit("NayaWaku", "shashin/ie_ki.jpg", 1.6f, 0.09f, TM_KI,
                                  new Color(0.92f, 0.86f, 0.78f));
        // 敷居と 鴨居
        NiwaBuhin.Hako(t, "Naya_Shikii", new Vector3(0f, DODAI + 0.03f, -HZ + 0.02f),
                       new Vector3(TO_H * 2f + 0.14f, 0.07f, 0.16f), mWaku);
        NiwaBuhin.Hako(t, "Naya_Kamoi", new Vector3(0f, TO_Y - 0.04f, -HZ + 0.02f),
                       new Vector3(TO_H * 2f + 0.14f, 0.09f, 0.16f), mWaku);
        // 戸 2枚（外の 溝＝手前 / 内の 溝）。西がわの 1枚に 東の 1枚を 重ねて 開けて ある
        float y0 = DODAI + 0.06f, y1 = TO_Y - 0.07f;
        Ita1(t, "Naya_ToA", -HZ - 0.015f, -0.35f, 0.74f, y0, y1);    // 閉じて いる 1枚
        Ita1(t, "Naya_ToB", -HZ + 0.045f, -0.31f, 0.74f, y0, y1);    // 引いて 重ねた 1枚
        // 引き手（黒い 小さな くぼみ）
        NiwaBuhin.Hako(t, "Naya_Hikite", new Vector3(-0.02f, 1.05f, -HZ - 0.03f),
                       new Vector3(0.05f, 0.13f, 0.02f), mTetsu);
    }

    static void Ita1(Transform t, string name, float z, float xc, float haba, float y0, float y1) {
        NiwaBuhin.Hako(t, name, new Vector3(xc, (y0 + y1) * 0.5f, z),
                       new Vector3(haba, y1 - y0, 0.035f), mTo);
        // 桟（框と 中桟）。板だけだと のっぺりした 1枚の 板に 見える
        var mSan = NiwaBuhin.Fit("NayaSan", "shashin/ie_ki.jpg", haba, 0.08f, TM_KI,
                                 new Color(0.86f, 0.80f, 0.71f));
        foreach (float y in new[] { y0 + 0.05f, (y0 + y1) * 0.5f, y1 - 0.05f })
            NiwaBuhin.Hako(t, name + "_San", new Vector3(xc, y, z - 0.026f),
                           new Vector3(haba, 0.08f, 0.02f), mSan);
        foreach (float s in new[] { -1f, 1f })
            NiwaBuhin.Hako(t, name + "_Kabetsuki_Kamachi",
                           new Vector3(xc + s * (haba * 0.5f - 0.04f), (y0 + y1) * 0.5f, z - 0.026f),
                           new Vector3(0.08f, y1 - y0, 0.02f), mSan);
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

        // ---- 虫かご（上の 棚。木の わく に 竹の 立子）
        {
            float kx = -0.30f, ky = 1.365f, kz = 1.12f;
            NiwaBuhin.Hako(d, "Naya_Kago_Soko", new Vector3(kx, ky + 0.02f, kz),
                           new Vector3(0.24f, 0.04f, 0.20f), mIta2);
            NiwaBuhin.Hako(d, "Naya_Kago_Ten", new Vector3(kx, ky + 0.30f, kz),
                           new Vector3(0.24f, 0.04f, 0.20f), mIta2);
            for (int i = 0; i < 6; i++) {
                float u = -0.10f + i * 0.04f;
                foreach (float sz in new[] { -0.09f, 0.09f })
                    NiwaBuhin.Bou(d, "Naya_Kago_Ko", new Vector3(kx + u, ky + 0.04f, kz + sz),
                                  new Vector3(kx + u, ky + 0.28f, kz + sz), 0.006f, mTake);
            }
            foreach (float sx in new[] { -0.115f, 0.115f })
                for (int i = 0; i < 4; i++) {
                    float u = -0.075f + i * 0.05f;
                    NiwaBuhin.Bou(d, "Naya_Kago_Ko", new Vector3(kx + sx, ky + 0.04f, kz + u),
                                  new Vector3(kx + sx, ky + 0.28f, kz + u), 0.006f, mTake);
                }
        }

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

        // ---- 虫取り網（東の 壁ぎわ。輪は 柄に 直角、網は その 先へ すぼまる）
        {
            var a = new Vector3(1.05f, DODAI, 0.10f);
            var b = new Vector3(1.14f, 1.76f, 0.62f);
            NiwaBuhin.Bou(d, "Naya_Ami_E", a, b, 0.014f, mTake);
            var dir = (b - a).normalized;
            var wa = NiwaBuhin.Mesh1(d, "Naya_Ami_Wa", NiwaBuhin.Wa("NayaAmiWa", 0.185f, 0.011f),
                                     mTetsu);
            wa.transform.localPosition = b + dir * 0.02f;
            wa.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
            var ami = NiwaBuhin.Mesh1(d, "Naya_Ami_Nuno",
                                      NiwaBuhin.Tsutsu("NayaAmiNuno", 0.185f, 0.03f, 0.30f, false, 0.2f, 12),
                                      mNuno);
            ami.transform.localPosition = b + dir * 0.02f;
            ami.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
        }

        // ---- 竹ぼうき（戸口の 内がわに 立てかける）
        Houki(d, new Vector3(1.08f, DODAI, 1.34f), new Vector3(1.16f, 1.52f, 1.02f));
        // ---- 刈込ばさみ（生垣の 整理用。西の 壁ぎわ）
        {
            var a = new Vector3(-1.06f, DODAI, 0.62f);
            var b = new Vector3(-0.94f, 1.06f, 0.78f);
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

        // ---- 外に 立てかけた もの（南の 壁・戸口の 両わき）：脚立と 竹ぼうき
        {
            float gy = NiwaJimenE.Takasa(CX + 0.2f, CZ - HZ - 0.3f) - NiwaJimenE.Takasa(CX, CZ);
            Kyatatsu(d, new Vector3(-1.00f, gy, -HZ - 0.26f));
            Houki(d, new Vector3(1.02f, gy, -HZ - 0.30f), new Vector3(0.98f, 1.44f, -HZ - 0.04f));
        }
    }

    static void Houki(Transform d, Vector3 a, Vector3 b) {
        NiwaBuhin.Bou(d, "Naya_Houki_E", a + (b - a).normalized * 0.32f, b, 0.017f, mTake);
        var dir = (b - a).normalized;
        for (int i = 0; i < 11; i++) {                 // 穂＝細い 竹を ひろげる
            float w = (i / 10f - 0.5f) * 0.26f;
            var yoko = Vector3.Cross(dir, Vector3.forward).normalized;
            NiwaBuhin.Bou(d, "Naya_Houki_Ho", a + yoko * w + dir * 0.02f,
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
            NiwaBuhin.Bou(d, "Naya_Kyatatsu_Ashi", a, b, 0.026f, mAshi);
        }
        for (int i = 1; i <= 3; i++) {
            float k = i / 4f;
            var y = Vector3.Lerp(at, at + new Vector3(0f, 1.46f, 0.30f), k);
            NiwaBuhin.Hako(d, "Naya_Kyatatsu_Dan", y, new Vector3(0.36f, 0.035f, 0.13f), mAshi);
        }
    }
}
