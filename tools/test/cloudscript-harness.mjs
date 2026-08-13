// Test harness for playfab/cloudscript.js.
//
// PlayFab Legacy CloudScript runs as plain ES5 against a `server` object, a
// `handlers` map and a `currentPlayerId` global. Nothing in it is PlayFab
// specific beyond those three, so the real file can be loaded into a VM with an
// in-memory Shared Group store and exercised exactly as production runs it.
// That keeps the duel rules under test without a Unity or PlayFab round trip.

import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";
import vm from "node:vm";

const here = dirname(fileURLToPath(import.meta.url));
const CLOUDSCRIPT = join(here, "..", "..", "playfab", "cloudscript.js");

// Mirrors PlayFab's Shared Group semantics: CreateSharedGroup is unique by id
// and throws on conflict, which is what the room/turn/ack claims rely on.
class SharedGroupStore {
  constructor() {
    this.groups = new Map();
    this.calls = { create: 0, read: 0, write: 0, delete: 0 };
  }

  CreateSharedGroup({ SharedGroupId }) {
    this.calls.create++;
    if (this.groups.has(SharedGroupId))
      throw new Error(`SharedGroupId ${SharedGroupId} already exists`);
    this.groups.set(SharedGroupId, {});
    return { SharedGroupId };
  }

  GetSharedGroupData({ SharedGroupId, Keys }) {
    this.calls.read++;
    if (!this.groups.has(SharedGroupId))
      throw new Error(`SharedGroupId ${SharedGroupId} not found`);

    const stored = this.groups.get(SharedGroupId);
    const Data = {};
    const wanted = Keys && Keys.length ? Keys : Object.keys(stored);
    for (const key of wanted)
      if (key in stored) Data[key] = { Value: stored[key] };
    return { Data };
  }

  UpdateSharedGroupData({ SharedGroupId, Data }) {
    this.calls.write++;
    if (!this.groups.has(SharedGroupId))
      throw new Error(`SharedGroupId ${SharedGroupId} not found`);
    Object.assign(this.groups.get(SharedGroupId), Data);
    return {};
  }

  DeleteSharedGroup({ SharedGroupId }) {
    this.calls.delete++;
    if (!this.groups.has(SharedGroupId))
      throw new Error(`SharedGroupId ${SharedGroupId} not found`);
    this.groups.delete(SharedGroupId);
    return {};
  }
}

export function loadCloudScript({ random } = {}) {
  const store = new SharedGroupStore();
  const sandbox = {
    handlers: {},
    server: store,
    currentPlayerId: "",
    JSON,
    String,
    Number,
    Math: random ? Object.assign(Object.create(Math), { random }) : Math,
    Object,
  };

  vm.createContext(sandbox);
  vm.runInContext(readFileSync(CLOUDSCRIPT, "utf8"), sandbox, { filename: CLOUDSCRIPT });

  // Every handler runs as a specific player, exactly as CloudScript does.
  const call = (name, playerId, args) => {
    if (typeof sandbox.handlers[name] !== "function")
      throw new Error(`handler '${name}' is not defined in cloudscript.js`);
    sandbox.currentPlayerId = playerId;
    return sandbox.handlers[name](args, {});
  };

  return {
    store,
    call,
    handlers: sandbox.handlers,
    // The room view the client would deserialize into PvpBackend.RoomState.
    view: (result) => JSON.parse(result.state),
  };
}

// A started duel: host and guest have both committed a secret and the opener
// coin flip has been taken.
export function startMatch(cs, { hostSecret = 42, guestSecret = 77 } = {}) {
  const created = cs.call("createRoom", "HOST", { hostName: "Host", hostSecret });
  if (!created.ok) throw new Error(`createRoom failed: ${created.error}`);

  const joined = cs.call("joinRoom", "GUEST", {
    roomId: created.roomId,
    guestName: "Guest",
    guestSecret,
  });
  if (!joined.ok) throw new Error(`joinRoom failed: ${joined.error}`);

  return { roomId: created.roomId, state: cs.view(joined) };
}

export const PLAYER = { host: "HOST", guest: "GUEST" };

export function guess(cs, roomId, side, value, lock = false) {
  return cs.call("submitGuess", PLAYER[side], { roomId, guess: value, lock });
}

// The strategy every rational player converges on: halve the interval.
export function midpointSolver() {
  let lo = 1;
  let hi = 100;
  return {
    next: () => Math.floor((lo + hi) / 2),
    remaining: () => hi - lo + 1,
    tell(value, hint) {
      if (hint === "higher") lo = value + 1;
      else if (hint === "lower") hi = value - 1;
    },
  };
}
