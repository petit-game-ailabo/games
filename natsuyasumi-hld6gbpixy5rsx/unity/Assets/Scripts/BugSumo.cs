using UnityEngine;
using UnityEngine.UI;

// むしずもう。かごの 虫で 大ようせいと 勝負する。
//
// `td/` 版から 引きついだ 芯：**スペース連打で おし返す**、相手は じわじわ 押してくる。
// 変えた ところ：
//  - 出す 虫は「かごの 中で いちばん 強い 1ぴき」。かごが 空なら 挑めない
//    ＝**取ってから 挑む**という 順番が 自然に できる
//  - 強さは 取りにくさと 逆に した。かぶとむしは 取りやすいが いちばん 強い
//    ＝あさ 早く 起きた ごほうびに なる
public class BugSumo : MonoBehaviour {
    public Texture2D atlas;
    public Sprite panel;
    public Font font;

    [Header("さそう 相手")]
    public Transform partner;         // 大ようせい
    public float talkRange = 2.0f;

    [Header("しかけ")]
    // ★つりあい：人が 1秒に 8回ぐらい おす として、
    //   ・強さが 釣りあう 取りくみ(4 vs 4) … 4秒ほど おし続けて やっと 勝てる
    //   ・かぶとむし(6)で 弱い 相手 … 2秒たらずで 押し切れる＝強さが 効く
    //   ・ちょう(1)で 強い 相手(5) … おし続けても じわじわ 負ける
    //   はじめ 0.075 に して いたら 8回 おしただけで 勝ててしまった
    public float pushBack = 0.018f;       // 1回 おした ぶん（強さ 1 あたり）
    public float opponentPush = 0.075f;   // 相手が 1秒に おす ぶん（強さ 1 あたり）
    // **おせる 間かくに 下限を おく。** 連打の 速さだけで 決まると、
    // 道具で 速く おす ほど 有利に なって 虫の 強さが 意味を 失う
    public float pushInterval = 0.08f;

    BugBook book;
    RectTransform root;
    Image myImg, opImg, gaugeFill;
    Text title, hint;
    RectTransform myRT, opRT;

    BugKind mine, opp;
    float pos;                 // -1 まけ 〜 +1 かち
    int phase;                 // 0=やっていない 1=とりくみ 2=けっか
    float resultT, pushCool;
    int wins;

    const string WinKey = "natsuyasumi.sumo.wins.v1";

    public bool Busy { get { return phase != 0; } }

    void Start() {
        book = FindFirstObjectByType<BugBook>();
        wins = PlayerPrefs.GetInt(WinKey, 0);
        Build();
    }

    /// <summary>挑める か（かごに 虫が いる か）</summary>
    public bool CanStart() {
        return phase == 0 && book != null && book.Recent.Count > 0;
    }

    /// <summary>相手の そばに いる か</summary>
    public bool PlayerNear(Transform who) {
        if (partner == null || who == null) return false;
        var d = who.position - partner.position; d.y *= 0.5f;
        return d.sqrMagnitude < talkRange * talkRange;
    }

    /// <summary>そばに いる ときの ひとこと（無ければ null）</summary>
    public string PromptFor(Transform who) {
        if (phase != 0 || !PlayerNear(who)) return null;
        return CanStart() ? "スペース：むしずもうを いどむ"
                          : "むしを つかまえてから おいで";
    }

    public bool Begin() {
        if (!CanStart()) return false;
        // かごの 中で いちばん 強い 1ぴき
        mine = null;
        foreach (var id in book.Recent) {
            var k = BugKind.Of(id);
            if (mine == null || k.power > mine.power) mine = k;
        }
        // 相手は こちらと 釣りあう ぐらいを 選ぶ（毎回 かぶとむしだと つまらない）。
        // **同じ 虫どうしは さける**（同種の 取りくみは 絵が つまらない）
        var pool = BugKind.All;
        opp = pool[Random.Range(0, pool.Length)];
        for (int i = 0; i < 12; i++) {
            bool ok = Mathf.Abs(opp.power - mine.power) <= 2 && opp.id != mine.id;
            if (ok) break;
            opp = pool[Random.Range(0, pool.Length)];
        }

        pos = 0f; phase = 1; resultT = 0f;
        Apply();
        root.gameObject.SetActive(true);
        return true;
    }

    // たしかめの 自動運転から おす（人の 連打の かわり）
    bool debugPush;
    public void DebugPush() { debugPush = true; }
    public string DebugState {
        get {
            return string.Format("phase={0} pos={1:F2} mine={2} opp={3} wins={4}",
                                 phase, pos, mine != null ? mine.name : "-", opp != null ? opp.name : "-", wins);
        }
    }

    void Update() {
        if (phase == 0) return;
        bool pressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)
                    || Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0);
        if (debugPush) { pressed = true; debugPush = false; }

        if (phase == 1) {
            pushCool -= Time.deltaTime;
            pos -= opp.power * opponentPush * Time.deltaTime;
            if (pressed && pushCool <= 0f) { pos += mine.power * pushBack; pushCool = pushInterval; }
            pos = Mathf.Clamp(pos, -1.15f, 1.15f);
            if (pos >= 1f) { phase = 2; resultT = 0f; wins++; PlayerPrefs.SetInt(WinKey, wins); PlayerPrefs.Save(); }
            else if (pos <= -1f) { phase = 2; resultT = 0f; }
            Apply();
        } else {
            resultT += Time.deltaTime;
            if (pressed || resultT > 3.0f) { phase = 0; root.gameObject.SetActive(false); }
        }
    }

    void Apply() {
        if (root == null) return;
        myImg.sprite = SpriteOf(mine); opImg.sprite = SpriteOf(opp);
        // 2ひきが 土俵の 上を 行ったり 来たり する
        float x = pos * 76f;
        myRT.anchoredPosition = new Vector2(-34f + x, 10f);
        opRT.anchoredPosition = new Vector2( 34f + x, 10f);
        gaugeFill.rectTransform.sizeDelta = new Vector2(Mathf.Clamp01(pos * 0.5f + 0.5f) * 240f, 10f);

        if (phase == 1) {
            title.text = "むしずもう！　" + mine.name + " vs " + opp.name;
            hint.text = "スペース れんだ！";
        } else {
            bool win = pos >= 1f;
            title.text = win ? "かった！" : "まけた…";
            hint.text = win ? ("つうさん " + wins + " しょう　　スペースで つづける")
                            : "スペースで つづける";
        }
    }

    Sprite SpriteOf(BugKind k) {
        if (atlas == null) return null;
        int cw = atlas.width / BugKind.Cols, ch = atlas.height / BugKind.Rows;
        int col = k.Index % BugKind.Cols, row = k.Index / BugKind.Cols;
        // Sprite の 座標は 下が 0。絵は 上が 0行め なので ひっくり返す
        var r = new Rect(col * cw, (BugKind.Rows - 1 - row) * ch, cw, ch);
        return Sprite.Create(atlas, r, new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect);
    }

    // ---- 組み立て
    void Build() {
        var hud = FindFirstObjectByType<BugHud>();
        Transform parent = hud != null && hud.CanvasRoot != null ? hud.CanvasRoot : transform;

        root = MakePanel(parent, new Vector2(340f, 176f));
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = new Vector2(0f, 20f);

        title = MakeText(root, TextAnchor.UpperCenter, new Vector2(0f, -12f), new Vector2(0f, -110f));
        hint  = MakeText(root, TextAnchor.LowerCenter, new Vector2(0f, 10f), new Vector2(0f, -110f));

        // 土俵の 線
        var line = new GameObject("Dohyo", typeof(Image));
        line.transform.SetParent(root, false);
        var lrt = line.GetComponent<RectTransform>();
        lrt.anchorMin = lrt.anchorMax = lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.sizeDelta = new Vector2(250f, 2f);
        lrt.anchoredPosition = new Vector2(0f, -8f);
        line.GetComponent<Image>().color = new Color(0.85f, 0.80f, 0.60f, 0.5f);
        line.GetComponent<Image>().raycastTarget = false;

        myImg = MakeBugImage(root, out myRT);
        opImg = MakeBugImage(root, out opRT);
        // **相手は 左右を 返して 向かいあわせる。** 同じ 向きだと 並んで いるだけに 見えた
        opRT.localScale = new Vector3(-1f, 1f, 1f);

        // ゲージ
        var back = new GameObject("Gauge", typeof(Image));
        back.transform.SetParent(root, false);
        var brt = back.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(240f, 10f);
        brt.anchoredPosition = new Vector2(0f, -46f);   // 下の 文字と かさならない ところ
        back.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.16f);
        back.GetComponent<Image>().raycastTarget = false;

        var fill = new GameObject("Fill", typeof(Image));
        fill.transform.SetParent(back.transform, false);
        gaugeFill = fill.GetComponent<Image>();
        var frt = fill.GetComponent<RectTransform>();
        frt.anchorMin = frt.anchorMax = new Vector2(0f, 0.5f);
        frt.pivot = new Vector2(0f, 0.5f);
        frt.anchoredPosition = Vector2.zero;
        frt.sizeDelta = new Vector2(120f, 10f);
        gaugeFill.color = new Color(0.56f, 0.86f, 0.35f);
        gaugeFill.raycastTarget = false;

        root.gameObject.SetActive(false);
    }

    Image MakeBugImage(Transform parent, out RectTransform rt) {
        var go = new GameObject("Bug", typeof(Image));
        go.transform.SetParent(parent, false);
        rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(48f, 48f);
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

    Text MakeText(Transform parent, TextAnchor align, Vector2 offset, Vector2 grow) {
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
