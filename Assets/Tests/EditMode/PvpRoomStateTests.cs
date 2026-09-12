using NUnit.Framework;

// Compile-time behavior contracts for the Unity-free PvP public room view.
public class PvpRoomStateTests
{
    // Mirrors the temporary PvpBackend.RoomState compatibility shim without
    // introducing an EditMode-test dependency on Assembly-CSharp. JsonUtility
    // must preserve inherited public fields because the shipping PlayFab client
    // still deserializes through that derived runtime type.
    [System.Serializable]
    sealed class DerivedRoomState : PvpRoomState { }

    [TestCase(true, "Host", "0")]
    [TestCase(false, "Guest", "6")]
    public void IdentityHelpersUseTheRequestedSeat(bool host, string name, string avatar)
    {
        var state = new PvpRoomState { hostName = "Host", guestName = "Guest",
            hostAvatarId = "0", guestAvatarId = "6" };
        Assert.AreEqual(name, state.NameFor(host));
        Assert.AreEqual(avatar, state.AvatarIdFor(host));
    }

    [TestCase("{}", "", "")]
    [TestCase("{\"hostAvatarId\":null,\"guestAvatarId\":\"\"}", "", "")]
    [TestCase("{\"hostAvatarId\":\"0\",\"guestAvatarId\":\"6\"}", "0", "6")]
    [TestCase("{\"hostAvatarId\":\"1\"}", "1", "")]
    public void InheritedAvatarWireFieldsPreserveZeroAndMissingSeparately(string json, string host, string guest)
    {
        var state = UnityEngine.JsonUtility.FromJson<DerivedRoomState>(json);
        Assert.AreEqual(host, state.AvatarIdFor(true));
        Assert.AreEqual(guest, state.AvatarIdFor(false));
        var roundTrip = UnityEngine.JsonUtility.FromJson<DerivedRoomState>(UnityEngine.JsonUtility.ToJson(state));
        Assert.AreEqual(host, roundTrip.AvatarIdFor(true));
        Assert.AreEqual(guest, roundTrip.AvatarIdFor(false));
    }

    [Test]
    public void SideHelpersReadTheMatchingPublicFields()
    {
        var state = new PvpRoomState
        {
            hostLockUsed = true,
            guestLockUsed = false,
            hostSkipNext = false,
            guestSkipNext = true,
            hostGuessCount = 4,
            guestGuessCount = 7,
        };

        Assert.IsTrue(state.LockUsedBy("host"));
        Assert.IsFalse(state.LockUsedBy("guest"));
        Assert.IsFalse(state.ForfeitPendingFor("host"));
        Assert.IsTrue(state.ForfeitPendingFor("guest"));
        Assert.AreEqual(4, state.GuessCountFor("host"));
        Assert.AreEqual(7, state.GuessCountFor("guest"));
    }

    [Test]
    public void MatchPointRequiresALivePlayPhaseAndTheOtherPendingWinner()
    {
        var state = new PvpRoomState
        {
            phase = "play",
            pendingWin = "host",
        };

        Assert.IsFalse(state.IsMatchPointAgainst("host"));
        Assert.IsTrue(state.IsMatchPointAgainst("guest"));

        state.phase = "done";
        Assert.IsFalse(state.IsMatchPointAgainst("guest"));

        state.phase = "play";
        state.pendingWin = "";
        Assert.IsFalse(state.IsMatchPointAgainst("guest"));
    }

    [Test]
    public void WireFieldsAreDirectlyWritableWithoutUnityTypes()
    {
        var state = new PvpRoomState
        {
            hostName = "Host",
            guestName = "Guest",
            turn = "guest",
            phase = "play",
            lastGuess = 51,
            lastHint = "lower",
            matchIndex = 3,
            signalId = 5,
            signalSeq = 9,
        };

        Assert.AreEqual("Host", state.hostName);
        Assert.AreEqual("Guest", state.guestName);
        Assert.AreEqual("guest", state.turn);
        Assert.AreEqual("play", state.phase);
        Assert.AreEqual(51, state.lastGuess);
        Assert.AreEqual("lower", state.lastHint);
        Assert.AreEqual(3, state.matchIndex);
        Assert.AreEqual(5, state.signalId);
        Assert.AreEqual(9, state.signalSeq);
    }

    [Test]
    public void JsonUtilityRoundTripPreservesInheritedWireFieldsUsedByRuntimeShim()
    {
        var expected = new DerivedRoomState
        {
            hostName = "Host",
            guestName = "Guest",
            phase = "done",
            winner = "draw",
            revealedSecret = 42,
            hostGuessCount = 6,
            guestGuessCount = 6,
            matchIndex = 4,
            opponentLeft = true,
        };

        string json = UnityEngine.JsonUtility.ToJson(expected);
        var actual = UnityEngine.JsonUtility.FromJson<DerivedRoomState>(json);

        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.hostName, actual.hostName);
        Assert.AreEqual(expected.guestName, actual.guestName);
        Assert.AreEqual(expected.phase, actual.phase);
        Assert.AreEqual(expected.winner, actual.winner);
        Assert.AreEqual(expected.revealedSecret, actual.revealedSecret);
        Assert.AreEqual(expected.hostGuessCount, actual.hostGuessCount);
        Assert.AreEqual(expected.guestGuessCount, actual.guestGuessCount);
        Assert.AreEqual(expected.matchIndex, actual.matchIndex);
        Assert.AreEqual(expected.opponentLeft, actual.opponentLeft);
    }
}
