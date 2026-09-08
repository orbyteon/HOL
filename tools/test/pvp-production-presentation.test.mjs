import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import { createHash } from "node:crypto";

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
  assert.match(owner, /"GuessInput", "number_placeholder",\s*new Vector2\(-13, 285\), new Vector2\(520, 140\), true\)/);
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
  const resources = [...owner.matchAll(/(?:const string \w+Resource = |Add(?:Vector)?Sprite\([^;]*?)"((?:reference|phase2a|mainmenu|solo)\/[^"\n]+)"/g)].map(match => match[1]);
  assert.ok(resources.length >= 30);
  for (const asset of ["solo_background_v1", "solo_player_card_shell_v1", "solo_opponent_card_shell_v1",
    "solo_interaction_board_v2", "solo_keypad_key_v1", "solo_primary_cta_v1",
    "solo_history_board_v1", "solo_opponent_speech_bubble_v2"]) {
    assert.ok(resources.includes(`solo/production/${asset}`), `Approved Solo asset required: ${asset}`);
  }
  const vectors = new Set(["reference/board_trophy_exact"]);
  // These four approved Solo imports already use mipmaps. Preserve their
  // exact committed metadata from the human-accepted cd2cd079 checkpoint;
  // this is not permission to enable mipmaps on any other UI import.
  const approvedSoloImports = new Map([
    ["solo/production/solo_background_v1", "014ae34ea58f6cf399a884b539104cc4e280095c"],
    ["solo/production/solo_interaction_board_v2", "e12c276e308e6d11062d34eb6ea4ea00776d1ab7"],
    ["solo/production/solo_opponent_speech_bubble_v2", "a066102c64b8a0cadb8dc65e6f64cbe978d19546"],
    ["solo/production/solo_vs_burst_v2", "5bdc98454994fb4faf1a9d5c42d0253cfe0a545d"],
  ]);
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
    if (approvedSoloImports.has(resource)) {
      // Git's text checkout filter may use CRLF on Windows, LF in CI.
      const bytes = Buffer.from(meta.replace(/\r\n/g, "\n"));
      const blob = createHash("sha1").update(`blob ${bytes.length}\0`).update(bytes).digest("hex");
      assert.equal(blob, approvedSoloImports.get(resource), `${asset} approved import identity`);
      assert.match(meta, /enableMipMap: 1/, asset);
    } else assert.match(meta, /enableMipMap: 0/, asset);
    assert.match(meta, /alphaIsTransparency: 1/, asset);
    if (resource.includes("_9s")) assert.doesNotMatch(meta, /spriteBorder: \{x: 0, y: 0, z: 0, w: 0\}/, asset);
  }
  assert.match(owner, /resource.Contains\("_9s"\) \? Image.Type.Sliced/);
  assert.match(owner, /AddComponent<Unity.VectorGraphics.SVGImage>\(\)/);
  assert.match(owner, /image.sprite.rect.width \/ size.x, image.sprite.rect.height \/ size.y/);
});
