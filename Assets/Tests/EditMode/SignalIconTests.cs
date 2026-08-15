using System;
using NUnit.Framework;
using UnityEngine;

// The drawn signal icons are wired without scene surgery: the SVGs live in a
// Resources folder and PvpRuntimeUI loads them by the same key Signals uses
// for localization. That only works if the Vector Graphics importer actually
// yields a UGUI-usable Sprite for each — which no stub compile can prove, so
// this test holds the load path green in real Unity. If an icon ever fails to
// import, the buttons degrade to text (the load is null-checked), but CI goes
// red here instead of the regression shipping silently.
//
// Reflection keeps the editor-only test assembly decoupled from
// Assembly-CSharp, matching DuelRulesTests and L10nIntegrityTests.
public class SignalIconTests
{
    static string[] SignalKeys()
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var signals = asm.GetType("Signals");
            if (signals == null) continue;

            var table = signals.GetField("Table");
            Assert.IsNotNull(table, "Signals.Table not found — renamed?");
            return (string[])table.GetValue(null);
        }

        Assert.Fail("Type 'Signals' not found in loaded assemblies — renamed?");
        return null;
    }

    [Test]
    public void EverySignalHasALoadableIconSprite()
    {
        var keys = SignalKeys();
        Assert.IsNotEmpty(keys, "Signals.Table is empty — the signal vocabulary vanished?");

        foreach (var key in keys)
        {
            var sprite = Resources.Load<Sprite>("design/" + key);
            Assert.IsNotNull(sprite,
                "Resources/design/" + key + " did not import as a Sprite — " +
                "check the Vector Graphics importer settings for that SVG");
        }
    }
}
