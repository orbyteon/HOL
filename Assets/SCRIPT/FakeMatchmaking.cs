using UnityEngine;
using System.Collections;
using TMPro;

public class FakeMatchmaking : MonoBehaviour
{
    public GameObject searchingPanel;
    public GameObject panelGame;
    public TMP_Text searchingText;

    public void StartSearch()
    {
        StartCoroutine(SearchRoutine());
    }

    IEnumerator SearchRoutine()
    {
        searchingPanel.SetActive(true);

        searchingText.text = "Searching opponent...";

        // τυχαίος χρόνος
        float waitTime;

        int randomTime = Random.Range(0, 3);

        if (randomTime == 0)
            waitTime = 5f;
        else if (randomTime == 1)
            waitTime = 8f;
        else
            waitTime = 10f;

        yield return new WaitForSeconds(waitTime);

        // 1 στις 6 φορές δεν βρίσκει αντίπαλο
        int failChance = Random.Range(0, 6);

        if (failChance == 0)
        {
            searchingText.text = "Opponent not found. Try again.";
            yield return new WaitForSeconds(2f);
            searchingPanel.SetActive(false);
            yield break;
        }

        searchingText.text = "Opponent found!";
        yield return new WaitForSeconds(1.5f);

        searchingPanel.SetActive(false);
        panelGame.SetActive(true);
    }
}
