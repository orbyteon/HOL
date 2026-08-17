using NUnit.Framework;
using UnityEngine;

public class MainMenuProductionAssetsTests
{
    static readonly string[] ProductionSprites =
    {
        "hol_logo_exact",
        "mainmenu_bg_night_arcade",
        "mainmenu_cta_blue_9s",
        "mainmenu_cta_gold_9s",
        "mainmenu_cta_magenta_9s",
        "mainmenu_cta_violet_9s",
        "mainmenu_daily_hunt_frame_9s",
        "mainmenu_deco_confetti_overlay",
        "mainmenu_deco_horizon_overlay",
        "mainmenu_deco_lightning_overlay",
        "mainmenu_deco_numbers_overlay",
        "mainmenu_deco_stars_overlay",
        "mainmenu_gear_glossy",
        "mainmenu_gloss_primary_row",
        "mainmenu_gloss_secondary_row",
        "mainmenu_glow_logo",
        "mainmenu_glow_primary",
        "mainmenu_glow_secondary_row",
        "mainmenu_icon_1v1",
        "mainmenu_icon_daily_hunt",
        "mainmenu_icon_private_room",
        "mainmenu_icon_solo",
        "mainmenu_icon_streak",
        "mainmenu_icon_tip_bulb",
        "mainmenu_player_chip_frame_9s",
        "mainmenu_tip_frame_9s",
        "mascot_3_exact",
        "mascot_7_exact",
        "opponent_purple_exact",
        "player_cyan_exact"
    };

    [Test]
    public void EveryProductionSpriteLoadsFromItsStableResourcePath()
    {
        foreach (var name in ProductionSprites)
        {
            var path = "mainmenu/" + name;
            Assert.IsNotNull(Resources.Load<Sprite>(path),
                "Production sprite did not import at Resources/" + path + ".");
        }
    }

    [TestCase("mainmenu_cta_gold_9s", 112, 80, 112, 80)]
    [TestCase("mainmenu_cta_blue_9s", 72, 64, 72, 64)]
    [TestCase("mainmenu_cta_violet_9s", 72, 64, 72, 64)]
    [TestCase("mainmenu_cta_magenta_9s", 72, 64, 72, 64)]
    [TestCase("mainmenu_daily_hunt_frame_9s", 72, 56, 72, 56)]
    [TestCase("mainmenu_player_chip_frame_9s", 48, 40, 48, 40)]
    [TestCase("mainmenu_tip_frame_9s", 130, 140, 130, 155)]
    public void SlicedBorderMatchesApprovedInsets(
        string name, float left, float bottom, float right, float top)
    {
        var path = "mainmenu/" + name;
        var sprite = Resources.Load<Sprite>(path);
        Assert.IsNotNull(sprite,
            "Sliced sprite did not import at Resources/" + path + ".");

        Assert.AreEqual(new Vector4(left, bottom, right, top), sprite.border,
            "Unexpected sliced border for Resources/" + path + ".");
    }
}
