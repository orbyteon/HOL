using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class MainMenuHomeVisualsPlayModeTests
{
    const BindingFlags StaticFlags =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [UnityTest]
    public IEnumerator HomeOwnerMapsExistingControlsWithoutInventingFeatures()
    {
        InvokeInstaller("MainMenuHomeVisuals");
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        for (int i = 0; i < 16; i++)
            yield return null;
        yield return new WaitForSecondsRealtime(0.35f);

        var ownerType = RuntimeType("MainMenuHomeVisuals");
        var owner = Object.FindObjectOfType(ownerType) as Component;
        Assert.That(owner, Is.Not.Null);
        Assert.That((bool)ownerType.GetProperty("IsReady").GetValue(owner, null), Is.True);
        Assert.That((bool)ownerType.GetProperty("IsSettled").GetValue(owner, null), Is.True);

        var canvas = owner.GetComponent<Canvas>();
        Assert.That(canvas, Is.Not.Null);
        Assert.That(Find(canvas.transform, "HomeVisualRoot"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "HomeLogo"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "HomeHeroBoy"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "HomeHeroGirl"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "HomeMascotSix"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "HomeMascotSeven"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "HomePlayerChip"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "HomeTipCard"), Is.Not.Null);
        Assert.That(Find(canvas.transform, "BoardHomeLogo"), Is.Null);
        Assert.That(Find(canvas.transform, "BoardStorePanel"), Is.Null);
        Assert.That(Find(canvas.transform, "BoardProfilePanel"), Is.Null);

        var six = Find(canvas.transform, "HomeMascotSix") as RectTransform;
        var seven = Find(canvas.transform, "HomeMascotSeven") as RectTransform;
        Assert.That(six.anchoredPosition.x, Is.LessThan(seven.anchoredPosition.x));

        var boy = Find(canvas.transform, "HomeHeroBoy") as RectTransform;
        var girl = Find(canvas.transform, "HomeHeroGirl") as RectTransform;
        Assert.That(boy.anchoredPosition.x, Is.LessThan(girl.anchoredPosition.x));

        foreach (var name in new[] { "HomeLogo", "HomeHeroBoy", "HomeHeroGirl",
                     "HomeMascotSix", "HomeMascotSeven" })
        {
            var image = Find(canvas.transform, name).GetComponent<Image>();
            Assert.That(image.preserveAspect, Is.True, name);
            Assert.That(image.raycastTarget, Is.False, name);
        }

        Assert.That(Object.FindObjectsOfType<Button>().Length,
            Is.GreaterThanOrEqualTo(4));
        foreach (var button in canvas.GetComponentsInChildren<Button>(true))
            Assert.That(button.name.StartsWith("Home"), Is.False,
                "Home owner must not invent a Button: " + button.name);

        var play = Find(canvas.transform, "ButtonPlay").GetComponent<Button>();
        var pvp = Find(canvas.transform, "ButtonPvP").GetComponent<Button>();
        var hunt = Find(canvas.transform, "DailyHuntButton").GetComponent<Button>();
        var settings = Find(canvas.transform, "Buttonsettings").GetComponent<Button>();
        Assert.That(play.onClick.GetPersistentEventCount() + play.onClick.GetPersistentEventCount(),
            Is.GreaterThanOrEqualTo(0));
        Assert.That(play.GetComponent<Image>().sprite.name, Does.Contain("gold"));
        Assert.That(pvp.GetComponent<Image>().sprite.name, Does.Contain("blue"));
        Assert.That(hunt.GetComponent<Image>().sprite.name, Does.Contain("magenta"));

        PlayerPrefs.SetInt("StatStreak", 7);
        PlayerPrefs.SetString("PlayerName", "Andreas");
        PlayerPrefs.Save();
        ownerType.GetMethod("RefreshChip", InstanceFlags).Invoke(owner, null);
        var chip = Find(canvas.transform, "HomePlayerChipText")
            .GetComponent<TMPro.TMP_Text>();
        Assert.That(chip.text, Does.Contain("7"));
        Assert.That(chip.text, Does.Not.Contain("2450"));
        Assert.That(chip.text, Does.Not.Contain("2,450"));
        Assert.That(Find(canvas.transform, "HomePlayerChip").GetComponent<Image>().raycastTarget,
            Is.False);

        var paths = (string[])ownerType.GetField("LoadedResources", StaticFlags)
            .GetValue(null);
        foreach (var path in paths)
            Assert.That(path.StartsWith("splash/"), Is.False, path);

        var menu = Object.FindObjectOfType(RuntimeType("MenuManager")) as Component;
        int playCalls = PersistentOrRuntime(play);
        menu.SendMessage("OnPlayPressed", SendMessageOptions.RequireReceiver);
        yield return null;
        var panelPlay = menu.GetType().GetField("panelPlay").GetValue(menu) as GameObject;
        Assert.That(panelPlay.activeSelf, Is.True);
        menu.SendMessage("BackToMenu", SendMessageOptions.RequireReceiver);
        yield return null;

        settings.onClick.Invoke();
        yield return null;
        var settingsPanel = menu.GetType().GetField("settingsPanel").GetValue(menu) as GameObject;
        Assert.That(settingsPanel.activeSelf, Is.True);
        menu.SendMessage("BackToMenu", SendMessageOptions.RequireReceiver);

        Assert.That(playCalls, Is.GreaterThanOrEqualTo(0));
        Assert.That(pvp, Is.Not.Null);
        Assert.That(hunt, Is.Not.Null);
    }

    static int PersistentOrRuntime(Button button)
    {
        return button.onClick.GetPersistentEventCount();
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
