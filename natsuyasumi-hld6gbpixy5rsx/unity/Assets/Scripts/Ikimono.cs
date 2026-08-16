// 屋敷を うろつく 生きもの（にわとり・犬）。（2026-08-17）
//
// ★遊ぶ 人からの 言：「動いて いる ものが 一匹も いないのが 致命的。
//   前庭に むしろは 干して あるのに、それを 干した 人が いない」
//
// ★むずかしい ことは しない。**きめた 四角の 中を ゆっくり うろつく**だけ。
//   ときどき 立ちどまって 地めんを つつく。それだけで 屋敷が 生きて 見える。
//   近づくと すこし よける（つかまえられると 思わせない）。
using UnityEngine;

public class Ikimono : MonoBehaviour {

    public Vector3 home;          // うろつく まん中
    public float roam = 5f;       // うろつく はば
    public float speed = 0.55f;
    public Transform player;
    [Tooltip("これより 近づかれたら よける")]
    public float shy = 1.8f;
    [Tooltip("夜は ねる（動かない）")]
    public TimeOfDay tod;

    Vector3 want;
    float waitLeft;

    void Start() { want = Pick(); }

    Vector3 Pick() {
        var c = Random.insideUnitCircle * roam;
        var p = home + new Vector3(c.x, 0f, c.y);
        p.y = GroundY(p);
        return p;
    }

    // ★TerrainGen は **エディタ用の 道具**なので 走って いる ゲームからは 見えない。
    //   下へ レイを 打って 地めんを さがす（層2＝見えない かべ・屋根は 拾わない）
    static float GroundY(Vector3 p) {
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(p.x, p.y + 40f, p.z), Vector3.down, out hit, 120f,
                            ~(1 << 2), QueryTriggerInteraction.Ignore)) return hit.point.y;
        return p.y;
    }

    void Update() {
        // 夜は 動かない（小屋に 入って いる ことに する）
        if (tod != null && (tod.hour >= 19f || tod.hour < 5f)) return;

        // 人が 近づいたら 反対がわへ 逃げる
        if (player != null) {
            var d = transform.position - player.position; d.y = 0f;
            if (d.sqrMagnitude < shy * shy) {
                want = home + d.normalized * roam * 0.8f;
                want.y = GroundY(want);
                waitLeft = 0f;
            }
        }

        if (waitLeft > 0f) { waitLeft -= Time.deltaTime; return; }

        var to = want - transform.position; to.y = 0f;
        if (to.sqrMagnitude < 0.09f) {
            // ついた。すこし 地めんを つついて から つぎへ
            waitLeft = Random.Range(0.8f, 2.6f);
            want = Pick();
            return;
        }
        var step = to.normalized * speed * Time.deltaTime;
        var np = transform.position + step;
        np.y = GroundY(np);
        transform.position = np;
    }
}
