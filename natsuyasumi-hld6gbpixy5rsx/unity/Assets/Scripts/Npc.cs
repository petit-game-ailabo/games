// 話しかけられる 人。（2026-08-17）
//
// ★なぜ 要るか
//   遊ぶ 人からの 言：「村に 誰も いない。50m x 26m の 屋敷に、蔵にも 離れにも
//   縁側にも 誰も いない。左の 町は 家が 5棟 あって 全部 空き家。
//   ぼくなつで いちばん 待って いたのは『きょうは おばさんと 何を 話そう』でした」
//
// ★台詞は **日づけ・時間帯・天気で 変える**。同じ ことしか 言わない 人は
//   1回 話したら 二度と 話しかけられない。日がわりで 一言 変わる だけで
//   「毎日 のぞきに 行く」に なる。
//
// ★台詞は 短く。長い 会話は 読むのが 仕事に なる。**1〜2行**で じゅうぶん。
using UnityEngine;

public class Npc : MonoBehaviour {

    [Header("だれ")]
    public string who = "おばあちゃん";
    [Tooltip("ここまで 近づいたら 話せる")]
    public float range = 2.6f;

    [Header("なにを 言うか")]
    [Tooltip("ふだんの 一言。日づけで 順に 変わる")]
    public string[] lines;
    [Tooltip("あさ だけ の 一言（あれば ゆうせん）")]
    public string[] morning;
    [Tooltip("よる だけ の 一言")]
    public string[] night;
    [Tooltip("雨の 日 だけ の 一言")]
    public string[] rain;
    [Tooltip("むしを たくさん とった 日 の 一言")]
    public string[] manyBugs;

    [HideInInspector] public Transform player;
    [HideInInspector] public BugHud hud;
    [HideInInspector] public Nikki nikki;
    [HideInInspector] public TimeOfDay tod;
    [HideInInspector] public Weather weather;
    [HideInInspector] public BugBook book;

    int step;               // 同じ日に 何回 話したか
    bool wasNear;

    // ★**時間帯で 居る 場所が 変わる。**（遊ぶ 人：「立ち位置固定の 看板の まま。
    //   夜中の 2時に 畑へ 行けば おじさんが 暗闇に 一人で 突っ立って いる」）
    //   「行ったら 居なかった」が 起きる だけで 人は 生き物に なる
    [HideInInspector] public Vector3 posAsa, posHiru, posYoru;
    [HideInInspector] public bool hasMoves;
    [HideInInspector] public bool hideOnRain;    // 雨の 日は 消える（大妖精）
    float moveLeft;

    /// <summary>いま 姿が あるか。**消えて いる ときは 話しかけられない。**
    /// （遊ぶ 人：「雨の 日、大妖精は 見えないのに 話しかけられる。
    ///   姿の 無い 相手が『あめの日の川はこわいです』と 喋る」）</summary>
    public bool Iru {
        get {
            if (!hideOnRain) return true;
            return !(weather != null &&
                     (weather.mode == Weather.Mode.Ame || weather.mode == Weather.Mode.Yudachi));
        }
    }

    public bool Near {
        get {
            if (!Iru) return false;
            if (player == null) return false;
            var d = player.position - transform.position;
            d.y *= 0.5f;
            return d.sqrMagnitude < range * range;
        }
    }

    void Update() {
        bool near = Near;
        if (near != wasNear) { wasNear = near; if (!near) step = 0; }

        moveLeft -= Time.deltaTime;
        if (moveLeft > 0f) return;
        moveLeft = 1.0f;

        // 雨なら 消える
        bool wet = weather != null && (weather.mode == Weather.Mode.Ame || weather.mode == Weather.Mode.Yudachi);
        if (hideOnRain) {
            bool show = !wet;
            // **子が 複数 ある ので ぜんぶ 消す**（1個だけだと 消え残る）
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                if (r.enabled != show) r.enabled = show;
            if (!show) return;
        }
        if (!hasMoves || tod == null) return;
        var want = tod.hour < 10f ? posAsa : (tod.hour < 17.5f ? posHiru : posYoru);
        if ((transform.position - want).sqrMagnitude > 0.04f) transform.position = want;
    }

    /// <summary>足もとに 出す ひとこと</summary>
    public string Prompt { get { return "スペース：" + who + "と はなす"; } }

    /// <summary>話しかけられた</summary>
    public void Talk() {
        string s = Pick();
        if (hud != null) hud.Say(who + "「" + s + "」");
        if (nikki != null) nikki.Talked(who);
        step++;
    }

    string Pick() {
        int d = nikki != null ? nikki.day : 1;
        // **その日 その場の 事情が あれば そちらを ゆうせん。**
        // ふだんの 一言だけだと「置き物」に 見える
        bool wet = weather != null && (weather.mode == Weather.Mode.Ame || weather.mode == Weather.Mode.Yudachi);
        if (wet && Has(rain)) return At(rain, d);
        if (tod != null && Has(night) && (tod.hour >= 18.5f || tod.hour < 5f)) return At(night, d);
        if (tod != null && Has(morning) && tod.hour < 10f) return At(morning, d);
        // ★**きょう とった 数**で 見る。累計(book.Total)だと 2日めから ずっと
        //   これしか 返らなく なり、せっかくの lines が 一生 出ない（遊ぶ 人の 指摘）
        if (nikki != null && Has(manyBugs) && nikki.CountOf("bug") >= 5 && (d + step) % 3 == 0)
            return At(manyBugs, d);
        if (Has(lines)) return At(lines, d + step);
        return "……";
    }

    static bool Has(string[] a) { return a != null && a.Length > 0; }
    static string At(string[] a, int i) { return a[((i % a.Length) + a.Length) % a.Length]; }
}
