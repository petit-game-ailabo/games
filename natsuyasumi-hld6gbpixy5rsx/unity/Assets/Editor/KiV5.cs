using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// チューブ木 v5（2026-08-31 に BuildMura から 取りだした）。
//
// ★由来：本人 2026-08-25「円柱の くっつく 部分が いびつ。ポリゴンを 増やして 精密に」
//   → 円柱の 継ぎ足しを やめ、**背骨に そって 輪を ならべて 面を 張る チューブ**で
//     みきも 枝も 1本の 連続した 皮に する（関節の 段差が 出ない）。
//   本人が「山の幹はすごくいい感じ。いったんこれでOK」と 止めた 形。**さわらない**。
// ★葉は カードの 房。自己発光は 廃止（夜に 光って 見えた・本人 2026-08-26）
// ★BuildMura の 中にも 同じ 形の ものが 残って いる。あちらは 凍結ずみの 場面なので
//   さわらない（この 版が これからの 正）
public static class KiV5 {
    static Mesh quadMesh;

    static Mesh Quad() {
        if (quadMesh != null) return quadMesh;
        var t = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadMesh = t.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(t);
        return quadMesh;
    }

    static Material Mat(string name, string tex, bool sukashi, Vector2 tiling) {
        string dir = "Assets/Art/Materials/Niwa";
        System.IO.Directory.CreateDirectory(dir);
        string path = dir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.shader = Shader.Find("Universal Render Pipeline/Lit");
        m.color = Color.white;
        m.SetFloat("_Smoothness", 0.05f);
        m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Art/Textures/shashin/" + tex);
        m.mainTextureScale = tiling;
        if (sukashi) {
            m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);   // 裏からも 見える
            m.DisableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
        }
        return m;
    }

    /// <summary>林。何本でも 植えて から まとめて 1つの メッシュに する
    /// （木 1本ずつ 別の 物に すると 描画が 本数ぶん かかる）</summary>
    public class Hayashi {
        readonly Transform root;
        readonly Dictionary<int, List<CombineInstance>> miki =
            new Dictionary<int, List<CombineInstance>>();
        readonly Dictionary<int, List<CombineInstance>> ha =
            new Dictionary<int, List<CombineInstance>>();
        /// <summary>木の 根もと（x, ybase, z）と みきの ふとさ。接地の 影に つかう</summary>
        public readonly List<Vector4> Moto = new List<Vector4>();
        int nowKey;

        public Hayashi(Transform root) { this.root = root; }

        static int Key(float x, float z) {
            return Mathf.FloorToInt(x / 24f) * 1000 + Mathf.FloorToInt(z / 24f);
        }
        static void Tsumu(Dictionary<int, List<CombineInstance>> d, int k, CombineInstance ci) {
            if (!d.TryGetValue(k, out var l)) d[k] = l = new List<CombineInstance>();
            l.Add(ci);
        }

        // ---- 背骨に そって 輪を ならべ、面を 張る（関節の 段差が 出ない）
        void Tube(Vector3[] pts, float[] rad, int sides) {
            int n = pts.Length;
            var verts = new Vector3[n * sides + 1];
            var uv = new Vector2[verts.Length];
            var nrm = Vector3.Cross((pts[1] - pts[0]).normalized, Vector3.right);
            if (nrm.sqrMagnitude < 0.01f) nrm = Vector3.forward; else nrm = nrm.normalized;
            float vlen = 0f;
            for (int i = 0; i < n; i++) {
                var dir = (i == 0 ? pts[1] - pts[0]
                         : i == n - 1 ? pts[n - 1] - pts[n - 2]
                         : pts[i + 1] - pts[i - 1]).normalized;
                nrm = (nrm - dir * Vector3.Dot(nrm, dir)).normalized;   // 前の 輪から 引きつぐ
                var bin = Vector3.Cross(dir, nrm);
                if (i > 0) vlen += (pts[i] - pts[i - 1]).magnitude;
                for (int s = 0; s < sides; s++) {
                    float a = s * Mathf.PI * 2f / sides;
                    verts[i * sides + s] = pts[i] + (nrm * Mathf.Cos(a) + bin * Mathf.Sin(a)) * rad[i];
                    uv[i * sides + s] = new Vector2((float)s / sides, vlen * 0.8f);
                }
            }
            verts[n * sides] = pts[n - 1];                              // 先端の ふさぎ
            uv[n * sides] = new Vector2(0.5f, vlen * 0.8f + 0.2f);
            var tris = new List<int>();
            for (int i = 0; i < n - 1; i++)
                for (int s = 0; s < sides; s++) {
                    int s2 = (s + 1) % sides;
                    int a0 = i * sides + s, a1 = i * sides + s2;
                    int b0 = (i + 1) * sides + s, b1 = (i + 1) * sides + s2;
                    tris.Add(a0); tris.Add(a1); tris.Add(b0);
                    tris.Add(a1); tris.Add(b1); tris.Add(b0);
                }
            for (int s = 0; s < sides; s++) {
                int s2 = (s + 1) % sides;
                tris.Add((n - 1) * sides + s); tris.Add(n * sides); tris.Add((n - 1) * sides + s2);
            }
            var mesh = new Mesh { vertices = verts, uv = uv, triangles = tris.ToArray() };
            mesh.RecalculateNormals();
            Tsumu(miki, nowKey, new CombineInstance { mesh = mesh, transform = Matrix4x4.identity });
        }

        void HaCards(Vector3 at, float rr, int n) {        // 枝先の 大量の 葉（world座標）
            for (int i = 0; i < n; i++) {
                float cs = Random.Range(1.9f, 3.1f);
                Tsumu(ha, nowKey, new CombineInstance {
                    mesh = Quad(),
                    transform = Matrix4x4.TRS(
                        at + Random.insideUnitSphere * rr,
                        Quaternion.Euler(Random.Range(-40f, 40f), Random.Range(0f, 360f),
                                         Random.Range(-25f, 25f)),
                        new Vector3(cs, cs * 0.8f, 1f)),
                });
            }
        }

        /// <summary>1本 植える。ybase＝根もとの 地めんの 高さ</summary>
        public void Ueru(float x, float ybase, float z, float h, float futosa) {
            nowKey = Key(x, z);
            Moto.Add(new Vector4(x, ybase, z, futosa));
            float r0 = futosa * 0.5f;
            // みきの 背骨＝輪 9つ。ゆるく 湾曲、根もとは ひろがり、上に いくほど 細い
            const int rings = 9;
            var pts = new Vector3[rings]; var rad = new float[rings];
            var p = new Vector3(x, ybase - 0.2f, z); var dir = Vector3.up;
            for (int i = 0; i < rings; i++) {
                pts[i] = p;
                float t01 = (float)i / (rings - 1);
                rad[i] = r0 * (i == 0 ? 1.55f : Mathf.Lerp(1.0f, 0.4f, t01))
                            * (1f + Random.Range(-0.05f, 0.05f));
                p += dir * ((h + 0.2f) / (rings - 1));
                dir = (dir + new Vector3(Random.Range(-0.08f, 0.08f), 0f,
                                         Random.Range(-0.08f, 0.08f))).normalized;
            }
            Tube(pts, rad, 12);
            var col = new GameObject("KiAtari");            // 当たりは カプセルで 別に
            col.transform.SetParent(root, false);
            col.transform.position = new Vector3(x, ybase + 1.7f, z);
            var cap = col.AddComponent<CapsuleCollider>();
            cap.radius = Mathf.Max(0.18f, r0); cap.height = 3.6f;
            // 枝＝3〜5本。みきの 輪の 位置から チューブで（先は 上へ しなる）
            int eda = Random.Range(3, 6);
            for (int e = 0; e < eda; e++) {
                int baseRing = Random.Range(4, 8);
                var moto = pts[baseRing];
                float yaw = (360f / eda) * e + Random.Range(-30f, 30f);
                float agari = Random.Range(22f, 48f) * Mathf.Deg2Rad;
                var yoko = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                var bdir = (yoko * Mathf.Cos(agari) + Vector3.up * Mathf.Sin(agari)).normalized;
                float blen = h * Random.Range(0.22f, 0.34f);
                const int bn = 5;
                var bp = new Vector3[bn]; var br = new float[bn];
                var q = moto; var d2 = bdir;
                float br0 = rad[baseRing] * 0.6f;
                for (int i = 0; i < bn; i++) {
                    bp[i] = q;
                    br[i] = br0 * Mathf.Lerp(1f, 0.22f, (float)i / (bn - 1));
                    q += d2 * (blen / (bn - 1));
                    d2 = (d2 + Vector3.up * 0.10f
                          + new Vector3(Random.Range(-0.08f, 0.08f), 0f,
                                        Random.Range(-0.08f, 0.08f))).normalized;
                }
                Tube(bp, br, 8);
                // 小枝 1〜2本（さらに 細い チューブ）＋葉
                int koeda = Random.Range(1, 3);
                for (int k2 = 0; k2 < koeda; k2++) {
                    var m2 = (d2 + new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.2f, 0.5f),
                                               Random.Range(-0.6f, 0.6f))).normalized;
                    float n2 = blen * Random.Range(0.4f, 0.6f);
                    var kmoto = bp[Random.Range(2, 4)];
                    var kp = new Vector3[3];
                    var kr = new float[3] { br0 * 0.35f, br0 * 0.22f, br0 * 0.1f };
                    kp[0] = kmoto; kp[1] = kmoto + m2 * (n2 * 0.5f);
                    kp[2] = kmoto + m2 * n2 + Vector3.up * 0.15f;
                    Tube(kp, kr, 6);
                    HaCards(kp[2], Random.Range(0.9f, 1.3f), 4);
                }
                HaCards(bp[3], Random.Range(0.9f, 1.2f), 3);                          // 枝の とちゅう
                HaCards(bp[bn - 1] + Vector3.up * 0.3f, Random.Range(1.0f, 1.5f), 5); // 枝の 先
            }
            // てっぺんの 冠＝房を 3つ 横に ならべて 層に（1つの 玉に しない）
            var teppen = pts[rings - 1];
            for (int c2 = 0; c2 < 3; c2++) {
                var off = Quaternion.Euler(0f, 120f * c2 + Random.Range(-30f, 30f), 0f)
                          * Vector3.forward * Random.Range(0.6f, 1.6f);
                HaCards(teppen + off + Vector3.up * Random.Range(0.2f, 0.9f),
                        Random.Range(1.1f, 1.6f), 6);
            }
        }

        static GameObject Katamari(Transform root, string name, List<CombineInstance> cis, Material m) {
            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.CombineMeshes(cis.ToArray(), true, true);
            var g = new GameObject(name);
            g.transform.SetParent(root, false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial = m;
            return g;
        }

        /// <summary>結合して 場面に 置く。24m四方ごとに 分ける（画角の 外は 描かない）</summary>
        public void Katameru() {
            // ★皮も 葉も 本人の 写真（2026-08-31）。前は 32x64px・5色の 点フィルタで、
            //   地面を 写真に した ぶん 木だけ 四角い 塊に 見えて いた。
            //   タイルは (1,1)＝みきの UVは たて 1.25mで 1回りなので ちょうど よい
            var mKawa = Mat("NiwaKiKawa", "ki_kawa.jpg", false, Vector2.one);
            var mHa = Mat("NiwaHaCard", "ki_ha.png", true, Vector2.one);
            int nm = 0, nh = 0;
            foreach (var kv in miki) { Katamari(root, "KiMiki" + kv.Key, kv.Value, mKawa); nm++; }
            foreach (var kv in ha) { Katamari(root, "KiHa" + kv.Key, kv.Value, mHa); nh++; }
            Debug.Log("[Probe] KiV5 " + Moto.Count + "本 みきの塊" + nm + " 葉の塊" + nh);
        }
    }
}
