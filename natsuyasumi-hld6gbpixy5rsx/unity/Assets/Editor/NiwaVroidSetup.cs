using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// VRoid の モデルを 場面に 立てる（見くらべ用・2026-09-06）。
///
/// 本人「Vroidを今のキャラの真横とかに乗せて比較できるようにして」。
/// **`-vroid` を つけた ときだけ 出る。**ふだんの 絵づくりの じゃまを しない。
///
/// ★モデル（`.vrm`）は **リポジトリに 入れて いない**（VRoid の サンプル・本番では 差しかわる）。
///   無ければ 何も 置かずに 進む。ここで 止めない。
/// ★アニメの すじみち（AnimatorController）も **コードで 作る**（GUI は 触らない）。
///   遷移は 作らず、3つの 状態を 並べて `CrossFade` で 切りかえる（`NiwaVroid`）。
/// </summary>
public static class NiwaVroidSetup {
    const string VRM = "Assets/Art/Models/vroid/AvatarSample_A.vrm";
    const string FBX = "Assets/Art/Models/anim/AnimationLibrary_Unity_Standard.fbx";
    const string CTRL = "Assets/Art/Materials/Niwa/NiwaVroid.controller";

    /// <summary>板の 絵の 見た たけ（m）。1.40m の 板に 312/336 だけ 絵が ある</summary>
    const float MITAKE = 1.40f * 312f / 336f;

    public static GameObject Tateru(Transform oya, Transform player) {
        var pf = AssetDatabase.LoadAssetAtPath<GameObject>(VRM);
        if (pf == null) {
            Debug.Log("[Probe] NiwaVroid: モデルが 無い ので 置かない (" + VRM + ")");
            return null;
        }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(pf);
        if (go == null) { Debug.LogWarning("[Probe] NiwaVroid: 置けない"); return null; }
        go.name = "VroidHikaku";
        go.transform.SetParent(oya, false);

        // ---- 大きさを 板の 絵に あわせる
        var rs = go.GetComponentsInChildren<Renderer>(true);
        var b = new Bounds();
        bool hajime = true;
        foreach (var r in rs) {
            if (hajime) { b = r.bounds; hajime = false; } else b.Encapsulate(r.bounds);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        float takasa = hajime ? 1.6f : b.size.y;
        float bai = takasa > 0.01f ? MITAKE / takasa : 1f;
        go.transform.localScale = Vector3.one * bai;
        Debug.Log("[Probe] NiwaVroid たかさ " + takasa.ToString("F2") + "m → 倍率 " + bai.ToString("F3")
                  + " (板の 絵は " + MITAKE.ToString("F2") + "m)");

        // ---- アニメ
        var an = go.GetComponentInChildren<Animator>();
        if (an == null) an = go.AddComponent<Animator>();
        an.runtimeAnimatorController = Suji();
        an.applyRootMotion = false;          // 位置は NiwaVroid が 決める

        var nv = go.AddComponent<NiwaVroid>();
        nv.target = player;
        nv.anim = an;
        nv.zure = new Vector3(1.1f, 0f, 0f);
        return go;
    }

    /// <summary>3つの 状態だけの すじみち を 作る（遷移は 作らない）</summary>
    static AnimatorController Suji() {
        var clips = new Dictionary<string, AnimationClip>();
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(FBX)) {
            if (a is AnimationClip c && !c.name.StartsWith("__preview__")) {
                // "Rig|Walk_Loop" の うしろだけ つかう
                var n = c.name;
                int bar = n.IndexOf('|');
                if (bar >= 0) n = n.Substring(bar + 1);
                clips[n] = c;
            }
        }
        Debug.Log("[Probe] NiwaVroid クリップ " + clips.Count + " 本");

        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(CTRL);
        if (ctrl != null) AssetDatabase.DeleteAsset(CTRL);
        ctrl = AnimatorController.CreateAnimatorControllerAtPath(CTRL);
        var sm = ctrl.layers[0].stateMachine;

        void Oku(string na, string clipNa) {
            AnimationClip c;
            if (!clips.TryGetValue(clipNa, out c)) {
                Debug.LogWarning("[Probe] NiwaVroid: クリップが 無い " + clipNa);
                return;
            }
            var st = sm.AddState(na);
            st.motion = c;
            if (na == "Idle") sm.defaultState = st;
        }
        // ★歩き／走りは **何とおりか 用意して 場で 切りかえる**。
        //   本人「今の走り方歩き方はまるでジョジョ」。汎用リグの 既定は 男性的で 重い。
        //   `Walk_Formal` と `Jog` の ほうが やわらかい はず なので 並べて 見くらべる
        Oku("Idle", "Idle_Loop");
        Oku("Walk", "Walk_Loop");
        Oku("WalkFormal", "Walk_Formal_Loop");
        Oku("Jog", "Jog_Fwd_Loop");
        Oku("Run", "Sprint_Loop");
        AssetDatabase.SaveAssets();
        return ctrl;
    }
}
