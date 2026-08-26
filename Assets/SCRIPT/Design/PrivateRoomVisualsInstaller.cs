using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Lifecycle bridge only. PvpRuntimeUI creates PvpGameController in Start(),
// after SceneManager.sceneLoaded has already fired. This installer waits for
// that real controller and then attaches the sole PrivateRoom presentation
// owner. It owns no visuals and destroys itself immediately afterwards.
public sealed class PrivateRoomVisualsInstaller : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // PvpRuntimeUI and its PvpGameController live only in MainMenu. Installing
        // this bridge in test/utility scenes creates a waiter that can never
        // succeed and later emits an unrelated 300-frame timeout.
        if (!scene.IsValid() || !scene.isLoaded || scene.name != "MainMenu")
            return;

        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<PrivateRoomVisualsInstaller>(true) != null)
                return;

        var host = new GameObject("PrivateRoomVisualsInstaller");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<PrivateRoomVisualsInstaller>();
    }

    IEnumerator Start()
    {
        for (int frame = 0; frame < 300; frame++)
        {
            var controller = FindController();
            if (controller != null)
            {
                if (controller.GetComponent<PrivateRoomVisuals>() == null)
                    controller.gameObject.AddComponent<PrivateRoomVisuals>();
                Destroy(gameObject);
                yield break;
            }
            yield return null;
        }

        Debug.LogError("[PrivateRoomVisualsInstaller] PvpGameController was not created within 300 frames.");
        Destroy(gameObject);
    }

    PvpGameController FindController()
    {
        var scene = gameObject.scene;
        if (!scene.IsValid() || !scene.isLoaded) return null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var controller = root.GetComponentInChildren<PvpGameController>(true);
            if (controller != null) return controller;
        }
        return null;
    }
}
