using UnityEngine;
using UnityEngine.UI;

// Three diffuse light pools — cyan high, magenta low, a scarce gold ember —
// drifting and breathing on slow, mutually prime periods so the night never
// visibly loops. Sits between the deep field and the number layers; never
// intercepts raycasts. Unscaled time keeps it alive behind panel transitions.
public class AuroraBackdrop : MonoBehaviour
{
    class Pool
    {
        public RectTransform rect;
        public Image image;
        public Vector2 basePos;
        public float baseAlpha;
        public float drift;      // px of positional wander
        public float period;     // seconds per full wander cycle
        public float phase;
    }

    Pool[] pools;

    // Builds the aurora layer under `parent` (stretched) and returns it.
    public static AuroraBackdrop Attach(Transform parent)
    {
        var layer = RuntimeUI.CreateObject("BackdropAurora", parent);
        RuntimeUI.Stretch(layer);
        var aurora = layer.AddComponent<AuroraBackdrop>();
        aurora.Build();
        return aurora;
    }

    void Build()
    {
        pools = new[]
        {
            MakePool("AuroraCyan", new Vector2(-330f, 640f), 980f,
                ConvergingLight.Cyan, 0.16f, 46f, 19f),
            MakePool("AuroraMagenta", new Vector2(360f, -700f), 1120f,
                ConvergingLight.Magenta, 0.13f, 56f, 23f),
            MakePool("AuroraGold", new Vector2(60f, -170f), 520f,
                ConvergingLight.Gold, 0.07f, 30f, 29f),
        };
    }

    Pool MakePool(string name, Vector2 pos, float size, Color color, float alpha,
        float drift, float period)
    {
        var go = RuntimeUI.CreateObject(name, transform);
        ConvergingLight.Center(go, pos, new Vector2(size, size));

        var img = go.AddComponent<Image>();
        img.sprite = ConvergingLightFX.RadialGlow;
        img.color = ConvergingLight.WithAlpha(color, alpha);
        img.raycastTarget = false;

        return new Pool
        {
            rect = (RectTransform)go.transform,
            image = img,
            basePos = pos,
            baseAlpha = alpha,
            drift = drift,
            period = period,
            phase = Random.value * period,
        };
    }

    void Update()
    {
        if (pools == null) return;

        float t = Time.unscaledTime;
        foreach (var p in pools)
        {
            if (p.rect == null) continue;

            float w = (t + p.phase) / p.period * Mathf.PI * 2f;
            p.rect.anchoredPosition = p.basePos + new Vector2(
                Mathf.Sin(w) * p.drift,
                Mathf.Cos(w * 0.7f) * p.drift * 0.6f);

            // Breathe around the base alpha, never brighter than +20%.
            var c = p.image.color;
            c.a = p.baseAlpha * (0.8f + 0.2f * (0.5f + 0.5f * Mathf.Sin(w * 1.3f)));
            p.image.color = c;
        }
    }
}
