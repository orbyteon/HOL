using System.Reflection;
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
    [TestCase("reference/mascot_6_exact")]
    [TestCase("reference/mascot_7_exact")]
    [TestCase("reference/char_girl_exact")]
    public void ApprovedCharacterPortraitLoadsAsSprite(string path)
    {
        Assert.IsNotNull(Resources.Load<Sprite>(path),
            "The approved portrait must import as a Sprite at Resources/" + path + ".");
    }

    [Test]
    public void PrivateRoomCopyHasEnglishAndGreekEntries()
    {
        var original = L10n.Current;
        try
        {
            L10n.SetLanguage(L10n.Language.English);
            Assert.AreNotEqual("private_room_title", L10n.Get("private_room_title"));
            Assert.AreNotEqual("private_room_tip", L10n.Get("private_room_tip"));

            L10n.SetLanguage(L10n.Language.Greek);
            Assert.AreNotEqual("private_room_title", L10n.Get("private_room_title"));
            Assert.AreNotEqual("private_room_tip", L10n.Get("private_room_tip"));
        }
        finally
        {
            L10n.SetLanguage(original);
        }
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
    public void ApprovedSplashDoesNotBuildLegacyVisuals()
    {
        var canvasObject = new GameObject("ExactSplashCanvas", typeof(RectTransform), typeof(Canvas));

        try
        {
            var panel = ChildWithImage(canvasObject.transform, "Panel");
            var oldLogo = ChildWithImage(canvasObject.transform, "Image");
            var numberField = Child(canvasObject.transform, "NumberField");
            var seam = Child(canvasObject.transform, "Seam");
            var seamBloom = Child(canvasObject.transform, "SeamBloom");
            var tagline = Child(canvasObject.transform, "Tagline");
            var progressTrack = Child(canvasObject.transform, "ProgressTrack");

            canvasObject.AddComponent(RuntimeType("SplashLoader"));
            var exact = canvasObject.AddComponent(RuntimeType("ExactReferenceVisuals"));
            InvokePrivate(exact, "Awake");
            InvokePrivate(exact, "LayoutSplash", canvasObject.transform);

            Assert.IsFalse(panel.GetComponent<Image>().enabled,
                "The legacy splash background must be hidden.");
            Assert.IsFalse(oldLogo.activeSelf,
                "The legacy splash logo must be hidden.");
            Assert.IsFalse(numberField.activeSelf);
            Assert.IsFalse(seam.activeSelf);
            Assert.IsFalse(seamBloom.activeSelf);
            Assert.IsFalse(tagline.activeSelf);
            Assert.IsTrue(progressTrack.activeSelf,
                "The existing loading line must remain available.");
            Assert.IsNotNull(canvasObject.transform.Find("ExactSplashLogo"),
                "The approved HOL logo should replace the legacy splash artwork.");
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    static GameObject Child(Transform parent, string name)
    {
        var child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child;
    }

    static GameObject ChildWithImage(Transform parent, string name)
    {
        var child = new GameObject(name, typeof(RectTransform), typeof(Image));
        child.transform.SetParent(parent, false);
        return child;
    }

    static void InvokePrivate(Component component, string methodName, params object[] arguments)
    {
        var method = component.GetType().GetMethod(
            methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, "Missing private method: " + methodName);
        method.Invoke(component, arguments);
    }

    static System.Type RuntimeType(string name)
    {
        var type = System.Type.GetType(name + ", Assembly-CSharp");
        Assert.IsNotNull(type, "Missing runtime component: " + name);
        return type;
    }
}
