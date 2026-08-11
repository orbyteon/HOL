using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public TMP_Text aiNumberText;
    public TMP_Text aiAnswerText;
    public TMP_Text turnText;
    public TMP_Text opponentNameText;

    public GameObject higherButton;
    public GameObject lowerButton;
    public GameObject correctButton;
    public GameObject stopGameButton;

    public AudioSource audioSource;
    public AudioClip winSound;
    public AudioClip loseSound;

    public NumberManager numberManager; // review #3: wire in Inspector
    public ConfettiBurst winConfetti;   // optional: drag a ConfettiBurst for win celebration

    // Review #6: lets NumberManager give feedback instead of silently
    // swallowing guesses typed during the opponent's turn.
    public bool IsPlayerTurn => playerTurn && !gameFinished;

    int min = 1;
    int max = 100;
    int aiGuess;

    bool playerTurn = false;
    bool gameFinished = false;
    bool firstAIGuess = true;

    int playerSecretNumber;
    int aiSecretNumber;

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

        gameFinished = false;
        playerTurn = false;
        firstAIGuess = true;

        aiSecretNumber = Random.Range(1, 101);

        aiNumberText.text = "?";
        aiAnswerText.text = "";

        stopGameButton.SetActive(false);
        HideButtons();

        // 🔥 RANDOM START (ΜΟΝΗ ΑΛΛΑΓΗ)
        bool aiStarts = Random.value < 0.5f;

        if (aiStarts)
        {
            turnText.text = currentOpponent + " thinking...";
            Invoke("AIGuess", Random.Range(0.8f, 1.5f));
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
            if (Random.value < 0.2f)
                aiGuess = Random.Range(min, max + 1);
            else
                aiGuess = (min + max) / 2;
        }

        aiNumberText.text = currentOpponent + ": " + aiGuess;

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
        min = aiGuess + 1;

        HideButtons();
        turnText.text = "Your guess";
        playerTurn = true;
    }

    public void Lower()
    {
        max = aiGuess - 1;

        HideButtons();
        turnText.text = "Your guess";
        playerTurn = true;
    }

    public void Correct()
    {
        aiAnswerText.text = currentOpponent + " found your number!";
        EndGame(false);
    }

    public void PlayerGuess(int guess)
    {
        if (!playerTurn || gameFinished) return;

        aiAnswerText.text = "Player: " + guess;

        if (guess == aiSecretNumber)
        {
            aiAnswerText.text = "Player: " + guess + "\nPlayer wins!";
            EndGame(true);
            return;
        }

        if (guess < aiSecretNumber)
            aiAnswerText.text = "Player: " + guess + "\n" + currentOpponent + ": Higher";
        else
            aiAnswerText.text = "Player: " + guess + "\n" + currentOpponent + ": Lower";

        playerTurn = false;

        turnText.text = currentOpponent + " thinking...";
        Invoke("AIGuess", Random.Range(1.5f, 3.5f)); // review #5: tighter pacing (was 2.8–6.5)
    }

    void EndGame(bool playerWon)
    {
        gameFinished = true;

        HideButtons();
        stopGameButton.SetActive(true);

        if (playerWon)
        {
            turnText.text = "YOU WIN!";
            if (audioSource != null && winSound != null) // review #14: don't throw on unwired scenes
                audioSource.PlayOneShot(winSound);
            if (winConfetti != null)
                winConfetti.Burst();
        }
        else
        {
            turnText.text = "YOU LOSE!";
            if (audioSource != null && loseSound != null)
                audioSource.PlayOneShot(loseSound);
        }
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

        stopGameButton.SetActive(false);
        HideButtons();

        if (numberManager != null)
            numberManager.ResetForNewMatch();
    }
}