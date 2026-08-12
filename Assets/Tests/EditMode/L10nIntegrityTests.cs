using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;

// Static-integrity tests for localization and cross-script release contracts.
// Reflection keeps the editor-only test assembly decoupled from Assembly-CSharp.
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
        string gm = PrivateConst("GameManager", "DifficultyPrefKey");
        string ui = PrivateConst("ExtrasRuntimeWiring", "DifficultyPrefKey");
        Assert.AreEqual(gm, ui,
            "GameManager and ExtrasRuntimeWiring disagree on the difficulty PlayerPrefs key");
    }

    [Test]
    public void PvpRoomStateExposesOnlySanitizedPlayFabViewFields()
    {
        var backend = FindGameType("PvpBackend");
        var room = backend.GetNestedType("RoomState", BindingFlags.Public);
        Assert.IsNotNull(room, "PvpBackend.RoomState missing");

        foreach (var name in new[] { "lastHint", "revealedSecret", "hostGuessCount", "guestGuessCount" })
            Assert.IsNotNull(room.GetField(name, BindingFlags.Public | BindingFlags.Instance),
                "RoomState is missing server-view field '" + name + "'");
    }

    [TestCase("0.1", "0.2", true)]
    [TestCase("0.2", "0.2", false)]
    [TestCase("0.10", "0.2", false)]
    [TestCase("1.0", "0.99", false)]
    [TestCase("0.2b", "0.3", true)]
    [TestCase("0.3", "0.2b", false)]
    public void ForceUpdateVersionComparisonIsNumeric(string current, string minimum, bool expected)
    {
        var forceUpdate = FindGameType("ForceUpdate");
        var method = forceUpdate.GetMethod("IsOutdated", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method, "ForceUpdate.IsOutdated not found — renamed?");
        Assert.AreEqual(expected, (bool)method.Invoke(null, new object[] { current, minimum }));
    }

    [Test]
    public void GameEventsSeparatesMatchAndStatsSignals()
    {
        var events = FindGameType("GameEvents");
        Assert.IsNotNull(events.GetField("OnMatchEnded", BindingFlags.Public | BindingFlags.Static));
        Assert.IsNotNull(events.GetField("OnStatsChanged", BindingFlags.Public | BindingFlags.Static));
        Assert.IsNotNull(events.GetField("OnDailyStreak", BindingFlags.Public | BindingFlags.Static));
    }

    static string PrivateConst(string typeName, string fieldName)
    {
        var t = FindGameType(typeName);
        var f = t.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(f, typeName + "." + fieldName + " not found — renamed?");
        return (string)f.GetValue(null);
    }
}
