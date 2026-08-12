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
        playerNumberText.text = DisplayPlayerName() + ": ?";
    }

    public void SubmitNumber()
    {
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
            playerNumberText.text = DisplayPlayerName() + ": " + playerNumber;

            stopButton.SetActive(true);
            playerGuessesPanel.SetActive(true);
            aiGuessesPanel.SetActive(true);

            gameManager.SetPlayerNumber(playerNumber);
            gameManager.StartGame();

            gameStarted = true;
        }
        else
        {
            if (gameManager != null && !gameManager.IsPlayerTurn)
            {
                messageText.gameObject.SetActive(true);
                messageText.text = L10n.Get("wait_your_turn");
                return;
            }

            if (gameManager != null && !gameManager.PlayerGuess(number))
                return;
        }

        numberInput.text = "";
        numberInput.DeactivateInputField();
        if (gameManager == null || !gameManager.IsMatchOver)
            numberInput.ActivateInputField();
    }

    public void CloseInput()
    {
        if (numberInput != null)
            numberInput.DeactivateInputField();
    }

    public void ResetForNewMatch()
    {
        gameStarted = false;
        playerNumberText.text = DisplayPlayerName() + ": ?";

        messageText.gameObject.SetActive(false);
        stopButton.SetActive(false);
        playerGuessesPanel.SetActive(false);
        aiGuessesPanel.SetActive(false);

        numberInput.text = "";
    }

    static string DisplayPlayerName()
    {
        string playerName = PlayerPrefs.GetString("PlayerName", "");
        return string.IsNullOrWhiteSpace(playerName) ? L10n.Get("player_default") : playerName;
    }

    public void ExitToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
