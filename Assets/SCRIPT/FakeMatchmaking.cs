using System;
using TMPro;
using UnityEngine;

// Truthful Solo-vs-AI preparation transition.
//
// This is not public matchmaking and never claims that a remote human is being
// searched. The approved radar modal remains visible only while the existing
// local duel board performs its real first-frame construction. Gameplay remains
// owned by GameManager, NumberManager and DuelRules.
public class FakeMatchmaking : MonoBehaviour
{
    public GameObject searchingPanel;
    public GameObject panelGame;
    public TMP_Text searchingText;
    public AudioSource foundSound;

    bool readyPhase;

    // Non-serialized lifecycle seam used by EditMode tests. Production leaves
    // this null and therefore validates the real HolDuelBoardLayout controls.
    internal Func<bool> BoardReadyProbe { get; set; }

    public bool IsPreparing { get; private set; }

    void OnEnable()
    {
        L10n.OnLanguageChanged -= RefreshStatusCopy;
        L10n.OnLanguageChanged += RefreshStatusCopy;
    }

    void Update()
    {
        TickPreparation();
    }

    public void StartSearch()
    {
        // A second tap while the blocking modal is already active is a no-op.
        // One deterministic state machine remains authoritative.
        if (IsPreparing) return;

        if (panelGame == null)
        {
            Debug.LogError("[FakeMatchmaking] Solo game panel is missing.");
            ResetSearchPresentation(false);
            IsPreparing = false;
            return;
        }

        // The board starts its actual Unity initialization immediately behind
        // the modal. There is no timer, random delay or fake queue simulation.
        panelGame.SetActive(true);
        ResetSearchPresentation(true);
        IsPreparing = true;
        readyPhase = false;
        RefreshStatusCopy();
    }

    // Production calls this from Update. EditMode tests invoke the same method
    // directly, avoiding editor-only coroutine scheduling differences.
    internal void TickPreparation()
    {
        if (!IsPreparing || !IsLocalBoardReady())
            return;

        if (!readyPhase)
        {
            readyPhase = true;
            RefreshStatusCopy();
            if (foundSound != null)
                foundSound.Play();
            return;
        }

        IsPreparing = false;
        readyPhase = false;
        ResetSearchPresentation(false);
    }

    bool IsLocalBoardReady()
    {
        if (BoardReadyProbe != null)
            return BoardReadyProbe();

        if (panelGame == null || !panelGame.activeInHierarchy)
            return false;

        var layout = panelGame.GetComponentInChildren<HolDuelBoardLayout>(true);
        return layout != null &&
               layout.KeypadRoot != null &&
               layout.SubmitControl != null;
    }

    // Back and the large Cancel CTA share this single cancellation path. There
    // is no deferred timer/coroutine left that can reopen gameplay afterward.
    public void CancelSearch()
    {
        IsPreparing = false;
        readyPhase = false;
        if (foundSound != null)
            foundSound.Stop();
        ResetSearchPresentation(false);
        if (panelGame != null)
            panelGame.SetActive(false);
    }

    void ResetSearchPresentation(bool visible)
    {
        if (searchingPanel == null) return;

        var canvasGroup = searchingPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = searchingPanel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
        searchingPanel.SetActive(visible);
    }

    void RefreshStatusCopy()
    {
        if (searchingText == null || !IsPreparing) return;

        string text = L10n.Get(
            readyPhase ? "solo_ai_ready" : "solo_ai_preparing");

        var ellipsis = searchingText.GetComponent<AnimatedEllipsis>();
        if (ellipsis != null)
        {
            ellipsis.enabled = !readyPhase;
            if (!readyPhase)
                ellipsis.SetBaseText(text);
        }

        searchingText.text = text;
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= RefreshStatusCopy;
        CancelSearch();
    }
}
