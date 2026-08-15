using System;
using System.Reflection;
using NUnit.Framework;

// The rounded-rectangle maths the PvP frames are drawn from.
//
// Converging Light generates every surface from code, so a frame that looks
// wrong is a maths bug rather than a bad export — and it only shows on a device,
// at which point a wrong corner or a border that antialiased itself out of
// existence is expensive to chase. These pin the properties that decide whether
// a frame reads as drawn or as a mistake.
//
// Reflection keeps the editor-only test assembly decoupled from
// Assembly-CSharp, matching DuelRulesTests and L10nIntegrityTests.
public class FrameGeometryTests
{
    static Type FindGameType(string name)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(name);
            if (t != null) return t;
        }
        Assert.Fail("Type '" + name + "' not found in loaded assemblies — renamed?");
        return null;
    }

    static Type Geometry => FindGameType("FrameGeometry");

    static float Distance(float x, float y, float halfW, float halfH, float radius) =>
        (float)Geometry.GetMethod("Distance").Invoke(null, new object[] { x, y, halfW, halfH, radius });

    static float OutlineAlpha(float distance, float thickness, float feather) =>
        (float)Geometry.GetMethod("OutlineAlpha").Invoke(null, new object[] { distance, thickness, feather });

    static float FillAlpha(float distance, float feather) =>
        (float)Geometry.GetMethod("FillAlpha").Invoke(null, new object[] { distance, feather });

    static float GlowAlpha(float distance, float radius) =>
        (float)Geometry.GetMethod("GlowAlpha").Invoke(null, new object[] { distance, radius });

    // ------------------------------------------------------------- the shape

    [Test]
    public void EdgeMidpointsSitExactlyOnTheBoundary()
    {
        Assert.AreEqual(0f, Distance(100f, 0f, 100f, 60f, 20f), 0.01f);
        Assert.AreEqual(0f, Distance(0f, 60f, 100f, 60f, 20f), 0.01f);
    }

    [Test]
    public void CornersAreArcsRatherThanChamfers()
    {
        // A point one radius from the corner arc's centre, along the diagonal,
        // must land on the boundary. A chamfer would not put it there.
        const float halfW = 100f, halfH = 60f, radius = 20f;
        float k = radius / (float)Math.Sqrt(2.0);

        Assert.AreEqual(0f, Distance(halfW - radius + k, halfH - radius + k, halfW, halfH, radius), 0.02f);
    }

    [Test]
    public void TheSquareCornerFallsOutsideTheRoundedShape()
    {
        // radius * (sqrt(2) - 1) ≈ 8.28 — if this ever reaches zero the corner
        // has stopped being rounded at all.
        Assert.Greater(Distance(100f, 60f, 100f, 60f, 20f), 8f);
    }

    [Test]
    public void RadiusClampsToTheShorterSide()
    {
        // An over-large radius must degrade to a capsule, not invert the field.
        Assert.AreEqual(-50f, Distance(0f, 0f, 50f, 50f, 999f), 0.01f);
    }

    // ----------------------------------------------------------- the outline

    [Test]
    public void AThreePixelBorderSurvivesAntialiasing()
    {
        // The frames are 3px on a 1080-wide reference canvas. If feathering
        // ever eats that, every panel loses its edge and nobody can say why.
        Assert.Greater(OutlineAlpha(0f, 3f, 1f), 0.99f);
    }

    [Test]
    public void TheOutlineStraddlesTheEdgeSymmetrically()
    {
        Assert.AreEqual(OutlineAlpha(-2.5f, 4f, 1f), OutlineAlpha(2.5f, 4f, 1f), 0.0001f);
        Assert.AreEqual(0.5f, OutlineAlpha(2f, 4f, 1f), 0.001f);
    }

    [Test]
    public void TheOutlineIsHollow()
    {
        Assert.AreEqual(0f, OutlineAlpha(-20f, 4f, 1f), 0.0001f);
        Assert.AreEqual(0f, OutlineAlpha(20f, 4f, 1f), 0.0001f);
    }

    [Test]
    public void ZeroThicknessDrawsNothing()
    {
        Assert.AreEqual(0f, OutlineAlpha(0f, 0f, 1f), 0.0001f);
    }

    // ------------------------------------------------------- fill and glow

    [Test]
    public void FillAndOutlineMeetOnTheSameEdge()
    {
        // Both are derived from one distance field, so the fill is half-covered
        // exactly where the outline is centred. Any other value leaves a seam.
        Assert.AreEqual(0.5f, FillAlpha(0f, 1f), 0.001f);
        Assert.AreEqual(1f, FillAlpha(-10f, 1f), 0.0001f);
        Assert.AreEqual(0f, FillAlpha(10f, 1f), 0.0001f);
    }

    [Test]
    public void GlowIsLightLeavingTheFrameNotASecondBorder()
    {
        Assert.AreEqual(0f, GlowAlpha(-5f, 20f), 0.0001f, "glow must not bleed inside the plate");
        Assert.AreEqual(0f, GlowAlpha(25f, 20f), 0.0001f, "glow must end at its radius");
        Assert.Greater(GlowAlpha(0.01f, 20f), 0.99f);
    }

    [Test]
    public void GlowDecaysMonotonically()
    {
        Assert.Greater(GlowAlpha(5f, 20f), GlowAlpha(10f, 20f));
        Assert.Greater(GlowAlpha(10f, 20f), GlowAlpha(15f, 20f));
    }
}
