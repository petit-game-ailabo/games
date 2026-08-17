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
    // ★2026-08-17 に 足した ぶん（遊ぶ 人の 指摘）
    Shukudai,     // **絵日記（宿題）**。夜、机で 1ページ。31ページ ある
    Kingyo,       // 金魚すくい（祭りの 屋台。10日だけ）
    Hanabi,       // 線香花火（夜の 縁側）
    Dagashi,      // 駄菓子屋（谷の おくの 町）。大きい 虫かご・ラムネ・アイス
    // ★祭りの 屋台（2026-08-17）。**もらった 小づかいを その夜の うちに つかい切る**
    Shateki,      // 射的
    Kuji,         // くじ引き
    Bonodori,     // 盆おどりの わに 入る
    // ★**後半 25日ぶんが まだ 平ら だった。**（2026-08-17・2度目の 集計）
    //   配っただけでは 1週目しか 埋まらない。**後半にも 開く ものが 要る**
    Toronagashi,  // とうろうを ながす（15日の 夜・川）
    Hoshi,        // 星を 見る（16日から・高台の 夜）
    Taikai,       // **村の 虫ずもう 大会**（8月27日）。育てた 相棒の 行き先
    Wakare,       // 相棒を にがす（30日から・川べり）
}

public class PlaySpot : MonoBehaviour {
    public PlayKind kind;
    [Tooltip("ここまで 近づいたら 遊べる")]
    public float range = 2.4f;

    // ★**いつ でも できる 遊びばかりだと、夜が 空き地に なる。**（遊ぶ 人の 指摘
    //   「夜に できるのは ホタルと カブトを 捕る ことと、寝る ことだけ。
    //     18時から 2時まで 8時間ぶんの 遊びが 3つしか ない」）
    [Tooltip("夜だけ できる")]
    public bool onlyNight;
    [Tooltip("この 日だけ できる（0で いつでも）。祭りの 屋台など")]
    public int onlyDay;
    [Tooltip("あいて いる 時こく（shimeru が 0 なら いつでも）。駄菓子屋の 店じまい")]
    public float akeru, shimeru;
    [Tooltip("この 日から できる（0で いつでも）")]
    public int fromDay;
    [Tooltip("この 日まで できる（0で いつまでも）")]
    public int toDay;

    [Header("水べ の とき")]
    public Vector3 water;        // 水面の 点
    public Vector3 flow = Vector3.forward;   // 流れの むき（ささぶねが 下る ほう）
    [Tooltip("岸から 見て 川の むこう岸まで の 長さ。水きりが とぶ 先")]
    public float span = 8f;

    [HideInInspector] public TimeOfDay tod;
    [HideInInspector] public Nikki nikki;

    /// <summary>いま できる 遊びか（時こく・日づけ）</summary>
    public bool Ima {
        get {
            if (onlyNight) {
                if (tod == null) return false;
                if (!(tod.hour >= 18.5f || tod.hour < 4.5f)) return false;
            }
            if (onlyDay > 0) {
                if (nikki == null || nikki.day != onlyDay) return false;
            }
            if (fromDay > 0 && (nikki == null || nikki.day < fromDay)) return false;
            if (toDay > 0 && (nikki == null || nikki.day > toDay)) return false;
            if (shimeru > 0f) {
                if (tod == null) return false;
                if (tod.hour < akeru || tod.hour >= shimeru) return false;
            }
            return true;
        }
    }

    public bool Near(Transform who) {
        if (!Ima) return false;
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
                case PlayKind.Shukudai:  return "スペース：えにっきを 書く";
                case PlayKind.Kingyo:    return "スペース：金魚すくいを する";
                case PlayKind.Hanabi:    return "スペース：線こう花火を する";
                case PlayKind.Dagashi:   return "スペース：駄がし屋を のぞく";
                case PlayKind.Shateki:   return "スペース：射的を する（20円）";
                case PlayKind.Kuji:      return "スペース：くじを 引く（30円）";
                case PlayKind.Bonodori:  return "スペース：おどりの わに 入る";
                case PlayKind.Toronagashi: return "スペース：とうろうを ながす";
                case PlayKind.Hoshi:     return "スペース：星を 見る";
                case PlayKind.Taikai:    return "スペース：虫ずもう 大会に 出る";
                case PlayKind.Wakare:    return "スペース：あいぼうを にがす";
                default:                 return "スペース：ひみつきちを 作る";
            }
        }
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
