// さいふ。（2026-08-17）
//
// ★遊ぶ 人からの 言：「谷の おくの 町が、まだ 空き家の まま。5棟 建って いて 誰も
//   いない。**行く 理由が ゼロ。** 一番 安いのは 駄菓子屋です。
//   『歩いて 15分 かかる 店に、300円 持って 行く』——あの 遠さが 子どもの 夏でした」
//
// ★**かごが 5ひきで 詰まる 痛みが、買う 理由に なる。**
//   さきに かごの 上限を 入れて から 店を 出す。順番が これで 合う。
//
// 置き場は PlayerPrefs 1つ。持ちものは いまの ところ お金だけ なので、
// わざわざ MonoBehaviour に しない（場面を 組みなおす たびに 配線が 増える）。
using UnityEngine;

public static class Saifu {
    const string Key = "natsuyasumi.okane.v1";
    const string GotKey = "natsuyasumi.okane.moratta.v1";   // 小づかいを もらった 日

    public static int Yen { get { return PlayerPrefs.GetInt(Key, 0); } }

    public static void Add(int n) {
        if (n <= 0) return;
        PlayerPrefs.SetInt(Key, Yen + n);
        PlayerPrefs.Save();
    }

    /// <summary>はらえたら true。**足りなければ 何も しない**</summary>
    public static bool Tsukau(int n) {
        if (Yen < n) return false;
        PlayerPrefs.SetInt(Key, Yen - n);
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>その日の 小づかいを まだ もらって いないか（1日 1回）</summary>
    public static bool Madamorattenai(int day) { return PlayerPrefs.GetInt(GotKey, 0) != day; }
    public static void Moratta(int day) { PlayerPrefs.SetInt(GotKey, day); PlayerPrefs.Save(); }

    /// <summary>はじめから の とき。**お金は 1周ぶんの もの**なので 消す</summary>
    public static void Reset0() {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.DeleteKey(GotKey);
        PlayerPrefs.Save();
    }
}
