using TMPro;
using UnityEngine;

// Abnormal PvP endings are deliberately separate from an authoritative match
// result. A missing room is not proof that the opponent left, and transport
// failure is not a win or loss. This component retains the typed reason so the
// copy can be repainted when the language changes without inferring gameplay.
public enum PvpTerminalReason
{
    None,
    ConnectionLost,
    RoomUnavailable,
    OpponentLeft,
}

public sealed class PvpTerminalPresentation : MonoBehaviour
{
    public GameObject terminalRoot;
    public TMP_Text titleText;
    public TMP_Text messageText;
    public TMP_Text resultStatusText;
    public GameObject terminalExitButton;
    public GameObject resultExitButton;

    public PvpTerminalReason Reason { get; private set; }
    public bool IsShown { get; private set; }
    public bool PreservesAuthoritativeResult { get; private set; }

    TMP_Text externalStatusText;

    void OnEnable()
    {
        L10n.OnLanguageChanged -= RefreshForLanguage;
        L10n.OnLanguageChanged += RefreshForLanguage;
        if (IsShown) RefreshForLanguage();
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= RefreshForLanguage;
    }

    public void Show(PvpTerminalReason reason, bool preserveAuthoritativeResult)
    {
        ShowInternal(reason, preserveAuthoritativeResult, null);
    }

    // Waiting-room failures use their existing status plate and Cancel button;
    // the typed owner still retains the reason and handles language changes.
    public void ShowStatus(PvpTerminalReason reason, TMP_Text statusText)
    {
        ShowInternal(reason, false, statusText);
    }

    void ShowInternal(PvpTerminalReason reason,
        bool preserveAuthoritativeResult, TMP_Text statusText)
    {
        Reason = reason;
        IsShown = reason != PvpTerminalReason.None;
        PreservesAuthoritativeResult = preserveAuthoritativeResult;
        externalStatusText = statusText;

        bool showTerminalRoot = IsShown && !preserveAuthoritativeResult &&
                                statusText == null;
        if (terminalRoot != null)
        {
            terminalRoot.SetActive(showTerminalRoot);
            if (showTerminalRoot) terminalRoot.transform.SetAsLastSibling();
        }
        if (terminalExitButton != null)
            terminalExitButton.SetActive(showTerminalRoot);
        if (resultExitButton != null && preserveAuthoritativeResult)
            resultExitButton.SetActive(true);

        RefreshForLanguage();
    }

    public void Hide()
    {
        IsShown = false;
        Reason = PvpTerminalReason.None;
        PreservesAuthoritativeResult = false;
        externalStatusText = null;
        if (terminalRoot != null) terminalRoot.SetActive(false);
        if (terminalExitButton != null) terminalExitButton.SetActive(false);
    }

    void RefreshForLanguage()
    {
        if (!IsShown) return;

        string titleKey;
        string messageKey;
        KeysFor(Reason, out titleKey, out messageKey);
        string message = L10n.Get(messageKey);

        if (externalStatusText != null)
            externalStatusText.text = message;
        else if (PreservesAuthoritativeResult)
        {
            if (resultStatusText != null) resultStatusText.text = message;
        }
        else
        {
            if (titleText != null) titleText.text = L10n.Get(titleKey);
            if (messageText != null) messageText.text = message;
        }
    }

    static void KeysFor(PvpTerminalReason reason, out string titleKey,
        out string messageKey)
    {
        switch (reason)
        {
            case PvpTerminalReason.ConnectionLost:
                titleKey = "pvp_terminal_connection_title";
                messageKey = "pvp_connection_lost";
                return;
            case PvpTerminalReason.OpponentLeft:
                titleKey = "pvp_terminal_opponent_title";
                messageKey = "pvp_opponent_left";
                return;
            default:
                titleKey = "pvp_terminal_room_title";
                messageKey = "pvp_room_unavailable";
                return;
        }
    }
}
