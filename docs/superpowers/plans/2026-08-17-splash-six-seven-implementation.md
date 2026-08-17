# HOL Splash 6–7 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use
> superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use
> checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build one authoritative, safe-area-aware HOL Splash screen with
mascot 6 on the left and mascot 7 on the right, while preserving the existing
2.5-second automatic transition and tap-to-skip behavior.

**Architecture:** The scene-authored `SplashDesign` becomes the sole
presentation owner on the existing Splash Canvas. It builds one runtime visual
root from shared reference art plus two Splash-specific PNGs; global Main Menu
presentation installers skip `SplashScene`. A Development-only Android capture
seam holds the Splash for deterministic evidence but is inert in normal builds.

**Tech Stack:** Unity 2022.3.62f3, UGUI, `Resources.Load<Sprite>`, NUnit
EditMode/PlayMode reflection tests, PNG assets, GameCI, Android API 35 emulator,
ARM64 Android compile artifact.

## Global Constraints

- Work only on `cursor/splash-six-seven-291c`, based on fresh `main`
  `6bd79b30a01044f87d9155bf2bcc22f2c3293d6d`.
- Splash screen only. Do not change Main Menu, Profile, Settings, Store, PvP,
  gameplay, backend, ads, or release behavior.
- Preserve `SplashLoader.waitTime == 2.5f`, automatic `MainMenu` transition,
  whole-screen tap/click skip, and the duplicate-load guard.
- Mascot 6 is left; mascot 7 is right. No `six seven` text or external cultural
  reference appears in the product.
- Do not create a Button or Canvas. Use the existing Splash Canvas and
  `SplashLoader`.
- Do not add async loading, percentages, login/network/update state, profile,
  score, currency, retry, error, or other nonexistent functionality.
- Every new `.cs`, folder, PNG, and text asset under `Assets/` has a committed
  `.meta` with a unique GUID.
- EditMode and PlayMode tests reach game types through reflection only.
- Do not change `Assets/Scenes/*.unity`, `ProjectSettings/ProjectSettings.asset`,
  bundle version/versionCode, production workflows, signing, production config,
  PlayFab, provisioner, minVersion, or Play Console.
- No merge, deploy, release, or Google Play upload.
- Stop after green CI, Android screenshot, ARM64 debug APK, and owner review.

---

### Task 1: Materialize the authoritative Splash art contract

**Files:**

- Create: `Assets/Tests/EditMode/SplashProductionAssetsTests.cs`
- Create: `Assets/Tests/EditMode/SplashProductionAssetsTests.cs.meta`
- Create: `tools/test/splash-assets.test.mjs`
- Create: `Assets/newdesign/Resources/reference/mascot_6_exact.png`
- Create: `Assets/newdesign/Resources/reference/mascot_6_exact.png.meta`
- Create: `Assets/newdesign/Resources/splash.meta`
- Create: `Assets/newdesign/Resources/splash/README.md`
- Create: `Assets/newdesign/Resources/splash/README.md.meta`
- Create: `Assets/newdesign/Resources/splash/splash_bg_neon_arcade.png`
- Create: `Assets/newdesign/Resources/splash/splash_bg_neon_arcade.png.meta`
- Create: `Assets/newdesign/Resources/splash/splash_logo_glow.png`
- Create: `Assets/newdesign/Resources/splash/splash_logo_glow.png.meta`

**Interfaces:**

- Produces `Resources/reference/mascot_6_exact`.
- Produces `Resources/splash/splash_bg_neon_arcade`.
- Produces `Resources/splash/splash_logo_glow`.
- The approved number-six payload SHA-256 is
  `067beafc207aea302e0993a3bacdb2b69478429aa3685f275bb6705bd902ac4b`.

- [ ] **Step 1: Write the failing EditMode resource contract**

Create a reflection-free asset test (the EditMode asmdef does not reference
Assembly-CSharp):

```csharp
using NUnit.Framework;
using UnityEngine;

public sealed class SplashProductionAssetsTests
{
    [TestCase("reference/hol_logo_exact")]
    [TestCase("reference/mascot_6_exact")]
    [TestCase("reference/mascot_7_exact")]
    [TestCase("splash/splash_bg_neon_arcade")]
    [TestCase("splash/splash_logo_glow")]
    public void SplashSpriteLoads(string path)
    {
        Assert.That(Resources.Load<Sprite>(path), Is.Not.Null,
            "Missing Resources/" + path);
    }

    [Test]
    public void BackgroundIsNativePortraitResolution()
    {
        Sprite sprite = Resources.Load<Sprite>("splash/splash_bg_neon_arcade");
        Assert.That(sprite.texture.width, Is.EqualTo(1080));
        Assert.That(sprite.texture.height, Is.EqualTo(1920));
    }

    [Test]
    public void MascotSixIsSquareReferenceArt()
    {
        Sprite sprite = Resources.Load<Sprite>("reference/mascot_6_exact");
        Assert.That(sprite.texture.width, Is.EqualTo(1024));
        Assert.That(sprite.texture.height, Is.EqualTo(1024));
    }
}
```

- [ ] **Step 2: Write the failing Node PNG/source contract**

`tools/test/splash-assets.test.mjs` uses only Node built-ins:

```js
import assert from "node:assert/strict";
import crypto from "node:crypto";
import fs from "node:fs";
import test from "node:test";

const root = new URL("../../", import.meta.url);
const read = relative => fs.readFileSync(new URL(relative, root));
const dimensions = png => ({
  width: png.readUInt32BE(16),
  height: png.readUInt32BE(20),
});

test("approved number six stays byte-exact", () => {
  const png = read("Assets/newdesign/Resources/reference/mascot_6_exact.png");
  assert.equal(crypto.createHash("sha256").update(png).digest("hex"),
    "067beafc207aea302e0993a3bacdb2b69478429aa3685f275bb6705bd902ac4b");
});

test("Splash background is exact Android portrait size", () => {
  assert.deepEqual(
    dimensions(read("Assets/newdesign/Resources/splash/splash_bg_neon_arcade.png")),
    { width: 1080, height: 1920 });
});

test("every Splash asset has a Unity meta", () => {
  for (const path of [
    "Assets/newdesign/Resources/reference/mascot_6_exact.png",
    "Assets/newdesign/Resources/splash/splash_bg_neon_arcade.png",
    "Assets/newdesign/Resources/splash/splash_logo_glow.png",
  ]) assert.equal(fs.existsSync(new URL(path + ".meta", root)), true, path);
});
```

- [ ] **Step 3: Commit and push RED**

Commit only the tests and metas, then push:

```bash
git add Assets/Tests/EditMode/SplashProductionAssetsTests.cs*
git add tools/test/splash-assets.test.mjs
git commit -m "test: define authoritative Splash art contract"
git push -u origin cursor/splash-six-seven-291c
```

Expected RED: mascot 6, Splash background, and Splash glow are absent.

- [ ] **Step 4: Materialize mascot 6 without regenerating it**

Copy only the PNG payload from approved PR #31 commit `9f3f701`:

```bash
git show 9f3f701:Assets/newdesign/Resources/avatars/numbers/avatar_number_06.png \
  > Assets/newdesign/Resources/reference/mascot_6_exact.png
sha256sum Assets/newdesign/Resources/reference/mascot_6_exact.png
```

Expected hash:

```text
067beafc207aea302e0993a3bacdb2b69478429aa3685f275bb6705bd902ac4b
```

Create a new Sprite `.meta` with a newly generated 32-hex GUID. Do not copy the
avatar asset's GUID.

- [ ] **Step 5: Generate the dedicated text-free background**

Use the image generation tool once per candidate with aspect ratio `9:16` and
this exact prompt:

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

Reject candidates containing accidental text, numbers, characters, fake UI, or
central clutter. Resize/crop the accepted source to exactly 1080×1920 with
high-quality Lanczos filtering and save it as
`splash_bg_neon_arcade.png`.

- [ ] **Step 6: Generate the transparent logo glow deterministically**

Use a temporary Pillow script outside the repository to create a transparent
960×620 RGBA image with overlapping blurred magenta-left and cyan-right
ellipses. The center alpha must remain below 160 so logo details stay readable.
Save as `splash_logo_glow.png`; no text or logo pixels are baked into it.

- [ ] **Step 7: Add importers and provenance**

Create `Resources/splash/README.md` recording:

- the exact background prompt;
- mascot 6 source commit/hash;
- no baked text/UI;
- the resource paths and intended dimensions.

Use Sprite/Single importer metas, PPU 100, mipmaps off, alpha transparency on
for glow/mascot, clamp, and max texture size 2048.

- [ ] **Step 8: Verify GREEN, commit, and push**

Run:

```bash
node --test tools/test/*.test.mjs
git diff --check
```

Require the focused Unity EditMode asset test in CI. Visually inspect each PNG
at full size and against both light/dark checkerboards. Commit and push:

```bash
git add Assets/newdesign/Resources Assets/Tests/EditMode tools/test
git commit -m "assets: add authoritative six-seven Splash art"
git push -u origin cursor/splash-six-seven-291c
```

---

### Task 2: Make SplashDesign the sole safe-area-aware owner

**Files:**

- Modify: `Assets/SCRIPT/Design/SplashDesign.cs`
- Modify: `Assets/SCRIPT/Design/ExactReferenceVisuals.cs`
- Modify: `Assets/SCRIPT/Design/AttachmentReskinVisuals.cs`
- Modify: `Assets/SCRIPT/Design/AttachmentReskinPolish.cs`
- Modify: `Assets/SCRIPT/Design/AttachmentReskinCanvasBindings.cs`
- Create: `Assets/Tests/PlayMode/SplashAuthoritativeVisualsPlayModeTests.cs`
- Create: `Assets/Tests/PlayMode/SplashAuthoritativeVisualsPlayModeTests.cs.meta`
- Modify: `Assets/Tests/EditMode/ExactReferenceAssetsTests.cs`
- Modify: `Assets/Tests/PlayMode/ExactReferenceVisualsPlayModeTests.cs`

**Interfaces:**

- `SplashDesign.IsReady`: hierarchy and art are bound.
- `SplashDesign.IsSettled`: entrance animation completed.
- `SplashDesign.NormalizedSafeArea(Rect safe, float width, float height)`:
  pure normalized-anchor conversion, exercised through reflection.
- Existing `SplashLoader` remains the sole navigation/tap owner.

- [ ] **Step 1: Write the failing ownership/layout PlayMode tests**

On the real `SplashScene`, after one frame, assert through reflection:

```csharp
Assert.That(FindInScene(scene, RuntimeType("SplashDesign")), Is.Not.Null);
Assert.That(FindInScene(scene, RuntimeType("ExactReferenceVisuals")), Is.Null);
Assert.That(FindInScene(scene, RuntimeType("AttachmentReskinVisuals")), Is.Null);
Assert.That(FindInScene(scene, RuntimeType("AttachmentReskinPolish")), Is.Null);
Assert.That(FindInScene(scene, RuntimeType("AttachmentReskinCanvasBindings")), Is.Null);
```

Then assert:

- one root screen-space Canvas, no new Canvas;
- exactly one direct `SplashVisualRoot`;
- exact hierarchy/names from the design spec;
- no `Button` in the scene;
- `SplashMascotSix.anchoredPosition.x < 0`;
- `SplashMascotSeven.anchoredPosition.x > 0`;
- logo/mascot Images have `preserveAspect == true`;
- every SplashVisualRoot Image has `raycastTarget == false`;
- legacy `Panel` Image is disabled and legacy `Image` GameObject inactive;
- progress track is 480×8;
- `IsReady == true`.

Use a table-driven exact reference-layout assertion for the five design rects.

- [ ] **Step 2: Write the failing safe-area conversion tests**

Invoke `NormalizedSafeArea` through reflection:

```csharp
[TestCase(0, 0, 1080, 1920, 0, 0, 1, 1)]
[TestCase(0, 80, 1080, 1760, 0, 0.0416667f, 1, 0.9166667f)]
[TestCase(60, 0, 1020, 1920, 0.0555556f, 0, 0.9444444f, 1)]
```

Assert the returned `Rect` values within `0.0001f` and zero-width/height inputs
fall back to a full normalized rect.

- [ ] **Step 3: Push RED**

Commit only test changes and push. Expected failures: competing presenters
still install, `SplashVisualRoot` and mascot 6 are absent, and safe-area API is
missing.

- [ ] **Step 4: Prevent competing installers on Splash**

At the top of each installer path, before selecting a Canvas:

```csharp
if (scene.name == "SplashScene") return;
```

Apply this to `ExactReferenceVisuals.InstallForScene` and the
`OnSceneLoaded` methods in all three `AttachmentReskin*` classes. Do not alter
their MainMenu logic.

- [ ] **Step 5: Rebuild SplashDesign as the sole owner**

Keep the existing class/file and add `[DefaultExecutionOrder(-2000)]`.
`Awake()`:

1. find the scene-authored root screen-space Canvas;
2. hide legacy `Panel` Image and legacy `Image` GameObject;
3. load the five approved sprites;
4. create/reuse one `SplashVisualRoot`;
5. create full-bleed `SplashBackground`;
6. create safe anchors from `Screen.safeArea`;
7. create the exact logo, 6, 7, and progress hierarchy;
8. set every decoration non-raycast;
9. initialize `CanvasGroup`/scale animation state;
10. set `IsReady`.

Implement the safe conversion exactly:

```csharp
static Rect NormalizedSafeArea(Rect safe, float width, float height)
{
    if (width <= 0f || height <= 0f) return new Rect(0f, 0f, 1f, 1f);
    return new Rect(
        Mathf.Clamp01(safe.xMin / width),
        Mathf.Clamp01(safe.yMin / height),
        Mathf.Clamp01(safe.width / width),
        Mathf.Clamp01(safe.height / height));
}
```

Use `Update()` only for unscaled entrance/breathe/progress presentation.
`SplashDesign` must not call `SceneManager`, inspect input, or add callbacks.

- [ ] **Step 6: Preserve MainMenu transition tests**

Update the old exact-visuals test only where it expected Exact/Attachment
components on Splash. It must now assert they are absent, invoke the existing
`SplashLoader.LoadMenu`, and then prove Exact/Attachment components still
install on MainMenu.

Replace the obsolete EditMode `LayoutSplash` expectation with a test that
`ExactReferenceVisuals.InstallForScene` skips a scene named `SplashScene`.

- [ ] **Step 7: Verify GREEN and compile all runtime scripts**

After the last C# edit:

```bash
mcs -target:library -langversion:latest -define:UNITY_EDITOR \
  -out:/tmp/splash-unity-stubs/HOL-SCRIPT.dll \
  /tmp/splash-unity-stubs/UnityStubs.cs \
  $(git ls-files 'Assets/SCRIPT/*.cs' 'Assets/SCRIPT/**/*.cs')
```

Require zero errors. Run PlayMode/EditMode through CI, commit, and push:

```bash
git add Assets/SCRIPT/Design Assets/Tests
git commit -m "feat: own authoritative six-seven Splash presentation"
git push -u origin cursor/splash-six-seven-291c
```

---

### Task 3: Preserve navigation and add a Development-only capture seam

**Files:**

- Modify: `Assets/SCRIPT/SplashLoader.cs`
- Create: `Assets/SCRIPT/Design/SplashCaptureBootstrap.cs`
- Create: `Assets/SCRIPT/Design/SplashCaptureBootstrap.cs.meta`
- Create: `Assets/Tests/PlayMode/SplashCapturePlayModeTests.cs`
- Create: `Assets/Tests/PlayMode/SplashCapturePlayModeTests.cs.meta`
- Create: `Assets/Editor/SplashPreviewBuild.cs`
- Create: `Assets/Editor/SplashPreviewBuild.cs.meta`
- Create: `.github/workflows/splash-android-preview.yml`

**Interfaces:**

- Intent extra: `hol_capture_screen=splash`.
- Marker: `HOL_SPLASH_CAPTURE_READY`.
- `SplashCaptureBootstrap.ShouldCapture(bool android, bool development,
  string requestedScreen)`.
- Normal timeout remains 2.5s; capture timeout is 30s.
- Artifact: `hol-splash-android-preview`.

- [ ] **Step 1: Write failing bootstrap/timing tests**

Through reflection assert:

```text
ShouldCapture(true, true, "splash")  -> true
ShouldCapture(false, true, "splash") -> false
ShouldCapture(true, false, "splash") -> false
ShouldCapture(true, true, null)      -> false
ShouldCapture(true, true, "menu")    -> false
```

On a real Splash scene assert:

- normal `SplashLoader.waitTime` remains `2.5f`;
- capture mode produces an effective timeout of `30f`;
- marker is emitted only after `SplashDesign.IsSettled`;
- no extra has no PlayerPrefs, language, hierarchy, or timing side effect;
- invoking `LoadMenu` twice changes scene only once.

- [ ] **Step 2: Commit and push RED**

Expected failures: capture bootstrap/API do not exist and SplashLoader has no
debug timeout seam.

- [ ] **Step 3: Implement the runtime capture seam**

`SplashCaptureBootstrap`:

- installs before scene load;
- on Android Development only, reads the current Activity intent string extra
  `hol_capture_screen`;
- exposes `CaptureRequested`;
- adds one nonvisual bootstrap component to a Splash root only when requested;
- waits for `SplashDesign.IsSettled`;
- logs `HOL_SPLASH_CAPTURE_READY` exactly once.

`SplashLoader.Start()` selects:

```csharp
float timeout = SplashCaptureBootstrap.CaptureRequested ? 30f : waitTime;
Invoke(nameof(LoadMenu), timeout);
```

No non-Development path reads Android Java APIs or changes the timeout.

- [ ] **Step 4: Implement the editor preview build**

`SplashPreviewBuild.Build()`:

- rejects a non-Android editor target;
- builds `SplashScene` and `MainMenu` as a Development x86_64 APK;
- output: `build/Android/HOL-splash-debug.apk`;
- saves/restores scripting backend, target architectures, app-bundle mode, and
  custom-keystore mode in `finally`;
- never assigns bundleVersion or Android bundleVersionCode.

- [ ] **Step 5: Add the PR-only emulator workflow**

`.github/workflows/splash-android-preview.yml`:

- triggers on PR changes to Splash files and manual dispatch;
- uses no `production` environment or typed confirmation;
- builds with pinned GameCI using `SplashPreviewBuild.Build`;
- boots API 35 x86_64 at 1080×1920;
- installs the exact APK;
- launches with `--es hol_capture_screen splash`;
- waits for `HOL_SPLASH_CAPTURE_READY`;
- captures `splash.png`;
- validates PNG IHDR is 1080×1920;
- hashes APK and screenshot;
- records `aapt dump badging`, physical size/density, package metadata, and
  logcat;
- uploads everything as `hol-splash-android-preview`.

Use the same pinned action SHAs already present in repository workflows.

- [ ] **Step 6: Verify, commit, and push**

Run YAML parsing/static contract checks, all-`Assets/SCRIPT` mcs compile after
the final edit, and `git diff --check`. Commit and push.

---

### Task 4: Final Splash checkpoint and handoff

**Files:**

- Modify: `CHANGELOG.md`
- Modify: PR #34 description after all commits are pushed

**Interfaces:**

- Produces no new runtime API.
- Delivers the exact SHA, CI links, ARM64 APK, screenshot, and hashes.

- [ ] **Step 1: Record the Unreleased Splash change**

Document the new authoritative 6–7 Splash, preserved 2.5s/tap behavior, safe
area, and Development-only capture path. Do not mention future pages as shipped.

- [ ] **Step 2: Run final local gates**

```bash
node --test tools/test/*.test.mjs
mcs -target:library -langversion:latest -define:UNITY_EDITOR \
  -out:/tmp/splash-unity-stubs/HOL-SCRIPT.dll \
  /tmp/splash-unity-stubs/UnityStubs.cs \
  $(git ls-files 'Assets/SCRIPT/*.cs' 'Assets/SCRIPT/**/*.cs')
git diff --check
git status --short
```

Confirm the branch does not change MainMenu scene/assets, ProjectSettings,
versionCode, production workflows, backend, ads, release config, or any later
page.

- [ ] **Step 3: Require CI authority**

Require green:

- Static integrity
- Provisioner tests
- Duel rule tests
- EditMode tests
- Exact visuals / Splash PlayMode tests
- Android compile
- Splash Android Preview

- [ ] **Step 4: Inspect the exact artifacts**

Download `hol-splash-android-preview` and generic `hol-android-debug`.

Verify:

- screenshot 1080×1920;
- 6 appears left and 7 right;
- logo, mascots, and gold line are inside safe content bounds;
- no duplicate backdrop/logo/progress line;
- no text, fake status, button, profile, currency, or 1V1 control;
- no consent/system overlay;
- preview checksums match;
- generic debug APK contains `arm64-v8a` and records its SHA-256;
- both runs target the exact final commit SHA.

- [ ] **Step 5: Final review and stop**

Run a whole-branch code review and a visual review. Fix Critical/Important
findings, rerun covering tests, push, and require final CI green.

Report:

- branch and SHA;
- changed files;
- CI links/results;
- ARM64 debug APK artifact link and SHA-256;
- Android screenshot and SHA-256;
- visual verdict.

Keep PR #34 unmerged. Do not begin Main Menu, Profile, Settings, Store, or PvP.
