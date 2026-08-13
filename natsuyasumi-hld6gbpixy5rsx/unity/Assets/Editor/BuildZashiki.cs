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

    // ★2026-08-14：いちど 消した 家を 建てなおした。
    // 写真から 起こした テクスチャを やめ、**木立ちの 絵から 吸いだした 色だけで
    // ドット絵の テクスチャを 描き起こした**（unity/ArtSource/make_textures.py）。
    // 本家も 建物は 3D で、2D なのは キャラと 小物だけ。直すべきは「3Dをやめる」ことでは
    // なく「貼る 絵を そろえる」ことだった
    const bool ShowRoom = true;

    // 部屋の 寸法（m）。畳＝おおよそ 1.8 x 0.9
    const float RoomW = 7.2f;    // 横
    const float RoomD = 5.4f;    // 奥ゆき
    const float WallH = 2.6f;    // 天井の 高さ

    // 庭（家の 左がわ）。地めんの 高さは -0.52m
    const float GardenX = -RoomW * 0.5f - 5.6f;   // まんなか
    const float GardenW = 12.4f;                  // 横 [-15.4, -3.0] ぐらい
    const float GardenZ = 0.9f, GardenD = 13.5f;  // 奥ゆき
    const float GroundY = -0.52f;                 // 草木を 置く 高さ

    [MenuItem("なつやすみ/ざしきを 組み立てる")]
    public static void Build() {
        Directory.CreateDirectory(MatDir);
        Directory.CreateDirectory(ScnDir);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- 素材（マテリアル）。テクスチャは すべて **32px＝1m** で 描いてある。
        // 敷きつめる 数は「面の 大きさ(m)」を そのまま わたせば 尺が 合う
        // 畳は 1まい 1.8m x 0.9m。床は 7.2 x 5.4 なので (4, 6) で ちょうど 畳割りに なる
        var mTatami  = Mat("Tatami",  ArtTex + "tatami.png",        new Vector2(4, 6), 0.0f, 0.94f);
        var mWood    = Mat("Wood",    ArtTex + "wood_beam.png",     new Vector2(1, 2), 0.0f, 0.80f);
        var mFloorW  = Mat("WoodFloor",ArtTex + "wood_floor.png",   new Vector2(RoomW, 0.9f), 0.0f, 0.72f);
        var mPlaster = Mat("Plaster", ArtTex + "plaster_wall.png",  new Vector2(RoomW, WallH), 0.0f, 0.96f);
        var mRoof    = Mat("RoofTile",ArtTex + "roof_tile.png",     new Vector2(RoomW, 1.6f), 0.0f, 0.86f);
        // 沓ぬぎ石。かわらと 同じ 灰の 絵を 大きめに 敷いて 石はだに 見せる
        var mStone   = Mat("Stone",   ArtTex + "roof_tile.png",     new Vector2(0.55f, 0.35f), 0.0f, 0.92f);
        mStone.SetColor("_BaseColor", new Color(0.82f, 0.80f, 0.76f));
        var mPaper   = Mat("ShojiPaper", ArtTex + "shoji_paper.png",new Vector2(2, 3), 0.0f,  0.90f);
        // 障子紙は **両面に えがく。** 板は 片面しか えがかれないので、
        // カメラを ななめに 回したら 紙が 消えて 桟だけの 柵に 見えた
        mPaper.SetFloat("_Cull", 0f);
        mPaper.doubleSidedGI = true;
        // 障子紙は 光を すこし とおす（裏から 光が あたると にじむ）
        mPaper.EnableKeyword("_EMISSION");
        mPaper.SetColor("_EmissionColor", new Color(1.00f, 0.94f, 0.80f) * 0.85f);   // 裏から 光が すける
        mPaper.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        var root = new GameObject("Zashiki").transform;

        Renderer shojiPaperRenderer = null;
        Light lamp = null;

        if (ShowRoom) {
            // --- 床（畳）と 縁がわの 板の間
            Box("Tatami_Floor", root, new Vector3(0, -0.05f, 0), new Vector3(RoomW, 0.1f, RoomD), mTatami);
            Box("Engawa", root, new Vector3(0, -0.04f, RoomD * 0.5f + 0.45f), new Vector3(RoomW, 0.12f, 0.9f), mFloorW);

            // --- ゆか下。**家は 地めんより 高い。** これが 無いと 畳が 宙に 浮いて 見える
            //（野原の 高さは GroundY。そこから 床板の 下(-0.10)まで を 木で ふさぐ）
            // ※ 上面を 床と ぴったり 同じ 高さに すると 面が かさなって ちらつき、
            //   畳が 板の間に 化けた。**0.10m 下で 止める**
            Box("Under_Floor", root, new Vector3(0, (GroundY - 0.10f) * 0.5f, 0),
                new Vector3(RoomW + 0.14f, -GroundY - 0.10f, RoomD + 0.9f), mWood);

            // --- 沓ぬぎ石。**家は 地めんより 0.5m 高いので、これが 無いと 出たきり もどれない。**
            // 当たり判定の またげる 高さ(0.35m)を こえない ように 2段に する
            Box("Fumiishi_Lo", root, new Vector3(1.2f, GroundY + 0.13f, RoomD * 0.5f + 1.35f),
                new Vector3(1.1f, 0.26f, 0.7f), mStone);
            Box("Fumiishi_Hi", root, new Vector3(1.2f, GroundY + 0.26f, RoomD * 0.5f + 0.98f),
                new Vector3(1.1f, 0.52f, 0.5f), mStone);

            // --- 天井の 梁は 置かない（本人の 判断。視界の じゃまに なる）

            // --- 壁。**カメラの がわの 壁は 建てない。**
            // 見おろしの 屋内は「手前の 壁と 屋根を はずした 切りぬき」で 見せるのが 作法。
            // カメラは 右手前(yaw 212)から 覗くので、奥(-Z)と 左(-X)だけ 残す
            Box("Wall_Back",  root, new Vector3(0, WallH * 0.5f, -RoomD * 0.5f), new Vector3(RoomW, WallH, 0.12f), mPlaster);

            // --- 左は 障子（紙＋格子）。ここから 光が 入る。手前がわは 壁を 置かない（抜き）
            // 奥がわ 6割だけ 障子に して、手前は **あけはなち**。そこから 庭が 見える
            float shojiW = RoomD * 0.6f;
            shojiPaperRenderer = ShojiWall(root, new Vector3(-RoomW * 0.5f, 0, -RoomD * 0.5f + shojiW * 0.5f), Quaternion.Euler(0, 90, 0),
                      shojiW, WallH, mPaper, mWood);
            // あけはなちの ふちに 柱を 1本（境目が ぼやけない ように）
            Box("Post_Open", root, new Vector3(-RoomW * 0.5f, WallH * 0.5f, -RoomD * 0.5f + shojiW),
                new Vector3(0.13f, WallH, 0.13f), mWood);

            // --- あんどん（行灯）。あたたかい 点光源
            var andon = Box("Andon", root, new Vector3(RoomW * 0.5f - 0.9f, 0.55f, -RoomD * 0.5f + 0.9f),
                            new Vector3(0.34f, 1.1f, 0.34f), mPaper);
            var lampGO = new GameObject("Andon_Light");
            lampGO.transform.SetParent(andon.transform, false);
            lamp = lampGO.AddComponent<Light>();
            lamp.type = LightType.Point; lamp.color = new Color(1f, 0.82f, 0.55f);
            lamp.intensity = 3.2f; lamp.range = 6f; lamp.shadows = LightShadows.Soft;

            // --- 屋根。**奥の 半分だけ 残して 手前は 切りとる。**
            // これが 無いと ただの 舞台に 見える。手前まで ふくと 中が 見えなく なるので、
            // 「屋根を 途中で 切って 中を のぞかせている」形に する（見おろしの 屋内の 作法）
            const float RoofFrontZ = 0.2f;                        // ここから 手前は 切りとり
            const float RoofBackZ = -RoomD * 0.5f - 1.5f;         // 軒先（壁より 外に 出す）
            const float RoofTilt = 12f;                           // 軒先へ 下がる 角度
            float roofLen = RoofFrontZ - RoofBackZ;
            float roofY = 2.95f;
            var roofRot = Quaternion.Euler(-RoofTilt, 0f, 0f);
            var roofAt = new Vector3(0, roofY, (RoofFrontZ + RoofBackZ) * 0.5f);

            // かわら。**厚みを もたせる**。うすいと ただの 板が 浮いて 見える
            var roof = Box("Roof", root, roofAt, new Vector3(RoomW + 1.6f, 0.34f, roofLen), mRoof);
            roof.transform.localRotation = roofRot;
            // 軒裏（したから 見あげる 面）。かわらの 裏が 見えると 妙なので 木で ふさぐ
            var soffit = Box("Roof_Soffit", root, roofAt + roofRot * new Vector3(0, -0.22f, 0),
                             new Vector3(RoomW + 1.45f, 0.12f, roofLen - 0.15f), mWood);
            soffit.transform.localRotation = roofRot;
            // 切り口の 化粧板（切りとった ところが 断面に 見える ように）
            var fascia = Box("Roof_Cut", root, roofAt + roofRot * new Vector3(0, 0f, roofLen * 0.5f),
                             new Vector3(RoomW + 1.6f, 0.40f, 0.10f), mWood);
            fascia.transform.localRotation = roofRot;
            // 屋根を ささえる 柱（切り口の 2本）
            float postY = roofY + roofLen * 0.5f * Mathf.Sin(RoofTilt * Mathf.Deg2Rad);
            for (int i = -1; i <= 1; i += 2)
                Box("Roof_Post" + i, root, new Vector3(i * (RoomW * 0.5f - 0.18f), postY * 0.5f, RoofFrontZ),
                    new Vector3(0.15f, postY, 0.15f), mWood);

            // --- ちゃぶ台（小物）。接地影が 出ると 立体に 見える
            Box("Table_Top", root, new Vector3(-0.6f, 0.34f, 0.5f), new Vector3(1.3f, 0.07f, 0.9f), mFloorW);
            for (int i = 0; i < 4; i++) {
                float sx = (i % 2 == 0) ? -1 : 1, sz = (i < 2) ? -1 : 1;
                Box("Table_Leg" + i, root, new Vector3(-0.6f + sx * 0.55f, 0.17f, 0.5f + sz * 0.36f),
                    new Vector3(0.07f, 0.34f, 0.07f), mWood);
            }
        }

        // --- 太陽。**手前がわ(カメラ寄り)の 上から** 差しこむ。
        // 前は 真横(yaw=66)から あてていたので 影が 横に のびて、板の 草木が
        // 「立てた 板」だと ばれていた。手前から あてると 影は **奥へ** 落ちる＝
        // 見おろしの 画では 影が 物の 下に 隠れる ように のびて、板でも 立体に 見える。
        // yaw を 90〜180 に とると 「手前・左」から に なり、障子(左の 壁)も まだ 光を とおす
        var sunGO = new GameObject("Sun");
        var sun = sunGO.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.95f, 0.83f);
        sun.intensity = 2.6f;
        sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.66f;
        sunGO.transform.rotation = Quaternion.Euler(38f, 150f, 0f);

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

        // --- キャラ（2Dの 板）。ドット絵を そのまま 立てる。
        // 家を 出さない ときは 庭に 立たせる（畳の 上に 置くと 宙に 浮く）
        var chars = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sprites/chars_tall.png");
        Vector3 p1 = ShowRoom ? new Vector3(0.6f, 0f, 1.4f)  : new Vector3(GardenX + 1.6f, -0.5f, 1.4f);
        Vector3 p2 = ShowRoom ? new Vector3(-1.8f, 0f, -0.2f) : new Vector3(GardenX - 0.6f, -0.5f, -0.4f);
        var player = MakeChar("Cirno",  chars, CI_CIRNO, p1, root);
        MakeChar("Daiyou", chars, CI_DAIYOU, p2, root);
        // あるけるように する。当たりは カプセル、壁や 卓は 箱の あたりで 止まる
        var ccc = player.AddComponent<CharacterController>();
        ccc.height = 1.0f; ccc.radius = 0.26f; ccc.center = new Vector3(0f, 0.52f, 0f);
        ccc.slopeLimit = 50f; ccc.stepOffset = 0.35f;
        var pm = player.AddComponent<PlayerMove>();
        pm.sprite = player.transform.GetChild(0);
        // むしとり。あみを ふる のは この人
        player.AddComponent<BugBook>();
        player.AddComponent<BugCatcher>();

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
        //
        // ★2026-08-14：**真正面(yaw=180)を やめて ななめから 見る。**
        //   板の キャラは いつも カメラを 向くので、真正面だと ずっと 目が 合って
        //   「見られている」感じに なる。ななめから 覗く 位置に すると、
        //   その場に 居あわせた ように 見える。
        // ★見おろしも 浅くした（34度 → 24度）。**キャラの 目の 高さに 近いほど 入りこめる。**
        //   ただし 一人称に は しない＝地面の 広がりが 見える ぶんは 残す
        var orbit = camGO.AddComponent<CamOrbit>();
        orbit.target = new Vector3(0f, 0.85f, 0.2f);
        orbit.pitch = 24f; orbit.yaw = 212f; orbit.distance = 8.0f;
        orbit.follow = player.transform;                 // あるくと ついてくる
        orbit.followOffset = new Vector3(0f, 0.70f, 0f);

        // --- ポストFX（被写界深度・ブルーム・カラグレ・四すみ落とし）
        var volGO = new GameObject("PostFX");
        var vol = volGO.AddComponent<Volume>();
        vol.isGlobal = true;
        var prof = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(prof, MatDir + "PostFX.asset");

        var dof = AddFX<DepthOfField>(prof);
        dof.mode.overrideState = true; dof.mode.value = DepthOfFieldMode.Bokeh;
        // **ミニチュアを 覗いている ように 見せる。**
        // ピントの 合う 帯を せまく して、その 手前と 奥を とかす。
        // ピントの 距離は FocusOnPlayer が 毎フレーム 主人公に あわせる
        dof.focusDistance.overrideState = true; dof.focusDistance.value = 8.0f;
        dof.aperture.overrideState = true; dof.aperture.value = 3.0f;    // 小さいほど よく ぼける
        dof.focalLength.overrideState = true; dof.focalLength.value = 62f;
        dof.bladeCount.overrideState = true; dof.bladeCount.value = 6;

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

        // **四すみを しっかり 落とす。** これが この 見た目の 半分を 作っている。
        // 画の まわりを 暗く すると、まん中の ひと組だけが 照らされて 見え、
        // 「箱庭を 覗きこんでいる」感じに なる
        var vig = AddFX<Vignette>(prof);
        vig.intensity.overrideState = true; vig.intensity.value = 0.42f;
        vig.smoothness.overrideState = true; vig.smoothness.value = 0.46f;
        vig.rounded.overrideState = true;   vig.rounded.value = false;   // 画面の 比に そって 楕円に

        // 粒子を すこし のせる。ドット絵の 面が つるっと しすぎない ように
        var grain = AddFX<FilmGrain>(prof);
        grain.type.overrideState = true; grain.type.value = FilmGrainLookup.Thin1;
        grain.intensity.overrideState = true; grain.intensity.value = 0.18f;
        grain.response.overrideState = true; grain.response.value = 0.85f;

        var tone = AddFX<Tonemapping>(prof);
        tone.mode.overrideState = true; tone.mode.value = TonemappingMode.Neutral;

        vol.sharedProfile = prof;

        // ピントを 主人公に 追わせる
        var focus = volGO.AddComponent<FocusOnPlayer>();
        focus.volume = vol; focus.target = player.transform;

        // --- 見えない かべ。**歩いて 落ちない ように** かこむ。
        // （たしかめで 前に あるいたら 縁側から 落ちつづけた）
        // 家を 出さない ときは 庭の へりで 止める
        // まわりの 木立ちの 内がわ まで あるける
        const float Play = 11.5f;
        float wz0 = GardenZ - Play, wz1 = GardenZ + Play;
        float wx0 = GardenX - Play, wx1 = GardenX + Play;
        Invisible("Bound_Front", root, new Vector3((wx0 + wx1) * 0.5f, 1.2f, wz1), new Vector3(wx1 - wx0, 3f, 0.3f));
        Invisible("Bound_Back",  root, new Vector3((wx0 + wx1) * 0.5f, 1.2f, wz0), new Vector3(wx1 - wx0, 3f, 0.3f));
        Invisible("Bound_Left",  root, new Vector3(wx0, 1.2f, (wz0 + wz1) * 0.5f), new Vector3(0.3f, 3f, wz1 - wz0));
        Invisible("Bound_Right", root, new Vector3(wx1, 1.2f, (wz0 + wz1) * 0.5f), new Vector3(0.3f, 3f, wz1 - wz0));

        // --- 天気（はれ/くもり/あめ/ゆうだち）。雨は **庭の うえだけ** に 降らせる。
        // 屋根を はずした 切りぬきなので、部屋の 中まで 降らせると 雨もりに 見える
        var wxGO = new GameObject("Weather");
        var wx = wxGO.AddComponent<Weather>();
        // 降らせる 範囲。**部屋の 上には かけない。**
        // 屋根を 切りとった 見せかたなので、部屋に 降らせると 雨もりに 見える。
        // カメラは 右手前から 覗くので、見えているのは 家の むこうの 庭がわ＝そこに 降らせれば 足りる
        wx.rain = Rain(root, new Vector3(GardenX - 1.5f, 7.0f, GardenZ), new Vector3(14f, 0.5f, 20f));
        // ※ もやの つぶは **やめた。** 板が 大きいので、どんな 重ねかたに しても
        //   四角い ふちが 見えて 湯気の かたまりに なった。もやは RenderSettings の 霧で 足りる
        wx.mist = null;

        // --- 時間帯の 光（あさ/ひる/ゆうがた/よる）。ここで まとめて 切りかえる
        var todGO = new GameObject("TimeOfDay");
        var todc = todGO.AddComponent<TimeOfDay>();
        todc.sun = sun; todc.fill = fill; todc.andon = lamp; todc.cam = cam;
        todc.shojiPaper = shojiPaperRenderer;
        // 家は あるが 場面は **野原の まんなか**。空の 色を 出さないと 地平線から さきが まっ黒に なる
        todc.outdoor = true;
        todc.weather = wx;                    // 時間帯を おいた **あと**に 天気を かぶせる
        wx.timeOfDay = todc;
        todc.tod = TimeOfDay.Tod.Asa;
        todc.Apply();

        // --- 塵（ほこり）。障子ごしの 光の すじの なかで きらきら 舞う＝屋内の 見どころ。
        // 外では 意味が ないので 家を 出す ときだけ
        if (ShowRoom) Dust(root);

        // --- 2Dドット絵の 小物（調べた とおり、草木や 小物は 板の ドット絵で 置くのが 作法）
        var props = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sprites/props.png");
        // そとの 地めん（縁の したは 庭）。これが ないと 草が 宙に 浮く
        var mGrass = Mat("GroundGrass", ArtTex + "grass_ground.png", new Vector2(8, 10), 0f, 1f);
        // 地めん。**画面の そとまで 広げる。**
        // 前は 庭のぶんしか なくて、カメラを 低くしたら 板の 切れめと 側面が 見えて
        // 「浮いている 板」に なった。遠くは 霧で 空に とけるので、広げれば ふちは 見えない
        const float FieldW = 64f, FieldD = 64f;
        // 草の 絵は 48px＝1.5m ぶんで 1まい。広げても 目が 詰まらない ように 数を そろえる
        var tileGrass = new Vector2(FieldW / 1.5f, FieldD / 1.5f);
        mGrass.SetTextureScale("_BaseMap", tileGrass);
        mGrass.mainTextureScale = tileGrass;
        Box("Ground", root, new Vector3(GardenX, GroundY - 0.10f, GardenZ),
            new Vector3(FieldW, 0.2f, FieldD), mGrass);
        // ※ 垣根（木の 板塀）は **やめた。** 3Dの 箱に 木目の テクスチャを 貼った ものなので、
        //   ドット絵の 木立ちと ならぶと 材質が ちぐはぐ だった。
        //   仕切りが 要るなら、木と 同じ 出どころの ドット絵で 置きなおす
        float fenceX = GardenX - GardenW * 0.5f + 3.6f;   // 木立ちの ならぶ 線（垣根の あった ところ）
        // 草木は **自分で 描くのを やめて**、ansimuz(CC0)の「Trees & Bushes」に 差し替えた。
        // 32px＝1m で 詰めなおして あるので、コマの 大きさ(4.5m)を そのまま わたせば 尺が 合う
        // （大きい 木＝4.1m、しげみ＝1.2m、小草＝0.5m ぐらいに なる）
        var nature = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sprites/nature.png");
        if (nature == null) Debug.LogError("[BuildZashiki] nature.png が 見つからない");

        // 垣根の むこうの 木立ち（庭がわだけ）。奥ゆきの ふた。
        // 一直線に ならべると 書割に 見えるので、奥ゆきを 少しずつ ずらす
        float[] kiZ = { -4.2f, -2.1f, -0.4f, 1.5f, 3.2f, 4.6f, 6.1f };
        float[] kiD = { 0.0f, 1.3f, 0.4f, 1.7f, 0.2f, 1.1f, 0.6f };
        for (int i = 0; i < kiZ.Length; i++)
            Nature("Ki" + i, nature, (i % 2 == 0) ? NA_KI_L : NA_KI_R,
                   new Vector3(fenceX - 1.2f - kiD[i], GroundY, kiZ[i]), root);
        // 草むらと しげみ＝**1枚の 板**。家の あった あたりから 庭に かけて 散らす
        float gx = -RoomW * 0.5f;
        Nature("Kusa1",  nature, NA_KUSA_A, new Vector3(gx - 0.55f, GroundY, 1.30f), root);
        Nature("Kusa2",  nature, NA_KUSA_B, new Vector3(gx - 1.35f, GroundY, 2.20f), root);
        Nature("Kusa3",  nature, NA_KUSA_C, new Vector3(gx - 0.75f, GroundY, 3.10f), root);
        Nature("Kusa4",  nature, NA_KUSA_A, new Vector3(gx - 2.10f, GroundY, 1.05f), root);
        Nature("Kusa5",  nature, NA_KUSA_B, new Vector3(gx - 4.30f, GroundY, -2.60f), root);
        Nature("Kusa6",  nature, NA_KUSA_C, new Vector3(gx - 6.10f, GroundY, 4.90f), root);
        Nature("Shige1", nature, NA_SHIGE_A,new Vector3(gx - 1.60f, GroundY, 0.35f), root);
        Nature("Shige2", nature, NA_SHIGE_B,new Vector3(gx - 2.40f, GroundY, 2.90f), root);
        Nature("Shige3", nature, NA_MATSU,  new Vector3(gx - 3.30f, GroundY, 4.10f), root);
        Nature("Shige4", nature, NA_SHIGE_B,new Vector3(gx - 5.20f, GroundY, -1.20f), root);
        Nature("Shige5", nature, NA_SHIGE_A,new Vector3(gx - 7.00f, GroundY, 2.10f), root);

        // --- まわりの 木立ち。**地平線を 木で ふさぐ。**
        // 地めんを 広げただけだと 遠くが のっぺりした 緑の 板に なるので、
        // 遊べる ところの まわりに 帯状に 木を 置いて 囲む。
        // 置き場所は 決めうちの 式で ばらす（毎回 同じ 絵に なるように）
        Woods(root, nature);

        // 部屋の なかの 小物（家を 出す ときだけ）
        if (ShowRoom) {
            Prop("Zabuton1", props, PROP_ZABU, new Vector3(-1.45f, 0.012f, 0.55f), 0.85f, root, PropKind.Flat);
            Prop("Zabuton2", props, PROP_ZABU, new Vector3( 0.35f, 0.012f, 1.55f), 0.85f, root, PropKind.Flat);
            Prop("Uchiwa",   props, PROP_UCHI, new Vector3( 1.55f, 0.014f, 1.15f), 0.55f, root, PropKind.Flat);
            Prop("Senko",    props, PROP_SENKO,new Vector3( 1.95f, 0.0f,   2.10f), 0.40f, root, PropKind.Still);
            Prop("Kabin",    props, PROP_KABIN,new Vector3(-2.45f, 0.0f,  -1.85f), 0.80f, root, PropKind.Still);
        }

        // --- むしとり。虫を 湧かせる 係。
        // 湧く 場所は 場面から 自分で さがす ので、ここでは 絵を わたすだけで よい
        var bugAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sprites/bugs.png");
        if (bugAtlas == null) Debug.LogError("[BuildZashiki] bugs.png が 見つからない");
        var bugsGO = new GameObject("Bugs");
        var spawner = bugsGO.AddComponent<BugSpawner>();
        spawner.atlas = bugAtlas;

        // --- 虫かご。縁がわの はしに 置く。**取った 虫が ここに たまって いくのが 見える**
        MakeBugCage(root, bugAtlas, new Vector3(-2.3f, 0.06f, RoomD * 0.5f + 0.45f), mWood);

        // --- 画面の 文字（数え・ひとこと・ずかん）
        var hudGO = new GameObject("HudRoot");
        var hud = hudGO.AddComponent<BugHud>();
        hud.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/PixelMplus12-Regular.ttf");
        hud.panel = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/panel.png");
        if (hud.font == null) Debug.LogError("[BuildZashiki] 書体が 見つからない");
        if (hud.panel == null) Debug.LogError("[BuildZashiki] panel.png が 見つからない");

        EditorSceneManager.SaveScene(scene, ScnDir + "Zashiki.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("[BuildZashiki] done: " + ScnDir + "Zashiki.unity");
    }

    // キャラの 絵：chars_tall.png は **1コマ 48x64 の 10列 x 3行**（index = 行*10 + 列）。
    // まえの 16x16 は 等身が ひくかったので、本人が 用意した 背の たかい 絵に 差し替えた
    const int CharCols = 10, CharRows = 3;
    const float CharCellW = 48f, CharCellH = 64f;
    const float CharHeight = 1.35f;     // 世界での 背たけ(m)。畳 1.8m と くらべて 子どもぐらい
    // ★どのコマが 誰かは 本人に 確認中。いまは 仮の わりあて
    const int CI_CIRNO = 5, CI_DAIYOU = 11;

    // 小物の 絵の ならび（props.png は 32px を 6こ 横に）
    const int PROP_KUSA = 0, PROP_SHIGE = 1, PROP_ZABU = 2, PROP_KABIN = 3, PROP_SENKO = 4, PROP_UCHI = 5;

    // 草木の 絵（nature.png：144pxの コマを 4列 x 2行）。
    // もとは ansimuz「Trees & Bushes」(CC0)。**32px＝1m** に そろえて 詰めなおして ある
    const int NatureCols = 4, NatureRows = 2;
    const float NatureCell = 4.5f;   // 144px ÷ 32px/m
    const int NA_KI_L = 0, NA_KI_R = 1, NA_SHIGE_A = 2, NA_SHIGE_B = 3,
              NA_MATSU = 4, NA_KUSA_A = 5, NA_KUSA_B = 6, NA_KUSA_C = 7;

    // 草木を 置く。大きさは 絵が もっている ので、コマの 大きさを そのまま わたす
    static void Nature(string name, Texture2D atlas, int index, Vector3 pos, Transform root) {
        Prop(name, atlas, index, NatureCols, NatureRows, pos, NatureCell, root, PropKind.Billboard);
    }

    // まわりを かこむ 木立ち。遊べる ところ(内がわの 半径)から 外へ 帯状に 置く
    static void Woods(Transform root, Texture2D nature) {
        if (nature == null) return;
        const int Count = 64;
        const float RIn = 13.5f, ROut = 27f;     // この あいだに ばらまく
        for (int i = 0; i < Count; i++) {
            // 黄金角で まわすと かたよらずに ばらける。半径は 平方根で とると 面で 均一に なる
            float a = i * 2.39996f;
            float t = (i * 0.6180339f) % 1f;
            float r = Mathf.Lerp(RIn, ROut, Mathf.Sqrt(t));
            var pos = new Vector3(GardenX + Mathf.Cos(a) * r, GroundY, GardenZ + Mathf.Sin(a) * r);
            Nature("Wood" + i, nature, (i % 2 == 0) ? NA_KI_L : NA_KI_R, pos, root);
        }
    }

    // Billboard＝**1枚の 板**。いつも こちらを 向く。草木も 木も これ。
    // Flat＝ゆかに 寝かせる（ざぶとん・うちわ）
    //
    // ※ 前は 草木を「十字に 組んだ 2枚」に していたが、よその ゲームの 画を 見ると
    //   草木は **1枚絵**で 置くのが ふつうだった。十字だと 交差の 線が 見え、
    //   影も ×印に なって かえって 板だと ばれる。1枚に して、そのぶん
    //   **太陽を 手前がわに 回して 影を 奥へ 落とす**ことで 立体に 見せる（上の Sun を 参照）
    // Still＝板だが 揺れない（屋内の 線香や 花瓶。かぜは 入ってこない）
    enum PropKind { Billboard, Flat, Still }

    // --- 2Dドット絵の 小物を 置く（props.png は 6列 x 1行）
    static void Prop(string name, Texture2D atlas, int index, Vector3 pos, float height,
                     Transform root, PropKind kind) {
        Prop(name, atlas, index, 6, 1, pos, height, root, kind);
    }

    static void Prop(string name, Texture2D atlas, int index, int cols, int rows,
                     Vector3 pos, float height, Transform root, PropKind kind) {
        if (atlas == null) return;
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = pos;

        // コマの ばしょ。画像は 上が 0行めだが UVは 下が 0 なので y は ひっくり返す
        int col = index % cols, row = index / cols;
        var uvS = new Vector2(1f / cols, 1f / rows);
        var uvO = new Vector2(col / (float)cols, (rows - 1 - row) / (float)rows);

        // 草木は かぜに ゆれる。寝かせた 小物は 動かさない
        bool sways = kind == PropKind.Billboard;
        var m = SpriteMat(MatDir + "Prop_" + name + ".mat", atlas, uvS, uvO,
                          sways ? 0.020f : 0f,   // たての のびちぢみ
                          sways ? 0.9f  : 0f,    // はやさ
                          sways ? 0.022f : 0f,   // よこ揺れ
                          sways ? 0.5f  : 0f,
                          PhaseOf(pos));

        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "Sheet";
        q.transform.SetParent(go.transform, false);
        Object.DestroyImmediate(q.GetComponent<Collider>());
        if (kind == PropKind.Flat) {
            q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // ゆかに 寝かせる
            q.transform.localPosition = Vector3.zero;
        } else {
            q.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
        }
        q.transform.localScale = new Vector3(height, height, 1f);
        var r = q.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = kind == PropKind.Flat
            ? UnityEngine.Rendering.ShadowCastingMode.Off      // 寝かせた 板は 影を おとさない
            : UnityEngine.Rendering.ShadowCastingMode.On;

        if (kind != PropKind.Flat) go.AddComponent<Billboard>();
    }

    // 置いた ところで 揺れの ずれを 決める。ならべても そろって 動かない ように
    static float PhaseOf(Vector3 pos) {
        return Mathf.Repeat(pos.x * 3.1f + pos.z * 1.7f, 6.2831853f);
    }

    // ドット絵の 板ようの 素材。**息づかい／ゆれの シェーダ**を つける。
    // 見つからない ときだけ URP/Lit に 落とす（ビルドが 止まらない ように）
    static Material SpriteMat(string path, Texture2D tex, Vector2 uvScale, Vector2 uvOffset,
                              float breatheAmp, float breatheSpeed,
                              float swayAmp, float swaySpeed, float phase) {
        var sh = Shader.Find("Natsuyasumi/PixelSprite");
        bool custom = sh != null;
        if (!custom) {
            Debug.LogWarning("[BuildZashiki] Natsuyasumi/PixelSprite が 見つからない。URP/Lit で 代用する");
            sh = Shader.Find("Universal Render Pipeline/Lit");
        }
        var m = new Material(sh);
        if (custom) {
            m.SetFloat("_Cutoff", 0.5f);
            m.SetFloat("_BreatheAmp", breatheAmp);
            m.SetFloat("_BreatheSpeed", breatheSpeed);
            m.SetFloat("_SwayAmp", swayAmp);
            m.SetFloat("_SwaySpeed", swaySpeed);
            m.SetFloat("_Phase", phase);
            m.SetFloat("_Wrap", 0.55f);
        } else {
            m.SetFloat("_Surface", 0);
            m.SetFloat("_AlphaClip", 1);
            m.SetFloat("_Cutoff", 0.5f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Smoothness", 0f);
            m.SetFloat("_Cull", 0f);
        }
        m.doubleSidedGI = true;
        m.SetTexture("_BaseMap", tex);
        m.SetTextureScale("_BaseMap", uvScale);
        m.SetTextureOffset("_BaseMap", uvOffset);
        m.mainTexture = tex;
        m.mainTextureScale = uvScale;
        m.mainTextureOffset = uvOffset;
        AssetDatabase.CreateAsset(m, path);
        return m;
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
        // 絵の たてよこ比を くずさない。足もとが ちょうど 地めんに くる ように 半分だけ 上げる
        float ch = CharHeight, cw = CharHeight * (CharCellW / CharCellH);
        quad.transform.localPosition = new Vector3(0, ch * 0.5f, 0);
        quad.transform.localScale = new Vector3(cw, ch, 1f);
        Object.DestroyImmediate(quad.GetComponent<Collider>());

        // ドット絵用：切りぬき＋点フィルタ（にじませない）＋**息づかい**。
        // 画像は 上が 0行めだが、UVは 下が 0。なので y は ひっくり返して 数える
        int col = index % CharCols, row = index / CharCols;
        var uvS = new Vector2(1f / CharCols, 1f / CharRows);
        var uvO = new Vector2(col / (float)CharCols, (CharRows - 1 - row) / (float)CharRows);
        // 呼吸は 背たけの 3.5%ぶん。周期は 2π/1.45 ≒ 4.3秒＝落ちついた いき。
        // ずれ(_Phase)を 置き場所から 決めるので、ふたりが 同じ 拍で 動かない
        var m = SpriteMat(MatDir + "Char_" + name + ".mat", sheet, uvS, uvO,
                          0.035f, 1.45f, 0.006f, 0.6f, PhaseOf(pos));
        quad.GetComponent<Renderer>().sharedMaterial = m;
        var mr = quad.GetComponent<Renderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;   // 板でも 影は 出す
        go.AddComponent<Billboard>();
        return go;
    }

    // --- 虫かご。竹ひごの かごを 細い 箱で 組む。中が 見える ように すきまを あける
    static void MakeBugCage(Transform root, Texture2D bugAtlas, Vector3 at, Material wood) {
        var go = new GameObject("BugCage");
        go.transform.SetParent(root, false);
        go.transform.position = at;

        const float W = 0.42f, H = 0.46f;   // かごの 大きさ（40cm ほど）
        // たての ひご
        for (int i = 0; i < 8; i++) {
            float a = i * Mathf.PI * 2f / 8f;
            var p = new Vector3(Mathf.Cos(a) * W * 0.5f, H * 0.5f, Mathf.Sin(a) * W * 0.5f);
            var b = Box("Bar" + i, go.transform, p, new Vector3(0.022f, H, 0.022f), wood);
            Object.DestroyImmediate(b.GetComponent<Collider>());
        }
        // 上と 下の わ
        foreach (var y in new[] { 0.02f, H }) {
            for (int i = 0; i < 8; i++) {
                float a = (i + 0.5f) * Mathf.PI * 2f / 8f;
                var p = new Vector3(Mathf.Cos(a) * W * 0.5f, y, Mathf.Sin(a) * W * 0.5f);
                var b = Box("Ring" + y + "_" + i, go.transform, p, new Vector3(0.17f, 0.026f, 0.026f), wood);
                b.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                Object.DestroyImmediate(b.GetComponent<Collider>());
            }
        }
        var cage = go.AddComponent<BugCage>();
        cage.atlas = bugAtlas;
    }

    // --- 雨。つぶは Kenney「Particle Pack」(CC0)の まるい 絵を たてに のばして 使う。
    // 出す/止めるは Weather が やる。ここでは **形だけ** 作る
    static ParticleSystem Rain(Transform root, Vector3 pos, Vector3 box) {
        var go = new GameObject("Rain");
        go.transform.SetParent(root, false);
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        // 落ちきる 手前で 消えれば よい（7mを 9〜16m/秒＋重力＝1秒たらず）。
        // ながく もたせると 生きている つぶが 増えるだけで、絵は 変わらない
        main.startLifetime = 1.2f; main.startSpeed = 9f; main.startSize = 0.05f;
        main.maxParticles = 5000;
        main.startColor = new Color(0.80f, 0.86f, 0.94f, 0.42f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.9f;
        main.startRotation = 0f;
        var em = ps.emission; em.rateOverTime = 0f;              // はじめは 降っていない
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box; sh.scale = box;
        sh.rotation = new Vector3(90f, 0f, 0f);                  // 下むきに 出す
        var r = go.GetComponent<ParticleSystemRenderer>();
        // **たてに のばした 板**＝雨すじ。丸のままだと 雪に 見える
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.10f; r.lengthScale = 1.2f;
        r.material = ParticleMat("Rain", "Assets/Art/Particles/circle_05.png",
                                 new Color(0.82f, 0.88f, 0.96f, 1f));
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    // つぶ用の 素材。シェーダ名は 版で かわるので 順に さがす（見つからないと まっピンクに なる）
    static Material ParticleMat(string name, string texPath, Color tint) {
        var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
              ?? Shader.Find("Universal Render Pipeline/Unlit")
              ?? Shader.Find("Sprites/Default");
        var m = new Material(sh);
        m.SetFloat("_Surface", 1);                    // 透ける
        m.SetFloat("_Blend", 0f);
        // **重ねかたは 数値で 直に 入れる。** _Blend だけだと 画面用の 設定で、
        // コードから 作った 素材には 反映されない ことが ある
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_AlphaClip", 0f);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        m.SetColor("_BaseColor", tint);
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (t == null) Debug.LogError("[BuildZashiki] つぶの 絵が 見つからない: " + texPath);
        else { m.SetTexture("_BaseMap", t); m.mainTexture = t; }
        AssetDatabase.CreateAsset(m, MatDir + name + ".mat");
        return m;
    }

    // --- 舞う 塵
    static void Dust(Transform root) {
        var go = new GameObject("Dust");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(0, 1.4f, 0.5f);
        var ps = go.AddComponent<ParticleSystem>();
        // **量も 大きさも 抑える。** 前は 220こ・0.022m で「屋内に 雪が 降っている」ように 見えた。
        // 光の すじの なかで たまに きらっと する ぐらいが ちょうどよい
        var main = ps.main;
        main.startLifetime = 16f; main.startSpeed = 0.035f;
        main.startSize = 0.009f;                       // 0.022 → 0.009（見た目 半分以下）
        main.maxParticles = 55;                        // 220 → 55
        main.startColor = new Color(1f, 0.97f, 0.86f, 0.36f);   // うすく
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.003f;
        var em = ps.emission; em.rateOverTime = 3.4f;  // 16 → 3.4
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
