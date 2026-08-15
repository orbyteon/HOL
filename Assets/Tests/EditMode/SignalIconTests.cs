using NUnit.Framework;
using UnityEngine;

// The drawn signal icons are wired without scene surgery: the SVGs live in a
// Resources folder and PvpRuntimeUI loads them by the same key Signals uses
// for localization. That only works if the Vector Graphics importer actually
// yields a UGUI-usable Sprite for each — which no stub compile can prove, so
// this test holds the load path green in real Unity. If an icon ever fails to
// import, the buttons degrade to text (the load is null-checked), but CI goes
// red here instead of the regression shipping silently.
public class SignalIconTests
{
    [Test]
    public void EverySignalHasALoadableIconSprite()
    {
        for (int i = 0; i < Signals.Count; i++)
        {
            var sprite = Resources.Load<Sprite>("design/" + Signals.Key(i));
            Assert.IsNotNull(sprite,
                "Resources/design/" + Signals.Key(i) + " did not import as a Sprite — " +
                "check the Vector Graphics importer settings for that SVG");
        }
    }
}
