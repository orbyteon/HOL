using System;
using System.Globalization;
using UnityEngine;

// Daily-play streak hook. Counts one streak day per local calendar day and
// emits GameEvents.OnDailyStreak only when that day is newly registered.
public class DailyStreak : MonoBehaviour
{
    const string LastPlayDateKey = "DailyLastPlayDate"; // yyyy-MM-dd
    const string StreakKey = "DailyStreakDays";

    public static int CurrentStreakDays => PlayerPrefs.GetInt(StreakKey, 0);

    void Start()
    {
        RegisterToday();
    }

    public static void RegisterToday()
    {
        // Invariant culture on both sides: the device culture's default
        // calendar (e.g. Buddhist) would otherwise store non-Gregorian years
        // that break the comparison after a system-language change.
        string today = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string last = PlayerPrefs.GetString(LastPlayDateKey, "");

        // Scene reloads on the same day are not new engagement events.
        if (last == today)
            return;

        bool consecutive = false;
        if (!string.IsNullOrEmpty(last))
        {
            DateTime lastDate;
            if (DateTime.TryParseExact(last, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out lastDate))
                consecutive = (DateTime.Now.Date - lastDate.Date).Days == 1;
        }

        int streak = consecutive ? CurrentStreakDays + 1 : 1;

        PlayerPrefs.SetString(LastPlayDateKey, today);
        PlayerPrefs.SetInt(StreakKey, streak);
        PlayerPrefs.Save();

        GameEvents.DailyStreak(streak);
    }
}
