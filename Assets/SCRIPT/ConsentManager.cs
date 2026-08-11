using UnityEngine;

// Review #9: first-launch ads-consent dialog.
//
// Unity editor wiring required:
//   1. Create a UI panel (ConsentPanel) in MainMenu with a short message, e.g.
//      "This game shows ads. Allow personalized ads?" and two buttons: Yes / No.
//   2. Add this component to a scene object; drag the panel and the AdsManager in.
//   3. Wire the Yes button OnClick -> ConsentManager.AcceptPersonalized
//      and the No button OnClick -> ConsentManager.DeclinePersonalized.
//
// The panel shows only until the player answers once; the choice is stored in
// PlayerPrefs ("AdsConsent") and AdsManager initializes the SDK after it.
public class ConsentManager : MonoBehaviour
{
    public GameObject consentPanel;
    public AdsManager adsManager;

    const string ConsentPrefKey = "AdsConsent";

    void Start()
    {
        bool alreadyAnswered = PlayerPrefs.HasKey(ConsentPrefKey);

        if (consentPanel != null)
            consentPanel.SetActive(!alreadyAnswered);
        else
            Debug.LogError("ConsentManager: consentPanel reference is not wired in the Inspector.");
    }

    public void AcceptPersonalized()
    {
        Choose(true);
    }

    public void DeclinePersonalized()
    {
        Choose(false);
    }

    void Choose(bool consent)
    {
        consentPanel.SetActive(false);

        // Persist here as the single source of truth — the choice must be
        // saved even if the AdsManager reference is missing in the scene.
        PlayerPrefs.SetInt(ConsentPrefKey, consent ? 1 : 0);
        PlayerPrefs.Save();

        if (adsManager != null)
            adsManager.OnConsentChosen(consent);
        else
            Debug.LogError("ConsentManager: adsManager reference is not wired in the Inspector — consent saved, but ads will not initialize this session.");
    }
}
