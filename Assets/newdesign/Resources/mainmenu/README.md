# HOL Main Menu neon-cartoon layer pack

Text-free production PNG layers for the Main Menu reskin.

## Exact reference art

These files are byte-for-byte copies from `Resources/reference/`:

- `hol_logo_exact.png`
- `mascot_3_exact.png`
- `mascot_7_exact.png`
- `opponent_purple_exact.png`
- `player_cyan_exact.png`

## Generated chrome and decor

- Festive indigo night-arcade background with confetti and stars
- Confetti and star overlays (horizon/lightning/number overlays stay in the pack but Home no longer shows them)
- Gold, cyan, violet, and magenta pill controls
- Daily Hunt, player-chip, and tip frames
- Circular blue settings, smiling-star Solo, chat-bubble Private Room,
  target-plus-calendar Daily Hunt, gold trophy, dormant 1V1, and tip icons
- Logo, primary-action, and secondary-row glow/highlight layers

All generated layers use clean alpha edges. Unity L/B/R/T 9-slice borders are
encoded in each applicable `.meta` file.

The TIP frame uses `{130, 140, 130, 155}` borders so its cyan and magenta end
accents remain inside fixed corner slices instead of stretching with the body.

## Runtime text

All labels remain live TextMesh Pro (TMP) content with EN/EL localization.
No user-facing copy is baked into this image pack.

`mainmenu_cta_violet_9s.png` remains dormant; it does not add a 1V1 ONLINE
button or any other interaction. `mainmenu_icon_1v1.png` is likewise a dormant
visual asset until a real online matchmaking callback exists.
