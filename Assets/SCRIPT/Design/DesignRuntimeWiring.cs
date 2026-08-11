using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Converging Light backdrop for the main menu, built one frame after Start
// (same pattern as Extras/JuiceRuntimeWiring). A semi-transparent indigo
// gradient laid over the scene's photo BACKROUND pulls the whole menu into
// the nocturnal palette, and a faint number field drifts above it. Both sit
// directly over BACKROUND in the canvas hierarchy — behind every button and
// panel — and never intercept raycasts.
public class DesignRuntimeWiring : MonoBehaviour
{
    const float OverlayAlpha = 0.62f;

    void Start()
    {
        StartCoroutine(BuildNextFrame());
    }

    IEnumerator BuildNextFrame()
    {
        yield return null; // let every other Start() finish first

        // The scene canvas is the one with a "BACKROUND" child — this
        // GameObject carries its own canvas for the PvP overlay.
        foreach (var canvas in FindObjectsOfType<Canvas>())
        {
            var bg = canvas.transform.Find("BACKROUND");
            if (bg == null) continue;

            int insert = bg.GetSiblingIndex() + 1;

            var overlay = RuntimeUI.CreateObject("BackdropDepth", canvas.transform);
            RuntimeUI.Stretch(overlay);
            var img = overlay.AddComponent<Image>();
            img.sprite = ConvergingLight.VerticalGradient(
                ConvergingLight.WithAlpha(ConvergingLight.DepthTop, OverlayAlpha),
                ConvergingLight.WithAlpha(ConvergingLight.DepthBottom, OverlayAlpha));
            img.color = Color.white;
            img.raycastTarget = false;
            overlay.transform.SetSiblingIndex(insert);

            var field = RuntimeUI.CreateObject("BackdropNumbers", canvas.transform);
            RuntimeUI.Stretch(field);
            ConvergingLight.NumberField(field.transform, 28, 0.05f);
            field.transform.SetSiblingIndex(insert + 1);

            break;
        }
    }
}
