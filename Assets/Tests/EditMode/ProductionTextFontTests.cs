using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;

public sealed class ProductionTextFontTests
{
    const string FallbackPath =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset";
    const string PrimaryPath =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    const string ExpectedGuid = "2e498d1c8094910479dc3e1b768306a4";

    [Test]
    public void GreekFallbackKeepsIdentityAndIsStatic()
    {
        var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackPath);
        Assert.That(fallback, Is.Not.Null);
        Assert.That(AssetDatabase.AssetPathToGUID(FallbackPath), Is.EqualTo(ExpectedGuid));
        Assert.That(fallback.atlasPopulationMode,
            Is.EqualTo(AtlasPopulationMode.Static));
        Assert.That(fallback.creationSettings.sourceFontFileGUID,
            Is.EqualTo("e3265ab4bf004d28a9537516768c1c75"),
            "The static atlas must remain reproducible from LiberationSans.ttf.");
    }

    [Test]
    public void PrimaryAndFallbackCoverTheCanonicalProductionSetWithoutRuntimeAdds()
    {
        var primary = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PrimaryPath);
        var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackPath);
        Assert.That(primary, Is.Not.Null);
        Assert.That(fallback, Is.Not.Null);

        uint[] canonical = BakerCodePoints("CanonicalCodePoints");
        uint[] requiredFallback = BakerCodePoints("RequiredFallbackCodePoints");
        var primaryCharacters = new HashSet<uint>(
            primary.characterTable.Select(character => character.unicode));
        var fallbackCharacters = new HashSet<uint>(
            fallback.characterTable.Select(character => character.unicode));

        uint[] missingCanonical = canonical.Where(codePoint =>
            !primaryCharacters.Contains(codePoint) && !fallbackCharacters.Contains(codePoint)).ToArray();
        uint[] missingFallback = requiredFallback.Where(codePoint =>
            !fallbackCharacters.Contains(codePoint)).ToArray();

        Assert.That(missingCanonical, Is.Empty,
            "Canonical production characters missing from the static font chain: " +
            CodePointList(missingCanonical));
        Assert.That(missingFallback, Is.Empty,
            "Characters assigned to the fallback were not pre-baked: " +
            CodePointList(missingFallback));
        Assert.That(canonical.Contains((uint)0x2605), Is.False);
        Assert.That(canonical.Contains((uint)0x232B), Is.False);
        Assert.That(fallbackCharacters.Contains((uint)0x2605), Is.False);
        Assert.That(fallbackCharacters.Contains((uint)0x232B), Is.False);
    }

    static uint[] BakerCodePoints(string methodName)
    {
        Type baker = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("ProductionTextFontBaker"))
            .FirstOrDefault(type => type != null);
        Assert.That(baker, Is.Not.Null, "ProductionTextFontBaker is not compiled.");
        MethodInfo method = baker.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        Assert.That(method, Is.Not.Null, "Missing font baker method: " + methodName);
        return (uint[])method.Invoke(null, null);
    }

    static string CodePointList(IEnumerable<uint> codePoints)
    {
        return string.Join(" ", codePoints.Select(value => "U+" + value.ToString("X4")).ToArray());
    }
}
