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

    [UnityTest]
    public IEnumerator SoloAiEntryIsImmediateWhilePrivateRoomRemainsSeparate()
    {
        InvokeInstaller("MainMenuHomeVisuals");
        InvokeInstaller("MainMenuPlayVisuals");
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        for (int i = 0; i < 16; i++)
            yield return null;
        yield return new WaitForSecondsRealtime(0.35f);

        var menu = Object.FindObjectOfType(RuntimeType("MenuManager")) as Component;
        Assert.That(menu, Is.Not.Null);
        menu.SendMessage("OnPlayPressed", SendMessageOptions.RequireReceiver);
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

        var panelPlay = menu.GetType().GetField("panelPlay").GetValue(menu) as GameObject;
        Assert.That(panelPlay.activeSelf, Is.True);
        var searching = menu.GetType().GetField("panelSearching").GetValue(menu) as GameObject;
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

        menu.SendMessage("OnPlayPressed", SendMessageOptions.RequireReceiver);
        yield return null;
        Assert.That(panelPlay.activeSelf, Is.True);

        var matchmaking = Object.FindObjectOfType(RuntimeType("FakeMatchmaking")) as Component;
        Assert.That(matchmaking, Is.Not.Null);
        var panelGame = matchmaking.GetType().GetField("panelGame").GetValue(matchmaking) as GameObject;
        Assert.That(panelGame, Is.Not.Null);
        Assert.That(panelGame.activeSelf, Is.False);

        // Same-call assertions are intentional: a coroutine-based fake search
        // cannot satisfy this deterministic local-AI entry contract.
        matchmaking.SendMessage("StartSearch", SendMessageOptions.RequireReceiver);
        Assert.That(searching.activeSelf, Is.False);
        Assert.That(panelGame.activeSelf, Is.True);
        for (int i = 0; i < 12; i++)
            yield return null;
        Assert.That(searching.activeSelf, Is.False);
        Assert.That(panelGame.activeSelf, Is.True);

        // Verify the actual runtime lifecycle rather than assuming EditMode
        // invokes MonoBehaviour disable callbacks.
        panelGame.SetActive(false);
        searching.SetActive(true);
        ((Behaviour)matchmaking).enabled = false;
        Assert.That(searching.activeSelf, Is.False);
        Assert.That(panelGame.activeSelf, Is.False);
        ((Behaviour)matchmaking).enabled = true;
        matchmaking.SendMessage("StartSearch", SendMessageOptions.RequireReceiver);
        Assert.That(searching.activeSelf, Is.False);
        Assert.That(panelGame.activeSelf, Is.True);
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
