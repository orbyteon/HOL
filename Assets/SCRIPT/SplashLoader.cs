using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashLoader : MonoBehaviour
{
    public float waitTime = 5f;

    void Start()
    {
        Invoke(nameof(LoadMenu), waitTime);
    }

    void LoadMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}