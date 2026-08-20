using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class PvpTerminalAndHistoryTests
{
    [UnityTest]
    public IEnumerator TerminalReasonsAreTruthfulAndRepaintInGreek()
    {
        yield return new EnterPlayMode();

        int subscriberBaseline = LanguageSubscriberCount();
        var host = new GameObject("TerminalOwner");
        host.SetActive(false);
        Component presentation = host.AddComponent(
            RuntimeType("PvpTerminalPresentation"));
        var root = Child(host.transform, "TerminalRoot");
        var title = Text(root.transform, "Title");
        var message = Text(root.transform, "Message");
        var status = Text(host.transform, "ResultStatus");
        var terminalExit = Child(root.transform, "TerminalExit");
        var resultExit = Child(host.transform, "ResultExit");

        SetField(presentation, "terminalRoot", root);
        SetField(presentation, "titleText", title);
        SetField(presentation, "messageText", message);
        SetField(presentation, "resultStatusText", status);
        SetField(presentation, "terminalExitButton", terminalExit);
        SetField(presentation, "resultExitButton", resultExit);

        SetLanguage("English");
        ShowTerminal(presentation, "RoomUnavailable", false);
        host.SetActive(true);
        Assert.That(LanguageSubscriberCount(), Is.EqualTo(subscriberBaseline + 1));
        string unavailable = message.text;
        Assert.That(unavailable, Does.Contain("room").IgnoreCase);
        Assert.That(unavailable, Does.Not.Contain("opponent").IgnoreCase);

        ShowTerminal(presentation, "OpponentLeft", false);
        Assert.That(message.text, Does.Contain("opponent").IgnoreCase);
        Assert.That(message.text, Is.Not.EqualTo(unavailable));

        ShowTerminal(presentation, "ConnectionLost", false);
        string english = message.text;
        int shownEvents = (bool)Property(presentation, "IsShown") ? 1 : 0;
        SetLanguage("Greek");
        string greek = message.text;
        Assert.That(greek, Is.Not.EqualTo(english));
        Assert.That((bool)Property(presentation, "IsShown"), Is.True);
        Assert.That(shownEvents, Is.EqualTo(1));

        ((Behaviour)presentation).enabled = false;
        Assert.That(LanguageSubscriberCount(), Is.EqualTo(subscriberBaseline));
        SetLanguage("English");
        Assert.That(message.text, Is.EqualTo(greek),
            "A disabled terminal must not retain a language subscription.");
        ((Behaviour)presentation).enabled = true;
        Assert.That(LanguageSubscriberCount(), Is.EqualTo(subscriberBaseline + 1));
        Assert.That(message.text, Is.EqualTo(english),
            "Re-enabling must repaint the retained terminal state.");

        ShowTerminal(presentation, "OpponentLeft", true);
        Assert.That(root.activeSelf, Is.False);
        Assert.That(terminalExit.activeSelf, Is.False);
        Assert.That(resultExit.activeSelf, Is.True);
        Assert.That(status.text, Is.Not.Empty);

        ((Behaviour)presentation).enabled = false;
        Assert.That(LanguageSubscriberCount(), Is.EqualTo(subscriberBaseline));
        UnityEngine.Object.Destroy(host);
        yield return null;
        Assert.That(LanguageSubscriberCount(), Is.EqualTo(subscriberBaseline));
        yield return new ExitPlayMode();
    }

    [Test]
    public void TypedHistoryKeepsExactRepeatsIdempotentAndNewestFirst()
    {
        var host = new GameObject("HistoryOwner");
        try
        {
            Component rail = NewRail(host);
            Reset(rail, 0);

            Assert.That(Record(rail, 0, "host", 1, 1, 42, "higher",
                false, true, "Rival"), Is.True);
            Assert.That(Record(rail, 0, "host", 1, 1, 42, "higher",
                false, true, "Rival"), Is.False,
                "Repeated polling of one authoritative event must be idempotent.");
            Assert.That(Record(rail, 0, "host", 2, 2, 42, "higher",
                false, true, "Rival"), Is.True,
                "An identical value with the next side ordinal is a new event.");
            Assert.That(Record(rail, 0, "guest", 1, 3, 50, "lower",
                false, false, "Rival"), Is.True);
            Assert.That(Record(rail, 0, "host", 3, 4, 55, "higher",
                false, true, "Rival"), Is.True);
            Assert.That(Record(rail, 0, "guest", 2, 5, 60, "lower",
                false, false, "Rival"), Is.True);

            Assert.That((int)Property(rail, "EventCount"), Is.EqualTo(4));
            Assert.That(Invoke(rail, "IdentityAt", 0), Is.EqualTo("0:guest:2"));
            Assert.That(Invoke(rail, "IdentityAt", 3), Is.EqualTo("0:host:2"));
            Assert.That(Invoke(rail, "ValueAt", 0), Is.EqualTo(60));
            Assert.That(Invoke(rail, "ValueAt", 3), Is.EqualTo(42));

            var source = (TMP_Text)Field(rail, "source");
            var target = (TMP_Text)Field(rail, "target");
            Assert.That(source.text, Does.Contain("60"));
            Assert.That(target.text.IndexOf("55", StringComparison.Ordinal),
                Is.LessThan(target.text.IndexOf("50", StringComparison.Ordinal)));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [UnityTest]
    public IEnumerator HistoryRejectsStaleSnapshotsAndResetsSynchronously()
    {
        yield return new EnterPlayMode();

        int subscriberBaseline = LanguageSubscriberCount();
        var host = new GameObject("HistoryFence");
        host.SetActive(false);
        SetLanguage("English");
        Component rail = NewRail(host);
        Assert.That(LanguageSubscriberCount(), Is.EqualTo(subscriberBaseline));
        Reset(rail, 3);
        Assert.That(Record(rail, 3, "host", 1, 1, 40, "higher",
            false, true, "Rival"), Is.True);
        Assert.That(Record(rail, 3, "guest", 1, 2, 70, "lower",
            false, false, "Rival"), Is.True);
        Assert.That(Record(rail, 3, "host", 2, 3, 55, "higher",
            false, true, "Rival"), Is.True);

        Assert.That(Record(rail, 2, "guest", 2, 4, 60, "lower",
            false, false, "Rival"), Is.False,
            "An older match must never enter the current rail.");
        Assert.That(Record(rail, 3, "guest", 2, 2, 60, "lower",
            false, false, "Rival"), Is.False,
            "A lower-total same-match snapshot must not roll history back.");
        Assert.That((int)Property(rail, "EventCount"), Is.EqualTo(3));

        string identity = (string)Invoke(rail, "IdentityAt", 0);
        string english = ((TMP_Text)Field(rail, "source")).text;
        SetLanguage("Greek");
        Assert.That(((TMP_Text)Field(rail, "source")).text, Is.EqualTo(english),
            "An inactive rail must not receive language events.");
        host.SetActive(true);
        Assert.That(LanguageSubscriberCount(), Is.EqualTo(subscriberBaseline + 1));
        string greek = ((TMP_Text)Field(rail, "source")).text;
        Assert.That(greek, Is.Not.EqualTo(english),
            "Enabling must repaint existing events in the current language.");
        Assert.That(Invoke(rail, "IdentityAt", 0), Is.EqualTo(identity));
        Assert.That((int)Property(rail, "EventCount"), Is.EqualTo(3));

        host.SetActive(false);
        Assert.That(LanguageSubscriberCount(), Is.EqualTo(subscriberBaseline));
        SetLanguage("English");
        Assert.That(((TMP_Text)Field(rail, "source")).text, Is.EqualTo(greek));
        host.SetActive(true);
        Assert.That(LanguageSubscriberCount(), Is.EqualTo(subscriberBaseline + 1));
        Assert.That(((TMP_Text)Field(rail, "source")).text, Is.EqualTo(english));
        Assert.That(Invoke(rail, "IdentityAt", 0), Is.EqualTo(identity));
        Assert.That((int)Property(rail, "EventCount"), Is.EqualTo(3));

        Reset(rail, 4);
        Assert.That((int)Property(rail, "EventCount"), Is.Zero);
        Assert.That(((TMP_Text)Field(rail, "source")).text, Is.Empty);
        Assert.That(((TMP_Text)Field(rail, "target")).text, Is.Empty);

        host.SetActive(false);
        Assert.That(LanguageSubscriberCount(), Is.EqualTo(subscriberBaseline));
        UnityEngine.Object.Destroy(host);
        yield return null;
        Assert.That(LanguageSubscriberCount(), Is.EqualTo(subscriberBaseline));
        yield return new ExitPlayMode();
    }

    static Component NewRail(GameObject host)
    {
        Component rail = host.AddComponent(RuntimeType("GuessHistoryRail"));
        SetField(rail, "source", Text(host.transform, "Latest"));
        SetField(rail, "target", Text(host.transform, "Previous"));
        Invoke(rail, "Repaint");
        return rail;
    }

    static void Reset(Component rail, int matchIndex)
    {
        Invoke(rail, "ResetForMatch", matchIndex);
    }

    static bool Record(Component rail, int matchIndex, string side,
        int ordinal, int total, int value, string hint, bool locked,
        bool isMine, string opponentName)
    {
        return (bool)Invoke(rail, "Record", matchIndex, side, ordinal, total,
            value, hint, locked, isMine, opponentName);
    }

    static void ShowTerminal(Component presentation, string reason,
        bool preserveResult)
    {
        Type reasonType = RuntimeType("PvpTerminalReason");
        Invoke(presentation, "Show", Enum.Parse(reasonType, reason),
            preserveResult);
    }

    static void SetLanguage(string name)
    {
        Type l10n = RuntimeType("L10n");
        Type language = l10n.GetNestedType("Language", BindingFlags.Public);
        l10n.GetMethod("SetLanguage", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new[] { Enum.Parse(language, name) });
    }

    static int LanguageSubscriberCount()
    {
        var callbacks = RuntimeType("L10n")
            .GetField("OnLanguageChanged", BindingFlags.Public |
                BindingFlags.Static).GetValue(null) as Delegate;
        return callbacks == null ? 0 : callbacks.GetInvocationList().Length;
    }

    static Type RuntimeType(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(name);
            if (type != null) return type;
        }
        Assert.Fail("Runtime type not found: " + name);
        return null;
    }

    static GameObject Child(Transform parent, string name)
    {
        var child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child;
    }

    static TMP_Text Text(Transform parent, string name)
    {
        return Child(parent, name).AddComponent<TextMeshProUGUI>();
    }

    static void SetField(Component target, string name, object value)
    {
        target.GetType().GetField(name, BindingFlags.Public |
            BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }

    static object Field(Component target, string name)
    {
        return target.GetType().GetField(name, BindingFlags.Public |
            BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    }

    static object Property(Component target, string name)
    {
        return target.GetType().GetProperty(name, BindingFlags.Public |
            BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target, null);
    }

    static object Invoke(Component target, string name, params object[] args)
    {
        foreach (var method in target.GetType().GetMethods(BindingFlags.Public |
                     BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (method.Name == name && method.GetParameters().Length == args.Length)
                return method.Invoke(target, args);
        }
        Assert.Fail("Method not found: " + target.GetType().Name + "." + name);
        return null;
    }
}
