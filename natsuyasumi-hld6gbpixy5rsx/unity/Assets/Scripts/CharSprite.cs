using UnityEngine;

// 8方向 x 8状態の 絵から、いまの 1コマを えらぶ。
//
// 絵の ならび（本人が 用意した とおり）
//   列 = 0:正面 1:左ななめ前 2:左 3:左ななめ奥 4:奥 5:右ななめ奥 6:右 7:右ななめ前
//   行 = 0:立ち 1:歩き 2:走り 3:喜 4:怒 5:哀 6:楽 7:目を とじた
//
// ★向きは **カメラから 見た 向き**で 決める。
//   世界の 向きで 決めると、高台や 川べりで カメラが 回りこんだ とたん
//   「画面の 右へ 歩いて いるのに 左を 向いた 絵」に なる。
//
// ★歩き・走りは **1コマ ずつ しか ない**ので、立ちと 交ごに 出して 2コマの
//   アニメに する。走りは 速く 切りかえる。昔の ドット絵と 同じ やりかた。
public class CharSprite : MonoBehaviour {
    public const int Cols = 8, Rows = 8;

    public enum Pose { Tachi = 0, Aruki = 1, Hashiri = 2, Yorokobi = 3, Ikari = 4, Kanashimi = 5, Tanoshii = 6, Meturi = 7 }

    [Header("つなぐ もの")]
    public Renderer target;             // 板の 見た目
    [Tooltip("空なら Camera.main を つかう")]
    public Camera cam;

    [Header("うごき")]
    public float walkFps = 5.0f;
    public float runFps = 8.0f;
    [Tooltip("これより 速ければ 走りの 絵")]
    public float runSpeed = 3.4f;

    [Header("まばたき")]
    public bool blink = true;
    public float blinkEvery = 4.2f;
    public float blinkHold = 0.13f;

    MaterialPropertyBlock mpb;
    int dir = 0;                        // いまの 向き（止まっても 保つ）
    float step;                         // 歩きの きざみ
    float blinkLeft, blinkT = -1f;
    float moodLeft;
    Pose mood = Pose.Tachi;
    int lastCell = -1;

    void Awake() {
        if (target == null) target = GetComponentInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();
        blinkLeft = Random.Range(1f, blinkEvery);
    }

    /// <summary>いまの 向き（0〜7）</summary>
    public int Dir { get { return dir; } }
    /// <summary>画面の 右を 向いて いるか（あみの ふり むきに つかう）</summary>
    public bool FacingRight { get { return dir >= 5 && dir <= 7; } }
    /// <summary>奥か 手前を 向いて いる（＝あみは たて振り）</summary>
    public bool FacingDepth { get { return dir == 0 || dir == 4; } }
    /// <summary>おく（画面の 奥）を 向いて いる。あみを 手前から 奥へ ふる</summary>
    public bool FacingAway { get { return dir == 4; } }

    /// <summary>喜・怒・哀・楽 を しばらく 出す</summary>
    public void ShowMood(Pose p, float seconds) {
        mood = p; moodLeft = seconds;
    }

    // たしかめの 自動運転から。**8方向 ぜんぶを 人手なしで 見くらべる** ため
    [HideInInspector] public bool debugCell;
    [HideInInspector] public int debugDir, debugRow;

    /// <summary>毎フレーム PlayerMove から 呼ぶ。wish は 世界での 進みたい むき</summary>
    public void Drive(Vector3 wish, float speed, bool locked) {
        if (debugCell) { dir = debugDir; Set(debugDir, debugRow); return; }
        var c = cam != null ? cam : Camera.main;
        bool moving = wish.sqrMagnitude > 1e-4f && !locked;

        if (moving && c != null) {
            // **カメラ基準の 角度**に 直す。奥(0度)を 4番、手前(180度)を 0番に
            var f = c.transform.forward; f.y = 0f; f.Normalize();
            var r = c.transform.right;   r.y = 0f; r.Normalize();
            float a = Mathf.Atan2(Vector3.Dot(wish, r), Vector3.Dot(wish, f)) * Mathf.Rad2Deg;
            dir = ((Mathf.RoundToInt(a / 45f) + 4) % 8 + 8) % 8;
        }

        float dt = Time.deltaTime;
        if (moodLeft > 0f) moodLeft -= dt;

        // まばたき（止まって いる ときだけ。走りながら 目を つぶらない）
        if (blink && !moving) {
            blinkLeft -= dt;
            if (blinkLeft <= 0f) { blinkT = blinkHold; blinkLeft = blinkEvery + Random.Range(-1f, 1.5f); }
        }
        if (blinkT > 0f) blinkT -= dt;

        int row;
        if (moodLeft > 0f) {
            row = (int)mood;
        } else if (moving) {
            bool run = speed > runSpeed;
            step += dt * (run ? runFps : walkFps);
            // 立ち と 歩き（走り）を 交ごに＝2コマの あし どり
            row = (Mathf.FloorToInt(step) % 2 == 0)
                ? (run ? (int)Pose.Hashiri : (int)Pose.Aruki)
                : (int)Pose.Tachi;
        } else {
            step = 0f;
            row = blinkT > 0f ? (int)Pose.Meturi : (int)Pose.Tachi;
        }

        Set(dir, row);
    }

    void Set(int col, int row) {
        if (target == null) return;
        int cell = row * Cols + col;
        if (cell == lastCell) return;      // 同じ コマなら 触らない
        lastCell = cell;
        target.GetPropertyBlock(mpb);

        // ★**コマの ふちを 半テクセル 内がわへ 寄せる。**（2026-08-16・本人
        //   「斜め左奥に走ると、別の画像の一部が出てくる」）
        //   1コマ ＝ 1/8 ちょうど で 切って いたので、はしの 1列で 計算の
        //   まるめが となりの コマに はみ出し、**べつの 向きの 絵が すじに なって 出て いた**。
        //   1コマ 115x167px に たいして 半テクセルなので 見た目は 変わらない。
        //   ★アトラスを 手で 切るなら **どこでも 起きる**。コマを 足す ときは 必ず これ
        var t = target.sharedMaterial != null ? target.sharedMaterial.mainTexture : null;
        float w = t != null ? t.width : 1024f, h = t != null ? t.height : 1024f;
        float insetU = 0.5f / Mathf.Max(w, 1f), insetV = 0.5f / Mathf.Max(h, 1f);

        // 画像は 上が 0行め だが UV は 下が 0。y は ひっくり返す
        mpb.SetVector("_BaseMap_ST", new Vector4(
            1f / Cols - insetU * 2f, 1f / Rows - insetV * 2f,
            col / (float)Cols + insetU, (Rows - 1 - row) / (float)Rows + insetV));
        target.SetPropertyBlock(mpb);
    }
}
