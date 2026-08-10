using UnityEngine;
using TMPro;
using System.Collections;

public class BlinkText : MonoBehaviour
{
    public TMP_Text textToBlink;

    void Start()
    {
        StartCoroutine(Blink());
    }

    IEnumerator Blink()
    {
        while (true)
        {
            textToBlink.enabled = false;
            yield return new WaitForSeconds(0.5f);

            textToBlink.enabled = true;
            yield return new WaitForSeconds(0.5f);
        }
    }
}
