// 部屋が 育つ。（2026-08-17）
//
// ★遊ぶ 人からの 言：「**8月31日の 魔理沙の 部屋は、8月1日と 完全に 同一です。**
//   8種類 集めても、31枚 書いても、部屋の 見た目は 1ミリも 変わらない。
//   ぼくなつで 31日を 過ごした 実感が どこから 来て いたかと いうと、
//   **溜まった ものが 目に 見えて いたから** です。虫かごが 増える。標本箱が 埋まる」
//
// ★**しくみは もう ある。**ひみつきち（HimitsuBase）が 5段階で 目に 見えて 育つ。
//   あれと 同じ「先に ぜんぶ 建てて おいて、できた ぶんだけ 見せる」を 部屋に 持ってくる。
//   そのつど 作ると 場面を 組みなおす たびに 消えるし、地めんを 測りなおす ことに なる。
//
// 見せる 数の もとは ぜんぶ **すでに 数えて ある もの**：
//   標本箱＝BugBook.SpecimenKinds／絵日記の 束＝Nikki.EnikkiMai／
//   花瓶の 花＝PlayHost.Flowers／壁の おし花＝PlayHost.Oshibana
using UnityEngine;

public class Sodatsu : MonoBehaviour {

    [Header("ふえて いく もの（先に ぜんぶ 建てて おく）")]
    [Tooltip("標本箱の 中み。しゅるいの 数だけ 出す")]
    public Renderer[] hyohon;
    [Tooltip("つくえに つみあがる 絵日記。4まいで 1だん")]
    public Renderer[] enikki;
    [Tooltip("花びんの 花。つんだ 数で ふえる")]
    public Renderer[] hana;
    [Tooltip("かべの おし花")]
    public Renderer[] oshi;

    [HideInInspector] public BugBook book;
    [HideInInspector] public Nikki nikki;
    [HideInInspector] public PlayHost play;

    float left;

    void Start() { Refresh(); }

    // **毎フレーム 見なくて いい。**数が 変わるのは 遊びが 1回 終わった ときだけ
    void Update() {
        left -= Time.deltaTime;
        if (left > 0f) return;
        left = 1.0f;
        Refresh();
    }

    public void Refresh() {
        Show(hyohon, book  != null ? book.SpecimenKinds : 0);
        Show(enikki, nikki != null ? (nikki.EnikkiMai + 3) / 4 : 0);   // 4まいで 1だん
        Show(hana,   play  != null ? play.Flowers : 0);
        Show(oshi,   play  != null ? play.Oshibana : 0);
    }

    // ★**Renderer.enabled は さわらない。**（2026-08-17）
    //   2階の 中みは「1階の おくに いる ときだけ 消す」しくみが すでに あって、
    //   そちらが enabled を 出し入れ して いる。同じ 札を 2人で 取りあうと
    //   **1びょう おきに ちらつく**。こちらは GameObject の 生き死にで 分ける
    static void Show(Renderer[] rs, int n) {
        if (rs == null) return;
        for (int i = 0; i < rs.Length; i++) {
            if (rs[i] == null) continue;
            bool on = i < n;
            if (rs[i].gameObject.activeSelf != on) rs[i].gameObject.SetActive(on);
        }
    }
}
