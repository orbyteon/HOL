using UnityEngine;

// TEMPORARY SOURCE-COMPATIBILITY SHIM.
//
// The Converging Light visual system is retired. Existing callers are being
// migrated to screen-specific production owners. Until the last caller is gone,
// this class exposes only generic layout/color helpers and an approved current
// background fallback. It MUST NOT generate theme decoration, recolor screens or
// create drifting-number fields.
public static class ConvergingLight
{
    // Compatibility aliases for dynamic text/state only. Approved sprites are
    // never recolored from these values.
    public static readonly Color DepthTop = ConsumerTokens.Background0;
    public static readonly Color DepthBottom = ConsumerTokens.Surface;
    public static readonly Color Cyan = ConsumerTokens.Cyan;
    public static readonly Color Magenta = ConsumerTokens.Magenta;
    public static readonly Color Gold = ConsumerTokens.Gold;
    public static readonly Color NearWhite = ConsumerTokens.TextPrimary;
    public static readonly Color TrackIndigo = ConsumerTokens.SurfaceElevated;
    public static readonly Color PanelIndigo = ConsumerTokens.Surface;
    public static readonly Color ScrimIndigo = ConsumerTokens.Background0;

    const string CurrentBackgroundResource = "phase2a/hol_neon_reference_bg_r3";
    static Sprite currentBackground;

    public static Sprite DepthGradientSprite
    {
        get
        {
            if (currentBackground != null) return currentBackground;
            currentBackground = Resources.Load<Sprite>(CurrentBackgroundResource);
            if (currentBackground == null)
            {
                Debug.LogError("[UI Migration] Missing approved Resources/" +
                    CurrentBackgroundResource + ".");
                currentBackground = VerticalGradient(DepthTop, DepthBottom);
            }
            return currentBackground;
        }
    }

    public static Color WithAlpha(Color color, float alpha)
    {
        return ConsumerTokens.WithAlpha(color, alpha);
    }

    // Generic utility retained only for small non-art surfaces such as progress
    // fills. Do not use it as a production screen/background replacement.
    public static Sprite VerticalGradient(Color top, Color bottom, int height = 32)
    {
        height = Mathf.Max(2, height);
        var tex = new Texture2D(1, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < height; y++)
        {
            float t = (float)y / (height - 1);
            tex.SetPixel(0, y, Color.Lerp(bottom, top, t));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, height),
            new Vector2(0.5f, 0.5f));
    }

    public static Sprite HorizontalGradient(Color left, Color right, int width = 32)
    {
        width = Mathf.Max(2, width);
        var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        for (int x = 0; x < width; x++)
        {
            float t = (float)x / (width - 1);
            tex.SetPixel(x, 0, Color.Lerp(left, right, t));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, 1),
            new Vector2(0.5f, 0.5f));
    }

    public static void Center(GameObject go, Vector2 pos, Vector2 size)
    {
        if (go == null) return;
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    // Retired decoration hook. Intentionally no-op so historical callers cannot
    // reintroduce the drifting-number theme while migration is in progress.
    public static void NumberField(Transform parent, int count, float maxAlpha)
    {
    }
}
