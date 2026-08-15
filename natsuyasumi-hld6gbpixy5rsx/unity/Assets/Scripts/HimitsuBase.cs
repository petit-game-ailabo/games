using UnityEngine;

// ひみつきち。**来る たびに すこしずつ 建つ。**
//
// 数だけ 増えても うれしく ない。やぶの 中に 行くと、きのう 立てた かべが
// ちゃんと 立って いる——それが この 遊びの ぜんぶ。
// だから 5段ぶんの 木ぎれを **場面に 先に 建てて おいて、できた ぶんだけ 見せる**。
// （そのつど 作ると、遊びを 中断した ときに 消えて しまう）
public class HimitsuBase : MonoBehaviour {

    [System.Serializable]
    public class Stage { public Renderer[] parts; }

    public Stage[] stages;

    void Start() { Refresh(); }

    /// <summary>できあがった 段の ぶんだけ 見せる</summary>
    public void Refresh() {
        var host = FindFirstObjectByType<PlayHost>();
        int step = host != null ? host.BaseStep : 0;
        Show(step);
    }

    public void Show(int step) {
        if (stages == null) return;
        for (int i = 0; i < stages.Length; i++) {
            if (stages[i] == null || stages[i].parts == null) continue;
            bool on = i < step;
            foreach (var r in stages[i].parts) if (r != null) r.enabled = on;
        }
    }
}
