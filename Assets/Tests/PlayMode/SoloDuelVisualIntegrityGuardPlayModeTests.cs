using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class SoloDuelVisualIntegrityGuardPlayModeTests
{
    [UnityTest]
    public IEnumerator GuardKeepsOneOwnerAboveLegacyAndPreservesDuelPhaseAuthority()
    {
        Screen.SetResolution(1080, 1920, false);
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);

        Type layoutType = RuntimeType("HolDuelBoardLayout");
        Type guardType = RuntimeType("SoloDuelVisualIntegrityGuard");
        Type numberManagerType = RuntimeType("NumberManager");
        Type gameManagerType = RuntimeType("GameManager");

        Component layout = FindInScene(layoutType);
        Component numberManager = FindInScene(numberManagerType);
        Component gameManager = FindInScene(gameManagerType);
        Assert.That(layout, Is.Not.Null);
        Assert.That(numberManager, Is.Not.Null);
        Assert.That(gameManager, Is.Not.Null);

        numberManager.gameObject.SetActive(true);

        Component guard = null;
        for (int frame = 0; frame < 180; frame++)
        {
            guard = numberManager.GetComponent(guardType) as Component;
            if (guard != null && GetProperty<bool>(guard, "IsSettled"))
                break;
            yield return null;
        }

        Assert.That(guard, Is.Not.Null);
        Assert.That(GetProperty<bool>(guard, "IsSettled"), Is.True);

        Transform visualRoot = Find(
            numberManager.transform, "SoloDuelVisualRoot");
        Transform safeRoot = Find(visualRoot, "SoloDuelSafeRoot");
        Assert.That(visualRoot, Is.Not.Null);
        Assert.That(safeRoot, Is.Not.Null);
        Assert.That(
            visualRoot.GetSiblingIndex(),
            Is.EqualTo(visualRoot.parent.childCount - 1),
            "The one production owner must render above every legacy sibling.");

        GameObject legacyPlayer = GetField<GameObject>(
            numberManager, "playerGuessesPanel");
        GameObject legacyAi = GetField<GameObject>(
            numberManager, "aiGuessesPanel");
        Assert.That(legacyPlayer, Is.Not.Null);
        Assert.That(legacyAi, Is.Not.Null);

        legacyPlayer.SetActive(true);
        legacyAi.SetActive(true);
        yield return null;
        Assert.That(legacyPlayer.activeSelf, Is.False,
            "The retired PlayerGuessText panel must never overlay production UI.");
        Assert.That(legacyAi.activeSelf, Is.False,
            "The retired AIguesses panel must never overlay production UI.");

        GameObject higher = GetField<GameObject>(gameManager, "higherButton");
        GameObject correct = GetField<GameObject>(gameManager, "correctButton");
        GameObject lower = GetField<GameObject>(gameManager, "lowerButton");
        Assert.That(higher, Is.Not.Null);
        Assert.That(correct, Is.Not.Null);
        Assert.That(lower, Is.Not.Null);

        PresentPhase(layout, "ChooseSecret", "EnterSecret");
        higher.SetActive(true);
        correct.SetActive(true);
        lower.SetActive(true);
        yield return null;
        Assert.That(new[] { higher, correct, lower }.Any(x => x.activeSelf),
            Is.False,
            "No answer action is valid while the player chooses a secret.");

        PresentPhase(layout, "AnswerOpponent", "AnswerOpponent");
        higher.SetActive(true);
        correct.SetActive(false);
        lower.SetActive(false);
        yield return null;
        Assert.That(new[] { higher, correct, lower }.Count(x => x.activeSelf),
            Is.EqualTo(1),
            "AnswerOpponent must preserve the single GameManager-authorized action.");
        Assert.That(higher.activeSelf, Is.True);

        PresentPhase(layout, "PlayerGuess", "YourGuess");
        yield return null;
        Assert.That(new[] { higher, correct, lower }.Any(x => x.activeSelf),
            Is.False,
            "Answer controls must close immediately outside AnswerOpponent.");

        GameObject resultControl = GetField<GameObject>(
            gameManager, "stopGameButton");
        Assert.That(resultControl, Is.Not.Null);
        Assert.That(resultControl.transform.IsChildOf(safeRoot), Is.True,
            "The real result/rematch control must remain inside the production owner.");
    }

    [UnityTest]
    public IEnumerator CurrentNumberHeadingValueAndInputHaveDistinctVisibleBounds()
    {
        Screen.SetResolution(1080, 1920, false);
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);

        Component numberManager = FindInScene(RuntimeType("NumberManager"));
        Assert.That(numberManager, Is.Not.Null);
        numberManager.gameObject.SetActive(true);

        Component guard = null;
        for (int frame = 0; frame < 180; frame++)
        {
            guard = numberManager.GetComponent(
                RuntimeType("SoloDuelVisualIntegrityGuard")) as Component;
            if (guard != null && GetProperty<bool>(guard, "IsSettled"))
                break;
            yield return null;
        }

        Assert.That(guard, Is.Not.Null);
        Assert.That(GetProperty<bool>(guard, "IsSettled"), Is.True);

        Transform root = Find(numberManager.transform, "SoloDuelVisualRoot");
        RectTransform heading = Find(root, "CurrentNumberHeading") as RectTransform;
        TMP_Text value = GetField<TMP_Text>(numberManager, "playerNumberText");
        TMP_InputField input = GetField<TMP_InputField>(numberManager, "numberInput");
        Assert.That(heading, Is.Not.Null);
        Assert.That(value, Is.Not.Null);
        Assert.That(input, Is.Not.Null);
        Assert.That(heading.parent, Is.SameAs(value.transform.parent));
        Assert.That(value.transform.parent, Is.SameAs(input.transform.parent));

        AssertNoOverlap(heading, value.rectTransform,
            "Current-number heading overlaps its live value.");
        AssertNoOverlap(value.rectTransform, input.transform as RectTransform,
            "Current-number value overlaps the guess input.");

        Assert.That(heading.anchoredPosition.y,
            Is.GreaterThan(value.rectTransform.anchoredPosition.y));
        Assert.That(value.rectTransform.anchoredPosition.y,
            Is.GreaterThan((input.transform as RectTransform).anchoredPosition.y));
    }

    static void PresentPhase(
        Component layout,
        string phaseName,
        string promptName)
    {
        Type phaseType = RuntimeType("SoloBoardPhase");
        Type promptType = RuntimeType("SoloBoardPrompt");
        MethodInfo method = layout.GetType().GetMethod(
            "PresentPhase",
            BindingFlags.Instance | BindingFlags.Public);
        Assert.That(method, Is.Not.Null);
        method.Invoke(layout, new[]
        {
            Enum.Parse(phaseType, phaseName),
            Enum.Parse(promptType, promptName),
            (object)1,
            1,
            100,
            0,
        });
    }

    static void AssertNoOverlap(
        RectTransform first,
        RectTransform second,
        string message)
    {
        Assert.That(first, Is.Not.Null);
        Assert.That(second, Is.Not.Null);
        Assert.That(first.parent, Is.SameAs(second.parent));

        Rect a = BoundsInParent(first);
        Rect b = BoundsInParent(second);
        bool overlaps = a.xMin < b.xMax && b.xMin < a.xMax &&
                        a.yMin < b.yMax && b.yMin < a.yMax;
        Assert.That(overlaps, Is.False, message + " " + a + " / " + b);
    }

    static Rect BoundsInParent(RectTransform rect)
    {
        Vector2 size = rect.rect.size;
        Vector2 minimum = rect.anchoredPosition -
                          Vector2.Scale(size, rect.pivot);
        return new Rect(minimum, size);
    }

    static T GetField<T>(Component component, string name) where T : class
    {
        FieldInfo field = component.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        return field.GetValue(component) as T;
    }

    static T GetProperty<T>(Component component, string name)
    {
        PropertyInfo property = component.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, name);
        return (T)property.GetValue(component);
    }

    static Component FindInScene(Type type)
    {
        foreach (GameObject root in
                 SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Component found = root.GetComponentInChildren(type, true) as Component;
            if (found != null)
                return found;
        }
        return null;
    }

    static Transform Find(Transform root, string objectName)
    {
        if (root == null)
            return null;
        if (root.name == objectName)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = Find(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }
        return null;
    }

    static Type RuntimeType(string typeName)
    {
        Type type = Type.GetType(typeName + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, typeName);
        return type;
    }
}
