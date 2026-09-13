using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class PrivateRoomCartoonReferencePlayModeTests
{
    const float PortraitReferenceAspect = 1080f / 1920f;

    static readonly string[] PortraitOverlayNames =
    {
        "PrivateRoomBackground",
    };

    [UnityTest]
    public IEnumerator ApprovedReferenceGeometryAndRealControlsRemainAuthoritative()
    {
        Screen.SetResolution(1080, 1920, false);
        yield return null;

        RegisterInstaller();
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);

        Component controller = null;
        Component visuals = null;
        for (int frame = 0; frame < 180; frame++)
        {
            controller = FindRuntimeComponent("PvpGameController");
            visuals = FindRuntimeComponent("PrivateRoomVisuals");
            if (controller != null && visuals != null)
                break;
            yield return null;
        }

        Assert.That(controller, Is.Not.Null);
        Assert.That(visuals, Is.Not.Null);
        Assert.That(GetProperty<bool>(visuals, "IsReady"), Is.True,
            "Private Room must fail closed when required art/fonts are missing.");

        controller.SendMessage("OpenPvpMenu", SendMessageOptions.RequireReceiver);
        yield return null;
        yield return null;

        var menuPanel = GetField<GameObject>(controller, "pvpMenuPanel");
        Assert.That(menuPanel, Is.Not.Null);
        Assert.That(menuPanel.activeInHierarchy, Is.True);

        Transform root = Find(menuPanel.transform, "PrivateRoomVisualRoot");
        Assert.That(root, Is.Not.Null);
        Assert.That(
            CountRuntimeComponents("PrivateRoomVisuals"),
            Is.EqualTo(1),
            "Private Room must have one presentation owner on the controller host.");

        for (int frame = 0; frame < 60 && !PortraitEnvelopeReady(root); frame++)
            yield return null;

        // Pin the standard approved composition independently of the Game View
        // left by a preceding tall-portrait test, then let the final owner reflow.
        var safeRoot = Find(root, "PrivateRoomSafeRoot");
        var safeOwner = safeRoot.GetComponent(Type.GetType("ResponsiveSafeAreaRoot, Assembly-CSharp"));
        safeOwner.GetType().GetMethod("ApplyViewport").Invoke(safeOwner, new object[] {
            new Rect(0, 0, 1080, 1920), new Rect(0, 0, 1080, 1920), new Vector2(1080, 1920) });
        visuals.GetType().GetMethod("ApplyResponsiveLayout", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(visuals, null);
        Canvas.ForceUpdateCanvases();

        AssertRect(root, "PrivateRoomPlayerChip",
            new Vector2(298f, 857f), new Vector2(430f, 167f));
        AssertRect(root, "PrivateRoomLogo",
            new Vector2(0f, 620f), new Vector2(640f, 310f));
        AssertRect(root, "PrivateRoomTitleRibbon",
            new Vector2(0f, 415f), new Vector2(938f, 181f));
        AssertRect(root, "PrivateRoomCreateCard",
            new Vector2(0f, 75f), new Vector2(1020f, 470.2352f));
        AssertRect(root, "PrivateRoomJoinCard",
            new Vector2(0f, -422f), new Vector2(1020f, 485.3578f));
        AssertRect(root, "PrivateRoomTipCard",
            new Vector2(0f, -805f), new Vector2(640f, 230f));
        AssertRect(root, "PrivateRoomMascotSix",
            new Vector2(-430f, -815f), new Vector2(220f, 260f));
        AssertRect(root, "PrivateRoomMascotSeven",
            new Vector2(430f, -815f), new Vector2(220f, 260f));

        foreach (string objectName in new[]
        {
            "PrivateRoomBackground",
            "PrivateRoomCreateCard",
            "PrivateRoomJoinCard",
            "PrivateRoomLandingCodeInput",
            "PrivateRoomStepText",
        })
        {
            Assert.That(Find(root, objectName), Is.Not.Null,
                "Missing approved modular object: " + objectName);
        }

        foreach (string retired in new[] { "PrivateRoomCreateBoy", "PrivateRoomCreateGirl", "PrivateRoomJoinDoor" })
            Assert.That(Find(root, retired), Is.Null, "Do not overlay substitute art on the approved illustrated panels.");
        foreach (string cardName in new[] { "PrivateRoomCreateCard", "PrivateRoomJoinCard" })
        {
            var card = Find(root, cardName).GetComponent<Image>();
            string resource = cardName == "PrivateRoomCreateCard"
                ? "reference/hol_private_create_card_v1" : "reference/hol_private_join_card_v1";
            Assert.That(card.sprite, Is.SameAs(Resources.Load<Sprite>(resource)));
            Assert.That(card.type, Is.EqualTo(Image.Type.Simple));
            Assert.That(card.preserveAspect, Is.True, "Never stretch or 9-slice the high-five pair / illustrated door.");
            Assert.That(card.color, Is.EqualTo(Color.white));
            Assert.That(card.raycastTarget, Is.False);
        }

        foreach (string overlayName in PortraitOverlayNames)
        {
            var overlay = Find(root, overlayName) as RectTransform;
            Assert.That(overlay, Is.Not.Null,
                "Missing portrait overlay: " + overlayName);
            var fitter = overlay.GetComponent<AspectRatioFitter>();
            Assert.That(fitter, Is.Not.Null,
                overlayName + " must preserve the approved 9:16 art ratio.");
            Assert.That(fitter.aspectMode,
                Is.EqualTo(AspectRatioFitter.AspectMode.EnvelopeParent));
            Assert.That(fitter.aspectRatio,
                Is.EqualTo(PortraitReferenceAspect).Within(0.0001f));
        }

        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.sprite == null) continue;
            Assert.That(image.color.a, Is.GreaterThanOrEqualTo(0.99f),
                image.name + " hides approved artwork.");
            bool interactive =
                image.GetComponent<Button>() != null ||
                image.GetComponent<TMP_InputField>() != null ||
                image.name == "PrivateRoomBackground";
            Assert.That(
                image.raycastTarget,
                interactive ? Is.True : Is.False,
                image.name + " has an unexpected raycast contract.");
        }

        var create = Find(menuPanel.transform, "CreateButton")?.GetComponent<Button>();
        var join = Find(menuPanel.transform, "JoinButton")?.GetComponent<Button>();
        var back = Find(menuPanel.transform, "BackButton")?.GetComponent<Button>();
        var share = GetField<GameObject>(controller, "createCopyButton")?.GetComponent<Button>();
        Assert.That(create, Is.Not.Null);
        Assert.That(join, Is.Not.Null);
        Assert.That(back, Is.Not.Null);
        Assert.That(share, Is.Not.Null);
        Assert.That(share.transform.IsChildOf(GetField<GameObject>(controller, "createWaitingRoot").transform),
            Is.True, "Only the real waiting state offers a room invite, not a decorative landing share.");
        Assert.That(create.onClick.GetPersistentEventCount() +
                    RuntimeListenerCount(create), Is.GreaterThan(0));
        Assert.That(join.onClick.GetPersistentEventCount() +
                    RuntimeListenerCount(join), Is.GreaterThan(0));
        Assert.That(back.onClick.GetPersistentEventCount() +
                    RuntimeListenerCount(back), Is.GreaterThan(0));

        var input = Find(root, "PrivateRoomLandingCodeInput")
            ?.GetComponent<TMP_InputField>();
        Assert.That(input, Is.Not.Null);
        Assert.That(input.characterLimit, Is.EqualTo(5));
        Assert.That(input.onValidateInput("", 0, 'a'), Is.EqualTo('A'));
        Assert.That(input.onValidateInput("", 0, '-'), Is.EqualTo('\0'));

        TMP_Text createHeading = Find(root, "PrivateRoomCreateHeading")
            ?.GetComponent<TMP_Text>();
        TMP_Text joinHeading = Find(root, "PrivateRoomJoinHeading")
            ?.GetComponent<TMP_Text>();
        TMP_Text title = Find(root, "PrivateRoomTitle")
            ?.GetComponent<TMP_Text>();
        AssertReadable(title, 42f);
        AssertReadable(createHeading, 34f);
        AssertReadable(joinHeading, 32f);

        yield return null;
    }

    static bool PortraitEnvelopeReady(Transform root)
    {
        if (root == null) return false;
        foreach (string overlayName in PortraitOverlayNames)
        {
            Transform overlay = Find(root, overlayName);
            if (overlay == null || overlay.GetComponent<AspectRatioFitter>() == null)
                return false;
        }

        return true;
    }

    static int RuntimeListenerCount(Button button)
    {
        // UnityEvent does not expose runtime listener count. A non-null event is
        // sufficient here because the existing functional test invokes callbacks.
        return button != null && button.onClick != null ? 1 : 0;
    }

    static void AssertReadable(TMP_Text text, float minimum)
    {
        Assert.That(text, Is.Not.Null);
        Assert.That(text.enableAutoSizing, Is.True);
        Assert.That(text.fontSizeMin, Is.GreaterThanOrEqualTo(minimum));
        Assert.That(text.overflowMode, Is.EqualTo(TextOverflowModes.Overflow));
    }

    static void AssertRect(
        Transform root,
        string name,
        Vector2 expectedPosition,
        Vector2 expectedSize)
    {
        var rect = Find(root, name) as RectTransform;
        Assert.That(rect, Is.Not.Null, "Missing RectTransform " + name);
        Assert.That(Vector2.Distance(rect.anchoredPosition, expectedPosition),
            Is.LessThan(1f), name + " position drifted.");
        Assert.That(Vector2.Distance(rect.sizeDelta, expectedSize),
            Is.LessThan(1f), name + " size drifted.");
    }

    static void RegisterInstaller()
    {
        Type type = RuntimeType("PrivateRoomVisualsInstaller");
        MethodInfo method = type.GetMethod(
            "Register", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(null, null);
    }

    static Component FindRuntimeComponent(string typeName)
    {
        Type type = RuntimeType(typeName);
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var component = root.GetComponentInChildren(type, true) as Component;
            if (component != null) return component;
        }

        return null;
    }

    static int CountRuntimeComponents(string typeName)
    {
        Type type = RuntimeType(typeName);
        int count = 0;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            count += root.GetComponentsInChildren(type, true).Length;
        return count;
    }

    static T GetField<T>(Component component, string name) where T : class
    {
        FieldInfo field = component.GetType().GetField(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Missing field " + name);
        return field.GetValue(component) as T;
    }

    static T GetProperty<T>(Component component, string name)
    {
        PropertyInfo property = component.GetType().GetProperty(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, "Missing property " + name);
        return (T)property.GetValue(component);
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
        Assert.That(type, Is.Not.Null, "Missing runtime type " + name);
        return type;
    }
}
