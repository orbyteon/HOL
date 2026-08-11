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

    void Reset()
    {
        text = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        phase = 0f;
        if (text == null) text = GetComponent<TMP_Text>();
    }

    void Update()
    {
        if (text == null) return;

        phase += Time.unscaledDeltaTime;
        float s = 0.5f + 0.5f * Mathf.Sin(phase / period * Mathf.PI * 2f);
        var c = text.color;
        c.a = Mathf.Lerp(minAlpha, 1f, s);
        text.color = c;
    }

    void OnDisable()
    {
        if (text != null)
        {
            var c = text.color;
            c.a = 1f;
            text.color = c;
        }
    }
}
