using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Applies the approved reference artwork as the literal presentation baseline.
// This is deliberately a skin over the existing controllers. It does not own
// game rules, matchmaking, room state, ads, statistics, or navigation.
public sealed class ExactReferenceVisuals : MonoBehaviour
{
    const string LogoResource = "reference/hol_logo_exact";

    static readonly Color Depth = Hex(0x08, 0x06, 0x25);
    static readonly Color Surface = Hex(0x18, 0x0B, 0x48);
    static readonly Color SurfaceRaised = Hex(0x2B, 0x16, 0x72);
    static readonly Color Violet = Hex(0x76, 0x31, 0xE8);
    static readonly Color Cyan = Hex(0x00, 0xC8, 0xFF);
    static readonly Color Blue = Hex(0x08, 0x6E, 0xD9);
    static readonly Color Pink = Hex(0xF3, 0x28, 0x91);
    static readonly Color Gold = Hex(0xFF, 0xC4, 0x10);
    static readonly Color Orange = Hex(0xF4, 0x75, 0x0A);
    static readonly Color Success = Hex(0x62, 0xD6, 0x2D);
    static readonly Color NearWhite = Hex(0xF7, 0xF5, 0xFF);
    static readonly Color Muted = Hex(0xC7, 0xB9, 0xEA);
    static readonly Color Ink = Hex(0x16, 0x0D, 0x24);

    Sprite logo;
    float nextRefresh;
    int lastButtonCount = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        foreach (var canvas in FindObjectsOfType<Canvas>())
        {
            if (canvas.GetComponent<ExactReferenceVisuals>() == null)
            {
                canvas.gameObject.AddComponent<ExactReferenceVisuals>();
                return;
            }
        }
    }

    void Awake()
    {
        logo = Resources.Load<Sprite>(LogoResource);
        if (logo == null)
            Debug.LogError("[ExactReferenceVisuals] Missing Resources/" + LogoResource +
                ". The approved HOL logo cannot render.");
    }

    void OnEnable()
    {
        L10n.OnLanguageChanged += ApplyAll;
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= ApplyAll;
    }

    IEnumerator Start()
    {
        // Runtime-built PvP and Daily Hunt surfaces appear after their own
        // Start methods. Reapply through the first second, then only when the
        // hierarchy gains or loses buttons.
        for (int i = 0; i < 4; i++)
        {
            yield return null;
            ApplyAll();
        }
    }

    void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.5f;

        int count = 0;
        foreach (var canvas in FindObjectsOfType<Canvas>())
            count += canvas.GetComponentsInChildren<Button>(true).Length;

        if (count != lastButtonCount)
        {
            lastButtonCount = count;
            ApplyAll();
        }
    }

    void ApplyAll()
    {
        foreach (var canvas in FindObjectsOfType<Canvas>())
        {
            ApplyBackdrop(canvas.transform);
            StylePanels(canvas.transform);
            StyleInputs(canvas.transform);
            StyleButtons(canvas.transform);
            StyleText(canvas.transform);
        }

        var menu = FindObjectOfType<MenuManager>();
        if (menu != null && menu.mainMenuPanel != null)
            BuildMainMenu(menu.mainMenuPanel.transform);
    }

    void ApplyBackdrop(Transform canvasRoot)
    {
        var existing = DirectChild(canvasRoot, "ExactReferenceBackdrop");
        if (existing == null)
        {
            var go = RuntimeUI.CreateObject("ExactReferenceBackdrop", canvasRoot);
            RuntimeUI.Stretch(go);
            var image = go.AddComponent<Image>();
            image.sprite = ConvergingLight.VerticalGradient(Hex(0x07, 0x04, 0x1D), Hex(0x1A, 0x06, 0x43));
            image.color = Color.white;
            image.raycastTarget = false;
            go.transform.SetAsFirstSibling();
        }
    }

    void BuildMainMenu(Transform root)
    {
        if (logo != null)
        {
            var logoImage = EnsureImage(root, "ExactHOLLogo");
            logoImage.sprite = logo;
            logoImage.color = Color.white;
            logoImage.preserveAspect = true;
            logoImage.raycastTarget = false;
            Place(logoImage.rectTransform, new Vector2(0f, 535f), new Vector2(650f, 360f));
        }

        var tagline = EnsureText(root, "ExactTagline");
        tagline.text = IsGreek
            ? "ΜΑΝΤΕΨΕ ΤΟΝ ΑΡΙΘΜΟ!"
            : "GUESS THE NUMBER!";
        tagline.fontSize = IsGreek ? 49f : 56f;
        tagline.fontStyle = FontStyles.Bold;
        tagline.color = NearWhite;
        tagline.alignment = TextAlignmentOptions.Center;
        Place(tagline.rectTransform, new Vector2(0f, 322f), new Vector2(900f, 90f));

        var profile = EnsureImage(root, "ExactPlayerChip");
        profile.sprite = RuntimeUI.RoundedRectSprite;
        profile.type = Image.Type.Sliced;
        profile.color = new Color(0.03f, 0.20f, 0.48f, 0.94f);
        Place(profile.rectTransform, new Vector2(-305f, 825f), new Vector2(430f, 92f));
        EnsureOutline(profile.gameObject, Cyan, 3f);

        var profileText = EnsureText(profile.transform, "ExactPlayerChipText");
        string player = PlayerPrefs.GetString("PlayerName", L10n.Get("player_default"));
        profileText.text = player.ToUpperInvariant() + "   🔥 " + GameStats.CurrentStreak;
        profileText.fontSize = 31f;
        profileText.fontStyle = FontStyles.Bold;
        profileText.color = NearWhite;
        RuntimeUI.Stretch(profileText.gameObject);

        AddConfetti(root);

        var play = FindButton("ButtonPlay");
        if (play != null)
        {
            Place((RectTransform)play.transform, new Vector2(0f, 80f), new Vector2(850f, 145f));
            StyleButton(play, Gold, Ink, true);
            SetButtonCopy(play,
                IsGreek ? "ΠΑΙΞΕ SOLO" : "PLAY SOLO",
                IsGreek ? "Νίκησε τον προσαρμοστικό αντίπαλο" : "Beat the adaptive opponent");
        }

        var room = FindButton("ButtonPvP");
        if (room != null)
        {
            Place((RectTransform)room.transform, new Vector2(0f, -125f), new Vector2(850f, 155f));
            StyleButton(room, Blue, NearWhite, false);
            SetButtonCopy(room,
                IsGreek ? "ΠΑΙΞΕ ΜΕ ΦΙΛΟ" : "PLAY WITH A FRIEND",
                IsGreek ? "Δημιούργησε ή μπες σε ιδιωτικό δωμάτιο" : "Create or join a private room");
        }

        var daily = FindButton("DailyHuntButton");
        if (daily != null)
        {
            Place((RectTransform)daily.transform, new Vector2(0f, -330f), new Vector2(850f, 155f));
            StyleButton(daily, Orange, NearWhite, false);
            SetButtonCopy(daily,
                IsGreek ? "ΚΑΘΗΜΕΡΙΝΟ ΚΥΝΗΓΙ" : "DAILY HUNT",
                IsGreek ? "Ένας κοινός αριθμός κάθε μέρα" : "One shared number every day");
        }

        var settings = FindButton("Buttonsettings");
        if (settings != null)
        {
            Place((RectTransform)settings.transform, new Vector2(440f, 825f), new Vector2(92f, 92f));
            StyleButton(settings, SurfaceRaised, NearWhite, false);
        }

        var quit = DeepFind(root, "ButtonQuit");
        if (quit != null) quit.gameObject.SetActive(false);
        var oldStats = DeepFind(root, "StatsLabel");
        if (oldStats != null) oldStats.gameObject.SetActive(false);
    }

    static void AddConfetti(Transform root)
    {
        if (DirectChild(root, "ExactConfetti") != null) return;
        var field = RuntimeUI.CreateObject("ExactConfetti", root);
        RuntimeUI.Stretch(field);
        field.transform.SetAsFirstSibling();

        Color[] colors = { Cyan, Pink, Gold, Violet, NearWhite };
        var rng = new System.Random(47031);
        for (int i = 0; i < 28; i++)
        {
            var bit = RuntimeUI.CreateObject("Confetti" + i, field.transform);
            var rect = (RectTransform)bit.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(rng.Next(8, 20), rng.Next(16, 38));
            rect.anchoredPosition = new Vector2(rng.Next(-500, 501), rng.Next(-800, 801));
            rect.localRotation = Quaternion.Euler(0f, 0f, rng.Next(0, 180));
            var image = bit.AddComponent<Image>();
            image.color = colors[i % colors.Length];
            image.raycastTarget = false;
        }
    }

    static void StylePanels(Transform root)
    {
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            string name = image.transform.name;
            if (name == "ExactReferenceBackdrop" || name == "ExactHOLLogo" ||
                name.StartsWith("Confetti") || name == "ExactPlayerChip")
                continue;

            bool fullPanel = name.IndexOf("Panel", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                             image.transform.GetComponent<Button>() == null;
            bool card = name.IndexOf("Card", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("Frame", System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (fullPanel)
                image.color = Depth;
            else if (card)
            {
                image.sprite = RuntimeUI.RoundedRectSprite;
                image.type = Image.Type.Sliced;
                image.color = Surface;
                EnsureOutline(image.gameObject, Violet, 2f);
            }
        }
    }

    static void StyleInputs(Transform root)
    {
        foreach (var input in root.GetComponentsInChildren<TMP_InputField>(true))
        {
            var image = input.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = RuntimeUI.RoundedRectSprite;
                image.type = Image.Type.Sliced;
                image.color = SurfaceRaised;
                EnsureOutline(image.gameObject, Violet, 3f);
            }
            if (input.textComponent != null)
            {
                input.textComponent.color = NearWhite;
                input.textComponent.fontStyle = FontStyles.Bold;
            }
            var placeholder = input.placeholder as TMP_Text;
            if (placeholder != null) placeholder.color = Muted;
        }
    }

    static void StyleButtons(Transform root)
    {
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            string name = button.transform.name;
            if (name == "ButtonPlay" || name == "ButtonPvP" || name == "DailyHuntButton")
                continue;

            Color fill = SurfaceRaised;
            Color label = NearWhite;
            bool primary = false;

            if (Contains(name, "Higher")) fill = Cyan;
            else if (Contains(name, "Correct")) fill = Success;
            else if (Contains(name, "Lower")) fill = Pink;
            else if (Contains(name, "Create")) { fill = Gold; label = Ink; primary = true; }
            else if (Contains(name, "Join")) fill = Blue;
            else if (Contains(name, "Submit") || Contains(name, "Confirm") || Contains(name, "Start"))
            { fill = Gold; label = Ink; primary = true; }
            else if (Contains(name, "Share") || Contains(name, "Copy")) fill = Blue;
            else if (Contains(name, "Rematch")) { fill = Gold; label = Ink; primary = true; }
            else if (Contains(name, "Revive") || Contains(name, "Reward")) fill = Pink;
            else if (Contains(name, "Leave") || Contains(name, "Quit") || Contains(name, "Cancel")) fill = Pink;
            else if (name.StartsWith("Key_")) fill = Hex(0x39, 0x2C, 0xAE);
            else if (Contains(name, "Signal")) fill = Hex(0x2B, 0x1C, 0x69);

            StyleButton(button, fill, label, primary);
        }
    }

    static void StyleButton(Button button, Color fill, Color labelColor, bool primary)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = RuntimeUI.RoundedRectSprite;
            image.type = Image.Type.Sliced;
            image.color = fill;
            EnsureOutline(image.gameObject, primary ? Hex(0xFF, 0xE4, 0x68) : Violet, primary ? 4f : 2f);
            EnsureShadow(image.gameObject, primary ? Hex(0x9B, 0x4B, 0x00) : Hex(0x09, 0x02, 0x24), primary ? 10f : 7f);
        }

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.disabledColor = new Color(0.42f, 0.40f, 0.52f, 0.78f);
        button.colors = colors;

        foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
        {
            text.color = labelColor;
            text.fontStyle = FontStyles.Bold;
        }
    }

    static void StyleText(Transform root)
    {
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.GetComponentInParent<Button>() != null ||
                text.GetComponentInParent<TMP_InputField>() != null)
                continue;
            if (text.transform.name.StartsWith("Exact")) continue;
            text.color = NearWhite;
        }
    }

    static void SetButtonCopy(Button button, string title, string subtitle)
    {
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = title;
            label.fontSize = title.Length > 20 ? 34f : 43f;
            label.fontStyle = FontStyles.Bold;
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(760f, 64f);
            rect.anchoredPosition = new Vector2(0f, 23f);
        }

        var sub = EnsureText(button.transform, "ExactButtonSubtitle");
        sub.text = subtitle;
        sub.fontSize = IsGreek && subtitle.Length > 32 ? 21f : 24f;
        sub.fontStyle = FontStyles.Normal;
        sub.color = button.transform.name == "ButtonPlay" ? Ink : NearWhite;
        sub.alignment = TextAlignmentOptions.Center;
        Place(sub.rectTransform, new Vector2(0f, -34f), new Vector2(760f, 42f));
    }

    static TMP_Text EnsureText(Transform parent, string name)
    {
        var existing = DirectChild(parent, name);
        if (existing != null)
        {
            var found = existing.GetComponent<TMP_Text>();
            if (found != null) return found;
        }
        return RuntimeUI.CreateText(parent, name, "", 30, Vector2.zero, new Vector2(100f, 50f), NearWhite);
    }

    static Image EnsureImage(Transform parent, string name)
    {
        var existing = DirectChild(parent, name);
        if (existing != null)
        {
            var found = existing.GetComponent<Image>();
            if (found != null) return found;
        }
        var go = RuntimeUI.CreateObject(name, parent);
        return go.AddComponent<Image>();
    }

    static void EnsureOutline(GameObject go, Color color, float distance)
    {
        var outline = go.GetComponent<Outline>();
        if (outline == null) outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(distance, distance);
        outline.useGraphicAlpha = true;
    }

    static void EnsureShadow(GameObject go, Color color, float distance)
    {
        var shadow = go.GetComponent<Shadow>();
        if (shadow == null) shadow = go.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = new Vector2(0f, -distance);
        shadow.useGraphicAlpha = true;
    }

    static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    static Button FindButton(string name)
    {
        foreach (var canvas in FindObjectsOfType<Canvas>())
        {
            var hit = DeepFind(canvas.transform, name);
            if (hit != null) return hit.GetComponent<Button>();
        }
        return null;
    }

    static Transform DirectChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name) return parent.GetChild(i);
        return null;
    }

    static Transform DeepFind(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var hit = DeepFind(root.GetChild(i), name);
            if (hit != null) return hit;
        }
        return null;
    }

    static bool Contains(string value, string part)
    {
        return value.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static bool IsGreek => L10n.Current == L10n.Language.Greek;

    static Color Hex(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }
}
