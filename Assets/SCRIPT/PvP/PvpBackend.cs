using System;
using UnityEngine;

// Abstract transport for HOL's server-authoritative PlayFab PvP rooms.
public abstract class PvpBackend : MonoBehaviour
{
    [Serializable]
    public class RoomState
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

        public string signalBy = "";   // "host" | "guest"
        public int signalId;           // index into Signals.Table
        public int signalSeq;          // bumped per signal; how clients spot a new one

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
            return phase == "play" && !string.IsNullOrEmpty(pendingWin) && pendingWin != side;
        }
    }

    public string RoomCode { get; protected set; } = "";
    public bool IsHost { get; protected set; }

    // Optional transports must explicitly opt into controls that require
    // server adjudication. The shipping PlayFab implementation does.
    public virtual bool IsServerAuthoritative { get { return false; } }

    public Action OnRoomClosed;
    public Action OnConnectionLost;

    public abstract void CreateRoom(string hostName, int hostSecret, Action<bool, string> done);
    public abstract void JoinRoom(string code, string guestName, int guestSecret, Action<bool, string> done);

    // useLock stakes this side's single Lock on the guess: it wins a same-round
    // tie, and a miss forfeits the next turn.
    public abstract void SubmitGuess(int guess, bool useLock, RoomState current, Action<bool> done);

    // Sends one entry from the fixed Signals table.
    public virtual void SendSignal(int signalId, Action<bool> done) { done?.Invoke(false); }
    public virtual void SendSignal(int signalId, int matchIndex,
        Action<bool> done)
    {
        SendSignal(signalId, done);
    }

    // Commits a fresh secret for another match in the same room. Every current
    // caller must include the authoritative match index so a delayed result-
    // screen command cannot mutate a later match in the same long-lived room.
    public virtual void RequestRematch(int secret, Action<bool> done)
    {
        RequestRematch(secret, -1, done);
    }

    public virtual void RequestRematch(int secret, int matchIndex,
        Action<bool> done)
    {
        done?.Invoke(false);
    }

    public abstract void StartPolling(Action<RoomState> onState);
    public abstract void StopPolling();
    public abstract void DeleteRoom();

    // Deletes completed room data after both clients observed the result. The
    // match-scoped overload fences delayed acknowledgements after a rematch.
    public virtual void AcknowledgeResult()
    {
        AcknowledgeResult(-1);
    }

    public virtual void AcknowledgeResult(int matchIndex) { }

    protected const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    protected string GenerateCode()
    {
        var sb = new System.Text.StringBuilder(5);
        for (int i = 0; i < 5; i++)
            sb.Append(CodeAlphabet[UnityEngine.Random.Range(0, CodeAlphabet.Length)]);
        return sb.ToString();
    }
}
