using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Sole PanelPlay presentation owner on MainMenu. Restyles Back, Find
// Challenger, and the simulated-opponents disclosure in place.
[DefaultExecutionOrder(1700)]
public sealed class MainMenuPlayVisuals : MonoBehaviour
{
    public const string VisualRootName = "PlayVisualRoot";
    public const string SafeRootName = "PlaySafeAreaRoot";
    public const string BackgroundName = "PlayBackground";
    public const string LogoName = "PlayLogo";
    public const string DisclosureName = "PlayDisclosure";
    public const string FindIconName = "PlayFindIcon";

    const string BackgroundResource = "mainmenu/mainmenu_bg_stairs_clouds";
    const string DecoStarsResource = "mainmenu/mainmenu_deco_stars";
    const string LogoResource = "reference/hol_logo_exact";
    const string GoldCtaResource = "mainmenu/mainmenu_cta_gold_9s";
    const string BlueCtaResource = "mainmenu/mainmenu_cta_blue_9s";
    const string TipFrameResource = "mainmenu/mainmenu_tip_frame_9s";
    const string FindIconResource = "mainmenu/mainmenu_icon_solo";
    const string BulbIconResource = "mainmenu/mainmenu_icon_tip_bulb";

    static readonly Color Ink = new Color(0.09f, 0.06f, 0.22f, 1f);
    const float ReferenceWidth = 1080f;
    const float ReferenceHeight = 1920f;

    public static readonly string[] LoadedResources =
    {
        BackgroundResource, DecoStarsResource, LogoResource, GoldCtaResource,
        BlueCtaResource, TipFrameResource, FindIconResource, BulbIconResource
    };

    RectTransform visualRoot;
    bool laidOut;

    public bool IsReady { get; private set; }
    public bool IsSettled { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MainMenu" || !scene.IsValid() || !scene.isLoaded)
            return;

        Canvas canvas = null;
        var menu = FindInScene<MenuManager>(scene);
        if (menu != null && menu.mainMenuPanel != null)
            canvas = menu.mainMenuPanel.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var candidate in root.GetComponentsInChildren<Canvas>(true))
                {
                    if (!candidate.isRootCanvas ||
                        candidate.renderMode == RenderMode.WorldSpace)
                        continue;
                    canvas = candidate;
                    break;
                }
                if (canvas != null) break;
            }
        }

        if (canvas != null && canvas.GetComponent<MainMenuPlayVisuals>() == null)
            canvas.gameObject.AddComponent<MainMenuPlayVisuals>();
    }

    IEnumerator Start()
    {
        for (int i = 0; i < 14; i++)
            yield return null;
        BuildPlay();
        IsSettled = IsReady;
        laidOut = true;
    }

    void LateUpdate()
    {
        if (!laidOut || visualRoot == null) return;
        bool visible = IsIdlePlayVisible();
        if (visualRoot.gameObject.activeSelf != visible)
            visualRoot.gameObject.SetActive(visible);
    }

    bool IsIdlePlayVisible()
    {
        var menu = FindInScene<MenuManager>(gameObject.scene);
        if (menu == null || menu.panelPlay == null || !menu.panelPlay.activeSelf)
            return false;
        if (menu.panelSearching != null && menu.panelSearching.activeSelf)
            return false;
        var matchmaking = FindInScene<FakeMatchmaking>(gameObject.scene);
        if (matchmaking != null && matchmaking.panelGame != null &&
            matchmaking.panelGame.activeSelf)
            return false;
        return true;
    }

    void BuildPlay()
    {
        var canvas = GetComponent<Canvas>();
        var menu = FindInScene<MenuManager>(gameObject.scene);
        if (canvas == null || menu == null || menu.panelPlay == null)
        {
            Debug.LogError("[MainMenuPlayVisuals] Missing Canvas or PanelPlay.");
            return;
        }

        var background = LoadRequired(BackgroundResource);
        var logo = LoadRequired(LogoResource);
        var gold = LoadRequired(GoldCtaResource);
        var cyan = LoadRequired(BlueCtaResource);
        var tipFrame = LoadRequired(TipFrameResource);
        IsReady = background != null && logo != null && gold != null &&
                  cyan != null && tipFrame != null;
        if (!IsReady) return;

        var panel = menu.panelPlay.transform;
        var panelImage = menu.panelPlay.GetComponent<Image>();
        if (panelImage != null) panelImage.enabled = false;

        var exactLogo = DeepFind(panel, "ExactPlayLogo");
        if (exactLogo != null) exactLogo.gameObject.SetActive(false);

        visualRoot = EnsureRect(panel, VisualRootName);
        Stretch(visualRoot);
        visualRoot.SetAsFirstSibling();

        var bg = EnsureImage(visualRoot, BackgroundName);
        Stretch(bg.rectTransform);
        ConfigureImage(bg, background, false);

        var safe = EnsureRect(visualRoot, SafeRootName);
        ResponsiveSafeAreaRoot.Attach(safe, (RectTransform)canvas.transform,
            new Vector2(ReferenceWidth, ReferenceHeight));

        BuildDeco(safe, "PlayDecoStars", LoadOptional(DecoStarsResource));

        var logoImage = EnsureImage(safe, LogoName);
        ConfigureImage(logoImage, logo, true);
        Place(logoImage.rectTransform, new Vector2(0f, 520f), new Vector2(640f, 360f));

        RestyleCta(safe, "ButtonChallenger", gold, FindIconResource, FindIconName,
            "find_challenger", new Vector2(0f, 40f), new Vector2(860f, 150f), true);
        RestyleCta(safe, "ButtonBack", cyan, null, null,
            "back", new Vector2(0f, -140f), new Vector2(860f, 128f), false);
        RestyleDisclosure(safe, panel, tipFrame);
    }

    void RestyleCta(
        Transform safe, string buttonName, Sprite frame, string iconResource,
        string iconName, string l10nKey, Vector2 position, Vector2 size, bool goldLabel)
    {
        var button = FindButton(buttonName);
        if (button == null) return;
        Reparent(button.transform, safe);
        Place((RectTransform)button.transform, position, size);
        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = frame;
            image.color = Color.white;
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.raycastTarget = true;
        }

        if (!string.IsNullOrEmpty(iconResource) && !string.IsNullOrEmpty(iconName))
        {
            var icon = LoadOptional(iconResource);
            if (icon != null)
            {
                var iconImage = EnsureImage(button.transform, iconName);
                ConfigureImage(iconImage, icon, true);
                Place(iconImage.rectTransform, new Vector2(-320f, 0f), new Vector2(88f, 88f));
            }
        }

        var label = EnsureButtonLabel(button);
        label.fontSize = goldLabel ? 48f : 40f;
        label.fontStyle = FontStyles.Bold;
        label.color = Ink;
        label.alignment = TextAlignmentOptions.Center;
        RuntimeUI.ConfigureText(label, ResponsiveTextRole.Action,
            goldLabel ? 48f : 40f);
        Place(label.rectTransform, new Vector2(36f, 0f), new Vector2(size.x - 180f, size.y - 24f));
        SetLocalized(label, l10nKey);
    }

    void RestyleDisclosure(Transform safe, Transform panelPlay, Sprite frame)
    {
        var labelTransform = DirectChild(panelPlay, "DisclosureLabel");
        if (labelTransform == null)
            labelTransform = DeepFind(safe, "DisclosureLabel");

        var card = EnsureImage(safe, DisclosureName);
        card.sprite = frame;
        card.color = Color.white;
        card.type = Image.Type.Sliced;
        card.raycastTarget = false;
        Place(card.rectTransform, new Vector2(0f, -520f), new Vector2(920f, 200f));

        var bulb = LoadOptional(BulbIconResource);
        if (bulb != null)
        {
            var icon = EnsureImage(card.transform, "PlayDisclosureBulb");
            ConfigureImage(icon, bulb, true);
            Place(icon.rectTransform, new Vector2(-380f, 0f), new Vector2(72f, 72f));
        }

        TMP_Text body;
        if (labelTransform != null)
        {
            Reparent(labelTransform, card.transform);
            body = labelTransform.GetComponent<TMP_Text>();
            if (body == null) body = EnsureTmp(labelTransform, "DisclosureLabel", 26f);
        }
        else
        {
            body = EnsureTmp(card.transform, "DisclosureLabel", 26f);
        }

        body.color = ConvergingLight.NearWhite;
        body.alignment = TextAlignmentOptions.Left;
        body.raycastTarget = false;
        Place(body.rectTransform, new Vector2(40f, 0f), new Vector2(760f, 140f));
        SetLocalized(body, "simulated_opponents");
    }

    void BuildDeco(Transform safe, string name, Sprite sprite)
    {
        if (sprite == null) return;
        var image = EnsureImage(safe, name);
        ConfigureImage(image, sprite, false);
        Place(image.rectTransform, Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight));
    }

    Button FindButton(string name)
    {
        var found = DeepFind(transform, name);
        return found == null ? null : found.GetComponent<Button>();
    }

    static void Reparent(Transform child, Transform parent)
    {
        if (child.parent != parent)
            child.SetParent(parent, false);
        child.SetAsLastSibling();
    }

    static TMP_Text EnsureButtonLabel(Button button)
    {
        var existing = button.GetComponentInChildren<TMP_Text>(true);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            return existing;
        }
        return EnsureTmp(button.transform, "Label", 40f);
    }

    static TMP_Text EnsureTmp(Transform parent, string name, float size)
    {
        var rect = parent.name == name ? parent as RectTransform : EnsureRect(parent, name);
        var tmp = rect.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.raycastTarget = false;
        tmp.color = ConvergingLight.NearWhite;
        RuntimeUI.ConfigureText(tmp, ResponsiveTextRole.Body, size);
        return tmp;
    }

    static void SetLocalized(TMP_Text text, string key)
    {
        var loc = text.GetComponent<LocalizedText>();
        if (loc == null)
        {
            RuntimeUI.Localize(text, key);
            loc = text.GetComponent<LocalizedText>();
        }
        if (loc != null) loc.key = key;
        text.text = L10n.Get(key);
    }

    static Sprite LoadRequired(string path)
    {
        var sprite = Resources.Load<Sprite>(path);
        if (sprite == null)
            Debug.LogError("[MainMenuPlayVisuals] Missing Resources/" + path + ".");
        return sprite;
    }

    static Sprite LoadOptional(string path)
    {
        var sprite = Resources.Load<Sprite>(path);
        if (sprite == null)
            Debug.LogError("[MainMenuPlayVisuals] Missing optional Resources/" + path + ".");
        return sprite;
    }

    static RectTransform EnsureRect(Transform parent, string name)
    {
        var existing = DirectChild(parent, name) as RectTransform;
        if (existing != null) return existing;
        return (RectTransform)RuntimeUI.CreateObject(name, parent).transform;
    }

    static Image EnsureImage(Transform parent, string name)
    {
        var rect = EnsureRect(parent, name);
        var image = rect.GetComponent<Image>();
        if (image == null) image = rect.gameObject.AddComponent<Image>();
        return image;
    }

    static void ConfigureImage(Image image, Sprite sprite, bool preserveAspect)
    {
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
    }

    static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    static Transform DirectChild(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name)
                return parent.GetChild(i);
        return null;
    }

    static Transform DeepFind(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var found = DeepFind(parent.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    static T FindInScene<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid()) return null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }
}
