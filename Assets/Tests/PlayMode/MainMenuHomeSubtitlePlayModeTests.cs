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
    public IEnumerator PlaySubtitleIsSingleLocalizedReadableLabel()
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

            Transform play = Find(owner.transform, "ButtonPlay");
            Assert.That(play, Is.Not.Null);

            TMP_Text[] visibleLabels =
                play.GetComponentsInChildren<TMP_Text>(false);
            Assert.That(visibleLabels, Has.Length.EqualTo(2),
                "PLAY must expose exactly its title and subtitle; no retired mode label may remain visible.");

            TMP_Text title = Find(play, "HomePlayTitle")
                ?.GetComponent<TMP_Text>();
            TMP_Text subtitle = Find(play, "HomePlaySubtitle")
                ?.GetComponent<TMP_Text>();
            Assert.That(title, Is.Not.Null);
            Assert.That(subtitle, Is.Not.Null);
            Assert.That(title.gameObject.activeInHierarchy, Is.True);
            Assert.That(subtitle.gameObject.activeInHierarchy, Is.True);
            Assert.That(title.text, Is.EqualTo(Localized("play")));
            Assert.That(subtitle.text, Is.EqualTo(Localized("home_play_subtitle")));
            Assert.That(subtitle.raycastTarget, Is.False);
            AssertApprovedSubtitle(subtitle, Localized("home_play_subtitle"));

            SetLanguage(1);
            Assert.That(title.text, Is.EqualTo("Παίξε"));
            Assert.That(subtitle.text,
                Is.EqualTo(Localized("home_play_subtitle")));
            Assert.That(subtitle.text,
                Is.EqualTo("Διάλεξε τρόπο παιχνιδιού"));
            Assert.That(play.GetComponentsInChildren<TMP_Text>(false),
                Has.Length.EqualTo(2));
            AssertApprovedSubtitle(subtitle, Localized("home_play_subtitle"));
        }
        finally
        {
            SetLanguage(savedLanguage == 1 ? 1 : 0);
            if (!hadLanguage)
                PlayerPrefs.DeleteKey("Language");
        }
    }

    static void AssertApprovedSubtitle(TMP_Text subtitle, string expected)
    {
        // Approved VS-AI-derived Home: near-white copy with an ink outline
        // inside the dark inset, not the retired dark-ink-on-gold subtitle.
        Assert.That(subtitle.color.r, Is.EqualTo(0.985f).Within(0.001f));
        Assert.That(subtitle.color.g, Is.EqualTo(0.975f).Within(0.001f));
        Assert.That(subtitle.color.b, Is.EqualTo(1f).Within(0.001f));
        Assert.That(subtitle.color.a, Is.EqualTo(1f).Within(0.001f));
        // TMP exposes outlineColor as Color32; compare normalized Color units.
        Color outline = subtitle.outlineColor;
        Assert.That(outline.r, Is.EqualTo(0.09f).Within(0.001f));
        Assert.That(outline.g, Is.EqualTo(0.05f).Within(0.001f));
        Assert.That(outline.b, Is.EqualTo(0.16f).Within(0.001f));
        Assert.That(subtitle.outlineWidth, Is.EqualTo(0.12f).Within(0.001f));
        Assert.That(subtitle.alignment, Is.EqualTo(TextAlignmentOptions.Center));
        Assert.That(subtitle.raycastTarget, Is.False);
        Assert.That(subtitle.isActiveAndEnabled, Is.True);
        Canvas.ForceUpdateCanvases();
        subtitle.ForceMeshUpdate();
        Assert.That(subtitle.fontSize, Is.GreaterThanOrEqualTo(25f));
        Assert.That(subtitle.isTextOverflowing, Is.False, expected);
        Assert.That(subtitle.isTextTruncated, Is.False, expected);
        Assert.That(subtitle.textInfo.characterCount, Is.EqualTo(expected.Length));
        for (int i = 0; i < expected.Length; i++)
        {
            TMP_CharacterInfo glyph = subtitle.textInfo.characterInfo[i];
            Assert.That(glyph.character, Is.EqualTo(expected[i]), expected + " glyph " + i);
            if (!char.IsWhiteSpace(expected[i]))
                Assert.That(glyph.isVisible, Is.True, expected + " glyph " + i);
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
