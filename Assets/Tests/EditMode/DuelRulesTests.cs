using System;
using NUnit.Framework;

// Rule tests for the duel state machine. HOL.EditModeTests references HOL.Core
// directly, so assembly moves and API drift now fail at compile time instead of
// being hidden behind reflection.
//
// playfab/cloudscript.js implements the same rules server-side for PlayFab and
// is covered by the equivalent cases in tools/test/cloudscript.test.mjs. When a
// rule changes here, change it there too.
public class DuelRulesTests
{
    // A duel in progress, wrapped so the tests read like the rules do.
    sealed class Duel
    {
        readonly DuelRules rules = new DuelRules();

        public Duel(DuelRules.Side opener)
        {
            rules.StartMatch(opener);
        }

        public DuelRules.Move Guess(
            DuelRules.Side side,
            int guess,
            int opponentSecret,
            bool useLock = false)
        {
            return rules.Submit(side, guess, opponentSecret, useLock);
        }

        public bool Finished { get { return rules.Finished; } }
        public DuelRules.Outcome Result { get { return rules.Result; } }
        public DuelRules.Side Turn { get { return rules.Turn; } }
        public DuelRules.Side PendingWin { get { return rules.PendingWin; } }

        public int GuessCount(DuelRules.Side side)
        {
            return rules.GuessCount(side);
        }

        public bool LockAvailable(DuelRules.Side side)
        {
            return rules.LockAvailable(side);
        }

        public bool ForfeitPending(DuelRules.Side side)
        {
            return rules.ForfeitPending(side);
        }

        public bool MatchPointAgainst(DuelRules.Side side)
        {
            return rules.IsMatchPointAgainst(side);
        }
    }

    // Host secret 42, guest secret 77 throughout: Host hunts 77, Guest hunts 42.
    const int HostSecret = 42;
    const int GuestSecret = 77;

    [Test]
    public void CorrectOpeningGuessDoesNotEndTheMatch()
    {
        var duel = new Duel(DuelRules.Side.Host);
        var move = duel.Guess(
            DuelRules.Side.Host,
            GuestSecret,
            GuestSecret);

        Assert.AreEqual(DuelRules.Hint.Correct, move.Hint);
        Assert.IsFalse(
            duel.Finished,
            "the round must close before a winner is declared");
        Assert.AreEqual(DuelRules.Side.Host, duel.PendingWin);
        Assert.AreEqual(
            DuelRules.Side.Guest,
            duel.Turn,
            "the responder is owed an answering guess");
        Assert.IsTrue(duel.MatchPointAgainst(DuelRules.Side.Guest));
        Assert.IsFalse(duel.MatchPointAgainst(DuelRules.Side.Host));
    }

    [Test]
    public void MissedAnswerLeavesTheProvisionalWinStanding()
    {
        var duel = new Duel(DuelRules.Side.Host);
        duel.Guess(DuelRules.Side.Host, GuestSecret, GuestSecret);
        duel.Guess(DuelRules.Side.Guest, 50, HostSecret);

        Assert.IsTrue(duel.Finished);
        Assert.AreEqual(DuelRules.Outcome.HostWins, duel.Result);
    }

    [Test]
    public void TiedRoundWithNoLockIsADraw()
    {
        var duel = new Duel(DuelRules.Side.Host);
        duel.Guess(DuelRules.Side.Host, GuestSecret, GuestSecret);
        duel.Guess(DuelRules.Side.Guest, HostSecret, HostSecret);

        Assert.AreEqual(DuelRules.Outcome.Draw, duel.Result);
    }

    [TestCase(DuelRules.Side.Host, DuelRules.Outcome.HostWins)]
    [TestCase(DuelRules.Side.Guest, DuelRules.Outcome.GuestWins)]
    public void TheLockBreaksATiedRound(
        DuelRules.Side locker,
        DuelRules.Outcome expected)
    {
        var duel = new Duel(DuelRules.Side.Host);
        duel.Guess(
            DuelRules.Side.Host,
            GuestSecret,
            GuestSecret,
            locker == DuelRules.Side.Host);
        duel.Guess(
            DuelRules.Side.Guest,
            HostSecret,
            HostSecret,
            locker == DuelRules.Side.Guest);

        Assert.AreEqual(expected, duel.Result);
    }

    [Test]
    public void BothSidesLockingATiedRoundIsStillADraw()
    {
        var duel = new Duel(DuelRules.Side.Host);
        duel.Guess(DuelRules.Side.Host, GuestSecret, GuestSecret, true);
        duel.Guess(DuelRules.Side.Guest, HostSecret, HostSecret, true);

        Assert.AreEqual(DuelRules.Outcome.Draw, duel.Result);
    }

    [Test]
    public void MissedLockForfeitsTheNextTurn()
    {
        var duel = new Duel(DuelRules.Side.Host);
        var move = duel.Guess(
            DuelRules.Side.Host,
            50,
            GuestSecret,
            true);

        Assert.AreEqual(
            DuelRules.Hint.Higher,
            move.Hint,
            "a missed Lock still earns its hint");
        Assert.IsFalse(duel.LockAvailable(DuelRules.Side.Host));
        Assert.IsTrue(duel.ForfeitPending(DuelRules.Side.Host));
        Assert.AreEqual(DuelRules.Side.Guest, duel.Turn);

        // Round two opens on the host, whose slot is burned, so the turn comes
        // straight back to the guest.
        duel.Guess(DuelRules.Side.Guest, 10, HostSecret);
        Assert.AreEqual(DuelRules.Side.Guest, duel.Turn);
        Assert.IsFalse(
            duel.ForfeitPending(DuelRules.Side.Host),
            "the forfeit is spent, not permanent");
        Assert.AreEqual(1, duel.GuessCount(DuelRules.Side.Host));

        duel.Guess(DuelRules.Side.Guest, 20, HostSecret);
        Assert.AreEqual(
            2,
            duel.GuessCount(DuelRules.Side.Guest),
            "the guest really does play twice");
        Assert.AreEqual(
            DuelRules.Side.Host,
            duel.Turn,
            "and the host is back in after paying the forfeit");
    }

    [Test]
    public void ForfeitedResponderCannotAnswerAProvisionalWin()
    {
        var duel = new Duel(DuelRules.Side.Guest);
        duel.Guess(DuelRules.Side.Guest, 50, HostSecret, true);
        duel.Guess(DuelRules.Side.Host, GuestSecret, GuestSecret);

        Assert.IsTrue(duel.Finished, "nobody is left to answer");
        Assert.AreEqual(DuelRules.Outcome.HostWins, duel.Result);
    }

    [Test]
    public void TheLockIsOncePerMatch()
    {
        var duel = new Duel(DuelRules.Side.Host);
        duel.Guess(DuelRules.Side.Host, 50, GuestSecret, true);
        duel.Guess(DuelRules.Side.Guest, 10, HostSecret);
        duel.Guess(DuelRules.Side.Guest, 20, HostSecret);

        var rejected = duel.Guess(
            DuelRules.Side.Host,
            60,
            GuestSecret,
            true);
        Assert.IsFalse(rejected.Accepted);
        Assert.AreEqual("lock already spent", rejected.Error);
    }

    [Test]
    public void TurnAndRangeDisciplineAreEnforced()
    {
        var duel = new Duel(DuelRules.Side.Host);

        var jumped = duel.Guess(
            DuelRules.Side.Guest,
            50,
            HostSecret);
        Assert.IsFalse(jumped.Accepted);
        Assert.AreEqual("not your turn", jumped.Error);

        foreach (int outOfRange in new[] { 0, 101, -5 })
        {
            Assert.IsFalse(
                duel.Guess(
                    DuelRules.Side.Host,
                    outOfRange,
                    GuestSecret).Accepted,
                outOfRange + " is outside 1-100 and must be rejected");
        }
    }

    // The reason the rules were rewritten: under "first correct guess wins" the
    // opener took 63.7% of duels against an identical opponent, because both
    // sides reach every guess number in lockstep. Equal turns removes that.
    [Test]
    public void TurnOrderNoLongerDecidesMatchesBetweenEqualPlayers()
    {
        const int runs = 6000;
        var rng = new Random(20260813);
        int openerWins = 0;
        int responderWins = 0;

        for (int i = 0; i < runs; i++)
        {
            DuelRules.Side opener = rng.Next(2) == 0
                ? DuelRules.Side.Host
                : DuelRules.Side.Guest;
            var duel = new Duel(opener);

            int hostSecret = rng.Next(1, 101);
            int guestSecret = rng.Next(1, 101);
            int hostLo = 1;
            int hostHi = 100;
            int guestLo = 1;
            int guestHi = 100;

            for (int step = 0; step < 60 && !duel.Finished; step++)
            {
                bool hostTurn = duel.Turn == DuelRules.Side.Host;
                int guess = hostTurn
                    ? (hostLo + hostHi) / 2
                    : (guestLo + guestHi) / 2;
                DuelRules.Side side = hostTurn
                    ? DuelRules.Side.Host
                    : DuelRules.Side.Guest;
                var move = duel.Guess(
                    side,
                    guess,
                    hostTurn ? guestSecret : hostSecret);

                Assert.IsTrue(
                    move.Accepted,
                    "simulated move rejected: " + move.Error);

                if (move.Hint == DuelRules.Hint.Higher)
                {
                    if (hostTurn) hostLo = guess + 1;
                    else guestLo = guess + 1;
                }
                else if (move.Hint == DuelRules.Hint.Lower)
                {
                    if (hostTurn) hostHi = guess - 1;
                    else guestHi = guess - 1;
                }
            }

            Assert.IsTrue(duel.Finished, "simulated match failed to finish");

            DuelRules.Side winner = duel.Result == DuelRules.Outcome.HostWins
                ? DuelRules.Side.Host
                : duel.Result == DuelRules.Outcome.GuestWins
                    ? DuelRules.Side.Guest
                    : DuelRules.Side.None;
            if (winner == DuelRules.Side.None) continue;
            if (winner == opener) openerWins++;
            else responderWins++;
        }

        double gap = Math.Abs(openerWins - responderWins) / (double)runs;
        Assert.Less(
            gap,
            0.05,
            string.Format(
                "opener won {0} and responder {1} of {2} — turn order still decides matches",
                openerWins,
                responderWins,
                runs));
    }
}
