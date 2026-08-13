using System.Collections.Generic;
using UnityEngine;

// 虫あみを ふって つかまえる。
//
// 手ざわりの ねらい：**「ふる」だけの 単純な 操作に、間あいと 運を のせる。**
//  - 近づく → 用心ぶかい 虫は 逃げようとする（Bug が やる）
//  - ふる → あみの 先の まるい 範囲に 入って いれば 見こみ判定
//  - はずすと 虫は 逃げる＝連打では 取れない
public class BugCatcher : MonoBehaviour {
    [Header("あみ")]
    public float reach = 1.30f;      // 手の さきから どこまで とどくか
    public float radius = 0.75f;     // あみの 口の 大きさ（よこ）
    // **たては ゆるく とる。** あみは 振り上げも 振り下ろしも できるので、
    // よこと 同じ 球で 判定すると、木の みきの セミ(地めんから 1.5m)に まったく 届かない。
    // 実際 これで 12回 ふって 0匹に なった
    public float reachUp = 1.35f;    // 頭より 上へ どこまで
    public float reachDown = 0.75f;  // 足もとへ どこまで
    public float swingTime = 0.42f;  // ふり切るまで
    public float cool = 0.30f;       // ふった あとの すき

    [Header("あみの 絵")]
    public Transform net;            // 見た目（無ければ 作る）

    BugBook book;
    float swing = -1f, coolLeft;
    bool resolved;
    Vector3 netHome;

    void Start() {
        book = FindFirstObjectByType<BugBook>();
        if (book == null) book = gameObject.AddComponent<BugBook>();
        if (net == null) net = MakeNet();
        netHome = net.localPosition;
    }

    void Update() {
        if (coolLeft > 0f) coolLeft -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0)) TrySwing();

        if (swing >= 0f) {
            swing += Time.deltaTime;
            float k = swing / swingTime;
            Pose(k);
            // **ふり切る すこし 手前で 判定する。** 当たり判定が 絵より 遅れると
            // 「当たったのに 取れない」と 感じる
            if (!resolved && k >= 0.45f) { resolved = true; Resolve(); }
            if (k >= 1f) { swing = -1f; coolLeft = cool; Pose(-1f); }
        }
    }

    /// <summary>あみを ふる。ふれた なら true（たしかめの 自動運転からも 呼ぶ）</summary>
    public bool TrySwing() {
        if (swing >= 0f || coolLeft > 0f) return false;
        swing = 0f; resolved = false;
        return true;
    }

    /// <summary>いちばん 近い 虫（たしかめ用）</summary>
    public Bug Nearest() {
        Bug best = null; float bd = float.MaxValue;
        foreach (var b in FindObjectsByType<Bug>(FindObjectsSortMode.None)) {
            if (b.caught) continue;
            float d = Vector3.Distance(b.transform.position, transform.position);
            if (d < bd) { bd = d; best = b; }
        }
        return best;
    }

    // あみの ふり。0→1 で 上から 前へ 払う
    void Pose(float k) {
        if (net == null) return;
        if (k < 0f) { net.localPosition = netHome; net.localRotation = Quaternion.identity; net.gameObject.SetActive(false); return; }
        net.gameObject.SetActive(true);
        float a = Mathf.SmoothStep(-70f, 60f, Mathf.Clamp01(k));
        net.localRotation = Quaternion.Euler(a, 0f, 0f);
        net.localPosition = netHome + new Vector3(0f, 0.10f * Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI), 0f);
    }

    void Resolve() {
        var cam = Camera.main;
        // 前＝カメラの 向き（歩きも カメラ基準なので そろえる）
        Vector3 fwd = cam != null ? cam.transform.forward : transform.forward;
        fwd.y = 0f; fwd.Normalize();
        Vector3 at = transform.position + Vector3.up * 0.85f + fwd * reach;

        Bug best = null; float bestD = float.MaxValue;
        foreach (var b in FindObjectsByType<Bug>(FindObjectsSortMode.None)) {
            if (b.caught) continue;
            var d = b.transform.position - at;
            float horiz = new Vector2(d.x, d.z).magnitude;
            if (horiz >= radius) continue;
            if (d.y > reachUp || d.y < -reachDown) continue;
            if (horiz < bestD) { best = b; bestD = horiz; }
        }
        if (best == null) return;

        // まん中で とらえるほど 取りやすい
        float aim = 1f - Mathf.Clamp01(bestD / radius) * 0.45f;
        if (Random.value < best.kind.catchRate * aim) {
            book.Add(best.kind.id);
            Pop(best.transform.position, true);
            best.Catch();
        } else {
            // はずした。虫は おどろいて 逃げる＝連打では 取れない
            Pop(best.transform.position, false);
            best.Startle(1.4f);
        }
    }

    // 取れた／逃げられた を つぶで 見せる
    void Pop(Vector3 at, bool ok) {
        var go = new GameObject("Pop");
        go.transform.position = at;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.55f; main.startSpeed = 2.2f; main.startSize = 0.10f;
        main.maxParticles = 24; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 1.2f;
        main.startColor = ok ? new Color(1f, 0.95f, 0.55f, 1f) : new Color(0.85f, 0.85f, 0.85f, 0.8f);
        var em = ps.emission; em.enabled = true; em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(ok ? 16 : 7)) });
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.12f;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")
                                  ?? Shader.Find("Sprites/Default"));
        r.material.SetFloat("_Surface", 1);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Destroy(go, 1.2f);
    }

    // 見た目の あみ。柄と 口わを 細い 箱で 組む（絵を 用意しなくても 成り立つ ように）
    Transform MakeNet() {
        var root = new GameObject("Net").transform;
        root.SetParent(transform, false);
        root.localPosition = new Vector3(0.16f, 0.72f, 0f);

        var woodCol = new Color(0.67f, 0.48f, 0.20f);
        var meshCol = new Color(0.92f, 0.94f, 0.88f);

        var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handle.name = "Handle"; handle.transform.SetParent(root, false);
        handle.transform.localPosition = new Vector3(0f, 0f, 0.34f);
        handle.transform.localScale = new Vector3(0.045f, 0.045f, 0.68f);
        Destroy(handle.GetComponent<Collider>());
        Tint(handle, woodCol);

        // 口わ＝4本の 細い 箱で 四角く
        for (int i = 0; i < 4; i++) {
            var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = "Rim" + i; seg.transform.SetParent(root, false);
            bool horiz = i < 2;
            float s = i % 2 == 0 ? 1f : -1f;
            seg.transform.localPosition = new Vector3(horiz ? 0f : s * 0.26f, horiz ? s * 0.26f : 0f, 0.74f);
            seg.transform.localScale = horiz ? new Vector3(0.55f, 0.04f, 0.04f) : new Vector3(0.04f, 0.55f, 0.04f);
            Destroy(seg.GetComponent<Collider>());
            Tint(seg, woodCol);
        }
        // あみの ふくろ
        var bag = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bag.name = "Bag"; bag.transform.SetParent(root, false);
        bag.transform.localPosition = new Vector3(0f, 0f, 0.94f);
        bag.transform.localScale = new Vector3(0.42f, 0.42f, 0.34f);
        Destroy(bag.GetComponent<Collider>());
        Tint(bag, meshCol);

        root.gameObject.SetActive(false);
        return root;
    }

    static void Tint(GameObject go, Color c) {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", 0.05f);
        go.GetComponent<Renderer>().sharedMaterial = m;
        go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }
}
