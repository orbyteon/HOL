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
        public string winner = "";

        // Server-computed public view. PlayFab never sends live secrets.
        public string lastHint = ""; // "higher" | "lower" | "correct"
        public int revealedSecret;    // opponent secret, only after phase == "done"
        public int hostGuessCount;
        public int guestGuessCount;
    }

    public string RoomCode { get; protected set; } = "";
    public bool IsHost { get; protected set; }

    public Action OnRoomClosed;
    public Action OnConnectionLost;

    public abstract void CreateRoom(string hostName, int hostSecret, Action<bool, string> done);
    public abstract void JoinRoom(string code, string guestName, int guestSecret, Action<bool, string> done);
    public abstract void SubmitGuess(int guess, RoomState current, Action<bool> done);
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
