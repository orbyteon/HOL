// Rounded-rectangle outline maths for the PvP frames.
//
// Converging Light generates everything from code — no art assets, no scene
// surgery — so the neon frame the PvP screens are built from has to be drawn
// rather than imported. This is the part of that drawing worth testing: the
// coverage of a single pixel, which decides whether a corner reads as a curve
// or a staircase and whether a 3px border survives on a 1080-wide canvas.
//
// Deliberately free of UnityEngine so it can be verified without the editor,
// the same reason DuelRules is. NeonFrame turns these numbers into a texture.
public static class FrameGeometry
{
    // Signed distance from a point to a rounded rectangle centred on the
    // origin: negative inside, zero on the edge, positive outside. Half-extents
    // are measured to the outer edge, so the straight sections run from
    // -(half - radius) to +(half - radius) and the corners are arcs of `radius`.
    public static float Distance(float x, float y, float halfW, float halfH, float radius)
    {
        if (radius < 0f) radius = 0f;
        float maxRadius = halfW < halfH ? halfW : halfH;
        if (radius > maxRadius) radius = maxRadius;

        // Fold into the first quadrant; the shape is symmetric in both axes.
        float dx = Abs(x) - (halfW - radius);
        float dy = Abs(y) - (halfH - radius);

        float outsideX = dx > 0f ? dx : 0f;
        float outsideY = dy > 0f ? dy : 0f;
        float outside = Sqrt(outsideX * outsideX + outsideY * outsideY);

        // Inside the corner-centre cross, the nearest edge is the closer axis.
        float insideMax = dx > dy ? dx : dy;
        float inside = insideMax < 0f ? insideMax : 0f;

        return outside + inside - radius;
    }

    // Coverage of a pixel by an outline of `thickness` straddling the edge,
    // feathered over `feather` pixels so corners antialias instead of jagging.
    // Returns 0 (clear) to 1 (solid).
    public static float OutlineAlpha(float distance, float thickness, float feather)
    {
        if (thickness <= 0f) return 0f;
        if (feather < 0f) feather = 0f;

        float half = thickness * 0.5f;
        float d = Abs(distance);

        if (feather <= 0f) return d <= half ? 1f : 0f;
        return 1f - Smoothstep(half - feather, half + feather, d);
    }

    // Coverage of the filled interior, feathered on the same edge so a fill and
    // an outline drawn from the same distance field meet without a seam.
    public static float FillAlpha(float distance, float feather)
    {
        if (feather <= 0f) return distance <= 0f ? 1f : 0f;
        return 1f - Smoothstep(-feather, feather, distance);
    }

    // A glow that falls off outside the edge and is absent within the shape,
    // so it reads as light escaping the frame rather than a second border.
    public static float GlowAlpha(float distance, float radius)
    {
        if (radius <= 0f || distance <= 0f) return 0f;
        if (distance >= radius) return 0f;

        float t = 1f - distance / radius;
        return t * t; // quadratic falloff: brighter at the edge, quick to fade
    }

    // ------------------------------------------------------------- primitives
    // Hand-rolled so the file stays free of UnityEngine and System.Math alike,
    // and so the exact curve of the feather is visible here rather than
    // inherited from whichever Mathf the caller happens to have.

    static float Abs(float v) => v < 0f ? -v : v;

    static float Smoothstep(float edge0, float edge1, float x)
    {
        if (edge1 <= edge0) return x < edge0 ? 0f : 1f;

        float t = (x - edge0) / (edge1 - edge0);
        if (t < 0f) t = 0f;
        else if (t > 1f) t = 1f;

        return t * t * (3f - 2f * t);
    }

    static float Sqrt(float v)
    {
        if (v <= 0f) return 0f;

        // Newton-Raphson from a decent seed; four passes is well inside float
        // precision for the pixel-scale magnitudes this ever sees.
        float guess = v > 1f ? v * 0.5f : 1f;
        for (int i = 0; i < 8; i++)
            guess = 0.5f * (guess + v / guess);
        return guess;
    }
}
