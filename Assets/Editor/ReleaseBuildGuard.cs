using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Release-only build contract. CI passes -holReleaseBuild for signed AABs;
// ordinary editor/debug builds are deliberately untouched.
public sealed class ReleaseBuildGuard : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (!HasArgument("-holReleaseBuild"))
            return;

        if (report.summary.platform != BuildTarget.Android)
            throw new BuildFailedException("HOL release builds must target Android.");

        if ((report.summary.options & BuildOptions.Development) != 0)
            throw new BuildFailedException("HOL release builds must not use Development Build.");

        string error;
        if (!ReleaseConfig.IsConfigured(out error))
            throw new BuildFailedException("HOL release configuration is invalid: " + error);

        string identifier = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        if (!string.Equals(identifier, "com.Orbyteon.HOL", StringComparison.Ordinal))
            throw new BuildFailedException("Unexpected Android application identifier: " + identifier);

        if (PlayerSettings.Android.targetSdkVersion != AndroidSdkVersions.AndroidApiLevel36)
            throw new BuildFailedException("HOL release builds must target Android API 36.");

        // GameCI supplies the keystore path/passwords immediately before the
        // BuildPipeline call. Its signing values only take effect when Unity's
        // Custom Keystore switch is enabled, so enable it for this build only.
        PlayerSettings.Android.useCustomKeystore = true;
    }

    static bool HasArgument(string expected)
    {
        foreach (string arg in Environment.GetCommandLineArgs())
            if (string.Equals(arg, expected, StringComparison.Ordinal))
                return true;
        return false;
    }
}
