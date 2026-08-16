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
    readonly List<string> today = new List<string>();
    readonly HashSet<string> todaySeen = new HashSet<string>();
    /// <summary>すぎた 日の 日記。あとから 読み返せる</summary>
    public readonly List<string> Past = new List<string>();

    public System.Action<int> OnNewDay;         // 日が 変わった
    public System.Action<string> OnDiary;       // 日記が できた

    const string KeyDay = "natsu_day";
    const string KeyPast = "natsu_nikki";

    void Awake() { Load(); }

    /// <summary>きょうの 出来事を ためる。同じ ことは 1日 1回だけ</summary>
    public void Note(string key, string text) {
        if (string.IsNullOrEmpty(text)) return;
        if (!todaySeen.Add(key)) return;        // もう ある
        today.Add(text);
    }

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

    /// <summary>寝る ときに 日記を 組み立てる。**型に はめこむ**</summary>
    public string Compose() {
        var sb = new System.Text.StringBuilder();
        sb.AppendFormat("― 8月{0}日 ―\n\n", day);

        int bugs = CountOf("bug");
        if (bugs >= 8)      sb.AppendLine("きょうは むしとりに 明けくれたぜ。");
        else if (bugs >= 3) sb.AppendLine("むしを " + bugs + "ひき つかまえた。まあまあだな。");
        else if (bugs == 0) sb.AppendLine("きょうは 1ぴきも とれなかった。まあ そんな 日も あるさ。");

        foreach (var t in today) sb.AppendLine(t);

        if (today.Count == 0 && bugs == 0)
            sb.AppendLine("とくに 何も しなかった。ぼーっと して いたら 日が くれた。");

        sb.AppendLine();
        int left = LastDay - day;
        if (left <= 0)      sb.AppendLine("あしたには 帰らないと いけない。");
        else if (left <= 3) sb.AppendLine("なつやすみも あと " + left + "日。みじかいぜ。");
        else                sb.AppendLine("あしたは どこへ 行こうかな。");
        return sb.ToString();
    }

    /// <summary>寝る。日記を 出して つぎの 日へ</summary>
    public string Sleep() {
        string diary = Compose();
        Past.Add(diary);
        if (Past.Count > 40) Past.RemoveAt(0);
        day = Mathf.Min(LastDay, day + 1);
        today.Clear(); todaySeen.Clear(); counts.Clear();
        greeted = false;
        Save();
        if (OnDiary != null) OnDiary(diary);
        if (OnNewDay != null) OnNewDay(day);
        return diary;
    }

    public void Save() {
        PlayerPrefs.SetInt(KeyDay, day);
        // 日記は 最後の 10日ぶんだけ のこす（長くなりすぎない ように）
        int keep = Mathf.Min(10, Past.Count);
        var sb = new System.Text.StringBuilder();
        for (int i = Past.Count - keep; i < Past.Count; i++) sb.Append(Past[i]).Append("␞");
        PlayerPrefs.SetString(KeyPast, sb.ToString());
        PlayerPrefs.Save();
    }

    void Load() {
        day = Mathf.Clamp(PlayerPrefs.GetInt(KeyDay, 1), 1, LastDay);
        Past.Clear();
        string s = PlayerPrefs.GetString(KeyPast, "");
        if (!string.IsNullOrEmpty(s))
            foreach (var t in s.Split('␞'))
                if (!string.IsNullOrEmpty(t)) Past.Add(t);
    }

    /// <summary>はじめから やりなおす</summary>
    public void Reset0() {
        day = 1; Past.Clear(); today.Clear(); todaySeen.Clear(); counts.Clear();
        greeted = false; Save();
    }
}
