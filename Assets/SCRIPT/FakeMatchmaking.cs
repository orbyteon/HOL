using UnityEngine;
using System.Collections;
using TMPro;

public class FakeMatchmaking : MonoBehaviour
{
    public GameObject searchingPanel;
    public GameObject panelGame;
    public TMP_Text searchingText;
    public AudioSource foundSound; // stinger, plays when an opponent is found

    bool isSearching; // review #8: prevent overlapping searches
    Coroutine dotsAnimation;

    public void StartSearch()
    {
        if (isSearching)
            return;

        StartCoroutine(SearchRoutine());
    }

    // Call this from a Cancel/Back button on the searching panel.
    public void CancelSearch()
    {
        if (!isSearching)
            return;

        StopAllCoroutines();
        isSearching = false;
        searchingPanel.SetActive(false);
    }

    IEnumerator SearchRoutine()
    {
        isSearching = true;

        searchingPanel.SetActive(true);

        // Animate "Searching opponent" with cycling dots so the panel
        // doesn't look frozen.
        dotsAnimation = StartCoroutine(AnimateSearchingText());

        // Just enough theatre to land the "found" stinger. The opponent is
        // a disclosed on-device AI, so long waits — and the old fabricated
        // 1-in-6 "no opponent found" — only taxed every single match with
        // dead time for a search that cannot actually fail.
        yield return new WaitForSeconds(Random.Range(0.9f, 1.8f));

        StopDotsAnimation();

        searchingText.text = L10n.Get("opponent_found");
        if (foundSound != null)
            foundSound.Play();
        yield return new WaitForSeconds(1f);

        searchingPanel.SetActive(false);
        panelGame.SetActive(true);

        isSearching = false;
    }

    IEnumerator AnimateSearchingText()
    {
        int dots = 0;

        while (true)
        {
            searchingText.text = L10n.Get("searching") + new string('.', dots);
            dots = (dots + 1) % 4;
            yield return new WaitForSeconds(0.4f);
        }
    }

    void StopDotsAnimation()
    {
        if (dotsAnimation != null)
        {
            StopCoroutine(dotsAnimation);
            dotsAnimation = null;
        }
    }

    void OnDisable()
    {
        // Don't leave the animation running if the object is deactivated.
        // Deactivation also kills SearchRoutine before it can reset the
        // guard, so clear it here or StartSearch is dead after re-enabling.
        StopDotsAnimation();
        isSearching = false;
    }
}
