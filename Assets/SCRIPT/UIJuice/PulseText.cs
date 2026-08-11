using UnityEngine;
using TMPro;

// Smooth alpha pulse for TMP text — a softer, more polished alternative to
// the hard on/off BlinkText. Safe with panel toggling (pure Update, no coroutine).
public class PulseText : MonoBehaviour
{
    public TMP_Text text;
    [Range(0f, 1f)] public float minAlpha = 0.25f;
    public float period = 1.4f;

    float phase;
    float baseAlpha = 1f; // the label's authored alpha — pulse peak and rest state

    void Reset()
    {
        text = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        // Start at the peak of the sine so enabling never pops the alpha.
        phase = period * 0.25f;
        if (text == null) text = GetComponent<TMP_Text>();
        if (text != null) baseAlpha = text.color.a;
    }

    void Update()
    {
        if (text == null) return;

        phase += Time.unscaledDeltaTime;
        float s = 0.5f + 0.5f * Mathf.Sin(phase / period * Mathf.PI * 2f);
        var c = text.color;
        c.a = Mathf.Lerp(minAlpha, baseAlpha, s);
        text.color = c;
    }

    void OnDisable()
    {
        // Restore the authored alpha, not a hardcoded 1 — labels designed
        // translucent (e.g. 0.6-alpha hints) must not brighten permanently.
        if (text != null)
        {
            var c = text.color;
            c.a = baseAlpha;
            text.color = c;
        }
    }
}
