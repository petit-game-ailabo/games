using UnityEngine;

// 縦切り1日（R4の 先がけ）：時計・太陽・朝夕の 色・17時の チャイム・Zで ねて 翌日。
// 「1日が 回る」ことを 箱の 村で 確かめる ための 最小の 器。
// 本番の 移植（Nikki/TimeOfDay 系）は R4 で これを 置きかえる。
public class MuraDay : MonoBehaviour {
    public Light sun;
    public Font font;

    public static int Day = 1;
    public static float Hour = 6.5f;
    public static bool Night { get { return Hour < 5f || Hour >= 19f; } }

    const float SecPerHour = 40f;        // 1時間=40秒 → 6:00-21:00 が 10分（検証用の 早回し）
    // その日 つかまえた 虫など「あしたに なったら 戻る」もの
    public static readonly System.Collections.Generic.List<GameObject> Ashita =
        new System.Collections.Generic.List<GameObject>();
    bool chimed;
    AudioSource chime;

    void Start() {
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

        // Z で ねる → 翌日の 朝
        if (Input.GetKeyDown(KeyCode.Z)) {
            Day = Mathf.Min(Day + 1, 31); Hour = 6.5f;
            foreach (var g in Ashita) if (g != null) g.SetActive(true);   // 虫は あしたも 出る
            Ashita.Clear();
        }

        if (sun == null) return;
        float t = Mathf.InverseLerp(5f, 19f, Hour);           // 日の出〜日の入り
        float bright = Mathf.Clamp01(Mathf.Sin(t * Mathf.PI));
        sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(8f, 172f, t), -35f, 0f);
        sun.intensity = Mathf.Lerp(0.02f, 1.25f, bright);
        sun.color = Color.Lerp(new Color(1f, 0.55f, 0.35f),   // 朝夕は 焼ける
                               new Color(1f, 0.95f, 0.84f), bright);
        RenderSettings.ambientLight = Color.Lerp(new Color(0.10f, 0.12f, 0.20f),
                                                 new Color(0.52f, 0.56f, 0.60f), bright);
        RenderSettings.fogColor = Color.Lerp(new Color(0.10f, 0.12f, 0.18f),
                                             new Color(0.74f, 0.78f, 0.74f), bright);
    }

    void OnGUI() {
        if (font != null) GUI.skin.font = font;
        int h = Mathf.FloorToInt(Hour), m = Mathf.FloorToInt((Hour - h) * 60f);
        GUI.Label(new Rect(Screen.width - 250, 8, 240, 26),
                  "8月" + Day + "日  " + h.ToString("00") + ":" + m.ToString("00") + "   Z=ねる");
    }
}
