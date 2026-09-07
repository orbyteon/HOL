using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Sole presentation owner for the Daily Challenge dashboard and its real
// number-hunt gameplay state. Domain state/callbacks stay outside this class;
// this owner alone chooses sprites and writes Daily Hunt layout.
[DisallowMultipleComponent]
public sealed class DailyHuntVisuals : MonoBehaviour
{
    public sealed class ViewBindings
    {
        public readonly TMP_Text Title;
        public readonly TMP_Text Status;
        public readonly TMP_Text Trail;
        public readonly TMP_Text Streak;
        public readonly TMP_InputField Input;
        public readonly Button GuessButton;
        public readonly Button ReviveButton;
        public readonly Button ShareButton;
        public readonly Button CloseButton;
        public readonly Button StartButton;

        public ViewBindings(
            TMP_Text title,
            TMP_Text status,
            TMP_Text trail,
            TMP_Text streak,
            TMP_InputField input,
            Button guessButton,
            Button reviveButton,
            Button shareButton,
            Button closeButton,
            Button startButton)
        {
            Title = title;
            Status = status;
            Trail = trail;
            Streak = streak;
            Input = input;
            GuessButton = guessButton;
            ReviveButton = reviveButton;
            ShareButton = shareButton;
            CloseButton = closeButton;
            StartButton = startButton;
        }
    }

    public const string VisualRootName = "DailyHuntVisualRoot";
    public const string SafeRootName = "DailyHuntSafeRoot";

    const string BackgroundResource = "phase2a/hol_neon_reference_bg_r3";
    const string LogoResource = "reference/hol_logo_exact";
    const string MascotSixResource = "reference/mascot_6_exact";
    const string MascotSevenResource = "reference/mascot_7_exact";
    const string CalendarResource = "dailyhunt/production/daily_calendar_target_production";
    const string StarsResource = "mainmenu/mainmenu_deco_stars";
    const string ConfettiResource = "mainmenu/mainmenu_deco_confetti";
    const string OuterBezelBodyResource = P0Root + "daily_outer_frame_v1";

    // Daily-Hunt-only production components derived from the locked
    // 06-daily-hunt-approved.png composition. They are intentionally owned
    // here rather than exposed as a second shared/global visual authority.
    const string P0Root = "dailyhunt/v1/";
    const string BackButtonResource = P0Root + "daily_back_button_v1";
    const string PlayerChipResource =
        "dailyhunt/production/daily_player_chip_shell_v3";
    const string PlayerAvatarRingResource =
        "dailyhunt/production/daily_player_avatar_ring_v1";
    const string PlayerXpTrackResource =
        "dailyhunt/production/daily_player_xp_track_v2";
    const string TitleRibbonResource = P0Root + "daily_title_ribbon_v1";
    const string ChallengeBoardResource = P0Root + "daily_challenge_board_v1";
    const string InfoPanelResource = P0Root + "daily_info_panel_v1";
    const string InputShellResource = P0Root + "daily_input_shell_v1";
    const string BaseAttemptResource = P0Root + "daily_attempt_base_v1";
    const string BonusAttemptResource = P0Root + "daily_attempt_bonus_v1";
    const string GuessActionResource = P0Root + "daily_action_guess_v1";
    const string ShareActionResource = P0Root + "daily_action_share_v1";
    const string ReviveActionResource = P0Root + "daily_action_revive_v1";
    const string StreakBoardResource = P0Root + "daily_streak_board_v1";
    const string ProductionRoot = "dailyhunt/production/";
    const string MissionTrophyResource = ProductionRoot + "daily_mission_icon_trophy";
    const string MissionBrainResource = ProductionRoot + "daily_mission_icon_brain";
    const string MissionShareResource = ProductionRoot + "daily_mission_icon_share";
    const string MissionProgressTrackResource = ProductionRoot + "daily_mission_progress_track";
    const string MissionProgressCyanResource = ProductionRoot + "daily_mission_progress_cyan";
    const string MissionProgressMagentaResource = ProductionRoot + "daily_mission_progress_magenta";
    const string MissionCheckCyanResource = ProductionRoot + "daily_mission_check_cyan";
    const string MissionCheckMagentaResource = ProductionRoot + "daily_mission_check_magenta";
    const string MissionRewardBoardResource = ProductionRoot + "daily_mission_reward_board";
    const string MissionRewardChestResource =
        ProductionRoot + "daily_reward_chest_reference_v1";
    const string MissionClockResource = ProductionRoot + "daily_mission_clock";
    const string MissionPortalResource = ProductionRoot + "daily_floor_portal";
    const string PlayerStarResource = ProductionRoot + "daily_player_star";

    // Use the same approved display/body hierarchy as the established HOL
    // cartoon shell. Live EN/EL strings and numeric state remain real TMP.
    const string DisplayFontResource =
        "dailyhunt/production/fonts/HOL Daily Display SDF";
    const string BodyFontResource =
        "dailyhunt/production/fonts/HOL Daily Body SDF";

    const float ReferenceWidth = 1080f;
    const float ReferenceHeight = 1920f;

    static readonly Color NearWhite = new Color(0.985f, 0.975f, 1f, 1f);
    static readonly Color Cyan = new Color(0.20f, 0.94f, 1f, 1f);
    static readonly Color Magenta = new Color(1f, 0.20f, 0.64f, 1f);
    static readonly Color Gold = new Color(1f, 0.80f, 0.20f, 1f);
    static readonly Color Muted = new Color(0.88f, 0.84f, 0.96f, 0.90f);
    static readonly Color Ink = new Color(0.08f, 0.04f, 0.17f, 1f);

    public static readonly string[] LoadedResources =
    {
        BackgroundResource,
        LogoResource,
        PlayerProfileAvatarResolver.FallbackResourcePath,
        MascotSixResource,
        MascotSevenResource,
        CalendarResource,
        StarsResource,
        ConfettiResource,
        OuterBezelBodyResource,
        BackButtonResource,
        PlayerChipResource,
        PlayerAvatarRingResource,
        PlayerXpTrackResource,
        TitleRibbonResource,
        ChallengeBoardResource,
        InfoPanelResource,
        InputShellResource,
        BaseAttemptResource,
        BonusAttemptResource,
        GuessActionResource,
        ShareActionResource,
        ReviveActionResource,
        StreakBoardResource,
        MissionRewardBoardResource,
        MissionRewardChestResource,
        MissionTrophyResource,
        MissionBrainResource,
        MissionShareResource,
        MissionProgressTrackResource,
        MissionProgressCyanResource,
        MissionProgressMagentaResource,
        MissionCheckCyanResource,
        MissionCheckMagentaResource,
        MissionClockResource,
        MissionPortalResource,
        PlayerStarResource,
    };

    RectTransform visualRoot;
    RectTransform safeRoot;
    RectTransform huntRoot;
    RectTransform missionRoot;
    RectTransform closeRect;
    RectTransform chipRect;
    RectTransform logoRect;
    RectTransform ribbonRect;
    RectTransform challengeCardRect;
    RectTransform rewardCardRect;
    RectTransform submitRect;
    RectTransform reviveRect;
    RectTransform shareRect;
    RectTransform mascotSixRect;
    RectTransform mascotSevenRect;
    RectTransform missionBoardRect;
    RectTransform missionRewardRect;
    RectTransform missionStartRect;
    RectTransform missionPortalRect;
    RectTransform statusFrameRect;
    TMP_FontAsset displayFont;
    TMP_FontAsset bodyFont;
    TMP_Text chipName;
    TMP_Text chipWins;
    TMP_Text chipProgress;
    RectTransform playerAvatarAperture;
    Image playerAvatarImage;
    Image chipProgressFill;
    TMP_Text ribbonTitle;
    TMP_Text challengeTitle;
    TMP_Text statusCopy;
    TMP_Text rewardHeading;
    TMP_Text attemptHeading;
    TMP_Text inputCaption;
    TMP_Text streakValue;
    TMP_Text trailText;
    TMP_Text missionHeading;
    TMP_Text missionCompletion;
    TMP_Text missionRewardHeading;
    TMP_Text missionResetLabel;
    TMP_Text missionReset;
    TMP_Text missionRewardAmount;
    TMP_Text missionRewardStatus;
    readonly TMP_Text[] missionLabels = new TMP_Text[3];
    readonly TMP_Text[] missionProgress = new TMP_Text[3];
    readonly Image[] missionChecks = new Image[3];
    readonly Image[] missionFills = new Image[3];
    readonly Image[] attemptSlots = new Image[9];
    readonly TMP_Text[] attemptSlotLabels = new TMP_Text[9];
    Sprite baseAttemptSprite;
    Sprite bonusAttemptSprite;
    Sprite missionCheckCyanSprite;
    Sprite missionCheckMagentaSprite;
    bool lastRevivedLayout;
    bool lastInputVisible = true;
    float nextChipRefresh;
    float nextMissionRefresh;
    int lastLayoutWidth = -1;
    int lastLayoutHeight = -1;
    L10n.Language lastLayoutLanguage;
    bool missionDashboardVisible = true;
    ViewBindings viewBindings;
    Button missionStartButton;

#if DEVELOPMENT_BUILD
    int captureChipPoints = -1;
    int captureChipProgress = -1;
    string captureResetText;
#endif

    public bool IsReady { get; private set; }
    public TMP_FontAsset DisplayFont => displayFont;
    public TMP_FontAsset BodyFont => bodyFont;
    public TMP_FontAsset ProductionFont => bodyFont;

    public static ViewBindings Apply(Transform panel)
    {
        if (panel == null) return null;

        var owner = panel.GetComponent<DailyHuntVisuals>();
        if (owner == null)
            owner = panel.gameObject.AddComponent<DailyHuntVisuals>();
        owner.Build(panel);
        return owner.viewBindings;
    }

#if DEVELOPMENT_BUILD
    public void SetCapturePlayerChipFixture(int points, int milestoneProgress)
    {
        captureChipPoints = Mathf.Max(0, points);
        captureChipProgress = Mathf.Clamp(
            milestoneProgress, 0, DailyChallengeProgress.PointsMilestone);
        captureResetText = "12:45:09";
        RefreshPlayerChip();
        RefreshMissionState();
    }
#endif

    void OnEnable()
    {
        L10n.OnLanguageChanged += RefreshCopy;
        DailyChallengeProgress.Changed += RefreshMissionState;
        RefreshCopy();
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= RefreshCopy;
        DailyChallengeProgress.Changed -= RefreshMissionState;
    }

    void LateUpdate()
    {
        if (!IsReady) return;

        ApplyResponsiveLayout();
        if (missionDashboardVisible)
        {
            if (Time.unscaledTime >= nextMissionRefresh)
            {
                nextMissionRefresh = Time.unscaledTime + 0.25f;
                RefreshMissionState();
            }
        }
        else
        {
            RefreshInteractionPresentation();
            RefreshVisibleTrail();
            RefreshStreakValue();
        }
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

        displayFont = Resources.Load<TMP_FontAsset>(DisplayFontResource);
        bodyFont = Resources.Load<TMP_FontAsset>(BodyFontResource);

        Sprite background = LoadRequired(BackgroundResource);
        Sprite logo = LoadRequired(LogoResource);
        Sprite fallbackAvatar = LoadRequired(
            PlayerProfileAvatarResolver.FallbackResourcePath);
        Sprite six = LoadRequired(MascotSixResource);
        Sprite seven = LoadRequired(MascotSevenResource);
        Sprite rewardChest = LoadRequired(MissionRewardChestResource);
        Sprite calendar = LoadRequired(CalendarResource);
        Sprite stars = LoadRequired(StarsResource);
        Sprite confetti = LoadRequired(ConfettiResource);
        Sprite outerBezelBody = LoadRequired(OuterBezelBodyResource);
        Sprite backButton = LoadRequired(BackButtonResource);
        Sprite playerChip = LoadRequired(PlayerChipResource);
        Sprite playerAvatarRing = LoadRequired(PlayerAvatarRingResource);
        Sprite playerXpTrack = LoadRequired(PlayerXpTrackResource);
        Sprite titleRibbon = LoadRequired(TitleRibbonResource);
        Sprite challengeBoard = LoadRequired(ChallengeBoardResource);
        Sprite infoPanel = LoadRequired(InfoPanelResource);
        Sprite inputShell = LoadRequired(InputShellResource);
        baseAttemptSprite = LoadRequired(BaseAttemptResource);
        bonusAttemptSprite = LoadRequired(BonusAttemptResource);
        Sprite guessAction = LoadRequired(GuessActionResource);
        Sprite shareAction = LoadRequired(ShareActionResource);
        Sprite reviveAction = LoadRequired(ReviveActionResource);
        Sprite streakBoard = LoadRequired(StreakBoardResource);
        Sprite missionRewardBoard = LoadRequired(MissionRewardBoardResource);
        Sprite missionTrophy = LoadRequired(MissionTrophyResource);
        Sprite missionBrain = LoadRequired(MissionBrainResource);
        Sprite missionShare = LoadRequired(MissionShareResource);
        Sprite missionProgressTrack = LoadRequired(MissionProgressTrackResource);
        Sprite missionProgressCyan = LoadRequired(MissionProgressCyanResource);
        Sprite missionProgressMagenta = LoadRequired(MissionProgressMagentaResource);
        missionCheckCyanSprite = LoadRequired(MissionCheckCyanResource);
        missionCheckMagentaSprite = LoadRequired(MissionCheckMagentaResource);
        Sprite missionClock = LoadRequired(MissionClockResource);
        Sprite missionPortal = LoadRequired(MissionPortalResource);
        Sprite playerStar = LoadRequired(PlayerStarResource);

        IsReady = ArtReady(
            background, logo, fallbackAvatar, six, seven, rewardChest, calendar,
            stars, confetti, outerBezelBody, backButton, playerChip,
            playerAvatarRing, playerXpTrack,
            titleRibbon, challengeBoard, infoPanel, inputShell,
            baseAttemptSprite, bonusAttemptSprite, guessAction,
            shareAction, reviveAction, streakBoard, missionRewardBoard,
            missionTrophy, missionBrain, missionShare,
            missionProgressTrack, missionProgressCyan,
            missionProgressMagenta, missionCheckCyanSprite,
            missionCheckMagentaSprite, missionClock, missionPortal,
            playerStar) &&
            displayFont != null && bodyFont != null;

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
        starsImage.color = new Color(1f, 1f, 1f, 0.20f);

        var confettiImage = EnsureImage(visualRoot, "DailyConfetti");
        Stretch(confettiImage.rectTransform);
        ConfigureImage(confettiImage, confetti, false, Image.Type.Simple);
        confettiImage.color = new Color(1f, 1f, 1f, 0.24f);

        // The locked Daily Hunt reference has its own measured portrait bezel.
        // Render that approved asset once, at the canonical full-screen bounds;
        // never stack another frame or procedural border over it.
        var outerBody = EnsureImage(visualRoot, "DailyOuterBezelBody");
        ConfigureImage(
            outerBody, outerBezelBody, false, Image.Type.Sliced);
        outerBody.fillCenter = false;
        outerBody.pixelsPerUnitMultiplier = 2.75f;
        Stretch(outerBody.rectTransform);
        outerBody.rectTransform.offsetMin = new Vector2(5f, 7f);
        outerBody.rectTransform.offsetMax = new Vector2(-5f, -7f);

        safeRoot = EnsureRect(visualRoot, SafeRootName);
        Stretch(safeRoot);
        Canvas canvas = panel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            ResponsiveSafeAreaRoot.Attach(
                safeRoot, canvas.transform as RectTransform,
                new Vector2(ReferenceWidth, ReferenceHeight));
        }

        Button close = RuntimeUI.CreateButton(
            safeRoot, "CloseButton", string.Empty, Vector2.zero,
            new Vector2(155f, 155f), Color.white);

        BuildTopBar(
            close, backButton, playerChip,
            playerAvatarRing, playerStar,
            playerXpTrack, guessAction);

        huntRoot = EnsureRect(safeRoot, "DailyNumberHuntRoot");
        Stretch(huntRoot);

        Button submit = RuntimeUI.CreateButton(
            huntRoot, "SubmitGuessButton", L10n.Get("pvp_guess"),
            Vector2.zero, new Vector2(700f, 200f), Color.white);
        Button revive = RuntimeUI.CreateButton(
            huntRoot, "ReviveButton", L10n.Get("second_chance", 2),
            Vector2.zero, new Vector2(700f, 200f), Color.white);
        Button share = RuntimeUI.CreateButton(
            huntRoot, "ShareButton", L10n.Get("share_result"),
            Vector2.zero, new Vector2(700f, 200f), Color.white);

        var logoImage = EnsureImage(safeRoot, "DailyLogo");
        ConfigureImage(logoImage, logo, true, Image.Type.Simple);
        logoRect = logoImage.rectTransform;

        var ribbon = EnsureImage(safeRoot, "DailyTitleRibbon");
        ConfigureImage(ribbon, titleRibbon, false, Image.Type.Simple);
        ribbonRect = ribbon.rectTransform;

        ribbonTitle = EnsureText(
            ribbon.transform, "DailyRibbonTitle", 48f, displayFont,
            NearWhite, TextAlignmentOptions.Center);
        Place(
            ribbonTitle.rectTransform, new Vector2(0f, 22f),
            new Vector2(780f, 120f));
        ConfigureDisplayText(ribbonTitle, 69f, 69f);
        ribbonTitle.enableAutoSizing = false;
        ribbonTitle.fontSize = 69f;
        ribbonTitle.enableWordWrapping = false;

        var challengeCard = EnsureImage(huntRoot, "DailyChallengeCard");
        ConfigureImage(challengeCard, challengeBoard, false, Image.Type.Simple);
        challengeCardRect = challengeCard.rectTransform;

        var calendarImage = EnsureImage(
            challengeCard.transform, "DailyCalendarTarget");
        ConfigureImage(calendarImage, calendar, true, Image.Type.Simple);
        Place(
            calendarImage.rectTransform, new Vector2(-278f, -6f),
            new Vector2(390f, 430f));

        var headingFrame = EnsureImage(
            challengeCard.transform, "DailyChallengeHeadingFrame");
        ConfigureImage(headingFrame, inputShell, false, Image.Type.Simple);
        Place(
            headingFrame.rectTransform, new Vector2(198f, 280f),
            new Vector2(545f, 100f));

        challengeTitle = EnsureText(
            headingFrame.transform, "Title", 43f, displayFont,
            Cyan, TextAlignmentOptions.Center);
        TMP_Text title = challengeTitle;
        title.font = displayFont;
        title.color = Cyan;
        title.alignment = TextAlignmentOptions.Center;
        title.fontStyle = FontStyles.Bold;
        StretchText(title.rectTransform, 58f, 24f);
        ConfigureDisplayText(title, 30f, 43f);

        var statusFrame = EnsureImage(
            challengeCard.transform, "DailyStatusFrame");
        ConfigureImage(statusFrame, infoPanel, false, Image.Type.Simple);
        statusFrameRect = statusFrame.rectTransform;
        Place(
            statusFrameRect, new Vector2(198f, 165f),
            new Vector2(545f, 134f));

        statusCopy = EnsureText(
            statusFrame.transform, "Status", 30f, bodyFont,
            NearWhite, TextAlignmentOptions.Center);
        TMP_Text status = statusCopy;
        status.font = bodyFont;
        status.color = NearWhite;
        status.alignment = TextAlignmentOptions.Center;
        status.fontStyle = FontStyles.Bold;
        StretchText(status.rectTransform, 50f, 24f);
        ConfigureBodyText(status, 21f, 30f);

        trailText = EnsureText(
            challengeCard.transform, "Trail", 44f, displayFont,
            Cyan, TextAlignmentOptions.Center);
        TMP_Text trail = trailText;
        trail.gameObject.SetActive(false);

        attemptHeading = EnsureText(
            challengeCard.transform, "DailyAttemptHeading", 31f,
            displayFont, NearWhite, TextAlignmentOptions.Center);
        Place(
            attemptHeading.rectTransform, new Vector2(0f, -174f),
            new Vector2(360f, 48f));
        ConfigureDisplayText(attemptHeading, 25f, 33f);

        for (int index = 0; index < attemptSlots.Length; index++)
        {
            var slot = EnsureImage(
                challengeCard.transform, "DailyAttemptSlot" + (index + 1));
            ConfigureImage(
                slot, index < 7 ? baseAttemptSprite : bonusAttemptSprite,
                false, Image.Type.Simple);
            attemptSlots[index] = slot;

            TMP_Text slotLabel = EnsureText(
                slot.transform, "DailyAttemptLabel" + (index + 1), 34f,
                displayFont, Muted, TextAlignmentOptions.Center);
            StretchText(slotLabel.rectTransform, 12f, 10f);
            ConfigureDisplayText(slotLabel, 25f, 36f);
            attemptSlotLabels[index] = slotLabel;
        }

        TMP_InputField input = RuntimeUI.CreateInputField(
            challengeCard.transform, "GuessInput",
            L10n.Get("number_placeholder"), Vector2.zero,
            new Vector2(545f, 124f));
        Place(
            (RectTransform)input.transform, new Vector2(198f, -8f),
            new Vector2(545f, 124f));
        StyleInput(input, inputShell);

        inputCaption = EnsureText(
            challengeCard.transform, "DailyInputCaption", 25f,
            displayFont, Cyan, TextAlignmentOptions.Center);
        Place(
            inputCaption.rectTransform, new Vector2(198f, 72f),
            new Vector2(430f, 34f));
        ConfigureDisplayText(inputCaption, 21f, 27f);

        submitRect = (RectTransform)submit.transform;
        StyleButton(submit, guessAction, Ink, 66f);

        var rewardCard = EnsureImage(huntRoot, "DailyRewardCard");
        ConfigureImage(rewardCard, streakBoard, false, Image.Type.Simple);
        rewardCardRect = rewardCard.rectTransform;

        rewardHeading = EnsureText(
            rewardCard.transform, "DailyRewardHeading", 36f,
            displayFont, NearWhite, TextAlignmentOptions.Center);
        Place(
            rewardHeading.rectTransform, new Vector2(190f, 72f),
            new Vector2(520f, 72f));
        ConfigureDisplayText(rewardHeading, 32f, 46f);

        streakValue = EnsureText(
            rewardCard.transform, "Streak", 90f, displayFont,
            Gold, TextAlignmentOptions.Center);
        TMP_Text streak = streakValue;
        Place(
            streak.rectTransform, new Vector2(190f, -42f),
            new Vector2(520f, 132f));
        streak.font = displayFont;
        streak.color = Gold;
        streak.alignment = TextAlignmentOptions.Center;
        streak.fontStyle = FontStyles.Bold;
        ConfigureDisplayText(streak, 62f, 90f);

        reviveRect = (RectTransform)revive.transform;
        StyleButton(revive, reviveAction, NearWhite, 36f);

        shareRect = (RectTransform)share.transform;
        StyleButton(share, shareAction, NearWhite, 46f);

        var sixImage = EnsureImage(safeRoot, "DailyMascotSix");
        ConfigureImage(sixImage, six, true, Image.Type.Simple);
        mascotSixRect = sixImage.rectTransform;

        var sevenImage = EnsureImage(safeRoot, "DailyMascotSeven");
        ConfigureImage(sevenImage, seven, true, Image.Type.Simple);
        mascotSevenRect = sevenImage.rectTransform;

        BuildMissionDashboard(
            challengeBoard, infoPanel, calendar, rewardChest,
            missionRewardBoard, guessAction, streakBoard,
            missionTrophy, missionBrain, missionShare,
            missionProgressTrack, missionProgressCyan,
            missionProgressMagenta, missionClock, missionPortal,
            playerStar);

        viewBindings = new ViewBindings(
            title, status, trail, streak, input,
            submit, revive, share, close, missionStartButton);
        ApplyResponsiveLayout(true);
        RefreshCopy();
        RefreshVisibleTrail();
        RefreshPlayerChip();
        SetMissionDashboardVisible(true);
    }

    void BuildMissionDashboard(
        Sprite challengeBoard,
        Sprite infoPanel,
        Sprite calendar,
        Sprite rewardChest,
        Sprite rewardBoard,
        Sprite startAction,
        Sprite rewardMask,
        Sprite trophyIcon,
        Sprite brainIcon,
        Sprite shareIcon,
        Sprite progressTrackSprite,
        Sprite progressCyanSprite,
        Sprite progressMagentaSprite,
        Sprite clockSprite,
        Sprite portalSprite,
        Sprite actionStarSprite)
    {
        missionRoot = EnsureRect(safeRoot, "DailyMissionDashboard");
        Stretch(missionRoot);

        var portal = EnsureImage(missionRoot, "DailyMissionPortal");
        ConfigureImage(portal, portalSprite, true, Image.Type.Simple);
        missionPortalRect = portal.rectTransform;

        var board = EnsureImage(missionRoot, "DailyMissionBoard");
        ConfigureImage(board, challengeBoard, false, Image.Type.Simple);
        missionBoardRect = board.rectTransform;

        var hero = EnsureImage(board.transform, "DailyMissionCalendar");
        ConfigureImage(hero, calendar, true, Image.Type.Simple);
        Place(
            hero.rectTransform, new Vector2(-290f, 7f),
            new Vector2(465f, 565f));

        missionHeading = EnsureText(
            board.transform, "DailyMissionHeading", 38f, displayFont,
            Cyan, TextAlignmentOptions.Center);
        Place(
            missionHeading.rectTransform, new Vector2(165f, 291f),
            new Vector2(470f, 106f));
        ConfigureDisplayText(missionHeading, 28f, 40f);

        Sprite[] icons = { trophyIcon, brainIcon, shareIcon };
        float[] rowY = { 160f, 2f, -161f };
        float[] iconX = { -223f, -237f, -230f };
        float[] iconSizes = { 115f, 112f, 125f };
        for (int index = 0; index < 3; index++)
        {
            var row = EnsureImage(board.transform, "DailyMissionRow" + (index + 1));
            ConfigureImage(row, infoPanel, false, Image.Type.Simple);
            row.color = new Color(0.66f, 0.74f, 0.84f, 0.55f);
            Place(
                row.rectTransform, new Vector2(190f, rowY[index]),
                new Vector2(610f, 205f));

            var icon = EnsureImage(
                row.transform, "DailyMissionIcon" + (index + 1));
            ConfigureImage(icon, icons[index], true, Image.Type.Simple);
            Place(
                icon.rectTransform, new Vector2(iconX[index], 0f),
                new Vector2(iconSizes[index], iconSizes[index]));

            missionLabels[index] = EnsureText(
                row.transform, "DailyMissionLabel" + (index + 1), 28f,
                displayFont, NearWhite, TextAlignmentOptions.Left);
            float labelX = index == 1 ? 9f : -11f;
            float labelY = index == 0 ? 35f : 36f;
            float labelWidth = index == 1 ? 300f : 280f;
            Place(
                missionLabels[index].rectTransform,
                new Vector2(labelX, labelY),
                new Vector2(labelWidth, 88f));
            ConfigureDisplayText(missionLabels[index], 28f, 28f);
            missionLabels[index].enableAutoSizing = false;
            missionLabels[index].fontSize = 28f;
            missionLabels[index].lineSpacing = index == 0 ? 0f : -6f;

            missionProgress[index] = EnsureText(
                row.transform, "DailyMissionProgress" + (index + 1), 25f,
                bodyFont, index == 1 ? Magenta : Cyan,
                TextAlignmentOptions.Center);
            Place(
                missionProgress[index].rectTransform,
                new Vector2(80f, -46f), new Vector2(110f, 42f));
            ConfigureBodyText(missionProgress[index], 26f, 26f);
            missionProgress[index].enableAutoSizing = false;
            missionProgress[index].fontSize = 26f;

            var progressTrack = EnsureImage(
                row.transform, "DailyMissionTrack" + (index + 1));
            ConfigureImage(
                progressTrack, progressTrackSprite,
                false, Image.Type.Simple);
            Place(
                progressTrack.rectTransform,
                new Vector2(-50f, -46f), new Vector2(190f, 48f));

            var fill = EnsureImage(
                progressTrack.transform, "DailyMissionFill" + (index + 1));
            ConfigureImage(
                fill, index == 1 ? progressMagentaSprite : progressCyanSprite,
                false, Image.Type.Filled);
            Stretch(fill.rectTransform);
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            missionFills[index] = fill;

            missionChecks[index] = EnsureImage(
                row.transform, "DailyMissionCheck" + (index + 1));
            ConfigureImage(
                missionChecks[index],
                index == 1 ? missionCheckMagentaSprite : missionCheckCyanSprite,
                true, Image.Type.Simple);
            Place(
                missionChecks[index].rectTransform,
                new Vector2(202f, 0f), new Vector2(128f, 128f));
        }

        missionCompletion = EnsureText(
            board.transform, "DailyMissionCompletion", 27f, displayFont,
            Cyan, TextAlignmentOptions.Center);
        Place(
            missionCompletion.rectTransform, new Vector2(50f, -294f),
            new Vector2(800f, 62f));
        ConfigureDisplayText(missionCompletion, 21f, 29f);

        // The generated production board is a faithful full-colour raster of
        // the approved component. Crop its non-art canvas at runtime through
        // an existing approved alpha silhouette; no second visual writer or
        // procedural visible panel is introduced.
        var rewardMaskImage = EnsureImage(
            missionRoot, "DailyMissionRewardBoard");
        ConfigureImage(rewardMaskImage, rewardMask, false, Image.Type.Simple);
        var rewardMaskComponent =
            rewardMaskImage.GetComponent<Mask>() ??
            rewardMaskImage.gameObject.AddComponent<Mask>();
        rewardMaskComponent.showMaskGraphic = false;
        missionRewardRect = rewardMaskImage.rectTransform;

        var reward = EnsureImage(
            rewardMaskImage.transform, "DailyMissionRewardArtwork");
        ConfigureImage(reward, rewardBoard, false, Image.Type.Simple);
        Place(
            reward.rectTransform, new Vector2(0f, -22f),
            new Vector2(1056f, 640f));

        var chest = EnsureImage(
            rewardMaskImage.transform, "DailyMissionRewardChest");
        ConfigureImage(chest, rewardChest, true, Image.Type.Simple);
        Place(
            chest.rectTransform, new Vector2(-271f, -16f),
            new Vector2(405f, 287f));

        missionRewardHeading = EnsureText(
            rewardMaskImage.transform, "DailyMissionRewardHeading", 39f,
            displayFont, NearWhite, TextAlignmentOptions.Center);
        Place(
            missionRewardHeading.rectTransform,
            new Vector2(176f, 123f), new Vector2(520f, 70f));
        ConfigureDisplayText(missionRewardHeading, 41f, 41f);
        missionRewardHeading.enableAutoSizing = false;
        missionRewardHeading.fontSize = 41f;

        missionRewardStatus = EnsureText(
            rewardMaskImage.transform, "DailyMissionRewardStatus", 20f,
            bodyFont, NearWhite, TextAlignmentOptions.Center);
        Place(
            missionRewardStatus.rectTransform,
            new Vector2(180f, 79f), new Vector2(430f, 32f));
        ConfigureBodyText(missionRewardStatus, 14f, 20f);
        missionRewardStatus.gameObject.SetActive(false);

        var clock = EnsureImage(
            rewardMaskImage.transform, "DailyMissionClock");
        ConfigureImage(clock, clockSprite, true, Image.Type.Simple);
        Place(
            clock.rectTransform, new Vector2(0f, 8f),
            new Vector2(88f, 88f));

        missionResetLabel = EnsureText(
            rewardMaskImage.transform, "DailyMissionResetLabel", 25f,
            displayFont, Magenta, TextAlignmentOptions.Center);
        Place(
            missionResetLabel.rectTransform,
            new Vector2(150f, 52f), new Vector2(330f, 44f));
        ConfigureDisplayText(missionResetLabel, 21f, 30f);

        missionReset = EnsureText(
            rewardMaskImage.transform, "DailyMissionReset", 35f,
            displayFont, Gold, TextAlignmentOptions.Center);
        Place(
            missionReset.rectTransform,
            new Vector2(140f, 0f), new Vector2(330f, 56f));
        ConfigureDisplayText(missionReset, 31f, 44f);

        var rewardTrophy = EnsureImage(
            rewardMaskImage.transform, "DailyMissionRewardTrophy");
        ConfigureImage(rewardTrophy, trophyIcon, true, Image.Type.Simple);
        Place(
            rewardTrophy.rectTransform, new Vector2(40f, -110f),
            new Vector2(125f, 125f));

        missionRewardAmount = EnsureText(
            rewardMaskImage.transform, "DailyMissionRewardAmount", 58f,
            displayFont, Gold, TextAlignmentOptions.Center);
        Place(
            missionRewardAmount.rectTransform,
            new Vector2(178f, -110f), new Vector2(350f, 104f));
        ConfigureDisplayText(missionRewardAmount, 88f, 88f);
        missionRewardAmount.enableAutoSizing = false;
        missionRewardAmount.fontSize = 88f;

        // Build this visual-owned control directly. RuntimeUI.CreateButton
        // registers its construction geometry with ResponsivePageLayout; that
        // second writer would keep restoring (0,0) after this owner placed the
        // button in the bottom action slot.
        missionStartRect = EnsureRect(missionRoot, "DailyMissionStartButton");
        var startImage = missionStartRect.GetComponent<Image>();
        if (startImage == null)
            startImage = missionStartRect.gameObject.AddComponent<Image>();
        var start = missionStartRect.GetComponent<Button>();
        if (start == null)
            start = missionStartRect.gameObject.AddComponent<Button>();
        TMP_Text startLabel = EnsureText(
            missionStartRect, "Label", 66f, displayFont,
            Ink, TextAlignmentOptions.Center);
        Stretch(startLabel.rectTransform);
        StyleButton(start, startAction, Ink, 66f);
        startLabel.enableAutoSizing = false;
        startLabel.fontSize = 70f;
        startLabel.rectTransform.offsetMin = new Vector2(118f, 30f);
        startLabel.rectTransform.offsetMax = new Vector2(-118f, -18f);

        var startStarLeft = EnsureImage(
            missionStartRect, "DailyMissionStartStarLeft");
        ConfigureImage(
            startStarLeft, actionStarSprite, true, Image.Type.Simple);
        Place(
            startStarLeft.rectTransform, new Vector2(-225f, 0f),
            new Vector2(64f, 64f));
        startStarLeft.transform.SetAsFirstSibling();

        var startStarRight = EnsureImage(
            missionStartRect, "DailyMissionStartStarRight");
        ConfigureImage(
            startStarRight, actionStarSprite, true, Image.Type.Simple);
        Place(
            startStarRight.rectTransform, new Vector2(225f, 0f),
            new Vector2(64f, 64f));
        startStarRight.transform.SetAsFirstSibling();
        missionStartButton = start;
    }

    void BuildTopBar(
        Button close,
        Sprite backButton,
        Sprite playerChipSprite,
        Sprite playerAvatarRing,
        Sprite playerStar,
        Sprite progressTrackSprite,
        Sprite progressFillSprite)
    {
        ClearButtonPresentation(close.transform);
        Reparent(close.transform, safeRoot);
        closeRect = (RectTransform)close.transform;
        StyleButton(close, backButton, NearWhite, 0f);
        HideButtonLabels(close.transform);

        var playerChipRoot = EnsureRect(safeRoot, "DailyPlayerChip");
        chipRect = playerChipRoot;

        var playerChipShell = EnsureImage(
            playerChipRoot, "DailyPlayerChipShell");
        ConfigureImage(
            playerChipShell, playerChipSprite, false, Image.Type.Simple);
        Place(
            playerChipShell.rectTransform, new Vector2(-3f, -9f),
            new Vector2(336f, 184f));

        var avatarRing = EnsureImage(
            playerChipRoot, "DailyPlayerAvatarRing");
        ConfigureImage(
            avatarRing, playerAvatarRing, true, Image.Type.Simple);
        Place(
            avatarRing.rectTransform, new Vector2(-120f, 5f),
            new Vector2(122f, 122f));

        Image avatarMaskImage = EnsureImage(
            playerChipRoot, "DailyPlayerAvatarClip");
        ConfigureCircularMask(avatarMaskImage);
        playerAvatarAperture = avatarMaskImage.rectTransform;
        Place(
            playerAvatarAperture, new Vector2(-120f, 5f),
            new Vector2(105f, 105f));

        playerAvatarImage = EnsureImage(
            avatarMaskImage.transform, "DailyPlayerAvatar");
        ConfigureImage(
            playerAvatarImage,
            PlayerProfileAvatarResolver.Resolve(),
            true,
            Image.Type.Simple);
        PlayerProfileAvatarFraming.Apply(
            playerAvatarImage, playerAvatarAperture);

        chipName = EnsureText(
            playerChipRoot, "DailyPlayerName", 31f, displayFont,
            NearWhite, TextAlignmentOptions.Center);
        Place(
            chipName.rectTransform, new Vector2(45f, 53f),
            new Vector2(220f, 40f));
        ConfigureDisplayText(chipName, 24f, 34f);
        chipName.enableAutoSizing = true;
        chipName.fontSize = 34f;
        chipName.overflowMode = TextOverflowModes.Overflow;

        var star = EnsureImage(
            playerChipRoot, "DailyPlayerStar");
        ConfigureImage(star, playerStar, true, Image.Type.Simple);
        Place(
            star.rectTransform, new Vector2(-9f, -4f),
            new Vector2(30f, 30f));

        chipWins = EnsureText(
            playerChipRoot, "DailyPlayerWins", 28f, displayFont,
            NearWhite, TextAlignmentOptions.Center);
        Place(
            chipWins.rectTransform, new Vector2(58f, 3f),
            new Vector2(120f, 38f));
        ConfigureDisplayText(chipWins, 21f, 27f);
        chipWins.enableAutoSizing = false;
        chipWins.fontSize = 30f;
        chipWins.overflowMode = TextOverflowModes.Ellipsis;

        chipProgress = EnsureText(
            playerChipRoot, "DailyPlayerProgress", 25f, displayFont,
            NearWhite, TextAlignmentOptions.Center);
        Place(
            chipProgress.rectTransform, new Vector2(45f, -71f),
            new Vector2(176f, 36f));
        ConfigureDisplayText(chipProgress, 31f, 34f);
        chipProgress.enableAutoSizing = false;
        chipProgress.fontSize = 34f;
        chipProgress.overflowMode = TextOverflowModes.Truncate;

        var xpTrack = EnsureImage(
            playerChipRoot, "DailyPlayerXpTrack");
        ConfigureImage(
            xpTrack, progressTrackSprite,
            false, Image.Type.Simple);
        Place(
            xpTrack.rectTransform, new Vector2(48f, -20f),
            new Vector2(150f, 24f));

        var progressFillTrack = EnsureImage(
            playerChipRoot, "DailyPlayerProgressFillTrack");
        ConfigureImage(
            progressFillTrack, progressTrackSprite,
            false, Image.Type.Simple);
        Place(
            progressFillTrack.rectTransform, new Vector2(-10f, -66f),
            new Vector2(270f, 34f));

        var progressFillRoot = EnsureRect(
            playerChipRoot, "DailyPlayerProgressFillRoot");
        // The approved chip uses a wide lower track: the live yellow fill
        // grows from its left edge while the numeric value remains readable
        // over the unfilled dark portion on the same baseline.
        Place(
            progressFillRoot, new Vector2(-10f, -66f),
            new Vector2(270f, 34f));

        chipProgressFill = EnsureImage(
            progressFillRoot, "DailyPlayerProgressFill");
        ConfigureImage(
            chipProgressFill, progressFillSprite,
            false, Image.Type.Sliced);
        Stretch(chipProgressFill.rectTransform);
        chipProgress.transform.SetAsLastSibling();
    }

    void RefreshCopy()
    {
        if (!IsReady) return;

        if (ribbonTitle != null)
            ribbonTitle.text = L10n.Get(
                missionDashboardVisible
                    ? "daily_challenge_title"
                    : "home_daily_title");
        if (rewardHeading != null)
            rewardHeading.text = L10n.Get("daily_streak_heading");
        if (attemptHeading != null)
            attemptHeading.text = L10n.Get("result_attempts");
        if (inputCaption != null)
            inputCaption.text = L10n.Get("your_guess").ToUpperInvariant();
        if (missionHeading != null)
            missionHeading.text = L10n.Get("daily_missions_heading");
        if (missionRewardHeading != null)
            missionRewardHeading.text = L10n.Get("daily_reward_heading");
        if (missionResetLabel != null)
            missionResetLabel.text = L10n.Get("daily_reset_label");
        if (missionLabels[0] != null)
            missionLabels[0].text = L10n.Get("daily_mission_win");
        if (missionLabels[1] != null)
            missionLabels[1].text = L10n.Get("daily_mission_correct");
        if (missionLabels[2] != null)
            missionLabels[2].text = L10n.Get("daily_mission_share_room");
        Transform start = missionRoot != null
            ? Find<Transform>(missionRoot, "DailyMissionStartButton")
            : null;
        if (start != null)
        {
            TMP_Text label = start.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = L10n.Get("daily_start");
        }
        ApplyResponsiveLayout(true);
        RefreshVisibleTrail();
        RefreshStreakValue();
        RefreshMissionState();
        RefreshPlayerChip();
    }

    public void SetMissionDashboardVisible(bool visible)
    {
        missionDashboardVisible = visible;
        if (missionRoot != null)
            missionRoot.gameObject.SetActive(visible);
        if (huntRoot != null)
            huntRoot.gameObject.SetActive(!visible);
        RefreshCopy();
    }

    void RefreshMissionState()
    {
        if (!IsReady || missionHeading == null) return;

        DailyChallengeProgress.Snapshot state = DailyChallengeProgress.Current;
        int[] values = { state.Wins, state.CorrectGuesses, state.RoomsShared };
        int[] targets =
        {
            DailyChallengeProgress.WinTarget,
            DailyChallengeProgress.CorrectGuessTarget,
            DailyChallengeProgress.RoomShareTarget,
        };

        int complete = 0;
        for (int index = 0; index < values.Length; index++)
        {
            bool done = values[index] >= targets[index];
            if (done) complete++;
            if (missionProgress[index] != null)
                missionProgress[index].text = values[index] + " / " + targets[index];
            if (missionFills[index] != null)
                missionFills[index].fillAmount = targets[index] <= 0
                    ? 0f
                    : Mathf.Clamp01(values[index] / (float)targets[index]);
            if (missionChecks[index] != null)
            {
                missionChecks[index].sprite = index == 1
                    ? missionCheckMagentaSprite
                    : missionCheckCyanSprite;
                missionChecks[index].gameObject.SetActive(done);
            }
        }

        if (missionCompletion != null)
        {
            missionCompletion.text = state.Complete
                ? L10n.Get("daily_all_missions_complete")
                : L10n.Get("daily_missions_progress", complete, 3);
            missionCompletion.color = Cyan;
        }

        TimeSpan reset = DailyChallengeProgress.TimeUntilReset;
        if (missionReset != null)
        {
#if DEVELOPMENT_BUILD
            missionReset.text = string.IsNullOrEmpty(captureResetText)
                ? string.Format(
                    "{0:00}:{1:00}:{2:00}",
                    Mathf.Max(0, (int)reset.TotalHours),
                    reset.Minutes,
                    reset.Seconds)
                : captureResetText;
#else
            missionReset.text = string.Format(
                "{0:00}:{1:00}:{2:00}",
                Mathf.Max(0, (int)reset.TotalHours),
                reset.Minutes,
                reset.Seconds);
#endif
        }
        if (missionRewardAmount != null)
            missionRewardAmount.text = DailyChallengeProgress.RewardPoints.ToString();
        if (missionRewardStatus != null)
            missionRewardStatus.text = state.RewardClaimed
                ? L10n.Get("daily_reward_collected")
                : L10n.Get("daily_reward_pending");

        RefreshPlayerChip();
    }

    void RefreshStreakValue()
    {
        if (streakValue == null) return;
        streakValue.text = PlayerPrefs.GetInt("DailyHuntStreak", 0).ToString();
    }

    void RefreshVisibleTrail()
    {
        if (trailText == null || attemptSlotLabels == null) return;

        string safe = (trailText.text ?? string.Empty)
            .Replace("🎯", "●")
            .Replace("🔺", "▲")
            .Replace("🔻", "▼")
            .Trim();

        bool revived = PlayerPrefs.GetInt("DailyHuntRevived", 0) == 1;
        ApplyAttemptLayout(revived);

        for (int index = 0; index < attemptSlotLabels.Length; index++)
        {
            TMP_Text label = attemptSlotLabels[index];
            if (label == null) continue;

            if (index >= safe.Length)
            {
                label.text = (index + 1).ToString();
                label.color = Muted;
                continue;
            }

            char result = safe[index];
            label.text = result.ToString();
            label.color = result == '●'
                ? Gold
                : result == '▲' ? Magenta : Cyan;
        }
    }

    void RefreshInteractionPresentation()
    {
        if (viewBindings == null || viewBindings.Input == null ||
            statusFrameRect == null || inputCaption == null)
            return;

        bool inputVisible = viewBindings.Input.gameObject.activeSelf;
        if (inputVisible == lastInputVisible &&
            inputCaption.gameObject.activeSelf == inputVisible)
            return;

        lastInputVisible = inputVisible;
        inputCaption.gameObject.SetActive(inputVisible);

        // While playing, status is a compact two-line information row above
        // the real numeric input. Found/failed/revive states hide that input;
        // the same approved info-panel asset then expands to its natural
        // aspect and occupies the released interaction zone. This prevents a
        // stale YOUR GUESS caption and a large dead gap without inventing a
        // second panel, moving the challenge board, or changing gameplay.
        Place(
            statusFrameRect,
            new Vector2(198f, inputVisible ? 165f : 74f),
            new Vector2(545f, inputVisible ? 134f : 218f));
    }

    void ApplyAttemptLayout(bool revived)
    {
        if (attemptSlots == null || attemptSlots.Length != 9) return;

        if (revived != lastRevivedLayout)
            lastRevivedLayout = revived;

        // Normal play never reserves or compresses space for nine guesses:
        // the seven base slots keep their canonical size and breathing room.
        // A successful Revive deliberately reflows them upward and reveals a
        // second centred bonus row without shrinking either family.
        float baseY = revived ? -190f : -254f;
        const float baseStep = 116f;
        for (int index = 0; index < 7; index++)
        {
            Image slot = attemptSlots[index];
            if (slot == null) continue;
            slot.gameObject.SetActive(true);
            Place(
                slot.rectTransform,
                new Vector2((index - 3) * baseStep, baseY),
                new Vector2(102f, 76f));
        }

        for (int index = 7; index < 9; index++)
        {
            Image slot = attemptSlots[index];
            if (slot == null) continue;
            slot.gameObject.SetActive(revived);
            if (!revived) continue;
            Place(
                slot.rectTransform,
                new Vector2(index == 7 ? -66f : 66f, -260f),
                new Vector2(102f, 76f));
        }

        if (attemptHeading != null)
            Place(
                attemptHeading.rectTransform,
                new Vector2(0f, revived ? -142f : -174f),
                new Vector2(360f, 48f));
    }

    void RefreshPlayerChip()
    {
        if (playerAvatarImage != null)
        {
            playerAvatarImage.sprite = PlayerProfileAvatarResolver.Resolve();
            PlayerProfileAvatarFraming.Apply(
                playerAvatarImage, playerAvatarAperture);
        }

        if (chipName == null || chipWins == null || chipProgress == null) return;

        string player = PlayerPrefs.GetString("PlayerName", "");
        if (string.IsNullOrWhiteSpace(player))
            player = L10n.Get("player_default");
        chipName.text = player;
        DailyChallengeProgress.Snapshot state = DailyChallengeProgress.Current;
        int displayedPoints = state.Points;
        int displayedProgress = state.PointsTowardMilestone;
#if DEVELOPMENT_BUILD
        if (captureChipPoints >= 0)
            displayedPoints = captureChipPoints;
        if (captureChipProgress >= 0)
            displayedProgress = captureChipProgress;
#endif
        chipWins.text = displayedPoints.ToString("N0");
        chipProgress.text = "<color=#FFCD33>" +
            displayedProgress.ToString("N0") + "</color> / " +
            DailyChallengeProgress.PointsMilestone.ToString("N0");
        if (chipProgressFill != null)
        {
            float progress = DailyChallengeProgress.PointsMilestone <= 0
                ? 0f
                : Mathf.Clamp01(
                    displayedProgress /
                    (float)DailyChallengeProgress.PointsMilestone);
            SetHorizontalFill(chipProgressFill.rectTransform, progress);
        }
    }

    void ApplyResponsiveLayout(bool force = false)
    {
        ApplyResponsiveLayoutForViewport(Screen.width, Screen.height, force);
    }

    // Deterministic seam for exact responsive geometry tests. Runtime still
    // enters through ApplyResponsiveLayout and therefore uses the real Screen
    // dimensions; tests can validate every approved viewport even when the
    // batchmode Game View ignores Screen.SetResolution.
    void ApplyResponsiveLayoutForViewport(
        int width,
        int height,
        bool force = false)
    {
        if (closeRect == null || chipRect == null || logoRect == null ||
            ribbonRect == null || challengeCardRect == null ||
            rewardCardRect == null || submitRect == null ||
            reviveRect == null || shareRect == null ||
            mascotSixRect == null || mascotSevenRect == null)
            return;

        L10n.Language language = L10n.Current;
        if (!force && width == lastLayoutWidth && height == lastLayoutHeight &&
            language == lastLayoutLanguage)
            return;

        lastLayoutWidth = width;
        lastLayoutHeight = height;
        lastLayoutLanguage = language;

        float aspect = width > 0
            ? Mathf.Max(1, height) / (float)width
            : ReferenceHeight / ReferenceWidth;
        float tall = Mathf.InverseLerp(1.78f, 2.22f, aspect);

        Place(
            closeRect, new Vector2(-435f, 836f + 165f * tall),
            new Vector2(155f, 155f));
        Place(
            chipRect, new Vector2(335f, 827f + 165f * tall),
            new Vector2(365f, 194f));
        Place(
            logoRect, new Vector2(-10f, 783f + 110f * tall),
            new Vector2(396f, 295f));
        Place(
            ribbonRect, new Vector2(0f, 585f + 90f * tall),
            new Vector2(1040f, 285f));
        Place(
            challengeCardRect, new Vector2(0f, 180f + 25f * tall),
            new Vector2(1000f, 790f));
        Place(
            rewardCardRect, new Vector2(0f, -350f - 35f * tall),
            new Vector2(1000f, 387f));
        if (missionBoardRect != null)
            Place(
                missionBoardRect, new Vector2(-1f, 119f + 30f * tall),
                new Vector2(1036f, 874f));
        if (missionRewardRect != null)
            Place(
                missionRewardRect, new Vector2(0f, -417f - 65f * tall),
                new Vector2(1060f, 425f));
        if (missionPortalRect != null)
            Place(
                missionPortalRect, new Vector2(0f, -860f - 240f * tall),
                new Vector2(1110f, 205f));

        // Gameplay owns which of these three controls is active. Presentation
        // gives them one shared state-specific action slot.
        Vector2 actionPosition = new Vector2(0f, -690f - 95f * tall);
        Vector2 actionSize = new Vector2(700f, 200f);
        Place(submitRect, actionPosition, actionSize);
        Place(reviveRect, actionPosition, actionSize);
        Place(shareRect, actionPosition, actionSize);
        if (missionStartRect != null)
            Place(
                missionStartRect,
                new Vector2(0f, -771f - 185f * tall),
                new Vector2(595f, 230f));

        Place(
            mascotSixRect,
            new Vector2(-372f, -754f - 165f * tall),
            new Vector2(322f, 375f));
        Place(
            mascotSevenRect,
            new Vector2(363f, -748f - 165f * tall),
            new Vector2(326f, 380f));

        if (ribbonTitle != null)
        {
            ribbonTitle.enableAutoSizing = false;
            ribbonTitle.fontSize = language == L10n.Language.Greek
                ? 69f
                : 72f;
        }
        if (challengeTitle != null)
        {
            challengeTitle.fontSizeMin =
                language == L10n.Language.Greek ? 20f : 30f;
            challengeTitle.fontSizeMax =
                language == L10n.Language.Greek ? 34f : 43f;
        }
        if (statusCopy != null)
        {
            statusCopy.fontSizeMin =
                language == L10n.Language.Greek ? 17f : 21f;
            statusCopy.fontSizeMax =
                language == L10n.Language.Greek ? 26f : 30f;
        }
        if (missionHeading != null)
        {
            missionHeading.fontSizeMin =
                language == L10n.Language.Greek ? 27f : 30f;
            missionHeading.fontSizeMax =
                language == L10n.Language.Greek ? 39f : 42f;
        }
        if (missionCompletion != null)
            missionCompletion.fontSizeMin =
                language == L10n.Language.Greek ? 21f : 23f;
    }

    void StyleInput(TMP_InputField input, Sprite frame)
    {
        if (input == null) return;

        var image = input.GetComponent<Image>();
        if (image == null)
            image = input.gameObject.AddComponent<Image>();
        image.enabled = true;
        image.sprite = frame;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = true;

        if (input.textComponent != null)
        {
            input.textComponent.font = bodyFont;
            input.textComponent.fontSize = 44f;
            input.textComponent.fontStyle = FontStyles.Bold;
            input.textComponent.color = NearWhite;
            input.textComponent.alignment = TextAlignmentOptions.Center;
            ConfigureDisplayText(input.textComponent, 39f, 52f);
        }

        TMP_Text placeholder = input.placeholder as TMP_Text;
        if (placeholder != null)
        {
            placeholder.font = bodyFont;
            placeholder.fontSize = 32f;
            placeholder.color = Muted;
            placeholder.alignment = TextAlignmentOptions.Center;
            ConfigureBodyText(placeholder, 25f, 34f);
        }
    }

    static void SetHorizontalFill(RectTransform rect, float amount)
    {
        if (rect == null) return;
        float clamped = Mathf.Clamp01(amount);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(clamped, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
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
        image.type = Image.Type.Simple;
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
            label.font = displayFont;
            label.color = labelColor;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            if (labelSize > 0f)
            {
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(72f, 30f);
                label.rectTransform.offsetMax = new Vector2(-72f, -18f);
                label.rectTransform.localScale = Vector3.one;
                label.rectTransform.localRotation = Quaternion.identity;
                label.enableAutoSizing = true;
                label.fontSizeMin = Mathf.Max(23f, labelSize - 9f);
                label.fontSizeMax = labelSize;
                label.overflowMode = TextOverflowModes.Overflow;
                ConfigureDisplayText(
                    label, Mathf.Max(23f, labelSize - 9f), labelSize);
            }
        }
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
        text.outlineColor = Ink;
        text.outlineWidth = 0.18f;

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

    static void ConfigureCircularMask(Image maskImage)
    {
        Sprite maskSprite =
            Resources.Load<Sprite>(
                PlayerProfileAvatarResolver.CircularApertureResourcePath);
        if (maskSprite == null)
        {
            Debug.LogError(
                "[DailyHuntVisuals] Shared circular avatar mask is missing.");
            maskImage.gameObject.SetActive(false);
            return;
        }
        ConfigureImage(
            maskImage,
            maskSprite,
            false,
            Image.Type.Simple);
        var mask = maskImage.GetComponent<Mask>();
        if (mask == null)
            mask = maskImage.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;
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
