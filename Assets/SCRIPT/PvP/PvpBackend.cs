using System;
using UnityEngine;

// Abstract transport for HOL's server-authoritative PlayFab PvP rooms.
public abstract class PvpBackend : MonoBehaviour
{
    // Transitional source-compatibility shim. The wire fields and pure helper
    // behavior now live in HOL.Application/PvpRoomState. Existing Unity callers
    // keep their PvpBackend.RoomState type until a later typed-backend slice
    // updates the remaining signatures in one controlled change.
    [Serializable]
    public class RoomState : PvpRoomState { }

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
