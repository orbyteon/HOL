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

        var languageType = l10nType.GetNestedType("Language", BindingFlags.Public);
        var currentLanguage = l10nType.GetProperty("Current", BindingFlags.Static | BindingFlags.Public);
        var setLanguage = l10nType.GetMethod("SetLanguage", BindingFlags.Static | BindingFlags.Public);
        Assert.That(languageType, Is.Not.Null);
        Assert.That(currentLanguage, Is.Not.Null);
        Assert.That(setLanguage, Is.Not.Null);
        object originalLanguage = currentLanguage.GetValue(null, null);
        object english = System.Enum.Parse(languageType, "English");
        object greek = System.Enum.Parse(languageType, "Greek");
        var prefs = SnapshotPlayerPrefs();
        try
        {
            UnsubscribeSceneLoaded(ownerType);
            PlayerPrefs.SetString("PlayerName", "");
            setLanguage.Invoke(null, new[] { english });

            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            Scene scene = SceneManager.GetActiveScene();

            Button[] originalButtons = null;
            for (int frame = 0; frame < 20 && originalButtons == null; frame++)
            {
                yield return null;
                originalButtons = FindButtons(scene, HomeButtonNames);
            }
            Assert.That(originalButtons, Is.Not.Null,
                "The scene/runtime builders must provide all four existing Home controls.");
            Assert.That(FindInScene(scene, ownerType), Is.Null,
                "The callback baseline must be captured before the authoritative owner runs.");

            var menu = FindInScene(scene, menuType) as Component;
            Assert.That(menu, Is.Not.Null);
            GameObject mainMenuRoot = FieldGameObject(menu, "mainMenuPanel");
            GameObject settingsPanel = FieldGameObject(menu, "settingsPanel");
            GameObject playPanel = FieldGameObject(menu, "panelPlay");
            Assert.That(mainMenuRoot, Is.Not.Null);
            var mainCanvas = mainMenuRoot.GetComponentInParent<Canvas>();
            Assert.That(mainCanvas, Is.Not.Null);

            var pvpController = FindInScene(scene, pvpControllerType) as Component;
            Assert.That(pvpController, Is.Not.Null);
            GameObject pvpMenuPanel = FieldGameObject(pvpController, "pvpMenuPanel");
            Transform dailyPanel = Find(mainCanvas.transform, "DailyHuntPanel");
            Assert.That(dailyPanel, Is.Not.Null);
            var daily = dailyPanel.GetComponent(dailyType) as Component;
            Assert.That(daily, Is.Not.Null);

            var originalIds = new int[originalButtons.Length];
            var originalEvents = new UnityEvent[originalButtons.Length];
            var originalPersistentCounts = new int[originalButtons.Length];
            for (int i = 0; i < originalButtons.Length; i++)
            {
                originalIds[i] = originalButtons[i].GetInstanceID();
                originalEvents[i] = originalButtons[i].onClick;
                originalPersistentCounts[i] = originalButtons[i].onClick.GetPersistentEventCount();
                Debug.Log("[MainMenu checkpoint] BEFORE " + ListenerEvidence(originalButtons[i]));
            }

            // Prove the captured controls reach their real destinations before
            // the authoritative presentation owner is added.
            originalButtons[0].onClick.Invoke();
            Assert.That(playPanel.activeSelf, Is.True, "Pre-owner Solo must open PanelPlay.");
            menu.SendMessage("BackToMenu", SendMessageOptions.RequireReceiver);
            originalButtons[1].onClick.Invoke();
            Assert.That(settingsPanel.activeSelf, Is.True,
                "Pre-owner Settings must open PanelSettings.");
            menu.SendMessage("BackToMenu", SendMessageOptions.RequireReceiver);
            originalButtons[2].onClick.Invoke();
            Assert.That(pvpMenuPanel.activeSelf, Is.True,
                "Pre-owner Private Room must open the existing PvPMenuPanel.");
            pvpController.SendMessage("ClosePvpMenu", SendMessageOptions.RequireReceiver);
            originalButtons[3].onClick.Invoke();
            Assert.That(dailyPanel.gameObject.activeSelf, Is.True,
                "Pre-owner Daily Hunt must open the existing DailyHuntPanel.");
            daily.SendMessage("Close", SendMessageOptions.RequireReceiver);
            Assert.That(mainMenuRoot.activeInHierarchy, Is.True);

            var owner = mainCanvas.gameObject.AddComponent(ownerType) as Component;
            Assert.That(owner, Is.Not.Null);
            for (int frame = 0;
                 frame < 30 && !(bool)ownerType.GetProperty("IsReady").GetValue(owner, null);
                 frame++)
                yield return null;
            Assert.That((bool)ownerType.GetProperty("IsReady").GetValue(owner, null), Is.True);
            Assert.That((bool)ownerType.GetProperty("OwnsHome").GetValue(owner, null), Is.True);
            Assert.That(mainMenuRoot.name, Is.EqualTo("MainMenuRoot"),
                "The serialized BACKROUND object must be renamed, not replaced.");
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
                Assert.That(styledButtons[i].GetInstanceID(), Is.EqualTo(originalIds[i]),
                    HomeButtonNames[i] + " was recreated instead of reparented.");
                Assert.That(styledButtons[i], Is.SameAs(originalButtons[i]));
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

            RectTransform playRect = (RectTransform)playButton.transform;
            RectTransform privateRect = (RectTransform)styledButtons[2].transform;
            RectTransform dailyRect = (RectTransform)styledButtons[3].transform;
            Vector2 stackedCta = new Vector2(900f, 156f);
            Assert.That(playRect.sizeDelta, Is.EqualTo(stackedCta));
            Assert.That(privateRect.sizeDelta, Is.EqualTo(stackedCta));
            Assert.That(dailyRect.sizeDelta, Is.EqualTo(stackedCta));
            Assert.That(playRect.anchoredPosition, Is.EqualTo(new Vector2(0f, 90f)));
            Assert.That(privateRect.anchoredPosition, Is.EqualTo(new Vector2(0f, -90f)));
            Assert.That(dailyRect.anchoredPosition, Is.EqualTo(new Vector2(0f, -270f)));
            AssertStackedCtaCopy(playButton.transform, "HomeSolo");
            AssertStackedCtaCopy(styledButtons[2].transform, "HomePrivate");
            AssertStackedCtaCopy(styledButtons[3].transform, "HomeDaily");
            AssertHiddenIfPresent(safeArea, "HomeSecondaryGlossRow");
            AssertHiddenIfPresent(safeArea, "HomeSecondaryGlow");
            Assert.That(Find(safeArea, "HomeTipMascot"), Is.Not.Null);

            AssertLocalizedHome(safeArea, l10nType);
            setLanguage.Invoke(null, new[] { greek });
            yield return null;
            AssertLocalizedHome(safeArea, l10nType);

            // Invoke the same preserved UnityEvents again after ownership.
            playButton.onClick.Invoke();
            Assert.That(playPanel.activeSelf, Is.True, "Solo must open PanelPlay.");
            menu.SendMessage("BackToMenu", SendMessageOptions.RequireReceiver);

            int rootId = mainMenuRoot.GetInstanceID();
            int safeAreaId = safeArea.GetInstanceID();
            styledButtons[1].onClick.Invoke();
            Assert.That(settingsPanel.activeSelf, Is.True, "Settings must open PanelSettings.");
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That((bool)ownerType.GetProperty("OwnsHome").GetValue(owner, null), Is.False);
            AssertOwnerStates(mainCanvas, exactType, reskinType, polishType,
                bindingsType, designType, true);

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
            AssertOwnerStates(mainCanvas, exactType, reskinType, polishType,
                bindingsType, designType, false);

            styledButtons[2].onClick.Invoke();
            Assert.That(pvpMenuPanel.activeSelf, Is.True,
                "Private Room must open the existing PvPMenuPanel.");
            Assert.That(mainMenuRoot.activeInHierarchy, Is.True,
                "Private Room must not deactivate Home.");
            yield return new WaitForSecondsRealtime(0.55f);
            Assert.That((bool)ownerType.GetProperty("OwnsHome").GetValue(owner, null), Is.True);
            AssertOwnerStates(mainCanvas, exactType, reskinType, polishType,
                bindingsType, designType, true);
            Assert.That(Find(pvpMenuPanel.transform, "BoardCreatePlusVector"), Is.Not.Null);
            Assert.That(Find(pvpMenuPanel.transform, "BoardJoinDoorVector"), Is.Not.Null);
            AssertProductionImages(safeArea);
            pvpController.SendMessage("ClosePvpMenu", SendMessageOptions.RequireReceiver);
            yield return new WaitForSecondsRealtime(0.3f);
            AssertOwnerStates(mainCanvas, exactType, reskinType, polishType,
                bindingsType, designType, false);

            styledButtons[3].onClick.Invoke();
            Assert.That(dailyPanel.gameObject.activeSelf, Is.True,
                "Daily Hunt must open the existing DailyHuntPanel.");
            Assert.That(mainMenuRoot.activeInHierarchy, Is.True,
                "Daily Hunt must not deactivate Home.");
            yield return new WaitForSecondsRealtime(0.55f);
            Assert.That((bool)ownerType.GetProperty("OwnsHome").GetValue(owner, null), Is.True);
            AssertOwnerStates(mainCanvas, exactType, reskinType, polishType,
                bindingsType, designType, true);
            AssertAttachmentButtonStyled(dailyPanel, "CloseButton");
            AssertProductionImages(safeArea);
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
            Debug.Log("[MainMenu checkpoint] STACKED CTAs 900x156 @ 90,-90,-270 magenta Daily");
        }
        finally
        {
            setLanguage.Invoke(null, new[] { originalLanguage });
            RestorePlayerPrefs(prefs);
            InvokeInstaller(ownerType);
        }
    }

    static void AssertOwnerStates(Canvas canvas, System.Type exactType,
        System.Type reskinType, System.Type polishType, System.Type bindingsType,
        System.Type designType, bool attachmentEnabled)
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
        Assert.That(reskin.enabled, Is.EqualTo(attachmentEnabled));
        Assert.That(polish.enabled, Is.EqualTo(attachmentEnabled));
        Assert.That(bindings.enabled, Is.EqualTo(attachmentEnabled));

        foreach (var root in canvas.gameObject.scene.GetRootGameObjects())
        {
            foreach (var component in root.GetComponentsInChildren(designType, true))
                Assert.That(((Behaviour)component).enabled, Is.False);
        }
    }

    static void AssertAttachmentButtonStyled(Transform root, string buttonName)
    {
        var found = Find(root, buttonName);
        Assert.That(found, Is.Not.Null);
        var button = found.GetComponent<Button>();
        Assert.That(button, Is.Not.Null);
        var image = button.GetComponent<Image>();
        Assert.That(image, Is.Not.Null);
        Assert.That(image.sprite, Is.Not.Null);
        Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
        Assert.That(button.GetComponent<Outline>(), Is.Not.Null,
            buttonName + " did not receive attachment reskin treatment.");
    }

    static void AssertProductionImages(Transform safeArea)
    {
        foreach (var image in safeArea.GetComponentsInChildren<Image>(true))
        {
            bool legacy = image.name.StartsWith("Board") || image.name.StartsWith("Exact");
            if (legacy)
            {
                Assert.That(image.gameObject.activeInHierarchy, Is.False,
                    image.name + " is legacy Home chrome and must stay hidden.");
                continue;
            }

            if (!image.gameObject.activeInHierarchy) continue;

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
        AssertButtonSprite(safeArea, "DailyHuntButton", "mainmenu_cta_magenta_9s");
        AssertButtonSprite(safeArea, "Buttonsettings", "mainmenu_gear_glossy");
    }

    static void AssertStackedCtaCopy(Transform button, string prefix)
    {
        var tmpType = System.Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
        Assert.That(tmpType, Is.Not.Null);

        var title = Find(button, prefix + "Title");
        var subtitle = Find(button, prefix + "Subtitle");
        var chevron = Find(button, prefix + "Chevron");
        Assert.That(title, Is.Not.Null, prefix + "Title missing on stacked CTA.");
        Assert.That(subtitle, Is.Not.Null, prefix + "Subtitle missing on stacked CTA.");
        Assert.That(chevron, Is.Not.Null, prefix + "Chevron missing on stacked CTA.");

        object titleAlign = title.GetComponent(tmpType).GetType()
            .GetProperty("alignment").GetValue(title.GetComponent(tmpType), null);
        object subtitleAlign = subtitle.GetComponent(tmpType).GetType()
            .GetProperty("alignment").GetValue(subtitle.GetComponent(tmpType), null);
        Assert.That(titleAlign.ToString(), Does.Contain("Left"));
        Assert.That(subtitleAlign.ToString(), Does.Contain("Left"));
        Assert.That(TextValue(chevron.GetComponent(tmpType)), Is.EqualTo("›"));
        Assert.That(chevron.GetComponent(tmpType).GetType()
            .GetProperty("raycastTarget").GetValue(chevron.GetComponent(tmpType), null),
            Is.False);
    }

    static void AssertHiddenIfPresent(Transform root, string name)
    {
        Transform found = Find(root, name);
        if (found != null)
            Assert.That(found.gameObject.activeSelf, Is.False, name + " must stay hidden on stacked Home.");
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
            { "HomeDailyTitle", LocalizedCopy(l10nType, "mainmenu_daily_title") },
            { "HomeDailySubtitle", LocalizedCopy(l10nType, "mainmenu_daily_subtitle") },
            { "HomeTipTitle", LocalizedCopy(l10nType, "hud_tip").ToUpperInvariant() + ":" },
            { "HomeTipBody", LocalizedCopy(l10nType, "mainmenu_tip_body") }
        };

        var tmpType = System.Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
        var liveTexts = new List<Component>();
        foreach (var component in safeArea.GetComponentsInChildren(tmpType, true))
        {
            var text = component as Component;
            if (text == null || !text.gameObject.activeInHierarchy) continue;
            if (text.name.EndsWith("Chevron")) continue;
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
                  LocalizedCopy(l10nType, "mainmenu_daily_title"));
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

    static readonly string[] SnapshotIntKeys =
    {
        "DailyHuntDay",
        "DailyHuntUsed",
        "DailyHuntDone",
        "DailyHuntFound",
        "DailyHuntRevived",
        "DailyHuntMin",
        "DailyHuntMax",
        "DailyHuntStreak",
        "DailyHuntLastFound",
        "DailyHuntPendingRevive",
        "PendingRewardEarned"
    };

    static readonly string[] SnapshotStringKeys =
    {
        "PlayerName",
        "DailyHuntTrail"
    };

    struct PrefSnapshot
    {
        public bool had;
        public bool isString;
        public string key;
        public string stringValue;
        public int intValue;
    }

    static PrefSnapshot[] SnapshotPlayerPrefs()
    {
        var snapshots = new PrefSnapshot[SnapshotIntKeys.Length + SnapshotStringKeys.Length];
        int index = 0;
        foreach (string key in SnapshotIntKeys)
        {
            snapshots[index++] = new PrefSnapshot
            {
                key = key,
                had = PlayerPrefs.HasKey(key),
                isString = false,
                intValue = PlayerPrefs.GetInt(key, 0)
            };
        }
        foreach (string key in SnapshotStringKeys)
        {
            snapshots[index++] = new PrefSnapshot
            {
                key = key,
                had = PlayerPrefs.HasKey(key),
                isString = true,
                stringValue = PlayerPrefs.GetString(key, "")
            };
        }
        return snapshots;
    }

    static void RestorePlayerPrefs(PrefSnapshot[] snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            if (!snapshot.had)
                PlayerPrefs.DeleteKey(snapshot.key);
            else if (snapshot.isString)
                PlayerPrefs.SetString(snapshot.key, snapshot.stringValue);
            else
                PlayerPrefs.SetInt(snapshot.key, snapshot.intValue);
        }
        PlayerPrefs.Save();
    }

    static void InvokeInstaller(System.Type type)
    {
        var install = type.GetMethod("Install", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(install, Is.Not.Null, "Missing runtime installer on " + type.Name);
        install.Invoke(null, null);
    }

    static void UnsubscribeSceneLoaded(System.Type type)
    {
        var callback = type.GetMethod("OnSceneLoaded",
            BindingFlags.Static | BindingFlags.NonPublic);
        var sceneLoaded = typeof(SceneManager).GetEvent("sceneLoaded",
            BindingFlags.Static | BindingFlags.Public);
        Assert.That(callback, Is.Not.Null, "Missing sceneLoaded callback on " + type.Name);
        Assert.That(sceneLoaded, Is.Not.Null);
        var handler = System.Delegate.CreateDelegate(sceneLoaded.EventHandlerType, callback);
        sceneLoaded.RemoveEventHandler(null, handler);
    }

    static System.Type RuntimeType(string name)
    {
        var type = System.Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "Missing runtime component: " + name);
        return type;
    }
}
