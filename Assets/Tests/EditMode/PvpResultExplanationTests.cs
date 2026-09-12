using NUnit.Framework;
using UnityEngine;

public sealed class PvpResultExplanationTests
{
    [TestCase("host", "only_correct", 30, 0, "Marinos")]
    [TestCase("guest", "only_correct", 0, 49, "Ανδρέας")]
    [TestCase("host", "lock", 100, 100, "Marinos")]
    [TestCase("guest", "lock", 100, 100, "Ανδρέας")]
    [TestCase("host", "range", 30, 49, "Marinos")]
    [TestCase("guest", "range", 49, 30, "Ανδρέας")]
    [TestCase("draw", "draw", 49, 49, "")]
    public void FinalReasonsRoundTripWithoutSeatGuessing(string winner, string reason, int host, int guest, string name)
    {
        var state = new PvpRoomState { phase = "done", winner = winner, resultReason = reason,
            hostName = "Marinos", guestName = "Ανδρέας", resultHostCandidates = host, resultGuestCandidates = guest };
        var read = JsonUtility.FromJson<PvpRoomState>(JsonUtility.ToJson(state));
        var model = PvpResultExplanation.Capture(read);
        Assert.That(model.Final, Is.True);
        Assert.That(model.Reason, Is.EqualTo(reason));
        Assert.That(model.WinnerName, Is.EqualTo(name));
        if (reason == "range") Assert.That(model.WinnerCandidates, Is.EqualTo(30));
        // The displayed explanation is a value snapshot, not a mutable room reference.
        read.phase = "play"; read.hostName = "Changed"; read.resultReason = "";
        Assert.That(model.Reason, Is.EqualTo(reason));
        Assert.That(model.WinnerName, Is.EqualTo(name));
        Assert.That(PvpResultExplanation.Capture(read).Final, Is.False);
    }

    [TestCase("waiting")][TestCase("play")][TestCase("closed")]
    public void LiveSnapshotsNeverExplainPendingResults(string phase)
    {
        var state = new PvpRoomState { phase = phase, winner = "host", resultReason = "lock",
            resultHostCandidates = 100, resultGuestCandidates = 100, hostName = "Marinos" };
        var model = PvpResultExplanation.Capture(state);
        Assert.That(model.Final, Is.False);
        Assert.That(model.Reason, Is.Empty);
    }

    [TestCase(0, 10)][TestCase(101, 100)][TestCase(50, 30)][TestCase(30, 30)]
    public void InvalidOrContradictoryRangeFactsUseGenericFinalFallback(int host, int guest)
    {
        var state = new PvpRoomState { phase = "done", winner = "host", resultReason = "range",
            resultHostCandidates = host, resultGuestCandidates = guest, hostName = "Marinos" };
        var model = PvpResultExplanation.Capture(state);
        Assert.That(model.Final, Is.True);
        Assert.That(model.Reason, Is.Empty);
    }

    [Test]
    public void LegacyCountsCannotManufactureAReasonAndOnlyConsumedLosingSlotIsNamed()
    {
        var legacy = new PvpRoomState { phase = "done", winner = "host", hostGuessCount = 3,
            guestGuessCount = 9, hostName = "Marinos", guestName = "Ανδρέας" };
        Assert.That(PvpResultExplanation.Capture(legacy).Reason, Is.Empty);
        legacy.resultReason = "only_correct"; legacy.resultHostCandidates = 30;
        legacy.resultForfeitedSide = "guest";
        Assert.That(PvpResultExplanation.Capture(legacy).ForfeitedName, Is.EqualTo("Ανδρέας"));
        legacy.resultForfeitedSide = "host";
        Assert.That(PvpResultExplanation.Capture(legacy).ForfeitedName, Is.Empty);
        legacy.resultReason = "future_unknown_reason";
        Assert.That(PvpResultExplanation.Capture(legacy).Reason, Is.Empty);
    }
}
