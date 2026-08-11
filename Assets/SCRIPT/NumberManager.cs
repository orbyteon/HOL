using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class NumberManager : MonoBehaviour
{
    public TMP_InputField numberInput;
    public TMP_Text playerNumberText;
    public TMP_Text messageText;

    public GameObject stopButton;
    public GameObject playerGuessesPanel;
    public GameObject aiGuessesPanel;

    public GameManager gameManager;

    public int playerNumber;

    bool gameStarted = false;

    void Start()
    {
        // Load the player name saved in Settings.
        string playerName = PlayerPrefs.GetString("PlayerName", "Player");

        // Show the name before the game starts.
        playerNumberText.text = playerName + ": ?";
    }

    public void SubmitNumber()
    {
        // Round already decided: any submit — Confirm tap or keyboard Done,
        // valid or empty — should do nothing. Checked BEFORE validation so a
        // stray empty submit can't flash "Invalid number" over the result.
        if (gameManager != null && gameManager.IsMatchOver)
            return;

        int number;

        if (!int.TryParse(numberInput.text, out number))
        {
            messageText.gameObject.SetActive(true);
            messageText.text = L10n.Get("invalid_number");
            return;
        }

        if (number < 1 || number > 100)
        {
            messageText.gameObject.SetActive(true);
            messageText.text = L10n.Get("number_out_of_range");
            return;
        }

        messageText.gameObject.SetActive(false);

        if (!gameStarted)
        {
            playerNumber = number;

            // Use the saved player name.
            string playerName = PlayerPrefs.GetString("PlayerName", "Player");

            // Show name + secret number.
            playerNumberText.text = playerName + ": " + playerNumber;

            stopButton.SetActive(true);
            playerGuessesPanel.SetActive(true);
            aiGuessesPanel.SetActive(true);

            gameManager.SetPlayerNumber(playerNumber);
            gameManager.StartGame();

            gameStarted = true;
        }
        else
        {
            // Review #6: don't silently swallow (and erase) a guess typed
            // during the opponent's turn — tell the player and keep the input.
            if (gameManager != null && !gameManager.IsPlayerTurn)
            {
                messageText.gameObject.SetActive(true);
                messageText.text = L10n.Get("wait_your_turn");
                return;
            }

            if (gameManager != null)
            {
                // A rejected guess (e.g. outside the known range) keeps the
                // input so the player can adjust it instead of retyping.
                if (!gameManager.PlayerGuess(number))
                    return;
            }
        }

        numberInput.text = "";
        numberInput.DeactivateInputField();
        // Don't pop the soft keyboard back up over the result screen — the
        // winning guess ends the match; refocus only while play continues.
        if (gameManager == null || !gameManager.IsMatchOver)
            numberInput.ActivateInputField();
    }

    // Called by GameManager.EndGame: the match can end on the OPPONENT's
    // turn (AI finds your number), when the soft keyboard was already
    // refocused after our last guess — close it so it doesn't cover the
    // result screen.
    public void CloseInput()
    {
        if (numberInput != null)
            numberInput.DeactivateInputField();
    }

    // Review #3: called by GameManager.RestartMatch so a rematch starts from
    // the number-entry state instead of reusing the old secret.
    public void ResetForNewMatch()
    {
        gameStarted = false;

        string playerName = PlayerPrefs.GetString("PlayerName", "Player");
        playerNumberText.text = playerName + ": ?";

        messageText.gameObject.SetActive(false);
        stopButton.SetActive(false);
        playerGuessesPanel.SetActive(false);
        aiGuessesPanel.SetActive(false);

        numberInput.text = "";
    }

    // Review #4: renamed from StopGame (which collided with GameManager's
    // opposite-behavior StopGame). Re-wire the exit button's OnClick to this.
    public void ExitToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
