using UnityEngine;
using UnityEngine.UI;

// Review #9: first-launch ads-consent dialog.
//
// Two ways to use:
//   A) No wiring at all (default): on first launch the component builds a
//      simple consent dialog from code (BuildRuntimeDialog) and shows it.
//      This is the zero-setup path so the consent flow — and therefore ads
//      initialization, which is gated on it — works out of the box.
//   B) Custom UI: create a ConsentPanel in the scene, drag it + AdsManager
//      into the fields below, and wire Yes/No buttons to AcceptPersonalized /
//      DeclinePersonalized. The runtime dialog is then never built.
//
// The choice is stored in PlayerPrefs ("AdsConsent"); AdsManager initializes
// the SDK only after a choice exists.
[RequireComponent(typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster))]
public class ConsentManager : MonoBehaviour
{
    public GameObject consentPanel; // optional — built from code if null
    public AdsManager adsManager;   // optional — found in scene if null

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

    public void AcceptPersonalized()
    {
        Choose(true);
    }

    public void DeclinePersonalized()
    {
        Choose(false);
    }

    // Re-open the consent dialog from Settings so the player can change
    // their ads-privacy choice in-app (GDPR: withdrawing consent must be
    // as easy as giving it). Choosing again overwrites the stored pref and
    // updates the SDK's privacy flags; AdsManager guards against re-init.
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

        // Persist here as the single source of truth — the choice must be
        // saved even if the AdsManager reference is missing in the scene.
        PlayerPrefs.SetInt(ConsentPrefKey, consent ? 1 : 0);
        PlayerPrefs.Save();

        if (adsManager == null)
            adsManager = FindFirstObjectByType<AdsManager>();

        if (adsManager != null)
            adsManager.OnConsentChosen(consent);
        else
            Debug.LogError("ConsentManager: no AdsManager in scene — consent saved, but ads will not initialize this session.");
    }

    // ------------------------------------------------ zero-setup runtime UI

    GameObject BuildRuntimeDialog()
    {
        // Dimmed fullscreen backdrop (Converging Light: indigo night, not black).
        var panel = CreateUIObject("ConsentPanel", transform);
        Stretch(panel);
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.12f, 0.92f);
        bg.raycastTarget = true; // block taps through the dialog

        // Centered card.
        var card = CreateUIObject("Card", panel.transform);
        var cardRect = (RectTransform)card.transform;
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(600f, 420f);
        var cardImage = card.AddComponent<Image>();
        cardImage.sprite = RuntimeUI.RoundedRectSprite;
        cardImage.type = Image.Type.Sliced;
        cardImage.color = new Color(0.10f, 0.09f, 0.18f, 1f);

        // Message.
        var message = CreateText(card.transform, "Message",
            L10n.Get("consent_message"), 34, new Vector2(0f, 60f));

        // Buttons.
        var yes = CreateButton(card.transform, "YesButton", L10n.Get("yes"),
            new Vector2(0f, -80f), new Color(0.25f, 0.85f, 1f), new Color(0.10f, 0.09f, 0.18f));
        yes.onClick.AddListener(AcceptPersonalized);

        var no = CreateButton(card.transform, "NoButton", L10n.Get("no"),
            new Vector2(0f, -170f), new Color(0.16f, 0.15f, 0.26f));
        no.onClick.AddListener(DeclinePersonalized);

        return panel;
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

    static Text CreateText(Transform parent, string name, string content, int fontSize, Vector2 position,
        Color? color = null)
    {
        var go = CreateUIObject(name, parent);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(520f, 160f);
        rect.anchoredPosition = position;

        var text = go.AddComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color ?? new Color(0.91f, 0.93f, 1f); // near-white, never pure white
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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

        // A dialog reopened from Settings is built long after the startup
        // juice pass — attach press feedback + click sound here.
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
