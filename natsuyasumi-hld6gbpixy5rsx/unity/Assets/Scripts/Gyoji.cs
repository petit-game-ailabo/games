// 行事（ぎょうじ）。**カレンダーの できごとを、世界の 側に 起こす。**（2026-08-17）
//
// ★遊ぶ 人からの 言：「いま 入ったのは カレンダーでは なく **掲示板**です。
//   8月10日の 朝、魔理沙が『きょうは 祭りだ！』と 言う。プレイヤーは 夜まで 待って
//   祠へ 行く。**何も ありません。** これは カレンダーが 無かった 昨日より 悪い。
//   昨日までの ゲームは 退屈でしたが、嘘は ついて いなかった」
//
// **まったく その とおり。祭りを 作ってから「祭りだ」と 言う。順番が それだけ。**
//
// ここは 朝に 1回 呼ばれ、その日の 飾りつけ・人の 集まり を 切りかえる。
// 飾りは **場面を 組む ときに ぜんぶ 建てて おいて、出し入れ だけ する**
//（走って いる あいだに 建てると 重い し、地めんの 高さを 測りなおす ことに なる）。
using UnityEngine;

public class Gyoji : MonoBehaviour {

    [Header("飾り（BuildZashiki が 割りあて）")]
    public GameObject matsuri;        // 祭りの 提灯・屋台 ひとそろい
    public GameObject junbi;          // 前の 日の「準備中」（提灯が 半分）
    public GameObject toro;           // とうろう流し
    public GameObject niji;           // 虹
    public GameObject ochiba;         // 祭りの あくる朝、落ちて いる 提灯

    [Header("さいごの 夕がた（8月31日）に みんなが 集まる ところ")]
    public Vector3 engawaBa;

    [Header("祭りの 夜に 人が あつまる ところ")]
    public Vector3 matsuriBa;
    public Npc[] people;

    Vector3[] yoruMoto;               // ふだんの 夜の 居場所（もどす ため）

    void Awake() {
        if (people != null) {
            yoruMoto = new Vector3[people.Length];
            for (int i = 0; i < people.Length; i++)
                if (people[i] != null) yoruMoto[i] = people[i].posYoru;
        }
    }

    /// <summary>その日の 世界に する。**朝に 1回**</summary>
    public void Apply(int day) {
        var k = Nikki.OnDay(day);
        // ★**祭りは 2晩（宵宮と 本祭り）。**飾りは 同じ ものを 出す
        Set(matsuri, k == Nikki.Koto2.Matsuri || k == Nikki.Koto2.Yoimiya);
        // あくる朝の 落とし物＝「祭りは あった」の 証拠
        Set(ochiba, k == Nikki.Koto2.Atokatazuke);
        // ★**予告は 文では なく 物で 出す。**祭りの 前の日の 夜、提灯が 半分だけ 吊ってある
        Set(junbi, k == Nikki.Koto2.MatsuriYokoku);
        Set(toro, k == Nikki.Koto2.Toro);
        Set(niji, k == Nikki.Koto2.Niji);

        // 祭りの 夜は **人が 祠に あつまる**。ふだんの 夜の 居場所を 1日だけ 上書き
        if (people == null || yoruMoto == null) return;
        for (int i = 0; i < people.Length; i++) {
            if (people[i] == null) continue;
            if (k == Nikki.Koto2.Matsuri || k == Nikki.Koto2.Yoimiya) {
                var c = new Vector3(Mathf.Cos(i * 1.9f), 0f, Mathf.Sin(i * 1.9f)) * 2.6f;
                var p = matsuriBa + c;
                p.y = GroundY(p);
                people[i].posYoru = p;
            } else if (k == Nikki.Koto2.Saigo) {
                // ★**さいごの 夕がた、縁側に みんなが いる。**
                //   祭りで 作った「人が 集まる」しくみを そのまま つかう
                var c = new Vector3((i - 2) * 1.6f, 0f, 0f);
                var p = engawaBa + c;
                p.y = GroundY(p);
                people[i].posYoru = p;
                people[i].posHiru = p;         // 夕がたには もう いて ほしい
            } else {
                people[i].posYoru = yoruMoto[i];
            }
            people[i].koto = k;
        }
    }

    static void Set(GameObject g, bool on) { if (g != null && g.activeSelf != on) g.SetActive(on); }

    static float GroundY(Vector3 p) {
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(p.x, p.y + 40f, p.z), Vector3.down, out hit, 120f,
                            ~(1 << 2), QueryTriggerInteraction.Ignore)) return hit.point.y;
        return p.y;
    }
}
