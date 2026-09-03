using UnityEngine;
using TMPro;

// Solo duel against the on-device AI. Turn order, rounds and the Lock all live
// in DuelRules, which the PvP backends mirror — so a player learns one set of
// rules here and meets the same ones in a real duel.
public class GameManager : MonoBehaviour
{
    // Disclosure hook: show this line in-game / store listing.
    public const string SimulatedOpponentDisclosure = "Opponents are simulated by an on-device AI.";

    // AI difficulty (PlayerPrefs "AIDifficulty"): 0 = Easy, 1 = Normal, 2 = Hard,
    // 3 = Adaptive (tunes itself from the player's recent win rate, see
    // AdaptiveRandomChance). The value is the chance the AI guesses randomly
    // instead of taking the binary-search midpoint — higher means a weaker,
    // more human opponent.
    const string DifficultyPrefKey = "AIDifficulty";
    static readonly float[] DifficultyRandomChance = { 0.6f, 0.2f, 0f, -1f };

    // The player is always the host seat; nothing in the rules depends on which
    // seat is which, only on who opens, and that is a coin flip each match.
    const DuelRules.Side PlayerSide = DuelRules.Side.Host;
    const DuelRules.Side AiSide = DuelRules.Side.Guest;

    public TMP_Text aiNumberText;
    public TMP_Text aiAnswerText;
    public TMP_Text turnText;
    public TMP_Text opponentNameText;

    // Runtime bindings populated by SoloDuelVisuals. The presenter is the
    // only writer of these fields; they stay public because the current visual
    // polish layer reads their references without owning their content.
    public TMP_Text rangeText;
    public TMP_Text playerHistoryText;
    public TMP_Text aiHistoryText;

    public GameObject higherButton;
    public GameObject lowerButton;
    public GameObject correctButton;
    public GameObject stopGameButton;

    public AudioSource audioSource;
    public AudioClip winSound;
    public AudioClip loseSound;

    public NumberManager numberManager;
    public ConfettiBurst winConfetti;
    public AdsManager adsManager;

    // The player may guess only on their own turn, and only once they have
    // answered the opponent's outstanding guess.
    public bool IsPlayerTurn => matchSetUp && !rules.Finished &&
                                rules.Turn == PlayerSide && !awaitingAnswer &&
                                presentationPhase == SoloBoardPhase.PlayerGuess;

    // "This match is decided, stop taking input." NumberManager gates the whole
    // submit path on this, so it MUST go false again once RestartMatch clears
    // the board — otherwise the player can never enter a number for the next
    // match and solo becomes a one-match-per-launch game. That is why it is not
    // simply rules.Finished: the rules object stays finished until the next
    // StartMatch, which does not happen until a number has been submitted.
    public bool IsMatchOver => matchSetUp && rules.Finished;
    public bool HasLiveMatch => matchSetUp && !rules.Finished;

    public string CurrentOpponentName => currentOpponent;
    public int CurrentRoundNumber => matchSetUp ? rules.RoundIndex + 1 : 0;
    public SoloBoardPhase CurrentPresentationPhase => presentationPhase;
    public int CurrentPlayerRangeMin => playerMin;
    public int CurrentPlayerRangeMax => playerMax;

    readonly DuelRules rules = new DuelRules();
    bool awaitingAnswer;
    DuelRules.Hint pendingAiHint;
    bool lockArmed;
    SoloBoardPhase presentationPhase = SoloBoardPhase.ChooseSecret;
    SoloBoardActor pendingForfeitActor = SoloBoardActor.None;
    bool pendingForfeitNotice;
    int pendingForfeitRound;
    bool lastLicksAcknowledged;
    bool presentationTransitioning;
    bool terminalRecorded;
    int lastAcknowledgementFrame = -1;

    // True from StartGame until RestartMatch clears the board. It stays true
    // across the result screen — the match is over, but it is still the match
    // being shown.
    bool matchSetUp;

    // The Lock is revealed once the player has played a round, and explains
    // itself the first time it appears. See LockIntro.
    bool lockRevealedThisMatch;

    int min = 1;
    int max = 100;
    int aiGuess;

    int playerMin = 1;
    int playerMax = 100;

    bool firstAIGuess = true;

    int playerSecretNumber;
    int aiSecretNumber;
    int playerGuessCount;

    string currentOpponent;
    SoloDuelVisuals boardPresenter;

    // Built at runtime next to the stop button, the same way the rewarded
    // streak-save offer is, so no scene surgery is needed.
    UnityEngine.UI.Button lockButton;
    TMP_Text lockButtonLabel;

    string[] fakeNames =
    {
        "Pierre", "Lucas", "Mathieu", "Marco", "Giovanni",
        "Luca", "Carlos", "Miguel", "Alejandro", "Javier",
        "Hans", "Lukas", "Erik", "Oliver", "Mateo",
        "Ivan", "Stefan", "Adrian", "Marek", "Bjorn",
        "Sven", "Kostas", "Andreas", "Nikos", "Konstantinos"
    };

    void Start()
    {
        ReconcilePendingStreakRestore();

        stopGameButton.SetActive(false);
        HideButtons();
        EnsureLockButton();
        RefreshLockButton();

        SelectOpponent();
        Presenter()?.BeginNewMatch(currentOpponent);
    }

    public void SetPlayerNumber(int number)
    {
        playerSecretNumber = number;
        Presenter()?.SetPlayerSecret(number);
    }

    public void StartGame()
    {
        StartGameWithOpener(Random.value < 0.5f ? AiSide : PlayerSide);
    }

    void StartGameWithOpener(DuelRules.Side opener)
    {
        CancelInvoke();

        SoloDuelVisuals presenter = Presenter();
        if (presenter == null)
        {
            RequirePresentation(false, "starter reveal");
            return;
        }

        min = 1;
        max = 100;
        playerMin = 1;
        playerMax = 100;

        awaitingAnswer = false;
        pendingAiHint = DuelRules.Hint.None;
        lockArmed = false;
        matchSetUp = false;
        lockRevealedThisMatch = false;
        firstAIGuess = true;
        playerGuessCount = 0;
        pendingForfeitActor = SoloBoardActor.None;
        pendingForfeitNotice = false;
        pendingForfeitRound = 0;
        lastLicksAcknowledged = false;
        presentationTransitioning = false;
        terminalRecorded = false;
        lastAcknowledgementFrame = -1;

        aiSecretNumber = Random.Range(1, 101);

        aiNumberText.text = "?";
        aiAnswerText.text = "";

        stopGameButton.SetActive(false);
        HideButtons();

        // Validate the mandatory presentation owner before starting canonical
        // rules. A board that cannot show the starter must fail closed while
        // the player can still safely retry secret entry.
        bool starterPresented = presenter.RevealStarter(
            opener == PlayerSide
                ? SoloBoardActor.Player
                : SoloBoardActor.Opponent,
            1,
            playerMin, playerMax, min, max);
        if (!starterPresented)
        {
            RequirePresentation(false, "starter reveal");
            return;
        }

        // A fair coin decides who opens; the equal-turns rule then makes sure
        // opening is not itself worth a win.
        rules.StartMatch(opener);
        matchSetUp = true;
        presentationPhase = SoloBoardPhase.StarterReveal;
        RefreshLockButton();
    }

    void HideButtons()
    {
        higherButton.SetActive(false);
        lowerButton.SetActive(false);
        correctButton.SetActive(false);
    }

    // Every non-input presentation beat advances only through this one guarded
    // acknowledgement path. No factual state is dismissed by a timer.
    public void AcknowledgePresentation()
    {
        if (presentationTransitioning ||
            lastAcknowledgementFrame == Time.frameCount)
            return;

        lastAcknowledgementFrame = Time.frameCount;
        presentationTransitioning = true;
        try
        {
            if (presentationPhase == SoloBoardPhase.StarterReveal)
            {
                RouteToCurrentTurn();
            }
            else if (presentationPhase == SoloBoardPhase.PlayerOutcome)
            {
                ContinueAfterMove();
            }
            else if (presentationPhase == SoloBoardPhase.OpponentThinking)
            {
                AIGuess();
            }
            else if (presentationPhase == SoloBoardPhase.OpponentGuess)
            {
                bool presented = Presenter()?.RevealOpponentOutcome() == true;
                presentationPhase = SoloBoardPhase.AnswerOpponent;
                RequirePresentation(presented, "opponent outcome");
            }
            else if (presentationPhase == SoloBoardPhase.AnswerOpponent)
            {
                AnswerGiven(pendingAiHint);
            }
            else if (presentationPhase == SoloBoardPhase.LastLicks)
            {
                lastLicksAcknowledged = true;
                BeginPlayerTurn(true);
            }
            else if (presentationPhase == SoloBoardPhase.LockForfeit)
            {
                pendingForfeitActor = SoloBoardActor.None;
                RouteToCurrentTurn();
            }
        }
        finally
        {
            presentationTransitioning = false;
        }
    }

    void AIGuess()
    {
        if (rules.Finished || rules.Turn != AiSide ||
            presentationPhase != SoloBoardPhase.OpponentThinking)
            return;

        aiGuess = ChooseAiGuess();

        int submittedRound = rules.RoundIndex + 1;
        int candidatesBefore = rules.CandidatesFor(AiSide);
        bool aiLocks = DuelRules.ShouldLock(AiLockStyle(), max - min + 1, rules.LockAvailable(AiSide));
        var move = rules.Submit(AiSide, aiGuess, playerSecretNumber, aiLocks);
        if (!move.Accepted) return;

        aiNumberText.text = currentOpponent + ": " + aiGuess +
                            (aiLocks ? "  [" + L10n.Get("lock_armed") + "]" : "");

        if (move.Hint == DuelRules.Hint.Higher)
            min = aiGuess + 1;
        else if (move.Hint == DuelRules.Hint.Lower)
            max = aiGuess - 1;

        pendingAiHint = move.Hint;
        awaitingAnswer = true;
        if (aiLocks && move.Hint != DuelRules.Hint.Correct)
        {
            pendingForfeitNotice = true;
            pendingForfeitActor = SoloBoardActor.Opponent;
            pendingForfeitRound = submittedRound;
        }

        bool presented = Presenter()?.RecordOpponentMove(
            submittedRound, aiGuess, move.Hint, aiLocks,
            candidatesBefore, playerMin, playerMax, min, max) == true;
        presentationPhase = SoloBoardPhase.OpponentGuess;
        RequirePresentation(presented, "opponent guess");

        HideButtons();
        aiAnswerText.text = OpponentFeedbackText(move.Hint);
        RefreshLockButton();
    }

    int ChooseAiGuess()
    {
        if (firstAIGuess)
        {
            // Hard opens on the midpoint like a solver should; the softer modes
            // keep the random opening, which costs about a quarter of a guess.
            int openingGuess = Difficulty() == 2
                ? (min + max) / 2
                : Random.Range(min, max + 1);
            firstAIGuess = false;
            return openingGuess;
        }

        int difficulty = Difficulty();
        float randomChance = difficulty == 3
            ? AdaptiveRandomChance()
            : DifficultyRandomChance[difficulty];
        return Random.value < randomChance
            ? Random.Range(min, max + 1)
            : (min + max) / 2;
    }

    static SoloBoardPrompt OpponentFeedbackPrompt(DuelRules.Hint hint)
    {
        if (hint == DuelRules.Hint.Higher)
            return SoloBoardPrompt.OpponentGuessedHigher;
        if (hint == DuelRules.Hint.Lower)
            return SoloBoardPrompt.OpponentGuessedLower;
        return SoloBoardPrompt.OpponentGuessedCorrect;
    }

    static string OpponentFeedbackText(DuelRules.Hint hint)
    {
        if (hint == DuelRules.Hint.Higher)
            return L10n.Get("your_number_is_higher");
        if (hint == DuelRules.Hint.Lower)
            return L10n.Get("your_number_is_lower");
        return L10n.Get("your_number_is_correct");
    }

    void ResolveAiAnswerAutomatically()
    {
        // Legacy deterministic-fixture seam. Production never schedules this
        // method; the visible board calls AcknowledgePresentation for each
        // factual stage.
        if (!awaitingAnswer) return;
        if (presentationPhase == SoloBoardPhase.OpponentGuess)
        {
            bool presented = Presenter()?.RevealOpponentOutcome() == true;
            presentationPhase = SoloBoardPhase.AnswerOpponent;
            RequirePresentation(presented, "legacy opponent outcome");
        }
        if (presentationPhase == SoloBoardPhase.AnswerOpponent)
            AnswerGiven(pendingAiHint);
    }

    public void Higher()
    {
        AnswerGiven(DuelRules.Hint.Higher);
    }

    public void Lower()
    {
        AnswerGiven(DuelRules.Hint.Lower);
    }

    // The opponent found the player's number. Under the equal-turns rule that
    // is provisional: if it is still the player's round, they get an answering
    // guess before the match is called.
    public void Correct()
    {
        AnswerGiven(DuelRules.Hint.Correct);
    }

    void AnswerGiven(DuelRules.Hint answer)
    {
        // The rule engine already computed the truthful hint. Public button
        // callbacks may arrive late or be miswired, so they must never mutate
        // the AI's search interval unless they match that pending authority.
        if (!awaitingAnswer || answer != pendingAiHint ||
            presentationPhase != SoloBoardPhase.AnswerOpponent)
            return;

        if (pendingAiHint == DuelRules.Hint.Correct)
            aiAnswerText.text = L10n.Get(
                "opponent_found_number", currentOpponent);

        awaitingAnswer = false;
        pendingAiHint = DuelRules.Hint.None;
        HideButtons();
        ContinueAfterMove();
    }

    static int Difficulty()
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(DifficultyPrefKey, 1), 0, 3);
    }

    static float AdaptiveRandomChance()
    {
        float winRate = GameStats.RecentWinRate();
        if (winRate < 0f) return DifficultyRandomChance[1];

        if (winRate > 0.6f) return 0.1f;
        if (winRate < 0.4f) return 0.5f;
        return 0.25f;
    }

    // Difficulty now shapes the opponent's judgement, not just its aim. A
    // reckless Lock loses far more matches than a random guess ever did, so an
    // Easy opponent over-commits and a Hard one waits for certainty.
    static DuelRules.LockStyle AiLockStyle()
    {
        switch (Difficulty())
        {
            case 0: return DuelRules.LockStyle.Reckless;
            case 1: return DuelRules.LockStyle.Bold;
            case 2: return DuelRules.LockStyle.Precise;
            default:
                float winRate = GameStats.RecentWinRate();
                return winRate > 0.6f ? DuelRules.LockStyle.Precise : DuelRules.LockStyle.Bold;
        }
    }

    public bool PlayerGuess(int guess)
    {
        if (!IsPlayerTurn) return false;

        if (guess < playerMin || guess > playerMax)
        {
            aiAnswerText.text = L10n.Get("already_know_range", playerMin, playerMax);
            return false;
        }

        int submittedRound = rules.RoundIndex + 1;
        int candidatesBefore = rules.CandidatesFor(PlayerSide);
        bool staked = lockArmed;
        var move = rules.Submit(PlayerSide, guess, aiSecretNumber, staked);
        if (!move.Accepted) return false;

        lockArmed = false;
        if (staked) LockIntro.MarkUsed();
        playerGuessCount = rules.GuessCount(PlayerSide);

        string playerLabel = PlayerPrefs.GetString("PlayerName", "");
        if (string.IsNullOrEmpty(playerLabel))
            playerLabel = L10n.Get("you");
        if (staked)
            playerLabel += "  [" + L10n.Get("lock_armed") + "]";

        aiAnswerText.text = playerLabel + ": " + guess;
        if (move.Hint == DuelRules.Hint.Correct)
        {
            // Not "you win" yet — the opponent may still answer this round.
            aiAnswerText.text = playerLabel + ": " + guess + "\n" + L10n.Get("correct") + "!";
            GameEvents.CorrectGuess();
        }
        else if (move.Hint == DuelRules.Hint.Higher)
        {
            aiAnswerText.text = playerLabel + ": " + guess + "\n" + currentOpponent + ": " + L10n.Get("higher");
            if (guess + 1 > playerMin) playerMin = guess + 1;
        }
        else
        {
            aiAnswerText.text = playerLabel + ": " + guess + "\n" + currentOpponent + ": " + L10n.Get("lower");
            if (guess - 1 < playerMax) playerMax = guess - 1;
        }

        if (staked && move.Hint != DuelRules.Hint.Correct)
        {
            aiAnswerText.text += "\n" + L10n.Get("lock_missed");
            pendingForfeitNotice = true;
            pendingForfeitActor = SoloBoardActor.Player;
            pendingForfeitRound = submittedRound;
        }

        lastLicksAcknowledged = false;
        bool presented = Presenter()?.RecordPlayerMove(
            submittedRound, guess, move.Hint, staked,
            candidatesBefore, playerMin, playerMax, min, max) == true;
        presentationPhase = SoloBoardPhase.PlayerOutcome;
        RequirePresentation(presented, "player guess");
        RefreshLockButton();
        return true;
    }

    // Hands play on after a completed move, or ends the match if the round that
    // just closed settled it.
    void ContinueAfterMove()
    {
        RefreshLockButton();

        if (rules.Finished)
        {
            EndGame();
            return;
        }

        if (pendingForfeitNotice)
        {
            pendingForfeitNotice = false;
            presentationPhase = SoloBoardPhase.LockForfeit;
            RequirePresentation(
                Presenter()?.ShowLockForfeit(
                    pendingForfeitActor, pendingForfeitRound) == true,
                "Lock forfeit");
            RefreshLockButton();
            return;
        }

        RouteToCurrentTurn();
    }

    void RouteToCurrentTurn()
    {
        if (rules.Finished)
        {
            EndGame();
            return;
        }

        if (rules.Turn == AiSide)
        {
            lastLicksAcknowledged = false;
            presentationPhase = SoloBoardPhase.OpponentThinking;
            RequirePresentation(Presenter()?.BeginOpponentThinking(
                rules.RoundIndex + 1,
                playerMin, playerMax, min, max) == true,
                "opponent thinking");
            RefreshLockButton();
            return;
        }

        if (rules.IsMatchPointAgainst(PlayerSide) && !lastLicksAcknowledged)
        {
            presentationPhase = SoloBoardPhase.LastLicks;
            RequirePresentation(
                Presenter()?.ShowLastLicks(rules.RoundIndex + 1) == true,
                "last licks");
            RefreshLockButton();
            return;
        }

        BeginPlayerTurn(rules.IsMatchPointAgainst(PlayerSide));
    }

    void BeginPlayerTurn(bool lastLicks)
    {
        presentationPhase = SoloBoardPhase.PlayerGuess;
        RequirePresentation(Presenter()?.BeginPlayerTurn(
            rules.RoundIndex + 1,
            playerMin, playerMax, min, max, lastLicks) == true,
            "player turn");
        RefreshLockButton();
    }

    void EndGame()
    {
        if (terminalRecorded)
            return;
        terminalRecorded = true;
        awaitingAnswer = false;
        pendingAiHint = DuelRules.Hint.None;
        lockArmed = false;

        HideButtons();
        stopGameButton.SetActive(true);
        RefreshLockButton();

        if (numberManager != null)
            numberManager.CloseInput();

        if (rules.Result == DuelRules.Outcome.Draw)
        {
            GameStats.RecordDraw();
            Haptics.Light();
            GameEvents.MatchCompleted(SoloOutcome(MatchOutcome.Result.Draw));

        }
        else if (rules.Result == DuelRules.Outcome.HostWins)
        {
            GameStats.RecordWin(playerGuessCount);
            Haptics.Success();
            GameEvents.MatchCompleted(SoloOutcome(MatchOutcome.Result.Win));

            if (audioSource != null && winSound != null)
                audioSource.PlayOneShot(winSound);
            if (winConfetti != null)
                winConfetti.Burst();
        }
        else
        {
            int streakBeforeLoss = GameStats.CurrentStreak;
            GameStats.RecordLoss();
            Haptics.Error();
            GameEvents.MatchCompleted(SoloOutcome(MatchOutcome.Result.Loss));

            if (audioSource != null && loseSound != null)
                audioSource.PlayOneShot(loseSound);

            OfferStreakSave(streakBeforeLoss);
        }

        // Persist the terminal result before rendering it. The presenter reads
        // live GameStats, so rendering first would display the previous wins
        // value even though the rules outcome was already final.
        presentationPhase = SoloBoardPhase.MatchResult;
        RequirePresentation(Presenter()?.CompleteMatch(
            rules.Result, playerSecretNumber, aiSecretNumber,
            rules.GuessCount(PlayerSide), rules.GuessCount(AiSide)) == true,
            "match result");

        if (adsManager != null && GameStats.Matches % 2 == 0)
            adsManager.ShowAd(null);
    }

    // The Lock starts available and is never returned, so "no longer available"
    // is exactly "was staked". Solo starts a fresh match every time rather than
    // rematching inside a room, so the rematch index is always zero.
    MatchOutcome SoloOutcome(MatchOutcome.Result result)
    {
        return new MatchOutcome
        {
            PlayMode = MatchOutcome.Mode.Solo,
            Outcome = result,
            Guesses = playerGuessCount,
            OpponentGuesses = rules.GuessCount(AiSide),
            Opened = rules.Opener == PlayerSide,
            LockStaked = !rules.LockAvailable(PlayerSide),
            RematchIndex = 0,
            AppVersion = Application.version,
        };
    }

    // ------------------------------------------------------------- the Lock

    public void OnLockTogglePressed()
    {
        if (!IsPlayerTurn) return;
        if (!rules.LockAvailable(PlayerSide)) return;

        lockArmed = !lockArmed;
        Haptics.Light();
        RefreshLockButton();
    }

    void EnsureLockButton()
    {
        if (lockButton != null || stopGameButton == null) return;

        var btn = RuntimeUI.CreateButton(stopGameButton.transform.parent, "LockButton",
            L10n.Get("lock"), Vector2.zero, new Vector2(320f, 84f),
            HolUiStateColors.Cyan, HolUiStateColors.WithAlpha(HolUiStateColors.Surface, 1f));

        var stopRect = (RectTransform)stopGameButton.transform;
        var rect = (RectTransform)btn.transform;
        rect.anchorMin = stopRect.anchorMin;
        rect.anchorMax = stopRect.anchorMax;
        rect.pivot = stopRect.pivot;
        rect.anchoredPosition = stopRect.anchoredPosition + new Vector2(0f, 210f);

        btn.onClick.AddListener(OnLockTogglePressed);
        lockButton = btn;
        Presenter()?.RegisterLockControl(lockButton);

        lockButtonLabel = btn.GetComponentInChildren<TMP_Text>();
        if (lockButtonLabel != null) lockButtonLabel.fontSize = 26;
    }

    // The button is also how the mechanic teaches itself: once the range is
    // down to a few candidates it stops reading "LOCK" and starts asking.
    void RefreshLockButton()
    {
        if (lockButton == null) return;

        // DuelRules owns legality: a player's one Lock is available even on
        // their opening guess. Presentation may teach it progressively, but it
        // must never silently narrow the canonical rule.
        bool revealed = matchSetUp;
        bool lockStillAvailable = rules.LockAvailable(PlayerSide);
        bool available = IsPlayerTurn && lockStillAvailable;
        bool show = matchSetUp && !rules.Finished;

        lockButton.gameObject.SetActive(show);
        lockButton.interactable = available;
        Presenter()?.UpdateLockState(
            revealed, available, lockArmed,
            matchSetUp && !rules.LockAvailable(PlayerSide),
            playerMax >= playerMin ? playerMax - playerMin + 1 : 0);
        if (!show) return;

        if (revealed && !lockRevealedThisMatch)
        {
            lockRevealedThisMatch = true;
            if (LockIntro.ShouldExplain)
            {
                LockIntro.MarkExplained();
            }
        }

        int left = playerMax >= playerMin ? playerMax - playerMin + 1 : 0;
        string text = !revealed
            ? L10n.Get("lock")
            : !lockStillAvailable
            ? L10n.Get("lock_spent")
            : lockArmed
            ? L10n.Get("lock_armed")
            : DuelRules.ShouldSuggestLock(left, lockStillAvailable)
                ? L10n.Get("lock_suggest", left)
                : L10n.Get("lock");

        if (lockButtonLabel != null)
            lockButtonLabel.text = text;
    }

    // -------------------------------------------------- rewarded streak save

    const int MinStreakToSave = 2;
    GameObject streakSaveButton;

    void ReconcilePendingStreakRestore()
    {
        int pending = PlayerPrefs.GetInt(AdsManager.PendingStreakRestoreKey, 0);
        if (pending <= 0) return;

        if (PlayerPrefs.GetInt(AdsManager.PendingRewardEarnedKey, 0) == 1)
        {
            GameStats.RestoreStreak(pending);
            GameEvents.StatsChanged();
        }

        PlayerPrefs.DeleteKey(AdsManager.PendingStreakRestoreKey);
        PlayerPrefs.DeleteKey(AdsManager.PendingRewardEarnedKey);
        PlayerPrefs.Save();
    }

    void OfferStreakSave(int streak)
    {
        if (streak < MinStreakToSave || adsManager == null || !adsManager.IsRewardedReady())
            return;
        if (stopGameButton == null) return;

        var btn = RuntimeUI.CreateButton(stopGameButton.transform.parent, "SaveStreakButton",
            L10n.Get("save_streak_ad", streak), Vector2.zero, new Vector2(560f, 90f),
            HolUiStateColors.Gold, HolUiStateColors.WithAlpha(HolUiStateColors.Surface, 1f));

        var stopRect = (RectTransform)stopGameButton.transform;
        var rect = (RectTransform)btn.transform;
        rect.anchorMin = stopRect.anchorMin;
        rect.anchorMax = stopRect.anchorMax;
        rect.pivot = stopRect.pivot;
        rect.anchoredPosition = stopRect.anchoredPosition + new Vector2(0f, 120f);

        streakSaveButton = btn.gameObject;
        Presenter()?.RegisterSaveStreakControl(btn);
        btn.onClick.AddListener(() =>
        {
            bool shown = adsManager.ShowRewardedAd(() =>
            {
                GameStats.RestoreStreak(streak);
                PlayerPrefs.DeleteKey(AdsManager.PendingStreakRestoreKey);
                PlayerPrefs.DeleteKey(AdsManager.PendingRewardEarnedKey);
                PlayerPrefs.Save();
                GameEvents.StatsChanged();
                if (streakSaveButton != null)
                {
                    Destroy(streakSaveButton);
                    streakSaveButton = null;
                }
            },
            () =>
            {
                PlayerPrefs.DeleteKey(AdsManager.PendingStreakRestoreKey);
                PlayerPrefs.Save();
                if (aiAnswerText != null)
                    aiAnswerText.text = L10n.Get("ad_not_ready");
            });

            if (shown)
            {
                PlayerPrefs.SetInt(AdsManager.PendingStreakRestoreKey, streak);
                PlayerPrefs.Save();
            }
        });
    }

    public void RestartMatch()
    {
        CancelInvoke();

        if (streakSaveButton != null)
        {
            Destroy(streakSaveButton);
            streakSaveButton = null;
        }

        // Clearing this is what re-opens number entry for the next match.
        matchSetUp = false;
        awaitingAnswer = false;
        pendingAiHint = DuelRules.Hint.None;
        lockArmed = false;
        lockRevealedThisMatch = false;
        pendingForfeitActor = SoloBoardActor.None;
        pendingForfeitNotice = false;
        pendingForfeitRound = 0;
        lastLicksAcknowledged = false;
        presentationTransitioning = false;
        terminalRecorded = false;
        lastAcknowledgementFrame = -1;
        presentationPhase = SoloBoardPhase.ChooseSecret;
        playerMin = 1;
        playerMax = 100;
        RefreshLockButton();

        SelectOpponent();

        aiNumberText.text = "?";
        aiAnswerText.text = "";
        Presenter()?.BeginNewMatch(currentOpponent);

        stopGameButton.SetActive(false);
        HideButtons();

        if (numberManager != null)
            numberManager.ResetForNewMatch();
    }

    // Called only after the explicit leave confirmation. It records one real
    // Solo loss before MenuManager leaves the scene; repeated confirm taps are
    // idempotent.
    public bool RecordLiveForfeitOnce()
    {
        if (!HasLiveMatch || terminalRecorded)
            return false;

        terminalRecorded = true;
        GameStats.RecordLoss();
        Haptics.Error();
        GameEvents.MatchCompleted(SoloOutcome(MatchOutcome.Result.Loss));
        return true;
    }

    // There are no Solo turn timers. Unity pause/focus callbacks intentionally
    // leave the presentation and DuelRules state untouched, so resume cannot
    // skip or duplicate an AI action.
    void OnApplicationPause(bool paused)
    {
    }

    void OnApplicationFocus(bool focused)
    {
    }

    void SelectOpponent()
    {
        int randomIndex = Random.Range(0, fakeNames.Length);
        currentOpponent = fakeNames[randomIndex];
    }

    SoloDuelVisuals Presenter()
    {
        if (boardPresenter == null)
            boardPresenter = FindObjectOfType<SoloDuelVisuals>(true);
        return boardPresenter;
    }

    static void RequirePresentation(bool accepted, string context)
    {
        if (!accepted)
            Debug.LogError("[GameManager] Solo presentation rejected " +
                           context + ".");
    }

    void Present(SoloBoardPhase phase, SoloBoardPrompt prompt, int detailValue = 0,
        int displayRound = 0)
    {
        var presenter = Presenter();
        if (presenter == null) return;
        presenter.PresentPhase(phase, prompt,
            displayRound > 0 ? displayRound : rules.RoundIndex + 1,
            playerMin, playerMax, detailValue);
    }
}
