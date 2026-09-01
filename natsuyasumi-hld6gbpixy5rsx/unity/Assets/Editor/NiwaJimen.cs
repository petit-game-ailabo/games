using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// 地面まわり（2026-08-30）。
//
// ★接地の 影（まわりこみの 暗さ＝アンビエントオクルージョン）
//   本人「田舎の地面ってこんな感じだっけ？」→ 調べたら **物の 根元が まったく 暗く なって
//   いない**。実測（昼の 庭）：塀の 柱の 根元の 草 (90,100,47) / 20px よこの 草 (67,98,38)
//   ＝根元の ほうが 明るい。だから 塀も 木も 地面に 貼りついた 絵に 見える。
//   太陽の 影は 出て いるが、真昼は 太陽が ほぼ 真上で 影が 物の 下に 隠れて 消える。
//   ぼくなつの 地面に いつも ある 根元の 暗い 輪は 太陽の 影では なく、時刻に よらない
//   まわりこみの 暗さ。プリレンダの 一枚絵なので 最初から 描きこまれて いる。
//
// ★ふちの ぼかしは **世界の 長さ**で なければ ならない（1回目の しくじり）
//   はじめ「足もとの 大きさ × 倍率」で 板を 作ったら、家の 影が 2m も 外へ 広がって
//   大きな 四角い 敷物に 見えた。まわりこみの 暗さは 物の 大きさに よらず 根元から
//   数十cm。なので 板は **足もと ＋ 一定の のりしろ**に して、のりしろの ぶんだけを
//   ぼかす。板ごとに 縮尺が ちがうので テクスチャでは そろわない → 頂点の 色で 持つ
//   （9枚に 割った 面。内がわ4点＝濃い / 外がわ12点＝透明）
//
// ★物は **root の 直下ごと**に ひとまとめ（家の 壁や 床を 別べつに 敷くと 影が 重なって
//   まっ黒に なる）
public static class NiwaJimen {
    // 影を 敷かない もの（地面 じしん・描き割り・あたり判定・主人公・仕組みの 入れもの）
    static readonly string[] NUKI = {
        "Jimen", "JimenE", "MichiSoto", "TakadaiMichi", "Sora", "Satoyama", "YamaToi", "Kumo",
        "BLK_", "Marisa", "Kage", "Cam", "Sun", "Day", "Volume", "Takadai",
        "Ie",     // ★家は L字。外接矩形で 敷くと 切りかき（空き地）が 暗く なる
                  //   → BuildNiwa が Kaku() で 2つに 分けて 敷く
        "KiMiki", "KiHa", "KiAtari",   // 木は 塊なので 1本ずつ Ki() で 敷く
    };

    static bool Nuku(string n) {
        foreach (var k in NUKI) if (n.StartsWith(k)) return true;
        return false;
    }

    public static Material KageMat() {
        const string path = "Assets/Art/Materials/Niwa/NiwaKage.mat";
        System.IO.Directory.CreateDirectory("Assets/Art/Materials/Niwa");
        var sh = Shader.Find("Niwa/Kage");
        if (sh == null) { Debug.LogError("[NiwaJimen] Niwa/Kage が 見つからない"); return null; }
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
        m.shader = sh;
        return m;
    }

    /// <summary>足もと(w x d)＋のりしろ nori の 9枚割りの 面。内がわ4点だけ 濃い</summary>
    public static Mesh Ita(float w, float d, float nori, float koi) {
        float xi = w * 0.5f, zi = d * 0.5f;
        float xo = xi + nori, zo = zi + nori;
        float[] xs = { -xo, -xi, xi, xo };
        float[] zs = { -zo, -zi, zi, zo };
        var v = new Vector3[16];
        var c = new Color[16];
        for (int j = 0; j < 4; j++)
            for (int i = 0; i < 4; i++) {
                int k = j * 4 + i;
                v[k] = new Vector3(xs[i], 0f, zs[j]);
                bool uchi = (i == 1 || i == 2) && (j == 1 || j == 2);
                c[k] = new Color(0f, 0f, 0f, uchi ? koi : 0f);
            }
        var tri = new List<int>(54);
        for (int j = 0; j < 3; j++)
            for (int i = 0; i < 3; i++) {
                int a = j * 4 + i, b = a + 1, e = a + 4, f = e + 1;
                tri.Add(a); tri.Add(e); tri.Add(f);
                tri.Add(a); tri.Add(f); tri.Add(b);
            }
        var m = new Mesh { name = "KageIta" };
        m.vertices = v; m.colors = c; m.triangles = tri.ToArray();
        m.RecalculateBounds();
        return m;
    }

    /// <summary>足もと(w x d)＋のりしろ nori の **だ円**の 輪。木や 岩の 影は 四角では おかしい</summary>
    public static Mesh Maru(float w, float d, float nori, float koi) {
        const int K = 24;
        var v = new Vector3[K * 2 + 1];
        var c = new Color[K * 2 + 1];
        v[0] = Vector3.zero; c[0] = new Color(0f, 0f, 0f, koi);
        for (int i = 0; i < K; i++) {
            float t = i / (float)K * Mathf.PI * 2f;
            float cs = Mathf.Cos(t), sn = Mathf.Sin(t);
            v[1 + i] = new Vector3(cs * w * 0.5f, 0f, sn * d * 0.5f);
            c[1 + i] = new Color(0f, 0f, 0f, koi);
            v[1 + K + i] = new Vector3(cs * (w * 0.5f + nori), 0f, sn * (d * 0.5f + nori));
            c[1 + K + i] = new Color(0f, 0f, 0f, 0f);
        }
        var tri = new List<int>(K * 9);
        for (int i = 0; i < K; i++) {
            int a = 1 + i, b = 1 + (i + 1) % K;
            tri.Add(0); tri.Add(a); tri.Add(b);
            int e = 1 + K + i, f = 1 + K + (i + 1) % K;
            tri.Add(a); tri.Add(e); tri.Add(f);
            tri.Add(a); tri.Add(f); tri.Add(b);
        }
        var m = new Mesh { name = "KageMaru" };
        m.vertices = v; m.colors = c; m.triangles = tri.ToArray();
        m.RecalculateBounds();
        return m;
    }

    // 丸い 影に する もの（木・岩・草など）。塀や 家は 四角の まま
    static readonly string[] MARUI = {
        "tree", "rock", "stone", "grass", "flower", "crops", "pot", "log", "mushroom", "bush",
    };
    static bool Marui(string n) {
        string l = n.ToLowerInvariant();
        foreach (var k in MARUI) if (l.Contains(k)) return true;
        return false;
    }

    /// <summary>**宙に 浮いて いる 物**を さがす（2026-09-02）。
    /// 本人「まだ浮いてる柱がある。再度確認して」＝目で さがすのを やめて 機械で 落とす。
    /// 下が 0.45mより 高い のに、その 真下（XZが かさなる ところ）に 何も 無い ものを 出す</summary>
    public static void Uki(Transform oya, string dare) {
        var rs = new List<Renderer>();
        foreach (var r in oya.GetComponentsInChildren<Renderer>())
            if (r != null && r.enabled) rs.Add(r);
        int n = 0;
        var sb = new System.Text.StringBuilder();
        foreach (var r in rs) {
            var b = r.bounds;
            if (b.min.y <= 0.45f) continue;                 // 地めんに ついて いる
            bool sasae = false;
            foreach (var o in rs) {
                if (o == r) continue;
                var c = o.bounds;
                if (c.max.y < b.min.y - 0.12f) continue;    // ずっと 下＝ささえに ならない
                if (c.min.y > b.min.y + 0.02f) continue;    // 自分より 上だけ
                // ★**真下に かさなる 面積**が ある か。横で 接して いる だけは 支えに しない
                float kx = Mathf.Min(c.max.x, b.max.x) - Mathf.Max(c.min.x, b.min.x);
                float kz = Mathf.Min(c.max.z, b.max.z) - Mathf.Max(c.min.z, b.min.z);
                if (kx <= 0.02f || kz <= 0.02f) continue;
                float jibun = Mathf.Max(0.01f, (b.max.x - b.min.x) * (b.max.z - b.min.z));
                if (kx * kz < jibun * 0.20f) continue;      // 2わり 以上 かさなって いる こと
                sasae = true; break;
            }
            if (!sasae) {
                n++;
                if (n <= 20) sb.Append(" ").Append(r.transform.name)
                               .Append(b.center.ToString("F1"));
            }
        }
        Debug.Log("[Probe] うき " + dare + " " + n + "こ" + sb);
    }

    /// <summary>四角い 影を 1つ 敷く（L字の 家など、外接矩形では 合わない もの）</summary>
    public static void Kaku(Transform root, string name, float x0, float x1, float z0, float z1,
                            float takasa) {
        var mat = KageMat();
        if (mat == null) return;
        float w = x1 - x0, d = z1 - z0;
        float cx = (x0 + x1) * 0.5f, cz = (z0 + z1) * 0.5f;
        float nori = Mathf.Clamp(0.10f + takasa * 0.06f, 0.14f, 0.38f);
        float koi = Mathf.Clamp(0.20f + takasa * 0.025f, 0.20f, 0.33f);
        float h0 = NiwaJimenE.Takasa(cx, cz);
        var go = new GameObject("Kage_" + name);
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(cx, h0 + 0.05f, cz);
        var ms = Ita(w, d, nori, koi);
        var vs = ms.vertices;
        for (int i = 0; i < vs.Length; i++)
            vs[i].y = NiwaJimenE.Takasa(cx + vs[i].x, cz + vs[i].z) - h0;
        ms.vertices = vs; ms.RecalculateBounds();
        go.AddComponent<MeshFilter>().sharedMesh = ms;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    /// <summary>木 1本ぶんの 影。林は メッシュを 結合して しまう ので 別に 敷く。
    /// moto = (x, ybase, z, みきの ふとさ)</summary>
    public static void Ki(Transform root, List<Vector4> moto) {
        var mat = KageMat();
        if (mat == null || moto == null) return;
        var oya = new GameObject("KageKi").transform;
        oya.SetParent(root, false);
        foreach (var m in moto) {
            float w = Mathf.Max(1.4f, m.w * 5.5f);        // 枝の ひろがりぶん
            var go = new GameObject("Kage_Ki");
            go.transform.SetParent(oya, false);
            go.transform.position = new Vector3(m.x, m.y + 0.05f, m.z);
            var ms = Maru(w, w, 0.6f, 0.34f);
            var vs = ms.vertices;
            for (int i = 0; i < vs.Length; i++)
                vs[i].y = NiwaJimenE.Takasa(m.x + vs[i].x, m.z + vs[i].z) - m.y;
            ms.vertices = vs; ms.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = ms;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }
        Debug.Log("[Probe] KageKi " + moto.Count);
    }

    /// <summary>場面の 物 ぜんぶの 足もとに 影を 敷く。組み立ての いちばん さいごに 呼ぶ</summary>
    public static int Setchi(Transform root) {
        var mat = KageMat();
        if (mat == null) return 0;
        var oya = new GameObject("KageOya").transform;
        oya.SetParent(root, false);

        // 直下の 子を 先に 控える（作りながら まわすと 自分の 影も 拾う）
        var ko = new List<Transform>();
        for (int i = 0; i < root.childCount; i++) ko.Add(root.GetChild(i));

        int n = 0;
        foreach (var t in ko) {
            if (t == null || Nuku(t.name)) continue;
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) continue;
            Bounds b = default; bool aru = false;
            foreach (var r in rs) {
                if (r == null || !r.enabled || r is ParticleSystemRenderer) continue;
                if (Nuku(r.transform.name)) continue;
                if (!aru) { b = r.bounds; aru = true; } else b.Encapsulate(r.bounds);
            }
            if (!aru) continue;
            if (b.min.y > 0.8f) continue;                 // 地面から 浮いて いる
            float w = b.size.x, d = b.size.z, h = Mathf.Max(0.05f, b.size.y);
            if (w < 0.10f || d < 0.10f) continue;
            if (w > 30f || d > 30f) continue;             // 地面なみに 大きい ものは 対象外

            // のりしろ＝根元から 影が とどく 長さ。背が 高いほど 少し 広いが 上限 0.5m
            float nori = Mathf.Clamp(0.10f + h * 0.06f, 0.14f, 0.38f);
            float koi  = Mathf.Clamp(0.20f + h * 0.025f, 0.20f, 0.33f);

            var go = new GameObject("Kage_" + t.name);
            go.transform.SetParent(oya, false);
            float h0 = NiwaJimenE.Takasa(b.center.x, b.center.z);
            go.transform.position = new Vector3(b.center.x, h0 + 0.05f, b.center.z);
            var ms = Marui(t.name) ? Maru(w, d, nori, koi) : Ita(w, d, nori, koi);
            // 地ばんが 凸凹に なった ので 影も そわせる（平らな ままだと 土に もぐる）
            var vs = ms.vertices;
            for (int i = 0; i < vs.Length; i++)
                vs[i].y = NiwaJimenE.Takasa(b.center.x + vs[i].x, b.center.z + vs[i].z) - h0;
            ms.vertices = vs; ms.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = ms;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            n++;
        }
        Debug.Log("[NiwaJimen] setchi=" + n);
        return n;
    }
}
