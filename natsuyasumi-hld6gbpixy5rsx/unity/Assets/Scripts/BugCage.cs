using System.Collections.Generic;
using UnityEngine;

// 虫かご。**取った 虫が 実際に 中で 動いている**ように 見せる。
//
// 数字が 増えるだけだと 何も うれしくない。かごを 縁がわに 置いて、
// つかまえた 虫が そこに 入って いく＝**手もとに たまって いくのが 目に 見える**ように する。
// 入るのは 最後の 数ひきだけ（かごは 小さい）。
public class BugCage : MonoBehaviour {
    public Texture2D atlas;
    public int shown = 6;            // 中に 見せる 数
    public Vector3 inner = new Vector3(0.34f, 0.30f, 0.26f);

    readonly List<Transform> inside = new List<Transform>();
    readonly List<BugId> kinds = new List<BugId>();
    readonly Dictionary<BugId, Material> mats = new Dictionary<BugId, Material>();

    void Start() {
        var book = FindFirstObjectByType<BugBook>();
        if (book != null) book.OnCaught += (id, n, first) => Put(id);
    }

    public void Put(BugId id) {
        if (atlas == null) return;
        // いっぱいなら いちばん 古いのを 逃がす（見た目の 話。記録は 減らない）
        if (inside.Count >= shown) {
            var old = inside[0]; inside.RemoveAt(0); kinds.RemoveAt(0);
            if (old != null) Destroy(old.gameObject);
        }
        var kind = BugKind.Of(id);
        var go = new GameObject("Caged_" + id);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(
            Random.Range(-inner.x, inner.x) * 0.6f,
            Random.Range(0.06f, inner.y * 0.9f),
            Random.Range(-inner.z, inner.z) * 0.6f);

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.SetParent(go.transform, false);
        Destroy(quad.GetComponent<Collider>());
        // かごの 中では 小さめに 見せる（ぎゅうぎゅうに 見えない ように）
        float h = kind.height * 0.55f;
        quad.transform.localScale = new Vector3(h, h, 1f);
        quad.GetComponent<Renderer>().sharedMaterial = MatFor(kind);
        quad.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.AddComponent<Billboard>();
        go.AddComponent<CagedWiggle>();

        inside.Add(go.transform); kinds.Add(id);
    }

    Material MatFor(BugKind kind) {
        Material m;
        if (mats.TryGetValue(kind.id, out m) && m != null) return m;
        var sh = Shader.Find("Natsuyasumi/PixelSprite") ?? Shader.Find("Universal Render Pipeline/Lit");
        m = new Material(sh);
        m.SetFloat("_Cutoff", 0.5f);
        m.SetFloat("_BreatheAmp", 0f); m.SetFloat("_SwayAmp", 0f); m.SetFloat("_Wrap", 0.85f);
        int col = kind.Index % BugKind.Cols, row = kind.Index / BugKind.Cols;
        var s = new Vector2(1f / BugKind.Cols, 1f / BugKind.Rows);
        var o = new Vector2(col * s.x, (BugKind.Rows - 1 - row) * s.y);
        m.SetTexture("_BaseMap", atlas); m.SetTextureScale("_BaseMap", s); m.SetTextureOffset("_BaseMap", o);
        m.mainTexture = atlas; m.mainTextureScale = s; m.mainTextureOffset = o;
        mats[kind.id] = m;
        return m;
    }
}

// かごの 中で もぞもぞ 動く
public class CagedWiggle : MonoBehaviour {
    Vector3 home; float phase;
    void Start() { home = transform.localPosition; phase = Random.value * 10f; }
    void Update() {
        float t = Time.time * 0.9f + phase;
        transform.localPosition = home + new Vector3(
            Mathf.Sin(t * 1.7f) * 0.035f,
            Mathf.Abs(Mathf.Sin(t * 2.3f)) * 0.045f,
            Mathf.Cos(t * 1.3f) * 0.030f);
    }
}
