# Splash production art

These assets form the authoritative standalone Splash art contract.

## Resources

- `Resources/reference/hol_logo_exact`: approved transparent HOL logo Sprite.
- `Resources/reference/mascot_6_exact`: approved 1024×1024 transparent
  mascot-six Sprite.
- `Resources/reference/mascot_7_exact`: approved transparent mascot-seven
  Sprite.
- `Resources/splash/splash_bg_stairs_clouds`: dedicated opaque 1080×1920
  cartoon night-sky, clouds, and magical-stairs background Sprite.
- `Resources/splash/splash_logo_glow`: deterministic 960×620 transparent
  magenta/cyan logo glow Sprite.
- `Resources/splash/splash_deco_stars`, `splash_deco_lightning`,
  `splash_deco_confetti`, and `splash_deco_numbers`: sparse transparent
  1080×1920 decoration overlays.
- `Resources/splash/splash_char_boy`: transparent blue-hoodie boy hero,
  facing right with his fist extended.
- `Resources/splash/splash_char_girl`: transparent pink-hoodie girl hero,
  facing left with her fist extended to meet the boy.

The background contains no baked text, logos, buttons, characters, numbers,
or other UI. Every overlay remains text-free and transparent away from its
art.

## Background prompt

```text
Full-screen portrait mobile game splash BACKGROUND ONLY, no logo, no text,
no letters, no numbers, no characters, no mascots, no buttons, no UI.
Premium glossy cartoon mobile-game style matching the approved HOL logo and
number mascots. Deep indigo night sky, broad friendly blue-violet clouds, and
chunky rounded glowing stairs receding upward into the clouds. Cyan and muted
magenta rim light, rich depth, polished highlights, and clean rounded shapes.
No synthwave, arcade grid, neon wireframe, or UI chrome.
```

The accepted generated source was center-cropped and resized with Lanczos
filtering to the native Android portrait dimensions. Generated checkerboard
previews for transparent art were converted to true alpha before import.

## Mascot-six provenance

The PNG payload was copied byte-for-byte from commit `9f3f701`, path
`Assets/newdesign/Resources/avatars/numbers/avatar_number_06.png`.
Its SHA-256 is
`067beafc207aea302e0993a3bacdb2b69478429aa3685f275bb6705bd902ac4b`.
The asset at this new path has a new Unity GUID.
