using UnityEngine;
using UnityEngine.UI;

// Owns presentation only for the scene-authored Splash. SplashLoader remains
// the sole owner of timing, taps, and the transition to MainMenu.
[DefaultExecutionOrder(-2000)]
public sealed class SplashDesign : MonoBehaviour
{
    const string BackgroundResource = "splash/splash_bg_stairs_clouds";
    const string DecoStarsResource = "splash/splash_deco_stars";
    const string DecoLightningResource = "splash/splash_deco_lightning";
    const string DecoConfettiResource = "splash/splash_deco_confetti";
    const string DecoNumbersResource = "splash/splash_deco_numbers";
    const string GlowResource = "splash/splash_logo_glow";
    const string LogoResource = "reference/hol_logo_exact";
    const string HeroBoyResource = "splash/splash_char_boy";
    const string HeroGirlResource = "splash/splash_char_girl";
    const string MascotSixResource = "reference/mascot_6_exact";
    const string MascotSevenResource = "reference/mascot_7_exact";

    const float ReferenceWidth = 1080f;
    const float ReferenceHeight = 1920f;
    const float EntranceDuration = 0.65f;
    const float SevenDelay = 0.08f;

    RectTransform logoRect;
    RectTransform heroBoyRect;
    RectTransform heroGirlRect;
    RectTransform mascotSixRect;
    RectTransform mascotSevenRect;
    CanvasGroup logoGlowGroup;
    CanvasGroup logoGroup;
    CanvasGroup heroBoyGroup;
    CanvasGroup heroGirlGroup;
    CanvasGroup mascotSixGroup;
    CanvasGroup mascotSevenGroup;
    Image progressFill;
    float waitTime = 2.5f;
    float elapsed;

    public bool IsReady { get; private set; }
    public bool IsSettled { get; private set; }

    void Awake()
    {
        var canvas = FindSceneCanvas();
        if (canvas == null)
        {
            Debug.LogError("[SplashDesign] SplashScene has no root screen-space Canvas.");
            return;
        }

        HideLegacyPresentation(canvas.transform);

        var background = LoadSprite(BackgroundResource);
        var decoStars = LoadSprite(DecoStarsResource);
        var decoLightning = LoadSprite(DecoLightningResource);
        var decoConfetti = LoadSprite(DecoConfettiResource);
        var decoNumbers = LoadSprite(DecoNumbersResource);
        var glow = LoadSprite(GlowResource);
        var logo = LoadSprite(LogoResource);
        var heroBoy = LoadSprite(HeroBoyResource);
        var heroGirl = LoadSprite(HeroGirlResource);
        var mascotSix = LoadSprite(MascotSixResource);
        var mascotSeven = LoadSprite(MascotSevenResource);

        var visualRoot = EnsureRect(canvas.transform, "SplashVisualRoot");
        Stretch(visualRoot);

        var backgroundImage = EnsureImage(visualRoot, "SplashBackground");
        Stretch(backgroundImage.rectTransform);
        ConfigureImage(backgroundImage, background, false);

        var safeRoot = EnsureRect(visualRoot, "SplashSafeAreaRoot");
        ConfigureSafeArea(safeRoot, (RectTransform)canvas.transform);

        BuildDeco(safeRoot, "SplashDecoStars", decoStars);
        BuildDeco(safeRoot, "SplashDecoLightning", decoLightning);
        BuildDeco(safeRoot, "SplashDecoConfetti", decoConfetti);
        BuildDeco(safeRoot, "SplashDecoNumbers", decoNumbers);

        var glowImage = EnsureImage(safeRoot, "SplashLogoGlow");
        ConfigureImage(glowImage, glow, true);
        Place(glowImage.rectTransform, new Vector2(0f, 280f), new Vector2(960f, 620f));

        var logoImage = EnsureImage(safeRoot, "SplashLogo");
        ConfigureImage(logoImage, logo, true);
        Place(logoImage.rectTransform, new Vector2(0f, 280f), new Vector2(820f, 546f));
        logoRect = logoImage.rectTransform;

        var boyImage = EnsureImage(safeRoot, "SplashHeroBoy");
        ConfigureImage(boyImage, heroBoy, true);
        Place(boyImage.rectTransform, new Vector2(-155f, -40f), new Vector2(380f, 460f));
        heroBoyRect = boyImage.rectTransform;

        var girlImage = EnsureImage(safeRoot, "SplashHeroGirl");
        ConfigureImage(girlImage, heroGirl, true);
        Place(girlImage.rectTransform, new Vector2(155f, -40f), new Vector2(380f, 460f));
        heroGirlRect = girlImage.rectTransform;

        var sixImage = EnsureImage(safeRoot, "SplashMascotSix");
        ConfigureImage(sixImage, mascotSix, true);
        Place(sixImage.rectTransform, new Vector2(-340f, -420f), new Vector2(240f, 320f));
        mascotSixRect = sixImage.rectTransform;

        var sevenImage = EnsureImage(safeRoot, "SplashMascotSeven");
        ConfigureImage(sevenImage, mascotSeven, true);
        Place(sevenImage.rectTransform, new Vector2(340f, -420f), new Vector2(230f, 320f));
        mascotSevenRect = sevenImage.rectTransform;

        BuildProgress(safeRoot);

        logoGlowGroup = EnsureCanvasGroup(glowImage.gameObject);
        logoGroup = EnsureCanvasGroup(logoImage.gameObject);
        heroBoyGroup = EnsureCanvasGroup(boyImage.gameObject);
        heroGirlGroup = EnsureCanvasGroup(girlImage.gameObject);
        mascotSixGroup = EnsureCanvasGroup(sixImage.gameObject);
        mascotSevenGroup = EnsureCanvasGroup(sevenImage.gameObject);
        SetEntranceState();

        var loader = FindInScene<SplashLoader>();
        if (loader != null && loader.waitTime > 0f)
            waitTime = loader.waitTime;

        ApplyRequiredArtReadiness(
            background, glow, logo, heroBoy, heroGirl, mascotSix, mascotSeven);
    }

    void Update()
    {
        if (!IsReady) return;

        elapsed += Time.unscaledDeltaTime;
        if (progressFill != null)
        {
            float target = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, waitTime));
            progressFill.fillAmount = Mathf.Max(progressFill.fillAmount, target);
        }

        float logoT = Mathf.Clamp01(elapsed / EntranceDuration);
        float sixT = logoT;
        float sevenT = Mathf.Clamp01((elapsed - SevenDelay) / EntranceDuration);

        ApplyEntrance(logoGroup, logoRect, logoT, 0.86f);
        if (logoGlowGroup != null) logoGlowGroup.alpha = EaseOut(logoT);
        ApplyEntrance(heroBoyGroup, heroBoyRect, logoT, 0.92f);
        ApplyEntrance(heroGirlGroup, heroGirlRect, logoT, 0.92f);
        ApplyEntrance(mascotSixGroup, mascotSixRect, sixT, 0.92f);
        ApplyEntrance(mascotSevenGroup, mascotSevenRect, sevenT, 0.92f);

        IsSettled = logoT >= 1f && sixT >= 1f && sevenT >= 1f;
        if (IsSettled && logoRect != null)
        {
            float breathe = 1f + Mathf.Sin(Time.unscaledTime * 1.6f) * 0.01f;
            logoRect.localScale = new Vector3(breathe, breathe, 1f);
        }
    }

    static void BuildDeco(Transform safeRoot, string name, Sprite sprite)
    {
        var image = EnsureImage(safeRoot, name);
        ConfigureImage(image, sprite, false);
        Place(
            image.rectTransform,
            Vector2.zero,
            new Vector2(ReferenceWidth, ReferenceHeight));
    }

    void BuildProgress(Transform safeRoot)
    {
        var track = EnsureImage(safeRoot, "SplashProgressTrack");
        ConfigureImage(track, null, false);
        track.color = new Color(0.10f, 0.06f, 0.28f, 0.92f);
        Place(track.rectTransform, new Vector2(0f, -770f), new Vector2(480f, 8f));

        progressFill = EnsureImage(track.transform, "SplashProgressFill");
        var gold = new Color(1f, 0.78f, 0.34f, 1f);
        ConfigureImage(progressFill, RuntimeUI.SolidSprite, false);
        progressFill.color = Color.white;
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;
        progressFill.fillAmount = 0f;
        Stretch(progressFill.rectTransform);
    }

    void ConfigureSafeArea(RectTransform safeRoot, RectTransform canvasRect)
    {
        ResponsiveSafeAreaRoot.Attach(safeRoot, canvasRect,
            new Vector2(ReferenceWidth, ReferenceHeight));
    }

    void SetEntranceState()
    {
        elapsed = 0f;
        IsSettled = false;
        if (logoGlowGroup != null) logoGlowGroup.alpha = 0f;
        if (logoGroup != null) logoGroup.alpha = 0f;
        if (heroBoyGroup != null) heroBoyGroup.alpha = 0f;
        if (heroGirlGroup != null) heroGirlGroup.alpha = 0f;
        if (mascotSixGroup != null) mascotSixGroup.alpha = 0f;
        if (mascotSevenGroup != null) mascotSevenGroup.alpha = 0f;
        SetScale(logoRect, 0.86f);
        SetScale(heroBoyRect, 0.92f);
        SetScale(heroGirlRect, 0.92f);
        SetScale(mascotSixRect, 0.92f);
        SetScale(mascotSevenRect, 0.92f);
    }

    static void ApplyEntrance(
        CanvasGroup group, RectTransform rect, float normalizedTime, float startScale)
    {
        float eased = EaseOut(normalizedTime);
        if (group != null) group.alpha = eased;
        SetScale(rect, Mathf.Lerp(startScale, 1f, eased));
    }

    static float EaseOut(float value)
    {
        return 1f - Mathf.Pow(1f - Mathf.Clamp01(value), 3f);
    }

    static void SetScale(RectTransform rect, float scale)
    {
        if (rect != null) rect.localScale = new Vector3(scale, scale, 1f);
    }

    void ApplyRequiredArtReadiness(
        Sprite background, Sprite glow, Sprite logo, Sprite heroBoy, Sprite heroGirl,
        Sprite mascotSix, Sprite mascotSeven)
    {
        IsReady = RequiredArtReady(
            background, glow, logo, heroBoy, heroGirl, mascotSix, mascotSeven);
        if (!IsReady) IsSettled = false;
    }

    static bool RequiredArtReady(
        Sprite background, Sprite glow, Sprite logo, Sprite heroBoy, Sprite heroGirl,
        Sprite mascotSix, Sprite mascotSeven)
    {
        return background != null &&
               glow != null &&
               logo != null &&
               heroBoy != null &&
               heroGirl != null &&
               mascotSix != null &&
               mascotSeven != null;
    }

    static Rect NormalizedSafeArea(Rect safe, float width, float height)
    {
        return ResponsiveViewportGeometry.CalculateNormalizedSafeArea(safe, width, height);
    }

    Canvas FindSceneCanvas()
    {
        var scene = gameObject.scene;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var candidate in root.GetComponentsInChildren<Canvas>(true))
            {
                if (candidate.gameObject.scene != scene ||
                    !candidate.isRootCanvas ||
                    candidate.renderMode == RenderMode.WorldSpace)
                    continue;
                return candidate;
            }
        }
        return null;
    }

    T FindInScene<T>() where T : Component
    {
        var scene = gameObject.scene;
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }

    static void HideLegacyPresentation(Transform canvas)
    {
        var panel = DirectChild(canvas, "Panel");
        if (panel != null)
        {
            var panelImage = panel.GetComponent<Image>();
            if (panelImage != null) panelImage.enabled = false;
        }

        var logo = DirectChild(canvas, "Image");
        if (logo != null) logo.gameObject.SetActive(false);
    }

    static Sprite LoadSprite(string resource)
    {
        var sprite = Resources.Load<Sprite>(resource);
        if (sprite == null)
            Debug.LogError("[SplashDesign] Missing Resources/" + resource + ".");
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

    static CanvasGroup EnsureCanvasGroup(GameObject target)
    {
        var group = target.GetComponent<CanvasGroup>();
        if (group == null) group = target.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        return group;
    }

    static void ConfigureImage(Image image, Sprite sprite, bool preserveAspect)
    {
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
    }

    static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.identity;
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
}
