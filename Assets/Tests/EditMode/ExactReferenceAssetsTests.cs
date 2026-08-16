using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

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

            canvasObject.AddComponent(RuntimeType("ExactReferenceVisuals"));

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
    public void ApprovedSplashDoesNotBuildLegacyVisuals()
    {
        var canvasObject = new GameObject("ExactSplashCanvas", typeof(RectTransform), typeof(Canvas));

        try
        {
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasObject.transform, false);
            var oldLogo = new GameObject("Image", typeof(RectTransform), typeof(Image));
            oldLogo.transform.SetParent(canvasObject.transform, false);

            canvasObject.AddComponent(RuntimeType("ExactReferenceVisuals"));
            var splash = canvasObject.AddComponent(RuntimeType("SplashDesign"));
            splash.SendMessage("Start");

            Assert.IsNotNull(canvasObject.transform.Find("ProgressTrack"),
                "The existing loading line must remain available.");
            Assert.IsNull(canvasObject.transform.Find("NumberField"));
            Assert.IsNull(canvasObject.transform.Find("Seam"));
            Assert.IsNull(canvasObject.transform.Find("SeamBloom"));
            Assert.IsNull(canvasObject.transform.Find("Tagline"));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    static System.Type RuntimeType(string name)
    {
        var type = System.Type.GetType(name + ", Assembly-CSharp");
        Assert.IsNotNull(type, "Missing runtime component: " + name);
        return type;
    }
}
