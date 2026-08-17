using System;
using NUnit.Framework;
using UnityEngine;

// Regression coverage for the manifest-to-Resources Sprite import contract.
public class AvatarAssetTests
{
    [Serializable]
    class Entry
    {
        public string id;
        public string resource;
    }

    [Serializable]
    class Manifest
    {
        public Entry[] humans;
        public Entry[] groups;
        public Entry[] numbers;
    }

    [Test]
    public void EveryProfileAvatarImportsAsSprite()
    {
        var text = Resources.Load<TextAsset>("avatars/manifest");
        Assert.IsNotNull(text,
            "Avatar manifest TextAsset is missing at Resources path avatars/manifest.");
        var manifest = JsonUtility.FromJson<Manifest>(text.text);
        AssertEntries(manifest.humans, 40, "human");
        AssertEntries(manifest.groups, 8, "group");
        AssertEntries(manifest.numbers, 10, "number");
    }

    static void AssertEntries(Entry[] entries, int expectedCount, string category)
    {
        Assert.IsNotNull(entries, category + " avatar manifest entries must not be null.");
        Assert.AreEqual(expectedCount, entries.Length,
            category + " avatar manifest entry count is incorrect.");
        foreach (var entry in entries)
            Assert.IsNotNull(Resources.Load<Sprite>(entry.resource),
                category + " avatar " + entry.resource + " did not import as a Sprite.");
    }
}
