using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Shared helpers for building simple UI from code at runtime. Used by
// PvpRuntimeUI and the zero-wire extras; keeps ConsentManager-style
// construction in one place.
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

    // Shared rounded-rect sprite (white, transparent corners) so every
    // runtime-built button/card/input has soft corners instead of hard
    // squares. Generated once, cached; sliced so corners stay round at
    // any size.
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
                // Distance outside the rounded corner circle, if any.
                float cx = Mathf.Min(x, size - 1 - x);
                float cy = Mathf.Min(y, size - 1 - y);
                float alpha = 1f;
                if (cx < radius && cy < radius)
                {
                    float dx = radius - cx - 0.5f;
                    float dy = radius - cy - 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    alpha = Mathf.Clamp01(radius + 0.5f - dist); // 1px anti-alias
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

    // Runtime page roots use a 1080x1920 portrait reference canvas. Clamp
    // direct children of a full-screen page to Android's safe area while leaving
    // nested labels/children in their local card coordinates.
    static Canvas FindPageCanvas(RectTransform rect)
    {
        if (rect == null || rect.parent == null) return null;
        Transform current = rect.parent;
        while (current != null)
        {
            var canvas = current.GetComponent<Canvas>();
            if (canvas != null) return canvas;

            var page = current as RectTransform;
            if (page == null ||
                page.anchorMin != Vector2.zero ||
                page.anchorMax != Vector2.one)
                return null;
            current = current.parent;
        }
        return null;
    }

    static void ClampPageChild(RectTransform rect, Vector2 size, Vector2 requested)
    {
        var canvas = FindPageCanvas(rect);
        if (canvas == null) return;
        var canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        Vector2 canvasSize = canvasRect != null && canvasRect.rect.size.sqrMagnitude > 0f
            ? canvasRect.rect.size
            : new Vector2(1080f, 1920f);

        Rect safe = Screen.safeArea;
        float width = Mathf.Max(1f, Screen.width);
        float height = Mathf.Max(1f, Screen.height);
        float left = safe.xMin / width * canvasSize.x - canvasSize.x * 0.5f;
        float right = safe.xMax / width * canvasSize.x - canvasSize.x * 0.5f;
        float bottom = safe.yMin / height * canvasSize.y - canvasSize.y * 0.5f;
        float top = safe.yMax / height * canvasSize.y - canvasSize.y * 0.5f;
        float halfW = size.x * 0.5f + 16f;
        float halfH = size.y * 0.5f + 16f;

        rect.anchoredPosition = new Vector2(
            Mathf.Clamp(requested.x, left + halfW, right - halfW),
            Mathf.Clamp(requested.y, bottom + halfH, top - halfH));

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

    public static GameObject FullscreenPanel(Transform parent, string name, Color color)
    {
        var panel = CreateObject(name, parent);
        Stretch(panel);
        var image = panel.AddComponent<Image>();
        var designBackground = DesignRuntimeWiring.BackgroundAsset;
        image.sprite = designBackground != null ? designBackground : null;
        image.type = designBackground != null ? Image.Type.Simple : Image.Type.Simple;
        image.color = designBackground != null ? Color.white : color;
        image.raycastTarget = true;
        return panel;
    }

    // Every runtime-built label is TextMesh Pro, same as the scene's TMP
    // labels and the input fields below — one text stack, one default font
    // asset. Wrapping and overflow mirror what the legacy version did: wrap
    // horizontally, overflow vertically rather than truncate.
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
        // Converging Light (design/philosophy.md): white exists only as
        // near-white, softened so it belongs to the night.
        text.color = color ?? new Color(0.91f, 0.93f, 1f);
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
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
        var primaryAsset = name.IndexOf("Play", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Start", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Confirm", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Save", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Submit", System.StringComparison.OrdinalIgnoreCase) >= 0;
        var designAsset = primaryAsset
            ? DesignRuntimeWiring.PrimaryButtonAsset
            : DesignRuntimeWiring.SecondaryButtonAsset;
        image.sprite = designAsset != null ? designAsset : RoundedRectSprite;
        image.type = designAsset != null ? Image.Type.Simple : Image.Type.Sliced;
        image.color = designAsset != null ? Color.white : color;

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;

        var text = CreateText(go.transform, "Label", label, 30, Vector2.zero, size, labelColor);
        Stretch(text.gameObject);

        // Juice at creation time: buttons built after JuiceRuntimeWiring's
        // one-shot startup pass would otherwise stay flat and silent.
        AttachJuice(button);

        return button;
    }

    // Numeric TMP input field built entirely from code. Pass contentType
    // Standard for fields that must accept letters (e.g. PvP room codes —
    // the backends normalize case, so plain Standard is enough).
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

        // Viewport (masked text area).
        var viewport = CreateObject("Text Area", go.transform);
        var viewportRect = (RectTransform)viewport.transform;
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(12f, 6f);
        viewportRect.offsetMax = new Vector2(-12f, -6f);
        viewport.AddComponent<RectMask2D>();
        input.textViewport = viewportRect;

        // Text.
        var textGo = CreateObject("Text", viewport.transform);
        Stretch(textGo);
        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.text = "";
        text.fontSize = 36;
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Center;
        input.textComponent = text;

        // Placeholder.
        var phGo = CreateObject("Placeholder", viewport.transform);
        Stretch(phGo);
        var ph = phGo.AddComponent<TextMeshProUGUI>();
        ph.text = placeholder;
        ph.fontSize = 36;
        ph.color = new Color(0f, 0f, 0f, 0.4f);
        ph.alignment = TextAlignmentOptions.Center;
        input.placeholder = ph;

        return input;
    }

    // ---------------------------------------------------------- live localization

    // Runtime-built labels bake one language at construction time. Attaching
    // a localizer with the L10n key makes them follow language changes live.
    // Dynamic labels (room codes, statuses, results) stay plain text.

    public static void Localize(TMP_Text text, string key)
    {
        if (text == null) return;
        var loc = text.gameObject.AddComponent<LocalizedText>();
        loc.key = key;
    }

    // Same, for a button's child label.
    public static void Localize(Button button, string key)
    {
        if (button == null) return;
        Localize(button.GetComponentInChildren<TMP_Text>(true), key);
    }

    // Same, for an input field's placeholder.
    public static void LocalizePlaceholder(TMP_InputField input, string key)
    {
        if (input == null) return;
        Localize(input.placeholder as TMP_Text, key);
    }

    // Unity forbids Object.Destroy from EditMode tests and editor scripts —
    // that path must use DestroyImmediate. Play mode keeps the deferred
    // Destroy so in-flight listeners cannot see a hole mid-frame.
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
