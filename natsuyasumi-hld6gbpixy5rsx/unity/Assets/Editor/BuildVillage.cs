// 谷そこの 作りこみ。納屋・畑・農具小屋・井戸・祠・車道。
//
// ★調べた こと（2026-08-15）
//  - 田舎の 農家は 母屋の ほかに **納屋（なや）** が 建つ。田んぼの 道具・わら・
//    米を しまう 別棟で、大きな 引き戸が つく。
//  - **祠（ほこら）** は 大きな 神社では なく、田んぼの わきや 辻に 立つ 小さな もの。
//    鳥居 1基と 石の 台に のった 小屋が ひとつ、というのが ふつう。
//  - **井戸** は 使われなく なっても 埋めずに 蓋を して 残す ことが 多い。
//  - 田舎の 車道は **1車線ぶんの 幅（3m ほど）** で、対向車が 来たら
//    どちらかが 待避所で 待つ。舗装されて いない ところも 多い。
//
// 建物は 3D の 箱で 組み、絵は 家と 同じ ドット絵の テクスチャを 貼る。
using UnityEngine;

public static class BuildVillage {

    /// <summary>谷そこに ひとそろい 建てる</summary>
    public static void Build(Transform root, Materials m,
                             System.Action<string, Transform, Vector3, Vector3, Material> box,
                             System.Action<string, Vector3, int, float> prop) {
        Naya(root, m, box);
        Hatake(root, m, box, prop);
        Nougu(root, m, box);
        Ido(root, m, box);
        Hokora(root, m, box);
        Kuruma(root, m, box);
    }

    // ---------------------------------------------------------------- 車道ばた
    // 道はば じたいは TerrainGen が 車 1台ぶんに 削って いる。
    // ここでは **そこが 車の 通る 道だと 分かる もの**を 立てる。
    //  - 電柱：田舎の 道ぞいに かならず 立って いて、これが あると 生活が 見える
    //  - カーブミラー：見とおしの わるい 辻に 立つ
    //  - 待避所の 目じるし：すれちがえない 道には 車を よける ふくらみが ある
    static void Kuruma(Transform root, Materials m,
                       System.Action<string, Transform, Vector3, Vector3, Material> box) {
        // 本道は z=7 を よこに 走る。すこし 北がわの 路肩に ならべる
        const float RoadZ = 7f, Shoulder = 2.9f;
        // ★**家の 正面には 立てない。** 等間かくに ならべたら ちょうど 玄関の まえに
        //   1本 立ち、家が 見えなく なった。母屋の 幅（±5.4m）は あける
        // 母屋(±5.4)も 納屋(6.3〜12.7)も よける。撮ったら 柱が 納屋の 屋根を 貫いて いた
        float[] poleX = { -24f, -14f, 14f, 23f };
        for (int i = 0; i < poleX.Length; i++) {
            float x = poleX[i];
            float z = RoadZ - Shoulder;
            float y = TerrainGen.Height(x, z);
            // 電柱。上に 腕木を 2本
            box("Denchu" + i, root, new Vector3(x, y + 3.1f, z), new Vector3(0.16f, 6.2f, 0.16f), m.post);
            box("Denchu_Ude" + i, root, new Vector3(x, y + 5.6f, z), new Vector3(1.3f, 0.09f, 0.09f), m.post);
            box("Denchu_Ude2" + i, root, new Vector3(x, y + 5.15f, z), new Vector3(1.0f, 0.08f, 0.08f), m.post);
            // 電線。となりの 柱まで 1本 わたす（たるみは 出さない＝細い 箱 1つで 足りる）。
            // **家の 上は またがない**ので、間が あきすぎる ところは とばす
            if (i < poleX.Length - 1) {
                float nx = poleX[i + 1];
                if (nx - x <= 12f) {
                    float ny = TerrainGen.Height(nx, z);
                    var mid = new Vector3((x + nx) * 0.5f, (y + ny) * 0.5f + 5.45f, z);
                    box("Densen" + i, root, mid, new Vector3(nx - x, 0.05f, 0.05f), m.post);
                }
            }
        }
        // カーブミラー（家へ 曲がる 辻）。
        // **道の 上には 立てない。**路肩の そとへ 出す（車が ぶつかる ところに 立って いた）
        {
            float x = 3.4f, z = RoadZ + 2.6f;
            float y = TerrainGen.Height(x, z);
            box("Mirror_Hashira", root, new Vector3(x, y + 1.3f, z), new Vector3(0.09f, 2.6f, 0.09f), m.post);
            box("Mirror_Kagami",  root, new Vector3(x, y + 2.5f, z), new Vector3(0.55f, 0.55f, 0.06f),
                m.seeThrough != null ? m.seeThrough : m.plaster);
        }
        // 待避所。**すれちがえない 道には かならず ある。**
        // 石を ならべて 路肩が ふくらんで いる ことを 見せる
        for (int i = -2; i <= 2; i++) {
            float x = -8f + i * 0.9f, z = RoadZ + 2.2f;
            box("Taihi" + i, root, On(x, z, 0.12f), new Vector3(0.5f, 0.26f, 0.42f), m.stone);
        }
    }

    /// <summary>組み立てに つかう 素材ひとまとめ</summary>
    public struct Materials {
        public Material wood, floor, plaster, roof, stone, paper;
        public Material soil;   // 畑の うね
        public Material post;   // 柱・鳥居（こい 木）。**主人公の まわりで 穴が あく**
        public Material seeThrough;   // カーブミラーの 板。これも 穴が あく
    }

    static Vector3 On(float x, float z, float lift = 0f) {
        return new Vector3(x, TerrainGen.Height(x, z) + lift, z);
    }

    // ---------------------------------------------------------------- 納屋
    // 母屋の となり。板ばりの 大きな 引き戸、なかは 暗い
    static void Naya(Transform root, Materials m,
                     System.Action<string, Transform, Vector3, Vector3, Material> box) {
        const float W = 6.4f, D = 4.6f, H = 3.2f;
        float cx = 9.5f, cz = -1.5f;
        float y = TerrainGen.Height(cx, cz);

        box("Naya_Floor", root, new Vector3(cx, y + 0.08f, cz), new Vector3(W, 0.16f, D), m.floor);
        // 板かべ 3面（手前は あけて 中が 見える＝家と 同じ 見せかた）
        box("Naya_Back",  root, new Vector3(cx, y + H * 0.5f, cz - D * 0.5f), new Vector3(W, H, 0.16f), m.wood);
        box("Naya_Left",  root, new Vector3(cx - W * 0.5f, y + H * 0.5f, cz), new Vector3(0.16f, H, D), m.wood);
        box("Naya_Right", root, new Vector3(cx + W * 0.5f, y + H * 0.5f, cz), new Vector3(0.16f, H, D), m.wood);
        // 引き戸（半分 あけて ある）
        box("Naya_Door",  root, new Vector3(cx - W * 0.25f, y + H * 0.45f, cz + D * 0.5f),
            new Vector3(W * 0.5f, H * 0.9f, 0.10f), m.wood);
        // 屋根。切妻に 見える ように 2枚 かける
        for (int i = -1; i <= 1; i += 2) {
            var r = new GameObject("Naya_Roof" + i);
            r.transform.SetParent(root, false);
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "R"; cube.transform.SetParent(r.transform, false);
            cube.transform.localPosition = new Vector3(cx, y + H + 0.55f, cz + i * D * 0.26f);
            cube.transform.localScale = new Vector3(W + 0.8f, 0.24f, D * 0.62f);
            cube.transform.localRotation = Quaternion.Euler(i * 22f, 0f, 0f);
            cube.GetComponent<Renderer>().sharedMaterial = m.roof;
        }
        // わらの 束（納屋の わき）
        for (int i = 0; i < 5; i++)
            box("Naya_Wara" + i, root, On(cx + W * 0.5f + 0.8f, cz - 1.4f + i * 0.62f, 0.34f),
                new Vector3(0.9f, 0.68f, 0.5f), m.floor);
    }

    // ---------------------------------------------------------------- 畑
    // うねを 何本か。作物は 板の ドット絵で 立てる
    static void Hatake(Transform root, Materials m,
                       System.Action<string, Transform, Vector3, Vector3, Material> box,
                       System.Action<string, Vector3, int, float> prop) {
        float ox = -6f, oz = 16.5f;           // 畑の 左手前
        const int Rows = 6;
        const float RowLen = 9.5f, RowGap = 1.15f;
        for (int r = 0; r < Rows; r++) {
            float z = oz + r * RowGap;
            // うね（土を もった すじ）
            for (int s = 0; s < 10; s++) {
                float x = ox + s * (RowLen / 9f);
                box("Une" + r + "_" + s, root, On(x, z, 0.09f),
                    new Vector3(RowLen / 9f + 0.05f, 0.18f, 0.74f), m.soil);
            }
            // 作物。うねの 上に ならべる
            for (int s = 0; s < 9; s++) {
                float x = ox + 0.4f + s * (RowLen / 9f);
                prop("Sakumotsu" + r + "_" + s, On(x, z, 0.2f), r % 3, 0.85f);
            }
        }
        // 畑の ふちの 杭と なわ
        for (int i = 0; i <= 8; i++) {
            float x = ox - 0.6f + i * (RowLen + 1.2f) / 8f;
            box("Kui" + i, root, On(x, oz - 1.1f, 0.42f), new Vector3(0.09f, 0.84f, 0.09f), m.post);
        }
    }

    // ---------------------------------------------------------------- 農具小屋
    // 中に 鍬・肥料の 袋・米の 保冷庫。**さわって 何かする ものでは ない**（子どもには むずかしい）
    static void Nougu(Transform root, Materials m,
                      System.Action<string, Transform, Vector3, Vector3, Material> box) {
        const float W = 3.6f, D = 2.8f, H = 2.4f;
        float cx = 5.5f, cz = 18.5f;
        float y = TerrainGen.Height(cx, cz);

        box("Nougu_Floor", root, new Vector3(cx, y + 0.07f, cz), new Vector3(W, 0.14f, D), m.floor);
        box("Nougu_Back",  root, new Vector3(cx, y + H * 0.5f, cz - D * 0.5f), new Vector3(W, H, 0.12f), m.wood);
        box("Nougu_Left",  root, new Vector3(cx - W * 0.5f, y + H * 0.5f, cz), new Vector3(0.12f, H, D), m.wood);
        box("Nougu_Roof",  root, new Vector3(cx, y + H + 0.12f, cz), new Vector3(W + 0.6f, 0.2f, D + 0.6f), m.roof);

        // 鍬（柄＋刃）
        box("Kuwa_E",  root, new Vector3(cx - 1.2f, y + 0.75f, cz - 1.0f), new Vector3(0.06f, 1.5f, 0.06f), m.wood);
        box("Kuwa_Ha", root, new Vector3(cx - 1.2f, y + 0.10f, cz - 0.86f), new Vector3(0.30f, 0.20f, 0.06f), m.stone);
        // 肥料の 袋
        for (int i = 0; i < 3; i++)
            box("Hiryo" + i, root, new Vector3(cx + 0.2f + i * 0.5f, y + 0.28f, cz - 0.9f),
                new Vector3(0.44f, 0.56f, 0.34f), m.paper);
        // 米の 保冷庫
        box("Reizo", root, new Vector3(cx + 1.2f, y + 0.7f, cz + 0.6f), new Vector3(0.9f, 1.4f, 0.7f), m.stone);
        box("Reizo_Tobira", root, new Vector3(cx + 1.2f, y + 0.7f, cz + 0.96f), new Vector3(0.78f, 1.2f, 0.05f), m.plaster);
    }

    // ---------------------------------------------------------------- 使われなく なった 井戸
    static void Ido(Transform root, Materials m,
                    System.Action<string, Transform, Vector3, Vector3, Material> box) {
        float cx = -2.5f, cz = 11.5f;
        float y = TerrainGen.Height(cx, cz);
        // 石の わく（8角に ならべる）
        for (int i = 0; i < 8; i++) {
            float a = i * Mathf.PI * 2f / 8f;
            var p = new Vector3(cx + Mathf.Cos(a) * 0.72f, y + 0.28f, cz + Mathf.Sin(a) * 0.72f);
            box("Ido_Ishi" + i, root, p, new Vector3(0.42f, 0.56f, 0.42f), m.stone);
        }
        // ふた（板）。**使われなく なっても 埋めずに 蓋を して 残す**
        box("Ido_Futa", root, new Vector3(cx, y + 0.58f, cz), new Vector3(1.5f, 0.09f, 1.5f), m.floor);
        // 屋根と 柱、滑車
        for (int i = -1; i <= 1; i += 2)
            box("Ido_Hashira" + i, root, new Vector3(cx + i * 0.8f, y + 1.1f, cz), new Vector3(0.11f, 2.1f, 0.11f), m.post);
        box("Ido_Yane", root, new Vector3(cx, y + 2.2f, cz), new Vector3(2.1f, 0.14f, 1.6f), m.roof);
        box("Ido_Kassha", root, new Vector3(cx, y + 2.0f, cz), new Vector3(1.5f, 0.08f, 0.08f), m.wood);
    }

    // ---------------------------------------------------------------- 祠（小さな 神さま）
    // 大きな 神社では ない。鳥居 1基と 石の 台に のった 小屋
    static void Hokora(Transform root, Materials m,
                       System.Action<string, Transform, Vector3, Vector3, Material> box) {
        float cx = 15.5f, cz = -9.5f;
        float y = TerrainGen.Height(cx, cz);

        // 鳥居（手前に 1基）
        float tz = cz + 3.4f;
        float ty = TerrainGen.Height(cx, tz);
        for (int i = -1; i <= 1; i += 2)
            box("Torii_Hashira" + i, root, new Vector3(cx + i * 1.05f, ty + 1.15f, tz),
                new Vector3(0.17f, 2.3f, 0.17f), m.post);
        box("Torii_Kasagi", root, new Vector3(cx, ty + 2.34f, tz), new Vector3(2.9f, 0.17f, 0.26f), m.post);
        box("Torii_Nuki",   root, new Vector3(cx, ty + 1.90f, tz), new Vector3(2.4f, 0.12f, 0.16f), m.post);

        // 石の 台
        box("Hokora_Dai", root, new Vector3(cx, y + 0.22f, cz), new Vector3(1.5f, 0.44f, 1.3f), m.stone);
        // 小屋（social な 大きさ＝人の こしぐらい）
        box("Hokora_Body", root, new Vector3(cx, y + 0.78f, cz), new Vector3(0.95f, 0.7f, 0.8f), m.wood);
        for (int i = -1; i <= 1; i += 2) {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Hokora_Yane" + i; cube.transform.SetParent(root, false);
            cube.transform.localPosition = new Vector3(cx, y + 1.28f, cz + i * 0.24f);
            cube.transform.localScale = new Vector3(1.35f, 0.12f, 0.62f);
            cube.transform.localRotation = Quaternion.Euler(i * 30f, 0f, 0f);
            cube.GetComponent<Renderer>().sharedMaterial = m.roof;
        }
        // お供えの 石と 灯ろう
        box("Hokora_Sonae", root, new Vector3(cx - 0.9f, y + 0.14f, cz + 0.8f), new Vector3(0.34f, 0.28f, 0.34f), m.stone);
        box("Hokora_Toro",  root, new Vector3(cx + 1.3f, y + 0.5f, cz + 1.0f), new Vector3(0.3f, 1.0f, 0.3f), m.stone);
        box("Hokora_Hi",    root, new Vector3(cx + 1.3f, y + 1.06f, cz + 1.0f), new Vector3(0.36f, 0.3f, 0.36f), m.paper);

        // 石だん（祠へ 上がる）
        for (int i = 0; i < 4; i++) {
            float sz = tz - 0.7f - i * 0.55f;
            box("Hokora_Dan" + i, root, new Vector3(cx, TerrainGen.Height(cx, sz) + 0.1f + i * 0.06f, sz),
                new Vector3(2.0f, 0.2f, 0.55f), m.stone);
        }
    }
}
