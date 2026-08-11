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
