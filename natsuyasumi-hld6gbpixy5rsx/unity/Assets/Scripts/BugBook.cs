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
    public const int CageSize = 6;          // かごに 入る 数

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
    public BugId? Release() {
        if (recent.Count == 0) return null;
        var id = recent[0];
        recent.RemoveAt(0);
        PlayerPrefs.SetInt(FreedKey, Freed + 1);
        Save();
        return id;
    }

    /// <summary>かごの いちばん 古い 1ぴきを 標本に する</summary>
    public BugId? MakeSpecimen() {
        if (recent.Count == 0) return null;
        var id = recent[0];
        recent.RemoveAt(0);
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

    public void Add(BugId id, int sizeMm) {
        int i = (int)id;
        bool first = counts[i] == 0;
        counts[i]++;
        bool rec = sizeMm > maxMm[i];
        if (rec) maxMm[i] = sizeMm;
        recent.Add(id);
        while (recent.Count > CageSize) recent.RemoveAt(0);
        Save();
        if (OnCaught != null)
            OnCaught(new BugCatch { id = id, count = counts[i], firstOfKind = first, sizeMm = sizeMm, record = rec });
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
        while (recent.Count > CageSize) recent.RemoveAt(0);
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
        recent.Clear();
        PlayerPrefs.SetInt(FreedKey, 0);
        Save();
    }
}
