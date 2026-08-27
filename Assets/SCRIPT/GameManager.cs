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

    // Runtime bindings populated by HolDuelBoardLayout. The presenter is the
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
                                rules.Turn == PlayerSide && !awaitingAnswer;

    // "This match is decided, stop taking input." NumberManager gates the whole
    // submit path on this, so it MUST go false again once RestartMatch clears
    // the board — otherwise the player can never enter a number for the next
    // match and solo becomes a one-match-per-launch game. That is why it is not
    // simply rules.Finished: the rules object stays finished until the next
    // StartMatch, which does not happen until a number has been submitted.
    public bool IsMatchOver => matchSetUp && rules.Finished;

    public string CurrentOpponentName => currentOpponent;
    public int CurrentRoundNumber => matchSetUp ? rules.RoundIndex + 1 : 0;

    readonly DuelRules rules = new DuelRules();
    bool awaitingAnswer;
    bool lockArmed;

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
    HolDuelBoardLayout boardPresenter;

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
    }

    public void StartGame()
    {
        StartGameWithOpener(Random.value < 0.5f ? AiSide : PlayerSide);
    }

    void StartGameWithOpener(DuelRules.Side opener)
    {
        CancelInvoke();

        min = 1;
        max = 100;
        playerMin = 1;
        playerMax = 100;

        awaitingAnswer = false;
        lockArmed = false;
        matchSetUp = true;
        lockRevealedThisMatch = false;
        firstAIGuess = true;
        playerGuessCount = 0;

        aiSecretNumber = Random.Range(1, 101);

        aiNumberText.text = "?";
        aiAnswerText.text = "";

        stopGameButton.SetActive(false);
        HideButtons();

        // A fair coin decides who opens; the equal-turns rule then makes sure
        // opening is not itself worth a win.
        rules.StartMatch(opener);
        RefreshLockButton();

        if (rules.Turn == AiSide)
        {
            Present(SoloBoardPhase.OpponentThinking, SoloBoardPrompt.OpponentThinking);
            Invoke(nameof(AIGuess), Random.Range(0.8f, 1.5f));
        }
        else
        {
            Present(SoloBoardPhase.PlayerGuess, SoloBoardPrompt.YourGuess);
        }
    }

    void HideButtons()
    {
        higherButton.SetActive(false);
        lowerButton.SetActive(false);
        correctButton.SetActive(false);
    }

    void AIGuess()
    {
        if (rules.Finished || rules.Turn != AiSide) return;

        if (firstAIGuess)
        {
            // Hard opens on the midpoint like a solver should; the softer modes
            // keep the random opening, which costs about a quarter of a guess.
            aiGuess = Difficulty() == 2 ? (min + max) / 2 : Random.Range(min, max + 1);
            firstAIGuess = false;
        }
        else
        {
            int difficulty = Difficulty();
            float randomChance = difficulty == 3
                ? AdaptiveRandomChance()
                : DifficultyRandomChance[difficulty];

            if (Random.value < randomChance)
                aiGuess = Random.Range(min, max + 1);
            else
                aiGuess = (min + max) / 2;
        }

        int submittedRound = rules.RoundIndex + 1;
        bool aiLocks = DuelRules.ShouldLock(AiLockStyle(), max - min + 1, rules.LockAvailable(AiSide));
        var move = rules.Submit(AiSide, aiGuess, playerSecretNumber, aiLocks);
        if (!move.Accepted) return;

        aiNumberText.text = currentOpponent + ": " + aiGuess +
                            (aiLocks ? "  [" + L10n.Get("lock_armed") + "]" : "");
        Presenter()?.RecordAiGuess(aiGuess);

        // If that guess closed the round the match is already decided, so do
        // not make the player answer a guess nobody can act on — it would put
        // a dead tap between them and the result.
        if (rules.Finished)
        {
            if (move.Hint == DuelRules.Hint.Correct)
                aiAnswerText.text = L10n.Get("opponent_found_number", currentOpponent);

            awaitingAnswer = false;
            HideButtons();
            ContinueAfterMove();
            return;
        }

        // The player still confirms the answer, so the hint the rules computed
        // decides which single button is offered — a player cannot lie.
        awaitingAnswer = true;
        Present(SoloBoardPhase.AnswerOpponent, SoloBoardPrompt.AnswerOpponent, 0, submittedRound);
        HideButtons();

        if (move.Hint == DuelRules.Hint.Higher) higherButton.SetActive(true);
        else if (move.Hint == DuelRules.Hint.Lower) lowerButton.SetActive(true);
        else correctButton.SetActive(true);

        RefreshLockButton();
    }

    public void Higher()
    {
        min = aiGuess + 1;
        AnswerGiven();
    }

    public void Lower()
    {
        max = aiGuess - 1;
        AnswerGiven();
    }

    // The opponent found the player's number. Under the equal-turns rule that
    // is provisional: if it is still the player's round, they get an answering
    // guess before the match is called.
    public void Correct()
    {
        aiAnswerText.text = L10n.Get("opponent_found_number", currentOpponent);
        AnswerGiven();
    }

    void AnswerGiven()
    {
        if (!awaitingAnswer) return;

        awaitingAnswer = false;
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
        Presenter()?.RecordPlayerGuess(guess);

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
            aiAnswerText.text += "\n" + L10n.Get("lock_missed");

        ContinueAfterMove();
        return true;
    }

    // Hands play on after a completed move, or ends the match if the round that
    // just closed settled it.
    void ContinueAfterMove()
    {
        Present(SoloBoardPhase.RoundResolution, SoloBoardPrompt.ResolvingRound);
        RefreshLockButton();

        if (rules.Finished)
        {
            EndGame();
            return;
        }

        if (rules.Turn == AiSide)
        {
            Present(SoloBoardPhase.OpponentThinking,
                rules.ForfeitPending(AiSide)
                    ? SoloBoardPrompt.OpponentForfeits
                    : SoloBoardPrompt.OpponentThinking);
            Invoke(nameof(AIGuess), Random.Range(1.5f, 3.5f));
            return;
        }

        if (rules.IsMatchPointAgainst(PlayerSide))
            Present(SoloBoardPhase.PlayerGuess, SoloBoardPrompt.MatchPoint);
        else if (rules.PendingWin == PlayerSide)
            Present(SoloBoardPhase.PlayerGuess, SoloBoardPrompt.MatchPointYours);
        else if (rules.ForfeitPending(PlayerSide))
            Present(SoloBoardPhase.PlayerGuess, SoloBoardPrompt.TurnForfeited);
        else
            Present(SoloBoardPhase.PlayerGuess, SoloBoardPrompt.YourGuess);
    }

    void EndGame()
    {
        awaitingAnswer = false;
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

            Present(SoloBoardPhase.MatchResult, SoloBoardPrompt.Draw, playerGuessCount);
        }
        else if (rules.Result == DuelRules.Outcome.HostWins)
        {
            GameStats.RecordWin(playerGuessCount);
            Haptics.Success();
            GameEvents.MatchCompleted(SoloOutcome(MatchOutcome.Result.Win));

            Present(SoloBoardPhase.MatchResult, SoloBoardPrompt.Win, playerGuessCount);
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

            Present(SoloBoardPhase.MatchResult, SoloBoardPrompt.Loss, aiSecretNumber);
            if (audioSource != null && loseSound != null)
                audioSource.PlayOneShot(loseSound);

            OfferStreakSave(streakBeforeLoss);
        }

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
        if (!matchSetUp || rules.Finished) return;
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

        lockButtonLabel = btn.GetComponentInChildren<TMP_Text>();
        if (lockButtonLabel != null) lockButtonLabel.fontSize = 26;
    }

    // The button is also how the mechanic teaches itself: once the range is
    // down to a few candidates it stops reading "LOCK" and starts asking.
    void RefreshLockButton()
    {
        if (lockButton == null) return;

        // Not offered until the player has taken a turn: a first match should
        // feel like plain higher-or-lower, and staking the Lock on an opening
        // guess is a trap rather than a choice.
        bool revealed = rules.GuessCount(PlayerSide) > 0;
        bool available = matchSetUp && !rules.Finished && revealed &&
                         rules.LockAvailable(PlayerSide);

        lockButton.gameObject.SetActive(available);
        if (!available) return;

        if (!lockRevealedThisMatch)
        {
            lockRevealedThisMatch = true;
            if (LockIntro.ShouldExplain)
            {
                // Keep the presenter's valid-range line truthful. The one-time
                // Lock explanation uses the ordinary feedback slot instead.
                if (aiAnswerText != null)
                    aiAnswerText.text = L10n.Get("lock_hint");
                LockIntro.MarkExplained();
            }
        }

        int left = playerMax >= playerMin ? playerMax - playerMin + 1 : 0;
        string text = lockArmed
            ? L10n.Get("lock_armed")
            : DuelRules.ShouldSuggestLock(left, true)
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
        lockArmed = false;
        lockRevealedThisMatch = false;
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

    void SelectOpponent()
    {
        int randomIndex = Random.Range(0, fakeNames.Length);
        currentOpponent = fakeNames[randomIndex];
    }

    HolDuelBoardLayout Presenter()
    {
        if (boardPresenter == null)
            boardPresenter = FindObjectOfType<HolDuelBoardLayout>(true);
        return boardPresenter;
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
