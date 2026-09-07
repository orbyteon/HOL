#if UNITY_STANDALONE_WIN && DEVELOPMENT_BUILD && !UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Explicit standalone-only Solo evidence driver. It is compiled only into the
/// dedicated development player built with IncludeTestAssemblies, and it stays
/// inert unless -holSoloCapturePath is present. The fixture drives the real
/// Home, keypad, submit, AI, Lock and rematch callbacks. Reflection is limited
/// to deterministic opener/secret setup that production deliberately randomizes.
/// </summary>
public sealed class SoloDuelLocalCapturePlayer : MonoBehaviour
{
    const string PathArgument = "-holSoloCapturePath";
    const string LayoutArgument = "-holSoloCaptureLayoutPath";
    const string StateArgument = "-holSoloCaptureState";
    const string LanguageArgument = "-holSoloCaptureLanguage";
    const string WidthArgument = "-holSoloCaptureWidth";
    const string HeightArgument = "-holSoloCaptureHeight";
    const string ScaleArgument = "-holSoloCaptureScale";

    static readonly string[] AllowedStates =
    {
        "preparation",
        "active-input",
        "ai-feedback",
        "history",
        "result",
        "rematch",
        "difficulty-easy",
        "difficulty-normal",
        "difficulty-hard",
        "difficulty-adaptive",
        "outcome-win",
        "outcome-loss",
        "outcome-draw",
        "outcome-lock",
    };

    static readonly Vector2Int[] AllowedViewports =
    {
        new Vector2Int(720, 1280),
        new Vector2Int(1080, 1920),
        new Vector2Int(1080, 2400),
        new Vector2Int(1179, 2556),
    };

    static readonly List<PreferenceSnapshot> PreferenceSnapshots =
        new List<PreferenceSnapshot>();

    static string capturePath;
    static string layoutPath;
    static string captureState;
    static string language;
    static int requestedWidth;
    static int requestedHeight;
    static int captureScale;
    static bool preferencesRestored;

    Component soloOwner;
    Component gameManager;
    Component numberManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        capturePath = ReadArgument(PathArgument);
        if (string.IsNullOrWhiteSpace(capturePath))
            return;

        try
        {
            ConfigureCapture();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Application.quitting -= RestorePreferences;
            Application.quitting += RestorePreferences;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            RestorePreferences();
            Application.Quit(90);
        }
    }

    static void ConfigureCapture()
    {
        capturePath = Path.GetFullPath(capturePath);
        layoutPath = ReadArgument(LayoutArgument);
        layoutPath = string.IsNullOrWhiteSpace(layoutPath)
            ? capturePath + ".layout.json"
            : Path.GetFullPath(layoutPath);
        captureState = (ReadArgument(StateArgument) ?? "preparation")
            .Trim().ToLowerInvariant();
        language = (ReadArgument(LanguageArgument) ?? "en")
            .Trim().ToLowerInvariant();
        requestedWidth = ReadPositiveInt(WidthArgument, 1080);
        requestedHeight = ReadPositiveInt(HeightArgument, 1920);
        captureScale = ReadPositiveInt(ScaleArgument, 1);

        if (Array.IndexOf(AllowedStates, captureState) < 0)
            throw new InvalidOperationException(
                "Unknown Solo capture state: " + captureState);
        if (language != "en" && language != "el")
            throw new InvalidOperationException(
                "Solo capture language must be 'en' or 'el'.");
        if (!IsAllowedViewport(requestedWidth, requestedHeight))
            throw new InvalidOperationException(
                "Solo capture viewport is not approved: " +
                requestedWidth + "x" + requestedHeight);
        if ((captureState.StartsWith("difficulty-", StringComparison.Ordinal) ||
             captureState.StartsWith("outcome-", StringComparison.Ordinal)) &&
            (requestedWidth != 1080 || requestedHeight != 1920))
        {
            throw new InvalidOperationException(
                "Difficulty/outcome evidence is restricted to 1080x1920.");
        }
        if (requestedWidth % captureScale != 0 ||
            requestedHeight % captureScale != 0)
        {
            throw new InvalidOperationException(
                "Capture dimensions must be divisible by capture scale.");
        }
        if (File.Exists(capturePath) || File.Exists(layoutPath))
            throw new IOException(
                "Solo evidence output already exists; evidence is never overwritten.");

        string captureDirectory = Path.GetDirectoryName(capturePath);
        string layoutDirectory = Path.GetDirectoryName(layoutPath);
        if (!string.IsNullOrWhiteSpace(captureDirectory))
            Directory.CreateDirectory(captureDirectory);
        if (!string.IsNullOrWhiteSpace(layoutDirectory))
            Directory.CreateDirectory(layoutDirectory);

        CapturePreferences();
        ApplyFixturePreferences();
        UnityEngine.Random.InitState(20260901);
        Screen.SetResolution(
            requestedWidth / captureScale,
            requestedHeight / captureScale,
            FullScreenMode.Windowed);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrWhiteSpace(capturePath) ||
            scene.name != "MainMenu")
            return;

        var host = new GameObject(nameof(SoloDuelLocalCapturePlayer));
        // A navigation regression that reloads MainMenu must fail and quit the
        // evidence process, not destroy the only driver and leave the runner
        // waiting forever.
        DontDestroyOnLoad(host);
        host.AddComponent<SoloDuelLocalCapturePlayer>();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    IEnumerator Start()
    {
        int exitCode = 0;
        var stack = new Stack<IEnumerator>();
        stack.Push(RunCapture());
        while (stack.Count > 0)
        {
            IEnumerator currentRoutine = stack.Peek();
            object yielded = null;
            bool advanced = false;
            try
            {
                advanced = currentRoutine.MoveNext();
                if (advanced)
                    yielded = currentRoutine.Current;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 91;
                break;
            }

            if (!advanced)
            {
                stack.Pop();
                continue;
            }
            if (yielded is IEnumerator nested)
            {
                stack.Push(nested);
                continue;
            }
            yield return yielded;
        }

        if (exitCode == 0 &&
            (!File.Exists(capturePath) ||
             new FileInfo(capturePath).Length <= 1024 ||
             !File.Exists(layoutPath)))
        {
            Debug.LogError("HOL_SOLO_LOCAL_CAPTURE_INCOMPLETE");
            exitCode = 92;
        }

        Time.timeScale = 1f;
        RestorePreferences();
        Debug.Log(
            exitCode == 0
                ? "HOL_SOLO_LOCAL_CAPTURE_READY " + captureState + " " +
                  language + " " + requestedWidth + "x" + requestedHeight +
                  " " + capturePath
                : "HOL_SOLO_LOCAL_CAPTURE_FAILED " + exitCode);
        Application.Quit(exitCode);
    }

    IEnumerator RunCapture()
    {
        int screenWidth = requestedWidth / captureScale;
        int screenHeight = requestedHeight / captureScale;
        for (int frame = 0; frame < 240; frame++)
        {
            if (Screen.width == screenWidth && Screen.height == screenHeight)
                break;
            yield return null;
        }
        Require(
            Screen.width == screenWidth && Screen.height == screenHeight,
            "Standalone viewport did not settle at the requested logical size.");

        HideCaptureOverlays();
        Button play = null;
        for (int frame = 0; frame < 600; frame++)
        {
            HideCaptureOverlays();
            play = FindActiveButton("ButtonPlay");
            if (play != null && play.interactable)
                break;
            yield return null;
        }
        Require(play != null, "The production PLAY SOLO button is missing.");
        play.onClick.Invoke();

        Type ownerType = RuntimeType("SoloDuelVisuals");
        Type gameType = RuntimeType("GameManager");
        Type numberType = RuntimeType("NumberManager");
        Type matchmakingType = RuntimeType("FakeMatchmaking");
        Component matchmaking = null;

        for (int frame = 0; frame < 900; frame++)
        {
            HideCaptureOverlays();
            soloOwner = FindInScene(ownerType);
            gameManager = FindInScene(gameType);
            numberManager = FindInScene(numberType);
            matchmaking = FindInScene(matchmakingType);
            bool ready = soloOwner != null && gameManager != null &&
                         numberManager != null &&
                         GetProperty<bool>(soloOwner, "IsReady") &&
                         GetProperty<GameObject>(soloOwner, "KeypadRoot") != null &&
                         GetProperty<Button>(soloOwner, "SubmitControl") != null;
            bool preparing = matchmaking != null &&
                             GetProperty<bool>(matchmaking, "IsPreparing");
            if (ready && !preparing)
                break;
            yield return null;
        }

        Require(soloOwner != null && GetProperty<bool>(soloOwner, "IsReady"),
            "The sole Solo presentation owner did not become ready.");
        Require(gameManager != null && numberManager != null,
            "Canonical Solo gameplay controllers are missing.");

        // Keep evidence deterministic without bypassing the real entry flow.
        ((MonoBehaviour)gameManager).CancelInvoke();
        SetField(gameManager, "adsManager", null);
        SetField(gameManager, "currentOpponent", "Nikos");
        Invoke(soloOwner, "BeginNewMatch", "Nikos");

        yield return ConfigureState();
        ((MonoBehaviour)gameManager).CancelInvoke();
        ApplyLanguage();
        HideCaptureOverlays();

        for (int frame = 0; frame < 6; frame++)
            yield return null;
        yield return new WaitForSecondsRealtime(1.25f);
        ApplyLanguage();
        HideCaptureOverlays();
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        string expectedPhase = ExpectedPhase(captureState);
        Require(CurrentPhase() == expectedPhase,
            "Capture state " + captureState + " settled in phase " +
            CurrentPhase() + " instead of " + expectedPhase + ".");
        Require(soloOwner.gameObject.activeInHierarchy,
            "Solo owner is not visible in the live scene.");

        DeactivateInputCaret();
        Time.timeScale = 0f;
        Canvas.ForceUpdateCanvases();
        WriteLayoutSidecar();
        ScreenCapture.CaptureScreenshot(capturePath, captureScale);

        for (int frame = 0; frame < 900; frame++)
        {
            if (File.Exists(capturePath) &&
                new FileInfo(capturePath).Length > 1024)
                yield break;
            yield return null;
        }

        throw new TimeoutException("Solo screenshot write timed out.");
    }

    IEnumerator ConfigureState()
    {
        if (captureState == "preparation" ||
            captureState.StartsWith("difficulty-", StringComparison.Ordinal))
            yield break;

        if (captureState == "active-input")
        {
            BeginDeterministicMatch(77, 63, true);
            PressDigits("62");
            DeactivateInputCaret();
            yield break;
        }

        if (captureState == "ai-feedback")
        {
            BeginDeterministicMatch(77, 63, false);
            RunAiGuess(false);
            yield break;
        }

        if (captureState == "history")
        {
            BeginDeterministicMatch(77, 63, true);
            SubmitPlayerGuess(25);
            RunAiGuess(true);
            SubmitPlayerGuess(75);
            RunAiGuess(true);
            SubmitPlayerGuess(60);
            ((MonoBehaviour)gameManager).CancelInvoke("AIGuess");
            yield break;
        }

        if (captureState == "result" || captureState == "outcome-win")
        {
            PlayWin();
            yield break;
        }
        if (captureState == "outcome-loss")
        {
            PlayLoss();
            yield break;
        }
        if (captureState == "outcome-draw")
        {
            PlayDraw();
            yield break;
        }
        if (captureState == "outcome-lock")
        {
            PlayLockMiss();
            yield break;
        }
        if (captureState == "rematch")
        {
            PlayWin();
            GameObject stop = GetField<GameObject>(gameManager, "stopGameButton");
            Require(stop != null && stop.activeInHierarchy,
                "The real result action is not visible.");
            Button rematch = stop.GetComponent<Button>();
            Require(rematch != null && rematch.interactable,
                "The real result action is not an interactable Button.");
            rematch.onClick.Invoke();
            yield return null;
            yield return null;
        }
    }

    void BeginDeterministicMatch(
        int playerSecret,
        int opponentSecret,
        bool playerOpens)
    {
        SetDifficulty(2);
        ClearNumberInput();
        PressDigits(playerSecret.ToString());
        GetProperty<Button>(soloOwner, "SubmitControl").onClick.Invoke();

        MonoBehaviour gameBehaviour = (MonoBehaviour)gameManager;
        gameBehaviour.CancelInvoke();
        MethodInfo start = gameManager.GetType().GetMethod(
            "StartGameWithOpener",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(start != null, "Deterministic opener seam is missing.");
        ParameterInfo[] parameters = start.GetParameters();
        Require(parameters.Length == 1 && parameters[0].ParameterType.IsEnum,
            "Deterministic opener seam has an unexpected signature.");
        object opener = Enum.Parse(
            parameters[0].ParameterType,
            playerOpens ? "Host" : "Guest");
        start.Invoke(gameManager, new[] { opener });
        gameBehaviour.CancelInvoke();
        SetField(gameManager, "playerSecretNumber", playerSecret);
        SetField(gameManager, "aiSecretNumber", opponentSecret);
        ClearNumberInput();
    }

    void SubmitPlayerGuess(int guess)
    {
        Require(GetProperty<bool>(gameManager, "IsPlayerTurn"),
            "A deterministic player guess was attempted out of turn.");
        ClearNumberInput();
        PressDigits(guess.ToString());
        GetProperty<Button>(soloOwner, "SubmitControl").onClick.Invoke();
    }

    void RunAiGuess(bool resolveFeedback)
    {
        MonoBehaviour behaviour = (MonoBehaviour)gameManager;
        behaviour.CancelInvoke("AIGuess");
        Invoke(gameManager, "AIGuess");
        behaviour.CancelInvoke("ResolveAiAnswerAutomatically");
        if (resolveFeedback && !GetProperty<bool>(gameManager, "IsMatchOver"))
            Invoke(gameManager, "ResolveAiAnswerAutomatically");
    }

    void PlayWin()
    {
        BeginDeterministicMatch(77, 50, true);
        SubmitPlayerGuess(50);
        RunAiGuess(false);
        Require(GetProperty<bool>(gameManager, "IsMatchOver"),
            "Deterministic win did not resolve.");
    }

    void PlayLoss()
    {
        BeginDeterministicMatch(50, 77, false);
        RunAiGuess(true);
        SubmitPlayerGuess(50);
        Require(GetProperty<bool>(gameManager, "IsMatchOver"),
            "Deterministic loss did not resolve.");
    }

    void PlayDraw()
    {
        BeginDeterministicMatch(50, 50, false);
        RunAiGuess(true);
        SubmitPlayerGuess(50);
        Require(GetProperty<bool>(gameManager, "IsMatchOver"),
            "Deterministic draw did not resolve.");
    }

    void PlayLockMiss()
    {
        BeginDeterministicMatch(75, 77, true);
        SubmitPlayerGuess(50);
        RunAiGuess(true);

        Button lockButton = FindButton(soloOwner.transform, "LockButton", true);
        Require(lockButton != null && lockButton.interactable,
            "The real Lock callback is not available on round two.");
        lockButton.onClick.Invoke();
        SubmitPlayerGuess(60);
        RunAiGuess(false);
        Require(GetProperty<bool>(gameManager, "IsMatchOver"),
            "Deterministic Lock-miss outcome did not resolve.");
    }

    void PressDigits(string digits)
    {
        GameObject keypad = GetProperty<GameObject>(soloOwner, "KeypadRoot");
        Require(keypad != null && keypad.activeInHierarchy,
            "The real numeric keypad is not active.");
        foreach (char digit in digits)
        {
            Button key = FindButton(
                keypad.transform, "Key_" + digit, true);
            Require(key != null && key.interactable,
                "The real keypad key is unavailable: " + digit);
            key.onClick.Invoke();
        }
    }

    void ClearNumberInput()
    {
        TMP_InputField input = GetField<TMP_InputField>(
            numberManager, "numberInput");
        Require(input != null, "Canonical number input is missing.");
        input.text = string.Empty;
    }

    void DeactivateInputCaret()
    {
        TMP_InputField input = GetField<TMP_InputField>(
            numberManager, "numberInput");
        if (input != null)
            input.DeactivateInputField();
    }

    string CurrentPhase()
    {
        object state = GetProperty<object>(soloOwner, "CurrentState");
        return GetProperty<object>(state, "Phase").ToString();
    }

    static string ExpectedPhase(string state)
    {
        if (state == "active-input") return "PlayerGuess";
        if (state == "ai-feedback") return "AnswerOpponent";
        if (state == "history") return "OpponentThinking";
        if (state == "result" ||
            state.StartsWith("outcome-", StringComparison.Ordinal))
            return "MatchResult";
        return "ChooseSecret";
    }

    void WriteLayoutSidecar()
    {
        var sidecar = new CaptureLayout
        {
            schemaVersion = 1,
            coordinateSystem = "bottom-left",
            state = captureState,
            language = language,
            requestedWidth = requestedWidth,
            requestedHeight = requestedHeight,
            screenWidth = Screen.width,
            screenHeight = Screen.height,
            captureScale = captureScale,
            safeArea = Scaled(Screen.safeArea),
        };

        string[] elementNames =
        {
            "SoloDuelSafeRoot",
            "DuelBack",
            "SoloDuelLogo",
            "SoloDuelPlayerChip",
            "PlayerCard",
            "OpponentCard",
            "SoloVsBurst",
            "SoloPromptRibbon",
            "SoloInteractionCard",
            "SoloOpponentRail",
            "SoloOpponentBubble",
            "HistoryCard",
            "SoloTipCard",
            "NumberKeypad",
            "ButtonConfirm",
            "LockButton",
            "ButtonSTOPGAME",
        };
        foreach (string elementName in elementNames)
        {
            RectTransform rect = FindRect(soloOwner.transform, elementName);
            if (rect == null) continue;
            sidecar.elements.Add(new ElementRecord
            {
                name = elementName,
                active = rect.gameObject.activeInHierarchy,
                rect = ScreenRect(rect),
            });
        }

        foreach (TMP_Text text in
                 soloOwner.GetComponentsInChildren<TMP_Text>(true))
        {
            bool active = text.gameObject.activeInHierarchy;
            RectRecord rect = ScreenRect(text.rectTransform);
            RectRecord glyph = new RectRecord();
            bool hasGlyphs = false;
            bool overflowing = false;
            if (active && !string.IsNullOrEmpty(text.text))
            {
                text.ForceMeshUpdate(true, true);
                glyph = GlyphRect(text, out hasGlyphs);
                overflowing = text.isTextOverflowing;
            }

            sidecar.texts.Add(new TextRecord
            {
                name = text.name,
                active = active,
                value = text.text ?? string.Empty,
                fontSize = text.fontSize,
                overflowing = overflowing,
                hasGlyphs = hasGlyphs,
                rect = rect,
                glyph = glyph,
            });
            if (active && !string.IsNullOrWhiteSpace(text.text))
                sidecar.dynamicRegions.Add(rect);
        }

        foreach (TMP_InputField input in
                 soloOwner.GetComponentsInChildren<TMP_InputField>(true))
        {
            if (input.gameObject.activeInHierarchy)
                sidecar.dynamicRegions.Add(
                    ScreenRect((RectTransform)input.transform));
        }

        foreach (Button button in
                 soloOwner.GetComponentsInChildren<Button>(true))
        {
            if (!button.gameObject.activeInHierarchy)
                continue;
            Graphic target = button.targetGraphic;
            sidecar.touchTargets.Add(new TouchRecord
            {
                name = button.name,
                active = true,
                interactable = button.interactable,
                raycastTarget = target != null && target.raycastTarget,
                rect = ScreenRect((RectTransform)button.transform),
            });
        }

        File.WriteAllText(
            layoutPath,
            JsonUtility.ToJson(sidecar, true));
    }

    static RectRecord GlyphRect(TMP_Text text, out bool hasGlyphs)
    {
        hasGlyphs = false;
        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;
        TMP_TextInfo info = text.textInfo;
        Camera camera = CanvasCamera(text.GetComponentInParent<Canvas>());
        for (int index = 0; index < info.characterCount; index++)
        {
            TMP_CharacterInfo character = info.characterInfo[index];
            if (!character.isVisible) continue;
            hasGlyphs = true;
            Vector3[] localCorners =
            {
                character.bottomLeft,
                new Vector3(character.topRight.x, character.bottomLeft.y, 0f),
                character.topRight,
                new Vector3(character.bottomLeft.x, character.topRight.y, 0f),
            };
            foreach (Vector3 local in localCorners)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(
                    camera, text.rectTransform.TransformPoint(local));
                minX = Mathf.Min(minX, screen.x * captureScale);
                minY = Mathf.Min(minY, screen.y * captureScale);
                maxX = Mathf.Max(maxX, screen.x * captureScale);
                maxY = Mathf.Max(maxY, screen.y * captureScale);
            }
        }

        return hasGlyphs
            ? RectRecord.FromMinMax(minX, minY, maxX, maxY)
            : new RectRecord();
    }

    static RectRecord ScreenRect(RectTransform rect)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Camera camera = CanvasCamera(rect.GetComponentInParent<Canvas>());
        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;
        foreach (Vector3 corner in corners)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(
                camera, corner);
            minX = Mathf.Min(minX, screen.x * captureScale);
            minY = Mathf.Min(minY, screen.y * captureScale);
            maxX = Mathf.Max(maxX, screen.x * captureScale);
            maxY = Mathf.Max(maxY, screen.y * captureScale);
        }
        return RectRecord.FromMinMax(minX, minY, maxX, maxY);
    }

    static Camera CanvasCamera(Canvas canvas)
    {
        return canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;
    }

    static RectRecord Scaled(Rect rect)
    {
        return new RectRecord
        {
            x = rect.x * captureScale,
            y = rect.y * captureScale,
            width = rect.width * captureScale,
            height = rect.height * captureScale,
        };
    }

    static void ApplyFixturePreferences()
    {
        string playerNameKey = RuntimeConstant<string>(
            "OnboardingProfile", "PlayerNameKey");
        string versionKey = RuntimeConstant<string>(
            "OnboardingProfile", "VersionKey");
        string avatarKey = RuntimeConstant<string>(
            "OnboardingProfile", "AvatarKey");
        int currentVersion = RuntimeConstant<int>(
            "OnboardingProfile", "CurrentVersion");
        string difficultyKey = RuntimeConstant<string>(
            "GameManager", "DifficultyPrefKey");

        PlayerPrefs.SetString(playerNameKey, "Marinos");
        PlayerPrefs.SetInt(versionKey, currentVersion);
        PlayerPrefs.SetInt(avatarKey, 5);
        PlayerPrefs.SetInt(difficultyKey, DifficultyForState(captureState));
        PlayerPrefs.SetInt(RuntimeConstant<string>(
            "AdsManager", "ConsentPrefKey"), 0);
        PlayerPrefs.SetInt(RuntimeConstant<string>("GameStats", "WinsKey"), 12);
        PlayerPrefs.SetInt(RuntimeConstant<string>("GameStats", "LossesKey"), 4);
        PlayerPrefs.SetInt(RuntimeConstant<string>("GameStats", "StreakKey"), 2);
        PlayerPrefs.SetInt(RuntimeConstant<string>(
            "GameStats", "BestStreakKey"), 5);
        PlayerPrefs.SetInt(RuntimeConstant<string>("GameStats", "MatchesKey"), 16);
        PlayerPrefs.SetInt(RuntimeConstant<string>("GameStats", "RecentKey"), 0x2B5);
        PlayerPrefs.SetInt(RuntimeConstant<string>(
            "GameStats", "RecentCountKey"), 10);
        PlayerPrefs.SetInt(RuntimeConstant<string>("LockIntro", "ShownKey"), 3);
        PlayerPrefs.SetInt(RuntimeConstant<string>("LockIntro", "UsedKey"), 0);
        ApplyLanguage();
        PlayerPrefs.Save();
    }

    static int DifficultyForState(string state)
    {
        if (state == "difficulty-easy") return 0;
        if (state == "difficulty-normal" || state == "preparation") return 1;
        if (state == "difficulty-adaptive") return 3;
        return 2;
    }

    static void SetDifficulty(int difficulty)
    {
        string key = RuntimeConstant<string>(
            "GameManager", "DifficultyPrefKey");
        PlayerPrefs.SetInt(key, Mathf.Clamp(difficulty, 0, 3));
    }

    static void ApplyLanguage()
    {
        Type type = RuntimeType("L10n");
        Type enumType = type.GetNestedType("Language", BindingFlags.Public);
        MethodInfo set = type.GetMethod(
            "SetLanguage", BindingFlags.Static | BindingFlags.Public);
        Require(enumType != null && set != null,
            "Canonical localization contract is unavailable.");
        set.Invoke(null, new[]
        {
            Enum.Parse(enumType, language == "el" ? "Greek" : "English"),
        });
    }

    static void CapturePreferences()
    {
        PreferenceSnapshots.Clear();
        preferencesRestored = false;

        string[] stringKeys =
        {
            RuntimeConstant<string>("OnboardingProfile", "PlayerNameKey"),
            RuntimeConstant<string>("DailyStreak", "LastPlayDateKey"),
        };
        string[] intKeys =
        {
            RuntimeConstant<string>("OnboardingProfile", "VersionKey"),
            RuntimeConstant<string>("OnboardingProfile", "GenderKey"),
            RuntimeConstant<string>("OnboardingProfile", "AvatarKey"),
            RuntimeConstant<string>("OnboardingProfile", "AgeKey"),
            RuntimeConstant<string>("GameManager", "DifficultyPrefKey"),
            RuntimeConstant<string>("L10n", "PrefKey"),
            RuntimeConstant<string>("AdsManager", "ConsentPrefKey"),
            RuntimeConstant<string>("GameStats", "WinsKey"),
            RuntimeConstant<string>("GameStats", "LossesKey"),
            RuntimeConstant<string>("GameStats", "StreakKey"),
            RuntimeConstant<string>("GameStats", "BestStreakKey"),
            RuntimeConstant<string>("GameStats", "BestGuessesKey"),
            RuntimeConstant<string>("GameStats", "DrawsKey"),
            RuntimeConstant<string>("GameStats", "MatchesKey"),
            RuntimeConstant<string>("GameStats", "RecentKey"),
            RuntimeConstant<string>("GameStats", "RecentCountKey"),
            RuntimeConstant<string>("DailyChallengeProgress", "DayKey"),
            RuntimeConstant<string>("DailyChallengeProgress", "WinsKey"),
            RuntimeConstant<string>(
                "DailyChallengeProgress", "CorrectGuessesKey"),
            RuntimeConstant<string>(
                "DailyChallengeProgress", "RoomsSharedKey"),
            RuntimeConstant<string>(
                "DailyChallengeProgress", "RewardClaimedKey"),
            RuntimeConstant<string>("DailyChallengeProgress", "PointsKey"),
            RuntimeConstant<string>("DailyStreak", "StreakKey"),
            RuntimeConstant<string>("LockIntro", "ShownKey"),
            RuntimeConstant<string>("LockIntro", "UsedKey"),
            RuntimeConstant<string>(
                "AdsManager", "PendingStreakRestoreKey"),
            RuntimeConstant<string>(
                "AdsManager", "PendingRewardEarnedKey"),
        };

        foreach (string key in stringKeys)
            PreferenceSnapshots.Add(PreferenceSnapshot.CaptureString(key));
        foreach (string key in intKeys)
            PreferenceSnapshots.Add(PreferenceSnapshot.CaptureInt(key));
    }

    static void RestorePreferences()
    {
        if (preferencesRestored || PreferenceSnapshots.Count == 0)
            return;
        preferencesRestored = true;
        foreach (PreferenceSnapshot snapshot in PreferenceSnapshots)
            snapshot.Restore();
        PlayerPrefs.Save();
    }

    static void HideCaptureOverlays()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid()) return;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform item in
                     root.GetComponentsInChildren<Transform>(true))
            {
                if (item.name == "ConsentPanel" ||
                    item.name == "ForceUpdatePanel")
                    item.gameObject.SetActive(false);
            }
        }
    }

    static bool IsAllowedViewport(int width, int height)
    {
        foreach (Vector2Int viewport in AllowedViewports)
            if (viewport.x == width && viewport.y == height)
                return true;
        return false;
    }

    static Button FindActiveButton(string name)
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Button found = FindButton(root.transform, name, true);
            if (found != null) return found;
        }
        return null;
    }

    static Button FindButton(Transform root, string name, bool requireActive)
    {
        if (root == null) return null;
        if (root.name == name &&
            (!requireActive || root.gameObject.activeInHierarchy))
            return root.GetComponent<Button>();
        for (int index = 0; index < root.childCount; index++)
        {
            Button found = FindButton(root.GetChild(index), name, requireActive);
            if (found != null) return found;
        }
        return null;
    }

    static RectTransform FindRect(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root as RectTransform;
        for (int index = 0; index < root.childCount; index++)
        {
            RectTransform found = FindRect(root.GetChild(index), name);
            if (found != null) return found;
        }
        return null;
    }

    static Component FindInScene(Type type)
    {
        foreach (GameObject root in
                 SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Component[] components = root.GetComponentsInChildren(type, true);
            if (components.Length > 0) return components[0];
        }
        return null;
    }

    static object Invoke(object target, string method, params object[] arguments)
    {
        MethodInfo info = target.GetType().GetMethod(
            method,
            BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.NonPublic);
        Require(info != null, "Missing runtime method: " + method);
        return info.Invoke(target, arguments);
    }

    static T GetField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.NonPublic);
        Require(field != null, "Missing runtime field: " + fieldName);
        return (T)field.GetValue(target);
    }

    static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.NonPublic);
        Require(field != null, "Missing runtime field: " + fieldName);
        field.SetValue(target, value);
    }

    static T GetProperty<T>(object target, string propertyName)
    {
        PropertyInfo property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.NonPublic);
        Require(property != null, "Missing runtime property: " + propertyName);
        return (T)property.GetValue(target);
    }

    static T RuntimeConstant<T>(string typeName, string fieldName)
    {
        FieldInfo field = RuntimeType(typeName).GetField(
            fieldName,
            BindingFlags.Static | BindingFlags.Public |
            BindingFlags.NonPublic);
        Require(field != null && field.IsLiteral,
            "Missing canonical constant: " + typeName + "." + fieldName);
        return (T)field.GetRawConstantValue();
    }

    static Type RuntimeType(string name)
    {
        Type type = Type.GetType(name + ", Assembly-CSharp");
        Require(type != null, "Missing runtime type: " + name);
        return type;
    }

    static int ReadPositiveInt(string key, int fallback)
    {
        return int.TryParse(ReadArgument(key), out int parsed) && parsed > 0
            ? parsed
            : fallback;
    }

    static string ReadArgument(string key)
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int index = 0; index + 1 < arguments.Length; index++)
        {
            if (string.Equals(
                    arguments[index], key,
                    StringComparison.OrdinalIgnoreCase))
                return arguments[index + 1];
        }
        return null;
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    [Serializable]
    sealed class CaptureLayout
    {
        public int schemaVersion;
        public string coordinateSystem;
        public string state;
        public string language;
        public int requestedWidth;
        public int requestedHeight;
        public int screenWidth;
        public int screenHeight;
        public int captureScale;
        public RectRecord safeArea;
        public List<ElementRecord> elements = new List<ElementRecord>();
        public List<TextRecord> texts = new List<TextRecord>();
        public List<TouchRecord> touchTargets = new List<TouchRecord>();
        public List<RectRecord> dynamicRegions = new List<RectRecord>();
    }

    [Serializable]
    sealed class ElementRecord
    {
        public string name;
        public bool active;
        public RectRecord rect;
    }

    [Serializable]
    sealed class TextRecord
    {
        public string name;
        public bool active;
        public string value;
        public float fontSize;
        public bool overflowing;
        public bool hasGlyphs;
        public RectRecord rect;
        public RectRecord glyph;
    }

    [Serializable]
    sealed class TouchRecord
    {
        public string name;
        public bool active;
        public bool interactable;
        public bool raycastTarget;
        public RectRecord rect;
    }

    [Serializable]
    sealed class RectRecord
    {
        public float x;
        public float y;
        public float width;
        public float height;

        public static RectRecord FromMinMax(
            float minX, float minY, float maxX, float maxY)
        {
            return new RectRecord
            {
                x = minX,
                y = minY,
                width = Mathf.Max(0f, maxX - minX),
                height = Mathf.Max(0f, maxY - minY),
            };
        }
    }

    sealed class PreferenceSnapshot
    {
        enum ValueKind
        {
            Int,
            String,
        }

        readonly ValueKind kind;
        readonly bool existed;
        readonly int intValue;
        readonly string stringValue;

        PreferenceSnapshot(
            string key,
            ValueKind kind,
            bool existed,
            int intValue,
            string stringValue)
        {
            Key = key;
            this.kind = kind;
            this.existed = existed;
            this.intValue = intValue;
            this.stringValue = stringValue;
        }

        public string Key { get; }

        public static PreferenceSnapshot CaptureInt(string key)
        {
            return new PreferenceSnapshot(
                key,
                ValueKind.Int,
                PlayerPrefs.HasKey(key),
                PlayerPrefs.GetInt(key, 0),
                null);
        }

        public static PreferenceSnapshot CaptureString(string key)
        {
            return new PreferenceSnapshot(
                key,
                ValueKind.String,
                PlayerPrefs.HasKey(key),
                0,
                PlayerPrefs.GetString(key, string.Empty));
        }

        public void Restore()
        {
            if (!existed)
            {
                PlayerPrefs.DeleteKey(Key);
                return;
            }

            if (kind == ValueKind.String)
                PlayerPrefs.SetString(Key, stringValue);
            else
                PlayerPrefs.SetInt(Key, intValue);
        }
    }
}
#endif
