// HOL — PlayFab CloudScript (Legacy)
// Paste this into Game Manager → Automation → CloudScript (Legacy) → Revisions,
// then Save and Deploy.
//
// joinRoom: adds the calling player to a room (shared group) by its invite
// code. Needed because the Client API only lets existing members add members;
// this runs with server authority, so any player holding a valid code can join.

handlers.joinRoom = function (args, context) {
    if (!args || !args.roomId) {
        return { ok: false, error: "missing roomId" };
    }

    var roomId = String(args.roomId).toUpperCase().trim();

    // Verify the room exists and is still open before admitting the player.
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

    server.AddSharedGroupMembers({
        SharedGroupId: roomId,
        PlayFabIds: [currentPlayerId],
    });

    return { ok: true };
};
