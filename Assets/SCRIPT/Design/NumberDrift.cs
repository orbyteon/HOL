using UnityEngine;

// Slow drift for ConvergingLight number-field digits. Wraps around the
// 1080x1920 reference space (with a small margin) so digits never collect
// at an edge. Unscaled time: keeps moving behind panel transitions.
[RequireComponent(typeof(RectTransform))]
public class NumberDrift : MonoBehaviour
{
    public Vector2 velocity = new Vector2(0f, 8f);

    const float HalfW = 560f;
    const float HalfH = 980f;

    RectTransform rect;

    void Awake()
    {
        rect = (RectTransform)transform;
    }

    void Update()
    {
        Vector2 p = rect.anchoredPosition + velocity * Time.unscaledDeltaTime;
        if (p.y > HalfH) p.y = -HalfH;
        if (p.x > HalfW) p.x = -HalfW;
        else if (p.x < -HalfW) p.x = HalfW;
        rect.anchoredPosition = p;
    }
}
