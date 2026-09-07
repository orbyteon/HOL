import assert from "node:assert/strict";
import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = new URL("../../", import.meta.url);
const read = relative => fs.readFileSync(new URL(relative, root));
const exists = relative => fs.existsSync(new URL(relative, root));
const dimensions = png => ({
  width: png.readUInt32BE(16),
  height: png.readUInt32BE(20),
});
const colorType = png => png[25];
const rootPath = fileURLToPath(root);

const filesBelow = directory => {
  const found = [];
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const full = path.join(directory, entry.name);
    if (entry.isDirectory()) found.push(...filesBelow(full));
    else found.push(full);
  }
  return found;
};

const approvedBackground =
  "Assets/newdesign/Resources/solo/production/solo_background_v1.png";
const rejectedCloudBackground =
  "Assets/newdesign/Resources/mainmenu/mainmenu_bg_stairs_clouds.png";

const homeMainmenuPngs = [
  "mainmenu_deco_stars.png",
  "mainmenu_deco_lightning.png",
  "mainmenu_deco_confetti.png",
  "mainmenu_deco_numbers.png",
  "mainmenu_cta_gold_9s.png",
  "mainmenu_cta_blue_9s.png",
  "mainmenu_cta_magenta_9s.png",
  "mainmenu_player_chip_frame_9s.png",
  "mainmenu_tip_frame_9s.png",
  "mainmenu_gear_glossy.png",
  "mainmenu_icon_solo.png",
  "mainmenu_icon_private_room.png",
  "mainmenu_icon_daily_hunt.png",
  "mainmenu_icon_streak.png",
  "mainmenu_icon_tip_bulb.png",
];

const sharedPngs = [
  "reference/hol_logo_exact.png",
  "reference/mascot_6_exact.png",
  "reference/mascot_7_exact.png",
  "reference/char_boy_exact.png",
  "reference/char_girl_exact.png",
];

test("approved number six stays byte-exact", () => {
  const png = read("Assets/newdesign/Resources/reference/mascot_6_exact.png");
  assert.equal(crypto.createHash("sha256").update(png).digest("hex"),
    "067beafc207aea302e0993a3bacdb2b69478429aa3685f275bb6705bd902ac4b");
});

test("Home reuses approved Solo background with canonical 941x1672 portrait geometry", () => {
  const png = read(approvedBackground);
  const { width, height } = dimensions(png);
  assert.equal(png.subarray(0, 8).toString("hex"), "89504e470d0a1a0a",
    "Home background must be a PNG");
  assert.equal(png[24], 8, "Home background must stay 8-bit");
  // 04-solo-duel-approved-contract.md fixes the source at 941x1672.
  assert.deepEqual({ width, height }, { width: 941, height: 1672 });
  assert.ok(width >= 900,
    `Home background width must stay production-resolution; got ${width}`);
  assert.ok(height >= 1600,
    `Home background height must stay production-resolution; got ${height}`);
  assert.ok(Math.abs(width / height - 9 / 16) <= 0.002,
    `Home background must stay approximately 9:16; got ${width}x${height}`);
  assert.ok(colorType(png) === 2 || colorType(png) === 6,
    "Home background must be RGB or RGBA");
  assert.equal(exists(approvedBackground + ".meta"), true,
    approvedBackground + ".meta");
});

test("rejected stairs/clouds Home background is deleted", () => {
  assert.equal(exists(rejectedCloudBackground), false, rejectedCloudBackground);
  assert.equal(exists(rejectedCloudBackground + ".meta"), false,
    rejectedCloudBackground + ".meta");
});

test("every current cartoon Home asset has a Unity meta", () => {
  for (const filename of homeMainmenuPngs) {
    const path = "Assets/newdesign/Resources/mainmenu/" + filename;
    assert.equal(exists(path), true, path);
    assert.equal(exists(path + ".meta"), true, path + ".meta");
  }
  for (const path of sharedPngs) {
    const full = "Assets/newdesign/Resources/" + path;
    assert.equal(exists(full), true, full);
    assert.equal(exists(full + ".meta"), true, full + ".meta");
  }
});

test("cartoon Home art stays isolated from Splash resources", () => {
  for (const filename of homeMainmenuPngs) {
    const path = "Assets/newdesign/Resources/splash/" + filename;
    assert.equal(exists(path), false, path);
  }
  assert.equal(
    exists("Assets/newdesign/Resources/splash/char_boy_exact.png"),
    false);
  assert.equal(
    exists("Assets/newdesign/Resources/mainmenu/splash_bg_stairs_clouds.png"),
    false);
});

test("rejected neon arcade Home background is absent", () => {
  for (const suffix of ["", ".meta"]) {
    const path = "Assets/newdesign/Resources/mainmenu/mainmenu_bg_night_arcade.png"
      + suffix;
    assert.equal(exists(path), false, path);
  }
});

test("Home does not ship a 1v1 CTA", () => {
  for (const name of [
    "mainmenu_cta_violet_9s.png",
    "mainmenu_icon_1v1.png",
  ]) {
    const path = "Assets/newdesign/Resources/mainmenu/" + name;
    assert.equal(exists(path), false, path);
    assert.equal(exists(path + ".meta"), false, path + ".meta");
  }
});

test("Home chrome PNGs stay text-free RGBA overlays or 9-slices", () => {
  for (const filename of homeMainmenuPngs) {
    const png = read("Assets/newdesign/Resources/mainmenu/" + filename);
    assert.equal(colorType(png), 6, filename + " must be RGBA");
  }
});

test("Home owner uses approved Solo background and cannot restore retired backgrounds", () => {
  const owner = "Assets/SCRIPT/Design/MainMenuHomeVisuals.cs";
  const source = read(owner).toString("utf8");
  assert.match(source,
    /const string BackgroundResource\s*=\s*"solo\/production\/solo_background_v1";/);
  assert.equal(source.includes("settings/hol_settings_bg_r1"), false);
  assert.equal(source.includes("phase2a/hol_neon_reference_bg_r3"), false);
  assert.equal(source.includes("mainmenu_bg_stairs_clouds"), false);
});

test("Home profile avatar reuses the sole Onboarding key and catalog mapping", () => {
  const scriptRoot = path.join(rootPath, "Assets", "SCRIPT");
  const scripts = filesBelow(scriptRoot).filter(file => file.endsWith(".cs"));
  const keyOwners = scripts
    .filter(file => fs.readFileSync(file, "utf8").includes(
      '"HOL.Onboarding.Avatar"'))
    .map(file => path.relative(scriptRoot, file).replaceAll("\\", "/"));
  const pathOwners = scripts
    .filter(file => /"onboarding\/avatars\/avatar_\d+/.test(
      fs.readFileSync(file, "utf8")))
    .map(file => path.relative(scriptRoot, file).replaceAll("\\", "/"));

  assert.deepEqual(keyOwners, ["Onboarding/OnboardingProfile.cs"]);
  assert.deepEqual(pathOwners, ["Onboarding/OnboardingAvatarCatalog.cs"]);

  const sharedOwners = [
    "Assets/SCRIPT/Design/MainMenuHomeVisuals.cs",
    "Assets/SCRIPT/Design/SoloDuelVisuals.cs",
    "Assets/SCRIPT/Design/DailyHuntVisuals.cs",
    "Assets/SCRIPT/Design/SettingsVisuals.cs",
  ];
  const resolver = read("Assets/SCRIPT/Design/PlayerProfileAvatarResolver.cs")
    .toString("utf8");
  for (const ownerPath of sharedOwners) {
    const owner = read(ownerPath).toString("utf8");
    assert.match(owner, /PlayerProfileAvatarResolver\.Resolve\s*\(\s*\)/,
      ownerPath);
    assert.match(owner, /PlayerProfileAvatarResolver\.FallbackResourcePath/,
      ownerPath);
    assert.equal(owner.includes("HOL.Onboarding.Avatar"), false, ownerPath);
    assert.equal(owner.includes("OnboardingProfile.AvatarKey"), false,
      ownerPath);
    assert.doesNotMatch(owner, /OnboardingProfile\.TryCommit\s*\(/,
      ownerPath);
    assert.doesNotMatch(owner, /OnboardingAvatarCatalog\.Get\s*\(/,
      ownerPath);
    assert.equal(/"onboarding\/avatars\/avatar_\d+/.test(owner), false,
      ownerPath);
  }
  assert.match(resolver, /OnboardingProfile\.TryLoadCommittedAvatar/);
  assert.match(resolver, /OnboardingAvatarCatalog\.Get/);
  assert.match(resolver,
    /public const string FallbackResourcePath\s*=\s*"reference\/player_cyan_exact"/);
  assert.equal(resolver.includes("HOL.Onboarding.Avatar"), false);
  assert.equal(/onboarding\/avatars\/avatar_\d+/.test(resolver), false);
  assert.doesNotMatch(resolver, /PlayerPrefs\./);
  assert.doesNotMatch(resolver, /OnboardingProfile\.TryCommit/);
  assert.doesNotMatch(resolver,
    /^\s*(?:public|private|internal|protected)?\s*static\s+(?:readonly\s+)?Sprite\s+\w+\s*(?:=|;)/m);
  assert.doesNotMatch(resolver, /(?:Dictionary|ConcurrentDictionary|List)</);

  assert.equal(read("Assets/SCRIPT/Design/MainMenuHomeVisuals.cs")
    .toString("utf8").includes('"reference/player_cyan_exact"'), false);
  assert.equal(read("Assets/SCRIPT/Design/DailyHuntVisuals.cs")
    .toString("utf8").includes('"reference/player_cyan_exact"'), false);
  assert.equal(read("Assets/SCRIPT/Design/SettingsVisuals.cs")
    .toString("utf8").includes('"reference/player_cyan_exact"'), false);
  const soloOwner = read("Assets/SCRIPT/Design/SoloDuelVisuals.cs")
    .toString("utf8");
  assert.match(soloOwner,
    /const string PlayerResource\s*=\s*"reference\/player_cyan_exact"/);
  assert.equal(soloOwner.includes("solo_player_avatar_v1"), false);

  const capture = read("Assets/SCRIPT/Design/MainMenuLocalCapturePlayer.cs")
    .toString("utf8").trim();
  assert.ok(capture.startsWith(
    "#if UNITY_STANDALONE_WIN && DEVELOPMENT_BUILD"));
  assert.ok(capture.endsWith("#endif"));
  assert.match(capture, /OnboardingProfile\.AvatarKey/);
  assert.match(capture, /OnboardingProfile\.VersionKey/);
  assert.equal(capture.includes("HOL.Onboarding.Avatar"), false);
});
