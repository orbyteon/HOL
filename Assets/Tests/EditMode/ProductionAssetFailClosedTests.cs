using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class ProductionAssetFailClosedTests
{
    [Test]
    public void MissingProductionSpriteClearsAndDisablesPreviousFallback()
    {
        var host = new GameObject(
            "ProductionAssetFailClosedHost",
            typeof(RectTransform),
            typeof(Image));
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        var fallback = Sprite.Create(
            texture,
            new Rect(0f, 0f, 2f, 2f),
            new Vector2(0.5f, 0.5f));

        try
        {
            var image = host.GetComponent<Image>();
            image.sprite = fallback;
            image.enabled = true;
            image.raycastTarget = true;

            const string missingPath = "__tests__/missing-production-art";
            LogAssert.Expect(
                LogType.Error,
                "HOL UI: missing approved production sprite Resources/" +
                missingPath + ".");

            bool applied = InvokeApplyProductionSprite(image, missingPath);

            Assert.That(applied, Is.False);
            Assert.That(image.sprite, Is.Null,
                "A procedural or generic fallback must not survive a required-art failure.");
            Assert.That(image.enabled, Is.False,
                "The Image must be disabled so missing art cannot become the production look.");
            Assert.That(image.raycastTarget, Is.False,
                "An invisible failed visual must not keep intercepting input.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
            UnityEngine.Object.DestroyImmediate(fallback);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    static bool InvokeApplyProductionSprite(Image image, string path)
    {
        Type runtimeUi = Type.GetType("RuntimeUI, Assembly-CSharp");
        Assert.That(runtimeUi, Is.Not.Null,
            "RuntimeUI must compile into Assembly-CSharp until assembly migration lands.");

        MethodInfo method = runtimeUi.GetMethod(
            "ApplyProductionSprite",
            BindingFlags.Public | BindingFlags.Static);
        Assert.That(method, Is.Not.Null, "RuntimeUI.ApplyProductionSprite not found.");

        object result = method.Invoke(
            null,
            new object[] { image, path, Image.Type.Sliced, false, 1f });
        Assert.That(result, Is.TypeOf<bool>());
        return (bool)result;
    }
}
