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

    void Awake() { Load(); }

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
        sb.AppendFormat("書いた 日記　{0} 日ぶん\n\n", Past.Count);
        sb.AppendLine("ひと月、あっという間だったぜ。");
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
    }

    // ================= カレンダー =================
    // ★遊ぶ 人からの 言：「8月2日と 8月28日で、起きる ことが 何ひとつ 違わない。
    //   のこり28日は **減って いく だけの 数字**。減った 先に 何も 無い。
    //   ぼくなつで 8月15日が 特別だったのは、**カレンダーの 上に 事件が 置いて あった**から」
    //
    // ★**予告が あることが 肝心。**「あさって 祭りだ」と 聞いた 瞬間に、
    //   プレイヤーは あした 寝る 理由が できる。
    public enum Koto2 { Nashi, Niji, MatsuriYokoku, Matsuri, Toro, Taifu, Shukudai, Owakare }

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
        }
        return Koto2.Nashi;
    }

    /// <summary>朝に 出す できごとの 知らせ</summary>
    public string TodayNews() {
        switch (Today()) {
            case Koto2.Niji:          return "ゆうべの 雨の あとだ。空が やけに きれいだぜ";
            case Koto2.MatsuriYokoku: return "なんだか 村が そわそわ して いるな";
            case Koto2.Matsuri:       return "きょうは 祭りだ！ 夜が たのしみだぜ";
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

    public void Reset0() {
        day = 1; Past.Clear(); today.Clear(); todaySeen.Clear(); counts.Clear(); talked.Clear();
        greeted = false; Save();
    }
}
