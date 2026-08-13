using UnityEngine;

// 虫の 種類と、その ふるまいの ちがいを 1か所に まとめる。
// **表と ふるまいを 分けない。** 虫を 足すときは ここに 1行 足せば、
// 出かた・逃げかた・図かんの ならびまで ぜんぶ ついてくる。
public enum BugId {
    Semi = 0, Kabuto = 1, Kuwagata = 2, Tonbo = 3,
    Oniyanma = 4, Chou = 5, Batta = 6, Hotaru = 7,
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

    // 1コマ 16x16 の 4列 x 2行。index が そのまま コマの ばんごう
    public const int Cols = 4, Rows = 2;

    // ★虫を 足すなら ここに 1行。あとは ぜんぶ ついてくる
    public static readonly BugKind[] All = {
        new BugKind { id = BugId.Semi,     name = "あぶらぜみ",   perch = BugPerch.Trunk, height = 0.30f,
                      wary = 0.55f, catchRate = 0.70f, weight = 30, tod = new[] { true,  true,  true,  false } },
        new BugKind { id = BugId.Kabuto,   name = "かぶとむし",   perch = BugPerch.Trunk, height = 0.34f,
                      wary = 0.10f, catchRate = 0.90f, weight = 10, tod = new[] { true,  false, false, true  } },
        new BugKind { id = BugId.Kuwagata, name = "くわがた",     perch = BugPerch.Trunk, height = 0.32f,
                      wary = 0.15f, catchRate = 0.85f, weight = 10, tod = new[] { true,  false, false, true  } },
        new BugKind { id = BugId.Tonbo,    name = "しおからとんぼ", perch = BugPerch.Air,  height = 0.30f,
                      wary = 0.70f, catchRate = 0.55f, weight = 28, tod = new[] { true,  true,  true,  false } },
        new BugKind { id = BugId.Oniyanma, name = "おにやんま",   perch = BugPerch.Air,   height = 0.40f,
                      wary = 0.90f, catchRate = 0.30f, weight = 6,  tod = new[] { false, true,  true,  false } },
        new BugKind { id = BugId.Chou,     name = "あげはちょう", perch = BugPerch.Air,   height = 0.28f,
                      wary = 0.45f, catchRate = 0.75f, weight = 24, tod = new[] { true,  true,  false, false } },
        new BugKind { id = BugId.Batta,    name = "しょうりょうばった", perch = BugPerch.Grass, height = 0.26f,
                      wary = 0.60f, catchRate = 0.65f, weight = 22, tod = new[] { true,  true,  true,  false } },
        new BugKind { id = BugId.Hotaru,   name = "ほたる",       perch = BugPerch.Bush,  height = 0.22f,
                      wary = 0.20f, catchRate = 0.80f, weight = 18, glows = true,
                      tod = new[] { false, false, false, true  } },
    };

    public static BugKind Of(BugId id) { return All[(int)id]; }

    public int Index { get { return (int)id; } }
}
