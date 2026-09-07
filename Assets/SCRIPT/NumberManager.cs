using UnityEngine;
using TMPro;

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
    bool submissionInProgress;

    public bool GameStarted => gameStarted;

    public bool HasCompleteValidValue
    {
        get
        {
            int value;
            return numberInput != null &&
                   int.TryParse(numberInput.text, out value) &&
                   value >= 1 && value <= 100;
        }
    }

    public bool CanSubmitCurrentValue =>
        gameManager != null &&
        HasCompleteValidValue &&
        (!gameStarted || gameManager.IsPlayerTurn);

    void OnEnable()
    {
        L10n.OnLanguageChanged += RefreshPlayerLabel;
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= RefreshPlayerLabel;
    }

    void Start()
    {
        RefreshPlayerLabel();
    }

    public void SubmitNumber()
    {
        if (submissionInProgress)
            return;
        if (gameManager != null && gameManager.IsMatchOver)
            return;

        submissionInProgress = true;
        try
        {
            SubmitNumberOnce();
        }
        finally
        {
            submissionInProgress = false;
        }
    }

    void SubmitNumberOnce()
    {
        if (numberInput == null || gameManager == null)
        {
            if (gameManager == null)
                Debug.LogError("[NumberManager] GameManager is required for Solo input.");
            return;
        }

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
            gameManager.SetPlayerNumber(playerNumber);
            gameManager.StartGame();
            if (!gameManager.HasLiveMatch)
                return;
            gameStarted = true;
            RefreshPlayerLabel();
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
            {
                messageText.gameObject.SetActive(true);
                messageText.text = L10n.Get(
                    "already_know_range",
                    gameManager.CurrentPlayerRangeMin,
                    gameManager.CurrentPlayerRangeMax);
                return;
            }
        }

        numberInput.text = "";
        numberInput.DeactivateInputField();
        if ((gameManager == null || !gameManager.IsMatchOver) &&
            numberInput.interactable && numberInput.gameObject.activeInHierarchy)
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
        submissionInProgress = false;
        RefreshPlayerLabel();

        messageText.gameObject.SetActive(false);
        stopButton.SetActive(false);
        playerGuessesPanel.SetActive(false);
        aiGuessesPanel.SetActive(false);

        numberInput.text = "";
    }

    void RefreshPlayerLabel()
    {
        if (playerNumberText == null) return;
        playerNumberText.text = gameStarted
            ? L10n.Get("solo_secret_value", playerNumber)
            : L10n.Get("solo_secret_unset");
    }

    public void ExitToMenu()
    {
        var menu = FindObjectOfType<MenuManager>(true);
        if (menu == null)
        {
            Debug.LogError(
                "[NumberManager] Refusing to bypass the Solo leave confirmation.");
            return;
        }
        menu.RequestSoloMatchExit();
    }
}
