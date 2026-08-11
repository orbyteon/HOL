using UnityEngine;
using TMPro;

// Animates trailing dots on a TMP text: "Searching opponent" → ".", "..", "...".
// Set baseText from code or leave empty to capture the text at enable time.
public class AnimatedEllipsis : MonoBehaviour
{
    public TMP_Text text;
    public string baseText = "";
    public float stepSeconds = 0.4f;

    float timer;
    int dots;

    void Reset()
    {
        text = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        if (text == null) text = GetComponent<TMP_Text>();
        if (string.IsNullOrEmpty(baseText) && text != null)
            baseText = text.text.TrimEnd('.');
        timer = 0f;
        dots = 0;
    }

    // Call this when the underlying message changes (e.g. from FakeMatchmaking).
    public void SetBaseText(string t)
    {
        baseText = t.TrimEnd('.');
        dots = 0;
        timer = 0f;
        if (text != null) text.text = baseText;
    }

    void Update()
    {
        if (text == null) return;

        timer += Time.unscaledDeltaTime;
        if (timer >= stepSeconds)
        {
            timer = 0f;
            dots = (dots + 1) % 4;
            text.text = baseText + new string('.', dots);
        }
    }
}
