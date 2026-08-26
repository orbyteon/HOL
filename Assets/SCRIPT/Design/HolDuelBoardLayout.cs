using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sole Design-layer presentation owner for the HOL solo duel and result.
/// Gameplay remains owned by GameManager, NumberManager and DuelRules; this
/// component owns prompts, round/range/history, board identity and which
/// numeric controls the current phase may expose.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class HolDuelBoardLayout : MonoBehaviour
{
    const string BackspaceCommand = "BACKSPACE";
    const string SoloPurpleFrameResource = CartoonUiKit.PurplePanel;
    const string SoloBlueFrameResource = CartoonUiKit.CyanCta;
    const string SoloMagentaFrameResource = CartoonUiKit.MagentaCta;
    const string SoloGoldFrameResource = CartoonUiKit.GoldCta;
    const string BackgroundResource = CartoonUiKit.Background;
    const string ScreenFrameResource = CartoonUiKit.ScreenFrame;
    const string TitleRibbonResource = CartoonUiKit.TitleRibbon;
    const string LogoResource = CartoonUiKit.Logo;
    const string PlayerResource = CartoonUiKit.PlayerAvatar;
    const string OpponentResource = CartoonUiKit.OpponentCharacter;
    const string AvatarResource = CartoonUiKit.PlayerAvatar;
    const string TrophyResource = CartoonUiKit.Trophy;
    const string VsResource = CartoonUiKit.HomeVs;
    const string MascotSixResource = CartoonUiKit.MascotSix;
    const string MascotSevenResource = CartoonUiKit.MascotSeven;
    const string MascotThreeResource = CartoonUiKit.MascotThree;
    const string BackButtonResource = CartoonUiKit.BackButton;
    const string PlayerCardResource = CartoonUiKit.DuelPlayerCard;
    const string OpponentCardResource = CartoonUiKit.DuelOpponentCard;
    const string KeyResource = CartoonUiKit.DuelKey;
    const string KeypadBoardResource = CartoonUiKit.DuelKeypadBoard;
    const string DuelBoardResource = CartoonUiKit.DuelBoard;
    const string SpeechBubbleResource = CartoonUiKit.SpeechBubble;
    const string ResultWinnerResource = CartoonUiKit.ResultWinner;
    const string DisplayFontResource = CartoonUiKit.DisplayFont;
    const string BodyFontResource = CartoonUiKit.BodyFont;
    const string StarsResource = "mainmenu/mainmenu_deco_stars";
    const string ConfettiResource = "mainmenu/mainmenu_deco_confetti";


    static readonly Color CardBlue = new Color(0.08f, 0.28f, 0.68f, 0.96f);
    static readonly Color CardPink = new Color(0.72f, 0.08f, 0.34f, 0.96f);
    static readonly Color KeyBlue = new Color(0.16f, 0.18f, 0.62f, 1f);
    static readonly Color Gold = new Color(1f, 0.72f, 0.08f, 1f);
    static readonly Color NearWhite = new Color(0.93f, 0.94f, 1f, 1f);
    static readonly Color Muted = new Color(0.66f, 0.70f, 0.86f, 1f);

    readonly SoloBoardPresentationModel presentation = new SoloBoardPresentationModel();
    RectTransform board;
    RectTransform duelBackRect;
    RectTransform duelLogoRect;
    RectTransform duelChipRect;
    RectTransform playerCardRect;
    RectTransform opponentCardRect;
    RectTransform duelVsRect;
    RectTransform roundRibbon;
    RectTransform duelMascotSevenRect;
    RectTransform duelMascotThreeRect;
    RectTransform keypadBoard;
    RectTransform opponentSpeechRect;
    RectTransform speechPortraitRect;
    RectTransform historyRect;
    RectTransform tipRect;
    RectTransform answerHigherRect;
    RectTransform answerCorrectRect;
    RectTransform answerLowerRect;
    RectTransform resultOuterFrameRect;
    RectTransform resultBackRect;
    RectTransform resultLogoRect;
    RectTransform resultChipRect;
    RectTransform resultRibbonRect;
    RectTransform resultWinnerRect;
    RectTransform resultOpponentCardRect;
    RectTransform resultStatsRect;
    RectTransform resultMascotSixRect;
    RectTransform resultMascotSevenRect;
    RectTransform resultRematchRect;
    RectTransform resultExitRect;
    NumberManager numberManager;
    GameManager gameManager;
    MenuManager menuManager;
    TMP_InputField input;
    TMP_Text phaseText;
    TMP_Text roundText;
    TMP_Text rangeText;
    TMP_Text playerHistoryText;
    TMP_Text aiHistoryText;
    TMP_Text opponentSpeechText;
    TMP_Text inputPlaceholderText;
    TMP_Text opponentIdentityText;
    TMP_Text playerChipText;
    TMP_Text playerNameText;
    TMP_Text resultTitleText;
    TMP_Text resultDetailText;
    TMP_Text resultOpponentText;
    TMP_Text resultRematchLabel;
    TMP_Text resultExitLabel;
    GameObject historyRoot;
    GameObject keypadRoot;
    GameObject resultRoot;
    Button submitControl;
    Button resultRematch;
    Button resultExit;
    TMP_FontAsset displayFont;
    TMP_FontAsset bodyFont;
    bool built;
    int lastLayoutWidth = -1;
    int lastLayoutHeight = -1;

    public SoloBoardPresentationState CurrentState => presentation.Current;
    public Button SubmitControl => submitControl;
    public GameObject KeypadRoot => keypadRoot;
    public bool IsReady => built;

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

    void LateUpdate()
    {
        if (!built) return;
        ApplyResponsiveLayout();
    }

    public void BeginNewMatch(string opponentName)
    {
        presentation.BeginNewMatch(opponentName);
        Render();
    }

    public void PresentPhase(SoloBoardPhase phase, SoloBoardPrompt prompt,
        int roundNumber, int rangeMin, int rangeMax, int detailValue = 0)
    {
        presentation.Present(phase, prompt, roundNumber, rangeMin, rangeMax, detailValue);
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
        input = numberManager != null ? numberManager.numberInput : FindObjectOfType<TMP_InputField>(true);
        displayFont = Resources.Load<TMP_FontAsset>(DisplayFontResource);
        bodyFont = Resources.Load<TMP_FontAsset>(BodyFontResource);

        BuildShell();
        BuildHeader();
        BuildKeypad();
        BuildHistoryCard();
        LayoutExistingGameplay();
        BuildResultOverlay();
        RemoveGenericResponsiveWriters();
        built = true;
        ApplyResponsiveLayout(true);
        Render();
    }

    void BuildShell()
    {
        var background = ProductionImage(
            board, "DuelBackground", BackgroundResource,
            Image.Type.Simple, false, 1f);
        RuntimeUI.Stretch(background.gameObject);
        background.transform.SetAsFirstSibling();

        var stars = ProductionImage(
            board, "DuelStars", StarsResource,
            Image.Type.Simple, false, 1f);
        RuntimeUI.Stretch(stars.gameObject);
        stars.raycastTarget = false;

        var confetti = ProductionImage(
            board, "DuelConfetti", ConfettiResource,
            Image.Type.Simple, false, 1f);
        RuntimeUI.Stretch(confetti.gameObject);
        confetti.raycastTarget = false;

    }

    static Image ProductionImage(Transform parent, string name,
        string resource, Image.Type type, bool preserveAspect,
        float pixelsPerUnitMultiplier)
    {
        var go = RuntimeUI.CreateObject(name, parent);
        var image = go.AddComponent<Image>();
        RuntimeUI.ApplyProductionSprite(
            image, resource, type, preserveAspect, pixelsPerUnitMultiplier);
        image.raycastTarget = false;
        return image;
    }

    Image PlaceProductionImage(Transform parent, string name,
        string resource, Vector2 position, Vector2 size,
        bool preserveAspect = true, Image.Type type = Image.Type.Simple,
        float pixelsPerUnitMultiplier = 1f)
    {
        var image = ProductionImage(
            parent, name, resource, type, preserveAspect,
            pixelsPerUnitMultiplier);
        Center(image.rectTransform, size, position);
        return image;
    }

    static void Center(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
    }

    static void MakeDecorative(Image image, string resource)
    {
        if (image == null) return;
        RuntimeUI.ApplyProductionSprite(image, resource, Image.Type.Sliced,
            false, 2f);
        image.raycastTarget = false;
    }

    static string ResolveCardResource(Color color)
    {
        if (ColorDistance(color, CardPink) < 0.35f)
            return SoloMagentaFrameResource;
        if (ColorDistance(color, CardBlue) < 0.35f)
            return SoloBlueFrameResource;
        if (ColorDistance(color, Gold) < 0.35f)
            return SoloGoldFrameResource;
        return SoloPurpleFrameResource;
    }

    static float ColorDistance(Color a, Color b)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return Mathf.Sqrt(dr * dr + dg * dg + db * db);
    }

    static void StyleSoloButton(Button button, string resource, Color labelColor)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image == null) image = button.gameObject.AddComponent<Image>();
        RuntimeUI.ApplyProductionSprite(image, resource, Image.Type.Simple,
            false, 1f);
        image.raycastTarget = true;
        button.targetGraphic = image;
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.color = labelColor;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }
    }

    void CenterRoot(RectTransform rect, Vector2 size, Vector2 position)
    {
        if (rect == null) return;
        // HolDuelBoardLayout is the one geometry authority for this composed
        // screen. Registering only some direct children with
        // ResponsivePageLayout created two concurrent writers at tall aspects.
        Center(rect, size, position);
    }

    GameObject Card(string name, Vector2 size, Vector2 position, Color color)
    {
        var card = RuntimeUI.CreateObject(name, board);
        var rect = (RectTransform)card.transform;
        CenterRoot(rect, size, position);
        var image = card.AddComponent<Image>();
        MakeDecorative(image, ResolveCardResource(color));
        return card;
    }

    TextMeshProUGUI Label(Transform parent, string name, string value, int size, Vector2 position,
        Vector2 dimensions, Color color = default)
    {
        var label = RuntimeUI.CreateText(parent, name, value, size, position, dimensions,
            color == default ? NearWhite : color);
        if (displayFont != null) label.font = displayFont;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    void BuildHeader()
    {
        var back = RuntimeUI.CreateButton(
            board, "DuelBack", string.Empty,
            new Vector2(-450f, 855f), new Vector2(128f, 128f),
            Color.white, NearWhite);
        StyleSoloButton(back, BackButtonResource, NearWhite);
        duelBackRect = (RectTransform)back.transform;
        CenterRoot(
            duelBackRect,
            new Vector2(128f, 128f), new Vector2(-450f, 855f));
        foreach (var text in back.GetComponentsInChildren<TMP_Text>(true))
            text.gameObject.SetActive(false);
        if (menuManager != null)
            back.onClick.AddListener(menuManager.RequestSoloMatchExit);

        duelLogoRect = PlaceProductionImage(
            board, "DuelLogo", LogoResource,
            new Vector2(-34f, 828f), new Vector2(390f, 260f)).rectTransform;

        var chip = PlaceProductionImage(
            board, "DuelPlayerChip", CartoonUiKit.PlayerChip,
            new Vector2(365f, 854f), new Vector2(330f, 142f), false);
        duelChipRect = chip.rectTransform;
        var avatarClip = RuntimeUI.CreateObject(
            "DuelPlayerAvatarClip", chip.transform).transform as RectTransform;
        Center(avatarClip, new Vector2(94f, 94f), new Vector2(-112f, 2f));
        avatarClip.gameObject.AddComponent<RectMask2D>();
        PlaceProductionImage(
            avatarClip, "DuelPlayerAvatar", AvatarResource,
            new Vector2(0f, -25f), new Vector2(124f, 124f));
        PlaceProductionImage(
            chip.transform, "DuelPlayerAvatarRing",
            CartoonUiKit.PlayerAvatarRing,
            new Vector2(-112f, 0f), new Vector2(102f, 102f));
        PlaceProductionImage(
            chip.transform, "DuelChipTrophy", TrophyResource,
            new Vector2(-8f, -24f), new Vector2(38f, 38f));
        playerChipText = Label(
            chip.transform, "DuelPlayerChipText", "", 23,
            new Vector2(58f, 2f), new Vector2(190f, 92f), NearWhite);
        playerChipText.alignment = TextAlignmentOptions.Center;

        var player = PlaceProductionImage(
            board, "PlayerCard", PlayerCardResource,
            new Vector2(-275f, 500f), new Vector2(460f, 550f), false);
        playerCardRect = player.rectTransform;
        PlaceProductionImage(
            player.transform, "PlayerPortrait", PlayerResource,
            new Vector2(0f, 12f), new Vector2(350f, 370f));
        var playerCaption = Label(
            player.transform, "PlayerCaption", L10n.Get("you"), 32,
            new Vector2(0f, 235f), new Vector2(280f, 54f),
            CartoonUiKit.Cyan);
        playerCaption.fontStyle |= FontStyles.UpperCase;
        RuntimeUI.Localize(playerCaption, "you");
        playerNameText = Label(
            player.transform, "PlayerName",
            PlayerPrefs.GetString("PlayerName", L10n.Get("player_default")),
            38, new Vector2(0f, -205f), new Vector2(360f, 58f), NearWhite);

        var opponent = PlaceProductionImage(
            board, "OpponentCard", OpponentCardResource,
            new Vector2(275f, 500f), new Vector2(460f, 550f), false);
        opponentCardRect = opponent.rectTransform;
        PlaceProductionImage(
            opponent.transform, "OpponentPortrait", OpponentResource,
            new Vector2(0f, 16f), new Vector2(350f, 370f));
        var opponentCaption = Label(
            opponent.transform, "OpponentCaption", L10n.Get("opponent_label", ""),
            30, new Vector2(0f, 235f), new Vector2(330f, 54f),
            NearWhite);
        opponentCaption.fontStyle |= FontStyles.UpperCase;
        opponentCaption.overflowMode = TextOverflowModes.Ellipsis;

        opponentIdentityText = Label(
            opponent.transform, "OpponentIdentity", "", 36,
            new Vector2(0f, -205f), new Vector2(360f, 58f), NearWhite);
        if (gameManager != null)
            gameManager.opponentNameText = opponentIdentityText;

        duelVsRect = PlaceProductionImage(
            board, "DuelVsBurst", VsResource,
            new Vector2(0f, 500f), new Vector2(250f, 250f)).rectTransform;

        roundRibbon = PlaceProductionImage(
            board, "DuelRoundRibbon", TitleRibbonResource,
            new Vector2(0f, 120f), new Vector2(760f, 160f), false)
            .rectTransform;
        roundText = Label(
            roundRibbon, "RoundLabel", "", 29,
            new Vector2(0f, 28f), new Vector2(610f, 42f), NearWhite);

        phaseText = gameManager != null ? gameManager.turnText : null;
        if (phaseText == null)
            phaseText = Label(roundRibbon, "PhasePrompt", "", 36,
                new Vector2(0f, -28f), new Vector2(650f, 62f), NearWhite);
        else
            phaseText.transform.SetParent(roundRibbon, false);
        LayoutText(
            phaseText, new Vector2(0f, -28f),
            new Vector2(650f, 62f), 42f, NearWhite);
        phaseText.fontStyle |= FontStyles.UpperCase;
        if (gameManager != null) gameManager.turnText = phaseText;

        duelMascotSevenRect = PlaceProductionImage(
            board, "DuelMascotSeven", MascotSevenResource,
            new Vector2(-455f, 102f), new Vector2(190f, 220f)).rectTransform;
        duelMascotThreeRect = PlaceProductionImage(
            board, "DuelMascotThree", MascotThreeResource,
            new Vector2(455f, 102f), new Vector2(185f, 220f)).rectTransform;
    }

    void LayoutExistingGameplay()
    {
        if (input != null)
        {
            input.transform.SetParent(keypadBoard, false);
            Center(
                input.transform as RectTransform,
                new Vector2(500f, 130f), new Vector2(0f, 215f));
            if (IsLegacyPlaceholderValue(input.text))
                input.text = string.Empty;
            input.shouldHideMobileInput = true;
            input.shouldHideSoftKeyboard = true;
            var image = input.GetComponent<Image>();
            if (image != null)
            {
                RuntimeUI.ApplyProductionSprite(image, SoloPurpleFrameResource,
                    Image.Type.Simple, false, 1f);
                image.raycastTarget = true;
            }
            if (input.textComponent != null)
            {
                if (displayFont != null) input.textComponent.font = displayFont;
                input.textComponent.color = NearWhite;
                input.textComponent.fontSize = 64f;
                input.textComponent.alignment = TextAlignmentOptions.Center;
                StretchInputText(input.textComponent.rectTransform, 24f);
            }
            var inputPlaceholder = input.placeholder as TMP_Text;
            if (inputPlaceholder != null)
            {
                inputPlaceholder.gameObject.SetActive(false);
            }
            inputPlaceholderText = Label(
                input.transform, "DuelInputPlaceholder", "?", 64,
                Vector2.zero, new Vector2(440f, 115f), Muted);
            StretchInputText(inputPlaceholderText.rectTransform, 24f);
            input.placeholder = inputPlaceholderText;
        }

        if (numberManager != null && numberManager.playerNumberText != null)
        {
            numberManager.playerNumberText.transform.SetParent(keypadBoard, false);
            Center(
                numberManager.playerNumberText.rectTransform,
                new Vector2(500f, 58f), new Vector2(0f, 355f));
            if (displayFont != null)
                numberManager.playerNumberText.font = displayFont;
            numberManager.playerNumberText.alignment = TextAlignmentOptions.Center;
            numberManager.playerNumberText.fontSize = 34f;
            numberManager.playerNumberText.color = NearWhite;
            numberManager.playerNumberText.fontStyle = FontStyles.Bold;
        }

        rangeText = gameManager != null ? gameManager.rangeText : null;
        if (rangeText == null)
            rangeText = Label(keypadBoard, "RangeLabel", "", 27,
                new Vector2(0f, 310f), new Vector2(500f, 42f),
                CartoonUiKit.Cyan);
        else
            rangeText.transform.SetParent(keypadBoard, false);
        LayoutText(
            rangeText, new Vector2(0f, 310f),
            new Vector2(500f, 42f), 27f, CartoonUiKit.Cyan);

        if (gameManager != null)
        {
            gameManager.rangeText = rangeText;
            if (gameManager.aiNumberText != null)
                gameManager.aiNumberText.gameObject.SetActive(false);
        }

        // The three answer buttons are scene-authored beneath a legacy 100x100
        // grouping RectTransform. Promote that presentation-only group to a
        // full-screen coordinate root at runtime so the shared page contract
        // can own its children without changing the scene or their callbacks.
        var answerRoot = FindChild("AIBUTTONSPANEL");
        if (answerRoot != null)
            RuntimeUI.Stretch(answerRoot.gameObject);
        MoveIfFound("ButtonHIGHER", new Vector2(-300f, -800f), new Vector2(260f, 110f));
        MoveIfFound("ButtonCORRECT", new Vector2(0f, -800f), new Vector2(260f, 110f));
        MoveIfFound("ButtonLOWER", new Vector2(300f, -800f), new Vector2(260f, 110f));
    }

    void LayoutText(TMP_Text text, Vector2 position, Vector2 size, float fontSize, Color color)
    {
        if (text == null) return;
        CenterRoot(text.rectTransform, size, position);
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.color = color;
        if (displayFont != null) text.font = displayFont;
        text.fontStyle = FontStyles.Bold;
        RuntimeUI.ConfigureText(text, ResponsiveTextRole.Heading, fontSize);
    }

    static void StretchInputText(RectTransform rect, float horizontalPadding)
    {
        if (rect == null) return;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(horizontalPadding, 10f);
        rect.offsetMax = new Vector2(-horizontalPadding, -10f);
        rect.localScale = Vector3.one;
    }

    static bool IsLegacyPlaceholderValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        string normalized = value.Trim();
        return normalized == "Your number? (1-100)" ||
               normalized == "Ο αριθμός σου; (1-100)" ||
               normalized == "1-100";
    }

    void BuildHistoryCard()
    {
        var speech = PlaceProductionImage(
            board, "OpponentSpeechBubble", SpeechBubbleResource,
            new Vector2(330f, -105f), new Vector2(320f, 205f), false);
        opponentSpeechRect = speech.rectTransform;
        opponentSpeechText = gameManager != null
            ? gameManager.aiAnswerText : null;
        if (opponentSpeechText == null)
            opponentSpeechText = Label(
                speech.transform, "OpponentSpeech", "", 28,
                new Vector2(-18f, 14f), new Vector2(240f, 130f),
                CartoonUiKit.Ink);
        else
            opponentSpeechText.transform.SetParent(speech.transform, false);
        LayoutText(
            opponentSpeechText, new Vector2(-18f, 14f),
            new Vector2(240f, 130f), 28f, CartoonUiKit.Ink);
        if (gameManager != null)
            gameManager.aiAnswerText = opponentSpeechText;
        speechPortraitRect = PlaceProductionImage(
            board, "SpeechOpponentPortrait", OpponentResource,
            new Vector2(430f, -118f), new Vector2(160f, 160f)).rectTransform;

        var historyImage = PlaceProductionImage(
            board, "HistoryCard", KeypadBoardResource,
            new Vector2(330f, -430f), new Vector2(330f, 410f), false);
        historyRoot = historyImage.gameObject;
        historyRect = historyImage.rectTransform;
        var title = Label(
            historyRoot.transform, "HistoryTitle", L10n.Get("guesses"), 32,
            new Vector2(0f, 160f), new Vector2(290f, 50f), NearWhite);
        RuntimeUI.Localize(title, "guesses");
        playerHistoryText = Label(
            historyRoot.transform, "PlayerGuessHistory", "", 27,
            new Vector2(0f, 55f), new Vector2(280f, 120f),
            CartoonUiKit.Magenta);
        aiHistoryText = Label(
            historyRoot.transform, "AiGuessHistory", "", 27,
            new Vector2(0f, -92f), new Vector2(280f, 120f),
            CartoonUiKit.Cyan);

        var tip = PlaceProductionImage(
            board, "DuelTipCard", DuelBoardResource,
            new Vector2(330f, -760f), new Vector2(330f, 230f), false)
            .gameObject;
        tipRect = tip.transform as RectTransform;
        Label(
            tip.transform, "DuelTip", L10n.Get("draw_tip"), 23,
            new Vector2(-30f, 5f), new Vector2(240f, 165f), NearWhite);
        PlaceProductionImage(
            tip.transform, "DuelTipMascot", MascotSevenResource,
            new Vector2(112f, -52f), new Vector2(135f, 155f));

        if (gameManager != null)
        {
            gameManager.playerHistoryText = playerHistoryText;
            gameManager.aiHistoryText = aiHistoryText;
        }
    }

    void MoveIfFound(string name, Vector2 position, Vector2 size)
    {
        var child = FindChild(name);
        if (child == null) return;
        CenterRoot(child, size, position);
        if (name == "ButtonHIGHER") answerHigherRect = child;
        else if (name == "ButtonCORRECT") answerCorrectRect = child;
        else if (name == "ButtonLOWER") answerLowerRect = child;
        var button = child.GetComponent<Button>();
        if (button == null) return;
        if (name == "ButtonCORRECT")
            StyleSoloButton(button, SoloGoldFrameResource,
                new Color(0.15f, 0.08f, 0.04f, 1f));
        else if (name == "ButtonLOWER")
            StyleSoloButton(button, SoloMagentaFrameResource, NearWhite);
        else
            StyleSoloButton(button, SoloBlueFrameResource, NearWhite);
    }

    RectTransform FindChild(string name)
    {
        foreach (var rect in GetComponentsInChildren<RectTransform>(true))
            if (rect.name == name) return rect;
        return null;
    }

    void BuildKeypad()
    {
        var boardImage = PlaceProductionImage(
            board, "DuelKeypadBoard", KeypadBoardResource,
            new Vector2(-215f, -485f), new Vector2(630f, 850f), false);
        keypadBoard = boardImage.rectTransform;

        keypadRoot = RuntimeUI.CreateObject("NumberKeypad", keypadBoard);
        var rootRect = (RectTransform)keypadRoot.transform;
        Center(rootRect, new Vector2(540f, 450f), new Vector2(0f, -65f));

        string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "×", "0", BackspaceCommand };
        for (int i = 0; i < keys.Length; i++)
        {
            int index = i;
            int column = i % 3;
            int row = i / 3;
            string label = keys[i] == BackspaceCommand ? "←" : keys[i];
            var button = RuntimeUI.CreateButton(keypadRoot.transform, "Key_" + keys[i], label,
                new Vector2(-176f + column * 176f, 150f - row * 110f),
                new Vector2(150f, 90f), KeyBlue, NearWhite);
            StyleSoloButton(button, KeyResource, NearWhite);
            var text = button.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                if (displayFont != null) text.font = displayFont;
                text.fontSize = keys[i] == BackspaceCommand || keys[i] == "×" ? 42 : 54;
            }
            button.onClick.AddListener(() => OnKeyPressed(keys[index]));
        }

        foreach (var duplicate in GetComponentsInChildren<Button>(true))
            if (duplicate.name == "NumberSubmit") duplicate.gameObject.SetActive(false);

        var existing = FindChild("ButtonConfirm");
        submitControl = existing != null ? existing.GetComponent<Button>() : null;
        if (submitControl == null)
        {
            submitControl = RuntimeUI.CreateButton(keypadBoard, "ButtonConfirm", L10n.Get("confirm"),
            new Vector2(0f, -355f), new Vector2(500f, 112f), Gold,
                new Color(0.15f, 0.08f, 0.04f, 1f));
            submitControl.onClick.AddListener(SubmitNumber);
        }
        else if (submitControl.onClick.GetPersistentEventCount() == 0 && numberManager != null)
        {
            submitControl.onClick.AddListener(numberManager.SubmitNumber);
        }

        submitControl.transform.SetParent(keypadBoard, false);
        Center(
            (RectTransform)submitControl.transform,
            new Vector2(500f, 112f), new Vector2(0f, -355f));
        StyleSoloButton(submitControl, SoloGoldFrameResource,
            new Color(0.15f, 0.08f, 0.04f, 1f));
        foreach (var legacyLabel in
                 submitControl.GetComponentsInChildren<TMP_Text>(true))
            legacyLabel.gameObject.SetActive(false);
        var submitLabel = Label(
            submitControl.transform, "DuelSubmitLabel", L10n.Get("confirm"),
            46, Vector2.zero, new Vector2(450f, 86f),
            new Color(0.15f, 0.08f, 0.04f, 1f));
        if (submitLabel != null)
        {
            if (displayFont != null) submitLabel.font = displayFont;
            submitLabel.fontSize = 46f;
            submitLabel.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            submitLabel.characterSpacing = 0f;
            StretchInputText(submitLabel.rectTransform, 28f);
        }
        RuntimeUI.Localize(submitLabel, "confirm");
        ValidateLayout();
    }

    void BuildResultOverlay()
    {
        resultRoot = RuntimeUI.CreateObject("SoloResultRoot", board);
        RuntimeUI.Stretch(resultRoot);

        var background = ProductionImage(
            resultRoot.transform, "ResultBackground", BackgroundResource,
            Image.Type.Simple, false, 1f);
        RuntimeUI.Stretch(background.gameObject);
        background.raycastTarget = true;

        var stars = ProductionImage(
            resultRoot.transform, "ResultStars", StarsResource,
            Image.Type.Simple, false, 1f);
        RuntimeUI.Stretch(stars.gameObject);

        var confetti = ProductionImage(
            resultRoot.transform, "ResultConfetti", ConfettiResource,
            Image.Type.Simple, false, 1f);
        RuntimeUI.Stretch(confetti.gameObject);

        resultOuterFrameRect = PlaceProductionImage(
            resultRoot.transform, "ResultOuterFrame", ScreenFrameResource,
            Vector2.zero, new Vector2(1056f, 1888f), false,
            Image.Type.Simple, 1f).rectTransform;

        var back = RuntimeUI.CreateButton(
            resultRoot.transform, "ResultBack", string.Empty,
            new Vector2(-450f, 855f), new Vector2(128f, 128f),
            Color.white, NearWhite);
        StyleSoloButton(back, BackButtonResource, NearWhite);
        resultBackRect = (RectTransform)back.transform;
        foreach (var label in back.GetComponentsInChildren<TMP_Text>(true))
            label.gameObject.SetActive(false);
        if (menuManager != null)
            back.onClick.AddListener(menuManager.RequestSoloMatchExit);

        resultLogoRect = PlaceProductionImage(
            resultRoot.transform, "ResultLogo", LogoResource,
            new Vector2(-35f, 815f), new Vector2(380f, 250f)).rectTransform;

        var chip = PlaceProductionImage(
            resultRoot.transform, "ResultPlayerChip", CartoonUiKit.PlayerChip,
            new Vector2(365f, 854f), new Vector2(330f, 142f), false);
        resultChipRect = chip.rectTransform;
        var avatarClip = RuntimeUI.CreateObject(
            "ResultPlayerAvatarClip", chip.transform).transform as RectTransform;
        Center(avatarClip, new Vector2(94f, 94f), new Vector2(-112f, 2f));
        avatarClip.gameObject.AddComponent<RectMask2D>();
        PlaceProductionImage(
            avatarClip, "ResultPlayerAvatar", AvatarResource,
            new Vector2(0f, -25f), new Vector2(124f, 124f));
        PlaceProductionImage(
            chip.transform, "ResultPlayerAvatarRing",
            CartoonUiKit.PlayerAvatarRing,
            new Vector2(-112f, 0f), new Vector2(102f, 102f));
        PlaceProductionImage(
            chip.transform, "ResultChipTrophy", TrophyResource,
            new Vector2(-8f, -24f), new Vector2(38f, 38f));
        var resultChipText = Label(
            chip.transform, "ResultPlayerChipText", "", 23,
            new Vector2(58f, 2f), new Vector2(190f, 92f), NearWhite);
        resultChipText.text =
            PlayerPrefs.GetString("PlayerName", L10n.Get("player_default")) +
            "\n" + GameStats.Wins;

        var ribbon = PlaceProductionImage(
            resultRoot.transform, "ResultTitleRibbon", TitleRibbonResource,
            new Vector2(0f, 610f), new Vector2(880f, 185f), false);
        resultRibbonRect = ribbon.rectTransform;
        resultTitleText = Label(
            ribbon.transform, "ResultTitle", "", 72,
            Vector2.zero, new Vector2(700f, 105f), NearWhite);
        resultTitleText.fontStyle = FontStyles.Bold;

        resultWinnerRect = PlaceProductionImage(
            resultRoot.transform, "ResultWinner", ResultWinnerResource,
            new Vector2(-120f, 260f), new Vector2(720f, 585f)).rectTransform;

        var opponentCard = PlaceProductionImage(
            resultRoot.transform, "ResultOpponentCard", OpponentCardResource,
            new Vector2(350f, 255f), new Vector2(300f, 365f), false)
            .gameObject;
        resultOpponentCardRect = opponentCard.transform as RectTransform;
        PlaceProductionImage(
            opponentCard.transform, "ResultOpponentPortrait", OpponentResource,
            new Vector2(0f, 10f), new Vector2(230f, 235f));
        var defeated = Label(
            opponentCard.transform, "ResultOpponentStatus",
            L10n.Get("result_defeated"), 22,
            new Vector2(0f, 155f), new Vector2(240f, 45f), NearWhite);
        defeated.enableAutoSizing = false;
        defeated.fontSize = 22f;
        defeated.enableWordWrapping = false;
        defeated.overflowMode = TextOverflowModes.Truncate;
        RuntimeUI.Localize(defeated, "result_defeated");
        resultOpponentText = Label(
            opponentCard.transform, "ResultOpponentName", "", 28,
            new Vector2(0f, -139f), new Vector2(250f, 52f), NearWhite);

        var stats = PlaceProductionImage(
            resultRoot.transform, "ResultStatsCard", DuelBoardResource,
            new Vector2(0f, -195f), new Vector2(870f, 330f), false)
            .gameObject;
        resultStatsRect = stats.transform as RectTransform;
        resultDetailText = Label(
            stats.transform, "ResultDetail", "", 38,
            Vector2.zero, new Vector2(760f, 245f), NearWhite);
        resultDetailText.alignment = TextAlignmentOptions.Center;

        resultMascotSixRect = PlaceProductionImage(
            resultRoot.transform, "ResultMascotSix", MascotSixResource,
            new Vector2(-405f, -710f), new Vector2(255f, 255f)).rectTransform;
        resultMascotSevenRect = PlaceProductionImage(
            resultRoot.transform, "ResultMascotSeven", MascotSevenResource,
            new Vector2(405f, -710f), new Vector2(255f, 255f)).rectTransform;

        resultRematch = gameManager != null && gameManager.stopGameButton != null
            ? gameManager.stopGameButton.GetComponent<Button>()
            : null;
        if (resultRematch == null)
        {
            resultRematch = RuntimeUI.CreateButton(
                resultRoot.transform, "SoloResultRematch", L10n.Get("rematch"),
                Vector2.zero, new Vector2(570f, 125f), Gold,
                new Color(0.15f, 0.08f, 0.04f, 1f));
            if (gameManager != null)
                resultRematch.onClick.AddListener(gameManager.RestartMatch);
        }
        else
        {
            resultRematch.transform.SetParent(resultRoot.transform, false);
        }

        resultRematchRect = resultRematch.transform as RectTransform;
        CenterRoot(
            resultRematchRect,
            new Vector2(570f, 125f), new Vector2(0f, -515f));
        StyleSoloButton(
            resultRematch, SoloGoldFrameResource,
            new Color(0.15f, 0.08f, 0.04f, 1f));
        foreach (var legacyLabel in
                 resultRematch.GetComponentsInChildren<TMP_Text>(true))
            legacyLabel.gameObject.SetActive(false);
        resultRematchLabel = Label(
            resultRematch.transform, "ResultRematchLabel",
            L10n.Get("rematch"), 52, Vector2.zero,
            new Vector2(510f, 90f), CartoonUiKit.Ink);
        resultRematchLabel.fontStyle |= FontStyles.UpperCase;
        RuntimeUI.Localize(resultRematchLabel, "rematch");

        resultExit = RuntimeUI.CreateButton(
            resultRoot.transform, "SoloResultExit", L10n.Get("result_exit"),
            new Vector2(0f, -665f), new Vector2(455f, 105f),
            CardBlue, NearWhite);
        resultExitRect = resultExit.transform as RectTransform;
        CenterRoot(
            resultExitRect,
            new Vector2(455f, 105f), new Vector2(0f, -665f));
        StyleSoloButton(resultExit, SoloBlueFrameResource, NearWhite);
        if (menuManager != null)
            resultExit.onClick.AddListener(menuManager.RequestSoloMatchExit);
        foreach (var legacyLabel in
                 resultExit.GetComponentsInChildren<TMP_Text>(true))
            legacyLabel.gameObject.SetActive(false);
        resultExitLabel = Label(
            resultExit.transform, "ResultExitLabel", L10n.Get("result_exit"),
            45, Vector2.zero, new Vector2(400f, 78f), NearWhite);
        RuntimeUI.Localize(resultExitLabel, "result_exit");

        resultRoot.SetActive(false);
    }

    void RemoveGenericResponsiveWriters()
    {
        // RuntimeUI.CreateButton/CreateText register direct full-screen children
        // with the generic page layout. This screen has measured, state-aware
        // owner geometry, so leaving those components alive would reintroduce a
        // second writer on only a subset of the composition.
        var boardLayout = board == null
            ? null
            : board.GetComponent<ResponsivePageLayout>();
        if (boardLayout != null)
            RuntimeUI.DestroyNow(boardLayout);

        var resultRect = resultRoot == null
            ? null
            : resultRoot.transform as RectTransform;
        var resultLayout = resultRect == null
            ? null
            : resultRect.GetComponent<ResponsivePageLayout>();
        if (resultLayout != null)
            RuntimeUI.DestroyNow(resultLayout);
    }

    void ApplyResponsiveLayout(bool force = false)
    {
        if (board == null || duelBackRect == null || duelLogoRect == null ||
            duelChipRect == null || playerCardRect == null ||
            opponentCardRect == null || duelVsRect == null ||
            roundRibbon == null || keypadBoard == null ||
            opponentSpeechRect == null || speechPortraitRect == null ||
            historyRect == null || tipRect == null ||
            resultOuterFrameRect == null || resultBackRect == null ||
            resultLogoRect == null || resultChipRect == null ||
            resultRibbonRect == null || resultWinnerRect == null ||
            resultOpponentCardRect == null || resultStatsRect == null ||
            resultRematchRect == null || resultExitRect == null)
            return;

        int width = Mathf.Max(1, Screen.width);
        int height = Mathf.Max(1, Screen.height);
        if (!force && width == lastLayoutWidth && height == lastLayoutHeight)
            return;

        lastLayoutWidth = width;
        lastLayoutHeight = height;
        // PanelGAME is governed by CanvasScaler, so its logical height is not
        // the raw Screen.height. Derive the usable vertical room from the
        // actual board rect; this prevents top clipping while still using every
        // extra logical pixel on tall phones.
        float halfExtra = Mathf.Max(0f, (board.rect.height - 1920f) * 0.5f);
        float tall = Mathf.InverseLerp(0f, 180f, halfExtra);
        float frameExtra = Mathf.Min(480f, halfExtra * 2f);
        float halfWidth = Mathf.Max(1f, board.rect.width * 0.5f);
        float backX = -Mathf.Min(450f, Mathf.Max(0f, halfWidth - 90f));
        float chipX = Mathf.Min(365f, Mathf.Max(0f, halfWidth - 175f));
        float cardX = Mathf.Min(275f, Mathf.Max(0f, halfWidth - 265f));
        float ribbonMascotX = Mathf.Min(455f, Mathf.Max(0f, halfWidth - 85f));
        float keypadX = Mathf.Max(-215f, -halfWidth + 325f);
        float portraitX = Mathf.Min(430f, Mathf.Max(0f, halfWidth - 110f));
        float resultOpponentX = Mathf.Min(
            350f, Mathf.Max(0f, halfWidth - 175f));
        float resultMascotX = Mathf.Min(
            405f, Mathf.Max(0f, halfWidth - 135f));
        float resultWinnerX = -Mathf.Min(
            120f, Mathf.Max(0f, halfWidth - 375f));

        Center(duelBackRect, new Vector2(128f, 128f),
            new Vector2(backX, 855f + 185f * tall));
        Center(duelLogoRect, new Vector2(390f, 260f),
            new Vector2(-34f, 828f + 165f * tall));
        Center(duelChipRect, new Vector2(330f, 142f),
            new Vector2(chipX, 854f + 185f * tall));
        Center(playerCardRect, new Vector2(460f, 550f),
            new Vector2(-cardX, 500f + 120f * tall));
        Center(opponentCardRect, new Vector2(460f, 550f),
            new Vector2(cardX, 500f + 120f * tall));
        Center(duelVsRect, new Vector2(250f, 250f),
            new Vector2(0f, 500f + 120f * tall));
        Center(roundRibbon, new Vector2(760f, 160f),
            new Vector2(0f, 120f + 70f * tall));
        if (duelMascotSevenRect != null)
            Center(duelMascotSevenRect, new Vector2(190f, 220f),
                new Vector2(-ribbonMascotX, 102f + 70f * tall));
        if (duelMascotThreeRect != null)
            Center(duelMascotThreeRect, new Vector2(185f, 220f),
                new Vector2(ribbonMascotX, 102f + 70f * tall));
        Center(keypadBoard, new Vector2(630f, 850f),
            new Vector2(keypadX, -485f - 130f * tall));
        Center(opponentSpeechRect, new Vector2(320f, 205f),
            new Vector2(330f, -105f - 60f * tall));
        Center(speechPortraitRect, new Vector2(160f, 160f),
            new Vector2(portraitX, -118f - 60f * tall));
        Center(historyRect, new Vector2(330f, 410f),
            new Vector2(330f, -430f - 70f * tall));
        Center(tipRect, new Vector2(330f, 230f),
            new Vector2(330f, -760f - 170f * tall));
        if (answerHigherRect != null)
            Center(answerHigherRect, new Vector2(260f, 110f),
                new Vector2(-300f, -800f - 180f * tall));
        if (answerCorrectRect != null)
            Center(answerCorrectRect, new Vector2(260f, 110f),
                new Vector2(0f, -800f - 180f * tall));
        if (answerLowerRect != null)
            Center(answerLowerRect, new Vector2(260f, 110f),
                new Vector2(300f, -800f - 180f * tall));

        Center(resultOuterFrameRect,
            new Vector2(1056f, 1888f + frameExtra), Vector2.zero);
        Center(resultBackRect, new Vector2(128f, 128f),
            new Vector2(backX, 855f + 185f * tall));
        Center(resultLogoRect, new Vector2(380f, 250f),
            new Vector2(-35f, 815f + 165f * tall));
        Center(resultChipRect, new Vector2(330f, 142f),
            new Vector2(chipX, 854f + 185f * tall));
        Center(resultRibbonRect, new Vector2(880f, 185f),
            new Vector2(0f, 610f + 105f * tall));
        Center(resultWinnerRect, new Vector2(720f, 585f),
            new Vector2(resultWinnerX, 260f + 45f * tall));
        Center(resultOpponentCardRect, new Vector2(300f, 365f),
            new Vector2(resultOpponentX, 255f + 45f * tall));
        Center(resultStatsRect, new Vector2(870f, 330f),
            new Vector2(0f, -195f - 30f * tall));
        Center(resultRematchRect, new Vector2(570f, 125f),
            new Vector2(0f, -515f - 125f * tall));
        Center(resultExitRect, new Vector2(455f, 105f),
            new Vector2(0f, -665f - 140f * tall));
        if (resultMascotSixRect != null)
            Center(resultMascotSixRect, new Vector2(255f, 255f),
                new Vector2(-resultMascotX, -710f - 180f * tall));
        if (resultMascotSevenRect != null)
            Center(resultMascotSevenRect, new Vector2(255f, 255f),
                new Vector2(resultMascotX, -710f - 180f * tall));
    }

    GameObject CardOn(Transform parent, string name, Vector2 size,
        Vector2 position, Color color)
    {
        var card = RuntimeUI.CreateObject(name, parent);
        var rect = (RectTransform)card.transform;
        Center(rect, size, position);
        var image = card.AddComponent<Image>();
        MakeDecorative(image, ResolveCardResource(color));
        return card;
    }

    void Render()
    {
        if (!built) return;
        var state = presentation.Current;
        bool showResult = state.Phase == SoloBoardPhase.MatchResult;

        if (playerChipText != null)
        {
            playerChipText.text =
                PlayerPrefs.GetString("PlayerName", L10n.Get("player_default")) +
                "\n" + GameStats.Wins;
        }
        if (playerNameText != null)
            playerNameText.text =
                PlayerPrefs.GetString(
                    "PlayerName", L10n.Get("player_default"));
        if (inputPlaceholderText != null)
            inputPlaceholderText.text = "?";

        if (resultRoot != null)
            resultRoot.SetActive(showResult);
        if (showResult)
        {
            if (resultTitleText != null)
                resultTitleText.text = ResultTitle(state);
            if (resultDetailText != null)
                resultDetailText.text = ResultDetail(state);
            if (resultOpponentText != null)
                resultOpponentText.text = state.OpponentName;
            if (resultRematchLabel != null)
                resultRematchLabel.text = L10n.Get("rematch");
            if (resultExitLabel != null)
                resultExitLabel.text = L10n.Get("result_exit");
            if (resultRematch != null)
                resultRematch.gameObject.SetActive(true);
            if (resultExit != null)
                resultExit.gameObject.SetActive(true);
        }

        if (phaseText != null)
            phaseText.text = PromptText(state);

        if (roundText != null)
        {
            bool showRound = state.RoundNumber > 0;
            roundText.gameObject.SetActive(showRound);
            roundText.text = showRound ? L10n.Get("round_label_open", state.RoundNumber) : "";
        }

        if (rangeText != null)
        {
            bool showRange = state.Phase != SoloBoardPhase.ChooseSecret &&
                             state.Phase != SoloBoardPhase.MatchResult;
            rangeText.gameObject.SetActive(showRange);
            rangeText.text = showRange ? L10n.Get("between_range", state.RangeMin, state.RangeMax) : "";
        }

        if (playerHistoryText != null)
            playerHistoryText.text = HistoryLine(L10n.Get("you"), state.PlayerGuessHistory);
        if (aiHistoryText != null)
            aiHistoryText.text = HistoryLine(state.OpponentName, state.AiGuessHistory);
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
            if (!numeric) input.DeactivateInputField();
        }
        if (keypadRoot != null) keypadRoot.SetActive(numeric);
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
                return "";
            case SoloBoardPrompt.Win:
            {
                string result = L10n.Get("you_win") + "\n" + L10n.Get("won_in_guesses", state.DetailValue);
                if (state.DetailValue <= 7) result += "\n" + L10n.Get("perfect_game");
                return result;
            }
            case SoloBoardPrompt.Loss:
                return L10n.Get("you_lose") + "\n" + L10n.Get("number_was", state.DetailValue);
            case SoloBoardPrompt.Draw:
                return L10n.Get("you_draw") + "\n" +
                       L10n.Get("draw_in_guesses", state.DetailValue) + "\n" + L10n.Get("draw_tip");
            default:
                return "";
        }
    }

    static string ResultTitle(SoloBoardPresentationState state)
    {
        switch (state.Prompt)
        {
            case SoloBoardPrompt.Win:
                return L10n.Get("you_win");
            case SoloBoardPrompt.Loss:
                return L10n.Get("you_lose");
            case SoloBoardPrompt.Draw:
                return L10n.Get("you_draw");
            default:
                return "";
        }
    }

    static string ResultDetail(SoloBoardPresentationState state)
    {
        switch (state.Prompt)
        {
            case SoloBoardPrompt.Win:
            {
                string detail =
                    L10n.Get("result_attempts") + ":  " + state.DetailValue +
                    "\n" + L10n.Get("stats_wins") + ":  " + GameStats.Wins;
                if (state.DetailValue <= 7)
                    detail += "\n" + L10n.Get("perfect_game");
                return detail;
            }
            case SoloBoardPrompt.Loss:
                return L10n.Get("number_was", state.DetailValue);
            case SoloBoardPrompt.Draw:
                return L10n.Get("draw_in_guesses", state.DetailValue);
            default:
                return "";
        }
    }

    static string HistoryLine(string label, IReadOnlyList<int> history)
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

    void ValidateLayout()
    {
        if (board == null) return;
        if (Vector3.Distance(board.localScale, Vector3.one) > 0.001f)
            Debug.LogWarning("HOL layout: PanelGAME scale is not 1.0.");

        var canvas = board.parent != null ? board.parent.GetComponentInParent<Canvas>() : null;
        var scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
        if (scaler != null && (scaler.referenceResolution.x != 1080f || scaler.referenceResolution.y != 1920f))
            Debug.LogWarning("HOL layout: expected CanvasScaler reference resolution 1080x1920.");

        RectTransform[] ownedRoots =
        {
            duelBackRect, duelLogoRect, duelChipRect, playerCardRect,
            opponentCardRect, duelVsRect, roundRibbon, keypadBoard,
            opponentSpeechRect, historyRect, tipRect,
            resultOuterFrameRect, resultBackRect, resultLogoRect,
            resultChipRect, resultRibbonRect, resultWinnerRect,
            resultOpponentCardRect, resultStatsRect, resultRematchRect,
            resultExitRect
        };
        foreach (var rect in ownedRoots)
        {
            if (rect == null) continue;
            if (rect.anchorMin != new Vector2(0.5f, 0.5f) ||
                rect.anchorMax != new Vector2(0.5f, 0.5f) ||
                rect.pivot != new Vector2(0.5f, 0.5f))
                Debug.LogWarning("HOL layout: non-centered root " + rect.name);
            if (rect.rect.width < 48f || rect.rect.height < 48f)
                Debug.LogWarning("HOL layout: touch target below 48px: " + rect.name);
        }
    }

    void OnKeyPressed(string key)
    {
        if (input == null || !input.interactable) return;
        if (key == "×")
        {
            input.text = "";
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
}
