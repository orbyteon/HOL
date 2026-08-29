using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class MainMenuHomeSubtitlePlayModeTests
{
    const BindingFlags StaticFlags =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    [UnityTest]
    public IEnumerator SoloSubtitleIsSingleLocalizedReadableLabel()
    {
        bool hadLanguage = PlayerPrefs.HasKey("Language");
        int savedLanguage = PlayerPrefs.GetInt("Language", 0);

        try
        {
            SetLanguage(0);
            InvokeInstaller();
            yield return SceneManager.LoadSceneAsync(
                "MainMenu", LoadSceneMode.Single);

            Component owner = null;
            for (int frame = 0; frame < 160; frame++)
            {
                owner = FindInScene(RuntimeType("MainMenuHomeVisuals"));
                if (owner != null &&
                    Property<bool>(owner, "IsReady") &&
                    Property<bool>(owner, "IsSettled"))
                    break;
                yield return null;
            }

            Assert.That(owner, Is.Not.Null);
            Assert.That(Property<bool>(owner, "IsReady"), Is.True);
            Assert.That(Property<bool>(owner, "IsSettled"), Is.True);

            Transform solo = Find(owner.transform, "ButtonPlay");
            Assert.That(solo, Is.Not.Null);

            TMP_Text[] visibleLabels =
                solo.GetComponentsInChildren<TMP_Text>(false);
            Assert.That(visibleLabels, Has.Length.EqualTo(2),
                "Solo CTA must expose exactly its title and subtitle; no legacy label may remain visible.");

            TMP_Text title = Find(solo, "HomeSoloTitle")
                ?.GetComponent<TMP_Text>();
            TMP_Text subtitle = Find(solo, "HomeSoloSubtitle")
                ?.GetComponent<TMP_Text>();
            Assert.That(title, Is.Not.Null);
            Assert.That(subtitle, Is.Not.Null);
            Assert.That(title.gameObject.activeInHierarchy, Is.True);
            Assert.That(subtitle.gameObject.activeInHierarchy, Is.True);
            Assert.That(subtitle.text, Is.EqualTo(Localized("home_solo_subtitle")));
            Assert.That(subtitle.raycastTarget, Is.False);
            Assert.That(subtitle.color.r, Is.LessThan(0.25f));
            Assert.That(subtitle.color.g, Is.LessThan(0.25f));
            Assert.That(subtitle.color.b, Is.LessThan(0.30f),
                "Solo subtitle must retain the approved dark-ink treatment on gold.");

            SetLanguage(1);
            Assert.That(subtitle.text,
                Is.EqualTo(Localized("home_solo_subtitle")));
            Assert.That(subtitle.text,
                Is.EqualTo("Παίξε και σπάσε το ρεκόρ σου!"));
            Assert.That(solo.GetComponentsInChildren<TMP_Text>(false),
                Has.Length.EqualTo(2));
        }
        finally
        {
            SetLanguage(savedLanguage == 1 ? 1 : 0);
            if (!hadLanguage)
                PlayerPrefs.DeleteKey("Language");
        }
    }

    static string Localized(string key)
    {
        MethodInfo get = RuntimeType("L10n").GetMethod("Get", StaticFlags);
        Assert.That(get, Is.Not.Null);
        return (string)get.Invoke(null, new object[] { key, new object[0] });
    }

    static void InvokeInstaller()
    {
        MethodInfo install = RuntimeType("MainMenuHomeVisuals").GetMethod(
            "Install", StaticFlags);
        Assert.That(install, Is.Not.Null);
        install.Invoke(null, null);
    }

    static void SetLanguage(int value)
    {
        Type l10n = RuntimeType("L10n");
        Type language = l10n.GetNestedType("Language");
        object enumValue = Enum.ToObject(language, value);
        l10n.GetMethod("SetLanguage", BindingFlags.Static | BindingFlags.Public)
            .Invoke(null, new[] { enumValue });
    }

    static T Property<T>(Component component, string name)
    {
        PropertyInfo property = component.GetType().GetProperty(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, "Missing property " + name);
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
        Assert.That(type, Is.Not.Null, "Missing runtime component: " + name);
        return type;
    }
}
