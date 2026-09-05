using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Sole presentation owner for the real Main Menu mode selector. It restyles
// the scene-authored Solo and Back controls plus the one runtime private-room
// control without replacing any of their authoritative callbacks.
[DefaultExecutionOrder(1700)]
public sealed class MainMenuPlayVisuals : MonoBehaviour
{
    public const string VisualRootName = "PlayVisualRoot";
    public const string SafeRootName = "PlaySafeAreaRoot";
    public const string BackgroundName = "PlayBackground";
    public const string DecorationsName = "PlayDecorations";
    public const string LogoName = "PlayLogo";
    public const string PromptRibbonName = "PlayTitleRibbon";
    public const string TitleName = "PlayHubTitle";
    public const string SubtitleName = "PlayHubSubtitle";
    public const string SoloIconName = "PlaySoloIcon";
    public const string SoloTitleName = "PlaySoloTitle";
    public const string SoloSubtitleName = "PlaySoloSubtitle";
    public const string SoloActionName = "PlaySoloAction";
    public const string FriendIconName = "PlayFriendIcon";
    public const string FriendTitleName = "PlayFriendTitle";
    public const string FriendSubtitleName = "PlayFriendSubtitle";
    public const string FriendActionName = "PlayFriendAction";
    public const string MascotSevenName = "PlayMascotSeven";
    public const string MascotThreeName = "PlayMascotThree";

    const string BackgroundResource = "solo/production/solo_background_v1";
    const string DecorationsResource = "solo/production/solo_decorations_v1";
    const string LogoResource = "reference/hol_logo_exact";
    const string PromptRibbonResource =
        "solo/production/solo_prompt_ribbon_v1";
    const string SoloCardResource =
        "solo/production/solo_player_card_shell_v1";
    const string FriendCardResource =
        "solo/production/solo_opponent_card_shell_v1";
    const string BackButtonResource =
        "solo/production/solo_back_button_v1";
    const string SoloIconResource = "phase2a/hol_mode_solo_r2";
    const string FriendIconResource = "phase2a/hol_mode_private_r2";
    const string MascotSevenResource = "reference/mascot_7_exact";
    const string MascotThreeResource = "reference/mascot_3_exact";
    const string DisplayFontResource = "phase2a/fonts/HOL Menu Display SDF";
    const string BodyFontResource = "phase2a/fonts/HOL Menu Body SDF";

    const float ReferenceWidth = 1080f;
    const float ReferenceHeight = 1920f;

    static readonly Color NearWhite = new Color(0.985f, 0.975f, 1f, 1f);
    static readonly Color Cyan = new Color(0.18f, 0.92f, 1f, 1f);

    public static readonly string[] LoadedResources =
    {
        BackgroundResource,
        DecorationsResource,
        LogoResource,
        PromptRibbonResource,
        SoloCardResource,
        FriendCardResource,
        BackButtonResource,
        SoloIconResource,
        FriendIconResource,
        MascotSevenResource,
        MascotThreeResource,
    };

    public static readonly string[] LoadedFontResources =
    {
        DisplayFontResource,
        BodyFontResource,
    };

    RectTransform visualRoot;
    RectTransform safeRoot;
    RectTransform logoRect;
    RectTransform promptRibbonRect;
    RectTransform titleRect;
    RectTransform subtitleRect;
    RectTransform soloButtonRect;
    RectTransform friendButtonRect;
    RectTransform backButtonRect;
    RectTransform mascotSevenRect;
    RectTransform mascotThreeRect;
    TMP_FontAsset displayFont;
    TMP_FontAsset bodyFont;
    TMP_Text titleText;
    TMP_Text subtitleText;
    TMP_Text soloTitleText;
    TMP_Text soloSubtitleText;
    TMP_Text soloActionText;
    TMP_Text friendTitleText;
    TMP_Text friendSubtitleText;
    TMP_Text friendActionText;
    MenuManager menu;
    PvpGameController pvpController;
    Button soloButton;
    Button friendButton;
    Button backButton;
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

        Canvas canvas = null;
        MenuManager owner = FindInScene<MenuManager>(scene);
        if (owner != null && owner.mainMenuPanel != null)
            canvas = owner.mainMenuPanel.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Canvas candidate in
                         root.GetComponentsInChildren<Canvas>(true))
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

        if (canvas != null && canvas.GetComponent<MainMenuPlayVisuals>() == null)
            canvas.gameObject.AddComponent<MainMenuPlayVisuals>();
    }

    IEnumerator Start()
    {
        for (int frame = 0; frame < 120; frame++)
        {
            menu = FindInScene<MenuManager>(gameObject.scene);
            pvpController = FindInScene<PvpGameController>(gameObject.scene);
            soloButton = FindButton("ButtonChallenger");
            friendButton = FindButton("ButtonPvP");
            backButton = FindButton("ButtonBack");
            if (menu != null && menu.panelPlay != null &&
                pvpController != null && soloButton != null &&
                friendButton != null && backButton != null)
                break;
            yield return null;
        }

        BuildPlay();
        if (IsReady)
        {
            yield return null;
            if (!Application.isBatchMode)
                yield return new WaitForEndOfFrame();
            HideRetiredSelectorPresentation();
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
        RemovePresentationListeners();
    }

    void LateUpdate()
    {
        if (!laidOut || visualRoot == null) return;
        HideRetiredSelectorPresentation();
        bool visible = IsIdlePlayVisible();
        if (visualRoot.gameObject.activeSelf != visible)
            visualRoot.gameObject.SetActive(visible);
        if (visible)
        {
            ApplyResponsiveLayout();
            CenterVisibleText();
        }
    }

    bool IsIdlePlayVisible()
    {
        menu = menu ?? FindInScene<MenuManager>(gameObject.scene);
        if (menu == null || menu.panelPlay == null ||
            !menu.panelPlay.activeSelf)
            return false;
        if (menu.panelSearching != null && menu.panelSearching.activeSelf)
            return false;
        FakeMatchmaking matchmaking =
            FindInScene<FakeMatchmaking>(gameObject.scene);
        if (matchmaking != null && matchmaking.panelGame != null &&
            matchmaking.panelGame.activeSelf)
            return false;
        return true;
    }

    void BuildPlay()
    {
        Canvas canvas = GetComponent<Canvas>();
        menu = menu ?? FindInScene<MenuManager>(gameObject.scene);
        pvpController = pvpController ??
            FindInScene<PvpGameController>(gameObject.scene);
        if (canvas == null || menu == null || menu.panelPlay == null ||
            pvpController == null || soloButton == null ||
            friendButton == null || backButton == null)
        {
            Debug.LogError(
                "[MainMenuPlayVisuals] Missing selector controls or owners.");
            return;
        }

        Sprite background = LoadRequired(BackgroundResource);
        Sprite decorations = LoadRequired(DecorationsResource);
        Sprite logo = LoadRequired(LogoResource);
        Sprite promptRibbon = LoadRequired(PromptRibbonResource);
        Sprite soloCard = LoadRequired(SoloCardResource);
        Sprite friendCard = LoadRequired(FriendCardResource);
        Sprite backSprite = LoadRequired(BackButtonResource);
        Sprite soloIcon = LoadRequired(SoloIconResource);
        Sprite friendIcon = LoadRequired(FriendIconResource);
        Sprite mascotSeven = LoadRequired(MascotSevenResource);
        Sprite mascotThree = LoadRequired(MascotThreeResource);
        displayFont = Resources.Load<TMP_FontAsset>(DisplayFontResource);
        bodyFont = Resources.Load<TMP_FontAsset>(BodyFontResource);

        IsReady = background != null && decorations != null && logo != null &&
                  promptRibbon != null && soloCard != null &&
                  friendCard != null && backSprite != null &&
                  soloIcon != null && friendIcon != null &&
                  mascotSeven != null && mascotThree != null &&
                  displayFont != null && bodyFont != null;
        if (!IsReady)
        {
            Debug.LogError(
                "[MainMenuPlayVisuals] Required selector art or fonts are missing.");
            return;
        }

        Transform panel = menu.panelPlay.transform;
        Image panelImage = menu.panelPlay.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.enabled = false;
            panelImage.raycastTarget = false;
        }

        HideRetiredSelectorPresentation();

        visualRoot = EnsureRect(panel, VisualRootName);
        Stretch(visualRoot);
        visualRoot.SetAsFirstSibling();

        Image bg = EnsureImage(visualRoot, BackgroundName);
        Stretch(bg.rectTransform);
        ConfigureImage(bg, background, false, Image.Type.Simple);

        Image decorationImage = EnsureImage(visualRoot, DecorationsName);
        Stretch(decorationImage.rectTransform);
        ConfigureImage(
            decorationImage, decorations, false, Image.Type.Simple);

        safeRoot = EnsureRect(visualRoot, SafeRootName);
        Stretch(safeRoot);
        ResponsiveSafeAreaRoot.Attach(
            safeRoot, (RectTransform)canvas.transform,
            new Vector2(ReferenceWidth, ReferenceHeight));

        Image logoImage = EnsureImage(safeRoot, LogoName);
        ConfigureImage(logoImage, logo, true, Image.Type.Simple);
        logoRect = logoImage.rectTransform;

        Image ribbonImage = EnsureImage(safeRoot, PromptRibbonName);
        ConfigureImage(ribbonImage, promptRibbon, true, Image.Type.Simple);
        promptRibbonRect = ribbonImage.rectTransform;

        titleText = EnsureText(
            ribbonImage.transform, TitleName, 64f, displayFont, NearWhite,
            TextAlignmentOptions.Center);
        titleRect = titleText.rectTransform;
        ConfigureDisplayText(titleText, 42f, 66f);
        Place(titleRect, new Vector2(0f, 8f), new Vector2(650f, 94f));

        subtitleText = EnsureText(
            safeRoot, SubtitleName, 31f, bodyFont, Cyan,
            TextAlignmentOptions.Center);
        subtitleRect = subtitleText.rectTransform;
        ConfigureBodyText(subtitleText, 24f, 32f);

        Image sevenImage = EnsureImage(safeRoot, MascotSevenName);
        ConfigureImage(sevenImage, mascotSeven, true, Image.Type.Simple);
        mascotSevenRect = sevenImage.rectTransform;

        Image threeImage = EnsureImage(safeRoot, MascotThreeName);
        ConfigureImage(threeImage, mascotThree, true, Image.Type.Simple);
        mascotThreeRect = threeImage.rectTransform;

        soloButtonRect = RestyleModeButton(
            soloButton, safeRoot, soloCard, soloIcon, SoloIconName,
            SoloTitleName, "play_hub_solo_title",
            SoloSubtitleName, "play_hub_solo_subtitle",
            SoloActionName, true);
        friendButtonRect = RestyleModeButton(
            friendButton, safeRoot, friendCard, friendIcon, FriendIconName,
            FriendTitleName, "play_hub_friend_title",
            FriendSubtitleName, "play_hub_friend_subtitle",
            FriendActionName, false);
        backButtonRect = RestyleBackButton(
            backButton, safeRoot, backSprite);

        RemovePresentationListeners();
        soloButton.onClick.AddListener(menu.ClosePlayHubForSoloSelection);
        friendButton.onClick.AddListener(
            menu.ClosePlayHubForPrivateRoomSelection);

        RefreshPresentation();
        ApplyResponsiveLayoutForViewport(Screen.width, Screen.height, true);
    }

    RectTransform RestyleModeButton(
        Button button,
        Transform parent,
        Sprite frame,
        Sprite icon,
        string iconName,
        string titleName,
        string titleKey,
        string subtitleName,
        string subtitleKey,
        string actionName,
        bool primary)
    {
        Reparent(button.transform, parent);
        HideChildGraphics(button.transform);
        RectTransform rect = (RectTransform)button.transform;
        Image image = button.GetComponent<Image>();
        if (image == null) image = button.gameObject.AddComponent<Image>();
        // The real callback-bearing mode button is also the complete approved
        // actor-card surface. No decorative clone sits over its hit target.
        ConfigureInteractiveImage(image, frame, Image.Type.Simple, 1f);
        // Ignore only the authored transparent side gutters, so the enlarged
        // paired card surfaces retain separate touch ownership.
        image.raycastPadding = new Vector4(40f, 0f, 40f, 0f);
        button.targetGraphic = image;
        ConfigureButtonState(button);
        ButtonJuice juice = RuntimeUI.AttachJuice(button);
        rect.localScale = Vector3.one;
        if (juice != null)
            juice.ResetBaseScale(Vector3.one);

        Image iconImage = EnsureImage(button.transform, iconName);
        ConfigureImage(iconImage, icon, true, Image.Type.Simple);
        Place(iconImage.rectTransform, new Vector2(0f, 73f),
            new Vector2(270f, 270f));

        TMP_Text title = EnsureText(
            button.transform, titleName, primary ? 36f : 30f,
            displayFont, NearWhite,
            TextAlignmentOptions.Center);
        Place(title.rectTransform,
            primary ? new Vector2(-16f, 283f) : new Vector2(7f, 283f),
            new Vector2(210f, 76f));
        ConfigureDisplayText(title, primary ? 32f : 24f,
            primary ? 40f : 30f);
        title.enableWordWrapping = !primary;
        title.lineSpacing = 0f;
        title.overflowMode = TextOverflowModes.Truncate;

        TMP_Text subtitle = EnsureText(
            button.transform, subtitleName, 27f, bodyFont,
            NearWhite, TextAlignmentOptions.Center);
        Place(subtitle.rectTransform, new Vector2(0f, -100f),
            new Vector2(392f, 120f));
        ConfigureBodyText(subtitle, 21f, 28f);
        subtitle.enableWordWrapping = true;
        subtitle.lineSpacing = 0f;
        subtitle.overflowMode = TextOverflowModes.Truncate;

        TMP_Text action = EnsureText(
            button.transform, actionName, 45f, displayFont,
            NearWhite, TextAlignmentOptions.Center);
        Place(action.rectTransform, new Vector2(0f, -267f),
            new Vector2(414f, 86f));
        ConfigureDisplayText(action, 36f, 52f);
        action.enableWordWrapping = false;
        action.fontStyle |= FontStyles.UpperCase;

        if (primary)
        {
            AddTextShadow(title, 0.58f);
            AddTextShadow(subtitle, 0.48f);
        }
        else
        {
            AddTextShadow(title, 0.72f);
            AddTextShadow(subtitle, 0.58f);
        }

        SetLocalized(title, titleKey);
        SetLocalized(subtitle, subtitleKey);
        SetLocalized(action, "play");
        if (primary)
        {
            soloTitleText = title;
            soloSubtitleText = subtitle;
            soloActionText = action;
        }
        else
        {
            friendTitleText = title;
            friendSubtitleText = subtitle;
            friendActionText = action;
        }
        return rect;
    }

    RectTransform RestyleBackButton(
        Button button,
        Transform parent,
        Sprite frame)
    {
        Reparent(button.transform, parent);
        HideChildGraphics(button.transform);
        RectTransform rect = (RectTransform)button.transform;
        Image image = button.GetComponent<Image>();
        if (image == null) image = button.gameObject.AddComponent<Image>();
        ConfigureImage(image, frame, true, Image.Type.Simple);
        image.raycastTarget = true;
        button.targetGraphic = image;
        ConfigureButtonState(button);
        ButtonJuice juice = RuntimeUI.AttachJuice(button);
        rect.localScale = Vector3.one;
        if (juice != null)
            juice.ResetBaseScale(Vector3.one);
        return rect;
    }

    void RefreshPresentation()
    {
        SetLocalized(titleText, "play_hub_title");
        SetLocalized(subtitleText, "play_hub_subtitle");
        SetLocalized(soloTitleText, "play_hub_solo_title");
        SetLocalized(soloSubtitleText, "play_hub_solo_subtitle");
        SetLocalized(soloActionText, "play");
        SetLocalized(friendTitleText, "play_hub_friend_title");
        SetLocalized(friendSubtitleText, "play_hub_friend_subtitle");
        SetLocalized(friendActionText, "play");
        ApplyResponsiveLayoutForViewport(Screen.width, Screen.height, true);
    }

    void ApplyResponsiveLayout()
    {
        ApplyResponsiveLayoutForViewport(Screen.width, Screen.height);
    }

    // Deterministic layout seam used by focused PlayMode viewport validation.
    void ApplyResponsiveLayoutForViewport(
        int width,
        int height,
        bool force = false)
    {
        if (logoRect == null || promptRibbonRect == null || titleRect == null ||
            subtitleRect == null ||
            soloButtonRect == null || friendButtonRect == null ||
            backButtonRect == null || mascotSevenRect == null ||
            mascotThreeRect == null)
            return;

        L10n.Language language = L10n.Current;
        if (!force && width == lastLayoutWidth &&
            height == lastLayoutHeight && language == lastLanguage)
            return;

        lastLayoutWidth = width;
        lastLayoutHeight = height;
        lastLanguage = language;

        float aspect = width > 0
            ? Mathf.Max(1, height) / (float)width
            : ReferenceHeight / ReferenceWidth;
        float tall = Mathf.InverseLerp(1.78f, 2.22f, aspect);

        Place(backButtonRect, new Vector2(-452f, 846f + 34f * tall),
            new Vector2(118f, 118f));
        Place(logoRect, new Vector2(0f, 760f + 40f * tall),
            new Vector2(403.3f, 234.35f));
        Place(promptRibbonRect, new Vector2(0f, 525f + 32f * tall),
            new Vector2(720f, 215f));
        Place(subtitleRect, new Vector2(0f, 370f + 24f * tall),
            new Vector2(850f, 70f));

        // These are the two real, callback-bearing choices. Their geometry is
        // intentionally identical so neither mode implies unavailable status.
        // Grow downward while retaining the safe-width gutters and top edge.
        // Art, copy and CTA are independently reflowed inside the taller faces;
        // neither the Canvas nor child transforms receive a blind scale.
        Place(soloButtonRect, new Vector2(-260f, -110f + 10f * tall),
            new Vector2(560f, 920f));
        Place(friendButtonRect, new Vector2(260f, -110f + 10f * tall),
            new Vector2(560f, 920f));

        foreach (string iconName in new[] { SoloIconName, FriendIconName })
        {
            Transform icon = DeepFind(safeRoot, iconName);
            if (icon != null)
                Place((RectTransform)icon, new Vector2(0f, 93f),
                    new Vector2(330f, 330f));
        }

        Place(mascotSevenRect,
            new Vector2(-325f, -713f - 20f * tall),
            new Vector2(360f, 410f));
        Place(mascotThreeRect,
            new Vector2(325f, -713f - 20f * tall),
            new Vector2(360f, 410f));

        if (friendTitleText != null)
        {
            friendTitleText.fontSizeMin = 24f;
            friendTitleText.fontSizeMax = 34f;
        }
        if (friendSubtitleText != null)
        {
            friendSubtitleText.fontSizeMin = 21f;
            friendSubtitleText.fontSizeMax = 34f;
        }
        if (soloTitleText != null) soloTitleText.fontSizeMax = 46f;
        if (soloSubtitleText != null) soloSubtitleText.fontSizeMax = 34f;
        if (soloActionText != null) soloActionText.fontSizeMax = 62f;
        if (friendActionText != null) friendActionText.fontSizeMax = 62f;

        Canvas.ForceUpdateCanvases();
        ForceMesh(titleText, subtitleText, soloTitleText, soloSubtitleText,
            soloActionText, friendTitleText, friendSubtitleText,
            friendActionText);

        // The inner panel has two authored surfaces: supporting copy in its
        // upper dark gradient and PLAY in the lower button face. Preserve their
        // exact normalized glyph centres within the vertically enlarged faces.
        CenteredTextRegions = new[]
        {
            new MainMenuCenteredTextRegion(titleText, 0f, 8f, 650f, 94f),
            new MainMenuCenteredTextRegion(subtitleText, 0f, 370f + 24f * tall, 850f, 70f),
            new MainMenuCenteredTextRegion(soloTitleText, -16f, 361.6f, 210f, 86.9f),
            new MainMenuCenteredTextRegion(friendTitleText, 7f, 361.6f, 210f, 86.9f),
            new MainMenuCenteredTextRegion(soloSubtitleText, -16f, -228.7f, 368f, 89.4f),
            new MainMenuCenteredTextRegion(friendSubtitleText, 7f, -228.7f, 368f, 89.4f),
            new MainMenuCenteredTextRegion(soloActionText, -16f, -327.1f, 368f, 79.2f),
            new MainMenuCenteredTextRegion(friendActionText, 7f, -327.1f, 368f, 79.2f),
        };
        CenterVisibleText();
    }

    void CenterVisibleText()
    {
        if (CenteredTextRegions == null) return;
        // Safe-area language refresh reapplies the generic wrapping policy.
        // This screen owns its one-line VS AI tab; restore that intent after
        // refresh, before measuring the final localized glyphs.
        if (soloTitleText != null) soloTitleText.enableWordWrapping = false;
        foreach (MainMenuCenteredTextRegion region in CenteredTextRegions)
            region.Apply();
    }

    void RemovePresentationListeners()
    {
        if (menu == null) return;
        if (soloButton != null)
            soloButton.onClick.RemoveListener(
                menu.ClosePlayHubForSoloSelection);
        if (friendButton != null)
            friendButton.onClick.RemoveListener(
                menu.ClosePlayHubForPrivateRoomSelection);
    }

    void HideRetiredSelectorPresentation()
    {
        if (menu == null || menu.panelPlay == null) return;
        Transform panel = menu.panelPlay.transform;
        HideAllNamed(panel, "ExactPlayLogo");
        HideAllNamed(panel, "PlayDisclosure");
        HideAllNamed(panel, "DisclosureLabel");
    }

    Button FindButton(string name)
    {
        Transform found = DeepFind(transform, name);
        return found == null ? null : found.GetComponent<Button>();
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
        TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>();
        if (text == null) text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
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

    static void ConfigureButtonState(Button button)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.80f, 0.84f, 0.94f, 1f);
        colors.disabledColor = new Color(0.55f, 0.56f, 0.64f, 0.72f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.06f;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = colors;
    }

    static void AddTextShadow(TMP_Text text, float alpha)
    {
        if (text == null) return;
        Shadow shadow = text.GetComponent<Shadow>();
        if (shadow == null) shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.02f, 0.01f, 0.12f, alpha);
        shadow.effectDistance = new Vector2(2f, -3f);
        shadow.useGraphicAlpha = true;
    }

    static void HideChildGraphics(Transform root)
    {
        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic.transform == root) continue;
            graphic.gameObject.SetActive(false);
        }
    }

    static void Reparent(Transform child, Transform parent)
    {
        if (child.parent != parent)
            child.SetParent(parent, false);
        child.gameObject.SetActive(true);
        child.SetAsLastSibling();
    }

    static void SetLocalized(TMP_Text text, string key)
    {
        if (text == null) return;
        LocalizedText localized = text.GetComponent<LocalizedText>();
        if (localized == null)
        {
            RuntimeUI.Localize(text, key);
            localized = text.GetComponent<LocalizedText>();
        }
        if (localized != null) localized.key = key;
        text.text = L10n.Get(key);
    }

    static Sprite LoadRequired(string path)
    {
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite == null)
            Debug.LogError(
                "[MainMenuPlayVisuals] Missing Resources/" + path + ".");
        return sprite;
    }

    static RectTransform EnsureRect(Transform parent, string name)
    {
        RectTransform existing = DirectChild(parent, name) as RectTransform;
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
        Image image = rect.GetComponent<Image>();
        if (image == null) image = rect.gameObject.AddComponent<Image>();
        return image;
    }

    static void ConfigureImage(
        Image image,
        Sprite sprite,
        bool preserveAspect,
        Image.Type type)
    {
        image.enabled = true;
        image.sprite = sprite;
        image.color = Color.white;
        image.type = type;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
    }

    static void ConfigureInteractiveImage(
        Image image,
        Sprite sprite,
        Image.Type type,
        float pixelsPerUnitMultiplier)
    {
        ConfigureImage(image, sprite, false, type);
        image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
        image.raycastTarget = true;
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

    static void ForceMesh(params TMP_Text[] texts)
    {
        foreach (TMP_Text text in texts)
            if (text != null && text.gameObject.activeInHierarchy)
                text.ForceMeshUpdate(true, true);
    }

    static Transform DirectChild(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int index = 0; index < parent.childCount; index++)
            if (parent.GetChild(index).name == name)
                return parent.GetChild(index);
        return null;
    }

    static Transform DeepFind(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name) return parent;
        for (int index = 0; index < parent.childCount; index++)
        {
            Transform found = DeepFind(parent.GetChild(index), name);
            if (found != null) return found;
        }
        return null;
    }

    static void HideAllNamed(Transform parent, string name)
    {
        if (parent == null) return;
        for (int index = parent.childCount - 1; index >= 0; index--)
        {
            Transform child = parent.GetChild(index);
            HideAllNamed(child, name);
            if (child.name == name)
                child.gameObject.SetActive(false);
        }
    }

    static T FindInScene<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid()) return null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }
}
