using UnityEngine;

// 部屋に 入ったら 名まえを 出す。
//
// 家具を 置いても、田の字の 4間は 見た目が よく 似て いる。
// 「おじさんたちの ねま」と 言われて どこの ことか 分かるように、
// **その 部屋に 立った ときに 一度だけ** 名を 出す。
// 出しっぱなしには しない（説明が うるさいと 家に 居る 気が しなく なる）。
public class RoomLabel : MonoBehaviour {

    [System.Serializable]
    public class Room {
        public string name;
        public Bounds area;
        [HideInInspector] public bool inside;
    }

    public Transform player;
    public Room[] rooms;
    [Tooltip("同じ 部屋の 名を もう一度 出すまでの 間（秒）")]
    public float repeatAfter = 40f;

    BugHud hud;
    float[] lastAt;

    void Start() {
        hud = FindFirstObjectByType<BugHud>();
        if (player == null) {
            var pm = FindFirstObjectByType<PlayerMove>();
            if (pm != null) player = pm.transform;
        }
        if (rooms != null) lastAt = new float[rooms.Length];
    }

    void Update() {
        if (player == null || rooms == null || hud == null) return;
        var p = player.position;
        for (int i = 0; i < rooms.Length; i++) {
            var r = rooms[i];
            if (r == null) continue;
            bool now = r.area.Contains(p);
            if (now && !r.inside && Time.time - lastAt[i] > repeatAfter) {
                lastAt[i] = Time.time;
                hud.Say(r.name);
            }
            r.inside = now;
        }
    }
}
