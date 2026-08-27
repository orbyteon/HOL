using NUnit.Framework;

// Direct compile-time contracts for the Unity-free fixed Signal vocabulary.
public class PvpSignalProtocolTests
{
    static readonly string[] ExpectedKeys =
    {
        "signal_luck",
        "signal_close",
        "signal_ouch",
        "signal_nice",
        "signal_your_turn",
        "signal_gg",
    };

    [Test]
    public void OrderedVocabularyIsAppendOnlyProtocol()
    {
        CollectionAssert.AreEqual(ExpectedKeys, PvpSignalProtocol.Keys);
        Assert.AreEqual(ExpectedKeys.Length, PvpSignalProtocol.Count);
    }

    [Test]
    public void IdValidationAndLookupFailClosed()
    {
        Assert.IsFalse(PvpSignalProtocol.IsValid(-1));
        Assert.IsFalse(PvpSignalProtocol.IsValid(PvpSignalProtocol.Count));
        Assert.AreEqual("", PvpSignalProtocol.Key(-1));
        Assert.AreEqual("", PvpSignalProtocol.Key(PvpSignalProtocol.Count));

        for (int id = 0; id < ExpectedKeys.Length; id++)
        {
            Assert.IsTrue(PvpSignalProtocol.IsValid(id));
            Assert.AreEqual(ExpectedKeys[id], PvpSignalProtocol.Key(id));
        }
    }

    [Test]
    public void PerSideCapRemainsTheServerContract()
    {
        Assert.AreEqual(12, PvpSignalProtocol.CapPerSide);
    }
}
