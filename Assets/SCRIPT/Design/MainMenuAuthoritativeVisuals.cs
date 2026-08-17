using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Sole presentation owner for the Main Menu Home checkpoint. It skins and
// reparents the four existing controls, but never creates an interaction,
// gameplay panel, Canvas, or callback.
[DefaultExecutionOrder(-2000)]
public sealed class MainMenuAuthoritativeVisuals : MonoBehaviour
{
    const float PollSeconds = 0.25f;

    static readonly Color NearWhite = new Color(0.91f, 0.93f, 1f, 1f);
    static readonly Color Ink = new Color(0.08f, 0.04f, 0.18f, 1f);
    static readonly Color Gold = new Color(1f, 0.78f, 0.34f, 1f);

    MenuManager menu;
    Canvas ownedCanvas;
    RectTransform mainMenuRoot;
    RectTransform safeAreaRoot;
    float nextPoll;

    Sprite background;
    Sprite horizon;
    Sprite lightning;
    Sprite numbers;
    Sprite stars;
    Sprite confetti;
    Sprite logoGlow;
    Sprite logo;
    Sprite playerHero;
    Sprite opponentHero;
    Sprite mascotSeven;
    Sprite mascotThree;
    Sprite playerChipFrame;
    Sprite tipFrame;
    Sprite ctaGold;
    Sprite ctaBlue;
    Sprite dailyFrame;
    Sprite gear;
    Sprite soloIcon;
    Sprite privateIcon;
    Sprite dailyIcon;
    Sprite tipIcon;
    Sprite streakIcon;
    Sprite primaryGlow;
    Sprite secondaryGlow;
    Sprite primaryGloss;
    Sprite secondaryGloss;

    public bool IsReady { get; private set; }

    public bool OwnsHome
    {
        get
        {
            return menu != null &&
                   menu.mainMenuPanel != null &&
                   menu.mainMenuPanel.activeInHierarchy;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;

        var sceneMenu = FindInScene<MenuManager>(scene);
        if (sceneMenu == null || sceneMenu.mainMenuPanel == null) return;

        var canvas = sceneMenu.mainMenuPanel.GetComponentInParent<Canvas>();
        if (canvas == null ||
            canvas.gameObject.scene != scene ||
            !canvas.isRootCanvas ||
            canvas.renderMode == RenderMode.WorldSpace)
            return;

        if (canvas.GetComponent<MainMenuAuthoritativeVisuals>() == null)
            canvas.gameObject.AddComponent<MainMenuAuthoritativeVisuals>();
    }

    void Awake()
    {
        ownedCanvas = GetComponent<Canvas>();
        menu = FindInScene<MenuManager>(gameObject.scene);
        LoadProductionSprites();
        PrepareHierarchy();
        ApplyOwnerState();
    }

    void OnEnable()
    {
        L10n.OnLanguageChanged += ApplyLocalization;
    }

    void Start()
    {
        // This script's execution order is earlier than the legacy Home
        // presentation passes. Suppress them again here in case their
        // sceneLoaded callbacks added components after this component's Awake.
        ApplyOwnerState();
    }

    void Update()
    {
        if (Time.unscaledTime < nextPoll) return;
        nextPoll = Time.unscaledTime + PollSeconds;
        ApplyOwnerState();
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= ApplyLocalization;
    }

    void ApplyOwnerState()
    {
        if (menu == null)
            menu = FindInScene<MenuManager>(gameObject.scene);
        if (ownedCanvas == null)
            ownedCanvas = GetComponent<Canvas>();

        bool ownsHome = OwnsHome;
        bool homeExclusive = ownsHome && !NonHomeOverlayVisible();
        SuppressCompetingPresentation(homeExclusive);
        if (!ownsHome) return;

        PrepareHierarchy();
        BuildProductionHome();
        IsReady = BindExistingButtons();
        HideLegacyHomePresentation();
        if (IsReady)
            ApplyLocalization();
    }

    bool NonHomeOverlayVisible()
    {
        var pvp = FindInScene<PvpGameController>(gameObject.scene);
        if (pvp != null &&
            (Visible(pvp.pvpMenuPanel) ||
             Visible(pvp.createPanel) ||
             Visible(pvp.joinPanel) ||
             Visible(pvp.matchPanel)))
            return true;

        var daily = FindInScene<DailyHunt>(gameObject.scene);
        return daily != null && Visible(daily.gameObject);
    }

    static bool Visible(GameObject panel)
    {
        return panel != null && panel.activeInHierarchy;
    }

    void SuppressCompetingPresentation(bool homeExclusive)
    {
        if (ownedCanvas != null)
        {
            SetEnabled(ownedCanvas.GetComponent<ExactReferenceVisuals>(), false);
            SetEnabled(ownedCanvas.GetComponent<AttachmentReskinVisuals>(), !homeExclusive);
            SetEnabled(ownedCanvas.GetComponent<AttachmentReskinPolish>(), !homeExclusive);
            SetEnabled(ownedCanvas.GetComponent<AttachmentReskinCanvasBindings>(), !homeExclusive);
        }

        // DesignRuntimeWiring is scene-authored outside the Canvas. It must
        // remain suppressed because re-enabling it would add obsolete root
        // backdrops and rewrite every screen's sprites in a one-shot Start.
        foreach (var root in gameObject.scene.GetRootGameObjects())
            foreach (var design in root.GetComponentsInChildren<DesignRuntimeWiring>(true))
                SetEnabled(design, false);
    }

    static void SetEnabled(Behaviour behaviour, bool enabled)
    {
        if (behaviour != null && behaviour.enabled != enabled)
            behaviour.enabled = enabled;
    }

    void PrepareHierarchy()
    {
        if (menu == null || menu.mainMenuPanel == null) return;

        // The MenuManager field is the authoritative serialized BACKROUND
        // reference. Rename and reuse that exact object; never replace it.
        mainMenuRoot = menu.mainMenuPanel.transform as RectTransform;
        if (mainMenuRoot == null) return;

        mainMenuRoot.name = "MainMenuRoot";
        ResetStretch(mainMenuRoot);
        var legacyImage = mainMenuRoot.GetComponent<Image>();
        if (legacyImage != null)
            legacyImage.enabled = false;

        var safe = DirectChild(mainMenuRoot, "SafeAreaRoot");
        if (safe == null)
            safe = RuntimeUI.CreateObject("SafeAreaRoot", mainMenuRoot).transform;
        safeAreaRoot = safe as RectTransform;
        ResetStretch(safeAreaRoot);

        // ExtrasRuntimeWiring can add StatsLabel one frame later. Pull any
        // late child beneath the one approved SafeAreaRoot so MainMenuRoot
        // always has exactly one direct child, then suppress it by name below.
        for (int i = mainMenuRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = mainMenuRoot.GetChild(i);
            if (child != safeAreaRoot)
                child.SetParent(safeAreaRoot, false);
        }
    }

    void BuildProductionHome()
    {
        if (safeAreaRoot == null) return;

        var bg = EnsureImage(safeAreaRoot, "HomeBackground", background);
        Stretch(bg.rectTransform);
        bg.transform.SetAsFirstSibling();

        AddStretchedImage("HomeHorizon", horizon);
        AddStretchedImage("HomeStars", stars);
        AddStretchedImage("HomeNumbers", numbers);
        AddStretchedImage("HomeLightning", lightning);
        AddStretchedImage("HomeConfetti", confetti);

        AddImage("HomeLogoGlow", logoGlow,
            new Vector2(0f, 650f), new Vector2(800f, 450f), true);
        AddImage("HomePrimaryGlow", primaryGlow,
            new Vector2(0f, 80f), new Vector2(780f, 330f), true);
        AddImage("HomeSecondaryGlow", secondaryGlow,
            new Vector2(0f, -150f), new Vector2(1000f, 320f), true);

        AddImage("HomePlayerHero", playerHero,
            new Vector2(-155f, 390f), new Vector2(300f, 300f), true);
        AddImage("HomeOpponentHero", opponentHero,
            new Vector2(155f, 390f), new Vector2(300f, 300f), true);
        AddImage("HomeMascotSeven", mascotSeven,
            new Vector2(-405f, 260f), new Vector2(210f, 280f), true);
        AddImage("HomeMascotThree", mascotThree,
            new Vector2(405f, 260f), new Vector2(210f, 280f), true);
        AddImage("HomeLogo", logo,
            new Vector2(0f, 650f), new Vector2(610f, 320f), true);

        BuildPlayerChip();
        BuildTipPanel();

        var gloss = AddImage("HomeSecondaryGlossRow", secondaryGloss,
            new Vector2(0f, -150f), new Vector2(1000f, 320f), false);
        if (gloss != null)
            gloss.transform.SetAsLastSibling();
    }

    void BuildPlayerChip()
    {
        var chip = AddImage("HomePlayerChipFrame", playerChipFrame,
            new Vector2(335f, 820f), new Vector2(390f, 110f), false);
        if (chip == null) return;
        chip.type = Image.Type.Sliced;

        AddImage(chip.transform, "HomeStreakIcon", streakIcon,
            new Vector2(-150f, 0f), new Vector2(58f, 58f), true);
        var text = EnsureText(chip.transform, "HomePlayerChipText", 25f, NearWhite);
        Place(text.rectTransform, new Vector2(35f, 0f), new Vector2(285f, 74f));
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        ConfigureText(text, 15f);
    }

    void BuildTipPanel()
    {
        var tip = AddImage("HomeTipFrame", tipFrame,
            new Vector2(0f, -430f), new Vector2(930f, 260f), false);
        if (tip == null) return;
        tip.type = Image.Type.Sliced;

        AddImage(tip.transform, "HomeTipIcon", tipIcon,
            new Vector2(-370f, 26f), new Vector2(86f, 86f), true);

        var title = EnsureText(tip.transform, "HomeTipTitle", 30f, Gold);
        Place(title.rectTransform, new Vector2(-220f, 62f), new Vector2(300f, 54f));
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Left;
        ConfigureText(title, 20f);

        var body = EnsureText(tip.transform, "HomeTipBody", 25f, NearWhite);
        Place(body.rectTransform, new Vector2(-20f, -34f), new Vector2(710f, 125f));
        body.alignment = TextAlignmentOptions.Left;
        ConfigureText(body, 16f);
    }

    bool BindExistingButtons()
    {
        if (safeAreaRoot == null || ownedCanvas == null) return false;

        Button play = FindButton("ButtonPlay");
        Button settings = FindButton("Buttonsettings");
        Button privateRoom = FindButton("ButtonPvP");
        Button daily = FindButton("DailyHuntButton");
        if (play == null || settings == null || privateRoom == null || daily == null)
            return false;

        // SetParent is the only ownership change. Button and ButtonClickedEvent
        // instances remain untouched.
        Reparent(play, new Vector2(0f, 80f), new Vector2(600f, 185f));
        Reparent(settings, new Vector2(-455f, 820f), new Vector2(82f, 82f));
        Reparent(privateRoom, new Vector2(-245f, -150f), new Vector2(450f, 165f));
        Reparent(daily, new Vector2(245f, -150f), new Vector2(450f, 165f));

        StyleButtonImage(play, ctaGold, Image.Type.Sliced);
        StyleButtonImage(privateRoom, ctaBlue, Image.Type.Sliced);
        StyleButtonImage(daily, dailyFrame, Image.Type.Sliced);
        StyleButtonImage(settings, gear, Image.Type.Simple);
        var settingsGraphic = settings.GetComponent<Image>();
        if (settingsGraphic != null)
            settingsGraphic.color = new Color(1f, 1f, 1f, 0.001f);

        HideLegacyButtonCopy(play);
        HideLegacyButtonCopy(privateRoom);
        HideLegacyButtonCopy(daily);
        HideLegacyButtonCopy(settings);

        BuildButtonCopy(play, "HomeSoloTitle", "HomeSoloSubtitle", soloIcon,
            44f, 24f, Ink, new Vector2(-222f, 0f), new Vector2(82f, 82f));
        BuildButtonCopy(privateRoom, "HomePrivateTitle", "HomePrivateSubtitle", privateIcon,
            30f, 21f, NearWhite, new Vector2(-170f, 0f), new Vector2(78f, 78f));
        BuildButtonCopy(daily, "HomeDailyTitle", "HomeDailySubtitle", dailyIcon,
            29f, 21f, NearWhite, new Vector2(-170f, 0f), new Vector2(78f, 78f));

        var settingsIcon = AddImage(settings.transform, "HomeSettingsIcon", gear,
            Vector2.zero, new Vector2(82f, 82f), true);
        if (settingsIcon != null)
            settingsIcon.transform.SetAsLastSibling();

        var primary = AddImage(play.transform, "HomePrimaryGloss", primaryGloss,
            Vector2.zero, Vector2.zero, false);
        if (primary != null)
        {
            Stretch(primary.rectTransform);
            primary.transform.SetAsFirstSibling();
        }

        var secondary = DirectChild(safeAreaRoot, "HomeSecondaryGlossRow");
        if (secondary != null)
            secondary.SetAsLastSibling();

        ApplyLocalization();
        return ButtonsAreFinal(play, settings, privateRoom, daily);
    }

    void Reparent(Button button, Vector2 position, Vector2 size)
    {
        var rect = button.transform as RectTransform;
        if (rect == null) return;
        if (rect.parent != safeAreaRoot)
            rect.SetParent(safeAreaRoot, false);
        Place(rect, position, size);
        button.gameObject.SetActive(true);
    }

    void StyleButtonImage(Button button, Sprite sprite, Image.Type type)
    {
        var image = button.GetComponent<Image>();
        if (image == null || sprite == null) return;
        image.enabled = true;
        image.sprite = sprite;
        image.type = type;
        image.color = Color.white;
        image.preserveAspect = false;
        image.raycastTarget = true;
        button.targetGraphic = image;
    }

    void BuildButtonCopy(Button button, string titleName, string subtitleName, Sprite icon,
        float titleSize, float subtitleSize, Color color, Vector2 iconPosition, Vector2 iconSize)
    {
        string iconName = titleName.EndsWith("Title")
            ? titleName.Substring(0, titleName.Length - "Title".Length) + "Icon"
            : titleName + "Icon";
        AddImage(button.transform, iconName, icon,
            iconPosition, iconSize, true);

        var title = EnsureText(button.transform, titleName, titleSize, color);
        Place(title.rectTransform, new Vector2(38f, 25f),
            new Vector2(((RectTransform)button.transform).sizeDelta.x - 150f, 64f));
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        ConfigureText(title, 18f);

        var subtitle = EnsureText(button.transform, subtitleName, subtitleSize, color);
        Place(subtitle.rectTransform, new Vector2(38f, -38f),
            new Vector2(((RectTransform)button.transform).sizeDelta.x - 150f, 44f));
        subtitle.fontStyle = FontStyles.Bold;
        subtitle.alignment = TextAlignmentOptions.Center;
        ConfigureText(subtitle, 14f);
    }

    static void HideLegacyButtonCopy(Button button)
    {
        if (button == null) return;
        foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
        {
            if (!text.name.StartsWith("Home"))
                text.gameObject.SetActive(false);
        }
        foreach (var text in button.GetComponentsInChildren<Text>(true))
            text.gameObject.SetActive(false);
    }

    bool ButtonsAreFinal(Button play, Button settings, Button privateRoom, Button daily)
    {
        return IsFinalButton(play, ctaGold) &&
               IsFinalButton(settings, gear) &&
               IsFinalButton(privateRoom, ctaBlue) &&
               IsFinalButton(daily, dailyFrame) &&
               Find(safeAreaRoot, "HomeSoloTitle") != null &&
               Find(safeAreaRoot, "HomePrivateTitle") != null &&
               Find(safeAreaRoot, "HomeDailyTitle") != null;
    }

    bool IsFinalButton(Button button, Sprite sprite)
    {
        if (button == null || button.transform.parent != safeAreaRoot) return false;
        var image = button.GetComponent<Image>();
        return image != null && image.sprite == sprite;
    }

    void ApplyLocalization()
    {
        if (safeAreaRoot == null) return;

        SetText("HomeSoloTitle", L10n.Get("mainmenu_play_title"));
        SetText("HomeSoloSubtitle", L10n.Get("mainmenu_play_subtitle"));
        SetText("HomePrivateTitle", L10n.Get("mainmenu_private_title"));
        SetText("HomePrivateSubtitle", L10n.Get("mainmenu_private_subtitle"));
        SetText("HomeDailyTitle", L10n.Get("daily_hunt").ToUpperInvariant());
        SetText("HomeDailySubtitle", L10n.Get("mainmenu_daily_subtitle"));
        SetText("HomeTipTitle", L10n.Get("hud_tip").ToUpperInvariant());
        SetText("HomeTipBody", L10n.Get("simulated_opponents"));

        string playerName = PlayerPrefs.GetString("PlayerName", "");
        if (string.IsNullOrWhiteSpace(playerName))
            playerName = L10n.Get("player_default");
        SetText("HomePlayerChipText",
            playerName.ToUpperInvariant() + "   " +
            L10n.Get("stats_streak").ToUpperInvariant() + " " +
            GameStats.CurrentStreak);
    }

    void SetText(string name, string value)
    {
        var target = Find(safeAreaRoot, name);
        var text = target == null ? null : target.GetComponent<TMP_Text>();
        if (text != null)
            text.text = value;
    }

    void HideLegacyHomePresentation()
    {
        if (ownedCanvas == null || mainMenuRoot == null) return;

        HideNamed(ownedCanvas.transform, "ButtonQuit");
        HideNamed(mainMenuRoot, "StatsLabel");

        string[] canvasBackdrops =
        {
            "BackdropDepth",
            "BackdropNumbers",
            "ExactReferenceBackdrop",
            "AttachmentReferenceBackdrop"
        };
        for (int i = 0; i < canvasBackdrops.Length; i++)
        {
            var old = DirectChild(ownedCanvas.transform, canvasBackdrops[i]);
            if (old != null) old.gameObject.SetActive(false);
        }

        foreach (var child in mainMenuRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child == safeAreaRoot || child.name.StartsWith("Home")) continue;
            if (child.name.StartsWith("Exact") ||
                child.name.StartsWith("Board") ||
                child.name == "StatsLabel")
                child.gameObject.SetActive(false);
        }
    }

    static void HideNamed(Transform root, string name)
    {
        var found = Find(root, name);
        if (found != null)
            found.gameObject.SetActive(false);
    }

    void LoadProductionSprites()
    {
        background = Load("mainmenu_bg_night_arcade");
        horizon = Load("mainmenu_deco_horizon_overlay");
        lightning = Load("mainmenu_deco_lightning_overlay");
        numbers = Load("mainmenu_deco_numbers_overlay");
        stars = Load("mainmenu_deco_stars_overlay");
        confetti = Load("mainmenu_deco_confetti_overlay");
        logoGlow = Load("mainmenu_glow_logo");
        logo = Load("hol_logo_exact");
        playerHero = Load("player_cyan_exact");
        opponentHero = Load("opponent_purple_exact");
        mascotSeven = Load("mascot_7_exact");
        mascotThree = Load("mascot_3_exact");
        playerChipFrame = Load("mainmenu_player_chip_frame_9s");
        tipFrame = Load("mainmenu_tip_frame_9s");
        ctaGold = Load("mainmenu_cta_gold_9s");
        ctaBlue = Load("mainmenu_cta_blue_9s");
        dailyFrame = Load("mainmenu_daily_hunt_frame_9s");
        gear = Load("mainmenu_gear_glossy");
        soloIcon = Load("mainmenu_icon_solo");
        privateIcon = Load("mainmenu_icon_private_room");
        dailyIcon = Load("mainmenu_icon_daily_hunt");
        tipIcon = Load("mainmenu_icon_tip_bulb");
        streakIcon = Load("mainmenu_icon_streak");
        primaryGlow = Load("mainmenu_glow_primary");
        secondaryGlow = Load("mainmenu_glow_secondary_row");
        primaryGloss = Load("mainmenu_gloss_primary_row");
        secondaryGloss = Load("mainmenu_gloss_secondary_row");
    }

    static Sprite Load(string name)
    {
        var sprite = Resources.Load<Sprite>("mainmenu/" + name);
        if (sprite == null)
            Debug.LogError("[MainMenuAuthoritativeVisuals] Missing Resources/mainmenu/" + name);
        return sprite;
    }

    void AddStretchedImage(string name, Sprite sprite)
    {
        var image = EnsureImage(safeAreaRoot, name, sprite);
        Stretch(image.rectTransform);
    }

    Image AddImage(string name, Sprite sprite, Vector2 position, Vector2 size,
        bool preserveAspect)
    {
        return AddImage(safeAreaRoot, name, sprite, position, size, preserveAspect);
    }

    static Image AddImage(Transform parent, string name, Sprite sprite,
        Vector2 position, Vector2 size, bool preserveAspect)
    {
        if (parent == null || sprite == null) return null;
        var image = EnsureImage(parent, name, sprite);
        image.gameObject.SetActive(true);
        image.preserveAspect = preserveAspect;
        Place(image.rectTransform, position, size);
        return image;
    }

    static Image EnsureImage(Transform parent, string name, Sprite sprite)
    {
        var child = DirectChild(parent, name);
        Image image = child == null ? null : child.GetComponent<Image>();
        if (image == null)
        {
            var go = RuntimeUI.CreateObject(name, parent);
            image = go.AddComponent<Image>();
        }
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.raycastTarget = false;
        return image;
    }

    static TMP_Text EnsureText(Transform parent, string name, float fontSize, Color color)
    {
        var child = DirectChild(parent, name);
        var text = child == null ? null : child.GetComponent<TMP_Text>();
        if (text == null)
            text = RuntimeUI.CreateText(parent, name, "", Mathf.RoundToInt(fontSize),
                Vector2.zero, new Vector2(100f, 50f), color);
        text.gameObject.SetActive(true);
        text.fontSize = fontSize;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    static void ConfigureText(TMP_Text text, float minimum)
    {
        if (text == null) return;
        text.enableAutoSizing = true;
        text.fontSizeMax = text.fontSize;
        text.fontSizeMin = Mathf.Min(text.fontSize, minimum);
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    Button FindButton(string name)
    {
        var found = ownedCanvas == null ? null : Find(ownedCanvas.transform, name);
        return found == null ? null : found.GetComponent<Button>();
    }

    static void ResetStretch(RectTransform rect)
    {
        if (rect == null) return;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localPosition = Vector3.zero;
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
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    static Transform DirectChild(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name) return parent.GetChild(i);
        return null;
    }

    static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = Find(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    static T FindInScene<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }
}
