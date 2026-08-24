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
    bool boardReady;

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
        SetField(transition, "searchingPanel", searchingPanel);
        SetField(transition, "panelGame", panelGame);
        SetField(transition, "searchingText", searchingText);

        boardReady = false;
        PropertyInfo probe = transitionType.GetProperty(
            "BoardReadyProbe",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(probe, Is.Not.Null, "Missing deterministic readiness seam.");
        probe.SetValue(transition, new Func<bool>(() => boardReady));

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
    public IEnumerator SoloEntryWaitsOnlyForTheRealLocalBoard()
    {
        Assert.That(transitionType.GetMethod(
            "TickPreparation",
            BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null,
            "Solo should expose one deterministic preparation tick.");

        Invoke("StartSearch");

        Assert.That(searchingPanel.activeSelf, Is.True);
        Assert.That(panelGame.activeSelf, Is.True,
            "The real local board must initialize immediately behind the modal.");
        Assert.That(GetProperty<bool>("IsPreparing"), Is.True);
        Assert.That(searchingText.text,
            Is.EqualTo(GetCopy("solo_ai_preparing")));

        Invoke("TickPreparation");
        Assert.That(searchingPanel.activeSelf, Is.True,
            "The modal must remain blocking until the board reports readiness.");

        MakeBoardReady();
        Invoke("TickPreparation");
        Assert.That(searchingPanel.activeSelf, Is.True,
            "The localized ready state must render for one engine update.");
        Assert.That(searchingText.text, Is.EqualTo(GetCopy("solo_ai_ready")));

        Invoke("TickPreparation");
        yield return null;

        Assert.That(searchingPanel.activeSelf, Is.False);
        Assert.That(panelGame.activeSelf, Is.True,
            "A ready local board must be revealed without an artificial timer.");
        Assert.That(GetProperty<bool>("IsPreparing"), Is.False);
    }

    [UnityTest]
    public IEnumerator CancelAndRepeatedEntryCannotLeaveAStaleCallback()
    {
        Invoke("StartSearch");
        Assert.That(searchingPanel.activeSelf, Is.True);
        Assert.That(panelGame.activeSelf, Is.True);
        Assert.That(searchingText.text,
            Is.EqualTo(GetCopy("solo_ai_preparing")));

        Invoke("CancelSearch");
        Assert.That(searchingPanel.activeSelf, Is.False);
        Assert.That(panelGame.activeSelf, Is.False);
        Assert.That(GetProperty<bool>("IsPreparing"), Is.False);

        MakeBoardReady();
        Invoke("TickPreparation");
        Invoke("TickPreparation");
        yield return null;
        Assert.That(panelGame.activeSelf, Is.False,
            "A cancelled preparation must not reopen gameplay later.");

        boardReady = false;
        Invoke("StartSearch");
        Invoke("StartSearch");
        Assert.That(searchingPanel.activeSelf, Is.True);
        Assert.That(panelGame.activeSelf, Is.True);
        Assert.That(GetProperty<bool>("IsPreparing"), Is.True);

        MakeBoardReady();
        Invoke("TickPreparation");
        Assert.That(searchingPanel.activeSelf, Is.True);
        Assert.That(searchingText.text, Is.EqualTo(GetCopy("solo_ai_ready")));
        Invoke("TickPreparation");
        yield return null;

        Assert.That(searchingPanel.activeSelf, Is.False);
        Assert.That(panelGame.activeSelf, Is.True,
            "Repeated entry must resolve once when the actual board becomes ready.");
        Assert.That(GetProperty<bool>("IsPreparing"), Is.False);
    }

    [Test]
    public void EnglishAndGreekCopySeparatesSoloAiFromPrivateRoom()
    {
        AssertLocalizedCopy("English",
            "Play Solo vs AI",
            "Play now vs AI",
            "Solo starts right away against a computer challenger.",
            "PREPARING AI OPPONENT",
            "AI OPPONENT READY!",
            "Private Room",
            "Play with a friend");

        AssertLocalizedCopy("Greek",
            "Παίξε Solo με AI",
            "Παίξε Solo με AI",
            "Το Solo ξεκινά αμέσως με αντίπαλο τον υπολογιστή.",
            "ΠΡΟΕΤΟΙΜΑΣΙΑ AI ΑΝΤΙΠΑΛΟΥ",
            "Ο AI ΑΝΤΙΠΑΛΟΣ ΕΙΝΑΙ ΕΤΟΙΜΟΣ!",
            "Ιδιωτικό δωμάτιο",
            "Παίξε με φίλο");
    }

    void AssertLocalizedCopy(
        string language,
        string solo,
        string start,
        string disclosure,
        string preparing,
        string ready,
        string privateRoom,
        string friend)
    {
        Type languageType = l10nType.GetNestedType(
            "Language", BindingFlags.Public);
        SetLanguage(Enum.Parse(languageType, language));

        Assert.That(GetCopy("play_solo"), Is.EqualTo(solo));
        Assert.That(GetCopy("find_challenger"), Is.EqualTo(start));
        Assert.That(GetCopy("simulated_opponents"), Is.EqualTo(disclosure));
        Assert.That(GetCopy("solo_ai_preparing"), Is.EqualTo(preparing));
        Assert.That(GetCopy("solo_ai_ready"), Is.EqualTo(ready));
        Assert.That(GetCopy("private_room"), Is.EqualTo(privateRoom));
        Assert.That(GetCopy("private_room_title"), Is.EqualTo(friend));
        Assert.That(GetCopy("find_challenger"), Is.Not.EqualTo(privateRoom));
        Assert.That(GetCopy("simulated_opponents"), Is.Not.EqualTo(friend));
    }

    void MakeBoardReady()
    {
        boardReady = true;
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

    void Invoke(string method)
    {
        MethodInfo target = transitionType.GetMethod(
            method, BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic);
        Assert.That(target, Is.Not.Null, method);
        target.Invoke(transition, null);
    }

    static void SetField(Component target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        field.SetValue(target, value);
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
