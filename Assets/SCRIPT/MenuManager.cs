using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject panelPlay;
    public GameObject panelSearching;

    public AdsManager adsManager; // 🔥 NEW

    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void BackToMenu()
    {
        settingsPanel.SetActive(false);
        panelPlay.SetActive(false);
        panelSearching.SetActive(false);

        mainMenuPanel.SetActive(true);
    }

    public void OnPlayPressed()
    {
        adsManager.ShowAd(OpenFindChallengerPanel); // 🔥 FIX
    }

    void OpenFindChallengerPanel()
    {
        mainMenuPanel.SetActive(false);
        panelPlay.SetActive(true);
    }
}