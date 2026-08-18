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

    public void Show(string title, int playerAttempts, int opponentAttempts,
        int revealedNumber)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (titleText != null) titleText.text = title;
        if (playerAttemptsText != null)
            playerAttemptsText.text = Mathf.Max(0, playerAttempts).ToString();
        if (opponentAttemptsText != null)
            opponentAttemptsText.text = Mathf.Max(0, opponentAttempts).ToString();
        if (revealedNumberText != null)
            revealedNumberText.text = L10n.Get("number_was", revealedNumber);

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
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }
}
