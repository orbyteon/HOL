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
    public IEnumerator ExactVisualsSurviveSceneTransitionRefreshNonButtonUiLocalizeAndStayScoped()
    {
        var exactType = RuntimeType("ExactReferenceVisuals");
        var boardReskinType = RuntimeType("AttachmentReskinVisuals");
        var homeType = RuntimeType("MainMenuHomeVisuals");

        // Invoke both runtime bootstraps explicitly so this regression remains
        // deterministic inside the Unity Test Runner as well as in a player.
        var install = exactType.GetMethod(
            "Install", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(install, Is.Not.Null);
        install.Invoke(null, null);

        var installBoardReskin = boardReskinType.GetMethod(
            "Install", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(installBoardReskin, Is.Not.Null);
        installBoardReskin.Invoke(null, null);

        var installHome = homeType.GetMethod(
            "Install", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(installHome, Is.Not.Null);
        installHome.Invoke(null, null);

        yield return SceneManager.LoadSceneAsync("SplashScene", LoadSceneMode.Single);
        yield return null;

        var splashVisuals = Object.FindObjectOfType(exactType) as Component;
        Assert.That(splashVisuals, Is.Not.Null,
            "SplashScene should receive the approved visuals owner.");
        var splashBoardReskin = Object.FindObjectOfType(boardReskinType) as Component;
        Assert.That(splashBoardReskin, Is.Not.Null,
            "SplashScene should receive the attachment reskin layer.");

        var splashLoader = Object.FindObjectOfType(RuntimeType("SplashLoader")) as Component;
        Assert.That(splashLoader, Is.Not.Null);
        splashLoader.SendMessage("LoadMenu", SendMessageOptions.RequireReceiver);

        while (SceneManager.GetActiveScene().name != "MainMenu")
            yield return null;
        for (int i = 0; i < 20; i++)
            yield return null;
        yield return new WaitForSecondsRealtime(0.35f);

        var mainMenuVisuals = Object.FindObjectOfType(exactType) as Component;
        Assert.That(mainMenuVisuals, Is.Not.Null,
            "ExactReferenceVisuals must be reinstalled after LoadScene(Single).");
        Assert.That(mainMenuVisuals.gameObject.scene.name, Is.EqualTo("MainMenu"));
        var ownedCanvas = mainMenuVisuals.GetComponent<Canvas>();
        Assert.That(ownedCanvas, Is.Not.Null,
            "The visuals owner should stay attached to its primary canvas.");

        var mainMenuBoardReskin = Object.FindObjectOfType(boardReskinType) as Component;
        Assert.That(mainMenuBoardReskin, Is.Not.Null,
            "The attachment reskin must survive the SplashScene to MainMenu transition.");
        Assert.That(mainMenuBoardReskin.GetComponent<Canvas>(), Is.SameAs(ownedCanvas));

        var homeOwner = Object.FindObjectOfType(homeType) as Component;
        Assert.That(homeOwner, Is.Not.Null,
            "MainMenu Home should be owned by MainMenuHomeVisuals.");
        Assert.That(FindByName(ownedCanvas.transform, "HomeLogo"), Is.Not.Null,
            "Cartoon Home must compose the HOL logo.");
        Assert.That(FindByName(ownedCanvas.transform, "HomeTipCard"), Is.Not.Null);
        Assert.That(FindByName(ownedCanvas.transform, "BoardHomeLogo"), Is.Null,
            "Attachment Home composition must not run on MainMenu.");

        // This is a reskin, not a feature pass. It may add images/text but it
        // must not create new interactive buttons, Store screens or Profile
        // screens that do not already exist in the product flow.
        foreach (var button in ownedCanvas.GetComponentsInChildren<Button>(true))
            Assert.That(button.name.StartsWith("Board"), Is.False,
                "AttachmentReskinVisuals must not invent a new interactive control: " + button.name);
        Assert.That(FindByName(ownedCanvas.transform, "BoardStorePanel"), Is.Null);
        Assert.That(FindByName(ownedCanvas.transform, "BoardProfilePanel"), Is.Null);

        // Runtime additions that contain no buttons still have to trigger a pass.
        // The previous button-count heuristic missed this case completely.
        var cardProbe = new GameObject("CardRefreshProbe",
            typeof(RectTransform), typeof(Image));
        cardProbe.transform.SetParent(ownedCanvas.transform, false);
        Assert.That(cardProbe.GetComponent<Outline>(), Is.Null);

        yield return new WaitForSecondsRealtime(0.6f);
        Assert.That(cardProbe.GetComponent<Outline>(), Is.Not.Null,
            "A newly-added non-button card should trigger exact visual styling.");

        // A screen can swap active panels without changing any component counts.
        // The signature must still change so the next refresh can restyle it.
        var visualSignature = exactType.GetMethod(
            "VisualSignature", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(visualSignature, Is.Not.Null);
        var activationProbe = new GameObject("ActivationSignatureProbe",
            typeof(RectTransform), typeof(Image));
        activationProbe.transform.SetParent(ownedCanvas.transform, false);
        activationProbe.SetActive(false);
        int inactiveSignature = (int)visualSignature.Invoke(mainMenuVisuals, null);
        activationProbe.SetActive(true);
        int activeSignature = (int)visualSignature.Invoke(mainMenuVisuals, null);
        Assert.That(activeSignature, Is.Not.EqualTo(inactiveSignature),
            "Active-state screen swaps must change the visual hierarchy signature.");

        var l10nType = RuntimeType("L10n");
        var languageType = l10nType.GetNestedType("Language", BindingFlags.Public);
        var currentLanguage = l10nType.GetProperty("Current", BindingFlags.Static | BindingFlags.Public);
        var setLanguage = l10nType.GetMethod("SetLanguage", BindingFlags.Static | BindingFlags.Public);
        Assert.That(languageType, Is.Not.Null);
        Assert.That(currentLanguage, Is.Not.Null);
        Assert.That(setLanguage, Is.Not.Null);

        object originalLanguage = currentLanguage.GetValue(null, null);
        object greek = System.Enum.Parse(languageType, "Greek");
        setLanguage.Invoke(null, new[] { greek });
        yield return null;

        var tmpTextType = System.Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
        Assert.That(tmpTextType, Is.Not.Null, "Missing TextMesh Pro runtime assembly.");
        Component profileText = null;
        foreach (var component in ownedCanvas.GetComponentsInChildren(tmpTextType, true))
        {
            if (component.name != "HomePlayerChipText") continue;
            profileText = component;
            break;
        }
        Assert.That(profileText, Is.Not.Null);
        string profileCopy = (string)tmpTextType.GetProperty("text").GetValue(profileText, null);
        string localizedStreak = LocalizedCopy(l10nType, "stats_streak");
        Assert.That(profileCopy, Does.Contain(localizedStreak));
        Assert.That(profileCopy, Does.Not.Contain("STREAK"));
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
        Object.Destroy(activationProbe);
        Object.Destroy(cardProbe);
    }

    static Transform FindByName(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindByName(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    static string LocalizedCopy(System.Type l10nType, string key)
    {
        foreach (var method in l10nType.GetMethods(BindingFlags.Static | BindingFlags.Public))
        {
            if (method.Name != "Get" || method.ReturnType != typeof(string)) continue;
            var parameters = method.GetParameters();
            if (parameters.Length == 0 || parameters[0].ParameterType != typeof(string)) continue;

            var arguments = new object[parameters.Length];
            arguments[0] = key;
            bool compatible = true;
            for (int i = 1; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType == typeof(object[]))
                    arguments[i] = new object[0];
                else if (parameters[i].HasDefaultValue)
                    arguments[i] = parameters[i].DefaultValue;
                else
                {
                    compatible = false;
                    break;
                }
            }
            if (compatible)
                return (string)method.Invoke(null, arguments);
        }

        Assert.Fail("Missing compatible L10n.Get(string, ...) overload.");
        return null;
    }

    static System.Type RuntimeType(string name)
    {
        var type = System.Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "Missing runtime component: " + name);
        return type;
    }
}
