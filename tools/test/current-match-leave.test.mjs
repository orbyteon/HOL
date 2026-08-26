import test from "node:test";
import assert from "node:assert/strict";
import {
  loadCloudScript,
  startMatch,
  guess,
  midpointSolver,
} from "./cloudscript-harness.mjs";

function playToEnd(cs, roomId, view) {
  const solvers = { host: midpointSolver(), guest: midpointSolver() };

  for (let step = 0; step < 80 && view.phase === "play"; step++) {
    const side = view.turn;
    const value = solvers[side].next();
    const result = guess(cs, roomId, side, value);
    assert.equal(result.ok, true, `submitGuess rejected: ${result.error}`);
    view = cs.view(result);
    solvers[side].tell(value, view.lastHint);
  }

  assert.equal(view.phase, "done");
  return view;
}

test("leave accepts the live match index after a rematch", () => {
  const cs = loadCloudScript();
  const started = startMatch(cs, { hostSecret: 42, guestSecret: 77 });
  const roomId = started.roomId;
  const firstResult = playToEnd(cs, roomId, started.state);

  assert.equal(cs.call("requestRematch", "HOST", {
    roomId,
    secret: 11,
    matchIndex: firstResult.matchIndex,
  }).ok, true);

  const secondMatch = cs.view(cs.call("requestRematch", "GUEST", {
    roomId,
    secret: 88,
    matchIndex: firstResult.matchIndex,
  }));
  assert.equal(secondMatch.phase, "play");
  assert.equal(secondMatch.matchIndex, 1);

  const left = cs.call("leaveRoom", "HOST", {
    roomId,
    matchIndex: secondMatch.matchIndex,
  });
  assert.equal(left.ok, true);
  assert.equal(cs.store.groups.has(roomId), false,
    "leaving a live current match must release the room");
});
