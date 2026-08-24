using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Keeps the approved 1080x1920 full-screen art layers proportional on every
// portrait viewport. The component only applies Unity's built-in aspect envelope
// to decorative Images; PrivateRoomVisuals remains the sole presentation owner.
[DefaultExecutionOrder(2650)]
public sealed class PrivateRoomPortraitArtEnvelope : MonoBehaviour
{
    public const float ReferenceAspect = 1080f / 1920f;

    static readonly string[] OverlayNames =
    {
        "PrivateRoomBackground",
        "PrivateRoomStars",
        "PrivateRoomConfetti",
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name != "MainMenu")
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<PrivateRoomPortraitArtEnvelope>(true) != null)
                return;
        }

        var host = new GameObject(nameof(PrivateRoomPortraitArtEnvelope));
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<PrivateRoomPortraitArtEnvelope>();
    }

    IEnumerator Start()
    {
        for (int frame = 0; frame < 300; frame++)
        {
            var visuals = FindObjectOfType<PrivateRoomVisuals>(true);
            Transform root = visuals == null
                ? null
                : Find(visuals.transform, PrivateRoomVisuals.VisualRootName);

            if (root != null && Apply(root))
            {
                Destroy(gameObject);
                yield break;
            }

            yield return null;
        }

        Debug.LogError(
            "[PrivateRoomPortraitArtEnvelope] Private Room overlays were not ready within 300 frames.");
        Destroy(gameObject);
    }

    static bool Apply(Transform root)
    {
        bool complete = true;
        foreach (string overlayName in OverlayNames)
        {
            var rect = Find(root, overlayName) as RectTransform;
            if (rect == null)
            {
                complete = false;
                continue;
            }

            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            var fitter = rect.GetComponent<AspectRatioFitter>();
            if (fitter == null)
                fitter = rect.gameObject.AddComponent<AspectRatioFitter>();

            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = ReferenceAspect;
            fitter.SetLayoutHorizontal();
            fitter.SetLayoutVertical();
        }

        return complete;
    }

    static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = Find(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }
}
