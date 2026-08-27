using System;

// Unity-free public view of a server-authoritative PvP room.
//
// Field names are the wire contract consumed by Unity JsonUtility. They must
// remain byte-for-byte aligned with the object emitted by
// playfab/cloudscript.js; tools/test/room-state-contract.test.mjs enforces that
// relationship across waiting, play, done and rematch states.
[Serializable]
public class PvpRoomState
{
    public string hostName = "";
    public string guestName = "";

    public string turn = "";    // "host" | "guest"
    public string phase = "";   // "waiting" | "play" | "done" | "closed"
    public int lastGuess;
    public string lastBy = "";
    public string winner = "";  // "host" | "guest" | "draw"

    // Server-computed public view. PlayFab never sends live secrets.
    public string lastHint = ""; // "higher" | "lower" | "correct"
    public int revealedSecret;    // opponent secret, only after phase == "done"
    public int hostGuessCount;
    public int guestGuessCount;

    // Duel rules (see DuelRules.cs and playfab/cloudscript.js). All of it is
    // symmetric information — both sides see identical values — so showing
    // it costs no fairness and lets the UI put the stakes on screen.
    public string opener = "";     // who moved first; fixed for the match
    public string pendingWin = ""; // provisional winner awaiting the answering guess
    public bool lastLocked;        // was the last guess staked on the Lock?
    public bool hostLockUsed;
    public bool guestLockUsed;
    public bool hostSkipNext;      // owes a forfeited turn after a missed Lock
    public bool guestSkipNext;
    public int roundIndex;

    // Signal ids are positional entries in the fixed quick-chat protocol.
    public string signalBy = "";   // "host" | "guest"
    public int signalId;
    public int signalSeq;           // bumped per signal; how clients spot a new one

    // Rematch handshake. A room outlives its match now, so friends can play
    // again without re-sharing an invite code. matchIndex changing is how a
    // client knows the next match actually started rather than being offered.
    public int matchIndex;
    public bool iWantRematch;
    public bool theyWantRematch;
    public bool opponentLeft;

    public bool LockUsedBy(string side)
    {
        return side == "host" ? hostLockUsed : guestLockUsed;
    }

    public bool ForfeitPendingFor(string side)
    {
        return side == "host" ? hostSkipNext : guestSkipNext;
    }

    public int GuessCountFor(string side)
    {
        return side == "host" ? hostGuessCount : guestGuessCount;
    }

    // True while this side is one guess away from losing: the opponent has
    // already found the number and this is the answering turn.
    public bool IsMatchPointAgainst(string side)
    {
        return phase == "play" &&
               !string.IsNullOrEmpty(pendingWin) &&
               pendingWin != side;
    }
}
