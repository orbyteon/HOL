using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public sealed class SoloProductionPolicyTests
{
    const BindingFlags StaticPrivate = BindingFlags.NonPublic | BindingFlags.Static;
    const BindingFlags InstancePrivate = BindingFlags.NonPublic | BindingFlags.Instance;
    const BindingFlags InstanceFields =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    const string DifficultyKey = "AIDifficulty";
    const string WinsKey = "StatWins";
    const string LossesKey = "StatLosses";
    const string DrawsKey = "StatDraws";
    const string StreakKey = "StatStreak";
    const string BestStreakKey = "StatBestStreak";
    const string BestGuessesKey = "StatBestGuesses";
    const string MatchesKey = "StatMatches";
    const string RecentBitsKey = "StatRecentBits";
    const string RecentCountKey = "StatRecentCount";

    static readonly string[] TouchedPreferenceKeys =
    {
        DifficultyKey,
        WinsKey,
        LossesKey,
        DrawsKey,
        StreakKey,
        BestStreakKey,
        BestGuessesKey,
        MatchesKey,
        RecentBitsKey,
        RecentCountKey,
    };

    bool[] preferenceExisted;
    int[] savedPreferenceValues;

    [SetUp]
    public void SetUp()
    {
        preferenceExisted = new bool[TouchedPreferenceKeys.Length];
        savedPreferenceValues = new int[TouchedPreferenceKeys.Length];

        for (int index = 0; index < TouchedPreferenceKeys.Length; index++)
        {
            string key = TouchedPreferenceKeys[index];
            preferenceExisted[index] = PlayerPrefs.HasKey(key);
            savedPreferenceValues[index] = PlayerPrefs.GetInt(key, 0);
            PlayerPrefs.DeleteKey(key);
        }

        PlayerPrefs.Save();
    }

    [TearDown]
    public void TearDown()
    {
        for (int index = 0; index < TouchedPreferenceKeys.Length; index++)
        {
            string key = TouchedPreferenceKeys[index];
            if (preferenceExisted[index])
                PlayerPrefs.SetInt(key, savedPreferenceValues[index]);
            else
                PlayerPrefs.DeleteKey(key);
        }

        PlayerPrefs.Save();
    }

    [Test]
    public void DifficultyUsesNormalDefaultAndClampsPersistedValueToFourPolicies()
    {
        PlayerPrefs.DeleteKey(DifficultyKey);
        Assert.That(ProductionDifficulty(), Is.EqualTo(1),
            "A missing preference must use the documented Normal default.");

        for (int difficulty = 0; difficulty <= 3; difficulty++)
        {
            PlayerPrefs.SetInt(DifficultyKey, difficulty);
            Assert.That(ProductionDifficulty(), Is.EqualTo(difficulty));
        }

        PlayerPrefs.SetInt(DifficultyKey, -25);
        Assert.That(ProductionDifficulty(), Is.EqualTo(0));

        PlayerPrefs.SetInt(DifficultyKey, 25);
        Assert.That(ProductionDifficulty(), Is.EqualTo(3));
    }

    [Test]
    public void OpeningGuessIsSeededRandomForEasyNormalAndAdaptiveButMidpointForHard()
    {
        const int seed = 9127;
        const int low = 10;
        const int high = 30;
        int seededRandomGuess = PredictOpeningGuess(seed, low, high);

        Assert.That(RunProductionGuess(0, true, seed, low, high),
            Is.EqualTo(seededRandomGuess), "Easy must retain its random opener.");
        Assert.That(RunProductionGuess(1, true, seed, low, high),
            Is.EqualTo(seededRandomGuess), "Normal must retain its random opener.");
        Assert.That(RunProductionGuess(2, true, seed, low, high),
            Is.EqualTo((low + high) / 2), "Hard must open at the solver midpoint.");
        Assert.That(RunProductionGuess(3, true, seed, low, high),
            Is.EqualTo(seededRandomGuess), "Adaptive must retain its random opener.");
    }

    [Test]
    public void SubsequentGuessUsesEachDifficultyRandomChanceAndMidpointFallback()
    {
        AssertSubsequentPolicy(0, 0.60f, 0, 0);
        AssertSubsequentPolicy(1, 0.20f, 0, 0);

        const int hardSeed = 2048;
        Assert.That(RunProductionGuess(2, false, hardSeed, 10, 30), Is.EqualTo(20),
            "Hard must always use the binary-search midpoint after its opener.");

        SetRecentResults(3, 10);
        Assert.That(ProductionAdaptiveRandomChance(), Is.EqualTo(0.50f));
        AssertSubsequentPolicy(3, 0.50f, 3, 10);
    }

    [Test]
    public void AdaptivePolicyHonorsNoDataAndBothStrictWinRateBoundaries()
    {
        SetRecentResults(0, 0);
        Assert.That(GameStatsRecentWinRate(), Is.EqualTo(-1f));
        Assert.That(ProductionAdaptiveRandomChance(), Is.EqualTo(0.20f),
            "No history must behave like Normal rather than inventing a record.");

        SetRecentResults(3, 10);
        Assert.That(ProductionAdaptiveRandomChance(), Is.EqualTo(0.50f),
            "A rate strictly below 0.4 must soften the opponent.");

        SetRecentResults(4, 10);
        Assert.That(ProductionAdaptiveRandomChance(), Is.EqualTo(0.25f),
            "The exact 0.4 boundary belongs to the middle policy.");

        SetRecentResults(6, 10);
        Assert.That(ProductionAdaptiveRandomChance(), Is.EqualTo(0.25f),
            "The exact 0.6 boundary belongs to the middle policy.");

        SetRecentResults(7, 10);
        Assert.That(ProductionAdaptiveRandomChance(), Is.EqualTo(0.10f),
            "A rate strictly above 0.6 must strengthen the opponent.");
    }

    [Test]
    public void EveryDifficultyUsesItsDocumentedLockJudgement()
    {
        PlayerPrefs.SetInt(DifficultyKey, 0);
        Assert.That(ProductionLockStyle(), Is.EqualTo(DuelRules.LockStyle.Reckless));
        Assert.That(DuelRules.ShouldLock(ProductionLockStyle(), 8, true), Is.True);
        Assert.That(DuelRules.ShouldLock(ProductionLockStyle(), 9, true), Is.False);

        PlayerPrefs.SetInt(DifficultyKey, 1);
        Assert.That(ProductionLockStyle(), Is.EqualTo(DuelRules.LockStyle.Bold));
        Assert.That(DuelRules.ShouldLock(ProductionLockStyle(), 3, true), Is.True);
        Assert.That(DuelRules.ShouldLock(ProductionLockStyle(), 4, true), Is.False);

        PlayerPrefs.SetInt(DifficultyKey, 2);
        Assert.That(ProductionLockStyle(), Is.EqualTo(DuelRules.LockStyle.Precise));
        Assert.That(DuelRules.ShouldLock(ProductionLockStyle(), 1, true), Is.True);
        Assert.That(DuelRules.ShouldLock(ProductionLockStyle(), 2, true), Is.False);

        PlayerPrefs.SetInt(DifficultyKey, 3);
        SetRecentResults(6, 10);
        Assert.That(ProductionLockStyle(), Is.EqualTo(DuelRules.LockStyle.Bold),
            "Adaptive is Bold at the exact 0.6 boundary.");
        SetRecentResults(7, 10);
        Assert.That(ProductionLockStyle(), Is.EqualTo(DuelRules.LockStyle.Precise),
            "Adaptive becomes Precise only above the 0.6 boundary.");
    }

    [Test]
    public void GameStatsPersistsWinLossDrawStreakBestAndRecentMatrixExactly()
    {
        AssertEmptyStats();

        InvokeGameStats("RecordWin", 7, true);
        InvokeGameStats("RecordWin", 9, true);
        InvokeGameStats("RecordLoss", true);
        InvokeGameStats("RecordDraw");
        InvokeGameStats("RecordWin", 4, false);
        InvokeGameStats("RecordLoss", false);
        InvokeGameStats("RestoreStreak", 5);

        Assert.That(GameStat<int>("Wins"), Is.EqualTo(3));
        Assert.That(GameStat<int>("Losses"), Is.EqualTo(2));
        Assert.That(GameStat<int>("Draws"), Is.EqualTo(1));
        Assert.That(GameStat<int>("Matches"), Is.EqualTo(6));
        Assert.That(GameStat<int>("CurrentStreak"), Is.EqualTo(5));
        Assert.That(GameStat<int>("BestStreak"), Is.EqualTo(5));
        Assert.That(GameStat<int>("BestWinningGuesses"), Is.EqualTo(4));
        Assert.That(GameStatsRecentWinRate(),
            Is.EqualTo(2f / 3f).Within(0.0001f));

        Assert.That(PlayerPrefs.GetInt(WinsKey), Is.EqualTo(3));
        Assert.That(PlayerPrefs.GetInt(LossesKey), Is.EqualTo(2));
        Assert.That(PlayerPrefs.GetInt(DrawsKey), Is.EqualTo(1));
        Assert.That(PlayerPrefs.GetInt(MatchesKey), Is.EqualTo(6));
        Assert.That(PlayerPrefs.GetInt(StreakKey), Is.EqualTo(5));
        Assert.That(PlayerPrefs.GetInt(BestStreakKey), Is.EqualTo(5));
        Assert.That(PlayerPrefs.GetInt(BestGuessesKey), Is.EqualTo(4));
        Assert.That(PlayerPrefs.GetInt(RecentBitsKey), Is.EqualTo(6),
            "Only the recent win, win, loss sequence should be encoded as 110b.");
        Assert.That(PlayerPrefs.GetInt(RecentCountKey), Is.EqualTo(3));
    }

    [Test]
    public void RecentHistoryKeepsExactlyTheLatestTenWinLossResults()
    {
        bool[] results =
        {
            true, false, true, true, false, false,
            true, false, true, true, false, true,
        };

        for (int index = 0; index < results.Length; index++)
        {
            if (results[index])
                InvokeGameStats("RecordWin", 12 - index, true);
            else
                InvokeGameStats("RecordLoss", true);
        }

        Assert.That(PlayerPrefs.GetInt(RecentCountKey), Is.EqualTo(10));
        Assert.That(PlayerPrefs.GetInt(RecentBitsKey), Is.EqualTo(813),
            "The rolling mask must contain only the final ten outcomes (1100101101b).");
        Assert.That(GameStatsRecentWinRate(),
            Is.EqualTo(0.6f).Within(0.0001f));
        Assert.That(GameStat<int>("Wins"), Is.EqualTo(7));
        Assert.That(GameStat<int>("Losses"), Is.EqualTo(5));
        Assert.That(GameStat<int>("Matches"), Is.EqualTo(12));
    }

    void AssertSubsequentPolicy(
        int difficulty,
        float randomChance,
        int adaptiveWins,
        int adaptiveCount)
    {
        const int low = 10;
        const int high = 30;

        int randomSeed = FindSeed(randomChance, true);
        if (difficulty == 3)
            SetRecentResults(adaptiveWins, adaptiveCount);
        int expectedRandom = PredictSubsequentGuess(randomSeed, randomChance, low, high);
        Assert.That(RunProductionGuess(difficulty, false, randomSeed, low, high),
            Is.EqualTo(expectedRandom),
            "The random side of difficulty " + difficulty + " must use the seeded in-range guess.");

        int midpointSeed = FindSeed(randomChance, false);
        if (difficulty == 3)
            SetRecentResults(adaptiveWins, adaptiveCount);
        Assert.That(PredictSubsequentGuess(midpointSeed, randomChance, low, high),
            Is.EqualTo((low + high) / 2));
        Assert.That(RunProductionGuess(difficulty, false, midpointSeed, low, high),
            Is.EqualTo((low + high) / 2),
            "The solver side of difficulty " + difficulty + " must use the midpoint.");
    }

    static int RunProductionGuess(
        int difficulty,
        bool firstGuess,
        int seed,
        int low,
        int high)
    {
        PlayerPrefs.SetInt(DifficultyKey, difficulty);

        var root = new GameObject("SoloProductionPolicyTests.GameManager");
        Component manager = root.AddComponent(RuntimeType("GameManager"));
        SetInstanceField(
            manager, "aiNumberText", CreateText(root.transform, "AiNumber"));
        SetInstanceField(
            manager, "aiAnswerText", CreateText(root.transform, "AiAnswer"));
        SetInstanceField(
            manager, "higherButton", CreateChild(root.transform, "Higher"));
        SetInstanceField(
            manager, "lowerButton", CreateChild(root.transform, "Lower"));
        SetInstanceField(
            manager, "correctButton", CreateChild(root.transform, "Correct"));
        SetInstanceField(
            manager, "stopGameButton", CreateChild(root.transform, "Stop"));

        SetInstanceField(manager, "min", low);
        SetInstanceField(manager, "max", high);
        SetInstanceField(manager, "firstAIGuess", firstGuess);
        SetInstanceField(manager, "playerSecretNumber", 101);
        SetInstanceField(manager, "currentOpponent", "PolicyBot");
        SetInstanceField(manager, "matchSetUp", true);

        var rules = (DuelRules)GetInstanceField(manager, "rules");
        rules.StartMatch(DuelRules.Side.Host);
        DuelRules.Move pendingWin = rules.Submit(
            DuelRules.Side.Host, 37, 37, false);
        Assert.That(pendingWin.Accepted, Is.True);
        Assert.That(pendingWin.Hint, Is.EqualTo(DuelRules.Hint.Correct));
        Assert.That(rules.Turn, Is.EqualTo(DuelRules.Side.Guest));

        UnityEngine.Random.State savedRandomState = UnityEngine.Random.state;
        try
        {
            UnityEngine.Random.InitState(seed);
            InvokeInstance(manager, "AIGuess");
            return (int)GetInstanceField(manager, "aiGuess");
        }
        finally
        {
            UnityEngine.Random.state = savedRandomState;
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static TMP_Text CreateText(Transform parent, string name)
    {
        var textObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        return textObject.GetComponent<TextMeshProUGUI>();
    }

    static GameObject CreateChild(Transform parent, string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    static int PredictOpeningGuess(int seed, int low, int high)
    {
        UnityEngine.Random.State savedRandomState = UnityEngine.Random.state;
        try
        {
            UnityEngine.Random.InitState(seed);
            return UnityEngine.Random.Range(low, high + 1);
        }
        finally
        {
            UnityEngine.Random.state = savedRandomState;
        }
    }

    static int PredictSubsequentGuess(int seed, float randomChance, int low, int high)
    {
        UnityEngine.Random.State savedRandomState = UnityEngine.Random.state;
        try
        {
            UnityEngine.Random.InitState(seed);
            return UnityEngine.Random.value < randomChance
                ? UnityEngine.Random.Range(low, high + 1)
                : (low + high) / 2;
        }
        finally
        {
            UnityEngine.Random.state = savedRandomState;
        }
    }

    static int FindSeed(float threshold, bool belowThreshold)
    {
        UnityEngine.Random.State savedRandomState = UnityEngine.Random.state;
        try
        {
            for (int seed = 1; seed < 10000; seed++)
            {
                UnityEngine.Random.InitState(seed);
                if ((UnityEngine.Random.value < threshold) == belowThreshold)
                    return seed;
            }
        }
        finally
        {
            UnityEngine.Random.state = savedRandomState;
        }

        Assert.Fail("Could not find a deterministic seed around threshold " + threshold + ".");
        return -1;
    }

    static void SetRecentResults(int wins, int count)
    {
        Assert.That(count, Is.InRange(0, 10));
        Assert.That(wins, Is.InRange(0, count));
        int bits = wins == 0 ? 0 : (1 << wins) - 1;
        PlayerPrefs.SetInt(RecentBitsKey, bits);
        PlayerPrefs.SetInt(RecentCountKey, count);
    }

    static void AssertEmptyStats()
    {
        Assert.That(GameStat<int>("Wins"), Is.Zero);
        Assert.That(GameStat<int>("Losses"), Is.Zero);
        Assert.That(GameStat<int>("Draws"), Is.Zero);
        Assert.That(GameStat<int>("Matches"), Is.Zero);
        Assert.That(GameStat<int>("CurrentStreak"), Is.Zero);
        Assert.That(GameStat<int>("BestStreak"), Is.Zero);
        Assert.That(GameStat<int>("BestWinningGuesses"), Is.Zero);
        Assert.That(GameStatsRecentWinRate(), Is.EqualTo(-1f));
    }

    static int ProductionDifficulty()
    {
        return (int)InvokeStatic("Difficulty");
    }

    static float ProductionAdaptiveRandomChance()
    {
        return (float)InvokeStatic("AdaptiveRandomChance");
    }

    static DuelRules.LockStyle ProductionLockStyle()
    {
        return (DuelRules.LockStyle)InvokeStatic("AiLockStyle");
    }

    static object InvokeStatic(string methodName)
    {
        MethodInfo method = RuntimeType("GameManager").GetMethod(
            methodName, StaticPrivate);
        Assert.That(method, Is.Not.Null, "Missing GameManager policy seam " + methodName + ".");
        return method.Invoke(null, null);
    }

    static void InvokeInstance(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, InstancePrivate);
        Assert.That(method, Is.Not.Null, "Missing GameManager policy seam " + methodName + ".");
        method.Invoke(target, null);
    }

    static object GetInstanceField(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, InstanceFields);
        Assert.That(field, Is.Not.Null, "Missing GameManager policy field " + fieldName + ".");
        return field.GetValue(target);
    }

    static void SetInstanceField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, InstanceFields);
        Assert.That(field, Is.Not.Null, "Missing GameManager policy field " + fieldName + ".");
        field.SetValue(target, value);
    }

    static T GameStat<T>(string propertyName)
    {
        PropertyInfo property = RuntimeType("GameStats").GetProperty(
            propertyName, BindingFlags.Static | BindingFlags.Public);
        Assert.That(property, Is.Not.Null,
            "Missing GameStats property " + propertyName + ".");
        return (T)property.GetValue(null);
    }

    static float GameStatsRecentWinRate()
    {
        return (float)InvokeGameStats("RecentWinRate");
    }

    static object InvokeGameStats(
        string methodName, params object[] arguments)
    {
        MethodInfo found = null;
        foreach (MethodInfo candidate in RuntimeType("GameStats").GetMethods(
                     BindingFlags.Static | BindingFlags.Public))
        {
            if (candidate.Name != methodName ||
                candidate.GetParameters().Length != arguments.Length)
                continue;
            found = candidate;
            break;
        }
        Assert.That(found, Is.Not.Null,
            "Missing GameStats method " + methodName + ".");
        return found.Invoke(null, arguments);
    }

    static Type RuntimeType(string name)
    {
        Type type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null,
            "Missing runtime type " + name + ".");
        return type;
    }
}
