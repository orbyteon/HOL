using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class ExactReferenceAssetsTests
{
    [Test]
    public void ApprovedHolLogoLoadsAsSprite()
    {
        var logo = Resources.Load<Sprite>("reference/hol_logo_exact");
        Assert.IsNotNull(logo,
            "The approved HOL logo must import as a Sprite at Resources/reference/hol_logo_exact.");
    }

    [TestCase("reference/player_cyan_exact")]
    [TestCase("reference/opponent_purple_exact")]
    [TestCase("reference/mascot_6_exact")]
    [TestCase("reference/mascot_7_exact")]
    [TestCase("reference/char_boy_exact")]
    [TestCase("reference/char_girl_exact")]
    public void ApprovedCharacterPortraitLoadsAsSprite(string path)
    {
        Assert.IsNotNull(Resources.Load<Sprite>(path),
            "The approved portrait must import as a Sprite at Resources/" + path + ".");
    }

    [TestCase("reference/board_vs_burst_exact")]
    [TestCase("reference/board_rocket_exact")]
    [TestCase("reference/board_trophy_exact")]
    [TestCase("reference/board_join_exact")]
    public void ProductionCompanionArtLoadsAsSprite(string path)
    {
        Assert.IsNotNull(Resources.Load<Sprite>(path),
            "Approved companion art must import as a Sprite at Resources/" + path + ".");
    }

    [Test]
    public void PrivateRoomCopyHasEnglishAndGreekEntries()
    {
        var l10n = RuntimeType("L10n");
        var field = l10n.GetField("Table", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(field, "L10n.Table field not found — renamed?");
        var table = (IDictionary)field.GetValue(null);

        foreach (var key in new[]
        {
            "private_room_title", "private_room_tip", "prebattle_title",
            "prebattle_you", "prebattle_opponent", "prebattle_found",
            "prebattle_rule_title", "prebattle_rule", "prebattle_waiting",
            "result_page_title", "result_attempts", "result_attempts_short",
            "result_rematch_heading", "result_reactions", "result_exit",
            "result_win_title", "result_loss_title", "result_draw_title",
            "prebattle_waiting_short", "versus", "settings_change",
            "language_english", "language_greek", "solo_search_title"
        })
        {
            Assert.IsTrue(table.Contains(key), "Missing L10n key: " + key);
            var pair = (string[])table[key];
            Assert.AreEqual(2, pair.Length, key + " must have EN and EL entries.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(pair[0]), key + " English is empty.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(pair[1]), key + " Greek is empty.");
        }
    }

    [Test]
    public void PrebattleBuildCreatesOnePanelPerFlow()
    {
        var root = new GameObject("PvpRoot", typeof(RectTransform),
            typeof(Canvas), typeof(GraphicRaycaster));
        try
        {
            var ui = root.AddComponent(RuntimeType("PvpRuntimeUI"));
            var controller = root.AddComponent(RuntimeType("PvpGameController"));
            InvokePrivate(ui, "BuildPanels", controller);

            Assert.AreEqual(1, DirectChildCount(root.transform, "PvPCreatePanel"),
                "Pre-battle must not leave a second create panel behind.");
            Assert.AreEqual(1, DirectChildCount(root.transform, "PvPJoinPanel"),
                "Pre-battle must not leave a second join panel behind.");
            var create = FindDescendant(root.transform, "PvPCreatePanel");
            var join = FindDescendant(root.transform, "PvPJoinPanel");
            Assert.AreEqual(1, DescendantCount(create, "EntryState"));
            Assert.AreEqual(1, DescendantCount(create, "WaitingState"));
            Assert.AreEqual(1, DescendantCount(join, "EntryState"));
            Assert.AreEqual(1, DescendantCount(join, "WaitingState"));
            Assert.AreEqual(0, DescendantCount(join, "ShareButton"),
                "Join waiting must not expose an inert Share action.");
            Assert.AreEqual(0, DescendantCount(join, "RoomCodeFrame"),
                "Join waiting must not show a stale placeholder code.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void ResultPresentationAndVictoryPopContractsExist()
    {
        Assert.IsNotNull(RuntimeType("PvpResultPresentation"),
            "The portrait result overlay needs a focused presentation owner.");

        var confetti = RuntimeType("ConfettiBurst");
        Assert.IsNotNull(confetti.GetField("popTarget"));
        Assert.IsNotNull(confetti.GetField("radial"));
        Assert.IsNotNull(confetti.GetField("secondaryPieces"));
    }

    [Test]
    public void ResultOverlayBuildsOneRootAndSixSignals()
    {
        var host = new GameObject("ResultHost", typeof(RectTransform),
            typeof(Canvas), typeof(GraphicRaycaster));
        try
        {
            var ui = host.AddComponent(RuntimeType("PvpRuntimeUI"));
            var controller = host.AddComponent(RuntimeType("PvpGameController"));
            var match = Child(host.transform, "PvPMatchPanel");

            InvokePrivate(ui, "BuildResultOverlay", controller, match);

            Assert.AreEqual(1, DescendantCount(match.transform, "ResultVisualRoot"));
            for (int i = 0; i < 6; i++)
                Assert.AreEqual(1, DescendantCount(match.transform,
                    "ResultSignal" + i));
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void RetiredResultReskinTypesArePhysicallyAbsent()
    {
        foreach (string typeName in new[]
        {
            "ExactReferenceVisuals",
            "AttachmentReskinVisuals",
            "AttachmentReskinPolish",
            "AttachmentReskinCanvasBindings"
        })
        {
            Assert.IsNull(System.Type.GetType(typeName + ", Assembly-CSharp"),
                "Retired visual owner returned to runtime: " + typeName);
        }
    }

    [Test]
    public void ResultPresentationPaintsAuthoritativeValues()
    {
        var root = new GameObject("ResultPresentation", typeof(RectTransform));
        try
        {
            var presentation = root.AddComponent(RuntimeType("PvpResultPresentation"));
            var title = TmpText(root.transform, "Title");
            var mine = TmpText(root.transform, "Mine");
            var theirs = TmpText(root.transform, "Theirs");
            var revealed = TmpText(root.transform, "Revealed");

            SetPublicField(presentation, "titleText", title);
            SetPublicField(presentation, "playerAttemptsText", mine);
            SetPublicField(presentation, "opponentAttemptsText", theirs);
            SetPublicField(presentation, "revealedNumberText", revealed);

            InvokePublic(presentation, "Show", "WIN", 5, 7, 67);

            Assert.AreEqual("WIN", TextOf(title));
            Assert.AreEqual("5", TextOf(mine));
            Assert.AreEqual("7", TextOf(theirs));
            Assert.IsTrue(TextOf(revealed).Contains("67"));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void DailyHuntRefreshesFormattedLabelsWhenLanguageChanges()
    {
        var root = new GameObject("DailyLanguage");
        Component hunt = null;
        try
        {
            hunt = root.AddComponent(RuntimeType("DailyHunt"));
            SetPrivateField(hunt, "title", TmpText(root.transform, "Title"));
            SetPrivateField(hunt, "status", TmpText(root.transform, "Status"));
            SetPrivateField(hunt, "trailText", TmpText(root.transform, "Trail"));
            SetPrivateField(hunt, "streakText", TmpText(root.transform, "Streak"));
            SetPrivateField(hunt, "reviveLabel", TmpText(root.transform, "Revive"));
            SetPrivateField(hunt, "input", Child(root.transform, "Input")
                .AddComponent(System.Type.GetType(
                    "TMPro.TMP_InputField, Unity.TextMeshPro")));
            SetPrivateField(hunt, "guessButton",
                Child(root.transform, "GuessButton").AddComponent<Button>());
            SetPrivateField(hunt, "reviveButton",
                Child(root.transform, "ReviveButton").AddComponent<Button>());
            SetPrivateField(hunt, "shareButton",
                Child(root.transform, "ShareButton").AddComponent<Button>());
            SetPrivateField(hunt, "day", 1);
            SetPrivateField(hunt, "budget", 7);
            SetPrivateField(hunt, "done", true);

            SetLanguage("English");
            InvokePrivate(hunt, "OnEnable");
            InvokePrivate(hunt, "Refresh");
            string englishTitle = TextOf((Component)GetPrivateField(hunt, "title"));
            string englishRevive = TextOf((Component)GetPrivateField(hunt, "reviveLabel"));

            SetLanguage("Greek");

            Assert.AreNotEqual(englishTitle,
                TextOf((Component)GetPrivateField(hunt, "title")));
            Assert.AreNotEqual(englishRevive,
                TextOf((Component)GetPrivateField(hunt, "reviveLabel")));
        }
        finally
        {
            InvokeIfPresent(hunt, "OnDisable");
            SetLanguage("English");
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void ResultPresentationRefreshesLocalizedDynamicLabelsWhenLanguageChanges()
    {
        var root = new GameObject("ResultLanguage", typeof(RectTransform));
        Component presentation = null;
        try
        {
            presentation = root.AddComponent(RuntimeType("PvpResultPresentation"));
            var title = TmpText(root.transform, "Title");
            var revealed = TmpText(root.transform, "Revealed");
            var chip = TmpText(root.transform, "Chip");
            SetPublicField(presentation, "titleText", title);
            SetPublicField(presentation, "revealedNumberText", revealed);
            SetPublicField(presentation, "playerChipText", chip);

            SetLanguage("English");
            InvokePrivate(presentation, "OnEnable");
            InvokePublic(presentation, "ShowLocalized",
                "result_win_title", 5, 7, 67, true);
            string englishTitle = TextOf(title);
            string englishRevealed = TextOf(revealed);
            string englishChip = TextOf(chip);

            SetLanguage("Greek");

            Assert.AreNotEqual(englishTitle, TextOf(title));
            Assert.AreNotEqual(englishRevealed, TextOf(revealed));
            Assert.AreNotEqual(englishChip, TextOf(chip));
        }
        finally
        {
            InvokeIfPresent(presentation, "OnDisable");
            SetLanguage("English");
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void InterruptedVictoryPopRestoresTargetScale()
    {
        var root = new GameObject("Confetti", typeof(RectTransform));
        var target = Child(root.transform, "PopTarget");
        try
        {
            var confetti = root.AddComponent(RuntimeType("ConfettiBurst"));
            var rect = (RectTransform)target.transform;
            rect.localScale = new Vector3(0.82f, 0.82f, 0.82f);

            SetPublicField(confetti, "popTarget", rect);
            SetPrivateField(confetti, "popBaseScale", Vector3.one);
            SetPrivateField(confetti, "popBaseCaptured", true);
            InvokePrivate(confetti, "OnDisable");

            Assert.IsTrue(Mathf.Abs(rect.localScale.x - 1f) < 0.001f);
            Assert.IsTrue(Mathf.Abs(rect.localScale.y - 1f) < 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void ResultAttemptCountsMapAuthoritativeRoomSides()
    {
        var backend = RuntimeType("PvpBackend");
        var roomType = backend.GetNestedType("RoomState", BindingFlags.Public);
        Assert.IsNotNull(roomType);
        var state = System.Activator.CreateInstance(roomType);
        roomType.GetField("hostGuessCount").SetValue(state, 5);
        roomType.GetField("guestGuessCount").SetValue(state, 7);

        var controller = RuntimeType("PvpGameController");
        var method = controller.GetMethod("ResultAttemptCounts",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);

        object[] hostArgs = { state, "host", 0, 0 };
        method.Invoke(null, hostArgs);
        Assert.AreEqual(5, hostArgs[2]);
        Assert.AreEqual(7, hostArgs[3]);

        object[] guestArgs = { state, "guest", 0, 0 };
        method.Invoke(null, guestArgs);
        Assert.AreEqual(7, guestArgs[2]);
        Assert.AreEqual(5, guestArgs[3]);
    }

    [Test]
    public void SignalCallbackFenceIncludesFlowGenerationAndMatch()
    {
        var host = new GameObject("SignalFence");
        try
        {
            var controller = host.AddComponent(RuntimeType("PvpGameController"));
            var backend = RuntimeType("PvpBackend");
            var roomType = backend.GetNestedType("RoomState", BindingFlags.Public);
            var state = System.Activator.CreateInstance(roomType);
            roomType.GetField("matchIndex").SetValue(state, 3);

            SetPrivateField(controller, "flowGeneration", 8);
            SetPrivateField(controller, "lastState", state);

            Assert.IsTrue((bool)InvokePrivateResult(controller,
                "IsCurrentSignalCallback", 8, 3));
            Assert.IsFalse((bool)InvokePrivateResult(controller,
                "IsCurrentSignalCallback", 7, 3));
            Assert.IsFalse((bool)InvokePrivateResult(controller,
                "IsCurrentSignalCallback", 8, 2));
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void StaleGuessResponseDoesNotOverwriteNewerMatchState()
    {
        var host = new GameObject("StaleGuessFence");
        try
        {
            var client = host.AddComponent(RuntimeType("PlayFabPvpClient"));
            var backend = RuntimeType("PvpBackend");
            var roomType = backend.GetNestedType("RoomState", BindingFlags.Public);
            var current = System.Activator.CreateInstance(roomType);
            roomType.GetField("matchIndex").SetValue(current, 1);
            roomType.GetField("hostGuessCount").SetValue(current, 0);
            roomType.GetField("phase").SetValue(current, "play");

            InvokePrivate(client, "ApplyReturnedState", current,
                "{\"ok\":true,\"state\":\"{\\\"matchIndex\\\":0," +
                "\\\"hostGuessCount\\\":9,\\\"phase\\\":\\\"done\\\"}\"}");

            Assert.AreEqual(1, roomType.GetField("matchIndex").GetValue(current));
            Assert.AreEqual(0, roomType.GetField("hostGuessCount").GetValue(current));
            Assert.AreEqual("play", roomType.GetField("phase").GetValue(current));

            InvokePrivate(client, "ApplyReturnedState", current,
                "{\"ok\":true,\"state\":\"{\\\"matchIndex\\\":1," +
                "\\\"hostGuessCount\\\":2,\\\"phase\\\":\\\"play\\\"}\"}");
            Assert.AreEqual(2, roomType.GetField("hostGuessCount").GetValue(current));
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void LeavingInvalidatesPendingPlayFabRoomRequest()
    {
        var host = new GameObject("PlayFabRoomFence");
        try
        {
            var client = host.AddComponent(RuntimeType("PlayFabPvpClient"));
            var epoch = client.GetType().GetField("roomRequestEpoch",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(epoch);
            int before = (int)epoch.GetValue(client);

            InvokePublic(client, "DeleteRoom");

            Assert.AreEqual(before + 1, (int)epoch.GetValue(client),
                "Cancel/leave must invalidate every in-flight create/join.");
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void SameCodeRetryAdoptsTheSuccessfulPendingJoin()
    {
        var host = new GameObject("PlayFabJoinRetry");
        try
        {
            var client = host.AddComponent(RuntimeType("PlayFabPvpClient"));
            bool completed = false;
            System.Action<bool, string> done = (ok, _) => completed = ok;
            SetPrivateField(client, "roomRequestEpoch", 2);
            SetPrivateField(client, "pendingRoomCode", "ABCDE");
            SetPrivateField(client, "pendingRoomDone", done);
            SetPrivateField(client, "pendingRequestIsJoin", true);

            bool adopted = (bool)InvokePrivateResult(client,
                "TryAdoptPendingJoin", "ABCDE");

            Assert.IsTrue(adopted);
            Assert.IsTrue(completed);
            var code = client.GetType().GetProperty("RoomCode").GetValue(client, null);
            Assert.AreEqual("ABCDE", code);
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void SettingsPresentationContractExists()
    {
        Assert.IsNotNull(RuntimeType("SettingsVisuals"));
        var l10n = RuntimeType("L10n");
        var table = (IDictionary)l10n.GetField("Table",
            BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        Assert.IsTrue(table.Contains("settings_title"));
        Assert.IsNotNull(Resources.Load<Sprite>("settings/hol_settings_bg_r1"));
    }

    [Test]
    public void DailyHuntPresentationContractExists()
    {
        Assert.IsNotNull(RuntimeType("DailyHuntVisuals"));
        Assert.IsNotNull(Resources.Load<Sprite>("phase2a/hol_neon_reference_bg_r3"));
        Assert.IsNotNull(Resources.Load<Sprite>("reference/hol_logo_exact"));
        Assert.IsNotNull(Resources.Load<Sprite>("reference/mascot_6_exact"));
        Assert.IsNotNull(Resources.Load<Sprite>("reference/mascot_7_exact"));
    }

    [Test]
    public void SoloSearchPresentationUsesProductionArtWithoutRadarProceduralTypes()
    {
        Assert.IsNotNull(RuntimeType("SoloSearchVisuals"));
        Assert.IsNull(System.Type.GetType("RadarScanner, Assembly-CSharp"));
        Assert.IsNull(System.Type.GetType("RadarPulse, Assembly-CSharp"));
        Assert.IsNotNull(Resources.Load<Sprite>("reference/board_vs_burst_exact"));
        Assert.IsNotNull(Resources.Load<Sprite>("reference/board_rocket_exact"));
    }

    [Test]
    public void ModernPrebattleHasNoRetiredReskinRuntimeOwner()
    {
        Assert.IsNull(System.Type.GetType("AttachmentReskinVisuals, Assembly-CSharp"));
        Assert.IsNull(System.Type.GetType("AttachmentReskinPolish, Assembly-CSharp"));
        Assert.IsNull(System.Type.GetType("AttachmentReskinCanvasBindings, Assembly-CSharp"));
    }

    [Test]
    public void DailyCloseWaitsForInFlightReviveSettlement()
    {
        var host = new GameObject("DailyRevive");
        try
        {
            var hunt = host.AddComponent(RuntimeType("DailyHunt"));
            SetPrivateField(hunt, "done", false);
            SetPrivateField(hunt, "used", 7);
            SetPrivateField(hunt, "budget", 7);
            SetPrivateField(hunt, "reviveInFlight", true);

            InvokePublic(hunt, "Close");

            var done = hunt.GetType().GetField("done",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsFalse((bool)done.GetValue(hunt),
                "Closing must not finalize while a reward is settling.");
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void SerializedLegacyThemeShimHasNoRuntimeThemeLifecycle()
    {
        var type = RuntimeType("DesignRuntimeWiring");
        Assert.IsNull(type.GetMethod("Awake",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
        Assert.IsNull(type.GetMethod("Start",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
        Assert.AreEqual(0, type.GetFields(
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
            BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Length,
            "The temporary serialized shim must remain behavior-free until scene YAML removes it.");
    }

    [Test]
    public void RetiredExactReferenceInstallerIsAbsent()
    {
        Assert.IsNull(System.Type.GetType("ExactReferenceVisuals, Assembly-CSharp"));
        Assert.IsNotNull(RuntimeType("SplashDesign"),
            "Splash must keep its dedicated current presentation owner.");
    }

    static GameObject Child(Transform parent, string name)
    {
        var child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child;
    }

    static int DirectChildCount(Transform parent, string name)
    {
        int count = 0;
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name)
                count++;
        return count;
    }

    static int DescendantCount(Transform parent, string name)
    {
        int count = 0;
        foreach (var child in parent.GetComponentsInChildren<Transform>(true))
            if (child.name == name)
                count++;
        return count;
    }

    static Transform FindDescendant(Transform parent, string name)
    {
        foreach (var child in parent.GetComponentsInChildren<Transform>(true))
            if (child.name == name)
                return child;
        Assert.IsTrue(false, "Missing descendant: " + name);
        return null;
    }

    static Component TmpText(Transform parent, string name)
    {
        var type = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        Assert.IsNotNull(type);
        var go = Child(parent, name);
        return go.AddComponent(type);
    }

    static string TextOf(Component text)
    {
        return (string)text.GetType().GetProperty("text").GetValue(text, null);
    }

    static void SetPublicField(Component component, string name, object value)
    {
        var field = component.GetType().GetField(name,
            BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(field, "Missing public field: " + name);
        field.SetValue(component, value);
    }

    static void SetPrivateField(Component component, string name, object value)
    {
        var field = component.GetType().GetField(name,
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, "Missing private field: " + name);
        field.SetValue(component, value);
    }

    static object GetPrivateField(Component component, string name)
    {
        var field = component.GetType().GetField(name,
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, "Missing private field: " + name);
        return field.GetValue(component);
    }

    static void InvokeIfPresent(Component component, string methodName)
    {
        if (component == null) return;
        var method = component.GetType().GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (method != null)
            method.Invoke(component, null);
    }

    static void SetLanguage(string name)
    {
        var l10n = RuntimeType("L10n");
        var language = l10n.GetNestedType("Language", BindingFlags.Public);
        Assert.IsNotNull(language, "Missing L10n.Language enum.");
        var setLanguage = l10n.GetMethod("SetLanguage",
            BindingFlags.Public | BindingFlags.Static);
        Assert.IsNotNull(setLanguage, "Missing L10n.SetLanguage.");
        setLanguage.Invoke(null, new[] { System.Enum.Parse(language, name) });
    }

    static void InvokePublic(Component component, string methodName,
        params object[] arguments)
    {
        InvokeNamed(component, methodName,
            BindingFlags.Instance | BindingFlags.Public, arguments);
    }

    static void InvokePrivate(Component component, string methodName,
        params object[] arguments)
    {
        InvokeNamed(component, methodName,
            BindingFlags.Instance | BindingFlags.NonPublic, arguments);
    }

    static object InvokePrivateResult(Component component, string methodName,
        params object[] arguments)
    {
        return InvokeNamed(component, methodName,
            BindingFlags.Instance | BindingFlags.NonPublic, arguments);
    }

    static object InvokeNamed(Component component, string methodName,
        BindingFlags flags, object[] arguments)
    {
        var types = ArgumentTypes(arguments);
        var method = component.GetType().GetMethod(
            methodName, flags, null, types, null);
        Assert.IsNotNull(method, "Missing method: " + methodName);
        return method.Invoke(component, arguments);
    }

    static System.Type[] ArgumentTypes(object[] arguments)
    {
        if (arguments == null || arguments.Length == 0)
            return System.Type.EmptyTypes;
        var types = new System.Type[arguments.Length];
        for (int i = 0; i < arguments.Length; i++)
        {
            Assert.IsNotNull(arguments[i],
                "Reflection invoke cannot infer a type from a null argument.");
            types[i] = arguments[i].GetType();
        }
        return types;
    }

    static System.Type RuntimeType(string name)
    {
        var type = System.Type.GetType(name + ", Assembly-CSharp");
        Assert.IsNotNull(type, "Missing runtime component: " + name);
        return type;
    }
}
