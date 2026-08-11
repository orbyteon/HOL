using UnityEngine;

// Persistent match statistics (PlayerPrefs-backed). Static helper so both
// GameManager and menu UI can read/write without a scene reference.
//
// RecordWin/RecordLoss take an optional countRecent: PvP passes false so
// real-multiplayer results still count toward wins/losses/streaks but do
// NOT enter the rolling recent-results window that tunes the solo
// adaptive AI (AdaptiveRandomChance in GameManager).
public static class GameStats
{
    const string WinsKey = "StatWins";
    const string LossesKey = "StatLosses";
    const string StreakKey = "StatStreak";
    const string BestStreakKey = "StatBestStreak";
    const string BestGuessesKey = "StatBestGuesses"; // fewest guesses in a win, 0 = none yet
    const string MatchesKey = "StatMatches";
    const string RecentKey = "StatRecentBits"; // last 10 results, bit0 = oldest
    const string RecentCountKey = "StatRecentCount";

    public static int Wins => PlayerPrefs.GetInt(WinsKey, 0);
    public static int Losses => PlayerPrefs.GetInt(LossesKey, 0);
    public static int CurrentStreak => PlayerPrefs.GetInt(StreakKey, 0);
    public static int BestStreak => PlayerPrefs.GetInt(BestStreakKey, 0);
    public static int BestWinningGuesses => PlayerPrefs.GetInt(BestGuessesKey, 0);
    public static int Matches => PlayerPrefs.GetInt(MatchesKey, 0);

    public static void RecordWin(int guessCount, bool countRecent = true)
    {
        PlayerPrefs.SetInt(WinsKey, Wins + 1);
        PlayerPrefs.SetInt(MatchesKey, Matches + 1);

        int streak = CurrentStreak + 1;
        PlayerPrefs.SetInt(StreakKey, streak);
        if (streak > BestStreak)
            PlayerPrefs.SetInt(BestStreakKey, streak);

        if (BestWinningGuesses == 0 || guessCount < BestWinningGuesses)
            PlayerPrefs.SetInt(BestGuessesKey, guessCount);

        if (countRecent) PushRecent(true);
        PlayerPrefs.Save();
    }

    public static void RecordLoss(bool countRecent = true)
    {
        PlayerPrefs.SetInt(LossesKey, Losses + 1);
        PlayerPrefs.SetInt(MatchesKey, Matches + 1);
        PlayerPrefs.SetInt(StreakKey, 0);

        if (countRecent) PushRecent(false);
        PlayerPrefs.Save();
    }

    // Rewarded "save your streak": the loss still counts (it happened), but
    // the streak survives intact. Best-streak bookkeeping is preserved.
    public static void RestoreStreak(int streak)
    {
        PlayerPrefs.SetInt(StreakKey, streak);
        if (streak > BestStreak)
            PlayerPrefs.SetInt(BestStreakKey, streak);
        PlayerPrefs.Save();
    }

    // Rolling window of the last 10 results for the adaptive AI difficulty.
    static void PushRecent(bool won)
    {
        int bits = PlayerPrefs.GetInt(RecentKey, 0);
        int count = PlayerPrefs.GetInt(RecentCountKey, 0);

        bits = ((bits << 1) | (won ? 1 : 0)) & 0x3FF; // keep 10 bits
        if (count < 10) count++;

        PlayerPrefs.SetInt(RecentKey, bits);
        PlayerPrefs.SetInt(RecentCountKey, count);
    }

    // Win rate over the last up-to-10 matches (0..1). Returns -1 with no data.
    public static float RecentWinRate()
    {
        int count = PlayerPrefs.GetInt(RecentCountKey, 0);
        if (count == 0) return -1f;

        int bits = PlayerPrefs.GetInt(RecentKey, 0) & ((1 << count) - 1);
        int wins = 0;
        for (int i = 0; i < count; i++)
            if ((bits & (1 << i)) != 0) wins++;

        return (float)wins / count;
    }

    // One-line summary for menu/end-screen UI.
    public static string Summary()
    {
        return "Wins: " + Wins + "  Losses: " + Losses +
               "\nStreak: " + CurrentStreak + "  Best: " + BestStreak;
    }
}
