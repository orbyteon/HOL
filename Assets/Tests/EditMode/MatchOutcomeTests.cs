using System;
using System.Reflection;
using NUnit.Framework;

// The match-outcome record and the event hub that raises it.
//
// Two things are worth pinning down here. The first is the wire format: the
// field names in MatchOutcome.BodyJson are a contract with whatever reads the
// PlayStream events, and renaming one silently splits a metric in half across a
// release boundary. The second is that adding the draw-capable event did not
// change what the existing win/lose listeners see — ExtrasRuntimeWiring is
// still bound to OnMatchEnded, and a draw must still not reach it, because
// (bool, int) has no truthful way to say "draw".
//
// Reflection keeps the editor-only test assembly decoupled from
// Assembly-CSharp, matching DuelRulesTests and L10nIntegrityTests.
public class MatchOutcomeTests
{
    // ------------------------------------------------------------- reflection

    static Type FindGameType(string name)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(name);
            if (t != null) return t;
        }
        Assert.Fail("Type '" + name + "' not found in loaded assemblies — renamed?");
        return null;
    }

    static Type OutcomeType => FindGameType("MatchOutcome");
    static Type EventsType => FindGameType("GameEvents");

    static object NewOutcome(string mode, string result, int guesses, int opponentGuesses,
                             bool opened, bool lockStaked, int rematchIndex, string version)
    {
        var t = OutcomeType;
        object boxed = Activator.CreateInstance(t);

        Set(boxed, "PlayMode", Enum.Parse(t.GetNestedType("Mode"), mode));
        Set(boxed, "Outcome", Enum.Parse(t.GetNestedType("Result"), result));
        Set(boxed, "Guesses", guesses);
        Set(boxed, "OpponentGuesses", opponentGuesses);
        Set(boxed, "Opened", opened);
        Set(boxed, "LockStaked", lockStaked);
        Set(boxed, "RematchIndex", rematchIndex);
        Set(boxed, "AppVersion", version);
        return boxed;
    }

    static void Set(object boxed, string field, object value)
    {
        var f = boxed.GetType().GetField(field);
        Assert.IsNotNull(f, "MatchOutcome." + field + " not found — renamed?");
        f.SetValue(boxed, value);
    }

    static string BodyJson(object outcome) =>
        (string)OutcomeType.GetMethod("BodyJson").Invoke(outcome, null);

    // ------------------------------------------------------------ wire format

    [Test]
    public void BodyJsonMatchesTheDocumentedContract()
    {
        var drawn = NewOutcome("Pvp", "Draw", 6, 6, true, false, 2, "0.3.0");

        Assert.AreEqual(
            "{\"mode\":\"pvp\",\"result\":\"draw\",\"guesses\":6,\"opponentGuesses\":6," +
            "\"opened\":true,\"lockStaked\":false,\"rematchIndex\":2,\"appVersion\":\"0.3.0\"}",
            BodyJson(drawn));
    }

    [Test]
    public void ADrawIsExpressible()
    {
        // The reason this record exists: OnMatchEnded's (bool, int) cannot say
        // "draw", so the draw paths had to route around analytics entirely.
        StringAssert.Contains("\"result\":\"draw\"", BodyJson(NewOutcome(
            "Pvp", "Draw", 5, 5, false, false, 0, "0.3.0")));
    }

    [Test]
    public void ALossKeepsItsGuessCount()
    {
        // GameEvents.MatchEnded(false, 0) threw this away, which made the draw
        // rate uninterpretable: you cannot tell a close duel from a rout.
        StringAssert.Contains("\"guesses\":7", BodyJson(NewOutcome(
            "Solo", "Loss", 7, 5, false, true, 0, "0.3.0")));
    }

    [Test]
    public void PlayerEventWrapsTheBodyForPlayFab()
    {
        var outcome = NewOutcome("Solo", "Win", 4, 6, true, true, 0, "0.3.0");
        string wrapped = (string)OutcomeType.GetMethod("PlayerEventJson")
            .Invoke(outcome, new object[] { "match_completed" });

        Assert.AreEqual(
            "{\"EventName\":\"match_completed\",\"Body\":" + BodyJson(outcome) + "}",
            wrapped);
    }

    [Test]
    public void VersionStringIsEscaped()
    {
        string body = BodyJson(NewOutcome("Solo", "Win", 1, 1, true, false, 0, "0.3\"x\\y"));
        StringAssert.Contains("\"appVersion\":\"0.3\\\"x\\\\y\"", body);
    }

    // -------------------------------------------------- event hub compatibility

    FieldInfo MatchEndedField => EventsType.GetField("OnMatchEnded",
        BindingFlags.Public | BindingFlags.Static);

    FieldInfo StatsChangedField => EventsType.GetField("OnStatsChanged",
        BindingFlags.Public | BindingFlags.Static);

    [SetUp]
    public void ClearBeforeEach() => ClearStaticHandlers();

    [TearDown]
    public void ClearAfterEach() => ClearStaticHandlers();

    // The hub's handlers are static, so a leaked subscription from one test
    // would fire inside the next one.
    void ClearStaticHandlers()
    {
        MatchEndedField.SetValue(null, null);
        StatsChangedField.SetValue(null, null);
        EventsType.GetField("OnMatchCompleted", BindingFlags.Public | BindingFlags.Static)
            .SetValue(null, null);
    }

    void RaiseCompleted(object outcome)
    {
        var m = EventsType.GetMethod("MatchCompleted",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(m, "GameEvents.MatchCompleted not found — renamed?");
        m.Invoke(null, new[] { outcome });
    }

    [Test]
    public void AnalyticsEventExistsAndTakesAnOutcome()
    {
        var field = EventsType.GetField("OnMatchCompleted",
            BindingFlags.Public | BindingFlags.Static);

        Assert.IsNotNull(field, "GameEvents.OnMatchCompleted not found — renamed?");
        Assert.AreEqual(typeof(Action<>).MakeGenericType(OutcomeType), field.FieldType);
    }

    [Test]
    public void AWinStillReachesTheLegacyListenersWithItsGuessCount()
    {
        bool fired = false, wonArg = false;
        int guessesArg = -1;
        MatchEndedField.SetValue(null, (Action<bool, int>)((won, guesses) =>
        {
            fired = true; wonArg = won; guessesArg = guesses;
        }));

        RaiseCompleted(NewOutcome("Pvp", "Win", 5, 6, true, false, 0, "0.3.0"));

        Assert.IsTrue(fired, "a win must still raise OnMatchEnded");
        Assert.IsTrue(wonArg);
        Assert.AreEqual(5, guessesArg);
    }

    [Test]
    public void ALossStillReachesTheLegacyListenersAsZeroGuesses()
    {
        // Deliberately unchanged: ExtrasRuntimeWiring's engagement hooks read
        // this argument, and altering it here would be a silent behaviour
        // change dressed up as a telemetry addition.
        int guessesArg = -1;
        MatchEndedField.SetValue(null, (Action<bool, int>)((won, guesses) =>
        {
            Assert.IsFalse(won); guessesArg = guesses;
        }));

        RaiseCompleted(NewOutcome("Solo", "Loss", 7, 4, false, true, 0, "0.3.0"));

        Assert.AreEqual(0, guessesArg);
    }

    [Test]
    public void ADrawStillNeverReachesTheWinLoseListeners()
    {
        bool fired = false;
        MatchEndedField.SetValue(null, (Action<bool, int>)((won, guesses) => fired = true));

        RaiseCompleted(NewOutcome("Pvp", "Draw", 6, 6, true, false, 1, "0.3.0"));

        Assert.IsFalse(fired,
            "a draw has no truthful (bool, int) form; it must not reach OnMatchEnded");
    }

    [Test]
    public void EveryResultRefreshesTheStatsListeners()
    {
        foreach (var result in new[] { "Win", "Loss", "Draw" })
        {
            ClearStaticHandlers();

            int calls = 0;
            StatsChangedField.SetValue(null, (Action)(() => calls++));
            RaiseCompleted(NewOutcome("Pvp", result, 5, 5, true, false, 0, "0.3.0"));

            Assert.AreEqual(1, calls, result + " must refresh the stats listeners exactly once");
        }
    }
}
