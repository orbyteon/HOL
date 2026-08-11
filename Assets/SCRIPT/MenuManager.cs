using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject panelPlay;
    public GameObject panelSearching;

    public AdsManager adsManager;         // legacy reference; ads now show at match end (GameManager)
    public FakeMatchmaking matchmaking;   // optional: lets BackToMenu cancel a running search

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

        settingsPanel.SetActive(false);
        panelPlay.SetActive(false);
        panelSearching.SetActive(false);

        mainMenuPanel.SetActive(true);
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
