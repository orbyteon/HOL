# Main Menu Checkpoint Integration Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Integrate the approved production Main Menu art into the existing MainMenu scene at runtime, preserving every real callback, and produce a debug APK plus exact 1080×1920 EN/EL Android screenshots for approval.

**Architecture:** A single `MainMenuAuthoritativeVisuals` component owns only the Home presentation on the existing `/Canvas` and existing serialized `BACKROUND` object, renamed at runtime to `MainMenuRoot`. It creates one `SafeAreaRoot`, reparents the four existing functional Home buttons without replacing them, and disables competing Home writers while Home is visible. Existing non-Home panels and the separate PvP/Consent canvases remain untouched.

**Tech Stack:** Unity 2022.3.62f3, UGUI, TextMesh Pro, `Resources.Load<Sprite>`, NUnit EditMode/PlayMode tests, GameCI, Android x86_64 debug APK, adb emulator screenshots.

## Global Constraints

- Work only on `agent/mainmenu-authoritative-integration`; do not merge any PR.
- Main Menu checkpoint only: no other screen, Store, Profile, ranking, public 1V1, gameplay, backend, or release feature.
- Do not change `ProjectSettings/ProjectSettings.asset`, `versionCode`, release workflows, production config, signing, PlayFab deployment, provisioner deployment, or Play Console.
- Use the existing `/Canvas`; create no additional Canvas.
- Reuse serialized `/Canvas/BACKROUND` as `MainMenuRoot`; keep `MenuManager.mainMenuPanel` pointing to the same GameObject.
- Create exactly one `SafeAreaRoot` under `MainMenuRoot`.
- Reparent, never recreate, `ButtonPlay`, `Buttonsettings`, runtime `ButtonPvP`, and runtime `DailyHuntButton`; preserve every existing `Button` and `onClick`.
- `ButtonPvP` remains private-room create/join. Do not expose dormant 1V1 assets.
- All user-facing copy is live TMP through `L10n`, with EN and EL entries.
- Hide legacy presentation without destroying controller-owned objects.
- Production home artwork loads only from `Resources/mainmenu/`.
- Primary/secondary gloss overlays are non-raycast and aligned to their final target rects. The two arcs in `mainmenu_gloss_secondary_row` must align with the Private Room and Daily Hunt controls.
- No runtime Home decoration may sit outside `SafeAreaRoot`.
- Stop after the Main Menu checkpoint and deliver changed files, CI, debug APK, and EN/EL 1080×1920 screenshots.

---

### Task 1: Materialize the approved production asset pack

**Files:**
- Delete: `.github/materialize-mainmenu/part-000` through `part-007`
- Create: `Assets/newdesign/Resources/mainmenu.meta`
- Create: `Assets/newdesign/Resources/mainmenu/**` from approved asset commit `9f3f7014a979cf903b867910bce7bb13376a6f5f`
- Create: `Assets/Tests/EditMode/MainMenuProductionAssetsTests.cs`
- Create: `Assets/Tests/EditMode/MainMenuProductionAssetsTests.cs.meta`

**Interfaces:**
- Produces stable resource paths `mainmenu/<filename-without-extension>`.
- Produces no runtime behavior.

- [ ] **Step 1: Write a failing Unity asset contract**

Create an EditMode test that loads all production sprites through
`Resources.Load<Sprite>`, including the dormant 1V1 assets, and asserts these
seven sliced borders:

```text
mainmenu_cta_gold_9s          112,80,112,80
mainmenu_cta_blue_9s           72,64,72,64
mainmenu_cta_violet_9s         72,64,72,64
mainmenu_cta_magenta_9s        72,64,72,64
mainmenu_daily_hunt_frame_9s   72,56,72,56
mainmenu_player_chip_frame_9s  48,40,48,40
mainmenu_tip_frame_9s         130,140,130,155
```

The test assembly must not reference `Assembly-CSharp`.

- [ ] **Step 2: Commit and push the failing contract**

Commit only the test and its `.meta`, push, then let CI prove RED because
`Resources/mainmenu/` is absent.

- [ ] **Step 3: Materialize exact approved files**

Copy only `Assets/newdesign/Resources/mainmenu.meta` and
`Assets/newdesign/Resources/mainmenu/**` from commit `9f3f701`. Delete the
eight encoded staging chunks; they are incomplete transport artifacts, not
shipping content.

- [ ] **Step 4: Commit, push, and verify GREEN**

Run the focused asset test in Unity CI. Locally run the repository Node suite
and verify every PNG has a `.meta`, every GUID is unique, and no file outside
the stated paths changed.

### Task 2: Authoritative Home owner and callback-preserving hierarchy

**Files:**
- Create: `Assets/SCRIPT/Design/MainMenuAuthoritativeVisuals.cs`
- Create: `Assets/SCRIPT/Design/MainMenuAuthoritativeVisuals.cs.meta`
- Modify: `Assets/SCRIPT/Localization/L10n.cs`
- Create: `Assets/Tests/PlayMode/MainMenuAuthoritativeVisualsPlayModeTests.cs`
- Create: `Assets/Tests/PlayMode/MainMenuAuthoritativeVisualsPlayModeTests.cs.meta`
- Modify: legacy Main Menu PlayMode tests only where their old owner assertion conflicts with the new approved owner

**Interfaces:**
- `MainMenuAuthoritativeVisuals.IsReady` — true only after the four real buttons are present, reparented, styled, and localized.
- `MainMenuAuthoritativeVisuals.OwnsHome` — true only while `MenuManager.mainMenuPanel` is active.
- No public gameplay API.

- [ ] **Step 1: Write failing PlayMode checkpoint tests**

On the real `MainMenu` scene, after runtime wiring settles, assert:

1. Exactly one root screen-space Main Menu canvas remains; existing PvP and Consent overlay canvases are unchanged and no fourth Canvas exists.
2. Existing serialized `BACKROUND` becomes `MainMenuRoot`, with exactly one direct `SafeAreaRoot`.
3. `ButtonPlay`, `Buttonsettings`, `ButtonPvP`, and `DailyHuntButton` are the original controls under `SafeAreaRoot`; total Home Button count is four.
4. Invoking Solo opens `PanelPlay`; Settings opens `PanelSettings`; Private opens the existing `PvPMenuPanel`; Daily Hunt opens `DailyHuntPanel`.
5. No Button name/text/sprite exposes `1V1`, `ONLINE`, `QuickMatch`, `mainmenu_icon_1v1`, or the violet CTA.
6. Home production images all use `mainmenu/` sprites and have `raycastTarget == false`.
7. Player/opponent heroes have equal `sizeDelta` and mirrored X magnitude; mascot 7/3 remain present.
8. EN and EL switches update every Home TMP title/subtitle/tip/chip string.
9. The primary gloss is contained by `ButtonPlay`.
10. The secondary gloss row is non-raycast and its left/right arc boxes align with the Private and Daily button rects.
11. Returning from Settings restores the same single Home root without duplicates.

Record the four button instance IDs and listener effects before/after styling.

- [ ] **Step 2: Push RED test revision**

Commit and push tests before running CI. Expected RED: missing
`MainMenuAuthoritativeVisuals`, `MainMenuRoot`, and `SafeAreaRoot`.

- [ ] **Step 3: Add localized Home copy**

Add EN/EL keys for:

```text
mainmenu_play_title       PLAY SOLO / ΠΑΙΞΕ SOLO
mainmenu_play_subtitle    FIND CHALLENGER / ΒΡΕΣ ΑΝΤΙΠΑΛΟ
mainmenu_private_title    PRIVATE ROOM / ΙΔΙΩΤΙΚΟ ΔΩΜΑΤΙΟ
mainmenu_private_subtitle PLAY WITH FRIEND / ΠΑΙΞΕ ΜΕ ΦΙΛΟ
mainmenu_daily_subtitle   SPECIAL EVENT / ΕΙΔΙΚΗ ΑΠΟΣΤΟΛΗ
```

Use existing `daily_hunt`, `hud_tip`, `simulated_opponents`,
`player_default`, and `stats_streak` for remaining copy.

- [ ] **Step 4: Implement the sole Home presentation owner**

`MainMenuAuthoritativeVisuals` must:

- install only on the root Canvas that owns `MenuManager.mainMenuPanel`;
- execute before legacy Home `Start()` passes and repeatedly suppress
  `ExactReferenceVisuals`, `AttachmentReskinVisuals`,
  `AttachmentReskinPolish`, `AttachmentReskinCanvasBindings`, and
  `DesignRuntimeWiring` while Home is active;
- re-enable only the attachment reskin components when Home is inactive, so
  existing non-Home screens retain their current presentation;
- rename the same serialized `BACKROUND` object to `MainMenuRoot`, disable its
  legacy `Image`, reset its RectTransform, and create/reuse one `SafeAreaRoot`;
- reparent the four real buttons with `SetParent(safeArea, false)`, preserving
  the Button instance and UnityEvent;
- hide `ButtonQuit`, `StatsLabel`, `Exact*`, `Board*`, old backdrops and legacy
  Home decorations without destroying them;
- create/reuse all new decorations under `SafeAreaRoot`, with
  `raycastTarget=false`;
- load only final `mainmenu/` PNG sprites;
- subscribe to `L10n.OnLanguageChanged`;
- poll at 0.25 s for late runtime buttons and Home/non-Home transitions;
- never instantiate a `Button`, `Canvas`, gameplay panel, or 1V1 object.

Approved 1080×1920 layout:

```text
Settings               (-455, 820), 82×82
Player chip            ( 335, 820), 390×110
HOL logo               (   0, 650), 610×320
Boy/Girl heroes        (±155, 390), 300×300 each
Mascot 7/3             (±405, 260), 210×280 each
PLAY SOLO              (   0,  80), 600×185
Private/Daily buttons  (±245,-150), 450×165 each
TIP panel              (   0,-430), 930×260
```

Button sprites:

- Solo: `mainmenu_cta_gold_9s`, `Image.Type.Sliced`.
- Private: `mainmenu_cta_blue_9s`, `Image.Type.Sliced`.
- Daily Hunt: `mainmenu_daily_hunt_frame_9s`, `Image.Type.Sliced`.
- Settings child icon: `mainmenu_gear_glossy`.
- Icons: solo/private/daily/tip/streak final PNGs.

Gloss constraints:

- Primary gloss is a non-raycast child stretched inside the final Solo rect.
- Secondary gloss row uses `mainmenu_gloss_secondary_row` at exactly
  `1000×320`, centered over the two 450×165 secondary rects at X ±245.
- Do not place the secondary gloss as two arbitrary children; preserve the
  authored two-arc alignment.

- [ ] **Step 5: Commit, push, and verify**

Run the all-`Assets/SCRIPT` mcs stub compile after the last C# edit. Push, then
require EditMode, PlayMode, and Android compile CI green.

### Task 3: Non-production Android capture path

**Files:**
- Create: `Assets/Editor/MainMenuPreviewBuild.cs`
- Create: `Assets/Editor/MainMenuPreviewBuild.cs.meta`
- Create: `Assets/SCRIPT/RuntimeUI/MainMenuCaptureBootstrap.cs`
- Create: `Assets/SCRIPT/RuntimeUI/MainMenuCaptureBootstrap.cs.meta`
- Create: `.github/workflows/mainmenu-android-preview.yml`
- Create: `Assets/Tests/PlayMode/MainMenuCapturePlayModeTests.cs`
- Create: `Assets/Tests/PlayMode/MainMenuCapturePlayModeTests.cs.meta`

**Interfaces:**
- Android intent extra `hol_capture_language=en|el`.
- Log marker `HOL_MAINMENU_CAPTURE_READY:<language>`.
- Artifact `hol-mainmenu-android-preview`.

- [ ] **Step 1: Write failing capture bootstrap tests**

Assert that the debug-only bootstrap:

- activates only for a Development Android build with the intent extra;
- seeds `AdsConsent=0`, sets the requested language, and waits for
  `MainMenuAuthoritativeVisuals.IsReady`;
- emits the exact ready marker;
- has no effect without the intent extra.

- [ ] **Step 2: Implement editor build entrypoint**

`MainMenuPreviewBuild.Build()` builds only `Assets/Scenes/MainMenu.unity` as a
Development x86_64 APK at `build/Android/HOL-mainmenu-debug.apk`. It must save
and restore temporary player settings in `finally`, must not change bundle
version/versionCode, and must reject non-Android editor targets.

- [ ] **Step 3: Implement capture bootstrap**

At very early execution order, on Android Development builds only, read the
intent extra, set `AdsConsent=0`, set EN/EL via `L10n`, and wait until Home is
ready and stable before logging the marker. No production path changes.

- [ ] **Step 4: Add PR-only emulator workflow**

The workflow:

- triggers only on PR/path changes and manual dispatch;
- uses no `production` environment and no typed release/deploy confirmation;
- builds with GameCI and `MainMenuPreviewBuild.Build`;
- boots an API 35 x86_64 emulator at 1080×1920;
- installs the exact debug APK;
- launches twice with `hol_capture_language=en` and `el`;
- waits for each ready marker, captures `adb exec-out screencap -p`;
- validates both PNGs are exactly 1080×1920;
- records APK and screenshot SHA-256 hashes;
- uploads APK, screenshots, hashes, package metadata, and logcat as
  `hol-mainmenu-android-preview`.

- [ ] **Step 5: Commit, push, and verify**

Run YAML syntax/static checks locally. Push and require the preview workflow to
produce the complete artifact without invoking any release workflow.

### Task 4: Final checkpoint verification and handoff

**Files:**
- Modify: `CHANGELOG.md`
- Modify: PR #32 description only after commits are pushed

- [ ] **Step 1: Final local gates**

Run:

```bash
node --test tools/test/*.test.mjs
mcs <all Unity/TMP stubs> $(git ls-files 'Assets/SCRIPT/*.cs' 'Assets/SCRIPT/**/*.cs')
git status --short
```

Confirm the diff contains no scene YAML, ProjectSettings, versionCode,
production workflow, CloudScript, provisioner, or release-config changes.

- [ ] **Step 2: Require CI authority**

Require green:

- Static integrity
- Provisioner
- Duel rules
- EditMode
- Exact visuals PlayMode
- Main Menu checkpoint PlayMode
- Android compile
- Main Menu Android Preview

- [ ] **Step 3: Download and inspect artifacts**

Download `hol-mainmenu-android-preview`. Verify:

- debug APK exists and installs;
- EN and EL PNGs are 1080×1920;
- screenshots show one Home root, correct copy, correct gloss alignment, no
  consent dialog, no dead/1V1 control, and no other screen;
- screenshot and APK checksums match the artifact manifest.

- [ ] **Step 4: Stop for owner approval**

Report the exact integration branch, SHA, changed files, CI links/results,
debug APK artifact link/path, EN screenshot, EL screenshot, and read-only
visual verdict. Keep PR #32 draft. Do not merge or start another screen.
