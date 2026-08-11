using UnityEngine;
using TMPro;

// Drives the PvP flow end to end: create/join panels, invite code display,
// waiting state, the live match, and the result. Pure UI orchestration on
// top of PvpClient — no edits to the single-player scripts.
//
// Hints are automatic and always honest: when a guess arrives, each client
// compares it against the relevant secret from the room state. There are no
// Higher/Lower answer buttons in PvP.
public class PvpGameController : MonoBehaviour
{
    [Header("Wiring")]
    [Tooltip("Drag in either a PvpClient (Firebase) or PlayFabPvpClient (Azure PlayFab)")]
    public PvpBackend client;

    [Header("Panels")]
    public GameObject pvpMenuPanel;    // Create / Join choice
    public GameObject createPanel;     // shows room code + waiting status
    public GameObject joinPanel;       // code entry
    public GameObject matchPanel;      // the actual duel UI

    [Header("Create flow")]
    public TMP_InputField createSecretInput;
    public TMP_Text roomCodeText;
    public TMP_Text createStatusText;

    [Header("Join flow")]
    public TMP_InputField joinCodeInput;
    public TMP_InputField joinSecretInput;
    public TMP_Text joinStatusText;

    [Header("Match UI")]
    public TMP_InputField guessInput;
    public TMP_Text opponentNameText;
    public TMP_Text turnText;
    public TMP_Text historyText;   // last guess + hint
    public TMP_Text resultText;

    public ConfettiBurst winConfetti; // optional
    public AudioSource audioSource;   // optional, shared with solo
    public AudioClip winSound;        // optional
    public AudioClip loseSound;       // optional

    PvpBackend.RoomState lastState;
    string shownGuessKey = "";
    bool matchOver;
    int myGuessCount;

    string MyName => PlayerPrefs.GetString("PlayerName", "Player");

    // ---------------------------------------------------------- UI entry points

    public void OpenPvpMenu()
    {
        pvpMenuPanel.SetActive(true);
        createPanel.SetActive(false);
        joinPanel.SetActive(false);
        matchPanel.SetActive(false);
    }

    public void OnCreateRoomPressed()
    {
        int secret;
        if (!TryReadSecret(createSecretInput, out secret))
        {
            createStatusText.text = L10n.Get("pvp_secret");
            createPanel.SetActive(true);
            pvpMenuPanel.SetActive(false);
            return;
        }

        pvpMenuPanel.SetActive(false);
        createPanel.SetActive(true);
        createStatusText.text = L10n.Get("pvp_creating");
        roomCodeText.text = "-----";

        client.CreateRoom(MyName, secret, (ok, codeOrError) =>
        {
            if (!ok)
            {
                createStatusText.text = L10n.Get("pvp_network_error");
                return;
            }
            roomCodeText.text = codeOrError;
            createStatusText.text = L10n.Get("pvp_waiting");
            BeginMatchPolling();
        });
    }

    public void OnCopyInvitePressed()
    {
        if (string.IsNullOrEmpty(client.RoomCode)) return;

        GUIUtility.systemCopyBuffer = L10n.Get("pvp_invite_text", client.RoomCode);
        createStatusText.text = L10n.Get("pvp_invite_copied");
    }

    public void OnJoinRoomPressed()
    {
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
        client.JoinRoom(joinCodeInput.text, MyName, secret, (ok, error) =>
        {
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
        if (matchOver || lastState == null) return;

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

        guessInput.text = "";
        turnText.text = L10n.Get("pvp_sending");
        myGuessCount++;
        client.SubmitGuess(guess, lastState, ok =>
        {
            if (!ok) turnText.text = L10n.Get("pvp_network_error");
        });
    }

    public void OnLeaveMatchPressed()
    {
        client.StopPolling();
        // Either side leaving closes the room: Firebase deletes it (the other
        // poller sees it vanish), PlayFab marks phase "closed".
        client.DeleteRoom();
        matchOver = false;
        lastState = null;
        shownGuessKey = "";
        OpenPvpMenu();
    }

    // ---------------------------------------------------------- state handling

    void BeginMatchPolling()
    {
        matchOver = false;
        myGuessCount = 0;
        shownGuessKey = "";
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

    // Match-ending status that works from whichever panel is visible.
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
            createStatusText.text = message;
        }
        else if (joinPanel.activeSelf)
        {
            joinStatusText.text = message;
        }
    }

    void OnState(PvpBackend.RoomState s)
    {
        // PlayFab rooms can't be deleted; a leaver marks them closed instead.
        if (s.phase == "closed")
        {
            client.StopPolling();
            HandleRoomClosed();
            return;
        }

        lastState = s;

        // still waiting for a guest?
        if (s.phase == "waiting")
            return;

        // first transition into the match
        if (!matchPanel.activeSelf)
        {
            createPanel.SetActive(false);
            joinPanel.SetActive(false);
            pvpMenuPanel.SetActive(false);
            matchPanel.SetActive(true);
            resultText.text = "";
            historyText.text = "";
        }

        string opponentName = client.IsHost ? s.guestName : s.hostName;
        opponentNameText.text = L10n.Get("opponent_label", opponentName);

        // show the latest guess with an honest, auto-computed hint
        string key = s.lastBy + ":" + s.lastGuess;
        if (s.lastBy != "" && s.lastGuess != 0 && key != shownGuessKey)
        {
            shownGuessKey = key;
            bool guessWasMine = (s.lastBy == (client.IsHost ? "host" : "guest"));
            int targetSecret = s.lastBy == "host" ? s.guestSecret : s.hostSecret;
            string who = guessWasMine ? L10n.Get("you") : opponentName;

            string hint;
            if (s.lastGuess == targetSecret) hint = L10n.Get("correct") + "!";
            else if (s.lastGuess < targetSecret) hint = L10n.Get("higher");
            else hint = L10n.Get("lower");

            historyText.text = who + ": " + s.lastGuess + "  →  " + hint;
        }

        if (s.phase == "done" && !matchOver)
        {
            matchOver = true;
            client.StopPolling();

            bool iWon = s.winner == (client.IsHost ? "host" : "guest");
            resultText.text = iWon
                ? L10n.Get("you_win") + "\n" + L10n.Get("won_in_guesses", myGuessCount)
                : L10n.Get("you_lose");
            turnText.text = "";

            // Same endgame treatment as solo: stats, stinger, haptic, confetti.
            if (iWon)
            {
                GameStats.RecordWin(myGuessCount);
                Haptics.Success();
                GameEvents.MatchEnded(true, myGuessCount);
            }
            else
            {
                GameStats.RecordLoss();
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
            return;
        }

        if (!matchOver)
        {
            bool myTurn = s.turn == (client.IsHost ? "host" : "guest");
            turnText.text = myTurn ? L10n.Get("your_guess") : L10n.Get("opponent_thinking", opponentName);
        }
    }

    // ---------------------------------------------------------- helpers

    static bool TryReadSecret(TMP_InputField field, out int value)
    {
        value = 0;
        if (field == null) return false;
        if (!int.TryParse(field.text, out value)) return false;
        return value >= 1 && value <= 100;
    }
}
