using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Evidence-only, no transport/account/build dependency. The original settings
// are kept in memory and restored after all scene writers have been unloaded.
public sealed class HandoffDailyCapturePlayModeTests
{
    static readonly string[] StringKeys = { "PlayerName", "DailyHuntTrail", "DailyLastPlayDate" };
    static readonly string[] IntKeys = {
        "Language", "AdsConsent", "HOL.Onboarding.Avatar", "HOL.Onboarding.Version",
        "StatWins", "StatLosses", "StatStreak", "StatBestStreak", "StatBestGuesses",
        "StatDraws", "StatMatches", "StatRecentBits", "StatRecentCount",
        "DailyHuntDay", "DailyHuntUsed", "DailyHuntDone", "DailyHuntFound",
        "DailyHuntRevived", "DailyHuntMin", "DailyHuntMax", "DailyHuntStreak",
        "DailyHuntLastFound", "DailyHuntPendingRevive", "DailyStreakDays",
        "DailyChallengeDay", "DailyChallengeWins", "DailyChallengeCorrectGuesses",
        "DailyChallengeRoomsShared", "DailyChallengeRewardClaimed", "DailyChallengePoints"
    };
    readonly Dictionary<string, string> strings = new Dictionary<string, string>();
    readonly Dictionary<string, int> ints = new Dictionary<string, int>();
    readonly HashSet<string> present = new HashSet<string>();
    bool fixtureStarted;
    object previousLanguage;

    [UnityTest, Explicit("Native Daily font/material comparison; requires an external -holHandoffEvidence directory.")]
    public IEnumerator NativeDailyDashboardAndHuntWithPreservedPreferences()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-holHandoffEvidence");
        Assert.That(index, Is.GreaterThanOrEqualTo(0));
        Assert.That(index + 1, Is.LessThan(args.Length));
        string output = Path.GetFullPath(args[index + 1]);
        string project = Path.GetFullPath(Path.Combine(Application.dataPath, "..")) + Path.DirectorySeparatorChar;
        Assert.That(output.StartsWith(project, StringComparison.OrdinalIgnoreCase), Is.False,
            "Evidence must stay outside the project.");
        Assert.That(Directory.Exists(output), Is.False, "Never overwrite evidence.");
        Directory.CreateDirectory(output);

        foreach (string key in StringKeys) {
            if (PlayerPrefs.HasKey(key)) present.Add(key);
            strings[key] = PlayerPrefs.GetString(key, "");
        }
        foreach (string key in IntKeys) {
            if (PlayerPrefs.HasKey(key)) present.Add(key);
            ints[key] = PlayerPrefs.GetInt(key, 0);
        }
        previousLanguage = T("L10n").GetProperty("Current").GetValue(null);
        fixtureStarted = true;
        foreach (string key in StringKeys) PlayerPrefs.DeleteKey(key);
        foreach (string key in IntKeys) PlayerPrefs.DeleteKey(key);
        PlayerPrefs.SetString("PlayerName", "CaptureTester");
        PlayerPrefs.SetInt("HOL.Onboarding.Version", 1);
        PlayerPrefs.SetInt("HOL.Onboarding.Avatar", 0);
        PlayerPrefs.SetInt("AdsConsent", 0);

        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        Component hunt = null;
        for (int frame = 0; frame < 240 && hunt == null; frame++) {
            hunt = UnityEngine.Object.FindObjectOfType(T("DailyHunt"), true) as Component;
            yield return null;
        }
        Assert.That(hunt, Is.Not.Null);
        var review = Type.GetType("PvpPresentationReviewTools, Assembly-CSharp-Editor", true);
        foreach (var viewport in new[] { new Vector2Int(1080, 1920), new Vector2Int(1080, 2340) })
        foreach (string language in new[] { "English", "Greek" })
        foreach (bool dashboard in new[] { true, false })
        {
            Type.GetType("OnboardingGameViewCapture, Assembly-CSharp-Editor", true)
                .GetMethod("SetResolution").Invoke(null, new object[] { viewport.x, viewport.y });
            Screen.SetResolution(viewport.x, viewport.y, false);
            review.GetMethod("FocusNativeGameView").Invoke(null, null);
            T("L10n").GetMethod("SetLanguage").Invoke(null,
                new[] { Enum.Parse(T("L10n").GetNestedType("Language"), language) });
            // The real menu callback invokes Open on an initially inactive
            // panel. SendMessage does not dispatch to that inactive receiver.
            hunt.GetType().GetMethod("Open").Invoke(hunt, null);
            if (!dashboard) hunt.GetType().GetMethod("StartChallenge").Invoke(hunt, null);
            yield return (IEnumerator)review.GetMethod("WaitForStableNativeViewport")
                .Invoke(null, new object[] { hunt.transform, viewport.x, viewport.y });
            float deadline = Time.realtimeSinceStartup + 5f;
            while (hunt.GetComponentsInParent<CanvasGroup>().Any(g => g.alpha < .9999f) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(hunt.GetComponentsInParent<CanvasGroup>().All(g => g.alpha >= .9999f), Is.True);
            for (int frame = 0; frame < 4; frame++) yield return null;
            string file = Path.Combine(output, (dashboard ? "daily-dashboard-" : "daily-hunt-") +
                (language == "Greek" ? "el-" : "en-") + viewport.x + "x" + viewport.y + ".png");
            ScreenCapture.CaptureScreenshot(file);
            deadline = Time.realtimeSinceStartup + 10f;
            while ((!File.Exists(file) || new FileInfo(file).Length == 0) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(File.Exists(file), Is.True);
            var png = new Texture2D(2, 2);
            Assert.That(png.LoadImage(File.ReadAllBytes(file)), Is.True);
            Assert.That(png.width, Is.EqualTo(viewport.x));
            Assert.That(png.height, Is.EqualTo(viewport.y));
            UnityEngine.Object.Destroy(png);
            review.GetMethod("WriteViewportMetrics").Invoke(null, new object[] {
                file + ".geometry.json", hunt.transform,
                new[] { "DailyHuntSafeRoot", "DailyLogo", "DailyPlayerChip", "DailyMissionReset" } });
        }
    }

    [UnityTearDown]
    public IEnumerator RestorePreferencesAfterSceneWritersStop()
    {
        if (!fixtureStarted) yield break;
        Scene active = SceneManager.GetActiveScene();
        Scene quiet = SceneManager.CreateScene("HandoffDailyQuiescent_" + Guid.NewGuid().ToString("N"));
        SceneManager.SetActiveScene(quiet);
        if (active.IsValid() && active.isLoaded) yield return SceneManager.UnloadSceneAsync(active);
        yield return null;
        T("L10n").GetMethod("SetLanguage").Invoke(null, new[] { previousLanguage });
        foreach (string key in StringKeys) {
            if (present.Contains(key)) PlayerPrefs.SetString(key, strings[key]);
            else PlayerPrefs.DeleteKey(key);
        }
        foreach (string key in IntKeys) {
            if (present.Contains(key)) PlayerPrefs.SetInt(key, ints[key]);
            else PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();
    }

    static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
}
