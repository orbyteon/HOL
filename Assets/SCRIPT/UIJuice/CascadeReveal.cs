using System.Collections.Generic;
using UnityEngine;

// Staggered content entrance: when the panel activates, its direct children
// fade + rise in sequence (~0.04s apart), so a screen assembles instead of
// popping. Composes with PanelAnimator (parent and child CanvasGroup alphas
// multiply). Children are re-collected on every enable, so controls built
// later at runtime join the cascade the next time the panel opens.
public class CascadeReveal : MonoBehaviour
{
    public float perChildDelay = 0.045f;
    public float childDuration = 0.24f;
    public float riseDistance = 16f;

    class Entry
    {
        public RectTransform rect;
        public CanvasGroup group;
        public Vector2 basePos;
        public float delay;
    }

    readonly List<Entry> entries = new List<Entry>();
    readonly Dictionary<RectTransform, Vector2> basePositions =
        new Dictionary<RectTransform, Vector2>();
    float t;
    bool playing;

    void OnEnable()
    {
        entries.Clear();
        int index = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (!child.gameObject.activeSelf) continue;
            var rect = child as RectTransform;
            if (rect == null) continue;

            var group = child.GetComponent<CanvasGroup>();
            if (group == null) group = child.gameObject.AddComponent<CanvasGroup>();

            Vector2 basePos;
            if (!basePositions.TryGetValue(rect, out basePos))
            {
                basePos = rect.anchoredPosition;
                basePositions[rect] = basePos;
            }

            entries.Add(new Entry
            {
                rect = rect,
                group = group,
                basePos = basePos,
                delay = index * perChildDelay,
            });
            index++;
        }

        t = 0f;
        playing = entries.Count > 0;
        Apply();
    }

    void Update()
    {
        if (!playing) return;

        t += Time.unscaledDeltaTime;
        Apply();

        var last = entries[entries.Count - 1];
        if (t >= last.delay + childDuration)
        {
            Settle();
            playing = false;
        }
    }

    void Apply()
    {
        foreach (var e in entries)
        {
            if (e.rect == null) continue;
            float p = Mathf.Clamp01((t - e.delay) / childDuration);
            float ease = 1f - Mathf.Pow(1f - p, 3f);
            e.group.alpha = ease;
            e.rect.anchoredPosition = e.basePos + Vector2.down * riseDistance * (1f - ease);
        }
    }

    void Settle()
    {
        foreach (var e in entries)
        {
            if (e.rect == null) continue;
            e.group.alpha = 1f;
            e.rect.anchoredPosition = e.basePos;
        }
    }

    void OnDisable()
    {
        // Rest state for the next open — and for anything that reads
        // positions while the panel is closed.
        Settle();
        playing = false;
    }
}
