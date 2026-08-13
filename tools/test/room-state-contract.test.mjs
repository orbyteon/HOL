// Contract test: CloudScript's room view vs. PvpBackend.RoomState.
// Run with: node --test tools/test/room-state-contract.test.mjs
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
const ROOM_STATE = join(here, "..", "..", "Assets", "SCRIPT", "PvP", "PvpBackend.cs");

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
  const start = source.indexOf("class RoomState");
  const end = source.indexOf("public string RoomCode");
  assert.ok(start > 0 && end > start, "could not locate RoomState in PvpBackend.cs");

  return new Set(
    [...source.slice(start, end).matchAll(/^\s*public\s+(?:string|int|bool)\s+(\w+)\s*[=;]/gm)]
      .map((m) => m[1])
  );
}

test("every key CloudScript emits binds to a RoomState field", () => {
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

test("RoomState carries nothing the PlayFab view forgot to send", () => {
  const emitted = emittedKeys();
  const fields = roomStateFields();

  // The Firebase fallback keeps live secrets in its own room document; the
  // PlayFab view deliberately never sends them. Everything else must arrive.
  const firebaseOnly = new Set(["hostSecret", "guestSecret"]);
  const unset = [...fields].filter((f) => !emitted.has(f) && !firebaseOnly.has(f));

  assert.deepEqual(
    unset,
    [],
    `these fields would sit at their default forever on PlayFab: ${unset.join(", ")}`
  );
});
