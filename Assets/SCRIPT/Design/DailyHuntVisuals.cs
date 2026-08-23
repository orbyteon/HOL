using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Presentation-only Daily Hunt owner. DailyHunt owns state/callbacks; this class
// reuses those controls and applies approved cartoon production sprites only.
public sealed class DailyHuntVisuals : MonoBehaviour
{
    const string BackgroundResource = "phase2a/hol_neon_reference_bg_r3";
    const string LogoResource = "reference/hol_logo_exact";
    const string TitleFrameResource = "phase2a/hol_cta_magenta_r2_9s";
    const string CardResource = "mainmenu/mainmenu_tip_frame_9s";
    const string GoldButtonResource = "mainmenu/mainmenu_cta_gold_9s";
    const string BlueButtonResource = "mainmenu/mainmenu_cta_blue_9s";
    const string MascotSixResource = "reference/mascot_6_exact";
    const string MascotSevenResource = "reference/mascot_7_exact";

    static readonly Color NearWhite = new Color(0.96f, 0.97f, 1f, 1f);
    static readonly Color DarkInk = new Color(0.07f, 0.04f, 0.16f, 1f);

    public static void Apply(Transform panel)
    {
        if (panel == null || panel.Find("DailyHuntVisualRoot") != null)
            return;

        var root = RuntimeUI.CreateObject("DailyHuntVisualRoot", panel);
        RuntimeUI.Stretch(root);
        root.transform.SetAsFirstSibling();

        var backdrop = root.AddComponent<Image>();
        SetSimpleSprite(backdrop, BackgroundResource, false);
        backdrop.raycastTarget = false;

        AddSprite(root.transform, "Logo", LogoResource,
            new Vector2(0f, 725f), new Vector2(420f, 210f));

        var ribbon = AddFrame(root.transform, "TitleRibbon", TitleFrameResource,
            new Vector2(0f, 515f), new Vector2(850f, 110f));
        var title = Find<TMP_Text>(panel, "Title");
        if (title != null)
        {
            title.transform.SetParent(ribbon.transform, false);
            Place(title, Vector2.zero, new Vector2(790f, 90f));
            title.fontSize = 38f;
            title.fontStyle = FontStyles.Bold;
            title.color = NearWhite;
            title.alignment = TextAlignmentOptions.Center;
        }

        var card = Find<Transform>(panel, "Card");
        if (card != null)
        {
            var image = card.GetComponent<Image>();
            if (image == null) image = card.gameObject.AddComponent<Image>();
            SetSlicedSprite(image, CardResource, 2f);
            image.raycastTarget = false;
            card.SetParent(root.transform, false);
            Place(card, new Vector2(0f, -50f), new Vector2(900f, 1240f));
        }

        RestyleText(Find<TMP_Text>(panel, "Status"), 34f);
        Place(Find<TMP_Text>(panel, "Status"), new Vector2(0f, 380f),
            new Vector2(780f, 130f));
        RestyleText(Find<TMP_Text>(panel, "Trail"), 29f);
        Place(Find<TMP_Text>(panel, "Trail"), new Vector2(0f, 170f),
            new Vector2(800f, 80f));

        var input = Find<TMP_InputField>(panel, "GuessInput");
        Place(input, new Vector2(0f, 15f), new Vector2(430f, 100f));
        StyleInput(input);

        var submit = Find<Button>(panel, "SubmitGuessButton");
        Place(submit, new Vector2(0f, -120f), new Vector2(480f, 96f));
        StyleButton(submit, GoldButtonResource, DarkInk);

        var revive = Find<Button>(panel, "ReviveButton");
        Place(revive, new Vector2(0f, -265f), new Vector2(640f, 92f));
        StyleButton(revive, GoldButtonResource, DarkInk);

        var share = Find<Button>(panel, "ShareButton");
        Place(share, new Vector2(0f, -265f), new Vector2(480f, 92f));
        StyleButton(share, BlueButtonResource, DarkInk);

        var streak = Find<TMP_Text>(panel, "Streak");
        RestyleText(streak, 28f);
        Place(streak, new Vector2(0f, -410f), new Vector2(650f, 50f));

        var close = Find<Button>(panel, "CloseButton");
        Place(close, new Vector2(0f, -550f), new Vector2(280f, 78f));
        StyleButton(close, BlueButtonResource, DarkInk);

        AddSprite(root.transform, "MascotSix", MascotSixResource,
            new Vector2(-420f, -790f), new Vector2(190f, 220f));
        AddSprite(root.transform, "MascotSeven", MascotSevenResource,
            new Vector2(420f, -790f), new Vector2(190f, 220f));

        SetActive(Find<Transform>(panel, "ExactDailyLogo"), false);
        SetActive(Find<Transform>(panel, "ExactDailySeven"), false);
        SetActive(Find<Transform>(panel, "ExactDailyThree"), false);
        SetActive(Find<Transform>(panel, "DailyHuntBackdrop"), false);
    }

    static GameObject AddFrame(Transform parent, string name, string resource,
        Vector2 position, Vector2 size)
    {
        var go = RuntimeUI.CreateObject(name, parent);
        Place(go.transform, position, size);
        var image = go.AddComponent<Image>();
        SetSlicedSprite(image, resource, 2f);
        image.raycastTarget = false;
        return go;
    }

    static void StyleInput(TMP_InputField input)
    {
        if (input == null) return;
        var image = input.GetComponent<Image>();
        if (image == null) image = input.gameObject.AddComponent<Image>();
        SetSlicedSprite(image, CardResource, 2f);
        image.raycastTarget = true;
        if (input.textComponent != null)
        {
            input.textComponent.fontSize = 34f;
            input.textComponent.color = NearWhite;
            input.textComponent.alignment = TextAlignmentOptions.Center;
        }
        var placeholder = input.placeholder as TMP_Text;
        if (placeholder != null)
        {
            placeholder.fontSize = 30f;
            placeholder.color = new Color(0.82f, 0.82f, 0.92f, 0.80f);
            placeholder.alignment = TextAlignmentOptions.Center;
        }
    }

    static void StyleButton(Button button, string resource, Color labelColor)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image == null) image = button.gameObject.AddComponent<Image>();
        SetSlicedSprite(image, resource, 2f);
        image.raycastTarget = true;
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.80f, 0.84f, 0.94f, 1f);
        colors.disabledColor = new Color(0.56f, 0.58f, 0.68f, 0.72f);
        colors.fadeDuration = 0.06f;
        colors.colorMultiplier = 1f;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = colors;

        var text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.gameObject.SetActive(true);
            text.fontStyle = FontStyles.Bold;
            text.color = labelColor;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 24f;
            text.fontSizeMax = 34f;
        }
    }

    static void RestyleText(TMP_Text text, float size)
    {
        if (text == null) return;
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(22f, size - 8f);
        text.fontSizeMax = size;
        text.color = NearWhite;
        text.alignment = TextAlignmentOptions.Center;
    }

    static void Place(Transform target, Vector2 position, Vector2 size)
    {
        if (target == null) return;
        var rect = target as RectTransform;
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        RuntimeUI.ClampToSafeArea(rect, size, position);
    }

    static void Place(Component target, Vector2 position, Vector2 size)
    {
        Place(target == null ? null : target.transform, position, size);
    }

    static T Find<T>(Transform parent, string name) where T : Component
    {
        foreach (var item in parent.GetComponentsInChildren<T>(true))
            if (item.name == name) return item;
        return null;
    }

    static void SetActive(Transform target, bool active)
    {
        if (target != null) target.gameObject.SetActive(active);
    }

    static Image AddSprite(Transform parent, string name, string resource,
        Vector2 position, Vector2 size)
    {
        var sprite = Resources.Load<Sprite>(resource);
        if (sprite == null)
        {
            Debug.LogError("[DailyHuntVisuals] Missing Resources/" + resource + ".");
            return null;
        }
        var go = RuntimeUI.CreateObject(name, parent);
        Place(go.transform, position, size);
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    static void SetSimpleSprite(Image image, string resource, bool preserveAspect)
    {
        var sprite = Resources.Load<Sprite>(resource);
        if (sprite == null)
        {
            Debug.LogError("[DailyHuntVisuals] Missing Resources/" + resource + ".");
            return;
        }
        image.enabled = true;
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
    }

    static void SetSlicedSprite(Image image, string resource, float ppu)
    {
        var sprite = Resources.Load<Sprite>(resource);
        if (sprite == null)
        {
            Debug.LogError("[DailyHuntVisuals] Missing Resources/" + resource + ".");
            return;
        }
        image.enabled = true;
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = ppu;
        image.preserveAspect = false;
        image.color = Color.white;
    }
}
