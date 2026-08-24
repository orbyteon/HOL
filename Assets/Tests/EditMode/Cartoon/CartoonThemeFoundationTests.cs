using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

// Reflection keeps the editor-only test assembly decoupled from the predefined
// Assembly-CSharp assembly, matching the other HOL integrity tests.
public sealed class CartoonThemeFoundationTests
{
    int originalLanguage;

    [SetUp]
    public void SetUp()
    {
        originalLanguage = Convert.ToInt32(L10nType()
            .GetProperty("Current", StaticFlags).GetValue(null));
        ResetTheme();
    }

    [TearDown]
    public void TearDown()
    {
        SetLanguage(originalLanguage);
        ResetTheme();
    }

    [Test]
    public void CartoonIsTheOnlyProductionThemeAndCatalogIsComplete()
    {
        Type theme = RuntimeType("HolTheme");
        object currentId = theme.GetProperty("CurrentId", StaticFlags)
            .GetValue(null);
        Assert.That(currentId.ToString(), Is.EqualTo("Cartoon"));

        object catalog = Catalog();
        Assert.That(catalog, Is.Not.Null);
        Assert.That((bool)catalog.GetType().GetProperty("IsComplete")
            .GetValue(catalog), Is.True);
        Assert.That((Vector2)Field(catalog, "referenceResolution"),
            Is.EqualTo(new Vector2(1080f, 1920f)));
    }

    [Test]
    public void TypographyResolvesExactFamiliesByLanguageAndRole()
    {
        SetLanguage(0);
        AssertFont("Hero", "Montserrat ExtraBold");
        AssertFont("SectionHeading", "Montserrat Bold");
        AssertFont("Body", "Plus Jakarta Sans Medium");
        AssertFont("Small", "Plus Jakarta Sans Regular");

        SetLanguage(1);
        AssertFont("Hero", "Noto Sans ExtraBold");
        AssertFont("SectionHeading", "Noto Sans Bold");
        AssertFont("Body", "Noto Sans Medium");
        AssertFont("Small", "Noto Sans Regular");
        AssertFont("LiveNumber", "Montserrat ExtraBold");
    }

    [Test]
    public void EveryCartoonFontIsStaticAndCanonicalCharactersAreCovered()
    {
        object typography = Field(Catalog(), "typography");
        string[] names =
        {
            "montserratExtraBold", "montserratBold",
            "plusJakartaSemiBold", "plusJakartaMedium",
            "plusJakartaRegular", "notoSansExtraBold", "notoSansBold",
            "notoSansSemiBold", "notoSansMedium", "notoSansRegular"
        };
        foreach (string name in names)
        {
            var font = (TMP_FontAsset)Field(typography, name);
            Assert.That(font, Is.Not.Null, name);
            Assert.That(font.atlasPopulationMode,
                Is.EqualTo(AtlasPopulationMode.Static), font.name);
        }

        string canonical = (string)RuntimeType("CartoonCharacterSet")
            .GetMethod("Build", StaticFlags).Invoke(null, null);
        var noto = (TMP_FontAsset)Field(typography, "notoSansRegular");
        foreach (char character in canonical)
        {
            Assert.That(HasCharacterOrFallback(noto, character), Is.True,
                "Missing canonical U+" + ((int)character).ToString("X4") +
                " " + character);
        }
    }

    [Test]
    public void ApprovedNineSliceSpritesRetainBorders()
    {
        object catalog = Catalog();
        object shared = Field(catalog, "shared");
        object splash = Field(catalog, "splash");
        AssertBorderFits((Sprite)Field(shared, "primaryButton"), 235f);
        AssertBorderFits((Sprite)Field(shared, "secondaryBlueButton"), 205f);
        AssertBorderFits((Sprite)Field(shared, "secondaryMagentaButton"), 205f);
        AssertBorderFits((Sprite)Field(shared, "neutralPanel"), 190f);
        AssertBorderFits((Sprite)Field(shared, "playerChip"), 120f);
        AssertBorderFits((Sprite)Field(splash, "loadingTrack"), 116f);
    }

    static BindingFlags StaticFlags => BindingFlags.Public |
                                       BindingFlags.NonPublic |
                                       BindingFlags.Static;

    static object Catalog()
    {
        return RuntimeType("HolTheme").GetProperty("Current", StaticFlags)
            .GetValue(null);
    }

    static object Field(object owner, string name)
    {
        Assert.That(owner, Is.Not.Null, name);
        var field = owner.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        return field.GetValue(owner);
    }

    static void AssertFont(string roleName, string expected)
    {
        Type roleType = RuntimeType("HolTextRole");
        object role = Enum.Parse(roleType, roleName);
        var font = (TMP_FontAsset)RuntimeType("CartoonTypography")
            .GetMethod("Resolve", StaticFlags).Invoke(null,
                new[] { role });
        Assert.That(font, Is.Not.Null, roleName);
        Assert.That(font.name, Does.Contain(expected), roleName);
    }

    static bool HasCharacterOrFallback(TMP_FontAsset font, char character)
    {
        if (font.HasCharacter(character, false, false)) return true;
        return HasFallback(font, character, new HashSet<TMP_FontAsset>());
    }

    static bool HasFallback(TMP_FontAsset font, char character,
        HashSet<TMP_FontAsset> visited)
    {
        if (font == null || !visited.Add(font)) return false;
        foreach (var fallback in font.fallbackFontAssetTable)
        {
            if (fallback != null &&
                fallback.HasCharacter(character, false, false)) return true;
            if (HasFallback(fallback, character, visited)) return true;
        }
        return false;
    }

    static void AssertBorder(Sprite sprite)
    {
        Assert.That(sprite, Is.Not.Null);
        Assert.That(sprite.border.sqrMagnitude, Is.GreaterThan(0f),
            sprite.name);
    }

    static void AssertBorderFits(Sprite sprite, float minimumAuthoredHeight)
    {
        AssertBorder(sprite);
        Assert.That(sprite.border.y + sprite.border.w,
            Is.LessThan(minimumAuthoredHeight),
            sprite.name + " collapses its center at the production height.");
    }

    static Type L10nType()
    {
        return RuntimeType("L10n");
    }

    static void SetLanguage(int value)
    {
        Type l10n = L10nType();
        Type language = l10n.GetNestedType("Language",
            BindingFlags.Public);
        l10n.GetMethod("SetLanguage", StaticFlags).Invoke(null,
            new[] { Enum.ToObject(language, value) });
    }

    static void ResetTheme()
    {
        RuntimeType("HolTheme").GetMethod("ResetCache", StaticFlags)
            .Invoke(null, null);
    }

    static Type RuntimeType(string name)
    {
        Type type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "Missing runtime type " + name);
        return type;
    }
}
