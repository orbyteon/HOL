using System;
using NUnit.Framework;

// Compile-time contracts for the Unity-free HOL.Application outcome and event
// boundary. A rename or signature drift must fail compilation instead of a
// reflection lookup at runtime.
public class MatchOutcomeTests
{
    static MatchOutcome NewOutcome(
        MatchOutcome.Mode mode,
        MatchOutcome.Result result,
        int guesses,
        int opponentGuesses,
        bool opened,
        bool lockStaked,
        int rematchIndex,
        string version)
    {
        return new MatchOutcome
        {
            PlayMode = mode,
            Outcome = result,
            Guesses = guesses,
            OpponentGuesses = opponentGuesses,
            Opened = opened,
            LockStaked = lockStaked,
            RematchIndex = rematchIndex,
            AppVersion = version,
        };
    }

    [SetUp]
    public void ClearBeforeEach() => ClearStaticHandlers();

    [TearDown]
    public void ClearAfterEach() => ClearStaticHandlers();

    static void ClearStaticHandlers()
    {
        GameEvents.OnMatchEnded = null;
        GameEvents.OnMatchCompleted = null;
        GameEvents.OnStatsChanged = null;
        GameEvents.OnDailyStreak = null;
    }

    [Test]
    public void BodyJsonMatchesTheDocumentedContract()
    {
        var drawn = NewOutcome(MatchOutcome.Mode.Pvp, MatchOutcome.Result.Draw,
            6, 6, true, false, 2, "0.3.0");

        Assert.AreEqual(
            "{\"mode\":\"pvp\",\"result\":\"draw\",\"guesses\":6,\"opponentGuesses\":6," +
            "\"opened\":true,\"lockStaked\":false,\"rematchIndex\":2,\"appVersion\":\"0.3.0\"}",
            drawn.BodyJson());
    }

    [Test]
    public void ADrawIsExpressible()
    {
        StringAssert.Contains("\"result\":\"draw\"", NewOutcome(
            MatchOutcome.Mode.Pvp, MatchOutcome.Result.Draw,
            5, 5, false, false, 0, "0.3.0").BodyJson());
    }

    [Test]
    public void ALossKeepsItsGuessCount()
    {
        StringAssert.Contains("\"guesses\":7", NewOutcome(
            MatchOutcome.Mode.Solo, MatchOutcome.Result.Loss,
            7, 5, false, true, 0, "0.3.0").BodyJson());
    }

    [Test]
    public void PlayerEventWrapsTheBodyForPlayFab()
    {
        var outcome = NewOutcome(MatchOutcome.Mode.Solo, MatchOutcome.Result.Win,
            4, 6, true, true, 0, "0.3.0");

        Assert.AreEqual(
            "{\"EventName\":\"match_completed\",\"Body\":" + outcome.BodyJson() + "}",
            outcome.PlayerEventJson("match_completed"));
    }

    [Test]
    public void VersionStringIsEscaped()
    {
        string body = NewOutcome(MatchOutcome.Mode.Solo, MatchOutcome.Result.Win,
            1, 1, true, false, 0, "0.3\"x\\y").BodyJson();
        StringAssert.Contains("\"appVersion\":\"0.3\\\"x\\\\y\"", body);
    }

    [Test]
    public void CompletedEventCarriesTheTypedOutcome()
    {
        bool fired = false;
        MatchOutcome received = default;
        GameEvents.OnMatchCompleted = outcome =>
        {
            fired = true;
            received = outcome;
        };

        var expected = NewOutcome(MatchOutcome.Mode.Pvp, MatchOutcome.Result.Draw,
            6, 6, true, false, 1, "0.3.0");
        GameEvents.MatchCompleted(expected);

        Assert.IsTrue(fired);
        Assert.AreEqual(expected.Outcome, received.Outcome);
        Assert.AreEqual(expected.Guesses, received.Guesses);
    }

    [Test]
    public void AWinStillReachesLegacyListenersWithItsGuessCount()
    {
        bool fired = false;
        bool wonArg = false;
        int guessesArg = -1;
        GameEvents.OnMatchEnded = (won, guesses) =>
        {
            fired = true;
            wonArg = won;
            guessesArg = guesses;
        };

        GameEvents.MatchCompleted(NewOutcome(MatchOutcome.Mode.Pvp,
            MatchOutcome.Result.Win, 5, 6, true, false, 0, "0.3.0"));

        Assert.IsTrue(fired);
        Assert.IsTrue(wonArg);
        Assert.AreEqual(5, guessesArg);
    }

    [Test]
    public void ALossStillReachesLegacyListenersAsZeroGuesses()
    {
        int guessesArg = -1;
        GameEvents.OnMatchEnded = (won, guesses) =>
        {
            Assert.IsFalse(won);
            guessesArg = guesses;
        };

        GameEvents.MatchCompleted(NewOutcome(MatchOutcome.Mode.Solo,
            MatchOutcome.Result.Loss, 7, 4, false, true, 0, "0.3.0"));

        Assert.AreEqual(0, guessesArg);
    }

    [Test]
    public void ADrawNeverReachesWinLoseListeners()
    {
        bool fired = false;
        GameEvents.OnMatchEnded = (_, __) => fired = true;

        GameEvents.MatchCompleted(NewOutcome(MatchOutcome.Mode.Pvp,
            MatchOutcome.Result.Draw, 6, 6, true, false, 1, "0.3.0"));

        Assert.IsFalse(fired);
    }

    [Test]
    public void EveryResultRefreshesStatsExactlyOnce()
    {
        foreach (MatchOutcome.Result result in Enum.GetValues(typeof(MatchOutcome.Result)))
        {
            ClearStaticHandlers();
            int calls = 0;
            GameEvents.OnStatsChanged = () => calls++;

            GameEvents.MatchCompleted(NewOutcome(MatchOutcome.Mode.Pvp,
                result, 5, 5, true, false, 0, "0.3.0"));

            Assert.AreEqual(1, calls,
                result + " must refresh the stats listeners exactly once");
        }
    }
}
