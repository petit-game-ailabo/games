using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// 庭に 置く 小さな 部品の 型（2026-09-05）。納屋(NiwaNaya)と 水まわり(NiwaMizu)で つかう。
//
// ★当たりは **既定で 外す**。道具や 花は 通りみちに 出っぱるので、当たりを のこすと
//   庭を 歩くたび 見えない 石に つまずく。当たりを つけるのは 壁と 屋根の 本体だけ。
// ★UV は **m で 焼く**（箱の 0〜1 UV を そのまま つかうと 0.2mの バケツと 2mの 壁で
//   絵の こまかさが 10倍 ちがう）。筒・輪・管は ここで m を 入れて 作る。
public static class NiwaBuhin {
    const string TEX = "Assets/Art/Textures/";
    const string DIR = "Assets/Art/Materials/Niwa";

    // ---------------------------------------------------------------- 材質
    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

    /// <summary>絵つきの 材質。tiling は 面の 大きさ÷1くりかえしの m。dither＝主人公の まわりを 抜く</summary>
    public static Material Mat(string name, string tex, Vector2 tiling, Color tint,
                               bool sukashi = false, bool dither = false, float tsuya = 0.05f) {
        Material got;
        if (cache.TryGetValue(name, out got)) return got;
        System.IO.Directory.CreateDirectory(DIR);
        string path = DIR + "/" + name + ".mat";
        var sh = dither ? Shader.Find("Natsuyasumi/DitherLit") : null;
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
        m.shader = sh;
        if (tex != null) {
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX + tex);
            if (t == null) Debug.LogError("[NiwaBuhin] 絵が ない: " + tex);
            else {
                m.SetTexture("_BaseMap", t); m.SetTextureScale("_BaseMap", tiling);
                m.mainTexture = t; m.mainTextureScale = tiling;
                string np = TEX + tex.Substring(0, tex.LastIndexOf('.')) + "_n.png";
                var nt = AssetDatabase.LoadAssetAtPath<Texture2D>(np);
                if (nt != null && m.HasProperty("_BumpMap")) {
                    m.SetTexture("_BumpMap", nt); m.SetTextureScale("_BumpMap", tiling);
                    m.EnableKeyword("_NORMALMAP");
                } else m.DisableKeyword("_NORMALMAP");
            }
        } else { m.SetTexture("_BaseMap", null); m.mainTexture = null; }
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", tsuya);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
        m.color = tint;
        if (sukashi) {
            m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        }
        cache[name] = m;
        return m;
    }

    /// <summary>絵の こまかさを 面の 大きさで そろえた 材質（箱の UV は どの 面も 0〜1）</summary>
    public static Material Fit(string prefix, string tex, float w, float h, float tileM,
                               Color tint, bool dither = false) {
        string k = prefix + "_" + Mathf.RoundToInt(w * 20) + "_" + Mathf.RoundToInt(h * 20);
        return Mat("NB_" + k, tex, new Vector2(w / tileM, h / tileM), tint, false, dither);
    }

    // ---------------------------------------------------------------- 箱・棒
    /// <summary>箱。atari＝当たりを のこす か（既定は 外す）</summary>
    public static GameObject Hako(Transform p, string name, Vector3 c, Vector3 s,
                                  Material m, bool atari = false) {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name; g.transform.SetParent(p, false);
        g.transform.localPosition = c; g.transform.localScale = s;
        if (m != null) g.GetComponent<Renderer>().sharedMaterial = m;
        if (!atari) Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }

    /// <summary>まわした 箱（立てかけた 板・斜めの 段）</summary>
    public static GameObject HakoR(Transform p, string name, Vector3 c, Vector3 s, Vector3 kaiten,
                                   Material m, bool atari = false) {
        var g = Hako(p, name, c, s, m, atari);
        g.transform.localRotation = Quaternion.Euler(kaiten);
        return g;
    }

    /// <summary>丸い 棒。a→b を 太さ r で つなぐ（円筒の 素は 高さ2・径1）</summary>
    public static GameObject Bou(Transform p, string name, Vector3 a, Vector3 b, float r,
                                 Material m, bool atari = false) {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name = name; g.transform.SetParent(p, false);
        var d = b - a;
        g.transform.localPosition = (a + b) * 0.5f;
        g.transform.localRotation = d.sqrMagnitude < 1e-8f ? Quaternion.identity
                                  : Quaternion.FromToRotation(Vector3.up, d.normalized);
        g.transform.localScale = new Vector3(r * 2f, d.magnitude * 0.5f, r * 2f);
        if (m != null) g.GetComponent<Renderer>().sharedMaterial = m;
        if (!atari) Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }

    // ---------------------------------------------------------------- メッシュ
    public static GameObject Mesh1(Transform p, string name, Mesh mesh, Material m,
                                   bool kage = true) {
        var g = new GameObject(name);
        g.transform.SetParent(p, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = g.AddComponent<MeshRenderer>();
        mr.sharedMaterial = m;
        if (!kage) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return g;
    }

    /// <summary>すぼまった 筒（バケツ・じょうろ・植木鉢）。底は y=0、口は y=h</summary>
    public static Mesh Tsutsu(string name, float rShita, float rUe, float h, bool soko,
                              float texM, int seg = 16) {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        float mawari = Mathf.PI * 2f * Mathf.Max(rShita, rUe);
        for (int i = 0; i <= seg; i++) {
            float a = i * Mathf.PI * 2f / seg;
            float u = mawari * i / seg / texM;
            v.Add(new Vector3(Mathf.Cos(a) * rShita, 0f, Mathf.Sin(a) * rShita)); uv.Add(new Vector2(u, 0f));
            v.Add(new Vector3(Mathf.Cos(a) * rUe, h, Mathf.Sin(a) * rUe)); uv.Add(new Vector2(u, h / texM));
        }
        for (int i = 0; i < seg; i++) {
            int a0 = i * 2, a1 = a0 + 1, b0 = a0 + 2, b1 = a0 + 3;
            tri.Add(a0); tri.Add(a1); tri.Add(b0);
            tri.Add(a1); tri.Add(b1); tri.Add(b0);
        }
        if (soko) {                                   // 底（上から のぞくと 見える）
            int c = v.Count;
            v.Add(new Vector3(0f, 0.012f, 0f)); uv.Add(new Vector2(0.5f, 0.5f));
            for (int i = 0; i < seg; i++) {
                float a = i * Mathf.PI * 2f / seg;
                v.Add(new Vector3(Mathf.Cos(a) * rShita * 0.96f, 0.012f, Mathf.Sin(a) * rShita * 0.96f));
                uv.Add(new Vector2(i / (float)seg, 0f));
            }
            for (int i = 0; i < seg; i++) {
                tri.Add(c); tri.Add(c + 1 + i); tri.Add(c + 1 + (i + 1) % seg);
            }
        }
        var m = new Mesh { name = name, vertices = v.ToArray(), uv = uv.ToArray(), triangles = tri.ToArray() };
        m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
        return m;
    }

    /// <summary>輪（虫とり網の わく・ホースの 巻き）。xz 面に ねかせた ドーナツ</summary>
    public static Mesh Wa(string name, float R, float r, int oo = 20, int uu = 6, float texM = 0.4f) {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        for (int i = 0; i <= oo; i++) {
            float a = i * Mathf.PI * 2f / oo;
            var c = new Vector3(Mathf.Cos(a) * R, 0f, Mathf.Sin(a) * R);
            var e = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            for (int j = 0; j <= uu; j++) {
                float b = j * Mathf.PI * 2f / uu;
                v.Add(c + e * (Mathf.Cos(b) * r) + Vector3.up * (Mathf.Sin(b) * r));
                uv.Add(new Vector2(R * a / texM, r * b / texM));
            }
        }
        int w = uu + 1;
        for (int i = 0; i < oo; i++)
            for (int j = 0; j < uu; j++) {
                int a0 = i * w + j, a1 = a0 + 1, b0 = a0 + w, b1 = b0 + 1;
                tri.Add(a0); tri.Add(b0); tri.Add(a1);
                tri.Add(a1); tri.Add(b0); tri.Add(b1);
            }
        var m = new Mesh { name = name, vertices = v.ToArray(), uv = uv.ToArray(), triangles = tri.ToArray() };
        m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
        return m;
    }

    /// <summary>折れ線に そった 管（ホース）。太さは 一定</summary>
    public static Mesh Kan(string name, IList<Vector3> pts, float r, int sides = 8, float texM = 0.25f) {
        int n = pts.Count, sd = sides + 1;
        var v = new Vector3[n * sd]; var uv = new Vector2[v.Length];
        var nrm = Vector3.up;
        float len = 0f;
        for (int i = 0; i < n; i++) {
            var dir = (i == 0 ? pts[1] - pts[0]
                     : i == n - 1 ? pts[n - 1] - pts[n - 2]
                     : pts[i + 1] - pts[i - 1]).normalized;
            if (Mathf.Abs(Vector3.Dot(nrm, dir)) > 0.98f) nrm = Vector3.right;
            nrm = (nrm - dir * Vector3.Dot(nrm, dir)).normalized;
            var bin = Vector3.Cross(dir, nrm);
            if (i > 0) len += (pts[i] - pts[i - 1]).magnitude;
            for (int s = 0; s <= sides; s++) {
                float a = s * Mathf.PI * 2f / sides;
                v[i * sd + s] = pts[i] + (nrm * Mathf.Cos(a) + bin * Mathf.Sin(a)) * r;
                uv[i * sd + s] = new Vector2(s * 2f * Mathf.PI * r / sides / texM, len / texM);
            }
        }
        var tri = new List<int>();
        for (int i = 0; i < n - 1; i++)
            for (int s = 0; s < sides; s++) {
                int a0 = i * sd + s, a1 = a0 + 1, b0 = a0 + sd, b1 = b0 + 1;
                tri.Add(a0); tri.Add(a1); tri.Add(b0);
                tri.Add(a1); tri.Add(b1); tri.Add(b0);
            }
        var m = new Mesh { name = name, vertices = v, uv = uv, triangles = tri.ToArray() };
        m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
        return m;
    }

    /// <summary>切妻の 妻壁（三角柱）。yz 面の 三角を x 方向に atsumi だけ 厚く する</summary>
    public static Mesh Tsuma(string name, float han, float yKata, float yMune, float atsumi, float texM) {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        System.Action<Vector3, Vector3, Vector3> Men = (a, b, c) => {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c);
            uv.Add(new Vector2(a.z / texM, a.y / texM));
            uv.Add(new Vector2(b.z / texM, b.y / texM));
            uv.Add(new Vector2(c.z / texM, c.y / texM));
            tri.Add(i); tri.Add(i + 1); tri.Add(i + 2);
        };
        float t = atsumi * 0.5f;
        var L0 = new Vector3(-t, yKata, -han); var L1 = new Vector3(-t, yKata, han); var LT = new Vector3(-t, yMune, 0f);
        var R0 = new Vector3(t, yKata, -han); var R1 = new Vector3(t, yKata, han); var RT = new Vector3(t, yMune, 0f);
        Men(R0, R1, RT);                       // +x むき
        Men(L1, L0, LT);                       // -x むき
        Men(L0, R0, RT); Men(L0, RT, LT);      // -z の 斜辺
        Men(R1, L1, LT); Men(R1, LT, RT);      // +z の 斜辺
        Men(L0, L1, R1); Men(L0, R1, R0);      // 下ば
        var m = new Mesh { name = name, vertices = v.ToArray(), uv = uv.ToArray(), triangles = tri.ToArray() };
        m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
        return m;
    }
}
