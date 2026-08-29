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
    public IEnumerator HomeMatchesApprovedFourModeCartoonCompositionAndRemainsPlayable()
    {
        Screen.SetResolution(1080, 1920, false);
        InvokeInstaller("MainMenuHomeVisuals");
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);

        Component owner = null;
        for (int frame = 0; frame < 160; frame++)
        {
            owner = FindInScene(RuntimeType("MainMenuHomeVisuals"));
            if (owner != null &&
                GetProperty<bool>(owner, "IsReady") &&
                GetProperty<bool>(owner, "IsSettled"))
                break;
            yield return null;
        }

        Assert.That(owner, Is.Not.Null);
        Assert.That(GetProperty<bool>(owner, "IsReady"), Is.True);
        Assert.That(GetProperty<bool>(owner, "IsSettled"), Is.True);
        Assert.That(CountInScene(RuntimeType("MainMenuHomeVisuals")), Is.EqualTo(1),
            "Home must have exactly one presentation owner.");

        var canvas = owner.GetComponent<Canvas>();
        Assert.That(canvas, Is.Not.Null);
        Transform root = Find(canvas.transform, "HomeVisualRoot");
        Assert.That(root, Is.Not.Null);

        string[] required =
        {
            "HomeBackground",
            "HomeOuterFrame",
            "HomeStars",
            "HomeConfetti",
            "HomeSafeAreaRoot",
            "HomeLogo",
            "HomeHeroBoy",
            "HomeHeroGirl",
            "HomeSpeechBubble",
            "HomeSpeechText",
            "HomePlayerChip",
            "HomePlayerAvatar",
            "HomeTrophyIcon",
            "HomePlayerChipText",
            "HomePlayerChipScore",
            "HomeSoloIcon",
            "HomePvpIcon",
            "HomeFriendIcon",
            "HomeDailyIcon",
            "HomeDailyGift",
            "HomeDailyPromo",
            "HomePromoTrophy",
            "HomePortal",
            "HomeMascotSix",
            "HomeMascotSeven",
        };
        foreach (string name in required)
            Assert.That(Find(root, name), Is.Not.Null,
                "Missing approved Home object: " + name);

        foreach (string retired in new[]
        {
            "ExactReferenceBackdrop",
            "AttachmentReferenceBackdrop",
            "BoardHomeLogo",
            "HomeNeonBackdrop",
            "HomeArenaGrid",
            "HomeDecoStars",
            "HomeDecoLightning",
            "HomeDecoConfetti",
            "HomeDecoNumbers",
        })
        {
            Transform found = Find(canvas.transform, retired);
            Assert.That(found == null || !found.gameObject.activeInHierarchy, Is.True,
                "Retired Home presentation is visible: " + retired);
        }

        AssertSprite(root, "HomeBackground",
            "settings/hol_settings_bg_r1", Image.Type.Simple);
        AssertSprite(root, "HomeLogo",
            "reference/hol_logo_exact", Image.Type.Simple);
        AssertSprite(root, "HomeHeroBoy",
            "phase2a/hol_menu_boy_arms_crossed_r3", Image.Type.Simple);
        AssertSprite(root, "HomeHeroGirl",
            "phase2a/hol_menu_girl_forward_fist_r3", Image.Type.Simple);
        AssertSprite(root, "HomeSpeechBubble",
            "cartoon/cartoon_speech_bubble_raster", Image.Type.Sliced);
        AssertSprite(root, "HomeOuterFrame",
            "mainmenu/mainmenu_outer_frame_reference_v1", Image.Type.Simple);
        AssertSprite(root, "HomePortal",
            "dailyhunt/production/daily_floor_portal", Image.Type.Simple);
        AssertSprite(root, "HomeDailyGift",
            "mainmenu/mainmenu_daily_gift_reference_v1", Image.Type.Simple);

        Button solo = Find(canvas.transform, "ButtonPlay").GetComponent<Button>();
        Button pvp = Find(canvas.transform, "ButtonPvP").GetComponent<Button>();
        Button friend = Find(canvas.transform, "ButtonPrivateRoom").GetComponent<Button>();
        Button daily = Find(canvas.transform, "DailyHuntButton").GetComponent<Button>();
        Button settings = Find(canvas.transform, "Buttonsettings").GetComponent<Button>();
        Assert.That(solo, Is.Not.Null);
        Assert.That(pvp, Is.Not.Null);
        Assert.That(friend, Is.Not.Null);
        Assert.That(daily, Is.Not.Null);
        Assert.That(settings, Is.Not.Null);
        Assert.That(CountNamedButtons(canvas.transform, "ButtonPrivateRoom"),
            Is.EqualTo(1));

        AssertProductionButton(solo, "phase2a/hol_cta_gold_r2_9s");
        AssertProductionButton(pvp, "phase2a/hol_cta_magenta_r2_9s");
        AssertProductionButton(friend, "phase2a/hol_cta_blue_r2_9s");
        AssertProductionButton(daily, "dailyhunt/v1/daily_action_revive_v1");

        Assert.That(PersistentMethods(solo), Does.Contain("OnPlayPressed"));
        Assert.That(PersistentMethods(settings), Does.Contain("OpenSettings"));

        owner.GetType().GetMethod(
            "ApplyResponsiveLayoutForWidth", InstanceFlags)
            .Invoke(owner, new object[] { 1080, true });

        AssertReferenceComposition(root, solo, pvp, friend, daily);

        foreach (string titleName in new[]
        {
            "HomeSoloTitle",
            "HomePvpTitle",
            "HomeFriendTitle",
            "HomeDailyTitle",
        })
        {
            TMP_Text title = Find(root, titleName).GetComponent<TMP_Text>();
            Assert.That(title.font, Is.SameAs(Resources.Load<TMP_FontAsset>(
                "phase2a/fonts/HOL Menu Display SDF")));
            Assert.That(title.enableAutoSizing, Is.False,
                titleName + " must not shrink to make rejected geometry fit.");
            Assert.That(title.fontSize, Is.GreaterThanOrEqualTo(70f), titleName);
            Assert.That(title.overflowMode, Is.EqualTo(TextOverflowModes.Overflow));
        }

        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            Assert.That(IsAllowedProductionGraphic(graphic), Is.True,
                "Procedural Graphic found on Home: " +
                graphic.GetType().Name + " / " + graphic.name);
            if (graphic is Image image && image.sprite != null)
                Assert.That(image.color.a, Is.GreaterThanOrEqualTo(0.99f),
                    image.name + " hides approved production art.");
        }

        PlayerPrefs.SetInt("StatWins", 12);
        PlayerPrefs.SetString("PlayerName", "Marinos");
        PlayerPrefs.Save();
        owner.GetType().GetMethod("RefreshChip", InstanceFlags).Invoke(owner, null);
        string chipText = Find(root, "HomePlayerChipText")
            .GetComponent<TMP_Text>().text;
        string chipScore = Find(root, "HomePlayerChipScore")
            .GetComponent<TMP_Text>().text;
        Assert.That(chipText, Is.EqualTo("Marinos"));
        Assert.That(chipScore, Is.EqualTo("12"));

        AssertLocalizedHomeCopy(root, 0,
            "PLAY SOLO", "PVP DUEL", "PLAY WITH A FRIEND", "DAILY HUNT");
        AssertLocalizedHomeCopy(root, 1,
            "ΠΑΙΞΕ SOLO", "PVP DUEL", "ΠΑΙΞΕ ΜΕ ΦΙΛΟ", "DAILY HUNT");
        SetLanguage(0);

        // Both online entries must route into the controller-owned room hub.
        Component controller = FindInScene(RuntimeType("PvpGameController"));
        Assert.That(controller, Is.Not.Null);
        GameObject pvpMenu = GetField<GameObject>(controller, "pvpMenuPanel");
        Assert.That(pvpMenu, Is.Not.Null);

        pvp.onClick.Invoke();
        yield return null;
        Assert.That(pvpMenu.activeSelf, Is.True,
            "PvP Duel entry lost its real controller callback.");

        pvpMenu.SetActive(false);
        var menuManager = FindInScene(RuntimeType("MenuManager"));
        GameObject mainMenu = GetField<GameObject>(menuManager, "mainMenuPanel");
        mainMenu.SetActive(true);
        friend.onClick.Invoke();
        yield return null;
        Assert.That(pvpMenu.activeSelf, Is.True,
            "Play With A Friend entry is not wired to the real room hub.");
    }

    static bool IsAllowedProductionGraphic(Graphic graphic)
    {
        if (graphic is Image || graphic is TMP_Text)
            return true;

        var subMesh = graphic as TMP_SubMeshUI;
        return subMesh != null &&
               subMesh.transform.parent != null &&
               subMesh.transform.parent.GetComponent<TMP_Text>() != null;
    }

    static void AssertSprite(
        Transform root,
        string objectName,
        string resource,
        Image.Type type)
    {
        Image image = Find(root, objectName).GetComponent<Image>();
        Sprite sprite = Resources.Load<Sprite>(resource);
        Assert.That(image, Is.Not.Null, objectName);
        Assert.That(sprite, Is.Not.Null, resource);
        Assert.That(image.sprite, Is.SameAs(sprite), objectName);
        Assert.That(image.type, Is.EqualTo(type), objectName);
        Assert.That(image.color.a, Is.EqualTo(1f).Within(0.001f), objectName);
        Assert.That(image.raycastTarget, Is.False, objectName);
    }

    static void AssertProductionButton(Button button, string resource)
    {
        Image hitImage = button.GetComponent<Image>();
        Transform frameTransform = Find(button.transform, "HomeCtaFrame");
        Image frame = frameTransform == null
            ? null
            : frameTransform.GetComponent<Image>();
        Sprite sprite = Resources.Load<Sprite>(resource);
        Assert.That(hitImage, Is.Not.Null, button.name);
        Assert.That(frame, Is.Not.Null, button.name + " visual frame");
        Assert.That(sprite, Is.Not.Null, resource);
        Assert.That(frame.sprite, Is.SameAs(sprite), button.name);
        Assert.That(frame.type, Is.EqualTo(Image.Type.Simple), button.name);
        Assert.That(frame.color.a, Is.EqualTo(1f).Within(0.001f), button.name);
        Assert.That(frame.raycastTarget, Is.False, button.name);
        Assert.That(hitImage.sprite, Is.Null, button.name);
        Assert.That(hitImage.raycastTarget, Is.True, button.name);
        Assert.That(button.targetGraphic, Is.SameAs(hitImage), button.name);
        Assert.That(button.transition, Is.EqualTo(Selectable.Transition.None));
        Assert.That(button.interactable, Is.True, button.name);
    }

    static void AssertReferenceComposition(
        Transform root,
        params Button[] buttons)
    {
        RectTransform logo = Find(root, "HomeLogo") as RectTransform;
        RectTransform boy = Find(root, "HomeHeroBoy") as RectTransform;
        RectTransform girl = Find(root, "HomeHeroGirl") as RectTransform;
        RectTransform bubble = Find(root, "HomeSpeechBubble") as RectTransform;
        RectTransform promo = Find(root, "HomeDailyPromo") as RectTransform;
        RectTransform six = Find(root, "HomeMascotSix") as RectTransform;
        RectTransform seven = Find(root, "HomeMascotSeven") as RectTransform;

        Assert.That(logo.sizeDelta.x, Is.GreaterThanOrEqualTo(540f));
        Assert.That(boy.sizeDelta.x, Is.GreaterThanOrEqualTo(420f));
        Assert.That(girl.sizeDelta.x, Is.GreaterThanOrEqualTo(420f));
        Assert.That(bubble.anchoredPosition.x, Is.GreaterThan(250f));
        Assert.That(promo.sizeDelta.x, Is.GreaterThanOrEqualTo(480f));
        Assert.That(six.anchoredPosition.x, Is.LessThan(-250f));
        Assert.That(seven.anchoredPosition.x, Is.GreaterThan(250f));

        for (int i = 0; i < buttons.Length; i++)
        {
            RectTransform hit = buttons[i].transform as RectTransform;
            RectTransform visual = Find(buttons[i].transform, "HomeCtaFrame")
                as RectTransform;
            Assert.That(hit.sizeDelta.x, Is.GreaterThanOrEqualTo(960f),
                buttons[i].name + " hit width");
            Assert.That(visual.sizeDelta.x, Is.GreaterThanOrEqualTo(980f),
                buttons[i].name + " visual width");
            Assert.That(visual.sizeDelta.y, Is.GreaterThan(hit.sizeDelta.y),
                buttons[i].name + " keeps visual glow outside its hit rect");
            if (i == 0) continue;

            RectTransform previous = buttons[i - 1].transform as RectTransform;
            float previousBottom = previous.anchoredPosition.y -
                                   previous.sizeDelta.y * 0.5f;
            float currentTop = hit.anchoredPosition.y + hit.sizeDelta.y * 0.5f;
            Assert.That(previousBottom, Is.GreaterThan(currentTop),
                buttons[i - 1].name + " and " + buttons[i].name +
                " must not have overlapping touch ownership.");
        }
    }

    static void AssertLocalizedHomeCopy(
        Transform root,
        int language,
        string solo,
        string pvp,
        string friend,
        string daily)
    {
        SetLanguage(language);
        Assert.That(Find(root, "HomeSoloTitle").GetComponent<TMP_Text>().text,
            Is.EqualTo(solo));
        Assert.That(Find(root, "HomePvpTitle").GetComponent<TMP_Text>().text,
            Is.EqualTo(pvp));
        Assert.That(Find(root, "HomeFriendTitle").GetComponent<TMP_Text>().text,
            Is.EqualTo(friend));
        Assert.That(Find(root, "HomeDailyTitle").GetComponent<TMP_Text>().text,
            Is.EqualTo(daily));
    }

    static int CountNamedButtons(Transform root, string name)
    {
        int count = 0;
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            if (button.name == name)
                count++;
        }
        return count;
    }

    static string[] PersistentMethods(Button button)
    {
        int count = button.onClick.GetPersistentEventCount();
        string[] names = new string[count];
        for (int i = 0; i < count; i++)
            names[i] = button.onClick.GetPersistentMethodName(i);
        return names;
    }

    static T GetField<T>(Component component, string name) where T : class
    {
        FieldInfo field = component.GetType().GetField(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Missing field " + name);
        return field.GetValue(component) as T;
    }

    static T GetProperty<T>(Component component, string name)
    {
        PropertyInfo property = component.GetType().GetProperty(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, "Missing property " + name);
        return (T)property.GetValue(component);
    }

    static void SetLanguage(int value)
    {
        Type l10n = RuntimeType("L10n");
        Type language = l10n.GetNestedType("Language");
        object enumValue = Enum.ToObject(language, value);
        l10n.GetMethod("SetLanguage", BindingFlags.Static | BindingFlags.Public)
            .Invoke(null, new[] { enumValue });
    }

    static void InvokeInstaller(string typeName)
    {
        Type type = RuntimeType(typeName);
        MethodInfo method = type.GetMethod(
            "Install", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(null, null);
    }

    static Component FindInScene(Type type)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Component found = root.GetComponentInChildren(type, true) as Component;
            if (found != null) return found;
        }
        return null;
    }

    static int CountInScene(Type type)
    {
        int count = 0;
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            count += root.GetComponentsInChildren(type, true).Length;
        return count;
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
