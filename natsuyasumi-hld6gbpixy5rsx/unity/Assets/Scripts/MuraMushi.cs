using UnityEngine;

// 縦切り用の 虫（うごく あそびスポット）。とまり木の まわりを ふわふわ 飛ぶ。
// つかまえの 判定は MuraAsobi（同じ GameObject に つける）が そのまま つかえる。
public class MuraMushi : MonoBehaviour {
    public Vector3 anchor;           // うろつきの 中心
    public float haba = 4f;          // うろつきの 半径
    public float takasa = 1.0f;      // 飛ぶ 高さの まん中
    public float hayasa = 1.2f;

    Vector3 goal; float seed;

    void Start() { seed = (transform.position.x * 13.7f + transform.position.z * 7.1f) % 10f; PickGoal(); }

    void PickGoal() {
        var r = new Vector2(Mathf.PerlinNoise(seed, Time.time * 0.3f) - 0.5f,
                            Mathf.PerlinNoise(Time.time * 0.3f, seed) - 0.5f) * 2f * haba;
        goal = anchor + new Vector3(r.x, 0f, r.y);
    }

    void Update() {
        if ((transform.position - goal).sqrMagnitude < 0.3f) PickGoal();
        var want = goal; want.y = takasa + Mathf.Sin(Time.time * 3.1f + seed) * 0.25f;
        transform.position = Vector3.MoveTowards(transform.position, want, hayasa * Time.deltaTime);
    }
}
