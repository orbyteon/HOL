using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Sole presentation owner for the production Settings page.
// All interactive controls are the existing scene/runtime controls; this class
// only seats and skins them with approved production sprites at alpha 1.
[DefaultExecutionOrder(2400)]
public sealed class SettingsVisuals : MonoBehaviour
{
    public const string RootName = "SettingsVisualRoot";
    public const string SafeRootName = "SettingsSafeRoot";
    public const string ShellName = "SettingsReferenceShell";

    const string BackgroundResource = "settings/hol_settings_bg_r1";
    const string LogoResource = "reference/hol_logo_exact";
    const string MascotSixResource = "reference/mascot_6_exact";
    const string MascotSevenResource = "reference/mascot_7_exact";
    const string PlayerIconResource = "settings/settings_icon_player_3d";
    const string LanguageIconResource = "settings/settings_icon_language_3d";
    const string MusicIconResource = "settings/settings_icon_music_3d";
    const string DifficultyIconResource = "settings/settings_icon_difficulty_3d";
    const string PrivacyIconResource = "settings/settings_icon_privacy_3d";
    const string BlueButtonResource = "mainmenu/mainmenu_cta_blue_9s";
    const string GoldButtonResource = "mainmenu/mainmenu_cta_gold_9s";
    const string NeutralButtonResource = "mainmenu/mainmenu_tip_frame_9s";
    const string MagentaButtonResource = "mainmenu/mainmenu_cta_magenta_9s";
    const string PlayerChipResource = "mainmenu/mainmenu_player_chip_frame_9s";
    const string ChevronResource = "phase2a/hol_chevron_r2";
    const string DisplayFontResource = "phase2a/fonts/HOL Menu Display SDF";
    const string BodyFontResource = "phase2a/fonts/HOL Menu Body SDF";
    const string DifficultyPrefKey = "AIDifficulty";
    const string ButtonStateOverlayName = "SettingsButtonStateOverlay";

    const float ReferenceWidth = 1080f;
    const float ReferenceHeight = 1920f;

    static readonly Color NearWhite = new Color(0.96f, 0.97f, 1f, 1f);
    static readonly Color Cyan = new Color(0.04f, 0.88f, 1f, 1f);
    static readonly Color DarkInk = new Color(0.045f, 0.025f, 0.12f, 1f);

    RectTransform root;
    RectTransform safeRoot;
    RectTransform shell;
    MenuManager menu;
    TMP_FontAsset displayFont;
    TMP_FontAsset bodyFont;
    TMP_Text chipName;
    TMP_Text chipStreak;
    TMP_Text musicStateText;
    Button englishButton;
    Button greekButton;
    readonly Button[] difficultyButtons = new Button[4];
    Toggle musicToggle;
    bool built;
    bool subscribed;
    int waitFrames;
    float nextRefresh;

    public bool IsReady => built && root != null && safeRoot != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        foreach (var sceneRoot in scene.GetRootGameObjects())
        {
            foreach (var canvas in sceneRoot.GetComponentsInChildren<Canvas>(true))
            {
                if (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace)
                    continue;
                if (canvas.GetComponent<SettingsVisuals>() == null)
                    canvas.gameObject.AddComponent<SettingsVisuals>();
                return;
            }
        }
    }

    void OnEnable()
    {
        if (subscribed) return;
        L10n.OnLanguageChanged += OnLanguageChanged;
        subscribed = true;
    }

    void OnDisable()
    {
        if (!subscribed) return;
        L10n.OnLanguageChanged -= OnLanguageChanged;
        subscribed = false;
    }

    void OnDestroy()
    {
        OnDisable();
    }

    IEnumerator Start()
    {
        while (!built && waitFrames++ < 240)
        {
            menu = FindObjectOfType<MenuManager>();
            if (ControlsReady())
            {
                Build();
                break;
            }
            yield return null;
        }

        if (!built)
            Debug.LogError("[SettingsVisuals] Required production controls/assets never became ready.");
    }

    void LateUpdate()
    {
        if (root == null || menu == null || menu.settingsPanel == null) return;
        bool visible = menu.settingsPanel.activeSelf;
        if (root.gameObject.activeSelf != visible)
        {
            root.gameObject.SetActive(visible);
            if (visible) RefreshPresentation();
        }
        if (visible && Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + 0.25f;
            RefreshPresentation();
        }
    }

    void OnLanguageChanged()
    {
        if (built) RefreshPresentation();
    }

    void Build()
    {
        if (built || menu == null || menu.settingsPanel == null) return;

        displayFont = Resources.Load<TMP_FontAsset>(DisplayFontResource);
        bodyFont = Resources.Load<TMP_FontAsset>(BodyFontResource);
        if (!RequiredAssetsReady() || displayFont == null || bodyFont == null)
        {
            Debug.LogError("[SettingsVisuals] Missing approved Settings artwork/fonts.");
            return;
        }

        built = true;
        var page = menu.settingsPanel.transform as RectTransform;
        if (page != null) page.localScale = Vector3.one;
        var pageImage = menu.settingsPanel.GetComponent<Image>();
        if (pageImage != null)
        {
            pageImage.enabled = false;
            pageImage.raycastTarget = false;
        }

        root = EnsureRect(menu.settingsPanel.transform, RootName);
        Stretch(root);
        root.SetAsFirstSibling();

        var background = EnsureImage(root, "SettingsReferenceBackground");
        Stretch(background.rectTransform);
        SetSimpleSprite(background, BackgroundResource, false);

        safeRoot = EnsureRect(root, SafeRootName);
        Stretch(safeRoot);
        var canvas = GetComponent<Canvas>();
        if (canvas != null)
            ResponsiveSafeAreaRoot.Attach(safeRoot,
                canvas.transform as RectTransform,
                new Vector2(ReferenceWidth, ReferenceHeight));

        AddSprite(safeRoot, "SettingsLogo", LogoResource,
            new Vector2(0f, 740f), new Vector2(500f, 255f));
        BuildBackButton();
        BuildPlayerChip();
        BuildTitle();
        BuildShell();
        BuildMascots();
        HideLegacyPresentation();
        RefreshPresentation();
    }

    void BuildBackButton()
    {
        var back = Find<Button>(menu.settingsPanel.transform, "Buttonback");
        if (back == null) return;
        Seat(back.transform, safeRoot, new Vector2(-455f, 812f),
            new Vector2(124f, 124f));
        HideChildGraphics(back.transform);
        StyleButton(back, NeutralButtonResource, false, 30f);
        var icon = AddSprite(back.transform, "BackIcon", ChevronResource,
            Vector2.zero, new Vector2(62f, 78f));
        if (icon != null)
            icon.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
    }

    void BuildPlayerChip()
    {
        var go = RuntimeUI.CreateObject("SettingsPlayerChip", safeRoot);
        Place(go.transform as RectTransform, new Vector2(356f, 812f),
            new Vector2(345f, 124f));
        var chip = go.AddComponent<Image>();
        SetProductionImage(chip, PlayerChipResource, 1f);
        chip.raycastTarget = false;

        AddSprite(go.transform, "PlayerAvatar", "reference/player_cyan_exact",
            new Vector2(-126f, -2f), new Vector2(76f, 82f));

        chipName = AddText(go.transform, "PlayerName", "", 31,
            new Vector2(35f, 23f), new Vector2(210f, 46f), NearWhite,
            TextAlignmentOptions.Center, ResponsiveTextRole.Heading, bodyFont);
        chipStreak = AddText(go.transform, "Streak", "", 30,
            new Vector2(35f, -27f), new Vector2(170f, 40f), NearWhite,
            TextAlignmentOptions.Center, ResponsiveTextRole.Action, bodyFont);
    }

    void BuildTitle()
    {
        var title = EnsureImage(safeRoot, "SettingsReferenceTitle");
        SetProductionImage(title, MagentaButtonResource, 2f);
        title.raycastTarget = false;
        Place(title.rectTransform, new Vector2(0f, 540f),
            new Vector2(600f, 118f));

        var copy = AddLocalized(title.transform, "SettingsReferenceTitleText",
            "settings_title_display", 58, Vector2.zero, new Vector2(520f, 90f),
            NearWhite, TextAlignmentOptions.Center,
            ResponsiveTextRole.Heading, displayFont);
        copy.enableAutoSizing = true;
        copy.fontSizeMin = 42f;
        copy.fontSizeMax = 58f;
    }

    void BuildShell()
    {
        var shellImage = EnsureImage(safeRoot, ShellName);
        SetProductionImage(shellImage, NeutralButtonResource, 2f);
        shellImage.raycastTarget = false;
        shell = shellImage.rectTransform;
        Place(shell, new Vector2(0f, -80f), new Vector2(970f, 1080f));

        BuildNameRow(390f);
        BuildLanguageRow(180f);
        BuildMusicRow(-30f);
        BuildDifficultyRow(-240f);
        BuildPrivacyRow(-450f);
    }

    RectTransform BuildRow(string name, string key, string iconResource,
        float y, int labelSize = 36, float labelY = 0f)
    {
        var row = EnsureImage(shell, name);
        SetProductionImage(row, NeutralButtonResource, 2f);
        row.raycastTarget = false;
        var rect = row.rectTransform;
        Place(rect, new Vector2(0f, y), new Vector2(890f, 190f));

        AddSprite(row.transform, name + "Icon", iconResource,
            new Vector2(-365f, 0f), new Vector2(128f, 128f));
        var label = AddLocalized(row.transform, name + "Label", key, labelSize,
            new Vector2(-105f, labelY), new Vector2(360f, 64f), NearWhite,
            TextAlignmentOptions.Left, ResponsiveTextRole.Heading, displayFont);
        label.enableWordWrapping = false;
        return rect;
    }

    void BuildNameRow(float y)
    {
        var row = BuildRow("SettingsNameRow", "settings_player_name",
            PlayerIconResource, y, labelY: 38f);
        var input = Find<TMP_InputField>(menu.settingsPanel.transform,
            "InputField (TMP)");
        Seat(input == null ? null : input.transform, row,
            new Vector2(-60f, -42f), new Vector2(460f, 84f));
        if (input != null) StyleInput(input);

        var save = Find<Button>(menu.settingsPanel.transform, "Buttonsave");
        Seat(save == null ? null : save.transform, row,
            new Vector2(310f, -42f), new Vector2(210f, 84f));
        if (save != null)
        {
            SetLocalizedButtonKey(save, "settings_save_display");
            StyleButton(save, BlueButtonResource, false, 30f);
        }
    }

    void BuildLanguageRow(float y)
    {
        var row = BuildRow("SettingsLanguageRow", "settings_language",
            LanguageIconResource, y);
        englishButton = Find<Button>(menu.settingsPanel.transform, "EnglishButton");
        greekButton = Find<Button>(menu.settingsPanel.transform, "GreekButton");
        Seat(englishButton == null ? null : englishButton.transform, row,
            new Vector2(35f, 0f), new Vector2(210f, 84f));
        Seat(greekButton == null ? null : greekButton.transform, row,
            new Vector2(275f, 0f), new Vector2(230f, 84f));
        StyleButton(englishButton, NeutralButtonResource, false, 30f);
        StyleButton(greekButton, NeutralButtonResource, false, 30f);
    }

    void BuildMusicRow(float y)
    {
        var row = BuildRow("SettingsMusicRow", "settings_music",
            MusicIconResource, y);
        musicToggle = Find<Toggle>(menu.settingsPanel.transform, "Toggle");
        Seat(musicToggle == null ? null : musicToggle.transform, row,
            new Vector2(290f, 0f), new Vector2(220f, 92f));
        if (musicToggle == null) return;

        HideChildGraphics(musicToggle.transform);
        var image = musicToggle.GetComponent<Image>();
        if (image == null) image = musicToggle.gameObject.AddComponent<Image>();
        SetProductionImage(image, NeutralButtonResource, 2f);
        image.raycastTarget = true;
        musicToggle.targetGraphic = image;
        musicToggle.graphic = null;

        musicStateText = AddText(musicToggle.transform, "SettingsMusicState", "", 30,
            Vector2.zero, new Vector2(180f, 68f), NearWhite,
            TextAlignmentOptions.Center, ResponsiveTextRole.Action, displayFont);
    }

    void BuildDifficultyRow(float y)
    {
        var row = BuildRow("SettingsDifficultyRow", "settings_ai_difficulty",
            DifficultyIconResource, y, labelY: 38f);
        Vector2[] positions =
        {
            new Vector2(-209f, -43f), new Vector2(-46f, -43f),
            new Vector2(117f, -43f), new Vector2(296f, -43f)
        };
        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            var button = Find<Button>(menu.settingsPanel.transform,
                "Difficulty" + i);
            difficultyButtons[i] = button;
            float width = i == 3 ? 188f : i == 1 ? 156f : 142f;
            Seat(button == null ? null : button.transform, row, positions[i],
                new Vector2(width, 82f));
            StyleButton(button, NeutralButtonResource, false, i == 3 ? 22f : 27f);
            if (button != null)
                button.onClick.AddListener(RefreshPresentation);
        }
    }

    void BuildPrivacyRow(float y)
    {
        var row = BuildRow("SettingsPrivacyRow", "settings_ads_privacy",
            PrivacyIconResource, y, 32);
        var ads = Find<Button>(menu.settingsPanel.transform, "AdsPrivacyButton");
        Seat(ads == null ? null : ads.transform, row,
            new Vector2(285f, 0f), new Vector2(210f, 84f));
        if (ads == null) return;
        SetLocalizedButtonKey(ads, "settings_change_display");
        StyleButton(ads, BlueButtonResource, false, 30f);
    }

    void BuildMascots()
    {
        AddSprite(safeRoot, "SettingsMascotSix", MascotSixResource,
            new Vector2(-385f, -745f), new Vector2(270f, 310f));
        AddSprite(safeRoot, "SettingsMascotSeven", MascotSevenResource,
            new Vector2(385f, -745f), new Vector2(270f, 310f));
    }

    void RefreshPresentation()
    {
        if (!built) return;

        string stored = PlayerPrefs.GetString("PlayerName", "");
        string player = string.IsNullOrWhiteSpace(stored)
            ? L10n.Get("player_default") : stored;
        if (chipName != null) chipName.text = player;
        if (chipStreak != null)
            chipStreak.text = L10n.Get("stats_streak") + " " + GameStats.CurrentStreak;

        var input = Find<TMP_InputField>(root, "InputField (TMP)");
        if (input != null && !input.isFocused &&
            string.IsNullOrWhiteSpace(input.text))
            input.SetTextWithoutNotify(player);

        bool english = L10n.Current == L10n.Language.English;
        SetButtonSelection(englishButton, english);
        SetButtonSelection(greekButton, !english);

        int difficulty = Mathf.Clamp(
            PlayerPrefs.GetInt(DifficultyPrefKey, 1), 0, 3);
        for (int i = 0; i < difficultyButtons.Length; i++)
            SetButtonSelection(difficultyButtons[i], i == difficulty);

        if (musicToggle != null)
        {
            var image = musicToggle.GetComponent<Image>();
            SetProductionImage(image,
                musicToggle.isOn ? GoldButtonResource : NeutralButtonResource, 2f);
            if (musicStateText != null)
            {
                musicStateText.text = musicToggle.isOn ? L10n.Get("yes") : L10n.Get("no");
                musicStateText.color = musicToggle.isOn ? DarkInk : NearWhite;
            }
        }
    }

    void StyleInput(TMP_InputField input)
    {
        var image = input.GetComponent<Image>();
        if (image == null) image = input.gameObject.AddComponent<Image>();
        SetProductionImage(image, NeutralButtonResource, 2f);
        image.raycastTarget = true;

        if (input.textComponent != null)
        {
            input.textComponent.font = bodyFont;
            input.textComponent.fontSize = 32f;
            input.textComponent.fontStyle = FontStyles.Normal;
            input.textComponent.color = NearWhite;
            input.textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            ResponsiveTextPolicy.Configure(input.textComponent,
                ResponsiveTextRole.Input, 32f);
        }
        var placeholder = input.placeholder as TMP_Text;
        if (placeholder != null)
        {
            placeholder.font = bodyFont;
            placeholder.fontSize = 30f;
            placeholder.color = new Color(0.78f, 0.80f, 0.92f, 0.82f);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        }
    }

    void StyleButton(Button button, string artwork, bool selected, float fontSize)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image == null) image = button.gameObject.AddComponent<Image>();
        SetProductionImage(image, artwork, 2f);
        image.raycastTarget = true;
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.82f, 0.86f, 0.96f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.56f, 0.58f, 0.68f, 0.72f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.06f;
        button.colors = colors;

        var text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.gameObject.SetActive(true);
            StretchInset(text.rectTransform, 12f, 7f);
            text.font = displayFont;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = selected ? DarkInk : NearWhite;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.margin = new Vector4(8f, 3f, 8f, 3f);
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            ResponsiveTextPolicy.Configure(text, ResponsiveTextRole.Action, fontSize);
            EnsureTextShadow(text,
                selected
                    ? new Color(0.32f, 0.16f, 0f, 0.35f)
                    : new Color(0.01f, 0f, 0.05f, 0.62f),
                new Vector2(2f, -3f));
        }

        var overlay = EnsureButtonStateOverlay(button);
        var feedback = button.GetComponent<SettingsButtonFeedback>();
        if (feedback == null)
            feedback = button.gameObject.AddComponent<SettingsButtonFeedback>();
        feedback.Bind(button, overlay);
    }

    void SetButtonSelection(Button button, bool selected)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        SetProductionImage(image,
            selected ? GoldButtonResource : NeutralButtonResource, 2f);
        var text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.color = selected ? DarkInk : NearWhite;
        var overlay = button.transform.Find(ButtonStateOverlayName)
            ?.GetComponent<Image>();
        if (overlay != null)
        {
            overlay.sprite = image.sprite;
            overlay.type = Image.Type.Sliced;
            overlay.pixelsPerUnitMultiplier = 2f;
        }
    }

    static void SetProductionImage(Image image, string resource, float ppu)
    {
        if (image == null) return;
        var sprite = Resources.Load<Sprite>(resource);
        if (sprite == null)
        {
            Debug.LogError("[SettingsVisuals] Missing Resources/" + resource + ".");
            return;
        }
        image.enabled = true;
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = ppu;
        image.preserveAspect = false;
        image.color = Color.white;
    }

    static void SetSimpleSprite(Image image, string resource, bool preserveAspect)
    {
        if (image == null) return;
        var sprite = Resources.Load<Sprite>(resource);
        if (sprite == null)
        {
            Debug.LogError("[SettingsVisuals] Missing Resources/" + resource + ".");
            return;
        }
        image.enabled = true;
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
    }

    static Image EnsureButtonStateOverlay(Button button)
    {
        var existing = button.transform.Find(ButtonStateOverlayName);
        var go = existing == null
            ? RuntimeUI.CreateObject(ButtonStateOverlayName, button.transform)
            : existing.gameObject;
        RuntimeUI.Stretch(go);
        go.transform.SetAsFirstSibling();
        var overlay = go.GetComponent<Image>();
        if (overlay == null) overlay = go.AddComponent<Image>();
        var baseImage = button.GetComponent<Image>();
        overlay.sprite = baseImage == null ? null : baseImage.sprite;
        overlay.type = Image.Type.Sliced;
        overlay.pixelsPerUnitMultiplier = 2f;
        overlay.preserveAspect = false;
        overlay.color = Color.clear;
        overlay.raycastTarget = false;
        return overlay;
    }

    static void EnsureTextShadow(TMP_Text text, Color color, Vector2 distance)
    {
        if (text == null) return;
        var shadow = text.GetComponent<Shadow>();
        if (shadow == null) shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    static void SetLocalizedButtonKey(Button button, string key)
    {
        if (button == null) return;
        var text = button.GetComponentInChildren<TMP_Text>(true);
        if (text == null) return;
        var localized = text.GetComponent<LocalizedText>();
        if (localized == null)
        {
            RuntimeUI.Localize(text, key);
            localized = text.GetComponent<LocalizedText>();
        }
        if (localized != null) localized.key = key;
        text.text = L10n.Get(key);
    }

    TMP_Text AddLocalized(Transform parent, string name, string key,
        int size, Vector2 position, Vector2 dimensions, Color color,
        TextAlignmentOptions alignment, ResponsiveTextRole role,
        TMP_FontAsset font)
    {
        var text = AddText(parent, name, L10n.Get(key), size, position,
            dimensions, color, alignment, role, font);
        RuntimeUI.Localize(text, key);
        return text;
    }

    static TMP_Text AddText(Transform parent, string name, string content,
        int size, Vector2 position, Vector2 dimensions, Color color,
        TextAlignmentOptions alignment, ResponsiveTextRole role,
        TMP_FontAsset font)
    {
        var text = RuntimeUI.CreateText(parent, name, content, size,
            position, dimensions, color);
        text.font = font;
        text.alignment = alignment;
        text.fontStyle = FontStyles.Bold;
        EnsureTextShadow(text, new Color(0.01f, 0f, 0.05f, 0.62f),
            new Vector2(2f, -3f));
        ResponsiveTextPolicy.Configure(text, role, size);
        return text;
    }

    static Image AddSprite(Transform parent, string name, string resource,
        Vector2 position, Vector2 size)
    {
        var sprite = Resources.Load<Sprite>(resource);
        if (sprite == null)
        {
            Debug.LogError("[SettingsVisuals] Missing Resources/" + resource + ".");
            return null;
        }
        var image = EnsureImage(parent, name);
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = false;
        Place(image.rectTransform, position, size);
        return image;
    }

    void HideLegacyPresentation()
    {
        var panel = menu.settingsPanel.transform;
        for (int i = panel.childCount - 1; i >= 0; i--)
        {
            var child = panel.GetChild(i);
            if (child == root) continue;
            child.gameObject.SetActive(false);
        }
    }

    bool ControlsReady()
    {
        if (menu == null || menu.settingsPanel == null || !RequiredAssetsReady())
            return false;
        var panel = menu.settingsPanel.transform;
        return Find<Button>(panel, "Buttonback") != null &&
               Find<TMP_InputField>(panel, "InputField (TMP)") != null &&
               Find<Button>(panel, "Buttonsave") != null &&
               Find<Button>(panel, "GreekButton") != null &&
               Find<Toggle>(panel, "Toggle") != null &&
               Find<Button>(panel, "Difficulty3") != null &&
               Find<Button>(panel, "AdsPrivacyButton") != null;
    }

    static bool RequiredAssetsReady()
    {
        string[] sprites =
        {
            BackgroundResource, LogoResource, MascotSixResource,
            MascotSevenResource, PlayerIconResource, LanguageIconResource,
            MusicIconResource, DifficultyIconResource, PrivacyIconResource,
            BlueButtonResource, GoldButtonResource, NeutralButtonResource,
            MagentaButtonResource, PlayerChipResource, ChevronResource,
            "reference/player_cyan_exact"
        };
        for (int i = 0; i < sprites.Length; i++)
            if (Resources.Load<Sprite>(sprites[i]) == null) return false;
        return true;
    }

    static void Seat(Transform target, Transform parent, Vector2 position,
        Vector2 size)
    {
        if (target == null || parent == null) return;
        target.SetParent(parent, false);
        Place(target as RectTransform, position, size);
        target.gameObject.SetActive(true);
    }

    static void HideChildGraphics(Transform root)
    {
        if (root == null) return;
        foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic.transform == root) continue;
            graphic.gameObject.SetActive(false);
        }
    }

    static RectTransform EnsureRect(Transform parent, string name)
    {
        var existing = parent.Find(name) as RectTransform;
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            return existing;
        }
        return RuntimeUI.CreateObject(name, parent).GetComponent<RectTransform>();
    }

    static Image EnsureImage(Transform parent, string name)
    {
        var rect = EnsureRect(parent, name);
        var image = rect.GetComponent<Image>();
        if (image == null) image = rect.gameObject.AddComponent<Image>();
        return image;
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

    static void Stretch(RectTransform rect)
    {
        if (rect == null) return;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    static void StretchInset(RectTransform rect, float x, float y)
    {
        if (rect == null) return;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(x, y);
        rect.offsetMax = new Vector2(-x, -y);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    static T Find<T>(Transform parent, string name) where T : Component
    {
        if (parent == null) return null;
        foreach (var item in parent.GetComponentsInChildren<T>(true))
            if (item.name == name) return item;
        return null;
    }
}

// Additive state feedback only. The production sprite remains the visible base;
// this component merely fades a same-sprite overlay during pointer press.
public sealed class SettingsButtonFeedback : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    Button button;
    Image overlay;

    public void Bind(Button owner, Image stateOverlay)
    {
        button = owner;
        overlay = stateOverlay;
        SetPressed(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && button.interactable) SetPressed(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetPressed(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetPressed(false);
    }

    void SetPressed(bool pressed)
    {
        if (overlay == null) return;
        overlay.color = pressed
            ? new Color(0.12f, 0.02f, 0.28f, 0.18f)
            : Color.clear;
    }
}
