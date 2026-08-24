using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class SoloModeClarityTests
{
    Type transitionType;
    Type l10nType;
    GameObject host;
    GameObject searchingPanel;
    GameObject panelGame;
    TMP_Text searchingText;
    Component transition;
    object initialLanguage;

    [SetUp]
    public void SetUp()
    {
        transitionType = RuntimeType("FakeMatchmaking");
        l10nType = RuntimeType("L10n");
        initialLanguage = l10nType.GetProperty(
            "Current", BindingFlags.Public | BindingFlags.Static)
            .GetValue(null, null);

        host = new GameObject("SoloEntryOwner");
        searchingPanel = new GameObject(
            "SoloPreparationPanel", typeof(RectTransform));
        searchingPanel.transform.SetParent(host.transform, false);
        searchingPanel.AddComponent<CanvasGroup>();
        panelGame = new GameObject("SoloGamePanel", typeof(RectTransform));
        panelGame.transform.SetParent(host.transform, false);

        var textObject = new GameObject("SearchStatus", typeof(RectTransform));
        textObject.transform.SetParent(searchingPanel.transform, false);
        searchingText = textObject.AddComponent<TextMeshProUGUI>();

        transition = host.AddComponent(transitionType);
        SetField("searchingPanel", searchingPanel);
        SetField("panelGame", panelGame);
        SetField("searchingText", searchingText);
        SetField("preparationSeconds", 0.03f);
        SetField("readyHoldSeconds", 0.02f);
        searchingPanel.SetActive(false);
        panelGame.SetActive(false);
    }

    [TearDown]
    public void TearDown()
    {
        SetLanguage(initialLanguage);
        UnityEngine.Object.DestroyImmediate(host);
    }

    [UnityTest]
    public IEnumerator SoloEntryUsesOneTruthfulDeterministicAiPreparation()
    {
        Assert.That(transitionType.GetMethod(
            "PrepareComputerChallenger",
            BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null,
            "Solo should own exactly one explicit AI preparation routine.");

        Invoke("StartSearch");
        Assert.That(searchingPanel.activeSelf, Is.True);
        Assert.That(panelGame.activeSelf, Is.False);
        Assert.That(GetProperty<bool>("IsPreparing"), Is.True);
        Assert.That(searchingText.text, Does.Contain("AI"));

        yield return new WaitForSecondsRealtime(0.08f);
        yield return null;

        Assert.That(searchingPanel.activeSelf, Is.False);
        Assert.That(panelGame.activeSelf, Is.True,
            "The deterministic preparation must always enter the local AI board.");
        Assert.That(GetProperty<bool>("IsPreparing"), Is.False);
    }

    [UnityTest]
    public IEnumerator CancelAndRepeatedEntryCannotLeaveAStaleCallback()
    {
        Invoke("StartSearch");
        Assert.That(searchingPanel.activeSelf, Is.True);
        Invoke("CancelSearch");
        Assert.That(searchingPanel.activeSelf, Is.False);
        Assert.That(panelGame.activeSelf, Is.False);
        Assert.That(GetProperty<bool>("IsPreparing"), Is.False);

        yield return new WaitForSecondsRealtime(0.08f);
        Assert.That(panelGame.activeSelf, Is.False,
            "A cancelled preparation must not reopen gameplay later.");

        Invoke("StartSearch");
        Invoke("StartSearch");
        Assert.That(searchingPanel.activeSelf, Is.True);
        Assert.That(panelGame.activeSelf, Is.False);
        yield return new WaitForSecondsRealtime(0.08f);
        yield return null;
        Assert.That(searchingPanel.activeSelf, Is.False);
        Assert.That(panelGame.activeSelf, Is.True,
            "Repeated entry must resolve once without a stale transition.");
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

    void AssertLocalizedCopy(
        string language,
        string solo,
        string start,
        string disclosure,
        string privateRoom,
        string friend)
    {
        Type languageType = l10nType.GetNestedType(
            "Language", BindingFlags.Public);
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
        return (string)l10nType.GetMethod(
            "Get", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new object[] { key, new object[0] });
    }

    T GetProperty<T>(string name)
    {
        PropertyInfo property = transitionType.GetProperty(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, name);
        return (T)property.GetValue(transition);
    }

    void SetLanguage(object language)
    {
        l10nType.GetMethod(
            "SetLanguage", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new[] { language });
    }

    void SetField(string name, object value)
    {
        FieldInfo field = transitionType.GetField(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        field.SetValue(transition, value);
    }

    void Invoke(string method)
    {
        transitionType.GetMethod(
            method, BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic)
            .Invoke(transition, null);
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
