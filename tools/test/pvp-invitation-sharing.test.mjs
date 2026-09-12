import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";

const platform = fs.readFileSync("Assets/SCRIPT/PvP/PvpInvitationSharing.cs", "utf8");
const controller = fs.readFileSync("Assets/SCRIPT/PvP/PvpGameController.cs", "utf8");

test("Android invitation uses the system text chooser, not a fixed app or recipient", () => {
  assert.match(platform, /#if UNITY_ANDROID && !UNITY_EDITOR/);
  for (const value of ["android.intent.action.SEND", "text/plain", "android.intent.extra.TEXT", "createChooser", "startActivity"])
    assert.ok(platform.includes(`"${value}"`), value);
  assert.doesNotMatch(platform, /setPackage|setComponent|com\.whatsapp|EXTRA_STREAM|SessionTicket|PlayerPrefs|deviceUniqueIdentifier/);
  assert.doesNotMatch(platform, /GameEvents\.|systemCopyBuffer|Debug\.[^(]+\([^;]*(?:invitation|Exception)/);
});

test("opening a chooser shares only the localized current room code and never reports delivery", () => {
  const body = controller.split("public void OnShareInvitePressed()")[1].split("void ResumeWaitingStatus()")[0];
  assert.match(body, /if \(!CanShareInvite\) return;/);
  assert.match(body, /L10n\.Get\("pvp_invite_text", client\.RoomCode\)/);
  assert.doesNotMatch(body, /RoomShared\(|systemCopyBuffer|secret|SessionTicket|deviceUniqueIdentifier/);
  assert.match(body, /pvp_share_unavailable/);
  assert.match(controller, /presentationRoomCode == client\.RoomCode/);
});
