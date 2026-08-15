using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies the new HOL duel-board composition to PanelGAME at runtime.
/// Coordinates are authored against the existing 1080x1920 CanvasScaler:
/// center anchors, pivot (0.5, 0.5), anchoredPosition in reference pixels.
/// The keypad is functional and submits through NumberManager.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class HolDuelBoardLayout : MonoBehaviour
{
    static readonly Color Indigo = new Color(0.035f, 0.035f, 0.12f, 0.98f);
    static readonly Color CardBlue = new Color(0.08f, 0.28f, 0.68f, 0.96f);
    static readonly Color CardPink = new Color(0.72f, 0.08f, 0.34f, 0.96f);
    static readonly Color KeyBlue = new Color(0.16f, 0.18f, 0.62f, 1f);
    static readonly Color Gold = new Color(1f, 0.72f, 0.08f, 1f);
    static readonly Color NearWhite = new Color(0.93f, 0.94f, 1f, 1f);
    static readonly Color Muted = new Color(0.66f, 0.70f, 0.86f, 1f);

    RectTransform board;
    NumberManager numberManager;
    GameManager gameManager;
    TMP_InputField input;
    GameObject keypadRoot;
    bool built;
    readonly List<RectTransform> layoutRoots = new List<RectTransform>();

    void Start()
    {
        Invoke(nameof(Build), 0f);
    }

    void Build()
    {
        if (built) return;
        board = (RectTransform)transform;
        board.localScale = Vector3.one;
        board.localPosition = Vector3.zero;
        numberManager = GetComponent<NumberManager>();
        gameManager = FindObjectOfType<GameManager>(true);
        input = numberManager != null ? numberManager.numberInput : FindObjectOfType<TMP_InputField>(true);

        BuildHeader();
        BuildHistoryCard();
        LayoutExistingGameplay();
        BuildKeypad();
        built = true;
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

    static void MakeDecorative(Image image)
    {
        image.raycastTarget = false;
        image.type = Image.Type.Sliced;
        image.sprite = RuntimeUI.RoundedRectSprite;
    }

    void CenterRoot(RectTransform rect, Vector2 size, Vector2 position)
    {
        Center(rect, size, position);
        rect.anchoredPosition = ClampToSafeArea(position, size);
        if (!layoutRoots.Contains(rect))
            layoutRoots.Add(rect);
    }

    Vector2 ClampToSafeArea(Vector2 position, Vector2 size)
    {
        var canvasRect = board != null ? board.parent as RectTransform : null;
        Vector2 canvasSize = canvasRect != null && canvasRect.rect.size.sqrMagnitude > 0f
            ? canvasRect.rect.size
            : new Vector2(1080f, 1920f);

        Rect safe = Screen.safeArea;
        float width = Mathf.Max(1f, Screen.width);
        float height = Mathf.Max(1f, Screen.height);
        float left = (safe.xMin / width) * canvasSize.x - canvasSize.x * 0.5f;
        float right = (safe.xMax / width) * canvasSize.x - canvasSize.x * 0.5f;
        float bottom = (safe.yMin / height) * canvasSize.y - canvasSize.y * 0.5f;
        float top = (safe.yMax / height) * canvasSize.y - canvasSize.y * 0.5f;

        float halfWidth = size.x * 0.5f + 16f;
        float halfHeight = size.y * 0.5f + 16f;
        return new Vector2(
            Mathf.Clamp(position.x, left + halfWidth, right - halfWidth),
            Mathf.Clamp(position.y, bottom + halfHeight, top - halfHeight));
    }

    GameObject Card(string name, Vector2 size, Vector2 position, Color color)
    {
        var card = RuntimeUI.CreateObject(name, board);
        var rect = (RectTransform)card.transform;
        CenterRoot(rect, size, position);
        var image = card.AddComponent<Image>();
        image.color = color;
        MakeDecorative(image);
        return card;
    }

    Text Label(Transform parent, string name, string value, int size, Vector2 position,
        Vector2 dimensions, Color color = default)
    {
        var label = RuntimeUI.CreateText(parent, name, value, size, position, dimensions,
            color == default ? NearWhite : color);
        label.raycastTarget = false;
        return label;
    }

    void BuildHeader()
    {
        // Top-left navigation affordance, aligned to the safe top inset.
        var back = RuntimeUI.CreateButton(board, "DuelBack", L10n.Get("back"),
            new Vector2(-438f, 790f), new Vector2(118f, 92f), new Color(0.26f, 0.10f, 0.60f, 1f),
            NearWhite);
        CenterRoot((RectTransform)back.transform, new Vector2(118f, 92f), new Vector2(-438f, 790f));
        RuntimeUI.Localize(back, "back");
        if (numberManager != null) back.onClick.AddListener(numberManager.ExitToMenu);

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
        Label(opponent.transform, "OpponentName", gameManager != null && gameManager.opponentNameText != null ? gameManager.opponentNameText.text : "Andreas",
            42, new Vector2(0f, -20f), new Vector2(410f, 56f), NearWhite);

        Label(board, "VsLabel", "VS", 78, new Vector2(0f, 565f),
            new Vector2(180f, 110f), Gold);
        Label(board, "RoundLabel", L10n.Get("round_label", 1, 10), 34, new Vector2(0f, 385f),
            new Vector2(700f, 52f), NearWhite);
        Label(board, "PromptLabel", L10n.Get("your_guess"), 42, new Vector2(0f, 325f),
            new Vector2(850f, 64f), NearWhite);
    }

    void LayoutExistingGameplay()
    {
        if (input != null)
        {
            CenterRoot(input.transform as RectTransform, new Vector2(440f, 122f), new Vector2(-220f, 180f));
            input.shouldHideMobileInput = true;
            var image = input.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = RuntimeUI.RoundedRectSprite;
                image.type = Image.Type.Sliced;
                image.color = new Color(0.05f, 0.04f, 0.18f, 1f);
            }
        }

        if (numberManager != null && numberManager.playerNumberText != null)
        {
            CenterRoot(numberManager.playerNumberText.rectTransform, new Vector2(420f, 60f),
                new Vector2(-220f, 310f));
            numberManager.playerNumberText.alignment = TextAlignmentOptions.Center;
            numberManager.playerNumberText.fontSize = 28f;
            numberManager.playerNumberText.color = Muted;
        }

        var range = gameManager != null ? gameManager.rangeText : null;
        if (range != null)
        {
            CenterRoot(range.rectTransform, new Vector2(420f, 60f), new Vector2(260f, 180f));
            range.alignment = TextAlignmentOptions.Center;
            range.fontSize = 27f;
            range.color = Muted;
        }

        if (gameManager != null)
        {
            LayoutText(gameManager.turnText, new Vector2(0f, 250f), new Vector2(900f, 60f), 28f, Muted);
            LayoutText(gameManager.aiNumberText, new Vector2(260f, 305f), new Vector2(360f, 58f), 27f, NearWhite);
            LayoutText(gameManager.aiAnswerText, new Vector2(260f, 180f), new Vector2(360f, 105f), 24f, NearWhite);
            LayoutText(gameManager.playerHistoryText, new Vector2(285f, -245f), new Vector2(310f, 90f), 24f, NearWhite);
            LayoutText(gameManager.aiHistoryText, new Vector2(285f, -365f), new Vector2(310f, 90f), 24f, NearWhite);
        }

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
        text.enableWordWrapping = true;
    }

    void BuildHistoryCard()
    {
        var history = Card("HistoryCard", new Vector2(360f, 360f), new Vector2(300f, -260f),
            new Color(0.05f, 0.04f, 0.20f, 0.98f));
        Label(history.transform, "HistoryTitle", L10n.Get("guesses"), 28,
            new Vector2(0f, 130f), new Vector2(320f, 48f), NearWhite);
        Label(history.transform, "HistoryHint", L10n.Get("your_guess"), 22,
            new Vector2(0f, 85f), new Vector2(300f, 40f), Muted);
    }

    void MoveIfFound(string name, Vector2 position, Vector2 size)
    {
        var child = FindChild(name);
        if (child == null) return;
        CenterRoot(child, size, position);
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
        CenterRoot(rootRect, new Vector2(660f, 620f), new Vector2(-180f, -285f));

        string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "×", "0", "⌫" };
        for (int i = 0; i < keys.Length; i++)
        {
            int index = i;
            int column = i % 3;
            int row = i / 3;
            var button = RuntimeUI.CreateButton(keypadRoot.transform, "Key_" + keys[i], keys[i],
                new Vector2(-220f + column * 220f, 215f - row * 142f),
                new Vector2(190f, 118f), KeyBlue, NearWhite);
            var text = button.GetComponentInChildren<Text>();
            if (text != null) text.fontSize = keys[i] == "⌫" || keys[i] == "×" ? 38 : 48;
            button.onClick.AddListener(() => OnKeyPressed(keys[index]));
        }

        var submit = RuntimeUI.CreateButton(board, "NumberSubmit", L10n.Get("confirm"),
            new Vector2(-180f, -850f), new Vector2(660f, 112f), Gold, new Color(0.15f, 0.08f, 0.04f, 1f));
        CenterRoot((RectTransform)submit.transform, new Vector2(660f, 112f), new Vector2(-180f, -850f));
        RuntimeUI.Localize(submit, "confirm");
        submit.onClick.AddListener(SubmitNumber);

        ValidateLayout();
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
        if (input == null) return;
        if (key == "×")
        {
            input.text = "";
            return;
        }

        if (key == "⌫")
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
