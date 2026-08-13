using System;
using UnityEngine;

// むしとり の 記録（ずかん）。
// 「何を 何びき 取ったか」と「はじめて 取った か」を 持つ。
// はじめての 1ぴきは 演出を 変えたいので、そこだけ 外に 知らせる。
public class BugBook : MonoBehaviour {
    const string Key = "natsuyasumi.bugbook.v1";

    int[] counts = new int[BugKind.All.Length];

    /// <summary>取った とき。(種類, その虫の 通算, はじめてか)</summary>
    public event Action<BugId, int, bool> OnCaught;

    void Awake() { Load(); }

    public int Count(BugId id) { return counts[(int)id]; }
    public int Total { get { int s = 0; foreach (var c in counts) s += c; return s; } }
    public int Kinds { get { int s = 0; foreach (var c in counts) if (c > 0) s++; return s; } }

    public void Add(BugId id) {
        bool first = counts[(int)id] == 0;
        counts[(int)id]++;
        Save();
        if (OnCaught != null) OnCaught(id, counts[(int)id], first);
    }

    void Load() {
        var s = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(s)) return;
        var p = s.Split(',');
        for (int i = 0; i < counts.Length && i < p.Length; i++) {
            int v; counts[i] = int.TryParse(p[i], out v) ? v : 0;
        }
    }

    void Save() {
        PlayerPrefs.SetString(Key, string.Join(",", Array.ConvertAll(counts, c => c.ToString())));
        PlayerPrefs.Save();
    }

    [ContextMenu("記録を まっさらに する")]
    public void Clear() {
        for (int i = 0; i < counts.Length; i++) counts[i] = 0;
        Save();
    }
}
