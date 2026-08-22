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
        InvokeInstaller(RuntimeType("MainMenuHomeVisuals"));

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

        Assert.That(Find(canvas.transform, "HomeLogo"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "HomeTipCard"), Is.Not.Null);
        var homeTip = Find(canvas.transform, "HomeTipCard").GetComponent<Image>();
        Assert.That(homeTip.sprite, Is.Not.Null);
        Assert.That(homeTip.sprite.name, Does.Contain("tip_frame"),
            "Polish must not restyle Home chrome owned by MainMenuHomeVisuals.");
        Assert.That(homeTip.GetComponent<Outline>(), Is.Null);
        var referenceIconType = RuntimeType("MainMenuReferenceIconGraphic");
        var privateIcon = Find(canvas.transform, "HomePrivateIcon");
        var dailyIcon = Find(canvas.transform, "HomeDailyIcon");
        Assert.That(privateIcon, Is.Not.Null,
            "The Home composition requires a private-room symbol.");
        Assert.That(dailyIcon, Is.Not.Null,
            "The Home composition requires a Daily Hunt symbol.");
        Assert.That(privateIcon.GetComponent(referenceIconType), Is.Not.Null);
        Assert.That(dailyIcon.GetComponent(referenceIconType), Is.Not.Null);
        Assert.That(privateIcon.GetComponent<Image>(), Is.Null,
            "The reference people symbol must not regress to the padded sticker PNG.");
        Assert.That(dailyIcon.GetComponent<Image>(), Is.Null,
            "The reference lightning symbol must not regress to the padded sticker PNG.");
        Canvas.ForceUpdateCanvases();
        Assert.That((privateIcon.GetComponent(referenceIconType) as Graphic)
            .canvasRenderer.GetMesh().vertexCount, Is.GreaterThan(40));
        Assert.That((dailyIcon.GetComponent(referenceIconType) as Graphic)
            .canvasRenderer.GetMesh().vertexCount, Is.GreaterThan(7));
        Assert.That(Find(canvas.transform, "HomePrivateTitle"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "HomeDailyTitle"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "BoardHomeLogo"), Is.Null);

        var exactLogo = Find(canvas.transform, "ExactHOLLogo");
        if (exactLogo != null)
            Assert.That(exactLogo.gameObject.activeSelf, Is.False,
                "The earlier logo decoration must not sit under the board logo.");

        // Open the existing PvP menu. This is the real controller flow, not a
        // test-only or reskin-created screen. PvP owns a separate runtime Canvas,
        // so assertions below are scoped to the controller's real menu panel.
        var pvp = Object.FindObjectOfType(RuntimeType("PvpGameController")) as Component;
        Assert.That(pvp, Is.Not.Null);
        pvp.SendMessage("OpenPvpMenu", SendMessageOptions.RequireReceiver);
        yield return new WaitForSecondsRealtime(0.35f);

        var pvpMenuField = pvp.GetType().GetField("pvpMenuPanel", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(pvpMenuField, Is.Not.Null);
        var pvpMenuPanel = pvpMenuField.GetValue(pvp) as GameObject;
        Assert.That(pvpMenuPanel, Is.Not.Null);
        Assert.That(Find(pvpMenuPanel.transform, "TitleRibbon"), Is.Not.Null);
        Assert.That(Find(pvpMenuPanel.transform, "FriendArt"), Is.Not.Null);
        Assert.That(Find(pvpMenuPanel.transform, "GirlArt"), Is.Not.Null);
        Assert.That(Find(pvpMenuPanel.transform, "DoorArt"), Is.Not.Null);
        Assert.That(Find(pvpMenuPanel.transform, "BoardCreatePlusVector"), Is.Null);
        Assert.That(Find(pvpMenuPanel.transform, "BoardJoinDoorVector"), Is.Null);

        // Exercise the real Solo entry flow rather than activating any nested
        // child in isolation. Solo now enters the local AI board directly.
        var menu = Object.FindObjectOfType(RuntimeType("MenuManager")) as Component;
        Assert.That(menu, Is.Not.Null);

        menu.SendMessage("OpenSettings", SendMessageOptions.RequireReceiver);
        yield return new WaitForSecondsRealtime(0.35f);
        var settingsField = menu.GetType().GetField("settingsPanel",
            BindingFlags.Instance | BindingFlags.Public);
        var settings = settingsField.GetValue(menu) as GameObject;
        Assert.That(settings, Is.Not.Null);
        Assert.That(Find(settings.transform, "SettingsVisualRoot"), Is.Not.Null);
        var nameInput = Find(settings.transform, "InputField (TMP)") as RectTransform;
        var save = Find(settings.transform, "Buttonsave") as RectTransform;
        Assert.That(Overlaps(nameInput, save), Is.False,
            "Name input and Save must not overlap.");
        var toggle = Find(settings.transform, "Toggle") as RectTransform;
        Assert.That(toggle.localScale, Is.EqualTo(Vector3.one));
        var back = Find(settings.transform, "Buttonback") as RectTransform;
        Assert.That(back.sizeDelta.x, Is.InRange(112f, 140f),
            "The Settings Back action must keep the approved prominent touch target.");
        for (int i = 0; i < 3; i++)
        {
            var current = Find(settings.transform, "Difficulty" + i)
                as RectTransform;
            var next = Find(settings.transform, "Difficulty" + (i + 1))
                as RectTransform;
            Assert.That(Overlaps(current, next), Is.False,
                "Difficulty choices must not overlap.");
        }
        menu.SendMessage("BackToMenu", SendMessageOptions.RequireReceiver);
        yield return null;

        menu.SendMessage("OnPlayPressed", SendMessageOptions.RequireReceiver);
        yield return null;

        var matchmaking = Object.FindObjectOfType(RuntimeType("FakeMatchmaking")) as Component;
        Assert.That(matchmaking, Is.Not.Null);
        matchmaking.SendMessage("StartSearch", SendMessageOptions.RequireReceiver);

        var searchingField = menu.GetType().GetField("panelSearching", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(searchingField, Is.Not.Null);
        var searchingPanel = searchingField.GetValue(menu) as GameObject;
        Assert.That(searchingPanel, Is.Not.Null);
        Assert.That(searchingPanel.activeInHierarchy, Is.False,
            "Local Solo entry must never expose simulated matchmaking.");
        var gameField = matchmaking.GetType().GetField("panelGame",
            BindingFlags.Instance | BindingFlags.Public);
        var gamePanel = gameField.GetValue(matchmaking) as GameObject;
        Assert.That(gamePanel, Is.Not.Null);
        Assert.That(gamePanel.activeSelf, Is.True,
            "Local Solo entry must activate the existing AI board immediately.");

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

    static bool Overlaps(RectTransform a, RectTransform b)
    {
        Assert.That(a, Is.Not.Null);
        Assert.That(b, Is.Not.Null);
        return Mathf.Abs(a.anchoredPosition.x - b.anchoredPosition.x) <
                   (a.sizeDelta.x + b.sizeDelta.x) * 0.5f &&
               Mathf.Abs(a.anchoredPosition.y - b.anchoredPosition.y) <
                   (a.sizeDelta.y + b.sizeDelta.y) * 0.5f;
    }

    static System.Type RuntimeType(string name)
    {
        var type = System.Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "Missing runtime component: " + name);
        return type;
    }
}
