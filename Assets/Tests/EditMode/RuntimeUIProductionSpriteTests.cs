using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class RuntimeUIProductionSpriteTests
{
    static MethodInfo ApplyProductionSprite()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType("RuntimeUI");
            if (type == null) continue;

            var method = type.GetMethod(
                "ApplyProductionSprite",
                BindingFlags.Public | BindingFlags.Static);
            if (method != null) return method;
        }

        Assert.Fail("RuntimeUI.ApplyProductionSprite not found");
        return null;
    }

    [Test]
    public void MissingApprovedSpriteRemovesFallbackAndDisablesImage()
    {
        var root = new GameObject("MissingProductionSpriteTest");
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        var fallback = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f));

        try
        {
            var image = root.AddComponent<Image>();
            image.sprite = fallback;
            image.enabled = true;
            image.raycastTarget = true;
            image.type = Image.Type.Sliced;
            image.preserveAspect = true;
            image.pixelsPerUnitMultiplier = 2f;
            image.color = Color.red;

            const string missingPath = "__tests__/missing-approved-production-sprite";
            LogAssert.Expect(
                LogType.Error,
                "HOL UI: missing approved production sprite Resources/" +
                missingPath + ".");

            object[] args =
            {
                image,
                missingPath,
                Image.Type.Sliced,
                false,
                2f,
            };
            bool applied = (bool)ApplyProductionSprite().Invoke(null, args);

            Assert.IsFalse(applied);
            Assert.IsNull(image.sprite,
                "A procedural or stale sprite must not survive a required-art failure.");
            Assert.IsFalse(image.enabled,
                "Missing approved artwork must fail closed rather than render a fallback.");
            Assert.IsFalse(image.raycastTarget,
                "An invisible failed control must not retain a hidden input target.");
            Assert.AreEqual(Image.Type.Simple, image.type);
            Assert.IsFalse(image.preserveAspect);
            Assert.AreEqual(1f, image.pixelsPerUnitMultiplier);
            Assert.AreEqual(Color.white, image.color);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(fallback);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
