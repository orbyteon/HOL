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
    public AnimatedEllipsis createStatusEllipsis; // optional: animates the waiting states

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
    bool guessInFlight;
    int myGuessCount;

    // Bumped on every leave/cancel/close. Create/join callbacks capture the
    // value at request time and bail — closing the room they just made —
    // when it has moved: the player backed out while the request was in
    // flight, and polling an explicitly-cancelled room would hijack them
    // into a match (or poll invisibly forever, stranding the opponent).
    int flowGeneration;
    // Double-tap guard for Create/Join (guessInFlight's equivalent). Cleared
    // only by the callback, never by cancel: two room requests can never be
    // in flight at once, so a stale callback can't close a newer flow's room.
    bool joinCreateInFlight;
    // Opponent-inactivity watchdog: consecutive polls with zero state change
    // while it is the opponent's turn. Leaving closes the room with a
    // fire-and-forget write; if that write fails (going offline is the usual
    // reason to leave), the room stays "play" and we'd poll a dead room
    // forever without this.
    string lastStateSignature = "";
    int silentPolls;
    // ~5 minutes at the 1.5s poll interval. Generous on purpose: a slow
    // thinker must never be punished, only a genuinely abandoned room.
    const int MaxSilentPolls = 200;

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
        // A live room means the invite code is already out in the world —
        // re-creating would orphan it and strand the joining friend. Both
        // backends clear RoomCode in DeleteRoom, so every leave/cancel path
        // re-enables this. (Reachable via the Confirm button or the secret
        // field's keyboard-submit while waiting.)
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
                // Backed out mid-flight: close the room we just created
                // instead of polling it (safe no-op if the request failed).
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

        // The copy confirmation is transient — fall back to the animated
        // waiting line so the panel doesn't look stalled on old text.
        CancelInvoke(nameof(ResumeWaitingStatus));
        Invoke(nameof(ResumeWaitingStatus), 2.5f);
    }

    // Restores "Waiting for your challenger..." after the copy confirmation,
    // but only if we are in fact still waiting on this panel.
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

    // Single entry point for the create-panel status line: static messages
    // stop the ellipsis animation so it can't overwrite them; waiting-style
    // messages animate trailing dots (a static "Waiting..." over the whole
    // invite handshake — the longest wait in the game — looked frozen).
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
        // Same guard as create: already in a room → a stray re-submit
        // (button or keyboard Done) must not join again.
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
                // Backed out mid-flight: leave the room we just joined
                // instead of polling it (safe no-op if the request failed).
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
        // guessInFlight: the button stays tappable until the next poll, so a
        // fast second tap would overwrite the guess and inflate the count.
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
        client.SubmitGuess(guess, lastState, ok =>
        {
            guessInFlight = false;
            if (ok) myGuessCount++; // count only guesses the room accepted
            else
            {
                // Same rule as solo: a guess the room never accepted is kept
                // in the input so the player can retry, not retype.
                guessInput.text = typed;
                turnText.text = L10n.Get("pvp_network_error");
            }
        });
    }

    public void OnLeaveMatchPressed()
    {
        flowGeneration++; // invalidate any create/join callback still in flight
        client.StopPolling();
        // Either side leaving closes the room: Firebase deletes it (the other
        // poller sees it vanish), PlayFab marks phase "closed".
        client.DeleteRoom();
        matchOver = false;
        lastState = null;
        shownGuessKey = "";
        OpenPvpMenu();
    }

    // Backing out of the create/join flow: same teardown as leaving a match
    // (DeleteRoom is a safe no-op when no room exists), then the PvP menu.
    // Without this a late joiner would hijack the screen after we navigated
    // away while the room and poller were still live.
    public void CancelRoomAndLeave()
    {
        OnLeaveMatchPressed();
    }

    // Close button on the PvP menu: tear down any live room/polling first,
    // then hide the whole PvP menu.
    public void ClosePvpMenu()
    {
        OnLeaveMatchPressed();
        if (pvpMenuPanel != null)
            pvpMenuPanel.SetActive(false);
    }

    void Update()
    {
        // Android back button on the PvP panels (Escape in the editor).
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (matchPanel != null && matchPanel.activeSelf)
            return; // mid-match: the Leave button closes the room cleanly

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

    // ---------------------------------------------------------- state handling

    void BeginMatchPolling()
    {
        matchOver = false;
        guessInFlight = false;
        myGuessCount = 0;
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
            SetCreateStatus(message, false);
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

        // Opponent-inactivity watchdog. Leaving closes the room with a
        // fire-and-forget write; when that write fails (the leaver went
        // offline — the usual reason to leave), the room stays "play" and
        // without this we'd poll a dead room forever. Only counted on the
        // opponent's turn, so a slow thinker on our side never triggers it.
        string me = client.IsHost ? "host" : "guest";
        string signature = s.phase + "|" + s.turn + "|" + s.lastBy + "|" + s.lastGuess + "|" + s.winner;
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
                client.DeleteRoom(); // best-effort cleanup of the dead room
                HandleConnectionLost();
                return;
            }
        }
        else silentPolls = 0;

        // still waiting for a guest?
        if (s.phase == "waiting")
            return;

        // first transition into the match — only while the player is still
        // in the PvP flow. If they backed out (all PvP panels inactive), a
        // late joiner must not force the match UI over the main menu — and
        // the orphaned poller must die here, not run silently forever.
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
            // On a loss, reveal the number we were hunting (the opponent's
            // secret) — same closure the solo endgame gives.
            int huntedSecret = client.IsHost ? s.guestSecret : s.hostSecret;
            resultText.text = iWon
                ? L10n.Get("you_win") + "\n" + L10n.Get("won_in_guesses", myGuessCount)
                : L10n.Get("you_lose") + "\n" + L10n.Get("number_was", huntedSecret);
            turnText.text = "";

            // Same endgame treatment as solo: stats, stinger, haptic, confetti.
            // countRecent: false — PvP results must not re-tune the solo
            // adaptive AI's recent-win-rate window.
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
