using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;

public sealed class DailyHuntProductionFontTests
{
    const string DisplayAssetPath =
        "Assets/newdesign/Resources/dailyhunt/production/fonts/HOL Daily Display SDF.asset";
    const string BodyAssetPath =
        "Assets/newdesign/Resources/dailyhunt/production/fonts/HOL Daily Body SDF.asset";

    static readonly string[] DailyLocalizationKeys =
    {
        "ad_not_ready", "between_range", "daily_all_missions_complete",
        "daily_challenge_title", "daily_come_back", "daily_failed",
        "daily_found", "daily_hunt", "daily_hunt_number", "daily_intro",
        "daily_mission_correct", "daily_mission_share_room",
        "daily_mission_win", "daily_missions_heading",
        "daily_missions_progress", "daily_reset_label",
        "daily_reward_collected", "daily_reward_heading",
        "daily_reward_pending", "daily_share", "daily_start",
        "daily_streak", "daily_streak_heading", "guesses_left",
        "home_daily_title", "invalid_number", "number_placeholder",
        "player_default", "pvp_guess", "result_attempts", "second_chance",
        "share_copied", "share_result", "your_guess",
    };

    [TestCase(DisplayAssetPath)]
    [TestCase(BodyAssetPath)]
    public void DailyFontIsStaticAndCoversEveryLocalizedCodePoint(string path)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        Assert.That(font, Is.Not.Null, path);
        Assert.That(font.atlasPopulationMode,
            Is.EqualTo(AtlasPopulationMode.Static), path);
        Assert.That(font.fallbackFontAssetTable, Is.Empty,
            path + " must not create runtime fallback submeshes.");

        var available = new HashSet<uint>(
            font.characterTable.Select(character => character.unicode));
        uint[] missing = RequiredCodePoints()
            .Where(codePoint => !available.Contains(codePoint)).ToArray();
        Assert.That(missing, Is.Empty,
            path + " is missing localized production glyphs: " +
            string.Join(" ", missing.Select(value =>
                "U+" + value.ToString("X4")).ToArray()));
    }

    static uint[] RequiredCodePoints()
    {
        var result = new SortedSet<uint>();
        Type l10n = Type.GetType("L10n, Assembly-CSharp");
        Assert.That(l10n, Is.Not.Null, "L10n runtime type");
        FieldInfo field = l10n.GetField(
            "Table", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(field, Is.Not.Null, "L10n.Table");
        var table = field.GetValue(null) as IDictionary;
        Assert.That(table, Is.Not.Null, "L10n.Table value");

        foreach (string key in DailyLocalizationKeys)
        {
            Assert.That(table.Contains(key), Is.True, key);
            var values = table[key] as string[];
            Assert.That(values, Is.Not.Null, key);
            foreach (string value in values)
            {
                AddCodePoints(result, value);
                AddCodePoints(result, value.ToUpperInvariant());
            }
        }

        AddCodePoints(result,
            "Player Παίκτης Andreas 0123456789 -/:,.!?%+×↑↓•");
        return result.ToArray();
    }

    static void AddCodePoints(ISet<uint> destination, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        for (int index = 0; index < text.Length; index++)
        {
            int codePoint = char.ConvertToUtf32(text, index);
            if (char.IsHighSurrogate(text[index])) index++;
            if (codePoint == '\r' || codePoint == '\n' || codePoint == '\t')
                continue;
            destination.Add((uint)codePoint);
        }
    }
}
