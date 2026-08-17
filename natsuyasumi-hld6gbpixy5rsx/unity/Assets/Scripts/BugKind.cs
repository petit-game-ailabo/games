using UnityEngine;

// 虫の 種類と、その ふるまいの ちがいを 1か所に まとめる。
// **表と ふるまいを 分けない。** 虫を 足すときは ここに 1行 足せば、
// 出かた・逃げかた・図かんの ならびまで ぜんぶ ついてくる。
public enum BugId {
    Semi = 0, Kabuto = 1, Kuwagata = 2, Tonbo = 3,
    Oniyanma = 4, Chou = 5, Batta = 6, Hotaru = 7,
}

// ★**どこの 土地に 出るか。**（2026-08-17）
//   遊ぶ 人からの 言：「屋敷の 前庭で 捕れる 虫と、山の 上で 捕れる 虫と、
//   川べりで 捕れる 虫が 全部 同じ。これだと 探検する 理由が ない。
//   ぼくなつで 山の 上まで 登ったのは、そこにしか いない 虫が いたから」
//   → 土地ごとに 顔ぶれを 変える。**そこでしか 出ない 虫**を 作る
public enum BugBa {
    Doko,      // どこでも
    Kawa,      // 川べり（トンボ・ホタル）
    Zoki,      // 雑木林・木立ち（カブト・クワガタ・セミ）
    Nohara,    // 野はら（チョウ・バッタ）
    Yama,      // 山の 上（**ここでしか 出ない**）
}

// どこに いるか
public enum BugPerch {
    Trunk,      // 木の みきに とまる（セミ・カブト・クワガタ）
    Air,        // 野原の 上を とぶ（トンボ・チョウ）
    Grass,      // 草の あいだ（バッタ）
    Bush,       // しげみの まわりを ただよう（ホタル）
}

[System.Serializable]
public class BugKind {
    public BugId id;
    public string name;          // ひらがな。図かんに 出す
    public BugPerch perch;
    public float height;         // 世界での 大きさ(m)
    public float wary;           // 用心ぶかさ 0〜1。高いほど 逃げやすい
    public float catchRate;      // つかまえられる 見こみ 0〜1
    public bool[] tod;           // あさ/ひる/ゆうがた/よる に 出るか
    public int weight;           // 出やすさ
    public bool glows;           // 自分で 光るか（ホタル）
    // むしずもうの 強さ。**取りにくい 虫が 強い**とは かぎらない ように する。
    // カブト・クワガタは 取りやすいが いちばん 強い＝「朝はやく 起きた ごほうび」に なる
    public int power = 3;
    // ふつうの 大きさ(mm)。1ぴきごとに この 0.7〜1.3倍で ばらつく。
    // **大きい 個体は 見た目も 大きく、ずもうでも 強い**＝さがす 理由が 1本に つながる
    public int sizeMm = 50;
    // 出る 土地。Doko なら どこでも
    public BugBa ba = BugBa.Doko;
    // ★**おじさんが 教えて くれる「どこに いるか」。**（2026-08-17）
    //   遊ぶ 人：「おじさんが『オニヤンマは 山の 上に いる』と 教えて くれても、
    //   その 言葉は 会話が 終われば 消える。**ずかんに 残らない**。
    //   聞いた ことを 持ち歩けないと、教わった 意味が ない」
    //   → 聞いた ヒントは BugBook に 記録し、ずかんの「？？？？？」の 横に 出す
    public string hint = "";

    // 1コマ 16x16 の 4列 x 2行。index が そのまま コマの ばんごう
    public const int Cols = 4, Rows = 2;

    // ★虫を 足すなら ここに 1行。あとは ぜんぶ ついてくる
    public static readonly BugKind[] All = {
        new BugKind { id = BugId.Semi, hint = "ぞうきばやしの みき。ひるま", sizeMm = 60,     name = "あぶらぜみ",   perch = BugPerch.Trunk, height = 0.30f,
                      wary = 0.55f, catchRate = 0.70f, weight = 30, power = 3, ba = BugBa.Zoki,   tod = new[] { true,  true,  true,  false } },
        new BugKind { id = BugId.Kabuto, hint = "ぞうきばやしの みき。あさ はやくか 夜", sizeMm = 78,   name = "かぶとむし",   perch = BugPerch.Trunk, height = 0.34f,
                      wary = 0.10f, catchRate = 0.90f, weight = 10, power = 6, ba = BugBa.Zoki,   tod = new[] { true,  false, false, true  } },
        new BugKind { id = BugId.Kuwagata, hint = "ぞうきばやしの みき。あさ はやくか 夜", sizeMm = 66, name = "くわがた",     perch = BugPerch.Trunk, height = 0.32f,
                      wary = 0.15f, catchRate = 0.85f, weight = 10, power = 5, ba = BugBa.Zoki,   tod = new[] { true,  false, false, true  } },
        new BugKind { id = BugId.Tonbo, hint = "かわべりの 上を とんで いる", sizeMm = 52,    name = "しおからとんぼ", perch = BugPerch.Air,  height = 0.30f,
                      wary = 0.70f, catchRate = 0.55f, weight = 28, power = 3, ba = BugBa.Kawa,   tod = new[] { true,  true,  true,  false } },
        new BugKind { id = BugId.Oniyanma, hint = "やまの 上。ひるまに 谷を 見おろして いる", sizeMm = 98, name = "おにやんま",   perch = BugPerch.Air,   height = 0.40f,
                      wary = 0.90f, catchRate = 0.30f, weight = 6,  power = 5, ba = BugBa.Yama,   tod = new[] { false, true,  true,  false } },
        new BugKind { id = BugId.Chou, hint = "のはらの 花の ある ところ", sizeMm = 105,     name = "あげはちょう", perch = BugPerch.Air,   height = 0.28f,
                      wary = 0.45f, catchRate = 0.75f, weight = 24, power = 1, ba = BugBa.Nohara, tod = new[] { true,  true,  false, false } },
        new BugKind { id = BugId.Batta, hint = "のはらの 草の あいだ", sizeMm = 48,    name = "しょうりょうばった", perch = BugPerch.Grass, height = 0.26f,
                      wary = 0.60f, catchRate = 0.65f, weight = 22, power = 4, ba = BugBa.Nohara, tod = new[] { true,  true,  true,  false } },
        new BugKind { id = BugId.Hotaru, hint = "かわべりの しげみ。よる だけ", sizeMm = 14,   name = "ほたる",       perch = BugPerch.Bush,  height = 0.22f,
                      wary = 0.20f, catchRate = 0.80f, weight = 18, power = 1, glows = true, ba = BugBa.Kawa,
                      tod = new[] { false, false, false, true  } },
    };

    public static BugKind Of(BugId id) { return All[(int)id]; }

    public int Index { get { return (int)id; } }
}
