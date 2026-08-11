using UnityEngine;
using TMPro;

public class SavePlayerName : MonoBehaviour
{
    public TMP_InputField nameInput;

    public void SaveName()
    {
        // Review #7: an empty saved name used to render as ": ?" in-game.
        string name = nameInput.text.Trim();
        if (string.IsNullOrEmpty(name))
            name = "Player";

        PlayerPrefs.SetString("PlayerName", name);
        PlayerPrefs.Save();
    }
}
