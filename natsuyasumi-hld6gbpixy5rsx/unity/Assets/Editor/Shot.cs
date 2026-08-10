// シーンを 絵に して 書きだす（GUIを ひらかずに 見た目を たしかめる ため）。
// Unity -batchmode -executeMethod Shot.Render -- -out <path> [-pitch 26] [-yaw -6] [-dist 7.2] [-fx 0|1]
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class Shot {
    static string Arg(string key, string def) {
        var a = Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++) if (a[i] == key) return a[i + 1];
        return def;
    }

    public static void Render() {
        string outPath = Arg("-out", "shot.png");
        int w = int.Parse(Arg("-w", "1280")), h = int.Parse(Arg("-h", "720"));
        bool fx = Arg("-fx", "1") == "1";

        // 書きだしの ときは ミップの 読みこみを 切る（遠くの 面が 黒く なるのを ふせぐ）
        QualitySettings.streamingMipmapsActive = false;
        QualitySettings.globalTextureMipmapLimit = 0;

        EditorSceneManager.OpenScene("Assets/Scenes/Zashiki.unity", OpenSceneMode.Single);

        var cam = Camera.main;
        var orbit = cam.GetComponent<CamOrbit>();
        if (orbit != null) {
            orbit.pitch    = float.Parse(Arg("-pitch", orbit.pitch.ToString()));
            orbit.yaw      = float.Parse(Arg("-yaw",   orbit.yaw.ToString()));
            orbit.distance = float.Parse(Arg("-dist",  orbit.distance.ToString()));
            orbit.SendMessage("Apply", SendMessageOptions.DontRequireReceiver);
        }
        var data = cam.GetComponent<UniversalAdditionalCameraData>();
        if (data != null) {
            data.renderPostProcessing = fx;
            data.volumeLayerMask = ~0;          // どの層の Volume も 拾う
        }

        // 影を 切って 見くらべる ため（-shadows 0）。自己遮蔽の 切りわけに 使う
        bool shadows = Arg("-shadows", "1") == "1";
        foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) {
            if (!shadows) l.shadows = LightShadows.None;
            if (l.type == LightType.Directional)
                Debug.Log($"[Shot] sun intensity={l.intensity} shadows={l.shadows} dir={l.transform.forward}");
        }
        Debug.Log($"[Shot] ambientMode={RenderSettings.ambientMode} sky={RenderSettings.ambientSkyColor} " +
                  $"equator={RenderSettings.ambientEquatorColor} fog={RenderSettings.fog}");

        // 板は カメラを 向く。実行していないので ここで 一度 向けてやる
        foreach (var b in UnityEngine.Object.FindObjectsByType<Billboard>(FindObjectsSortMode.None))
            b.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);

        // 切りわけ用：ぜんぶ 白い マテリアルに して 光だけを 見る（-plain 1）
        if (Arg("-plain", "0") == "1") {
            var white = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            white.SetColor("_BaseColor", Color.white);
            white.SetFloat("_Smoothness", 0.1f);
            foreach (var mr in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                mr.sharedMaterial = white;
        }

        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;

        // 画面の 何点かの 色を 数字で 出す（目で 見るより 確実）
        void Probe(string label, float u, float v) {
            var c = tex.GetPixel(Mathf.RoundToInt(u * w), Mathf.RoundToInt((1f - v) * h));
            Debug.Log($"[Shot] probe {label} = R{c.r:F3} G{c.g:F3} B{c.b:F3}");
        }
        Probe("floor-center", 0.42f, 0.80f);
        Probe("floor-left",   0.18f, 0.72f);
        Probe("table-top",    0.56f, 0.63f);
        Probe("shoji",        0.86f, 0.30f);
        Probe("backwall",     0.45f, 0.18f);

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath)));
        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        Debug.Log("[Shot] wrote " + Path.GetFullPath(outPath));
        EditorApplication.Exit(0);
    }
}
