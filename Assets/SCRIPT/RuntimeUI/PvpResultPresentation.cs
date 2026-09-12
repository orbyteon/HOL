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
    public TMP_Text explanationText;
    public TMP_Text playerChipText;
    public TMP_Text opponentNameText;
    public GameObject trophy;

    string localizedTitleKey = "";
    string displayedTitle = "";
    int displayedRevealedNumber;
    bool isShown;
    string opponentName = "";
    PvpResultExplanation explanation = PvpResultExplanation.Capture(null);

    public void SetResultSnapshot(PvpRoomState state)
    {
        explanation = PvpResultExplanation.Capture(state);
        PaintExplanation();
    }

    void PaintExplanation()
    {
        if (explanationText == null) return;
        explanationText.richText = false;
        if (!explanation.Final) { explanationText.text = ""; return; }
        string key = "pvp_reason_" + (string.IsNullOrEmpty(explanation.Reason) ? "legacy" : explanation.Reason);
        if (explanation.Reason == "only_correct" && !string.IsNullOrWhiteSpace(explanation.ForfeitedName))
            key = "pvp_reason_forfeit";
        explanationText.text = L10n.Get(key, explanation.WinnerName,
            explanation.WinnerCandidates, explanation.OtherCandidates, explanation.ForfeitedName);
    }

    // Called for every arriving room snapshot, including later finished-room
    // polls. Never freeze an empty construction-time label as opponent identity.
    public void SetOpponentName(string value)
    {
        opponentName = value ?? "";
        if (opponentNameText != null) opponentNameText.text = opponentName;
    }

    void OnEnable()
    {
        L10n.OnLanguageChanged -= RefreshForLanguage;
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
        SetOpponentName(opponentName);
        PaintExplanation();
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
        explanation = PvpResultExplanation.Capture(null);
        PaintExplanation();
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }
}
