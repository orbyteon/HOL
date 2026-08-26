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
            "HomeSpeechBubble",
            "HomeSpeechText",
            "HomePlayerChip",
            "HomePlayerAvatar",
            "HomeTrophyIcon",
            "HomePlayerChipText",
            "HomeSoloIcon",
            "HomePvpIcon",
            "HomeFriendIcon",
            "HomeDailyIcon",
            "HomeDailyPromo",
            "HomePromoTrophy",
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
            "HomeHeroGirl",
        })
        {
            Transform found = Find(canvas.transform, retired);
            Assert.That(found == null || !found.gameObject.activeInHierarchy, Is.True,
                "Retired Home presentation is visible: " + retired);
        }

        AssertSprite(root, "HomeBackground",
            "cartoonui/v1/home/hol_home_background_v1", Image.Type.Simple);
        AssertSprite(root, "HomeLogo",
            "reference/hol_logo_exact", Image.Type.Simple);
        AssertSprite(root, "HomeHeroBoy",
            "reference/player_cyan_exact", Image.Type.Simple);
        AssertSprite(root, "HomeSpeechBubble",
            "cartoonui/v1/raster/hol_speech_bubble_v1", Image.Type.Simple);

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
        AssertProductionButton(daily, "phase2a/hol_tip_frame_r2_9s");

        Assert.That(PersistentMethods(solo), Does.Contain("OnPlayPressed"));
        Assert.That(PersistentMethods(settings), Does.Contain("OpenSettings"));

        owner.GetType().GetMethod(
            "ApplyResponsiveLayoutForWidth", InstanceFlags)
            .Invoke(owner, new object[] { 1080, true });

        AssertRect(root, "HomeLogo",
            new Vector2(0f, 690f), new Vector2(585f, 310f));
        AssertRect(root, "HomeHeroBoy",
            new Vector2(-75f, 395f), new Vector2(500f, 500f));
        AssertRect(root, "HomeSpeechBubble",
            new Vector2(310f, 400f), new Vector2(340f, 190f));
        AssertRect(canvas.transform, "ButtonPlay",
            new Vector2(0f, 115f), new Vector2(930f, 164f));
        AssertRect(canvas.transform, "ButtonPvP",
            new Vector2(0f, -70f), new Vector2(930f, 164f));
        AssertRect(canvas.transform, "ButtonPrivateRoom",
            new Vector2(0f, -255f), new Vector2(930f, 164f));
        AssertRect(canvas.transform, "DailyHuntButton",
            new Vector2(0f, -440f), new Vector2(930f, 164f));
        AssertRect(root, "HomeDailyPromo",
            new Vector2(0f, -655f), new Vector2(650f, 155f));
        AssertRect(root, "HomeMascotSix",
            new Vector2(-430f, -805f), new Vector2(245f, 280f));
        AssertRect(root, "HomeMascotSeven",
            new Vector2(430f, -805f), new Vector2(245f, 280f));

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
            Assert.That(title.enableAutoSizing, Is.True);
            Assert.That(title.fontSizeMin, Is.GreaterThanOrEqualTo(32f), titleName);
            Assert.That(title.overflowMode, Is.EqualTo(TextOverflowModes.Overflow));
        }

        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            Assert.That(graphic is Image || graphic is TMP_Text, Is.True,
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
        Assert.That(chipText, Does.Contain("Marinos"));
        Assert.That(chipText, Does.Contain("12"));

        AssertLocalizedHomeCopy(root, 0,
            "PLAY SOLO VS AI", "PvP Duel", "Play with a friend", "DAILY HUNT");
        AssertLocalizedHomeCopy(root, 1,
            "ΠΑΙΞΕ SOLO ΜΕ AI", "Μονομαχία PvP", "Παίξε με φίλο", "ΚΥΝΗΓΙ ΗΜΕΡΑΣ");
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
        Image image = button.GetComponent<Image>();
        Sprite sprite = Resources.Load<Sprite>(resource);
        Assert.That(image, Is.Not.Null, button.name);
        Assert.That(sprite, Is.Not.Null, resource);
        Assert.That(image.sprite, Is.SameAs(sprite), button.name);
        Assert.That(image.type, Is.EqualTo(Image.Type.Sliced), button.name);
        Assert.That(image.color.a, Is.EqualTo(1f).Within(0.001f), button.name);
        Assert.That(image.raycastTarget, Is.True, button.name);
        Assert.That(button.targetGraphic, Is.SameAs(image), button.name);
        Assert.That(button.colors.fadeDuration, Is.LessThanOrEqualTo(0.08f));
    }

    static void AssertRect(
        Transform root,
        string objectName,
        Vector2 expectedPosition,
        Vector2 expectedSize)
    {
        RectTransform rect = Find(root, objectName) as RectTransform;
        Assert.That(rect, Is.Not.Null, objectName);
        Assert.That(Vector2.Distance(rect.anchoredPosition, expectedPosition),
            Is.LessThan(1f), objectName + " position drifted.");
        Assert.That(Vector2.Distance(rect.sizeDelta, expectedSize),
            Is.LessThan(1f), objectName + " size drifted.");
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
