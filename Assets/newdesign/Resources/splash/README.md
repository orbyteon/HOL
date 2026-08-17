# Splash production art

These assets form the authoritative standalone Splash art contract.

## Resources

- `Resources/reference/mascot_6_exact`: approved 1024×1024 transparent
  mascot-six Sprite.
- `Resources/splash/splash_bg_neon_arcade`: dedicated 1080×1920
  portrait background Sprite.
- `Resources/splash/splash_logo_glow`: deterministic 960×620 transparent
  magenta/cyan logo glow Sprite.

The background and glow contain no baked text, logos, buttons, characters,
or other UI.

## Background prompt

```text
Full-screen portrait mobile game splash BACKGROUND ONLY, no logo, no text,
no letters, no numbers, no characters, no mascots, no buttons, no UI.
Premium glossy neon arcade cartoon style. Deep indigo night center with large
clean negative space for a logo. Controlled magenta energy and lightning only
along the far left edge, cyan energy and lightning only along the far right
edge. Sparse cyan, magenta and muted-gold stars and confetti near the edges.
Subtle perspective arcade grid and soft blue-violet horizon at the bottom.
Rich depth, polished highlights, clean shapes, no visual noise in the central
60 percent. Symmetric visual balance but organic details.
```

The accepted generated source was center-cropped and resized with Lanczos
filtering to the native Android portrait dimensions.

## Mascot-six provenance

The PNG payload was copied byte-for-byte from commit `9f3f701`, path
`Assets/newdesign/Resources/avatars/numbers/avatar_number_06.png`.
Its SHA-256 is
`067beafc207aea302e0993a3bacdb2b69478429aa3685f275bb6705bd902ac4b`.
The asset at this new path has a new Unity GUID.
