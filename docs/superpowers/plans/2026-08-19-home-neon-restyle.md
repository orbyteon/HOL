# Neon Solo-First Home Restyle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the live MainMenu Home match the supplied solo-first neon reference while preserving every existing button callback, localization path, and safe-area guarantee.

**Architecture:** Keep `MainMenuHomeVisuals` as the sole presentation owner and keep `MenuManager` plus runtime wiring as the behavior owners. Replace only the Home background/layout construction with a procedural neon backdrop and portrait hierarchy; reuse the existing approved sprites and scene-owned controls. Extend the existing PlayMode contract so the visual hierarchy is verified through the real MainMenu scene.

**Tech Stack:** Unity 2022.3 UGUI, TextMesh Pro, runtime C# UI construction, reflection-only PlayMode tests, Node.js test suite.

## Global Constraints

- Every user-facing string goes through `L10n` with English and Greek entries.
- `MainMenuHomeVisuals` must remain the sole Home presentation owner.
- Existing `ButtonPlay`, `ButtonPvP`, `DailyHuntButton`, and `Buttonsettings` callbacks remain untouched.
- Decorative UI graphics must not raycast; only existing interactive controls may receive input.
- UI uses the 1080x1920 reference coordinate system and clamps to Android safe area.
- Gold remains reserved for the primary solo CTA; secondary cards use cyan and magenta.
- Do not add Store/Profile screens, gameplay changes, backend changes, or new dependencies.
- Keep `CHANGELOG.md` Unreleased current.

---

### Task 1: Establish the clean baseline

**Files:**
- Read: `Assets/SCRIPT/Design/MainMenuHomeVisuals.cs`
- Read: `Assets/Tests/PlayMode/MainMenuHomeVisualsPlayModeTests.cs`
- Read: `tools/test/mainmenu-assets.test.mjs`

**Interfaces:**
- Consumes: `origin/main` at `663cdbc`.
- Produces: A recorded green baseline before the Home-specific red test.

- [ ] **Step 1: Confirm the feature worktree is clean and on the required branch**

Run:

```bash
git status --short --branch
git log -1 --oneline
```

Expected: branch `cursor/home-neon-restyle-291c`, no modified files, base commit
`663cdbc`.

- [ ] **Step 2: Run the existing Node test suite**

Run:

```bash
node --test tools/test/*.test.mjs
```

Expected: all existing tests pass before Home-specific edits.

- [ ] **Step 3: Commit the approved design documentation**

Run:

```bash
git add docs/superpowers/specs/2026-08-19-home-neon-design.md
git commit -m "docs: define neon solo-first Home design"
```

### Task 2: Add the failing Home hierarchy contract

**Files:**
- Modify: `Assets/Tests/PlayMode/MainMenuHomeVisualsPlayModeTests.cs`

**Interfaces:**
- Consumes: The real `MainMenu` scene and `MainMenuHomeVisuals` runtime installer.
- Produces: Assertions for `HomeNeonBackdrop`, portrait mode hierarchy, and
  secondary-card geometry.

- [ ] **Step 1: Add assertions for the procedural backdrop**

After the existing Home root assertions, require:

```csharp
var backdrop = Find(canvas.transform, "HomeNeonBackdrop");
Assert.That(backdrop, Is.Not.Null);
Assert.That(backdrop.GetComponent<Image>().raycastTarget, Is.False);
```

Require the Home background sprite name not to contain `stairs_clouds`.

- [ ] **Step 2: Add assertions for primary/secondary hierarchy**

Read the four existing controls as `RectTransform`s and assert:

```csharp
Assert.That(play.sizeDelta.x, Is.GreaterThan(pvp.sizeDelta.x));
Assert.That(play.sizeDelta.x, Is.GreaterThan(hunt.sizeDelta.x));
Assert.That(Mathf.Abs(pvp.anchoredPosition.y - hunt.anchoredPosition.y),
    Is.LessThan(0.01f));
Assert.That(pvp.anchoredPosition.x, Is.LessThan(hunt.anchoredPosition.x));
```

Assert that their vertical extents remain inside the safe reference area and
that the primary button still uses the gold sprite while the secondary
buttons use blue and magenta sprites.

- [ ] **Step 3: Run the focused PlayMode test and verify the expected RED**

Run the Unity PlayMode test through the repository's existing GameCI command
or, when Unity is unavailable locally, run the repository's static test subset
and record that the new contract is expected to fail against the old layout.

Expected failure: `HomeNeonBackdrop` is missing or the existing stairs/clouds
background/stacked button geometry violates the new contract.

### Task 3: Implement the procedural neon Home

**Files:**
- Modify: `Assets/SCRIPT/Design/MainMenuHomeVisuals.cs`

**Interfaces:**
- Consumes: Existing `ConvergingLight`, `RuntimeUI`, `L10n`, and approved Home
  sprites.
- Produces: A runtime Home whose visual hierarchy matches the reference while
  retaining the existing scene-owned controls.

- [ ] **Step 1: Replace the bitmap background with the shared deep-indigo surface**

Use `ConvergingLight.DepthGradientSprite` for `HomeBackground`, retain the
existing transparent star/lightning/number decorations where they support the
reference, and add a full-screen `HomeNeonBackdrop` child with no raycast.

- [ ] **Step 2: Add non-interactive neon seams**

Create thin `Image` strips under `HomeNeonBackdrop` using the existing rounded
rectangle sprite, cyan/magenta colors with low alpha, and fixed rotations. Add
the existing number-field helper at low alpha. Keep all decorative children
behind the logo and characters.

- [ ] **Step 3: Recompose the hero group**

Keep the approved HOL logo, boy, girl, mascot 6, and mascot 7 resources. Place
the logo high center, the boy/girl together beneath it, and the mascots on the
lower left/right edges, with safe-area clamping and non-raycast images.

- [ ] **Step 4: Recompose the mode controls**

Keep the existing control objects and listeners. Place `ButtonPlay` as the
large gold CTA. Place `ButtonPvP` and `DailyHuntButton` in one compact
side-by-side row, with their existing localization keys and icon children.
Keep button labels live through `LocalizedText`.

- [ ] **Step 5: Reposition chip, gear, and tip**

Keep the player chip and settings button at the top corners. Move the tip
below the mode row, keep its localized title/body, and ensure it cannot block
input.

### Task 4: Update the changelog and clean up

**Files:**
- Modify: `CHANGELOG.md`

**Interfaces:**
- Consumes: The implemented Home hierarchy and test contract.
- Produces: Release-facing documentation of the visual behavior change.

- [ ] **Step 1: Add an Unreleased Changed entry**

Document the solo-first neon Home hierarchy, the procedural backdrop, and the
fact that existing controls/callbacks remain unchanged.

- [ ] **Step 2: Run formatting/static checks**

Run:

```bash
node --check playfab/cloudscript.js
node --test tools/test/*.test.mjs
git diff --check
```

Expected: exit code 0 for every command.

### Task 5: Verify and submit

**Files:**
- Read: `Assets/SCRIPT/Design/MainMenuHomeVisuals.cs`
- Read: `Assets/Tests/PlayMode/MainMenuHomeVisualsPlayModeTests.cs`
- Read: `CHANGELOG.md`

**Interfaces:**
- Consumes: The complete feature branch.
- Produces: A pushed branch and draft PR for CI validation.

- [ ] **Step 1: Inspect the full diff**

Run:

```bash
git diff --stat
git diff --check
git status --short
```

Expected: only the Home owner, Home PlayMode contract, changelog, and
committed design/plan docs are changed.

- [ ] **Step 2: Commit the implementation**

Run:

```bash
git add Assets/SCRIPT/Design/MainMenuHomeVisuals.cs \
  Assets/Tests/PlayMode/MainMenuHomeVisualsPlayModeTests.cs \
  CHANGELOG.md \
  docs/superpowers/plans/2026-08-19-home-neon-restyle.md
git commit -m "feat: restyle Home as solo-first neon menu"
```

- [ ] **Step 3: Push and create/update the draft PR**

Run:

```bash
git push -u origin cursor/home-neon-restyle-291c
```

Create a draft PR targeting `main` with the Home redesign summary and the
verification results. If tests require another fix, commit and push that fix,
then update the same PR before handoff.
