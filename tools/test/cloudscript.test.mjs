// Rule tests for playfab/cloudscript.js — run with: node --test tools/test/
//
// The duel rules changed in a way that is hard to eyeball: the opener is now a
// coin flip, a match ends only at the end of a round, and the Lock decides
// same-round ties. These tests pin that behaviour down, and the fairness
// simulation at the bottom is the actual proof that the turn-order bias is gone.

import test from "node:test";
import assert from "node:assert/strict";
import {
  loadCloudScript,
  startMatch,
  guess,
  midpointSolver,
} from "./cloudscript-harness.mjs";

const other = (side) => (side === "host" ? "guest" : "host");

// Rooms draw their opener randomly, so tests that need a known opener just keep
// dealing fresh rooms until the coin lands the right way.
function matchWithOpener(opener, secrets) {
  for (let attempt = 0; attempt < 200; attempt++) {
    const cs = loadCloudScript();
    const { roomId, state } = startMatch(cs, secrets);
    if (state.opener === opener) return { cs, roomId, state };
  }
  throw new Error(`never drew ${opener} as opener`);
}

test("the opener is a coin flip, not always the joiner", () => {
  const openers = { host: 0, guest: 0 };
  for (let i = 0; i < 600; i++) {
    const cs = loadCloudScript();
    const { state } = startMatch(cs);
    openers[state.opener]++;
    assert.equal(state.turn, state.opener, "the match must start on the opener's turn");
  }

  // Before this change the guest opened 100% of the time. Allow a generous
  // band; the point is that neither side is systematically favoured.
  assert.ok(
    openers.host > 200 && openers.guest > 200,
    `opener draw looks biased: ${JSON.stringify(openers)}`
  );
});

test("a correct opening guess does not end the match — the responder answers it", () => {
  const { cs, roomId, state } = matchWithOpener("host", { hostSecret: 42, guestSecret: 77 });
  const view = cs.view(guess(cs, roomId, "host", 77));

  assert.equal(view.lastHint, "correct");
  assert.equal(view.phase, "play", "the round must finish before a winner is declared");
  assert.equal(view.pendingWin, "host");
  assert.equal(view.turn, "guest", "the responder is owed a matching final guess");
  assert.equal(state.opener, "host");
});

test("responder misses the answer, so the provisional win stands", () => {
  const { cs, roomId } = matchWithOpener("host", { hostSecret: 42, guestSecret: 77 });
  guess(cs, roomId, "host", 77);
  const view = cs.view(guess(cs, roomId, "guest", 50));

  assert.equal(view.phase, "done");
  assert.equal(view.winner, "host");
  assert.equal(view.revealedSecret, 42, "the loser sees the number they were hunting");
});

test("both correct in the same round with no Lock staked is an honest draw", () => {
  const { cs, roomId } = matchWithOpener("host", { hostSecret: 42, guestSecret: 77 });
  guess(cs, roomId, "host", 77);
  const view = cs.view(guess(cs, roomId, "guest", 42));

  assert.equal(view.phase, "done");
  assert.equal(view.winner, "draw");
});

test("the Lock breaks a same-round tie for whichever side staked it", () => {
  for (const locker of ["host", "guest"]) {
    const { cs, roomId } = matchWithOpener("host", { hostSecret: 42, guestSecret: 77 });
    guess(cs, roomId, "host", 77, locker === "host");
    const view = cs.view(guess(cs, roomId, "guest", 42, locker === "guest"));

    assert.equal(view.phase, "done");
    assert.equal(view.winner, locker, `${locker} staked the Lock and should take the tie`);
  }
});

test("both sides locking a tied round is still a draw", () => {
  const { cs, roomId } = matchWithOpener("host", { hostSecret: 42, guestSecret: 77 });
  guess(cs, roomId, "host", 77, true);
  const view = cs.view(guess(cs, roomId, "guest", 42, true));

  assert.equal(view.winner, "draw");
});

test("a missed Lock forfeits the next turn", () => {
  const { cs, roomId } = matchWithOpener("host", { hostSecret: 42, guestSecret: 77 });

  const missed = cs.view(guess(cs, roomId, "host", 50, true));
  assert.equal(missed.lastHint, "higher");
  assert.equal(missed.hostLockUsed, true);
  assert.equal(missed.hostSkipNext, true, "the miss must leave a forfeited turn behind");
  assert.equal(missed.turn, "guest");

  // Round 2 opens on the host, whose slot is burned, so the turn comes straight
  // back to the guest and the host's guess count stays put.
  const afterSkip = cs.view(guess(cs, roomId, "guest", 10));
  assert.equal(afterSkip.turn, "guest", "the forfeited slot passes straight back");
  assert.equal(afterSkip.hostSkipNext, false, "the forfeit is spent, not permanent");
  assert.equal(afterSkip.hostGuessCount, 1);
  assert.equal(afterSkip.guestGuessCount, 1);

  const secondInARow = cs.view(guess(cs, roomId, "guest", 20));
  assert.equal(secondInARow.guestGuessCount, 2, "the guest really does play twice");
  assert.equal(secondInARow.hostGuessCount, 1);
  assert.equal(secondInARow.turn, "host", "and the host is back in after paying the forfeit");
});

test("a forfeited responder cannot answer a provisional win", () => {
  const { cs, roomId } = matchWithOpener("guest", { hostSecret: 42, guestSecret: 77 });

  // Guest opens and misses a Lock; host then finds the number. The guest owes a
  // turn, so there is nobody left to answer and the host takes it.
  guess(cs, roomId, "guest", 50, true);
  const view = cs.view(guess(cs, roomId, "host", 77));

  assert.equal(view.phase, "done");
  assert.equal(view.winner, "host");
});

test("the Lock is once per match", () => {
  const { cs, roomId } = matchWithOpener("host", { hostSecret: 42, guestSecret: 77 });
  guess(cs, roomId, "host", 50, true);   // missed, lock spent, host forfeits next
  guess(cs, roomId, "guest", 10);        // round closes
  guess(cs, roomId, "guest", 20);        // host's slot is burned

  const rejected = guess(cs, roomId, "host", 60, true);
  assert.equal(rejected.ok, false);
  assert.equal(rejected.error, "lock already spent");
});

test("live secrets never appear in the room view", () => {
  const { cs, roomId } = matchWithOpener("host", { hostSecret: 42, guestSecret: 77 });
  const view = cs.view(guess(cs, roomId, "host", 50));

  assert.equal(view.hostSecret, undefined);
  assert.equal(view.guestSecret, undefined);
  assert.equal(view.revealedSecret, 0, "nothing is revealed before the match ends");
});

test("a turn can only be submitted once", () => {
  const { cs, roomId, state } = matchWithOpener("host", { hostSecret: 42, guestSecret: 77 });
  assert.equal(state.turn, "host");

  assert.equal(guess(cs, roomId, "host", 50).ok, true);
  const replay = guess(cs, roomId, "host", 51);
  assert.equal(replay.ok, false);
  assert.equal(replay.error, "not your turn");
});

// ------------------------------------------------------------------- signals

test("signals carry an index, are capped, and reject anything off the table", () => {
  const { cs, roomId } = matchWithOpener("host", { hostSecret: 42, guestSecret: 77 });

  const sent = cs.view(cs.call("sendSignal", "HOST", { roomId, signalId: 3 }));
  assert.equal(sent.signalBy, "host");
  assert.equal(sent.signalId, 3);
  assert.equal(sent.signalSeq, 1, "the sequence is what tells the client a signal is new");

  for (const bad of [-1, 6, 99]) {
    const rejected = cs.call("sendSignal", "HOST", { roomId, signalId: bad });
    assert.equal(rejected.ok, false, `signal id ${bad} must be rejected`);
    assert.equal(rejected.error, "bad signal");
  }

  // 12 per side per match: one is already spent above.
  for (let i = 0; i < 11; i++)
    assert.equal(cs.call("sendSignal", "HOST", { roomId, signalId: 0 }).ok, true);

  const capped = cs.call("sendSignal", "HOST", { roomId, signalId: 0 });
  assert.equal(capped.ok, false);
  assert.equal(capped.error, "signal limit");

  // The cap is per side, so the opponent is unaffected.
  assert.equal(cs.call("sendSignal", "GUEST", { roomId, signalId: 1 }).ok, true);
});

test("outsiders cannot signal into a room", () => {
  const { cs, roomId } = matchWithOpener("host", { hostSecret: 42, guestSecret: 77 });
  const rejected = cs.call("sendSignal", "STRANGER", { roomId, signalId: 1 });

  assert.equal(rejected.ok, false);
  assert.equal(rejected.error, "not a member");
});

test("signals survive into the result screen so players can say gg", () => {
  const { cs, roomId } = matchWithOpener("host", { hostSecret: 42, guestSecret: 77 });
  guess(cs, roomId, "host", 77);
  guess(cs, roomId, "guest", 50);

  assert.equal(cs.view(cs.call("getRoom", "HOST", { roomId })).phase, "done");
  assert.equal(cs.call("sendSignal", "GUEST", { roomId, signalId: 5 }).ok, true);
});

// ------------------------------------------------------------------- rematch

// Plays the current match out with both sides binary-searching, and returns the
// finished view.
function playToEnd(cs, roomId, view) {
  const solvers = { host: midpointSolver(), guest: midpointSolver() };

  for (let step = 0; step < 80 && view.phase === "play"; step++) {
    const side = view.turn;
    const value = solvers[side].next();
    const result = guess(cs, roomId, side, value);
    if (!result.ok) throw new Error(`submitGuess rejected: ${result.error}`);
    view = cs.view(result);
    solvers[side].tell(value, view.lastHint);
  }

  assert.equal(view.phase, "done", "match failed to finish");
  return view;
}

test("a rematch needs both players to commit a new secret", () => {
  const { cs, roomId } = matchWithOpener("host", { hostSecret: 42, guestSecret: 77 });
  playToEnd(cs, roomId, cs.view(cs.call("getRoom", "HOST", { roomId })));

  const asked = cs.view(cs.call("requestRematch", "HOST", { roomId, secret: 11 }));
  assert.equal(asked.phase, "done", "one player asking must not restart the match");
  assert.equal(asked.iWantRematch, true);
  assert.equal(asked.theyWantRematch, false);

  // The opponent sees the offer from their own side of the room.
  const opponentView = cs.view(cs.call("getRoom", "GUEST", { roomId }));
  assert.equal(opponentView.iWantRematch, false);
  assert.equal(opponentView.theyWantRematch, true);

  const started = cs.view(cs.call("requestRematch", "GUEST", { roomId, secret: 88 }));
  assert.equal(started.phase, "play", "both committed, so the next match is dealt");
  assert.equal(started.matchIndex, 1);
  assert.equal(started.iWantRematch, false, "the handshake resets once it fires");
});

test("a rematch deals a clean match, and its first guess is accepted", () => {
  const { cs, roomId } = matchWithOpener("host", { hostSecret: 42, guestSecret: 77 });
  const finished = playToEnd(cs, roomId, cs.view(cs.call("getRoom", "HOST", { roomId })));
  assert.ok(finished.hostGuessCount > 0);

  cs.call("requestRematch", "HOST", { roomId, secret: 11 });
  const next = cs.view(cs.call("requestRematch", "GUEST", { roomId, secret: 88 }));

  assert.equal(next.hostGuessCount, 0, "guess counts start over");
  assert.equal(next.guestGuessCount, 0);
  assert.equal(next.hostLockUsed, false, "each match grants a fresh Lock");
  assert.equal(next.guestLockUsed, false);
  assert.equal(next.winner, "");
  assert.equal(next.pendingWin, "");
  assert.equal(next.revealedSecret, 0, "the previous secret is not still on show");

  // The real hazard: turn claims from the finished match must not block the new
  // one. Turn ids are never reused and the old window is swept on reset.
  const opened = guess(cs, roomId, next.turn, 50);
  assert.equal(opened.ok, true, `first guess of the rematch was rejected: ${opened.error}`);

  // And the new secrets are the ones now in play.
  const view = cs.view(opened);
  const hunted = next.turn === "host" ? 88 : 11;
  const expected = 50 === hunted ? "correct" : 50 < hunted ? "higher" : "lower";
  assert.equal(view.lastHint, expected, "the rematch is being played against the new secrets");
});

test("a room survives a chain of rematches", () => {
  const { cs, roomId } = matchWithOpener("host", { hostSecret: 42, guestSecret: 77 });
  let view = cs.view(cs.call("getRoom", "HOST", { roomId }));

  for (let match = 0; match < 4; match++) {
    view = playToEnd(cs, roomId, view);
    assert.equal(view.matchIndex, match);

    cs.call("requestRematch", "HOST", { roomId, secret: 20 + match });
    const next = cs.call("requestRematch", "GUEST", { roomId, secret: 60 + match });
    assert.equal(next.ok, true, `rematch ${match + 1} failed: ${next.error}`);
    view = cs.view(next);
    assert.equal(view.phase, "play");
  }

  assert.equal(view.matchIndex, 4);
});

test("leaving the result screen tells the opponent and closes the offer", () => {
  const { cs, roomId } = matchWithOpener("host", { hostSecret: 42, guestSecret: 77 });
  playToEnd(cs, roomId, cs.view(cs.call("getRoom", "HOST", { roomId })));

  assert.equal(cs.call("leaveRoom", "GUEST", { roomId }).ok, true);

  const hostView = cs.view(cs.call("getRoom", "HOST", { roomId }));
  assert.equal(hostView.opponentLeft, true, "the room must report the departure");

  const refused = cs.call("requestRematch", "HOST", { roomId, secret: 11 });
  assert.equal(refused.ok, false);
  assert.equal(refused.error, "opponent left");
});

test("rematch rejects a bad secret, a stranger, and a live match", () => {
  const { cs, roomId } = matchWithOpener("host", { hostSecret: 42, guestSecret: 77 });

  const midMatch = cs.call("requestRematch", "HOST", { roomId, secret: 11 });
  assert.equal(midMatch.ok, false);
  assert.equal(midMatch.error, "not done");

  playToEnd(cs, roomId, cs.view(cs.call("getRoom", "HOST", { roomId })));

  for (const bad of [0, 101, -3, undefined]) {
    const rejected = cs.call("requestRematch", "HOST", { roomId, secret: bad });
    assert.equal(rejected.ok, false, `secret ${bad} must be rejected`);
    assert.equal(rejected.error, "bad secret");
  }

  const stranger = cs.call("requestRematch", "STRANGER", { roomId, secret: 11 });
  assert.equal(stranger.ok, false);
  assert.equal(stranger.error, "not a member");
});

// --------------------------------------------------------------- the fairness
// proof. This is the finding that motivated the rule change: under the old
// "first correct guess wins" rule two identical players did not split matches
// 50/50, because the opener reached every guess number first.

function playMatch(lockPolicy, slop = 0) {
  const cs = loadCloudScript();
  const hostSecret = 1 + Math.floor(Math.random() * 100);
  const guestSecret = 1 + Math.floor(Math.random() * 100);
  const { roomId, state } = startMatch(cs, { hostSecret, guestSecret });

  const solvers = { host: midpointSolver(slop), guest: midpointSolver(slop) };
  let view = state;

  for (let step = 0; step < 80 && view.phase === "play"; step++) {
    const side = view.turn;
    const solver = solvers[side];
    const value = solver.next();
    const lockSpent = side === "host" ? view.hostLockUsed : view.guestLockUsed;
    const lock = !lockSpent && lockPolicy(solver, side, view);

    const result = guess(cs, roomId, side, value, lock);
    if (!result.ok) throw new Error(`submitGuess rejected: ${result.error}`);

    view = cs.view(result);
    solver.tell(value, view.lastHint);
  }

  assert.equal(view.phase, "done", "simulated match failed to finish");
  return { winner: view.winner, opener: view.opener };
}

function simulate(label, lockPolicy, runs, slop = 0) {
  const tally = { host: 0, guest: 0, draw: 0, openerWins: 0, responderWins: 0 };
  for (let i = 0; i < runs; i++) {
    const { winner, opener } = playMatch(lockPolicy, slop);
    tally[winner]++;
    if (winner === opener) tally.openerWins++;
    else if (winner === other(opener)) tally.responderWins++;
  }

  const pct = (n) => ((100 * n) / runs).toFixed(1) + "%";
  console.log(
    `  ${label.padEnd(26)} host ${pct(tally.host)} / guest ${pct(tally.guest)} / ` +
      `draw ${pct(tally.draw)}   (opener ${pct(tally.openerWins)} vs responder ${pct(tally.responderWins)})`
  );
  return tally;
}

const neverLock = () => false;
const lockAtPromptThreshold = (solver) => solver.remaining() <= 3;

test("equally skilled players are not decided by who moves first", () => {
  const runs = 3000;
  console.log("\n  3000 matches per row:");

  const perfect = simulate("flawless, never lock", neverLock, runs);
  const perfectLock = simulate("flawless, lock at 3", lockAtPromptThreshold, runs);
  // Nobody plays a perfect binary search on a phone. A little slop is what a
  // real match looks like, and it is where the draw rate actually lands.
  const human = simulate("human-ish, lock at 3", lockAtPromptThreshold, runs, 0.2);

  for (const tally of [perfect, perfectLock, human]) {
    const gap = Math.abs(tally.openerWins - tally.responderWins) / runs;
    assert.ok(
      gap < 0.05,
      `opener/responder win gap is ${(gap * 100).toFixed(1)}% — turn order still decides matches`
    );

    const sideGap = Math.abs(tally.host - tally.guest) / runs;
    assert.ok(
      sideGap < 0.06,
      `host/guest win gap is ${(sideGap * 100).toFixed(1)}% — the room role still decides matches`
    );
  }

  // Two flawless binary searchers narrow in lockstep, so they tie often and no
  // tiebreak drawn from their search can separate them — roughly a quarter of
  // those duels are honest draws. That is the worst case, not the normal one:
  // once players stray from the midpoint at all, the guess counts and remaining
  // candidates diverge and the draw rate collapses. Guard the case that ships.
  assert.ok(
    human.draw < 0.15 * runs,
    `human-paced play still drew ${((100 * human.draw) / runs).toFixed(1)}% of matches`
  );
  assert.ok(
    human.draw < perfect.draw,
    "imperfect play should draw less often than flawless play, not more"
  );
});
