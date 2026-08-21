using UnityEngine;
using System.Collections.Generic;

// 縦切り用の「あそびスポット」の器（R4の 本実装までの 置きもの）。
// そばに 立つと 下に「スペース：◯◯」と 出て、押すと できごとが 1行 出る。
// 昼/夜の 出しわけ つき（EVENTS の 採用ぶんを 置くのに 使う）。
public class MuraAsobi : MonoBehaviour {
    public string namae = "しらべる";
    public string dekigoto = "なにかが いた！";
    public float chikasa = 1.9f;
    public int hiruYoru = 0;          // 0=いつも 1=昼だけ 2=夜だけ
    public bool kieru = false;        // つかまえたら いなくなる（虫）

    public static readonly List<MuraAsobi> All = new List<MuraAsobi>();
    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    public bool Dekiru {
        get {
            if (hiruYoru == 1) return !MuraDay.Night;
            if (hiruYoru == 2) return MuraDay.Night;
            return true;
        }
    }
}

/// <summary>主人公に つける がわ。近くの スポットの 案内と、押した ときの できごと 表示</summary>
public class MuraAsobiTe : MonoBehaviour {
    public Font font;
    string toast; float toastT;
    int kazu;

    MuraAsobi Nearest() {
        MuraAsobi best = null; float bd = float.MaxValue;
        foreach (var a in MuraAsobi.All) {
            if (a == null || !a.Dekiru) continue;
            float d = Vector3.Distance(transform.position, a.transform.position);
            if (d < a.chikasa && d < bd) { bd = d; best = a; }
        }
        return best;
    }

    void Update() {
        if (toastT > 0f) toastT -= Time.deltaTime;
        var n = Nearest();
        if (n != null && Input.GetKeyDown(KeyCode.Space)) {
            toast = n.dekigoto; toastT = 2.6f; kazu++;
            if (n.kieru) Destroy(n.gameObject);
        }
    }

    void OnGUI() {
        if (font != null) GUI.skin.font = font;
        var n = Nearest();
        if (n != null)
            GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height - 64, 400, 26),
                      "スペース：" + n.namae);
        if (toastT > 0f)
            GUI.Label(new Rect(Screen.width / 2 - 220, Screen.height / 2 - 90, 440, 26),
                      "『" + toast + "』");
        GUI.Label(new Rect(Screen.width - 250, 34, 240, 24), "あそんだ かず " + kazu);
    }
}
