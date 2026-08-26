using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Shared, theme-agnostic helpers for runtime UI construction.
//
// IMPORTANT: this class owns infrastructure only (object creation, anchoring,
// localization, input plumbing and neutral emergency fallbacks). It must never
// choose the product theme or silently replace approved production artwork.
// Screen-specific presentation owners are responsible for assigning approved
// sprites and typography after construction.
public static class RuntimeUI
{
    // Scene click sound, discovered once per scene by JuiceRuntimeWiring.
    // Buttons built AFTER its one-shot wiring pass (streak-save offer,
    // reopened consent dialog) pick their sound up from here at creation,
    // so every button clicks — not just the ones that existed at startup.
    // The null checks below treat destroyed (scene-reloaded) sources as
    // absent, so a stale cache can never be handed out.
    public static AudioSource SharedClickSource;
    public static AudioClip SharedClickClip;

    // Attaches press-squash feedback (plus the shared click sound when
    // known). Safe to call on any button; keeps an existing ButtonJuice.
    public static ButtonJuice AttachJuice(Button button)
    {
        if (button == null) return null;

        var juice = button.GetComponent<ButtonJuice>();
        if (juice == null)
            juice = button.gameObject.AddComponent<ButtonJuice>();

        if (juice.clickSound == null && SharedClickSource != null && SharedClickClip != null)
        {
            juice.audioSource = SharedClickSource;
            juice.clickSound = SharedClickClip;
        }
        return juice;
    }

    // Neutral construction fallback only. Approved production screens must
    // replace it with real artwork before becoming visible. ApplyProductionSprite
    // removes and disables this fallback when required art is missing, so it can
    // never silently become the shipping look.
    static Sprite roundedSprite;

    public static Sprite RoundedRectSprite
    {
        get
        {
            if (roundedSprite == null)
                roundedSprite = GenerateRoundedRect(64, 14);
            return roundedSprite;
        }
    }

    static Sprite GenerateRoundedRect(int size, int radius)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Min(x, size - 1 - x);
                float cy = Mathf.Min(y, size - 1 - y);
                float alpha = 1f;
                if (cx < radius && cy < radius)
                {
                    float dx = radius - cx - 0.5f;
                    float dy = radius - cy - 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    alpha = Mathf.Clamp01(radius + 0.5f - dist);
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        int border = radius + 1;
        return Sprite.Create(tex, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
            new Vector4(border, border, border, border));
    }

    // Runtime page roots use a 1080x1920 portrait reference canvas. Register
    // direct page content with the one responsive owner for its nearest
    // full-screen root. Nested labels and card children remain in local card
    // coordinates, so safe-area compensation is never applied twice.
    static void ClampPageChild(RectTransform rect, Vector2 size, Vector2 requested)
    {
        ResponsivePageLayout.Register(rect, size, requested);

        if (size.x < 48f || size.y < 48f)
            Debug.LogWarning("HOL UI: page touch target below 48px: " + rect.name);
    }

    // Public hook for presentation-only layout passes that reposition an
    // already-built direct child after the initial construction clamp.
    public static void ClampToSafeArea(RectTransform rect, Vector2 size,
        Vector2 requested)
    {
        ClampPageChild(rect, size, requested);
    }

    public static GameObject CreateObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    public static void Stretch(GameObject go)
    {
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static void Center(GameObject go, Vector2 position, Vector2 size)
    {
        if (go == null) return;
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    static Sprite solidSprite;
    public static Sprite SolidSprite
    {
        get
        {
            if (solidSprite != null) return solidSprite;
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            solidSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f));
            return solidSprite;
        }
    }

    public static Sprite LoadProductionSprite(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath)) return null;
        var sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite == null)
            Debug.LogError("HOL UI: missing approved production sprite Resources/" +
                resourcePath + ".");
        return sprite;
    }

    public static GameObject FullscreenPanel(Transform parent, string name, Color color)
    {
        var panel = CreateObject(name, parent);
        Stretch(panel);
        var image = panel.AddComponent<Image>();
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = color;
        image.raycastTarget = true;
        return panel;
    }

    // Production presentation owners should use this helper when a runtime
    // control has an approved Resources sprite. It deliberately fails closed:
    // no procedural substitute is painted over missing production art.
    public static bool ApplyProductionSprite(Image image, string resourcePath,
        Image.Type type = Image.Type.Simple, bool preserveAspect = false,
        float pixelsPerUnitMultiplier = 1f)
    {
        if (image == null) return false;

        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            FailClosedProductionImage(image);
            Debug.LogError("HOL UI: approved production sprite path is empty.");
            return false;
        }

        var sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite == null)
        {
            // A runtime-created button/input may already carry RoundedRectSprite.
            // Remove it before reporting the failure so generic infrastructure
            // cannot remain visible as an accidental production replacement.
            FailClosedProductionImage(image);
            Debug.LogError("HOL UI: missing approved production sprite Resources/" +
                resourcePath + ".");
            return false;
        }

        image.enabled = true;
        image.sprite = sprite;
        image.type = type;
        image.preserveAspect = preserveAspect;
        image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
        image.color = Color.white;
        return true;
    }

    static void FailClosedProductionImage(Image image)
    {
        image.sprite = null;
        image.enabled = false;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.pixelsPerUnitMultiplier = 1f;
        image.color = Color.white;
        image.raycastTarget = false;

        // A controller may later call SetActive(true) on this same button/input.
        // Persist the failure independently of active state so the full child
        // hierarchy remains invisible and non-interactive after reactivation.
        var guard = image.GetComponent<RequiredProductionArtFailure>();
        if (guard == null)
            guard = image.gameObject.AddComponent<RequiredProductionArtFailure>();
        guard.Apply();
        image.gameObject.SetActive(false);
    }

    // Neutral infrastructure for a caller-owned production frame. The
    // caller supplies the exact approved sprite path; RuntimeUI never chooses
    // a visual language or substitutes generated artwork.
    public static GameObject CreateProductionFrame(
        Transform parent, string name, Vector2 position, Vector2 size,
        string resourcePath, float pixelsPerUnitMultiplier = 2f)
    {
        var frame = CreateObject(name, parent);
        Center(frame, position, size);
        ClampToSafeArea((RectTransform)frame.transform, size, position);
        var image = frame.AddComponent<Image>();
        if (!ApplyProductionSprite(image, resourcePath, Image.Type.Sliced, false,
                pixelsPerUnitMultiplier))
            frame.SetActive(false);
        image.raycastTarget = false;
        return frame;
    }

    // Every runtime-built label is TextMesh Pro, same as the scene's TMP
    // labels and the input fields below. The shared responsive policy keeps
    // English and Greek inside their authored regions with bounded autosizing.
    public static TextMeshProUGUI CreateText(Transform parent, string name, string content,
        int fontSize, Vector2 position, Vector2 size, Color? color = null)
    {
        var go = CreateObject(name, parent);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        ClampPageChild(rect, size, position);

        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color ?? new Color(0.96f, 0.97f, 1f);
        text.alignment = TextAlignmentOptions.Center;
        ResponsiveTextPolicy.Configure(text, ResponsiveTextRole.Body, fontSize);
        return text;
    }

    public static Button CreateButton(Transform parent, string name, string label,
        Vector2 position, Vector2 size, Color color, Color? labelColor = null)
    {
        var go = CreateObject(name, parent);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        ClampPageChild(rect, size, position);

        var image = go.AddComponent<Image>();
        image.sprite = RoundedRectSprite;
        image.type = Image.Type.Sliced;
        image.color = color;

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;

        var text = CreateText(go.transform, "Label", label, 30, Vector2.zero, size, labelColor);
        Stretch(text.gameObject);
        ResponsiveTextPolicy.Configure(text, ResponsiveTextRole.Action, 30f);

        AttachJuice(button);
        return button;
    }

    // Numeric TMP input field built entirely from code. Pass contentType
    // Standard for fields that must accept letters (e.g. PvP room codes).
    public static TMP_InputField CreateInputField(Transform parent, string name,
        string placeholder, Vector2 position, Vector2 size, int characterLimit = 3,
        TMP_InputField.ContentType contentType = TMP_InputField.ContentType.IntegerNumber)
    {
        var go = CreateObject(name, parent);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        ClampPageChild(rect, size, position);

        var image = go.AddComponent<Image>();
        image.sprite = RoundedRectSprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(1f, 1f, 1f, 0.9f);

        var input = go.AddComponent<TMP_InputField>();
        input.contentType = contentType;
        input.characterLimit = characterLimit;

        var viewport = CreateObject("Text Area", go.transform);
        var viewportRect = (RectTransform)viewport.transform;
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(12f, 6f);
        viewportRect.offsetMax = new Vector2(-12f, -6f);
        viewport.AddComponent<RectMask2D>();
        input.textViewport = viewportRect;

        var textGo = CreateObject("Text", viewport.transform);
        Stretch(textGo);
        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.text = "";
        text.fontSize = 36;
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Center;
        ResponsiveTextPolicy.Configure(text, ResponsiveTextRole.Input, 36f);
        input.textComponent = text;

        var phGo = CreateObject("Placeholder", viewport.transform);
        Stretch(phGo);
        var ph = phGo.AddComponent<TextMeshProUGUI>();
        ph.text = placeholder;
        ph.fontSize = 36;
        ph.color = new Color(0f, 0f, 0f, 0.4f);
        ph.alignment = TextAlignmentOptions.Center;
        ResponsiveTextPolicy.Configure(ph, ResponsiveTextRole.Input, 36f);
        input.placeholder = ph;

        return input;
    }

    // ---------------------------------------------------------- live localization

    public static void Localize(TMP_Text text, string key)
    {
        if (text == null) return;
        var loc = text.gameObject.AddComponent<LocalizedText>();
        loc.key = key;
    }

    public static void Localize(Button button, string key)
    {
        if (button == null) return;
        Localize(button.GetComponentInChildren<TMP_Text>(true), key);
    }

    public static void LocalizePlaceholder(TMP_InputField input, string key)
    {
        if (input == null) return;
        Localize(input.placeholder as TMP_Text, key);
    }

    public static void ConfigureText(TMP_Text text, ResponsiveTextRole role,
        float configuredMaximum = 0f)
    {
        ResponsiveTextPolicy.Configure(text, role, configuredMaximum);
    }

    public static void DestroyNow(Object target)
    {
        if (target == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Object.DestroyImmediate(target);
            return;
        }
#endif
        Object.Destroy(target);
    }
}
