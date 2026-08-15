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
        billboards.Clear();

        Renderer shojiPaperRenderer = null;
        Light lamp = null;

        RoomCutaway.Piece[] cutPieces = null;
        if (ShowRoom) {
            // **田の字型の 農家を 建てる。** 間取りの 出どころは BuildHouse.cs の 頭に 書いた。
            // 手前の 壁と 屋根は「中に 入ったら 消える」＝RoomCutaway が やる
            var hm = new BuildHouse.Mats {
                tatami = mTatami, wood = mWood, floor = mFloorW, plaster = mPlaster,
                roof = mRoof, paper = mPaper, stone = mStone,
                soil = Mat("DomaSoil", ArtTex + "dirt_path.png", new Vector2(3f, 2f), 0f, 1f),
            };
            cutPieces = BuildHouse.Build(root, hm, (nm, par, pos, size, mat) => Box(nm, par, pos, size, mat));
            shojiPaperRenderer = null;
            // 行灯（居間）
            var andon = Box("Andon", root, new Vector3(-3.4f, 0.55f, -1.6f),
                            new Vector3(0.34f, 1.1f, 0.34f), mPaper);
            var lampGO = new GameObject("Andon_Light");
            lampGO.transform.SetParent(andon.transform, false);
            lamp = lampGO.AddComponent<Light>();
            lamp.type = LightType.Point; lamp.color = new Color(1f, 0.82f, 0.55f);
            lamp.intensity = 3.2f; lamp.range = 7f; lamp.shadows = LightShadows.Soft;
            // ちゃぶ台（居間）
            Box("Table_Top", root, new Vector3(-3.4f, 0.34f, 1.4f), new Vector3(1.3f, 0.07f, 0.9f), mFloorW);
            for (int i = 0; i < 4; i++) {
                float sx = (i % 2 == 0) ? -1 : 1, sz = (i < 2) ? -1 : 1;
                Box("Table_Leg" + i, root, new Vector3(-3.4f + sx * 0.55f, 0.17f, 1.4f + sz * 0.36f),
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
        DynamicGI.UpdateEnvironment();

        // --- キャラ（2Dの 板）。ドット絵を そのまま 立てる。
        // 家を 出さない ときは 庭に 立たせる（畳の 上に 置くと 宙に 浮く）
        var chars = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sprites/chars_tall.png");
        // ★2026-08-15：**主人公は 8方向 x 8状態の 魔理沙**（本人が 用意）。
        //   立ち／歩き／走り／喜／怒／哀／楽／目とじ が 向きごとに そろって いる ので、
        //   これまでの「1枚を 左右 反転」を やめて、向きで 絵を 差しかえる
        var marisa = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sprites/marisa_8x8.png");
        if (marisa == null) Debug.LogError("[BuildZashiki] marisa_8x8.png が 見つからない");
        // 玄関の 前から はじめる（家は 扉から 入る）
        Vector3 p1 = new Vector3(BuildHouse.DoorOpenX, TerrainGen.Height(BuildHouse.DoorOpenX, 6.2f) + 0.1f, 6.2f);
        Vector3 p2 = new Vector3(-3.4f, 0.05f, 1.2f);
        var player = MakeChar("Marisa", marisa, 0, p1, root, MarisaCols, MarisaRows, MarisaCellW, MarisaCellH);
        var partner = MakeChar("Daiyou", chars, CI_DAIYOU, p2, root);
        // あるけるように する。当たりは カプセル、壁や 卓は 箱の あたりで 止まる
        var ccc = player.AddComponent<CharacterController>();
        ccc.height = 1.0f; ccc.radius = 0.26f; ccc.center = new Vector3(0f, 0.52f, 0f);
        ccc.slopeLimit = 50f; ccc.stepOffset = 0.35f;
        var pm = player.AddComponent<PlayerMove>();
        pm.sprite = player.transform.GetChild(0);
        // 8方向 x 8状態の 絵から 1コマを えらぶ 係
        var cs = player.AddComponent<CharSprite>();
        cs.target = player.transform.GetChild(0).GetComponent<Renderer>();
        cs.runSpeed = (pm.speed + pm.runSpeed) * 0.5f;   // 歩きと 走りの あいだで 切りかえる
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
        cam.nearClipPlane = 0.1f; cam.farClipPlane = 260f;   // 山の むこうまで 見える ように
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
        // **正面から。** 斜めだと 家の うしろが 死角に なり、
        // どこが 通れて どこが 見えるかが 分からなく なる
        orbit.pitch = 26f; orbit.yaw = 180f; orbit.distance = 9.0f;
        orbit.follow = player.transform;                 // あるくと ついてくる
        orbit.followOffset = new Vector3(0f, 0.70f, 0f);

        // **主人公の まわりだけ 手前の ものを 抜く。**
        // 電柱や カーブミラー、手前の 木で 主人公が 隠れなく なる
        var see = camGO.AddComponent<SeeThrough>();
        see.target = player.transform;

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
        // **遊べる ところは 四角。** カメラを 正面に 固定したので、
        // 死角に 入れると 何も 見えなくなる。TerrainGen が 決めた 四角に そろえる
        float wx0 = TerrainGen.PlayMinX, wx1 = TerrainGen.PlayMaxX;
        float wz0 = TerrainGen.PlayMinZ, wz1 = TerrainGen.PlayMaxZ;
        Invisible("Bound_Front", root, new Vector3((wx0 + wx1) * 0.5f, 1.2f, wz1), new Vector3(wx1 - wx0, 3f, 0.3f));
        Invisible("Bound_Left",  root, new Vector3(wx0, 1.2f, (wz0 + wz1) * 0.5f), new Vector3(0.3f, 3f, wz1 - wz0));
        Invisible("Bound_Right", root, new Vector3(wx1, 1.2f, (wz0 + wz1) * 0.5f), new Vector3(0.3f, 3f, wz1 - wz0));
        // おくの かべは **山への 登り口ぶんだけ 開けて おく**。
        // ここが 高台への 一本道に なる
        float tx = TerrainGen.TrailX, th = TerrainGen.TrailHalf;
        Invisible("Bound_Back_L", root, new Vector3((wx0 + (tx - th)) * 0.5f, 1.2f, wz0),
                  new Vector3((tx - th) - wx0, 3f, 0.3f));
        Invisible("Bound_Back_R", root, new Vector3(((tx + th) + wx1) * 0.5f, 1.2f, wz0),
                  new Vector3(wx1 - (tx + th), 3f, 0.3f));

        // --- 山への 一本道と 高台の かこい。
        // **のぼり坂なので 高さは 地めんに あわせて 置く。**
        // まっすぐな 箱を 1つ 置くと、坂の 上では 埋まって しまい 素通りできた
        var lk = TerrainGen.Lookout;
        float lhx = TerrainGen.LookoutHalfX, lhz = TerrainGen.LookoutHalfZ;
        WallRun(root, "Trail_L", new Vector2(tx - th, wz0), new Vector2(tx - th, lk.y + lhz));
        WallRun(root, "Trail_R", new Vector2(tx + th, wz0), new Vector2(tx + th, lk.y + lhz));
        WallRun(root, "Look_L",  new Vector2(lk.x - lhx, lk.y + lhz), new Vector2(lk.x - lhx, lk.y - lhz));
        WallRun(root, "Look_R",  new Vector2(lk.x + lhx, lk.y + lhz), new Vector2(lk.x + lhx, lk.y - lhz));
        WallRun(root, "Look_B",  new Vector2(lk.x - lhx, lk.y - lhz), new Vector2(lk.x + lhx, lk.y - lhz));
        // 高台の 手前がわ（一本道の 出口の 両わき）
        WallRun(root, "Look_F1", new Vector2(lk.x - lhx, lk.y + lhz), new Vector2(tx - th, lk.y + lhz));
        WallRun(root, "Look_F2", new Vector2(tx + th, lk.y + lhz), new Vector2(lk.x + lhx, lk.y + lhz));

        // --- 高台に のぼると カメラが 裏へ まわりこむ。
        // ふだんは 正面 固定の まま＝ここだけの 見せ場に する
        orbit.zones = new[] {
            // ★川べり。**正面 固定の ままだと 川が カメラの うしろに なる。**
            //   大きい 川は 手前(+Z)を よこに 貫いて いるので、yaw180 の ままだと
            //   釣りや 水きりを する 当人には 水面が まったく 見えない
            //  （実さい 撮ったら 画に 入って いたのは 畑と 小屋だった）。
            //   岸に 立ったら 川の ほうへ 向きなおる
            new CamOrbit.Zone {
                name = "かわべり",
                area = new Bounds(new Vector3(0f, TerrainGen.Flat + 3f, 25.6f),
                                  new Vector3(54f, 10f, 5.2f)),
                // ★**回すのは 90度だけ。** 180度 回すと 押した キーの 行き先が
                //   まるごと 裏がえり、操作しづらかった（本人の 指摘）。
                //   90度なら 川に そって 横に 押しっぱなしで そのまま 進める
                yaw = 90f, pitch = 30f, distance = 11.5f,
                lookOffset = new Vector3(0f, 0.4f, 3.0f),
                fogScale = 0.7f,
                blend = 1.0f,
            },
            // ※高台の 見せ場は **いったん 止めて ある。**
            //   谷は 高台から 見て 手前(+Z)に あるので、見わたすには 180度 回すしか なく、
            //   「回すのは 90度まで」と 食いちがう。90度で 撮ったら 斜面しか 映らず
            //   主人公も 画から 出た。高台を 谷の 東がわへ 移せば 90度で 成りたつので、
            //   そのときに 入れなおす
            new CamOrbit.Zone {
                name = "みはらし（いまは 止めて ある）",
                // 手が とどかない ところに 置いて 効かなく して ある。
                // 高台じたい（登り道・棚・かこい）は そのまま 残す＝あとで 入れなおせる
                area = new Bounds(new Vector3(0f, -9999f, 0f), Vector3.one),
                yaw = 180f, pitch = 32f, distance = 17f,
                fogScale = 0.33f, blend = 0.8f,
            },
        };

        // --- 天気（はれ/くもり/あめ/ゆうだち）。雨は **庭の うえだけ** に 降らせる。
        // 屋根を はずした 切りぬきなので、部屋の 中まで 降らせると 雨もりに 見える
        var wxGO = new GameObject("Weather");
        var wx = wxGO.AddComponent<Weather>();
        // 降らせる 範囲。**部屋の 上には かけない。**
        // 屋根を 切りとった 見せかたなので、部屋に 降らせると 雨もりに 見える。
        // カメラは 右手前から 覗くので、見えているのは 家の むこうの 庭がわ＝そこに 降らせれば 足りる
        wx.rain = Rain(root, new Vector3(GardenX - 1.5f, 7.5f, GardenZ), new Vector3(22f, 0.5f, 24f));
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
        // 空。手続きで 描く（絵は 置かない）
        var skyMat = new Material(Shader.Find("Natsuyasumi/Sky"));
        AssetDatabase.CreateAsset(skyMat, MatDir + "Sky.mat");
        todc.skybox = skyMat;
        todc.weather = wx;                    // 時間帯を おいた **あと**に 天気を かぶせる
        wx.timeOfDay = todc;
        todc.tod = TimeOfDay.Tod.Asa;
        // **時間は 遊んで いる あいだ 進む。** 夏休みの 一日を 遊びきれる 長さに して、
        // あさに 起きて よるに なる ところまで ひと続きで 見えるように する
        todc.runClock = true;
        todc.hour = 6.5f;                     // 起きたて。朝の 低い 光から はじまる
        todc.realMinutesPerDay = 42f;
        todc.Apply();

        // --- 塵（ほこり）。障子ごしの 光の すじの なかで きらきら 舞う＝屋内の 見どころ。
        // 外では 意味が ないので 家を 出す ときだけ
        if (ShowRoom) Dust(root);

        // --- 2Dドット絵の 小物（調べた とおり、草木や 小物は 板の ドット絵で 置くのが 作法）
        var props = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sprites/props.png");
        // そとの 地めん（縁の したは 庭）。これが ないと 草が 宙に 浮く
        // --- 地めん。**平らな 板を やめて 山ぎわに した。**
        // 家の まわりだけ 平ら、そこから 山へ 上がる 斜面。
        // 人の 通る ところは 草が はげて 土が 出る（踏み分け道）＝TerrainGen が 作る
        var mGround = new Material(Shader.Find("Natsuyasumi/Ground")
                                   ?? Shader.Find("Universal Render Pipeline/Lit"));
        var texGrass = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtTex + "grass_ground.png");
        var texDirt  = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtTex + "dirt_path.png");
        if (texGrass == null || texDirt == null) Debug.LogError("[BuildZashiki] 地めんの 絵が 見つからない");
        mGround.SetTexture("_GrassMap", texGrass);
        mGround.SetTexture("_DirtMap", texDirt);
        mGround.SetFloat("_TileGrass", 1.5f);      // 48px = 1.5m
        mGround.SetFloat("_TileDirt", 1.5f);
        mGround.SetFloat("_Wrap", 0.25f);
        AssetDatabase.CreateAsset(mGround, MatDir + "Ground.mat");
        TerrainGen.Build(root, mGround);

        // --- 水。小川（笹船を ながす）と 大きめの 川（水きり・釣り）
        var mWater = new Material(Shader.Find("Natsuyasumi/Water")
                                  ?? Shader.Find("Universal Render Pipeline/Lit"));
        AssetDatabase.CreateAsset(mWater, MatDir + "Water.mat");
        TerrainGen.BuildWater(root, mWater);

        // 草木は **自分で 描くのを やめて**、ansimuz(CC0)の「Trees & Bushes」に 差し替えた。
        // 32px＝1m で 詰めなおして あるので、コマの 大きさ(4.5m)を そのまま わたせば 尺が 合う
        // （大きい 木＝4.1m、しげみ＝1.2m、小草＝0.5m ぐらいに なる）
        var nature = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sprites/nature.png");
        if (nature == null) Debug.LogError("[BuildZashiki] 草木の 絵が 見つからない");

        // --- 谷そこ の 作りこみ。納屋・畑・農具小屋・井戸・祠
        // （それぞれの 中身と 出どころは BuildVillage.cs の 頭に 書いた）
        // 畑の うねは **土の 色**。石の 灰色だと 骨組みに 見えた
        var mSoil = Mat("Soil", ArtTex + "dirt_path.png", new Vector2(1.2f, 0.6f), 0f, 1f);
        // 納屋の 板は 母屋より 明るく（母屋の 柱と 同じ 暗さだと かたまりに 見える）
        var mNaya = Mat("NayaWood", ArtTex + "wood_floor.png", new Vector2(2.4f, 1.4f), 0f, 0.76f);
        // ★柱（電柱・鳥居・カーブミラーの さお）は **穴の あく シェーダ**に する。
        //   細くて 背が たかい ものは カメラと 主人公の あいだに 立ちやすい。
        //   PixelSprite は ClipHole を もって いるので、主人公の まわりだけ 抜ける
        var mPost = SeeThroughMat("PostST", ArtTex + "wood_beam.png", new Vector2(1f, 2f));
        var mMirror = SeeThroughMat("MirrorST", ArtTex + "plaster_wall.png", new Vector2(1f, 1f));
        var vmat = new BuildVillage.Materials {
            wood = mNaya, floor = mFloorW, plaster = mPlaster,
            roof = mRoof, stone = mStone, paper = mPaper, soil = mSoil,
            post = mPost, seeThrough = mMirror,
        };
        BuildVillage.Build(root, vmat,
            (nm, par, pos, size, mat) => Box(nm, par, pos, size, mat),
            (nm, pos, kind, h) => {
                int cell = kind == 0 ? NA_KUSA_A : (kind == 1 ? NA_KUSA_B : NA_KUSA_C);
                Prop(nm, nature, cell, NatureCols, NatureRows, pos, h, root, PropKind.Billboard);
            });

        // --- 山の 木。**素材は ansimuz「Trees & Bushes」(CC0) だけ を つかう。**
        // 針葉樹は こちらで 描いて みたが、素材と ならべると すぐ 見おとりした（本人の 指摘）。
        // CC0 の 針葉樹は 探した かぎり 見つからなかった（LPC Trees は CC-BY-SA、
        // Various pixel art trees は 配布停止、CraftPix の 無料版は 再配布 不可）。
        // → **まだらは 色づけで 出す。** CC0 なので 改変は 自由
        var rngTree = new System.Random(20260815);
        var spots = TerrainGen.Scatter(9000, 12f, 70f, rngTree, 3.4f, true, true);
        for (int i = 0; i < spots.Count; i++) {
            var sp = spots[i];
            bool conifer = sp.cover == TerrainGen.Cover.Conifer;
            int cell = (i % 2 == 0) ? NA_KI_L : NA_KI_R;
            var tint = conifer ? new Color(0.70f, 0.86f, 0.80f)
                               : new Color(1.05f, 1.00f, 0.84f);
            NatureTinted("Ki" + i, nature, cell, sp.pos,
                         NatureCell * sp.size * (conifer ? 1.15f : 1f), root, tint);
        }

        // --- 下ばえ。しげみ・草。これも 素材の コマだけ
        var underSpots = TerrainGen.Scatter(4000, 10f, 66f, new System.Random(7788), 4.6f, true, true);
        for (int i = 0; i < underSpots.Count; i++) {
            var sp = underSpots[i];
            int cell = i % 3 == 0 ? NA_SHIGE_A : (i % 3 == 1 ? NA_SHIGE_B : NA_MATSU);
            NatureTinted("Shige" + i, nature, cell, sp.pos, NatureCell * sp.size, root,
                         sp.cover == TerrainGen.Cover.Conifer
                             ? new Color(0.80f, 0.90f, 0.84f) : Color.white);
        }
        // 家の まわりの 草むら
        var weedSpots = TerrainGen.Scatter(1200, 5f, 28f, new System.Random(4242), 3.0f, false);
        for (int i = 0; i < weedSpots.Count; i++) {
            int cell = (i % 3 == 0) ? NA_KUSA_A : (i % 3 == 1 ? NA_KUSA_B : NA_KUSA_C);
            NatureTinted("Kusa" + i, nature, cell, weedSpots[i].pos,
                         NatureCell * weedSpots[i].size, root, Color.white);
        }
        Debug.Log(string.Format("[BuildZashiki] 木={0} 下ばえ={1} 草={2}",
                  spots.Count, underSpots.Count, weedSpots.Count));

        // --- さかいの 生垣。**見えない かべだけだと「なぜ 進めないか」が 分からない。**
        // 左右の へりに しげみを ならべて、目にも 行きどまりだと 分かる ように する
        {
            float step = 2.3f;
            for (float z = TerrainGen.PlayMinZ; z <= TerrainGen.PlayMaxZ; z += step) {
                foreach (float x in new[] { TerrainGen.PlayMinX - 0.7f, TerrainGen.PlayMaxX + 0.7f }) {
                    var p = new Vector3(x, TerrainGen.Height(x, z), z);
                    NatureTinted("Ikegaki" + x + "_" + z, nature, NA_SHIGE_A, p,
                                 NatureCell * 0.95f, root, new Color(0.86f, 0.94f, 0.86f));
                }
            }
            // おくの へり（山の 足もと）。登り口の ぶんだけ あける
            for (float x = TerrainGen.PlayMinX; x <= TerrainGen.PlayMaxX; x += step) {
                if (x > -22.5f && x < -17.5f) continue;      // 山への 登り口
                float z = TerrainGen.PlayMinZ - 0.7f;
                var p = new Vector3(x, TerrainGen.Height(x, z), z);
                NatureTinted("IkegakiN" + x, nature, NA_SHIGE_B, p,
                             NatureCell * 1.05f, root, new Color(0.80f, 0.90f, 0.84f));
            }
        }

        // --- ひみつきちの やぶ。
        // **「ひみつ」なのだから 草はらの まん中に 建って いては いけない。**
        // 遊べる 四角の 中には 木を 生やさない きまりなので、ここだけ 手で しげみを 回す。
        // 手前(+Z)は あけて おく＝入口。カメラから 中が 見える
        {
            // **まばらに 置くと「点々と 草が ある 原っぱ」に しか ならない。**
            // 半径ぞいに 詰めて 並べて、はじめて 囲まれて 見える。
            // おく と 左右は 木の 高さ、手前は しげみ（低く して 中が 見える ように）
            float bx = 19f, bz = 15f;
            int n = 0;
            for (float a = 20f; a <= 340f; a += 13f) {
                float r = 3.9f + ((n % 3) - 1) * 0.35f;
                float x = bx + Mathf.Sin(a * Mathf.Deg2Rad) * r;
                float z = bz - Mathf.Cos(a * Mathf.Deg2Rad) * r;
                if (z > bz + 2.4f) { n++; continue; }          // 手前は 入口ぶん あける
                bool tall = z < bz + 0.5f;                      // おくがわは 背たかく
                var p = new Vector3(x, TerrainGen.Height(x, z), z);
                NatureTinted("Yabu" + n, nature,
                             tall ? (n % 2 == 0 ? NA_KI_L : NA_KI_R) : (n % 3 == 0 ? NA_MATSU : (n % 2 == 0 ? NA_SHIGE_A : NA_SHIGE_B)),
                             p, NatureCell * (tall ? 0.80f + (n % 3) * 0.08f : 1.00f + (n % 3) * 0.08f), root,
                             new Color(0.70f, 0.84f, 0.70f));
                n++;
            }
        }

        // 部屋の なかの 小物（家を 出す ときだけ）
        if (ShowRoom) {
            Prop("Zabuton1", props, PROP_ZABU,  new Vector3(-1.45f, 0.012f, 0.55f), 0.85f, root, PropKind.Flat);
            Prop("Zabuton2", props, PROP_ZABU,  new Vector3( 0.35f, 0.012f, 1.55f), 0.85f, root, PropKind.Flat);
            Prop("Uchiwa",   props, PROP_UCHI,  new Vector3( 1.55f, 0.014f, 1.15f), 0.55f, root, PropKind.Flat);
            Prop("Boushi",   props, PROP_BOUSHI,new Vector3(-2.55f, 0.016f, 1.75f), 0.60f, root, PropKind.Flat);
            Prop("Senko",    props, PROP_SENKO, new Vector3( 1.95f, 0.0f,   2.10f), 0.40f, root, PropKind.Still);
            Prop("Kabin",    props, PROP_KABIN, new Vector3(-2.45f, 0.0f,  -1.85f), 0.80f, root, PropKind.Still);
            // ちゃぶ台の 上の すいか（台の 天板は y=0.34、厚み 0.07）
            Prop("Suika",    props, PROP_SUIKA, new Vector3(-0.60f, 0.385f, 0.50f), 0.42f, root, PropKind.Still);
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

        // --- むしずもう。大ようせいの そばで スペース＝いどむ（あみを ふるのと 同じ ボタン）
        var sumoGO = new GameObject("BugSumo");
        var sumo = sumoGO.AddComponent<BugSumo>();
        sumo.atlas = bugAtlas; sumo.font = hud.font; sumo.panel = hud.panel;
        sumo.partner = partner.transform;

        // --- 田舎の 遊び（ささぶね／水きり／つり／花つみ／色水／おし花／ひみつきち）
        var playAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sprites/play.png");
        if (playAtlas == null) Debug.LogError("[BuildZashiki] play.png が 見つからない");
        var play = player.AddComponent<PlayHost>();
        play.atlas = playAtlas; play.font = hud.font; play.panel = hud.panel;
        PlaySpots(root);

        // --- 屋内の 切りぬき。**中に 入ったら 手前の 壁と 屋根を 消す**
        if (cutPieces != null) {
            var cutGO = new GameObject("RoomCutaway");
            cutGO.transform.SetParent(root, false);
            var cut = cutGO.AddComponent<RoomCutaway>();
            cut.player = player.transform;
            cut.pieces = cutPieces;
            cut.doorZ = BuildHouse.Z1;
            cut.houseArea = new Bounds(
                new Vector3(0f, 1.6f, (BuildHouse.Z0 + BuildHouse.EngawaZ) * 0.5f),
                new Vector3(BuildHouse.X1 - BuildHouse.X0 + 0.6f, 7f,
                            BuildHouse.EngawaZ - BuildHouse.Z0 + 0.6f));
        }

        // --- 部屋の 名。**家具だけでは どこが どの 部屋か 読めない**ので、
        // 立った ときに ひとこと 出す
        if (ShowRoom) {
            var rl = new GameObject("RoomLabel");
            rl.transform.SetParent(root, false);
            var lab = rl.AddComponent<RoomLabel>();
            lab.player = player.transform;
            lab.rooms = HouseRooms();
        }

        // --- 草木の 向きを まとめる 係
        var fieldGO = new GameObject("BillboardField");
        fieldGO.transform.SetParent(root, false);
        var field = fieldGO.AddComponent<BillboardField>();
        field.items = billboards.ToArray();
        field.follow = player.transform;
        Debug.Log("[BuildZashiki] 板の 草木 = " + billboards.Count + " まい");

        EditorSceneManager.SaveScene(scene, ScnDir + "Zashiki.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("[BuildZashiki] done: " + ScnDir + "Zashiki.unity");
    }

    // キャラの 絵：chars_tall.png は **1コマ 48x64 の 10列 x 3行**（index = 行*10 + 列）。
    // まえの 16x16 は 等身が ひくかったので、本人が 用意した 背の たかい 絵に 差し替えた
    const int CharCols = 10, CharRows = 3;
    const float CharCellW = 48f, CharCellH = 64f;
    // 魔理沙の 絵。8方向(列) x 8状態(行)。make_marisa.py が 詰めなおした 大きさ
    const int MarisaCols = 8, MarisaRows = 8;
    const float MarisaCellW = 115f, MarisaCellH = 167f;
    const float CharHeight = 1.35f;     // 世界での 背たけ(m)。畳 1.8m と くらべて 子どもぐらい
    // ★どのコマが 誰かは 本人に 確認中。いまは 仮の わりあて
    const int CI_CIRNO = 5, CI_DAIYOU = 11;

    // 部屋の 小物（props.png は 1コマ 32px を 6こ 横に）。
    // 2026-08-14、木立ちの 20色で 描き直した（ここだけ 前の 絵で 浮いていた）
    const int PROP_BOUSHI = 0, PROP_SUIKA = 1, PROP_ZABU = 2, PROP_KABIN = 3, PROP_SENKO = 4, PROP_UCHI = 5;

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

    // 色を つけて 置く。**まだらは これで 出す**（素材は 1つの まま）
    static void NatureTinted(string name, Texture2D atlas, int index, Vector3 pos, float height,
                             Transform root, Color tint) {
        Prop(name, atlas, index, NatureCols, NatureRows, pos, height, root, PropKind.Billboard, tint);
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

    // 木は **ぶつかる**。すりぬけると 森が ただの 絵に なる。
    // みきの ぶんだけ 細い 円柱で 止める（葉の ぶんまで 止めると 歩けない）
    static void AddTrunkCollider(GameObject go, float height, float trunkRadius) {
        var col = go.AddComponent<CapsuleCollider>();
        col.radius = trunkRadius;
        col.height = Mathf.Max(height * 0.55f, trunkRadius * 2.2f);
        col.center = new Vector3(0f, col.height * 0.5f, 0f);
    }

    // --- 2Dドット絵の 小物を 置く（props.png は 6列 x 1行）
    static void Prop(string name, Texture2D atlas, int index, Vector3 pos, float height,
                     Transform root, PropKind kind) {
        Prop(name, atlas, index, 6, 1, pos, height, root, kind);
    }

    static void Prop(string name, Texture2D atlas, int index, int cols, int rows,
                     Vector3 pos, float height, Transform root, PropKind kind) {
        Prop(name, atlas, index, cols, rows, pos, height, root, kind, Color.white);
    }

    static void Prop(string name, Texture2D atlas, int index, int cols, int rows,
                     Vector3 pos, float height, Transform root, PropKind kind, Color tint) {
        if (atlas == null) return;
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = pos;

        // コマの ばしょ。画像は 上が 0行めだが UVは 下が 0 なので y は ひっくり返す
        int col = index % cols, row = index / cols;
        var uvS = new Vector2(1f / cols, 1f / rows);
        var uvO = new Vector2(col / (float)cols, (rows - 1 - row) / (float)rows);

        // 草木は かぜに ゆれる。**たてには のびちぢみさせない**
        //（木が 息を している ように 見えて おかしかった。ゆれるのは 葉＝よこだけ）。
        // 寝かせた 小物・屋内の 物は 動かさない。
        // ★素材は コマごとに 1つだけ 作って 使いまわす。ずらしは シェーダが
        //   置き場所から 決める ので、木が 増えても 素材は 増えない
        bool sways = kind == PropKind.Billboard;
        var m = SpriteMat(atlas, uvS, uvO,
                          0f,                    // たての のびちぢみ＝なし
                          0f,
                          sways ? 0.030f : 0f,   // よこ揺れ（葉の ぶん）
                          sways ? 0.55f : 0f,
                          0f, tint);

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

        // **1本ずつ Billboard を つけない。** 木が 数千本に なると
        // 毎フレーム 数千回の 呼び出しに なる。まとめ役(BillboardField)に 登録して、
        // カメラの 向きが 変わった ときだけ まとめて 回す
        if (kind != PropKind.Flat) billboards.Add(go.transform);
        // 大きい ものだけ ぶつかる（草に ぶつかると 歩けない）
        if (kind == PropKind.Billboard && height >= 2.2f)
            AddTrunkCollider(go, height, Mathf.Clamp(height * 0.055f, 0.16f, 0.45f));
    }

    // 板の 草木。あとで まとめて カメラの ほうへ 向ける
    static readonly System.Collections.Generic.List<Transform> billboards
        = new System.Collections.Generic.List<Transform>();

    // 置いた ところで 揺れの ずれを 決める。ならべても そろって 動かない ように
    static float PhaseOf(Vector3 pos) {
        return Mathf.Repeat(pos.x * 3.1f + pos.z * 1.7f, 6.2831853f);
    }

    // 同じ 見た目の 素材は 1つだけ 作って 使いまわす。
    // 1つずつ 作ると 木の 数だけ 描きなおしが 増えて、たくさん 置けなくなる
    static readonly System.Collections.Generic.Dictionary<string, Material> matCache
        = new System.Collections.Generic.Dictionary<string, Material>();

    static Material SpriteMat(Texture2D tex, Vector2 uvScale, Vector2 uvOffset,
                              float breatheAmp, float breatheSpeed,
                              float swayAmp, float swaySpeed, float phase) {
        return SpriteMat(tex, uvScale, uvOffset, breatheAmp, breatheSpeed,
                         swayAmp, swaySpeed, phase, Color.white);
    }

    static Material SpriteMat(Texture2D tex, Vector2 uvScale, Vector2 uvOffset,
                              float breatheAmp, float breatheSpeed,
                              float swayAmp, float swaySpeed, float phase, Color tint) {
        string key = string.Format("{0}_{1:F3}_{2:F3}_{3:F3}_{4:F3}_{5:F3}_{6:F3}_{7:F3}",
                                   tex != null ? tex.name : "none", uvScale.x, uvOffset.x, uvOffset.y,
                                   breatheAmp, swayAmp, swaySpeed, phase)
                   + "_" + Mathf.RoundToInt(tint.r * 99) + Mathf.RoundToInt(tint.g * 99)
                   + Mathf.RoundToInt(tint.b * 99);
        Material cached;
        if (matCache.TryGetValue(key, out cached) && cached != null) return cached;
        var path = MatDir + "Sprite_" + key.Replace('.', '_') + ".mat";
        var mm = SpriteMatNew(path, tex, uvScale, uvOffset,
                              breatheAmp, breatheSpeed, swayAmp, swaySpeed, phase);
        mm.SetColor("_BaseColor", tint);
        return matCache[key] = mm;
    }

    // ドット絵の 板ようの 素材。**息づかい／ゆれの シェーダ**を つける。
    // 見つからない ときだけ URP/Lit に 落とす（ビルドが 止まらない ように）
    // 主人公の まわりで **穴の あく** ふつうの 材質（3Dの 柱などに つかう）。
    // 息づかいも 揺れも 切って、ただの 板ばりに 見せる
    static Material SeeThroughMat(string name, string texPath, Vector2 tiling) {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex == null) Debug.LogError("[BuildZashiki] テクスチャが 見つからない: " + texPath);
        var m = SpriteMatNew(MatDir + name + ".mat", tex, tiling, Vector2.zero, 0f, 0f, 0f, 0f, 0f);
        m.SetFloat("_Wrap", 0.35f);       // 3Dの 面なので 板ほど 下駄は 要らない
        return m;
    }

    static Material SpriteMatNew(string path, Texture2D tex, Vector2 uvScale, Vector2 uvOffset,
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
        return MakeChar(name, sheet, index, pos, root, CharCols, CharRows, CharCellW, CharCellH);
    }

    static GameObject MakeChar(string name, Texture2D sheet, int index, Vector3 pos, Transform root,
                               int cols, int rows, float cellW, float cellH) {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = pos;

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Sprite";
        quad.transform.SetParent(go.transform, false);
        // 絵の たてよこ比を くずさない。足もとが ちょうど 地めんに くる ように 半分だけ 上げる
        float ch = CharHeight, cw = CharHeight * (cellW / cellH);
        quad.transform.localPosition = new Vector3(0, ch * 0.5f, 0);
        quad.transform.localScale = new Vector3(cw, ch, 1f);
        Object.DestroyImmediate(quad.GetComponent<Collider>());

        // ドット絵用：切りぬき＋点フィルタ（にじませない）＋**息づかい**。
        // 画像は 上が 0行めだが、UVは 下が 0。なので y は ひっくり返して 数える
        int col = index % cols, row = index / cols;
        var uvS = new Vector2(1f / cols, 1f / rows);
        var uvO = new Vector2(col / (float)cols, (rows - 1 - row) / (float)rows);
        // 呼吸は 背たけの 3.5%ぶん。周期は 2π/1.45 ≒ 4.3秒＝落ちついた いき。
        // ずれ(_Phase)を 置き場所から 決めるので、ふたりが 同じ 拍で 動かない
        var m = SpriteMatNew(MatDir + "Char_" + name + ".mat", sheet, uvS, uvO,
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

        const float W = 0.44f, H = 0.48f;   // かごの 大きさ（40cm ほど）
        // ※ はじめは 細い 棒を 8本 立てて 輪を まわしただけで、遠目には
        //   ただの 小枝の 束に 見えた。**上下を 円板で しめる**と かごに 見える
        Cyl("Base", go.transform, new Vector3(0, 0.025f, 0), new Vector3(W, 0.05f, W), wood);
        Cyl("Lid",  go.transform, new Vector3(0, H, 0),       new Vector3(W, 0.05f, W), wood);
        // たての ひご。数を へらして 太くする（細かいと 遠くで 消える）
        for (int i = 0; i < 6; i++) {
            float a = i * Mathf.PI * 2f / 6f;
            var p = new Vector3(Mathf.Cos(a) * W * 0.46f, H * 0.5f, Mathf.Sin(a) * W * 0.46f);
            var b = Box("Bar" + i, go.transform, p, new Vector3(0.034f, H, 0.034f), wood);
            Object.DestroyImmediate(b.GetComponent<Collider>());
        }
        // 手さげの わ（2本の 柱＋わたし）
        for (int i = -1; i <= 1; i += 2) {
            var b = Box("Grip" + i, go.transform, new Vector3(i * W * 0.30f, H + 0.09f, 0f),
                        new Vector3(0.026f, 0.18f, 0.026f), wood);
            Object.DestroyImmediate(b.GetComponent<Collider>());
        }
        var top = Box("GripTop", go.transform, new Vector3(0f, H + 0.18f, 0f),
                      new Vector3(W * 0.66f, 0.026f, 0.026f), wood);
        Object.DestroyImmediate(top.GetComponent<Collider>());
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
        // **レイの じゃまに しない（層 2 = Ignore Raycast）。**
        // 地めんの 高さを 真下への レイで 測って いるので、高さ 3m の この 壁に
        // 当たると そこを 地めんと 思いこみ、**ばったが 宙に 湧いた**。
        // 歩く 当たり（CharacterController）は 層に かかわらず 効くので これで よい
        go.layer = 2;
    }

    // 家の 部屋わけ。BuildHouse の 間取りの 数字から そのまま 作る
    static RoomLabel.Room[] HouseRooms() {
        const float H = 2.2f;                       // 部屋の 見はり箱の 高さ
        float f1 = BuildHouse.F1 + H * 0.5f;
        float f2 = BuildHouse.F2 + H * 0.5f;
        System.Func<string, float, float, float, float, float, RoomLabel.Room> mk =
            (name, x0, x1, z0, z1, y) => new RoomLabel.Room {
                name = name,
                area = new Bounds(new Vector3((x0 + x1) * 0.5f, y, (z0 + z1) * 0.5f),
                                  new Vector3(x1 - x0, H, z1 - z0)),
            };
        float X0 = BuildHouse.X0, MidX = BuildHouse.MidX, DomaX = BuildHouse.DomaX;
        float Z0 = BuildHouse.Z0, MidZ = BuildHouse.MidZ, Z1 = BuildHouse.Z1;
        return new[] {
            mk("ちゃのま",                    MidX, DomaX, MidZ, Z1, f1),
            mk("ざしき",                      X0, MidX, MidZ, Z1, f1),
            mk("おじさんたちの ねま",          MidX, DomaX, Z0, MidZ, f1),
            mk("ぶつま",                      X0, MidX, Z0, MidZ, f1),
            mk("だいどころ（どま）",           DomaX, BuildHouse.X1, Z0, Z1, f1),
            mk("いとこの へや",               X0, MidX, Z0, Z1, f2),
            mk("じぶんの へや",               MidX, DomaX, Z0, Z1, f2),
        };
    }

    // --- 田舎の 遊びが できる ところ を 置く。
    // **その 場所でしか できない こと**に する のが 肝。
    // どこでも できると 場所を おぼえる 意味が なくなり、地図が ただの 通路に なる
    static void PlaySpots(Transform root) {
        var host = new GameObject("PlaySpots");
        host.transform.SetParent(root, false);

        // 小川ばた：ささぶねを ながす。水は 山から 下って くる＝+Z へ 流れる
        Water(host, PlayKind.Sasabune, -20.3f, 13f, -22f, 13f, Vector3.forward, 2f);
        // 大きい 川：水きりと つり。岸は 遊べる 四角の 手前の へり
        Water(host, PlayKind.Mizukiri, 9f, 25.4f, 9f, 29.5f, Vector3.right, 9f);
        Water(host, PlayKind.Tsuri,   -1f, 25.4f, -1f, 29.5f, Vector3.right, 9f);
        // 野はら：花を つむ
        Spot(host, PlayKind.Hanatsumi, -9f, 13f, 2.4f);
        // 井戸ばた：つんだ 花を もんで 色水に する
        Spot(host, PlayKind.Irozu, -2.5f, 13.4f, 2.0f);
        // 縁がわ：本に はさんで おし花に する
        Spot(host, PlayKind.Oshibana, 2.0f, BuildHouse.EngawaZ - 0.5f, 1.6f);
        // やぶの 中：ひみつきち。**建った ぶんが その場に のこる**ように、
        // 5段ぶんを 先に 建てて おいて できた ぶんだけ 見せる
        var him = Spot(host, PlayKind.Himitsu, 19f, 15f, 2.6f);
        Himitsu(him.transform);
    }

    // ひみつきちの 5段。えだ→かべ→屋根→つくえ→はた
    static void Himitsu(Transform at) {
        var hb = at.gameObject.AddComponent<HimitsuBase>();
        float bx = at.position.x, bz = at.position.z, by = at.position.y;

        System.Func<string, Vector3, Vector3, Vector3, Color, Renderer> piece =
            (name, pos, scale, rot, col) => {
                var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                g.name = name; g.transform.SetParent(at, false);
                g.transform.position = new Vector3(bx, by, bz) + pos;
                g.transform.localScale = scale;
                g.transform.localRotation = Quaternion.Euler(rot);
                var mm = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mm.SetColor("_BaseColor", col);
                mm.SetFloat("_Smoothness", 0.04f);
                g.GetComponent<Renderer>().sharedMaterial = mm;
                Object.DestroyImmediate(g.GetComponent<Collider>());   // 通りぬけられる ほうが よい
                return g.GetComponent<Renderer>();
            };

        var wood = new Color(0.42f, 0.27f, 0.13f);
        var wood2 = new Color(0.52f, 0.36f, 0.16f);
        var leaf = new Color(0.21f, 0.40f, 0.07f);
        var cloth = new Color(0.72f, 0.30f, 0.22f);

        // 1段め：地ならし（丸太を 2本 ころがした だけ）
        var s1 = new[] {
            piece("HB_Maruta1", new Vector3(-1.2f, 0.14f, -0.9f), new Vector3(0.28f, 0.28f, 2.4f), Vector3.zero, wood),
            piece("HB_Maruta2", new Vector3( 1.2f, 0.14f, -0.9f), new Vector3(0.28f, 0.28f, 2.4f), Vector3.zero, wood),
        };
        // 2段め：えだの かべ（3面）。
        // **すきまを あけると 柵に 見える。**ほとんど 触れるまで 詰めて はじめて「かべ」に なる
        var s2 = new System.Collections.Generic.List<Renderer>();
        for (int i = 0; i < 14; i++) {
            float x = -1.55f + i * 0.24f;
            s2.Add(piece("HB_Eda" + i, new Vector3(x, 0.75f, -2.0f),
                         new Vector3(0.19f, 1.5f + (i % 3) * 0.12f, 0.13f),
                         new Vector3(0f, 0f, (i % 2 == 0 ? 4f : -3f)), i % 3 == 0 ? wood : wood2));
        }
        for (int i = 0; i < 9; i++) {
            float z = -1.95f + i * 0.24f;
            s2.Add(piece("HB_EdaL" + i, new Vector3(-1.5f, 0.72f, z),
                         new Vector3(0.13f, 1.42f + (i % 3) * 0.10f, 0.19f), Vector3.zero, i % 2 == 0 ? wood : wood2));
            s2.Add(piece("HB_EdaR" + i, new Vector3( 1.5f, 0.72f, z),
                         new Vector3(0.13f, 1.42f + (i % 3) * 0.10f, 0.19f), Vector3.zero, i % 2 == 0 ? wood2 : wood));
        }
        // 3段め：板きれの 屋根（葉を のせる）
        var s3 = new[] {
            piece("HB_Yane",  new Vector3(0f, 1.55f, -1.1f), new Vector3(3.3f, 0.10f, 2.2f), new Vector3(-9f, 0f, 0f), wood),
            piece("HB_Ha",    new Vector3(0f, 1.64f, -1.1f), new Vector3(3.0f, 0.08f, 1.9f), new Vector3(-9f, 0f, 0f), leaf),
        };
        // 4段め：木の 箱の つくえ と こしかけ
        var s4 = new[] {
            piece("HB_Tsukue", new Vector3(0f, 0.44f, -1.6f), new Vector3(1.1f, 0.85f, 0.7f), Vector3.zero, wood2),
            piece("HB_Isu",    new Vector3(0f, 0.22f, -0.7f), new Vector3(0.5f, 0.42f, 0.5f), Vector3.zero, wood),
        };
        // 5段め：はた（ここは おれたちの もの という しるし）
        var s5 = new[] {
            piece("HB_Sao",  new Vector3(1.55f, 1.35f, -2.05f), new Vector3(0.07f, 2.7f, 0.07f), Vector3.zero, wood2),
            piece("HB_Hata", new Vector3(1.95f, 2.35f, -2.05f), new Vector3(0.75f, 0.5f, 0.03f), Vector3.zero, cloth),
        };

        hb.stages = new[] {
            new HimitsuBase.Stage { parts = s1 },
            new HimitsuBase.Stage { parts = s2.ToArray() },
            new HimitsuBase.Stage { parts = s3 },
            new HimitsuBase.Stage { parts = s4 },
            new HimitsuBase.Stage { parts = s5 },
        };
        hb.Show(0);
    }

    static PlaySpot Spot(GameObject parent, PlayKind kind, float x, float z, float range) {
        var go = new GameObject("Play_" + kind);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = new Vector3(x, TerrainGen.Height(x, z), z);
        var s = go.AddComponent<PlaySpot>();
        s.kind = kind; s.range = range;
        return s;
    }

    static void Water(GameObject parent, PlayKind kind, float x, float z,
                      float wx, float wz, Vector3 flow, float span) {
        var s = Spot(parent, kind, x, z, 2.6f);
        // **水面の 高さは 場面を 組む ときに 入れて おく。**
        // 走って いる あいだに さがそうにも、川の 面には 当たりが ない
        int si; float across, waterY;
        TerrainGen.NearestStream(wx, wz, out si, out across, out waterY);
        s.water = new Vector3(wx, waterY, wz);
        s.flow = flow; s.span = span;
    }

    // 坂に そった 見えない かべ。**まっすぐな 箱 1つでは 坂を ふさげない。**
    // 坂の 下では 宙に 浮き、坂の 上では 地めんに 埋まって 素通りできる。
    // 短い 箱を つないで、それぞれ その場の 地めんの 高さに 置く
    static void WallRun(Transform parent, string name, Vector2 a, Vector2 b) {
        float len = Vector2.Distance(a, b);
        int n = Mathf.Max(1, Mathf.CeilToInt(len / 2f));
        for (int i = 0; i < n; i++) {
            var p0 = Vector2.Lerp(a, b, i / (float)n);
            var p1 = Vector2.Lerp(a, b, (i + 1) / (float)n);
            var mid = (p0 + p1) * 0.5f;
            float y = Mathf.Max(TerrainGen.Height(p0.x, p0.y), TerrainGen.Height(p1.x, p1.y));
            var d = p1 - p0;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name + "_" + i;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(mid.x, y + 1.3f, mid.y);
            go.transform.localRotation = Quaternion.Euler(0f, -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, 0f);
            go.transform.localScale = new Vector3(d.magnitude + 0.4f, 3.2f, 0.3f);
            Object.DestroyImmediate(go.GetComponent<MeshRenderer>());
            go.layer = 2;                     // レイの じゃまに しない（Invisible と 同じ 理由）
        }
    }

    // 円い もの（かごの ふた・そこ）。Cylinder は 高さ 1 が 縦 2 ぶんなので 半分に する
    static GameObject Cyl(string name, Transform parent, Vector3 pos, Vector3 size, Material m) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name; go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(size.x, size.y * 0.5f, size.z);
        go.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
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
