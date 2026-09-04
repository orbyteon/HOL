using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class ResponsiveUIFoundationPlayModeTests
{
    const BindingFlags StaticFlags =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [UnityTest]
    public IEnumerator LivePagesShareTheViewportContractAcrossTheRequiredMatrix()
    {
        Install("MainMenuHomeVisuals");
        Install("MainMenuPlayVisuals");
        Install("SettingsVisuals");
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        for (int i = 0; i < 24; i++) yield return null;
        yield return new WaitForSecondsRealtime(0.35f);

        var gameManager = FindInScene(RuntimeType("GameManager"));
        if (gameManager != null) ((MonoBehaviour)gameManager).CancelInvoke();
        var menu = FindInScene(RuntimeType("MenuManager"));
        var matchmaking = FindInScene(RuntimeType("FakeMatchmaking"));
        Assert.That(menu, Is.Not.Null);
        Assert.That(matchmaking, Is.Not.Null);

        var panelGame = (GameObject)Field(matchmaking, "panelGame");
        panelGame.SetActive(true);
        for (int i = 0; i < 5; i++) yield return null;
        if (gameManager != null) ((MonoBehaviour)gameManager).CancelInvoke();

        var canvas = GameObject.Find("Canvas");
        Assert.That(canvas, Is.Not.Null);
        Transform homeSafe = Find(canvas.transform, "HomeSafeAreaRoot");
        Transform playSafe = Find(canvas.transform, "PlaySafeAreaRoot");
        AssertSingleSafeOwner(homeSafe);
        AssertSingleSafeOwner(playSafe);

        var targets = new List<RectTransform>();
        Component soloOwner = FindInScene(RuntimeType("SoloDuelVisuals"));
        Assert.That(soloOwner, Is.Not.Null);
        Transform soloSafe = Find(panelGame.transform, "SoloDuelSafeRoot");
        Assert.That(soloSafe, Is.Not.Null);
        Assert.That(soloSafe.GetComponent(RuntimeType("ResponsiveSafeAreaRoot")),
            Is.Not.Null);
        Assert.That(soloSafe.GetComponent(RuntimeType("ResponsivePageLayout")),
            Is.Null,
            "SoloDuelVisuals owns its measured composition; a generic second writer is forbidden.");

        var settingsPanel = (GameObject)Field(menu, "settingsPanel");
        AddTargets(targets, settingsPanel.transform,
            "EnglishButton", "GreekButton", "Difficulty0",
            "Difficulty3", "AdsPrivacyButton");

        // Daily Hunt intentionally has one screen-owned responsive writer and
        // its own EN/EL portrait matrix in DailyHuntCartoonVisualsPlayModeTests.
        // It must not be forced back through the generic ResponsivePageLayout
        // hierarchy or its retired pre-production generic-card contract.

        var pvp = FindInScene(RuntimeType("PvpGameController"));
        Assert.That(pvp, Is.Not.Null);
        var pvpMenu = (GameObject)Field(pvp, "pvpMenuPanel");
        var pvpCreate = (GameObject)Field(pvp, "createPanel");
        var pvpJoin = (GameObject)Field(pvp, "joinPanel");
        var pvpMatch = (GameObject)Field(pvp, "matchPanel");
        AddTargets(targets, pvpMenu.transform,
            "CreateButton", "JoinButton", "PrivateRoomTipCard");
        AddTargets(targets, pvpCreate.transform,
            "YouCard", "OpponentCard", "RuleCard", "CancelButton",
            "ConfirmCreateButton");
        AddTargets(targets, pvpJoin.transform,
            "YouCard", "OpponentCard", "RuleCard", "CancelButton",
            "ConfirmJoinButton");
        AddTargets(targets, pvpMatch.transform,
            "PlayerCard", "OpponentCard", "PromptBanner", "GuessCard",
            "SignalBubble", "HistoryCard", "TipCard", "LeaveButton");
        Transform result = Find(pvpMatch.transform, "ResultVisualRoot");
        Transform terminal = Find(pvpMatch.transform, "PvpTerminalRoot");
        Assert.That(result, Is.Not.Null);
        Assert.That(terminal, Is.Not.Null);
        AddTargets(targets, result,
            "ResultPopTarget", "RematchCard", "ResultRematchStatus", "ReactionCard");
        AddTargets(targets, terminal, "TerminalCard");

        Type layoutType = RuntimeType("ResponsivePageLayout");
        var owners = new Dictionary<Component, List<RectTransform>>();
        foreach (RectTransform target in targets)
        {
            Component owner = FindOwner(target, layoutType);
            Assert.That(owner, Is.Not.Null,
                target.name + " has no responsive page owner.");
            if (!owners.ContainsKey(owner)) owners.Add(owner, new List<RectTransform>());
            owners[owner].Add(target);
        }

        Vector2[] viewports =
        {
            new Vector2(720f, 1280f),
            new Vector2(1080f, 1920f),
            new Vector2(1080f, 2400f),
            new Vector2(1179f, 2556f)
        };
        MethodInfo apply = layoutType.GetMethod("ApplyViewport", InstanceFlags);
        foreach (Vector2 viewport in viewports)
        {
            Rect[] safeAreas =
            {
                new Rect(0f, 0f, viewport.x, viewport.y),
                new Rect(0f, 0f, viewport.x, viewport.y * 0.92f),
                new Rect(0f, viewport.y * 0.05f, viewport.x, viewport.y * 0.87f)
            };
            Vector2 canvasSize = CanvasSize(viewport);
            foreach (Rect safe in safeAreas)
            {
                foreach (var pair in owners)
                {
                    apply.Invoke(pair.Key, new object[]
                    {
                        new Rect(Vector2.zero, viewport), safe, canvasSize
                    });
                    Rect safeRect = Property<Rect>(pair.Key, "LastSafeRect");
                    foreach (RectTransform target in pair.Value)
                        AssertContained(safeRect, RectFor(target),
                            viewport + " / " + target.name);
                }
                AssertSafeRoot(homeSafe, viewport, safe, canvasSize,
                    "Buttonsettings", "ButtonPlay", "DailyHuntButton",
                    "HomeSpeechBubble", "HomeDailyPromo");
                AssertSafeRoot(playSafe, viewport, safe, canvasSize,
                    "ButtonChallenger", "ButtonPvP", "ButtonBack",
                    "PlayHubTitle", "PlayHubSubtitle");
            }
        }

        // Reapplying the same geometry must derive the exact same output.
        Component firstOwner = FindOwner(targets[0], layoutType);
        RectTransform firstTarget = targets[0];
        Vector2 beforePosition = firstTarget.anchoredPosition;
        Vector3 beforeScale = firstTarget.localScale;
        Vector2 lastViewport = viewports[viewports.Length - 1];
        Rect lastSafe = new Rect(0f, lastViewport.y * 0.05f,
            lastViewport.x, lastViewport.y * 0.87f);
        apply.Invoke(firstOwner, new object[]
        {
            new Rect(Vector2.zero, lastViewport), lastSafe, CanvasSize(lastViewport)
        });
        Assert.That(firstTarget.anchoredPosition, Is.EqualTo(beforePosition));
        Assert.That(firstTarget.localScale, Is.EqualTo(beforeScale));
    }

    [UnityTest]
    public IEnumerator LanguageAndEnableLifecycleRepaintWithoutGeometryOrSubscriptionLeak()
    {
        bool hadLanguage = PlayerPrefs.HasKey("Language");
        int savedLanguage = PlayerPrefs.GetInt("Language", 0);
        Install("MainMenuHomeVisuals");
        Install("MainMenuPlayVisuals");
        try
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            for (int i = 0; i < 20; i++) yield return null;
            yield return new WaitForSecondsRealtime(0.35f);

            var menu = FindInScene(RuntimeType("MenuManager"));
            GameObject panelPlay = (GameObject)Field(menu, "panelPlay");
            menu.GetType().GetMethod("OnPlayPressed", InstanceFlags)
                .Invoke(menu, null);
            yield return null;

            Transform safe = Find(panelPlay.transform, "PlaySafeAreaRoot");
            Transform button = Find(safe, "ButtonChallenger");
            Assert.That(safe, Is.Not.Null);
            Assert.That(button, Is.Not.Null);
            var label = Find(button, "PlaySoloTitle").GetComponent<TMP_Text>();
            Type ownerType = RuntimeType("ResponsiveSafeAreaRoot");
            Component owner = safe.GetComponent(ownerType);
            Assert.That(owner, Is.Not.Null);

            SetLanguage("English");
            string english = Localized("play_hub_solo_title");
            Assert.That(label.text, Is.EqualTo(english));
            Vector2 position = ((RectTransform)button).anchoredPosition;
            int childCount = safe.childCount;
            int enabledCount = Property<int>(owner, "RecalculationCount");

            SetLanguage("Greek");
            Assert.That(label.text, Is.EqualTo(Localized("play_hub_solo_title")));
            Assert.That(((RectTransform)button).anchoredPosition, Is.EqualTo(position));
            Assert.That(safe.childCount, Is.EqualTo(childCount));
            Assert.That(Property<int>(owner, "RecalculationCount"),
                Is.GreaterThan(enabledCount));
            Assert.That(label.enableAutoSizing, Is.True);
            Assert.That(label.fontSizeMin, Is.GreaterThanOrEqualTo(18f));
            Assert.That(label.overflowMode, Is.EqualTo(TextOverflowModes.Overflow));

            safe.gameObject.SetActive(false);
            int disabledCount = Property<int>(owner, "RecalculationCount");
            SetLanguage("English");
            Assert.That(Property<int>(owner, "RecalculationCount"),
                Is.EqualTo(disabledCount),
                "Disabled owners must not retain language-event subscriptions.");

            safe.gameObject.SetActive(true);
            yield return null;
            Assert.That(label.text, Is.EqualTo(english));
            Assert.That(((RectTransform)button).anchoredPosition, Is.EqualTo(position));
            Assert.That(safe.childCount, Is.EqualTo(childCount));
            Assert.That(Property<int>(owner, "RecalculationCount"),
                Is.GreaterThan(disabledCount));
        }
        finally
        {
            SetLanguage(savedLanguage == 1 ? "Greek" : "English");
            if (!hadLanguage) PlayerPrefs.DeleteKey("Language");
        }
    }

    [UnityTest]
    public IEnumerator SplashUsesTheSameSafeRootContractForEveryViewport()
    {
        yield return SceneManager.LoadSceneAsync("SplashScene", LoadSceneMode.Single);
        var loader = FindInScene(RuntimeType("SplashLoader"));
        if (loader != null) ((MonoBehaviour)loader).CancelInvoke();
        yield return null;

        Transform safe = Find(SceneManager.GetActiveScene(), "SplashSafeAreaRoot");
        AssertSingleSafeOwner(safe);
        Vector2[] viewports =
        {
            new Vector2(720f, 1280f), new Vector2(1080f, 1920f),
            new Vector2(1080f, 2400f), new Vector2(1440f, 3200f)
        };
        foreach (Vector2 viewport in viewports)
        {
            Rect safePixels = new Rect(0f, viewport.y * 0.05f,
                viewport.x, viewport.y * 0.87f);
            AssertSafeRoot(safe, viewport, safePixels, CanvasSize(viewport),
                "SplashLogo", "SplashHeroBoy", "SplashHeroGirl",
                "SplashProgressTrack");
        }
    }

    static void AssertSingleSafeOwner(Transform root)
    {
        Assert.That(root, Is.Not.Null);
        Type ownerType = RuntimeType("ResponsiveSafeAreaRoot");
        Assert.That(root.GetComponents(ownerType), Has.Length.EqualTo(1));
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == root) continue;
            Assert.That(child.GetComponent(ownerType), Is.Null,
                root.name + " contains nested safe-area owner " + child.name);
        }
    }

    static void AssertSafeRoot(Transform root, Vector2 viewport, Rect safePixels,
        Vector2 canvasSize, params string[] childNames)
    {
        Type ownerType = RuntimeType("ResponsiveSafeAreaRoot");
        Component owner = root.GetComponent(ownerType);
        ownerType.GetMethod("ApplyViewport", InstanceFlags).Invoke(owner, new object[]
        {
            new Rect(Vector2.zero, viewport), safePixels, canvasSize
        });
        Rect safeRect = Property<Rect>(owner, "LastSafeRect");
        float scale = ((RectTransform)root).localScale.x;
        foreach (string childName in childNames)
        {
            var child = Find(root, childName) as RectTransform;
            Assert.That(child, Is.Not.Null, root.name + " missing " + childName);
            Vector2 size = child.sizeDelta * scale;
            Rect bounds = new Rect(
                safeRect.center + child.anchoredPosition * scale - size * 0.5f,
                size);
            AssertContained(safeRect, bounds, viewport + " / " + childName);
        }
    }

    static void AddTargets(List<RectTransform> targets, Transform root,
        params string[] names)
    {
        foreach (string name in names)
        {
            var found = Find(root, name) as RectTransform;
            Assert.That(found, Is.Not.Null, root.name + " missing " + name);
            targets.Add(found);
        }
    }

    static Component FindOwner(RectTransform target, Type layoutType)
    {
        Transform current = target.parent;
        while (current != null)
        {
            var owner = current.GetComponent(layoutType);
            if (owner != null) return owner;
            current = current.parent;
        }
        return null;
    }

    static Rect RectFor(RectTransform rect)
    {
        Vector2 size = Vector2.Scale(rect.sizeDelta,
            new Vector2(Mathf.Abs(rect.localScale.x), Mathf.Abs(rect.localScale.y)));
        return new Rect(rect.anchoredPosition - size * 0.5f, size);
    }

    static void AssertContained(Rect safe, Rect bounds, string context)
    {
        const float tolerance = 0.05f;
        Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(safe.xMin - tolerance), context);
        Assert.That(bounds.xMax, Is.LessThanOrEqualTo(safe.xMax + tolerance), context);
        Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(safe.yMin - tolerance), context);
        Assert.That(bounds.yMax, Is.LessThanOrEqualTo(safe.yMax + tolerance), context);
    }

    static Vector2 CanvasSize(Vector2 viewport)
    {
        return (Vector2)RuntimeType("ResponsiveViewportGeometry")
            .GetMethod("CanvasSizeForViewport", StaticFlags)
            .Invoke(null, new object[] { viewport, new Vector2(1080f, 1920f), 0.5f });
    }

    static void SetLanguage(string language)
    {
        Type l10n = RuntimeType("L10n");
        Type enumType = l10n.GetNestedType("Language", BindingFlags.Public);
        l10n.GetMethod("SetLanguage", StaticFlags).Invoke(null,
            new[] { Enum.Parse(enumType, language) });
    }

    static string Localized(string key)
    {
        return (string)RuntimeType("L10n").GetMethod("Get", StaticFlags)
            .Invoke(null, new object[] { key, new object[0] });
    }

    static void Install(string typeName)
    {
        MethodInfo install = RuntimeType(typeName).GetMethod("Install", StaticFlags);
        Assert.That(install, Is.Not.Null, "Missing installer for " + typeName);
        install.Invoke(null, null);
    }

    static Component FindInScene(Type type)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var found = root.GetComponentInChildren(type, true);
            if (found != null) return found;
        }
        return null;
    }

    static Transform Find(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = Find(root.transform, name);
            if (found != null) return found;
        }
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

    static object Field(Component target, string name)
    {
        return target.GetType().GetField(name, InstanceFlags).GetValue(target);
    }

    static T Property<T>(Component target, string name)
    {
        return (T)target.GetType().GetProperty(name, InstanceFlags).GetValue(target, null);
    }

    static Type RuntimeType(string name)
    {
        var type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "Missing runtime type " + name);
        return type;
    }
}
