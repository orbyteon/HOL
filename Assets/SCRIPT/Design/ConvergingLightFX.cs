using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Converging Light (design/philosophy.md) — the publisher-polish layer on top
// of ConvergingLight's palette: cached procedural sprites (deep field, radial
// glow, vignette, shine band) and the shared component treatments (card
// surfaces, elevation shadows, hairline edge seams, button roles). Everything
// is generated from code — no art assets, no scene surgery.
public static class ConvergingLightFX
{
    // Elevation shadow canon: one soft-dark drop used by every raised surface.
    static readonly Color ShadowColor = new Color(0.01f, 0.01f, 0.05f, 0.55f);
    static readonly Vector2 ShadowOffset = new Vector2(0f, -7f);

    // Ghost surfaces sit between panel and depth; labels stay near-white.
    // Property, not field: themes swap mid-session and a baked static would
    // keep serving the boot theme's color.
    public static Color GhostSurface =>
        ConvergingLight.WithAlpha(ConvergingLight.TrackIndigo, 0.92f);
    public static readonly Color DarkLabel = new Color(0.10f, 0.09f, 0.18f);

    // ------------------------------------------------------------ sprites

    static Sprite deepField;
    static Sprite radialGlow;
    static Sprite vignette;
    static Sprite shineBand;
    static Sprite seam;

    // The nocturnal field: layered depth, darkest at the top, warming very
    // slightly toward the seam's second color at the bottom. Built from the
    // active theme's depth colors.
    public static Sprite DeepField
    {
        get
        {
            if (deepField == null)
            {
                Color top = ConvergingLight.DepthTop;
                Color bottom = ConvergingLight.DepthBottom;
                deepField = MultiStopVertical(new[]
                {
                    Color.Lerp(top, Color.black, 0.18f),
                    Color.Lerp(top, bottom, 0.45f),
                    Color.Lerp(top, bottom, 0.82f),
                    Color.Lerp(bottom, ConvergingLight.Magenta, 0.07f),
                });
            }
            return deepField;
        }
    }

    // Theme swaps must rebuild the sprites that bake palette colors; the
    // white falloff sprites (glow, vignette, shine) are tinted per-Image
    // and stay valid.
    public static void InvalidateThemedSprites()
    {
        deepField = null;
        seam = null;
    }

    // White radial falloff; tint with Image.color. "Every light source is
    // haloed with painstaking gradation."
    public static Sprite RadialGlow
    {
        get
        {
            if (radialGlow == null)
                radialGlow = GenerateRadialGlow(256);
            return radialGlow;
        }
    }

    // Transparent center, alpha rising toward the edges; tint dark to pull
    // the composition's corners back into the night.
    public static Sprite Vignette
    {
        get
        {
            if (vignette == null)
                vignette = GenerateVignette(256);
            return vignette;
        }
    }

    // Soft horizontal light band for CTA shine sweeps.
    public static Sprite ShineBand
    {
        get
        {
            if (shineBand == null)
                shineBand = GenerateShineBand(64);
            return shineBand;
        }
    }

    // The one seam of light, cyan to magenta, shared by every hairline.
    public static Sprite Seam
    {
        get
        {
            if (seam == null)
                seam = ConvergingLight.HorizontalGradient(
                    ConvergingLight.Cyan, ConvergingLight.Magenta);
            return seam;
        }
    }

    static Sprite MultiStopVertical(Color[] stops, int height = 512)
    {
        var tex = new Texture2D(1, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < height; y++)
        {
            // y=0 is the bottom of the sprite; stops[] reads top -> bottom.
            float t = 1f - (float)y / (height - 1);
            float f = t * (stops.Length - 1);
            int i = Mathf.Min(stops.Length - 2, Mathf.FloorToInt(f));
            tex.SetPixel(0, y, Color.Lerp(stops[i], stops[i + 1], f - i));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, height), new Vector2(0.5f, 0.5f));
    }

    static Sprite GenerateRadialGlow(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.4f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    static Sprite GenerateVignette(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                // Clear until ~55% out, then ease toward the corners.
                float a = Mathf.Pow(Mathf.Clamp01((d - 0.55f) / 0.65f), 1.6f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    static Sprite GenerateShineBand(int width)
    {
        var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        for (int x = 0; x < width; x++)
        {
            float t = (float)x / (width - 1);
            float a = Mathf.Pow(Mathf.Sin(t * Mathf.PI), 2f);
            tex.SetPixel(x, 0, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, 1), new Vector2(0.5f, 0.5f));
    }

    // ------------------------------------------------------------ treatments

    // One soft-dark elevation shadow. Idempotent per graphic.
    public static void AddShadow(Graphic graphic)
    {
        if (graphic == null || graphic.GetComponent<Shadow>() != null) return;
        var shadow = graphic.gameObject.AddComponent<Shadow>();
        shadow.effectColor = ShadowColor;
        shadow.effectDistance = ShadowOffset;
    }

    // Hairline cyan-to-magenta seam along the top inside edge of a rect —
    // "light never floods; it traces". Inset past the corner radius.
    public static void AddEdgeLight(Transform parent, float alpha = 0.5f)
    {
        if (parent == null || parent.Find("EdgeLight") != null) return;

        var go = RuntimeUI.CreateObject("EdgeLight", parent);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(18f, -4f);
        rect.offsetMax = new Vector2(-18f, -2f);

        var img = go.AddComponent<Image>();
        img.sprite = Seam;
        img.color = ConvergingLight.WithAlpha(Color.white, alpha);
        img.raycastTarget = false;
    }

    // A standalone seam hairline with a soft bloom behind it (hero underline).
    public static void AddSeamRule(Transform parent, Vector2 pos, float width, float alpha = 0.85f)
    {
        var bloom = RuntimeUI.CreateObject("SeamBloom", parent);
        ConvergingLight.Center(bloom, pos, new Vector2(width, 36f));
        var bloomImg = bloom.AddComponent<Image>();
        bloomImg.sprite = Seam;
        bloomImg.color = ConvergingLight.WithAlpha(Color.white, alpha * 0.2f);
        bloomImg.raycastTarget = false;

        var rule = RuntimeUI.CreateObject("SeamRule", parent);
        ConvergingLight.Center(rule, pos, new Vector2(width, 2f));
        var ruleImg = rule.AddComponent<Image>();
        ruleImg.sprite = Seam;
        ruleImg.color = ConvergingLight.WithAlpha(Color.white, alpha);
        ruleImg.raycastTarget = false;
    }

    // Raised card surface: rounded, indigo, shadowed, seam-lit.
    public static void StyleCard(Image surface, float alpha = 0.94f)
    {
        if (surface == null) return;
        surface.sprite = RuntimeUI.RoundedRectSprite;
        surface.type = Image.Type.Sliced;
        surface.color = ConvergingLight.WithAlpha(ConvergingLight.PanelIndigo, alpha);
        AddShadow(surface);
        AddEdgeLight(surface.transform, 0.4f);
    }

    // A diffuse light pool behind a card. Parent must be BEHIND the card in
    // sibling order (children paint over parents), so this inserts at index 0.
    public static void AddCardGlow(Transform panel, Vector2 pos, Vector2 cardSize, Color color, float alpha = 0.10f)
    {
        if (panel == null || panel.Find("CardGlow") != null) return;

        var go = RuntimeUI.CreateObject("CardGlow", panel);
        ConvergingLight.Center(go, pos, cardSize + new Vector2(360f, 360f));
        var img = go.AddComponent<Image>();
        img.sprite = RadialGlow;
        img.color = ConvergingLight.WithAlpha(color, alpha);
        img.raycastTarget = false;
        go.transform.SetSiblingIndex(0);
    }

    // ------------------------------------------------------------ button roles

    public enum ButtonRole { Primary, Secondary, Ghost }

    // Unifies any button (scene-authored art or runtime-built) into the
    // design language: rounded surface, role color, correct label color,
    // elevation shadow. The shine sweep is NOT automatic — restraint is the
    // philosophy, so callers gild only each screen's single true CTA.
    public static void StyleButton(Button button, ButtonRole role)
    {
        if (button == null) return;

        var img = button.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = RuntimeUI.RoundedRectSprite;
            img.type = Image.Type.Sliced;
            img.color = role == ButtonRole.Primary ? ConvergingLight.Gold
                : role == ButtonRole.Secondary ? ConvergingLight.Cyan
                : GhostSurface;
            AddShadow(img);
        }

        Color label = role == ButtonRole.Ghost ? ConvergingLight.NearWhite : DarkLabel;
        foreach (var tmp in button.GetComponentsInChildren<TMP_Text>(true))
            tmp.color = ConvergingLight.WithAlpha(label, tmp.color.a);
        foreach (var legacy in button.GetComponentsInChildren<Text>(true))
            legacy.color = ConvergingLight.WithAlpha(label, legacy.color.a);

        if (role == ButtonRole.Ghost)
            AddEdgeLight(button.transform, 0.28f);
    }

    // The glint that marks a screen's one primary call to action.
    public static void AddShine(Button button)
    {
        if (button != null && button.GetComponent<ShineSweep>() == null)
            button.gameObject.AddComponent<ShineSweep>();
    }

    // Icon buttons (art glyphs with no label): keep the art, soften it into
    // the palette and give it the shared elevation.
    public static void StyleIconButton(Button button)
    {
        if (button == null) return;
        var img = button.GetComponent<Image>();
        if (img == null) return;
        img.color = new Color(0.85f, 0.87f, 1f, img.color.a);
        AddShadow(img);
    }

    // Engraved-caption treatment for TMP headers: letterspaced, near-white.
    public static void StyleHeading(TMP_Text text, float spacing = 4f)
    {
        if (text == null) return;
        text.characterSpacing = spacing;
        text.color = ConvergingLight.WithAlpha(ConvergingLight.NearWhite, text.color.a);
    }
}
