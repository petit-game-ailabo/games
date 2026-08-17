// 日づけと 日記。**この ゲームの 背骨。**（2026-08-17）
//
// ★なぜ 要るか
//   遊ぶ 人からの 言：「1日が 終わらない＝何も 積み重ならない。
//   時計が 42分で 一周する だけで、寝る ことも、日が 変わる ことも、
//   日記を 書く ことも できない。いくら 家を 精巧に 作っても 散歩デモから 抜け出せない」
//   まったく その とおりで、『ぼくのなつやすみ』の 芯は
//   **朝 起きる → きょうは 何を しよう → 夜 ねる → つぎの 日が 来る** の くりかえし。
//   風景では なく **ここ**が 遊びの 本体。
//
// ★日記は「その日 やった こと」を ためて、寝る ときに **3〜4行に 組み立てる**。
//   全文を 書き分ける のでは なく、**型に はめこむ**（雛形＋差しこみ）。
//   口ぶりは 魔理沙（「〜だぜ」「〜なんだ」）。
using System.Collections.Generic;
using UnityEngine;

public class Nikki : MonoBehaviour {

    public const int LastDay = 31;              // 夏休みは 31日

    [Header("いま")]
    public int day = 1;                         // 何日め
    [Tooltip("けさ 起きた ことを もう 出したか")]
    public bool greeted;

    /// <summary>きょう あった こと（種類ごとに 1回だけ ためる）</summary>
    readonly List<Koto> today = new List<Koto>();
    readonly HashSet<string> todaySeen = new HashSet<string>();
    /// <summary>すぎた 日の 日記。あとから 読み返せる</summary>
    public readonly List<string> Past = new List<string>();

    /// <summary>きょう 話した 人（日記では 1行に まとめる）</summary>
    readonly List<string> talked = new List<string>();
    public void Talked(string who) { if (!talked.Contains(who)) talked.Add(who); }

    public System.Action<int> OnNewDay;         // 日が 変わった
    public System.Action<string> OnDiary;       // 日記が できた

    const string KeyDay = "natsu_day";
    const string KeyHour = "natsu_hour";
    const string KeyPast = "natsu_nikki";
    // ★**絵日記は「まいすう」では ない。**（2026-08-17・遊ぶ 人の 指摘）
    //   「31まい きっちり 書き上げても、1まいも 書かなくても、
    //     8月31日の まとめは 一字一句 おなじです」
    //   「ぼくなつの 絵日記が 良かったのは、めくって 読み返せるから です。
    //     31まい ためて、さいごに 1まいずつ めくって『ああ、この日は 雨だったな』と やる」
    //   → 中みを そのまま ためる。まとめにも 効かせる
    const string KeyEnikki = "natsu_enikki";
    const string KeyEnikkiDay = "natsu_enikki_day";
    public const int EnikkiZen = 31;                 // 全ページ数
    public readonly List<string> Enikki = new List<string>();
    public int EnikkiMai { get { return Enikki.Count; } }
    /// <summary>きょうの ぶんは もう 書いたか（1日 1ページ）</summary>
    public bool EnikkiKyou { get { return PlayerPrefs.GetInt(KeyEnikkiDay, 0) == day; } }

    /// <summary>絵日記を 1ページ 書く。**その日 やった ことが そのまま ページに なる**</summary>
    public string EnikkiKaku() {
        string page = Compose();
        Enikki.Add(page);
        PlayerPrefs.SetInt(KeyEnikkiDay, day);
        SaveEnikki();
        return page;
    }

    void SaveEnikki() {
        var sb = new System.Text.StringBuilder();
        foreach (var t in Enikki) sb.Append(t).Append("\u241e");
        PlayerPrefs.SetString(KeyEnikki, sb.ToString());
        PlayerPrefs.Save();
    }

    void Awake() {
        Load();
        // 絵を 撮る ための 決めうち。-day 10 で 祭りの 日を 出せる
        var a = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++) {
            if (a[i] != "-day") continue;
            int v; if (int.TryParse(a[i + 1], out v)) day = Mathf.Clamp(v, 1, LastDay);
        }
    }

    /// <summary>きょうの 出来事を ためる。同じ ことは 1日 1回だけ。
    /// ★**omoi（重み）が 大きい ものだけ 日記に 書く。**
    ///   遊ぶ 人からの 言：「全部 書いたら 日記では なく レシート」。
    ///   はじめての 虫 100 ／ 記録更新 90 ／ はじめての 遊び 80 ／ 遊び 50 ／
    ///   めずらしい 虫 40 ／ ふつうの 虫 10 ／ 会話 5</summary>
    public void Note(string key, string text, int omoi = 10) {
        if (string.IsNullOrEmpty(text)) return;
        if (!todaySeen.Add(key)) return;        // もう ある
        today.Add(new Koto { text = text, omoi = omoi });
    }

    public struct Koto { public string text; public int omoi; }

    /// <summary>何回 やったかを かぞえる もの（虫の 数など）は こちら</summary>
    readonly Dictionary<string, int> counts = new Dictionary<string, int>();
    public void Count(string key) {
        int n; counts.TryGetValue(key, out n); counts[key] = n + 1;
    }
    public int CountOf(string key) { int n; counts.TryGetValue(key, out n); return n; }

    /// <summary>けさの ひとこと</summary>
    public string Morning() {
        int left = LastDay - day;
        if (day == 1) return "8月1日。ひと月ぜんぶ おれの ものだぜ！";
        if (left <= 0) return "8月31日。とうとう さいごの 日か…";
        if (left <= 3) return string.Format("8月{0}日。のこり {1}日。まだ やりのこしが あるぜ", day, left);
        return string.Format("8月{0}日。きょうは 何を して あそぼうか", day);
    }

    /// <summary>寝る ときに 日記を 組み立てる。**型に はめこむ**。
    /// ★重みの 大きい ものから 2〜3件だけ。全部 ならべると レシートに なる</summary>
    public string Compose() {
        var sb = new System.Text.StringBuilder();
        sb.AppendFormat("― 8月{0}日 ―\n\n", day);

        // 重い 順に ならべかえて 上から 3つ
        var sorted = new List<Koto>(today);
        sorted.Sort((a, b) => b.omoi.CompareTo(a.omoi));
        int wrote = 0;
        foreach (var k in sorted) {
            if (wrote >= 3) break;
            sb.AppendLine(k.text);
            wrote++;
        }

        int bugs = CountOf("bug");
        if (wrote == 0 && bugs > 0) sb.AppendLine("むしを " + bugs + "ひき つかまえた。まあまあだな。");
        else if (bugs >= 8) sb.AppendLine("ほかにも いろいろ とった。きょうは むしとりの 日だったな。");

        // 会話は 1行に まとめる
        if (talked.Count == 1) sb.AppendLine(talked[0] + "と 話した。");
        else if (talked.Count >= 2) sb.AppendLine(talked.Count + "人と 話した。");

        if (wrote == 0 && bugs == 0 && talked.Count == 0)
            sb.AppendLine("とくに 何も しなかった。ぼーっと して いたら 日が くれた。");

        sb.AppendLine();
        int left = LastDay - day;
        if (left <= 0)      sb.AppendLine("あしたには 帰らないと いけない。");
        else if (left <= 3) sb.AppendLine("なつやすみも あと " + left + "日。みじかいぜ。");
        else                sb.AppendLine("あしたは どこへ 行こうかな。");
        return sb.ToString();
    }

    /// <summary>ひと夏の まとめ（8月31日の あと）</summary>
    public string Owari(BugBook book) {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("― なつやすみが おわった ―\n");
        if (book != null) {
            sb.AppendFormat("つかまえた むし　{0} しゅるい　{1} ひき\n", book.Kinds, book.Total);
            sb.AppendFormat("ひょうほんに した むし　{0}\n", book.SpecimenTotal);
        }
        if (book != null) sb.AppendFormat("にがした むし　{0} ひき\n", book.Freed);
        sb.AppendFormat("書いた 日記　{0} 日ぶん\n", Past.Count);
        // ★**焦らせた 先に 何かを 置く。**（遊ぶ 人：「25日の『宿題は やったのかい』も、
        //   『……◯日ぶん たまって いる』も、結局 なにも 起きません。
        //   焦らせる 仕掛けを 作って、焦った 先に 何も 置いて いない」）
        int e = Enikki.Count;
        sb.AppendFormat("えにっき　{0} / {1} まい\n\n", e, EnikkiZen);
        sb.AppendLine("ひと月、あっという間だったぜ。");
        if (e >= EnikkiZen)
            sb.AppendLine("えにっきも ぜんぶ 書ききった。おれ、やれば できるんだぜ。");
        else if (e >= EnikkiZen * 2 / 3)
            sb.AppendLine("えにっきは " + e + "まい。……まあ、なんとか なるだろ。");
        else if (e > 0)
            sb.AppendLine("……えにっきは " + e + "まいで 止まって いる。");
        else
            sb.AppendLine("……えにっきは 1まいも 書いて いない。");
        if (e < EnikkiZen)
            sb.AppendLine("8月31日の 夜は、ながい 夜に なりそうだ。");
        sb.AppendLine("……また 来年、来るからな。\n");
        sb.AppendLine("X：日記を 読み返す　　Z：ずかんを 見る　　Enter：はじめから");
        return sb.ToString();
    }

    /// <summary>寝る。日記を 出して つぎの 日へ</summary>
    public string Sleep() {
        string diary = Compose();
        Past.Add(diary);
        if (Past.Count > 40) Past.RemoveAt(0);
        // ★**31日で 止めない。**（遊ぶ 人：「8月31日に 寝ると、翌朝また 8月31日。
        //   永久に。エンディングが 存在しません」）→ こえたら Owari
        day = day + 1;
        today.Clear(); todaySeen.Clear(); counts.Clear(); talked.Clear();
        greeted = false;
        Save();
        if (OnDiary != null) OnDiary(diary);
        if (OnNewDay != null) OnNewDay(day);
        return diary;
    }

    public void Save() {
        PlayerPrefs.SetInt(KeyDay, day);
        PlayerPrefs.SetFloat(KeyHour, savedHour);
        // 日記は 最後の 10日ぶんだけ のこす（長くなりすぎない ように）
        int keep = Mathf.Min(10, Past.Count);
        var sb = new System.Text.StringBuilder();
        for (int i = Past.Count - keep; i < Past.Count; i++) sb.Append(Past[i]).Append("␞");
        PlayerPrefs.SetString(KeyPast, sb.ToString());
        PlayerPrefs.Save();
    }

    /// <summary>時こくも おぼえる（いま 何時に 寝たか）</summary>
    public float savedHour = 6.5f;

    void Load() {
        savedHour = PlayerPrefs.GetFloat(KeyHour, 6.5f);
        day = Mathf.Clamp(PlayerPrefs.GetInt(KeyDay, 1), 1, LastDay + 1);
        Past.Clear();
        string s = PlayerPrefs.GetString(KeyPast, "");
        if (!string.IsNullOrEmpty(s))
            foreach (var t in s.Split('␞'))
                if (!string.IsNullOrEmpty(t)) Past.Add(t);
        // 絵日記は **31まい ぜんぶ のこす**（日記は 直近10日だけ だが、
        // こちらは 8月31日に 1まいずつ めくる ためのもの）
        Enikki.Clear();
        string e = PlayerPrefs.GetString(KeyEnikki, "");
        if (!string.IsNullOrEmpty(e))
            foreach (var t in e.Split('␞'))
                if (!string.IsNullOrEmpty(t)) Enikki.Add(t);
    }

    // ================= カレンダー =================
    // ★遊ぶ 人からの 言：「8月2日と 8月28日で、起きる ことが 何ひとつ 違わない。
    //   のこり28日は **減って いく だけの 数字**。減った 先に 何も 無い。
    //   ぼくなつで 8月15日が 特別だったのは、**カレンダーの 上に 事件が 置いて あった**から」
    //
    // ★**予告が あることが 肝心。**「あさって 祭りだ」と 聞いた 瞬間に、
    //   プレイヤーは あした 寝る 理由が できる。
    public const int MatsuriDay = 10;

    public enum Koto2 {
        Nashi, Niji, MatsuriYokoku, Matsuri, Toro, Taifu, Shukudai, Owakare,
        // ★2026-08-17 に 足した ぶん（遊ぶ 人の 指摘）
        Yoimiya,      // 祭りの 前の 晩（宵宮）。**取り逃しを 減らす**
        Atokatazuke,  // 祭りの あくる朝。落ちた 提灯＝「あった」証拠
        Yokan,        // 別れの 予告（28日）
        Omiyage,      // 「おみやげ、なにが いい?」（29日）
        Saigo,        // 8月31日。**夕方、縁側に みんなが 集まる**
    }

    /// <summary>その日の できごと</summary>
    public Koto2 Today() { return OnDay(day); }
    public static Koto2 OnDay(int d) {
        switch (d) {
            case 5:  return Koto2.Niji;           // 夕立の あと 虹
            case 8:  return Koto2.MatsuriYokoku;  // 「あさって 祭りだ」
            case 10: return Koto2.Matsuri;        // 祠に 提灯。夜だけ 屋台
            case 15: return Koto2.Toro;           // 川に とうろうを ながす
            case 20: return Koto2.Taifu;          // 台風。一日じゅう 雨
            case 25: return Koto2.Shukudai;       // 「宿題は やったのかい」
            case 30: return Koto2.Owakare;        // みんなが「もう 帰るのかい」
            // ★**祭りは 2晩。**（遊ぶ 人：「金魚すくいを 丸ごと 1つ 作って、
            //   稼働率は 31日中 1日。取り逃したら 2周目まで 遊べない」）
            case 9:  return Koto2.Yoimiya;        // 宵宮。提灯は もう ついて いる
            case 11: return Koto2.Atokatazuke;    // あくる朝、祠に 提灯が 1つ 落ちて いる
            // ★**最後の 一週間が いちばん 平坦だった。**山は 10日の 祭り、
            //   そこから 20日 下がりっぱなし。落ちを ここに 置く
            case 28: return Koto2.Yokan;          // 大妖精「いつまで いるんですか……?」
            case 29: return Koto2.Omiyage;        // おばあちゃん「おみやげ、なにが いい?」
            case 31: return Koto2.Saigo;          // 夕方、縁側に みんなが 集まる
        }
        return Koto2.Nashi;
    }

    /// <summary>朝に 出す できごとの 知らせ</summary>
    public string TodayNews() {
        switch (Today()) {
            case Koto2.Niji:          return "ゆうべの 雨の あとだ。空が やけに きれいだぜ";
            case Koto2.MatsuriYokoku: return "なんだか 村が そわそわ して いるな";
            case Koto2.Matsuri:       return "きょうは 祭りだ！ 夜が たのしみだぜ";
            case Koto2.Yoimiya:       return "祠に 提灯が ついて いる。……きょうは 宵宮 らしい";
            case Koto2.Atokatazuke:   return "祭りは 終わったか。……なんだか 静かだぜ";
            case Koto2.Yokan:         return "のこり 4日。……なんだか 落ちつかないな";
            case Koto2.Omiyage:       return "あさっては もう 8月31日か";
            case Koto2.Saigo:         return "きょうで さいごだ。……夕がた、縁側に 行って みるか";
            case Koto2.Toro:          return "きょうは とうろう ながしの 日 らしい";
            case Koto2.Taifu:         return "うわ、すごい 雨だ。きょうは 外に 出られないな";
            case Koto2.Shukudai:      return "……そういえば 宿題、まだ 手を つけて ないぜ";
            case Koto2.Owakare:       return "あしたで さいごか。なんだか さみしいな";
        }
        return null;
    }

    /// <summary>はじめから やりなおす</summary>
    /// <summary>なつやすみは 終わったか</summary>
    public bool Owatta { get { return day > LastDay; } }

    // ★**「はじめから」が ほんとうに はじまる ように する。**（2026-08-17・遊ぶ 人の 指摘）
    //   「Reset0 と BugBook.Clear が 消して いるのは 2つだけ。PlayHost の 11個の キーは
    //     1つも 消えて いません。……2周目は 8月1日に『えにっきは ぜんぶ 書きおわって いる』。
    //     ひみつきちは 完成済み。asobi1_* が のこって いるので、どの 遊びも
    //     二度と『はじめて』に ならない」
    //
    //   前に「エンディングで 全消去するな」と 言われた 反動で、今度は 何も 消えなく なって いた。
    //   **正解は その 中間。すすみ具合(1周ぶん)は 消す・きろく(さいこう記録)は のこす。**
    public void Reset0() {
        day = 1; Past.Clear(); today.Clear(); todaySeen.Clear(); counts.Clear(); talked.Clear();
        Enikki.Clear();
        PlayerPrefs.DeleteKey(KeyEnikki);
        PlayerPrefs.DeleteKey(KeyEnikkiDay);
        greeted = false; Save();
    }
}
