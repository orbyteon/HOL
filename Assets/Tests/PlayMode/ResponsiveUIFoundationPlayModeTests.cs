using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
        Transform result = Find(pvpMatch.transform, "ResultVisualRoot");
        Transform terminal = Find(pvpMatch.transform, "PvpTerminalRoot");
        Assert.That(result, Is.Not.Null);
        Assert.That(terminal, Is.Not.Null);
        // Final PvP construction owns measured safe roots, not the retired
        // generic page-layout hierarchy. Keep every semantic region under the
        // same viewport/safe-area containment gate using the real final names.
        var pvpSafeTargets = new Dictionary<Transform, string[]>
        {
            { Find(pvpMenu.transform, "PrivateRoomSafeRoot"), new[] {
                "CreateButton", "JoinButton", "PrivateRoomTipCard" } },
            { Find(pvpCreate.transform, "PvPCreatePanelVisualsSafeRoot"), new[] {
                "YouCard", "OpponentCard", "PrebattleBoard", "CancelButton", "ConfirmCreateButton", "SecretInput", "RoomCodeFrame" } },
            { Find(pvpJoin.transform, "PvPJoinPanelVisualsSafeRoot"), new[] {
                "YouCard", "OpponentCard", "PrebattleBoard", "CancelButton", "ConfirmJoinButton", "CodeInput", "SecretInput" } },
            { Find(pvpMatch.transform, "PvpDuelCartoonRootSafeRoot"), new[] {
                "PvpPlayerCard", "PvpOpponentCard", "PvpPromptRibbon", "PvpInteractionCard",
                "PvpSignalBubble", "PvpHistoryCard", "PvpTipCard", "LeaveButton" } },
            { Find(result, "PvpResultCartoonRootSafeRoot"), new[] {
                "PvpResultHero", "PvpResultOpponentCard", "PvpResultStatsCard",
                "PvpResultActions", "ResultRematchStatus", "ResultSignals" } },
            { Find(terminal, "PvpTerminalVisualsSafeRoot"), new[] { "TerminalCard" } }
        };
        foreach (var pair in pvpSafeTargets)
        {
            AssertSingleSafeOwner(pair.Key);
            Assert.That(pair.Key.GetComponent(RuntimeType("ResponsivePageLayout")), Is.Null,
                pair.Key.name + " must not have a competing generic page writer.");
        }

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
                foreach (var pair in pvpSafeTargets)
                    AssertPvpSafeRoot(pair.Key, viewport, safe, canvasSize, pair.Value);
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
            AssertCompleteVisibleCopy(label, english);
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
            // MainMenuPlayVisuals authors Truncate; the language event's final
            // ResponsiveTextPolicy pass selects Ellipsis. This is a safety mode,
            // not permission to omit any of the approved EN/EL title glyphs.
            Assert.That(label.overflowMode, Is.EqualTo(TextOverflowModes.Ellipsis));
            AssertCompleteVisibleCopy(label, Localized("play_hub_solo_title"));

            safe.gameObject.SetActive(false);
            int disabledCount = Property<int>(owner, "RecalculationCount");
            SetLanguage("English");
            Assert.That(Property<int>(owner, "RecalculationCount"),
                Is.EqualTo(disabledCount),
                "Disabled owners must not retain language-event subscriptions.");

            safe.gameObject.SetActive(true);
            yield return null;
            Assert.That(label.text, Is.EqualTo(english));
            AssertCompleteVisibleCopy(label, english);
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
        // Select the returning-player branch explicitly. A fresh CI machine
        // legitimately opens onboarding; unrelated tests must not decide which
        // presentation this Splash-only contract sees.
        bool hadVersion = PlayerPrefs.HasKey("HOL.Onboarding.Version");
        int version = PlayerPrefs.GetInt("HOL.Onboarding.Version", 0);
        try
        {
            PlayerPrefs.SetInt("HOL.Onboarding.Version", 1);
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
        finally
        {
            if (hadVersion) PlayerPrefs.SetInt("HOL.Onboarding.Version", version);
            else PlayerPrefs.DeleteKey("HOL.Onboarding.Version");
        }
    }

    static void AssertCompleteVisibleCopy(TMP_Text label, string expected)
    {
        Assert.That(label.isActiveAndEnabled, Is.True, expected);
        Assert.That(label.color.a, Is.GreaterThan(0f), expected);
        Canvas.ForceUpdateCanvases();
        label.ForceMeshUpdate();
        Assert.That(label.isTextOverflowing, Is.False, expected);
        Assert.That(label.isTextTruncated, Is.False, expected);
        Assert.That(label.textInfo.characterCount, Is.EqualTo(expected.Length), expected);
        for (int i = 0; i < expected.Length; i++)
        {
            TMP_CharacterInfo glyph = label.textInfo.characterInfo[i];
            Assert.That(glyph.character, Is.EqualTo(expected[i]), expected + " glyph " + i);
            if (!char.IsWhiteSpace(expected[i]))
                Assert.That(glyph.isVisible, Is.True, expected + " glyph " + i);
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
        if (root.name == "HomeSafeAreaRoot" || root.name == "PlaySafeAreaRoot")
        {
            var presentation = root.GetComponentInParent(RuntimeType(
                root.name == "HomeSafeAreaRoot" ? "MainMenuHomeVisuals" : "MainMenuPlayVisuals"));
            presentation.GetType().GetMethod("ApplyResponsiveLayoutForViewport", InstanceFlags)
                .Invoke(presentation, new object[] { (int)viewport.x, (int)viewport.y, true });
        }
        float scale = ((RectTransform)root).localScale.x;
        foreach (string childName in childNames)
        {
            var child = Find(root, childName) as RectTransform;
            Assert.That(child, Is.Not.Null, root.name + " missing " + childName);
            // Titles may belong to a ribbon inside the safe root. Measure the
            // full parent chain, not a nested anchoredPosition as root-local.
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            Vector2 minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var corner in corners)
            {
                Vector2 local = root.InverseTransformPoint(corner);
                Vector2 point = safeRect.center + local * scale;
                minimum = Vector2.Min(minimum, point);
                maximum = Vector2.Max(maximum, point);
            }
            AssertContained(safeRect, Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y),
                viewport + " / " + childName);
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

    static void AssertPvpSafeRoot(Transform root, Vector2 viewport, Rect safePixels,
        Vector2 canvasSize, string[] childNames)
    {
        // Invoke the sole production layout owner for the requested viewport;
        // do not retain the Editor's prior tall layout while testing 720x1280.
        if (root.name == "PvpDuelCartoonRootSafeRoot")
        {
            Component matchOwner = root.GetComponentInParent(RuntimeType("PvpDuelCartoonVisuals"));
            Assert.That(matchOwner, Is.Not.Null);
            matchOwner.GetType().GetMethod("ApplyResponsiveLayoutForViewport", InstanceFlags)
                .Invoke(matchOwner, new object[] { safePixels.width, safePixels.height });
        }
        Type ownerType = RuntimeType("ResponsiveSafeAreaRoot");
        Component owner = root.GetComponent(ownerType);
        ownerType.GetMethod("ApplyViewport", InstanceFlags).Invoke(owner, new object[]
        {
            new Rect(Vector2.zero, viewport), safePixels, canvasSize
        });
        Rect safeRect = Property<Rect>(owner, "LastSafeRect");
        var privateRoom = root.GetComponentInParent(RuntimeType("PrivateRoomVisuals"));
        if (privateRoom != null && (root.name == "PrivateRoomSafeRoot" ||
            root.name == "PvPCreatePanelVisualsSafeRoot" || root.name == "PvPJoinPanelVisualsSafeRoot"))
        {
            privateRoom.GetType().GetMethod("ApplyResponsiveLayout", InstanceFlags)
                .Invoke(privateRoom, null);
        }
        Vector2 scale = ((RectTransform)root).localScale;
        foreach (string name in childNames)
        {
            var child = Find(root, name) as RectTransform;
            Assert.That(child, Is.Not.Null, root.name + " missing " + name);
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            if (name == "PvpInteractionCard")
            {
                var image = child.GetComponent<Image>();
                Assert.That(image.sprite, Is.SameAs(Resources.Load<Sprite>("solo/production/solo_interaction_board_v2")));
                Assert.That(image.type, Is.EqualTo(Image.Type.Simple));
                Assert.That(image.preserveAspect, Is.False);
                // This exact Solo PNG has transparent overscan. Test every
                // nonzero-alpha pixel, not its invisible rectangular padding.
                Rect visible = VisiblePngBounds(image.sprite);
                Rect rect = child.rect;
                var local = new Rect(rect.xMin + visible.xMin * rect.width,
                    rect.yMin + visible.yMin * rect.height,
                    visible.width * rect.width, visible.height * rect.height);
                corners = new[] {
                    child.TransformPoint(new Vector3(local.xMin, local.yMin)),
                    child.TransformPoint(new Vector3(local.xMin, local.yMax)),
                    child.TransformPoint(new Vector3(local.xMax, local.yMax)),
                    child.TransformPoint(new Vector3(local.xMax, local.yMin)) };
            }
            Vector2 minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (Vector3 corner in corners)
            {
                // Unlike top-level cards, text/actions may be nested. Include
                // every parent transform instead of treating anchoredPosition
                // as if it were relative to the safe root.
                Vector2 local = root.InverseTransformPoint(corner);
                Vector2 point = safeRect.center + Vector2.Scale(local, scale);
                minimum = Vector2.Min(minimum, point);
                maximum = Vector2.Max(maximum, point);
            }
            AssertContained(safeRect, Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y),
                viewport + " / " + name);
        }
    }

    static readonly Dictionary<Sprite, Rect> VisiblePngBoundsCache = new Dictionary<Sprite, Rect>();
    static Rect VisiblePngBounds(Sprite sprite)
    {
        Rect bounds;
        if (VisiblePngBoundsCache.TryGetValue(sprite, out bounds)) return bounds;
#if UNITY_EDITOR
        string path = UnityEditor.AssetDatabase.GetAssetPath(sprite);
        Assert.That(path.EndsWith(".png", StringComparison.OrdinalIgnoreCase), Is.True);
        var texture = new Texture2D(2, 2);
        try
        {
            Assert.That(texture.LoadImage(System.IO.File.ReadAllBytes(path)), Is.True);
            var pixels = texture.GetPixels32();
            int left = texture.width, bottom = texture.height, right = -1, top = -1;
            for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < texture.width; x++)
                if (pixels[y * texture.width + x].a != 0)
                {
                    left = Mathf.Min(left, x); right = Mathf.Max(right, x);
                    bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y);
                }
            Assert.That(right, Is.GreaterThanOrEqualTo(left), "Required artwork cannot be empty.");
            bounds = Rect.MinMaxRect((float)left / texture.width, (float)bottom / texture.height,
                (float)(right + 1) / texture.width, (float)(top + 1) / texture.height);
            VisiblePngBoundsCache.Add(sprite, bounds);
            return bounds;
        }
        finally { UnityEngine.Object.DestroyImmediate(texture); }
#else
        throw new InvalidOperationException("Exact source-PNG alpha containment requires the Editor test lane.");
#endif
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
