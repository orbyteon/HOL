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
    const string AvatarResource = "solo/production/solo_player_avatar_v1";
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
    GameObject opponentBubbleRoot;
    GameObject tipRoot;
    NumberManager numberManager;
    GameManager gameManager;
    MenuManager menuManager;
    TMP_FontAsset displayFont;
    TMP_FontAsset bodyFont;
    TMP_InputField input;
    TMP_Text phaseText;
    TMP_Text roundText;
    TMP_Text rangeText;
    readonly GameObject[] historyRows = new GameObject[3];
    readonly Image[] historyRowImages = new Image[3];
    readonly TMP_Text[] historyNumberTexts = new TMP_Text[3];
    readonly TMP_Text[] historyOutcomeTexts = new TMP_Text[3];
    readonly Image[] historyIconImages = new Image[3];
    TMP_Text opponentSpeechText;
    TMP_Text opponentPromptText;
    TMP_Text opponentIdentityText;
    TMP_Text playerNameText;
    TMP_Text playerWinsText;
    TMP_Text opponentDifficultyText;
    TMP_Text chipText;
    Image playerAvatarImage;
    GameObject historyRoot;
    GameObject keypadRoot;
    Button submitControl;
    Button lockControl;
    Button saveStreakControl;
    bool built;
    int lastLayoutWidth = -1;
    int lastLayoutHeight = -1;
    Rect lastLayoutSafeArea = new Rect(-1f, -1f, -1f, -1f);

    public SoloBoardPresentationState CurrentState => presentation.Current;
    public Button SubmitControl => submitControl;
    public GameObject KeypadRoot => keypadRoot;
    public bool IsReady { get; private set; }
    public float CurrentTallBlend { get; private set; }

    public void RegisterLockControl(Button control)
    {
        lockControl = control;
        if (built)
            SeatLockControl();
    }

    public void RegisterSaveStreakControl(Button control)
    {
        saveStreakControl = control;
        if (built)
            SeatSaveStreakControl();
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

        LayoutNamed("SoloInteractionCard",
            new Vector2(-189f, -488f), new Vector2(-175f, -437f),
            new Vector2(760f, 1004f), new Vector2(730f, 1220f), blend);
        LayoutNamed("SoloOpponentRail",
            new Vector2(339f, -488f), new Vector2(321f, -437f),
            new Vector2(362f, 901f), new Vector2(340f, 1220f), blend);
        LayoutNamed("SoloOpponentBubble",
            new Vector2(-45f, 350f), new Vector2(-40f, 457f),
            new Vector2(315f, 234f), new Vector2(315f, 244f), blend);
        LayoutNamed("HistoryCard",
            new Vector2(0f, 7f), new Vector2(0f, 12f),
            new Vector2(374f, 462f), new Vector2(340f, 625f), blend);
        LayoutNamed("SoloTipCard",
            new Vector2(0f, -342f), new Vector2(0f, -413f),
            new Vector2(390f, 249f), new Vector2(342f, 363f), blend);

        LayoutNamed("CurrentNumberHeading",
            new Vector2(-9f, 402f), new Vector2(-9f, 506f),
            new Vector2(545f, 60f), new Vector2(545f, 60f), blend);
        LayoutControl(input != null ? input.transform as RectTransform : null,
            new Vector2(-13f, 274f), new Vector2(-13f, 377f),
            new Vector2(500f, 184f), new Vector2(500f, 208f), blend);
        LayoutControl(
            numberManager != null && numberManager.playerNumberText != null
                ? numberManager.playerNumberText.rectTransform
                : null,
            new Vector2(-13f, 274f), new Vector2(-13f, 377f),
            new Vector2(500f, 184f), new Vector2(500f, 208f), blend);
        LayoutControl(
            numberManager != null && numberManager.messageText != null
                ? numberManager.messageText.rectTransform
                : null,
            new Vector2(0f, 225f), new Vector2(0f, 236f),
            new Vector2(540f, 42f), new Vector2(540f, 50f), blend);

        LayoutControl(keypadRoot != null
                ? keypadRoot.transform as RectTransform
                : null,
            new Vector2(-1f, -74f), new Vector2(-1f, -69f),
            new Vector2(577f, 477f), new Vector2(577f, 628f), blend);
        LayoutKeypadButtons(blend);
        LayoutControl(submitControl != null
                ? submitControl.transform as RectTransform
                : null,
            new Vector2(-1f, -383f), new Vector2(-1f, -418f),
            new Vector2(575f, 94f), new Vector2(575f, 150f), blend);
        TMP_Text submitLabel = submitControl != null
            ? submitControl.GetComponentInChildren<TMP_Text>(true)
            : null;
        LayoutControl(submitLabel != null ? submitLabel.rectTransform : null,
            Vector2.zero, Vector2.zero,
            new Vector2(430f, 76f), new Vector2(430f, 110f), blend);

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
            float baseY = 176f - row * 120f;
            float tallY = 243f - row * 162f;
            LayoutControl(key as RectTransform,
                new Vector2(x, baseY), new Vector2(x, tallY),
                new Vector2(186f, 120f), new Vector2(186f, 142f), blend);
        }
    }

    void LayoutHistory(float blend)
    {
        LayoutNamed("HistoryTitle",
            new Vector2(0f, 196f), new Vector2(0f, 258f),
            new Vector2(310f, 50f), new Vector2(310f, 58f), blend);
        LayoutNamed("HistoryTitleSparkleLeft",
            new Vector2(-150f, 196f), new Vector2(-150f, 258f),
            new Vector2(34f, 40f), new Vector2(34f, 40f), blend);
        LayoutNamed("HistoryTitleSparkleRight",
            new Vector2(150f, 196f), new Vector2(150f, 258f),
            new Vector2(34f, 40f), new Vector2(34f, 40f), blend);

        float[] baseY = { 110f, -5f, -120f };
        float[] tallY = { 145f, -18f, -181f };
        for (int i = 0; i < historyRows.Length; i++)
        {
            LayoutNamed("HistoryRow" + (i + 1),
                new Vector2(0f, baseY[i]), new Vector2(0f, tallY[i]),
                new Vector2(334f, 104f), new Vector2(314f, 170f), blend);
        }
    }

    void LayoutTip(float blend)
    {
        LayoutNamed("SoloTipHeading",
            new Vector2(-90f, 91f), new Vector2(-90f, 112f),
            new Vector2(150f, 42f), new Vector2(150f, 46f), blend);
        LayoutNamed("SoloTipBulb",
            new Vector2(-150f, 88f), new Vector2(-150f, 109f),
            new Vector2(64f, 72f), new Vector2(64f, 72f), blend);
        LayoutNamed("SoloTipMascot",
            new Vector2(118f, -31f), new Vector2(85f, -66f),
            new Vector2(164f, 190f), new Vector2(145f, 176f), blend);
        LayoutControl(rangeText != null ? rangeText.rectTransform : null,
            new Vector2(-65f, 2f), new Vector2(-62f, -5f),
            new Vector2(195f, 88f), new Vector2(210f, 126f), blend);
    }

    void LayoutStateControls(float blend)
    {
        if (gameManager != null)
        {
            LayoutControl(gameManager.stopGameButton != null
                    ? gameManager.stopGameButton.transform as RectTransform
                    : null,
                new Vector2(0f, -835f), new Vector2(0f, -1035f),
                new Vector2(500f, 96f), new Vector2(500f, 118f), blend);
            LayoutAnswerControl(gameManager.higherButton, -190f, blend);
            LayoutAnswerControl(gameManager.correctButton, 0f, blend);
            LayoutAnswerControl(gameManager.lowerButton, 190f, blend);
        }
        LayoutControl(saveStreakControl != null
                ? saveStreakControl.transform as RectTransform
                : null,
            new Vector2(0f, -710f), new Vector2(0f, -900f),
            new Vector2(560f, 90f), new Vector2(560f, 112f), blend);
        LayoutControl(lockControl != null
                ? lockControl.transform as RectTransform
                : null,
            new Vector2(0f, -63f), new Vector2(0f, -78f),
            new Vector2(292f, 82f), new Vector2(292f, 96f), blend);
    }

    void LayoutAnswerControl(GameObject control, float x, float blend)
    {
        LayoutControl(control != null
                ? control.transform as RectTransform
                : null,
            new Vector2(x, -383f), new Vector2(x, -418f),
            new Vector2(170f, 94f), new Vector2(170f, 150f), blend);
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
        Sprite fallbackAvatar = LoadRequired(AvatarResource);
        Sprite avatar = ResolvePlayerAvatar(fallbackAvatar);
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

        playerAvatarImage = AddSprite(
            chipImage.transform, "SoloDuelChipAvatar", avatar,
            new Vector2(128f, 0f), new Vector2(104f, 104f));
        AddSprite(
            chipImage.transform, "SoloDuelChipTrophy", trophy,
            new Vector2(-102f, 0f), new Vector2(52f, 52f));
        chipText = DisplayLabel(
            chipImage.transform, "SoloDuelChipText", "", 36,
            new Vector2(-22f, 0f), new Vector2(172f, 72f), NearWhite);

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
        if (menuManager != null)
            back.onClick.AddListener(menuManager.RequestSoloMatchExit);

        GameObject playerCard = ArtFrame(
            safeRoot, "PlayerCard", new Vector2(-276f, 472f),
            new Vector2(514f, 620f), PlayerCardResource, false);
        AddSprite(
            playerCard.transform, "PlayerCharacter", LoadRequired(PlayerResource),
            new Vector2(-52f, 46f), new Vector2(370f, 370f));
        TMP_Text playerCaption = DisplayLabel(
            playerCard.transform, "PlayerCaption",
            L10n.Get("solo_you_header"), 38,
            new Vector2(0f, 248f), new Vector2(320f, 52f), NearWhite);
        RuntimeUI.Localize(playerCaption, "solo_you_header");
        playerNameText = DisplayLabel(
            playerCard.transform, "PlayerName",
            PlayerPrefs.GetString("PlayerName", L10n.Get("player_default")),
            60, new Vector2(0f, -159f), new Vector2(380f, 70f), NearWhite);
        AddSprite(
            playerCard.transform, "PlayerCardTrophy", LoadRequired(TrophyResource),
            new Vector2(-72f, -224f), new Vector2(54f, 54f));
        playerWinsText = DisplayLabel(
            playerCard.transform, "PlayerWins", string.Empty, 44,
            new Vector2(38f, -224f), new Vector2(170f, 54f), Gold);

        GameObject opponentCard = ArtFrame(
            safeRoot, "OpponentCard", new Vector2(282f, 470f),
            new Vector2(514f, 620f), OpponentCardResource, false);
        AddSprite(
            opponentCard.transform, "OpponentCharacter",
            LoadRequired(OpponentResource), new Vector2(-20f, 65f),
            new Vector2(360f, 360f));
        TMP_Text opponentCaption = DisplayLabel(
            opponentCard.transform, "OpponentCaption",
            L10n.Get("prebattle_opponent"), 37,
            new Vector2(0f, 248f), new Vector2(320f, 52f), NearWhite);
        RuntimeUI.Localize(opponentCaption, "prebattle_opponent");

        opponentIdentityText = gameManager != null
            ? gameManager.opponentNameText
            : null;
        if (opponentIdentityText == null)
        {
            opponentIdentityText = DisplayLabel(
                opponentCard.transform, "OpponentIdentity", "", 60,
                new Vector2(0f, -159f), new Vector2(380f, 70f), NearWhite);
        }
        else
        {
            Reparent(opponentIdentityText.transform, opponentCard.transform);
            Place(
                opponentIdentityText.rectTransform,
                new Vector2(0f, -159f), new Vector2(380f, 70f));
            ConfigureDisplayText(opponentIdentityText, 48f, 61f);
            opponentIdentityText.enableAutoSizing = false;
            opponentIdentityText.fontSize = 60f;
            opponentIdentityText.color = NearWhite;
            opponentIdentityText.alignment = TextAlignmentOptions.Center;
        }

        AddSprite(
            opponentCard.transform, "OpponentCardTrophy", LoadRequired(TrophyResource),
            new Vector2(-72f, -224f), new Vector2(54f, 54f));
        opponentDifficultyText = DisplayLabel(
            opponentCard.transform, "OpponentDifficulty", string.Empty, 32,
            new Vector2(48f, -224f), new Vector2(250f, 54f), Gold);
        opponentDifficultyText.enableAutoSizing = true;
        opponentDifficultyText.fontSizeMin = 24f;
        opponentDifficultyText.fontSizeMax = 32f;
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

        GameObject promptRibbon = ArtFrame(
            safeRoot, "SoloPromptRibbon", new Vector2(-18f, 86f),
            new Vector2(636f, 181f), PromptRibbonResource, false);
        roundText = DisplayLabel(
            promptRibbon.transform, "RoundLabel", "", 34,
            new Vector2(0f, 45f), new Vector2(520f, 42f), NearWhite);

        phaseText = gameManager != null ? gameManager.turnText : null;
        if (phaseText == null)
        {
            phaseText = DisplayLabel(
                promptRibbon.transform, "PhasePrompt", "", 56,
                new Vector2(0f, -15f), new Vector2(584f, 70f), NearWhite);
        }
        else
        {
            Reparent(phaseText.transform, promptRibbon.transform);
            Place(
                phaseText.rectTransform, new Vector2(0f, -15f),
                new Vector2(584f, 70f));
            phaseText.color = NearWhite;
            phaseText.alignment = TextAlignmentOptions.Center;
        }
        ConfigureDisplayText(phaseText, 30f, 45f);
        phaseText.enableAutoSizing = true;
        phaseText.fontSizeMin = 30f;
        phaseText.fontSizeMax = 45f;
        phaseText.enableWordWrapping = false;
        phaseText.overflowMode = TextOverflowModes.Overflow;
        phaseText.margin = new Vector4(8f, 0f, 8f, 0f);
        roundText.outlineWidth = 0.20f;
        phaseText.outlineWidth = 0.22f;

        interactionCard = ArtFrame(
            safeRoot, "SoloInteractionCard", new Vector2(-189f, -488f),
            new Vector2(760f, 1004f), InteractionBoardResource, false);
        TMP_Text currentLabel = DisplayLabel(
            interactionCard.transform, "CurrentNumberHeading",
            L10n.Get("hud_current_number"), 30,
            new Vector2(-9f, 402f), new Vector2(545f, 60f), NearWhite);
        RuntimeUI.Localize(currentLabel, "hud_current_number");

        GameObject rail = RuntimeUI.CreateObject("SoloOpponentRail", safeRoot);
        Place(
            rail.transform as RectTransform, new Vector2(339f, -488f),
            new Vector2(362f, 901f));

        opponentBubbleRoot = MirroredArtFrame(
            rail.transform, "SoloOpponentBubble", new Vector2(-45f, 350f),
            new Vector2(315f, 234f), SpeechBubbleResource, false);
        AddSprite(
            opponentBubbleRoot.transform, "OpponentBubbleAvatar",
            LoadRequired(OpponentAvatarResource), new Vector2(160f, -20f),
            new Vector2(130f, 130f));
        opponentPromptText = DisplayLabel(
            opponentBubbleRoot.transform, "OpponentBubblePrompt", "", 32,
            new Vector2(-47f, 8f), new Vector2(224f, 126f), Ink);
        AddSprite(
            opponentBubbleRoot.transform, "OpponentReaction",
            LoadRequired(ReactionEmojiResource), new Vector2(-45f, -72f),
            new Vector2(106f, 106f));

        tipRoot = ArtFrame(
            rail.transform, "SoloTipCard", new Vector2(0f, -342f),
            new Vector2(390f, 249f), TipBoardResource);
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
                input.transform as RectTransform, new Vector2(-13f, 274f),
                new Vector2(500f, 184f));
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
                placeholder.text = "?";
                placeholder.font = displayFont;
                placeholder.fontSize = 170f;
                placeholder.fontStyle = FontStyles.Bold;
                placeholder.enableAutoSizing = false;
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
                new Vector2(-13f, 274f), new Vector2(500f, 184f));
            numberManager.playerNumberText.font = displayFont;
            numberManager.playerNumberText.alignment =
                TextAlignmentOptions.Center;
            numberManager.playerNumberText.fontSize = 110f;
            numberManager.playerNumberText.fontStyle = FontStyles.Bold;
            numberManager.playerNumberText.color = NearWhite;
        }

        rangeText = gameManager != null ? gameManager.rangeText : null;
        if (rangeText == null)
        {
            rangeText = BodyLabel(
                tipRoot.transform, "RangeLabel", "", 26,
                new Vector2(-65f, 2f), new Vector2(195f, 88f), NearWhite);
        }
        else
        {
            Reparent(rangeText.transform, tipRoot.transform);
            Place(
                rangeText.rectTransform, new Vector2(-65f, 2f),
                new Vector2(195f, 88f));
            ConfigureBodyText(rangeText, 22f, 28f);
            rangeText.color = NearWhite;
            rangeText.alignment = TextAlignmentOptions.Center;
        }

        if (gameManager != null)
        {
            gameManager.rangeText = rangeText;

            if (gameManager.aiNumberText != null)
            {
                Reparent(
                    gameManager.aiNumberText.transform,
                    opponentBubbleRoot.transform);
                Place(
                    gameManager.aiNumberText.rectTransform,
                    new Vector2(-35f, 55f), new Vector2(270f, 48f));
                ConfigureDisplayText(gameManager.aiNumberText, 23f, 30f);
                gameManager.aiNumberText.color = Ink;
                gameManager.aiNumberText.alignment =
                    TextAlignmentOptions.Center;
            }

            if (gameManager.aiAnswerText != null)
            {
                opponentSpeechText = gameManager.aiAnswerText;
                Reparent(
                    opponentSpeechText.transform,
                    opponentBubbleRoot.transform);
                Place(
                    opponentSpeechText.rectTransform,
                    new Vector2(-35f, -24f), new Vector2(220f, 108f));
                opponentSpeechText.font = displayFont;
                ConfigureDisplayText(opponentSpeechText, 30f, 37f);
                opponentSpeechText.enableAutoSizing = false;
                opponentSpeechText.fontSize = 34f;
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
            rail, "HistoryCard", new Vector2(0f, 7f),
            new Vector2(374f, 462f), HistoryBoardResource, false);
        TMP_Text title = DisplayLabel(
            historyRoot.transform, "HistoryTitle", L10n.Get("hud_history"), 30,
            new Vector2(0f, 196f), new Vector2(310f, 50f), NearWhite);
        RuntimeUI.Localize(title, "hud_history");
        AddSprite(
            historyRoot.transform, "HistoryTitleSparkleLeft",
            LoadRequired(TitleSparkleResource), new Vector2(-150f, 196f),
            new Vector2(34f, 40f));
        AddSprite(
            historyRoot.transform, "HistoryTitleSparkleRight",
            LoadRequired(TitleSparkleResource), new Vector2(150f, 196f),
            new Vector2(34f, 40f));

        BuildHistoryRow(0, 110f, HistoryHigherResource, HistoryUpIconResource);
        BuildHistoryRow(1, -5f, HistoryLowerResource, HistoryDownIconResource);
        BuildHistoryRow(
            2, -120f, HistoryCorrectResource, HistoryCorrectIconResource);
    }

    void BuildHistoryRow(
        int index, float y, string resource, string iconResource)
    {
        GameObject row = ArtFrame(
            historyRoot.transform, "HistoryRow" + (index + 1),
            new Vector2(0f, y), new Vector2(334f, 104f), resource, false);
        historyRows[index] = row;
        historyRowImages[index] = row.GetComponent<Image>();
        historyNumberTexts[index] = DisplayLabel(
            row.transform, "HistoryNumber", "", 64,
            new Vector2(-106f, 0f), new Vector2(82f, 68f), NearWhite);
        historyOutcomeTexts[index] = DisplayLabel(
            row.transform, "HistoryOutcome", "", 30,
            new Vector2(14f, 0f), new Vector2(150f, 62f), NearWhite);
        historyOutcomeTexts[index].enableAutoSizing = true;
        historyOutcomeTexts[index].fontSizeMin = 23f;
        historyOutcomeTexts[index].fontSizeMax = 30f;
        historyOutcomeTexts[index].margin = new Vector4(6f, 0f, 6f, 0f);
        historyIconImages[index] = AddSprite(
            row.transform, "HistoryIcon", LoadRequired(iconResource),
            new Vector2(125f, 0f), new Vector2(64f, 64f));
    }

    void BuildKeypad()
    {
        keypadRoot = RuntimeUI.CreateObject(
            "NumberKeypad", interactionCard.transform);
        RectTransform rootRect = (RectTransform)keypadRoot.transform;
        Place(rootRect, new Vector2(-1f, -74f), new Vector2(577f, 477f));

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
                new Vector2(-203f + column * 203f, 176f - row * 120f),
                new Vector2(186f, 120f), KeyBlue, NearWhite);
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
            new Vector2(-1f, -383f), new Vector2(575f, 94f));
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
            submitLabel.enableAutoSizing = false;
            submitLabel.characterSpacing = 0f;
            submitLabel.wordSpacing = 0f;
            submitLabel.lineSpacing = 0f;
            submitLabel.overflowMode = TextOverflowModes.Overflow;
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
        ValidateLayout();
    }

    void SeatControllerOwnedControls()
    {
        if (numberManager != null && numberManager.messageText != null)
        {
            TMP_Text message = numberManager.messageText;
            Reparent(message.transform, interactionCard.transform);
            Place(message.rectTransform, new Vector2(0f, 225f),
                new Vector2(540f, 42f));
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
            gameManager.stopGameButton, safeRoot,
            new Vector2(0f, -835f), new Vector2(500f, 96f),
            SoloGoldFrameResource, Ink);

        SeatAnswerControl(
            gameManager.higherButton, new Vector2(-190f, -383f),
            SoloBlueFrameResource, NearWhite);
        SeatAnswerControl(
            gameManager.correctButton, new Vector2(0f, -383f),
            SoloGoldFrameResource, Ink);
        SeatAnswerControl(
            gameManager.lowerButton, new Vector2(190f, -383f),
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
        if (lockControl == null || tipRoot == null)
            return;

        SeatButton(
            lockControl.gameObject, tipRoot.transform,
            new Vector2(0f, -63f), new Vector2(292f, 82f),
            SoloBlueFrameResource, Ink);
    }

    void SeatSaveStreakControl()
    {
        if (saveStreakControl == null || safeRoot == null)
            return;

        SeatButton(
            saveStreakControl.gameObject, safeRoot,
            new Vector2(0f, -710f), new Vector2(560f, 90f),
            SoloPurpleFrameResource, NearWhite);
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

        SetInactive(numberManager.playerGuessesPanel);
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
            playerAvatarImage.sprite = ResolvePlayerAvatar(
                LoadRequired(AvatarResource));

        if (phaseText != null)
            phaseText.text = PromptText(state);

        if (roundText != null)
        {
            bool showRound = state.RoundNumber > 0;
            roundText.gameObject.SetActive(showRound);
            roundText.text = showRound
                ? L10n.Get("round_label_open", state.RoundNumber)
                : string.Empty;
        }

        if (rangeText != null)
        {
            bool showRange =
                state.Phase != SoloBoardPhase.ChooseSecret &&
                state.Phase != SoloBoardPhase.MatchResult;
            rangeText.gameObject.SetActive(showRange);
            rangeText.text = showRange
                ? L10n.Get("solo_tip_range", state.RangeMin, state.RangeMax)
                : string.Empty;
        }

        RenderHistoryRows(state);
        if (historyRoot != null)
            historyRoot.SetActive(state.Phase != SoloBoardPhase.ChooseSecret);

        if (opponentIdentityText != null)
        {
            opponentIdentityText.text = state.OpponentName;
            opponentIdentityText.transform.SetAsLastSibling();
        }

        bool numeric = state.NumericControlsAvailable;
        if (numberManager != null && numberManager.playerNumberText != null)
        {
            numberManager.playerNumberText.gameObject.SetActive(
                !numeric && state.Phase != SoloBoardPhase.MatchResult);
        }

        if (opponentBubbleRoot != null)
        {
            bool showBubble =
                state.Phase == SoloBoardPhase.PlayerGuess ||
                state.Phase == SoloBoardPhase.OpponentThinking ||
                state.Phase == SoloBoardPhase.AnswerOpponent;
            opponentBubbleRoot.SetActive(showBubble);
        }
        if (opponentSpeechText != null &&
            state.Phase == SoloBoardPhase.PlayerGuess)
        {
            opponentSpeechText.gameObject.SetActive(true);
            opponentSpeechText.text = string.Empty;
        }
        if (opponentPromptText != null)
        {
            bool showPrompt = state.Phase == SoloBoardPhase.PlayerGuess;
            opponentPromptText.gameObject.SetActive(showPrompt);
            if (showPrompt)
            {
                opponentPromptText.text = L10n.Get("solo_ai_taunt");
                opponentPromptText.transform.SetAsLastSibling();
            }
        }
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
        if (submitControl != null)
        {
            submitControl.interactable = numeric;
            submitControl.gameObject.SetActive(state.SubmitControlVisible);
        }

        EnforcePhaseVisibility(state);
        SuppressRetiredLegacyPanels();
        EnsureVisualRootOnTop();
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
        int historyCount = state.PlayerGuessHistory.Count;
        int first = Mathf.Max(0, historyCount - historyRows.Length);
        int visible = historyCount - first;
        for (int i = 0; i < historyRows.Length; i++)
        {
            bool active = i < visible;
            if (historyRows[i] != null)
                historyRows[i].SetActive(active);
            if (!active) continue;

            int sourceIndex = first + i;
            SoloGuessOutcome outcome = sourceIndex <
                state.PlayerGuessOutcomeHistory.Count
                ? state.PlayerGuessOutcomeHistory[sourceIndex]
                : SoloGuessOutcome.Unknown;
            historyNumberTexts[i].text =
                state.PlayerGuessHistory[sourceIndex].ToString();
            historyOutcomeTexts[i].text = HistoryOutcomeLabel(outcome);
            Color color = HistoryOutcomeColor(outcome);
            historyNumberTexts[i].color = color;
            historyOutcomeTexts[i].color = NearWhite;
            ConfigureHistoryIcon(historyIconImages[i], outcome);
            ConfigureImage(
                historyRowImages[i], HistoryOutcomeSprite(outcome),
                false, Image.Type.Simple);
        }
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

    static Sprite ResolvePlayerAvatar(Sprite fallback)
    {
        if (!OnboardingProfile.TryLoadCommittedAvatar(out int avatarIndex))
            return fallback;

        OnboardingAvatarCatalog.Entry entry =
            OnboardingAvatarCatalog.Get(avatarIndex);
        if (string.IsNullOrWhiteSpace(entry.ResourcePath))
            return fallback;

        Sprite selected = Resources.Load<Sprite>(entry.ResourcePath);
        return selected != null ? selected : fallback;
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
        switch (state.Prompt)
        {
            case SoloBoardPrompt.EnterSecret:
                return L10n.Get("solo_choose_secret");
            case SoloBoardPrompt.YourGuess:
                return L10n.Get("solo_guess_number");
            case SoloBoardPrompt.OpponentThinking:
                return L10n.Get("opponent_thinking", state.OpponentName);
            case SoloBoardPrompt.AnswerOpponent:
                return L10n.Get("answer_opponent", state.OpponentName);
            case SoloBoardPrompt.OpponentGuessedHigher:
                return L10n.Get("your_number_is_higher");
            case SoloBoardPrompt.OpponentGuessedLower:
                return L10n.Get("your_number_is_lower");
            case SoloBoardPrompt.OpponentGuessedCorrect:
                return L10n.Get("your_number_is_correct");
            case SoloBoardPrompt.OpponentForfeits:
                return L10n.Get("opponent_forfeits", state.OpponentName);
            case SoloBoardPrompt.MatchPoint:
                return L10n.Get("match_point");
            case SoloBoardPrompt.MatchPointYours:
                return L10n.Get("match_point_yours", state.OpponentName);
            case SoloBoardPrompt.TurnForfeited:
                return L10n.Get("turn_forfeited");
            case SoloBoardPrompt.ResolvingRound:
                return string.Empty;
            case SoloBoardPrompt.Win:
            {
                string result = L10n.Get("you_win") + "\n" +
                                L10n.Get("won_in_guesses", state.DetailValue);
                if (state.DetailValue <= 7)
                    result += "\n" + L10n.Get("perfect_game");
                return result;
            }
            case SoloBoardPrompt.Loss:
                return L10n.Get("you_lose") + "\n" +
                       L10n.Get("number_was", state.DetailValue);
            case SoloBoardPrompt.Draw:
                return L10n.Get("you_draw") + "\n" +
                       L10n.Get("draw_in_guesses", state.DetailValue) +
                       "\n" + L10n.Get("draw_tip");
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

    void OnKeyPressed(string key)
    {
        if (input == null || !input.interactable) return;
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
