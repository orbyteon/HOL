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
        yield return null;
        Canvas.ForceUpdateCanvases();
        Assert.That(hunt.gameObject.activeInHierarchy, Is.True);

        Transform root = Find(hunt.transform, "DailyHuntVisualRoot");
        Assert.That(root, Is.Not.Null);
        Assert.That(Find(hunt.transform, "Card"), Is.Null,
            "Daily Hunt must not build a legacy presentation before its owner.");
        Assert.That(hunt.transform.childCount, Is.EqualTo(1),
            "DailyHuntVisuals must construct the only runtime hierarchy.");

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
            "DailyPlayerProgressFillTrack",
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

        // Human-approved 1080x1920 production geometry. These assertions
        // deliberately lock the internal component composition as well as the
        // outer containers so a later pass cannot regress the player-chip XP
        // readability or compress the mission/reward hierarchy while keeping
        // only the parent rectangles unchanged.
        AssertRect(root, "DailyPlayerChipShell",
            new Vector2(-3f, -9f), new Vector2(336f, 184f));
        AssertRect(root, "DailyPlayerAvatarRing",
            new Vector2(-120f, 5f), new Vector2(122f, 122f));
        AssertRect(root, "DailyPlayerAvatarClip",
            new Vector2(-128f, 20f), new Vector2(118f, 118f));
        AssertRect(root, "DailyPlayerName",
            new Vector2(30f, 53f), new Vector2(170f, 40f));
        AssertRect(root, "DailyPlayerStar",
            new Vector2(-9f, -4f), new Vector2(30f, 30f));
        AssertRect(root, "DailyPlayerWins",
            new Vector2(58f, 3f), new Vector2(120f, 38f));
        AssertRect(root, "DailyPlayerXpTrack",
            new Vector2(48f, -20f), new Vector2(150f, 24f));
        AssertRect(root, "DailyPlayerProgressFillTrack",
            new Vector2(-10f, -66f), new Vector2(270f, 34f));
        AssertRect(root, "DailyPlayerProgress",
            new Vector2(45f, -71f), new Vector2(176f, 36f));

        AssertRect(root, "DailyMissionCalendar",
            new Vector2(-290f, 7f), new Vector2(465f, 565f));
        AssertRect(root, "DailyMissionHeading",
            new Vector2(165f, 291f), new Vector2(470f, 106f));
        AssertRect(root, "DailyMissionRow1",
            new Vector2(190f, 160f), new Vector2(610f, 205f));
        AssertRect(root, "DailyMissionRow2",
            new Vector2(190f, 2f), new Vector2(610f, 205f));
        AssertRect(root, "DailyMissionRow3",
            new Vector2(190f, -161f), new Vector2(610f, 205f));
        AssertRect(root, "DailyMissionCompletion",
            new Vector2(50f, -294f), new Vector2(800f, 62f));
        AssertRect(root, "DailyMissionRewardChest",
            new Vector2(-271f, -16f), new Vector2(405f, 287f));
        AssertRect(root, "DailyMissionRewardHeading",
            new Vector2(176f, 123f), new Vector2(520f, 70f));
        AssertRect(root, "DailyMissionClock",
            new Vector2(0f, 8f), new Vector2(88f, 88f));
        AssertRect(root, "DailyMissionResetLabel",
            new Vector2(150f, 52f), new Vector2(330f, 44f));
        AssertRect(root, "DailyMissionReset",
            new Vector2(140f, 0f), new Vector2(330f, 56f));
        AssertRect(root, "DailyMissionRewardTrophy",
            new Vector2(40f, -110f), new Vector2(125f, 125f));
        AssertRect(root, "DailyMissionRewardAmount",
            new Vector2(178f, -110f), new Vector2(350f, 104f));
        AssertRect(root, "DailyMissionPortal",
            new Vector2(0f, -860f), new Vector2(1110f, 205f));

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
            "dailyhunt/production/fonts/HOL Daily Display SDF");
        TMP_FontAsset bodyFont = Resources.Load<TMP_FontAsset>(
            "dailyhunt/production/fonts/HOL Daily Body SDF");
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
                yield return null;
            Canvas.ForceUpdateCanvases();

            foreach (string language in new[] { "English", "Greek" })
            {
                SetLanguage(language);
                for (int frame = 0; frame < 2; frame++)
                    yield return null;
                Canvas.ForceUpdateCanvases();
                AssertResponsiveViewport(root, viewport, language);
            }
        }

        SetLanguage("English");
        Screen.SetResolution(1080, 1920, false);
        for (int frame = 0; frame < 3; frame++)
            yield return null;
        Canvas.ForceUpdateCanvases();

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
        Assert.That(visibleTrail, Does.Match("[↑↓•]"),
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
        AssertApprovedResponsiveGeometry(root, viewport);

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

    static void AssertApprovedResponsiveGeometry(
        Transform root,
        Vector2Int viewport)
    {
        float aspect = viewport.x > 0
            ? Mathf.Max(1, viewport.y) / (float)viewport.x
            : 1920f / 1080f;
        float tall = Mathf.InverseLerp(1.78f, 2.22f, aspect);

        AssertRect(root, "CloseButton",
            new Vector2(-435f, 836f + 165f * tall),
            new Vector2(155f, 155f));
        AssertRect(root, "DailyPlayerChip",
            new Vector2(335f, 827f + 165f * tall),
            new Vector2(365f, 194f));
        AssertRect(root, "DailyLogo",
            new Vector2(-10f, 783f + 110f * tall),
            new Vector2(396f, 295f));
        AssertRect(root, "DailyTitleRibbon",
            new Vector2(0f, 585f + 90f * tall),
            new Vector2(1040f, 285f));
        AssertRect(root, "DailyMissionBoard",
            new Vector2(-1f, 119f + 30f * tall),
            new Vector2(1036f, 874f));
        AssertRect(root, "DailyMissionRewardBoard",
            new Vector2(0f, -417f - 65f * tall),
            new Vector2(1060f, 425f));
        AssertRect(root, "DailyMissionPortal",
            new Vector2(0f, -860f - 240f * tall),
            new Vector2(1110f, 205f));
        AssertRect(root, "DailyMissionStartButton",
            new Vector2(0f, -771f - 185f * tall),
            new Vector2(595f, 230f));
        AssertRect(root, "DailyMascotSix",
            new Vector2(-372f, -754f - 165f * tall),
            new Vector2(322f, 375f));
        AssertRect(root, "DailyMascotSeven",
            new Vector2(363f, -748f - 165f * tall),
            new Vector2(326f, 380f));
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
