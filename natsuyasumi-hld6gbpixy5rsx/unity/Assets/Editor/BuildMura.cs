using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

// R2 箱の村。MURA.md の 設計図を **グレーボックス**で 組む（部品の 差し替えは R5）。
// 大きさ・道・遮蔽・見せ場カメラを 実際に 歩いて たしかめる ための もの。
//   rebuild.ps1 -Only BuildMura.Build
public static class BuildMura {

    static Material mGround, mRoad, mWater, mWood, mRed, mGrey, mGreen, mDark, mPaddy;

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

        mGround = Mat("MuraGround", new Color(0.55f, 0.62f, 0.42f));
        mRoad   = Mat("MuraRoad",   new Color(0.62f, 0.55f, 0.42f));
        mWater  = Mat("MuraWater",  new Color(0.36f, 0.56f, 0.66f));
        mWood   = Mat("MuraWood",   new Color(0.45f, 0.35f, 0.26f));
        mRed    = Mat("MuraRed",    new Color(0.78f, 0.25f, 0.20f));
        mGrey   = Mat("MuraGrey",   new Color(0.72f, 0.72f, 0.70f));
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

        // ---- 高台（+6m・南西）と やぐら
        Box(root, "Oka_Takadai", new Vector3(-62f, 3f, -32f), new Vector3(34f, 6f, 30f), mGround);
        Ramp(root, "Saka_Takadai", new Vector3(-44f, 0f, -32f), 270f, 14f, 6f, 3.6f, mRoad);
        Box(root, "Yagura_Ashi", new Vector3(-64f, 6f + 2.6f, -34f), new Vector3(2.6f, 5.2f, 2.6f), mWood);
        Box(root, "Yagura_Ue",  new Vector3(-64f, 6f + 5.6f, -34f), new Vector3(3.4f, 0.9f, 3.4f), mWood);

        // ---- 母屋（南東・24x12 の 田の字の 置きしろ）と 庭・井戸
        Box(root, "Omoya", new Vector3(42f, 2.4f, -42f), new Vector3(24f, 4.8f, 12f), mGrey);
        Box(root, "Omoya_Yane", new Vector3(42f, 5.6f, -42f), new Vector3(26f, 1.6f, 14f), mDark);
        Box(root, "Ido", new Vector3(33f, 0.5f, -33f), new Vector3(1.4f, 1.0f, 1.4f), mGrey);
        Box(root, "Monohoshi", new Vector3(47f, 1.1f, -33f), new Vector3(6f, 0.1f, 0.1f), mGrey);

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
        Box(root, "Yama_Kabe", new Vector3(0f, 4f, 62f), new Vector3(184f, 8f, 8f), mGreen);
        Ramp(root, "Yamamichi1", new Vector3(-20f, 4f, 44f), 0f, 12f, 2f, 3.0f, mRoad);   // 丘つづき→上へ
        Box(root, "Oka_Yama", new Vector3(0f, 5f, 52f), new Vector3(120f, 2f, 12f), mGround); // 山すその 棚(+6)
        // 丘(祠+4)から 山の棚(+6)へ：まがって のぼる
        Box(root, "Himitsu", new Vector3(-10f, 6f + 1.1f, 52f), new Vector3(3.2f, 2.2f, 2.6f), mWood);
        foreach (var p in new[] { new Vector2(25,52), new Vector2(31,50), new Vector2(37,53) })
            Tree(root, p.x, p.y, 5f, 6f);
        Tree(root, 45f, 52f, 8f, 6f);   // ぬしの木（ひときわ 太く）
        var nushi = root.Find("Ki"); // 直近の Tree の みき。太らせる
        // （名まえが かぶる ので 最後に 足した みきを さがして 太らせる）
        foreach (Transform ch in root) if (ch.name == "Ki") nushi = ch;
        if (nushi != null) nushi.localScale = new Vector3(1.0f, nushi.localScale.y, 1.0f);

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
        player.transform.position = new Vector3(38f, 0.2f, -34f);   // 母屋の 庭から はじまる
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
                                "祠", "高台", "山道", "ひみつきち", "沢" };
        var tourPos = new[] {
            // ★縁側は 母屋から 15m はなす。近いと カメラ(主人公の 南 9m)が 母屋の 箱の 中に 入る
            new Vector3(40f, 0f, -25f), new Vector3(12f, 0f, -18f), new Vector3(20f, 0f, 2f),
            new Vector3(-30f, 0.4f, 8f), new Vector3(-45f, 0f, 18f), new Vector3(-45f, 4.2f, 32f),
            new Vector3(-58f, 6.2f, -30f), new Vector3(-20f, 4.2f, 44f), new Vector3(-10f, 6.2f, 49f),
            new Vector3(25f, 6.2f, 50f) };
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
            // 家（被写体＝母屋）：庭の 真北から 南の 家を 見る＝庭の どこに いても
            // 主人公は カメラと 家の あいだ（ななめに 置くと 領域の 半分が 視野の そとに 出る）
            S("いえ",       42f, -34f, 28f, 20f,   38f, 5.0f, -20f,   42f, 1.8f, -40f),
            // バス停（被写体＝バス停の 標識）
            S("ばすてい",   14f, -52f, 30f, 16f,   19f, 3.0f, -47f,   10f, 1.5f, -56f, 42f),
            // 田んぼ（被写体＝田と 案山子）：南の 高み から 田んぼだけ
            S("たんぼ",     14f, -18f, 40f, 22f,   15f, 6.0f, -32f,   13f, 0.5f, -17f),
            // 飛び石（被写体＝飛び石の 列）
            S("とびいし",   20f, 1f, 24f, 12f,     20f, 4.0f, -5f,    20f, -0.3f, 8f, 42f),
            // 橋（被写体＝橋。すこし 俯瞰）
            S("はし",      -30f, 8f, 18f, 18f,    -23f, 9.0f, -2f,   -30f, 0.3f, 8f),
            // 川の 北がわの 道（被写体＝川と 対岸）
            S("かわ きた",   6f, 16f, 44f, 12f,     6f, 5.0f, 24f,     6f, 0f, 8f),
            // 駄菓子屋（被写体＝店と のれん）
            S("だがしや",  -40f, -2f, 16f, 14f,   -31f, 3.0f, -8f,   -38f, 1.5f, -1f, 42f),
            // 石段（被写体＝鳥居。見上げの ハレの 絵）
            S("いしだん",  -45f, 16f, 14f, 12f,   -45f, 2.2f, 8f,    -45f, 4.5f, 26f, 40f),
            // 祠（被写体＝祠と 杉）
            S("ほこら",    -48f, 38f, 40f, 30f,   -37f, 8.5f, 26f,   -48f, 5.5f, 36f, 42f),
            // 高台（被写体＝谷の ながめ。ここだけは 引きが 正しい）
            S("たかだい",  -62f, -32f, 30f, 26f,  -74f, 10.5f, -32f, -30f, 2f, -10f, 50f),
            // 高台への 坂（被写体＝坂と やぐら）
            S("さか",      -40f, -20f, 12f, 26f,  -33f, 4.5f, -6f,   -44f, 3f, -24f),
            // 池（被写体＝池と ハス）
            S("いけ",       55f, 26f, 22f, 16f,    43f, 4.0f, 20f,    55f, 0.5f, 26f, 42f),
            // 竹やぶ（被写体＝竹。余白の 隅）
            S("たけやぶ",   62f, -26f, 18f, 16f,   55f, 3.0f, -17f,   62f, 2f, -26f, 42f),
            // 山道の 口（被写体＝道の 先の 暗がり）
            S("やまみち",  -20f, 44f, 16f, 12f,   -20f, 6.5f, 36f,   -20f, 6f, 50f),
            // ひみつきち（被写体＝小屋。子どもの 目線）
            S("ひみつきち", -10f, 52f, 18f, 10f,   -10f, 7.8f, 44f,   -10f, 7f, 52f, 42f),
            // 蛍の 沢（被写体＝木々の あいだ）
            S("さわ",       25f, 50f, 20f, 10f,    25f, 7.8f, 43f,    25f, 6.5f, 51f, 42f),
            // 浅瀬（被写体＝川底の 石）
            S("あさせ",     45f, 8f, 14f, 14f,     45f, 4.0f, -3f,    45f, -0.5f, 8f, 42f),
        };
        // 起動直後に どこにも 入って いない ときだけ（南の 空から 村ぜんたい＝山頂級の 俯瞰は
        // こういう 意図した 場面に だけ 使う）
        fix.fallback = new MuraCamFixed.Spot {
            name = "ひき", pos = new Vector3(0f, 26f, -78f),
            lookAt = new Vector3(0f, 0f, -20f), fov = 40f,
        };

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
        RenderSettings.ambientLight = new Color(0.52f, 0.56f, 0.60f);
        // 空気感（奥ほど かすむ）。遠くの 山が 溶けて 遠近が 出る
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.74f, 0.78f, 0.74f);
        RenderSettings.fogDensity = 0.010f;   // ★もっと あってもいい（本人）→ 増量

        // ---- とおくの 山なみ（仮）。地平の 空白を 埋める。じっさいは 絵に 差し替える。
        //   霧で とけて シルエットに なる。あたりは つけない（遊び場の そと）
        var mYama  = Mat("MuraYama",  new Color(0.40f, 0.50f, 0.47f));
        var mYama2 = Mat("MuraYama2", new Color(0.52f, 0.62f, 0.58f));
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
