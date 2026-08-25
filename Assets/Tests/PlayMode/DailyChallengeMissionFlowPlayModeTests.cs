using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class DailyChallengeMissionFlowPlayModeTests
{
    static readonly string[] Keys =
    {
        "DailyChallengeDay",
        "DailyChallengeWins",
        "DailyChallengeCorrectGuesses",
        "DailyChallengeRoomsShared",
        "DailyChallengeRewardClaimed",
        "DailyChallengePoints",
        "DailyHuntDay",
        "DailyHuntUsed",
        "DailyHuntTrail",
        "DailyHuntDone",
        "DailyHuntFound",
        "DailyHuntRevived",
        "DailyHuntMin",
        "DailyHuntMax",
    };

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Clear();
        // The production lifecycle bridges intentionally ignore SplashScene.
        // Reuse that empty scene name so isolated component tests do not spawn
        // Main Menu/PvP installers whose controllers are absent by design.
        Scene isolated = SceneManager.GetSceneByName("SplashScene");
        if (!isolated.IsValid() || !isolated.isLoaded)
            isolated = SceneManager.CreateScene("SplashScene");
        SceneManager.SetActiveScene(isolated);
        for (int index = SceneManager.sceneCount - 1; index >= 0; index--)
        {
            Scene loaded = SceneManager.GetSceneAt(index);
            if (loaded == isolated || !loaded.isLoaded) continue;
            AsyncOperation unload = SceneManager.UnloadSceneAsync(loaded);
            if (unload != null)
                yield return unload;
        }
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Clear();
        yield return null;
    }

    [UnityTest]
    public IEnumerator SemanticEventsCompleteMissionsAndRewardOnlyOnce()
    {
        var host = new GameObject("DailyChallengeTrackerTestHost");
        host.AddComponent(RuntimeType("DailyChallengeTracker"));
        yield return null;

        InvokeStatic("GameEvents", "CorrectGuess");
        InvokeStatic("GameEvents", "CorrectGuess");
        InvokeStatic("GameEvents", "CorrectGuess");
        InvokeStatic("GameEvents", "RoomShared");
        InvokeStatic("GameEvents", "MatchCompleted", WinningOutcome());
        yield return null;

        object state = GetStatic("DailyChallengeProgress", "Current");
        Assert.That(GetField<int>(state, "Wins"), Is.EqualTo(1));
        Assert.That(GetField<int>(state, "CorrectGuesses"), Is.EqualTo(3));
        Assert.That(GetField<int>(state, "RoomsShared"), Is.EqualTo(1));
        Assert.That(GetField<bool>(state, "RewardClaimed"), Is.True);
        Assert.That(GetField<int>(state, "Points"), Is.EqualTo(500));

        InvokeStatic("GameEvents", "CorrectGuess");
        InvokeStatic("GameEvents", "RoomShared");
        InvokeStatic("GameEvents", "MatchCompleted", WinningOutcome());
        state = GetStatic("DailyChallengeProgress", "Current");
        Assert.That(GetField<int>(state, "Points"), Is.EqualTo(500));

        UnityEngine.Object.Destroy(host);
        yield return null;
    }

    [UnityTest]
    public IEnumerator DashboardStartOpensTheRealNumberHunt()
    {
        Screen.SetResolution(1080, 1920, false);
        var canvasObject = new GameObject(
            "Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        MethodInfo attach = RuntimeType("DailyHunt").GetMethod(
            "Attach", BindingFlags.Static | BindingFlags.Public);
        Assert.That(attach, Is.Not.Null);
        Component hunt = (Component)attach.Invoke(
            null, new object[] { canvasObject.transform, null });
        Component visuals = hunt.GetComponent(RuntimeType("DailyHuntVisuals"));
        yield return null;

        Assert.That(hunt, Is.Not.Null);
        Assert.That(visuals, Is.Not.Null);
        Assert.That(GetProperty<bool>(visuals, "IsReady"), Is.True);
        Invoke(hunt, "Open");
        for (int frame = 0; frame < 3; frame++)
            yield return new WaitForEndOfFrame();

        Transform visualRoot = Find(hunt.transform, "DailyHuntVisualRoot");
        Transform dashboard = Find(visualRoot, "DailyMissionDashboard");
        Transform numberHunt = Find(visualRoot, "DailyNumberHuntRoot");
        Button start = Find(visualRoot, "DailyMissionStartButton")
            .GetComponent<Button>();

        Assert.That(dashboard, Is.Not.Null);
        Assert.That(numberHunt, Is.Not.Null);
        Assert.That(start, Is.Not.Null);
        Assert.That(dashboard.gameObject.activeInHierarchy, Is.True);
        Assert.That(numberHunt.gameObject.activeInHierarchy, Is.False);
        Assert.That(Find(dashboard, "DailyMissionRow1"), Is.Not.Null);
        Assert.That(Find(dashboard, "DailyMissionRow2"), Is.Not.Null);
        Assert.That(Find(dashboard, "DailyMissionRow3"), Is.Not.Null);
        Assert.That(Find(dashboard, "DailyMissionReset"), Is.Not.Null);
        Assert.That(Find(dashboard, "DailyMissionRewardAmount")
            .GetComponent<TMP_Text>().text, Is.EqualTo("500"));
        Assert.That(
            ((RectTransform)start.transform).anchoredPosition.y,
            Is.LessThan(-600f),
            "A second responsive writer moved START back over the mission rows.");
        Image rewardBoard = Find(dashboard, "DailyMissionRewardArtwork")
            .GetComponent<Image>();
        Assert.That(
            rewardBoard.sprite,
            Is.SameAs(Resources.Load<Sprite>(
                "dailyhunt/production/daily_mission_reward_board")),
            "The Daily Reward must use the approved production artwork.");

        start.onClick.Invoke();
        yield return null;

        Assert.That(dashboard.gameObject.activeInHierarchy, Is.False);
        Assert.That(numberHunt.gameObject.activeInHierarchy, Is.True);
        Assert.That(Find(numberHunt, "GuessInput")
            .gameObject.activeInHierarchy, Is.True);
        Assert.That(Find(numberHunt, "SubmitGuessButton")
            .gameObject.activeInHierarchy, Is.True);

        UnityEngine.Object.Destroy(canvasObject);
        yield return null;
    }

    static object WinningOutcome()
    {
        Type type = RuntimeType("MatchOutcome");
        object value = Activator.CreateInstance(type);
        FieldInfo result = type.GetField("Outcome");
        Assert.That(result, Is.Not.Null);
        result.SetValue(value, Enum.Parse(result.FieldType, "Win"));
        return value;
    }

    static object InvokeStatic(string typeName, string name, params object[] args)
    {
        MethodInfo method = RuntimeType(typeName).GetMethod(
            name,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, typeName + "." + name);
        return method.Invoke(null, args);
    }

    static object GetStatic(string typeName, string name)
    {
        PropertyInfo property = RuntimeType(typeName).GetProperty(
            name,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, typeName + "." + name);
        return property.GetValue(null);
    }

    static object Invoke(Component target, string name)
    {
        MethodInfo method = target.GetType().GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, name);
        return method.Invoke(target, null);
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

    static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = Find(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    static Type RuntimeType(string name)
    {
        Type type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, name);
        return type;
    }

    static void Clear()
    {
        foreach (string key in Keys)
            PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }
}
