using UnityEngine;
using UnityEngine.UI;

// Shared presentation-side reader for the canonical Onboarding avatar
// contract. This class deliberately keeps no cache and writes no PlayerPrefs,
// so scene changes, language changes and rematches always observe the latest
// valid committed selection.
public static class PlayerProfileAvatarResolver
{
    public const string FallbackResourcePath =
        "reference/player_cyan_exact";
    public const string CircularApertureResourcePath =
        "onboarding/icons/onboarding_indicator_disc_neutral";

    public static Sprite Resolve()
    {
        Sprite fallback = Resources.Load<Sprite>(FallbackResourcePath);
        if (!OnboardingProfile.TryLoadCommittedAvatar(out int avatarIndex))
            return fallback;

        OnboardingAvatarCatalog.Entry entry =
            OnboardingAvatarCatalog.Get(avatarIndex);
        if (string.IsNullOrWhiteSpace(entry.ResourcePath))
            return fallback;

        Sprite selected = Resources.Load<Sprite>(entry.ResourcePath);
        return selected != null ? selected : fallback;
    }
}

// Presentation-only framing for resolved profile portraits. The identity and
// persistence contract above remains the sole authority for which Sprite is
// shown. These measurements only remove each approved PNG's transparent
// padding so its visible artwork is consistently centered and scaled.
public static class PlayerProfileAvatarFraming
{
    public const float TargetVisibleFill = 0.825f;
    public const float MinimumVisibleFill = 0.80f;
    public const float MaximumVisibleFill = 0.85f;

    // Guaranteed non-transparent circular support of the existing shared mask
    // sprite. The mask texture itself has transparent padding, so this usable
    // diameter (rather than the backing RectTransform) is the visual aperture.
    const float MaskCenterX = 0.5f;
    const float MaskCenterY = 0.5f;
    const float MaskRadius = 0.4469f;
    const float ApertureSafety = 0.5f;
    const float SourceEdgeSafety = 0.0015f;

    public readonly struct Metrics
    {
        public Metrics(
            Vector2 centerNormalized,
            float radiusNormalized,
            float dominantExtentNormalized)
        {
            CenterNormalized = centerNormalized;
            RadiusNormalized = radiusNormalized;
            DominantExtentNormalized = dominantExtentNormalized;
        }

        public Vector2 CenterNormalized { get; }
        public float RadiusNormalized { get; }
        public float DominantExtentNormalized { get; }
    }

    public readonly struct FramingLayout
    {
        public FramingLayout(
            Vector2 size,
            Vector2 position,
            float visibleFill,
            float radialSafety)
        {
            Size = size;
            Position = position;
            VisibleFill = visibleFill;
            RadialSafety = radialSafety;
        }

        public Vector2 Size { get; }
        public Vector2 Position { get; }
        public float VisibleFill { get; }
        public float RadialSafety { get; }
    }

    public static FramingLayout Apply(Image portrait, RectTransform aperture)
    {
        if (portrait == null || aperture == null || portrait.sprite == null)
            return default;

        FramingLayout layout = Calculate(portrait.sprite, aperture.rect.size);
        RectTransform rect = portrait.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = layout.Size;
        rect.anchoredPosition = layout.Position;
        portrait.preserveAspect = true;
        portrait.useSpriteMesh = false;
        return layout;
    }

    public static FramingLayout Calculate(Sprite sprite, Vector2 apertureSize)
    {
        if (sprite == null)
            return default;

        if (!TryGetMetrics(sprite, out Metrics metrics))
        {
            metrics = new Metrics(
                new Vector2(0.5f, 0.5f),
                0.7072f,
                1f);
        }

        Vector2 sourceSize = sprite.rect.size;
        float sourceMaximum = Mathf.Max(sourceSize.x, sourceSize.y);
        float apertureDiameter = Mathf.Min(
            Mathf.Abs(apertureSize.x), Mathf.Abs(apertureSize.y));
        if (sourceMaximum <= Mathf.Epsilon ||
            apertureDiameter <= Mathf.Epsilon)
            return default;

        float innerApertureRadius = MaskRadius * apertureDiameter;
        float innerApertureDiameter = innerApertureRadius * 2f;
        float dominantPixels =
            metrics.DominantExtentNormalized * sourceMaximum;
        float radiusPixels =
            (metrics.RadiusNormalized + SourceEdgeSafety) * sourceMaximum;
        float fillScale =
            innerApertureDiameter * TargetVisibleFill / dominantPixels;
        float containmentScale =
            Mathf.Max(0f, innerApertureRadius - ApertureSafety) /
            radiusPixels;
        float scale = Mathf.Min(fillScale, containmentScale);

        Vector2 size = sourceSize * scale;
        Vector2 sourceCenter = Vector2.Scale(
            metrics.CenterNormalized, sourceSize);
        Vector2 sourceRectCenter = sourceSize * 0.5f;
        Vector2 maskCenter = new Vector2(
            (MaskCenterX - 0.5f) * apertureSize.x,
            (MaskCenterY - 0.5f) * apertureSize.y);
        Vector2 position = maskCenter +
            (sourceRectCenter - sourceCenter) * scale;
        float visibleFill =
            dominantPixels * scale / innerApertureDiameter;
        float radialSafety =
            innerApertureRadius - radiusPixels * scale;

        return new FramingLayout(
            size, position, visibleFill, radialSafety);
    }

    public static bool TryGetMetrics(Sprite sprite, out Metrics metrics)
    {
        metrics = default;
        if (sprite == null)
            return false;

        switch (sprite.name)
        {
            case "avatar_01_teal_boy":
                metrics = M(0.584254f, 0.424383f, 0.532345f, 0.936464f);
                return true;
            case "avatar_02_cap_boy":
                metrics = M(0.574586f, 0.390990f, 0.514243f, 0.900552f);
                return true;
            case "avatar_03_glasses_boy":
                metrics = M(0.526243f, 0.423044f, 0.532967f, 0.928177f);
                return true;
            case "avatar_04_blue_hair":
                metrics = M(0.497238f, 0.425414f, 0.542510f, 0.933702f);
                return true;
            case "avatar_05_ponytail_girl":
                metrics = M(0.588398f, 0.516575f, 0.584243f, 0.969613f);
                return true;
            case "avatar_06_cat_ear_girl":
                metrics = M(0.563036f, 0.518134f, 0.592793f, 0.969613f);
                return true;
            case "avatar_07_bubblegum_girl":
                metrics = M(0.513812f, 0.508883f, 0.595277f, 0.969613f);
                return true;
            case "avatar_08_gold_hoodie_girl":
                metrics = M(0.500000f, 0.509250f, 0.594975f, 0.969613f);
                return true;
            case "avatar_09_green_cap":
                metrics = M(0.551105f, 0.480602f, 0.507531f, 0.883978f);
                return true;
            case "avatar_10_silver_hair":
                metrics = M(0.513812f, 0.521033f, 0.528785f, 0.870166f);
                return true;
            case "avatar_11_black_red_hair":
                metrics = M(0.495856f, 0.494475f, 0.527083f, 0.897790f);
                return true;
            case "player_cyan_exact":
                metrics = M(0.488428f, 0.485567f, 0.524300f, 0.948891f);
                return true;
            default:
                return false;
        }
    }

    static Metrics M(
        float centerX,
        float centerY,
        float radius,
        float dominantExtent)
    {
        return new Metrics(
            new Vector2(centerX, centerY), radius, dominantExtent);
    }
}
