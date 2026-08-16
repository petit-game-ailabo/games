// 画面の 左(+X)に、借りものの アセットで 町を ならべる（2026-08-16・本人の 依頼）。
//
// 「全部、今の建物の左側に建てていって。その分マップを左に広げよう」
//   → TerrainGen の PlayMaxX を 26→53、平らな ところを x=49 まで のばし、
//     本道(z=7)を x=50 まで のばして 枝道を 4本 足した（TerrainGen.Paths）。
//
// ならび（画面の 右から 左へ ＝ x が 小さい 順）
//   x=11 … 石づくりの 家（BuildNayaKit。納屋の あと）
//   x=20 … 開けっぱなしの 小屋
//   x=26 … 門（本道を またぐ）
//   x=31 … **せり出し(ジェッティ)の 家**＋バルコニー＋屋根裏の 出窓。このキットの 見せ場
//   x=41 … 高い 基壇の 家
//   x=47 … 塔
//
// ★このキットの くせは MegaKit.Put の コメントに 書いた（ねている／100倍）。
//   置くときは **必ず MegaKit.Put を 通す**。
using UnityEngine;

public static class BuildKitTown {

    const float G = 2.0f;        // わりつけ
    const float SH = 3.12f;      // 1階ぶんの 高さ

    public static void Build(Transform parent) {
        var root = new GameObject("KitTown").transform;
        root.SetParent(parent, false);

        Koya(root, 20f, 0.5f);
        Mon(root, 26f, 7f);
        Jetty(root, 31f, -2.0f);
        Kidan(root, 41f, -1.5f);
        Tou(root, 47f, -2.5f);

        Debug.Log("[KitTown] 左の 町を 建てた: " + root.GetComponentsInChildren<MeshRenderer>().Length + " まとまり");
    }

    // ---------------------------------------------------------------- 小屋
    // 柱4本＋板の 屋根の、壁の 無い 小屋。農具小屋の たぐい
    static void Koya(Transform parent, float cx, float cz) {
        var t = Site(parent, "Kit_Koya", cx, cz);
        const float hx = 2.0f, hz = 2.0f;
        // 柱
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                MegaKit.Put(t, "Corner_Exterior_Wood", new Vector3(sx * hx, 0f, sz * hz));
        // おくの 壁だけ 立てる（風よけ）。手前と 左右は あけて 中が 見える
        MegaKit.Put(t, "Wall_UnevenBrick_Straight", new Vector3(-1f, 0f, -hz), 0f);
        MegaKit.Put(t, "Wall_UnevenBrick_Straight", new Vector3( 1f, 0f, -hz), 0f);
        // 板の 屋根。2x1_Middle は 2m x 2m の 平らな 板
        for (int i = -1; i <= 1; i += 2)
            for (int j = -1; j <= 1; j += 2)
                MegaKit.Put(t, "Roof_Wooden_2x1_Middle", new Vector3(i * 1.0f, SH, j * 1.0f));
        // 屋根の ふち
        MegaKit.Put(t, "Roof_Wooden_2x1_L", new Vector3(-1f, SH, hz), 180f);
        MegaKit.Put(t, "Roof_Wooden_2x1_R", new Vector3( 1f, SH, hz), 180f);
        // 中の もの
        MegaKit.Put(t, "Prop_Crate", new Vector3(-1.2f, 0.05f, -1.2f), 14f);
        MegaKit.Put(t, "Prop_Crate", new Vector3(-1.2f, 1.10f, -1.1f), -8f);
        MegaKit.Put(t, "Prop_Wagon", new Vector3(1.1f, 0.05f, 0.2f), 96f);
        // あたりは 柱と おくの 壁だけ（中は 通りぬけ できる）
        MegaKit.Hit(t, new Vector3(0f, 1.6f, -hz), new Vector3(hx * 2f + 0.4f, 3.2f, 0.4f));
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                MegaKit.Hit(t, new Vector3(sx * hx, 1.6f, sz * hz), new Vector3(0.35f, 3.2f, 0.35f));
    }

    // ---------------------------------------------------------------- 門
    // 本道を またぐ。**道は ふさがない**（柱の あいだを 4m あける）
    static void Mon(Transform parent, float cx, float cz) {
        var t = Site(parent, "Kit_Mon", cx, cz);
        const float half = 2.6f;
        for (int s = -1; s <= 1; s += 2) {
            MegaKit.Put(t, "Corner_Exterior_Brick", new Vector3(s * half, 0f, -0.25f), s > 0 ? 270f : 90f);
            MegaKit.Put(t, "Corner_Exterior_Brick", new Vector3(s * half, 0f, 0.25f), s > 0 ? 180f : 0f);
            MegaKit.Hit(t, new Vector3(s * half, 1.6f, 0f), new Vector3(0.7f, 3.2f, 1.0f));
        }
        // アーチの かざり（うすい 板なので 両がわに 貼る）
        for (int i = -1; i <= 1; i += 2)
            for (float x = -1.2f; x <= 1.3f; x += 2.4f)
                MegaKit.Put(t, "Wall_Arch", new Vector3(x, 0f, i * 0.30f), i > 0 ? 180f : 0f);
        // 上に かわらの 笠
        MegaKit.Put(t, "Roof_RoundTile_2x1", new Vector3(-1.6f, SH - 0.1f, 0f), 180f);
        MegaKit.Put(t, "Roof_RoundTile_2x1", new Vector3( 1.6f, SH - 0.1f, 0f), 180f);
        MegaKit.Put(t, "Prop_Vine1", new Vector3(-half, SH - 0.3f, 0.1f), 90f);
    }

    // ---------------------------------------------------------------- せり出しの 家
    // ★このキットの **見せ場**。2階が 手前へ 2m せり出す（ジェッティ）。
    //   Overhang_Plaster_Long は「壁＋せり出した 床」が ひとかたまりに なった 部品で、
    //   -Z 方向へ 2m 出る。yaw=180 で 手前(+Z)へ 出す
    static void Jetty(Transform parent, float cx, float cz) {
        var t = Site(parent, "Kit_Jetty", cx, cz);
        const int NX = 3, NZ = 4;
        float hx = NX * G * 0.5f, hz = NZ * G * 0.5f;   // 3.0 x 4.0

        Pad(t, NX * G + 0.9f, NZ * G + 0.9f);
        for (int i = 0; i < NX; i++)
            for (int j = 0; j < NZ; j++)
                MegaKit.Put(t, "Floor_UnevenBrick", new Vector3(-hx + G * (i + 0.5f), 0.02f, -hz + G * (j + 0.5f)));

        // --- 1階＝石づみ
        MegaKit.Put(t, "Wall_UnevenBrick_Straight",         -G, 0f, hz, 180f);
        MegaKit.Put(t, "Wall_UnevenBrick_Door_Round",        0f, 0f, hz, 180f);
        MegaKit.Put(t, "Wall_UnevenBrick_Window_Wide_Round", G, 0f, hz, 180f);
        MegaKit.Put(t, "DoorFrame_Round_WoodDark", new Vector3(0f, 0f, hz), 180f);
        MegaKit.Put(t, "Door_2_Round", new Vector3(-0.55f, 0f, hz - 0.06f), 180f);
        MegaKit.Put(t, "Window_Wide_Round1", new Vector3(G, 0f, hz), 180f);
        for (int i = 0; i < NX; i++)
            MegaKit.Put(t, "Wall_UnevenBrick_Straight", -hx + G * (i + 0.5f), 0f, -hz, 0f);
        for (int j = 0; j < NZ; j++) {
            float z = -hz + G * (j + 0.5f);
            MegaKit.Put(t, j == 1 ? "Wall_UnevenBrick_Window_Wide_Round" : "Wall_UnevenBrick_Straight",
                        -hx, 0f, z, 90f);
            MegaKit.Put(t, j == 2 ? "Wall_UnevenBrick_Window_Wide_Round" : "Wall_UnevenBrick_Straight",
                         hx, 0f, z, 270f);
        }
        MegaKit.Put(t, "Window_Wide_Round1", new Vector3(-hx, 0f, -hz + G * 1.5f), 90f);
        MegaKit.Put(t, "Window_Wide_Round1", new Vector3( hx, 0f, -hz + G * 2.5f), 270f);

        // --- 2階＝**手前へ せり出す**
        for (int i = 0; i < NX; i++)
            MegaKit.Put(t, i == 1 ? "Overhang_Plaster_Short" : "Overhang_Plaster_Long",
                        new Vector3(-hx + G * (i + 0.5f), SH, hz), 180f);
        // せり出しの 横がわ（左右の はしを ふさぐ）
        MegaKit.Put(t, "Overhang_Side_Plaster_Long_L", new Vector3(-hx, 0f, hz), 180f);
        MegaKit.Put(t, "Overhang_Side_Plaster_Long_R", new Vector3( hx, 0f, hz), 180f);
        // せり出しの 底（下から 見上げた とき すけない ように）
        for (int i = 0; i < NX; i++)
            MegaKit.Put(t, "Overhang_Roof_Plaster", new Vector3(-hx + G * (i + 0.5f), SH, hz + 1.0f), 180f);
        // 2階の のこりの 面＝ハーフティンバー
        for (int i = 0; i < NX; i++)
            MegaKit.Put(t, "Wall_Plaster_WoodGrid", -hx + G * (i + 0.5f), SH, -hz, 0f);
        for (int j = 0; j < NZ; j++) {
            float z = -hz + G * (j + 0.5f);
            MegaKit.Put(t, j == 1 ? "Wall_Plaster_Window_Wide_Round" : "Wall_Plaster_WoodGrid", -hx, SH, z, 90f);
            MegaKit.Put(t, j == 2 ? "Wall_Plaster_Window_Wide_Round" : "Wall_Plaster_WoodGrid",  hx, SH, z, 270f);
        }
        MegaKit.Put(t, "Window_Wide_Round1", new Vector3(-hx, SH, -hz + G * 1.5f), 90f);
        MegaKit.Put(t, "Window_Wide_Round1", new Vector3( hx, SH, -hz + G * 2.5f), 270f);

        // --- バルコニー（せり出しの 先に つける）
        for (int i = -1; i <= 1; i += 2)
            MegaKit.Put(t, "Balcony_Simple_Straight", new Vector3(i * 1.0f, SH + 1.0f, hz + 2.0f), 180f);
        MegaKit.Put(t, "Balcony_Simple_Corner", new Vector3(-hx, SH + 1.0f, hz + 2.0f), 180f);

        // --- 隅の 柱
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2) {
                float yaw = Corner(sx, sz);
                MegaKit.Put(t, "Corner_Exterior_Brick", new Vector3(sx * hx, 0f, sz * hz), yaw);
                if (sz < 0) MegaKit.Put(t, "Corner_Exterior_Wood", new Vector3(sx * hx, SH, sz * hz), yaw);
            }

        // --- 屋根。せり出した ぶん 手前へ ずらして 大きめの ものを かける
        float yr = SH * 2f;
        MegaKit.Put(t, "Roof_RoundTiles_6x10", new Vector3(0f, yr, 0.9f));
        MegaKit.Put(t, "Roof_Front_Brick6", new Vector3(0f, yr, hz + 1.9f), 180f);
        MegaKit.Put(t, "Roof_Front_Brick6", new Vector3(0f, yr, -hz), 0f);
        // 屋根裏の 出窓（ドーマー）
        MegaKit.Put(t, "Roof_Dormer_RoundTile", new Vector3(-1.6f, yr + 0.4f, hz + 0.6f), 180f);
        MegaKit.Put(t, "Prop_Chimney", new Vector3(hx - 0.6f, yr - 0.4f, -hz + 1.8f));
        MegaKit.Put(t, "Prop_Vine2", new Vector3(-hx, SH + 2.2f, -hz + G * 1.0f), 90f);

        // --- 中：階段で 2階へ 上がれる。床には 穴を あける
        MegaKit.Put(t, "Stair_Interior_Simple", new Vector3(hx - 1.0f, 0.03f, -hz + 0.4f), 0f);
        for (int i = 0; i < NX; i++)
            for (int j = 0; j < NZ; j++) {
                if (i == NX - 1 && j <= 1) continue;              // 階段の 穴
                MegaKit.Put(t, "Floor_WoodDark", new Vector3(-hx + G * (i + 0.5f), SH, -hz + G * (j + 0.5f)));
            }
        MegaKit.Put(t, "HoleCover_Straight", new Vector3(hx - 1.0f, SH, -hz + G * 2f), 0f);
        Lamp(t, new Vector3(0f, 2.0f, -0.5f));
        Lamp(t, new Vector3(0f, SH + 2.0f, 0.5f));

        Walls(t, hx, hz, SH * 2f, 0.62f);
    }

    // ---------------------------------------------------------------- 高い 基壇の 家
    static void Kidan(Transform parent, float cx, float cz) {
        var t = Site(parent, "Kit_Kidan", cx, cz);
        const int NX = 3, NZ = 3;
        float hx = NX * G * 0.5f, hz = NZ * G * 0.5f;   // 3.0 x 3.0
        const float lift = 1.0f;                        // 基壇の 高さ

        // 基壇＝1m の 石の 台を しきつめる
        for (int i = 0; i < NX; i++)
            for (int j = 0; j < NZ; j++)
                MegaKit.Put(t, "Stairs_Exterior_Platform",
                            new Vector3(-hx + G * (i + 0.5f), 0f, -hz + G * (j + 0.5f)));
        Pad(t, NX * G + 0.2f, NZ * G + 0.2f);          // 台の すきま うめ
        for (int i = 0; i < NX; i++)
            for (int j = 0; j < NZ; j++)
                MegaKit.Put(t, "Floor_WoodDark", new Vector3(-hx + G * (i + 0.5f), lift + 0.02f, -hz + G * (j + 0.5f)));

        // 登る 階段（手前の まん中）
        MegaKit.Put(t, "Stairs_Exterior_Straight", new Vector3(0f, 0f, hz + 1.0f), 180f);

        // 壁
        MegaKit.Put(t, "Wall_Plaster_Straight_Base",       -G, lift, hz, 180f);
        MegaKit.Put(t, "Wall_Plaster_Door_RoundInset",     0f, lift, hz, 180f);
        MegaKit.Put(t, "Wall_Plaster_Window_Wide_Flat",     G, lift, hz, 180f);
        MegaKit.Put(t, "DoorFrame_Round_WoodDark", new Vector3(0f, lift, hz), 180f);
        MegaKit.Put(t, "Door_2_Round", new Vector3(-0.55f, lift, hz - 0.06f), 180f);
        MegaKit.Put(t, "Window_Wide_Flat1", new Vector3(G, lift, hz), 180f);
        MegaKit.Put(t, "WindowShutters_Wide_Flat_Open", new Vector3(G, lift, hz), 180f);
        for (int i = 0; i < NX; i++)
            MegaKit.Put(t, "Wall_Plaster_Straight_Base", -hx + G * (i + 0.5f), lift, -hz, 0f);
        for (int j = 0; j < NZ; j++) {
            float z = -hz + G * (j + 0.5f);
            MegaKit.Put(t, j == 1 ? "Wall_Plaster_Window_Wide_Flat" : "Wall_Plaster_Straight_Base", -hx, lift, z, 90f);
            MegaKit.Put(t, j == 1 ? "Wall_Plaster_Window_Wide_Flat" : "Wall_Plaster_Straight_Base",  hx, lift, z, 270f);
        }
        MegaKit.Put(t, "Window_Wide_Flat1", new Vector3(-hx, lift, 0f), 90f);
        MegaKit.Put(t, "Window_Wide_Flat1", new Vector3( hx, lift, 0f), 270f);
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                MegaKit.Put(t, "Corner_Exterior_Wood", new Vector3(sx * hx, lift, sz * hz), Corner(sx, sz));

        MegaKit.Put(t, "Roof_RoundTiles_6x6", new Vector3(0f, lift + SH, 0f));
        MegaKit.Put(t, "Roof_Front_Brick6", new Vector3(0f, lift + SH, hz), 180f);
        MegaKit.Put(t, "Roof_Front_Brick6", new Vector3(0f, lift + SH, -hz), 0f);
        MegaKit.Put(t, "Prop_Chimney", new Vector3(-hx + 0.6f, lift + SH - 0.4f, -hz + 1.4f));
        MegaKit.Put(t, "Prop_Crate", new Vector3(hx + 1.4f, 0.05f, hz - 0.4f), 18f);
        Lamp(t, new Vector3(0f, lift + 1.9f, 0f));

        Walls(t, hx, hz, SH, 0.62f, lift);
        // 基壇 そのものの あたり（台の 上に 乗れる ように）
        MegaKit.Hit(t, new Vector3(0f, lift * 0.5f, 0f), new Vector3(NX * G, lift, NZ * G));
    }

    // ---------------------------------------------------------------- 塔
    // ★丸い 壁の 部品は 無い ので、**まっすぐな 壁を 八角に ならべる**。
    //   1辺 2m の 八角形は、中心から 辺までが 2 / (2*tan(22.5)) = 2.414m
    static void Tou(Transform parent, float cx, float cz) {
        var t = Site(parent, "Kit_Tou", cx, cz);
        float ap = 1f / Mathf.Tan(22.5f * Mathf.Deg2Rad);   // 2.414
        Pad(t, 6.2f, 6.2f);
        for (int floor = 0; floor < 2; floor++) {
            float y = floor * SH;
            for (int i = 0; i < 8; i++) {
                float th = i * 45f, r = th * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Sin(r) * ap, y, Mathf.Cos(r) * ap);
                // 外がわは -Z。yaw=180+th で 角度 th の 向きを 外に 向ける
                string piece = (floor == 0 && i == 0) ? "Wall_UnevenBrick_Door_Round"
                             : (i % 2 == 1) ? "Wall_UnevenBrick_Window_Thin_Round"
                             : "Wall_UnevenBrick_Straight";
                MegaKit.Put(t, piece, p, 180f + th);
                if (floor == 0 && i == 0) {
                    MegaKit.Put(t, "DoorFrame_Round_WoodDark", p, 180f + th);
                    MegaKit.Put(t, "Door_2_Round", p + new Vector3(-0.55f, 0f, -0.06f), 180f + th);
                }
                // あたり（戸口の ぶんは あける）
                if (!(floor == 0 && i == 0))
                    MegaKit.Hit(t, new Vector3(p.x, SH, p.z), new Vector3(2.1f, SH * 2f, 0.4f));
            }
        }
        MegaKit.Put(t, "Roof_Tower_RoundTiles", new Vector3(0f, SH * 2f, 0f));
        MegaKit.Put(t, "Prop_MetalFence_Simple", new Vector3(0f, SH * 2f + 6.4f, 0f));
        for (int i = 0; i < 3; i++)
            MegaKit.Put(t, "Prop_Brick" + (i + 1), new Vector3(3.4f + i * 0.5f, 0.05f, 2.6f - i * 0.7f), i * 40f);
        Lamp(t, new Vector3(0f, 2.0f, 0f));
    }

    // ---------------------------------------------------------------- 道具
    /// <summary>建てる 土地を つくる。**地めんの 高さは 数字で とる**（絵では 読めない）</summary>
    static Transform Site(Transform parent, string name, float cx, float cz) {
        float y = -999f;
        for (int i = -3; i <= 3; i++)
            for (int j = -3; j <= 3; j++)
                y = Mathf.Max(y, TerrainGen.Height(cx + i * 1.6f, cz + j * 1.6f));
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = new Vector3(cx, y, cz);
        return t;
    }

    /// <summary>地めんとの すきまを うめる 石の 台。**FBX に あたりが 無い ので 床も かねる**</summary>
    static void Pad(Transform t, float w, float d) {
        var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pad.name = "Kit_Base";
        pad.transform.SetParent(t, false);
        const float h = 0.60f;
        pad.transform.localPosition = new Vector3(0f, 0.03f - h * 0.5f, 0f);
        pad.transform.localScale = new Vector3(w, h, d);
        pad.GetComponent<Renderer>().sharedMaterial = MegaKit.Mat("MI_RockTrim");
    }

    /// <summary>四方の 壁の あたり。**手前の まん中＝戸口の ぶんだけ あける**</summary>
    static void Walls(Transform t, float hx, float hz, float h, float door, float lift = 0f) {
        const float T = 0.40f;
        float cy = lift + h * 0.5f;
        MegaKit.Hit(t, new Vector3(0f, cy, -hz), new Vector3(hx * 2f + T, h, T));
        MegaKit.Hit(t, new Vector3(-hx, cy, 0f), new Vector3(T, h, hz * 2f + T));
        MegaKit.Hit(t, new Vector3( hx, cy, 0f), new Vector3(T, h, hz * 2f + T));
        float sideW = hx - door;
        MegaKit.Hit(t, new Vector3(-(door + sideW * 0.5f), cy, hz), new Vector3(sideW, h, T));
        MegaKit.Hit(t, new Vector3( (door + sideW * 0.5f), cy, hz), new Vector3(sideW, h, T));
    }

    /// <summary>隅の 柱の 向き。部品は (-X,-Z) の かどむけ</summary>
    static float Corner(int sx, int sz) {
        return (sx < 0 && sz > 0) ? 90f : (sx > 0 && sz > 0) ? 180f : (sx > 0 && sz < 0) ? 270f : 0f;
    }

    static void Lamp(Transform t, Vector3 pos) {
        var go = new GameObject("Kit_Light");
        go.transform.SetParent(t, false);
        go.transform.localPosition = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.84f, 0.58f);
        l.intensity = 3.6f; l.range = 9f; l.shadows = LightShadows.None;
    }
}
