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
    public const string SpeechBubbleName = "HomeSpeechBubble";
    public const string ChipName = "HomePlayerChip";
    public const string ChipTextName = "HomePlayerChipText";
    public const string SoloIconName = "HomeSoloIcon";
    public const string PvpIconName = "HomePvpIcon";
    public const string FriendIconName = "HomeFriendIcon";
    public const string DailyIconName = "HomeDailyIcon";
    public const string PromoName = "HomeDailyPromo";
    public const string MascotSixName = "HomeMascotSix";
    public const string MascotSevenName = "HomeMascotSeven";
    public const string FriendButtonName = "ButtonPrivateRoom";

    const string BackgroundResource = CartoonUiKit.HomeBackground;
    const string LogoResource = CartoonUiKit.Logo;
    const string AvatarResource = CartoonUiKit.PlayerAvatar;
    const string HeroBoyResource = CartoonUiKit.PlayerAvatar;
    const string MascotSixResource = CartoonUiKit.MascotSix;
    const string MascotSevenResource = CartoonUiKit.MascotSeven;
    const string TrophyResource = CartoonUiKit.HomeTrophy;
    const string VsResource = CartoonUiKit.HomeVs;
    const string FriendResource = CartoonUiKit.HomeFriends;
    const string SoloResource = CartoonUiKit.HomeTrophy;
    const string DailyResource = CartoonUiKit.HomeTarget;
    const string DailyGiftResource = CartoonUiKit.HomeGift;
    const string SpeechBubbleResource = CartoonUiKit.SpeechBubble;
    const string StarsResource = "mainmenu/mainmenu_deco_stars";
    const string ConfettiResource = "mainmenu/mainmenu_deco_confetti";
    const string OuterFrameResource = CartoonUiKit.ScreenFrame;

    const string GoldCtaResource = CartoonUiKit.GoldCta;
    const string MagentaCtaResource = CartoonUiKit.MagentaCta;
    const string BlueCtaResource = CartoonUiKit.CyanCta;
    const string PurpleFrameResource = CartoonUiKit.PurplePanel;
    const string ChipFrameResource = CartoonUiKit.PlayerChip;
    const string ChipAvatarRingResource = CartoonUiKit.PlayerAvatarRing;
    const string GearResource = "phase2a/hol_settings_gear_r2";
    const string DisplayFontResource = CartoonUiKit.DisplayFont;
    const string BodyFontResource = CartoonUiKit.BodyFont;

    const float ReferenceWidth = 1080f;
    const float ReferenceHeight = 1920f;

    static readonly Color Ink = new Color(0.09f, 0.05f, 0.16f, 1f);
    static readonly Color NearWhite = new Color(0.985f, 0.975f, 1f, 1f);
    static readonly Color Cyan = new Color(0.18f, 0.92f, 1f, 1f);
    static readonly Color Gold = new Color(1f, 0.80f, 0.20f, 1f);
    static readonly Color Muted = new Color(0.87f, 0.84f, 0.96f, 0.90f);

    public static readonly string[] LoadedResources =
    {
        BackgroundResource,
        LogoResource,
        AvatarResource,
        HeroBoyResource,
        MascotSixResource,
        MascotSevenResource,
        TrophyResource,
        VsResource,
        FriendResource,
        SoloResource,
        DailyResource,
        DailyGiftResource,
        SpeechBubbleResource,
        StarsResource,
        ConfettiResource,
        OuterFrameResource,
        GoldCtaResource,
        MagentaCtaResource,
        BlueCtaResource,
        PurpleFrameResource,
        ChipFrameResource,
        GearResource,
    };

    public static readonly string[] LoadedFontResources =
    {
        DisplayFontResource,
        BodyFontResource,
    };

    RectTransform visualRoot;
    RectTransform safeRoot;
    RectTransform outerFrameRect;
    RectTransform logoRect;
    RectTransform heroBoyRect;
    RectTransform speechBubbleRect;
    RectTransform gearRect;
    RectTransform chipRect;
    RectTransform soloButtonRect;
    RectTransform pvpButtonRect;
    RectTransform friendButtonRect;
    RectTransform dailyButtonRect;
    RectTransform promoRect;
    RectTransform mascotSixRect;
    RectTransform mascotSevenRect;

    TMP_FontAsset displayFont;
    TMP_FontAsset bodyFont;
    TMP_Text chipText;
    TMP_Text speechText;
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
            // BuildHome has already created and measured the complete visual
            // tree.  Publish readiness before the optional frame barriers so
            // headless PlayMode runners cannot observe a false negative when
            // WaitForEndOfFrame is not serviced promptly.  The barriers still
            // run for normal runtime/capture callers before their own capture.
            Canvas.ForceUpdateCanvases();
            IsSettled = true;
            laidOut = true;

            // Two presentation barriers let layout, fonts and localized text
            // settle before screenshot capture or user input.
            yield return null;
            yield return new WaitForEndOfFrame();
        }

        if (!IsReady)
        {
            IsSettled = false;
            laidOut = true;
        }
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
        Sprite heroBoy = LoadRequired(HeroBoyResource);
        Sprite six = LoadRequired(MascotSixResource);
        Sprite seven = LoadRequired(MascotSevenResource);
        Sprite trophy = LoadRequired(TrophyResource);
        Sprite vs = LoadRequired(VsResource);
        Sprite friend = LoadRequired(FriendResource);
        Sprite solo = LoadRequired(SoloResource);
        Sprite daily = LoadRequired(DailyResource);
        Sprite dailyGift = LoadRequired(DailyGiftResource);
        Sprite speech = LoadRequired(SpeechBubbleResource);
        Sprite stars = LoadRequired(StarsResource);
        Sprite confetti = LoadRequired(ConfettiResource);
        Sprite outerFrame = LoadRequired(OuterFrameResource);
        Sprite goldFrame = LoadRequired(GoldCtaResource);
        Sprite magentaFrame = LoadRequired(MagentaCtaResource);
        Sprite blueFrame = LoadRequired(BlueCtaResource);
        Sprite purpleFrame = LoadRequired(PurpleFrameResource);
        Sprite chipFrame = LoadRequired(ChipFrameResource);
        Sprite chipAvatarRing = LoadRequired(ChipAvatarRingResource);
        Sprite gear = LoadRequired(GearResource);
        displayFont = Resources.Load<TMP_FontAsset>(DisplayFontResource);
        bodyFont = Resources.Load<TMP_FontAsset>(BodyFontResource);

        IsReady = ArtReady(
            background, logo, avatar, heroBoy, six, seven, trophy, vs,
            friend, solo, daily, dailyGift, speech, stars, confetti, goldFrame,
            outerFrame, magentaFrame, blueFrame, purpleFrame, chipFrame,
            chipAvatarRing, gear) &&
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

        var confettiImage = EnsureImage(visualRoot, ConfettiName);
        Stretch(confettiImage.rectTransform);
        ConfigureImage(confettiImage, confetti, false, Image.Type.Simple);

        var outer = EnsureImage(visualRoot, OuterFrameName);
        ConfigureImage(outer, outerFrame, false, Image.Type.Simple);
        outerFrameRect = outer.rectTransform;
        Place(outerFrameRect, Vector2.zero, new Vector2(1056f, 1888f));

        safeRoot = EnsureRect(visualRoot, SafeRootName);
        Stretch(safeRoot);
        ResponsiveSafeAreaRoot.Attach(
            safeRoot, (RectTransform)canvas.transform,
            new Vector2(ReferenceWidth, ReferenceHeight));

        BuildTopBar(
            safeRoot, gear, chipFrame, chipAvatarRing, avatar, trophy);

        var logoImage = EnsureImage(safeRoot, LogoName);
        ConfigureImage(logoImage, logo, true, Image.Type.Simple);
        logoRect = logoImage.rectTransform;

        var heroImage = EnsureImage(safeRoot, HeroBoyName);
        ConfigureImage(heroImage, heroBoy, true, Image.Type.Simple);
        heroBoyRect = heroImage.rectTransform;

        var bubble = EnsureImage(safeRoot, SpeechBubbleName);
        ConfigureImage(bubble, speech, false, Image.Type.Simple);
        speechBubbleRect = bubble.rectTransform;

        speechText = EnsureText(
            bubble.transform, "HomeSpeechText", 32f, bodyFont, Ink,
            TextAlignmentOptions.Center);
        StretchText(speechText.rectTransform, 34f, 32f);
        ConfigureBodyText(speechText, 25f, 34f);
        SetLocalized(speechText, "home_speech_body");

        soloButtonRect = RestyleCta(
            safeRoot, FindButton("ButtonPlay"), goldFrame, solo,
            SoloIconName, "HomeSoloTitle", "home_solo_title",
            "HomeSoloSubtitle", "home_solo_subtitle", true, NearWhite);

        pvpButtonRect = RestyleCta(
            safeRoot, FindButton("ButtonPvP"), magentaFrame, vs,
            PvpIconName, "HomePvpTitle", "home_pvp_title",
            "HomePvpSubtitle", "home_pvp_subtitle", false, NearWhite);

        friendButton = EnsureFriendButton(safeRoot);
        friendButtonRect = RestyleCta(
            safeRoot, friendButton, blueFrame, friend,
            FriendIconName, "HomeFriendTitle", "home_friend_title",
            "HomeFriendSubtitle", "home_friend_subtitle", false,
            NearWhite);

        dailyButtonRect = RestyleCta(
            safeRoot, FindButton("DailyHuntButton"), purpleFrame, daily,
            DailyIconName, "HomeDailyTitle", "home_daily_title",
            "HomeDailySubtitle", "home_daily_subtitle", false,
            NearWhite);
        if (dailyButtonRect != null)
        {
            var giftImage = EnsureImage(
                dailyButtonRect, "HomeDailyGiftIcon");
            ConfigureImage(
                giftImage, dailyGift, true, Image.Type.Simple);
            Place(
                giftImage.rectTransform, new Vector2(350f, 0f),
                new Vector2(190f, 190f));
        }

        BuildBottomPromo(safeRoot, purpleFrame, trophy, six, seven);

        // Home owns every direct child of this safe root.  Friend is created
        // through RuntimeUI for its callback/localization plumbing, which
        // also registers a generic ResponsivePageLayout.  Leaving that
        // second writer alive lets it restore the construction position (the
        // origin) after this owner places the four CTA rows, most visibly on
        // tall viewports where it covers the PvP row.  Remove only the
        // Home-local generic writer; the shared infrastructure remains in
        // place for screens that still use it.
        var genericHomeLayout = safeRoot.GetComponent<ResponsivePageLayout>();
        if (genericHomeLayout != null)
            RuntimeUI.DestroyNow(genericHomeLayout);

        ApplyResponsiveLayout(true);
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

        var avatarImage = EnsureImage(chip.transform, "HomePlayerAvatar");
        ConfigureImage(avatarImage, avatar, true, Image.Type.Simple);
        Place(
            avatarImage.rectTransform, new Vector2(-112f, 6f),
            new Vector2(106f, 106f));

        var avatarRingImage = EnsureImage(
            chip.transform, "HomePlayerAvatarRing");
        ConfigureImage(
            avatarRingImage, avatarRing, true, Image.Type.Simple);
        Place(
            avatarRingImage.rectTransform, new Vector2(-112f, 6f),
            new Vector2(112f, 112f));
        avatarRingImage.transform.SetAsLastSibling();

        var trophyImage = EnsureImage(chip.transform, "HomeTrophyIcon");
        ConfigureImage(trophyImage, trophy, true, Image.Type.Simple);
        Place(
            trophyImage.rectTransform, new Vector2(-14f, -34f),
            new Vector2(46f, 46f));

        chipText = EnsureText(
            chip.transform, ChipTextName, 32f, displayFont, NearWhite,
            TextAlignmentOptions.Center);
        Place(
            chipText.rectTransform, new Vector2(59f, 6f),
            new Vector2(188f, 106f));
        chipText.enableAutoSizing = false;
        chipText.fontSize = 32f;
        chipText.overflowMode = TextOverflowModes.Ellipsis;
    }

    Button EnsureFriendButton(Transform parent)
    {
        Transform existing = DeepFind(transform, FriendButtonName);
        Button button = existing == null ? null : existing.GetComponent<Button>();
        if (button == null)
        {
            button = RuntimeUI.CreateButton(
                parent, FriendButtonName, L10n.Get("private_room_title"),
                Vector2.zero, new Vector2(930f, 160f), Color.white,
                NearWhite);
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

        ConfigureInteractiveImage(image, frame, false, Image.Type.Simple, 1f);
        button.targetGraphic = image;
        ConfigureButtonState(button);
        RuntimeUI.AttachJuice(button);

        if (icon != null)
        {
            var iconImage = EnsureImage(button.transform, iconName);
            ConfigureImage(iconImage, icon, true, Image.Type.Simple);
            Place(
                iconImage.rectTransform, new Vector2(-350f, 0f),
                new Vector2(primary ? 230f : 210f, primary ? 230f : 210f));
        }

        var title = EnsureText(
            button.transform, titleName, primary ? 64f : 58f,
            displayFont, labelColor, TextAlignmentOptions.Center);
        Place(
            title.rectTransform, new Vector2(60f, 25f),
            new Vector2(690f, 70f));
        ConfigureDisplayText(
            title, primary ? 46f : 40f, primary ? 64f : 60f);
        SetLocalized(title, titleKey);

        var subtitle = EnsureText(
            button.transform, subtitleName, primary ? 31f : 29f,
            bodyFont, primary ? Ink : Muted,
            TextAlignmentOptions.Center);
        Place(
            subtitle.rectTransform, new Vector2(60f, -17f),
            new Vector2(690f, 50f));
        ConfigureBodyText(
            subtitle, primary ? 25f : 23f, primary ? 32f : 30f);
        SetLocalized(subtitle, subtitleKey);

        return rect;
    }

    void BuildBottomPromo(
        Transform safe,
        Sprite frame,
        Sprite trophy,
        Sprite six,
        Sprite seven)
    {
        var promo = EnsureImage(safe, PromoName);
        ConfigureImage(promo, frame, false, Image.Type.Simple);
        promoRect = promo.rectTransform;

        var trophyImage = EnsureImage(promo.transform, "HomePromoTrophy");
        ConfigureImage(trophyImage, trophy, true, Image.Type.Simple);
        Place(
            trophyImage.rectTransform, new Vector2(-125f, -35f),
            new Vector2(66f, 66f));

        var promoTitle = EnsureText(
            promo.transform, "HomePromoTitle", 31f, displayFont, Cyan,
            TextAlignmentOptions.Center);
        Place(
            promoTitle.rectTransform, new Vector2(20f, 29f),
            new Vector2(440f, 50f));
        ConfigureDisplayText(promoTitle, 24f, 32f);
        SetLocalized(promoTitle, "home_promo_title");

        var promoBody = EnsureText(
            promo.transform, "HomePromoBody", 27f, bodyFont, Gold,
            TextAlignmentOptions.Center);
        Place(
            promoBody.rectTransform, new Vector2(42f, -35f),
            new Vector2(340f, 54f));
        ConfigureBodyText(promoBody, 21f, 28f);
        SetLocalized(promoBody, "home_promo_body");

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
    }

    void RefreshChip()
    {
        if (chipText == null) return;

        string player = PlayerPrefs.GetString("PlayerName", "");
        if (string.IsNullOrWhiteSpace(player))
            player = L10n.Get("player_default");

        chipText.text = "<b>" + player + "</b>\n<size=95%>" +
                        GameStats.Wins.ToString("N0") + "</size>";
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
        if (outerFrameRect == null || logoRect == null || heroBoyRect == null ||
            speechBubbleRect == null || soloButtonRect == null ||
            pvpButtonRect == null || friendButtonRect == null ||
            dailyButtonRect == null || promoRect == null ||
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

        // The approved 1080x1920 silhouette is unchanged at tall == 0. On
        // taller phones the bezel itself expands with the viewport instead of
        // leaving a centered 9:16 frame floating inside unused space.
        Place(
            outerFrameRect, Vector2.zero,
            new Vector2(1056f, 1888f + 480f * tall));

        Place(
            gearRect, new Vector2(-432f, 833f + 165f * tall),
            new Vector2(126f, 126f));
        Place(
            chipRect, new Vector2(350f, 829f + 165f * tall),
            new Vector2(350f, 150f));
        Place(
            logoRect, new Vector2(-62f, 797f + 110f * tall),
            new Vector2(560f, 300f));
        Place(
            heroBoyRect, new Vector2(-66f, 362f + 62f * tall),
            new Vector2(600f, 600f));
        Place(
            speechBubbleRect, new Vector2(292f, 382f + 62f * tall),
            new Vector2(300f, 190f));

        float buttonShift = 30f * tall;
        Place(
            soloButtonRect, new Vector2(0f, 114f + buttonShift),
            new Vector2(930f, 188f));
        Place(
            pvpButtonRect, new Vector2(0f, -94f + buttonShift),
            new Vector2(930f, 188f));
        Place(
            friendButtonRect, new Vector2(0f, -302f + buttonShift),
            new Vector2(930f, 188f));
        Place(
            dailyButtonRect, new Vector2(0f, -510f + buttonShift),
            new Vector2(930f, 188f));
        Place(
            promoRect, new Vector2(0f, -735f - 30f * tall),
            new Vector2(520f, 200f));
        Place(
            mascotSixRect, new Vector2(-380f, -744f - 45f * tall),
            new Vector2(280f, 320f));
        Place(
            mascotSevenRect, new Vector2(370f, -753f - 45f * tall),
            new Vector2(300f, 300f));

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
            text.fontSizeMin = language == L10n.Language.Greek ? 32f : 34f;
            text.fontSizeMax = textName == "HomeSoloTitle" ? 54f : 48f;
        }
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
