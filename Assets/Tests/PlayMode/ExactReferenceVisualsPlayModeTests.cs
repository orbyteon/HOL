using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class ExactReferenceVisualsPlayModeTests
{
    [UnityTest]
    public IEnumerator ExactVisualsSurviveSceneTransitionRefreshNonButtonUiLocalizeAndStayScoped()
    {
        var exactType = RuntimeType("ExactReferenceVisuals");

        // Invoke the runtime bootstrap explicitly so this regression remains
        // deterministic inside the Unity Test Runner as well as in a player.
        var install = exactType.GetMethod(
            "Install", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(install, Is.Not.Null);
        install.Invoke(null, null);

        yield return SceneManager.LoadSceneAsync("SplashScene", LoadSceneMode.Single);
        yield return null;

        var splashVisuals = Object.FindObjectOfType(exactType) as Component;
        Assert.That(splashVisuals, Is.Not.Null,
            "SplashScene should receive the approved visuals owner.");

        var splashLoader = Object.FindObjectOfType(RuntimeType("SplashLoader")) as Component;
        Assert.That(splashLoader, Is.Not.Null);
        splashLoader.SendMessage("LoadMenu", SendMessageOptions.RequireReceiver);

        while (SceneManager.GetActiveScene().name != "MainMenu")
            yield return null;
        yield return null;

        var mainMenuVisuals = Object.FindObjectOfType(exactType) as Component;
        Assert.That(mainMenuVisuals, Is.Not.Null,
            "ExactReferenceVisuals must be reinstalled after LoadScene(Single).");
        Assert.That(mainMenuVisuals.gameObject.scene.name, Is.EqualTo("MainMenu"));
        var ownedCanvas = mainMenuVisuals.GetComponent<Canvas>();
        Assert.That(ownedCanvas, Is.Not.Null,
            "The visuals owner should stay attached to its primary canvas.");

        // Runtime additions that contain no buttons still have to trigger a pass.
        // The previous button-count heuristic missed this case completely.
        var cardProbe = new GameObject("CardRefreshProbe",
            typeof(RectTransform), typeof(Image));
        cardProbe.transform.SetParent(ownedCanvas.transform, false);
        Assert.That(cardProbe.GetComponent<Outline>(), Is.Null);

        yield return new WaitForSecondsRealtime(0.6f);
        Assert.That(cardProbe.GetComponent<Outline>(), Is.Not.Null,
            "A newly-added non-button card should trigger exact visual styling.");

        var l10nType = RuntimeType("L10n");
        var languageType = l10nType.GetNestedType("Language", BindingFlags.Public);
        var currentLanguage = l10nType.GetProperty("Current", BindingFlags.Static | BindingFlags.Public);
        var setLanguage = l10nType.GetMethod("SetLanguage", BindingFlags.Static | BindingFlags.Public);
        Assert.That(languageType, Is.Not.Null);
        Assert.That(currentLanguage, Is.Not.Null);
        Assert.That(setLanguage, Is.Not.Null);

        object originalLanguage = currentLanguage.GetValue(null, null);
        object greek = Enum.Parse(languageType, "Greek");
        setLanguage.Invoke(null, new[] { greek });
        yield return null;

        TMP_Text profileText = null;
        foreach (var text in ownedCanvas.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name != "ExactPlayerChipText") continue;
            profileText = text;
            break;
        }
        Assert.That(profileText, Is.Not.Null);
        Assert.That(profileText.text, Does.Contain("ΣΕΡΙ"));
        Assert.That(profileText.text, Does.Not.Contain("STREAK"));
        setLanguage.Invoke(null, new[] { originalLanguage });

        // A separate world-space canvas represents SDK/debug/3D UI. The exact
        // layer must not style it just because names resemble game UI.
        var unrelated = new GameObject("UnrelatedWorldSpaceCanvas",
            typeof(RectTransform), typeof(Canvas));
        var unrelatedCanvas = unrelated.GetComponent<Canvas>();
        unrelatedCanvas.renderMode = RenderMode.WorldSpace;

        var panel = new GameObject("PanelDebug", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(unrelated.transform, false);
        var panelImage = panel.GetComponent<Image>();
        var originalColor = new Color(0.21f, 0.32f, 0.43f, 0.54f);
        panelImage.color = originalColor;

        yield return new WaitForSecondsRealtime(0.6f);
        Assert.That(panelImage.color, Is.EqualTo(originalColor),
            "Unrelated canvases must not be mutated by the exact visual skin.");
        Assert.That(panel.GetComponent<Outline>(), Is.Null,
            "Unrelated canvases must not receive exact visual components.");

        Object.Destroy(unrelated);
        Object.Destroy(cardProbe);
    }

    static Type RuntimeType(string name)
    {
        var type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "Missing runtime component: " + name);
        return type;
    }
}
