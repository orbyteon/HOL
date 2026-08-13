using UnityEngine;
using UnityEngine.UI;

// A soft light band that sweeps across a button every few seconds — the
// glint that marks the primary CTA. Builds its own masked band child; the
// button's rounded Image doubles as the stencil so nothing spills past the
// corners. Random phase per button so multiple CTAs never sweep in sync.
[RequireComponent(typeof(Image))]
public class ShineSweep : MonoBehaviour
{
    public float sweepSeconds = 0.9f;
    public float restSeconds = 3.6f;
    [Range(0f, 1f)] public float intensity = 0.32f;

    RectTransform band;
    float cycle;
    float clock;

    void Awake()
    {
        // The button's own image becomes the mask; keep drawing it.
        var mask = GetComponent<Mask>();
        if (mask == null) mask = gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        var go = RuntimeUI.CreateObject("Shine", transform);
        band = (RectTransform)go.transform;
        band.anchorMin = new Vector2(0.5f, 0.5f);
        band.anchorMax = new Vector2(0.5f, 0.5f);
        band.localRotation = Quaternion.Euler(0f, 0f, 18f);

        var img = go.AddComponent<Image>();
        img.sprite = ConvergingLightFX.ShineBand;
        img.color = new Color(1f, 1f, 1f, intensity);
        img.raycastTarget = false;

        // Under the label: children paint in order, so first = lowest.
        band.SetSiblingIndex(0);
    }

    void OnEnable()
    {
        cycle = sweepSeconds + restSeconds;
        clock = Random.value * cycle; // desync from other CTAs
        Place(-1f);
    }

    void Update()
    {
        if (band == null) return;

        clock += Time.unscaledDeltaTime;
        float inCycle = Mathf.Repeat(clock, cycle);
        if (inCycle >= sweepSeconds)
        {
            Place(-1f);
            return;
        }

        // Ease the band across: -1 (fully left) to +1 (fully right).
        float t = inCycle / sweepSeconds;
        Place(Mathf.Lerp(-1f, 1f, t * t * (3f - 2f * t)));
    }

    void Place(float t)
    {
        var size = ((RectTransform)transform).rect.size;
        band.sizeDelta = new Vector2(size.x * 0.34f, size.y * 2.4f);
        band.anchoredPosition = new Vector2(t * size.x * 0.72f, 0f);
    }
}
