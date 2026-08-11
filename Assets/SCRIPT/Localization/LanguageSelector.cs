using UnityEngine;
using TMPro;

// Settings-screen language picker. Wire two buttons to SetEnglish() /
// SetGreek(), or a single toggle button to ToggleLanguage().
// Optionally wire a TMP_Text to show the active language.
public class LanguageSelector : MonoBehaviour
{
    public TMP_Text currentLanguageLabel; // optional

    void OnEnable()
    {
        L10n.OnLanguageChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= Refresh;
    }

    public void SetEnglish()
    {
        L10n.SetLanguage(L10n.Language.English);
    }

    public void SetGreek()
    {
        L10n.SetLanguage(L10n.Language.Greek);
    }

    public void ToggleLanguage()
    {
        L10n.SetLanguage(L10n.Current == L10n.Language.English
            ? L10n.Language.Greek
            : L10n.Language.English);
    }

    void Refresh()
    {
        if (currentLanguageLabel != null)
            currentLanguageLabel.text = L10n.Current == L10n.Language.English
                ? "English"
                : "Ελληνικά";
    }
}
