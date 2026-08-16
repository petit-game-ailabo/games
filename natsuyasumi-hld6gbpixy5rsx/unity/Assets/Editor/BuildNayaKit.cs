// 納屋の あとに、**借りものの アセットで 家を 建てる**（2026-08-16）。
//
// ★これは 質の 試し。本人：「アセットを つかったら どれだけ 質の 高い ものが できるのか
//   テストしたい。風景に 合わなくて いい」。中世ヨーロッパの キットを そのまま つかう。
//   ここで つかんだ 作りを 手本に、じぶんで 起こした モデルを 入れて いく。
//
// ★このキットの くせ（MegaKit.Dump で 測った 実さいの 数字）
//   - **部品は ねている。** 高さが -Z、厚みが Y。置くときに **X まわりに +90度** まわす。
//     まわすと (x,y,z) が (x,-z,y) に なり、高さ 3.12m・足もと y=0 の 部品に なる
//   - **外がわは -Z。** 壁の かざり も、よろい戸 も、せり出し も -Z へ 出る。
//     だから「手前(+Z)を 見せる 面」に する には yaw=180 でまわす
//   - **わりつけは 2m。** 壁 1まい＝2.00m x 3.12m x 0.41m。床タイルも 2m x 2m
//   - 隅の 柱は (-X,-Z) の かどむけ。かどごとに yaw を 変える
using UnityEditor;
using UnityEngine;

public static class BuildNayaKit {

    // わりつけ
    const float G = 2.0f;        // ますめ
    const float SH = 3.12f;      // 1階ぶんの 高さ
    const int NX = 3, NZ = 4;    // 横3ます(6m) x 奥ゆき4ます(8m)
    static float HX { get { return NX * G * 0.5f; } }   // 3.0
    static float HZ { get { return NZ * G * 0.5f; } }   // 4.0

    // 建てる ところ。**母屋の 軒(x=6.55)に かからない ように 右へ よせる。**
    // 前の 納屋は cx=9.5 だったが、キットの 屋根は 8.25m 幅で 軒が 1.1m 出るので
    // そのままだと 母屋の 屋根と 重なる
    const float CX = 40.0f, CZ = -4.0f;

    public static void Build(Transform parent) {
        // ---------- 地めん。**絵では 高さが 読めない。数字を 出してから 決める**
        float y = -999f, lo = 999f;
        for (int i = 0; i <= 4; i++)
            for (int j = 0; j <= 4; j++) {
                float h = TerrainGen.Height(CX - HX + i * (HX * 2f / 4f), CZ - HZ + j * (HZ * 2f / 4f));
                if (h > y) y = h;
                if (h < lo) lo = h;
            }
        Debug.Log(string.Format("[NayaKit] 地めん さいこう={0:0.00} さいてい={1:0.00} 差={2:0.00}m", y, lo, y - lo));

        var root = new GameObject("MegaKitHouse").transform;
        root.SetParent(parent, false);
        root.localPosition = new Vector3(CX, y, CZ);

        // ---------- 土台。かたむいた 地めんとの すきまを 石で うめる
        // 差が 大きいと 建物が 宙に 浮く（前の 納屋は 箱だったので ごまかせて いた）
        float baseH = Mathf.Max(0.35f, y - lo + 0.25f);
        var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pad.name = "Kit_Base";
        pad.transform.SetParent(root, false);
        // ★土台の 天は **床タイルの 天(0.03)に ぴったり そろえる**。
        //   前は 0.06 で、床タイル(0.01〜0.03)より 3cm 高く、部屋の 中に
        //   灰色の 段が 走って 見えた（本人「部屋の中も高さがあってない」）。
        //   FBX には あたりが 無い ので、**この 土台が そのまま 床の あたり**に なる
        pad.transform.localPosition = new Vector3(0f, 0.03f - baseH * 0.5f, 0f);
        pad.transform.localScale = new Vector3(NX * G + 0.9f, baseH, NZ * G + 0.9f);
        pad.GetComponent<Renderer>().sharedMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(MegaKit.MatDir + "MI_RockTrim.mat");

        // ---------- 床
        for (int i = 0; i < NX; i++)
            for (int j = 0; j < NZ; j++)
                Put(root, "Floor_UnevenBrick", new Vector3(-HX + G * (i + 0.5f), 0.02f, -HZ + G * (j + 0.5f)));

        // ---------- 1階＝石づみ
        // 手前(+Z)。まん中が 戸口、右が 窓
        Wall(root, "Wall_UnevenBrick_Straight",          -G, 0f, HZ, 180f);
        Wall(root, "Wall_UnevenBrick_Door_Round",         0f, 0f, HZ, 180f);
        Wall(root, "Wall_UnevenBrick_Window_Wide_Round",  G, 0f, HZ, 180f);
        Put(root, "DoorFrame_Round_WoodDark", new Vector3(0f, 0f, HZ), 180f);
        // 戸は **ちょうつがいが x=0、身は +X がわ**（x[-0.04..1.07]）。
        // yaw180 で 裏返る ので、左へ 0.55 ずらして 置くと わくの まん中に おさまる
        Put(root, "Door_2_Round",             new Vector3(-0.55f, 0f, HZ - 0.06f), 180f);
        Put(root, "Window_Wide_Round1",       new Vector3(G, 0f, HZ), 180f);
        Put(root, "WindowShutters_Wide_Round_Open", new Vector3(G, 0f, HZ), 180f);
        // おく(-Z)
        for (int i = 0; i < NX; i++)
            Wall(root, "Wall_UnevenBrick_Straight", -HX + G * (i + 0.5f), 0f, -HZ, 0f);
        // 左右
        for (int j = 0; j < NZ; j++) {
            float z = -HZ + G * (j + 0.5f);
            Wall2(root, j == 1 ? "Wall_UnevenBrick_Window_Wide_Round" : "Wall_UnevenBrick_Straight", -HX, 0f, z, 90f);
            Wall2(root, j == 2 ? "Wall_UnevenBrick_Window_Wide_Round" : "Wall_UnevenBrick_Straight",  HX, 0f, z, 270f);
        }
        Put(root, "Window_Wide_Round1", new Vector3(-HX, 0f, -HZ + G * 1.5f), 90f);
        Put(root, "Window_Wide_Round1", new Vector3( HX, 0f, -HZ + G * 2.5f), 270f);

        // ---------- 2階＝ハーフティンバー（木の 格子を 見せた 漆喰）
        float y2 = SH;
        Wall(root, "Wall_Plaster_WoodGrid",            -G, y2, HZ, 180f);
        Wall(root, "Wall_Plaster_Window_Wide_Round",    0f, y2, HZ, 180f);
        Wall(root, "Wall_Plaster_WoodGrid",              G, y2, HZ, 180f);
        Put(root, "Window_Wide_Round1",             new Vector3(0f, y2, HZ - 0.02f), 180f);
        Put(root, "WindowShutters_Wide_Round_Open", new Vector3(0f, y2, HZ), 180f);
        for (int i = 0; i < NX; i++)
            Wall(root, "Wall_Plaster_WoodGrid", -HX + G * (i + 0.5f), y2, -HZ, 0f);
        for (int j = 0; j < NZ; j++) {
            float z = -HZ + G * (j + 0.5f);
            Wall2(root, j == 1 ? "Wall_Plaster_Window_Wide_Round" : "Wall_Plaster_WoodGrid", -HX, y2, z, 90f);
            Wall2(root, j == 2 ? "Wall_Plaster_Window_Wide_Round" : "Wall_Plaster_WoodGrid",  HX, y2, z, 270f);
        }
        Put(root, "Window_Wide_Round1", new Vector3(-HX, y2, -HZ + G * 1.5f), 90f);
        Put(root, "Window_Wide_Round1", new Vector3( HX, y2, -HZ + G * 2.5f), 270f);

        // ---------- 隅の 柱。**部品は (-X,-Z) の かどむけ**なので かどごとに 向きを 変える
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2) {
                float yaw = (sx < 0 && sz > 0) ? 90f : (sx > 0 && sz > 0) ? 180f
                          : (sx > 0 && sz < 0) ? 270f : 0f;
                Put(root, "Corner_Exterior_Brick", new Vector3(sx * HX, 0f, sz * HZ), yaw);
                Put(root, "Corner_Exterior_Wood",  new Vector3(sx * HX, y2, sz * HZ), yaw);
            }

        // ---------- 屋根。棟は Z ぞい。6x8 の 屋根が 6m x 8m の 建物に 合う
        float yr = y2 + SH;
        Put(root, "Roof_RoundTiles_6x8", new Vector3(0f, yr, 0f));
        Put(root, "Roof_Front_Brick6",   new Vector3(0f, yr, HZ), 180f);
        Put(root, "Roof_Front_Brick6",   new Vector3(0f, yr, -HZ), 0f);
        // えんとつは **屋根を つき抜けさせる。** 屋根は x=0 が 棟で、x=-2.45 の あたりの
        // 高さは 棟の 半分ほど。足もとを yr より 下に 入れないと 屋根に 埋まる
        Put(root, "Prop_Chimney",        new Vector3(-2.45f, yr - 0.4f, -2.4f));

        // ---------- まわりの もの。**建物だけだと 模型に 見える**
        Put(root, "Prop_Wagon", new Vector3(HX + 2.1f, 0.05f, 1.6f), 24f);
        Put(root, "Prop_Crate", new Vector3(-HX - 1.0f, 0.05f, HZ - 0.7f), 12f);
        Put(root, "Prop_Crate", new Vector3(-HX - 1.5f, 0.05f, HZ - 1.6f), -20f);
        Put(root, "Prop_Crate", new Vector3(-HX - 1.05f, 1.10f, HZ - 0.9f), 40f);
        Put(root, "Prop_Vine1", new Vector3(-HX - 0.02f, y2 + 2.2f, -HZ + G * 0.7f), 90f);
        Put(root, "Prop_Vine2", new Vector3(HX + 0.02f, y2 + 2.4f, -HZ + G * 3.2f), 270f);
        for (int j = 0; j < 3; j++)
            Put(root, "Prop_WoodenFence_Single", new Vector3(HX + 1.2f, 0.05f, HZ - 0.4f - j * 2.05f), 90f);
        // ★外の 階段は 置かない（2026-08-16・本人）。
        //   **床は 地めんと ほぼ 同じ 高さ**（土台の 天が 地めん +0.06m）なので、
        //   2m の 階段を つけると 戸口より 高い ところへ 登って しまい 話が 合わない

        // ---------- 中の もの。がらんどうだと 入っても 何も 起きない 部屋に なる
        Put(root, "Prop_Crate", new Vector3(-HX + 0.9f, 0.05f, -HZ + 1.0f), 8f);
        Put(root, "Prop_Crate", new Vector3(-HX + 0.9f, 1.10f, -HZ + 1.1f), -14f);
        Put(root, "Prop_Crate", new Vector3(HX - 0.9f, 0.05f, -HZ + 0.9f), -22f);
        Put(root, "Prop_Brick1", new Vector3(HX - 1.4f, 0.05f, -HZ + 2.2f), 30f);

        // ---------- 中の あかり。しめきった 石の 箱は 昼でも まっ暗に なる
        var lampGO = new GameObject("Kit_Light");
        lampGO.transform.SetParent(root, false);
        lampGO.transform.localPosition = new Vector3(0f, 2.0f, -0.5f);
        var lamp = lampGO.AddComponent<Light>();
        lamp.type = LightType.Point;
        lamp.color = new Color(1f, 0.84f, 0.58f);
        lamp.intensity = 4.0f; lamp.range = 9f; lamp.shadows = LightShadows.None;

        // ---------- あたり
        // ★**1つの 箱で かこうと 中に 入れない。**（2026-08-16・本人「部屋の中に入れない」）
        //   FBX には あたりが 付いて いない ので 手で 置くしか ないが、
        //   まるごと 1個の 箱に すると **戸口ごと ふさがる**。壁ごとに 置いて、
        //   手前の まん中＝戸口の ぶんだけ あける
        const float T = 0.40f, HH = SH * 2f;      // 壁の 厚み／高さ
        float cy = HH * 0.5f;
        float door = 0.62f;                        // 戸口の 半分の はば（わくの 内のり 1.2m ほど）
        // おく
        Hit(root, new Vector3(0f, cy, -HZ), new Vector3(NX * G + T, HH, T));
        // 左右
        Hit(root, new Vector3(-HX, cy, 0f), new Vector3(T, HH, NZ * G + T));
        Hit(root, new Vector3( HX, cy, 0f), new Vector3(T, HH, NZ * G + T));
        // 手前は 戸口の 左右だけ
        float sideW = HX - door;
        Hit(root, new Vector3(-(door + sideW * 0.5f), cy, HZ), new Vector3(sideW, HH, T));
        Hit(root, new Vector3( (door + sideW * 0.5f), cy, HZ), new Vector3(sideW, HH, T));
        // 2階の 床＝1階の 天井。**無いと 6m の 吹きぬけに なって 部屋に 見えない**
        for (int i = 0; i < NX; i++)
            for (int j = 0; j < NZ; j++)
                Put(root, "Floor_WoodDark", new Vector3(-HX + G * (i + 0.5f), SH, -HZ + G * (j + 0.5f)));

        Debug.Log("[NayaKit] 借りものの 家を 建てた: " + root.GetComponentsInChildren<MeshRenderer>().Length + " まとまり");
    }

    /// <summary>手前・おくの 壁（X ぞいに ならぶ）</summary>
    static void Wall(Transform root, string piece, float x, float y, float z, float yaw) {
        Put(root, piece, new Vector3(x, y, z), yaw);
    }

    /// <summary>左右の 壁（Z ぞいに ならぶ）</summary>
    static void Wall2(Transform root, string piece, float x, float y, float z, float yaw) {
        Put(root, piece, new Vector3(x, y, z), yaw);
    }

    /// <summary>部品を 1つ 置く。**X まわりに +90度 まわして 立たせる**のが みそ</summary>
    static GameObject Put(Transform root, string piece, Vector3 pos, float yaw = 0f) {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(MegaKit.ModelDir + piece + ".fbx");
        if (src == null) { Debug.LogWarning("[NayaKit] 部品が ない: " + piece); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src, root);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        go.name = piece;
        go.transform.localPosition = pos;
        // yaw を さきに かけてから 立たせる。順を まちがえると 向きが ねじれる
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(90f, 0f, 0f);
        // ★**大きさを 1 に もどす。** この キットの prefab は 根っこに 100倍が 入って いて、
        //   メッシュ そのものは すでに 2m。そのまま 置くと **ぴったり 100倍**に なり、
        //   壁 1まいが 200m x 312m の 板に なって 画面ぜんぶを 覆った（2026-08-16）。
        //   MegaKit.Dump は scale を 1 に 直してから 測って いた ので 気づけなかった——
        //   **測るときと 置くときで 条件を そろえないと、正しい 数字が 嘘に なる**
        go.transform.localScale = Vector3.one;
        return go;
    }

    static void Hit(Transform root, Vector3 pos, Vector3 size) {
        var go = new GameObject("Kit_Hit");
        go.transform.SetParent(root, false);
        go.transform.localPosition = pos;
        go.layer = 2;                       // 真下への レイの じゃまを しない
        var c = go.AddComponent<BoxCollider>();
        c.size = size;
    }
}
