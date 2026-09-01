using UnityEngine;

// Persistent data contract for the first-run profile flow. Presentation and
// navigation stay outside this class; a profile is written only after every
// required choice is valid so a partial flow cannot look completed.
public static class OnboardingProfile
{
    public enum GenderChoice
    {
        Boy = 0,
        Girl = 1,
        PreferNotToSay = 2,
    }

    public enum AgeCategory
    {
        Under13 = 0,
        Teen13To17 = 1,
        Adult18Plus = 2,
    }

    public readonly struct Snapshot
    {
        public Snapshot(
            string playerName,
            GenderChoice gender,
            int avatarIndex,
            AgeCategory age,
            int version)
        {
            PlayerName = playerName;
            Gender = gender;
            AvatarIndex = avatarIndex;
            Age = age;
            Version = version;
        }

        public string PlayerName { get; }
        public GenderChoice Gender { get; }
        public int AvatarIndex { get; }
        public AgeCategory Age { get; }
        public int Version { get; }
    }

    public const int CurrentVersion = 1;
    public const int MinNameLength = 3;
    public const int MaxNameLength = 12;
    public static int AvatarCount => OnboardingAvatarCatalog.Count;

    public const string PlayerNameKey = "PlayerName";
    public const string VersionKey = "HOL.Onboarding.Version";
    public const string GenderKey = "HOL.Onboarding.Gender";
    public const string AvatarKey = "HOL.Onboarding.Avatar";
    public const string AgeKey = "HOL.Onboarding.AgeCategory";

    public static bool IsComplete =>
        PlayerPrefs.GetInt(VersionKey, 0) >= CurrentVersion;

    // Existing players who already have the legacy PlayerName key are not
    // forced through a new first-run flow after an update. Fresh installs have
    // neither the version key nor the legacy key and see onboarding.
    public static bool ShouldRun =>
        !IsComplete && !PlayerPrefs.HasKey(PlayerNameKey);

    public static string NormalizeName(string value)
    {
        string normalized = (value ?? string.Empty).Trim();
        return normalized.Length <= MaxNameLength
            ? normalized
            : normalized.Substring(0, MaxNameLength);
    }

    public static bool IsValidName(string value)
    {
        int length = NormalizeName(value).Length;
        return length >= MinNameLength && length <= MaxNameLength;
    }

    public static bool IsValidAvatar(int avatarIndex)
    {
        return OnboardingAvatarCatalog.CanEverSelect(avatarIndex);
    }

    public static bool TryCommit(
        string playerName,
        GenderChoice gender,
        int avatarIndex,
        AgeCategory age)
    {
        string normalized = NormalizeName(playerName);
        if (!IsValidName(normalized) ||
            !System.Enum.IsDefined(typeof(GenderChoice), gender) ||
            !IsValidAvatar(avatarIndex) ||
            !System.Enum.IsDefined(typeof(AgeCategory), age))
            return false;

        PlayerPrefs.SetString(PlayerNameKey, normalized);
        PlayerPrefs.SetInt(GenderKey, (int)gender);
        PlayerPrefs.SetInt(AvatarKey, avatarIndex);
        PlayerPrefs.SetInt(AgeKey, (int)age);
        // Written last: this is the commit marker for the whole profile.
        PlayerPrefs.SetInt(VersionKey, CurrentVersion);
        PlayerPrefs.Save();
        return true;
    }

    public static Snapshot Load()
    {
        return new Snapshot(
            PlayerPrefs.GetString(PlayerNameKey, string.Empty),
            (GenderChoice)PlayerPrefs.GetInt(
                GenderKey, (int)GenderChoice.PreferNotToSay),
            Mathf.Clamp(PlayerPrefs.GetInt(AvatarKey, 0), 0, AvatarCount - 1),
            (AgeCategory)PlayerPrefs.GetInt(
                AgeKey, (int)AgeCategory.Adult18Plus),
            PlayerPrefs.GetInt(VersionKey, 0));
    }
}
