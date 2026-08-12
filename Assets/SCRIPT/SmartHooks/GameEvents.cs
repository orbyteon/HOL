using UnityEngine;

// Central, wiring-free event hub for engagement hooks. Analytics can subscribe
// to semantic events without UI refreshes impersonating new matches.
public static class GameEvents
{
    // Fired exactly once when a match ends: playerWon, guesses (0 on loss).
    public static System.Action<bool, int> OnMatchEnded;

    // Fired whenever persisted match stats change, including streak restoration.
    public static System.Action OnStatsChanged;

    // Fired only when a new calendar day is actually registered.
    public static System.Action<int> OnDailyStreak;

    internal static void MatchEnded(bool playerWon, int guessCount)
    {
        OnMatchEnded?.Invoke(playerWon, guessCount);
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
