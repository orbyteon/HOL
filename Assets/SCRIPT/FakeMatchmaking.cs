using System.Collections;
using TMPro;
using UnityEngine;

// Deterministic Solo-vs-AI preparation transition.
//
// This is intentionally not public matchmaking and never claims that a remote
// human is being searched. The short presentation window lets the approved
// radar screen read clearly, then opens the existing local AI board. Gameplay
// ownership remains with GameManager/DuelRules.
public class FakeMatchmaking : MonoBehaviour
{
    public GameObject searchingPanel;
    public GameObject panelGame;
    public TMP_Text searchingText;
    public AudioSource foundSound;

    [Min(0f)] public float preparationSeconds = 0.85f;
    [Min(0f)] public float readyHoldSeconds = 0.25f;

    Coroutine preparationRoutine;
    bool readyPhase;

    public bool IsPreparing { get; private set; }

    void OnEnable()
    {
        L10n.OnLanguageChanged += RefreshStatusCopy;
    }

    public void StartSearch()
    {
        StopPendingTransition();

        if (panelGame != null)
            panelGame.SetActive(false);

        ResetSearchPresentation(true);
        IsPreparing = true;
        readyPhase = false;
        RefreshStatusCopy();
        preparationRoutine = StartCoroutine(PrepareComputerChallenger());
    }

    IEnumerator PrepareComputerChallenger()
    {
        if (preparationSeconds > 0f)
            yield return new WaitForSecondsRealtime(preparationSeconds);

        if (!IsPreparing)
            yield break;

        readyPhase = true;
        RefreshStatusCopy();
        if (foundSound != null)
            foundSound.Play();

        if (readyHoldSeconds > 0f)
            yield return new WaitForSecondsRealtime(readyHoldSeconds);

        if (!IsPreparing)
            yield break;

        IsPreparing = false;
        readyPhase = false;
        preparationRoutine = null;
        ResetSearchPresentation(false);

        if (panelGame != null)
            panelGame.SetActive(true);
    }

    // Back and the large Cancel CTA share this single cancellation path. It
    // invalidates every delayed callback before hiding the presentation.
    public void CancelSearch()
    {
        StopPendingTransition();
        IsPreparing = false;
        readyPhase = false;
        if (foundSound != null)
            foundSound.Stop();
        ResetSearchPresentation(false);
        if (panelGame != null)
            panelGame.SetActive(false);
    }

    void StopPendingTransition()
    {
        if (preparationRoutine != null)
        {
            StopCoroutine(preparationRoutine);
            preparationRoutine = null;
        }
        StopAllCoroutines();
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

        string text;
        if (readyPhase)
        {
            text = L10n.Current == L10n.Language.Greek
                ? "Ο AI ΑΝΤΙΠΑΛΟΣ ΕΙΝΑΙ ΕΤΟΙΜΟΣ!"
                : "AI OPPONENT READY!";
        }
        else
        {
            text = L10n.Current == L10n.Language.Greek
                ? "ΠΡΟΕΤΟΙΜΑΣΙΑ AI ΑΝΤΙΠΑΛΟΥ"
                : "PREPARING AI OPPONENT";
        }

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
