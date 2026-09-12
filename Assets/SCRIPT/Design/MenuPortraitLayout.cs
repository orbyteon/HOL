using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pure construction/geometry shared by the existing menu presentation owners.
// No lifecycle, profile storage, room state or callbacks are owned here.
internal static class MenuPortraitLayout
{
    internal const string ChipResource = "solo/production/solo_player_chip_v1";
    internal static readonly Vector2 ChipSize = new Vector2(430f, 167f);
    internal static readonly Vector2 LogoSize = new Vector2(640f, 310f);
    internal static readonly Rect NameFace = new Rect(-180f, 1f, 240f, 50f);
    internal static readonly Rect ScoreFace = new Rect(-123f, -55f, 160f, 45f);

    internal static float Height(RectTransform safe)
    {
        var owner = safe == null ? null : safe.GetComponent<ResponsiveSafeAreaRoot>();
        if (owner == null || owner.LastSafeRect.height <= 0f) return 1920f;
        return owner.LastSafeRect.height / Mathf.Max(.001f, safe.localScale.y);
    }

    internal static float Top(RectTransform safe) => Height(safe) * .5f;
    internal static float Extra(RectTransform safe) => Mathf.Max(0f, Height(safe) - 1920f);
    internal static Vector2 HeaderPosition(RectTransform safe) => new Vector2(298f, Top(safe) - 103f);
    internal static Vector2 LogoPosition(RectTransform safe) => new Vector2(0f, Top(safe) - 340f);

    internal static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    internal static Image CreatePortrait(Transform chip, string name)
    {
        var aperture = RuntimeUI.CreateObject(name + "Aperture", chip).AddComponent<Image>();
        aperture.sprite = Resources.Load<Sprite>(PlayerProfileAvatarResolver.CircularApertureResourcePath);
        aperture.preserveAspect = true;
        aperture.raycastTarget = false;
        Place(aperture.rectTransform, new Vector2(138f, 0f), new Vector2(126f, 126f));
        aperture.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        var portrait = RuntimeUI.CreateObject(name, aperture.transform).AddComponent<Image>();
        portrait.raycastTarget = false;
        portrait.preserveAspect = true;
        return portrait;
    }

    internal static void PaintPortrait(Image portrait, Sprite sprite)
    {
        if (portrait == null) return;
        portrait.sprite = sprite;
        PlayerProfileAvatarFraming.Apply(portrait, portrait.transform.parent as RectTransform);
    }

    internal static void StyleName(TMP_Text text)
    {
        text.enableAutoSizing = true;
        text.fontSizeMin = 24f;
        text.fontSizeMax = 34f;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
    }

    internal static MainMenuCenteredTextRegion Region(TMP_Text text, Rect face)
        => new MainMenuCenteredTextRegion(text, face.center.x, face.center.y, face.width, face.height);
}
