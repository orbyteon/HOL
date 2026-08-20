using UnityEngine;
using TMPro;

public class FakeMatchmaking : MonoBehaviour
{
    public GameObject searchingPanel;
    public GameObject panelGame;
    public TMP_Text searchingText;
    public AudioSource foundSound;

    // Kept as the scene-authored Solo button target so existing bindings remain
    // intact. Solo is local AI play, so entry is a synchronous panel transition:
    // there is no search coroutine, artificial wait, or random failure path.
    public void StartSearch()
    {
        StopAllCoroutines();
        ResetLegacySearchPresentation();

        if (panelGame != null)
            panelGame.SetActive(true);
    }

    // Back/cancel remains safe for the existing scene and runtime bindings.
    // With no deferred Solo callback, it cannot reopen the board later.
    public void CancelSearch()
    {
        StopAllCoroutines();
        if (foundSound != null) foundSound.Stop();
        ResetLegacySearchPresentation();
    }

    void ResetLegacySearchPresentation()
    {
        if (searchingPanel == null) return;

        var canvasGroup = searchingPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        searchingPanel.SetActive(false);
    }

    void OnDisable()
    {
        CancelSearch();
    }
}
