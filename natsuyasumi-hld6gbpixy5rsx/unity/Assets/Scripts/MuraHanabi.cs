using UnityEngine;

// 遠くの 花火大会（EVENTS D・8月8日の 夜だけ）。南の 山なみの 上に ぽつぽつ 上がる。
// となり町の 花火＝小さくて 音は おくれて 届かない（無音）。高台から 見るのが いちばん。
public class MuraHanabi : MonoBehaviour {
    public int hi = 8;                   // この 日の 夜だけ
    float nextT;

    class Tama {
        public GameObject go; public Light li; public float t; public Color c;
    }
    readonly System.Collections.Generic.List<Tama> tamas = new System.Collections.Generic.List<Tama>();

    void Update() {
        bool onNight = MuraDay.Day == hi && MuraDay.Night;
        if (onNight && Time.time > nextT) {
            nextT = Time.time + Random.Range(1.6f, 3.4f);
            var c = new[] { new Color(1f, 0.55f, 0.4f), new Color(0.5f, 0.8f, 1f),
                            new Color(1f, 0.9f, 0.5f), new Color(0.8f, 0.6f, 1f) }[Random.Range(0, 4)];
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "HanabiTama";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = new Vector3(Random.Range(-50f, 70f), Random.Range(24f, 34f), -108f);
            var li = go.AddComponent<Light>();
            li.type = LightType.Point; li.range = 40f; li.color = c;
            tamas.Add(new Tama { go = go, li = li, c = c });
        }
        for (int i = tamas.Count - 1; i >= 0; i--) {
            var t = tamas[i];
            t.t += Time.deltaTime;
            float k = t.t / 1.8f;                     // 1.8秒で ひらいて 消える
            if (k >= 1f) { Destroy(t.go); tamas.RemoveAt(i); continue; }
            float r = Mathf.Lerp(1.5f, 9f, Mathf.Sqrt(k));
            t.go.transform.localScale = Vector3.one * r;
            float a = 1f - k;
            var mat = t.go.GetComponent<Renderer>().material;
            mat.color = new Color(t.c.r, t.c.g, t.c.b, a);
            t.li.intensity = 90f * a;
        }
    }
}
