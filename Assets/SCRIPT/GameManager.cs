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

    // Optional UI (wire in Inspector): shows the player's narrowed guess
    // range and running guess histories for both sides.
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

    readonly DuelRules rules = new DuelRules();
    bool awaitingAnswer;
    bool lockArmed;

    // True from StartGame until RestartMatch clears the board. It stays true
    // across the result screen — the match is over, but it is still the match
    // being shown.
    bool matchSetUp;

    int min = 1;
    int max = 100;
    int aiGuess;

    int playerMin = 1;
    int playerMax = 100;

    bool firstAIGuess = true;

    int playerSecretNumber;
    int aiSecretNumber;
    int playerGuessCount;

    readonly System.Text.StringBuilder playerHistory = new System.Text.StringBuilder();
    readonly System.Text.StringBuilder aiHistory = new System.Text.StringBuilder();

    string currentOpponent;

    // Built at runtime next to the stop button, the same way the rewarded
    // streak-save offer is, so no scene surgery is needed.
    UnityEngine.UI.Button lockButton;
    UnityEngine.UI.Text lockButtonLabel;

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

        int randomIndex = Random.Range(0, fakeNames.Length);
        currentOpponent = fakeNames[randomIndex];

        opponentNameText.text = L10n.Get("opponent_label", currentOpponent);

        if (turnText != null)
            turnText.text = L10n.Get("enter_your_number");
    }

    public void SetPlayerNumber(int number)
    {
        playerSecretNumber = number;
    }

    public void StartGame()
    {
        CancelInvoke();

        min = 1;
        max = 100;
        playerMin = 1;
        playerMax = 100;

        awaitingAnswer = false;
        lockArmed = false;
        matchSetUp = true;
        firstAIGuess = true;
        playerGuessCount = 0;

        aiSecretNumber = Random.Range(1, 101);

        aiNumberText.text = "?";
        aiAnswerText.text = "";

        ResetHistory();
        UpdateRangeText();

        stopGameButton.SetActive(false);
        HideButtons();

        // A fair coin decides who opens; the equal-turns rule then makes sure
        // opening is not itself worth a win.
        rules.StartMatch(Random.value < 0.5f ? AiSide : PlayerSide);
        RefreshLockButton();

        if (rules.Turn == AiSide)
        {
            turnText.text = L10n.Get("opponent_thinking", currentOpponent);
            Invoke(nameof(AIGuess), Random.Range(0.8f, 1.5f));
        }
        else
        {
            turnText.text = L10n.Get("your_guess");
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

        bool aiLocks = DuelRules.ShouldLock(AiLockStyle(), max - min + 1, rules.LockAvailable(AiSide));
        var move = rules.Submit(AiSide, aiGuess, playerSecretNumber, aiLocks);
        if (!move.Accepted) return;

        aiNumberText.text = currentOpponent + ": " + aiGuess +
                            (aiLocks ? "  [" + L10n.Get("lock_armed") + "]" : "");
        AppendHistory(aiHistory, aiHistoryText, aiGuess);

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
        turnText.text = L10n.Get("answer_opponent", currentOpponent);
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
        playerGuessCount = rules.GuessCount(PlayerSide);

        string playerLabel = PlayerPrefs.GetString("PlayerName", "");
        if (string.IsNullOrEmpty(playerLabel))
            playerLabel = L10n.Get("you");
        if (staked)
            playerLabel += "  [" + L10n.Get("lock_armed") + "]";

        aiAnswerText.text = playerLabel + ": " + guess;
        AppendHistory(playerHistory, playerHistoryText, guess);

        if (move.Hint == DuelRules.Hint.Correct)
        {
            // Not "you win" yet — the opponent may still answer this round.
            aiAnswerText.text = playerLabel + ": " + guess + "\n" + L10n.Get("correct") + "!";
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

        UpdateRangeText();
        ContinueAfterMove();
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

        if (rules.Turn == AiSide)
        {
            turnText.text = rules.ForfeitPending(AiSide)
                ? L10n.Get("opponent_forfeits", currentOpponent)
                : L10n.Get("opponent_thinking", currentOpponent);
            Invoke(nameof(AIGuess), Random.Range(1.5f, 3.5f));
            return;
        }

        if (rules.IsMatchPointAgainst(PlayerSide))
            turnText.text = L10n.Get("match_point");
        else if (rules.PendingWin == PlayerSide)
            turnText.text = L10n.Get("match_point_yours", currentOpponent);
        else if (rules.ForfeitPending(PlayerSide))
            turnText.text = L10n.Get("turn_forfeited");
        else
            turnText.text = L10n.Get("your_guess");
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
            GameEvents.StatsChanged();

            turnText.text = L10n.Get("you_draw") + "\n" +
                            L10n.Get("draw_in_guesses", playerGuessCount) + "\n" +
                            L10n.Get("draw_tip");
        }
        else if (rules.Result == DuelRules.Outcome.HostWins)
        {
            GameStats.RecordWin(playerGuessCount);
            Haptics.Success();
            GameEvents.MatchEnded(true, playerGuessCount);

            turnText.text = L10n.Get("you_win") + "\n" + L10n.Get("won_in_guesses", playerGuessCount);
            if (playerGuessCount <= 7)
                turnText.text += "\n" + L10n.Get("perfect_game");
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
            GameEvents.MatchEnded(false, 0);

            turnText.text = L10n.Get("you_lose") + "\n" + L10n.Get("number_was", aiSecretNumber);
            if (audioSource != null && loseSound != null)
                audioSource.PlayOneShot(loseSound);

            OfferStreakSave(streakBeforeLoss);
        }

        if (adsManager != null && GameStats.Matches % 2 == 0)
            adsManager.ShowAd(null);
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
            ConvergingLight.Cyan, ConvergingLight.WithAlpha(ConvergingLight.PanelIndigo, 1f));

        var stopRect = (RectTransform)stopGameButton.transform;
        var rect = (RectTransform)btn.transform;
        rect.anchorMin = stopRect.anchorMin;
        rect.anchorMax = stopRect.anchorMax;
        rect.pivot = stopRect.pivot;
        rect.anchoredPosition = stopRect.anchoredPosition + new Vector2(0f, 210f);

        btn.onClick.AddListener(OnLockTogglePressed);
        lockButton = btn;

        lockButtonLabel = btn.GetComponentInChildren<UnityEngine.UI.Text>();
        if (lockButtonLabel != null) lockButtonLabel.fontSize = 26;
    }

    // The button is also how the mechanic teaches itself: once the range is
    // down to a few candidates it stops reading "LOCK" and starts asking.
    void RefreshLockButton()
    {
        if (lockButton == null) return;

        bool available = matchSetUp && !rules.Finished && rules.LockAvailable(PlayerSide);
        lockButton.gameObject.SetActive(available);
        if (!available) return;

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
            ConvergingLight.Gold, ConvergingLight.WithAlpha(ConvergingLight.PanelIndigo, 1f));

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
        playerMin = 1;
        playerMax = 100;
        RefreshLockButton();

        int randomIndex = Random.Range(0, fakeNames.Length);
        currentOpponent = fakeNames[randomIndex];
        opponentNameText.text = L10n.Get("opponent_label", currentOpponent);

        aiNumberText.text = "?";
        aiAnswerText.text = "";
        turnText.text = L10n.Get("enter_your_number");

        ResetHistory();

        stopGameButton.SetActive(false);
        HideButtons();

        if (numberManager != null)
            numberManager.ResetForNewMatch();
    }

    void ResetHistory()
    {
        playerHistory.Length = 0;
        aiHistory.Length = 0;

        if (playerHistoryText != null)
            playerHistoryText.text = "";
        if (aiHistoryText != null)
            aiHistoryText.text = "";
    }

    static void AppendHistory(System.Text.StringBuilder history, TMP_Text target, int guess)
    {
        if (history.Length > 0)
            history.Append("  ");
        history.Append(guess);

        if (target != null)
            target.text = history.ToString();
    }

    void UpdateRangeText()
    {
        if (rangeText != null)
            rangeText.text = L10n.Get("between_range", playerMin, playerMax);
    }
}
