using System;

// Data authority for onboarding avatar presentation and eligibility. The
// catalog deliberately stores rules and requirements separately from artwork;
// sprites remain presentation assets loaded by SplashDesign.
public static class OnboardingAvatarCatalog
{
    public enum AvailabilityKind
    {
        Free = 0,
        Coins = 1,
        Experience = 2,
        Locked = 3,
    }

    [Flags]
    public enum Category
    {
        None = 0,
        Boys = 1 << 0,
        Girls = 1 << 1,
        Cool = 1 << 2,
        Epic = 1 << 3,
        All = Boys | Girls | Cool | Epic,
    }

    public readonly struct BalanceSnapshot
    {
        public BalanceSnapshot(
            bool hasCoins,
            int coins,
            bool hasExperience,
            int experience)
        {
            HasCoins = hasCoins;
            Coins = Math.Max(0, coins);
            HasExperience = hasExperience;
            Experience = Math.Max(0, experience);
        }

        public bool HasCoins { get; }
        public int Coins { get; }
        public bool HasExperience { get; }
        public int Experience { get; }

        public static BalanceSnapshot Unavailable =>
            new BalanceSnapshot(false, 0, false, 0);
    }

    public readonly struct Entry
    {
        public Entry(
            int index,
            string resourcePath,
            Category categories,
            AvailabilityKind availability,
            int requirement)
        {
            Index = index;
            ResourcePath = resourcePath;
            Categories = categories;
            Availability = availability;
            Requirement = Math.Max(0, requirement);
        }

        public int Index { get; }
        public string ResourcePath { get; }
        public Category Categories { get; }
        public AvailabilityKind Availability { get; }
        public int Requirement { get; }

        public bool Matches(Category filter)
        {
            return filter == Category.All ||
                filter == Category.None ||
                (Categories & filter) != 0;
        }
    }

    static readonly Entry[] Catalog =
    {
        new Entry(0, "onboarding/avatars/avatar_01_teal_boy",
            Category.Boys, AvailabilityKind.Free, 0),
        new Entry(1, "onboarding/avatars/avatar_02_cap_boy",
            Category.Boys | Category.Cool, AvailabilityKind.Free, 0),
        new Entry(2, "onboarding/avatars/avatar_03_glasses_boy",
            Category.Boys, AvailabilityKind.Free, 0),
        new Entry(3, "onboarding/avatars/avatar_04_blue_hair",
            Category.Boys | Category.Cool, AvailabilityKind.Coins, 75),
        new Entry(4, "onboarding/avatars/avatar_05_ponytail_girl",
            Category.Girls, AvailabilityKind.Free, 0),
        new Entry(5, "onboarding/avatars/avatar_06_cat_ear_girl",
            Category.Girls | Category.Cool, AvailabilityKind.Coins, 75),
        new Entry(6, "onboarding/avatars/avatar_07_bubblegum_girl",
            Category.Girls | Category.Cool, AvailabilityKind.Free, 0),
        new Entry(7, "onboarding/avatars/avatar_08_gold_hoodie_girl",
            Category.Girls | Category.Epic, AvailabilityKind.Coins, 150),
        new Entry(8, "onboarding/avatars/avatar_09_green_cap",
            Category.Boys | Category.Cool, AvailabilityKind.Experience, 150),
        new Entry(9, "onboarding/avatars/avatar_10_silver_hair",
            Category.Girls | Category.Epic, AvailabilityKind.Experience, 250),
        new Entry(10, "onboarding/avatars/avatar_11_black_red_hair",
            Category.Boys | Category.Cool | Category.Epic,
            AvailabilityKind.Experience, 250),
        new Entry(11, "onboarding/avatars/avatar_12_teal_braids",
            Category.Girls | Category.Epic, AvailabilityKind.Locked, 0),
    };

    public static int Count => Catalog.Length;

    public static bool IsDefined(int index)
    {
        return index >= 0 && index < Catalog.Length;
    }

    public static Entry Get(int index)
    {
        if (!IsDefined(index))
            throw new ArgumentOutOfRangeException(nameof(index));
        return Catalog[index];
    }

    public static bool CanEverSelect(int index)
    {
        return IsDefined(index) &&
            Catalog[index].Availability != AvailabilityKind.Locked;
    }

    public static bool IsSelectable(int index, BalanceSnapshot balances)
    {
        if (!IsDefined(index)) return false;
        Entry entry = Catalog[index];
        switch (entry.Availability)
        {
            case AvailabilityKind.Free:
                return true;
            case AvailabilityKind.Coins:
                return balances.HasCoins && balances.Coins >= entry.Requirement;
            case AvailabilityKind.Experience:
                return balances.HasExperience &&
                    balances.Experience >= entry.Requirement;
            default:
                return false;
        }
    }
}
