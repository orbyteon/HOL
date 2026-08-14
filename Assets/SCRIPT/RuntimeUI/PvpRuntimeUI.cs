using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Zero-setup PvP interface: builds the whole duel UI from code and injects
// a "PvP DUEL" entry button into the scene's main Canvas. No scene wiring
// needed beyond dropping this component on one GameObject.
//
// Backend choice: Firebase (PvpClient) by default; tick usePlayFab to use
// PlayFabPvpClient instead. Either way the backend component needs its
// dashboard config (Firebase databaseUrl / PlayFab titleId) — set it in the
// fields below; they are copied onto the backend component created at Start.
[RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
public class PvpRuntimeUI : MonoBehaviour
{
    [Tooltip("Use PlayFab backend instead of Firebase")]
    public bool usePlayFab;

    [Tooltip("PlayFab Title ID from Game Manager — copied onto the PlayFab backend")]
    public string playFabTitleId = "";

    [Tooltip("Firebase Realtime Database URL — copied onto the Firebase backend")]
    public string firebaseDatabaseUrl = "";

    // Converging Light palette (design/philosophy.md): indigo depth, cyan and
    // gold as the disciplined lights, near-white text. Gold is reserved for
    // the single most important action on each screen (primary CTA).
    static readonly Color PanelColor = new Color(0.08f, 0.05f, 0.15f, 0.97f);
    static readonly Color Accent = new Color(1f, 0.78f, 0.34f, 1f);   // muted gold
    static readonly Color AccentBlue = new Color(0.25f, 0.85f, 1f, 1f); // cyan seam
    static readonly Color Neutral = new Color(0.16f, 0.15f, 0.26f, 1f); // indigo gray
    static readonly Color DarkLabel = new Color(0.10f, 0.09f, 0.18f, 1f);

    void Start()
    {
        // Release builds must not run the Firebase fallback when PlayFab is
        // configured: PvpClient is dev-only (secrets readable in the room
        // document, last-write-wins joins — see its header). A stray
        // Inspector tick must not silently downgrade production security.
        if (!usePlayFab && !Debug.isDebugBuild && !string.IsNullOrEmpty(playFabTitleId))
        {
            Debug.LogWarning("PvpRuntimeUI: Firebase backend selected in a release build " +
                "with PlayFab configured — overriding to PlayFab (Firebase is dev-only).");
            usePlayFab = true;
        }

        // Backend + controller live on this same GameObject. The backend is
        // created here, so its dashboard config comes from our own fields.
        PvpBackend backend;
        if (usePlayFab)
        {
            var playFab = gameObject.AddComponent<PlayFabPvpClient>();
            playFab.titleId = playFabTitleId;
            backend = playFab;
        }
        else
        {
            var firebase = gameObject.AddComponent<PvpClient>();
            if (!string.IsNullOrEmpty(firebaseDatabaseUrl))
                firebase.databaseUrl = firebaseDatabaseUrl;
            backend = firebase;
        }

        var controller = gameObject.AddComponent<PvpGameController>();
        controller.client = backend;

        BuildPanels(controller);

        InjectEntryButton(controller);
    }

    // ------------------------------------------------------------ construction

    // The indigo depth gradient the rest of Converging Light sits on. Added as
    // the panel's first child so it draws behind every control placed after it,
    // which is why it is called immediately after the panel is created.
    static void NeonBackdrop(GameObject panel)
    {
        var backdrop = RuntimeUI.CreateObject("Backdrop", panel.transform);
        RuntimeUI.Stretch(backdrop);

        var image = backdrop.AddComponent<Image>();
        image.sprite = ConvergingLight.VerticalGradient(
            ConvergingLight.DepthTop, ConvergingLight.DepthBottom);
        image.type = Image.Type.Simple;
        image.raycastTarget = false;
    }

    void BuildPanels(PvpGameController controller)
    {
        // PvP menu: create or join.
        var menuPanel = RuntimeUI.FullscreenPanel(transform, "PvPMenuPanel", PanelColor);
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
        var createGo = RuntimeUI.CreateButton(createPanel.transform, "GoButton",
            L10n.Get("confirm"), new Vector2(0f, 225f), new Vector2(460f, 90f), Accent, DarkLabel);

        var codeFrame = NeonFrame.Frame(createPanel.transform, "RoomCodeFrame",
            new Vector2(0f, 0f), new Vector2(760f, 280f), ConvergingLight.Cyan);
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
            ConvergingLight.WithAlpha(ConvergingLight.Magenta, 0.5f), 0.5f);
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
        var joinGo = RuntimeUI.CreateButton(joinPanel.transform, "GoButton",
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

        // Match.
        var matchPanel = RuntimeUI.FullscreenPanel(transform, "PvPMatchPanel", PanelColor);
        var opponentText = RuntimeUI.CreateText(matchPanel.transform, "Opponent", "", 44,
            new Vector2(0f, 420f), new Vector2(1000f, 90f));
        var turnText = RuntimeUI.CreateText(matchPanel.transform, "Turn", "", 44,
            new Vector2(0f, 300f), new Vector2(1000f, 90f));
        var historyText = RuntimeUI.CreateText(matchPanel.transform, "History", "", 40,
            new Vector2(0f, 120f), new Vector2(1000f, 120f));
        // How far the player has narrowed the opponent's number. Solo play has
        // always shown this; PvP never did.
        var rangeText = RuntimeUI.CreateText(matchPanel.transform, "Range", "", 34,
            new Vector2(0f, 205f), new Vector2(1000f, 70f), AccentBlue);
        var signalFeed = RuntimeUI.CreateText(matchPanel.transform, "SignalFeed", "", 34,
            new Vector2(0f, 20f), new Vector2(1000f, 70f), AccentBlue);
        var guessInput = RuntimeUI.CreateInputField(matchPanel.transform, "GuessInput",
            "1-100", new Vector2(0f, -80f), new Vector2(460f, 90f));
        var guessBtn = RuntimeUI.CreateButton(matchPanel.transform, "GuessButton",
            L10n.Get("pvp_guess"), new Vector2(-135f, -200f), new Vector2(390f, 90f), Accent, DarkLabel);

        // Gold stays reserved for the primary action (design/philosophy.md), so
        // the Lock takes the cyan seam. Its label is state-driven — it becomes
        // a prompt once the range is down to a few candidates — so the
        // controller sets the text rather than a LocalizedText component.
        var lockBtn = RuntimeUI.CreateButton(matchPanel.transform, "LockButton",
            L10n.Get("lock"), new Vector2(200f, -200f), new Vector2(260f, 90f), AccentBlue, DarkLabel);
        var lockLabel = AsTmp(lockBtn.GetComponentInChildren<Text>());
        lockLabel.fontSize = 26;

        var signalsRoot = RuntimeUI.CreateObject("Signals", matchPanel.transform);
        var signalButtons = new Button[Signals.Count];
        for (int i = 0; i < Signals.Count; i++)
        {
            float x = (i % 3 - 1) * 330f;
            float y = i < 3 ? -300f : -374f;
            var signalBtn = RuntimeUI.CreateButton(signalsRoot.transform, "Signal" + i,
                Signals.Text(i), new Vector2(x, y), new Vector2(315f, 66f), Neutral);
            var signalLabel = signalBtn.GetComponentInChildren<Text>();
            if (signalLabel != null) signalLabel.fontSize = 24;
            RuntimeUI.Localize(signalBtn, Signals.Key(i));
            signalButtons[i] = signalBtn;
        }

        // Offered on the result screen, in the slot the guess controls vacate.
        var rematchSecret = RuntimeUI.CreateInputField(matchPanel.transform, "RematchSecret",
            "1-100", new Vector2(0f, -80f), new Vector2(460f, 90f));
        var rematchBtn = RuntimeUI.CreateButton(matchPanel.transform, "RematchButton",
            L10n.Get("rematch"), new Vector2(0f, -200f), new Vector2(460f, 90f), Accent, DarkLabel);
        RuntimeUI.LocalizePlaceholder(rematchSecret, "rematch_prompt");
        RuntimeUI.Localize(rematchBtn, "rematch");

        // Sized for the longest result, which is the draw: three lines in
        // English and four in Greek, where the closing tip wraps. At 64pt those
        // spilled up into the second Signals row, which shares the result
        // screen. 48pt keeps every line unwrapped inside 1000px and leaves a
        // clear gap above and below.
        var resultText = RuntimeUI.CreateText(matchPanel.transform, "Result", "", 48,
            new Vector2(0f, -530f), new Vector2(1000f, 200f));
        var leaveBtn = RuntimeUI.CreateButton(matchPanel.transform, "LeaveButton",
            L10n.Get("pvp_leave"), new Vector2(0f, -720f), new Vector2(300f, 78f), Neutral);

        RuntimeUI.LocalizePlaceholder(guessInput, "number_placeholder");
        RuntimeUI.Localize(guessBtn, "pvp_guess");
        RuntimeUI.Localize(leaveBtn, "pvp_leave");

        // Wire the controller.
        controller.pvpMenuPanel = menuPanel;
        controller.createPanel = createPanel;
        controller.joinPanel = joinPanel;
        controller.matchPanel = matchPanel;

        controller.createSecretInput = createSecret;
        controller.roomCodeText = AsTmp(codeText);
        controller.createStatusText = AsTmp(createStatus);

        // Waiting-state dots for the create panel's status line; disabled
        // until the controller shows an animated status (SetCreateStatus).
        var statusEllipsis = controller.createStatusText.gameObject.AddComponent<AnimatedEllipsis>();
        statusEllipsis.text = controller.createStatusText;
        statusEllipsis.enabled = false;
        controller.createStatusEllipsis = statusEllipsis;
        controller.joinCodeInput = joinCode;
        controller.joinSecretInput = joinSecret;
        controller.joinStatusText = AsTmp(joinStatus);
        controller.guessInput = guessInput;
        controller.opponentNameText = AsTmp(opponentText);
        controller.turnText = AsTmp(turnText);
        controller.historyText = AsTmp(historyText);
        controller.resultText = AsTmp(resultText);
        controller.rangeText = AsTmp(rangeText);
        controller.signalFeedText = AsTmp(signalFeed);
        controller.lockButton = lockBtn.gameObject;
        controller.lockButtonLabel = lockLabel;
        controller.signalsRoot = signalsRoot;
        controller.guessButton = guessBtn.gameObject;
        controller.rematchButton = rematchBtn.gameObject;
        controller.rematchSecretInput = rematchSecret;
        // The turn line is empty once a match is decided, so the rematch
        // handshake reports there rather than adding an element that would sit
        // on top of it.
        controller.rematchStatusText = controller.turnText;

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

    // The controller's fields are TMP_Text; RuntimeUI builds legacy Text for
    // labels, so replace the legacy Text with a TMP_Text on the same object.
    // Both are Graphic components and Unity allows only one Graphic per
    // GameObject, so the legacy Text must be destroyed first — immediately,
    // since Destroy() is deferred to end-of-frame and AddComponent would
    // still conflict (and return null).
    static TMP_Text AsTmp(Text legacy)
    {
        var go = legacy.gameObject;
        string content = legacy.text;
        int fontSize = legacy.fontSize;
        Color color = legacy.color;
        DestroyImmediate(legacy);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        return tmp;
    }
}
