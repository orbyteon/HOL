using UnityEngine;
using TMPro;
using System.Collections;

public class BlinkText : MonoBehaviour
{
    public TMP_Text textToBlink;

    Coroutine blinkRoutine;

    // Review #2: Start() runs only once per lifetime, so a panel that is
    // toggled off and on would permanently stop blinking (and could stay
    // stuck invisible). OnEnable/OnDisable restart the blink every time.
    void OnEnable()
    {
        if (textToBlink == null) return; // unwired reference — nothing to blink
        blinkRoutine = StartCoroutine(Blink());
    }

    void OnDisable()
    {
        if (blinkRoutine != null)
            StopCoroutine(blinkRoutine);

        if (textToBlink != null)
            textToBlink.enabled = true; // never leave the text stuck hidden
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
