using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Migration compatibility facade for callers that still request a framed
// runtime surface. The old implementation generated rounded-rectangle fill,
// outline and glow textures from code. That visual system is retired.
//
// Every frame now renders ONE approved production 9-slice sprite at alpha 1.
// Callers are being migrated to screen-specific owners; once the last caller is
// removed this compatibility class should be deleted/renamed as part of the
// legacy-theme purge.
public static class NeonFrame
{
    const string PurpleFrame = "mainmenu/mainmenu_tip_frame_9s";
    const string BlueFrame = "mainmenu/mainmenu_cta_blue_9s";
    const string MagentaFrame = "phase2a/hol_cta_magenta_r2_9s";
    const string GoldFrame = "mainmenu/mainmenu_cta_gold_9s";

    // Kept only for source compatibility with old layout code. No generated
    // halo exists anymore, so the real visual footprint equals the frame bounds.
    public const float GlowPadding = 0f;

    public static GameObject Frame(
        Transform parent,
        string name,
        Vector2 pos,
        Vector2 size,
        Color accent,
        float fillAlpha = 0.85f,
        bool glow = true,
        Color? fillColor = null)
    {
        var frame = RuntimeUI.CreateObject(name, parent);
        Center(frame, pos, size);
        RuntimeUI.ClampToSafeArea((RectTransform)frame.transform, size, pos);

        var image = frame.AddComponent<Image>();
        string resource = ResolveFrameResource(accent, fillColor);
        if (!RuntimeUI.ApplyProductionSprite(
                image, resource, Image.Type.Sliced, false, 2f))
        {
            // Functional fallback only. Required-art tests must catch this
            // before a production build is accepted.
            image.sprite = RuntimeUI.RoundedRectSprite;
            image.type = Image.Type.Sliced;
            image.color = fillColor ?? ConsumerTokens.Surface;
        }
        image.raycastTarget = false;
        return frame;
    }

    public static TextMeshProUGUI StatChip(
        Transform parent,
        string name,
        Vector2 pos,
        Vector2 size,
        string caption,
        string value,
        Color accent)
    {
        var frame = Frame(parent, name, pos, size, accent, 1f, false);

        RuntimeUI.CreateText(
            frame.transform,
            "Caption",
            caption,
            22,
            new Vector2(0f, size.y * 0.26f),
            new Vector2(size.x - 16f, size.y * 0.4f),
            ConsumerTokens.WithAlpha(ConsumerTokens.TextPrimary, 0.78f));

        return RuntimeUI.CreateText(
            frame.transform,
            "Value",
            value,
            44,
            new Vector2(0f, -size.y * 0.18f),
            new Vector2(size.x - 16f, size.y * 0.5f),
            accent);
    }

    static string ResolveFrameResource(Color accent, Color? fillColor)
    {
        // We select an approved sprite family; we never tint/repaint it.
        if (Distance(accent, ConsumerTokens.Gold) < 0.35f)
            return GoldFrame;
        if (Distance(accent, ConsumerTokens.Magenta) < 0.42f ||
            (fillColor.HasValue && Distance(fillColor.Value, ConsumerTokens.CardPink) < 0.45f))
            return MagentaFrame;
        if (Distance(accent, ConsumerTokens.Cyan) < 0.48f ||
            Distance(accent, ConsumerTokens.Blue) < 0.48f ||
            (fillColor.HasValue && Distance(fillColor.Value, ConsumerTokens.CardBlue) < 0.50f))
            return BlueFrame;
        return PurpleFrame;
    }

    static float Distance(Color a, Color b)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return Mathf.Sqrt(dr * dr + dg * dg + db * db);
    }

    static void Center(GameObject go, Vector2 pos, Vector2 size)
    {
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }
}
