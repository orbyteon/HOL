using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class SoloBoardPresenterPlayModeTests
{
    Component game;
    Component numberManager;
    Component layout;
    Component menuManager;
    GameObject panel;
    TMP_InputField input;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        for (int i = 0; i < 8; i++) yield return null;

        game = FindInScene(RuntimeType("GameManager"));
        numberManager = FindInScene(RuntimeType("NumberManager"));
        layout = FindInScene(RuntimeType("SoloDuelVisuals"));
        menuManager = FindInScene(RuntimeType("MenuManager"));
        Assert.That(game, Is.Not.Null);
        Assert.That(numberManager, Is.Not.Null);
        Assert.That(layout, Is.Not.Null);
        Assert.That(menuManager, Is.Not.Null);

        panel = numberManager.gameObject;
        panel.SetActive(true);
        for (int i = 0; i < 4; i++) yield return null;

        input = (TMP_InputField)Field(numberManager, "numberInput");
        Assert.That(input, Is.Not.Null);
        ((MonoBehaviour)game).CancelInvoke();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (game != null) ((MonoBehaviour)game).CancelInvoke();
        if (menuManager != null) ((MonoBehaviour)menuManager).CancelInvoke();
        yield return null;
    }

    [UnityTest]
    public IEnumerator SecretEntryUsesOneSubmitAndSuppressesTheSoftKeyboard()
    {
        AssertPhase("ChooseSecret");
        Assert.That(((TMP_Text)Field(game, "turnText")).text,
            Is.EqualTo(Localized("solo_choose_secret")));
        Assert.That(input.shouldHideMobileInput, Is.True);
        Assert.That(input.shouldHideSoftKeyboard, Is.True);

        Button[] visibleSubmits = panel.GetComponentsInChildren<Button>(true)
            .Where(button => (button.name == "ButtonConfirm" || button.name == "NumberSubmit") &&
                             button.gameObject.activeInHierarchy)
            .ToArray();
        Assert.That(visibleSubmits, Has.Length.EqualTo(1));
        Assert.That(visibleSubmits[0].name, Is.EqualTo("ButtonConfirm"));

        input.text = "42";
        visibleSubmits[0].onClick.Invoke();

        Assert.That(Field(numberManager, "playerNumber"), Is.EqualTo(42));
        Assert.That(Property(State(), "RoundNumber"), Is.EqualTo(1));
        AssertPhase("StarterReveal");
        Assert.That(Property(State(), "NextAction").ToString(), Is.EqualTo("Start"));
        Assert.That(input.gameObject.activeSelf, Is.False);

        TMP_Text round = Find(panel.transform, "RoundLabel").GetComponent<TMP_Text>();
        Assert.That(round.text, Is.EqualTo(Localized("round_label_open", 1)));
        Assert.That(round.text, Does.Not.Contain("/"));
        yield return null;
    }

    [UnityTest]
    public IEnumerator OnScreenKeypadEditsAndSubmitsTheLiveNumericInput()
    {
        AssertPhase("ChooseSecret");
        Assert.That(input.text, Is.Empty,
            "Instructional copy must be placeholder text, not the input value.");

        Button key4 = Find(panel.transform, "Key_4").GetComponent<Button>();
        Button key2 = Find(panel.transform, "Key_2").GetComponent<Button>();
        Assert.That(key4.interactable, Is.True);
        Assert.That(key2.interactable, Is.True);

        key4.onClick.Invoke();
        yield return null;
        Assert.That(input.text, Is.EqualTo("4"));
        Assert.That(RenderedInputText(), Is.EqualTo("4"));

        key2.onClick.Invoke();
        yield return null;
        Assert.That(input.text, Is.EqualTo("42"));
        Assert.That(RenderedInputText(), Is.EqualTo("42"));

        Submit().onClick.Invoke();

        Assert.That(Field(numberManager, "playerNumber"), Is.EqualTo(42));
        AssertPhase("StarterReveal");
    }

    [UnityTest]
    public IEnumerator SecretInputRejectsInvalidAndOutOfRangeValuesAndAcceptsLowerBoundary()
    {
        AssertPhase("ChooseSecret");
        TMP_Text message = (TMP_Text)Field(numberManager, "messageText");

        input.text = string.Empty;
        Submit().onClick.Invoke();
        AssertPhase("ChooseSecret");
        Assert.That(message.gameObject.activeSelf, Is.True);
        Assert.That(message.text, Is.EqualTo(Localized("invalid_number")));

        foreach (string rejected in new[] { "0", "101" })
        {
            input.text = rejected;
            Submit().onClick.Invoke();
            AssertPhase("ChooseSecret");
            Assert.That(message.gameObject.activeSelf, Is.True);
            Assert.That(message.text,
                Is.EqualTo(Localized("number_out_of_range")));
            Assert.That(Field(numberManager, "gameStarted"), Is.EqualTo(false));
        }

        input.text = "1";
        Submit().onClick.Invoke();

        Assert.That(Field(numberManager, "playerNumber"), Is.EqualTo(1));
        Assert.That(Field(numberManager, "gameStarted"), Is.EqualTo(true));
        AssertPhase("StarterReveal");
        yield return null;
    }

    [UnityTest]
    public IEnumerator SubmitIsEnabledOnlyForCompleteOneToOneHundredOnPlayerInput()
    {
        AssertPhase("ChooseSecret");

        foreach (string invalid in new[] { string.Empty, "-", "0", "101" })
        {
            input.text = invalid;
            yield return null;
            Assert.That(Property(numberManager, "HasCompleteValidValue"), Is.False,
                "Value '" + invalid + "' must remain incomplete or invalid.");
            Assert.That(Property(numberManager, "CanSubmitCurrentValue"), Is.False);
            Assert.That(Submit().interactable, Is.False);
        }

        foreach (string valid in new[] { "1", "100" })
        {
            input.text = valid;
            yield return null;
            Assert.That(Property(numberManager, "HasCompleteValidValue"), Is.True);
            Assert.That(Property(numberManager, "CanSubmitCurrentValue"), Is.True);
            Assert.That(Submit().interactable, Is.True);
        }

        StartWithOpener("Host", 73);
        SetField(numberManager, "gameStarted", true);
        input.text = string.Empty;
        yield return null;
        AssertPhase("StarterReveal");
        Assert.That(input.gameObject.activeSelf, Is.False);
        Assert.That(Submit().gameObject.activeSelf, Is.False);
        Assert.That(Property(numberManager, "CanSubmitCurrentValue"), Is.False);

        Acknowledge();
        yield return null;
        AssertPhase("PlayerGuess");
        Assert.That(Submit().interactable, Is.False);

        input.text = "50";
        yield return null;
        Assert.That(Submit().interactable, Is.True);
        Submit().onClick.Invoke();
        AssertPhase("PlayerOutcome");
        Assert.That(Submit().gameObject.activeSelf, Is.False);
        Assert.That(Property(numberManager, "CanSubmitCurrentValue"), Is.False);
    }

    [UnityTest]
    public IEnumerator PlayerOpeningRequiresOutcomeAckThenAiThinkingGuessOutcomeAck()
    {
        var difficulty = new PrefValue("AIDifficulty");
        try
        {
            PlayerPrefs.SetInt("AIDifficulty", 2);
            StartWithOpener("Host", 100);
            SetField(game, "aiSecretNumber", 90);

            AssertPhase("StarterReveal");
            AssertStateFacts("Player", "Opponent", "Start", 1);
            Assert.That(Property(State(), "Prompt").ToString(), Is.EqualTo("PlayerStarts"));
            Assert.That(input.gameObject.activeSelf, Is.False);
            Transform openingLock = Find(panel.transform, "LockButton");
            Assert.That(openingLock, Is.Not.Null);
            Assert.That(openingLock.gameObject.activeInHierarchy, Is.True);
            Assert.That(openingLock.GetComponent<Button>().interactable, Is.False,
                "LOCK must remain visible but disabled during starter acknowledgement.");

            Acknowledge();
            AssertPhase("PlayerGuess");
            AssertStateFacts("Player", "Opponent", "SubmitGuess", 1);
            Assert.That(input.interactable, Is.True);
            Assert.That(Submit().gameObject.activeSelf, Is.True);
            Assert.That(Property(State(), "LockRevealed"), Is.EqualTo(true));
            Assert.That(Property(State(), "LockAvailable"), Is.EqualTo(true));
            Assert.That(openingLock.gameObject.activeInHierarchy, Is.True);
            Assert.That(openingLock.GetComponent<Button>().interactable, Is.True,
                "Opening-turn Lock availability must match DuelRules.LockAvailable.");

            Assert.That(Invoke(game, "PlayerGuess", 50), Is.EqualTo(true));
            AssertPhase("PlayerOutcome");
            AssertStateFacts("Player", "Opponent", "Continue", 1);
            Assert.That(Property(State(), "Prompt").ToString(),
                Is.EqualTo("PlayerGuessedHigher"));
            Assert.That(Property(State(), "DetailValue"), Is.EqualTo(50));
            Assert.That(History("PlayerGuessHistory"), Is.EqualTo(new[] { 50 }));
            Assert.That(Property(State(), "PlayerRangeMin"), Is.EqualTo(51));
            Assert.That(Property(State(), "PlayerRangeMax"), Is.EqualTo(100));
            Assert.That(Property(State(), "AiRangeMin"), Is.EqualTo(1));
            Assert.That(Property(State(), "AiRangeMax"), Is.EqualTo(100));
            Assert.That(input.gameObject.activeSelf, Is.False);
            Assert.That(Submit().gameObject.activeSelf, Is.False);
            Assert.That(openingLock.gameObject.activeInHierarchy, Is.True);
            Assert.That(openingLock.GetComponent<Button>().interactable, Is.False,
                "LOCK must explain itself without accepting input after a guess.");

            yield return null;
            Acknowledge();
            AssertPhase("OpponentThinking");
            AssertStateFacts("Opponent", "Player", "RevealGuess", 1);
            Assert.That(History("AiGuessHistory"), Is.Empty);
            Assert.That(openingLock.gameObject.activeInHierarchy, Is.True);
            Assert.That(openingLock.GetComponent<Button>().interactable, Is.False);

            yield return null;
            Acknowledge();
            AssertPhase("OpponentGuess");
            AssertStateFacts("Opponent", "Player", "RevealOutcome", 1);
            int publishedGuess = (int)Field(game, "aiGuess");
            Assert.That(Property(State(), "DetailValue"), Is.EqualTo(publishedGuess));
            Assert.That(History("AiGuessHistory"), Is.EqualTo(new[] { publishedGuess }));

            yield return null;
            Acknowledge();
            AssertPhase("AnswerOpponent");
            AssertStateFacts("Opponent", "Player", "Continue", 1);
            Assert.That(Property(State(), "Prompt").ToString(),
                Is.EqualTo("OpponentGuessedHigher"));
            Assert.That(Property(State(), "DetailValue"), Is.EqualTo(publishedGuess));
            Assert.That(AnswerActions().Any(action => action.activeSelf), Is.False,
                "The truthful Solo outcome must not expose manual answer controls.");
            Assert.That(input.gameObject.activeSelf, Is.False);
            Assert.That(Submit().gameObject.activeSelf, Is.False);

            yield return null;
            Acknowledge();
            AssertPhase("PlayerGuess");
            Assert.That(Property(State(), "RoundNumber"), Is.EqualTo(2));
            Assert.That(input.gameObject.activeSelf, Is.True);
            Assert.That(input.interactable, Is.True);
            Assert.That(Property(State(), "LockRevealed"), Is.EqualTo(true));
            Assert.That(Property(State(), "LockAvailable"), Is.EqualTo(true));
            Assert.That(openingLock.GetComponent<Button>().interactable, Is.True);

            Assert.That(Invoke(game, "PlayerGuess", 50), Is.EqualTo(false));
            Assert.That(History("PlayerGuessHistory"), Is.EqualTo(new[] { 50 }),
                "A known-range rejection must not become a history event.");
        }
        finally
        {
            difficulty.Restore();
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator RapidDuplicatePlayerSubmissionCreatesOnlyOneTurnAndHistoryEntry()
    {
        StartWithOpener("Host", 73);
        SetField(game, "aiSecretNumber", 90);
        Acknowledge();
        AssertPhase("PlayerGuess");

        Button lockButton = Find(panel.transform, "LockButton")
            .GetComponent<Button>();
        lockButton.onClick.Invoke();
        lockButton.onClick.Invoke();
        Assert.That(Field(game, "lockArmed"), Is.EqualTo(true),
            "A same-frame double tap must commit one deliberate LOCK toggle.");

        Assert.That(Invoke(game, "PlayerGuess", 50), Is.EqualTo(true));
        lockButton.onClick.Invoke();
        Assert.That(Field(game, "lockArmed"), Is.EqualTo(false),
            "LOCK cannot be re-armed after the accepted move.");
        for (int tap = 1; tap < 10; tap++)
        {
            Assert.That(Invoke(game, "PlayerGuess", 50 + tap), Is.EqualTo(false),
                "Rapid submit " + (tap + 1) + " must not create another move.");
        }
        Assert.That(History("PlayerGuessHistory"), Is.EqualTo(new[] { 50 }));
        Assert.That(HistoryEvents(), Has.Length.EqualTo(1));
        Assert.That(Property(HistoryEvents()[0], "LockStaked"), Is.EqualTo(true));
        Assert.That(Property(HistoryEvents()[0], "LockMissed"), Is.EqualTo(true));
        AssertPhase("PlayerOutcome");
        yield return null;
    }

    [UnityTest]
    public IEnumerator AiOpeningRequiresStarterThinkingGuessOutcomeAcknowledgements()
    {
        StartWithOpener("Guest", 100);
        AssertPhase("StarterReveal");
        Assert.That(Property(State(), "Starter").ToString(), Is.EqualTo("Opponent"));
        AssertStateFacts("Opponent", "Player", "Start", 1);
        Assert.That(input.gameObject.activeSelf, Is.False);

        Acknowledge();
        AssertPhase("OpponentThinking");
        AssertStateFacts("Opponent", "Player", "RevealGuess", 1);
        Assert.That(input.gameObject.activeSelf, Is.False);
        Assert.That(History("AiGuessHistory"), Is.Empty);

        yield return null;
        Acknowledge();
        AssertPhase("OpponentGuess");
        AssertStateFacts("Opponent", "Player", "RevealOutcome", 1);
        Assert.That(History("AiGuessHistory"), Has.Length.EqualTo(1));
        Assert.That(input.gameObject.activeSelf, Is.False);

        yield return null;
        Acknowledge();
        AssertPhase("AnswerOpponent");
        Assert.That(History("AiGuessHistory"), Has.Length.EqualTo(1));
        Assert.That(AnswerActions().Any(action => action.activeSelf), Is.False);
        Assert.That(input.gameObject.activeSelf, Is.False);
        Assert.That(Submit().gameObject.activeSelf, Is.False);

        yield return null;
        Acknowledge();
        AssertPhase("PlayerGuess");
        Assert.That(Property(State(), "RoundNumber"), Is.EqualTo(1));
        Assert.That(input.gameObject.activeSelf, Is.True);
        Assert.That(Submit().gameObject.activeSelf, Is.True);
        yield return null;
    }

    [UnityTest]
    public IEnumerator AiTurnAdvancesOnceWithoutPermissionAndHoldsTruthfulOutcome()
    {
        var difficulty = new PrefValue("AIDifficulty");
        try
        {
            PlayerPrefs.SetInt("AIDifficulty", 2);
            StartWithOpener("Guest", 80);
            Acknowledge();
            AssertPhase("OpponentThinking");
            Assert.That(Property(game, "HasAutomaticTransitionScheduled"),
                Is.EqualTo(true));
            Transform continueButton = Find(panel.transform, "SoloContinueButton");
            Assert.That(continueButton, Is.Not.Null);
            Assert.That(continueButton.gameObject.activeInHierarchy, Is.False,
                "AI thinking must not ask the player for permission.");

            yield return new WaitForSecondsRealtime(0.65f);
            AssertPhase("OpponentThinking");
            Assert.That(History("AiGuessHistory"), Is.Empty,
                "The AI must remain visibly thinking for its natural delay.");

            float thinkingStarted = Time.realtimeSinceStartup - 0.65f;
            yield return WaitForPhase("OpponentGuess", 1f);
            Assert.That(Time.realtimeSinceStartup - thinkingStarted,
                Is.InRange(0.8f, 1.45f));
            Assert.That(History("AiGuessHistory"), Has.Length.EqualTo(1));
            Assert.That(continueButton.gameObject.activeInHierarchy, Is.False);

            yield return WaitForPhase("AnswerOpponent", 0.8f);
            AssertPhase("AnswerOpponent");
            Assert.That(AnswerActions().Any(action => action.activeSelf), Is.False);
            Assert.That(input.gameObject.activeSelf, Is.False);
            object stateAtZero = State();
            object[] historyAtZero = HistoryEvents();
            int guessAtZero = Convert.ToInt32(Property(stateAtZero, "DetailValue"));
            string outcomeAtZero = Property(stateAtZero, "LatestAiOutcome").ToString();
            string renderedAtZero = RenderedAiFactSnapshot();
            Assert.That(renderedAtZero, Does.Contain(
                guessAtZero.ToString()),
                "The automatic AI fact must remain visibly rendered.");

            yield return new WaitForSecondsRealtime(1.45f);
            AssertPhase("AnswerOpponent");
            Assert.That(State(), Is.SameAs(stateAtZero));
            Assert.That(HistoryEvents(), Has.Length.EqualTo(historyAtZero.Length));
            Assert.That(Property(State(), "DetailValue"), Is.EqualTo(guessAtZero));
            Assert.That(Property(State(), "LatestAiOutcome").ToString(),
                Is.EqualTo(outcomeAtZero));
            Assert.That(RenderedAiFactSnapshot(), Is.EqualTo(renderedAtZero));

            yield return WaitForPhase("PlayerGuess", 0.6f);
            AssertPhase("PlayerGuess");
            Assert.That(input.gameObject.activeSelf, Is.True);
            Assert.That(HistoryEvents(), Has.Length.EqualTo(historyAtZero.Length),
                "Automatic resolution must not duplicate the AI move.");
            Assert.That(Property(game, "HasAutomaticTransitionScheduled"),
                Is.EqualTo(false));
        }
        finally
        {
            difficulty.Restore();
        }
    }

    [UnityTest]
    public IEnumerator RapidAckBurstAdvancesOneBeatAndLegacyAnswersCannotCorruptTruth()
    {
        var difficulty = new PrefValue("AIDifficulty");
        try
        {
            PlayerPrefs.SetInt("AIDifficulty", 2);
            StartWithOpener("Guest", 80);

            for (int tap = 0; tap < 10; tap++)
                Acknowledge();
            AssertPhase("OpponentThinking");
            Assert.That(History("AiGuessHistory"), Is.Empty,
                "Ten acknowledgement callbacks in one frame must not skip the thinking beat.");

            yield return null;
            Acknowledge();
            AssertPhase("OpponentGuess");
            Assert.That(Field(game, "aiGuess"), Is.EqualTo(50));
            Assert.That(History("AiGuessHistory"), Is.EqualTo(new[] { 50 }));

            Invoke(game, "Lower");
            AssertPhase("OpponentGuess");
            Assert.That(Field(game, "min"), Is.EqualTo(51));
            Assert.That(Field(game, "max"), Is.EqualTo(100));

            yield return null;
            Acknowledge();
            AssertPhase("AnswerOpponent");
            Assert.That(Property(State(), "Prompt").ToString(),
                Is.EqualTo("OpponentGuessedHigher"));

            Invoke(game, "Lower");
            AssertPhase("AnswerOpponent");
            Assert.That(Field(game, "min"), Is.EqualTo(51));
            Assert.That(Field(game, "max"), Is.EqualTo(100));

            yield return null;
            Acknowledge();
            AssertPhase("PlayerGuess");
            Assert.That(Field(game, "min"), Is.EqualTo(51));
            Assert.That(Field(game, "max"), Is.EqualTo(100));
        }
        finally
        {
            difficulty.Restore();
        }
    }

    [UnityTest]
    public IEnumerator PauseAndResumePreserveThinkingAndOutcomeSnapshotsExactly()
    {
        var difficulty = new PrefValue("AIDifficulty");
        try
        {
            PlayerPrefs.SetInt("AIDifficulty", 2);
            StartWithOpener("Guest", 80);
            Acknowledge();
            AssertPhase("OpponentThinking");

            object thinking = State();
            int round = Convert.ToInt32(Property(thinking, "RoundNumber"));
            int min = Convert.ToInt32(Field(game, "min"));
            int max = Convert.ToInt32(Field(game, "max"));
            Invoke(game, "OnApplicationPause", true);
            Invoke(game, "OnApplicationFocus", false);
            yield return new WaitForSecondsRealtime(0.1f);
            Invoke(game, "OnApplicationFocus", true);
            Invoke(game, "OnApplicationPause", false);

            Assert.That(State(), Is.SameAs(thinking));
            Assert.That(Property(State(), "RoundNumber"), Is.EqualTo(round));
            Assert.That(Field(game, "min"), Is.EqualTo(min));
            Assert.That(Field(game, "max"), Is.EqualTo(max));
            Assert.That(HistoryEvents(), Is.Empty,
                "Pause must not synthesize or skip an AI move.");

            Acknowledge();
            AssertPhase("OpponentGuess");
            yield return null;
            Acknowledge();
            AssertPhase("AnswerOpponent");
            object outcome = State();
            int historyCount = HistoryEvents().Length;
            int detail = Convert.ToInt32(Property(outcome, "DetailValue"));

            Invoke(game, "OnApplicationPause", true);
            yield return new WaitForSecondsRealtime(0.1f);
            Invoke(game, "OnApplicationPause", false);
            Assert.That(State(), Is.SameAs(outcome));
            Assert.That(HistoryEvents(), Has.Length.EqualTo(historyCount));
            Assert.That(Property(State(), "DetailValue"), Is.EqualTo(detail));

            yield return null;
            Acknowledge();
            AssertPhase("PlayerGuess");
        }
        finally
        {
            difficulty.Restore();
        }
    }

    [UnityTest]
    public IEnumerator MissingPresentationOwnerFailsClosedBeforeRulesStart()
    {
        Invoke(game, "SetPlayerNumber", 73);
        UnityEngine.Object.DestroyImmediate(layout);
        layout = null;
        SetField(game, "boardPresenter", null);

        MethodInfo start = game.GetType().GetMethod(
            "StartGameWithOpener", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(start, Is.Not.Null);
        Type sideType = start.GetParameters()[0].ParameterType;
        LogAssert.Expect(LogType.Error,
            "[GameManager] Solo presentation rejected starter reveal.");
        start.Invoke(game, new[] { Enum.Parse(sideType, "Host") });
        yield return null;

        Assert.That(Property(game, "CurrentPresentationPhase").ToString(),
            Is.EqualTo("ChooseSecret"));
        Assert.That(Property(game, "HasLiveMatch"), Is.EqualTo(false));
        Assert.That(Field(game, "matchSetUp"), Is.EqualTo(false));
    }

    [UnityTest]
    public IEnumerator ResultAndRematchResetHistoryAndRepublishOpponentIdentity()
    {
        string[] statKeys =
        {
            "StatWins", "StatLosses", "StatStreak", "StatBestStreak", "StatBestGuesses",
            "StatDraws", "StatMatches", "StatRecentBits", "StatRecentCount",
        };
        var prefs = statKeys.ToDictionary(key => key, key => new PrefValue(key));
        var difficulty = new PrefValue("AIDifficulty");

        try
        {
            PlayerPrefs.SetInt("AIDifficulty", 2);
            StartWithOpener("Host", 100);
            SetField(game, "aiSecretNumber", 77);
            game.GetType().GetField("adsManager").SetValue(game, null);

            Acknowledge();
            AssertPhase("PlayerGuess");
            Assert.That(Invoke(game, "PlayerGuess", 77), Is.EqualTo(true));
            AssertPhase("PlayerOutcome");
            yield return null;
            Acknowledge();
            AssertPhase("OpponentThinking");
            yield return null;
            Acknowledge();
            AssertPhase("OpponentGuess");
            yield return null;
            Acknowledge();
            AssertPhase("AnswerOpponent");
            yield return null;
            Acknowledge();

            AssertPhase("MatchResult");
            Assert.That(((TMP_Text)Field(game, "turnText")).text,
                Does.StartWith(Localized("you_win")));
            Assert.That(input.gameObject.activeSelf, Is.False);
            Assert.That(Submit().gameObject.activeSelf, Is.False);
            Assert.That(History("PlayerGuessHistory"), Is.EqualTo(new[] { 77 }));
            Assert.That(History("AiGuessHistory"), Has.Length.EqualTo(1));

            Invoke(game, "RestartMatch");
            AssertPhase("ChooseSecret");
            Assert.That(History("PlayerGuessHistory"), Is.Empty);
            Assert.That(History("AiGuessHistory"), Is.Empty);
            Assert.That(Property(State(), "OpponentName"),
                Is.EqualTo(Property(game, "CurrentOpponentName")));
            Assert.That(((TMP_Text)Field(game, "turnText")).text,
                Is.EqualTo(Localized("solo_choose_secret")));
            Assert.That(input.gameObject.activeSelf, Is.True);
            Assert.That(Submit().gameObject.activeSelf, Is.True);
        }
        finally
        {
            foreach (PrefValue pref in prefs.Values) pref.Restore();
            difficulty.Restore();
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator LiveBackConfirmationCanCancelWithoutChangingTheMatch()
    {
        StartWithOpener("Host", 73);
        object stateBefore = State();
        int historyBefore = HistoryEvents().Length;
        string sceneBefore = SceneManager.GetActiveScene().name;
        Transform back = Find(panel.transform, "DuelBack");
        Assert.That(back, Is.Not.Null);

        back.GetComponent<Button>().onClick.Invoke();
        yield return null;

        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneBefore));
        Assert.That(Property(menuManager, "IsSoloLeaveConfirmationVisible"), Is.True);
        Assert.That(State(), Is.SameAs(stateBefore));
        Assert.That(HistoryEvents(), Has.Length.EqualTo(historyBefore));

        Invoke(menuManager, "CancelSoloMatchExit");
        yield return null;
        Assert.That(Property(menuManager, "IsSoloLeaveConfirmationVisible"), Is.False);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneBefore));
        Assert.That(State(), Is.SameAs(stateBefore),
            "Cancel must resume the exact presentation snapshot.");
        Assert.That(HistoryEvents(), Has.Length.EqualTo(historyBefore));
    }

    void StartWithOpener(string opener, int playerSecret)
    {
        Invoke(game, "SetPlayerNumber", playerSecret);
        MethodInfo start = game.GetType().GetMethod("StartGameWithOpener",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(start, Is.Not.Null);
        ParameterInfo[] parameters = start.GetParameters();
        Assert.That(parameters, Has.Length.EqualTo(1),
            "StartGameWithOpener must take exactly one opener enum.");
        Assert.That(parameters[0].ParameterType.IsEnum, Is.True,
            "StartGameWithOpener opener must remain an enum.");
        start.Invoke(game,
            new[] { Enum.Parse(parameters[0].ParameterType, opener) });
        ((MonoBehaviour)game).CancelInvoke("AIGuess");
    }

    void Acknowledge()
    {
        Invoke(game, "AcknowledgePresentation");
    }

    void AssertPhase(string expected)
    {
        Assert.That(Property(State(), "Phase").ToString(), Is.EqualTo(expected));
    }

    IEnumerator WaitForPhase(string expected, float timeoutSeconds)
    {
        float deadline = Time.realtimeSinceStartup + timeoutSeconds;
        while (Property(State(), "Phase").ToString() != expected &&
               Time.realtimeSinceStartup < deadline)
            yield return null;
        AssertPhase(expected);
    }

    void AssertStateFacts(
        string actor,
        string target,
        string action,
        int round)
    {
        object state = State();
        Assert.That(Property(state, "ActiveActor").ToString(), Is.EqualTo(actor));
        Assert.That(Property(state, "TargetActor").ToString(), Is.EqualTo(target));
        Assert.That(Property(state, "NextAction").ToString(), Is.EqualTo(action));
        Assert.That(Property(state, "RoundNumber"), Is.EqualTo(round));
    }

    object State()
    {
        return Property(layout, "CurrentState");
    }

    int[] History(string name)
    {
        return ((IEnumerable)Property(State(), name)).Cast<object>()
            .Select(Convert.ToInt32).ToArray();
    }

    object[] HistoryEvents()
    {
        return ((IEnumerable)Property(State(), "History")).Cast<object>().ToArray();
    }

    Button Submit()
    {
        return (Button)Property(layout, "SubmitControl");
    }

    string RenderedInputText()
    {
        // TMP appends a zero-width caret marker to its render string while
        // the field is active. It is not part of the submitted value.
        return input.textComponent.text.TrimEnd('\u200B');
    }

    string RenderedAiFactSnapshot()
    {
        string[] names =
        {
            "CentralGuess",
            "CentralOutcome",
            "OpponentLatestGuess",
            "HistoryMeta",
            "HistoryNumber",
            "HistoryOutcome",
        };
        var parts = new List<string>();
        foreach (string name in names)
        {
            Transform found = Find(panel.transform, name);
            Assert.That(found, Is.Not.Null, name);
            TMP_Text text = found.GetComponent<TMP_Text>();
            Assert.That(text, Is.Not.Null, name);
            Assert.That(text.gameObject.activeInHierarchy, Is.True, name);
            Assert.That(text.text, Is.Not.Empty, name);
            parts.Add(name + "=" + text.text);
        }
        return string.Join("|", parts.ToArray());
    }

    GameObject[] AnswerActions()
    {
        return new[]
        {
            (GameObject)Field(game, "higherButton"),
            (GameObject)Field(game, "lowerButton"),
            (GameObject)Field(game, "correctButton"),
        };
    }

    static object Invoke(Component target, string method, params object[] arguments)
    {
        MethodInfo info = target.GetType().GetMethod(method,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(info, Is.Not.Null, "Missing method " + method);
        return info.Invoke(target, arguments);
    }

    static object Field(Component target, string name)
    {
        FieldInfo field = target.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Missing field " + name);
        return field.GetValue(target);
    }

    static void SetField(Component target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Missing field " + name);
        field.SetValue(target, value);
    }

    static object Property(object target, string name)
    {
        PropertyInfo property = target.GetType().GetProperty(name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, "Missing property " + name);
        return property.GetValue(target);
    }

    static string Localized(string key, params object[] arguments)
    {
        MethodInfo get = RuntimeType("L10n").GetMethod("Get",
            BindingFlags.Public | BindingFlags.Static);
        return (string)get.Invoke(null, new object[] { key, arguments });
    }

    static Component FindInScene(Type type)
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Component found = root.GetComponentsInChildren(type, true).FirstOrDefault();
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

    sealed class PrefValue
    {
        readonly string key;
        readonly bool existed;
        readonly int value;

        public PrefValue(string key)
        {
            this.key = key;
            existed = PlayerPrefs.HasKey(key);
            value = PlayerPrefs.GetInt(key, 0);
        }

        public void Restore()
        {
            if (existed) PlayerPrefs.SetInt(key, value);
            else PlayerPrefs.DeleteKey(key);
        }
    }
}
