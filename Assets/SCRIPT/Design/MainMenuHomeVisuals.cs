using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Shared geometry only: each screen's sole presentation owner supplies its own
// sprite-safe text regions. This is not a component or another layout writer.
internal sealed class MainMenuCenteredTextRegion
{
    internal readonly TMP_Text Text;
    internal readonly Rect SafeRect;
    readonly Vector2 layoutSize;
    string lastText;
    Vector2 lastPosition;
    bool applied;

    internal MainMenuCenteredTextRegion(
        TMP_Text text, float x, float y, float width, float height)
    {
        Text = text;
        SafeRect = new Rect(x - width * 0.5f, y - height * 0.5f, width, height);
        // TMP line metrics include ascender/descender space with no visible
        // ink. Keep that layout space so centering cannot reduce an approved
        // font merely to fit its invisible line box. The smaller SafeRect is
        // still the strict boundary for the actual rendered glyphs.
        layoutSize = new Vector2(width, Mathf.Max(height + 32f,
            text == null ? height : text.rectTransform.rect.height));
    }

    internal void Apply()
    {
        if (Text == null || !Text.gameObject.activeInHierarchy)
        {
            applied = false;
            return;
        }
        RectTransform rect = Text.rectTransform;
        if (applied && lastText == Text.text && !Text.havePropertiesChanged &&
            rect.anchoredPosition == lastPosition && rect.sizeDelta == layoutSize)
            return;

        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = SafeRect.center;
        rect.sizeDelta = layoutSize;
        Text.alignment = TextAlignmentOptions.Center;
        Text.margin = Vector4.zero;
        Text.ForceMeshUpdate();

        // TMP centers line metrics, not necessarily the visible ink. Accents,
        // descenders, bearings and multiline blocks need this final glyph pass.
        Vector2 minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        bool visible = false;
        for (int index = 0; index < Text.textInfo.characterCount; index++)
        {
            TMP_CharacterInfo glyph = Text.textInfo.characterInfo[index];
            if (!glyph.isVisible) continue;
            Vector2 bottom = rect.parent.InverseTransformPoint(rect.TransformPoint(glyph.bottomLeft));
            Vector2 top = rect.parent.InverseTransformPoint(rect.TransformPoint(glyph.topRight));
            minimum = Vector2.Min(minimum, Vector2.Min(bottom, top));
            maximum = Vector2.Max(maximum, Vector2.Max(bottom, top));
            visible = true;
        }
        if (!visible) return; // Never manufacture bounds for an inactive/empty mesh.
        rect.anchoredPosition += SafeRect.center - (minimum + maximum) * 0.5f;
        Text.ForceMeshUpdate();
        lastText = Text.text;
        lastPosition = rect.anchoredPosition;
        applied = true;
    }
}

// Sole Home presentation owner on MainMenu.
//
// The approved cartoon composition is built from modular production sprites and
// live TMP. Existing gameplay/navigation buttons stay callback-authoritative;
// Home exposes one PLAY gateway and one Daily Hunt event card. Mode selection
// is owned separately by MainMenuPlayVisuals on the existing PanelPlay.
[DefaultExecutionOrder(1600)]
public sealed class MainMenuHomeVisuals : MonoBehaviour
{
    public const string VisualRootName = "HomeVisualRoot";
    public const string SafeRootName = "HomeSafeAreaRoot";
    public const string BackgroundName = "HomeBackground";
    public const string DecorationsName = "HomeDecorations";
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
    public const string DailyIconName = "HomeDailyIcon";
    public const string DailyGiftName = "HomeDailyGift";
    public const string PromoName = "HomeDailyPromo";
    public const string PortalName = "HomePortal";
    public const string MascotSixName = "HomeMascotSix";
    public const string MascotSevenName = "HomeMascotSeven";

    // The accepted Solo VS AI screen is the material and depth authority for
    // Home.  Its background is authored at the production portrait aspect and
    // its transparent decoration layer provides the same restrained neon
    // confetti without reviving the retired Home chrome.
    const string BackgroundResource = "solo/production/solo_background_v1";
    const string DecorationsResource =
        "solo/production/solo_decorations_v1";
    const string LogoResource = "reference/hol_logo_exact";
    const string AvatarResource =
        PlayerProfileAvatarResolver.FallbackResourcePath;
    const string HeroBoyResource = "phase2a/hol_menu_boy_arms_crossed_r3";
    const string HeroGirlResource = "phase2a/hol_menu_girl_forward_fist_r3";
    const string MascotSixResource = "reference/mascot_6_exact";
    const string MascotSevenResource = "reference/mascot_7_exact";
    const string TrophyResource =
        "dailyhunt/production/daily_mission_icon_trophy";
    const string DailyResource = "phase2a/hol_mode_daily_r2";
    const string DailyGiftResource =
        "mainmenu/mainmenu_daily_gift_reference_v1";
    const string SpeechBubbleResource = "cartoon/cartoon_speech_bubble_raster";
    const string OuterFrameResource =
        "mainmenu/mainmenu_outer_frame_reference_v1";
    const string PortalResource = "dailyhunt/production/daily_floor_portal";
    const string PromoStarResource = "dailyhunt/production/daily_player_star";
    const string StarsResource = "mainmenu/mainmenu_deco_stars";
    const string ConfettiResource = "mainmenu/mainmenu_deco_confetti";

    const string PlayCardResource =
        "solo/production/solo_player_card_shell_v1";
    const string DailyCardResource =
        "solo/production/solo_opponent_card_shell_v1";
    const string PromoFrameResource =
        "solo/production/solo_prompt_ribbon_v1";
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

    public static readonly string[] LoadedResources =
    {
        BackgroundResource,
        DecorationsResource,
        LogoResource,
        AvatarResource,
        HeroBoyResource,
        HeroGirlResource,
        MascotSixResource,
        MascotSevenResource,
        TrophyResource,
        DailyResource,
        DailyGiftResource,
        SpeechBubbleResource,
        OuterFrameResource,
        PortalResource,
        PromoStarResource,
        StarsResource,
        ConfettiResource,
        PlayCardResource,
        DailyCardResource,
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
    RectTransform outerFrameRect;
    RectTransform safeRoot;
    RectTransform logoRect;
    RectTransform heroBoyRect;
    RectTransform heroGirlRect;
    RectTransform speechBubbleRect;
    RectTransform gearRect;
    RectTransform chipRect;
    RectTransform playButtonRect;
    RectTransform dailyButtonRect;
    RectTransform promoRect;
    RectTransform portalRect;
    RectTransform mascotSixRect;
    RectTransform mascotSevenRect;

    TMP_FontAsset displayFont;
    TMP_FontAsset bodyFont;
    Image chipAvatarImage;
    TMP_Text chipText;
    TMP_Text chipScoreText;
    TMP_Text speechText;
    TMP_Text promoTitleText;
    TMP_Text promoBodyText;
    PvpGameController pvpController;
    bool laidOut;
    int lastLayoutWidth = -1;
    int lastLayoutHeight = -1;
    L10n.Language lastLanguage;

    public bool IsReady { get; private set; }
    public bool IsSettled { get; private set; }
    internal MainMenuCenteredTextRegion[] CenteredTextRegions { get; private set; }

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
        CenterVisibleText();
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
        Sprite decorations = LoadRequired(DecorationsResource);
        Sprite logo = LoadRequired(LogoResource);
        Sprite avatar = LoadRequired(AvatarResource);
        Sprite heroBoy = LoadRequired(HeroBoyResource);
        Sprite heroGirl = LoadRequired(HeroGirlResource);
        Sprite six = LoadRequired(MascotSixResource);
        Sprite seven = LoadRequired(MascotSevenResource);
        Sprite trophy = LoadRequired(TrophyResource);
        Sprite daily = LoadRequired(DailyResource);
        Sprite dailyGift = LoadRequired(DailyGiftResource);
        Sprite speech = LoadRequired(SpeechBubbleResource);
        Sprite outerFrame = LoadRequired(OuterFrameResource);
        Sprite portal = LoadRequired(PortalResource);
        Sprite promoStar = LoadRequired(PromoStarResource);
        Sprite stars = LoadRequired(StarsResource);
        Sprite confetti = LoadRequired(ConfettiResource);
        Sprite playCard = LoadRequired(PlayCardResource);
        Sprite dailyCard = LoadRequired(DailyCardResource);
        Sprite promoFrame = LoadRequired(PromoFrameResource);
        Sprite chipFrame = LoadRequired(ChipFrameResource);
        Sprite avatarRing = LoadRequired(ChipAvatarRingResource);
        Sprite gear = LoadRequired(GearResource);
        displayFont = Resources.Load<TMP_FontAsset>(DisplayFontResource);
        bodyFont = Resources.Load<TMP_FontAsset>(BodyFontResource);

        IsReady = ArtReady(
            background, decorations, logo, avatar, heroBoy, heroGirl, six,
            seven, trophy,
            daily, dailyGift, speech, outerFrame, portal, promoStar, stars,
            confetti, playCard, dailyCard, promoFrame, chipFrame, avatarRing,
            gear) &&
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

        var decorationsImage = EnsureImage(visualRoot, DecorationsName);
        Stretch(decorationsImage.rectTransform);
        ConfigureImage(
            decorationsImage, decorations, false, Image.Type.Simple);

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
        outerFrameRect = outer.rectTransform;
        Place(outerFrameRect, Vector2.zero,
            new Vector2(ReferenceWidth, ReferenceHeight));
        // Compatibility node retained for cross-screen test discovery only.
        // The previous artwork contains opaque top/bottom chrome and is not
        // part of the VS-AI-derived Home composition.
        outer.gameObject.SetActive(false);

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

        speechText = EnsureText(
            bubble.transform, "HomeSpeechText", 32f, displayFont, Ink,
            TextAlignmentOptions.Center);
        StretchText(speechText.rectTransform, 34f, 32f);
        ConfigureBodyText(speechText, 25f, 34f);
        SetLocalized(speechText, "home_hero_speech");

        // Keep the established compatibility nodes discoverable while the new
        // Home card composition has one clear information owner.
        bubble.gameObject.SetActive(false);

        playButtonRect = RestyleCta(
            safeRoot, FindButton("ButtonPlay"), playCard, null,
            null, "HomePlayTitle", "play",
            "HomePlaySubtitle", "home_play_subtitle", true, NearWhite);

        // The existing approved heroes now live inside the PLAY card's framed
        // artwork region.  They remain presentation-only and cannot intercept
        // the real ButtonPlay hit target.
        Reparent(heroImage.transform, playButtonRect);
        Place(heroBoyRect, new Vector2(-77f, 65f), new Vector2(376f, 376f));
        heroImage.transform.SetAsFirstSibling();
        Reparent(heroineImage.transform, playButtonRect);
        Place(heroGirlRect, new Vector2(80f, 60f), new Vector2(376f, 376f));
        heroineImage.transform.SetSiblingIndex(1);

        // PanelPlay owns the one real private-room entry. Suppress the injected
        // button here until that owner reparents it into the selector.
        Button selectorPvp = FindButton("ButtonPvP");
        Transform playVisualRoot = DeepFind(canvas.transform, "PlayVisualRoot");
        if (selectorPvp != null &&
            (playVisualRoot == null ||
             !selectorPvp.transform.IsChildOf(playVisualRoot)))
            selectorPvp.gameObject.SetActive(false);
        HideNamed("ButtonPrivateRoom");

        dailyButtonRect = RestyleCta(
            safeRoot, FindButton("DailyHuntButton"), dailyCard, daily,
            DailyIconName, "HomeDailyTitle", "home_daily_title",
            "HomeDailySubtitle", "home_daily_subtitle", false,
            NearWhite);

        var dailyGiftImage = EnsureImage(dailyButtonRect, DailyGiftName);
        ConfigureImage(dailyGiftImage, dailyGift, true, Image.Type.Simple);
        Place(dailyGiftImage.rectTransform, new Vector2(170f, -129f),
            new Vector2(92f, 88f));
        // The target-board artwork is the sole mode illustration. Retain the
        // approved legacy gift node for compatibility without letting a second
        // icon compete with or cover the card's information hierarchy.
        dailyGiftImage.gameObject.SetActive(false);

        BuildBottomPromo(
            safeRoot, promoFrame, trophy, promoStar, six, seven, portal);

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

        // The accepted VS AI actor-card shell is the real Button targetGraphic.
        // Its authored bevel, rim, rays and depth remain fully visible in every
        // state; no procedural or transparent replacement sits above it.
        image.enabled = true;
        image.sprite = frame;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = true;
        button.targetGraphic = image;
        ConfigureButtonState(button);
        var juice = RuntimeUI.AttachJuice(button);
        rect.localScale = Vector3.one;
        if (juice != null)
            juice.ResetBaseScale(Vector3.one);

        if (icon != null)
        {
            var iconImage = EnsureImage(button.transform, iconName);
            ConfigureImage(iconImage, icon, true, Image.Type.Simple);
            Place(
                iconImage.rectTransform,
                new Vector2(0f, 65f),
                new Vector2(410f, 384f));
            iconImage.transform.SetAsFirstSibling();
        }

        var title = EnsureText(
            button.transform, titleName, primary ? 54f : 48f,
            displayFont, labelColor, TextAlignmentOptions.Center);
        Place(
            title.rectTransform,
            primary ? new Vector2(0f, 354f) : new Vector2(7f, 354f),
            primary ? new Vector2(246f, 100f) : new Vector2(210f, 95f));
        ConfigureDisplayText(
            title, primary ? 50f : 27f, primary ? 64f : 34f);
        title.enableWordWrapping = !primary;
        title.lineSpacing = primary ? 0f : -8f;
        title.overflowMode = TextOverflowModes.Truncate;
        SetLocalized(title, titleKey);

        var subtitle = EnsureText(
            button.transform, subtitleName, primary ? 31f : 29f,
            displayFont, NearWhite,
            TextAlignmentOptions.Center);
        Place(
            subtitle.rectTransform,
            new Vector2(0f, -312f), new Vector2(primary ? 300f : 364f, 145f));
        ConfigureDisplayText(
            subtitle, primary ? 25f : 24f, primary ? 34f : 32f);
        subtitle.enableWordWrapping = true;
        subtitle.lineSpacing = -4f;
        subtitle.overflowMode = TextOverflowModes.Truncate;
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
            trophyImage.rectTransform, new Vector2(-220f, -18f),
            new Vector2(62f, 62f));

        var leftStar = EnsureImage(promo.transform, "HomePromoStarLeft");
        ConfigureImage(leftStar, star, true, Image.Type.Simple);
        Place(leftStar.rectTransform, new Vector2(-214f, 42f),
            new Vector2(34f, 34f));

        var rightStar = EnsureImage(promo.transform, "HomePromoStarRight");
        ConfigureImage(rightStar, star, true, Image.Type.Simple);
        Place(rightStar.rectTransform, new Vector2(232f, 38f),
            new Vector2(34f, 34f));

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
            chipAvatarImage.sprite = PlayerProfileAvatarResolver.Resolve();

        if (chipText == null || chipScoreText == null) return;

        string player = PlayerPrefs.GetString("PlayerName", "");
        if (string.IsNullOrWhiteSpace(player))
            player = L10n.Get("player_default");

        chipText.text = player;
        chipScoreText.text = GameStats.Wins.ToString("N0");
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
        if (outerFrameRect == null || logoRect == null ||
            heroBoyRect == null || heroGirlRect == null ||
            speechBubbleRect == null || playButtonRect == null ||
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

        // The compatibility bezel stays inactive.  Size it deterministically
        // for tests without allowing its retired horizontal chrome to render.
        float visibleReferenceHeight = ReferenceWidth * aspect;
        Place(outerFrameRect, Vector2.zero,
            new Vector2(ReferenceWidth, visibleReferenceHeight));

        Place(
            gearRect, new Vector2(-454f, 838f + 45f * tall),
            new Vector2(118f, 118f));
        Place(
            chipRect, new Vector2(330f, 838f + 45f * tall),
            new Vector2(370f, 150f));
        Place(
            logoRect, new Vector2(0f, 600f + 40f * tall),
            new Vector2(512.3f, 304.11f));

        // Enlarge the authored card rects, not the Canvas or a transform scale.
        // The sprite's transparent side gutters can share layout space; inset
        // raycasts keep the two visible interactive surfaces independently owned.
        Place(
            playButtonRect, new Vector2(-260f, -100f + 18f * tall),
            new Vector2(560f, 1140f));
        Place(
            dailyButtonRect, new Vector2(260f, -100f + 18f * tall),
            new Vector2(560f, 1140f));
        playButtonRect.GetComponent<Image>().raycastPadding = new Vector4(40f, 0f, 40f, 0f);
        dailyButtonRect.GetComponent<Image>().raycastPadding = new Vector4(40f, 0f, 40f, 0f);

        // The paired art region grows from 381 to 465 reference pixels high.
        // Its width is already constrained: enlarge each portrait modestly and
        // stagger vertically so the girl's hair cannot cover the boy's eyes.
        // Both full silhouettes remain inside PLAY's framed art aperture.
        Place(
            heroBoyRect, new Vector2(-98f, 115f),
            new Vector2(410f, 410f));
        Place(
            heroGirlRect, new Vector2(58f, 60f),
            new Vector2(410f, 410f));

        Transform dailyArt = DeepFind(dailyButtonRect, DailyIconName);
        if (dailyArt != null)
            Place((RectTransform)dailyArt, new Vector2(0f, 82f),
                new Vector2(500f, 468f));

        // Retained inactive compatibility node; it has no visual ownership.
        Place(
            speechBubbleRect, new Vector2(0f, 430f),
            new Vector2(300f, 200f));
        Place(
            promoRect, new Vector2(0f, -748f - 18f * tall),
            new Vector2(600f, 182f));
        Place(
            portalRect, new Vector2(0f, -890f - 62f * tall),
            new Vector2(610f, 165f));
        Place(
            mascotSixRect, new Vector2(-398f, -780f - 42f * tall),
            new Vector2(230f, 255f));
        Place(
            mascotSevenRect, new Vector2(398f, -780f - 42f * tall),
            new Vector2(220f, 255f));

        // Greek needs the full title bounds, never a tiny-font fallback.
        foreach (string textName in new[]
        {
            "HomePlayTitle",
            "HomeDailyTitle",
        })
        {
            Transform found = DeepFind(safeRoot, textName);
            TMP_Text text = found == null ? null : found.GetComponent<TMP_Text>();
            if (text == null) continue;
            text.fontSizeMin = textName == "HomePlayTitle"
                ? 50f
                : language == L10n.Language.Greek ? 27f : 29f;
            text.fontSizeMax = textName == "HomePlayTitle" ? 72f : 38f;
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
            promoTitleText.fontSizeMin = 22f;
            promoTitleText.fontSizeMax = 29f;
            promoTitleText.enableWordWrapping = false;
            promoTitleText.overflowMode = TextOverflowModes.Truncate;
            promoTitleText.lineSpacing = 0f;
            Place(promoTitleText.rectTransform, new Vector2(18f, 43f),
                new Vector2(382f, 34f));
        }

        if (promoBodyText != null)
        {
            promoBodyText.alignment = TextAlignmentOptions.Center;
            promoBodyText.enableAutoSizing = true;
            promoBodyText.fontSizeMin = 20f;
            promoBodyText.fontSizeMax = 28f;
            promoBodyText.enableWordWrapping = false;
            promoBodyText.overflowMode = TextOverflowModes.Truncate;
            promoBodyText.lineSpacing = 0f;
            Place(promoBodyText.rectTransform, new Vector2(18f, -10f),
                new Vector2(382f, 64f));
        }

        if (chipText != null)
        {
            chipText.alignment = TextAlignmentOptions.Center;
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
            chipScoreText.alignment = TextAlignmentOptions.Center;
            chipScoreText.enableAutoSizing = true;
            chipScoreText.fontSizeMin = 30f;
            chipScoreText.fontSizeMax = 40f;
            chipScoreText.enableWordWrapping = false;
            chipScoreText.overflowMode = TextOverflowModes.Overflow;
            Place(chipScoreText.rectTransform, new Vector2(108f, -30f),
                new Vector2(164f, 52f));
        }

        ConfigureCtaTypography(
            "HomePlayTitle", "HomePlaySubtitle", true);
        ConfigureCtaTypography(
            "HomeDailyTitle", "HomeDailySubtitle", false);

        // Regions follow the visible faces in the approved sprites, not their
        // asymmetric transparent gutters. Card-local vertical regions follow
        // the taller tab and inset faces; the approved glyph-centering math and
        // its strict containment/centre guarantees remain unchanged.
        CenteredTextRegions = new[]
        {
            new MainMenuCenteredTextRegion(chipText, 72f, 29f, 220f, 48f),
            new MainMenuCenteredTextRegion(chipScoreText, 108f, -30f, 164f, 52f),
            new MainMenuCenteredTextRegion(FindText("HomePlayTitle"), -16f, 448.4f, 222f, 101.3f),
            new MainMenuCenteredTextRegion(FindText("HomeDailyTitle"), 7f, 448.4f, 210f, 101.3f),
            new MainMenuCenteredTextRegion(FindText("HomePlaySubtitle"), -16f, -342f, 340f, 183.6f),
            new MainMenuCenteredTextRegion(FindText("HomeDailySubtitle"), 7f, -342f, 364f, 183.6f),
            new MainMenuCenteredTextRegion(promoTitleText, 18f, 43f, 382f, 34f),
            new MainMenuCenteredTextRegion(promoBodyText, 18f, -10f, 382f, 64f),
        };
        CenterVisibleText();
    }

    void CenterVisibleText()
    {
        if (CenteredTextRegions == null) return;
        foreach (MainMenuCenteredTextRegion region in CenteredTextRegions)
            region.Apply();
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
            title.alignment = TextAlignmentOptions.Center;
            title.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            title.enableAutoSizing = true;
            title.fontSize = primary ? 72f : 38f;
            title.fontSizeMin = primary ? 50f :
                L10n.Current == L10n.Language.Greek ? 27f : 29f;
            title.fontSizeMax = primary ? 72f : 38f;
            title.outlineColor = Ink;
            title.outlineWidth = 0.16f;
            title.enableWordWrapping = !primary;
            title.overflowMode = TextOverflowModes.Truncate;
            Place(title.rectTransform,
                daily ? new Vector2(7f, 448.4f) : new Vector2(-16f, 448.4f),
                daily ? new Vector2(210f, 120f) : new Vector2(222f, 126f));
            title.lineSpacing = 0f;
        }

        if (subtitle != null)
        {
            subtitle.color = NearWhite;
            subtitle.outlineColor = Ink;
            subtitle.outlineWidth = 0.12f;
            subtitle.alignment = TextAlignmentOptions.Center;
            subtitle.enableAutoSizing = true;
            subtitle.fontSizeMin = primary ? 25f : 24f;
            subtitle.fontSizeMax = primary ? 40f : 38f;
            subtitle.enableWordWrapping = true;
            subtitle.overflowMode = TextOverflowModes.Truncate;
            subtitle.lineSpacing = 0f;
            Place(subtitle.rectTransform, new Vector2(primary ? -16f : 7f, -342f),
                new Vector2(primary ? 340f : 364f, 183.6f));
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
        colors.pressedColor = new Color(0.80f, 0.84f, 0.94f, 1f);
        colors.disabledColor = new Color(0.56f, 0.58f, 0.68f, 0.72f);
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
