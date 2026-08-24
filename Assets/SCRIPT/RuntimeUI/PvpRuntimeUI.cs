using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Zero-setup PvP interface: builds the whole duel UI from code and injects
// a "PvP DUEL" entry button into the scene's main Canvas. No scene wiring
// needed beyond dropping this component on one GameObject.
//
// PvP is always server-authoritative through PlayFab. The public Title ID is
// copied onto the backend component created at Start.
[RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
public class PvpRuntimeUI : MonoBehaviour
{
    [Tooltip("PlayFab Title ID from Game Manager — copied onto the PlayFab backend")]
    public string playFabTitleId = "";

    // Dynamic state colors only. Production surfaces are approved sprites.
    static readonly Color PanelColor = HolUiStateColors.WithAlpha(HolUiStateColors.Background0, 0.97f);
    static readonly Color Accent = HolUiStateColors.Gold;
    static readonly Color AccentBlue = HolUiStateColors.Cyan;
    static readonly Color Neutral = HolUiStateColors.SurfaceElevated;
    static readonly Color DarkLabel = new Color(0.10f, 0.09f, 0.18f, 1f);

    void Start()
    {
        // Backend + controller live on this same GameObject. The backend is
        // created here, so its public dashboard config comes from this field.
        var backend = gameObject.AddComponent<PlayFabPvpClient>();
        backend.titleId = playFabTitleId;

        var controller = gameObject.AddComponent<PvpGameController>();
        controller.client = backend;

        BuildPanels(controller);

        InjectEntryButton(controller);
    }

    // ------------------------------------------------------------ construction

    // The big "current number" treatment: centred display text, no soft
    // keyboard — the on-screen keypad is the input path, so the OS keyboard
    // never slides up over the board it is editing.
    static void StyleNumberDisplay(TMP_InputField input)
    {
        input.shouldHideSoftKeyboard = true;
        if (input.textComponent != null)
        {
            input.textComponent.fontSize = 60;
            input.textComponent.alignment = TextAlignmentOptions.Center;
        }
        var placeholder = input.placeholder as TMP_Text;
        if (placeholder != null)
        {
            placeholder.fontSize = 44;
            placeholder.alignment = TextAlignmentOptions.Center;
        }
    }

    // Keeps the duel header's own-name card honest: the controller only ever
    // writes the opponent's name, and the local name can change in settings
    // between matches, so it is re-read every time the match panel comes up.
    class PlayerNameLabel : MonoBehaviour
    {
        public TMP_Text target;

        void OnEnable()
        {
            if (target == null) return;
            string name = PlayerPrefs.GetString("PlayerName", "");
            target.text = string.IsNullOrWhiteSpace(name) ? L10n.Get("player_default") : name;
        }
    }

    const string PvpBackgroundResource = "phase2a/hol_neon_reference_bg_r3";
    const string PvpPurpleFrameResource = "mainmenu/mainmenu_tip_frame_9s";
    const string PvpBlueFrameResource = "mainmenu/mainmenu_cta_blue_9s";
    const string PvpMagentaFrameResource = "phase2a/hol_cta_magenta_r2_9s";
    const string PvpGoldFrameResource = "mainmenu/mainmenu_cta_gold_9s";

    static GameObject CreatePvpPanel(Transform parent, string name, Color fallback)
    {
        var panel = RuntimeUI.FullscreenPanel(parent, name, fallback);
        var image = panel.GetComponent<Image>();
        RuntimeUI.ApplyProductionSprite(image, PvpBackgroundResource,
            Image.Type.Simple, false);
        image.raycastTarget = true;
        return panel;
    }

    static Button CreatePvpButton(Transform parent, string name, string label,
        Vector2 position, Vector2 size, Color color, Color? labelColor = null)
    {
        var button = RuntimeUI.CreateButton(parent, name, label, position, size,
            color, labelColor);
        StylePvpButton(button, color);
        return button;
    }

    static TMP_InputField CreatePvpInput(Transform parent, string name,
        string placeholder, Vector2 position, Vector2 size, int characterLimit = 3,
        TMP_InputField.ContentType contentType = TMP_InputField.ContentType.IntegerNumber)
    {
        var input = RuntimeUI.CreateInputField(parent, name, placeholder, position,
            size, characterLimit, contentType);
        var image = input.GetComponent<Image>();
        RuntimeUI.ApplyProductionSprite(image, PvpPurpleFrameResource,
            Image.Type.Sliced, false, 2f);
        if (input.textComponent != null)
            input.textComponent.color = HolUiStateColors.TextPrimary;
        var placeholderText = input.placeholder as TMP_Text;
        if (placeholderText != null)
            placeholderText.color = HolUiStateColors.WithAlpha(
                HolUiStateColors.TextSecondary, 0.82f);
        return input;
    }

    static GameObject PvpFrame(Transform parent, string name, Vector2 position,
        Vector2 size, Color accent, float fillAlpha = 0.85f, bool glow = true,
        Color? fillColor = null)
    {
        return RuntimeUI.CreateProductionFrame(parent, name, position, size,
            ResolvePvpFrameResource(accent, fillColor), 2f);
    }

    static void StylePvpButton(Button button, Color accent)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image == null) return;
        RuntimeUI.ApplyProductionSprite(image,
            ResolvePvpFrameResource(accent, null), Image.Type.Sliced, false, 2f);
        image.raycastTarget = true;
        button.targetGraphic = image;
    }

    static string ResolvePvpFrameResource(Color accent, Color? fillColor)
    {
        if (ColorDistance(accent, HolUiStateColors.Gold) < 0.35f)
            return PvpGoldFrameResource;
        if (ColorDistance(accent, HolUiStateColors.Magenta) < 0.42f ||
            (fillColor.HasValue &&
             ColorDistance(fillColor.Value, HolUiStateColors.CardPink) < 0.45f))
            return PvpMagentaFrameResource;
        if (ColorDistance(accent, HolUiStateColors.Cyan) < 0.48f ||
            ColorDistance(accent, HolUiStateColors.Blue) < 0.48f ||
            ColorDistance(accent, HolUiStateColors.KeyBlue) < 0.52f ||
            (fillColor.HasValue &&
             ColorDistance(fillColor.Value, HolUiStateColors.CardBlue) < 0.50f))
            return PvpBlueFrameResource;
        return PvpPurpleFrameResource;
    }

    static float ColorDistance(Color a, Color b)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return Mathf.Sqrt(dr * dr + dg * dg + db * db);
    }

    void BuildPanels(PvpGameController controller)
    {
        BuildMatchPanel(controller);
        ReplacePrivateRoomPanels(controller);
    }

    void BuildMatchPanel(PvpGameController controller)
    {
        // Build only the live match surface here. Private Room landing and
        // prebattle controls are built once by ReplacePrivateRoomPanels and
        // presented by their dedicated current owner.
        // Match — current production duel board: a duel-identity header, one
        // prompt banner, the guess flow in a left card with an on-screen
        // keypad, and the opponent's story — signal bubble, history, tip —
        // stacked on the right. Every coordinate below was collision-checked
        // in both visibility states (match and result) before landing.
        var matchPanel = CreatePvpPanel(transform, "PvPMatchPanel", PanelColor);

        // --- duel header: who is playing whom.
        var playerCard = PvpFrame(matchPanel.transform, "PlayerCard",
            new Vector2(-262f, 790f), new Vector2(480f, 200f),
            HolUiStateColors.Cyan, 0.96f, true, HolUiStateColors.CardBlue);
        var youCaption = RuntimeUI.CreateText(playerCard.transform, "Caption",
            L10n.Get("you"), 26, new Vector2(0f, 62f), new Vector2(440f, 44f),
            HolUiStateColors.TextSecondary);
        var myNameText = RuntimeUI.CreateText(playerCard.transform, "Name", "", 40,
            new Vector2(0f, -24f), new Vector2(440f, 92f));
        playerCard.AddComponent<PlayerNameLabel>().target = myNameText;

        // The controller writes "Opponent: {name}" here — caption included —
        // so this card carries no caption of its own.
        var oppCard = PvpFrame(matchPanel.transform, "OpponentCard",
            new Vector2(262f, 790f), new Vector2(480f, 200f),
            HolUiStateColors.Magenta, 0.96f, true, HolUiStateColors.CardPink);
        var opponentText = RuntimeUI.CreateText(oppCard.transform, "Opponent", "", 38,
            Vector2.zero, new Vector2(440f, 150f));

        var vsBadge = RuntimeUI.CreateText(matchPanel.transform, "VsBadge",
            L10n.Get("versus"), 56,
            new Vector2(0f, 790f), new Vector2(110f, 80f), HolUiStateColors.Gold);
        RuntimeUI.Localize(vsBadge, "versus");

        // --- the prompt banner: turn state during play, the result after.
        // The two texts share the slot and are mutually exclusive by content —
        // the controller blanks the turn line whenever it writes a result, and
        // the rematch handshake now reports into its own label in the guess
        // card instead of borrowing the turn line.
        var banner = PvpFrame(matchPanel.transform, "PromptBanner",
            new Vector2(0f, 555f), new Vector2(900f, 200f),
            HolUiStateColors.Gold, 0.9f, true, HolUiStateColors.SurfaceElevated);
        var roundText = RuntimeUI.CreateText(banner.transform, "Round", "", 24,
            new Vector2(0f, 70f), new Vector2(840f, 36f), HolUiStateColors.TextSecondary);
        var turnText = RuntimeUI.CreateText(banner.transform, "Turn", "", 40,
            new Vector2(0f, -22f), new Vector2(840f, 130f));
        var resultText = RuntimeUI.CreateText(banner.transform, "Result", "", 36,
            Vector2.zero, new Vector2(840f, 190f));

        // --- left card: the guess flow. Rematch has one separately owned input
        // in the approved result overlay; this keypad edits only the live guess.
        var guessCard = PvpFrame(matchPanel.transform, "GuessCard",
            new Vector2(-255f, -130f), new Vector2(530f, 900f),
            HolUiStateColors.Blue, 0.94f, true, HolUiStateColors.Surface);
        var guessCaption = RuntimeUI.CreateText(guessCard.transform, "Caption",
            L10n.Get("hud_current_number"), 26, new Vector2(0f, 410f), new Vector2(440f, 36f),
            HolUiStateColors.TextSecondary);
        var guessInput = CreatePvpInput(guessCard.transform, "GuessInput",
            L10n.Get("number_placeholder"), new Vector2(0f, 315f),
            new Vector2(440f, 120f));
        StyleNumberDisplay(guessInput);

        System.Action<string> tapKey = key =>
        {
            string text = guessInput.text ?? "";
            if (key == "C") text = "";
            else if (key == "<") text = text.Length > 0 ? text.Substring(0, text.Length - 1) : text;
            else if (text.Length < 3) text += key;
            guessInput.text = text;
        };
        var keypadRoot = RuntimeUI.CreateObject("Keypad", guessCard.transform);
        RuntimeUI.Stretch(keypadRoot);
        string[] keypadKeys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "<" };
        for (int i = 0; i < keypadKeys.Length; i++)
        {
            string key = keypadKeys[i];
            string keyLabel = key == "<" ? "←" : key;
            var keyBtn = CreatePvpButton(keypadRoot.transform, "Key" + keyLabel, keyLabel,
                new Vector2((i % 3 - 1) * 152f, 180f - (i / 3) * 118f),
                new Vector2(142f, 104f), HolUiStateColors.KeyBlue);
            StylePvpButton(keyBtn, HolUiStateColors.KeyBlue);
            var keyText = keyBtn.GetComponentInChildren<TMP_Text>();
            if (keyText != null) keyText.fontSize = 44;
            keyBtn.onClick.AddListener(() => tapKey(key));
        }

        // Gold marks the primary action, so
        // the Lock takes the cyan seam. Its label is state-driven — it becomes
        // a prompt once the range is down to a few candidates — so the
        // controller sets the text rather than a LocalizedText component.
        var lockBtn = CreatePvpButton(guessCard.transform, "LockButton",
            L10n.Get("lock"), new Vector2(0f, -290f), new Vector2(440f, 80f),
            HolUiStateColors.Cyan, DarkLabel);
        StylePvpButton(lockBtn, HolUiStateColors.Cyan);
        var lockLabel = lockBtn.GetComponentInChildren<TMP_Text>();
        lockLabel.fontSize = 26;

        var guessBtn = CreatePvpButton(guessCard.transform, "SubmitGuessButton",
            L10n.Get("pvp_guess"), new Vector2(0f, -395f), new Vector2(460f, 100f),
            HolUiStateColors.Gold, DarkLabel);

        // --- right column: the opponent's story.
        var bubbleCard = PvpFrame(matchPanel.transform, "SignalBubble",
            new Vector2(285f, 245f), new Vector2(470f, 170f),
            HolUiStateColors.Magenta, 0.7f, true, HolUiStateColors.SurfaceElevated);
        var signalFeed = RuntimeUI.CreateText(bubbleCard.transform, "SignalFeed", "", 30,
            Vector2.zero, new Vector2(420f, 140f));

        var historyCard = PvpFrame(matchPanel.transform, "HistoryCard",
            new Vector2(285f, -35f), new Vector2(470f, 320f),
            HolUiStateColors.Cyan, 0.7f, true, HolUiStateColors.Surface);
        var historyCaption = RuntimeUI.CreateText(historyCard.transform, "Caption",
            L10n.Get("hud_history"), 26, new Vector2(0f, 120f), new Vector2(420f, 40f),
            HolUiStateColors.TextSecondary);
        // Latest guess on top, the rail of earlier ones underneath. The rail
        // watches the latest-guess label rather than the controller, so it
        // needs no controller surface at all.
        var historyText = RuntimeUI.CreateText(historyCard.transform, "History", "", 32,
            new Vector2(0f, 42f), new Vector2(420f, 84f));
        var historyRailText = RuntimeUI.CreateText(historyCard.transform, "HistoryRail", "", 22,
            new Vector2(0f, -72f), new Vector2(420f, 128f), HolUiStateColors.TextSecondary);

        // How far the player has narrowed the opponent's number. Solo play has
        // always shown this; PvP never did.
        var tipCard = PvpFrame(matchPanel.transform, "TipCard",
            new Vector2(285f, -390f), new Vector2(470f, 320f),
            HolUiStateColors.Gold, 0.7f, true, HolUiStateColors.Surface);
        var tipCaption = RuntimeUI.CreateText(tipCard.transform, "Caption",
            L10n.Get("hud_tip"), 26, new Vector2(0f, 120f), new Vector2(420f, 40f),
            HolUiStateColors.TextSecondary);
        var rangeText = RuntimeUI.CreateText(tipCard.transform, "Range", "", 30,
            new Vector2(0f, -30f), new Vector2(420f, 220f), HolUiStateColors.Cyan);

        // --- bottom strip: the six Signals, both during play and on the result.
        var signalsRoot = RuntimeUI.CreateObject("Signals", matchPanel.transform);
        RuntimeUI.Stretch(signalsRoot);
        var signalButtons = new Button[Signals.Count];
        for (int i = 0; i < Signals.Count; i++)
        {
            float x = (i % 3 - 1) * 350f;
            float y = i < 3 ? -640f : -716f;
            var signalBtn = CreatePvpButton(signalsRoot.transform, "Signal" + i,
                Signals.Text(i), new Vector2(x, y), new Vector2(330f, 64f),
                HolUiStateColors.SurfaceElevated);
            var signalLabel = signalBtn.GetComponentInChildren<TMP_Text>();
            if (signalLabel != null) signalLabel.fontSize = 24;

            // The drawn signal icons ship in Resources so no scene wiring is
            // needed; a missing or unimportable icon leaves the text button
            // exactly as it was. An EditMode test holds the load path green.
            var icon = Resources.Load<Sprite>("design/" + Signals.Key(i));
            if (icon != null)
            {
                var iconGo = RuntimeUI.CreateObject("Icon", signalBtn.transform);
                RuntimeUI.Center(iconGo, new Vector2(-135f, 0f), new Vector2(40f, 40f));
                var iconImage = iconGo.AddComponent<Image>();
                iconImage.sprite = icon;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }
            RuntimeUI.Localize(signalBtn, Signals.Key(i));
            signalButtons[i] = signalBtn;
        }

        var leaveBtn = CreatePvpButton(matchPanel.transform, "LeaveButton",
            L10n.Get("pvp_leave"), new Vector2(0f, -830f), new Vector2(300f, 72f), Neutral);

        RuntimeUI.LocalizePlaceholder(guessInput, "number_placeholder");
        RuntimeUI.Localize(guessBtn, "pvp_guess");
        RuntimeUI.Localize(leaveBtn, "pvp_leave");
        RuntimeUI.Localize(youCaption, "you");
        RuntimeUI.Localize(guessCaption, "hud_current_number");
        RuntimeUI.Localize(historyCaption, "hud_history");
        RuntimeUI.Localize(tipCaption, "hud_tip");

        // Wire match state only. Private Room fields are assigned exactly once
        // by ReplacePrivateRoomPanels below.
        controller.matchPanel = matchPanel;
        controller.guessInput = guessInput;
        controller.opponentNameText = opponentText;
        controller.turnText = turnText;
        controller.roundText = roundText;
        controller.historyText = historyText;

        // The controller records typed, server-accepted events. Localized text
        // is output only and never participates in event identity.
        var rail = historyCard.AddComponent<GuessHistoryRail>();
        rail.source = controller.historyText;
        rail.target = historyRailText;
        controller.historyRail = rail;
        controller.resultText = resultText;
        controller.rangeText = rangeText;
        controller.signalFeedText = signalFeed;
        controller.lockButton = lockBtn.gameObject;
        controller.lockButtonLabel = lockLabel;
        controller.signalsRoot = signalsRoot;
        controller.guessButton = guessBtn.gameObject;
        controller.keypadRoot = keypadRoot;
        controller.leaveButton = leaveBtn.gameObject;

        // Match button hooks.
        guessBtn.onClick.AddListener(controller.OnSubmitGuessPressed);
        lockBtn.onClick.AddListener(controller.OnLockTogglePressed);
        leaveBtn.onClick.AddListener(controller.OnLeaveMatchPressed);

        for (int i = 0; i < signalButtons.Length; i++)
        {
            int signalId = i; // capture per iteration, not the shared loop variable
            signalButtons[i].onClick.AddListener(() => controller.OnSignalPressed(signalId));
        }

        BuildResultOverlay(controller, matchPanel);
        BuildTerminalOverlay(controller, matchPanel);

        // Soft-keyboard Done submits the live guess flow.
        guessInput.onSubmit.AddListener(_ => controller.OnSubmitGuessPressed());

        // Match starts hidden; OpenPvpMenu is owned by the current Private Room flow.
        lockBtn.gameObject.SetActive(false);
        signalsRoot.SetActive(false);
        matchPanel.SetActive(false);
    }

    static Image AddSprite(Transform parent, string name, string resource,
        Vector2 position, Vector2 size, float alpha = 1f)
    {
        var sprite = Resources.Load<Sprite>(resource);
        if (sprite == null) return null;
        var go = RuntimeUI.CreateObject(name, parent);
        RuntimeUI.Center(go, position, size);
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = new Color(1f, 1f, 1f, alpha);
        image.raycastTarget = false;
        return image;
    }

    static TextMeshProUGUI AddLocalizedText(Transform parent, string name,
        string key, int fontSize, Vector2 position, Vector2 size, Color color)
    {
        var text = RuntimeUI.CreateText(parent, name, L10n.Get(key), fontSize,
            position, size, color);
        RuntimeUI.Localize(text, key);
        return text;
    }

    static void AddRoomTip(Transform parent, Vector2 position)
    {
        var card = PvpFrame(parent, "TipCard", position,
            new Vector2(860f, 190f), HolUiStateColors.Gold, 0.82f, true,
            HolUiStateColors.Surface);
        AddSprite(card.transform, "TipIcon", "design/signal_luck",
            new Vector2(-330f, 0f), new Vector2(72f, 72f));
        AddLocalizedText(card.transform, "TipText", "private_room_tip", 26,
            new Vector2(90f, 0f), new Vector2(620f, 140f),
            HolUiStateColors.TextPrimary);
    }

    void BuildResultOverlay(PvpGameController controller, GameObject matchPanel)
    {
        var root = RuntimeUI.CreateObject("ResultVisualRoot", matchPanel.transform);
        RuntimeUI.Stretch(root);
        var background = root.AddComponent<Image>();
        background.sprite = RuntimeUI.LoadProductionSprite("phase2a/hol_neon_reference_bg_r3");
        background.color = Color.white;
        background.raycastTarget = true;

        AddLocalizedText(root.transform, "PageTitle", "result_page_title", 28,
            new Vector2(-345f, 825f), new Vector2(330f, 62f),
            HolUiStateColors.TextPrimary);
        AddSprite(root.transform, "Logo", "reference/hol_logo_exact",
            new Vector2(0f, 790f), new Vector2(330f, 150f));

        var chip = PvpFrame(root.transform, "PlayerChip",
            new Vector2(350f, 825f), new Vector2(310f, 92f),
            HolUiStateColors.Cyan, 0.84f, true, HolUiStateColors.Surface);
        RuntimeUI.ClampToSafeArea((RectTransform)chip.transform,
            new Vector2(310f, 92f), new Vector2(350f, 825f));
        AddSprite(chip.transform, "Avatar", "reference/player_cyan_exact",
            new Vector2(-105f, 0f), new Vector2(70f, 70f));
        var chipText = RuntimeUI.CreateText(chip.transform, "Text", "", 20,
            new Vector2(35f, 0f), new Vector2(190f, 64f));

        var confettiGo = RuntimeUI.CreateObject(
            "ResultConfettiLayer", root.transform);
        RuntimeUI.Center(confettiGo, new Vector2(0f, 250f),
            new Vector2(10f, 10f));
        var confetti = confettiGo.AddComponent<ConfettiBurst>();
        confetti.pieces = 48;
        confetti.secondaryPieces = 16;
        confetti.secondaryDelay = 0.12f;
        confetti.force = 720f;
        confetti.gravity = 1150f;
        confetti.lifetime = 1.5f;
        confetti.radial = true;

        var pop = RuntimeUI.CreateObject("ResultPopTarget", root.transform);
        RuntimeUI.Center(pop, new Vector2(0f, 260f),
            new Vector2(1000f, 900f));
        RuntimeUI.ClampToSafeArea((RectTransform)pop.transform,
            new Vector2(1000f, 900f), new Vector2(0f, 260f));
        confetti.popTarget = (RectTransform)pop.transform;

        var title = RuntimeUI.CreateText(pop.transform, "ResultTitle", "", 78,
            new Vector2(0f, 330f), new Vector2(900f, 140f),
            HolUiStateColors.Gold);
        title.enableAutoSizing = true;
        title.fontSizeMin = 42f;
        title.fontSizeMax = 78f;

        var hero = PvpFrame(pop.transform, "ResultHeroCard",
            new Vector2(0f, -25f), new Vector2(940f, 560f),
            HolUiStateColors.Cyan, 0.93f, true, HolUiStateColors.Surface);
        AddSprite(hero.transform, "WinnerBoy", "reference/player_cyan_exact",
            new Vector2(-290f, -15f), new Vector2(390f, 430f));
        var trophy = AddSprite(hero.transform, "Trophy",
            "reference/board_trophy_exact",
            new Vector2(-80f, -45f), new Vector2(250f, 300f));

        var attempts = PvpFrame(hero.transform, "AttemptsBoard",
            new Vector2(255f, 35f), new Vector2(390f, 430f),
            HolUiStateColors.Magenta, 0.72f, false, HolUiStateColors.Surface);
        AddLocalizedText(attempts.transform, "Heading", "result_attempts", 24,
            new Vector2(0f, 175f), new Vector2(350f, 50f),
            HolUiStateColors.TextPrimary);

        var playerColumn = PvpFrame(attempts.transform, "PlayerAttempts",
            new Vector2(-92f, 0f), new Vector2(170f, 290f),
            HolUiStateColors.Cyan, 0.78f, false, HolUiStateColors.CardBlue);
        AddLocalizedText(playerColumn.transform, "Role", "you", 20,
            new Vector2(0f, 105f), new Vector2(145f, 38f),
            HolUiStateColors.TextPrimary);
        var playerAttempts = RuntimeUI.CreateText(playerColumn.transform,
            "Value", "0", 76, new Vector2(0f, 10f),
            new Vector2(145f, 110f), HolUiStateColors.Cyan);
        AddLocalizedText(playerColumn.transform, "Unit",
            "result_attempts_short", 18, new Vector2(0f, -91f),
            new Vector2(145f, 38f), HolUiStateColors.Cyan);

        var opponentColumn = PvpFrame(attempts.transform,
            "OpponentAttempts", new Vector2(92f, 0f),
            new Vector2(170f, 290f), HolUiStateColors.Magenta, 0.78f,
            false, HolUiStateColors.CardPink);
        AddLocalizedText(opponentColumn.transform, "Role",
            "prebattle_opponent", 18, new Vector2(0f, 105f),
            new Vector2(155f, 38f), HolUiStateColors.TextPrimary);
        var opponentAttempts = RuntimeUI.CreateText(opponentColumn.transform,
            "Value", "0", 76, new Vector2(0f, 10f),
            new Vector2(145f, 110f), HolUiStateColors.Magenta);
        AddLocalizedText(opponentColumn.transform, "Unit",
            "result_attempts_short", 18, new Vector2(0f, -91f),
            new Vector2(145f, 38f), HolUiStateColors.Magenta);
        AddSprite(opponentColumn.transform, "Girl",
            "reference/char_girl_exact", new Vector2(42f, -95f),
            new Vector2(80f, 80f));

        var revealed = RuntimeUI.CreateText(attempts.transform,
            "RevealedNumber", "", 22, new Vector2(0f, -185f),
            new Vector2(350f, 52f), HolUiStateColors.TextPrimary);

        var rematchCard = PvpFrame(root.transform, "RematchCard",
            new Vector2(0f, -365f), new Vector2(850f, 230f),
            HolUiStateColors.Magenta, 0.88f, true, HolUiStateColors.Surface);
        RuntimeUI.ClampToSafeArea((RectTransform)rematchCard.transform,
            new Vector2(850f, 230f), new Vector2(0f, -365f));
        AddLocalizedText(rematchCard.transform, "Heading",
            "result_rematch_heading", 24, new Vector2(0f, 82f),
            new Vector2(780f, 42f), HolUiStateColors.TextPrimary);
        var rematchSecret = CreatePvpInput(rematchCard.transform,
            "RematchSecret", L10n.Get("rematch_prompt"),
            new Vector2(0f, 27f), new Vector2(760f, 64f));
        RuntimeUI.LocalizePlaceholder(rematchSecret, "rematch_prompt");
        var rematch = CreatePvpButton(rematchCard.transform,
            "ResultConfirmRematchButton", L10n.Get("rematch"),
            new Vector2(-205f, -62f), new Vector2(370f, 72f),
            HolUiStateColors.Gold, DarkLabel);
        RuntimeUI.Localize(rematch, "rematch");
        var exit = CreatePvpButton(rematchCard.transform,
            "ResultExitButton", L10n.Get("result_exit"),
            new Vector2(205f, -62f), new Vector2(370f, 72f),
            HolUiStateColors.Cyan, DarkLabel);
        RuntimeUI.Localize(exit, "result_exit");
        var rematchStatus = RuntimeUI.CreateText(root.transform,
            "ResultRematchStatus", "", 20, new Vector2(0f, -505f),
            new Vector2(780f, 42f), HolUiStateColors.TextSecondary);

        var reactionCard = PvpFrame(root.transform, "ReactionCard",
            new Vector2(0f, -690f), new Vector2(760f, 310f),
            HolUiStateColors.Magenta, 0.82f, true, HolUiStateColors.Surface);
        RuntimeUI.ClampToSafeArea((RectTransform)reactionCard.transform,
            new Vector2(760f, 310f), new Vector2(0f, -690f));
        AddLocalizedText(reactionCard.transform, "Heading",
            "result_reactions", 23, new Vector2(0f, 125f),
            new Vector2(700f, 40f), HolUiStateColors.TextPrimary);
        var resultSignalFeed = RuntimeUI.CreateText(reactionCard.transform,
            "SignalFeed", "", 17, new Vector2(0f, 88f),
            new Vector2(680f, 30f), HolUiStateColors.TextSecondary);
        var resultSignals = RuntimeUI.CreateObject(
            "ResultSignals", reactionCard.transform);
        RuntimeUI.Stretch(resultSignals);
        for (int i = 0; i < Signals.Count; i++)
        {
            int signalId = i;
            float x = (i % 3 - 1) * 235f;
            float y = i < 3 ? 35f : -55f;
            var signal = CreatePvpButton(resultSignals.transform,
                "ResultSignal" + i, Signals.Text(i), new Vector2(x, y),
                new Vector2(220f, 76f), HolUiStateColors.SurfaceElevated);
            var label = signal.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.fontSize = 18f;
                label.enableAutoSizing = true;
                label.fontSizeMin = 13f;
                label.fontSizeMax = 18f;
            }
            var icon = Resources.Load<Sprite>("design/" + Signals.Key(i));
            if (icon != null)
            {
                var iconImage = AddSprite(signal.transform, "Icon",
                    "design/" + Signals.Key(i), new Vector2(-78f, 0f),
                    new Vector2(34f, 34f));
                if (iconImage != null) iconImage.raycastTarget = false;
            }
            RuntimeUI.Localize(signal, Signals.Key(i));
            signal.onClick.AddListener(
                () => controller.OnSignalPressed(signalId));
        }

        AddSprite(root.transform, "MascotSix", "reference/mascot_6_exact",
            new Vector2(-455f, -780f), new Vector2(170f, 185f));
        AddSprite(root.transform, "MascotSeven", "reference/mascot_7_exact",
            new Vector2(455f, -780f), new Vector2(170f, 185f));

        var presentation = root.AddComponent<PvpResultPresentation>();
        presentation.titleText = title;
        presentation.playerAttemptsText = playerAttempts;
        presentation.opponentAttemptsText = opponentAttempts;
        presentation.revealedNumberText = revealed;
        presentation.playerChipText = chipText;
        presentation.trophy = trophy == null ? null : trophy.gameObject;

        controller.resultPresentation = presentation;
        controller.resultSignalsRoot = resultSignals;
        controller.resultSignalFeedText = resultSignalFeed;
        controller.rematchButton = rematch.gameObject;
        controller.rematchSecretInput = rematchSecret;
        controller.rematchStatusText = rematchStatus;
        controller.resultExitButton = exit.gameObject;
        controller.winConfetti = confetti;

        rematch.onClick.AddListener(controller.OnRematchPressed);
        rematchSecret.onSubmit.AddListener(_ => controller.OnRematchPressed());
        exit.onClick.AddListener(controller.OnLeaveMatchPressed);

        rematch.gameObject.SetActive(false);
        rematchSecret.gameObject.SetActive(false);
        resultSignals.SetActive(false);
        root.SetActive(false);
    }

    void BuildTerminalOverlay(PvpGameController controller,
        GameObject matchPanel)
    {
        var root = RuntimeUI.CreateObject("PvpTerminalRoot", matchPanel.transform);
        RuntimeUI.Stretch(root);
        var background = root.AddComponent<Image>();
        background.sprite = RuntimeUI.LoadProductionSprite("phase2a/hol_neon_reference_bg_r3");
        background.color = Color.white;
        background.raycastTarget = true;

        var card = PvpFrame(root.transform, "TerminalCard",
            Vector2.zero, new Vector2(840f, 540f), HolUiStateColors.Magenta,
            0.92f, true, HolUiStateColors.Surface);
        var title = RuntimeUI.CreateText(card.transform, "Title", "", 52,
            new Vector2(0f, 145f), new Vector2(760f, 110f),
            HolUiStateColors.Gold);
        var message = RuntimeUI.CreateText(card.transform, "Message", "", 30,
            new Vector2(0f, 20f), new Vector2(700f, 150f),
            HolUiStateColors.TextPrimary);
        var exit = CreatePvpButton(card.transform, "TerminalExitButton",
            L10n.Get("result_exit"), new Vector2(0f, -155f),
            new Vector2(420f, 86f), HolUiStateColors.Cyan, DarkLabel);
        RuntimeUI.Localize(exit, "result_exit");

        var presentation = gameObject.AddComponent<PvpTerminalPresentation>();
        presentation.terminalRoot = root;
        presentation.titleText = title;
        presentation.messageText = message;
        presentation.resultStatusText = controller.rematchStatusText;
        presentation.terminalExitButton = exit.gameObject;
        presentation.resultExitButton = controller.resultExitButton;
        controller.terminalPresentation = presentation;

        exit.onClick.AddListener(controller.OnLeaveMatchPressed);
        root.SetActive(false);
    }

    sealed class PrebattleParts
    {
        public GameObject panel;
        public GameObject entryRoot;
        public GameObject waitingRoot;
        public TMP_InputField secret;
        public TMP_InputField codeInput;
        public GameObject confirm;
        public TMP_Text codeText;
        public Button copy;
        public TMP_Text entryStatus;
        public TMP_Text opponentStatus;
        public TMP_Text status;
        public Button back;
    }

    PrebattleParts BuildPrebattlePanel(string name, bool createMode)
    {
        var parts = new PrebattleParts();
        parts.panel = BuildPortraitPanel(transform, name);
        var root = parts.panel.transform;

        AddLocalizedText(root, "PageTitle", "prebattle_title", 30,
            new Vector2(-245f, 820f), new Vector2(430f, 62f),
            HolUiStateColors.TextPrimary);
        AddSprite(root, "Logo", "reference/hol_logo_exact",
            new Vector2(0f, 625f), new Vector2(440f, 210f));
        AddLocalizedText(root, "YourLabel", "prebattle_you", 24,
            new Vector2(-280f, 455f), new Vector2(360f, 52f),
            HolUiStateColors.TextPrimary);
        AddLocalizedText(root, "OpponentLabel", "prebattle_opponent", 24,
            new Vector2(280f, 455f), new Vector2(360f, 52f),
            HolUiStateColors.TextPrimary);

        var left = PvpFrame(root, "YouCard",
            new Vector2(-275f, 235f), new Vector2(430f, 430f),
            HolUiStateColors.Cyan, 0.90f, true, HolUiStateColors.CardBlue);
        AddSprite(left.transform, "Boy", "reference/player_cyan_exact",
            new Vector2(0f, 20f), new Vector2(330f, 320f));
        var playerName = AddLocalizedText(left.transform, "Name",
            "player_default", 28,
            new Vector2(0f, -160f), new Vector2(360f, 58f),
            HolUiStateColors.TextPrimary);
        left.AddComponent<PlayerNameLabel>().target = playerName;

        var right = PvpFrame(root, "OpponentCard",
            new Vector2(275f, 235f), new Vector2(430f, 430f),
            HolUiStateColors.Magenta, 0.90f, true, HolUiStateColors.CardPink);
        AddSprite(right.transform, "Girl", "reference/char_girl_exact",
            new Vector2(0f, 20f), new Vector2(330f, 320f));
        parts.opponentStatus = AddLocalizedText(right.transform, "Status",
            "prebattle_waiting_short", 26,
            new Vector2(0f, -160f), new Vector2(360f, 58f),
            HolUiStateColors.TextPrimary);
        AddSprite(root, "VsBurst", "reference/board_vs_burst_exact",
            new Vector2(0f, 235f), new Vector2(180f, 180f));

        var rule = PvpFrame(root, "RuleCard",
            new Vector2(0f, -70f), new Vector2(900f, 190f),
            HolUiStateColors.Cyan, 0.88f, true, HolUiStateColors.Surface);
        AddLocalizedText(rule.transform, "RuleTitle", "prebattle_rule_title",
            30, new Vector2(-170f, 52f), new Vector2(430f, 52f),
            HolUiStateColors.Cyan);
        AddLocalizedText(rule.transform, "Rule", "prebattle_rule", 24,
            new Vector2(-125f, -30f), new Vector2(560f, 90f),
            HolUiStateColors.TextPrimary);
        AddSprite(rule.transform, "Rocket", "reference/board_rocket_exact",
            new Vector2(300f, 0f), new Vector2(170f, 170f));

        parts.entryRoot = RuntimeUI.CreateObject("EntryState", root);
        RuntimeUI.Stretch(parts.entryRoot);
        parts.waitingRoot = RuntimeUI.CreateObject("WaitingState", root);
        RuntimeUI.Stretch(parts.waitingRoot);

        if (createMode)
        {
            var code = PvpFrame(parts.waitingRoot.transform,
                "RoomCodeFrame", new Vector2(-190f, -285f),
                new Vector2(430f, 110f), HolUiStateColors.Magenta,
                0.82f, false, HolUiStateColors.Surface);
            AddLocalizedText(code.transform, "Caption", "pvp_enter_code",
                18, new Vector2(0f, 24f), new Vector2(390f, 34f),
                HolUiStateColors.WithAlpha(HolUiStateColors.TextPrimary, 0.72f));
            parts.codeText = RuntimeUI.CreateText(code.transform, "RoomCode",
                "-----", 44, new Vector2(0f, -20f),
                new Vector2(390f, 52f));

            parts.copy = CreatePvpButton(parts.waitingRoot.transform,
                "ShareButton", L10n.Get("private_room_share"),
                new Vector2(275f, -285f), new Vector2(300f, 96f),
                HolUiStateColors.SurfaceElevated);
            RuntimeUI.Localize(parts.copy, "private_room_share");
        }

        var waiting = PvpFrame(parts.waitingRoot.transform,
            "WaitingPlate", new Vector2(0f, createMode ? -500f : -380f),
            new Vector2(820f, 150f), HolUiStateColors.Gold, 0.86f,
            true, HolUiStateColors.Surface);
        parts.status = RuntimeUI.CreateText(waiting.transform, "Status",
            "", 30, Vector2.zero, new Vector2(760f, 100f));

        if (createMode)
        {
            parts.secret = CreatePvpInput(
                parts.entryRoot.transform, "SecretInput",
                L10n.Get("pvp_secret"), new Vector2(0f, -360f),
                new Vector2(500f, 84f));
            parts.confirm = CreatePvpButton(
                parts.entryRoot.transform, "ConfirmCreateButton",
                L10n.Get("confirm"), new Vector2(0f, -490f),
                new Vector2(420f, 82f), HolUiStateColors.Gold, DarkLabel).gameObject;
            parts.entryStatus = RuntimeUI.CreateText(
                parts.entryRoot.transform, "EntryStatus", "", 22,
                new Vector2(0f, -590f), new Vector2(700f, 60f),
                HolUiStateColors.TextSecondary);
            RuntimeUI.LocalizePlaceholder(parts.secret, "pvp_secret");
            RuntimeUI.Localize(parts.confirm.GetComponent<Button>(), "confirm");
        }
        else
        {
            parts.codeInput = CreatePvpInput(
                parts.entryRoot.transform, "CodeInput",
                L10n.Get("pvp_enter_code"), new Vector2(0f, -320f),
                new Vector2(500f, 84f), 5, TMP_InputField.ContentType.Standard);
            parts.codeInput.onValidateInput = (text, index, ch) =>
                char.ToUpperInvariant(ch);
            parts.secret = CreatePvpInput(
                parts.entryRoot.transform, "SecretInput",
                L10n.Get("pvp_secret"), new Vector2(0f, -430f),
                new Vector2(500f, 84f));
            parts.confirm = CreatePvpButton(
                parts.entryRoot.transform, "ConfirmJoinButton",
                L10n.Get("confirm"), new Vector2(0f, -550f),
                new Vector2(420f, 82f), HolUiStateColors.Gold, DarkLabel).gameObject;
            parts.entryStatus = RuntimeUI.CreateText(
                parts.entryRoot.transform, "EntryStatus", "", 22,
                new Vector2(0f, -650f), new Vector2(700f, 60f),
                HolUiStateColors.TextSecondary);
            RuntimeUI.LocalizePlaceholder(parts.codeInput, "pvp_enter_code");
            RuntimeUI.LocalizePlaceholder(parts.secret, "pvp_secret");
            RuntimeUI.Localize(parts.confirm.GetComponent<Button>(), "confirm");
        }

        parts.back = CreatePvpButton(root, "CancelButton",
            L10n.Get("cancel"), new Vector2(0f, -835f),
            new Vector2(270f, 70f), HolUiStateColors.SurfaceElevated);
        RuntimeUI.Localize(parts.back, "cancel");
        AddSprite(root, "MascotSix", "reference/mascot_6_exact",
            new Vector2(-440f, -760f), new Vector2(150f, 180f));
        AddSprite(root, "MascotSeven", "reference/mascot_7_exact",
            new Vector2(440f, -760f), new Vector2(150f, 180f));
        parts.waitingRoot.SetActive(false);
        return parts;
    }

    void ReplacePrivateRoomPanels(PvpGameController controller)
    {
        var menu = BuildPortraitPanel(transform, "PvPMenuPanel");
        var create = CreatePvpButton(menu.transform, "CreateButton",
            L10n.Get("pvp_create_room"), Vector2.zero, new Vector2(360f, 104f),
            HolUiStateColors.Cyan, DarkLabel);
        var join = CreatePvpButton(menu.transform, "JoinButton",
            L10n.Get("pvp_join_room"), Vector2.zero, new Vector2(430f, 104f),
            HolUiStateColors.Gold, DarkLabel);
        var back = CreatePvpButton(menu.transform, "BackButton",
            L10n.Get("back"), Vector2.zero, new Vector2(86f, 86f),
            HolUiStateColors.SurfaceElevated);
        RuntimeUI.Localize(create, "pvp_create_room");
        RuntimeUI.Localize(join, "pvp_join_room");
        RuntimeUI.Localize(back, "back");

        var prebattleCreate = BuildPrebattlePanel("PvPCreatePanel", true);
        var prebattleJoin = BuildPrebattlePanel("PvPJoinPanel", false);

        controller.pvpMenuPanel = menu;
        controller.createPanel = prebattleCreate.panel;
        controller.joinPanel = prebattleJoin.panel;
        controller.createSecretInput = prebattleCreate.secret;
        controller.createConfirmButton = prebattleCreate.confirm;
        controller.createEntryRoot = prebattleCreate.entryRoot;
        controller.createWaitingRoot = prebattleCreate.waitingRoot;
        controller.createEntryStatusText = prebattleCreate.entryStatus;
        controller.createOpponentStatusText =
            prebattleCreate.opponentStatus;
        controller.roomCodeText = prebattleCreate.codeText;
        controller.createStatusText = prebattleCreate.status;
        controller.createCopyButton = prebattleCreate.copy.gameObject;
        controller.joinCodeInput = prebattleJoin.codeInput;
        controller.joinSecretInput = prebattleJoin.secret;
        controller.joinConfirmButton = prebattleJoin.confirm;
        controller.joinEntryRoot = prebattleJoin.entryRoot;
        controller.joinWaitingRoot = prebattleJoin.waitingRoot;
        controller.joinEntryStatusText = prebattleJoin.entryStatus;
        controller.joinOpponentStatusText = prebattleJoin.opponentStatus;
        controller.joinStatusText = prebattleJoin.status;
        var prebattleEllipsis = prebattleCreate.status.gameObject
            .AddComponent<AnimatedEllipsis>();
        prebattleEllipsis.text = prebattleCreate.status;
        prebattleEllipsis.enabled = false;
        controller.createStatusEllipsis = prebattleEllipsis;

        create.onClick.AddListener(() => ShowOnly(controller, prebattleCreate.panel));
        join.onClick.AddListener(() => ShowOnly(controller, prebattleJoin.panel));
        back.onClick.AddListener(controller.ClosePvpMenu);
        prebattleCreate.confirm.GetComponent<Button>().onClick.AddListener(
            controller.OnCreateRoomPressed);
        prebattleCreate.copy.onClick.AddListener(controller.OnCopyInvitePressed);
        prebattleCreate.back.onClick.AddListener(controller.CancelRoomAndLeave);
        prebattleJoin.confirm.GetComponent<Button>().onClick.AddListener(
            controller.OnJoinRoomPressed);
        prebattleJoin.back.onClick.AddListener(controller.CancelRoomAndLeave);
        prebattleCreate.secret.onSubmit.AddListener(
            _ => controller.OnCreateRoomPressed());
        prebattleJoin.codeInput.onSubmit.AddListener(
            _ => controller.OnJoinRoomPressed());
        prebattleJoin.secret.onSubmit.AddListener(
            _ => controller.OnJoinRoomPressed());

        menu.SetActive(false);
        prebattleCreate.panel.SetActive(false);
        prebattleJoin.panel.SetActive(false);
    }


    static GameObject BuildPortraitPanel(Transform parent, string name)
    {
        var panel = CreatePvpPanel(parent, name, PanelColor);
        return panel;
    }

    void InjectEntryButton(PvpGameController controller)
    {
        // Find the scene's main menu canvas and drop an entry button on it.
        var mainCanvasGo = GameObject.Find("Canvas");
        if (mainCanvasGo == null)
        {
            Debug.LogWarning("PvpRuntimeUI: no 'Canvas' object found — PvP entry button not added.");
            return;
        }

        var entry = CreatePvpButton(mainCanvasGo.transform, "ButtonPvP",
            L10n.Get("pvp_duel"), new Vector2(0f, -620f), new Vector2(460f, 100f), Accent, DarkLabel);
        entry.onClick.AddListener(controller.OpenPvpMenu);
        RuntimeUI.Localize(entry, "pvp_duel");

        // Sit right after the settings button instead of as the last child,
        // so scene panels opened later render/raycast above this button.
        var settingsButton = GameObject.Find("Buttonsettings");
        if (settingsButton != null && settingsButton.transform.parent == mainCanvasGo.transform)
            entry.transform.SetSiblingIndex(settingsButton.transform.GetSiblingIndex() + 1);
    }

    // ------------------------------------------------------------ helpers

    static void ShowOnly(PvpGameController controller, GameObject panel)
    {
        controller.pvpMenuPanel.SetActive(false);
        controller.createPanel.SetActive(false);
        controller.joinPanel.SetActive(false);
        controller.matchPanel.SetActive(false);
        panel.SetActive(true);
    }

}
