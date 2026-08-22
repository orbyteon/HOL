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
    public IEnumerator HomeOwnerMapsExistingControlsWithoutInventingFeatures()
    {
        Screen.SetResolution(1080, 1920, false);
        InvokeInstaller("MainMenuHomeVisuals");
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        for (int i = 0; i < 16; i++)
            yield return null;
        yield return new WaitForSecondsRealtime(0.35f);

        var ownerType = RuntimeType("MainMenuHomeVisuals");
        var owner = Object.FindObjectOfType(ownerType) as Component;
        Assert.That(owner, Is.Not.Null);
        Assert.That(Object.FindObjectsOfType(ownerType).Length, Is.EqualTo(1));
        Assert.That((bool)ownerType.GetProperty("IsReady").GetValue(owner, null), Is.True);
        Assert.That((bool)ownerType.GetProperty("IsSettled").GetValue(owner, null), Is.True);

        var canvas = owner.GetComponent<Canvas>();
        Assert.That(canvas, Is.Not.Null);
        Assert.That(Find(canvas.transform, "HomeVisualRoot"), Is.Not.Null);
        foreach (var decoration in new[]
                 {
                     "HomeDecoStars", "HomeDecoLightning",
                     "HomeDecoConfetti", "HomeDecoNumbers"
                 })
        {
            Assert.That(Find(canvas.transform, decoration), Is.Null,
                decoration + " full-screen composite must not compete with the hero lineup.");
        }
        Assert.That(Find(canvas.transform, "HomeArenaGrid"), Is.Null,
            "The Revision 3 background already owns the exact perspective rays and grid.");
        Assert.That(Find(canvas.transform, "HomeLogo"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "HomeHeroBoy"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "HomeHeroGirl"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "HomeMascotSix"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "HomeMascotThree"), Is.Null);
        Assert.That(Find(canvas.transform, "HomeMascotSeven"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "HomePlayerChip"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "HomeTipCard"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "HomeTipIcon"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "HomeTipMascotSeven"), Is.Null,
            "The approved compact Tip panel must not repeat the hero mascot.");
        Assert.That(Find(canvas.transform, "BoardHomeLogo"), Is.Null);
        Assert.That(Find(canvas.transform, "BoardStorePanel"), Is.Null);
        Assert.That(Find(canvas.transform, "BoardProfilePanel"), Is.Null);
        var legacyStats = Find(canvas.transform, "StatsLabel");
        Assert.That(legacyStats, Is.Not.Null);
        Assert.That(legacyStats.gameObject.activeSelf, Is.False,
            "The legacy stats summary must not compete with the player chip.");

        Assert.That(Find(canvas.transform, "HomeNeonBackdrop"), Is.Null,
            "Revision 2 must not place a large translucent rectangle over the arena.");

        var background = Find(canvas.transform, "HomeBackground")
            .GetComponent<Image>();
        Assert.That(background.sprite, Is.SameAs(
            Resources.Load<Sprite>("phase2a/hol_neon_reference_bg_r3")));

        var six = Find(canvas.transform, "HomeMascotSix") as RectTransform;
        var boy = Find(canvas.transform, "HomeHeroBoy") as RectTransform;
        var girl = Find(canvas.transform, "HomeHeroGirl") as RectTransform;
        var seven = Find(canvas.transform, "HomeMascotSeven") as RectTransform;
        Assert.That(six.anchoredPosition.x, Is.LessThan(boy.anchoredPosition.x));
        Assert.That(boy.anchoredPosition.x, Is.LessThan(girl.anchoredPosition.x));
        Assert.That(girl.anchoredPosition.x, Is.LessThan(seven.anchoredPosition.x));

        foreach (var name in new[]
                 {
                     "HomeLogo", "HomeMascotSix", "HomeHeroBoy",
                     "HomeHeroGirl", "HomeMascotSeven"
                 })
        {
            var image = Find(canvas.transform, name).GetComponent<Image>();
            Assert.That(image.preserveAspect, Is.True, name);
            Assert.That(image.raycastTarget, Is.False, name);
        }

        Assert.That(Object.FindObjectsOfType<Button>().Length,
            Is.GreaterThanOrEqualTo(4));
        foreach (var button in canvas.GetComponentsInChildren<Button>(true))
            Assert.That(button.name.StartsWith("Home"), Is.False,
                "Home owner must not invent a Button: " + button.name);

        var play = Find(canvas.transform, "ButtonPlay").GetComponent<Button>();
        var pvp = Find(canvas.transform, "ButtonPvP").GetComponent<Button>();
        var hunt = Find(canvas.transform, "DailyHuntButton").GetComponent<Button>();
        var settings = Find(canvas.transform, "Buttonsettings").GetComponent<Button>();
        ownerType.GetMethod("ApplyResponsiveLayoutForWidth", InstanceFlags)
            .Invoke(owner, new object[] { 1080, true });
        Assert.That(settings.GetComponent<Image>().sprite, Is.SameAs(
            Resources.Load<Sprite>("phase2a/hol_settings_gear_r2")));
        var referenceIconType = RuntimeType("MainMenuReferenceIconGraphic");
        var gearSymbol = Find(settings.transform, "HomeSettingsGearSymbol");
        Assert.That(gearSymbol, Is.Not.Null);
        Assert.That(gearSymbol.GetComponent(referenceIconType), Is.Not.Null,
            "Settings must use the unboxed cyan reference gear.");
        var playRect = play.transform as RectTransform;
        var pvpRect = pvp.transform as RectTransform;
        var huntRect = hunt.transform as RectTransform;
        var logoRect = Find(canvas.transform, "HomeLogo") as RectTransform;
        var tipRect = Find(canvas.transform, "HomeTipCard") as RectTransform;
        Assert.That(playRect.sizeDelta.x, Is.GreaterThan(pvpRect.sizeDelta.x));
        Assert.That(playRect.sizeDelta.x, Is.GreaterThan(huntRect.sizeDelta.x));
        Assert.That(playRect.sizeDelta.y, Is.GreaterThan(pvpRect.sizeDelta.y));
        Assert.That(playRect.sizeDelta.x, Is.GreaterThanOrEqualTo(900f));
        Assert.That(playRect.sizeDelta.y, Is.GreaterThanOrEqualTo(230f));
        Assert.That(pvpRect.sizeDelta.y, Is.GreaterThanOrEqualTo(200f));
        Assert.That(huntRect.sizeDelta.y, Is.GreaterThanOrEqualTo(200f));
        Assert.That(logoRect.sizeDelta.y, Is.GreaterThanOrEqualTo(500f));
        Assert.That(six.sizeDelta.y, Is.GreaterThanOrEqualTo(430f));
        Assert.That(boy.sizeDelta.y, Is.GreaterThanOrEqualTo(400f));
        Assert.That(girl.sizeDelta.y, Is.GreaterThanOrEqualTo(400f));
        Assert.That(seven.sizeDelta.y, Is.GreaterThanOrEqualTo(430f));
        Assert.That(tipRect.sizeDelta.y, Is.GreaterThanOrEqualTo(185f));
        Assert.That(tipRect.anchoredPosition.y, Is.LessThanOrEqualTo(-700f));
        Assert.That(pvpRect.sizeDelta.x, Is.EqualTo(huntRect.sizeDelta.x));
        Assert.That(pvpRect.anchoredPosition.x, Is.LessThan(0f));
        Assert.That(huntRect.anchoredPosition.x, Is.GreaterThan(0f));
        Assert.That(playRect.anchoredPosition.y, Is.GreaterThan(
            pvpRect.anchoredPosition.y));
        Assert.That(pvpRect.anchoredPosition.y,
            Is.EqualTo(huntRect.anchoredPosition.y).Within(0.01f));
        Assert.That(huntRect.anchoredPosition.y, Is.GreaterThan(-900f));
        Assert.That(PersistentMethods(play), Does.Contain("OnPlayPressed"));
        Assert.That(PersistentMethods(settings), Does.Contain("OpenSettings"));
        Assert.That(play.GetComponent<Image>().sprite.name, Does.Contain("gold"));
        Assert.That(pvp.GetComponent<Image>().sprite.name, Does.Contain("blue"));
        Assert.That(hunt.GetComponent<Image>().sprite.name, Does.Contain("gold"));
        var luminousType = RuntimeType("MainMenuCtaLuminousSurface");
        var chamferedType = RuntimeType("MainMenuChamferedCtaGraphic");
        foreach (var button in new[] { play, pvp, hunt })
        {
            Assert.That(button.GetComponent(luminousType), Is.Not.Null,
                button.name + " must use the live luminous CTA material.");
            var surface = Find(button.transform, "HomeCtaInnerLight");
            Assert.That(surface, Is.Not.Null);
            Assert.That(surface.GetComponent(chamferedType), Is.Not.Null,
                button.name + " must render the approved chamfered CTA silhouette.");
            Canvas.ForceUpdateCanvases();
            var surfaceGraphic = surface.GetComponent(chamferedType) as Graphic;
            Assert.That(surfaceGraphic.canvasRenderer.GetMesh().vertexCount,
                Is.GreaterThan(50),
                button.name + " chamfered material must contribute visible mesh geometry.");
            Assert.That(Find(button.transform, "HomeCtaTopGloss"), Is.Null,
                "The native frame owns its curved gloss; no flat overlay may cover it.");
            Assert.That(Find(button.transform, "HomeCtaMovingSheen"), Is.Null,
                "A flat sheen strip must not cross live text or native artwork.");
            Assert.That(button.colors.pressedColor.r, Is.LessThan(0.9f),
                button.name + " must have obvious tactile press feedback.");
            Assert.That(button.colors.fadeDuration, Is.LessThanOrEqualTo(0.08f));
        }
        Assert.That(Find(play.transform, "HomeSoloIcon"), Is.Null,
            "The approved central gold CTA has no competing left-side icon.");
        var privateIcon = Find(pvp.transform, "HomePrivateIcon");
        var dailyIcon = Find(hunt.transform, "HomeDailyIcon");
        Assert.That(privateIcon, Is.Not.Null);
        Assert.That(dailyIcon, Is.Not.Null);
        Assert.That(privateIcon.GetComponent(referenceIconType), Is.Not.Null);
        Assert.That(dailyIcon.GetComponent(referenceIconType), Is.Not.Null);
        Assert.That(privateIcon.GetComponent<Image>(), Is.Null,
            "The reference people symbol must not reuse the padded sticker PNG.");
        Assert.That(dailyIcon.GetComponent<Image>(), Is.Null,
            "The reference lightning symbol must not reuse the padded sticker PNG.");
        Assert.That(((RectTransform)privateIcon).sizeDelta.x, Is.GreaterThanOrEqualTo(120f));
        Assert.That(((RectTransform)dailyIcon).sizeDelta.x, Is.GreaterThanOrEqualTo(120f));
        Assert.That(Find(play.transform, "HomeActionChevron"), Is.Null);
        Assert.That(Find(pvp.transform, "HomeActionChevron"), Is.Null);
        Assert.That(Find(hunt.transform, "HomeActionChevron"), Is.Null,
            "The authoritative reference buttons do not contain chevrons.");
        Canvas.ForceUpdateCanvases();
        Assert.That((privateIcon.GetComponent(referenceIconType) as Graphic)
            .canvasRenderer.GetMesh().vertexCount, Is.GreaterThan(40));
        Assert.That((dailyIcon.GetComponent(referenceIconType) as Graphic)
            .canvasRenderer.GetMesh().vertexCount, Is.GreaterThan(7));

        var chipSurface = Find(canvas.transform, "HomePlayerChipSurface");
        var avatarSymbol = Find(canvas.transform, "HomePlayerAvatarSymbol");
        Assert.That(chipSurface, Is.Not.Null);
        Assert.That(chipSurface.GetComponent(RuntimeType("MainMenuPlayerChipGraphic")),
            Is.Not.Null);
        Assert.That(avatarSymbol, Is.Not.Null);
        Assert.That(avatarSymbol.GetComponent(referenceIconType), Is.Not.Null);

        var displayFont = Resources.Load<TMP_FontAsset>(
            "phase2a/fonts/HOL Menu Display SDF");
        var bodyFont = Resources.Load<TMP_FontAsset>(
            "phase2a/fonts/HOL Menu Body SDF");
        Assert.That(displayFont, Is.Not.Null);
        Assert.That(bodyFont, Is.Not.Null);
        Assert.That(displayFont.atlasPopulationMode,
            Is.EqualTo(AtlasPopulationMode.Static));
        Assert.That(bodyFont.atlasPopulationMode,
            Is.EqualTo(AtlasPopulationMode.Static));
        Assert.That(Find(play.transform, "HomeSoloTitle").GetComponent<TMP_Text>().font,
            Is.SameAs(displayFont));
        Assert.That(Find(pvp.transform, "HomePrivateTitle").GetComponent<TMP_Text>().font,
            Is.SameAs(displayFont));
        Assert.That(Find(hunt.transform, "HomeDailyTitle").GetComponent<TMP_Text>().font,
            Is.SameAs(displayFont));
        Assert.That(Find(play.transform, "HomeSoloSubtitle"), Is.Null);
        Assert.That(Find(pvp.transform, "HomePrivateSubtitle"), Is.Null);
        Assert.That(Find(hunt.transform, "HomeDailySubtitle"), Is.Null,
            "The authoritative CTA composition uses one strong live title per action.");
        Assert.That(Find(pvp.transform, "HomePrivateTitle").GetComponent<TMP_Text>()
            .fontSizeMax, Is.GreaterThanOrEqualTo(42f));

        PlayerPrefs.SetInt("StatStreak", 7);
        PlayerPrefs.SetString("PlayerName", "Andreas");
        PlayerPrefs.Save();
        ownerType.GetMethod("RefreshChip", InstanceFlags).Invoke(owner, null);
        var tmpTextType = System.Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
        Assert.That(tmpTextType, Is.Not.Null);
        Component chip = null;
        foreach (var component in Find(canvas.transform, "HomePlayerChipText")
                     .GetComponentsInChildren(tmpTextType, true))
        {
            chip = component;
            break;
        }
        Assert.That(chip, Is.Not.Null);
        string chipCopy = (string)tmpTextType.GetProperty("text").GetValue(chip, null);
        Assert.That(chipCopy, Does.Contain("7"));
        Assert.That(chipCopy, Does.Not.Contain("2450"));
        Assert.That(chipCopy, Does.Not.Contain("2,450"));
        Assert.That(Find(canvas.transform, "HomePlayerChip").GetComponent<Image>().raycastTarget,
            Is.False);

        AssertLocalizedHomeCopy(canvas.transform, 0,
            "PLAY SOLO VS AI", "PRIVATE ROOM", "DAILY HUNT",
            "TIP:", "Every guess narrows the range!");
        AssertLocalizedHomeCopy(canvas.transform, 1,
            "ΠΑΙΞΕ SOLO ΜΕ AI", "ΙΔΙΩΤΙΚΟ ΔΩΜΑΤΙΟ", "ΚΥΝΗΓΙ ΗΜΕΡΑΣ",
            "ΣΥΜΒΟΥΛΗ:", "Κάθε μαντεψιά μικραίνει το εύρος!");
        Assert.That(pvpRect.anchoredPosition.x, Is.LessThan(0f),
            "Approved Greek Home keeps the supporting actions paired.");
        Assert.That(huntRect.anchoredPosition.x, Is.GreaterThan(0f));
        Assert.That(pvpRect.anchoredPosition.y,
            Is.EqualTo(huntRect.anchoredPosition.y).Within(0.01f));
        SetLanguage(0);

        yield return new WaitForSecondsRealtime(0.35f);
        var tip = Find(canvas.transform, "HomeTipCard").GetComponent<Image>();
        Assert.That(tip.sprite.name, Does.Contain("tip_frame"));
        Assert.That(tip.GetComponent<Outline>(), Is.Null);

        var paths = (string[])ownerType.GetField("LoadedResources", StaticFlags)
            .GetValue(null);
        foreach (var path in paths)
        {
            Assert.That(path.StartsWith("splash/"), Is.False, path);
            Assert.That(path.Contains("stairs_clouds"), Is.False, path);
        }

        var menu = Object.FindObjectOfType(RuntimeType("MenuManager")) as Component;
        play.onClick.Invoke();
        yield return null;
        var panelPlay = menu.GetType().GetField("panelPlay").GetValue(menu) as GameObject;
        Assert.That(panelPlay.activeSelf, Is.True);
        menu.SendMessage("BackToMenu", SendMessageOptions.RequireReceiver);
        yield return null;

        pvp.onClick.Invoke();
        yield return null;
        var pvpController = Object.FindObjectOfType(
            RuntimeType("PvpGameController")) as Component;
        Assert.That(pvpController, Is.Not.Null);
        var pvpMenu = pvpController.GetType().GetField("pvpMenuPanel")
            .GetValue(pvpController) as GameObject;
        Assert.That(pvpMenu, Is.Not.Null);
        Assert.That(pvpMenu.activeSelf, Is.True);
        pvpController.SendMessage("ClosePvpMenu", SendMessageOptions.RequireReceiver);
        yield return null;

        hunt.onClick.Invoke();
        yield return null;
        var dailyPanel = Find(canvas.transform, "DailyHuntPanel");
        Assert.That(dailyPanel, Is.Not.Null);
        Assert.That(dailyPanel.gameObject.activeSelf, Is.True);
        dailyPanel.SendMessage("Close", SendMessageOptions.RequireReceiver);
        yield return null;

        settings.onClick.Invoke();
        yield return null;
        var settingsPanel = menu.GetType().GetField("settingsPanel").GetValue(menu) as GameObject;
        Assert.That(settingsPanel.activeSelf, Is.True);
        menu.SendMessage("BackToMenu", SendMessageOptions.RequireReceiver);

        Assert.That(pvp, Is.Not.Null);
        Assert.That(hunt, Is.Not.Null);

        ownerType.GetMethod("ApplyResponsiveLayoutForWidth", InstanceFlags)
            .Invoke(owner, new object[] { 720, true });
        yield return null;
        Assert.That(pvpRect.anchoredPosition.x, Is.LessThan(0f));
        Assert.That(huntRect.anchoredPosition.x, Is.GreaterThan(0f));
        Assert.That(pvpRect.anchoredPosition.y,
            Is.EqualTo(huntRect.anchoredPosition.y).Within(0.01f));
        Assert.That(Find(canvas.transform, "HomeTipCard").GetComponent<RectTransform>()
            .anchoredPosition.y, Is.LessThan(-650f));

        ownerType.GetMethod("ApplyResponsiveLayoutForViewport", InstanceFlags)
            .Invoke(owner, new object[] { 1080, 2400, true });
        Assert.That(settings.GetComponent<RectTransform>().anchoredPosition.y,
            Is.GreaterThanOrEqualTo(1000f));
        Assert.That(Find(canvas.transform, "HomeTipCard").GetComponent<RectTransform>()
            .anchoredPosition.y, Is.LessThanOrEqualTo(-800f));
        ownerType.GetMethod("ApplyResponsiveLayoutForViewport", InstanceFlags)
            .Invoke(owner, new object[] { 1080, 1920, true });

        var ownerBehaviour = (MonoBehaviour)owner;
        ownerBehaviour.enabled = false;
        ownerBehaviour.enabled = true;
        yield return null;
        Assert.That(CountNamed(canvas.transform, "HomeVisualRoot"), Is.EqualTo(1));
    }

    [Test]
    public void MissingRequiredHomeArtFailsReadiness()
    {
        var ownerType = RuntimeType("MainMenuHomeVisuals");
        var paths = (string[])ownerType.GetField("LoadedResources", StaticFlags)
            .GetValue(null);
        var sprites = new Sprite[paths.Length];
        for (int i = 0; i < paths.Length; i++)
        {
            sprites[i] = Resources.Load<Sprite>(paths[i]);
            Assert.That(sprites[i], Is.Not.Null, paths[i]);
        }

        var readiness = ownerType.GetMethod("RequiredArtReady", StaticFlags);
        Assert.That(readiness, Is.Not.Null);
        Assert.That((bool)readiness.Invoke(null, new object[] { sprites }), Is.True);
        sprites[0] = null;
        Assert.That((bool)readiness.Invoke(null, new object[] { sprites }), Is.False);
    }

    static string[] PersistentMethods(Button button)
    {
        var methods = new string[button.onClick.GetPersistentEventCount()];
        for (int i = 0; i < methods.Length; i++)
            methods[i] = button.onClick.GetPersistentMethodName(i);
        return methods;
    }

    static void AssertLocalizedHomeCopy(Transform root, int language,
        params string[] expected)
    {
        string[] names =
        {
            "HomeSoloTitle", "HomePrivateTitle", "HomeDailyTitle",
            "HomeTipTitle", "HomeTipBody"
        };
        Assert.That(expected.Length, Is.EqualTo(names.Length));
        SetLanguage(language);
        for (int i = 0; i < names.Length; i++)
        {
            var label = Find(root, names[i]).GetComponent<TMP_Text>();
            Assert.That(label.text, Is.EqualTo(expected[i]), names[i]);
            label.ForceMeshUpdate(true, true);
            Assert.That(label.textInfo.characterCount, Is.GreaterThan(0),
                names[i] + " must generate visible localized glyph geometry.");
            Assert.That(label.textBounds.size.x, Is.GreaterThan(1f),
                names[i] + " localized mesh must have non-zero width.");
            var localRect = label.rectTransform.rect;
            Assert.That(label.textBounds.max.y, Is.GreaterThan(localRect.yMin),
                names[i] + " localized mesh must intersect its visible text region.");
            Assert.That(label.textBounds.min.y, Is.LessThan(localRect.yMax),
                names[i] + " localized mesh must intersect its visible text region.");
        }
    }

    static void SetLanguage(int language)
    {
        var l10n = RuntimeType("L10n");
        var languageType = l10n.GetNestedType("Language");
        l10n.GetMethod("SetLanguage").Invoke(null,
            new[] { System.Enum.ToObject(languageType, language) });
    }

    static int CountNamed(Transform root, string name)
    {
        int count = root.name == name ? 1 : 0;
        for (int i = 0; i < root.childCount; i++)
            count += CountNamed(root.GetChild(i), name);
        return count;
    }

    static void InvokeInstaller(string typeName)
    {
        var type = RuntimeType(typeName);
        var install = type.GetMethod("Install", StaticFlags);
        Assert.That(install, Is.Not.Null);
        install.Invoke(null, null);
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

    static System.Type RuntimeType(string name)
    {
        var type = System.Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "Missing runtime component: " + name);
        return type;
    }
}
