using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Onboarding presentation half of SplashDesign. This is the same component and
// the same presentation owner as the returning-player Splash; it is split only
// to keep the five-state implementation readable.
public sealed partial class SplashDesign
{
    public const string OnboardingRootName = "HOLOnboardingRoot";

    const string OnboardingBackgroundResource =
        "phase2a/hol_neon_reference_bg_r3";
    const string OnboardingLogoResource = "reference/hol_logo_exact";
    const string OnboardingBackResource =
        "solo/production/solo_back_button_v1";
    const string OnboardingWelcomeEnsembleResource =
        "onboarding/characters/welcome_human_ensemble";
    const string OnboardingGenderBoyResource =
        "phase2a/hol_menu_boy_arms_crossed_r3";
    const string OnboardingGenderGirlResource =
        "onboarding/characters/gender_girl_arms_crossed";
    const string OnboardingGenderOtherResource =
        "onboarding/characters/gender_other_purple";
    const string OnboardingMascotThreeResource = "reference/mascot_3_exact";
    const string OnboardingMascotSixResource = "reference/mascot_6_exact";
    const string OnboardingMascotSevenResource = "reference/mascot_7_exact";
    const string OnboardingAgeUnder13MascotResource =
        "onboarding/mascots/age_under13_mascot_3_green";
    const string OnboardingAgeTeenMascotResource =
        "onboarding/mascots/age_teen_mascot_7_blue";
    const string OnboardingAgeAdultMascotResource =
        "onboarding/mascots/age_adult_mascot_6_pink";
    const string OnboardingStarsResource = "mainmenu/mainmenu_deco_stars";
    const string OnboardingConfettiResource =
        "mainmenu/mainmenu_deco_confetti";
    const string OnboardingGoldResource =
        "phase2a/hol_cta_gold_r2_9s";
    const string OnboardingBlueResource =
        "phase2a/hol_cta_blue_r2_9s";
    const string OnboardingMagentaResource =
        "phase2a/hol_cta_magenta_r2_9s";
    const string OnboardingPanelResource =
        "phase2a/hol_tip_frame_r2_9s";
    const string OnboardingPrivacyIconResource =
        "settings/settings_icon_privacy_3d";
    const string OnboardingNameInputIconResource =
        "settings/settings_icon_player_3d";
    const string OnboardingAgeUnder13IconResource =
        "onboarding/icons/age_under13_shield_star";
    const string OnboardingAgeTeenIconResource =
        "onboarding/icons/age_teen_star";
    const string OnboardingAgeAdultIconResource =
        "onboarding/icons/age_adult_crown";
    const string OnboardingIndicatorDiscResource =
        "onboarding/icons/onboarding_indicator_disc_neutral";
    const string OnboardingDisplayFontResource =
        "phase2a/fonts/HOL Menu Display SDF";
    const string OnboardingBodyFontResource =
        "phase2a/fonts/HOL Menu Body SDF";

    static readonly Color OnboardingInk =
        new Color(0.07f, 0.025f, 0.14f, 1f);
    static readonly Color OnboardingWhite =
        new Color(0.99f, 0.98f, 1f, 1f);
    static readonly Color OnboardingMuted =
        new Color(0.83f, 0.80f, 0.93f, 0.96f);
    static readonly Color OnboardingCyan =
        new Color(0.15f, 0.94f, 1f, 1f);
    static readonly Color OnboardingMagenta =
        new Color(1f, 0.20f, 0.68f, 1f);
    static readonly Color OnboardingGold =
        new Color(1f, 0.82f, 0.20f, 1f);
    static readonly Color OnboardingGreen =
        new Color(0.34f, 1f, 0.38f, 1f);
    static readonly Color OnboardingDisabledCopy =
        new Color(0.82f, 0.80f, 0.90f, 1f);
    static readonly Vector2 OnboardingContinueCtaSize =
        new Vector2(920f, 205f);
    static readonly Vector2 AvatarFilterSize = new Vector2(170f, 82f);
    static readonly Vector2 AvatarCardSize = new Vector2(184f, 208f);

    readonly RectTransform[] onboardingScreens = new RectTransform[5];
    readonly RectTransform[] onboardingHeaderGroups = new RectTransform[5];
    readonly RectTransform[] onboardingContentGroups = new RectTransform[5];
    readonly RectTransform[] onboardingFooterGroups = new RectTransform[5];
    readonly Button[] onboardingContinueButtons = new Button[5];
    readonly GameObject[] genderSelectionBadges = new GameObject[3];
    readonly Image[] genderCardImages = new Image[3];
    readonly Outline[] genderSelectionOutlines = new Outline[3];
    readonly GameObject[] avatarSelectionBadges = new GameObject[12];
    readonly Image[] avatarCardImages = new Image[12];
    readonly Outline[] avatarSelectionOutlines = new Outline[12];
    readonly Button[] avatarCardButtons = new Button[12];
    readonly TMP_Text[] avatarAvailabilityLabels = new TMP_Text[12];
    readonly RectTransform[] avatarCardRects = new RectTransform[12];
    readonly Image[] avatarFilterImages = new Image[5];
    readonly GameObject[] ageSelectionBadges = new GameObject[3];
    readonly Image[] ageCardImages = new Image[3];
    readonly Outline[] ageSelectionOutlines = new Outline[3];

    RectTransform onboardingRoot;
    RectTransform onboardingSafeRoot;
    SplashOnboardingController onboardingController;
    TMP_InputField onboardingNameInput;
    TMP_Text onboardingNameCounter;
    Image onboardingAvatarPreview;
    TMP_Text onboardingAvatarPreviewPrompt;
    TMP_Text onboardingAvatarStatus;
    Image onboardingStarsImage;
    Image onboardingConfettiImage;
    TMP_FontAsset onboardingDisplayFont;
    TMP_FontAsset onboardingBodyFont;
    Sprite onboardingGold;
    Sprite onboardingBlue;
    Sprite onboardingMagenta;
    Sprite onboardingPanel;
    Sprite onboardingCircle;
    Sprite onboardingLogo;
    Sprite onboardingBack;
    Sprite[] onboardingAvatars;
    OnboardingAvatarCatalog.Category onboardingAvatarFilter =
        OnboardingAvatarCatalog.Category.All;
    int onboardingLayoutWidth = -1;
    int onboardingLayoutHeight = -1;

    public bool IsOnboardingVisible { get; private set; }

    public SplashOnboardingController.Step CurrentOnboardingStep =>
        onboardingController == null
            ? SplashOnboardingController.Step.Welcome
            : onboardingController.CurrentStep;

    public static readonly string[] OnboardingLoadedResources =
    {
        OnboardingBackgroundResource,
        OnboardingLogoResource,
        OnboardingBackResource,
        OnboardingWelcomeEnsembleResource,
        OnboardingGenderBoyResource,
        OnboardingGenderGirlResource,
        OnboardingGenderOtherResource,
        OnboardingMascotThreeResource,
        OnboardingMascotSixResource,
        OnboardingMascotSevenResource,
        OnboardingAgeUnder13MascotResource,
        OnboardingAgeTeenMascotResource,
        OnboardingAgeAdultMascotResource,
        OnboardingStarsResource,
        OnboardingConfettiResource,
        OnboardingGoldResource,
        OnboardingBlueResource,
        OnboardingMagentaResource,
        OnboardingPanelResource,
        OnboardingPrivacyIconResource,
        OnboardingNameInputIconResource,
        OnboardingAgeUnder13IconResource,
        OnboardingAgeTeenIconResource,
        OnboardingAgeAdultIconResource,
        OnboardingIndicatorDiscResource,
    };

    void BuildOnboarding(Canvas canvas)
    {
        Sprite background = LoadSprite(OnboardingBackgroundResource);
        Sprite logo = LoadSprite(OnboardingLogoResource);
        Sprite back = LoadSprite(OnboardingBackResource);
        Sprite welcomeEnsemble = LoadSprite(
            OnboardingWelcomeEnsembleResource);
        Sprite genderBoy = LoadSprite(OnboardingGenderBoyResource);
        Sprite genderGirl = LoadSprite(OnboardingGenderGirlResource);
        Sprite genderOther = LoadSprite(OnboardingGenderOtherResource);
        Sprite mascotThree = LoadSprite(OnboardingMascotThreeResource);
        Sprite mascotSix = LoadSprite(OnboardingMascotSixResource);
        Sprite mascotSeven = LoadSprite(OnboardingMascotSevenResource);
        Sprite ageUnder13Mascot = LoadSprite(
            OnboardingAgeUnder13MascotResource);
        Sprite ageTeenMascot = LoadSprite(OnboardingAgeTeenMascotResource);
        Sprite ageAdultMascot = LoadSprite(OnboardingAgeAdultMascotResource);
        Sprite stars = LoadSprite(OnboardingStarsResource);
        Sprite confetti = LoadSprite(OnboardingConfettiResource);
        onboardingGold = LoadSprite(OnboardingGoldResource);
        onboardingBlue = LoadSprite(OnboardingBlueResource);
        onboardingMagenta = LoadSprite(OnboardingMagentaResource);
        onboardingPanel = LoadSprite(OnboardingPanelResource);
        Sprite privacyIcon = LoadSprite(OnboardingPrivacyIconResource);
        Sprite nameInputIcon = LoadSprite(OnboardingNameInputIconResource);
        onboardingCircle = LoadSprite(OnboardingIndicatorDiscResource);
        Sprite ageUnder13Icon = LoadSprite(
            OnboardingAgeUnder13IconResource);
        Sprite ageTeenIcon = LoadSprite(OnboardingAgeTeenIconResource);
        Sprite ageAdultIcon = LoadSprite(OnboardingAgeAdultIconResource);
        onboardingDisplayFont =
            Resources.Load<TMP_FontAsset>(OnboardingDisplayFontResource);
        onboardingBodyFont =
            Resources.Load<TMP_FontAsset>(OnboardingBodyFontResource);

        if (!EnsureUnderlineCharacter(onboardingDisplayFont) ||
            !EnsureUnderlineCharacter(onboardingBodyFont))
        {
            Debug.LogError(
                "Onboarding fonts require an in-memory underscore mapping.");
            IsReady = false;
            IsSettled = false;
            return;
        }

        Sprite[] avatars = new Sprite[OnboardingAvatarCatalog.Count];
        for (int index = 0; index < avatars.Length; index++)
            avatars[index] = LoadSprite(
                OnboardingAvatarCatalog.Get(index).ResourcePath);

        if (!OnboardingArtReady(
                background, logo, back, welcomeEnsemble,
                genderBoy, genderGirl, genderOther,
                mascotThree, mascotSix, mascotSeven, stars, confetti,
                ageUnder13Mascot, ageTeenMascot, ageAdultMascot,
                onboardingGold, onboardingBlue, onboardingMagenta,
                onboardingPanel, privacyIcon, nameInputIcon,
                onboardingCircle,
                ageUnder13Icon, ageTeenIcon, ageAdultIcon,
                onboardingDisplayFont, onboardingBodyFont,
                avatars))
        {
            IsReady = false;
            IsSettled = false;
            return;
        }

        onboardingLogo = logo;
        onboardingBack = back;
        onboardingAvatars = avatars;

        onboardingRoot = EnsureRect(canvas.transform, OnboardingRootName);
        Stretch(onboardingRoot);

        Image backgroundImage = EnsureImage(
            onboardingRoot, "OnboardingBackground");
        ConfigureImage(backgroundImage, background, false);
        Stretch(backgroundImage.rectTransform);

        onboardingSafeRoot = EnsureRect(
            onboardingRoot, "OnboardingSafeAreaRoot");
        ConfigureSafeArea(
            onboardingSafeRoot, (RectTransform)canvas.transform);

        onboardingStarsImage = EnsureImage(
            onboardingSafeRoot, "OnboardingStars");
        ConfigureImage(onboardingStarsImage, stars, false);
        Place(onboardingStarsImage.rectTransform, Vector2.zero,
            new Vector2(ReferenceWidth, ReferenceHeight));

        onboardingConfettiImage = EnsureImage(
            onboardingSafeRoot, "OnboardingConfetti");
        ConfigureImage(onboardingConfettiImage, confetti, false);
        Place(onboardingConfettiImage.rectTransform, Vector2.zero,
            new Vector2(ReferenceWidth, ReferenceHeight));
        onboardingConfettiImage.color = new Color(1f, 1f, 1f, 0.68f);

        onboardingController =
            GetComponent<SplashOnboardingController>();
        if (onboardingController == null)
            onboardingController =
                gameObject.AddComponent<SplashOnboardingController>();
        onboardingController.Initialize(FindInScene<SplashLoader>());

        BuildWelcomeScreen(
            logo, welcomeEnsemble, mascotSix, mascotSeven, stars);
        BuildNameScreen(welcomeEnsemble, nameInputIcon, stars);
        BuildGenderScreen(genderBoy, genderGirl, genderOther, stars);
        BuildAvatarScreen(avatars, stars);
        BuildAgeScreen(
            ageUnder13Mascot, ageTeenMascot, ageAdultMascot, privacyIcon,
            ageUnder13Icon, ageTeenIcon, ageAdultIcon, stars);

        onboardingController.StateChanged += RefreshOnboardingState;
        RefreshOnboardingState();
        ApplyOnboardingResponsiveLayout(true);

        IsOnboardingVisible = true;
        IsReady = true;
        IsSettled = true;
    }

    void BuildWelcomeScreen(
        Sprite logo,
        Sprite welcomeEnsemble,
        Sprite mascotSix,
        Sprite mascotSeven,
        Sprite stars)
    {
        RectTransform screen = CreateOnboardingScreen(
            SplashOnboardingController.Step.Welcome,
            "OnboardingWelcomeScreen");
        RectTransform header = onboardingHeaderGroups[0];
        RectTransform content = onboardingContentGroups[0];
        RectTransform footer = onboardingFooterGroups[0];

        Image leftAccents = CreateSprite(
            content, "WelcomeStarsLeft", stars,
            new Vector2(-430f, 190f), new Vector2(430f, 760f), true);
        leftAccents.color = new Color(1f, 1f, 1f, 0.72f);
        Image rightAccents = CreateSprite(
            content, "WelcomeStarsRight", stars,
            new Vector2(430f, 190f), new Vector2(430f, 760f), true);
        rightAccents.color = new Color(1f, 1f, 1f, 0.72f);
        rightAccents.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

        CreateSprite(
            header, "WelcomeLogo", logo,
            new Vector2(0f, 690f), new Vector2(540f, 360f), true);
        BuildOnboardingProgress(header, 1, 870f);

        CreateSprite(
            content, "WelcomeHumanEnsemble", welcomeEnsemble,
            new Vector2(0f, 80f), new Vector2(1000f, 914f), true);
        CreateSprite(
            content, "WelcomeMascotSix", mascotSix,
            new Vector2(-395f, -405f), new Vector2(265f, 300f), true);
        CreateSprite(
            content, "WelcomeMascotSeven", mascotSeven,
            new Vector2(395f, -405f), new Vector2(260f, 300f), true);

        CreateLocalizedText(
            content, "WelcomeHeading", "onboarding_welcome_title",
            58f, new Vector2(0f, -475f), new Vector2(900f, 260f),
            onboardingDisplayFont, OnboardingWhite,
            TextAlignmentOptions.Center, ResponsiveTextRole.Heading);
        CreateLocalizedText(
            content, "WelcomeBody", "onboarding_welcome_body",
            35f, new Vector2(0f, -660f), new Vector2(820f, 125f),
            onboardingBodyFont, OnboardingWhite,
            TextAlignmentOptions.Center, ResponsiveTextRole.Body);

        onboardingContinueButtons[0] = CreateCta(
            footer, "WelcomeContinue", "onboarding_go",
            new Vector2(0f, -825f), new Vector2(780f, 180f),
            () => onboardingController.Advance());
    }

    void BuildNameScreen(Sprite neutralEnsemble, Sprite inputIcon, Sprite stars)
    {
        RectTransform screen = CreateOnboardingScreen(
            SplashOnboardingController.Step.Name,
            "OnboardingNameScreen");
        RectTransform header = onboardingHeaderGroups[1];
        RectTransform content = onboardingContentGroups[1];
        RectTransform footer = onboardingFooterGroups[1];

        Image dimmer = EnsureImage(screen, "NameBackgroundDimmer");
        ConfigureImage(dimmer, RuntimeUI.SolidSprite, false);
        Stretch(dimmer.rectTransform);
        dimmer.color = new Color(0.015f, 0.008f, 0.08f, 0.32f);
        dimmer.raycastTarget = false;
        dimmer.transform.SetAsFirstSibling();

        BuildOnboardingHeader(
            header, 2, "onboarding_name_title",
            "onboarding_name_subtitle");

        Image leftAccents = CreateSprite(
            content, "NameStarsLeft", stars,
            new Vector2(-430f, 190f), new Vector2(210f, 460f), true);
        leftAccents.color = new Color(1f, 1f, 1f, 0.42f);
        leftAccents.raycastTarget = false;
        Image rightAccents = CreateSprite(
            content, "NameStarsRight", stars,
            new Vector2(430f, 190f), new Vector2(210f, 460f), true);
        rightAccents.color = new Color(1f, 1f, 1f, 0.42f);
        rightAccents.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
        rightAccents.raycastTarget = false;

        CreateSprite(
            content, "NameNeutralEnsemble", neutralEnsemble,
            new Vector2(0f, 70f), new Vector2(640f, 585f), true);
        onboardingNameInput = CreateNameInput(
            content, inputIcon,
            new Vector2(0f, -330f), new Vector2(780f, 145f));
        onboardingNameInput.onValueChanged.AddListener(
            onboardingController.SetName);

        CreateLocalizedText(
            content, "NameHint", "onboarding_name_hint",
            26f, new Vector2(-260f, -440f), new Vector2(340f, 52f),
            onboardingBodyFont, OnboardingMuted,
            TextAlignmentOptions.Left, ResponsiveTextRole.Compact);
        onboardingNameCounter = CreateText(
            content, "NameCounter", "0 / 12", 26f,
            new Vector2(300f, -440f), new Vector2(220f, 52f),
            onboardingBodyFont, OnboardingWhite,
            TextAlignmentOptions.Right, ResponsiveTextRole.Compact);

        onboardingContinueButtons[1] = CreateCta(
            footer, "NameContinue", "onboarding_continue",
            new Vector2(0f, -780f), OnboardingContinueCtaSize,
            () => onboardingController.Advance());
    }

    void ApplyNameReferenceHeader(RectTransform header)
    {
        Image line = header.Find("OnboardingProgressLine")?.GetComponent<Image>();
        if (line != null)
        {
            Place(line.rectTransform, new Vector2(0f, 845f),
                new Vector2(430f, 5f));
            line.color = new Color(0.36f, 0.18f, 0.58f, 0.92f);
        }

        for (int index = 1; index <= 5; index++)
        {
            RectTransform node = header.Find(
                "OnboardingProgressNode" + index) as RectTransform;
            if (node == null) continue;
            Place(node, new Vector2(-200f + (index - 1) * 100f, 845f),
                new Vector2(50f, 50f));

            Image disc = node.Find("ProgressDisc")?.GetComponent<Image>();
            bool active = index == 1;
            if (disc != null)
            {
                Place(disc.rectTransform, Vector2.zero,
                    new Vector2(42f, 42f));
                disc.color = active ? OnboardingMagenta : OnboardingInk;
                Outline outline = disc.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = active
                        ? OnboardingWhite
                        : new Color(OnboardingGold.r, OnboardingGold.g,
                            OnboardingGold.b, 0.72f);
                    outline.effectDistance = new Vector2(1.5f, -1.5f);
                }
            }

            TMP_Text number = node.Find("ProgressNumber")
                ?.GetComponent<TMP_Text>();
            if (number != null)
            {
                number.enableAutoSizing = false;
                number.fontSize = 20f;
                number.color = active ? OnboardingWhite : OnboardingMuted;
                Place(number.rectTransform, Vector2.zero,
                    new Vector2(36f, 36f));
            }
        }

        TMP_Text title = header.Find("OnboardingTitle")
            ?.GetComponent<TMP_Text>();
        if (title != null)
        {
            title.enableAutoSizing = false;
            title.fontSize = 50f;
            Place(title.rectTransform, new Vector2(0f, 720f),
                new Vector2(820f, 72f));
        }

        TMP_Text subtitle = header.Find("OnboardingSubtitle")
            ?.GetComponent<TMP_Text>();
        if (subtitle != null)
        {
            subtitle.enableAutoSizing = false;
            subtitle.fontSize = 25f;
            Place(subtitle.rectTransform, new Vector2(0f, 640f),
                new Vector2(650f, 90f));
        }
    }

    void BuildGenderScreen(
        Sprite boy, Sprite girl, Sprite other, Sprite stars)
    {
        RectTransform screen = CreateOnboardingScreen(
            SplashOnboardingController.Step.Gender,
            "OnboardingGenderScreen");
        RectTransform header = onboardingHeaderGroups[2];
        RectTransform content = onboardingContentGroups[2];
        RectTransform footer = onboardingFooterGroups[2];

        // Screen 2 deliberately uses a quieter field than the other setup
        // screens. Keep the shared arcade background, but suppress its visual
        // weight behind this compact, control-first composition.
        Image dimmer = EnsureImage(screen, "GenderBackgroundDimmer");
        ConfigureImage(dimmer, RuntimeUI.SolidSprite, false);
        Stretch(dimmer.rectTransform);
        dimmer.color = new Color(0.015f, 0.008f, 0.08f, 0.28f);
        dimmer.raycastTarget = false;
        dimmer.transform.SetAsFirstSibling();

        BuildOnboardingHeader(
            header, 3, "onboarding_gender_title",
            "onboarding_gender_subtitle");

        Image leftAccents = CreateSprite(
            content, "GenderStarsLeft", stars,
            new Vector2(-430f, 360f), new Vector2(230f, 430f), true);
        leftAccents.color = new Color(1f, 1f, 1f, 0.44f);
        leftAccents.raycastTarget = false;
        Image rightAccents = CreateSprite(
            content, "GenderStarsRight", stars,
            new Vector2(430f, 360f), new Vector2(230f, 430f), true);
        rightAccents.color = new Color(1f, 1f, 1f, 0.44f);
        rightAccents.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
        rightAccents.raycastTarget = false;

        // One deterministic container owns the complete three-card group.
        // This keeps equal card bounds, gaps and outer margins by construction.
        RectTransform cardGroup = EnsureRect(content, "GenderCards");
        Place(cardGroup, new Vector2(0f, 90f),
            new Vector2(ReferenceWidth, ReferenceHeight));

        Sprite[] frames = { onboardingBlue, onboardingMagenta, onboardingPanel };
        Sprite[] art = { boy, girl, other };
        string[] labels =
        {
            "onboarding_gender_boy",
            "onboarding_gender_girl",
            "onboarding_gender_other",
        };
        float[] xs = { -292f, 0f, 292f };
        for (int index = 0; index < 3; index++)
        {
            int captured = index;
            Button card = CreateSelectionCard(
                cardGroup, "GenderCard" + index, frames[index], art[index],
                labels[index], new Vector2(xs[index], 0f),
                new Vector2(270f, 690f),
                () => onboardingController.SelectGender(captured),
                out genderCardImages[index],
                out genderSelectionBadges[index],
                out genderSelectionOutlines[index]);
            if (index == 2)
            {
                CreateLocalizedText(
                    card.transform, "GenderOtherHint",
                    "onboarding_gender_other_hint", 21f,
                    new Vector2(0f, -245f), new Vector2(220f, 92f),
                    onboardingBodyFont, OnboardingWhite,
                    TextAlignmentOptions.Center,
                    ResponsiveTextRole.Compact);
            }
        }

        onboardingContinueButtons[2] = CreateCta(
            footer, "GenderContinue", "onboarding_continue",
            new Vector2(0f, -780f), OnboardingContinueCtaSize,
            () => onboardingController.Advance());
    }

    void ApplyGenderReferenceHeader(RectTransform header)
    {
        Image line = header.Find("OnboardingProgressLine")?.GetComponent<Image>();
        if (line != null)
        {
            Place(line.rectTransform, new Vector2(0f, 845f),
                new Vector2(430f, 5f));
            line.color = new Color(0.36f, 0.18f, 0.58f, 0.92f);
        }

        for (int index = 1; index <= 5; index++)
        {
            RectTransform node = header.Find(
                "OnboardingProgressNode" + index) as RectTransform;
            if (node == null) continue;
            Place(node, new Vector2(-200f + (index - 1) * 100f, 845f),
                new Vector2(50f, 50f));

            Image disc = node.Find("ProgressDisc")?.GetComponent<Image>();
            bool active = index == 2;
            if (disc != null)
            {
                Place(disc.rectTransform, Vector2.zero,
                    new Vector2(42f, 42f));
                disc.color = active ? OnboardingMagenta : OnboardingInk;
                Outline outline = disc.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = active
                        ? OnboardingWhite
                        : new Color(OnboardingGold.r, OnboardingGold.g,
                            OnboardingGold.b, 0.72f);
                    outline.effectDistance = new Vector2(1.5f, -1.5f);
                }
            }

            TMP_Text number = node.Find("ProgressNumber")
                ?.GetComponent<TMP_Text>();
            if (number != null)
            {
                number.enableAutoSizing = false;
                number.fontSize = 20f;
                number.color = active ? OnboardingWhite : OnboardingMuted;
                Place(number.rectTransform, Vector2.zero,
                    new Vector2(36f, 36f));
            }
        }

        TMP_Text title = header.Find("OnboardingTitle")
            ?.GetComponent<TMP_Text>();
        if (title != null)
        {
            title.enableAutoSizing = false;
            title.fontSize = 48f;
            Place(title.rectTransform, new Vector2(0f, 735f),
                new Vector2(800f, 70f));
        }

        TMP_Text subtitle = header.Find("OnboardingSubtitle")
            ?.GetComponent<TMP_Text>();
        if (subtitle != null)
        {
            subtitle.enableAutoSizing = false;
            subtitle.fontSize = 26f;
            Place(subtitle.rectTransform, new Vector2(0f, 665f),
                new Vector2(780f, 55f));
        }
    }

    void BuildAvatarScreen(Sprite[] avatars, Sprite stars)
    {
        RectTransform screen = CreateOnboardingScreen(
            SplashOnboardingController.Step.Avatar,
            "OnboardingAvatarScreen");
        RectTransform header = onboardingHeaderGroups[3];
        RectTransform content = onboardingContentGroups[3];
        RectTransform footer = onboardingFooterGroups[3];

        Image dimmer = EnsureImage(screen, "AvatarBackgroundDimmer");
        ConfigureImage(dimmer, RuntimeUI.SolidSprite, false);
        Stretch(dimmer.rectTransform);
        dimmer.color = new Color(0.015f, 0.008f, 0.08f, 0.32f);
        dimmer.raycastTarget = false;
        dimmer.transform.SetAsFirstSibling();

        BuildOnboardingHeader(
            header, 4, "onboarding_avatar_title",
            "onboarding_avatar_subtitle");

        Image leftAccents = CreateSprite(
            content, "AvatarStarsLeft", stars,
            new Vector2(-455f, 430f), new Vector2(190f, 360f), true);
        leftAccents.color = new Color(1f, 1f, 1f, 0.38f);
        leftAccents.raycastTarget = false;
        Image rightAccents = CreateSprite(
            content, "AvatarStarsRight", stars,
            new Vector2(455f, 430f), new Vector2(190f, 360f), true);
        rightAccents.color = new Color(1f, 1f, 1f, 0.38f);
        rightAccents.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
        rightAccents.raycastTarget = false;

        RectTransform filters = EnsureRect(content, "AvatarFilters");
        Place(filters, new Vector2(0f, 408f),
            new Vector2(ReferenceWidth, AvatarFilterSize.y));
        OnboardingAvatarCatalog.Category[] filterValues =
        {
            OnboardingAvatarCatalog.Category.All,
            OnboardingAvatarCatalog.Category.Boys,
            OnboardingAvatarCatalog.Category.Girls,
            OnboardingAvatarCatalog.Category.Cool,
            OnboardingAvatarCatalog.Category.Epic,
        };
        string[] filterKeys =
        {
            "onboarding_avatar_filter_all",
            "onboarding_avatar_filter_boys",
            "onboarding_avatar_filter_girls",
            "onboarding_avatar_filter_cool",
            "onboarding_avatar_filter_epic",
        };
        float[] filterXs = { -352f, -176f, 0f, 176f, 352f };
        for (int index = 0; index < filterValues.Length; index++)
        {
            OnboardingAvatarCatalog.Category captured = filterValues[index];
            avatarFilterImages[index] = CreateAvatarFilterButton(
                filters, "AvatarFilter" + index, filterKeys[index],
                new Vector2(filterXs[index], 0f),
                () => SetAvatarFilter(captured));
        }

        Image previewPanel = CreateProductionImage(
            content, "AvatarPreviewPanel", onboardingPanel,
            new Vector2(-326f, -20f), new Vector2(350f, 660f), false, true);
        previewPanel.pixelsPerUnitMultiplier = 6f;
        onboardingAvatarPreview = CreateSprite(
            previewPanel.transform, "AvatarSelectedPreview", avatars[0],
            new Vector2(0f, 62f), new Vector2(320f, 430f), true);
        onboardingAvatarPreview.gameObject.SetActive(false);
        onboardingAvatarPreviewPrompt = CreateLocalizedText(
            previewPanel.transform, "AvatarPreviewPrompt",
            "onboarding_avatar_choose_preview", 29f,
            new Vector2(0f, 45f), new Vector2(285f, 150f),
            onboardingDisplayFont, OnboardingWhite,
            TextAlignmentOptions.Center, ResponsiveTextRole.Action);
        onboardingAvatarStatus = CreateText(
            previewPanel.transform, "AvatarSelectedStatus", string.Empty, 22f,
            new Vector2(0f, -255f), new Vector2(300f, 78f),
            onboardingBodyFont, OnboardingCyan,
            TextAlignmentOptions.Center, ResponsiveTextRole.Compact);

        RectTransform grid = EnsureRect(content, "AvatarGrid");
        Place(grid, Vector2.zero,
            new Vector2(ReferenceWidth, ReferenceHeight));

        for (int index = 0; index < avatars.Length; index++)
        {
            int captured = index;
            Button card = CreateAvatarCard(
                grid, "AvatarCard" + (index + 1), avatars[index],
                AvatarGridPosition(index), AvatarCardSize,
                () => onboardingController.SelectAvatar(captured),
                out avatarCardImages[index],
                out avatarSelectionBadges[index],
                out avatarSelectionOutlines[index]);
            avatarCardButtons[index] = card;
            avatarCardRects[index] = card.GetComponent<RectTransform>();
            avatarAvailabilityLabels[index] = CreateText(
                card.transform, "Availability", GetAvatarAvailabilityLabel(index),
                24f, new Vector2(0f, -79f), new Vector2(168f, 46f),
                onboardingDisplayFont, OnboardingGold,
                TextAlignmentOptions.Center, ResponsiveTextRole.Compact);
            avatarAvailabilityLabels[index].fontSizeMin = 20f;
            card.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        }

        onboardingContinueButtons[3] = CreateCta(
            footer, "AvatarContinue", "onboarding_continue",
            new Vector2(0f, -780f), OnboardingContinueCtaSize,
            () => onboardingController.Advance());
    }

    void ApplyAvatarReferenceHeader(RectTransform header)
    {
        Image line = header.Find("OnboardingProgressLine")?.GetComponent<Image>();
        if (line != null)
        {
            Place(line.rectTransform, new Vector2(0f, 845f),
                new Vector2(430f, 5f));
            line.color = new Color(0.36f, 0.18f, 0.58f, 0.92f);
        }

        for (int index = 1; index <= 5; index++)
        {
            RectTransform node = header.Find(
                "OnboardingProgressNode" + index) as RectTransform;
            if (node == null) continue;
            Place(node, new Vector2(-200f + (index - 1) * 100f, 845f),
                new Vector2(50f, 50f));

            Image disc = node.Find("ProgressDisc")?.GetComponent<Image>();
            bool active = index == 3;
            if (disc != null)
            {
                Place(disc.rectTransform, Vector2.zero,
                    new Vector2(42f, 42f));
                disc.color = active ? OnboardingMagenta : OnboardingInk;
                Outline outline = disc.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = active
                        ? OnboardingWhite
                        : new Color(OnboardingGold.r, OnboardingGold.g,
                            OnboardingGold.b, 0.72f);
                    outline.effectDistance = new Vector2(1.5f, -1.5f);
                }
            }

            TMP_Text number = node.Find("ProgressNumber")
                ?.GetComponent<TMP_Text>();
            if (number != null)
            {
                number.enableAutoSizing = false;
                number.fontSize = 20f;
                number.color = active ? OnboardingWhite : OnboardingMuted;
                Place(number.rectTransform, Vector2.zero,
                    new Vector2(36f, 36f));
            }
        }

        TMP_Text title = header.Find("OnboardingTitle")
            ?.GetComponent<TMP_Text>();
        if (title != null)
        {
            title.enableAutoSizing = false;
            title.fontSize = 46f;
            Place(title.rectTransform, new Vector2(0f, 735f),
                new Vector2(800f, 68f));
        }

        TMP_Text subtitle = header.Find("OnboardingSubtitle")
            ?.GetComponent<TMP_Text>();
        if (subtitle != null)
        {
            subtitle.enableAutoSizing = false;
            subtitle.fontSize = 25f;
            Place(subtitle.rectTransform, new Vector2(0f, 670f),
                new Vector2(780f, 52f));
        }
    }

    void BuildAgeScreen(
        Sprite ageUnder13Mascot,
        Sprite ageTeenMascot,
        Sprite ageAdultMascot,
        Sprite privacyIcon,
        Sprite ageUnder13Icon,
        Sprite ageTeenIcon,
        Sprite ageAdultIcon,
        Sprite stars)
    {
        RectTransform screen = CreateOnboardingScreen(
            SplashOnboardingController.Step.Age,
            "OnboardingAgeScreen");
        RectTransform header = onboardingHeaderGroups[4];
        RectTransform content = onboardingContentGroups[4];
        RectTransform footer = onboardingFooterGroups[4];

        Image dimmer = EnsureImage(screen, "AgeBackgroundDimmer");
        ConfigureImage(dimmer, RuntimeUI.SolidSprite, false);
        Stretch(dimmer.rectTransform);
        dimmer.color = new Color(0.015f, 0.008f, 0.08f, 0.34f);
        dimmer.raycastTarget = false;
        dimmer.transform.SetAsFirstSibling();

        BuildOnboardingHeader(
            header, 5, "onboarding_age_title",
            "onboarding_age_subtitle");

        Image leftAccents = CreateSprite(
            content, "AgeStarsLeft", stars,
            new Vector2(-470f, 430f), new Vector2(170f, 340f), true);
        leftAccents.color = new Color(1f, 1f, 1f, 0.34f);
        leftAccents.raycastTarget = false;
        Image rightAccents = CreateSprite(
            content, "AgeStarsRight", stars,
            new Vector2(470f, 430f), new Vector2(170f, 340f), true);
        rightAccents.color = new Color(1f, 1f, 1f, 0.34f);
        rightAccents.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
        rightAccents.raycastTarget = false;

        RectTransform cardGroup = EnsureRect(content, "AgeCards");
        Place(cardGroup, new Vector2(0f, -5f),
            new Vector2(ReferenceWidth, ReferenceHeight));

        Sprite[] frames = { onboardingGold, onboardingBlue, onboardingMagenta };
        Sprite[] mascots =
            { ageUnder13Mascot, ageTeenMascot, ageAdultMascot };
        Sprite[] leadingIcons =
            { ageUnder13Icon, ageTeenIcon, ageAdultIcon };
        string[] labels =
        {
            "onboarding_age_under13",
            "onboarding_age_teen",
            "onboarding_age_adult",
        };
        float[] ys = { 230f, -20f, -270f };
        for (int index = 0; index < 3; index++)
        {
            int captured = index;
            CreateAgeCard(
                cardGroup, "AgeCard" + index, frames[index],
                leadingIcons[index], mascots[index], labels[index],
                new Vector2(0f, ys[index]),
                new Vector2(850f, 180f),
                () => onboardingController.SelectAge(captured),
                out ageCardImages[index], out ageSelectionBadges[index],
                out ageSelectionOutlines[index]);
        }

        Image privacyPanel = CreateProductionImage(
            content, "AgePrivacyPanel", onboardingPanel,
            new Vector2(0f, -535f), new Vector2(900f, 170f), false, true);
        CreateSprite(
            privacyPanel.transform, "AgePrivacyIcon", privacyIcon,
            new Vector2(-385f, 0f), new Vector2(82f, 82f), true);
        TMP_Text privacyText = CreateLocalizedText(
            privacyPanel.transform, "AgePrivacyText",
            "onboarding_age_privacy", 28f,
            new Vector2(34f, 2f), new Vector2(760f, 124f),
            onboardingBodyFont, OnboardingWhite,
            TextAlignmentOptions.Center, ResponsiveTextRole.Compact);
        privacyText.fontSizeMin = 23f;

        onboardingContinueButtons[4] = CreateCta(
            footer, "AgeContinue", "onboarding_continue",
            new Vector2(0f, -810f), OnboardingContinueCtaSize,
            () => onboardingController.Advance());
    }

    void ApplyAgeReferenceHeader(RectTransform header)
    {
        Image line = header.Find("OnboardingProgressLine")?.GetComponent<Image>();
        if (line != null)
        {
            Place(line.rectTransform, new Vector2(0f, 845f),
                new Vector2(430f, 5f));
            line.color = new Color(0.36f, 0.18f, 0.58f, 0.92f);
        }

        for (int index = 1; index <= 5; index++)
        {
            RectTransform node = header.Find(
                "OnboardingProgressNode" + index) as RectTransform;
            if (node == null) continue;
            Place(node, new Vector2(-200f + (index - 1) * 100f, 845f),
                new Vector2(50f, 50f));

            Image disc = node.Find("ProgressDisc")?.GetComponent<Image>();
            bool active = index == 4;
            if (disc != null)
            {
                Place(disc.rectTransform, Vector2.zero,
                    new Vector2(42f, 42f));
                disc.color = active ? OnboardingMagenta : OnboardingInk;
                Outline outline = disc.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = active
                        ? OnboardingWhite
                        : new Color(OnboardingGold.r, OnboardingGold.g,
                            OnboardingGold.b, 0.72f);
                    outline.effectDistance = new Vector2(1.5f, -1.5f);
                }
            }

            TMP_Text number = node.Find("ProgressNumber")
                ?.GetComponent<TMP_Text>();
            if (number != null)
            {
                number.enableAutoSizing = false;
                number.fontSize = 20f;
                number.color = active ? OnboardingWhite : OnboardingMuted;
                Place(number.rectTransform, Vector2.zero,
                    new Vector2(36f, 36f));
            }
        }

        TMP_Text title = header.Find("OnboardingTitle")
            ?.GetComponent<TMP_Text>();
        if (title != null)
        {
            title.enableAutoSizing = false;
            title.fontSize = 43f;
            Place(title.rectTransform, new Vector2(0f, 720f),
                new Vector2(900f, 118f));
        }

        TMP_Text subtitle = header.Find("OnboardingSubtitle")
            ?.GetComponent<TMP_Text>();
        if (subtitle != null)
        {
            subtitle.enableAutoSizing = false;
            subtitle.fontSize = 24f;
            Place(subtitle.rectTransform, new Vector2(0f, 615f),
                new Vector2(760f, 84f));
        }
    }

    RectTransform CreateOnboardingScreen(
        SplashOnboardingController.Step step, string name)
    {
        int index = (int)step;
        RectTransform screen = EnsureRect(onboardingSafeRoot, name);
        Stretch(screen);
        onboardingScreens[index] = screen;
        onboardingHeaderGroups[index] = CreateReferenceGroup(
            screen, name + "Header");
        onboardingContentGroups[index] = CreateReferenceGroup(
            screen, name + "Content");
        onboardingFooterGroups[index] = CreateReferenceGroup(
            screen, name + "Footer");
        return screen;
    }

    RectTransform CreateReferenceGroup(Transform parent, string name)
    {
        RectTransform group = EnsureRect(parent, name);
        Place(group, Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight));
        return group;
    }

    void BuildOnboardingHeader(
        Transform parent, int activeStep, string titleKey, string subtitleKey)
    {
        Image backImage = CreateSprite(
            parent, "OnboardingBack", onboardingBack,
            new Vector2(-455f, 825f), new Vector2(105f, 105f), true);
        backImage.raycastTarget = true;
        Button backButton = backImage.gameObject.AddComponent<Button>();
        backButton.targetGraphic = backImage;
        backButton.onClick.AddListener(() => onboardingController.GoBack());
        RuntimeUI.AttachJuice(backButton);

        CreateSprite(
            parent, "OnboardingHeaderLogo", onboardingLogo,
            new Vector2(0f, 825f), new Vector2(245f, 155f), true);

        if (activeStep == 3)
        {
            Image skipHitArea = CreateProductionImage(
                parent, "OnboardingGenderSkip", RuntimeUI.SolidSprite,
                new Vector2(430f, 825f), new Vector2(190f, 82f),
                false, false);
            skipHitArea.color = new Color(0.08f, 0.03f, 0.17f, 0.58f);
            skipHitArea.raycastTarget = true;
            Button skipButton = skipHitArea.gameObject.AddComponent<Button>();
            skipButton.targetGraphic = skipHitArea;
            skipButton.onClick.AddListener(
                () => onboardingController.SkipGender());
            RuntimeUI.AttachJuice(skipButton);
            CreateLocalizedText(
                skipButton.transform, "Label", "onboarding_skip", 27f,
                Vector2.zero, new Vector2(170f, 64f),
                onboardingDisplayFont, OnboardingWhite,
                TextAlignmentOptions.Center, ResponsiveTextRole.Action);
        }

        BuildOnboardingProgress(parent, activeStep, 700f);

        bool ageStep = activeStep == 5;
        CreateLocalizedText(
            parent, "OnboardingTitle", titleKey, ageStep ? 46f : 52f,
            new Vector2(0f, ageStep ? 575f : 590f),
            new Vector2(940f, ageStep ? 120f : 78f),
            onboardingDisplayFont, OnboardingWhite,
            TextAlignmentOptions.Center, ResponsiveTextRole.Heading);
        TMP_Text subtitle = CreateLocalizedText(
            parent, "OnboardingSubtitle", subtitleKey, ageStep ? 26f : 28f,
            new Vector2(0f, ageStep ? 475f : 510f),
            new Vector2(900f, ageStep ? 94f : 66f),
            onboardingBodyFont, OnboardingWhite,
            TextAlignmentOptions.Center, ResponsiveTextRole.Body);
        if (ageStep)
        {
            subtitle.fontSizeMax = 29f;
            subtitle.fontSize = 29f;
            subtitle.fontSizeMin = 23f;
        }
    }

    void BuildOnboardingProgress(
        Transform parent, int activeStep, float y)
    {
        Image line = EnsureImage(parent, "OnboardingProgressLine");
        ConfigureImage(line, RuntimeUI.SolidSprite, false);
        line.color = new Color(0.36f, 0.18f, 0.58f, 1f);
        Place(line.rectTransform, new Vector2(0f, y),
            new Vector2(430f, 6f));

        for (int index = 1; index <= 5; index++)
        {
            bool completed = index < activeStep;
            bool active = index == activeStep;
            RectTransform node = EnsureRect(
                parent, "OnboardingProgressNode" + index);
            Place(node, new Vector2(-200f + (index - 1) * 100f, y),
                new Vector2(54f, 54f));
            Image disc = CreateSprite(
                node, "ProgressDisc", onboardingCircle,
                Vector2.zero, new Vector2(48f, 48f), true);
            disc.color = active
                ? OnboardingMagenta
                : completed ? OnboardingCyan : OnboardingInk;
            Outline outline = disc.gameObject.AddComponent<Outline>();
            outline.effectColor = active
                ? OnboardingWhite
                : completed ? OnboardingCyan : OnboardingGold;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            CreateText(
                node, "ProgressNumber", index.ToString(), 21f,
                Vector2.zero, new Vector2(42f, 42f), onboardingDisplayFont,
                active || completed ? OnboardingWhite : OnboardingMuted,
                TextAlignmentOptions.Center,
                ResponsiveTextRole.Compact);
        }
    }

    TMP_InputField CreateNameInput(
        Transform parent, Sprite inputIcon, Vector2 position, Vector2 size)
    {
        Image shell = CreateProductionImage(
            parent, "OnboardingNameInput", onboardingPanel,
            position, size, false, true);
        TMP_InputField input = shell.GetComponent<TMP_InputField>();
        if (input == null) input = shell.gameObject.AddComponent<TMP_InputField>();
        input.targetGraphic = shell;
        input.characterLimit = OnboardingProfile.MaxNameLength;
        input.contentType = TMP_InputField.ContentType.Standard;
        input.lineType = TMP_InputField.LineType.SingleLine;

        RectTransform viewport = EnsureRect(shell.transform, "Text Area");
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(126f, 15f);
        viewport.offsetMax = new Vector2(-32f, -15f);
        if (viewport.GetComponent<RectMask2D>() == null)
            viewport.gameObject.AddComponent<RectMask2D>();
        input.textViewport = viewport;

        TMP_Text value = CreateText(
            viewport, "Text", string.Empty, 43f,
            Vector2.zero, Vector2.zero, onboardingBodyFont,
            OnboardingWhite, TextAlignmentOptions.Left,
            ResponsiveTextRole.Input);
        Stretch(value.rectTransform);
        value.margin = new Vector4(8f, 0f, 8f, 0f);
        input.textComponent = value;

        TMP_Text placeholder = CreateLocalizedText(
            viewport, "Placeholder", "onboarding_name_placeholder", 40f,
            Vector2.zero, Vector2.zero, onboardingBodyFont,
            OnboardingMuted, TextAlignmentOptions.Left,
            ResponsiveTextRole.Input);
        Stretch(placeholder.rectTransform);
        placeholder.margin = new Vector4(8f, 0f, 8f, 0f);
        input.placeholder = placeholder;

        Image icon = CreateSprite(
            shell.transform, "NameInputIcon", inputIcon,
            new Vector2(-320f, 0f), new Vector2(72f, 72f), true);
        icon.raycastTarget = false;
        return input;
    }

    Button CreateCta(
        Transform parent,
        string name,
        string labelKey,
        Vector2 position,
        Vector2 size,
        Action callback)
    {
        Image image = CreateProductionImage(
            parent, name, onboardingGold, position, size, false, true);
        image.raycastTarget = true;
        Button button = image.GetComponent<Button>();
        if (button == null) button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.RemoveAllListeners();
        if (callback != null) button.onClick.AddListener(() => callback());
        RuntimeUI.AttachJuice(button);
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        colors.disabledColor = new Color(0.30f, 0.30f, 0.38f, 1f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        CreateLocalizedText(
            button.transform, "Label", labelKey, 55f,
            new Vector2(-20f, 3f), new Vector2(size.x - 180f, size.y - 40f),
            onboardingDisplayFont, OnboardingInk,
            TextAlignmentOptions.Center, ResponsiveTextRole.Action);
        CreateText(
            button.transform, "Arrow", "→", 56f,
            new Vector2(size.x * 0.37f, 3f), new Vector2(90f, 90f),
            onboardingDisplayFont, OnboardingInk,
            TextAlignmentOptions.Center, ResponsiveTextRole.Action);
        return button;
    }

    Image CreateAvatarFilterButton(
        Transform parent,
        string name,
        string labelKey,
        Vector2 position,
        Action callback)
    {
        Image image = CreateProductionImage(
            parent, name, onboardingPanel, position,
            AvatarFilterSize, false, true);
        image.pixelsPerUnitMultiplier = 8f;
        image.raycastTarget = true;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => callback());
        RuntimeUI.AttachJuice(button);
        TMP_Text label = CreateLocalizedText(
            button.transform, "Label", labelKey, 24f,
            Vector2.zero, new Vector2(154f, 62f),
            onboardingDisplayFont, OnboardingWhite,
            TextAlignmentOptions.Center, ResponsiveTextRole.Action);
        label.fontSizeMin = 20f;
        return image;
    }

    static Vector2 AvatarGridPosition(int visibleIndex)
    {
        int row = visibleIndex / 3;
        int column = visibleIndex % 3;
        return new Vector2(58f + column * 190f, 245f - row * 212f);
    }

    void SetAvatarFilter(OnboardingAvatarCatalog.Category filter)
    {
        if (onboardingAvatarFilter == filter) return;
        onboardingAvatarFilter = filter;
        ApplyAvatarFilter();
    }

    void ApplyAvatarFilter()
    {
        int visibleIndex = 0;
        for (int index = 0; index < avatarCardRects.Length; index++)
        {
            RectTransform card = avatarCardRects[index];
            if (card == null) continue;
            bool visible = OnboardingAvatarCatalog.Get(index)
                .Matches(onboardingAvatarFilter);
            card.gameObject.SetActive(visible);
            if (visible)
            {
                Place(card, AvatarGridPosition(visibleIndex),
                    AvatarCardSize);
                visibleIndex++;
            }
        }

        OnboardingAvatarCatalog.Category[] values =
        {
            OnboardingAvatarCatalog.Category.All,
            OnboardingAvatarCatalog.Category.Boys,
            OnboardingAvatarCatalog.Category.Girls,
            OnboardingAvatarCatalog.Category.Cool,
            OnboardingAvatarCatalog.Category.Epic,
        };
        for (int index = 0; index < avatarFilterImages.Length; index++)
            if (avatarFilterImages[index] != null)
                avatarFilterImages[index].color =
                    values[index] == onboardingAvatarFilter
                        ? OnboardingMagenta
                        : new Color(0.70f, 0.70f, 0.82f, 1f);
    }

    static string GetAvatarAvailabilityLabel(int index)
    {
        OnboardingAvatarCatalog.Entry entry =
            OnboardingAvatarCatalog.Get(index);
        switch (entry.Availability)
        {
            case OnboardingAvatarCatalog.AvailabilityKind.Free:
                return L10n.Get("onboarding_avatar_free");
            case OnboardingAvatarCatalog.AvailabilityKind.Coins:
                return L10n.Get("onboarding_avatar_coins", entry.Requirement);
            case OnboardingAvatarCatalog.AvailabilityKind.Experience:
                return L10n.Get("onboarding_avatar_xp", entry.Requirement);
            default:
                return L10n.Get("onboarding_avatar_locked");
        }
    }

    Button CreateSelectionCard(
        Transform parent,
        string name,
        Sprite frame,
        Sprite art,
        string labelKey,
        Vector2 position,
        Vector2 size,
        Action callback,
        out Image cardImage,
        out GameObject selectionBadge,
        out Outline selectionOutline)
    {
        cardImage = CreateProductionImage(
            parent, name, frame, position, size, false, true);
        cardImage.raycastTarget = true;
        Button button = cardImage.gameObject.AddComponent<Button>();
        button.targetGraphic = cardImage;
        button.onClick.AddListener(() => callback());
        RuntimeUI.AttachJuice(button);

        CreateSprite(
            button.transform, "Character", art,
            new Vector2(0f, 115f),
            new Vector2(280f, 280f), true);
        CreateLocalizedText(
            button.transform, "Label", labelKey, 34f,
            new Vector2(0f, -145f),
            new Vector2(size.x - 36f, 70f),
            onboardingDisplayFont, OnboardingWhite,
            TextAlignmentOptions.Center, ResponsiveTextRole.Action);
        selectionBadge = CreateSelectionBadge(
            button.transform,
            new Vector2(size.x * 0.30f, size.y * 0.36f), 72f);
        selectionOutline = CreateSelectionOutline(cardImage, 7f);
        return button;
    }

    Button CreateAvatarCard(
        Transform parent,
        string name,
        Sprite avatar,
        Vector2 position,
        Vector2 size,
        Action callback,
        out Image cardImage,
        out GameObject selectionBadge,
        out Outline selectionOutline)
    {
        cardImage = CreateProductionImage(
            parent, name, onboardingPanel, position, size, false, true);
        // Avatar tiles are much smaller than the large landscape panels this
        // production sprite normally serves. A higher local PPU keeps all four
        // borders and a real dark center instead of collapsing the slice into
        // the partial neon cap seen in the rejected runtime capture.
        cardImage.pixelsPerUnitMultiplier = 10f;
        cardImage.raycastTarget = true;
        Button button = cardImage.gameObject.AddComponent<Button>();
        button.targetGraphic = cardImage;
        button.onClick.AddListener(() => callback());
        RuntimeUI.AttachJuice(button);
        RectTransform viewport = EnsureRect(button.transform, "PortraitViewport");
        Place(viewport, new Vector2(0f, 23f),
            new Vector2(size.x - 24f, size.y - 66f));
        RectMask2D mask = viewport.GetComponent<RectMask2D>();
        if (mask == null) mask = viewport.gameObject.AddComponent<RectMask2D>();
        mask.padding = new Vector4(2f, 2f, 2f, 2f);
        CreateSprite(
            viewport, "Portrait", avatar,
            Vector2.zero, new Vector2(size.x - 30f, size.y - 70f),
            true);
        selectionBadge = CreateSelectionBadge(
            button.transform,
            new Vector2(size.x * 0.32f, size.y * 0.34f), 56f);
        selectionOutline = CreateSelectionOutline(cardImage, 5f);
        return button;
    }

    Button CreateAgeCard(
        Transform parent,
        string name,
        Sprite frame,
        Sprite leadingIcon,
        Sprite mascot,
        string labelKey,
        Vector2 position,
        Vector2 size,
        Action callback,
        out Image cardImage,
        out GameObject selectionBadge,
        out Outline selectionOutline)
    {
        cardImage = CreateProductionImage(
            parent, name, frame, position, size, false, true);
        cardImage.raycastTarget = true;
        Button button = cardImage.gameObject.AddComponent<Button>();
        button.targetGraphic = cardImage;
        button.onClick.AddListener(() => callback());
        RuntimeUI.AttachJuice(button);
        CreateSprite(
            button.transform, "CategoryIcon", leadingIcon,
            new Vector2(-335f, 0f), new Vector2(122f, 122f), true);
        CreateLocalizedText(
            button.transform, "Label", labelKey, 43f,
            Vector2.zero, new Vector2(520f, 90f),
            onboardingDisplayFont, OnboardingWhite,
            TextAlignmentOptions.Center, ResponsiveTextRole.Action);
        CreateSprite(
            button.transform, "Mascot", mascot,
            new Vector2(340f, 4f), new Vector2(137f, 137f), true);
        selectionBadge = CreateSelectionBadge(
            button.transform, new Vector2(315f, 0f), 70f);
        selectionOutline = CreateSelectionOutline(cardImage, 7f);
        return button;
    }

    static Outline CreateSelectionOutline(Image image, float distance)
    {
        Outline outline = image.GetComponent<Outline>();
        if (outline == null) outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.clear;
        outline.effectDistance = new Vector2(distance, -distance);
        outline.useGraphicAlpha = true;
        outline.enabled = false;
        return outline;
    }

    GameObject CreateSelectionBadge(
        Transform parent, Vector2 position, float diameter = 60f)
    {
        float scale = diameter / 60f;
        RectTransform badge = EnsureRect(parent, "SelectedBadge");
        Place(badge, position, new Vector2(diameter, diameter));
        Image disc = CreateSprite(
            badge, "Disc", onboardingCircle,
            Vector2.zero, new Vector2(diameter, diameter), true);
        disc.color = OnboardingCyan;
        Outline outline = disc.gameObject.AddComponent<Outline>();
        outline.effectColor = OnboardingMagenta;
        outline.effectDistance = new Vector2(3f * scale, -3f * scale);

        Image shortStroke = EnsureImage(badge, "CheckShort");
        ConfigureImage(shortStroke, RuntimeUI.SolidSprite, false);
        shortStroke.color = OnboardingWhite;
        Place(shortStroke.rectTransform,
            new Vector2(-8f, -3f) * scale,
            new Vector2(10f, 26f) * scale);
        shortStroke.rectTransform.localRotation =
            Quaternion.Euler(0f, 0f, 45f);

        Image longStroke = EnsureImage(badge, "CheckLong");
        ConfigureImage(longStroke, RuntimeUI.SolidSprite, false);
        longStroke.color = OnboardingWhite;
        Place(longStroke.rectTransform,
            new Vector2(8f, 4f) * scale,
            new Vector2(10f, 39f) * scale);
        longStroke.rectTransform.localRotation =
            Quaternion.Euler(0f, 0f, -45f);
        badge.gameObject.SetActive(false);
        return badge.gameObject;
    }

    Image CreateProductionImage(
        Transform parent,
        string name,
        Sprite sprite,
        Vector2 position,
        Vector2 size,
        bool preserveAspect,
        bool sliced)
    {
        Image image = EnsureImage(parent, name);
        ConfigureImage(image, sprite, preserveAspect);
        image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        // The Phase 2A production sprites carry deliberately generous
        // nine-slice borders (roughly 220-240px horizontally and 170-180px
        // vertically). A multiplier of two collapses the center region on
        // compact inputs/cards and causes the visible seams seen in the first
        // real runtime captures. Four preserves the authored bevel while
        // leaving a real scalable center surface at every approved size.
        image.pixelsPerUnitMultiplier = sliced ? 4f : 1f;
        Place(image.rectTransform, position, size);
        return image;
    }

    Image CreateSprite(
        Transform parent,
        string name,
        Sprite sprite,
        Vector2 position,
        Vector2 size,
        bool preserveAspect)
    {
        return CreateProductionImage(
            parent, name, sprite, position, size, preserveAspect, false);
    }

    TMP_Text CreateLocalizedText(
        Transform parent,
        string name,
        string key,
        float fontSize,
        Vector2 position,
        Vector2 size,
        TMP_FontAsset font,
        Color color,
        TextAlignmentOptions alignment,
        ResponsiveTextRole role)
    {
        TMP_Text text = CreateText(
            parent, name, L10n.Get(key), fontSize, position, size,
            font, color, alignment, role);
        LocalizedText localized = text.GetComponent<LocalizedText>();
        if (localized == null)
            localized = text.gameObject.AddComponent<LocalizedText>();
        localized.key = key;
        return text;
    }

    TMP_Text CreateText(
        Transform parent,
        string name,
        string value,
        float fontSize,
        Vector2 position,
        Vector2 size,
        TMP_FontAsset font,
        Color color,
        TextAlignmentOptions alignment,
        ResponsiveTextRole role)
    {
        RectTransform rect = EnsureRect(parent, name);
        TMP_Text text = rect.GetComponent<TextMeshProUGUI>();
        if (text == null) text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.outlineColor = OnboardingInk;
        text.outlineWidth = 0.16f;
        Place(rect, position, size);
        ResponsiveTextPolicy.Configure(text, role, fontSize);
        return text;
    }

    void RefreshOnboardingState()
    {
        if (onboardingController == null) return;
        int active = (int)onboardingController.CurrentStep;
        // The approved Welcome composition uses sparse, small accents. The
        // shared full-screen celebration sheets are intentionally suppressed
        // for the compact, reference-led states. Each has its own sparse
        // accents; Name and Age retain their established treatment.
        bool sparseDecorations =
            active == (int)SplashOnboardingController.Step.Welcome ||
            active == (int)SplashOnboardingController.Step.Name ||
            active == (int)SplashOnboardingController.Step.Gender ||
            active == (int)SplashOnboardingController.Step.Avatar ||
            active == (int)SplashOnboardingController.Step.Age;
        if (onboardingStarsImage != null)
        {
            onboardingStarsImage.gameObject.SetActive(!sparseDecorations);
            onboardingStarsImage.color = Color.white;
        }
        if (onboardingConfettiImage != null)
            onboardingConfettiImage.gameObject.SetActive(!sparseDecorations);
        for (int index = 0; index < onboardingScreens.Length; index++)
            if (onboardingScreens[index] != null)
                onboardingScreens[index].gameObject.SetActive(index == active);

        if (onboardingNameInput != null &&
            onboardingNameInput.text != onboardingController.DraftName)
            onboardingNameInput.SetTextWithoutNotify(
                onboardingController.DraftName);
        if (onboardingNameCounter != null)
        {
            int count = onboardingController.DraftName.Length;
            onboardingNameCounter.text = count + " / " +
                OnboardingProfile.MaxNameLength;
            onboardingNameCounter.color =
                OnboardingProfile.IsValidName(onboardingController.DraftName)
                    ? OnboardingCyan
                    : OnboardingWhite;
        }

        for (int index = 0; index < onboardingContinueButtons.Length; index++)
        {
            if (onboardingContinueButtons[index] != null)
            {
                bool interactable =
                    index != active || onboardingController.CanContinue;
                onboardingContinueButtons[index].interactable = interactable;
                RefreshCtaCopy(onboardingContinueButtons[index], interactable);
            }
        }

        RefreshSelection(
            genderCardImages, genderSelectionBadges,
            genderSelectionOutlines,
            onboardingController.SelectedGender);
        RefreshAvatarSelection(onboardingController.SelectedAvatar);
        RefreshAgeSelection(onboardingController.SelectedAge);
    }

    static void RefreshSelection(
        Image[] cards, GameObject[] badges, Outline[] outlines, int selected)
    {
        for (int index = 0; index < cards.Length; index++)
        {
            bool isSelected = index == selected;
            if (cards[index] != null)
                cards[index].color = isSelected
                    ? Color.white
                    : new Color(0.82f, 0.82f, 0.90f, 1f);
            if (badges[index] != null)
                badges[index].SetActive(isSelected);
            RefreshSelectionOutline(outlines[index], isSelected);
        }
    }

    static void RefreshCtaCopy(Button button, bool interactable)
    {
        if (button == null) return;
        Color color = interactable ? OnboardingInk : OnboardingDisabledCopy;
        TMP_Text label = button.transform.Find("Label")?.GetComponent<TMP_Text>();
        TMP_Text arrow = button.transform.Find("Arrow")?.GetComponent<TMP_Text>();
        if (label != null) label.color = color;
        if (arrow != null) arrow.color = color;
    }

    static void RefreshSelectionOutline(Outline outline, bool selected)
    {
        if (outline == null) return;
        outline.effectColor = selected
            ? new Color(1f, 1f, 1f, 0.96f)
            : Color.clear;
        outline.enabled = selected;
    }

    void RefreshAvatarSelection(int selected)
    {
        for (int index = 0; index < avatarCardImages.Length; index++)
        {
            bool selectable = onboardingController != null &&
                onboardingController.IsAvatarSelectable(index);
            bool isSelected = index == selected;
            if (avatarCardButtons[index] != null)
                avatarCardButtons[index].interactable = selectable;
            if (avatarCardImages[index] != null)
                avatarCardImages[index].color = isSelected
                    ? Color.white
                    : selectable
                        ? new Color(0.82f, 0.82f, 0.90f, 1f)
                        : new Color(0.42f, 0.42f, 0.52f, 1f);
            if (avatarSelectionBadges[index] != null)
                avatarSelectionBadges[index].SetActive(isSelected);
            RefreshSelectionOutline(
                avatarSelectionOutlines[index], isSelected);
            if (avatarAvailabilityLabels[index] != null)
            {
                avatarAvailabilityLabels[index].text =
                    GetAvatarAvailabilityLabel(index);
                OnboardingAvatarCatalog.AvailabilityKind kind =
                    OnboardingAvatarCatalog.Get(index).Availability;
                avatarAvailabilityLabels[index].color =
                    kind == OnboardingAvatarCatalog.AvailabilityKind.Free
                        ? OnboardingGreen
                        : kind == OnboardingAvatarCatalog.AvailabilityKind.Locked
                            ? OnboardingMuted
                            : OnboardingGold;
            }
        }

        bool hasSelection = selected >= 0 &&
            selected < OnboardingAvatarCatalog.Count;
        if (onboardingAvatarPreview != null)
        {
            onboardingAvatarPreview.gameObject.SetActive(hasSelection);
            if (hasSelection) onboardingAvatarPreview.sprite =
                onboardingAvatars[selected];
        }
        if (onboardingAvatarPreviewPrompt != null)
            onboardingAvatarPreviewPrompt.gameObject.SetActive(!hasSelection);
        if (onboardingAvatarStatus != null)
            onboardingAvatarStatus.text = hasSelection
                ? L10n.Get(
                    "onboarding_avatar_selected",
                    selected + 1,
                    GetAvatarAvailabilityLabel(selected))
                : string.Empty;

        ApplyAvatarFilter();
    }

    void RefreshAgeSelection(int selected)
    {
        for (int index = 0; index < ageCardImages.Length; index++)
        {
            bool isSelected = index == selected;
            if (ageCardImages[index] != null)
            {
                Color semantic = index == 0
                    ? OnboardingGreen
                    : Color.white;
                float state = isSelected ? 1f : 0.82f;
                ageCardImages[index].color = new Color(
                    semantic.r * state,
                    semantic.g * state,
                    semantic.b * state,
                    1f);
            }
            if (ageSelectionBadges[index] != null)
                ageSelectionBadges[index].SetActive(isSelected);
            RefreshSelectionOutline(ageSelectionOutlines[index], isSelected);
        }
    }

    void UpdateOnboardingLayout()
    {
        ApplyOnboardingResponsiveLayout(false);
    }

    void ApplyOnboardingResponsiveLayout(bool force)
    {
        if (!force &&
            onboardingLayoutWidth == Screen.width &&
            onboardingLayoutHeight == Screen.height)
            return;
        onboardingLayoutWidth = Screen.width;
        onboardingLayoutHeight = Screen.height;

        float aspect = Screen.width > 0
            ? Mathf.Max(1, Screen.height) / (float)Screen.width
            : ReferenceHeight / ReferenceWidth;
        float tall = Mathf.InverseLerp(1.78f, 2.22f, aspect);
        for (int index = 0; index < onboardingScreens.Length; index++)
        {
            if (onboardingHeaderGroups[index] != null)
                Place(onboardingHeaderGroups[index],
                    new Vector2(0f, 28f * tall),
                    new Vector2(ReferenceWidth, ReferenceHeight));
            if (onboardingContentGroups[index] != null)
                Place(onboardingContentGroups[index],
                    new Vector2(0f, -10f * tall),
                    new Vector2(ReferenceWidth, ReferenceHeight));
            if (onboardingFooterGroups[index] != null)
                Place(onboardingFooterGroups[index],
                    new Vector2(0f, -35f * tall),
                    new Vector2(ReferenceWidth, ReferenceHeight));
        }
    }

    static bool EnsureUnderlineCharacter(TMP_FontAsset font)
    {
        if (font == null) return false;
        if (font.HasCharacter('_')) return true;

        TMP_Character fallback = null;
        for (int index = 0; index < font.characterTable.Count; index++)
        {
            uint unicode = font.characterTable[index].unicode;
            if (unicode == '-' || unicode == 0x2013)
            {
                fallback = font.characterTable[index];
                break;
            }
        }
        if (fallback == null || fallback.glyph == null) return false;

        font.characterTable.Add(
            new TMP_Character((uint)'_', font, fallback.glyph));
        font.ReadFontAssetDefinition();
        return font.HasCharacter('_');
    }

    void OnDestroy()
    {
        if (onboardingController != null)
            onboardingController.StateChanged -= RefreshOnboardingState;
    }

    static bool OnboardingArtReady(
        Sprite background,
        Sprite logo,
        Sprite back,
        Sprite welcomeEnsemble,
        Sprite genderBoy,
        Sprite genderGirl,
        Sprite genderOther,
        Sprite mascotThree,
        Sprite mascotSix,
        Sprite mascotSeven,
        Sprite stars,
        Sprite confetti,
        Sprite ageUnder13Mascot,
        Sprite ageTeenMascot,
        Sprite ageAdultMascot,
        Sprite gold,
        Sprite blue,
        Sprite magenta,
        Sprite panel,
        Sprite privacyIcon,
        Sprite nameInputIcon,
        Sprite circle,
        Sprite ageUnder13Icon,
        Sprite ageTeenIcon,
        Sprite ageAdultIcon,
        TMP_FontAsset displayFont,
        TMP_FontAsset bodyFont,
        Sprite[] avatars)
    {
        if (background == null || logo == null || back == null ||
            welcomeEnsemble == null ||
            genderBoy == null || genderGirl == null ||
            genderOther == null || mascotThree == null ||
            mascotSix == null || mascotSeven == null || stars == null ||
            confetti == null || ageUnder13Mascot == null ||
            ageTeenMascot == null || ageAdultMascot == null ||
            gold == null || blue == null ||
            magenta == null || panel == null || displayFont == null ||
            privacyIcon == null || nameInputIcon == null ||
            circle == null ||
            ageUnder13Icon == null ||
            ageTeenIcon == null || ageAdultIcon == null ||
            bodyFont == null || avatars == null ||
            avatars.Length != OnboardingProfile.AvatarCount)
            return false;

        for (int index = 0; index < avatars.Length; index++)
            if (avatars[index] == null) return false;
        return true;
    }
}
