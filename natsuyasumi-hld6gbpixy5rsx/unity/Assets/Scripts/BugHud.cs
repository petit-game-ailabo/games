using System.Text;
using UnityEngine;
using UnityEngine.UI;

// 画面の 文字まわり。
//  - ひだり上：いま 何しゅるい 何びき
//  - まん中した：取った ときの ひとこと（しばらくで 消える）
//  - Zキー：ずかん（8しゅるい ぜんぶと 取った 数）
//
// 字は PixelMplus（M+ FONT LICENSE・商用可）。**点で 描かれた 書体**なので、
// 12px の 整数倍で 出せば ドット絵と 目の こまかさが そろう。
public class BugHud : MonoBehaviour {
    public Font font;
    public Sprite panel;
    public BugBook book;

    /// <summary>ほかの 画面（むしずもうなど）も この 下に ぶらさげる</summary>
    public Transform CanvasRoot { get; private set; }

    Text counter, toast, bookText, prompt;
    RectTransform promptPanel;
    // ★**足もとに ひとことが 出て いる ときは 右下の 説明を 消す。**
    //   （遊ぶ 人：「スペースが 同時に 2か所 出て いて、どっちが 起きるか 分からない」）
    RectTransform hintPanel;
    RectTransform counterPanel;

    // ★**帳面を ひらいて いる あいだは まわりを 消す。**（2026-08-17）
    //   絵日記の 上に「むしとり 3/8」と「スペース：あみを ふる」が 重なって いた
    bool kakusu;
    public void Chomen(bool hiraita) { kakusu = hiraita; }
    RectTransform toastPanel, bookPanel;
    float toastLeft;
    bool bookOpen;

    const int FontSize = 12;

    // **画面は Awake で 組む。** ほかの 画面（むしずもう）が Start で この 下に
    // ぶらさがりに くるので、そのときには もう できて いないと いけない
    void Awake() { Build(); }

    void Start() {
        if (book == null) book = FindFirstObjectByType<BugBook>();
        if (book != null) book.OnCaught += OnCaught;
        Refresh();
    }

    void OnDestroy() {
        if (book != null) book.OnCaught -= OnCaught;
    }

    void Update() {
        if (toastLeft > 0f) {
            toastLeft -= Time.deltaTime;
            if (toastLeft <= 0f && toastPanel != null) toastPanel.gameObject.SetActive(false);
        }
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Tab)) {
            bookOpen = !bookOpen;
            if (bookPanel != null) bookPanel.gameObject.SetActive(bookOpen);
            if (bookOpen) RefreshBook();
        }
        // **ずかんを ひらいて いる あいだ だけ**、かごの 虫を 逃がす／標本に する。
        // 歩きながら 押せると、うっかり 大物を 標本に して しまう
        if (bookOpen && book != null) {
            if (Input.GetKeyDown(KeyCode.X)) {
                var id = book.Release();
                if (id != null) Say(BugKind.Of(id.Value).name + "を にがした");
                RefreshBook(); Refresh();
            } else if (Input.GetKeyDown(KeyCode.C)) {
                var id = book.MakeSpecimen();
                if (id != null) Say(BugKind.Of(id.Value).name + "を ひょうほんに した");
                RefreshBook(); Refresh();
            }
        }
    }

    void OnCaught(BugCatch c) {
        var k = BugKind.Of(c.id);
        string s = string.Format("{0}　{1}mm　を つかまえた！", k.name, c.sizeMm);
        if (c.firstOfKind) s += "　はじめて！";
        else if (c.record) s += "　★さいだい きろく！";
        Say(s);
        Refresh();
        if (bookOpen) RefreshBook();
    }

    /// <summary>ずかんを ひらく（たしかめの 自動運転からも 呼ぶ）</summary>
    public void OpenBook() {
        bookOpen = true;
        if (bookPanel != null) bookPanel.gameObject.SetActive(true);
        RefreshBook();
    }

    /// <summary>足もとの ひとこと（null で 消す）。毎フレーム 呼ばれる 前提</summary>
    // ★**足もとの ひとことは ここが 決める。**（2026-08-17）
    //   前は BugCatcher と DayHost が それぞれ 好きに 書きこんで いて、
    //   あとから 書いた 空っぽが 先の ひとことを 消して いた
    //  （遊ぶ 人：「スペースが 同時に 2か所 出て いて、どっちが 起きるか 分からない」）。
    //   毎フレーム **候補と 強さ**を もらい、いちばん 強い ものだけ 出す。
    string offerText; int offerRank = int.MinValue;

    /// <summary>足もとに 出したい ひとことを 出す。強い ものが 勝つ</summary>
    public void Offer(string s, int rank) {
        if (string.IsNullOrEmpty(s)) return;
        if (rank <= offerRank) return;
        offerRank = rank; offerText = s;
    }

    /// <summary>むかしの 呼びかた。いまは 弱い Offer と 同じ</summary>
    public void SetPrompt(string s) {
        if (string.IsNullOrEmpty(s)) return;
        Offer(s, 0);
    }

    void LateUpdate() {
        if (promptPanel == null) return;
        if (kakusu) {
            offerText = null; offerRank = int.MinValue;
            if (promptPanel.gameObject.activeSelf) promptPanel.gameObject.SetActive(false);
            if (hintPanel != null && hintPanel.gameObject.activeSelf) hintPanel.gameObject.SetActive(false);
            if (counterPanel != null && counterPanel.gameObject.activeSelf) counterPanel.gameObject.SetActive(false);
            return;
        }
        if (counterPanel != null && !counterPanel.gameObject.activeSelf) counterPanel.gameObject.SetActive(true);
        bool on = !string.IsNullOrEmpty(offerText);
        if (on && prompt.text != offerText) prompt.text = offerText;
        if (promptPanel.gameObject.activeSelf != on) promptPanel.gameObject.SetActive(on);
        // 足もとに 出て いる あいだは 右下を ひっこめる
        if (hintPanel != null && hintPanel.gameObject.activeSelf == on)
            hintPanel.gameObject.SetActive(!on);
        offerText = null; offerRank = int.MinValue;
    }

    public void Say(string s) {
        if (toast == null) return;
        toast.text = s;
        toastPanel.gameObject.SetActive(true);
        toastLeft = 2.6f;
    }

    void Refresh() {
        if (counter == null || book == null) return;
        counter.text = string.Format("むしとり　{0} / {1} しゅるい　　{2} ひき",
                                     book.Kinds, BugKind.All.Length, book.Total);
    }

    /// <summary>にがした ときに そばの 人が 反応する（BugBook.OnFreed から）</summary>
    public void Nigashita(BugId id) {
        Npc best = null; float bd = float.MaxValue;
        foreach (var n in FindObjectsByType<Npc>(FindObjectsSortMode.None)) {
            if (n == null || !n.Near) continue;
            float d = (n.transform.position - transform.position).sqrMagnitude;
            if (d < bd) { bd = d; best = n; }
        }
        // ★**大妖精が「にがして あげて くださいね?」と 言うのに、
        //   にがしても 何も 起きなかった。**彼女に 台詞を 書いた 意味を ここで 出す
        if (best != null && best.nigasu != null && best.nigasu.Length > 0)
            Say(best.who + "「" + best.nigasu[Mathf.Abs(id.GetHashCode()) % best.nigasu.Length] + "」");
    }

    void RefreshBook() {
        if (bookText == null || book == null) return;
        var sb = new StringBuilder();
        sb.AppendLine("― むし ずかん ―");
        sb.AppendLine();
        foreach (var k in BugKind.All) {
            int n = book.Count(k.id);
            if (n <= 0) {
                // 聞いた ヒントが あれば 出す。**聞いた ことが 手もとに のこる**
                sb.AppendLine(book.HasHint(k.id) && !string.IsNullOrEmpty(k.hint)
                              ? "？？？？？　" + k.hint : "？？？？？");
                continue;
            }
            int sp = book.Specimen(k.id);
            // ★**きょ年の さいだい**も 出す。2周目の ずかんが 目標に なる
            //   （これも 作った だけで どこにも 出て いなかった）
            int ky = book.Kyonen(k.id);
            sb.AppendLine(string.Format("{0}　{1}ひき　さいだい {2}mm{3}{4}",
                          k.name, n, book.MaxMm(k.id),
                          sp > 0 ? "　ひょうほん" + sp : "",
                          ky > 0 ? "　（きょ年 " + ky + "）" : ""));
        }
        sb.AppendLine();
        // ★**かごの 虫は 逃がすか 標本に するか。**
        //   どちらも かごから 消える。標本は のこるが 二どと 動かない。
        //   夏の おわりに かごを 空に する ときの、あの 迷いを 出したい
        sb.AppendFormat("― むしかご　{0}/{1} ―\n", book.Recent.Count, book.CageMax);
        if (book.Recent.Count == 0) sb.AppendLine("からっぽ");
        else {
            // ★**1ぴきずつ、あずかった 日数と いっしょに 出す。**（2026-08-17）
            //   3日で 弱り、4日で 逃げる ので、**どれが あぶないか が 見えないと
            //   えらべない**。名を ならべる だけでは 選択に ならなかった
            for (int i = 0; i < book.Recent.Count; i++) {
                int azu = book.Azukari(i);
                sb.AppendFormat("{0}{1}{2}\n",
                    i == 0 ? "▶" : "　",
                    BugKind.Of(book.Recent[i]).name,
                    book.Yowatta(i) ? "　よわって いる！" : (azu > 0 ? "　" + azu + "日め" : ""));
            }
            sb.AppendLine("▶の 1ぴきに　X：にがす　　C：ひょうほんに する");
        }
        sb.AppendLine(string.Format("にがした {0}　ひょうほん {1}", book.Freed, book.SpecimenTotal));
        // ★**持ちがねが 見えないと 貯める 気に ならない。**駄菓子屋の 大かごは 120円
        sb.AppendLine(string.Format("おこづかい　{0} 円", Saifu.Yen));
        sb.AppendLine();
        sb.AppendLine("Z で とじる");
        bookText.text = sb.ToString();
    }

    // ---- 組み立て（絵を 置かずに コードで 作る。GUIを さわらない 方針に そろえる）
    void Build() {
        var canvasGO = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        // **640x360 を 基準に、整数倍で 引きのばす。** 半端に 拡大すると 字が にじむ
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(640, 360);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
        CanvasRoot = canvasGO.transform;

        // ひだり上の 数え
        var cPanel = Panel(canvasGO.transform, new Vector2(0f, 1f), new Vector2(8f, -8f), new Vector2(240f, 26f));
        counter = Label(cPanel, TextAnchor.MiddleLeft, new Vector2(10f, 0f), new Vector2(-20f, 0f));
        counterPanel = cPanel;

        // まん中したの ひとこと
        toastPanel = Panel(canvasGO.transform, new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(340f, 28f));
        toastPanel.pivot = new Vector2(0.5f, 0f);
        toastPanel.anchorMin = toastPanel.anchorMax = new Vector2(0.5f, 0f);
        toastPanel.anchoredPosition = new Vector2(0f, 26f);
        toast = Label(toastPanel, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(-16f, 0f));
        toastPanel.gameObject.SetActive(false);

        // 近づいた ときの ひとこと（ひとことの すぐ 上）
        promptPanel = Panel(canvasGO.transform, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(300f, 26f));
        promptPanel.pivot = new Vector2(0.5f, 0f);
        promptPanel.anchorMin = promptPanel.anchorMax = new Vector2(0.5f, 0f);
        promptPanel.anchoredPosition = new Vector2(0f, 60f);
        prompt = Label(promptPanel, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(-16f, 0f));
        promptPanel.gameObject.SetActive(false);

        // ずかん
        // むしかごの ぶんが 増えた ので 縦を のばす（220 では 字が 枠から 出て いた）。
        // ★かごを **1ぴきずつ 5行** 出す ように した ので さらに のばす。
        //   268 の ままだと「Z で とじる」が 枠の 外に こぼれて いた
        bookPanel = Panel(canvasGO.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(330f, 350f));
        bookPanel.pivot = new Vector2(0.5f, 0.5f);
        bookPanel.anchorMin = bookPanel.anchorMax = new Vector2(0.5f, 0.5f);
        bookPanel.anchoredPosition = Vector2.zero;
        bookText = Label(bookPanel, TextAnchor.UpperLeft, new Vector2(14f, -12f), new Vector2(-28f, -24f));
        bookPanel.gameObject.SetActive(false);

        // みぎしたの 操作の 説明
        var hPanel = Panel(canvasGO.transform, new Vector2(1f, 0f), new Vector2(-8f, 8f), new Vector2(250f, 26f));
        hPanel.pivot = new Vector2(1f, 0f);
        var hint = Label(hPanel, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(-14f, 0f));
        hint.text = "スペース：あみを ふる　　Z：ずかん";
        hintPanel = hPanel;
    }

    RectTransform Panel(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size) {
        var go = new GameObject("Panel", typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(anchor.x, anchor.y);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.sprite = panel;
        img.type = Image.Type.Sliced;
        img.color = Color.white;
        if (panel == null) img.color = new Color(0.12f, 0.10f, 0.09f, 0.85f);
        return rt;
    }

    Text Label(Transform parent, TextAnchor align, Vector2 offset, Vector2 grow) {
        var go = new GameObject("Text", typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(offset.x, offset.y + grow.y);
        rt.offsetMax = new Vector2(offset.x + grow.x, offset.y);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = FontSize;
        t.alignment = align;
        t.color = new Color(0.96f, 0.95f, 0.88f);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.supportRichText = false;
        return t;
    }
}
