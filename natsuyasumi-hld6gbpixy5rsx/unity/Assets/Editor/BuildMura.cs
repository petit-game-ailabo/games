using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

// R2 箱の村。MURA.md の 設計図を **グレーボックス**で 組む（部品の 差し替えは R5）。
// 大きさ・道・遮蔽・見せ場カメラを 実際に 歩いて たしかめる ための もの。
//   rebuild.ps1 -Only BuildMura.Build
public static class BuildMura {

    static Material mGround, mRoad, mWater, mWood, mRed, mGrey, mGreen, mDark, mPaddy;
    static Material mPlaster, mRoofT, mTatamiT;

    // R5テスト第二段：本編の ドット絵化テクスチャ（CC0）を 貼る
    static Material MatT(string name, string tex, float tx, float ty) {
        var m = Mat(name, Color.white);
        m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/" + tex);
        m.mainTextureScale = new Vector2(tx, ty);
        return m;
    }

    static Material MatH(string name, Color c, float emi) {
        var m = Mat(name, c);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", c * emi);
        return m;
    }

    static Material Mat(string name, Color c) {
        string dir = "Assets/Art/Materials/Mura";
        System.IO.Directory.CreateDirectory(dir);
        string path = dir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = c;
        return m;
    }

    static GameObject Box(Transform t, string name, Vector3 c, Vector3 s, Material m) {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name; g.transform.SetParent(t, false);
        g.transform.position = c; g.transform.localScale = s;
        if (m != null) g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

    static GameObject Ramp(Transform t, string name, Vector3 foot, float yaw, float deg, float climb, float width, Material m) {
        // foot＝のぼり口の 地面、yaw の 向きへ deg 度で climb ぶん のぼる 坂
        float len = climb / Mathf.Sin(deg * Mathf.Deg2Rad);
        var g = Box(t, name, Vector3.zero, new Vector3(width, 0.3f, len), m);
        g.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(-deg, 0f, 0f);
        var fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        g.transform.position = foot + fwd * (len * Mathf.Cos(deg * Mathf.Deg2Rad) * 0.5f)
                             + Vector3.up * (climb * 0.5f - 0.14f);
        return g;
    }

    static void Tree(Transform t, float x, float z, float h, float y) {
        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Ki"; trunk.transform.SetParent(t, false);
        trunk.transform.position = new Vector3(x, y + h * 0.5f, z);
        trunk.transform.localScale = new Vector3(0.35f, h * 0.5f, 0.35f);
        trunk.GetComponent<Renderer>().sharedMaterial = mWood;
        var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown.name = "Ha"; crown.transform.SetParent(t, false);
        crown.transform.position = new Vector3(x, y + h + 1.1f, z);
        crown.transform.localScale = new Vector3(3.2f, 2.6f, 3.2f);
        crown.GetComponent<Renderer>().sharedMaterial = mGreen;
        Object.DestroyImmediate(crown.GetComponent<Collider>());   // 葉は 通れる
    }

    public static void Build() {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
            UnityEditor.SceneManagement.NewSceneMode.Single);
        var root = new GameObject("Mura").transform;

        mGround = MatT("MuraGroundT", "grass_ground.png", 90f, 60f);
        mRoad   = MatT("MuraRoadT",   "dirt_path.png", 2f, 10f);
        mWater  = Mat("MuraWater",  new Color(0.36f, 0.56f, 0.66f));
        mWood   = MatT("MuraWoodT",  "wood_beam.png", 2f, 2f);
        mRed    = Mat("MuraRed",    new Color(0.78f, 0.25f, 0.20f));
        mGrey   = MatT("MuraStoneT", "stone.png", 2f, 2f);
        mPlaster = MatT("MuraPlasterT", "plaster_wall.png", 4f, 2f);
        mRoofT   = MatT("MuraRoofT",  "roof_tile.png", 10f, 5f);
        mTatamiT = MatT("MuraTatamiT", "tatami.png", 6f, 3f);
        mGreen  = Mat("MuraGreen",  new Color(0.36f, 0.52f, 0.30f));
        mDark   = Mat("MuraDark",   new Color(0.30f, 0.30f, 0.30f));
        mPaddy  = Mat("MuraPaddy",  new Color(0.42f, 0.58f, 0.34f));

        // ---- 地めん 180x120（x -90..90 / z -60..60。南=-z が 村の 入り口）
        // 川（z=5..11）で 南北に 割って 3まい
        Box(root, "G_Minami", new Vector3(0f, -0.25f, -27.5f), new Vector3(184f, 0.5f, 65f), mGround);
        Box(root, "G_Kita",   new Vector3(0f, -0.25f, 35.5f),  new Vector3(184f, 0.5f, 49f), mGround);
        Box(root, "G_Kawa",   new Vector3(0f, -0.60f, 8f),     new Vector3(184f, 0.5f, 6f),  mDark);   // 川床
        Box(root, "Mizu",     new Vector3(0f, -0.42f, 8f),     new Vector3(184f, 0.1f, 5.6f), mWater)
            .GetComponent<Collider>().isTrigger = true;                                    // 水は 触れる だけ
        // 川岸の 段は 0.35（どこからでも 上がれる。D-100 の 曖昧帯を さける）
        // 地めんの 高低差が 0.35 なので そのまま

        // ---- 祠の 丘（+4m・北西）と 石段
        Box(root, "Oka_Hokora", new Vector3(-48f, 2f, 38f), new Vector3(44f, 4f, 34f), mGround);
        // 石段（蹴上0.25×踏0.28・D-100）。丘の 南から のぼる
        for (int j = 0; j < 16; j++)
            Box(root, "Ishidan" + j, new Vector3(-45f, (j + 1) * 0.25f * 0.5f, 21.2f + j * 0.28f),
                new Vector3(2.6f, (j + 1) * 0.25f, 0.28f), mGrey);
        // 鳥居（村から ちらちら 見える しるし）
        Box(root, "Torii_L", new Vector3(-46.6f, 5.9f, 26.5f), new Vector3(0.5f, 3.8f, 0.5f), mRed);
        Box(root, "Torii_R", new Vector3(-43.4f, 5.9f, 26.5f), new Vector3(0.5f, 3.8f, 0.5f), mRed);
        Box(root, "Torii_T", new Vector3(-45f, 7.9f, 26.5f), new Vector3(5.2f, 0.6f, 0.6f), mRed);
        // 祠と 杉
        Box(root, "Hokora", new Vector3(-48f, 5.1f, 36f), new Vector3(3.6f, 2.2f, 2.7f), mWood);
        foreach (var p in new[] { new Vector2(-58,32), new Vector2(-56,42), new Vector2(-40,44),
                                  new Vector2(-36,34), new Vector2(-52,28), new Vector2(-60,38) })
            Tree(root, p.x, p.y, 6f, 4f);

        // ---- 夏祭り（R5テスト・本編Zashikiのレシピを移植。本番は9-10日だけ→R4で出し入れ）
        // 提灯＝なわに つるす。上下に 赤い わっか、まん中は **光る 紙**（発光マテリアル）。
        // 白い 箱を ならべても 祭りには 見えない、が 本編の 教訓
        {
            var mAka   = Mat("MatsuriAka2",   new Color(0.78f, 0.16f, 0.14f));
            var mShiro = Mat("MatsuriShiro2", new Color(0.94f, 0.92f, 0.86f));
            var mHi    = MatH("MatsuriHi2",   new Color(1.00f, 0.82f, 0.46f), 2.2f);
            var mNawa  = Mat("MatsuriNawa2",  new Color(0.30f, 0.24f, 0.16f));
            const float Y = 4f;                       // 祠の 丘の 上
            // 参道の 両がわに 竹→なわ→提灯
            for (int side = -1; side <= 1; side += 2) {
                float x = -45f + side * 3.2f;
                for (int i = 0; i < 5; i++) {
                    float z = 28f + i * 1.9f;
                    Box(root, "M_Take" + side + "_" + i, new Vector3(x, Y + 1.55f, z),
                        new Vector3(0.09f, 3.1f, 0.09f), mWood);
                    if (i == 4) continue;
                    Box(root, "M_Nawa" + side + "_" + i, new Vector3(x, Y + 3.02f, z + 0.95f),
                        new Vector3(0.035f, 0.035f, 1.9f), mNawa);
                    for (int k = 0; k < 2; k++) {
                        float cz = z + 0.55f + k * 0.85f, cy = Y + 2.62f;
                        Box(root, "M_ChoUe" + side + "_" + i + "_" + k, new Vector3(x, cy + 0.20f, cz),
                            new Vector3(0.20f, 0.05f, 0.20f), mAka);
                        Box(root, "M_Cho" + side + "_" + i + "_" + k, new Vector3(x, cy, cz),
                            new Vector3(0.30f, 0.36f, 0.30f), mHi);
                        Box(root, "M_ChoSita" + side + "_" + i + "_" + k, new Vector3(x, cy - 0.20f, cz),
                            new Vector3(0.20f, 0.05f, 0.20f), mAka);
                    }
                }
                // 地めんを 照らす 灯は 片がわ 1つずつ（点光源を 増やすと 夜が 白飛びする）
                var gl = new GameObject("M_Akari" + side);
                gl.transform.SetParent(root, false);
                gl.transform.position = new Vector3(x, Y + 2.6f, 32f);
                var gt = gl.AddComponent<Light>();
                gt.type = LightType.Point; gt.color = new Color(1f, 0.78f, 0.46f);
                gt.intensity = 3.2f; gt.range = 12f; gt.shadows = LightShadows.None;
            }
            // 屋台 2つ（紅白しまの 屋根＋看板＋弱い 灯）
            for (int i = 0; i < 2; i++) {
                float x = i == 0 ? -52f : -38f, z = 32.5f;
                Box(root, "M_YataiDai" + i, new Vector3(x, Y + 0.5f, z), new Vector3(3.0f, 1.0f, 1.4f), mWood);
                for (int k = -1; k <= 1; k += 2)
                    Box(root, "M_YataiHashira" + i + "_" + k, new Vector3(x + k * 1.5f, Y + 1.35f, z),
                        new Vector3(0.10f, 2.7f, 0.10f), mWood);
                for (int k = 0; k < 6; k++)
                    Box(root, "M_YataiYane" + i + "_" + k,
                        new Vector3(x - 1.65f + 0.275f + k * 0.55f, Y + 2.65f, z),
                        new Vector3(0.55f, 0.12f, 2.0f), (k % 2 == 0) ? mAka : mShiro);
                Box(root, "M_YataiKanban" + i, new Vector3(x, Y + 2.20f, z - 0.98f),
                    new Vector3(2.2f, 0.44f, 0.06f), i == 0 ? mShiro : mAka);
                var yl = new GameObject("M_Hi" + i);
                yl.transform.SetParent(root, false);
                yl.transform.position = new Vector3(x, Y + 2.25f, z + 0.2f);
                var yt = yl.AddComponent<Light>();
                yt.type = LightType.Point; yt.color = new Color(1f, 0.86f, 0.62f);
                yt.intensity = 2.4f; yt.range = 6f; yt.shadows = LightShadows.None;
            }
            // のぼり（石段の 上の 入り口に 2本）
            for (int k = -1; k <= 1; k += 2) {
                float x = -45f + k * 5.4f;
                Box(root, "M_NoboriBo" + k, new Vector3(x, Y + 1.9f, 27.5f), new Vector3(0.08f, 3.8f, 0.08f), mWood);
                Box(root, "M_Nobori" + k, new Vector3(x + 0.34f, Y + 2.6f, 27.5f), new Vector3(0.6f, 2.0f, 0.04f), mAka);
            }
        }

        // ---- 高台（+6m・南西）と やぐら
        Box(root, "Oka_Takadai", new Vector3(-62f, 3f, -32f), new Vector3(34f, 6f, 30f), mGround);
        Ramp(root, "Saka_Takadai", new Vector3(-44f, 0f, -32f), 270f, 14f, 6f, 3.6f, mRoad);
        Box(root, "Yagura_Ashi", new Vector3(-64f, 6f + 2.6f, -34f), new Vector3(2.6f, 5.2f, 2.6f), mWood);
        Box(root, "Yagura_Ue",  new Vector3(-64f, 6f + 5.6f, -34f), new Vector3(3.4f, 0.9f, 3.4f), mWood);

        // ---- 母屋（南東）。★S0-4：入れる 家に した。北壁に 戸口、屋内カメラの あいだは
        //   北壁だけ 消える（すかし＝IeKabeN）。屋根は 屋内カメラの 視線を さえぎらない 高さ
        Box(root, "Omoya_Yuka", new Vector3(42f, 0.15f, -42f), new Vector3(24f, 0.3f, 12f), mTatamiT);
        // 北壁（戸口 1.3m を x=40 に。3まい＝左・右・かもいの上）
        Box(root, "IeKabeN_L", new Vector3(34.6f, 1.45f, -36.2f), new Vector3(9.2f, 2.6f, 0.3f), mPlaster);
        Box(root, "IeKabeN_R", new Vector3(47.6f, 1.45f, -36.2f), new Vector3(12.7f, 2.6f, 0.3f), mPlaster);
        Box(root, "IeKabeN_Ue", new Vector3(40.7f, 2.35f, -36.2f), new Vector3(1.4f, 0.8f, 0.3f), mPlaster);
        // 南・東・西の 壁
        Box(root, "IeKabeS", new Vector3(42f, 1.45f, -47.8f), new Vector3(24f, 2.6f, 0.3f), mPlaster);
        Box(root, "IeKabeW", new Vector3(30.2f, 1.45f, -42f), new Vector3(0.3f, 2.6f, 12f), mPlaster);
        Box(root, "IeKabeE", new Vector3(53.8f, 1.45f, -42f), new Vector3(0.3f, 2.6f, 12f), mPlaster);
        // 中の しきり（西の 部屋と 東の 土間。戸口 1.3m）
        Box(root, "IeSikiri_N", new Vector3(38f, 1.45f, -40.4f), new Vector3(0.25f, 2.6f, 3.4f), mPlaster);
        Box(root, "IeSikiri_S", new Vector3(38f, 1.45f, -46.2f), new Vector3(0.25f, 2.6f, 3.2f), mPlaster);
        // ちゃぶ台と ふとん（部屋の 目じるし）
        Box(root, "Chabudai", new Vector3(34f, 0.5f, -42f), new Vector3(1.6f, 0.4f, 1.6f), mWood);
        Box(root, "Futon", new Vector3(50f, 0.4f, -45f), new Vector3(2.2f, 0.25f, 1.4f), mGrey);
        // 屋根（高さ 3.4〜4.0。屋内カメラは 3.2 より 低い 視線で 入る）
        Box(root, "Omoya_Yane", new Vector3(42f, 3.7f, -42f), new Vector3(26f, 0.6f, 14f), mRoofT);
        // 屋内の 明かり（裸電球ふう。屋根で 太陽が 入らない）
        void Denkyu(string n, float x, float z) {
            var g = new GameObject("Denkyu_" + n);
            g.transform.SetParent(root, false);
            g.transform.position = new Vector3(x, 2.6f, z);
            var li = g.AddComponent<Light>();
            li.type = LightType.Point; li.range = 9f; li.intensity = 14f;
            li.color = new Color(1f, 0.9f, 0.7f);
        }
        Denkyu("doma", 46f, -42f);
        Denkyu("heya", 34f, -42f);
        Box(root, "Ido", new Vector3(33f, 0.5f, -33f), new Vector3(1.4f, 1.0f, 1.4f), mGrey);
        Box(root, "Monohoshi", new Vector3(47f, 1.1f, -33f), new Vector3(6f, 0.1f, 0.1f), mGrey);
        Tree(root, 51f, -31f, 4.5f, 0f);   // 庭の木（セミの羽化・夜の観察の場）
        // 精霊馬の 置き台（縁側の まえ。中身は 13-15日だけ 出す＝R4）
        Box(root, "Shoryo_Dai", new Vector3(37f, 0.35f, -35.4f), new Vector3(1.2f, 0.3f, 0.5f), mWood);
        // ★家の 裏は 生垣で ふさぐ（本人の 方針：隠れる 場所は 配置で 消す。
        //   HD-2Dで カメラが 家に めり込む 経路も これで 消える）
        Box(root, "Ikegaki_Ura", new Vector3(42f, 1.0f, -49f), new Vector3(36f, 2.0f, 1.2f), mGreen);
        Box(root, "Ikegaki_UraE", new Vector3(59f, 1.0f, -44f), new Vector3(1.2f, 2.0f, 11f), mGreen);

        // ---- 田んぼ と あぜ道（中央 南）
        for (int i = 0; i < 3; i++)
            for (int k = 0; k < 2; k++) {
                Box(root, "Ta" + i + k, new Vector3(2f + i * 13f, 0.06f, -22f + k * 9f),
                    new Vector3(12f, 0.12f, 8f), mPaddy);
                var mizu = Box(root, "TaMizu" + i + k, new Vector3(2f + i * 13f, 0.13f, -22f + k * 9f),
                    new Vector3(11.4f, 0.02f, 7.4f), mWater);
                Object.DestroyImmediate(mizu.GetComponent<Collider>());
            }
        Box(root, "Kakashi_Bo", new Vector3(8f, 1.0f, -17f), new Vector3(0.2f, 2.0f, 0.2f), mWood);
        Box(root, "Kakashi_Te", new Vector3(8f, 1.5f, -17f), new Vector3(1.4f, 0.15f, 0.15f), mWood);
        Box(root, "Kakashi_Atama", new Vector3(8f, 2.2f, -17f), new Vector3(0.5f, 0.4f, 0.5f), mRed);
        // 用水路（田んぼの きわ）
        Box(root, "Yosui", new Vector3(2f, -0.05f, -12.5f), new Vector3(40f, 0.25f, 1.2f), mWater);

        // ---- 道（幹線 3.6m・枝 1.8m。うすい 板を 地めんに はる）
        void Road(string n, Vector2 a, Vector2 b, float w) {
            var mid = (a + b) * 0.5f; var d = b - a;
            var g = Box(root, n, new Vector3(mid.x, 0.03f, mid.y),
                        new Vector3(w, 0.06f, d.magnitude + w), mRoad);
            g.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg, 0f);
            Object.DestroyImmediate(g.GetComponent<Collider>());   // 道は 絵だけ（地めんを 歩く）
        }
        Road("Michi_Kansen1", new Vector2(42f, -35f), new Vector2(20f, -14f), 3.6f);  // 母屋→あぜ
        Road("Michi_Kansen2", new Vector2(20f, -14f), new Vector2(-30f, -2f), 3.6f);  // あぜ→橋
        Road("Michi_Kansen3", new Vector2(-30f, 14f), new Vector2(-45f, 20f), 3.6f);  // 橋→石段した
        Road("Michi_Takadai", new Vector2(-38f, -10f), new Vector2(-44f, -32f), 1.8f);
        Road("Michi_Ike",     new Vector2(-20f, 14f), new Vector2(50f, 24f), 1.8f);
        Road("Michi_Take",    new Vector2(30f, -25f), new Vector2(58f, -25f), 1.8f);
        Road("Michi_Bus",     new Vector2(42f, -49f), new Vector2(12f, -56f), 1.8f);

        // ---- 川の 渡り 3つ：橋（幹線）・飛び石・浅瀬
        Box(root, "Hashi", new Vector3(-30f, 0.15f, 8f), new Vector3(4.2f, 0.3f, 7.6f), mWood);
        Box(root, "Hashi_TesuriL", new Vector3(-32f, 0.75f, 8f), new Vector3(0.15f, 0.9f, 7.6f), mWood);
        Box(root, "Hashi_TesuriR", new Vector3(-28f, 0.75f, 8f), new Vector3(0.15f, 0.9f, 7.6f), mWood);
        for (int j = 0; j < 5; j++)
            Box(root, "Tobiishi" + j, new Vector3(20f + (j % 2 == 0 ? 0.4f : -0.4f), -0.2f, 5.4f + j * 1.3f),
                new Vector3(1.1f, 0.5f, 1.1f), mGrey);
        Box(root, "Asase", new Vector3(45f, -0.5f, 8f), new Vector3(6f, 0.44f, 6.4f), mGrey);

        // ---- 川上流の 岩場と 淵（D-111：飛び込み・ゴーグル潜り・水泳大会の 会場）
        // 淵＝色の 濃い 深み（絵だけ）。岩＝低い 足場(0.3きざみ)から 飛び込み岩(1.8m)へ
        var fuchi = Box(root, "Fuchi", new Vector3(-60f, -0.36f, 8f), new Vector3(14f, 0.06f, 5.2f),
                        Mat("MuraFukamizu", new Color(0.22f, 0.38f, 0.52f)));
        Object.DestroyImmediate(fuchi.GetComponent<Collider>());
        Box(root, "Iwa_Fumi1", new Vector3(-63f, 0.15f, 12.4f), new Vector3(2.0f, 0.3f, 1.6f), mGrey);
        Box(root, "Iwa_Fumi2", new Vector3(-61f, 0.45f, 12.2f), new Vector3(1.8f, 0.9f, 1.5f), mGrey);
        Box(root, "Iwa_Tobikomi", new Vector3(-59f, 0.9f, 12.0f), new Vector3(2.4f, 1.8f, 2.0f), mGrey);
        // ---- 川岸の 草ぎわ（ガサガサの 場。ヨシの 帯・あたり無し）
        for (int i = 0; i < 9; i++) {
            var yoshi = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            yoshi.name = "Yoshi" + i; yoshi.transform.SetParent(root, false);
            yoshi.transform.position = new Vector3(13.5f + i * 1.6f, 0.5f, 4.7f + (i % 3) * 0.3f);
            yoshi.transform.localScale = new Vector3(0.12f, 0.55f, 0.12f);
            yoshi.GetComponent<Renderer>().sharedMaterial = mGreen;
            Object.DestroyImmediate(yoshi.GetComponent<Collider>());
        }

        // ---- 駄菓子屋（橋の たもと 南）・池（東）・バス停（南の入り口）
        Box(root, "Dagashiya", new Vector3(-38f, 1.5f, -1f), new Vector3(7f, 3.0f, 5f), mGrey);
        Box(root, "Dagashiya_Noren", new Vector3(-38f, 1.9f, 1.7f), new Vector3(5.4f, 1.1f, 0.1f),
            Mat("MuraAi", new Color(0.20f, 0.28f, 0.52f)));
        Box(root, "Ike", new Vector3(55f, -0.15f, 26f), new Vector3(16f, 0.3f, 12f), mWater);
        foreach (var p in new[] { new Vector2(49,22), new Vector2(60,30), new Vector2(53,31) })
            Box(root, "Hasu" + p.x, new Vector3(p.x, 0.05f, p.y), new Vector3(1.6f, 0.05f, 1.6f), mGreen);
        Box(root, "Bustei_Hashira", new Vector3(10f, 1.4f, -57f), new Vector3(0.2f, 2.8f, 0.2f), mDark);
        Box(root, "Bustei_Fuda", new Vector3(10f, 2.4f, -57f), new Vector3(1.0f, 0.7f, 0.1f), mGrey);

        // ---- 山道（北・直角2回）→ ひみつきち／蛍の沢／ぬしの木。山すそは +8 の 壁
        Box(root, "Yama_Kabe", new Vector3(0f, 4f, 62f), new Vector3(184f, 8f, 8f), mGround);  // 草の斜面に 見せる（フラットな 黄緑は 目立ちすぎ）
        Ramp(root, "Yamamichi1", new Vector3(-20f, 4f, 44f), 0f, 12f, 2f, 3.0f, mRoad);   // 丘つづき→上へ
        Box(root, "Oka_Yama", new Vector3(0f, 5f, 52f), new Vector3(120f, 2f, 12f), mGround); // 山すその 棚(+6)
        // 丘(祠+4)から 山の棚(+6)へ：まがって のぼる
        Box(root, "Himitsu", new Vector3(-10f, 6f + 1.1f, 52f), new Vector3(3.2f, 2.2f, 2.6f), mWood);

        // ---- こだわりの 道（R5・見た目の 基準を 1本で つくる。本人 2026-08-23 の 写真より）
        //   写真の 構造：人が 歩く すじだけ 土が むき出し → 縁は 苔と まばらな 草 →
        //   外は 草むら。低い 草（房）と 高い 木が まざり、みきの あいだから 遠くと 空が 抜ける
        {
            Material MatA(string name, string tex) {          // 透かし付き（草の房・落ち葉）
                var m = Mat(name, Color.white);
                m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);  // 裏からも 見える
                m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/" + tex);
                return m;
            }
            var mTsuchi = MatT("MuraMichiTsuchiT", "michi_tsuchi.png", 0.7f, 1.4f);
            var mFuchi  = MatT("MuraMichiFuchiT",  "michi_fuchi.png", 1.6f, 2.0f);
            var mKiKawa = MatT("MuraKiKawaT", "ki_kawa.png", 1f, 3f);
            var mHaMori = MatT("MuraHaMoriT", "ha_mori.png", 2f, 2f);
            var mTuft   = MatA("MuraKusaTuftA", "kusa_tuft.png");
            var mOchiba = MatA("MuraOchibaA", "ochiba.png");
            Random.InitState(20260823);                        // 配置は 毎回 同じ（差分を 見る ため）
            const float TopY = 6f;                             // 山の 棚の 上めん
            float PathZ(float x) => 52f + 2.6f * Mathf.Sin((x + 22f) * 0.13f);  // 蛇行（ひみつきちを よける）
            // 地面の 凸凹（写真：じめんは 平らじゃない）。道と 縁は 平らの まま、端は 0 に
            float Deko(float wx, float wz) {
                float damp = Mathf.Clamp01((Mathf.Abs(wz - PathZ(wx)) - 1.9f) / 1.6f);
                float edge = Mathf.Clamp01(Mathf.Min(wz - 47f, 57f - wz) / 1.0f);
                return Mathf.Max(0f, (Mathf.PerlinNoise(wx * 0.35f + 3.1f, wz * 0.35f) - 0.35f) * 0.5f) * damp * edge;
            }
            // level0破損の 切り分け用スイッチ（環境変数 KODA：m=道 k=草 o=落ち葉 t=木。無指定=全部）
            string koda = System.Environment.GetEnvironmentVariable("KODA") ?? "mkot";
            Debug.Log("[Probe] KODA=" + koda);

            // 道すじ 2層：下＝苔の 縁どり帯（ひろい）、上＝踏み固めた 土（せまい・幅ゆらぎ）。
            // ★薄い 箱を 並べると 継ぎ目が 見える（本人 2026-08-25「薄い四角を配置してる？」）
            //   → 曲線に そった **1枚の リボンメッシュ**で 作る
            void Ribbon(string name, System.Func<float, float> half, float yOff, Material m) {
                const float x0 = -16.5f, x1 = 46f, dx = 1.1f;
                int n = Mathf.CeilToInt((x1 - x0) / dx);
                var verts = new Vector3[(n + 1) * 2];
                var uv = new Vector2[verts.Length];
                for (int i = 0; i <= n; i++) {
                    float x = Mathf.Min(x1, x0 + i * dx);
                    float zc = PathZ(x);
                    float zd = 2.6f * 0.13f * Mathf.Cos((x + 22f) * 0.13f);   // dPathZ/dx
                    var side = new Vector3(-zd, 0f, 1f).normalized;           // 接線と 直交
                    float hw = half(x);
                    verts[i * 2]     = new Vector3(x, TopY + yOff, zc) - side * hw;
                    verts[i * 2 + 1] = new Vector3(x, TopY + yOff, zc) + side * hw;
                    uv[i * 2]     = new Vector2(0f, x * 0.5f);
                    uv[i * 2 + 1] = new Vector2(1f, x * 0.5f);
                }
                var tris = new int[n * 6];
                for (int i = 0; i < n; i++) {
                    int a = i * 2;
                    tris[i * 6] = a; tris[i * 6 + 1] = a + 1; tris[i * 6 + 2] = a + 2;
                    tris[i * 6 + 3] = a + 1; tris[i * 6 + 4] = a + 3; tris[i * 6 + 5] = a + 2;
                }
                var mesh = new Mesh { vertices = verts, uv = uv, triangles = tris };
                mesh.RecalculateNormals();
                var g = new GameObject(name);
                g.transform.SetParent(root, false);
                g.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = g.AddComponent<MeshRenderer>();
                mr.sharedMaterial = m;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            if (koda.Contains("m")) {
                Ribbon("MichiFuchi", x => 1.7f, 0.020f, mFuchi);
                Ribbon("MichiTsuchi", x => 0.75f + 0.18f * Mathf.Sin(x * 0.7f), 0.036f, mTsuchi);
            }
            // 凸凹の 地面の 皮（見た目＋当たり。草・木も Deko と 同じ 高さに 置く）
            if (koda.Contains("m")) {
                int nx = 128, nzc = 20; float gx0 = -17f, gz0 = 47f; const float step = 0.5f;
                var verts = new Vector3[(nx + 1) * (nzc + 1)];
                var uv = new Vector2[verts.Length];
                for (int iz = 0; iz <= nzc; iz++)
                    for (int ix = 0; ix <= nx; ix++) {
                        float wx = gx0 + ix * step, wz = gz0 + iz * step;
                        verts[iz * (nx + 1) + ix] = new Vector3(wx, TopY + 0.012f + Deko(wx, wz), wz);
                        uv[iz * (nx + 1) + ix] = new Vector2(wx * 0.5f, wz * 0.5f);
                    }
                var tris = new int[nx * nzc * 6];
                int ti = 0;
                for (int iz = 0; iz < nzc; iz++)
                    for (int ix = 0; ix < nx; ix++) {
                        int a = iz * (nx + 1) + ix;
                        tris[ti++] = a; tris[ti++] = a + nx + 1; tris[ti++] = a + 1;
                        tris[ti++] = a + 1; tris[ti++] = a + nx + 1; tris[ti++] = a + nx + 2;
                    }
                var jm = new Mesh { vertices = verts, uv = uv, triangles = tris };
                jm.RecalculateNormals();
                var jg = new GameObject("JimenDekoboko");
                jg.transform.SetParent(root, false);
                jg.AddComponent<MeshFilter>().sharedMesh = jm;
                jg.AddComponent<MeshRenderer>().sharedMaterial = mGround;
                jg.AddComponent<MeshCollider>().sharedMesh = jm;
            }

            // 板ポリの もとに なる Quad メッシュ（結合用）
            Mesh quadMesh;
            {
                var tmpQ = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quadMesh = tmpQ.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(tmpQ);
            }
            GameObject Katamari(string name, List<CombineInstance> cis, Material m, bool kage = false, bool ueMuki = false) {
                var mesh = new Mesh();
                mesh.CombineMeshes(cis.ToArray(), true, true);
                if (ueMuki) {
                    // ★草の 板は 表裏で ライティングが 割れて 2色に 見える（本人 2026-08-25）
                    //   → 法線を ぜんぶ 上向きに して 地面と 同じ 光の 受けかたに（ビルボード草の 定石）
                    var ns = new Vector3[mesh.vertexCount];
                    for (int i = 0; i < ns.Length; i++) ns[i] = Vector3.up;
                    mesh.normals = ns;
                }
                var g = new GameObject(name);
                g.transform.SetParent(root, false);
                g.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = g.AddComponent<MeshRenderer>();
                mr.sharedMaterial = m;
                // ★薄い 板の 影は シャドウマップで ちらつく（本人 2026-08-23）→ 草・落ち葉は 影を 落とさない。
                //   大きい 葉カード・枯れ枝は kage=true（木もれ日の 陰影）
                if (!kage) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                return g;
            }

            // 草の 房（十字の 板）。道の 上は 0、縁は まばら、外は **大量に** しげる。
            // 2400束＝板4800枚でも、8mごとの チャンクに **メッシュ結合**するので 描画は 十数回ぶん
            var mTakeKusa = MatA("MuraKusaTakeA", "kusa_take.png");
            if (koda.Contains("k")) {
                var chunks = new Dictionary<int, List<CombineInstance>>();   // key = チャンク*2 + (0=低い/1=高い)
                void KusaAt(float x, float z, float s, int kind) {
                    int key = Mathf.FloorToInt((x + 17f) / 8f) * 2 + kind;
                    if (!chunks.TryGetValue(key, out var list)) chunks[key] = list = new List<CombineInstance>();
                    float yaw0 = Random.Range(0f, 180f);
                    for (int q = 0; q < 2; q++)
                        list.Add(new CombineInstance {
                            mesh = quadMesh,
                            transform = Matrix4x4.TRS(
                                new Vector3(x, TopY + Deko(x, z) + s * 0.5f, z),
                                Quaternion.Euler(0f, yaw0 + 90f * q + Random.Range(-20f, 20f), 0f),
                                new Vector3(s * 1.15f, s, 1f)),
                        });
                }
                // ★写真は 草が 地面を「覆う」（本人 2026-08-24）→ 9000束。低い 覆い草が 主体
                for (int i = 0; i < 9000; i++) {
                    float x = Random.Range(-17f, 47f);
                    float z = Random.Range(47.2f, 56.8f);
                    float dz = Mathf.Abs(z - PathZ(x));
                    if (dz < 0.95f) continue;                         // 踏まれる ところに 草は ない
                    if (dz < 1.9f && Random.value < 0.55f) continue;  // 縁は まばら
                    if (dz > 2.4f && Random.value < 0.10f) KusaAt(x, z, Random.Range(0.55f, 0.95f), 1);
                    else KusaAt(x, z, Random.Range(0.16f, 0.40f), 0);
                }
                foreach (var kv in chunks)
                    Katamari("KusaChunk" + kv.Key, kv.Value, (kv.Key % 2) == 1 ? mTakeKusa : mTuft, false, true);
            }

            // 落ち葉・苔・枯れ枝（写真：苔が あり、枝が 落ちていて、陰影が ある）
            var mKoke = MatA("MuraKokeA", "koke.png");
            if (koda.Contains("o")) {
                var cis = new List<CombineInstance>();
                for (int i = 0; i < 90; i++) {
                    float x = Random.Range(-16f, 45f);
                    float z = PathZ(x) + Random.Range(-2.1f, 2.1f);
                    float s = Random.Range(0.45f, 0.85f);
                    cis.Add(new CombineInstance {
                        mesh = quadMesh,
                        transform = Matrix4x4.TRS(
                            new Vector3(x, TopY + 0.062f + Deko(x, z), z),
                            Quaternion.Euler(90f, Random.Range(0f, 360f), 0f),
                            new Vector3(s, s, 1f)),
                    });
                }
                Katamari("OchibaChunk", cis, mOchiba);
                // 苔（本人 2026-08-25「どの辺を見れば？」→ 小さすぎて 見えなかった）：
                // 写真の とおり **帯に なって 道の 上や きわを おおう**。かたまりで 置く
                var koCis = new List<CombineInstance>();
                for (int c = 0; c < 26; c++) {
                    float cx = Random.Range(-15f, 45f);
                    float cz = (c % 2 == 0)
                        ? PathZ(cx) + Random.Range(-0.6f, 0.6f)                                  // 道の 上
                        : PathZ(cx) + (Random.value < 0.5f ? -1f : 1f) * Random.Range(1.0f, 2.2f); // きわ
                    for (int i = 0; i < 10; i++) {
                        float x = cx + Random.Range(-1.3f, 1.3f);
                        float z = cz + Random.Range(-0.9f, 0.9f);
                        float s = Random.Range(0.8f, 2.0f);
                        koCis.Add(new CombineInstance {
                            mesh = quadMesh,
                            transform = Matrix4x4.TRS(
                                new Vector3(x, TopY + 0.055f + Deko(x, z), z),
                                Quaternion.Euler(90f, Random.Range(0f, 360f), 0f),
                                new Vector3(s, s, 1f)),
                        });
                    }
                }
                Katamari("KokeChunk", koCis, mKoke, false, true);
                // 枯れ枝（ころがる 小枝。影あり）
                Mesh cylMesh;
                {
                    var t2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    cylMesh = t2.GetComponent<MeshFilter>().sharedMesh;
                    Object.DestroyImmediate(t2);
                }
                // 枯れ枝＝先細り＋折れ曲がり 2〜3節（丸い 棒 1本は おかしい・本人 2026-08-25）
                var edCis = new List<CombineInstance>();
                for (int i = 0; i < 46; i++) {
                    float x = Random.Range(-16f, 46f);
                    float z = PathZ(x) + Random.Range(-3.4f, 3.4f);
                    var p = new Vector3(x, TopY + 0.05f + Deko(x, z), z);
                    float yaw = Random.Range(0f, 360f);
                    var dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                    float len = Random.Range(0.35f, 0.8f), r = Random.Range(0.035f, 0.06f);
                    int setsu = Random.Range(2, 4);
                    for (int s2 = 0; s2 < setsu; s2++) {
                        edCis.Add(new CombineInstance {
                            mesh = cylMesh,
                            transform = Matrix4x4.TRS(
                                p + dir * (len * 0.5f),
                                Quaternion.FromToRotation(Vector3.up, dir),
                                new Vector3(r, len * 0.5f, r)),
                        });
                        p += dir * (len * 0.92f);
                        dir = (Quaternion.Euler(0f, Random.Range(-40f, 40f), 0f) * dir
                               + Vector3.up * Random.Range(-0.06f, 0.10f)).normalized;
                        r *= 0.62f; len *= 0.78f;
                    }
                }
                Katamari("EdaKareChunk", edCis, mKiKawa, true);
            }

            // 高い 木 v5（本人 2026-08-25「円柱のくっつく部分がいびつ。ポリゴンを増やして精密に」）：
            //   円柱の 継ぎ足しを 廃止。**背骨に そって 輪を ならべて 面を 張る チューブメッシュ**で
            //   みきも 枝も 1本の 連続した 皮に する（関節の 段差が 出ない）。
            //   葉は カードの 房（影あり・弱い 自己発光）。みきは 全部で 1メッシュに 結合
            var mHaCard = MatA("MuraHaCardA", "happa_card.png");
            var haCis = new List<CombineInstance>();
            var mikiCis = new List<CombineInstance>();
            void Tube(Vector3[] pts, float[] rad, int sides) {
                int n = pts.Length;
                var verts = new Vector3[n * sides + 1];
                var uv = new Vector2[verts.Length];
                var nrm = Vector3.Cross((pts[1] - pts[0]).normalized, Vector3.right);
                if (nrm.sqrMagnitude < 0.01f) nrm = Vector3.forward; else nrm = nrm.normalized;
                float vlen = 0f;
                for (int i = 0; i < n; i++) {
                    var dir = (i == 0 ? pts[1] - pts[0]
                             : i == n - 1 ? pts[n - 1] - pts[n - 2]
                             : pts[i + 1] - pts[i - 1]).normalized;
                    nrm = (nrm - dir * Vector3.Dot(nrm, dir)).normalized;   // ねじれない ように 前の 輪から 引きつぐ
                    var bin = Vector3.Cross(dir, nrm);
                    if (i > 0) vlen += (pts[i] - pts[i - 1]).magnitude;
                    for (int s = 0; s < sides; s++) {
                        float a = s * Mathf.PI * 2f / sides;
                        verts[i * sides + s] = pts[i] + (nrm * Mathf.Cos(a) + bin * Mathf.Sin(a)) * rad[i];
                        uv[i * sides + s] = new Vector2((float)s / sides, vlen * 0.8f);
                    }
                }
                verts[n * sides] = pts[n - 1];                              // 先端の ふさぎ
                uv[n * sides] = new Vector2(0.5f, vlen * 0.8f + 0.2f);
                var tris = new List<int>();
                for (int i = 0; i < n - 1; i++)
                    for (int s = 0; s < sides; s++) {
                        int s2 = (s + 1) % sides;
                        int a0 = i * sides + s, a1 = i * sides + s2;
                        int b0 = (i + 1) * sides + s, b1 = (i + 1) * sides + s2;
                        tris.Add(a0); tris.Add(a1); tris.Add(b0);
                        tris.Add(a1); tris.Add(b1); tris.Add(b0);
                    }
                for (int s = 0; s < sides; s++) {
                    int s2 = (s + 1) % sides;
                    tris.Add((n - 1) * sides + s); tris.Add(n * sides); tris.Add((n - 1) * sides + s2);
                }
                var mesh = new Mesh { vertices = verts, uv = uv, triangles = tris.ToArray() };
                mesh.RecalculateNormals();
                mikiCis.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.identity });
            }
            void HaCards(Vector3 at, float rr, int n) {        // ★枝先の 大量の 葉（world座標）
                for (int i = 0; i < n; i++) {
                    float cs = Random.Range(1.2f, 2.1f);
                    haCis.Add(new CombineInstance {
                        mesh = quadMesh,
                        transform = Matrix4x4.TRS(
                            at + Random.insideUnitSphere * rr,
                            Quaternion.Euler(Random.Range(-40f, 40f), Random.Range(0f, 360f), Random.Range(-25f, 25f)),
                            new Vector3(cs, cs * 0.8f, 1f)),
                    });
                }
            }
            void KiTakai(float x, float z, float h, float futosa) {
                float ybase = TopY + Deko(x, z) * 0.9f;
                float r0 = futosa * 0.5f;
                // みきの 背骨＝輪 9つ。ゆるく 湾曲、根もとは ひろがり、上に いくほど 細い
                const int rings = 9;
                var pts = new Vector3[rings]; var rad = new float[rings];
                var p = new Vector3(x, ybase - 0.2f, z); var dir = Vector3.up;
                for (int i = 0; i < rings; i++) {
                    pts[i] = p;
                    float t01 = (float)i / (rings - 1);
                    rad[i] = r0 * (i == 0 ? 1.55f : Mathf.Lerp(1.0f, 0.4f, t01))
                                * (1f + Random.Range(-0.05f, 0.05f));
                    p += dir * ((h + 0.2f) / (rings - 1));
                    dir = (dir + new Vector3(Random.Range(-0.08f, 0.08f), 0f, Random.Range(-0.08f, 0.08f))).normalized;
                }
                Tube(pts, rad, 12);
                var col = new GameObject("KiAtari");            // 当たりは カプセルで 別に
                col.transform.SetParent(root, false);
                col.transform.position = new Vector3(x, ybase + 1.7f, z);
                var cap = col.AddComponent<CapsuleCollider>();
                cap.radius = Mathf.Max(0.18f, r0); cap.height = 3.6f;
                // 枝＝3〜5本。みきの 輪の 位置から チューブで（先は 上へ しなる）
                int eda = Random.Range(3, 6);
                for (int e = 0; e < eda; e++) {
                    int baseRing = Random.Range(4, 8);
                    var moto = pts[baseRing];
                    float yaw = (360f / eda) * e + Random.Range(-30f, 30f);
                    float agari = Random.Range(22f, 48f) * Mathf.Deg2Rad;
                    var yoko = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                    var bdir = (yoko * Mathf.Cos(agari) + Vector3.up * Mathf.Sin(agari)).normalized;
                    float blen = h * Random.Range(0.22f, 0.34f);
                    const int bn = 5;
                    var bp = new Vector3[bn]; var br = new float[bn];
                    var q = moto; var d2 = bdir;
                    float br0 = rad[baseRing] * 0.6f;
                    for (int i = 0; i < bn; i++) {
                        bp[i] = q;
                        br[i] = br0 * Mathf.Lerp(1f, 0.22f, (float)i / (bn - 1));
                        q += d2 * (blen / (bn - 1));
                        d2 = (d2 + Vector3.up * 0.10f
                              + new Vector3(Random.Range(-0.08f, 0.08f), 0f, Random.Range(-0.08f, 0.08f))).normalized;
                    }
                    Tube(bp, br, 8);
                    // 小枝 1〜2本（さらに 細い チューブ）＋葉
                    int koeda = Random.Range(1, 3);
                    for (int k2 = 0; k2 < koeda; k2++) {
                        var m2 = (d2 + new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.2f, 0.5f), Random.Range(-0.6f, 0.6f))).normalized;
                        float n2 = blen * Random.Range(0.4f, 0.6f);
                        var kmoto = bp[Random.Range(2, 4)];
                        var kp = new Vector3[3];
                        var kr = new float[3] { br0 * 0.35f, br0 * 0.22f, br0 * 0.1f };
                        kp[0] = kmoto; kp[1] = kmoto + m2 * (n2 * 0.5f);
                        kp[2] = kmoto + m2 * n2 + Vector3.up * 0.15f;
                        Tube(kp, kr, 6);
                        HaCards(kp[2], Random.Range(0.9f, 1.3f), 7);
                    }
                    HaCards(bp[3], Random.Range(0.9f, 1.2f), 6);                    // 枝の とちゅう
                    HaCards(bp[bn - 1] + Vector3.up * 0.3f, Random.Range(1.0f, 1.5f), 9);   // 枝の 先
                }
                // てっぺんの 冠＝房を 3つ 横に ならべて 層に（1つの 玉に しない）
                var teppen = pts[rings - 1];
                for (int c2 = 0; c2 < 3; c2++) {
                    var off = Quaternion.Euler(0f, 120f * c2 + Random.Range(-30f, 30f), 0f) * Vector3.forward
                              * Random.Range(0.6f, 1.6f);
                    HaCards(teppen + off + Vector3.up * Random.Range(0.2f, 0.9f), Random.Range(1.1f, 1.6f), 11);
                }
            }
            // 道ぞいに 互いちがい・間かくは 4〜8m で ばらす（すき間が 抜け）
            if (koda.Contains("t")) {
                float[] kiX = { -14f, -8.5f, -2f, 4f, 9.5f, 16f, 21f, 27f, 33f, 39f, 43.5f };
                for (int i = 0; i < kiX.Length; i++) {
                    float side = (i % 2 == 0) ? 1f : -1f;
                    float off = Random.Range(1.8f, 3.4f) * side;
                    KiTakai(kiX[i], PathZ(kiX[i]) + off, Random.Range(7f, 10f), Random.Range(0.26f, 0.38f));
                }
                KiTakai(45f, 52f, 11f, 0.8f);   // ぬしの木（道ばたに ひときわ 太く 高く）
                // 北がわ（山すその 壁ぎわ）に もう 一列。幹が 層に なって 林に 見える＋壁を 隠す
                foreach (float kx in new[] { -12f, -5f, 2f, 9f, 18f, 26f, 30f, 42f })  // 35 は こみちカメラの 目の前 → 30 へ
                    KiTakai(kx, Random.Range(56.3f, 57.3f), Random.Range(8f, 11f), Random.Range(0.28f, 0.4f));
                // みき＝1メッシュ（影あり）。葉カード＝影あり＋弱い 自己発光で 逆光の 白飛びを おさえる
                //   （法線上書きを 葉の 塊に かけた v12/v13 は corrupted を 踏んだ ので 使わない）
                if (mikiCis.Count > 0) Katamari("KiMikiChunk", mikiCis, mKiKawa, true);
                mHaCard.EnableKeyword("_EMISSION");
                mHaCard.SetColor("_EmissionColor", new Color(0.20f, 0.28f, 0.14f));
                if (haCis.Count > 0) Katamari("HaCardChunk", haCis, mHaCard, true);
                // ※2Dドット絵の 木（ビルボード）は 本人判定「いまいち」で 廃止（2026-08-25）
            }
        }

        // ---- 竹やぶ（余白・東南）
        foreach (var p in new[] { new Vector2(60,-22), new Vector2(63,-27), new Vector2(58,-29),
                                  new Vector2(66,-23), new Vector2(62,-31) }) {
            var take = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            take.name = "Take"; take.transform.SetParent(root, false);
            take.transform.position = new Vector3(p.x, 2.5f, p.y);
            take.transform.localScale = new Vector3(0.25f, 2.5f, 0.25f);
            take.GetComponent<Renderer>().sharedMaterial = mGreen;
        }

        // ---- 見えない かべ（外周）
        Box(root, "BLK_S", new Vector3(0f, 2f, -61f), new Vector3(184f, 4f, 0.5f), null).GetComponent<Renderer>().enabled = false;
        Box(root, "BLK_N", new Vector3(0f, 2f, 61f), new Vector3(184f, 4f, 0.5f), null).GetComponent<Renderer>().enabled = false;
        Box(root, "BLK_W", new Vector3(-91f, 2f, 0f), new Vector3(0.5f, 4f, 124f), null).GetComponent<Renderer>().enabled = false;
        Box(root, "BLK_E", new Vector3(91f, 2f, 0f), new Vector3(0.5f, 4f, 124f), null).GetComponent<Renderer>().enabled = false;

        // ---- 主人公（実物と 同じ 寸法。見た目は マリサ 8方向スプライト＝S0-3）
        var player = new GameObject("Player");
        // はじまりは「こみち」＝こだわり道の 東・見くらべの 木の となり（本人 2026-08-25）
        player.transform.position = new Vector3(38f, 6.4f, 54.6f);   // 道の まん中
        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.0f; cc.radius = 0.26f; cc.center = new Vector3(0f, 0.52f, 0f);
        cc.slopeLimit = 50f; cc.stepOffset = 0.35f;
        var marisa = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sprites/marisa_8x8.png");
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Mi"; quad.transform.SetParent(player.transform, false);
        // 1コマ 115x167px。背たけ 1.30m に あわせて 横は 比で
        quad.transform.localPosition = new Vector3(0f, 0.66f, 0f);
        quad.transform.localScale = new Vector3(1.30f * 115f / 167f, 1.30f, 1f);
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        var sm = Mat("MuraMarisa", Color.white);
        sm.SetFloat("_AlphaClip", 1f); sm.SetFloat("_Cutoff", 0.5f);
        sm.EnableKeyword("_ALPHATEST_ON");
        sm.mainTexture = marisa;
        quad.GetComponent<Renderer>().sharedMaterial = sm;
        quad.AddComponent<MuraBillboard>();
        var cs = player.AddComponent<CharSprite>();
        cs.target = quad.GetComponent<Renderer>();
        cs.runSpeed = 3.4f;
        var mv = player.AddComponent<MuraMove>();
        mv.sprite = cs;
        // カメラ基準の 移動に つかう（あとで camGO を 入れる）

        // ---- 見せ場の たちば（MURA.md の 10枚。-tour が 順に 撮る）
        var tourNames = new[] { "縁側", "あぜ道", "川べり", "橋の上", "石段した",
                                "祠", "高台", "山道", "ひみつきち", "沢", "こみち", "きくらべ", "どま(屋内)" };
        var tourPos = new[] {
            // ★縁側は 母屋から 15m はなす。近いと カメラ(主人公の 南 9m)が 母屋の 箱の 中に 入る
            new Vector3(40f, 0f, -25f), new Vector3(12f, 0f, -18f), new Vector3(20f, 0f, 2f),
            new Vector3(-30f, 0.4f, 8f), new Vector3(-45f, 0f, 18f), new Vector3(-45f, 4.2f, 32f),
            new Vector3(-58f, 6.2f, -30f), new Vector3(-20f, 4.2f, 44f), new Vector3(-10f, 6.2f, 49f),
            new Vector3(25f, 6.2f, 50f), new Vector3(38f, 6.3f, 54.6f), new Vector3(30f, 6.4f, 52.6f),
            new Vector3(46f, 0.5f, -42f) };
        var tour = new Transform[tourPos.Length];
        for (int i = 0; i < tourPos.Length; i++) {
            var g = new GameObject("Mise_" + tourNames[i]);
            g.transform.SetParent(root, false); g.transform.position = tourPos[i];
            tour[i] = g.transform;
        }
        mv.tour = tour;

        // ---- カメラ（本編と 同じ CamOrbit＋ゾーン）
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.fieldOfView = 46f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 400f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.70f, 0.80f, 0.88f);
        camGO.AddComponent<AudioListener>();   // ★これが 無いと 全部 無音（手組みカメラの 抜け）
        var camData = camGO.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;                 // DoF/Bloom を 効かせる
        camData.antialiasing = AntialiasingMode.None;        // ドット絵を にじませない
        // ★ぼくなつ式の 固定カメラ（S0-2 v1.3・本人「一旦カメラ固定を試せないか」）。
        //   追従（CamOrbit）は やめた。手で 置いた カメラの あいだを **カットで** 切り替える。
        //   置き場所の 決めごと：
        //   - 家の まえは「家が 画面に 入る」がわに 置く（家に 向かって いるのに
        //     家が 見えない・消される、を なくす）
        //   - 地形に 埋まらない 高さに 手で 置く（自動よけを しない＝パッと 動かない）
        //   - 領域は 道なりに 並べ、さかい目は 曲がり角に 置く（動線と カットを そろえる）
        var fix = camGO.AddComponent<MuraCamFixed>();
        fix.target = player.transform;
        // ★1台＝1つの 被写体（本人 2026-08-18「家なら家、下に行ったら田んぼだけ、
        //   さらに下は橋を俯瞰、くらいの 寄り」）。lookAt は 被写体そのもの
        MuraCamFixed.Spot S(string name, float ax, float az, float sx, float sz,
                            float px, float py, float pz,
                            float lx, float ly, float lz, float fov = 44f) {
            return new MuraCamFixed.Spot {
                name = name,
                area = new Bounds(new Vector3(ax, 5f, az), new Vector3(sx, 24f, sz)),
                pos = new Vector3(px, py, pz),
                lookAt = new Vector3(lx, ly, lz), fov = fov,
            };
        }
        fix.spots = new[] {
            // ★S1-1（D-107）：範囲は 遮蔽物の 裏を 含まない 形に 整形、置き場所は MuraCamCheck.Fit の 逆算。
            //   直したら 必ず -Only MuraCamCheck.Check
            S("いえ", 41f, -34f, 26f, 20f,   34.1f, 14.7f, -6.4f,   41f, 0.8f, -34f, 44f),  // 残る問題点=0
            S("ばすてい", 12f, -52f, 26f, 14f,   25.4f, 14.0f, -28.6f,   12f, 0.8f, -52f, 42f),  // 残る問題点=0
            S("みちみなみ", 27f, -27f, 16f, 26f,   22.2f, 14.4f, 0.5f,   27f, 0.8f, -27f, 42f),  // 残る問題点=0
            S("たんぼ", 10f, -18f, 26f, 20f,   10.0f, 15.4f, -48.0f,   10f, 0.8f, -18f, 42f),  // 残る問題点=0
            S("とびいし", 20f, 1f, 24f, 12f,   20.0f, 12.8f, -23.5f,   20f, 0.8f, 1f, 42f),  // 残る問題点=0
            S("はし", -30f, 6f, 16f, 12f,   -19.2f, 9.3f, -7.6f,   -30f, 0.8f, 6f, 44f),  // 残る問題点=0
            S("かわ きた", 6f, 16f, 44f, 12f,   6.0f, 20.1f, 55.6f,   6f, 0.8f, 16f, 44f),  // 残る問題点=0
            S("だがしや", -36f, -7f, 14f, 10f,   -32.2f, 8.5f, -22.3f,   -36f, 0.8f, -7f, 42f),  // 残る問題点=0
            S("いしだん", -45f, 16f, 14f, 12f,   -45.0f, 9.5f, -1.8f,   -45f, 0.8f, 16f, 40f),  // 残る問題点=0
            S("ほこら", -46f, 36f, 28f, 20f,   -15.7f, 16.1f, 27.5f,   -46f, 0.8f, 36f, 42f),  // 残る問題点=0
            S("たかだい", -54f, -32f, 14f, 24f,   -38f, 13.0f, -14f,   -54f, 0.8f, -34f, 48f),  // やぐらを 範囲外に・北東から（挟まれ回避）
            S("さか", -40f, -20f, 12f, 26f,   -41.5f, 12.9f, 4.8f,   -40f, 0.8f, -20f, 44f),  // 残る問題点=0
            S("いけ", 55f, 26f, 22f, 16f,   32.8f, 12.9f, 14.9f,   55f, 0.8f, 26f, 42f),  // 残る問題点=0
            S("たけやぶ", 60f, -26f, 14f, 14f,   50.0f, 9.6f, -10.9f,   60f, 0.8f, -26f, 42f),  // 残る問題点=0
            S("やまみち", -20f, 44f, 16f, 12f,   -20.0f, 9.3f, 26.6f,   -20f, 0.8f, 44f, 44f),  // 残る問題点=0
            S("ひみつきち", -10f, 51f, 16f, 8f,   -10f, 9.0f, 37.5f,   -10f, 0.8f, 51f, 42f),  // 真南から（Fitの斜めは枠不足だった）
            S("さわ", 25f, 50f, 20f, 10f,   25.0f, 10.8f, 29.6f,   25f, 0.8f, 50f, 42f),  // 残る問題点=0
            // こだわり道の 東半分：目の 高さ・南東向き（幹の あいだから 下の 村と 空が 抜ける＝写真の④)
            S("こみち", 41f, 52f, 14f, 12f,   34.0f, 8.0f, 57.0f,   44f, 4.2f, 44f, 50f),
            // 木の 見くらべ（3Dモデリング木 と 2Dドット絵木 を 全身で。D-113 二刀流の 判定用）。
            // ★ゾーンを こみちの 範囲の **そと**（西）に 置く：入れ子だと「今の カメラに 映っている」が
            //   勝って カットされない（居座りルールの 仕様）
            S("きくらべ", 30f, 52.6f, 3f, 3f,   28.0f, 7.6f, 46.5f,   37f, 10f, 52f, 55f),
            S("あさせ", 45f, 8f, 14f, 14f,   45.0f, 9.6f, -10.1f,   45f, 0.8f, 8f, 42f),
            // 岩場と 淵（D-111 の 新地形）。南から 岩と 淵を 一枚に
            S("いわば", -60f, 8.5f, 16f, 11f,   -60.0f, 8.0f, -9.0f,   -60f, 0.8f, 9f, 46f),  // 残る問題点=0
        };
        // ★S0-4 屋内カメラの 型：入ると カット＋北壁だけ 消える（sukashi）。
        //   **部屋ごとに 1台**（入口の 1台では 中じきりの むこうが 見えない＝Checkで 実測）
        {
            var list = new List<MuraCamFixed.Spot>(fix.spots);
            list.Add(new MuraCamFixed.Spot {
                name = "どま",
                area = new Bounds(new Vector3(46.5f, 1.2f, -42f), new Vector3(14f, 4.4f, 9.6f)),
                pos = new Vector3(46f, 2.9f, -30.5f),
                lookAt = new Vector3(46f, 0.6f, -43f), fov = 52f, sukashi = "IeKabeN",
            });
            list.Add(new MuraCamFixed.Spot {
                name = "へや",
                area = new Bounds(new Vector3(34f, 1.2f, -42f), new Vector3(7.4f, 4.4f, 9.6f)),
                pos = new Vector3(34f, 2.9f, -30.5f),
                lookAt = new Vector3(34f, 0.6f, -43.5f), fov = 44f, sukashi = "IeKabeN",
            });
            fix.spots = list.ToArray();
        }

        // 起動直後に どこにも 入って いない ときだけ（南の 空から 村ぜんたい＝山頂級の 俯瞰は
        // こういう 意図した 場面に だけ 使う）
        fix.fallback = new MuraCamFixed.Spot {
            name = "ひき", pos = new Vector3(0f, 26f, -78f),
            lookAt = new Vector3(0f, 0f, -20f), fov = 40f,
        };

        // ---- S1-2：俯瞰エディタ（F2）。カメラの 担当範囲・位置・向き、音源の 半径を 見る
        var fukan = camGO.AddComponent<MuraFukan>();
        fukan.fix = fix; fukan.target = player.transform;
        var uiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/PixelMplus12-Regular.ttf");
        fukan.font = uiFont; fix.font = uiFont;

        // ---- S1-3：音源（位置・届く半径・遮蔽つき。聞き手は 主人公）
        player.AddComponent<MuraOtoKikite>();
        void Oto(string name, MuraOto.Koe koe, float x, float y, float z,
                 float r, float vol, float pitch = 1f, int hy = 0) {
            var g = new GameObject("Oto_" + name);
            g.transform.SetParent(root, false);
            g.transform.position = new Vector3(x, y, z);
            var o = g.AddComponent<MuraOto>();
            o.namae = name; o.koe = koe; o.kikoeru = r; o.ookisa = vol; o.takasa = pitch; o.hiruYoru = hy;
        }
        Oto("せみ・ぬしの木", MuraOto.Koe.Semi, 45f, 7f, 52f, 26f, 0.6f, 1f, 1);
        Oto("せみ・すぎ", MuraOto.Koe.Semi, -56f, 6f, 42f, 22f, 0.45f, 0.93f, 1);
        Oto("かわ", MuraOto.Koe.Kawa, -10f, 0f, 8f, 30f, 0.8f);
        Oto("かわ・ひがし", MuraOto.Koe.Kawa, 40f, 0f, 8f, 26f, 0.7f, 1.15f);
        Oto("すずむし・たけやぶ", MuraOto.Koe.Suzumushi, 62f, 1f, -26f, 16f, 0.55f, 1f, 2);
        Oto("かえる・たんぼ", MuraOto.Koe.Kaeru, 8f, 0.3f, -18f, 18f, 0.5f, 1f, 2);

        // ---- あそびスポット（縦切り用の 器。EVENTS 採用ぶんの 小物）
        var te = player.AddComponent<MuraAsobiTe>();
        te.font = uiFont;
        void Asobi(string namae, string deki, float x, float y, float z, int hy = 0) {
            var g = new GameObject("Asobi_" + namae);
            g.transform.SetParent(root, false);
            g.transform.position = new Vector3(x, y, z);
            var a = g.AddComponent<MuraAsobi>();
            a.namae = namae; a.dekigoto = deki; a.hiruYoru = hy;
        }
        // 川がわ
        Asobi("石を ひっくり返す", "サワガニが いた！", 24f, 0f, 3.6f);
        Asobi("あみで ガサガサ", "ちいさな エビと ヤゴが とれた！", 17f, 0f, 4.8f, 1);
        Asobi("えいっと 飛びこむ", "ざぶん！ ……つめたい！", -59f, 1.9f, 12f, 1);
        {   // スイカ冷やし＝2段階（しずめる → 3時間 まつ → つめたい）
            var g = new GameObject("Asobi_スイカ");
            g.transform.SetParent(root, false);
            g.transform.position = new Vector3(6f, 0f, -12.2f);
            var a = g.AddComponent<MuraAsobi>();
            a.namae = "スイカを ひやす"; a.hiruYoru = 1;
            a.dekigoto = "用水路に しずめた。あとで とりに こよう";
            a.mada = "……まだ ぬるい。もう すこし あとで";
            a.dekigoto2 = "よく ひえてる！ 今夜は スイカだ";
            a.matsu = 3f;
        }
        // いえ がわ
        Asobi("縁の下を のぞく", "すり鉢の あな……アリジゴクだ", 34f, 0f, -35.8f, 1);
        Asobi("木を 見あげる", "セミの ようちゅうが のぼって いく……", 50.5f, 0f, -30.6f, 2);
        // 目じるしの 小石（しらべる 石が 見える ように）
        Box(root, "Ishi_Kani", new Vector3(24f, 0.12f, 3.6f), new Vector3(0.7f, 0.25f, 0.6f), mGrey);
        Asobi("ラムネを のむ", "しゅわしゅわ……王冠を もらった！", -37f, 0f, -3.4f, 1);

        // ---- 虫（うごく あそびスポット。昼＝チョウ/トンボ、夜＝ホタル）
        void Mushi(string name, string deki, Color c, float x, float z, float takasa,
                   int hy, bool hikaru = false) {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = "Mushi_" + name;
            g.transform.SetParent(root, false);
            g.transform.position = new Vector3(x, takasa, z);
            g.transform.localScale = new Vector3(0.22f, 0.1f, 0.22f);
            g.GetComponent<Renderer>().sharedMaterial = Mat("MuraMushi_" + name.Substring(0, 2), c);
            Object.DestroyImmediate(g.GetComponent<Collider>());
            var mm = g.AddComponent<MuraMushi>();
            mm.anchor = new Vector3(x, 0f, z); mm.takasa = takasa; mm.hiruYoru = hy;
            var a = g.AddComponent<MuraAsobi>();
            a.namae = "あみを ふる"; a.dekigoto = deki; a.hiruYoru = hy; a.kieru = true; a.chikasa = 1.6f;
            if (hikaru) {
                var li = g.AddComponent<Light>();
                li.type = LightType.Point; li.range = 3.5f; li.intensity = 2.4f;
                li.color = new Color(0.7f, 1f, 0.5f);
            }
        }
        Mushi("チョウ1", "モンシロチョウを つかまえた！", Color.white, 10f, -19f, 1.1f, 1);
        Mushi("チョウ2", "キアゲハを つかまえた！", new Color(1f, 0.85f, 0.2f), 18f, -14f, 1.2f, 1);
        Mushi("トンボ1", "シオカラトンボを つかまえた！", new Color(0.6f, 0.75f, 0.95f), 4f, -13f, 1.4f, 1);
        Mushi("トンボ2", "アキアカネを つかまえた！", new Color(0.9f, 0.3f, 0.25f), 26f, 1.5f, 1.3f, 1);
        Mushi("ホタル1", "ホタルを そっと つかまえた……", new Color(0.75f, 1f, 0.5f), 25f, 50f, 1.0f, 2, true);
        Mushi("ホタル2", "ホタルが 手の なかで ひかって いる", new Color(0.75f, 1f, 0.5f), 27f, 48f, 0.9f, 2, true);

        mv.cam = camGO.transform;
        // ★手前の 物を 透明にする 保険（MuraKabenuki）は 廃止（本人 2026-08-18
        //   「急に消えるのはおかしい。消えている物を実在すると認識しながら操作できない」）。
        //   遮蔽は カメラの 置き場所＝構図で 解く 一択に する

        // ---- ひかり（S0-3：舞台照明の 入口。夏の 昼の 基本＋祠の アクセント）
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional; sun.intensity = 1.25f;
        sun.color = new Color(1f, 0.95f, 0.84f);
        sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
        sun.shadows = LightShadows.Soft;
        // 縦切り1日（R4の 先がけ）：時計・太陽・チャイム・Zで ねる
        var dayGO = new GameObject("Day");
        var md = dayGO.AddComponent<MuraDay>();
        md.sun = sun; md.font = uiFont;
        dayGO.AddComponent<MuraHanabi>();   // 8月8日の 夜、南の 山なみの 上に 遠花火
        // ★環境光は Flat（色指定）に 固定。Skyboxモードだと MuraDay が 毎フレーム 入れる
        //   夜の 暗い ambient が 効かず、夜でも 夕方くらいの 明るさに なる（2026-08-25）
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.52f, 0.56f, 0.60f);
        // 空気感（奥ほど かすむ）。遠くの 山が 溶けて 遠近が 出る
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.74f, 0.78f, 0.74f);
        RenderSettings.fogDensity = 0.010f;   // ★もっと あってもいい（本人）→ 増量

        // ---- とおくの 山なみ（仮）。地平の 空白を 埋める。じっさいは 絵に 差し替える。
        //   霧で とけて シルエットに なる。あたりは つけない（遊び場の そと）
        // 近い 列は 樹冠テクスチャで「森の 山」に（素の 灰色板は 目立ちすぎ）。遠い 列は 霞いろ
        var mYama  = MatT("MuraYamaT", "ha_mori.png", 8f, 4f);
        var mYama2 = MatT("MuraYama2T", "ha_mori.png", 8f, 4f);
        mYama2.color = new Color(0.80f, 0.88f, 0.84f);   // 遠い 列は 霞んで うすく
        void Yama(float x, float z, float w, float d, float h, Material m) {
            var g = Box(root, "Yamanami", new Vector3(x, h * 0.5f - 2f, z), new Vector3(w, h, d), m);
            Object.DestroyImmediate(g.GetComponent<Collider>());
        }
        // 北（山。近い 列は こく、遠い 列は うすく）
        Yama(-70f, 92f, 120f, 24f, 26f, mYama);  Yama(10f, 98f, 150f, 26f, 34f, mYama);
        Yama(85f, 90f, 100f, 22f, 22f, mYama);
        Yama(-30f, 135f, 200f, 30f, 44f, mYama2); Yama(90f, 130f, 160f, 28f, 38f, mYama2);
        // 東・西（低い 尾根）
        Yama(125f, 30f, 26f, 140f, 18f, mYama);  Yama(150f, -40f, 30f, 160f, 28f, mYama2);
        Yama(-125f, 20f, 26f, 150f, 20f, mYama); Yama(-150f, -30f, 30f, 160f, 30f, mYama2);
        // 南（村の 入り口の むこうの 低い 丘）
        Yama(-40f, -115f, 130f, 24f, 14f, mYama); Yama(60f, -120f, 120f, 26f, 12f, mYama);
        // 祠の アクセント（舞台照明：見どころに 1灯）
        var spot = new GameObject("Spot_Hokora").AddComponent<Light>();
        spot.type = LightType.Spot; spot.intensity = 60f; spot.range = 22f;
        spot.spotAngle = 55f; spot.color = new Color(1f, 0.88f, 0.65f);
        spot.transform.position = new Vector3(-40f, 12f, 28f);
        spot.transform.rotation = Quaternion.LookRotation(
            new Vector3(-48f, 5f, 36f) - spot.transform.position);

        // ---- ポストFX（S0-3：HD-2Dの 型＝DoF・Bloom・ビネット・粒子・トーン）
        var volGO = new GameObject("PostFX");
        var vol = volGO.AddComponent<UnityEngine.Rendering.Volume>();
        vol.isGlobal = true;
        var prof = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
        AssetDatabase.CreateAsset(prof, "Assets/Art/Materials/Mura/MuraPostFX.asset");
        T AddFX<T>() where T : UnityEngine.Rendering.VolumeComponent {
            var c = prof.Add<T>(); return c;
        }
        var dof = AddFX<DepthOfField>();
        dof.mode.overrideState = true; dof.mode.value = DepthOfFieldMode.Bokeh;
        dof.focusDistance.overrideState = true; dof.focusDistance.value = 10f;
        dof.aperture.overrideState = true; dof.aperture.value = 3.0f;   // ぼかし 増量（本人）
        dof.focalLength.overrideState = true; dof.focalLength.value = 50f;
        var bloom = AddFX<Bloom>();
        bloom.threshold.overrideState = true; bloom.threshold.value = 1.0f;
        bloom.intensity.overrideState = true; bloom.intensity.value = 0.9f;
        bloom.tint.overrideState = true; bloom.tint.value = new Color(1f, 0.96f, 0.86f);
        var grade = AddFX<ColorAdjustments>();
        grade.postExposure.overrideState = true; grade.postExposure.value = 0.1f;
        grade.contrast.overrideState = true; grade.contrast.value = 10f;
        grade.saturation.overrideState = true; grade.saturation.value = 6f;
        var vig = AddFX<Vignette>();
        vig.intensity.overrideState = true; vig.intensity.value = 0.34f;
        vig.smoothness.overrideState = true; vig.smoothness.value = 0.46f;
        var tone = AddFX<Tonemapping>();
        tone.mode.overrideState = true; tone.mode.value = TonemappingMode.Neutral;
        vol.sharedProfile = prof;
        // ピントは 主人公に（FocusOnPlayer は 本編の 流用）
        var focus = volGO.AddComponent<FocusOnPlayer>();
        focus.volume = vol; focus.target = player.transform;

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, "Assets/Scenes/Mura.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("[Probe] BuildMura done");
    }
}
