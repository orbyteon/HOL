using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Sole presentation owner for the real Daily Hunt number challenge.
// DailyHunt remains responsible for date-seeded state, guesses, revive/share
// callbacks and persistence. This owner only seats those real controls inside
// the approved cartoon composition.
[DisallowMultipleComponent]
public sealed class DailyHuntVisuals : MonoBehaviour
{
    public const string VisualRootName = "DailyHuntVisualRoot";
    public const string SafeRootName = "DailyHuntSafeRoot";

    const string BackgroundResource = "phase2a/hol_neon_reference_bg_r3";
    const string LogoResource = "reference/hol_logo_exact";
    const string AvatarResource = "reference/player_cyan_exact";
    const string MascotSixResource = "reference/mascot_6_exact";
    const string MascotSevenResource = "reference/mascot_7_exact";
    const string CalendarResource = "cartoon/cartoon_daily_calendar";
    const string ChestResource = "cartoon/cartoon_reward_chest";
    const string TrophyResource = "reference/board_trophy_exact";
    const string StarsResource = "mainmenu/mainmenu_deco_stars";
    const string ConfettiResource = "mainmenu/mainmenu_deco_confetti";
    const string PurpleFrameResource = "mainmenu/mainmenu_tip_frame_9s";
    const string BlueFrameResource = "phase2a/hol_cta_blue_r2_9s";
    const string MagentaFrameResource = "phase2a/hol_cta_magenta_r2_9s";
    const string GoldFrameResource = "phase2a/hol_cta_gold_r2_9s";
    const string ChipFrameResource = "phase2a/hol_player_chip_r2_9s";
    const string BackChevronResource = "phase2a/hol_chevron_r2";

    // Daily Hunt renders live mixed-case EN/EL copy, player names, day numbers
    // and ▲ / ▼ / ● trail symbols. The canonical Liberation Sans production
    // chain is statically baked and covered by ProductionTextFontTests.
    const string ProductionFontResource =
        "Fonts & Materials/LiberationSans SDF";

    const float ReferenceWidth = 1080f;
    const float ReferenceHeight = 1920f;

    static readonly Color NearWhite = new Color(0.985f, 0.975f, 1f, 1f);
    static readonly Color Cyan = new Color(0.20f, 0.94f, 1f, 1f);
    static readonly Color Gold = new Color(1f, 0.80f, 0.20f, 1f);
    static readonly Color Muted = new Color(0.88f, 0.84f, 0.96f, 0.90f);
    static readonly Color Ink = new Color(0.08f, 0.04f, 0.17f, 1f);

    public static readonly string[] LoadedResources =
    {
        BackgroundResource,
        LogoResource,
        AvatarResource,
        MascotSixResource,
        MascotSevenResource,
        CalendarResource,
        ChestResource,
        TrophyResource,
        StarsResource,
        ConfettiResource,
        PurpleFrameResource,
        BlueFrameResource,
        MagentaFrameResource,
        GoldFrameResource,
        ChipFrameResource,
        BackChevronResource,
    };

    RectTransform visualRoot;
    RectTransform safeRoot;
    TMP_FontAsset productionFont;
    TMP_Text chipName;
    TMP_Text chipWins;
    TMP_Text challengeHeading;
    TMP_Text rewardHeading;
    TMP_Text trailText;
    float nextChipRefresh;

    public bool IsReady { get; private set; }
    public TMP_FontAsset ProductionFont => productionFont;

    public static void Apply(Transform panel)
    {
        if (panel == null) return;

        var owner = panel.GetComponent<DailyHuntVisuals>();
        if (owner == null)
            owner = panel.gameObject.AddComponent<DailyHuntVisuals>();
        owner.Build(panel);
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

        RefreshVisibleTrail();
        if (Time.unscaledTime < nextChipRefresh) return;
        nextChipRefresh = Time.unscaledTime + 0.25f;
        RefreshPlayerChip();
    }

    void Build(Transform panel)
    {
        if (panel == null) return;
        if (panel.Find(VisualRootName) != null)
        {
            IsReady = true;
            return;
        }

        productionFont = Resources.Load<TMP_FontAsset>(ProductionFontResource);

        Sprite background = LoadRequired(BackgroundResource);
        Sprite logo = LoadRequired(LogoResource);
        Sprite avatar = LoadRequired(AvatarResource);
        Sprite six = LoadRequired(MascotSixResource);
        Sprite seven = LoadRequired(MascotSevenResource);
        Sprite calendar = LoadRequired(CalendarResource);
        Sprite chest = LoadRequired(ChestResource);
        Sprite trophy = LoadRequired(TrophyResource);
        Sprite stars = LoadRequired(StarsResource);
        Sprite confetti = LoadRequired(ConfettiResource);
        Sprite purple = LoadRequired(PurpleFrameResource);
        Sprite blue = LoadRequired(BlueFrameResource);
        Sprite magenta = LoadRequired(MagentaFrameResource);
        Sprite goldFrame = LoadRequired(GoldFrameResource);
        Sprite chip = LoadRequired(ChipFrameResource);
        Sprite chevron = LoadRequired(BackChevronResource);

        IsReady = ArtReady(
            background, logo, avatar, six, seven, calendar, chest, trophy,
            stars, confetti, purple, blue, magenta, goldFrame, chip,
            chevron) && productionFont != null;

        if (!IsReady)
        {
            Debug.LogError(
                "[DailyHuntVisuals] Required production artwork/font is missing.");
            return;
        }

        var panelImage = panel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.enabled = false;
            panelImage.raycastTarget = false;
        }

        TMP_Text title = Find<TMP_Text>(panel, "Title");
        TMP_Text status = Find<TMP_Text>(panel, "Status");
        TMP_Text trail = Find<TMP_Text>(panel, "Trail");
        TMP_Text streak = Find<TMP_Text>(panel, "Streak");
        TMP_InputField input = Find<TMP_InputField>(panel, "GuessInput");
        Button submit = Find<Button>(panel, "SubmitGuessButton");
        Button revive = Find<Button>(panel, "ReviveButton");
        Button share = Find<Button>(panel, "ShareButton");
        Button close = Find<Button>(panel, "CloseButton");

        if (title == null || status == null || trail == null ||
            streak == null || input == null || submit == null ||
            revive == null || share == null || close == null)
        {
            IsReady = false;
            Debug.LogError("[DailyHuntVisuals] Real Daily Hunt controls are missing.");
            return;
        }

        Transform oldCard = Find<Transform>(panel, "Card");
        if (oldCard != null)
            oldCard.gameObject.SetActive(false);

        visualRoot = (RectTransform)RuntimeUI.CreateObject(
            VisualRootName, panel).transform;
        Stretch(visualRoot);
        visualRoot.SetAsFirstSibling();

        var backgroundImage = EnsureImage(visualRoot, "DailyBackground");
        Stretch(backgroundImage.rectTransform);
        ConfigureImage(backgroundImage, background, false, Image.Type.Simple);
        backgroundImage.raycastTarget = true;

        var starsImage = EnsureImage(visualRoot, "DailyStars");
        Stretch(starsImage.rectTransform);
        ConfigureImage(starsImage, stars, false, Image.Type.Simple);

        var confettiImage = EnsureImage(visualRoot, "DailyConfetti");
        Stretch(confettiImage.rectTransform);
        ConfigureImage(confettiImage, confetti, false, Image.Type.Simple);

        var outer = EnsureImage(visualRoot, "DailyOuterFrame");
        ConfigureImage(outer, purple, false, Image.Type.Sliced);
        outer.pixelsPerUnitMultiplier = 2f;
        Place(outer.rectTransform, Vector2.zero, new Vector2(1032f, 1872f));

        safeRoot = EnsureRect(visualRoot, SafeRootName);
        Stretch(safeRoot);
        Canvas canvas = panel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            ResponsiveSafeAreaRoot.Attach(
                safeRoot, canvas.transform as RectTransform,
                new Vector2(ReferenceWidth, ReferenceHeight));
        }

        BuildTopBar(close, purple, chip, avatar, trophy, chevron);

        var logoImage = EnsureImage(safeRoot, "DailyLogo");
        ConfigureImage(logoImage, logo, true, Image.Type.Simple);
        Place(
            logoImage.rectTransform, new Vector2(0f, 700f),
            new Vector2(560f, 300f));

        var ribbon = EnsureImage(safeRoot, "DailyTitleRibbon");
        ConfigureImage(ribbon, purple, false, Image.Type.Sliced);
        ribbon.pixelsPerUnitMultiplier = 2f;
        Place(
            ribbon.rectTransform, new Vector2(0f, 505f),
            new Vector2(910f, 150f));

        Reparent(title.transform, ribbon.transform);
        title.font = productionFont;
        title.color = NearWhite;
        title.alignment = TextAlignmentOptions.Center;
        title.fontStyle = FontStyles.Bold;
        StretchText(title.rectTransform, 50f, 20f);
        ConfigureDisplayText(title, 38f, 54f);

        var challengeCard = EnsureImage(safeRoot, "DailyChallengeCard");
        ConfigureImage(challengeCard, blue, false, Image.Type.Sliced);
        challengeCard.pixelsPerUnitMultiplier = 2f;
        Place(
            challengeCard.rectTransform, new Vector2(0f, 40f),
            new Vector2(940f, 760f));

        var calendarImage = EnsureImage(
            challengeCard.transform, "DailyCalendarTarget");
        ConfigureImage(calendarImage, calendar, true, Image.Type.Simple);
        Place(
            calendarImage.rectTransform, new Vector2(-300f, 70f),
            new Vector2(340f, 410f));

        challengeHeading = EnsureText(
            challengeCard.transform, "DailyChallengeHeading", 34f,
            productionFont, Cyan, TextAlignmentOptions.Center);
        Place(
            challengeHeading.rectTransform, new Vector2(220f, 285f),
            new Vector2(500f, 70f));
        ConfigureDisplayText(challengeHeading, 26f, 35f);

        var statusFrame = EnsureImage(
            challengeCard.transform, "DailyStatusFrame");
        ConfigureImage(statusFrame, purple, false, Image.Type.Sliced);
        statusFrame.pixelsPerUnitMultiplier = 2f;
        Place(
            statusFrame.rectTransform, new Vector2(220f, 125f),
            new Vector2(500f, 220f));

        Reparent(status.transform, statusFrame.transform);
        status.font = productionFont;
        status.color = NearWhite;
        status.alignment = TextAlignmentOptions.Center;
        StretchText(status.rectTransform, 28f, 22f);
        ConfigureBodyText(status, 23f, 31f);

        var trailFrame = EnsureImage(
            challengeCard.transform, "DailyTrailFrame");
        ConfigureImage(trailFrame, purple, false, Image.Type.Sliced);
        trailFrame.pixelsPerUnitMultiplier = 2f;
        Place(
            trailFrame.rectTransform, new Vector2(220f, -60f),
            new Vector2(500f, 100f));

        Reparent(trail.transform, trailFrame.transform);
        trailText = trail;
        trail.font = productionFont;
        trail.color = Cyan;
        trail.alignment = TextAlignmentOptions.Center;
        StretchText(trail.rectTransform, 24f, 12f);
        ConfigureDisplayText(trail, 28f, 40f);

        Reparent(input.transform, challengeCard.transform);
        Place(
            (RectTransform)input.transform, new Vector2(120f, -220f),
            new Vector2(390f, 92f));
        StyleInput(input, purple);

        Reparent(submit.transform, challengeCard.transform);
        Place(
            (RectTransform)submit.transform, new Vector2(120f, -330f),
            new Vector2(470f, 98f));
        StyleButton(submit, goldFrame, Ink, 38f);

        var rewardCard = EnsureImage(safeRoot, "DailyRewardCard");
        ConfigureImage(rewardCard, magenta, false, Image.Type.Sliced);
        rewardCard.pixelsPerUnitMultiplier = 2f;
        Place(
            rewardCard.rectTransform, new Vector2(0f, -510f),
            new Vector2(920f, 280f));

        var chestImage = EnsureImage(
            rewardCard.transform, "DailyRewardChest");
        ConfigureImage(chestImage, chest, true, Image.Type.Simple);
        Place(
            chestImage.rectTransform, new Vector2(-300f, 0f),
            new Vector2(250f, 250f));

        rewardHeading = EnsureText(
            rewardCard.transform, "DailyRewardHeading", 36f,
            productionFont, NearWhite, TextAlignmentOptions.Center);
        Place(
            rewardHeading.rectTransform, new Vector2(185f, 85f),
            new Vector2(500f, 65f));
        ConfigureDisplayText(rewardHeading, 28f, 38f);

        Reparent(streak.transform, rewardCard.transform);
        Place(
            streak.rectTransform, new Vector2(185f, 24f),
            new Vector2(500f, 55f));
        streak.font = productionFont;
        streak.color = Gold;
        streak.alignment = TextAlignmentOptions.Center;
        ConfigureBodyText(streak, 23f, 31f);

        Reparent(revive.transform, rewardCard.transform);
        Place(
            (RectTransform)revive.transform, new Vector2(185f, -67f),
            new Vector2(500f, 86f));
        StyleButton(revive, goldFrame, Ink, 29f);

        Reparent(share.transform, rewardCard.transform);
        Place(
            (RectTransform)share.transform, new Vector2(185f, -67f),
            new Vector2(500f, 86f));
        StyleButton(share, blue, Ink, 32f);

        var sixImage = EnsureImage(safeRoot, "DailyMascotSix");
        ConfigureImage(sixImage, six, true, Image.Type.Simple);
        Place(
            sixImage.rectTransform, new Vector2(-420f, -805f),
            new Vector2(250f, 285f));

        var sevenImage = EnsureImage(safeRoot, "DailyMascotSeven");
        ConfigureImage(sevenImage, seven, true, Image.Type.Simple);
        Place(
            sevenImage.rectTransform, new Vector2(420f, -805f),
            new Vector2(250f, 285f));

        HideLegacyPresentation(panel);
        RefreshCopy();
        RefreshVisibleTrail();
        RefreshPlayerChip();
    }

    void BuildTopBar(
        Button close,
        Sprite purple,
        Sprite chip,
        Sprite avatar,
        Sprite trophy,
        Sprite chevron)
    {
        ClearButtonPresentation(close.transform);
        Reparent(close.transform, safeRoot);
        Place(
            (RectTransform)close.transform, new Vector2(-484f, 842f),
            new Vector2(90f, 90f));
        StyleButton(close, purple, NearWhite, 0f);
        HideButtonLabels(close.transform);

        var backIcon = EnsureImage(close.transform, "DailyBackIcon");
        ConfigureImage(backIcon, chevron, true, Image.Type.Simple);
        Place(backIcon.rectTransform, Vector2.zero, new Vector2(46f, 58f));
        backIcon.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

        var playerChip = EnsureImage(safeRoot, "DailyPlayerChip");
        ConfigureImage(playerChip, chip, false, Image.Type.Sliced);
        playerChip.pixelsPerUnitMultiplier = 2f;
        Place(
            playerChip.rectTransform, new Vector2(350f, 842f),
            new Vector2(365f, 118f));

        var avatarImage = EnsureImage(
            playerChip.transform, "DailyPlayerAvatar");
        ConfigureImage(avatarImage, avatar, true, Image.Type.Simple);
        Place(
            avatarImage.rectTransform, new Vector2(-126f, 0f),
            new Vector2(84f, 84f));

        var trophyImage = EnsureImage(
            playerChip.transform, "DailyTrophyIcon");
        ConfigureImage(trophyImage, trophy, true, Image.Type.Simple);
        Place(
            trophyImage.rectTransform, new Vector2(-18f, -28f),
            new Vector2(42f, 42f));

        chipName = EnsureText(
            playerChip.transform, "DailyPlayerName", 29f, productionFont,
            NearWhite, TextAlignmentOptions.Center);
        Place(
            chipName.rectTransform, new Vector2(48f, 23f),
            new Vector2(205f, 40f));
        chipName.enableAutoSizing = true;
        chipName.fontSizeMin = 22f;
        chipName.fontSizeMax = 30f;
        chipName.overflowMode = TextOverflowModes.Ellipsis;

        chipWins = EnsureText(
            playerChip.transform, "DailyPlayerWins", 28f, productionFont,
            Gold, TextAlignmentOptions.Center);
        Place(
            chipWins.rectTransform, new Vector2(65f, -28f),
            new Vector2(125f, 40f));
    }

    void RefreshCopy()
    {
        if (!IsReady) return;

        if (challengeHeading != null)
            challengeHeading.text = L10n.Get("home_daily_title");
        if (rewardHeading != null)
            rewardHeading.text = L10n.Get("stats_streak").ToUpperInvariant();
        RefreshVisibleTrail();
        RefreshPlayerChip();
    }

    void RefreshVisibleTrail()
    {
        if (trailText == null || string.IsNullOrEmpty(trailText.text)) return;

        string safe = trailText.text
            .Replace("🎯", "●")
            .Replace("🔺", "▲")
            .Replace("🔻", "▼");
        if (safe != trailText.text)
            trailText.text = safe;
    }

    void RefreshPlayerChip()
    {
        if (chipName == null || chipWins == null) return;

        string player = PlayerPrefs.GetString("PlayerName", "");
        if (string.IsNullOrWhiteSpace(player))
            player = L10n.Get("player_default");
        chipName.text = player;
        chipWins.text = GameStats.Wins.ToString();
    }

    void StyleInput(TMP_InputField input, Sprite frame)
    {
        if (input == null) return;

        var image = input.GetComponent<Image>();
        if (image == null)
            image = input.gameObject.AddComponent<Image>();
        image.enabled = true;
        image.sprite = frame;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 2f;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = true;

        if (input.textComponent != null)
        {
            input.textComponent.font = productionFont;
            input.textComponent.fontSize = 36f;
            input.textComponent.fontStyle = FontStyles.Bold;
            input.textComponent.color = NearWhite;
            input.textComponent.alignment = TextAlignmentOptions.Center;
        }

        TMP_Text placeholder = input.placeholder as TMP_Text;
        if (placeholder != null)
        {
            placeholder.font = productionFont;
            placeholder.fontSize = 27f;
            placeholder.color = Muted;
            placeholder.alignment = TextAlignmentOptions.Center;
        }
    }

    void StyleButton(
        Button button,
        Sprite sprite,
        Color labelColor,
        float labelSize)
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

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.gameObject.SetActive(true);
            label.font = productionFont;
            label.color = labelColor;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            if (labelSize > 0f)
            {
                label.enableAutoSizing = true;
                label.fontSizeMin = Mathf.Max(23f, labelSize - 9f);
                label.fontSizeMax = labelSize;
                label.overflowMode = TextOverflowModes.Overflow;
            }
        }
    }

    static void HideLegacyPresentation(Transform panel)
    {
        foreach (string name in new[]
        {
            "ExactDailyLogo",
            "ExactDailySeven",
            "ExactDailyThree",
            "DailyHuntBackdrop",
            "Card",
        })
        {
            Transform legacy = Find<Transform>(panel, name);
            if (legacy != null && !IsDescendantOf(legacy, panel.Find(VisualRootName)))
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
            Debug.LogError("[DailyHuntVisuals] Missing Resources/" + resource + ".");
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
}
