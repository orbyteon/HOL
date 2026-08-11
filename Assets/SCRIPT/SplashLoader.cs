using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashLoader : MonoBehaviour
{
    // Review #5: shortened from 5s. NOTE: if the scene's Inspector has its own
    // value for waitTime, the Inspector wins — update it there too.
    public float waitTime = 2.5f;

    void Start()
    {
        Invoke(nameof(LoadMenu), waitTime);
    }

    void LoadMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
