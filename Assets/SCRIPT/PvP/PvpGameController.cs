using UnityEngine;
using TMPro;

// Drives the PvP flow end to end: create/join panels, invite code display,
// waiting state, the live match, and the result. Pure UI orchestration on
// top of PvpBackend — no edits to the single-player scripts.
//
// PlayFab hints and result counts come from the server-authoritative room
// view; no client-writable fallback participates in the match.
public class PvpGameController : MonoBehaviour
{
    [Header("Wiring")]
    [Tooltip("Drag in the server-authoritative PlayFab PvP client")]
    public PvpBackend client;

    [Header("Panels")]
    public GameObject pvpMenuPanel;
    public GameObject createPanel;
    public GameObject joinPanel;
    public GameObject matchPanel;

    [Header("Create flow")]
    public TMP_InputField createSecretInput;
    public GameObject createConfirmButton;
    public TMP_Text roomCodeText;
    public TMP_Text createStatusText;
    public AnimatedEllipsis createStatusEllipsis;

    [Header("Join flow")]
    public TMP_InputField joinCodeInput;
    public TMP_InputField joinSecretInput;
    public GameObject joinConfirmButton;
    public TMP_Text joinStatusText;

    [Header("Match UI")]
    public TMP_InputField guessInput;
    public GameObject guessButton;
    public TMP_Text opponentNameText;
    // Optional: the Consumer First banner's round counter; null-safe everywhere.
    public TMP_Text roundText;
    public TMP_Text turnText;
    public TMP_Text historyText;
    public TMP_Text resultText;
    public PvpResultPresentation resultPresentation;

    [Header("Duel rules UI")]
    // How far the player has narrowed the opponent's number, plus the Lock.
    // Both are optional and are shown only when the server-authoritative backend
    // exposes the corresponding controls.
    public TMP_Text rangeText;
    public GameObject lockButton;
    public TMP_Text lockButtonLabel;

    [Header("Signals")]
    public GameObject signalsRoot;
    public GameObject resultSignalsRoot;
    public TMP_Text signalFeedText;
    public TMP_Text resultSignalFeedText;

    [Header("Rematch")]
    // Offered on the result screen so friends can play again without swapping a
    // new invite code. Needs the server-authoritative backend.
    public GameObject rematchButton;
    public TMP_InputField rematchSecretInput;
    public TMP_Text rematchStatusText;

    public ConfettiBurst winConfetti;
    public AudioSource audioSource;
    public AudioClip winSound;
    public AudioClip loseSound;

    PvpBackend.RoomState lastState;
    string shownGuessKey = "";
    bool matchOver;
    bool guessInFlight;

    // The player's own narrowing interval on the opponent's number. PvP used to
    // show no range at all while solo play did; it is also what tells us how
    // many candidates are left, and therefore whether the Lock is worth
    // offering.
    int myMin = 1;
    int myMax = 100;
    bool lockArmed;

    int lastSignalSeq;
    int signalsSent;

    // The Lock is revealed once the player has played a round, and explains
    // itself the first time it appears. See LockIntro.
    bool lockRevealedThisMatch;

    int lastMatchIndex;
    bool rematchInFlight;
    int donePolls;
    // A finished room is held open for a rematch, but not forever: at a 1.5s
    // poll this is about two minutes before the room is released.
    const int MaxDonePolls = 80;

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
        if (resultPresentation != null) resultPresentation.Hide();
        if (createSecretInput != null) createSecretInput.gameObject.SetActive(true);
        if (createConfirmButton != null) createConfirmButton.SetActive(true);
        if (joinCodeInput != null) joinCodeInput.gameObject.SetActive(true);
        if (joinSecretInput != null) joinSecretInput.gameObject.SetActive(true);
        if (joinConfirmButton != null) joinConfirmButton.SetActive(true);
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
            if (createSecretInput != null) createSecretInput.gameObject.SetActive(false);
            if (createConfirmButton != null) createConfirmButton.SetActive(false);
            SetCreateStatus(L10n.Get("prebattle_waiting"), true);
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
            if (joinCodeInput != null) joinCodeInput.gameObject.SetActive(false);
            if (joinSecretInput != null) joinSecretInput.gameObject.SetActive(false);
            if (joinConfirmButton != null) joinConfirmButton.SetActive(false);
            joinStatusText.text = L10n.Get("prebattle_waiting");
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
        bool staked = lockArmed;
        guessInput.text = "";
        turnText.text = L10n.Get("pvp_sending");
        guessInFlight = true;
        int gen = flowGeneration;
        client.SubmitGuess(guess, staked, lastState, ok =>
        {
            if (gen != flowGeneration) return;

            guessInFlight = false;
            if (ok)
            {
                lockArmed = false;
                if (staked) LockIntro.MarkUsed();
                NarrowMyRange(guess, lastState != null ? lastState.lastHint : "");

                // Render the server's answer straight away rather than leaving
                // the player staring at "sending" for a poll interval. This
                // also covers a winning guess, whose result the loser's leave
                // could otherwise beat to the room.
                if (lastState != null && !matchOver)
                    OnState(lastState);
            }
            else
            {
                guessInput.text = typed;
                turnText.text = L10n.Get("pvp_network_error");
                RefreshLockButton();
            }
        });
    }

    // Arms or disarms the Lock for the guess about to be sent. Nothing goes to
    // the server until the guess itself does, so the opponent cannot see it
    // coming.
    public void OnLockTogglePressed()
    {
        if (matchOver || lastState == null || guessInFlight) return;
        if (!client.IsServerAuthoritative) return;

        string me = client.IsHost ? "host" : "guest";
        if (lastState.LockUsedBy(me) || lastState.phase != "play") return;

        lockArmed = !lockArmed;
        Haptics.Light();
        RefreshLockButton();
    }

    public void OnSignalPressed(int signalId)
    {
        if (lastState == null || !Signals.IsValid(signalId)) return;
        if (!client.IsServerAuthoritative) return;
        if (signalsSent >= Signals.CapPerSide)
        {
            SetSignalFeed(L10n.Get("signal_limit"));
            return;
        }

        signalsSent++;
        RefreshSignalsAvailability();

        // Show it locally straight away — the sender should never wait a poll
        // interval to see their own message land.
        ShowSignalLine(L10n.Get("you"), signalId);
        int sentMatchIndex = lastState.matchIndex;
        client.SendSignal(signalId, ok =>
        {
            if (ok || lastState == null ||
                lastState.matchIndex != sentMatchIndex)
                return;

            if (signalsSent > 0) signalsSent--;
            SetSignalFeed(L10n.Get("pvp_network_error"));
            RefreshSignalsAvailability();
        });
    }

    // Commits a fresh secret for another match in this room. The server deals
    // the next match only once both players have committed.
    public void OnRematchPressed()
    {
        if (!matchOver || lastState == null || rematchInFlight) return;
        if (!client.IsServerAuthoritative || lastState.opponentLeft) return;

        int secret;
        if (!TryReadSecret(rematchSecretInput, out secret))
        {
            SetRematchStatus(L10n.Get("pvp_secret"));
            return;
        }

        rematchInFlight = true;
        SetRematchStatus(L10n.Get("rematch_waiting"));

        int gen = flowGeneration;
        client.RequestRematch(secret, ok =>
        {
            if (gen != flowGeneration) return;

            rematchInFlight = false;
            if (ok) donePolls = 0;
            else SetRematchStatus(L10n.Get("pvp_network_error"));
        });
    }

    public void OnLeaveMatchPressed()
    {
        flowGeneration++;
        client.StopPolling();
        client.DeleteRoom();
        matchOver = false;
        guessInFlight = false;
        rematchInFlight = false;
        lastState = null;
        shownGuessKey = "";
        ShowRematchOffer(false);
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
        shownGuessKey = "";
        silentPolls = 0;
        lastStateSignature = "";
        myMin = 1;
        myMax = 100;
        lockArmed = false;
        lockRevealedThisMatch = false;
        lastSignalSeq = 0;
        signalsSent = 0;
        lastMatchIndex = 0;
        rematchInFlight = false;
        donePolls = 0;
        ShowRematchOffer(false);
        UpdateRangeText();
        RefreshLockButton();
        RefreshSignalsAvailability();
        if (signalFeedText != null) signalFeedText.text = "";
        if (resultSignalFeedText != null) resultSignalFeedText.text = "";
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
        string them = client.IsHost ? "guest" : "host";
        string signature = s.phase + "|" + s.turn + "|" + s.lastBy + "|" +
                           s.lastGuess + "|" + s.winner + "|" + s.hostGuessCount +
                           "|" + s.guestGuessCount + "|" + s.matchIndex + "|" +
                           s.iWantRematch + "|" + s.theyWantRematch;
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

        // Both sides committed a new secret, so the room dealt a fresh match in
        // place. Everything local to the old one has to go.
        if (matchOver && s.phase == "play" && s.matchIndex != lastMatchIndex)
        {
            lastMatchIndex = s.matchIndex;
            BeginRematchedMatch();
        }

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
            if (resultPresentation != null) resultPresentation.Hide();
            resultText.text = "";
            historyText.text = "";
        }

        string opponentName = client.IsHost ? s.guestName : s.hostName;
        opponentNameText.text = L10n.Get("opponent_label", opponentName);

        // Optional HUD: the live round number, blank once the match is over so
        // the result banner has the slot to itself.
        if (roundText != null)
            roundText.text = s.phase == "play"
                ? L10n.Get("round_label_open", s.roundIndex + 1)
                : "";

        string key = s.lastBy + ":" + s.lastGuess;
        if (s.lastBy != "" && s.lastGuess != 0 && key != shownGuessKey)
        {
            shownGuessKey = key;
            bool guessWasMine = (s.lastBy == me);
            string who = guessWasMine ? L10n.Get("you") : opponentName;
            string hint = LocalizedHint(s.lastHint);
            if (s.lastLocked) who += " [" + L10n.Get("lock_armed") + "]";
            historyText.text = who + ": " + s.lastGuess + "  →  " + hint;

            if (guessWasMine)
                NarrowMyRange(s.lastGuess, s.lastHint);
            if (s.lastLocked && s.lastHint != "correct")
                historyText.text += "\n" + (guessWasMine
                    ? L10n.Get("lock_missed")
                    : L10n.Get("opponent_forfeits", opponentName));
        }

        ShowIncomingSignal(s, opponentName);

        if (s.phase == "done" && !matchOver)
        {
            matchOver = true;

            // Keep polling so the room can hear a rematch offer. Backends that
            // cannot deal one release the room immediately, as before.
            bool canRematch = client.IsServerAuthoritative;
            if (!canRematch) client.StopPolling();

            bool isDraw = s.winner == "draw";
            bool iWon = !isDraw && s.winner == me;
            int myGuessCount = s.GuessCountFor(me);
            int opponentGuessCount = s.GuessCountFor(them);
            int huntedSecret = s.revealedSecret;

            if (isDraw)
                resultText.text = L10n.Get("you_draw") + "\n" +
                                  L10n.Get("draw_in_guesses", myGuessCount) + "\n" +
                                  L10n.Get("draw_tip");
            else
                resultText.text = iWon
                    ? L10n.Get("you_win") + "\n" + L10n.Get("won_in_guesses", myGuessCount)
                    : L10n.Get("you_lose") + "\n" + L10n.Get("number_was", huntedSecret);
            turnText.text = "";
            lockArmed = false;
            RefreshLockButton();

            if (isDraw)
            {
                // Neither a win nor a loss: the streak survives, and the
                // win/lose analytics event would misreport it, so MatchCompleted
                // routes it to the stats listeners alone.
                GameStats.RecordDraw();
                Haptics.Light();
                GameEvents.MatchCompleted(PvpOutcome(s, MatchOutcome.Result.Draw, myGuessCount));
            }
            else if (iWon)
            {
                GameStats.RecordWin(myGuessCount, false);
                Haptics.Success();
                GameEvents.MatchCompleted(PvpOutcome(s, MatchOutcome.Result.Win, myGuessCount));
            }
            else
            {
                GameStats.RecordLoss(false);
                Haptics.Error();
                GameEvents.MatchCompleted(PvpOutcome(s, MatchOutcome.Result.Loss, myGuessCount));
            }
            if (resultPresentation != null)
            {
                string title = isDraw
                    ? L10n.Get("result_draw_title")
                    : iWon
                        ? L10n.Get("result_win_title")
                        : L10n.Get("result_loss_title");
                resultPresentation.Show(title, myGuessCount,
                    opponentGuessCount, huntedSecret);
            }
            RefreshSignalsAvailability();
            if (audioSource != null && !isDraw)
            {
                var clip = iWon ? winSound : loseSound;
                if (clip != null) audioSource.PlayOneShot(clip);
            }
            if (iWon && winConfetti != null)
                winConfetti.Burst();

            if (canRematch)
            {
                donePolls = 0;
                lastMatchIndex = s.matchIndex;
                if (rematchSecretInput != null) rematchSecretInput.text = "";
                SetRematchStatus("");
                ShowRematchOffer(true);
            }
            else
            {
                client.AcknowledgeResult();
            }
            return;
        }

        // Later polls on a finished room carry the rematch handshake.
        if (matchOver && s.phase == "done")
        {
            HandleRematchState(s, opponentName);
            return;
        }

        if (!matchOver)
        {
            UpdateTurnText(s, me, opponentName);
            UpdateRangeText();
            RefreshLockButton();
        }
    }

    // The turn line is where the new rules become legible: that a correct guess
    // is only provisional, and that the answering guess is the last one.
    void UpdateTurnText(PvpBackend.RoomState s, string me, string opponentName)
    {
        bool myTurn = s.turn == me;

        if (s.IsMatchPointAgainst(me))
        {
            turnText.text = myTurn
                ? L10n.Get("match_point")
                : L10n.Get("opponent_thinking", opponentName);
            return;
        }

        if (!string.IsNullOrEmpty(s.pendingWin) && s.pendingWin == me)
        {
            turnText.text = L10n.Get("match_point_yours", opponentName);
            return;
        }

        if (!myTurn && s.ForfeitPendingFor(me))
        {
            turnText.text = L10n.Get("turn_forfeited");
            return;
        }

        turnText.text = myTurn ? L10n.Get("your_guess") : L10n.Get("opponent_thinking", opponentName);
    }

    // The guess count is passed in rather than re-read: the caller has already
    // reconciled the authoritative count against the local one for the case
    // where the winning guess is still in flight when "done" arrives.
    MatchOutcome PvpOutcome(PvpBackend.RoomState s, MatchOutcome.Result result, int myGuessCount)
    {
        string me = client.IsHost ? "host" : "guest";
        string them = client.IsHost ? "guest" : "host";

        return new MatchOutcome
        {
            PlayMode = MatchOutcome.Mode.Pvp,
            Outcome = result,
            Guesses = myGuessCount,
            OpponentGuesses = s.GuessCountFor(them),
            Opened = s.opener == me,
            LockStaked = s.LockUsedBy(me),
            RematchIndex = s.matchIndex,
            AppVersion = Application.version,
        };
    }

    // ------------------------------------------------------------- rematch UI

    void HandleRematchState(PvpBackend.RoomState s, string opponentName)
    {
        if (!client.IsServerAuthoritative) return;

        if (s.opponentLeft)
        {
            ShowRematchOffer(false);
            SetRematchStatus(L10n.Get("rematch_closed"));
            ReleaseFinishedRoom();
            return;
        }

        if (s.iWantRematch)
            SetRematchStatus(L10n.Get("rematch_waiting"));
        else if (s.theyWantRematch)
            SetRematchStatus(L10n.Get("rematch_offered", opponentName));

        // Nobody is coming back. Let the room go rather than holding server
        // state open for a player who has put the phone down — with a longer
        // grace period when we are the ones waiting on an answer.
        int limit = s.iWantRematch ? MaxDonePolls * 2 : MaxDonePolls;
        if (++donePolls >= limit)
        {
            ShowRematchOffer(false);
            SetRematchStatus(L10n.Get("rematch_closed"));
            ReleaseFinishedRoom();
        }
    }

    void ReleaseFinishedRoom()
    {
        client.StopPolling();
        client.AcknowledgeResult();
    }

    // The room dealt a new match in place: clear everything that described the
    // old one, keeping the connection and the opponent.
    void BeginRematchedMatch()
    {
        matchOver = false;
        guessInFlight = false;
        rematchInFlight = false;
        shownGuessKey = "";
        silentPolls = 0;
        donePolls = 0;
        myMin = 1;
        myMax = 100;
        lockArmed = false;
        lockRevealedThisMatch = false;
        signalsSent = 0; // the server grants a fresh allowance per match

        ShowRematchOffer(false);
        SetRematchStatus("");
        if (resultPresentation != null) resultPresentation.Hide();
        if (resultText != null) resultText.text = "";
        if (historyText != null) historyText.text = "";
        if (signalFeedText != null) signalFeedText.text = "";
        if (resultSignalFeedText != null) resultSignalFeedText.text = "";
        if (guessInput != null) guessInput.text = "";

        UpdateRangeText();
        RefreshLockButton();
        RefreshSignalsAvailability();
    }

    void ShowRematchOffer(bool visible)
    {
        if (rematchButton != null) rematchButton.SetActive(visible);
        if (rematchSecretInput != null) rematchSecretInput.gameObject.SetActive(visible);

        // The guess controls are dead once the match is decided, so the rematch
        // controls take their slot rather than sitting beside a field that can
        // no longer be used. They come back only for a live match.
        bool matchLive = !matchOver;
        if (guessInput != null) guessInput.gameObject.SetActive(!visible && matchLive);
        if (guessButton != null) guessButton.SetActive(!visible && matchLive);

        if (!visible) SetRematchStatus("");
    }

    void SetRematchStatus(string message)
    {
        if (rematchStatusText != null)
            rematchStatusText.text = message;
    }

    // ---------------------------------------------------------- duel rules UI

    void NarrowMyRange(int guess, string hint)
    {
        if (hint == "higher" && guess + 1 > myMin) myMin = guess + 1;
        else if (hint == "lower" && guess - 1 < myMax) myMax = guess - 1;
        UpdateRangeText();
    }

    int CandidatesLeft()
    {
        return myMax >= myMin ? myMax - myMin + 1 : 0;
    }

    void UpdateRangeText()
    {
        if (rangeText == null) return;

        if (matchOver || lastState == null || lastState.phase != "play")
        {
            rangeText.text = "";
            return;
        }

        rangeText.text = L10n.Get("between_range", myMin, myMax);
    }

    // The Lock button doubles as the tutorial for the mechanic: once the range
    // is down to a few candidates it stops saying "LOCK" and starts asking.
    // Two players who never lock draw roughly a quarter of their duels, so the
    // prompt is what keeps the draw rate down in practice.
    void RefreshLockButton()
    {
        if (lockButton == null) return;

        string me = client != null && client.IsHost ? "host" : "guest";

        // Not offered until the player has taken a turn: a first duel should
        // feel like plain higher-or-lower, and staking the Lock on an opening
        // guess is a trap rather than a choice.
        bool revealed = lastState != null && lastState.GuessCountFor(me) > 0;
        bool available = client != null && client.IsServerAuthoritative &&
                         !matchOver && lastState != null && revealed &&
                         lastState.phase == "play" && !lastState.LockUsedBy(me);

        lockButton.SetActive(available);
        if (!available) return;

        if (!lockRevealedThisMatch)
        {
            lockRevealedThisMatch = true;
            if (LockIntro.ShouldExplain && rangeText != null)
            {
                // One line, in the secondary slot, replaced by the range again
                // on the next guess.
                rangeText.text = L10n.Get("lock_hint");
                LockIntro.MarkExplained();
            }
        }

        if (lockButtonLabel == null) return;

        int left = CandidatesLeft();
        if (lockArmed)
            lockButtonLabel.text = L10n.Get("lock_armed");
        else if (DuelRules.ShouldSuggestLock(left, true))
            lockButtonLabel.text = L10n.Get("lock_suggest", left);
        else
            lockButtonLabel.text = L10n.Get("lock");
    }

    void RefreshSignalsAvailability()
    {
        bool available = client != null && client.IsServerAuthoritative &&
                         signalsSent < Signals.CapPerSide;
        if (signalsRoot != null)
            signalsRoot.SetActive(available && !matchOver);
        if (resultSignalsRoot != null)
            resultSignalsRoot.SetActive(available && matchOver);
    }

    void ShowSignalLine(string who, int signalId)
    {
        SetSignalFeed(L10n.Get("signal_from", who, Signals.Text(signalId)));
    }

    void SetSignalFeed(string line)
    {
        if (signalFeedText != null) signalFeedText.text = line;
        if (resultSignalFeedText != null) resultSignalFeedText.text = line;
    }

    void ShowIncomingSignal(PvpBackend.RoomState s, string opponentName)
    {
        if (s.signalSeq <= lastSignalSeq) return;
        lastSignalSeq = s.signalSeq;

        // The sender already saw their own line the moment they tapped.
        if (s.signalBy == (client.IsHost ? "host" : "guest")) return;

        ShowSignalLine(opponentName, s.signalId);
        Haptics.Light();
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
