// Does the Lock actually reward skill? — node tools/test/lock-policy-sim.mjs
//
// The rule change is only worth its complexity if using the Lock well beats not
// using it. This plays lock policies head to head through the real cloudscript
// rules, with the policies randomly assigned to host/guest each match so the
// room role cannot colour the result.

import { loadCloudScript, startMatch, guess, midpointSolver } from "./cloudscript-harness.mjs";

const RUNS = Number(process.argv[2] || 6000);

// A policy decides whether to stake the Lock on the guess about to be made.
// `mine`/`theirs` are guess counts so far, which both players can see.
const POLICIES = {
  "never lock": () => false,
  "lock when certain": (s) => s.remaining() === 1,
  "lock at 3 left": (s) => s.remaining() <= 3,
  "lock at 8 left": (s) => s.remaining() <= 8,
  "lock always": () => true,
  // Stake it only when the race is tight enough that a tie looks likely.
  "lock when racing": (s, mine, theirs) => s.remaining() <= 4 || (theirs >= mine && s.remaining() <= 8),
};

function playMatch(policyA, policyB) {
  const cs = loadCloudScript();
  const { roomId, state } = startMatch(cs, {
    hostSecret: 1 + Math.floor(Math.random() * 100),
    guestSecret: 1 + Math.floor(Math.random() * 100),
  });

  // Randomise which room role each policy holds so role bias cannot leak in.
  const aIsHost = Math.random() < 0.5;
  const policyFor = {
    host: aIsHost ? policyA : policyB,
    guest: aIsHost ? policyB : policyA,
  };
  const sideOfA = aIsHost ? "host" : "guest";

  const solvers = { host: midpointSolver(), guest: midpointSolver() };
  let view = state;

  for (let step = 0; step < 60 && view.phase === "play"; step++) {
    const side = view.turn;
    const solver = solvers[side];
    const value = solver.next();
    const mine = side === "host" ? view.hostGuessCount : view.guestGuessCount;
    const theirs = side === "host" ? view.guestGuessCount : view.hostGuessCount;

    const lockSpent = side === "host" ? view.hostLockUsed : view.guestLockUsed;
    const lock = !lockSpent && policyFor[side](solver, mine, theirs);

    const result = guess(cs, roomId, side, value, lock);
    if (!result.ok) throw new Error(`submitGuess rejected: ${result.error}`);
    view = cs.view(result);
    solver.tell(value, view.lastHint);
  }

  if (view.winner === "draw") return "draw";
  return view.winner === sideOfA ? "A" : "B";
}

function headToHead(nameA, nameB, runs) {
  const tally = { A: 0, B: 0, draw: 0 };
  for (let i = 0; i < runs; i++) tally[playMatch(POLICIES[nameA], POLICIES[nameB])]++;

  const pct = (n) => ((100 * n) / runs).toFixed(1).padStart(5) + "%";
  console.log(
    `${nameA.padEnd(19)} ${pct(tally.A)}  vs  ${pct(tally.B)} ${nameB.padEnd(19)}` +
      `   draws ${pct(tally.draw)}`
  );
  return tally;
}

console.log(`\nLock policy head-to-head — ${RUNS} matches each, perfect binary search on both sides\n`);

const baseline = "never lock";
for (const name of Object.keys(POLICIES)) {
  if (name === baseline) continue;
  headToHead(name, baseline, RUNS);
}

console.log("\nAnd the two policies a good player actually chooses between:\n");
headToHead("lock when racing", "lock when certain", RUNS);
console.log();
