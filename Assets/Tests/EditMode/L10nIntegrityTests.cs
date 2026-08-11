using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;

// Static-integrity tests for the localization table and cross-script
// contracts. The game scripts live in the predefined Assembly-CSharp,
// which a test asmdef cannot reference at compile time — so these tests
// reach the game types via reflection. That keeps this assembly fully
// decoupled: it is editor-only (UNITY_INCLUDE_TESTS) and can never ship
// with, or break, the player build.
public class L10nIntegrityTests
{
    static Type FindGameType(string name)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(name);
            if (t != null) return t;
        }
        Assert.Fail("Type '" + name + "' not found in loaded assemblies — renamed?");
        return null;
    }

    static IDictionary L10nTable()
    {
        var l10n = FindGameType("L10n");
        var field = l10n.GetField("Table", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(field, "L10n.Table field not found — renamed?");
        return (IDictionary)field.GetValue(null);
    }

    static readonly Regex Placeholder = new Regex(@"\{\d+\}");

    [Test]
    public void EveryEntryHasEnglishAndGreek()
    {
        var table = L10nTable();
        Assert.Greater(table.Count, 0, "L10n table is empty");

        foreach (DictionaryEntry entry in table)
        {
            var values = (string[])entry.Value;
            Assert.AreEqual(2, values.Length,
                $"Key '{entry.Key}' must have exactly {{English, Greek}}");
            Assert.IsFalse(string.IsNullOrWhiteSpace(values[0]),
                $"Key '{entry.Key}' has an empty English entry");
            Assert.IsFalse(string.IsNullOrWhiteSpace(values[1]),
                $"Key '{entry.Key}' has an empty Greek entry");
        }
    }

    [Test]
    public void FormatPlaceholdersMatchAcrossLanguages()
    {
        // A {0} present in English but missing in Greek (or vice versa)
        // silently drops information for one language's players.
        foreach (DictionaryEntry entry in L10nTable())
        {
            var values = (string[])entry.Value;
            CollectionAssert.AreEquivalent(
                PlaceholderSet(values[0]), PlaceholderSet(values[1]),
                $"Key '{entry.Key}': format placeholders differ between English and Greek");
        }
    }

    static List<string> PlaceholderSet(string s)
    {
        var result = new List<string>();
        foreach (Match m in Placeholder.Matches(s))
            if (!result.Contains(m.Value))
                result.Add(m.Value);
        return result;
    }

    [Test]
    public void FormattedEntriesSurviveStringFormat()
    {
        // string.Format throws on malformed braces ("{0", "{}"). L10n.Get
        // formats every entry that receives args — a typo must fail here,
        // not in front of a player mid-match. Ten dummy args so a valid
        // future {3}+ placeholder isn't misreported as malformed.
        foreach (DictionaryEntry entry in L10nTable())
        {
            var values = (string[])entry.Value;
            foreach (var s in values)
                Assert.DoesNotThrow(
                    () => string.Format(s, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10),
                    $"Key '{entry.Key}': malformed format string '{s}'");
        }
    }

    [Test]
    public void SceneTextKeysAllResolveToTableEntries()
    {
        // ExtrasRuntimeWiring maps scene-authored label content onto L10n
        // keys; a key that fell out of the table would leave a scene label
        // showing the raw key name after a language switch.
        var wiring = FindGameType("ExtrasRuntimeWiring");
        var field = wiring.GetField("SceneTextKeys", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(field, "ExtrasRuntimeWiring.SceneTextKeys not found — renamed?");
        var map = (IDictionary)field.GetValue(null);

        var table = L10nTable();
        foreach (DictionaryEntry entry in map)
            Assert.IsTrue(table.Contains((string)entry.Value),
                $"SceneTextKeys maps '{entry.Key}' to missing L10n key '{entry.Value}'");
    }

    [Test]
    public void DifficultyPrefKeyMatchesBetweenGameManagerAndSettingsUI()
    {
        // GameManager reads the difficulty per AI guess; the Settings row in
        // ExtrasRuntimeWiring writes it. Each declares the PlayerPrefs key as
        // its own private const — if they drift apart, the difficulty
        // selector silently stops affecting the AI.
        string gm = PrivateConst("GameManager", "DifficultyPrefKey");
        string ui = PrivateConst("ExtrasRuntimeWiring", "DifficultyPrefKey");
        Assert.AreEqual(gm, ui,
            "GameManager and ExtrasRuntimeWiring disagree on the difficulty PlayerPrefs key");
    }

    static string PrivateConst(string typeName, string fieldName)
    {
        var t = FindGameType(typeName);
        var f = t.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(f, typeName + "." + fieldName + " not found — renamed?");
        return (string)f.GetValue(null);
    }
}
