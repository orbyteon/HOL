using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Converging Light look for the main menu, built one frame after Start
// (same pattern as Extras/JuiceRuntimeWiring):
//   1. Backdrop stack over the scene's photo BACKROUND: deep indigo field,
//      breathing aurora light pools, two parallax planes of drifting digits,
//      and a corner vignette. All behind every button and panel, never
//      intercepting raycasts; the photo survives only as faint texture.
//   2. A palette pass maps the scene-authored label colors (mixed white/
//      black/red/orange) onto the canon: near-white text, cyan accents,
//      gold primary. Texts on buttons and inside input fields are handled
//      by the component pass instead.
//   3. A component pass pulls every scene-authored control into the design
//      system: panels become floating cards (rounded, shadowed, seam-lit),
//      buttons take their role color (gold CTA / cyan action / ghost),
//      icon art is softened into the palette, input fields become rounded
//      near-white wells, and menu panels gain a staggered content reveal.
public class DesignRuntimeWiring : MonoBehaviour
{
    const float OverlayAlpha = 0.92f;
    static readonly Color BackdropTint = new Color(0.34f, 0.34f, 0.52f);

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
            if (bgImg != null) bgImg.color = BackdropTint; // photo survives as faint texture

            // The owned look, back to front: deep indigo field, breathing
            // aurora, two parallax digit planes, vignette. All raycast-off,
            // all behind every panel and button.
            var overlay = RuntimeUI.CreateObject("BackdropDepth", canvas.transform);
            RuntimeUI.Stretch(overlay);
            var img = overlay.AddComponent<Image>();
            img.sprite = ConvergingLightFX.DeepField;
            img.color = ConvergingLight.WithAlpha(Color.white, OverlayAlpha);
            img.raycastTarget = false;
            overlay.transform.SetSiblingIndex(insert);

            var aurora = AuroraBackdrop.Attach(canvas.transform);
            aurora.transform.SetSiblingIndex(insert + 1);

            var far = RuntimeUI.CreateObject("BackdropNumbersFar", canvas.transform);
            RuntimeUI.Stretch(far);
            ConvergingLight.NumberField(far.transform, 24, 0.045f, 16, 30);
            far.transform.SetSiblingIndex(insert + 2);

            var near = RuntimeUI.CreateObject("BackdropNumbersNear", canvas.transform);
            RuntimeUI.Stretch(near);
            ConvergingLight.NumberField(near.transform, 10, 0.08f, 34, 52);
            near.transform.SetSiblingIndex(insert + 3);

            var edge = RuntimeUI.CreateObject("BackdropVignette", canvas.transform);
            RuntimeUI.Stretch(edge);
            var vig = edge.AddComponent<Image>();
            vig.sprite = ConvergingLightFX.Vignette;
            vig.color = ConvergingLight.WithAlpha(new Color(0.015f, 0.015f, 0.05f), 0.6f);
            vig.raycastTarget = false;
            edge.transform.SetSiblingIndex(insert + 4);

            RestylePalette(canvas.transform);
            RestyleControls(canvas.transform);
            break;
        }
    }

    // --- component pass -----------------------------------------------------
    //
    // Scene-authored controls join the same language runtime-built UI already
    // speaks: card panels with elevation and a seam hairline, role-colored
    // rounded buttons (gold CTA / cyan action / ghost indigo), softened icon
    // art. Names are the scene's own; every lookup is null-guarded so a
    // missing object simply keeps its authored look.

    static readonly string[] PrimaryButtons =
        { "ButtonPlay", "ButtonChallenger", "ButtonConfirm", "Buttonsave", "ButtonSTOPGAME" };
    static readonly string[] SecondaryButtons =
        { "ButtonHIGHER", "ButtonLOWER", "ButtonCORRECT" };
    static readonly string[] GhostButtons =
        { "ButtonBack", "Buttonback" };
    static readonly string[] IconButtons =
        { "Buttonsettings", "ButtonQuit" };

    void RestyleControls(Transform canvasRoot)
    {
        foreach (var name in PrimaryButtons)
            ConvergingLightFX.StyleButton(FindButton(canvasRoot, name),
                ConvergingLightFX.ButtonRole.Primary);
        foreach (var name in SecondaryButtons)
            ConvergingLightFX.StyleButton(FindButton(canvasRoot, name),
                ConvergingLightFX.ButtonRole.Secondary);
        foreach (var name in GhostButtons)
            ConvergingLightFX.StyleButton(FindButton(canvasRoot, name),
                ConvergingLightFX.ButtonRole.Ghost);
        foreach (var name in IconButtons)
            ConvergingLightFX.StyleIconButton(FindButton(canvasRoot, name));

        // Panels become floating cards; the searching scrim stays a scrim.
        StyleCardPanel(canvasRoot, "PanelPlay");
        StyleCardPanel(canvasRoot, "PanelSettings");
        StyleCardPanel(canvasRoot, "PanelGAME");

        // Scene input fields: rounded near-white wells, matching the
        // runtime-built ones.
        foreach (var input in canvasRoot.GetComponentsInChildren<TMP_InputField>(true))
        {
            var img = input.GetComponent<Image>();
            if (img == null) continue;
            img.sprite = RuntimeUI.RoundedRectSprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(1f, 1f, 1f, 0.95f);
            ConvergingLightFX.AddShadow(img);
        }

        // One gilded gesture per screen: the menu's Play and the challenger
        // panel's CTA carry the shine sweep; every other gold stays still.
        ConvergingLightFX.AddShine(FindButton(canvasRoot, "ButtonPlay"));
        ConvergingLightFX.AddShine(FindButton(canvasRoot, "ButtonChallenger"));

        // In-match headers read as engraved captions.
        StyleHeader(canvasRoot, "Opponentanme");
        StyleHeader(canvasRoot, "Yourtern");

        // Menu screens assemble instead of popping.
        AttachCascade(canvasRoot, "PanelPlay");
        AttachCascade(canvasRoot, "PanelSettings");
    }

    static Button FindButton(Transform root, string name)
    {
        var t = DeepFind(root, name);
        return t != null ? t.GetComponent<Button>() : null;
    }

    static void StyleCardPanel(Transform root, string name)
    {
        var t = DeepFind(root, name);
        var img = t != null ? t.GetComponent<Image>() : null;
        if (img != null) ConvergingLightFX.StyleCard(img);
    }

    static void StyleHeader(Transform root, string name)
    {
        var t = DeepFind(root, name);
        if (t == null) return;
        ConvergingLightFX.StyleHeading(t.GetComponent<TMP_Text>(), 3f);
    }

    static void AttachCascade(Transform root, string name)
    {
        var t = DeepFind(root, name);
        if (t != null && t.GetComponent<CascadeReveal>() == null)
            t.gameObject.AddComponent<CascadeReveal>();
    }

    // --- palette pass -------------------------------------------------------

    static void RestylePalette(Transform canvasRoot)
    {
        // Panel surfaces are handled by the component pass (StyleCardPanel).

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
