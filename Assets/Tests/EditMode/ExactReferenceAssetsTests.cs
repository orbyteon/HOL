using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExactReferenceAssetsTests
{
    [Test]
    public void ApprovedHolLogoLoadsAsSprite()
    {
        var logo = Resources.Load<Sprite>("reference/hol_logo_exact");
        Assert.IsNotNull(logo,
            "The approved HOL logo must import as a Sprite at Resources/reference/hol_logo_exact.");
    }

    [TestCase("reference/player_cyan_exact")]
    [TestCase("reference/opponent_purple_exact")]
    [TestCase("reference/mascot_7_exact")]
    [TestCase("reference/mascot_3_exact")]
    public void ApprovedCharacterPortraitLoadsAsSprite(string path)
    {
        Assert.IsNotNull(Resources.Load<Sprite>(path),
            "The approved portrait must import as a Sprite at Resources/" + path + ".");
    }

    [Test]
    public void ApprovedLayerDisablesLegacyScenePresentation()
    {
        var legacyObject = new GameObject("LegacyDesign");
        var canvasObject = new GameObject("ExactCanvas", typeof(RectTransform), typeof(Canvas));

        try
        {
            var legacy = (Behaviour)legacyObject.AddComponent(RuntimeType("DesignRuntimeWiring"));
            Assert.IsTrue(legacy.enabled);

            var exact = canvasObject.AddComponent(RuntimeType("ExactReferenceVisuals"));
            InvokePrivate(exact, "Awake");

            Assert.IsFalse(legacy.enabled,
                "The discarded scene presentation must be disabled before its Start method.");
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(legacyObject);
        }
    }

    [Test]
    public void ExactReferenceInstallerSkipsSplashScene()
    {
        var splashScene = SceneManager.CreateScene("SplashScene");
        var canvasObject = new GameObject("SplashCanvas", typeof(RectTransform), typeof(Canvas));

        try
        {
            SceneManager.MoveGameObjectToScene(canvasObject, splashScene);
            var exactType = RuntimeType("ExactReferenceVisuals");

            InvokePrivateStatic(exactType, "InstallForScene", splashScene);

            Assert.IsNull(canvasObject.GetComponent(exactType),
                "ExactReferenceVisuals must leave Splash presentation to SplashDesign.");
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
            EditorSceneManager.CloseScene(splashScene, true);
        }
    }

    static void InvokePrivate(Component component, string methodName, params object[] arguments)
    {
        var method = component.GetType().GetMethod(
            methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, "Missing private method: " + methodName);
        method.Invoke(component, arguments);
    }

    static void InvokePrivateStatic(
        System.Type type, string methodName, params object[] arguments)
    {
        var method = type.GetMethod(
            methodName, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(method, "Missing private static method: " + methodName);
        method.Invoke(null, arguments);
    }

    static System.Type RuntimeType(string name)
    {
        var type = System.Type.GetType(name + ", Assembly-CSharp");
        Assert.IsNotNull(type, "Missing runtime component: " + name);
        return type;
    }
}
