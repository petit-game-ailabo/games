using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// 3Dの 魔理沙を **8方向の 板に 焼く**（2026-09-05・案A）。
//
// ★これは **作業台**。庭の 場面には 何も 足さない。焼いた 絵は はじめ ArtSource に 出して、
//   本人が 見て よければ Assets/Art/Sprites へ 移す（いきなり 差しかえない）。
//
// そろえる きまり（`CharSprite` と `BuildNiwa` から）
//   ・シートは **8列 x 10行**。1コマ 224x336px（＝いまの marisa_walk.png と 同じ 1792x3360）
//   ・列＝向き。`CharSprite.Drive` は カメラ基準で 0＝手前(南) 4＝奥(北)、
//     2＝画面の 左(西) 6＝画面の 右(東)。**世界の 向きでは ない**
//   ・行＝0..7 走りの 8コマ／8 立ち／9 目とじ
//   ・板は 高さ 1.40m。絵は ほぼ いっぱいに 入って いる（足もと y=0・帽子の 天 1.34m）
//   ・**カメラは ふせ角 10度**。板は たて向きの ビルボードで、それを 10度 上から 見る ので、
//     真横から 焼くと 足もとの 見え方が 世界と 食いちがう
//   ・**コマの ふちは 1px あける**（アトラスの にじみ。CharSprite が 半テクセル 内に 寄せて いても、
//     絵じたいに あきが 無いと 隣が 出る。marisa_8x8 で 実際に 起きた）
//
// 輪郭線は **焼いた あとに 2Dで 引く**。3Dで 背面法を やると シェーダが 要る うえ、
// 太さが 距離で 変わる。ここは 板に する ので 2Dで 引く ほうが 素直で 太さも 決めうちできる。
public static class MarisaYaku {
    const int CW = 224, CH = 336;          // 1コマ
    const int COLS = 8, ROWS = 10;
    const float TAKASA = 1.40f;            // 板の 高さ(m)
    const float FUSE = 10f;                // カメラの ふせ角（BuildNiwa の hdPitch）
    const int BAI = 3;                     // 3倍で 焼いて 縮める（ぎざぎざ よけ）

    [MenuItem("なつやすみ/魔理沙を 3Dから 焼く")]
    public static void Yaku() {
        string outDir = "ArtSource/marisa3d";
        System.IO.Directory.CreateDirectory(outDir);

        var oya = new GameObject("MarisaYakuba") { hideFlags = HideFlags.DontSave };
        var karada = MarisaV6.Kumu(oya.transform);

        // ---- カメラ（正射影。板と 同じ 高さの ぶんだけ 映す）
        var camGO = new GameObject("YakuCam") { hideFlags = HideFlags.DontSave };
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = TAKASA * 0.5f;
        cam.aspect = CW / (float)CH;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.nearClipPlane = 0.01f; cam.farClipPlane = 12f;
        cam.cullingMask = ~0;
        // 板の まん中は 足もとから 0.70m（BuildNiwa の quad.localPosition.y = 0.66 + 足もと 0.04）
        var mato = new Vector3(0f, 0.70f, 0f);
        var muki = Quaternion.Euler(FUSE, 0f, 0f);
        camGO.transform.position = mato - muki * Vector3.forward * 5f;
        camGO.transform.rotation = muki;

        var rt = new RenderTexture(CW * BAI, CH * BAI, 24, RenderTextureFormat.ARGB32) {
            antiAliasing = 8,
        };
        cam.targetTexture = rt;

        var sheet = new Texture2D(CW * COLS, CH * ROWS, TextureFormat.RGBA32, false);
        var kara = new Color32[CW * COLS * CH * ROWS];
        sheet.SetPixels32(kara);

        var yomi = new Texture2D(CW * BAI, CH * BAI, TextureFormat.RGBA32, false);
        for (int row = 0; row < ROWS; row++) {
            MarisaV6.Pose(karada, row);
            for (int col = 0; col < COLS; col++) {
                // 列 0＝手前(南)＝カメラの ほうを 向く。2＝画面左 4＝奥 6＝画面右。
                // ★モデルの 顔は -Z、カメラも -Z 側に いる ので **col*45 が そのまま 正解**。
                //   はじめ 180 を 足して いて、8方向 まるごと 裏返って いた（前が うしろ）
                karada.root.localRotation = Quaternion.Euler(0f, col * 45f, 0f);
                cam.Render();
                var mae = RenderTexture.active;
                RenderTexture.active = rt;
                yomi.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                yomi.Apply(false);
                RenderTexture.active = mae;
                Haru(sheet, Chijimeru(yomi), col, row);
            }
        }
        Fuchi(sheet);
        sheet.Apply(false);

        System.IO.File.WriteAllBytes(outDir + "/marisa3d_8x10.png", sheet.EncodeToPNG());
        Kurabe(sheet, outDir + "/kurabe.png");

        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(yomi);
        Object.DestroyImmediate(sheet);
        Object.DestroyImmediate(camGO);
        Object.DestroyImmediate(oya);
        Debug.Log("[Probe] MarisaYaku 焼いた " + outDir + "/marisa3d_8x10.png  "
                  + (CW * COLS) + "x" + (CH * ROWS));
    }

    /// <summary>BAI倍で 焼いた ものを 1コマの 大きさへ。**アルファも 混ぜる**ので ふちが なめらか</summary>
    static Color[] Chijimeru(Texture2D src) {
        var s = src.GetPixels();
        var d = new Color[CW * CH];
        for (int y = 0; y < CH; y++)
            for (int x = 0; x < CW; x++) {
                float r = 0f, g = 0f, b = 0f, a = 0f;
                for (int j = 0; j < BAI; j++)
                    for (int i = 0; i < BAI; i++) {
                        var c = s[(y * BAI + j) * CW * BAI + (x * BAI + i)];
                        r += c.r * c.a; g += c.g * c.a; b += c.b * c.a; a += c.a;
                    }
                if (a > 0.0001f) d[y * CW + x] = new Color(r / a, g / a, b / a, a / (BAI * BAI));
                else d[y * CW + x] = new Color(0f, 0f, 0f, 0f);
            }
        return d;
    }

    static void Haru(Texture2D sheet, Color[] koma, int col, int row) {
        // 画像は 上が 0行め、Texture2D は 下が 0行め
        int ox = col * CW, oy = (ROWS - 1 - row) * CH;
        sheet.SetPixels(ox, oy, CW, CH, koma);
    }

    /// <summary>輪郭線。★まず **アルファを かたく する**（材質が 0.5で 切るので、
    /// なめらかな ふちは どうせ 捨てられる）。そのうえで 外へ 2px の 線を 引き、
    /// 中の 色が 大きく 変わる ところ（服と 肌の 境）にも 細い 線を 入れる。
    /// 平らな 色 ＋ 線 が いちばん 描いた 絵に 近い（陰影を 3Dで つけると 3Dに 見える）</summary>
    static void Fuchi(Texture2D sheet) {
        int W = sheet.width, H = sheet.height;
        var p = sheet.GetPixels();
        for (int i = 0; i < p.Length; i++) {            // かたく する
            var c = p[i];
            p[i] = c.a >= 0.5f ? new Color(c.r, c.g, c.b, 1f) : new Color(0f, 0f, 0f, 0f);
        }
        var q = new Color[p.Length];
        System.Array.Copy(q, q, 0);
        System.Array.Copy(p, q, p.Length);
        var sen = new Color(0.13f, 0.10f, 0.12f, 1f);
        System.Func<int, int, bool> Naka = (x, y) => {
            if (x < 0 || y < 0 || x >= W || y >= H) return false;
            return p[y * W + x].a > 0.5f;
        };
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++) {
                int i = y * W + x;
                int cx = x % CW, cy = y % CH;
                var c = p[i];
                if (c.a < 0.5f) {
                    // 外がわ：2px 以内に 中みが あれば 線に する
                    bool chikai = false;
                    for (int dy = -2; dy <= 2 && !chikai; dy++)
                        for (int dx = -2; dx <= 2; dx++) {
                            if (dx * dx + dy * dy > 5) continue;
                            int nx = cx + dx, ny = cy + dy;
                            if (nx < 0 || ny < 0 || nx >= CW || ny >= CH) continue;   // コマを またがない
                            if (Naka(x + dx, y + dy)) { chikai = true; break; }
                        }
                    if (chikai) q[i] = sen;
                    continue;
                }
                // 内がわ：色の 段差に 細い 線
                float sa = 0f;
                for (int d = 0; d < 4; d++) {
                    int dx = d == 0 ? -1 : d == 1 ? 1 : 0, dy = d == 2 ? -1 : d == 3 ? 1 : 0;
                    int nx = cx + dx, ny = cy + dy;
                    if (nx < 0 || ny < 0 || nx >= CW || ny >= CH) continue;
                    var o = p[(y + dy) * W + (x + dx)];
                    if (o.a < 0.5f) continue;
                    sa = Mathf.Max(sa, Mathf.Abs(o.r - c.r) + Mathf.Abs(o.g - c.g) + Mathf.Abs(o.b - c.b));
                }
                if (sa > 0.60f) q[i] = Color.Lerp(c, sen, 0.50f);
            }
        sheet.SetPixels(q);
    }

    /// <summary>いまの 2Dの 絵と **同じ 画面の 大きさ**で ならべる。
    /// ★これを 見ない かぎり 良し悪しは 決められない（コマ単体では どちらも きれいに 見える）</summary>
    static void Kurabe(Texture2D atarashii, string path) {
        var ima = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sprites/marisa_walk.png");
        // 画面で 出る 大きさ：板 1.40m を FOV33・距離15 で 見ると 720pxの 画面で 約 190px
        const int SH = 190, SW = SH * CW / CH;
        int[] rows = { 8, 0, 2, 4, 6 };          // 立ち＋走りの 4コマ
        int[] cols = { 0, 6, 4, 2 };             // 手前・右・奥・左
        int W = SW * rows.Length * cols.Length + 20, H = SH * 2 + 30;
        var o = new Texture2D(W, H, TextureFormat.RGBA32, false);
        var bg = new Color[W * H];
        for (int i = 0; i < bg.Length; i++) bg[i] = new Color(0.42f, 0.52f, 0.30f, 1f);
        o.SetPixels(bg);
        int k = 0;
        foreach (int col in cols)
            foreach (int row in rows) {
                Nuki(o, atarashii, col, row, k * SW + 10, 10, SW, SH);
                if (ima != null) Nuki(o, ima, col, row, k * SW + 10, SH + 20, SW, SH);
                k++;
            }
        o.Apply(false);
        System.IO.File.WriteAllBytes(path, o.EncodeToPNG());
        Object.DestroyImmediate(o);
        Debug.Log("[Probe] MarisaYaku くらべ " + path + "（上の段＝いまの2D 下の段＝3D）");
    }

    static void Nuki(Texture2D dst, Texture2D src, int col, int row, int ox, int oy, int w, int h) {
        if (!src.isReadable) {
            var ti = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(src)) as TextureImporter;
            if (ti != null && !ti.isReadable) { ti.isReadable = true; ti.SaveAndReimport(); }
        }
        int sw = src.width / COLS, sh = src.height / ROWS;
        int sx = col * sw, sy = (ROWS - 1 - row) * sh;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++) {
                var c = src.GetPixelBilinear((sx + (x + 0.5f) * sw / w) / src.width,
                                             (sy + (y + 0.5f) * sh / h) / src.height);
                if (c.a < 0.4f) continue;
                if (ox + x < 0 || ox + x >= dst.width || oy + y < 0 || oy + y >= dst.height) continue;
                dst.SetPixel(ox + x, oy + y, new Color(c.r, c.g, c.b, 1f));
            }
    }
}
