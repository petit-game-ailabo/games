// 1日を まわす 係。**寝る → 暗転 → つぎの 朝**。（2026-08-17）
//
// ★遊ぶ 人からの 言：「夜、寝間に もどって 布団に 入りたい。
//   『きょうは 川で 水きりを した。8だん とんだ』と 勝手に 日記に 書かれて いて ほしい。
//   朝、起きて『きょうは 3日め』と 出て ほしい。**残り日数が 減って いく 焦り**が ほしい」
//
// ここが この ゲームの わっか（ループ）。風景では なく これが 本体。
//
//  - 布団に 近づく → スペース → 「ねる」
//  - 画面が 暗く なる → 日記が 出る → スペースで 送る
//  - 明けて つぎの 日の 朝 6時半。日づけが 1 ふえる
//  - 話しかけられる 人・虫の 顔ぶれ・NPCの 台詞が つぎの 日の ものに 変わる
using UnityEngine;
using UnityEngine.UI;

public class DayHost : MonoBehaviour {

    [Header("つなぐ もの")]
    public Transform player;
    public Nikki nikki;
    public TimeOfDay tod;
    public BugHud hud;
    public Font font;
    public Sprite panel;

    [Header("ねる ところ（ふとん）")]
    public Vector3 futon;
    public float futonRange = 2.2f;
    [Tooltip("この 時こく から 寝られる")]
    public float sleepFrom = 18.0f;

    // 画面
    Image fade;
    Text diaryText, dayText;
    RectTransform diaryPanel;

    enum St { Asobu, Kurayami, Nikki, Akeru }
    St st = St.Asobu;
    float t;
    Npc[] npcs;

    void Start() {
        if (nikki == null) nikki = FindFirstObjectByType<Nikki>();
        if (tod == null) tod = FindFirstObjectByType<TimeOfDay>();
        if (hud == null) hud = FindFirstObjectByType<BugHud>();
        npcs = FindObjectsByType<Npc>(FindObjectsSortMode.None);
        Build();
        Refresh();
        // 朝の ひとこと
        if (nikki != null && !nikki.greeted) {
            nikki.greeted = true;
            if (hud != null) hud.Say(nikki.Morning());
        }
    }

    bool NearFuton {
        get {
            if (player == null) return false;
            var d = player.position - futon; d.y *= 0.5f;
            return d.sqrMagnitude < futonRange * futonRange;
        }
    }

    /// <summary>ほかの 仕組み（PlayHost・BugCatcher）が 入力を 取って いいか</summary>
    public bool Busy { get { return st != St.Asobu; } }

    /// <summary>遊び(PlayHost)を 止める か。**人と 話す・ねる ほうが ゆうせん**</summary>
    public bool BlockPlay { get { return Busy || NearNpc() != null || NearFutonNow; } }

    bool NearFutonNow {
        get {
            if (!NearFuton) return false;
            return tod == null || tod.hour >= sleepFrom || tod.hour < 4.5f;
        }
    }

    Npc NearNpc() {
        if (npcs == null || player == null) return null;
        Npc best = null; float bd = float.MaxValue;
        foreach (var n in npcs) {
            if (n == null || !n.Near) continue;
            float d = (n.transform.position - player.position).sqrMagnitude;
            if (d < bd) { bd = d; best = n; }
        }
        return best;
    }

    void Update() {
        switch (st) {
            case St.Asobu: Asobu(); break;
            case St.Kurayami:
                t += Time.deltaTime;
                SetFade(Mathf.Clamp01(t / 1.1f));
                if (t >= 1.1f) {
                    string d = nikki != null ? nikki.Sleep() : "";
                    if (diaryText != null) diaryText.text = d;
                    if (diaryPanel != null) diaryPanel.gameObject.SetActive(true);
                    Refresh();
                    st = St.Nikki; t = 0f;
                }
                break;
            case St.Nikki:
                t += Time.deltaTime;
                // すぐ 押しても 飛ばない ように 少し 待つ
                if (t > 0.6f && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))) {
                    if (diaryPanel != null) diaryPanel.gameObject.SetActive(false);
                    // 朝に する
                    if (tod != null) { tod.hour = 6.5f; tod.runClock = true; tod.useHour = true; }
                    st = St.Akeru; t = 0f;
                }
                break;
            case St.Akeru:
                t += Time.deltaTime;
                SetFade(1f - Mathf.Clamp01(t / 1.3f));
                if (t >= 1.3f) {
                    SetFade(0f);
                    st = St.Asobu;
                    if (nikki != null) {
                        nikki.greeted = true;
                        if (hud != null) hud.Say(nikki.Morning());
                    }
                }
                break;
        }
    }

    void Asobu() {
        if (player == null || nikki == null) return;

        // **人が いたら まず そちら。** ねる より 話す ほうが 手前に ある
        var npc = NearNpc();
        if (npc != null) {
            if (hud != null) hud.SetPrompt(npc.Prompt);
            if (Input.GetKeyDown(KeyCode.Space)) npc.Talk();
            return;
        }
        if (!NearFuton) return;
        bool lateEnough = tod == null || tod.hour >= sleepFrom || tod.hour < 4.5f;
        if (!lateEnough) {
            if (hud != null) hud.SetPrompt("まだ 明るい。夜に なったら ねよう");
            return;
        }
        if (hud != null) hud.SetPrompt("スペース：ねる");
        if (Input.GetKeyDown(KeyCode.Space)) {
            if (hud != null) hud.SetPrompt(null);
            if (tod != null) tod.runClock = false;
            st = St.Kurayami; t = 0f;
        }
    }

    void Refresh() {
        if (dayText == null || nikki == null) return;
        int left = Nikki.LastDay - nikki.day;
        dayText.text = string.Format("8月 {0}日　のこり {1}日", nikki.day, Mathf.Max(0, left));
    }

    /// <summary>時こくの 表示を 毎フレーム 更新（TimeOfDay が 進める）</summary>
    void LateUpdate() {
        if (dayText != null && nikki != null && tod != null) {
            int left = Nikki.LastDay - nikki.day;
            dayText.text = string.Format("8月 {0}日　{1}　のこり {2}日",
                                         nikki.day, tod.ClockText, Mathf.Max(0, left));
        }
    }

    void SetFade(float a) {
        if (fade == null) return;
        var c = fade.color; c.a = a; fade.color = c;
        fade.gameObject.SetActive(a > 0.001f);
    }

    // ---------------------------------------------------------------- 画面
    void Build() {
        var root = hud != null ? hud.CanvasRoot : null;
        if (root == null) return;

        // 日づけ（右上）
        var dayGO = new GameObject("DayPanel");
        var dp = MakePanel(dayGO, root, new Vector2(1f, 1f), new Vector2(-12f, -12f),
                           new Vector2(340f, 34f), new Vector2(1f, 1f));
        dayText = MakeText(dayGO.transform, "8月 1日");
        dayText.alignment = TextAnchor.MiddleCenter;

        // 日記（まん中）
        var diGO = new GameObject("DiaryPanel");
        diaryPanel = MakePanel(diGO, root, new Vector2(0.5f, 0.5f), Vector2.zero,
                               new Vector2(680f, 380f), new Vector2(0.5f, 0.5f));
        diaryText = MakeText(diGO.transform, "");
        diaryText.alignment = TextAnchor.UpperLeft;
        diaryText.lineSpacing = 1.5f;
        var dr = diaryText.rectTransform;
        dr.offsetMin = new Vector2(28f, 44f); dr.offsetMax = new Vector2(-28f, -24f);
        // 「スペースで つぎの 日へ」
        var tip = MakeText(diGO.transform, "スペース：あさに なる");
        tip.alignment = TextAnchor.LowerCenter;
        var tr = tip.rectTransform;
        tr.offsetMin = new Vector2(12f, 14f); tr.offsetMax = new Vector2(-12f, -12f);
        diaryPanel.gameObject.SetActive(false);

        // 暗転（いちばん 上に かぶせる）
        var fGO = new GameObject("Fade");
        fGO.transform.SetParent(root, false);
        fade = fGO.AddComponent<Image>();
        fade.color = new Color(0f, 0f, 0f, 0f);
        fade.raycastTarget = false;
        var fr = fade.rectTransform;
        fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one;
        fr.offsetMin = Vector2.zero; fr.offsetMax = Vector2.zero;
        fGO.transform.SetAsLastSibling();
        fGO.SetActive(false);
    }

    RectTransform MakePanel(GameObject go, Transform root, Vector2 anchor, Vector2 pos,
                            Vector2 size, Vector2 pivot) {
        go.transform.SetParent(root, false);
        var img = go.AddComponent<Image>();
        img.sprite = panel;
        img.type = Image.Type.Sliced;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    Text MakeText(Transform parent, string s) {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font; t.fontSize = 12; t.text = s;
        t.color = new Color(0.96f, 0.93f, 0.84f);
        t.raycastTarget = false;
        var rt = t.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(10f, 8f); rt.offsetMax = new Vector2(-10f, -8f);
        return t;
    }
}
