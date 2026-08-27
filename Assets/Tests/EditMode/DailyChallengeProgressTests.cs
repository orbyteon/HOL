using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Reflection keeps the EditMode asmdef independent from Assembly-CSharp while
// still exercising the real persisted Daily Challenge domain implementation.
public sealed class DailyChallengeProgressTests
{
    static readonly string[] Keys =
    {
        "DailyChallengeDay",
        "DailyChallengeWins",
        "DailyChallengeCorrectGuesses",
        "DailyChallengeRoomsShared",
        "DailyChallengeRewardClaimed",
        "DailyChallengePoints",
    };

    [SetUp]
    public void SetUp()
    {
        Clear();
    }

    [TearDown]
    public void TearDown()
    {
        Clear();
    }

    [Test]
    public void ThreeTruthfulMissionsGrantRewardExactlyOnce()
    {
        int day = GetStatic<int>("CurrentUtcDayNumber");
        Invoke("EnsureDay", day);

        Invoke("RecordWin");
        Invoke("RecordCorrectGuess");
        Invoke("RecordCorrectGuess");
        Invoke("RecordCorrectGuess");
        Invoke("RecordRoomShared");

        object state = GetStatic<object>("Current");
        Assert.That(GetField<int>(state, "Wins"), Is.EqualTo(1));
        Assert.That(GetField<int>(state, "CorrectGuesses"), Is.EqualTo(3));
        Assert.That(GetField<int>(state, "RoomsShared"), Is.EqualTo(1));
        Assert.That(GetField<bool>(state, "RewardClaimed"), Is.True);
        Assert.That(GetField<int>(state, "Points"), Is.EqualTo(500));
        Assert.That(GetProperty<bool>(state, "Complete"), Is.True);

        // Repeated callbacks and UI reads cannot mint the reward twice.
        Invoke("RecordWin");
        Invoke("RecordCorrectGuess");
        Invoke("RecordRoomShared");
        state = GetStatic<object>("Current");
        Assert.That(GetField<int>(state, "Points"), Is.EqualTo(500));
    }

    [Test]
    public void NewUtcDayResetsMissionsButPreservesEarnedPoints()
    {
        int day = GetStatic<int>("CurrentUtcDayNumber");
        Invoke("EnsureDay", day);
        Invoke("RecordWin");
        Invoke("RecordCorrectGuess");
        Invoke("RecordCorrectGuess");
        Invoke("RecordCorrectGuess");
        Invoke("RecordRoomShared");

        Invoke("EnsureDay", day + 1);
        object state = GetStatic<object>("Current");
        Assert.That(GetField<int>(state, "Day"), Is.EqualTo(day + 1));
        Assert.That(GetField<int>(state, "Wins"), Is.Zero);
        Assert.That(GetField<int>(state, "CorrectGuesses"), Is.Zero);
        Assert.That(GetField<int>(state, "RoomsShared"), Is.Zero);
        Assert.That(GetField<bool>(state, "RewardClaimed"), Is.False);
        Assert.That(GetField<int>(state, "Points"), Is.EqualTo(500));
    }

    [Test]
    public void ResetCountdownUsesTheNextUtcMidnight()
    {
        object value = Invoke(
            "UntilNextUtcDay",
            new DateTime(2026, 8, 25, 23, 59, 30, DateTimeKind.Utc));
        Assert.That((TimeSpan)value, Is.EqualTo(TimeSpan.FromSeconds(30)));
    }

    static Type DomainType()
    {
        Type type = Type.GetType("DailyChallengeProgress, Assembly-CSharp");
        Assert.That(type, Is.Not.Null);
        return type;
    }

    static object Invoke(string name, params object[] args)
    {
        MethodInfo method = DomainType().GetMethod(
            name,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, name);
        return method.Invoke(null, args);
    }

    static T GetStatic<T>(string name)
    {
        PropertyInfo property = DomainType().GetProperty(
            name,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, name);
        return (T)property.GetValue(null);
    }

    static T GetField<T>(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        return (T)field.GetValue(target);
    }

    static T GetProperty<T>(object target, string name)
    {
        PropertyInfo property = target.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, name);
        return (T)property.GetValue(target);
    }

    static void Clear()
    {
        foreach (string key in Keys)
            PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }
}
