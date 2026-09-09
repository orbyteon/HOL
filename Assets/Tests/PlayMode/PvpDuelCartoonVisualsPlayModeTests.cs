using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class PvpDuelCartoonVisualsPlayModeTests
{
    [UnityTest]
    public IEnumerator MatchAndResultReuseAuthoritativeControllerControls()
    {
        Screen.SetResolution(1080, 1920, false);
        RegisterInstaller();
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);

        Component controller = null;
        Component visuals = null;
        for (int frame = 0; frame < 240; frame++)
        {
            controller = FindInScene(RuntimeType("PvpGameController"));
            visuals = FindInScene(RuntimeType("PvpDuelCartoonVisuals"));
            if (controller != null && visuals != null &&
                GetProperty<bool>(visuals, "IsReady"))
                break;
            yield return null;
        }

        Assert.That(controller, Is.Not.Null);
        Assert.That(visuals, Is.Not.Null);
        Assert.That(GetProperty<bool>(visuals, "IsReady"), Is.True);
        Assert.That(CountInScene(RuntimeType("PvpDuelCartoonVisuals")), Is.EqualTo(1));

        GameObject matchPanel = GetField<GameObject>(controller, "matchPanel");
        Assert.That(matchPanel, Is.Not.Null);
        matchPanel.SetActive(true);
        yield return null;

        Transform matchRoot = Find(matchPanel.transform, "PvpDuelCartoonRoot");
        Assert.That(matchRoot, Is.Not.Null);
        foreach (string name in new[]
        {
            "PvpMatchBackground",
            "PvpMatchStars",
            "PvpMatchConfetti",
            "PvpMatchOuterFrame",
            "PvpMatchSafeRoot",
            "PvpMatchLogo",
            "PvpLeaveBackIcon",
            "PvpMatchPlayerChip",
            "PvpPlayerCard",
            "PvpPlayerCharacter",
            "PvpOpponentCard",
            "PvpOpponentCharacter",
            "PvpVsBurst",
            "PvpPromptRibbon",
            "PvpInteractionCard",
            "PvpCurrentNumberHeading",
            "PvpOpponentRail",
            "PvpSignalBubble",
            "PvpSignalAvatar",
            "PvpHistoryCard",
            "PvpHistoryTitle",
            "PvpTipCard",
            "PvpTipTitle",
        })
        {
            Assert.That(Find(matchRoot, name), Is.Not.Null,
                "Missing approved PvP match object: " + name);
        }

        AssertRect(matchRoot, "PvpPlayerCard",
            new Vector2(-270f, 610f), new Vector2(470f, 345f));
        AssertRect(matchRoot, "PvpOpponentCard",
            new Vector2(270f, 610f), new Vector2(470f, 345f));
        AssertRect(matchRoot, "PvpVsBurst",
            new Vector2(0f, 605f), new Vector2(190f, 190f));
        AssertRect(matchRoot, "PvpPromptRibbon",
            new Vector2(0f, 370f), new Vector2(900f, 150f));
        AssertRect(matchRoot, "PvpInteractionCard",
            new Vector2(-225f, -255f), new Vector2(610f, 940f));
        AssertRect(matchRoot, "PvpOpponentRail",
            new Vector2(330f, -255f), new Vector2(350f, 940f));

        AssertSprite(matchRoot, "PvpMatchLogo", "reference/hol_logo_exact");
        AssertSprite(matchRoot, "PvpPlayerCharacter", "reference/player_cyan_exact");
        AssertSprite(matchRoot, "PvpOpponentCharacter", "reference/opponent_purple_exact");
        AssertSprite(matchRoot, "PvpVsBurst", "reference/board_vs_burst_exact");

        TMP_InputField guessInput = GetField<TMP_InputField>(controller, "guessInput");
        GameObject guessButton = GetField<GameObject>(controller, "guessButton");
        GameObject keypadRoot = GetField<GameObject>(controller, "keypadRoot");
        GameObject lockButton = GetField<GameObject>(controller, "lockButton");
        TMP_Text opponentName = GetField<TMP_Text>(controller, "opponentNameText");
        TMP_Text turn = GetField<TMP_Text>(controller, "turnText");
        TMP_Text history = GetField<TMP_Text>(controller, "historyText");
        TMP_Text range = GetField<TMP_Text>(controller, "rangeText");
        Assert.That(guessInput.transform.IsChildOf(matchRoot), Is.True);
        Assert.That(guessButton.transform.IsChildOf(matchRoot), Is.True);
        Assert.That(keypadRoot.transform.IsChildOf(matchRoot), Is.True);
        Assert.That(lockButton.transform.IsChildOf(matchRoot), Is.True);
        Assert.That(opponentName.transform.IsChildOf(
            Find(matchRoot, "PvpOpponentCard")), Is.True);
        Assert.That(turn.transform.IsChildOf(
            Find(matchRoot, "PvpPromptRibbon")), Is.True);
        Assert.That(history.transform.IsChildOf(
            Find(matchRoot, "PvpHistoryCard")), Is.True);
        Assert.That(range.transform.IsChildOf(
            Find(matchRoot, "PvpTipCard")), Is.True);

        Button[] keypadButtons = keypadRoot.GetComponentsInChildren<Button>(true);
        Assert.That(keypadButtons, Has.Length.EqualTo(12));
        foreach (Button key in keypadButtons)
        {
            Assert.That(key.targetGraphic, Is.SameAs(key.GetComponent<Image>()));
            Assert.That(key.GetComponent<RectTransform>().rect.width,
                Is.GreaterThanOrEqualTo(160f));
            Assert.That(key.GetComponent<RectTransform>().rect.height,
                Is.GreaterThanOrEqualTo(104f));
        }

        AssertOnlyImageAndTmpGraphics(matchRoot, "PvP match");

        Component resultPresentation = GetField<Component>(
            controller, "resultPresentation");
        Assert.That(resultPresentation, Is.Not.Null);
        Transform resultRoot = Find(
            resultPresentation.transform, "PvpResultCartoonRoot");
        Assert.That(resultRoot, Is.Not.Null);

        foreach (string name in new[]
        {
            "PvpResultBackground",
            "PvpResultStars",
            "PvpResultConfetti",
            "PvpResultOuterFrame",
            "PvpResultSafeRoot",
            "PvpResultLogo",
            "PvpResultPlayerChip",
            "PvpResultTitleRibbon",
            "PvpResultHero",
            "PvpResultTrophy",
            "PvpResultOpponentCard",
            "PvpResultOpponentCharacter",
            "PvpResultStatsCard",
            "PlayerAttemptsRow",
            "OpponentAttemptsRow",
            "PvpResultStreak",
            "PvpResultActions",
            "PvpResultMascotSix",
            "PvpResultMascotSeven",
        })
        {
            Assert.That(Find(resultRoot, name), Is.Not.Null,
                "Missing approved PvP result object: " + name);
        }

        TMP_Text title = GetField<TMP_Text>(resultPresentation, "titleText");
        TMP_Text playerAttempts = GetField<TMP_Text>(
            resultPresentation, "playerAttemptsText");
        TMP_Text opponentAttempts = GetField<TMP_Text>(
            resultPresentation, "opponentAttemptsText");
        TMP_Text revealed = GetField<TMP_Text>(
            resultPresentation, "revealedNumberText");
        GameObject rematch = GetField<GameObject>(controller, "rematchButton");
        TMP_InputField rematchSecret = GetField<TMP_InputField>(
            controller, "rematchSecretInput");
        GameObject exit = GetField<GameObject>(controller, "resultExitButton");
        Assert.That(title.transform.IsChildOf(
            Find(resultRoot, "PvpResultTitleRibbon")), Is.True);
        Assert.That(playerAttempts.transform.IsChildOf(
            Find(resultRoot, "PlayerAttemptsRow")), Is.True);
        Assert.That(opponentAttempts.transform.IsChildOf(
            Find(resultRoot, "OpponentAttemptsRow")), Is.True);
        Assert.That(revealed.transform.IsChildOf(
            Find(resultRoot, "PvpResultStatsCard")), Is.True);
        Assert.That(rematch.transform.IsChildOf(
            Find(resultRoot, "PvpResultActions")), Is.True);
        Assert.That(rematchSecret.transform.IsChildOf(
            Find(resultRoot, "PvpResultActions")), Is.True);
        Assert.That(exit.transform.IsChildOf(
            Find(resultRoot, "PvpResultActions")), Is.True);

        MethodInfo show = resultPresentation.GetType().GetMethod(
            "ShowLocalized", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(show, Is.Not.Null);
        show.Invoke(resultPresentation,
            new object[] { "you_win", 4, 7, 42, true });
        yield return null;
        Assert.That(resultPresentation.gameObject.activeSelf, Is.True);
        Assert.That(title.text, Is.EqualTo(Localized("you_win")));
        Assert.That(playerAttempts.text, Is.EqualTo("4"));
        Assert.That(opponentAttempts.text, Is.EqualTo("7"));
        Assert.That(revealed.text, Does.Contain("42"));
        Assert.That(GetField<GameObject>(resultPresentation, "trophy").activeSelf,
            Is.True);

        AssertOnlyImageAndTmpGraphics(resultRoot, "PvP result");
    }

    static void AssertOnlyImageAndTmpGraphics(Transform root, string screen)
    {
        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            Assert.That(graphic is Image || graphic is TMP_Text, Is.True,
                "Procedural Graphic found in " + screen + ": " +
                graphic.GetType().Name + " / " + graphic.name);
            if (graphic is Image image && image.sprite != null)
                Assert.That(image.color.a, Is.GreaterThanOrEqualTo(0.99f),
                    image.name + " hides approved production art.");
        }
    }

    static string Localized(string key)
    {
        MethodInfo get = RuntimeType("L10n").GetMethod(
            "Get", BindingFlags.Static | BindingFlags.Public);
        return (string)get.Invoke(null, new object[] { key, new object[0] });
    }

    static void RegisterInstaller()
    {
        MethodInfo register = RuntimeType("PvpDuelCartoonVisualsInstaller")
            .GetMethod("Register", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(register, Is.Not.Null);
        register.Invoke(null, null);
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

    static T GetField<T>(Component component, string name) where T : class
    {
        FieldInfo field = component.GetType().GetField(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        return field.GetValue(component) as T;
    }

    static T GetProperty<T>(Component component, string name)
    {
        PropertyInfo property = component.GetType().GetProperty(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, name);
        return (T)property.GetValue(component);
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
