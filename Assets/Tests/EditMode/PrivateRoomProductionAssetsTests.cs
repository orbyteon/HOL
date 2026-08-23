using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class PrivateRoomProductionAssetsTests
{
    static readonly string[] RequiredSprites =
    {
        "phase2a/hol_neon_reference_bg_r3",
        "reference/hol_logo_exact",
        "reference/char_boy_exact",
        "reference/char_girl_exact",
        "reference/board_join_exact",
        "reference/mascot_6_exact",
        "reference/mascot_7_exact",
        "reference/player_cyan_exact",
        "phase2a/hol_chevron_r2",
        "mainmenu/mainmenu_cta_blue_9s",
        "mainmenu/mainmenu_cta_gold_9s",
        "phase2a/hol_cta_magenta_r2_9s",
        "mainmenu/mainmenu_tip_frame_9s",
        "mainmenu/mainmenu_player_chip_frame_9s",
        "mainmenu/mainmenu_icon_tip_bulb",
        "mainmenu/mainmenu_icon_streak"
    };

    static readonly string[] RequiredFonts =
    {
        "phase2a/fonts/HOL Menu Display SDF",
        "phase2a/fonts/HOL Menu Body SDF"
    };

    [Test]
    public void EveryPrivateRoomProductionSpriteLoads()
    {
        foreach (string path in RequiredSprites)
        {
            var sprite = Resources.Load<Sprite>(path);
            Assert.That(sprite, Is.Not.Null,
                "Private Room requires Resources/" + path + ".");
        }
    }

    [Test]
    public void EveryPrivateRoomNineSliceHasAuthoredBorders()
    {
        foreach (string path in RequiredSprites)
        {
            if (!path.Contains("_9s")) continue;
            var sprite = Resources.Load<Sprite>(path);
            Assert.That(sprite, Is.Not.Null, path);
            Vector4 border = sprite.border;
            Assert.That(border.x + border.y + border.z + border.w,
                Is.GreaterThan(0f),
                path + " is named _9s but has no Sprite border metadata.");
        }
    }

    [Test]
    public void PrivateRoomProductionFontsLoad()
    {
        Type fontType = Type.GetType("TMPro.TMP_FontAsset, Unity.TextMeshPro");
        Assert.That(fontType, Is.Not.Null);
        foreach (string path in RequiredFonts)
        {
            var font = Resources.Load(path, fontType);
            Assert.That(font, Is.Not.Null,
                "Private Room requires Resources/" + path + ".");
        }
    }

    [Test]
    public void PrivateRoomReferenceCopyStaysExactInEnglishAndGreek()
    {
        Type l10n = RuntimeType("L10n");
        Assert.That(l10n, Is.Not.Null);
        MethodInfo get = l10n.GetMethod("Get", BindingFlags.Public | BindingFlags.Static);
        Assert.That(get, Is.Not.Null);

        bool hadLanguage = PlayerPrefs.HasKey("Language");
        int previousLanguage = PlayerPrefs.GetInt("Language", 0);
        try
        {
            PlayerPrefs.SetInt("Language", 0);
            Assert.That(Localize(get, "private_room_create_title"), Is.EqualTo("CREATE A ROOM"));
            Assert.That(Localize(get, "private_room_create_action"), Is.EqualTo("CREATE"));
            Assert.That(Localize(get, "private_room_join_action"), Is.EqualTo("JOIN!"));

            PlayerPrefs.SetInt("Language", 1);
            Assert.That(Localize(get, "private_room_create_title"), Is.EqualTo("ΔΗΜΙΟΥΡΓΗΣΕ ΔΩΜΑΤΙΟ"));
            Assert.That(Localize(get, "private_room_create_action"), Is.EqualTo("ΔΗΜΙΟΥΡΓΙΑ"));
            Assert.That(Localize(get, "private_room_join_action"), Is.EqualTo("ΜΠΕΣ!"));
        }
        finally
        {
            if (hadLanguage) PlayerPrefs.SetInt("Language", previousLanguage);
            else PlayerPrefs.DeleteKey("Language");
        }
    }

    [Test]
    public void CurrentPrivateRoomOwnerExistsAndRetiredReskinOwnersAreGone()
    {
        Assert.That(RuntimeType("PrivateRoomVisuals"), Is.Not.Null);
        Assert.That(RuntimeType("PrivateRoomVisualsInstaller"), Is.Not.Null);

        string[] retiredTypes =
        {
            "ExactReferenceVisuals",
            "AttachmentReskinVisuals",
            "AttachmentReskinPolish",
            "AttachmentReskinCanvasBindings",
            "FrameGeometry",
            "NumberDrift"
        };
        foreach (string typeName in retiredTypes)
            Assert.That(RuntimeType(typeName), Is.Null,
                "Retired visual type returned to production: " + typeName);
    }

    [Test]
    public void PrivateRoomInstallerIsTheOnlyRuntimeLifecycleOwner()
    {
        Type visuals = RuntimeType("PrivateRoomVisuals");
        Type installer = RuntimeType("PrivateRoomVisualsInstaller");
        Assert.That(visuals, Is.Not.Null);
        Assert.That(installer, Is.Not.Null);

        foreach (MethodInfo method in visuals.GetMethods(
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            object[] attributes = method.GetCustomAttributes(
                typeof(RuntimeInitializeOnLoadMethodAttribute), false);
            Assert.That(attributes, Is.Empty,
                "PrivateRoomVisuals must not register its own runtime lifecycle hook: " + method.Name);
        }

        MethodInfo register = installer.GetMethod("Register",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(register, Is.Not.Null);
        Assert.That(register.GetCustomAttributes(
            typeof(RuntimeInitializeOnLoadMethodAttribute), false), Is.Not.Empty,
            "PrivateRoomVisualsInstaller must remain the sole runtime lifecycle bridge.");
    }

    [Test]
    public void RetiredGenericThemeSurfacesCannotLoad()
    {
        string[] retired =
        {
            "design/background_deep",
            "design/panel_surface",
            "design/button_primary",
            "design/button_secondary"
        };
        foreach (string path in retired)
            Assert.That(Resources.Load<Sprite>(path), Is.Null,
                "Retired generic theme surface returned: Resources/" + path);
    }

    static string Localize(MethodInfo get, string key)
    {
        return (string)get.Invoke(null, new object[] { key, Array.Empty<object>() });
    }

    static Type RuntimeType(string name)
    {
        return Type.GetType(name + ", Assembly-CSharp");
    }
}
