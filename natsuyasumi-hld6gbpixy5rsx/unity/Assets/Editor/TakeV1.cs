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
        readonly List<CombineInstance>[] kan = { new List<CombineInstance>(), new List<CombineInstance>(), new List<CombineInstance>() };
        readonly List<CombineInstance> ha = new List<CombineInstance>();
        readonly List<Vector3> teppen = new List<Vector3>();     // 葉の 法線の もと（稈の 上の ほう）
        public int Kazu;

        public Yabu(Transform root) { this.root = root; }

        void Tube(List<CombineInstance> dst, Vector3[] pts, float[] rad, int sides) {
            int n = pts.Length;
            var verts = new Vector3[n * sides + 1];
            var uv = new Vector2[verts.Length];
            var nrm = Vector3.Cross((pts[1] - pts[0]).normalized, Vector3.right);
            if (nrm.sqrMagnitude < 0.01f) nrm = Vector3.forward; else nrm = nrm.normalized;
            float vlen = 0f;
            for (int i = 0; i < n; i++) {
                var dir = (i == 0 ? pts[1] - pts[0] : i == n - 1 ? pts[n - 1] - pts[n - 2] : pts[i + 1] - pts[i - 1]).normalized;
                nrm = (nrm - dir * Vector3.Dot(nrm, dir)).normalized;
                var bin = Vector3.Cross(dir, nrm);
                if (i > 0) vlen += (pts[i] - pts[i - 1]).magnitude;
                for (int s = 0; s < sides; s++) {
                    float a = s * Mathf.PI * 2f / sides;
                    verts[i * sides + s] = pts[i] + (nrm * Mathf.Cos(a) + bin * Mathf.Sin(a)) * rad[i];
                    uv[i * sides + s] = new Vector2((float)s / sides, vlen * 0.8f);
                }
            }
            verts[n * sides] = pts[n - 1];
            uv[n * sides] = new Vector2(0.5f, vlen * 0.8f + 0.2f);
            var tris = new List<int>();
            for (int i = 0; i < n - 1; i++)
                for (int s = 0; s < sides; s++) {
                    int s2 = (s + 1) % sides;
                    int a0 = i * sides + s, a1 = i * sides + s2, b0 = (i + 1) * sides + s, b1 = (i + 1) * sides + s2;
                    tris.Add(a0); tris.Add(a1); tris.Add(b0);
                    tris.Add(a1); tris.Add(b1); tris.Add(b0);
                }
            for (int s = 0; s < sides; s++) { int s2 = (s + 1) % sides; tris.Add((n - 1) * sides + s); tris.Add(n * sides); tris.Add((n - 1) * sides + s2); }
            var mesh = new Mesh { vertices = verts, uv = uv, triangles = tris.ToArray() };
            mesh.RecalculateNormals();
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
            string[] kawa = { "take_kawa_midori.jpg", "take_kawa_ki.jpg", "take_kawa_cha.jpg" };
            string[] nm = { "Midori", "Ki", "Cha" };
            for (int i = 0; i < 3; i++)
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
