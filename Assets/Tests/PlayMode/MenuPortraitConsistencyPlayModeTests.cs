using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class MenuPortraitConsistencyPlayModeTests
{
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    [UnityTest, Explicit("Native shared-menu layout evidence; run HOL/PvP/Review Shared Menu Portrait Layout in the licensed Editor.")]
    public IEnumerator NativeEnElSharedHeadersCardsAndButtonGlyphs()
    {
        var review = Type.GetType("PvpPresentationReviewTools, Assembly-CSharp-Editor", true);
        string parent = (string)review.GetProperty("OutputDirectory").GetValue(null);
        Assert.That(Directory.Exists(parent), Is.True, "Choose the existing external evidence parent in the Editor first.");
        string output = Path.Combine(parent, "MenuPortrait_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff"));
        Assert.That(Directory.Exists(output), Is.False, "Never overwrite an evidence run.");
        Directory.CreateDirectory(output);
        bool hadLanguage = PlayerPrefs.HasKey("Language");
        int language = PlayerPrefs.GetInt("Language", 0);
        var errors = new List<string>();
        var metrics = new List<string>();
        try
        {
            foreach (string owner in new[] { "MainMenuHomeVisuals", "MainMenuPlayVisuals", "SettingsVisuals" })
                T(owner).GetMethod("Install", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            Component home = null;
            for (int frame = 0; frame < 240; frame++)
            {
                home = UnityEngine.Object.FindObjectOfType(T("MainMenuHomeVisuals")) as Component;
                if (home != null && (bool)home.GetType().GetProperty("IsSettled").GetValue(home) &&
                    UnityEngine.Object.FindObjectOfType(T("PrivateRoomVisuals")) != null) break;
                yield return null;
            }
            Assert.That(home, Is.Not.Null);
            var menu = UnityEngine.Object.FindObjectOfType(T("MenuManager")) as Component;
            var pvp = UnityEngine.Object.FindObjectOfType(T("PvpGameController")) as Component;
            Assert.That(menu, Is.Not.Null);
            Assert.That(pvp, Is.Not.Null);
            Transform canvas = home.transform;
            string[] pageRoots = { "HomeVisualRoot", "PlayVisualRoot", "PrivateRoomVisualRoot", "PvPCreatePanelVisuals", "PvPJoinPanelVisuals", "SettingsVisualRoot" };
            string[] chips = { "HomePlayerChip", "PlayPlayerChip", "PrivateRoomPlayerChip", "PvPCreatePanelPlayerChip", "PvPJoinPanelPlayerChip", "SettingsPlayerChip" };
            string[] logos = { "HomeLogo", "PlayLogo", "PrivateRoomLogo", "PvPCreatePanelLogo", "PvPJoinPanelLogo", "SettingsLogo" };
            string[] labels = { "Home", "Play", "PrivateRoom", "Create", "Join", "Settings" };
            foreach (var viewport in new[] { new Vector2Int(1080, 1920), new Vector2Int(1080, 2340), new Vector2Int(1179, 2556) })
            {
                Type.GetType("OnboardingGameViewCapture, Assembly-CSharp-Editor", true)
                    .GetMethod("SetResolution").Invoke(null, new object[] { viewport.x, viewport.y });
                review.GetMethod("FocusNativeGameView").Invoke(null, null);
                yield return (IEnumerator)review.GetMethod("WaitForStableNativeViewport").Invoke(null, new object[] { canvas, viewport.x, viewport.y });
                Rect? expectedChip = null, expectedLogo = null;
                for (int lang = 0; lang < 2; lang++)
                {
                    SetLanguage(lang);
                    for (int page = 0; page < pageRoots.Length; page++)
                    {
                        pvp.SendMessage("ClosePvpMenu", SendMessageOptions.RequireReceiver);
                        menu.SendMessage("BackToMenu", SendMessageOptions.RequireReceiver);
                        if (page == 1) Find(canvas, "ButtonPlay").GetComponent<Button>().onClick.Invoke();
                        if (page >= 2 && page <= 4)
                        {
                            menu.SendMessage("OnPlayPressed", SendMessageOptions.RequireReceiver);
                            Find(canvas, "ButtonPvP").GetComponent<Button>().onClick.Invoke();
                            if (page > 2) Find(pvp.transform, page == 3 ? "CreateButton" : "JoinButton").GetComponent<Button>().onClick.Invoke();
                        }
                        if (page == 5) Find(canvas, "Buttonsettings").GetComponent<Button>().onClick.Invoke();
                        Transform pageCanvas = page >= 2 && page <= 4 ? pvp.transform : canvas;
                        Transform root = Find(pageCanvas, pageRoots[page]);
                        Assert.That(root, Is.Not.Null, pageRoots[page]);
                        yield return (IEnumerator)review.GetMethod("WaitForStableNativeViewport").Invoke(null, new object[] { pageCanvas, viewport.x, viewport.y });
                        // Capture the completed real entrance, not an arbitrary
                        // number of fast Editor frames halfway through its fade.
                        float settleDeadline = Time.realtimeSinceStartup + 5f;
                        while (root.GetComponentsInParent<CanvasGroup>().Any(group => group.alpha < .9999f) &&
                            Time.realtimeSinceStartup < settleDeadline) yield return null;
                        Assert.That(root.GetComponentsInParent<CanvasGroup>().All(group => group.alpha >= .9999f), Is.True,
                            pageRoots[page] + " entrance did not settle");
                        for (int frame = 0; frame < 4; frame++) yield return null;
                        Assert.That(root.gameObject.activeInHierarchy, Is.True, pageRoots[page]);
                        string name = labels[page] + "-" + (lang == 0 ? "en" : "el") + "-" + viewport.x + "x" + viewport.y;
                        var chip = Find(root, chips[page]) as RectTransform;
                        var logo = Find(root, logos[page]) as RectTransform;
                        Assert.That(chip, Is.Not.Null, chips[page]);
                        Assert.That(logo, Is.Not.Null, logos[page]);
                        Rect chipPixels = ScreenRect(chip), logoPixels = ScreenRect(logo);
                        if (!expectedChip.HasValue) { expectedChip = chipPixels; expectedLogo = logoPixels; }
                        CompareRect(expectedChip.Value, chipPixels, name + " shared profile", errors);
                        CompareRect(expectedLogo.Value, logoPixels, name + " shared HOL logo", errors);
                        if ((chip.sizeDelta - new Vector2(430, 167)).sqrMagnitude > .01f) errors.Add(name + " profile size");
                        if ((logo.sizeDelta - new Vector2(640, 310)).sqrMagnitude > .01f) errors.Add(name + " logo size");
                        var mask = chip.GetComponentInChildren<Mask>(true);
                        if (mask == null || mask.showMaskGraphic) errors.Add(name + " missing invisible circular portrait mask");
                        else if ((((RectTransform)mask.transform).sizeDelta - new Vector2(126, 126)).sqrMagnitude > .01f)
                            errors.Add(name + " portrait aperture size");
                        Contained(Screen.safeArea, chipPixels, name + " profile safe area", errors);
                        AuditGlyphs(root, name, errors);
                        AuditCenteredOwners(pageCanvas, root, name, errors);
                        AuditFormAperture(root, name, errors);
                        metrics.Add(name + " profile=" + chipPixels + " logo=" + logoPixels);
                        string path = Path.Combine(output, name + ".png");
                        ScreenCapture.CaptureScreenshot(path);
                        float deadline = Time.realtimeSinceStartup + 10f;
                        while ((!File.Exists(path) || new FileInfo(path).Length == 0) && Time.realtimeSinceStartup < deadline) yield return null;
                        Assert.That(File.Exists(path), Is.True, path);
                        var png = new Texture2D(2, 2);
                        Assert.That(png.LoadImage(File.ReadAllBytes(path)), Is.True);
                        Assert.That(png.width, Is.EqualTo(viewport.x));
                        Assert.That(png.height, Is.EqualTo(viewport.y));
                        UnityEngine.Object.Destroy(png);
                    }
                }
            }
            File.WriteAllLines(Path.Combine(output, "geometry.txt"), metrics);
            File.WriteAllLines(Path.Combine(output, "violations.txt"), errors);
            Debug.Log("HOL_MENU_PORTRAIT_NATIVE_EVIDENCE " + output + " violations=" + errors.Count);
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }
        finally
        {
            File.WriteAllLines(Path.Combine(output, "geometry.txt"), metrics);
            File.WriteAllLines(Path.Combine(output, "violations.txt"), errors);
            SetLanguage(language);
            if (!hadLanguage) PlayerPrefs.DeleteKey("Language");
            PlayerPrefs.Save();
        }
    }

    static void AuditGlyphs(Transform root, string context, List<string> errors)
    {
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(false))
        {
            if (!text.isActiveAndEnabled || string.IsNullOrWhiteSpace(text.text)) continue;
            var input = text.GetComponentInParent<TMP_InputField>();
            if (input != null && text == input.placeholder && !string.IsNullOrEmpty(input.text)) continue;
            // TMP keeps a zero-width caret sentinel in an empty input's text
            // mesh. Its visible localized placeholder is audited separately.
            if (input != null && text == input.textComponent && string.IsNullOrEmpty(input.text)) continue;
            text.ForceMeshUpdate();
            Rect? glyph = Glyphs(text, text.rectTransform);
            if (!glyph.HasValue) { errors.Add(context + " " + text.name + " has no visible glyphs"); continue; }
            if (text.isTextOverflowing || text.isTextTruncated) errors.Add(context + " " + text.name + " overflow/truncation: " + text.text);
            Contained(text.rectTransform.rect, glyph.Value, context + " " + text.name, errors);
        }
    }

    static void AuditFormAperture(Transform root, string context, List<string> errors)
    {
        var board = Find(root, "PrebattleBoard") as RectTransform;
        if (board == null) return;
        // Independent measured inner face of the production board PNG, excluding
        // its transparent gutter, rounded corners and rim. Never the full rect.
        Rect face = new Rect(-.39f * board.rect.width, -.4f * board.rect.height,
            .78f * board.rect.width, .8f * board.rect.height);
        foreach (string stateName in new[] { "EntryState", "WaitingState" })
        {
            Transform state = Find(root, stateName);
            if (state == null || !state.gameObject.activeInHierarchy) continue;
            if (state.GetComponent(T("ResponsivePageLayout")) != null)
                errors.Add(context + " competing generic page writer in " + stateName);
            foreach (var text in state.GetComponentsInChildren<TMP_Text>(false))
            {
                Rect? ink = Glyphs(text, board);
                if (ink.HasValue) Contained(face, ink.Value, context + " " + text.name + " board aperture", errors);
                if (!ink.HasValue) continue;
                foreach (var control in state.GetComponentsInChildren<Selectable>(false))
                {
                    if (text.transform.IsChildOf(control.transform)) continue;
                    var corners = new Vector3[4]; ((RectTransform)control.transform).GetWorldCorners(corners);
                    Vector3 a = board.InverseTransformPoint(corners[0]), b = board.InverseTransformPoint(corners[2]);
                    Rect surface = Rect.MinMaxRect(a.x, a.y, b.x, b.y);
                    float overlapX = Mathf.Min(surface.xMax, ink.Value.xMax) - Mathf.Max(surface.xMin, ink.Value.xMin);
                    float overlapY = Mathf.Min(surface.yMax, ink.Value.yMax) - Mathf.Max(surface.yMin, ink.Value.yMin);
                    if (overlapX > 1 && overlapY > 1)
                        errors.Add(context + " " + text.name + " covered by " + control.name);
                }
            }
        }
    }

    static void AuditCenteredOwners(Transform canvas, Transform page, string context, List<string> errors)
    {
        foreach (string name in new[] { "MainMenuHomeVisuals", "MainMenuPlayVisuals", "PrivateRoomVisuals", "SettingsVisuals" })
        {
            var owner = canvas.GetComponent(T(name));
            if (owner == null) continue;
            var property = owner.GetType().GetProperty("CenteredTextRegions", Flags);
            var regions = (IEnumerable)(property != null ? property.GetValue(owner) : owner.GetType().GetField("centered", Flags).GetValue(owner));
            if (regions == null) continue;
            foreach (object region in regions)
            {
                var text = (TMP_Text)region.GetType().GetField("Text", Flags).GetValue(region);
                if (text == null || !text.isActiveAndEnabled || !text.transform.IsChildOf(page) || string.IsNullOrWhiteSpace(text.text)) continue;
                Rect safe = (Rect)region.GetType().GetField("SafeRect", Flags).GetValue(region);
                Rect? glyph = Glyphs(text, text.transform.parent);
                if (!glyph.HasValue) continue;
                Contained(safe, glyph.Value, context + " " + text.name + " artwork face", errors);
                if (Mathf.Abs(glyph.Value.center.x - safe.center.x) > 4 || Mathf.Abs(glyph.Value.center.y - safe.center.y) > 4)
                    errors.Add(context + " " + text.name + " ink not centered: " + glyph.Value + " face=" + safe);
            }
        }
    }

    static Rect? Glyphs(TMP_Text text, Transform target)
    {
        text.ForceMeshUpdate();
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity), max = -new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        bool visible = false;
        foreach (var glyph in text.textInfo.characterInfo.Take(text.textInfo.characterCount))
        {
            if (!glyph.isVisible) continue;
            visible = true;
            Vector2 a = target.InverseTransformPoint(text.transform.TransformPoint(glyph.bottomLeft));
            Vector2 b = target.InverseTransformPoint(text.transform.TransformPoint(glyph.topRight));
            min = Vector2.Min(min, Vector2.Min(a, b)); max = Vector2.Max(max, Vector2.Max(a, b));
        }
        return visible ? (Rect?)Rect.MinMaxRect(min.x, min.y, max.x, max.y) : null;
    }

    static void Contained(Rect outer, Rect inner, string context, List<string> errors)
    {
        if (inner.xMin < outer.xMin - 1 || inner.xMax > outer.xMax + 1 || inner.yMin < outer.yMin - 1 || inner.yMax > outer.yMax + 1)
            errors.Add(context + " bounds=" + inner + " outside=" + outer);
    }
    static void CompareRect(Rect expected, Rect actual, string context, List<string> errors)
    {
        if (Mathf.Abs(expected.x - actual.x) > 1 || Mathf.Abs(expected.y - actual.y) > 1 ||
            Mathf.Abs(expected.width - actual.width) > 1 || Mathf.Abs(expected.height - actual.height) > 1)
            errors.Add(context + " differs: " + actual + " expected=" + expected);
    }
    static Rect ScreenRect(RectTransform rect)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
    static Transform Find(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == name);
    static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    static void SetLanguage(int language) => T("L10n").GetMethod("SetLanguage").Invoke(null,
        new[] { Enum.ToObject(T("L10n").GetNestedType("Language"), language) });
}
