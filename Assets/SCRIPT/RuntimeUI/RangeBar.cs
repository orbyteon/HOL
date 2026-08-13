using UnityEngine;
using UnityEngine.UI;
using TMPro;

// The converging interval, made visible: a 1–100 track whose lit window
// narrows with every answered guess, min/max in gold at the window's edges.
// This is the game's core mental model — Converging Light's "field of
// possibility being gently compressed from both sides" — surfaced as UI.
// Build once via Attach(); with a GameManager it follows
// OnPlayerRangeChanged, and any owner can drive it manually via SetRange
// (the Daily Hunt does).
public class RangeBar : MonoBehaviour
{
    const float TrackWidth = 700f;
    const float TrackHeight = 6f;
    const float AnimateSpeed = 9f;

    GameManager gameManager;
    RectTransform window;
    TMP_Text minLabel;
    TMP_Text maxLabel;

    float shownMin = 1f;
    float shownMax = 100f;
    int targetMin = 1;
    int targetMax = 100;

    bool subscribed;

    public static RangeBar Attach(Transform parent, GameManager gm, Vector2 position)
    {
        var root = RuntimeUI.CreateObject("RangeBar", parent);
        ConvergingLight.Center(root, position, new Vector2(TrackWidth, 60f));

        // AddComponent can fire OnEnable before the manager is assigned
        // (whenever the parent panel happens to be active), so subscription
        // is idempotent and retried both here and in OnEnable — otherwise
        // the bar silently froze at 1–100 for the whole match.
        var bar = root.AddComponent<RangeBar>();
        bar.gameManager = gm;
        bar.Build();
        bar.Subscribe();
        return bar;
    }

    void Build()
    {
        var track = RuntimeUI.CreateObject("Track", transform);
        ConvergingLight.Center(track, Vector2.zero, new Vector2(TrackWidth, TrackHeight));
        var trackImg = track.AddComponent<Image>();
        trackImg.sprite = RuntimeUI.RoundedRectSprite;
        trackImg.type = Image.Type.Sliced;
        trackImg.color = ConvergingLight.WithAlpha(ConvergingLight.TrackIndigo, 0.9f);
        trackImg.raycastTarget = false;

        var win = RuntimeUI.CreateObject("Window", track.transform);
        window = (RectTransform)win.transform;
        window.anchorMin = new Vector2(0.5f, 0.5f);
        window.anchorMax = new Vector2(0.5f, 0.5f);
        var winImg = win.AddComponent<Image>();
        winImg.sprite = ConvergingLightFX.Seam;
        winImg.color = ConvergingLight.WithAlpha(Color.white, 0.85f);
        winImg.raycastTarget = false;

        minLabel = RuntimeUI.CreateTmpText(transform, "Min", "1", 30,
            Vector2.zero, new Vector2(120f, 40f), ConvergingLight.Gold);
        maxLabel = RuntimeUI.CreateTmpText(transform, "Max", "100", 30,
            Vector2.zero, new Vector2(120f, 40f), ConvergingLight.Gold);

        Apply(true);
    }

    void OnEnable()
    {
        Subscribe();
    }

    void Subscribe()
    {
        if (subscribed || gameManager == null || !isActiveAndEnabled) return;

        gameManager.OnPlayerRangeChanged += OnRangeChanged;
        subscribed = true;
        // Panels can activate after StartGame already fired the event.
        targetMin = gameManager.PlayerRangeMin;
        targetMax = gameManager.PlayerRangeMax;
        Apply(true);
    }

    void OnDisable()
    {
        if (subscribed && gameManager != null)
            gameManager.OnPlayerRangeChanged -= OnRangeChanged;
        subscribed = false;
    }

    void OnRangeChanged(int min, int max)
    {
        targetMin = min;
        targetMax = max;
    }

    // Manual drive for owners without a GameManager. `instant` snaps the
    // window (fresh challenge) instead of animating from the previous state.
    public void SetRange(int min, int max, bool instant = false)
    {
        targetMin = min;
        targetMax = max;
        if (instant) Apply(true);
    }

    void Update()
    {
        if (Mathf.Abs(shownMin - targetMin) < 0.01f &&
            Mathf.Abs(shownMax - targetMax) < 0.01f)
            return;
        Apply(false);
    }

    void Apply(bool instant)
    {
        if (instant)
        {
            shownMin = targetMin;
            shownMax = targetMax;
        }
        else
        {
            float k = Time.unscaledDeltaTime * AnimateSpeed;
            shownMin = Mathf.Lerp(shownMin, targetMin, k);
            shownMax = Mathf.Lerp(shownMax, targetMax, k);
        }

        float left = ToX(shownMin);
        float right = ToX(shownMax);
        float width = Mathf.Max(right - left, 8f);

        window.sizeDelta = new Vector2(width, TrackHeight);
        window.anchoredPosition = new Vector2((left + right) * 0.5f, 0f);

        int minShown = Mathf.RoundToInt(shownMin);
        int maxShown = Mathf.RoundToInt(shownMax);

        // Near convergence the two 120px labels overlap into a smear at
        // exactly the dramatic beat the bar exists to sell — collapse them
        // into one centered "49–50" (or "50") once they'd collide.
        if (right - left < 130f)
        {
            minLabel.text = minShown == maxShown
                ? minShown.ToString()
                : minShown + "-" + maxShown;
            maxLabel.text = "";
            minLabel.rectTransform.anchoredPosition = new Vector2((left + right) * 0.5f, 34f);
        }
        else
        {
            minLabel.text = minShown.ToString();
            maxLabel.text = maxShown.ToString();
            minLabel.rectTransform.anchoredPosition = new Vector2(left, 34f);
            maxLabel.rectTransform.anchoredPosition = new Vector2(right, 34f);
        }
    }

    // Maps a 1..100 value onto the track's local x (centered).
    static float ToX(float value)
    {
        return (value - 50.5f) / 99f * TrackWidth;
    }
}
