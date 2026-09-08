import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";

const read = path => fs.readFileSync(path, "utf8");
const owner = read("Assets/SCRIPT/Design/PvpDuelCartoonVisuals.cs");
const runtime = read("Assets/SCRIPT/RuntimeUI/PvpRuntimeUI.cs");
const controller = read("Assets/SCRIPT/PvP/PvpGameController.cs");

test("PvP constructs its final owner directly without a second reskin installer", () => {
  assert.match(runtime, /AddComponent<PvpDuelCartoonVisuals>/);
  assert.match(runtime, /visuals\.Build\(controller\)/);
  assert.doesNotMatch(runtime, /CreateText|CreateInputField|PvpPlayerCard|ResultVisualRoot/);
  assert.doesNotMatch(owner, /IEnumerator Start|DestroyImmediate|Destroy\(/);
  assert.match(owner, /DisallowMultipleComponent/);
});

test("Rematch keeps native mobile input while only live guessing owns a keypad", () => {
  assert.match(owner, /"RematchSecret", "rematch_prompt",\s*new Vector2\(0, 68\), new Vector2\(740, 80\), false\)/);
  assert.match(owner, /"GuessInput", "number_placeholder",\s*new Vector2\(0, 315\), new Vector2\(500, 125\), true\)/);
  assert.match(owner, /field\.shouldHideSoftKeyboard = keypadOnly;/);
  assert.match(owner, /field\.shouldHideMobileInput = keypadOnly;/);
  assert.match(owner, /TouchScreenKeyboardType.NumberPad/);
  assert.match(owner, /rematch\.onClick.AddListener\(pvp.OnRematchPressed\)/);
});

test("PvP profile binding shares canonical identity and never writes avatar preferences", () => {
  const privateRoom = read("Assets/SCRIPT/Design/PrivateRoomVisuals.cs");
  for (const source of [owner, privateRoom]) {
    assert.match(source, /PlayerProfileAvatarResolver.Resolve\(\)/);
    assert.match(source, /PlayerProfileAvatarFraming.Apply\(/);
    assert.match(source, /AddComponent<Mask>\(\).showMaskGraphic = false/);
    assert.doesNotMatch(source, /PlayerPrefs.Set|AvatarResource\s*=/);
  }
});

test("PvP result captions localize and opponent names refresh from every arriving snapshot", () => {
  assert.match(owner, /name \+ "Caption", captionKey/);
  assert.match(owner, /RuntimeUI.Localize\(label, key\)/);
  const arriving = controller.slice(controller.indexOf("void OnState("));
  assert.ok(arriving.indexOf("resultPresentation.SetOpponentName(opponentName)") < arriving.indexOf('if (s.phase == "done" && !matchOver)'));
  const result = read("Assets/SCRIPT/RuntimeUI/PvpResultPresentation.cs");
  assert.match(result, /SetOpponentName\(opponentName\)/);
});

test("PvP capture transport cannot exist in an Android or release player", () => {
  const fixture = read("Assets/SCRIPT/Design/PvpPresentationFixtureBackend.cs");
  assert.match(fixture, /^#if UNITY_EDITOR \|\| \(UNITY_STANDALONE_WIN && DEVELOPMENT_BUILD\)/);
  assert.doesNotMatch(fixture, /RuntimeInitializeOnLoadMethod|InitializeOnLoadMethod|PlayerPrefs|CloudScript|DuelRules/);
  assert.doesNotMatch(runtime + controller + owner, /PvpPresentationFixtureBackend/);
});

test("PvP uses committed modular production art with valid sprite metadata", () => {
  const resources = [...owner.matchAll(/(?:const string \w+Resource = |Add(?:Vector)?Sprite\([^;]*?)"((?:reference|phase2a|mainmenu|cartoon)\/[^"\n]+)"/g)].map(match => match[1]);
  assert.ok(resources.length >= 15);
  const vectors = new Set(["reference/board_vs_burst_exact", "reference/board_trophy_exact",
    "reference/board_rocket_exact", "cartoon/cartoon_speech_bubble"]);
  for (const resource of new Set(resources)) {
    const asset = `Assets/newdesign/Resources/${resource}.${vectors.has(resource) ? "svg" : "png"}`;
    const meta = read(`${asset}.meta`);
    assert.match(meta, /guid: [a-f0-9]{32}/, asset);
    if (vectors.has(resource)) {
      assert.match(read(asset), /<svg\b/, asset);
      assert.match(meta, /ScriptedImporter:/, asset);
      continue;
    }
    const png = fs.readFileSync(asset);
    assert.equal(png.subarray(1, 4).toString(), "PNG", asset);
    assert.match(meta, /spriteMode: 1/, asset);
    assert.match(meta, /enableMipMap: 0/, asset);
    assert.match(meta, /alphaIsTransparency: 1/, asset);
    if (resource.includes("_9s")) assert.doesNotMatch(meta, /spriteBorder: \{x: 0, y: 0, z: 0, w: 0\}/, asset);
  }
  assert.match(owner, /resource.Contains\("_9s"\) \? Image.Type.Sliced/);
  assert.match(owner, /AddComponent<Unity.VectorGraphics.SVGImage>\(\)/);
  assert.match(owner, /image.sprite.rect.width \/ size.x, image.sprite.rect.height \/ size.y/);
});
