using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sole presentation owner for the Solo duel board. GameManager, NumberManager
/// and DuelRules remain gameplay-authoritative; this component renders their
/// typed state inside the approved cartoon composition and keeps the real
/// callback-bearing controls.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class SoloDuelVisuals : MonoBehaviour
{
    public const string VisualRootName = "SoloDuelVisualRoot";
    public const string SafeRootName = "SoloDuelSafeRoot";

    const string BackspaceCommand = "BACKSPACE";

    const string BackgroundResource = "solo/production/solo_background_v1";
    const string LogoResource = "reference/hol_logo_exact";
    const string PlayerResource = "reference/player_cyan_exact";
    const string OpponentResource = "reference/opponent_purple_exact";
    const string OpponentAvatarResource =
        "solo/production/solo_opponent_medallion_v1";
    const string VsResource = "solo/production/solo_vs_burst_v2";
    const string TrophyResource = "solo/production/solo_trophy_v1";
    const string MascotSevenResource = "reference/mascot_7_exact";
    const string MascotThreeResource = "reference/mascot_3_exact";
    const string SpeechBubbleResource =
        "solo/production/solo_opponent_speech_bubble_v2";
    const string DecorationResource =
        "solo/production/solo_decorations_v1";
    const string BackButtonResource = "solo/production/solo_back_button_v1";
    const string ChipFrameResource = "solo/production/solo_player_chip_v1";
    const string PlayerCardResource =
        "solo/production/solo_player_card_shell_v1";
    const string OpponentCardResource =
        "solo/production/solo_opponent_card_shell_v1";
    const string PromptRibbonResource =
        "solo/production/solo_prompt_ribbon_v1";
    const string InteractionBoardResource =
        "solo/production/solo_interaction_board_v2";
    const string InputFieldResource = "solo/production/solo_input_field_v1";
    const string HistoryBoardResource =
        "solo/production/solo_history_board_v1";
    const string TipBoardResource = "solo/production/solo_tip_board_v1";
    const string PrimaryCtaResource = "solo/production/solo_primary_cta_v1";
    const string KeypadKeyResource = "solo/production/solo_keypad_key_v1";
    const string KeyClearIconResource =
        "solo/production/solo_key_clear_icon_v1";
    const string KeyBackspaceIconResource =
        "solo/production/solo_key_backspace_icon_v1";
    const string HistoryHigherResource =
        "solo/production/solo_history_high_v1";
    const string HistoryLowerResource =
        "solo/production/solo_history_low_v1";
    const string HistoryCorrectResource =
        "solo/production/solo_history_correct_v1";
    const string TipBulbResource = "solo/production/solo_tip_bulb_v1";
    const string HistoryUpIconResource =
        "solo/production/solo_history_up_icon_v1";
    const string HistoryDownIconResource =
        "solo/production/solo_history_down_icon_v1";
    const string HistoryCorrectIconResource =
        "solo/production/solo_history_correct_icon_v1";
    const string ReactionEmojiResource =
        "solo/production/solo_reaction_emoji_v1";
    const string TitleSparkleResource =
        "solo/production/solo_title_sparkle_v1";

    const string SoloPurpleFrameResource = "phase2a/hol_tip_frame_r2_9s";
    const string SoloBlueFrameResource = "mainmenu/mainmenu_cta_blue_9s";
    const string SoloMagentaFrameResource = "phase2a/hol_cta_magenta_r2_9s";
    const string SoloGoldFrameResource = "phase2a/hol_cta_gold_r2_9s";
    const string DisplayFontResource = "phase2a/fonts/HOL Menu Display SDF";
    const string BodyFontResource = "phase2a/fonts/HOL Menu Body SDF";

    const float ReferenceWidth = 1080f;
    const float ReferenceHeight = 1920f;
    const float TallReferenceWidth = 757f;
    const float TallReferenceHeight = 1600f;

    static readonly Color CardBlue = new Color(0.08f, 0.28f, 0.68f, 0.96f);
    static readonly Color CardPink = new Color(0.72f, 0.08f, 0.34f, 0.96f);
    static readonly Color KeyBlue = new Color(0.16f, 0.18f, 0.62f, 1f);
    static readonly Color Gold = new Color(1f, 0.76f, 0.10f, 1f);
    static readonly Color NearWhite = new Color(0.985f, 0.975f, 1f, 1f);
    static readonly Color Muted = new Color(0.75f, 0.78f, 0.92f, 1f);
    static readonly Color Ink = new Color(0.09f, 0.05f, 0.16f, 1f);
    static readonly Color Cyan = new Color(0.20f, 0.94f, 1f, 1f);
    static readonly Color Magenta = new Color(1f, 0.22f, 0.55f, 1f);
    static readonly Color Success = new Color(0.34f, 1f, 0.12f, 1f);

    readonly SoloBoardPresentationModel presentation =
        new SoloBoardPresentationModel();
    readonly List<RectTransform> layoutRoots = new List<RectTransform>();

    RectTransform board;
    RectTransform visualRoot;
    RectTransform safeRoot;
    GameObject interactionCard;
    GameObject playerCardRoot;
    GameObject opponentCardRoot;
    GameObject promptRibbonRoot;
    GameObject opponentBubbleRoot;
    RectTransform opponentBubbleTextSafeArea;
    GameObject tipRoot;
    NumberManager numberManager;
    GameManager gameManager;
    MenuManager menuManager;
    TMP_FontAsset displayFont;
    TMP_FontAsset bodyFont;
    TMP_InputField input;
    TMP_Text phaseText;
    TMP_Text roundText;
    TMP_Text currentHeadingText;
    TMP_Text currentRangeText;
    TMP_Text centralGuessText;
    TMP_Text centralOutcomeText;
    TMP_Text playerRangeText;
    TMP_Text aiRangeText;
    TMP_Text lockExplanationText;
    readonly List<GameObject> historyRows = new List<GameObject>();
    readonly List<Image> historyRowImages = new List<Image>();
    readonly List<TMP_Text> historyMetaTexts = new List<TMP_Text>();
    readonly List<TMP_Text> historyNumberTexts = new List<TMP_Text>();
    readonly List<TMP_Text> historyOutcomeTexts = new List<TMP_Text>();
    readonly List<TMP_Text> historyNewestTexts = new List<TMP_Text>();
    readonly List<Image> historyIconImages = new List<Image>();
    RectTransform historyViewport;
    RectTransform historyContent;
    ScrollRect historyScroll;
    int renderedHistoryCount = -1;
    TMP_Text opponentSpeechText;
    TMP_Text opponentGuessText;
    TMP_Text opponentPromptText;
    TMP_Text opponentIdentityText;
    TMP_Text playerNameText;
    TMP_Text playerWinsText;
    TMP_Text opponentDifficultyText;
    TMP_Text playerActiveBadgeText;
    TMP_Text opponentActiveBadgeText;
    TMP_Text playerSecretText;
    TMP_Text playerLatestGuessText;
    TMP_Text opponentLatestGuessText;
    CanvasGroup playerCardCanvasGroup;
    CanvasGroup opponentCardCanvasGroup;
    TMP_Text chipText;
    RectTransform playerAvatarAperture;
    Image playerAvatarImage;
    GameObject historyRoot;
    GameObject keypadRoot;
    GameObject resultRoot;
    TMP_Text resultReasonText;
    TMP_Text resultSecretsText;
    TMP_Text resultTurnsText;
    TMP_Text resultGuessesText;
    GameObject leaveModalRoot;
    TMP_Text leaveTitleText;
    TMP_Text leaveBodyText;
    Button submitControl;
    Button continueControl;
    Button homeControl;
    Button leaveConfirmControl;
    Button leaveCancelControl;
    Button lockControl;
    Button saveStreakControl;
    bool leaveConfirmationVisible;
    bool built;
    int lastLayoutWidth = -1;
    int lastLayoutHeight = -1;
    Rect lastLayoutSafeArea = new Rect(-1f, -1f, -1f, -1f);

    public SoloBoardPresentationState CurrentState => presentation.Current;
    public Button SubmitControl => submitControl;
    public GameObject KeypadRoot => keypadRoot;
    public bool IsLeaveConfirmationVisible => leaveConfirmationVisible;
    public bool IsReady { get; private set; }
    public float CurrentTallBlend { get; private set; }

    public void RegisterLockControl(Button control)
    {
        lockControl = control;
        if (built)
        {
            SeatLockControl();
            LayoutStateControls(CurrentTallBlend);
            Render();
        }
    }

    public void RegisterSaveStreakControl(Button control)
    {
        saveStreakControl = control;
        if (built)
        {
            SeatSaveStreakControl();
            LayoutStateControls(CurrentTallBlend);
        }
    }

    void OnEnable()
    {
        L10n.OnLanguageChanged += Render;
        Render();
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= Render;
    }

    void OnTransformChildrenChanged()
    {
        EnsureVisualRootOnTop();
    }

    void LateUpdate()
    {
        EnsureVisualRootOnTop();
        if (!built)
            return;

        EnforcePhaseVisibility(presentation.Current);
        SuppressRetiredLegacyPanels();

        Rect safeArea = Screen.safeArea;
        if (lastLayoutWidth == Screen.width &&
            lastLayoutHeight == Screen.height &&
            lastLayoutSafeArea == safeArea)
            return;

        ApplyResponsiveLayoutForViewport(
            Mathf.Max(1f, safeArea.width), Mathf.Max(1f, safeArea.height));
        lastLayoutWidth = Screen.width;
        lastLayoutHeight = Screen.height;
        lastLayoutSafeArea = safeArea;
    }

    void Start()
    {
        Invoke(nameof(Build), 0f);
    }

    public void BeginNewMatch(string opponentName)
    {
        presentation.BeginNewMatch(opponentName);
        SetLeaveConfirmationVisible(false);
        Render();
    }

    public void PresentPhase(
        SoloBoardPhase phase,
        SoloBoardPrompt prompt,
        int roundNumber,
        int rangeMin,
        int rangeMax,
        int detailValue = 0)
    {
        presentation.Present(
            phase, prompt, roundNumber, rangeMin, rangeMax, detailValue);
        Render();
    }

    public void RecordPlayerGuess(int guess, DuelRules.Hint hint)
    {
        presentation.RecordPlayerGuess(guess, hint);
        Render();
    }

    public void RecordPlayerGuessResult(int guess, SoloGuessOutcome outcome)
    {
        presentation.RecordPlayerGuessResult(guess, outcome);
        Render();
    }

    public void RecordAiGuess(int guess, DuelRules.Hint hint)
    {
        presentation.RecordAiGuess(guess, hint);
        Render();
    }

    public bool SetPlayerSecret(int value)
    {
        bool changed = presentation.SetPlayerSecret(value);
        Render();
        return changed;
    }

    public bool RevealStarter(
        SoloBoardActor openingActor,
        int roundNumber,
        int playerMin,
        int playerMax,
        int opponentMin,
        int opponentMax)
    {
        bool changed = presentation.RevealStarter(
            openingActor, roundNumber, playerMin, playerMax,
            opponentMin, opponentMax);
        Render();
        return changed;
    }

    public bool BeginPlayerTurn(
        int roundNumber,
        int playerMin,
        int playerMax,
        int opponentMin,
        int opponentMax,
        bool lastLicks)
    {
        bool changed = presentation.BeginPlayerTurn(
            roundNumber, playerMin, playerMax, opponentMin, opponentMax,
            lastLicks);
        Render();
        return changed;
    }

    public bool SetOutcomeDestination(
        bool terminalResultFollows,
        SoloBoardActor nextActor)
    {
        bool changed = presentation.SetOutcomeDestination(
            terminalResultFollows, nextActor);
        Render();
        return changed;
    }

    public bool DismissLatestAiHandoff()
    {
        bool changed = presentation.DismissLatestAiHandoff();
        if (changed)
            Render();
        return changed;
    }

    public string CurrentPresentationTimingText =>
        PresentationTimingText(presentation.Current);

    public bool BeginOpponentThinking(
        int roundNumber,
        int playerMin,
        int playerMax,
        int opponentMin,
        int opponentMax)
    {
        bool changed = presentation.BeginOpponentThinking(
            roundNumber, playerMin, playerMax, opponentMin, opponentMax);
        Render();
        return changed;
    }

    public bool RecordPlayerMove(
        int roundNumber,
        int guess,
        DuelRules.Hint hint,
        bool usedLock,
        int candidatesBefore,
        int newPlayerMin,
        int newPlayerMax,
        int opponentMin,
        int opponentMax)
    {
        bool changed = presentation.RecordPlayerMove(
            roundNumber, guess, hint, usedLock, candidatesBefore,
            newPlayerMin, newPlayerMax, opponentMin, opponentMax);
        Render();
        return changed;
    }

    public bool RecordOpponentMove(
        int roundNumber,
        int guess,
        DuelRules.Hint hint,
        bool usedLock,
        int candidatesBefore,
        int playerMin,
        int playerMax,
        int newOpponentMin,
        int newOpponentMax)
    {
        bool changed = presentation.RecordOpponentMove(
            roundNumber, guess, hint, usedLock, candidatesBefore,
            playerMin, playerMax, newOpponentMin, newOpponentMax);
        Render();
        return changed;
    }

    public bool RevealOpponentOutcome()
    {
        bool changed = presentation.RevealOpponentOutcome();
        Render();
        return changed;
    }

    public bool ShowLastLicks(int roundNumber)
    {
        bool changed = presentation.ShowLastLicks(roundNumber);
        Render();
        return changed;
    }

    public bool ShowLockForfeit(SoloBoardActor actor, int roundNumber)
    {
        bool changed = presentation.ShowLockForfeit(actor, roundNumber);
        Render();
        return changed;
    }

    public bool CompleteMatch(
        DuelRules.Outcome outcome,
        int playerSecret,
        int opponentSecret,
        int playerGuessCount,
        int opponentGuessCount)
    {
        bool changed = presentation.CompleteMatch(
            outcome, playerSecret, opponentSecret,
            playerGuessCount, opponentGuessCount);
        Render();
        return changed;
    }

    public void UpdateLockState(
        bool revealed,
        bool available,
        bool armed,
        bool spent,
        int candidates)
    {
        presentation.UpdateLockState(
            revealed, available, armed, spent, candidates);
        Render();
    }

    public void SetLeaveConfirmationVisible(bool visible)
    {
        leaveConfirmationVisible = visible;
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>(true);
        if (gameManager != null)
            gameManager.SetPresentationSuspended(visible);
        if (leaveModalRoot != null)
        {
            leaveModalRoot.SetActive(visible);
            if (visible)
                leaveModalRoot.transform.SetAsLastSibling();
        }
    }

    void Build()
    {
        if (built) return;

        board = (RectTransform)transform;
        board.localScale = Vector3.one;
        board.localPosition = Vector3.zero;
        numberManager = GetComponent<NumberManager>();
        gameManager = FindObjectOfType<GameManager>(true);
        menuManager = FindObjectOfType<MenuManager>(true);
        input = numberManager != null
            ? numberManager.numberInput
            : FindObjectOfType<TMP_InputField>(true);

        BuildVisualShell();
        if (!IsReady) return;

        BuildHeader();
        BuildHistoryCard();
        LayoutExistingGameplay();
        BuildKeypad();
        SeatControllerOwnedControls();
        BuildSemanticSurfaces();
        BuildLeaveConfirmation();
        SuppressRetiredLegacyPanels();
        built = true;
        Rect safeArea = Screen.safeArea;
        ApplyResponsiveLayoutForViewport(
            Mathf.Max(1f, safeArea.width), Mathf.Max(1f, safeArea.height));
        lastLayoutWidth = Screen.width;
        lastLayoutHeight = Screen.height;
        lastLayoutSafeArea = safeArea;
        Render();
    }

    // The canonical 1080x1920 composition and the approved 757x1600 tall
    // composition are two measured layouts of the same screen. Interpolate
    // between them by aspect ratio; never uniformly shrink the 1920 block and
    // leave the extra height as a dead zone.
    public void ApplyResponsiveLayoutForViewport(float width, float height)
    {
        if (!built || safeRoot == null)
            return;

        float aspect = Mathf.Max(1f, height) / Mathf.Max(1f, width);
        float canonicalAspect = ReferenceHeight / ReferenceWidth;
        float tallAspect = TallReferenceHeight / TallReferenceWidth;
        float blend = Mathf.Clamp01(Mathf.InverseLerp(
            canonicalAspect, tallAspect, aspect));
        CurrentTallBlend = blend;

        LayoutNamed("DuelBack",
            new Vector2(-453f, 860f), new Vector2(-448f, 932f),
            new Vector2(132f, 129f), new Vector2(120f, 120f), blend);
        LayoutNamed("SoloDuelLogo",
            new Vector2(-46f, 835f), new Vector2(-10f, 933f),
            new Vector2(390f, 229f), new Vector2(390f, 229f), blend);
        LayoutNamed("SoloDuelPlayerChip",
            new Vector2(339f, 860f), new Vector2(339f, 932f),
            new Vector2(370f, 150f), new Vector2(310f, 132f), blend);
        LayoutControl(playerAvatarAperture,
            new Vector2(116f, 0f), new Vector2(97f, 0f),
            new Vector2(108f, 108f), new Vector2(96f, 96f), blend);
        LayoutControl(
            playerAvatarImage != null
                ? playerAvatarImage.rectTransform
                : null,
            Vector2.zero, Vector2.zero,
            new Vector2(74f, 74f), new Vector2(66f, 66f), blend);

        LayoutNamed("PlayerCard",
            new Vector2(-276f, 472f), new Vector2(-259f, 586f),
            new Vector2(514f, 620f), new Vector2(526f, 606f), blend, 0.90f);
        LayoutNamed("OpponentCard",
            new Vector2(282f, 470f), new Vector2(266f, 584f),
            new Vector2(514f, 620f), new Vector2(526f, 606f), blend, 0.90f);
        LayoutNamed("SoloVsBurst",
            new Vector2(1f, 508f), new Vector2(1f, 595f),
            new Vector2(338f, 290f), new Vector2(306f, 263f), blend);
        LayoutNamed("SoloDuelMascotSeven",
            new Vector2(-426f, 80f), new Vector2(-420f, 210f),
            new Vector2(220f, 245f), new Vector2(200f, 223f), blend);
        LayoutNamed("SoloDuelMascotThree",
            new Vector2(420f, 80f), new Vector2(393f, 210f),
            new Vector2(225f, 250f), new Vector2(190f, 212f), blend);
        LayoutNamed("SoloPromptRibbon",
            new Vector2(-18f, 86f), new Vector2(-18f, 219f),
            new Vector2(636f, 181f), new Vector2(636f, 181f), blend);
        LayoutControl(roundText != null ? roundText.rectTransform : null,
            new Vector2(0f, 62f), new Vector2(0f, 62f),
            new Vector2(552f, 28f), new Vector2(552f, 28f), blend);
        LayoutControl(centralGuessText != null
                ? centralGuessText.rectTransform
                : null,
            new Vector2(0f, 27f), new Vector2(0f, 27f),
            new Vector2(620f, 46f), new Vector2(620f, 46f), blend);
        LayoutControl(centralOutcomeText != null
                ? centralOutcomeText.rectTransform
                : null,
            new Vector2(0f, -9f), new Vector2(0f, -9f),
            new Vector2(620f, 34f), new Vector2(620f, 34f), blend);
        LayoutControl(phaseText != null ? phaseText.rectTransform : null,
            new Vector2(0f, -50f), new Vector2(0f, -50f),
            new Vector2(572f, 46f), new Vector2(572f, 46f), blend);

        LayoutNamed("SoloInteractionCard",
            new Vector2(-189f, -488f), new Vector2(-175f, -437f),
            new Vector2(760f, 1004f), new Vector2(730f, 1220f), blend);
        LayoutNamed("SoloOpponentRail",
            new Vector2(339f, -488f), new Vector2(321f, -437f),
            new Vector2(362f, 901f), new Vector2(340f, 1220f), blend);
        LayoutNamed("SoloOpponentBubble",
            new Vector2(-45f, 350f), new Vector2(-40f, 457f),
            new Vector2(315f, 220f), new Vector2(315f, 220f), blend);
        LayoutNamed("HistoryCard",
            new Vector2(0f, 20f), new Vector2(0f, 40f),
            new Vector2(374f, 420f), new Vector2(340f, 590f), blend);
        LayoutNamed("SoloTipCard",
            new Vector2(0f, -330f), new Vector2(0f, -425f),
            new Vector2(390f, 260f), new Vector2(342f, 320f), blend);

        LayoutNamed("CurrentNumberHeading",
            new Vector2(-9f, 426f), new Vector2(-9f, 538f),
            new Vector2(612f, 40f), new Vector2(612f, 42f), blend);
        LayoutNamed("CurrentRangeLabel",
            new Vector2(-9f, 379f), new Vector2(-9f, 488f),
            new Vector2(612f, 32f), new Vector2(612f, 34f), blend);
        LayoutControl(input != null ? input.transform as RectTransform : null,
            new Vector2(-13f, 285f), new Vector2(-13f, 385f),
            new Vector2(520f, 140f), new Vector2(520f, 150f), blend);
        LayoutControl(
            numberManager != null && numberManager.playerNumberText != null
                ? numberManager.playerNumberText.rectTransform
                : null,
            new Vector2(-13f, 285f), new Vector2(-13f, 385f),
            new Vector2(520f, 140f), new Vector2(520f, 150f), blend);
        LayoutControl(
            numberManager != null && numberManager.messageText != null
                ? numberManager.messageText.rectTransform
                : null,
            new Vector2(0f, 180f), new Vector2(0f, 275f),
            new Vector2(560f, 46f), new Vector2(560f, 50f), blend);

        LayoutControl(keypadRoot != null
                ? keypadRoot.transform as RectTransform
                : null,
            new Vector2(-1f, -80f), new Vector2(-1f, -65f),
            new Vector2(577f, 440f), new Vector2(577f, 600f), blend);
        LayoutKeypadButtons(blend);
        TMP_Text submitLabel = submitControl != null
            ? submitControl.GetComponentInChildren<TMP_Text>(true)
            : null;
        LayoutControl(submitLabel != null ? submitLabel.rectTransform : null,
            Vector2.zero, Vector2.zero,
            new Vector2(250f, 72f), new Vector2(250f, 96f), blend);

        LayoutControl(resultRoot != null
                ? resultRoot.transform as RectTransform
                : null,
            new Vector2(-9f, 20f), new Vector2(-9f, 40f),
            new Vector2(640f, 660f), new Vector2(640f, 820f), blend);
        LayoutNamed("ResultReason",
            new Vector2(0f, 190f), new Vector2(0f, 250f),
            new Vector2(608f, 150f), new Vector2(608f, 190f), blend);
        LayoutNamed("ResultSecrets",
            new Vector2(0f, 66f), new Vector2(0f, 90f),
            new Vector2(600f, 60f), new Vector2(600f, 70f), blend);
        LayoutNamed("ResultLatestGuesses",
            new Vector2(0f, -2f), new Vector2(0f, 5f),
            new Vector2(600f, 92f), new Vector2(600f, 112f), blend);
        LayoutNamed("ResultTurns",
            new Vector2(0f, -88f), new Vector2(0f, -98f),
            new Vector2(600f, 60f), new Vector2(600f, 72f), blend);

        LayoutHistory(blend);
        LayoutTip(blend);
        LayoutStateControls(blend);
    }

    void LayoutKeypadButtons(float blend)
    {
        if (keypadRoot == null)
            return;

        string[] keys =
        {
            "1", "2", "3", "4", "5", "6",
            "7", "8", "9", "×", "0", BackspaceCommand,
        };
        for (int i = 0; i < keys.Length; i++)
        {
            Transform key = DeepFind(keypadRoot.transform, "Key_" + keys[i]);
            if (key == null)
                continue;
            int column = i % 3;
            int row = i / 3;
            float x = -203f + column * 203f;
            float baseY = 165f - row * 110f;
            float tallY = 225f - row * 150f;
            LayoutControl(key as RectTransform,
                new Vector2(x, baseY), new Vector2(x, tallY),
                new Vector2(186f, 108f), new Vector2(186f, 132f), blend);
        }
    }

    void LayoutHistory(float blend)
    {
        LayoutNamed("HistoryTitle",
            new Vector2(0f, 174f), new Vector2(0f, 255f),
            new Vector2(310f, 50f), new Vector2(310f, 58f), blend);
        LayoutNamed("HistoryTitleSparkleLeft",
            new Vector2(-150f, 174f), new Vector2(-150f, 255f),
            new Vector2(34f, 40f), new Vector2(34f, 40f), blend);
        LayoutNamed("HistoryTitleSparkleRight",
            new Vector2(150f, 174f), new Vector2(150f, 255f),
            new Vector2(34f, 40f), new Vector2(34f, 40f), blend);
        LayoutControl(historyViewport,
            new Vector2(0f, -28f), new Vector2(0f, -42f),
            new Vector2(334f, 310f), new Vector2(314f, 450f), blend);

        if (historyContent == null)
            return;

        float rowHeight = Mathf.Lerp(102f, 114f, blend);
        float viewportHeight = Mathf.Lerp(310f, 450f, blend);
        float contentHeight = Mathf.Max(
            viewportHeight, historyRows.Count * rowHeight);
        historyContent.anchorMin = new Vector2(0f, 1f);
        historyContent.anchorMax = new Vector2(1f, 1f);
        historyContent.pivot = new Vector2(0.5f, 1f);
        historyContent.anchoredPosition = Vector2.zero;
        historyContent.sizeDelta = new Vector2(0f, contentHeight);

        for (int i = 0; i < historyRows.Count; i++)
        {
            RectTransform row = historyRows[i].transform as RectTransform;
            if (row == null)
                continue;
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, -i * rowHeight);
            row.sizeDelta = new Vector2(
                Mathf.Lerp(326f, 306f, blend), rowHeight - 8f);
            row.localRotation = Quaternion.identity;
            row.localScale = Vector3.one;
        }
    }

    void LayoutTip(float blend)
    {
        LayoutNamed("SoloTipHeading",
            new Vector2(-76f, 91f), new Vector2(-66f, 128f),
            new Vector2(176f, 36f), new Vector2(168f, 38f), blend);
        LayoutNamed("SoloTipBulb",
            new Vector2(-157f, 90f), new Vector2(-143f, 128f),
            new Vector2(54f, 61f), new Vector2(54f, 61f), blend);
        LayoutNamed("SoloTipMascot",
            new Vector2(143f, -66f), new Vector2(126f, -70f),
            new Vector2(100f, 117f), new Vector2(100f, 123f), blend);
        LayoutControl(playerRangeText != null
                ? playerRangeText.rectTransform
                : null,
            new Vector2(-8f, 43f), new Vector2(-5f, 80f),
            new Vector2(338f, 32f), new Vector2(290f, 34f), blend);
        LayoutControl(aiRangeText != null ? aiRangeText.rectTransform : null,
            new Vector2(-8f, 14f), new Vector2(-5f, 43f),
            new Vector2(338f, 36f), new Vector2(290f, 50f), blend);
        LayoutControl(lockExplanationText != null
                ? lockExplanationText.rectTransform
                : null,
            new Vector2(-48f, -67f), new Vector2(-44f, -65f),
            new Vector2(258f, 118f), new Vector2(220f, 170f), blend);
    }

    void LayoutStateControls(float blend)
    {
        bool splitNumeric = presentation.Current.Phase ==
                            SoloBoardPhase.PlayerGuess;
        bool splitAcknowledge =
            presentation.Current.AcknowledgeControlVisible &&
            LiveLockVisible(presentation.Current);
        LayoutControl(submitControl != null
                ? submitControl.transform as RectTransform
                : null,
            splitNumeric ? new Vector2(-150f, -385f) :
                new Vector2(-1f, -385f),
            splitNumeric ? new Vector2(-150f, -455f) :
                new Vector2(-1f, -455f),
            splitNumeric ? new Vector2(276f, 94f) :
                new Vector2(575f, 94f),
            splitNumeric ? new Vector2(276f, 120f) :
                new Vector2(575f, 120f), blend);
        LayoutControl(continueControl != null
                ? continueControl.transform as RectTransform
                : null,
            splitAcknowledge ? new Vector2(-150f, -385f) :
                new Vector2(-1f, -385f),
            splitAcknowledge ? new Vector2(-150f, -455f) :
                new Vector2(-1f, -455f),
            splitAcknowledge ? new Vector2(276f, 94f) :
                new Vector2(575f, 94f),
            splitAcknowledge ? new Vector2(276f, 120f) :
                new Vector2(575f, 120f), blend);
        LayoutControl(lockControl != null
                ? lockControl.transform as RectTransform
                : null,
            new Vector2(150f, -385f), new Vector2(150f, -455f),
            new Vector2(276f, 94f), new Vector2(276f, 120f), blend);

        if (gameManager != null)
        {
            LayoutControl(gameManager.stopGameButton != null
                    ? gameManager.stopGameButton.transform as RectTransform
                    : null,
                new Vector2(-150f, -385f), new Vector2(-150f, -455f),
                new Vector2(276f, 94f), new Vector2(276f, 120f), blend);
            LayoutAnswerControl(gameManager.higherButton, -190f, blend);
            LayoutAnswerControl(gameManager.correctButton, 0f, blend);
            LayoutAnswerControl(gameManager.lowerButton, 190f, blend);
        }
        LayoutControl(homeControl != null
                ? homeControl.transform as RectTransform
                : null,
            new Vector2(150f, -385f), new Vector2(150f, -455f),
            new Vector2(276f, 94f), new Vector2(276f, 120f), blend);
        LayoutControl(saveStreakControl != null
                ? saveStreakControl.transform as RectTransform
                : null,
            new Vector2(0f, -275f), new Vector2(0f, -330f),
            new Vector2(560f, 82f), new Vector2(560f, 96f), blend);
    }

    void LayoutAnswerControl(GameObject control, float x, float blend)
    {
        LayoutControl(control != null
                ? control.transform as RectTransform
                : null,
            new Vector2(x, -385f), new Vector2(x, -455f),
            new Vector2(170f, 94f), new Vector2(170f, 120f), blend);
    }

    void LayoutNamed(
        string name,
        Vector2 canonicalPosition,
        Vector2 tallPosition,
        Vector2 canonicalSize,
        Vector2 tallSize,
        float blend,
        float tallScale = 1f)
    {
        if (safeRoot == null)
            return;
        Transform found = DeepFind(safeRoot, name);
        RectTransform rect = found as RectTransform;
        LayoutControl(rect, canonicalPosition, tallPosition,
            canonicalSize, tallSize, blend, tallScale);
    }

    static void LayoutControl(
        RectTransform rect,
        Vector2 canonicalPosition,
        Vector2 tallPosition,
        Vector2 canonicalSize,
        Vector2 tallSize,
        float blend,
        float tallScale = 1f)
    {
        if (rect == null)
            return;
        Place(rect, Vector2.Lerp(canonicalPosition, tallPosition, blend),
            Vector2.Lerp(canonicalSize, tallSize, blend));
        float scale = Mathf.Lerp(1f, tallScale, blend);
        rect.localScale = new Vector3(scale, scale, 1f);
    }

    void BuildVisualShell()
    {
        displayFont = Resources.Load<TMP_FontAsset>(DisplayFontResource);
        bodyFont = Resources.Load<TMP_FontAsset>(BodyFontResource);

        Sprite background = LoadRequired(BackgroundResource);
        Sprite logo = LoadRequired(LogoResource);
        Sprite player = LoadRequired(PlayerResource);
        Sprite opponent = LoadRequired(OpponentResource);
        Sprite fallbackAvatar = LoadRequired(
            PlayerProfileAvatarResolver.FallbackResourcePath);
        Sprite avatar = PlayerProfileAvatarResolver.Resolve();
        Sprite opponentAvatar = LoadRequired(OpponentAvatarResource);
        Sprite vs = LoadRequired(VsResource);
        Sprite trophy = LoadRequired(TrophyResource);
        Sprite seven = LoadRequired(MascotSevenResource);
        Sprite three = LoadRequired(MascotThreeResource);
        Sprite bubble = LoadRequired(SpeechBubbleResource);
        Sprite decorations = LoadRequired(DecorationResource);
        Sprite backButton = LoadRequired(BackButtonResource);
        Sprite chip = LoadRequired(ChipFrameResource);
        Sprite playerCard = LoadRequired(PlayerCardResource);
        Sprite opponentCard = LoadRequired(OpponentCardResource);
        Sprite promptRibbon = LoadRequired(PromptRibbonResource);
        Sprite interactionBoard = LoadRequired(InteractionBoardResource);
        Sprite inputField = LoadRequired(InputFieldResource);
        Sprite historyBoard = LoadRequired(HistoryBoardResource);
        Sprite tipBoard = LoadRequired(TipBoardResource);
        Sprite primaryCta = LoadRequired(PrimaryCtaResource);
        Sprite keypadKey = LoadRequired(KeypadKeyResource);
        Sprite keyClearIcon = LoadRequired(KeyClearIconResource);
        Sprite keyBackspaceIcon = LoadRequired(KeyBackspaceIconResource);
        Sprite historyHigher = LoadRequired(HistoryHigherResource);
        Sprite historyLower = LoadRequired(HistoryLowerResource);
        Sprite historyCorrect = LoadRequired(HistoryCorrectResource);
        Sprite tipBulb = LoadRequired(TipBulbResource);
        Sprite historyUpIcon = LoadRequired(HistoryUpIconResource);
        Sprite historyDownIcon = LoadRequired(HistoryDownIconResource);
        Sprite historyCorrectIcon = LoadRequired(HistoryCorrectIconResource);
        Sprite reactionEmoji = LoadRequired(ReactionEmojiResource);
        Sprite titleSparkle = LoadRequired(TitleSparkleResource);
        Sprite purple = LoadRequired(SoloPurpleFrameResource);
        Sprite blue = LoadRequired(SoloBlueFrameResource);
        Sprite magenta = LoadRequired(SoloMagentaFrameResource);
        Sprite gold = LoadRequired(SoloGoldFrameResource);

        IsReady = ArtReady(
            background, logo, player, opponent, fallbackAvatar, avatar,
            opponentAvatar, vs,
            trophy, seven, three, bubble, decorations, backButton, chip,
            playerCard, opponentCard, promptRibbon, interactionBoard,
            inputField, historyBoard,
            tipBoard, primaryCta, keypadKey, keyClearIcon,
            keyBackspaceIcon, historyHigher, historyLower,
            historyCorrect, tipBulb, historyUpIcon, historyDownIcon,
            historyCorrectIcon, reactionEmoji, titleSparkle,
            purple, blue, magenta, gold) &&
            displayFont != null && bodyFont != null;

        if (!IsReady)
        {
            Debug.LogError(
                "[SoloDuelVisuals] Required production artwork/fonts are missing.");
            return;
        }

        var boardImage = board.GetComponent<Image>();
        if (boardImage != null)
        {
            boardImage.enabled = false;
            boardImage.raycastTarget = false;
        }

        visualRoot = (RectTransform)RuntimeUI.CreateObject(
            VisualRootName, board).transform;
        Stretch(visualRoot);
        visualRoot.SetAsLastSibling();

        var backgroundImage = EnsureImage(visualRoot, "SoloDuelBackground");
        Stretch(backgroundImage.rectTransform);
        ConfigureImage(backgroundImage, background, false, Image.Type.Simple);
        backgroundImage.raycastTarget = true;

        var decorationImage = EnsureImage(visualRoot, "SoloDuelDecorations");
        Stretch(decorationImage.rectTransform);
        ConfigureImage(
            decorationImage, decorations, false, Image.Type.Simple);
        decorationImage.gameObject.SetActive(false);

        safeRoot = EnsureRect(visualRoot, SafeRootName);
        Stretch(safeRoot);
        Canvas canvas = board.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            ResponsiveSafeAreaRoot.Attach(
                safeRoot, canvas.transform as RectTransform,
                new Vector2(ReferenceWidth, ReferenceHeight));
        }

        var logoImage = AddSprite(
            safeRoot, "SoloDuelLogo", logo,
            new Vector2(-46f, 835f), new Vector2(390f, 229f));
        logoImage.preserveAspect = false;
        logoImage.raycastTarget = false;

        var chipImage = EnsureImage(safeRoot, "SoloDuelPlayerChip");
        ConfigureImage(chipImage, chip, false, Image.Type.Simple);
        CenterRoot(
            chipImage.rectTransform, new Vector2(370f, 150f),
            new Vector2(339f, 860f));

        Image avatarMaskImage = EnsureImage(
            chipImage.transform, "SoloDuelChipAvatarAperture");
        ConfigureCircularMask(avatarMaskImage);
        playerAvatarAperture = avatarMaskImage.rectTransform;
        Place(
            playerAvatarAperture, new Vector2(116f, 0f),
            new Vector2(108f, 108f));
        playerAvatarImage = AddSprite(
            playerAvatarAperture, "SoloDuelChipAvatar", avatar,
            Vector2.zero, new Vector2(74f, 74f));
        AddSprite(
            chipImage.transform, "SoloDuelChipTrophy", trophy,
            new Vector2(-102f, 0f), new Vector2(52f, 52f));
        chipText = DisplayLabel(
            chipImage.transform, "SoloDuelChipText", "", 36,
            new Vector2(-18f, 0f), new Vector2(172f, 72f), NearWhite);

        AddSprite(
            safeRoot, "SoloDuelMascotSeven", seven,
            new Vector2(-426f, 80f), new Vector2(220f, 245f));
        AddSprite(
            safeRoot, "SoloDuelMascotThree", three,
            new Vector2(420f, 80f), new Vector2(225f, 250f));
    }

    void BuildHeader()
    {
        GameObject backObject = RuntimeUI.CreateObject("DuelBack", safeRoot);
        var back = backObject.AddComponent<Button>();
        backObject.AddComponent<Image>();
        StyleSoloButton(back, SoloPurpleFrameResource, NearWhite);
        ConfigureImage(
            back.GetComponent<Image>(), LoadRequired(BackButtonResource),
            true, Image.Type.Simple);
        back.GetComponent<Image>().raycastTarget = true;
        back.targetGraphic = back.GetComponent<Image>();
        CenterRoot(
            (RectTransform)back.transform, new Vector2(132f, 129f),
            new Vector2(-453f, 860f));
        HideButtonLabels(back.transform);
        back.onClick.AddListener(RequestMatchExit);

        playerCardRoot = ArtFrame(
            safeRoot, "PlayerCard", new Vector2(-276f, 472f),
            new Vector2(514f, 620f), PlayerCardResource, false);
        playerCardCanvasGroup = playerCardRoot.GetComponent<CanvasGroup>();
        if (playerCardCanvasGroup == null)
            playerCardCanvasGroup = playerCardRoot.AddComponent<CanvasGroup>();
        AddSprite(
            playerCardRoot.transform, "PlayerCharacter", LoadRequired(PlayerResource),
            new Vector2(-52f, 62f), new Vector2(350f, 350f));
        TMP_Text playerCaption = DisplayLabel(
            playerCardRoot.transform, "PlayerCaption",
            L10n.Get("solo_you_header"), 38,
            new Vector2(0f, 248f), new Vector2(320f, 52f), NearWhite);
        RuntimeUI.Localize(playerCaption, "solo_you_header");
        GameObject playerBadge = Frame(
            playerCardRoot.transform, "PlayerActiveBadge",
            new Vector2(0f, 202f), new Vector2(226f, 46f),
            SoloBlueFrameResource);
        playerActiveBadgeText = DisplayLabel(
            playerBadge.transform, "PlayerActiveBadgeLabel", "", 24,
            Vector2.zero, new Vector2(202f, 38f), NearWhite);
        playerActiveBadgeText.enableAutoSizing = true;
        playerActiveBadgeText.fontSizeMin = 16f;
        playerActiveBadgeText.fontSizeMax = 24f;
        playerNameText = DisplayLabel(
            playerCardRoot.transform, "PlayerName",
            PlayerPrefs.GetString("PlayerName", L10n.Get("player_default")),
            50, new Vector2(0f, -126f), new Vector2(420f, 60f), NearWhite);
        ConfigureDisplayText(playerNameText, 36f, 50f);
        playerNameText.enableWordWrapping = false;
        playerNameText.margin = new Vector4(8f, 0f, 8f, 0f);
        playerSecretText = BodyLabel(
            playerCardRoot.transform, "PlayerSecretValue", "", 25,
            new Vector2(0f, -178f), new Vector2(390f, 42f), Gold);
        playerSecretText.alignment = TextAlignmentOptions.Center;
        playerLatestGuessText = BodyLabel(
            playerCardRoot.transform, "PlayerLatestGuess", "", 23,
            new Vector2(0f, -218f), new Vector2(408f, 38f), NearWhite);
        playerLatestGuessText.alignment = TextAlignmentOptions.Center;
        ConfigureBodyText(playerLatestGuessText, 19f, 23f);
        playerLatestGuessText.enableWordWrapping = false;
        AddSprite(
            playerCardRoot.transform, "PlayerCardTrophy", LoadRequired(TrophyResource),
            new Vector2(-92f, -270f), new Vector2(48f, 48f));
        playerWinsText = DisplayLabel(
            playerCardRoot.transform, "PlayerWins", string.Empty, 38,
            new Vector2(36f, -270f), new Vector2(144f, 50f), Gold);

        opponentCardRoot = ArtFrame(
            safeRoot, "OpponentCard", new Vector2(282f, 470f),
            new Vector2(514f, 620f), OpponentCardResource, false);
        opponentCardCanvasGroup = opponentCardRoot.GetComponent<CanvasGroup>();
        if (opponentCardCanvasGroup == null)
            opponentCardCanvasGroup = opponentCardRoot.AddComponent<CanvasGroup>();
        AddSprite(
            opponentCardRoot.transform, "OpponentCharacter",
            LoadRequired(OpponentResource), new Vector2(-20f, 72f),
            new Vector2(344f, 344f));
        TMP_Text opponentCaption = DisplayLabel(
            opponentCardRoot.transform, "OpponentCaption",
            L10n.Get("prebattle_opponent"), 37,
            new Vector2(0f, 248f), new Vector2(320f, 52f), NearWhite);
        RuntimeUI.Localize(opponentCaption, "prebattle_opponent");
        GameObject opponentBadge = Frame(
            opponentCardRoot.transform, "OpponentActiveBadge",
            new Vector2(0f, 202f), new Vector2(226f, 46f),
            SoloMagentaFrameResource);
        opponentActiveBadgeText = DisplayLabel(
            opponentBadge.transform, "OpponentActiveBadgeLabel", "", 24,
            new Vector2(-1f, 0f), new Vector2(216f, 38f), NearWhite);
        opponentActiveBadgeText.enableAutoSizing = true;
        opponentActiveBadgeText.fontSizeMin = 16f;
        opponentActiveBadgeText.fontSizeMax = 24f;
        opponentActiveBadgeText.margin = new Vector4(3f, 0f, 3f, 0f);

        opponentIdentityText = gameManager != null
            ? gameManager.opponentNameText
            : null;
        if (opponentIdentityText == null)
        {
            opponentIdentityText = DisplayLabel(
                opponentCardRoot.transform, "OpponentIdentity", "", 50,
                new Vector2(0f, -126f), new Vector2(420f, 60f), NearWhite);
        }
        else
        {
            Reparent(opponentIdentityText.transform, opponentCardRoot.transform);
            Place(
                opponentIdentityText.rectTransform,
                new Vector2(0f, -126f), new Vector2(420f, 60f));
            ConfigureDisplayText(opponentIdentityText, 36f, 50f);
            opponentIdentityText.enableAutoSizing = true;
            opponentIdentityText.fontSize = 50f;
            opponentIdentityText.color = NearWhite;
            opponentIdentityText.alignment = TextAlignmentOptions.Center;
        }
        ConfigureDisplayText(opponentIdentityText, 36f, 50f);
        opponentIdentityText.enableAutoSizing = true;
        opponentIdentityText.fontSizeMin = 36f;
        opponentIdentityText.fontSizeMax = 50f;
        opponentIdentityText.margin = new Vector4(5f, 0f, 5f, 0f);
        opponentIdentityText.enableWordWrapping = false;

        opponentLatestGuessText = BodyLabel(
            opponentCardRoot.transform, "OpponentLatestGuess", "", 23,
            new Vector2(0f, -210f), new Vector2(408f, 38f), NearWhite);
        opponentLatestGuessText.alignment = TextAlignmentOptions.Center;
        ConfigureBodyText(opponentLatestGuessText, 19f, 23f);
        opponentLatestGuessText.enableWordWrapping = false;

        AddSprite(
            opponentCardRoot.transform, "OpponentCardTrophy", LoadRequired(TrophyResource),
            new Vector2(-116f, -270f), new Vector2(48f, 48f));
        opponentDifficultyText = DisplayLabel(
            opponentCardRoot.transform, "OpponentDifficulty", string.Empty, 30,
            new Vector2(48f, -260f), new Vector2(252f, 72f), Gold);
        opponentDifficultyText.enableAutoSizing = true;
        opponentDifficultyText.fontSizeMin = 24f;
        opponentDifficultyText.fontSizeMax = 32f;
        opponentDifficultyText.enableWordWrapping = false;
        opponentDifficultyText.margin = new Vector4(6f, 0f, 6f, 0f);

        Image vsBurst = AddSprite(
            safeRoot, "SoloVsBurst", LoadRequired(VsResource),
            new Vector2(1f, 508f), new Vector2(338f, 290f));
        TMP_Text vsOutline = DisplayLabel(
            vsBurst.transform, "SoloVsOutline", "VS", 80,
            new Vector2(0f, -2f), new Vector2(168f, 108f), Ink);
        vsOutline.enableAutoSizing = false;
        vsOutline.fontSize = 80f;
        TMP_Text vsLabel = DisplayLabel(
            vsBurst.transform, "SoloVsLabel", "VS", 66,
            new Vector2(0f, 1f), new Vector2(152f, 94f), Gold);
        vsLabel.enableAutoSizing = false;
        vsLabel.fontSize = 66f;

        promptRibbonRoot = ArtFrame(
            safeRoot, "SoloPromptRibbon", new Vector2(-18f, 86f),
            new Vector2(636f, 181f), PromptRibbonResource, false);
        roundText = DisplayLabel(
            promptRibbonRoot.transform, "RoundLabel", "", 24,
            new Vector2(0f, 62f), new Vector2(552f, 28f), NearWhite);
        ConfigureDisplayText(roundText, 21f, 24f);
        roundText.enableWordWrapping = false;

        phaseText = gameManager != null ? gameManager.turnText : null;
        if (phaseText == null)
        {
            phaseText = DisplayLabel(
                promptRibbonRoot.transform, "PhasePrompt", "", 24,
                new Vector2(0f, -50f), new Vector2(572f, 46f), NearWhite);
        }
        else
        {
            Reparent(phaseText.transform, promptRibbonRoot.transform);
            Place(
                phaseText.rectTransform, new Vector2(0f, -50f),
                new Vector2(572f, 46f));
            phaseText.color = NearWhite;
            phaseText.alignment = TextAlignmentOptions.Center;
        }
        ConfigureDisplayText(phaseText, 19f, 24f);
        phaseText.enableAutoSizing = true;
        phaseText.fontSizeMin = 19f;
        phaseText.fontSizeMax = 24f;
        phaseText.enableWordWrapping = true;
        phaseText.overflowMode = TextOverflowModes.Truncate;
        phaseText.margin = new Vector4(8f, 0f, 8f, 0f);
        roundText.outlineWidth = 0.16f;
        phaseText.outlineWidth = 0.16f;

        interactionCard = ArtFrame(
            safeRoot, "SoloInteractionCard", new Vector2(-189f, -488f),
            new Vector2(760f, 1004f), InteractionBoardResource, false);
        currentHeadingText = DisplayLabel(
            interactionCard.transform, "CurrentNumberHeading",
            L10n.Get("hud_current_number"), 30,
            new Vector2(-9f, 426f), new Vector2(612f, 40f), NearWhite);
        currentHeadingText.enableAutoSizing = true;
        currentHeadingText.fontSizeMin = 23f;
        currentHeadingText.fontSizeMax = 30f;
        currentHeadingText.enableWordWrapping = false;
        currentHeadingText.overflowMode = TextOverflowModes.Truncate;
        currentHeadingText.margin = new Vector4(8f, 0f, 8f, 0f);
        currentRangeText = BodyLabel(
            interactionCard.transform, "CurrentRangeLabel", "", 24,
            new Vector2(-9f, 379f), new Vector2(612f, 32f), Gold);
        ConfigureBodyText(currentRangeText, 20f, 24f);
        currentRangeText.enableWordWrapping = false;
        currentRangeText.alignment = TextAlignmentOptions.Center;
        currentRangeText.margin = new Vector4(8f, 0f, 8f, 0f);

        GameObject rail = RuntimeUI.CreateObject("SoloOpponentRail", safeRoot);
        Place(
            rail.transform as RectTransform, new Vector2(339f, -488f),
            new Vector2(362f, 901f));

        opponentBubbleRoot = MirroredArtFrame(
            rail.transform, "SoloOpponentBubble", new Vector2(-45f, 350f),
            new Vector2(315f, 220f), SpeechBubbleResource, false);
        opponentBubbleTextSafeArea = EnsureRect(
            opponentBubbleRoot.transform, "OpponentBubbleTextSafeArea");
        Place(
            opponentBubbleTextSafeArea,
            new Vector2(-43f, 34f), new Vector2(206f, 84f));
        AddSprite(
            opponentBubbleRoot.transform, "OpponentBubbleAvatar",
            LoadRequired(OpponentAvatarResource), new Vector2(160f, -20f),
            new Vector2(130f, 130f));
        opponentPromptText = DisplayLabel(
            opponentBubbleTextSafeArea, "OpponentBubblePrompt", "", 32,
            new Vector2(0f, 5.75f), new Vector2(190f, 80f), Ink);
        ConfigureDisplayText(opponentPromptText, 24f, 28f);
        opponentPromptText.enableWordWrapping = true;
        opponentPromptText.overflowMode = TextOverflowModes.Truncate;
        AddSprite(
            opponentBubbleRoot.transform, "OpponentReaction",
            LoadRequired(ReactionEmojiResource), new Vector2(-45f, -72f),
            new Vector2(106f, 106f));

        tipRoot = ArtFrame(
            rail.transform, "SoloTipCard", new Vector2(0f, -330f),
            new Vector2(390f, 260f), TipBoardResource);
        TMP_Text tipHeading = DisplayLabel(
            tipRoot.transform, "SoloTipHeading",
            L10n.Get("solo_tip_heading"), 27,
            new Vector2(-90f, 91f), new Vector2(150f, 42f), Gold);
        RuntimeUI.Localize(tipHeading, "solo_tip_heading");
        AddSprite(
            tipRoot.transform, "SoloTipBulb", LoadRequired(TipBulbResource),
            new Vector2(-150f, 88f), new Vector2(64f, 72f));
        AddSprite(
            tipRoot.transform, "SoloTipMascot",
            LoadRequired(MascotSevenResource), new Vector2(118f, -31f),
            new Vector2(164f, 190f));
    }

    void LayoutExistingGameplay()
    {
        if (input != null)
        {
            Reparent(input.transform, interactionCard.transform);
            Place(
                input.transform as RectTransform, new Vector2(-13f, 285f),
                new Vector2(520f, 140f));
            input.shouldHideMobileInput = true;
            input.shouldHideSoftKeyboard = true;
            var image = input.GetComponent<Image>();
            if (image == null)
                image = input.gameObject.AddComponent<Image>();
            ConfigureImage(
                image, LoadRequired(InputFieldResource), false,
                Image.Type.Simple);
            image.raycastTarget = true;
            if (input.textComponent != null)
            {
                input.textComponent.font = displayFont;
                input.textComponent.fontSize = 64f;
                input.textComponent.fontStyle = FontStyles.Bold;
                input.textComponent.color = NearWhite;
                input.textComponent.alignment = TextAlignmentOptions.Center;
            }
            TMP_Text placeholder = input.placeholder as TMP_Text;
            if (placeholder != null)
            {
                // This runtime board is the sole owner of its dynamic input
                // cue. A scene-local LocalizedText would restore the legacy
                // generic 1-100 placeholder after this owner has painted the
                // live strategic range.
                LocalizedText legacyOwner =
                    placeholder.GetComponent<LocalizedText>();
                if (legacyOwner != null)
                {
                    legacyOwner.enabled = false;
                    Destroy(legacyOwner);
                }
                placeholder.text = L10n.Get("solo_secret_domain");
                placeholder.font = displayFont;
                placeholder.fontSize = 68f;
                placeholder.fontStyle = FontStyles.Bold;
                placeholder.enableAutoSizing = true;
                placeholder.fontSizeMin = 40f;
                placeholder.fontSizeMax = 68f;
                placeholder.color = NearWhite;
                placeholder.alignment = TextAlignmentOptions.Center;
            }
            input.text = string.Empty;
        }

        if (numberManager != null && numberManager.playerNumberText != null)
        {
            Reparent(
                numberManager.playerNumberText.transform,
                interactionCard.transform);
            Place(
                numberManager.playerNumberText.rectTransform,
                new Vector2(-13f, 285f), new Vector2(520f, 140f));
            numberManager.playerNumberText.font = displayFont;
            numberManager.playerNumberText.alignment =
                TextAlignmentOptions.Center;
            numberManager.playerNumberText.fontSize = 110f;
            numberManager.playerNumberText.fontStyle = FontStyles.Bold;
            numberManager.playerNumberText.color = NearWhite;
        }

        playerRangeText = gameManager != null ? gameManager.rangeText : null;
        if (playerRangeText == null)
        {
            playerRangeText = BodyLabel(
                tipRoot.transform, "PlayerRangeLabel", "", 23,
                new Vector2(-43f, 42f), new Vector2(238f, 36f), NearWhite);
        }
        else
        {
            Reparent(playerRangeText.transform, tipRoot.transform);
            playerRangeText.name = "PlayerRangeLabel";
            Place(
                playerRangeText.rectTransform, new Vector2(-43f, 42f),
                new Vector2(238f, 36f));
        }
        // The scene does not serialize GameManager.rangeText today, but keep
        // both construction paths on one deterministic production policy.
        // Range rows are a single semantic line: fit them horizontally inside
        // the mascot-safe corridor instead of wrapping into a clipped row.
        playerRangeText.font = bodyFont;
        ConfigureBodyText(playerRangeText, 18f, 23f);
        playerRangeText.enableWordWrapping = false;
        playerRangeText.margin = new Vector4(3f, 0f, 3f, 0f);
        playerRangeText.color = NearWhite;
        playerRangeText.alignment = TextAlignmentOptions.Left;
        aiRangeText = BodyLabel(
            tipRoot.transform, "OpponentRangeLabel", "", 22,
            new Vector2(-43f, 5f), new Vector2(238f, 36f), NearWhite);
        aiRangeText.alignment = TextAlignmentOptions.Left;
        ConfigureBodyText(aiRangeText, 18f, 23f);
        aiRangeText.enableWordWrapping = false;
        aiRangeText.margin = new Vector4(3f, 0f, 3f, 0f);
        lockExplanationText = BodyLabel(
            tipRoot.transform, "LockExplanation", "", 19,
            new Vector2(-50f, -67f), new Vector2(230f, 108f), NearWhite);
        lockExplanationText.alignment = TextAlignmentOptions.TopLeft;
        ConfigureBodyText(lockExplanationText, 17f, 20f);
        lockExplanationText.lineSpacing = 1.5f;
        lockExplanationText.margin = new Vector4(3f, 2f, 3f, 2f);

        if (gameManager != null)
        {
            gameManager.rangeText = playerRangeText;

            if (gameManager.aiNumberText != null)
            {
                opponentGuessText = gameManager.aiNumberText;
                Reparent(
                    opponentGuessText.transform,
                    opponentBubbleTextSafeArea);
                Place(
                    opponentGuessText.rectTransform,
                    new Vector2(0f, 5f), new Vector2(190f, 80f));
                opponentGuessText.font = displayFont;
                ConfigureDisplayText(opponentGuessText, 24f, 30f);
                opponentGuessText.enableWordWrapping = true;
                opponentGuessText.overflowMode = TextOverflowModes.Truncate;
                opponentGuessText.margin = new Vector4(5f, 2f, 5f, 2f);
                opponentGuessText.color = Ink;
                opponentGuessText.alignment =
                    TextAlignmentOptions.Center;
            }

            if (gameManager.aiAnswerText != null)
            {
                opponentSpeechText = gameManager.aiAnswerText;
                Reparent(
                    opponentSpeechText.transform,
                    opponentBubbleTextSafeArea);
                Place(
                    opponentSpeechText.rectTransform,
                    new Vector2(0f, 5f), new Vector2(190f, 80f));
                opponentSpeechText.font = displayFont;
                ConfigureDisplayText(opponentSpeechText, 24f, 30f);
                opponentSpeechText.enableAutoSizing = true;
                opponentSpeechText.fontSize = 30f;
                opponentSpeechText.fontSizeMin = 24f;
                opponentSpeechText.fontSizeMax = 30f;
                opponentSpeechText.enableWordWrapping = true;
                opponentSpeechText.overflowMode = TextOverflowModes.Truncate;
                opponentSpeechText.margin = new Vector4(5f, 2f, 5f, 2f);
                opponentSpeechText.color = Ink;
                opponentSpeechText.alignment =
                    TextAlignmentOptions.Center;
            }
        }

    }

    void BuildHistoryCard()
    {
        Transform rail = DeepFind(safeRoot, "SoloOpponentRail");
        historyRoot = ArtFrame(
            rail, "HistoryCard", new Vector2(0f, 20f),
            new Vector2(374f, 420f), HistoryBoardResource, false);
        TMP_Text title = DisplayLabel(
            historyRoot.transform, "HistoryTitle", L10n.Get("hud_history"), 30,
            new Vector2(0f, 174f), new Vector2(310f, 50f), NearWhite);
        RuntimeUI.Localize(title, "hud_history");
        AddSprite(
            historyRoot.transform, "HistoryTitleSparkleLeft",
            LoadRequired(TitleSparkleResource), new Vector2(-150f, 174f),
            new Vector2(34f, 40f));
        AddSprite(
            historyRoot.transform, "HistoryTitleSparkleRight",
            LoadRequired(TitleSparkleResource), new Vector2(150f, 174f),
            new Vector2(34f, 40f));

        historyViewport = EnsureRect(historyRoot.transform, "HistoryViewport");
        Place(historyViewport, new Vector2(0f, -28f), new Vector2(334f, 310f));
        Image viewportImage = historyViewport.GetComponent<Image>();
        if (viewportImage == null)
            viewportImage = historyViewport.gameObject.AddComponent<Image>();
        viewportImage.color = Color.clear;
        viewportImage.raycastTarget = true;
        if (historyViewport.GetComponent<RectMask2D>() == null)
            historyViewport.gameObject.AddComponent<RectMask2D>();

        historyContent = EnsureRect(historyViewport, "HistoryContent");
        historyContent.anchorMin = new Vector2(0f, 1f);
        historyContent.anchorMax = new Vector2(1f, 1f);
        historyContent.pivot = new Vector2(0.5f, 1f);
        historyContent.anchoredPosition = Vector2.zero;
        historyContent.sizeDelta = new Vector2(0f, 310f);

        historyScroll = historyRoot.GetComponent<ScrollRect>();
        if (historyScroll == null)
            historyScroll = historyRoot.AddComponent<ScrollRect>();
        historyScroll.content = historyContent;
        historyScroll.viewport = historyViewport;
        historyScroll.horizontal = false;
        historyScroll.vertical = true;
        historyScroll.movementType = ScrollRect.MovementType.Clamped;
        historyScroll.inertia = true;
        historyScroll.scrollSensitivity = 38f;

        EnsureHistoryRowCount(3);
    }

    void EnsureHistoryRowCount(int required)
    {
        while (historyRows.Count < required)
            BuildHistoryRow(historyRows.Count);
    }

    void BuildHistoryRow(int index)
    {
        GameObject row = ArtFrame(
            historyContent, "HistoryRow" + (index + 1),
            Vector2.zero, new Vector2(326f, 100f),
            HistoryLowerResource, false);
        historyRows.Add(row);
        historyRowImages.Add(row.GetComponent<Image>());

        TMP_Text meta = BodyLabel(
            row.transform, "HistoryMeta", "", 21,
            new Vector2(-47f, 29f), new Vector2(202f, 32f), NearWhite);
        meta.alignment = TextAlignmentOptions.Left;
        ConfigureBodyText(meta, 17f, 21f);
        meta.enableWordWrapping = false;
        meta.margin = new Vector4(2f, 0f, 2f, 0f);
        historyMetaTexts.Add(meta);

        historyNumberTexts.Add(DisplayLabel(
            row.transform, "HistoryNumber", "", 43,
            new Vector2(-112f, -13f), new Vector2(78f, 52f), NearWhite));
        TMP_Text outcome = DisplayLabel(
            row.transform, "HistoryOutcome", "", 25,
            new Vector2(6f, -12f), new Vector2(168f, 54f), NearWhite);
        outcome.enableAutoSizing = true;
        outcome.fontSizeMin = 20f;
        outcome.fontSizeMax = 25f;
        outcome.margin = new Vector4(4f, 0f, 4f, 0f);
        historyOutcomeTexts.Add(outcome);

        TMP_Text newest = BodyLabel(
            row.transform, "HistoryNewest", "", 20,
            new Vector2(115f, 29f), new Vector2(64f, 32f), Gold);
        newest.alignment = TextAlignmentOptions.Center;
        ConfigureBodyText(newest, 17f, 20f);
        newest.enableWordWrapping = false;
        historyNewestTexts.Add(newest);

        historyIconImages.Add(AddSprite(
            row.transform, "HistoryIcon", LoadRequired(HistoryDownIconResource),
            new Vector2(127f, -13f), new Vector2(48f, 48f)));
        row.SetActive(false);
    }

    void BuildKeypad()
    {
        keypadRoot = RuntimeUI.CreateObject(
            "NumberKeypad", interactionCard.transform);
        RectTransform rootRect = (RectTransform)keypadRoot.transform;
        Place(rootRect, new Vector2(-1f, -80f), new Vector2(577f, 440f));

        string[] keys =
        {
            "1", "2", "3",
            "4", "5", "6",
            "7", "8", "9",
            "×", "0", BackspaceCommand,
        };

        for (int i = 0; i < keys.Length; i++)
        {
            int index = i;
            int column = i % 3;
            int row = i / 3;
            bool clearKey = keys[i] == "×";
            bool backspaceKey = keys[i] == BackspaceCommand;
            string label = clearKey || backspaceKey ? string.Empty : keys[i];
            Button button = RuntimeUI.CreateButton(
                keypadRoot.transform, "Key_" + keys[i], label,
                new Vector2(-203f + column * 203f, 165f - row * 110f),
                new Vector2(186f, 108f), KeyBlue, NearWhite);
            StyleSoloButton(button, SoloBlueFrameResource, NearWhite);
            ConfigureImage(
                button.GetComponent<Image>(), LoadRequired(KeypadKeyResource),
                false, Image.Type.Simple);
            button.GetComponent<Image>().raycastTarget = true;
            button.targetGraphic = button.GetComponent<Image>();
            TMP_Text text = button.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.font = displayFont;
                text.fontSize = 76f;
                text.fontStyle = FontStyles.Bold;
                text.enableAutoSizing = false;
                text.gameObject.SetActive(!clearKey && !backspaceKey);
            }
            if (clearKey || backspaceKey)
            {
                AddSprite(
                    button.transform, "KeyActionIcon",
                    LoadRequired(clearKey
                        ? KeyClearIconResource
                        : KeyBackspaceIconResource),
                    Vector2.zero, new Vector2(96f, 68f));
            }
            button.onClick.AddListener(() => OnKeyPressed(keys[index]));
        }

        foreach (Button duplicate in GetComponentsInChildren<Button>(true))
        {
            if (duplicate.name == "NumberSubmit")
                duplicate.gameObject.SetActive(false);
        }

        RectTransform existing = FindChild("ButtonConfirm");
        submitControl = existing != null
            ? existing.GetComponent<Button>()
            : null;
        if (submitControl == null)
        {
            submitControl = RuntimeUI.CreateButton(
                interactionCard.transform, "ButtonConfirm",
                L10n.Get("confirm"), new Vector2(-1f, -383f),
                new Vector2(575f, 94f), Gold, Ink);
            submitControl.onClick.AddListener(SubmitNumber);
        }
        else
        {
            Reparent(submitControl.transform, interactionCard.transform);
            if (submitControl.onClick.GetPersistentEventCount() == 0 &&
                numberManager != null)
                submitControl.onClick.AddListener(numberManager.SubmitNumber);
        }

        Place(
            (RectTransform)submitControl.transform,
            new Vector2(-1f, -385f), new Vector2(575f, 94f));
        StyleSoloButton(submitControl, SoloGoldFrameResource, Ink);
        ConfigureImage(
            submitControl.GetComponent<Image>(), LoadRequired(PrimaryCtaResource),
            false, Image.Type.Simple);
        submitControl.GetComponent<Image>().raycastTarget = true;
        submitControl.targetGraphic = submitControl.GetComponent<Image>();
        TMP_Text submitLabel =
            submitControl.GetComponentInChildren<TMP_Text>(true);
        if (submitLabel != null)
        {
            Place(
                submitLabel.rectTransform, Vector2.zero,
                new Vector2(430f, 76f));
            submitLabel.font = displayFont;
            submitLabel.fontSize = 80f;
            submitLabel.fontStyle = FontStyles.Bold;
            submitLabel.enableAutoSizing = true;
            submitLabel.fontSizeMin = 46f;
            submitLabel.fontSizeMax = 80f;
            submitLabel.characterSpacing = 0f;
            submitLabel.wordSpacing = 0f;
            submitLabel.lineSpacing = 0f;
            submitLabel.enableWordWrapping = false;
            submitLabel.margin = new Vector4(4f, 2f, 4f, 2f);
            submitLabel.overflowMode = TextOverflowModes.Truncate;
            LocalizedText localized =
                submitLabel.GetComponent<LocalizedText>();
            if (localized == null)
            {
                RuntimeUI.Localize(submitControl, "solo_submit");
                localized = submitLabel.GetComponent<LocalizedText>();
            }
            if (localized != null)
                localized.key = "solo_submit";
            submitLabel.text = L10n.Get("solo_submit");
        }
        if (input != null)
            input.onValueChanged.AddListener(OnInputValueChanged);
        ValidateLayout();
    }

    void BuildSemanticSurfaces()
    {
        centralGuessText = DisplayLabel(
            promptRibbonRoot.transform, "CentralGuess", "", 31,
            new Vector2(0f, 27f), new Vector2(620f, 46f), NearWhite);
        centralGuessText.alignment = TextAlignmentOptions.Center;
        centralGuessText.enableAutoSizing = true;
        centralGuessText.fontSizeMin = 24f;
        centralGuessText.fontSizeMax = 31f;
        centralGuessText.enableWordWrapping = false;
        centralGuessText.overflowMode = TextOverflowModes.Truncate;
        centralGuessText.margin = new Vector4(4f, 0f, 4f, 0f);

        centralOutcomeText = DisplayLabel(
            promptRibbonRoot.transform, "CentralOutcome", "", 28,
            new Vector2(0f, -9f), new Vector2(620f, 34f), Gold);
        centralOutcomeText.alignment = TextAlignmentOptions.Center;
        centralOutcomeText.enableAutoSizing = true;
        centralOutcomeText.fontSizeMin = 22f;
        centralOutcomeText.fontSizeMax = 28f;
        centralOutcomeText.enableWordWrapping = false;
        centralOutcomeText.overflowMode = TextOverflowModes.Truncate;
        centralOutcomeText.margin = new Vector4(4f, 0f, 4f, 0f);

        continueControl = RuntimeUI.CreateButton(
            interactionCard.transform, "SoloContinueButton",
            L10n.Get("solo_continue"), new Vector2(-1f, -385f),
            new Vector2(575f, 94f), Gold, Ink);
        StyleSoloButton(continueControl, SoloGoldFrameResource, Ink);
        ConfigureImage(
            continueControl.GetComponent<Image>(),
            LoadRequired(PrimaryCtaResource), false, Image.Type.Simple);
        continueControl.GetComponent<Image>().raycastTarget = true;
        continueControl.targetGraphic = continueControl.GetComponent<Image>();
        ConfigureActionLabel(continueControl, 58f, Ink);
        TMP_Text continueLabel =
            continueControl.GetComponentInChildren<TMP_Text>(true);
        if (continueLabel != null)
        {
            continueLabel.enableWordWrapping = false;
            continueLabel.fontSizeMin = 32f;
        }
        continueControl.onClick.AddListener(AcknowledgePresentation);

        resultRoot = RuntimeUI.CreateObject(
            "SoloResultDetail", interactionCard.transform);
        Place(resultRoot.transform as RectTransform, new Vector2(-9f, 20f),
            new Vector2(640f, 660f));
        resultReasonText = DisplayLabel(
            resultRoot.transform, "ResultReason", "", 34,
            new Vector2(0f, 190f), new Vector2(608f, 150f), NearWhite);
        resultReasonText.alignment = TextAlignmentOptions.Center;
        resultReasonText.enableAutoSizing = true;
        resultReasonText.fontSizeMin = 22f;
        resultReasonText.fontSizeMax = 34f;
        resultSecretsText = BodyLabel(
            resultRoot.transform, "ResultSecrets", "", 27,
            new Vector2(0f, 66f), new Vector2(600f, 60f), Gold);
        resultSecretsText.alignment = TextAlignmentOptions.Center;
        resultGuessesText = BodyLabel(
            resultRoot.transform, "ResultLatestGuesses", "", 25,
            new Vector2(0f, -2f), new Vector2(600f, 92f), NearWhite);
        resultGuessesText.alignment = TextAlignmentOptions.Center;
        resultTurnsText = BodyLabel(
            resultRoot.transform, "ResultTurns", "", 27,
            new Vector2(0f, -88f), new Vector2(600f, 60f), NearWhite);
        resultTurnsText.alignment = TextAlignmentOptions.Center;

        homeControl = RuntimeUI.CreateButton(
            interactionCard.transform, "SoloHomeButton", L10n.Get("solo_home"),
            new Vector2(150f, -385f), new Vector2(276f, 94f),
            CardBlue, NearWhite);
        StyleSoloButton(homeControl, SoloBlueFrameResource, NearWhite);
        ConfigureActionLabel(homeControl, 43f, NearWhite);
        homeControl.onClick.AddListener(RequestMatchExit);
    }

    void BuildLeaveConfirmation()
    {
        leaveModalRoot = RuntimeUI.CreateObject(
            "SoloLeaveConfirmation", visualRoot);
        Stretch(leaveModalRoot.transform as RectTransform);
        Image blocker = leaveModalRoot.AddComponent<Image>();
        blocker.color = new Color(0.035f, 0.015f, 0.09f, 0.84f);
        blocker.raycastTarget = true;

        GameObject modal = Frame(
            leaveModalRoot.transform, "SoloLeaveConfirmationCard",
            Vector2.zero, new Vector2(850f, 560f),
            SoloPurpleFrameResource);
        leaveTitleText = DisplayLabel(
            modal.transform, "SoloLeaveTitle", L10n.Get("solo_leave_title"),
            46, new Vector2(0f, 170f), new Vector2(720f, 92f), NearWhite);
        leaveTitleText.alignment = TextAlignmentOptions.Center;
        leaveTitleText.enableAutoSizing = true;
        leaveTitleText.fontSizeMin = 30f;
        leaveTitleText.fontSizeMax = 46f;
        leaveBodyText = BodyLabel(
            modal.transform, "SoloLeaveBody", L10n.Get("solo_leave_body"),
            31, new Vector2(0f, 55f), new Vector2(690f, 112f), NearWhite);
        leaveBodyText.alignment = TextAlignmentOptions.Center;

        leaveConfirmControl = RuntimeUI.CreateButton(
            modal.transform, "SoloLeaveConfirmButton",
            L10n.Get("solo_leave_confirm"), new Vector2(-200f, -130f),
            new Vector2(360f, 112f), CardPink, NearWhite);
        StyleSoloButton(
            leaveConfirmControl, SoloMagentaFrameResource, NearWhite);
        ConfigureActionLabel(leaveConfirmControl, 34f, NearWhite);

        leaveCancelControl = RuntimeUI.CreateButton(
            modal.transform, "SoloLeaveCancelButton", L10n.Get("cancel"),
            new Vector2(220f, -130f), new Vector2(300f, 112f),
            CardBlue, NearWhite);
        StyleSoloButton(leaveCancelControl, SoloBlueFrameResource, NearWhite);
        ConfigureActionLabel(leaveCancelControl, 38f, NearWhite);

        leaveConfirmControl.onClick.AddListener(ConfirmMatchExit);
        leaveCancelControl.onClick.AddListener(CancelMatchExit);
        leaveModalRoot.SetActive(leaveConfirmationVisible);
    }

    void AcknowledgePresentation()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>(true);
        if (gameManager != null)
            gameManager.AcknowledgePresentation();
    }

    void RequestMatchExit()
    {
        if (menuManager == null)
            menuManager = FindObjectOfType<MenuManager>(true);
        if (menuManager != null)
            menuManager.RequestSoloMatchExit();
    }

    void ConfirmMatchExit()
    {
        if (menuManager == null)
            menuManager = FindObjectOfType<MenuManager>(true);
        if (menuManager != null)
            menuManager.ConfirmSoloMatchExit();
    }

    void CancelMatchExit()
    {
        if (menuManager == null)
            menuManager = FindObjectOfType<MenuManager>(true);
        if (menuManager != null)
            menuManager.CancelSoloMatchExit();
        else
            SetLeaveConfirmationVisible(false);
    }

    void ConfigureActionLabel(Button button, float size, Color color)
    {
        if (button == null)
            return;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
            return;
        label.font = displayFont;
        label.fontSize = size;
        label.enableAutoSizing = true;
        label.fontSizeMin = Mathf.Max(22f, size - 16f);
        label.fontSizeMax = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Truncate;
        label.margin = new Vector4(10f, 3f, 10f, 3f);
    }

    void SeatControllerOwnedControls()
    {
        if (numberManager != null && numberManager.messageText != null)
        {
            TMP_Text message = numberManager.messageText;
            Reparent(message.transform, interactionCard.transform);
            Place(message.rectTransform, new Vector2(0f, 180f),
                new Vector2(560f, 46f));
            message.font = bodyFont;
            message.enableAutoSizing = true;
            message.fontSizeMin = 20f;
            message.fontSizeMax = 26f;
            message.alignment = TextAlignmentOptions.Center;
            message.overflowMode = TextOverflowModes.Overflow;
            message.raycastTarget = false;
        }

        if (gameManager == null)
            return;

        SeatButton(
            gameManager.stopGameButton, interactionCard.transform,
            new Vector2(-150f, -385f), new Vector2(276f, 94f),
            SoloGoldFrameResource, Ink);
        if (gameManager.stopGameButton != null)
            ConfigureActionLabel(
                gameManager.stopGameButton.GetComponent<Button>(), 42f, Ink);

        SeatAnswerControl(
            gameManager.higherButton, new Vector2(-190f, -385f),
            SoloBlueFrameResource, NearWhite);
        SeatAnswerControl(
            gameManager.correctButton, new Vector2(0f, -385f),
            SoloGoldFrameResource, Ink);
        SeatAnswerControl(
            gameManager.lowerButton, new Vector2(190f, -385f),
            SoloMagentaFrameResource, NearWhite);

        if (lockControl == null)
            lockControl = FindNamedButton("LockButton");
        if (saveStreakControl == null)
            saveStreakControl = FindNamedButton("SaveStreakButton");
        SeatLockControl();
        SeatSaveStreakControl();
    }

    void SeatAnswerControl(
        GameObject control,
        Vector2 localPosition,
        string spriteResource,
        Color labelColor)
    {
        SeatButton(
            control, interactionCard.transform, localPosition,
            new Vector2(170f, 94f), spriteResource, labelColor);
    }

    void SeatLockControl()
    {
        if (lockControl == null || interactionCard == null)
            return;

        SeatButton(
            lockControl.gameObject, interactionCard.transform,
            new Vector2(150f, -385f), new Vector2(276f, 94f),
            SoloBlueFrameResource, Ink);
        ConfigureActionLabel(lockControl, 35f, Ink);
    }

    void SeatSaveStreakControl()
    {
        if (saveStreakControl == null || interactionCard == null)
            return;

        SeatButton(
            saveStreakControl.gameObject, interactionCard.transform,
            new Vector2(0f, -275f), new Vector2(560f, 82f),
            SoloPurpleFrameResource, NearWhite);
        ConfigureActionLabel(saveStreakControl, 31f, NearWhite);
    }

    static void SeatButton(
        GameObject control,
        Transform parent,
        Vector2 position,
        Vector2 size,
        string spriteResource,
        Color labelColor)
    {
        if (control == null || parent == null)
            return;

        bool wasActive = control.activeSelf;
        Reparent(control.transform, parent);
        Place(control.transform as RectTransform, position, size);

        Button button = control.GetComponent<Button>();
        if (button != null)
            StyleSoloButton(button, spriteResource, labelColor);
        control.SetActive(wasActive);
    }

    Button FindNamedButton(string objectName)
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
            if (button.name == objectName)
                return button;
        return null;
    }

    void SuppressRetiredLegacyPanels()
    {
        if (numberManager == null)
            return;

        // The legacy scene serialized each "panel" reference on the same
        // GameObject as the TMP label.  Once this sole visual owner reparents
        // those labels into the factual opponent bubble, they are live
        // presentation surfaces rather than retired panels and must follow
        // RenderOpponentBubble's phase visibility.
        if (opponentSpeechText == null ||
            numberManager.playerGuessesPanel !=
            opponentSpeechText.gameObject)
            SetInactive(numberManager.playerGuessesPanel);
        if (opponentGuessText == null ||
            numberManager.aiGuessesPanel != opponentGuessText.gameObject)
            SetInactive(numberManager.aiGuessesPanel);
    }

    void EnforcePhaseVisibility(SoloBoardPresentationState state)
    {
        if (gameManager == null) return;

        SetInactive(gameManager.higherButton);
        SetInactive(gameManager.correctButton);
        SetInactive(gameManager.lowerButton);
    }

    static void SetInactive(GameObject value)
    {
        if (value != null && value.activeSelf)
            value.SetActive(false);
    }

    void Render()
    {
        if (!built) return;
        SoloBoardPresentationState state = presentation.Current;

        if (playerNameText != null)
        {
            string player = PlayerPrefs.GetString("PlayerName", "");
            if (string.IsNullOrWhiteSpace(player))
                player = L10n.Get("player_default");
            playerNameText.text = player;
        }

        if (chipText != null)
            chipText.text = FormatScore(GameStats.Wins);

        if (playerWinsText != null)
            playerWinsText.text = FormatScore(GameStats.Wins);
        if (opponentDifficultyText != null)
            opponentDifficultyText.text = DifficultyLabel();
        if (playerAvatarImage != null)
            playerAvatarImage.sprite = PlayerProfileAvatarResolver.Resolve();

        RenderActorCards(state);

        RenderRibbon(state);

        bool showStrategy = state.Phase != SoloBoardPhase.ChooseSecret &&
                            state.Phase != SoloBoardPhase.MatchResult;
        if (tipRoot != null)
            tipRoot.SetActive(showStrategy);
        if (playerRangeText != null)
            playerRangeText.text = L10n.Get(
                "solo_range_player", state.PlayerRangeMin,
                state.PlayerRangeMax);
        if (aiRangeText != null)
            aiRangeText.text = L10n.Get(
                "solo_range_ai", state.AiRangeMin, state.AiRangeMax);
        if (lockExplanationText != null)
            lockExplanationText.text = LockExplanation(state);

        RenderHistoryRows(state);
        if (historyRoot != null)
            historyRoot.SetActive(state.Phase != SoloBoardPhase.ChooseSecret);

        if (opponentIdentityText != null)
        {
            opponentIdentityText.text = OpponentName(state);
            opponentIdentityText.transform.SetAsLastSibling();
        }

        bool numeric = state.NumericControlsAvailable;
        if (numberManager != null && numberManager.playerNumberText != null)
            numberManager.playerNumberText.gameObject.SetActive(false);

        bool showInputGuidance = state.Phase == SoloBoardPhase.ChooseSecret ||
                                 state.Phase == SoloBoardPhase.PlayerGuess;
        if (currentHeadingText != null)
        {
            currentHeadingText.gameObject.SetActive(showInputGuidance);
            currentHeadingText.text = showInputGuidance
                ? CurrentHeading(state)
                : string.Empty;
        }
        if (currentRangeText != null)
        {
            string currentRange = CurrentRange(state);
            currentRangeText.gameObject.SetActive(
                !string.IsNullOrWhiteSpace(currentRange));
            currentRangeText.text = currentRange;
        }
        RenderInputCue(state);

        RenderOpponentBubble(state);
        if (input != null)
        {
            input.shouldHideMobileInput = true;
            input.shouldHideSoftKeyboard = true;
            input.interactable = numeric;
            input.gameObject.SetActive(numeric);
            if (!numeric)
                input.DeactivateInputField();
        }
        if (keypadRoot != null)
            keypadRoot.SetActive(numeric);
        if (numberManager != null && numberManager.messageText != null &&
            !numeric)
            numberManager.messageText.gameObject.SetActive(false);
        if (submitControl != null)
        {
            submitControl.interactable = numeric && numberManager != null &&
                                         numberManager.CanSubmitCurrentValue;
            submitControl.gameObject.SetActive(state.SubmitControlVisible);
        }
        if (continueControl != null)
        {
            continueControl.gameObject.SetActive(
                state.AcknowledgeControlVisible);
            continueControl.interactable = state.AcknowledgeControlVisible;
            TMP_Text label = continueControl.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = AcknowledgeLabel(state.NextAction);
        }
        if (lockControl != null)
        {
            bool showLock = LiveLockVisible(state);
            lockControl.gameObject.SetActive(showLock);
            lockControl.interactable = showLock && LockCanBePressed(state);
        }

        RenderResult(state);
        LayoutStateControls(CurrentTallBlend);
        RenderLeaveConfirmation();

        EnforcePhaseVisibility(state);
        SuppressRetiredLegacyPanels();
        EnsureVisualRootOnTop();
    }

    void RenderActorCards(SoloBoardPresentationState state)
    {
        SoloBoardActor active = state.ActiveActor;
        if (state.Phase == SoloBoardPhase.ChooseSecret)
            active = SoloBoardActor.Player;
        bool hasActive = active == SoloBoardActor.Player ||
                         active == SoloBoardActor.Opponent;
        if (playerCardCanvasGroup != null)
            playerCardCanvasGroup.alpha = hasActive &&
                                          active != SoloBoardActor.Player
                ? 0.56f
                : 1f;
        if (opponentCardCanvasGroup != null)
            opponentCardCanvasGroup.alpha = hasActive &&
                                            active != SoloBoardActor.Opponent
                ? 0.56f
                : 1f;

        if (playerActiveBadgeText != null)
            playerActiveBadgeText.text = active == SoloBoardActor.Player
                ? L10n.Get("solo_player_active")
                : L10n.Get("solo_waiting");
        if (opponentActiveBadgeText != null)
            opponentActiveBadgeText.text = active == SoloBoardActor.Opponent
                ? L10n.Get("solo_opponent_active", OpponentName(state))
                : L10n.Get("solo_waiting");
        if (playerSecretText != null)
            playerSecretText.text = state.PlayerSecretNumber > 0
                ? L10n.Get("solo_secret_value", state.PlayerSecretNumber)
                : L10n.Get("solo_secret_unset");
        if (playerLatestGuessText != null)
            playerLatestGuessText.text = state.LatestPlayerGuess > 0
                ? L10n.Get("solo_player_latest_guess", state.LatestPlayerGuess)
                : L10n.Get("solo_player_no_guess");
        if (opponentLatestGuessText != null)
            opponentLatestGuessText.text = state.LatestAiGuess > 0
                ? L10n.Get("solo_ai_latest_guess", state.LatestAiGuess)
                : L10n.Get("solo_ai_no_guess");
    }

    void RenderRibbon(SoloBoardPresentationState state)
    {
        if (roundText != null)
        {
            bool showRound = state.RoundNumber > 0;
            roundText.gameObject.SetActive(showRound);
            roundText.text = showRound
                ? L10n.Get("round_label_open", state.RoundNumber)
                : string.Empty;
        }

        SetRibbonText(centralGuessText, RibbonAction(state));
        SetRibbonText(centralOutcomeText, RibbonOutcome(state));
        SetRibbonText(phaseText, RibbonHandoff(state));
        if (centralOutcomeText != null)
            centralOutcomeText.color = OutcomeColorForState(state);
    }

    static void SetRibbonText(TMP_Text label, string value)
    {
        if (label == null)
            return;
        bool visible = !string.IsNullOrWhiteSpace(value);
        label.gameObject.SetActive(visible);
        label.text = visible ? value : string.Empty;
    }

    void RenderOpponentBubble(SoloBoardPresentationState state)
    {
        bool pinnedAiHandoff = state.Phase == SoloBoardPhase.PlayerGuess &&
                               state.LatestAiHandoffPinned;
        bool playerTaunt = state.Phase == SoloBoardPhase.PlayerGuess &&
                           !pinnedAiHandoff;
        bool thinking = state.Phase == SoloBoardPhase.OpponentThinking;
        bool guessReveal = state.Phase == SoloBoardPhase.OpponentGuess;
        if (opponentBubbleRoot != null)
            opponentBubbleRoot.SetActive(playerTaunt || thinking || guessReveal);
        if (opponentPromptText != null)
        {
            opponentPromptText.gameObject.SetActive(playerTaunt);
            opponentPromptText.text = playerTaunt
                ? L10n.Get("solo_ai_taunt")
                : "";
        }
        if (opponentGuessText != null)
        {
            opponentGuessText.gameObject.SetActive(guessReveal);
            opponentGuessText.text = guessReveal
                ? L10n.Get("solo_ai_bubble_guess", state.LatestAiGuess)
                : "";
        }
        if (opponentSpeechText == null)
            return;
        opponentSpeechText.gameObject.SetActive(thinking);
        if (thinking)
            opponentSpeechText.text = L10n.Get("solo_ai_thinking_flavor");
        else
            opponentSpeechText.text = "";
    }

    void RenderInputCue(SoloBoardPresentationState state)
    {
        if (input == null)
            return;
        TMP_Text placeholder = input.placeholder as TMP_Text;
        if (placeholder == null)
            return;

        LocalizedText legacyOwner = placeholder.GetComponent<LocalizedText>();
        if (legacyOwner != null && legacyOwner.enabled)
            legacyOwner.enabled = false;
        placeholder.text = state.Phase == SoloBoardPhase.ChooseSecret
            ? L10n.Get("solo_secret_domain")
            : L10n.Get(
                "solo_input_range",
                state.PlayerRangeMin,
                state.PlayerRangeMax);
    }

    static string OpponentName(SoloBoardPresentationState state)
    {
        return string.IsNullOrWhiteSpace(state.OpponentName)
            ? L10n.Get("prebattle_opponent")
            : state.OpponentName;
    }

    static string CurrentHeading(SoloBoardPresentationState state)
    {
        if (state.Phase == SoloBoardPhase.ChooseSecret)
            return L10n.Get("solo_secret_action_heading");
        if (state.Phase == SoloBoardPhase.StarterReveal)
            return L10n.Get("solo_starter_heading");
        if (state.Phase == SoloBoardPhase.PlayerGuess)
            return L10n.Get("solo_guess_target_heading", OpponentName(state));
        if (state.Phase == SoloBoardPhase.PlayerOutcome ||
            state.Phase == SoloBoardPhase.LastLicks)
            return L10n.Get("solo_current_guess", OpponentName(state));
        if (state.Phase == SoloBoardPhase.OpponentThinking ||
            state.Phase == SoloBoardPhase.OpponentGuess ||
            state.Phase == SoloBoardPhase.AnswerOpponent)
            return L10n.Get("solo_current_ai_guess", OpponentName(state));
        if (state.Phase == SoloBoardPhase.LockForfeit)
            return L10n.Get("solo_lock_forfeit_heading");
        if (state.Phase == SoloBoardPhase.RoundResolution)
            return L10n.Get("solo_round_update_heading");
        if (state.Phase == SoloBoardPhase.MatchResult)
            return L10n.Get("result_page_title");
        return L10n.Get("solo_round_update_heading");
    }

    static string CurrentRange(SoloBoardPresentationState state)
    {
        if (state.Phase == SoloBoardPhase.ChooseSecret)
            return L10n.Get("solo_legal_domain");
        if (state.Phase == SoloBoardPhase.PlayerGuess)
            return L10n.Get(
                "solo_strategic_legal_range",
                state.PlayerRangeMin,
                state.PlayerRangeMax);
        return string.Empty;
    }

    static string RibbonAction(SoloBoardPresentationState state)
    {
        if (state.Phase == SoloBoardPhase.MatchResult)
            return PromptText(state);
        if (state.Phase == SoloBoardPhase.ChooseSecret)
            return string.Empty;
        if (state.Phase == SoloBoardPhase.PlayerGuess)
        {
            return state.LatestAiHandoffPinned
                ? L10n.Get(
                    "solo_opponent_guessed", OpponentName(state),
                    state.LatestAiGuess)
                : string.Empty;
        }

        switch (state.Phase)
        {
            case SoloBoardPhase.StarterReveal:
                return state.Starter == SoloBoardActor.Player
                    ? L10n.Get("solo_player_starts")
                    : L10n.Get("solo_opponent_starts", OpponentName(state));
            case SoloBoardPhase.PlayerOutcome:
                return L10n.Get("solo_you_guessed", state.LatestPlayerGuess);
            case SoloBoardPhase.OpponentThinking:
                return L10n.Get("opponent_thinking", OpponentName(state));
            case SoloBoardPhase.OpponentGuess:
            case SoloBoardPhase.AnswerOpponent:
                return L10n.Get(
                    "solo_opponent_guessed", OpponentName(state),
                    state.LatestAiGuess);
            case SoloBoardPhase.LastLicks:
                return L10n.Get("solo_last_licks", OpponentName(state));
            case SoloBoardPhase.LockForfeit:
                return L10n.Get("solo_lock_failed_short");
            case SoloBoardPhase.RoundResolution:
                return L10n.Get("solo_round_update_heading");
            default:
                return string.Empty;
        }
    }

    static string RibbonOutcome(SoloBoardPresentationState state)
    {
        if (state.Phase == SoloBoardPhase.PlayerOutcome)
            return PlayerTargetRelation(state);
        if (state.Phase == SoloBoardPhase.AnswerOpponent ||
            (state.Phase == SoloBoardPhase.PlayerGuess &&
             state.LatestAiHandoffPinned))
            return AiTargetRelation(state);
        if (state.Phase == SoloBoardPhase.StarterReveal)
            return L10n.Get("solo_objective_short", OpponentName(state));
        if (state.Phase == SoloBoardPhase.LockForfeit)
        {
            return state.ActiveActor == SoloBoardActor.Opponent
                ? L10n.Get("solo_player_lock_penalty_short")
                : L10n.Get(
                    "solo_ai_lock_penalty_short", OpponentName(state));
        }
        return string.Empty;
    }

    static string RibbonHandoff(SoloBoardPresentationState state)
    {
        if (state.Phase == SoloBoardPhase.MatchResult)
            return string.Empty;
        if (state.Phase == SoloBoardPhase.ChooseSecret ||
            (state.Phase == SoloBoardPhase.PlayerGuess &&
             !state.LatestAiHandoffPinned))
            return PromptText(state);
        if (state.Phase == SoloBoardPhase.PlayerOutcome)
            return PlayerHandoffLine(state);
        if (state.Phase == SoloBoardPhase.AnswerOpponent ||
            (state.Phase == SoloBoardPhase.PlayerGuess &&
             state.LatestAiHandoffPinned))
            return AiHandoffLine(state);
        if (state.Phase == SoloBoardPhase.OpponentGuess)
            return L10n.Get("solo_ai_result_pending");
        if (state.Phase == SoloBoardPhase.LockForfeit)
            return HandoffDestination(state, state.ActiveActor);
        if (state.Phase == SoloBoardPhase.LastLicks)
            return L10n.Get("solo_your_turn_short");
        return string.Empty;
    }

    static Color OutcomeColorForState(SoloBoardPresentationState state)
    {
        if (state.Phase == SoloBoardPhase.PlayerOutcome)
            return HistoryOutcomeColor(state.LatestPlayerOutcome);
        if (state.Phase == SoloBoardPhase.AnswerOpponent ||
            (state.Phase == SoloBoardPhase.PlayerGuess &&
             state.LatestAiHandoffPinned))
            return HistoryOutcomeColor(state.LatestAiOutcome);
        return Gold;
    }

    static string LockExplanation(SoloBoardPresentationState state)
    {
        if (state.LockSpent)
            return L10n.Get("solo_lock_spent_reason");
        if (state.LockArmed)
            return L10n.Get("solo_lock_locked");
        if (LockCanBePressed(state))
            return L10n.Get("solo_lock_available");
        return L10n.Get("solo_lock_after_guess");
    }

    static string WithLatestLockFact(
        SoloBoardPresentationState state,
        SoloBoardActor actor,
        string label)
    {
        if (state.History.Count == 0)
            return label;

        SoloHistoryEvent latest = state.History[state.History.Count - 1];
        if (latest.Actor != actor || !latest.LockStaked)
            return label;
        return label + " • " + L10n.Get(
            latest.LockMissed
                ? "solo_lock_failed_short"
                : "solo_lock_success_short");
    }

    static string PlayerTargetRelation(SoloBoardPresentationState state)
    {
        string key = state.LatestPlayerOutcome == SoloGuessOutcome.Higher
            ? "solo_target_number_higher"
            : state.LatestPlayerOutcome == SoloGuessOutcome.Lower
                ? "solo_target_number_lower"
                : "solo_target_number_correct";
        return WithLatestLockFact(
            state, SoloBoardActor.Player,
            L10n.Get(key, OpponentName(state)));
    }

    static string AiTargetRelation(SoloBoardPresentationState state)
    {
        string key = state.LatestAiOutcome == SoloGuessOutcome.Higher
            ? "your_number_is_higher"
            : state.LatestAiOutcome == SoloGuessOutcome.Lower
                ? "your_number_is_lower"
                : "your_number_is_correct";
        return WithLatestLockFact(
            state, SoloBoardActor.Opponent, L10n.Get(key));
    }

    static string HandoffDestination(
        SoloBoardPresentationState state,
        SoloBoardActor normalActor)
    {
        if (state.ResultFollows)
            return L10n.Get("solo_result_next");
        SoloBoardActor actor = state.HandoffActor == SoloBoardActor.None
            ? normalActor
            : state.HandoffActor;
        return actor == SoloBoardActor.Player
            ? L10n.Get("solo_your_turn_short")
            : L10n.Get("solo_opponent_turn_short", OpponentName(state));
    }

    static string PlayerHandoffBody(SoloBoardPresentationState state)
    {
        return L10n.Get(
            "solo_handoff_summary",
            PlayerTargetRelation(state),
            state.PlayerRangeMin,
            state.PlayerRangeMax,
            HandoffDestination(state, SoloBoardActor.Opponent));
    }

    static string AiHandoffBody(SoloBoardPresentationState state)
    {
        return L10n.Get(
            "solo_handoff_summary",
            AiTargetRelation(state),
            state.AiRangeMin,
            state.AiRangeMax,
            HandoffDestination(state, SoloBoardActor.Player));
    }

    static string PlayerHandoffLine(SoloBoardPresentationState state)
    {
        return L10n.Get(
            "solo_player_range_handoff",
            state.PlayerRangeMin,
            state.PlayerRangeMax,
            HandoffDestination(state, SoloBoardActor.Opponent));
    }

    static string AiHandoffLine(SoloBoardPresentationState state)
    {
        return L10n.Get(
            "solo_ai_range_handoff",
            state.AiRangeMin,
            state.AiRangeMax,
            HandoffDestination(state, SoloBoardActor.Player));
    }

    static string PlayerOutcomeSummary(SoloBoardPresentationState state)
    {
        return L10n.Get("solo_you_guessed", state.LatestPlayerGuess) +
               "\n" + PlayerHandoffBody(state);
    }

    static string AiOutcomeSummary(SoloBoardPresentationState state)
    {
        return L10n.Get(
                   "solo_opponent_guessed", OpponentName(state),
                   state.LatestAiGuess) +
               "\n" + AiHandoffBody(state);
    }

    static string PresentationTimingText(SoloBoardPresentationState state)
    {
        if (state.Phase == SoloBoardPhase.PlayerOutcome)
            return PlayerOutcomeSummary(state);
        if (state.Phase == SoloBoardPhase.AnswerOpponent)
            return AiOutcomeSummary(state);
        if (state.Phase == SoloBoardPhase.OpponentGuess)
            return RibbonAction(state) + "\n" + RibbonHandoff(state);
        if (state.Phase == SoloBoardPhase.OpponentThinking)
            return L10n.Get("opponent_thinking", OpponentName(state));
        return PromptText(state);
    }

    static bool LockCanBePressed(SoloBoardPresentationState state)
    {
        // DuelRules exposes one Lock from the opening player turn. The old
        // first-guess reveal was only a tutorial presentation choice and must
        // not make the real action unavailable.
        return state.Phase == SoloBoardPhase.PlayerGuess &&
               !state.LockSpent &&
               (state.LockAvailable || !state.LockRevealed || state.LockArmed);
    }

    static bool LiveLockVisible(SoloBoardPresentationState state)
    {
        return state.Phase != SoloBoardPhase.ChooseSecret &&
               state.Phase != SoloBoardPhase.MatchResult;
    }

    static string AcknowledgeLabel(SoloBoardNextAction action)
    {
        switch (action)
        {
            case SoloBoardNextAction.Start:
                return L10n.Get("solo_start");
            case SoloBoardNextAction.RevealGuess:
                return L10n.Get("solo_reveal_guess");
            case SoloBoardNextAction.RevealOutcome:
                return L10n.Get("solo_reveal_outcome");
            default:
                return L10n.Get("solo_continue");
        }
    }

    void RenderResult(SoloBoardPresentationState state)
    {
        bool show = state.Phase == SoloBoardPhase.MatchResult;
        if (resultRoot != null)
            resultRoot.SetActive(show);
        if (homeControl != null)
            homeControl.gameObject.SetActive(show);
        if (gameManager != null && gameManager.stopGameButton != null)
        {
            gameManager.stopGameButton.SetActive(show);
            TMP_Text rematch = gameManager.stopGameButton
                .GetComponentInChildren<TMP_Text>(true);
            if (rematch != null && show)
                rematch.text = L10n.Get("rematch");
        }
        if (!show)
        {
            if (saveStreakControl != null)
                saveStreakControl.gameObject.SetActive(false);
            return;
        }

        string opponent = OpponentName(state);
        if (resultReasonText != null)
            resultReasonText.text = ResultReason(state, opponent);
        if (resultSecretsText != null)
        {
            bool secretsKnown = state.PlayerSecretNumber > 0 &&
                                state.OpponentSecretNumber > 0;
            resultSecretsText.gameObject.SetActive(secretsKnown);
            resultSecretsText.text = secretsKnown
                ? L10n.Get(
                    "solo_result_secrets", state.PlayerSecretNumber,
                    opponent, state.OpponentSecretNumber)
                : "";
        }
        if (resultGuessesText != null)
        {
            string decisive = DecisiveEvents(state);
            if (!string.IsNullOrEmpty(decisive))
                resultGuessesText.text = decisive;
            else
            {
                string playerGuess = state.LatestPlayerGuess > 0
                    ? state.LatestPlayerGuess.ToString(
                        CultureInfo.InvariantCulture)
                    : "—";
                string aiGuess = state.LatestAiGuess > 0
                    ? state.LatestAiGuess.ToString(
                        CultureInfo.InvariantCulture)
                    : "—";
                resultGuessesText.text = L10n.Get("solo_you_header") + ": " +
                    L10n.Get("solo_latest_guess", playerGuess) + "\n" +
                    opponent + ": " + L10n.Get("solo_latest_guess", aiGuess);
            }
        }
        if (resultTurnsText != null)
            resultTurnsText.text = L10n.Get(
                "solo_result_turns", state.PlayerTurns,
                opponent, state.AiTurns);
        if (homeControl != null)
        {
            TMP_Text home = homeControl.GetComponentInChildren<TMP_Text>(true);
            if (home != null)
                home.text = L10n.Get("solo_home");
        }
    }

    static string ResultReason(
        SoloBoardPresentationState state,
        string opponent)
    {
        List<SoloHistoryEvent> terminal = TerminalRoundEvents(state);
        var correct = new List<SoloHistoryEvent>(2);
        foreach (SoloHistoryEvent item in terminal)
        {
            if (item.Outcome == SoloGuessOutcome.Correct)
                correct.Add(item);
        }

        if (correct.Count == 1 &&
            OutcomeMatches(state.MatchOutcome, correct[0].Actor))
        {
            SoloHistoryEvent winner = correct[0];
            SoloBoardActor loser = winner.Actor == SoloBoardActor.Player
                ? SoloBoardActor.Opponent
                : SoloBoardActor.Player;
            if (terminal.Count == 1 && PreviousLockMiss(state, loser))
            {
                return L10n.Get(
                    "solo_result_forfeit_decider", winner.RoundNumber,
                    ActorLabel(winner.Actor, state), winner.Guess,
                    ActorLabel(loser, state));
            }
            return L10n.Get(
                "solo_result_only_correct", winner.RoundNumber,
                ActorLabel(winner.Actor, state), winner.Guess);
        }

        if (correct.Count == 2)
        {
            SoloHistoryEvent first = correct[0];
            SoloHistoryEvent second = correct[1];
            if (first.LockStaked != second.LockStaked)
            {
                SoloHistoryEvent winner = first.LockStaked ? first : second;
                if (OutcomeMatches(state.MatchOutcome, winner.Actor))
                {
                    return L10n.Get(
                        "solo_result_lock_tiebreak",
                        ActorLabel(winner.Actor, state), winner.Guess);
                }
            }
            else if (first.CandidatesBefore != second.CandidatesBefore)
            {
                SoloHistoryEvent winner =
                    first.CandidatesBefore < second.CandidatesBefore
                        ? first
                        : second;
                SoloHistoryEvent loser = ReferenceEquals(winner, first)
                    ? second
                    : first;
                if (OutcomeMatches(state.MatchOutcome, winner.Actor))
                {
                    return L10n.Get(
                        "solo_result_range_tiebreak",
                        ActorLabel(winner.Actor, state), winner.Guess,
                        winner.CandidatesBefore, loser.CandidatesBefore);
                }
            }
            else if (state.MatchOutcome == DuelRules.Outcome.Draw)
            {
                return L10n.Get(
                    "solo_result_exact_draw", first.CandidatesBefore);
            }
        }

        if (state.MatchOutcome == DuelRules.Outcome.HostWins)
            return L10n.Get("solo_result_win_reason", opponent);
        if (state.MatchOutcome == DuelRules.Outcome.GuestWins)
            return L10n.Get("solo_result_loss_reason", opponent);
        return L10n.Get("solo_result_draw_reason");
    }

    static bool OutcomeMatches(
        DuelRules.Outcome outcome,
        SoloBoardActor winner)
    {
        return outcome == DuelRules.Outcome.HostWins &&
               winner == SoloBoardActor.Player ||
               outcome == DuelRules.Outcome.GuestWins &&
               winner == SoloBoardActor.Opponent;
    }

    static bool PreviousLockMiss(
        SoloBoardPresentationState state,
        SoloBoardActor actor)
    {
        for (int index = state.History.Count - 1; index >= 0; index--)
        {
            SoloHistoryEvent item = state.History[index];
            if (item.Actor == actor)
                return item.LockMissed;
        }
        return false;
    }

    static List<SoloHistoryEvent> TerminalRoundEvents(
        SoloBoardPresentationState state)
    {
        var result = new List<SoloHistoryEvent>(2);
        foreach (SoloHistoryEvent item in state.History)
        {
            if (item.RoundNumber == state.RoundNumber)
                result.Add(item);
        }
        return result;
    }

    static string DecisiveEvents(SoloBoardPresentationState state)
    {
        List<SoloHistoryEvent> terminal = TerminalRoundEvents(state);
        if (terminal.Count == 0)
            return string.Empty;

        var lines = new List<string>(2);
        foreach (SoloHistoryEvent item in terminal)
        {
            lines.Add(
                L10n.Get("solo_history_round", item.RoundNumber) + " • " +
                ActorLabel(item.Actor, state) + " > " +
                ActorLabel(item.Target, state) + " • " +
                item.Guess.ToString(CultureInfo.InvariantCulture) + " • " +
                HistoryOutcomeLabel(item.Outcome) +
                (item.LockStaked ? " • " + L10n.Get("lock") : ""));
        }
        return string.Join("\n", lines.ToArray());
    }

    void RenderLeaveConfirmation()
    {
        if (leaveTitleText != null)
            leaveTitleText.text = L10n.Get("solo_leave_title");
        if (leaveBodyText != null)
            leaveBodyText.text = L10n.Get("solo_leave_body");
        if (leaveConfirmControl != null)
        {
            TMP_Text confirm = leaveConfirmControl
                .GetComponentInChildren<TMP_Text>(true);
            if (confirm != null)
                confirm.text = L10n.Get("solo_leave_confirm");
        }
        if (leaveCancelControl != null)
        {
            TMP_Text cancel = leaveCancelControl
                .GetComponentInChildren<TMP_Text>(true);
            if (cancel != null)
                cancel.text = L10n.Get("cancel");
        }
        if (leaveModalRoot != null)
        {
            leaveModalRoot.SetActive(leaveConfirmationVisible);
            if (leaveConfirmationVisible)
                leaveModalRoot.transform.SetAsLastSibling();
        }
    }

    void EnsureVisualRootOnTop()
    {
        if (visualRoot == null || visualRoot.parent == null)
            return;
        if (visualRoot.GetSiblingIndex() != visualRoot.parent.childCount - 1)
            visualRoot.SetAsLastSibling();
    }

    void RenderHistoryRows(SoloBoardPresentationState state)
    {
        int historyCount = state.History.Count;
        EnsureHistoryRowCount(Mathf.Max(3, historyCount));
        for (int i = 0; i < historyRows.Count; i++)
        {
            bool active = i < historyCount;
            if (historyRows[i] != null)
                historyRows[i].SetActive(active);
            if (!active) continue;

            SoloHistoryEvent item = state.History[i];
            bool awaitingReveal = i == historyCount - 1 &&
                                  item.Actor == SoloBoardActor.Opponent &&
                                  state.Phase == SoloBoardPhase.OpponentGuess;
            SoloGuessOutcome outcome = awaitingReveal
                ? SoloGuessOutcome.Unknown
                : item.Outcome;
            string actor = ActorLabel(item.Actor, state);
            string target = ActorLabel(item.Target, state);
            historyMetaTexts[i].text = L10n.Get(
                "solo_history_round", item.RoundNumber) + "  •  " +
                actor + " > " + target;
            historyNumberTexts[i].text =
                item.Guess.ToString(CultureInfo.InvariantCulture);
            historyOutcomeTexts[i].text = HistoryOutcomeLabel(outcome) +
                (!awaitingReveal && item.LockStaked
                    ? " • " + L10n.Get(
                        item.LockMissed
                            ? "solo_lock_failed_short"
                            : "solo_lock_success_short")
                    : "");
            historyNewestTexts[i].text = i == historyCount - 1
                ? L10n.Get("solo_history_newest")
                : "";
            Color color = HistoryOutcomeColor(outcome);
            historyNumberTexts[i].color = color;
            historyOutcomeTexts[i].color = NearWhite;
            ConfigureHistoryIcon(historyIconImages[i], outcome);
            ConfigureImage(
                historyRowImages[i], HistoryOutcomeSprite(outcome),
                false, Image.Type.Simple);
            if (outcome == SoloGuessOutcome.Unknown)
                historyRowImages[i].color = new Color(
                    0.58f, 0.60f, 0.72f, 1f);
        }

        LayoutHistory(CurrentTallBlend);
        if (historyCount != renderedHistoryCount && historyScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            historyScroll.verticalNormalizedPosition = 0f;
            renderedHistoryCount = historyCount;
        }
    }

    static string ActorLabel(
        SoloBoardActor actor, SoloBoardPresentationState state)
    {
        if (actor == SoloBoardActor.Player)
            return L10n.Get("solo_you_header");
        if (actor == SoloBoardActor.Opponent)
            return OpponentName(state);
        return "—";
    }

    static string HistoryOutcomeLabel(SoloGuessOutcome outcome)
    {
        switch (outcome)
        {
            case SoloGuessOutcome.Higher:
                return L10n.Get("solo_history_higher");
            case SoloGuessOutcome.Lower:
                return L10n.Get("solo_history_lower");
            case SoloGuessOutcome.Correct:
                return L10n.Get("solo_history_correct");
            default:
                return string.Empty;
        }
    }

    static void ConfigureHistoryIcon(
        Image icon, SoloGuessOutcome outcome)
    {
        if (icon == null) return;
        switch (outcome)
        {
            case SoloGuessOutcome.Higher:
                ConfigureImage(
                    icon, LoadRequired(HistoryUpIconResource), true,
                    Image.Type.Simple);
                break;
            case SoloGuessOutcome.Lower:
                ConfigureImage(
                    icon, LoadRequired(HistoryDownIconResource), true,
                    Image.Type.Simple);
                break;
            case SoloGuessOutcome.Correct:
                ConfigureImage(
                    icon, LoadRequired(HistoryCorrectIconResource), true,
                    Image.Type.Simple);
                icon.rectTransform.localRotation = Quaternion.identity;
                break;
            default:
                icon.enabled = false;
                return;
        }
        icon.enabled = true;
        icon.color = Color.white;
    }

    static Color HistoryOutcomeColor(SoloGuessOutcome outcome)
    {
        switch (outcome)
        {
            case SoloGuessOutcome.Higher: return Magenta;
            case SoloGuessOutcome.Lower: return Cyan;
            case SoloGuessOutcome.Correct: return Success;
            default: return Muted;
        }
    }

    static Sprite HistoryOutcomeSprite(SoloGuessOutcome outcome)
    {
        switch (outcome)
        {
            case SoloGuessOutcome.Higher:
                return LoadRequired(HistoryHigherResource);
            case SoloGuessOutcome.Lower:
                return LoadRequired(HistoryLowerResource);
            case SoloGuessOutcome.Correct:
                return LoadRequired(HistoryCorrectResource);
            default:
                return LoadRequired(HistoryLowerResource);
        }
    }

    static string DifficultyLabel()
    {
        string[] keys = { "easy", "normal", "hard", "adaptive" };
        int difficulty = Mathf.Clamp(
            PlayerPrefs.GetInt("AIDifficulty", 1), 0, keys.Length - 1);
        return "AI " + L10n.Get(keys[difficulty]).ToUpperInvariant();
    }

    static string FormatScore(int value)
    {
        return Mathf.Max(0, value).ToString(
            "N0", CultureInfo.InvariantCulture);
    }

    static string PromptText(SoloBoardPresentationState state)
    {
        string opponent = OpponentName(state);
        if (state.Phase == SoloBoardPhase.PlayerGuess &&
            state.LatestAiHandoffPinned)
            return AiOutcomeSummary(state);

        switch (state.Prompt)
        {
            case SoloBoardPrompt.EnterSecret:
                return L10n.Get("solo_choose_secret");
            case SoloBoardPrompt.YourGuess:
                return L10n.Get("solo_player_turn", opponent);
            case SoloBoardPrompt.OpponentThinking:
                return L10n.Get("solo_opponent_turn", opponent);
            case SoloBoardPrompt.AnswerOpponent:
                return L10n.Get("solo_opponent_turn", opponent);
            case SoloBoardPrompt.OpponentGuessedHigher:
                return L10n.Get("solo_opponent_turn", opponent) + "\n" +
                       L10n.Get("your_number_is_higher");
            case SoloBoardPrompt.OpponentGuessedLower:
                return L10n.Get("solo_opponent_turn", opponent) + "\n" +
                       L10n.Get("your_number_is_lower");
            case SoloBoardPrompt.OpponentGuessedCorrect:
                return L10n.Get("solo_opponent_turn", opponent) + "\n" +
                       L10n.Get("your_number_is_correct");
            case SoloBoardPrompt.OpponentForfeits:
                return L10n.Get("opponent_forfeits", opponent);
            case SoloBoardPrompt.MatchPoint:
                return L10n.Get("solo_player_turn", opponent) + "\n" +
                       L10n.Get("match_point");
            case SoloBoardPrompt.MatchPointYours:
                return L10n.Get("match_point_yours", opponent);
            case SoloBoardPrompt.TurnForfeited:
                return L10n.Get("turn_forfeited");
            case SoloBoardPrompt.ResolvingRound:
                return string.Empty;
            case SoloBoardPrompt.PlayerStarts:
                return L10n.Get("solo_player_starts") + "\n" +
                       L10n.Get("solo_player_objective", opponent);
            case SoloBoardPrompt.OpponentStarts:
                return L10n.Get("solo_opponent_starts", opponent) + "\n" +
                       L10n.Get("solo_opponent_objective", opponent);
            case SoloBoardPrompt.PlayerGuessedHigher:
            case SoloBoardPrompt.PlayerGuessedLower:
            case SoloBoardPrompt.PlayerGuessedCorrect:
                return PlayerOutcomeSummary(state);
            case SoloBoardPrompt.OpponentGuess:
                return L10n.Get("solo_opponent_turn", opponent) + "\n" +
                       L10n.Get(
                           "solo_opponent_guessed", opponent,
                           state.LatestAiGuess);
            case SoloBoardPrompt.LastLicks:
                return L10n.Get("solo_last_licks", opponent);
            case SoloBoardPrompt.PlayerLockForfeit:
                return L10n.Get("solo_player_lock_forfeit");
            case SoloBoardPrompt.OpponentLockForfeit:
                return L10n.Get("solo_ai_lock_forfeit", opponent);
            case SoloBoardPrompt.Win:
                return L10n.Get("you_win");
            case SoloBoardPrompt.Loss:
                return L10n.Get("you_lose");
            case SoloBoardPrompt.Draw:
                return L10n.Get("you_draw");
            default:
                return string.Empty;
        }
    }

    RectTransform FindChild(string name)
    {
        foreach (RectTransform rect in
                 GetComponentsInChildren<RectTransform>(true))
        {
            if (rect.name == name)
                return rect;
        }
        return null;
    }

    void ValidateLayout()
    {
        if (board == null) return;
        if (Vector3.Distance(board.localScale, Vector3.one) > 0.001f)
            Debug.LogWarning("HOL layout: PanelGAME scale is not 1.0.");

        Canvas canvas = board.parent != null
            ? board.parent.GetComponentInParent<Canvas>()
            : null;
        CanvasScaler scaler = canvas != null
            ? canvas.GetComponent<CanvasScaler>()
            : null;
        if (scaler != null &&
            (scaler.referenceResolution.x != 1080f ||
             scaler.referenceResolution.y != 1920f))
        {
            Debug.LogWarning(
                "HOL layout: expected CanvasScaler reference resolution 1080x1920.");
        }

        foreach (RectTransform rect in layoutRoots)
        {
            if (rect == null) continue;
            if (rect.anchorMin != new Vector2(0.5f, 0.5f) ||
                rect.anchorMax != new Vector2(0.5f, 0.5f) ||
                rect.pivot != new Vector2(0.5f, 0.5f))
            {
                Debug.LogWarning(
                    "HOL layout: non-centered root " + rect.name);
            }
            if (rect.rect.width < 48f || rect.rect.height < 48f)
            {
                Debug.LogWarning(
                    "HOL layout: touch target below 48px: " + rect.name);
            }
        }
    }

    void OnInputValueChanged(string unused)
    {
        if (!built || submitControl == null)
            return;
        bool numeric = presentation.Current.NumericControlsAvailable;
        if (numeric && !string.IsNullOrEmpty(input != null ? input.text : null))
            DismissLatestAiHandoff();
        submitControl.interactable = numeric && numberManager != null &&
                                     numberManager.CanSubmitCurrentValue;
        if (numeric && numberManager != null &&
            numberManager.HasCompleteValidValue &&
            numberManager.messageText != null)
            numberManager.messageText.gameObject.SetActive(false);
    }

    void OnKeyPressed(string key)
    {
        if (input == null || !input.interactable) return;
        DismissLatestAiHandoff();
        if (key == "×")
        {
            input.text = string.Empty;
            return;
        }

        if (key == BackspaceCommand)
        {
            if (!string.IsNullOrEmpty(input.text))
                input.text = input.text.Substring(0, input.text.Length - 1);
            return;
        }

        if (input.text.Length >= 3) return;
        input.text += key;
        input.caretPosition = input.text.Length;
    }

    void SubmitNumber()
    {
        if (numberManager != null)
            numberManager.SubmitNumber();
    }

    GameObject Frame(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        string resource)
    {
        GameObject frame = RuntimeUI.CreateObject(name, parent);
        RectTransform rect = (RectTransform)frame.transform;
        Place(rect, position, size);
        if (parent == safeRoot)
        {
            if (!layoutRoots.Contains(rect))
                layoutRoots.Add(rect);
        }
        var image = frame.AddComponent<Image>();
        RuntimeUI.ApplyProductionSprite(
            image, resource, Image.Type.Sliced, false, 2f);
        image.raycastTarget = false;
        return frame;
    }

    GameObject ArtFrame(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        string resource,
        bool preserveAspect = true)
    {
        GameObject frame = RuntimeUI.CreateObject(name, parent);
        RectTransform rect = (RectTransform)frame.transform;
        Place(rect, position, size);
        if (parent == safeRoot && !layoutRoots.Contains(rect))
            layoutRoots.Add(rect);

        var image = frame.AddComponent<Image>();
        ConfigureImage(
            image, LoadRequired(resource), preserveAspect, Image.Type.Simple);
        image.raycastTarget = false;
        return frame;
    }

    GameObject MirroredArtFrame(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        string resource,
        bool preserveAspect = true)
    {
        GameObject frame = RuntimeUI.CreateObject(name, parent);
        RectTransform rect = (RectTransform)frame.transform;
        Place(rect, position, size);
        if (parent == safeRoot && !layoutRoots.Contains(rect))
            layoutRoots.Add(rect);

        RectTransform artwork = EnsureRect(frame.transform, name + "Artwork");
        Place(artwork, Vector2.zero, size);
        var image = artwork.GetComponent<Image>();
        if (image == null)
            image = artwork.gameObject.AddComponent<Image>();
        ConfigureImage(
            image, LoadRequired(resource), preserveAspect, Image.Type.Simple);
        image.raycastTarget = false;
        artwork.localScale = new Vector3(-1f, 1f, 1f);
        artwork.SetAsFirstSibling();
        return frame;
    }

    Image AddSprite(
        Transform parent,
        string name,
        Sprite sprite,
        Vector2 position,
        Vector2 size)
    {
        RectTransform rect = EnsureRect(parent, name);
        Place(rect, position, size);
        var image = rect.GetComponent<Image>();
        if (image == null)
            image = rect.gameObject.AddComponent<Image>();
        ConfigureImage(image, sprite, true, Image.Type.Simple);
        return image;
    }

    TextMeshProUGUI DisplayLabel(
        Transform parent,
        string name,
        string value,
        int size,
        Vector2 position,
        Vector2 dimensions,
        Color color)
    {
        TextMeshProUGUI label = RuntimeUI.CreateText(
            parent, name, value, size, position, dimensions, color);
        label.font = displayFont;
        ConfigureDisplayText(
            label, Mathf.Max(20f, size - 9f), size + 1f);
        label.enableAutoSizing = false;
        label.fontSize = size;
        return label;
    }

    TextMeshProUGUI BodyLabel(
        Transform parent,
        string name,
        string value,
        int size,
        Vector2 position,
        Vector2 dimensions,
        Color color)
    {
        TextMeshProUGUI label = RuntimeUI.CreateText(
            parent, name, value, size, position, dimensions, color);
        label.font = bodyFont;
        ConfigureBodyText(
            label, Mathf.Max(18f, size - 6f), size + 1f);
        return label;
    }

    void CenterRoot(RectTransform rect, Vector2 size, Vector2 position)
    {
        if (rect == null) return;
        Place(rect, position, size);
        if (!layoutRoots.Contains(rect))
            layoutRoots.Add(rect);
    }

    static void StyleSoloButton(
        Button button,
        string resource,
        Color labelColor)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image == null)
            image = button.gameObject.AddComponent<Image>();
        RuntimeUI.ApplyProductionSprite(
            image, resource, Image.Type.Sliced, false, 2f);
        image.raycastTarget = true;
        button.targetGraphic = image;

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.80f, 0.84f, 0.94f, 1f);
        colors.disabledColor = new Color(0.56f, 0.58f, 0.68f, 0.72f);
        colors.fadeDuration = 0.06f;
        colors.colorMultiplier = 1f;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = colors;
        RuntimeUI.AttachJuice(button);

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.gameObject.SetActive(true);
            label.color = labelColor;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.characterSpacing = 0f;
            label.wordSpacing = 0f;
            label.lineSpacing = 0f;
            label.raycastTarget = false;
        }
    }

    static void ConfigureDisplayText(
        TMP_Text text,
        float minimum,
        float maximum)
    {
        text.fontStyle = FontStyles.Bold;
        text.enableAutoSizing = true;
        text.fontSizeMin = minimum;
        text.fontSizeMax = maximum;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.outlineColor = Ink;
        text.outlineWidth = 0.16f;

        var shadow = text.GetComponent<Shadow>();
        if (shadow == null)
            shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.02f, 0.01f, 0.12f, 0.68f);
        shadow.effectDistance = new Vector2(3f, -4f);
        shadow.useGraphicAlpha = true;
    }

    static void ConfigureBodyText(
        TMP_Text text,
        float minimum,
        float maximum)
    {
        text.enableAutoSizing = true;
        text.fontSizeMin = minimum;
        text.fontSizeMax = maximum;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
    }

    static void HideButtonLabels(Transform root)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            text.gameObject.SetActive(false);
        foreach (Text text in root.GetComponentsInChildren<Text>(true))
            text.gameObject.SetActive(false);
    }

    static void Reparent(Transform child, Transform parent)
    {
        if (child == null || parent == null) return;
        bool wasActive = child.gameObject.activeSelf;
        if (child.parent != parent)
            child.SetParent(parent, false);
        child.SetAsLastSibling();
        child.gameObject.SetActive(wasActive);
    }

    static bool ArtReady(params Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0) return false;
        foreach (Sprite sprite in sprites)
            if (sprite == null) return false;
        return true;
    }

    static Sprite LoadRequired(string resource)
    {
        Sprite sprite = Resources.Load<Sprite>(resource);
        if (sprite == null)
        {
            Debug.LogError(
                "[SoloDuelVisuals] Missing Resources/" + resource + ".");
        }
        return sprite;
    }

    static RectTransform EnsureRect(Transform parent, string name)
    {
        Transform existing = DirectChild(parent, name);
        if (existing is RectTransform rect)
        {
            rect.gameObject.SetActive(true);
            return rect;
        }
        return (RectTransform)RuntimeUI.CreateObject(name, parent).transform;
    }

    static Image EnsureImage(Transform parent, string name)
    {
        RectTransform rect = EnsureRect(parent, name);
        var image = rect.GetComponent<Image>();
        if (image == null)
            image = rect.gameObject.AddComponent<Image>();
        return image;
    }

    static void ConfigureImage(
        Image image,
        Sprite sprite,
        bool preserveAspect,
        Image.Type type)
    {
        image.enabled = true;
        image.sprite = sprite;
        image.type = type;
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    static void ConfigureCircularMask(Image maskImage)
    {
        Sprite maskSprite =
            Resources.Load<Sprite>(
                PlayerProfileAvatarResolver.CircularApertureResourcePath);
        if (maskSprite == null)
        {
            Debug.LogError(
                "[SoloDuelVisuals] Shared circular avatar mask is missing.");
            maskImage.gameObject.SetActive(false);
            return;
        }
        ConfigureImage(
            maskImage,
            maskSprite,
            false,
            Image.Type.Simple);
        var mask = maskImage.GetComponent<Mask>();
        if (mask == null)
            mask = maskImage.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;
    }

    static void Place(
        RectTransform rect,
        Vector2 position,
        Vector2 size)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    static Transform DirectChild(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name) return child;
        }
        return null;
    }

    static Transform DeepFind(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = DeepFind(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
