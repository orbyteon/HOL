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
        var exactType = RuntimeType("ExactReferenceVisuals");
        var reskinType = RuntimeType("AttachmentReskinVisuals");
        var polishType = RuntimeType("AttachmentReskinPolish");
        var bindingsType = RuntimeType("AttachmentReskinCanvasBindings");

        InvokeInstaller(exactType);
        InvokeInstaller(reskinType);
        InvokeInstaller(polishType);
        InvokeInstaller(bindingsType);

        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        for (int i = 0; i < 12; i++)
            yield return null;
        yield return new WaitForSecondsRealtime(0.35f);

        var polish = Object.FindObjectOfType(polishType) as Component;
        Assert.That(polish, Is.Not.Null,
            "The reference-board polish should be installed on MainMenu.");
        var canvas = polish.GetComponent<Canvas>();
        Assert.That(canvas, Is.Not.Null);

        var bindings = Object.FindObjectOfType(bindingsType) as Component;
        Assert.That(bindings, Is.Not.Null,
            "Runtime-injected main-menu controls should receive canvas-scoped reskin bindings.");
        Assert.That(bindings.GetComponent<Canvas>(), Is.SameAs(canvas));

        var baseline = Object.FindObjectOfType(exactType) as Behaviour;
        Assert.That(baseline, Is.Not.Null);
        Assert.That(baseline.enabled, Is.False,
            "Only the board reskin should own MainMenu presentation after bootstrap.");

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

        Assert.That(Find(canvas.transform, "BoardHomeLogo"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "BoardHomeTipCard"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "BoardFriendVector"), Is.Not.Null,
            "The real runtime-injected PvP button should receive the friend artwork.");
        Assert.That(Find(canvas.transform, "BoardDailyVector"), Is.Not.Null,
            "The real runtime-injected Daily Hunt button should receive the lightning artwork.");

        var exactLogo = Find(canvas.transform, "ExactHOLLogo");
        if (exactLogo != null)
            Assert.That(exactLogo.gameObject.activeSelf, Is.False,
                "The earlier logo decoration must not sit under the board logo.");

        // Open the existing PvP menu. This is the real controller flow, not a
        // test-only or reskin-created screen.
        var pvp = Object.FindObjectOfType(RuntimeType("PvpGameController")) as Component;
        Assert.That(pvp, Is.Not.Null);
        pvp.SendMessage("OpenPvpMenu", SendMessageOptions.RequireReceiver);
        yield return new WaitForSecondsRealtime(0.35f);

        Assert.That(Find(canvas.transform, "BoardCreatePlusVector"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "BoardJoinDoorVector"), Is.Not.Null);

        // Activate the existing searching panel so the reference rocket/VS
        // treatment is exercised without inventing a new pre-match state.
        var menu = Object.FindObjectOfType(RuntimeType("MenuManager")) as Component;
        Assert.That(menu, Is.Not.Null);
        var searchingField = menu.GetType().GetField("panelSearching", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(searchingField, Is.Not.Null);
        var searchingPanel = searchingField.GetValue(menu) as GameObject;
        Assert.That(searchingPanel, Is.Not.Null);
        searchingPanel.SetActive(true);
        yield return new WaitForSecondsRealtime(0.35f);

        Assert.That(Find(searchingPanel.transform, "BoardSearchRocketVector"), Is.Not.Null);
        Assert.That(Find(searchingPanel.transform, "BoardVsBurstVector"), Is.Not.Null);

        // Presentation-only contract: every Button must still be a controller/
        // scene button. Board-prefixed objects are decoration and text only.
        foreach (var button in canvas.GetComponentsInChildren<Button>(true))
            Assert.That(button.name.StartsWith("Board"), Is.False,
                "The reskin must not invent an interactive control: " + button.name);

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
