using UnityEngine;

// 田舎の 遊びが できる ところ の 目じるし。
//
// 川べり・小川ばた・野はら など、**その場所でしか できない こと**を ここに 置く。
// 遊びの 中身は PlayHost が やる。ここは 「どこで 何が できるか」だけ を もつ。
//
// 水の 高さや 流れの むきは **場面を 組む ときに 入れて おく**。
// 走って いる あいだに 水面を さがすのは むだだし、川の 面には 当たりが ない
public enum PlayKind {
    Sasabune,     // ささぶね を ながす（小川）
    Mizukiri,     // 水きり（大きい 川）
    Tsuri,        // つり（大きい 川）
    Hanatsumi,    // 花を つむ（野はら）
    Irozu,        // 色水を 作る（井戸ばた）
    Oshibana,     // おし花に する（じぶんの 部屋）
    Himitsu,      // ひみつ きち を 作る（やぶの 中）
}

public class PlaySpot : MonoBehaviour {
    public PlayKind kind;
    [Tooltip("ここまで 近づいたら 遊べる")]
    public float range = 2.4f;

    [Header("水べ の とき")]
    public Vector3 water;        // 水面の 点
    public Vector3 flow = Vector3.forward;   // 流れの むき（ささぶねが 下る ほう）
    [Tooltip("岸から 見て 川の むこう岸まで の 長さ。水きりが とぶ 先")]
    public float span = 8f;

    public bool Near(Transform who) {
        if (who == null) return false;
        var d = who.position - transform.position;
        d.y *= 0.5f;                              // 高さの ちがいは ゆるく 見る
        return d.sqrMagnitude < range * range;
    }

    /// <summary>近づいた ときに 足もとに 出す ひとこと</summary>
    public string Prompt {
        get {
            switch (kind) {
                case PlayKind.Sasabune:  return "スペース：ささぶねを ながす";
                case PlayKind.Mizukiri:  return "スペース：水きりを する";
                case PlayKind.Tsuri:     return "スペース：つりを する";
                case PlayKind.Hanatsumi: return "スペース：花を つむ";
                case PlayKind.Irozu:     return "スペース：色水を 作る";
                case PlayKind.Oshibana:  return "スペース：おし花に する";
                default:                 return "スペース：ひみつきちを 作る";
            }
        }
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
