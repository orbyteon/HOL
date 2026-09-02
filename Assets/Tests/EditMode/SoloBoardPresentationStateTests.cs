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

        foreach (string phase in new[] { "OpponentThinking", "AnswerOpponent", "RoundResolution", "MatchResult" })
        {
            Present(model, phase, phase == "AnswerOpponent" ? "AnswerOpponent" :
                phase == "MatchResult" ? "Win" :
                phase == "RoundResolution" ? "ResolvingRound" : "OpponentThinking", 1, 1, 100);
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
}
