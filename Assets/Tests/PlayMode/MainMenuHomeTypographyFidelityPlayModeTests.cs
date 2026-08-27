using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class MainMenuHomeTypographyFidelityPlayModeTests
{
    const BindingFlags StaticFlags =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    [UnityTest]
    public IEnumerator FinalDeviceTypographyRemainsInsideApprovedHomeControls()
    {
        Screen.SetResolution(1080, 1920, false);
        InvokeInstaller("MainMenuHomeVisuals");
        InvokeInstaller("MainMenuHomeTypographyFidelity");
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);

        Type ownerType = RuntimeType("MainMenuHomeVisuals");
        Type fidelityType = RuntimeType("MainMenuHomeTypographyFidelity");
        Component owner = null;
        Component fidelity = null;

        for (int frame = 0; frame < 240; frame++)
        {
            owner = FindInScene(ownerType);
            fidelity = FindInScene(fidelityType);
            if (owner != null && fidelity != null &&
                GetProperty<bool>(owner, "IsReady") &&
                GetProperty<bool>(owner, "IsSettled"))
            {
                Transform visual = Find(owner.transform, "HomeVisualRoot");
                TMP_Text title = Find(visual, "HomeSoloTitle")
                    ?.GetComponent<TMP_Text>();
                if (title != null &&
                    Vector2.Distance(title.rectTransform.anchoredPosition,
                        new Vector2(60f, 18f)) < 1f)
                    break;
            }
            yield return null;
        }

        Assert.That(owner, Is.Not.Null);
        Assert.That(fidelity, Is.Not.Null);
        Assert.That(CountInScene(fidelityType), Is.EqualTo(1));

        Transform root = Find(owner.transform, "HomeVisualRoot");
        Assert.That(root, Is.Not.Null);

        TMP_Text speech = Text(root, "HomeSpeechText");
        Assert.That(speech.color.r, Is.GreaterThan(0.9f));
        Assert.That(speech.color.g, Is.GreaterThan(0.9f));
        Assert.That(speech.color.b, Is.GreaterThan(0.9f));
        Assert.That(speech.fontSizeMin, Is.GreaterThanOrEqualTo(24f));
        Assert.That(speech.fontSizeMax, Is.LessThanOrEqualTo(30f));
        Assert.That(speech.GetComponent<UnityEngine.UI.Shadow>(), Is.Not.Null);

        AssertRect(root, "HomePlayerChipText",
            new Vector2(42f, 0f), new Vector2(190f, 78f));
        TMP_Text chip = Text(root, "HomePlayerChipText");
        Assert.That(chip.fontSizeMin, Is.GreaterThanOrEqualTo(18f));
        Assert.That(chip.fontSizeMax, Is.LessThanOrEqualTo(24f));

        foreach (string titleName in new[]
        {
            "HomeSoloTitle",
            "HomePvpTitle",
            "HomeFriendTitle",
            "HomeDailyTitle",
        })
        {
            AssertRect(root, titleName,
                new Vector2(60f, 18f), new Vector2(690f, 50f));
            TMP_Text title = Text(root, titleName);
            Assert.That(title.fontSizeMin, Is.GreaterThanOrEqualTo(32f),
                titleName);
            Assert.That(title.fontSizeMax, Is.LessThanOrEqualTo(46f),
                titleName);
            Assert.That(title.enableWordWrapping, Is.False, titleName);
        }

        foreach (string subtitleName in new[]
        {
            "HomeSoloSubtitle",
            "HomePvpSubtitle",
            "HomeFriendSubtitle",
            "HomeDailySubtitle",
        })
        {
            AssertRect(root, subtitleName,
                new Vector2(60f, -28f), new Vector2(690f, 34f));
            TMP_Text subtitle = Text(root, subtitleName);
            Assert.That(subtitle.fontSizeMin, Is.GreaterThanOrEqualTo(18f),
                subtitleName);
            Assert.That(subtitle.fontSizeMax, Is.LessThanOrEqualTo(22f),
                subtitleName);
            Assert.That(subtitle.enableWordWrapping, Is.False, subtitleName);
        }
    }

    static void InvokeInstaller(string typeName)
    {
        Type type = RuntimeType(typeName);
        MethodInfo install = type.GetMethod("Install", StaticFlags);
        Assert.That(install, Is.Not.Null, typeName + ".Install");
        install.Invoke(null, null);
    }

    static TMP_Text Text(Transform root, string name)
    {
        Transform found = Find(root, name);
        Assert.That(found, Is.Not.Null, name);
        TMP_Text text = found.GetComponent<TMP_Text>();
        Assert.That(text, Is.Not.Null, name);
        return text;
    }

    static void AssertRect(
        Transform root,
        string name,
        Vector2 expectedPosition,
        Vector2 expectedSize)
    {
        RectTransform rect = Find(root, name) as RectTransform;
        Assert.That(rect, Is.Not.Null, name);
        Assert.That(Vector2.Distance(rect.anchoredPosition, expectedPosition),
            Is.LessThan(1f), name + " position drifted.");
        Assert.That(Vector2.Distance(rect.sizeDelta, expectedSize),
            Is.LessThan(1f), name + " size drifted.");
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

    static T GetProperty<T>(Component component, string name)
    {
        PropertyInfo property = component.GetType().GetProperty(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, name);
        return (T)property.GetValue(component);
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
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(name);
            if (type != null) return type;
        }
        Assert.Fail("Missing runtime type: " + name);
        return null;
    }
}
