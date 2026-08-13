// HOL — PlayFab CloudScript (Legacy)
// Server-authoritative PvP room functions. Clients call only ExecuteCloudScript.
// Room groups deliberately have NO client members: private Shared Group state is
// therefore unreadable and unwritable through Client Shared Group APIs even if
// an API Access Policy is accidentally left permissive.
//
// Duel rules (mirrored in C# by Assets/SCRIPT/DuelRules.cs for solo play and
// the Firebase development fallback — keep the two in step):
//   * The opener is a coin flip taken when the guest joins, and stays fixed for
//     the match. Before this, the guest always opened, which handed the joiner a
//     ~64% win rate between equally skilled players.
//   * A round is one guess per side. A match ends only at the END of a round, so
//     the responder always answers the opener's winning guess ("last licks").
//     Turn order therefore no longer decides the duel — efficiency does.
//   * Each side holds one Lock per match. A correct locked guess wins a
//     same-round tie; a wrong locked guess forfeits that side's next turn.
//     Both locked or neither locked on a tie is an honest draw.
//   * Signals are a closed vocabulary of six pre-localized messages. There is no
//     free text anywhere in the protocol, by design.

var CODE_ALPHABET = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // no 0/O/1/I/L
var ROOM_CREATE_ATTEMPTS = 12;
var SIGNAL_COUNT = 6;       // valid signal ids are 0..5, resolved to text on the client
var SIGNAL_CAP_PER_SIDE = 12; // per match; blocks spam and caps CloudScript spend

function cleanName(value, fallback) {
    var s = String(value || fallback || "Player").trim();
    if (!s) s = fallback || "Player";
    return s.substring(0, 16);
}

function validSecret(value) {
    var n = value | 0;
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

function claimGroupId(roomId) {
    return roomId + "-GUEST";
}

function turnGroupId(roomId, turnIndex) {
    return roomId + "-TURN-" + turnIndex;
}

function ackGroupId(roomId, side) {
    return roomId + "-ACK-" + side.toUpperCase();
}

function groupExists(groupId) {
    try {
        server.GetSharedGroupData({ SharedGroupId: groupId, Keys: [] });
        return true;
    } catch (e) {
        return false;
    }
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

function writeState(roomId, state) {
    server.UpdateSharedGroupData({
        SharedGroupId: roomId,
        Data: { state: JSON.stringify(state) },
        Permission: "Private",
    });
}

function deleteGroupQuietly(groupId) {
    try { server.DeleteSharedGroup({ SharedGroupId: groupId }); }
    catch (e) { }
}

function deleteRoomArtifacts(roomId, state) {
    deleteGroupQuietly(roomId);
    deleteGroupQuietly(claimGroupId(roomId));
    deleteGroupQuietly(ackGroupId(roomId, "host"));
    deleteGroupQuietly(ackGroupId(roomId, "guest"));

    var maxTurn = state && typeof state.turnIndex === "number" ? state.turnIndex : 0;
    maxTurn = Math.max(0, Math.min(maxTurn, 200));
    for (var i = 0; i <= maxTurn; i++)
        deleteGroupQuietly(turnGroupId(roomId, i));
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

// Both sides found the number in the same round. The Lock is the tiebreaker:
// whoever staked it and was right takes the match, otherwise it is a draw.
function resolveTie(state, latestSide, latestLocked) {
    if (latestLocked && !state.pendingWinLocked) return latestSide;
    if (!latestLocked && state.pendingWinLocked) return state.pendingWin;
    return "draw";
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
    };
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
            turnIndex: 0,
            roundIndex: 0,
            actedHost: false,
            actedGuest: false,
            skipHost: false,
            skipGuest: false,
            lockUsedHost: false,
            lockUsedGuest: false,
            pendingWin: "",
            pendingWinLocked: false,
            signalBy: "",
            signalId: 0,
            signalSeq: 0,
            hostSignalCount: 0,
            guestSignalCount: 0,
        };
        writeState(roomId, state);
        return { ok: true, roomId: roomId, state: JSON.stringify(viewFor(state, currentPlayerId)) };
    } catch (e2) {
        deleteRoomArtifacts(roomId, null);
        return { ok: false, error: "create failed" };
    }
};

handlers.joinRoom = function (args, context) {
    if (!args || !args.roomId) return { ok: false, error: "missing roomId" };

    var roomId = String(args.roomId).toUpperCase().trim();
    var guestSecret = validSecret(args.guestSecret);
    if (!guestSecret) return { ok: false, error: "bad secret" };

    var state = readState(roomId);
    if (!state || state.phase === "closed") return { ok: false, error: "room not found" };
    if (state.phase !== "waiting" || state.guestId) return { ok: false, error: "room full" };
    if (state.hostId === currentPlayerId) return { ok: false, error: "room full" };

    // Atomic guest-slot claim: CreateSharedGroup is unique by ID. Exactly one
    // concurrent joiner can create the claim group; all others get a conflict.
    try {
        server.CreateSharedGroup({ SharedGroupId: claimGroupId(roomId) });
    } catch (claimError) {
        return { ok: false, error: "room full" };
    }

    try {
        // Re-read after claiming in case the host closed the room immediately
        // before this invocation won the claim.
        state = readState(roomId);
        if (!state || state.phase !== "waiting" || state.guestId) {
            deleteGroupQuietly(claimGroupId(roomId));
            return { ok: false, error: state ? "room full" : "room not found" };
        }

        state.guestId = currentPlayerId;
        state.guestName = cleanName(args.guestName, "Player");
        state.guestSecret = guestSecret;
        state.phase = "play";
        state.opener = Math.random() < 0.5 ? "host" : "guest";
        state.turn = state.opener;
        writeState(roomId, state);

        return { ok: true, state: JSON.stringify(viewFor(state, currentPlayerId)) };
    } catch (e) {
        deleteGroupQuietly(claimGroupId(roomId));
        return { ok: false, error: "join failed" };
    }
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

    var state = readState(roomId);
    if (!state) return { ok: false, error: "room not found" };

    var side = sideForPlayer(state, currentPlayerId);
    if (!side) return { ok: false, error: "not a member" };
    if (state.phase !== "play") return { ok: false, error: "not in play" };
    if (state.turn !== side) return { ok: false, error: "not your turn" };

    var locked = args.lock === true || args.lock === 1;
    var lockKey = side === "host" ? "lockUsedHost" : "lockUsedGuest";
    if (locked && state[lockKey]) return { ok: false, error: "lock already spent" };

    // One immutable claim group per turn makes duplicate/concurrent submissions
    // for the same turn fail closed without relying on client-side tap guards.
    var turnIndex = state.turnIndex | 0;
    var turnClaim = turnGroupId(roomId, turnIndex);
    try {
        server.CreateSharedGroup({ SharedGroupId: turnClaim });
    } catch (turnClaimError) {
        return { ok: false, error: "turn already submitted" };
    }

    var opponentSecret = side === "host" ? (state.guestSecret | 0) : (state.hostSecret | 0);
    var correct = guess === opponentSecret;

    state.lastGuess = guess;
    state.lastBy = side;
    state.lastLocked = locked;
    state.lastHint = correct ? "correct" : (guess < opponentSecret ? "higher" : "lower");
    state.turnIndex = turnIndex + 1;

    if (side === "host") state.hostGuessCount = (state.hostGuessCount | 0) + 1;
    else state.guestGuessCount = (state.guestGuessCount | 0) + 1;

    if (locked) state[lockKey] = true;

    if (correct) {
        // A win is provisional until the round closes, so the responder always
        // gets the answering guess the opener just had.
        state.pendingWin = state.pendingWin
            ? resolveTie(state, side, locked)
            : side;
        if (!state.pendingWinLocked) state.pendingWinLocked = locked;
    } else if (locked) {
        // Staking the Lock and missing costs the next turn.
        if (side === "host") state.skipHost = true;
        else state.skipGuest = true;
    }

    markActed(state, side);
    advanceTurn(state);

    try {
        writeState(roomId, state);
    } catch (writeError) {
        // A failed state write must not consume the turn forever. Roll the
        // immutable claim back so the same player can retry after recovery.
        deleteGroupQuietly(turnClaim);
        return { ok: false, error: "write failed" };
    }

    return { ok: true, state: JSON.stringify(viewFor(state, currentPlayerId)) };
};

// Closed-vocabulary quick chat. The wire carries an index into a table the
// client renders in the reader's own language — never player-authored text —
// so there is no user-generated content to moderate, translate, or retain.
handlers.sendSignal = function (args, context) {
    if (!args || !args.roomId) return { ok: false, error: "missing roomId" };

    var roomId = String(args.roomId).toUpperCase().trim();
    var signalId = args.signalId | 0;
    if (signalId < 0 || signalId >= SIGNAL_COUNT) return { ok: false, error: "bad signal" };

    var state = readState(roomId);
    if (!state) return { ok: false, error: "room not found" };

    var side = sideForPlayer(state, currentPlayerId);
    if (!side) return { ok: false, error: "not a member" };
    if (state.phase !== "play" && state.phase !== "done")
        return { ok: false, error: "not in play" };

    var countKey = side === "host" ? "hostSignalCount" : "guestSignalCount";
    if ((state[countKey] | 0) >= SIGNAL_CAP_PER_SIDE)
        return { ok: false, error: "signal limit" };

    state[countKey] = (state[countKey] | 0) + 1;
    state.signalBy = side;
    state.signalId = signalId;
    state.signalSeq = (state.signalSeq | 0) + 1;

    try { writeState(roomId, state); }
    catch (writeError) { return { ok: false, error: "write failed" }; }

    return { ok: true, state: JSON.stringify(viewFor(state, currentPlayerId)) };
};

handlers.ackResult = function (args, context) {
    if (!args || !args.roomId) return { ok: false, error: "missing roomId" };

    var roomId = String(args.roomId).toUpperCase().trim();
    var state = readState(roomId);
    if (!state) return { ok: true, deleted: true };
    if (state.phase !== "done") return { ok: false, error: "not done" };

    var side = sideForPlayer(state, currentPlayerId);
    if (!side) return { ok: false, error: "not a member" };

    // Each side claims an immutable acknowledgement group. This avoids the
    // lost-update race that would occur if both clients toggled booleans in
    // the same Shared Group state at nearly the same time.
    try { server.CreateSharedGroup({ SharedGroupId: ackGroupId(roomId, side) }); }
    catch (alreadyAcknowledged) { }

    if (groupExists(ackGroupId(roomId, "host")) && groupExists(ackGroupId(roomId, "guest"))) {
        deleteRoomArtifacts(roomId, state);
        return { ok: true, deleted: true };
    }

    return { ok: true, deleted: false };
};

handlers.leaveRoom = function (args, context) {
    if (!args || !args.roomId) return { ok: false, error: "missing roomId" };

    var roomId = String(args.roomId).toUpperCase().trim();
    var state = readState(roomId);
    if (!state) return { ok: true };
    var side = sideForPlayer(state, currentPlayerId);
    if (!side) return { ok: false, error: "not a member" };

    // A finished room must survive until the opponent has observed the
    // result: leaving after "done" counts as this side's acknowledgement,
    // and artifacts are destroyed only once both sides have acked (here or
    // in ackResult). Leaving mid-match still deletes immediately so the
    // opponent's poll reports the departure.
    if (state.phase === "done") {
        try { server.CreateSharedGroup({ SharedGroupId: ackGroupId(roomId, side) }); }
        catch (alreadyAcknowledged) { }

        var otherAck = side === "host" ? "guest" : "host";
        if (!groupExists(ackGroupId(roomId, otherAck)))
            return { ok: true };
    }

    deleteRoomArtifacts(roomId, state);
    return { ok: true };
};
