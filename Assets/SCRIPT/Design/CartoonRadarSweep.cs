using UnityEngine;

// Presentation-only rotation for the modular radar sweep sprite. The sweep is
// an approved Image asset; this component animates its RectTransform and never
// draws a procedural Graphic.
[DisallowMultipleComponent]
public sealed class CartoonRadarSweep : MonoBehaviour
{
    [Min(1f)] public float degreesPerSecond = 72f;

    RectTransform rect;
    float angle;

    void Awake()
    {
        rect = transform as RectTransform;
    }

    void OnEnable()
    {
        angle = 0f;
        Apply();
    }

    void Update()
    {
        if (rect == null)
            rect = transform as RectTransform;
        if (rect == null) return;

        angle = Mathf.Repeat(
            angle - degreesPerSecond * Time.unscaledDeltaTime,
            360f);
        Apply();
    }

    void Apply()
    {
        if (rect != null)
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
