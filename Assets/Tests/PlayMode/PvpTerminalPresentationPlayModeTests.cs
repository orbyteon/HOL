using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class PvpTerminalPresentationPlayModeTests
{
    [UnityTest]
    public IEnumerator ConnectionLossLocksEveryControlAndExitReturnsToMenu()
    {
        Harness harness = BuildLiveHarness();
        try
        {
            Invoke(harness.controller, "HandleConnectionLost");

            AssertTerminalReason(harness, "ConnectionLost");
            AssertLiveControlsHidden(harness);
            var active = ActiveButtons(harness.matchPanel);
            Assert.That(active.Select(button => button.name),
                Is.EqualTo(new[] { "TerminalExitButton" }),
                "An abnormal live terminal must expose exactly one action.");

            active[0].onClick.Invoke();
            Assert.That(harness.pvpMenuPanel.activeSelf, Is.True);
            Assert.That(harness.matchPanel.activeSelf, Is.False);
            Assert.That((bool)Property(harness.terminal, "IsShown"), Is.False);
        }
        finally
        {
            UnityEngine.Object.Destroy(harness.root);
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator MissingRoomUsesNeutralUnavailableState()
    {
        Harness harness = BuildLiveHarness();
        try
        {
            Invoke(harness.controller, "HandleRoomClosed");
            AssertTerminalReason(harness, "RoomUnavailable");
            string message = TextOf(Field(harness.terminal, "messageText"));
            Assert.That(message, Does.Contain("room").IgnoreCase);
            Assert.That(message, Does.Not.Contain("opponent").IgnoreCase,
                "A missing room is not proof that the opponent left.");
            AssertLiveControlsHidden(harness);
        }
        finally
        {
            UnityEngine.Object.Destroy(harness.root);
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator ProvenOpponentDeparturePreservesAuthoritativeResult()
    {
        Harness harness = BuildLiveHarness();
        try
        {
            object done = NewState("done", "", 0);
            SetState(done, "winner", "guest");
            SetState(done, "revealedSecret", 73);
            SetState(done, "hostGuessCount", 4);
            SetState(done, "guestGuessCount", 3);
            SetState(done, "opponentLeft", true);
            Invoke(harness.controller, "OnState", done);

            AssertTerminalReason(harness, "OpponentLeft");
            Component result = (Component)Field(harness.controller,
                "resultPresentation");
            Assert.That(result.gameObject.activeSelf, Is.True,
                "The legitimate result must remain visible.");
            Assert.That((bool)Property(harness.terminal,
                "PreservesAuthoritativeResult"), Is.True);
            Assert.That(((GameObject)Field(harness.terminal,
                "terminalRoot")).activeSelf, Is.False);
            Assert.That(((GameObject)Field(harness.controller,
                "rematchButton")).activeSelf, Is.False);
            Assert.That(((GameObject)Field(harness.controller,
                "resultSignalsRoot")).activeSelf, Is.False);

            var active = ActiveButtons(harness.matchPanel);
            Assert.That(active.Select(button => button.name),
                Is.EqualTo(new[] { "ResultExitButton" }));
        }
        finally
        {
            UnityEngine.Object.Destroy(harness.root);
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator AuthoritativeDoneStillUsesApprovedNormalResult()
    {
        Harness harness = BuildLiveHarness();
        try
        {
            object done = NewState("done", "", 0);
            SetState(done, "winner", "guest");
            SetState(done, "revealedSecret", 64);
            SetState(done, "hostGuessCount", 5);
            SetState(done, "guestGuessCount", 4);
            Invoke(harness.controller, "OnState", done);

            Assert.That((bool)Property(harness.terminal, "IsShown"), Is.False);
            Component result = (Component)Field(harness.controller,
                "resultPresentation");
            Assert.That(result.gameObject.activeSelf, Is.True);
            Assert.That(((GameObject)Field(harness.controller,
                "rematchButton")).activeSelf, Is.True);
            Assert.That(((GameObject)Field(harness.controller,
                "resultSignalsRoot")).activeSelf, Is.True);
        }
        finally
        {
            UnityEngine.Object.Destroy(harness.root);
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator UnchangedSuccessfulSnapshotsNeverBecomeConnectionLoss()
    {
        Harness harness = BuildLiveHarness();
        try
        {
            int roomRequestEpoch = (int)Field(harness.backend,
                "roomRequestEpoch");
            object unchanged = NewState("play", "host", 0);
            for (int i = 0; i < 225; i++)
                Invoke(harness.controller, "OnState", unchanged);

            Assert.That((bool)Property(harness.terminal, "IsShown"), Is.False);
            Assert.That(harness.matchPanel.activeSelf, Is.True);
            Assert.That(((GameObject)Field(harness.controller,
                "guessButton")).activeSelf, Is.True);
            Assert.That(((GameObject)Field(harness.controller,
                "signalsRoot")).activeSelf, Is.True);
            Assert.That((int)Field(harness.backend, "roomRequestEpoch"),
                Is.EqualTo(roomRequestEpoch),
                "Successful unchanged snapshots must never call DeleteRoom.");
        }
        finally
        {
            UnityEngine.Object.Destroy(harness.root);
        }
        yield return null;
    }

    static Harness BuildLiveHarness()
    {
        var root = new GameObject("PvpHarness", typeof(RectTransform),
            typeof(Canvas), typeof(GraphicRaycaster));
        Component ui = root.AddComponent(RuntimeType("PvpRuntimeUI"));
        ((Behaviour)ui).enabled = false; // prevent Start from constructing twice
        Component backend = root.AddComponent(RuntimeType("PlayFabPvpClient"));
        Component controller = root.AddComponent(RuntimeType("PvpGameController"));
        SetField(controller, "client", backend);
        Invoke(ui, "BuildPanels", controller);
        Invoke(controller, "OpenPvpMenu");

        var menu = (GameObject)Field(controller, "pvpMenuPanel");
        var join = (GameObject)Field(controller, "joinPanel");
        menu.SetActive(false);
        join.SetActive(true);
        Invoke(controller, "BeginMatchPolling");
        Invoke(controller, "OnState", NewState("play", "host", 0));

        return new Harness
        {
            root = root,
            controller = controller,
            backend = backend,
            terminal = (Component)Field(controller, "terminalPresentation"),
            matchPanel = (GameObject)Field(controller, "matchPanel"),
            pvpMenuPanel = menu,
        };
    }

    static object NewState(string phase, string turn, int matchIndex)
    {
        Type backend = RuntimeType("PvpBackend");
        Type stateType = backend.GetNestedType("RoomState", BindingFlags.Public);
        object state = Activator.CreateInstance(stateType);
        SetState(state, "phase", phase);
        SetState(state, "turn", turn);
        SetState(state, "matchIndex", matchIndex);
        SetState(state, "hostName", "Rival");
        SetState(state, "guestName", "Player");
        SetState(state, "opener", "host");
        return state;
    }

    static void AssertTerminalReason(Harness harness, string expected)
    {
        Assert.That((bool)Property(harness.terminal, "IsShown"), Is.True);
        Assert.That(Property(harness.terminal, "Reason").ToString(),
            Is.EqualTo(expected));
    }

    static void AssertLiveControlsHidden(Harness harness)
    {
        foreach (string field in new[]
                 {
                     "guessButton", "keypadRoot", "lockButton", "signalsRoot",
                     "resultSignalsRoot", "rematchButton", "leaveButton"
                 })
        {
            var control = (GameObject)Field(harness.controller, field);
            Assert.That(control.activeSelf, Is.False, field + " stayed visible");
        }
        var guessInput = (Component)Field(harness.controller, "guessInput");
        var rematchInput = (Component)Field(harness.controller,
            "rematchSecretInput");
        Assert.That(guessInput.gameObject.activeSelf, Is.False);
        Assert.That(rematchInput.gameObject.activeSelf, Is.False);
    }

    static Button[] ActiveButtons(GameObject root)
    {
        return root.GetComponentsInChildren<Button>(true)
            .Where(button => button.gameObject.activeInHierarchy)
            .OrderBy(button => button.name, StringComparer.Ordinal)
            .ToArray();
    }

    sealed class Harness
    {
        public GameObject root;
        public Component controller;
        public Component backend;
        public Component terminal;
        public GameObject matchPanel;
        public GameObject pvpMenuPanel;
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

    static void SetState(object state, string name, object value)
    {
        state.GetType().GetField(name, BindingFlags.Public |
            BindingFlags.Instance).SetValue(state, value);
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

    static string TextOf(object text)
    {
        return (string)text.GetType().GetProperty("text").GetValue(text, null);
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
