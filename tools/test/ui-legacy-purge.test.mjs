import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = fileURLToPath(new URL("../../", import.meta.url));
const fromRoot = relative => path.join(root, relative);
const exists = relative => fs.existsSync(fromRoot(relative));

function filesUnder(relative, predicate = () => true) {
  const base = fromRoot(relative);
  const out = [];
  if (!fs.existsSync(base)) return out;
  const walk = current => {
    for (const entry of fs.readdirSync(current, { withFileTypes: true })) {
      const full = path.join(current, entry.name);
      if (entry.isDirectory()) walk(full);
      else if (predicate(full)) out.push(full);
    }
  };
  walk(base);
  return out;
}

function read(relative) {
  return fs.readFileSync(fromRoot(relative), "utf8");
}

function productionUiText() {
  const files = [
    ...filesUnder("Assets/SCRIPT", file => file.endsWith(".cs")),
    ...filesUnder("Assets/Scenes", file => file.endsWith(".unity")),
  ];
  return files.map(file => ({
    file: path.relative(root, file).replaceAll(path.sep, "/"),
    text: fs.readFileSync(file, "utf8"),
  }));
}

const retiredRuntimeSymbols = [
  "ConvergingLight",
  "DesignRuntimeWiring",
  "NeonFrame",
  "NumberDrift",
  "AttachmentReskin",
  "ExactReferenceVisuals",
  "FrameGeometry",
  "ConsumerTokens",
  "HOL Consumer First",
];

const retiredPaths = [
  "Assets/SCRIPT/Design/ConvergingLight.cs",
  "Assets/SCRIPT/Design/ConvergingLight.cs.meta",
  "Assets/SCRIPT/Design/DesignRuntimeWiring.cs",
  "Assets/SCRIPT/Design/DesignRuntimeWiring.cs.meta",
  "Assets/SCRIPT/Design/NeonFrame.cs",
  "Assets/SCRIPT/Design/NeonFrame.cs.meta",
  "Assets/SCRIPT/Design/NumberDrift.cs",
  "Assets/SCRIPT/Design/NumberDrift.cs.meta",
  "Assets/SCRIPT/Design/FrameGeometry.cs",
  "Assets/SCRIPT/Design/FrameGeometry.cs.meta",
  "Assets/SCRIPT/Design/ConsumerTokens.cs",
  "Assets/SCRIPT/Design/ConsumerTokens.cs.meta",
  "design/philosophy.md",
  "Assets/newdesign/Resources/design/background_deep.svg",
  "Assets/newdesign/Resources/design/panel_surface.svg",
  "Assets/newdesign/Resources/design/button_primary.svg",
  "Assets/newdesign/Resources/design/button_secondary.svg",
  "Assets/newdesign/Resources/mainmenu/mainmenu_bg_stairs_clouds.png",
  "Assets/newdesign/ads",
  "Assets/newdesign/avatars",
  "Assets/newdesign/badges",
  "Assets/newdesign/cosmetics",
  "Assets/newdesign/navigation",
  "Assets/newdesign/number-system",
  "Assets/newdesign/results",
  "Assets/newdesign/signals",
  "Assets/newdesign/design-tokens.json",
  "Assets/newdesign/asset-inventory.md",
  "Assets/newdesign/icon_lock.svg",
  "Assets/newdesign/icon_reaction.svg",
  "Assets/newdesign/icon_rewarded_ad.svg",
  "Assets/newdesign/icon_trophy.svg",
];

test("retired visual runtime symbols cannot return to production source or scenes", () => {
  const hits = [];
  for (const { file, text } of productionUiText()) {
    for (const symbol of retiredRuntimeSymbols) {
      if (text.includes(symbol)) hits.push(`${file}: ${symbol}`);
    }
  }
  assert.deepEqual(hits, []);
});

test("retired theme source, doctrine, generic surfaces, and source packs stay deleted", () => {
  for (const retired of retiredPaths) {
    assert.equal(exists(retired), false, retired);
    if (/\.(svg|png)$/.test(retired)) {
      assert.equal(exists(retired + ".meta"), false, retired + ".meta");
    }
  }

  const designDir = fromRoot("Assets/SCRIPT/Design");
  const reskinFiles = fs.readdirSync(designDir)
    .filter(name => name.startsWith("AttachmentReskin"));
  assert.deepEqual(reskinFiles, []);

  assert.equal(exists("Assets/SCRIPT/Design/HolUiStateColors.cs"), true);
  assert.equal(exists("Assets/SCRIPT/Design/HolUiStateColors.cs.meta"), true);
});

test("branding source folder retains only the ProjectSettings-referenced app icon", () => {
  const dir = fromRoot("Assets/newdesign/branding");
  assert.equal(fs.existsSync(dir), true, "referenced app-icon folder must remain");
  const files = fs.readdirSync(dir).sort();
  assert.deepEqual(files, ["hol_app_icon_exact.png", "hol_app_icon_exact.png.meta"]);

  const iconMeta = read("Assets/newdesign/branding/hol_app_icon_exact.png.meta");
  const guid = iconMeta.match(/^guid:\s*([0-9a-f]{32})$/m)?.[1];
  assert.ok(guid, "app icon meta must retain a valid GUID");
  assert.match(read("ProjectSettings/ProjectSettings.asset"), new RegExp(guid));
});

test("production screens cannot use the neutral rounded-rectangle fallback", () => {
  const hits = [];
  for (const { file, text } of productionUiText()) {
    if (file === "Assets/SCRIPT/RuntimeUI/RuntimeUI.cs") continue;
    if (text.includes("RuntimeUI.RoundedRectSprite")) hits.push(file);
  }
  assert.deepEqual(hits, []);
});

test("near-zero alpha sprite hiding cannot return", () => {
  const hits = [];
  const hiddenAlpha = /new\s+Color\([^\n;]*,\s*0\.00[12]f\s*\)/g;
  for (const { file, text } of productionUiText()) {
    if (hiddenAlpha.test(text)) hits.push(file);
    hiddenAlpha.lastIndex = 0;
  }
  assert.deepEqual(hits, []);
});

test("PvP does not rebuild the retired Private Room landing presentation", () => {
  const source = read("Assets/SCRIPT/RuntimeUI/PvpRuntimeUI.cs");
  assert.equal(source.includes("BuildPanelsLegacy"), false);
  assert.equal(source.includes("RetireLegacyPanel"), false);
  for (const retiredChild of [
    "FriendArt",
    "GirlArt",
    "DoorArt",
    "JoinTitle",
    "CodeCaption",
  ]) {
    assert.equal(source.includes(retiredChild), false, retiredChild);
  }
  assert.match(source, /BuildMatchPanel\(controller\)/);
  assert.match(source, /ReplacePrivateRoomPanels\(controller\)/);
});

test("Private Room remains owned by current production art and real room-code input", () => {
  const source = read("Assets/SCRIPT/Design/PrivateRoomVisuals.cs");
  assert.match(source, /private_room_step/);
  assert.match(source, /mainmenu\/mainmenu_icon_streak/);
  assert.match(source, /TMP_InputField\.ContentType\.Standard/);
  assert.match(source, /characterLimit\s*=\s*5|new Vector2\([^)]*\).*5/);
  assert.match(source, /Image\.Type\.Sliced/);
  assert.equal(source.includes("0.002f"), false);
});

test("current design docs cannot reinstate retired visual doctrine", () => {
  for (const relative of ["Assets/newdesign/README.md", "Assets/newdesign/screen-map.md"]) {
    const text = read(relative);
    assert.equal(text.includes("HOL Consumer First"), false, relative);
    assert.equal(text.includes("Converging Light"), false, relative);
    assert.equal(text.includes("DesignRuntimeWiring"), false, relative);
  }

  const agents = read("AGENTS.md");
  assert.match(agents, /Visual Ownership & Legacy Theme Purge Contract/);
  assert.match(agents, /Do not add or restore/);
});

test("temporary purge workflows and scripts are never production dependencies", () => {
  for (const phase of ["a", "b", "c", "d", "e"]) {
    assert.equal(exists(`.github/workflows/one-shot-legacy-purge-phase-${phase}.yml`), false);
    assert.equal(exists(`.github/scripts/legacy_purge_${phase}.py`), false);
  }
  assert.equal(exists(".github/scripts/fix_legacy_purge_e_refs.py"), false);
});
