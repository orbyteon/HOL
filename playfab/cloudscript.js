// HOL — PlayFab CloudScript (Legacy)
// Server-authoritative PvP room functions. Clients call only ExecuteCloudScript.
// Room groups deliberately have NO client members: private Shared Group state is
// therefore unreadable and unwritable through Client Shared Group APIs even if
// an API Access Policy is accidentally left permissive.
//
// Duel rules are mirrored in C# by Assets/SCRIPT/DuelRules.cs for solo play.
// Keep the two implementations in step:
//   * The opener is a coin flip taken when the guest joins, and stays fixed for
//     the match. Before this, the guest always opened, which handed the joiner a
//     ~64% win rate between equally skilled players.
//   * A round is one guess per side. A match ends only at the END of a round, so
//     the responder always answers the opener's winning guess ("last licks").
//     Turn order therefore no longer decides the duel — efficiency does.
//   * Each side holds one Lock per match. A correct locked guess wins a
//     same-round tie; a wrong locked guess forfeits that side's next turn.
//   * If the Lock does not separate a tied round, the win goes to whoever had
//     narrowed the number further. The hints a side has received imply an exact
//     interval, and finding the number among two candidates is a better claim
//     than hitting it among twelve. Without this step two players using the
//     same strategy both lock or both do not, and roughly a quarter of duels
//     ended in stalemate; only a tie on guess count, Lock and remaining
//     candidates alike is a draw now.
//   * Signals are a closed vocabulary of six pre-localized messages. There is no
//     free text in the messaging channel, by design.

var CODE_ALPHABET = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // no 0/O/1/I/L
var ROOM_CREATE_ATTEMPTS = 12;
var SIGNAL_COUNT = 6;       // valid signal ids are 0..5, resolved to text on the client
var SIGNAL_CAP_PER_SIDE = 12; // per match; blocks spam and caps CloudScript spend
var ROOM_REGISTRY_SHARDS = 8;
// Keep scheduled executions well below Legacy CloudScript's four-second API
// time budget. Two rooms plus eight registry reads is a bounded request count;
// the five-minute schedule drains ordinary orphan volume continuously.
var ROOM_CLEANUP_LIMIT = 2;
var MUTATION_LOCK_STALE_MS = 120000;
var WAITING_ROOM_TTL_MS = 30 * 60 * 1000;
var PLAY_ROOM_TTL_MS = 6 * 60 * 60 * 1000;
var DONE_ROOM_TTL_MS = 15 * 60 * 1000;

function cleanName(value, fallback) {
    var s = String(value || fallback || "Player").trim();
    if (!s) s = fallback || "Player";
    return s.substring(0, 16);
}

function validSecret(value) {
    var n = Number(value);
    if (n !== Math.floor(n)) return 0;
    return n >= 1 && n <= 100 ? n : 0;
}

function randomCode() {
    var s = "";
    for (var i = 0; i < 5; i++)
        s += CODE_ALPHABET.charAt(Math.floor(Math.random() * CODE_ALPHABET.length));
    return s;
}

function otherSide(side) {
    return side === "host" ? "guest" : "host";
}

function nowMs() {
    return Date.now ? Date.now() : new Date().getTime();
}

function registryShard(roomId) {
    var total = 0;
    for (var i = 0; i < roomId.length; i++) total += roomId.charCodeAt(i);
    return total % ROOM_REGISTRY_SHARDS;
}

function registryGroupId(roomId) {
    return "HOL-ROOM-REG-" + registryShard(roomId);
}

function mutationLockId(roomId, revision, epoch) {
    return roomId + "-MUT-" + revision + "-" + epoch;
}

function recoveryGroupId(lockId, timestamp) {
    // Time-bucketed so an invocation that dies immediately after claiming
    // recovery cannot block the room forever. A later bucket may take over;
    // normal CloudScript executions cannot survive long enough to overlap it.
    return lockId + "-RECOVERY-" + Math.floor(timestamp / MUTATION_LOCK_STALE_MS);
}

function observationGroupId(lockId) {
    return lockId + "-OBSERVED";
}

function readState(roomId) {
    var data;
    try {
        data = server.GetSharedGroupData({ SharedGroupId: roomId, Keys: ["state"] });
    } catch (e) {
        return null;
    }
    if (!data || !data.Data || !data.Data.state || !data.Data.state.Value)
        return null;

    try { return JSON.parse(data.Data.state.Value); }
    catch (e2) { return null; }
}

function writeStateRaw(roomId, state) {
    server.UpdateSharedGroupData({
        SharedGroupId: roomId,
        Data: { state: JSON.stringify(state) },
        Permission: "Private",
    });
}

function roomTtlMs(state) {
    if (state.phase === "waiting") return WAITING_ROOM_TTL_MS;
    if (state.phase === "done") return DONE_ROOM_TTL_MS;
    return PLAY_ROOM_TTL_MS;
}

function updateRegistry(roomId, expiresAt) {
    var groupId = registryGroupId(roomId);
    var data = {};
    data[roomId] = String(expiresAt);

    try {
        server.UpdateSharedGroupData({
            SharedGroupId: groupId,
            Data: data,
            Permission: "Private",
        });
    } catch (missingRegistry) {
        try { server.CreateSharedGroup({ SharedGroupId: groupId }); }
        catch (createdByAnotherInvocation) { }
        server.UpdateSharedGroupData({
            SharedGroupId: groupId,
            Data: data,
            Permission: "Private",
        });
    }
}

function unregisterRoom(roomId) {
    try {
        server.UpdateSharedGroupData({
            SharedGroupId: registryGroupId(roomId),
            KeysToRemove: [roomId],
            Permission: "Private",
        });
    } catch (e) { }
}

function writeState(roomId, state) {
    var now = nowMs();
    if (!Number(state.createdAt)) state.createdAt = now;
    state.updatedAt = now;
    state.expiresAt = now + roomTtlMs(state);

    // Register first. If the room write then fails, cleanup will remove the
    // dangling registry entry after observing that the room does not exist.
    // The reverse order could create an unindexed room that never expires.
    updateRegistry(roomId, state.expiresAt);
    writeStateRaw(roomId, state);
}

function deleteGroupQuietly(groupId) {
    try { server.DeleteSharedGroup({ SharedGroupId: groupId }); }
    catch (e) { }
}

function deleteRoomArtifacts(roomId) {
    deleteGroupQuietly(roomId);
    unregisterRoom(roomId);
}

function sameMutationGeneration(state, revision, epoch) {
    return !!state && (state.revision | 0) === revision &&
        (state.lockEpoch | 0) === epoch;
}

function storedValue(group, key) {
    return group && group.Data && group.Data[key] ? group.Data[key].Value : "";
}

// A lock is fenced by both room revision and epoch. Recovery advances the
// epoch before removing a stale lock, so a delayed invocation cannot write a
// state snapshot acquired under the previous lease.
function recoverStaleMutationLock(roomId, state, lockId) {
    var lock;
    try { lock = server.GetSharedGroupData({ SharedGroupId: lockId, Keys: ["startedAt"] }); }
    catch (missingLock) { return; }

    var startedAt = Number(storedValue(lock, "startedAt"));
    if (!startedAt) {
        var observationId = observationGroupId(lockId);
        try {
            server.CreateSharedGroup({ SharedGroupId: observationId });
            server.UpdateSharedGroupData({
                SharedGroupId: observationId,
                Data: { startedAt: String(nowMs()) },
                Permission: "Private",
            });
            return;
        } catch (alreadyObserved) {
            try {
                var observed = server.GetSharedGroupData({
                    SharedGroupId: observationId,
                    Keys: ["startedAt"],
                });
                startedAt = Number(storedValue(observed, "startedAt"));
            } catch (missingObservation) { return; }
        }
    }
    if (!startedAt || nowMs() - startedAt < MUTATION_LOCK_STALE_MS) return;

    var recoveryId = recoveryGroupId(lockId, nowMs());
    try {
        server.CreateSharedGroup({ SharedGroupId: recoveryId });
    } catch (anotherRecovery) {
        return;
    }

    try {
        var latest = readState(roomId);
        var revision = state.revision | 0;
        var epoch = state.lockEpoch | 0;
        if (sameMutationGeneration(latest, revision, epoch)) {
            latest.lockEpoch = epoch + 1;
            writeState(roomId, latest);
        }
        deleteGroupQuietly(lockId);
        deleteGroupQuietly(observationGroupId(lockId));
    } finally {
        deleteGroupQuietly(recoveryId);
    }
}

function withRoomMutation(roomId, mutate) {
    var initial = readState(roomId);
    if (!initial) return { ok: false, error: "room not found" };

    var revision = initial.revision | 0;
    var epoch = initial.lockEpoch | 0;
    var lockId = mutationLockId(roomId, revision, epoch);
    var created = false;

    try {
        server.CreateSharedGroup({ SharedGroupId: lockId });
        created = true;
        server.UpdateSharedGroupData({
            SharedGroupId: lockId,
            Data: { startedAt: String(nowMs()) },
            Permission: "Private",
        });
    } catch (lockError) {
        if (created) deleteGroupQuietly(lockId);
        else recoverStaleMutationLock(roomId, initial, lockId);
        return { ok: false, error: "room busy" };
    }

    try {
        var state = readState(roomId);
        if (!sameMutationGeneration(state, revision, epoch))
            return { ok: false, error: "room busy" };

        var change = mutate(state) || {};
        if (!change.commit && !change.remove)
            return change.result || { ok: false, error: "mutation rejected" };

        // Fence immediately before the unconditional Shared Group write.
        // A stale-lock recovery changes the epoch, making this invocation fail
        // closed instead of overwriting a newer room snapshot.
        var fence = readState(roomId);
        if (!sameMutationGeneration(fence, revision, epoch))
            return { ok: false, error: "room busy" };

        if (change.remove) {
            deleteRoomArtifacts(roomId);
            return change.result || { ok: true, deleted: true };
        }

        state.revision = revision + 1;
        state.lockEpoch = 0;
        writeState(roomId, state);
        return typeof change.afterCommit === "function"
            ? change.afterCommit(state)
            : (change.result || { ok: true });
    } catch (writeError) {
        return { ok: false, error: "write failed" };
    } finally {
        deleteGroupQuietly(lockId);
    }
}

function sideForPlayer(state, playerId) {
    if (state.hostId === playerId) return "host";
    if (state.guestId === playerId) return "guest";
    return "";
}

// ---------------------------------------------------------------- round model

function hasActed(state, side) {
    return side === "host" ? !!state.actedHost : !!state.actedGuest;
}

function markActed(state, side) {
    if (side === "host") state.actedHost = true;
    else state.actedGuest = true;
}

// Returns true when the side owed a forfeited turn, clearing the debt.
function consumeSkip(state, side) {
    if (side === "host" && state.skipHost) { state.skipHost = false; return true; }
    if (side === "guest" && state.skipGuest) { state.skipGuest = false; return true; }
    return false;
}

// Hands the turn to whoever is owed one. A round holds one slot per side; when
// both slots are spent the round closes, and a win recorded during it becomes
// final. A side carrying a forfeit silently burns its slot.
function advanceTurn(state) {
    for (var guard = 0; guard < 8; guard++) {
        if (state.actedHost && state.actedGuest) {
            if (state.pendingWin) {
                state.phase = "done";
                state.winner = state.pendingWin;
                state.turn = "";
                return;
            }
            state.actedHost = false;
            state.actedGuest = false;
            state.roundIndex = (state.roundIndex | 0) + 1;
        }

        var next = hasActed(state, state.opener) ? otherSide(state.opener) : state.opener;
        if (consumeSkip(state, next)) {
            markActed(state, next);
            continue;
        }

        state.turn = next;
        return;
    }

    state.turn = state.opener;
}

// Both sides found the number in the same round. The Lock decides first —
// whoever staked it and was right takes the match. Failing that, the tighter
// search wins: fewer candidates left means the win was earned, not stumbled on.
function resolveTie(state, latestSide, latestLocked, latestCandidates) {
    if (latestLocked && !state.pendingWinLocked) return latestSide;
    if (!latestLocked && state.pendingWinLocked) return state.pendingWin;

    var held = state.pendingWinCandidates | 0;
    if (latestCandidates > 0 && held > 0) {
        if (latestCandidates < held) return latestSide;
        if (held < latestCandidates) return state.pendingWin;
    }

    return "draw";
}

// How many numbers the hints this side has received still leave open. It is
// objective: it follows from the answers given, not from how well the player
// used them.
function candidatesFor(state, side) {
    var lo = side === "host" ? (state.hostLo | 0) : (state.guestLo | 0);
    var hi = side === "host" ? (state.hostHi | 0) : (state.guestHi | 0);
    if (lo < 1) lo = 1;
    if (hi < 1 || hi > 100) hi = 100;
    return hi >= lo ? hi - lo + 1 : 1;
}

function narrowFor(state, side, guess, hint) {
    if (side === "host") {
        if (hint === "higher" && guess + 1 > (state.hostLo | 0)) state.hostLo = guess + 1;
        else if (hint === "lower" && guess - 1 < (state.hostHi | 0)) state.hostHi = guess - 1;
    } else {
        if (hint === "higher" && guess + 1 > (state.guestLo | 0)) state.guestLo = guess + 1;
        else if (hint === "lower" && guess - 1 < (state.guestHi | 0)) state.guestHi = guess - 1;
    }
}

function viewFor(state, playerId) {
    var side = sideForPlayer(state, playerId);
    var revealed = 0;
    if (state.phase === "done")
        revealed = side === "host" ? (state.guestSecret | 0) : (state.hostSecret | 0);

    return {
        hostName: String(state.hostName || ""),
        guestName: String(state.guestName || ""),
        turn: String(state.turn || ""),
        phase: String(state.phase || ""),
        lastGuess: state.lastGuess | 0,
        lastBy: String(state.lastBy || ""),
        winner: String(state.winner || ""),
        lastHint: String(state.lastHint || ""),
        revealedSecret: revealed,
        hostGuessCount: state.hostGuessCount | 0,
        guestGuessCount: state.guestGuessCount | 0,

        // Duel rules. Symmetric information: both sides see the same values, so
        // exposing them costs nothing and lets the UI show the stakes.
        opener: String(state.opener || ""),
        pendingWin: String(state.pendingWin || ""),
        lastLocked: !!state.lastLocked,
        hostLockUsed: !!state.lockUsedHost,
        guestLockUsed: !!state.lockUsedGuest,
        hostSkipNext: !!state.skipHost,
        guestSkipNext: !!state.skipGuest,
        roundIndex: state.roundIndex | 0,

        signalBy: String(state.signalBy || ""),
        signalId: state.signalId | 0,
        signalSeq: state.signalSeq | 0,

        // Rematch handshake. matchIndex changing is how a client knows the next
        // match has actually started rather than merely being offered.
        matchIndex: state.matchIndex | 0,
        iWantRematch: side === "host" ? !!state.rematchHost : !!state.rematchGuest,
        theyWantRematch: side === "host" ? !!state.rematchGuest : !!state.rematchHost,
        opponentLeft: side === "host" ? !!state.leftGuest : !!state.leftHost,
    };
}

// Both players committed a fresh secret, so deal the next match in the same
// room. Everything that describes the finished match is cleared.
function resetForRematch(state) {
    state.hostSecret = state.rematchHost | 0;
    state.guestSecret = state.rematchGuest | 0;
    state.rematchHost = 0;
    state.rematchGuest = 0;
    state.ackedHost = false;
    state.ackedGuest = false;
    state.leftHost = false;
    state.leftGuest = false;

    state.matchIndex = (state.matchIndex | 0) + 1;
    state.phase = "play";
    state.opener = Math.random() < 0.5 ? "host" : "guest";
    state.turn = state.opener;
    state.winner = "";
    state.lastGuess = 0;
    state.lastBy = "";
    state.lastHint = "";
    state.lastLocked = false;
    state.hostGuessCount = 0;
    state.guestGuessCount = 0;
    state.roundIndex = 0;
    state.actedHost = false;
    state.actedGuest = false;
    state.skipHost = false;
    state.skipGuest = false;
    state.lockUsedHost = false;
    state.lockUsedGuest = false;
    state.pendingWin = "";
    state.pendingWinLocked = false;
    state.pendingWinCandidates = 0;
    state.hostLo = 1;
    state.hostHi = 100;
    state.guestLo = 1;
    state.guestHi = 100;

    // Each match gets a fresh signal allowance; the sequence stays monotonic so
    // a client that missed one does not replay it.
    state.hostSignalCount = 0;
    state.guestSignalCount = 0;
}

handlers.createRoom = function (args, context) {
    var hostSecret = validSecret(args && args.hostSecret);
    if (!hostSecret) return { ok: false, error: "bad secret" };

    var hostName = cleanName(args && args.hostName, "Player");
    var roomId = "";

    for (var i = 0; i < ROOM_CREATE_ATTEMPTS; i++) {
        var candidate = randomCode();
        try {
            server.CreateSharedGroup({ SharedGroupId: candidate });
            roomId = candidate;
            break;
        } catch (e) { }
    }

    if (!roomId) return { ok: false, error: "create failed" };

    try {
        var state = {
            hostId: currentPlayerId,
            guestId: "",
            hostName: hostName,
            guestName: "",
            hostSecret: hostSecret,
            guestSecret: 0,
            // The opener is drawn when the guest arrives, so neither side can
            // shop for a favourable draw by recreating rooms.
            opener: "",
            turn: "",
            phase: "waiting",
            lastGuess: 0,
            lastBy: "",
            winner: "",
            lastHint: "",
            lastLocked: false,
            hostGuessCount: 0,
            guestGuessCount: 0,
            matchIndex: 0,
            rematchHost: 0,
            rematchGuest: 0,
            ackedHost: false,
            ackedGuest: false,
            leftHost: false,
            leftGuest: false,
            revision: 0,
            lockEpoch: 0,
            roundIndex: 0,
            hostLo: 1,
            hostHi: 100,
            guestLo: 1,
            guestHi: 100,
            actedHost: false,
            actedGuest: false,
            skipHost: false,
            skipGuest: false,
            lockUsedHost: false,
            lockUsedGuest: false,
            pendingWin: "",
            pendingWinLocked: false,
            pendingWinCandidates: 0,
            signalBy: "",
            signalId: 0,
            signalSeq: 0,
            hostSignalCount: 0,
            guestSignalCount: 0,
        };
        writeState(roomId, state);
        return { ok: true, roomId: roomId, state: JSON.stringify(viewFor(state, currentPlayerId)) };
    } catch (e2) {
        deleteRoomArtifacts(roomId);
        return { ok: false, error: "create failed" };
    }
};

handlers.joinRoom = function (args, context) {
    if (!args || !args.roomId) return { ok: false, error: "missing roomId" };

    var roomId = String(args.roomId).toUpperCase().trim();
    var guestSecret = validSecret(args.guestSecret);
    if (!guestSecret) return { ok: false, error: "bad secret" };
    var playerId = currentPlayerId;

    return withRoomMutation(roomId, function (state) {
        if (state.phase !== "waiting" || state.guestId)
            return { result: { ok: false, error: "room full" } };
        if (state.hostId === playerId)
            return { result: { ok: false, error: "room full" } };

        state.guestId = playerId;
        state.guestName = cleanName(args.guestName, "Player");
        state.guestSecret = guestSecret;
        state.phase = "play";
        state.opener = Math.random() < 0.5 ? "host" : "guest";
        state.turn = state.opener;
        return {
            commit: true,
            afterCommit: function (committed) {
                return { ok: true, state: JSON.stringify(viewFor(committed, playerId)) };
            },
        };
    });
};

handlers.getRoom = function (args, context) {
    if (!args || !args.roomId) return { ok: false, error: "missing roomId" };

    var roomId = String(args.roomId).toUpperCase().trim();
    var state = readState(roomId);
    if (!state) return { ok: false, error: "room not found" };
    if (!sideForPlayer(state, currentPlayerId)) return { ok: false, error: "not a member" };

    return { ok: true, state: JSON.stringify(viewFor(state, currentPlayerId)) };
};

handlers.submitGuess = function (args, context) {
    if (!args || !args.roomId) return { ok: false, error: "missing roomId" };

    var roomId = String(args.roomId).toUpperCase().trim();
    var guess = validSecret(args.guess);
    if (!guess) return { ok: false, error: "bad guess" };

    var playerId = currentPlayerId;
    var locked = args.lock === true || args.lock === 1;

    return withRoomMutation(roomId, function (state) {
        var side = sideForPlayer(state, playerId);
        if (!side) return { result: { ok: false, error: "not a member" } };
        if (state.phase !== "play")
            return { result: { ok: false, error: "not in play" } };
        if (state.turn !== side)
            return { result: { ok: false, error: "not your turn" } };

        var lockKey = side === "host" ? "lockUsedHost" : "lockUsedGuest";
        if (locked && state[lockKey])
            return { result: { ok: false, error: "lock already spent" } };

        var opponentSecret = side === "host" ? (state.guestSecret | 0) : (state.hostSecret | 0);
        var correct = guess === opponentSecret;

        state.lastGuess = guess;
        state.lastBy = side;
        state.lastLocked = locked;
        state.lastHint = correct ? "correct" : (guess < opponentSecret ? "higher" : "lower");

        if (side === "host") state.hostGuessCount = (state.hostGuessCount | 0) + 1;
        else state.guestGuessCount = (state.guestGuessCount | 0) + 1;

        if (locked) state[lockKey] = true;

        // Candidates left *before* this guess narrowed anything: that is the
        // pool the guess was actually drawn from.
        var candidates = candidatesFor(state, side);
        narrowFor(state, side, guess, state.lastHint);

        if (correct) {
            // A win is provisional until the round closes, so the responder
            // always gets the answering guess the opener just had.
            if (state.pendingWin)
                state.pendingWin = resolveTie(state, side, locked, candidates);
            else {
                state.pendingWin = side;
                state.pendingWinLocked = locked;
                state.pendingWinCandidates = candidates;
            }
        } else if (locked) {
            if (side === "host") state.skipHost = true;
            else state.skipGuest = true;
        }

        markActed(state, side);
        advanceTurn(state);
        return {
            commit: true,
            afterCommit: function (committed) {
                return { ok: true, state: JSON.stringify(viewFor(committed, playerId)) };
            },
        };
    });
};

// Closed-vocabulary quick chat. The wire carries an index into a table the
// client renders in the reader's own language — never player-authored text —
// so there is no user-generated content to moderate, translate, or retain.
handlers.sendSignal = function (args, context) {
    if (!args || !args.roomId) return { ok: false, error: "missing roomId" };

    var roomId = String(args.roomId).toUpperCase().trim();
    var signalId = Number(args.signalId);
    if (signalId !== Math.floor(signalId) || signalId < 0 || signalId >= SIGNAL_COUNT)
        return { ok: false, error: "bad signal" };

    var playerId = currentPlayerId;
    return withRoomMutation(roomId, function (state) {
        var side = sideForPlayer(state, playerId);
        if (!side) return { result: { ok: false, error: "not a member" } };
        if (state.phase !== "play" && state.phase !== "done")
            return { result: { ok: false, error: "not in play" } };

        var countKey = side === "host" ? "hostSignalCount" : "guestSignalCount";
        if ((state[countKey] | 0) >= SIGNAL_CAP_PER_SIDE)
            return { result: { ok: false, error: "signal limit" } };

        state[countKey] = (state[countKey] | 0) + 1;
        state.signalBy = side;
        state.signalId = signalId;
        state.signalSeq = (state.signalSeq | 0) + 1;
        return {
            commit: true,
            afterCommit: function (committed) {
                return { ok: true, state: JSON.stringify(viewFor(committed, playerId)) };
            },
        };
    });
};

// Rematch handshake: each side commits a fresh secret, and the next match is
// dealt only once both have. Keeping the same room means friends do not have to
// re-share an invite code to play again.
handlers.requestRematch = function (args, context) {
    if (!args || !args.roomId) return { ok: false, error: "missing roomId" };

    var roomId = String(args.roomId).toUpperCase().trim();
    var secret = validSecret(args.secret);
    if (!secret) return { ok: false, error: "bad secret" };

    var playerId = currentPlayerId;
    return withRoomMutation(roomId, function (state) {
        var side = sideForPlayer(state, playerId);
        if (!side) return { result: { ok: false, error: "not a member" } };
        if (state.phase !== "done")
            return { result: { ok: false, error: "not done" } };

        var opponentGone = side === "host" ? state.leftGuest : state.leftHost;
        if (opponentGone)
            return { result: { ok: false, error: "opponent left" } };

        if (side === "host") state.rematchHost = secret;
        else state.rematchGuest = secret;

        if (state.rematchHost && state.rematchGuest) resetForRematch(state);
        return {
            commit: true,
            afterCommit: function (committed) {
                return { ok: true, state: JSON.stringify(viewFor(committed, playerId)) };
            },
        };
    });
};

handlers.ackResult = function (args, context) {
    if (!args || !args.roomId) return { ok: false, error: "missing roomId" };

    var roomId = String(args.roomId).toUpperCase().trim();
    var playerId = currentPlayerId;
    var outcome = withRoomMutation(roomId, function (state) {
        if (state.phase !== "done")
            return { result: { ok: false, error: "not done" } };

        var side = sideForPlayer(state, playerId);
        if (!side) return { result: { ok: false, error: "not a member" } };

        if (side === "host") state.ackedHost = true;
        else state.ackedGuest = true;
        if (state.ackedHost && state.ackedGuest)
            return { remove: true, result: { ok: true, deleted: true } };

        return { commit: true, result: { ok: true, deleted: false } };
    });
    return outcome && outcome.error === "room not found"
        ? { ok: true, deleted: true }
        : outcome;
};

handlers.leaveRoom = function (args, context) {
    if (!args || !args.roomId) return { ok: false, error: "missing roomId" };

    var roomId = String(args.roomId).toUpperCase().trim();
    var playerId = currentPlayerId;
    var outcome = withRoomMutation(roomId, function (state) {
        var side = sideForPlayer(state, playerId);
        if (!side) return { result: { ok: false, error: "not a member" } };

        // A finished room survives until the opponent has observed the result.
        // Leaving counts as this side's acknowledgement and closes rematch.
        if (state.phase === "done") {
            if (side === "host") {
                state.ackedHost = true;
                state.leftHost = true;
                state.rematchHost = 0;
            } else {
                state.ackedGuest = true;
                state.leftGuest = true;
                state.rematchGuest = 0;
            }

            if (state.ackedHost && state.ackedGuest)
                return { remove: true, result: { ok: true } };
            return { commit: true, result: { ok: true } };
        }

        return { remove: true, result: { ok: true } };
    });
    return outcome && outcome.error === "room not found" ? { ok: true } : outcome;
};

// Scheduled every five minutes by tools/playfab/deploy-cloudscript.mjs. The
// sharded registry makes cleanup discoverable without a database scan, and the
// same mutation lock keeps expiry from deleting a room that is being updated.
handlers.cleanupExpiredRooms = function (args, context) {
    if (typeof currentPlayerId !== "undefined" && currentPlayerId)
        return { ok: false, error: "not authorized" };

    var now = nowMs();
    var cleaned = 0;
    var busy = 0;
    var staleEntries = 0;

    for (var shard = 0; shard < ROOM_REGISTRY_SHARDS && cleaned < ROOM_CLEANUP_LIMIT; shard++) {
        var group;
        try {
            group = server.GetSharedGroupData({
                SharedGroupId: "HOL-ROOM-REG-" + shard,
                Keys: [],
            });
        } catch (missingRegistry) {
            continue;
        }

        var data = group && group.Data ? group.Data : {};
        for (var roomId in data) {
            if (!Object.prototype.hasOwnProperty.call(data, roomId)) continue;
            if (cleaned >= ROOM_CLEANUP_LIMIT) break;
            if (Number(data[roomId].Value) > now) continue;

            var outcome = withRoomMutation(roomId, function (state) {
                if (Number(state.expiresAt) > now)
                    return {
                        result: {
                            ok: true,
                            deleted: false,
                            refreshExpiresAt: Number(state.expiresAt),
                        },
                    };
                return { remove: true, result: { ok: true, deleted: true } };
            });
            if (outcome && outcome.deleted) cleaned++;
            else if (outcome && outcome.error === "room not found") {
                unregisterRoom(roomId);
                staleEntries++;
            } else if (outcome && outcome.refreshExpiresAt > now) {
                updateRegistry(roomId, outcome.refreshExpiresAt);
                staleEntries++;
            }
            else if (outcome && outcome.error === "room busy") busy++;
        }
    }

    return { ok: true, cleaned: cleaned, busy: busy, staleEntries: staleEntries };
};
