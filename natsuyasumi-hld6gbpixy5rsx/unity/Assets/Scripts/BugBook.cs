using System;
using UnityEngine;

// むしとり の 記録（ずかん）。
// 「何を 何びき 取ったか」と「はじめて 取った か」を 持つ。
// はじめての 1ぴきは 演出を 変えたいので、そこだけ 外に 知らせる。
public class BugBook : MonoBehaviour {
    const string Key = "natsuyasumi.bugbook.v1";
    const string RecentKey = "natsuyasumi.bugcage.v1";
    public const int CageSize = 6;          // かごに 入る 数

    int[] counts = new int[BugKind.All.Length];
    // かごの 中身（新しい 順では なく 入れた 順）。**数だけ 覚えても かごが 空に なる**ので、
    // 何が 入っているかも 別に 持つ
    readonly System.Collections.Generic.List<BugId> recent = new System.Collections.Generic.List<BugId>();

    /// <summary>取った とき。(種類, その虫の 通算, はじめてか)</summary>
    public event Action<BugId, int, bool> OnCaught;

    void Awake() { Load(); }

    public int Count(BugId id) { return counts[(int)id]; }
    public int Total { get { int s = 0; foreach (var c in counts) s += c; return s; } }
    public int Kinds { get { int s = 0; foreach (var c in counts) if (c > 0) s++; return s; } }

    public void Add(BugId id) {
        bool first = counts[(int)id] == 0;
        counts[(int)id]++;
        recent.Add(id);
        while (recent.Count > CageSize) recent.RemoveAt(0);
        Save();
        if (OnCaught != null) OnCaught(id, counts[(int)id], first);
    }

    /// <summary>かごの 中身（入れた 順）</summary>
    public System.Collections.Generic.IList<BugId> Recent { get { return recent; } }

    void Load() {
        var s = PlayerPrefs.GetString(Key, "");
        if (!string.IsNullOrEmpty(s)) {
            var p = s.Split(',');
            for (int i = 0; i < counts.Length && i < p.Length; i++) {
                int v; counts[i] = int.TryParse(p[i], out v) ? v : 0;
            }
        }
        var r = PlayerPrefs.GetString(RecentKey, "");
        if (string.IsNullOrEmpty(r)) return;
        foreach (var t in r.Split(',')) {
            int v;
            if (int.TryParse(t, out v) && v >= 0 && v < BugKind.All.Length) recent.Add((BugId)v);
        }
        while (recent.Count > CageSize) recent.RemoveAt(0);
    }

    void Save() {
        PlayerPrefs.SetString(Key, string.Join(",", Array.ConvertAll(counts, c => c.ToString())));
        PlayerPrefs.SetString(RecentKey, string.Join(",", recent.ConvertAll(id => ((int)id).ToString()).ToArray()));
        PlayerPrefs.Save();
    }

    [ContextMenu("記録を まっさらに する")]
    public void Clear() {
        for (int i = 0; i < counts.Length; i++) counts[i] = 0;
        recent.Clear();
        Save();
    }
}
