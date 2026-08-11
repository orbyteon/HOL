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
        consentPanel.SetActive(!alreadyAnswered);
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

        if (adsManager != null)
            adsManager.OnConsentChosen(consent);
    }
}
