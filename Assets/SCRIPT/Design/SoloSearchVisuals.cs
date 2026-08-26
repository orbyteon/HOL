using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Sole presentation owner for the truthful Solo-vs-AI preparation screen.
// FakeMatchmaking owns the deterministic transition/cancellation lifecycle;
// this class only composes approved modular sprites around the real status and
// Cancel control.
[DefaultExecutionOrder(2500)]
public sealed class SoloSearchVisuals : MonoBehaviour
{
    public const string VisualRootName = "SoloSearchVisualRoot";
    public const string SafeRootName = "SoloSearchSafeRoot";

    const string BackgroundResource = CartoonUiKit.Background;
    const string LogoResource = CartoonUiKit.Logo;
    const string PlayerResource = CartoonUiKit.PlayerAvatar;
    const string AvatarResource = CartoonUiKit.PlayerAvatar;
    const string MascotSixResource = CartoonUiKit.MascotSix;
    const string MascotSevenResource = CartoonUiKit.MascotSeven;
    const string RadarBaseResource = CartoonUiKit.RadarBase;
    const string RadarSweepResource = CartoonUiKit.RadarSweep;
    const string StarsResource = "mainmenu/mainmenu_deco_stars";
    const string ConfettiResource = "mainmenu/mainmenu_deco_confetti";
    const string TitleRibbonResource = CartoonUiKit.TitleRibbon;
    const string CardFrameResource = "dailyhunt/v1/daily_challenge_board_v1";
    const string BlueFrameResource = CartoonUiKit.CyanAction;
    const string ChipFrameResource = CartoonUiKit.PlayerChip;
    const string AvatarRingResource = CartoonUiKit.PlayerAvatarRing;
    const string BackButtonResource = CartoonUiKit.BackButton;
    const string PortalResource = CartoonUiKit.FloorPortal;
    const string StreakIconResource = "mainmenu/mainmenu_icon_streak";
    const string DisplayFontResource = CartoonUiKit.DisplayFont;
    const string BodyFontResource = CartoonUiKit.BodyFont;

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
        TitleRibbonResource,
        CardFrameResource,
        BlueFrameResource,
        ChipFrameResource,
        AvatarRingResource,
        BackButtonResource,
        PortalResource,
        StreakIconResource,
    };

    FakeMatchmaking matchmaking;
    RectTransform visualRoot;
    RectTransform safeRoot;
    RectTransform backRect;
    RectTransform chipRect;
    RectTransform logoRect;
    RectTransform ribbonRect;
    RectTransform cardRect;
    RectTransform cancelRect;
    RectTransform portalRect;
    RectTransform mascotSixRect;
    RectTransform mascotSevenRect;
    TMP_FontAsset displayFont;
    TMP_FontAsset bodyFont;
    TMP_Text chipName;
    TMP_Text chipStreak;
    TMP_Text titleText;
    TMP_Text modeBadgeText;
    TMP_Text searchStatus;
    Button cancelButton;
    Button backButton;
    float nextChipRefresh;
    int lastLayoutWidth = -1;
    int lastLayoutHeight = -1;

    public bool IsReady { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;

        var owner = FindInScene<FakeMatchmaking>(scene);
        Install(owner);
    }

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
        if (!IsReady) return;
        ApplyResponsiveLayout();

        // The board initializes concurrently behind this modal. Keep the
        // modal fully opaque and top-sorted even if legacy transition state
        // touches the shared panel after it was activated.
        var group = GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
        var modalCanvas = GetComponent<Canvas>();
        if (modalCanvas != null)
        {
            modalCanvas.overrideSorting = true;
            modalCanvas.sortingOrder = 50;
        }

        if (Time.unscaledTime < nextChipRefresh) return;
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
        Sprite titleRibbon = LoadRequired(TitleRibbonResource);
        Sprite cardFrame = LoadRequired(CardFrameResource);
        Sprite blue = LoadRequired(BlueFrameResource);
        Sprite chip = LoadRequired(ChipFrameResource);
        Sprite avatarRing = LoadRequired(AvatarRingResource);
        Sprite backButtonSprite = LoadRequired(BackButtonResource);
        Sprite portal = LoadRequired(PortalResource);
        Sprite streak = LoadRequired(StreakIconResource);

        IsReady = ArtReady(
            background, logo, player, avatar, six, seven, radarBase,
            radarSweep, stars, confetti, titleRibbon, cardFrame, blue,
            chip, avatarRing, backButtonSprite, portal,
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

        // The duel board initializes while this modal is visible. Give the
        // searching panel its own nested sorting boundary so the already-built
        // duel header can never peek through on tall viewports.
        var modalCanvas = GetComponent<Canvas>();
        if (modalCanvas == null)
            modalCanvas = gameObject.AddComponent<Canvas>();
        modalCanvas.overrideSorting = true;
        modalCanvas.sortingOrder = 50;
        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        var legacyImage = GetComponent<Image>();
        if (legacyImage != null)
        {
            legacyImage.enabled = false;
            legacyImage.raycastTarget = false;
        }

        cancelButton = Find<Button>(transform, "CancelButton");
        if (cancelButton == null)
        {
            cancelButton = RuntimeUI.CreateButton(
                transform, "CancelButton", L10n.Get("cancel"),
                Vector2.zero, new Vector2(520f, 110f), Color.white,
                Ink);
            cancelButton.onClick.AddListener(matchmaking.CancelSearch);
        }
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

        safeRoot = EnsureRect(visualRoot, SafeRootName);
        Stretch(safeRoot);
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            ResponsiveSafeAreaRoot.Attach(
                safeRoot, canvas.transform as RectTransform,
                new Vector2(ReferenceWidth, ReferenceHeight));
        }

        BuildTopBar(
            safeRoot, backButtonSprite, chip, avatarRing, avatar, streak);

        var logoImage = EnsureImage(safeRoot, "SearchLogo");
        ConfigureImage(logoImage, logo, true, Image.Type.Simple);
        logoRect = logoImage.rectTransform;
        Place(
            logoRect, new Vector2(0f, 728f),
            new Vector2(455f, 306f));

        var ribbon = EnsureImage(safeRoot, "SearchTitleRibbon");
        ConfigureImage(ribbon, titleRibbon, false, Image.Type.Simple);
        ribbonRect = ribbon.rectTransform;
        Place(
            ribbonRect, new Vector2(0f, 560f),
            new Vector2(940f, 164f));

        titleText = EnsureText(
            ribbon.transform, "SearchTitle", 58f, displayFont, NearWhite,
            TextAlignmentOptions.Center);
        StretchText(titleText.rectTransform, 50f, 20f);
        ConfigureDisplayText(titleText, 40f, 58f);
        titleText.fontStyle |= FontStyles.UpperCase;
        SetLocalized(titleText, "solo_search_title");

        var card = EnsureImage(safeRoot, "SearchCard");
        ConfigureImage(card, cardFrame, false, Image.Type.Simple);
        cardRect = card.rectTransform;
        Place(
            cardRect, new Vector2(0f, 130f),
            new Vector2(1030f, 690f));

        var playerImage = EnsureImage(card.transform, "SearchPlayer");
        ConfigureImage(playerImage, player, true, Image.Type.Simple);
        Place(
            playerImage.rectTransform, new Vector2(-326f, -4f),
            new Vector2(385f, 520f));

        var radarRoot = EnsureRect(card.transform, "SearchRadarRoot");
        Place(
            radarRoot, new Vector2(12f, 18f), new Vector2(410f, 410f));

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
            status.rectTransform, new Vector2(315f, -10f),
            new Vector2(350f, 230f));
        ConfigureDisplayText(status, 19f, 33f);
        status.overflowMode = TextOverflowModes.Truncate;
        searchStatus = status;

        var ellipsis = status.GetComponent<AnimatedEllipsis>();
        if (ellipsis == null)
            ellipsis = status.gameObject.AddComponent<AnimatedEllipsis>();
        ellipsis.text = status;
        ellipsis.stepSeconds = 0.32f;

        Reparent(cancelButton.transform, safeRoot);
        cancelRect = (RectTransform)cancelButton.transform;
        StyleButton(cancelButton, blue, NearWhite);
        Place(
            cancelRect, new Vector2(0f, -290f),
            new Vector2(520f, 150f));
        ConfigureButtonLabel(cancelButton, "cancel", 52f, NearWhite);

        var portalImage = EnsureImage(safeRoot, "SearchFloorPortal");
        ConfigureImage(portalImage, portal, true, Image.Type.Simple);
        portalRect = portalImage.rectTransform;
        Place(
            portalRect, new Vector2(0f, -600f),
            new Vector2(660f, 185f));

        var sixImage = EnsureImage(safeRoot, "SearchMascotSix");
        ConfigureImage(sixImage, six, true, Image.Type.Simple);
        mascotSixRect = sixImage.rectTransform;
        Place(
            mascotSixRect, new Vector2(-392f, -560f),
            new Vector2(300f, 360f));

        var sevenImage = EnsureImage(safeRoot, "SearchMascotSeven");
        ConfigureImage(sevenImage, seven, true, Image.Type.Simple);
        mascotSevenRect = sevenImage.rectTransform;
        Place(
            mascotSevenRect, new Vector2(392f, -560f),
            new Vector2(300f, 360f));

        HideLegacyPresentation();
        RefreshCopy();
        RefreshPlayerChip();
        ApplyResponsiveLayout(true);
    }

    void BuildTopBar(
        Transform safe,
        Sprite backSprite,
        Sprite chip,
        Sprite avatarRing,
        Sprite avatar,
        Sprite streak)
    {
        backButton = RuntimeUI.CreateButton(
            safe, "SearchBackButton", string.Empty,
            new Vector2(-450f, 840f), new Vector2(128f, 128f),
            Color.white, NearWhite);
        backButton.onClick.AddListener(matchmaking.CancelSearch);
        StyleButton(backButton, backSprite, NearWhite);
        backRect = (RectTransform)backButton.transform;
        HideButtonLabels(backButton.transform);

        var playerChip = EnsureImage(safe, "SearchPlayerChip");
        ConfigureImage(playerChip, chip, false, Image.Type.Simple);
        chipRect = playerChip.rectTransform;
        Place(
            chipRect, new Vector2(356f, 838f),
            new Vector2(320f, 142f));

        var ringImage = EnsureImage(
            playerChip.transform, "SearchPlayerAvatarRing");
        ConfigureImage(ringImage, avatarRing, true, Image.Type.Simple);
        Place(
            ringImage.rectTransform, new Vector2(-110f, 0f),
            new Vector2(102f, 102f));

        var avatarClip = EnsureRect(
            playerChip.transform, "SearchPlayerAvatarClip");
        Place(
            avatarClip, new Vector2(-110f, 2f),
            new Vector2(94f, 94f));
        if (avatarClip.GetComponent<RectMask2D>() == null)
            avatarClip.gameObject.AddComponent<RectMask2D>();

        var avatarImage = EnsureImage(
            avatarClip, "SearchPlayerAvatar");
        ConfigureImage(avatarImage, avatar, true, Image.Type.Simple);
        Place(
            avatarImage.rectTransform, new Vector2(0f, -25f),
            new Vector2(124f, 124f));
        ringImage.transform.SetAsLastSibling();

        chipName = EnsureText(
            playerChip.transform, "SearchPlayerName", 29f, bodyFont,
            NearWhite, TextAlignmentOptions.Center);
        Place(
            chipName.rectTransform, new Vector2(45f, 27f),
            new Vector2(190f, 44f));
        chipName.enableAutoSizing = true;
        chipName.fontSizeMin = 22f;
        chipName.fontSizeMax = 30f;
        chipName.overflowMode = TextOverflowModes.Ellipsis;

        var streakIcon = EnsureImage(
            playerChip.transform, "SearchStreakIcon");
        ConfigureImage(streakIcon, streak, true, Image.Type.Simple);
        Place(
            streakIcon.rectTransform, new Vector2(-20f, -30f),
            new Vector2(44f, 44f));

        chipStreak = EnsureText(
            playerChip.transform, "SearchStreak", 29f, bodyFont, Gold,
            TextAlignmentOptions.Center);
        Place(
            chipStreak.rectTransform, new Vector2(62f, -30f),
            new Vector2(120f, 40f));
    }

    void ApplyResponsiveLayout(bool force = false)
    {
        if (backRect == null || chipRect == null || logoRect == null ||
            ribbonRect == null || cardRect == null || cancelRect == null ||
            portalRect == null || mascotSixRect == null ||
            mascotSevenRect == null)
            return;

        int width = Mathf.Max(1, Screen.width);
        int height = Mathf.Max(1, Screen.height);
        if (!force && width == lastLayoutWidth && height == lastLayoutHeight)
            return;

        lastLayoutWidth = width;
        lastLayoutHeight = height;
        float aspect = height / (float)width;
        float tall = Mathf.InverseLerp(1.78f, 2.22f, aspect);

        Place(backRect, new Vector2(-450f, 840f + 185f * tall),
            new Vector2(128f, 128f));
        Place(chipRect, new Vector2(356f, 838f + 185f * tall),
            new Vector2(320f, 142f));
        Place(logoRect, new Vector2(0f, 728f + 150f * tall),
            new Vector2(455f, 306f));
        Place(ribbonRect, new Vector2(0f, 560f + 100f * tall),
            new Vector2(940f, 164f));
        Place(cardRect, new Vector2(0f, 130f + 20f * tall),
            new Vector2(1030f, 690f));
        Place(cancelRect, new Vector2(0f, -290f - 70f * tall),
            new Vector2(520f, 150f));
        Place(portalRect, new Vector2(0f, -600f - 220f * tall),
            new Vector2(660f, 185f));
        Place(mascotSixRect, new Vector2(-392f, -560f - 220f * tall),
            new Vector2(300f, 360f));
        Place(mascotSevenRect, new Vector2(392f, -560f - 220f * tall),
            new Vector2(300f, 360f));
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
        if (searchStatus != null)
        {
            searchStatus.fontSizeMin = 19f;
            searchStatus.fontSizeMax =
                L10n.Current == L10n.Language.Greek ? 27f : 33f;
            searchStatus.ForceMeshUpdate();
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
        label.fontStyle |= FontStyles.UpperCase;
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
        image.type = Image.Type.Simple;
        image.pixelsPerUnitMultiplier = 1f;
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

    static T FindInScene<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid()) return null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }
}
