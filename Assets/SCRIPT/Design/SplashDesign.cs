using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Owns presentation only for the scene-authored Splash. SplashLoader remains
// the sole owner of timing, taps, and the transition to MainMenu.
[DefaultExecutionOrder(-2000)]
public sealed class SplashDesign : MonoBehaviour
{
    const string BackgroundResource = "phase2a/hol_neon_arena_bg_r2";
    const string LoadingTrackResource = "phase2a/hol_loading_track_r2_9s";
    const string LogoResource = "reference/hol_logo_exact";
    const string HeroBoyResource = "splash/splash_char_boy";
    const string HeroGirlResource = "splash/splash_char_girl";

    const float ReferenceWidth = 1080f;
    const float ReferenceHeight = 1920f;
    const float EntranceDuration = 0.65f;
    const float HeroDelay = 0.08f;

    RectTransform logoRect;
    RectTransform heroBoyRect;
    RectTransform heroGirlRect;
    CanvasGroup logoGroup;
    CanvasGroup heroBoyGroup;
    CanvasGroup heroGirlGroup;
    Image progressFill;
    Image progressCap;
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
        var loadingTrack = LoadSprite(LoadingTrackResource);
        var logo = LoadSprite(LogoResource);
        var heroBoy = LoadSprite(HeroBoyResource);
        var heroGirl = LoadSprite(HeroGirlResource);

        var visualRoot = EnsureRect(canvas.transform, "SplashVisualRoot");
        Stretch(visualRoot);

        var backgroundImage = EnsureImage(visualRoot, "SplashBackground");
        Stretch(backgroundImage.rectTransform);
        ConfigureImage(backgroundImage, background, false);
        backgroundImage.color = Color.white;

        var safeRoot = EnsureRect(visualRoot, "SplashSafeAreaRoot");
        ConfigureSafeArea(safeRoot, (RectTransform)canvas.transform);

        var logoImage = EnsureImage(safeRoot, "SplashLogo");
        ConfigureImage(logoImage, logo, true);
        Place(logoImage.rectTransform, new Vector2(0f, 570f), new Vector2(760f, 506f));
        logoRect = logoImage.rectTransform;

        var boyImage = EnsureImage(safeRoot, "SplashHeroBoy");
        ConfigureImage(boyImage, heroBoy, true);
        Place(boyImage.rectTransform, new Vector2(-205f, 20f), new Vector2(515f, 630f));
        heroBoyRect = boyImage.rectTransform;

        var girlImage = EnsureImage(safeRoot, "SplashHeroGirl");
        ConfigureImage(girlImage, heroGirl, true);
        Place(girlImage.rectTransform, new Vector2(205f, 20f), new Vector2(515f, 630f));
        heroGirlRect = girlImage.rectTransform;

        BuildProgress(safeRoot, loadingTrack);

        logoGroup = EnsureCanvasGroup(logoImage.gameObject);
        heroBoyGroup = EnsureCanvasGroup(boyImage.gameObject);
        heroGirlGroup = EnsureCanvasGroup(girlImage.gameObject);
        SetEntranceState();

        var loader = FindInScene<SplashLoader>();
        if (loader != null && loader.waitTime > 0f)
            waitTime = loader.waitTime;

        ApplyRequiredArtReadiness(background, logo, heroBoy, heroGirl, loadingTrack);
    }

    void Update()
    {
        if (!IsReady) return;

        elapsed += Time.unscaledDeltaTime;
        if (progressFill != null)
        {
            float target = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, waitTime));
            progressFill.fillAmount = Mathf.Max(progressFill.fillAmount, target);
            if (progressCap != null)
            {
                float shown = progressFill.fillAmount;
                progressCap.enabled = shown > 0.01f;
                progressCap.color = Color.Lerp(
                    new Color(0.05f, 0.80f, 1f, 1f),
                    new Color(1f, 0.12f, 0.76f, 1f), shown);
                var capRect = progressCap.rectTransform;
                capRect.anchorMin = capRect.anchorMax = new Vector2(shown, 0.5f);
                capRect.anchoredPosition = new Vector2(
                    Mathf.Lerp(19f, -19f, shown), 0f);
            }
        }

        float logoT = Mathf.Clamp01(elapsed / EntranceDuration);
        float girlT = Mathf.Clamp01((elapsed - HeroDelay) / EntranceDuration);

        ApplyEntrance(logoGroup, logoRect, logoT, 0.86f);
        ApplyEntrance(heroBoyGroup, heroBoyRect, logoT, 0.92f);
        ApplyEntrance(heroGirlGroup, heroGirlRect, girlT, 0.92f);

        IsSettled = logoT >= 1f && girlT >= 1f;
        if (IsSettled && logoRect != null)
        {
            float breathe = 1f + Mathf.Sin(Time.unscaledTime * 1.6f) * 0.01f;
            logoRect.localScale = new Vector3(breathe, breathe, 1f);
        }
    }

    void BuildProgress(Transform safeRoot, Sprite trackFrame)
    {
        var label = EnsureTmp(safeRoot, "SplashLoadingText", 38f);
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = ConvergingLight.NearWhite;
        label.characterSpacing = 1f;
        Place(label.rectTransform, new Vector2(0f, -650f), new Vector2(800f, 70f));
        SetLocalized(label, "splash_loading");
        var labelShadow = label.GetComponent<Shadow>();
        if (labelShadow == null) labelShadow = label.gameObject.AddComponent<Shadow>();
        labelShadow.effectColor = new Color(0.02f, 0.01f, 0.14f, 0.78f);
        labelShadow.effectDistance = new Vector2(3f, -4f);

        var track = EnsureImage(safeRoot, "SplashProgressTrack");
        ConfigureImage(track, trackFrame, false);
        track.type = Image.Type.Simple;
        track.color = Color.white;
        Place(track.rectTransform, new Vector2(0f, -742f), new Vector2(860f, 145f));

        var interior = EnsureImage(track.transform, "SplashProgressInterior");
        ConfigureImage(interior, RuntimeUI.RoundedRectSprite, false);
        interior.type = Image.Type.Sliced;
        interior.color = new Color(0.025f, 0.02f, 0.12f, 0.90f);
        Place(interior.rectTransform, new Vector2(0f, -2f), new Vector2(735f, 52f));

        progressFill = EnsureImage(interior.transform, "SplashProgressFill");
        ConfigureImage(progressFill, ConvergingLight.HorizontalGradient(
            new Color(0.05f, 0.80f, 1f, 1f),
            new Color(1f, 0.12f, 0.76f, 1f), 256), false);
        progressFill.color = Color.white;
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;
        progressFill.fillAmount = 0f;
        Stretch(progressFill.rectTransform);
        progressFill.rectTransform.offsetMin = new Vector2(5f, 5f);
        progressFill.rectTransform.offsetMax = new Vector2(-5f, -5f);

        progressCap = EnsureImage(interior.transform, "SplashProgressCap");
        ConfigureImage(progressCap, RuntimeUI.RoundedRectSprite, false);
        progressCap.type = Image.Type.Sliced;
        progressCap.enabled = false;
        var progressCapRect = progressCap.rectTransform;
        progressCapRect.anchorMin = progressCapRect.anchorMax = new Vector2(0f, 0.5f);
        progressCapRect.pivot = new Vector2(0.5f, 0.5f);
        progressCapRect.anchoredPosition = new Vector2(19f, 0f);
        progressCapRect.sizeDelta = new Vector2(38f, 38f);
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
        if (logoGroup != null) logoGroup.alpha = 0f;
        if (heroBoyGroup != null) heroBoyGroup.alpha = 0f;
        if (heroGirlGroup != null) heroGirlGroup.alpha = 0f;
        SetScale(logoRect, 0.86f);
        SetScale(heroBoyRect, 0.92f);
        SetScale(heroGirlRect, 0.92f);
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
        Sprite background, Sprite logo, Sprite heroBoy, Sprite heroGirl,
        Sprite loadingTrack)
    {
        IsReady = RequiredArtReady(background, logo, heroBoy, heroGirl, loadingTrack);
        if (!IsReady) IsSettled = false;
    }

    static bool RequiredArtReady(
        Sprite background, Sprite logo, Sprite heroBoy, Sprite heroGirl,
        Sprite loadingTrack)
    {
        return background != null &&
               logo != null &&
               heroBoy != null &&
               heroGirl != null &&
               loadingTrack != null;
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

    static TMP_Text EnsureTmp(Transform parent, string name, float size)
    {
        var rect = EnsureRect(parent, name);
        var tmp = rect.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.raycastTarget = false;
        RuntimeUI.ConfigureText(tmp, ResponsiveTextRole.Body, size);
        return tmp;
    }

    static void SetLocalized(TMP_Text text, string key)
    {
        var localized = text.GetComponent<LocalizedText>();
        if (localized == null)
        {
            RuntimeUI.Localize(text, key);
            localized = text.GetComponent<LocalizedText>();
        }
        if (localized != null) localized.key = key;
        text.text = L10n.Get(key);
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
