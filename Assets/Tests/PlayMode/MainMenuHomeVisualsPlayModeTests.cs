using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class MainMenuHomeVisualsPlayModeTests
{
    const BindingFlags StaticFlags =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [UnityTest]
    public IEnumerator HomeUsesApprovedSpritesOnRealControlsWithoutProceduralReplacement()
    {
        Screen.SetResolution(1080, 1920, false);
        InvokeInstaller("MainMenuHomeVisuals");
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        for (int i = 0; i < 28; i++) yield return null;

        Type ownerType = RuntimeType("MainMenuHomeVisuals");
        var owner = UnityEngine.Object.FindObjectOfType(ownerType) as Component;
        Assert.That(owner, Is.Not.Null);
        Assert.That(UnityEngine.Object.FindObjectsOfType(ownerType).Length,
            Is.EqualTo(1), "Home must have exactly one visual owner.");
        Assert.That((bool)ownerType.GetProperty("IsReady").GetValue(owner, null),
            Is.True);
        Assert.That((bool)ownerType.GetProperty("IsSettled").GetValue(owner, null),
            Is.True);

        var canvas = owner.GetComponent<Canvas>();
        Assert.That(canvas, Is.Not.Null);
        Transform visualRoot = Find(canvas.transform, "HomeVisualRoot");
        Assert.That(visualRoot, Is.Not.Null);

        // Current composition: production background + logo + characters +
        // mascots + real CTAs + chip/tip. No old global decoration layers.
        string[] required =
        {
            "HomeBackground", "HomeLogo", "HomeHeroBoy", "HomeHeroGirl",
            "HomeMascotSix", "HomeMascotSeven", "HomePlayerChip",
            "HomePlayerAvatar", "HomeStreakIcon", "HomeTipCard", "HomeTipIcon",
            "HomeSoloTitle", "HomePrivateTitle", "HomeDailyTitle"
        };
        foreach (string name in required)
            Assert.That(Find(visualRoot, name), Is.Not.Null, name);

        foreach (string retired in new[]
        {
            "ExactReferenceBackdrop", "AttachmentReferenceBackdrop",
            "BoardHomeLogo", "HomeNeonBackdrop", "HomeArenaGrid",
            "HomeDecoStars", "HomeDecoLightning", "HomeDecoConfetti",
            "HomeDecoNumbers"
        })
        {
            Transform found = Find(canvas.transform, retired);
            Assert.That(found == null || !found.gameObject.activeInHierarchy, Is.True,
                "Retired Home layer is visible: " + retired);
        }

        var background = Find(visualRoot, "HomeBackground").GetComponent<Image>();
        Assert.That(background.sprite, Is.SameAs(
            Resources.Load<Sprite>("phase2a/hol_neon_reference_bg_r3")));
        Assert.That(background.color.a, Is.EqualTo(1f).Within(0.001f));

        var play = Find(canvas.transform, "ButtonPlay").GetComponent<Button>();
        var pvp = Find(canvas.transform, "ButtonPvP").GetComponent<Button>();
        var hunt = Find(canvas.transform, "DailyHuntButton").GetComponent<Button>();
        var settings = Find(canvas.transform, "Buttonsettings").GetComponent<Button>();
        Assert.That(play, Is.Not.Null);
        Assert.That(pvp, Is.Not.Null);
        Assert.That(hunt, Is.Not.Null);
        Assert.That(settings, Is.Not.Null);

        AssertProductionButton(play, "phase2a/hol_cta_gold_r2_9s");
        AssertProductionButton(pvp, "phase2a/hol_cta_blue_r2_9s");
        AssertProductionButton(hunt, "phase2a/hol_cta_magenta_r2_9s");
        Assert.That(hunt.GetComponent<Image>().sprite,
            Is.Not.SameAs(play.GetComponent<Image>().sprite),
            "PLAY must remain the sole gold primary CTA.");

        var playTitle = Find(play.transform, "HomeSoloTitle").GetComponent<TMP_Text>();
        var privateTitle = Find(pvp.transform, "HomePrivateTitle").GetComponent<TMP_Text>();
        var dailyTitle = Find(hunt.transform, "HomeDailyTitle").GetComponent<TMP_Text>();
        Assert.That(playTitle.color.r, Is.LessThan(0.3f),
            "Gold PLAY uses dark ink for contrast.");
        Assert.That(privateTitle.color.r, Is.GreaterThan(0.8f),
            "Blue Private Room must use near-white copy.");
        Assert.That(dailyTitle.color.r, Is.GreaterThan(0.8f),
            "Magenta Daily Hunt must use near-white copy.");

        var gearImage = settings.GetComponent<Image>();
        Assert.That(gearImage, Is.Not.Null);
        Assert.That(gearImage.sprite, Is.SameAs(
            Resources.Load<Sprite>("phase2a/hol_settings_gear_r2")));
        Assert.That(gearImage.color.a, Is.EqualTo(1f).Within(0.001f));
        Assert.That(gearImage.preserveAspect, Is.True);

        var privateIcon = Find(pvp.transform, "HomePrivateIcon")
            .GetComponent<Image>();
        var dailyIcon = Find(hunt.transform, "HomeDailyIcon")
            .GetComponent<Image>();
        Assert.That(privateIcon.sprite, Is.SameAs(
            Resources.Load<Sprite>("phase2a/hol_mode_private_r2")));
        Assert.That(dailyIcon.sprite, Is.SameAs(
            Resources.Load<Sprite>("phase2a/hol_mode_daily_r2")));
        Assert.That(privateIcon.color.a, Is.EqualTo(1f).Within(0.001f));
        Assert.That(dailyIcon.color.a, Is.EqualTo(1f).Within(0.001f));

        // The retired procedural Home classes must be physically absent, not
        // merely disabled behind production art.
        foreach (string retiredType in new[]
        {
            "MainMenuCtaLuminousSurface",
            "MainMenuChamferedCtaGraphic",
            "MainMenuReferenceIconGraphic",
            "MainMenuPlayerChipGraphic",
            "MainMenuNeonArenaGraphic"
        })
        {
            Assert.That(Type.GetType(retiredType + ", Assembly-CSharp"), Is.Null,
                "Retired procedural Home type returned: " + retiredType);
        }

        foreach (var image in visualRoot.GetComponentsInChildren<Image>(true))
        {
            if (image.sprite == null) continue;
            Assert.That(image.color.a, Is.GreaterThanOrEqualTo(0.99f),
                image.name + " hides its approved sprite instead of rendering it.");
        }

        foreach (var button in canvas.GetComponentsInChildren<Button>(true))
            Assert.That(button.name.StartsWith("Home", StringComparison.Ordinal), Is.False,
                "Home owner must reuse real controls, not invent a Button: " + button.name);

        Assert.That(PersistentMethods(play), Does.Contain("OnPlayPressed"));
        Assert.That(PersistentMethods(settings), Does.Contain("OpenSettings"));

        // 1080×1920 reference hierarchy/density.
        ownerType.GetMethod("ApplyResponsiveLayoutForWidth", InstanceFlags)
            .Invoke(owner, new object[] { 1080, true });
        var playRect = (RectTransform)play.transform;
        var pvpRect = (RectTransform)pvp.transform;
        var huntRect = (RectTransform)hunt.transform;
        var logoRect = (RectTransform)Find(visualRoot, "HomeLogo");
        var tipRect = (RectTransform)Find(visualRoot, "HomeTipCard");
        Assert.That(playRect.sizeDelta.x, Is.GreaterThanOrEqualTo(900f));
        Assert.That(playRect.sizeDelta.y, Is.GreaterThanOrEqualTo(230f));
        Assert.That(pvpRect.sizeDelta.x, Is.EqualTo(huntRect.sizeDelta.x));
        Assert.That(pvpRect.anchoredPosition.x, Is.LessThan(0f));
        Assert.That(huntRect.anchoredPosition.x, Is.GreaterThan(0f));
        Assert.That(pvpRect.anchoredPosition.y,
            Is.EqualTo(huntRect.anchoredPosition.y).Within(0.01f));
        Assert.That(logoRect.sizeDelta.y, Is.GreaterThanOrEqualTo(500f));
        Assert.That(tipRect.sizeDelta.y, Is.GreaterThanOrEqualTo(185f));
        Assert.That(tipRect.anchoredPosition.y, Is.LessThanOrEqualTo(-700f));

        // Explicit production viewport matrix: reference Android, tall Android,
        // representative iPhone portrait and the established 720×1280 fallback.
        AssertViewportLayout(ownerType, owner, visualRoot, play, pvp, hunt,
            1080, 1920, 0, "Android reference EN");
        AssertViewportLayout(ownerType, owner, visualRoot, play, pvp, hunt,
            1080, 2400, 1, "Tall Android EL");
        AssertViewportLayout(ownerType, owner, visualRoot, play, pvp, hunt,
            1179, 2556, 0, "iPhone portrait EN");
        AssertViewportLayout(ownerType, owner, visualRoot, play, pvp, hunt,
            720, 1280, 1, "Android fallback EL");
        SetLanguage(0);

        var displayFont = Resources.Load<TMP_FontAsset>(
            "phase2a/fonts/HOL Menu Display SDF");
        var bodyFont = Resources.Load<TMP_FontAsset>(
            "phase2a/fonts/HOL Menu Body SDF");
        Assert.That(displayFont, Is.Not.Null);
        Assert.That(bodyFont, Is.Not.Null);
        Assert.That(Find(play.transform, "HomeSoloTitle").GetComponent<TMP_Text>().font,
            Is.SameAs(displayFont));
        Assert.That(Find(pvp.transform, "HomePrivateTitle").GetComponent<TMP_Text>().font,
            Is.SameAs(displayFont));
        Assert.That(Find(hunt.transform, "HomeDailyTitle").GetComponent<TMP_Text>().font,
            Is.SameAs(displayFont));

        PlayerPrefs.SetInt("StatStreak", 7);
        PlayerPrefs.SetString("PlayerName", "Andreas");
        PlayerPrefs.Save();
        ownerType.GetMethod("RefreshChip", InstanceFlags).Invoke(owner, null);
        string chip = Find(visualRoot, "HomePlayerChipText")
            .GetComponent<TMP_Text>().text;
        Assert.That(chip, Does.Contain("Andreas"));
        Assert.That(chip, Does.Contain("7"));

        AssertLocalizedHomeCopy(visualRoot, 0,
            "PLAY SOLO VS AI", "PRIVATE ROOM", "DAILY HUNT",
            "TIP:", "Every guess narrows the range!");
        AssertLocalizedHomeCopy(visualRoot, 1,
            "ΠΑΙΞΕ SOLO ΜΕ AI", "ΙΔΙΩΤΙΚΟ ΔΩΜΑΤΙΟ", "ΚΥΝΗΓΙ ΗΜΕΡΑΣ",
            "ΣΥΜΒΟΥΛΗ:", "Κάθε μαντεψιά μικραίνει το εύρος!");
        SetLanguage(0);

        string[] paths = (string[])ownerType.GetField("LoadedResources", StaticFlags)
            .GetValue(null);
        Assert.That(paths, Does.Contain("phase2a/hol_cta_magenta_r2_9s"));
        foreach (string path in paths)
        {
            Assert.That(path.Contains("stairs_clouds"), Is.False, path);
            Assert.That(path.StartsWith("design/", StringComparison.Ordinal), Is.False,
                "Home must not load a generic legacy theme resource: " + path);
        }

        // Functional smoke: the real PvP entry must still drive the real
        // controller after being reparented/styled.
        pvp.onClick.Invoke();
        yield return null;
        var controller = UnityEngine.Object.FindObjectOfType(
            RuntimeType("PvpGameController")) as Component;
        Assert.That(controller, Is.Not.Null);
        var pvpMenu = controller.GetType().GetField("pvpMenuPanel")
            .GetValue(controller) as GameObject;
        Assert.That(pvpMenu, Is.Not.Null);
        Assert.That(pvpMenu.activeSelf, Is.True);
    }

    static void AssertViewportLayout(
        Type ownerType,
        Component owner,
        Transform visualRoot,
        Button play,
        Button pvp,
        Button hunt,
        int width,
        int height,
        int language,
        string viewport)
    {
        SetLanguage(language);
        MethodInfo layout = ownerType.GetMethod(
            "ApplyResponsiveLayoutForViewport", InstanceFlags);
        Assert.That(layout, Is.Not.Null);
        layout.Invoke(owner, new object[] { width, height, true });
        Canvas.ForceUpdateCanvases();

        var playRect = (RectTransform)play.transform;
        var pvpRect = (RectTransform)pvp.transform;
        var huntRect = (RectTransform)hunt.transform;
        var tipRect = (RectTransform)Find(visualRoot, "HomeTipCard");

        AssertNoOverlap(playRect, pvpRect, viewport + " PLAY/Private");
        AssertNoOverlap(playRect, huntRect, viewport + " PLAY/Daily");
        AssertNoOverlap(pvpRect, tipRect, viewport + " Private/Tip");
        AssertNoOverlap(huntRect, tipRect, viewport + " Daily/Tip");

        AssertTextFits(play.transform, "HomeSoloTitle", 54f, viewport);
        AssertTextFits(pvp.transform, "HomePrivateTitle", 32f, viewport);
        AssertTextFits(hunt.transform, "HomeDailyTitle", 32f, viewport);
        AssertTextFits(visualRoot, "HomeTipTitle", 30f, viewport);
        AssertTextFits(visualRoot, "HomeTipBody", 22f, viewport);
    }

    static void AssertNoOverlap(RectTransform a, RectTransform b, string context)
    {
        Assert.That(a, Is.Not.Null, context);
        Assert.That(b, Is.Not.Null, context);
        Assert.That(AnchoredBounds(a).Overlaps(AnchoredBounds(b)), Is.False, context);
    }

    static Rect AnchoredBounds(RectTransform rect)
    {
        Vector2 size = rect.rect.size;
        Vector2 min = rect.anchoredPosition - Vector2.Scale(size, rect.pivot);
        return new Rect(min, size);
    }

    static void AssertTextFits(
        Transform root,
        string name,
        float minimumFontSize,
        string viewport)
    {
        Transform target = Find(root, name);
        Assert.That(target, Is.Not.Null, viewport + " " + name);
        TMP_Text text = target.GetComponent<TMP_Text>();
        Assert.That(text, Is.Not.Null, viewport + " " + name);
        text.ForceMeshUpdate();
        Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(minimumFontSize),
            viewport + " shrank " + name + " below the readability contract.");
        Assert.That(text.isTextOverflowing, Is.False,
            viewport + " overflows " + name + ": " + text.text);
    }

    static void AssertProductionButton(Button button, string resource)
    {
        var image = button.GetComponent<Image>();
        var sprite = Resources.Load<Sprite>(resource);
        Assert.That(image, Is.Not.Null, button.name);
        Assert.That(sprite, Is.Not.Null, resource);
        Assert.That(image.sprite, Is.SameAs(sprite), button.name);
        Assert.That(image.type, Is.EqualTo(Image.Type.Sliced), button.name);
        Assert.That(image.color.a, Is.EqualTo(1f).Within(0.001f), button.name);
        Assert.That(button.targetGraphic, Is.SameAs(image));
        Assert.That(button.colors.pressedColor.r, Is.LessThan(0.9f), button.name);
        Assert.That(button.colors.fadeDuration, Is.LessThanOrEqualTo(0.08f));
    }

    static void AssertLocalizedHomeCopy(
        Transform root, int language,
        string solo, string room, string daily, string tipTitle, string tipBody)
    {
        SetLanguage(language);
        Assert.That(Find(root, "HomeSoloTitle").GetComponent<TMP_Text>().text,
            Is.EqualTo(solo));
        Assert.That(Find(root, "HomePrivateTitle").GetComponent<TMP_Text>().text,
            Is.EqualTo(room));
        Assert.That(Find(root, "HomeDailyTitle").GetComponent<TMP_Text>().text,
            Is.EqualTo(daily));
        Assert.That(Find(root, "HomeTipTitle").GetComponent<TMP_Text>().text,
            Is.EqualTo(tipTitle));
        Assert.That(Find(root, "HomeTipBody").GetComponent<TMP_Text>().text,
            Is.EqualTo(tipBody));
    }

    static void SetLanguage(int value)
    {
        Type l10n = RuntimeType("L10n");
        Type language = l10n.GetNestedType("Language");
        object enumValue = Enum.ToObject(language, value);
        l10n.GetMethod("SetLanguage", BindingFlags.Static | BindingFlags.Public)
            .Invoke(null, new[] { enumValue });
    }

    static string[] PersistentMethods(Button button)
    {
        int count = button.onClick.GetPersistentEventCount();
        string[] names = new string[count];
        for (int i = 0; i < count; i++)
            names[i] = button.onClick.GetPersistentMethodName(i);
        return names;
    }

    static void InvokeInstaller(string typeName)
    {
        Type type = RuntimeType(typeName);
        MethodInfo method = type.GetMethod("Install",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(null, null);
    }

    static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = Find(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    static Type RuntimeType(string name)
    {
        Type type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "Missing runtime component: " + name);
        return type;
    }
}
