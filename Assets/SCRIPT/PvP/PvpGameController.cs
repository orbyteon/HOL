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

    PvpBackend.RoomState lastState;
    string shownGuessKey = "";
    bool matchOver;

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
            createStatusText.text = "Enter your secret number (1-100) first";
            createPanel.SetActive(true);
            pvpMenuPanel.SetActive(false);
            return;
        }

        pvpMenuPanel.SetActive(false);
        createPanel.SetActive(true);
        createStatusText.text = "Creating room...";
        roomCodeText.text = "-----";

        client.CreateRoom(MyName, secret, (ok, codeOrError) =>
        {
            if (!ok)
            {
                createStatusText.text = "Could not create room. Check connection.";
                return;
            }
            roomCodeText.text = codeOrError;
            createStatusText.text = "Waiting for your challenger...";
            BeginMatchPolling();
        });
    }

    public void OnCopyInvitePressed()
    {
        if (string.IsNullOrEmpty(client.RoomCode)) return;

        GUIUtility.systemCopyBuffer =
            "Duel me in HOL — Higher or Lower! My room code: " + client.RoomCode;
        createStatusText.text = "Invite copied! Send it to a friend.";
    }

    public void OnJoinRoomPressed()
    {
        int secret;
        if (!TryReadSecret(joinSecretInput, out secret))
        {
            joinStatusText.text = "Enter your secret number (1-100)";
            return;
        }
        if (string.IsNullOrEmpty(joinCodeInput.text.Trim()))
        {
            joinStatusText.text = "Enter the room code";
            return;
        }

        joinStatusText.text = "Joining...";
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
            turnText.text = "Wait for your turn...";
            return;
        }

        int guess;
        if (!TryReadSecret(guessInput, out guess))
        {
            turnText.text = "Guess a number 1-100";
            return;
        }

        guessInput.text = "";
        turnText.text = "Sending...";
        client.SubmitGuess(guess, lastState, ok =>
        {
            if (!ok) turnText.text = "Network hiccup — try again";
        });
    }

    public void OnLeaveMatchPressed()
    {
        client.StopPolling();
        if (client.IsHost) client.DeleteRoom();
        matchOver = false;
        lastState = null;
        shownGuessKey = "";
        OpenPvpMenu();
    }

    // ---------------------------------------------------------- state handling

    void BeginMatchPolling()
    {
        matchOver = false;
        shownGuessKey = "";
        client.StartPolling(OnState);
    }

    void OnState(PvpBackend.RoomState s)
    {
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
        opponentNameText.text = "Opponent: " + opponentName;

        // show the latest guess with an honest, auto-computed hint
        string key = s.lastBy + ":" + s.lastGuess;
        if (s.lastBy != "" && s.lastGuess != 0 && key != shownGuessKey)
        {
            shownGuessKey = key;
            bool guessWasMine = (s.lastBy == (client.IsHost ? "host" : "guest"));
            int targetSecret = s.lastBy == "host" ? s.guestSecret : s.hostSecret;
            string who = guessWasMine ? "You" : opponentName;

            string hint;
            if (s.lastGuess == targetSecret) hint = "CORRECT!";
            else if (s.lastGuess < targetSecret) hint = "Higher";
            else hint = "Lower";

            historyText.text = who + ": " + s.lastGuess + "  →  " + hint;
        }

        if (s.phase == "done" && !matchOver)
        {
            matchOver = true;
            client.StopPolling();

            bool iWon = s.winner == (client.IsHost ? "host" : "guest");
            resultText.text = iWon ? "YOU WIN!" : "YOU LOSE!";
            turnText.text = "";

            if (iWon && winConfetti != null)
                winConfetti.Burst();
            return;
        }

        if (!matchOver)
        {
            bool myTurn = s.turn == (client.IsHost ? "host" : "guest");
            turnText.text = myTurn ? "Your guess" : opponentName + " is thinking...";
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
