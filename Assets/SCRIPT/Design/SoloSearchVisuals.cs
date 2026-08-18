using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Presentation-only owner for the simulated solo search panel. The existing
// FakeMatchmaking component keeps all timing and callbacks; this layer animates
// only the radar and positions the existing searching/cancel controls.
public sealed class SoloSearchVisuals : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        var matchmaking = Object.FindObjectOfType<FakeMatchmaking>();
        Install(matchmaking);
    }

    public static void Install(FakeMatchmaking matchmaking)
    {
        if (matchmaking == null || matchmaking.searchingPanel == null) return;
        var panel = matchmaking.searchingPanel.transform;
        if (panel.GetComponent<SoloSearchVisuals>() == null)
            panel.gameObject.AddComponent<SoloSearchVisuals>();
    }

    void Awake()
    {
        Build();
    }

    void LateUpdate()
    {
        var cancel = Find<Button>(transform, "CancelButton");
        if (cancel == null) return;
        var rect = cancel.transform as RectTransform;
        if (rect == null) return;
        rect.anchoredPosition = new Vector2(0f, -680f);
        rect.sizeDelta = new Vector2(480f, 100f);
    }

    void Build()
    {
        if (transform.Find("SoloSearchVisualRoot") != null) return;

        var root = RuntimeUI.CreateObject("SoloSearchVisualRoot", transform);
        RuntimeUI.Stretch(root);
        root.transform.SetAsFirstSibling();
        var background = root.AddComponent<Image>();
        background.sprite = ConvergingLight.VerticalGradient(
            ConvergingLight.DepthTop, ConvergingLight.DepthBottom);
        background.raycastTarget = false;

        AddSprite(root.transform, "Logo", "reference/hol_logo_exact",
            new Vector2(0f, 700f), new Vector2(400f, 195f));
        var ribbon = NeonFrame.Frame(root.transform, "TitleRibbon",
            new Vector2(0f, 510f), new Vector2(860f, 120f),
            ConsumerTokens.Magenta, 0.90f, true, ConsumerTokens.CardPink);
        RuntimeUI.CreateText(ribbon.transform, "Title", "ΒΡΕΣ ΑΝΤΙΠΑΛΟ",
            44, Vector2.zero, new Vector2(800f, 95f));

        var card = NeonFrame.Frame(root.transform, "SearchCard",
            new Vector2(0f, 100f), new Vector2(920f, 650f),
            ConsumerTokens.Cyan, 0.94f, true, ConsumerTokens.Surface);
        AddSprite(card.transform, "Player", "reference/player_cyan_exact",
            new Vector2(-270f, 0f), new Vector2(390f, 430f));

        var radar = RuntimeUI.CreateObject("Radar", card.transform);
        ConvergingLight.Center(radar, new Vector2(90f, 30f),
            new Vector2(330f, 330f));
        for (int i = 0; i < 3; i++)
        {
            var ring = radar.AddComponent<Image>();
            ring.sprite = RuntimeUI.RoundedRectSprite;
            ring.type = Image.Type.Sliced;
            ring.color = new Color(0.05f, 0.75f, 1f,
                0.10f + i * 0.05f);
            ring.rectTransform.sizeDelta =
                new Vector2(300f - i * 70f, 300f - i * 70f);
        }
        var sweep = RuntimeUI.CreateObject("Sweep", radar.transform);
        ConvergingLight.Center(sweep, new Vector2(68f, 0f),
            new Vector2(140f, 18f));
        var sweepImage = sweep.AddComponent<Image>();
        sweepImage.color = new Color(0.20f, 0.92f, 1f, 0.82f);
        sweepImage.raycastTarget = false;
        sweep.AddComponent<RadarScanner>();

        var dot = RuntimeUI.CreateObject("CenterDot", radar.transform);
        ConvergingLight.Center(dot, Vector2.zero, new Vector2(34f, 34f));
        var dotImage = dot.AddComponent<Image>();
        dotImage.sprite = RuntimeUI.RoundedRectSprite;
        dotImage.type = Image.Type.Sliced;
        dotImage.color = ConsumerTokens.Cyan;
        dotImage.raycastTarget = false;
        var pulse = dot.AddComponent<RadarPulse>();
        pulse.target = dot.GetComponent<RectTransform>();

        var searchText = Find<TMP_Text>(transform, "Text");
        if (searchText == null)
            searchText = Find<TMP_Text>(transform, "SearchingText");
        if (searchText != null)
        {
            searchText.fontSize = 38f;
            searchText.enableAutoSizing = true;
            searchText.fontSizeMin = 26f;
            searchText.fontSizeMax = 38f;
            searchText.rectTransform.anchoredPosition =
                new Vector2(285f, 95f);
            searchText.rectTransform.sizeDelta =
                new Vector2(360f, 110f);
            searchText.alignment = TextAlignmentOptions.Center;
        }

        AddSprite(root.transform, "MascotSix", "reference/mascot_6_exact",
            new Vector2(-420f, -790f), new Vector2(180f, 210f));
        AddSprite(root.transform, "MascotSeven", "reference/mascot_7_exact",
            new Vector2(420f, -790f), new Vector2(180f, 210f));
    }

    static T Find<T>(Transform parent, string name) where T : Component
    {
        foreach (var item in parent.GetComponentsInChildren<T>(true))
            if (item.name == name) return item;
        return null;
    }

    static Image AddSprite(Transform parent, string name, string resource,
        Vector2 position, Vector2 size)
    {
        var sprite = Resources.Load<Sprite>(resource);
        if (sprite == null) return null;
        var go = RuntimeUI.CreateObject(name, parent);
        ConvergingLight.Center(go, position, size);
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }
}

public sealed class RadarScanner : MonoBehaviour
{
    public float degreesPerSecond = -110f;

    void Update()
    {
        transform.Rotate(0f, 0f, degreesPerSecond * Time.unscaledDeltaTime);
    }
}

public sealed class RadarPulse : MonoBehaviour
{
    public RectTransform target;
    Vector3 baseScale = Vector3.one;
    float age;

    void Awake()
    {
        if (target != null) baseScale = target.localScale;
    }

    void Update()
    {
        if (target == null) return;
        age += Time.unscaledDeltaTime;
        float pulse = 1f + Mathf.Sin(age * 4.5f) * 0.10f;
        target.localScale = baseScale * pulse;
    }
}
