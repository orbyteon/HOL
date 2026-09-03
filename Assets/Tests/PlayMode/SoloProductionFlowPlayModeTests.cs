using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class SoloProductionFlowPlayModeTests
{
    const string PlayerName = "SoloTester";
    const int SelectedAvatar = 6;

    static readonly string[] StringPreferenceKeys =
    {
        "PlayerName",
        "DailyLastPlayDate",
    };

    static readonly string[] IntegerPreferenceKeys =
    {
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
        "LockIntroShown",
        "LockEverUsed",
        "PendingStreakRestore",
        "PendingRewardEarned",
        "DailyChallengeDay",
        "DailyChallengeWins",
        "DailyChallengeCorrectGuesses",
        "DailyChallengeRoomsShared",
        "DailyChallengeRewardClaimed",
        "DailyChallengePoints",
        "DailyStreakDays",
        "HOL.Onboarding.Version",
        "HOL.Onboarding.Gender",
        "HOL.Onboarding.Avatar",
        "HOL.Onboarding.AgeCategory",
    };

    readonly List<MatchOutcome> completedOutcomes =
        new List<MatchOutcome>();

    List<PreferenceSnapshot> preferenceSnapshots;
    Component game;
    Component numberManager;
    Component owner;
    Component menu;
    GameObject panel;
    TMP_InputField input;

    Action<MatchOutcome> completedHandler;
    Action<bool, int> legacyHandler;
    Action statsChangedHandler;
    UnityEngine.Random.State randomState;
    int legacyEventCount;
    bool legacyWon;
    int legacyGuesses;
    int statsChangedCount;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        randomState = UnityEngine.Random.state;
        preferenceSnapshots = IntegerPreferenceKeys
            .Select(PreferenceSnapshot.CaptureInt)
            .ToList();
        preferenceSnapshots.AddRange(StringPreferenceKeys
            .Select(PreferenceSnapshot.CaptureString));

        EstablishControlledPreferences();
        UnityEngine.Random.InitState(20260901);

        yield return SceneManager.LoadSceneAsync(
            "MainMenu", LoadSceneMode.Single);
        for (int frame = 0; frame < 8; frame++)
            yield return null;

        game = FindInScene(RuntimeType("GameManager"));
        numberManager = FindInScene(RuntimeType("NumberManager"));
        menu = FindInScene(RuntimeType("MenuManager"));
        Assert.That(game, Is.Not.Null);
        Assert.That(numberManager, Is.Not.Null);
        Assert.That(menu, Is.Not.Null);

        panel = numberManager.gameObject;
        panel.SetActive(true);
        for (int frame = 0; frame < 4; frame++)
            yield return null;

        owner = FindInScene(RuntimeType("SoloDuelVisuals"));
        Assert.That(owner, Is.Not.Null);
        input = (TMP_InputField)Field(numberManager, "numberInput");
        Assert.That(input, Is.Not.Null);

        var behaviour = (MonoBehaviour)game;
        behaviour.CancelInvoke();
        SetField(game, "adsManager", null);
        SetField(game, "audioSource", null);
        SetField(game, "winConfetti", null);

        completedOutcomes.Clear();
        legacyEventCount = 0;
        legacyWon = false;
        legacyGuesses = -1;
        statsChangedCount = 0;

        completedHandler = outcome => completedOutcomes.Add(outcome);
        legacyHandler = (won, guesses) =>
        {
            legacyEventCount++;
            legacyWon = won;
            legacyGuesses = guesses;
        };
        statsChangedHandler = () => statsChangedCount++;

        GameEvents.OnMatchCompleted += completedHandler;
        GameEvents.OnMatchEnded += legacyHandler;
        GameEvents.OnStatsChanged += statsChangedHandler;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (completedHandler != null)
            GameEvents.OnMatchCompleted -= completedHandler;
        if (legacyHandler != null)
            GameEvents.OnMatchEnded -= legacyHandler;
        if (statsChangedHandler != null)
            GameEvents.OnStatsChanged -= statsChangedHandler;

        CancelInvokesIfPresent("GameManager");
        CancelInvokesIfPresent("MenuManager");
        CancelInvokesIfPresent("FakeMatchmaking");

        // Quiesce every MainMenu behaviour before restoring caller-owned
        // preferences. ExtrasRuntimeWiring installs trackers one frame late;
        // leaving that scene alive could rewrite the restored daily keys.
        Scene active = SceneManager.GetActiveScene();
        Scene quiescent = SceneManager.CreateScene(
            "SoloProductionFlowQuiescent");
        SceneManager.SetActiveScene(quiescent);
        if (active.IsValid() && active.isLoaded)
            yield return SceneManager.UnloadSceneAsync(active);
        yield return null;

        if (preferenceSnapshots != null)
        {
            foreach (PreferenceSnapshot snapshot in preferenceSnapshots)
                snapshot.Restore();
            PlayerPrefs.Save();
        }

        UnityEngine.Random.state = randomState;

        yield return null;
    }

    [UnityTest]
    public IEnumerator LossPublishesOutcomeAndRematchCannotDuplicateTerminalWork()
    {
        PlayerPrefs.SetInt("StatStreak", 3);
        PlayerPrefs.SetInt("StatBestStreak", 5);
        PlayerPrefs.Save();

        // Hard opens at 50. When 50 is also the player's secret, the AI has
        // a provisional win and the player receives last licks.
        StartWithOpener("Guest", 50, 77);
        AssertPhase("OpponentThinking");
        yield return null;
        Acknowledge();
        AssertPhase("OpponentGuess");
        Assert.That(Field(game, "aiGuess"), Is.EqualTo(50));
        Assert.That(StateProperty("ActiveActor").ToString(), Is.EqualTo("Opponent"));
        Assert.That(StateProperty("TargetActor").ToString(), Is.EqualTo("Player"));
        Assert.That(StateProperty("NextAction").ToString(), Is.EqualTo("RevealOutcome"));

        yield return null;
        Acknowledge();
        AssertPhase("AnswerOpponent");
        Assert.That(StateProperty("Prompt").ToString(),
            Is.EqualTo("OpponentGuessedCorrect"));
        yield return null;
        Acknowledge();
        AssertPhase("LastLicks");
        Assert.That(StateProperty("IsLastLicks"), Is.EqualTo(true));
        Assert.That(StateProperty("ActiveActor").ToString(), Is.EqualTo("Player"));
        Assert.That(StateProperty("TargetActor").ToString(), Is.EqualTo("Opponent"));
        Assert.That(StateProperty("NextAction").ToString(), Is.EqualTo("Continue"));

        yield return null;
        Acknowledge();
        AssertPhase("PlayerGuess");
        Assert.That(StateProperty("Prompt").ToString(),
            Is.EqualTo("MatchPoint"));
        Assert.That(StateProperty("IsLastLicks"), Is.EqualTo(true));

        Assert.That(Invoke(game, "PlayerGuess", 50), Is.EqualTo(true));
        AssertPhase("PlayerOutcome");
        yield return null;
        Acknowledge();
        AssertTerminal("Loss");

        AssertSingleOutcome(
            MatchOutcome.Result.Loss,
            expectedGuesses: 1,
            expectedOpponentGuesses: 1,
            expectedOpened: false,
            expectedLockStaked: false);
        Assert.That(legacyEventCount, Is.EqualTo(1));
        Assert.That(legacyWon, Is.False);
        Assert.That(legacyGuesses, Is.Zero);
        Assert.That(statsChangedCount, Is.EqualTo(1));

        AssertStat("StatWins", 0);
        AssertStat("StatLosses", 1);
        AssertStat("StatDraws", 0);
        AssertStat("StatMatches", 1);
        AssertStat("StatStreak", 0);
        AssertStat("StatBestStreak", 5);
        AssertStat("StatRecentBits", 0);
        AssertStat("StatRecentCount", 1);
        CollectionAssert.AreEqual(
            new[] { 50 }, History("PlayerGuessHistory"));
        CollectionAssert.AreEqual(
            new[] { 50 }, History("AiGuessHistory"));
        CollectionAssert.AreEqual(
            new[] { "Higher" }, HistoryNames("PlayerGuessHints"));
        CollectionAssert.AreEqual(
            new[] { "Correct" }, HistoryNames("AiGuessHints"));
        object[] chronological = HistoryEvents();
        Assert.That(chronological, Has.Length.EqualTo(2));
        AssertHistoryEvent(chronological[0], 1, 1, "Opponent", "Player", 50, "Correct");
        AssertHistoryEvent(chronological[1], 2, 1, "Player", "Opponent", 50, "Higher");
        Assert.That(StateProperty("OpponentSecretNumber"), Is.EqualTo(77));

        var behaviour = (MonoBehaviour)game;
        Assert.That(behaviour.IsInvoking("AIGuess"), Is.False);
        Assert.That(behaviour.IsInvoking("ResolveAiAnswerAutomatically"), Is.False,
            "No staged Solo fact may be driven by a timer.");
        var rematchObject = (GameObject)Field(game, "stopGameButton");
        Button rematch = rematchObject.GetComponent<Button>();
        Assert.That(rematch, Is.Not.Null);
        Assert.That(rematchObject.activeInHierarchy, Is.True);
        Assert.That(rematch.interactable, Is.True);

        int sceneHandle = SceneManager.GetActiveScene().handle;
        int mainMenuLoads = 0;
        UnityAction<Scene, LoadSceneMode> loaded = (scene, mode) =>
        {
            if (scene.name == "MainMenu")
                mainMenuLoads++;
        };
        SceneManager.sceneLoaded += loaded;
        try
        {
            rematch.onClick.Invoke();
            for (int frame = 0; frame < 3; frame++)
                yield return null;

            Assert.That(mainMenuLoads, Is.Zero,
                "REMATCH must not invoke the retired scene-exit callback.");
            Assert.That(SceneManager.GetActiveScene().handle,
                Is.EqualTo(sceneHandle));
            Assert.That(behaviour.IsInvoking("ResolveAiAnswerAutomatically"), Is.False);
            AssertPhase("ChooseSecret");
            Assert.That(GetProperty<bool>(game, "IsMatchOver"), Is.False);
            Assert.That(History("PlayerGuessHistory"), Is.Empty);
            Assert.That(History("AiGuessHistory"), Is.Empty);
            Assert.That(input.gameObject.activeSelf, Is.True);
            Assert.That(Submit().gameObject.activeSelf, Is.True);

            // Even if a stale callback is invoked explicitly, and even if
            // RestartMatch is requested again, terminal work remains exactly
            // once and persistent stats are not applied twice.
            Invoke(game, "ResolveAiAnswerAutomatically");
            Invoke(game, "AIGuess");
            Invoke(game, "RestartMatch");
            yield return null;

            Assert.That(completedOutcomes, Has.Count.EqualTo(1));
            Assert.That(legacyEventCount, Is.EqualTo(1));
            Assert.That(statsChangedCount, Is.EqualTo(1));
            AssertStat("StatLosses", 1);
            AssertStat("StatMatches", 1);
            AssertPhase("ChooseSecret");
            Assert.That(GetProperty<bool>(game, "IsMatchOver"), Is.False);
            Assert.That(History("PlayerGuessHistory"), Is.Empty);
            Assert.That(History("AiGuessHistory"), Is.Empty);
        }
        finally
        {
            SceneManager.sceneLoaded -= loaded;
        }
    }

    [UnityTest]
    public IEnumerator EqualCorrectRoundIsDrawAndPreservesStreakAndRecentForm()
    {
        PlayerPrefs.SetInt("StatStreak", 4);
        PlayerPrefs.SetInt("StatBestStreak", 6);
        PlayerPrefs.SetInt("StatRecentBits", 5);
        PlayerPrefs.SetInt("StatRecentCount", 3);
        PlayerPrefs.Save();

        yield return PlaySymmetricTwoRoundFinish(usePlayerLock: false);
        AssertTerminal("Draw");

        AssertSingleOutcome(
            MatchOutcome.Result.Draw,
            expectedGuesses: 2,
            expectedOpponentGuesses: 2,
            expectedOpened: true,
            expectedLockStaked: false);
        Assert.That(legacyEventCount, Is.Zero,
            "A draw has no truthful legacy win/loss representation.");
        Assert.That(statsChangedCount, Is.EqualTo(1));

        AssertStat("StatWins", 0);
        AssertStat("StatLosses", 0);
        AssertStat("StatDraws", 1);
        AssertStat("StatMatches", 1);
        AssertStat("StatStreak", 4);
        AssertStat("StatBestStreak", 6);
        AssertStat("StatRecentBits", 5);
        AssertStat("StatRecentCount", 3);
        Assert.That(StateProperty("PlayerTurns"), Is.EqualTo(2));
        Assert.That(StateProperty("AiTurns"), Is.EqualTo(2));
        AssertSymmetricHistories();

        yield return null;
    }

    [UnityTest]
    public IEnumerator PlayerLockBreaksEqualCorrectRoundAndIsReportedOnce()
    {
        yield return PlaySymmetricTwoRoundFinish(usePlayerLock: true);
        AssertTerminal("Win");

        AssertSingleOutcome(
            MatchOutcome.Result.Win,
            expectedGuesses: 2,
            expectedOpponentGuesses: 2,
            expectedOpened: true,
            expectedLockStaked: true);
        Assert.That(legacyEventCount, Is.EqualTo(1));
        Assert.That(legacyWon, Is.True);
        Assert.That(legacyGuesses, Is.EqualTo(2));
        Assert.That(statsChangedCount, Is.EqualTo(1));

        AssertStat("StatWins", 1);
        AssertStat("StatLosses", 0);
        AssertStat("StatDraws", 0);
        AssertStat("StatMatches", 1);
        AssertStat("StatStreak", 1);
        AssertStat("StatBestStreak", 1);
        AssertStat("StatBestGuesses", 2);
        AssertStat("StatRecentBits", 1);
        AssertStat("StatRecentCount", 1);
        AssertStat("LockEverUsed", 1);
        Assert.That(StateProperty("PlayerTurns"), Is.EqualTo(2));
        Assert.That(StateProperty("AiTurns"), Is.EqualTo(2));
        AssertSymmetricHistories();

        yield return null;
    }

    [UnityTest]
    public IEnumerator MissedPlayerLockShowsForfeitAndBurnsExactlyTheNextTurn()
    {
        StartWithOpener("Host", 80, 77);
        Assert.That(Invoke(game, "PlayerGuess", 50), Is.EqualTo(true));
        yield return null;
        Acknowledge();
        AssertPhase("OpponentThinking");
        yield return null;
        Acknowledge();
        AssertPhase("OpponentGuess");
        Assert.That(Field(game, "aiGuess"), Is.EqualTo(50));
        yield return null;
        Acknowledge();
        AssertPhase("AnswerOpponent");
        yield return null;
        Acknowledge();
        AssertPhase("PlayerGuess");
        Assert.That(StateProperty("RoundNumber"), Is.EqualTo(2));

        Transform lockButton = Find(panel.transform, "LockButton");
        Assert.That(lockButton, Is.Not.Null);
        Button lockControl = lockButton.GetComponent<Button>();
        Assert.That(lockControl.interactable, Is.True);
        lockControl.onClick.Invoke();
        Assert.That(Field(game, "lockArmed"), Is.EqualTo(true));

        Assert.That(Invoke(game, "PlayerGuess", 60), Is.EqualTo(true));
        AssertPhase("PlayerOutcome");
        object lockedMove = HistoryEvents().Last();
        Assert.That(EventProperty(lockedMove, "Actor").ToString(), Is.EqualTo("Player"));
        Assert.That(EventProperty(lockedMove, "Target").ToString(), Is.EqualTo("Opponent"));
        Assert.That(EventProperty(lockedMove, "LockStaked"), Is.EqualTo(true));
        Assert.That(EventProperty(lockedMove, "LockMissed"), Is.EqualTo(true));

        yield return null;
        Acknowledge();
        AssertPhase("LockForfeit");
        Assert.That(StateProperty("Prompt").ToString(),
            Is.EqualTo("PlayerLockForfeit"));
        Assert.That(StateProperty("ActiveActor").ToString(), Is.EqualTo("Opponent"));
        Assert.That(StateProperty("TargetActor").ToString(), Is.EqualTo("Player"));
        Assert.That(StateProperty("NextAction").ToString(), Is.EqualTo("Continue"));
        Assert.That(StateProperty("LockSpent"), Is.EqualTo(true));

        yield return null;
        Acknowledge();
        AssertPhase("OpponentThinking");
        yield return null;
        Acknowledge();
        AssertPhase("OpponentGuess");
        Assert.That(Field(game, "aiGuess"), Is.EqualTo(75));
        yield return null;
        Acknowledge();
        AssertPhase("AnswerOpponent");
        yield return null;
        Acknowledge();

        AssertPhase("OpponentThinking");
        Assert.That(StateProperty("RoundNumber"), Is.EqualTo(3));
        Assert.That(History("PlayerGuessHistory"), Is.EqualTo(new[] { 50, 60 }));
        Assert.That(History("AiGuessHistory"), Is.EqualTo(new[] { 50, 75 }));
        Assert.That(StateProperty("ActiveActor").ToString(), Is.EqualTo("Opponent"),
            "The player's forfeited round-three slot must hand the AI a consecutive turn.");
    }

    [UnityTest]
    public IEnumerator LiveBackCancelPreservesMatchAndConfirmRecordsOneForfeit()
    {
        StartWithOpener("Host", 75, 77);
        object stateBefore = GetProperty<object>(owner, "CurrentState");
        int historyBefore = HistoryEvents().Length;

        int mainMenuLoads = 0;
        UnityAction<Scene, LoadSceneMode> loaded = (scene, mode) =>
        {
            if (scene.name == "MainMenu")
                mainMenuLoads++;
        };
        SceneManager.sceneLoaded += loaded;
        try
        {
            Button back = Back();
            back.onClick.Invoke();
            yield return null;

            Assert.That(mainMenuLoads, Is.Zero);
            Assert.That(GetProperty<bool>(menu, "IsSoloLeaveConfirmationVisible"),
                Is.True);
            Assert.That(GetProperty<object>(owner, "CurrentState"),
                Is.SameAs(stateBefore));
            Assert.That(HistoryEvents(), Has.Length.EqualTo(historyBefore));

            Invoke(menu, "CancelSoloMatchExit");
            yield return null;
            Assert.That(GetProperty<bool>(menu, "IsSoloLeaveConfirmationVisible"),
                Is.False);
            Assert.That(GetProperty<object>(owner, "CurrentState"),
                Is.SameAs(stateBefore), "Cancel must resume the exact match snapshot.");
            Assert.That(HistoryEvents(), Has.Length.EqualTo(historyBefore));

            back.onClick.Invoke();
            Assert.That(GetProperty<bool>(menu, "IsSoloLeaveConfirmationVisible"),
                Is.True);
            Invoke(menu, "ConfirmSoloMatchExit");
            Invoke(menu, "ConfirmSoloMatchExit");
            yield return WaitUntilOrFail(
                () => mainMenuLoads == 1,
                5f,
                "Confirmed live forfeit did not reload MainMenu.");
            for (int frame = 0; frame < 3; frame++)
                yield return null;

            Assert.That(mainMenuLoads, Is.EqualTo(1));
            Assert.That(completedOutcomes, Has.Count.EqualTo(1));
            Assert.That(completedOutcomes[0].Outcome,
                Is.EqualTo(MatchOutcome.Result.Loss));
            Assert.That(statsChangedCount, Is.EqualTo(1));
            AssertStat("StatLosses", 1);
            AssertStat("StatMatches", 1);
        }
        finally
        {
            SceneManager.sceneLoaded -= loaded;
        }
    }

    [UnityTest]
    public IEnumerator DecidedBackReloadsOnceAndKeepsOnlyPersistentSoloData()
    {
        // A correct Host opener followed by a missed answering AI guess is a
        // deterministic one-round win under last licks.
        StartWithOpener("Host", 75, 77);
        Assert.That(Invoke(game, "PlayerGuess", 77), Is.EqualTo(true));
        AssertPhase("PlayerOutcome");
        yield return null;
        Acknowledge();
        AssertPhase("OpponentThinking");
        Assert.That(StateProperty("Prompt").ToString(),
            Is.EqualTo("MatchPointYours"));
        Assert.That(History("AiGuessHistory"), Is.Empty,
            "The answering AI guess must wait for explicit acknowledgement.");
        yield return null;
        Acknowledge();
        AssertPhase("OpponentGuess");
        yield return null;
        Acknowledge();
        AssertPhase("AnswerOpponent");
        yield return null;
        Acknowledge();
        AssertTerminal("Win");
        AssertSingleOutcome(
            MatchOutcome.Result.Win,
            expectedGuesses: 1,
            expectedOpponentGuesses: 1,
            expectedOpened: true,
            expectedLockStaked: false);

        int mainMenuLoads = 0;
        UnityAction<Scene, LoadSceneMode> loaded = (scene, mode) =>
        {
            if (scene.name == "MainMenu")
                mainMenuLoads++;
        };
        SceneManager.sceneLoaded += loaded;
        try
        {
            Back().onClick.Invoke();
            yield return WaitUntilOrFail(
                () => mainMenuLoads == 1,
                5f,
                "A decided Solo result must leave on the first Back press.");
            for (int frame = 0; frame < 8; frame++)
                yield return null;

            Assert.That(mainMenuLoads, Is.EqualTo(1));
            Assert.That(completedOutcomes, Has.Count.EqualTo(1));
            Assert.That(statsChangedCount, Is.EqualTo(1));
            AssertStat("StatWins", 1);
            AssertStat("StatMatches", 1);
            AssertStat("AIDifficulty", 2);
            AssertStat("HOL.Onboarding.Avatar", SelectedAvatar);
            AssertStat("HOL.Onboarding.Version", 1);
            Assert.That(PlayerPrefs.GetString("PlayerName", string.Empty),
                Is.EqualTo(PlayerName));
            AssertCommittedAvatar(SelectedAvatar);

            Component reloadedGame = FindInScene(RuntimeType("GameManager"));
            Component reloadedNumbers =
                FindInScene(RuntimeType("NumberManager"));
            Assert.That(reloadedGame, Is.Not.Null);
            Assert.That(reloadedNumbers, Is.Not.Null);

            reloadedNumbers.gameObject.SetActive(true);
            for (int frame = 0; frame < 5; frame++)
                yield return null;

            Component reloadedOwner =
                FindInScene(RuntimeType("SoloDuelVisuals"));
            Assert.That(reloadedOwner, Is.Not.Null);
            Assert.That(StateProperty(reloadedOwner, "Phase").ToString(),
                Is.EqualTo("ChooseSecret"));
            Assert.That(History(reloadedOwner, "PlayerGuessHistory"), Is.Empty);
            Assert.That(History(reloadedOwner, "AiGuessHistory"), Is.Empty);
            Assert.That(GetProperty<bool>(reloadedGame, "IsMatchOver"),
                Is.False);

            TMP_InputField reloadedInput = (TMP_InputField)Field(
                reloadedNumbers, "numberInput");
            Assert.That(reloadedInput.text, Is.Empty);
            Assert.That(reloadedInput.gameObject.activeSelf, Is.True);
        }
        finally
        {
            SceneManager.sceneLoaded -= loaded;
        }
    }

    IEnumerator PlaySymmetricTwoRoundFinish(bool usePlayerLock)
    {
        // Both solvers miss at 50, narrowing their objective ranges to the
        // same 50 candidates. They then find their opponent's secret in the
        // same round, so only the player's optional Lock can separate them.
        StartWithOpener("Host", 75, 77);
        Assert.That(Invoke(game, "PlayerGuess", 50), Is.EqualTo(true));
        AssertPhase("PlayerOutcome");
        yield return null;
        Acknowledge();
        AssertPhase("OpponentThinking");
        yield return null;
        Acknowledge();
        AssertPhase("OpponentGuess");
        Assert.That(Field(game, "aiGuess"), Is.EqualTo(50));
        yield return null;
        Acknowledge();
        AssertPhase("AnswerOpponent");
        yield return null;
        Acknowledge();

        AssertPhase("PlayerGuess");
        Assert.That(StateProperty("RoundNumber"), Is.EqualTo(2));
        if (usePlayerLock)
        {
            Transform lockButton = Find(panel.transform, "LockButton");
            Assert.That(lockButton, Is.Not.Null);
            Assert.That(lockButton.gameObject.activeInHierarchy, Is.True);
            Button lockControl = lockButton.GetComponent<Button>();
            Assert.That(lockControl, Is.Not.Null);
            Assert.That(lockControl.interactable, Is.True);
            lockControl.onClick.Invoke();
            Assert.That(Field(game, "lockArmed"), Is.EqualTo(true));
        }

        Assert.That(Invoke(game, "PlayerGuess", 77), Is.EqualTo(true));
        AssertPhase("PlayerOutcome");
        yield return null;
        Acknowledge();
        AssertPhase("OpponentThinking");
        Assert.That(StateProperty("Prompt").ToString(),
            Is.EqualTo("MatchPointYours"));
        yield return null;
        Acknowledge();
        AssertPhase("OpponentGuess");
        Assert.That(Field(game, "aiGuess"), Is.EqualTo(75));
        yield return null;
        Acknowledge();
        AssertPhase("AnswerOpponent");
        yield return null;
        Acknowledge();
        AssertPhase("MatchResult");
    }

    void StartWithOpener(string opener, int playerSecret, int aiSecret)
    {
        Invoke(game, "SetPlayerNumber", playerSecret);
        MethodInfo start = game.GetType().GetMethod(
            "StartGameWithOpener",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(start, Is.Not.Null);
        ParameterInfo[] parameters = start.GetParameters();
        Assert.That(parameters, Has.Length.EqualTo(1));
        Assert.That(parameters[0].ParameterType.IsEnum, Is.True);
        start.Invoke(game, new[]
        {
            Enum.Parse(parameters[0].ParameterType, opener),
        });
        ((MonoBehaviour)game).CancelInvoke("AIGuess");
        SetField(game, "aiSecretNumber", aiSecret);

        AssertPhase("StarterReveal");
        Assert.That(StateProperty("Starter").ToString(),
            Is.EqualTo(opener == "Host" ? "Player" : "Opponent"));
        Assert.That(StateProperty("NextAction").ToString(), Is.EqualTo("Start"));
        Assert.That(HistoryEvents(), Is.Empty);
        Acknowledge();
        AssertPhase(opener == "Host" ? "PlayerGuess" : "OpponentThinking");
    }

    void Acknowledge()
    {
        Invoke(game, "AcknowledgePresentation");
    }

    void AssertTerminal(string prompt)
    {
        AssertPhase("MatchResult");
        Assert.That(StateProperty("Prompt").ToString(), Is.EqualTo(prompt));
        Assert.That(GetProperty<bool>(game, "IsMatchOver"), Is.True);
        Assert.That(input.gameObject.activeSelf, Is.False);
        Assert.That(Submit().gameObject.activeSelf, Is.False);
        Assert.That(((GameObject)Field(game, "stopGameButton")).activeSelf,
            Is.True);
    }

    void AssertSingleOutcome(
        MatchOutcome.Result result,
        int expectedGuesses,
        int expectedOpponentGuesses,
        bool expectedOpened,
        bool expectedLockStaked)
    {
        Assert.That(completedOutcomes, Has.Count.EqualTo(1));
        MatchOutcome outcome = completedOutcomes[0];
        Assert.That(outcome.PlayMode, Is.EqualTo(MatchOutcome.Mode.Solo));
        Assert.That(outcome.Outcome, Is.EqualTo(result));
        Assert.That(outcome.Guesses, Is.EqualTo(expectedGuesses));
        Assert.That(outcome.OpponentGuesses,
            Is.EqualTo(expectedOpponentGuesses));
        Assert.That(outcome.Opened, Is.EqualTo(expectedOpened));
        Assert.That(outcome.LockStaked, Is.EqualTo(expectedLockStaked));
        Assert.That(outcome.RematchIndex, Is.Zero);
        Assert.That(outcome.AppVersion,
            Is.EqualTo(Application.version));
    }

    void AssertSymmetricHistories()
    {
        CollectionAssert.AreEqual(
            new[] { 50, 77 }, History("PlayerGuessHistory"));
        CollectionAssert.AreEqual(
            new[] { 50, 75 }, History("AiGuessHistory"));
        CollectionAssert.AreEqual(
            new[] { "Higher", "Correct" },
            HistoryNames("PlayerGuessHints"));
        CollectionAssert.AreEqual(
            new[] { "Higher", "Correct" },
            HistoryNames("AiGuessHints"));
    }

    void AssertPhase(string expected)
    {
        Assert.That(StateProperty("Phase").ToString(), Is.EqualTo(expected));
    }

    object StateProperty(string name)
    {
        return StateProperty(owner, name);
    }

    static object StateProperty(Component targetOwner, string name)
    {
        object state = GetProperty<object>(targetOwner, "CurrentState");
        PropertyInfo property = state.GetType().GetProperty(
            name, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(property, Is.Not.Null,
            "Missing presentation-state property " + name);
        return property.GetValue(state);
    }

    int[] History(string name)
    {
        return History(owner, name);
    }

    static int[] History(Component targetOwner, string name)
    {
        var values = StateProperty(targetOwner, name) as IEnumerable;
        Assert.That(values, Is.Not.Null, name);
        return values.Cast<object>().Select(Convert.ToInt32).ToArray();
    }

    object[] HistoryEvents()
    {
        var values = StateProperty("History") as IEnumerable;
        Assert.That(values, Is.Not.Null, "History");
        return values.Cast<object>().ToArray();
    }

    static object EventProperty(object historyEvent, string name)
    {
        PropertyInfo property = historyEvent.GetType().GetProperty(
            name, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(property, Is.Not.Null, "Missing history-event property " + name);
        return property.GetValue(historyEvent);
    }

    static void AssertHistoryEvent(
        object historyEvent,
        int sequence,
        int round,
        string actor,
        string target,
        int guess,
        string outcome)
    {
        Assert.That(EventProperty(historyEvent, "Sequence"), Is.EqualTo(sequence));
        Assert.That(EventProperty(historyEvent, "RoundNumber"), Is.EqualTo(round));
        Assert.That(EventProperty(historyEvent, "Actor").ToString(), Is.EqualTo(actor));
        Assert.That(EventProperty(historyEvent, "Target").ToString(), Is.EqualTo(target));
        Assert.That(EventProperty(historyEvent, "Guess"), Is.EqualTo(guess));
        Assert.That(EventProperty(historyEvent, "Outcome").ToString(), Is.EqualTo(outcome));
    }

    string[] HistoryNames(string name)
    {
        var values = StateProperty(name) as IEnumerable;
        Assert.That(values, Is.Not.Null, name);
        return values.Cast<object>().Select(value => value.ToString()).ToArray();
    }

    Button Submit()
    {
        return GetProperty<Button>(owner, "SubmitControl");
    }

    Button Back()
    {
        Transform back = Find(panel.transform, "DuelBack");
        Assert.That(back, Is.Not.Null);
        Assert.That(back.gameObject.activeInHierarchy, Is.True);
        Button control = back.GetComponent<Button>();
        Assert.That(control, Is.Not.Null);
        Assert.That(control.interactable, Is.True);
        return control;
    }

    static void AssertCommittedAvatar(int expected)
    {
        MethodInfo method = RuntimeType("OnboardingProfile").GetMethod(
            "TryLoadCommittedAvatar",
            BindingFlags.Static | BindingFlags.Public);
        Assert.That(method, Is.Not.Null);
        object[] arguments = { -1 };
        bool loaded = (bool)method.Invoke(null, arguments);
        Assert.That(loaded, Is.True);
        Assert.That(arguments[0], Is.EqualTo(expected));
    }

    static void AssertStat(string key, int expected)
    {
        Assert.That(PlayerPrefs.GetInt(key, int.MinValue), Is.EqualTo(expected),
            key);
    }

    static object Invoke(
        Component target, string method, params object[] arguments)
    {
        MethodInfo info = target.GetType().GetMethod(
            method,
            BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.NonPublic);
        Assert.That(info, Is.Not.Null, "Missing method " + method);
        return info.Invoke(target, arguments);
    }

    static object Field(Component target, string name)
    {
        FieldInfo field = target.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Missing field " + name);
        return field.GetValue(target);
    }

    static void SetField(Component target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Missing field " + name);
        field.SetValue(target, value);
    }

    static T GetProperty<T>(object target, string name)
    {
        PropertyInfo property = target.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, "Missing property " + name);
        return (T)property.GetValue(target);
    }

    static Component FindInScene(Type type)
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Component found = root.GetComponentsInChildren(type, true)
                .FirstOrDefault();
            if (found != null)
                return found;
        }
        return null;
    }

    static Transform Find(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;
        for (int index = 0; index < root.childCount; index++)
        {
            Transform found = Find(root.GetChild(index), name);
            if (found != null)
                return found;
        }
        return null;
    }

    static Type RuntimeType(string name)
    {
        Type type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "Missing runtime component: " + name);
        return type;
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

    static void CancelInvokesIfPresent(string typeName)
    {
        Type type = Type.GetType(typeName + ", Assembly-CSharp");
        if (type == null || !SceneManager.GetActiveScene().IsValid())
            return;
        Component component = FindInScene(type);
        if (component is MonoBehaviour behaviour)
            behaviour.CancelInvoke();
    }

    static void EstablishControlledPreferences()
    {
        foreach (string key in IntegerPreferenceKeys)
            PlayerPrefs.SetInt(key, 0);

        PlayerPrefs.SetInt("AIDifficulty", 2);
        PlayerPrefs.SetInt("Language", 0);
        PlayerPrefs.SetInt("AdsConsent", 0);
        PlayerPrefs.SetInt("LockIntroShown", 3);
        PlayerPrefs.SetInt("DailyChallengeDay", TodayKey());
        PlayerPrefs.SetString(
            "DailyLastPlayDate",
            DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        CommitCanonicalProfile();
    }

    static void CommitCanonicalProfile()
    {
        MethodInfo method = RuntimeType("OnboardingProfile").GetMethod(
            "TryCommit", BindingFlags.Static | BindingFlags.Public);
        Assert.That(method, Is.Not.Null);
        ParameterInfo[] parameters = method.GetParameters();
        Assert.That(parameters, Has.Length.EqualTo(4));
        Assert.That(parameters[1].ParameterType.IsEnum, Is.True);
        Assert.That(parameters[3].ParameterType.IsEnum, Is.True);

        bool committed = (bool)method.Invoke(null, new object[]
        {
            PlayerName,
            Enum.Parse(parameters[1].ParameterType, "Boy"),
            SelectedAvatar,
            Enum.Parse(parameters[3].ParameterType, "Teen13To17"),
        });
        Assert.That(committed, Is.True,
            "The fixture profile must use the canonical onboarding contract.");
    }

    static int TodayKey()
    {
        DateTime today = DateTime.UtcNow.Date;
        return today.Year * 10000 + today.Month * 100 + today.Day;
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

        string Key { get; }

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
