using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

// 第3の環境（D-114・2026-08-29）：HD-2D路線の「家の庭」1シーン。
//   本人の言：「1シーンにこだわって作ってから広げる。まずは家の庭。
//   見た目は基本アセットで（自分で作るよくわからん3Dの家や木がいちばんダメ）」
// 家=megakit(CC0)のジェッティの家／木・草・塀・門・飛び石=Kenney Nature Kit(CC0)。
//   rebuild.ps1 -Only BuildNiwa.Build
public static class BuildNiwa {

    static Material MatT(string name, string tex, float tx, float ty) {
        string dir = "Assets/Art/Materials/Niwa";
        System.IO.Directory.CreateDirectory(dir);
        string path = dir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = Color.white;
        m.SetFloat("_Smoothness", 0.05f);                       // つや消し（夜の 白うき よけ）
        m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/" + tex);
        m.mainTextureScale = new Vector2(tx, ty);
        return m;
    }

    static GameObject Box(Transform t, string name, Vector3 c, Vector3 s, Material m, bool mieru = true) {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name; g.transform.SetParent(t, false);
        g.transform.position = c; g.transform.localScale = s;
        if (m != null) g.GetComponent<Renderer>().sharedMaterial = m;
        if (!mieru) g.GetComponent<Renderer>().enabled = false;
        return g;
    }

    public static void Build() {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
            UnityEditor.SceneManagement.NewSceneMode.Single);
        var root = new GameObject("Niwa").transform;
        Random.InitState(20260829);

        // ---- 環境光は Flat・環境反射は 切る（mura で 学んだ 型）
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.52f, 0.56f, 0.60f);
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = null;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.74f, 0.78f, 0.74f);
        RenderSettings.fogDensity = 0.0028f;   // せまい シーンなので うすめ（0.008 は 白く かすんだ）

        // ---- 地めん（草）と 門の外の 道（土）
        var mGrass = MatT("NiwaGrassT", "grass_ground.png", 65f, 55f);
        var mDirt  = MatT("NiwaDirtT",  "dirt_path.png", 20f, 2f);
        // 地めんは 広く（浅い 追従カメラは 遠くまで 見える。せまいと 端の 空色が 見える）
        Box(root, "Jimen", new Vector3(0f, -0.25f, 4f), new Vector3(130f, 0.5f, 110f), mGrass);
        Box(root, "MichiSoto", new Vector3(0f, 0.011f, -9.5f), new Vector3(80f, 0.02f, 5.0f), mDirt)
            .GetComponent<Collider>().enabled = false;

        // ---- 家（megakit の ジェッティの 家。玄関は 南=庭がわ を 向く）
        var ie = new GameObject("Ie").transform;
        ie.SetParent(root, false);
        ie.position = new Vector3(0f, 0f, 10f);
        ie.rotation = Quaternion.Euler(0f, 180f, 0f);           // 部品の 手前(+Z)を 南へ
        BuildKitTown.JettyBody(ie);

        // ---- 塀と 門（Kenney。fence は 板が pivot から +Z*0.46 はなれて いる → 線に そろえる）
        const float FS = 2.5f;                                   // 塀の 縮尺（高さ 0.87m）
        void Fence(string piece, Vector3 linePos, float yaw) {
            var rot = Quaternion.Euler(0f, yaw, 0f);
            KenneyKit.Put(root, piece, linePos - rot * Vector3.forward * (0.46f * FS), yaw, FS);
        }
        // 南（門を まん中に）: z=-6。すきま |x|<1.25 が 門
        for (float x = -10f; x <= 10.01f; x += FS) {
            if (Mathf.Abs(x) < 1.3f) continue;
            Fence("fence_simple", new Vector3(x, 0f, -6f), 0f);
        }
        KenneyKit.Put(root, "fence_gate", new Vector3(0f, 0f, -6f - 0.46f * FS), 0f, FS);
        // 東西: x=±11.2、z -6..14
        for (float z = -4.5f; z <= 14.01f; z += FS) {
            Fence("fence_simple", new Vector3(-11.2f, 0f, z), 90f);
            Fence("fence_simple", new Vector3( 11.2f, 0f, z), 270f);
        }
        // 北（家の 両わきだけ）
        for (float x = -10f; x <= 10.01f; x += FS) {
            if (Mathf.Abs(x) < 4.6f) continue;                   // 家の うしろは 家が 壁
            Fence("fence_simple", new Vector3(x, 0f, 15.2f), 180f);
        }

        // ---- 見えない かべ（Kenney の 塀には あたりが 無い）
        Box(root, "BLK_S1", new Vector3(-6.15f, 1f, -6f), new Vector3(9.7f, 2f, 0.3f), null, false);
        Box(root, "BLK_S2", new Vector3( 6.15f, 1f, -6f), new Vector3(9.7f, 2f, 0.3f), null, false);
        Box(root, "BLK_E",  new Vector3( 11.2f, 1f, 4f),  new Vector3(0.3f, 2f, 21f), null, false);
        Box(root, "BLK_W",  new Vector3(-11.2f, 1f, 4f),  new Vector3(0.3f, 2f, 21f), null, false);
        Box(root, "BLK_N",  new Vector3(0f, 1f, 15.2f),   new Vector3(23f, 2f, 0.3f), null, false);
        // 道の 外がわ（散歩の はんい）
        Box(root, "BLK_Road", new Vector3(0f, 1f, -12.2f), new Vector3(80f, 2f, 0.3f), null, false);
        Box(root, "BLK_RoadE", new Vector3(30f, 1f, -9f), new Vector3(0.3f, 2f, 7f), null, false);
        Box(root, "BLK_RoadW", new Vector3(-30f, 1f, -9f), new Vector3(0.3f, 2f, 7f), null, false);

        // ---- 玄関→門の 飛び石、くつぬぎ石、鉢
        for (int i = 0; i < 6; i++)
            KenneyKit.Put(root, (i % 2 == 0) ? "path_stone" : "path_stoneCircle",
                new Vector3(Random.Range(-0.25f, 0.25f), 0.02f, 4.6f - i * 1.85f),
                Random.Range(-14f, 14f), 1.6f);
        KenneyKit.Put(root, "stone_smallFlatA", new Vector3(0f, 0.02f, 5.65f), 8f, 2.2f);
        KenneyKit.Put(root, "pot_large", new Vector3(2.6f, 0f, 5.4f), 30f, 2f);
        KenneyKit.Put(root, "pot_small", new Vector3(3.4f, 0f, 5.1f), 70f, 2f);

        // ---- 木（シンボルツリー＝セミの木。あたりは カプセルを 手で）
        void Ki(string piece, float x, float z, float s, float yaw = -1f) {
            KenneyKit.Put(root, piece, new Vector3(x, 0f, z), yaw < 0f ? Random.Range(0f, 360f) : yaw, s);
            var col = new GameObject("KiAtari");
            col.transform.SetParent(root, false);
            col.transform.position = new Vector3(x, 1f, z);
            var cap = col.AddComponent<CapsuleCollider>();
            cap.radius = 0.12f * s; cap.height = 2.5f;
        }
        Ki("tree_oak", -7.6f, 8.6f, 4.6f);                       // 庭の ぬし（セミの木）
        Ki("tree_default", 8.4f, 11.5f, 3.4f);
        Ki("tree_fat", -9.0f, -2.0f, 2.8f);
        // 門の外（道の 向こうがわ）に 木立ち。★門の正面と カメラの すじは あける
        foreach (var p in new[] { new Vector2(-26f, -13.6f), new Vector2(-18f, -14.2f),
                                  new Vector2(17f, -13.8f), new Vector2(23f, -14.4f), new Vector2(28f, -13.5f) })
            KenneyKit.Put(root, (Random.value < 0.5f) ? "tree_default" : "tree_detailed",
                new Vector3(p.x, 0f, p.y), Random.Range(0f, 360f), Random.Range(3.0f, 4.2f));
        // 竹（北西の かど・和の 気配）
        for (int i = 0; i < 5; i++)
            KenneyKit.Put(root, (i % 2 == 0) ? "crops_bambooStageB" : "crops_bambooStageA",
                new Vector3(-10.2f + Random.Range(-0.5f, 0.5f), 0f, 12.5f + i * 0.55f),
                Random.Range(0f, 360f), 4.2f);

        // ---- 草・花（塀ぎわ・木の 根もと・玄関わき）
        string[] kusa = { "grass", "grass_large", "grass_leafs", "grass_leafsLarge" };
        void KusaMure(float cx, float cz, float r, int n, float s0, float s1) {
            for (int i = 0; i < n; i++) {
                var d = Random.insideUnitCircle * r;
                KenneyKit.Put(root, kusa[Random.Range(0, kusa.Length)],
                    new Vector3(cx + d.x, 0.01f, cz + d.y), Random.Range(0f, 360f), Random.Range(s0, s1));
            }
        }
        for (float x = -10f; x <= 10f; x += 2.6f) KusaMure(x, -5.3f, 0.8f, 3, 1.4f, 2.2f);   // 南塀ぎわ
        for (float z = -4f; z <= 13f; z += 2.8f) {
            KusaMure(-10.5f, z, 0.8f, 3, 1.4f, 2.2f);
            KusaMure( 10.5f, z, 0.8f, 2, 1.4f, 2.0f);
        }
        KusaMure(-7.6f, 8.6f, 2.2f, 10, 1.5f, 2.4f);             // ぬしの木の 根もと
        KusaMure(8.4f, 11.5f, 1.6f, 6, 1.5f, 2.2f);
        KusaMure(0f, -9.5f, 14f, 16, 1.2f, 2.0f);                // 道ばた
        foreach (var f in new[] { "flower_redA", "flower_yellowA", "flower_purpleA" })
            for (int i = 0; i < 4; i++)
                KenneyKit.Put(root, f,
                    new Vector3(Random.Range(1.6f, 4.2f), 0.01f, Random.Range(3.2f, 5.2f)),
                    Random.Range(0f, 360f), 2f);
        KenneyKit.Put(root, "rock_smallA", new Vector3(-4.4f, 0f, 4.6f), 20f, 2f);
        KenneyKit.Put(root, "rock_smallB", new Vector3(6.2f, 0f, -4.2f), 200f, 2f);
        KenneyKit.Put(root, "log", new Vector3(9.2f, 0f, 6.5f), 75f, 2f);
        KenneyKit.Put(root, "mushroom_red", new Vector3(-8.6f, 0f, 7.2f), 0f, 1.6f);

        // ---- 遠景の 描き割り（絵はがき文法の 検証・2026-08-30 本人GO）：
        //   歩けないが 見える 遠景が「世界は 続いてる」を 作る。山なみ 2層＋入道雲。
        //   絵なので 影は 受けず 落とさず、昼夜の 明るさだけ 受ける（Lit・かげOFF）
        // ★描き割りは **Unlit**（本人 2026-08-30「もっときれいに」）。Lit で 光を 受けさせると
        //   写真の 色が 飛んで 白い もやに なる。昼夜は NiwaHaikeiIro が 色で つける
        Material MatE(string name, string tex) {
            string dir = "Assets/Art/Materials/Niwa";
            string path = dir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) {
                m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = Shader.Find("Universal Render Pipeline/Unlit");
            m.color = Color.white;
            m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/" + tex);
            return m;
        }
        GameObject Kakiwari(string name, Material m, Vector3 pos, float w, float h) {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(root, false);
            q.transform.position = pos;
            q.transform.rotation = Quaternion.Euler(0f, 0f, 0f);    // 南を 向く（カメラは 南から）
            q.transform.localScale = new Vector3(w, h, 1f);
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return q;
        }
        var mYamaToi = MatE("NiwaYamaToi", "yama_toi.png");
        var mYamaChikai = MatE("NiwaYamaChikai", "yama_chikai.png");
        var mKumo = MatE("NiwaKumo", "kumo_nyudo.png");
        // ★カメラ連動（NiwaKakiwari）：追従カメラでも 画面の 上の 帯に いつも 山が 出る。
        //   ずれの 数字は「ピッチ17°・FOV30」の 画角から 逆算（上端≒水平線）
        void KakiwariCam(string name, Material m, Vector3 zurashi, float w, float h) {
            var q = Kakiwari(name, m, Vector3.zero, w, h);
            q.AddComponent<NiwaKakiwari>().zurashi = zurashi;
        }
        // ★里山（本人 2026-08-30「田舎って山が近い。遠くの峰って感じではない」）：
        //   近い 山は 画面上端を つきぬける 高さ。谷間の くぼみから だけ 空と 遠い 峰が のぞく
        var mSatoyama = MatE("NiwaSatoyama", "satoyama.png");
        // 尾根線が 画面の 帯（水平線0〜+4°）を またぐ 高さ：高い ところは 山・低い ところは 空
        // 稜線（テクスチャ上から 24%）が 水平線+3°に 来る 高さ：+2.9 = 上端13.5 → 中心 -8.5
        KakiwariCam("Satoyama", mSatoyama, new Vector3(6f, -1.55f, 55f), 72.0f, 16.0f);
        KakiwariCam("Yama_Toi", mYamaToi,  new Vector3(-14f, 6.65f, 92f), 116.0f, 22.0f);      // 遠い 峰＝谷間の おく
        KakiwariCam("Kumo1", mKumo, new Vector3(-34f, 9.9f, 88f), 30f, 16f);                // 入道雲（写真の 比率）
        KakiwariCam("Kumo2", mKumo, new Vector3(14f, 10.9f, 89f), 40f, 21f);
        KakiwariCam("Kumo3", mKumo, new Vector3(44f, 9.1f, 87f), 24f, 13f);

        // ---- 主人公（マリサ 8方向スプライト・ライトを 受ける）
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 0.3f, -1.5f);
        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.0f; cc.radius = 0.26f; cc.center = new Vector3(0f, 0.52f, 0f);
        cc.slopeLimit = 50f; cc.stepOffset = 0.35f;
        var marisa = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sprites/marisa_8x8.png");
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Mi"; quad.transform.SetParent(player.transform, false);
        quad.transform.localPosition = new Vector3(0f, 0.66f, 0f);
        quad.transform.localScale = new Vector3(1.30f * 115f / 167f, 1.30f, 1f);
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        var sm = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        AssetDatabase.CreateAsset(sm, "Assets/Art/Materials/Niwa/NiwaMarisa.mat");
        sm.SetFloat("_AlphaClip", 1f); sm.SetFloat("_Cutoff", 0.5f);
        sm.EnableKeyword("_ALPHATEST_ON");
        sm.SetFloat("_Smoothness", 0.05f);
        sm.mainTexture = marisa;
        quad.GetComponent<Renderer>().sharedMaterial = sm;
        quad.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        quad.AddComponent<MuraBillboard>();
        var cs = player.AddComponent<CharSprite>();
        cs.target = quad.GetComponent<Renderer>();
        cs.runSpeed = 3.4f;
        var mv = player.AddComponent<MuraMove>();
        mv.sprite = cs;

        // ---- 撮影ツアーの たちば
        var tourNames = new[] { "にわ", "もんのそと" };
        var tourPos = new[] { new Vector3(0f, 0.3f, -1.5f), new Vector3(3f, 0.3f, -9.3f) };
        var tour = new Transform[tourPos.Length];
        for (int i = 0; i < tourPos.Length; i++) {
            var g = new GameObject("Mise_" + tourNames[i]);
            g.transform.SetParent(root, false); g.transform.position = tourPos[i];
            tour[i] = g.transform;
        }
        mv.tour = tour;

        // ---- カメラ（HD-2Dの 型＝望遠 FOV26・見下ろし 33度・固定 2台）
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.fieldOfView = 26f; cam.nearClipPlane = 0.3f; cam.farClipPlane = 300f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.70f, 0.80f, 0.88f);
        camGO.AddComponent<AudioListener>();
        var camData = camGO.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;
        camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;   // FXAA（HD-2D必須の 型）
        var fix = camGO.AddComponent<MuraCamFixed>();
        fix.target = player.transform;
        // ★初期は HD-2D追従（本人 2026-08-30「初期値はHD-2Dの方にして」。
        //   固定カットは ゾーン境界の カット往復＝ちらつきが 出ていた。Tで 切替は 残す）
        fix.hd2d = true;
        // ★ピッチは 浅く（本人 2026-08-30「背景の山が見えてない」）。32°だと 画面の 上端でも
        //   水平線より 18°下＝地面の 15m先までしか 映らず、遠景が 構造的に 出ない
        fix.hdPitch = 10f; fix.hdDist = 15f; fix.hdFov = 33f;   // 上端＝水平線+6.5°（山を 大きく 見せる）
        fix.spots = new[] {
            new MuraCamFixed.Spot {
                name = "にわ",
                area = new Bounds(new Vector3(0f, 3f, 4.5f), new Vector3(23f, 14f, 21.5f)),
                pos = new Vector3(0f, 11f, -18f),
                lookAt = new Vector3(0f, 4.2f, 5f), fov = 26f,    // 屋根の 上に 山なみが 抜ける 角度
            },
            new MuraCamFixed.Spot {
                name = "もんのそと",
                area = new Bounds(new Vector3(0f, 3f, -9f), new Vector3(60f, 14f, 6.5f)),
                // ★道の 近くに 置く：遠いと「今の カメラ（にわ）の ほうが 近い」に なって
                //   切り替わらない（nearest-visible の 罠・mura で 学んだ）
                pos = new Vector3(0f, 5.5f, -16.5f),
                lookAt = new Vector3(0f, 3.4f, -6.5f), fov = 32f,   // 門ごしの 家＋奥に 山と 雲
            },
        };
        fix.fallback = new MuraCamFixed.Spot {
            name = "ひき", pos = new Vector3(0f, 18f, -26f),
            lookAt = new Vector3(0f, 1f, 4f), fov = 30f,
        };
        var uiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/PixelMplus12-Regular.ttf");
        fix.font = uiFont;
        mv.cam = camGO.transform;

        // ---- 太陽と 1日（mura の 型を 流用。台形の 日照・夕焼け・月明かりの 夜）
        var sunGO = new GameObject("Sun");
        var sun = sunGO.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        var dayGO = new GameObject("Day");
        var md = dayGO.AddComponent<MuraDay>();
        md.sun = sun; md.font = uiFont;
        // 描き割りの 昼夜（Unlit なので 色で つける）
        var haikei = dayGO.AddComponent<NiwaHaikeiIro>();
        haikei.sun = sun;
        haikei.mats = new[] { mSatoyama, mYamaToi, mKumo };

        // 玄関わきの 舞台照明（HD-2D＝スポットの 型。夜に 効く）
        var porch = new GameObject("PorchLight");
        porch.transform.SetParent(root, false);
        porch.transform.position = new Vector3(0f, 2.6f, 6.2f);
        porch.transform.rotation = Quaternion.Euler(65f, 0f, 0f);
        var pl = porch.AddComponent<Light>();
        pl.type = LightType.Spot; pl.range = 7f; pl.spotAngle = 70f;
        pl.intensity = 1.6f; pl.color = new Color(1f, 0.85f, 0.6f);

        // ---- ポストFX（HD-2Dの 型＝深めDoF・Bloom・ビネット・トーン）
        var volGO = new GameObject("PostFX");
        var vol = volGO.AddComponent<UnityEngine.Rendering.Volume>();
        vol.isGlobal = true;
        var prof = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
        AssetDatabase.CreateAsset(prof, "Assets/Art/Materials/Niwa/NiwaPostFX.asset");
        T AddFX<T>() where T : UnityEngine.Rendering.VolumeComponent { return prof.Add<T>(); }
        var dof = AddFX<DepthOfField>();
        dof.mode.overrideState = true; dof.mode.value = DepthOfFieldMode.Bokeh;
        dof.focusDistance.overrideState = true; dof.focusDistance.value = 16f;
        dof.aperture.overrideState = true; dof.aperture.value = 3.2f;
        dof.focalLength.overrideState = true; dof.focalLength.value = 55f;
        var bloom = AddFX<Bloom>();
        bloom.threshold.overrideState = true; bloom.threshold.value = 1.0f;
        bloom.intensity.overrideState = true; bloom.intensity.value = 0.9f;
        bloom.tint.overrideState = true; bloom.tint.value = new Color(1f, 0.96f, 0.86f);
        var grade = AddFX<ColorAdjustments>();
        grade.postExposure.overrideState = true; grade.postExposure.value = 0.1f;
        grade.contrast.overrideState = true; grade.contrast.value = 10f;
        grade.saturation.overrideState = true; grade.saturation.value = 6f;
        var vig = AddFX<Vignette>();
        vig.intensity.overrideState = true; vig.intensity.value = 0.30f;
        vig.smoothness.overrideState = true; vig.smoothness.value = 0.46f;
        var tone = AddFX<Tonemapping>();
        tone.mode.overrideState = true; tone.mode.value = TonemappingMode.Neutral;
        vol.sharedProfile = prof;
        var focus = volGO.AddComponent<FocusOnPlayer>();
        focus.volume = vol; focus.target = player.transform;

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, "Assets/Scenes/Niwa.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("[Probe] BuildNiwa done");
    }
}
