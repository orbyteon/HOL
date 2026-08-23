using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class MainMenuHomeFidelityEnforcerTests
{
    const string VisualRootName = "HomeVisualRoot";
    const string ChipName = "HomePlayerChip";
    const string PrivateIconName = "HomePrivateIcon";
    const string DailyIconName = "HomeDailyIcon";

    GameObject root;
    Canvas canvas;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var visualRoot = Child(root.transform, VisualRootName);
        BuildButton(visualRoot.transform, "ButtonPlay");
        BuildButton(visualRoot.transform, "ButtonPvP");
        BuildButton(visualRoot.transform, "DailyHuntButton");
        BuildButton(visualRoot.transform, "Buttonsettings");

        var chip = Child(visualRoot.transform, ChipName);
        chip.AddComponent<Image>();
        AddRuntimeComponent(Child(chip.transform, "HomePlayerChipSurface"),
            "MainMenuPlayerChipGraphic");
        Child(chip.transform, "HomePlayerAvatar").AddComponent<Image>();
        AddRuntimeComponent(Child(chip.transform, "HomePlayerAvatarSymbol"),
            "MainMenuReferenceIconGraphic");

        AddRuntimeComponent(Child(visualRoot.transform, PrivateIconName),
            "MainMenuReferenceIconGraphic");
        AddRuntimeComponent(Child(visualRoot.transform, DailyIconName),
            "MainMenuReferenceIconGraphic");

        AddRuntimeComponent(Child(Find(visualRoot.transform, "Buttonsettings"),
            "HomeSettingsGearSymbol"), "MainMenuReferenceIconGraphic");
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(root);
    }

    [Test]
    public void ApplyToCanvas_RestoresApprovedSpritesAtFullAlphaAndSlicedNineSlice()
    {
        Assert.That(Apply(), Is.True);

        AssertNineSlice("ButtonPlay", "phase2a/hol_cta_gold_r2_9s");
        AssertNineSlice("ButtonPvP", "phase2a/hol_cta_blue_r2_9s");
        AssertNineSlice("DailyHuntButton", "phase2a/hol_cta_gold_r2_9s");

        var chip = Find(root.transform, ChipName).GetComponent<Image>();
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

        Apply();
        button.onClick.Invoke();

        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void ApplyToCanvas_DisablesProceduralReplacementGraphics()
    {
        Apply();

        AssertRuntimeBehavioursDisabled("MainMenuReferenceIconGraphic");
        AssertRuntimeBehavioursDisabled("MainMenuPlayerChipGraphic");

        var avatar = Find(root.transform, "HomePlayerAvatar").GetComponent<Image>();
        Assert.That(avatar.sprite, Is.SameAs(Resources.Load<Sprite>("reference/player_cyan_exact")));
        Assert.That(avatar.color.a, Is.EqualTo(1f));

        var privateIcon = Find(root.transform, PrivateIconName).GetComponent<Image>();
        Assert.That(privateIcon.sprite, Is.SameAs(Resources.Load<Sprite>("phase2a/hol_mode_private_r2")));
        Assert.That(privateIcon.color.a, Is.EqualTo(1f));

        var dailyIcon = Find(root.transform, DailyIconName).GetComponent<Image>();
        Assert.That(dailyIcon.sprite, Is.SameAs(Resources.Load<Sprite>("phase2a/hol_mode_daily_r2")));
        Assert.That(dailyIcon.color.a, Is.EqualTo(1f));
    }

    bool Apply()
    {
        var type = RuntimeType("MainMenuHomeFidelityEnforcer");
        var method = type.GetMethod("ApplyToCanvas", BindingFlags.Public | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);
        return (bool)method.Invoke(null, new object[] { canvas });
    }

    void AssertRuntimeBehavioursDisabled(string runtimeTypeName)
    {
        var type = RuntimeType(runtimeTypeName);
        foreach (var component in root.GetComponentsInChildren(type, true))
            Assert.That(((Behaviour)component).enabled, Is.False, component.name);
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

    static void AddRuntimeComponent(GameObject host, string typeName)
    {
        host.AddComponent(RuntimeType(typeName));
    }

    static Type RuntimeType(string name)
    {
        var type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, name);
        return type;
    }

    static GameObject Child(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
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
}
