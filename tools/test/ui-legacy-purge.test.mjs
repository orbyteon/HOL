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
  "design/philosophy.md",
  "Assets/newdesign/Resources/design/background_deep.svg",
  "Assets/newdesign/Resources/design/panel_surface.svg",
  "Assets/newdesign/Resources/design/button_primary.svg",
  "Assets/newdesign/Resources/design/button_secondary.svg",
  "Assets/newdesign/Resources/mainmenu/mainmenu_bg_stairs_clouds.png",
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

test("retired theme source, doctrine, and generic surface assets stay deleted", () => {
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

test("temporary purge workflows and scripts are never production dependencies", () => {
  for (const phase of ["a", "b", "c", "d"]) {
    assert.equal(exists(`.github/workflows/one-shot-legacy-purge-phase-${phase}.yml`), false);
    assert.equal(exists(`.github/scripts/legacy_purge_${phase}.py`), false);
  }
});
