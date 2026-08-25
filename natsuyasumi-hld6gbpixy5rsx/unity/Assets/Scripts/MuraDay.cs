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
        // ★夜は 月明かりだけ の 暗さ（本人 2026-08-25「明かりを用意してないから、月明かりだけの明るさになるはず」）
        if (bright > 0.001f) {
            sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(8f, 172f, t), -35f, 0f);
            sun.intensity = Mathf.Lerp(0.02f, 1.25f, bright);
            sun.color = Color.Lerp(new Color(1f, 0.55f, 0.35f),   // 朝夕は 焼ける（オレンジに 寄せぎみ）
                                   new Color(1f, 0.95f, 0.84f), bright * bright);
        } else {
            sun.transform.rotation = Quaternion.Euler(55f, 140f, 0f);       // 太陽の 光を 月に 兼ねさせる
            sun.intensity = 0.06f;
            sun.color = new Color(0.62f, 0.70f, 0.90f);                     // 青白い 月光
        }
        RenderSettings.ambientLight = Color.Lerp(new Color(0.030f, 0.038f, 0.070f),
                                                 new Color(0.52f, 0.56f, 0.60f), bright);
        RenderSettings.fogColor = Color.Lerp(new Color(0.020f, 0.026f, 0.050f),
                                             new Color(0.74f, 0.78f, 0.74f), bright);
        var cam = Camera.main;                                 // 空の 色も 夜は 落とす
        if (cam != null)
            cam.backgroundColor = Color.Lerp(new Color(0.020f, 0.030f, 0.065f),
                                             new Color(0.70f, 0.80f, 0.88f), bright);
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
