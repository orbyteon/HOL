using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class DailyHuntVisualFidelityPassPlayModeTests
{
    [UnityTest]
    public IEnumerator FidelityPassLocksReadableNonOverlappingDailyHuntComposition()
    {
        Screen.SetResolution(1080, 1920, false);
        SetLanguage("English");
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);

        Component hunt = FindInScene(RuntimeType("DailyHunt"));
        Assert.That(hunt, Is.Not.Null);
        Invoke(hunt, "Open");
        yield return new WaitForSecondsRealtime(0.36f);

        Component pass = null;
        for (int frame = 0; frame < 180; frame++)
        {
            pass = FindInScene(RuntimeType("DailyHuntVisualFidelityPass"));
            if (pass != null && GetProperty<bool>(pass, "IsSettled"))
                break;
            yield return null;
        }

        Assert.That(pass, Is.Not.Null);
        Assert.That(GetProperty<bool>(pass, "IsSettled"), Is.True);

        Transform root = Find(hunt.transform, "DailyHuntVisualRoot");
        Assert.That(root, Is.Not.Null);

        AssertRect(root, "DailyPlayerChip",
            new Vector2(350f, 842f), new Vector2(330f, 110f));
        AssertRect(root, "DailyPlayerAvatar",
            new Vector2(-118f, 0f), new Vector2(76f, 76f));
        AssertRect(root, "DailyChallengeHeading",
            new Vector2(215f, 278f), new Vector2(430f, 52f));
        AssertRect(root, "DailyStatusFrame",
            new Vector2(215f, 112f), new Vector2(470f, 188f));
        AssertRect(root, "DailyTrailFrame",
            new Vector2(215f, -62f), new Vector2(470f, 82f));
        AssertRect(root, "GuessInput",
            new Vector2(215f, -184f), new Vector2(340f, 84f));
        AssertRect(root, "SubmitGuessButton",
            new Vector2(215f, -292f), new Vector2(420f, 88f));
        AssertRect(root, "DailyRewardChest",
            new Vector2(-305f, 0f), new Vector2(205f, 205f));
        AssertRect(root, "DailyRewardHeading",
            new Vector2(180f, 72f), new Vector2(470f, 48f));
        AssertRect(root, "Streak",
            new Vector2(180f, 17f), new Vector2(470f, 42f));
        AssertRect(root, "ReviveButton",
            new Vector2(180f, -64f), new Vector2(430f, 72f));
        AssertRect(root, "ShareButton",
            new Vector2(180f, -64f), new Vector2(430f, 72f));

        Image avatar = Find(root, "DailyPlayerAvatar").GetComponent<Image>();
        Assert.That(avatar.sprite,
            Is.SameAs(Resources.Load<Sprite>("reference/char_boy_exact")));
        Assert.That(avatar.preserveAspect, Is.True);
        Assert.That(avatar.color.a, Is.EqualTo(1f).Within(0.001f));

        AssertNoOverlap(root, "DailyChallengeHeading", "DailyStatusFrame");
        AssertNoOverlap(root, "DailyStatusFrame", "DailyTrailFrame");
        AssertNoOverlap(root, "DailyTrailFrame", "GuessInput");
        AssertNoOverlap(root, "GuessInput", "SubmitGuessButton");
        AssertNoOverlap(root, "DailyRewardHeading", "Streak");
        AssertNoOverlap(root, "Streak", "ReviveButton");
        AssertNoOverlap(root, "Streak", "ShareButton");

        TMP_InputField input = Find(root, "GuessInput")
            .GetComponent<TMP_InputField>();
        Assert.That(input, Is.Not.Null);
        Assert.That(input.textComponent.font,
            Is.SameAs(Resources.Load<TMP_FontAsset>(
                "Fonts & Materials/LiberationSans SDF")));
        Assert.That(input.textComponent.fontSizeMin, Is.GreaterThanOrEqualTo(25f));
        Assert.That(input.textComponent.alignment,
            Is.EqualTo(TextAlignmentOptions.Center));

        foreach (string language in new[] { "English", "Greek" })
        {
            SetLanguage(language);
            for (int frame = 0; frame < 3; frame++)
                yield return new WaitForEndOfFrame();
            Canvas.ForceUpdateCanvases();

            foreach (string name in new[]
            {
                "DailyChallengeHeading",
                "Status",
                "Trail",
                "DailyRewardHeading",
                "Streak",
                "DailyPlayerName",
                "DailyPlayerWins",
            })
            {
                TMP_Text text = Find(root, name).GetComponent<TMP_Text>();
                text.ForceMeshUpdate();
                Assert.That(text.isTextOverflowing, Is.False,
                    language + " / " + name + " overflowed.");
                Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(20f),
                    language + " / " + name + " became unreadable.");
            }
        }

        SetLanguage("English");
        Button close = Find(root, "CloseButton").GetComponent<Button>();
        Assert.That(close, Is.Not.Null);
        close.onClick.Invoke();
        yield return null;
        Assert.That(hunt.gameObject.activeSelf, Is.False,
            "The fidelity pass must preserve the real Daily Hunt Close callback.");
    }

    static void AssertRect(
        Transform root,
        string objectName,
        Vector2 position,
        Vector2 size)
    {
        RectTransform rect = Find(root, objectName) as RectTransform;
        Assert.That(rect, Is.Not.Null, objectName);
        Assert.That(Vector2.Distance(rect.anchoredPosition, position),
            Is.LessThan(1f), objectName + " position drifted.");
        Assert.That(Vector2.Distance(rect.sizeDelta, size),
            Is.LessThan(1f), objectName + " size drifted.");
    }

    static void AssertNoOverlap(
        Transform root,
        string firstName,
        string secondName)
    {
        RectTransform first = Find(root, firstName) as RectTransform;
        RectTransform second = Find(root, secondName) as RectTransform;
        Assert.That(first, Is.Not.Null, firstName);
        Assert.That(second, Is.Not.Null, secondName);
        Assert.That(first.parent, Is.SameAs(second.parent),
            firstName + " and " + secondName + " must share one layout space.");

        Rect a = BoundsInParent(first);
        Rect b = BoundsInParent(second);
        bool overlaps = a.xMin < b.xMax && b.xMin < a.xMax &&
                        a.yMin < b.yMax && b.yMin < a.yMax;
        Assert.That(overlaps, Is.False,
            firstName + " overlaps " + secondName + ": " + a + " / " + b);
    }

    static Rect BoundsInParent(RectTransform rect)
    {
        Vector2 size = rect.rect.size;
        Vector2 minimum = rect.anchoredPosition -
                          Vector2.Scale(size, rect.pivot);
        return new Rect(minimum, size);
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

    static object Invoke(Component target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, methodName);
        return method.Invoke(target, null);
    }

    static T GetProperty<T>(Component target, string propertyName)
    {
        PropertyInfo property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, propertyName);
        return (T)property.GetValue(target);
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
