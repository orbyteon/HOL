// HOL — PlayFab CloudScript (Legacy)
// Paste this into Game Manager → Automation → CloudScript (Legacy) → Revisions,
// then Save and Deploy.
//
// joinRoom: atomically admits the calling player to a room (shared group) by
// its invite code AND claims the guest slot (name + secret + phase "play")
// in the same server execution. Needed because:
//   - the Client API only lets existing members add members (server authority
//     required), and
//   - a client-side "check empty, then write" join leaves a seconds-wide race
//     window in which two guests could both join. Doing it here shrinks that
//     window to the CloudScript execution itself.
//
// args: { roomId: string, guestName: string, guestSecret: number (1-100) }
// returns: { ok: true } | { ok: false, error: "room not found" | "room full" | ... }
//
// submitGuess: server-authoritative guess submission. The old client-side
// read-modify-write of the whole state was last-write-wins: a leaver's
// closed-write could erase a winning guess that landed first, and a stale
// in-flight guess write could resurrect a closed room. Applying the guess
// here (with the turn + phase checks) shrinks that race to this execution.
//
// args: { roomId: string, side: "host" | "guest", guess: number (1-100) }
// returns: { ok: true, state: "<new state JSON>" } |
//          { ok: false, error: "room not found" | "room corrupt" |
//                "bad side" | "bad guess" | "not in play" | "not your turn" }

handlers.joinRoom = function (args, context) {
    if (!args || !args.roomId) {
        return { ok: false, error: "missing roomId" };
    }

    var roomId = String(args.roomId).toUpperCase().trim();
    var guestName = String(args.guestName || "Guest").substring(0, 24);
    var guestSecret = args.guestSecret | 0;
    if (guestSecret < 1 || guestSecret > 100) {
        return { ok: false, error: "bad secret" };
    }

    // Verify the room exists and the guest slot is still free.
    var data;
    try {
        data = server.GetSharedGroupData({ SharedGroupId: roomId, Keys: ["state"] });
    } catch (e) {
        return { ok: false, error: "room not found" };
    }

    if (!data || !data.Data || !data.Data.state) {
        return { ok: false, error: "room not found" };
    }

    var state;
    try { state = JSON.parse(data.Data.state.Value); } catch (e) { state = null; }
    if (!state) {
        return { ok: false, error: "room corrupt" };
    }
    if (state.guestName && state.guestName.length > 0) {
        return { ok: false, error: "room full" };
    }
    if (state.phase === "closed") {
        return { ok: false, error: "room not found" };
    }

    server.AddSharedGroupMembers({
        SharedGroupId: roomId,
        PlayFabIds: [currentPlayerId],
    });

    // Claim the slot server-side: guest joins, match begins.
    state.guestName = guestName;
    state.guestSecret = guestSecret;
    state.phase = "play";

    server.UpdateSharedGroupData({
        SharedGroupId: roomId,
        Data: { state: JSON.stringify(state) },
        Permission: "Public",
    });

    return { ok: true };
};

// See the header above for why guesses are applied server-side. Mirrors the
// client's old SubmitGuess mutation field-for-field (turn/phase/lastGuess/
// lastBy/winner semantics exactly as the client writes them).
handlers.submitGuess = function (args, context) {
    if (!args || !args.roomId) {
        return { ok: false, error: "missing roomId" };
    }

    var roomId = String(args.roomId).toUpperCase().trim();
    var side = String(args.side || "");
    if (side !== "host" && side !== "guest") {
        return { ok: false, error: "bad side" };
    }
    var guess = args.guess | 0;
    if (guess < 1 || guess > 100) {
        return { ok: false, error: "bad guess" };
    }

    var data;
    try {
        data = server.GetSharedGroupData({ SharedGroupId: roomId, Keys: ["state"] });
    } catch (e) {
        return { ok: false, error: "room not found" };
    }

    if (!data || !data.Data || !data.Data.state) {
        return { ok: false, error: "room not found" };
    }

    var state;
    try { state = JSON.parse(data.Data.state.Value); } catch (e) { state = null; }
    if (!state) {
        return { ok: false, error: "room corrupt" };
    }

    // Server authority on the race-prone checks: a closed/finished room can
    // never be resurrected by a stale in-flight guess, and a guess lands
    // only on the caller's own turn.
    if (state.phase !== "play") {
        return { ok: false, error: "not in play" };
    }
    if (state.turn !== side) {
        return { ok: false, error: "not your turn" };
    }

    // Apply the guess exactly as the client's old read-modify-write did.
    var opponentSecret = side === "host" ? state.guestSecret : state.hostSecret;
    state.lastGuess = guess;
    state.lastBy = side;
    if (guess === opponentSecret) {
        state.phase = "done";
        state.winner = side;
    } else {
        state.turn = side === "host" ? "guest" : "host";
    }

    server.UpdateSharedGroupData({
        SharedGroupId: roomId,
        Data: { state: JSON.stringify(state) },
        Permission: "Public",
    });

    // Return the new state so the client can apply it immediately instead of
    // waiting for the next poll (keeps its cached snapshot from regressing).
    return { ok: true, state: JSON.stringify(state) };
};
