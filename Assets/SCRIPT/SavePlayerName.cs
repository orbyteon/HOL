using UnityEngine;
using TMPro;

public class SavePlayerName : MonoBehaviour
{
    public TMP_InputField nameInput;

    public void SaveName()
    {
        PlayerPrefs.SetString("PlayerName", nameInput.text);
        PlayerPrefs.Save();
    }
}
