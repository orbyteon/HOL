using UnityEngine;
using TMPro;

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

    public NumberManager numberManager; // review #3: wire in Inspector
    public ConfettiBurst winConfetti;   // optional: drag a ConfettiBurst for win celebration
    public AdsManager adsManager;       // optional: match-end interstitial (see EndGame)

    // Review #6: lets NumberManager give feedback instead of silently
    // swallowing guesses typed during the opponent's turn.
    public bool IsPlayerTurn => playerTurn && !gameFinished;

    int min = 1;
    int max = 100;
    int aiGuess;

    // Bounds on what the player still knows about the AI's secret number.
    // Narrowed by each Higher/Lower hint; used to reject out-of-range guesses.
    int playerMin = 1;
    int playerMax = 100;

    bool playerTurn = false;
    bool gameFinished = false;
    bool firstAIGuess = true;

    int playerSecretNumber;
    int aiSecretNumber;
    int playerGuessCount;

    readonly System.Text.StringBuilder playerHistory = new System.Text.StringBuilder();
    readonly System.Text.StringBuilder aiHistory = new System.Text.StringBuilder();

    string currentOpponent;

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

        gameFinished = false;
        playerTurn = false;
        firstAIGuess = true;
        playerGuessCount = 0;

        aiSecretNumber = Random.Range(1, 101);

        aiNumberText.text = "?";
        aiAnswerText.text = "";

        ResetHistory();
        UpdateRangeText();

        stopGameButton.SetActive(false);
        HideButtons();

        // Randomly decide who starts.
        bool aiStarts = Random.value < 0.5f;

        if (aiStarts)
        {
            turnText.text = L10n.Get("opponent_thinking", currentOpponent);
            Invoke(nameof(AIGuess), Random.Range(0.8f, 1.5f));
        }
        else
        {
            turnText.text = L10n.Get("your_guess");
            playerTurn = true;
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
        if (gameFinished) return;

        if (firstAIGuess)
        {
            aiGuess = Random.Range(min, max + 1);
            firstAIGuess = false;
        }
        else
        {
            int difficulty = Mathf.Clamp(PlayerPrefs.GetInt(DifficultyPrefKey, 1), 0, 3);
            float randomChance = difficulty == 3
                ? AdaptiveRandomChance()
                : DifficultyRandomChance[difficulty];

            if (Random.value < randomChance)
                aiGuess = Random.Range(min, max + 1);
            else
                aiGuess = (min + max) / 2;
        }

        aiNumberText.text = currentOpponent + ": " + aiGuess;

        AppendHistory(aiHistory, aiHistoryText, aiGuess);

        playerTurn = false;
        turnText.text = L10n.Get("answer_opponent", currentOpponent);

        HideButtons();

        if (playerSecretNumber > aiGuess)
            higherButton.SetActive(true);
        else if (playerSecretNumber < aiGuess)
            lowerButton.SetActive(true);
        else
            correctButton.SetActive(true);
    }

    public void Higher()
    {
        // Cheat detection: this answer contradicts an earlier hint.
        if (aiGuess + 1 > max)
        {
            HandleInconsistentAnswer();
            return;
        }

        min = aiGuess + 1;

        HideButtons();
        turnText.text = L10n.Get("your_guess");
        playerTurn = true;
    }

    public void Lower()
    {
        // Cheat detection: this answer contradicts an earlier hint.
        if (aiGuess - 1 < min)
        {
            HandleInconsistentAnswer();
            return;
        }

        max = aiGuess - 1;

        HideButtons();
        turnText.text = L10n.Get("your_guess");
        playerTurn = true;
    }

    // Adaptive difficulty: aims the AI so a regular player wins about half
    // their matches. Winning a lot → tougher AI (less randomness); losing a
    // lot → friendlier AI. Falls back to Normal with little/no history.
    static float AdaptiveRandomChance()
    {
        float winRate = GameStats.RecentWinRate();
        if (winRate < 0f) return DifficultyRandomChance[1]; // no data → Normal

        if (winRate > 0.6f) return 0.1f;  // streaking player → tougher
        if (winRate < 0.4f) return 0.5f;  // struggling player → friendlier
        return 0.25f;                     // balanced → near Normal
    }

    void HandleInconsistentAnswer()
    {
        // The player gave an answer impossible for their secret number
        // (e.g. "Higher" when the remaining range is already at 100).
        // End the round instead of letting the AI guess from an empty range.
        aiAnswerText.text = L10n.Get("caught_cheating", currentOpponent);
        EndGame(false);
    }

    public void Correct()
    {
        aiAnswerText.text = L10n.Get("opponent_found_number", currentOpponent);
        EndGame(false);
    }

    // Returns true when the guess was accepted, so the caller knows whether
    // to clear the input. Rejected guesses (wrong turn, out of range) keep it.
    public bool PlayerGuess(int guess)
    {
        if (!playerTurn || gameFinished) return false;

        // Reject guesses outside the range the player has already narrowed to.
        if (guess < playerMin || guess > playerMax)
        {
            aiAnswerText.text = L10n.Get("already_know_range", playerMin, playerMax);
            return false;
        }

        playerGuessCount++;

        // Show the player's own name (fall back to "You"), like NumberManager.
        string playerLabel = PlayerPrefs.GetString("PlayerName", "");
        if (string.IsNullOrEmpty(playerLabel))
            playerLabel = L10n.Get("you");

        aiAnswerText.text = playerLabel + ": " + guess;

        AppendHistory(playerHistory, playerHistoryText, guess);

        if (guess == aiSecretNumber)
        {
            aiAnswerText.text = playerLabel + ": " + guess + "\n" + L10n.Get("you_win");
            EndGame(true);
            return true;
        }

        if (guess < aiSecretNumber)
        {
            aiAnswerText.text = playerLabel + ": " + guess + "\n" + currentOpponent + ": " + L10n.Get("higher");
            if (guess + 1 > playerMin) playerMin = guess + 1;
        }
        else
        {
            aiAnswerText.text = playerLabel + ": " + guess + "\n" + currentOpponent + ": " + L10n.Get("lower");
            if (guess - 1 < playerMax) playerMax = guess - 1;
        }

        UpdateRangeText();

        playerTurn = false;

        turnText.text = L10n.Get("opponent_thinking", currentOpponent);
        Invoke(nameof(AIGuess), Random.Range(1.5f, 3.5f)); // review #5: tighter pacing (was 2.8–6.5)
        return true;
    }

    void EndGame(bool playerWon)
    {
        gameFinished = true;

        HideButtons();
        stopGameButton.SetActive(true);

        if (playerWon)
        {
            GameStats.RecordWin(playerGuessCount);
            Haptics.Success();
            GameEvents.MatchEnded(true, playerGuessCount);

            turnText.text = L10n.Get("you_win") + "\n" + L10n.Get("won_in_guesses", playerGuessCount);
            // Optimal binary search solves 1-100 in at most 7 guesses —
            // celebrate a perfect run.
            if (playerGuessCount <= 7)
                turnText.text += "\n" + L10n.Get("perfect_game");
            if (audioSource != null && winSound != null) // review #14: don't throw on unwired scenes
                audioSource.PlayOneShot(winSound);
            if (winConfetti != null)
                winConfetti.Burst();
        }
        else
        {
            int streakBeforeLoss = GameStats.CurrentStreak; // captured before reset
            GameStats.RecordLoss();
            Haptics.Error();
            GameEvents.MatchEnded(false, 0);

            turnText.text = L10n.Get("you_lose");
            if (audioSource != null && loseSound != null)
                audioSource.PlayOneShot(loseSound);

            OfferStreakSave(streakBeforeLoss);
        }

        // Interstitial at a natural break (match end), every 2nd match —
        // instead of gating every Play press. ShowAd's own caps still apply.
        if (adsManager != null && GameStats.Matches % 2 == 0)
            adsManager.ShowAd(null);
    }

    // Rewarded "save your streak": offered on a loss when the streak was
    // worth saving and a rewarded ad is loaded. The loss still counts —
    // only the streak survives.
    const int MinStreakToSave = 2;
    GameObject streakSaveButton;

    // A rewarded ad that earned its reward but whose close callback was lost
    // (app killed mid-ad) restores its streak here on the next launch. An ad
    // that was shown but never earned leaves no PendingRewardEarned flag, so
    // there is nothing to restore — the stale key is just swept.
    void ReconcilePendingStreakRestore()
    {
        int pending = PlayerPrefs.GetInt(AdsManager.PendingStreakRestoreKey, 0);
        if (pending <= 0) return;

        if (PlayerPrefs.GetInt(AdsManager.PendingRewardEarnedKey, 0) == 1)
            GameStats.RestoreStreak(pending);

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

        // Sit just above the rematch button.
        var stopRect = (RectTransform)stopGameButton.transform;
        var rect = (RectTransform)btn.transform;
        rect.anchorMin = stopRect.anchorMin;
        rect.anchorMax = stopRect.anchorMax;
        rect.pivot = stopRect.pivot;
        rect.anchoredPosition = stopRect.anchoredPosition + new Vector2(0f, 120f);

        streakSaveButton = btn.gameObject;
        btn.onClick.AddListener(() =>
        {
            // Destroy the button only once an ad is actually on its way. If
            // the ad is unavailable, keep the button and tell the player —
            // otherwise the tap would leave no button, no ad, no feedback.
            bool shown = adsManager.ShowRewardedAd(() =>
            {
                GameStats.RestoreStreak(streak);
                PlayerPrefs.DeleteKey(AdsManager.PendingStreakRestoreKey);
                PlayerPrefs.DeleteKey(AdsManager.PendingRewardEarnedKey);
                PlayerPrefs.Save();
                GameEvents.MatchEnded(false, 0); // refresh the menu stats label
            },
            () =>
            {
                if (aiAnswerText != null)
                    aiAnswerText.text = L10n.Get("ad_not_ready");
            });

            if (shown)
            {
                // Persist in case the app is killed after the reward is
                // earned but before the ad-close callback (see Start).
                PlayerPrefs.SetInt(AdsManager.PendingStreakRestoreKey, streak);
                PlayerPrefs.Save();

                Destroy(streakSaveButton);
                streakSaveButton = null;
            }
        });
    }

    // Review #3 + #4: replaces the old StopGame(), which silently replayed
    // with the player's stale secret number and the same opponent.
    // IMPORTANT: re-wire the stop-game button's OnClick to this method.
    public void RestartMatch()
    {
        CancelInvoke();

        if (streakSaveButton != null)
        {
            Destroy(streakSaveButton);
            streakSaveButton = null;
        }

        gameFinished = false;
        playerTurn = false;

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
