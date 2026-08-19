using TMPro;
using UnityEngine;

// Presentation-only owner for the portrait PvP result overlay. The controller
// supplies authoritative state; this component only paints it and refreshes the
// live player chip when the overlay opens.
public sealed class PvpResultPresentation : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text playerAttemptsText;
    public TMP_Text opponentAttemptsText;
    public TMP_Text revealedNumberText;
    public TMP_Text playerChipText;
    public GameObject trophy;

    string localizedTitleKey = "";
    string displayedTitle = "";
    int displayedRevealedNumber;
    bool isShown;

    void OnEnable()
    {
        L10n.OnLanguageChanged += RefreshForLanguage;
        if (isShown)
            RefreshForLanguage();
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= RefreshForLanguage;
    }

    void RefreshForLanguage()
    {
        if (!isShown) return;
        if (titleText != null)
            titleText.text = string.IsNullOrEmpty(localizedTitleKey)
                ? displayedTitle
                : L10n.Get(localizedTitleKey);
        if (revealedNumberText != null)
            revealedNumberText.text =
                L10n.Get("number_was", displayedRevealedNumber);
        RefreshPlayerChip();
    }

    public void Show(string title, int playerAttempts, int opponentAttempts,
        int revealedNumber)
    {
        Show(title, playerAttempts, opponentAttempts, revealedNumber, true);
    }

    public void Show(string title, int playerAttempts, int opponentAttempts,
        int revealedNumber, bool showTrophy)
    {
        localizedTitleKey = "";
        displayedTitle = title ?? "";
        ShowInternal(playerAttempts, opponentAttempts, revealedNumber,
            showTrophy);
    }

    public void ShowLocalized(string titleKey, int playerAttempts,
        int opponentAttempts, int revealedNumber, bool showTrophy)
    {
        localizedTitleKey = titleKey ?? "";
        displayedTitle = L10n.Get(localizedTitleKey);
        ShowInternal(playerAttempts, opponentAttempts, revealedNumber,
            showTrophy);
    }

    void ShowInternal(int playerAttempts, int opponentAttempts,
        int revealedNumber, bool showTrophy)
    {
        displayedRevealedNumber = revealedNumber;
        isShown = true;
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        transform.SetAsLastSibling();

        RefreshForLanguage();
        if (playerAttemptsText != null)
            playerAttemptsText.text = Mathf.Max(0, playerAttempts).ToString();
        if (opponentAttemptsText != null)
            opponentAttemptsText.text = Mathf.Max(0, opponentAttempts).ToString();
        if (trophy != null) trophy.SetActive(showTrophy);
    }

    void RefreshPlayerChip()
    {
        if (playerChipText != null)
        {
            string playerName = PlayerPrefs.GetString("PlayerName", "");
            if (string.IsNullOrWhiteSpace(playerName))
                playerName = L10n.Get("player_default");
            playerChipText.text = playerName + "  •  " +
                                  L10n.Get("stats_streak") + " " +
                                  GameStats.CurrentStreak;
        }
    }

    public void Hide()
    {
        isShown = false;
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }
}
