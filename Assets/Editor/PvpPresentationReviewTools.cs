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
    [Serializable] sealed class ViewportMetrics
    {
        public int width, height;
        public Rect safeArea;
        public RegionMetrics[] regions;
    }
    [Serializable] sealed class RegionMetrics
    {
        public string name;
        public Rect rect;
        public Vector3 scale;
        public Vector3[] screenCorners;
    }
    public static IEnumerator WaitForStableNativeViewport(Transform root, int width, int height)
    {
        // A native resize reaches Screen, CanvasScaler and the safe-area owner
        // on different frames. Record settled production geometry, never the
        // first frame with merely the requested PNG dimensions.
        string previous = null;
        int stable = 0;
        for (int frame = 0; frame < 120; frame++)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            string current = Screen.width + "x" + Screen.height + "/" + Screen.safeArea;
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(false))
                if (rect.name.EndsWith("SafeRoot", StringComparison.Ordinal) || rect.GetComponent<Canvas>() != null)
                    current += "/" + rect.GetInstanceID() + ":" + rect.rect + ":" + rect.lossyScale;
            stable = Screen.width == width && Screen.height == height && current == previous ? stable + 1 : 0;
            previous = current;
            if (stable >= 3) yield break;
        }
        throw new InvalidOperationException("Native viewport did not settle at " + width + "x" + height);
    }
    public static void WriteViewportMetrics(string path, Transform root, string[] names)
    {
        // Device Simulator can retain a different device safe area while a
        // native Game View is resized. Such frames are not comparable native
        // evidence: fail rather than silently recording a clipped tall layout.
        Rect area = Screen.safeArea;
        if (area.width <= 0 || area.height <= 0 || area.xMin < 0 || area.yMin < 0 ||
            area.xMax > Screen.width || area.yMax > Screen.height)
            throw new InvalidOperationException("Native capture has a conflicting safe area " + area +
                " for " + Screen.width + "x" + Screen.height + ". Close Device Simulator before native capture.");
        var all = root.GetComponentsInChildren<RectTransform>(true);
        var metrics = new ViewportMetrics {
            width = Screen.width, height = Screen.height, safeArea = Screen.safeArea,
            regions = names.Select(name => {
                var rect = all.First(r => r.name == name);
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                for (int i = 0; i < corners.Length; i++)
                    corners[i] = RectTransformUtility.WorldToScreenPoint(null, corners[i]);
                return new RegionMetrics { name = name, rect = rect.rect,
                    scale = rect.lossyScale, screenCorners = corners };
            }).ToArray()
        };
        using (var stream = new FileStream(path, FileMode.CreateNew))
        using (var writer = new StreamWriter(stream)) writer.Write(JsonUtility.ToJson(metrics, true));
    }
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
        OnboardingGameViewCapture.SetResolution(1080, 1920);
        FocusNativeGameView();
        Api.Execute(new ExecutionSettings(new Filter {
            testMode = TestMode.PlayMode,
            testNames = new[] { "PvpProductionPresentationPlayModeTests.CaptureNativeEnElFlowAndTallViewports" }
        }));
    }

    [MenuItem("HOL/PvP/Capture Current Solo Reference")]
    public static void CaptureCurrentSoloReference()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Wait for the Editor to be idle before reference capture.");
        if (!Directory.Exists(OutputDirectory)) ChooseCaptureFolder();
        if (!Directory.Exists(OutputDirectory)) return;
        OnboardingGameViewCapture.SetResolution(1080, 1920);
        FocusNativeGameView();
        Api.Execute(new ExecutionSettings(new Filter {
            testMode = TestMode.PlayMode,
            testNames = new[] { "SoloDuelVisualsPlayModeTests.CaptureCurrentSoloReferenceForPvp" }
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
                fixture + "MatchUsesMeasuredSoloGeometryAssetsAndRealData",
                fixture + "SignalsDrawerPreservesIndexedCallbacksAndCancelsOnExit",
                fixture + "DirectConstructionHasOneOwnerSharedProfileAndTouchRematch",
                fixture + "EntireEnElPortraitFlowKeepsRenderedGlyphsInsideOwnedRegions",
                fixture + "KeypadSubmitAndLockReflectAuthoritativeTurnAndInFlightGuards",
                fixture + "RealCreateJoinValidationWaitingCancelAndLateCallbackRemainWired",
                fixture + "ResultCaptionsRepaintOpponentArrivesAndRealRematchResets"
            }
        }));
    }

    [MenuItem("HOL/PvP/Run Invitation Result Regressions")]
    public static void RunInvitationResultRegressions()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Wait for the Editor to be idle before focused validation.");
        if (!Directory.Exists(OutputDirectory)) ChooseCaptureFolder();
        if (!Directory.Exists(OutputDirectory)) return;
        OnboardingGameViewCapture.SetResolution(1080, 1920);
        FocusNativeGameView();
        string fixture = "PvpProductionPresentationPlayModeTests.";
        Api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode, testNames = new[] {
            fixture + "InviteSharingUsesCurrentCodeAndResumeRefreshesWithoutSendingOrResetting",
            fixture + "ReturnedSnapshotsPropagateResultFactsWithoutChangingMatchOrIdentity",
            fixture + "InvitationAndFinalReasonsFitEnElPortraitRegions",
            fixture + "RealCreateJoinValidationWaitingCancelAndLateCallbackRemainWired",
            fixture + "ResultCaptionsRepaintOpponentArrivesAndRealRematchResets",
            fixture + "PrematchValidationKeyboardLanguageAndCancelRemainTruthful",
            fixture + "RoomIdentitiesStayAuthoritativeAcrossSeatsRefreshRematchAndExit"
        } }));
    }

    [MenuItem("HOL/PvP/Run Invitation Result EditMode")]
    public static void RunInvitationResultEditMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Wait for the Editor to be idle before focused validation.");
        if (!Directory.Exists(OutputDirectory)) ChooseCaptureFolder();
        if (!Directory.Exists(OutputDirectory)) return;
        Api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode,
            testNames = new[] { "PvpResultExplanationTests", "PvpRoomStateTests" } }));
    }

    [MenuItem("HOL/PvP/Capture Invitation Result Review")]
    public static void CaptureInvitationResultReview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Wait for the Editor to be idle before focused capture.");
        if (!Directory.Exists(OutputDirectory)) ChooseCaptureFolder();
        if (!Directory.Exists(OutputDirectory)) return;
        FocusNativeGameView();
        Api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode,
            testNames = new[] { "PvpProductionPresentationPlayModeTests.CaptureNativeInvitationAndFinalReasons" } }));
    }

    [MenuItem("HOL/PvP/Run Focused Pre-match Regressions")]
    public static void RunPrematchRegressions()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Wait for the Editor to be idle before focused validation.");
        if (!Directory.Exists(OutputDirectory)) ChooseCaptureFolder();
        if (!Directory.Exists(OutputDirectory)) return;
        Api.Execute(new ExecutionSettings(new Filter {
            testMode = TestMode.PlayMode,
            testNames = new[] {
                "PvpProductionPresentationPlayModeTests.PrematchButtonsUseSoloFacesAndCenteredNativeGlyphsInEnEl",
                "PvpProductionPresentationPlayModeTests.PrematchValidationKeyboardLanguageAndCancelRemainTruthful",
                "PvpProductionPresentationPlayModeTests.DirectConstructionHasOneOwnerSharedProfileAndTouchRematch",
                "PvpProductionPresentationPlayModeTests.RealCreateJoinValidationWaitingCancelAndLateCallbackRemainWired",
                "PvpProductionPresentationPlayModeTests.ResultCaptionsRepaintOpponentArrivesAndRealRematchResets",
                "PrivateRoomVisualsPlayModeTests.PrivateRoomUsesOneProductionOwnerAndPreservesCreateJoinFlows",
                "PrivateRoomCartoonReferencePlayModeTests.ApprovedReferenceGeometryAndRealControlsRemainAuthoritative"
            }
        }));
    }

    [MenuItem("HOL/PvP/Run Native Pre-match Capture %&#p")]
    public static void RunPrematchCapture()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Wait for the Editor to be idle before capture.");
        if (!Directory.Exists(OutputDirectory)) ChooseCaptureFolder();
        if (!Directory.Exists(OutputDirectory)) return;
        // Capture the real Game View, not the Simulator preview surface.
        OnboardingGameViewCapture.SetResolution(1080, 1920);
        FocusNativeGameView();
        Api.Execute(new ExecutionSettings(new Filter {
            testMode = TestMode.PlayMode,
            testNames = new[] { "PvpProductionPresentationPlayModeTests.CaptureNativePrematchEnElAndRepresentativeTall" }
        }));
    }

    [MenuItem("HOL/PvP/Run Existing Responsive Integration Regression")]
    public static void RunResponsiveIntegrationRegression()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Wait for the Editor to be idle before responsive validation.");
        if (!Directory.Exists(OutputDirectory)) ChooseCaptureFolder();
        if (!Directory.Exists(OutputDirectory)) return;
        Api.Execute(new ExecutionSettings(new Filter {
            testMode = TestMode.PlayMode,
            testNames = new[] { "ResponsiveUIFoundationPlayModeTests.LivePagesShareTheViewportContractAcrossTheRequiredMatrix" }
        }));
    }

    public static void FocusNativeGameView()
    {
        if (!EditorApplication.ExecuteMenuItem("Window/General/Game"))
            throw new InvalidOperationException("Unity could not open its real Game View.");
        var game = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView", true));
        game.ShowTab();
        game.Focus();
        game.Repaint();
    }

    [MenuItem("HOL/PvP/Run Pre-match Asset Contracts %&a")]
    public static void RunPrematchAssetContracts()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Wait for the Editor to be idle before asset validation.");
        if (!Directory.Exists(OutputDirectory)) ChooseCaptureFolder();
        if (!Directory.Exists(OutputDirectory)) return;
        Api.Execute(new ExecutionSettings(new Filter {
            testMode = TestMode.EditMode,
            testNames = new[] { "PrivateRoomProductionAssetsTests" }
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
        // Keep configuration inside Unity's non-modal Editor UI. Windows'
        // native folder picker can leave its disabled owner inaccessible to
        // desktop input; no capture or test starts until a valid path is set.
        var window = EditorWindow.GetWindow<EvidenceParentWindow>();
        window.titleContent = new GUIContent("PvP Evidence Folder");
        window.minSize = new Vector2(760, 170);
        window.Show();
        window.Focus();
    }

    public sealed class EvidenceParentWindow : EditorWindow
    {
        string parent = "";
        string error = "";

        void OnGUI()
        {
            EditorGUILayout.HelpBox("Paste an existing absolute folder outside the Unity project. A new uniquely named evidence directory will be created inside it. No tests or captures start here.", MessageType.Info);
            EditorGUILayout.LabelField("External evidence parent");
            parent = EditorGUILayout.TextField(parent);
            if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(parent)))
            {
                if (GUILayout.Button("Create fresh evidence directory", GUILayout.Height(32)))
                {
                    try { SetExternalEvidenceParent(parent); Close(); }
                    catch (Exception exception) { error = exception.Message; }
                }
            }
        }
    }

    static void SetExternalEvidenceParent(string parent)
    {
        if (!Path.IsPathRooted(parent) || !Directory.Exists(parent))
            throw new InvalidOperationException("Choose an existing absolute external parent directory.");
        parent = Path.GetFullPath(parent);
        string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if ((parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar).StartsWith(
            project + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("PvP evidence must be outside the Unity project.");
        string output = Path.Combine(parent, "HOL_PVP_FIXTURE_CAPTURE_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff"));
        if (Directory.Exists(output)) throw new IOException("Evidence directory already exists.");
        Directory.CreateDirectory(output);
        SessionState.SetString(OutputKey, output);
        Debug.Log("HOL_PVP_FIXTURE_CAPTURE_OUTPUT " + output);
    }
}
