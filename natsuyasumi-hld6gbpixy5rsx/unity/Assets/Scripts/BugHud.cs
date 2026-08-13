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
    public void SetPrompt(string s) {
        if (promptPanel == null) return;
        bool on = !string.IsNullOrEmpty(s);
        if (on && prompt.text != s) prompt.text = s;
        if (promptPanel.gameObject.activeSelf != on) promptPanel.gameObject.SetActive(on);
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

    void RefreshBook() {
        if (bookText == null || book == null) return;
        var sb = new StringBuilder();
        sb.AppendLine("― むし ずかん ―");
        sb.AppendLine();
        foreach (var k in BugKind.All) {
            int n = book.Count(k.id);
            sb.AppendLine(n > 0 ? string.Format("{0}　{1}ひき　さいだい {2}mm", k.name, n, book.MaxMm(k.id))
                                : "？？？？？");
        }
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
        bookPanel = Panel(canvasGO.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 220f));
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
