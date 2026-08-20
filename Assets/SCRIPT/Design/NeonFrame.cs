using UnityEngine;
using UnityEngine.UI;
using TMPro;

// The framed panels the PvP screens are drawn from: a filled plate, a thin
// accent border, and light escaping around it.
//
// RuntimeUI already provides the filled rounded rectangle. What Converging
// Light was missing for PvP is the border and the glow, which is where the
// screens get their character. Both are generated from FrameGeometry rather
// than imported, and both are nine-sliced from a single small texture, so a
// screen full of frames costs two textures rather than one per panel.
public static class NeonFrame
{
    const int OutlineTex = 64;
    const int OutlineRadius = 16;
    const float OutlineThickness = 3f;

    const int GlowTex = 96;
    const int GlowInset = 16;   // how far the glow reaches beyond the frame
    const int GlowRadius = 16;

    static Sprite outlineSprite;
    static Sprite glowSprite;

    // A hollow rounded rectangle: border only, transparent within.
    public static Sprite OutlineSprite
    {
        get
        {
            if (outlineSprite == null) outlineSprite = BuildOutline();
            return outlineSprite;
        }
    }

    // A soft halo sitting outside the same shape, absent inside it, so it reads
    // as light off the frame rather than a second border.
    public static Sprite GlowSprite
    {
        get
        {
            if (glowSprite == null) glowSprite = BuildGlow();
            return glowSprite;
        }
    }

    static Sprite BuildOutline()
    {
        var tex = new Texture2D(OutlineTex, OutlineTex, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float half = OutlineTex * 0.5f;
        for (int y = 0; y < OutlineTex; y++)
        {
            for (int x = 0; x < OutlineTex; x++)
            {
                float px = x + 0.5f - half;
                float py = y + 0.5f - half;
                float d = FrameGeometry.Distance(px, py, half, half, OutlineRadius);
                float a = FrameGeometry.OutlineAlpha(d, OutlineThickness, 1f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();

        // The slice border must clear the corner arc, or stretching a wide
        // frame would smear the curve along its top and bottom edges.
        int border = OutlineRadius + 4;
        return Sprite.Create(tex, new Rect(0, 0, OutlineTex, OutlineTex),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
            new Vector4(border, border, border, border));
    }

    static Sprite BuildGlow()
    {
        var tex = new Texture2D(GlowTex, GlowTex, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float half = GlowTex * 0.5f;
        float shapeHalf = half - GlowInset;
        for (int y = 0; y < GlowTex; y++)
        {
            for (int x = 0; x < GlowTex; x++)
            {
                float px = x + 0.5f - half;
                float py = y + 0.5f - half;
                float d = FrameGeometry.Distance(px, py, shapeHalf, shapeHalf, OutlineRadius);
                float a = FrameGeometry.GlowAlpha(d, GlowRadius);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();

        int border = GlowInset + OutlineRadius + 4;
        return Sprite.Create(tex, new Rect(0, 0, GlowTex, GlowTex),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
            new Vector4(border, border, border, border));
    }

    // How much larger the glow object is than the frame it surrounds. Callers
    // laying out by hand need this to know a frame's true visual footprint.
    public const float GlowPadding = GlowInset * 2f;

    // A framed plate: glow, fill, then border. Returns the frame object itself,
    // so children added to it sit above the border and inside the padding.
    // `fillColor` overrides the plate colour; the default stays the indigo panel so
    // existing callers render unchanged.
    public static GameObject Frame(Transform parent, string name, Vector2 pos, Vector2 size,
                                   Color accent, float fillAlpha = 0.85f, bool glow = true,
                                   Color? fillColor = null)
    {
        var frame = RuntimeUI.CreateObject(name, parent);
        ConvergingLight.Center(frame, pos, size);
        RuntimeUI.ClampToSafeArea((RectTransform)frame.transform, size, pos);

        if (glow)
        {
            var halo = RuntimeUI.CreateObject("Glow", frame.transform);
            ConvergingLight.Center(halo, Vector2.zero, size + new Vector2(GlowPadding, GlowPadding));
            var haloImage = halo.AddComponent<Image>();
            haloImage.sprite = GlowSprite;
            haloImage.type = Image.Type.Sliced;
            haloImage.color = ConvergingLight.WithAlpha(accent, 0.30f);
            haloImage.raycastTarget = false;
        }

        var fill = RuntimeUI.CreateObject("Fill", frame.transform);
        RuntimeUI.Stretch(fill);
        var fillImage = fill.AddComponent<Image>();
        fillImage.sprite = RuntimeUI.RoundedRectSprite;
        fillImage.type = Image.Type.Sliced;
        fillImage.color = ConvergingLight.WithAlpha(fillColor ?? ConvergingLight.PanelIndigo, fillAlpha);
        fillImage.raycastTarget = false;

        var border = RuntimeUI.CreateObject("Border", frame.transform);
        RuntimeUI.Stretch(border);
        var borderImage = border.AddComponent<Image>();
        borderImage.sprite = OutlineSprite;
        borderImage.type = Image.Type.Sliced;
        borderImage.color = accent;
        borderImage.raycastTarget = false;

        return frame;
    }

    // One cell of the match-stat row: a small caption over a large value.
    // The caption is the label the player reads once; the value is what they
    // glance back at, so it carries the weight and the accent.
    public static TextMeshProUGUI StatChip(Transform parent, string name, Vector2 pos, Vector2 size,
                                           string caption, string value, Color accent)
    {
        var frame = Frame(parent, name, pos, size, ConvergingLight.WithAlpha(accent, 0.55f),
                          0.55f, false);

        RuntimeUI.CreateText(frame.transform, "Caption", caption, 22,
            new Vector2(0f, size.y * 0.26f), new Vector2(size.x - 16f, size.y * 0.4f),
            ConvergingLight.WithAlpha(ConvergingLight.NearWhite, 0.65f));

        return RuntimeUI.CreateText(frame.transform, "Value", value, 44,
            new Vector2(0f, -size.y * 0.18f), new Vector2(size.x - 16f, size.y * 0.5f),
            accent);
    }
}
