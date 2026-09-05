using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class MainMenuPlayVisualsPlayModeTests
{
    const BindingFlags StaticFlags =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    [UnityTearDown]
    public IEnumerator RestoreFixtureState()
    {
        Scene active = SceneManager.GetActiveScene();
        Scene quiescent = SceneManager.CreateScene(
            "MainMenuPlayVisualsQuiescent");
        SceneManager.SetActiveScene(quiescent);
        if (active.IsValid() && active.isLoaded &&
            active.handle != quiescent.handle)
            yield return SceneManager.UnloadSceneAsync(active);
        yield return null;
#if UNITY_EDITOR
        FirstLaunchSoloEndToEndPlayModeTests
            .RestoreEditorWindowAfterSettlement();
#endif
    }

    [UnityTest]
    public IEnumerator SoloAiEntryIsImmediateWhilePrivateRoomRemainsSeparate()
    {
#if UNITY_EDITOR
        FirstLaunchSoloEndToEndPlayModeTests
            .FocusGameViewForEndOfFrameSettlement();
#endif
        InvokeInstaller("MainMenuHomeVisuals");
        InvokeInstaller("MainMenuPlayVisuals");
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        for (int i = 0; i < 16; i++)
            yield return null;
        yield return new WaitForSecondsRealtime(0.35f);

        var menu = Object.FindObjectOfType(RuntimeType("MenuManager")) as Component;
        Assert.That(menu, Is.Not.Null);
        var panelPlay = menu.GetType().GetField("panelPlay").GetValue(menu) as GameObject;
        var mainMenuPanel = menu.GetType().GetField("mainMenuPanel").GetValue(menu) as GameObject;
        var searching = menu.GetType().GetField("panelSearching").GetValue(menu) as GameObject;
        Assert.That(panelPlay, Is.Not.Null);
        Assert.That(mainMenuPanel, Is.Not.Null);
        Assert.That(searching, Is.Not.Null);
        Assert.That(CountInScene(RuntimeType("SoloSearchVisuals")), Is.Zero,
            "The retired Solo Search owner must be absent from normal MainMenu startup.");
        Assert.That(Find(searching.transform, "SoloSearchVisualRoot"), Is.Null);
        Assert.That(CountNamedButtons(searching.transform, "CancelButton"), Is.Zero);
        Assert.That(CountNamedButtons(searching.transform, "SearchBackButton"), Is.Zero);

        // PanelPlay remains available only as an isolated compatibility/capture
        // seam. Production PLAY SOLO bypasses it completely.
        mainMenuPanel.SetActive(false);
        panelPlay.SetActive(true);
        for (int i = 0; i < 8; i++)
            yield return null;
        yield return new WaitForSecondsRealtime(0.35f);

        var ownerType = RuntimeType("MainMenuPlayVisuals");
        var owner = Object.FindObjectOfType(ownerType) as Component;
        Assert.That(owner, Is.Not.Null);
        Assert.That((bool)ownerType.GetProperty("IsReady").GetValue(owner, null), Is.True);
        Assert.That((bool)ownerType.GetProperty("IsSettled").GetValue(owner, null), Is.True);

        var canvas = owner.GetComponent<Canvas>();
        Assert.That(canvas, Is.Not.Null);
        Assert.That(Find(canvas.transform, "PlayVisualRoot"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "PlayLogo"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "PlayHeroBoy"), Is.Null);
        Assert.That(Find(canvas.transform, "PlayMascotSix"), Is.Null);
        Assert.That(Find(canvas.transform, "HomeVisualRoot").gameObject.activeSelf, Is.False);

        var playRoot = Find(canvas.transform, "PlayVisualRoot");
        Assert.That(playRoot.gameObject.activeSelf, Is.True);

        var logo = Find(canvas.transform, "PlayLogo").GetComponent<Image>();
        Assert.That(logo.preserveAspect, Is.True);
        Assert.That(logo.raycastTarget, Is.False);

        foreach (var button in canvas.GetComponentsInChildren<Button>(true))
            Assert.That(button.name.StartsWith("Play"), Is.False,
                "Play owner must not invent a Button: " + button.name);

        var back = Find(canvas.transform, "ButtonBack").GetComponent<Button>();
        var find = Find(canvas.transform, "ButtonChallenger").GetComponent<Button>();
        Assert.That(find.GetComponent<Image>().sprite.name, Does.Contain("gold"));
        Assert.That(back.GetComponent<Image>().sprite.name, Does.Contain("blue"));

        Assert.That(panelPlay.activeSelf, Is.True);
        Assert.That(searching.activeSelf, Is.False);

        var exactLogo = Find(canvas.transform, "ExactPlayLogo");
        if (exactLogo != null)
            Assert.That(exactLogo.gameObject.activeSelf, Is.False);

        var tmpTextType = System.Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
        Assert.That(tmpTextType, Is.Not.Null);
        var disclosure = Find(panelPlay.transform, "DisclosureLabel");
        Assert.That(disclosure, Is.Not.Null);
        Component disclosureText = null;
        foreach (var component in disclosure.GetComponentsInChildren(tmpTextType, true))
        {
            disclosureText = component;
            break;
        }
        Assert.That(disclosureText, Is.Not.Null);
        string copy = (string)tmpTextType.GetProperty("text").GetValue(disclosureText, null);
        string expected = LocalizedCopy("simulated_opponents");
        Assert.That(copy, Does.Contain(expected));
        Assert.That(find.GetComponentInChildren<TMP_Text>(true).text,
            Is.EqualTo(LocalizedCopy("find_challenger")));
        Assert.That(find.GetComponentInChildren<TMP_Text>(true).text,
            Does.Contain("AI").IgnoreCase);

        var card = Find(canvas.transform, "PlayDisclosure");
        Assert.That(card, Is.Not.Null);
        Assert.That(card.GetComponent<Image>().raycastTarget, Is.False);
        Assert.That(card.GetComponent<Outline>(), Is.Null);

        var paths = (string[])ownerType.GetField("LoadedResources", StaticFlags)
            .GetValue(null);
        foreach (var path in paths)
            Assert.That(path.StartsWith("splash/"), Is.False, path);

        // Back before entering the local duel must return home cleanly. Re-entry
        // must not leave a deferred callback capable of reopening the board.
        back.onClick.Invoke();
        yield return null;
        Assert.That(panelPlay.activeSelf, Is.False);
        Assert.That(searching.activeSelf, Is.False);
        Assert.That(Find(canvas.transform, "HomeVisualRoot").gameObject.activeSelf, Is.True);

        // The PvP Duel Home entry remains routed to the real room-based online
        // flow and still exposes the existing Create and Join actions.
        var pvpEntry = Find(canvas.transform, "ButtonPvP").GetComponent<Button>();
        Assert.That(pvpEntry.GetComponentInChildren<TMP_Text>(true).text,
            Is.EqualTo(LocalizedCopy("pvp_duel")));
        pvpEntry.onClick.Invoke();
        yield return null;
        var pvp = Object.FindObjectOfType(RuntimeType("PvpGameController")) as Component;
        Assert.That(pvp, Is.Not.Null);
        var pvpMenu = pvp.GetType().GetField("pvpMenuPanel").GetValue(pvp) as GameObject;
        Assert.That(pvpMenu, Is.Not.Null);
        Assert.That(pvpMenu.activeSelf, Is.True);
        Assert.That(Find(pvpMenu.transform, "CreateButton").GetComponent<Button>().interactable,
            Is.True);
        Assert.That(Find(pvpMenu.transform, "JoinButton").GetComponent<Button>().interactable,
            Is.True);
        pvp.SendMessage("ClosePvpMenu", SendMessageOptions.RequireReceiver);
        yield return null;
        Assert.That(pvpMenu.activeSelf, Is.False);

        Transform soloEntryTransform = Find(canvas.transform, "ButtonPlay");
        Assert.That(soloEntryTransform, Is.Not.Null,
            "The direct-entry proof must find the real Home Solo button.");
        Button soloEntry = soloEntryTransform.GetComponent<Button>();
        Assert.That(soloEntry, Is.Not.Null);
        soloEntry.onClick.Invoke();
        yield return null;
        Assert.That(panelPlay.activeSelf, Is.False,
            "Production Solo entry must not expose the retired PanelPlay screen.");

        var matchmaking = Object.FindObjectOfType(RuntimeType("FakeMatchmaking")) as Component;
        Assert.That(matchmaking, Is.Not.Null);
        var panelGame = matchmaking.GetType().GetField("panelGame").GetValue(matchmaking) as GameObject;
        Assert.That(panelGame, Is.Not.Null);
        Assert.That(mainMenuPanel.activeSelf, Is.False);
        Assert.That(panelGame.activeSelf, Is.True,
            "The real Solo board must activate in the same Home CTA call.");
        Assert.That(searching.activeSelf, Is.False,
            "Production direct entry cannot expose the retired search screen.");
        Assert.That(CountInScene(RuntimeType("SoloSearchVisuals")), Is.Zero);
        Assert.That(Find(searching.transform, "SoloSearchVisualRoot"), Is.Null);
        Assert.That(CountNamedButtons(searching.transform, "CancelButton"), Is.Zero);
        Assert.That(CountNamedButtons(searching.transform, "SearchBackButton"), Is.Zero);
        Assert.That(IsPreparing(matchmaking), Is.True);
        for (int frame = 0; frame < 120 && IsPreparing(matchmaking); frame++)
            yield return null;
        Assert.That(IsPreparing(matchmaking), Is.False,
            "Solo preparation did not finish after the real board became ready.");
        Assert.That(searching.activeSelf, Is.False);
        Assert.That(panelGame.activeSelf, Is.True);
    }

    static bool IsPreparing(Component matchmaking)
    {
        var property = matchmaking.GetType().GetProperty("IsPreparing");
        Assert.That(property, Is.Not.Null);
        return (bool)property.GetValue(matchmaking, null);
    }

    static string LocalizedCopy(string key)
    {
        var l10n = RuntimeType("L10n");
        foreach (var method in l10n.GetMethods(StaticFlags))
        {
            if (method.Name != "Get" || method.ReturnType != typeof(string)) continue;
            var parameters = method.GetParameters();
            if (parameters.Length == 0 || parameters[0].ParameterType != typeof(string))
                continue;
            var arguments = new object[parameters.Length];
            arguments[0] = key;
            for (int i = 1; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType == typeof(object[]))
                    arguments[i] = new object[0];
                else if (parameters[i].HasDefaultValue)
                    arguments[i] = parameters[i].DefaultValue;
            }
            return (string)method.Invoke(null, arguments);
        }
        Assert.Fail("Missing L10n.Get");
        return null;
    }

    static void InvokeInstaller(string typeName)
    {
        var type = RuntimeType(typeName);
        var install = type.GetMethod("Install", StaticFlags);
        Assert.That(install, Is.Not.Null);
        install.Invoke(null, null);
    }

    static int CountInScene(System.Type type)
    {
        int count = 0;
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            count += root.GetComponentsInChildren(type, true).Length;
        return count;
    }

    static int CountNamedButtons(Transform root, string name)
    {
        int count = 0;
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            if (button.name == name)
                count++;
        }
        return count;
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
