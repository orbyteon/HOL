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

    public NumberManager numberManager;
    public ConfettiBurst winConfetti;
    public AdsManager adsManager;

    public bool IsPlayerTurn => playerTurn && !gameFinished;
    public bool IsMatchOver => gameFinished;

    // The player's narrowed hunting interval, for UI that visualizes the
    // converging range (RangeBar). Fired from UpdateRangeText.
    public System.Action<int, int> OnPlayerRangeChanged;
    public int PlayerRangeMin => playerMin;
    public int PlayerRangeMax => playerMax;

    int min = 1;
    int max = 100;
    int aiGuess;

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
        min = aiGuess + 1;
        HideButtons();
        turnText.text = L10n.Get("your_guess");
        playerTurn = true;
        FocusGuessInput();
    }

    public void Lower()
    {
        max = aiGuess - 1;
        HideButtons();
        turnText.text = L10n.Get("your_guess");
        playerTurn = true;
        FocusGuessInput();
    }

    // Answering the opponent hands the turn back to the player — put the
    // caret straight into the guess field so typing can start immediately.
    void FocusGuessInput()
    {
        if (numberManager != null)
            numberManager.FocusInput();
    }

    static float AdaptiveRandomChance()
    {
        float winRate = GameStats.RecentWinRate();
        if (winRate < 0f) return DifficultyRandomChance[1];

        if (winRate > 0.6f) return 0.1f;
        if (winRate < 0.4f) return 0.5f;
        return 0.25f;
    }

    public void Correct()
    {
        aiAnswerText.text = L10n.Get("opponent_found_number", currentOpponent);
        EndGame(false);
    }

    public bool PlayerGuess(int guess)
    {
        if (!playerTurn || gameFinished) return false;

        if (guess < playerMin || guess > playerMax)
        {
            aiAnswerText.text = L10n.Get("already_know_range", playerMin, playerMax);
            return false;
        }

        playerGuessCount++;

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
        Invoke(nameof(AIGuess), Random.Range(1.5f, 3.5f));
        return true;
    }

    void EndGame(bool playerWon)
    {
        gameFinished = true;

        HideButtons();
        stopGameButton.SetActive(true);

        if (numberManager != null)
            numberManager.CloseInput();

        if (playerWon)
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

        OnPlayerRangeChanged?.Invoke(playerMin, playerMax);
    }
}
