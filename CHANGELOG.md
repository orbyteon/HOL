# Changelog

All notable changes to HOL. Dates are commit dates; versions follow the
Play Console versionName in `ProjectSettings.asset`.

## [Unreleased]

### Added (product pass: rivals, modes, themes)

- Rivals now carry a visible persona next to their name ("Nikos · the
  Chaotic"): Calculator, Cautious, Chaotic, Intuitive — difficulty still
  decides how often the AI errs, the persona decides what its play and
  its lapses look like
- Solo modes on the challenger panel: Classic, Sudden Death (find it
  within 7 guesses), Time Attack (45 s clock that burns only on your
  turn), with a live status readout during the match
- Cosmetic themes over the whole design system, unlocked through play:
  Converging (default), Ember (5 wins), Frost (3-day Daily Hunt streak),
  Mono (perfect win) — swatches in Settings; unlocks are sticky

### Added (product pass: daily ritual + telemetry)

- Daily Hunt: everyone hunts the same date-seeded secret number, seven
  guesses, one attempt per day — with an emoji-trail result that copies
  to the clipboard for sharing, one rewarded-ad revive (+2 guesses), and
  its own completion streak; reachable from the menu, in EN and EL
- Anonymous gameplay telemetry (session start, match results, daily-hunt
  results) through PlayFab's client event pipeline — no new SDKs; events
  are silently dropped when PlayFab isn't configured; disclosed in the
  privacy policy (solo-mode wording updated accordingly, effective date
  bumped)
- The range bar can now be driven manually (SetRange), powering the
  Daily Hunt's converging window

### Improved (UX pass)

- Solo play finally shows the converging interval: a live 1–100 range bar
  whose lit window narrows with every answered guess (min/max in gold at
  its edges), a "Between X and Y" caption, and both guess histories on
  the game panel — all of it was tracked internally but never surfaced
- New "How to play" card: shows once on first launch (never stacking over
  the consent dialog) and stays reachable from a "?" button on the menu,
  in English and Greek
- PvP create/join/guess buttons disable while their request is in flight,
  so a slow network reads as "working" instead of "broken"
- Android back on a live PvP match now asks for a second press (same
  contract as solo); a decided match leaves immediately
- Answering Higher/Lower puts the caret straight back into the guess
  field; the solo number input is numeric-only with a 3-character limit

### Improved (Converging Light visual redesign)

- The menu and splash now wear a fully owned look: a deep multi-stop
  indigo field with a slowly breathing cyan/magenta/gold aurora, two
  parallax planes of drifting digits, and a corner vignette — the old
  photo background survives only as faint texture
- Every screen panel is now a floating card: rounded surface, soft
  elevation shadow, and a hairline cyan-to-magenta seam along its top edge
- All buttons joined one role system: muted-gold primary CTA, cyan
  secondary actions, ghost-indigo tertiary (with seam hairline), softened
  icon art for settings/quit — replacing the mixed scene button art
- Primary CTAs (Play, Find challenger, PvP entry, room create, guess)
  carry a slow masked shine sweep; sweeps are phase-randomized so they
  never sync
- Menu and PvP screens assemble with a staggered content cascade instead
  of popping; PvP screens became dimming scrim + centered card layouts
  with the room code as a gold letterspaced hero mark over a seam rule
- Victory now lands with a warm full-screen light pulse synced to the
  confetti (solo and PvP, each scoped to its own screen)
- The splash gained the same aurora/vignette stack plus a gold halo bloom
  behind the logo
- Scene input fields became rounded near-white wells with elevation,
  matching the runtime-built ones; in-match headers read as letterspaced
  engraved captions

### Improved (frontend experience pass)

- Losses now reveal the secret number you were hunting (solo and PvP) —
  no more finishing a round without ever learning the answer
- The soft keyboard's Done key (Enter in the editor) submits everywhere:
  solo number entry and all PvP inputs (secret, room code, guess)
- Settings language buttons highlight the active language in gold, same
  as the difficulty row (they used to look identical either way)
- PvP: a guess rejected by a network error is put back into the input for
  retry instead of being erased (matches solo behavior)
- Buttons built after startup (streak-save offer, reopened consent dialog)
  now get press-squash feedback and the shared click sound too
- Android back mid-match now asks for a second press ("Press back again to
  leave the match") instead of instantly forfeiting on one stray gesture;
  once the match is decided, back exits immediately
- The soft keyboard no longer pops back up over the result screen after a
  winning guess, and a stray post-match submit no longer claims
  "Wait for your turn..."
- PvP room-code input auto-uppercases as you type, matching how codes are
  displayed and shared (backends already normalized case)
- Main-menu stats now include the fastest win ("Fastest win: N guesses") —
  tracked since launch but never surfaced
- PvP "Creating room..." / "Waiting for your challenger..." now animate
  trailing dots (the invite handshake is the longest wait in the game and
  looked frozen); the copy-invite confirmation shows briefly, then the
  animated waiting line resumes
- Disabled buttons no longer play the press-squash animation or click
  sound (`ButtonJuice` checks `Selectable.IsInteractable`)

### Fixed (self-review of this pass)

- Solo keyboard-submit wiring now finds `NumberManager` on the inactive
  `PanelGAME` (`FindObjectOfType(true)`) — it was silently never wired
- Create/Join are guarded against re-submission while a room is live: a
  stray Confirm tap or keyboard Done while "Waiting for your challenger"
  used to create a second room and orphan the already-shared invite code
- The post-match submit guard runs before validation, so an empty stray
  submit can't flash "Invalid number" over the result screen
- The soft keyboard is closed when the match ends on the opponent's turn
  (`GameManager.EndGame` → `NumberManager.CloseInput`), not only after the
  player's own winning guess

### Fixed (backend/ads sweep)

- `AdsManager.ShowAd` re-entrancy: a second call while an interstitial was
  on screen overwrote the pending callback, cancelled the live ad's
  safety timer, and cleared the in-progress flag mid-ad; blocked calls
  now invoke their callback directly without touching the live ad's state
  (`AdsManager`, `ForceUpdate`, and both PvP backends were audited in the
  same sweep — no other findings)

### Infrastructure

- GameCI workflow (`.github/workflows/ci.yml`): Android compile-check
  build on every PR and push to main; skips with a warning until the
  Unity license secrets are configured (release-checklist step 0)
- EditMode test assembly (`Assets/Tests/EditMode`, reflection-based so it
  never touches the player build): localization-table integrity (every
  key has non-empty EN+EL, matching format placeholders, no malformed
  format strings), scene-label key mapping resolves, and the difficulty
  PlayerPrefs key stays in sync between `GameManager` and the Settings
  UI; CI runs them in a new "EditMode tests" job
  (`com.unity.test-framework` re-added to the package manifest)
- Release builds override a stray Firebase backend selection to PlayFab
  when a Title ID is configured — the Firebase fallback is dev-only
  (plaintext secrets in the room document, non-atomic joins)
- Background music track imports as Streaming (was Decompress On Load —
  the whole 6 MB track unpacked into RAM at startup on device) with
  load-in-background on and Vorbis quality 0.7; short SFX stay
  Decompress On Load, which is correct for them. Release-audit checks
  passed alongside: IL2CPP + ARM64 + targetSdk 35 confirmed, both scenes
  in the build list in order, no missing-script components or broken
  event wiring in either scene, app icon reference intact

### Fixed

- `PulseText` no longer permanently brightens translucent labels to full
  alpha on disable, and no longer pops on enable (note: the component is
  not yet attached anywhere — fixed for future use)
- `ButtonJuice` skips its per-frame lerp once settled (dozens of live
  buttons on mobile) and guards zero-scale buttons
- `MenuManager.BackToMenu` is null-guarded like `Update`, so a partially
  wired scene can't crash back-navigation

## [0.2.0] — 2026-08-11 (release candidate: `v0.2.0-rc2`)

First public release candidate.

### Added

- **Live PvP duels** — room-code invites (5 letters), PlayFab backend with
  server-atomic joins via CloudScript; Firebase RTDB as documented fallback
- **Force-update gate** — optional PlayFab TitleData `minVersion` blocks
  outdated builds (fail-open when unconfigured), with Quit escape hatch
- **Rewarded "save your streak"** — watch a rewarded ad to rescue a lost
  streak (LevelPlay `Rewarded_Android` placement); restore survives an app
  kill between reward and ad-close
- **Perfect-run celebration** — win in 7 guesses or fewer
- **Difficulty modes** — Easy / Normal / Hard / Adaptive (tunes to your
  recent solo form), selectable in Settings
- **Converging Light design layer** — indigo depth gradients, animated
  splash with logo bloom and loading hairline, drifting number fields,
  rounded runtime UI, confetti, button squash, panel animations
- **English + native Greek** localization with live switching (runtime
  labels included); first launch follows the device language
- **App icons** — adaptive icon (indigo + chevron/dot layers, all
  densities) plus legacy fallback
- **Daily streak, haptics, sound coverage** — win/lose stingers (solo and
  PvP), click on every button, opponent-found stinger
- **Store package** — listing copy EN+EL, release notes, feature graphic,
  hi-res icon, promo teaser, privacy policy (`docs/`)
- **Proprietary LICENSE** (© 2026 Orbyteon)

### Fixed (QA review, 3 rounds — 18 findings)

- LevelPlay 9.x `OnAdDisplayFailed` signature (Safe Mode compile errors)
- PvP backend was unconfigurable: Title ID / database URL now set via
  `PvpRuntimeUI` Inspector fields, copied to the runtime-created backend
- PvP Back/Close leaked a live room + active poller; a late joiner could
  hijack the screen mid-solo-game
- Android back mid-solo-match hit the wrong branch (menu + game overlap);
  back now works on all PvP panels too
- PvP entry button rendered above every panel
- No `UnityWebRequest.timeout` — stalled connections froze matches silently
- PlayFab session-ticket expiry bricked PvP until restart (401 → re-login
  once, retry once)
- Leave-vs-winning-guess race on PlayFab (guesses now server-authoritative
  via CloudScript `submitGuess`); Firebase ghost rooms (PATCH after DELETE)
  treated as closed
- Guess double-submit swallowed guesses and inflated the count
- Streak-save button self-destructed when the rewarded ad was unavailable;
  failure now surfaces a message and keeps the button
- Interstitial 10s safety timer fired mid-ad (now 120s); frequency cap
  stamps on actual ad close
- PvP results no longer skew solo adaptive difficulty
- Localization: STOP GAME button and number-input placeholder were
  hardcoded English; runtime UI ignored language switches until restart
- `bundleVersion` bumped to 0.2.0 (was 0.1 — would have false-triggered
  the force-update gate)
- Splash logo had no alpha (opaque black box over the indigo splash)
- Missing `.meta` files (9 UI PNGs + 4 folders) caused per-machine GUID
  churn; default app icon was unset
- Unreachable cheat-detection code removed

## [0.1.0] — internal

Original solo game: Higher/Lower duel vs simulated opponent, stats via
PlayerPrefs, LevelPlay interstitials with consent gate, two-scene flow.
