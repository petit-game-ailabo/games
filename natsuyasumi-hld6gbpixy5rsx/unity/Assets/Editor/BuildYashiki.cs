// 屋敷（やしき）＝**母屋のまわり ひとそろい**。（2026-08-17・本人の 依頼）
//
// 本人「田舎の家って庭もある、石垣みたいなもので仕切っている中に母屋と離れがあって、
//       倉庫もあって、庭もあって、とにかく広いはずだよ」
//
// ★調べた こと
//  - 田舎の 一けんは **敷地 300〜1000坪（1000〜3300平米）**が ふつう。
//    その中に 母屋・蔵(くら)・納屋・離れ・便所・風呂・前庭・裏庭 が おさまる。
//  - **前庭(まえにわ)は「作業庭」**。花を 植える 庭では なく、**土のまま**の 広場で、
//    もみを ほし、農具を ひろげ、車を 止める。母屋の 土間の 前に つづく。
//  - **裏庭**は 台所に つづく 菜園。井戸・物ほし・つけもの樽。
//  - **屋敷林（やしきりん・いぐね）**：北がわと 西がわに 木を 列に 植えて 風を よける。
//    これが ある だけで「昔からの 家」に 見える。
//  - 囲いは **石垣＋生垣**、もしくは 板塀。門は 一か所。
//  - **蔵**は 白い 漆喰の 壁・小さな 窓・重い 扉。母屋とは わざと 離して 建てる（火よけ）。
//  - **離れ**は 隠居や 客の ための 小さな 棟。渡り廊下で つながる ことも ある。
//
// この 屋敷の 大きさ：x -32〜18、z -13〜13 ＝ 50 x 26m ＝ 1300平米（約390坪）。
using UnityEngine;

public static class BuildYashiki {

    // 敷地の 四すみ
    public const float SX0 = -32f, SX1 = 18f;
    public const float SZ0 = -13f, SZ1 = 13f;
    public const float GateX = 8.25f;        // 門（母屋の 玄関の 正面）

    public struct Mats {
        public Material stone, wood, plaster, floor, roof, post, tatami;
        public Material roofM, woodM;
        public System.Func<float, float, Material> plasterFit, woodFit, koshiFit;
    }

    public static void Build(Transform parent, Mats m,
                             System.Func<string, Transform, Vector3, Vector3, Material, GameObject> box,
                             System.Action<string, Vector3, int, float> plant) {
        var root = new GameObject("Yashiki").transform;
        root.SetParent(parent, false);

        Ishigaki(root, m, box);
        Kura(root, m, box);
        Hanare(root, m, box);
        Niwa(root, m, box);
        Yashikirin(root, plant);

        Debug.Log("[Yashiki] 屋敷ひとそろい: " + root.GetComponentsInChildren<MeshRenderer>().Length + " まとまり");
    }

    // ---------------------------------------------------------------- 石垣と 門
    static void Ishigaki(Transform root, Mats m,
                         System.Func<string, Transform, Vector3, Vector3, Material, GameObject> box) {
        const float H = 0.85f, T = 0.55f, Step = 2.0f;
        var mat = m.stone;
        // 手前(+Z)。門の ぶんだけ あける
        for (float x = SX0; x < SX1; x += Step) {
            if (Mathf.Abs(x + Step * 0.5f - GateX) < 2.4f) continue;      // 門の 口
            Wall(box, root, "Ig_F", x, SZ1, Step, T, H, mat);
        }
        // おく(-Z)・左(-X)・右(+X)
        for (float x = SX0; x < SX1; x += Step) Wall(box, root, "Ig_B", x, SZ0, Step, T, H, mat);
        for (float z = SZ0; z < SZ1; z += Step) WallZ(box, root, "Ig_L", SX0, z, Step, T, H, mat);
        for (float z = SZ0; z < SZ1; z += Step) WallZ(box, root, "Ig_R", SX1, z, Step, T, H, mat);

        // 門（薬医門ふうの 簡単な もの）。柱2本＋かさ木＋瓦の 屋根
        float gy = TerrainGen.Height(GateX, SZ1);
        for (int s = -1; s <= 1; s += 2) {
            box("Mon_Hashira" + s, root, new Vector3(GateX + s * 2.1f, gy + 1.5f, SZ1),
                new Vector3(0.34f, 3.0f, 0.34f), m.post);
            box("Mon_Ishi" + s, root, new Vector3(GateX + s * 2.1f, gy + 0.16f, SZ1),
                new Vector3(0.6f, 0.32f, 0.6f), m.stone);
        }
        box("Mon_Kasagi", root, new Vector3(GateX, gy + 3.1f, SZ1), new Vector3(5.2f, 0.26f, 0.5f), m.post);
        HouseRoof.Shed(root, "Mon_Yane_F", GateX - 2.9f, GateX + 2.9f,
                       SZ1 - 0.05f, SZ1 + 1.1f, gy + 3.6f, gy + 3.15f, 1.5f, m.roofM, m.woodM);
        HouseRoof.Shed(root, "Mon_Yane_B", GateX - 2.9f, GateX + 2.9f,
                       SZ1 + 0.05f, SZ1 - 1.1f, gy + 3.6f, gy + 3.15f, 1.5f, m.roofM, m.woodM);
        // 表札がわりの 石
        box("Mon_Hyosatsu", root, new Vector3(GateX - 2.7f, gy + 0.6f, SZ1 + 0.4f),
            new Vector3(0.4f, 1.2f, 0.35f), m.stone);
    }

    static void Wall(System.Func<string, Transform, Vector3, Vector3, Material, GameObject> box,
                     Transform root, string n, float x, float z, float len, float t, float h, Material m) {
        float y = TerrainGen.Height(x + len * 0.5f, z);
        box(n + Mathf.RoundToInt(x), root, new Vector3(x + len * 0.5f, y + h * 0.5f - 0.1f, z),
            new Vector3(len + 0.06f, h, t), m);
    }
    static void WallZ(System.Func<string, Transform, Vector3, Vector3, Material, GameObject> box,
                      Transform root, string n, float x, float z, float len, float t, float h, Material m) {
        float y = TerrainGen.Height(x, z + len * 0.5f);
        box(n + Mathf.RoundToInt(z), root, new Vector3(x, y + h * 0.5f - 0.1f, z + len * 0.5f),
            new Vector3(t, h, len + 0.06f), m);
    }

    // ---------------------------------------------------------------- 蔵（くら）
    // 白い 漆喰の 壁・小さな 窓・重い 扉。**母屋とは 離して 建てる（火よけ）**
    static void Kura(Transform root, Mats m,
                     System.Func<string, Transform, Vector3, Vector3, Material, GameObject> box) {
        const float W = 5.4f, D = 4.2f, H = 4.6f;
        float cx = -20f, cz = -7.5f;
        float y = TerrainGen.Height(cx, cz);
        // 石の 腰（蔵は 土台が 高い）
        box("Kura_Dai", root, new Vector3(cx, y + 0.28f, cz), new Vector3(W + 0.7f, 0.56f, D + 0.7f), m.stone);
        // 白い 壁 4面
        box("Kura_B", root, new Vector3(cx, y + 0.56f + H * 0.5f, cz - D * 0.5f),
            new Vector3(W, H, 0.32f), m.plasterFit(W, H));
        box("Kura_F", root, new Vector3(cx, y + 0.56f + H * 0.5f, cz + D * 0.5f),
            new Vector3(W, H, 0.32f), m.plasterFit(W, H));
        box("Kura_L", root, new Vector3(cx - W * 0.5f, y + 0.56f + H * 0.5f, cz),
            new Vector3(0.32f, H, D), m.plasterFit(D, H));
        box("Kura_R", root, new Vector3(cx + W * 0.5f, y + 0.56f + H * 0.5f, cz),
            new Vector3(0.32f, H, D), m.plasterFit(D, H));
        // 重い 扉（黒い 木）と まわりの 枠
        box("Kura_Tobira", root, new Vector3(cx, y + 1.7f, cz + D * 0.5f + 0.14f),
            new Vector3(1.5f, 2.2f, 0.14f), m.wood);
        box("Kura_Waku", root, new Vector3(cx, y + 1.75f, cz + D * 0.5f + 0.08f),
            new Vector3(1.9f, 2.5f, 0.1f), m.woodFit(1.9f, 2.5f));
        // 小さな 窓（高い ところに ひとつ）
        box("Kura_Mado", root, new Vector3(cx + 1.6f, y + 3.4f, cz + D * 0.5f + 0.1f),
            new Vector3(0.7f, 0.7f, 0.1f), m.wood);
        // 腰の 下見板（なまこ壁の かわり）
        box("Kura_Koshi", root, new Vector3(cx, y + 1.0f, cz + D * 0.5f + 0.16f),
            new Vector3(W + 0.1f, 0.9f, 0.12f), m.koshiFit(W, 0.9f));
        // 切妻の 屋根
        HouseRoof.Shed(root, "Kura_Yane_F", cx - W * 0.5f - 0.5f, cx + W * 0.5f + 0.5f,
                       cz - 0.02f, cz + D * 0.5f + 0.7f, y + H + 1.5f, y + H + 0.6f, 1.5f, m.roofM, m.woodM);
        HouseRoof.Shed(root, "Kura_Yane_B", cx - W * 0.5f - 0.5f, cx + W * 0.5f + 0.5f,
                       cz + 0.02f, cz - D * 0.5f - 0.7f, y + H + 1.5f, y + H + 0.6f, 1.5f, m.roofM, m.woodM);
    }

    // ---------------------------------------------------------------- 離れ（はなれ）
    // 隠居や 客の ための 小さな 棟。縁側つき
    static void Hanare(Transform root, Mats m,
                       System.Func<string, Transform, Vector3, Vector3, Material, GameObject> box) {
        const float W = 6.6f, D = 4.8f, H = 2.35f;
        float cx = -21f, cz = 5.5f;
        float y = TerrainGen.Height(cx, cz);
        box("Hanare_Floor", root, new Vector3(cx, y + 0.30f, cz), new Vector3(W, 0.2f, D), m.tatami);
        box("Hanare_Under", root, new Vector3(cx, y + 0.12f, cz), new Vector3(W + 0.2f, 0.44f, D + 0.2f), m.wood);
        box("Hanare_B", root, new Vector3(cx, y + 0.4f + H * 0.5f, cz - D * 0.5f),
            new Vector3(W, H, 0.14f), m.plasterFit(W, H));
        box("Hanare_L", root, new Vector3(cx - W * 0.5f, y + 0.4f + H * 0.5f, cz),
            new Vector3(0.14f, H, D), m.plasterFit(D, H));
        box("Hanare_R", root, new Vector3(cx + W * 0.5f, y + 0.4f + H * 0.5f, cz),
            new Vector3(0.14f, H, D), m.plasterFit(D, H));
        // 手前は 障子（縁側に 面する）
        for (int i = 0; i < 4; i++) {
            float w = W / 4f, x = cx - W * 0.5f + w * (i + 0.5f);
            box("Hanare_Shoji" + i, root, new Vector3(x, y + 1.35f, cz + D * 0.5f),
                new Vector3(w * 0.95f, 1.75f, 0.05f), m.plaster);
        }
        // 縁側
        box("Hanare_En", root, new Vector3(cx, y + 0.38f, cz + D * 0.5f + 0.45f),
            new Vector3(W, 0.1f, 0.9f), m.floor);
        // 屋根
        HouseRoof.Shed(root, "Hanare_Yane_F", cx - W * 0.5f - 0.6f, cx + W * 0.5f + 0.6f,
                       cz - 0.02f, cz + D * 0.5f + 1.3f, y + H + 1.35f, y + H + 0.55f, 1.5f, m.roofM, m.woodM);
        HouseRoof.Shed(root, "Hanare_Yane_B", cx - W * 0.5f - 0.6f, cx + W * 0.5f + 0.6f,
                       cz + 0.02f, cz - D * 0.5f - 1.0f, y + H + 1.35f, y + H + 0.55f, 1.5f, m.roofM, m.woodM);
        // 沓ぬぎ石
        box("Hanare_Ishi", root, new Vector3(cx, y + 0.14f, cz + D * 0.5f + 1.15f),
            new Vector3(1.0f, 0.28f, 0.6f), m.stone);
    }

    // ---------------------------------------------------------------- 庭
    // ★**前庭は 花の 庭では ない。**もみを ほし、農具を ひろげる **土の 作業庭**。
    //   裏庭は 台所に つづく 菜園（井戸・物ほし・つけもの樽）
    static void Niwa(Transform root, Mats m,
                     System.Func<string, Transform, Vector3, Vector3, Material, GameObject> box) {
        // むしろ を ひろげて もみを ほす（前庭）
        for (int i = 0; i < 3; i++) {
            float x = 2.0f + i * 2.6f, z = 9.5f;
            box("Niwa_Mushiro" + i, root, On(x, z, 0.04f), new Vector3(2.2f, 0.06f, 3.0f), m.tatami);
        }
        // 農具・木箱・たきぎ
        for (int i = 0; i < 4; i++)
            box("Niwa_Takigi" + i, root, On(-3.0f - i * 0.55f, 9.8f, 0.22f), new Vector3(0.5f, 0.44f, 1.8f), m.wood);
        box("Niwa_Dai", root, On(-8f, 9.6f, 0.4f), new Vector3(1.8f, 0.8f, 0.9f), m.wood);

        // 裏庭：物ほし（竿）と つけもの樽
        for (int s = -1; s <= 1; s += 2)
            box("Niwa_Sao" + s, root, On(-13f + s * 2.2f, -9.5f, 0.9f), new Vector3(0.12f, 1.8f, 0.12f), m.post);
        box("Niwa_Sao_Yoko", root, On(-13f, -9.5f, 1.75f), new Vector3(4.6f, 0.08f, 0.08f), m.post);
        for (int i = 0; i < 3; i++)
            box("Niwa_Taru" + i, root, On(-17f + i * 0.95f, -10.5f, 0.35f), new Vector3(0.8f, 0.7f, 0.8f), m.wood);
        // 風呂の 焚き口（外に ある）
        box("Niwa_Furo", root, On(5.5f, -9.0f, 0.6f), new Vector3(2.2f, 1.2f, 1.8f), m.stone);
        box("Niwa_Entotsu", root, On(6.4f, -9.0f, 1.9f), new Vector3(0.4f, 2.6f, 0.4f), m.stone);
    }

    // ---------------------------------------------------------------- 屋敷林
    // ★北がわ・西がわに 木を 列で 植えて 風を よける。**これが あると 昔からの 家に 見える**
    static void Yashikirin(Transform root, System.Action<string, Vector3, int, float> plant) {
        if (plant == null) return;
        int n = 0;
        for (float x = SX0 + 1f; x < SX1; x += 3.1f) {
            plant("Yashikirin_N" + (n++), On(x, SZ0 - 1.6f, 0f), 0, 6.4f);
            if (n % 2 == 0) plant("Yashikirin_N2" + n, On(x + 1.4f, SZ0 - 3.2f, 0f), 1, 5.6f);
        }
        for (float z = SZ0 + 2f; z < SZ1; z += 3.1f) {
            plant("Yashikirin_W" + (n++), On(SX0 - 1.6f, z, 0f), 1, 6.0f);
            if (n % 2 == 0) plant("Yashikirin_W2" + n, On(SX0 - 3.2f, z + 1.4f, 0f), 0, 5.4f);
        }
    }

    static Vector3 On(float x, float z, float lift) {
        return new Vector3(x, TerrainGen.Height(x, z) + lift, z);
    }
}
