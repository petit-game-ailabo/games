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

    public bool Near {
        get {
            if (player == null) return false;
            var d = player.position - transform.position;
            d.y *= 0.5f;
            return d.sqrMagnitude < range * range;
        }
    }

    void Update() {
        bool near = Near;
        if (near != wasNear) { wasNear = near; if (!near) step = 0; }
    }

    /// <summary>足もとに 出す ひとこと</summary>
    public string Prompt { get { return "スペース：" + who + "と はなす"; } }

    /// <summary>話しかけられた</summary>
    public void Talk() {
        string s = Pick();
        if (hud != null) hud.Say(who + "「" + s + "」");
        if (nikki != null) nikki.Note("talk_" + who, who + "と はなした。");
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
        if (book != null && Has(manyBugs) && book.Total >= 6) return At(manyBugs, d);
        if (Has(lines)) return At(lines, d + step);
        return "……";
    }

    static bool Has(string[] a) { return a != null && a.Length > 0; }
    static string At(string[] a, int i) { return a[((i % a.Length) + a.Length) % a.Length]; }
}
