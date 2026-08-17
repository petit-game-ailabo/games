using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 田舎の 遊び を まとめて 引きうける ところ。
//
// ★手ざわりの きまり（虫ずもうで しくじった ところから）
//   - **連打は させない。** どの 遊びも 押すのは 1回か 2回。
//     ねらう のは 間あいと 度あいで あって、指の はやさでは ない
//   - **技の キーと 送りの キーを 分ける。** 結果の 画が 連打で 飛ばない ように
//   - 待つ 遊び（つり）は **待って いる ことが 絵で 分かる**ように する
//
// 遊びの 場所は PlaySpot が もつ。ここは その 中身だけ。
public class PlayHost : MonoBehaviour {
    public Texture2D atlas;          // play.png（0=ささぶね 1=うき 2=石 3=はな 4=さかな 5=えだ）
    public Font font;
    public Sprite panel;

    const int Cols = 6;
    public const int IconBoat = 0, IconFloat = 1, IconStone = 2, IconFlower = 3, IconFish = 4, IconBranch = 5;

    BugHud hud;
    PlayerMove move;
    CharSprite chars;
    PlaySpot near;
    Coroutine running;

    void Mood(CharSprite.Pose p, float sec) { if (chars != null) chars.ShowMood(p, sec); }

    // ---- 画面（ゲージ 1本と ひとこと。ちいさく すませる）
    RectTransform ui;
    Image gaugeBack, gaugeFill, gaugeMark;
    Text uiLine;

    /// <summary>遊びの さいちゅう。あみや ずもうは 手を 出さない</summary>
    public bool Busy { get { return running != null; } }

    // ---- 記録
    const string KeyBoat  = "natsuyasumi.play.boats.v1";
    const string KeySkip  = "natsuyasumi.play.skip.v1";     // 水きりの さいこう記録
    const string KeyFish  = "natsuyasumi.play.fish.v1";
    const string KeyFlow  = "natsuyasumi.play.flowers.v1";  // つんだ 花（色水・押し花の もと）
    const string KeyOshi  = "natsuyasumi.play.oshibana.v1";
    const string KeyIrozu = "natsuyasumi.play.irozu.v1";
    const string KeyBase  = "natsuyasumi.play.himitsu.v1";  // ひみつきちの できぐあい 0〜4

    public int Boats   { get { return PlayerPrefs.GetInt(KeyBoat, 0); } }
    public int BestSkip{ get { return PlayerPrefs.GetInt(KeySkip, 0); } }
    public int Fish    { get { return PlayerPrefs.GetInt(KeyFish, 0); } }
    public int Flowers { get { return PlayerPrefs.GetInt(KeyFlow, 0); } }
    public int Oshibana{ get { return PlayerPrefs.GetInt(KeyOshi, 0); } }
    public int Irozu   { get { return PlayerPrefs.GetInt(KeyIrozu, 0); } }
    public int BaseStep{ get { return PlayerPrefs.GetInt(KeyBase, 0); } }

    // ★**はじめから の ときに 消す キー。**（2026-08-17）
    //   すすみ具合＝1周ぶんの もの。さいこう記録・つうさん は のこす。
    //   asobi1_* を 消さないと **どの 遊びも 二度と「はじめて」に ならない**
    public static void ResetSusumi() {
        PlayerPrefs.DeleteKey(KeyBase);                       // ひみつきちの できぐあい
        foreach (PlayKind k in System.Enum.GetValues(typeof(PlayKind)))
            PlayerPrefs.DeleteKey("asobi1_" + k);             // はじめて やった しるし
        PlayerPrefs.Save();
    }

    static void Bump(string key, int by) {
        PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key, 0) + by);
        PlayerPrefs.Save();
    }

    void Start() {
        hud = FindFirstObjectByType<BugHud>();
        move = GetComponent<PlayerMove>();
        chars = GetComponent<CharSprite>();
        BuildUI();
    }

    /// <summary>いま そばに ある 遊び場（足もとの ひとことは BugCatcher が まとめて 出す）</summary>
    public PlaySpot NearSpot { get { return Busy ? null : near; } }

    float scanLeft;
    PlaySpot[] all;

    // ★人と 話す・ねる ほうが スペースを つかう ときは 遊びを 止める（2026-08-17）。
    //   どちらも スペースなので、両方 反応すると 話しかけた とたん 水きりが 始まる
    [HideInInspector] public DayHost dayHost;
    // ★あそんだ ことを 日記に ためる
    [HideInInspector] public Nikki nikki;

    /// <summary>あそんだ ことを 日記に。**数字を 入れる**のが 肝。
    /// 「ばったを つかまえた」より「川で 水きりを した。8だん。じぶんでも おどろいたぜ」</summary>
    public void NoteAsobi(PlayKind k, int score) {
        if (nikki == null) return;
        string key = "asobi_" + k;
        string t;
        switch (k) {
            case PlayKind.Mizukiri:
                t = score >= 6 ? string.Format("川で 水きりを した。{0}だん。じぶんでも おどろいたぜ", score)
                               : string.Format("川で 水きりを した。{0}だん。まだまだ だな", score);
                break;
            case PlayKind.Tsuri:
                t = score > 0 ? string.Format("{0}cm の さかなを つった。にがして やったぜ", score)
                              : "つりを した。ぜんぜん かからなかった";
                break;
            case PlayKind.Sasabune:
                t = "ささぶねを ながした。見えなく なるまで 見て いた";
                break;
            case PlayKind.Hanatsumi:
                t = string.Format("花を {0}本 つんだ。だれに やろうかな", Mathf.Max(1, score));
                break;
            case PlayKind.Irozu:
                t = "花を もんで 色水を 作った。手が むらさきに なった";
                break;
            case PlayKind.Oshibana:
                t = "おし花に した。本に はさんで おいたぜ";
                break;
            case PlayKind.Himitsu:
                t = "やぶの 中に ひみつきちを 作った。だれにも 教えない";
                break;
            default: return;
        }
        // **はじめて やった 遊びは 重い**（80）。2回めからは 50
        bool first = !PlayerPrefs.HasKey("asobi1_" + k);
        if (first) PlayerPrefs.SetInt("asobi1_" + k, 1);
        nikki.Note(key, t, first ? 80 : 50);
    }

    void Update() {
        if (Busy) return;
        if (dayHost != null && dayHost.BlockPlay) { near = null; return; }
        // **場面ぜんぶを 毎フレーム さがさない。** 遊び場は 動かないので 1度 集めて おき、
        // 近さだけを ときどき 見る
        if (all == null) all = FindObjectsByType<PlaySpot>(FindObjectsSortMode.None);
        scanLeft -= Time.deltaTime;
        if (scanLeft > 0f) return;
        scanLeft = 0.15f;
        near = Nearest();
    }

    PlaySpot Nearest() {
        if (all == null) return null;
        PlaySpot best = null; float bd = float.MaxValue;
        foreach (var s in all) {
            if (s == null || !s.Near(transform)) continue;
            float d = (s.transform.position - transform.position).sqrMagnitude;
            if (d < bd) { bd = d; best = s; }
        }
        return best;
    }

    /// <summary>そばに 遊び場が あれば 始める。始めたら true</summary>
    public bool TryBegin() {
        if (Busy) return false;
        var s = Nearest();
        if (s == null) return false;
        running = StartCoroutine(Play(s));
        return true;
    }

    /// <summary>たしかめの 自動運転から。どの 遊びを 何回 押すか</summary>
    public bool DebugBegin(PlayKind kind) {
        if (Busy) return false;
        foreach (var s in FindObjectsByType<PlaySpot>(FindObjectsSortMode.None)) {
            if (s.kind != kind) continue;
            running = StartCoroutine(Play(s));
            return true;
        }
        Debug.Log("[PlayHost] 遊び場が ない: " + kind);
        return false;
    }
    bool debugPress;
    public void DebugPress() { debugPress = true; }
    // ★**自動運転は 上手に 遊ぶ。**
    //   ただ 一定間かくで 押させると、まぐれで 0点に なった のか
    //   しくみが 壊れて いるのか 区別が つかない。
    //   ねらい所で 押す ようにして、**うまく いった ときの 画**を 確かめる
    [HideInInspector] public bool debugAuto;
    public string DebugState {
        get {
            return string.Format("ふね={0} 水きり最高={1} さかな={2} 花={3} 色水={4} おし花={5} きち={6}"
                               + " えにっき={7}/31 金魚={8} 花火さいこう={9}",
                                 Boats, BestSkip, Fish, Flowers, Irozu, Oshibana, BaseStep,
                                 ShukudaiPages, KingyoTotal, HanabiBest);
        }
    }

    // 押しっぱなし。debugAuto の ときは 5びょう だけ 押しつづけた ことに する
    //（自動で 撮る ときに 線こう花火が 一しゅんで 終わって しまう ため）
    float heldUntil = -1f;
    bool Held() {
        if (debugAuto) {
            if (heldUntil < 0f) heldUntil = Time.time + 5f;
            if (Time.time < heldUntil) return true;
            heldUntil = -1f;
            return false;
        }
        return Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Return)
            || Input.GetKey(KeyCode.J) || Input.GetMouseButton(0);
    }

    bool Pressed() {
        if (debugPress) { debugPress = false; return true; }
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.J)
            || Input.GetMouseButtonDown(0);
    }

    IEnumerator Play(PlaySpot s) {
        if (move != null) move.locked = true;
        if (hud != null) hud.SetPrompt(null);
        switch (s.kind) {
            case PlayKind.Sasabune:  yield return Sasabune(s); break;
            case PlayKind.Mizukiri:  yield return Mizukiri(s); break;
            case PlayKind.Tsuri:     yield return Tsuri(s); break;
            case PlayKind.Hanatsumi: yield return Hanatsumi(s); break;
            case PlayKind.Irozu:     yield return Irozu2(s); break;
            case PlayKind.Oshibana:  yield return Oshibana2(s); break;
            case PlayKind.Shukudai:  yield return Shukudai(s); break;
            case PlayKind.Kingyo:    yield return Kingyo(s); break;
            case PlayKind.Hanabi:    yield return Hanabi(s); break;
            case PlayKind.Dagashi:   yield return Dagashi(s); break;
            default:                 yield return Himitsu(s); break;
        }
        ShowGauge(false);
        if (move != null) move.locked = false;
        running = null;
    }

    // ================= ささぶね =================
    // 作って ながす だけ。**むずかしさは 要らない。**
    // 見て いる あいだ 舟が 遠ざかって いく、それが この 遊びの ぜんぶ
    IEnumerator Sasabune(PlaySpot s) {
        Line("ささの 葉を 裂いて…");
        yield return new WaitForSeconds(0.9f);

        var boat = MakeIcon(IconBoat, 0.34f);
        boat.transform.position = s.water + Vector3.up * 0.04f;
        var dir = s.flow.sqrMagnitude > 0.01f ? s.flow.normalized : Vector3.forward;

        Bump(KeyBoat, 1);
        Line("ながれて いった　（つうさん " + Boats + " そう）");
        NoteAsobi(PlayKind.Sasabune, Boats);

        // 下って いく。ゆらぎながら 小さく なって 消える
        float t = 0f;
        while (t < 9f) {
            t += Time.deltaTime;
            boat.transform.position += dir * (0.9f + Mathf.Sin(t * 1.7f) * 0.12f) * Time.deltaTime
                                     + Vector3.right * Mathf.Sin(t * 2.3f) * 0.06f * Time.deltaTime;
            // 舟は そのまま 流す。人は とちゅうで 歩きだせる ように 早めに 手を はなす
            if (t > 1.6f) break;
            yield return null;
        }
        // **あとは 舟に まかせる。**川下に 見えなく なるまで ひとりでに 流れる
        var d = boat.AddComponent<Drifter>();
        d.dir = dir; d.speed = 0.95f; d.life = 14f;
        yield return new WaitForSeconds(0.3f);
    }

    // ================= 水きり =================
    // **2回 おす。** 1回めで 力、2回めで 角度。連打では どうにも ならない。
    // 角度は「ねらいの 帯」に 入れる。ひらたい 石を 低く 投げるのが こつ、
    // という 本ものの 感じを ゲージの あたりの 位置で 表す
    IEnumerator Mizukiri(PlaySpot s) {
        float power = 0f, angle = 0f;

        Line("ちからを ためる…　スペースで きめる");
        ShowGauge(true, 0f);
        float t = 0f;
        while (true) {
            t += Time.deltaTime * 1.15f;
            power = Mathf.PingPong(t, 1f);
            SetGauge(power, -1f);
            if (debugAuto ? power > 0.92f : Pressed()) break;
            yield return null;
        }
        yield return new WaitForSeconds(0.25f);

        // **ねらいの 帯は まん中より 下。** 石は 低く 投げる ほうが よく はねる
        const float Sweet = 0.34f, Width = 0.12f;
        Line("なげる 角ど　　ひくいほど よく はねる");
        t = 0f;
        while (true) {
            t += Time.deltaTime * 1.45f;
            angle = Mathf.PingPong(t, 1f);
            SetGauge(angle, Sweet);
            if (debugAuto ? Mathf.Abs(angle - Sweet) < 0.02f : Pressed()) break;
            yield return null;
        }
        ShowGauge(false);

        float miss = Mathf.Abs(angle - Sweet);
        float acc = Mathf.Clamp01(1f - miss / (Width * 3f));        // 1＝どんぴしゃ
        int skips = Mathf.RoundToInt(power * 7f * acc * acc);
        if (miss > Width * 3.2f) skips = 0;

        // 石を とばす。はねる ごとに 水しぶき
        var stone = MakeIcon(IconStone, 0.26f);
        var dir = s.flow.sqrMagnitude > 0.01f ? Vector3.Cross(Vector3.up, s.flow.normalized) : Vector3.forward;
        if (Vector3.Dot(dir, s.water - transform.position) < 0f) dir = -dir;
        Vector3 from = s.water + Vector3.up * 0.5f - dir * 0.6f;
        stone.transform.position = from;

        if (skips <= 0) {
            // ぼちゃん
            var to = from + dir * 1.6f;
            for (float k = 0f; k < 1f; k += Time.deltaTime * 2.6f) {
                stone.transform.position = Vector3.Lerp(from, to, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 0.5f - k * 0.5f);
                yield return null;
            }
            Splash(to, 0.9f);
            Line("ぼちゃん…　石が ひらたく なかった");
        } else {
            float hop = (s.span * 0.9f) / skips;
            Vector3 at = from;
            for (int i = 0; i < skips; i++) {
                var to = at + dir * hop;
                to.y = s.water.y;
                float h = Mathf.Lerp(0.55f, 0.12f, i / (float)Mathf.Max(1, skips - 1));
                for (float k = 0f; k < 1f; k += Time.deltaTime * 4.2f) {
                    stone.transform.position = Vector3.Lerp(at, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * h;
                    yield return null;
                }
                Splash(to, 0.5f);
                Line((i + 1) + " だん！");
                at = to;
            }
            if (skips > BestSkip) {
                PlayerPrefs.SetInt(KeySkip, skips); PlayerPrefs.Save();
                Mood(CharSprite.Pose.Yorokobi, 1.8f);
                Line(skips + " だん！　★じこ さいこう きろく");
                NoteAsobi(PlayKind.Mizukiri, skips);
            } else Line(skips + " だん　（さいこう " + BestSkip + "）");
            NoteAsobi(PlayKind.Mizukiri, skips);
        }
        Destroy(stone, 0.6f);
        yield return new WaitForSeconds(1.1f);
    }

    // ================= つり =================
    // **待つ 遊び。** うきが しずんだ 合図の あとの ひと呼吸で あわせる。
    // 早おしは 空ぶり＝連打では 取れない
    IEnumerator Tsuri(PlaySpot s) {
        Line("いとを たらした…");
        // **4m 先の 水面に 置くので 小さいと ただの 点。** あたりが 読めない
        var uki = MakeIcon(IconFloat, 0.46f);
        uki.transform.position = s.water + Vector3.up * 0.12f;

        float wait = Random.Range(2.2f, 6.5f);
        float t = 0f;
        bool early = false;
        while (t < wait) {
            t += Time.deltaTime;
            var p = s.water + Vector3.up * (0.12f + Mathf.Sin(t * 2.4f) * 0.03f);
            uki.transform.position = p;
            // ときどき 前ぶれ（さそいの あたり）。ここで 押すと 空ぶり
            if (!debugAuto && Pressed()) { early = true; break; }
            yield return null;
        }
        if (early) {
            Line("はやい！　まだ かかって いない");
            Destroy(uki); yield return new WaitForSeconds(1.0f); yield break;
        }

        // ★あたり。うきが しずむ
        Line("！");
        Splash(s.water, 0.4f);
        float window = 0.85f;
        bool hooked = false;
        for (float k = 0f; k < window; k += Time.deltaTime) {
            uki.transform.position = s.water + Vector3.up * (0.12f - k * 0.5f);
            if (debugAuto ? k > 0.15f : Pressed()) { hooked = true; break; }
            yield return null;
        }
        Destroy(uki);
        if (!hooked) {
            Line("にげられた…");
            NoteAsobi(PlayKind.Tsuri, 0);
            Mood(CharSprite.Pose.Kanashimi, 1.6f);
            yield return new WaitForSeconds(1.1f); yield break;
        }

        // かかった。引きあげる
        var fishName = FishNames[Random.Range(0, FishNames.Length)];
        int mm = Random.Range(70, 240);
        var fish = MakeIcon(IconFish, 0.34f + mm / 900f);
        Vector3 a = s.water, b = transform.position + Vector3.up * 1.1f;
        for (float k = 0f; k < 1f; k += Time.deltaTime * 1.8f) {
            fish.transform.position = Vector3.Lerp(a, b, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.6f;
            yield return null;
        }
        Bump(KeyFish, 1);
        Mood(CharSprite.Pose.Yorokobi, 1.6f);
        Line(fishName + "　" + mm + "mm　が つれた！　（つうさん " + Fish + " ひき）");
        NoteAsobi(PlayKind.Tsuri, Mathf.RoundToInt(mm / 10f));
        // ★**大物は お金に なる。**（2026-08-17）
        //   おじさんに 見せると 小づかいを くれる、という 建てつけ。
        //   谷の おくの 駄菓子屋まで 歩く 理由は、これと 虫ずもうの 勝ちで つくる
        // さかなは 70〜240mm。**200mm(20cm)から 大物** あつかい に する
        if (mm >= 200) {
            int en = 20 + (mm - 200) / 10 * 5;
            Saifu.Add(en);
            Line(string.Format("おじさんが 見て おどろいて いた。……小づかいを {0}円 くれたぜ", en));
            yield return new WaitForSeconds(1.6f);
        }
        Destroy(fish, 1.2f);
        yield return new WaitForSeconds(1.3f);
    }

    static readonly string[] FishNames = { "おいかわ", "かわむつ", "うぐい", "どじょう", "ふな", "やまめ" };

    // ================= 花つみ =================
    IEnumerator Hanatsumi(PlaySpot s) {
        Line("しゃがんで 花を つんだ");
        var f = MakeIcon(IconFlower, 0.30f);
        f.transform.position = transform.position + Vector3.up * 0.5f;
        for (float k = 0f; k < 1f; k += Time.deltaTime * 1.6f) {
            f.transform.position = transform.position + Vector3.up * (0.5f + k * 0.7f);
            yield return null;
        }
        Destroy(f);
        int n = Random.Range(1, 4);
        Bump(KeyFlow, n);
        Line("はなを " + n + "本 つんだ　（もちもの " + Flowers + "本）");
        NoteAsobi(PlayKind.Hanatsumi, Flowers);
        yield return new WaitForSeconds(1.0f);
    }

    // ================= 色水 =================
    IEnumerator Irozu2(PlaySpot s) {
        if (Flowers < 3) { Line("花が たりない（3本 いる）"); yield return new WaitForSeconds(1.2f); yield break; }
        Line("花を もんで…");
        yield return new WaitForSeconds(1.0f);
        PlayerPrefs.SetInt(KeyFlow, Flowers - 3);
        Bump(KeyIrozu, 1);
        var col = IrozuNames[Random.Range(0, IrozuNames.Length)];
        Line(col + "の 色水が できた　（つうさん " + Irozu + " ぱい）");
        NoteAsobi(PlayKind.Irozu, Irozu);
        yield return new WaitForSeconds(1.4f);
    }
    static readonly string[] IrozuNames = { "むらさき", "うすあか", "あお", "きいろ", "ももいろ" };

    // ================= おし花 =================
    IEnumerator Oshibana2(PlaySpot s) {
        if (Flowers < 1) { Line("花を もって いない"); yield return new WaitForSeconds(1.2f); yield break; }
        Line("本に はさんだ。かわくまで しばらく…");
        yield return new WaitForSeconds(1.3f);
        PlayerPrefs.SetInt(KeyFlow, Flowers - 1);
        Bump(KeyOshi, 1);
        Line("おし花が " + Oshibana + " まいに なった");
        NoteAsobi(PlayKind.Oshibana, Oshibana);
        yield return new WaitForSeconds(1.2f);
    }


    // ================= 絵日記（宿題） =================
    // ★遊ぶ 人からの 言：「**毎日 ちょっとずつ 進む ものが 1本も 無い。**
    //   ぼくなつには 絵日記が あった。毎日 1ページ、31ページ 埋まって いく。
    //   あれが『積み重ね』の 目に 見える 形。25日の『宿題は やったのかい』が
    //   ただの 脅し文句で 終わらない ために」
    //
    // **夜、机に むかうと 1ページ 書ける。1日 1ページまで。**
    // ためると 31日目に 地ごくを 見る＝のこり日数が 急に こわく なる
    // ★中みは Nikki が もつ（読み返せる ように する ため）。ここは 遊びの 手つづき だけ
    public int ShukudaiPages { get { return nikki != null ? nikki.EnikkiMai : 0; } }

    IEnumerator Shukudai(PlaySpot s) {
        if (nikki == null) yield break;
        if (nikki.EnikkiKyou) {
            Line("きょうの ぶんは もう 書いた");
            yield return new WaitForSeconds(1.3f); yield break;
        }
        if (nikki.EnikkiMai >= Nikki.EnikkiZen) {
            Line("えにっきは ぜんぶ 書きおわって いる。えらいぜ");
            yield return new WaitForSeconds(1.4f); yield break;
        }
        Line("えんぴつを けずって…");
        yield return new WaitForSeconds(1.0f);
        // **その日 やった ことが そのまま 絵日記に なる**（日記と 同じ たね）
        nikki.EnikkiKaku();
        Line("きょうの ことを 書いた…");
        yield return new WaitForSeconds(1.2f);
        int pages = nikki.EnikkiMai;
        int nokori = Nikki.EnikkiZen - pages;
        Line(string.Format("えにっき {0}/{1} まい　（のこり {2}まい）", pages, Nikki.EnikkiZen, nokori));
        // **ためて いる ほど 重い**（あとに なるほど 書いた ことが 大事に なる）
        int okure = Mathf.Max(0, nikki.day - pages);
        nikki.Note("shukudai", okure > 5
            ? string.Format("えにっきを 書いた（{0}/{1}）。……{2}日ぶん たまって いる。まずいぜ",
                            pages, Nikki.EnikkiZen, okure)
            : string.Format("えにっきを 書いた（{0}/{1}）。きょうの ぶんは かたづいた",
                            pages, Nikki.EnikkiZen),
            okure > 5 ? 75 : 55);
        yield return new WaitForSeconds(1.5f);
    }

    // ================= 金魚すくい（祭りの 屋台） =================
    // つりと 同じ「間」の 遊び。**紙は すぐ やぶれる**ので、欲ばると 0びき
    const string KeyKingyo = "natsuyasumi.play.kingyo.v1";
    public int KingyoTotal { get { return PlayerPrefs.GetInt(KeyKingyo, 0); } }

    IEnumerator Kingyo(PlaySpot s) {
        Line("かみの ポイを もらった。やぶれるまで すくえるぜ");
        yield return new WaitForSeconds(1.3f);
        int got = 0;
        float yowasa = 0f;                  // 紙の いたみ 0〜1
        for (int i = 0; i < 8; i++) {
            Line("金魚が 近づいて きた…　スペースで すくう");
            float w = 0f; bool did = false;
            // **来る 間が まちまち。**待てば 待つほど すくいやすいが、紙は 待つと ふやける
            float best = Random.Range(0.5f, 1.4f);
            while (w < best + 0.9f) {
                w += Time.deltaTime;
                if (Pressed()) { did = true; break; }
                yield return null;
            }
            if (!did) { Line("にげられた"); yield return new WaitForSeconds(0.8f); continue; }
            float zure = Mathf.Abs(w - best);
            yowasa += 0.16f + zure * 0.5f;          // 早すぎ・遅すぎ ほど 紙が いたむ
            if (zure < 0.30f) {
                got++;
                Line(string.Format("すくえた！　{0}ひきめ", got));
            } else {
                Line("あっ、すりぬけた");
            }
            yield return new WaitForSeconds(0.9f);
            if (yowasa >= 1f) { Line("ポイが やぶれた。おしまい"); break; }
        }
        Bump(KeyKingyo, got);
        Line(got > 0 ? string.Format("金魚を {0}ひき すくった　（つうさん {1}ひき）", got, KingyoTotal)
                     : "1ぴきも すくえなかった…");
        if (nikki != null)
            nikki.Note("kingyo", got > 0
                ? string.Format("祭りで 金魚を {0}ひき すくった。ポイが やぶれるまで やったぜ", got)
                : "祭りで 金魚すくいを した。1ぴきも すくえなかった。くやしいぜ", 100);
        yield return new WaitForSeconds(1.6f);
    }

    // ================= 線香花火（夜の 縁側） =================
    // **押しつづけると 玉が 育ち、離すと 落ちる。**欲ばると 落ちる、が 全部
    const string KeyHanabi = "natsuyasumi.play.hanabi.v1";
    public int HanabiBest { get { return PlayerPrefs.GetInt(KeyHanabi, 0); } }

    IEnumerator Hanabi(PlaySpot s) {
        Line("線こう花火に 火を つけた…");
        // 手もとの 位置（人の むね の 前あたり）
        // **人に ついて まわる。**その場の 座標に 置くと、立ち位置が すこし ちがう だけで
        // 玉が 縁側から はみ出して 庭の 草の 上で 光る
        System.Func<Vector3> Te = () => transform.position
                   + Vector3.up * 0.70f + Vector3.forward * 0.30f + Vector3.right * 0.30f;
        Vector3 te = Te();
        var bo = Tama(new Color(0.75f, 0.62f, 0.42f, 0.9f), 0.05f, 0f);   // こより（軸）
        bo.transform.position = Te() + Vector3.up * 0.16f;
        bo.transform.localScale = new Vector3(0.03f, 0.34f, 1f);
        Destroy(bo.GetComponent<Billboard>());
        yield return new WaitForSeconds(1.2f);
        Line("スペースを おしつづける　（はなすと 玉が おちる）");

        // ★**まず「押しはじめる」のを 待つ。**（2026-08-17）
        //   いきなり 押しっぱなし 判定に 入れると、まだ 手を 出して いない うちに
        //   「はなした」と みなされ、0.4びょうで 終わって しまう
        float matsu = 0f;
        while (!Held() && matsu < 6f) { matsu += Time.deltaTime; yield return null; }
        if (matsu >= 6f) {
            Line("……火が きえて しまった");
            Destroy(bo);
            yield return new WaitForSeconds(1.4f);
            yield break;
        }

        var tamaGO = Tama(new Color(1f, 0.72f, 0.30f, 1f), 0.10f, 0.9f);
        tamaGO.transform.position = Te();
        var hibana = new System.Collections.Generic.List<GameObject>();
        var muki = new System.Collections.Generic.List<Vector3>();

        float tama = 0f;
        bool fell = false;
        int saigo = -1;                       // さいごに 出した ぱちぱちの 目もり
        float tsugi = 0f;                     // つぎに 火花を 散らす 時こく
        // だんだん 落ちやすく なる。**長く もたせるほど えらい**
        while (Held()) {
            tama += Time.deltaTime;
            // 玉は すこしずつ 育ち、光も 強く なる
            tamaGO.transform.position = Te();
            if (bo != null) bo.transform.position = Te() + Vector3.up * 0.16f;
            float f = Mathf.Min(tama, 6f) / 6f;
            tamaGO.transform.localScale = Vector3.one * (0.10f + f * 0.10f);
            var lt0 = tamaGO.GetComponentInChildren<Light>();
            if (lt0 != null) lt0.intensity = 0.9f + f * 1.1f + Mathf.Sin(tama * 22f) * 0.22f;

            // 火花。**数を しぼって、短い いのちで 散らす**
            if (tama > 0.5f && Time.time >= tsugi) {
                tsugi = Time.time + 0.055f;
                var h = Tama(new Color(1f, 0.84f, 0.50f, 1f), 0.040f, 0f);
                h.transform.position = tamaGO.transform.position;
                hibana.Add(h);
                var v = Random.onUnitSphere; v.y = Mathf.Abs(v.y) * 0.6f + 0.15f;
                muki.Add(v * Random.Range(0.9f, 2.0f));
            }
            for (int i = hibana.Count - 1; i >= 0; i--) {
                if (hibana[i] == null) { hibana.RemoveAt(i); muki.RemoveAt(i); continue; }
                var v = muki[i];
                v.y -= 5.0f * Time.deltaTime;                       // おちて いく
                muki[i] = v;
                hibana[i].transform.position += v * Time.deltaTime;
                var sc = hibana[i].transform.localScale.x - Time.deltaTime * 0.030f;
                if (sc <= 0.002f || hibana[i].transform.position.y < Te().y - 0.9f) {
                    Destroy(hibana[i]); hibana.RemoveAt(i); muki.RemoveAt(i);
                } else hibana[i].transform.localScale = Vector3.one * sc;
            }

            // 落ちる 見こみは 時間が たつほど 上がる
            if (tama > 2.0f && Random.value < (tama - 2.0f) * 0.012f) { fell = true; break; }
            // **毎フレーム 言わない。**1びょう おきに 1回だけ 出す
            int me = Mathf.FloorToInt(tama);
            if (me >= 1 && me != saigo) { saigo = me; Line(string.Format("ぱちぱち…　{0}びょう", me)); }
            yield return null;
        }

        // 玉が おちる
        var ochi = tamaGO;
        float oy = 0f, ot = 0f;
        while (ot < 1.1f && ochi != null) {
            ot += Time.deltaTime; oy -= 3.4f * Time.deltaTime;
            ochi.transform.position += Vector3.up * oy * Time.deltaTime;
            var l2 = ochi.GetComponentInChildren<Light>();
            if (l2 != null) l2.intensity = Mathf.Max(0f, 3f * (1f - ot / 1.1f));
            ochi.transform.localScale = Vector3.one * Mathf.Max(0.005f, 0.18f * (1f - ot / 1.1f));
            yield return null;
        }
        foreach (var h in hibana) if (h != null) Destroy(h);
        if (tamaGO != null) Destroy(tamaGO);
        if (bo != null) Destroy(bo);

        int mm = Mathf.RoundToInt(tama * 10f);
        bool best = mm > HanabiBest;
        if (fell) Line(string.Format("ぽとり。……{0:F1}びょう だった", tama));
        else      Line(string.Format("そっと はなした。{0:F1}びょう もったぜ{1}", tama,
                                     best ? "　（さいこう きろく！）" : ""));
        if (best) { PlayerPrefs.SetInt(KeyHanabi, mm); PlayerPrefs.Save(); }
        if (nikki != null)
            nikki.Note("hanabi", string.Format("縁側で 線こう花火を した。{0:F1}びょう もった。{1}",
                       tama, tama > 4f ? "われながら 見ごとだぜ" : "すぐ 落ちて しまった"),
                       tama > 4f ? 80 : 55);
        yield return new WaitForSeconds(1.4f);
    }

    // ================= 駄菓子屋 =================
    // ★遊ぶ 人：「歩いて 15分 かかる 店に、300円 持って 行く——あの 遠さが 子どもの 夏」
    //   **かごが 5ひきで 詰まる 痛みが、ここへ 歩く 理由に なる。**
    //   だから 一番 高くて 一番 うれしいのが「大きい 虫かご」。
    // ★**ひとことの わくは 340px しか ない。**長い 説明を 1行に つめると
    //   枠から はみ出す（実機で 見て わかった）。品名は 短く、説明は 別の 行に する
    static readonly string[] ShinaNa = { "大きい むしかご", "ラムネ", "アイス" };
    static readonly string[] ShinaSetsu = { "かごが 2ひき ふえる", "きゅっと 冷たい", "あたま きーん" };
    static readonly int[] ShinaNe = { 120, 30, 50 };

    IEnumerator Dagashi(PlaySpot s) {
        var book = GetComponent<BugBook>();
        int sel = 0;
        Line(string.Format("いらっしゃい。お金は {0}円 だね", Saifu.Yen));
        yield return new WaitForSeconds(1.4f);
        Line("← → えらぶ　スペース：買う　X：やめる");
        yield return new WaitForSeconds(1.6f);
        float nokori = 22f;                       // ながく のぞいて いると 帰る
        while (nokori > 0f) {
            nokori -= Time.deltaTime;
            Line(string.Format("▶{0}　{1}円　（{2}）", ShinaNa[sel], ShinaNe[sel], ShinaSetsu[sel]));
            if (Input.GetKeyDown(KeyCode.RightArrow)) { sel = (sel + 1) % ShinaNa.Length; }
            else if (Input.GetKeyDown(KeyCode.LeftArrow)) { sel = (sel + ShinaNa.Length - 1) % ShinaNa.Length; }
            else if (Input.GetKeyDown(KeyCode.X)) break;
            else if (Pressed()) {
                if (!Saifu.Tsukau(ShinaNe[sel])) {
                    Line("……お金が 足りないぜ");
                    yield return new WaitForSeconds(1.4f);
                    continue;
                }
                switch (sel) {
                    case 0:
                        if (book != null) book.CageUp(2);
                        Line(string.Format("大きい むしかごを 買った！　これで {0}ひき 入る",
                                           book != null ? book.CageMax : 0));
                        if (nikki != null) nikki.Note("dagashi",
                            "町の 駄がし屋で 大きい むしかごを 買った。これで もっと 持って 帰れる", 85);
                        break;
                    case 1:
                        Line("ラムネを 飲んだ。……ビー玉が じゃまだぜ");
                        if (nikki != null) nikki.Note("dagashi",
                            "駄がし屋で ラムネを 飲んだ。ビー玉が どうしても 出てこない", 55);
                        break;
                    default:
                        Line("アイスを 食べた。あたまが きーんと する");
                        if (nikki != null) nikki.Note("dagashi",
                            "駄がし屋で アイスを 食べた。帰り道で とけて 手が べたべたに なった", 55);
                        break;
                }
                yield return new WaitForSeconds(1.8f);
                Line(string.Format("のこりは {0}円", Saifu.Yen));
                yield return new WaitForSeconds(1.2f);
            }
            yield return null;
        }
        Line("またね");
        yield return new WaitForSeconds(1.0f);
    }

    // ================= ひみつきち =================
    // 一気には できない。**来る たびに すこしずつ 建つ**のが この 遊びの 芯
    static readonly string[] BaseSteps = {
        "やぶを かき分けて 場所を 作った",
        "えだを ならべて かべに した",
        "板きれを わたして 屋根に した",
        "木の 箱を もちこんで つくえに した",
        "はたを 立てた。もう だれにも 見つからない",
    };
    IEnumerator Himitsu(PlaySpot s) {
        int step = BaseStep;
        if (step >= BaseSteps.Length) {
            Line("ひみつきちは できあがって いる");
            yield return new WaitForSeconds(1.2f); yield break;
        }
        Line("…");
        var e = MakeIcon(IconBranch, 0.40f);
        e.transform.position = s.transform.position + Vector3.up * 1.2f;
        for (float k = 0f; k < 1f; k += Time.deltaTime * 1.2f) {
            e.transform.position = s.transform.position + Vector3.up * (1.2f - k * 0.9f);
            yield return null;
        }
        Destroy(e);
        PlayerPrefs.SetInt(KeyBase, step + 1); PlayerPrefs.Save();
        // **建った ぶんが その場に のこる。**数が 増えるだけでは 通う 理由に ならない
        var hb = s.GetComponentInChildren<HimitsuBase>();
        if (hb == null) hb = FindFirstObjectByType<HimitsuBase>();
        if (hb != null) hb.Show(step + 1);
        Line(BaseSteps[step] + "　（" + (step + 1) + "/" + BaseSteps.Length + "）");
        if (nikki != null) nikki.Note("himitsu", "やぶの 中の ひみつきちが すすんだ（" + (step + 1) + "/" + BaseSteps.Length + "）。だれにも 教えない", 80);
        yield return new WaitForSeconds(1.6f);
    }

    // ---- 見た目の 部品
    GameObject MakeIcon(int index, float size) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "PlayIcon";
        Destroy(go.GetComponent<Collider>());
        go.transform.localScale = new Vector3(size, size, 1f);
        go.AddComponent<Billboard>();
        var r = go.GetComponent<Renderer>();
        var sh = Shader.Find("Natsuyasumi/PixelSprite") ?? Shader.Find("Sprites/Default");
        var m = new Material(sh);
        if (atlas != null) {
            m.SetTexture("_BaseMap", atlas);
            m.mainTexture = atlas;
            var st = new Vector4(1f / Cols, 1f, index / (float)Cols, 0f);
            m.SetVector("_BaseMap_ST", st);
            m.SetTextureScale("_BaseMap", new Vector2(st.x, st.y));
            m.SetTextureOffset("_BaseMap", new Vector2(st.z, st.w));
        }
        if (m.HasProperty("_BreatheAmp")) m.SetFloat("_BreatheAmp", 0f);
        if (m.HasProperty("_SwayAmp")) m.SetFloat("_SwayAmp", 0f);
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    // ★**「ぱちぱち…3びょう」と 字が 出るだけでは 花火に ならない。**（2026-08-17）
    //   手もとに **光る 玉**を 出し、玉から 火花を 散らす。玉は 育ち、はなすと 落ちる。
    //   絵を 用意しなくても、光る 点と 落ちる 点だけで あの 遊びは 成り立つ
    GameObject Tama(Color c, float size, float hikari) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "Hibana";
        Destroy(go.GetComponent<Collider>());
        go.transform.localScale = Vector3.one * size;
        go.AddComponent<Billboard>();
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m.color = c;
        m.SetFloat("_Surface", 1f); m.renderQueue = 3100;
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        m.SetFloat("_ZWrite", 0f);
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        if (hikari > 0f) {
            var lg = new GameObject("Hi");
            lg.transform.SetParent(go.transform, false);
            var lt = lg.AddComponent<Light>();
            lt.type = LightType.Point; lt.color = new Color(1f, 0.78f, 0.42f);
            lt.intensity = hikari; lt.range = 3.2f; lt.shadows = LightShadows.None;
        }
        return go;
    }

    void Splash(Vector3 at, float scale) {
        var go = new GameObject("Splash");
        go.transform.position = at;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.5f * scale + 0.2f; main.startSpeed = 1.8f * scale;
        main.startSize = 0.07f * scale + 0.03f;
        main.maxParticles = 30; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 1.6f;
        main.startColor = new Color(0.85f, 0.94f, 1f, 0.95f);
        var em = ps.emission; em.enabled = true; em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.RoundToInt(10 * scale + 4)) });
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.08f;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = SplashMat();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Destroy(go, 1.4f);
    }

    // 水しぶきの 材質も 使いまわす（水きりは 1回で 何度も はねる）
    static Material splashMat;
    static Material SplashMat() {
        if (splashMat != null) return splashMat;
        splashMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")
                                 ?? Shader.Find("Sprites/Default"));
        splashMat.SetFloat("_Surface", 1);
        return splashMat;
    }

    void Line(string s) { if (hud != null) hud.Say(s); if (uiLine != null) uiLine.text = s; }

    void ShowGauge(bool on, float v = 0f) {
        if (ui == null) return;
        ui.gameObject.SetActive(on);
        if (on) SetGauge(v, -1f);
    }

    void SetGauge(float v, float mark) {
        if (gaugeFill == null) return;
        gaugeFill.rectTransform.sizeDelta = new Vector2(Mathf.Clamp01(v) * 220f, 10f);
        if (gaugeMark != null) {
            bool on = mark >= 0f;
            if (gaugeMark.gameObject.activeSelf != on) gaugeMark.gameObject.SetActive(on);
            if (on) gaugeMark.rectTransform.anchoredPosition = new Vector2(mark * 220f, 0f);
        }
    }

    void BuildUI() {
        var host = hud != null && hud.CanvasRoot != null ? hud.CanvasRoot : null;
        if (host == null) return;

        var go = new GameObject("PlayUI", typeof(Image));
        go.transform.SetParent(host, false);
        ui = go.GetComponent<RectTransform>();
        ui.anchorMin = ui.anchorMax = ui.pivot = new Vector2(0.5f, 0f);
        ui.anchoredPosition = new Vector2(0f, 96f);
        ui.sizeDelta = new Vector2(250f, 46f);
        var bg = go.GetComponent<Image>();
        bg.sprite = panel; bg.type = Image.Type.Sliced; bg.raycastTarget = false;
        if (panel == null) bg.color = new Color(0.12f, 0.10f, 0.09f, 0.88f);

        var back = new GameObject("Back", typeof(Image));
        back.transform.SetParent(ui, false);
        var brt = back.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(220f, 10f);
        brt.anchoredPosition = new Vector2(0f, -6f);
        gaugeBack = back.GetComponent<Image>();
        gaugeBack.color = new Color(1f, 1f, 1f, 0.16f); gaugeBack.raycastTarget = false;

        var fill = new GameObject("Fill", typeof(Image));
        fill.transform.SetParent(back.transform, false);
        var frt = fill.GetComponent<RectTransform>();
        frt.anchorMin = frt.anchorMax = new Vector2(0f, 0.5f);
        frt.pivot = new Vector2(0f, 0.5f);
        frt.anchoredPosition = Vector2.zero; frt.sizeDelta = new Vector2(0f, 10f);
        gaugeFill = fill.GetComponent<Image>();
        gaugeFill.color = new Color(0.56f, 0.86f, 0.35f); gaugeFill.raycastTarget = false;

        // ねらいの 帯（水きりの 角ど）
        var mk = new GameObject("Mark", typeof(Image));
        mk.transform.SetParent(back.transform, false);
        var mrt = mk.GetComponent<RectTransform>();
        mrt.anchorMin = mrt.anchorMax = new Vector2(0f, 0.5f);
        mrt.pivot = new Vector2(0.5f, 0.5f);
        mrt.sizeDelta = new Vector2(26f, 16f);
        gaugeMark = mk.GetComponent<Image>();
        gaugeMark.color = new Color(1f, 0.85f, 0.35f, 0.45f); gaugeMark.raycastTarget = false;
        mk.SetActive(false);

        var tx = new GameObject("Line", typeof(Text));
        tx.transform.SetParent(ui, false);
        var trt = tx.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(8f, 16f); trt.offsetMax = new Vector2(-8f, -6f);
        uiLine = tx.GetComponent<Text>();
        uiLine.font = font; uiLine.fontSize = 12;
        uiLine.alignment = TextAnchor.MiddleCenter;
        uiLine.color = new Color(0.96f, 0.95f, 0.88f);
        uiLine.horizontalOverflow = HorizontalWrapMode.Overflow;
        uiLine.verticalOverflow = VerticalWrapMode.Overflow;
        uiLine.raycastTarget = false; uiLine.supportRichText = false;

        ui.gameObject.SetActive(false);
    }
}
