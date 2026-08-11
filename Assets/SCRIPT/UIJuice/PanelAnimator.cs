using UnityEngine;

// Drop-in panel entrance: fade + rise + gentle scale whenever the panel activates.
// Add to any UI panel GameObject. Adds its own CanvasGroup if missing.
[RequireComponent(typeof(RectTransform))]
public class PanelAnimator : MonoBehaviour
{
    public float duration = 0.28f;
    public float riseDistance = 36f;
    [Range(0.8f, 1f)] public float startScale = 0.96f;

    CanvasGroup group;
    RectTransform rect;
    Vector2 basePos;
    float t;
    bool captured;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        if (!captured)
        {
            basePos = rect.anchoredPosition;
            captured = true;
        }

        t = 0f;
        Apply(0f);
    }

    void Update()
    {
        if (t >= 1f) return;

        t = Mathf.Min(1f, t + Time.unscaledDeltaTime / duration);
        // ease-out cubic
        float e = 1f - Mathf.Pow(1f - t, 3f);
        Apply(e);
    }

    void Apply(float e)
    {
        group.alpha = e;
        rect.anchoredPosition = basePos + Vector2.down * riseDistance * (1f - e);
        float s = Mathf.Lerp(startScale, 1f, e);
        rect.localScale = new Vector3(s, s, 1f);
    }

    void OnDisable()
    {
        // leave the panel in its rest state for the next show
        if (captured) rect.anchoredPosition = basePos;
        rect.localScale = Vector3.one;
        if (group != null) group.alpha = 1f;
    }
}
