using UnityEngine;
using UnityEngine.UI;

// Called synchronously by the sole PrivateRoomVisuals owner for each current
// screen background. No scene-wide installer or waiter for retired overlays.
public static class PrivateRoomPortraitArtEnvelope
{
    public const float ReferenceAspect = 1080f / 1920f;

    public static void Attach(Image background)
    {
        if (background == null || background.sprite == null)
            throw new System.ArgumentException("A real production background is required.", nameof(background));
        var rect = background.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        var fitter = background.GetComponent<AspectRatioFitter>();
        if (fitter == null) fitter = background.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = ReferenceAspect;
    }
}
