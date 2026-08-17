using UnityEngine;

// 虫 1ぴき。
// とまって いる／ただよって いる／逃げて いる、の 3つだけ。
// **見つけて 近づいて ふる**という 遊びが 成りたつ ように、
// 「近づくと 逃げる」が 種類ごとに ちがう ところを 手ざわりの 芯に する。
public class Bug : MonoBehaviour {
    public BugKind kind;
    public Vector3 home;             // もとの 居場所
    public float roam = 1.2f;        // どれくらい ふらつくか
    // この 1ぴきの 大きさ(mm)。**絵の 大きさにも 効かせる**ので、
    // 見て「あ、こいつ 大きい」と 分かる＝さがす 楽しみに なる
    [HideInInspector] public int sizeMm;
    [HideInInspector] public float sizeScale = 1f;

    [HideInInspector] public bool caught;
    // ★**同じ 8種で 長く 遊べる ように する。**（2026-08-17・遊ぶ 人の 指摘）
    //   「虫が 8種。10日ほどで 図鑑が 埋まり、そのあと 20日ぶん 集める ものが ありません」
    //   種類を ふやすには 新しい ドット絵が 要る＝素材の 課題。
    //   **大きさと 色は 既存の 絵で 作れる**ので、そこで 終盤の 山を つくる
    [HideInInspector] public bool nushi;   // ぬし（飛びぬけて 大きい 1ぴき）
    [HideInInspector] public bool kin;     // きんいろ（色ちがい）

    Transform sprite;
    Transform player;
    Vector3 vel;
    float phase, fleeLeft, restLeft;
    float sinkT = -1f;               // つかまえられた あとの 演出

    public void Init(BugKind k, Vector3 at, Transform spriteChild, int mm) {
        kind = k; home = at; sprite = spriteChild;
        sizeMm = mm;
        sizeScale = Mathf.Clamp(mm / (float)k.sizeMm, 0.7f, 1.45f);
        phase = Random.value * 10f;
        transform.position = at;
        restLeft = Random.Range(0.5f, 3f);
    }

    void Start() {
        var pm = FindFirstObjectByType<PlayerMove>();
        if (pm != null) player = pm.transform;
    }

    void Update() {
        if (sinkT >= 0f) { Caught(); return; }
        float t = Time.time + phase;

        // ★**近づいただけでは 逃げない。**
        //   そばを 走っただけで 飛んで いかれると、追いかける ことが できず
        //   ただ 意地わるに 感じる。むずかしさは 虫の 動きかたで 出す
        //  （とんぼは 速く ただよう、ばったは はねる、など）

        switch (kind.perch) {
            case BugPerch.Trunk:
                // みきに とまって ときどき ずれる。逃げると 木から はなれて 飛びさる
                if (fleeLeft > 0f) {
                    vel = Vector3.Lerp(vel, (transform.position - (player ? player.position : home)).normalized * 3.2f + Vector3.up * 1.4f, 6f * Time.deltaTime);
                } else {
                    var want = home + new Vector3(Mathf.Sin(t * 0.7f) * 0.06f, Mathf.Cos(t * 0.5f) * 0.10f, 0f);
                    vel = (want - transform.position) * 4f;
                }
                break;

            case BugPerch.Air:
                // 8の字ぎみに ただよう。逃げると まっすぐ 遠ざかる
                if (fleeLeft > 0f) {
                    var away = (transform.position - (player ? player.position : home)); away.y = 0f;
                    vel = Vector3.Lerp(vel, away.normalized * 4.2f + Vector3.up * 0.6f, 5f * Time.deltaTime);
                } else {
                    var want = home + new Vector3(Mathf.Sin(t * 0.6f) * roam,
                                                  Mathf.Sin(t * 1.3f) * 0.35f,
                                                  Mathf.Sin(t * 0.41f) * Mathf.Cos(t * 0.33f) * roam);
                    vel = Vector3.Lerp(vel, (want - transform.position) * 2.2f, 3f * Time.deltaTime);
                }
                break;

            case BugPerch.Grass:
                // 草に かくれて いて、たまに はねる
                restLeft -= Time.deltaTime;
                if (fleeLeft > 0f || restLeft <= 0f) {
                    if (vel.sqrMagnitude < 0.01f) {
                        var dir = Random.insideUnitCircle.normalized;
                        vel = new Vector3(dir.x, 2.6f, dir.y) * (fleeLeft > 0f ? 1.8f : 1.0f);
                    }
                    vel += Vector3.down * 9f * Time.deltaTime;
                    if (transform.position.y <= home.y && vel.y < 0f) {
                        vel = Vector3.zero;
                        restLeft = Random.Range(1.2f, 4f);
                        var p = transform.position; p.y = home.y; transform.position = p;
                        home = new Vector3(transform.position.x, home.y, transform.position.z);
                    }
                } else vel = Vector3.zero;
                break;

            case BugPerch.Bush:
                // ゆっくり ただよう。逃げない かわりに つかまえても すぐ また 湧く
                var w2 = home + new Vector3(Mathf.Sin(t * 0.35f) * roam,
                                            0.35f + Mathf.Sin(t * 0.8f) * 0.30f,
                                            Mathf.Cos(t * 0.29f) * roam);
                vel = Vector3.Lerp(vel, (w2 - transform.position) * 1.4f, 2f * Time.deltaTime);
                break;
        }

        transform.position += vel * Time.deltaTime;
        if (fleeLeft > 0f) {
            fleeLeft -= Time.deltaTime;
            // 逃げきったら 消える。BugSpawner が また 湧かせる
            if (fleeLeft <= 0f) Destroy(gameObject);
        }

        // 羽ばたきの ふるえ。とまって いる ときは 出さない
        if (sprite != null) {
            bool flying = kind.perch == BugPerch.Air || kind.perch == BugPerch.Bush || fleeLeft > 0f;
            float s = flying ? 1f + Mathf.Sin(t * 26f) * 0.10f : 1f;
            float h = kind.height * sizeScale;
            sprite.localScale = new Vector3(h * Mathf.Abs(s), h, 1f);
        }
    }

    // おどろかせる（あみを はずした とき）。しばらく 逃げて 消える
    public void Startle(float seconds) {
        if (caught) return;
        fleeLeft = Mathf.Max(fleeLeft, seconds);
    }

    // つかまえられた。上に 吸われて 消える
    public void Catch() {
        if (caught) return;
        caught = true; sinkT = 0f;
    }

    void Caught() {
        sinkT += Time.deltaTime;
        transform.position += Vector3.up * 1.6f * Time.deltaTime;
        if (sprite != null) {
            float k = Mathf.Clamp01(1f - sinkT / 0.45f) * kind.height * sizeScale;
            sprite.localScale = new Vector3(k, k, 1f);
        }
        if (sinkT > 0.45f) Destroy(gameObject);
    }
}
