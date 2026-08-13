using UnityEngine;
using TMPro;

// Drives the PvP flow end to end: create/join panels, invite code display,
// waiting state, the live match, and the result. Pure UI orchestration on
// top of PvpBackend — no edits to the single-player scripts.
//
// PlayFab hints and result counts come from the server-authoritative room
// view. The Firebase fallback still fills the same RoomState locally.
public class PvpGameController : MonoBehaviour
{
    [Header("Wiring")]
    [Tooltip("Drag in either a PvpClient (Firebase) or PlayFabPvpClient (Azure PlayFab)")]
    public PvpBackend client;

    [Header("Panels")]
    public GameObject pvpMenuPanel;
    public GameObject createPanel;
    public GameObject joinPanel;
    public GameObject matchPanel;

    [Header("Create flow")]
    public TMP_InputField createSecretInput;
    public TMP_Text roomCodeText;
    public TMP_Text createStatusText;
    public AnimatedEllipsis createStatusEllipsis;

    [Header("Join flow")]
    public TMP_InputField joinCodeInput;
    public TMP_InputField joinSecretInput;
    public TMP_Text joinStatusText;

    [Header("Match UI")]
    public TMP_InputField guessInput;
    public TMP_Text opponentNameText;
    public TMP_Text turnText;
    public TMP_Text historyText;
    public TMP_Text resultText;

    public ConfettiBurst winConfetti;
    public AudioSource audioSource;
    public AudioClip winSound;
    public AudioClip loseSound;

    [Header("Optional affordances")]
    // In-flight requests disable their button so "is it doing anything?"
    // never needs asking. All null-guarded — wire what exists.
    public UnityEngine.UI.Button createGoButton;
    public UnityEngine.UI.Button joinGoButton;
    public UnityEngine.UI.Button guessButton;

    PvpBackend.RoomState lastState;
    string shownGuessKey = "";
    string myHistory = "";
    string theirHistory = "";
    bool matchOver;
    bool guessInFlight;
    int localAcceptedGuessCount;

    int flowGeneration;
    bool joinCreateInFlight;
    string lastStateSignature = "";
    int silentPolls;
    const int MaxSilentPolls = 200;

    // A host code nobody redeems expires instead of holding the panel forever.
    const float WaitingTimeoutSeconds = 240f;
    float waitingStartTime;

    string MyName
    {
        get
        {
            string name = PlayerPrefs.GetString("PlayerName", "");
            return string.IsNullOrWhiteSpace(name) ? L10n.Get("player_default") : name;
        }
    }

    public void OpenPvpMenu()
    {
        pvpMenuPanel.SetActive(true);
        createPanel.SetActive(false);
        joinPanel.SetActive(false);
        matchPanel.SetActive(false);
    }

    public void OnCreateRoomPressed()
    {
        if (joinCreateInFlight || !string.IsNullOrEmpty(client.RoomCode)) return;

        int secret;
        if (!TryReadSecret(createSecretInput, out secret))
        {
            SetCreateStatus(L10n.Get("pvp_secret"), false);
            createPanel.SetActive(true);
            pvpMenuPanel.SetActive(false);
            return;
        }

        pvpMenuPanel.SetActive(false);
        createPanel.SetActive(true);
        SetCreateStatus(L10n.Get("pvp_creating"), true);
        roomCodeText.text = "-----";

        joinCreateInFlight = true;
        SetInteractable(createGoButton, false);
        int gen = flowGeneration;
        client.CreateRoom(MyName, secret, (ok, codeOrError) =>
        {
            joinCreateInFlight = false;
            SetInteractable(createGoButton, true);
            if (gen != flowGeneration)
            {
                client.DeleteRoom();
                return;
            }
            if (!ok)
            {
                SetCreateStatus(L10n.Get("pvp_network_error"), false);
                return;
            }
            roomCodeText.text = codeOrError;
            SetCreateStatus(L10n.Get("pvp_waiting"), true);
            BeginMatchPolling();
        });
    }

    public void OnCopyInvitePressed()
    {
        if (string.IsNullOrEmpty(client.RoomCode)) return;

        GUIUtility.systemCopyBuffer = L10n.Get("pvp_invite_text", client.RoomCode);
        SetCreateStatus(L10n.Get("pvp_invite_copied"), false);
        CancelInvoke(nameof(ResumeWaitingStatus));
        Invoke(nameof(ResumeWaitingStatus), 2.5f);
    }

    void ResumeWaitingStatus()
    {
        if (createPanel == null || !createPanel.activeSelf || matchOver)
            return;
        if (string.IsNullOrEmpty(client.RoomCode))
            return;
        if (lastState != null && lastState.phase != "waiting")
            return;

        SetCreateStatus(L10n.Get("pvp_waiting"), true);
    }

    void SetCreateStatus(string message, bool animateDots)
    {
        if (createStatusEllipsis != null)
            createStatusEllipsis.enabled = false;

        createStatusText.text = message;

        if (animateDots && createStatusEllipsis != null)
        {
            createStatusEllipsis.SetBaseText(message);
            createStatusEllipsis.enabled = true;
        }
    }

    public void OnJoinRoomPressed()
    {
        if (joinCreateInFlight || !string.IsNullOrEmpty(client.RoomCode)) return;

        int secret;
        if (!TryReadSecret(joinSecretInput, out secret))
        {
            joinStatusText.text = L10n.Get("pvp_secret");
            return;
        }
        if (string.IsNullOrEmpty(joinCodeInput.text.Trim()))
        {
            joinStatusText.text = L10n.Get("pvp_enter_code");
            return;
        }

        joinStatusText.text = L10n.Get("pvp_joining");
        joinCreateInFlight = true;
        SetInteractable(joinGoButton, false);
        int gen = flowGeneration;
        client.JoinRoom(joinCodeInput.text, MyName, secret, (ok, error) =>
        {
            joinCreateInFlight = false;
            SetInteractable(joinGoButton, true);
            if (gen != flowGeneration)
            {
                client.DeleteRoom();
                return;
            }
            if (!ok)
            {
                joinStatusText.text = error;
                return;
            }
            BeginMatchPolling();
        });
    }

    public void OnSubmitGuessPressed()
    {
        if (matchOver || lastState == null || guessInFlight) return;

        string me = client.IsHost ? "host" : "guest";
        if (lastState.turn != me || lastState.phase != "play")
        {
            turnText.text = L10n.Get("pvp_wait_turn");
            return;
        }

        int guess;
        if (!TryReadSecret(guessInput, out guess))
        {
            turnText.text = L10n.Get("enter_your_number");
            return;
        }

        string typed = guessInput.text;
        guessInput.text = "";
        turnText.text = L10n.Get("pvp_sending");
        guessInFlight = true;
        SetInteractable(guessButton, false);
        int gen = flowGeneration;
        client.SubmitGuess(guess, lastState, ok =>
        {
            SetInteractable(guessButton, !matchOver);
            if (gen != flowGeneration) return;

            guessInFlight = false;
            if (ok)
            {
                localAcceptedGuessCount++;
                // Render the returned authoritative state right away: the
                // hint (and a winning result) must not wait on the next poll,
                // which the opponent's reply or the loser's leave may beat.
                if (lastState != null && !matchOver)
                    OnState(lastState);
            }
            else if (!matchOver)
            {
                var playFab = client as PlayFabPvpClient;
                string serverError = playFab != null ? playFab.LastGuessError : "";
                if (serverError == "not your turn" || serverError == "turn already submitted" ||
                    serverError == "not in play")
                {
                    // The state moved on without us (an earlier submit landed
                    // or the poll lagged) — resync now, nothing to report.
                    turnText.text = L10n.Get("pvp_wait_turn");
                    client.StartPolling(OnState);
                }
                else if (serverError == "room not found")
                {
                    client.StopPolling();
                    HandleRoomClosed();
                }
                else
                {
                    guessInput.text = typed;
                    turnText.text = L10n.Get("pvp_network_error");
                }
            }
        });
    }

    // On-screen Leave uses the same two-step contract as hardware back: a
    // decided match has nothing to forfeit, a live one asks to confirm.
    public void OnLeaveButtonPressed()
    {
        if (matchOver || !matchPanel.activeSelf ||
            Time.unscaledTime - lastMatchBackTime <= BackConfirmSeconds)
        {
            OnLeaveMatchPressed();
            return;
        }

        lastMatchBackTime = Time.unscaledTime;
        ShowConfirmHint("tap_again_to_leave");
    }

    public void OnLeaveMatchPressed()
    {
        flowGeneration++;
        client.StopPolling();
        client.DeleteRoom();
        matchOver = false;
        guessInFlight = false;
        lastState = null;
        shownGuessKey = "";
        myHistory = "";
        theirHistory = "";
        OpenPvpMenu();
    }

    public void CancelRoomAndLeave()
    {
        OnLeaveMatchPressed();
    }

    public void ClosePvpMenu()
    {
        OnLeaveMatchPressed();
        if (pvpMenuPanel != null)
            pvpMenuPanel.SetActive(false);
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (matchPanel != null && matchPanel.activeSelf)
        {
            // Same contract as solo: a decided match has nothing to forfeit,
            // so back leaves immediately; a live one asks for a second press.
            if (matchOver || Time.unscaledTime - lastMatchBackTime <= BackConfirmSeconds)
            {
                OnLeaveMatchPressed();
                return;
            }

            lastMatchBackTime = Time.unscaledTime;
            ShowBackHint();
            return;
        }

        if ((createPanel != null && createPanel.activeSelf) ||
            (joinPanel != null && joinPanel.activeSelf))
        {
            CancelRoomAndLeave();
        }
        else if (pvpMenuPanel != null && pvpMenuPanel.activeSelf)
        {
            ClosePvpMenu();
        }
    }

    const float BackConfirmSeconds = 2f;
    float lastMatchBackTime = -10f;
    TMP_Text backHintLabel; // transient, built lazily on the match panel

    void ShowBackHint()
    {
        ShowConfirmHint("back_again_to_leave");
    }

    void ShowConfirmHint(string l10nKey)
    {
        if (backHintLabel == null)
        {
            backHintLabel = RuntimeUI.CreateTmpText(matchPanel.transform, "BackExitHint",
                "", 26, new Vector2(0f, -660f), new Vector2(860f, 60f),
                new Color(0.91f, 0.93f, 1f, 0.85f));
        }

        backHintLabel.text = L10n.Get(l10nKey);
        backHintLabel.gameObject.SetActive(true);

        CancelInvoke(nameof(HideBackHint));
        Invoke(nameof(HideBackHint), BackConfirmSeconds);
    }

    void HideBackHint()
    {
        if (backHintLabel != null)
            backHintLabel.gameObject.SetActive(false);
    }

    static void SetInteractable(UnityEngine.UI.Button button, bool on)
    {
        if (button != null)
            button.interactable = on;
    }

    void BeginMatchPolling()
    {
        matchOver = false;
        guessInFlight = false;
        localAcceptedGuessCount = 0;
        shownGuessKey = "";
        myHistory = "";
        theirHistory = "";
        silentPolls = 0;
        lastStateSignature = "";
        waitingStartTime = Time.unscaledTime;
        // A leave mid-request must not strand next match's buttons disabled.
        SetInteractable(createGoButton, true);
        SetInteractable(joinGoButton, true);
        SetGuessControls(true);
        client.OnRoomClosed = HandleRoomClosed;
        client.OnConnectionLost = HandleConnectionLost;
        var playFab = client as PlayFabPvpClient;
        if (playFab != null)
        {
            playFab.OnReconnecting = HandleReconnecting;
            playFab.OnReconnected = HandleReconnected;
        }
        client.StartPolling(OnState);
    }

    void HandleRoomClosed()
    {
        ShowTerminalStatus(L10n.Get("pvp_opponent_left"));
    }

    void HandleConnectionLost()
    {
        ShowTerminalStatus(L10n.Get("pvp_connection_lost"));
    }

    void HandleReconnecting()
    {
        if (matchOver) return;

        string message = L10n.Get("pvp_reconnecting");
        if (matchPanel.activeSelf)
            turnText.text = message;
        else if (createPanel.activeSelf)
            SetCreateStatus(message, true);
        else if (joinPanel.activeSelf)
            joinStatusText.text = message;
    }

    void HandleReconnected()
    {
        if (matchOver) return;

        // The match panel repaints from the next poll's OnState; the create
        // panel's waiting status has no poll-driven repaint, restore it here.
        if (createPanel.activeSelf && (lastState == null || lastState.phase == "waiting"))
            SetCreateStatus(L10n.Get("pvp_waiting"), true);
    }

    void ShowTerminalStatus(string message)
    {
        if (matchPanel.activeSelf)
        {
            matchOver = true;
            resultText.text = message;
            turnText.text = "";
            SetGuessControls(false);
        }
        else if (createPanel.activeSelf)
        {
            SetCreateStatus(message, false);
        }
        else if (joinPanel.activeSelf)
        {
            joinStatusText.text = message;
        }
    }

    void SetGuessControls(bool on)
    {
        SetInteractable(guessButton, on);
        if (guessInput != null)
            guessInput.interactable = on;
    }

    void OnState(PvpBackend.RoomState s)
    {
        if (s.phase == "closed")
        {
            client.StopPolling();
            HandleRoomClosed();
            return;
        }

        lastState = s;

        string me = client.IsHost ? "host" : "guest";
        string signature = s.phase + "|" + s.turn + "|" + s.lastBy + "|" +
                           s.lastGuess + "|" + s.winner + "|" + s.hostGuessCount +
                           "|" + s.guestGuessCount;
        if (signature != lastStateSignature)
        {
            lastStateSignature = signature;
            silentPolls = 0;
        }
        else if (s.phase == "play" && s.turn != me && !matchOver)
        {
            if (++silentPolls >= MaxSilentPolls)
            {
                silentPolls = 0;
                client.StopPolling();
                client.DeleteRoom();
                HandleConnectionLost();
                return;
            }
        }
        else silentPolls = 0;

        if (s.phase == "waiting")
        {
            if (Time.unscaledTime - waitingStartTime >= WaitingTimeoutSeconds)
            {
                client.StopPolling();
                client.DeleteRoom();
                lastState = null;
                roomCodeText.text = "-----";
                // Back at the create entry state: RoomCode is cleared, so the
                // player can mint a fresh room from this same panel.
                SetCreateStatus(L10n.Get("pvp_room_expired"), false);
            }
            return;
        }

        if (!matchPanel.activeSelf)
        {
            if (!createPanel.activeSelf && !joinPanel.activeSelf && !pvpMenuPanel.activeSelf)
            {
                client.StopPolling();
                client.DeleteRoom();
                return;
            }

            createPanel.SetActive(false);
            joinPanel.SetActive(false);
            pvpMenuPanel.SetActive(false);
            matchPanel.SetActive(true);
            resultText.text = "";
            historyText.text = "";
        }

        string opponentName = client.IsHost ? s.guestName : s.hostName;
        opponentNameText.text = L10n.Get("opponent_label", opponentName);

        string key = s.lastBy + ":" + s.lastGuess;
        if (s.lastBy != "" && s.lastGuess != 0 && key != shownGuessKey)
        {
            shownGuessKey = key;
            bool guessWasMine = (s.lastBy == me);
            string rawHint = s.lastHint;
            if (string.IsNullOrEmpty(rawHint))
            {
                int targetSecret = s.lastBy == "host" ? s.guestSecret : s.hostSecret;
                if (targetSecret > 0)
                    rawHint = s.lastGuess == targetSecret ? "correct"
                        : (s.lastGuess < targetSecret ? "higher" : "lower");
            }

            // One appended chain per player: "50>" = secret is higher than
            // 50, "50<" = lower, "50=" = found. ASCII on purpose — the
            // shipped fonts are not verified for arrow glyphs.
            string mark = rawHint == "higher" ? ">"
                : rawHint == "lower" ? "<"
                : rawHint == "correct" ? "=" : "";
            string entry = s.lastGuess + mark;
            if (guessWasMine)
                myHistory += (myHistory.Length > 0 ? "  " : "") + entry;
            else
                theirHistory += (theirHistory.Length > 0 ? "  " : "") + entry;

            string lines = "";
            if (myHistory.Length > 0)
                lines = L10n.Get("you") + ": " + myHistory;
            if (theirHistory.Length > 0)
                lines += (lines.Length > 0 ? "\n" : "") + opponentName + ": " + theirHistory;
            historyText.text = lines;
        }

        if (s.phase == "done" && !matchOver)
        {
            matchOver = true;
            client.StopPolling();
            SetGuessControls(false);

            bool iWon = s.winner == me;
            int authoritativeGuessCount = client.IsHost ? s.hostGuessCount : s.guestGuessCount;
            // The poll can deliver the finished state before the winning
            // submit's own response returns; that in-flight guess is mine and
            // accepted (the state says so), so count it.
            int localCount = localAcceptedGuessCount;
            if (guessInFlight && s.lastBy == me)
                localCount++;
            int myGuessCount = authoritativeGuessCount > 0 ? authoritativeGuessCount : localCount;
            int huntedSecret = s.revealedSecret > 0
                ? s.revealedSecret
                : (client.IsHost ? s.guestSecret : s.hostSecret);

            resultText.text = iWon
                ? L10n.Get("you_win") + "\n" + L10n.Get("won_in_guesses", myGuessCount)
                : L10n.Get("you_lose") + "\n" + L10n.Get("number_was", huntedSecret);
            turnText.text = "";

            Analytics.SetMatchMode("pvp");

            if (iWon)
            {
                GameStats.RecordWin(myGuessCount, false);
                Haptics.Success();
                GameEvents.MatchEnded(true, myGuessCount);
            }
            else
            {
                GameStats.RecordLoss(false);
                Haptics.Error();
                GameEvents.MatchEnded(false, 0);
            }
            if (audioSource != null)
            {
                var clip = iWon ? winSound : loseSound;
                if (clip != null) audioSource.PlayOneShot(clip);
            }
            if (iWon && winConfetti != null)
                winConfetti.Burst();

            client.AcknowledgeResult();
            return;
        }

        if (!matchOver)
        {
            bool myTurn = s.turn == me;
            turnText.text = myTurn ? L10n.Get("your_guess") : L10n.Get("opponent_thinking", opponentName);
        }
    }

    static bool TryReadSecret(TMP_InputField field, out int value)
    {
        value = 0;
        if (field == null) return false;
        if (!int.TryParse(field.text, out value)) return false;
        return value >= 1 && value <= 100;
    }
}
