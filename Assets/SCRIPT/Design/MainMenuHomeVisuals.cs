using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Sole Home presentation owner on MainMenu.
//
// The approved cartoon composition is built from modular production sprites and
// live TMP. Existing gameplay/navigation buttons stay callback-authoritative;
// the additional Play With A Friend entry is a real Button routed into the same
// PvpGameController room hub rather than a disconnected visual clone.
[DefaultExecutionOrder(1600)]
public sealed class MainMenuHomeVisuals : MonoBehaviour
{
    public const string VisualRootName = "HomeVisualRoot";
    public const string SafeRootName = "HomeSafeAreaRoot";
    public const string BackgroundName = "HomeBackground";
    public const string OuterFrameName = "HomeOuterFrame";
    public const string StarsName = "HomeStars";
    public const string ConfettiName = "HomeConfetti";
    public const string LogoName = "HomeLogo";
    public const string HeroBoyName = "HomeHeroBoy";
    public const string HeroGirlName = "HomeHeroGirl";
    public const string SpeechBubbleName = "HomeSpeechBubble";
    public const string ChipName = "HomePlayerChip";
    public const string ChipTextName = "HomePlayerChipText";
    public const string ChipScoreName = "HomePlayerChipScore";
    public const string SoloIconName = "HomeSoloIcon";
    public const string PvpIconName = "HomePvpIcon";
    public const string FriendIconName = "HomeFriendIcon";
    public const string DailyIconName = "HomeDailyIcon";
    public const string DailyGiftName = "HomeDailyGift";
    public const string PromoName = "HomeDailyPromo";
    public const string PortalName = "HomePortal";
    public const string MascotSixName = "HomeMascotSix";
    public const string MascotSevenName = "HomeMascotSeven";
    public const string FriendButtonName = "ButtonPrivateRoom";
    public const string CtaFrameName = "HomeCtaFrame";

    const string BackgroundResource = "settings/hol_settings_bg_r1";
    const string LogoResource = "reference/hol_logo_exact";
    const string AvatarResource = "reference/player_cyan_exact";
    const string HeroBoyResource = "phase2a/hol_menu_boy_arms_crossed_r3";
    const string HeroGirlResource = "phase2a/hol_menu_girl_forward_fist_r3";
    const string MascotSixResource = "reference/mascot_6_exact";
    const string MascotSevenResource = "reference/mascot_7_exact";
    const string TrophyResource =
        "dailyhunt/production/daily_mission_icon_trophy";
    const string VsResource = "cartoon/cartoon_vs_burst_base_raster";
    const string FriendResource = "cartoon/cartoon_friend_base_raster";
    const string DailyResource = "cartoon/cartoon_radar_base_raster";
    const string DailyGiftResource =
        "mainmenu/mainmenu_daily_gift_reference_v1";
    const string SpeechBubbleResource = "cartoon/cartoon_speech_bubble_raster";
    const string OuterFrameResource =
        "mainmenu/mainmenu_outer_frame_reference_v1";
    const string PortalResource = "dailyhunt/production/daily_floor_portal";
    const string PromoStarResource = "dailyhunt/production/daily_player_star";
    const string StarsResource = "mainmenu/mainmenu_deco_stars";
    const string ConfettiResource = "mainmenu/mainmenu_deco_confetti";

    const string GoldCtaResource = "phase2a/hol_cta_gold_r2_9s";
    const string MagentaCtaResource = "phase2a/hol_cta_magenta_r2_9s";
    const string BlueCtaResource = "phase2a/hol_cta_blue_r2_9s";
    const string DailyCtaResource = "dailyhunt/v1/daily_action_revive_v1";
    const string PromoFrameResource = "dailyhunt/v1/daily_input_shell_v1";
    const string ChipFrameResource =
        "dailyhunt/production/daily_player_chip_shell_v3";
    const string ChipAvatarRingResource =
        "dailyhunt/production/daily_player_avatar_ring_v1";
    const string GearResource = "phase2a/hol_settings_gear_r2";
    const string DisplayFontResource = "phase2a/fonts/HOL Menu Display SDF";
    const string BodyFontResource = "phase2a/fonts/HOL Menu Body SDF";

    const float ReferenceWidth = 1080f;
    const float ReferenceHeight = 1920f;

    static readonly Color Ink = new Color(0.09f, 0.05f, 0.16f, 1f);
    static readonly Color NearWhite = new Color(0.985f, 0.975f, 1f, 1f);
    static readonly Color Cyan = new Color(0.18f, 0.92f, 1f, 1f);
    static readonly Color Gold = new Color(1f, 0.80f, 0.20f, 1f);
    static readonly Color DailyMuted = new Color(0.82f, 0.52f, 1f, 1f);
    static readonly Color Muted = new Color(0.87f, 0.84f, 0.96f, 0.90f);

    public static readonly string[] LoadedResources =
    {
        BackgroundResource,
        LogoResource,
        AvatarResource,
        HeroBoyResource,
        HeroGirlResource,
        MascotSixResource,
        MascotSevenResource,
        TrophyResource,
        VsResource,
        FriendResource,
        DailyResource,
        DailyGiftResource,
        SpeechBubbleResource,
        OuterFrameResource,
        PortalResource,
        PromoStarResource,
        StarsResource,
        ConfettiResource,
        GoldCtaResource,
        MagentaCtaResource,
        BlueCtaResource,
        DailyCtaResource,
        PromoFrameResource,
        ChipFrameResource,
        ChipAvatarRingResource,
        GearResource,
    };

    public static readonly string[] LoadedFontResources =
    {
        DisplayFontResource,
        BodyFontResource,
    };

    RectTransform visualRoot;
    RectTransform safeRoot;
    RectTransform logoRect;
    RectTransform heroBoyRect;
    RectTransform heroGirlRect;
    RectTransform speechBubbleRect;
    RectTransform gearRect;
    RectTransform chipRect;
    RectTransform soloButtonRect;
    RectTransform pvpButtonRect;
    RectTransform friendButtonRect;
    RectTransform dailyButtonRect;
    RectTransform promoRect;
    RectTransform portalRect;
    RectTransform mascotSixRect;
    RectTransform mascotSevenRect;

    TMP_FontAsset displayFont;
    TMP_FontAsset bodyFont;
    Sprite defaultAvatarSprite;
    Image chipAvatarImage;
    TMP_Text chipText;
    TMP_Text chipScoreText;
    TMP_Text speechText;
    TMP_Text promoTitleText;
    TMP_Text promoBodyText;
    Button friendButton;
    PvpGameController pvpController;
    bool laidOut;
    int lastLayoutWidth = -1;
    int lastLayoutHeight = -1;
    L10n.Language lastLanguage;

    public bool IsReady { get; private set; }
    public bool IsSettled { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MainMenu" || !scene.IsValid() || !scene.isLoaded)
            return;

        Canvas canvas = FindOwnedCanvas(scene);
        if (canvas != null && canvas.GetComponent<MainMenuHomeVisuals>() == null)
            canvas.gameObject.AddComponent<MainMenuHomeVisuals>();
    }

    public static bool OwnsHome(Scene scene)
    {
        if (!scene.IsValid()) return false;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<MainMenuHomeVisuals>(true) != null)
                return true;
        }

        return false;
    }

    IEnumerator Start()
    {
        // Runtime owners inject PvP and Daily Hunt controls after scene load.
        // Wait for those real buttons and the room controller instead of
        // replacing them with visual-only copies.
        for (int frame = 0; frame < 120; frame++)
        {
            pvpController = FindInScene<PvpGameController>(gameObject.scene);
            if (FindButton("ButtonPlay") != null &&
                FindButton("ButtonPvP") != null &&
                FindButton("DailyHuntButton") != null &&
                FindButton("Buttonsettings") != null &&
                pvpController != null)
                break;

            yield return null;
        }

        BuildHome();
        if (IsReady)
        {
            // Runtime/device capture gets an end-of-frame paint barrier. Unity's
            // headless batchmode never resumes WaitForEndOfFrame, so PlayMode
            // validation uses the deterministic frame barrier only.
            yield return null;
            if (!Application.isBatchMode)
                yield return new WaitForEndOfFrame();
        }

        IsSettled = IsReady;
        laidOut = true;
    }

    void OnEnable()
    {
        L10n.OnLanguageChanged += RefreshPresentation;
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= RefreshPresentation;
    }

    void LateUpdate()
    {
        if (!laidOut || visualRoot == null) return;

        var menu = FindInScene<MenuManager>(gameObject.scene);
        bool homeVisible = menu != null &&
                           menu.mainMenuPanel != null &&
                           menu.mainMenuPanel.activeSelf;

        if (visualRoot.gameObject.activeSelf != homeVisible)
            visualRoot.gameObject.SetActive(homeVisible);

        if (!homeVisible) return;

        RefreshChip();
        ApplyResponsiveLayout();
    }

    void BuildHome()
    {
        Canvas canvas = GetComponent<Canvas>();
        var menu = FindInScene<MenuManager>(gameObject.scene);
        if (canvas == null || menu == null || menu.mainMenuPanel == null)
        {
            Debug.LogError("[MainMenuHomeVisuals] Missing Canvas/MenuManager.");
            return;
        }

        pvpController = pvpController ??
            FindInScene<PvpGameController>(gameObject.scene);

        Sprite background = LoadRequired(BackgroundResource);
        Sprite logo = LoadRequired(LogoResource);
        Sprite avatar = LoadRequired(AvatarResource);
        defaultAvatarSprite = avatar;
        Sprite heroBoy = LoadRequired(HeroBoyResource);
        Sprite heroGirl = LoadRequired(HeroGirlResource);
        Sprite six = LoadRequired(MascotSixResource);
        Sprite seven = LoadRequired(MascotSevenResource);
        Sprite trophy = LoadRequired(TrophyResource);
        Sprite vs = LoadRequired(VsResource);
        Sprite friend = LoadRequired(FriendResource);
        Sprite daily = LoadRequired(DailyResource);
        Sprite dailyGift = LoadRequired(DailyGiftResource);
        Sprite speech = LoadRequired(SpeechBubbleResource);
        Sprite outerFrame = LoadRequired(OuterFrameResource);
        Sprite portal = LoadRequired(PortalResource);
        Sprite promoStar = LoadRequired(PromoStarResource);
        Sprite stars = LoadRequired(StarsResource);
        Sprite confetti = LoadRequired(ConfettiResource);
        Sprite goldFrame = LoadRequired(GoldCtaResource);
        Sprite magentaFrame = LoadRequired(MagentaCtaResource);
        Sprite blueFrame = LoadRequired(BlueCtaResource);
        Sprite dailyFrame = LoadRequired(DailyCtaResource);
        Sprite promoFrame = LoadRequired(PromoFrameResource);
        Sprite chipFrame = LoadRequired(ChipFrameResource);
        Sprite avatarRing = LoadRequired(ChipAvatarRingResource);
        Sprite gear = LoadRequired(GearResource);
        displayFont = Resources.Load<TMP_FontAsset>(DisplayFontResource);
        bodyFont = Resources.Load<TMP_FontAsset>(BodyFontResource);

        IsReady = ArtReady(
            background, logo, avatar, heroBoy, heroGirl, six, seven, trophy, vs,
            friend, daily, dailyGift, speech, outerFrame, portal, promoStar,
            stars, confetti, goldFrame,
            magentaFrame, blueFrame, dailyFrame, promoFrame, chipFrame,
            avatarRing, gear) &&
            displayFont != null &&
            bodyFont != null &&
            pvpController != null;

        if (!IsReady)
        {
            Debug.LogError(
                "[MainMenuHomeVisuals] Required Home art, fonts or controller are missing.");
            return;
        }

        var panelImage = menu.mainMenuPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.enabled = false;
            panelImage.raycastTarget = false;
        }

        HideLegacyHome(canvas.transform);
        HideNamed("ButtonQuit");

        visualRoot = EnsureRect(canvas.transform, VisualRootName);
        Stretch(visualRoot);
        visualRoot.SetAsFirstSibling();

        var bg = EnsureImage(visualRoot, BackgroundName);
        Stretch(bg.rectTransform);
        ConfigureImage(bg, background, false, Image.Type.Simple);

        var starsImage = EnsureImage(visualRoot, StarsName);
        Stretch(starsImage.rectTransform);
        ConfigureImage(starsImage, stars, false, Image.Type.Simple);
        starsImage.gameObject.SetActive(false);

        var confettiImage = EnsureImage(visualRoot, ConfettiName);
        Stretch(confettiImage.rectTransform);
        ConfigureImage(confettiImage, confetti, false, Image.Type.Simple);
        confettiImage.gameObject.SetActive(false);

        var outer = EnsureImage(visualRoot, OuterFrameName);
        ConfigureImage(outer, outerFrame, false, Image.Type.Simple);
        Place(outer.rectTransform, Vector2.zero,
            new Vector2(ReferenceWidth, ReferenceHeight));

        safeRoot = EnsureRect(visualRoot, SafeRootName);
        Stretch(safeRoot);
        ResponsiveSafeAreaRoot.Attach(
            safeRoot, (RectTransform)canvas.transform,
            new Vector2(ReferenceWidth, ReferenceHeight));

        BuildTopBar(safeRoot, gear, chipFrame, avatarRing, avatar, trophy);

        var logoImage = EnsureImage(safeRoot, LogoName);
        ConfigureImage(logoImage, logo, true, Image.Type.Simple);
        logoRect = logoImage.rectTransform;

        var heroImage = EnsureImage(safeRoot, HeroBoyName);
        ConfigureImage(heroImage, heroBoy, true, Image.Type.Simple);
        heroBoyRect = heroImage.rectTransform;

        var heroineImage = EnsureImage(safeRoot, HeroGirlName);
        ConfigureImage(heroineImage, heroGirl, true, Image.Type.Simple);
        heroGirlRect = heroineImage.rectTransform;

        var bubble = EnsureImage(safeRoot, SpeechBubbleName);
        ConfigureImage(bubble, speech, false, Image.Type.Sliced);
        bubble.pixelsPerUnitMultiplier = 2f;
        speechBubbleRect = bubble.rectTransform;
        // The approved bubble overlaps the hero art, so it must paint above the
        // two characters while remaining below the interactive CTA layer.
        bubble.transform.SetAsLastSibling();

        speechText = EnsureText(
            bubble.transform, "HomeSpeechText", 32f, displayFont, Ink,
            TextAlignmentOptions.Center);
        StretchText(speechText.rectTransform, 34f, 32f);
        ConfigureBodyText(speechText, 25f, 34f);
        SetLocalized(speechText, "home_hero_speech");

        soloButtonRect = RestyleCta(
            safeRoot, FindButton("ButtonPlay"), goldFrame, trophy,
            SoloIconName, "HomeSoloTitle", "home_solo_title",
            "HomeSoloSubtitle", "home_solo_subtitle", true, NearWhite);

        pvpButtonRect = RestyleCta(
            safeRoot, FindButton("ButtonPvP"), magentaFrame, vs,
            PvpIconName, "HomePvpTitle", "home_pvp_title",
            "HomePvpSubtitle", "home_pvp_subtitle", false, NearWhite);

        friendButton = EnsureFriendButton(safeRoot);
        friendButtonRect = RestyleCta(
            safeRoot, friendButton, blueFrame, friend,
            FriendIconName, "HomeFriendTitle", "home_private_title",
            "HomeFriendSubtitle", "home_private_subtitle", false,
            NearWhite);

        dailyButtonRect = RestyleCta(
            safeRoot, FindButton("DailyHuntButton"), dailyFrame, daily,
            DailyIconName, "HomeDailyTitle", "home_daily_title",
            "HomeDailySubtitle", "home_daily_subtitle", false,
            NearWhite);

        var dailyGiftImage = EnsureImage(dailyButtonRect, DailyGiftName);
        ConfigureImage(dailyGiftImage, dailyGift, true, Image.Type.Simple);
        Place(dailyGiftImage.rectTransform, new Vector2(390f, 1f),
            new Vector2(158f, 150f));

        BuildBottomPromo(
            safeRoot, promoFrame, trophy, promoStar, six, seven, portal);

        // The bezel is the final non-interactive paint layer. It must mask the
        // composition edges instead of allowing decorative art over the frame.
        outer.transform.SetAsLastSibling();

        ApplyResponsiveLayout(true);
        ApplyTypographyLayout();
        RefreshChip();
    }

    void BuildTopBar(
        Transform safe,
        Sprite gear,
        Sprite chipFrame,
        Sprite avatarRing,
        Sprite avatar,
        Sprite trophy)
    {
        var settings = FindButton("Buttonsettings");
        if (settings != null)
        {
            Reparent(settings.transform, safe);
            HideChildGraphics(settings.transform);
            gearRect = (RectTransform)settings.transform;

            var image = settings.GetComponent<Image>();
            if (image == null)
                image = settings.gameObject.AddComponent<Image>();
            ConfigureInteractiveImage(image, gear, true, Image.Type.Simple, 1f);
            settings.targetGraphic = image;
            ConfigureButtonState(settings);
        }

        var chip = EnsureImage(safe, ChipName);
        ConfigureImage(chip, chipFrame, false, Image.Type.Simple);
        chipRect = chip.rectTransform;

        var avatarRingImage = EnsureImage(chip.transform, "HomePlayerAvatarRing");
        ConfigureImage(avatarRingImage, avatarRing, true, Image.Type.Simple);
        Place(
            avatarRingImage.rectTransform, new Vector2(-126f, 0f),
            new Vector2(108f, 108f));

        chipAvatarImage = EnsureImage(chip.transform, "HomePlayerAvatar");
        ConfigureImage(chipAvatarImage, avatar, true, Image.Type.Simple);
        Place(
            chipAvatarImage.rectTransform, new Vector2(-126f, 0f),
            new Vector2(92f, 92f));

        var trophyImage = EnsureImage(chip.transform, "HomeTrophyIcon");
        ConfigureImage(trophyImage, trophy, true, Image.Type.Simple);
        Place(
            trophyImage.rectTransform, new Vector2(0f, -30f),
            new Vector2(44f, 44f));

        chipText = EnsureText(
            chip.transform, ChipTextName, 35f, displayFont, NearWhite,
            TextAlignmentOptions.Left);
        Place(
            chipText.rectTransform, new Vector2(72f, 29f),
            new Vector2(220f, 48f));
        chipText.enableAutoSizing = true;
        chipText.fontSizeMin = 27f;
        chipText.fontSizeMax = 36f;
        chipText.overflowMode = TextOverflowModes.Overflow;
        chipText.outlineColor = Ink;
        chipText.outlineWidth = 0.11f;

        chipScoreText = EnsureText(
            chip.transform, ChipScoreName, 38f, displayFont, NearWhite,
            TextAlignmentOptions.Left);
        Place(
            chipScoreText.rectTransform, new Vector2(108f, -30f),
            new Vector2(164f, 52f));
        chipScoreText.enableAutoSizing = true;
        chipScoreText.fontSizeMin = 30f;
        chipScoreText.fontSizeMax = 40f;
        chipScoreText.overflowMode = TextOverflowModes.Overflow;
        chipScoreText.outlineColor = Ink;
        chipScoreText.outlineWidth = 0.11f;
    }

    Button EnsureFriendButton(Transform parent)
    {
        // Home owns this button. Never steal a same-named control from one of
        // the PvP/private-room panels, because that creates a second layout
        // writer and moves the Home CTA after our measured layout pass.
        Transform existing = DirectChild(parent, FriendButtonName);
        Button button = existing == null ? null : existing.GetComponent<Button>();
        if (button == null)
        {
            // Do not use RuntimeUI.CreateButton here. That helper registers a
            // generic ResponsivePageLayout writer, while this screen's one
            // measured presentation owner is MainMenuHomeVisuals.
            RectTransform rect = EnsureRect(parent, FriendButtonName);
            var image = rect.GetComponent<Image>();
            if (image == null)
                image = rect.gameObject.AddComponent<Image>();
            button = rect.GetComponent<Button>();
            if (button == null)
                button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(pvpController.OpenPvpMenu);
        }

        return button;
    }

    RectTransform RestyleCta(
        Transform safe,
        Button button,
        Sprite frame,
        Sprite icon,
        string iconName,
        string titleName,
        string titleKey,
        string subtitleName,
        string subtitleKey,
        bool primary,
        Color labelColor)
    {
        if (button == null) return null;

        Reparent(button.transform, safe);
        HideChildGraphics(button.transform);

        var rect = (RectTransform)button.transform;
        var image = button.GetComponent<Image>();
        if (image == null)
            image = button.gameObject.AddComponent<Image>();

        // Keep the real hit rectangle compact and non-overlapping. The larger
        // reference-faithful painted frame is a non-raycast child, so adjacent
        // visual glows never create ambiguous touch ownership.
        image.enabled = true;
        image.sprite = null;
        image.type = Image.Type.Simple;
        // This Image is an intentional raycast-only accessibility surface. Keep it
        // fully transparent; a near-zero alpha can leak a faint rectangle on some
        // renderers and violates the production UI integrity contract.
        image.color = Color.clear;
        image.raycastTarget = true;
        button.targetGraphic = image;
        ConfigureButtonState(button);
        button.transition = Selectable.Transition.None;

        var visualFrame = EnsureImage(button.transform, CtaFrameName);
        ConfigureImage(visualFrame, frame, false, Image.Type.Simple);
        Vector2 visualSize = button.name == "ButtonPlay"
            ? new Vector2(990f, 255f)
            : button.name == "DailyHuntButton"
                ? new Vector2(990f, 235f)
                : new Vector2(990f, 250f);
        Place(visualFrame.rectTransform, Vector2.zero, visualSize);
        visualFrame.transform.SetAsFirstSibling();
        var juice = RuntimeUI.AttachJuice(button);
        rect.localScale = Vector3.one;
        if (juice != null)
            juice.ResetBaseScale(Vector3.one);

        if (icon != null)
        {
            var iconImage = EnsureImage(button.transform, iconName);
            ConfigureImage(iconImage, icon, true, Image.Type.Simple);
            float iconSize = primary ? 188f :
                iconName == PvpIconName ? 220f :
                iconName == FriendIconName ? 200f : 185f;
            Place(
                iconImage.rectTransform, new Vector2(-350f, 0f),
                new Vector2(iconSize, iconSize));

            if (iconName == PvpIconName)
            {
                var vsText = EnsureText(
                    iconImage.transform, "HomePvpIconText", 53f,
                    displayFont, Ink, TextAlignmentOptions.Center);
                StretchText(vsText.rectTransform, 10f, 10f);
                ConfigureDisplayText(vsText, 42f, 56f);
                vsText.text = "VS";
                AddTextShadow(vsText, 0.30f);
            }
        }

        var title = EnsureText(
            button.transform, titleName, primary ? 64f : 60f,
            displayFont, labelColor, TextAlignmentOptions.Center);
        Place(
            title.rectTransform, new Vector2(60f, 24f),
            new Vector2(700f, 78f));
        ConfigureDisplayText(
            title, primary ? 48f : 40f, primary ? 66f : 62f);
        SetLocalized(title, titleKey);

        Color subtitleColor = button.name == "DailyHuntButton"
            ? DailyMuted
            : Ink;
        var subtitle = EnsureText(
            button.transform, subtitleName, primary ? 31f : 29f,
            displayFont, subtitleColor,
            TextAlignmentOptions.Center);
        Place(
            subtitle.rectTransform, new Vector2(60f, -39f),
            new Vector2(700f, 54f));
        ConfigureDisplayText(
            subtitle, primary ? 24f : 22f, primary ? 32f : 30f);
        SetLocalized(subtitle, subtitleKey);

        return rect;
    }

    void BuildBottomPromo(
        Transform safe,
        Sprite frame,
        Sprite trophy,
        Sprite star,
        Sprite six,
        Sprite seven,
        Sprite portal)
    {
        var promo = EnsureImage(safe, PromoName);
        ConfigureImage(promo, frame, false, Image.Type.Simple);
        promoRect = promo.rectTransform;

        var trophyImage = EnsureImage(promo.transform, "HomePromoTrophy");
        ConfigureImage(trophyImage, trophy, true, Image.Type.Simple);
        Place(
            trophyImage.rectTransform, new Vector2(-168f, -48f),
            new Vector2(68f, 68f));

        var leftStar = EnsureImage(promo.transform, "HomePromoStarLeft");
        ConfigureImage(leftStar, star, true, Image.Type.Simple);
        Place(leftStar.rectTransform, new Vector2(-178f, 38f),
            new Vector2(38f, 38f));

        var rightStar = EnsureImage(promo.transform, "HomePromoStarRight");
        ConfigureImage(rightStar, star, true, Image.Type.Simple);
        Place(rightStar.rectTransform, new Vector2(220f, 38f),
            new Vector2(38f, 38f));

        promoTitleText = EnsureText(
            promo.transform, "HomePromoTitle", 31f, displayFont, Cyan,
            TextAlignmentOptions.Center);
        ConfigureDisplayText(promoTitleText, 24f, 32f);
        SetLocalized(promoTitleText, "home_reward_title");

        promoBodyText = EnsureText(
            promo.transform, "HomePromoBody", 29f, displayFont, Gold,
            TextAlignmentOptions.Center);
        ConfigureDisplayText(promoBodyText, 23f, 34f);
        SetLocalized(promoBodyText, "home_reward_body");

        var portalImage = EnsureImage(safe, PortalName);
        ConfigureImage(portalImage, portal, true, Image.Type.Simple);
        portalRect = portalImage.rectTransform;
        portalImage.transform.SetSiblingIndex(promo.transform.GetSiblingIndex());

        var sixImage = EnsureImage(safe, MascotSixName);
        ConfigureImage(sixImage, six, true, Image.Type.Simple);
        mascotSixRect = sixImage.rectTransform;

        var sevenImage = EnsureImage(safe, MascotSevenName);
        ConfigureImage(sevenImage, seven, true, Image.Type.Simple);
        mascotSevenRect = sevenImage.rectTransform;
    }

    void RefreshPresentation()
    {
        RefreshChip();
        ApplyResponsiveLayout(true);
        ApplyTypographyLayout();
    }

    void RefreshChip()
    {
        if (chipAvatarImage != null)
            chipAvatarImage.sprite = ResolveProfileAvatar(defaultAvatarSprite);

        if (chipText == null || chipScoreText == null) return;

        string player = PlayerPrefs.GetString("PlayerName", "");
        if (string.IsNullOrWhiteSpace(player))
            player = L10n.Get("player_default");

        chipText.text = player;
        chipScoreText.text = GameStats.Wins.ToString("N0");
    }

    static Sprite ResolveProfileAvatar(Sprite fallback)
    {
        if (!OnboardingProfile.TryLoadCommittedAvatar(out int avatarIndex))
            return fallback;

        OnboardingAvatarCatalog.Entry entry =
            OnboardingAvatarCatalog.Get(avatarIndex);
        if (string.IsNullOrWhiteSpace(entry.ResourcePath))
            return fallback;

        Sprite selected = Resources.Load<Sprite>(entry.ResourcePath);
        return selected != null ? selected : fallback;
    }

    void ApplyResponsiveLayout(bool force = false)
    {
        ApplyResponsiveLayoutForViewport(Screen.width, Screen.height, force);
    }

    // Regression/capture seam retained for existing automation.
    void ApplyResponsiveLayoutForWidth(int width, bool force = false)
    {
        ApplyResponsiveLayoutForViewport(width, Screen.height, force);
    }

    void ApplyResponsiveLayoutForViewport(
        int width,
        int height,
        bool force = false)
    {
        if (logoRect == null || heroBoyRect == null || heroGirlRect == null ||
            speechBubbleRect == null || soloButtonRect == null ||
            pvpButtonRect == null || friendButtonRect == null ||
            dailyButtonRect == null || promoRect == null || portalRect == null ||
            mascotSixRect == null || mascotSevenRect == null)
            return;

        L10n.Language language = L10n.Current;
        if (!force &&
            width == lastLayoutWidth &&
            height == lastLayoutHeight &&
            language == lastLanguage)
            return;

        lastLayoutWidth = width;
        lastLayoutHeight = height;
        lastLanguage = language;

        float aspect = width > 0
            ? Mathf.Max(1, height) / (float)width
            : ReferenceHeight / ReferenceWidth;
        float tall = Mathf.InverseLerp(1.78f, 2.22f, aspect);

        Place(
            gearRect, new Vector2(-432f, 838f + 45f * tall),
            new Vector2(124f, 124f));
        Place(
            chipRect, new Vector2(325f, 838f + 45f * tall),
            new Vector2(360f, 140f));
        Place(
            logoRect, new Vector2(-42f, 797f + 110f * tall),
            new Vector2(580f, 306f));
        Place(
            heroBoyRect, new Vector2(-205f, 430f + 62f * tall),
            new Vector2(455f, 455f));
        Place(
            heroGirlRect, new Vector2(92f, 425f + 62f * tall),
            new Vector2(460f, 460f));
        Place(
            speechBubbleRect, new Vector2(350f, 390f + 62f * tall),
            new Vector2(300f, 200f));

        float buttonShift = 30f * tall;
        Place(
            soloButtonRect, new Vector2(0f, 105f + buttonShift),
            new Vector2(990f, 190f));
        Place(
            pvpButtonRect, new Vector2(0f, -92f + buttonShift),
            new Vector2(990f, 180f));
        Place(
            friendButtonRect, new Vector2(0f, -288f + buttonShift),
            new Vector2(990f, 180f));
        Place(
            dailyButtonRect, new Vector2(0f, -478f + buttonShift),
            new Vector2(990f, 175f));
        Place(
            promoRect, new Vector2(0f, -710f - 34f * tall),
            new Vector2(500f, 220f));
        Place(
            portalRect, new Vector2(0f, -876f - 42f * tall),
            new Vector2(650f, 180f));
        Place(
            mascotSixRect, new Vector2(-380f, -735f - 44f * tall),
            new Vector2(300f, 350f));
        Place(
            mascotSevenRect, new Vector2(335f, -735f - 44f * tall),
            new Vector2(300f, 350f));

        // Greek needs the full title bounds, never a tiny-font fallback.
        foreach (string textName in new[]
        {
            "HomeSoloTitle",
            "HomePvpTitle",
            "HomeFriendTitle",
            "HomeDailyTitle",
        })
        {
            Transform found = DeepFind(safeRoot, textName);
            TMP_Text text = found == null ? null : found.GetComponent<TMP_Text>();
            if (text == null) continue;
            text.fontSizeMin = language == L10n.Language.Greek ? 38f : 40f;
            text.fontSizeMax = textName == "HomeSoloTitle" ? 66f : 62f;
        }

        ApplyTypographyLayout();
    }

    void ApplyTypographyLayout()
    {
        if (safeRoot == null) return;

        if (speechText != null)
        {
            speechText.color = Ink;
            speechText.alignment = TextAlignmentOptions.Center;
            speechText.enableAutoSizing = true;
            speechText.fontSizeMin = 23f;
            speechText.fontSizeMax = 30f;
            speechText.enableWordWrapping = false;
            speechText.overflowMode = TextOverflowModes.Overflow;
            speechText.lineSpacing = 0f;
            // Deliberately stay inside the bubble's flat cream body. The tail
            // begins below this rectangle, so the emphasis line cannot paint
            // across the purple outline even on the narrow 720px viewport.
            Place(speechText.rectTransform, new Vector2(0f, 22f),
                new Vector2(248f, 150f));
            AddTextShadow(speechText, 0.22f);
        }

        if (promoTitleText != null)
        {
            promoTitleText.alignment = TextAlignmentOptions.Center;
            promoTitleText.enableAutoSizing = true;
            promoTitleText.fontSizeMin = 24f;
            promoTitleText.fontSizeMax = 32f;
            promoTitleText.enableWordWrapping = false;
            promoTitleText.overflowMode = TextOverflowModes.Overflow;
            promoTitleText.lineSpacing = 0f;
            Place(promoTitleText.rectTransform, new Vector2(42f, 47f),
                new Vector2(360f, 48f));
        }

        if (promoBodyText != null)
        {
            promoBodyText.alignment = TextAlignmentOptions.Center;
            promoBodyText.enableAutoSizing = true;
            promoBodyText.fontSizeMin = 23f;
            promoBodyText.fontSizeMax = 34f;
            promoBodyText.enableWordWrapping = false;
            promoBodyText.overflowMode = TextOverflowModes.Overflow;
            promoBodyText.lineSpacing = 0f;
            // The narrower, raised body block preserves a real gap beside the
            // trophy and clears the lower cyan frame without moving any art.
            Place(promoBodyText.rectTransform, new Vector2(35f, -17f),
                new Vector2(340f, 88f));
        }

        if (chipText != null)
        {
            chipText.alignment = TextAlignmentOptions.Left;
            chipText.enableAutoSizing = true;
            chipText.fontSizeMin = 27f;
            chipText.fontSizeMax = 36f;
            chipText.enableWordWrapping = false;
            chipText.overflowMode = TextOverflowModes.Overflow;
            Place(chipText.rectTransform, new Vector2(72f, 29f),
                new Vector2(220f, 48f));
        }

        if (chipScoreText != null)
        {
            chipScoreText.alignment = TextAlignmentOptions.Left;
            chipScoreText.enableAutoSizing = true;
            chipScoreText.fontSizeMin = 30f;
            chipScoreText.fontSizeMax = 40f;
            chipScoreText.enableWordWrapping = false;
            chipScoreText.overflowMode = TextOverflowModes.Overflow;
            Place(chipScoreText.rectTransform, new Vector2(108f, -30f),
                new Vector2(164f, 52f));
        }

        ConfigureCtaTypography(
            "HomeSoloTitle", "HomeSoloSubtitle", true);
        ConfigureCtaTypography(
            "HomePvpTitle", "HomePvpSubtitle", false);
        ConfigureCtaTypography(
            "HomeFriendTitle", "HomeFriendSubtitle", false);
        ConfigureCtaTypography(
            "HomeDailyTitle", "HomeDailySubtitle", false);
    }

    void ConfigureCtaTypography(
        string titleName,
        string subtitleName,
        bool primary)
    {
        TMP_Text title = FindText(titleName);
        TMP_Text subtitle = FindText(subtitleName);

        if (title != null)
        {
            bool daily = titleName == "HomeDailyTitle";
            bool friend = titleName == "HomeFriendTitle";
            float fixedSize = primary ? 88f : daily ? 76f : friend ? 70f : 86f;
            title.alignment = TextAlignmentOptions.Center;
            title.enableAutoSizing = false;
            title.fontSize = fixedSize;
            title.outlineColor = Ink;
            title.outlineWidth = 0.16f;
            title.enableWordWrapping = false;
            title.overflowMode = TextOverflowModes.Overflow;
            Place(title.rectTransform, new Vector2(daily ? 0f : 58f, 25f),
                new Vector2(daily ? 680f : 730f, 108f));
        }

        if (subtitle != null)
        {
            subtitle.color = titleName == "HomeDailyTitle"
                ? new Color(0.88f, 0.50f, 1f, 1f)
                : Ink;
            subtitle.alignment = TextAlignmentOptions.Center;
            subtitle.enableAutoSizing = true;
            subtitle.fontSizeMin = primary ? 25f : 23f;
            subtitle.fontSizeMax = primary ? 36f : 33f;
            subtitle.enableWordWrapping = false;
            subtitle.overflowMode = TextOverflowModes.Overflow;
            bool daily = titleName == "HomeDailyTitle";
            Place(subtitle.rectTransform,
                new Vector2(daily ? 0f : 58f, -28f),
                new Vector2(daily ? 650f : 700f, 52f));
        }
    }

    TMP_Text FindText(string name)
    {
        Transform found = DeepFind(safeRoot, name);
        return found == null ? null : found.GetComponent<TMP_Text>();
    }

    static void ConfigureDisplayText(
        TMP_Text text,
        float minimum,
        float maximum)
    {
        if (text == null) return;

        text.fontStyle = FontStyles.Bold;
        text.enableAutoSizing = true;
        text.fontSizeMin = minimum;
        text.fontSizeMax = maximum;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        AddTextShadow(text, 0.68f);
    }

    static void ConfigureBodyText(
        TMP_Text text,
        float minimum,
        float maximum)
    {
        if (text == null) return;

        text.enableAutoSizing = true;
        text.fontSizeMin = minimum;
        text.fontSizeMax = maximum;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
    }

    static void AddTextShadow(TMP_Text text, float alpha)
    {
        if (text == null) return;

        var shadow = text.GetComponent<Shadow>();
        if (shadow == null)
            shadow = text.gameObject.AddComponent<Shadow>();

        shadow.effectColor = new Color(0.02f, 0.01f, 0.12f, alpha);
        shadow.effectDistance = new Vector2(2f, -3f);
        shadow.useGraphicAlpha = true;
    }

    static void ConfigureButtonState(Button button)
    {
        if (button == null) return;

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.78f, 0.82f, 0.92f, 1f);
        colors.disabledColor = new Color(0.55f, 0.56f, 0.64f, 0.72f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.06f;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = colors;
    }

    void HideLegacyHome(Transform canvas)
    {
        foreach (string name in new[]
        {
            "ExactReferenceBackdrop",
            "AttachmentReferenceBackdrop",
            "ExactHOLLogo",
            "BoardHomeLogo",
            "StatsLabel",
            "HomeNeonBackdrop",
            "HomeArenaGrid",
            "HomeDecoStars",
            "HomeDecoLightning",
            "HomeDecoConfetti",
            "HomeDecoNumbers",
        })
        {
            Transform child = DeepFind(canvas, name);
            if (child != null && child != visualRoot)
                child.gameObject.SetActive(false);
        }
    }

    Button FindButton(string name)
    {
        Transform found = DeepFind(transform, name);
        return found == null ? null : found.GetComponent<Button>();
    }

    void HideNamed(string name)
    {
        Transform found = DeepFind(transform, name);
        if (found != null)
            found.gameObject.SetActive(false);
    }

    static void HideChildGraphics(Transform root)
    {
        if (root == null) return;

        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic.transform == root) continue;
            graphic.gameObject.SetActive(false);
        }
    }

    static void Reparent(Transform child, Transform parent)
    {
        if (child == null || parent == null) return;

        if (child.parent != parent)
            child.SetParent(parent, false);

        child.gameObject.SetActive(true);
        child.SetAsLastSibling();
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

    static Sprite LoadRequired(string path)
    {
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite == null)
            Debug.LogError("[MainMenuHomeVisuals] Missing Resources/" + path + ".");
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

        text.gameObject.SetActive(true);
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

    static void ConfigureInteractiveImage(
        Image image,
        Sprite sprite,
        bool preserveAspect,
        Image.Type type,
        float pixelsPerUnit)
    {
        ConfigureImage(image, sprite, preserveAspect, type);
        image.pixelsPerUnitMultiplier = pixelsPerUnit;
        image.raycastTarget = true;
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

    static T FindInScene<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid()) return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null)
                return found;
        }

        return null;
    }

    static Canvas FindOwnedCanvas(Scene scene)
    {
        var menu = FindInScene<MenuManager>(scene);
        if (menu != null && menu.mainMenuPanel != null)
        {
            Canvas owned = menu.mainMenuPanel.GetComponentInParent<Canvas>();
            if (owned != null &&
                owned.isRootCanvas &&
                owned.renderMode != RenderMode.WorldSpace)
                return owned;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas.gameObject.scene == scene &&
                    canvas.isRootCanvas &&
                    canvas.renderMode != RenderMode.WorldSpace)
                    return canvas;
            }
        }

        return null;
    }
}
