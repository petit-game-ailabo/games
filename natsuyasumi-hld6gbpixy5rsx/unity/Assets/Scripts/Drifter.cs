using UnityEngine;

// 水に のせた ものを、そのまま 流して おく ための もの。
//
// ささぶねは **手を はなした あとも 流れつづける**のが 良い ところ。
// 人が 見て いようが いまいが 下って いって、やがて 見えなく なる。
// だから 遊びの コルーチンでは なく、舟 じしんに もたせる。
public class Drifter : MonoBehaviour {
    public Vector3 dir = Vector3.forward;
    public float speed = 0.95f;
    public float life = 14f;
    public float wobble = 0.10f;

    float t;
    Vector3 side;
    float baseX = 0.34f, baseY = 0.34f;

    void Awake() { baseX = transform.localScale.x; baseY = transform.localScale.y; }

    void Start() {
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward;
        dir.Normalize();
        side = Vector3.Cross(Vector3.up, dir);
        t = Random.value * 6f;
    }

    void Update() {
        t += Time.deltaTime;
        life -= Time.deltaTime;
        // 流れは 一定では ない。すこし 速くなったり ゆれたり する
        float v = speed * (1f + Mathf.Sin(t * 1.3f) * 0.18f);
        transform.position += (dir * v + side * Mathf.Sin(t * 2.1f) * wobble) * Time.deltaTime;

        // 終わりぎわは 小さく なって 消える＝川下に 遠ざかった ように 見える
        if (life < 1.2f) {
            float s = Mathf.Max(0.001f, Mathf.Clamp01(life / 1.2f));
            transform.localScale = new Vector3(baseX * s, baseY * s, 1f);
        }
        if (life <= 0f) Destroy(gameObject);
    }
}
