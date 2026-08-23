using UnityEngine;

// HOL Cartoon Theme runtime tokens.
//
// Canonical machine-readable source:
// Assets/newdesign/design-tokens.json
//
// The theme is a polished 2.5D cartoon mobile-arcade identity. Converging
// Light remains an atmospheric legacy subsystem only; it must not replace
// approved cartoon production artwork or the hierarchy defined by these
// tokens.
public static class ConsumerTokens
{
    public static readonly Color Outline = Hex(0x14, 0x0A, 0x22);
    public static readonly Color Background0 = Hex(0x12, 0x07, 0x1F);
    public static readonly Color Background1 = Hex(0x24, 0x16, 0x45);
    public static readonly Color Surface = Hex(0x2C, 0x19, 0x52);
    public static readonly Color SurfaceElevated = Hex(0x3A, 0x20, 0x68);
    public static readonly Color Cyan = Hex(0x40, 0xD9, 0xFF);
    public static readonly Color Blue = Hex(0x2A, 0x8D, 0xFF);
    public static readonly Color Magenta = Hex(0xE8, 0x47, 0x91);
    public static readonly Color Violet = Hex(0x8B, 0x6C, 0xFF);
    public static readonly Color Gold = Hex(0xFF, 0xCA, 0x55);
    public static readonly Color Success = Hex(0x8A, 0xD6, 0x4F);
    public static readonly Color TextPrimary = Hex(0xF5, 0xF0, 0xFF);
    public static readonly Color TextSecondary = Hex(0xC9, 0xBF, 0xE1);
    public static readonly Color TextMuted = Hex(0x8E, 0x82, 0xAE);
    public static readonly Color Ink = Hex(0x17, 0x10, 0x2B);

    // Existing duel-side semantic aliases. Keep these stable while screen
    // owners migrate to the canonical theme palette.
    public static readonly Color CardBlue = new Color(0.08f, 0.28f, 0.68f, 0.96f);
    public static readonly Color CardPink = new Color(0.72f, 0.08f, 0.34f, 0.96f);
    public static readonly Color KeyBlue = new Color(0.16f, 0.18f, 0.62f, 1f);

    public const float ReferenceWidth = 1080f;
    public const float ReferenceHeight = 1920f;
    public const float MinimumTouchTarget = 48f;

    static Color Hex(int r, int g, int b) =>
        new Color(r / 255f, g / 255f, b / 255f, 1f);
}
