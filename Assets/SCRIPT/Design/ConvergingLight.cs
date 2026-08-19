using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Converging Light (design/philosophy.md) — shared palette and runtime
// texture helpers for the design layer (SplashDesign, DesignRuntimeWiring).
// Everything is generated from code: no art assets, no scene surgery.
public static class ConvergingLight
{
    // Palette canon: layered indigo depth, one cyan-to-magenta seam of light,
    // muted gold reserved for the single most important mark, near-white text.
    public static readonly Color DepthTop = new Color(0.035f, 0.035f, 0.110f);
    public static readonly Color DepthBottom = new Color(0.100f, 0.090f, 0.235f);
    public static readonly Color Cyan = new Color(0.25f, 0.85f, 1f);
    public static readonly Color Magenta = new Color(0.83f, 0.35f, 1f);
    public static readonly Color Gold = new Color(1f, 0.78f, 0.34f);
    public static readonly Color NearWhite = new Color(0.91f, 0.93f, 1f);
    public static readonly Color TrackIndigo = new Color(0.16f, 0.15f, 0.26f);
    public static readonly Color PanelIndigo = new Color(0.09f, 0.08f, 0.19f);
    public static readonly Color ScrimIndigo = new Color(0.05f, 0.05f, 0.12f);
    static Sprite depthGradientSprite;

    public static Sprite DepthGradientSprite
    {
        get
        {
            if (depthGradientSprite == null)
                depthGradientSprite = VerticalGradient(DepthTop, DepthBottom);
            return depthGradientSprite;
        }
    }

    public static Color WithAlpha(Color c, float a)
    {
        c.a = a;
        return c;
    }

    // 1 x height vertical gradient (top -> bottom) as a sprite.
    public static Sprite VerticalGradient(Color top, Color bottom, int height = 256)
    {
        var tex = new Texture2D(1, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < height; y++)
        {
            float t = (float)y / (height - 1);
            tex.SetPixel(0, y, Color.Lerp(bottom, top, t));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, height), new Vector2(0.5f, 0.5f));
    }

    // width x 1 horizontal gradient (left -> right) as a sprite.
    public static Sprite HorizontalGradient(Color left, Color right, int width = 512)
    {
        var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        for (int x = 0; x < width; x++)
        {
            float t = (float)x / (width - 1);
            tex.SetPixel(x, 0, Color.Lerp(left, right, t));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, 1), new Vector2(0.5f, 0.5f));
    }

    // Centers `go` (created via RuntimeUI.CreateObject) at `pos` with `size`.
    public static void Center(GameObject go, Vector2 pos, Vector2 size)
    {
        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
    }

    // "Numbers are treated as texture and talisman rather than data — a faint
    // field of them can fill a sky." Spawns low-alpha drifting digits under
    // `parent`; each digit carries a NumberDrift. Positions are in the
    // 1080x1920 reference space the canvases share.
    public static void NumberField(Transform parent, int count, float maxAlpha)
    {
        var rng = new System.Random();
        for (int i = 0; i < count; i++)
        {
            var go = RuntimeUI.CreateObject("n" + i, parent);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(60f, 60f);
            rect.anchoredPosition = new Vector2(
                Mathf.Lerp(-520f, 520f, (float)rng.NextDouble()),
                Mathf.Lerp(-940f, 940f, (float)rng.NextDouble()));

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = rng.Next(0, 10).ToString();
            text.fontSize = rng.Next(18, 44);
            text.alignment = TextAlignmentOptions.Center;
            text.color = WithAlpha(NearWhite,
                Mathf.Lerp(maxAlpha * 0.4f, maxAlpha, (float)rng.NextDouble()));
            text.raycastTarget = false;

            var drift = go.AddComponent<NumberDrift>();
            drift.velocity = new Vector2(
                Mathf.Lerp(-6f, 6f, (float)rng.NextDouble()),
                Mathf.Lerp(4f, 14f, (float)rng.NextDouble()));
        }
    }
}
