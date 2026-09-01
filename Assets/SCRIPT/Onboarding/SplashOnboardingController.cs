using System;
using UnityEngine;

// Functional owner for onboarding state and callbacks. SplashDesign observes
// this controller and remains the sole presentation/layout authority.
[DisallowMultipleComponent]
public sealed class SplashOnboardingController : MonoBehaviour
{
    public enum Step
    {
        Welcome = 0,
        Name = 1,
        Gender = 2,
        Avatar = 3,
        Age = 4,
    }

    SplashLoader loader;
    string draftName = string.Empty;
    int selectedGender = -1;
    int selectedAvatar = -1;
    int selectedAge = -1;
    OnboardingAvatarCatalog.BalanceSnapshot avatarBalances =
        OnboardingAvatarCatalog.BalanceSnapshot.Unavailable;

    public event Action StateChanged;

    public Step CurrentStep { get; private set; } = Step.Welcome;
    public string DraftName => draftName;
    public int SelectedGender => selectedGender;
    public int SelectedAvatar => selectedAvatar;
    public int SelectedAge => selectedAge;
    public int CurrentStageNumber => (int)CurrentStep + 1;
    public bool CanGoBack => CurrentStep != Step.Welcome;
    public bool CanSkipGender => CurrentStep == Step.Gender;
    public bool HasLiveAvatarBalances =>
        avatarBalances.HasCoins || avatarBalances.HasExperience;

    public bool CanContinue
    {
        get
        {
            switch (CurrentStep)
            {
                case Step.Welcome:
                    return true;
                case Step.Name:
                    return OnboardingProfile.IsValidName(draftName);
                case Step.Gender:
                    return selectedGender >= 0;
                case Step.Avatar:
                    return IsAvatarSelectable(selectedAvatar);
                case Step.Age:
                    return selectedAge >= 0;
                default:
                    return false;
            }
        }
    }

    public void Initialize(SplashLoader sceneLoader)
    {
        loader = sceneLoader;
        CurrentStep = Step.Welcome;
        draftName = string.Empty;
        selectedGender = -1;
        selectedAvatar = -1;
        selectedAge = -1;
        avatarBalances = OnboardingAvatarCatalog.BalanceSnapshot.Unavailable;
        NotifyChanged();
    }

    public void SetName(string value)
    {
        string normalized = OnboardingProfile.NormalizeName(value);
        if (draftName == normalized) return;
        draftName = normalized;
        NotifyChanged();
    }

    public void SelectGender(int value)
    {
        if (!Enum.IsDefined(typeof(OnboardingProfile.GenderChoice), value))
            return;
        if (selectedGender == value) return;
        selectedGender = value;
        NotifyChanged();
    }

    public void SelectAvatar(int value)
    {
        if (!IsAvatarSelectable(value)) return;
        if (selectedAvatar == value) return;
        selectedAvatar = value;
        NotifyChanged();
    }

    public void SelectAge(int value)
    {
        if (!Enum.IsDefined(typeof(OnboardingProfile.AgeCategory), value))
            return;
        if (selectedAge == value) return;
        selectedAge = value;
        NotifyChanged();
    }

    public bool Advance()
    {
        if (!CanContinue) return false;

        switch (CurrentStep)
        {
            case Step.Welcome:
                CurrentStep = Step.Name;
                break;
            case Step.Name:
                CurrentStep = Step.Gender;
                break;
            case Step.Gender:
                CurrentStep = Step.Avatar;
                break;
            case Step.Avatar:
                CurrentStep = Step.Age;
                break;
            case Step.Age:
                if (!Commit()) return false;
                if (loader == null) loader = FindObjectOfType<SplashLoader>();
                if (loader != null) loader.ContinueToMainMenu();
                return true;
            default:
                return false;
        }

        NotifyChanged();
        return true;
    }

    public bool GoBack()
    {
        if (!CanGoBack) return false;
        CurrentStep = (Step)((int)CurrentStep - 1);
        NotifyChanged();
        return true;
    }

    public bool SkipGender()
    {
        if (!CanSkipGender) return false;
        selectedGender = (int)OnboardingProfile.GenderChoice.PreferNotToSay;
        CurrentStep = Step.Avatar;
        NotifyChanged();
        return true;
    }

    public bool IsAvatarSelectable(int value)
    {
        return OnboardingAvatarCatalog.IsSelectable(value, avatarBalances);
    }

#if UNITY_EDITOR
    // Editor-only deterministic fixture. Production never invents account
    // balances when no currency / XP authority is available.
    public void ConfigureAvatarBalancesForCapture(int coins, int experience)
    {
        avatarBalances = new OnboardingAvatarCatalog.BalanceSnapshot(
            true, coins, true, experience);
        if (selectedAvatar >= 0 && !IsAvatarSelectable(selectedAvatar))
            selectedAvatar = -1;
        NotifyChanged();
    }
#endif

    bool Commit()
    {
        return OnboardingProfile.TryCommit(
            draftName,
            (OnboardingProfile.GenderChoice)selectedGender,
            selectedAvatar,
            (OnboardingProfile.AgeCategory)selectedAge);
    }

    void NotifyChanged()
    {
        StateChanged?.Invoke();
    }
}
