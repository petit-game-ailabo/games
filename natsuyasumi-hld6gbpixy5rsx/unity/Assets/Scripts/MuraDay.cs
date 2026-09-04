using UnityEngine;

// 縦切り1日（R4の 先がけ）：時計・太陽・朝夕の 色・17時の チャイム・Zで ねて 翌日。
// 「1日が 回る」ことを 箱の 村で 確かめる ための 最小の 器。
// 本番の 移植（Nikki/TimeOfDay 系）は R4 で これを 置きかえる。
public class MuraDay : MonoBehaviour {
    public Light sun;
    public Font font;

    public static int Day = 1;
    public static float Hour = 6.5f;
    public static bool Night { get { return Hour < 4.7f || Hour >= 19f; } }   // 8月の 実際：日の出4:50ごろ・19時で 夜

    const float SecPerHour = 60f;        // 1時間=60秒 → 1日=24分（本人 2026-08-25「1日30分とか20分ぐらい」）
    // その日 つかまえた 虫など「あしたに なったら 戻る」もの
    public static readonly System.Collections.Generic.List<GameObject> Ashita =
        new System.Collections.Generic.List<GameObject>();
    bool chimed; bool f3;
    AudioSource chime;

    void Start() {
        // ★一旦 全部 ミュート（本人 2026-08-23「音が気持ち悪いところもある。一旦音消しておいて」）。
        //   音の 見直しフェーズ（PLAN）で この 行を 消して 戻す
        AudioListener.volume = 0f;
        foreach (var a in System.Environment.GetCommandLineArgs()) {
            if (a == "-yoru") { Day = 9; Hour = 19.8f; }      // 祭りの 夜を すぐ 見る
            if (a == "-yuyake") { Day = 3; Hour = 18.0f; }    // 夕焼けの 確認用
            // ★-hour 12 で その 時刻から はじめる（撮影の 検証用・2026-08-30）。
            //   真昼は 太陽が ほぼ 真上＝落ちる 影が 消える ので、接地の 影の 確かめに いる
            if (a.StartsWith("-hour=")) {
                float h;
                if (float.TryParse(a.Substring(6), out h)) Hour = Mathf.Repeat(h, 24f);
            }
        }
        // ★shot.ps1 の 引数を 庭でも 受ける（2026-09-05）。tools/shot.ps1 は
        //   -tod/-clock/-day を **2語で** わたす（座敷の TimeOfDay 向けの 書きかた）。
        //   ここで 読まないと 「-tod hiru で 撮った つもりが 朝6時の 絵」に なる
        var av = System.Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < av.Length; i++) {
            if (av[i] == "-clock") {
                float h2;
                if (float.TryParse(av[i + 1], out h2)) Hour = Mathf.Repeat(h2, 24f);
            } else if (av[i] == "-day") {
                int d2;
                if (int.TryParse(av[i + 1], out d2)) Day = Mathf.Clamp(d2, 1, 31);
            } else if (av[i] == "-tod") {
                switch (av[i + 1]) {
                    case "asa":    Hour = 7.0f; break;
                    case "hiru":   Hour = 12.0f; break;
                    case "yugata": Hour = 18.0f; break;
                    case "yoru":   Hour = 21.0f; break;
                }
            }
        }
        chime = gameObject.AddComponent<AudioSource>();
        chime.clip = OtoGen.Chime();
        chime.spatialBlend = 0f; chime.volume = 0.5f;
    }

    void Update() {
        Hour += Time.deltaTime / SecPerHour;
        if (Hour >= 24f) Hour -= 24f;

        // 17時の チャイム（終わりの 合図。EVENTS A の 採用ぶん）
        if (!chimed && Hour >= 17f && Hour < 17.2f) { chimed = true; chime.Play(); }
        if (Hour < 16f) chimed = false;

        // デバッグ：H=+1時間 J=あしたの朝 K=8日の夜 F3=きょうできること
        if (Input.GetKeyDown(KeyCode.H)) Hour = Mathf.Repeat(Hour + 1f, 24f);
        if (Input.GetKeyDown(KeyCode.K)) { Day = 8; Hour = 19.5f; }
        if (Input.GetKeyDown(KeyCode.F3)) f3 = !f3;
        // Z で ねる → 翌日の 朝（J も 同じ）
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.J)) {
            Day = Mathf.Min(Day + 1, 31); Hour = 6.5f;
            foreach (var g in Ashita) if (g != null) g.SetActive(true);   // 虫は あしたも 出る
            Ashita.Clear();
        }

        if (sun == null) return;
        float t = Mathf.InverseLerp(4.7f, 19f, Hour);         // 日の出〜日の入り（8月の 関東の 実際）
        // ★台形カーブ（本人 2026-08-26「18時はまだ夕焼け」「実際の日本の日の出を意識して」）：
        //   日の出 4:50ごろ→5:50には 明るい／17:40から 夕焼け→日の入り 18:45ごろ→19時で 夜
        float bright;
        if (Hour < 4.7f || Hour >= 19f) bright = 0f;
        else if (Hour < 5.8f) bright = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4.7f, 5.8f, Hour));
        else if (Hour < 18f) bright = 1f;
        else bright = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(18f, 19f, Hour));
        // ★夕焼けの 山（本人 2026-08-26「赤い光になると思う」）：18:24ごろを 頂点に 世界が 赤く 染まる
        float yu = Mathf.Clamp01(1f - Mathf.Abs(Hour - 18.4f) / 1.0f);
        // ★夜は 月明かりだけ の 暗さ。草・地面は 上向きの 面で 月光を まともに 受けるので
        //   月光は かなり 弱く（本人 2026-08-26「草が光ってない？地面も」）
        if (bright > 0.001f) {
            sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(8f, 172f, t), -35f, 0f);
            sun.intensity = Mathf.Lerp(0.02f, 1.25f, bright);
            var hiruIro = Color.Lerp(new Color(1f, 0.55f, 0.35f),
                                     new Color(1f, 0.95f, 0.84f), bright * bright);
            sun.color = Color.Lerp(hiruIro, new Color(1f, 0.38f, 0.22f), yu);   // 夕焼け＝赤い 光
        } else {
            sun.transform.rotation = Quaternion.Euler(55f, 140f, 0f);       // 太陽の 光を 月に 兼ねさせる
            sun.intensity = 0.035f;
            sun.color = new Color(0.62f, 0.70f, 0.90f);                     // 青白い 月光
        }
        var amb = Color.Lerp(new Color(0.022f, 0.028f, 0.055f),
                             new Color(0.52f, 0.56f, 0.60f), bright);
        RenderSettings.ambientLight = Color.Lerp(amb, new Color(0.42f, 0.30f, 0.24f), yu * 0.8f);
        var fogc = Color.Lerp(new Color(0.020f, 0.026f, 0.050f),
                              new Color(0.74f, 0.78f, 0.74f), bright);
        RenderSettings.fogColor = Color.Lerp(fogc, new Color(0.86f, 0.55f, 0.38f), yu * 0.85f);
        var cam = Camera.main;                                 // 空の 色も 夕焼け／夜に 合わせる
        if (cam != null) {
            var sky = Color.Lerp(new Color(0.020f, 0.030f, 0.065f),
                                 new Color(0.70f, 0.80f, 0.88f), bright);
            cam.backgroundColor = Color.Lerp(sky, new Color(0.90f, 0.56f, 0.38f), yu * 0.9f);
        }
    }

    void OnGUI() {
        if (font != null) GUI.skin.font = font;
        int h = Mathf.FloorToInt(Hour), m = Mathf.FloorToInt((Hour - h) * 60f);
        GUI.Label(new Rect(Screen.width - 250, 8, 240, 26),
                  "8月" + Day + "日  " + h.ToString("00") + ":" + m.ToString("00") + "   Z=ねる");
        if (f3) {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("== きょう できること（F3で とじる） ==");
            foreach (var a in MuraAsobi.All)
                if (a != null && a.Dekiru && a.gameObject.activeInHierarchy)
                    sb.AppendLine("・" + a.namae + "  (" +
                        a.transform.position.x.ToString("F0") + "," +
                        a.transform.position.z.ToString("F0") + ")");
            if (Day == 8) sb.AppendLine("・よる：とおくの 花火大会（高台から）");
            sb.AppendLine("（H=+1時間  J=あしたの朝  K=8日のよる）");
            GUI.Label(new Rect(Screen.width - 360, 60, 350, 500), sb.ToString());
        }
    }
}
