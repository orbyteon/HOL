using UnityEngine;

// Central, wiring-free event hub for engagement hooks. UI juice, analytics,
// notifications, and haptics can subscribe here instead of coupling to
// GameManager.
public static class GameEvents
{
    // Fired when a match ends: playerWon, guesses the player needed (0 on loss).
    public static System.Action<bool, int> OnMatchEnded;

    // Fired on app start when a daily-play streak day is counted.
    public static System.Action<int> OnDailyStreak;

    internal static void MatchEnded(bool playerWon, int guessCount)
    {
        OnMatchEnded?.Invoke(playerWon, guessCount);
    }

    internal static void DailyStreak(int days)
    {
        OnDailyStreak?.Invoke(days);
    }
}
