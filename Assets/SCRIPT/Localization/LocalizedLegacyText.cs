using UnityEngine;
using UnityEngine.UI;

// Legacy-Text twin of LocalizedText: RuntimeUI builds UnityEngine.UI.Text
// labels, which LocalizedText (TMP-only) cannot attach to. Same behavior —
// set the key and the text follows the selected language live.
[RequireComponent(typeof(Text))]
public class LocalizedLegacyText : MonoBehaviour
{
    [Tooltip("Key into the L10n table, e.g. 'play'")]
    public string key;

    Text label;

    void Awake()
    {
        label = GetComponent<Text>();
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
