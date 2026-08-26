using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class ProductionSymbolPlayModeTests
{
    [UnityTest]
    public IEnumerator HomeSpeechAndPromoUseCurrentOwnerWithoutUnsupportedStarGlyphs()
    {
        InvokeInstaller(RuntimeType("MainMenuHomeVisuals"));
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        for (int i = 0; i < 20; i++) yield return null;

        var owner = UnityEngine.Object.FindObjectOfType(
            RuntimeType("MainMenuHomeVisuals")) as Component;
        Assert.That(owner, Is.Not.Null);

        foreach (string textName in new[]
        {
            "HomeSpeechText",
            "HomePromoTitle",
            "HomePromoBody",
        })
        {
            Transform target = Find(owner.transform, textName);
            Assert.That(target, Is.Not.Null, "Missing current Home text: " + textName);
            var text = target.GetComponent<TMP_Text>();
            Assert.That(text, Is.Not.Null, textName);
            Assert.That(text.text, Does.Not.Contain(char.ConvertFromUtf32(0x2605)),
                textName + " must use dedicated art/text rather than an unsupported star glyph.");
        }
    }

    [UnityTest]
    public IEnumerator SoloBackspaceShowsLeftArrowAndDeletesExactlyOneDigit()
    {
        var boardObject = new GameObject("PanelGAME", typeof(RectTransform));
        var inputObject = new GameObject("NumberInput", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        inputObject.transform.SetParent(boardObject.transform, false);

        Component numberManager = boardObject.AddComponent(RuntimeType("NumberManager"));
        var input = inputObject.GetComponent<TMP_InputField>();
        numberManager.GetType().GetField("numberInput").SetValue(numberManager, input);

        Component layout = boardObject.AddComponent(RuntimeType("HolDuelBoardLayout"));
        ((Behaviour)layout).enabled = false;
        MethodInfo build = layout.GetType().GetMethod("Build",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(build, Is.Not.Null);
        build.Invoke(layout, null);

        Transform backspaceTransform = Find(boardObject.transform, "Key_BACKSPACE");
        Assert.That(backspaceTransform, Is.Not.Null,
            "The visible label must not also be the internal command identifier.");
        var label = backspaceTransform.GetComponentInChildren<TMP_Text>();
        Assert.That(label, Is.Not.Null);
        Assert.That(label.text, Is.EqualTo(char.ConvertFromUtf32(0x2190)));

        input.text = "123";
        backspaceTransform.GetComponent<Button>().onClick.Invoke();
        Assert.That(input.text, Is.EqualTo("12"),
            "Backspace must remove exactly the final digit.");

        UnityEngine.Object.Destroy(boardObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ApprovedSoloTrophyIsARealProductionSprite()
    {
        // Vector Graphics resources finish their player-side initialization on
        // the first PlayMode frame. A synchronous NUnit test can run before that
        // boundary and report a false missing-resource failure.
        yield return null;

        var trophy = Resources.Load<Sprite>("reference/board_trophy_exact");
        Assert.That(trophy, Is.Not.Null,
            "Result presentation requires the approved trophy sprite.");
        Assert.That(trophy.texture, Is.Not.Null);
        Assert.That(trophy.texture.width, Is.GreaterThanOrEqualTo(256));
        Assert.That(trophy.texture.height, Is.GreaterThanOrEqualTo(256));
    }

    static void InvokeInstaller(Type type)
    {
        MethodInfo install = type.GetMethod("Install",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(install, Is.Not.Null, "Missing runtime installer on " + type.Name);
        install.Invoke(null, null);
    }

    static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = Find(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    static Type RuntimeType(string name)
    {
        Type type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "Missing runtime component: " + name);
        return type;
    }
}
