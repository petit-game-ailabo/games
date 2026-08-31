using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// 庭の 母屋を **日本の 農家**に する（2026-09-01）。
//
// ★本人「家を日本の田舎風にしていきたい。庭も。これってどうやったらできるんだろう。
//   今までの木のようにテクスチャを用意すれば何とかなるものなのかな」
//   → **絵では 届かない**。ちがいは 表面では なく 形。
//     屋根＝入母屋・軒の出が1m前後・反りが ある／壁＝真壁（柱と貫が 外に 見える）＋
//     下見板の 腰壁／開口＝障子・雨戸・**縁側**。深い 軒が つくる 影が 日本家屋の 見えかた
//     そのものなので、ヨーロッパ風の 箱に 和風の 絵を 貼っても 出ない。
//
// ★作り直さない。**この 企画には すでに 農家を 建てる 仕組みが ある**：
//     HouseRoof.cs  … 入母屋の 屋根を メッシュで 起こす（軒の出・反り・隅棟・軒先のはね上げ）
//     BuildHouse.cs … 土間・玄関・中廊下・下屋・庇まで 含む 母屋 一式（24 x 12m）
//   箱の村（BuildZashiki）むけに 作った まま 庭シーンへ 持ちこんで いなかっただけ。
//   木を BuildMura から KiV5 として 取りだしたのと 同じ 要領で つなぐ。
//
// ★大きさ：桁行 24.0m（13間）・梁間 12.0m（6.6間）。民家園の 実寸を 調べた 結果で、
//   中〜大規模の 農家の 母屋は 桁行 14.5〜23.6m。**縮めない**（縮めると 戸や 縁側が
//   人の 大きさと 合わなく なる）。庭の ほうを 広げる。
public static class NiwaIe {
    const string TEX = "Assets/Art/Textures/";
    const string DIR = "Assets/Art/Materials/Niwa";

    static Material Mat(string name, string tex, Vector2 tiling, float rough, Color tint) {
        System.IO.Directory.CreateDirectory(DIR);
        string path = DIR + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        // ★建物は **ディザで 抜ける Lit**（BuildZashiki と 同じ）。
        //   主人公より 手前の 画素だけ ちらして 抜く ので、家の 裏に まわっても
        //   画面が 壁 1色に ならない
        var sh = Shader.Find("Natsuyasumi/DitherLit");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
        m.shader = sh;
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX + tex);
        if (t == null) Debug.LogError("[NiwaIe] 絵が ない: " + tex);
        else {
            m.SetTexture("_BaseMap", t);
            m.SetTextureScale("_BaseMap", tiling);
            m.mainTexture = t;
            m.mainTextureScale = tiling;
        }
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 1f - rough);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
        return m;
    }

    /// <summary>面の 大きさ(m)から 貼りかたを 決める。
    /// ★箱の UVは どの 面も 0〜1 なので、貼りかたが 1つだと 10.8mの 壁と 0.6mの 壁で
    ///   絵の こまかさが 18倍 ちがう（BuildZashiki で 踏んだ）。1.5m/まい に そろえる</summary>
    static System.Func<float, float, Material> FitMat(string prefix, string tex, float rough,
                                                      Color tint) {
        var cache = new Dictionary<string, Material>();
        return (w, h) => {
            int kw = Mathf.Max(1, Mathf.RoundToInt(w * 20)), kh = Mathf.Max(1, Mathf.RoundToInt(h * 20));
            string k = kw + "_" + kh;
            Material got;
            if (cache.TryGetValue(k, out got)) return got;
            got = Mat("IeFit_" + prefix + "_" + k, tex, new Vector2(w / 1.5f, h / 1.5f), rough, tint);
            cache[k] = got;
            return got;
        };
    }

    static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material m) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name; go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localScale = size;
        if (m != null) go.GetComponent<Renderer>().sharedMaterial = m;
        return go;
    }

    /// <summary>母屋を 建てる。ie は **南（-Z）を 向いた** 入れもの
    /// （BuildHouse は 縁側と 玄関が +Z がわ なので、呼ぶ 側で 180°まわす）</summary>
    public static void Build(Transform ie) {
        var mats = new BuildHouse.Mats {
            tatami  = Mat("IeTatami",  "tatami.png",       new Vector2(6f, 6f),   0.95f, Color.white),
            wood    = Mat("IeWood",    "wood_beam.png",    new Vector2(4f, 1f),   0.80f, Color.white),
            floor   = Mat("IeFloor",   "wood_floor.png",   new Vector2(6f, 4f),   0.75f, Color.white),
            plaster = Mat("IePlaster", "plaster_wall.png", new Vector2(6f, 3f),   0.96f, Color.white),
            roof    = Mat("IeRoof",    "roof_tile.png",    new Vector2(7.2f, 1.6f), 0.86f, Color.white),
            paper   = Mat("IePaper",   "shoji_paper.png",  new Vector2(3f, 3f),   0.90f, Color.white),
            stone   = Mat("IeStone",   "stone.png",        new Vector2(3f, 2f),   0.95f, Color.white),
            soil    = Mat("IeSoil",    "ji_tsuchi.jpg",    new Vector2(3f, 2f),   1f,    Color.white),
            // ★メッシュで 起こす 屋根は **UVに mを 焼きこむ**ので、貼りかた(1,1)の 別材質が 要る。
            //   箱用の (7.2,1.6) を わたすと 絵が 7倍に のびて ただの 灰色に なる
            roofM   = Mat("IeRoofMesh", "roof_tile.png",   Vector2.one, 0.86f, Color.white),
            woodM   = Mat("IeWoodMesh", "wood_beam.png",   Vector2.one, 0.80f, Color.white),
            plasterFit = FitMat("Plaster", "plaster_wall.png", 0.96f, Color.white),
            woodFit    = FitMat("Wood",    "wood_beam.png",    0.80f, Color.white),
            // 腰の 下見板は **柱より ぐっと 暗く**（柿渋や すすで 黒に 近い 焦茶に なる）。
            // 同じ 明るさだと 板の すじが 見えて いても 暗い かたまりに しか 読めない
            koshiFit   = FitMat("Koshi",   "wood_beam.png",    0.88f,
                                new Color(0.48f, 0.42f, 0.36f)),
        };
        BuildHouse.Build(ie, mats, (nm, par, pos, size, mat) => Box(nm, par, pos, size, mat));
    }
}
