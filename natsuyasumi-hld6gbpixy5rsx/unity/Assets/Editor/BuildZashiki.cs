// ざしき を まるごと コードで 組み立てる（GUIを さわらずに 作れるように）。
// 3Dの 床/壁/障子 ＋ 2Dの キャラ（板）＋ ポストFX。奥行きの ある 見た目の 検証用。
// 使いかた: Unity -batchmode -executeMethod BuildZashiki.Build
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class BuildZashiki {
    const string ArtTex = "Assets/Art/Textures/";
    const string MatDir = "Assets/Art/Materials/";
    const string ScnDir = "Assets/Scenes/";

    // 部屋の 寸法（m）。畳＝おおよそ 1.8 x 0.9
    const float RoomW = 7.2f;    // 横
    const float RoomD = 5.4f;    // 奥ゆき
    const float WallH = 2.6f;    // 天井の 高さ

    [MenuItem("なつやすみ/ざしきを 組み立てる")]
    public static void Build() {
        Directory.CreateDirectory(MatDir);
        Directory.CreateDirectory(ScnDir);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- 素材（マテリアル）
        // 畳は 1まい 1.8m x 0.9m。床は 7.2 x 5.4 なので (4, 6) で ちょうど 畳割りに なる
        var mTatami  = Mat("Tatami",  ArtTex + "tatami.png",        new Vector2(4, 6), 0.02f, 0.92f);
        var mWood    = Mat("Wood",    ArtTex + "wood_beam.jpg",     new Vector2(2, 2), 0.05f, 0.72f);
        var mFloorW  = Mat("WoodFloor",ArtTex + "wood_floor.jpg",   new Vector2(3, 2), 0.06f, 0.60f);
        var mPlaster = Mat("Plaster", ArtTex + "plaster_wall.jpg",  new Vector2(3, 2), 0.0f,  0.95f);
        var mPaper   = Mat("ShojiPaper", ArtTex + "shoji_paper.png",new Vector2(1, 1), 0.0f,  0.88f);
        // 障子紙は 光を すこし とおす（裏から 光が あたると にじむ）
        mPaper.EnableKeyword("_EMISSION");
        mPaper.SetColor("_EmissionColor", new Color(1.00f, 0.94f, 0.80f) * 0.85f);   // 裏から 光が すける
        mPaper.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        var root = new GameObject("Zashiki").transform;

        // --- 床（畳）と 縁がわの 板の間
        var floor = Box("Tatami_Floor", root, new Vector3(0, -0.05f, 0), new Vector3(RoomW, 0.1f, RoomD), mTatami);
        Box("Engawa", root, new Vector3(0, -0.04f, RoomD * 0.5f + 0.45f), new Vector3(RoomW, 0.12f, 0.9f), mFloorW);

        // --- 天井の 梁は 置かない（本人の 判断。視界の じゃまに なる）

        // --- 奥の 壁（土壁）と 左右の 壁
        Box("Wall_Back",  root, new Vector3(0, WallH * 0.5f, -RoomD * 0.5f), new Vector3(RoomW, WallH, 0.12f), mPlaster);
        Box("Wall_Right", root, new Vector3(RoomW * 0.5f, WallH * 0.5f, 0),  new Vector3(0.12f, WallH, RoomD), mPlaster);

        // --- 左は 障子（紙＋格子）。ここから 光が 入る。手前がわは 壁を 置かない（抜き）
        // 奥がわ 6割だけ 障子に して、手前は **あけはなち**。そこから 庭が 見える
        float shojiW = RoomD * 0.6f;
        var shojiPaperRenderer = ShojiWall(root, new Vector3(-RoomW * 0.5f, 0, -RoomD * 0.5f + shojiW * 0.5f), Quaternion.Euler(0, 90, 0),
                  shojiW, WallH, mPaper, mWood);
        // あけはなちの ふちに 柱を 1本（境目が ぼやけない ように）
        Box("Post_Open", root, new Vector3(-RoomW * 0.5f, WallH * 0.5f, -RoomD * 0.5f + shojiW),
            new Vector3(0.13f, WallH, 0.13f), mWood);

        // --- 庭の 書割は やめた。実際に 庭へ 出られる ように なったので、
        // 横から 見ると ただの 板に 見えて じゃまだった
        // --- あんどん（行灯）。あたたかい 点光源
        var andon = Box("Andon", root, new Vector3(RoomW * 0.5f - 0.9f, 0.55f, -RoomD * 0.5f + 0.9f),
                        new Vector3(0.34f, 1.1f, 0.34f), mPaper);
        var lampGO = new GameObject("Andon_Light");
        lampGO.transform.SetParent(andon.transform, false);
        var lamp = lampGO.AddComponent<Light>();
        lamp.type = LightType.Point; lamp.color = new Color(1f, 0.82f, 0.55f);
        lamp.intensity = 3.2f; lamp.range = 6f; lamp.shadows = LightShadows.Soft;

        // --- ちゃぶ台（小物）。接地影が 出ると 立体に 見える
        Box("Table_Top", root, new Vector3(-0.6f, 0.34f, 0.5f), new Vector3(1.3f, 0.07f, 0.9f), mFloorW);
        for (int i = 0; i < 4; i++) {
            float sx = (i % 2 == 0) ? -1 : 1, sz = (i < 2) ? -1 : 1;
            Box("Table_Leg" + i, root, new Vector3(-0.6f + sx * 0.55f, 0.17f, 0.5f + sz * 0.36f),
                new Vector3(0.07f, 0.34f, 0.07f), mWood);
        }

        // --- 太陽。障子ごしの あさの 光（斜めから）
        var sunGO = new GameObject("Sun");
        var sun = sunGO.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.95f, 0.83f);
        sun.intensity = 2.6f;
        sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.66f;
        sunGO.transform.rotation = Quaternion.Euler(34f, 66f, 0f);   // 左（障子）の 上から 差しこむ

        // おき光（フィル）。奥の 壁が 真っ黒に 沈まないよう、手前がわから 弱く あてる。
        // 影は おとさない＝形を こわさない
        var fillGO = new GameObject("Fill");
        var fill = fillGO.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.80f, 0.84f, 0.95f);   // 空からの まわりこみ＝やや つめたい
        fill.intensity = 0.55f;
        fill.shadows = LightShadows.None;
        fillGO.transform.rotation = Quaternion.Euler(22f, 200f, 0f);

        // 室内の 空気は あたたかい 側に。青いと 朝の さむさに なってしまう
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor    = new Color(0.58f, 0.58f, 0.54f);
        RenderSettings.ambientEquatorColor= new Color(0.46f, 0.42f, 0.35f);
        RenderSettings.ambientGroundColor = new Color(0.24f, 0.21f, 0.17f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.72f, 0.70f, 0.62f);
        RenderSettings.fogDensity = 0.022f;   // 奥ほど かすむ＝空気感
        // 既定の 青い 空から 環境光を 拾わないように する。**更新を 明示しないと 反映されない**
        RenderSettings.skybox = null;
        DynamicGI.UpdateEnvironment();

        // --- キャラ（2Dの 板）。ドット絵を そのまま 立てる
        var chars = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sprites/chars.png");
        var player = MakeChar("Cirno",  chars, 2, new Vector3(0.6f, 0, 1.4f), root);   // ci=2 チルノ
        MakeChar("Daiyou", chars, 3, new Vector3(-1.8f, 0, -0.2f), root);              // ci=3 だいようせい
        // あるけるように する。当たりは カプセル、壁や 卓は 箱の あたりで 止まる
        var ccc = player.AddComponent<CharacterController>();
        ccc.height = 1.0f; ccc.radius = 0.26f; ccc.center = new Vector3(0f, 0.52f, 0f);
        ccc.slopeLimit = 50f; ccc.stepOffset = 0.35f;
        var pm = player.AddComponent<PlayerMove>();
        pm.sprite = player.transform.GetChild(0);

        // --- カメラ。ななめ 上から 見おろす（真横だと 2Dに 見える）
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        // 調べた ところ、この 見た目は **見おろし 約30度・画角 約60度** が 目安。
        // 画角を ひろく とると 視点が 平たく なり、それでいて 奥ゆきは のこる
        cam.fieldOfView = 46f;
        cam.nearClipPlane = 0.1f; cam.farClipPlane = 60f;
        // まっ黒だと 抜けて 見える。あたたかい 暗さに して 箱庭らしく
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.055f, 0.045f, 0.040f);
        var camData = camGO.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;
        camData.antialiasing = AntialiasingMode.None;   // ドット絵を にじませない
        camData.volumeLayerMask = ~0;                   // どの層の Volume も 拾う
        // カメラの 位置は CamOrbit が 決める（実行中に さわれるように）。
        // yaw=180 ＝ 部屋の 手前がわから 見おろす
        var orbit = camGO.AddComponent<CamOrbit>();
        orbit.target = new Vector3(0f, 0.85f, 0.2f);
        orbit.pitch = 34f; orbit.yaw = 180f; orbit.distance = 8.6f;   // 部屋が ちょうど 収まる ところ
        orbit.follow = player.transform;                 // あるくと ついてくる
        orbit.followOffset = new Vector3(0f, 0.75f, 0f);

        // --- ポストFX（被写界深度・ブルーム・カラグレ・四すみ落とし）
        var volGO = new GameObject("PostFX");
        var vol = volGO.AddComponent<Volume>();
        vol.isGlobal = true;
        var prof = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(prof, MatDir + "PostFX.asset");

        var dof = AddFX<DepthOfField>(prof);
        dof.mode.overrideState = true; dof.mode.value = DepthOfFieldMode.Bokeh;
        // ミニチュア（ティルトシフト）ふうに：キャラに ピントを おき、奥と 手前を ぼかす。
        // 見おろしの 画では 遠さ＝画面の 上下に なるので、これで 帯状に ぼける
        dof.focusDistance.overrideState = true; dof.focusDistance.value = 8.4f;   // キャラの あたり
        dof.aperture.overrideState = true; dof.aperture.value = 3.6f;    // 小さいほど よく ぼける
        dof.focalLength.overrideState = true; dof.focalLength.value = 60f;

        var bloom = AddFX<Bloom>(prof);
        bloom.threshold.overrideState = true; bloom.threshold.value = 1.05f;
        bloom.intensity.overrideState = true; bloom.intensity.value = 1.15f;
        bloom.scatter.overrideState = true;   bloom.scatter.value = 0.72f;
        bloom.tint.overrideState = true;      bloom.tint.value = new Color(1f, 0.96f, 0.86f);

        var grade = AddFX<ColorAdjustments>(prof);
        grade.postExposure.overrideState = true; grade.postExposure.value = 0.15f;
        grade.contrast.overrideState = true;     grade.contrast.value = 12f;
        grade.saturation.overrideState = true;   grade.saturation.value = 8f;

        var wb = AddFX<WhiteBalance>(prof);
        wb.temperature.overrideState = true; wb.temperature.value = 12f;   // あたたかく

        var vig = AddFX<Vignette>(prof);
        vig.intensity.overrideState = true; vig.intensity.value = 0.34f;
        vig.smoothness.overrideState = true; vig.smoothness.value = 0.5f;

        var tone = AddFX<Tonemapping>(prof);
        tone.mode.overrideState = true; tone.mode.value = TonemappingMode.Neutral;

        vol.sharedProfile = prof;

        // --- 見えない かべ。**歩いて 落ちない ように** 部屋と 庭を かこむ。
        // （たしかめで 前に あるいたら 縁側から 落ちつづけた）
        float wz0 = -RoomD * 0.5f - 0.6f, wz1 = RoomD * 0.5f + 1.1f;
        float wx0 = -RoomW * 0.5f - 6.6f, wx1 = RoomW * 0.5f + 0.4f;
        Invisible("Bound_Front", root, new Vector3((wx0 + wx1) * 0.5f, 1.2f, wz1), new Vector3(wx1 - wx0, 3f, 0.3f));
        Invisible("Bound_Back",  root, new Vector3((wx0 + wx1) * 0.5f, 1.2f, wz0), new Vector3(wx1 - wx0, 3f, 0.3f));
        Invisible("Bound_Left",  root, new Vector3(wx0, 1.2f, (wz0 + wz1) * 0.5f), new Vector3(0.3f, 3f, wz1 - wz0));
        Invisible("Bound_Right", root, new Vector3(wx1, 1.2f, (wz0 + wz1) * 0.5f), new Vector3(0.3f, 3f, wz1 - wz0));

        // --- 時間帯の 光（あさ/ひる/ゆうがた/よる）。ここで まとめて 切りかえる
        var todGO = new GameObject("TimeOfDay");
        var todc = todGO.AddComponent<TimeOfDay>();
        todc.sun = sun; todc.fill = fill; todc.andon = lamp; todc.cam = cam;
        todc.shojiPaper = shojiPaperRenderer;
        todc.tod = TimeOfDay.Tod.Asa;
        todc.Apply();

        // --- 塵（ほこり）。光の なかで きらきら 舞う
        Dust(root);

        // --- 2Dドット絵の 小物（調べた とおり、草木や 小物は 板の ドット絵で 置くのが 作法）
        var props = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sprites/props.png");
        // そとの 地めん（縁の したは 庭）。これが ないと 草が 宙に 浮く
        var mGrass = Mat("GroundGrass", ArtTex + "grass_ground.png", new Vector2(8, 10), 0f, 1f);
        // 屋内は「屋根を はずした 切りぬき」。まわりは 暗い ままで よい（実物も そう）。
        // 庭は あけはなちから 見える ぶんだけ
        Box("Garden_Ground", root, new Vector3(-RoomW * 0.5f - 3.2f, -0.62f, 0.9f),
            new Vector3(7.4f, 0.2f, 12.0f), mGrass);
        // 垣根（低い 板塀）
        for (int i = -5; i <= 5; i++)
            Box("Fence" + i, root, new Vector3(-RoomW * 0.5f - 8.4f, -0.15f, i * 1.2f + 1.0f),
                new Vector3(0.10f, 0.95f, 1.10f), mWood);
        // 垣根の むこうの 木立ち（庭がわだけ）。奥ゆきの ふた
        for (int i = 0; i < 7; i++)
            Prop("Ki" + i, props, PROP_SHIGE,
                 new Vector3(-RoomW * 0.5f - 7.4f - (i % 2) * 0.9f, -0.52f, -3.4f + i * 1.7f),
                 2.6f + (i % 3) * 0.6f, root, PropKind.Crossed);
        // 草むらと しげみ＝十字に 組んだ 板（どの 向きからでも 立体に 見える）。
        // あけはなちの すぐ そとに 寄せて、部屋から 見えるように する
        float gx = -RoomW * 0.5f;
        Prop("Kusa1",  props, PROP_KUSA,  new Vector3(gx - 0.55f, -0.52f, 1.30f), 1.05f, root, PropKind.Crossed);
        Prop("Kusa2",  props, PROP_KUSA,  new Vector3(gx - 1.35f, -0.52f, 2.20f), 0.90f, root, PropKind.Crossed);
        Prop("Kusa3",  props, PROP_KUSA,  new Vector3(gx - 0.75f, -0.52f, 3.10f), 1.00f, root, PropKind.Crossed);
        Prop("Kusa4",  props, PROP_KUSA,  new Vector3(gx - 2.10f, -0.52f, 1.05f), 0.85f, root, PropKind.Crossed);
        Prop("Shige1", props, PROP_SHIGE, new Vector3(gx - 1.60f, -0.52f, 0.35f), 1.60f, root, PropKind.Crossed);
        Prop("Shige2", props, PROP_SHIGE, new Vector3(gx - 2.40f, -0.52f, 2.90f), 1.35f, root, PropKind.Crossed);
        // 部屋の なかの 小物
        Prop("Zabuton1", props, PROP_ZABU, new Vector3(-1.45f, 0.012f, 0.55f), 0.85f, root, PropKind.Flat);
        Prop("Zabuton2", props, PROP_ZABU, new Vector3( 0.35f, 0.012f, 1.55f), 0.85f, root, PropKind.Flat);
        Prop("Uchiwa",   props, PROP_UCHI, new Vector3( 1.55f, 0.014f, 1.15f), 0.55f, root, PropKind.Flat);
        Prop("Senko",    props, PROP_SENKO,new Vector3( 1.95f, 0.0f,   2.10f), 0.40f, root, PropKind.Billboard);
        Prop("Kabin",    props, PROP_KABIN,new Vector3(-2.45f, 0.0f,  -1.85f), 0.80f, root, PropKind.Billboard);

        EditorSceneManager.SaveScene(scene, ScnDir + "Zashiki.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("[BuildZashiki] done: " + ScnDir + "Zashiki.unity");
    }

    // 小物の 絵の ならび（props.png は 32px を 6こ 横に）
    const int PROP_KUSA = 0, PROP_SHIGE = 1, PROP_ZABU = 2, PROP_KABIN = 3, PROP_SENKO = 4, PROP_UCHI = 5;
    enum PropKind { Billboard, Crossed, Flat }

    // --- 2Dドット絵の 小物を 置く。
    // Crossed＝十字に 2枚。草木は これで どの 向きからも 立体に 見え、影も それらしく 出る
    // Flat＝ゆかに 寝かせる（ざぶとん・うちわ）。Billboard＝いつも こちらを 向く
    static void Prop(string name, Texture2D atlas, int index, Vector3 pos, float height,
                     Transform root, PropKind kind) {
        if (atlas == null) return;
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = pos;

        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetFloat("_Surface", 0);
        m.SetFloat("_AlphaClip", 1);
        m.SetFloat("_Cutoff", 0.5f);
        m.EnableKeyword("_ALPHATEST_ON");
        m.SetFloat("_Smoothness", 0f);
        // **両面に えがく。** 板は 片面しか えがかれないので、裏を 向くと
        // 影だけ 出て 本体が 消える（実際 草木が 影だけに なった）。草木は 両面が ふつう
        m.SetFloat("_Cull", 0f);
        m.doubleSidedGI = true;
        m.SetTexture("_BaseMap", atlas);
        m.SetTextureScale("_BaseMap", new Vector2(1f / 6f, 1f));
        m.SetTextureOffset("_BaseMap", new Vector2(index / 6f, 0f));
        m.mainTexture = atlas;
        m.mainTextureScale = new Vector2(1f / 6f, 1f);
        m.mainTextureOffset = new Vector2(index / 6f, 0f);
        AssetDatabase.CreateAsset(m, MatDir + "Prop_" + name + ".mat");

        int sheets = kind == PropKind.Crossed ? 2 : 1;
        for (int i = 0; i < sheets; i++) {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Sheet" + i;
            q.transform.SetParent(go.transform, false);
            Object.DestroyImmediate(q.GetComponent<Collider>());
            if (kind == PropKind.Flat) {
                q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // ゆかに 寝かせる
                q.transform.localPosition = Vector3.zero;
            } else {
                q.transform.localRotation = Quaternion.Euler(0f, i * 90f, 0f);
                q.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            }
            q.transform.localScale = new Vector3(height, height, 1f);
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = kind == PropKind.Flat
                ? UnityEngine.Rendering.ShadowCastingMode.Off      // 寝かせた 板は 影を おとさない
                : UnityEngine.Rendering.ShadowCastingMode.On;
        }
        if (kind == PropKind.Billboard) go.AddComponent<Billboard>();
    }

    // --- 障子の 壁。紙の 面＋格子（細い 角材）。格子が 影を おとす
    static Renderer ShojiWall(Transform root, Vector3 pos, Quaternion rot, float width, float height, Material paper, Material wood) {
        var go = new GameObject("Shoji");
        go.transform.SetParent(root, false);
        go.transform.SetPositionAndRotation(pos, rot);
        var p = Quad("Paper", go.transform, new Vector3(0, height * 0.5f, 0), Quaternion.identity, new Vector3(width, height, 1));
        var pr = p.GetComponent<Renderer>();
        pr.sharedMaterial = paper;
        // **紙は 影を おとさない。** 障子は 光を とおすので、ここで さえぎると 部屋が まっ暗に なる。
        // 桟（木）だけが 影を おとす＝畳に 格子の 影が のびる。これが 見どころ
        pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        int cols = Mathf.RoundToInt(width / 0.30f), rows = Mathf.RoundToInt(height / 0.34f);
        for (int i = 0; i <= cols; i++)      // たての 桟
            Box("V" + i, go.transform, new Vector3(-width * 0.5f + i * (width / cols), height * 0.5f, 0.03f),
                new Vector3(0.035f, height, 0.045f), wood);
        for (int j = 0; j <= rows; j++)      // よこの 桟
            Box("H" + j, go.transform, new Vector3(0, j * (height / rows), 0.03f),
                new Vector3(width, 0.035f, 0.045f), wood);
        Box("Frame_L", go.transform, new Vector3(-width * 0.5f, height * 0.5f, 0.03f), new Vector3(0.10f, height, 0.09f), wood);
        Box("Frame_R", go.transform, new Vector3( width * 0.5f, height * 0.5f, 0.03f), new Vector3(0.10f, height, 0.09f), wood);
        Box("Frame_T", go.transform, new Vector3(0, height, 0.03f), new Vector3(width, 0.12f, 0.09f), wood);
        return pr;
    }

    // --- ドット絵を 板に して 立てる（ビルボード）。足もとに 影を おとす
    static GameObject MakeChar(string name, Texture2D sheet, int index, Vector3 pos, Transform root) {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = pos;

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Sprite";
        quad.transform.SetParent(go.transform, false);
        quad.transform.localPosition = new Vector3(0, 0.525f, 0);
        quad.transform.localScale = new Vector3(1.05f, 1.05f, 1f);   // 実物に あわせる（子どもの 背たけ）
        Object.DestroyImmediate(quad.GetComponent<Collider>());

        // ドット絵用：切りぬき＋点フィルタ（にじませない）
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetFloat("_Surface", 0);
        m.SetFloat("_AlphaClip", 1);
        m.SetFloat("_Cutoff", 0.5f);
        m.EnableKeyword("_ALPHATEST_ON");
        m.SetFloat("_Smoothness", 0f);
        m.mainTexture = sheet;
        // 絵は 8列 x 4行、1こま 16x16。index = 行*8 + 列。
        // 画像は 上が 0行めだが、UVは 下が 0。なので y は ひっくり返して 数える
        const int Cols = 8, Rows = 4;
        int col = index % Cols, row = index / Cols;
        m.mainTextureScale  = new Vector2(1f / Cols, 1f / Rows);
        m.mainTextureOffset = new Vector2(col / (float)Cols, (Rows - 1 - row) / (float)Rows);
        AssetDatabase.CreateAsset(m, MatDir + "Char_" + name + ".mat");
        quad.GetComponent<Renderer>().sharedMaterial = m;
        var mr = quad.GetComponent<Renderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;   // 板でも 影は 出す
        go.AddComponent<Billboard>();
        return go;
    }

    // --- 舞う 塵
    static void Dust(Transform root) {
        var go = new GameObject("Dust");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(0, 1.4f, 0.5f);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 14f; main.startSpeed = 0.045f;
        main.startSize = 0.022f; main.maxParticles = 220;
        main.startColor = new Color(1f, 0.97f, 0.86f, 0.55f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.004f;
        var em = ps.emission; em.rateOverTime = 16f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(RoomW, 2.2f, RoomD);
        var noise = ps.noise; noise.enabled = true; noise.strength = 0.09f; noise.frequency = 0.28f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f),
                             new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);
        var r = go.GetComponent<ParticleSystemRenderer>();
        // シェーダ名は 版で かわる。見つからないと まっピンクに なるので 順に さがす
        var dustSh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                  ?? Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Sprites/Default");
        var pm = new Material(dustSh);
        pm.SetFloat("_Surface", 1);                       // 透ける
        pm.SetFloat("_Blend", 1);                         // 加算ぎみ
        pm.SetColor("_BaseColor", new Color(1f, 0.96f, 0.85f, 1f));
        AssetDatabase.CreateAsset(pm, MatDir + "Dust.mat");
        r.sharedMaterial = pm;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Debug.Log("[BuildZashiki] dust shader = " + (dustSh != null ? dustSh.name : "NULL"));
    }


    // Volumeの 効果は **サブアセットとして 保存**しないと、ビルドで 消えてしまう
    // （被写界深度も ヴィネットも 効かなかった 原因）
    static T AddFX<T>(VolumeProfile prof) where T : VolumeComponent {
        var c = prof.Add<T>(true);
        c.hideFlags = HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(c, prof);
        return c;
    }

    // ---- 小道具
    static Material Mat(string name, string texPath, Vector2 tiling, float metal, float rough) {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (t == null) Debug.LogError("[BuildZashiki] テクスチャが 見つからない: " + texPath);
        else {
            // _BaseMap を **名前で** 入れる。mainTexture ばかりだと 版により 効かないことがある
            m.SetTexture("_BaseMap", t);
            m.SetTextureScale("_BaseMap", tiling);
            m.mainTexture = t;
            m.mainTextureScale = tiling;
        }
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Metallic", metal);
        m.SetFloat("_Smoothness", 1f - rough);
        AssetDatabase.CreateAsset(m, MatDir + name + ".mat");
        return m;
    }

    // 見えない かべ（当たりだけ ある 箱）
    static void Invisible(string name, Transform parent, Vector3 pos, Vector3 size) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name; go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localScale = size;
        Object.DestroyImmediate(go.GetComponent<MeshRenderer>());   // 絵は 出さない。当たりだけ のこす
    }

    static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material m) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name; go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = m;
        return go;
    }

    static GameObject Quad(string name, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name; go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localRotation = rot; go.transform.localScale = scale;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
}
