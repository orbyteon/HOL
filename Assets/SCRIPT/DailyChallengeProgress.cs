using System;
using UnityEngine;

// Persistent, UTC-day-scoped state for the Daily Challenge mission board.
// This is deliberately a domain owner only: it does not create UI, choose
// sprites or move RectTransforms. DailyHuntVisuals remains the one Daily Hunt
// presentation owner.
public static class DailyChallengeProgress
{
    public const int WinTarget = 1;
    public const int CorrectGuessTarget = 3;
    public const int RoomShareTarget = 1;
    public const int RewardPoints = 500;
    public const int PointsMilestone = 1500;

    const string DayKey = "DailyChallengeDay";
    const string WinsKey = "DailyChallengeWins";
    const string CorrectGuessesKey = "DailyChallengeCorrectGuesses";
    const string RoomsSharedKey = "DailyChallengeRoomsShared";
    const string RewardClaimedKey = "DailyChallengeRewardClaimed";
    const string PointsKey = "DailyChallengePoints";

    static readonly DateTime Epoch =
        new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public struct Snapshot
    {
        public int Day;
        public int Wins;
        public int CorrectGuesses;
        public int RoomsShared;
        public bool RewardClaimed;
        public int Points;

        public bool Complete =>
            Wins >= WinTarget &&
            CorrectGuesses >= CorrectGuessTarget &&
            RoomsShared >= RoomShareTarget;

        public int PointsTowardMilestone =>
            PointsMilestone <= 0 ? 0 : Points % PointsMilestone;
    }

    public static event Action Changed;

    public static int CurrentUtcDayNumber => UtcDayNumber(DateTime.UtcNow);

    public static Snapshot Current
    {
        get
        {
            int day = EnsureDay(CurrentUtcDayNumber);
            ReconcileReward();
            return Read(day);
        }
    }

    public static TimeSpan TimeUntilReset
    {
        get { return UntilNextUtcDay(DateTime.UtcNow); }
    }

    internal static int UtcDayNumber(DateTime utcNow)
    {
        DateTime normalized = utcNow.Kind == DateTimeKind.Utc
            ? utcNow
            : utcNow.ToUniversalTime();
        return (normalized.Date - Epoch).Days + 1;
    }

    internal static TimeSpan UntilNextUtcDay(DateTime utcNow)
    {
        DateTime normalized = utcNow.Kind == DateTimeKind.Utc
            ? utcNow
            : utcNow.ToUniversalTime();
        TimeSpan remaining = normalized.Date.AddDays(1) - normalized;
        return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    internal static void RecordWin()
    {
        RecordProgress(WinsKey, WinTarget);
    }

    internal static void RecordCorrectGuess()
    {
        RecordProgress(CorrectGuessesKey, CorrectGuessTarget);
    }

    internal static void RecordRoomShared()
    {
        RecordProgress(RoomsSharedKey, RoomShareTarget);
    }

    internal static int EnsureDay(int requestedDay)
    {
        int storedDay = PlayerPrefs.GetInt(DayKey, 0);

        // Match Daily Hunt's clock-rollback protection: once a later UTC day
        // has been opened, moving the device clock backwards must not reopen an
        // already rewarded mission set.
        int effectiveDay = Mathf.Max(requestedDay, storedDay);
        if (storedDay >= effectiveDay)
            return effectiveDay;

        PlayerPrefs.SetInt(DayKey, effectiveDay);
        PlayerPrefs.SetInt(WinsKey, 0);
        PlayerPrefs.SetInt(CorrectGuessesKey, 0);
        PlayerPrefs.SetInt(RoomsSharedKey, 0);
        PlayerPrefs.SetInt(RewardClaimedKey, 0);
        PlayerPrefs.Save();
        Changed?.Invoke();
        return effectiveDay;
    }

    static void RecordProgress(string key, int target)
    {
        EnsureDay(CurrentUtcDayNumber);
        int current = PlayerPrefs.GetInt(key, 0);
        if (current >= target)
        {
            ReconcileReward();
            return;
        }

        PlayerPrefs.SetInt(key, Mathf.Min(target, current + 1));
        GrantRewardIfComplete();
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    // The approved screen has no separate claim action. Completing the final
    // real mission therefore grants the displayed reward immediately. The
    // claimed marker and balance are written in the same save, making repeated
    // events and screen refreshes idempotent.
    static void GrantRewardIfComplete()
    {
        if (PlayerPrefs.GetInt(RewardClaimedKey, 0) == 1)
            return;
        if (PlayerPrefs.GetInt(WinsKey, 0) < WinTarget ||
            PlayerPrefs.GetInt(CorrectGuessesKey, 0) < CorrectGuessTarget ||
            PlayerPrefs.GetInt(RoomsSharedKey, 0) < RoomShareTarget)
            return;

        PlayerPrefs.SetInt(RewardClaimedKey, 1);
        int points = Mathf.Max(0, PlayerPrefs.GetInt(PointsKey, 0));
        PlayerPrefs.SetInt(PointsKey, points + RewardPoints);
    }

    static void ReconcileReward()
    {
        int claimedBefore = PlayerPrefs.GetInt(RewardClaimedKey, 0);
        GrantRewardIfComplete();
        if (claimedBefore == PlayerPrefs.GetInt(RewardClaimedKey, 0))
            return;

        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    static Snapshot Read(int day)
    {
        return new Snapshot
        {
            Day = day,
            Wins = Mathf.Clamp(PlayerPrefs.GetInt(WinsKey, 0), 0, WinTarget),
            CorrectGuesses = Mathf.Clamp(
                PlayerPrefs.GetInt(CorrectGuessesKey, 0), 0, CorrectGuessTarget),
            RoomsShared = Mathf.Clamp(
                PlayerPrefs.GetInt(RoomsSharedKey, 0), 0, RoomShareTarget),
            RewardClaimed = PlayerPrefs.GetInt(RewardClaimedKey, 0) == 1,
            Points = Mathf.Max(0, PlayerPrefs.GetInt(PointsKey, 0)),
        };
    }
}
