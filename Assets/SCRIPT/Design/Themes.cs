using UnityEngine;

// Cosmetic palettes over the Converging Light design system — the whole UI
// is generated from ConvergingLight's colors at scene build, so a theme is
// just a palette swap plus a scene reload. Themes unlock through play
// (wins, Daily Hunt streaks, a perfect game) and unlocks are sticky: once a
// condition has been observed, the theme stays yours even if the stat later
// resets.
public static class Themes
{
    public struct Palette
    {
        public Color depthTop, depthBottom;
        public Color cyan, magenta, gold;
        public Color nearWhite;
        public Color trackIndigo, panelIndigo, scrimIndigo;
    }

    public enum Unlock { Always, Wins, DailyStreak, PerfectWin }

    public class Theme
    {
        public string id;
        public string nameKey;
        public Unlock unlock;
        public int threshold;
        public Palette palette;
    }

    const string ThemePrefKey = "ThemeId";
    const string UnlockedPrefPrefix = "ThemeUnlocked_";

    public static readonly Theme[] All =
    {
        new Theme
        {
            id = "converging", nameKey = "theme_converging", unlock = Unlock.Always,
            palette = new Palette
            {
                depthTop = new Color(0.035f, 0.035f, 0.110f),
                depthBottom = new Color(0.100f, 0.090f, 0.235f),
                cyan = new Color(0.25f, 0.85f, 1f),
                magenta = new Color(0.83f, 0.35f, 1f),
                gold = new Color(1f, 0.78f, 0.34f),
                nearWhite = new Color(0.91f, 0.93f, 1f),
                trackIndigo = new Color(0.16f, 0.15f, 0.26f),
                panelIndigo = new Color(0.09f, 0.08f, 0.19f),
                scrimIndigo = new Color(0.05f, 0.05f, 0.12f),
            },
        },
        new Theme
        {
            id = "ember", nameKey = "theme_ember", unlock = Unlock.Wins, threshold = 5,
            palette = new Palette
            {
                depthTop = new Color(0.085f, 0.025f, 0.040f),
                depthBottom = new Color(0.220f, 0.070f, 0.065f),
                cyan = new Color(1f, 0.55f, 0.25f),      // amber takes the cool seat
                magenta = new Color(1f, 0.30f, 0.45f),   // rose
                gold = new Color(1f, 0.78f, 0.34f),
                nearWhite = new Color(1f, 0.94f, 0.90f),
                trackIndigo = new Color(0.24f, 0.12f, 0.10f),
                panelIndigo = new Color(0.16f, 0.06f, 0.08f),
                scrimIndigo = new Color(0.10f, 0.03f, 0.04f),
            },
        },
        new Theme
        {
            id = "frost", nameKey = "theme_frost", unlock = Unlock.DailyStreak, threshold = 3,
            palette = new Palette
            {
                depthTop = new Color(0.020f, 0.050f, 0.100f),
                depthBottom = new Color(0.060f, 0.130f, 0.220f),
                cyan = new Color(0.55f, 0.90f, 1f),
                magenta = new Color(0.60f, 0.65f, 1f),   // periwinkle
                gold = new Color(0.95f, 0.88f, 0.65f),   // pale silver-gold
                nearWhite = new Color(0.90f, 0.95f, 1f),
                trackIndigo = new Color(0.13f, 0.18f, 0.26f),
                panelIndigo = new Color(0.07f, 0.11f, 0.18f),
                scrimIndigo = new Color(0.03f, 0.06f, 0.10f),
            },
        },
        new Theme
        {
            // 5 guesses, not 7: ⌈log2(100)⌉ = 7 is merely competent binary
            // search, which unlocked the "rarest" theme on the first decent
            // win — before Ember's 5 wins or Frost's 3-day streak.
            id = "mono", nameKey = "theme_mono", unlock = Unlock.PerfectWin, threshold = 5,
            palette = new Palette
            {
                depthTop = new Color(0.050f, 0.050f, 0.060f),
                depthBottom = new Color(0.130f, 0.130f, 0.150f),
                cyan = new Color(0.80f, 0.80f, 0.84f),   // the seam turns silver
                magenta = new Color(0.52f, 0.52f, 0.58f),
                gold = new Color(1f, 0.78f, 0.34f),      // the one metal survives
                nearWhite = new Color(0.93f, 0.93f, 0.95f),
                trackIndigo = new Color(0.18f, 0.18f, 0.20f),
                panelIndigo = new Color(0.10f, 0.10f, 0.12f),
                scrimIndigo = new Color(0.05f, 0.05f, 0.06f),
            },
        },
    };

    static Palette? active;

    public static Palette Active
    {
        get
        {
            if (active == null)
                active = Find(PlayerPrefs.GetString(ThemePrefKey, "converging")).palette;
            return active.Value;
        }
    }

    public static string CurrentId => PlayerPrefs.GetString(ThemePrefKey, "converging");

    static Theme Find(string id)
    {
        foreach (var t in All)
            if (t.id == id) return t;
        return All[0];
    }

    // Sticky unlock: check the live condition once, then remember forever.
    public static bool IsUnlocked(Theme theme)
    {
        if (theme.unlock == Unlock.Always) return true;
        if (PlayerPrefs.GetInt(UnlockedPrefPrefix + theme.id, 0) == 1) return true;

        bool met =
            (theme.unlock == Unlock.Wins && GameStats.Wins >= theme.threshold) ||
            (theme.unlock == Unlock.DailyStreak &&
                PlayerPrefs.GetInt(DailyHunt.StreakPrefKey, 0) >= theme.threshold) ||
            (theme.unlock == Unlock.PerfectWin &&
                GameStats.BestWinningGuesses > 0 && GameStats.BestWinningGuesses <= theme.threshold);

        if (met)
        {
            PlayerPrefs.SetInt(UnlockedPrefPrefix + theme.id, 1);
            PlayerPrefs.Save();
        }
        return met;
    }

    public static string LockHint(Theme theme)
    {
        switch (theme.unlock)
        {
            case Unlock.Wins: return L10n.Get("theme_locked_wins", theme.threshold);
            case Unlock.DailyStreak: return L10n.Get("theme_locked_daily", theme.threshold);
            case Unlock.PerfectWin: return L10n.Get("theme_locked_perfect", theme.threshold);
            default: return "";
        }
    }

    // Applies and persists a theme. The UI is baked from the palette at
    // scene build, so the caller reloads the scene for it to take effect.
    public static void Apply(Theme theme)
    {
        PlayerPrefs.SetString(ThemePrefKey, theme.id);
        PlayerPrefs.Save();
        active = theme.palette;
        ConvergingLightFX.InvalidateThemedSprites();
    }
}
