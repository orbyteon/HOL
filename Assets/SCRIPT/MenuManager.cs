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
            // Mid-match exit, same as the old stop button: reload the scene
            // for a clean state. Solo only — live PvP duels keep their
            // explicit Leave button so the room closes cleanly.
            // Checked BEFORE panelPlay: panelPlay stays active for the
            // whole match, so it must not shadow this branch.
            SceneManager.LoadScene("MainMenu");
        }
        else if (panelPlay != null && panelPlay.activeSelf)
            BackToMenu();
        // On the main menu, back is a no-op — exit happens via the Quit
        // button so an accidental tap can't kill the app.
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
        OpenFindChallengerPanel();
    }

    void OpenFindChallengerPanel()
    {
        mainMenuPanel.SetActive(false);
        panelPlay.SetActive(true);
    }
}
