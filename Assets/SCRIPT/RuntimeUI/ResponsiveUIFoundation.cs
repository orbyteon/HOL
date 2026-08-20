using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared portrait reference geometry used by every runtime page. The
/// calculation is pure so EditMode tests and capture tooling can exercise the
/// same contract without changing Screen or recreating a page.
/// </summary>
public struct ResponsiveViewportGeometry
{
    public static readonly Vector2 DefaultReferenceSize = new Vector2(1080f, 1920f);

    public readonly Rect NormalizedSafeArea;
    public readonly Rect SafeRect;
    public readonly float Scale;

    ResponsiveViewportGeometry(Rect normalizedSafeArea, Rect safeRect, float scale)
    {
        NormalizedSafeArea = normalizedSafeArea;
        SafeRect = safeRect;
        Scale = scale;
    }

    public static ResponsiveViewportGeometry Calculate(
        Rect safeAreaPixels, Vector2 viewportPixels, Vector2 canvasSize,
        Vector2 referenceSize)
    {
        float width = Mathf.Max(1f, viewportPixels.x);
        float height = Mathf.Max(1f, viewportPixels.y);
        float canvasWidth = Mathf.Max(1f, canvasSize.x);
        float canvasHeight = Mathf.Max(1f, canvasSize.y);
        float referenceWidth = Mathf.Max(1f, referenceSize.x);
        float referenceHeight = Mathf.Max(1f, referenceSize.y);

        float xMin = Mathf.Clamp(safeAreaPixels.xMin, 0f, width);
        float xMax = Mathf.Clamp(safeAreaPixels.xMax, xMin, width);
        float yMin = Mathf.Clamp(safeAreaPixels.yMin, 0f, height);
        float yMax = Mathf.Clamp(safeAreaPixels.yMax, yMin, height);

        var normalized = new Rect(
            xMin / width,
            yMin / height,
            (xMax - xMin) / width,
            (yMax - yMin) / height);
        var safeRect = new Rect(
            normalized.xMin * canvasWidth - canvasWidth * 0.5f,
            normalized.yMin * canvasHeight - canvasHeight * 0.5f,
            normalized.width * canvasWidth,
            normalized.height * canvasHeight);
        float scale = Mathf.Min(1f, Mathf.Min(
            safeRect.width / referenceWidth,
            safeRect.height / referenceHeight));

        return new ResponsiveViewportGeometry(
            normalized, safeRect, Mathf.Max(0.01f, scale));
    }

    public static Rect CalculateNormalizedSafeArea(Rect safeAreaPixels, float width, float height)
    {
        if (width <= 0f || height <= 0f)
            return new Rect(0f, 0f, 1f, 1f);
        return Calculate(safeAreaPixels, new Vector2(width, height),
            new Vector2(width, height), new Vector2(width, height)).NormalizedSafeArea;
    }

    // Mirrors CanvasScaler.ScaleWithScreenSize in MatchWidthOrHeight mode.
    // This is validation support for deterministic viewport matrices; live
    // pages use their actual RectTransform dimensions.
    public static Vector2 CanvasSizeForViewport(
        Vector2 viewportPixels, Vector2 referenceSize, float matchWidthOrHeight = 0.5f)
    {
        float widthScale = Mathf.Max(0.0001f, viewportPixels.x / referenceSize.x);
        float heightScale = Mathf.Max(0.0001f, viewportPixels.y / referenceSize.y);
        float logWidth = Mathf.Log(widthScale, 2f);
        float logHeight = Mathf.Log(heightScale, 2f);
        float scale = Mathf.Pow(2f, Mathf.Lerp(
            logWidth, logHeight, Mathf.Clamp01(matchWidthOrHeight)));
        return viewportPixels / Mathf.Max(0.0001f, scale);
    }

    public Vector2 Place(Vector2 requestedPosition, Vector2 size, float padding = 16f)
    {
        Vector2 scaledSize = size * Scale;
        float inset = padding * Scale;
        float minX = SafeRect.xMin + scaledSize.x * 0.5f + inset;
        float maxX = SafeRect.xMax - scaledSize.x * 0.5f - inset;
        float minY = SafeRect.yMin + scaledSize.y * 0.5f + inset;
        float maxY = SafeRect.yMax - scaledSize.y * 0.5f - inset;
        Vector2 desired = SafeRect.center + requestedPosition * Scale;

        return new Vector2(
            minX <= maxX ? Mathf.Clamp(desired.x, minX, maxX) : SafeRect.center.x,
            minY <= maxY ? Mathf.Clamp(desired.y, minY, maxY) : SafeRect.center.y);
    }

    public Rect Bounds(Vector2 requestedPosition, Vector2 size, float padding = 16f)
    {
        Vector2 center = Place(requestedPosition, size, padding);
        Vector2 scaled = size * Scale;
        return new Rect(center - scaled * 0.5f, scaled);
    }
}

public enum ResponsiveTextRole
{
    Body,
    Heading,
    Action,
    Input,
    Compact
}

/// <summary>
/// One bounded TMP policy for English and Greek. Existing configured font
/// sizes remain the maximum; autosizing may only shrink to the explicit floor,
/// wrapping is controlled by role, and vertical overflow becomes ellipsis.
/// </summary>
public static class ResponsiveTextPolicy
{
    public static void Configure(TMP_Text text, ResponsiveTextRole role,
        float configuredMaximum = 0f)
    {
        if (text == null) return;

        bool preserveExistingMinimum = configuredMaximum <= 0f &&
            text.enableAutoSizing && text.fontSizeMin > 0f;
        float maximum = configuredMaximum > 0f
            ? configuredMaximum
            : text.enableAutoSizing && text.fontSizeMax > 0f
                ? text.fontSizeMax
                : Mathf.Max(1f, text.fontSize);
        float factor;
        float absoluteFloor;
        switch (role)
        {
            case ResponsiveTextRole.Action:
                factor = 0.68f;
                absoluteFloor = 18f;
                break;
            case ResponsiveTextRole.Input:
                factor = 0.75f;
                absoluteFloor = 20f;
                break;
            case ResponsiveTextRole.Heading:
                factor = 0.62f;
                absoluteFloor = 20f;
                break;
            case ResponsiveTextRole.Compact:
                factor = 0.65f;
                absoluteFloor = 13f;
                break;
            default:
                factor = 0.62f;
                absoluteFloor = 16f;
                break;
        }

        text.enableAutoSizing = true;
        text.fontSizeMax = maximum;
        text.fontSizeMin = preserveExistingMinimum
            ? Mathf.Min(maximum, text.fontSizeMin)
            : Mathf.Min(maximum, Mathf.Max(absoluteFloor, maximum * factor));
        text.enableWordWrapping = role != ResponsiveTextRole.Input;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    public static void ApplyHierarchy(Transform root)
    {
        if (root == null) return;
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            Configure(text, RoleFor(text));
    }

    static ResponsiveTextRole RoleFor(TMP_Text text)
    {
        if (text.GetComponentInParent<TMP_InputField>() != null)
            return ResponsiveTextRole.Input;
        if (text.GetComponentInParent<Button>() != null)
            return ResponsiveTextRole.Action;

        string name = text.name ?? "";
        if (name.IndexOf("title", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("heading", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("prompt", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("result", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return ResponsiveTextRole.Heading;
        if (text.fontSize <= 24f)
            return ResponsiveTextRole.Compact;
        return ResponsiveTextRole.Body;
    }
}

/// <summary>
/// Page-level owner for reference-positioned direct content. It records
/// immutable requested geometry, recomputes only when viewport/safe geometry
/// changes, and always derives output from the original values so recalculation
/// cannot drift. A target is owned by only its nearest full-screen root.
/// </summary>
[DisallowMultipleComponent]
public sealed class ResponsivePageLayout : MonoBehaviour
{
    sealed class Entry
    {
        public RectTransform Target;
        public Vector2 RequestedPosition;
        public Vector2 Size;
        public Vector3 BaseScale;
    }

    readonly List<Entry> entries = new List<Entry>();
    RectTransform page;
    Canvas canvas;
    bool subscribed;
    bool dirty = true;
    int lastWidth = -1;
    int lastHeight = -1;
    Rect lastSafeArea;
    Vector2 lastCanvasSize;
    ResponsiveViewportGeometry currentGeometry;
    bool hasGeometry;

    public int RecalculationCount { get; private set; }
    public Rect LastSafeRect { get; private set; }
    public int RegisteredCount => entries.Count;

    public static bool Register(RectTransform target, Vector2 size,
        Vector2 requestedPosition)
    {
        Canvas ownerCanvas;
        RectTransform owner = FindOwner(target, out ownerCanvas);
        if (owner == null || ownerCanvas == null) return false;

        var layout = owner.GetComponent<ResponsivePageLayout>();
        if (layout == null)
            layout = owner.gameObject.AddComponent<ResponsivePageLayout>();
        layout.Configure(owner, ownerCanvas);
        layout.RegisterTarget(target, size, requestedPosition);
        return true;
    }

    public void ApplyViewport(Rect viewportPixels, Rect safeAreaPixels,
        Vector2 canvasSize)
    {
        EnsureReferences();
        var geometry = ResponsiveViewportGeometry.Calculate(
            safeAreaPixels, viewportPixels.size, canvasSize,
            ResponsiveViewportGeometry.DefaultReferenceSize);
        currentGeometry = geometry;
        hasGeometry = true;
        LastSafeRect = geometry.SafeRect;

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            Entry entry = entries[i];
            if (entry.Target == null || !IsStillOwned(entry.Target))
            {
                entries.RemoveAt(i);
                continue;
            }

            ApplyEntry(entry, geometry);
        }

        ResponsiveTextPolicy.ApplyHierarchy(transform);
        RecalculationCount++;
        dirty = false;
    }

    public void RefreshNow()
    {
        EnsureReferences();
        if (page == null || canvas == null) return;
        Vector2 size = page.rect.size;
        if (size.x <= 0f || size.y <= 0f)
        {
            var canvasRect = canvas.transform as RectTransform;
            size = canvasRect != null ? canvasRect.rect.size :
                ResponsiveViewportGeometry.DefaultReferenceSize;
        }
        ApplyViewport(new Rect(0f, 0f,
            Mathf.Max(1f, Screen.width), Mathf.Max(1f, Screen.height)),
            Screen.safeArea, size);
        CacheViewport(size);
    }

    void Configure(RectTransform owner, Canvas ownerCanvas)
    {
        page = owner;
        canvas = ownerCanvas;
    }

    void RegisterTarget(RectTransform target, Vector2 size,
        Vector2 requestedPosition)
    {
        Entry entry = entries.Find(item => item.Target == target);
        if (entry == null)
        {
            entry = new Entry
            {
                Target = target,
                BaseScale = target.localScale
            };
            entries.Add(entry);
        }
        entry.Size = size;
        entry.RequestedPosition = requestedPosition;
        if (!hasGeometry || dirty)
            RefreshNow();
        else
            ApplyEntry(entry, currentGeometry);
    }

    void Awake()
    {
        EnsureReferences();
    }

    void OnEnable()
    {
        if (!subscribed)
        {
            L10n.OnLanguageChanged += OnLanguageChanged;
            subscribed = true;
        }
        dirty = true;
        RefreshNow();
    }

    void OnDisable()
    {
        if (!subscribed) return;
        L10n.OnLanguageChanged -= OnLanguageChanged;
        subscribed = false;
    }

    void OnDestroy()
    {
        OnDisable();
    }

    void OnRectTransformDimensionsChange()
    {
        dirty = true;
    }

    void LateUpdate()
    {
        EnsureReferences();
        if (page == null || canvas == null) return;
        Vector2 size = page.rect.size;
        if (dirty || Screen.width != lastWidth || Screen.height != lastHeight ||
            Screen.safeArea != lastSafeArea || size != lastCanvasSize)
            RefreshNow();
    }

    void OnLanguageChanged()
    {
        ResponsiveTextPolicy.ApplyHierarchy(transform);
        dirty = true;
        RefreshNow();
    }

    void CacheViewport(Vector2 size)
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        lastSafeArea = Screen.safeArea;
        lastCanvasSize = size;
    }

    void EnsureReferences()
    {
        if (page == null) page = transform as RectTransform;
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
    }

    static void ApplyEntry(Entry entry, ResponsiveViewportGeometry geometry)
    {
        entry.Target.sizeDelta = entry.Size;
        entry.Target.anchoredPosition = geometry.Place(
            entry.RequestedPosition, entry.Size);
        entry.Target.localScale = new Vector3(
            entry.BaseScale.x * geometry.Scale,
            entry.BaseScale.y * geometry.Scale,
            entry.BaseScale.z);
    }

    bool IsStillOwned(RectTransform target)
    {
        Canvas currentCanvas;
        return FindOwner(target, out currentCanvas) == page && currentCanvas == canvas;
    }

    static RectTransform FindOwner(RectTransform target, out Canvas ownerCanvas)
    {
        ownerCanvas = null;
        if (target == null || target.parent == null) return null;

        RectTransform closestFullScreen = null;
        Transform current = target.parent;
        while (current != null)
        {
            var foundCanvas = current.GetComponent<Canvas>();
            if (foundCanvas != null)
            {
                if (!foundCanvas.isRootCanvas ||
                    foundCanvas.renderMode == RenderMode.WorldSpace)
                    return null;
                ownerCanvas = foundCanvas;
                return closestFullScreen != null
                    ? closestFullScreen
                    : current as RectTransform;
            }

            var rect = current as RectTransform;
            if (!IsFullScreen(rect)) return null;
            if (closestFullScreen == null) closestFullScreen = rect;
            current = current.parent;
        }
        return null;
    }

    static bool IsFullScreen(RectTransform rect)
    {
        return rect != null && rect.anchorMin == Vector2.zero &&
               rect.anchorMax == Vector2.one &&
               rect.offsetMin == Vector2.zero &&
               rect.offsetMax == Vector2.zero;
    }
}

/// <summary>
/// Safe-root owner for authored 1080x1920 compositions. It is attached only
/// to the one safe root beneath a full-screen visual root; nested ownership is
/// rejected to prevent double inset compensation.
/// </summary>
[DisallowMultipleComponent]
public sealed class ResponsiveSafeAreaRoot : MonoBehaviour
{
    RectTransform safeRoot;
    RectTransform canvasRect;
    Vector2 referenceSize = ResponsiveViewportGeometry.DefaultReferenceSize;
    bool subscribed;
    bool dirty = true;
    int lastWidth = -1;
    int lastHeight = -1;
    Rect lastSafeArea;
    Vector2 lastCanvasSize;

    public int RecalculationCount { get; private set; }
    public Rect LastSafeRect { get; private set; }

    public static ResponsiveSafeAreaRoot Attach(RectTransform root,
        RectTransform canvas, Vector2 reference)
    {
        if (root == null || canvas == null || HasSafeAreaAncestor(root))
            return null;
        var owner = root.GetComponent<ResponsiveSafeAreaRoot>();
        if (owner == null)
            owner = root.gameObject.AddComponent<ResponsiveSafeAreaRoot>();
        owner.safeRoot = root;
        owner.canvasRect = canvas;
        owner.referenceSize = reference;
        owner.dirty = true;
        owner.RefreshNow();
        return owner;
    }

    public static bool HasSafeAreaAncestor(RectTransform root)
    {
        if (root == null) return false;
        Transform current = root.parent;
        while (current != null)
        {
            if (current.GetComponent<ResponsiveSafeAreaRoot>() != null)
                return true;
            current = current.parent;
        }
        return false;
    }

    public void ApplyViewport(Rect viewportPixels, Rect safeAreaPixels,
        Vector2 canvasSize)
    {
        EnsureReferences();
        if (safeRoot == null) return;
        var geometry = ResponsiveViewportGeometry.Calculate(
            safeAreaPixels, viewportPixels.size, canvasSize, referenceSize);
        safeRoot.anchorMin = geometry.NormalizedSafeArea.min;
        safeRoot.anchorMax = geometry.NormalizedSafeArea.max;
        safeRoot.offsetMin = Vector2.zero;
        safeRoot.offsetMax = Vector2.zero;
        safeRoot.pivot = new Vector2(0.5f, 0.5f);
        safeRoot.localScale = new Vector3(geometry.Scale, geometry.Scale, 1f);
        LastSafeRect = geometry.SafeRect;
        ResponsiveTextPolicy.ApplyHierarchy(transform);
        RecalculationCount++;
        dirty = false;
    }

    public void RefreshNow()
    {
        EnsureReferences();
        if (safeRoot == null || canvasRect == null) return;
        Vector2 canvasSize = canvasRect.rect.size;
        if (canvasSize.x <= 0f || canvasSize.y <= 0f)
            canvasSize = referenceSize;
        ApplyViewport(new Rect(0f, 0f,
            Mathf.Max(1f, Screen.width), Mathf.Max(1f, Screen.height)),
            Screen.safeArea, canvasSize);
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        lastSafeArea = Screen.safeArea;
        lastCanvasSize = canvasSize;
    }

    void Awake()
    {
        EnsureReferences();
    }

    void OnEnable()
    {
        if (!subscribed)
        {
            L10n.OnLanguageChanged += OnLanguageChanged;
            subscribed = true;
        }
        dirty = true;
        RefreshNow();
    }

    void OnDisable()
    {
        if (!subscribed) return;
        L10n.OnLanguageChanged -= OnLanguageChanged;
        subscribed = false;
    }

    void OnDestroy()
    {
        OnDisable();
    }

    void OnRectTransformDimensionsChange()
    {
        dirty = true;
    }

    void LateUpdate()
    {
        EnsureReferences();
        if (safeRoot == null || canvasRect == null) return;
        Vector2 size = canvasRect.rect.size;
        if (dirty || Screen.width != lastWidth || Screen.height != lastHeight ||
            Screen.safeArea != lastSafeArea || size != lastCanvasSize)
            RefreshNow();
    }

    void OnLanguageChanged()
    {
        ResponsiveTextPolicy.ApplyHierarchy(transform);
        dirty = true;
        RefreshNow();
    }

    void EnsureReferences()
    {
        if (safeRoot == null) safeRoot = transform as RectTransform;
        if (canvasRect == null)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) canvasRect = canvas.transform as RectTransform;
        }
    }
}
