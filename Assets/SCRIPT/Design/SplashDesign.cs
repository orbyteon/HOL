using UnityEngine;
using UnityEngine.UI;

// Converging Light splash dressing, built entirely at runtime on top of the
// hand-authored scene (flat "Panel" background + HOLSPASH logo + SplashLoader):
//   - layered indigo depth replaces the flat black
//   - faint drifting number field ("numbers as texture")
//   - logo entrance bloom (fade + scale), then a gentle breathing pulse
//   - cyan-to-magenta seam hairline with soft bloom under the logo
//   - letterspaced brand tagline (L10n "splash_tagline")
//   - gold loading hairline tracking SplashLoader.waitTime
// Null-guarded: if the scene pieces are missing it quietly does nothing,
// and SplashLoader keeps working untouched (tap-to-skip included).
public class SplashDesign : MonoBehaviour
{
    const float EntranceDuration = 0.9f;

    RectTransform logoRect;
    CanvasGroup logoGroup;
    Image progressFill;
    float waitTime = 2.5f;
    float elapsed;
    float entranceT;
    bool ready;

    void Start()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        var loader = FindObjectOfType<SplashLoader>();
        if (loader != null) waitTime = loader.waitTime;

        Transform panelT = canvas.transform.Find("Panel");
        Transform logoT = canvas.transform.Find("Image");
        if (panelT == null || logoT == null) return;

        // 1. Indigo depth instead of flat black.
        var bg = panelT.GetComponent<Image>();
        if (bg != null)
        {
            bg.sprite = ConvergingLight.VerticalGradient(
                ConvergingLight.DepthTop, ConvergingLight.DepthBottom);
            bg.color = Color.white;
            bg.type = Image.Type.Simple;
        }

        // 2. Aurora + number field + vignette: above the background, below
        // the logo. The same nocturnal stack the menu wears.
        var aurora = AuroraBackdrop.Attach(canvas.transform);
        aurora.transform.SetSiblingIndex(1);

        var field = RuntimeUI.CreateObject("NumberField", canvas.transform);
        RuntimeUI.Stretch(field);
        field.transform.SetSiblingIndex(2);
        ConvergingLight.NumberField(field.transform, 36, 0.08f);

        var edge = RuntimeUI.CreateObject("Vignette", canvas.transform);
        RuntimeUI.Stretch(edge);
        var vig = edge.AddComponent<Image>();
        vig.sprite = ConvergingLightFX.Vignette;
        vig.color = ConvergingLight.WithAlpha(new Color(0.015f, 0.015f, 0.05f), 0.6f);
        vig.raycastTarget = false;
        edge.transform.SetSiblingIndex(3);

        // 3. Logo entrance bloom, over its own gold halo — "the way a
        // cartographer gilds only the destination".
        logoRect = (RectTransform)logoT;
        BuildLogoHalo(canvas.transform, logoRect, logoT);
        logoGroup = logoT.GetComponent<CanvasGroup>();
        if (logoGroup == null) logoGroup = logoT.gameObject.AddComponent<CanvasGroup>();
        logoGroup.alpha = 0f;
        entranceT = 0f;

        // 4. The seam: one hairline of light, cyan to magenta, with bloom.
        BuildSeam(canvas.transform);

        // 5. Brand tagline in engraved small caps.
        var tag = RuntimeUI.CreateText(canvas.transform, "Tagline",
            L10n.Get("splash_tagline"), 30, new Vector2(0f, -580f),
            new Vector2(1020f, 60f),
            ConvergingLight.WithAlpha(ConvergingLight.NearWhite, 0.75f));
        tag.raycastTarget = false;

        // 6. Gold loading hairline — the one scarce metal, at the destination.
        BuildProgress(canvas.transform);

        ready = true;
    }

    static void BuildLogoHalo(Transform canvas, RectTransform logoRect, Transform logoT)
    {
        var halo = RuntimeUI.CreateObject("LogoHalo", canvas);
        var haloRect = (RectTransform)halo.transform;
        if (logoRect.anchorMin == logoRect.anchorMax)
        {
            haloRect.anchorMin = logoRect.anchorMin;
            haloRect.anchorMax = logoRect.anchorMax;
            haloRect.anchoredPosition = logoRect.anchoredPosition;
        }
        haloRect.sizeDelta = new Vector2(700f, 700f);

        var img = halo.AddComponent<Image>();
        img.sprite = ConvergingLightFX.RadialGlow;
        img.color = ConvergingLight.WithAlpha(ConvergingLight.Gold, 0.10f);
        img.raycastTarget = false;

        // Just beneath the logo in paint order.
        halo.SetSiblingIndex(logoT.GetSiblingIndex());
    }

    void BuildSeam(Transform canvas)
    {
        var seamSprite = ConvergingLight.HorizontalGradient(
            ConvergingLight.Cyan, ConvergingLight.Magenta);

        // Soft bloom: a taller, very low alpha halo behind the hairline.
        var bloom = RuntimeUI.CreateObject("SeamBloom", canvas);
        ConvergingLight.Center(bloom, new Vector2(0f, -520f), new Vector2(640f, 42f));
        var bloomImg = bloom.AddComponent<Image>();
        bloomImg.sprite = seamSprite;
        bloomImg.color = ConvergingLight.WithAlpha(Color.white, 0.18f);
        bloomImg.raycastTarget = false;

        var seam = RuntimeUI.CreateObject("Seam", canvas);
        ConvergingLight.Center(seam, new Vector2(0f, -520f), new Vector2(640f, 3f));
        var seamImg = seam.AddComponent<Image>();
        seamImg.sprite = seamSprite;
        seamImg.color = ConvergingLight.WithAlpha(Color.white, 0.9f);
        seamImg.raycastTarget = false;
    }

    void BuildProgress(Transform canvas)
    {
        var track = RuntimeUI.CreateObject("ProgressTrack", canvas);
        ConvergingLight.Center(track, new Vector2(0f, -820f), new Vector2(420f, 3f));
        var trackImg = track.AddComponent<Image>();
        trackImg.color = ConvergingLight.WithAlpha(ConvergingLight.TrackIndigo, 0.9f);
        trackImg.raycastTarget = false;

        var fill = RuntimeUI.CreateObject("ProgressFill", track.transform);
        var fillRect = (RectTransform)fill.transform;
        fillRect.anchorMin = new Vector2(0f, 0.5f);
        fillRect.anchorMax = new Vector2(0f, 0.5f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.sizeDelta = new Vector2(420f, 3f);
        fillRect.anchoredPosition = Vector2.zero;

        progressFill = fill.AddComponent<Image>();
        progressFill.sprite = ConvergingLight.VerticalGradient(
            ConvergingLight.Gold, ConvergingLight.Gold, 4);
        progressFill.color = Color.white;
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillAmount = 0f;
        progressFill.raycastTarget = false;
    }

    void Update()
    {
        if (!ready) return;

        // Logo: ease-out entrance, then a quiet breathing pulse.
        if (entranceT < 1f)
        {
            entranceT = Mathf.Min(1f, entranceT + Time.unscaledDeltaTime / EntranceDuration);
            float e = 1f - Mathf.Pow(1f - entranceT, 3f);
            logoGroup.alpha = e;
            float s = Mathf.Lerp(0.86f, 1f, e);
            logoRect.localScale = new Vector3(s, s, 1f);
        }
        else
        {
            float breathe = 1f + Mathf.Sin(Time.unscaledTime * 1.6f) * 0.012f;
            logoRect.localScale = new Vector3(breathe, breathe, 1f);
        }

        elapsed += Time.unscaledDeltaTime;
        if (progressFill != null)
            progressFill.fillAmount = Mathf.Clamp01(elapsed / waitTime);
    }
}
