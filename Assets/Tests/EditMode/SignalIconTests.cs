using NUnit.Framework;
using UnityEngine;

// The drawn signal icons are wired without scene surgery: the SVGs live in a
// Resources folder and use the Unity-free protocol's localization keys. This
// test holds the Vector Graphics import path green in real Unity while the
// protocol itself is tested directly without reflection.
public class SignalIconTests
{
    [Test]
    public void EverySignalHasALoadableIconSprite()
    {
        Assert.IsNotEmpty(PvpSignalProtocol.Keys,
            "The fixed Signal vocabulary vanished.");

        foreach (string key in PvpSignalProtocol.Keys)
        {
            var sprite = Resources.Load<Sprite>("design/" + key);
            Assert.IsNotNull(sprite,
                "Resources/design/" + key + " did not import as a Sprite — " +
                "check the Vector Graphics importer settings for that SVG");
        }
    }
}
