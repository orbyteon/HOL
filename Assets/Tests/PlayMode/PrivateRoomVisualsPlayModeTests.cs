using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class PrivateRoomVisualsPlayModeTests
{
    [UnityTest]
    public IEnumerator PrivateRoomUsesOneProductionOwnerAndPreservesCreateJoinFlows()
    {
        Screen.SetResolution(1080, 1920, false);
        yield return null;

        RegisterInstaller();
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);

        Component controller = null;
        Component visuals = null;
        for (int frame = 0; frame < 120; frame++)
        {
            controller = FindRuntimeComponent("PvpGameController");
            visuals = FindRuntimeComponent("PrivateRoomVisuals");
            if (controller != null && visuals != null)
                break;
            yield return null;
        }

        Assert.That(controller, Is.Not.Null,
            "The real PvpGameController must exist in MainMenu.");
        Assert.That(visuals, Is.Not.Null,
            "PrivateRoomVisuals must attach after the runtime PvP controller exists.");
        Assert.That(CountRuntimeComponents("PrivateRoomVisuals"), Is.EqualTo(1),
            "Private Room must have exactly one production presentation owner.");

        controller.SendMessage("OpenPvpMenu", SendMessageOptions.RequireReceiver);
        yield return null;
        yield return null;

        var menuPanel = GetField<GameObject>(controller, "pvpMenuPanel");
        Assert.That(menuPanel, Is.Not.Null);
        Assert.That(menuPanel.activeInHierarchy, Is.True);

        Transform visualRoot = Find(menuPanel.transform, "PrivateRoomVisualRoot");
        Assert.That(visualRoot, Is.Not.Null,
            "Approved Private Room visual root was not built.");
        Assert.That(visualRoot.gameObject.activeInHierarchy, Is.True);

        yield return CapturePrivateRoomScreenshot();

        string[] requiredObjects =
        {
            "PrivateRoomBackground",
            "PrivateRoomPlayerChip",
            "PrivateRoomLogo",
            "PrivateRoomTitleRibbon",
            "PrivateRoomCreateCard",
            "PrivateRoomCreateBoy",
            "PrivateRoomCreateGirl",
            "PrivateRoomJoinCard",
            "PrivateRoomJoinDoor",
            "PrivateRoomLandingCodeInput",
            "PrivateRoomShareButton",
            "PrivateRoomTipCard",
            "PrivateRoomMascotSix",
            "PrivateRoomMascotSeven"
        };
        foreach (string objectName in requiredObjects)
            Assert.That(Find(visualRoot, objectName), Is.Not.Null,
                "Missing approved Private Room object: " + objectName);

        // Every visible sprite-bearing production control must render the actual
        // sprite at full normal-state alpha. The translucent outer cabinet frame
        // is an additive decorative overlay and is intentionally excluded.
        foreach (var image in visualRoot.GetComponentsInChildren<Image>(true))
        {
            if (image.sprite == null || image.name == "PrivateRoomOuterFrame")
                continue;
            Assert.That(image.color.a, Is.GreaterThanOrEqualTo(0.99f),
                image.name + " hides/fades its approved production sprite.");
        }

        // No custom procedural Graphic is permitted under the approved landing
        // screen. Images render approved art; TMP renders localized/dynamic copy.
        // TMP_SubMeshUI is allowed only as a direct generated child of TMP text.
        foreach (var graphic in visualRoot.GetComponentsInChildren<Graphic>(true))
        {
            Assert.That(IsAllowedProductionGraphic(graphic), Is.True,
                "Procedural Graphic found in Private Room: " +
                graphic.GetType().Name + " on " + graphic.name);
        }

        var createButton = Find(menuPanel.transform, "CreateButton")
            ?.GetComponent<Button>();
        var joinButton = Find(menuPanel.transform, "JoinButton")
            ?.GetComponent<Button>();
        Assert.That(createButton, Is.Not.Null);
        Assert.That(joinButton, Is.Not.Null);
        Assert.That(IsDescendantOf(createButton.transform, visualRoot), Is.True,
            "The real Create button must be seated inside the production visual root.");
        Assert.That(IsDescendantOf(joinButton.transform, visualRoot), Is.True,
            "The real Join button must be seated inside the production visual root.");

        // Exercise the existing Create navigation callback. This proves the
        // visual owner reused the real controller-owned control rather than a
        // disconnected visual clone.
        var createPanel = GetField<GameObject>(controller, "createPanel");
        Assert.That(createPanel, Is.Not.Null);
        createButton.onClick.Invoke();
        yield return null;
        Assert.That(createPanel.activeSelf, Is.True,
            "Create callback was lost during Private Room restyling.");

        // Reopen the landing screen and exercise Join with the new landing code
        // field. The real Join panel remains the destination because it owns the
        // secret-number step and server-authoritative join operation.
        controller.SendMessage("OpenPvpMenu", SendMessageOptions.RequireReceiver);
        yield return null;
        var landingInput = Find(menuPanel.transform, "PrivateRoomLandingCodeInput")
            ?.GetComponent<TMP_InputField>();
        Assert.That(landingInput, Is.Not.Null);
        Assert.That(landingInput.characterLimit, Is.EqualTo(5));
        Assert.That(landingInput.onValidateInput("", 0, 'a'), Is.EqualTo('A'),
            "Room-code validation must normalize letters to uppercase.");
        landingInput.SetTextWithoutNotify("ab12c");

        var joinPanel = GetField<GameObject>(controller, "joinPanel");
        var joinCodeInput = GetField<TMP_InputField>(controller, "joinCodeInput");
        Assert.That(joinPanel, Is.Not.Null);
        Assert.That(joinCodeInput, Is.Not.Null);
        joinButton.onClick.Invoke();
        yield return null;
        Assert.That(joinPanel.activeSelf, Is.True,
            "Join callback was lost during Private Room restyling.");
        Assert.That(joinCodeInput.text, Is.EqualTo("AB12C"),
            "Landing room code must transfer into the real Join flow.");

        // Retired decoration roots must never return underneath the current
        // Private Room screen.
        foreach (var t in menuPanel.GetComponentsInChildren<Transform>(true))
        {
            Assert.That(t.name.StartsWith("Exact", StringComparison.Ordinal), Is.False,
                "Retired Exact visual returned: " + t.name);
            Assert.That(t.name.StartsWith("Attachment", StringComparison.Ordinal), Is.False,
                "Retired Attachment reskin returned: " + t.name);
            Assert.That(t.name, Is.Not.EqualTo("BackdropNumbers"),
                "Retired drifting-number backdrop returned.");
        }
    }

    static bool IsAllowedProductionGraphic(Graphic graphic)
    {
        if (graphic is Image || graphic is TMP_Text)
            return true;

        var subMesh = graphic as TMP_SubMeshUI;
        return subMesh != null &&
               subMesh.transform.parent != null &&
               subMesh.transform.parent.GetComponent<TMP_Text>() != null;
    }

    static IEnumerator CapturePrivateRoomScreenshot()
    {
        yield return new WaitForEndOfFrame();

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string artifactDirectory = Path.Combine(projectRoot, "artifacts", "private-room-render");
        Directory.CreateDirectory(artifactDirectory);
        string path = Path.Combine(artifactDirectory, "private-room-1080x1920.png");

        if (File.Exists(path))
            File.Delete(path);

        ScreenCapture.CaptureScreenshot(path);

        for (int frame = 0; frame < 120 && !File.Exists(path); frame++)
            yield return null;

        Assert.That(File.Exists(path), Is.True,
            "Private Room screenshot was not written to the PlayMode artifact directory.");
        var info = new FileInfo(path);
        Assert.That(info.Length, Is.GreaterThan(1024),
            "Private Room screenshot artifact is unexpectedly empty.");
    }

    static void RegisterInstaller()
    {
        Type type = RuntimeType("PrivateRoomVisualsInstaller");
        Assert.That(type, Is.Not.Null);
        MethodInfo method = type.GetMethod("Register",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(null, null);
    }

    static Component FindRuntimeComponent(string typeName)
    {
        Type type = RuntimeType(typeName);
        if (type == null) return null;
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
        if (type == null) return 0;
        int count = 0;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            count += root.GetComponentsInChildren(type, true).Length;
        return count;
    }

    static T GetField<T>(Component component, string name) where T : class
    {
        FieldInfo field = component.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Missing controller field: " + name);
        return field.GetValue(component) as T;
    }

    static bool IsDescendantOf(Transform child, Transform ancestor)
    {
        while (child != null)
        {
            if (child == ancestor) return true;
            child = child.parent;
        }
        return false;
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
        return Type.GetType(name + ", Assembly-CSharp");
    }
}
