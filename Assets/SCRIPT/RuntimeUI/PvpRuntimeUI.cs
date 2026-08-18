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

    // Converging Light palette (design/philosophy.md): indigo depth, cyan and
    // gold as the disciplined lights, near-white text. Gold is reserved for
    // the single most important action on each screen (primary CTA).
    static readonly Color PanelColor = ConvergingLight.WithAlpha(ConsumerTokens.Background0, 0.97f);
    static readonly Color Accent = ConsumerTokens.Gold;
    static readonly Color AccentBlue = ConsumerTokens.Cyan;
    static readonly Color Neutral = ConsumerTokens.SurfaceElevated;
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

    // CreateButton swaps in the wired design sprites and paints them white,
    // which is right for ordinary buttons but would erase the keypad's KeyBlue
    // and the Lock's cyan — identities this board depends on. These buttons
    // keep the procedural plate whatever the scene has wired.
    static void ForceProceduralButton(Button button, Color color)
    {
        var image = button.GetComponent<Image>();
        if (image == null) return;
        image.sprite = RuntimeUI.RoundedRectSprite;
        image.type = Image.Type.Sliced;
        image.color = color;
    }

    // The indigo depth gradient the rest of Converging Light sits on. Added as
    // the panel's first child so it draws behind every control placed after it,
    // which is why it is called immediately after the panel is created.
    static void NeonBackdrop(GameObject panel)
    {
        var backdrop = RuntimeUI.CreateObject("Backdrop", panel.transform);
        RuntimeUI.Stretch(backdrop);

        var image = backdrop.AddComponent<Image>();
        image.sprite = ConvergingLight.DepthGradientSprite;
        image.type = Image.Type.Simple;
        image.raycastTarget = false;
    }

    void BuildPanels(PvpGameController controller)
    {
        BuildPanelsLegacy(controller);
        ReplacePrivateRoomPanels(controller);
    }

    void BuildPanelsLegacy(PvpGameController controller)
    {
        // PvP menu: create or join.
        var menuPanel = RuntimeUI.FullscreenPanel(transform, "PvPMenuPanel", PanelColor);
        NeonBackdrop(menuPanel);
        var menuTitle = RuntimeUI.CreateText(menuPanel.transform, "Title", L10n.Get("pvp_duel"), 64,
            new Vector2(0f, 420f), new Vector2(800f, 120f));
        var createBtn = RuntimeUI.CreateButton(menuPanel.transform, "CreateButton",
            L10n.Get("pvp_create_room"), new Vector2(0f, 120f), new Vector2(460f, 100f), Accent, DarkLabel);
        var joinBtn = RuntimeUI.CreateButton(menuPanel.transform, "JoinButton",
            L10n.Get("pvp_join_room"), new Vector2(0f, -20f), new Vector2(460f, 100f), AccentBlue, DarkLabel);
        var closeBtn = RuntimeUI.CreateButton(menuPanel.transform, "CloseButton",
            L10n.Get("back"), new Vector2(0f, -200f), new Vector2(300f, 80f), Neutral);

        // Static labels follow language changes; dynamic ones (room code,
        // status lines, results) are set per-event and stay plain.
        RuntimeUI.Localize(menuTitle, "pvp_duel");
        RuntimeUI.Localize(createBtn, "pvp_create_room");
        RuntimeUI.Localize(joinBtn, "pvp_join_room");
        RuntimeUI.Localize(closeBtn, "back");

        // Create flow. The room code is the one thing on this screen a player
        // has to read aloud or copy accurately, so it gets the frame, the glow
        // and a caption naming it; everything else stays quieter than it.
        var createPanel = RuntimeUI.FullscreenPanel(transform, "PvPCreatePanel", PanelColor);
        NeonBackdrop(createPanel);
        var createTitle = RuntimeUI.CreateText(createPanel.transform, "Title", L10n.Get("pvp_create_room"), 48,
            new Vector2(0f, 560f), new Vector2(800f, 100f));
        var createSecret = RuntimeUI.CreateInputField(createPanel.transform, "SecretInput",
            L10n.Get("pvp_secret"), new Vector2(0f, 350f), new Vector2(460f, 90f));
        var createGo = RuntimeUI.CreateButton(createPanel.transform, "ConfirmCreateButton",
            L10n.Get("confirm"), new Vector2(0f, 225f), new Vector2(460f, 90f), Accent, DarkLabel);

        var codeFrame = NeonFrame.Frame(createPanel.transform, "RoomCodeFrame",
            new Vector2(0f, 0f), new Vector2(760f, 280f), ConsumerTokens.Cyan,
            0.9f, true, ConsumerTokens.Surface);
        var codeCaption = RuntimeUI.CreateText(codeFrame.transform, "CodeCaption",
            L10n.Get("pvp_enter_code"), 28, new Vector2(0f, 90f), new Vector2(700f, 50f),
            ConvergingLight.WithAlpha(ConvergingLight.NearWhite, 0.7f));
        var codeText = RuntimeUI.CreateText(codeFrame.transform, "RoomCode", "-----", 96,
            new Vector2(0f, -25f), new Vector2(700f, 140f));

        var copyBtn = RuntimeUI.CreateButton(createPanel.transform, "CopyButton",
            L10n.Get("pvp_copy"), new Vector2(0f, -215f), new Vector2(460f, 90f), AccentBlue, DarkLabel);

        // The wait for a challenger is the longest pause in the game. Framing
        // the status keeps it looking like a live thing rather than a caption
        // stranded on a background.
        var statusFrame = NeonFrame.Frame(createPanel.transform, "StatusFrame",
            new Vector2(0f, -380f), new Vector2(860f, 150f),
            ConvergingLight.WithAlpha(ConsumerTokens.Magenta, 0.5f), 0.7f,
            true, ConsumerTokens.Surface);
        var createStatus = RuntimeUI.CreateText(statusFrame.transform, "Status", "", 32,
            Vector2.zero, new Vector2(800f, 130f));

        var createBack = RuntimeUI.CreateButton(createPanel.transform, "BackButton",
            L10n.Get("back"), new Vector2(0f, -580f), new Vector2(300f, 80f), Neutral);

        RuntimeUI.Localize(createTitle, "pvp_create_room");
        RuntimeUI.Localize(codeCaption, "pvp_enter_code");
        RuntimeUI.LocalizePlaceholder(createSecret, "pvp_secret");
        RuntimeUI.Localize(createGo, "confirm");
        RuntimeUI.Localize(copyBtn, "pvp_copy");
        RuntimeUI.Localize(createBack, "back");

        // Join flow.
        var joinPanel = RuntimeUI.FullscreenPanel(transform, "PvPJoinPanel", PanelColor);
        NeonBackdrop(joinPanel);
        NeonFrame.Frame(joinPanel.transform, "JoinCard",
            new Vector2(0f, 110f), new Vector2(640f, 420f), ConsumerTokens.Blue,
            0.9f, true, ConsumerTokens.Surface);
        var joinTitle = RuntimeUI.CreateText(joinPanel.transform, "Title", L10n.Get("pvp_join_room"), 48,
            new Vector2(0f, 420f), new Vector2(800f, 100f));
        var joinCode = RuntimeUI.CreateInputField(joinPanel.transform, "CodeInput",
            L10n.Get("pvp_enter_code"), new Vector2(0f, 240f), new Vector2(460f, 90f), 5,
            TMP_InputField.ContentType.Standard);
        // Codes are shown and shared in caps ("-----" display, invite text);
        // typing lowercase looked like a different code. Backends already
        // normalize case — this is purely visual consistency.
        joinCode.onValidateInput = (text, index, ch) => char.ToUpperInvariant(ch);
        var joinSecret = RuntimeUI.CreateInputField(joinPanel.transform, "SecretInput",
            L10n.Get("pvp_secret"), new Vector2(0f, 110f), new Vector2(460f, 90f));
        var joinGo = RuntimeUI.CreateButton(joinPanel.transform, "ConfirmJoinButton",
            L10n.Get("confirm"), new Vector2(0f, -20f), new Vector2(460f, 90f), AccentBlue, DarkLabel);
        var joinStatus = RuntimeUI.CreateText(joinPanel.transform, "Status", "", 32,
            new Vector2(0f, -200f), new Vector2(900f, 120f));
        var joinBack = RuntimeUI.CreateButton(joinPanel.transform, "BackButton",
            L10n.Get("back"), new Vector2(0f, -380f), new Vector2(300f, 80f), Neutral);

        RuntimeUI.Localize(joinTitle, "pvp_join_room");
        RuntimeUI.LocalizePlaceholder(joinCode, "pvp_enter_code");
        RuntimeUI.LocalizePlaceholder(joinSecret, "pvp_secret");
        RuntimeUI.Localize(joinGo, "confirm");
        RuntimeUI.Localize(joinBack, "back");

        // Match — laid out to the "HOL Consumer First" board the design tokens
        // and the newdesign asset library describe: a duel-identity header, one
        // prompt banner, the guess flow in a left card with an on-screen
        // keypad, and the opponent's story — signal bubble, history, tip —
        // stacked on the right. Every coordinate below was collision-checked
        // in both visibility states (match and result) before landing.
        var matchPanel = RuntimeUI.FullscreenPanel(transform, "PvPMatchPanel", PanelColor);
        NeonBackdrop(matchPanel);

        // --- duel header: who is playing whom.
        var playerCard = NeonFrame.Frame(matchPanel.transform, "PlayerCard",
            new Vector2(-262f, 790f), new Vector2(480f, 200f),
            ConsumerTokens.Cyan, 0.96f, true, ConsumerTokens.CardBlue);
        var youCaption = RuntimeUI.CreateText(playerCard.transform, "Caption",
            L10n.Get("you"), 26, new Vector2(0f, 62f), new Vector2(440f, 44f),
            ConsumerTokens.TextSecondary);
        var myNameText = RuntimeUI.CreateText(playerCard.transform, "Name", "", 40,
            new Vector2(0f, -24f), new Vector2(440f, 92f));
        playerCard.AddComponent<PlayerNameLabel>().target = myNameText;

        // The controller writes "Opponent: {name}" here — caption included —
        // so this card carries no caption of its own.
        var oppCard = NeonFrame.Frame(matchPanel.transform, "OpponentCard",
            new Vector2(262f, 790f), new Vector2(480f, 200f),
            ConsumerTokens.Magenta, 0.96f, true, ConsumerTokens.CardPink);
        var opponentText = RuntimeUI.CreateText(oppCard.transform, "Opponent", "", 38,
            Vector2.zero, new Vector2(440f, 150f));

        var vsBadge = RuntimeUI.CreateText(matchPanel.transform, "VsBadge",
            L10n.Get("versus"), 56,
            new Vector2(0f, 790f), new Vector2(110f, 80f), ConsumerTokens.Gold);
        RuntimeUI.Localize(vsBadge, "versus");

        // --- the prompt banner: turn state during play, the result after.
        // The two texts share the slot and are mutually exclusive by content —
        // the controller blanks the turn line whenever it writes a result, and
        // the rematch handshake now reports into its own label in the guess
        // card instead of borrowing the turn line.
        var banner = NeonFrame.Frame(matchPanel.transform, "PromptBanner",
            new Vector2(0f, 555f), new Vector2(900f, 200f),
            ConsumerTokens.Gold, 0.9f, true, ConsumerTokens.SurfaceElevated);
        var roundText = RuntimeUI.CreateText(banner.transform, "Round", "", 24,
            new Vector2(0f, 70f), new Vector2(840f, 36f), ConsumerTokens.TextSecondary);
        var turnText = RuntimeUI.CreateText(banner.transform, "Turn", "", 40,
            new Vector2(0f, -22f), new Vector2(840f, 130f));
        var resultText = RuntimeUI.CreateText(banner.transform, "Result", "", 36,
            Vector2.zero, new Vector2(840f, 190f));

        // --- left card: the guess flow. The keypad edits whichever input is
        // live (guess during play, new secret on the rematch offer), so the
        // soft keyboard never has to cover the board.
        var guessCard = NeonFrame.Frame(matchPanel.transform, "GuessCard",
            new Vector2(-255f, -130f), new Vector2(530f, 900f),
            ConsumerTokens.Blue, 0.94f, true, ConsumerTokens.Surface);
        var guessCaption = RuntimeUI.CreateText(guessCard.transform, "Caption",
            L10n.Get("hud_current_number"), 26, new Vector2(0f, 410f), new Vector2(440f, 36f),
            ConsumerTokens.TextSecondary);
        var guessInput = RuntimeUI.CreateInputField(guessCard.transform, "GuessInput",
            L10n.Get("number_placeholder"), new Vector2(0f, 315f),
            new Vector2(440f, 120f));
        StyleNumberDisplay(guessInput);

        // Offered on the result screen, in the slots the guess controls vacate.
        var rematchSecret = RuntimeUI.CreateInputField(guessCard.transform, "RematchSecret",
            L10n.Get("number_placeholder"), new Vector2(0f, 315f),
            new Vector2(440f, 120f));
        StyleNumberDisplay(rematchSecret);
        RuntimeUI.LocalizePlaceholder(rematchSecret, "rematch_prompt");

        System.Action<string> tapKey = key =>
        {
            var target = rematchSecret.gameObject.activeInHierarchy ? rematchSecret : guessInput;
            string text = target.text ?? "";
            if (key == "C") text = "";
            else if (key == "<") text = text.Length > 0 ? text.Substring(0, text.Length - 1) : text;
            else if (text.Length < 3) text += key;
            target.text = text;
        };
        string[] keypadKeys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "<" };
        for (int i = 0; i < keypadKeys.Length; i++)
        {
            string key = keypadKeys[i];
            string keyLabel = key == "<" ? "←" : key;
            var keyBtn = RuntimeUI.CreateButton(guessCard.transform, "Key" + keyLabel, keyLabel,
                new Vector2((i % 3 - 1) * 152f, 180f - (i / 3) * 118f),
                new Vector2(142f, 104f), ConsumerTokens.KeyBlue);
            ForceProceduralButton(keyBtn, ConsumerTokens.KeyBlue);
            var keyText = keyBtn.GetComponentInChildren<TMP_Text>();
            if (keyText != null) keyText.fontSize = 44;
            keyBtn.onClick.AddListener(() => tapKey(key));
        }

        // Gold stays reserved for the primary action (design/philosophy.md), so
        // the Lock takes the cyan seam. Its label is state-driven — it becomes
        // a prompt once the range is down to a few candidates — so the
        // controller sets the text rather than a LocalizedText component.
        var lockBtn = RuntimeUI.CreateButton(guessCard.transform, "LockButton",
            L10n.Get("lock"), new Vector2(0f, -290f), new Vector2(440f, 80f),
            ConsumerTokens.Cyan, DarkLabel);
        ForceProceduralButton(lockBtn, ConsumerTokens.Cyan);
        var lockLabel = lockBtn.GetComponentInChildren<TMP_Text>();
        lockLabel.fontSize = 26;

        // The rematch handshake reports here; the Lock hides once the match is
        // decided, so the slot is free exactly when the status needs it.
        var rematchStatus = RuntimeUI.CreateText(guessCard.transform, "RematchStatus", "", 28,
            new Vector2(0f, -290f), new Vector2(440f, 80f), ConsumerTokens.TextSecondary);

        var guessBtn = RuntimeUI.CreateButton(guessCard.transform, "SubmitGuessButton",
            L10n.Get("pvp_guess"), new Vector2(0f, -395f), new Vector2(460f, 100f),
            ConsumerTokens.Gold, DarkLabel);
        var rematchBtn = RuntimeUI.CreateButton(guessCard.transform, "RematchConfirmButton",
            L10n.Get("rematch"), new Vector2(0f, -395f), new Vector2(460f, 100f),
            ConsumerTokens.Gold, DarkLabel);
        RuntimeUI.Localize(rematchBtn, "rematch");

        // --- right column: the opponent's story.
        var bubbleCard = NeonFrame.Frame(matchPanel.transform, "SignalBubble",
            new Vector2(285f, 245f), new Vector2(470f, 170f),
            ConsumerTokens.Magenta, 0.7f, true, ConsumerTokens.SurfaceElevated);
        var signalFeed = RuntimeUI.CreateText(bubbleCard.transform, "SignalFeed", "", 30,
            Vector2.zero, new Vector2(420f, 140f));

        var historyCard = NeonFrame.Frame(matchPanel.transform, "HistoryCard",
            new Vector2(285f, -35f), new Vector2(470f, 320f),
            ConsumerTokens.Cyan, 0.7f, true, ConsumerTokens.Surface);
        var historyCaption = RuntimeUI.CreateText(historyCard.transform, "Caption",
            L10n.Get("hud_history"), 26, new Vector2(0f, 120f), new Vector2(420f, 40f),
            ConsumerTokens.TextSecondary);
        // Latest guess on top, the rail of earlier ones underneath. The rail
        // watches the latest-guess label rather than the controller, so it
        // needs no controller surface at all.
        var historyText = RuntimeUI.CreateText(historyCard.transform, "History", "", 32,
            new Vector2(0f, 42f), new Vector2(420f, 84f));
        var historyRailText = RuntimeUI.CreateText(historyCard.transform, "HistoryRail", "", 22,
            new Vector2(0f, -72f), new Vector2(420f, 128f), ConsumerTokens.TextSecondary);

        // How far the player has narrowed the opponent's number. Solo play has
        // always shown this; PvP never did.
        var tipCard = NeonFrame.Frame(matchPanel.transform, "TipCard",
            new Vector2(285f, -390f), new Vector2(470f, 320f),
            ConsumerTokens.Gold, 0.7f, true, ConsumerTokens.Surface);
        var tipCaption = RuntimeUI.CreateText(tipCard.transform, "Caption",
            L10n.Get("hud_tip"), 26, new Vector2(0f, 120f), new Vector2(420f, 40f),
            ConsumerTokens.TextSecondary);
        var rangeText = RuntimeUI.CreateText(tipCard.transform, "Range", "", 30,
            new Vector2(0f, -30f), new Vector2(420f, 220f), ConsumerTokens.Cyan);

        // --- bottom strip: the six Signals, both during play and on the result.
        var signalsRoot = RuntimeUI.CreateObject("Signals", matchPanel.transform);
        RuntimeUI.Stretch(signalsRoot);
        var signalButtons = new Button[Signals.Count];
        for (int i = 0; i < Signals.Count; i++)
        {
            float x = (i % 3 - 1) * 350f;
            float y = i < 3 ? -640f : -716f;
            var signalBtn = RuntimeUI.CreateButton(signalsRoot.transform, "Signal" + i,
                Signals.Text(i), new Vector2(x, y), new Vector2(330f, 64f),
                ConsumerTokens.SurfaceElevated);
            var signalLabel = signalBtn.GetComponentInChildren<TMP_Text>();
            if (signalLabel != null) signalLabel.fontSize = 24;

            // The drawn signal icons ship in Resources so no scene wiring is
            // needed; a missing or unimportable icon leaves the text button
            // exactly as it was. An EditMode test holds the load path green.
            var icon = Resources.Load<Sprite>("design/" + Signals.Key(i));
            if (icon != null)
            {
                var iconGo = RuntimeUI.CreateObject("Icon", signalBtn.transform);
                ConvergingLight.Center(iconGo, new Vector2(-135f, 0f), new Vector2(40f, 40f));
                var iconImage = iconGo.AddComponent<Image>();
                iconImage.sprite = icon;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }
            RuntimeUI.Localize(signalBtn, Signals.Key(i));
            signalButtons[i] = signalBtn;
        }

        var leaveBtn = RuntimeUI.CreateButton(matchPanel.transform, "LeaveButton",
            L10n.Get("pvp_leave"), new Vector2(0f, -830f), new Vector2(300f, 72f), Neutral);

        RuntimeUI.LocalizePlaceholder(guessInput, "number_placeholder");
        RuntimeUI.Localize(guessBtn, "pvp_guess");
        RuntimeUI.Localize(leaveBtn, "pvp_leave");
        RuntimeUI.Localize(youCaption, "you");
        RuntimeUI.Localize(guessCaption, "hud_current_number");
        RuntimeUI.Localize(historyCaption, "hud_history");
        RuntimeUI.Localize(tipCaption, "hud_tip");

        // Wire the controller.
        controller.pvpMenuPanel = menuPanel;
        controller.createPanel = createPanel;
        controller.joinPanel = joinPanel;
        controller.matchPanel = matchPanel;

        controller.createSecretInput = createSecret;
        controller.roomCodeText = codeText;
        controller.createStatusText = createStatus;

        // Waiting-state dots for the create panel's status line; disabled
        // until the controller shows an animated status (SetCreateStatus).
        var statusEllipsis = controller.createStatusText.gameObject.AddComponent<AnimatedEllipsis>();
        statusEllipsis.text = controller.createStatusText;
        statusEllipsis.enabled = false;
        controller.createStatusEllipsis = statusEllipsis;
        controller.joinCodeInput = joinCode;
        controller.joinSecretInput = joinSecret;
        controller.joinStatusText = joinStatus;
        controller.guessInput = guessInput;
        controller.opponentNameText = opponentText;
        controller.turnText = turnText;
        controller.roundText = roundText;
        controller.historyText = historyText;

        // The rail watches the latest-guess label the controller writes into;
        // repaints are signature-gated, so every change is a real guess.
        var rail = historyCard.AddComponent<GuessHistoryRail>();
        rail.source = controller.historyText;
        rail.target = historyRailText;
        controller.resultText = resultText;
        controller.rangeText = rangeText;
        controller.signalFeedText = signalFeed;
        controller.lockButton = lockBtn.gameObject;
        controller.lockButtonLabel = lockLabel;
        controller.signalsRoot = signalsRoot;
        controller.guessButton = guessBtn.gameObject;
        controller.rematchButton = rematchBtn.gameObject;
        controller.rematchSecretInput = rematchSecret;
        // The rematch handshake reports into the Lock's slot in the guess card
        // — free exactly when a match is decided — so the banner can hold the
        // result text without the status writing over it.
        controller.rematchStatusText = rematchStatus;

        // Button hooks.
        createBtn.onClick.AddListener(() => ShowOnly(controller, createPanel));
        joinBtn.onClick.AddListener(() => ShowOnly(controller, joinPanel));
        closeBtn.onClick.AddListener(controller.ClosePvpMenu);
        createGo.onClick.AddListener(controller.OnCreateRoomPressed);
        copyBtn.onClick.AddListener(controller.OnCopyInvitePressed);
        createBack.onClick.AddListener(controller.CancelRoomAndLeave);
        joinGo.onClick.AddListener(controller.OnJoinRoomPressed);
        joinBack.onClick.AddListener(controller.CancelRoomAndLeave);
        guessBtn.onClick.AddListener(controller.OnSubmitGuessPressed);
        lockBtn.onClick.AddListener(controller.OnLockTogglePressed);
        rematchBtn.onClick.AddListener(controller.OnRematchPressed);
        rematchSecret.onSubmit.AddListener(_ => controller.OnRematchPressed());
        leaveBtn.onClick.AddListener(controller.OnLeaveMatchPressed);

        for (int i = 0; i < signalButtons.Length; i++)
        {
            int signalId = i; // capture per iteration, not the shared loop variable
            signalButtons[i].onClick.AddListener(() => controller.OnSignalPressed(signalId));
        }

        BuildResultOverlay(controller, matchPanel);

        // Soft-keyboard Done (Enter in the editor) submits the field's flow;
        // the handlers validate and give feedback, so a premature submit is
        // safe. The join-code field routes to join too — with the secret
        // still empty it just shows the "enter your secret" status.
        createSecret.onSubmit.AddListener(_ => controller.OnCreateRoomPressed());
        joinCode.onSubmit.AddListener(_ => controller.OnJoinRoomPressed());
        joinSecret.onSubmit.AddListener(_ => controller.OnJoinRoomPressed());
        guessInput.onSubmit.AddListener(_ => controller.OnSubmitGuessPressed());

        // All panels start hidden; OpenPvpMenu shows the menu panel.
        lockBtn.gameObject.SetActive(false);
        signalsRoot.SetActive(false);
        rematchBtn.gameObject.SetActive(false);
        rematchSecret.gameObject.SetActive(false);
        menuPanel.SetActive(false);
        createPanel.SetActive(false);
        joinPanel.SetActive(false);
        matchPanel.SetActive(false);
    }

    static Image AddSprite(Transform parent, string name, string resource,
        Vector2 position, Vector2 size, float alpha = 1f)
    {
        var sprite = Resources.Load<Sprite>(resource);
        if (sprite == null) return null;
        var go = RuntimeUI.CreateObject(name, parent);
        ConvergingLight.Center(go, position, size);
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
        var card = NeonFrame.Frame(parent, "TipCard", position,
            new Vector2(860f, 190f), ConsumerTokens.Gold, 0.82f, true,
            ConsumerTokens.Surface);
        AddSprite(card.transform, "TipIcon", "design/signal_luck",
            new Vector2(-330f, 0f), new Vector2(72f, 72f));
        AddLocalizedText(card.transform, "TipText", "private_room_tip", 26,
            new Vector2(90f, 0f), new Vector2(620f, 140f),
            ConvergingLight.NearWhite);
    }

    void BuildResultOverlay(PvpGameController controller, GameObject matchPanel)
    {
        var root = RuntimeUI.CreateObject("ResultVisualRoot", matchPanel.transform);
        RuntimeUI.Stretch(root);
        var background = root.AddComponent<Image>();
        background.sprite = ConvergingLight.DepthGradientSprite;
        background.color = Color.white;
        background.raycastTarget = true;

        AddLocalizedText(root.transform, "PageTitle", "result_page_title", 28,
            new Vector2(-345f, 825f), new Vector2(330f, 62f),
            ConvergingLight.NearWhite);
        AddSprite(root.transform, "Logo", "reference/hol_logo_exact",
            new Vector2(0f, 790f), new Vector2(330f, 150f));

        var chip = NeonFrame.Frame(root.transform, "PlayerChip",
            new Vector2(350f, 825f), new Vector2(310f, 92f),
            ConsumerTokens.Cyan, 0.84f, true, ConsumerTokens.Surface);
        AddSprite(chip.transform, "Avatar", "reference/player_cyan_exact",
            new Vector2(-105f, 0f), new Vector2(70f, 70f));
        var chipText = RuntimeUI.CreateText(chip.transform, "Text", "", 20,
            new Vector2(35f, 0f), new Vector2(190f, 64f));

        var confettiGo = RuntimeUI.CreateObject(
            "ResultConfettiLayer", root.transform);
        ConvergingLight.Center(confettiGo, new Vector2(0f, 250f),
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
        ConvergingLight.Center(pop, new Vector2(0f, 260f),
            new Vector2(1000f, 900f));
        confetti.popTarget = (RectTransform)pop.transform;

        var title = RuntimeUI.CreateText(pop.transform, "ResultTitle", "", 78,
            new Vector2(0f, 260f), new Vector2(900f, 140f),
            ConsumerTokens.Gold);
        title.enableAutoSizing = true;
        title.fontSizeMin = 42f;
        title.fontSizeMax = 78f;

        var hero = NeonFrame.Frame(pop.transform, "ResultHeroCard",
            new Vector2(0f, -25f), new Vector2(940f, 560f),
            ConsumerTokens.Cyan, 0.93f, true, ConsumerTokens.Surface);
        AddSprite(hero.transform, "WinnerBoy", "reference/player_cyan_exact",
            new Vector2(-290f, -15f), new Vector2(390f, 430f));
        var trophy = AddSprite(hero.transform, "Trophy",
            "reference/board_trophy_exact",
            new Vector2(-80f, -45f), new Vector2(250f, 300f));

        var attempts = NeonFrame.Frame(hero.transform, "AttemptsBoard",
            new Vector2(255f, 35f), new Vector2(390f, 430f),
            ConsumerTokens.Magenta, 0.72f, false, ConsumerTokens.Surface);
        AddLocalizedText(attempts.transform, "Heading", "result_attempts", 24,
            new Vector2(0f, 175f), new Vector2(350f, 50f),
            ConvergingLight.NearWhite);

        var playerColumn = NeonFrame.Frame(attempts.transform, "PlayerAttempts",
            new Vector2(-92f, 0f), new Vector2(170f, 290f),
            ConsumerTokens.Cyan, 0.78f, false, ConsumerTokens.CardBlue);
        AddLocalizedText(playerColumn.transform, "Role", "you", 20,
            new Vector2(0f, 105f), new Vector2(145f, 38f),
            ConvergingLight.NearWhite);
        var playerAttempts = RuntimeUI.CreateText(playerColumn.transform,
            "Value", "0", 76, new Vector2(0f, 10f),
            new Vector2(145f, 110f), ConsumerTokens.Cyan);
        AddLocalizedText(playerColumn.transform, "Unit",
            "result_attempts_short", 18, new Vector2(0f, -91f),
            new Vector2(145f, 38f), ConsumerTokens.Cyan);

        var opponentColumn = NeonFrame.Frame(attempts.transform,
            "OpponentAttempts", new Vector2(92f, 0f),
            new Vector2(170f, 290f), ConsumerTokens.Magenta, 0.78f,
            false, ConsumerTokens.CardPink);
        AddLocalizedText(opponentColumn.transform, "Role",
            "prebattle_opponent", 18, new Vector2(0f, 105f),
            new Vector2(155f, 38f), ConvergingLight.NearWhite);
        var opponentAttempts = RuntimeUI.CreateText(opponentColumn.transform,
            "Value", "0", 76, new Vector2(0f, 10f),
            new Vector2(145f, 110f), ConsumerTokens.Magenta);
        AddLocalizedText(opponentColumn.transform, "Unit",
            "result_attempts_short", 18, new Vector2(0f, -91f),
            new Vector2(145f, 38f), ConsumerTokens.Magenta);
        AddSprite(opponentColumn.transform, "Girl",
            "reference/char_girl_exact", new Vector2(42f, -95f),
            new Vector2(80f, 80f));

        var revealed = RuntimeUI.CreateText(attempts.transform,
            "RevealedNumber", "", 22, new Vector2(0f, -185f),
            new Vector2(350f, 52f), ConvergingLight.NearWhite);

        var rematchCard = NeonFrame.Frame(root.transform, "RematchCard",
            new Vector2(0f, -365f), new Vector2(850f, 230f),
            ConsumerTokens.Magenta, 0.88f, true, ConsumerTokens.Surface);
        AddLocalizedText(rematchCard.transform, "Heading",
            "result_rematch_heading", 24, new Vector2(0f, 82f),
            new Vector2(780f, 42f), ConvergingLight.NearWhite);
        var rematchSecret = RuntimeUI.CreateInputField(rematchCard.transform,
            "RematchSecret", L10n.Get("rematch_prompt"),
            new Vector2(0f, 27f), new Vector2(760f, 64f));
        RuntimeUI.LocalizePlaceholder(rematchSecret, "rematch_prompt");
        var rematch = RuntimeUI.CreateButton(rematchCard.transform,
            "ResultConfirmRematchButton", L10n.Get("rematch"),
            new Vector2(-205f, -62f), new Vector2(370f, 72f),
            ConsumerTokens.Gold, DarkLabel);
        RuntimeUI.Localize(rematch, "rematch");
        var exit = RuntimeUI.CreateButton(rematchCard.transform,
            "ResultExitButton", L10n.Get("result_exit"),
            new Vector2(205f, -62f), new Vector2(370f, 72f),
            ConsumerTokens.Cyan, DarkLabel);
        RuntimeUI.Localize(exit, "result_exit");
        var rematchStatus = RuntimeUI.CreateText(root.transform,
            "ResultRematchStatus", "", 20, new Vector2(0f, -505f),
            new Vector2(780f, 42f), ConsumerTokens.TextSecondary);

        var reactionCard = NeonFrame.Frame(root.transform, "ReactionCard",
            new Vector2(0f, -690f), new Vector2(760f, 310f),
            ConsumerTokens.Magenta, 0.82f, true, ConsumerTokens.Surface);
        AddLocalizedText(reactionCard.transform, "Heading",
            "result_reactions", 23, new Vector2(0f, 125f),
            new Vector2(700f, 40f), ConvergingLight.NearWhite);
        var resultSignalFeed = RuntimeUI.CreateText(reactionCard.transform,
            "SignalFeed", "", 17, new Vector2(0f, 88f),
            new Vector2(680f, 30f), ConsumerTokens.TextSecondary);
        var resultSignals = RuntimeUI.CreateObject(
            "ResultSignals", reactionCard.transform);
        RuntimeUI.Stretch(resultSignals);
        for (int i = 0; i < Signals.Count; i++)
        {
            int signalId = i;
            float x = (i % 3 - 1) * 235f;
            float y = i < 3 ? 35f : -55f;
            var signal = RuntimeUI.CreateButton(resultSignals.transform,
                "ResultSignal" + i, Signals.Text(i), new Vector2(x, y),
                new Vector2(220f, 76f), ConsumerTokens.SurfaceElevated);
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
        controller.winConfetti = confetti;

        rematch.onClick.AddListener(controller.OnRematchPressed);
        rematchSecret.onSubmit.AddListener(_ => controller.OnRematchPressed());
        exit.onClick.AddListener(controller.OnLeaveMatchPressed);

        rematch.gameObject.SetActive(false);
        rematchSecret.gameObject.SetActive(false);
        resultSignals.SetActive(false);
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
            ConvergingLight.NearWhite);
        AddSprite(root, "Logo", "reference/hol_logo_exact",
            new Vector2(0f, 625f), new Vector2(440f, 210f));
        AddLocalizedText(root, "YourLabel", "prebattle_you", 24,
            new Vector2(-280f, 455f), new Vector2(360f, 52f),
            ConvergingLight.NearWhite);
        AddLocalizedText(root, "OpponentLabel", "prebattle_opponent", 24,
            new Vector2(280f, 455f), new Vector2(360f, 52f),
            ConvergingLight.NearWhite);

        var left = NeonFrame.Frame(root, "YouCard",
            new Vector2(-275f, 235f), new Vector2(430f, 430f),
            ConsumerTokens.Cyan, 0.90f, true, ConsumerTokens.CardBlue);
        AddSprite(left.transform, "Boy", "reference/player_cyan_exact",
            new Vector2(0f, 20f), new Vector2(330f, 320f));
        var playerName = AddLocalizedText(left.transform, "Name",
            "player_default", 28,
            new Vector2(0f, -160f), new Vector2(360f, 58f),
            ConvergingLight.NearWhite);
        left.AddComponent<PlayerNameLabel>().target = playerName;

        var right = NeonFrame.Frame(root, "OpponentCard",
            new Vector2(275f, 235f), new Vector2(430f, 430f),
            ConsumerTokens.Magenta, 0.90f, true, ConsumerTokens.CardPink);
        AddSprite(right.transform, "Girl", "reference/char_girl_exact",
            new Vector2(0f, 20f), new Vector2(330f, 320f));
        parts.opponentStatus = AddLocalizedText(right.transform, "Status",
            "prebattle_waiting_short", 26,
            new Vector2(0f, -160f), new Vector2(360f, 58f),
            ConvergingLight.NearWhite);
        AddSprite(root, "VsBurst", "reference/board_vs_burst_exact",
            new Vector2(0f, 235f), new Vector2(180f, 180f));

        var rule = NeonFrame.Frame(root, "RuleCard",
            new Vector2(0f, -70f), new Vector2(900f, 190f),
            ConsumerTokens.Cyan, 0.88f, true, ConsumerTokens.Surface);
        AddLocalizedText(rule.transform, "RuleTitle", "prebattle_rule_title",
            30, new Vector2(-170f, 52f), new Vector2(430f, 52f),
            ConsumerTokens.Cyan);
        AddLocalizedText(rule.transform, "Rule", "prebattle_rule", 24,
            new Vector2(-125f, -30f), new Vector2(560f, 90f),
            ConvergingLight.NearWhite);
        AddSprite(rule.transform, "Rocket", "reference/board_rocket_exact",
            new Vector2(300f, 0f), new Vector2(170f, 170f));

        parts.entryRoot = RuntimeUI.CreateObject("EntryState", root);
        RuntimeUI.Stretch(parts.entryRoot);
        parts.waitingRoot = RuntimeUI.CreateObject("WaitingState", root);
        RuntimeUI.Stretch(parts.waitingRoot);

        if (createMode)
        {
            var code = NeonFrame.Frame(parts.waitingRoot.transform,
                "RoomCodeFrame", new Vector2(-190f, -285f),
                new Vector2(430f, 110f), ConsumerTokens.Magenta,
                0.82f, false, ConsumerTokens.Surface);
            AddLocalizedText(code.transform, "Caption", "pvp_enter_code",
                18, new Vector2(0f, 24f), new Vector2(390f, 34f),
                ConvergingLight.WithAlpha(ConvergingLight.NearWhite, 0.72f));
            parts.codeText = RuntimeUI.CreateText(code.transform, "RoomCode",
                "-----", 44, new Vector2(0f, -20f),
                new Vector2(390f, 52f));

            parts.copy = RuntimeUI.CreateButton(parts.waitingRoot.transform,
                "ShareButton", L10n.Get("private_room_share"),
                new Vector2(275f, -285f), new Vector2(300f, 96f),
                ConsumerTokens.SurfaceElevated);
            RuntimeUI.Localize(parts.copy, "private_room_share");
        }

        var waiting = NeonFrame.Frame(parts.waitingRoot.transform,
            "WaitingPlate", new Vector2(0f, createMode ? -500f : -380f),
            new Vector2(820f, 150f), ConsumerTokens.Gold, 0.86f,
            true, ConsumerTokens.Surface);
        parts.status = RuntimeUI.CreateText(waiting.transform, "Status",
            "", 30, Vector2.zero, new Vector2(760f, 100f));

        if (createMode)
        {
            parts.secret = RuntimeUI.CreateInputField(
                parts.entryRoot.transform, "SecretInput",
                L10n.Get("pvp_secret"), new Vector2(0f, -360f),
                new Vector2(500f, 84f));
            parts.confirm = RuntimeUI.CreateButton(
                parts.entryRoot.transform, "ConfirmCreateButton",
                L10n.Get("confirm"), new Vector2(0f, -490f),
                new Vector2(420f, 82f), ConsumerTokens.Gold, DarkLabel).gameObject;
            parts.entryStatus = RuntimeUI.CreateText(
                parts.entryRoot.transform, "EntryStatus", "", 22,
                new Vector2(0f, -590f), new Vector2(700f, 60f),
                ConsumerTokens.TextSecondary);
            RuntimeUI.LocalizePlaceholder(parts.secret, "pvp_secret");
            RuntimeUI.Localize(parts.confirm.GetComponent<Button>(), "confirm");
        }
        else
        {
            parts.codeInput = RuntimeUI.CreateInputField(
                parts.entryRoot.transform, "CodeInput",
                L10n.Get("pvp_enter_code"), new Vector2(0f, -320f),
                new Vector2(500f, 84f), 5, TMP_InputField.ContentType.Standard);
            parts.codeInput.onValidateInput = (text, index, ch) =>
                char.ToUpperInvariant(ch);
            parts.secret = RuntimeUI.CreateInputField(
                parts.entryRoot.transform, "SecretInput",
                L10n.Get("pvp_secret"), new Vector2(0f, -430f),
                new Vector2(500f, 84f));
            parts.confirm = RuntimeUI.CreateButton(
                parts.entryRoot.transform, "ConfirmJoinButton",
                L10n.Get("confirm"), new Vector2(0f, -550f),
                new Vector2(420f, 82f), ConsumerTokens.Gold, DarkLabel).gameObject;
            parts.entryStatus = RuntimeUI.CreateText(
                parts.entryRoot.transform, "EntryStatus", "", 22,
                new Vector2(0f, -650f), new Vector2(700f, 60f),
                ConsumerTokens.TextSecondary);
            RuntimeUI.LocalizePlaceholder(parts.codeInput, "pvp_enter_code");
            RuntimeUI.LocalizePlaceholder(parts.secret, "pvp_secret");
            RuntimeUI.Localize(parts.confirm.GetComponent<Button>(), "confirm");
        }

        parts.back = RuntimeUI.CreateButton(root, "CancelButton",
            L10n.Get("cancel"), new Vector2(0f, -835f),
            new Vector2(270f, 70f), ConsumerTokens.SurfaceElevated);
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
        RetireLegacyPanel(controller.pvpMenuPanel);
        RetireLegacyPanel(controller.createPanel);
        RetireLegacyPanel(controller.joinPanel);

        var menu = BuildPortraitPanel(transform, "PvPMenuPanel");
        AddLocalizedText(menu.transform, "PageTitle",
            "private_room_title", 30, new Vector2(0f, 800f),
            new Vector2(720f, 70f), ConvergingLight.NearWhite);
        AddSprite(menu.transform, "Logo", "reference/hol_logo_exact",
            new Vector2(0f, 620f), new Vector2(500f, 240f));
        var ribbon = NeonFrame.Frame(menu.transform, "TitleRibbon",
            new Vector2(0f, 365f), new Vector2(900f, 130f),
            ConsumerTokens.Magenta, 0.92f, true, ConsumerTokens.CardPink);
        AddLocalizedText(ribbon.transform, "Title", "private_room_title", 48,
            Vector2.zero, new Vector2(860f, 110f), ConvergingLight.NearWhite);

        var create = RuntimeUI.CreateButton(menu.transform, "CreateButton",
            L10n.Get("pvp_create_room"), new Vector2(0f, 40f),
            new Vector2(860f, 330f), ConsumerTokens.Cyan, DarkLabel);
        ForceProceduralButton(create, ConsumerTokens.Cyan);
        AddSprite(create.transform, "FriendArt", "reference/player_cyan_exact",
            new Vector2(-255f, 22f), new Vector2(250f, 220f));
        AddSprite(create.transform, "GirlArt", "reference/char_girl_exact",
            new Vector2(-70f, 4f), new Vector2(190f, 180f));
        AddLocalizedText(create.transform, "Hint", "private_room_create_hint", 25,
            new Vector2(245f, 54f), new Vector2(300f, 84f),
            ConvergingLight.NearWhite);
        RuntimeUI.Localize(create, "pvp_create_room");

        var join = RuntimeUI.CreateButton(menu.transform, "JoinButton",
            L10n.Get("pvp_join_room"), new Vector2(0f, -315f),
            new Vector2(860f, 330f), ConsumerTokens.Magenta, DarkLabel);
        ForceProceduralButton(join, ConsumerTokens.Magenta);
        var joinBuiltInLabel = join.GetComponentInChildren<TMP_Text>();
        if (joinBuiltInLabel != null)
            joinBuiltInLabel.gameObject.SetActive(false);
        AddSprite(join.transform, "DoorArt", "reference/board_join_exact",
            new Vector2(-250f, 20f), new Vector2(220f, 190f));
        AddLocalizedText(join.transform, "JoinTitle", "private_room_join_title", 30,
            new Vector2(220f, 78f), new Vector2(400f, 72f),
            ConvergingLight.NearWhite);
        AddLocalizedText(join.transform, "CodeCaption", "pvp_enter_code", 22,
            new Vector2(220f, -8f), new Vector2(360f, 48f),
            ConvergingLight.WithAlpha(ConvergingLight.NearWhite, 0.75f));
        RuntimeUI.Localize(join, "pvp_join_room");

        AddRoomTip(menu.transform, new Vector2(0f, -720f));
        AddSprite(menu.transform, "MascotSix", "reference/mascot_6_exact",
            new Vector2(-440f, -760f), new Vector2(150f, 180f));
        AddSprite(menu.transform, "MascotSeven", "reference/mascot_7_exact",
            new Vector2(440f, -760f), new Vector2(150f, 180f));
        var back = RuntimeUI.CreateButton(menu.transform, L10n.Get("back"),
            L10n.Get("back"), new Vector2(0f, -850f),
            new Vector2(260f, 70f), ConsumerTokens.SurfaceElevated);
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

    void RetireLegacyPanel(GameObject panel)
    {
        if (panel == null) return;
        panel.SetActive(false);
        panel.name = "Retired" + panel.name;
        Destroy(panel);
    }

    static GameObject BuildPortraitPanel(Transform parent, string name)
    {
        var panel = RuntimeUI.FullscreenPanel(parent, name, PanelColor);
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

        var entry = RuntimeUI.CreateButton(mainCanvasGo.transform, "ButtonPvP",
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
