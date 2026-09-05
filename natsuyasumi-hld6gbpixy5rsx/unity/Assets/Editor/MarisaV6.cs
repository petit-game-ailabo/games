using System.Collections.Generic;
using UnityEngine;

// 魔理沙の 3Dモデル（2026-09-05・第6版＝はじめての 立体）。
//
// ★本人「2Dの様々な動きを全部用意するの大変かな。AI生成で画像も毎回微妙にぶれちゃう。
//   君の方で3Dの人間モデリングして、魔理沙を動くキャラにできるかな」
//   → **案A：3Dで 作って 8方向の 板に 焼く**（`MarisaYaku`）。見た目は いまのまま、
//     ぶれは 消え、動きを 足すのは 絵では なく **式を 書く** ことに なる。
//
// 作りの きまり
//   ・**皮づけ（スキニング）を しない。** 部品を 親子で つないで 回すだけ（剛体の 人形）。
//     画面で 224x336px、頭は 60px しか ない ので、関節の なめらかさは 見えない。
//     そのぶん **ボーンも ウェイトも 要らず、動きは ぜんぶ 式で 書ける**。
//   ・部品は 上から 下へ 親子：腰 → 胸 → 首 → 頭 → 帽子／腰 → もも → すね／胸 → 上腕 → 前腕。
//   ・**大きさは 板に あわせる**。板は 1.40m（BuildNiwa）で、絵は ほぼ いっぱいに 入って いる。
//     背は 1.30m（子ども・5頭身）、足もとが y=0。
//   ・色は 平ら（Unlit）。立体感は 焼く ときの 輪郭線で 出す（`MarisaYaku`）。
//     陰影を 3Dで つけると「3Dの キャラを 貼りました」に なる。
public static class MarisaV6 {

    // ---- 色（魔理沙）
    static readonly Color KURO  = new Color(0.13f, 0.12f, 0.15f);   // 服・帽子
    static readonly Color KUROU = new Color(0.20f, 0.19f, 0.23f);   // 服の 明るい ところ
    static readonly Color SHIRO = new Color(0.95f, 0.94f, 0.90f);   // エプロン・袖
    static readonly Color KAMI  = new Color(0.94f, 0.80f, 0.36f);   // 髪
    static readonly Color KAMIK = new Color(0.83f, 0.66f, 0.24f);   // 髪の 濃い ところ
    static readonly Color HADA  = new Color(0.99f, 0.86f, 0.76f);
    static readonly Color ME    = new Color(0.22f, 0.16f, 0.12f);
    static readonly Color KUTSU = new Color(0.16f, 0.14f, 0.16f);

    /// <summary>部品の 入れもの。動かすのは ここに ある Transform だけ</summary>
    public class Karada {
        public Transform root, koshi, mune, kubi, atama, boushi, skirt;
        public readonly Transform[] kata = new Transform[2];   // 上腕（0=左 1=右）
        public readonly Transform[] hiji = new Transform[2];   // 前腕
        public readonly Transform[] momo = new Transform[2];
        public readonly Transform[] hiza = new Transform[2];
        public readonly Transform[] me = new Transform[2];     // 目（まばたきで つぶす）
        public readonly List<Renderer> subete = new List<Renderer>();
    }

    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static Material M(string name, Color c) {
        Material m;
        if (mats.TryGetValue(name, out m)) return m;
        var sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        m = new Material(sh) { name = name, hideFlags = HideFlags.DontSave };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.color = c;
        mats[name] = m;
        return m;
    }

    static Transform Ten(Transform oya, string na, Vector3 at) {
        var g = new GameObject(na);
        g.transform.SetParent(oya, false);
        g.transform.localPosition = at;
        return g.transform;
    }

    static Transform Bu(Karada k, Transform oya, string na, PrimitiveType t,
                        Vector3 at, Vector3 s, Vector3 kai, Material m) {
        var g = GameObject.CreatePrimitive(t);
        g.name = na; g.hideFlags = HideFlags.DontSave;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(oya, false);
        g.transform.localPosition = at;
        g.transform.localRotation = Quaternion.Euler(kai);
        g.transform.localScale = s;
        var r = g.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        k.subete.Add(r);
        return g.transform;
    }

    /// <summary>組み立てる。足もとが y=0、背は 帽子まで 1.34m</summary>
    public static Karada Kumu(Transform oya) {
        var k = new Karada();
        k.root = Ten(oya, "Marisa", Vector3.zero);

        // ---- 腰（ここから 上を まとめて 動かす）
        k.koshi = Ten(k.root, "Koshi", new Vector3(0f, 0.52f, 0f));
        // ---- 脚（腰の 子。もも → すね → くつ）
        for (int i = 0; i < 2; i++) {
            float sx = i == 0 ? -1f : 1f;
            k.momo[i] = Ten(k.koshi, "Momo" + i, new Vector3(sx * 0.075f, 0f, 0f));
            // ★ももは **黒**（スカートの 下の もの）。肌色に して いたら、走りで 前へ 出した とたん
            //   スカートを つきぬけて **白い かたまりが 前に 出た**（2026-09-05）。
            //   剛体の 人形なので つきぬけは 完全には 消せない。色を あわせて 気づかせない
            Bu(k, k.momo[i], "MomoMi", PrimitiveType.Capsule,
               new Vector3(0f, -0.13f, 0f), new Vector3(0.128f, 0.13f, 0.128f), Vector3.zero, M("kuro", KURO));
            k.hiza[i] = Ten(k.momo[i], "Hiza" + i, new Vector3(0f, -0.26f, 0f));
            Bu(k, k.hiza[i], "SuneMi", PrimitiveType.Capsule,
               new Vector3(0f, -0.11f, 0f), new Vector3(0.106f, 0.115f, 0.106f), Vector3.zero, M("hada", HADA));
            Bu(k, k.hiza[i], "Kutsu", PrimitiveType.Cube,
               new Vector3(0f, -0.24f, 0.015f), new Vector3(0.115f, 0.075f, 0.175f), Vector3.zero, M("kutsu", KUTSU));
        }
        // ---- スカート（腰の 子。すそが 広がる 円すい台）
        // ★白い ふちは **すそ**。腰に つけて いた ので、まわりに 白い 皿が 出て いた（2026-09-05）
        k.skirt = Ten(k.koshi, "Skirt", new Vector3(0f, 0.03f, 0f));
        Sube(k, k.skirt, "SkirtMi", 0.140f, 0.245f, -0.285f, M("kuro", KURO));
        var fuchi = Ten(k.skirt, "SkirtFuchi", new Vector3(0f, -0.285f, 0f));
        Sube(k, fuchi, "FuchiMi", 0.248f, 0.243f, 0.026f, M("shiro", SHIRO));   // 細い フリル

        // ---- 胸（腰の 子）
        k.mune = Ten(k.koshi, "Mune", new Vector3(0f, 0.10f, 0f));
        Bu(k, k.mune, "Doui", PrimitiveType.Capsule,
           new Vector3(0f, 0.07f, 0f), new Vector3(0.20f, 0.115f, 0.155f), Vector3.zero, M("kuro", KURO));
        // ★胴は **まるごと 黒**。肩に 白い 帯（袖）を 回したら 胸が 白く なり、
        //   その 上に 黒い ベストを 置いた ので「白の 中に 黒い 四角」に なった（2026-09-05）。
        //   魔理沙は **黒い ドレス ＋ 前に 白い エプロン**。白は 前の 1枚だけ に する
        Bu(k, k.mune, "Apron", PrimitiveType.Cube,
           new Vector3(0f, 0.015f, -0.086f), new Vector3(0.118f, 0.185f, 0.020f), Vector3.zero, M("shiro", SHIRO));
        Bu(k, k.mune, "ApronKata", PrimitiveType.Cube,                            // 肩ひも（細い 2本）
           new Vector3(-0.052f, 0.125f, -0.084f), new Vector3(0.026f, 0.075f, 0.022f), Vector3.zero, M("shiro", SHIRO));
        Bu(k, k.mune, "ApronKata", PrimitiveType.Cube,
           new Vector3(0.052f, 0.125f, -0.084f), new Vector3(0.026f, 0.075f, 0.022f), Vector3.zero, M("shiro", SHIRO));

        // ---- 腕（胸の 子。上腕 → 前腕）
        for (int i = 0; i < 2; i++) {
            float sx = i == 0 ? -1f : 1f;
            k.kata[i] = Ten(k.mune, "Kata" + i, new Vector3(sx * 0.128f, 0.145f, -0.012f));
            Bu(k, k.kata[i], "UdeShiro", PrimitiveType.Capsule,
               new Vector3(0f, -0.055f, 0f), new Vector3(0.085f, 0.06f, 0.085f), Vector3.zero, M("shiro", SHIRO));
            Bu(k, k.kata[i], "UdeMi", PrimitiveType.Capsule,
               new Vector3(0f, -0.115f, 0f), new Vector3(0.072f, 0.075f, 0.072f), Vector3.zero, M("hada", HADA));
            k.hiji[i] = Ten(k.kata[i], "Hiji" + i, new Vector3(0f, -0.20f, 0f));
            Bu(k, k.hiji[i], "MaeudeMi", PrimitiveType.Capsule,
               new Vector3(0f, -0.085f, 0f), new Vector3(0.066f, 0.075f, 0.066f), Vector3.zero, M("hada", HADA));
            Bu(k, k.hiji[i], "Te", PrimitiveType.Sphere,
               new Vector3(0f, -0.175f, 0f), new Vector3(0.075f, 0.075f, 0.075f), Vector3.zero, M("hada", HADA));
        }

        // ---- 首と 頭
        k.kubi = Ten(k.mune, "Kubi", new Vector3(0f, 0.19f, 0f));
        Bu(k, k.kubi, "KubiMi", PrimitiveType.Capsule,
           new Vector3(0f, 0.02f, 0f), new Vector3(0.06f, 0.03f, 0.06f), Vector3.zero, M("hada", HADA));
        k.atama = Ten(k.kubi, "Atama", new Vector3(0f, 0.05f, 0f));
        Bu(k, k.atama, "Kao", PrimitiveType.Sphere,
           new Vector3(0f, 0.105f, 0f), new Vector3(0.245f, 0.255f, 0.230f), Vector3.zero, M("hada", HADA));
        // 髪：うしろの かたまり ＋ 前がみ ＋ 三つ編み 2本
        Bu(k, k.atama, "KamiUshiro", PrimitiveType.Sphere,
           new Vector3(0f, 0.10f, 0.030f), new Vector3(0.258f, 0.265f, 0.240f), Vector3.zero, M("kami", KAMI));
        Bu(k, k.atama, "Maegami", PrimitiveType.Sphere,
           new Vector3(0f, 0.170f, -0.026f), new Vector3(0.245f, 0.125f, 0.215f), Vector3.zero, M("kami", KAMI));
        Bu(k, k.atama, "KamiNaga", PrimitiveType.Capsule,
           new Vector3(0f, -0.03f, 0.055f), new Vector3(0.225f, 0.125f, 0.145f), Vector3.zero, M("kami", KAMI));
        for (int i = 0; i < 2; i++) {
            float sx = i == 0 ? -1f : 1f;
            Bu(k, k.atama, "Mitsuami", PrimitiveType.Capsule,
               new Vector3(sx * 0.128f, -0.05f, -0.018f), new Vector3(0.058f, 0.115f, 0.058f),
               new Vector3(8f, 0f, sx * -7f), M("kamik", KAMIK));
        }
        // 目（まばたきで z を つぶす）
        for (int i = 0; i < 2; i++) {
            float sx = i == 0 ? -1f : 1f;
            k.me[i] = Bu(k, k.atama, "Me" + i, PrimitiveType.Sphere,
                         new Vector3(sx * 0.060f, 0.098f, -0.112f), new Vector3(0.046f, 0.066f, 0.03f),
                         Vector3.zero, M("me", ME));
        }
        // 口（小さな 線）
        Bu(k, k.atama, "Kuchi", PrimitiveType.Cube,
           new Vector3(0f, 0.040f, -0.115f), new Vector3(0.028f, 0.008f, 0.02f), Vector3.zero, M("me", ME));

        // ---- 帽子（とんがり＋つば＋白い 帯）
        // ★つばは **小さめ・少し 上向き**。r=0.30（差しわたし 0.6m）だと ふせ角10度の カメラから
        //   顔が まるごと 隠れた（2026-09-05）。魔理沙の 帽子は 大きいが、絵では 顔が 見えて いる
        k.boushi = Ten(k.atama, "Boushi", new Vector3(0f, 0.215f, 0.012f));
        k.boushi.localRotation = Quaternion.Euler(-9f, 0f, 0f);                  // 少し 上向き
        Sube(k, k.boushi, "Tsuba", 0.235f, 0.225f, -0.024f, M("kuro", KURO));    // つば
        Sube(k, k.boushi, "Yama", 0.150f, 0.028f, 0.245f, M("kuro", KURO));      // とんがり
        Sube(k, k.boushi, "Obi", 0.157f, 0.150f, 0.036f, M("shiro", SHIRO));     // 白い 帯
        // リボン（うしろ）
        Bu(k, k.boushi, "Ribbon", PrimitiveType.Cube,
           new Vector3(0f, 0.048f, 0.140f), new Vector3(0.150f, 0.072f, 0.035f),
           new Vector3(0f, 0f, 12f), M("shiro", SHIRO));
        return k;
    }

    /// <summary>すぼまった 円すい台。r0＝下、r1＝上、h＝高さ（負なら 下へ 伸びる）</summary>
    static void Sube(Karada k, Transform oya, string na, float r0, float r1, float h, Material m) {
        const int seg = 14;
        var v = new List<Vector3>(); var tri = new List<int>();
        float y0 = h >= 0f ? 0f : h, y1 = h >= 0f ? h : 0f;
        float a0 = h >= 0f ? r0 : r1, a1 = h >= 0f ? r1 : r0;
        for (int i = 0; i <= seg; i++) {
            float a = i * Mathf.PI * 2f / seg;
            v.Add(new Vector3(Mathf.Cos(a) * a0, y0, Mathf.Sin(a) * a0));
            v.Add(new Vector3(Mathf.Cos(a) * a1, y1, Mathf.Sin(a) * a1));
        }
        for (int i = 0; i < seg; i++) {
            int b = i * 2;
            tri.Add(b); tri.Add(b + 1); tri.Add(b + 2);
            tri.Add(b + 1); tri.Add(b + 3); tri.Add(b + 2);
        }
        int c0 = v.Count; v.Add(new Vector3(0f, y1, 0f));          // 上ぶた
        for (int i = 0; i <= seg; i++) {
            float a = i * Mathf.PI * 2f / seg;
            v.Add(new Vector3(Mathf.Cos(a) * a1, y1, Mathf.Sin(a) * a1));
        }
        for (int i = 0; i < seg; i++) { tri.Add(c0); tri.Add(c0 + 1 + i); tri.Add(c0 + 2 + i); }
        int d0 = v.Count; v.Add(new Vector3(0f, y0, 0f));          // 下ぶた
        for (int i = 0; i <= seg; i++) {
            float a = i * Mathf.PI * 2f / seg;
            v.Add(new Vector3(Mathf.Cos(a) * a0, y0, Mathf.Sin(a) * a0));
        }
        for (int i = 0; i < seg; i++) { tri.Add(d0); tri.Add(d0 + 2 + i); tri.Add(d0 + 1 + i); }

        var mesh = new Mesh { name = na, hideFlags = HideFlags.DontSave };
        mesh.SetVertices(v); mesh.SetTriangles(tri, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var g = new GameObject(na) { hideFlags = HideFlags.DontSave };
        g.transform.SetParent(oya, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = g.AddComponent<MeshRenderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        k.subete.Add(r);
    }

    // ================================================================ ポーズ
    /// <summary>row 0..7＝走りの 8コマ／8＝立ち／9＝目とじ。すべて **式で 書く**。
    /// 動きを 足す ときは ここに 1つ 関数を 足すだけで、絵は 1枚も 描かない</summary>
    public static void Pose(Karada k, int row) {
        // まず 素の 姿に もどす
        k.koshi.localPosition = new Vector3(0f, 0.52f, 0f);
        k.koshi.localRotation = Quaternion.identity;
        k.mune.localRotation = Quaternion.identity;
        k.atama.localRotation = Quaternion.identity;
        k.skirt.localRotation = Quaternion.identity;
        for (int i = 0; i < 2; i++) {
            k.momo[i].localRotation = Quaternion.identity;
            k.hiza[i].localRotation = Quaternion.identity;
            k.kata[i].localRotation = Quaternion.identity;
            k.hiji[i].localRotation = Quaternion.identity;
            var s = k.me[i].localScale; s.y = 0.062f; k.me[i].localScale = s;
        }

        if (row >= 8) {                                   // 立ち／目とじ
            for (int i = 0; i < 2; i++) {
                float sx = i == 0 ? -1f : 1f;
                k.kata[i].localRotation = Quaternion.Euler(4f, 0f, sx * -7f);
                k.hiji[i].localRotation = Quaternion.Euler(10f, 0f, 0f);
            }
            if (row == 9) for (int i = 0; i < 2; i++) {    // まばたき＝目を つぶす
                var s = k.me[i].localScale; s.y = 0.008f; k.me[i].localScale = s;
            }
            return;
        }

        // ---- 走り 8コマ。位相 0..1
        float t = row / 8f;
        float w = t * Mathf.PI * 2f;
        float sw = Mathf.Sin(w);                          // 脚の ふり
        // 腰：1歩に 2回 上下する（接地の たびに 沈む）
        k.koshi.localPosition = new Vector3(0f, 0.52f + 0.035f + Mathf.Cos(w * 2f) * 0.028f, 0f);
        k.koshi.localRotation = Quaternion.Euler(0f, sw * 8f, 0f);
        k.mune.localRotation = Quaternion.Euler(9f, sw * -10f, 0f);   // 前かがみ＋ひねり
        // スカートは 一歩 おくれて ゆれる（腰に かたく ついて いると 板に 見える）
        k.skirt.localRotation = Quaternion.Euler(Mathf.Cos(w) * -7f, 0f, sw * 5f);
        k.atama.localRotation = Quaternion.Euler(-5f, sw * 5f, 0f);
        for (int i = 0; i < 2; i++) {
            float sx = i == 0 ? -1f : 1f;
            float p = i == 0 ? sw : -sw;                  // 左右で 逆
            float pc = i == 0 ? Mathf.Cos(w) : -Mathf.Cos(w);
            k.momo[i].localRotation = Quaternion.Euler(p * -32f, 0f, 0f);   // 32度＝スカートの 内に おさまる
            // ひざは 後ろへ しか 曲がらない。前へ 出す ときに たたむ
            k.hiza[i].localRotation = Quaternion.Euler(Mathf.Max(0f, pc * 62f + 18f), 0f, 0f);
            k.kata[i].localRotation = Quaternion.Euler(p * 46f, 0f, sx * -9f);
            k.hiji[i].localRotation = Quaternion.Euler(52f + p * 16f, 0f, 0f);
        }
    }
}
