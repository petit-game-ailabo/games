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
    public const int CageSize = 6;          // かごに 入る 数

    int[] counts = new int[BugKind.All.Length];
    int[] maxMm  = new int[BugKind.All.Length];
    // かごの 中身（入れた 順）。**数だけ 覚えても かごが 空に なる**ので、
    // 何が 入っているかも 別に 持つ
    readonly List<BugId> recent = new List<BugId>();

    public event Action<BugCatch> OnCaught;

    void Awake() { Load(); }

    public int Count(BugId id) { return counts[(int)id]; }
    public int MaxMm(BugId id) { return maxMm[(int)id]; }
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
        PlayerPrefs.SetString(RecentKey, string.Join(",", recent.ConvertAll(id => ((int)id).ToString()).ToArray()));
        PlayerPrefs.Save();
    }

    [ContextMenu("記録を まっさらに する")]
    public void Clear() {
        for (int i = 0; i < counts.Length; i++) { counts[i] = 0; maxMm[i] = 0; }
        recent.Clear();
        Save();
    }
}
