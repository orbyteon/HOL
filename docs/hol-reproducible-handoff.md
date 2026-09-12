# HOL reproducible developer handoff — 2026-09-12

## Open the complete project

- Unity **2022.3.62f3** (`96770f904ca7`), with Android Build Support, SDK/NDK and OpenJDK for Android work.
- On Marinos's PC the actual canonical project is `C:\Users\orbyt\Desktop\HOL_GitHub_clean\HOL`. The differently nested `HOL\_GitHub\_clean\HOL` path does not exist there.
- On Andreas's PC, clone the whole repository into one project folder. After integration, check out the verified `main` revision and add that folder in Unity Hub. Do not copy `Library`, `Temp`, signing material, credentials or another player's preferences.
- Allow the pinned `Packages/manifest.json` and `packages-lock.json` dependencies to restore, including the configured OpenUPM registry. Do not upgrade Unity or packages during handoff.
- Open **Assets/Scenes/SplashScene.unity** and press Play. This is the enabled startup scene. **Assets/Scenes/MainMenu.unity** is the second enabled scene and contains the runtime screen owners. Opening MainMenu directly skips the first-launch entry and is not the onboarding verification path.
- A fresh local profile follows the existing onboarding; a completed local profile enters Home. Do not delete a real player's saved profile to demonstrate onboarding: use the existing isolated capture/test fixtures.
- Most production UI is constructed by its screen owner in Play Mode. An empty/incomplete edit-time hierarchy is not a missing artwork package. Do not save a transient test scene or runtime UI back over a production scene.

## Preservation and reconstruction

The integration includes the complete visual checkpoint `60f2c298cd82fcc1f0e3a92f162885c77ee1668f`, not only the earlier PvP implementation. All 21 original canonical dirty/untracked project files were separately archived and hash-checked before and after reference capture. Existing backup/recovery branches remain preserved.

Recovery `6035bcf09f56e4eb625a90ba89ff0dda3df8b594` was inspected separately. The two Daily Hunt SDF fonts contributed four semantic material/hash fields each. Their atlas bytes and glyph data did not change. Serialization whitespace and machine-generated Editor settings were not mechanically merged. A clean import reproduced the LevelPlay Standalone dependency symbol without copying those local settings. A nonempty generated platform field remains in protected local preservation only, not Git.

The only additional presentation change is the minimal Private Room landing mascot containment correction: at standard portrait width each mascot moves outward by five reference units to clear the tip panel; tall layouts keep the existing maximum artwork size and place it below the tip within the safe edge. No artwork, card identity, navigation, rules, timing, backend or economy was redesigned.

## Local evidence and limitations

Source candidate `2b894eb02908b95e16d5a8e9e11637b68d2c1428` was checked out clean, its independent prior Library preserved outside the project, and imported again from **no Library** using the installed pinned Unity. No canonical Library was copied.

- Focused Node presentation/asset contracts: **21/21 passed**.
- Recovered Daily Hunt font EditMode cases: **2/2 passed**.
- Focused PlayMode regressions: **19/19 passed**, including the ten originally failing methods, onboarding/navigation, sharing/result propagation, identity and the additional mascot-containment test.
- Five native capture fixtures produced **130 original PNGs**: 36 shared-menu, 52 PvP, 4 Solo, 30 onboarding and 8 Daily Hunt. Both EN/EL and standard/tall portrait states match the corresponding preserved local-reference cases.
- **104/130 pairs are pixel-identical**, including all onboarding, all Solo and every non-Private-Room shared-menu frame. The remaining pairs include the explicit mascot adjustment, live Daily reset time, result/rematch confetti and native animation/render timing. They are not claimed pixel-identical or automatically human-approved.
- The combined clean-import run recorded **22 passed / 2 failed**, not a fully passing suite. Running captures before regressions exposed two fixture-order defects: inherited tall TMP metric boxes and measuring result children before invoking the final result-layout owner. Both were corrected in tests only; the subsequent focused run passed **19/19**. No captures were rerun for that test-only correction; production/asset bytes remain identical to the captured candidate.
- Zero-tolerance geometry remains zero-tolerance. Expected floats are stored at explicit IEEE-754 boundaries rather than expanding tolerances. Existing visible-glyph containment, readability, centering and interaction checks remain in force.

Raw XML, import logs, all original paired PNGs, SHA-256 inventory, pixel-delta CSV and the browsable comparison are preserved externally under `HOL_REPRODUCIBLE_HANDOFF_20260912_01`. Failed attempts remain available; the local reference is not replaced by a fabricated mockup.

## Networking, approvals and integration

Offline/native fixtures do not prove authenticated two-device networking. Existing isolated test target remains **HOL-PvP-Test / 11CB9E**; only its approved test APK/operator procedure may be used for the Marinos/Andreas playtest. Do not copy operator keys or device/account identities into this project. Production provisioning, signing, Play Console, releases and deployment are separate gates, not developer-import prerequisites.

The approval on `60f2c298…` does not approve later corrections. Required automatic CI/downstream PlayMode and an independent approving review must cover the final PR head. Keep the canonical checkout and its authoring changes preserved until those gates permit a normal merge. Only then, with the HOL Editor closed and preservation verified, switch canonical HOL to `main` and fast-forward; verify local and remote SHAs and zero staged/unstaged/untracked/conflicted paths. No reset/clean or protection bypass is part of this handoff.
