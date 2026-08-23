using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class MainMenuHomeFidelityEnforcerTests
{
    GameObject root;
    Canvas canvas;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var visualRoot = Child(root.transform, MainMenuHomeVisuals.VisualRootName);
        BuildButton(visualRoot.transform, "ButtonPlay");
        BuildButton(visualRoot.transform, "ButtonPvP");
        BuildButton(visualRoot.transform, "DailyHuntButton");
        BuildButton(visualRoot.transform, "Buttonsettings");

        var chip = Child(visualRoot.transform, MainMenuHomeVisuals.ChipName);
        chip.AddComponent<Image>();
        Child(chip.transform, "HomePlayerChipSurface").AddComponent<MainMenuPlayerChipGraphic>();
        Child(chip.transform, "HomePlayerAvatar").AddComponent<Image>();
        Child(chip.transform, "HomePlayerAvatarSymbol").AddComponent<MainMenuReferenceIconGraphic>();

        Child(visualRoot.transform, MainMenuHomeVisuals.PrivateIconName)
            .AddComponent<MainMenuReferenceIconGraphic>();
        Child(visualRoot.transform, MainMenuHomeVisuals.DailyIconName)
            .AddComponent<MainMenuReferenceIconGraphic>();

        Child(Find(visualRoot.transform, "Buttonsettings"), "HomeSettingsGearSymbol")
            .AddComponent<MainMenuReferenceIconGraphic>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
    }

    [Test]
    public void ApplyToCanvas_RestoresApprovedSpritesAtFullAlphaAndSlicedNineSlice()
    {
        Assert.That(MainMenuHomeFidelityEnforcer.ApplyToCanvas(canvas), Is.True);

        AssertNineSlice("ButtonPlay", "phase2a/hol_cta_gold_r2_9s");
        AssertNineSlice("ButtonPvP", "phase2a/hol_cta_blue_r2_9s");
        AssertNineSlice("DailyHuntButton", "phase2a/hol_cta_gold_r2_9s");

        var chip = Find(root.transform, MainMenuHomeVisuals.ChipName).GetComponent<Image>();
        Assert.That(chip.sprite, Is.SameAs(Resources.Load<Sprite>("phase2a/hol_player_chip_r2_9s")));
        Assert.That(chip.color.a, Is.EqualTo(1f));
        Assert.That(chip.type, Is.EqualTo(Image.Type.Sliced));

        var gear = Find(root.transform, "Buttonsettings").GetComponent<Image>();
        Assert.That(gear.sprite, Is.SameAs(Resources.Load<Sprite>("phase2a/hol_settings_gear_r2")));
        Assert.That(gear.color.a, Is.EqualTo(1f));
    }

    [Test]
    public void ApplyToCanvas_PreservesExistingButtonCallbacks()
    {
        var button = Find(root.transform, "ButtonPlay").GetComponent<Button>();
        int calls = 0;
        UnityAction callback = () => calls++;
        button.onClick.AddListener(callback);

        MainMenuHomeFidelityEnforcer.ApplyToCanvas(canvas);
        button.onClick.Invoke();

        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void ApplyToCanvas_DisablesProceduralReplacementGraphics()
    {
        MainMenuHomeFidelityEnforcer.ApplyToCanvas(canvas);

        foreach (var graphic in root.GetComponentsInChildren<MainMenuReferenceIconGraphic>(true))
            Assert.That(graphic.enabled, Is.False, graphic.name);
        foreach (var graphic in root.GetComponentsInChildren<MainMenuPlayerChipGraphic>(true))
            Assert.That(graphic.enabled, Is.False, graphic.name);

        var avatar = Find(root.transform, "HomePlayerAvatar").GetComponent<Image>();
        Assert.That(avatar.sprite, Is.SameAs(Resources.Load<Sprite>("reference/player_cyan_exact")));
        Assert.That(avatar.color.a, Is.EqualTo(1f));

        var privateIcon = Find(root.transform, MainMenuHomeVisuals.PrivateIconName).GetComponent<Image>();
        Assert.That(privateIcon.sprite, Is.SameAs(Resources.Load<Sprite>("phase2a/hol_mode_private_r2")));
        Assert.That(privateIcon.color.a, Is.EqualTo(1f));

        var dailyIcon = Find(root.transform, MainMenuHomeVisuals.DailyIconName).GetComponent<Image>();
        Assert.That(dailyIcon.sprite, Is.SameAs(Resources.Load<Sprite>("phase2a/hol_mode_daily_r2")));
        Assert.That(dailyIcon.color.a, Is.EqualTo(1f));
    }

    void AssertNineSlice(string name, string resource)
    {
        var image = Find(root.transform, name).GetComponent<Image>();
        Assert.That(image.sprite, Is.SameAs(Resources.Load<Sprite>(resource)), name);
        Assert.That(image.color.a, Is.EqualTo(1f), name);
        Assert.That(image.type, Is.EqualTo(Image.Type.Sliced), name);
    }

    static GameObject BuildButton(Transform parent, string name)
    {
        var go = Child(parent, name);
        go.AddComponent<Image>();
        go.AddComponent<Button>();
        return go;
    }

    static GameObject Child(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static Transform Find(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = Find(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
