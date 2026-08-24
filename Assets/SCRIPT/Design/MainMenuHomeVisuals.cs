using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Sole Home presentation owner on MainMenu. Restyles the five existing
// controls in place and never creates a Button or Canvas.
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
    const string GoldCtaResource = "mainmenu/mainmenu_cta_gold_9s";
    const string BlueCtaResource = "mainmenu/mainmenu_cta_blue_9s";
    const string MagentaCtaResource = "mainmenu/mainmenu_cta_magenta_9s";
    const string ChipFrameResource = "mainmenu/mainmenu_player_chip_frame_9s";
    const string TipFrameResource = "mainmenu/mainmenu_tip_frame_9s";
    const string TipIconResource = "mainmenu/mainmenu_icon_tip_bulb";
    const string GearResource = "phase2a/hol_settings_gear_r2";
    const string SoloIconResource = "phase2a/hol_mode_solo_r2";
    const string PrivateIconResource = "phase2a/hol_mode_private_r2";
    const string DailyIconResource = "phase2a/hol_mode_daily_r2";
    const string ChevronResource = "phase2a/hol_chevron_r2";

    static readonly Color Ink = new Color(0.09f, 0.06f, 0.22f, 1f);
    static readonly Color GoldLight = new Color(1f, 0.68f, 0.08f, 1f);
    static readonly Color CyanLight = new Color(0.02f, 0.84f, 1f, 1f);
    static readonly Color MagentaLight = new Color(1f, 0.08f, 0.78f, 1f);
    const float ReferenceWidth = 1080f;
    const float ReferenceHeight = 1920f;

    public static readonly string[] LoadedResources =
    {
        BackgroundResource, LogoResource, AvatarResource,
        MascotSixResource, MascotSevenResource, HeroBoyResource, HeroGirlResource,
        GoldCtaResource, BlueCtaResource, MagentaCtaResource, ChipFrameResource,
        TipFrameResource, TipIconResource, GearResource, SoloIconResource, PrivateIconResource,
        DailyIconResource, ChevronResource
    };

    public static readonly string[] LoadedFontResources =
    {
        "Themes/Cartoon/Fonts/Cartoon Montserrat ExtraBold SDF",
        "Themes/Cartoon/Fonts/Cartoon Plus Jakarta Sans Medium SDF",
        "Themes/Cartoon/Fonts/Cartoon Noto Sans ExtraBold SDF",
        "Themes/Cartoon/Fonts/Cartoon Noto Sans Medium SDF"
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

        Canvas canvas = null;
        var menu = FindInScene<MenuManager>(scene);
        if (menu != null && menu.mainMenuPanel != null)
            canvas = menu.mainMenuPanel.GetComponentInParent<Canvas>();
        if (canvas == null || !canvas.isRootCanvas ||
            canvas.renderMode == RenderMode.WorldSpace)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var candidate in root.GetComponentsInChildren<Canvas>(true))
                {
                    if (!candidate.isRootCanvas ||
                        candidate.renderMode == RenderMode.WorldSpace)
                        continue;
                    canvas = candidate;
                    break;
                }
                if (canvas != null) break;
            }
        }

        if (canvas != null && canvas.GetComponent<MainMenuHomeVisuals>() == null)
            canvas.gameObject.AddComponent<MainMenuHomeVisuals>();
    }

    public static bool OwnsHome(Scene scene)
    {
        if (!scene.IsValid()) return false;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<MainMenuHomeVisuals>(true) != null)
                return true;
        }
        return false;
    }

    IEnumerator Start()
    {
        for (int i = 0; i < 12; i++)
            yield return null;
        BuildHome();
        // Procedural neon seams and CTAs need a painted frame before Android
        // capture logs HOL_MAINMENU_CAPTURE_READY. WaitForEndOfFrame never
        // completes in headless PlayMode CI (batchmode), so use null yields there.
        if (Application.isBatchMode)
        {
            yield return null;
            yield return null;
        }
        else
        {
            yield return new WaitForEndOfFrame();
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
        if (homeVisible)
        {
            RefreshChip();
            ApplyResponsiveLayout();
        }
    }

    void BuildHome()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[MainMenuHomeVisuals] Missing Canvas host.");
            return;
        }

        var theme = HolTheme.Current;
        if (theme == null || !theme.IsComplete)
        {
            Debug.LogError("[MainMenuHomeVisuals] Cartoon theme catalog is incomplete.");
            return;
        }
        var background = theme.home.background;
        var logo = theme.shared.logo;
        var avatar = theme.shared.playerPortrait;
        var six = theme.shared.mascotSix;
        var seven = theme.shared.mascotSeven;
        var heroBoy = theme.home.heroBoy;
        var heroGirl = theme.home.heroGirl;
        var gold = theme.shared.primaryButton;
        var cyan = theme.shared.secondaryBlueButton;
        var magenta = theme.shared.secondaryMagentaButton;
        var chipFrame = theme.shared.playerChip;
        var tipFrame = theme.shared.neutralPanel;
        var tipIcon = theme.home.tipIcon;
        var gear = theme.home.settingsGear;
        var soloIcon = theme.home.soloIcon;
        var privateIcon = theme.home.privateRoomIcon;
        var dailyIcon = theme.home.dailyHuntIcon;
        var chevron = theme.shared.chevron;

        IsReady = RequiredArtReady(background, logo, avatar, six, seven,
            heroBoy, heroGirl,
            gold, cyan, magenta, chipFrame, tipFrame, tipIcon, gear, soloIcon,
            privateIcon, dailyIcon, chevron);
        if (!IsReady) return;

        HideLegacyHome(canvas.transform);
        HideNamed("ButtonQuit");

        visualRoot = EnsureRect(canvas.transform, VisualRootName);
        Stretch(visualRoot);
        visualRoot.SetAsFirstSibling();

        var bg = EnsureImage(visualRoot, BackgroundName);
        Stretch(bg.rectTransform);
        ConfigureImage(bg, background, false);
        bg.color = Color.white;

        // The Revision 3 background owns the exact diagonal light field,
        // horizon and perspective floor. Do not double-paint those lines with
        // the earlier procedural arena overlay.
        var oldArenaGrid = visualRoot.Find(ArenaGridName);
        if (oldArenaGrid != null) oldArenaGrid.gameObject.SetActive(false);

        safeRoot = EnsureRect(visualRoot, SafeRootName);
        ResponsiveSafeAreaRoot.Attach(safeRoot, (RectTransform)canvas.transform,
            new Vector2(ReferenceWidth, ReferenceHeight));

        var logoImage = EnsureImage(safeRoot, LogoName);
        ConfigureImage(logoImage, logo, true);
        Place(logoImage.rectTransform, new Vector2(0f, 650f), new Vector2(760f, 505f));
        logoRect = logoImage.rectTransform;

        var sixImage = EnsureImage(safeRoot, MascotSixName);
        ConfigureImage(sixImage, six, true);
        Place(sixImage.rectTransform, new Vector2(-390f, 185f), new Vector2(440f, 440f));
        mascotSixRect = sixImage.rectTransform;

        var boyImage = EnsureImage(safeRoot, HeroBoyName);
        ConfigureImage(boyImage, heroBoy, true);
        Place(boyImage.rectTransform, new Vector2(-145f, 175f), new Vector2(470f, 470f));
        heroBoyRect = boyImage.rectTransform;

        var girlImage = EnsureImage(safeRoot, HeroGirlName);
        ConfigureImage(girlImage, heroGirl, true);
        Place(girlImage.rectTransform, new Vector2(145f, 175f), new Vector2(470f, 470f));
        heroGirlRect = girlImage.rectTransform;

        var sevenImage = EnsureImage(safeRoot, MascotSevenName);
        ConfigureImage(sevenImage, seven, true);
        Place(sevenImage.rectTransform, new Vector2(390f, 185f), new Vector2(440f, 440f));
        mascotSevenRect = sevenImage.rectTransform;

        RestyleGear(safeRoot, gear);
        BuildChip(safeRoot, chipFrame, avatar);
        soloButtonRect = RestyleCta(safeRoot, "ButtonPlay", gold, null,
            "HomeSoloTitle", "home_solo_title", true);
        privateButtonRect = RestyleCta(safeRoot, "ButtonPvP", cyan,
            privateIcon, "HomePrivateTitle", "home_private_title", false);
        dailyButtonRect = RestyleCta(safeRoot, "DailyHuntButton", gold,
            dailyIcon, "HomeDailyTitle", "home_daily_title", false);
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
        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = gear;
            // Preserve the scene-authored Button hit target, but render the
            // approved unboxed cyan gear with live vector geometry.
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = true;
        }
        HideChildLabels(button.transform);
        var obsolete = button.transform.Find("HomeSettingsGearSymbol");
        if (obsolete != null) obsolete.gameObject.SetActive(false);
    }

    RectTransform RestyleCta(
        Transform safe, string buttonName, Sprite frame, Sprite iconSprite,
        string titleName, string titleKey, bool primary)
    {
        var button = FindButton(buttonName);
        if (button == null) return null;
        Reparent(button.transform, safe);
        HideChildGraphics(button.transform);
        RemoveLegacyCtaLabels(button.transform, titleName);
        var rect = (RectTransform)button.transform;
        Vector2 size = primary ? new Vector2(930f, 235f) : new Vector2(450f, 205f);
        Place(rect, Vector2.zero, size);
        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = frame;
            // Keep the scene-authored Image as the Button hit target while the
            // deterministic chamfered surface below owns visible rendering.
            image.color = Color.white;
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.raycastTarget = true;
            button.targetGraphic = image;
        }

        Color accent = primary
            ? GoldLight
            : buttonName == "ButtonPvP" ? CyanLight : GoldLight;
        ConfigureCtaMaterial(button, frame, accent, primary);

        var title = EnsureTmp(button.transform, titleName, primary ? 76f : 48f);
        ApplyFont(title, primary ? HolTextRole.PrimaryCta : HolTextRole.SecondaryCta);
        title.fontSize = primary ? 76f : 48f;
        title.color = Ink;
        title.alignment = primary
            ? TextAlignmentOptions.Center
            : TextAlignmentOptions.MidlineLeft;
        title.enableWordWrapping = !primary;
        title.enableAutoSizing = true;
        title.fontSizeMin = primary ? 54f : 32f;
        title.fontSizeMax = primary ? 76f : 48f;
        RuntimeUI.ConfigureText(title, ResponsiveTextRole.Action,
            primary ? 76f : 48f);
        title.enableWordWrapping = !primary;
        ApplyFont(title, primary ? HolTextRole.PrimaryCta : HolTextRole.SecondaryCta);
        title.characterSpacing = primary ? -2f : -1f;
        Place(title.rectTransform,
            primary ? Vector2.zero : new Vector2(52f, 0f),
            primary ? new Vector2(760f, 132f) : new Vector2(286f, 150f));
        SetLocalized(title, titleKey);
        AddTextShadow(title, primary ? 0.28f : 0.22f);

        if (!primary)
        {
            var icon = EnsureImage(button.transform,
                buttonName == "ButtonPvP" ? PrivateIconName : DailyIconName);
            ConfigureImage(icon, iconSprite, true);
            Place(icon.rectTransform, new Vector2(-154f, 0f), new Vector2(128f, 128f));
        }
        return rect;
    }

    static void ConfigureCtaMaterial(Button button, Sprite frame, Color accent,
        bool primary)
    {
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colors.selectedColor = new Color(1f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.76f, 0.82f, 0.92f, 1f);
        colors.disabledColor = new Color(0.38f, 0.40f, 0.48f, 0.62f);
        colors.colorMultiplier = 1.18f;
        colors.fadeDuration = 0.07f;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = colors;

        var oldSurface = button.transform.Find("HomeCtaInnerLight");
        if (oldSurface != null) oldSurface.gameObject.SetActive(false);
    }

    void BuildChip(Transform safe, Sprite frame, Sprite avatar)
    {
        var chip = EnsureImage(safe, ChipName);
        chip.sprite = frame;
        chip.color = Color.white;
        chip.type = Image.Type.Sliced;
        chip.raycastTarget = false;
        Place(chip.rectTransform, new Vector2(360f, 820f), new Vector2(330f, 120f));
        chipRect = chip.rectTransform;

        var avatarImage = EnsureImage(chip.transform, "HomePlayerAvatar");
        ConfigureImage(avatarImage, avatar, true);
        Place(avatarImage.rectTransform, new Vector2(-105f, -1f), new Vector2(78f, 78f));

        chipText = EnsureTmp(chip.transform, ChipTextName, 27f);
        ApplyFont(chipText, HolTextRole.Emphasis);
        chipText.alignment = TextAlignmentOptions.MidlineLeft;
        chipText.color = ConvergingLight.NearWhite;
        chipText.raycastTarget = false;
        chipText.enableAutoSizing = true;
        chipText.richText = true;
        chipText.fontSizeMin = 23f;
        chipText.fontSizeMax = 31f;
        chipText.lineSpacing = -10f;
        Place(chipText.rectTransform, new Vector2(48f, 0f), new Vector2(190f, 82f));
        AddTextShadow(chipText, 0.65f);
    }

    void BuildTip(Transform safe, Sprite frame, Sprite tipIcon)
    {
        var tip = EnsureImage(safe, TipName);
        tip.sprite = frame;
        tip.color = new Color(0.78f, 0.68f, 1f, 1f);
        tip.type = Image.Type.Simple;
        tip.raycastTarget = false;
        Place(tip.rectTransform, new Vector2(0f, -715f), new Vector2(900f, 190f));
        tipRect = tip.rectTransform;

        var icon = EnsureImage(tip.transform, TipIconName);
        ConfigureImage(icon, tipIcon, true);
        Place(icon.rectTransform, new Vector2(-365f, 0f), new Vector2(104f, 104f));

        var title = EnsureTmp(tip.transform, "HomeTipTitle", 34f);
        ApplyFont(title, HolTextRole.SectionHeading);
        title.color = CyanLight;
        title.alignment = TextAlignmentOptions.MidlineLeft;
        Place(title.rectTransform, new Vector2(72f, 35f), new Vector2(620f, 52f));
        SetLocalized(title, "home_tip_title");
        AddTextShadow(title, 0.72f);

        var body = EnsureTmp(tip.transform, "HomeTipBody", 28f);
        ApplyFont(body, HolTextRole.Body);
        body.color = ConvergingLight.NearWhite;
        body.alignment = TextAlignmentOptions.Left;
        body.enableAutoSizing = true;
        body.fontSizeMin = 22f;
        body.fontSizeMax = 30f;
        body.fontStyle = FontStyles.Normal;
        body.alignment = TextAlignmentOptions.MidlineLeft;
        Place(body.rectTransform, new Vector2(0f, -30f), new Vector2(500f, 84f));
        SetLocalized(body, "home_tip_body");
        AddTextShadow(body, 0.72f);
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

    void ApplyResponsiveLayoutForWidth(int width, bool force = false)
    {
        ApplyResponsiveLayoutForViewport(width, Screen.height, force);
    }

    void ApplyResponsiveLayoutForViewport(int width, int height, bool force = false)
    {
        if (soloButtonRect == null || privateButtonRect == null ||
            dailyButtonRect == null || tipRect == null) return;

        var language = L10n.Current;
        // The approved Home composition keeps its two supporting cards paired,
        // including the 720-wide Greek adaptation. Only genuinely ultra-narrow
        // surfaces fall back to a vertical safety layout.
        bool shouldCompact = width > 0 && width < 600;
        if (!force && width == lastLayoutWidth && height == lastLayoutHeight &&
            language == lastLayoutLanguage &&
            shouldCompact == compactLayout) return;

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
            Place(privateButtonRect, new Vector2(0f, -430f), new Vector2(930f, 195f));
            Place(dailyButtonRect, new Vector2(0f, -640f), new Vector2(930f, 195f));
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
        var icon = DirectChild(card, iconName) as RectTransform;
        var title = DirectChild(card, titleName).GetComponent<TMP_Text>();
        if (compact)
        {
            Place(icon, new Vector2(-345f, 0f), new Vector2(132f, 132f));
            Place(title.rectTransform, new Vector2(42f, 0f), new Vector2(650f, 130f));
            title.fontSizeMin = 24f;
            title.fontSizeMax = 46f;
            title.fontSize = 44f;
        }
        else
        {
            Place(icon, new Vector2(-153f, 0f), new Vector2(138f, 138f));
            Place(title.rectTransform, new Vector2(60f, 0f), new Vector2(286f, 150f));
            title.fontSizeMin = 32f;
            title.fontSizeMax = 48f;
            title.fontSize = 46f;
        }
        title.enableAutoSizing = true;
        title.enableWordWrapping = true;
        title.alignment = TextAlignmentOptions.MidlineLeft;
        bool darkText = card.name == "DailyHuntButton";
        title.color = darkText ? Ink : ConvergingLight.NearWhite;
        title.overflowMode = TextOverflowModes.Overflow;
        AddTextShadow(title, darkText ? 0.22f : 0.72f);
        title.SetLayoutDirty();
        title.SetVerticesDirty();
        title.ForceMeshUpdate(true, true);
    }

    static void AddTextShadow(TMP_Text text, float alpha)
    {
        var shadow = text.GetComponent<Shadow>();
        if (shadow == null) shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.02f, 0.01f, 0.12f, alpha);
        shadow.effectDistance = new Vector2(2f, -3f);
        shadow.useGraphicAlpha = true;
    }

    static void ApplyFont(TMP_Text text, HolTextRole role)
    {
        CartoonTypography.Bind(text, role);
        text.fontStyle = FontStyles.Normal;
        text.fontWeight = role == HolTextRole.Body || role == HolTextRole.Small
            ? FontWeight.Regular
            : FontWeight.Bold;
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
        if (string.IsNullOrEmpty(player))
            player = L10n.Get("player_default");
        chipText.text = "<b>" + player + "</b>\n<color=#FFD451><size=80%>" +
                        L10n.Get("stats_streak") + " " +
                        GameStats.CurrentStreak + "</size></color>";
    }

    void HideLegacyHome(Transform canvas)
    {
        var menu = FindInScene<MenuManager>(gameObject.scene);
        if (menu != null && menu.mainMenuPanel != null)
        {
            var panelImage = menu.mainMenuPanel.GetComponent<Image>();
            if (panelImage != null) panelImage.enabled = false;
        }

        foreach (var name in new[]
                 {
                     "ExactReferenceBackdrop", "AttachmentReferenceBackdrop",
                     "ExactHOLLogo", "BoardHomeLogo", "StatsLabel"
                 })
        {
            var child = DeepFind(canvas, name);
            if (child != null) child.gameObject.SetActive(false);
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

    static void Reparent(Transform child, Transform parent)
    {
        if (child.parent != parent)
            child.SetParent(parent, false);
        child.SetAsLastSibling();
    }

    static void HideChildLabels(Transform root)
    {
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            text.gameObject.SetActive(false);
        foreach (var text in root.GetComponentsInChildren<Text>(true))
            text.gameObject.SetActive(false);
    }

    static void RemoveLegacyCtaLabels(Transform root, string titleName)
    {
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.gameObject.name == titleName) continue;
            RuntimeUI.DestroyNow(text.gameObject);
        }
        foreach (var text in root.GetComponentsInChildren<Text>(true))
            RuntimeUI.DestroyNow(text.gameObject);
    }

    static void HideChildGraphics(Transform root)
    {
        foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic.transform != root)
                graphic.gameObject.SetActive(false);
        }
    }

    static TMP_Text EnsureTmp(Transform parent, string name, float size)
    {
        var rect = EnsureRect(parent, name);
        rect.gameObject.SetActive(true);
        var tmp = rect.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.raycastTarget = false;
        tmp.color = ConvergingLight.NearWhite;
        RuntimeUI.ConfigureText(tmp, ResponsiveTextRole.Body, size);
        return tmp;
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

    static Sprite LoadOptional(string path)
    {
        var sprite = Resources.Load<Sprite>(path);
        if (sprite == null)
            Debug.LogError("[MainMenuHomeVisuals] Missing optional Resources/" + path + ".");
        return sprite;
    }

    static RectTransform EnsureRect(Transform parent, string name)
    {
        var existing = DirectChild(parent, name) as RectTransform;
        if (existing != null) return existing;
        return (RectTransform)RuntimeUI.CreateObject(name, parent).transform;
    }

    static Image EnsureImage(Transform parent, string name)
    {
        var rect = EnsureRect(parent, name);
        var image = rect.GetComponent<Image>();
        if (image == null) image = rect.gameObject.AddComponent<Image>();
        return image;
    }

    static void ConfigureImage(Image image, Sprite sprite, bool preserveAspect)
    {
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
    }

    static void BuildDecorationLayer(Transform parent, string name, Sprite sprite,
        float opacity)
    {
        var image = EnsureImage(parent, name);
        Stretch(image.rectTransform);
        ConfigureImage(image, sprite, false);
        image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(opacity));
    }

    static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
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

    static Transform DirectChild(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name)
                return parent.GetChild(i);
        return null;
    }

    static Transform DeepFind(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var found = DeepFind(parent.GetChild(i), name);
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
}

// A lightweight perspective floor and diagonal arena lighting layer. The
// native background owns the painted atmosphere; this graphic adds the crisp
// screen-space geometry visible in the approved Main Menu without baking UI
// text or controls into a bitmap.
public sealed class MainMenuNeonArenaGraphic : MaskableGraphic
{
    static readonly Color Cyan = new Color(0.00f, 0.82f, 1.00f, 1f);
    static readonly Color Magenta = new Color(1.00f, 0.12f, 0.76f, 1f);

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect bounds = rectTransform.rect;
        float horizon = Mathf.Lerp(bounds.yMin, bounds.yMax, 0.38f);
        Vector2 vanishing = new Vector2(0f, horizon);

        AddLine(vh,
            new Vector2(bounds.xMin, horizon),
            new Vector2(bounds.xMax, horizon),
            7f,
            WithAlpha(Magenta, 0.09f),
            WithAlpha(Cyan, 0.09f));

        for (int i = -6; i <= 6; i++)
        {
            float u = i / 6f;
            Vector2 edge = new Vector2(
                Mathf.Lerp(bounds.center.x, u < 0f ? bounds.xMin : bounds.xMax,
                    Mathf.Abs(u)),
                bounds.yMin);
            Color ray = (i & 1) == 0 ? Cyan : Magenta;
            AddLine(vh, vanishing, edge, 3.2f,
                WithAlpha(ray, 0.018f), WithAlpha(ray, 0.12f));
        }

        for (int i = 1; i <= 11; i++)
        {
            float t = i / 11f;
            float y = Mathf.Lerp(horizon, bounds.yMin,
                Mathf.Pow(t, 1.72f));
            float alpha = Mathf.Lerp(0.025f, 0.105f, t);
            Color line = (i & 1) == 0 ? Cyan : Magenta;
            AddLine(vh,
                new Vector2(bounds.xMin, y),
                new Vector2(bounds.xMax, y),
                Mathf.Lerp(1.4f, 3.5f, t),
                WithAlpha(line, alpha), WithAlpha(line, alpha));
        }

        for (int side = -1; side <= 1; side += 2)
        {
            Color wall = side < 0 ? Cyan : Magenta;
            for (int i = 0; i < 3; i++)
            {
                float inset = 42f + i * 62f;
                Vector2 lower = new Vector2(
                    side < 0 ? bounds.xMin + inset : bounds.xMax - inset,
                    horizon - 25f - i * 28f);
                Vector2 upper = new Vector2(
                    side < 0 ? bounds.xMin : bounds.xMax,
                    bounds.yMax - 170f - i * 250f);
                AddLine(vh, lower, upper, 3f,
                    WithAlpha(wall, 0.08f), WithAlpha(wall, 0.018f));
            }
        }
    }

    static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    static void AddLine(VertexHelper vh, Vector2 from, Vector2 to,
        float width, Color fromColor, Color toColor)
    {
        Vector2 direction = to - from;
        if (direction.sqrMagnitude < 0.001f) return;
        Vector2 normal = new Vector2(-direction.y, direction.x).normalized *
                         (width * 0.5f);
        int start = vh.currentVertCount;
        vh.AddVert(from - normal, fromColor, Vector2.zero);
        vh.AddVert(from + normal, fromColor, Vector2.up);
        vh.AddVert(to + normal, toColor, Vector2.one);
        vh.AddVert(to - normal, toColor, Vector2.right);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }
}

// Adds live, restrained 2026 arcade lighting to the native CTA artwork.
// It creates no controls and never owns navigation or button listeners.
public sealed class MainMenuCtaLuminousSurface : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    MainMenuChamferedCtaGraphic surfaceGraphic;
    Button owner;
    Color accent;
    float phaseOffset;
    float baseOpacity;
    bool pointerPressed;

    public void Configure(Sprite frame, Color lightColor, bool primary)
    {
        accent = lightColor;
        phaseOffset = primary ? 0.38f : transform.name == "ButtonPvP" ? 0.12f : 0.64f;
        baseOpacity = primary ? 0.10f : 0.14f;

        owner = GetComponent<Button>();
        var surface = EnsureRect("HomeCtaInnerLight", transform);
        Stretch(surface);
        surface.SetAsFirstSibling();
        if (surface.GetComponent<CanvasRenderer>() == null)
            surface.gameObject.AddComponent<CanvasRenderer>();
        surfaceGraphic = surface.GetComponent<MainMenuChamferedCtaGraphic>();
        if (surfaceGraphic == null)
            surfaceGraphic = surface.gameObject.AddComponent<MainMenuChamferedCtaGraphic>();
        surfaceGraphic.raycastTarget = false;
        surfaceGraphic.Configure(accent, primary);
    }

    void Update()
    {
        if (surfaceGraphic == null) return;
        float pulse = 0.5f + 0.5f * Mathf.Sin(
            (Time.unscaledTime + phaseOffset) * Mathf.PI * 0.72f);
        surfaceGraphic.SetPresentation(
            baseOpacity + pulse * 0.035f,
            pointerPressed,
            owner != null && !owner.IsInteractable());
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (owner == null || owner.IsInteractable()) pointerPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerPressed = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerPressed = false;
    }

    static RectTransform EnsureRect(string name, Transform parent)
    {
        var child = parent.Find(name) as RectTransform;
        if (child != null) return child;
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

}

// Deterministic, text-free 2.5D CTA construction. It mirrors the approved
// chamfered production sheet with one dark outline, one luminous metal rim,
// a thick extruded base, a saturated body gradient and a controlled gloss.
// The geometry stays crisp at every Phase 1D viewport without stretching a
// rounded bitmap or baking localized copy into artwork.
public sealed class MainMenuChamferedCtaGraphic : MaskableGraphic
{
    Color accent = Color.white;
    bool primary;
    bool pressed;
    bool disabled;
    float pulse;

    public void Configure(Color lightColor, bool isPrimary)
    {
        accent = lightColor;
        primary = isPrimary;
        SetVerticesDirty();
    }

    public void SetPresentation(float lightPulse, bool isPressed, bool isDisabled)
    {
        float nextPulse = Mathf.Clamp01(lightPulse);
        if (Mathf.Abs(pulse - nextPulse) < 0.002f &&
            pressed == isPressed && disabled == isDisabled) return;
        pulse = nextPulse;
        pressed = isPressed;
        disabled = isDisabled;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect bounds = rectTransform.rect;
        float shortest = Mathf.Min(bounds.width, bounds.height);
        float chamfer = Mathf.Clamp(shortest * (primary ? 0.22f : 0.20f),
            24f, primary ? 56f : 42f);
        float down = pressed ? -7f : 0f;
        float dim = disabled ? 0.42f : pressed ? 0.78f : 1f;

        Color deepShadow = new Color(0.018f, 0.010f, 0.085f, 0.96f);
        Color outerInk = new Color(0.055f, 0.025f, 0.20f, 1f);
        Color rimDark = Multiply(accent, 0.42f, 1f);
        Color rimBright = Color.Lerp(accent, Color.white, 0.54f);
        Color bodyTop;
        Color bodyBottom;
        if (accent.r > 0.75f && accent.g > 0.45f)
        {
            bodyTop = new Color(1f, 0.71f, 0.045f, 1f);
            bodyBottom = new Color(0.96f, 0.36f, 0.008f, 1f);
        }
        else
        {
            bodyTop = new Color(0.025f, 0.75f, 1f, 1f);
            bodyBottom = new Color(0.008f, 0.20f, 0.73f, 1f);
        }

        AddChamfer(vh, Inset(bounds, 2f, -12f + down), chamfer,
            deepShadow, deepShadow);
        AddChamfer(vh, Inset(bounds, 3f, -4f + down), chamfer,
            Multiply(outerInk, dim, 1f), Multiply(outerInk, dim, 1f));
        AddChamfer(vh, Inset(bounds, 8f, down), chamfer - 4f,
            Multiply(rimDark, dim, 1f), Multiply(rimDark, dim, 1f));
        AddChamfer(vh, Inset(bounds, 13f, down), chamfer - 8f,
            Multiply(rimBright, dim, 1f), Multiply(accent, dim, 1f));
        AddChamfer(vh, Inset(bounds, 19f, down), chamfer - 13f,
            Multiply(outerInk, dim, 1f), Multiply(outerInk, dim, 1f));
        AddChamfer(vh, Inset(bounds, 24f, down), chamfer - 17f,
            Multiply(bodyTop, dim, 1f), Multiply(bodyBottom, dim, 1f));

        // Gloss is clipped to the same chamfered silhouette and occupies only
        // the upper material band, never crossing the live text baseline.
        Rect gloss = Inset(bounds, 32f, 1f + down);
        gloss.yMin = Mathf.Lerp(gloss.yMin, gloss.yMax, 0.54f);
        Color glossTop = new Color(1f, 1f, 1f,
            disabled ? 0.035f : 0.11f + pulse * 0.075f);
        Color glossBottom = new Color(1f, 1f, 1f, 0.01f);
        AddChamfer(vh, gloss, Mathf.Max(8f, chamfer - 24f),
            glossTop, glossBottom);

        // A slim lower accent makes the base read as tactile extrusion.
        Rect lower = Inset(bounds, 20f, -2f + down);
        lower.yMax = lower.yMin + Mathf.Max(8f, shortest * 0.065f);
        AddChamfer(vh, lower, Mathf.Max(5f, chamfer - 19f),
            new Color(accent.r, accent.g, accent.b,
                disabled ? 0.08f : 0.24f + pulse * 0.20f),
            new Color(accent.r, accent.g, accent.b, 0.015f));
    }

    static Rect Inset(Rect rect, float inset, float yOffset)
    {
        return new Rect(rect.xMin + inset, rect.yMin + inset + yOffset,
            Mathf.Max(1f, rect.width - inset * 2f),
            Mathf.Max(1f, rect.height - inset * 2f));
    }

    static Color Multiply(Color color, float value, float alpha)
    {
        return new Color(color.r * value, color.g * value, color.b * value,
            color.a * alpha);
    }

    static void AddChamfer(VertexHelper vh, Rect rect, float chamfer,
        Color top, Color bottom)
    {
        if (rect.width <= 1f || rect.height <= 1f) return;
        float c = Mathf.Clamp(chamfer, 1f,
            Mathf.Min(rect.width, rect.height) * 0.48f);
        Vector2[] points =
        {
            new Vector2(rect.xMin + c, rect.yMax),
            new Vector2(rect.xMax - c, rect.yMax),
            new Vector2(rect.xMax, rect.yMax - c),
            new Vector2(rect.xMax, rect.yMin + c),
            new Vector2(rect.xMax - c, rect.yMin),
            new Vector2(rect.xMin + c, rect.yMin),
            new Vector2(rect.xMin, rect.yMin + c),
            new Vector2(rect.xMin, rect.yMax - c)
        };
        int center = vh.currentVertCount;
        vh.AddVert(rect.center, Color.Lerp(bottom, top, 0.5f),
            new Vector2(0.5f, 0.5f));
        for (int i = 0; i < points.Length; i++)
        {
            float vertical = Mathf.InverseLerp(rect.yMin, rect.yMax, points[i].y);
            vh.AddVert(points[i], Color.Lerp(bottom, top, vertical),
                new Vector2(
                    Mathf.InverseLerp(rect.xMin, rect.xMax, points[i].x),
                    vertical));
        }
        for (int i = 0; i < points.Length; i++)
            vh.AddTriangle(center,
                center + 1 + ((i + 1) % points.Length),
                center + 1 + i);
    }
}

public enum MainMenuReferenceIconKind
{
    PrivateRoom,
    DailyHunt,
    Gear,
    Player
}

// Crisp resolution-independent symbols matching the approved reference.
// These shapes replace padded sticker PNGs and intentionally contain no text.
public sealed class MainMenuReferenceIconGraphic : MaskableGraphic
{
    MainMenuReferenceIconKind kind;

    public void Configure(MainMenuReferenceIconKind value)
    {
        kind = value;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = rectTransform.rect;
        float scale = Mathf.Min(rect.width, rect.height);
        Vector2 center = rect.center;
        Color ink = new Color(0.055f, 0.035f, 0.16f, 1f);
        Color white = new Color(0.94f, 0.98f, 1f, 1f);
        Color cyan = new Color(0.02f, 0.90f, 1f, 1f);
        Color shadow = new Color(0.01f, 0.01f, 0.08f, 0.58f);

        switch (kind)
        {
            case MainMenuReferenceIconKind.PrivateRoom:
                DrawPeople(vh, center + new Vector2(4f, -6f), scale, shadow);
                DrawPeople(vh, center, scale, white);
                break;
            case MainMenuReferenceIconKind.DailyHunt:
                DrawBolt(vh, center + new Vector2(5f, -7f), scale, shadow);
                DrawBolt(vh, center, scale, ink);
                break;
            case MainMenuReferenceIconKind.Gear:
                AddGearRing(vh, center + new Vector2(4f, -5f), scale * 0.47f,
                    scale * 0.385f, shadow);
                AddEllipse(vh, center, new Vector2(scale * 0.365f, scale * 0.365f),
                    new Color(0.025f, 0.018f, 0.12f, 1f), 32);
                AddGearRing(vh, center, scale * 0.44f, scale * 0.38f, cyan);
                break;
            case MainMenuReferenceIconKind.Player:
                AddRing(vh, center + new Vector2(3f, -4f), scale * 0.46f,
                    scale * 0.055f, shadow, 36);
                AddRing(vh, center, scale * 0.43f, scale * 0.045f, cyan, 36);
                AddEllipse(vh, center + new Vector2(0f, scale * 0.13f),
                    new Vector2(scale * 0.14f, scale * 0.17f), cyan, 28);
                AddEllipse(vh, center + new Vector2(0f, -scale * 0.17f),
                    new Vector2(scale * 0.255f, scale * 0.18f), cyan, 32);
                break;
        }
    }

    static void DrawPeople(VertexHelper vh, Vector2 center, float scale, Color color)
    {
        AddEllipse(vh, center + new Vector2(-scale * 0.17f, scale * 0.18f),
            new Vector2(scale * 0.15f, scale * 0.16f), color, 24);
        AddEllipse(vh, center + new Vector2(scale * 0.17f, scale * 0.25f),
            new Vector2(scale * 0.13f, scale * 0.14f), color, 24);
        AddShoulders(vh, center + new Vector2(-scale * 0.17f, -scale * 0.16f),
            scale * 0.34f, scale * 0.28f, color);
        AddShoulders(vh, center + new Vector2(scale * 0.18f, -scale * 0.11f),
            scale * 0.30f, scale * 0.25f, color);
    }

    static void DrawBolt(VertexHelper vh, Vector2 center, float scale, Color color)
    {
        Vector2[] points =
        {
            center + new Vector2(-0.08f, 0.46f) * scale,
            center + new Vector2(0.24f, 0.46f) * scale,
            center + new Vector2(0.03f, 0.09f) * scale,
            center + new Vector2(0.29f, 0.09f) * scale,
            center + new Vector2(-0.24f, -0.48f) * scale,
            center + new Vector2(-0.08f, -0.11f) * scale,
            center + new Vector2(-0.31f, -0.11f) * scale
        };
        AddPolygon(vh, points, color);
    }

    static void AddShoulders(VertexHelper vh, Vector2 center,
        float width, float height, Color color)
    {
        Vector2[] points =
        {
            center + new Vector2(-width * 0.50f, -height * 0.30f),
            center + new Vector2(-width * 0.42f, height * 0.20f),
            center + new Vector2(-width * 0.22f, height * 0.48f),
            center + new Vector2(width * 0.22f, height * 0.48f),
            center + new Vector2(width * 0.42f, height * 0.20f),
            center + new Vector2(width * 0.50f, -height * 0.30f)
        };
        AddPolygon(vh, points, color);
    }

    static void AddEllipse(VertexHelper vh, Vector2 center,
        Vector2 radius, Color color, int segments)
    {
        int start = vh.currentVertCount;
        vh.AddVert(center, color, new Vector2(0.5f, 0.5f));
        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            vh.AddVert(center + Vector2.Scale(direction, radius), color,
                direction * 0.5f + Vector2.one * 0.5f);
        }
        for (int i = 0; i < segments; i++)
            vh.AddTriangle(start, start + 1 + ((i + 1) % segments), start + 1 + i);
    }

    static void AddPolygon(VertexHelper vh, Vector2[] points, Color color)
    {
        Vector2 center = Vector2.zero;
        for (int i = 0; i < points.Length; i++) center += points[i];
        center /= points.Length;
        int start = vh.currentVertCount;
        vh.AddVert(center, color, new Vector2(0.5f, 0.5f));
        for (int i = 0; i < points.Length; i++)
            vh.AddVert(points[i], color, Vector2.zero);
        for (int i = 0; i < points.Length; i++)
            vh.AddTriangle(start, start + 1 + ((i + 1) % points.Length), start + 1 + i);
    }

    static void AddRing(VertexHelper vh, Vector2 center, float radius,
        float thickness, Color color, int segments)
    {
        for (int i = 0; i < segments; i++)
        {
            float a0 = Mathf.PI * 2f * i / segments;
            float a1 = Mathf.PI * 2f * (i + 1) / segments;
            Vector2 d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0));
            Vector2 d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
            int start = vh.currentVertCount;
            vh.AddVert(center + d0 * radius, color, Vector2.zero);
            vh.AddVert(center + d0 * (radius - thickness), color, Vector2.zero);
            vh.AddVert(center + d1 * (radius - thickness), color, Vector2.zero);
            vh.AddVert(center + d1 * radius, color, Vector2.zero);
            vh.AddTriangle(start, start + 2, start + 1);
            vh.AddTriangle(start, start + 3, start + 2);
        }
    }

    static void AddGearRing(VertexHelper vh, Vector2 center, float outerRadius,
        float innerRadius, Color color)
    {
        const int segments = 48;
        for (int i = 0; i < segments; i++)
        {
            float a0 = Mathf.PI * 2f * i / segments;
            float a1 = Mathf.PI * 2f * (i + 1) / segments;
            float r0 = GearRadius(i, outerRadius);
            float r1 = GearRadius(i + 1, outerRadius);
            Vector2 d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0));
            Vector2 d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
            int start = vh.currentVertCount;
            vh.AddVert(center + d0 * r0, color, Vector2.zero);
            vh.AddVert(center + d0 * innerRadius, color, Vector2.zero);
            vh.AddVert(center + d1 * innerRadius, color, Vector2.zero);
            vh.AddVert(center + d1 * r1, color, Vector2.zero);
            vh.AddTriangle(start, start + 2, start + 1);
            vh.AddTriangle(start, start + 3, start + 2);
        }
    }

    static float GearRadius(int index, float radius)
    {
        int phase = index % 4;
        return radius * (phase == 0 || phase == 1 ? 1f : 0.82f);
    }
}

public sealed class MainMenuPlayerChipGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect bounds = rectTransform.rect;
        float chamfer = Mathf.Min(26f, bounds.height * 0.24f);
        AddChamfer(vh, Offset(bounds, 0f, -7f), chamfer,
            new Color(0.015f, 0.008f, 0.07f, 0.92f),
            new Color(0.015f, 0.008f, 0.07f, 0.92f));
        AddChamfer(vh, Inset(bounds, 2f), chamfer,
            new Color(0.28f, 0.10f, 0.66f, 1f),
            new Color(0.12f, 0.04f, 0.30f, 1f));
        AddChamfer(vh, Inset(bounds, 5f), chamfer - 3f,
            new Color(0.42f, 0.17f, 0.78f, 1f),
            new Color(0.20f, 0.07f, 0.44f, 1f));
        AddChamfer(vh, Inset(bounds, 8f), chamfer - 6f,
            new Color(0.055f, 0.045f, 0.18f, 1f),
            new Color(0.018f, 0.014f, 0.075f, 1f));
    }

    static Rect Inset(Rect rect, float value)
    {
        return new Rect(rect.xMin + value, rect.yMin + value,
            rect.width - value * 2f, rect.height - value * 2f);
    }

    static Rect Offset(Rect rect, float x, float y)
    {
        rect.position += new Vector2(x, y);
        return rect;
    }

    static void AddChamfer(VertexHelper vh, Rect rect, float chamfer,
        Color top, Color bottom)
    {
        float c = Mathf.Clamp(chamfer, 1f,
            Mathf.Min(rect.width, rect.height) * 0.48f);
        Vector2[] points =
        {
            new Vector2(rect.xMin + c, rect.yMax),
            new Vector2(rect.xMax - c, rect.yMax),
            new Vector2(rect.xMax, rect.yMax - c),
            new Vector2(rect.xMax, rect.yMin + c),
            new Vector2(rect.xMax - c, rect.yMin),
            new Vector2(rect.xMin + c, rect.yMin),
            new Vector2(rect.xMin, rect.yMin + c),
            new Vector2(rect.xMin, rect.yMax - c)
        };
        int center = vh.currentVertCount;
        vh.AddVert(rect.center, Color.Lerp(bottom, top, 0.5f), Vector2.one * 0.5f);
        for (int i = 0; i < points.Length; i++)
        {
            float vertical = Mathf.InverseLerp(rect.yMin, rect.yMax, points[i].y);
            vh.AddVert(points[i], Color.Lerp(bottom, top, vertical), Vector2.zero);
        }
        for (int i = 0; i < points.Length; i++)
            vh.AddTriangle(center, center + 1 + ((i + 1) % points.Length), center + 1 + i);
    }
}
