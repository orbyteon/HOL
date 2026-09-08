#if UNITY_EDITOR || (UNITY_STANDALONE_WIN && DEVELOPMENT_BUILD)
using System;

// Explicitly attached by presentation tests/captures only. No initializer,
// scene reference, network request, rules engine or production Android code.
// Every room snapshot is supplied by the test; this is not a two-client test.
public sealed class PvpPresentationFixtureBackend : PvpBackend
{
    public int CreateCalls, JoinCalls, GuessCalls, RematchCalls, LeaveCalls;
    public int LastSecret, LastGuess;
    public bool LastLock;
    public bool HoldRequests;
    public bool HoldGuesses;
    public Action<bool, string> PendingRoomRequest;
    public Action<bool> PendingGuess;
    Action<RoomState> observer;
    public override bool IsServerAuthoritative => true;
    public void Configure(bool host) { IsHost = host; }
    public void Emit(RoomState state) { observer?.Invoke(state); }
    public override void CreateRoom(string name, int secret, Action<bool, string> done)
    {
        CreateCalls++; LastSecret = secret; IsHost = true;
        if (HoldRequests) { PendingRoomRequest = done; return; }
        RoomCode = "MTW8H";
        done(true, RoomCode);
    }
    public override void JoinRoom(string code, string name, int secret, Action<bool, string> done)
    {
        JoinCalls++; LastSecret = secret; IsHost = false;
        if (HoldRequests) { PendingRoomRequest = done; return; }
        RoomCode = code;
        done(true, "");
    }
    public override void SubmitGuess(int guess, bool locked, RoomState state, Action<bool> done)
    {
        GuessCalls++; LastGuess = guess; LastLock = locked;
        // A test publishes the next authoritative snapshot separately.
        if (HoldGuesses) { PendingGuess = done; return; }
        done(true);
    }
    public override void RequestRematch(int secret, Action<bool> done)
    {
        RematchCalls++; LastSecret = secret; done(true);
    }
    public override void SendSignal(int id, int matchIndex, Action<bool> done) { done(true); }
    public override void StartPolling(Action<RoomState> onState) { observer = onState; }
    public override void StopPolling() { observer = null; }
    public override void DeleteRoom() { LeaveCalls++; RoomCode = ""; observer = null; }
}
#endif
