using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

public sealed class LegacyAssetDependencyTests
{
    static readonly string[] LegacyPhotoAssets =
    {
        "Assets/Photos/1000032794.png",
        "Assets/Photos/HOLSPASH.png",
        "Assets/Photos/file_0000000084c4724399e7ba24d1cfdd36.png",
        "Assets/Photos/file_00000000a7a072438484999e9b21581a.png",
        "Assets/Photos/quit.png",
        "Assets/Photos/yournumber.png"
    };

    [Test]
    public void LegacyPhotoAssetsHaveNoSerializedProjectDependants()
    {
        var liveLegacy = new HashSet<string>();
        foreach (string path in LegacyPhotoAssets)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                liveLegacy.Add(path);
        }

        if (liveLegacy.Count == 0)
            Assert.Pass("Retired Assets/Photos artwork is already absent.");

        var offenders = new List<string>();
        foreach (string sourcePath in AssetDatabase.GetAllAssetPaths())
        {
            if (!sourcePath.StartsWith("Assets/")) continue;
            if (sourcePath.StartsWith("Assets/Photos/")) continue;
            if (AssetDatabase.IsValidFolder(sourcePath)) continue;

            string[] dependencies = AssetDatabase.GetDependencies(sourcePath, true);
            foreach (string dependency in dependencies)
            {
                if (liveLegacy.Contains(dependency))
                    offenders.Add(sourcePath + " -> " + dependency);
            }
        }

        Assert.That(offenders, Is.Empty,
            "Retired Assets/Photos artwork still has serialized project dependants:\n" +
            string.Join("\n", offenders));
    }
}
