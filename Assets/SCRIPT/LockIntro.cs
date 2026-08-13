using UnityEngine;

// Onboarding state for the Lock.
//
// The duel's core loop is still "higher or lower", and a first match should
// feel like that and nothing else. The Lock is the one decision layered on top,
// so it stays off screen until the player has actually played a round — there
// is nothing sensible to stake it on before their first guess anyway, with a
// hundred candidates still open — and it explains itself in one line when it
// finally appears.
//
// This is presentation only. The server still accepts a Lock on any turn; the
// client simply does not offer one before the player has seen the loop work.
public static class LockIntro
{
    const string ShownKey = "LockIntroShown";
    const string UsedKey = "LockEverUsed";

    // Enough repeats to land, few enough to stop nagging someone who has
    // understood it and simply chooses not to stake it.
    const int MaxIntros = 3;

    public static bool EverUsed
    {
        get { return PlayerPrefs.GetInt(UsedKey, 0) == 1; }
    }

    public static bool ShouldExplain
    {
        get { return !EverUsed && PlayerPrefs.GetInt(ShownKey, 0) < MaxIntros; }
    }

    public static void MarkExplained()
    {
        if (EverUsed) return;

        PlayerPrefs.SetInt(ShownKey, PlayerPrefs.GetInt(ShownKey, 0) + 1);
        PlayerPrefs.Save();
    }

    // Staking it once is the clearest possible signal that the explanation has
    // done its job.
    public static void MarkUsed()
    {
        if (EverUsed) return;

        PlayerPrefs.SetInt(UsedKey, 1);
        PlayerPrefs.Save();
    }
}
