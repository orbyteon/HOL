using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Lifecycle bridge only. PvpRuntimeUI creates PvpGameController during Start,
// so this installer waits for that controller and then attaches exactly one
// PvP duel/result presentation owner.
public sealed class PvpDuelCartoonVisualsInstaller : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name == "SplashScene")
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<PvpDuelCartoonVisualsInstaller>(true) != null)
                return;
        }

        var host = new GameObject("PvpDuelCartoonVisualsInstaller");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<PvpDuelCartoonVisualsInstaller>();
    }

    IEnumerator Start()
    {
        for (int frame = 0; frame < 300; frame++)
        {
            PvpGameController controller = FindController();
            if (controller != null)
            {
                if (controller.GetComponent<PvpDuelCartoonVisuals>() == null)
                    controller.gameObject.AddComponent<PvpDuelCartoonVisuals>();
                Destroy(gameObject);
                yield break;
            }
            yield return null;
        }

        Debug.LogError(
            "[PvpDuelCartoonVisualsInstaller] PvpGameController was not created within 300 frames.");
        Destroy(gameObject);
    }

    PvpGameController FindController()
    {
        Scene scene = gameObject.scene;
        if (!scene.IsValid() || !scene.isLoaded) return null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            PvpGameController controller =
                root.GetComponentInChildren<PvpGameController>(true);
            if (controller != null) return controller;
        }
        return null;
    }
}
