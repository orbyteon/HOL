# Changelog

All notable changes to HOL. Dates are commit dates; versions follow the
Play Console versionName in `ProjectSettings.asset`.

## [Unreleased]

### Fixed

- Delayed PvP guesses from a finished match can no longer land on a rematch:
  `submitGuess` rejects a stale or omitted `matchIndex`, the PlayFab client
  ignores a late success that belongs to a previous match, and the controller
  drops that callback instead of re-entering result handling.
- EditMode PvP panel builds no longer call `Destroy` (Unity forbids it
  outside Play mode) and reflection helpers bind `Show` by argument types
  so the 4-argument result presentation overload is no longer ambiguous.
- The Consumer First art actually renders. All 25 hand-authored design
  asset `.meta` files carried invalid GUIDs (30–31 hex chars where Unity
  requires exactly 32), so Unity silently regenerated them at import and
  the MainMenu scene's sprite references on `DesignRuntimeWiring` resolved
  to nothing — the menu kept the old art and every runtime-built screen
  fell back to flat colors, which is exactly how versionCode 5 looked on
  hardware. Every meta now carries a valid GUID with the scene references
  updated in lockstep, the four surface SVGs (background, panel, both
  buttons) moved under `Resources/design/` where `DesignRuntimeWiring`
  falls back to loading them by path when a scene reference is null, and a
  new `DesignSurfaceTests` EditMode test holds that load path green the
  same way `SignalIconTests` holds the signal icons.
- Ads initialize. `AdsManager` sent LevelPlay a Unity Ads-shaped game id
  where the ironSource App Key belongs, so every build so far failed ad
  init with error 2110 "Bad Request" — the consent flow worked, but no ad
  could ever load behind it. The constant now carries the dashboard's real
  App Key and is named `AppKey` so the two credential types cannot be
  confused again.

### Added

- A non-production Splash Android preview workflow builds a universal ARM64
  and x86_64 Development APK, captures and validates a 1080×1920
  `splash.png`, and uploads `hol-splash-android-preview` for QA. It does not
  use the production environment or change `versionCode`.
- **Portrait Private Room menu** — create/join and their waiting states now use
  the approved Teen Polish cards, 6/7 cast, live code sharing, and the existing
  PlayFab-authoritative callbacks.
- **Portrait Settings and Solo Search** — existing name, language, music,
  difficulty, ads-privacy, search, and cancel controls now share the same
  safe-area-aware Teen Polish presentation; Solo Search adds an animated radar.
- **Portrait Daily Hunt presentation** — the existing persisted daily challenge
  now uses the approved logo/ribbon/card layout without changing its guess,
  revive, or share rules.
- **Portrait PvP result celebration** — win/loss/draw now opens the approved
  Teen Polish result overlay with authoritative attempt counts, revealed
  number, fresh-secret rematch, Exit, the six fixed Signals, and a one-shot
  radial confetti/trophy pop on wins.
- **Portrait pre-battle waiting UI** — the existing PvP create/join waiting
  surfaces now use the approved Teen Polish versus layout with live room code,
  automatic-start status, localized rule copy, and no new gameplay action.
- **Daily Hunt** — the parked product-pass draft's one unshipped idea (#6),
  ported onto the Consumer First board. One date-seeded secret number
  shared by every player per UTC day, seven guesses, an emoji-trail result
  that copies to the clipboard for sharing, one rewarded-ad revive worth
  two extra guesses, and its own found-day streak. Pure client and fully
  resumable: state persists after every guess, the answer uses a stable
  domain-separated day hash (not a predictable seeded PRNG, but still
  client-readable rather than cheat-resistant), a backwards clock
  cannot replay a revealed answer, and a missed day ends the streak the
  moment the panel opens rather than at the next win. Deliberately left
  for follow-ups so this ships without a server or package change: the
  CloudScript percentile, the local notification reminder, and the
  draft's RangeBar visual.

### Changed

- Splash Home / `SplashScene` presentation is now owned by `SplashDesign`:
  cartoon stairs and clouds, the HOL logo, a boy-and-girl fist-bump, mascot
  6 on the left and 7 on the right, and only a gold elapsed hairline. It has
  no Main Menu chrome and no user-facing Splash text.
- Main Menu **Home** now uses the cartoon stairs/clouds bible: HOL logo,
  blue-hoodie boy + pink-hoodie girl, mascot **6 left / 7 right**, mapped
  onto the existing Settings gear, live name + match-win streak chip,
  Play Solo, Private Room, Daily Hunt, and tip. No Store, Profile, 1v1,
  coins, or fake `2,450`. Development Android QA captures a 1080×1920 Home
  screenshot plus an ARM64-capable debug APK; that preview is not a store
  build.
- **PanelPlay** (Play Solo → find challenger) uses the same stairs/clouds
  bible as a quieter inner page: HOL logo, cyan Back, gold Find Challenger,
  and the existing simulated-opponents disclosure. Searching, Settings,
  PvP, Daily Hunt, and the board stay unchanged. Development Android QA
  captures a 1080×1920 idle PanelPlay screenshot; that preview is not a
  store build.
- Daily Hunt and the PvP result presentation now refresh their formatted
  dynamic labels when the player changes language while either screen is
  active; result headings retain their localization key instead of a stale
  rendered string.
- The privacy policy's contact address is now `support@orbyteon.com` —
  a role mailbox owned by the publisher instead of a personal one. Both
  committed copies (the canonical `docs/privacy.html` and the byte-identical
  copy the provisioner serves at `/api/privacy`) change together; the live
  page updates on the next provisioner deploy.

- Every runtime-built label now renders through TextMesh Pro instead of
  legacy `UnityEngine.UI.Text`. The board was split between the two stacks
  — TMP in the scene and the input fields, legacy Arial in every label
  built from code — which is why screens never quite matched the mockups'
  type. `RuntimeUI.CreateText` (and the ConsentManager twin) now produce
  `TextMeshProUGUI`, the PvP board no longer rebuilds its labels through
  the `AsTmp` destroy-and-replace bridge, and `LocalizedLegacyText` is
  gone — runtime labels all localize through `LocalizedText`. Labels the
  scene itself serializes stay legacy until an editor session converts
  them; `ExtrasRuntimeWiring` still refreshes those on language change.
  The font is TMP's default atlas (LiberationSans SDF); a brand typeface
  remains an editor-session task.

## [0.4.0] — 2026-08-15

### Added

- End-of-match analytics. The 0.3.0 duel rules were tuned against a
  simulation — the opener coin flip took first-mover advantage from 63.7%
  to roughly even, and draws sit near 7% at human accuracy but climb
  toward 23% as both players approach a flawless binary search. That last
  number moves with the real population's skill and a simulation cannot
  settle it, so the release would have felt fine at launch and grown
  steadily more drawish with no signal but reviews. `MatchOutcome` carries
  result (draw included), both guess counts, who opened, whether the Lock
  was staked, and the rematch depth; `MatchTelemetry` posts one PlayFab
  event per finished match.
- `GameEvents.OnMatchCompleted`, the draw-capable counterpart to
  `OnMatchEnded`. `GameEvents.MatchCompleted` is now the single raise point
  for a finished match, so a call site cannot report one event and forget
  the other. Win/lose still reach `OnMatchEnded` unchanged, and a draw
  still does not — `(bool, int)` has no truthful way to say "draw".
- `PlayFabPvpClient.HasSession`, so a caller can reuse an existing session
  without being the reason one is created.

### Changed

- `docs/privacy.html` gains a "Gameplay analytics" section, and two claims
  that were true before this change are corrected: solo matches previously
  sent nothing to PlayFab, and the service previously held no match
  history. Both statements now describe what actually happens. The Play
  Console Data safety declaration has to be updated in the same release —
  see `docs/release-checklist.md` section 6.

### Fixed

- A lost match no longer discards its guess count. `MatchEnded(false, 0)`
  reported every defeat identically, which left the draw rate
  uninterpretable: a duel decided on the last candidate and a rout looked
  the same in the data.

### Security and reliability

- Serialized every PlayFab room mutation with a revision/epoch-fenced server
  lock, with bounded client retries for ordinary contention and stale-lock
  recovery. Signals, rematch commitments, acknowledgements, leaves, and guesses
  can no longer overwrite one another through Shared Group last-write-wins.
- Added sharded room expiry indexes and a deployment-managed five-minute
  `cleanupExpiredRooms` task for orphaned waiting, active, and completed rooms.
- Removed the client-writable Firebase fallback; PlayFab CloudScript is now the
  only PvP authority in every build.
- Raised Android target API from 35 to 36, pinned third-party GitHub Actions to
  immutable commits, and added automated dependency updates.
- Bounded the provisioning service's in-memory abuse limiter and added eviction
  tests so spoofed/high-cardinality IP input cannot grow the process map without
  limit.

## [0.3.0] — 2026-08-13

> **Deploying this release is order-sensitive.** The duel rules are not
> backward compatible with a 0.2.x client, which renders a drawn match as a
> loss. Ship the client, set PlayFab `minVersion` to `0.3.0`, *then* deploy
> CloudScript. See `docs/release-checklist.md` section 3.

### Fixed

- The app launched upside down on device. `defaultScreenOrientation` was `1`
  (PortraitUpsideDown) rather than `0` (Portrait). Any value other than `4`
  (AutoRotation) is a hard orientation lock, so the `allowedAutorotateTo*`
  allow-list underneath it — which correctly permitted portrait only — was
  never consulted. That combination is why the settings read as portrait-only
  while every launch came up rotated 180°, and why it survived review: the
  allow-list is the part a reader checks. Found on a real device against the
  0.3.0 versionCode 2 candidate; fixed for versionCode 3.
- Solo was playable exactly once per launch. The end-of-match button is
  relabelled "Rematch" and wired to `GameManager.RestartMatch`, but the
  match-over flag that `NumberManager` gates every number submission on
  was left set, so the next match could never be started. The flag now
  tracks whether a match is *set up* rather than whether the rules object
  has finished, and clears when the board is reset.
- A decided match no longer waits on a pointless tap. When the opponent's
  guess was the one that closed the round, the game still asked the player
  to answer Higher/Lower for a match that was already over, putting a dead
  interaction between them and the result.
- Solo never actually showed the narrowed range. `GameManager.rangeText` is
  an optional Inspector field wired nowhere — not in the scene, not at
  runtime — so `UpdateRangeText` had always written into nothing, and the
  "live range label" the README credits to solo play only ever existed in
  PvP. It is built at runtime now, which is also what gives the Lock's
  one-line explanation somewhere to appear.
- The PvP result line no longer collides with the Signals row. A drawn
  match is three lines in English and four in Greek, where the closing tip
  wraps; at 64pt that spilled into the second row of Signal buttons, which
  shares the result screen. The result is now 48pt in a taller box, with
  the leave button moved down to match.

### Changed (duel rules — gameplay balance)

- **Whoever moves first no longer wins the duel.** Two players who both
  binary-search a number in 1–100 need the same number of guesses 27% of
  the time and otherwise reach each guess number in lockstep, so under
  "first correct guess wins" the opening player took **63.7%** of matches
  against an identical opponent. PvP was worse still: CloudScript
  hardcoded `turn: "guest"`, so the joiner opened *every* match and
  carried that win rate by default.
- The opener is now a coin flip, taken when the second player joins and
  fixed for the match (PlayFab and the Firebase fallback both).
- **Equal turns.** A round is one guess per side, and a match can only end
  once a round closes — so the responder always answers the opener's
  winning guess. Simulated over the real rules at human accuracy, the two
  sides now take 46.3% and 46.5% with 7.3% draws; the seeded simulation in
  `tools/test/cloudscript.test.mjs` reproduces those figures exactly.
- **The Lock**, one per match: stake it on a guess and a correct one wins
  a same-round tie, while a wrong one forfeits your next turn. It is the
  game's first genuine decision — staking it only on a certain guess
  beats never locking 50.3% to 36.6%, and locking on every guess loses
  18.2% to 63.8%. The button turns into a prompt once the range is down
  to three candidates, which is what keeps draws rare in practice.
- The Lock is **revealed only after the player has played a round**, and
  explains itself in one line the first few times it appears. A first
  match should read as plain higher-or-lower — the original concept — with
  the one added decision arriving after the loop has been felt. Nothing is
  lost strategically: with a hundred candidates still open there is nothing
  sensible to stake it on. This is presentation only; the server still
  accepts a Lock on any turn.
- A tied round with both sides locked, or neither, is an honest draw. It
  counts as a match, breaks no streak, and stays out of the window that
  tunes the adaptive AI.
- Solo plays by exactly these rules too, so the mechanic is learned
  against the AI before it decides a real duel. Difficulty now shapes the
  opponent's *judgement* as well as its aim: Easy over-commits the Lock,
  Hard waits for certainty and opens on the midpoint instead of at random.

### Added

- **Rematch in the same room.** A finished duel no longer tears the room
  down: both players commit a fresh secret and the next match is dealt in
  place, so friends never re-share an invite code to play again. The
  handshake needs both sides, a leaver is reported to the opponent instead
  of leaving a dead button on screen, and an unanswered room is released
  after about two minutes rather than being held open indefinitely.
- **Signals** — a fixed six-entry vocabulary ("Good luck!", "So close!",
  "Ouch!", "Nice one!", "Your turn!", "Good game!") players can send
  during a PvP match and on the result screen. Only the index travels, so
  each player reads it in their own language. Deliberately not free-form
  chat: with no user-authored text there is nothing to moderate, no
  reports or blocks to build, and nothing new to declare on the Play Data
  Safety form. Server-validated and capped at 12 per side per match.
- PvP now shows the narrowed guess range that solo play has always shown.
- Rule tests: `tools/test/cloudscript.test.mjs` drives the real
  CloudScript through an in-memory Shared Group store (`node --test`), and
  `Assets/Tests/EditMode/DuelRulesTests.cs` covers the C# implementation.
  `tools/test/lock-policy-sim.mjs` plays Lock policies head to head.

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
