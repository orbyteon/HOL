using UnityEngine;
using System.Collections;
using TMPro;

public class FakeMatchmaking : MonoBehaviour
{
    public GameObject searchingPanel;
    public GameObject panelGame;
    public TMP_Text searchingText;

    bool isSearching; // review #8: prevent overlapping searches
    Coroutine dotsAnimation;

    public void StartSearch()
    {
        if (isSearching)
            return;

        StartCoroutine(SearchRoutine());
    }

    IEnumerator SearchRoutine()
    {
        isSearching = true;

        searchingPanel.SetActive(true);

        // Animate "Searching opponent" with cycling dots so the panel
        // doesn't look frozen.
        dotsAnimation = StartCoroutine(AnimateSearchingText());

        // Review #5: shortened waits (were 5/8/10s)
        float waitTime;

        int randomTime = Random.Range(0, 3);

        if (randomTime == 0)
            waitTime = 2f;
        else if (randomTime == 1)
            waitTime = 3.5f;
        else
            waitTime = 5f;

        yield return new WaitForSeconds(waitTime);

        StopDotsAnimation();

        // 1 in 6 searches finds no opponent
        int failChance = Random.Range(0, 6);

        if (failChance == 0)
        {
            searchingText.text = "Opponent not found. Try again.";
            yield return new WaitForSeconds(2f);
            searchingPanel.SetActive(false);
            isSearching = false;
            yield break;
        }

        searchingText.text = "Opponent found!";
        yield return new WaitForSeconds(1.5f);

        searchingPanel.SetActive(false);
        panelGame.SetActive(true);

        isSearching = false;
    }

    IEnumerator AnimateSearchingText()
    {
        const string baseText = "Searching opponent";
        int dots = 0;

        while (true)
        {
            searchingText.text = baseText + new string('.', dots);
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
        StopDotsAnimation();
    }
}
