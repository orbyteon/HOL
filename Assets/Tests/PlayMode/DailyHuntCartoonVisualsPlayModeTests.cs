using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class DailyHuntCartoonVisualsPlayModeTests
{
    static readonly string[] DailyKeys =
    {
        "DailyHuntDay",
        "DailyHuntUsed",
        "DailyHuntTrail",
        "DailyHuntDone",
        "DailyHuntFound",
        "DailyHuntRevived",
        "DailyHuntMin",
        "DailyHuntMax",
        "DailyHuntStreak",
        "DailyHuntLastFound",
        "DailyHuntPendingRevive",
    };

    static readonly Vector2Int[] PortraitViewports =
    {
        new Vector2Int(1080, 1920),
        new Vector2Int(1080, 2400),
        new Vector2Int(1179, 2556),
    };

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SetLanguage("English");
        foreach (string key in DailyKeys)
            PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        SetLanguage("English");
        foreach (string key in DailyKeys)
            PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        yield return null;
    }

    [UnityTest]
    public IEnumerator DailyHuntUsesApprovedCartoonCompositionAndRealGuessFlow()
    {
        Screen.SetResolution(1080, 1920, false);
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);

        Component hunt = null;
        Component visuals = null;
        for (int frame = 0; frame < 180; frame++)
        {
            hunt = FindInScene(RuntimeType("DailyHunt"));
            visuals = FindInScene(RuntimeType("DailyHuntVisuals"));
            if (hunt != null && visuals != null)
                break;
            yield return null;
        }

        Assert.That(hunt, Is.Not.Null);
        Assert.That(visuals, Is.Not.Null);
        Assert.That(GetProperty<bool>(visuals, "IsReady"), Is.True);
        Assert.That(CountInScene(RuntimeType("DailyHuntVisuals")), Is.EqualTo(1));
        Assert.That(
            Type.GetType("DailyHuntVisualFidelityPass, Assembly-CSharp"),
            Is.Null,
            "DailyHuntVisuals must remain the sole Daily Hunt visual/layout owner.");
        Assert.That(
            Type.GetType("DailyHuntVisualFidelityInstaller, Assembly-CSharp"),
            Is.Null,
            "Daily Hunt must not install a second runtime visual/layout writer.");

        Invoke(hunt, "Open");
        yield return new WaitForSecondsRealtime(0.36f);
        yield return new WaitForEndOfFrame();
        Assert.That(hunt.gameObject.activeInHierarchy, Is.True);

        Transform root = Find(hunt.transform, "DailyHuntVisualRoot");
        Assert.That(root, Is.Not.Null);

        foreach (string name in new[]
        {
            "DailyBackground",
            "DailyStars",
            "DailyConfetti",
            "DailyOuterBezelBody",
            "DailyHuntSafeRoot",
            "CloseButton",
            "DailyPlayerChip",
            "DailyPlayerChipShell",
            "DailyPlayerAvatarRing",
            "DailyPlayerAvatar",
            "DailyPlayerStar",
            "DailyPlayerName",
            "DailyPlayerWins",
            "DailyPlayerProgress",
            "DailyPlayerXpTrack",
            "DailyLogo",
            "DailyTitleRibbon",
            "DailyRibbonTitle",
            "DailyMissionDashboard",
            "DailyMissionBoard",
            "DailyMissionCalendar",
            "DailyMissionHeading",
            "DailyMissionRow1",
            "DailyMissionRow2",
            "DailyMissionRow3",
            "DailyMissionCompletion",
            "DailyMissionRewardBoard",
            "DailyMissionRewardArtwork",
            "DailyMissionRewardChest",
            "DailyMissionRewardHeading",
            "DailyMissionReset",
            "DailyMissionRewardAmount",
            "DailyMissionStartButton",
            "DailyMissionPortal",
            "DailyMascotSix",
            "DailyMascotSeven",
        })
        {
            Assert.That(Find(root, name), Is.Not.Null,
                "Missing approved Daily Hunt object: " + name);
        }

        AssertRect(root, "CloseButton",
            new Vector2(-435f, 836f), new Vector2(155f, 155f));
        AssertRect(root, "DailyPlayerChip",
            new Vector2(335f, 827f), new Vector2(365f, 194f));
        AssertRect(root, "DailyLogo",
            new Vector2(-10f, 783f), new Vector2(396f, 295f));
        AssertRect(root, "DailyTitleRibbon",
            new Vector2(0f, 585f), new Vector2(1040f, 285f));
        AssertRect(root, "DailyMissionBoard",
            new Vector2(-1f, 119f), new Vector2(1036f, 874f));
        AssertRect(root, "DailyMissionRewardBoard",
            new Vector2(0f, -417f), new Vector2(1060f, 425f));
        AssertRect(root, "DailyMissionStartButton",
            new Vector2(0f, -771f), new Vector2(595f, 230f));
        AssertRect(root, "DailyMascotSix",
            new Vector2(-372f, -754f), new Vector2(322f, 375f));
        AssertRect(root, "DailyMascotSeven",
            new Vector2(363f, -748f), new Vector2(326f, 380f));

        AssertSprite(root, "DailyMissionCalendar",
            "dailyhunt/production/daily_calendar_target_production");
        AssertSprite(root, "DailyMissionRewardChest",
            "dailyhunt/production/daily_reward_chest_reference_v1");
        AssertSprite(root, "DailyPlayerChipShell",
            "dailyhunt/production/daily_player_chip_shell_v3");
        AssertSprite(root, "DailyPlayerAvatarRing",
            "dailyhunt/production/daily_player_avatar_ring_v1");
        AssertSprite(root, "DailyPlayerXpTrack",
            "dailyhunt/production/daily_player_xp_track_v2");
        AssertSprite(root, "DailyLogo", "reference/hol_logo_exact");
        Assert.That(Resources.Load<Sprite>("cartoon/cartoon_daily_calendar"), Is.Null,
            "The retired code-drawn calendar approximation must not return.");
        Assert.That(Resources.Load<Sprite>("cartoon/cartoon_reward_chest"), Is.Null,
            "The retired code-drawn reward approximation must not return.");

        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            Assert.That(graphic is Image || graphic is TMP_Text, Is.True,
                "Procedural Graphic found in Daily Hunt: " +
                graphic.GetType().Name + " / " + graphic.name);
            if (graphic is Image image && image.sprite != null)
                Assert.That(image.color.a, Is.GreaterThan(0f),
                    image.name + " hides approved production art completely.");
        }

        TMP_FontAsset displayFont = Resources.Load<TMP_FontAsset>(
            "phase2a/fonts/HOL Menu Display SDF");
        TMP_FontAsset bodyFont = Resources.Load<TMP_FontAsset>(
            "phase2a/fonts/HOL Menu Body SDF");
        Assert.That(displayFont, Is.Not.Null);
        Assert.That(bodyFont, Is.Not.Null);
        foreach (string name in new[]
        {
            "DailyPlayerName",
            "DailyPlayerWins",
            "DailyPlayerProgress",
            "DailyRibbonTitle",
            "DailyMissionHeading",
            "DailyMissionRewardHeading",
            "DailyMissionReset",
            "DailyMissionRewardAmount",
        })
        {
            TMP_Text text = Find(root, name).GetComponent<TMP_Text>();
            Assert.That(text.font, Is.SameAs(displayFont),
                name + " must use the approved HOL display font.");
        }
        Assert.That(
            Find(root, "DailyMissionProgress1").GetComponent<TMP_Text>().font,
            Is.SameAs(bodyFont));

        Assert.That(
            Find(root, "DailyRibbonTitle").GetComponent<TMP_Text>().text,
            Is.EqualTo(Localized("daily_challenge_title")));
        Assert.That(
            Find(root, "DailyMissionHeading").GetComponent<TMP_Text>().text,
            Is.EqualTo(Localized("daily_missions_heading")));
        Assert.That(
            Find(root, "DailyMissionRewardHeading").GetComponent<TMP_Text>().text,
            Is.EqualTo(Localized("daily_reward_heading")));

        foreach (Vector2Int viewport in PortraitViewports)
        {
            Screen.SetResolution(viewport.x, viewport.y, false);
            for (int frame = 0; frame < 3; frame++)
                yield return new WaitForEndOfFrame();

            foreach (string language in new[] { "English", "Greek" })
            {
                SetLanguage(language);
                for (int frame = 0; frame < 2; frame++)
                    yield return new WaitForEndOfFrame();
                Canvas.ForceUpdateCanvases();
                AssertResponsiveViewport(root, viewport, language);
            }
        }

        SetLanguage("English");
        Screen.SetResolution(1080, 1920, false);
        for (int frame = 0; frame < 3; frame++)
            yield return new WaitForEndOfFrame();

        Find(root, "DailyMissionStartButton").GetComponent<Button>()
            .onClick.Invoke();
        yield return null;

        TMP_InputField input = Find(root, "GuessInput")
            .GetComponent<TMP_InputField>();
        Button submit = Find(root, "SubmitGuessButton").GetComponent<Button>();
        Button close = Find(root, "CloseButton").GetComponent<Button>();
        Assert.That(input, Is.Not.Null);
        Assert.That(submit, Is.Not.Null);
        Assert.That(close, Is.Not.Null);
        Assert.That(input.gameObject.activeInHierarchy, Is.True);
        Assert.That(submit.gameObject.activeInHierarchy, Is.True);

        int usedBefore = GetField<int>(hunt, "used");
        input.text = "50";
        submit.onClick.Invoke();
        yield return null;
        yield return null;
        int usedAfter = GetField<int>(hunt, "used");
        Assert.That(usedAfter, Is.EqualTo(usedBefore + 1),
            "The restyled Submit control lost the real Daily Hunt callback.");
        Assert.That(Find(root, "Status").GetComponent<TMP_Text>().text,
            Is.Not.Empty);

        string visibleTrail = Find(root, "Trail").GetComponent<TMP_Text>().text;
        Assert.That(visibleTrail, Does.Not.Contain("🎯"));
        Assert.That(visibleTrail, Does.Not.Contain("🔺"));
        Assert.That(visibleTrail, Does.Not.Contain("🔻"));
        Assert.That(visibleTrail, Does.Match("[▲▼●]"),
            "The visible trail must use glyphs covered by the production font chain.");

        close.onClick.Invoke();
        yield return null;
        Assert.That(hunt.gameObject.activeSelf, Is.False,
            "The top-left Back control lost the real Close callback.");
    }

    static void AssertResponsiveViewport(
        Transform root,
        Vector2Int viewport,
        string language)
    {
        foreach (string name in new[]
        {
            "CloseButton",
            "DailyPlayerChip",
            "DailyLogo",
            "DailyTitleRibbon",
            "DailyMissionBoard",
            "DailyMissionRewardBoard",
            "DailyMissionStartButton",
            "DailyMissionPortal",
            "DailyMascotSix",
            "DailyMascotSeven",
        })
        {
            AssertInsideScreen(
                Find(root, name) as RectTransform,
                viewport,
                language + " / " + name);
        }

        foreach (string name in new[]
        {
            "DailyPlayerName",
            "DailyPlayerWins",
            "DailyPlayerProgress",
            "DailyRibbonTitle",
            "DailyMissionHeading",
            "DailyMissionLabel1",
            "DailyMissionLabel2",
            "DailyMissionLabel3",
            "DailyMissionCompletion",
            "DailyMissionRewardHeading",
            "DailyMissionReset",
            "DailyMissionRewardAmount",
        })
        {
            TMP_Text text = Find(root, name).GetComponent<TMP_Text>();
            text.ForceMeshUpdate();
            Assert.That(text.isTextOverflowing, Is.False,
                language + " / " + viewport + " / " + name + " overflowed.");
            Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(20f),
                language + " / " + viewport + " / " + name + " became unreadable.");
        }
    }

    static void AssertInsideScreen(
        RectTransform rect,
        Vector2Int viewport,
        string context)
    {
        Assert.That(rect, Is.Not.Null, context);
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        foreach (Vector3 corner in corners)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, corner);
            Assert.That(screen.x, Is.GreaterThanOrEqualTo(-2f), context + " left clipped.");
            Assert.That(screen.y, Is.GreaterThanOrEqualTo(-2f), context + " bottom clipped.");
            Assert.That(screen.x, Is.LessThanOrEqualTo(viewport.x + 2f), context + " right clipped.");
            Assert.That(screen.y, Is.LessThanOrEqualTo(viewport.y + 2f), context + " top clipped.");
        }
    }

    static void AssertSprite(Transform root, string name, string resource)
    {
        Image image = Find(root, name).GetComponent<Image>();
        Sprite sprite = Resources.Load<Sprite>(resource);
        Assert.That(sprite, Is.Not.Null, resource);
        Assert.That(image.sprite, Is.SameAs(sprite), name);
        Assert.That(image.type, Is.EqualTo(Image.Type.Simple), name);
        Assert.That(image.color.a, Is.EqualTo(1f).Within(0.001f), name);
        Assert.That(image.raycastTarget, Is.False, name);
    }

    static void AssertRect(
        Transform root,
        string name,
        Vector2 position,
        Vector2 size)
    {
        RectTransform rect = Find(root, name) as RectTransform;
        Assert.That(rect, Is.Not.Null, name);
        Assert.That(Vector2.Distance(rect.anchoredPosition, position),
            Is.LessThan(1f), name + " position drifted.");
        Assert.That(Vector2.Distance(rect.sizeDelta, size),
            Is.LessThan(1f), name + " size drifted.");
    }

    static string Localized(string key)
    {
        Type type = RuntimeType("L10n");
        MethodInfo method = type.GetMethod(
            "Get", BindingFlags.Public | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);
        return (string)method.Invoke(null, new object[] { key, new object[0] });
    }

    static void SetLanguage(string name)
    {
        Type type = RuntimeType("L10n");
        Type language = type.GetNestedType("Language", BindingFlags.Public);
        MethodInfo method = type.GetMethod(
            "SetLanguage", BindingFlags.Public | BindingFlags.Static);
        Assert.That(language, Is.Not.Null);
        Assert.That(method, Is.Not.Null);
        method.Invoke(null, new[] { Enum.Parse(language, name) });
    }

    static object Invoke(Component target, string name)
    {
        MethodInfo method = target.GetType().GetMethod(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, name);
        return method.Invoke(target, null);
    }

    static T GetField<T>(Component target, string name)
    {
        FieldInfo field = target.GetType().GetField(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        return (T)field.GetValue(target);
    }

    static T GetProperty<T>(Component target, string name)
    {
        PropertyInfo property = target.GetType().GetProperty(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, name);
        return (T)property.GetValue(target);
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
        Assert.That(type, Is.Not.Null, name);
        return type;
    }
}
