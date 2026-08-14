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

    void Start()
    {
        Invoke(nameof(Build), 0f);
    }

    void Build()
    {
        if (built) return;
        board = (RectTransform)transform;
        numberManager = GetComponent<NumberManager>();
        gameManager = GetComponentInChildren<GameManager>(true);
        input = numberManager != null ? numberManager.numberInput : FindObjectOfType<TMP_InputField>(true);

        BuildHeader();
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

    GameObject Card(string name, Vector2 size, Vector2 position, Color color)
    {
        var card = RuntimeUI.CreateObject(name, board);
        var rect = (RectTransform)card.transform;
        Center(rect, size, position);
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
        RuntimeUI.Localize(back, "back");

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
        Label(opponent.transform, "OpponentName", gameManager != null ? gameManager.opponentNameText.text : "Andreas",
            42, new Vector2(0f, -20f), new Vector2(410f, 56f), NearWhite);

        Label(board, "VsLabel", "VS", 78, new Vector2(0f, 565f),
            new Vector2(180f, 110f), Gold);
        Label(board, "RoundLabel", "ΓΥΡΟΣ 3/10", 34, new Vector2(0f, 385f),
            new Vector2(700f, 52f), NearWhite);
        Label(board, "PromptLabel", L10n.Get("your_guess"), 42, new Vector2(0f, 325f),
            new Vector2(850f, 64f), NearWhite);
    }

    void LayoutExistingGameplay()
    {
        if (input != null)
        {
            Center(input.transform as RectTransform, new Vector2(440f, 122f), new Vector2(-220f, 180f));
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
            Center(numberManager.playerNumberText.rectTransform, new Vector2(420f, 60f),
                new Vector2(-220f, 310f));
            numberManager.playerNumberText.alignment = TextAlignmentOptions.Center;
            numberManager.playerNumberText.fontSize = 28f;
            numberManager.playerNumberText.color = Muted;
        }

        var range = gameManager != null ? gameManager.rangeText : null;
        if (range != null)
        {
            Center(range.rectTransform, new Vector2(420f, 60f), new Vector2(260f, 180f));
            range.alignment = TextAlignmentOptions.Center;
            range.fontSize = 27f;
            range.color = Muted;
        }

        MoveIfFound("ButtonHIGHER", new Vector2(-300f, -650f), new Vector2(260f, 100f));
        MoveIfFound("ButtonCORRECT", new Vector2(0f, -650f), new Vector2(260f, 100f));
        MoveIfFound("ButtonLOWER", new Vector2(300f, -650f), new Vector2(260f, 100f));
    }

    void MoveIfFound(string name, Vector2 position, Vector2 size)
    {
        var child = FindChild(name);
        if (child == null) return;
        Center(child, size, position);
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
        Center(rootRect, new Vector2(660f, 620f), new Vector2(-180f, -360f));

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
            new Vector2(-180f, -815f), new Vector2(660f, 112f), Gold, new Color(0.15f, 0.08f, 0.04f, 1f));
        RuntimeUI.Localize(submit, "confirm");
        submit.onClick.AddListener(SubmitNumber);
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
