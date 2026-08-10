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
        // Φορτώνουμε το όνομα του παίκτη από τα Settings
        string playerName = PlayerPrefs.GetString("PlayerName", "Player");

        // Εμφανίζουμε το όνομα πριν ξεκινήσει το παιχνίδι
        playerNumberText.text = playerName + ": ?";
    }

    public void SubmitNumber()
    {
        int number;

        if (!int.TryParse(numberInput.text, out number))
        {
            messageText.gameObject.SetActive(true);
            messageText.text = "Enter a valid number";
            return;
        }

        if (number < 1 || number > 100)
        {
            messageText.gameObject.SetActive(true);
            messageText.text = "Number must be between 1 and 100";
            return;
        }

        messageText.gameObject.SetActive(false);

        if (!gameStarted)
        {
            playerNumber = number;

            // Παίρνουμε το όνομα που αποθηκεύτηκε
            string playerName = PlayerPrefs.GetString("PlayerName", "Player");

            // Εμφανίζουμε όνομα + αριθμό
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
            if (gameManager != null)
                gameManager.PlayerGuess(number);
        }

        numberInput.text = "";
        numberInput.DeactivateInputField();
        numberInput.ActivateInputField();
    }

    public void StopGame()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
