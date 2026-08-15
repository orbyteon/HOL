using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Converging Light backdrop for the main menu, built one frame after Start
// (same pattern as Extras/JuiceRuntimeWiring):
//   1. A semi-transparent indigo gradient over the scene's photo BACKROUND
//      pulls the whole menu into the nocturnal palette, plus a faint
//      drifting number field above it. Both sit directly over BACKROUND in
//      the canvas hierarchy — behind every button and panel — and never
//      intercept raycasts.
//   2. A palette pass retints the scene-authored panels and labels, which
//      were a mixed bag (pure white panels, a bright red settings panel,
//      pure black/white/red/orange text). Button art sprites and input
//      fields are left untouched, so dark labels on light button art stay
//      readable; everything directly on a panel is mapped onto the canon
//      palette (near-white text, cyan accents, gold primary).
public class DesignRuntimeWiring : MonoBehaviour
{
    const float OverlayAlpha = 0.62f;
    static readonly Color BackdropTint = new Color(0.62f, 0.60f, 0.85f);

    // Assigned from the Unity scene so the approved design assets are used
    // by the live UI, rather than existing only as an unused asset library.
    [SerializeField] Sprite backgroundSprite;
    [SerializeField] Sprite panelSprite;
    [SerializeField] Sprite primaryButtonSprite;
    [SerializeField] Sprite secondaryButtonSprite;

    void Start()
    {
        StartCoroutine(BuildNextFrame());
    }

    IEnumerator BuildNextFrame()
    {
        yield return null; // let every other Start() finish first

        // The scene canvas is the one with a "BACKROUND" child — this
        // GameObject carries its own canvas for the PvP overlay.
        foreach (var canvas in FindObjectsOfType<Canvas>())
        {
            var bg = canvas.transform.Find("BACKROUND");
            if (bg == null) continue;

            int insert = bg.GetSiblingIndex() + 1;

            var bgImg = bg.GetComponent<Image>();
            if (bgImg != null) bgImg.color = BackdropTint; // soften the magenta cast

            var overlay = RuntimeUI.CreateObject("BackdropDepth", canvas.transform);
            RuntimeUI.Stretch(overlay);
            var img = overlay.AddComponent<Image>();
            img.sprite = ConvergingLight.VerticalGradient(
                ConvergingLight.WithAlpha(ConvergingLight.DepthTop, OverlayAlpha),
                ConvergingLight.WithAlpha(ConvergingLight.DepthBottom, OverlayAlpha));
            img.color = Color.white;
            img.raycastTarget = false;
            overlay.transform.SetSiblingIndex(insert);

            var field = RuntimeUI.CreateObject("BackdropNumbers", canvas.transform);
            RuntimeUI.Stretch(field);
            ConvergingLight.NumberField(field.transform, 28, 0.05f);
            field.transform.SetSiblingIndex(insert + 1);

            RestylePalette(canvas.transform);
            ApplyDesignAssets(canvas.transform);
            break;
        }
    }

    // --- palette pass -------------------------------------------------------

    static void RestylePalette(Transform canvasRoot)
    {
        // Panels: indigo depth, never pure white or pure black.
        SetImageColor(canvasRoot, "PanelPlay", ConvergingLight.PanelIndigo);
        SetImageColor(canvasRoot, "PanelGAME", ConvergingLight.PanelIndigo);
        SetImageColor(canvasRoot, "PanelSettings", ConvergingLight.PanelIndigo);

        // Matchmaking scrim keeps its alpha, gains the indigo cast.
        var searching = DeepFind(canvasRoot, "PanelSearching");
        var scrim = searching != null ? searching.GetComponent<Image>() : null;
        if (scrim != null)
            scrim.color = ConvergingLight.WithAlpha(ConvergingLight.ScrimIndigo, scrim.color.a);

        foreach (var tmp in canvasRoot.GetComponentsInChildren<TMP_Text>(true))
        {
            if (Skippable(tmp.transform)) continue;
            tmp.color = MapColor(tmp.color);
        }
        foreach (var txt in canvasRoot.GetComponentsInChildren<Text>(true))
        {
            if (Skippable(txt.transform)) continue;
            txt.color = MapColor(txt.color);
        }
    }

    void ApplyDesignAssets(Transform canvasRoot)
    {
        // The background is a full-bleed 1080x1920 sprite, so it is safe to
        // stretch to the CanvasScaler reference frame.
        var background = DeepFind(canvasRoot, "BACKROUND");
        AssignSprite(background, backgroundSprite, Image.Type.Simple);

        foreach (var panelName in new[] { "PanelPlay", "PanelGAME", "PanelSettings" })
        {
            var panel = DeepFind(canvasRoot, panelName);
            AssignSprite(panel, panelSprite, Image.Type.Simple);
        }

        foreach (var button in canvasRoot.GetComponentsInChildren<Button>(true))
        {
            var image = button.GetComponent<Image>();
            if (image == null) continue;

            bool primary = IsPrimary(button.transform.name);
            var sprite = primary ? primaryButtonSprite : secondaryButtonSprite;
            if (sprite == null) continue;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
        }
    }

    static bool IsPrimary(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        return name.IndexOf("Play", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Confirm", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Save", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Challenger", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("START", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static void AssignSprite(Transform target, Sprite sprite, Image.Type type)
    {
        if (target == null || sprite == null) return;
        var image = target.GetComponent<Image>();
        if (image == null) return;
        image.sprite = sprite;
        image.type = type;
        image.color = Color.white;
        image.preserveAspect = false;
    }

    static void SetImageColor(Transform root, string name, Color color)
    {
        var t = DeepFind(root, name);
        var img = t != null ? t.GetComponent<Image>() : null;
        if (img != null) img.color = color;
    }

    // Transform.Find only sees direct children; the panels sit at varying
    // depths in the hand-authored hierarchy.
    static Transform DeepFind(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var hit = DeepFind(root.GetChild(i), name);
            if (hit != null) return hit;
        }
        return null;
    }

    // Texts on light button art or inside white input fields keep their dark
    // color — flipping them would make them unreadable.
    static bool Skippable(Transform t)
    {
        return t.GetComponentInParent<Button>() != null
            || t.GetComponentInParent<TMP_InputField>() != null
            || t.GetComponentInParent<InputField>() != null;
    }

    // Maps the scene's ad-hoc label colors onto the Converging Light canon.
    // Alpha is always preserved.
    static Color MapColor(Color c)
    {
        bool isWhite = c.r > 0.9f && c.g > 0.9f && c.b > 0.9f;
        bool isDark = c.r < 0.5f && c.g < 0.5f && c.b < 0.5f;
        bool isRed = c.r > 0.7f && c.g < 0.45f && c.b < 0.5f;
        bool isOrange = c.r > 0.7f && c.g >= 0.3f && c.g < 0.75f && c.b < 0.45f;
        bool isYellow = c.r > 0.7f && c.g > 0.75f && c.b < 0.3f;

        if (isWhite || isDark) return ConvergingLight.WithAlpha(ConvergingLight.NearWhite, c.a);
        if (isRed || isYellow) return ConvergingLight.WithAlpha(ConvergingLight.Cyan, c.a);
        if (isOrange) return ConvergingLight.WithAlpha(ConvergingLight.Gold, c.a);
        return c; // already on-palette or code-driven; leave it
    }
}
