using System;
using NUnit.Framework;
using UnityEngine;

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
        Assert.IsNotNull(text);
        var manifest = JsonUtility.FromJson<Manifest>(text.text);
        AssertEntries(manifest.humans, 40);
        AssertEntries(manifest.groups, 8);
        AssertEntries(manifest.numbers, 10);
    }

    static void AssertEntries(Entry[] entries, int expectedCount)
    {
        Assert.AreEqual(expectedCount, entries.Length);
        foreach (var entry in entries)
            Assert.IsNotNull(Resources.Load<Sprite>(entry.resource),
                entry.resource + " did not import as a Sprite.");
    }
}
