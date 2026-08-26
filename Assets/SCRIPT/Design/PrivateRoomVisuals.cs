using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Sole presentation owner for the Private Room landing screen.
//
// PvpRuntimeUI/PvpGameController remain responsible for networking, room state,
// navigation and callbacks. This component seats those real controls inside the
// approved modular cartoon composition; it never replaces the controller flow
// with a screenshot or disconnected visual clone.
[DefaultExecutionOrder(2600)]
public sealed class PrivateRoomVisuals : MonoBehaviour
{
    public const string VisualRootName = "PrivateRoomVisualRoot";
    public const string SafeRootName = "PrivateRoomSafeRoot";

    const string BackgroundResource = CartoonUiKit.Background;
    const string LogoResource = CartoonUiKit.Logo;
    const string CreateCardResource = CartoonUiKit.PrivateCreateCard;
    const string JoinCardResource = CartoonUiKit.PrivateJoinCard;
    const string CreateIconResource = CartoonUiKit.PrivateAddPlayer;
    const string ShareIconResource = CartoonUiKit.PrivateShare;
    const string MascotSixResource = CartoonUiKit.MascotSix;
    const string MascotSevenResource = CartoonUiKit.MascotSeven;
    const string AvatarResource = CartoonUiKit.PlayerAvatar;
    const string ConfettiResource = "mainmenu/mainmenu_deco_confetti";
    const string StarsResource = "mainmenu/mainmenu_deco_stars";
    const string OuterFrameResource = CartoonUiKit.ScreenFrame;
    const string TitleRibbonResource = CartoonUiKit.TitleRibbon;

    const string CyanActionResource = CartoonUiKit.CyanAction;
    const string GoldFrameResource = CartoonUiKit.GoldAction;
    const string PurpleActionResource = CartoonUiKit.PurpleAction;
    const string PurpleTrackResource = CartoonUiKit.PurpleTrack;
    const string PlayerChipResource = CartoonUiKit.PlayerChip;
    const string TipIconResource = CartoonUiKit.PrivateTipBulb;
    const string StreakIconResource = "mainmenu/mainmenu_icon_streak";

    const string DisplayFontResource = CartoonUiKit.DisplayFont;
    const string BodyFontResource = CartoonUiKit.BodyFont;

    const float ReferenceWidth = 1080f;
    const float ReferenceHeight = 1920f;

    static readonly Color NearWhite = new Color(0.985f, 0.975f, 1f, 1f);
    static readonly Color CyanText = new Color(0.20f, 0.92f, 1f, 1f);
    static readonly Color GoldText = new Color(1f, 0.82f, 0.22f, 1f);
    static readonly Color DarkInk = new Color(0.08f, 0.04f, 0.17f, 1f);
    static readonly Color MutedWhite = new Color(0.90f, 0.86f, 0.97f, 0.86f);

    PvpGameController pvp;
    RectTransform visualRoot;
    RectTransform safeRoot;
    RectTransform outerFrameRect;
    RectTransform backRect;
    RectTransform chipRect;
    RectTransform logoRect;
    RectTransform titleRibbonRect;
    RectTransform createCardRect;
    RectTransform joinCardRect;
    RectTransform shareRect;
    RectTransform tipCardRect;
    RectTransform mascotSixRect;
    RectTransform mascotSevenRect;
    TMP_FontAsset displayFont;
    TMP_FontAsset bodyFont;
    TMP_InputField landingCodeInput;
    TMP_Text playerNameText;
    TMP_Text streakText;
    TMP_Text stepText;
    TMP_Text joinHeading;
    Button createButton;
    Button joinButton;
    Button backButton;
    Button shareButton;
    bool built;
    float nextRefresh;
    int lastLayoutWidth = -1;
    int lastLayoutHeight = -1;

    public bool IsReady { get; private set; }

    IEnumerator Start()
    {
        for (int frame = 0; frame < 240 && !built; frame++)
        {
            pvp = GetComponent<PvpGameController>();
            if (pvp != null &&
                pvp.pvpMenuPanel != null &&
                pvp.createPanel != null &&
                pvp.joinPanel != null &&
                pvp.joinCodeInput != null)
            {
                Build();
                break;
            }

            yield return null;
        }

        if (!built)
            Debug.LogError(
                "[PrivateRoomVisuals] PvP controls were not ready within 240 frames.");
    }

    void OnEnable()
    {
        L10n.OnLanguageChanged += RefreshCopy;
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= RefreshCopy;
    }

    void LateUpdate()
    {
        if (!built || pvp == null || pvp.pvpMenuPanel == null) return;
        if (!pvp.pvpMenuPanel.activeInHierarchy) return;
        ApplyResponsiveLayout();
        if (Time.unscaledTime < nextRefresh) return;

        nextRefresh = Time.unscaledTime + 0.25f;
        RefreshPlayerChip();
    }

    void Build()
    {
        if (built || pvp == null || pvp.pvpMenuPanel == null) return;
        built = true;

        displayFont = Resources.Load<TMP_FontAsset>(DisplayFontResource);
        bodyFont = Resources.Load<TMP_FontAsset>(BodyFontResource);

        Sprite background = LoadRequired(BackgroundResource);
        Sprite logo = LoadRequired(LogoResource);
        Sprite createCard = LoadRequired(CreateCardResource);
        Sprite joinCard = LoadRequired(JoinCardResource);
        Sprite createIcon = LoadRequired(CreateIconResource);
        Sprite shareIcon = LoadRequired(ShareIconResource);
        Sprite six = LoadRequired(MascotSixResource);
        Sprite seven = LoadRequired(MascotSevenResource);
        Sprite avatar = LoadRequired(AvatarResource);
        Sprite cyanAction = LoadRequired(CyanActionResource);
        Sprite gold = LoadRequired(GoldFrameResource);
        Sprite purpleAction = LoadRequired(PurpleActionResource);
        Sprite purpleTrack = LoadRequired(PurpleTrackResource);
        Sprite tipPanel = LoadRequired(CartoonUiKit.PurplePanel);
        Sprite chip = LoadRequired(PlayerChipResource);
        Sprite tip = LoadRequired(TipIconResource);
        Sprite streakIcon = LoadRequired(StreakIconResource);
        Sprite confetti = LoadRequired(ConfettiResource);
        Sprite stars = LoadRequired(StarsResource);
        Sprite outerFrame = LoadRequired(OuterFrameResource);
        Sprite titleRibbonSprite = LoadRequired(TitleRibbonResource);

        IsReady = ArtReady(
            background, logo, createCard, joinCard, createIcon, shareIcon, six,
            seven, avatar, cyanAction, gold, purpleAction, purpleTrack,
            tipPanel, chip, tip,
            streakIcon, confetti, stars, outerFrame, titleRibbonSprite) &&
            displayFont != null && bodyFont != null;

        if (!IsReady)
        {
            Debug.LogError(
                "[PrivateRoomVisuals] Required production artwork/fonts are missing.");
            return;
        }

        Transform panel = pvp.pvpMenuPanel.transform;
        var panelImage = pvp.pvpMenuPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.enabled = false;
            panelImage.raycastTarget = false;
        }

        createButton = FindButton(panel, "CreateButton");
        joinButton = FindButton(panel, "JoinButton");
        backButton = FindBackButton(panel, createButton, joinButton);
        if (createButton == null || joinButton == null || backButton == null)
        {
            Debug.LogError(
                "[PrivateRoomVisuals] Create/Join/Back controls are missing.");
            IsReady = false;
            return;
        }

        // Preserve only the callback-bearing roots. Their old presentation
        // children would otherwise be duplicated underneath the approved cards.
        ClearButtonPresentation(createButton.transform);
        ClearButtonPresentation(joinButton.transform);
        ClearButtonPresentation(backButton.transform);
        HideLegacyPresentation(
            panel, createButton.transform, joinButton.transform,
            backButton.transform);

        visualRoot = EnsureRect(panel, VisualRootName);
        Stretch(visualRoot);
        visualRoot.SetAsFirstSibling();

        var backgroundImage = EnsureImage(visualRoot, "PrivateRoomBackground");
        Stretch(backgroundImage.rectTransform);
        ConfigureImage(
            backgroundImage, background, false, Image.Type.Simple);

        var starsImage = EnsureImage(visualRoot, "PrivateRoomStars");
        Stretch(starsImage.rectTransform);
        ConfigureImage(starsImage, stars, false, Image.Type.Simple);

        var confettiImage = EnsureImage(visualRoot, "PrivateRoomConfetti");
        Stretch(confettiImage.rectTransform);
        ConfigureImage(confettiImage, confetti, false, Image.Type.Simple);

        // Decorative full-screen layers use the approved 9:16 art envelope.
        // This is owned here so no second runtime component rewrites geometry
        // after the Private Room presentation has settled.
        ApplyPortraitEnvelope(backgroundImage.rectTransform);
        ApplyPortraitEnvelope(starsImage.rectTransform);
        ApplyPortraitEnvelope(confettiImage.rectTransform);

        // The outer frame is decorative and never intercepts input.
        var outer = EnsureImage(visualRoot, "PrivateRoomOuterFrame");
        ConfigureImage(outer, outerFrame, false, Image.Type.Simple);
        outerFrameRect = outer.rectTransform;
        Place(outerFrameRect, Vector2.zero, new Vector2(1056f, 1888f));
        outer.raycastTarget = false;

        safeRoot = EnsureRect(visualRoot, SafeRootName);
        Stretch(safeRoot);
        var canvas = pvp.pvpMenuPanel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            ResponsiveSafeAreaRoot.Attach(
                safeRoot, canvas.transform as RectTransform,
                new Vector2(ReferenceWidth, ReferenceHeight));
        }

        BuildTopBar(chip, avatar, purpleTrack, streakIcon);

        var logoImage = EnsureImage(safeRoot, "PrivateRoomLogo");
        ConfigureImage(logoImage, logo, true, Image.Type.Simple);
        logoRect = logoImage.rectTransform;
        Place(
            logoRect, new Vector2(-44f, 738f),
            new Vector2(442f, 318f));

        var titleRibbon = EnsureImage(safeRoot, "PrivateRoomTitleRibbon");
        ConfigureImage(
            titleRibbon, titleRibbonSprite, false, Image.Type.Simple);
        titleRibbonRect = titleRibbon.rectTransform;
        Place(
            titleRibbonRect, new Vector2(0f, 499f),
            new Vector2(936f, 156f));

        var title = EnsureText(
            titleRibbon.transform, "PrivateRoomTitle", 58f, displayFont,
            NearWhite, TextAlignmentOptions.Center);
        StretchText(title.rectTransform, 70f, 24f);
        ConfigureDisplayText(title, 44f, 60f);
        title.fontStyle |= FontStyles.UpperCase;
        SetLocalized(title, "private_room_title");

        BuildCreateCard(createCard, cyanAction, createIcon);
        BuildJoinCard(joinCard, gold, purpleTrack);
        BuildShareAndTip(
            purpleAction, tipPanel, shareIcon, tip, six, seven);

        RefreshCopy();
        RefreshPlayerChip();
        ApplyResponsiveLayout(true);
    }

    void BuildTopBar(
        Sprite chipSprite,
        Sprite avatar,
        Sprite pillSprite,
        Sprite streakIcon)
    {
        Reparent(backButton.transform, safeRoot);
        backRect = (RectTransform)backButton.transform;
        Place(
            backRect, new Vector2(-328f, 852f),
            new Vector2(300f, 72f));
        StyleButton(
            backButton, pillSprite, NearWhite, 1f, Image.Type.Simple);
        HideButtonLabels(backButton.transform);

        stepText = EnsureText(
            backButton.transform, "PrivateRoomStepText", 30f, displayFont,
            NearWhite, TextAlignmentOptions.Center);
        StretchText(stepText.rectTransform, 20f, 10f);
        ConfigureDisplayText(stepText, 21f, 25f);
        SetLocalized(stepText, "private_room_step");

        var chip = EnsureImage(safeRoot, "PrivateRoomPlayerChip");
        ConfigureImage(chip, chipSprite, false, Image.Type.Simple);
        chipRect = chip.rectTransform;
        Place(
            chipRect, new Vector2(338f, 826f),
            new Vector2(300f, 134f));

        var avatarImage = EnsureImage(
            chip.transform, "PrivateRoomPlayerAvatar");
        ConfigureImage(avatarImage, avatar, true, Image.Type.Simple);
        Place(
            avatarImage.rectTransform, new Vector2(-104f, 0f),
            new Vector2(92f, 92f));

        playerNameText = EnsureText(
            chip.transform, "PrivateRoomPlayerName", 31f, bodyFont,
            NearWhite, TextAlignmentOptions.Center);
        Place(
            playerNameText.rectTransform, new Vector2(42f, 27f),
            new Vector2(184f, 46f));
        playerNameText.fontStyle = FontStyles.Bold;
        playerNameText.enableAutoSizing = true;
        playerNameText.fontSizeMin = 25f;
        playerNameText.fontSizeMax = 33f;
        playerNameText.overflowMode = TextOverflowModes.Ellipsis;

        streakText = EnsureText(
            chip.transform, "PrivateRoomStreak", 30f, bodyFont, GoldText,
            TextAlignmentOptions.Center);
        Place(
            streakText.rectTransform, new Vector2(60f, -30f),
            new Vector2(112f, 44f));

        var streakImage = EnsureImage(
            chip.transform, "PrivateRoomStreakIcon");
        ConfigureImage(streakImage, streakIcon, true, Image.Type.Simple);
        Place(
            streakImage.rectTransform, new Vector2(-24f, -30f),
            new Vector2(46f, 46f));
    }

    void BuildCreateCard(
        Sprite frame,
        Sprite actionFrame,
        Sprite createIcon)
    {
        var card = EnsureImage(safeRoot, "PrivateRoomCreateCard");
        ConfigureImage(card, frame, false, Image.Type.Simple);
        createCardRect = card.rectTransform;
        Place(
            createCardRect, new Vector2(0f, 169f),
            new Vector2(970f, 480f));

        var createBadge = EnsureImage(
            card.transform, "PrivateRoomCreateIcon");
        ConfigureImage(createBadge, createIcon, true, Image.Type.Simple);
        Place(
            createBadge.rectTransform, new Vector2(300f, 126f),
            new Vector2(104f, 104f));

        var heading = EnsureText(
            card.transform, "PrivateRoomCreateHeading", 46f, displayFont,
            NearWhite, TextAlignmentOptions.Center);
        Place(
            heading.rectTransform, new Vector2(270f, 52f),
            new Vector2(420f, 106f));
        ConfigureDisplayText(heading, 36f, 49f);
        SetLocalized(heading, "private_room_create_title");

        var hint = EnsureText(
            card.transform, "PrivateRoomCreateHint", 28f, bodyFont,
            CyanText, TextAlignmentOptions.Center);
        Place(
            hint.rectTransform, new Vector2(270f, -40f),
            new Vector2(420f, 82f));
        ConfigureBodyText(hint, 24f, 31f);
        SetLocalized(hint, "private_room_create_hint");

        Reparent(createButton.transform, card.transform);
        Place(
            (RectTransform)createButton.transform,
            new Vector2(270f, -148f), new Vector2(360f, 96f));
        StyleButton(
            createButton, actionFrame, NearWhite, 1f, Image.Type.Simple);
        ConfigureButtonLabel(
            createButton, "private_room_create_action", 38f, NearWhite);
    }

    void BuildJoinCard(
        Sprite magentaFrame,
        Sprite goldFrame,
        Sprite inputFrame)
    {
        var card = EnsureImage(safeRoot, "PrivateRoomJoinCard");
        ConfigureImage(card, magentaFrame, false, Image.Type.Simple);
        joinCardRect = card.rectTransform;
        Place(
            joinCardRect, new Vector2(0f, -321f),
            new Vector2(970f, 448f));

        joinHeading = EnsureText(
            card.transform, "PrivateRoomJoinHeading", 43f, displayFont,
            NearWhite, TextAlignmentOptions.Center);
        Place(
            joinHeading.rectTransform, new Vector2(220f, 126f),
            new Vector2(520f, 92f));
        ConfigureDisplayText(joinHeading, 23f, 43f);
        joinHeading.overflowMode = TextOverflowModes.Truncate;
        joinHeading.fontStyle |= FontStyles.UpperCase;
        SetLocalized(joinHeading, "private_room_join_title");

        var codeCaption = EnsureText(
            card.transform, "PrivateRoomCodeCaption", 24f, bodyFont,
            MutedWhite, TextAlignmentOptions.Center);
        Place(
            codeCaption.rectTransform, new Vector2(250f, 52f),
            new Vector2(450f, 42f));
        ConfigureBodyText(codeCaption, 22f, 27f);
        SetLocalized(codeCaption, "pvp_enter_code");

        landingCodeInput = RuntimeUI.CreateInputField(
            card.transform, "PrivateRoomLandingCodeInput",
            L10n.Get("pvp_enter_code"), new Vector2(250f, -12f),
            new Vector2(460f, 88f), 5, TMP_InputField.ContentType.Standard);
        landingCodeInput.onValidateInput = ValidateRoomCodeCharacter;
        landingCodeInput.onValueChanged.AddListener(NormalizeLandingCode);
        landingCodeInput.shouldHideMobileInput = true;

        var inputImage = landingCodeInput.GetComponent<Image>();
        if (inputImage != null)
        {
            inputImage.sprite = inputFrame;
            inputImage.type = Image.Type.Simple;
            inputImage.color = Color.white;
            inputImage.pixelsPerUnitMultiplier = 1f;
            inputImage.raycastTarget = true;
        }

        if (landingCodeInput.textComponent != null)
        {
            landingCodeInput.textComponent.font = displayFont;
            landingCodeInput.textComponent.fontSize = 42f;
            landingCodeInput.textComponent.fontStyle = FontStyles.Bold;
            landingCodeInput.textComponent.color = NearWhite;
            landingCodeInput.textComponent.alignment =
                TextAlignmentOptions.Center;
            landingCodeInput.textComponent.characterSpacing = 4f;
        }

        var placeholder = landingCodeInput.placeholder as TMP_Text;
        if (placeholder != null)
        {
            placeholder.font = bodyFont;
            placeholder.fontSize = 25f;
            placeholder.color = MutedWhite;
            placeholder.alignment = TextAlignmentOptions.Center;
        }
        RuntimeUI.LocalizePlaceholder(landingCodeInput, "pvp_enter_code");

        Reparent(joinButton.transform, card.transform);
        Place(
            (RectTransform)joinButton.transform,
            new Vector2(250f, -142f), new Vector2(460f, 98f));
        StyleButton(
            joinButton, goldFrame, DarkInk, 1f, Image.Type.Simple);
        ConfigureButtonLabel(
            joinButton, "private_room_join_action", 42f, DarkInk);
        joinButton.onClick.AddListener(CopyLandingCodeIntoJoinFlow);
    }

    void BuildShareAndTip(
        Sprite shareFrame,
        Sprite tipFrame,
        Sprite shareIcon,
        Sprite tipIcon,
        Sprite six,
        Sprite seven)
    {
        var shareGo = RuntimeUI.CreateObject(
            "PrivateRoomShareButton", safeRoot);
        shareRect = (RectTransform)shareGo.transform;
        Place(
            shareRect, new Vector2(0f, -608f),
            new Vector2(390f, 90f));

        var shareImage = shareGo.AddComponent<Image>();
        ConfigureImage(
            shareImage, shareFrame, false, Image.Type.Simple);
        shareImage.raycastTarget = true;

        shareButton = shareGo.AddComponent<Button>();
        shareButton.targetGraphic = shareImage;
        ConfigureButtonState(shareButton);
        RuntimeUI.AttachJuice(shareButton);

        var icon = EnsureImage(
            shareGo.transform, "PrivateRoomShareIcon");
        ConfigureImage(icon, shareIcon, true, Image.Type.Simple);
        Place(
            icon.rectTransform, new Vector2(-138f, 0f),
            new Vector2(68f, 68f));

        var shareLabel = EnsureText(
            shareGo.transform, "PrivateRoomShareLabel", 36f, displayFont,
            NearWhite, TextAlignmentOptions.Center);
        Place(
            shareLabel.rectTransform, new Vector2(38f, 0f),
            new Vector2(315f, 62f));
        ConfigureDisplayText(shareLabel, 28f, 37f);
        SetLocalized(shareLabel, "private_room_share");
        shareButton.onClick.AddListener(ShareAvailableCode);

        var tipCard = EnsureImage(safeRoot, "PrivateRoomTipCard");
        ConfigureImage(tipCard, tipFrame, false, Image.Type.Simple);
        tipCardRect = tipCard.rectTransform;
        Place(
            tipCardRect, new Vector2(0f, -792f),
            new Vector2(460f, 206f));

        var bulb = EnsureImage(
            tipCard.transform, "PrivateRoomTipIcon");
        ConfigureImage(bulb, tipIcon, true, Image.Type.Simple);
        Place(
            bulb.rectTransform, new Vector2(-178f, 0f),
            new Vector2(86f, 86f));

        var tipText = EnsureText(
            tipCard.transform, "PrivateRoomTipText", 28f, bodyFont,
            NearWhite, TextAlignmentOptions.Left);
        Place(
            tipText.rectTransform, new Vector2(48f, 0f),
            new Vector2(340f, 154f));
        ConfigureBodyText(tipText, 21f, 26f);
        SetLocalized(tipText, "private_room_tip");

        var sixImage = EnsureImage(
            safeRoot, "PrivateRoomMascotSix");
        ConfigureImage(sixImage, six, true, Image.Type.Simple);
        mascotSixRect = sixImage.rectTransform;
        Place(
            mascotSixRect, new Vector2(-382f, -768f),
            new Vector2(270f, 344f));

        var sevenImage = EnsureImage(
            safeRoot, "PrivateRoomMascotSeven");
        ConfigureImage(sevenImage, seven, true, Image.Type.Simple);
        mascotSevenRect = sevenImage.rectTransform;
        Place(
            mascotSevenRect, new Vector2(382f, -768f),
            new Vector2(296f, 360f));
    }

    void ApplyResponsiveLayout(bool force = false)
    {
        if (outerFrameRect == null || backRect == null || chipRect == null ||
            logoRect == null || titleRibbonRect == null ||
            createCardRect == null || joinCardRect == null ||
            shareRect == null || tipCardRect == null ||
            mascotSixRect == null || mascotSevenRect == null)
            return;

        int width = Mathf.Max(1, Screen.width);
        int height = Mathf.Max(1, Screen.height);
        if (!force && width == lastLayoutWidth && height == lastLayoutHeight)
            return;

        lastLayoutWidth = width;
        lastLayoutHeight = height;
        float aspect = height / (float)width;
        float tall = Mathf.InverseLerp(1.78f, 2.22f, aspect);

        Place(outerFrameRect, Vector2.zero,
            new Vector2(1056f, 1888f + 480f * tall));
        Place(backRect, new Vector2(-328f, 852f + 185f * tall),
            new Vector2(300f, 72f));
        Place(chipRect, new Vector2(338f, 826f + 185f * tall),
            new Vector2(300f, 134f));
        Place(logoRect, new Vector2(-44f, 738f + 150f * tall),
            new Vector2(442f, 318f));
        Place(titleRibbonRect, new Vector2(0f, 499f + 100f * tall),
            new Vector2(936f, 156f));
        Place(createCardRect, new Vector2(0f, 169f + 40f * tall),
            new Vector2(970f, 480f));
        Place(joinCardRect, new Vector2(0f, -321f - 30f * tall),
            new Vector2(970f, 448f));
        Place(shareRect, new Vector2(0f, -608f - 90f * tall),
            new Vector2(390f, 90f));
        Place(tipCardRect, new Vector2(0f, -792f - 170f * tall),
            new Vector2(460f, 206f));
        Place(mascotSixRect, new Vector2(-382f, -768f - 180f * tall),
            new Vector2(270f, 344f));
        Place(mascotSevenRect, new Vector2(382f, -768f - 180f * tall),
            new Vector2(296f, 360f));
    }

    static char ValidateRoomCodeCharacter(
        string currentText,
        int characterIndex,
        char proposed)
    {
        char normalized = char.ToUpperInvariant(proposed);
        return char.IsLetterOrDigit(normalized) ? normalized : '\0';
    }

    void NormalizeLandingCode(string value)
    {
        if (landingCodeInput == null) return;

        string normalized = (value ?? string.Empty)
            .Trim()
            .ToUpperInvariant();

        if (normalized.Length > 5)
            normalized = normalized.Substring(0, 5);

        if (landingCodeInput.text != normalized)
            landingCodeInput.SetTextWithoutNotify(normalized);
    }

    void CopyLandingCodeIntoJoinFlow()
    {
        if (landingCodeInput == null ||
            pvp == null ||
            pvp.joinCodeInput == null)
            return;

        string normalized = (landingCodeInput.text ?? string.Empty)
            .Trim()
            .ToUpperInvariant();

        pvp.joinCodeInput.SetTextWithoutNotify(normalized);
    }

    void ShareAvailableCode()
    {
        string code = string.Empty;
        if (pvp != null && pvp.roomCodeText != null)
        {
            string room = (pvp.roomCodeText.text ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(room) && room != "-----")
                code = room;
        }

        if (string.IsNullOrEmpty(code) && landingCodeInput != null)
        {
            code = (landingCodeInput.text ?? string.Empty)
                .Trim()
                .ToUpperInvariant();
        }

        if (string.IsNullOrEmpty(code)) return;

        GUIUtility.systemCopyBuffer = code;
        if (pvp != null &&
            pvp.roomCodeText != null &&
            pvp.roomCodeText.text == code)
        {
            pvp.OnCopyInvitePressed();
        }
    }

    void RefreshCopy()
    {
        if (!built) return;

        if (stepText != null)
            stepText.text = L10n.Get("private_room_step");
        if (joinHeading != null)
        {
            joinHeading.text = L10n.Get("private_room_join_title");
            joinHeading.fontSizeMin = 23f;
            joinHeading.fontSizeMax =
                L10n.Current == L10n.Language.Greek ? 34f : 43f;
            joinHeading.ForceMeshUpdate();
        }

        RefreshPlayerChip();
    }

    void RefreshPlayerChip()
    {
        if (playerNameText == null || streakText == null) return;

        string player = PlayerPrefs.GetString("PlayerName", "");
        if (string.IsNullOrWhiteSpace(player))
            player = L10n.Get("player_default");

        playerNameText.text = player;
        streakText.text = GameStats.CurrentStreak.ToString();
    }

    void ConfigureButtonLabel(
        Button button,
        string key,
        float size,
        Color color)
    {
        if (button == null) return;

        var labelTransform = DirectChild(
            button.transform, "PrivateRoomActionLabel");
        var label = labelTransform == null
            ? null
            : labelTransform.GetComponent<TMP_Text>();

        if (label == null)
        {
            label = EnsureText(
                button.transform, "PrivateRoomActionLabel", size,
                displayFont, color, TextAlignmentOptions.Center);
        }

        label.gameObject.SetActive(true);
        label.font = displayFont;
        label.fontSize = size;
        label.fontStyle = FontStyles.Bold;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = Mathf.Max(28f, size - 10f);
        label.fontSizeMax = size;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        StretchText(label.rectTransform, 24f, 14f);
        SetLocalized(label, key);
    }

    static void ConfigureDisplayText(
        TMP_Text text,
        float minSize,
        float maxSize)
    {
        if (text == null) return;

        text.fontStyle = FontStyles.Bold;
        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        AddTextShadow(text);
    }

    static void ConfigureBodyText(
        TMP_Text text,
        float minSize,
        float maxSize)
    {
        if (text == null) return;

        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
    }

    static void AddTextShadow(TMP_Text text)
    {
        if (text == null) return;

        var shadow = text.GetComponent<Shadow>();
        if (shadow == null)
            shadow = text.gameObject.AddComponent<Shadow>();

        shadow.effectColor = new Color(0.02f, 0.01f, 0.12f, 0.68f);
        shadow.effectDistance = new Vector2(2f, -3f);
        shadow.useGraphicAlpha = true;
    }

    static void StyleButton(
        Button button,
        Sprite sprite,
        Color labelColor,
        float pixelsPerUnit,
        Image.Type type)
    {
        if (button == null) return;

        var image = button.GetComponent<Image>();
        if (image == null)
            image = button.gameObject.AddComponent<Image>();

        image.enabled = true;
        image.sprite = sprite;
        image.type = type;
        image.color = Color.white;
        image.preserveAspect = false;
        image.pixelsPerUnitMultiplier = pixelsPerUnit;
        image.raycastTarget = true;
        button.targetGraphic = image;

        ConfigureButtonState(button);

        foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
            text.color = labelColor;
    }

    static void ConfigureButtonState(Button button)
    {
        if (button == null) return;

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.82f, 0.86f, 0.96f, 1f);
        colors.disabledColor = new Color(0.56f, 0.57f, 0.65f, 0.72f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.06f;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = colors;
    }

    static void HideLegacyPresentation(
        Transform panel,
        params Transform[] keep)
    {
        for (int i = panel.childCount - 1; i >= 0; i--)
        {
            Transform child = panel.GetChild(i);
            bool preserve = false;
            for (int k = 0; k < keep.Length; k++)
            {
                if (child == keep[k])
                {
                    preserve = true;
                    break;
                }
            }

            if (preserve || child.name == VisualRootName) continue;
            child.gameObject.SetActive(false);
        }
    }

    static Button FindButton(Transform root, string name)
    {
        var target = DeepFind(root, name);
        return target == null ? null : target.GetComponent<Button>();
    }

    static Button FindBackButton(
        Transform root,
        Button create,
        Button join)
    {
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            if (button != create && button != join)
                return button;
        }

        return null;
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
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            text.gameObject.SetActive(false);

        foreach (var text in root.GetComponentsInChildren<Text>(true))
            text.gameObject.SetActive(false);
    }

    static void SetLocalized(TMP_Text text, string key)
    {
        if (text == null) return;

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
        {
            if (sprite == null)
                return false;
        }

        return true;
    }

    static Sprite LoadRequired(string resource)
    {
        var sprite = Resources.Load<Sprite>(resource);
        if (sprite == null)
        {
            Debug.LogError(
                "[PrivateRoomVisuals] Missing Resources/" + resource + ".");
        }

        return sprite;
    }

    static RectTransform EnsureRect(Transform parent, string name)
    {
        var existing = DirectChild(parent, name) as RectTransform;
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            return existing;
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
        text.enableAutoSizing = false;
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
        if (rect == null) return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    static void ApplyPortraitEnvelope(RectTransform rect)
    {
        if (rect == null) return;

        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;

        var fitter = rect.GetComponent<AspectRatioFitter>();
        if (fitter == null)
            fitter = rect.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = ReferenceWidth / ReferenceHeight;
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
            if (child.name == name)
                return child;
        }

        return null;
    }

    static Transform DeepFind(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = DeepFind(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }
}
