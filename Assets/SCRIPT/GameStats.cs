using UnityEngine;

// Persistent match statistics (PlayerPrefs-backed). Static helper so both
// GameManager and menu UI can read/write without a scene reference.
public static class GameStats
{
    const string WinsKey = "StatWins";
    const string LossesKey = "StatLosses";
    const string StreakKey = "StatStreak";
    const string BestStreakKey = "StatBestStreak";
    const string BestGuessesKey = "StatBestGuesses"; // fewest guesses in a win, 0 = none yet
    const string MatchesKey = "StatMatches";

    public static int Wins => PlayerPrefs.GetInt(WinsKey, 0);
    public static int Losses => PlayerPrefs.GetInt(LossesKey, 0);
    public static int CurrentStreak => PlayerPrefs.GetInt(StreakKey, 0);
    public static int BestStreak => PlayerPrefs.GetInt(BestStreakKey, 0);
    public static int BestWinningGuesses => PlayerPrefs.GetInt(BestGuessesKey, 0);
    public static int Matches => PlayerPrefs.GetInt(MatchesKey, 0);

    public static void RecordWin(int guessCount)
    {
        PlayerPrefs.SetInt(WinsKey, Wins + 1);
        PlayerPrefs.SetInt(MatchesKey, Matches + 1);

        int streak = CurrentStreak + 1;
        PlayerPrefs.SetInt(StreakKey, streak);
        if (streak > BestStreak)
            PlayerPrefs.SetInt(BestStreakKey, streak);

        if (BestWinningGuesses == 0 || guessCount < BestWinningGuesses)
            PlayerPrefs.SetInt(BestGuessesKey, guessCount);

        PlayerPrefs.Save();
    }

    public static void RecordLoss()
    {
        PlayerPrefs.SetInt(LossesKey, Losses + 1);
        PlayerPrefs.SetInt(MatchesKey, Matches + 1);
        PlayerPrefs.SetInt(StreakKey, 0);
        PlayerPrefs.Save();
    }

    // One-line summary for menu/end-screen UI.
    public static string Summary()
    {
        return "Wins: " + Wins + "  Losses: " + Losses +
               "\nStreak: " + CurrentStreak + "  Best: " + BestStreak;
    }
}
