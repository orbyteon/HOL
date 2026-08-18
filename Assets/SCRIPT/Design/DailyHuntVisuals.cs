using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Presentation-only Daily Hunt layer. DailyHunt owns all state and callbacks;
// this class only adds non-interactive chrome and repositions existing controls.
public sealed class DailyHuntVisuals : MonoBehaviour
{
    public static void Apply(Transform panel)
    {
        if (panel == null || panel.Find("DailyHuntVisualRoot") != null)
            return;

        var root = RuntimeUI.CreateObject("DailyHuntVisualRoot", panel);
        RuntimeUI.Stretch(root);
        root.transform.SetAsFirstSibling();

        var backdrop = root.AddComponent<Image>();
        backdrop.sprite = ConvergingLight.VerticalGradient(
            ConvergingLight.DepthTop, ConvergingLight.DepthBottom);
        backdrop.raycastTarget = false;

        AddSprite(root.transform, "Logo", "reference/hol_logo_exact",
            new Vector2(0f, 725f), new Vector2(390f, 190f));
        var ribbon = NeonFrame.Frame(root.transform, "TitleRibbon",
            new Vector2(0f, 515f), new Vector2(850f, 110f),
            ConsumerTokens.Magenta, 0.9f, true, ConsumerTokens.CardPink);
        var title = Find<TMP_Text>(panel, "Title");
        if (title != null)
        {
            title.transform.SetParent(ribbon.transform, false);
            ConvergingLight.Center(title.gameObject, Vector2.zero,
                new Vector2(790f, 90f));
            title.fontSize = 38f;
            title.color = ConvergingLight.NearWhite;
        }

        var card = Find<Transform>(panel, "Card");
        Place(card, new Vector2(0f, -50f), new Vector2(900f, 1240f));
        Place(Find<TMP_Text>(panel, "Status"), new Vector2(0f, 380f),
            new Vector2(780f, 130f));
        Place(Find<TMP_Text>(panel, "Trail"), new Vector2(0f, 170f),
            new Vector2(800f, 80f));
        Place(Find<TMP_InputField>(panel, "GuessInput"),
            new Vector2(0f, 15f), new Vector2(430f, 100f));
        Place(Find<Button>(panel, "SubmitGuessButton"),
            new Vector2(0f, -120f), new Vector2(480f, 96f));
        Place(Find<Button>(panel, "ReviveButton"),
            new Vector2(0f, -265f), new Vector2(640f, 92f));
        Place(Find<Button>(panel, "ShareButton"),
            new Vector2(0f, -265f), new Vector2(480f, 92f));
        Place(Find<TMP_Text>(panel, "Streak"), new Vector2(0f, -410f),
            new Vector2(650f, 50f));
        Place(Find<Button>(panel, "CloseButton"), new Vector2(0f, -550f),
            new Vector2(280f, 78f));

        AddSprite(panel, "MascotSix", "reference/mascot_6_exact",
            new Vector2(-420f, -790f), new Vector2(180f, 210f));
        AddSprite(panel, "MascotSeven", "reference/mascot_7_exact",
            new Vector2(420f, -790f), new Vector2(180f, 210f));
    }

    static void Place(Transform target, Vector2 position, Vector2 size)
    {
        if (target == null) return;
        var rect = target as RectTransform;
        if (rect == null) return;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
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

    static Image AddSprite(Transform parent, string name, string resource,
        Vector2 position, Vector2 size)
    {
        var sprite = Resources.Load<Sprite>(resource);
        if (sprite == null) return null;
        var go = RuntimeUI.CreateObject(name, parent);
        ConvergingLight.Center(go, position, size);
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }
}
