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

    PvpBackend.RoomState lastState;
    string shownGuessKey = "";
    bool matchOver;
    bool guessInFlight;
    int localAcceptedGuessCount;

    int flowGeneration;
    bool joinCreateInFlight;
    string lastStateSignature = "";
    int silentPolls;
    const int MaxSilentPolls = 200;

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
        int gen = flowGeneration;
        client.CreateRoom(MyName, secret, (ok, codeOrError) =>
        {
            joinCreateInFlight = false;
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
        int gen = flowGeneration;
        client.JoinRoom(joinCodeInput.text, MyName, secret, (ok, error) =>
        {
            joinCreateInFlight = false;
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
        int gen = flowGeneration;
        client.SubmitGuess(guess, lastState, ok =>
        {
            if (gen != flowGeneration) return;

            guessInFlight = false;
            if (ok)
            {
                localAcceptedGuessCount++;
                // A winning guess ends the match in the submit response itself;
                // don't leave the winner staring at "sending" until the next poll
                // (which the loser's leave may beat to the room).
                if (lastState != null && lastState.phase == "done" && !matchOver)
                    OnState(lastState);
            }
            else
            {
                guessInput.text = typed;
                turnText.text = L10n.Get("pvp_network_error");
            }
        });
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
            return;

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

    void BeginMatchPolling()
    {
        matchOver = false;
        guessInFlight = false;
        localAcceptedGuessCount = 0;
        shownGuessKey = "";
        silentPolls = 0;
        lastStateSignature = "";
        client.OnRoomClosed = HandleRoomClosed;
        client.OnConnectionLost = HandleConnectionLost;
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

    void ShowTerminalStatus(string message)
    {
        if (matchPanel.activeSelf)
        {
            matchOver = true;
            resultText.text = message;
            turnText.text = "";
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
            return;

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
            string who = guessWasMine ? L10n.Get("you") : opponentName;
            string hint = LocalizedHint(s.lastHint);
            if (string.IsNullOrEmpty(hint))
            {
                int targetSecret = s.lastBy == "host" ? s.guestSecret : s.hostSecret;
                if (targetSecret > 0)
                    hint = s.lastGuess == targetSecret ? L10n.Get("correct") + "!"
                        : (s.lastGuess < targetSecret ? L10n.Get("higher") : L10n.Get("lower"));
            }
            historyText.text = who + ": " + s.lastGuess + "  →  " + hint;
        }

        if (s.phase == "done" && !matchOver)
        {
            matchOver = true;
            client.StopPolling();

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

    static string LocalizedHint(string hint)
    {
        if (hint == "correct") return L10n.Get("correct") + "!";
        if (hint == "higher") return L10n.Get("higher");
        if (hint == "lower") return L10n.Get("lower");
        return "";
    }

    static bool TryReadSecret(TMP_InputField field, out int value)
    {
        value = 0;
        if (field == null) return false;
        if (!int.TryParse(field.text, out value)) return false;
        return value >= 1 && value <= 100;
    }
}
