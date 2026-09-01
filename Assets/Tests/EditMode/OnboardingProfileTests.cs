using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class OnboardingProfileTests
{
    const BindingFlags StaticFlags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    const string PlayerNameKey = "PlayerName";
    const string VersionKey = "HOL.Onboarding.Version";
    const string GenderKey = "HOL.Onboarding.Gender";
    const string AvatarKey = "HOL.Onboarding.Avatar";
    const string AgeKey = "HOL.Onboarding.AgeCategory";

    readonly string[] keys =
    {
        PlayerNameKey, VersionKey, GenderKey, AvatarKey, AgeKey,
    };

    bool[] hadKey;
    string savedName;
    int[] savedInts;

    [SetUp]
    public void SetUp()
    {
        hadKey = new bool[keys.Length];
        savedInts = new int[keys.Length];
        for (int index = 0; index < keys.Length; index++)
        {
            hadKey[index] = PlayerPrefs.HasKey(keys[index]);
            if (index == 0)
                savedName = PlayerPrefs.GetString(keys[index], string.Empty);
            else
                savedInts[index] = PlayerPrefs.GetInt(keys[index], 0);
            PlayerPrefs.DeleteKey(keys[index]);
        }
        PlayerPrefs.Save();
    }

    [TearDown]
    public void TearDown()
    {
        for (int index = 0; index < keys.Length; index++)
        {
            if (!hadKey[index])
            {
                PlayerPrefs.DeleteKey(keys[index]);
                continue;
            }

            if (index == 0)
                PlayerPrefs.SetString(keys[index], savedName);
            else
                PlayerPrefs.SetInt(keys[index], savedInts[index]);
        }
        PlayerPrefs.Save();
    }

    [Test]
    public void FreshInstallRunsButLegacyPlayerSkipsOnboarding()
    {
        Assert.That(GetBool("ShouldRun"), Is.True);
        Assert.That(GetBool("IsComplete"), Is.False);

        PlayerPrefs.SetString(PlayerNameKey, "ReturningPlayer");
        Assert.That(GetBool("ShouldRun"), Is.False,
            "An existing PlayerName must preserve the pre-onboarding returning-player path.");
    }

    [Test]
    public void NameNormalizationTrimsAndCapsTheProductionLimit()
    {
        Assert.That(InvokeStatic("NormalizeName", "  Andreas  "),
            Is.EqualTo("Andreas"));
        Assert.That(InvokeStatic("NormalizeName", "ABCDEFGHIJKLMNO"),
            Is.EqualTo("ABCDEFGHIJKL"));
        Assert.That(InvokeStatic("IsValidName", "ab"), Is.False);
        Assert.That(InvokeStatic("IsValidName", "abc"), Is.True);
    }

    [Test]
    public void InvalidProfileNeverWritesTheCompletionMarker()
    {
        Assert.That(TryCommit("ab", 0, 0, 0), Is.False);
        Assert.That(PlayerPrefs.HasKey(VersionKey), Is.False);
        Assert.That(PlayerPrefs.HasKey(PlayerNameKey), Is.False);

        Assert.That(TryCommit("Marinos", 0, 12, 0), Is.False);
        Assert.That(PlayerPrefs.HasKey(VersionKey), Is.False);
        Assert.That(PlayerPrefs.HasKey(PlayerNameKey), Is.False);

        Assert.That(TryCommit("Marinos", 0, 11, 0), Is.False,
            "The explicitly locked catalog entry must never be committed.");
    }

    [Test]
    public void ValidProfilePersistsEveryChoiceAndMarksCompletion()
    {
        Assert.That(TryCommit("  Marinos  ", 1, 7, 2), Is.True);
        Assert.That(PlayerPrefs.GetString(PlayerNameKey), Is.EqualTo("Marinos"));
        Assert.That(PlayerPrefs.GetInt(GenderKey), Is.EqualTo(1));
        Assert.That(PlayerPrefs.GetInt(AvatarKey), Is.EqualTo(7));
        Assert.That(PlayerPrefs.GetInt(AgeKey), Is.EqualTo(2));
        Assert.That(PlayerPrefs.GetInt(VersionKey), Is.EqualTo(1));
        Assert.That(GetBool("IsComplete"), Is.True);
        Assert.That(GetBool("ShouldRun"), Is.False);
    }

    [Test]
    public void AvatarCatalogCarriesAllFourAvailabilityKindsInData()
    {
        Type catalog = RuntimeType("OnboardingAvatarCatalog");
        Assert.That(catalog.GetProperty("Count", StaticFlags)
            .GetValue(null, null), Is.EqualTo(12));
        MethodInfo get = catalog.GetMethod("Get", StaticFlags);
        var kinds = new System.Collections.Generic.HashSet<string>();
        for (int index = 0; index < 12; index++)
        {
            object entry = get.Invoke(null, new object[] { index });
            Type entryType = entry.GetType();
            Assert.That(entryType.GetProperty("ResourcePath")
                .GetValue(entry, null), Is.Not.Null.And.Not.Empty);
            kinds.Add(entryType.GetProperty("Availability")
                .GetValue(entry, null).ToString());
        }
        CollectionAssert.AreEquivalent(
            new[] { "Free", "Coins", "Experience", "Locked" }, kinds);
    }

    [Test]
    public void CommittedAvatarReadUsesTheCanonicalCatalogAndRejectsFallbackCases()
    {
        AssertCommittedAvatar(false, -1, "missing profile");

        PlayerPrefs.SetInt(AvatarKey, 1);
        AssertCommittedAvatar(false, -1, "incomplete profile");
        PlayerPrefs.SetInt(VersionKey, 1);

        Type catalog = RuntimeType("OnboardingAvatarCatalog");
        int count = (int)catalog.GetProperty("Count", StaticFlags)
            .GetValue(null, null);
        MethodInfo get = catalog.GetMethod("Get", StaticFlags);
        MethodInfo valid = RuntimeType("OnboardingProfile")
            .GetMethod("IsValidAvatar", StaticFlags);

        for (int index = 0; index < count; index++)
        {
            bool selectable = (bool)valid.Invoke(null, new object[] { index });
            PlayerPrefs.SetInt(AvatarKey, index);
            AssertCommittedAvatar(selectable, selectable ? index : -1,
                "catalog avatar " + index);

            object entry = get.Invoke(null, new object[] { index });
            string resource = (string)entry.GetType()
                .GetProperty("ResourcePath").GetValue(entry, null);
            if (selectable)
                Assert.That(Resources.Load<Sprite>(resource), Is.Not.Null,
                    "Missing canonical avatar resource " + resource);
        }

        PlayerPrefs.DeleteKey(AvatarKey);
        AssertCommittedAvatar(false, -1, "missing avatar key");
        PlayerPrefs.SetString(AvatarKey, string.Empty);
        AssertCommittedAvatar(false, -1, "empty legacy avatar");
        PlayerPrefs.SetString(AvatarKey, "avatar_02_cap_boy");
        AssertCommittedAvatar(false, -1, "string legacy avatar");
        PlayerPrefs.SetInt(AvatarKey, -1);
        AssertCommittedAvatar(false, -1, "negative avatar");
        PlayerPrefs.SetInt(AvatarKey, count);
        AssertCommittedAvatar(false, -1, "out-of-range avatar");
    }

    static bool TryCommit(string name, int gender, int avatar, int age)
    {
        Type profile = RuntimeType("OnboardingProfile");
        Type genderType = profile.GetNestedType("GenderChoice", BindingFlags.Public);
        Type ageType = profile.GetNestedType("AgeCategory", BindingFlags.Public);
        MethodInfo commit = profile.GetMethod("TryCommit", StaticFlags);
        return (bool)commit.Invoke(null, new[]
        {
            name,
            Enum.ToObject(genderType, gender),
            avatar,
            Enum.ToObject(ageType, age),
        });
    }

    static void AssertCommittedAvatar(
        bool expectedResult,
        int expectedAvatar,
        string label)
    {
        object[] arguments = { -1 };
        bool result = (bool)RuntimeType("OnboardingProfile")
            .GetMethod("TryLoadCommittedAvatar", StaticFlags)
            .Invoke(null, arguments);
        Assert.That(result, Is.EqualTo(expectedResult), label);
        Assert.That((int)arguments[0], Is.EqualTo(expectedAvatar), label);
    }

    static bool GetBool(string property)
    {
        return (bool)RuntimeType("OnboardingProfile")
            .GetProperty(property, StaticFlags).GetValue(null, null);
    }

    static object InvokeStatic(string method, params object[] arguments)
    {
        return RuntimeType("OnboardingProfile")
            .GetMethod(method, StaticFlags).Invoke(null, arguments);
    }

    static Type RuntimeType(string name)
    {
        Type type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, name + " runtime type is missing.");
        return type;
    }
}
