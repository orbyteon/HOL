using System;
using UnityEngine;

// Abstract transport for HOL's PvP rooms. Two implementations ship:
//   - PvpClient          (Firebase Realtime Database, REST)
//   - PlayFabPvpClient   (Microsoft Azure PlayFab, REST + CloudScript)
// PvpGameController talks only to this base class — pick the backend by
// dropping the matching component into its Inspector field.
public abstract class PvpBackend : MonoBehaviour
{
    [Serializable]
    public class RoomState
    {
        public string hostName = "";
        public string guestName = "";
        public int hostSecret;     // 0 = not set yet
        public int guestSecret;    // 0 = not set yet
        public string turn = "";    // "host" | "guest"
        public string phase = "";   // "waiting" | "play" | "done" | "closed"
        public int lastGuess;
        public string lastBy = "";
        public string winner = "";  // "host" | "guest" when phase == "done"
    }

    public string RoomCode { get; protected set; } = "";
    public bool IsHost { get; protected set; }

    // Fired by the poller when the room disappears or is marked "closed"
    // (the opponent left). Also surfaced as phase == "closed" via onState
    // on backends that can't delete rooms (PlayFab).
    public Action OnRoomClosed;

    // Fired by the poller after too many consecutive failed polls
    // (network down, backend unreachable). Polling stops at that point.
    public Action OnConnectionLost;

    public abstract void CreateRoom(string hostName, int hostSecret, Action<bool, string> done);
    public abstract void JoinRoom(string code, string guestName, int guestSecret, Action<bool, string> done);
    public abstract void SubmitGuess(int guess, RoomState current, Action<bool> done);
    public abstract void StartPolling(Action<RoomState> onState);
    public abstract void StopPolling();
    public abstract void DeleteRoom();

    protected const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // no 0/O/1/I/L

    protected string GenerateCode()
    {
        var sb = new System.Text.StringBuilder(5);
        for (int i = 0; i < 5; i++)
            sb.Append(CodeAlphabet[UnityEngine.Random.Range(0, CodeAlphabet.Length)]);
        return sb.ToString();
    }
}
