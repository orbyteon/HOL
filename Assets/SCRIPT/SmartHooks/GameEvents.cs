using UnityEngine;

// Central, wiring-free event hub for engagement hooks. Analytics can subscribe
// to semantic events without UI refreshes impersonating new matches.
public static class GameEvents
{
    // Fired exactly once when a match ends: playerWon, guesses (0 on loss).
    // Cannot represent a draw, which is why the draw paths raise only
    // OnStatsChanged. Kept as-is for the UI listeners already bound to it.
    public static System.Action<bool, int> OnMatchEnded;

    // Fired exactly once for every finished match, draws included. This is the
    // event analytics should bind to; OnMatchEnded stays for the engagement
    // hooks that only care about win/lose.
    public static System.Action<MatchOutcome> OnMatchCompleted;

    // Fired whenever persisted match stats change, including streak restoration.
    public static System.Action OnStatsChanged;

    // Fired only when a new calendar day is actually registered.
    public static System.Action<int> OnDailyStreak;

    // The single raise point for a finished match, so a call site cannot report
    // one event and forget the other. Win/lose still reach OnMatchEnded exactly
    // as before, and a draw still reaches only the stats listeners — it has no
    // truthful (bool, int) form.
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
}
