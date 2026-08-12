using UnityEngine;

// あるく。やじるし／WASD。カメラの 向きを 基準に うごく。
// ドット絵は 板なので、進む むきで 左右を 反転させる（絵は 1まいで すむ）
[RequireComponent(typeof(CharacterController))]
public class PlayerMove : MonoBehaviour {
    [Header("うごき")]
    public float speed = 2.6f;          // m/秒。子どもの 歩きぐらい
    public float runSpeed = 4.4f;
    public float gravity = 18f;

    [Header("見た目")]
    public Transform sprite;            // 板（子）
    public float bobHeight = 0.055f;    // あるくと すこし はずむ
    public float bobSpeed = 9.5f;

    CharacterController cc;
    Renderer spriteRen;
    MaterialPropertyBlock mpb;
    float bob, vy;
    Vector3 startPos;
    int face = 1;                       // 1=右 -1=左
    Vector2 baseScale, baseOffset;

    void Awake() {
        cc = GetComponent<CharacterController>();
        startPos = transform.position;
        if (sprite == null && transform.childCount > 0) sprite = transform.GetChild(0);
        if (sprite != null) spriteRen = sprite.GetComponent<Renderer>();
        if (spriteRen != null) {
            var m = spriteRen.sharedMaterial;
            baseScale  = m.GetTextureScale("_BaseMap");
            baseOffset = m.GetTextureOffset("_BaseMap");
            mpb = new MaterialPropertyBlock();
        }
    }

    [Header("たしかめ用（自動で あるかせる）")]
    public bool useAutoInput;
    public Vector2 autoInput;

    void Update() {
        var cam = Camera.main;
        float h = Input.GetAxisRaw("Horizontal"), v = Input.GetAxisRaw("Vertical");
        if (useAutoInput) { h = autoInput.x; v = autoInput.y; }
        Vector3 wish = Vector3.zero;
        if (cam != null) {
            var f = cam.transform.forward; f.y = 0f; f.Normalize();
            var r = cam.transform.right;   r.y = 0f; r.Normalize();
            wish = f * v + r * h;
        } else wish = new Vector3(h, 0f, v);
        if (wish.sqrMagnitude > 1f) wish.Normalize();

        bool moving = wish.sqrMagnitude > 0.001f;
        float sp = Input.GetKey(KeyCode.LeftShift) ? runSpeed : speed;

        // 万一 落ちたら もどす（穴が あっても 詰まない ための 保険）
        if (transform.position.y < -4f) {
            cc.enabled = false;
            transform.position = startPos;
            cc.enabled = true; vy = 0f;
        }

        // 地めんに つける
        if (cc.isGrounded && vy < 0f) vy = -2f;
        vy -= gravity * Time.deltaTime;

        cc.Move((wish * sp + Vector3.up * vy) * Time.deltaTime);

        // むき（画面の 左右で 決める。奥/手前だけの ときは 変えない）
        if (cam != null && moving) {
            float screenX = Vector3.Dot(wish, cam.transform.right);
            if (Mathf.Abs(screenX) > 0.15f) face = screenX > 0f ? 1 : -1;
        }

        // あるく はずみ
        bob = moving ? bob + Time.deltaTime * bobSpeed : 0f;
        if (sprite != null) {
            var p = sprite.localPosition;
            p.y = sprite.localScale.y * 0.5f + (moving ? Mathf.Abs(Mathf.Sin(bob)) * bobHeight : 0f);
            sprite.localPosition = p;
        }

        // 左右の 反転は UVで やる（板を 裏返すと 描かれなく なるので）
        if (spriteRen != null) {
            spriteRen.GetPropertyBlock(mpb);
            var s = baseScale; var o = baseOffset;
            if (face < 0) { s.x = -baseScale.x; o.x = baseOffset.x + baseScale.x; }
            mpb.SetVector("_BaseMap_ST", new Vector4(s.x, s.y, o.x, o.y));
            spriteRen.SetPropertyBlock(mpb);
        }
    }
}
