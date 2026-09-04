using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// 竹藪 v1（2026-09-03）と 写真の 草むら。
//
// ★本人「竹とか木とか草とかのローポリやだな。せめて絵を貼り付けたい。竹はこんな棒じゃなくて、
//   竹藪調べて。茶色も緑も合って、長い短い、そして長い奴らは斜めにたれてくる」
//
// 調べた こと（モウソウチク／マダケ）：
//   ・色は 年齢：今年竹＝黄緑（白い 粉）、1〜3年＝濃い 緑、4〜5年＝黄ばみ、枯れ竹＝灰茶
//   ・8月の 割合：手入れ された 竹林は 枯れ 5〜10%、放置竹林（空き家の 脇）は 枯れ 2〜4割。
//     この 家は 放置ぎみ なので **緑 60・黄ばみ 15・茶 25**
//   ・葉は 稈の 上半分。重みで 先が 弧を 描いて 外へ 垂れ、藪の ふちほど 大きく 傾く
//   ・枯れ竹は 葉を 落とし、より 大きく 傾いて 倒れかかる
//   ・節は 20〜40cm ごと。太さ 7〜12cm。高さは この 場面の 木（6〜8.5m）に あわせ 5.5〜9m
//
// 作りは 木（KiV5）と 同じ：背骨に 輪を ならべた チューブの 稈 ＋ 枝の 先に 写真の 葉カード。
// 稈の 皮は 描いた 絵（make_take.py・節の 帯 4本/1.25m）、葉は 本人の 画像（take_ha.png）。
// 葉の 画像が まだ 無い あいだは 木の 葉（ki_ha.png）で 代用する
public static class TakeV1 {
    static Mesh quadMesh;
    static Mesh Quad() {
        if (quadMesh != null) return quadMesh;
        var t = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadMesh = t.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(t);
        return quadMesh;
    }

    static Material Mat(string name, string tex, bool sukashi) {
        string dir = "Assets/Art/Materials/Niwa";
        System.IO.Directory.CreateDirectory(dir);
        string path = dir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.shader = Shader.Find("Universal Render Pipeline/Lit");
        m.color = Color.white;
        m.SetFloat("_Smoothness", 0.06f);
        m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/shashin/" + tex);
        m.mainTextureScale = Vector2.one;
        if (sukashi) {
            m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        }
        return m;
    }

    static bool Aru(string tex) {
        return AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/shashin/" + tex) != null;
    }

    /// <summary>竹藪。何本でも 植えて から 色ごとに 1つの メッシュに まとめる</summary>
    public class Yabu {
        readonly Transform root;
        readonly List<CombineInstance>[] kan = { new List<CombineInstance>(), new List<CombineInstance>(), new List<CombineInstance>(), new List<CombineInstance>(), new List<CombineInstance>() };   // 0緑 1黄 2茶 3杭の木 4木の皮
        readonly List<CombineInstance> ha = new List<CombineInstance>();
        readonly List<Vector3> teppen = new List<Vector3>();     // 葉の 法線の もと（稈の 上の ほう）
        public int Kazu;

        public Yabu(Transform root) { this.root = root; }

        void Tube(List<CombineInstance> dst, Vector3[] pts, float[] rad, int sides) {
            // ★継ぎ目の 直し（2026-09-03・本人「どの木も、木の手前から見ると縦線が入ったようになってる」）。
            //   輪は sides 点で、最後の 面だけ u が 11/12→0 に もどる ので 絵が 逆向きに 詰まって
            //   **縦の 線**に なって いた。しかも 輪の 0番は -Z（カメラの 正面）に 来る 作りだった。
            //   → 輪を sides+1 点に して 最後の 点は 最初と 同じ 位置で u=1、法線は 両はしを そろえ、
            //     0番の 角度を π ずらして 継ぎ目を うしろ（+Z）へ まわす
            int n = pts.Length, sd = sides + 1;
            var verts = new Vector3[n * sd + 1];
            var uv = new Vector2[verts.Length];
            var nrm = Vector3.Cross((pts[1] - pts[0]).normalized, Vector3.right);
            if (nrm.sqrMagnitude < 0.01f) nrm = Vector3.forward; else nrm = nrm.normalized;
            float vlen = 0f;
            for (int i = 0; i < n; i++) {
                var dir = (i == 0 ? pts[1] - pts[0]
                         : i == n - 1 ? pts[n - 1] - pts[n - 2]
                         : pts[i + 1] - pts[i - 1]).normalized;
                nrm = (nrm - dir * Vector3.Dot(nrm, dir)).normalized;
                var bin = Vector3.Cross(dir, nrm);
                if (i > 0) vlen += (pts[i] - pts[i - 1]).magnitude;
                for (int s = 0; s <= sides; s++) {
                    float a = s * Mathf.PI * 2f / sides + Mathf.PI;
                    verts[i * sd + s] = pts[i] + (nrm * Mathf.Cos(a) + bin * Mathf.Sin(a)) * rad[i];
                    uv[i * sd + s] = new Vector2((float)s / sides, vlen * 0.8f);
                }
            }
            verts[n * sd] = pts[n - 1];
            uv[n * sd] = new Vector2(0.5f, vlen * 0.8f + 0.2f);
            var tris = new List<int>();
            for (int i = 0; i < n - 1; i++)
                for (int s = 0; s < sides; s++) {
                    int a0 = i * sd + s, a1 = a0 + 1, b0 = (i + 1) * sd + s, b1 = b0 + 1;
                    tris.Add(a0); tris.Add(a1); tris.Add(b0);
                    tris.Add(a1); tris.Add(b1); tris.Add(b0);
                }
            for (int s = 0; s < sides; s++) { tris.Add((n - 1) * sd + s); tris.Add(n * sd); tris.Add((n - 1) * sd + s + 1); }
            var mesh = new Mesh { vertices = verts, uv = uv, triangles = tris.ToArray() };
            mesh.RecalculateNormals();
            var ns = mesh.normals;
            for (int i = 0; i < n; i++) { var av = (ns[i * sd] + ns[i * sd + sides]).normalized; ns[i * sd] = av; ns[i * sd + sides] = av; }
            mesh.normals = ns;
            dst.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.identity });
        }

        /// <summary>葉の 房。カメラ（南から 北）に 真横を 向けない（KiV5 と 同じ 決まり）。少し 下へ 垂れる</summary>
        void HaCards(Vector3 at, float rr, int n, float ookisa) {
            for (int i = 0; i < n; i++) {
                float cs = ookisa * Random.Range(0.8f, 1.15f);
                float yaw = (Random.value < 0.5f ? 0f : 180f) + Random.Range(-40f, 40f);
                ha.Add(new CombineInstance {
                    mesh = Quad(),
                    transform = Matrix4x4.TRS(at + Random.insideUnitSphere * rr,
                                              Quaternion.Euler(Random.Range(5f, 25f), yaw, Random.Range(-15f, 15f)),
                                              new Vector3(cs, cs, 1f)),
                });
            }
        }

        /// <summary>まっすぐな 棒（垣の 杭・貫・丸太に つかう）。iro は kan の 番号</summary>
        public void Bou(Vector3 a, Vector3 b, float r0, float r1, int iro, int sides = 8) {
            var mid = (a + b) * 0.5f;
            Tube(kan[iro], new[] { a, mid, b }, new[] { r0, (r0 + r1) * 0.5f, r1 }, sides);
        }

        /// <summary>1本。iro 0=緑 1=黄ばみ 2=枯れ。soto＝藪の 外への 向き（xz）。katamuki＝傾きの 強さ 0..1</summary>
        public void Ueru(float x, float ybase, float z, float h, float r, int iro, Vector2 soto, float katamuki) {
            Kazu++;
            const int rings = 14;
            var pts = new Vector3[rings]; var rad = new float[rings];
            var p = new Vector3(x, ybase - 0.2f, z);
            var lean = new Vector3(soto.x, 0f, soto.y).normalized;
            float dan = (h + 0.2f) / (rings - 1);
            for (int i = 0; i < rings; i++) {
                float t01 = (float)i / (rings - 1);
                pts[i] = p;
                rad[i] = r * Mathf.Lerp(1f, 0.55f, t01 * t01);
                // ★上へ いくほど 外へ 弧を 描く（葉の 重み）。傾きの 強さは 藪の ふち・枯れ竹ほど 大きく
                var dir = (Vector3.up + lean * (katamuki * 1.4f * t01 * t01)
                           + new Vector3(Random.Range(-0.02f, 0.02f), 0f, Random.Range(-0.02f, 0.02f))).normalized;
                p += dir * dan;
            }
            Tube(kan[iro], pts, rad, 8);
            teppen.Add(pts[rings - 1]);
            var col = new GameObject("TakeAtari");
            col.transform.SetParent(root, false);
            col.transform.position = new Vector3(x, ybase + 1.6f, z);
            var cap = col.AddComponent<CapsuleCollider>();
            cap.radius = Mathf.Max(0.06f, r); cap.height = 3.2f;

            // 枝は 上半分の 節から。左右 交互に 出て、外へ 上がって 先で 垂れる。枯れ竹は 葉を 落とす
            bool kareta = iro == 2;
            float haOokisa = Mathf.Lerp(0.55f, 0.85f, Mathf.InverseLerp(5f, 9f, h));
            int edaKazu = 0;
            for (int i = Mathf.RoundToInt(rings * 0.45f); i < rings; i++) {
                if (Random.value < 0.25f) continue;
                var moto = pts[i];
                float yaw = (edaKazu % 2 == 0 ? 0f : 180f) + Random.Range(-70f, 70f);
                edaKazu++;
                var yoko = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                float agari = Random.Range(20f, 45f) * Mathf.Deg2Rad;
                var bdir = (yoko * Mathf.Cos(agari) + Vector3.up * Mathf.Sin(agari)).normalized;
                float blen = Random.Range(0.55f, 1.1f);
                var bp = new Vector3[3];
                bp[0] = moto; bp[1] = moto + bdir * (blen * 0.55f);
                bp[2] = moto + bdir * blen + Vector3.down * (blen * 0.25f);    // 先は 垂れる
                var br = new float[] { r * 0.30f, r * 0.18f, r * 0.08f };
                Tube(kan[iro], bp, br, 6);
                if (!kareta) {
                    HaCards(bp[1] + Vector3.down * 0.1f, 0.25f, 2, haOokisa);
                    HaCards(bp[2] + Vector3.down * 0.15f, 0.30f, 3, haOokisa);
                } else if (Random.value < 0.25f) {
                    HaCards(bp[2], 0.2f, 1, haOokisa * 0.7f);
                }
            }
        }

        void Housen(Mesh mesh) {
            if (teppen.Count == 0) return;
            var vs = mesh.vertices; var ns = new Vector3[vs.Length];
            for (int i = 0; i < vs.Length; i++) {
                var v = vs[i]; float best = float.MaxValue; var c = v;
                foreach (var k in teppen) { var d = new Vector3(k.x - v.x, (k.y - v.y) * 0.3f, k.z - v.z); if (d.sqrMagnitude < best) { best = d.sqrMagnitude; c = k; } }
                var o = v - c; o.y *= 0.3f;
                ns[i] = (o.sqrMagnitude < 1e-4f ? Vector3.up : (o.normalized + Vector3.up * 0.7f).normalized);
            }
            mesh.normals = ns;
        }

        GameObject Katamari(string name, List<CombineInstance> cis, Material m, bool isHa) {
            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.CombineMeshes(cis.ToArray(), true, true);
            if (isHa) Housen(mesh);
            var g = new GameObject(name);
            g.transform.SetParent(root, false);
            g.layer = 0;
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial = m;
            return g;
        }

        public void Katameru() {
            string[] kawa = { "take_kawa_midori.jpg", "take_kawa_ki.jpg", "take_kawa_cha.jpg", "ie_ki.jpg", "ki_kawa.jpg" };
            string[] nm = { "Midori", "Ki", "Cha", "KuiKi", "KiKawa" };
            for (int i = 0; i < 5; i++)
                if (kan[i].Count > 0) Katamari("TakeKan" + nm[i], kan[i], Mat("NiwaTakeKan" + nm[i], kawa[i], false), false);
            string haTex = Aru("take_ha.png") ? "take_ha.png" : "ki_ha.png";
            if (ha.Count > 0) Katamari("TakeHa", ha, Mat("NiwaTakeHa", haTex, true), true);
            Debug.Log("[Probe] TakeV1 " + Kazu + "本 葉の絵=" + haTex);
        }
    }

    /// <summary>藪を 1つ 植える。中心 (cx,cz)、ひろがり rx×rz、n本。色は 緑60・黄15・茶25。
    /// ふちの 稈ほど 外へ 傾き、枯れ竹は さらに 傾く</summary>
    public static Yabu Mure(Transform root, float cx, float cz, float rx, float rz, int n,
                            System.Func<float, float, float> jimenY) {
        var yabu = new Yabu(root);
        for (int i = 0; i < n; i++) {
            var d = Random.insideUnitCircle;
            float x = cx + d.x * rx, z = cz + d.y * rz;
            float fuchi = Mathf.Clamp01(d.magnitude);                     // 0=まん中 1=ふち
            float roll = Random.value;
            int iro = roll < 0.60f ? 0 : roll < 0.75f ? 1 : 2;
            float h = Random.Range(5.5f, 9.0f) * (iro == 2 ? Random.Range(0.75f, 1f) : 1f);
            float r = Random.Range(0.035f, 0.06f);
            float katamuki = Mathf.Lerp(0.15f, 0.55f, fuchi) + (iro == 2 ? Random.Range(0.2f, 0.5f) : 0f);
            var soto = d.sqrMagnitude < 1e-4f ? Random.insideUnitCircle.normalized : d.normalized;
            yabu.Ueru(x, jimenY(x, z), z, h, r, iro, soto, katamuki);
        }
        yabu.Katameru();
        return yabu;
    }

    // ---------------------------------------------------------------- 四ツ目垣・冠木門・丸太・岩
    // ★本人「柵はｍ柵じゃなくて日本の田舎っぽいやつにしよう」（2026-09-03）。
    //   田舎の 家の 庭の 囲いは **四ツ目垣**（杭に 竹の 貫を 2本 渡し、竹の 立子を 前後 交互に 結う）が 定番。
    //   門は 柱 2本に 冠木を 渡した **冠木門**（扉は 無い・田舎は 開けっぱなし）
    /// <summary>四ツ目垣を a→b に。高さ takasa。地めんの 高さは jimenY で 拾う</summary>
    public static void Kaki(Yabu y, Vector3 a, Vector3 b, float takasa, System.Func<float, float, float> jimenY) {
        var d = b - a; d.y = 0f; float len = d.magnitude; if (len < 0.1f) return;
        var dir = d / len; var yoko = Vector3.Cross(Vector3.up, dir);
        int kui = Mathf.Max(1, Mathf.RoundToInt(len / 1.8f));
        // 杭（木・少し 太い）
        for (int i = 0; i <= kui; i++) {
            var p = a + dir * (len * i / kui); p.y = jimenY(p.x, p.z) - 0.05f;
            y.Bou(p, p + Vector3.up * (takasa + 0.12f), 0.055f, 0.045f, 3, 8);
        }
        // 貫（竹・2段）
        foreach (float h in new[] { takasa * 0.40f, takasa * 0.80f }) {
            var p0 = a; p0.y = jimenY(a.x, a.z) + h; var p1 = b; p1.y = jimenY(b.x, b.z) + h;
            y.Bou(p0 - dir * 0.05f, p1 + dir * 0.05f, 0.024f, 0.024f, 1, 6);
        }
        // 立子（竹・前後 交互）
        int n = Mathf.Max(1, Mathf.RoundToInt(len / 0.30f));
        for (int i = 0; i <= n; i++) {
            float t = (float)i / n;
            var p = a + dir * (len * t) + yoko * ((i % 2 == 0) ? 0.03f : -0.03f);
            p.y = jimenY(p.x, p.z) + 0.02f;
            y.Bou(p, p + Vector3.up * (takasa + Random.Range(-0.04f, 0.06f)), 0.018f, 0.015f, (Random.value < 0.8f) ? 1 : 2, 6);
        }
    }

    /// <summary>冠木門：柱 2本 ＋ 冠木。at＝門の まん中、haba＝柱の 間、muki＝柱を ならべる 向き</summary>
    public static void Mon(Yabu y, Vector3 at, float haba, Vector3 muki, float takasa, System.Func<float, float, float> jimenY) {
        muki.y = 0f; muki.Normalize();
        var l = at - muki * (haba * 0.5f); var r = at + muki * (haba * 0.5f);
        l.y = jimenY(l.x, l.z) - 0.05f; r.y = jimenY(r.x, r.z) - 0.05f;
        y.Bou(l, l + Vector3.up * takasa, 0.09f, 0.08f, 3, 10);
        y.Bou(r, r + Vector3.up * takasa, 0.09f, 0.08f, 3, 10);
        var top = Vector3.up * (takasa - 0.10f);
        y.Bou(l + top - muki * 0.25f, r + top + muki * 0.25f, 0.07f, 0.07f, 3, 8);
    }

    /// <summary>丸太：木の 皮の 筒を 横に。両はしが ふさがる よう まん中から 2本</summary>
    public static void Maruta(Yabu y, Vector3 at, float yaw, float len, float r) {
        var dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        var c = at + Vector3.up * (r * 0.9f);
        y.Bou(c, c + dir * (len * 0.5f), r, r * 0.92f, 4, 10);
        y.Bou(c, c - dir * (len * 0.5f), r, r * 0.92f, 4, 10);
    }

    static Material mIwa;
    /// <summary>岩：ゆがめた 球（下は 平ら）に 岩はだの 絵。size＝差しわたし(m)</summary>
    public static void Iwa(Transform root, Vector3 at, float size, float yaw) {
        if (mIwa == null) mIwa = Mat("NiwaIwa", "iwa_hada.jpg", false);
        const int la = 7, lo = 12;
        var v = new List<Vector3>(); var uv = new List<Vector2>();
        float seed = Random.Range(0f, 100f);
        for (int i = 0; i <= la; i++) {
            float ph = Mathf.PI * i / la;                       // 0=上 π=下
            for (int j = 0; j <= lo; j++) {
                float th = Mathf.PI * 2f * j / lo;
                var n = new Vector3(Mathf.Sin(ph) * Mathf.Cos(th), Mathf.Cos(ph), Mathf.Sin(ph) * Mathf.Sin(th));
                float bump = 0.72f + 0.28f * Mathf.PerlinNoise(seed + n.x * 1.7f + n.y * 0.9f, seed + n.z * 1.7f);
                var p = n * (size * 0.5f * bump);
                p.y *= 0.62f;                                   // 平たい 岩
                if (p.y < -size * 0.12f) p.y = -size * 0.12f;   // 下は 地めんに うまる
                v.Add(p); uv.Add(new Vector2((float)j / lo * 2f, (float)i / la));
            }
        }
        var tri = new List<int>();
        for (int i = 0; i < la; i++)
            for (int j = 0; j < lo; j++) {
                int a0 = i * (lo + 1) + j, a1 = a0 + 1, b0 = a0 + lo + 1, b1 = b0 + 1;
                tri.Add(a0); tri.Add(b0); tri.Add(a1); tri.Add(a1); tri.Add(b0); tri.Add(b1);
            }
        var m = new Mesh { name = "Iwa" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        var g = new GameObject("Iwa");
        g.transform.SetParent(root, false);
        g.transform.position = at + Vector3.up * (size * 0.12f);
        g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        g.AddComponent<MeshFilter>().sharedMesh = m;
        g.AddComponent<MeshRenderer>().sharedMaterial = mIwa;
        var col = g.AddComponent<SphereCollider>(); col.radius = size * 0.42f; col.center = Vector3.zero;
    }

    // ---------------------------------------------------------------- 写真の 草むら
    static Material mKusa;
    /// <summary>草むら 1株＝写真の 板を 十字に 2まい。絵（kusa_kabu.png）が 無ければ false（呼ぶ がわは ローポリで 代用）</summary>
    public static bool KusaKabu(Transform root, Vector3 at, float yaw, float takasa) {
        if (!Aru("kusa_kabu.png")) return false;
        if (mKusa == null) mKusa = Mat("NiwaKusaKabu", "kusa_kabu.png", true);
        var g = new GameObject("KusaKabu");
        g.transform.SetParent(root, false);
        g.transform.position = at;
        g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        for (int i = 0; i < 2; i++) {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.name = "Ita";
            q.transform.SetParent(g.transform, false);
            q.transform.localPosition = new Vector3(0f, takasa * 0.5f, 0f);
            q.transform.localRotation = Quaternion.Euler(0f, i * 90f, 0f);
            q.transform.localScale = new Vector3(takasa * 1.1f, takasa, 1f);
            var mf = q.GetComponent<MeshFilter>();
            // 法線は ぜんぶ 上（草の 板で 実証ずみ：横向きの 法線だと 片面が まっ黒に なる）
            var m = Object.Instantiate(mf.sharedMesh);
            var ns = new Vector3[m.vertexCount]; for (int k = 0; k < ns.Length; k++) ns[k] = Vector3.up;
            m.normals = ns; mf.sharedMesh = m;
            var mr = q.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mKusa;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        return true;
    }
}
