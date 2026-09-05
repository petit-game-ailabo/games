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
    // ★地めんの 色（2026-09-03・本人「地面の色味が合ってなくて違和感」）。
    //   画面で 測ると 草だけ 明るさ 0.59・彩度 0.55、木の葉 0.33/0.33、家 0.31/0.15。
    //   草を 明るさ 0.45・彩度 0.40（色相は 葉と 同じ 80°）へ 寄せる 掛け算。地面の 材質 ぜんぶに かける
    public static readonly Color JIMEN_IRO = new Color(0.78f, 0.80f, 0.90f);
    // 土（道）は 青を のこすと 紫に 転ぶ ので 暖かい 掛け算に 分ける
    public static readonly Color TSUCHI_IRO = new Color(0.80f, 0.76f, 0.66f);

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
        // ★素材は 本人が 用意した 真上からの 写真（2026-08-31）。
        //   タイルの 大きさは **世界の 長さで** 決める（草3m・土2.25m）。
        //   焼いた 一枚絵（NiwaJimenE）も 同じ 大きさで 敷く ので、さかい目で 柄が つながる
        // ★あみの UVは すでに 世界の 長さ÷タイル（草3m・土2.25m）なので、
        //   材質がわの 倍率は **1** に する。箱の ころの 43.33倍を のこしたら
        //   かけ算に なって 1タイル7cmに なり、ミップで つぶれて 単色の 板に 見えた
        var mGrass = MatT("NiwaGrassT", "ji_kusa.jpg", 1f, 1f);
        var mDirt  = MatT("NiwaDirtT",  "ji_tsuchi.jpg", 1f, 1f);
        mGrass.SetColor("_BaseColor", JIMEN_IRO);
        mDirt.SetColor("_BaseColor", TSUCHI_IRO);
        // ---- 地めんは **凸凹の あみ**（箱では ない・本人 2026-08-31）
        //   ふせ角10°の カメラは 高さだけ 6倍に 拡大して 映す（奥ゆき1m=21px / 高さ1m=121px）。
        //   20cmの 起伏でも 24px 動く。歩く ところ（庭の 踏み跡・家・道・高台）は 平ら
        //   地めんは 広く（浅い 追従カメラは 遠くまで 見える。せまいと 端の 空色が 見える）
        System.IO.Directory.CreateDirectory("Assets/Art/Meshes");
        var jibanM = NiwaJimenE.Ami("NiwaJiban", 130f, 110f, new Vector2(0f, 4f), 0.5f, 3.0f);
        AssetDatabase.CreateAsset(jibanM, "Assets/Art/Meshes/NiwaJiban.asset");
        var jiban = new GameObject("Jimen");
        jiban.transform.SetParent(root, false);
        jiban.transform.position = new Vector3(0f, 0f, 4f);
        jiban.AddComponent<MeshFilter>().sharedMesh = jibanM;
        jiban.AddComponent<MeshRenderer>().sharedMaterial = mGrass;
        jiban.AddComponent<MeshCollider>().sharedMesh = jibanM;

        // 門の 外の 道。**うねった ふち**の 帯（箱だと まっすぐな 線が 出る）。
        // ふちの 式は 焼いた 一枚絵と 同じ ものを つかう ので、絵の ところ（±16m）と つながる
        var michiM = NiwaJimenE.MichiAmi(-40f, 44f, 0.5f, 2.25f);
        AssetDatabase.CreateAsset(michiM, "Assets/Art/Meshes/NiwaMichi.asset");
        var michiGO = new GameObject("MichiSoto");
        michiGO.transform.SetParent(root, false);
        michiGO.transform.position = new Vector3(2f, 0f, 0f);
        michiGO.AddComponent<MeshFilter>().sharedMesh = michiM;
        michiGO.AddComponent<MeshRenderer>().sharedMaterial = mDirt;

        // ---- 家（megakit の ジェッティの 家。玄関は 南=庭がわ を 向く）
        var ie = new GameObject("Ie").transform;
        ie.SetParent(root, false);
        // ★母屋＝**ぼくなつ1の 空野家**に 合わせた ふつうの 民家（10.8 x 7.2m の 平屋）。
        //   NiwaIe が 南（-Z）を 向いて 組む ので まわさない。
        //   ガラス戸の 面が z=4 に 来る 位置に 置く（棟が 画面に 入る 距離を かせぐ）
        ie.position = new Vector3(0f, NiwaJimenE.NH, 4f - NiwaIe.MINAMI);   // 段の 上（D-187）
        NiwaIe.Build(ie);

        // ---- 屋敷の 囲い（2026-09-04・D-187〜D-191）：南は 石垣＋斜めの 坂（門も 柵も 無し）、
        //   東西北は 刈りこんだ 生垣（塊）。四ツ目垣は 庭の 中の 仕切りに 1本だけ
        {
            System.Func<float, float, float> jy = (x, z) => NiwaJimenE.Takasa(x, z);
            var naka = new Vector3(0f, 0f, 4f);                         // 庭の 中（外向きを 決める）
            float SW = NiwaJimenE.SAKA_HABA + 0.38f;   // 板の 段（|x| 1.5→1.9）の 外に 立てる
            float zSakaMoto = NiwaJimenE.SAKA_Z0 + 1.75f;               // 坂が 0.3m に なる ところ
            // ★石垣は 左右 それぞれ **1本の 折れ線**（坂脇→角→南）。角は 留め継ぎ（D-191）
            foreach (float sgn in new[] { -1f, 1f }) {
                const float ZK = -6.34f;                                  // 板の 段（z -6.25→-6.0）の 外
                var kado = new List<Vector3> {
                    new Vector3(sgn * SW, 0f, zSakaMoto),
                    new Vector3(sgn * SW, 0f, ZK),
                    new Vector3(sgn * 10.6f, 0f, ZK),
                };
                var pts = TakeV1.Kizamu(kado, 0.5f);
                var lo = new List<float>(); var hi = new List<float>();
                foreach (var p in pts) {
                    // 外がわ（庭から 遠い ほう）の 地めんに 下を そろえ、内がわ（坂か 庭）の 面に 天端を そろえる
                    var toOut = p - naka; toOut.y = 0f; toOut.Normalize();
                    bool yoko = Mathf.Abs(p.z - ZK) > 0.01f;            // 坂脇の 部分
                    Vector3 soto = yoko ? new Vector3(sgn, 0f, 0f) : Vector3.back;
                    Vector3 uchi = -soto;
                    var po = p + soto * 0.5f; var pu = p + uchi * 0.75f;   // 内は 段の 上（板の 面）まで 入って 測る
                    lo.Add(Mathf.Min(jy(po.x, po.z), jy(p.x, p.z)) - 0.3f);
                    hi.Add(jy(pu.x, pu.z) + 0.02f);
                }
                TakeV1.Ishigaki(root, pts, lo, hi, naka);
            }
            // 東西北の 生垣＝**1本の 折れ線で コの字**（2026-09-05・本人「生け垣が家の後ろに
            //   配置されていない」）。前は 家の うしろ（x -5.8..5.8）が 空いて いた。
            //   ★北の 線を z=13.5 の ままに すると 家の 北の 壁（z=13.0）に めりこむ
            //     （生垣の 厚み 0.9＝13.05〜13.95）。z=14.4 へ 下げ、段の 平場も
            //     NiwaJimenE.Dan で 15.4 まで 広げた。角は 折れ線なので 留め継ぎに なる
            // ★西の 線だけ 0.35m 外へ（2026-09-05）。生垣は 線から **0.80m** ふくらむ
            //   （芯の 半分 0.44 ＋ 毛の シェル 0.24 ＋ 横ゆらぎ 0.12）ので、-9.7 のままだと
            //   納屋の 壁を 抜けて 中に 葉が 入って いた
            TakeV1.Ikegaki(root, TakeV1.Kizamu(new List<Vector3> {
                new Vector3(-10.05f, 0f, -5.7f), new Vector3(-10.05f, 0f, 14.4f),
                new Vector3(9.7f, 0f, 14.4f), new Vector3(9.7f, 0f, -5.7f) }, 0.2f),
                1.7f, 0.9f, naka, jy);
            // ★四ツ目垣は やめた（2026-09-05・本人「家の左側に木の柵みたいなものがあるけど、
            //   これはどういったもの？ここにあることは正解？」）。四ツ目垣は 庭の 中の 仕切り
            //   （前庭と 裏庭を 分ける）なので、区切る ものが 無い ところに 1本だけ 立って いても
            //   意味が 読めない。その 場所は 納屋に した
        }

        // ---- 見えない かべ（Kenney の 塀には あたりが 無い）
        Box(root, "BLK_S1", new Vector3(-5.85f, 1f, -6f), new Vector3(8.1f, 2f, 0.3f), null, false);
        Box(root, "BLK_S2", new Vector3( 5.85f, 1f, -6f), new Vector3(8.1f, 2f, 0.3f), null, false);
        // ★かべは **生垣の 線では なく 内がわの 面**に 置く（2026-09-05・本人「生け垣に体がめり込む」）。
        //   生垣は 線から 0.80m ふくらむ（D-209）ので、線に かべを 置くと 葉の 中まで 入れて しまう
        Box(root, "BLK_E",  new Vector3( 8.90f, 1f, 3.5f),  new Vector3(0.3f, 2f, 20.2f), null, false);
        Box(root, "BLK_W",  new Vector3(-9.25f, 1f, 3.5f),  new Vector3(0.3f, 2f, 20.2f), null, false);
        Box(root, "BLK_N",  new Vector3(0f, 1f, 13.60f),   new Vector3(20f, 2f, 0.3f), null, false);
        // ★家の 真裏と 生垣の あいだ（0.6m）には 入れない（本人 2026-09-05）。
        //   通り抜けられない すきまに 入れると、出られなく なった ように 見える
        Box(root, "BLK_IeUra", new Vector3(0f, 1f, 12.85f), new Vector3(13.6f, 2f, 0.3f), null, false);
        // 道の 外がわ（散歩の はんい）
        Box(root, "BLK_Road", new Vector3(0f, 1f, -12.2f), new Vector3(80f, 2f, 0.3f), null, false);
        Box(root, "BLK_RoadE", new Vector3(39f, 1f, -9f), new Vector3(0.3f, 2f, 9f), null, false);
        Box(root, "BLK_RoadW", new Vector3(-30f, 1f, -9f), new Vector3(0.3f, 2f, 7f), null, false);

        // ---- 玄関→門の 飛び石、くつぬぎ石、鉢
        for (int i = 0; i < 10; i++)
            KenneyKit.Put(root, (i % 2 == 0) ? "path_stone" : "path_stoneCircle",
                // 門(x=0)から 玄関(x=3.15)へ ゆるく 東へ 寄せる
                new Vector3(NiwaIe.GENKAN_X * (1f - i / 9f) + Random.Range(-0.2f, 0.2f),
                            0.02f, 2.0f - i * 1.15f),
                Random.Range(-14f, 14f), 1.6f);
        // 鉢は 消した（本人 2026-09-03「鉢もいらない」）

        // ---- 木（本人 2026-08-31「3Dで作った方の木で、葉っぱとかも作りこんでたやつを
        //   大量に配置してみてほしい」）。チューブ木v5＝KiV5（BuildMura から 取りだした）。
        //   ★根もとは 凸凹の 地ばんに あわせる
        var hayashi = new KiV5.Hayashi(root);
        void Ki(float x, float z, float h, float futosa) {
            float y = NiwaJimenE.Takasa(x, z);
            // ★本人 2026-08-31「高台のところは奥側の木を減らそう、奥の背景が見えない。
            //   山の頂上は木が少ない、みたいなイメージで」
            if (y > 0.9f) return;                                   // 高みは 木が まばら
            // 高台から 北を 見た ときの 眺めを あける（背景の 山と 空を 見せる）
            //   ★塀の そとから（x>12）。7 から に したら 庭の 東の 木まで 消えた
            if (x > 11f && x < 36f && z > 4f && z < 24f && Random.value > 0.14f) return;
            // ★竹やぶの 中には 木を 植えない（2026-09-05）。竹は 地下茎で 場所を 取るので
            //   竹やぶと 雑木は くっきり 分かれる。混ぜると どちらにも 見えない
            float bx1 = (x + 14.0f) / 8.0f, bz1 = (z - 18.5f) / 6.0f;
            if (bx1 * bx1 + bz1 * bz1 < 1f) return;
            float bx2 = (x + 6.4f) / 5.2f, bz2 = (z - 20.5f) / 4.2f;
            if (bx2 * bx2 + bz2 * bz2 < 1f) return;
            hayashi.Ueru(x, y, z, h, futosa);
        }
        // ★高さは **画角に 入る 上限**から 決める（2026-08-31）。
        //   ふせ角10°・FOV33 → 画面の 上端は 水平線+6.5°。カメラの 目線は 3.30m。
        //   距離 d で 葉が 画面に 入る 高さ h < 3.30 + 0.114*d。
        //   はじめ 7〜13m で 植えたら **葉が ぜんぶ フレームの 外**＝幹だけの 林に なった
        // ★本人 2026-08-31「手前からのカメラに固定…キャラより奥側の木はもっとあってもいい。
        //   逆にキャラより手前は最小限。高台とか青空の画像を見せたいから手前の木は無くていい」
        //   → カメラは いつも 南から 北を 見る。**奥（北）は 厚く、手前（南）は ほんの 数本**
        // ★庭の 木は **1本だけ**（2026-09-05・本人「庭に木が、しかも離れたところに生えてる。
        //   これって普通？自分のイメージだと木は一本ぐらいだし、何かしら季節性のあるものを
        //   植えるイメージ、梅とか桜とか」）。
        //   調べ：田舎の 庭は「主木(しゅぼく) 1本 ＋ まわりの 屋敷林」。主木は 梅・柿・松が 多く、
        //   南〜南西に 植えて 夏の 日ざしを さえぎる。3本を 離して 植えて いたのは 林の 置きかた。
        //   → 母屋の 南西に 1本。低くて 幹が 太い＝年を とった 梅の 姿に する
        Ki(-3.25f, 0.45f, 5.4f, 0.52f);               // 庭の 主木（セミの木）
        // 西の 塀の そと（庭を 木立ちで はさむ）
        for (int i = 0; i < 13; i++) {
            if (Random.value < 0.15f) continue;
            Ki(-12f - Random.Range(0f, 10f), -5f + i * 2.0f + Random.Range(-1.2f, 1.2f),
               Random.Range(5.5f, 7.2f), Random.Range(0.26f, 0.40f));
        }
        // 家の うしろ（北）＝林の ふち。★等間かくは 柵に 見える ので 抜けを つくって かたまらせる
        for (int i = 0; i < 40; i++) {
            if (Random.value < 0.12f) continue;
            Ki(-38f + i * 2.1f + Random.Range(-1.7f, 1.7f), 17f + Random.Range(0f, 8f),
               Random.Range(6.6f, 8.1f), Random.Range(0.30f, 0.46f));
        }
        for (int i = 0; i < 32; i++) {
            if (Random.value < 0.12f) continue;
            Ki(-42f + i * 2.8f + Random.Range(-2f, 2f), 27f + Random.Range(0f, 12f),
               Random.Range(7.5f, 9.3f), Random.Range(0.30f, 0.46f));
        }
        // 東の おく（高台の むこう）
        for (int i = 0; i < 10; i++)
            Ki(38f + Random.Range(0f, 12f), -12f + i * 3.4f + Random.Range(-2f, 2f),
               Random.Range(6.5f, 8.5f), Random.Range(0.28f, 0.44f));
        // 手前（南）＝**最小限**。左右の はしだけ。門の 正面と 高台の 見あげは あける
        foreach (float hx in new[] { -30f, -24f, 25f, 31f })
            Ki(hx + Random.Range(-1.5f, 1.5f), -16f - Random.Range(0f, 5f),
               Random.Range(6.0f, 7.6f), Random.Range(0.28f, 0.42f));

        // 竹（2026-09-05・本人「竹ってどの位置に生えてるのが正しい？イメージ的には裏山みたいな
        //   ところに大量に生えてるイメージ。今は庭の端っこだけどここはあってる？」）。
        //   調べ：モウソウチクは 江戸期に 植えられた **人が 植えた やぶ**。タケノコと 材を 取る ため
        //   母屋の **裏（北〜北西）の 傾斜地**に まとめて 作る。地下茎で 広がる ので 庭の 中には
        //   置かない（庭に 1株だけ 生える ことは 無い）。防風林も かねる。
        //   → 庭の かどの 1かたまりは やめ、**生垣の そと・家の 裏**に 大きな やぶを 2つ
        //   ★本数は **多いほうに 振る**（本人「大量に生えてるイメージ」）。放置竹林は
        //     1平方mに 1本 前後。62本/75平方m で 撮ったら 遠目に 林と 見分けが つかなかった
        TakeV1.Mure(root, -14.0f, 18.5f, 6.5f, 4.5f, 112, (x, z) => NiwaJimenE.Takasa(x, z));
        TakeV1.Mure(root, -6.4f, 20.5f, 4.0f, 3.0f, 46, (x, z) => NiwaJimenE.Takasa(x, z));

        // ---- 草・花（塀ぎわ・木の 根もと・玄関わき）
        string[] kusa = { "grass", "grass_large", "grass_leafs", "grass_leafsLarge" };
        // Kusa1: photo grass card (kusa_kabu.png) when the picture exists, else the low-poly prop
        void Kusa1(Vector3 at, float yaw, float scale) {
            // ★地めんの 草むらは 置かない（2026-09-04・本人「地面に生えてるちっちゃい草無くして」）。
            //   黄色い 房が 点々と 散って 目ざわりだった。芝の 絵と 生垣・竹藪だけで 足りる
        }
        void KusaMure(float cx, float cz, float r, int n, float s0, float s1) {
            for (int i = 0; i < n; i++) {
                var d = Random.insideUnitCircle * r;
                Kusa1(new Vector3(cx + d.x, 0.01f, cz + d.y), Random.Range(0f, 360f), Random.Range(s0, s1));
            }
        }
        for (float x = -10f; x <= 10f; x += 2.6f) KusaMure(x, -5.3f, 0.8f, 3, 1.4f, 2.2f);   // 南塀ぎわ
        for (float z = -4f; z <= 13f; z += 2.8f) {
            KusaMure(-9.0f, z, 0.6f, 3, 1.4f, 2.2f);    // 生垣の 内がわ（外は 段の 斜面で 浮いた）
            KusaMure( 9.0f, z, 0.6f, 2, 1.4f, 2.0f);
        }
        KusaMure(-7.6f, 8.6f, 2.2f, 10, 1.5f, 2.4f);             // ぬしの木の 根もと
        KusaMure(8.4f, 11.5f, 1.6f, 6, 1.5f, 2.2f);
        KusaMure(0f, -9.5f, 14f, 16, 1.2f, 2.0f);                // 道ばた
        // ★玄関の 前の チューリップは 消した（本人 2026-09-02「チューリップが埋まってるので消しておいて」）。
        //   地めんの 起伏に 半分 うまって いた うえ、古い 家に 花壇は 似あわない
        // 岩と 丸太は TakeV1（写真の 皮・岩はだの 絵）。キノコは 消した（本人 2026-09-03）
        {
            var mono = new TakeV1.Yabu(root);
            // ★岩は **見えて いる 地面板（当たり＋0.05）の 上**に 置く（2026-09-05）。
            //   当たりの 高さに 置いて いた ので 芝生に 黒い 三日月が 落ちて いるだけに 見えた
            TakeV1.Iwa(root, new Vector3(-4.4f, NiwaJimenE.Takasa(-4.4f, 4.6f) + 0.10f, 4.6f), 0.9f, 20f);
            TakeV1.Iwa(root, new Vector3(6.2f, NiwaJimenE.Takasa(6.2f, -4.2f) + 0.10f, -4.2f), 0.7f, 200f);
            TakeV1.Maruta(mono, new Vector3(9.2f, NiwaJimenE.Takasa(9.2f, 6.5f), 6.5f), 75f, 1.7f, 0.17f);
            mono.Katameru();
        }

        // ---- 納屋（庭の 西）と 水まわり（家の 東）。2026-09-05・本人の 指示。
        //   どちらも **地めんの 高さを 自分で 決めて** 置く ので、下の すわらせ直しからは 外す
        var nayaT = NiwaNaya.Build(root);
        NiwaMizu.Build(root);

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
        // ★カメラ連動（NiwaKakiwari）：追従カメラでも 画面の 上の 帯に いつも 山が 出る。
        //   ずれの 数字は「ピッチ17°・FOV30」の 画角から 逆算（上端≒水平線）
        void Toumei(Material m, int queue) {
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
            m.SetFloat("_AlphaClip", 0f); m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            // ★描き割りは **不とうめいより 先に** 描く（2026-08-31・本人
            //   「高台辺りで、奥の背景の山が、木より手前に表示されて、木が見えなくなる」）。
            //   透明の 列（2500〜）に 置くと 不とうめいの あとに 描かれ、深度で 判定される。
            //   板は カメラの 55m前に 追従する ので、いちばん 奥の 木(z=41)は
            //   カメラz < -14＝主人公z < 0.77 の あいだ ずっと 山に 隠れて いた。
            //   2000より 小さい 列に すれば 深度は まだ 空っぽで、あとから 来る 地面や 木が
            //   かならず 上に 乗る＝**背景の 本来の 描きかた**（距離に 左右されない）
            m.renderQueue = queue;     // 空1000 → 雲1100 → 山1200 の 順に かさねる
        }

        GameObject KakiwariCam(string name, Material m, Vector3 zurashi, float w, float h) {
            var q = Kakiwari(name, m, Vector3.zero, w, h);
            q.AddComponent<NiwaKakiwari>().zurashi = zurashi;
            return q;
        }
        // ★里山（本人 2026-08-30「田舎って山が近い。遠くの峰って感じではない」）：
        //   近い 山は 画面上端を つきぬける 高さ。谷間の くぼみから だけ 空と 遠い 峰が のぞく
        // ★背景の 絵は 本人が 用意（2026-08-30）：山＝重なる 稜線（緑の 近景＋青い 遠景）、
        //   空＝入道雲の 空。空は いちばん おくの 幕、山は その 手前、雲は あいだを ながれる
        var mSora = MatE("NiwaSora", "sora.png");
        mSora.SetFloat("_AlphaClip", 0f); mSora.DisableKeyword("_ALPHATEST_ON");
        mSora.renderQueue = 1000;                       // いちばん おく（不とうめいの まま）。
        // ★1900の ままだと 雲(1100)より **あとに** 描かれて 雲を 消す。
        //   空は 深度を 書く ので、雲と 山(どちらも 手前・深度は 書かない)は 通る
        KakiwariCam("Sora", mSora, new Vector3(0f, 44f, 200f), 320f, 120f);

        var mSatoyama = MatE("NiwaSatoyama", "satoyama.png");
        Toumei(mSatoyama, 1200);
        Toumei(mYamaToi, 1150);
        KakiwariCam("Satoyama", mSatoyama, new Vector3(0f, 10.01f, 55f), 96.0f, 36.0f);

        // 流れる 雲（空の 絵から 抜いた 3つ）。山の おく・空の 手前
        var kumoTex = new[] { "kumo_a.png", "kumo_b.png", "kumo_c.png" };
        var mKumos = new Material[kumoTex.Length];
        for (int i = 0; i < kumoTex.Length; i++) {
            mKumos[i] = MatE("NiwaKumo" + (i + 1), kumoTex[i]);
            Toumei(mKumos[i], 1100);                    // 空(1000)と 山(1200)の あいだ
        }
        //  （よこ位置, 見上げ角, 奥ゆき, はば, 絵, ながれる 速さ）
        var kumoSet = new[] {
            new [] { -30f,  9.4f, 150f, 60f, 0f, 0.24f },
            new [] {  60f,  11.8f, 165f, 48f, 1f, 0.32f },
            new [] {-120f,  8.4f, 140f, 40f, 2f, 0.40f },
            new [] { 140f, 14.2f, 175f, 66f, 0f, 0.18f },
            new [] { -60f, 16.6f, 185f, 52f, 1f, 0.26f },
        };
        foreach (var k in kumoSet) {
            var mat = mKumos[(int)k[4]];
            var tex = mat.mainTexture;
            float w = k[3], h = w * tex.height / (float)tex.width;
            var q = Kakiwari("Kumo", mat, Vector3.zero, w, h);
            Object.DestroyImmediate(q.GetComponent<NiwaKakiwari>());
            var km = q.AddComponent<NiwaKumo>();
            km.zurashi = new Vector3(k[0], k[2] * Mathf.Tan(k[1] * Mathf.Deg2Rad), k[2]);
            km.hayasa = k[5]; km.haba = 460f;
        }

        // ---- 高台（本人 2026-08-31「先に高台の箱を直しておいて」）。
        //   箱を やめて **地ばんの 高さの 一部**に した（NiwaJimenE の Takadai）。
        //   箱だと 垂直な 壁と まっすぐな 天＝画面で いちばん 目立つ 直線に なる。
        //   地ばんに して しまえば 地面の あみ・物の すわり・接地の 影・足もとの 影の
        //   落ちる さきが ぜんぶ ついてくる。坂も 別に 作らない（ふち全体が 21°の 土手）。
        //   ★道（z -12.4〜-7）に かからない よう 北へ ずらした（道が 土手を 登って しまう）
        {
            float TX = NiwaJimenE.TX, TZ = NiwaJimenE.TZ, TH = NiwaJimenE.TH;
            // 上の かざり（草・岩・丸太）。すわらせ直しで 地ばんの 高さに のる
            for (int i2 = 0; i2 < 16; i2++) {
                var d = Random.insideUnitCircle * 3.6f;
                Kusa1(new Vector3(TX + d.x, 0f, TZ + d.y), Random.Range(0f, 360f), Random.Range(1.4f, 2.2f));
            }
            {
                var mono2 = new TakeV1.Yabu(root);
                TakeV1.Iwa(root, new Vector3(TX + 2.6f, NiwaJimenE.Takasa(TX + 2.6f, TZ + 2.0f) + 0.10f, TZ + 2.0f), 1.1f, 40f);
                TakeV1.Maruta(mono2, new Vector3(TX - 1.4f, NiwaJimenE.Takasa(TX - 1.4f, TZ - 1.8f), TZ - 1.8f), 15f, 1.9f, 0.19f);
                mono2.Katameru();
            }
            // 土手の ふちにも 草（切り口を やわらげる）
            for (int i2 = 0; i2 < 22; i2++) {
                float a = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float r = Random.Range(0.62f, 0.95f);
                Kusa1(new Vector3(TX + Mathf.Cos(a) * 7.5f * r, 0f, TZ + Mathf.Sin(a) * 6.0f * r),
                      Random.Range(0f, 360f), Random.Range(1.3f, 2.0f));
            }
            // 東の あそび場を 囲う（絵はがきの 外へ 出られない ように）
            Box(root, "BLK_HigashiN", new Vector3(24f, 3f, 20f), new Vector3(28f, 8f, 0.3f), null, false);
            Box(root, "BLK_HigashiE", new Vector3(39f, 3f, 10f), new Vector3(0.3f, 8f, 30f), null, false);
        }

        // ---- 主人公（マリサ 8方向スプライト・ライトを 受ける）
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0f, NiwaJimenE.NH + 0.3f, -1.5f);
        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.0f; cc.radius = 0.26f; cc.center = new Vector3(0f, 0.52f, 0f);
        cc.slopeLimit = 50f; cc.stepOffset = 0.35f;
        // ★既定は **手描きの 2D**（D-224）。3Dは `-3d` で 見くらべる ときだけ。
        //   材質に 入れる 絵も こちらに して おく（NiwaKae は 起動時に 上書きする だけ なので、
        //   ここが 2Dの ままだと 一瞬 前の 絵が 出る）
        var marisa = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sprites/marisa_walk.png");
        var marisa3d = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sprites/marisa_hybrid.png");
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Mi"; quad.transform.SetParent(player.transform, false);
        quad.transform.localPosition = new Vector3(0f, 0.66f, 0f);
        quad.transform.localScale = new Vector3(1.40f * 224f / 336f, 1.40f, 1f);
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
        // ★既定＝Meshy の 3D。`-kyu2d` で 前の 手描き 2Dに もどせる（見くらべ用）
        var kae = player.AddComponent<NiwaKae>();
        kae.target = quad.GetComponent<Renderer>();
        kae.futsu = marisa;
        kae.meshy = marisa3d;
        var cs = player.AddComponent<CharSprite>();
        cs.target = quad.GetComponent<Renderer>();
        cs.runSpeed = 3.4f;
        cs.walkSheet = true;            // 行0..5=走り / 6=立ち / 7=目とじ（2026-08-30）
        cs.Cols = 8; cs.Rows = 10;      // 8方向(列) x 走り8コマ＋立ち＋目とじ(行)
        cs.cycleFrames = 8;             // 行0..7＝走り
        cs.idleCol = -1;                // 止まっても **向きは そのまま**
        cs.idleRow = 8; cs.blinkRow = 9;
        cs.walkCycleFps = 9f; cs.runCycleFps = 14f;
        // ★歩きは 走りの スロー再生 では なく **べつの 絵**（D-225）
        cs.arukiTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sprites/marisa_aruki.png");
        cs.arukiRows = 8;
        // ---- 足もとの 影（絵の 板は 影を 落とせない ので 別に 敷く）
        var kageGO = new GameObject("KageAshi");
        kageGO.transform.SetParent(root, false);
        kageGO.AddComponent<MeshFilter>().sharedMesh = NiwaJimen.Ita(0.50f, 0.34f, 0.22f, 0.50f);
        var kmr = kageGO.AddComponent<MeshRenderer>();
        kmr.sharedMaterial = NiwaJimen.KageMat();
        kmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        kmr.receiveShadows = false;
        kageGO.AddComponent<NiwaKageAshi>().target = player.transform;

        var mv = player.AddComponent<MuraMove>();
        mv.sprite = cs;

        // ---- 撮影ツアーの たちば
        var tourNames = new[] { "にわ", "もんのそと", "たかだい", "にわのにし" };
        var tourPos = new[] { new Vector3(0f, NiwaJimenE.NH + 0.3f, -1.5f), new Vector3(3f, 0.3f, -9.3f), new Vector3(NiwaJimenE.TX, NiwaJimenE.TH + 0.6f, NiwaJimenE.TZ),
                                new Vector3(-7.4f, NiwaJimenE.NH + 0.5f, 6f) };   // ikegaki no naka ni hairanai you 2.3m uchigawa
        var tour = new Transform[tourPos.Length];
        for (int i = 0; i < tourPos.Length; i++) {
            var g = new GameObject("Mise_" + tourNames[i]);
            g.transform.SetParent(root, false); g.transform.position = tourPos[i];
            tour[i] = g.transform;
        }
        mv.tour = tour;
        // のぼれるかの 機械検査（-noboru）：南の ふもとから 北へ 歩いて 高台に 上がれるか
        mv.noboruKara = new Vector3(NiwaJimenE.TX, 1.5f, NiwaJimenE.TZ - 12f);
        mv.noboruMade = NiwaJimenE.TH;

        // ---- カメラ（HD-2Dの 型＝望遠 FOV26・見下ろし 33度・固定 2台）
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.fieldOfView = 26f; cam.nearClipPlane = 0.3f; cam.farClipPlane = 300f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.667f, 0.902f, 0.980f);   // 空の 絵の 地平ぎわの 色
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
                name = "たかだい",
                area = new Bounds(new Vector3(NiwaJimenE.TX, 6f, NiwaJimenE.TZ),
                                  new Vector3(13f, 12f, 11f)),
                pos = new Vector3(NiwaJimenE.TX, 9.5f, NiwaJimenE.TZ - 15f),
                lookAt = new Vector3(NiwaJimenE.TX, 7.5f, NiwaJimenE.TZ), fov = 34f,
                hdPitchOver = -6f, hdDistOver = 13f,      // ★空を 見上げる
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
        haikei.mats = new[] { mSora, mSatoyama, mYamaToi, mKumos[0], mKumos[1], mKumos[2] };

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
        // ★Volumeの 効果は **サブアセットとして 保存**しないと ビルドで まるごと 消える
        //   （BuildZashiki は 直して あるのに 庭は 直し わすれて いた）。実機では 被写界深度も
        //   ブルームも ヴィネットも 色調整も **1つも 効いて いなかった**（2026-09-05・
        //   NiwaNayaNaka の TryGet が 両方 false を 返した のが 発見の きっかけ）
        T AddFX<T>() where T : UnityEngine.Rendering.VolumeComponent {
            var c = prof.Add<T>(true);
            c.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(c, prof);
            return c;
        }
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

        // ---- 納屋の 屋内カメラ（2026-09-05）。カメラと ポストFXが できて から つなぐ
        {
            var naka = nayaT.GetComponent<NiwaNayaNaka>();
            if (naka != null) {
                naka.target = player.transform;
                naka.cam = camGO.transform;
                naka.fix = fix;
                naka.vol = vol;
            } else Debug.LogError("[BuildNiwa] NiwaNayaNaka が 納屋に ついて いない");
        }

        Debug.Log("[Probe] Takadai いちばん急な傾斜 " + NiwaJimenE.TakadaiKeisha());
        hayashi.Katameru();

        // ---- 虫（見せかたの 段・2026-09-02・D-171）。絵は Codex の 画像を 切りぬいた もの。
        //   幹の 位置は hayashi.Moto から わたす（名まえで 場面を 探させない）
        {
            var mushiGO = new GameObject("Mushi");
            mushiGO.transform.SetParent(root, false);
            var mu = mushiGO.AddComponent<NiwaMushi>();
            for (int i = 0; i < hayashi.Suji.Count; i++)
                mu.miki.Add(new NiwaMushi.Miki { pts = hayashi.Suji[i], rad = hayashi.Futo[i] });
            mu.font = uiFont;
            // ★建てものの 中には 湧かない・入らない（2026-09-05・本人「納屋の中、虫が入ってくる」）。
            //   足もとの 箱を わたす。虫の 配置そのものの 作りこみは あと（PLAN）
            System.Action<Transform, float> Yoke = (o, nobi) => {
                if (o == null) return;
                Bounds bb = default; bool ar = false;
                foreach (var r in o.GetComponentsInChildren<Renderer>()) {
                    if (r == null || !r.enabled) continue;
                    if (!ar) { bb = r.bounds; ar = true; } else bb.Encapsulate(r.bounds);
                }
                if (!ar) return;
                bb.Expand(new Vector3(nobi * 2f, 0f, nobi * 2f));
                mu.yoke.Add(bb);
            };
            Yoke(ie, 1.1f);
            Yoke(nayaT, 0.8f);
            {   // 足もとの 影の 材質（接地影と 同じ Niwa/Kage）
                string kp = "Assets/Art/Materials/Niwa/MushiKage.mat";
                var km = AssetDatabase.LoadAssetAtPath<Material>(kp);
                if (km == null) { km = new Material(Shader.Find("Niwa/Kage")); AssetDatabase.CreateAsset(km, kp); }
                km.shader = Shader.Find("Niwa/Kage");
                mu.kageZairyo = km;
            }
            int e_ari = 0;
            foreach (var e in NiwaMushi.Shurui()) {
                string d = "Assets/Art/Sprites/mushi/" + e.id;
                e.yoko = AssetDatabase.LoadAssetAtPath<Texture2D>(d + "_yoko.png");
                e.ue = AssetDatabase.LoadAssetAtPath<Texture2D>(d + "_ue.png");
                e.naname = AssetDatabase.LoadAssetAtPath<Texture2D>(d + "_naname.png");
                if (e.yoko != null || e.ue != null) e_ari++;
                // 材質は ここで 作る（主人公 NiwaMarisa.mat と 同じ：Lit＋アルファ抜き・両面）
                Material MushiMat(string nm, Texture2D tex) {
                    if (tex == null) return null;
                    string mp = "Assets/Art/Materials/Niwa/Mushi_" + nm + ".mat";
                    var mm = AssetDatabase.LoadAssetAtPath<Material>(mp);
                    if (mm == null) { mm = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mm, mp); }
                    mm.shader = Shader.Find("Universal Render Pipeline/Lit");
                    mm.SetFloat("_AlphaClip", 1f); mm.SetFloat("_Cutoff", 0.45f);
                    mm.EnableKeyword("_ALPHATEST_ON");
                    mm.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
                    mm.SetFloat("_Smoothness", 0.08f);
                    mm.mainTexture = tex; mm.SetTexture("_BaseMap", tex);
                    return mm;
                }
                e.zairyo = MushiMat(e.id, e.Sekai);
                // 飛ぶ 虫の 横の 絵（真横に 進む とき）
                if (e.perch == NiwaMushi.Perch.Sora && e.yoko != null) e.zairyoYoko = MushiMat(e.id + "_yoko", e.yoko);
                mu.shu.Add(e);
            }
            Debug.Log("[Probe] NiwaMushi 種 " + e_ari + "/" + mu.shu.Count + " みき " + mu.miki.Count
                      + " よけ " + mu.yoke.Count);

            // ---- 道具（虫とりの ひとそろい）と めにゅー
            var dg = root.GetComponentInChildren<NiwaDougu>();
            var menuGO = new GameObject("Menu");
            menuGO.transform.SetParent(root, false);
            var mn = menuGO.AddComponent<NiwaMenu>();
            mn.dougu = dg; mn.mushi = mu; mn.mv = mv; mn.font = uiFont;
            if (dg != null) {
                dg.player = player.transform; dg.mushi = mu; dg.font = uiFont;
                dg.naya = nayaT.GetComponent<NiwaNayaNaka>();
                dg.menu = mn;
            } else Debug.LogError("[BuildNiwa] NiwaDougu が 場面に ない");
        }

        // ---- 物を 地ばんに すわらせる（凸凹に した ぶん、y=0 のままだと 浮く／沈む）
        {
            // ★自分で 地めんの 高さを 決めて 置いた もの（結合メッシュは 原点に あるので、ここで
            //   Takasa(0,0)＝段の 0.6 を 足されると **まるごと 0.6m 浮く**）は 外す（2026-09-04・本人
            //   「地面を下げたせいで、浮いてる物体がある？」→ 石垣・生垣・竹・柵・丸太・岩が 浮いて いた。
            //   石垣が 背高く 見えた（1.2m）のも これ）。座らせ直すのは y=0 で 置いた Kenney の 物だけ
            string[] nuki = { "Jimen", "JimenE", "MichiSoto", "BLK_", "Sora", "Satoyama",
                              "YamaToi", "Kumo", "Cam", "Sun", "Day", "Volume", "Player",
                              "Takadai", "Kage", "KiMiki", "KiHa", "KiAtari",
                              "Take", "Ishigaki", "Ikegaki", "Iwa", "Mushi", "Mise_", "Ie",
                              "Naya", "Suido", "Hanadan", "Dougu", "Menu" };
            int naosi = 0;
            for (int i = 0; i < root.childCount; i++) {
                var t = root.GetChild(i);
                bool tobu = false;
                foreach (var k in nuki) if (t.name.StartsWith(k)) { tobu = true; break; }
                if (tobu) continue;
                var q = t.position;
                float dy = NiwaJimenE.Takasa(q.x, q.z);
                if (Mathf.Abs(dy) < 0.0005f) continue;
                t.position = new Vector3(q.x, q.y + dy, q.z);
                naosi++;
            }
            Debug.Log("[Probe] NiwaJiban すわらせた " + naosi + " 個");
        }

        // ---- 庭の 地面の 一枚絵（D-119）。物が ぜんぶ 置かれて から 焼く
        //   （とびいし・木・塀・家の 位置を 場面から 拾って 踏み跡や 苔を 描く）
        var jimenE = NiwaJimenE.Yaku(root);
        if (jimenE != null) {
            var ita = new GameObject("JimenE");
            ita.transform.SetParent(root, false);
            ita.transform.position = new Vector3(NiwaJimenE.NAKA.x, 0.05f, NiwaJimenE.NAKA.y);
            ita.AddComponent<MeshFilter>().sharedMesh = NiwaJimenE.Ita();
            ita.AddComponent<MeshRenderer>();                        // あたりは Jimen が 持つ
            var mE = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mE, "Assets/Art/Materials/Niwa/NiwaJimenE.mat");
            mE.SetFloat("_Smoothness", 0.03f);
            // JIMEN_IRO (D-180): grass measured val0.59/sat0.55 vs leaves val0.33/sat0.33, house val0.31.
            // Pull the ground toward val0.45/sat0.40, hue 80deg like the leaves
            mE.SetColor("_BaseColor", JIMEN_IRO);
            mE.mainTexture = jimenE;
            ita.GetComponent<Renderer>().sharedMaterial = mE;
            ita.GetComponent<Renderer>().shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ---- 接地の影（いちばん さいごに。物が ぜんぶ 置かれて から 足もとに 敷く）
        NiwaJimen.Setchi(root);
        NiwaJimen.Ki(root, hayashi.Moto);
        NiwaJimen.Uki(ie, "家");
        NiwaJimen.Uki(root, "場面ぜんぶ");
        // 家は L字。**切りかき（家の 前の 空き地）に 影を 落とさない**よう 2つに 分けて 敷く
        //   実測：外接矩形で 敷いて いた ときは 切りかきの 芝生が 緑成分 110／
        //   同じ 奥ゆきの 庭 134＝18%も 暗かった
        {
            float z = ie.position.z;
            NiwaJimen.Kaku(root, "IeHonya", NiwaIe.X0, NiwaIe.X1,
                           z + NiwaIe.ZM, z + NiwaIe.ZN, NiwaIe.NOKI);
            NiwaJimen.Kaku(root, "IeGenkan", NiwaIe.KX, NiwaIe.X1,
                           z + NiwaIe.ZS, z + NiwaIe.ZM, NiwaIe.GNOKI);
        }

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, "Assets/Scenes/Niwa.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("[Probe] BuildNiwa done");
    }
}
