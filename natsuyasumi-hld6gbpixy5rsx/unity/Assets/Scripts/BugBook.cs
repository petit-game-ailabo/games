using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>1ぴき つかまえた ときの ことがら</summary>
public struct BugCatch {
    public BugId id;
    public int count;        // その虫の 通算
    public bool firstOfKind; // はじめて の 1ぴき か
    public int sizeMm;       // この 1ぴきの 大きさ
    public bool record;      // さいだい きろくを 更新したか
}

// むしとり の 記録（ずかん）。
// 「何を 何びき 取ったか」「いちばん 大きかったのは 何mm か」「かごに 何が 入っているか」。
// **数だけ 数えても つづける 理由に ならない**ので、大きさの きろくを 長い 目標に する。
public class BugBook : MonoBehaviour {
    const string Key = "natsuyasumi.bugbook.v1";
    const string MaxKey = "natsuyasumi.bugmax.v1";
    const string RecentKey = "natsuyasumi.bugcage.v1";
    const string SpecKey = "natsuyasumi.bugspec.v1";
    const string FreedKey = "natsuyasumi.bugfreed.v1";
    // ★**かごに 上限と 寿命を 入れる。**（2026-08-17・遊ぶ 人の 指摘）
    //   「逃がす＝何も 残らない／標本＝数字が 残る。**一方が 完全に 上位互換**です。
    //     かごに 上限も ない、虫は 弱りも しない。だから 常に 標本が 正解で、
    //     プレイヤーは 一度も 迷いません。……いまの 虫とりは
    //     『振る→捕れる→数字が 増える』で、最初から 最後まで
    //     **一度も 迷う ところが ありません**」
    //
    //   上限を 5に して、6ぴきめで「どれを 手ばなす?」が 始まる。
    //   3日 かかえると 弱り、4日で 逃げて いく＝**かかえこむ ことに 代金が つく**。
    public const int CageSize = 5;          // かごに 入る 数（駄菓子屋の 大かごで ふえる）
    const string CageMoreKey = "natsuyasumi.bugcagemore.v1";
    public int CageMax { get { return CageSize + PlayerPrefs.GetInt(CageMoreKey, 0); } }
    public bool CageFull { get { return recent.Count >= CageMax; } }
    /// <summary>大きい かごを 買った（上限が ふえる）</summary>
    public void CageUp(int by) {
        PlayerPrefs.SetInt(CageMoreKey, PlayerPrefs.GetInt(CageMoreKey, 0) + by);
        PlayerPrefs.Save();
    }

    // かごに 入れた 日（recent と 同じ ならび）。**弱る までの 数え**
    const string CageDayKey = "natsuyasumi.bugcageday.v1";
    readonly List<int> putDay = new List<int>();
    /// <summary>いまの 日づけ（Nikki が 入れる）。0なら 弱りを 見ない</summary>
    [HideInInspector] public int today;
    public const int YowaruDay = 3;         // これだけ かかえると 弱る
    public const int NigeruDay = 4;         // ここまで 来ると 逃げる

    /// <summary>かごの i ばんめは 何日 かかえて いるか</summary>
    public int Azukari(int i) {
        if (today <= 0 || i < 0 || i >= putDay.Count || putDay[i] <= 0) return 0;
        return Mathf.Max(0, today - putDay[i]);
    }
    public bool Yowatta(int i) { return Azukari(i) >= YowaruDay; }

    // ★**とった 虫を 人に 見せる。**（2026-08-17）
    //   遊ぶ 人：「オニヤンマを 苦労して 捕っても、村の 誰も 見て くれない。
    //   ぼくなつで いちばん うれしかったのは、虫かごを 見せた ときの あの 反応」
    //   だれに 何を 見せたかを おぼえて、**同じ 虫で 二度 おどろかせない**
    const string MiseKey = "natsuyasumi.bugmise.";
    public bool Mita(string who, BugId id) {
        return (PlayerPrefs.GetInt(MiseKey + who, 0) & (1 << (int)id)) != 0;
    }
    // 見せた 相手の 名。**はじめから の とき、名を 知らないと 消せない**
    static readonly List<string> MiseAite = new List<string>();
    public void Miseta(string who, BugId id) {
        if (!MiseAite.Contains(who)) MiseAite.Add(who);
        int m = PlayerPrefs.GetInt(MiseKey + who, 0);
        PlayerPrefs.SetInt(MiseKey + who, m | (1 << (int)id));
        PlayerPrefs.Save();
    }

    int[] counts = new int[BugKind.All.Length];
    int[] maxMm  = new int[BugKind.All.Length];
    // ★**標本に した ぶん。** かごの 虫は 逃がすか 標本に するか の どちらか。
    //   標本は のこるが 二どと 動かない。逃がすと 消えるが 数だけ のこる。
    //   どちらを えらぶかに 迷いが 出る のが この 遊びの ねらい
    int[] specimens = new int[BugKind.All.Length];
    // かごの 中身（入れた 順）。**数だけ 覚えても かごが 空に なる**ので、
    // 何が 入っているかも 別に 持つ
    readonly List<BugId> recent = new List<BugId>();

    public event Action<BugCatch> OnCaught;
    /// <summary>にがした（そばに いる 人が 反応する）</summary>
    public event Action<BugId> OnFreed;

    void Awake() { Load(); }

    public int Count(BugId id) { return counts[(int)id]; }
    public int MaxMm(BugId id) { return maxMm[(int)id]; }
    public int Specimen(BugId id) { return specimens[(int)id]; }
    public int SpecimenTotal { get { int s = 0; foreach (var c in specimens) s += c; return s; } }
    public int SpecimenKinds { get { int s = 0; foreach (var c in specimens) if (c > 0) s++; return s; } }
    public int Freed { get { return PlayerPrefs.GetInt(FreedKey, 0); } }

    // ★**教わった ことは 持ち歩ける。**（2026-08-17）
    //   村の 人から 聞いた「どこに いるか」を 1ビットずつ ためて、ずかんに 出す。
    //   これが あると **話しかける ことが 攻略に つながる**
    const string HintKey = "natsuyasumi.bughint.v1";
    public bool HasHint(BugId id) { return (PlayerPrefs.GetInt(HintKey, 0) & (1 << (int)id)) != 0; }
    public void AddHint(BugId id) {
        int m = PlayerPrefs.GetInt(HintKey, 0);
        if ((m & (1 << (int)id)) != 0) return;
        PlayerPrefs.SetInt(HintKey, m | (1 << (int)id));
        PlayerPrefs.Save();
    }
    /// <summary>まだ 取って いなくて、まだ 聞いても いない 虫（無ければ null）</summary>
    public BugId? Shiranai() {
        foreach (var k in BugKind.All)
            if (Count(k.id) == 0 && !HasHint(k.id)) return k.id;
        return null;
    }

    /// <summary>かごの いちばん 古い 1ぴきを 逃がす。逃がした 虫（無ければ null）</summary>
    public BugId? Release() { return Release(0); }

    /// <summary>かごの i ばんめを にがす</summary>
    public BugId? Release(int i) {
        if (i < 0 || i >= recent.Count) return null;
        var id = recent[i];
        recent.RemoveAt(i);
        if (i < putDay.Count) putDay.RemoveAt(i);
        PlayerPrefs.SetInt(FreedKey, Freed + 1);
        Save();
        if (OnFreed != null) OnFreed(id);
        return id;
    }

    /// <summary>かごの いちばん 古い 1ぴきを 標本に する</summary>
    public BugId? MakeSpecimen() {
        if (recent.Count == 0) return null;
        var id = recent[0];
        recent.RemoveAt(0);
        if (putDay.Count > 0) putDay.RemoveAt(0);
        specimens[(int)id]++;
        Save();
        return id;
    }
    public int Total { get { int s = 0; foreach (var c in counts) s += c; return s; } }
    public int Kinds { get { int s = 0; foreach (var c in counts) if (c > 0) s++; return s; } }
    public IList<BugId> Recent { get { return recent; } }

    /// <summary>1ぴきの 大きさを 決める（0.7〜1.3倍。たまに 大物）</summary>
    public static int RollSize(BugKind k) {
        float t = UnityEngine.Random.value;
        // ふつうは まんなか あたり。**たまに 飛びぬけて 大きいのが 出る**ように 端を のばす
        float f = 0.72f + t * t * 0.62f;
        if (UnityEngine.Random.value < 0.06f) f += 0.14f;      // 大物
        return Mathf.Max(1, Mathf.RoundToInt(k.sizeMm * f));
    }

    /// <summary>1ぴき 入れる。**かごが いっぱいなら 入らない**（false）</summary>
    public bool Add(BugId id, int sizeMm) {
        // ★前は いっぱいでも だまって いちばん 古いのを 捨てて いた。
        //   それだと「どれを 手ばなすか」を 人が えらべない
        if (CageFull) return false;
        int i = (int)id;
        bool first = counts[i] == 0;
        counts[i]++;
        bool rec = sizeMm > maxMm[i];
        if (rec) maxMm[i] = sizeMm;
        recent.Add(id);
        putDay.Add(today);
        Save();
        if (OnCaught != null)
            OnCaught(new BugCatch { id = id, count = counts[i], firstOfKind = first, sizeMm = sizeMm, record = rec });
        return true;
    }

    // ★**朝に かごを 見る。**3日で 弱り、4日で 逃げる。
    //   かかえこむ ほど 減る＝「いま 標本に するか、にがすか」を 毎朝 つきつける
    /// <summary>朝の 手入れ。逃げて いった 虫の 名（無ければ null）</summary>
    public List<BugId> Asa(int day) {
        today = day;
        var nigeta = new List<BugId>();
        for (int i = recent.Count - 1; i >= 0; i--) {
            if (Azukari(i) < NigeruDay) continue;
            nigeta.Add(recent[i]);
            recent.RemoveAt(i);
            if (i < putDay.Count) putDay.RemoveAt(i);
            PlayerPrefs.SetInt(FreedKey, Freed + 1);
        }
        if (nigeta.Count > 0) Save();
        return nigeta;
    }

    void Load() {
        ReadInts(Key, counts);
        ReadInts(MaxKey, maxMm);
        ReadInts(SpecKey, specimens);
        ReadInts(KyonenKey, kyonen);
        var r = PlayerPrefs.GetString(RecentKey, "");
        if (string.IsNullOrEmpty(r)) return;
        foreach (var t in r.Split(',')) {
            int v;
            if (int.TryParse(t, out v) && v >= 0 && v < BugKind.All.Length) recent.Add((BugId)v);
        }
        while (recent.Count > CageMax) recent.RemoveAt(0);
        var dd = PlayerPrefs.GetString(CageDayKey, "");
        if (!string.IsNullOrEmpty(dd))
            foreach (var t in dd.Split(',')) { int v; putDay.Add(int.TryParse(t, out v) ? v : 0); }
        while (putDay.Count < recent.Count) putDay.Add(0);
        while (putDay.Count > recent.Count) putDay.RemoveAt(putDay.Count - 1);
    }

    static void ReadInts(string key, int[] into) {
        var s = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(s)) return;
        var p = s.Split(',');
        for (int i = 0; i < into.Length && i < p.Length; i++) {
            int v; into[i] = int.TryParse(p[i], out v) ? v : 0;
        }
    }

    void Save() {
        PlayerPrefs.SetString(Key, string.Join(",", Array.ConvertAll(counts, c => c.ToString())));
        PlayerPrefs.SetString(MaxKey, string.Join(",", Array.ConvertAll(maxMm, c => c.ToString())));
        PlayerPrefs.SetString(SpecKey, string.Join(",", Array.ConvertAll(specimens, c => c.ToString())));
        PlayerPrefs.SetString(RecentKey, string.Join(",", recent.ConvertAll(id => ((int)id).ToString()).ToArray()));
        PlayerPrefs.SetString(CageDayKey, string.Join(",", putDay.ConvertAll(d => d.ToString()).ToArray()));
        PlayerPrefs.Save();
    }

    // ★**きょ年の さいだい は のこす。**（2026-08-17・遊ぶ 人の 指摘
    //   「すすみ具合(1周ぶん)は 消す・きろく(さいこう記録)は のこす」）
    //   2周目に「きょ年は 82mm だった」が 出る ので、ずかんが 目標に なる
    const string KyonenKey = "natsuyasumi.bugkyonen.v1";
    int[] kyonen = new int[BugKind.All.Length];
    public int Kyonen(BugId id) { return kyonen[(int)id]; }

    [ContextMenu("記録を まっさらに する")]
    public void Clear() {
        PlayerPrefs.DeleteKey(HintKey);
        foreach (var w in MiseAite) PlayerPrefs.DeleteKey(MiseKey + w);
        // いまの さいだいを 「きょ年」へ 送って から 消す
        for (int i = 0; i < counts.Length; i++) {
            if (maxMm[i] > kyonen[i]) kyonen[i] = maxMm[i];
            counts[i] = 0; maxMm[i] = 0; specimens[i] = 0;
        }
        PlayerPrefs.SetString(KyonenKey, string.Join(",", Array.ConvertAll(kyonen, c => c.ToString())));
        recent.Clear(); putDay.Clear();
        PlayerPrefs.DeleteKey(CageMoreKey);
        PlayerPrefs.SetInt(FreedKey, 0);
        Save();
    }
}
