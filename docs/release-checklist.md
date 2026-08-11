# HOL — Release checklist

Everything code-side is done on `main`. These are the remaining manual
steps, in order. Each links to where the details live.

## 1. Smoke test in Unity (on the machine with Unity installed)

Pull `main`, open the project, let package resolution finish (the manifest
diet removed four packages — let Unity prune them), then press Play and
verify:

Splash:

- [ ] Indigo gradient background (not flat black), faint drifting digits
- [ ] Logo blooms in, then breathes; cyan→magenta seam + tagline below it
- [ ] Gold loading hairline fills at the bottom; tap skips the splash

Main menu / solo:

- [ ] Menu backdrop is indigo-toned (photo dulled, drifting digits behind
      panels); panels are indigo, never white or red
- [ ] Consent dialog appears on first launch; ads initialize after a choice
- [ ] First launch on a Greek-language device starts in Greek
- [ ] Settings gear opens settings (menu panel hides underneath)
- [ ] EN/EL buttons switch all labels, including scene labels ("SAVE" → "ΑΠΟΘΗΚΕΥΣΗ")
- [ ] Difficulty buttons highlight the active choice in gold
- [ ] Stats label shows on the main menu (wins/losses/streak/best)
- [ ] Play → searching → Cancel button stops the search; Android back too
- [ ] Opponent-found stinger plays when found, NOT when tapping Search
- [ ] Every button clicks (menu, settings, game) and squashes on press;
      panels fade+rise when shown
- [ ] Match end → Rematch button restarts via number entry; win bursts
      confetti, plays the win stinger
- [ ] Android back during a solo match exits to the menu
- [ ] "Opponents are simulated" disclosure visible on find/searching panels

PvP:

- [ ] PvP Duel → create room shows a 5-letter code; join from a second
      device/instance; leaving a match notifies the opponent
- [ ] Duel win/lose plays the stinger, updates the menu stats label, and
      shows "In X guesses" on a win

Regression checks for the QA fixes (rounds 1–3):

- [ ] Solo: a guess outside the narrowed range is rejected but keeps the
      typed number in the input (no retyping)
- [ ] Solo: STOP GAME and the number-input placeholder ("1-100") follow the
      selected language
- [ ] Solo: Android back mid-match lands on a clean menu in ONE press —
      no menu/game overlap, no second press needed
- [ ] The gold PvP Duel button is covered by panels (not tappable) during
      a solo match and while settings are open
- [ ] Switch language with the PvP menu open: every PvP label, button, and
      placeholder relabels live (no restart)
- [ ] PvP: back out of create/join, then have a friend join the code →
      they get "room not found"; no match panel hijacks your screen
- [ ] PvP: Android back navigates PvP panels (create/join → menu, menu →
      closed); mid-match back does nothing (Leave button is the way out)
- [ ] PvP: double-tap Guess quickly → only one guess counted and sent
- [ ] Lose with a streak ≥ 2 → save-streak offer; if no rewarded ad is
      available (e.g. unit missing), a "no ad available" message shows and
      the button STAYS for retry
- [ ] Force-update dialog (set TitleData minVersion higher than the build)
      shows Update AND Quit buttons
- [ ] Player Settings → Icon shows the HOL icon (indigo, chevrons, gold
      dot), not the Unity default

## 2. PlayFab (PvP backend)

- [ ] developer.playfab.com → create Studio + Title, copy the Title ID
- [ ] Automation → CloudScript (Legacy) → paste `playfab/cloudscript.js`
      → Save → **Deploy** (the client requires the revision with the atomic
      `joinRoom` + server-authoritative `submitGuess` — an old deployed
      revision breaks joining and makes every guess fail)
- [ ] Paste the Title ID into `PvpRuntimeUI.playFabTitleId` (Inspector on the
      `PvpRuntimeUI` object in `MainMenu`; copied onto the backend at startup)
- [ ] Optional force-update gate: Title Data → add key `minVersion`
      (e.g. `0.1`). Builds older than it get a blocking update dialog;
      while the key is absent every version plays (fail-open)

## 3. Release keystore

- [ ] Player Settings → Keystore Manager → Create New →
      `hol-release.keystore`, alias `hol` (never reuse another title's key —
      this project once pointed at RideCore's)
- [ ] Back up the keystore + passwords offline **immediately**; losing it
      means never updating `com.Orbyteon.HOL` again
- [ ] Keystore stays out of git (`*.keystore` is gitignored)

## 4. Dashboards

- [ ] LevelPlay: app registered with key `6076495`, bundle `com.Orbyteon.HOL`,
      `Interstitial_Android` unit active (unityads-adapter 5.6.0 is
      catalog-compatible with SDK 9.5.0 — no change needed) — plus a
      `Rewarded_Android` unit for the save-your-streak placement
- [ ] Play Console: create app (bundle id is permanent), upload AAB signed
      with the HOL keystore, content rating, data-safety form (declare:
      device ID via PlayFab + ads, optional display name in PvP), privacy
      policy URL from step 5. Listing copy (EN + EL, short/full
      descriptions, release notes) is paste-ready in
      `docs/store-listing.md`. Mandatory listing assets are ready in
      `docs/store/` (feature graphic 1024×500, hi-res icon 512×512).
      Promo video: upload `promo/hol_teaser.mp4` to YouTube (unlisted is
      fine) and paste its URL into the listing's promo-video field

## 5. Privacy policy hosting

GitHub Pages on a **private** repo requires a paid plan (API-verified:
"Your current plan does not support GitHub Pages for this repository").
Pick one:

- [ ] **Make the repo public** (if acceptable), then repo → Settings →
      Pages → Source: `main` branch, `/docs` folder → policy live at
      `https://orbyteon.github.io/HOL/privacy.html`
- [ ] Or upgrade the GitHub plan, then same Pages steps
- [ ] Or host `docs/privacy.html` anywhere public (Netlify Drop, itch.io
      project page, Google Sites) — any static URL works
- [ ] Link the final URL in the Play Console listing

## 6. Security hygiene

- [ ] Rotate the GitHub personal access token used during development
      (it was exposed in chat): github.com → Settings → Developer settings
      → revoke, then re-add the new one locally when pushing
- [ ] Keep `promo/hol_teaser.mp4` and other secrets considerations in mind
      before making the repo public
