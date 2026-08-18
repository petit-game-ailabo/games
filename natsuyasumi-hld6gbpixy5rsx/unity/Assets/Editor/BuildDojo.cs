using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// R0：ものさし道場。廊下幅・戸口・段差・階段・スロープを ならべた 試験場を 組み、
// **実物と 同じ CharacterController を 歩かせて** 通れる/通れないの 境目を 測る。
// （3d-modular-building スキルの「メトリクステストマップ」。寸法は 測って から 凍結する）
//
//   rebuild.ps1 -Only BuildDojo.Build   … 試験場を 組む（Assets/Scenes/Dojo.unity）
//   rebuild.ps1 -Only BuildDojo.Walk    … 歩かせて 表を 出す
public static class BuildDojo {

    // 本編の 主人公と 同じ 寸法（BuildZashiki と そろえる）
    const float R = 0.26f, H = 1.0f, StepOff = 0.35f, Slope = 50f;
    const float WalkSpd = 2.6f;

    struct Fx { public string name; public Vector3 start, goal; public float minY; }
    static readonly List<Fx> fixtures = new List<Fx>();

    static GameObject Box(Transform t, string name, Vector3 c, Vector3 s) {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name; g.transform.SetParent(t, false);
        g.transform.position = c; g.transform.localScale = s;
        return g;
    }

    /// <summary>試験場を 組む。列ごとに 1つの ものさし</summary>
    public static void Build() {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
            UnityEditor.SceneManagement.NewSceneMode.Single);
        var root = new GameObject("Dojo").transform;
        fixtures.Clear();

        // 地めん（50 x 36）
        Box(root, "Ground", new Vector3(22f, -0.25f, 15f), new Vector3(52f, 0.5f, 38f));

        // --- 列1（z=0→5）：廊下の 幅。かべ 2まいの すきま
        float[] widths = { 0.55f, 0.60f, 0.70f, 0.80f, 1.00f, 1.50f };
        for (int i = 0; i < widths.Length; i++) {
            float x = 2f + i * 6f, w = widths[i];
            Box(root, "RoukaL" + i, new Vector3(x - (w + 0.15f) * 0.5f, 1f, 2.5f), new Vector3(0.15f, 2f, 3f));
            Box(root, "RoukaR" + i, new Vector3(x + (w + 0.15f) * 0.5f, 1f, 2.5f), new Vector3(0.15f, 2f, 3f));
            fixtures.Add(new Fx { name = "廊下 " + w.ToString("F2") + "m", start = new Vector3(x, 0.1f, 0f), goal = new Vector3(x, 0f, 5f) });
        }

        // --- 列2（z=7→10）：戸口。かべに あなを 1つ
        float[] doors = { 0.55f, 0.65f, 0.80f, 1.00f, 1.30f };
        for (int i = 0; i < doors.Length; i++) {
            float x = 2f + i * 6f, w = doors[i];
            Box(root, "DoorL" + i, new Vector3(x - (w + 2f) * 0.5f, 1f, 8.5f), new Vector3(2f, 2f, 0.15f));
            Box(root, "DoorR" + i, new Vector3(x + (w + 2f) * 0.5f, 1f, 8.5f), new Vector3(2f, 2f, 0.15f));
            fixtures.Add(new Fx { name = "戸口 " + w.ToString("F2") + "m", start = new Vector3(x, 0.1f, 7f), goal = new Vector3(x, 0f, 10f) });
        }

        // --- 列3（z=12→14）：段差。台に 上がれるか
        float[] steps = { 0.20f, 0.30f, 0.35f, 0.40f, 0.50f };
        for (int i = 0; i < steps.Length; i++) {
            float x = 2f + i * 6f, h = steps[i];
            Box(root, "Dan" + i, new Vector3(x, h * 0.5f, 13f), new Vector3(2f, h, 2f));
            fixtures.Add(new Fx { name = "段差 " + h.ToString("F2") + "m", start = new Vector3(x, 0.1f, 11.5f), goal = new Vector3(x, h, 13f), minY = h - 0.15f });
        }

        // --- 列4（z=16→N）：階段。蹴上×踏面で 2.5m のぼる
        float[][] stairs = { new[]{0.20f,0.30f}, new[]{0.25f,0.28f}, new[]{0.28f,0.28f}, new[]{0.32f,0.26f} };
        for (int i = 0; i < stairs.Length; i++) {
            float x = 2f + i * 6f, rise = stairs[i][0], tread = stairs[i][1];
            int n = Mathf.CeilToInt(2.5f / rise);
            for (int j = 0; j < n; j++)
                Box(root, "Kai" + i + "_" + j,
                    new Vector3(x, (j + 1) * rise * 0.5f, 17f + j * tread + tread * 0.5f),
                    new Vector3(1.2f, (j + 1) * rise, tread));
            // てっぺんの 台
            Box(root, "KaiTop" + i, new Vector3(x, n * rise * 0.5f, 17f + n * tread + 1f), new Vector3(1.2f, n * rise, 2f));
            fixtures.Add(new Fx { name = "階段 蹴上" + rise.ToString("F2") + "×踏面" + tread.ToString("F2"),
                                  start = new Vector3(x, 0.1f, 15.5f),
                                  goal = new Vector3(x, n * rise, 17f + n * tread + 1f), minY = n * rise - 0.2f });
        }

        // --- 列5（z=24→N）：スロープ。角度で 2m のぼる
        float[] slopes = { 20f, 35f, 45f, 49f, 55f };
        for (int i = 0; i < slopes.Length; i++) {
            float x = 2f + i * 6f, deg = slopes[i];
            float len = 2f / Mathf.Sin(deg * Mathf.Deg2Rad);   // 高さ2m ぶんの 坂の 長さ
            var g = Box(root, "Saka" + i,
                new Vector3(x, 1f - 0.15f * Mathf.Cos(deg * Mathf.Deg2Rad), 25f + len * 0.5f * Mathf.Cos(deg * Mathf.Deg2Rad)),
                new Vector3(1.6f, 0.3f, len));
            g.transform.rotation = Quaternion.Euler(-deg, 0f, 0f);
            float zTop = 25f + len * Mathf.Cos(deg * Mathf.Deg2Rad);
            Box(root, "SakaTop" + i, new Vector3(x, 1f, zTop + 1f), new Vector3(1.6f, 2f, 2f));
            fixtures.Add(new Fx { name = "スロープ " + deg + "度",
                                  start = new Vector3(x, 0.1f, 23.5f),
                                  goal = new Vector3(x, 2f, zTop + 1f), minY = 1.8f });
        }

        // --- 階段＋ランププロキシ（**見た目＝段(あたり無し)・あたり＝坂**）。R2の 作りかたの 試験
        {
            float x = 26f, rise = 0.25f, tread = 0.28f;
            int n = Mathf.CeilToInt(2.5f / rise);
            for (int j = 0; j < n; j++) {
                var g = Box(root, "PKai" + j,
                    new Vector3(x, (j + 1) * rise * 0.5f, 17f + j * tread + tread * 0.5f),
                    new Vector3(1.2f, (j + 1) * rise, tread));
                Object.DestroyImmediate(g.GetComponent<Collider>());
            }
            float ang = Mathf.Atan2(rise, tread) * Mathf.Rad2Deg;
            float hgt = n * rise;
            float len = Mathf.Sqrt(hgt * hgt + n * tread * n * tread);
            var ramp = Box(root, "PKaiRamp", Vector3.zero, new Vector3(1.2f, 0.1f, len));
            ramp.transform.rotation = Quaternion.Euler(-ang, 0f, 0f);
            ramp.transform.position = new Vector3(x, hgt * 0.5f, 17f + n * tread * 0.5f);
            Object.DestroyImmediate(ramp.GetComponent<MeshRenderer>());
            Box(root, "PKaiTop", new Vector3(x, hgt * 0.5f, 17f + n * tread + 1f), new Vector3(1.2f, hgt, 2f));
            fixtures.Add(new Fx { name = "階段+ランプ 0.25/0.28", start = new Vector3(x, 0.1f, 15.5f),
                                  goal = new Vector3(x, hgt, 17f + n * tread + 1f), minY = hgt - 0.2f });
        }

        // --- レール。歩かせ役が 衝突で 横に 逃げ、坂を 迂回して「だめ」に 見えて いた
        //（初回の 実測の 教訓：**たしかめが 嘘を つくと その先が 全部 あてに ならない**）
        for (int i = 0; i < 5; i++) {
            float x = 2f + i * 6f;
            Box(root, "RailK_L" + i, new Vector3(x - 0.9f, 1.5f, 19.5f), new Vector3(0.15f, 3f, 9f));
            Box(root, "RailK_R" + i, new Vector3(x + 0.9f, 1.5f, 19.5f), new Vector3(0.15f, 3f, 9f));
            Box(root, "RailS_L" + i, new Vector3(x - 0.9f, 1.5f, 28.5f), new Vector3(0.15f, 3f, 11f));
            Box(root, "RailS_R" + i, new Vector3(x + 0.9f, 1.5f, 28.5f), new Vector3(0.15f, 3f, 11f));
        }

        // Kensa の 塗りつぶしの 種に なる 目じるし
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 0.1f, 0f);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, "Assets/Scenes/Dojo.unity");
        SaveFixtures();
        Debug.Log("[Probe] BuildDojo done: fixtures=" + fixtures.Count);
    }

    // Build と Walk は 別の 起動に なる ので、ものさしの 表は ファイルで わたす
    static string FxPath { get {
        var d = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "natsuyasumi");
        System.IO.Directory.CreateDirectory(d);
        return System.IO.Path.Combine(d, "dojo_fixtures.txt");
    } }
    static void SaveFixtures() {
        var sb = new System.Text.StringBuilder();
        foreach (var f in fixtures)
            sb.AppendLine(f.name + "|" + V(f.start) + "|" + V(f.goal) + "|" + f.minY);
        System.IO.File.WriteAllText(FxPath, sb.ToString());
    }
    static string V(Vector3 v) { return v.x + "," + v.y + "," + v.z; }
    static Vector3 PV(string s) {
        var p = s.Split(',');
        return new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]));
    }

    /// <summary>実物と 同じ CharacterController を 目標へ 歩かせ、着いたかを 表に する</summary>
    public static void Walk() {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            "Assets/Scenes/Dojo.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
        var go = new GameObject("Walker");
        var cc = go.AddComponent<CharacterController>();
        cc.height = H; cc.radius = R; cc.center = new Vector3(0f, H * 0.5f + 0.02f, 0f);
        cc.slopeLimit = Slope; cc.stepOffset = StepOff;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[Probe] Dojo.Walk（実物の CharacterController・歩き " + WalkSpd + "m/s）");
        foreach (var line in System.IO.File.ReadAllLines(FxPath)) {
            if (line.Trim().Length == 0) continue;
            var p = line.Split('|');
            Vector3 start = PV(p[1]), goal = PV(p[2]);
            float minY = float.Parse(p[3]);
            // ★CharacterController は transform.position を 書いても テレポートできない。
            //   内部の 位置が のこり、**前の 試験の 場所から 歩き つづける**（実測で 発覚）。
            //   いったん 切ってから 動かし、Physics.SyncTransforms で 反映する
            cc.enabled = false;
            go.transform.position = start + Vector3.up * 0.05f;
            Physics.SyncTransforms();
            cc.enabled = true;
            float vy = 0f; const float dt = 0.02f;
            for (int s = 0; s < 700; s++) {
                var flat = goal - go.transform.position; flat.y = 0f;
                var dir = flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.zero;
                vy = cc.isGrounded ? -0.5f : vy - 9.8f * dt;
                cc.Move((dir * WalkSpd + Vector3.up * vy) * dt);
            }
            var at = go.transform.position;
            var d2 = goal - at; d2.y = 0f;
            bool ok = d2.magnitude < 0.5f && at.y >= minY;
            sb.AppendFormat("[Probe]   {0}  →  {1}   (着いた 所 {2:F2},{3:F2},{4:F2})\n",
                            (p[0] + "                    ").Substring(0, 20), ok ? "とおれた" : "だめ",
                            at.x, at.y, at.z);
        }
        Debug.Log(sb.ToString());
    }
}
