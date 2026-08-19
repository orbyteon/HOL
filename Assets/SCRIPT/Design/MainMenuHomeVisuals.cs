using System.Collections;
using TMPro;
using UnityEngine;
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
    public const string LogoName = "HomeLogo";
    public const string HeroBoyName = "HomeHeroBoy";
    public const string HeroGirlName = "HomeHeroGirl";
    public const string MascotSixName = "HomeMascotSix";
    public const string MascotSevenName = "HomeMascotSeven";
    public const string ChipName = "HomePlayerChip";
    public const string ChipTextName = "HomePlayerChipText";
    public const string TipName = "HomeTipCard";
    public const string SoloIconName = "HomeSoloIcon";
    public const string PrivateIconName = "HomePrivateIcon";
    public const string DailyIconName = "HomeDailyIcon";

    const string DecoStarsResource = "mainmenu/mainmenu_deco_stars";
    const string DecoLightningResource = "mainmenu/mainmenu_deco_lightning";
    const string DecoConfettiResource = "mainmenu/mainmenu_deco_confetti";
    const string DecoNumbersResource = "mainmenu/mainmenu_deco_numbers";
    const string LogoResource = "reference/hol_logo_exact";
    const string HeroBoyResource = "reference/char_boy_exact";
    const string HeroGirlResource = "reference/char_girl_exact";
    const string MascotSixResource = "reference/mascot_6_exact";
    const string MascotSevenResource = "reference/mascot_7_exact";
    const string GoldCtaResource = "mainmenu/mainmenu_cta_gold_9s";
    const string BlueCtaResource = "mainmenu/mainmenu_cta_blue_9s";
    const string MagentaCtaResource = "mainmenu/mainmenu_cta_magenta_9s";
    const string ChipFrameResource = "mainmenu/mainmenu_player_chip_frame_9s";
    const string TipFrameResource = "mainmenu/mainmenu_tip_frame_9s";
    const string GearResource = "mainmenu/mainmenu_gear_glossy";
    const string SoloIconResource = "mainmenu/mainmenu_icon_solo";
    const string PrivateIconResource = "mainmenu/mainmenu_icon_private_room";
    const string DailyIconResource = "mainmenu/mainmenu_icon_daily_hunt";
    const string StreakIconResource = "mainmenu/mainmenu_icon_streak";
    const string BulbIconResource = "mainmenu/mainmenu_icon_tip_bulb";

    static readonly Color Ink = new Color(0.09f, 0.06f, 0.22f, 1f);
    const float ReferenceWidth = 1080f;
    const float ReferenceHeight = 1920f;

    public static readonly string[] LoadedResources =
    {
        DecoStarsResource, DecoLightningResource,
        DecoConfettiResource, DecoNumbersResource, LogoResource,
        HeroBoyResource, HeroGirlResource, MascotSixResource, MascotSevenResource,
        GoldCtaResource, BlueCtaResource, MagentaCtaResource, ChipFrameResource,
        TipFrameResource, GearResource, SoloIconResource, PrivateIconResource,
        DailyIconResource, StreakIconResource, BulbIconResource
    };

    RectTransform visualRoot;
    TMP_Text chipText;
    bool laidOut;

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
        L10n.OnLanguageChanged += RefreshChip;
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= RefreshChip;
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
            RefreshChip();
    }

    void BuildHome()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[MainMenuHomeVisuals] Missing Canvas host.");
            return;
        }

        var logo = LoadRequired(LogoResource);
        var boy = LoadRequired(HeroBoyResource);
        var girl = LoadRequired(HeroGirlResource);
        var six = LoadRequired(MascotSixResource);
        var seven = LoadRequired(MascotSevenResource);
        var gold = LoadRequired(GoldCtaResource);
        var cyan = LoadRequired(BlueCtaResource);
        var magenta = LoadRequired(MagentaCtaResource);
        var chipFrame = LoadRequired(ChipFrameResource);
        var tipFrame = LoadRequired(TipFrameResource);
        var gear = LoadRequired(GearResource);

        IsReady = logo != null && boy != null &&
                  girl != null && six != null && seven != null &&
                  gold != null && cyan != null && magenta != null &&
                  chipFrame != null && tipFrame != null && gear != null;
        if (!IsReady) return;

        HideLegacyHome(canvas.transform);
        HideNamed("ButtonQuit");

        visualRoot = EnsureRect(canvas.transform, VisualRootName);
        Stretch(visualRoot);
        visualRoot.SetAsFirstSibling();

        var bg = EnsureImage(visualRoot, BackgroundName);
        Stretch(bg.rectTransform);
        ConfigureImage(bg, ConvergingLight.DepthGradientSprite, false);

        var safe = EnsureRect(visualRoot, SafeRootName);
        ConfigureSafeArea(safe, (RectTransform)canvas.transform);

        BuildNeonBackdrop(safe);
        BuildDeco(safe, "HomeDecoStars", LoadOptional(DecoStarsResource));
        BuildDeco(safe, "HomeDecoLightning", LoadOptional(DecoLightningResource));
        BuildDeco(safe, "HomeDecoConfetti", LoadOptional(DecoConfettiResource));
        BuildDeco(safe, "HomeDecoNumbers", LoadOptional(DecoNumbersResource));

        var logoImage = EnsureImage(safe, LogoName);
        ConfigureImage(logoImage, logo, true);
        Place(logoImage.rectTransform, new Vector2(0f, 600f), new Vector2(760f, 440f));

        var boyImage = EnsureImage(safe, HeroBoyName);
        ConfigureImage(boyImage, boy, true);
        Place(boyImage.rectTransform, new Vector2(-170f, 220f), new Vector2(340f, 400f));

        var girlImage = EnsureImage(safe, HeroGirlName);
        ConfigureImage(girlImage, girl, true);
        Place(girlImage.rectTransform, new Vector2(170f, 220f), new Vector2(340f, 400f));

        var sixImage = EnsureImage(safe, MascotSixName);
        ConfigureImage(sixImage, six, true);
        Place(sixImage.rectTransform, new Vector2(-410f, 90f), new Vector2(240f, 310f));

        var sevenImage = EnsureImage(safe, MascotSevenName);
        ConfigureImage(sevenImage, seven, true);
        Place(sevenImage.rectTransform, new Vector2(410f, 90f), new Vector2(230f, 310f));

        RestyleGear(safe, gear);
        BuildChip(safe, chipFrame);
        RestyleCta(safe, "ButtonPlay", gold, SoloIconResource, SoloIconName,
            "play_solo", new Vector2(0f, -265f), new Vector2(900f, 180f), true);
        RestyleCta(safe, "ButtonPvP", cyan, PrivateIconResource, PrivateIconName,
            "private_room", new Vector2(-225f, -475f), new Vector2(420f, 132f), false);
        RestyleCta(safe, "DailyHuntButton", magenta, DailyIconResource, DailyIconName,
            "daily_hunt", new Vector2(225f, -475f), new Vector2(420f, 132f), false);
        BuildTip(safe, tipFrame);
        RefreshChip();
    }

    void RestyleGear(Transform safe, Sprite gear)
    {
        var button = FindButton("Buttonsettings");
        if (button == null) return;
        Reparent(button.transform, safe);
        Place((RectTransform)button.transform, new Vector2(-455f, 840f), new Vector2(88f, 88f));
        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = gear;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = true;
        }
        HideChildLabels(button.transform);
    }

    void RestyleCta(
        Transform safe, string buttonName, Sprite frame, string iconResource,
        string iconName, string l10nKey, Vector2 position, Vector2 size, bool goldLabel)
    {
        var button = FindButton(buttonName);
        if (button == null) return;
        Reparent(button.transform, safe);
        Place((RectTransform)button.transform, position, size);
        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = frame;
            image.color = Color.white;
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.raycastTarget = true;
        }

        var icon = LoadOptional(iconResource);
        if (icon != null)
        {
            var iconImage = EnsureImage(button.transform, iconName);
            ConfigureImage(iconImage, icon, true);
            float iconX = Mathf.Max(-320f, -size.x * 0.5f + 58f);
            Place(iconImage.rectTransform, new Vector2(iconX, 0f),
                new Vector2(88f, 88f));
        }

        var label = EnsureButtonLabel(button);
        label.fontSize = goldLabel ? 52f : (size.x <= 500f ? 30f : 40f);
        label.fontStyle = FontStyles.Bold;
        label.color = goldLabel ? Ink : Ink;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = !goldLabel && size.x <= 500f;
        label.enableAutoSizing = !goldLabel && size.x <= 500f;
        if (label.enableAutoSizing)
        {
            label.fontSizeMin = 20f;
            label.fontSizeMax = 30f;
        }
        Place(label.rectTransform, new Vector2(goldLabel ? 36f : 30f, 0f),
            new Vector2(size.x - (goldLabel ? 180f : 125f), size.y - 24f));
        SetLocalized(label, l10nKey);
    }

    void BuildChip(Transform safe, Sprite frame)
    {
        var chip = EnsureImage(safe, ChipName);
        chip.sprite = frame;
        chip.color = Color.white;
        chip.type = Image.Type.Sliced;
        chip.raycastTarget = false;
        Place(chip.rectTransform, new Vector2(320f, 820f), new Vector2(400f, 92f));

        var streak = LoadOptional(StreakIconResource);
        if (streak != null)
        {
            var icon = EnsureImage(chip.transform, "HomeStreakIcon");
            ConfigureImage(icon, streak, true);
            Place(icon.rectTransform, new Vector2(-150f, 0f), new Vector2(56f, 56f));
        }

        chipText = EnsureTmp(chip.transform, ChipTextName, 28f);
        chipText.alignment = TextAlignmentOptions.MidlineLeft;
        chipText.color = ConvergingLight.NearWhite;
        chipText.raycastTarget = false;
        Place(chipText.rectTransform, new Vector2(28f, 0f), new Vector2(300f, 70f));
    }

    void BuildTip(Transform safe, Sprite frame)
    {
        var tip = EnsureImage(safe, TipName);
        tip.sprite = frame;
        tip.color = Color.white;
        tip.type = Image.Type.Sliced;
        tip.raycastTarget = false;
        Place(tip.rectTransform, new Vector2(0f, -730f), new Vector2(900f, 190f));

        var bulb = LoadOptional(BulbIconResource);
        if (bulb != null)
        {
            var icon = EnsureImage(tip.transform, "HomeTipBulb");
            ConfigureImage(icon, bulb, true);
            Place(icon.rectTransform, new Vector2(-380f, 40f), new Vector2(72f, 72f));
        }

        var title = EnsureTmp(tip.transform, "HomeTipTitle", 28f);
        title.fontStyle = FontStyles.Bold;
        title.color = ConvergingLight.Gold;
        title.alignment = TextAlignmentOptions.MidlineLeft;
        Place(title.rectTransform, new Vector2(40f, 50f), new Vector2(640f, 48f));
        SetLocalized(title, "hud_tip");

        var body = EnsureTmp(tip.transform, "HomeTipBody", 26f);
        body.color = ConvergingLight.NearWhite;
        body.alignment = TextAlignmentOptions.Left;
        Place(body.rectTransform, new Vector2(40f, -28f), new Vector2(760f, 110f));
        SetLocalized(body, "home_tip_body");
    }

    void BuildNeonBackdrop(Transform safe)
    {
        var backdrop = EnsureRect(safe, "HomeNeonBackdrop");
        Stretch(backdrop);
        backdrop.SetAsFirstSibling();

        var wash = backdrop.GetComponent<Image>();
        if (wash == null) wash = backdrop.gameObject.AddComponent<Image>();
        wash.sprite = ConvergingLight.VerticalGradient(
            ConvergingLight.WithAlpha(ConvergingLight.DepthTop, 0.98f),
            ConvergingLight.WithAlpha(
                new Color(0.08f, 0.04f, 0.30f, 1f), 0.98f));
        wash.type = Image.Type.Simple;
        wash.color = Color.white;
        wash.raycastTarget = false;

        AddNeonSeam(backdrop, "HomeNeonCyanSeam",
            new Vector2(-250f, 650f), new Vector2(720f, 5f), -38f,
            ConvergingLight.WithAlpha(ConvergingLight.Cyan, 0.42f));
        AddNeonSeam(backdrop, "HomeNeonMagentaSeam",
            new Vector2(310f, 190f), new Vector2(840f, 5f), 38f,
            ConvergingLight.WithAlpha(ConvergingLight.Magenta, 0.38f));
        AddNeonSeam(backdrop, "HomeNeonBlueSeam",
            new Vector2(-300f, -360f), new Vector2(760f, 4f), 34f,
            new Color(0.16f, 0.42f, 1f, 0.34f));
        AddNeonSeam(backdrop, "HomeNeonPinkSeam",
            new Vector2(250f, -720f), new Vector2(640f, 4f), -34f,
            new Color(1f, 0.10f, 0.68f, 0.28f));

        ConvergingLight.NumberField(backdrop, 18, 0.035f);
    }

    static Image AddNeonSeam(Transform parent, string name, Vector2 position,
        Vector2 size, float angle, Color color)
    {
        var seam = EnsureImage(parent, name);
        seam.sprite = RuntimeUI.RoundedRectSprite;
        seam.type = Image.Type.Sliced;
        seam.color = color;
        seam.raycastTarget = false;
        Place(seam.rectTransform, position, size);
        seam.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        return seam;
    }

    void BuildDeco(Transform safe, string name, Sprite sprite)
    {
        if (sprite == null) return;
        var image = EnsureImage(safe, name);
        ConfigureImage(image, sprite, false);
        Place(image.rectTransform, Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight));
    }

    void RefreshChip()
    {
        if (chipText == null) return;
        string player = PlayerPrefs.GetString("PlayerName", "");
        if (string.IsNullOrEmpty(player))
            player = L10n.Get("player_default");
        chipText.text = player + "  " + L10n.Get("stats_streak") + " " +
                        GameStats.CurrentStreak;
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
                     "ExactHOLLogo", "BoardHomeLogo"
                 })
        {
            var child = DeepFind(canvas, name);
            if (child != null) child.gameObject.SetActive(false);
        }
    }

    void ConfigureSafeArea(RectTransform safeRoot, RectTransform canvasRect)
    {
        Rect normalized = NormalizedSafeArea(Screen.safeArea, Screen.width, Screen.height);
        safeRoot.anchorMin = normalized.min;
        safeRoot.anchorMax = normalized.max;
        safeRoot.offsetMin = Vector2.zero;
        safeRoot.offsetMax = Vector2.zero;
        safeRoot.pivot = new Vector2(0.5f, 0.5f);

        Vector2 canvasSize = canvasRect.rect.size;
        if (canvasSize.x <= 0f || canvasSize.y <= 0f)
            canvasSize = new Vector2(ReferenceWidth, ReferenceHeight);
        float availableWidth = canvasSize.x * normalized.width;
        float availableHeight = canvasSize.y * normalized.height;
        float scale = Mathf.Min(1f,
            Mathf.Min(availableWidth / ReferenceWidth, availableHeight / ReferenceHeight));
        safeRoot.localScale = new Vector3(scale, scale, 1f);
    }

    static Rect NormalizedSafeArea(Rect safe, float width, float height)
    {
        if (width <= 0f || height <= 0f) return new Rect(0f, 0f, 1f, 1f);
        return new Rect(
            Mathf.Clamp01(safe.xMin / width),
            Mathf.Clamp01(safe.yMin / height),
            Mathf.Clamp01(safe.width / width),
            Mathf.Clamp01(safe.height / height));
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

    static TMP_Text EnsureButtonLabel(Button button)
    {
        var existing = button.GetComponentInChildren<TMP_Text>(true);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            return existing;
        }
        return EnsureTmp(button.transform, "Label", 40f);
    }

    static TMP_Text EnsureTmp(Transform parent, string name, float size)
    {
        var rect = EnsureRect(parent, name);
        var tmp = rect.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.raycastTarget = false;
        tmp.color = ConvergingLight.NearWhite;
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
