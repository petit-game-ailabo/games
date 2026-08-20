using UnityEngine;

// S1-2：俯瞰エディタ（まず「見る」）。F2 で 真上からの 見取り図に 切り替わる。
// 見える もの：カメラの 担当範囲（わく）・カメラの 位置と 向き（点と 線）・
// いま 効いて いる 台（黄色）・主人公（赤い 点）・音源の 届く 半径（円・S1-3で つなぐ）。
// 名前は その場所の 上に 出す。移動も そのまま できる ので、歩きながら 範囲を たしかめる。
public class MuraFukan : MonoBehaviour {
    public MuraCamFixed fix;
    public Transform target;
    public float height = 55f, size = 45f;

    bool on;
    Camera cam;
    Material lineMat;
    float bakPitch; Vector3 bakPos; Quaternion bakRot; float bakFov;

    void Start() {
        cam = GetComponent<Camera>();
        lineMat = new Material(Shader.Find("Hidden/Internal-Colored"));
        lineMat.hideFlags = HideFlags.HideAndDontSave;
        lineMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
    }

    public bool On { get { return on; } }
    Vector3 pan;                         // 見て いる 場所の ずらし（マウスで 動かす）

    public void Set(bool v) {
        if (on == v) return;
        on = v;
        MuraCamFixed.Suspended = on;     // 俯瞰の あいだは カメラ制御を 完全に 止める
        if (on) { pan = Vector3.zero; }
        else {
            // ★もどす（前は 正射影の まま 置き去りに なり「戻れない」に なって いた）
            if (cam != null) cam.orthographic = false;
            var fx = GetComponent<MuraCamFixed>();
            if (fx != null) fx.Reapply();
        }
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.F2)) Set(!on);
        if (!on) return;
        // マウスの 左ドラッグで 見る場所を うごかす／ホイールで 寄り引き
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
            pan -= new Vector3(Input.GetAxis("Mouse X"), 0f, Input.GetAxis("Mouse Y"))
                   * (size * 0.06f);
        float w = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(w) > 0.0001f) size = Mathf.Clamp(size - w * 22f, 12f, 95f);
    }

    void LateUpdate() {
        if (!on || target == null) return;
        transform.position = target.position + pan + Vector3.up * height;
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        if (cam != null) { cam.orthographic = true; cam.orthographicSize = size; }
    }

    // ★URP は OnPostRender を 呼ばない。パイプラインの「カメラ描画おわり」に つなぐ
    void OnEnable() { UnityEngine.Rendering.RenderPipelineManager.endCameraRendering += Draw; }
    void OnDisable() {
        UnityEngine.Rendering.RenderPipelineManager.endCameraRendering -= Draw;
        if (cam != null) cam.orthographic = false;
    }

    // ---- 線を ひく ----
    void Draw(UnityEngine.Rendering.ScriptableRenderContext ctx, Camera c) {
        if (c != cam) return;
        if (!on || fix == null || fix.spots == null) return;
        lineMat.SetPass(0);
        GL.Begin(GL.LINES);
        foreach (var s in fix.spots) {
            if (s == null) continue;
            bool active = MuraCamFixed.CurName == s.name;
            GL.Color(active ? new Color(1f, 0.85f, 0.1f) : new Color(0.15f, 0.55f, 0.95f, 0.9f));
            var a = s.area;
            float y = 1.5f;
            Seg(new Vector3(a.min.x, y, a.min.z), new Vector3(a.max.x, y, a.min.z));
            Seg(new Vector3(a.max.x, y, a.min.z), new Vector3(a.max.x, y, a.max.z));
            Seg(new Vector3(a.max.x, y, a.max.z), new Vector3(a.min.x, y, a.max.z));
            Seg(new Vector3(a.min.x, y, a.max.z), new Vector3(a.min.x, y, a.min.z));
            // カメラの 位置 → 見る さき
            GL.Color(active ? new Color(1f, 0.6f, 0.1f) : new Color(0.6f, 0.6f, 0.6f, 0.8f));
            Seg(s.pos, s.lookAt);
            Cross(s.pos, 1.2f);
        }
        // 主人公
        GL.Color(Color.red);
        Cross(target.position + Vector3.up * 1f, 1.5f);
        // 音源（S1-3）
        var otos = FindObjectsByType<MuraOto>(FindObjectsSortMode.None);
        foreach (var o in otos) {
            GL.Color(new Color(0.2f, 0.9f, 0.4f, 0.9f));
            Circle(o.transform.position, o.kikoeru, 40);
            GL.Color(new Color(0.2f, 0.9f, 0.4f, 0.35f));
            Circle(o.transform.position, o.kikoeru * 0.35f, 24);
        }
        GL.End();
    }

    void Seg(Vector3 a, Vector3 b) { GL.Vertex(a); GL.Vertex(b); }
    void Cross(Vector3 p, float r) {
        Seg(p + Vector3.left * r, p + Vector3.right * r);
        Seg(p + Vector3.forward * r, p + Vector3.back * r);
    }
    void Circle(Vector3 c, float r, int n) {
        for (int i = 0; i < n; i++) {
            float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
            Seg(c + new Vector3(Mathf.Cos(a0) * r, 1.5f, Mathf.Sin(a0) * r),
                c + new Vector3(Mathf.Cos(a1) * r, 1.5f, Mathf.Sin(a1) * r));
        }
    }

    // ---- 名前 ----
    void OnGUI() {
        if (!on || fix == null || fix.spots == null || cam == null) return;
        GUI.Label(new Rect(10, 34, 900, 24), "F2=俯瞰をとじる   左ドラッグ=見る場所をうごかす   ホイール=寄り引き");
        // 凡例（画面の 左下）
        var lg = new Rect(10, Screen.height - 118, 420, 112);
        GUI.Box(lg, "");
        GUI.Label(new Rect(18, Screen.height - 114, 400, 22), "■青のわく：カメラの担当範囲");
        GUI.Label(new Rect(18, Screen.height - 93, 400, 22), "■黄のわく：いま効いている台（線＝カメラの位置と向き）");
        GUI.Label(new Rect(18, Screen.height - 72, 400, 22), "●緑の円：音の届く半径（♪＝音源）");
        GUI.Label(new Rect(18, Screen.height - 51, 400, 22), "＋赤の十字：主人公");
        foreach (var s in fix.spots) {
            if (s == null) continue;
            var sp = cam.WorldToScreenPoint(new Vector3(s.area.center.x, 1.5f, s.area.center.z));
            if (sp.z < 0) continue;
            GUI.Label(new Rect(sp.x - 40, Screen.height - sp.y - 10, 120, 22), s.name);
        }
        var otos = FindObjectsByType<MuraOto>(FindObjectsSortMode.None);
        foreach (var o in otos) {
            var sp = cam.WorldToScreenPoint(o.transform.position);
            if (sp.z < 0) continue;
            GUI.Label(new Rect(sp.x - 30, Screen.height - sp.y - 10, 120, 22), "♪" + o.namae);
        }
    }
}
