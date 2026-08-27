// Central, wiring-free application event hub. Analytics and engagement hooks
// subscribe to semantic events without UI refreshes impersonating new matches.
public static class GameEvents
{
    // Fired exactly once when a match ends: playerWon, guesses (0 on loss).
    // Cannot represent a draw, which is why the draw paths do not raise it.
    // Kept as-is for existing engagement listeners.
    public static System.Action<bool, int> OnMatchEnded;

    // Fired exactly once for every finished match, draws included. This is the
    // event analytics should bind to; OnMatchEnded stays for hooks that only
    // understand win/lose.
    public static System.Action<MatchOutcome> OnMatchCompleted;

    // Fired whenever persisted match stats change, including streak restoration.
    public static System.Action OnStatsChanged;

    // Fired only when a new calendar day is actually registered.
    public static System.Action<int> OnDailyStreak;

    // Fired exactly once after an accepted player guess is authoritatively
    // known to be correct. UI refreshes and opponent guesses never raise it.
    public static System.Action OnCorrectGuess;

    // Fired after a real created-room invite has been copied for sharing.
    // Copying arbitrary text or a join code is deliberately not enough.
    public static System.Action OnRoomShared;

    // The single raise point for a finished match, so a call site cannot report
    // one event and forget the other. Win/lose still reach OnMatchEnded exactly
    // as before, and a draw still reaches only the stats listeners because it
    // has no truthful (bool, int) form.
    internal static void MatchCompleted(MatchOutcome outcome)
    {
        bool won = outcome.Outcome == MatchOutcome.Result.Win;
        if (outcome.Outcome != MatchOutcome.Result.Draw)
            OnMatchEnded?.Invoke(won, won ? outcome.Guesses : 0);

        OnMatchCompleted?.Invoke(outcome);
        OnStatsChanged?.Invoke();
    }

    internal static void StatsChanged()
    {
        OnStatsChanged?.Invoke();
    }

    internal static void DailyStreak(int days)
    {
        OnDailyStreak?.Invoke(days);
    }

    internal static void CorrectGuess()
    {
        OnCorrectGuess?.Invoke();
    }

    internal static void RoomShared()
    {
        OnRoomShared?.Invoke();
    }
}
