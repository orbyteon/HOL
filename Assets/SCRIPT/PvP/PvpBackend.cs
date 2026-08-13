using System;
using UnityEngine;

// Abstract transport for HOL's PvP rooms. Two implementations ship:
//   - PvpClient          (Firebase Realtime Database, REST; dev fallback)
//   - PlayFabPvpClient   (PlayFab REST + server-authoritative CloudScript)
public abstract class PvpBackend : MonoBehaviour
{
    [Serializable]
    public class RoomState
    {
        public string hostName = "";
        public string guestName = "";

        // Firebase fallback still carries secrets in its room document.
        // PlayFab's sanitized room view leaves these at 0 and instead returns
        // lastHint/revealedSecret from server authority.
        public int hostSecret;
        public int guestSecret;

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

    // The Lock and Signals are only offered on a backend that adjudicates them.
    // Firebase's room document is client-writable, so a client could forge a
    // Lock it never staked or a result it never earned; the development
    // fallback therefore plays the plain round rules and hides both controls.
    public virtual bool IsServerAuthoritative { get { return false; } }

    public Action OnRoomClosed;
    public Action OnConnectionLost;

    public abstract void CreateRoom(string hostName, int hostSecret, Action<bool, string> done);
    public abstract void JoinRoom(string code, string guestName, int guestSecret, Action<bool, string> done);

    // useLock stakes this side's single Lock on the guess: it wins a same-round
    // tie, and a miss forfeits the next turn.
    public abstract void SubmitGuess(int guess, bool useLock, RoomState current, Action<bool> done);

    // Sends one entry from the fixed Signals table. Backends that cannot carry
    // signals simply report failure; the UI then leaves the control alone.
    public virtual void SendSignal(int signalId, Action<bool> done) { done?.Invoke(false); }
    public abstract void StartPolling(Action<RoomState> onState);
    public abstract void StopPolling();
    public abstract void DeleteRoom();

    // PlayFab uses this to delete completed room data after both clients have
    // observed the result. Firebase has no separate acknowledgement flow.
    public virtual void AcknowledgeResult() { }

    protected const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    protected string GenerateCode()
    {
        var sb = new System.Text.StringBuilder(5);
        for (int i = 0; i < 5; i++)
            sb.Append(CodeAlphabet[UnityEngine.Random.Range(0, CodeAlphabet.Length)]);
        return sb.ToString();
    }
}
