using UnityEngine;
using UnityEngine.UI;

// Durable fail-closed marker for a control whose required production artwork
// could not be loaded. State owners may still call SetActive(true), but this
// guard keeps the complete hierarchy invisible and non-interactive for the
// remainder of the object's lifetime. ExecuteAlways keeps the same lifecycle
// contract in editor previews and EditMode validation as in a player build.
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class RequiredProductionArtFailure : MonoBehaviour
{
    CanvasGroup blocker;

    public void Apply()
    {
        if (blocker == null)
            blocker = GetComponent<CanvasGroup>();
        if (blocker == null)
            blocker = gameObject.AddComponent<CanvasGroup>();

        blocker.alpha = 0f;
        blocker.interactable = false;
        blocker.blocksRaycasts = false;

        foreach (var selectable in GetComponentsInChildren<Selectable>(true))
            selectable.interactable = false;
        foreach (var graphic in GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
    }

    void Awake()
    {
        Apply();
    }

    void OnEnable()
    {
        Apply();
    }

    void OnTransformChildrenChanged()
    {
        Apply();
    }
}
