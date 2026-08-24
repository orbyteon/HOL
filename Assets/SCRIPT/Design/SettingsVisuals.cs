using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Presentation-only owner for the production Settings page. All interactive
// controls are the original scene/runtime controls: this component only seats
// them inside the approved portrait composition and paints their visual skin.
[DefaultExecutionOrder(2400)]
public sealed class SettingsVisuals : MonoBehaviour
{
    const string RootName = "SettingsVisualRoot";
    const string SafeRootName = "SettingsSafeRoot";
    const string ShellName = "SettingsReferenceShell";
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
    const string PlayerChipResource = "mainmenu/mainmenu_player_chip_frame_9s";
    const string ChevronResource = "phase2a/hol_chevron_r2";
    const string ButtonStateOverlayName = "SettingsButtonStateOverlay";
    const string DifficultyPrefKey = "AIDifficulty";
    const float ReferenceWidth = 1080f;
    const float ReferenceHeight = 1920f;

    static readonly Color NearWhite = new Color(0.96f, 0.97f, 1f, 1f);
    static readonly Color Cyan = new Color(0.02f, 0.88f, 1f, 1f);
    static readonly Color Gold = new Color(1f, 0.70f, 0.08f, 1f);
    static readonly Color DarkInk = new Color(0.045f, 0.025f, 0.12f, 1f);

    RectTransform root;
    RectTransform safeRoot;
    RectTransform shell;
    MenuManager menu;
    CartoonThemeCatalog theme;
    TMP_Text chipName;
    TMP_Text chipStreak;
    SettingsToggleGraphic musicVisual;
    Button englishButton;
    Button greekButton;
    readonly Button[] difficultyButtons = new Button[4];
    int frames;
    bool built;
    bool subscribed;
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
        foreach (var item in scene.GetRootGameObjects())
        {
            var canvas = item.GetComponentInChildren<Canvas>(true);
            if (canvas == null || !canvas.isRootCanvas ||
                canvas.GetComponent<SettingsVisuals>() != null)
                continue;
            canvas.gameObject.AddComponent<SettingsVisuals>();
            return;
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
        while (!built && frames++ < 180)
        {
            menu = FindObjectOfType<MenuManager>();
            if (ControlsReady())
            {
                Build();
                break;
            }
            yield return null;
        }
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
        built = true;
        theme = HolTheme.Current;
        if (theme == null || !theme.IsComplete)
        {
            Debug.LogError("[SettingsVisuals] Cartoon theme catalog is incomplete.");
            built = false;
            return;
        }

        var page = menu.settingsPanel.transform as RectTransform;
        if (page != null) page.localScale = Vector3.one;
        var pageImage = menu.settingsPanel.GetComponent<Image>();
        if (pageImage != null)
        {
            pageImage.enabled = false;
            pageImage.raycastTarget = false;
        }

        root = RuntimeUI.CreateObject(RootName, menu.settingsPanel.transform)
            .GetComponent<RectTransform>();
        RuntimeUI.Stretch(root.gameObject);
        root.SetAsFirstSibling();

        AddBackground(root);
        safeRoot = RuntimeUI.CreateObject(SafeRootName, root)
            .GetComponent<RectTransform>();
        RuntimeUI.Stretch(safeRoot.gameObject);
        var canvas = GetComponent<Canvas>();
        if (canvas != null)
            ResponsiveSafeAreaRoot.Attach(safeRoot,
                canvas.transform as RectTransform,
                new Vector2(ReferenceWidth, ReferenceHeight));

        AddSprite(safeRoot, "SettingsLogo", theme.shared.logo,
            new Vector2(0f, 740f), new Vector2(500f, 255f));
        BuildBackButton();
        BuildPlayerChip();
        BuildTitle();
        BuildShell();
        BuildMascots();
        HideLegacyPresentation();
        RefreshPresentation();
    }

    void AddBackground(Transform parent)
    {
        var sprite = theme.settings.background;
        var go = RuntimeUI.CreateObject("SettingsReferenceBackground", parent);
        RuntimeUI.Stretch(go);
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    void BuildBackButton()
    {
        var back = Find<Button>(menu.settingsPanel.transform, "Buttonback");
        if (back == null) return;
        Seat(back.transform, safeRoot, new Vector2(-455f, 812f),
            new Vector2(124f, 124f));
        StyleButton(back, SettingsSurfaceKind.BackButton, false, 30f);
        var icon = AddSprite(back.transform, "BackIcon", theme.settings.chevron,
            Vector2.zero, new Vector2(62f, 78f));
        if (icon != null)
            icon.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
        HideControlLabels(back.transform);
    }

    void BuildPlayerChip()
    {
        var go = RuntimeUI.CreateObject("SettingsPlayerChip", safeRoot);
        PlaceLocal(go.transform as RectTransform, new Vector2(356f, 812f),
            new Vector2(345f, 124f));
        var chip = go.AddComponent<Image>();
        chip.sprite = theme.settings.playerChip;
        chip.type = Image.Type.Sliced;
        chip.pixelsPerUnitMultiplier = 1f;
        chip.preserveAspect = false;
        chip.color = Color.white;
        chip.raycastTarget = false;

        AddSprite(go.transform, "PlayerAvatar", theme.shared.playerPortrait,
            new Vector2(-126f, -2f), new Vector2(76f, 82f));

        chipName = AddText(go.transform, "PlayerName", "", 31,
            new Vector2(35f, 23f), new Vector2(210f, 46f), NearWhite,
            TextAlignmentOptions.Center, ResponsiveTextRole.Heading,
            HolTextRole.Emphasis);
        chipStreak = AddText(go.transform, "Streak", "", 30,
            new Vector2(55f, -27f), new Vector2(120f, 40f), NearWhite,
            TextAlignmentOptions.Left, ResponsiveTextRole.Action,
            HolTextRole.LiveNumber);
        AddIcon(go.transform, "StreakFlame", SettingsIconKind.Flame,
            new Vector2(-20f, -27f), new Vector2(44f, 44f));
    }

    void BuildTitle()
    {
        var title = RuntimeUI.CreateObject("SettingsReferenceTitle", safeRoot);
        PlaceLocal(title.transform as RectTransform, new Vector2(0f, 540f),
            new Vector2(520f, 112f));
        EnsureCanvasRenderer(title);
        var surface = title.AddComponent<SettingsSurfaceGraphic>();
        surface.Configure(SettingsSurfaceKind.Title, false);
        surface.raycastTarget = false;
        AddLocalized(title.transform, "TitleText", "settings_title_display", 58,
            Vector2.zero, new Vector2(460f, 86f), NearWhite,
            TextAlignmentOptions.Center, ResponsiveTextRole.Heading,
            HolTextRole.Hero);
        AddIcon(safeRoot, "SettingsTitleStarLeft", SettingsIconKind.Star,
            new Vector2(-340f, 540f), new Vector2(92f, 92f));
        AddIcon(safeRoot, "SettingsTitleStarRight", SettingsIconKind.Star,
            new Vector2(340f, 540f), new Vector2(92f, 92f));
    }

    void BuildShell()
    {
        var shellGo = RuntimeUI.CreateObject(ShellName, safeRoot);
        shell = shellGo.transform as RectTransform;
        PlaceLocal(shell, new Vector2(0f, -80f), new Vector2(970f, 1080f));
        EnsureCanvasRenderer(shellGo);
        var surface = shellGo.AddComponent<SettingsSurfaceGraphic>();
        surface.Configure(SettingsSurfaceKind.Shell, false);
        surface.raycastTarget = false;

        BuildNameRow(390f);
        BuildLanguageRow(180f);
        BuildMusicRow(-30f);
        BuildDifficultyRow(-240f);
        BuildPrivacyRow(-450f);
    }

    RectTransform BuildRow(string name, string key, SettingsIconKind icon,
        float y, int labelSize = 36, float labelY = 0f)
    {
        var row = RuntimeUI.CreateObject(name, shell);
        var rect = row.transform as RectTransform;
        PlaceLocal(rect, new Vector2(0f, y), new Vector2(890f, 190f));
        EnsureCanvasRenderer(row);
        var surface = row.AddComponent<SettingsSurfaceGraphic>();
        surface.Configure(SettingsSurfaceKind.Row, false);
        surface.raycastTarget = false;
        AddSprite(row.transform, name + "Icon", IconSprite(icon),
            new Vector2(-365f, 0f), new Vector2(128f, 128f));
        var label = AddLocalized(row.transform, name + "Label", key, labelSize,
            new Vector2(-105f, labelY), new Vector2(360f, 64f), NearWhite,
            TextAlignmentOptions.Left, ResponsiveTextRole.Heading,
            HolTextRole.SectionHeading);
        label.enableWordWrapping = false;
        return rect;
    }

    void BuildNameRow(float y)
    {
        var row = BuildRow("SettingsNameRow", "settings_player_name",
            SettingsIconKind.Player, y, labelY: 38f);
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
            var localized = save.GetComponentInChildren<LocalizedText>(true);
            if (localized != null) localized.key = "settings_save_display";
            StyleButton(save, SettingsSurfaceKind.CyanButton, false, 30f);
        }
    }

    void BuildLanguageRow(float y)
    {
        var row = BuildRow("SettingsLanguageRow", "settings_language",
            SettingsIconKind.Globe, y);
        englishButton = Find<Button>(menu.settingsPanel.transform, "EnglishButton");
        greekButton = Find<Button>(menu.settingsPanel.transform, "GreekButton");
        Seat(englishButton == null ? null : englishButton.transform, row,
            new Vector2(35f, 0f), new Vector2(210f, 84f));
        Seat(greekButton == null ? null : greekButton.transform, row,
            new Vector2(275f, 0f), new Vector2(230f, 84f));
        StyleButton(englishButton, SettingsSurfaceKind.NeutralChoice, false, 30f);
        StyleButton(greekButton, SettingsSurfaceKind.NeutralChoice, false, 30f);
    }

    void BuildMusicRow(float y)
    {
        var row = BuildRow("SettingsMusicRow", "settings_music",
            SettingsIconKind.Music, y);
        var toggle = Find<Toggle>(menu.settingsPanel.transform, "Toggle");
        Seat(toggle == null ? null : toggle.transform, row,
            new Vector2(300f, 0f), new Vector2(200f, 92f));
        if (toggle == null) return;
        foreach (var graphic in toggle.GetComponentsInChildren<Graphic>(true))
        {
            graphic.color = new Color(1f, 1f, 1f, 0.002f);
            graphic.raycastTarget = graphic == toggle.targetGraphic;
        }
        HideControlLabels(toggle.transform);
        var visual = RuntimeUI.CreateObject("SettingsMusicToggleVisual",
            toggle.transform);
        RuntimeUI.Stretch(visual);
        visual.transform.SetAsFirstSibling();
        EnsureCanvasRenderer(visual);
        musicVisual = visual.AddComponent<SettingsToggleGraphic>();
        musicVisual.raycastTarget = false;
    }

    void BuildDifficultyRow(float y)
    {
        var row = BuildRow("SettingsDifficultyRow", "settings_ai_difficulty",
            SettingsIconKind.Brain, y, labelY: 38f);
        Vector2[] positions =
        {
            new Vector2(-209f, -43f), new Vector2(-46f, -43f),
            new Vector2(117f, -43f), new Vector2(314f, -43f)
        };
        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            var button = Find<Button>(menu.settingsPanel.transform,
                "Difficulty" + i);
            difficultyButtons[i] = button;
            float width = i == 3 ? 224f : i == 1 ? 156f : 142f;
            Seat(button == null ? null : button.transform, row, positions[i],
                new Vector2(width, 82f));
            StyleButton(button, SettingsSurfaceKind.NeutralChoice, false,
                i == 3 ? 20f : 27f);
            if (button != null)
                button.onClick.AddListener(RefreshPresentation);
        }
    }

    void BuildPrivacyRow(float y)
    {
        var row = BuildRow("SettingsPrivacyRow", "settings_ads_privacy",
            SettingsIconKind.Shield, y, 32);
        var ads = Find<Button>(menu.settingsPanel.transform, "AdsPrivacyButton");
        Seat(ads == null ? null : ads.transform, row,
            new Vector2(285f, 0f), new Vector2(210f, 84f));
        if (ads == null) return;
        var localized = ads.GetComponentInChildren<LocalizedText>(true);
        if (localized != null) localized.key = "settings_change_display";
        StyleButton(ads, SettingsSurfaceKind.CyanButton, false, 30f);
    }

    void BuildMascots()
    {
        AddSprite(safeRoot, "SettingsMascotSix", theme.shared.mascotSix,
            new Vector2(-385f, -745f), new Vector2(270f, 310f));
        AddSprite(safeRoot, "SettingsMascotSeven", theme.shared.mascotSeven,
            new Vector2(385f, -745f), new Vector2(270f, 310f));
    }

    void RefreshPresentation()
    {
        if (!built) return;
        // AttachmentReskinVisuals may perform a generic late pass when the
        // hierarchy signature changes. Reassert the approved production sprites
        // directly on the real controls; they remain the visible source of truth.
        SetButtonArtwork(Find<Button>(root, "Buttonback"), theme.settings.neutralButton);
        SetButtonArtwork(Find<Button>(root, "Buttonsave"), theme.settings.blueButton);
        SetButtonArtwork(Find<Button>(root, "AdsPrivacyButton"), theme.settings.blueButton);

        string storedPlayerName = PlayerPrefs.GetString("PlayerName", "");
        bool hasStoredPlayerName = !string.IsNullOrWhiteSpace(storedPlayerName);
        string playerName = storedPlayerName;
        if (!hasStoredPlayerName)
            playerName = L10n.Get("player_default");
        if (chipName != null) chipName.text = playerName;
        if (chipStreak != null) chipStreak.text = GameStats.CurrentStreak.ToString();
        var input = Find<TMP_InputField>(root, "InputField (TMP)");
        if (input != null)
            SetProductionImage(input.GetComponent<Image>(), theme.settings.neutralButton);
        if (input != null && !input.isFocused &&
            (!hasStoredPlayerName || string.IsNullOrWhiteSpace(input.text)))
            input.SetTextWithoutNotify(playerName);

        bool english = L10n.Current == L10n.Language.English;
        SetButtonSelection(englishButton, english);
        SetButtonSelection(greekButton, !english);

        int difficulty = Mathf.Clamp(PlayerPrefs.GetInt(DifficultyPrefKey, 1), 0, 3);
        for (int i = 0; i < difficultyButtons.Length; i++)
            SetButtonSelection(difficultyButtons[i], i == difficulty);

        var toggle = Find<Toggle>(root, "Toggle");
        if (musicVisual != null && toggle != null)
            musicVisual.SetOn(toggle.isOn);
    }

    void StyleInput(TMP_InputField input)
    {
        var image = input.GetComponent<Image>();
        if (image != null)
        {
            SetProductionImage(image, theme.settings.neutralButton);
            image.raycastTarget = true;
        }
        if (input.textComponent != null)
        {
            CartoonTypography.Bind(input.textComponent, HolTextRole.Body);
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
            CartoonTypography.Bind(placeholder, HolTextRole.Small);
            placeholder.fontSize = 30f;
            placeholder.color = new Color(0.78f, 0.80f, 0.92f, 0.82f);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        }
    }

    void StyleButton(Button button, SettingsSurfaceKind kind, bool selected,
        float fontSize)
    {
        if (button == null) return;
        Sprite artwork = kind == SettingsSurfaceKind.CyanButton
            ? theme.settings.blueButton
            : selected ? theme.settings.goldButton : theme.settings.neutralButton;
        SetButtonArtwork(button, artwork);
        var overlay = EnsureButtonStateOverlay(button);
        BindFeedback(button, overlay);
        var text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.transform.SetAsLastSibling();
            var textRect = text.transform as RectTransform;
            if (textRect != null)
            {
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.pivot = new Vector2(0.5f, 0.5f);
                textRect.offsetMin = new Vector2(12f, 7f);
                textRect.offsetMax = new Vector2(-12f, -7f);
                textRect.localScale = Vector3.one;
                textRect.localRotation = Quaternion.identity;
            }
            CartoonTypography.Bind(text, HolTextRole.SecondaryCta);
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Normal;
            text.fontWeight = FontWeight.Regular;
            text.color = selected ? DarkInk : NearWhite;
            text.alignment = TextAlignmentOptions.Center;
            ResponsiveTextPolicy.Configure(text, ResponsiveTextRole.Action,
                fontSize);
            if (text.GetComponent<ResponsiveNoWrapText>() == null)
                text.gameObject.AddComponent<ResponsiveNoWrapText>();
            text.enableWordWrapping = false;
            text.margin = new Vector4(8f, 3f, 8f, 3f);
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            EnsureTextShadow(text, selected
                ? new Color(0.32f, 0.16f, 0f, 0.42f)
                : new Color(0.01f, 0f, 0.05f, 0.72f),
                new Vector2(2f, -3f));
        }
    }

    static void BindFeedback(Button button, Image overlay)
    {
        if (button == null || overlay == null) return;
        var feedback = button.GetComponent<SettingsButtonFeedback>();
        if (feedback == null) feedback = button.gameObject.AddComponent<SettingsButtonFeedback>();
        feedback.Bind(button, overlay);
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

    void SetButtonSelection(Button button, bool selected)
    {
        if (button == null) return;
        SetButtonArtwork(button, selected
            ? theme.settings.goldButton
            : theme.settings.neutralButton);
        var text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.color = selected ? DarkInk : NearWhite;
    }

    static void SetButtonArtwork(Button button, Sprite sprite)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image == null) return;
        SetProductionImage(image, sprite);
        image.raycastTarget = true;
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.90f, 0.92f, 1f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.56f, 0.58f, 0.68f, 0.72f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.06f;
        button.colors = colors;

        var overlay = button.transform.Find(ButtonStateOverlayName);
        if (overlay != null)
        {
            var overlayImage = overlay.GetComponent<Image>();
            if (overlayImage != null)
            {
                overlayImage.sprite = image.sprite;
                overlayImage.type = Image.Type.Sliced;
            }
        }
    }

    static void SetProductionImage(Image image, Sprite sprite)
    {
        if (image == null) return;
        if (sprite == null) return;
        image.enabled = true;
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 2f;
        image.preserveAspect = false;
        image.color = Color.white;
    }

    static Image EnsureButtonStateOverlay(Button button)
    {
        var existing = button.transform.Find(ButtonStateOverlayName);
        GameObject go = existing == null
            ? RuntimeUI.CreateObject(ButtonStateOverlayName, button.transform)
            : existing.gameObject;
        RuntimeUI.Stretch(go);
        go.transform.SetAsFirstSibling();
        var overlay = go.GetComponent<Image>();
        if (overlay == null) overlay = go.AddComponent<Image>();
        overlay.sprite = button.GetComponent<Image>().sprite;
        overlay.type = Image.Type.Sliced;
        overlay.pixelsPerUnitMultiplier = 2f;
        overlay.preserveAspect = false;
        overlay.color = Color.clear;
        overlay.raycastTarget = false;
        return overlay;
    }

    Transform AddSurface(Transform parent, string name,
        SettingsSurfaceKind kind, bool selected, Vector2 position, Vector2 size)
    {
        var existing = parent.Find(name);
        if (existing != null)
        {
            var existingGraphic = existing.GetComponent<SettingsSurfaceGraphic>();
            if (existingGraphic != null) existingGraphic.Configure(kind, selected);
            return existing;
        }
        var go = RuntimeUI.CreateObject(name, parent);
        PlaceLocal(go.transform as RectTransform, position, size);
        EnsureCanvasRenderer(go);
        var graphic = go.AddComponent<SettingsSurfaceGraphic>();
        graphic.Configure(kind, selected);
        graphic.raycastTarget = false;
        return go.transform;
    }

    SettingsIconGraphic AddIcon(Transform parent, string name,
        SettingsIconKind kind, Vector2 position, Vector2 size)
    {
        var go = RuntimeUI.CreateObject(name, parent);
        PlaceLocal(go.transform as RectTransform, position, size);
        EnsureCanvasRenderer(go);
        var icon = go.AddComponent<SettingsIconGraphic>();
        icon.Configure(kind);
        icon.raycastTarget = false;
        return icon;
    }

    TMP_Text AddLocalized(Transform parent, string name, string key,
        int size, Vector2 position, Vector2 dimensions, Color color,
        TextAlignmentOptions alignment, ResponsiveTextRole role,
        HolTextRole fontRole)
    {
        var text = AddText(parent, name, L10n.Get(key), size, position,
            dimensions, color, alignment, role, fontRole);
        RuntimeUI.Localize(text, key);
        return text;
    }

    static TMP_Text AddText(Transform parent, string name, string content,
        int size, Vector2 position, Vector2 dimensions, Color color,
        TextAlignmentOptions alignment, ResponsiveTextRole role,
        HolTextRole fontRole)
    {
        var text = RuntimeUI.CreateText(parent, name, content, size,
            position, dimensions, color);
        CartoonTypography.Bind(text, fontRole);
        text.alignment = alignment;
        text.fontStyle = FontStyles.Normal;
        text.fontWeight = FontWeight.Regular;
        EnsureTextShadow(text, new Color(0.01f, 0f, 0.05f, 0.68f),
            new Vector2(2f, -3f));
        ResponsiveTextPolicy.Configure(text, role, size);
        return text;
    }

    static Image AddSprite(Transform parent, string name, Sprite sprite,
        Vector2 position, Vector2 size)
    {
        if (sprite == null) return null;
        var go = RuntimeUI.CreateObject(name, parent);
        PlaceLocal(go.transform as RectTransform, position, size);
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    Sprite IconSprite(SettingsIconKind kind)
    {
        switch (kind)
        {
            case SettingsIconKind.Player: return theme.settings.playerIcon;
            case SettingsIconKind.Globe: return theme.settings.languageIcon;
            case SettingsIconKind.Music: return theme.settings.musicIcon;
            case SettingsIconKind.Brain: return theme.settings.difficultyIcon;
            case SettingsIconKind.Shield: return theme.settings.privacyIcon;
            default: return null;
        }
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
        var catalog = HolTheme.Current;
        if (menu == null || menu.settingsPanel == null ||
            catalog == null || !catalog.IsComplete)
            return false;
        var panel = menu.settingsPanel.transform;
        return Find<Button>(panel, "Buttonback") != null &&
               Find<TMP_InputField>(panel, "InputField (TMP)") != null &&
               Find<Button>(panel, "GreekButton") != null &&
               Find<Button>(panel, "Difficulty3") != null &&
               Find<Button>(panel, "AdsPrivacyButton") != null;
    }

    static void Seat(Transform target, Transform parent, Vector2 position,
        Vector2 size)
    {
        if (target == null || parent == null) return;
        target.SetParent(parent, false);
        PlaceLocal(target as RectTransform, position, size);
        target.gameObject.SetActive(true);
    }

    static void PlaceLocal(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    static void HideControlLabels(Transform control)
    {
        if (control == null) return;
        foreach (var text in control.GetComponentsInChildren<TMP_Text>(true))
            text.gameObject.SetActive(false);
        foreach (var text in control.GetComponentsInChildren<Text>(true))
            text.gameObject.SetActive(false);
    }

    static void EnsureCanvasRenderer(GameObject target)
    {
        if (target != null && target.GetComponent<CanvasRenderer>() == null)
            target.AddComponent<CanvasRenderer>();
    }

    static T Find<T>(Transform parent, string name) where T : Component
    {
        if (parent == null) return null;
        foreach (var item in parent.GetComponentsInChildren<T>(true))
            if (item.name == name) return item;
        return null;
    }
}

// Adds tactile press depth without replacing or wrapping the production Button,
// so every existing listener and navigation path remains authoritative.
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
        if (button != null && button.IsInteractable()) SetPressed(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetPressed(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetPressed(false);
    }

    void OnDisable()
    {
        SetPressed(false);
    }

    void SetPressed(bool pressed)
    {
        if (overlay == null) return;
        overlay.color = pressed
            ? new Color(1f, 1f, 1f, 0.16f)
            : Color.clear;
    }
}

public enum SettingsSurfaceKind
{
    Shell,
    Row,
    Title,
    Input,
    BackButton,
    CyanButton,
    NeutralChoice
}

// Multi-layer rounded materials: shadow, bloom/rim, body gradient and gloss
// are one non-interactive mesh so they cannot interfere with real controls.
public sealed class SettingsSurfaceGraphic : MaskableGraphic
{
    SettingsSurfaceKind kind;
    bool selected;

    public void Configure(SettingsSurfaceKind value, bool isSelected)
    {
        kind = value;
        selected = isSelected;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        bool large = kind == SettingsSurfaceKind.Shell;
        bool button = kind == SettingsSurfaceKind.Title ||
                      kind == SettingsSurfaceKind.BackButton ||
                      kind == SettingsSurfaceKind.CyanButton ||
                      kind == SettingsSurfaceKind.NeutralChoice;
        float radius = large ? 58f : kind == SettingsSurfaceKind.Row ? 34f : 24f;
        float depth = kind == SettingsSurfaceKind.Shell ? 14f
            : kind == SettingsSurfaceKind.Row ? 9f : button ? 13f : 8f;

        Color rim = selected
            ? new Color(1f, 0.79f, 0.08f, 1f)
            : kind == SettingsSurfaceKind.CyanButton
                ? new Color(0.06f, 0.91f, 1f, 1f)
                : kind == SettingsSurfaceKind.Title
                    ? new Color(1f, 0.20f, 0.91f, 1f)
                    : new Color(0.72f, 0.18f, 1f, 1f);
        Color top;
        Color bottom;
        switch (kind)
        {
            case SettingsSurfaceKind.CyanButton:
                top = new Color(0.30f, 0.96f, 1f, 1f);
                bottom = new Color(0.00f, 0.28f, 0.76f, 1f);
                break;
            case SettingsSurfaceKind.NeutralChoice:
                top = selected ? new Color(1f, 0.94f, 0.34f, 1f)
                    : new Color(0.29f, 0.18f, 0.62f, 1f);
                bottom = selected ? new Color(1f, 0.38f, 0.005f, 1f)
                    : new Color(0.035f, 0.012f, 0.16f, 1f);
                break;
            case SettingsSurfaceKind.Input:
                top = new Color(0.095f, 0.065f, 0.29f, 1f);
                bottom = new Color(0.010f, 0.004f, 0.060f, 1f);
                rim = new Color(0.43f, 0.24f, 0.88f, 1f);
                break;
            case SettingsSurfaceKind.Title:
                top = new Color(0.56f, 0.13f, 0.83f, 1f);
                bottom = new Color(0.15f, 0.018f, 0.38f, 1f);
                break;
            case SettingsSurfaceKind.BackButton:
                top = new Color(0.30f, 0.17f, 0.62f, 1f);
                bottom = new Color(0.028f, 0.010f, 0.13f, 1f);
                break;
            case SettingsSurfaceKind.Shell:
                top = new Color(0.036f, 0.011f, 0.12f, 0.985f);
                bottom = new Color(0.002f, 0.001f, 0.021f, 0.995f);
                break;
            case SettingsSurfaceKind.Row:
                top = new Color(0.13f, 0.045f, 0.34f, 0.995f);
                bottom = new Color(0.008f, 0.002f, 0.050f, 1f);
                rim = new Color(0.82f, 0.18f, 1f, 1f);
                break;
            default:
                top = new Color(0.028f, 0.015f, 0.105f, 0.98f);
                bottom = new Color(0.006f, 0.004f, 0.035f, 0.99f);
                break;
        }

        Color rimBright = Color.Lerp(rim, Color.white, selected ? 0.42f : 0.24f);
        // Three reference pixels keep neighbouring controls visually distinct
        // at the 720-wide device adaptation (their touch targets remain unchanged).
        float bloom = button ? 3f : kind == SettingsSurfaceKind.Row ? 2f : 1f;
        AddRounded(vh, Inset(r, -bloom), radius + bloom,
            new Color(rim.r, rim.g, rim.b, button ? 0.22f : 0.12f),
            new Color(rim.r, rim.g, rim.b, 0.01f));
        AddRounded(vh, Offset(r, 0f, -depth), radius,
            new Color(0f, 0f, 0.025f, 0.82f), new Color(0f, 0f, 0.02f, 0.94f));
        AddRounded(vh, r, radius, new Color(0.04f, 0.015f, 0.12f, 1f),
            new Color(0.01f, 0.005f, 0.04f, 1f));
        AddRounded(vh, Inset(r, large ? 5f : 4f), radius - 4f, rimBright,
            Color.Lerp(rim, DarkRim(), 0.48f));
        AddRounded(vh, Inset(r, large ? 9f : 7f), radius - 7f,
            new Color(0.11f, 0.025f, 0.25f, 1f),
            new Color(0.012f, 0.004f, 0.055f, 1f));
        AddRounded(vh, Inset(r, large ? 13f : 11f), radius - 11f, top, bottom);
        if (button)
        {
            Rect gloss = Inset(r, 18f);
            gloss.yMin = Mathf.Lerp(gloss.yMin, gloss.yMax, 0.70f);
            AddRounded(vh, gloss, Mathf.Max(8f, radius - 12f),
                new Color(1f, 1f, 1f, selected ? 0.30f : 0.24f),
                new Color(1f, 1f, 1f, 0.005f));
            Rect lowerGlow = Inset(r, 17f);
            lowerGlow.yMax = Mathf.Lerp(lowerGlow.yMin, lowerGlow.yMax, 0.20f);
            AddRounded(vh, lowerGlow, Mathf.Max(7f, radius - 13f),
                new Color(rim.r, rim.g, rim.b, 0.03f),
                new Color(rim.r, rim.g, rim.b, 0.22f));
        }
        else if (kind == SettingsSurfaceKind.Row)
        {
            Rect glow = Inset(r, 18f);
            glow.yMin = Mathf.Lerp(glow.yMin, glow.yMax, 0.58f);
            AddRounded(vh, glow, Mathf.Max(10f, radius - 16f),
                new Color(0.56f, 0.22f, 0.88f, 0.22f),
                new Color(0.05f, 0.02f, 0.18f, 0.01f));
        }
    }

    static Color DarkRim()
    {
        return new Color(0.08f, 0.015f, 0.18f, 1f);
    }

    static Rect Inset(Rect rect, float value)
    {
        return new Rect(rect.xMin + value, rect.yMin + value,
            Mathf.Max(1f, rect.width - value * 2f),
            Mathf.Max(1f, rect.height - value * 2f));
    }

    static Rect Offset(Rect rect, float x, float y)
    {
        rect.position += new Vector2(x, y);
        return rect;
    }

    static void AddRounded(VertexHelper vh, Rect rect, float radius,
        Color top, Color bottom)
    {
        radius = Mathf.Clamp(radius, 1f,
            Mathf.Min(rect.width, rect.height) * 0.5f);
        const int cornerSteps = 5;
        int perimeter = cornerSteps * 4;
        int center = vh.currentVertCount;
        vh.AddVert(rect.center, Color.Lerp(bottom, top, 0.5f), Vector2.one * 0.5f);
        for (int corner = 0; corner < 4; corner++)
        {
            Vector2 cornerCenter;
            float startAngle;
            if (corner == 0)
            {
                cornerCenter = new Vector2(rect.xMax - radius, rect.yMax - radius);
                startAngle = 0f;
            }
            else if (corner == 1)
            {
                cornerCenter = new Vector2(rect.xMin + radius, rect.yMax - radius);
                startAngle = 90f;
            }
            else if (corner == 2)
            {
                cornerCenter = new Vector2(rect.xMin + radius, rect.yMin + radius);
                startAngle = 180f;
            }
            else
            {
                cornerCenter = new Vector2(rect.xMax - radius, rect.yMin + radius);
                startAngle = 270f;
            }
            for (int step = 0; step < cornerSteps; step++)
            {
                float angle = (startAngle + step * 90f / (cornerSteps - 1)) * Mathf.Deg2Rad;
                Vector2 point = cornerCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                float vertical = Mathf.InverseLerp(rect.yMin, rect.yMax, point.y);
                vh.AddVert(point, Color.Lerp(bottom, top, vertical), Vector2.zero);
            }
        }
        for (int i = 0; i < perimeter; i++)
            vh.AddTriangle(center,
                center + 1 + ((i + 1) % perimeter), center + 1 + i);
    }
}

public enum SettingsIconKind
{
    Back,
    AvatarRing,
    Player,
    Globe,
    Music,
    Brain,
    Shield,
    Flame,
    Star
}

// Resolution-independent symbols keep all Settings labels as live TMP and
// avoid raster placeholders or unsupported font glyphs.
public sealed class SettingsIconGraphic : MaskableGraphic
{
    SettingsIconKind kind;

    public void Configure(SettingsIconKind value)
    {
        kind = value;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        float s = Mathf.Min(r.width, r.height);
        Vector2 c = r.center;
        Color cyan = new Color(0.02f, 0.88f, 1f, 1f);
        Color blue = new Color(0.01f, 0.31f, 0.88f, 1f);
        Color purple = new Color(0.72f, 0.16f, 1f, 1f);
        Color white = new Color(0.97f, 0.98f, 1f, 1f);
        Color gold = new Color(1f, 0.65f, 0.04f, 1f);
        Color symbolShadow = new Color(0.005f, 0.002f, 0.035f, 0.88f);

        if (kind != SettingsIconKind.Back && kind != SettingsIconKind.Flame &&
            kind != SettingsIconKind.Star)
        {
            AddDisc(vh, c + new Vector2(0f, -6f), s * 0.50f,
                new Color(0f, 0f, 0.025f, 0.82f), 40);
            AddDisc(vh, c, s * 0.495f,
                new Color(0.65f, 0.06f, 1f, 0.22f), 40);
            AddRing(vh, c, s * 0.475f, s * 0.075f,
                new Color(0.22f, 0.14f, 0.74f, 1f), 40);
            AddRing(vh, c, s * 0.468f, s * 0.036f,
                new Color(0.08f, 0.88f, 1f, 1f), 40);
            AddRing(vh, c, s * 0.430f, s * 0.023f,
                new Color(0.94f, 0.24f, 1f, 0.92f), 40);
            AddDisc(vh, c, s * 0.405f,
                new Color(0.010f, 0.020f, 0.13f, 1f), 40);
            AddDisc(vh, c + new Vector2(-0.15f, 0.18f) * s, s * 0.085f,
                new Color(1f, 1f, 1f, 0.13f), 24);
        }

        switch (kind)
        {
            case SettingsIconKind.Back:
                AddPolygon(vh, new[]
                {
                    c + new Vector2(-0.34f, -0.035f) * s,
                    c + new Vector2(0.02f, 0.305f) * s,
                    c + new Vector2(0.22f, 0.125f) * s,
                    c + new Vector2(0.05f, -0.035f) * s,
                    c + new Vector2(0.22f, -0.195f) * s,
                    c + new Vector2(0.02f, -0.375f) * s
                }, symbolShadow);
                AddPolygon(vh, new[]
                {
                    c + new Vector2(-0.34f, 0f) * s,
                    c + new Vector2(0.02f, 0.34f) * s,
                    c + new Vector2(0.22f, 0.16f) * s,
                    c + new Vector2(0.05f, 0f) * s,
                    c + new Vector2(0.22f, -0.16f) * s,
                    c + new Vector2(0.02f, -0.34f) * s
                }, white);
                break;
            case SettingsIconKind.AvatarRing:
                break;
            case SettingsIconKind.Player:
                AddDisc(vh, c + new Vector2(0f, 0.10f) * s, s * 0.18f,
                    symbolShadow, 28);
                AddRoundedBox(vh, c + new Vector2(0f, -0.21f) * s,
                    new Vector2(0.54f, 0.31f) * s, symbolShadow);
                AddDisc(vh, c + new Vector2(0f, 0.15f) * s, s * 0.17f, blue, 28);
                AddDisc(vh, c + new Vector2(-0.035f, 0.19f) * s,
                    s * 0.125f, cyan, 24);
                AddRoundedBox(vh, c + new Vector2(0f, -0.17f) * s,
                    new Vector2(0.52f, 0.30f) * s, blue);
                AddRoundedBox(vh, c + new Vector2(-0.025f, -0.12f) * s,
                    new Vector2(0.45f, 0.18f) * s, cyan);
                break;
            case SettingsIconKind.Globe:
                AddRing(vh, c + new Vector2(0f, -0.025f) * s,
                    s * 0.32f, s * 0.065f, symbolShadow, 36);
                AddEllipseRing(vh, c + new Vector2(0f, -0.025f) * s,
                    new Vector2(0.18f, 0.32f) * s, s * 0.05f, symbolShadow, 32);
                AddRing(vh, c, s * 0.31f, s * 0.065f, blue, 36);
                AddRing(vh, c, s * 0.295f, s * 0.03f, cyan, 36);
                AddEllipseRing(vh, c, new Vector2(0.16f, 0.30f) * s,
                    s * 0.045f, blue, 32);
                AddEllipseRing(vh, c, new Vector2(0.15f, 0.29f) * s,
                    s * 0.022f, cyan, 32);
                AddQuad(vh, new Rect(c.x - s * 0.30f, c.y - s * 0.035f,
                    s * 0.60f, s * 0.07f), blue);
                AddQuad(vh, new Rect(c.x - s * 0.285f, c.y - s * 0.015f,
                    s * 0.57f, s * 0.03f), cyan);
                break;
            case SettingsIconKind.Music:
                AddDisc(vh, c + new Vector2(-0.18f, -0.25f) * s,
                    s * 0.15f, symbolShadow, 24);
                AddDisc(vh, c + new Vector2(0.20f, -0.15f) * s,
                    s * 0.15f, symbolShadow, 24);
                AddDisc(vh, c + new Vector2(-0.18f, -0.22f) * s, s * 0.135f, blue, 24);
                AddDisc(vh, c + new Vector2(0.20f, -0.12f) * s, s * 0.135f, blue, 24);
                AddDisc(vh, c + new Vector2(-0.21f, -0.18f) * s, s * 0.085f, cyan, 20);
                AddDisc(vh, c + new Vector2(0.17f, -0.08f) * s, s * 0.085f, cyan, 20);
                AddQuad(vh, new Rect(c.x - s * 0.08f, c.y - s * 0.18f,
                    s * 0.09f, s * 0.50f), blue);
                AddQuad(vh, new Rect(c.x + s * 0.30f, c.y - s * 0.08f,
                    s * 0.09f, s * 0.50f), blue);
                AddPolygon(vh, new[]
                {
                    c + new Vector2(-0.01f, 0.32f) * s,
                    c + new Vector2(0.37f, 0.23f) * s,
                    c + new Vector2(0.37f, 0.10f) * s,
                    c + new Vector2(-0.01f, 0.19f) * s
                }, cyan);
                break;
            case SettingsIconKind.Brain:
                Vector2[] lobes =
                {
                    new Vector2(-0.20f, 0.16f), new Vector2(0f, 0.22f),
                    new Vector2(0.20f, 0.16f), new Vector2(-0.24f, -0.05f),
                    new Vector2(0.24f, -0.05f), new Vector2(-0.12f, -0.23f),
                    new Vector2(0.12f, -0.23f)
                };
                foreach (var lobe in lobes)
                    AddDisc(vh, c + (lobe + new Vector2(0f, -0.035f)) * s,
                        s * 0.19f, symbolShadow, 24);
                foreach (var lobe in lobes)
                    AddDisc(vh, c + lobe * s, s * 0.18f,
                        new Color(0.39f, 0.04f, 0.72f, 1f), 24);
                foreach (var lobe in lobes)
                    AddDisc(vh, c + (lobe + new Vector2(-0.025f, 0.035f)) * s,
                        s * 0.125f, purple, 20);
                AddDisc(vh, c + new Vector2(-0.16f, 0.22f) * s, s * 0.06f,
                    new Color(1f, 0.63f, 1f, 0.72f), 18);
                AddQuad(vh, new Rect(c.x - s * 0.025f, c.y - s * 0.30f,
                    s * 0.05f, s * 0.56f), new Color(0.16f, 0.01f, 0.34f, 1f));
                break;
            case SettingsIconKind.Shield:
                AddPolygon(vh, new[]
                {
                    c + new Vector2(0f, 0.38f) * s,
                    c + new Vector2(0.33f, 0.25f) * s,
                    c + new Vector2(0.29f, -0.17f) * s,
                    c + new Vector2(0f, -0.41f) * s,
                    c + new Vector2(-0.29f, -0.17f) * s,
                    c + new Vector2(-0.33f, 0.25f) * s
                }, symbolShadow);
                AddPolygon(vh, new[]
                {
                    c + new Vector2(0f, 0.34f) * s,
                    c + new Vector2(0.29f, 0.22f) * s,
                    c + new Vector2(0.25f, -0.13f) * s,
                    c + new Vector2(0f, -0.36f) * s,
                    c + new Vector2(-0.25f, -0.13f) * s,
                    c + new Vector2(-0.29f, 0.22f) * s
                }, purple);
                AddPolygon(vh, new[]
                {
                    c + new Vector2(0f, 0.30f) * s,
                    c + new Vector2(0.24f, 0.19f) * s,
                    c + new Vector2(0.20f, -0.10f) * s,
                    c + new Vector2(0f, -0.30f) * s,
                    c + new Vector2(-0.20f, -0.10f) * s,
                    c + new Vector2(-0.24f, 0.19f) * s
                }, blue);
                // Shackle is painted before the body so only the upper U remains.
                AddRing(vh, c + new Vector2(0f, 0.08f) * s, s * 0.11f,
                    s * 0.04f, gold, 20);
                AddRoundedBox(vh, c + new Vector2(0f, -0.06f) * s,
                    new Vector2(0.32f, 0.27f) * s, symbolShadow);
                AddRoundedBox(vh, c + new Vector2(0f, -0.035f) * s,
                    new Vector2(0.29f, 0.24f) * s, cyan);
                AddDisc(vh, c + new Vector2(0f, -0.025f) * s, s * 0.035f,
                    new Color(0.015f, 0.02f, 0.12f, 1f), 16);
                AddQuad(vh, new Rect(c.x - s * 0.017f, c.y - s * 0.13f,
                    s * 0.034f, s * 0.11f),
                    new Color(0.015f, 0.02f, 0.12f, 1f));
                break;
            case SettingsIconKind.Flame:
                AddPolygon(vh, new[]
                {
                    c + new Vector2(0.02f, 0.48f) * s,
                    c + new Vector2(0.25f, 0.13f) * s,
                    c + new Vector2(0.18f, -0.30f) * s,
                    c + new Vector2(0f, -0.48f) * s,
                    c + new Vector2(-0.22f, -0.25f) * s,
                    c + new Vector2(-0.28f, 0.08f) * s,
                    c + new Vector2(-0.07f, 0.28f) * s
                }, gold);
                break;
            case SettingsIconKind.Star:
                AddStar(vh, c + new Vector2(0f, -3f), s * 0.47f,
                    s * 0.20f, new Color(0f, 0f, 0.03f, 0.75f));
                AddStar(vh, c, s * 0.48f, s * 0.21f,
                    new Color(0.82f, 0.10f, 1f, 0.28f));
                AddStar(vh, c, s * 0.39f, s * 0.17f,
                    name != null && name.IndexOf("Right",
                        System.StringComparison.OrdinalIgnoreCase) >= 0
                        ? cyan : new Color(1f, 0.14f, 0.86f, 1f));
                AddStar(vh, c + new Vector2(-0.035f, 0.045f) * s,
                    s * 0.31f, s * 0.135f,
                    new Color(0.97f, 0.98f, 1f, 1f));
                AddStar(vh, c, s * 0.25f, s * 0.105f,
                    new Color(0.055f, 0.018f, 0.19f, 1f));
                break;
        }
    }

    static void AddStar(VertexHelper vh, Vector2 center, float outerRadius,
        float innerRadius, Color color)
    {
        var points = new Vector2[10];
        for (int i = 0; i < points.Length; i++)
        {
            float angle = (90f + i * 36f) * Mathf.Deg2Rad;
            float radius = (i & 1) == 0 ? outerRadius : innerRadius;
            points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        AddPolygon(vh, points, color);
    }

    static void AddRoundedBox(VertexHelper vh, Vector2 center,
        Vector2 size, Color color)
    {
        Rect r = new Rect(center - size * 0.5f, size);
        AddQuad(vh, r, color);
    }

    static void AddQuad(VertexHelper vh, Rect rect, Color color)
    {
        int i = vh.currentVertCount;
        vh.AddVert(new Vector2(rect.xMin, rect.yMin), color, Vector2.zero);
        vh.AddVert(new Vector2(rect.xMin, rect.yMax), color, Vector2.up);
        vh.AddVert(new Vector2(rect.xMax, rect.yMax), color, Vector2.one);
        vh.AddVert(new Vector2(rect.xMax, rect.yMin), color, Vector2.right);
        vh.AddTriangle(i, i + 2, i + 1);
        vh.AddTriangle(i, i + 3, i + 2);
    }

    static void AddPolygon(VertexHelper vh, Vector2[] points, Color color)
    {
        Vector2 center = Vector2.zero;
        foreach (var point in points) center += point;
        center /= points.Length;
        int start = vh.currentVertCount;
        vh.AddVert(center, color, Vector2.one * 0.5f);
        foreach (var point in points) vh.AddVert(point, color, Vector2.zero);
        for (int i = 0; i < points.Length; i++)
            vh.AddTriangle(start,
                start + 1 + ((i + 1) % points.Length), start + 1 + i);
    }

    static void AddDisc(VertexHelper vh, Vector2 center, float radius,
        Color color, int segments)
    {
        int start = vh.currentVertCount;
        vh.AddVert(center, color, Vector2.one * 0.5f);
        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            var d = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            vh.AddVert(center + d * radius, color, d * 0.5f + Vector2.one * 0.5f);
        }
        for (int i = 0; i < segments; i++)
            vh.AddTriangle(start,
                start + 1 + ((i + 1) % segments), start + 1 + i);
    }

    static void AddRing(VertexHelper vh, Vector2 center, float radius,
        float thickness, Color color, int segments)
    {
        for (int i = 0; i < segments; i++)
        {
            float a0 = Mathf.PI * 2f * i / segments;
            float a1 = Mathf.PI * 2f * (i + 1) / segments;
            Vector2 d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0));
            Vector2 d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
            int start = vh.currentVertCount;
            vh.AddVert(center + d0 * radius, color, Vector2.zero);
            vh.AddVert(center + d0 * (radius - thickness), color, Vector2.zero);
            vh.AddVert(center + d1 * (radius - thickness), color, Vector2.zero);
            vh.AddVert(center + d1 * radius, color, Vector2.zero);
            vh.AddTriangle(start, start + 2, start + 1);
            vh.AddTriangle(start, start + 3, start + 2);
        }
    }

    static void AddEllipseRing(VertexHelper vh, Vector2 center,
        Vector2 radius, float thickness, Color color, int segments)
    {
        for (int i = 0; i < segments; i++)
        {
            float a0 = Mathf.PI * 2f * i / segments;
            float a1 = Mathf.PI * 2f * (i + 1) / segments;
            Vector2 d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0));
            Vector2 d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
            Vector2 inner = new Vector2(
                Mathf.Max(1f, radius.x - thickness),
                Mathf.Max(1f, radius.y - thickness));
            int start = vh.currentVertCount;
            vh.AddVert(center + Vector2.Scale(d0, radius), color, Vector2.zero);
            vh.AddVert(center + Vector2.Scale(d0, inner), color, Vector2.zero);
            vh.AddVert(center + Vector2.Scale(d1, inner), color, Vector2.zero);
            vh.AddVert(center + Vector2.Scale(d1, radius), color, Vector2.zero);
            vh.AddTriangle(start, start + 2, start + 1);
            vh.AddTriangle(start, start + 3, start + 2);
        }
    }
}

public sealed class SettingsToggleGraphic : MaskableGraphic
{
    bool isOn;

    public void SetOn(bool value)
    {
        if (isOn == value) return;
        isOn = value;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        Color rim = isOn ? new Color(0.02f, 0.90f, 1f, 1f)
            : new Color(0.28f, 0.22f, 0.55f, 1f);
        Color body = isOn ? new Color(0.00f, 0.48f, 0.75f, 1f)
            : new Color(0.06f, 0.045f, 0.18f, 1f);
        Rect shadow = r;
        shadow.position += new Vector2(0f, -6f);
        AddCapsule(vh, shadow, new Color(0f, 0f, 0.025f, 0.72f), 28);
        AddCapsule(vh, Inset(r, -2f),
            isOn ? new Color(0.00f, 0.82f, 1f, 0.20f)
                : new Color(0.55f, 0.20f, 1f, 0.12f), 28);
        AddCapsule(vh, r, new Color(0.015f, 0.008f, 0.09f, 1f), 28);
        AddCapsule(vh, Inset(r, 3f), rim, 26);
        AddCapsule(vh, Inset(r, 7f), new Color(0.018f, 0.025f, 0.15f, 1f), 24);
        AddCapsule(vh, Inset(r, 11f), body, 22);
        Rect gloss = new Rect(r.xMin + 28f, r.center.y + 9f,
            r.width - 56f, 7f);
        AddCapsule(vh, gloss, new Color(1f, 1f, 1f, 0.15f), 18);

        float radius = r.height * 0.355f;
        Vector2 center = new Vector2(isOn ? r.xMax - radius - 11f
            : r.xMin + radius + 11f, r.center.y + 1f);
        SettingsIconGraphicAddDisc(vh, center + new Vector2(0f, -3f),
            radius + 5f, new Color(0f, 0f, 0.03f, 0.72f), 28);
        SettingsIconGraphicAddDisc(vh, center, radius + 2f,
            isOn ? new Color(1f, 0.44f, 0.02f, 1f) :
            new Color(0.38f, 0.34f, 0.60f, 1f), 28);
        SettingsIconGraphicAddDisc(vh, center, radius - 2f,
            isOn ? new Color(1f, 0.73f, 0.08f, 1f) :
            new Color(0.72f, 0.75f, 0.86f, 1f), 28);
        SettingsIconGraphicAddDisc(vh, center + new Vector2(-radius * 0.22f,
            radius * 0.25f), radius * 0.44f,
            new Color(1f, 1f, 1f, 0.42f), 20);
    }

    static Rect Inset(Rect rect, float value)
    {
        return new Rect(rect.xMin + value, rect.yMin + value,
            rect.width - value * 2f, rect.height - value * 2f);
    }

    static void AddCapsule(VertexHelper vh, Rect rect, Color color, int segments)
    {
        float radius = rect.height * 0.5f;
        Vector2 left = new Vector2(rect.xMin + radius, rect.center.y);
        Vector2 right = new Vector2(rect.xMax - radius, rect.center.y);
        SettingsIconGraphicAddDisc(vh, left, radius, color, segments);
        SettingsIconGraphicAddDisc(vh, right, radius, color, segments);
        int i = vh.currentVertCount;
        vh.AddVert(new Vector2(left.x, rect.yMin), color, Vector2.zero);
        vh.AddVert(new Vector2(left.x, rect.yMax), color, Vector2.up);
        vh.AddVert(new Vector2(right.x, rect.yMax), color, Vector2.one);
        vh.AddVert(new Vector2(right.x, rect.yMin), color, Vector2.right);
        vh.AddTriangle(i, i + 2, i + 1);
        vh.AddTriangle(i, i + 3, i + 2);
    }

    static void SettingsIconGraphicAddDisc(VertexHelper vh, Vector2 center,
        float radius, Color color, int segments)
    {
        int start = vh.currentVertCount;
        vh.AddVert(center, color, Vector2.one * 0.5f);
        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            var d = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            vh.AddVert(center + d * radius, color, d * 0.5f + Vector2.one * 0.5f);
        }
        for (int i = 0; i < segments; i++)
            vh.AddTriangle(start,
                start + 1 + ((i + 1) % segments), start + 1 + i);
    }
}
