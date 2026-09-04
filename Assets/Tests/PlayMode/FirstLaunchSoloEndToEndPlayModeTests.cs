using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class FirstLaunchSoloEndToEndPlayModeTests
{
    const string PlayerName = "SmokePlayer";

#if UNITY_EDITOR
    static UnityEngine.Object settlementGameView;
    static UnityEngine.Object previouslyFocusedEditorWindow;
    static HashSet<int> preexistingGameViewInstanceIds;
#endif

    static readonly string[] StringPreferenceKeys =
    {
        "PlayerName",
        "DailyLastPlayDate",
    };

    static readonly string[] IntPreferenceKeys =
    {
        "HOL.Onboarding.Version",
        "HOL.Onboarding.Gender",
        "HOL.Onboarding.Avatar",
        "HOL.Onboarding.AgeCategory",
        "AIDifficulty",
        "Language",
        "AdsConsent",
        "StatWins",
        "StatLosses",
        "StatStreak",
        "StatBestStreak",
        "StatBestGuesses",
        "StatDraws",
        "StatMatches",
        "StatRecentBits",
        "StatRecentCount",
        "DailyChallengeDay",
        "DailyChallengeWins",
        "DailyChallengeCorrectGuesses",
        "DailyChallengeRoomsShared",
        "DailyChallengeRewardClaimed",
        "DailyChallengePoints",
        "DailyStreakDays",
        "LockIntroShown",
        "LockEverUsed",
        "PendingStreakRestore",
        "PendingRewardEarned",
    };

    readonly List<PreferenceSnapshot> preferences =
        new List<PreferenceSnapshot>();
    UnityEngine.Random.State randomState;
    double fakePresentationTime;

    [SetUp]
    public void SetUp()
    {
        randomState = UnityEngine.Random.state;
        preferences.Clear();
        foreach (string key in StringPreferenceKeys)
            preferences.Add(PreferenceSnapshot.CaptureString(key));
        foreach (string key in IntPreferenceKeys)
            preferences.Add(PreferenceSnapshot.CaptureInt(key));

        foreach (PreferenceSnapshot preference in preferences)
            PlayerPrefs.DeleteKey(preference.Key);

        // The smoke exercises a fresh profile, not the third-party consent
        // dialog. A stored decline prevents an ads SDK initialization and keeps
        // the requested Splash -> onboarding -> Home path deterministic.
        PlayerPrefs.SetInt("AdsConsent", 0);
        PlayerPrefs.SetInt("AIDifficulty", 2); // Hard: deterministic midpoint AI.
        PlayerPrefs.SetInt("Language", 0);     // Stable EN semantic assertions.
        PlayerPrefs.Save();
        UnityEngine.Random.InitState(20260830);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        CancelInvokesIfPresent("GameManager");
        CancelInvokesIfPresent("SplashLoader");

        // Destroy the returned Main Menu before restoring preferences. Its
        // delayed ExtrasRuntimeWiring pass can otherwise create a tracker one
        // frame after restoration and rewrite the caller's DailyChallenge keys.
        Scene active = SceneManager.GetActiveScene();
        Scene quiescent = SceneManager.CreateScene(
            "FirstLaunchSoloEndToEndQuiescent");
        SceneManager.SetActiveScene(quiescent);
        if (active.IsValid() && active.isLoaded)
            yield return SceneManager.UnloadSceneAsync(active);
        yield return null;
#if UNITY_EDITOR
        RestoreEditorWindowAfterSettlement();
#endif

        foreach (PreferenceSnapshot preference in preferences)
            preference.Restore();
        PlayerPrefs.Save();
        UnityEngine.Random.state = randomState;
        yield return null;
    }

    [UnityTest]
    public IEnumerator FreshLaunchCompletesOnboardingAndOneSoloMatchThenSkipsOnboarding()
    {
        yield return SceneManager.LoadSceneAsync(
            "SplashScene", LoadSceneMode.Single);
        yield return WaitUntilOrFail(
            () => Find(SceneManager.GetActiveScene(), "HOLOnboardingRoot") != null,
            5f,
            "Fresh launch did not build the onboarding root.");
        yield return null;
        yield return null;

        Scene splash = SceneManager.GetActiveScene();
        Assert.That(splash.name, Is.EqualTo("SplashScene"));
        Assert.That(CountInScene(splash, RuntimeType("SplashDesign")), Is.EqualTo(1));
        Assert.That(CountInScene(splash, RuntimeType("SplashOnboardingController")),
            Is.EqualTo(1));
        Assert.That(Find(splash, "SplashVisualRoot"), Is.Null,
            "Fresh onboarding and returning-player Splash must not compete.");

        Transform onboarding = Find(splash, "HOLOnboardingRoot");
        Component loader = FindInScene(splash, RuntimeType("SplashLoader"));
        Assert.That(loader, Is.Not.Null);
        Assert.That(((MonoBehaviour)loader).IsInvoking(), Is.False,
            "Fresh onboarding must own navigation until the profile is committed.");

        Click(onboarding, "WelcomeContinue");
        yield return null;

        TMP_InputField nameInput = Find(onboarding, "OnboardingNameInput")
            .GetComponent<TMP_InputField>();
        nameInput.text = PlayerName;
        yield return null;
        Click(onboarding, "NameContinue");
        yield return null;

        Click(onboarding, "GenderCard1");
        Click(onboarding, "GenderContinue");
        yield return null;

        Click(onboarding, "AvatarCard1");
        Click(onboarding, "AvatarContinue");
        yield return null;

        Click(onboarding, "AgeCard2");
        Click(onboarding, "AgeContinue");
#if UNITY_EDITOR
        FocusGameViewForEndOfFrameSettlement();
        yield return null;
#endif

        yield return WaitForScene("MainMenu", 5f);

        Assert.That(PlayerPrefs.GetString("PlayerName"), Is.EqualTo(PlayerName));
        Assert.That(PlayerPrefs.GetInt("HOL.Onboarding.Version"), Is.EqualTo(1));
        Assert.That(PlayerPrefs.GetInt("HOL.Onboarding.Gender"), Is.EqualTo(1));
        Assert.That(PlayerPrefs.GetInt("HOL.Onboarding.Avatar"), Is.EqualTo(0));
        Assert.That(PlayerPrefs.GetInt("HOL.Onboarding.AgeCategory"), Is.EqualTo(2));
        Assert.That(GetStaticProperty<bool>("OnboardingProfile", "IsComplete"),
            Is.True);
        Assert.That(GetStaticProperty<bool>("OnboardingProfile", "ShouldRun"),
            Is.False);

        Component homeOwner = null;
        for (int frame = 0; frame < 160; frame++)
        {
            homeOwner = FindInScene(
                SceneManager.GetActiveScene(), RuntimeType("MainMenuHomeVisuals"));
            if (homeOwner != null &&
                GetProperty<bool>(homeOwner, "IsReady") &&
                GetProperty<bool>(homeOwner, "IsSettled"))
                break;
#if UNITY_EDITOR
            FocusGameViewForEndOfFrameSettlement();
#endif
            yield return null;
        }

        Assert.That(homeOwner, Is.Not.Null,
            "Main Menu Home owner did not appear within 160 Unity frames.");
        Assert.That(GetProperty<bool>(homeOwner, "IsReady"), Is.True,
            "Main Menu Home owner appeared but its production dependencies were not ready.");
        Assert.That(GetProperty<bool>(homeOwner, "IsSettled"), Is.True,
            "Main Menu Home was ready but its production presentation did not settle within 160 Unity frames.");
        yield return null;
        Canvas.ForceUpdateCanvases();

        Assert.That(CountInScene(
            SceneManager.GetActiveScene(), RuntimeType("MainMenuHomeVisuals")),
            Is.EqualTo(1), "Home must have one presentation owner.");
        Canvas homeCanvas = homeOwner.GetComponent<Canvas>();
        Assert.That(homeCanvas, Is.Not.Null);
        Assert.That(Find(homeCanvas.transform, "HomePlayerChipText")
            .GetComponent<TMP_Text>().text, Is.EqualTo(PlayerName));

        Button soloEntry = Find(homeCanvas.transform, "ButtonPlay")
            .GetComponent<Button>();
        Assert.That(PersistentMethods(soloEntry), Does.Contain("OnPlayPressed"));
        Assert.That(soloEntry.interactable, Is.True);
        Click(soloEntry);

        Component matchmaking = FindInScene(
            SceneManager.GetActiveScene(), RuntimeType("FakeMatchmaking"));
        Component menu = FindInScene(
            SceneManager.GetActiveScene(), RuntimeType("MenuManager"));
        Assert.That(matchmaking, Is.Not.Null);
        Assert.That(menu, Is.Not.Null);

        GameObject gamePanel = GetField<GameObject>(matchmaking, "panelGame");
        GameObject retiredPlay = GetField<GameObject>(menu, "panelPlay");
        GameObject retiredSearch = GetField<GameObject>(menu, "panelSearching");
        Assert.That(gamePanel, Is.Not.Null);

        Component soloOwner = null;
        yield return WaitUntilOrFail(() =>
        {
            soloOwner = FindInScene(
                SceneManager.GetActiveScene(), RuntimeType("SoloDuelVisuals"));
            return gamePanel.activeInHierarchy && soloOwner != null &&
                   GetProperty<GameObject>(soloOwner, "KeypadRoot") != null &&
                   GetProperty<Button>(soloOwner, "SubmitControl") != null &&
                   !GetProperty<bool>(matchmaking, "IsPreparing");
        }, 8f, "Direct Solo entry did not expose a ready real board.");

        Assert.That(retiredPlay == null || !retiredPlay.activeInHierarchy, Is.True,
            "Direct Solo entry exposed the retired intermediate screen.");
        Assert.That(retiredSearch == null || !retiredSearch.activeInHierarchy, Is.True,
            "Direct Solo entry exposed the retired search screen.");
        Assert.That(CountInScene(
            SceneManager.GetActiveScene(), RuntimeType("SoloDuelVisuals")),
            Is.EqualTo(1), "Solo must have one presentation owner.");

        Component numberManager = FindInScene(
            SceneManager.GetActiveScene(), RuntimeType("NumberManager"));
        Component game = FindInScene(
            SceneManager.GetActiveScene(), RuntimeType("GameManager"));
        Assert.That(numberManager, Is.Not.Null);
        Assert.That(game, Is.Not.Null);

        fakePresentationTime = 3000d;
        Invoke(game, "SetPresentationClockForTests",
            new Func<double>(() => fakePresentationTime));

        TMP_InputField numberInput = GetField<TMP_InputField>(
            numberManager, "numberInput");
        AssertPhase(soloOwner, "ChooseSecret");
        Assert.That(numberInput.text, Is.Empty);

        PressKey(gamePanel.transform, "1");
        PressKey(gamePanel.transform, "0");
        PressKey(gamePanel.transform, "0");
        Assert.That(numberInput.text, Is.EqualTo("100"));
        Click(GetProperty<Button>(soloOwner, "SubmitControl"));
        Assert.That(GetField<int>(numberManager, "playerNumber"), Is.EqualTo(100));

        // The real submit above deliberately uses the product's random opener.
        // Reset that just-started match through the production rematch path,
        // then choose a deterministic opener while every move still runs
        // through GameManager, NumberManager and canonical DuelRules.
        Invoke(game, "RestartMatch");
        Invoke(game, "SetPlayerNumber", 100);
        SetField(numberManager, "playerNumber", 100);
        SetField(numberManager, "gameStarted", true);
        SetField(game, "adsManager", null);
        StartWithOpener(game, "Host");
        SetField(game, "aiSecretNumber", 77);
        AssertPhase(soloOwner, "StarterReveal");
        Assert.That(Convert.ToInt32(StateProperty(soloOwner, "RoundNumber")),
            Is.EqualTo(1));
        Transform permissionAction = Find(
            gamePanel.transform, "SoloContinueButton");
        Assert.That(permissionAction, Is.Not.Null);
        Assert.That(permissionAction.gameObject.activeInHierarchy, Is.False,
            "Automatic Solo pacing must not require a permission tap.");

        yield return AdvanceScheduledBeat(game);
        AssertPhase(soloOwner, "PlayerGuess");

        PressKey(gamePanel.transform, "5");
        PressKey(gamePanel.transform, "0");
        Click(GetProperty<Button>(soloOwner, "SubmitControl"));

        AssertPhase(soloOwner, "PlayerOutcome");
        CollectionAssert.AreEqual(new[] { 50 }, History(soloOwner, "PlayerGuessHistory"));
        Assert.That(StateProperty(soloOwner, "Prompt").ToString(),
            Is.EqualTo("PlayerGuessedHigher"));

        yield return AdvanceScheduledBeat(game);
        AssertPhase(soloOwner, "OpponentThinking");
        CollectionAssert.IsEmpty(History(soloOwner, "AiGuessHistory"));

        yield return AdvanceScheduledBeat(game);
        AssertPhase(soloOwner, "OpponentGuess");
        Assert.That(GetField<int>(game, "aiGuess"), Is.EqualTo(50));
        CollectionAssert.AreEqual(new[] { 50 }, History(soloOwner, "AiGuessHistory"));

        yield return AdvanceScheduledBeat(game);
        AssertPhase(soloOwner, "AnswerOpponent");
        Assert.That(StateProperty(soloOwner, "Prompt").ToString(),
            Is.EqualTo("OpponentGuessedHigher"));

        TMP_Text phasePrompt = GetField<TMP_Text>(game, "turnText");
        Assert.That(phasePrompt.gameObject.activeInHierarchy, Is.True);
        TMP_Text centralOutcome = Find(gamePanel.transform, "CentralOutcome")
            .GetComponent<TMP_Text>();
        Assert.That(centralOutcome.text,
            Does.Contain(Localized("solo_history_higher")));
        centralOutcome.ForceMeshUpdate();
        Assert.That(centralOutcome.isTextOverflowing, Is.False,
            "The acknowledged AI outcome must remain fully readable.");

        Assert.That(AnswerActions(game).Any(action => action.activeSelf), Is.False,
            "Truthful Solo feedback must not expose manual answer controls.");

        yield return AdvanceScheduledBeat(game);
        Canvas.ForceUpdateCanvases();

        AssertPhase(soloOwner, "PlayerGuess");
        Assert.That(Convert.ToInt32(StateProperty(soloOwner, "RoundNumber")),
            Is.EqualTo(2));
        Assert.That(numberInput.interactable, Is.True);

        PressKey(gamePanel.transform, "7");
        PressKey(gamePanel.transform, "7");
        Click(GetProperty<Button>(soloOwner, "SubmitControl"));

        AssertPhase(soloOwner, "PlayerOutcome");
        CollectionAssert.AreEqual(new[] { 50, 77 },
            History(soloOwner, "PlayerGuessHistory"));

        yield return AdvanceScheduledBeat(game);
        AssertPhase(soloOwner, "OpponentThinking");

        yield return AdvanceScheduledBeat(game);
        AssertPhase(soloOwner, "OpponentGuess");

        yield return AdvanceScheduledBeat(game);
        AssertPhase(soloOwner, "AnswerOpponent");

        yield return AdvanceScheduledBeat(game);

        AssertPhase(soloOwner, "MatchResult");
        Assert.That(StateProperty(soloOwner, "Prompt").ToString(), Is.EqualTo("Win"));
        Assert.That(GetProperty<bool>(game, "IsMatchOver"), Is.True);
        CollectionAssert.AreEqual(new[] { 50, 75 },
            History(soloOwner, "AiGuessHistory"));
        Assert.That(phasePrompt.gameObject.activeInHierarchy, Is.True,
            "The authoritative terminal headline must remain visible.");
        Assert.That(phasePrompt.text, Does.StartWith(Localized("you_win")));
        TMP_Text resultReason = Find(gamePanel.transform, "ResultReason")
            .GetComponent<TMP_Text>();
        Assert.That(resultReason.gameObject.activeInHierarchy, Is.True);
        Assert.That(resultReason.text, Is.Not.Empty,
            "The terminal result must include its authoritative reason.");
        Assert.That(numberInput.gameObject.activeSelf, Is.False);
        Assert.That(GetProperty<Button>(soloOwner, "SubmitControl").gameObject.activeSelf,
            Is.False);
        Assert.That(GetField<GameObject>(game, "stopGameButton").activeSelf, Is.True);

        Assert.That(PlayerPrefs.GetInt("StatWins"), Is.EqualTo(1));
        Assert.That(PlayerPrefs.GetInt("StatMatches"), Is.EqualTo(1));
        Assert.That(PlayerPrefs.GetInt("DailyChallengeCorrectGuesses"), Is.EqualTo(1));
        Assert.That(PlayerPrefs.GetInt("DailyChallengeWins"), Is.EqualTo(1));

        Assert.That(PlayerPrefs.GetString("PlayerName"), Is.EqualTo(PlayerName));
        Assert.That(PlayerPrefs.GetInt("HOL.Onboarding.Version"), Is.EqualTo(1));

        int returningMainMenuLoads = 0;
        UnityEngine.Events.UnityAction<Scene, LoadSceneMode> onSceneLoaded =
            (scene, mode) =>
            {
                if (scene.name == "MainMenu") returningMainMenuLoads++;
            };
        SceneManager.sceneLoaded += onSceneLoaded;
        try
        {
            yield return SceneManager.LoadSceneAsync(
                "SplashScene", LoadSceneMode.Single);
            yield return WaitUntilOrFail(
                () => Find(SceneManager.GetActiveScene(), "SplashVisualRoot") != null,
                5f,
                "Returning-player Splash did not build its production presentation.");

            Scene returningSplash = SceneManager.GetActiveScene();
            Assert.That(returningSplash.name, Is.EqualTo("SplashScene"));
            Assert.That(Find(returningSplash, "HOLOnboardingRoot"), Is.Null,
                "A committed profile must not be sent through onboarding again.");
            Assert.That(Find(returningSplash, "SplashVisualRoot"), Is.Not.Null);

            Component returningLoader = FindInScene(
                returningSplash, RuntimeType("SplashLoader"));
            Assert.That(returningLoader, Is.Not.Null);
            Assert.That(((MonoBehaviour)returningLoader).IsInvoking(), Is.True,
                "Returning-player Splash must schedule its normal menu transition.");

            yield return WaitForScene("MainMenu", 5f);
            Assert.That(returningMainMenuLoads, Is.EqualTo(1));
            Assert.That(PlayerPrefs.GetString("PlayerName"), Is.EqualTo(PlayerName));
            Assert.That(PlayerPrefs.GetInt("HOL.Onboarding.Version"), Is.EqualTo(1));
            Assert.That(GetStaticProperty<bool>("OnboardingProfile", "ShouldRun"),
                Is.False);
        }
        finally
        {
            SceneManager.sceneLoaded -= onSceneLoaded;
        }
    }

    static void StartWithOpener(Component game, string opener)
    {
        MethodInfo method = game.GetType().GetMethod(
            "StartGameWithOpener",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        ParameterInfo[] parameters = method.GetParameters();
        Assert.That(parameters, Has.Length.EqualTo(1));
        Assert.That(parameters[0].ParameterType.IsEnum, Is.True);
        method.Invoke(game, new[]
        {
            Enum.Parse(parameters[0].ParameterType, opener),
        });
        ((MonoBehaviour)game).CancelInvoke("AIGuess");
    }

    static void AssertPhase(Component soloOwner, string expected)
    {
        Assert.That(StateProperty(soloOwner, "Phase").ToString(), Is.EqualTo(expected));
    }

    static object StateProperty(Component soloOwner, string name)
    {
        object state = GetProperty<object>(soloOwner, "CurrentState");
        PropertyInfo property = state.GetType().GetProperty(
            name, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(property, Is.Not.Null, "Missing presentation-state property " + name);
        return property.GetValue(state);
    }

    static int[] History(Component soloOwner, string name)
    {
        var values = StateProperty(soloOwner, name) as IEnumerable;
        Assert.That(values, Is.Not.Null, name);
        return values.Cast<object>().Select(Convert.ToInt32).ToArray();
    }

    static GameObject[] AnswerActions(Component game)
    {
        return new[]
        {
            GetField<GameObject>(game, "higherButton"),
            GetField<GameObject>(game, "lowerButton"),
            GetField<GameObject>(game, "correctButton"),
        };
    }

    static void PressKey(Transform root, string digit)
    {
        Click(root, "Key_" + digit);
    }

    static void Click(Transform root, string name)
    {
        Transform target = Find(root, name);
        Assert.That(target, Is.Not.Null, name + " is missing.");
        Button button = target.GetComponent<Button>();
        Assert.That(button, Is.Not.Null, name + " is not a real Button.");
        Click(button);
    }

    static void Click(Button button)
    {
        Assert.That(button, Is.Not.Null, "The requested Button is missing.");
        Assert.That(button.interactable, Is.True,
            button.name + " is disabled.");
        Assert.That(button.gameObject.activeInHierarchy, Is.True,
            button.name + " is not visible in the live flow.");

        EventSystem eventSystem = EventSystem.current;
        Assert.That(eventSystem, Is.Not.Null,
            button.name + " requires the live EventSystem.");
        RectTransform rect = button.transform as RectTransform;
        Canvas canvas = button.GetComponentInParent<Canvas>();
        Assert.That(rect, Is.Not.Null);
        Assert.That(canvas, Is.Not.Null);
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            eventCamera, rect.TransformPoint(rect.rect.center));
        var pointer = new PointerEventData(eventSystem)
        {
            button = PointerEventData.InputButton.Left,
            position = screenPoint,
        };
        var hits = new List<RaycastResult>();
        eventSystem.RaycastAll(pointer, hits);
        Assert.That(hits, Is.Not.Empty,
            button.name + " is not reachable through the live UI raycasters.");
        RaycastResult hit = hits[0];
        Assert.That(
            hit.gameObject.GetComponentInParent<Button>(), Is.SameAs(button),
            button.name + " is covered by " + hit.gameObject.name + ".");
        pointer.pointerCurrentRaycast = hit;
        pointer.pointerPressRaycast = hit;
        ExecuteEvents.Execute(
            button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(
            button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(
            button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }

    static IEnumerator WaitForScene(string expected, float timeoutSeconds)
    {
        yield return WaitUntilOrFail(
            () => SceneManager.GetActiveScene().name == expected,
            timeoutSeconds,
            "Timed out waiting for scene " + expected + ".");
    }

    static IEnumerator WaitUntilOrFail(
        Func<bool> predicate,
        float timeoutSeconds,
        string failure)
    {
        float deadline = Time.realtimeSinceStartup + timeoutSeconds;
        while (!predicate() && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(predicate(), Is.True, failure);
    }

    IEnumerator AdvanceScheduledBeat(Component game)
    {
        float remaining = GetProperty<float>(
            game, "CurrentAutomaticRemainingSeconds");
        Assert.That(remaining, Is.GreaterThan(0f),
            "A readable automatic phase must expose one pending deadline.");
        fakePresentationTime += remaining + 0.01d;
        yield return null;
        yield return null;
    }

#if UNITY_EDITOR
    internal static void FocusGameViewForEndOfFrameSettlement()
    {
        if (Application.isBatchMode)
            return;

        Type editorWindowType = Type.GetType("UnityEditor.EditorWindow, UnityEditor");
        Type gameViewType = Type.GetType("UnityEditor.GameView, UnityEditor");
        Assert.That(editorWindowType, Is.Not.Null,
            "Unity EditorWindow type is required to exercise the real end-of-frame settlement path.");
        Assert.That(gameViewType, Is.Not.Null,
            "Unity Game View type is required to exercise the real end-of-frame settlement path.");

        PropertyInfo focusedWindow = editorWindowType.GetProperty(
            "focusedWindow", BindingFlags.Public | BindingFlags.Static);
        Assert.That(focusedWindow, Is.Not.Null,
            "Unity EditorWindow.focusedWindow is required to restore the test runner after settlement.");

        if (preexistingGameViewInstanceIds == null)
        {
            previouslyFocusedEditorWindow =
                focusedWindow.GetValue(null, null) as UnityEngine.Object;
            preexistingGameViewInstanceIds = new HashSet<int>(
                Resources.FindObjectsOfTypeAll(gameViewType)
                    .Select(view => view.GetInstanceID()));
        }

        if (settlementGameView == null)
        {
            Type editorApplicationType = Type.GetType(
                "UnityEditor.EditorApplication, UnityEditor");
            MethodInfo executeMenuItem = editorApplicationType == null
                ? null
                : editorApplicationType.GetMethod(
                    "ExecuteMenuItem",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string) },
                    null);
            Assert.That(executeMenuItem, Is.Not.Null,
                "Unity EditorApplication.ExecuteMenuItem is required to open the real Game View.");
            Assert.That((bool)executeMenuItem.Invoke(
                    null, new object[] { "Window/General/Game" }),
                Is.True, "Unity did not open its real Game View.");

            MethodInfo getWindow = editorWindowType.GetMethod(
                "GetWindow",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Type) },
                null);
            Assert.That(getWindow, Is.Not.Null,
                "Unity EditorWindow.GetWindow(Type) is required to acquire the real Game View.");
            settlementGameView = getWindow.Invoke(
                null, new object[] { gameViewType }) as UnityEngine.Object;
            Assert.That(settlementGameView, Is.Not.Null,
                "Unity did not provide its real Game View for the presentation settlement path.");
        }

        MethodInfo showTab = editorWindowType.GetMethod(
            "ShowTab",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null);
        MethodInfo focus = editorWindowType.GetMethod(
            "Focus",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null);
        MethodInfo repaint = editorWindowType.GetMethod(
            "Repaint",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null);
        Assert.That(showTab, Is.Not.Null);
        Assert.That(focus, Is.Not.Null);
        Assert.That(repaint, Is.Not.Null);
        showTab.Invoke(settlementGameView, null);
        focus.Invoke(settlementGameView, null);
        repaint.Invoke(settlementGameView, null);
    }

    internal static void RestoreEditorWindowAfterSettlement()
    {
        if (preexistingGameViewInstanceIds == null)
            return;

        Type editorWindowType = Type.GetType("UnityEditor.EditorWindow, UnityEditor");
        if (editorWindowType != null && settlementGameView != null &&
            !preexistingGameViewInstanceIds.Contains(
                settlementGameView.GetInstanceID()))
        {
            MethodInfo close = editorWindowType.GetMethod(
                "Close",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);
            if (close != null)
                close.Invoke(settlementGameView, null);
        }

        if (editorWindowType != null && previouslyFocusedEditorWindow != null)
        {
            foreach (string methodName in new[] { "ShowTab", "Focus", "Repaint" })
            {
                MethodInfo method = editorWindowType.GetMethod(
                    methodName,
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    Type.EmptyTypes,
                    null);
                if (method != null)
                    method.Invoke(previouslyFocusedEditorWindow, null);
            }
        }

        settlementGameView = null;
        previouslyFocusedEditorWindow = null;
        preexistingGameViewInstanceIds = null;
    }
#endif

    static string[] PersistentMethods(Button button)
    {
        int count = button.onClick.GetPersistentEventCount();
        var methods = new string[count];
        for (int index = 0; index < count; index++)
            methods[index] = button.onClick.GetPersistentMethodName(index);
        return methods;
    }

    static string Localized(string key, params object[] arguments)
    {
        MethodInfo get = RuntimeType("L10n").GetMethod(
            "Get", BindingFlags.Public | BindingFlags.Static);
        Assert.That(get, Is.Not.Null);
        return (string)get.Invoke(null, new object[] { key, arguments });
    }

    static object Invoke(Component target, string method, params object[] arguments)
    {
        MethodInfo info = target.GetType().GetMethod(
            method,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(info, Is.Not.Null, "Missing method " + method);
        return info.Invoke(target, arguments);
    }

    static T GetField<T>(Component target, string name)
    {
        FieldInfo field = target.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Missing field " + name);
        return (T)field.GetValue(target);
    }

    static void SetField(Component target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Missing field " + name);
        field.SetValue(target, value);
    }

    static T GetProperty<T>(object target, string name)
    {
        PropertyInfo property = target.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, "Missing property " + name);
        return (T)property.GetValue(target);
    }

    static T GetStaticProperty<T>(string typeName, string name)
    {
        PropertyInfo property = RuntimeType(typeName).GetProperty(
            name,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, "Missing static property " + name);
        return (T)property.GetValue(null);
    }

    static void CancelInvokesIfPresent(string typeName)
    {
        Type type = Type.GetType(typeName + ", Assembly-CSharp");
        if (type == null || !SceneManager.GetActiveScene().IsValid()) return;
        Component component = FindInScene(SceneManager.GetActiveScene(), type);
        if (component is MonoBehaviour behaviour)
            behaviour.CancelInvoke();
    }

    static int CountInScene(Scene scene, Type type)
    {
        int count = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
            count += root.GetComponentsInChildren(type, true).Length;
        return count;
    }

    static Component FindInScene(Scene scene, Type type)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Component found = root.GetComponentsInChildren(type, true)
                .FirstOrDefault();
            if (found != null) return found;
        }
        return null;
    }

    static Transform Find(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = Find(root.transform, name);
            if (found != null) return found;
        }
        return null;
    }

    static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int index = 0; index < root.childCount; index++)
        {
            Transform found = Find(root.GetChild(index), name);
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

    sealed class PreferenceSnapshot
    {
        enum ValueKind
        {
            Int,
            String,
        }

        readonly ValueKind kind;
        readonly bool existed;
        readonly int intValue;
        readonly string stringValue;

        PreferenceSnapshot(
            string key,
            ValueKind kind,
            bool existed,
            int intValue,
            string stringValue)
        {
            Key = key;
            this.kind = kind;
            this.existed = existed;
            this.intValue = intValue;
            this.stringValue = stringValue;
        }

        public string Key { get; }

        public static PreferenceSnapshot CaptureInt(string key)
        {
            return new PreferenceSnapshot(
                key,
                ValueKind.Int,
                PlayerPrefs.HasKey(key),
                PlayerPrefs.GetInt(key, 0),
                null);
        }

        public static PreferenceSnapshot CaptureString(string key)
        {
            return new PreferenceSnapshot(
                key,
                ValueKind.String,
                PlayerPrefs.HasKey(key),
                0,
                PlayerPrefs.GetString(key, string.Empty));
        }

        public void Restore()
        {
            if (!existed)
            {
                PlayerPrefs.DeleteKey(Key);
                return;
            }

            if (kind == ValueKind.String)
                PlayerPrefs.SetString(Key, stringValue);
            else
                PlayerPrefs.SetInt(Key, intValue);
        }
    }
}
