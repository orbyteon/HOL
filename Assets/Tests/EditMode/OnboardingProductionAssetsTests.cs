using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class OnboardingProductionAssetsTests
{
    static readonly string[] CharacterPaths =
    {
        "onboarding/characters/welcome_human_ensemble",
        "onboarding/characters/name_hero_wink_peace",
        "onboarding/characters/gender_girl_arms_crossed",
        "onboarding/characters/gender_other_purple",
    };

    static readonly string[] IconPaths =
    {
        "onboarding/icons/age_under13_shield_star",
        "onboarding/icons/age_teen_star",
        "onboarding/icons/age_adult_crown",
        "onboarding/icons/onboarding_indicator_disc_neutral",
    };

    static readonly string[] AgeMascotPaths =
    {
        "onboarding/mascots/age_under13_mascot_3_green",
        "onboarding/mascots/age_teen_mascot_7_blue",
        "onboarding/mascots/age_adult_mascot_6_pink",
    };

    [Test]
    public void EveryGeneratedProductionCutoutLoadsAsAnAlphaSprite()
    {
        foreach (string resourcePath in CharacterPaths)
            AssertProductionSprite(resourcePath);
        foreach (string resourcePath in IconPaths)
            AssertProductionSprite(resourcePath);
        foreach (string resourcePath in AgeMascotPaths)
            AssertProductionSprite(resourcePath);

        for (int index = 1; index <= 12; index++)
        {
            string[] names =
            {
                "teal_boy", "cap_boy", "glasses_boy", "blue_hair",
                "ponytail_girl", "cat_ear_girl", "bubblegum_girl",
                "gold_hoodie_girl", "green_cap", "silver_hair",
                "black_red_hair", "teal_braids",
            };
            AssertProductionSprite(
                "onboarding/avatars/avatar_" + index.ToString("00") +
                "_" + names[index - 1]);
        }
    }

    static void AssertProductionSprite(string resourcePath)
    {
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        Assert.That(sprite, Is.Not.Null,
            "Resources/" + resourcePath + " must import as a Sprite.");

        string assetPath = AssetDatabase.GetAssetPath(sprite);
        TextureImporter importer =
            AssetImporter.GetAtPath(assetPath) as TextureImporter;
        Assert.That(importer, Is.Not.Null, assetPath);
        Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
        Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
        Assert.That(importer.mipmapEnabled, Is.False);
        Assert.That(importer.alphaIsTransparency, Is.True);
        Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None));
        Assert.That(importer.textureCompression,
            Is.EqualTo(TextureImporterCompression.Uncompressed));
    }
}
