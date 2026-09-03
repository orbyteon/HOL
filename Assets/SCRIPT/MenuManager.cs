using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject panelPlay;
    public GameObject panelSearching;

    public AdsManager adsManager;         // legacy reference; ads now show at match end (GameManager)
    public FakeMatchmaking matchmaking;   // optional: lets BackToMenu cancel a running search

    GameManager gameManager; // found once; used to detect the decided state
    SoloDuelVisuals soloVisuals;
    bool exitInProgress;

    public bool IsSoloLeaveConfirmationVisible =>
        soloVisuals != null && soloVisuals.IsLeaveConfirmationVisible;

    void Start()
    {
        // PanelGAME is serialized inactive on MainMenu startup. Include it so
        // decided-result Back can distinguish a finished match from a live one
        // as soon as direct Solo entry activates the board.
        gameManager = FindObjectOfType<GameManager>(true);
        soloVisuals = FindObjectOfType<SoloDuelVisuals>(true);
    }

    void Update()
    {
        // Android back button / gesture. Escape is Unity's mapping for it.
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (settingsPanel != null && settingsPanel.activeSelf)
            BackToMenu();
        else if (panelSearching != null && panelSearching.activeSelf)
            BackToMenu(); // cancels the running search via matchmaking
        else if (matchmaking != null && matchmaking.panelGame != null
            && matchmaking.panelGame.activeSelf)
        {
            if (IsSoloLeaveConfirmationVisible)
                CancelSoloMatchExit();
            else
                RequestSoloMatchExit();
        }
        else if (panelPlay != null && panelPlay.activeSelf)
            BackToMenu();
        // On the main menu, back is a no-op — exit happens via the Quit
        // button so an accidental tap can't kill the app.
    }

    void ConfirmMatchExit()
    {
        RequestSoloMatchExit();
    }

    // Runtime solo-board Back uses exactly the same guarded path as Android's
    // system Back gesture. It must never bypass the live-match confirmation by
    // calling NumberManager.ExitToMenu directly.
    public void RequestSoloMatchExit()
    {
        if (exitInProgress)
            return;
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>(true);
        if (soloVisuals == null)
            soloVisuals = FindObjectOfType<SoloDuelVisuals>(true);

        if (gameManager != null && gameManager.HasLiveMatch)
        {
            if (soloVisuals == null)
            {
                Debug.LogError(
                    "[MenuManager] Refusing to leave a live Solo match " +
                    "without its forfeit confirmation owner.");
                return;
            }
            soloVisuals.SetLeaveConfirmationVisible(true);
            return;
        }

        ExitSoloToMainMenu();
    }

    public void ConfirmSoloMatchExit()
    {
        if (exitInProgress)
            return;
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>(true);
        if (gameManager != null)
            gameManager.RecordLiveForfeitOnce();
        ExitSoloToMainMenu();
    }

    public void CancelSoloMatchExit()
    {
        if (soloVisuals == null)
            soloVisuals = FindObjectOfType<SoloDuelVisuals>(true);
        if (soloVisuals != null)
            soloVisuals.SetLeaveConfirmationVisible(false);
    }

    public void ExitSoloToMainMenu()
    {
        if (exitInProgress)
            return;
        exitInProgress = true;
        SceneManager.LoadScene("MainMenu");
    }

    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void BackToMenu()
    {
        // Never let a backgrounded search coroutine fire panelGame over the menu.
        if (matchmaking != null)
            matchmaking.CancelSearch();

        // Null-guarded like Update: a partially wired scene must not NRE out
        // of the back-navigation path.
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (panelPlay != null) panelPlay.SetActive(false);
        if (panelSearching != null) panelSearching.SetActive(false);

        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void OnPlayPressed()
    {
        // Ads moved to match end (GameManager.EndGame): gating every Play
        // press with an interstitial hurt retention and monetized the fake
        // "opponent not found" retry loop.
        if (matchmaking == null)
            matchmaking = FindObjectOfType<FakeMatchmaking>();
        if (matchmaking == null || matchmaking.panelGame == null)
        {
            Debug.LogError(
                "[MenuManager] Solo match cannot start: " +
                "FakeMatchmaking/panelGame is missing.");
            return;
        }

        // Solo is a local AI match. Enter the real board in the same call and
        // never expose the retired find-challenger/search presentation.
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (panelPlay != null) panelPlay.SetActive(false);
        if (panelSearching != null) panelSearching.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        matchmaking.StartSearch();
    }
}
