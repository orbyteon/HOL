using UnityEngine;
using TMPro;

public class SavePlayerName : MonoBehaviour
{
    public TMP_InputField nameInput;

    const int MaxNameLength = 16;

    public void SaveName()
    {
        // Empty is the canonical "use localized default" value. Persisting a
        // literal fallback like "Player" would bake the language into prefs.
        string name = nameInput != null ? nameInput.text.Trim() : "";
        if (name.Length > MaxNameLength)
            name = name.Substring(0, MaxNameLength);

        PlayerPrefs.SetString("PlayerName", name);
        PlayerPrefs.Save();
    }
}
