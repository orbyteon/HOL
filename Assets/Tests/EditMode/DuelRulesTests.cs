using System;
using System.Reflection;
using NUnit.Framework;

// Rule tests for the duel state machine. Reflection keeps the editor-only test
// assembly decoupled from Assembly-CSharp, matching L10nIntegrityTests.
//
// playfab/cloudscript.js implements the same rules server-side for PlayFab and
// is covered by the equivalent cases in tools/test/cloudscript.test.mjs. When a
// rule changes here, change it there too.
public class DuelRulesTests
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

    static Type RulesType { get { return FindGameType("DuelRules"); } }

    static Type Nested(string name)
    {
        var t = RulesType.GetNestedType(name);
        Assert.IsNotNull(t, "DuelRules." + name + " not found — renamed?");
        return t;
    }

    static object Side(string name) { return Enum.Parse(Nested("Side"), name); }

    // A duel in progress, wrapped so the tests read like the rules do.
    class Duel
    {
        readonly object rules;

        public Duel(string opener)
        {
            rules = Activator.CreateInstance(RulesType);
            RulesType.GetMethod("StartMatch").Invoke(rules, new[] { Side(opener) });
        }

        public Move Guess(string side, int guess, int opponentSecret, bool useLock = false)
        {
            var move = RulesType.GetMethod("Submit")
                .Invoke(rules, new object[] { Side(side), guess, opponentSecret, useLock });
            return new Move(move);
        }

        object Get(string property) { return RulesType.GetProperty(property).GetValue(rules); }

        bool Ask(string method, string side)
        {
            return (bool)RulesType.GetMethod(method).Invoke(rules, new[] { Side(side) });
        }

        public bool Finished { get { return (bool)Get("Finished"); } }
        public string Result { get { return Get("Result").ToString(); } }
        public string Turn { get { return Get("Turn").ToString(); } }
        public string PendingWin { get { return Get("PendingWin").ToString(); } }

        public int GuessCount(string side)
        {
            return (int)RulesType.GetMethod("GuessCount").Invoke(rules, new[] { Side(side) });
        }

        public bool LockAvailable(string side) { return Ask("LockAvailable", side); }
        public bool ForfeitPending(string side) { return Ask("ForfeitPending", side); }
        public bool MatchPointAgainst(string side) { return Ask("IsMatchPointAgainst", side); }
    }

    class Move
    {
        readonly object move;
        public Move(object move) { this.move = move; }

        object Field(string name) { return move.GetType().GetField(name).GetValue(move); }

        public bool Accepted { get { return (bool)Field("Accepted"); } }
        public string Error { get { return (string)Field("Error"); } }
        public string Hint { get { return Field("Hint").ToString(); } }
    }

    // Host secret 42, guest secret 77 throughout: "Host" hunts 77, "Guest" hunts 42.
    const int HostSecret = 42;
    const int GuestSecret = 77;

    // ------------------------------------------------------------------ tests

    [Test]
    public void CorrectOpeningGuessDoesNotEndTheMatch()
    {
        var duel = new Duel("Host");
        var move = duel.Guess("Host", GuestSecret, GuestSecret);

        Assert.AreEqual("Correct", move.Hint);
        Assert.IsFalse(duel.Finished, "the round must close before a winner is declared");
        Assert.AreEqual("Host", duel.PendingWin);
        Assert.AreEqual("Guest", duel.Turn, "the responder is owed an answering guess");
        Assert.IsTrue(duel.MatchPointAgainst("Guest"));
        Assert.IsFalse(duel.MatchPointAgainst("Host"));
    }

    [Test]
    public void MissedAnswerLeavesTheProvisionalWinStanding()
    {
        var duel = new Duel("Host");
        duel.Guess("Host", GuestSecret, GuestSecret);
        duel.Guess("Guest", 50, HostSecret);

        Assert.IsTrue(duel.Finished);
        Assert.AreEqual("HostWins", duel.Result);
    }

    [Test]
    public void TiedRoundWithNoLockIsADraw()
    {
        var duel = new Duel("Host");
        duel.Guess("Host", GuestSecret, GuestSecret);
        duel.Guess("Guest", HostSecret, HostSecret);

        Assert.AreEqual("Draw", duel.Result);
    }

    [TestCase("Host", "HostWins")]
    [TestCase("Guest", "GuestWins")]
    public void TheLockBreaksATiedRound(string locker, string expected)
    {
        var duel = new Duel("Host");
        duel.Guess("Host", GuestSecret, GuestSecret, locker == "Host");
        duel.Guess("Guest", HostSecret, HostSecret, locker == "Guest");

        Assert.AreEqual(expected, duel.Result);
    }

    [Test]
    public void BothSidesLockingATiedRoundIsStillADraw()
    {
        var duel = new Duel("Host");
        duel.Guess("Host", GuestSecret, GuestSecret, true);
        duel.Guess("Guest", HostSecret, HostSecret, true);

        Assert.AreEqual("Draw", duel.Result);
    }

    [Test]
    public void MissedLockForfeitsTheNextTurn()
    {
        var duel = new Duel("Host");
        var move = duel.Guess("Host", 50, GuestSecret, true);

        Assert.AreEqual("Higher", move.Hint, "a missed Lock still earns its hint");
        Assert.IsFalse(duel.LockAvailable("Host"));
        Assert.IsTrue(duel.ForfeitPending("Host"));
        Assert.AreEqual("Guest", duel.Turn);

        // Round two opens on the host, whose slot is burned, so the turn comes
        // straight back to the guest.
        duel.Guess("Guest", 10, HostSecret);
        Assert.AreEqual("Guest", duel.Turn);
        Assert.IsFalse(duel.ForfeitPending("Host"), "the forfeit is spent, not permanent");
        Assert.AreEqual(1, duel.GuessCount("Host"));

        duel.Guess("Guest", 20, HostSecret);
        Assert.AreEqual(2, duel.GuessCount("Guest"), "the guest really does play twice");
        Assert.AreEqual("Host", duel.Turn, "and the host is back in after paying the forfeit");
    }

    [Test]
    public void ForfeitedResponderCannotAnswerAProvisionalWin()
    {
        var duel = new Duel("Guest");
        duel.Guess("Guest", 50, HostSecret, true);  // opens, misses the Lock
        duel.Guess("Host", GuestSecret, GuestSecret);

        Assert.IsTrue(duel.Finished, "nobody is left to answer");
        Assert.AreEqual("HostWins", duel.Result);
    }

    [Test]
    public void TheLockIsOncePerMatch()
    {
        var duel = new Duel("Host");
        duel.Guess("Host", 50, GuestSecret, true);
        duel.Guess("Guest", 10, HostSecret);
        duel.Guess("Guest", 20, HostSecret);

        var rejected = duel.Guess("Host", 60, GuestSecret, true);
        Assert.IsFalse(rejected.Accepted);
        Assert.AreEqual("lock already spent", rejected.Error);
    }

    [Test]
    public void TurnAndRangeDisciplineAreEnforced()
    {
        var duel = new Duel("Host");

        var jumped = duel.Guess("Guest", 50, HostSecret);
        Assert.IsFalse(jumped.Accepted);
        Assert.AreEqual("not your turn", jumped.Error);

        foreach (int outOfRange in new[] { 0, 101, -5 })
            Assert.IsFalse(duel.Guess("Host", outOfRange, GuestSecret).Accepted,
                outOfRange + " is outside 1-100 and must be rejected");
    }

    // The reason the rules were rewritten: under "first correct guess wins" the
    // opener took 63.7% of duels against an identical opponent, because both
    // sides reach every guess number in lockstep. Equal turns removes that.
    [Test]
    public void TurnOrderNoLongerDecidesMatchesBetweenEqualPlayers()
    {
        const int runs = 6000;
        var rng = new System.Random(20260813);
        int openerWins = 0, responderWins = 0;

        for (int i = 0; i < runs; i++)
        {
            string opener = rng.Next(2) == 0 ? "Host" : "Guest";
            var duel = new Duel(opener);

            int hostSecret = rng.Next(1, 101);
            int guestSecret = rng.Next(1, 101);
            int hostLo = 1, hostHi = 100, guestLo = 1, guestHi = 100;

            for (int step = 0; step < 60 && !duel.Finished; step++)
            {
                bool hostTurn = duel.Turn == "Host";
                int guess = hostTurn ? (hostLo + hostHi) / 2 : (guestLo + guestHi) / 2;
                var move = duel.Guess(hostTurn ? "Host" : "Guest", guess,
                    hostTurn ? guestSecret : hostSecret);

                Assert.IsTrue(move.Accepted, "simulated move rejected: " + move.Error);

                if (move.Hint == "Higher")
                {
                    if (hostTurn) hostLo = guess + 1; else guestLo = guess + 1;
                }
                else if (move.Hint == "Lower")
                {
                    if (hostTurn) hostHi = guess - 1; else guestHi = guess - 1;
                }
            }

            Assert.IsTrue(duel.Finished, "simulated match failed to finish");

            string winner = duel.Result == "HostWins" ? "Host"
                : duel.Result == "GuestWins" ? "Guest" : "";
            if (winner == "") continue;
            if (winner == opener) openerWins++; else responderWins++;
        }

        double gap = Math.Abs(openerWins - responderWins) / (double)runs;
        Assert.Less(gap, 0.05,
            string.Format("opener won {0} and responder {1} of {2} — turn order still decides matches",
                openerWins, responderWins, runs));
    }
}
