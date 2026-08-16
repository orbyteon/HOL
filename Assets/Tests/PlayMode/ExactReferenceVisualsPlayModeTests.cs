using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class ExactReferenceVisualsPlayModeTests
{
    [UnityTest]
    public IEnumerator ExactVisualsReinstallFromSplashIntoMainMenuAndStayScoped()
    {
        // Invoke the runtime bootstrap explicitly so this regression remains
        // deterministic inside the Unity Test Runner as well as in a player.
        var install = typeof(ExactReferenceVisuals).GetMethod(
            "Install", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(install, Is.Not.Null);
        install.Invoke(null, null);

        yield return SceneManager.LoadSceneAsync("SplashScene", LoadSceneMode.Single);
        yield return null;

        var splashVisuals = Object.FindObjectOfType<ExactReferenceVisuals>();
        Assert.That(splashVisuals, Is.Not.Null,
            "SplashScene should receive the approved visuals owner.");

        var splashLoader = Object.FindObjectOfType<SplashLoader>();
        Assert.That(splashLoader, Is.Not.Null);
        splashLoader.SendMessage("LoadMenu", SendMessageOptions.RequireReceiver);

        while (SceneManager.GetActiveScene().name != "MainMenu")
            yield return null;
        yield return null;

        var mainMenuVisuals = Object.FindObjectOfType<ExactReferenceVisuals>();
        Assert.That(mainMenuVisuals, Is.Not.Null,
            "ExactReferenceVisuals must be reinstalled after LoadScene(Single).");
        Assert.That(mainMenuVisuals.gameObject.scene.name, Is.EqualTo("MainMenu"));
        Assert.That(mainMenuVisuals.GetComponent<Canvas>(), Is.Not.Null,
            "The visuals owner should stay attached to its primary canvas.");

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

        Object.Destroy(unrelated);
    }
}
