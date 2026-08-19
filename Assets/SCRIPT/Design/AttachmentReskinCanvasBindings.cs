using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Binds the reference-board Home treatment to controls that existing runtime
// systems inject directly under the main Canvas (not under mainMenuPanel).
// Presentation only: no Button or listener is created or replaced here.
[DefaultExecutionOrder(1400)]
public sealed class AttachmentReskinCanvasBindings : MonoBehaviour
{
    const string FriendResource = "reference/board_friend_exact";
    const string LightningResource = "reference/board_lightning_exact";

    static readonly Color Panel = Hex(0x14, 0x0A, 0x43);
    static readonly Color Purple = Hex(0x72, 0x27, 0xD8);
    static readonly Color Blue = Hex(0x06, 0x70, 0xD8);
    static readonly Color Cyan = Hex(0x00, 0xBA, 0xF5);
    static readonly Color Gold = Hex(0xFF, 0xC2, 0x00);
    static readonly Color GoldDark = Hex(0xA9, 0x62, 0x00);
    static readonly Color White = Hex(0xFA, 0xF7, 0xFF);
    static readonly Color Ink = Hex(0x22, 0x13, 0x09);

    Sprite friendIcon;
    Sprite lightningIcon;
    float nextPass;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        if (scene.name == "SplashScene") return;

        Canvas canvas = null;
        var menu = FindInScene<MenuManager>(scene);
        if (menu != null && menu.mainMenuPanel != null)
            canvas = menu.mainMenuPanel.GetComponentInParent<Canvas>();

        if (!OwnedCanvas(canvas, scene))
        {
            canvas = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var candidate in root.GetComponentsInChildren<Canvas>(true))
                {
                    if (!OwnedCanvas(candidate, scene)) continue;
                    canvas = candidate;
                    break;
                }
                if (canvas != null) break;
            }
        }

        if (canvas != null && canvas.GetComponent<AttachmentReskinCanvasBindings>() == null)
            canvas.gameObject.AddComponent<AttachmentReskinCanvasBindings>();
    }

    static bool OwnedCanvas(Canvas canvas, Scene scene)
    {
        return canvas != null && canvas.gameObject.scene == scene &&
               canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace;
    }

    void Awake()
    {
        friendIcon = Resources.Load<Sprite>(FriendResource);
        lightningIcon = Resources.Load<Sprite>(LightningResource);
    }

    void OnEnable()
    {
        L10n.OnLanguageChanged += ApplyBindings;
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= ApplyBindings;
    }

    IEnumerator Start()
    {
        // PvP and Daily Hunt inject their entry buttons one frame after Start.
        // Wait beyond those builders and then bind their existing controls.
        for (int i = 0; i < 10; i++)
            yield return null;
        ApplyBindings();
    }

    void LateUpdate()
    {
        if (Time.unscaledTime < nextPass) return;
        nextPass = Time.unscaledTime + 0.25f;
        ApplyBindings();
    }

    void ApplyBindings()
    {
        var menu = FindInScene<MenuManager>(gameObject.scene);
        if (menu != null && menu.mainMenuPanel != null && menu.mainMenuPanel.activeInHierarchy)
            ApplyHomeButtons();

        // ExtrasRuntimeWiring turns the existing solo result button into the
        // real RestartMatch action. Keep that product truth in the reskin.
        var game = FindInScene<GameManager>(gameObject.scene);
        if (game != null && game.IsMatchOver && game.stopGameButton != null)
        {
            var rematch = game.stopGameButton.GetComponent<Button>();
            var label = MainLabel(rematch);
            if (label != null)
            {
                label.text = L10n.Get("rematch").ToUpperInvariant();
                label.fontSize = 38f;
                label.fontStyle = FontStyles.Bold;
                label.color = White;
                label.alignment = TextAlignmentOptions.Center;
            }
        }

        // The test runner's fallback TMP font does not contain the star glyph.
        // The board already uses dedicated decorative art, so keep the TIP copy
        // textual rather than producing a missing-glyph square on devices that
        // use the same fallback.
        var tip = DeepFind(transform, "BoardHomeTipTitle");
        if (tip != null)
        {
            var text = tip.GetComponent<TMP_Text>();
            if (text != null)
                text.text = L10n.Get("hud_tip").ToUpperInvariant() + ":";
        }
    }

    void ApplyHomeButtons()
    {
        var play = FindButton("ButtonPlay");
        if (play != null)
        {
            Place((RectTransform)play.transform, new Vector2(0f, 115f), new Vector2(540f, 185f));
            StyleButton(play, Gold, Ink, GoldDark, 5f);
            SetButtonCopy(play,
                L10n.Get("play").ToUpperInvariant() + "!",
                L10n.Get("find_challenger").ToUpperInvariant(), 54f, 25f);
        }

        var friend = FindButton("ButtonPvP");
        if (friend != null)
        {
            Place((RectTransform)friend.transform, new Vector2(-245f, -130f), new Vector2(450f, 165f));
            StyleButton(friend, Blue, White, Cyan, 3f);
            SetButtonCopy(friend,
                IsGreek ? "ΠΑΙΞΕ\nΜΕ ΦΙΛΟ" : "PLAY WITH\nA FRIEND",
                "", 38f, 20f);
            AddVector(friend.transform, "BoardFriendVector", friendIcon,
                new Vector2(-155f, 0f), new Vector2(100f, 80f));
            SetActive(DeepFind(friend.transform, "BoardFriendIcon"), false);
        }

        var daily = FindButton("DailyHuntButton");
        if (daily != null)
        {
            Place((RectTransform)daily.transform, new Vector2(245f, -130f), new Vector2(450f, 165f));
            StyleButton(daily, Purple, White, Hex(0xA8, 0x58, 0xFF), 3f);
            SetButtonCopy(daily, L10n.Get("daily_hunt").ToUpperInvariant(), "", 36f, 20f);
            AddVector(daily.transform, "BoardDailyVector", lightningIcon,
                new Vector2(-155f, 0f), new Vector2(82f, 92f));
            SetActive(DeepFind(daily.transform, "BoardDailyIcon"), false);
        }

        var settings = FindButton("Buttonsettings");
        if (settings != null)
        {
            Place((RectTransform)settings.transform, new Vector2(-455f, 820f), new Vector2(82f, 82f));
            StyleButton(settings, Panel, White, Purple, 2f);
        }
    }

    Button FindButton(string name)
    {
        var found = DeepFind(transform, name);
        return found == null ? null : found.GetComponent<Button>();
    }

    static void AddVector(Transform parent, string name, Sprite sprite,
        Vector2 position, Vector2 size)
    {
        if (parent == null || sprite == null) return;
        var image = EnsureImage(parent, name);
        image.gameObject.SetActive(true);
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        Place(image.rectTransform, position, size);
        image.transform.SetAsLastSibling();
    }

    static void SetButtonCopy(Button button, string title, string subtitle,
        float titleSize, float subtitleSize)
    {
        if (button == null) return;
        var label = MainLabel(button);
        if (label != null)
        {
            label.text = title;
            label.fontSize = titleSize;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            Place(label.rectTransform,
                string.IsNullOrEmpty(subtitle) ? new Vector2(28f, 0f) : new Vector2(0f, 28f),
                new Vector2(((RectTransform)button.transform).sizeDelta.x - 80f,
                    string.IsNullOrEmpty(subtitle) ? 120f : 80f));
            Responsive(label, 20f);
        }

        var sub = EnsureText(button.transform, "BoardCanvasButtonSubtitle");
        sub.text = subtitle;
        sub.fontSize = subtitleSize;
        sub.fontStyle = FontStyles.Bold;
        sub.color = label == null ? White : label.color;
        sub.alignment = TextAlignmentOptions.Center;
        sub.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
        if (sub.gameObject.activeSelf)
        {
            Place(sub.rectTransform, new Vector2(0f, -48f),
                new Vector2(((RectTransform)button.transform).sizeDelta.x - 50f, 60f));
            Responsive(sub, 16f);
        }
    }

    static TMP_Text MainLabel(Button button)
    {
        if (button == null) return null;
        foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name.StartsWith("Board") || text.name.StartsWith("Exact")) continue;
            return text;
        }
        return button.GetComponentInChildren<TMP_Text>(true);
    }

    static void StyleButton(Button button, Color fill, Color labelColor, Color edge, float edgeSize)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = RuntimeUI.RoundedRectSprite;
            image.type = Image.Type.Sliced;
            image.color = fill;
            EnsureOutline(image.gameObject, edge, edgeSize);
            EnsureShadow(image.gameObject, new Color(0f, 0f, 0f, 0.72f), 9f);
        }

        var label = MainLabel(button);
        if (label != null)
        {
            label.color = labelColor;
            label.fontStyle = FontStyles.Bold;
            Responsive(label, 16f);
        }
    }

    static Image EnsureImage(Transform parent, string name)
    {
        var existing = DirectChild(parent, name);
        if (existing != null)
        {
            var image = existing.GetComponent<Image>();
            if (image != null) return image;
        }
        var go = RuntimeUI.CreateObject(name, parent);
        return go.AddComponent<Image>();
    }

    static TMP_Text EnsureText(Transform parent, string name)
    {
        var existing = DirectChild(parent, name);
        if (existing != null)
        {
            var found = existing.GetComponent<TMP_Text>();
            if (found != null) return found;
        }
        return RuntimeUI.CreateText(parent, name, "", 30, Vector2.zero,
            new Vector2(100f, 50f), White);
    }

    static Transform DirectChild(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name) return parent.GetChild(i);
        return null;
    }

    static Transform DeepFind(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = DeepFind(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    static T FindInScene<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }

    static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static void Responsive(TMP_Text text, float minimum)
    {
        if (text == null) return;
        float max = Mathf.Max(text.fontSize, minimum);
        text.enableAutoSizing = true;
        text.fontSizeMax = max;
        text.fontSizeMin = Mathf.Min(max, minimum);
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    static void EnsureOutline(GameObject go, Color color, float distance)
    {
        if (go == null) return;
        var outline = go.GetComponent<Outline>();
        if (outline == null) outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(distance, distance);
        outline.useGraphicAlpha = true;
    }

    static void EnsureShadow(GameObject go, Color color, float distance)
    {
        if (go == null) return;
        var shadow = go.GetComponent<Shadow>();
        if (shadow == null) shadow = go.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = new Vector2(0f, -distance);
        shadow.useGraphicAlpha = true;
    }

    static void SetActive(Transform target, bool active)
    {
        if (target != null && target.gameObject.activeSelf != active)
            target.gameObject.SetActive(active);
    }

    static bool IsGreek => L10n.Current == L10n.Language.Greek;

    static Color Hex(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }
}
