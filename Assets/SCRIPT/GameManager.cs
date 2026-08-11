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
        stopGameButton.SetActive(false);
        HideButtons();

        int randomIndex = Random.Range(0, fakeNames.Length);
        currentOpponent = fakeNames[randomIndex];

        opponentNameText.text = "Opponent: " + currentOpponent;

        if (turnText != null)
            turnText.text = "Enter your number";
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
            turnText.text = currentOpponent + " thinking...";
            Invoke(nameof(AIGuess), Random.Range(0.8f, 1.5f));
        }
        else
        {
            turnText.text = "Your guess";
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
        turnText.text = "Answer " + currentOpponent;

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
        turnText.text = "Your guess";
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
        turnText.text = "Your guess";
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
        aiAnswerText.text = "That doesn't add up! " + currentOpponent + " caught you cheating.";
        EndGame(false);
    }

    public void Correct()
    {
        aiAnswerText.text = currentOpponent + " found your number!";
        EndGame(false);
    }

    public void PlayerGuess(int guess)
    {
        if (!playerTurn || gameFinished) return;

        // Reject guesses outside the range the player has already narrowed to.
        if (guess < playerMin || guess > playerMax)
        {
            aiAnswerText.text = "You already know it's between " + playerMin + " and " + playerMax + "!";
            return;
        }

        playerGuessCount++;

        aiAnswerText.text = "Player: " + guess;

        AppendHistory(playerHistory, playerHistoryText, guess);

        if (guess == aiSecretNumber)
        {
            aiAnswerText.text = "Player: " + guess + "\nPlayer wins!";
            EndGame(true);
            return;
        }

        if (guess < aiSecretNumber)
        {
            aiAnswerText.text = "Player: " + guess + "\n" + currentOpponent + ": Higher";
            if (guess + 1 > playerMin) playerMin = guess + 1;
        }
        else
        {
            aiAnswerText.text = "Player: " + guess + "\n" + currentOpponent + ": Lower";
            if (guess - 1 < playerMax) playerMax = guess - 1;
        }

        UpdateRangeText();

        playerTurn = false;

        turnText.text = currentOpponent + " thinking...";
        Invoke(nameof(AIGuess), Random.Range(1.5f, 3.5f)); // review #5: tighter pacing (was 2.8–6.5)
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

            turnText.text = "YOU WIN!\nIn " + playerGuessCount + " guesses";
            if (audioSource != null && winSound != null) // review #14: don't throw on unwired scenes
                audioSource.PlayOneShot(winSound);
            if (winConfetti != null)
                winConfetti.Burst();
        }
        else
        {
            GameStats.RecordLoss();
            Haptics.Error();
            GameEvents.MatchEnded(false, 0);

            turnText.text = "YOU LOSE!";
            if (audioSource != null && loseSound != null)
                audioSource.PlayOneShot(loseSound);
        }

        // Interstitial at a natural break (match end), every 2nd match —
        // instead of gating every Play press. ShowAd's own caps still apply.
        if (adsManager != null && GameStats.Matches % 2 == 0)
            adsManager.ShowAd(null);
    }

    // Review #3 + #4: replaces the old StopGame(), which silently replayed
    // with the player's stale secret number and the same opponent.
    // IMPORTANT: re-wire the stop-game button's OnClick to this method.
    public void RestartMatch()
    {
        CancelInvoke();

        gameFinished = false;
        playerTurn = false;

        int randomIndex = Random.Range(0, fakeNames.Length);
        currentOpponent = fakeNames[randomIndex];
        opponentNameText.text = "Opponent: " + currentOpponent;

        aiNumberText.text = "?";
        aiAnswerText.text = "";
        turnText.text = "Enter your number";

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
            rangeText.text = "Between " + playerMin + " and " + playerMax;
    }
}
