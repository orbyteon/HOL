using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Sole Home presentation owner on MainMenu.
//
// This component reuses the real ButtonPlay, ButtonPvP, DailyHuntButton and
// Buttonsettings controls. Approved sprites stay visibly rendered at alpha 1;
// no custom Graphic paints a lookalike over hidden artwork.
[DefaultExecutionOrder(1600)]
public sealed class MainMenuHomeVisuals : MonoBehaviour
{
    public const string VisualRootName = "HomeVisualRoot";
    public const string SafeRootName = "HomeSafeAreaRoot";
    public const string BackgroundName = "HomeBackground";
    public const string DecoStarsName = "HomeDecoStars";
    public const string DecoLightningName = "HomeDecoLightning";
    public const string DecoConfettiName = "HomeDecoConfetti";
    public const string DecoNumbersName = "HomeDecoNumbers";
    public const string ArenaGridName = "HomeArenaGrid";
    public const string LogoName = "HomeLogo";
    public const string MascotSixName = "HomeMascotSix";
    public const string MascotSevenName = "HomeMascotSeven";
    public const string HeroBoyName = "HomeHeroBoy";
    public const string HeroGirlName = "HomeHeroGirl";
    public const string TipIconName = "HomeTipIcon";
    public const string ChipName = "HomePlayerChip";
    public const string ChipTextName = "HomePlayerChipText";
    public const string TipName = "HomeTipCard";
    public const string SoloIconName = "HomeSoloIcon";
    public const string PrivateIconName = "HomePrivateIcon";
    public const string DailyIconName = "HomeDailyIcon";

    const string BackgroundResource = "phase2a/hol_neon_reference_bg_r3";
    const string LogoResource = "reference/hol_logo_exact";
    const string AvatarResource = "reference/player_cyan_exact";
    const string MascotSixResource = "reference/mascot_6_exact";
    const string MascotSevenResource = "reference/mascot_7_exact";
    const string HeroBoyResource = "phase2a/hol_menu_boy_arms_crossed_r3";
    const string HeroGirlResource = "phase2a/hol_menu_girl_forward_fist_r3";
    const string GoldCtaResource = "phase2a/hol_cta_gold_r2_9s";
    const string BlueCtaResource = "phase2a/hol_cta_blue_r2_9s";
    const string MagentaCtaResource = "phase2a/hol_cta_magenta_r2_9s";
    const string ChipFrameResource = "phase2a/hol_player_chip_r2_9s";
    const string TipFrameResource = "mainmenu/mainmenu_tip_frame_9s";
    const string TipIconResource = "mainmenu/mainmenu_icon_tip_bulb";
    const string StreakIconResource = "mainmenu/mainmenu_icon_streak";
    const string GearResource = "phase2a/hol_settings_gear_r2";
    const string PrivateIconResource = "phase2a/hol_mode_private_r2";
    const string DailyIconResource = "phase2a/hol_mode_daily_r2";
    const string DisplayFontResource = "phase2a/fonts/HOL Menu Display SDF";
    const string BodyFontResource = "phase2a/fonts/HOL Menu Body SDF";

    const float ReferenceWidth = 1080f;
    const float ReferenceHeight = 1920f;

    static readonly Color Ink = new Color(0.09f, 0.06f, 0.22f, 1f);
    static readonly Color NearWhite = new Color(0.96f, 0.97f, 1f, 1f);
    static readonly Color Cyan = new Color(0.08f, 0.86f, 1f, 1f);

    public static readonly string[] LoadedResources =
    {
        BackgroundResource, LogoResource, AvatarResource,
        MascotSixResource, MascotSevenResource, HeroBoyResource, HeroGirlResource,
        GoldCtaResource, BlueCtaResource, MagentaCtaResource, ChipFrameResource,
        TipFrameResource, TipIconResource, StreakIconResource, GearResource,
        PrivateIconResource, DailyIconResource
    };

    public static readonly string[] LoadedFontResources =
    {
        DisplayFontResource, BodyFontResource
    };

    RectTransform visualRoot;
    RectTransform safeRoot;
    RectTransform logoRect;
    RectTransform mascotSixRect;
    RectTransform mascotSevenRect;
    RectTransform heroBoyRect;
    RectTransform heroGirlRect;
    RectTransform gearRect;
    RectTransform chipRect;
    RectTransform soloButtonRect;
    RectTransform privateButtonRect;
    RectTransform dailyButtonRect;
    RectTransform tipRect;
    TMP_Text chipText;
    TMP_FontAsset displayFont;
    TMP_FontAsset bodyFont;
    bool laidOut;
    bool compactLayout;
    int lastLayoutWidth = -1;
    int lastLayoutHeight = -1;
    L10n.Language lastLayoutLanguage;

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
        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<MainMenuHomeVisuals>(true) != null)
                return true;
        return false;
    }

    IEnumerator Start()
    {
        // PvP and Daily Hunt entry buttons are runtime-injected shortly after
        // scene start. Wait for those real controls rather than inventing clones.
        for (int i = 0; i < 24; i++)
        {
            if (FindButton("ButtonPlay") != null &&
                FindButton("ButtonPvP") != null &&
                FindButton("DailyHuntButton") != null &&
                FindButton("Buttonsettings") != null)
                break;
            yield return null;
        }

        BuildHome();
        if (IsReady)
        {
            yield return null;
            yield return null;
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
        bool homeVisible = menu != null && menu.mainMenuPanel != null &&
                           menu.mainMenuPanel.activeSelf;
        if (visualRoot.gameObject.activeSelf != homeVisible)
            visualRoot.gameObject.SetActive(homeVisible);
        if (!homeVisible) return;

        RefreshChip();
        ApplyResponsiveLayout();
    }

    void BuildHome()
    {
        var canvas = GetComponent<Canvas>();
        var menu = FindInScene<MenuManager>(gameObject.scene);
        if (canvas == null || menu == null || menu.mainMenuPanel == null)
        {
            Debug.LogError("[MainMenuHomeVisuals] Missing Canvas/MenuManager.");
            return;
        }

        Sprite background = LoadRequired(BackgroundResource);
        Sprite logo = LoadRequired(LogoResource);
        Sprite avatar = LoadRequired(AvatarResource);
        Sprite six = LoadRequired(MascotSixResource);
        Sprite seven = LoadRequired(MascotSevenResource);
        Sprite heroBoy = LoadRequired(HeroBoyResource);
        Sprite heroGirl = LoadRequired(HeroGirlResource);
        Sprite gold = LoadRequired(GoldCtaResource);
        Sprite blue = LoadRequired(BlueCtaResource);
        Sprite magenta = LoadRequired(MagentaCtaResource);
        Sprite chipFrame = LoadRequired(ChipFrameResource);
        Sprite tipFrame = LoadRequired(TipFrameResource);
        Sprite tipIcon = LoadRequired(TipIconResource);
        Sprite streakIcon = LoadRequired(StreakIconResource);
        Sprite gear = LoadRequired(GearResource);
        Sprite privateIcon = LoadRequired(PrivateIconResource);
        Sprite dailyIcon = LoadRequired(DailyIconResource);
        displayFont = Resources.Load<TMP_FontAsset>(DisplayFontResource);
        bodyFont = Resources.Load<TMP_FontAsset>(BodyFontResource);

        IsReady = RequiredArtReady(background, logo, avatar, six, seven,
            heroBoy, heroGirl, gold, blue, magenta, chipFrame, tipFrame, tipIcon,
            streakIcon, gear, privateIcon, dailyIcon) &&
            displayFont != null && bodyFont != null;
        if (!IsReady)
        {
            Debug.LogError("[MainMenuHomeVisuals] Required Home production assets are missing.");
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

        safeRoot = EnsureRect(visualRoot, SafeRootName);
        Stretch(safeRoot);
        ResponsiveSafeAreaRoot.Attach(safeRoot, (RectTransform)canvas.transform,
            new Vector2(ReferenceWidth, ReferenceHeight));

        var logoImage = EnsureImage(safeRoot, LogoName);
        ConfigureImage(logoImage, logo, true, Image.Type.Simple);
        Place(logoImage.rectTransform, new Vector2(0f, 650f),
            new Vector2(780f, 520f));
        logoRect = logoImage.rectTransform;

        var sixImage = EnsureImage(safeRoot, MascotSixName);
        ConfigureImage(sixImage, six, true, Image.Type.Simple);
        Place(sixImage.rectTransform, new Vector2(-395f, 185f),
            new Vector2(430f, 430f));
        mascotSixRect = sixImage.rectTransform;

        var boyImage = EnsureImage(safeRoot, HeroBoyName);
        ConfigureImage(boyImage, heroBoy, true, Image.Type.Simple);
        Place(boyImage.rectTransform, new Vector2(-145f, 165f),
            new Vector2(470f, 470f));
        heroBoyRect = boyImage.rectTransform;

        var girlImage = EnsureImage(safeRoot, HeroGirlName);
        ConfigureImage(girlImage, heroGirl, true, Image.Type.Simple);
        Place(girlImage.rectTransform, new Vector2(145f, 165f),
            new Vector2(470f, 470f));
        heroGirlRect = girlImage.rectTransform;

        var sevenImage = EnsureImage(safeRoot, MascotSevenName);
        ConfigureImage(sevenImage, seven, true, Image.Type.Simple);
        Place(sevenImage.rectTransform, new Vector2(395f, 185f),
            new Vector2(430f, 430f));
        mascotSevenRect = sevenImage.rectTransform;

        RestyleGear(safeRoot, gear);
        BuildChip(safeRoot, chipFrame, avatar, streakIcon);
        soloButtonRect = RestyleCta(safeRoot, "ButtonPlay", gold, null,
            SoloIconName, "HomeSoloTitle", "home_solo_title", true);
        privateButtonRect = RestyleCta(safeRoot, "ButtonPvP", blue, privateIcon,
            PrivateIconName, "HomePrivateTitle", "home_private_title", false);
        dailyButtonRect = RestyleCta(safeRoot, "DailyHuntButton", magenta, dailyIcon,
            DailyIconName, "HomeDailyTitle", "home_daily_title", false);
        BuildTip(safeRoot, tipFrame, tipIcon);

        ApplyResponsiveLayout(true);
        RefreshChip();
    }

    void RestyleGear(Transform safe, Sprite gear)
    {
        var button = FindButton("Buttonsettings");
        if (button == null) return;
        Reparent(button.transform, safe);
        gearRect = (RectTransform)button.transform;
        Place(gearRect, new Vector2(-455f, 820f), new Vector2(132f, 132f));

        HideChildGraphics(button.transform);
        var image = button.GetComponent<Image>();
        if (image == null) image = button.gameObject.AddComponent<Image>();
        image.enabled = true;
        image.sprite = gear;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = true;
        button.targetGraphic = image;
        ConfigureButtonState(button);
    }

    RectTransform RestyleCta(
        Transform safe,
        string buttonName,
        Sprite frame,
        Sprite icon,
        string iconName,
        string titleName,
        string titleKey,
        bool primary)
    {
        var button = FindButton(buttonName);
        if (button == null) return null;

        Reparent(button.transform, safe);
        HideChildGraphics(button.transform);
        var rect = (RectTransform)button.transform;
        Vector2 size = primary ? new Vector2(930f, 235f) : new Vector2(450f, 205f);
        Place(rect, Vector2.zero, size);

        var image = button.GetComponent<Image>();
        if (image == null) image = button.gameObject.AddComponent<Image>();
        image.enabled = true;
        image.sprite = frame;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 2f;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = true;
        button.targetGraphic = image;
        ConfigureButtonState(button);

        if (!primary && icon != null)
        {
            var iconImage = EnsureImage(button.transform, iconName);
            ConfigureImage(iconImage, icon, true, Image.Type.Simple);
            Place(iconImage.rectTransform, new Vector2(-154f, 0f),
                new Vector2(128f, 128f));
        }

        var title = EnsureTmp(button.transform, titleName,
            primary ? 76f : 46f);
        ApplyDisplayFont(title);
        title.color = primary ? Ink : NearWhite;
        title.alignment = primary
            ? TextAlignmentOptions.Center
            : TextAlignmentOptions.MidlineLeft;
        title.enableWordWrapping = !primary;
        title.enableAutoSizing = true;
        title.fontSizeMin = primary ? 54f : 32f;
        title.fontSizeMax = primary ? 76f : 48f;
        title.overflowMode = TextOverflowModes.Overflow;
        title.raycastTarget = false;
        Place(title.rectTransform,
            primary ? Vector2.zero : new Vector2(58f, 0f),
            primary ? new Vector2(780f, 145f) : new Vector2(286f, 150f));
        SetLocalized(title, titleKey);
        AddTextShadow(title, primary ? 0.24f : 0.55f);
        return rect;
    }

    static void ConfigureButtonState(Button button)
    {
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

    void BuildChip(Transform safe, Sprite frame, Sprite avatar, Sprite streakIcon)
    {
        var chip = EnsureImage(safe, ChipName);
        ConfigureImage(chip, frame, false, Image.Type.Sliced);
        chip.pixelsPerUnitMultiplier = 2f;
        Place(chip.rectTransform, new Vector2(360f, 820f),
            new Vector2(330f, 120f));
        chipRect = chip.rectTransform;

        var avatarImage = EnsureImage(chip.transform, "HomePlayerAvatar");
        ConfigureImage(avatarImage, avatar, true, Image.Type.Simple);
        Place(avatarImage.rectTransform, new Vector2(-105f, -1f),
            new Vector2(78f, 78f));

        var streak = EnsureImage(chip.transform, "HomeStreakIcon");
        ConfigureImage(streak, streakIcon, true, Image.Type.Simple);
        Place(streak.rectTransform, new Vector2(-3f, -26f),
            new Vector2(38f, 38f));

        chipText = EnsureTmp(chip.transform, ChipTextName, 28f);
        ApplyBodyFont(chipText, true);
        chipText.alignment = TextAlignmentOptions.MidlineLeft;
        chipText.color = NearWhite;
        chipText.enableAutoSizing = true;
        chipText.fontSizeMin = 22f;
        chipText.fontSizeMax = 30f;
        chipText.lineSpacing = -8f;
        chipText.richText = true;
        Place(chipText.rectTransform, new Vector2(62f, 0f),
            new Vector2(170f, 84f));
        AddTextShadow(chipText, 0.62f);
    }

    void BuildTip(Transform safe, Sprite frame, Sprite tipIcon)
    {
        var tip = EnsureImage(safe, TipName);
        ConfigureImage(tip, frame, false, Image.Type.Sliced);
        tip.pixelsPerUnitMultiplier = 2f;
        Place(tip.rectTransform, new Vector2(0f, -715f),
            new Vector2(900f, 190f));
        tipRect = tip.rectTransform;

        var icon = EnsureImage(tip.transform, TipIconName);
        ConfigureImage(icon, tipIcon, true, Image.Type.Simple);
        Place(icon.rectTransform, new Vector2(-365f, 0f),
            new Vector2(104f, 104f));

        var title = EnsureTmp(tip.transform, "HomeTipTitle", 34f);
        ApplyDisplayFont(title);
        title.color = Cyan;
        title.alignment = TextAlignmentOptions.MidlineLeft;
        Place(title.rectTransform, new Vector2(72f, 35f),
            new Vector2(620f, 52f));
        SetLocalized(title, "home_tip_title");
        AddTextShadow(title, 0.66f);

        var body = EnsureTmp(tip.transform, "HomeTipBody", 28f);
        ApplyBodyFont(body, true);
        body.color = NearWhite;
        body.alignment = TextAlignmentOptions.MidlineLeft;
        body.enableAutoSizing = true;
        body.fontSizeMin = 22f;
        body.fontSizeMax = 30f;
        Place(body.rectTransform, new Vector2(0f, -30f),
            new Vector2(500f, 84f));
        SetLocalized(body, "home_tip_body");
        AddTextShadow(body, 0.66f);
    }

    void RefreshPresentation()
    {
        RefreshChip();
        ApplyResponsiveLayout(true);
    }

    void ApplyResponsiveLayout(bool force = false)
    {
        ApplyResponsiveLayoutForViewport(Screen.width, Screen.height, force);
    }

    // Kept for capture/regression tools.
    void ApplyResponsiveLayoutForWidth(int width, bool force = false)
    {
        ApplyResponsiveLayoutForViewport(width, Screen.height, force);
    }

    void ApplyResponsiveLayoutForViewport(int width, int height, bool force = false)
    {
        if (soloButtonRect == null || privateButtonRect == null ||
            dailyButtonRect == null || tipRect == null) return;

        var language = L10n.Current;
        bool shouldCompact = width > 0 && width < 600;
        if (!force && width == lastLayoutWidth && height == lastLayoutHeight &&
            language == lastLayoutLanguage && shouldCompact == compactLayout)
            return;

        compactLayout = shouldCompact;
        lastLayoutWidth = width;
        lastLayoutHeight = height;
        lastLayoutLanguage = language;

        float aspect = width > 0
            ? Mathf.Max(1, height) / (float)width
            : ReferenceHeight / ReferenceWidth;
        float tall = Mathf.InverseLerp(1.85f, 2.22f, aspect);

        Place(logoRect, new Vector2(0f, 650f + 150f * tall),
            new Vector2(780f, 520f));
        Place(mascotSixRect, new Vector2(-395f, 185f + 95f * tall),
            new Vector2(430f, 430f));
        Place(heroBoyRect, new Vector2(-145f, 165f + 95f * tall),
            new Vector2(470f, 470f));
        Place(heroGirlRect, new Vector2(145f, 165f + 95f * tall),
            new Vector2(470f, 470f));
        Place(mascotSevenRect, new Vector2(395f, 185f + 95f * tall),
            new Vector2(430f, 430f));
        Place(gearRect, new Vector2(-455f, 820f + 190f * tall),
            new Vector2(132f, 132f));
        Place(chipRect, new Vector2(360f, 820f + 190f * tall),
            new Vector2(330f, 120f));
        Place(soloButtonRect, new Vector2(0f, -145f + 20f * tall),
            new Vector2(930f, 235f));

        if (compactLayout)
        {
            Place(privateButtonRect, new Vector2(0f, -430f),
                new Vector2(930f, 195f));
            Place(dailyButtonRect, new Vector2(0f, -640f),
                new Vector2(930f, 195f));
            Place(tipRect, new Vector2(0f, -855f - 70f * tall),
                new Vector2(930f, 200f));
        }
        else
        {
            Place(privateButtonRect, new Vector2(-240f, -390f - 45f * tall),
                new Vector2(450f, 205f));
            Place(dailyButtonRect, new Vector2(240f, -390f - 45f * tall),
                new Vector2(450f, 205f));
            Place(tipRect, new Vector2(0f, -715f - 90f * tall),
                new Vector2(900f, 190f));
        }

        LayoutSupportingContent(privateButtonRect, PrivateIconName,
            "HomePrivateTitle", compactLayout);
        LayoutSupportingContent(dailyButtonRect, DailyIconName,
            "HomeDailyTitle", compactLayout);
    }

    static void LayoutSupportingContent(RectTransform card, string iconName,
        string titleName, bool compact)
    {
        if (card == null) return;
        var icon = DirectChild(card, iconName) as RectTransform;
        var titleTransform = DirectChild(card, titleName);
        var title = titleTransform == null ? null : titleTransform.GetComponent<TMP_Text>();
        if (icon == null || title == null) return;

        if (compact)
        {
            Place(icon, new Vector2(-345f, 0f), new Vector2(132f, 132f));
            Place(title.rectTransform, new Vector2(42f, 0f),
                new Vector2(650f, 130f));
            title.fontSizeMin = 28f;
            title.fontSizeMax = 46f;
        }
        else
        {
            Place(icon, new Vector2(-153f, 0f), new Vector2(128f, 128f));
            Place(title.rectTransform, new Vector2(60f, 0f),
                new Vector2(286f, 150f));
            title.fontSizeMin = 32f;
            title.fontSizeMax = 48f;
        }
        title.enableAutoSizing = true;
        title.enableWordWrapping = true;
        title.alignment = TextAlignmentOptions.MidlineLeft;
    }

    static void AddTextShadow(TMP_Text text, float alpha)
    {
        if (text == null) return;
        var shadow = text.GetComponent<Shadow>();
        if (shadow == null) shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.02f, 0.01f, 0.12f, alpha);
        shadow.effectDistance = new Vector2(2f, -3f);
        shadow.useGraphicAlpha = true;
    }

    void ApplyDisplayFont(TMP_Text text)
    {
        if (text == null) return;
        if (displayFont != null) text.font = displayFont;
        text.fontStyle = FontStyles.Normal;
        text.fontWeight = FontWeight.Bold;
    }

    void ApplyBodyFont(TMP_Text text, bool semibold)
    {
        if (text == null) return;
        if (bodyFont != null) text.font = bodyFont;
        text.fontStyle = semibold ? FontStyles.Bold : FontStyles.Normal;
        text.fontWeight = semibold ? FontWeight.SemiBold : FontWeight.Regular;
    }

    static bool RequiredArtReady(params Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0) return false;
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] == null) return false;
        return true;
    }

    void RefreshChip()
    {
        if (chipText == null) return;
        string player = PlayerPrefs.GetString("PlayerName", "");
        if (string.IsNullOrEmpty(player)) player = L10n.Get("player_default");
        chipText.text = "<b>" + player + "</b>\n<size=82%>" +
                        L10n.Get("stats_streak") + " " +
                        GameStats.CurrentStreak + "</size>";
    }

    void HideLegacyHome(Transform canvas)
    {
        foreach (var name in new[]
        {
            "ExactReferenceBackdrop", "AttachmentReferenceBackdrop",
            "ExactHOLLogo", "BoardHomeLogo", "StatsLabel",
            "HomeNeonBackdrop", "HomeArenaGrid", "HomeDecoStars",
            "HomeDecoLightning", "HomeDecoConfetti", "HomeDecoNumbers"
        })
        {
            var child = DeepFind(canvas, name);
            if (child != null && child != visualRoot)
                child.gameObject.SetActive(false);
        }
    }

    Button FindButton(string name)
    {
        var found = DeepFind(transform, name);
        return found == null ? null : found.GetComponent<Button>();
    }

    void HideNamed(string name)
    {
        var found = DeepFind(transform, name);
        if (found != null) found.gameObject.SetActive(false);
    }

    static void HideChildGraphics(Transform root)
    {
        if (root == null) return;
        foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic.transform == root) continue;
            graphic.gameObject.SetActive(false);
        }
    }

    static void Reparent(Transform child, Transform parent)
    {
        if (child == null || parent == null) return;
        if (child.parent != parent) child.SetParent(parent, false);
        child.gameObject.SetActive(true);
        child.SetAsLastSibling();
    }

    static void SetLocalized(TMP_Text text, string key)
    {
        var loc = text.GetComponent<LocalizedText>();
        if (loc == null)
        {
            RuntimeUI.Localize(text, key);
            loc = text.GetComponent<LocalizedText>();
        }
        if (loc != null) loc.key = key;
        text.text = L10n.Get(key);
    }

    static Sprite LoadRequired(string path)
    {
        var sprite = Resources.Load<Sprite>(path);
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
        var rect = EnsureRect(parent, name);
        var image = rect.GetComponent<Image>();
        if (image == null) image = rect.gameObject.AddComponent<Image>();
        return image;
    }

    static TMP_Text EnsureTmp(Transform parent, string name, float size)
    {
        var rect = EnsureRect(parent, name);
        var tmp = rect.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.gameObject.SetActive(true);
        tmp.fontSize = size;
        tmp.raycastTarget = false;
        tmp.color = NearWhite;
        RuntimeUI.ConfigureText(tmp, ResponsiveTextRole.Body, size);
        return tmp;
    }

    static void ConfigureImage(Image image, Sprite sprite, bool preserveAspect,
        Image.Type type)
    {
        image.enabled = true;
        image.sprite = sprite;
        image.type = type;
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    static void Place(RectTransform rect, Vector2 position, Vector2 size)
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

    static Transform DirectChild(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name) return parent.GetChild(i);
        return null;
    }

    static Transform DeepFind(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = DeepFind(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    static T FindInScene<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid()) return null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }

    static Canvas FindOwnedCanvas(Scene scene)
    {
        var menu = FindInScene<MenuManager>(scene);
        if (menu != null && menu.mainMenuPanel != null)
        {
            var owned = menu.mainMenuPanel.GetComponentInParent<Canvas>();
            if (owned != null && owned.isRootCanvas &&
                owned.renderMode != RenderMode.WorldSpace)
                return owned;
        }

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas.gameObject.scene == scene && canvas.isRootCanvas &&
                    canvas.renderMode != RenderMode.WorldSpace)
                    return canvas;
            }
        }
        return null;
    }
}
