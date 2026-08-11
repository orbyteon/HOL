using UnityEngine;
using TMPro;

public class SavePlayerName : MonoBehaviour
{
    public TMP_InputField nameInput;

    // Display names show up in the duel UI and in PlayFab room data —
    // keep them short enough to fit a label.
    const int MaxNameLength = 16;

    public void SaveName()
    {
        // Review #7: an empty saved name used to render as ": ?" in-game.
        string name = nameInput.text.Trim();
        if (string.IsNullOrEmpty(name))
            name = "Player";
        if (name.Length > MaxNameLength)
            name = name.Substring(0, MaxNameLength);

        PlayerPrefs.SetString("PlayerName", name);
        PlayerPrefs.Save();
    }
}
