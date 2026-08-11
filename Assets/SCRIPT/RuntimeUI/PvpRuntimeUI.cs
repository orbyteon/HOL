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
        RuntimeUI.CreateText(menuPanel.transform, "Title", L10n.Get("pvp_duel"), 64,
            new Vector2(0f, 420f), new Vector2(800f, 120f));
        var createBtn = RuntimeUI.CreateButton(menuPanel.transform, "CreateButton",
            L10n.Get("pvp_create_room"), new Vector2(0f, 120f), new Vector2(460f, 100f), Accent, DarkLabel);
        var joinBtn = RuntimeUI.CreateButton(menuPanel.transform, "JoinButton",
            L10n.Get("pvp_join_room"), new Vector2(0f, -20f), new Vector2(460f, 100f), AccentBlue, DarkLabel);
        var closeBtn = RuntimeUI.CreateButton(menuPanel.transform, "CloseButton",
            L10n.Get("back"), new Vector2(0f, -200f), new Vector2(300f, 80f), Neutral);

        // Create flow.
        var createPanel = RuntimeUI.FullscreenPanel(transform, "PvPCreatePanel", PanelColor);
        RuntimeUI.CreateText(createPanel.transform, "Title", L10n.Get("pvp_create_room"), 48,
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

        // Join flow.
        var joinPanel = RuntimeUI.FullscreenPanel(transform, "PvPJoinPanel", PanelColor);
        RuntimeUI.CreateText(joinPanel.transform, "Title", L10n.Get("pvp_join_room"), 48,
            new Vector2(0f, 420f), new Vector2(800f, 100f));
        var joinCode = RuntimeUI.CreateInputField(joinPanel.transform, "CodeInput",
            L10n.Get("pvp_enter_code"), new Vector2(0f, 240f), new Vector2(460f, 90f), 5);
        var joinSecret = RuntimeUI.CreateInputField(joinPanel.transform, "SecretInput",
            L10n.Get("pvp_secret"), new Vector2(0f, 110f), new Vector2(460f, 90f));
        var joinGo = RuntimeUI.CreateButton(joinPanel.transform, "GoButton",
            L10n.Get("confirm"), new Vector2(0f, -20f), new Vector2(460f, 90f), AccentBlue, DarkLabel);
        var joinStatus = RuntimeUI.CreateText(joinPanel.transform, "Status", "", 32,
            new Vector2(0f, -200f), new Vector2(900f, 120f));
        var joinBack = RuntimeUI.CreateButton(joinPanel.transform, "BackButton",
            L10n.Get("back"), new Vector2(0f, -380f), new Vector2(300f, 80f), Neutral);

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

        // Wire the controller.
        controller.pvpMenuPanel = menuPanel;
        controller.createPanel = createPanel;
        controller.joinPanel = joinPanel;
        controller.matchPanel = matchPanel;

        controller.createSecretInput = createSecret;
        controller.roomCodeText = AsTmp(codeText);
        controller.createStatusText = AsTmp(createStatus);
        controller.joinCodeInput = joinCode;
        controller.joinSecretInput = joinSecret;
        controller.joinStatusText = AsTmp(joinStatus);
        controller.guessInput = guessInput;
        controller.opponentNameText = AsTmp(opponentText);
        controller.turnText = AsTmp(turnText);
        controller.historyText = AsTmp(historyText);
        controller.resultText = AsTmp(resultText);

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
        leaveBtn.onClick.AddListener(controller.OnLeaveMatchPressed);

        // All panels start hidden; OpenPvpMenu shows the menu panel.
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
    // labels, so swap in a TMP_Text on the same object and copy the content.
    static TMP_Text AsTmp(Text legacy)
    {
        var tmp = legacy.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = legacy.text;
        tmp.fontSize = legacy.fontSize;
        tmp.color = legacy.color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        legacy.enabled = false; // TMP renders instead
        return tmp;
    }
}
