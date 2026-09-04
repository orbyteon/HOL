using UnityEngine;

// Shared presentation-side reader for the canonical Onboarding avatar
// contract. This class deliberately keeps no cache and writes no PlayerPrefs,
// so scene changes, language changes and rematches always observe the latest
// valid committed selection.
public static class PlayerProfileAvatarResolver
{
    public const string FallbackResourcePath =
        "reference/player_cyan_exact";
    public const string CircularApertureResourcePath =
        "onboarding/icons/onboarding_indicator_disc_neutral";

    public static Sprite Resolve()
    {
        Sprite fallback = Resources.Load<Sprite>(FallbackResourcePath);
        if (!OnboardingProfile.TryLoadCommittedAvatar(out int avatarIndex))
            return fallback;

        OnboardingAvatarCatalog.Entry entry =
            OnboardingAvatarCatalog.Get(avatarIndex);
        if (string.IsNullOrWhiteSpace(entry.ResourcePath))
            return fallback;

        Sprite selected = Resources.Load<Sprite>(entry.ResourcePath);
        return selected != null ? selected : fallback;
    }
}
