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

public sealed class SoloDuelCartoonVisualsPlayModeTests
{
    [UnityTest]
    public IEnumerator SoloBoardMatchesApprovedCartoonCompositionAndKeepsRealControls()
    {
        Screen.SetResolution(1080, 1920, false);
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);

        Component layout = null;
        Component numberManager = null;
        for (int frame = 0; frame < 120; frame++)
        {
            layout = FindInScene(RuntimeType("HolDuelBoardLayout"));
            numberManager = FindInScene(RuntimeType("NumberManager"));
            if (layout != null && numberManager != null)
                break;
            yield return null;
        }

        Assert.That(layout, Is.Not.Null);
        Assert.That(numberManager, Is.Not.Null);
        numberManager.gameObject.SetActive(true);
        for (int frame = 0; frame < 6; frame++)
            yield return null;

        Assert.That(GetProperty<bool>(layout, "IsReady"), Is.True);
        Assert.That(CountInScene(RuntimeType("HolDuelBoardLayout")), Is.EqualTo(1));

        Transform root = Find(numberManager.transform, "SoloDuelVisualRoot");
        Assert.That(root, Is.Not.Null);

        foreach (string name in new[]
        {
            "SoloDuelBackground",
            "SoloDuelStars",
            "SoloDuelConfetti",
            "SoloDuelOuterFrame",
            "SoloDuelSafeRoot",
            "DuelBack",
            "DuelBackIcon",
            "SoloDuelLogo",
            "SoloDuelPlayerChip",
            "SoloDuelChipAvatar",
            "SoloDuelChipTrophy",
            "SoloDuelChipText",
            "PlayerCard",
            "PlayerCharacter",
            "PlayerCaption",
            "PlayerName",
            "OpponentCard",
            "OpponentCharacter",
            "OpponentCaption",
            "OpponentIdentity",
            "SoloVsBurst",
            "SoloPromptRibbon",
            "RoundLabel",
            "SoloDuelMascotSeven",
            "SoloDuelMascotThree",
            "SoloInteractionCard",
            "CurrentNumberHeading",
            "SoloOpponentRail",
            "SoloOpponentBubble",
            "OpponentBubbleAvatar",
            "HistoryCard",
            "HistoryTitle",
            "PlayerGuessHistory",
            "AiGuessHistory",
            "SoloTipCard",
            "SoloTipHeading",
            "RangeLabel",
            "NumberKeypad",
            "ButtonConfirm",
        })
        {
            Assert.That(Find(root, name), Is.Not.Null,
                "Missing approved Solo duel object: " + name);
        }

        AssertRect(root, "PlayerCard",
            new Vector2(-270f, 605f), new Vector2(470f, 340f));
        AssertRect(root, "OpponentCard",
            new Vector2(270f, 605f), new Vector2(470f, 340f));
        AssertRect(root, "SoloVsBurst",
            new Vector2(0f, 600f), new Vector2(190f, 190f));
        AssertRect(root, "SoloPromptRibbon",
            new Vector2(0f, 365f), new Vector2(900f, 150f));
        AssertRect(root, "SoloInteractionCard",
            new Vector2(-225f, -260f), new Vector2(610f, 950f));
        AssertRect(root, "SoloOpponentRail",
            new Vector2(330f, -260f), new Vector2(350f, 950f));
        AssertRect(root, "NumberKeypad",
            new Vector2(0f, -30f), new Vector2(560f, 560f));
        AssertRect(root, "ButtonConfirm",
            new Vector2(0f, -408f), new Vector2(530f, 104f));

        AssertSprite(root, "SoloDuelLogo", "reference/hol_logo_exact");
        AssertSprite(root, "PlayerCharacter", "reference/player_cyan_exact");
        AssertSprite(root, "OpponentCharacter", "reference/opponent_purple_exact");
        AssertSprite(root, "SoloVsBurst", "reference/board_vs_burst_exact");
        AssertSprite(root, "SoloDuelMascotSeven", "reference/mascot_7_exact");
        AssertSprite(root, "SoloDuelMascotThree", "reference/mascot_3_exact");

        Transform keypad = Find(root, "NumberKeypad");
        Button[] keypadButtons = keypad.GetComponentsInChildren<Button>(true);
        Assert.That(keypadButtons, Has.Length.EqualTo(12));
        foreach (Button key in keypadButtons)
        {
            Assert.That(key.GetComponent<Image>().sprite,
                Is.SameAs(Resources.Load<Sprite>(
                    "mainmenu/mainmenu_cta_blue_9s")));
            Assert.That(key.GetComponent<RectTransform>().rect.width,
                Is.GreaterThanOrEqualTo(160f));
            Assert.That(key.GetComponent<RectTransform>().rect.height,
                Is.GreaterThanOrEqualTo(108f));
        }

        TMP_InputField input = GetField<TMP_InputField>(
            numberManager, "numberInput");
        Assert.That(input, Is.Not.Null);
        Assert.That(input.shouldHideMobileInput, Is.True);
        Assert.That(input.shouldHideSoftKeyboard, Is.True);
        Assert.That(GetProperty<Button>(layout, "SubmitControl"), Is.Not.Null);
        Assert.That(GetProperty<GameObject>(layout, "KeypadRoot"),
            Is.SameAs(keypad.gameObject));

        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            Assert.That(graphic is Image || graphic is TMP_Text, Is.True,
                "Procedural Graphic found in Solo duel: " +
                graphic.GetType().Name + " / " + graphic.name);
            if (graphic is Image image && image.sprite != null)
                Assert.That(image.color.a, Is.GreaterThanOrEqualTo(0.99f),
                    image.name + " hides approved production art.");
        }

        TMP_Text prompt = GetField<TMP_Text>(
            FindInScene(RuntimeType("GameManager")), "turnText");
        Assert.That(prompt.transform.IsChildOf(root), Is.True,
            "The real GameManager prompt must be seated in the approved ribbon.");

        string[] answerNames =
        {
            "ButtonHIGHER", "ButtonCORRECT", "ButtonLOWER",
        };
        foreach (string answerName in answerNames)
        {
            Button answer = Find(numberManager.transform, answerName)
                .GetComponent<Button>();
            Assert.That(answer, Is.Not.Null);
            Assert.That(answer.targetGraphic, Is.SameAs(answer.GetComponent<Image>()));
        }

        Assert.That(numberManager.GetComponentsInChildren<Button>(true)
            .Count(button => button.name == "ButtonConfirm" &&
                             button.gameObject.activeInHierarchy),
            Is.EqualTo(1),
            "The cartoon board must expose one real submit control.");
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
