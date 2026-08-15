using UnityEngine;

// 手ざわりを 見るための カメラ。実行中に 角度と 寄りを かえられる。
// **数値は インスペクタに 出してある**ので、GUIで さわって 好みを 決められる。
public class CamOrbit : MonoBehaviour {
    [Header("見る 中心")]
    public Vector3 target = new Vector3(0f, 0.9f, 0.4f);
    [Tooltip("これを 入れると その人を 追いかける")]
    public Transform follow;
    public Vector3 followOffset = new Vector3(0f, 0.7f, 0f);
    public float followLag = 6f;        // 大きいほど きびきび ついていく
    [Header("角度（度）")]
    [Range(8f, 70f)]  public float pitch = 28f;    // 見おろす 角度。30度前後が それらしい
    // yaw=180 で「部屋の 手前がわ（+Z）から 見る」。0 だと 裏に まわりこむ
    [Range(0f, 360f)] public float yaw = 180f;
    [Header("よりかた")]
    [Range(3f, 20f)] public float distance = 8.2f;
    // ★2026-08-15：**手で 動かせなく した。**
    // 見おろしの 絵は「決めた 画角で 見せる」もの。自由に 回せると 構図が くずれ、
    // 通れる ところ・見える ところの 設計が 意味を 失う
    public bool allowMouse = false;

    // ★2026-08-15：**決まった 場所だけ カメラが 動く。**
    //   ふだんは 正面で 固定。けれど 高台に のぼった ときだけ 裏へ まわりこんで、
    //   それまで 背中がわで 見えなかった 谷ぜんたいを 見せる。
    //   「自由に 回せる」のとは ちがう＝どこで どう 見えるかは こちらが 決める ので、
    //   構図は くずれない し、見せ場に なる。
    [System.Serializable]
    public class Zone {
        public string name = "みはらし";
        public Bounds area;                 // ここに 主人公が 入ったら 効く
        public float pitch = 34f;
        public float yaw = 0f;              // 180 と 0 で ちょうど 裏がわ
        public float distance = 15f;
        public Vector3 lookOffset;          // 見る 中心を ずらす（谷を 画に 入れる）
        [Tooltip("もやの こさ の かけ算。**遠くを 見せる ところでは 薄く する**")]
        public float fogScale = 1f;
        [Tooltip("うつり変わりの はやさ。小さいほど ゆっくり")]
        public float blend = 1.1f;
        [HideInInspector] public bool wasIn;
    }
    [Header("ここに 来たら カメラが 動く")]
    public Zone[] zones;
    [Tooltip("地めんから これだけは 浮かせる。0 で 切る")]
    public float groundClearance = 1.6f;

    // いま 実さいに 使って いる 値（base ＝ 上の pitch/yaw/distance）
    float curPitch, curYaw, curDist, curFog = 1f;
    Vector3 curOff;
    bool ready;

    /// <summary>いまの もやの こさ の かけ算。TimeOfDay が 霧を おく ときに 見る</summary>
    public float FogScale { get { return curFog; } }

    void OnEnable()  { ready = false; Apply(); }
    void OnValidate(){ Apply(); }

    void Update() {
        if (allowMouse && Application.isPlaying) {
            if (Input.GetMouseButton(1)) {                    // 右ドラッグで まわす
                yaw   += Input.GetAxis("Mouse X") * 2.2f;
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 1.6f, 8f, 70f);
            }
            float w = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(w) > 0.0001f) distance = Mathf.Clamp(distance - w * 6f, 3f, 20f);
        }
        Apply();
    }

    /// <summary>いま 主人公が 入って いる 見せ場。無ければ null</summary>
    public Zone ActiveZone() {
        if (zones == null || follow == null) return null;
        Zone best = null; float bestV = float.MaxValue;
        foreach (var z in zones) {
            if (z == null) continue;
            // **ふちで ぱたぱたしない ように、入った あとは 少し 広げて 見る。**
            // これが 無いと さかい目で カメラが 行ったり 来たり する
            var a = z.area;
            if (z.wasIn) a.Expand(new Vector3(1.6f, 0f, 1.6f));
            bool inside = a.Contains(follow.position);
            z.wasIn = inside;
            if (!inside) continue;
            // かさなって いたら **せまい ほうを とる**（こまかい 指定が 勝つ）
            float v = z.area.size.x * z.area.size.z;
            if (v < bestV) { bestV = v; best = z; }
        }
        return best;
    }

    void Apply() {
        var zone = ActiveZone();
        float wantPitch = zone != null ? zone.pitch : pitch;
        float wantYaw   = zone != null ? zone.yaw : yaw;
        float wantDist  = zone != null ? zone.distance : distance;
        Vector3 wantOff = zone != null ? zone.lookOffset : Vector3.zero;
        float wantFog = zone != null ? zone.fogScale : 1f;
        float blend = zone != null ? zone.blend : 1.1f;

        if (!ready || !Application.isPlaying) {
            curPitch = wantPitch; curYaw = wantYaw; curDist = wantDist; curOff = wantOff;
            curFog = wantFog;
            ready = true;
        } else {
            float k = 1f - Mathf.Exp(-blend * Time.deltaTime);
            curPitch = Mathf.Lerp(curPitch, wantPitch, k);
            // **角度は 近い ほうから 回す。** ふつうの Lerp だと 180→0 で
            // 世界を ぐるっと 一周して しまう
            curYaw = Mathf.LerpAngle(curYaw, wantYaw, k);
            curDist = Mathf.Lerp(curDist, wantDist, k);
            curOff = Vector3.Lerp(curOff, wantOff, k);
            curFog = Mathf.Lerp(curFog, wantFog, k);
        }

        if (follow != null) {
            var want = follow.position + followOffset + curOff;
            target = Application.isPlaying
                ? Vector3.Lerp(target, want, 1f - Mathf.Exp(-followLag * Time.deltaTime))
                : want;
        }
        var rot = Quaternion.Euler(curPitch, curYaw, 0f);
        var pos = target - (rot * Vector3.forward) * curDist;

        // ★**地めんに もぐらせない。**
        //   高台は 山の 中ほどに 削った 棚なので、そこで カメラを 裏へ まわすと
        //   カメラの 置き場所が **山の 内がわ**に なる（実さい 地形を 測ったら
        //   棚が 11.6m なのに その 奥の 斜面は 19m あった）。
        //   下に レイを 打って、地めんより 下なら 持ちあげる。
        //   ※見えない かべは 層2(Ignore Raycast)なので 拾わない
        if (Application.isPlaying && groundClearance > 0f) {
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(pos.x, pos.y + 80f, pos.z), Vector3.down,
                                out hit, 300f, ~0, QueryTriggerInteraction.Ignore)) {
                float floor = hit.point.y + groundClearance;
                if (pos.y < floor) pos.y = floor;
            }
        }

        transform.position = pos;
        transform.rotation = rot;
    }
}
