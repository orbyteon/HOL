using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public sealed class SoloBoardPresentationStateTests
{
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

    static object NewModel()
    {
        return Activator.CreateInstance(RuntimeType("SoloBoardPresentationModel"));
    }

    static object Current(object model)
    {
        return model.GetType().GetProperty("Current").GetValue(model);
    }

    static object EnumValue(string typeName, string value)
    {
        return Enum.Parse(RuntimeType(typeName), value);
    }

    static object Property(object target, string name)
    {
        return target.GetType().GetProperty(name).GetValue(target);
    }

    static int[] History(object state, string property)
    {
        return ((IEnumerable)Property(state, property)).Cast<object>()
            .Select(Convert.ToInt32).ToArray();
    }

    static string[] HintHistory(object state, string property)
    {
        return ((IEnumerable)Property(state, property)).Cast<object>()
            .Select(value => value.ToString()).ToArray();
    }

    static void Record(object model, string method, int guess, string hint)
    {
        model.GetType().GetMethod(method).Invoke(model, new[]
        {
            (object)guess,
            EnumValue("DuelRules+Hint", hint),
        });
    }

    static void Begin(object model, string opponent)
    {
        model.GetType().GetMethod("BeginNewMatch").Invoke(model, new object[] { opponent });
    }

    static void Present(object model, string phase, string prompt, int round, int min, int max)
    {
        model.GetType().GetMethod("Present").Invoke(model, new[]
        {
            EnumValue("SoloBoardPhase", phase),
            EnumValue("SoloBoardPrompt", prompt),
            (object)round,
            min,
            max,
            0,
        });
    }

    [Test]
    public void TypedStateCoversEveryRequiredSoloPhase()
    {
        CollectionAssert.AreEqual(new[]
        {
            "ChooseSecret", "PlayerGuess", "OpponentThinking",
            "AnswerOpponent", "RoundResolution", "MatchResult",
            "StarterReveal", "PlayerOutcome", "OpponentGuess",
            "LastLicks", "LockForfeit",
        }, Enum.GetNames(RuntimeType("SoloBoardPhase")));
    }

    [Test]
    public void NumericAndSubmitControlsFollowOnlyTheTruthfulPhases()
    {
        object model = NewModel();
        Begin(model, "Nikos");

        Assert.That(Property(Current(model), "NumericControlsAvailable"), Is.True);
        Assert.That(Property(Current(model), "SubmitControlVisible"), Is.True);

        Present(model, "PlayerGuess", "YourGuess", 1, 1, 100);
        Assert.That(Property(Current(model), "NumericControlsAvailable"), Is.True);

        foreach (string phase in new[]
                 {
                     "StarterReveal", "PlayerOutcome", "OpponentThinking",
                     "OpponentGuess", "AnswerOpponent", "LastLicks",
                     "LockForfeit", "RoundResolution", "MatchResult",
                 })
        {
            string prompt = phase == "StarterReveal" ? "PlayerStarts" :
                phase == "PlayerOutcome" ? "PlayerGuessedHigher" :
                phase == "OpponentGuess" ? "OpponentGuess" :
                phase == "LastLicks" ? "LastLicks" :
                phase == "LockForfeit" ? "PlayerLockForfeit" :
                phase == "AnswerOpponent" ? "AnswerOpponent" :
                phase == "MatchResult" ? "Win" :
                phase == "RoundResolution" ? "ResolvingRound" : "OpponentThinking";
            Present(model, phase, prompt, 1, 1, 100);
            Assert.That(Property(Current(model), "NumericControlsAvailable"), Is.False, phase);
            Assert.That(Property(Current(model), "SubmitControlVisible"), Is.False, phase);
        }
    }

    [Test]
    public void PhaseChangesPreserveAcceptedHistoryIncludingRepeatedEvents()
    {
        object model = NewModel();
        Begin(model, "Kostas");
        Record(model, "RecordPlayerGuess", 42, "Higher");
        Record(model, "RecordPlayerGuess", 42, "Correct");
        Record(model, "RecordAiGuess", 50, "Lower");
        Record(model, "RecordAiGuess", 50, "Higher");

        Present(model, "AnswerOpponent", "AnswerOpponent", 3, 43, 81);
        object state = Current(model);
        CollectionAssert.AreEqual(new[] { 42, 42 }, History(state, "PlayerGuessHistory"));
        CollectionAssert.AreEqual(new[] { 50, 50 }, History(state, "AiGuessHistory"));
        CollectionAssert.AreEqual(new[] { "Higher", "Correct" },
            HintHistory(state, "PlayerGuessHints"));
        CollectionAssert.AreEqual(new[] { "Lower", "Higher" },
            HintHistory(state, "AiGuessHints"));
        Assert.That(Property(state, "RoundNumber"), Is.EqualTo(3));
        Assert.That(Property(state, "RangeMin"), Is.EqualTo(43));
        Assert.That(Property(state, "RangeMax"), Is.EqualTo(81));
    }

    [Test]
    public void OnlyBeginningANewMatchClearsHistoryAndRefreshesOpponent()
    {
        object model = NewModel();
        Begin(model, "Marco");
        Record(model, "RecordPlayerGuess", 25, "Higher");
        Present(model, "OpponentThinking", "OpponentThinking", 2, 26, 100);
        Assert.That(History(Current(model), "PlayerGuessHistory"), Has.Length.EqualTo(1));

        Begin(model, "Andreas");
        object state = Current(model);
        Assert.That(Property(state, "Phase").ToString(), Is.EqualTo("ChooseSecret"));
        Assert.That(Property(state, "OpponentName"), Is.EqualTo("Andreas"));
        Assert.That(History(state, "PlayerGuessHistory"), Is.Empty);
        Assert.That(History(state, "AiGuessHistory"), Is.Empty);
    }

    [Test]
    public void InvalidPresentedRangesAreRejectedInsteadOfSilentlyDisplayed()
    {
        object model = NewModel();
        Begin(model, "Luca");

        var error = Assert.Throws<TargetInvocationException>(() =>
            Present(model, "PlayerGuess", "YourGuess", 1, 80, 20));
        Assert.That(error.InnerException, Is.TypeOf<ArgumentOutOfRangeException>());
    }

    [TestCase("Player", "PlayerStarts", "PlayerGuess")]
    [TestCase("Opponent", "OpponentStarts", "OpponentThinking")]
    public void BothStartersAreBlockingFactsUntilTheirExplicitAcknowledgement(
        string starter,
        string expectedPrompt,
        string expectedNextPhase)
    {
        object model = NewModel();
        Begin(model, "Nikos");
        Assert.That(Call(model, "SetPlayerSecret", 73), Is.True);

        Assert.That(Call(model, "RevealStarter",
            EnumValue("SoloBoardActor", starter), 1, 1, 100, 1, 100), Is.True);
        AssertState(model, "StarterReveal", expectedPrompt, starter,
            starter == "Player" ? "Opponent" : "Player", "Start", 1);
        Assert.That(Property(Current(model), "AcknowledgeControlVisible"), Is.True);
        Assert.That(Events(model), Is.Empty);

        bool advanced = expectedNextPhase == "PlayerGuess"
            ? Call(model, "BeginPlayerTurn", 1, 1, 100, 1, 100, false)
            : Call(model, "BeginOpponentThinking", 1, 1, 100, 1, 100);
        Assert.That(advanced, Is.True);
        Assert.That(Property(Current(model), "Phase").ToString(),
            Is.EqualTo(expectedNextPhase));
    }

    [Test]
    public void PlayerOutcomeAndAiThinkingGuessOutcomeRequireExactOrderedTransitions()
    {
        object model = NewModel();
        Begin(model, "Eleni");
        Assert.That(Call(model, "SetPlayerSecret", 73), Is.True);
        Assert.That(Call(model, "RevealStarter",
            EnumValue("SoloBoardActor", "Player"), 1, 1, 100, 1, 100), Is.True);
        Assert.That(Call(model, "BeginPlayerTurn", 1, 1, 100, 1, 100, false), Is.True);

        Assert.That(Call(model, "RecordPlayerMove", 1, 40,
            EnumValue("DuelRules+Hint", "Higher"), false, 100,
            41, 100, 1, 100), Is.True);
        AssertState(model, "PlayerOutcome", "PlayerGuessedHigher", "Player",
            "Opponent", "Continue", 1);
        Assert.That(Property(Current(model), "DetailValue"), Is.EqualTo(40));

        Assert.That(Call(model, "BeginOpponentThinking",
            1, 41, 100, 1, 100), Is.True);
        AssertState(model, "OpponentThinking", "OpponentThinking", "Opponent",
            "Player", "RevealGuess", 1);

        Assert.That(Call(model, "RecordOpponentMove", 1, 60,
            EnumValue("DuelRules+Hint", "Lower"), false, 100,
            41, 100, 1, 59), Is.True);
        AssertState(model, "OpponentGuess", "OpponentGuess", "Opponent",
            "Player", "RevealOutcome", 1);
        Assert.That(Property(Current(model), "DetailValue"), Is.EqualTo(60));

        Assert.That(Call(model, "RevealOpponentOutcome"), Is.True);
        AssertState(model, "AnswerOpponent", "OpponentGuessedLower", "Opponent",
            "Player", "Continue", 1);

        Assert.That(Call(model, "BeginPlayerTurn",
            2, 41, 100, 1, 59, false), Is.True);
        AssertState(model, "PlayerGuess", "YourGuess", "Player", "Opponent",
            "SubmitGuess", 2);
    }

    [Test]
    public void CorrectPlayerGuessMakesTheAnsweringAiTurnExplicitBeforeItsGuess()
    {
        object model = NewModel();
        Begin(model, "Nikos");
        Assert.That(Call(model, "SetPlayerSecret", 73), Is.True);
        Assert.That(Call(model, "RevealStarter",
            EnumValue("SoloBoardActor", "Player"),
            1, 1, 100, 1, 100), Is.True);
        Assert.That(Call(model, "BeginPlayerTurn",
            1, 1, 100, 1, 100, false), Is.True);
        Assert.That(Call(model, "RecordPlayerMove", 1, 77,
            EnumValue("DuelRules+Hint", "Correct"), false, 100,
            1, 100, 1, 100), Is.True);

        object[] beforeAcknowledgement = Events(model);
        AssertState(model, "PlayerOutcome", "PlayerGuessedCorrect", "Player",
            "Opponent", "Continue", 1);
        Assert.That(Call(model, "BeginOpponentThinking",
            1, 1, 100, 1, 100), Is.True);
        AssertState(model, "OpponentThinking", "MatchPointYours", "Opponent",
            "Player", "RevealGuess", 1);
        Assert.That(Events(model), Has.Length.EqualTo(
            beforeAcknowledgement.Length),
            "The answering AI move must not exist before REVEAL GUESS.");
    }

    [TestCase("Player", "Opponent")]
    [TestCase("Opponent", "Player")]
    public void LockForfeitHighlightsTheActorWhoActuallyPlaysNext(
        string forfeiter,
        string nextActor)
    {
        object model = NewModel();
        Begin(model, "Nikos");
        Assert.That(Call(model, "SetPlayerSecret", 73), Is.True);
        Assert.That(Call(model, "RevealStarter",
            EnumValue("SoloBoardActor", "Player"),
            1, 1, 100, 1, 100), Is.True);
        Assert.That(Call(model, "BeginPlayerTurn",
            1, 1, 100, 1, 100, false), Is.True);
        Assert.That(Call(model, "RecordPlayerMove", 1, 40,
            EnumValue("DuelRules+Hint", "Higher"), true, 100,
            41, 100, 1, 100), Is.True);

        Assert.That(Call(model, "ShowLockForfeit",
            EnumValue("SoloBoardActor", forfeiter), 1), Is.True);
        AssertState(
            model, "LockForfeit",
            forfeiter == "Player"
                ? "PlayerLockForfeit"
                : "OpponentLockForfeit",
            nextActor, forfeiter, "Continue", 1);
    }

    [Test]
    public void UnifiedHistoryRetainsChronologicalRoundActorTargetGuessAndOutcome()
    {
        object model = NewModel();
        Begin(model, "Kostas");
        Assert.That(Call(model, "SetPlayerSecret", 75), Is.True);
        Assert.That(Call(model, "RevealStarter",
            EnumValue("SoloBoardActor", "Player"), 1, 1, 100, 1, 100), Is.True);
        Assert.That(Call(model, "BeginPlayerTurn", 1, 1, 100, 1, 100, false), Is.True);
        Assert.That(Call(model, "RecordPlayerMove", 1, 50,
            EnumValue("DuelRules+Hint", "Higher"), false, 100,
            51, 100, 1, 100), Is.True);
        Assert.That(Call(model, "BeginOpponentThinking", 1, 51, 100, 1, 100), Is.True);
        Assert.That(Call(model, "RecordOpponentMove", 1, 50,
            EnumValue("DuelRules+Hint", "Higher"), false, 100,
            51, 100, 51, 100), Is.True);
        Assert.That(Call(model, "RevealOpponentOutcome"), Is.True);
        Assert.That(Call(model, "BeginPlayerTurn", 2, 51, 100, 51, 100, false), Is.True);
        Assert.That(Call(model, "RecordPlayerMove", 2, 77,
            EnumValue("DuelRules+Hint", "Correct"), true, 50,
            51, 100, 51, 100), Is.True);

        object[] events = Events(model);
        Assert.That(events, Has.Length.EqualTo(3));
        AssertEvent(events[0], 1, 1, "Player", "Opponent", 50, "Higher", false, false);
        AssertEvent(events[1], 2, 1, "Opponent", "Player", 50, "Higher", false, false);
        AssertEvent(events[2], 3, 2, "Player", "Opponent", 77, "Correct", true, false);

        CollectionAssert.AreEqual(new[] { 50, 77 }, History(Current(model), "PlayerGuessHistory"));
        CollectionAssert.AreEqual(new[] { 50 }, History(Current(model), "AiGuessHistory"));
    }

    [Test]
    public void PlayerAndAiRangesAreIndependentAndRejectedMovesAreIdempotent()
    {
        object model = NewModel();
        Begin(model, "Marco");
        Assert.That(Call(model, "SetPlayerSecret", 75), Is.True);
        Assert.That(Call(model, "RevealStarter",
            EnumValue("SoloBoardActor", "Player"), 1, 10, 90, 20, 80), Is.True);
        Assert.That(Call(model, "BeginPlayerTurn", 1, 10, 90, 20, 80, false), Is.True);
        Assert.That(Call(model, "RecordPlayerMove", 1, 42,
            EnumValue("DuelRules+Hint", "Higher"), false, 81,
            43, 90, 20, 80), Is.True);

        object afterPlayer = Current(model);
        Assert.That(Property(afterPlayer, "PlayerRangeMin"), Is.EqualTo(43));
        Assert.That(Property(afterPlayer, "PlayerRangeMax"), Is.EqualTo(90));
        Assert.That(Property(afterPlayer, "AiRangeMin"), Is.EqualTo(20));
        Assert.That(Property(afterPlayer, "AiRangeMax"), Is.EqualTo(80));
        Assert.That(Call(model, "RecordPlayerMove", 1, 43,
            EnumValue("DuelRules+Hint", "Higher"), false, 48,
            44, 90, 20, 80), Is.False);
        Assert.That(Events(model), Has.Length.EqualTo(1));
        Assert.That(Current(model), Is.SameAs(afterPlayer));

        Assert.That(Call(model, "BeginOpponentThinking", 1, 43, 90, 20, 80), Is.True);
        Assert.That(Call(model, "RecordOpponentMove", 1, 66,
            EnumValue("DuelRules+Hint", "Lower"), false, 61,
            43, 90, 20, 65), Is.True);
        object afterAi = Current(model);
        Assert.That(Property(afterAi, "PlayerRangeMin"), Is.EqualTo(43));
        Assert.That(Property(afterAi, "PlayerRangeMax"), Is.EqualTo(90));
        Assert.That(Property(afterAi, "AiRangeMin"), Is.EqualTo(20));
        Assert.That(Property(afterAi, "AiRangeMax"), Is.EqualTo(65));
    }

    [Test]
    public void RejectedPhaseAndRoundTransitionsLeaveNoLatentState()
    {
        object model = NewModel();
        Begin(model, "Nikos");
        Assert.That(Call(model, "SetPlayerSecret", 73), Is.True);

        AssertRejectedWithoutMutation(model, () => Call(model,
            "BeginPlayerTurn", 1, 10, 90, 20, 80, false));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "BeginOpponentThinking", 1, 10, 90, 20, 80));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "RecordPlayerMove", 1, 42,
            EnumValue("DuelRules+Hint", "Higher"), false, 81,
            43, 90, 20, 80));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "RecordOpponentMove", 1, 42,
            EnumValue("DuelRules+Hint", "Higher"), false, 61,
            10, 90, 43, 80));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "RevealOpponentOutcome"));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "ShowLastLicks", 1));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "ShowLockForfeit", EnumValue("SoloBoardActor", "Player"), 1));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "CompleteMatch", EnumValue("DuelRules+Outcome", "HostWins"),
            73, 24, 2, 2));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "RevealStarter", EnumValue("SoloBoardActor", "Player"),
            1, 80, 20, 1, 100));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "RevealStarter", EnumValue("SoloBoardActor", "Player"),
            0, 10, 90, 20, 80));

        Assert.That(Call(model, "RevealStarter",
            EnumValue("SoloBoardActor", "Player"),
            1, 1, 100, 1, 100), Is.True);
        AssertRejectedWithoutMutation(model, () => Call(model,
            "RevealStarter", EnumValue("SoloBoardActor", "Opponent"),
            2, 10, 90, 20, 80));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "BeginOpponentThinking", 0, 10, 90, 20, 80));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "BeginPlayerTurn", 0, 10, 90, 20, 80, false));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "BeginPlayerTurn", 1, 90, 10, 20, 80, false));

        Assert.That(Call(model, "BeginPlayerTurn",
            1, 1, 100, 1, 100, false), Is.True);
        AssertRejectedWithoutMutation(model, () => Call(model,
            "BeginOpponentThinking", 1, 10, 90, 20, 80));
    }

    [Test]
    public void RejectedMoveAndResultFactsLeaveNoLatentState()
    {
        object model = NewModel();
        Begin(model, "Marco");
        Assert.That(Call(model, "SetPlayerSecret", 75), Is.True);
        Assert.That(Call(model, "RevealStarter",
            EnumValue("SoloBoardActor", "Player"),
            1, 10, 90, 20, 80), Is.True);
        Assert.That(Call(model, "BeginPlayerTurn",
            1, 10, 90, 20, 80, false), Is.True);

        AssertRejectedWithoutMutation(model, () => Call(model,
            "RecordPlayerMove", 0, 42,
            EnumValue("DuelRules+Hint", "Higher"), false, 81,
            43, 90, 20, 80));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "RecordPlayerMove", 1, 0,
            EnumValue("DuelRules+Hint", "Higher"), false, 81,
            43, 90, 20, 80));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "RecordPlayerMove", 1, 42,
            EnumValue("DuelRules+Hint", "None"), false, 81,
            43, 90, 20, 80));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "RecordPlayerMove", 1, 42,
            EnumValue("DuelRules+Hint", "Higher"), false, 0,
            43, 90, 20, 80));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "RecordPlayerMove", 1, 42,
            EnumValue("DuelRules+Hint", "Higher"), false, 81,
            91, 90, 20, 80));

        Assert.That(Call(model, "RecordPlayerMove", 1, 42,
            EnumValue("DuelRules+Hint", "Higher"), false, 81,
            43, 90, 20, 80), Is.True);
        AssertRejectedWithoutMutation(model, () => Call(model,
            "ShowLastLicks", 0));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "ShowLockForfeit", EnumValue("SoloBoardActor", "None"), 1));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "ShowLockForfeit", EnumValue("SoloBoardActor", "Player"), 0));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "CompleteMatch", Enum.ToObject(
                RuntimeType("DuelRules+Outcome"), 99), 75, 24, 2, 2));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "CompleteMatch", EnumValue("DuelRules+Outcome", "HostWins"),
            0, 24, 2, 2));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "CompleteMatch", EnumValue("DuelRules+Outcome", "HostWins"),
            75, 101, 2, 2));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "CompleteMatch", EnumValue("DuelRules+Outcome", "HostWins"),
            75, 24, -1, 2));

        Assert.That(Call(model, "BeginOpponentThinking",
            1, 43, 90, 20, 80), Is.True);
        AssertRejectedWithoutMutation(model, () => Call(model,
            "RecordOpponentMove", 0, 66,
            EnumValue("DuelRules+Hint", "Lower"), false, 61,
            43, 90, 20, 65));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "RecordOpponentMove", 1, 101,
            EnumValue("DuelRules+Hint", "Lower"), false, 61,
            43, 90, 20, 65));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "RecordOpponentMove", 1, 66,
            EnumValue("DuelRules+Hint", "None"), false, 61,
            43, 90, 20, 65));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "RecordOpponentMove", 1, 66,
            EnumValue("DuelRules+Hint", "Lower"), false, 0,
            43, 90, 20, 65));
        AssertRejectedWithoutMutation(model, () => Call(model,
            "RecordOpponentMove", 1, 66,
            EnumValue("DuelRules+Hint", "Lower"), false, 61,
            43, 90, 66, 65));
    }

    [Test]
    public void OutcomeRevealRequiresARecordedValidOpponentMove()
    {
        object model = NewModel();
        Begin(model, "Luca");
        Present(model, "OpponentGuess", "OpponentGuess", 1, 1, 100);

        AssertRejectedWithoutMutation(model, () => Call(model,
            "RevealOpponentOutcome"));
    }

    static bool Call(object target, string method, params object[] arguments)
    {
        MethodInfo info = target.GetType().GetMethod(
            method, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(info, Is.Not.Null, "Missing method " + method);
        return (bool)info.Invoke(target, arguments);
    }

    static object[] Events(object model)
    {
        return ((IEnumerable)Property(Current(model), "History")).Cast<object>().ToArray();
    }

    static void AssertRejectedWithoutMutation(
        object model, Func<bool> operation)
    {
        object before = Current(model);
        string beforeFingerprint = Fingerprint(before);

        Assert.That(operation(), Is.False);
        Assert.That(Current(model), Is.SameAs(before),
            "A rejected operation replaced the immutable current snapshot.");

        RepublishWithoutChangingFacts(model, before);
        Assert.That(Fingerprint(Current(model)), Is.EqualTo(beforeFingerprint),
            "A rejected operation changed latent model state.");
    }

    static void RepublishWithoutChangingFacts(object model, object state)
    {
        MethodInfo update = model.GetType().GetMethod(
            "UpdateLockState", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(update, Is.Not.Null);
        update.Invoke(model, new[]
        {
            Property(state, "LockRevealed"),
            Property(state, "LockAvailable"),
            Property(state, "LockArmed"),
            Property(state, "LockSpent"),
            Property(state, "LockCandidates"),
        });
    }

    static string Fingerprint(object value)
    {
        if (value == null) return "<null>";

        Type type = value.GetType();
        if (value is string || type.IsPrimitive || type.IsEnum ||
            value is decimal)
            return value.ToString();

        var sequence = value as IEnumerable;
        if (sequence != null)
        {
            return "[" + string.Join(",",
                sequence.Cast<object>().Select(Fingerprint).ToArray()) + "]";
        }

        PropertyInfo[] properties = type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead &&
                               property.GetIndexParameters().Length == 0)
            .OrderBy(property => property.Name)
            .ToArray();
        return type.Name + "{" + string.Join(",",
            properties.Select(property =>
                    property.Name + "=" + Fingerprint(property.GetValue(value)))
                .ToArray()) + "}";
    }

    static void AssertState(
        object model,
        string phase,
        string prompt,
        string actor,
        string target,
        string nextAction,
        int round)
    {
        object state = Current(model);
        Assert.That(Property(state, "Phase").ToString(), Is.EqualTo(phase));
        Assert.That(Property(state, "Prompt").ToString(), Is.EqualTo(prompt));
        Assert.That(Property(state, "ActiveActor").ToString(), Is.EqualTo(actor));
        Assert.That(Property(state, "TargetActor").ToString(), Is.EqualTo(target));
        Assert.That(Property(state, "NextAction").ToString(), Is.EqualTo(nextAction));
        Assert.That(Property(state, "RoundNumber"), Is.EqualTo(round));
    }

    static void AssertEvent(
        object item,
        int sequence,
        int round,
        string actor,
        string target,
        int guess,
        string outcome,
        bool lockStaked,
        bool lockMissed)
    {
        Assert.That(Property(item, "Sequence"), Is.EqualTo(sequence));
        Assert.That(Property(item, "RoundNumber"), Is.EqualTo(round));
        Assert.That(Property(item, "Actor").ToString(), Is.EqualTo(actor));
        Assert.That(Property(item, "Target").ToString(), Is.EqualTo(target));
        Assert.That(Property(item, "Guess"), Is.EqualTo(guess));
        Assert.That(Property(item, "Outcome").ToString(), Is.EqualTo(outcome));
        Assert.That(Property(item, "LockStaked"), Is.EqualTo(lockStaked));
        Assert.That(Property(item, "LockMissed"), Is.EqualTo(lockMissed));
    }
}
