using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Presentation-only fallback owner for the simulated Solo search panel.
// FakeMatchmaking retains timing/state/callback ownership. This screen uses only
// approved cartoon production sprites; the old generated radar visual is gone.
public sealed class SoloSearchVisuals : MonoBehaviour
{
    const string BackgroundResource = "phase2a/hol_neon_reference_bg_r3";
    const string LogoResource = "reference/hol_logo_exact";
    const string TitleFrameResource = "phase2a/hol_cta_magenta_r2_9s";
    const string CardResource = "mainmenu/mainmenu_cta_blue_9s";
    const string CancelResource = "mainmenu/mainmenu_cta_blue_9s";
    const string PlayerResource = "reference/player_cyan_exact";
    const string OpponentResource = "reference/char_girl_exact";
    const string VsResource = "reference/board_vs_burst_exact";
    const string RocketResource = "reference/board_rocket_exact";
    const string MascotSixResource = "reference/mascot_6_exact";
    const string MascotSevenResource = "reference/mascot_7_exact";

    static readonly Color NearWhite = new Color(0.96f, 0.97f, 1f, 1f);

    Button cancelButton;
    bool cancelStyled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        var matchmaking = Object.FindObjectOfType<FakeMatchmaking>();
        Install(matchmaking);
    }

    public static void Install(FakeMatchmaking matchmaking)
    {
        if (matchmaking == null || matchmaking.searchingPanel == null) return;
        var panel = matchmaking.searchingPanel.transform;
        if (panel.GetComponent<SoloSearchVisuals>() == null)
            panel.gameObject.AddComponent<SoloSearchVisuals>();
    }

    void Awake()
    {
        Build();
    }

    void LateUpdate()
    {
        if (cancelButton == null)
            cancelButton = Find<Button>(transform, "CancelButton");
        if (cancelButton == null) return;
        if (!cancelStyled)
        {
            StyleCancel(cancelButton);
            cancelStyled = true;
        }
        var rect = cancelButton.transform as RectTransform;
        if (rect == null) return;
        Place(rect, new Vector2(0f, -680f), new Vector2(480f, 100f));
        RuntimeUI.ClampToSafeArea(rect, rect.sizeDelta, rect.anchoredPosition);
    }

    void Build()
    {
        if (transform.Find("SoloSearchVisualRoot") != null) return;

        var canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        var root = RuntimeUI.CreateObject("SoloSearchVisualRoot", transform);
        RuntimeUI.Stretch(root);
        root.transform.SetAsFirstSibling();

        var background = root.AddComponent<Image>();
        SetSimpleSprite(background, BackgroundResource, false);
        background.raycastTarget = false;

        AddSprite(root.transform, "Logo", LogoResource,
            new Vector2(0f, 700f), new Vector2(430f, 220f));

        var ribbon = AddFrame(root.transform, "TitleRibbon", TitleFrameResource,
            new Vector2(0f, 510f), new Vector2(860f, 120f));
        var ribbonTitle = RuntimeUI.CreateText(ribbon.transform, "Title",
            L10n.Get("solo_search_title"), 44, Vector2.zero,
            new Vector2(800f, 95f), NearWhite);
        ribbonTitle.fontStyle = FontStyles.Bold;
        RuntimeUI.Localize(ribbonTitle, "solo_search_title");

        var card = AddFrame(root.transform, "SearchCard", CardResource,
            new Vector2(0f, 100f), new Vector2(920f, 650f));
        AddSprite(card.transform, "Player", PlayerResource,
            new Vector2(-285f, 25f), new Vector2(340f, 390f));
        AddSprite(card.transform, "Opponent", OpponentResource,
            new Vector2(285f, 25f), new Vector2(340f, 390f));
        AddSprite(card.transform, "VsBurst", VsResource,
            new Vector2(0f, 80f), new Vector2(170f, 170f));
        AddSprite(card.transform, "Rocket", RocketResource,
            new Vector2(0f, -180f), new Vector2(190f, 190f));

        var matchmaking = Object.FindObjectOfType<FakeMatchmaking>();
        var searchText = matchmaking == null ? null : matchmaking.searchingText;
        if (searchText != null)
        {
            searchText.transform.SetParent(card.transform, false);
            searchText.gameObject.SetActive(true);
            searchText.fontSize = 38f;
            searchText.enableAutoSizing = true;
            searchText.fontSizeMin = 28f;
            searchText.fontSizeMax = 38f;
            searchText.color = NearWhite;
            searchText.alignment = TextAlignmentOptions.Center;
            Place(searchText.rectTransform, new Vector2(0f, -250f),
                new Vector2(650f, 100f));
        }

        AddSprite(root.transform, "MascotSix", MascotSixResource,
            new Vector2(-420f, -790f), new Vector2(190f, 220f));
        AddSprite(root.transform, "MascotSeven", MascotSevenResource,
            new Vector2(420f, -790f), new Vector2(190f, 220f));

        // Old reference/reskin objects may still exist in a historical scene
        // serialization. Hide them; do not layer on top of current production.
        SetActive(Find<Transform>(transform, "ExactSearchingLogo"), false);
        SetActive(Find<Transform>(transform, "ExactSearchingPlayer"), false);
        SetActive(Find<Transform>(transform, "ExactSearchingOpponent"), false);
        SetActive(Find<Transform>(transform, "ExactSearchingVs"), false);
        SetActive(Find<Transform>(transform, "Radar"), false);
    }

    static GameObject AddFrame(Transform parent, string name, string resource,
        Vector2 position, Vector2 size)
    {
        var go = RuntimeUI.CreateObject(name, parent);
        Place(go.transform as RectTransform, position, size);
        var image = go.AddComponent<Image>();
        SetSlicedSprite(image, resource, 2f);
        image.raycastTarget = false;
        return go;
    }

    static void StyleCancel(Button button)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image == null) image = button.gameObject.AddComponent<Image>();
        SetSlicedSprite(image, CancelResource, 2f);
        image.raycastTarget = true;
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.80f, 0.84f, 0.94f, 1f);
        colors.disabledColor = new Color(0.55f, 0.56f, 0.64f, 0.72f);
        colors.fadeDuration = 0.06f;
        colors.colorMultiplier = 1f;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = colors;

        var text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.gameObject.SetActive(true);
            text.fontSize = 34f;
            text.fontStyle = FontStyles.Bold;
            text.color = NearWhite;
            text.alignment = TextAlignmentOptions.Center;
        }
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
            Debug.LogError("[SoloSearchVisuals] Missing Resources/" + resource + ".");
            return null;
        }
        var go = RuntimeUI.CreateObject(name, parent);
        Place(go.transform as RectTransform, position, size);
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
            Debug.LogError("[SoloSearchVisuals] Missing Resources/" + resource + ".");
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
            Debug.LogError("[SoloSearchVisuals] Missing Resources/" + resource + ".");
            return;
        }
        image.enabled = true;
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = ppu;
        image.preserveAspect = false;
        image.color = Color.white;
    }

    static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }
}
