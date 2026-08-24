using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the existing HOL solo board and is the sole renderer of its typed
/// presentation state. Gameplay remains owned by GameManager and DuelRules;
/// this component owns prompts, round/range/history, board identity and which
/// numeric controls the current phase may expose.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class HolDuelBoardLayout : MonoBehaviour
{
    const string BackspaceCommand = "BACKSPACE";
    const string SoloPurpleFrameResource = "mainmenu/mainmenu_tip_frame_9s";
    const string SoloBlueFrameResource = "mainmenu/mainmenu_cta_blue_9s";
    const string SoloMagentaFrameResource = "phase2a/hol_cta_magenta_r2_9s";
    const string SoloGoldFrameResource = "mainmenu/mainmenu_cta_gold_9s";


    static readonly Color CardBlue = new Color(0.08f, 0.28f, 0.68f, 0.96f);
    static readonly Color CardPink = new Color(0.72f, 0.08f, 0.34f, 0.96f);
    static readonly Color KeyBlue = new Color(0.16f, 0.18f, 0.62f, 1f);
    static readonly Color Gold = new Color(1f, 0.72f, 0.08f, 1f);
    static readonly Color NearWhite = new Color(0.93f, 0.94f, 1f, 1f);
    static readonly Color Muted = new Color(0.66f, 0.70f, 0.86f, 1f);

    readonly SoloBoardPresentationModel presentation = new SoloBoardPresentationModel();
    readonly List<RectTransform> layoutRoots = new List<RectTransform>();

    RectTransform board;
    NumberManager numberManager;
    GameManager gameManager;
    MenuManager menuManager;
    TMP_InputField input;
    TMP_Text phaseText;
    TMP_Text roundText;
    TMP_Text rangeText;
    TMP_Text playerHistoryText;
    TMP_Text aiHistoryText;
    TMP_Text opponentIdentityText;
    GameObject historyRoot;
    GameObject keypadRoot;
    Button submitControl;
    bool built;

    public SoloBoardPresentationState CurrentState => presentation.Current;
    public Button SubmitControl => submitControl;
    public GameObject KeypadRoot => keypadRoot;

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

        BuildHeader();
        BuildHistoryCard();
        LayoutExistingGameplay();
        BuildKeypad();
        built = true;
        Render();
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
        RuntimeUI.ApplyProductionSprite(image, resource, Image.Type.Sliced,
            false, 2f);
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
        Center(rect, size, position);
        RuntimeUI.ClampToSafeArea(rect, size, position);
        if (!layoutRoots.Contains(rect))
            layoutRoots.Add(rect);
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
        label.raycastTarget = false;
        return label;
    }

    void BuildHeader()
    {
        var back = RuntimeUI.CreateButton(board, "DuelBack", L10n.Get("back"),
            new Vector2(-438f, 790f), new Vector2(118f, 92f), new Color(0.26f, 0.10f, 0.60f, 1f),
            NearWhite);
        StyleSoloButton(back, SoloPurpleFrameResource, NearWhite);
        CenterRoot((RectTransform)back.transform, new Vector2(118f, 92f), new Vector2(-438f, 790f));
        RuntimeUI.Localize(back, "back");
        if (menuManager != null)
            back.onClick.AddListener(menuManager.RequestSoloMatchExit);

        Label(board, "DuelTitle", "HOL", 82, new Vector2(0f, 790f),
            new Vector2(360f, 110f), new Color(0.95f, 0.20f, 0.82f, 1f));

        var player = Card("PlayerCard", new Vector2(470f, 205f), new Vector2(-265f, 565f), CardBlue);
        Label(player.transform, "PlayerCaption", L10n.Get("you"), 30,
            new Vector2(0f, 62f), new Vector2(400f, 44f), NearWhite);
        Label(player.transform, "PlayerName", PlayerPrefs.GetString("PlayerName", L10n.Get("player_default")),
            42, new Vector2(0f, -20f), new Vector2(410f, 56f), NearWhite);

        var opponent = Card("OpponentCard", new Vector2(470f, 205f), new Vector2(265f, 565f), CardPink);
        Label(opponent.transform, "OpponentCaption", L10n.Get("opponent_label", ""),
            30, new Vector2(0f, 62f), new Vector2(420f, 44f), NearWhite);

        Label(board, "VsLabel", "VS", 78, new Vector2(0f, 565f),
            new Vector2(180f, 110f), Gold);
        roundText = Label(board, "RoundLabel", "", 34, new Vector2(0f, 385f),
            new Vector2(700f, 52f), NearWhite);

        phaseText = gameManager != null ? gameManager.turnText : null;
        if (phaseText == null)
            phaseText = Label(board, "PhasePrompt", "", 42, new Vector2(0f, 300f),
                new Vector2(850f, 90f), NearWhite);

        opponentIdentityText = gameManager != null ? gameManager.opponentNameText : null;
        if (opponentIdentityText == null)
            opponentIdentityText = Label(board, "OpponentIdentity", "", 30,
                new Vector2(255f, 680f), new Vector2(430f, 52f), NearWhite);
    }

    void LayoutExistingGameplay()
    {
        if (input != null)
        {
            CenterRoot(input.transform as RectTransform, new Vector2(440f, 122f), new Vector2(-220f, 135f));
            input.shouldHideMobileInput = true;
            input.shouldHideSoftKeyboard = true;
            var image = input.GetComponent<Image>();
            if (image != null)
            {
                RuntimeUI.ApplyProductionSprite(image, SoloPurpleFrameResource,
                    Image.Type.Sliced, false, 2f);
                image.raycastTarget = true;
            }
            if (input.textComponent != null)
                input.textComponent.color = NearWhite;
            var inputPlaceholder = input.placeholder as TMP_Text;
            if (inputPlaceholder != null)
                inputPlaceholder.color = Muted;
        }

        if (numberManager != null && numberManager.playerNumberText != null)
        {
            CenterRoot(numberManager.playerNumberText.rectTransform, new Vector2(420f, 54f),
                new Vector2(-220f, 225f));
            numberManager.playerNumberText.alignment = TextAlignmentOptions.Center;
            numberManager.playerNumberText.fontSize = 28f;
            numberManager.playerNumberText.color = Muted;
        }

        rangeText = gameManager != null ? gameManager.rangeText : null;
        if (rangeText == null)
            rangeText = Label(board, "RangeLabel", "", 27, new Vector2(0f, 20f),
                new Vector2(820f, 48f), Muted);
        LayoutText(rangeText, new Vector2(0f, 20f), new Vector2(820f, 48f), 27f, Muted);

        if (gameManager != null)
        {
            gameManager.rangeText = rangeText;
            LayoutText(phaseText, new Vector2(0f, 300f), new Vector2(850f, 90f), 34f, NearWhite);
            LayoutText(gameManager.aiNumberText, new Vector2(260f, 205f), new Vector2(360f, 48f), 27f, NearWhite);
            LayoutText(gameManager.aiAnswerText, new Vector2(260f, 115f), new Vector2(360f, 105f), 24f, NearWhite);
            LayoutText(opponentIdentityText, new Vector2(255f, 680f), new Vector2(430f, 52f), 28f, NearWhite);
        }

        // The three answer buttons are scene-authored beneath a legacy 100x100
        // grouping RectTransform. Promote that presentation-only group to a
        // full-screen coordinate root at runtime so the shared page contract
        // can own its children without changing the scene or their callbacks.
        var answerRoot = FindChild("AIBUTTONSPANEL");
        if (answerRoot != null)
            RuntimeUI.Stretch(answerRoot.gameObject);
        MoveIfFound("ButtonHIGHER", new Vector2(-300f, -705f), new Vector2(260f, 100f));
        MoveIfFound("ButtonCORRECT", new Vector2(0f, -705f), new Vector2(260f, 100f));
        MoveIfFound("ButtonLOWER", new Vector2(300f, -705f), new Vector2(260f, 100f));
    }

    void LayoutText(TMP_Text text, Vector2 position, Vector2 size, float fontSize, Color color)
    {
        if (text == null) return;
        CenterRoot(text.rectTransform, size, position);
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.color = color;
        RuntimeUI.ConfigureText(text, ResponsiveTextRole.Heading, fontSize);
    }

    void BuildHistoryCard()
    {
        historyRoot = Card("HistoryCard", new Vector2(330f, 360f), new Vector2(330f, -260f),
            new Color(0.05f, 0.04f, 0.20f, 0.98f));
        var title = Label(historyRoot.transform, "HistoryTitle", L10n.Get("guesses"), 28,
            new Vector2(0f, 130f), new Vector2(290f, 48f), NearWhite);
        RuntimeUI.Localize(title, "guesses");
        playerHistoryText = Label(historyRoot.transform, "PlayerGuessHistory", "", 23,
            new Vector2(0f, 45f), new Vector2(290f, 105f), NearWhite);
        aiHistoryText = Label(historyRoot.transform, "AiGuessHistory", "", 23,
            new Vector2(0f, -82f), new Vector2(290f, 105f), NearWhite);

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
        keypadRoot = RuntimeUI.CreateObject("NumberKeypad", board);
        var rootRect = (RectTransform)keypadRoot.transform;
        CenterRoot(rootRect, new Vector2(620f, 620f), new Vector2(-240f, -320f));

        string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "×", "0", BackspaceCommand };
        for (int i = 0; i < keys.Length; i++)
        {
            int index = i;
            int column = i % 3;
            int row = i / 3;
            string label = keys[i] == BackspaceCommand ? "←" : keys[i];
            var button = RuntimeUI.CreateButton(keypadRoot.transform, "Key_" + keys[i], label,
                new Vector2(-205f + column * 205f, 215f - row * 142f),
                new Vector2(178f, 118f), KeyBlue, NearWhite);
            StyleSoloButton(button, SoloBlueFrameResource, NearWhite);
            var text = button.GetComponentInChildren<TMP_Text>();
            if (text != null) text.fontSize = keys[i] == BackspaceCommand || keys[i] == "×" ? 38 : 48;
            button.onClick.AddListener(() => OnKeyPressed(keys[index]));
        }

        foreach (var duplicate in GetComponentsInChildren<Button>(true))
            if (duplicate.name == "NumberSubmit") duplicate.gameObject.SetActive(false);

        var existing = FindChild("ButtonConfirm");
        submitControl = existing != null ? existing.GetComponent<Button>() : null;
        if (submitControl == null)
        {
            submitControl = RuntimeUI.CreateButton(board, "ButtonConfirm", L10n.Get("confirm"),
                new Vector2(-180f, -850f), new Vector2(660f, 112f), Gold,
                new Color(0.15f, 0.08f, 0.04f, 1f));
            submitControl.onClick.AddListener(SubmitNumber);
        }
        else if (submitControl.onClick.GetPersistentEventCount() == 0 && numberManager != null)
        {
            submitControl.onClick.AddListener(numberManager.SubmitNumber);
        }

        CenterRoot((RectTransform)submitControl.transform, new Vector2(660f, 112f), new Vector2(-180f, -850f));
        StyleSoloButton(submitControl, SoloGoldFrameResource,
            new Color(0.15f, 0.08f, 0.04f, 1f));
        var submitLabel = submitControl.GetComponentInChildren<TMP_Text>(true);
        if (submitLabel != null && submitLabel.GetComponent<LocalizedText>() == null)
            RuntimeUI.Localize(submitControl, "confirm");
        ValidateLayout();
    }

    void Render()
    {
        if (!built) return;
        var state = presentation.Current;

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
            opponentIdentityText.text = L10n.Get("opponent_label", state.OpponentName);
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

        foreach (var rect in layoutRoots)
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
