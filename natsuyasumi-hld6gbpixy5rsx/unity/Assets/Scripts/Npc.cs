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

    // ★**かごの 虫に 反応する。**（2026-08-17）
    //   {0} に 虫の 名が 入る。**はじめて 見せた 虫 だけ** 反応する ので、
    //   新しい 虫を とるたび「あの 人に 見せに 行こう」に なる
    [Tooltip("かごの 虫を はじめて 見せた ときの 一言（{0}＝虫の 名）")]
    public string[] mushi;
    [Tooltip("虫の いる 場所を 教えて くれる 人か（おじさん）")]
    public bool mushiHakase;
    [Tooltip("目の 前で 虫を にがした ときの 一言")]
    public string[] nigasu;
    [Tooltip("祭りの 日に くれる 小づかい（0で なし）")]
    public int kozukai;

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

    // ★**村人が カレンダーを 知って いる。**（遊ぶ 人：「8月30日、みんなが
    //   平常運転で『井戸の 水は つめたい よ』と 言う。魔理沙だけが 朝に つぶやいて 終わり。
    //   **30日目に 村じゅうの 台詞が 変わる。これが 積み重なったの 正体**」）
    [HideInInspector] public Nikki.Koto2 koto;
    public string[] kotoYokoku, kotoMatsuri, kotoToro, kotoTaifu, kotoShukudai, kotoOwakare;
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
        // ★**かごの 中みが さきに 目に 入る。**まだ この 人に 見せて いない 虫が
        //   1ぴきでも いれば、天気の 話より そちらが 先
        if (book != null && Has(mushi)) {
            foreach (var id in book.Recent) {
                if (book.Mita(who, id)) continue;
                book.Miseta(who, id);
                string na = BugKind.Of(id).name;
                string ln = string.Format(At(mushi, (int)id), na);
                if (hud != null) hud.Say(who + "「" + ln + "」");
                if (nikki != null) {
                    nikki.Talked(who);
                    nikki.Note("mise", na + "を " + who + "に 見せた。おどろいて いたぜ", 70);
                }
                step++;
                return;
            }
        }
        // ★**教わった ことは ずかんに のこる。**むしはかせは、まだ 取って いない 虫の
        //   いる ところを 1つずつ 教えて くれる
        if (mushiHakase && book != null) {
            var yet = book.Shiranai();
            if (yet.HasValue) {
                var k = BugKind.Of(yet.Value);
                book.AddHint(yet.Value);
                if (hud != null)
                    hud.Say(who + "「" + k.name + "なら " + k.hint + "。ずかんに 書いて おきな」");
                if (nikki != null) {
                    nikki.Talked(who);
                    nikki.Note("hint", who + "に " + k.name + "の いる ところを 教わった", 60);
                }
                step++;
                return;
            }
        }
        // ★**「はい、これ 小づかい」と 言ったら、ほんとうに わたす。**（2026-08-17）
        //   台詞だけ 出して 財布が 増えないなら、それは また 約束を やぶって いる
        if (kozukai > 0 && nikki != null && koto == Nikki.Koto2.Matsuri
            && Saifu.Madamorattenai(nikki.day)) {
            Saifu.Moratta(nikki.day);
            Saifu.Add(kozukai);
            if (hud != null)
                hud.Say(who + "「はい、これ 小づかい。屋台で 何か おあがり」　（+" + kozukai + "円）");
            nikki.Talked(who);
            nikki.Note("kozukai", "おばあちゃんに 小づかいを " + kozukai + "円 もらった", 65);
            step++;
            return;
        }
        string s = Pick();
        if (hud != null) hud.Say(who + "「" + s + "」");
        if (nikki != null) nikki.Talked(who);
        step++;
    }

    string Pick() {
        int d = nikki != null ? nikki.day : 1;
        // **その日の できごとが あれば まっさきに それ。**
        switch (koto) {
            case Nikki.Koto2.MatsuriYokoku: if (Has(kotoYokoku))  return At(kotoYokoku, d);  break;
            case Nikki.Koto2.Matsuri:       if (Has(kotoMatsuri)) return At(kotoMatsuri, d); break;
            case Nikki.Koto2.Toro:          if (Has(kotoToro))    return At(kotoToro, d);    break;
            case Nikki.Koto2.Taifu:         if (Has(kotoTaifu))   return At(kotoTaifu, d);   break;
            case Nikki.Koto2.Shukudai:      if (Has(kotoShukudai))return At(kotoShukudai, d);break;
            case Nikki.Koto2.Owakare:       if (Has(kotoOwakare)) return At(kotoOwakare, d); break;
        }

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
