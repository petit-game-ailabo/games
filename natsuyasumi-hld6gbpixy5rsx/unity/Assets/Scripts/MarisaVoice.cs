// 魔理沙の 口。**「〜だぜ」で しゃべる。**（2026-08-17）
//
// ★遊ぶ 人からの 言：「いまの 魔理沙は、ドット絵が 魔理沙なだけの 無言の アバター。
//   喋らない、箒を 持たない、性格が 無い。これなら 普通の 少年主人公で いい。
//   魔理沙の 魅力は **うるさいくらい 前のめりで、なんでも 自分で やりたがって、
//   やたら 自信満々**な ところ。それが 一つも 出て いない」
//
// ★UIの「〜を つかまえた！」は 事務的すぎる。**同じ 出来事に 本人の 一言を そえる**。
//   ついでに 喜怒哀楽の ポーズも 出す（**未使用だった「怒」は ここで つかう**）。
using UnityEngine;

public class MarisaVoice : MonoBehaviour {

    public BugHud hud;
    public BugBook book;
    public CharSprite sprite;
    public Nikki nikki;

    // 同じ 一言が つづくと しらける。直前の を おぼえて よける
    int last = -1;

    static readonly string[] Caught = {
        "よし、いただきだぜ！",
        "へっ、おれさまに かかれば こんなもんだ",
        "ふふん、なかなか いい のが とれたな",
        "ようし、つぎ いくぜ",
    };
    static readonly string[] First = {
        "おっ、はじめて 見る やつだ！ ずかんに のせて おこう",
        "こいつは めずらしいぜ。とっておきだ",
    };
    static readonly string[] Record = {
        "でかい！ こいつは きろくものだぜ",
        "こんな 大きいの、はじめてだ！",
    };
    static readonly string[] Missed = {
        "ちっ、にげられた。つぎだ つぎ",
        "あー！ あと ちょっと だったのに",
        "おっかしいな、いまのは 入った はずだぜ",
    };

    void Start() {
        if (hud == null) hud = FindFirstObjectByType<BugHud>();
        if (book == null) book = GetComponent<BugBook>();
        if (sprite == null) sprite = GetComponent<CharSprite>();
        if (nikki == null) nikki = FindFirstObjectByType<Nikki>();
        if (book != null) book.OnCaught += OnCaught;
    }
    void OnDestroy() { if (book != null) book.OnCaught -= OnCaught; }

    void OnCaught(BugCatch c) {
        if (c.firstOfKind) { Say(First, CharSprite.Pose.Yorokobi); return; }
        if (c.record)      { Say(Record, CharSprite.Pose.Yorokobi); return; }
        Say(Caught, CharSprite.Pose.Tanoshii);
    }

    /// <summary>にげられた とき（BugCatcher から 呼ぶ）</summary>
    public void Missed_() { Say(Missed, CharSprite.Pose.Ikari); }

    /// <summary>その場に あわせた ひとこと（高台・雨など）</summary>
    public void Line(string s, CharSprite.Pose pose) {
        if (hud != null) hud.Say("まりさ「" + s + "」");
        if (sprite != null) sprite.ShowMood(pose, 1.6f);
    }

    void Say(string[] a, CharSprite.Pose pose) {
        if (a == null || a.Length == 0) return;
        int i = Random.Range(0, a.Length);
        if (a.Length > 1 && i == last) i = (i + 1) % a.Length;
        last = i;
        if (hud != null) hud.Say("まりさ「" + a[i] + "」");
        if (sprite != null) sprite.ShowMood(pose, 1.5f);
    }

    // ---------------------------------------------------------------- 場所の 一言
    // ★**同じ 場所では 1日 1回だけ。**毎回 しゃべると うるさい
    [HideInInspector] public Transform player;
    bool saidLookout, saidRiver;
    int saidDay = -1;

    void Update() {
        if (player == null) player = transform;
        int d = nikki != null ? nikki.day : 0;
        if (d != saidDay) { saidDay = d; saidLookout = false; saidRiver = false; }

        var p = player.position;
        if (!saidLookout && p.y > 3.2f) {
            saidLookout = true;
            Line("うわ、ぜんぶ 見えるぜ！ ここが いちばん 高いのか", CharSprite.Pose.Yorokobi);
            if (nikki != null) nikki.Note("lookout", "山の 上まで のぼった。谷が ぜんぶ 見えたぜ。");
        }
        if (!saidRiver && p.z > 26f) {
            saidRiver = true;
            Line("お、川だ。水きりでも して いくか", CharSprite.Pose.Tanoshii);
        }
    }
}
