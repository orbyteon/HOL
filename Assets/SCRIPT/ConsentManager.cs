using UnityEngine;
using UnityEngine.UI;
using TMPro;

// First-launch ads permission dialog. A decline means LevelPlay is not
// initialized at all on the next launch and no ads are shown this session.
[RequireComponent(typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster))]
public class ConsentManager : MonoBehaviour
{
    public GameObject consentPanel;
    public AdsManager adsManager;

    const string ConsentPrefKey = "AdsConsent";

    void Start()
    {
        bool alreadyAnswered = PlayerPrefs.HasKey(ConsentPrefKey);

        if (consentPanel == null && !alreadyAnswered)
            consentPanel = BuildRuntimeDialog();

        if (consentPanel != null)
            consentPanel.SetActive(!alreadyAnswered);

        if (adsManager == null)
            adsManager = FindFirstObjectByType<AdsManager>();
    }

    // Method names are kept for scene compatibility; the choice now controls
    // whether the ads SDK may initialize, not merely ad personalization.
    public void AcceptPersonalized() { Choose(true); }
    public void DeclinePersonalized() { Choose(false); }

    public void ReopenConsent()
    {
        if (consentPanel == null)
            consentPanel = BuildRuntimeDialog();

        consentPanel.SetActive(true);
    }

    void Choose(bool consent)
    {
        if (consentPanel != null)
            consentPanel.SetActive(false);

        PlayerPrefs.SetInt(ConsentPrefKey, consent ? 1 : 0);
        PlayerPrefs.Save();

        if (adsManager == null)
            adsManager = FindFirstObjectByType<AdsManager>();

        if (adsManager != null)
            adsManager.OnConsentChosen(consent);
        else
            Debug.LogError("ConsentManager: no AdsManager in scene — privacy choice saved, but ad state cannot update this session.");
    }

    GameObject BuildRuntimeDialog()
    {
        var panel = CreateUIObject("ConsentPanel", transform);
        Stretch(panel);
        var bg = panel.AddComponent<Image>();
        bg.color = ConsumerTokens.WithAlpha(ConsumerTokens.Background0, 0.92f);
        bg.raycastTarget = true;

        // The first dialog a new player ever sees, so it is the first place
        // the Consumer First board has to hold: framed card, token surface.
        var card = NeonFrame.Frame(panel.transform, "Card", Vector2.zero,
            new Vector2(600f, 420f), ConsumerTokens.Cyan, 0.97f, true,
            ConsumerTokens.Surface);

        var message = CreateText(card.transform, "Message",
            L10n.Get("consent_message"), 34, new Vector2(0f, 60f));
        Localize(message, "consent_message");

        var yes = CreateButton(card.transform, "YesButton", L10n.Get("yes"),
            new Vector2(0f, -80f), ConsumerTokens.Cyan, new Color(0.10f, 0.09f, 0.18f));
        yes.onClick.AddListener(AcceptPersonalized);
        Localize(yes.GetComponentInChildren<TMP_Text>(true), "yes");

        var no = CreateButton(card.transform, "NoButton", L10n.Get("no"),
            new Vector2(0f, -170f), ConsumerTokens.SurfaceElevated);
        no.onClick.AddListener(DeclinePersonalized);
        Localize(no.GetComponentInChildren<TMP_Text>(true), "no");

        return panel;
    }

    static void Localize(TMP_Text text, string key)
    {
        if (text == null) return;
        var localized = text.GetComponent<LocalizedText>();
        if (localized == null)
            localized = text.gameObject.AddComponent<LocalizedText>();
        localized.key = key;
    }

    static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(GameObject go)
    {
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string content, int fontSize, Vector2 position,
        Color? color = null)
    {
        var go = CreateUIObject(name, parent);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(520f, 160f);
        rect.anchoredPosition = position;

        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color ?? new Color(0.91f, 0.93f, 1f);
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        return text;
    }

    static Button CreateButton(Transform parent, string name, string label, Vector2 position, Color color,
        Color? labelColor = null)
    {
        var go = CreateUIObject(name, parent);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(300f, 70f);
        rect.anchoredPosition = position;

        var image = go.AddComponent<Image>();
        image.sprite = RuntimeUI.RoundedRectSprite;
        image.type = Image.Type.Sliced;
        image.color = color;

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;

        CreateText(go.transform, "Label", label, 30, Vector2.zero, labelColor)
            .GetComponent<RectTransform>()
            .FillParent();

        RuntimeUI.AttachJuice(button);
        return button;
    }
}

static class RectTransformConsentExtensions
{
    public static void FillParent(this RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
