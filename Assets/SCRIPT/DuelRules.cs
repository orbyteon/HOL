using System;

// The HOL duel state machine: rounds, last licks, and the Lock.
//
// One implementation shared by solo play (GameManager) and the Firebase
// development fallback (PvpClient). playfab/cloudscript.js mirrors it for the
// server-authoritative PlayFab backend — keep the two in step. The same cases
// are covered on each side by Assets/Tests/EditMode/DuelRulesTests.cs and
// tools/test/cloudscript.test.mjs.
//
// Deliberately free of UnityEngine references so the rules can be exercised
// without a scene, a backend, or a running game.
//
// Why the rules look like this: under the previous "first correct guess wins"
// rule two players who both binary-search reach every guess number in lockstep,
// so whoever moved first won 63.7% of duels. Turn order, not skill, decided the
// match. Three rules fix that:
//
//   * The opener is a coin flip and stays fixed for the whole match.
//   * A round is one guess per side, and the match can only end when a round
//     closes — so the responder always answers the opener's winning guess
//     ("last licks"). Efficiency decides the duel instead of tempo.
//   * Each side holds one Lock. Staking it on a correct guess wins a same-round
//     tie; staking it on a wrong guess forfeits that side's next turn.
//   * If the Lock does not separate a tied round, the win goes to whoever had
//     narrowed the number further: finding it among two candidates is a better
//     claim than hitting it among twelve.
//
// A round can still tie on all three counts, and that is an honest draw. It
// happens about a quarter of the time between two flawless binary searchers,
// who narrow in lockstep and so cannot be separated by anything drawn from
// their own search — but that is the worst case, not the normal one. Once
// players stray from the midpoint at all the guess counts diverge, and at
// human accuracy the draw rate lands near 8%.
public class DuelRules
{
    public enum Side { None = 0, Host = 1, Guest = 2 }
    public enum Hint { None = 0, Higher = 1, Lower = 2, Correct = 3 }
    public enum Outcome { Undecided = 0, HostWins = 1, GuestWins = 2, Draw = 3 }

    public struct Move
    {
        public bool Accepted;
        public string Error;
        public Hint Hint;
    }

    // Match shape
    public Side Opener { get; private set; }
    public Side Turn { get; private set; }
    public int RoundIndex { get; private set; }

    // Provisional result: someone has found the number but the round is still
    // open, so the other side is owed its answering guess. PendingWin is None
    // for a tied round, which is why the fact that a result exists is tracked
    // separately from who holds it.
    public Side PendingWin { get; private set; }
    bool pendingResolved;
    bool pendingWinLocked;

    public bool Finished { get; private set; }
    public Outcome Result { get; private set; }

    public int HostGuessCount { get; private set; }
    public int GuestGuessCount { get; private set; }

    bool actedHost, actedGuest;
    bool skipHost, skipGuest;
    bool lockUsedHost, lockUsedGuest;

    // The interval each side's received hints leave open. Objective: it follows
    // from the answers given, not from how well the player used them.
    int hostLo, hostHi, guestLo, guestHi;
    int pendingWinCandidates;

    public static Side Other(Side side)
    {
        if (side == Side.Host) return Side.Guest;
        if (side == Side.Guest) return Side.Host;
        return Side.None;
    }

    public void StartMatch(Side opener)
    {
        if (opener != Side.Host && opener != Side.Guest)
            throw new ArgumentException("opener must be Host or Guest", "opener");

        Opener = opener;
        Turn = opener;
        RoundIndex = 0;
        PendingWin = Side.None;
        pendingResolved = false;
        pendingWinLocked = false;
        Finished = false;
        Result = Outcome.Undecided;
        HostGuessCount = 0;
        GuestGuessCount = 0;
        actedHost = actedGuest = false;
        skipHost = skipGuest = false;
        lockUsedHost = lockUsedGuest = false;
        hostLo = guestLo = 1;
        hostHi = guestHi = 100;
        pendingWinCandidates = 0;
    }

    // How many numbers this side has not ruled out yet.
    public int CandidatesFor(Side side)
    {
        int lo = side == Side.Host ? hostLo : guestLo;
        int hi = side == Side.Host ? hostHi : guestHi;
        return hi >= lo ? hi - lo + 1 : 1;
    }

    void Narrow(Side side, int guess, Hint hint)
    {
        if (side == Side.Host)
        {
            if (hint == Hint.Higher && guess + 1 > hostLo) hostLo = guess + 1;
            else if (hint == Hint.Lower && guess - 1 < hostHi) hostHi = guess - 1;
        }
        else
        {
            if (hint == Hint.Higher && guess + 1 > guestLo) guestLo = guess + 1;
            else if (hint == Hint.Lower && guess - 1 < guestHi) guestHi = guess - 1;
        }
    }

    public bool LockAvailable(Side side)
    {
        return side == Side.Host ? !lockUsedHost : !lockUsedGuest;
    }

    public bool ForfeitPending(Side side)
    {
        return side == Side.Host ? skipHost : skipGuest;
    }

    public int GuessCount(Side side)
    {
        return side == Side.Host ? HostGuessCount : GuestGuessCount;
    }

    // True while the given side is one guess away from losing the match: the
    // opponent has already found the number and this is the answering turn.
    public bool IsMatchPointAgainst(Side side)
    {
        return !Finished && PendingWin != Side.None && PendingWin == Other(side);
    }

    public Move Submit(Side side, int guess, int opponentSecret, bool useLock)
    {
        var move = new Move { Accepted = false, Hint = Hint.None, Error = "" };

        if (Finished) { move.Error = "match over"; return move; }
        if (side != Side.Host && side != Side.Guest) { move.Error = "not a member"; return move; }
        if (Turn != side) { move.Error = "not your turn"; return move; }
        if (guess < 1 || guess > 100) { move.Error = "bad guess"; return move; }
        if (useLock && !LockAvailable(side)) { move.Error = "lock already spent"; return move; }

        bool correct = guess == opponentSecret;
        move.Hint = correct ? Hint.Correct : (guess < opponentSecret ? Hint.Higher : Hint.Lower);
        move.Accepted = true;

        if (side == Side.Host) HostGuessCount++;
        else GuestGuessCount++;

        if (useLock)
        {
            if (side == Side.Host) lockUsedHost = true;
            else lockUsedGuest = true;
        }

        // Candidates left before this guess narrowed anything: the pool the
        // guess was actually drawn from.
        int candidates = CandidatesFor(side);
        Narrow(side, guess, move.Hint);

        if (correct)
        {
            if (pendingResolved)
            {
                // Both sides found the number in the same round.
                PendingWin = ResolveTie(side, useLock, candidates);
            }
            else
            {
                pendingResolved = true;
                PendingWin = side;
                pendingWinLocked = useLock;
                pendingWinCandidates = candidates;
            }
        }
        else if (useLock)
        {
            // Staked the Lock and missed: the next turn is forfeit.
            if (side == Side.Host) skipHost = true;
            else skipGuest = true;
        }

        MarkActed(side);
        Advance();
        return move;
    }

    // The Lock decides first. Failing that, the tighter search wins.
    Side ResolveTie(Side latest, bool latestLocked, int latestCandidates)
    {
        if (latestLocked && !pendingWinLocked) return latest;
        if (!latestLocked && pendingWinLocked) return PendingWin;

        if (latestCandidates > 0 && pendingWinCandidates > 0)
        {
            if (latestCandidates < pendingWinCandidates) return latest;
            if (pendingWinCandidates < latestCandidates) return PendingWin;
        }

        return Side.None; // a genuine dead heat
    }

    void MarkActed(Side side)
    {
        if (side == Side.Host) actedHost = true;
        else actedGuest = true;
    }

    bool HasActed(Side side)
    {
        return side == Side.Host ? actedHost : actedGuest;
    }

    // Returns true when the side owed a forfeited turn, clearing the debt.
    bool ConsumeForfeit(Side side)
    {
        if (side == Side.Host && skipHost) { skipHost = false; return true; }
        if (side == Side.Guest && skipGuest) { skipGuest = false; return true; }
        return false;
    }

    // Hands the turn to whoever is owed one. A round holds one slot per side;
    // when both slots are spent the round closes and any provisional win
    // becomes final. A side carrying a forfeit silently burns its slot.
    void Advance()
    {
        for (int guard = 0; guard < 8; guard++)
        {
            if (actedHost && actedGuest)
            {
                if (pendingResolved)
                {
                    Finish();
                    return;
                }
                actedHost = actedGuest = false;
                RoundIndex++;
            }

            Side next = HasActed(Opener) ? Other(Opener) : Opener;
            if (ConsumeForfeit(next))
            {
                MarkActed(next);
                continue;
            }

            Turn = next;
            return;
        }

        Turn = Opener;
    }

    void Finish()
    {
        Finished = true;
        Turn = Side.None;

        if (PendingWin == Side.Host) Result = Outcome.HostWins;
        else if (PendingWin == Side.Guest) Result = Outcome.GuestWins;
        else Result = Outcome.Draw;
    }

    // ------------------------------------------------------------ lock policy
    //
    // Shared by the solo AI and by the hint the UI shows a human ("worth
    // locking?"). Simulation across 4000 matches per policy (see
    // tools/test/lock-policy-sim.mjs) puts staking the Lock only on a certain
    // guess at 50.3% against a player who never locks — and locking on every
    // guess at 18.2%, because each miss hands over a turn. Skill lives in the
    // gap between those two numbers.
    public enum LockStyle
    {
        Never = 0,      // never stakes it
        Reckless = 1,   // stakes it far too early
        Bold = 2,       // stakes it at three candidates left
        Precise = 3,    // stakes it only on a certain guess
    }

    public static bool ShouldLock(LockStyle style, int candidatesRemaining, bool lockAvailable)
    {
        if (!lockAvailable) return false;

        switch (style)
        {
            case LockStyle.Precise: return candidatesRemaining <= 1;
            case LockStyle.Bold: return candidatesRemaining <= 3;
            case LockStyle.Reckless: return candidatesRemaining <= 8;
            default: return false;
        }
    }

    // Candidate count at or below which the UI should offer the Lock.
    //
    // This threshold is doing real work. Two players who never touch the Lock
    // draw 27% of their duels, because binary search makes both of them need
    // the same number of guesses that often. Staking the Lock at three
    // candidates costs almost nothing in win rate but drops the draw rate to
    // roughly 3%, so the game is better off teaching the move at the moment it
    // matters than leaving players to discover it after a stack of stalemates.
    public const int SuggestLockAtOrBelow = 3;

    public static bool ShouldSuggestLock(int candidatesRemaining, bool lockAvailable)
    {
        return lockAvailable && candidatesRemaining > 0 && candidatesRemaining <= SuggestLockAtOrBelow;
    }
}
