using UnityEngine;
using UnityEngine.UI;

public class MusicSettings : MonoBehaviour
{
    public Toggle musicToggle;
    public AudioSource musicSource;

    void Start()
    {
        bool musicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;

        ApplyMusicState(musicOn);

        if (musicToggle != null)
            musicToggle.onValueChanged.AddListener(OnToggleChanged);
    }

    void OnToggleChanged(bool isOn)
    {
        ApplyMusicState(isOn);

        PlayerPrefs.SetInt("MusicOn", isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    void ApplyMusicState(bool isOn)
    {
        if (musicToggle != null)
            musicToggle.isOn = isOn;

        if (musicSource != null)
            musicSource.mute = !isOn;
    }
}
