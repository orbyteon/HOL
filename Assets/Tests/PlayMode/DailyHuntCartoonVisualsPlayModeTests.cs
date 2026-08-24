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

        Invoke(hunt, "Open");
        yield return null;
        yield return null;
        Assert.That(hunt.gameObject.activeInHierarchy, Is.True);

        Transform root = Find(hunt.transform, "DailyHuntVisualRoot");
        Assert.That(root, Is.Not.Null);

        foreach (string name in new[]
        {
            "DailyBackground",
            "DailyStars",
            "DailyConfetti",
            "DailyOuterFrame",
            "DailyHuntSafeRoot",
            "CloseButton",
            "DailyBackIcon",
            "DailyPlayerChip",
            "DailyPlayerAvatar",
            "DailyTrophyIcon",
            "DailyPlayerName",
            "DailyPlayerWins",
            "DailyLogo",
            "DailyTitleRibbon",
            "Title",
            "DailyChallengeCard",
            "DailyCalendarTarget",
            "DailyChallengeHeading",
            "DailyStatusFrame",
            "Status",
            "DailyTrailFrame",
            "Trail",
            "GuessInput",
            "SubmitGuessButton",
            "DailyRewardCard",
            "DailyRewardChest",
            "DailyRewardHeading",
            "Streak",
            "ReviveButton",
            "ShareButton",
            "DailyMascotSix",
            "DailyMascotSeven",
        })
        {
            Assert.That(Find(root, name), Is.Not.Null,
                "Missing approved Daily Hunt object: " + name);
        }

        AssertRect(root, "DailyLogo",
            new Vector2(0f, 700f), new Vector2(560f, 300f));
        AssertRect(root, "DailyTitleRibbon",
            new Vector2(0f, 505f), new Vector2(910f, 150f));
        AssertRect(root, "DailyChallengeCard",
            new Vector2(0f, 40f), new Vector2(940f, 760f));
        AssertRect(root, "DailyCalendarTarget",
            new Vector2(-300f, 70f), new Vector2(340f, 410f));
        AssertRect(root, "DailyRewardCard",
            new Vector2(0f, -510f), new Vector2(920f, 280f));
        AssertRect(root, "DailyMascotSix",
            new Vector2(-420f, -805f), new Vector2(250f, 285f));
        AssertRect(root, "DailyMascotSeven",
            new Vector2(420f, -805f), new Vector2(250f, 285f));

        AssertSprite(root, "DailyCalendarTarget", "cartoon/cartoon_daily_calendar");
        AssertSprite(root, "DailyRewardChest", "cartoon/cartoon_reward_chest");
        AssertSprite(root, "DailyLogo", "reference/hol_logo_exact");

        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            Assert.That(graphic is Image || graphic is TMP_Text, Is.True,
                "Procedural Graphic found in Daily Hunt: " +
                graphic.GetType().Name + " / " + graphic.name);
            if (graphic is Image image && image.sprite != null)
                Assert.That(image.color.a, Is.GreaterThanOrEqualTo(0.99f),
                    image.name + " hides approved production art.");
        }

        TMP_FontAsset productionFont = Resources.Load<TMP_FontAsset>(
            "Fonts & Materials/LiberationSans SDF");
        Assert.That(productionFont, Is.Not.Null);
        foreach (string name in new[]
        {
            "Title",
            "Status",
            "Trail",
            "Streak",
            "DailyPlayerName",
            "DailyPlayerWins",
            "DailyChallengeHeading",
            "DailyRewardHeading",
        })
        {
            TMP_Text text = Find(root, name).GetComponent<TMP_Text>();
            Assert.That(text.font, Is.SameAs(productionFont),
                name + " must use the statically baked production font chain.");
        }

        Assert.That(
            Find(root, "DailyChallengeHeading").GetComponent<TMP_Text>().text,
            Is.EqualTo(Localized("home_daily_title")));
        Assert.That(
            Find(root, "DailyRewardHeading").GetComponent<TMP_Text>().text,
            Is.EqualTo(Localized("stats_streak").ToUpperInvariant()));

        SetLanguage("Greek");
        yield return null;
        Assert.That(
            Find(root, "DailyChallengeHeading").GetComponent<TMP_Text>().text,
            Is.EqualTo(Localized("home_daily_title")));
        Assert.That(
            Find(root, "DailyRewardHeading").GetComponent<TMP_Text>().text,
            Is.EqualTo(Localized("stats_streak").ToUpperInvariant()));
        SetLanguage("English");
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
