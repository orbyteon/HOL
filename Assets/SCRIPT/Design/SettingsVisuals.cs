using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Presentation-only Settings owner. It creates no interactive controls:
// existing scene/runtime controls retain their callbacks and are only moved
// into the approved portrait row layout.
[DefaultExecutionOrder(1400)]
public sealed class SettingsVisuals : MonoBehaviour
{
    const string RootName = "SettingsVisualRoot";
    static readonly Color Ink = new Color(0.08f, 0.04f, 0.20f, 1f);
    static readonly Color Muted = new Color(0.70f, 0.72f, 0.92f, 1f);

    RectTransform root;
    MenuManager menu;
    int frames;
    bool built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        foreach (var go in scene.GetRootGameObjects())
        {
            var canvas = go.GetComponentInChildren<Canvas>(true);
            if (canvas != null && canvas.isRootCanvas &&
                canvas.GetComponent<SettingsVisuals>() == null)
            {
                canvas.gameObject.AddComponent<SettingsVisuals>();
                return;
            }
        }
    }

    IEnumerator Start()
    {
        while (!built && frames++ < 24)
        {
            menu = FindObjectOfType<MenuManager>();
            if (menu != null && menu.settingsPanel != null &&
                menu.settingsPanel.GetComponentInChildren<Button>(true) != null)
            {
                Build();
                break;
            }
            yield return null;
        }
    }

    void LateUpdate()
    {
        if (root == null || menu == null || menu.settingsPanel == null) return;
        bool visible = menu.settingsPanel.activeSelf;
        if (root.gameObject.activeSelf != visible)
            root.gameObject.SetActive(visible);
    }

    void Build()
    {
        if (built || menu == null || menu.settingsPanel == null) return;
        built = true;

        root = RuntimeUI.CreateObject(RootName, menu.settingsPanel.transform)
            .GetComponent<RectTransform>();
        RuntimeUI.Stretch(root.gameObject);
        root.SetAsFirstSibling();

        var back = root.gameObject.AddComponent<Image>();
        back.sprite = ConvergingLight.VerticalGradient(
            ConvergingLight.DepthTop, ConvergingLight.DepthBottom);
        back.raycastTarget = false;

        AddSprite(root, "Logo", "reference/hol_logo_exact",
            new Vector2(0f, 690f), new Vector2(420f, 210f));
        AddLocalized(root, "Title", "settings_title", 44,
            new Vector2(0f, 455f), new Vector2(420f, 78f),
            ConvergingLight.NearWhite);

        var card = NeonFrame.Frame(root, "SettingsCard",
            new Vector2(0f, -65f), new Vector2(930f, 980f),
            ConsumerTokens.Magenta, 0.88f, true, ConsumerTokens.Surface);
        BuildRow(card.transform, "NameRow", "player_name", 335f);
        BuildRow(card.transform, "LanguageRow", "language", 105f);
        BuildRow(card.transform, "MusicRow", "music", -125f);
        BuildRow(card.transform, "DifficultyRow", "difficulty", -355f);
        BuildRow(card.transform, "AdsRow", "ads_privacy", -585f);

        AddSprite(root, "MascotSix", "reference/mascot_6_exact",
            new Vector2(-410f, -780f), new Vector2(190f, 220f));
        AddSprite(root, "MascotSeven", "reference/mascot_7_exact",
            new Vector2(410f, -780f), new Vector2(190f, 220f));

        RepositionExistingControls();
    }

    void BuildRow(Transform parent, string name, string key, float y)
    {
        var row = RuntimeUI.CreateObject(name, parent);
        ConvergingLight.Center(row, new Vector2(0f, y),
            new Vector2(860f, 170f));
        var line = row.AddComponent<Image>();
        line.sprite = RuntimeUI.RoundedRectSprite;
        line.type = Image.Type.Sliced;
        line.color = new Color(0.04f, 0.03f, 0.18f, 0.72f);
        line.raycastTarget = false;
        AddLocalized(row.transform, "Label", key, 28,
            new Vector2(-250f, 0f), new Vector2(310f, 60f),
            ConvergingLight.NearWhite);
    }

    void RepositionExistingControls()
    {
        var panel = menu.settingsPanel.transform;
        var input = Find<TMP_InputField>(panel, "InputField (TMP)");
        Place(input == null ? null : input.transform, new Vector2(120f, 335f),
            new Vector2(430f, 82f));
        Place(Find<Button>(panel, "Buttonsave")?.transform,
            new Vector2(365f, 335f), new Vector2(190f, 74f));
        Place(Find<Button>(panel, "EnglishButton")?.transform,
            new Vector2(30f, 105f), new Vector2(210f, 70f));
        Place(Find<Button>(panel, "GreekButton")?.transform,
            new Vector2(260f, 105f), new Vector2(210f, 70f));
        Place(Find<Toggle>(panel, "Toggle")?.transform,
            new Vector2(250f, -125f), new Vector2(150f, 70f));

        for (int i = 0; i < 4; i++)
            Place(Find<Button>(panel, "Difficulty" + i)?.transform,
                new Vector2(45f + i * 145f, -355f),
                new Vector2(130f, 65f));
        Place(Find<Button>(panel, "AdsPrivacyButton")?.transform,
            new Vector2(305f, -585f), new Vector2(200f, 72f));
    }

    static void Place(Transform target, Vector2 position, Vector2 size)
    {
        if (target == null) return;
        var rect = target as RectTransform;
        if (rect == null) return;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
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

    static TextMeshProUGUI AddLocalized(Transform parent, string name,
        string key, int fontSize, Vector2 position, Vector2 size, Color color)
    {
        var text = RuntimeUI.CreateText(parent, name, L10n.Get(key), fontSize,
            position, size, color);
        RuntimeUI.Localize(text, key);
        return text;
    }
}
