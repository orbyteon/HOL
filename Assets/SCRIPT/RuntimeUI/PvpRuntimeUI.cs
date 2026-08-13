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

    // Converging Light palette (design/philosophy.md), routed through the
    // active theme: depth panels, cyan and gold as the disciplined lights,
    // near-white text. Gold is reserved for the single most important
    // action on each screen (primary CTA).
    static Color PanelColor => ConvergingLight.WithAlpha(ConvergingLight.PanelIndigo, 0.97f);
    static Color Accent => ConvergingLight.Gold;
    static Color AccentBlue => ConvergingLight.Cyan;
    static Color Neutral => ConvergingLightFX.GhostSurface;
    static Color DarkLabel => ConvergingLightFX.DarkLabel;

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

        // Create flow.
        var createPanel = RuntimeUI.FullscreenPanel(transform, "PvPCreatePanel", PanelColor);
        var createTitle = RuntimeUI.CreateText(createPanel.transform, "Title", L10n.Get("pvp_create_room"), 48,
            new Vector2(0f, 420f), new Vector2(800f, 100f));
        var createSecret = RuntimeUI.CreateInputField(createPanel.transform, "SecretInput",
            L10n.Get("pvp_secret"), new Vector2(0f, 240f), new Vector2(460f, 90f));
        var createGo = RuntimeUI.CreateButton(createPanel.transform, "GoButton",
            L10n.Get("confirm"), new Vector2(0f, 110f), new Vector2(460f, 90f), Accent, DarkLabel);
        var codeText = RuntimeUI.CreateText(createPanel.transform, "RoomCode", "-----", 96,
            new Vector2(0f, -80f), new Vector2(800f, 140f));
        var copyBtn = RuntimeUI.CreateButton(createPanel.transform, "CopyButton",
            L10n.Get("pvp_copy"), new Vector2(0f, -220f), new Vector2(460f, 90f), AccentBlue, DarkLabel);
        var createStatus = RuntimeUI.CreateText(createPanel.transform, "Status", "", 32,
            new Vector2(0f, -380f), new Vector2(900f, 120f));
        var createBack = RuntimeUI.CreateButton(createPanel.transform, "BackButton",
            L10n.Get("back"), new Vector2(0f, -540f), new Vector2(300f, 80f), Neutral);

        RuntimeUI.Localize(createTitle, "pvp_create_room");
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
        var guessInput = RuntimeUI.CreateInputField(matchPanel.transform, "GuessInput",
            "1-100", new Vector2(0f, -80f), new Vector2(460f, 90f));
        var guessBtn = RuntimeUI.CreateButton(matchPanel.transform, "GuessButton",
            L10n.Get("pvp_guess"), new Vector2(0f, -210f), new Vector2(460f, 90f), Accent, DarkLabel);
        var resultText = RuntimeUI.CreateText(matchPanel.transform, "Result", "", 72,
            new Vector2(0f, -400f), new Vector2(1000f, 120f));
        var leaveBtn = RuntimeUI.CreateButton(matchPanel.transform, "LeaveButton",
            L10n.Get("pvp_leave"), new Vector2(0f, -560f), new Vector2(300f, 80f), Neutral);

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

        // Busy affordances: these disable while their request is in flight.
        controller.createGoButton = createGo;
        controller.joinGoButton = joinGo;
        controller.guessButton = guessBtn;

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
        // Leave routes through the armed two-step confirm, like hardware back.
        leaveBtn.onClick.AddListener(controller.OnLeaveButtonPressed);

        // Soft-keyboard Done (Enter in the editor) submits the field's flow;
        // the handlers validate and give feedback, so a premature submit is
        // safe. The join-code field routes to join too — with the secret
        // still empty it just shows the "enter your secret" status.
        createSecret.onSubmit.AddListener(_ => controller.OnCreateRoomPressed());
        joinCode.onSubmit.AddListener(_ => controller.OnJoinRoomPressed());
        joinSecret.onSubmit.AddListener(_ => controller.OnJoinRoomPressed());
        guessInput.onSubmit.AddListener(_ => controller.OnSubmitGuessPressed());

        // Publisher pass: each fullscreen fill becomes a dimming scrim with a
        // floating card behind the controls, the room code becomes the gold
        // hero mark, headers read as engraved captions, and the gold CTAs
        // carry the shine sweep.
        StyleAsCard(menuPanel, new Vector2(0f, 110f), new Vector2(880f, 960f), true);
        StyleAsCard(createPanel, new Vector2(0f, -60f), new Vector2(1000f, 1360f), true);
        StyleAsCard(joinPanel, new Vector2(0f, 20f), new Vector2(1000f, 1160f), true);
        StyleAsCard(matchPanel, new Vector2(0f, -70f), new Vector2(1000f, 1360f), false);

        controller.roomCodeText.color = Accent;
        controller.roomCodeText.characterSpacing = 14f;
        ConvergingLightFX.AddSeamRule(createPanel.transform, new Vector2(0f, -172f), 460f);

        ConvergingLightFX.StyleHeading(controller.opponentNameText, 3f);
        ConvergingLightFX.StyleHeading(controller.resultText, 2f);

        ConvergingLightFX.AddShine(createGo);
        ConvergingLightFX.AddShine(guessBtn);

        // Victory light pulse for duels, scoped to the match screen.
        var flash = WinFlash.Attach(transform);
        flash.gate = matchPanel;

        // All panels start hidden; OpenPvpMenu shows the menu panel.
        menuPanel.SetActive(false);
        createPanel.SetActive(false);
        joinPanel.SetActive(false);
        matchPanel.SetActive(false);
    }

    // Scrim + card treatment for a fullscreen PvP panel. The panel's own
    // image dims the scene behind it; a rounded, shadowed, seam-lit card
    // floats behind the controls (children paint in order, so glow at index
    // 0 and card at index 1 sit under everything built before this call).
    static void StyleAsCard(GameObject panel, Vector2 cardPos, Vector2 cardSize, bool cascade)
    {
        var scrim = panel.GetComponent<Image>();
        if (scrim != null)
            scrim.color = ConvergingLight.WithAlpha(ConvergingLight.ScrimIndigo, 0.88f);

        var card = RuntimeUI.CreateObject("Card", panel.transform);
        ConvergingLight.Center(card, cardPos, cardSize);
        var surface = card.AddComponent<Image>();
        surface.raycastTarget = false;
        ConvergingLightFX.StyleCard(surface, 0.97f);
        card.transform.SetSiblingIndex(0);

        ConvergingLightFX.AddCardGlow(panel.transform, cardPos, cardSize,
            ConvergingLight.Cyan, 0.09f);

        if (panel.GetComponent<PanelAnimator>() == null)
            panel.AddComponent<PanelAnimator>();
        if (cascade && panel.GetComponent<CascadeReveal>() == null)
            panel.AddComponent<CascadeReveal>();
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
        ConvergingLightFX.AddShine(entry); // gold CTA earns the glint

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
