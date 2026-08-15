using System.Collections.Generic;
using UnityEngine;

// 屋内を 見せる ための「壁の 抜きかた」。
//
// **家の 中に 入ったら、手前の 壁が 消える。奥へ 進むと さらに 奥の 壁と
// 2階の 床が 消える。** こうしないと 主人公が 壁の うらに 隠れて 見えなくなる。
//
// 見おろしの ゲームでは これを「切りぬき」と 呼ぶ。すけさせる やりかたも あるが、
// ドット絵の 切りぬきは 半とうめいに すると 汚くなるので、**消す**ほうを とる。
//
// つかいかた：かくす ものを Piece に 入れ、hideWhenPlayerBeyondZ より
// 主人公が おくに いたら 消す。1階/2階は floor で 分ける。
public class RoomCutaway : MonoBehaviour {

    [System.Serializable]
    public class Piece {
        public Renderer[] parts;
        [Tooltip("主人公が この Z より おくに いたら 消す")]
        public float hideBeyondZ = 99f;
        // ★hideBelowY を 入れた ときは **両方 そろって はじめて 消す。**
        //   「または」に して いた ころは、2階に 上がった とたん その 2階が
        //   まるごと 消えて 主人公が 宙に 浮いた。
        //   2階を 消したいのは「1階の おくに いる とき」だけ＝奥ゆき と 高さ の 両方
        [Tooltip("主人公が この 高さより 下に いたら 消す（2階むけ。奥ゆきの 条件と かつ で 効く）")]
        public float hideBelowY = -99f;
        [HideInInspector] public bool hidden;
    }

    public Transform player;
    public Piece[] pieces;
    [Tooltip("家の 外との さかい。ここより おくが 屋内")]
    public float doorZ = 3.2f;
    public Bounds houseArea = new Bounds(new Vector3(0f, 1f, 0f), new Vector3(9f, 6f, 8f));

    bool inside;

    void Start() {
        if (player == null) {
            var pm = FindFirstObjectByType<PlayerMove>();
            if (pm != null) player = pm.transform;
        }
        Apply(true);
    }

    void LateUpdate() { Apply(false); }

    void Apply(bool force) {
        if (player == null || pieces == null) return;
        var p = player.position;
        bool nowInside = houseArea.Contains(p);
        if (!force && nowInside == inside) {
            // 中に いる あいだは 奥ゆきで こまかく 切りかえる
            if (!nowInside) return;
        }
        inside = nowInside;

        foreach (var pc in pieces) {
            if (pc == null || pc.parts == null) continue;
            // そとに いる ときは ぜんぶ 見せる（家は ふつうに 建って 見える）
            bool byZ = p.z < pc.hideBeyondZ;
            bool hide = inside && (pc.hideBelowY > -98f ? (byZ && p.y < pc.hideBelowY) : byZ);
            if (!force && hide == pc.hidden) continue;
            pc.hidden = hide;
            foreach (var r in pc.parts) if (r != null) r.enabled = !hide;
        }
    }
}
