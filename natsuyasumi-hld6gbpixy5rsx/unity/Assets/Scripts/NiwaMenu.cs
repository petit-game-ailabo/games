using System.Collections.Generic;
using UnityEngine;

// めにゅー（2026-09-05）。M で 開き、左右で ページを 切りかえる。
//
// ★本人「メニューボタンみたいなものを用意して、それを押すとメニュー画面が開いて、
//   虫かごの中とかをみれるようにしよう。虫かご以外に町の地図とか絵日記とかも観れるようにしようと思ってる」
//
// いまは **むしかご だけ 中みが ある**。ちず と えにっき は 枠だけ 置いて おく
// （あとから 中みを 入れる ときに 場所を さがさなくて すむ）。
// 開いて いる あいだは 主人公を 止め、スペースの 案内も 消す（`NiwaDougu` が 見て いる）。
public class NiwaMenu : MonoBehaviour {
    public NiwaDougu dougu;
    public NiwaMushi mushi;
    public MuraMove mv;
    public Font font;
    public KeyCode kii = KeyCode.M;

    static readonly string[] PAGE = { "むしかご", "ちず", "えにっき" };
    int page;
    bool hiraita;

    public bool Hiraiteru { get { return hiraita; } }

    // 撮影用（-menu）：8秒たったら ひとりでに 開く。**先に 虫を つかまえさせて から**
    // 開かないと 中みが 空の 画しか 撮れない
    bool autoMenu;
    void Start() {
        foreach (var a in System.Environment.GetCommandLineArgs()) if (a == "-menu") autoMenu = true;
    }

    void Update() {
        if (autoMenu && !hiraita && Time.time > 8f) {
            autoMenu = false; hiraita = true;
            if (mv != null) mv.Tomeru = true;
        }
        if (Input.GetKeyDown(kii) || (hiraita && Input.GetKeyDown(KeyCode.Escape))) {
            hiraita = !hiraita;
            if (mv != null) mv.Tomeru = hiraita;
        }
        NiwaMushi.Kakusu = hiraita;
        if (!hiraita) return;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            page = (page + 1) % PAGE.Length;
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            page = (page + PAGE.Length - 1) % PAGE.Length;
    }

    void OnDisable() {
        if (hiraita && mv != null) mv.Tomeru = false;
        NiwaMushi.Kakusu = false;
    }

    void OnGUI() {
        if (!hiraita) return;
        if (font != null) GUI.skin.font = font;
        int W = Screen.width, H = Screen.height;
        // 画面ぜんたいを 落として、まん中に 帳面を 1枚
        var c0 = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture);
        GUI.color = new Color(0.16f, 0.13f, 0.10f, 0.96f);
        var waku = new Rect(W * 0.5f - 380, H * 0.5f - 250, 760, 500);
        GUI.DrawTexture(waku, Texture2D.whiteTexture);
        GUI.color = c0;

        // ---- 見出し（いまの ページ）
        float x = waku.x + 26f;
        for (int i = 0; i < PAGE.Length; i++) {
            NiwaDougu.Ichigyou(new Rect(x, waku.y + 18f, 200f, 28f),
                               (i == page ? "▼" : "　") + PAGE[i]);
            x += 150f;
        }
        NiwaDougu.Ichigyou(new Rect(waku.xMax - 250f, waku.y + 18f, 240f, 24f), "←→：ページ　Ｍ：とじる");

        var naka = new Rect(waku.x + 26f, waku.y + 64f, waku.width - 52f, waku.height - 92f);
        if (page == 0) Kago(naka);
        else NiwaDougu.Ichigyou(new Rect(naka.x, naka.y + 40f, naka.width, 28f),
                                "まだ つくって いない");
    }

    /// <summary>むしかご：つかまえた 虫を 種類ごとに。絵は 寄りカードと 同じ もの</summary>
    void Kago(Rect r) {
        if (dougu == null || dougu.Kago.Count == 0) {
            NiwaDougu.Ichigyou(new Rect(r.x, r.y + 40f, r.width, 28f), "まだ なにも いない");
            return;
        }
        var namae = new List<string>(); var kazu = new List<int>();
        foreach (var k in dougu.Kago) {
            int i = namae.IndexOf(k);
            if (i < 0) { namae.Add(k); kazu.Add(1); } else kazu[i]++;
        }
        const float CW = 176f, CH = 132f;
        for (int i = 0; i < namae.Count; i++) {
            float cx = r.x + (i % 4) * CW, cy = r.y + (i / 4) * CH;
            var tex = Ekaki(namae[i]);
            if (tex != null) GUI.DrawTexture(new Rect(cx + 22f, cy, 128f, 96f), tex,
                                             ScaleMode.ScaleToFit);
            NiwaDougu.Ichigyou(new Rect(cx, cy + 96f, CW, 24f), namae[i] + " x" + kazu[i]);
        }
        NiwaDougu.Ichigyou(new Rect(r.x, r.yMax - 26f, r.width, 24f),
                           "ぜんぶで " + dougu.Kago.Count + "ひき");
    }

    Texture2D Ekaki(string na) {
        if (mushi == null || mushi.shu == null) return null;
        foreach (var s in mushi.shu) if (s.name == na) return s.Card;
        return null;
    }
}
