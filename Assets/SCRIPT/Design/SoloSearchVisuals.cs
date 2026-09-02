using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Sole presentation owner for the truthful Solo-vs-AI preparation screen.
// FakeMatchmaking owns the deterministic transition/cancellation lifecycle;
// this class only composes approved modular sprites around the real status and
// Cancel control.
[DefaultExecutionOrder(2500)]
[DisallowMultipleComponent]
public sealed class SoloSearchVisuals : MonoBehaviour
{
    public const string VisualRootName = "SoloSearchVisualRoot";
    public const string SafeRootName = "SoloSearchSafeRoot";

    const string BackgroundResource = "phase2a/hol_neon_reference_bg_r3";
    const string LogoResource = "reference/hol_logo_exact";
    const string PlayerResource = "reference/char_boy_exact";
    const string AvatarResource = "reference/player_cyan_exact";
    const string MascotSixResource = "reference/mascot_6_exact";
    const string MascotSevenResource = "reference/mascot_7_exact";
    const string RadarBaseResource = "cartoon/cartoon_radar_base";
    const string RadarSweepResource = "cartoon/cartoon_radar_sweep";
    const string StarsResource = "mainmenu/mainmenu_deco_stars";
    const string ConfettiResource = "mainmenu/mainmenu_deco_confetti";
    const string PurpleFrameResource = "mainmenu/mainmenu_tip_frame_9s";
    const string BlueFrameResource = "phase2a/hol_cta_blue_r2_9s";
    const string ChipFrameResource = "phase2a/hol_player_chip_r2_9s";
    const string BackChevronResource = "phase2a/hol_chevron_r2";
    const string StreakIconResource = "mainmenu/mainmenu_icon_streak";
    const string DisplayFontResource = "phase2a/fonts/HOL Menu Display SDF";
    const string BodyFontResource = "phase2a/fonts/HOL Menu Body SDF";

    const float ReferenceWidth = 1080f;
    const float ReferenceHeight = 1920f;

    static readonly Color NearWhite = new Color(0.985f, 0.975f, 1f, 1f);
    static readonly Color Cyan = new Color(0.20f, 0.94f, 1f, 1f);
    static readonly Color Ink = new Color(0.08f, 0.04f, 0.17f, 1f);
    static readonly Color Gold = new Color(1f, 0.80f, 0.20f, 1f);

    public static readonly string[] LoadedResources =
    {
        BackgroundResource,
        LogoResource,
        PlayerResource,
        AvatarResource,
        MascotSixResource,
        MascotSevenResource,
        RadarBaseResource,
        RadarSweepResource,
        StarsResource,
        ConfettiResource,
        PurpleFrameResource,
        BlueFrameResource,
        ChipFrameResource,
        BackChevronResource,
        StreakIconResource,
    };

    FakeMatchmaking matchmaking;
    RectTransform visualRoot;
    RectTransform safeRoot;
    TMP_FontAsset displayFont;
    TMP_FontAsset bodyFont;
    TMP_Text chipName;
    TMP_Text chipStreak;
    TMP_Text titleText;
    TMP_Text modeBadgeText;
    Button cancelButton;
    Button backButton;
    float nextChipRefresh;

    public bool IsReady { get; private set; }

    // Retired compatibility preview only. Production Solo entry never calls
    // this method; capture/test seams must opt in explicitly.
    public static void Install(FakeMatchmaking owner)
    {
        if (owner == null || owner.searchingPanel == null) return;

        if (owner.searchingPanel.GetComponent<SoloSearchVisuals>() == null)
            owner.searchingPanel.AddComponent<SoloSearchVisuals>();
    }

    void Awake()
    {
        matchmaking = FindObjectOfType<FakeMatchmaking>(true);
        Build();
    }

    void OnEnable()
    {
        L10n.OnLanguageChanged += RefreshCopy;
        RefreshCopy();
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= RefreshCopy;
    }

    void LateUpdate()
    {
        if (!IsReady || Time.unscaledTime < nextChipRefresh) return;
        nextChipRefresh = Time.unscaledTime + 0.25f;
        RefreshPlayerChip();
    }

    void Build()
    {
        if (transform.Find(VisualRootName) != null)
        {
            IsReady = true;
            return;
        }

        matchmaking = matchmaking ?? FindObjectOfType<FakeMatchmaking>(true);
        if (matchmaking == null)
        {
            Debug.LogError("[SoloSearchVisuals] Missing FakeMatchmaking owner.");
            return;
        }

        displayFont = Resources.Load<TMP_FontAsset>(DisplayFontResource);
        bodyFont = Resources.Load<TMP_FontAsset>(BodyFontResource);

        Sprite background = LoadRequired(BackgroundResource);
        Sprite logo = LoadRequired(LogoResource);
        Sprite player = LoadRequired(PlayerResource);
        Sprite avatar = LoadRequired(AvatarResource);
        Sprite six = LoadRequired(MascotSixResource);
        Sprite seven = LoadRequired(MascotSevenResource);
        Sprite radarBase = LoadRequired(RadarBaseResource);
        Sprite radarSweep = LoadRequired(RadarSweepResource);
        Sprite stars = LoadRequired(StarsResource);
        Sprite confetti = LoadRequired(ConfettiResource);
        Sprite purple = LoadRequired(PurpleFrameResource);
        Sprite blue = LoadRequired(BlueFrameResource);
        Sprite chip = LoadRequired(ChipFrameResource);
        Sprite chevron = LoadRequired(BackChevronResource);
        Sprite streak = LoadRequired(StreakIconResource);

        IsReady = ArtReady(
            background, logo, player, avatar, six, seven, radarBase,
            radarSweep, stars, confetti, purple, blue, chip, chevron,
            streak) && displayFont != null && bodyFont != null;

        if (!IsReady)
        {
            Debug.LogError(
                "[SoloSearchVisuals] Required production artwork/fonts are missing.");
            return;
        }

        var group = GetComponent<CanvasGroup>();
        if (group == null)
            group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;

        var legacyImage = GetComponent<Image>();
        if (legacyImage != null)
        {
            legacyImage.enabled = false;
            legacyImage.raycastTarget = false;
        }

        cancelButton = EnsureOwnedButton(transform, "CancelButton");
        cancelButton.onClick.RemoveListener(matchmaking.CancelSearch);
        cancelButton.onClick.AddListener(matchmaking.CancelSearch);
        ClearButtonPresentation(cancelButton.transform);

        visualRoot = (RectTransform)RuntimeUI.CreateObject(
            VisualRootName, transform).transform;
        Stretch(visualRoot);
        visualRoot.SetAsFirstSibling();

        var backgroundImage = EnsureImage(visualRoot, "SearchBackground");
        Stretch(backgroundImage.rectTransform);
        ConfigureImage(
            backgroundImage, background, false, Image.Type.Simple);
        // The full-screen modal background intentionally blocks underlying Home
        // input while preparation is active.
        backgroundImage.raycastTarget = true;

        var starsImage = EnsureImage(visualRoot, "SearchStars");
        Stretch(starsImage.rectTransform);
        ConfigureImage(starsImage, stars, false, Image.Type.Simple);

        var confettiImage = EnsureImage(visualRoot, "SearchConfetti");
        Stretch(confettiImage.rectTransform);
        ConfigureImage(confettiImage, confetti, false, Image.Type.Simple);

        var outer = EnsureImage(visualRoot, "SearchOuterFrame");
        ConfigureImage(outer, purple, false, Image.Type.Sliced);
        outer.pixelsPerUnitMultiplier = 2f;
        Place(outer.rectTransform, Vector2.zero, new Vector2(1032f, 1872f));

        safeRoot = EnsureRect(visualRoot, SafeRootName);
        Stretch(safeRoot);
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            ResponsiveSafeAreaRoot.Attach(
                safeRoot, canvas.transform as RectTransform,
                new Vector2(ReferenceWidth, ReferenceHeight));
        }

        BuildTopBar(safeRoot, purple, chip, avatar, chevron, streak);

        var logoImage = EnsureImage(safeRoot, "SearchLogo");
        ConfigureImage(logoImage, logo, true, Image.Type.Simple);
        Place(
            logoImage.rectTransform, new Vector2(0f, 690f),
            new Vector2(585f, 310f));

        var ribbon = EnsureImage(safeRoot, "SearchTitleRibbon");
        ConfigureImage(ribbon, purple, false, Image.Type.Sliced);
        ribbon.pixelsPerUnitMultiplier = 2f;
        Place(
            ribbon.rectTransform, new Vector2(0f, 495f),
            new Vector2(900f, 150f));

        titleText = EnsureText(
            ribbon.transform, "SearchTitle", 58f, displayFont, NearWhite,
            TextAlignmentOptions.Center);
        StretchText(titleText.rectTransform, 50f, 20f);
        ConfigureDisplayText(titleText, 40f, 58f);
        SetLocalized(titleText, "solo_search_title");

        var card = EnsureImage(safeRoot, "SearchCard");
        ConfigureImage(card, blue, false, Image.Type.Sliced);
        card.pixelsPerUnitMultiplier = 2f;
        Place(
            card.rectTransform, new Vector2(0f, 70f),
            new Vector2(940f, 650f));

        var playerImage = EnsureImage(card.transform, "SearchPlayer");
        ConfigureImage(playerImage, player, true, Image.Type.Simple);
        Place(
            playerImage.rectTransform, new Vector2(-305f, -10f),
            new Vector2(365f, 440f));

        modeBadgeText = EnsureText(
            card.transform, "SearchModeBadge", 24f, bodyFont, Cyan,
            TextAlignmentOptions.Center);
        Place(
            modeBadgeText.rectTransform, new Vector2(-302f, 245f),
            new Vector2(300f, 46f));
        ConfigureBodyText(modeBadgeText, 20f, 25f);

        var radarRoot = EnsureRect(card.transform, "SearchRadarRoot");
        Place(
            radarRoot, new Vector2(35f, 25f), new Vector2(340f, 340f));

        var radarBaseImage = EnsureImage(radarRoot, "SearchRadarBase");
        Stretch(radarBaseImage.rectTransform);
        ConfigureImage(
            radarBaseImage, radarBase, true, Image.Type.Simple);

        var radarSweepImage = EnsureImage(radarRoot, "SearchRadarSweep");
        Stretch(radarSweepImage.rectTransform);
        ConfigureImage(
            radarSweepImage, radarSweep, true, Image.Type.Simple);
        radarSweepImage.gameObject.AddComponent<CartoonRadarSweep>();

        TMP_Text status = matchmaking.searchingText;
        if (status == null)
        {
            status = EnsureText(
                card.transform, "SearchStatus", 34f, displayFont,
                NearWhite, TextAlignmentOptions.Center);
            matchmaking.searchingText = status;
        }
        else
        {
            status.transform.SetParent(card.transform, false);
            status.gameObject.SetActive(true);
            status.name = "SearchStatus";
        }

        status.font = displayFont;
        status.fontStyle = FontStyles.Bold;
        status.color = NearWhite;
        status.alignment = TextAlignmentOptions.Center;
        Place(
            status.rectTransform, new Vector2(310f, -15f),
            new Vector2(270f, 190f));
        ConfigureDisplayText(status, 27f, 36f);

        var ellipsis = status.GetComponent<AnimatedEllipsis>();
        if (ellipsis == null)
            ellipsis = status.gameObject.AddComponent<AnimatedEllipsis>();
        ellipsis.text = status;
        ellipsis.stepSeconds = 0.32f;

        Reparent(cancelButton.transform, safeRoot);
        StyleButton(cancelButton, blue, Ink);
        Place(
            (RectTransform)cancelButton.transform, new Vector2(0f, -500f),
            new Vector2(520f, 112f));
        ConfigureButtonLabel(cancelButton, "cancel", 44f, Ink);

        var sixImage = EnsureImage(safeRoot, "SearchMascotSix");
        ConfigureImage(sixImage, six, true, Image.Type.Simple);
        Place(
            sixImage.rectTransform, new Vector2(-410f, -790f),
            new Vector2(265f, 300f));

        var sevenImage = EnsureImage(safeRoot, "SearchMascotSeven");
        ConfigureImage(sevenImage, seven, true, Image.Type.Simple);
        Place(
            sevenImage.rectTransform, new Vector2(410f, -790f),
            new Vector2(265f, 300f));

        HideLegacyPresentation();
        RefreshCopy();
        RefreshPlayerChip();
    }

    void BuildTopBar(
        Transform safe,
        Sprite purple,
        Sprite chip,
        Sprite avatar,
        Sprite chevron,
        Sprite streak)
    {
        backButton = EnsureOwnedButton(safe, "SearchBackButton");
        Place(
            (RectTransform)backButton.transform,
            new Vector2(-484f, 842f), new Vector2(90f, 90f));
        backButton.onClick.RemoveListener(matchmaking.CancelSearch);
        backButton.onClick.AddListener(matchmaking.CancelSearch);
        StyleButton(backButton, purple, NearWhite);
        HideButtonLabels(backButton.transform);

        var backIcon = EnsureImage(backButton.transform, "SearchBackIcon");
        ConfigureImage(backIcon, chevron, true, Image.Type.Simple);
        Place(
            backIcon.rectTransform, Vector2.zero, new Vector2(46f, 58f));
        backIcon.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

        var playerChip = EnsureImage(safe, "SearchPlayerChip");
        ConfigureImage(playerChip, chip, false, Image.Type.Sliced);
        playerChip.pixelsPerUnitMultiplier = 2f;
        Place(
            playerChip.rectTransform, new Vector2(350f, 842f),
            new Vector2(365f, 118f));

        var avatarImage = EnsureImage(
            playerChip.transform, "SearchPlayerAvatar");
        ConfigureImage(avatarImage, avatar, true, Image.Type.Simple);
        Place(
            avatarImage.rectTransform, new Vector2(-126f, 0f),
            new Vector2(84f, 84f));

        chipName = EnsureText(
            playerChip.transform, "SearchPlayerName", 29f, bodyFont,
            NearWhite, TextAlignmentOptions.Center);
        Place(
            chipName.rectTransform, new Vector2(44f, 23f),
            new Vector2(205f, 40f));
        chipName.enableAutoSizing = true;
        chipName.fontSizeMin = 22f;
        chipName.fontSizeMax = 30f;
        chipName.overflowMode = TextOverflowModes.Ellipsis;

        var streakIcon = EnsureImage(
            playerChip.transform, "SearchStreakIcon");
        ConfigureImage(streakIcon, streak, true, Image.Type.Simple);
        Place(
            streakIcon.rectTransform, new Vector2(-18f, -28f),
            new Vector2(42f, 42f));

        chipStreak = EnsureText(
            playerChip.transform, "SearchStreak", 29f, bodyFont, Gold,
            TextAlignmentOptions.Center);
        Place(
            chipStreak.rectTransform, new Vector2(62f, -28f),
            new Vector2(120f, 40f));
    }

    void RefreshCopy()
    {
        if (!IsReady) return;

        if (modeBadgeText != null)
        {
            modeBadgeText.text = L10n.Current == L10n.Language.Greek
                ? "SOLO • AI ΑΝΤΙΠΑΛΟΣ"
                : "SOLO • AI OPPONENT";
        }

        RefreshPlayerChip();
    }

    void RefreshPlayerChip()
    {
        if (chipName == null || chipStreak == null) return;

        string player = PlayerPrefs.GetString("PlayerName", "");
        if (string.IsNullOrWhiteSpace(player))
            player = L10n.Get("player_default");

        chipName.text = player;
        chipStreak.text = GameStats.CurrentStreak.ToString();
    }

    void ConfigureButtonLabel(
        Button button,
        string key,
        float size,
        Color color)
    {
        TMP_Text label = EnsureText(
            button.transform, "SearchActionLabel", size, displayFont,
            color, TextAlignmentOptions.Center);
        StretchText(label.rectTransform, 28f, 14f);
        ConfigureDisplayText(label, size - 10f, size);
        SetLocalized(label, key);
    }

    static void StyleButton(Button button, Sprite sprite, Color labelColor)
    {
        if (button == null) return;

        var image = button.GetComponent<Image>();
        if (image == null)
            image = button.gameObject.AddComponent<Image>();
        image.enabled = true;
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 2f;
        image.preserveAspect = false;
        image.color = Color.white;
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
        RuntimeUI.AttachJuice(button);

        foreach (TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true))
            text.color = labelColor;
    }

    void HideLegacyPresentation()
    {
        foreach (string name in new[]
        {
            "ExactSearchingLogo",
            "ExactSearchingPlayer",
            "ExactSearchingOpponent",
            "ExactSearchingVs",
            "Radar",
            "Rocket",
        })
        {
            Transform legacy = Find<Transform>(transform, name);
            if (legacy != null && !IsDescendantOf(legacy, visualRoot))
                legacy.gameObject.SetActive(false);
        }
    }

    static bool IsDescendantOf(Transform child, Transform parent)
    {
        while (child != null)
        {
            if (child == parent) return true;
            child = child.parent;
        }
        return false;
    }

    static void ClearButtonPresentation(Transform root)
    {
        if (root == null) return;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            child.gameObject.SetActive(false);
            child.SetParent(null, false);
            RuntimeUI.DestroyNow(child.gameObject);
        }
    }

    static void HideButtonLabels(Transform root)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            text.gameObject.SetActive(false);
        foreach (Text text in root.GetComponentsInChildren<Text>(true))
            text.gameObject.SetActive(false);
    }

    static void ConfigureDisplayText(
        TMP_Text text,
        float minimum,
        float maximum)
    {
        text.fontStyle = FontStyles.Bold;
        text.enableAutoSizing = true;
        text.fontSizeMin = minimum;
        text.fontSizeMax = maximum;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;

        var shadow = text.GetComponent<Shadow>();
        if (shadow == null)
            shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.02f, 0.01f, 0.12f, 0.68f);
        shadow.effectDistance = new Vector2(2f, -3f);
        shadow.useGraphicAlpha = true;
    }

    static void ConfigureBodyText(
        TMP_Text text,
        float minimum,
        float maximum)
    {
        text.enableAutoSizing = true;
        text.fontSizeMin = minimum;
        text.fontSizeMax = maximum;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
    }

    static void SetLocalized(TMP_Text text, string key)
    {
        var localized = text.GetComponent<LocalizedText>();
        if (localized == null)
        {
            RuntimeUI.Localize(text, key);
            localized = text.GetComponent<LocalizedText>();
        }
        if (localized != null)
            localized.key = key;
        text.text = L10n.Get(key);
    }

    static bool ArtReady(params Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0) return false;
        foreach (Sprite sprite in sprites)
            if (sprite == null) return false;
        return true;
    }

    static Sprite LoadRequired(string resource)
    {
        Sprite sprite = Resources.Load<Sprite>(resource);
        if (sprite == null)
            Debug.LogError("[SoloSearchVisuals] Missing Resources/" + resource + ".");
        return sprite;
    }

    static RectTransform EnsureRect(Transform parent, string name)
    {
        Transform existing = DirectChild(parent, name);
        if (existing is RectTransform rect)
        {
            rect.gameObject.SetActive(true);
            return rect;
        }
        return (RectTransform)RuntimeUI.CreateObject(name, parent).transform;
    }

    static Button EnsureOwnedButton(Transform parent, string name)
    {
        RectTransform rect = EnsureRect(parent, name);
        var image = rect.GetComponent<Image>();
        if (image == null)
            image = rect.gameObject.AddComponent<Image>();
        image.raycastTarget = true;

        var button = rect.GetComponent<Button>();
        if (button == null)
            button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        RuntimeUI.AttachJuice(button);
        return button;
    }

    static Image EnsureImage(Transform parent, string name)
    {
        RectTransform rect = EnsureRect(parent, name);
        var image = rect.GetComponent<Image>();
        if (image == null)
            image = rect.gameObject.AddComponent<Image>();
        return image;
    }

    static TMP_Text EnsureText(
        Transform parent,
        string name,
        float size,
        TMP_FontAsset font,
        Color color,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = EnsureRect(parent, name);
        var text = rect.GetComponent<TextMeshProUGUI>();
        if (text == null)
            text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    static void ConfigureImage(
        Image image,
        Sprite sprite,
        bool preserveAspect,
        Image.Type type)
    {
        image.enabled = true;
        image.sprite = sprite;
        image.type = type;
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    static void Reparent(Transform child, Transform parent)
    {
        if (child.parent != parent)
            child.SetParent(parent, false);
        child.gameObject.SetActive(true);
        child.SetAsLastSibling();
    }

    static void Place(
        RectTransform rect,
        Vector2 position,
        Vector2 size)
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
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    static void StretchText(
        RectTransform rect,
        float horizontalInset,
        float verticalInset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontalInset, verticalInset);
        rect.offsetMax = new Vector2(-horizontalInset, -verticalInset);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    static Transform DirectChild(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name) return child;
        }
        return null;
    }

    static T Find<T>(Transform parent, string name) where T : Component
    {
        if (parent == null) return null;
        foreach (T item in parent.GetComponentsInChildren<T>(true))
            if (item.name == name) return item;
        return null;
    }

}
