// Contract test: CloudScript's room view vs. HOL.Application/PvpRoomState.
//
// Unity's JsonUtility binds JSON to fields by exact name and silently leaves
// anything it cannot match at its default. A renamed or mistyped key therefore
// fails invisibly — a Lock that never reads as staked, a rematch offer the
// opponent never sees, a departure nobody is told about. Nothing throws, and
// the bug only shows up on a device.
//
// So the two sides are compared directly, across every phase a room passes
// through, rather than trusted to stay in step by review.

import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";
import { loadCloudScript, startMatch, guess, midpointSolver } from "./cloudscript-harness.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const ROOM_STATE = join(
  here, "..", "..", "Assets", "SCRIPT", "Application", "PvpRoomState.cs"
);
const BACKEND = join(
  here, "..", "..", "Assets", "SCRIPT", "PvP", "PvpBackend.cs"
);

// Every key the server can put on the wire, gathered by walking a room through
// waiting → play → done → rematch, plus a Lock and a Signal on the way.
function emittedKeys() {
  const keys = new Set();
  const note = (view) => Object.keys(view).forEach((k) => keys.add(k));

  const cs = loadCloudScript();
  const { roomId, state } = startMatch(cs, { hostSecret: 42, guestSecret: 77 });
  note(state);

  // Both sides binary-search so the walk always reaches a decision. Guessing by
  // a fixed pattern does not: turns alternate, so each side would only ever see
  // one parity of numbers and could miss its target entirely.
  const solvers = { host: midpointSolver(), guest: midpointSolver() };
  let view = state;
  let lockStaked = false;

  for (let i = 0; i < 80 && view.phase === "play"; i++) {
    const side = view.turn;
    const solver = solvers[side];
    const value = solver.next();

    // Stake one Lock early so the locked-guess and forfeited-turn fields
    // appear in at least one view.
    const useLock = !lockStaked;
    lockStaked = true;

    const result = guess(cs, roomId, side, value, useLock);
    assert.equal(result.ok, true, `guess rejected during the walk: ${result.error}`);
    view = cs.view(result);
    solver.tell(value, view.lastHint);
    note(view);
  }

  assert.equal(view.phase, "done", "the walk never reached a finished room");

  note(cs.view(cs.call("sendSignal", "HOST", { roomId, signalId: 2 })));
  note(cs.view(cs.call("requestRematch", "HOST", { roomId, secret: 30 })));
  note(cs.view(cs.call("requestRematch", "GUEST", { roomId, secret: 60 })));

  return keys;
}

function roomStateFields() {
  const source = readFileSync(ROOM_STATE, "utf8");
  assert.match(source, /public\s+class\s+PvpRoomState/);

  return new Set(
    [...source.matchAll(/^\s*public\s+(?:string|int|bool)\s+(\w+)\s*[=;]/gm)]
      .map((m) => m[1])
  );
}

test("every key CloudScript emits binds to a PvpRoomState field", () => {
  const emitted = emittedKeys();
  const fields = roomStateFields();

  assert.ok(emitted.size > 20, `only ${emitted.size} keys collected — did the walk stop early?`);

  const unbindable = [...emitted].filter((key) => !fields.has(key));
  assert.deepEqual(
    unbindable,
    [],
    `CloudScript sends these with nowhere to land on the client: ${unbindable.join(", ")}`
  );
});

test("PvpRoomState carries nothing the PlayFab view forgot to send", () => {
  const emitted = emittedKeys();
  const fields = roomStateFields();

  const unset = [...fields].filter((f) => !emitted.has(f));

  assert.deepEqual(
    unset,
    [],
    `these fields would sit at their default forever on PlayFab: ${unset.join(", ")}`
  );
});

test("PvpBackend keeps only a fieldless serializable compatibility shim", () => {
  const backend = readFileSync(BACKEND, "utf8");

  assert.match(
    backend,
    /\[Serializable\]\s*public\s+class\s+RoomState\s*:\s*PvpRoomState\s*\{\s*\}/s
  );
  assert.doesNotMatch(backend, /public\s+string\s+hostName\s*=/);
  assert.doesNotMatch(backend, /public\s+int\s+matchIndex\s*;/);
});

test("avatar wire validation exactly matches canonical CanEverSelect, not cosmetic ownership", () => {
  const catalog = readFileSync(join(here, "../../Assets/SCRIPT/Onboarding/OnboardingAvatarCatalog.cs"), "utf8");
  assert.match(catalog, /return IsDefined\(index\) &&\s*Catalog\[index\]\.Availability != AvailabilityKind.Locked;/);
  const entries = [...catalog.matchAll(/new Entry\((\d+),\s*"([^"]+)"[\s\S]*?AvailabilityKind\.(\w+),/g)];
  assert.equal(entries.length, 12);
  const cs = loadCloudScript();
  for (const [, id, , availability] of entries) {
    const created = cs.call("createRoom", "HOST", { hostName: "Host", hostAvatarId: id, hostSecret: 42 });
    assert.equal(created.ok, true);
    const joined = cs.call("joinRoom", "GUEST", { roomId: created.roomId, guestName: "Guest", guestAvatarId: id, guestSecret: 77 });
    assert.equal(joined.ok, true);
    const expected = availability === "Locked" ? "" : id;
    assert.equal(cs.view(created).hostAvatarId, expected, `host catalog ${id}`);
    assert.equal(cs.view(joined).guestAvatarId, expected, `guest catalog ${id}`);
  }
  const source = readFileSync(ROOM_STATE, "utf8");
  for (const field of ["hostAvatarId", "guestAvatarId"])
    assert.match(source, new RegExp(`public string ${field} = "";`));
});

test("avatar ids preserve explicit zero and fail closed for malformed or legacy values", () => {
  const cs = loadCloudScript();
  for (const value of [undefined, null, "", 0, 6, -1, "-1", "11", "12", "01", " 0", "0 ", "1\n", "1.0", "1e0", "1.5", "NaN", "Infinity", {}, [], true, "reference/player_cyan_exact"]) {
    const created = cs.call("createRoom", "HOST", { hostSecret: 42, hostAvatarId: value });
    assert.equal(created.ok, true);
    assert.equal(cs.view(created).hostAvatarId, "", JSON.stringify(value));
    const joined = cs.call("joinRoom", "GUEST", { roomId: created.roomId, guestSecret: 77, guestAvatarId: value });
    assert.equal(cs.view(joined).guestAvatarId, "", JSON.stringify(value));
  }
  const created = cs.call("createRoom", "HOST", { hostSecret: 42, hostAvatarId: "0" });
  assert.equal(cs.view(created).hostAvatarId, "0");
  assert.equal(cs.view(created).guestAvatarId, "");
  const legacy = JSON.parse(cs.store.groups.get(created.roomId).state);
  delete legacy.hostAvatarId;
  delete legacy.guestAvatarId;
  cs.store.groups.get(created.roomId).state = JSON.stringify(legacy);
  const joined = cs.call("joinRoom", "GUEST", { roomId: created.roomId, guestSecret: 77, guestAvatarId: "6" });
  assert.equal(cs.view(joined).hostAvatarId, "");
  assert.equal(cs.view(joined).guestAvatarId, "6");
});

test("authenticated seats own name/avatar identity through waiting, result and rematch in either profile order", () => {
  for (const [hostName, hostAvatarId, guestName, guestAvatarId] of [
    ["Marinos", "1", "Ελένη", "6"], ["Ελένη", "6", "Marinos", "1"]]) {
    const cs = loadCloudScript({ random: () => 0 });
    const created = cs.call("createRoom", "HOST", {
      hostName, hostAvatarId, hostSecret: 42, guestName: "Spoof", guestAvatarId: "0", guestId: "INTRUDER",
    });
    const roomId = created.roomId;
    const waiting = cs.view(created);
    assert.equal(waiting.hostAvatarId, hostAvatarId);
    assert.equal(waiting.guestAvatarId, "");
    assert.equal(waiting.guestName, "");
    const selfJoin = cs.call("joinRoom", "HOST", { roomId, guestName: "Spoof", guestAvatarId: "0", guestSecret: 77 });
    assert.equal(selfJoin.ok, false);
    const joined = cs.call("joinRoom", "GUEST", {
      roomId, guestName, guestAvatarId, guestSecret: 77, hostName: "Spoof", hostAvatarId: "0", hostId: "GUEST",
    });
    assert.equal(joined.ok, true);
    const expected = { hostName, hostAvatarId, guestName, guestAvatarId };
    const identity = view => Object.fromEntries(Object.keys(expected).map(key => [key, view[key]]));
    for (const actor of ["HOST", "GUEST"]) {
      const view = cs.view(cs.call("getRoom", actor, { roomId }));
      assert.deepEqual(identity(view), expected);
      assert.equal(view.revealedSecret, 0);
      assert.equal("hostSecret" in view || "guestSecret" in view, false);
    }
    assert.equal(cs.call("getRoom", "INTRUDER", { roomId }).ok, false);
    assert.equal(cs.call("joinRoom", "INTRUDER", { roomId, guestSecret: 1, guestAvatarId: "0" }).ok, false);
    guess(cs, roomId, "host", 77);
    const done = cs.view(guess(cs, roomId, "guest", 42));
    assert.equal(done.phase, "done");
    assert.deepEqual(identity(done), expected);
    cs.call("requestRematch", "HOST", { roomId, secret: 30, matchIndex: 0, hostAvatarId: "0" });
    const rematch = cs.view(cs.call("requestRematch", "GUEST", { roomId, secret: 60, matchIndex: 0, guestAvatarId: "0" }));
    assert.equal(rematch.phase, "play");
    assert.equal(rematch.matchIndex, 1);
    assert.deepEqual(identity(rematch), expected);
    assert.equal(rematch.revealedSecret, 0);
    const stale = cs.call("submitGuess", "GUEST", { roomId, guess: 30, matchIndex: 0, hostAvatarId: "0" });
    assert.equal(stale.ok, false);
    assert.deepEqual(identity(cs.view(cs.call("getRoom", "HOST", { roomId }))), expected);
  }
});
