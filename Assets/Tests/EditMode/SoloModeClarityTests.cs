using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class SoloModeClarityTests
{
    Type transitionType;
    Type l10nType;
    GameObject host;
    GameObject searchingPanel;
    GameObject panelGame;
    Component transition;
    object initialLanguage;

    [SetUp]
    public void SetUp()
    {
        transitionType = RuntimeType("FakeMatchmaking");
        l10nType = RuntimeType("L10n");
        initialLanguage = l10nType.GetProperty("Current",
            BindingFlags.Public | BindingFlags.Static).GetValue(null, null);

        host = new GameObject("SoloEntryOwner");
        searchingPanel = new GameObject("LegacySearchingPanel");
        searchingPanel.transform.SetParent(host.transform, false);
        searchingPanel.AddComponent<CanvasGroup>();
        panelGame = new GameObject("SoloGamePanel");
        panelGame.transform.SetParent(host.transform, false);

        transition = host.AddComponent(transitionType);
        SetField("searchingPanel", searchingPanel);
        SetField("panelGame", panelGame);
        searchingPanel.SetActive(false);
        panelGame.SetActive(false);
    }

    [TearDown]
    public void TearDown()
    {
        SetLanguage(initialLanguage);
        UnityEngine.Object.DestroyImmediate(host);
    }

    [Test]
    public void SoloEntryIsImmediateDeterministicAndHasNoDeferredSearchRoutine()
    {
        var deferredMethods = transitionType.GetMethods(BindingFlags.Instance |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly)
            .Where(method => typeof(IEnumerator).IsAssignableFrom(method.ReturnType))
            .Select(method => method.Name)
            .ToArray();
        Assert.That(deferredMethods, Is.Empty,
            "Local Solo entry must not retain delayed search callbacks.");

        for (int attempt = 0; attempt < 64; attempt++)
        {
            panelGame.SetActive(false);
            searchingPanel.SetActive(true);
            searchingPanel.GetComponent<CanvasGroup>().alpha = 0.25f;

            Invoke("StartSearch");

            Assert.That(searchingPanel.activeSelf, Is.False,
                "Solo must never expose the legacy search panel.");
            Assert.That(panelGame.activeSelf, Is.True,
                "Every Solo attempt must enter the AI board synchronously.");
            Assert.That(searchingPanel.GetComponent<CanvasGroup>().alpha,
                Is.EqualTo(1f));
        }
    }

    [Test]
    public void BackCancellationAndRepeatedEntryCannotLeaveAStaleTransition()
    {
        searchingPanel.SetActive(true);
        Invoke("CancelSearch");
        Assert.That(searchingPanel.activeSelf, Is.False);
        Assert.That(panelGame.activeSelf, Is.False);

        Invoke("StartSearch");
        Invoke("StartSearch");
        Assert.That(searchingPanel.activeSelf, Is.False);
        Assert.That(panelGame.activeSelf, Is.True);
    }

    [Test]
    public void EnglishAndGreekCopySeparatesSoloAiFromPrivateRoom()
    {
        AssertLocalizedCopy("English",
            "Play Solo vs AI",
            "Play now vs AI",
            "Solo starts right away against a computer challenger.",
            "Private Room",
            "Play with a friend");

        AssertLocalizedCopy("Greek",
            "Παίξε Solo με AI",
            "Παίξε Solo με AI",
            "Το Solo ξεκινά αμέσως με αντίπαλο τον υπολογιστή.",
            "Ιδιωτικό δωμάτιο",
            "Παίξε με φίλο");
    }

    void AssertLocalizedCopy(string language, string solo, string start,
        string disclosure, string privateRoom, string friend)
    {
        Type languageType = l10nType.GetNestedType("Language",
            BindingFlags.Public);
        SetLanguage(Enum.Parse(languageType, language));

        Assert.That(GetCopy("play_solo"), Is.EqualTo(solo));
        Assert.That(GetCopy("find_challenger"), Is.EqualTo(start));
        Assert.That(GetCopy("simulated_opponents"), Is.EqualTo(disclosure));
        Assert.That(GetCopy("private_room"), Is.EqualTo(privateRoom));
        Assert.That(GetCopy("private_room_title"), Is.EqualTo(friend));
        Assert.That(GetCopy("find_challenger"), Is.Not.EqualTo(privateRoom));
        Assert.That(GetCopy("simulated_opponents"), Is.Not.EqualTo(friend));
    }

    string GetCopy(string key)
    {
        return (string)l10nType.GetMethod("Get",
            BindingFlags.Public | BindingFlags.Static).Invoke(null,
            new object[] { key, new object[0] });
    }

    void SetLanguage(object language)
    {
        l10nType.GetMethod("SetLanguage",
            BindingFlags.Public | BindingFlags.Static).Invoke(null,
            new[] { language });
    }

    void SetField(string name, object value)
    {
        transitionType.GetField(name, BindingFlags.Instance |
            BindingFlags.Public | BindingFlags.NonPublic).SetValue(transition, value);
    }

    void Invoke(string method)
    {
        transitionType.GetMethod(method, BindingFlags.Instance |
            BindingFlags.Public | BindingFlags.NonPublic).Invoke(transition, null);
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
