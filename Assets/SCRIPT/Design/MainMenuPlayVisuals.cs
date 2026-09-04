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
    public const string LogoName = "PlayLogo";
    public const string TitleName = "PlayHubTitle";
    public const string SubtitleName = "PlayHubSubtitle";
    public const string SoloIconName = "PlaySoloIcon";
    public const string SoloTitleName = "PlaySoloTitle";
    public const string SoloSubtitleName = "PlaySoloSubtitle";
    public const string FriendIconName = "PlayFriendIcon";
    public const string FriendTitleName = "PlayFriendTitle";
    public const string FriendSubtitleName = "PlayFriendSubtitle";
    public const string BackTitleName = "PlayBackTitle";

    const string BackgroundResource = "phase2a/hol_neon_reference_bg_r3";
    const string DecoStarsResource = "mainmenu/mainmenu_deco_stars";
    const string LogoResource = "reference/hol_logo_exact";
    const string GoldCtaResource = "phase2a/hol_cta_gold_r2_9s";
    const string BlueCtaResource = "phase2a/hol_cta_blue_r2_9s";
    const string SoloIconResource = "phase2a/hol_mode_solo_r2";
    const string FriendIconResource = "phase2a/hol_mode_private_r2";
    const string DisplayFontResource = "phase2a/fonts/HOL Menu Display SDF";
    const string BodyFontResource = "phase2a/fonts/HOL Menu Body SDF";

    const float ReferenceWidth = 1080f;
    const float ReferenceHeight = 1920f;

    static readonly Color Ink = new Color(0.09f, 0.05f, 0.16f, 1f);
    static readonly Color NearWhite = new Color(0.985f, 0.975f, 1f, 1f);
    static readonly Color Cyan = new Color(0.18f, 0.92f, 1f, 1f);

    public static readonly string[] LoadedResources =
    {
        BackgroundResource,
        DecoStarsResource,
        LogoResource,
        GoldCtaResource,
        BlueCtaResource,
        SoloIconResource,
        FriendIconResource,
    };

    public static readonly string[] LoadedFontResources =
    {
        DisplayFontResource,
        BodyFontResource,
    };

    RectTransform visualRoot;
    RectTransform safeRoot;
    RectTransform logoRect;
    RectTransform titleRect;
    RectTransform subtitleRect;
    RectTransform soloButtonRect;
    RectTransform friendButtonRect;
    RectTransform backButtonRect;
    TMP_FontAsset displayFont;
    TMP_FontAsset bodyFont;
    TMP_Text titleText;
    TMP_Text subtitleText;
    TMP_Text soloTitleText;
    TMP_Text soloSubtitleText;
    TMP_Text friendTitleText;
    TMP_Text friendSubtitleText;
    TMP_Text backTitleText;
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
            ApplyResponsiveLayout();
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
        Sprite stars = LoadRequired(DecoStarsResource);
        Sprite logo = LoadRequired(LogoResource);
        Sprite gold = LoadRequired(GoldCtaResource);
        Sprite blue = LoadRequired(BlueCtaResource);
        Sprite soloIcon = LoadRequired(SoloIconResource);
        Sprite friendIcon = LoadRequired(FriendIconResource);
        displayFont = Resources.Load<TMP_FontAsset>(DisplayFontResource);
        bodyFont = Resources.Load<TMP_FontAsset>(BodyFontResource);

        IsReady = background != null && stars != null && logo != null &&
                  gold != null && blue != null && soloIcon != null &&
                  friendIcon != null && displayFont != null && bodyFont != null;
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

        safeRoot = EnsureRect(visualRoot, SafeRootName);
        Stretch(safeRoot);
        ResponsiveSafeAreaRoot.Attach(
            safeRoot, (RectTransform)canvas.transform,
            new Vector2(ReferenceWidth, ReferenceHeight));

        Image starsImage = EnsureImage(safeRoot, "PlayDecoStars");
        ConfigureImage(starsImage, stars, false, Image.Type.Simple);
        Place(starsImage.rectTransform, Vector2.zero,
            new Vector2(ReferenceWidth, ReferenceHeight));

        Image logoImage = EnsureImage(safeRoot, LogoName);
        ConfigureImage(logoImage, logo, true, Image.Type.Simple);
        logoRect = logoImage.rectTransform;

        titleText = EnsureText(
            safeRoot, TitleName, 64f, displayFont, NearWhite,
            TextAlignmentOptions.Center);
        titleRect = titleText.rectTransform;
        ConfigureDisplayText(titleText, 42f, 66f);

        subtitleText = EnsureText(
            safeRoot, SubtitleName, 31f, bodyFont, Cyan,
            TextAlignmentOptions.Center);
        subtitleRect = subtitleText.rectTransform;
        ConfigureBodyText(subtitleText, 24f, 32f);

        soloButtonRect = RestyleModeButton(
            soloButton, safeRoot, gold, soloIcon, SoloIconName,
            SoloTitleName, "play_hub_solo_title",
            SoloSubtitleName, "play_hub_solo_subtitle", true);
        friendButtonRect = RestyleModeButton(
            friendButton, safeRoot, blue, friendIcon, FriendIconName,
            FriendTitleName, "play_hub_friend_title",
            FriendSubtitleName, "play_hub_friend_subtitle", false);
        backButtonRect = RestyleBackButton(backButton, safeRoot, blue);

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
        bool primary)
    {
        Reparent(button.transform, parent);
        HideChildGraphics(button.transform);
        RectTransform rect = (RectTransform)button.transform;
        Image image = button.GetComponent<Image>();
        if (image == null) image = button.gameObject.AddComponent<Image>();
        ConfigureInteractiveImage(image, frame, Image.Type.Sliced, 2f);
        button.targetGraphic = image;
        ConfigureButtonState(button);

        Image iconImage = EnsureImage(button.transform, iconName);
        ConfigureImage(iconImage, icon, true, Image.Type.Simple);
        Place(iconImage.rectTransform, new Vector2(-354f, 0f),
            new Vector2(154f, 154f));

        TMP_Text title = EnsureText(
            button.transform, titleName, primary ? 62f : 54f,
            displayFont, primary ? Ink : NearWhite,
            TextAlignmentOptions.Center);
        Place(title.rectTransform, new Vector2(72f, 35f),
            new Vector2(660f, 82f));
        ConfigureDisplayText(title, primary ? 42f : 34f,
            primary ? 64f : 56f);
        title.enableWordWrapping = false;

        TMP_Text subtitle = EnsureText(
            button.transform, subtitleName, 29f, bodyFont,
            NearWhite, TextAlignmentOptions.Center);
        Place(subtitle.rectTransform, new Vector2(72f, -30f),
            new Vector2(primary ? 660f : 680f, 64f));
        ConfigureBodyText(subtitle, 22f, 30f);
        subtitle.enableWordWrapping = false;

        if (primary)
        {
            AddTextShadow(title, 0.28f);
            AddTextShadow(subtitle, 0.20f);
        }
        else
        {
            AddTextShadow(title, 0.72f);
            AddTextShadow(subtitle, 0.58f);
        }

        SetLocalized(title, titleKey);
        SetLocalized(subtitle, subtitleKey);
        if (primary)
        {
            soloTitleText = title;
            soloSubtitleText = subtitle;
        }
        else
        {
            friendTitleText = title;
            friendSubtitleText = subtitle;
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
        ConfigureInteractiveImage(image, frame, Image.Type.Sliced, 2f);
        button.targetGraphic = image;
        ConfigureButtonState(button);

        backTitleText = EnsureText(
            button.transform, BackTitleName, 42f, displayFont, NearWhite,
            TextAlignmentOptions.Center);
        StretchText(backTitleText.rectTransform, 48f, 18f);
        ConfigureDisplayText(backTitleText, 32f, 44f);
        SetLocalized(backTitleText, "back");
        return rect;
    }

    void RefreshPresentation()
    {
        SetLocalized(titleText, "play_hub_title");
        SetLocalized(subtitleText, "play_hub_subtitle");
        SetLocalized(soloTitleText, "play_hub_solo_title");
        SetLocalized(soloSubtitleText, "play_hub_solo_subtitle");
        SetLocalized(friendTitleText, "play_hub_friend_title");
        SetLocalized(friendSubtitleText, "play_hub_friend_subtitle");
        SetLocalized(backTitleText, "back");
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
        if (logoRect == null || titleRect == null || subtitleRect == null ||
            soloButtonRect == null || friendButtonRect == null ||
            backButtonRect == null)
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

        Place(logoRect, new Vector2(0f, 610f + 80f * tall),
            new Vector2(620f, 340f));
        Place(titleRect, new Vector2(0f, 310f),
            new Vector2(900f, 100f));
        Place(subtitleRect, new Vector2(0f, 235f),
            new Vector2(880f, 64f));
        Place(soloButtonRect, new Vector2(0f, 70f + 20f * tall),
            new Vector2(920f, 210f));
        Place(friendButtonRect, new Vector2(0f, -190f + 8f * tall),
            new Vector2(920f, 210f));
        Place(backButtonRect, new Vector2(0f, -455f - 28f * tall),
            new Vector2(700f, 120f));

        if (friendTitleText != null)
        {
            friendTitleText.fontSizeMin = language == L10n.Language.Greek
                ? 32f : 34f;
            friendTitleText.fontSizeMax = 56f;
        }
        if (friendSubtitleText != null)
        {
            friendSubtitleText.fontSizeMin = language == L10n.Language.Greek
                ? 23f : 24f;
            friendSubtitleText.fontSizeMax = 30f;
        }

        Canvas.ForceUpdateCanvases();
        ForceMesh(titleText, subtitleText, soloTitleText, soloSubtitleText,
            friendTitleText, friendSubtitleText, backTitleText);
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

    static void StretchText(RectTransform rect, float horizontal, float vertical)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontal, vertical);
        rect.offsetMax = new Vector2(-horizontal, -vertical);
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
