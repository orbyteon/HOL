using System.Collections.Generic;
using System.Text;
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
public sealed class HolDuelBoardLayout : MonoBehaviour
{
    public const string VisualRootName = "SoloDuelVisualRoot";
    public const string SafeRootName = "SoloDuelSafeRoot";

    const string BackspaceCommand = "BACKSPACE";

    const string BackgroundResource = "phase2a/hol_neon_reference_bg_r3";
    const string LogoResource = "reference/hol_logo_exact";
    const string PlayerResource = "reference/player_cyan_exact";
    const string OpponentResource = "reference/opponent_purple_exact";
    const string AvatarResource = "reference/player_cyan_exact";
    const string VsResource = "reference/board_vs_burst_exact";
    const string TrophyResource = "reference/board_trophy_exact";
    const string MascotSevenResource = "reference/mascot_7_exact";
    const string MascotThreeResource = "reference/mascot_3_exact";
    const string SpeechBubbleResource = "cartoon/cartoon_speech_bubble";
    const string StarsResource = "mainmenu/mainmenu_deco_stars";
    const string ConfettiResource = "mainmenu/mainmenu_deco_confetti";
    const string BackChevronResource = "phase2a/hol_chevron_r2";
    const string ChipFrameResource = "phase2a/hol_player_chip_r2_9s";

    const string SoloPurpleFrameResource = "mainmenu/mainmenu_tip_frame_9s";
    const string SoloBlueFrameResource = "mainmenu/mainmenu_cta_blue_9s";
    const string SoloMagentaFrameResource = "phase2a/hol_cta_magenta_r2_9s";
    const string SoloGoldFrameResource = "mainmenu/mainmenu_cta_gold_9s";
    const string DisplayFontResource = "phase2a/fonts/HOL Menu Display SDF";
    const string BodyFontResource = "phase2a/fonts/HOL Menu Body SDF";

    const float ReferenceWidth = 1080f;
    const float ReferenceHeight = 1920f;

    static readonly Color CardBlue = new Color(0.08f, 0.28f, 0.68f, 0.96f);
    static readonly Color CardPink = new Color(0.72f, 0.08f, 0.34f, 0.96f);
    static readonly Color KeyBlue = new Color(0.16f, 0.18f, 0.62f, 1f);
    static readonly Color Gold = new Color(1f, 0.76f, 0.10f, 1f);
    static readonly Color NearWhite = new Color(0.985f, 0.975f, 1f, 1f);
    static readonly Color Muted = new Color(0.75f, 0.78f, 0.92f, 1f);
    static readonly Color Ink = new Color(0.09f, 0.05f, 0.16f, 1f);
    static readonly Color Cyan = new Color(0.20f, 0.94f, 1f, 1f);

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
    TMP_Text playerHistoryText;
    TMP_Text aiHistoryText;
    TMP_Text opponentIdentityText;
    TMP_Text playerNameText;
    TMP_Text chipText;
    GameObject historyRoot;
    GameObject keypadRoot;
    Button submitControl;
    bool built;

    public SoloBoardPresentationState CurrentState => presentation.Current;
    public Button SubmitControl => submitControl;
    public GameObject KeypadRoot => keypadRoot;
    public bool IsReady { get; private set; }

    void OnEnable()
    {
        L10n.OnLanguageChanged += Render;
        Render();
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= Render;
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

    public void RecordPlayerGuess(int guess)
    {
        presentation.RecordPlayerGuess(guess);
        Render();
    }

    public void RecordAiGuess(int guess)
    {
        presentation.RecordAiGuess(guess);
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
        built = true;
        Render();
    }

    void BuildVisualShell()
    {
        displayFont = Resources.Load<TMP_FontAsset>(DisplayFontResource);
        bodyFont = Resources.Load<TMP_FontAsset>(BodyFontResource);

        Sprite background = LoadRequired(BackgroundResource);
        Sprite logo = LoadRequired(LogoResource);
        Sprite player = LoadRequired(PlayerResource);
        Sprite opponent = LoadRequired(OpponentResource);
        Sprite avatar = LoadRequired(AvatarResource);
        Sprite vs = LoadRequired(VsResource);
        Sprite trophy = LoadRequired(TrophyResource);
        Sprite seven = LoadRequired(MascotSevenResource);
        Sprite three = LoadRequired(MascotThreeResource);
        Sprite bubble = LoadRequired(SpeechBubbleResource);
        Sprite stars = LoadRequired(StarsResource);
        Sprite confetti = LoadRequired(ConfettiResource);
        Sprite chevron = LoadRequired(BackChevronResource);
        Sprite chip = LoadRequired(ChipFrameResource);
        Sprite purple = LoadRequired(SoloPurpleFrameResource);
        Sprite blue = LoadRequired(SoloBlueFrameResource);
        Sprite magenta = LoadRequired(SoloMagentaFrameResource);
        Sprite gold = LoadRequired(SoloGoldFrameResource);

        IsReady = ArtReady(
            background, logo, player, opponent, avatar, vs, trophy, seven,
            three, bubble, stars, confetti, chevron, chip, purple, blue,
            magenta, gold) && displayFont != null && bodyFont != null;

        if (!IsReady)
        {
            Debug.LogError(
                "[HolDuelBoardLayout] Required production artwork/fonts are missing.");
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
        visualRoot.SetAsFirstSibling();

        var backgroundImage = EnsureImage(visualRoot, "SoloDuelBackground");
        Stretch(backgroundImage.rectTransform);
        ConfigureImage(backgroundImage, background, false, Image.Type.Simple);
        backgroundImage.raycastTarget = true;

        var starsImage = EnsureImage(visualRoot, "SoloDuelStars");
        Stretch(starsImage.rectTransform);
        ConfigureImage(starsImage, stars, false, Image.Type.Simple);

        var confettiImage = EnsureImage(visualRoot, "SoloDuelConfetti");
        Stretch(confettiImage.rectTransform);
        ConfigureImage(confettiImage, confetti, false, Image.Type.Simple);

        var outer = EnsureImage(visualRoot, "SoloDuelOuterFrame");
        ConfigureImage(outer, purple, false, Image.Type.Sliced);
        outer.pixelsPerUnitMultiplier = 2f;
        Place(outer.rectTransform, Vector2.zero, new Vector2(1032f, 1872f));

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
            new Vector2(0f, 822f), new Vector2(330f, 170f));
        logoImage.raycastTarget = false;

        var chipImage = EnsureImage(safeRoot, "SoloDuelPlayerChip");
        ConfigureImage(chipImage, chip, false, Image.Type.Sliced);
        chipImage.pixelsPerUnitMultiplier = 2f;
        CenterRoot(
            chipImage.rectTransform, new Vector2(310f, 100f),
            new Vector2(350f, 842f));

        AddSprite(
            chipImage.transform, "SoloDuelChipAvatar", avatar,
            new Vector2(-105f, 0f), new Vector2(70f, 70f));
        AddSprite(
            chipImage.transform, "SoloDuelChipTrophy", trophy,
            new Vector2(-18f, -22f), new Vector2(38f, 38f));
        chipText = BodyLabel(
            chipImage.transform, "SoloDuelChipText", "", 25,
            new Vector2(52f, 0f), new Vector2(170f, 72f), NearWhite);

        AddSprite(
            safeRoot, "SoloDuelMascotSeven", seven,
            new Vector2(-455f, 366f), new Vector2(155f, 175f));
        AddSprite(
            safeRoot, "SoloDuelMascotThree", three,
            new Vector2(455f, 366f), new Vector2(155f, 175f));
    }

    void BuildHeader()
    {
        var back = RuntimeUI.CreateButton(
            safeRoot, "DuelBack", string.Empty,
            new Vector2(-484f, 842f), new Vector2(90f, 90f),
            Color.white, NearWhite);
        StyleSoloButton(back, SoloPurpleFrameResource, NearWhite);
        CenterRoot(
            (RectTransform)back.transform, new Vector2(90f, 90f),
            new Vector2(-484f, 842f));
        HideButtonLabels(back.transform);
        AddSprite(
            back.transform, "DuelBackIcon", LoadRequired(BackChevronResource),
            Vector2.zero, new Vector2(46f, 58f))
            .rectTransform.localScale = new Vector3(-1f, 1f, 1f);
        if (menuManager != null)
            back.onClick.AddListener(menuManager.RequestSoloMatchExit);

        GameObject playerCard = Frame(
            safeRoot, "PlayerCard", new Vector2(-270f, 605f),
            new Vector2(470f, 340f), SoloBlueFrameResource);
        AddSprite(
            playerCard.transform, "PlayerCharacter", LoadRequired(PlayerResource),
            new Vector2(0f, 25f), new Vector2(310f, 260f));
        TMP_Text playerCaption = DisplayLabel(
            playerCard.transform, "PlayerCaption", L10n.Get("you"), 27,
            new Vector2(0f, 132f), new Vector2(390f, 44f), NearWhite);
        RuntimeUI.Localize(playerCaption, "you");
        playerNameText = DisplayLabel(
            playerCard.transform, "PlayerName",
            PlayerPrefs.GetString("PlayerName", L10n.Get("player_default")),
            38, new Vector2(0f, -125f), new Vector2(410f, 58f), NearWhite);

        GameObject opponentCard = Frame(
            safeRoot, "OpponentCard", new Vector2(270f, 605f),
            new Vector2(470f, 340f), SoloMagentaFrameResource);
        AddSprite(
            opponentCard.transform, "OpponentCharacter",
            LoadRequired(OpponentResource), new Vector2(0f, 25f),
            new Vector2(310f, 260f));
        TMP_Text opponentCaption = DisplayLabel(
            opponentCard.transform, "OpponentCaption",
            L10n.Get("prebattle_opponent"), 25,
            new Vector2(0f, 132f), new Vector2(400f, 44f), NearWhite);
        RuntimeUI.Localize(opponentCaption, "prebattle_opponent");

        opponentIdentityText = gameManager != null
            ? gameManager.opponentNameText
            : null;
        if (opponentIdentityText == null)
        {
            opponentIdentityText = DisplayLabel(
                opponentCard.transform, "OpponentIdentity", "", 34,
                new Vector2(0f, -125f), new Vector2(410f, 58f), NearWhite);
        }
        else
        {
            Reparent(opponentIdentityText.transform, opponentCard.transform);
            Place(
                opponentIdentityText.rectTransform,
                new Vector2(0f, -125f), new Vector2(410f, 58f));
            ConfigureDisplayText(opponentIdentityText, 26f, 35f);
            opponentIdentityText.color = NearWhite;
            opponentIdentityText.alignment = TextAlignmentOptions.Center;
        }

        AddSprite(
            safeRoot, "SoloVsBurst", LoadRequired(VsResource),
            new Vector2(0f, 600f), new Vector2(190f, 190f));

        GameObject promptRibbon = Frame(
            safeRoot, "SoloPromptRibbon", new Vector2(0f, 365f),
            new Vector2(900f, 150f), SoloPurpleFrameResource);
        roundText = DisplayLabel(
            promptRibbon.transform, "RoundLabel", "", 30,
            new Vector2(0f, 42f), new Vector2(760f, 42f), NearWhite);

        phaseText = gameManager != null ? gameManager.turnText : null;
        if (phaseText == null)
        {
            phaseText = DisplayLabel(
                promptRibbon.transform, "PhasePrompt", "", 38,
                new Vector2(0f, -26f), new Vector2(820f, 78f), NearWhite);
        }
        else
        {
            Reparent(phaseText.transform, promptRibbon.transform);
            Place(
                phaseText.rectTransform, new Vector2(0f, -26f),
                new Vector2(820f, 78f));
            ConfigureDisplayText(phaseText, 29f, 40f);
            phaseText.color = NearWhite;
            phaseText.alignment = TextAlignmentOptions.Center;
        }

        interactionCard = Frame(
            safeRoot, "SoloInteractionCard", new Vector2(-225f, -260f),
            new Vector2(610f, 950f), SoloPurpleFrameResource);
        TMP_Text currentLabel = DisplayLabel(
            interactionCard.transform, "CurrentNumberHeading",
            L10n.Get("hud_current_number"), 30,
            new Vector2(0f, 410f), new Vector2(520f, 48f), NearWhite);
        RuntimeUI.Localize(currentLabel, "hud_current_number");

        GameObject rail = Frame(
            safeRoot, "SoloOpponentRail", new Vector2(330f, -260f),
            new Vector2(350f, 950f), SoloPurpleFrameResource);

        opponentBubbleRoot = Frame(
            rail.transform, "SoloOpponentBubble", new Vector2(0f, 320f),
            new Vector2(310f, 230f), SpeechBubbleResource);
        AddSprite(
            opponentBubbleRoot.transform, "OpponentBubbleAvatar",
            LoadRequired(OpponentResource), new Vector2(105f, -65f),
            new Vector2(85f, 85f));

        tipRoot = Frame(
            rail.transform, "SoloTipCard", new Vector2(0f, -330f),
            new Vector2(310f, 220f), SoloPurpleFrameResource);
        TMP_Text tipHeading = DisplayLabel(
            tipRoot.transform, "SoloTipHeading", L10n.Get("hud_tip"), 27,
            new Vector2(0f, 76f), new Vector2(260f, 42f), Gold);
        RuntimeUI.Localize(tipHeading, "hud_tip");
    }

    void LayoutExistingGameplay()
    {
        if (input != null)
        {
            Reparent(input.transform, interactionCard.transform);
            Place(
                input.transform as RectTransform, new Vector2(0f, 315f),
                new Vector2(500f, 125f));
            input.shouldHideMobileInput = true;
            input.shouldHideSoftKeyboard = true;
            var image = input.GetComponent<Image>();
            if (image == null)
                image = input.gameObject.AddComponent<Image>();
            RuntimeUI.ApplyProductionSprite(
                image, SoloPurpleFrameResource, Image.Type.Sliced,
                false, 2f);
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
                placeholder.font = bodyFont;
                placeholder.fontSize = 32f;
                placeholder.color = Muted;
                placeholder.alignment = TextAlignmentOptions.Center;
            }
        }

        if (numberManager != null && numberManager.playerNumberText != null)
        {
            Reparent(
                numberManager.playerNumberText.transform,
                interactionCard.transform);
            Place(
                numberManager.playerNumberText.rectTransform,
                new Vector2(0f, 390f), new Vector2(500f, 45f));
            numberManager.playerNumberText.font = bodyFont;
            numberManager.playerNumberText.alignment =
                TextAlignmentOptions.Center;
            numberManager.playerNumberText.fontSize = 27f;
            numberManager.playerNumberText.color = Muted;
        }

        rangeText = gameManager != null ? gameManager.rangeText : null;
        if (rangeText == null)
        {
            rangeText = BodyLabel(
                tipRoot.transform, "RangeLabel", "", 26,
                new Vector2(0f, -20f), new Vector2(270f, 125f), Cyan);
        }
        else
        {
            Reparent(rangeText.transform, tipRoot.transform);
            Place(
                rangeText.rectTransform, new Vector2(0f, -20f),
                new Vector2(270f, 125f));
            ConfigureBodyText(rangeText, 22f, 28f);
            rangeText.color = Cyan;
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
                    new Vector2(-30f, 55f), new Vector2(230f, 48f));
                ConfigureDisplayText(gameManager.aiNumberText, 23f, 30f);
                gameManager.aiNumberText.color = Ink;
                gameManager.aiNumberText.alignment =
                    TextAlignmentOptions.Center;
            }

            if (gameManager.aiAnswerText != null)
            {
                Reparent(
                    gameManager.aiAnswerText.transform,
                    opponentBubbleRoot.transform);
                Place(
                    gameManager.aiAnswerText.rectTransform,
                    new Vector2(-30f, -24f), new Vector2(230f, 100f));
                ConfigureBodyText(gameManager.aiAnswerText, 20f, 27f);
                gameManager.aiAnswerText.color = Ink;
                gameManager.aiAnswerText.alignment =
                    TextAlignmentOptions.Center;
            }
        }

        RectTransform answerRoot = FindChild("AIBUTTONSPANEL");
        if (answerRoot != null)
            RuntimeUI.Stretch(answerRoot.gameObject);
        MoveIfFound(
            "ButtonHIGHER", new Vector2(-300f, -835f),
            new Vector2(270f, 96f));
        MoveIfFound(
            "ButtonCORRECT", new Vector2(0f, -835f),
            new Vector2(270f, 96f));
        MoveIfFound(
            "ButtonLOWER", new Vector2(300f, -835f),
            new Vector2(270f, 96f));
    }

    void BuildHistoryCard()
    {
        Transform rail = DeepFind(safeRoot, "SoloOpponentRail");
        historyRoot = Frame(
            rail, "HistoryCard", new Vector2(0f, 35f),
            new Vector2(310f, 390f), SoloPurpleFrameResource);
        TMP_Text title = DisplayLabel(
            historyRoot.transform, "HistoryTitle", L10n.Get("hud_history"), 27,
            new Vector2(0f, 150f), new Vector2(270f, 46f), NearWhite);
        RuntimeUI.Localize(title, "hud_history");
        playerHistoryText = BodyLabel(
            historyRoot.transform, "PlayerGuessHistory", "", 22,
            new Vector2(0f, 55f), new Vector2(270f, 120f), NearWhite);
        aiHistoryText = BodyLabel(
            historyRoot.transform, "AiGuessHistory", "", 22,
            new Vector2(0f, -92f), new Vector2(270f, 120f), NearWhite);

        if (gameManager != null)
        {
            gameManager.playerHistoryText = playerHistoryText;
            gameManager.aiHistoryText = aiHistoryText;
        }
    }

    void BuildKeypad()
    {
        keypadRoot = RuntimeUI.CreateObject(
            "NumberKeypad", interactionCard.transform);
        RectTransform rootRect = (RectTransform)keypadRoot.transform;
        Place(rootRect, new Vector2(0f, -30f), new Vector2(560f, 560f));

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
            string label = keys[i] == BackspaceCommand ? "←" : keys[i];
            Button button = RuntimeUI.CreateButton(
                keypadRoot.transform, "Key_" + keys[i], label,
                new Vector2(-182f + column * 182f, 205f - row * 132f),
                new Vector2(160f, 108f), KeyBlue, NearWhite);
            StyleSoloButton(button, SoloBlueFrameResource, NearWhite);
            TMP_Text text = button.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.font = displayFont;
                text.fontSize = keys[i] == BackspaceCommand || keys[i] == "×"
                    ? 38f
                    : 48f;
                text.fontStyle = FontStyles.Bold;
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
                L10n.Get("confirm"), new Vector2(0f, -408f),
                new Vector2(530f, 104f), Gold, Ink);
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
            new Vector2(0f, -408f), new Vector2(530f, 104f));
        StyleSoloButton(submitControl, SoloGoldFrameResource, Ink);
        TMP_Text submitLabel =
            submitControl.GetComponentInChildren<TMP_Text>(true);
        if (submitLabel != null)
        {
            submitLabel.font = displayFont;
            submitLabel.fontSize = 42f;
            submitLabel.fontStyle = FontStyles.Bold;
            if (submitLabel.GetComponent<LocalizedText>() == null)
                RuntimeUI.Localize(submitControl, "confirm");
        }
        ValidateLayout();
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
        {
            string player = PlayerPrefs.GetString("PlayerName", "");
            if (string.IsNullOrWhiteSpace(player))
                player = L10n.Get("player_default");
            chipText.text = "<b>" + player + "</b>\n<size=78%>" +
                            L10n.Get("stats_wins") + ": " +
                            GameStats.Wins + "</size>";
        }

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
                ? L10n.Get("between_range", state.RangeMin, state.RangeMax)
                : string.Empty;
        }

        if (playerHistoryText != null)
        {
            playerHistoryText.text = HistoryLine(
                L10n.Get("you"), state.PlayerGuessHistory);
        }
        if (aiHistoryText != null)
        {
            aiHistoryText.text = HistoryLine(
                state.OpponentName, state.AiGuessHistory);
        }
        if (historyRoot != null)
            historyRoot.SetActive(state.Phase != SoloBoardPhase.ChooseSecret);

        if (opponentIdentityText != null)
        {
            opponentIdentityText.text = state.OpponentName;
            opponentIdentityText.transform.SetAsLastSibling();
        }

        bool numeric = state.NumericControlsAvailable;
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
    }

    static string PromptText(SoloBoardPresentationState state)
    {
        switch (state.Prompt)
        {
            case SoloBoardPrompt.EnterSecret:
                return L10n.Get("enter_your_number");
            case SoloBoardPrompt.YourGuess:
                return L10n.Get("your_guess");
            case SoloBoardPrompt.OpponentThinking:
                return L10n.Get("opponent_thinking", state.OpponentName);
            case SoloBoardPrompt.AnswerOpponent:
                return L10n.Get("answer_opponent", state.OpponentName);
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

    static string HistoryLine(
        string label,
        IReadOnlyList<int> history)
    {
        var text = new StringBuilder();
        text.Append(label);
        text.Append(':');
        for (int i = 0; i < history.Count; i++)
        {
            text.Append("  ");
            text.Append(history[i]);
        }
        return text.ToString();
    }

    void MoveIfFound(string name, Vector2 position, Vector2 size)
    {
        RectTransform child = FindChild(name);
        if (child == null) return;
        Reparent(child, safeRoot);
        CenterRoot(child, size, position);
        Button button = child.GetComponent<Button>();
        if (button == null) return;

        if (name == "ButtonCORRECT")
            StyleSoloButton(button, SoloGoldFrameResource, Ink);
        else if (name == "ButtonHIGHER")
            StyleSoloButton(button, SoloMagentaFrameResource, NearWhite);
        else
            StyleSoloButton(button, SoloBlueFrameResource, NearWhite);
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

        var shadow = text.GetComponent<Shadow>();
        if (shadow == null)
            shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.02f, 0.01f, 0.12f, 0.68f);
        shadow.effectDistance = new Vector2(2f, -3f);
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
        if (child.parent != parent)
            child.SetParent(parent, false);
        child.gameObject.SetActive(true);
        child.SetAsLastSibling();
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
                "[HolDuelBoardLayout] Missing Resources/" + resource + ".");
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
