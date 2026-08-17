using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class AttachmentReskinPolishPlayModeTests
{
    [UnityTest]
    public IEnumerator ReferenceBoardReskinsExistingFlowsWithoutAddingFeatures()
    {
        var ownerType = RuntimeType("MainMenuAuthoritativeVisuals");
        var exactType = RuntimeType("ExactReferenceVisuals");
        var reskinType = RuntimeType("AttachmentReskinVisuals");
        var polishType = RuntimeType("AttachmentReskinPolish");
        var bindingsType = RuntimeType("AttachmentReskinCanvasBindings");

        InvokeInstaller(exactType);
        InvokeInstaller(reskinType);
        InvokeInstaller(polishType);
        InvokeInstaller(bindingsType);
        InvokeInstaller(ownerType);

        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        for (int i = 0; i < 12; i++)
            yield return null;
        yield return new WaitForSecondsRealtime(0.35f);

        var owner = Object.FindObjectOfType(ownerType) as Component;
        Assert.That(owner, Is.Not.Null,
            "The authoritative Home owner should be installed on MainMenu.");
        var canvas = owner.GetComponent<Canvas>();
        Assert.That(canvas, Is.Not.Null);
        Assert.That((bool)ownerType.GetProperty("IsReady").GetValue(owner, null), Is.True);

        var bindings = Object.FindObjectOfType(bindingsType) as Component;
        Assert.That(bindings, Is.Not.Null,
            "Runtime-injected main-menu controls should receive canvas-scoped reskin bindings.");
        Assert.That(bindings.GetComponent<Canvas>(), Is.SameAs(canvas));
        Assert.That(((Behaviour)bindings).enabled, Is.False,
            "The attachment layer must yield Home ownership while MainMenuRoot is active.");

        var baseline = Object.FindObjectOfType(exactType) as Behaviour;
        Assert.That(baseline, Is.Not.Null);
        Assert.That(baseline.enabled, Is.False,
            "The superseded exact pass must not compete with the authoritative Home.");

        string[] resources =
        {
            "reference/board_vs_burst_exact",
            "reference/board_trophy_exact",
            "reference/board_rocket_exact",
            "reference/board_friend_exact",
            "reference/board_lightning_exact",
            "reference/board_plus_exact",
            "reference/board_join_exact"
        };
        foreach (string resource in resources)
            Assert.That(Resources.Load<Sprite>(resource), Is.Not.Null,
                "Missing reference-board sprite: " + resource);

        Assert.That(Find(canvas.transform, "HomeLogo"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "HomeTipFrame"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "HomePrivateIcon"), Is.Not.Null,
            "The real runtime-injected PvP button should receive the friend artwork.");
        Assert.That(Find(canvas.transform, "HomeDailyIcon"), Is.Not.Null,
            "The real runtime-injected Daily Hunt button should receive the lightning artwork.");

        var exactLogo = Find(canvas.transform, "ExactHOLLogo");
        if (exactLogo != null)
            Assert.That(exactLogo.gameObject.activeSelf, Is.False,
                "The earlier logo decoration must not sit under the board logo.");

        var menu = Object.FindObjectOfType(RuntimeType("MenuManager")) as Component;
        Assert.That(menu, Is.Not.Null);
        var mainMenuField = menu.GetType().GetField(
            "mainMenuPanel", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(mainMenuField, Is.Not.Null);
        var mainMenuPanel = mainMenuField.GetValue(menu) as GameObject;
        Assert.That(mainMenuPanel, Is.Not.Null);

        // Open the existing PvP menu. This is the real controller flow, not a
        // test-only or reskin-created screen. PvP owns a separate runtime Canvas,
        // so assertions below are scoped to the controller's real menu panel.
        var pvp = Object.FindObjectOfType(RuntimeType("PvpGameController")) as Component;
        Assert.That(pvp, Is.Not.Null);
        pvp.SendMessage("OpenPvpMenu", SendMessageOptions.RequireReceiver);
        Assert.That(mainMenuPanel.activeInHierarchy, Is.True,
            "Production PvP overlay must leave Home active.");
        yield return new WaitForSecondsRealtime(0.35f);

        var pvpMenuField = pvp.GetType().GetField("pvpMenuPanel", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(pvpMenuField, Is.Not.Null);
        var pvpMenuPanel = pvpMenuField.GetValue(pvp) as GameObject;
        Assert.That(pvpMenuPanel, Is.Not.Null);
        Assert.That(Find(pvpMenuPanel.transform, "BoardCreatePlusVector"), Is.Not.Null);
        Assert.That(Find(pvpMenuPanel.transform, "BoardJoinDoorVector"), Is.Not.Null);
        pvp.SendMessage("ClosePvpMenu", SendMessageOptions.RequireReceiver);
        yield return new WaitForSecondsRealtime(0.35f);

        // Exercise the existing solo-search flow rather than activating the
        // nested searching child in isolation. PanelSearching lives under
        // PanelPlay, so MenuManager.OnPlayPressed must open its parent first.
        menu.SendMessage("OnPlayPressed", SendMessageOptions.RequireReceiver);
        yield return null;

        var matchmaking = Object.FindObjectOfType(RuntimeType("FakeMatchmaking")) as Component;
        Assert.That(matchmaking, Is.Not.Null);
        matchmaking.SendMessage("StartSearch", SendMessageOptions.RequireReceiver);
        yield return new WaitForSecondsRealtime(0.35f);

        var searchingField = menu.GetType().GetField("panelSearching", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(searchingField, Is.Not.Null);
        var searchingPanel = searchingField.GetValue(menu) as GameObject;
        Assert.That(searchingPanel, Is.Not.Null);
        Assert.That(searchingPanel.activeInHierarchy, Is.True,
            "The real StartSearch flow should make PanelSearching visible.");

        Assert.That(Find(searchingPanel.transform, "BoardSearchRocketVector"), Is.Not.Null);
        Assert.That(Find(searchingPanel.transform, "BoardVsBurstVector"), Is.Not.Null);
        matchmaking.SendMessage("CancelSearch", SendMessageOptions.RequireReceiver);

        // Presentation-only contract: every Button must still be a controller/
        // scene button. Board-prefixed objects are decoration and text only.
        foreach (var button in canvas.GetComponentsInChildren<Button>(true))
            Assert.That(button.name.StartsWith("Board"), Is.False,
                "The reskin must not invent an interactive control: " + button.name);

        foreach (var button in pvpMenuPanel.GetComponentsInChildren<Button>(true))
            Assert.That(button.name.StartsWith("Board"), Is.False,
                "The PvP reskin must not invent an interactive control: " + button.name);

        Assert.That(Find(canvas.transform, "BoardStorePanel"), Is.Null,
            "Store is reference-only and must not be added by a reskin.");
        Assert.That(Find(canvas.transform, "BoardProfilePanel"), Is.Null,
            "Profile is reference-only and must not be added by a reskin.");
    }

    static void InvokeInstaller(System.Type type)
    {
        var install = type.GetMethod("Install", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(install, Is.Not.Null, "Missing runtime installer on " + type.Name);
        install.Invoke(null, null);
    }

    static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = Find(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    static System.Type RuntimeType(string name)
    {
        var type = System.Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "Missing runtime component: " + name);
        return type;
    }
}
