using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class MainMenuAuthoritativeVisualsPlayModeTests
{
    static readonly string[] HomeButtonNames =
    {
        "ButtonPlay",
        "Buttonsettings",
        "ButtonPvP",
        "DailyHuntButton"
    };

    [UnityTest]
    public IEnumerator HomeCheckpointPreservesCallbacksHierarchyLocalizationAndGloss()
    {
        var ownerType = RuntimeType("MainMenuAuthoritativeVisuals");
        var exactType = RuntimeType("ExactReferenceVisuals");
        var reskinType = RuntimeType("AttachmentReskinVisuals");
        var polishType = RuntimeType("AttachmentReskinPolish");
        var bindingsType = RuntimeType("AttachmentReskinCanvasBindings");
        var designType = RuntimeType("DesignRuntimeWiring");
        var menuType = RuntimeType("MenuManager");
        var pvpRuntimeType = RuntimeType("PvpRuntimeUI");
        var pvpControllerType = RuntimeType("PvpGameController");
        var consentType = RuntimeType("ConsentManager");
        var dailyType = RuntimeType("DailyHunt");
        var l10nType = RuntimeType("L10n");

        InvokeInstaller(exactType);
        InvokeInstaller(reskinType);
        InvokeInstaller(polishType);
        InvokeInstaller(bindingsType);
        InvokeInstaller(ownerType);

        var languageType = l10nType.GetNestedType("Language", BindingFlags.Public);
        var currentLanguage = l10nType.GetProperty("Current", BindingFlags.Static | BindingFlags.Public);
        var setLanguage = l10nType.GetMethod("SetLanguage", BindingFlags.Static | BindingFlags.Public);
        Assert.That(languageType, Is.Not.Null);
        Assert.That(currentLanguage, Is.Not.Null);
        Assert.That(setLanguage, Is.Not.Null);
        object originalLanguage = currentLanguage.GetValue(null, null);
        object english = System.Enum.Parse(languageType, "English");
        object greek = System.Enum.Parse(languageType, "Greek");

        bool hadPlayerName = PlayerPrefs.HasKey("PlayerName");
        string originalPlayerName = PlayerPrefs.GetString("PlayerName", "");
        PlayerPrefs.SetString("PlayerName", "");
        setLanguage.Invoke(null, new[] { english });

        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        Scene scene = SceneManager.GetActiveScene();

        // Capture the four real controls as soon as the existing runtime
        // builders have produced them, before the authoritative owner's
        // quarter-second late-button pass is allowed to settle.
        Button[] originalButtons = null;
        for (int frame = 0; frame < 20 && originalButtons == null; frame++)
        {
            yield return null;
            originalButtons = FindButtons(scene, HomeButtonNames);
        }
        Assert.That(originalButtons, Is.Not.Null,
            "The scene/runtime builders must provide all four existing Home controls.");

        var originalEvents = new UnityEvent[originalButtons.Length];
        var originalPersistentCounts = new int[originalButtons.Length];
        for (int i = 0; i < originalButtons.Length; i++)
        {
            originalEvents[i] = originalButtons[i].onClick;
            originalPersistentCounts[i] = originalButtons[i].onClick.GetPersistentEventCount();
            Debug.Log("[MainMenu checkpoint] BEFORE " + ListenerEvidence(originalButtons[i]));
        }

        yield return new WaitForSecondsRealtime(0.45f);
        yield return null;

        var owner = FindInScene(scene, ownerType) as Component;
        Assert.That(owner, Is.Not.Null,
            "MainMenu must install its authoritative Home owner.");
        Assert.That((bool)ownerType.GetProperty("IsReady").GetValue(owner, null), Is.True);
        Assert.That((bool)ownerType.GetProperty("OwnsHome").GetValue(owner, null), Is.True);

        var menu = FindInScene(scene, menuType) as Component;
        Assert.That(menu, Is.Not.Null);
        GameObject mainMenuRoot = FieldGameObject(menu, "mainMenuPanel");
        GameObject settingsPanel = FieldGameObject(menu, "settingsPanel");
        GameObject playPanel = FieldGameObject(menu, "panelPlay");
        Assert.That(mainMenuRoot, Is.Not.Null);
        Assert.That(mainMenuRoot.name, Is.EqualTo("MainMenuRoot"),
            "The serialized BACKROUND object must be renamed, not replaced.");

        var mainCanvas = mainMenuRoot.GetComponentInParent<Canvas>();
        Assert.That(mainCanvas, Is.Not.Null);
        Assert.That(owner.GetComponent<Canvas>(), Is.SameAs(mainCanvas),
            "The owner must install only on the root canvas containing mainMenuPanel.");

        Transform safeArea = DirectChild(mainMenuRoot.transform, "SafeAreaRoot");
        Assert.That(safeArea, Is.Not.Null);
        Assert.That(mainMenuRoot.transform.childCount, Is.EqualTo(1),
            "MainMenuRoot must have SafeAreaRoot as its sole direct child.");
        Assert.That(safeArea.GetComponentsInChildren<Button>(true), Has.Length.EqualTo(4),
            "SafeAreaRoot must contain exactly the four real Home controls.");

        Button[] styledButtons = FindButtons(scene, HomeButtonNames);
        Assert.That(styledButtons, Is.Not.Null);
        for (int i = 0; i < styledButtons.Length; i++)
        {
            Assert.That(styledButtons[i], Is.SameAs(originalButtons[i]),
                HomeButtonNames[i] + " was recreated instead of reparented.");
            Assert.That(styledButtons[i].transform.parent, Is.SameAs(safeArea));
            Assert.That(styledButtons[i].onClick, Is.SameAs(originalEvents[i]),
                HomeButtonNames[i] + " received a replacement UnityEvent.");
            Assert.That(styledButtons[i].onClick.GetPersistentEventCount(),
                Is.EqualTo(originalPersistentCounts[i]));
        }

        var pvpRuntime = FindInScene(scene, pvpRuntimeType) as Component;
        var consent = FindInScene(scene, consentType) as Component;
        Assert.That(pvpRuntime, Is.Not.Null);
        Assert.That(consent, Is.Not.Null);
        Canvas pvpCanvas = pvpRuntime.GetComponent<Canvas>();
        Canvas consentCanvas = consent.GetComponent<Canvas>();
        Assert.That(pvpCanvas, Is.Not.Null);
        Assert.That(consentCanvas, Is.Not.Null);

        var screenCanvases = RootScreenCanvases(scene);
        Assert.That(screenCanvases, Has.Count.EqualTo(3),
            "MainMenu, PvP, and Consent are the only root screen-space canvases.");
        CollectionAssert.AreEquivalent(
            new[] { mainCanvas.GetInstanceID(), pvpCanvas.GetInstanceID(), consentCanvas.GetInstanceID() },
            new[]
            {
                screenCanvases[0].GetInstanceID(),
                screenCanvases[1].GetInstanceID(),
                screenCanvases[2].GetInstanceID()
            });

        AssertProductionImages(safeArea);
        AssertNoDormantOneVersusOne(safeArea);

        var playerHero = RequiredRect(safeArea, "HomePlayerHero");
        var opponentHero = RequiredRect(safeArea, "HomeOpponentHero");
        Assert.That(playerHero.sizeDelta, Is.EqualTo(opponentHero.sizeDelta));
        Assert.That(Mathf.Abs(playerHero.anchoredPosition.x),
            Is.EqualTo(Mathf.Abs(opponentHero.anchoredPosition.x)).Within(0.01f));
        Assert.That(playerHero.anchoredPosition.x,
            Is.EqualTo(-opponentHero.anchoredPosition.x).Within(0.01f));
        Assert.That(Find(safeArea, "HomeMascotSeven"), Is.Not.Null);
        Assert.That(Find(safeArea, "HomeMascotThree"), Is.Not.Null);

        var playButton = styledButtons[0];
        RectTransform primaryGloss = RequiredRect(playButton.transform, "HomePrimaryGloss");
        Assert.That(primaryGloss.parent, Is.SameAs(playButton.transform));
        Assert.That(primaryGloss.anchorMin, Is.EqualTo(Vector2.zero));
        Assert.That(primaryGloss.anchorMax, Is.EqualTo(Vector2.one));
        Assert.That(primaryGloss.offsetMin, Is.EqualTo(Vector2.zero));
        Assert.That(primaryGloss.offsetMax, Is.EqualTo(Vector2.zero));
        Assert.That(primaryGloss.GetComponent<Image>().raycastTarget, Is.False);

        RectTransform privateRect = (RectTransform)styledButtons[2].transform;
        RectTransform dailyRect = (RectTransform)styledButtons[3].transform;
        RectTransform secondaryGloss = RequiredRect(safeArea, "HomeSecondaryGlossRow");
        Assert.That(secondaryGloss.sizeDelta, Is.EqualTo(new Vector2(1000f, 320f)));
        Assert.That(secondaryGloss.GetComponent<Image>().raycastTarget, Is.False);
        Assert.That(privateRect.sizeDelta, Is.EqualTo(new Vector2(450f, 165f)));
        Assert.That(dailyRect.sizeDelta, Is.EqualTo(new Vector2(450f, 165f)));
        Assert.That(privateRect.anchoredPosition.y,
            Is.EqualTo(secondaryGloss.anchoredPosition.y).Within(0.01f));
        Assert.That(dailyRect.anchoredPosition.y,
            Is.EqualTo(secondaryGloss.anchoredPosition.y).Within(0.01f));
        Assert.That(privateRect.anchoredPosition.x,
            Is.EqualTo(secondaryGloss.anchoredPosition.x - 245f).Within(0.01f));
        Assert.That(dailyRect.anchoredPosition.x,
            Is.EqualTo(secondaryGloss.anchoredPosition.x + 245f).Within(0.01f));

        AssertLocalizedHome(safeArea, l10nType);
        setLanguage.Invoke(null, new[] { greek });
        yield return null;
        AssertLocalizedHome(safeArea, l10nType);

        // Invoke each preserved UnityEvent and prove it still reaches the
        // controller-owned destination that existed before this presentation.
        playButton.onClick.Invoke();
        Assert.That(playPanel.activeSelf, Is.True, "Solo must open PanelPlay.");
        menu.SendMessage("BackToMenu", SendMessageOptions.RequireReceiver);

        int rootId = mainMenuRoot.GetInstanceID();
        int safeAreaId = safeArea.GetInstanceID();
        styledButtons[1].onClick.Invoke();
        Assert.That(settingsPanel.activeSelf, Is.True, "Settings must open PanelSettings.");
        yield return new WaitForSecondsRealtime(0.3f);
        Assert.That((bool)ownerType.GetProperty("OwnsHome").GetValue(owner, null), Is.False);
        AssertLegacyOwnerStates(mainCanvas, exactType, reskinType, polishType,
            bindingsType, designType, false);

        menu.SendMessage("BackToMenu", SendMessageOptions.RequireReceiver);
        yield return new WaitForSecondsRealtime(0.3f);
        Assert.That((bool)ownerType.GetProperty("OwnsHome").GetValue(owner, null), Is.True);
        Assert.That(FieldGameObject(menu, "mainMenuPanel").GetInstanceID(), Is.EqualTo(rootId));
        Assert.That(DirectChild(mainMenuRoot.transform, "SafeAreaRoot").GetInstanceID(),
            Is.EqualTo(safeAreaId));
        Assert.That(mainMenuRoot.transform.childCount, Is.EqualTo(1));
        Assert.That(CountNamed(scene, "MainMenuRoot"), Is.EqualTo(1));
        Assert.That(CountNamed(scene, "SafeAreaRoot"), Is.EqualTo(1));
        AssertUniqueHomeNames(safeArea);
        AssertLegacyOwnerStates(mainCanvas, exactType, reskinType, polishType,
            bindingsType, designType, true);

        var pvpController = FindInScene(scene, pvpControllerType) as Component;
        Assert.That(pvpController, Is.Not.Null);
        GameObject pvpMenuPanel = FieldGameObject(pvpController, "pvpMenuPanel");
        styledButtons[2].onClick.Invoke();
        Assert.That(pvpMenuPanel.activeSelf, Is.True,
            "Private Room must open the existing PvPMenuPanel.");
        pvpController.SendMessage("ClosePvpMenu", SendMessageOptions.RequireReceiver);

        Transform dailyPanel = Find(mainCanvas.transform, "DailyHuntPanel");
        Assert.That(dailyPanel, Is.Not.Null);
        styledButtons[3].onClick.Invoke();
        Assert.That(dailyPanel.gameObject.activeSelf, Is.True,
            "Daily Hunt must open the existing DailyHuntPanel.");
        var daily = dailyPanel.GetComponent(dailyType) as Component;
        Assert.That(daily, Is.Not.Null);
        daily.SendMessage("Close", SendMessageOptions.RequireReceiver);

        for (int i = 0; i < styledButtons.Length; i++)
        {
            Assert.That(styledButtons[i], Is.SameAs(originalButtons[i]));
            Assert.That(styledButtons[i].onClick, Is.SameAs(originalEvents[i]));
            Debug.Log("[MainMenu checkpoint] AFTER " + ListenerEvidence(styledButtons[i]));
        }
        Debug.Log("[MainMenu checkpoint] ROOT MainMenuRoot=" + rootId +
                  " SafeAreaRoot=" + safeAreaId +
                  " MainCanvas=" + mainCanvas.GetInstanceID() +
                  " PvPCanvas=" + pvpCanvas.GetInstanceID() +
                  " ConsentCanvas=" + consentCanvas.GetInstanceID());
        Debug.Log("[MainMenu checkpoint] GLOSS primaryParent=" + playButton.GetInstanceID() +
                  " secondary=(1000x320 @ 0,-150; arcs @ -245,+245)");

        setLanguage.Invoke(null, new[] { originalLanguage });
        if (hadPlayerName)
            PlayerPrefs.SetString("PlayerName", originalPlayerName);
        else
            PlayerPrefs.DeleteKey("PlayerName");
        PlayerPrefs.Save();
    }

    static void AssertLegacyOwnerStates(Canvas canvas, System.Type exactType,
        System.Type reskinType, System.Type polishType, System.Type bindingsType,
        System.Type designType, bool homeActive)
    {
        var exact = canvas.GetComponent(exactType) as Behaviour;
        var reskin = canvas.GetComponent(reskinType) as Behaviour;
        var polish = canvas.GetComponent(polishType) as Behaviour;
        var bindings = canvas.GetComponent(bindingsType) as Behaviour;
        Assert.That(exact, Is.Not.Null);
        Assert.That(reskin, Is.Not.Null);
        Assert.That(polish, Is.Not.Null);
        Assert.That(bindings, Is.Not.Null);
        Assert.That(exact.enabled, Is.False);
        Assert.That(reskin.enabled, Is.EqualTo(!homeActive));
        Assert.That(polish.enabled, Is.EqualTo(!homeActive));
        Assert.That(bindings.enabled, Is.EqualTo(!homeActive));

        foreach (var root in canvas.gameObject.scene.GetRootGameObjects())
        {
            foreach (var component in root.GetComponentsInChildren(designType, true))
                Assert.That(((Behaviour)component).enabled, Is.False);
        }
    }

    static void AssertProductionImages(Transform safeArea)
    {
        foreach (var image in safeArea.GetComponentsInChildren<Image>(true))
        {
            Assert.That(image.sprite, Is.Not.Null, image.name + " has no production sprite.");
            var resource = Resources.Load<Sprite>("mainmenu/" + image.sprite.name);
            Assert.That(image.sprite, Is.SameAs(resource),
                image.name + " does not use the final Resources/mainmenu sprite.");
            if (image.GetComponent<Button>() == null)
                Assert.That(image.raycastTarget, Is.False,
                    image.name + " is decoration and must not intercept input.");
        }

        AssertButtonSprite(safeArea, "ButtonPlay", "mainmenu_cta_gold_9s");
        AssertButtonSprite(safeArea, "ButtonPvP", "mainmenu_cta_blue_9s");
        AssertButtonSprite(safeArea, "DailyHuntButton", "mainmenu_daily_hunt_frame_9s");
        AssertButtonSprite(safeArea, "Buttonsettings", "mainmenu_gear_glossy");
    }

    static void AssertButtonSprite(Transform root, string buttonName, string resourceName)
    {
        var button = Find(root, buttonName).GetComponent<Button>();
        var image = button.GetComponent<Image>();
        Assert.That(image.sprite, Is.SameAs(Resources.Load<Sprite>("mainmenu/" + resourceName)));
        if (buttonName != "Buttonsettings")
            Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
    }

    static void AssertNoDormantOneVersusOne(Transform safeArea)
    {
        string[] banned = { "1V1", "ONLINE", "QUICKMATCH", "MAINMENU_ICON_1V1",
            "MAINMENU_CTA_VIOLET_9S" };
        var tmpType = System.Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
        Assert.That(tmpType, Is.Not.Null);

        foreach (var child in safeArea.GetComponentsInChildren<Transform>(true))
        {
            AssertNoBanned(child.name, banned, "object name");
            var image = child.GetComponent<Image>();
            if (image != null && image.sprite != null)
                AssertNoBanned(image.sprite.name, banned, "sprite");
            var text = child.GetComponent(tmpType);
            if (text != null)
                AssertNoBanned(TextValue(text), banned, "TMP text");
        }
    }

    static void AssertNoBanned(string value, string[] banned, string source)
    {
        string upper = (value ?? "").ToUpperInvariant();
        foreach (string term in banned)
            Assert.That(upper, Does.Not.Contain(term), source + " exposes dormant 1V1 UI: " + value);
    }

    static void AssertLocalizedHome(Transform safeArea, System.Type l10nType)
    {
        var expected = new Dictionary<string, string>
        {
            { "HomeSoloTitle", LocalizedCopy(l10nType, "mainmenu_play_title") },
            { "HomeSoloSubtitle", LocalizedCopy(l10nType, "mainmenu_play_subtitle") },
            { "HomePrivateTitle", LocalizedCopy(l10nType, "mainmenu_private_title") },
            { "HomePrivateSubtitle", LocalizedCopy(l10nType, "mainmenu_private_subtitle") },
            { "HomeDailyTitle", LocalizedCopy(l10nType, "daily_hunt").ToUpperInvariant() },
            { "HomeDailySubtitle", LocalizedCopy(l10nType, "mainmenu_daily_subtitle") },
            { "HomeTipTitle", LocalizedCopy(l10nType, "hud_tip").ToUpperInvariant() },
            { "HomeTipBody", LocalizedCopy(l10nType, "simulated_opponents") }
        };

        var tmpType = System.Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
        var liveTexts = new List<Component>();
        foreach (var component in safeArea.GetComponentsInChildren(tmpType, true))
        {
            var text = component as Component;
            if (text != null && text.gameObject.activeInHierarchy)
                liveTexts.Add(text);
        }

        Assert.That(liveTexts, Has.Count.EqualTo(expected.Count + 1),
            "Every visible Home TMP string must be part of the localized checkpoint copy.");
        foreach (var pair in expected)
        {
            Transform item = Find(safeArea, pair.Key);
            Assert.That(item, Is.Not.Null, "Missing localized Home TMP: " + pair.Key);
            Assert.That(TextValue(item.GetComponent(tmpType)), Is.EqualTo(pair.Value));
        }

        Transform chip = Find(safeArea, "HomePlayerChipText");
        Assert.That(chip, Is.Not.Null);
        string chipCopy = TextValue(chip.GetComponent(tmpType));
        Assert.That(chipCopy, Does.Contain(
            LocalizedCopy(l10nType, "player_default").ToUpperInvariant()));
        Assert.That(chipCopy, Does.Contain(
            LocalizedCopy(l10nType, "stats_streak").ToUpperInvariant()));

        Debug.Log("[MainMenu checkpoint] L10N " +
                  LocalizedCopy(l10nType, "mainmenu_play_title") + " | " +
                  LocalizedCopy(l10nType, "mainmenu_private_title") + " | " +
                  LocalizedCopy(l10nType, "daily_hunt"));
    }

    static string TextValue(Component component)
    {
        Assert.That(component, Is.Not.Null);
        return (string)component.GetType().GetProperty("text").GetValue(component, null);
    }

    static string LocalizedCopy(System.Type l10nType, string key)
    {
        foreach (var method in l10nType.GetMethods(BindingFlags.Static | BindingFlags.Public))
        {
            if (method.Name != "Get" || method.ReturnType != typeof(string)) continue;
            var parameters = method.GetParameters();
            if (parameters.Length == 0 || parameters[0].ParameterType != typeof(string)) continue;

            var arguments = new object[parameters.Length];
            arguments[0] = key;
            for (int i = 1; i < parameters.Length; i++)
                arguments[i] = parameters[i].ParameterType == typeof(object[])
                    ? new object[0]
                    : parameters[i].DefaultValue;
            return (string)method.Invoke(null, arguments);
        }

        Assert.Fail("Missing compatible L10n.Get(string, ...) overload.");
        return null;
    }

    static string ListenerEvidence(Button button)
    {
        var methods = new List<string>();
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            methods.Add(button.onClick.GetPersistentMethodName(i));
        return button.name + " id=" + button.GetInstanceID() +
               " event=" + button.onClick.GetHashCode() +
               " persistent=[" + string.Join(",", methods.ToArray()) + "]";
    }

    static Button[] FindButtons(Scene scene, string[] names)
    {
        var result = new Button[names.Length];
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                for (int i = 0; i < names.Length; i++)
                    if (button.name == names[i])
                        result[i] = button;
            }
        }
        for (int i = 0; i < result.Length; i++)
            if (result[i] == null) return null;
        return result;
    }

    static List<Canvas> RootScreenCanvases(Scene scene)
    {
        var canvases = new List<Canvas>();
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace)
                    canvases.Add(canvas);
            }
        }
        return canvases;
    }

    static void AssertUniqueHomeNames(Transform safeArea)
    {
        var names = new HashSet<string>();
        foreach (var child in safeArea.GetComponentsInChildren<Transform>(true))
        {
            if (!child.name.StartsWith("Home")) continue;
            Assert.That(names.Add(child.name), Is.True,
                "Returning Home created a duplicate " + child.name);
        }
    }

    static int CountNamed(Scene scene, string name)
    {
        int count = 0;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) count++;
        return count;
    }

    static RectTransform RequiredRect(Transform root, string name)
    {
        Transform found = Find(root, name);
        Assert.That(found, Is.Not.Null, "Missing Home element: " + name);
        var rect = found as RectTransform;
        Assert.That(rect, Is.Not.Null, name + " is not a RectTransform.");
        return rect;
    }

    static GameObject FieldGameObject(Component component, string fieldName)
    {
        var field = component.GetType().GetField(fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Missing field " + fieldName);
        return field.GetValue(component) as GameObject;
    }

    static Transform DirectChild(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name) return parent.GetChild(i);
        return null;
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

    static Component FindInScene(Scene scene, System.Type type)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = root.GetComponentInChildren(type, true) as Component;
            if (found != null) return found;
        }
        return null;
    }

    static void InvokeInstaller(System.Type type)
    {
        var install = type.GetMethod("Install", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(install, Is.Not.Null, "Missing runtime installer on " + type.Name);
        install.Invoke(null, null);
    }

    static System.Type RuntimeType(string name)
    {
        var type = System.Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "Missing runtime component: " + name);
        return type;
    }
}
