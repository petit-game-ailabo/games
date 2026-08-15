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

    /// <summary>true の あいだは 入力を うけつけない（あみを ふって いる あいだ など）</summary>
    public bool locked;

    CharacterController cc;
    Renderer spriteRen;
    MaterialPropertyBlock mpb;
    float bob, vy;
    float breatheAmp;                   // 立ち止まって いる ときの 息づかいの 大きさ
    float pixel = 0.02f;                // 絵の 1ドットぶんの 高さ(m)
    Vector3 startPos;
    int face = 1;                       // 1=右 -1=左
    /// <summary>いま 向いている 左右（1=右 -1=左）。あみを ふる 向きに 使う</summary>
    // **絵の 向きと あみの 向きは 必ず そろえる。**
    // ばらばらだと「右を 向いて いるのに 左へ 振る」に なる
    public int Face { get { return chars != null ? (chars.FacingRight ? 1 : -1) : face; } }

    // **奥/手前を 向いて いる ときは あみを たてに ふる。**
    // よこ振りでは 空の 高い ところを とぶ 虫に 当たらない
    bool depthFacing;
    /// <summary>奥か 手前を 向いて いる（＝あみは たて振り）</summary>
    public bool DepthFacing { get { return chars != null ? chars.FacingDepth : depthFacing; } }
    Vector2 baseScale, baseOffset;

    // ★2026-08-15：**8方向の 絵が 来たので UVの 裏がえしは やめた。**
    //   1枚を 左右 反転して 使って いた ころは 右向きと 左向きしか 無かった。
    //   いまは 向きごとに 別の 絵が あるので、CharSprite に まかせる
    CharSprite chars;

    // 入力を 読みかえる 向き（押しっぱなしの あいだ 固定する）
    float basisYaw;
    bool basisReady;

    void Awake() {
        cc = GetComponent<CharacterController>();
        startPos = transform.position;
        if (sprite == null && transform.childCount > 0) sprite = transform.GetChild(0);
        if (sprite != null) spriteRen = sprite.GetComponent<Renderer>();
        chars = GetComponent<CharSprite>();
        if (spriteRen != null) {
            var m = spriteRen.sharedMaterial;
            baseScale  = m.GetTextureScale("_BaseMap");
            baseOffset = m.GetTextureOffset("_BaseMap");
            breatheAmp = m.HasProperty("_BreatheAmp") ? m.GetFloat("_BreatheAmp") : 0f;
            mpb = new MaterialPropertyBlock();
        }
        // 絵は たて 64ドットで 背たけぶん。1ドット＝この 高さ
        if (sprite != null) pixel = Mathf.Max(0.004f, sprite.localScale.y / 64f);
    }

    [Header("たしかめ用（自動で あるかせる）")]
    public bool useAutoInput;
    public Vector2 autoInput;
    [Tooltip("自動運転で 走らせる（Shift を おした ことに する）")]
    public bool autoRun;

    void Update() {
        var cam = Camera.main;
        float h = 0f, v = 0f;
        // **あみを ふって いる あいだは 動かない。**
        // ふりながら 右を 向いたり 左を 向いたり できると、振りの 向きが 途中で
        // 入れかわって 何を して いるか 分からなく なる
        if (!locked) { h = Input.GetAxisRaw("Horizontal"); v = Input.GetAxisRaw("Vertical"); }
        if (useAutoInput) { h = autoInput.x; v = autoInput.y; }
        // ★2026-08-15：**入力を 読みかえる 向きは、キーを 押して いる あいだ 変えない。**
        //   カメラが 回ると 同じ キーが ちがう 方角に なり、道の とちゅうで
        //   進む むきが 折れて 操作しづらかった（本人の 指摘）。
        //   押しっぱなしの あいだは 前の 向きの まま＝**そのまま 進める**。
        //   手を はなした ときに いまの カメラに そろえる
        bool anyInput = Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f;
        if (cam != null && (!basisReady || !anyInput)) {
            basisYaw = cam.transform.eulerAngles.y;
            basisReady = true;
        }
        Vector3 wish;
        if (basisReady) {
            var rot = Quaternion.Euler(0f, basisYaw, 0f);
            wish = (rot * Vector3.forward) * v + (rot * Vector3.right) * h;
        } else wish = new Vector3(h, 0f, v);
        if (wish.sqrMagnitude > 1f) wish.Normalize();

        bool moving = wish.sqrMagnitude > 0.001f;
        float sp = (Input.GetKey(KeyCode.LeftShift) || autoRun) ? runSpeed : speed;

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

        // むき。画面の よこ成分と おく成分を くらべて、どちらを 向いて いるか 決める
        if (cam != null && moving) {
            float screenX = Vector3.Dot(wish, cam.transform.right);
            var camF = cam.transform.forward; camF.y = 0f; camF.Normalize();
            float screenZ = Vector3.Dot(wish, camF);
            depthFacing = Mathf.Abs(screenZ) > Mathf.Abs(screenX);
            if (Mathf.Abs(screenX) > 0.15f) face = screenX > 0f ? 1 : -1;
        }

        // あるく はずみ。
        // ★**ドット の きざみに そろえる。** なめらかに 上下させると、点で 拡大して
        //   いる 絵の 行が すべって がくがく 見える。1ドット単位で 跳ねさせると 落ちつく
        bob = moving ? bob + Time.deltaTime * bobSpeed : 0f;
        if (sprite != null) {
            float raw = moving ? Mathf.Abs(Mathf.Sin(bob)) * bobHeight : 0f;
            var p = sprite.localPosition;
            p.y = sprite.localScale.y * 0.5f + Mathf.Round(raw / pixel) * pixel;
            sprite.localPosition = p;
        }

        // 向きと あしどりは CharSprite が 絵を えらぶ。
        // **8方向の 絵が あるので 左右の 反転は もう 要らない**
        if (chars != null) chars.Drive(wish, sp, locked);

        if (spriteRen != null) {
            spriteRen.GetPropertyBlock(mpb);
            if (chars == null) {
                // 8方向の 絵が 無い 相手（1枚絵の キャラ）は これまでどおり UVで 裏がえす
                var s = baseScale; var o = baseOffset;
                if (face < 0) { s.x = -baseScale.x; o.x = baseOffset.x + baseScale.x; }
                mpb.SetVector("_BaseMap_ST", new Vector4(s.x, s.y, o.x, o.y));
            }
            // **歩いて いる あいだは 息づかいを 止める。**
            // 歩きの はずみと 息の のびちぢみが 重なって、絵が がくがくして いた。
            // そもそも 走りながら 肩で 息を する 絵は 要らない
            mpb.SetFloat("_BreatheAmp", moving ? 0f : breatheAmp);
            spriteRen.SetPropertyBlock(mpb);
        }
    }
}
