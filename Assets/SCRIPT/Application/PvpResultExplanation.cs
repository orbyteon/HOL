// Immutable, Unity-free interpretation of finalized server facts, not a second
// rules engine. Never infer a winner/criterion from attempts or client history.
public sealed class PvpResultExplanation
{
    public readonly string Reason, WinnerName, ForfeitedName;
    public readonly int WinnerCandidates, OtherCandidates;
    public readonly bool Final;

    PvpResultExplanation(bool final, string reason = "", string winner = "",
        int winnerCandidates = 0, int otherCandidates = 0, string forfeited = "")
    {
        Final = final;
        Reason = reason;
        WinnerName = winner;
        WinnerCandidates = winnerCandidates;
        OtherCandidates = otherCandidates;
        ForfeitedName = forfeited;
    }

    static bool ValidCount(int value) => value >= 1 && value <= 100;

    public static PvpResultExplanation Capture(PvpRoomState state)
    {
        if (state == null || state.phase != "done") return new PvpResultExplanation(false);
        var fallback = new PvpResultExplanation(true);
        int host = state.resultHostCandidates, guest = state.resultGuestCandidates;
        if (state.resultReason == "draw")
            return state.winner == "draw" && ValidCount(host) && host == guest
                ? new PvpResultExplanation(true, "draw") : fallback;
        if (state.winner != "host" && state.winner != "guest") return fallback;
        bool winnerHost = state.winner == "host";
        int won = winnerHost ? host : guest, lost = winnerHost ? guest : host;
        string winner = state.NameFor(winnerHost);
        if (!ValidCount(won) || string.IsNullOrWhiteSpace(winner)) return fallback;
        switch (state.resultReason)
        {
            case "only_correct":
                if (lost != 0) return fallback;
                string forfeited = state.resultForfeitedSide == (winnerHost ? "guest" : "host")
                    ? state.NameFor(!winnerHost) : "";
                return new PvpResultExplanation(true, "only_correct", winner, won, 0, forfeited);
            case "lock":
                return ValidCount(lost) ? new PvpResultExplanation(true, "lock", winner, won, lost) : fallback;
            case "range":
                return ValidCount(lost) && won < lost
                    ? new PvpResultExplanation(true, "range", winner, won, lost) : fallback;
            default:
                return fallback;
        }
    }
}
