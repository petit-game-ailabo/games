using UnityEngine;
using UnityEngine.UI;

// むしずもう。
//
// ★2026-08-15 作りなおし。前は **スペース連打で おしあう** 形だった が、
//   それだと「自分が 虫に なって いる」（本人の 指摘）。実際の 虫ずもうは こう：
//     - 虫は 虫で **かってに 動く**。人は 押しあわない
//     - 人は 筆で **虫の どこかを 刺激する**。つついた ところに よって
//       角を ふりあげたり、踏んばったり する
//     - 効くかどうかは **そのときの 間あい**で 決まる。遠ければ 空ぶり
//   → やじるし＝つつく ところ。スペース＝声えん（連打の 意味を 変えた）。
//     **結果を 送るのは べつの キー**（連打の いきおいで 画面が 飛ばない ように）
public class BugSumo : MonoBehaviour {
    public Texture2D atlas;
    public Sprite panel;
    public Font font;

    [Header("さそう 相手")]
    public Transform partner;
    public float talkRange = 2.0f;

    [Header("しかけ")]
    public float approach = 0.30f;      // 虫が 自分から よって いく はやさ
    public float pushRate = 0.11f;      // 組んだ ときの 押しあい（1秒で どれだけ ずれるか）
    public float pokeCool = 0.34f;      // つぎに つつけるまで
    public float cheerCool = 0.22f;     // 声えんの 間かく（これ以上 連打しても 増えない）

    BugBook book;
    RectTransform root;
    Image myImg, opImg, gaugeFill, cheerFill;
    Text title, hint, legend, callout;
    RectTransform myRT, opRT;
    RectTransform fudeRT;               // つついた ところに 出る 筆
    float fudeLeft;

    BugKind mine, opp;
    int myMm, oppMm;
    float myPow, oppPow;

    float myX, opX;             // 土俵の 上の 位置 -1〜1
    float cheer;                // 声えん 0〜1
    float myGuard, opGuard;     // 踏んばりの のこり 時間
    float myBurst, opBurst;     // 技で 出た 押し
    float pokeLeft, cheerLeft, calloutLeft;
    int phase;                  // 0=やって いない 1=とりくみ 2=けっか
    float resultT;
    int wins;

    const string WinKey = "natsuyasumi.sumo.wins.v1";
    const float Contact = 0.34f;        // これより 近ければ 組んで いる
    // **虫は すりぬけない。**からだの ぶんは あく。
    // ※0.26 では 画の 上で 27px しか 離れず、48px の 絵が 重なって
    //   1ぴきの かたまりに 見えた。絵の 幅に 合わせて 広げて ある
    const float MinGap  = 0.40f;

    public bool Busy { get { return phase != 0; } }

    // ---- つつく ところ と 出る 技
    //
    // ★画面は **真よこから** 見て いる。だから つつく ところも 真よこから 見えるように
    //   とる。「左の わき」は 見えないので やめた（どこを つついたか 分からなかった）
    //     → あたま ← おしり ↑ せなか ↓ おなかの 下
    enum Spot { Head, Tail, Back, Belly }

    struct Move {
        public string name;
        public float near, far;    // 効く 間あい
        public float push;         // 決まった ときの 押し
        public float guard;        // ふんばる 時間
        public float step;         // 自分が 動く ぶん
    }

    static Move MoveFor(BugId id, Spot s) {
        // 虫に よって 技の 名が ちがう（かぶと＝角、くわがた＝大あご、ばった＝けり）
        string horn = id == BugId.Kabuto ? "つのを ふりあげた"
                    : id == BugId.Kuwagata ? "大あごで はさんだ"
                    : id == BugId.Batta ? "うしろあしで けった"
                    : "からだで ぶつかった";
        switch (s) {
            case Spot.Head:    // あたまを つつく → とつげき（間あいが ある ときだけ 効く）
                return new Move { name = "とつげきした", near = 0.30f, far = 0.95f, push = 0.10f, step = 0.20f };
            case Spot.Tail:    // おしりを つつく → ふんばる（いつでも 効く）
                return new Move { name = "ふんばった", near = 0f, far = 2f, guard = 1.3f };
            case Spot.Back:    // せなかを つつく → 角。**組んで いる ときだけ**きまる
                return new Move { name = horn, near = 0f, far = Contact, push = 0.34f };
            default:           // おなかの 下を つつく → いなす。すこし さがる
                return new Move { name = "いなした", near = 0f, far = Contact + 0.14f, push = 0.14f, step = -0.14f };
        }
    }

    // 筆を 出す 場所（自分の 虫から 見た ずれ）と むき。筆は もとは 下むき
    static void FudeAt(Spot s, out Vector2 off, out float rotZ) {
        switch (s) {
            case Spot.Head:  off = new Vector2( 30f,   0f); rotZ = -90f; break;
            case Spot.Tail:  off = new Vector2(-30f,   0f); rotZ =  90f; break;
            case Spot.Back:  off = new Vector2(  0f,  30f); rotZ =   0f; break;
            default:         off = new Vector2(  0f, -28f); rotZ = 180f; break;
        }
    }

    void Start() {
        book = FindFirstObjectByType<BugBook>();
        wins = PlayerPrefs.GetInt(WinKey, 0);
        Build();
    }

    public bool CanStart() { return phase == 0 && book != null && book.Recent.Count > 0; }

    public bool PlayerNear(Transform who) {
        if (partner == null || who == null) return false;
        var d = who.position - partner.position; d.y *= 0.5f;
        return d.sqrMagnitude < talkRange * talkRange;
    }

    public string PromptFor(Transform who) {
        if (phase != 0 || !PlayerNear(who)) return null;
        return CanStart() ? "スペース：むしずもうを いどむ" : "むしを つかまえてから おいで";
    }

    public bool Begin() {
        if (!CanStart()) return false;
        mine = null; myPow = 0f; myMm = 0;
        foreach (var id in book.Recent) {
            var k = BugKind.Of(id);
            int mm = book.MaxMm(id); if (mm <= 0) mm = k.sizeMm;
            float p = PowerOf(k, mm);
            if (mine == null || p > myPow) { mine = k; myPow = p; myMm = mm; }
        }
        var pool = BugKind.All;
        opp = pool[Random.Range(0, pool.Length)];
        for (int i = 0; i < 12; i++) {
            if (Mathf.Abs(opp.power - mine.power) <= 2 && opp.id != mine.id) break;
            opp = pool[Random.Range(0, pool.Length)];
        }
        oppMm = Mathf.RoundToInt(opp.sizeMm * Random.Range(0.85f, 1.25f));
        oppPow = PowerOf(opp, oppMm);

        myX = -0.5f; opX = 0.5f;
        cheer = 0f; myGuard = opGuard = myBurst = opBurst = 0f;
        pokeLeft = cheerLeft = calloutLeft = 0f;
        fudeLeft = 0f; if (fudeRT != null) fudeRT.gameObject.SetActive(false);
        phase = 1; resultT = 0f;
        Say("はっけよい");
        Apply();
        root.gameObject.SetActive(true);
        return true;
    }

    // ---- たしかめの 自動運転から
    Spot? debugPoke; bool debugCheer;
    public void DebugPoke(int dir) { debugPoke = (Spot)Mathf.Clamp(dir, 0, 3); }
    public void DebugCheer() { debugCheer = true; }
    public void DebugPush() { DebugCheer(); }
    public string DebugState {
        get {
            return string.Format("phase={0} my={1:F2} op={2:F2} cheer={3:F2} mine={4} opp={5} wins={6}",
                phase, myX, opX, cheer, mine != null ? mine.name : "-", opp != null ? opp.name : "-", wins);
        }
    }

    void Update() {
        if (phase == 0) return;
        float dt = Time.deltaTime;
        if (calloutLeft > 0f) { calloutLeft -= dt; if (calloutLeft <= 0f && callout != null) callout.text = ""; }
        if (fudeLeft > 0f) { fudeLeft -= dt; if (fudeLeft <= 0f && fudeRT != null) fudeRT.gameObject.SetActive(false); }

        if (phase == 1) Fight(dt);
        else {
            resultT += dt;
            // **結果は べつの キーで 送る。** 技の キーで 送れると、
            // 連打の いきおいで 勝ち負けの 画面が 飛んでしまう
            bool go = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z)
                   || Input.GetKeyDown(KeyCode.Escape);
            if ((go && resultT > 0.8f) || resultT > 7f) { phase = 0; root.gameObject.SetActive(false); }
        }
    }

    void Fight(float dt) {
        pokeLeft -= dt; cheerLeft -= dt;
        myGuard -= dt; opGuard -= dt;
        cheer = Mathf.Max(0f, cheer - dt * 0.13f);          // 声えんは さめて いく

        // ---- 声えん。**連打の 意味を 変えた。**
        // 押しあうのでは なく「虫の 動きが よく なる」。間かくを おいて あるので
        // 壊れるほど 叩く 必要は ない
        if (Input.GetKeyDown(KeyCode.Space) || debugCheer) {
            debugCheer = false;
            if (cheerLeft <= 0f) { cheerLeft = cheerCool; cheer = Mathf.Min(1f, cheer + 0.13f); }
        }

        // ---- 筆で つつく。やじるし＝つつく ところ（真よこ 見に そろえた）
        Spot? s = debugPoke;
        debugPoke = null;
        if (s == null && pokeLeft <= 0f) {
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) s = Spot.Head;
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) s = Spot.Tail;
            else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) s = Spot.Back;
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) s = Spot.Belly;
        }
        if (s != null && pokeLeft <= 0f) {
            pokeLeft = pokeCool;
            ShowFude(s.Value);
            var mv = MoveFor(mine.id, s.Value);
            float gap = opX - myX;
            if (gap >= mv.near && gap <= mv.far) {
                // **効きめは 間あいで 決まる**
                float k = 1f + cheer * 0.6f;
                myBurst += mv.push * k;
                myGuard = Mathf.Max(myGuard, mv.guard);
                myX += mv.step * k;
                Say(mine.name + "は " + mv.name + "！");
            } else {
                Say(gap < mv.near ? "空ぶり…　近すぎた" : "空ぶり…　とどかない");
            }
        }

        // ---- 虫は 虫で かってに 動く（CPU）。人は 押さない
        myX += approach * (1f + cheer * 0.5f) * dt;
        opX -= approach * (0.85f + oppPow * 0.02f) * dt;
        // 相手も ときどき 技を 出す
        if (Random.value < dt * (0.35f + oppPow * 0.05f)) {
            float g2 = opX - myX;
            if (g2 <= Contact) opBurst += 0.22f;
            else if (g2 <= 0.9f) opX -= 0.16f;
        }

        // ---- 組んで いる あいだの おしあい。**土俵ごと ずれる** のが 押しあい
        if (opX - myX <= Contact) {
            float a = myPow * (myGuard > 0f ? 1.55f : 1f) * (1f + cheer * 0.35f) + myBurst * 6f;
            float b = oppPow * (opGuard > 0f ? 1.5f : 1f) + opBurst * 6f;
            float net = (a - b) * pushRate * dt;
            myX += net; opX += net;
        }
        myBurst = Mathf.Max(0f, myBurst - dt * 1.6f);
        opBurst = Mathf.Max(0f, opBurst - dt * 1.6f);

        // ★**すりぬけない ように する。**
        //   これが 無いと 2匹が まったく 同じ ところに 乗って 1匹に 見える。
        //   実際 たしかめの 記録が my=-0.52 op=-0.52 に なって 気づいた
        float mid = (myX + opX) * 0.5f;
        if (opX - myX < MinGap) { myX = mid - MinGap * 0.5f; opX = mid + MinGap * 0.5f; }

        myX = Mathf.Clamp(myX, -1.2f, 1.2f);
        opX = Mathf.Clamp(opX, -1.2f, 1.2f);

        if (opX >= 1f) Win(true);
        else if (myX <= -1f) Win(false);
        Apply();
    }

    void Win(bool won) {
        phase = 2; resultT = 0f;
        if (won) { wins++; PlayerPrefs.SetInt(WinKey, wins); PlayerPrefs.Save(); }
        var cs = FindFirstObjectByType<CharSprite>();
        if (cs != null) cs.ShowMood(won ? CharSprite.Pose.Tanoshii : CharSprite.Pose.Kanashimi, 2.4f);
        if (callout != null) callout.text = "";
        Apply();
    }

    void Say(string s) {
        if (callout == null) return;
        callout.text = s; calloutLeft = 1.5f;
    }

    // つついた ところに 筆を 出す。**どこを つついたか 見えないと
    // やじるしと 技が 結びつかない**ので、これは 飾りでは なく しくみの 一部
    void ShowFude(Spot s) {
        if (fudeRT == null) return;
        Vector2 off; float rot;
        FudeAt(s, out off, out rot);
        fudeRT.anchoredPosition = myRT.anchoredPosition + off;
        fudeRT.localRotation = Quaternion.Euler(0f, 0f, rot);
        fudeRT.gameObject.SetActive(true);
        fudeLeft = 0.26f;
    }

    void Apply() {
        if (root == null) return;
        myImg.sprite = SpriteOf(mine); opImg.sprite = SpriteOf(opp);
        myRT.anchoredPosition = new Vector2(myX * 118f, 6f);
        opRT.anchoredPosition = new Vector2(opX * 118f, 6f);
        float bal = Mathf.Clamp01((myX + opX) * 0.5f * 0.9f + 0.5f);
        gaugeFill.rectTransform.sizeDelta = new Vector2(bal * 240f, 8f);
        cheerFill.rectTransform.sizeDelta = new Vector2(cheer * 240f, 6f);

        if (phase == 1) {
            title.text = string.Format("{0} {1}mm  vs  {2} {3}mm", mine.name, myMm, opp.name, oppMm);
            legend.text = "→あたま　←おしり　↑せなか　↓おなか";
            hint.text = "ふでで つつく　　スペース：おうえん";
        } else {
            bool won = opX >= 1f;
            title.text = won ? "かった！" : "まけた…";
            legend.text = "";
            hint.text = (won ? "つうさん " + wins + " しょう　　" : "") + "Enter か Z で つづける";
        }
    }

    static float PowerOf(BugKind k, int mm) {
        float f = mm <= 0 ? 1f : Mathf.Clamp(mm / (float)k.sizeMm, 0.8f, 1.35f);
        return k.power * f;
    }

    // ★**1回 作ったら 使いまわす。**
    //   Apply() は とりくみの あいだ 毎フレーム 走るので、ここで 作り直して いた ころは
    //   1秒に 120個の Sprite を 生んで いた。ブラウザ版は 使える メモリが 512MB しか ない
    readonly System.Collections.Generic.Dictionary<BugId, Sprite> sprites
        = new System.Collections.Generic.Dictionary<BugId, Sprite>();

    Sprite SpriteOf(BugKind k) {
        if (atlas == null || k == null) return null;
        Sprite s;
        if (sprites.TryGetValue(k.id, out s) && s != null) return s;
        int cw = atlas.width / BugKind.Cols, ch = atlas.height / BugKind.Rows;
        int col = k.Index % BugKind.Cols, row = k.Index / BugKind.Cols;
        var r = new Rect(col * cw, (BugKind.Rows - 1 - row) * ch, cw, ch);
        s = Sprite.Create(atlas, r, new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect);
        sprites[k.id] = s;
        return s;
    }

    // ---- 組み立て
    void Build() {
        var hud = FindFirstObjectByType<BugHud>();
        Transform parent = hud != null && hud.CanvasRoot != null ? hud.CanvasRoot : transform;

        root = MakePanel(parent, new Vector2(360f, 200f));
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = new Vector2(0f, 20f);

        title = MakeText(root, TextAnchor.UpperCenter, new Vector2(0f, -10f));
        callout = MakeText(root, TextAnchor.UpperCenter, new Vector2(0f, -30f));
        callout.color = new Color(1f, 0.92f, 0.55f);
        legend = MakeText(root, TextAnchor.LowerCenter, new Vector2(0f, 22f));
        legend.color = new Color(0.72f, 0.80f, 0.66f);
        hint = MakeText(root, TextAnchor.LowerCenter, new Vector2(0f, 8f));

        var line = new GameObject("Dohyo", typeof(Image));
        line.transform.SetParent(root, false);
        var lrt = line.GetComponent<RectTransform>();
        lrt.anchorMin = lrt.anchorMax = lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.sizeDelta = new Vector2(250f, 2f);
        lrt.anchoredPosition = new Vector2(0f, -14f);
        line.GetComponent<Image>().color = new Color(0.85f, 0.80f, 0.60f, 0.5f);
        line.GetComponent<Image>().raycastTarget = false;

        myImg = MakeBugImage(root, out myRT);
        opImg = MakeBugImage(root, out opRT);
        opRT.localScale = new Vector3(-1f, 1f, 1f);

        gaugeFill = MakeGauge(root, -44f, 8f, new Color(0.56f, 0.86f, 0.35f));
        cheerFill = MakeGauge(root, -58f, 6f, new Color(1f, 0.78f, 0.35f));

        fudeRT = MakeFude(root);

        root.gameObject.SetActive(false);
    }

    // 筆。もちてと 穂さきの 2まいだけ。もとは **下むき**（穂さきが 下）で、
    // つつく ところに あわせて まわす
    RectTransform MakeFude(Transform parent) {
        var go = new GameObject("Fude");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;

        var e = new GameObject("Jiku", typeof(Image));
        e.transform.SetParent(rt, false);
        var ert = e.GetComponent<RectTransform>();
        ert.anchorMin = ert.anchorMax = ert.pivot = new Vector2(0.5f, 0.5f);
        ert.sizeDelta = new Vector2(4f, 20f);
        ert.anchoredPosition = new Vector2(0f, 12f);
        var ei = e.GetComponent<Image>();
        ei.color = new Color(0.80f, 0.68f, 0.42f); ei.raycastTarget = false;

        var h = new GameObject("Ho", typeof(Image));
        h.transform.SetParent(rt, false);
        var hrt = h.GetComponent<RectTransform>();
        hrt.anchorMin = hrt.anchorMax = hrt.pivot = new Vector2(0.5f, 0.5f);
        hrt.sizeDelta = new Vector2(4f, 8f);
        hrt.anchoredPosition = new Vector2(0f, -1f);
        var hi = h.GetComponent<Image>();
        hi.color = new Color(0.24f, 0.20f, 0.17f); hi.raycastTarget = false;

        go.SetActive(false);
        return rt;
    }

    Image MakeGauge(Transform parent, float y, float h, Color c) {
        var back = new GameObject("Gauge", typeof(Image));
        back.transform.SetParent(parent, false);
        var brt = back.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(240f, h);
        brt.anchoredPosition = new Vector2(0f, y);
        back.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.16f);
        back.GetComponent<Image>().raycastTarget = false;

        var fill = new GameObject("Fill", typeof(Image));
        fill.transform.SetParent(back.transform, false);
        var img = fill.GetComponent<Image>();
        var frt = fill.GetComponent<RectTransform>();
        frt.anchorMin = frt.anchorMax = new Vector2(0f, 0.5f);
        frt.pivot = new Vector2(0f, 0.5f);
        frt.anchoredPosition = Vector2.zero;
        frt.sizeDelta = new Vector2(120f, h);
        img.color = c; img.raycastTarget = false;
        return img;
    }

    Image MakeBugImage(Transform parent, out RectTransform rt) {
        var go = new GameObject("Bug", typeof(Image));
        go.transform.SetParent(parent, false);
        rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(44f, 44f);
        var img = go.GetComponent<Image>();
        img.preserveAspect = true; img.raycastTarget = false;
        return img;
    }

    RectTransform MakePanel(Transform parent, Vector2 size) {
        var go = new GameObject("SumoPanel", typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.sprite = panel; img.type = Image.Type.Sliced;
        if (panel == null) img.color = new Color(0.12f, 0.10f, 0.09f, 0.9f);
        return rt;
    }

    Text MakeText(Transform parent, TextAnchor align, Vector2 offset) {
        var go = new GameObject("Text", typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(12f + offset.x, 10f + offset.y);
        rt.offsetMax = new Vector2(-12f + offset.x, -10f + offset.y);
        var t = go.GetComponent<Text>();
        t.font = font; t.fontSize = 12; t.alignment = align;
        t.color = new Color(0.96f, 0.95f, 0.88f);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false; t.supportRichText = false;
        return t;
    }
}
