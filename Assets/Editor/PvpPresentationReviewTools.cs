using System;
using System.IO;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

// GUI-only evidence configuration. SessionState is Editor-local, not a
// ProjectSettings change or a production preference. Never overwrites evidence.
[InitializeOnLoad]
public static class PvpPresentationReviewTools
{
    const string OutputKey = "HOL.PvpPresentationReview.Output";
    public static string OutputDirectory => SessionState.GetString(OutputKey, "");
    static readonly TestRunnerApi Api;
    static readonly ResultsObserver Observer;

    static PvpPresentationReviewTools()
    {
        Api = ScriptableObject.CreateInstance<TestRunnerApi>();
        Observer = new ResultsObserver();
        Api.RegisterCallbacks(Observer);
        AssemblyReloadEvents.beforeAssemblyReload += () => Api.UnregisterCallbacks(Observer);
    }

    sealed class ResultsObserver : ICallbacks
    {
        public void RunStarted(ITestAdaptor tests) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            if (!Directory.Exists(OutputDirectory)) return;
            string path = Path.Combine(OutputDirectory, "TestResults-" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + ".xml");
            using (var stream = new FileStream(path, FileMode.CreateNew))
            using (var writer = new StreamWriter(stream)) writer.Write(result.ToXml().OuterXml);
            Debug.Log("HOL_PVP_TEST_RESULTS " + path);
        }
    }

    [MenuItem("HOL/PvP/Export Existing Focused Result Snapshot %&e")]
    public static void ExportExistingResults()
    {
        if (!Directory.Exists(OutputDirectory)) { ChooseCaptureFolder(); }
        if (!Directory.Exists(OutputDirectory)) return;
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        Type type = typeof(TestRunnerApi).Assembly.GetType("UnityEditor.TestTools.TestRunner.TestRunnerWindow", true);
        var window = Resources.FindObjectsOfTypeAll(type).FirstOrDefault();
        if (window == null) throw new InvalidOperationException("Test Runner is not open.");
        object list = type.GetField("m_PlayModeTestListGUI", flags).GetValue(window);
        var results = (IEnumerable)list.GetType().GetProperty("newResultList", flags).GetValue(list);
        var json = new StringBuilder("[\n");
        bool first = true;
        foreach (object result in results)
        {
            var resultType = result.GetType();
            string name = (string)resultType.GetField("fullName").GetValue(result);
            string status = resultType.GetField("resultStatus").GetValue(result).ToString();
            if (!name.StartsWith("Pvp", StringComparison.Ordinal) || status == "NotRun") continue;
            if (!first) json.Append(",\n");
            json.Append(JsonUtility.ToJson(result, true));
            first = false;
        }
        json.Append("\n]");
        string path = Path.Combine(OutputDirectory, "ExistingResults-" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + ".json");
        using (var stream = new FileStream(path, FileMode.CreateNew))
        using (var writer = new StreamWriter(stream)) writer.Write(json.ToString());
        Debug.Log("HOL_PVP_EXISTING_RESULT_SNAPSHOT " + path);
    }

    [MenuItem("HOL/PvP/Run Native Presentation Capture %&#c")]
    public static void RunNativeCapture()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Wait for the Editor to be idle before capture.");
        if (!Directory.Exists(OutputDirectory)) ChooseCaptureFolder();
        if (!Directory.Exists(OutputDirectory)) return;
        Api.Execute(new ExecutionSettings(new Filter {
            testMode = TestMode.PlayMode,
            testNames = new[] { "PvpProductionPresentationPlayModeTests.CaptureNativeEnElFlowAndTallViewports" }
        }));
    }

    [MenuItem("HOL/PvP/Run Focused Presentation Regressions %&t")]
    public static void RunFocusedRegressions()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Wait for the Editor to be idle before focused validation.");
        if (!Directory.Exists(OutputDirectory)) ChooseCaptureFolder();
        if (!Directory.Exists(OutputDirectory)) return;
        // Explicit method list excludes captures and unrelated suites. This is
        // the same licensed Test Runner API used by Run Selected in the Editor.
        string fixture = "PvpProductionPresentationPlayModeTests.";
        Api.Execute(new ExecutionSettings(new Filter {
            testMode = TestMode.PlayMode,
            testNames = new[] {
                fixture + "ApprovedSpritesKeepTheirMeshCornersAndOpaqueNormalFace",
                fixture + "DirectConstructionHasOneOwnerSharedProfileAndTouchRematch",
                fixture + "EntireEnElPortraitFlowKeepsRenderedGlyphsInsideOwnedRegions",
                fixture + "KeypadSubmitAndLockReflectAuthoritativeTurnAndInFlightGuards",
                fixture + "RealCreateJoinValidationWaitingCancelAndLateCallbackRemainWired",
                fixture + "ResultCaptionsRepaintOpponentArrivesAndRealRematchResets"
            }
        }));
    }

    [MenuItem("HOL/PvP/Run Existing Route And Terminal Regressions %&#t")]
    public static void RunExistingRouteRegressions()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Wait for the Editor to be idle before route validation.");
        if (!Directory.Exists(OutputDirectory)) ChooseCaptureFolder();
        if (!Directory.Exists(OutputDirectory)) return;
        Api.Execute(new ExecutionSettings(new Filter {
            testMode = TestMode.PlayMode,
            testNames = new[] {
                "MainMenuPlayVisualsPlayModeTests.PlayHubExposesOnlyAuthoritativeSoloAndPrivateRoomRoutes",
                "PrivateRoomCartoonReferencePlayModeTests.ApprovedReferenceGeometryAndRealControlsRemainAuthoritative",
                "PvpTerminalPresentationPlayModeTests.ConnectionLossLocksEveryControlAndExitReturnsToMenu",
                "PvpTerminalPresentationPlayModeTests.MissingRoomUsesNeutralUnavailableState",
                "PvpTerminalPresentationPlayModeTests.ProvenOpponentDeparturePreservesAuthoritativeResult",
                "PvpTerminalPresentationPlayModeTests.AuthoritativeDoneStillUsesApprovedNormalResult",
                "PvpTerminalPresentationPlayModeTests.UnchangedSuccessfulSnapshotsNeverBecomeConnectionLoss"
            }
        }));
    }

    [MenuItem("HOL/PvP/Run Home Route In Visible Game View %&r")]
    public static void RunHomeRouteRegression()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Wait for the Editor to be idle before route validation.");
        if (!Directory.Exists(OutputDirectory)) ChooseCaptureFolder();
        if (!Directory.Exists(OutputDirectory)) return;
        // Home deliberately waits for a genuine rendered end-of-frame. Open
        // Unity's Game View before the run; a Simulator-only layout is not a
        // rendering lane for that existing production barrier.
        OnboardingGameViewCapture.SetResolution(1080, 1920);
        var game = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView", true));
        game.Show();
        game.Focus();
        Api.Execute(new ExecutionSettings(new Filter {
            testMode = TestMode.PlayMode,
            testNames = new[] { "MainMenuPlayVisualsPlayModeTests.PlayHubExposesOnlyAuthoritativeSoloAndPrivateRoomRoutes" }
        }));
    }

    [MenuItem("HOL/PvP/Choose New External Capture Folder %&d")]
    public static void ChooseCaptureFolder()
    {
        string parent = EditorUtility.OpenFolderPanel("Choose external PvP evidence parent", "", "");
        if (string.IsNullOrEmpty(parent)) return;
        string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if ((Path.GetFullPath(parent) + Path.DirectorySeparatorChar).StartsWith(
            project + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("PvP evidence must be outside the Unity project.");
        string output = Path.Combine(parent, "HOL_PVP_FIXTURE_CAPTURE_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff"));
        if (Directory.Exists(output)) throw new IOException("Evidence directory already exists.");
        Directory.CreateDirectory(output);
        SessionState.SetString(OutputKey, output);
        Debug.Log("HOL_PVP_FIXTURE_CAPTURE_OUTPUT " + output);
    }
}
