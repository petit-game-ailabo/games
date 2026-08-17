using System.Collections.Generic;
using UnityEngine;

// 虫あみを ふって つかまえる。
//
// 手ざわりの ねらい：**「ふる」だけの 単純な 操作に、間あいと 運を のせる。**
//  - 近づく → 用心ぶかい 虫は 逃げようとする（Bug が やる）
//  - ふる → あみの 先の まるい 範囲に 入って いれば 見こみ判定
//  - はずすと 虫は 逃げる＝連打では 取れない
public class BugCatcher : MonoBehaviour {
    // ★人と 話す・ねる ときは あみを ふらない（どちらも スペース）
    [HideInInspector] public DayHost dayHost;
    // ★とった ことを 日記に ためる
    [HideInInspector] public Nikki nikki;
    [HideInInspector] public MarisaVoice voice;
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
    BugSumo sumo;
    BugHud hud;
    PlayerMove move;
    PlayHost play;
    float swing = -1f, coolLeft;
    bool resolved;

    void Start() {
        book = FindFirstObjectByType<BugBook>();
        if (book == null) book = gameObject.AddComponent<BugBook>();
        sumo = FindFirstObjectByType<BugSumo>();
        hud = FindFirstObjectByType<BugHud>();
        move = GetComponent<PlayerMove>();
        play = GetComponent<PlayHost>();
        if (net == null) net = MakeNet();
    }

    void Update() {
        if (coolLeft > 0f) coolLeft -= Time.deltaTime;

        // ずもうの さいちゅうは あみを ふらない（同じ キーを 使うので）
        if (sumo != null && sumo.Busy) {
            // **やじるしは ずもうの 技に つかう。** 同時に 歩けると 落ちついて 見られない
            if (move != null) move.locked = true;
            Pose(-1f);
            return;
        }

        // 遊びの さいちゅうも あみは ふらない（同じ ボタンを つかう ので）
        if (play != null && play.Busy) { Pose(-1f); return; }

        // ★**足もとの ひとことは ここで ひとつに まとめる。**
        //   前は それぞれが 好きに 書きこんで いて、あとから 書いた 空っぽが
        //   先の ひとことを 消して いた。近い ものから 順に えらぶ
        if (hud != null) {
            string p = null;
            if (play != null && play.NearSpot != null) p = play.NearSpot.Prompt;
            if (p == null && sumo != null) p = sumo.PromptFor(transform);
            hud.Offer(p, 20);
        }

        // ★人と 話す・ねる ほうが ゆうせん。**話しかけた とたん あみを ふる**のを 止める
        if (dayHost != null && dayHost.BlockPlay) { if (move != null) move.locked = swing >= 0f; return; }
        if (dayHost != null && dayHost.Busy) { if (move != null) move.locked = false; return; }

        bool pressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)
                    || Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0);
        if (pressed) {
            // **そばに 遊び場が あれば そちらが 勝つ。**川べりで あみを ふっても 何も 起きない
            if (play != null && play.TryBegin()) return;
            if (sumo != null && sumo.PlayerNear(transform)) { sumo.Begin(); return; }
            TrySwing();
        }

        if (move != null) move.locked = swing >= 0f;

        if (swing >= 0f) {
            swing += Time.deltaTime;
            float k = swing / swingTime;
            Pose(k);
            // **ふり切る すこし 手前で 判定する。** 当たり判定が 絵より 遅れると
            // 「当たったのに 取れない」と 感じる
            // **判定は あみが 正面を 通る ころ。** 早すぎても 遅すぎても
            // 「当たったのに 取れない」に なる（ふり幅 -80〜+55度の まん中あたり）
            if (!resolved && k >= 0.58f) { resolved = true; Resolve(); }
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

    // あみの ふり。**向いている ほうへ 払う。**
    // →を おした あとは 右を 向いて いるので、あみも 右へ 振りおろす。
    // 板の キャラは いつも カメラを 向くので、「前」も「右」も カメラ基準で とる
    void Pose(float k) {
        if (net == null) return;
        if (k < 0f) { net.gameObject.SetActive(false); return; }
        net.gameObject.SetActive(true);

        var cam = Camera.main;
        Vector3 fwd = cam != null ? cam.transform.forward : Vector3.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
        fwd.Normalize();
        Vector3 right = new Vector3(fwd.z, 0f, -fwd.x);   // 画面の 右
        int face = move != null ? move.Face : 1;

        float t = Mathf.Clamp01(k);
        bool up = move != null && move.DepthFacing;
        if (up) {
            // **たて振り。奥/手前の どちらを 向いて いるかで 振る むきを 変える。**
            //   おく向き　… 手前から 奥へ（自分の 前へ 払いだす）
            //   手前向き … 奥から 手前へ（自分の ほうへ 引きよせる）
            // 向きに かかわらず 同じ 振りかたを して いた ころは、
            // 手前を 向いて いるのに 背中がわへ 振って いた
            bool away = move != null && move.FacingAway;
            float s = away ? 1f : -1f;
            // 奥ゆき方向に 手前(-0.35)から 奥(+0.95)へ 通す。手前向きなら 逆に たどる
            float depth = Mathf.SmoothStep(-0.35f, 0.95f, t) * s;
            float pitch = Mathf.SmoothStep(away ? 40f : -70f, away ? -70f : 40f, t);
            net.rotation = Quaternion.LookRotation(fwd * s, Vector3.up) * Quaternion.Euler(pitch, 0f, 0f);
            net.position = transform.position
                         + Vector3.up * (0.72f + 0.30f * Mathf.Sin(t * Mathf.PI))
                         + fwd * depth
                         + right * (face * 0.08f);
        } else {
            // うしろ(-80度)から 前(+55度)へ、向いている ほうを 通って 払う
            float yaw = Mathf.SmoothStep(-80f, 55f, t) * face;
            float pitch = Mathf.Lerp(-32f, 26f, t);
            net.rotation = Quaternion.LookRotation(fwd, Vector3.up) * Quaternion.Euler(pitch, yaw, 0f);
            net.position = transform.position
                         + Vector3.up * (0.74f + 0.10f * Mathf.Sin(t * Mathf.PI))
                         + right * (face * 0.16f);
        }
    }

    void Resolve() {
        var cam = Camera.main;
        // 前＝カメラの 向き（歩きも カメラ基準なので そろえる）
        Vector3 fwd = cam != null ? cam.transform.forward : transform.forward;
        fwd.y = 0f; fwd.Normalize();
        // あみが 通る ところ＝前＋向いている がわ。絵と 判定が ずれると
        // 「当たったのに 取れない」と 感じる
        Vector3 right = new Vector3(fwd.z, 0f, -fwd.x);
        int face = move != null ? move.Face : 1;
        Vector3 at;
        if (move != null && move.DepthFacing) {
            // たて振り。**頭の 上を さらう**ので、判定も 高く とる。
            // 振る むき（奥/手前）に あわせて 判定も 前後へ ずらす＝絵と 判定を そろえる
            float s = move.FacingAway ? 1f : -1f;
            at = transform.position + Vector3.up * 1.65f + fwd * (reach * 0.55f * s);
        } else {
            at = transform.position + Vector3.up * 0.85f
               + fwd * (reach * 0.88f) + right * (face * reach * 0.30f);
        }

        Bug best = null; float bestD = float.MaxValue;
        foreach (var b in FindObjectsByType<Bug>(FindObjectsSortMode.None)) {
            if (b.caught) continue;
            var d = b.transform.position - at;
            float horiz = new Vector2(d.x, d.z).magnitude;
            if (horiz >= radius) continue;
            float up1 = (move != null && move.DepthFacing) ? reachUp * 1.5f : reachUp;
            // ★**たて振りは 頭の 上から 足もとまで さらう。**（2026-08-17）
            //   判定の まん中を 頭の 高さ(1.65m)に 置いた ままで 下だけ 1.2m しか
            //   見て いなかった ので、**草の あいだの ばったに 一生 とどかなかった**
            //  （たしかめ：野はらで 6回 ふって しょうりょうばったは 0回）。
            //   ふる 絵は 上から 下への 弧なので、地めんまで 届くのが 正しい
            float dn1 = (move != null && move.DepthFacing) ? reachDown * 2.4f : reachDown;
            if (d.y > up1 || d.y < -dn1) continue;
            if (horiz < bestD) { best = b; bestD = horiz; }
        }
        if (best == null) {
            // ★**おしかった ときだけ 一言。**ただの 空ぶりでは しゃべらない
            //（何も いない ところで ふるたびに 悔しがると うるさい）
            if (voice != null) {
                foreach (var b in FindObjectsByType<Bug>(FindObjectsSortMode.None)) {
                    if (b == null || b.caught) continue;
                    var dd = b.transform.position - at;
                    if (new Vector2(dd.x, dd.z).sqrMagnitude < (radius + 1.1f) * (radius + 1.1f)) {
                        voice.Missed_(); break;
                    }
                }
            }
            return;
        }

        // **あみが 当たったら 取れる。さいころは ふらない。**
        // 見た目では 当たって いるのに 取れない のは、遊ぶ 側から すると ただの 故障に 見える。
        // むずかしさは 「近づけるか・追いつけるか」＝場所の 話で 出す
        // ★**かごが いっぱいなら 入らない。**（2026-08-17）
        //   ここが「どれを 手ばなすか」の 始まり。だまって 古いのを 捨てて いた ころは
        //   ずっと 標本が 正解で、一度も 迷う ところが なかった
        if (!book.Add(best.kind.id, best.sizeMm)) {
            if (hud != null) hud.Say("かごが いっぱいだ。Z：ずかんで にがすか ひょうほんに する");
            return;
        }
        // 日記に ためる。**その日 何を したかが 夜に 文章に なる**
        if (nikki != null) {
            nikki.Count("bug");
            // **数字を 入れる。**「かぶとむしを つかまえた」より
            // 「78mm の かぶとむしを つかまえた。でかいぜ」
            bool big = best.sizeMm > best.kind.sizeMm * 1.15f;
            string t = string.Format("{0}mm の {1}を つかまえた。{2}",
                                     best.sizeMm, best.kind.name, big ? "でかいぜ" : "");
            // めずらしい 虫（出やすさが 低い）は 重く
            int omoi = best.kind.weight <= 10 ? 40 : 10;
            if (big) omoi += 25;
            nikki.Note("bug_" + best.kind.id, t.TrimEnd(), omoi);
        }
        Pop(best.transform.position);
        best.Catch();
        // 取れたら よろこぶ。**用意して もらった 顔は 使って なんぼ**
        var cs = GetComponent<CharSprite>();
        if (cs != null) cs.ShowMood(CharSprite.Pose.Yorokobi, 1.1f);
    }

    // 取れた ことを つぶで 見せる。
    // ※ はずした ときの 白い つぶは やめた。**あみが 当たっても ふつう 何も 出ない。**
    //   出ると「当てたのに 取れなかった」と 誤って 伝わる
    void Pop(Vector3 at) {
        var go = new GameObject("Pop");
        go.transform.position = at;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.55f; main.startSpeed = 2.2f; main.startSize = 0.10f;
        main.maxParticles = 24; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 1.2f;
        main.startColor = new Color(1f, 0.95f, 0.55f, 1f);
        var em = ps.emission; em.enabled = true; em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)16) });
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.12f;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = PopMat();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Destroy(go, 1.2f);
    }

    // つぶの 材質は **1つ 作って 使いまわす**。取る たびに 作ると たまる 一方
    static Material popMat;
    static Material PopMat() {
        if (popMat != null) return popMat;
        popMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")
                              ?? Shader.Find("Sprites/Default"));
        popMat.SetFloat("_Surface", 1);
        return popMat;
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
