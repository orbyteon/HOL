# Changelog

All notable changes to HOL. Dates are commit dates; versions follow the
Play Console versionName in `ProjectSettings.asset`.

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
