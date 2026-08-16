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
    public BugBook book;
    public Font font;
    public Sprite panel;

    [Header("ねる ところ（ふとん）")]
    public Vector3 futon;
    public float futonRange = 2.2f;
    [Tooltip("この 時こく から 寝られる")]
    public float sleepFrom = 18.0f;

    // 画面
    Image fade;
    Text diaryText, dayText, bigDay;
    RectTransform diaryPanel;

    enum St { Asobu, Kurayami, Nikki, Akeru, Owari }
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
        // ★**アプリを 落として 開き直しても 1日を やり直せない。**
        //   （遊ぶ 人：「夕方6時まで 遊んで、寝ずに 落として 開き直すと 朝6時半に もどる。
        //     のこり日数の 焦りを 作った そばから、それを 無効に する 裏口が 開いて いる」）
        if (tod != null && nikki != null && nikki.savedHour > 0.01f) {
            tod.hour = nikki.savedHour; tod.useHour = true;
        }
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
    public bool Busy { get { return st != St.Asobu || readBack >= 0; } }

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
                    // 布団まで はこんで もらう
                    if (player != null) {
                        var cc = player.GetComponent<CharacterController>();
                        if (cc != null) cc.enabled = false;
                        player.position = futon;
                        if (cc != null) cc.enabled = true;
                    }
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
                    // ★**なつやすみが 終わったら まとめを 出す。**
                    //   31日で クランプして いた ころは、翌朝また 31日が 来て いた
                    if (nikki != null && nikki.Owatta) {
                        if (diaryText != null) diaryText.text = nikki.Owari(book);
                        st = St.Owari; t = 0f;
                        break;
                    }
                    if (diaryPanel != null) diaryPanel.gameObject.SetActive(false);
                    // 朝に する
                    if (tod != null) { tod.hour = 6.5f; tod.runClock = true; tod.useHour = true; }
                    st = St.Akeru; t = 0f;
                }
                break;
            case St.Owari:
                t += Time.deltaTime;
                // ★**まとめは スペース 1回で 消えない。**（遊ぶ 人：「31日 かけて 集めた
                //   ずかんと 10日ぶんの 日記が スペース 1回で 消える。一番 残したい ものを
                //   一番 確実に 消す 設計」）→ 3たくに する
                if (t > 1.2f) {
                    if (Input.GetKeyDown(KeyCode.X)) { readBack = 0; ShowPast(); }
                    else if (Input.GetKeyDown(KeyCode.Z)) { if (hud != null) hud.OpenBook(); }
                    else if (Input.GetKeyDown(KeyCode.Return)) {
                        if (nikki != null) nikki.Reset0();
                        if (book != null) book.Clear();
                        if (diaryPanel != null) diaryPanel.gameObject.SetActive(false);
                        if (tod != null) { tod.hour = 6.5f; tod.runClock = true; tod.useHour = true; }
                        Refresh();
                        st = St.Akeru; t = 0f;
                    }
                }
                break;
            case St.Akeru:
                t += Time.deltaTime;
                SetFade(1f - Mathf.Clamp01(t / 1.3f));
                // ★**朝の 一拍。**（遊ぶ 人：「日記を 閉じて、すっと 明るく なって
                //   『8月3日』が どんと 出る 一拍が ほしい。いまは 前の 日と 地つづき すぎる」）
                if (bigDay != null) {
                    bigDay.gameObject.SetActive(t < 1.6f);
                    if (nikki != null) bigDay.text = "8月 " + nikki.day + "日";
                    var c = bigDay.color; c.a = Mathf.Clamp01(1.6f - t) * 0.95f; bigDay.color = c;
                }
                if (t >= 1.3f) {
                    SetFade(0f);
                    st = St.Asobu;
                    if (nikki != null) {
                        nikki.greeted = true;
                        if (hud != null) {
                            hud.Say(carried ? "気が ついたら 布団の 中だったぜ。だれが はこんだんだ？"
                                            : nikki.Morning());
                        }
                        carried = false;
                        // ★**きょうの できごとを 知らせる。**予告が あるから 明日が 待ち遠しく なる
                        news = nikki.TodayNews();
                        newsLeft = news != null ? 3.4f : 0f;
                    }
                }
                break;
        }
    }

    float nagged;
    bool carried;      // 力ずくで 寝かされた＝だれかが 布団まで はこんだ

    int readBack = -1;

    /// <summary>すぎた 日の 日記を 見せる（X）</summary>
    void ShowPast() {
        if (nikki == null || nikki.Past.Count == 0) {
            if (hud != null) hud.Say("まだ 日記は 1日も 書いて いない");
            return;
        }
        readBack = Mathf.Clamp(readBack, 0, nikki.Past.Count - 1);
        int i = nikki.Past.Count - 1 - readBack;
        if (diaryText != null)
            diaryText.text = nikki.Past[i] + "\n← → で 前後の 日　　X：とじる";
        if (diaryPanel != null) diaryPanel.gameObject.SetActive(true);
    }

    void Asobu() {
        if (player == null || nikki == null) return;

        // ★**日記は 読み返せる。**（遊ぶ 人：「Past に 10日ぶん ためて いるのに、
        //   プレイヤーが 読む 手段が ない。31日目に『あの日 こんな ことしたな』と
        //   遡れる ことが、積み重ねの 実感そのもの」）
        if (readBack >= 0) {
            if (Input.GetKeyDown(KeyCode.X)) {
                readBack = -1;
                if (diaryPanel != null) diaryPanel.gameObject.SetActive(false);
            } else if (Input.GetKeyDown(KeyCode.RightArrow)) { readBack++; ShowPast(); }
            else if (Input.GetKeyDown(KeyCode.LeftArrow)) { readBack = Mathf.Max(0, readBack - 1); ShowPast(); }
            return;
        }
        if (Input.GetKeyDown(KeyCode.X)) { readBack = 0; ShowPast(); return; }

        // ★**夜ふかしは できない。**（遊ぶ 人：「布団に 行かなければ 深夜3時の 8月1日を
        //   延々と 遊べる。時計と 暦が つながって いない」）
        //   23時を すぎたら せかし、深夜2時で 力ずくで 寝かせる
        if (tod != null && tod.runClock) {
            float h = tod.hour;
            bool fukashi = h >= 23f || h < 4.5f;
            if (h >= 2f && h < 4.5f) {                 // 深夜2時：ここまで
                if (hud != null) hud.Say("もう 目を あけて いられない…");
                if (tod != null) tod.runClock = false;
                // ★**気を うしなった 場所で 起きない。**（遊ぶ 人：「裏山の 高台で
                //   2時を むかえると、そこで 暗転し、6時半に 高台で 立ったまま 起きます」）
                carried = true;
                st = St.Kurayami; t = 0f;
                return;
            }
            if (fukashi) {
                nagged -= Time.deltaTime;
                if (nagged <= 0f) {
                    nagged = 22f;
                    if (hud != null) hud.Say("もう おそい。かえって ねよう");
                }
            }
        }

        // **人が いたら まず そちら。** ねる より 話す ほうが 手前に ある
        var npc = NearNpc();
        if (npc != null) {
            if (hud != null) hud.Offer(npc.Prompt, 50);
            if (Input.GetKeyDown(KeyCode.Space)) npc.Talk();
            return;
        }
        if (!NearFuton) return;
        bool lateEnough = tod == null || tod.hour >= sleepFrom || tod.hour < 4.5f;
        if (!lateEnough) {
            if (hud != null) hud.Offer("まだ 明るい。夜に なったら ねよう", 60);
            return;
        }
        if (hud != null) hud.Offer("スペース：ねる", 60);
        if (Input.GetKeyDown(KeyCode.Space)) {
            if (tod != null) tod.runClock = false;
            st = St.Kurayami; t = 0f;
        }
    }

    string news; float newsLeft;

    void Refresh() {
        if (dayText == null || nikki == null) return;
        int left = Nikki.LastDay - nikki.day;
        dayText.text = string.Format("8月 {0}日　のこり {1}日", nikki.day, Mathf.Max(0, left));
    }

    /// <summary>時こくの 表示を 毎フレーム 更新（TimeOfDay が 進める）</summary>
    void LateUpdate() {
        // 朝の 知らせは ひとことの あとで 出す
        if (newsLeft > 0f) {
            newsLeft -= Time.deltaTime;
            if (newsLeft <= 0f && news != null && hud != null) { hud.Say(news); news = null; }
        }
        // 時こくを おぼえる（アプリを 落として 開き直しても 1日を やり直せない ように）
        if (nikki != null && tod != null) nikki.savedHour = tod.hour;
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

        // 朝の「8月○日」（大きく まん中）
        var bigGO = new GameObject("BigDay");
        bigGO.transform.SetParent(root, false);
        bigDay = bigGO.AddComponent<Text>();
        bigDay.font = font; bigDay.fontSize = 48; bigDay.text = "";
        bigDay.alignment = TextAnchor.MiddleCenter;
        bigDay.color = new Color(1f, 0.97f, 0.88f, 0f);
        bigDay.raycastTarget = false;
        var br = bigDay.rectTransform;
        br.anchorMin = new Vector2(0.5f, 0.5f); br.anchorMax = new Vector2(0.5f, 0.5f);
        br.sizeDelta = new Vector2(700f, 120f);
        br.anchoredPosition = new Vector2(0f, 60f);
        bigGO.SetActive(false);

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
