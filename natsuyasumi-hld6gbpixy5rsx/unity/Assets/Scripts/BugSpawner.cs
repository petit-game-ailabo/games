using System.Collections.Generic;
using UnityEngine;

// 虫を 湧かせる。
//
// 考えかた：**その場に ふさわしい ところにしか 出さない。**
// セミは 木の みき、トンボは 野原の 上、バッタは 草の あいだ、ホタルは しげみの まわり。
// 湧く 場所は 場面から 自分で さがす（木や しげみの 位置を あらかじめ 手で 書かない）。
// 時間帯が 変わったら 顔ぶれも 変える＝あさに ホタルは いない。
public class BugSpawner : MonoBehaviour {
    public Texture2D atlas;             // bugs.png
    public int maxAlive = 14;
    public float interval = 0.9f;       // 湧かせを ためす 間かく(秒)

    TimeOfDay tod;
    Weather weather;
    readonly List<Vector3> trunks = new List<Vector3>();   // 木の みき（の 根もと）
    readonly List<Vector3> bushes = new List<Vector3>();   // しげみ
    readonly List<Bug> alive = new List<Bug>();
    float timer;
    Transform player;

    void Start() {
        tod = FindFirstObjectByType<TimeOfDay>();
        weather = FindFirstObjectByType<Weather>();
        var pm = FindFirstObjectByType<PlayerMove>();
        if (pm != null) player = pm.transform;
        if (atlas == null) atlas = Resources.Load<Texture2D>("bugs");
        ScanWorld();
        // はじめから 何びきか 居る ように しておく（湧くのを 待たされない）
        for (int i = 0; i < 8; i++) TrySpawn(true);
    }

    // 場面から 木と しげみを ひろう。名前で 見わける（BuildZashiki が つけた 名前）
    void ScanWorld() {
        foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None)) {
            var n = t.name;
            if (n.StartsWith("Ki") || n.StartsWith("Wood")) trunks.Add(t.position);
            else if (n.StartsWith("Shige") || n.StartsWith("Kusa")) bushes.Add(t.position);
        }
        Debug.Log("[BugSpawner] みき=" + trunks.Count + " しげみ=" + bushes.Count);
    }

    void Update() {
        alive.RemoveAll(b => b == null);
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = interval;
        TrySpawn(false);
    }

    int TodIndex() {
        if (tod == null) return 1;
        switch (tod.tod) {
            case TimeOfDay.Tod.Asa: return 0;
            case TimeOfDay.Tod.Hiru: return 1;
            case TimeOfDay.Tod.Yugata: return 2;
            default: return 3;
        }
    }

    void TrySpawn(bool near) {
        if (alive.Count >= maxAlive || atlas == null) return;
        // 雨の 日は 虫が 出ない。**降っている のに 飛んで いたら 嘘に なる**
        if (weather != null && (weather.mode == Weather.Mode.Ame || weather.mode == Weather.Mode.Yudachi)) return;

        int ti = TodIndex();
        int total = 0;
        foreach (var k in BugKind.All) if (k.tod[ti]) total += k.weight;
        if (total == 0) return;
        int roll = Random.Range(0, total);
        BugKind kind = null;
        foreach (var k in BugKind.All) {
            if (!k.tod[ti]) continue;
            roll -= k.weight;
            if (roll < 0) { kind = k; break; }
        }
        if (kind == null) return;

        Vector3 at;
        if (!PickSpot(kind, near, out at)) return;
        Make(kind, at);
    }

    bool PickSpot(BugKind kind, bool near, out Vector3 at) {
        at = Vector3.zero;
        Vector3 origin = player != null ? player.position : transform.position;
        // 近すぎると 湧くのが 見えてしまう。遠すぎると 出会えない
        float rMin = near ? 3f : 6f, rMax = near ? 9f : 15f;

        for (int tryI = 0; tryI < 12; tryI++) {
            switch (kind.perch) {
                case BugPerch.Trunk: {
                    if (trunks.Count == 0) return false;
                    var p = trunks[Random.Range(0, trunks.Count)];
                    // みきの 高さ。木の 絵は 4.5m コマなので、みきは 下の ほう
                    at = p + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0.7f, 1.9f), 0.30f);
                    break;
                }
                case BugPerch.Bush: {
                    if (bushes.Count == 0) return false;
                    var p = bushes[Random.Range(0, bushes.Count)];
                    at = p + new Vector3(Random.Range(-0.9f, 0.9f), Random.Range(0.3f, 0.9f), Random.Range(-0.9f, 0.9f));
                    break;
                }
                case BugPerch.Grass: {
                    var c = Random.insideUnitCircle.normalized * Random.Range(rMin, rMax);
                    at = new Vector3(origin.x + c.x, 0f, origin.z + c.y);
                    if (InsideHouse(at)) continue;      // 畳の 上に ばったは いない
                    at.y = GroundYNear(at) + 0.12f;
                    break;
                }
                default: {   // Air
                    var c = Random.insideUnitCircle.normalized * Random.Range(rMin, rMax);
                    at = new Vector3(origin.x + c.x, 0f, origin.z + c.y);
                    if (InsideHouse(at)) continue;      // 部屋の 中を とんでいると 妙
                    at.y = GroundYNear(at) + Random.Range(0.8f, 1.8f);
                    break;
                }
            }
            float d = Vector3.Distance(at, origin);
            if (d >= rMin && d <= rMax + 4f) return true;
        }
        return false;
    }

    [Header("家の 中には 出さない（縁がわ ふくむ）")]
    public Vector3 houseCenter = new Vector3(0f, 0f, 0.45f);
    public Vector2 houseHalf = new Vector2(4.2f, 3.7f);

    bool InsideHouse(Vector3 p) {
        return Mathf.Abs(p.x - houseCenter.x) < houseHalf.x
            && Mathf.Abs(p.z - houseCenter.z) < houseHalf.y;
    }

    // その場の 地めんの 高さ。家の 床の 上か、野原か
    static float GroundYNear(Vector3 p) {
        RaycastHit hit;
        if (Physics.Raycast(p + Vector3.up * 12f, Vector3.down, out hit, 30f)) return hit.point.y;
        return -0.52f;
    }

    void Make(BugKind kind, Vector3 at) {
        var go = new GameObject("Bug_" + kind.id);
        go.transform.position = at;

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Sprite";
        quad.transform.SetParent(go.transform, false);
        Destroy(quad.GetComponent<Collider>());
        int mm = BugBook.RollSize(kind);
        float h0 = kind.height * Mathf.Clamp(mm / (float)kind.sizeMm, 0.7f, 1.45f);
        quad.transform.localScale = new Vector3(h0, h0, 1f);
        quad.GetComponent<Renderer>().sharedMaterial = BugMaterial(kind);
        quad.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        go.AddComponent<Billboard>();
        var bug = go.AddComponent<Bug>();
        bug.Init(kind, at, quad.transform, mm);

        // ホタルは 自分で 光る。夜の 目じるしに なる
        if (kind.glows) {
            var lg = new GameObject("Glow");
            lg.transform.SetParent(go.transform, false);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(0.85f, 1f, 0.45f);
            l.range = 2.4f; l.intensity = 1.6f; l.shadows = LightShadows.None;
            lg.AddComponent<Flicker>();
        }
        alive.Add(bug);
    }

    // 種類ごとに 1つだけ 作って 使いまわす
    readonly Dictionary<BugId, Material> mats = new Dictionary<BugId, Material>();

    Material BugMaterial(BugKind kind) {
        Material m;
        if (mats.TryGetValue(kind.id, out m) && m != null) return m;
        var sh = Shader.Find("Natsuyasumi/PixelSprite") ?? Shader.Find("Universal Render Pipeline/Lit");
        m = new Material(sh);
        m.SetFloat("_Cutoff", 0.5f);
        m.SetFloat("_BreatheAmp", 0f); m.SetFloat("_SwayAmp", 0f);
        m.SetFloat("_Wrap", 0.8f);           // 小さいので 影で つぶれない ように 明るめ
        int col = kind.Index % BugKind.Cols, row = kind.Index / BugKind.Cols;
        var s = new Vector2(1f / BugKind.Cols, 1f / BugKind.Rows);
        var o = new Vector2(col * s.x, (BugKind.Rows - 1 - row) * s.y);
        m.SetTexture("_BaseMap", atlas);
        m.SetTextureScale("_BaseMap", s);
        m.SetTextureOffset("_BaseMap", o);
        m.mainTexture = atlas; m.mainTextureScale = s; m.mainTextureOffset = o;
        mats[kind.id] = m;
        return m;
    }

    public int AliveCount { get { return alive.Count; } }
}

// ホタルの 明滅
public class Flicker : MonoBehaviour {
    Light l; float phase;
    void Start() { l = GetComponent<Light>(); phase = Random.value * 10f; }
    void Update() {
        if (l == null) return;
        float t = Time.time * 1.6f + phase;
        // ふっと 点いて ゆっくり 消える
        float k = Mathf.Pow(Mathf.Clamp01(Mathf.Sin(t) * 0.5f + 0.5f), 3f);
        l.intensity = 0.25f + k * 2.2f;
    }
}
