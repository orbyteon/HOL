using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Assembly-CSharp is a genuine Unity boundary for this EditMode assembly.
public sealed class DailyHuntRolloverTests
{
    readonly Dictionary<string, int> previous = new Dictionary<string, int>();
    string previousTrail;
    bool hadTrail;
    GameObject root;
    Component hunt;
    int today;
    const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly string[] Keys = {
        "DailyHuntDay", "DailyHuntUsed", "DailyHuntDone", "DailyHuntFound",
        "DailyHuntRevived", "DailyHuntMin", "DailyHuntMax", "DailyHuntStreak",
        "DailyHuntLastFound", "DailyHuntPendingRevive"
    };
    string rewardKey;

    [SetUp]
    public void SetUp()
    {
        rewardKey = (string)Type.GetType("AdsManager, Assembly-CSharp")
            .GetField("PendingRewardEarnedKey", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .GetRawConstantValue();
        previous.Clear();
        foreach (string key in Keys) SaveAndClear(key);
        SaveAndClear(rewardKey);
        hadTrail = PlayerPrefs.HasKey("DailyHuntTrail");
        previousTrail = PlayerPrefs.GetString("DailyHuntTrail", "");
        PlayerPrefs.DeleteKey("DailyHuntTrail");
        today = 20000;
        root = new GameObject("DailyHuntRolloverTest");
        hunt = root.AddComponent(Type.GetType("DailyHunt, Assembly-CSharp"));
        hunt.GetType().GetField("currentUtcDay", Instance).SetValue(hunt, (Func<int>)(() => today));
        Call("EnsureToday");
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(root);
        foreach (string key in Keys) Restore(key);
        Restore(rewardKey);
        if (hadTrail) PlayerPrefs.SetString("DailyHuntTrail", previousTrail);
        else PlayerPrefs.DeleteKey("DailyHuntTrail");
        PlayerPrefs.Save();
    }

    [Test]
    public void GuessAcrossMidnightResetsAndDoesNotSpendANewDayAttempt()
    {
        PlayerPrefs.SetInt("DailyHuntUsed", 6);
        Call("EnsureToday");
        today++;
        // No input exists: rollover must return before reading yesterday's input.
        Call("SubmitGuess");
        AssertFreshDay();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void LateReviveCallbackCannotRewardOrFailTheNewDay(bool earned)
    {
        int requestedDay = today;
        PlayerPrefs.SetInt("DailyHuntPendingRevive", requestedDay);
        PlayerPrefs.SetInt(rewardKey, earned ? 1 : 0);
        today++;
        Call("CompleteRevive", requestedDay, earned);
        AssertFreshDay();
        Assert.That(PlayerPrefs.HasKey("DailyHuntPendingRevive"), Is.False);
        Assert.That(PlayerPrefs.HasKey(rewardKey), Is.False);
    }

    [Test]
    public void ClockRollbackCannotResetAttempts()
    {
        PlayerPrefs.SetInt("DailyHuntUsed", 4);
        Call("EnsureToday");
        today--;
        Assert.That(Call("ReconcileDay"), Is.False);
        Assert.That(PlayerPrefs.GetInt("DailyHuntUsed"), Is.EqualTo(4));
        Assert.That(PlayerPrefs.GetInt("DailyHuntDay"), Is.EqualTo(today + 1));
    }

    void AssertFreshDay()
    {
        Assert.That(PlayerPrefs.GetInt("DailyHuntDay"), Is.EqualTo(today));
        Assert.That(PlayerPrefs.GetInt("DailyHuntUsed"), Is.Zero);
        Assert.That(PlayerPrefs.GetInt("DailyHuntDone"), Is.Zero);
        Assert.That(PlayerPrefs.GetInt("DailyHuntRevived"), Is.Zero);
        Assert.That(PlayerPrefs.GetInt("DailyHuntMin"), Is.EqualTo(1));
        Assert.That(PlayerPrefs.GetInt("DailyHuntMax"), Is.EqualTo(100));
    }

    object Call(string name, params object[] args) => hunt.GetType().GetMethod(name, Instance).Invoke(hunt, args);
    void SaveAndClear(string key)
    {
        if (PlayerPrefs.HasKey(key)) previous[key] = PlayerPrefs.GetInt(key);
        PlayerPrefs.DeleteKey(key);
    }
    void Restore(string key)
    {
        if (previous.TryGetValue(key, out int value)) PlayerPrefs.SetInt(key, value);
        else PlayerPrefs.DeleteKey(key);
    }
}
