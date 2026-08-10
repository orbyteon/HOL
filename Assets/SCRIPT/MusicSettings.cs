using UnityEngine;
using UnityEngine.UI;

public class MusicSettings : MonoBehaviour
{
    public Toggle musicToggle;
    public AudioSource musicSource;

    void Start()
    {
        bool musicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;

        musicToggle.isOn = musicOn;
        musicSource.mute = !musicOn;

        musicToggle.onValueChanged.AddListener(OnToggleChanged);
    }

    void OnToggleChanged(bool isOn)
    {
        musicSource.mute = !isOn;

        PlayerPrefs.SetInt("MusicOn", isOn ? 1 : 0);
        PlayerPrefs.Save();
    }
}
