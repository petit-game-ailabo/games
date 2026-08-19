using UnityEngine;

// S1-3：位置に 置く 音源。距離と 遮蔽で 聞こえ方が 変わる。
// ・きこえる 半径（kikoeru）の 中で、近いほど 大きい。
// ・あいだに 物が あると こもる（音量が 下がり、高い 成分が 削れる）。
// ・**低い 音ほど 回り込む**：遮蔽の 減衰を 周波数で 変える（物理どおり）。
// 音は 手続き生成（本編の audio.js と 同じ 思想。ファイル 不要）。
public class MuraOto : MonoBehaviour {
    public string namae = "むし";
    public enum Koe { Semi, Suzumushi, Kawa, Kaeru }
    public Koe koe = Koe.Semi;
    public float kikoeru = 18f;      // 届く 半径(m)
    public float ookisa = 0.8f;      // もとの 大きさ 0..1
    public float takasa = 1f;        // 音の 高さの かけ算（1=素）

    Transform listener;
    AudioSource src;
    AudioLowPassFilter lpf;
    float sissoku;                   // 遮蔽の なめらか値 0..1

    // 声ごとの 基本周波数（回り込みの 計算にも つかう）
    float BaseHz() {
        switch (koe) {
            case Koe.Semi: return 4200f;
            case Koe.Suzumushi: return 2200f;
            case Koe.Kaeru: return 700f;
            default: return 220f;    // 川（低い ごう音）
        }
    }

    void Start() {
        src = gameObject.AddComponent<AudioSource>();
        lpf = gameObject.AddComponent<AudioLowPassFilter>();
        lpf.cutoffFrequency = 22000f;
        src.clip = OtoGen.Clip(koe, takasa);
        src.loop = true; src.spatialBlend = 0f;   // 距離は 自前で 計算（2D再生）
        src.volume = 0f;
        src.Play();
        var cam = Camera.main;
        listener = cam != null ? cam.transform : null;
    }

    void Update() {
        // 聞き手は 主人公（カメラだと カットで 音が 飛ぶ）
        if (MuraOtoKikite.I != null) listener = MuraOtoKikite.I.transform;
        if (listener == null || src == null) return;
        float d = Vector3.Distance(listener.position, transform.position);
        float kyori = Mathf.Clamp01(1f - d / kikoeru);
        kyori *= kyori;                                        // 近くで ぐっと 大きく

        // 遮蔽：聞き手との あいだに 物が あるか（0.25秒 なめらか）
        bool blocked = false;
        if (d < kikoeru) {
            RaycastHit hit;
            var a = transform.position + Vector3.up * 0.8f;
            var b = listener.position + Vector3.up * 0.8f;
            if (Physics.Linecast(a, b, out hit, ~0, QueryTriggerInteraction.Ignore))
                blocked = hit.collider.transform != listener &&
                          !listener.IsChildOf(hit.collider.transform) &&
                          !hit.collider.transform.IsChildOf(listener);
        }
        sissoku = Mathf.Lerp(sissoku, blocked ? 1f : 0f, 1f - Mathf.Exp(-4f * Time.deltaTime));

        // 低い 音ほど 回り込む：4kHz(セミ)は 0.2 まで 落ち、220Hz(川)は 0.75 のこる
        float mawari = Mathf.Lerp(0.85f, 0.2f, Mathf.InverseLerp(200f, 4500f, BaseHz() * takasa));
        float saegiri = Mathf.Lerp(1f, mawari, sissoku);
        src.volume = ookisa * kyori * saegiri;
        lpf.cutoffFrequency = Mathf.Lerp(22000f, 900f, sissoku); // こもり
    }
}

/// <summary>聞き手の しるし（主人公に つける）</summary>
public class MuraOtoKikite : MonoBehaviour {
    public static MuraOtoKikite I;
    void Awake() { I = this; }
}

/// <summary>声を 手続き生成する（ファイル 不要・44100Hz 2秒 ループ）</summary>
public static class OtoGen {
    public static AudioClip Clip(MuraOto.Koe koe, float takasa) {
        const int SR = 44100; int n = SR * 2;
        var d = new float[n];
        var rnd = new System.Random((int)koe * 7 + 1);
        switch (koe) {
            case MuraOto.Koe.Semi: {          // ジー…（ノイズを 4.2kHzに 寄せて 揺らす）
                float ph = 0f;
                for (int i = 0; i < n; i++) {
                    float t = i / (float)SR;
                    float am = 0.6f + 0.4f * Mathf.Sin(t * 22f);
                    ph += (4200f * takasa + 600f * Mathf.Sin(t * 9f)) / SR;
                    float tone = Mathf.Sin(ph * Mathf.PI * 2f);
                    float noise = (float)(rnd.NextDouble() * 2 - 1);
                    d[i] = (tone * 0.55f + noise * 0.45f) * am * 0.5f;
                }
                break;
            }
            case MuraOto.Koe.Suzumushi: {     // リーン、リーン（減衰する 正弦の 繰り返し）
                for (int i = 0; i < n; i++) {
                    float t = i / (float)SR;
                    float cyc = t % 0.9f;
                    float env = cyc < 0.45f ? Mathf.Exp(-cyc * 6f) : 0f;
                    d[i] = Mathf.Sin(t * 2200f * takasa * Mathf.PI * 2f)
                         * (0.7f + 0.3f * Mathf.Sin(t * 40f)) * env * 0.5f;
                }
                break;
            }
            case MuraOto.Koe.Kaeru: {         // ゲコッ（短い 下がり音の 繰り返し）
                for (int i = 0; i < n; i++) {
                    float t = i / (float)SR;
                    float cyc = t % 0.62f;
                    float env = cyc < 0.14f ? Mathf.Sin(cyc / 0.14f * Mathf.PI) : 0f;
                    float f = (760f - cyc * 900f) * takasa;
                    d[i] = Mathf.Sin(t * f * Mathf.PI * 2f) * env * 0.6f;
                }
                break;
            }
            default: {                        // 川（低い ノイズの ざわめき）
                float lp = 0f;
                for (int i = 0; i < n; i++) {
                    float noise = (float)(rnd.NextDouble() * 2 - 1);
                    lp = Mathf.Lerp(lp, noise, 0.04f * takasa);   // ローパス＝ごう音
                    d[i] = lp * 1.6f;
                }
                break;
            }
        }
        // ループの つなぎ目を なめらかに
        for (int i = 0; i < 600; i++) {
            float k = i / 600f;
            d[i] *= k; d[n - 1 - i] *= k;
        }
        var clip = AudioClip.Create(koe.ToString(), n, 1, SR, false);
        clip.SetData(d, 0);
        return clip;
    }
}
