using UnityEngine;

// Current live UI state colors for values that cannot be baked into approved
// artwork (status copy, numbers, player/opponent state, accessibility text).
// This is not a theme selector and must never recolor or replace production art.
public static class HolUiStateColors
{
    public static readonly Color Background0 = Hex(0x07, 0x09, 0x1C);
    public static readonly Color Surface = Hex(0x17, 0x1B, 0x46);
    public static readonly Color SurfaceElevated = Hex(0x20, 0x26, 0x5A);
    public static readonly Color Cyan = Hex(0x40, 0xD9, 0xFF);
    public static readonly Color Blue = Hex(0x2A, 0x8D, 0xFF);
    public static readonly Color Magenta = Hex(0xE8, 0x47, 0x91);
    public static readonly Color Gold = Hex(0xFF, 0xCA, 0x55);
    public static readonly Color Success = Hex(0x8A, 0xD6, 0x4F);
    public static readonly Color TextPrimary = Hex(0xF5, 0xF4, 0xFF);
    public static readonly Color TextSecondary = Hex(0xC9, 0xC2, 0xE4);

    public static readonly Color CardBlue = new Color(0.08f, 0.28f, 0.68f, 0.96f);
    public static readonly Color CardPink = new Color(0.72f, 0.08f, 0.34f, 0.96f);
    public static readonly Color KeyBlue = new Color(0.16f, 0.18f, 0.62f, 1f);

    public static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    static Color Hex(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }
}
