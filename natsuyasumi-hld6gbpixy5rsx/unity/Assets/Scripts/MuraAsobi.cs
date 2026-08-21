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
    public bool kieru = false;        // つかまえたら その日は いなくなる（あした 戻る）
    // 2段階もの（スイカ冷やし 等）：はじめに dekigoto → matsu 時間 まって dekigoto2。
    // 待ちの あいだに 押すと mada
    public string dekigoto2 = "", mada = "";
    public float matsu = 0f;
    [HideInInspector] public float readyHour = -1f;

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
            toastT = 2.6f;
            if (n.matsu > 0f) {                       // 2段階もの
                if (n.readyHour < 0f) { toast = n.dekigoto; n.readyHour = MuraDay.Hour + n.matsu; }
                else if (MuraDay.Hour < n.readyHour) { toast = n.mada; }
                else { toast = n.dekigoto2; n.readyHour = -1f; kazu++; }
            } else {
                toast = n.dekigoto; kazu++;
                if (n.kieru) { n.gameObject.SetActive(false); MuraDay.Ashita.Add(n.gameObject); }
            }
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
