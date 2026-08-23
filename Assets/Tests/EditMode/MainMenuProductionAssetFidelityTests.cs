using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuProductionAssetFidelityTests
{
    GameObject root;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("CanvasRoot", typeof(RectTransform));
        BuildHomeFixture(root.transform);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
    }

    [Test]
    public void ApprovedCtasAreVisibleSlicedAndKeepCallbacks()
    {
        var play = Find("ButtonPlay").GetComponent<Button>();
        var pvp = Find("ButtonPvP").GetComponent<Button>();
        var daily = Find("DailyHuntButton").GetComponent<Button>();
        int calls = 0;
        play.onClick.AddListener(() => calls++);
        pvp.onClick.AddListener(() => calls++);
        daily.onClick.AddListener(() => calls++);

        Assert.That(MainMenuProductionAssetFidelity.ApplyToRoot(root.transform), Is.True);

        AssertCta("ButtonPlay", "phase2a/hol_cta_gold_r2_9s");
        AssertCta("ButtonPvP", "phase2a/hol_cta_blue_r2_9s");
        AssertCta("DailyHuntButton", "phase2a/hol_cta_gold_r2_9s");

        play.onClick.Invoke();
        pvp.onClick.Invoke();
        daily.onClick.Invoke();
        Assert.That(calls, Is.EqualTo(3),
            "Fidelity correction must preserve existing Button callbacks.");

        Assert.That(play.GetComponent<MainMenuCtaLuminousSurface>(), Is.Null);
        Assert.That(pvp.GetComponent<MainMenuCtaLuminousSurface>(), Is.Null);
        Assert.That(daily.GetComponent<MainMenuCtaLuminousSurface>(), Is.Null);
        Assert.That(DirectChild(play.transform, "HomeCtaInnerLight"), Is.Null);
        Assert.That(DirectChild(pvp.transform, "HomeCtaInnerLight"), Is.Null);
        Assert.That(DirectChild(daily.transform, "HomeCtaInnerLight"), Is.Null);
    }

    [Test]
    public void ApprovedChipAvatarGearIconsAndTipReplaceProceduralSubstitutes()
    {
        Assert.That(MainMenuProductionAssetFidelity.ApplyToRoot(root.transform), Is.True);

        var chip = Find(MainMenuHomeVisuals.ChipName).GetComponent<Image>();
        AssertSprite(chip, "phase2a/hol_player_chip_r2_9s");
        Assert.That(chip.color.a, Is.EqualTo(1f));
        Assert.That(chip.type, Is.EqualTo(Image.Type.Sliced));
        Assert.That(DirectChild(chip.transform, "HomePlayerChipSurface"), Is.Null);

        var avatar = Find("HomePlayerAvatar").GetComponent<Image>();
        AssertSprite(avatar, "reference/player_cyan_exact");
        Assert.That(avatar.color.a, Is.EqualTo(1f));
        Assert.That(DirectChild(chip.transform, "HomePlayerAvatarSymbol"), Is.Null);

        var gear = Find("Buttonsettings").GetComponent<Image>();
        AssertSprite(gear, "phase2a/hol_settings_gear_r2");
        Assert.That(gear.color.a, Is.EqualTo(1f));
        Assert.That(DirectChild(gear.transform, "HomeSettingsGearSymbol"), Is.Null);

        AssertModeIcon("ButtonPvP", MainMenuHomeVisuals.PrivateIconName,
            "phase2a/hol_mode_private_r2");
        AssertModeIcon("DailyHuntButton", MainMenuHomeVisuals.DailyIconName,
            "phase2a/hol_mode_daily_r2");

        var tip = Find(MainMenuHomeVisuals.TipName).GetComponent<Image>();
        AssertSprite(tip, "phase2a/hol_tip_frame_r2_9s");
        Assert.That(tip.color.a, Is.EqualTo(1f));
        Assert.That(tip.type, Is.EqualTo(Image.Type.Sliced));
    }

    static void BuildHomeFixture(Transform parent)
    {
        BuildButton(parent, "ButtonPlay", true);
        var pvp = BuildButton(parent, "ButtonPvP", true);
        var daily = BuildButton(parent, "DailyHuntButton", true);
        var settings = BuildButton(parent, "Buttonsettings", false);

        AddProceduralIcon(pvp.transform, MainMenuHomeVisuals.PrivateIconName,
            MainMenuReferenceIconKind.PrivateRoom);
        AddProceduralIcon(daily.transform, MainMenuHomeVisuals.DailyIconName,
            MainMenuReferenceIconKind.DailyHunt);
        AddProceduralIcon(settings.transform, "HomeSettingsGearSymbol",
            MainMenuReferenceIconKind.Gear);

        var chip = NewRect(parent, MainMenuHomeVisuals.ChipName);
        var chipImage = chip.gameObject.AddComponent<Image>();
        chipImage.sprite = Resources.Load<Sprite>("phase2a/hol_player_chip_r2_9s");
        chipImage.color = new Color(1f, 1f, 1f, 0.002f);
        chipImage.type = Image.Type.Simple;

        var chipSurface = NewRect(chip, "HomePlayerChipSurface");
        chipSurface.gameObject.AddComponent<MainMenuPlayerChipGraphic>();

        var avatar = NewRect(chip, "HomePlayerAvatar");
        var avatarImage = avatar.gameObject.AddComponent<Image>();
        avatarImage.sprite = Resources.Load<Sprite>("reference/player_cyan_exact");
        avatarImage.color = new Color(1f, 1f, 1f, 0.002f);

        AddProceduralIcon(chip, "HomePlayerAvatarSymbol",
            MainMenuReferenceIconKind.Player);

        var tip = NewRect(parent, MainMenuHomeVisuals.TipName);
        var tipImage = tip.gameObject.AddComponent<Image>();
        tipImage.sprite = Resources.Load<Sprite>("mainmenu/mainmenu_tip_frame_9s");
        tipImage.color = new Color(0.78f, 0.68f, 1f, 1f);
        tipImage.type = Image.Type.Simple;
    }

    static Button BuildButton(Transform parent, string name, bool addProceduralCta)
    {
        var rect = NewRect(parent, name);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.002f);
        image.type = Image.Type.Simple;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        if (addProceduralCta)
        {
            rect.gameObject.AddComponent<MainMenuCtaLuminousSurface>();
            var inner = NewRect(rect, "HomeCtaInnerLight");
            inner.gameObject.AddComponent<MainMenuChamferedCtaGraphic>();
        }
        return button;
    }

    static void AddProceduralIcon(Transform parent, string name,
        MainMenuReferenceIconKind kind)
    {
        var rect = NewRect(parent, name);
        var graphic = rect.gameObject.AddComponent<MainMenuReferenceIconGraphic>();
        graphic.Configure(kind);
    }

    static RectTransform NewRect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    void AssertCta(string buttonName, string resource)
    {
        var button = Find(buttonName).GetComponent<Button>();
        var image = button.GetComponent<Image>();
        AssertSprite(image, resource);
        Assert.That(image.color.a, Is.EqualTo(1f));
        Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
        Assert.That(button.targetGraphic, Is.SameAs(image));
    }

    void AssertModeIcon(string buttonName, string iconName, string resource)
    {
        var button = Find(buttonName);
        var icon = DirectChild(button, iconName);
        Assert.That(icon, Is.Not.Null);
        Assert.That(icon.GetComponent<MainMenuReferenceIconGraphic>(), Is.Null,
            iconName + " must not keep the procedural replacement graphic.");
        var image = icon.GetComponent<Image>();
        Assert.That(image, Is.Not.Null);
        AssertSprite(image, resource);
        Assert.That(image.color.a, Is.EqualTo(1f));
    }

    static void AssertSprite(Image image, string resource)
    {
        var expected = Resources.Load<Sprite>(resource);
        Assert.That(expected, Is.Not.Null, resource);
        Assert.That(image.sprite, Is.SameAs(expected));
    }

    Transform Find(string name)
    {
        var found = DeepFind(root.transform, name);
        Assert.That(found, Is.Not.Null, name);
        return found;
    }

    static Transform DirectChild(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name)
                return parent.GetChild(i);
        return null;
    }

    static Transform DeepFind(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var found = DeepFind(parent.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
