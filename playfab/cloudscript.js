// HOL — PlayFab CloudScript (Legacy)
// Server-authoritative PvP room functions. Clients call only ExecuteCloudScript.
// Room groups deliberately have NO client members: private Shared Group state is
// therefore unreadable and unwritable through Client Shared Group APIs even if
// an API Access Policy is accidentally left permissive.

var CODE_ALPHABET = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // no 0/O/1/I/L
var ROOM_CREATE_ATTEMPTS = 12;

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
            turn: "guest",
            phase: "waiting",
            lastGuess: 0,
            lastBy: "",
            winner: "",
            lastHint: "",
            hostGuessCount: 0,
            guestGuessCount: 0,
            turnIndex: 0,
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
    state.lastGuess = guess;
    state.lastBy = side;
    state.lastHint = guess === opponentSecret ? "correct" : (guess < opponentSecret ? "higher" : "lower");

    if (side === "host") state.hostGuessCount = (state.hostGuessCount | 0) + 1;
    else state.guestGuessCount = (state.guestGuessCount | 0) + 1;

    if (guess === opponentSecret) {
        state.phase = "done";
        state.winner = side;
    } else {
        state.turn = side === "host" ? "guest" : "host";
        state.turnIndex = turnIndex + 1;
    }

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

// ---------------------------------------------------------------- daily hunt

function dailyStatsGroupId(day) {
    return "DAILY-STATS-" + day;
}

// One counter bucket per day: { "1".."20": count, "fail": count }. Counters
// are best-effort (Shared Groups have no atomic increment, so simultaneous
// submissions can lose an update) — the percentile is display-grade social
// proof, not an authoritative leaderboard.
handlers.submitDaily = function (args, context) {
    var day = args ? (args.day | 0) : 0;
    var guesses = args ? (args.guesses | 0) : -1; // 0 = failed, else guesses used
    if (day < 1) return { ok: false, error: "bad day" };
    if (guesses < 0 || guesses > 20) return { ok: false, error: "bad guesses" };

    // Clients number days from local-calendar 2026-01-01; allow one day of
    // timezone skew around the server's UTC day.
    var serverDay = Math.floor((Date.now() - Date.UTC(2026, 0, 1)) / 86400000) + 1;
    if (Math.abs(day - serverDay) > 1) return { ok: false, error: "bad day" };

    var groupId = dailyStatsGroupId(day);
    try { server.CreateSharedGroup({ SharedGroupId: groupId }); } catch (exists) { }

    var stats = {};
    try {
        var data = server.GetSharedGroupData({ SharedGroupId: groupId, Keys: ["stats"] });
        if (data && data.Data && data.Data.stats && data.Data.stats.Value)
            stats = JSON.parse(data.Data.stats.Value);
    } catch (e) { stats = {}; }

    var key = guesses === 0 ? "fail" : String(guesses);
    stats[key] = (stats[key] | 0) + 1;
    server.UpdateSharedGroupData({
        SharedGroupId: groupId,
        Data: { stats: JSON.stringify(stats) },
        Permission: "Private",
    });

    // Opportunistic cleanup keeps storage bounded: yesterday's bucket is
    // still being read, day-2 is done.
    deleteGroupQuietly(dailyStatsGroupId(day - 2));

    // "better" = submissions this result beats: every fail, and every find
    // that needed more guesses.
    var total = 0, better = 0;
    for (var k in stats) {
        var n = stats[k] | 0;
        total += n;
        if (guesses !== 0 && (k === "fail" || parseInt(k, 10) > guesses))
            better += n;
    }
    return { ok: true, total: total, better: better };
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

        var otherSide = side === "host" ? "guest" : "host";
        if (!groupExists(ackGroupId(roomId, otherSide)))
            return { ok: true };
    }

    deleteRoomArtifacts(roomId, state);
    return { ok: true };
};
