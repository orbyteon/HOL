using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Typography-only fidelity pass for the approved Home composition.
// MainMenuHomeVisuals remains the sole presentation owner and keeps every real
// callback. This component only corrects live TMP bounds after the owner has
// finished building so device rendering cannot place labels outside CTA art.
[DisallowMultipleComponent]
[DefaultExecutionOrder(2600)]
public sealed class MainMenuHomeTypographyFidelity : MonoBehaviour
{
    static readonly Color NearWhite =
        new Color(0.985f, 0.975f, 1f, 1f);
    static readonly Color Ink =
        new Color(0.09f, 0.05f, 0.16f, 1f);

    MainMenuHomeVisuals owner;
    Transform visualRoot;
    bool applied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name != "MainMenu")
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<MainMenuHomeTypographyFidelity>(true)
                != null)
                return;
        }

        var host = new GameObject("HomeTypographyFidelity");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<MainMenuHomeTypographyFidelity>();
    }

    IEnumerator Start()
    {
        for (int frame = 0; frame < 240; frame++)
        {
            owner = FindInScene<MainMenuHomeVisuals>(gameObject.scene);
            if (owner != null && owner.IsReady && owner.IsSettled)
                break;
            yield return null;
        }

        if (owner == null || !owner.IsReady || !owner.IsSettled)
        {
            Debug.LogError(
                "[MainMenuHomeTypographyFidelity] Home owner did not settle.");
            yield break;
        }

        visualRoot = DeepFind(owner.transform, MainMenuHomeVisuals.VisualRootName);
        Apply();
        L10n.OnLanguageChanged += RequestRefresh;
    }

    void OnDestroy()
    {
        L10n.OnLanguageChanged -= RequestRefresh;
    }

    void LateUpdate()
    {
        if (!applied || visualRoot == null) return;

        // The owner updates font minima when the language or viewport changes.
        // Reassert the approved internal bounds afterward without moving CTAs.
        Apply();
    }

    void RequestRefresh()
    {
        applied = false;
        StartCoroutine(RefreshNextFrame());
    }

    IEnumerator RefreshNextFrame()
    {
        yield return null;
        Apply();
    }

    void Apply()
    {
        if (visualRoot == null) return;

        ConfigureSpeech();
        ConfigureChip();
        ConfigureCta("HomeSoloTitle", "HomeSoloSubtitle", true);
        ConfigureCta("HomePvpTitle", "HomePvpSubtitle", false);
        ConfigureCta("HomeFriendTitle", "HomeFriendSubtitle", false);
        ConfigureCta("HomeDailyTitle", "HomeDailySubtitle", false);
        applied = true;
    }

    void ConfigureSpeech()
    {
        TMP_Text text = FindText("HomeSpeechText");
        if (text == null) return;

        text.color = NearWhite;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 24f;
        text.fontSizeMax = 30f;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;

        var rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(30f, 28f);
        rect.offsetMax = new Vector2(-30f, -28f);

        var shadow = text.GetComponent<Shadow>();
        if (shadow == null)
            shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.02f, 0.01f, 0.12f, 0.86f);
        shadow.effectDistance = new Vector2(2f, -3f);
        shadow.useGraphicAlpha = true;
    }

    void ConfigureChip()
    {
        TMP_Text text = FindText(MainMenuHomeVisuals.ChipTextName);
        if (text == null) return;

        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 24f;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        Place(text.rectTransform, new Vector2(42f, 0f),
            new Vector2(190f, 78f));
    }

    void ConfigureCta(string titleName, string subtitleName, bool primary)
    {
        TMP_Text title = FindText(titleName);
        TMP_Text subtitle = FindText(subtitleName);

        if (title != null)
        {
            title.alignment = TextAlignmentOptions.Center;
            title.enableAutoSizing = true;
            title.fontSizeMin = primary ? 36f : 32f;
            title.fontSizeMax = primary ? 46f : 42f;
            title.enableWordWrapping = false;
            title.overflowMode = TextOverflowModes.Overflow;
            Place(title.rectTransform, new Vector2(60f, 18f),
                new Vector2(690f, 50f));
        }

        if (subtitle != null)
        {
            subtitle.color = primary ? Ink : NearWhite;
            subtitle.alignment = TextAlignmentOptions.Center;
            subtitle.enableAutoSizing = true;
            subtitle.fontSizeMin = 18f;
            subtitle.fontSizeMax = primary ? 22f : 21f;
            subtitle.enableWordWrapping = false;
            subtitle.overflowMode = TextOverflowModes.Overflow;
            Place(subtitle.rectTransform, new Vector2(60f, -28f),
                new Vector2(690f, 34f));
        }
    }

    TMP_Text FindText(string name)
    {
        Transform found = DeepFind(visualRoot, name);
        return found == null ? null : found.GetComponent<TMP_Text>();
    }

    static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    static Transform DeepFind(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = DeepFind(root.GetChild(i), name);
            if (found != null) return found;
        }

        return null;
    }

    static T FindInScene<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid()) return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }

        return null;
    }
}
