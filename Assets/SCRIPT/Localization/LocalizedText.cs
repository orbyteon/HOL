using UnityEngine;
using TMPro;

// Put this on any GameObject with a TMP_Text, set the key, and the text
// follows the selected language (updates live when the language changes).
[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [Tooltip("Key into the L10n table, e.g. 'play'")]
    public string key;

    TMP_Text label;

    void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        L10n.OnLanguageChanged += Apply;
        Apply();
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= Apply;
    }

    void Apply()
    {
        if (!string.IsNullOrEmpty(key))
            label.text = L10n.Get(key);
    }
}
