using NUnit.Framework;
using UnityEngine;

// The four Consumer First surfaces (menu background, panel, both buttons)
// reach the live UI two ways: scene references on DesignRuntimeWiring, and a
// Resources.Load fallback for when a scene reference resolves to null. vc5
// shipped with the old menu art because the scene references pointed at
// invalid GUIDs and nothing failed anywhere — a stale reference is silent by
// design in Unity. The fallback path is the one this test can hold green:
// if any surface stops importing as a UGUI-usable Sprite, CI goes red here
// instead of a release candidate shipping flat again.
//
// No reflection needed: unlike the Signals table, the surface names are not
// game types — they are fixed asset paths shared with DesignRuntimeWiring.
public class DesignSurfaceTests
{
    static readonly string[] Surfaces =
    {
        "background_deep",
        "panel_surface",
        "button_primary",
        "button_secondary",
    };

    [Test]
    public void EveryDesignSurfaceHasALoadableSprite()
    {
        foreach (var name in Surfaces)
        {
            var sprite = Resources.Load<Sprite>("design/" + name);
            Assert.IsNotNull(sprite,
                "Resources/design/" + name + " did not import as a Sprite — " +
                "the menu and every runtime-built screen would fall back to " +
                "flat colors. Check the Vector Graphics importer settings.");
        }
    }
}
