using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Local-only entry point. Reuses ReleaseBuildGuard; does not edit CI guards,
// deploy a backend, publish to Play, or modify scenes and player preferences.
public static class HolLocalReleaseBuild
{
    const string PayloadPath = "Assets/Resources/HOLReleaseConfig.json";
    const string Package = "com.Orbyteon.HOL";
    static readonly string[] Scenes = { "Assets/Scenes/SplashScene.unity", "Assets/Scenes/MainMenu.unity" };

    [Serializable] public sealed class Configuration
    {
        public string playFabTitleId, provisioningUrl, version, outputAab;
        public long googleCloudProjectNumber;
        public int versionCode;
        public string uploadCertificateSha256;
    }
    [Serializable] sealed class Payload
    {
        public string playFabTitleId, provisioningUrl;
        public long googleCloudProjectNumber;
    }

    static string Required(string name)
    {
        string value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(value)) throw new BuildFailedException("Missing local build input: " + name);
        return value;
    }

    static Configuration ReadConfiguration()
    {
        string path = Path.GetFullPath(Required("HOL_LOCAL_AAB_CONFIG"));
        string project = Path.GetFullPath(Path.Combine(Application.dataPath, "..")) + Path.DirectorySeparatorChar;
        if (path.StartsWith(project, StringComparison.OrdinalIgnoreCase))
            throw new BuildFailedException("Keep local release configuration outside the Unity project.");
        var cfg = JsonUtility.FromJson<Configuration>(File.ReadAllText(path));
        if (cfg == null) throw new BuildFailedException("Invalid local release JSON.");
        string error;
        if (!ReleaseConfig.ValidateValues(cfg.playFabTitleId, cfg.provisioningUrl, cfg.googleCloudProjectNumber, out error))
            throw new BuildFailedException(error);
        if (cfg.playFabTitleId == PvpIsolatedPlaytest.TitleId)
            throw new BuildFailedException("An operator-only test Title is not a public HOL release target.");
        if (cfg.versionCode <= 0 || !Regex.IsMatch(cfg.version ?? "", @"^\d+\.\d+\.\d+$"))
            throw new BuildFailedException("Supply the verified available Play versionCode and semantic version.");
        if (!Regex.IsMatch(cfg.uploadCertificateSha256 ?? "", @"^[0-9A-Fa-f]{64}$"))
            throw new BuildFailedException("Supply the Play Console upload certificate SHA-256.");
        if (!Path.IsPathRooted(cfg.outputAab ?? "")) throw new BuildFailedException("AAB output must be absolute.");
        string output = Path.GetFullPath(cfg.outputAab);
        if (!output.EndsWith(".aab", StringComparison.OrdinalIgnoreCase) ||
            output.StartsWith(project, StringComparison.OrdinalIgnoreCase) || File.Exists(output))
            throw new BuildFailedException("Use a new .aab output outside the project; never overwrite a delivery.");
        if (Application.unityVersion != "2022.3.62f3") throw new BuildFailedException("Use Unity 2022.3.62f3.");
        if (PvpIsolatedPlaytest.Enabled || PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android)
            .Split(';').Contains("HOL_PVP_ISOLATED_PLAYTEST"))
            throw new BuildFailedException("Remove isolated-test configuration before a release; never silently substitute it.");
        if (!EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).SequenceEqual(Scenes))
            throw new BuildFailedException("Expected SplashScene followed by MainMenu, with no fixture scenes.");
        return cfg;
    }

    public static void ValidatePublicConfiguration()
    {
        ReadConfiguration();
        Debug.Log("HOL_LOCAL_AAB_PUBLIC_PREFLIGHT_OK (not signing, live login, or build acceptance)");
    }

    static void VerifyUploadKey(string path, string alias, string password, string expected)
    {
        if (!File.Exists(path) || path.Contains('"') || alias.Contains('"') || alias.Contains('\n'))
            throw new BuildFailedException("Invalid local upload keystore path or alias.");
        string keytool = Path.Combine(EditorApplication.applicationContentsPath,
            "PlaybackEngines/AndroidPlayer/OpenJDK/bin/keytool.exe");
        var start = new System.Diagnostics.ProcessStartInfo(keytool,
            "-J-Duser.language=en -J-Duser.country=US -list -v -keystore \"" + path +
            "\" -alias \"" + alias + "\" -storepass:env HOL_LOCAL_CERT_CHECK_PASS")
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        start.EnvironmentVariables["HOL_LOCAL_CERT_CHECK_PASS"] = password;
        using (var process = System.Diagnostics.Process.Start(start))
        {
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(15000)) { process.Kill(); throw new BuildFailedException("Upload key verification timed out."); }
            string output = outputTask.GetAwaiter().GetResult();
            errorTask.GetAwaiter().GetResult(); // Never emit signing process output or credentials.
            Match match = Regex.Match(output, @"SHA256:\s*([0-9A-Fa-f:]+)");
            if (process.ExitCode != 0 || !output.Contains("PrivateKeyEntry") || !match.Success ||
                !string.Equals(match.Groups[1].Value.Replace(":", ""), expected, StringComparison.OrdinalIgnoreCase))
                throw new BuildFailedException("The private upload key could not be verified against Play Console.");
        }
    }

    public static void Build()
    {
        var cfg = ReadConfiguration();
        if (!Environment.GetCommandLineArgs().Contains("-holReleaseBuild"))
            throw new BuildFailedException("The existing -holReleaseBuild guard is mandatory for a local AAB.");
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android || EditorApplication.isPlaying)
            throw new BuildFailedException("Use the Android target, outside Play Mode.");
        string keyPath = Required("HOL_LOCAL_KEYSTORE_PATH");
        string alias = Required("HOL_LOCAL_KEY_ALIAS");
        string storePass = Required("HOL_LOCAL_STORE_PASS");
        string keyPass = Required("HOL_LOCAL_KEY_PASS");
        VerifyUploadKey(keyPath, alias, storePass, cfg.uploadCertificateSha256);

        byte[] originalPayload = File.ReadAllBytes(PayloadPath);
        const BuildTargetGroup group = BuildTargetGroup.Android;
        string originalPackage = PlayerSettings.GetApplicationIdentifier(group), originalVersion = PlayerSettings.bundleVersion;
        int originalCode = PlayerSettings.Android.bundleVersionCode;
        var originalBackend = PlayerSettings.GetScriptingBackend(group);
        var originalArchitectures = PlayerSettings.Android.targetArchitectures;
        var originalSdk = PlayerSettings.Android.targetSdkVersion;
        bool originalBundle = EditorUserBuildSettings.buildAppBundle, originalDevelopment = EditorUserBuildSettings.development;
        bool originalCustom = PlayerSettings.Android.useCustomKeystore;
        string originalKeyPath = PlayerSettings.Android.keystoreName, originalAlias = PlayerSettings.Android.keyaliasName;
        string originalStorePass = PlayerSettings.Android.keystorePass, originalKeyPass = PlayerSettings.Android.keyaliasPass;
        try
        {
            File.WriteAllText(PayloadPath, JsonUtility.ToJson(new Payload { playFabTitleId = cfg.playFabTitleId,
                provisioningUrl = cfg.provisioningUrl, googleCloudProjectNumber = cfg.googleCloudProjectNumber }, true));
            AssetDatabase.ImportAsset(PayloadPath, ImportAssetOptions.ForceSynchronousImport);
            ReleaseConfig.ResetForTests();
            PlayerSettings.SetApplicationIdentifier(group, Package);
            PlayerSettings.bundleVersion = cfg.version;
            PlayerSettings.Android.bundleVersionCode = cfg.versionCode;
            PlayerSettings.SetScriptingBackend(group, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keyPath;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keystorePass = storePass;
            PlayerSettings.Android.keyaliasPass = keyPass;
            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.development = false;
            Directory.CreateDirectory(Path.GetDirectoryName(cfg.outputAab));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = Scenes,
                target = BuildTarget.Android, locationPathName = cfg.outputAab, options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Local AAB build failed.");
            using (var sha = SHA256.Create())
                File.WriteAllText(cfg.outputAab + ".sha256", BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(cfg.outputAab))).Replace("-", ""));
            Debug.Log("HOL_LOCAL_AAB_BUILT; verify final signature and package before delivery. No upload performed.");
        }
        finally
        {
            File.WriteAllBytes(PayloadPath, originalPayload);
            AssetDatabase.ImportAsset(PayloadPath, ImportAssetOptions.ForceSynchronousImport);
            ReleaseConfig.ResetForTests();
            PlayerSettings.SetApplicationIdentifier(group, originalPackage);
            PlayerSettings.bundleVersion = originalVersion;
            PlayerSettings.Android.bundleVersionCode = originalCode;
            PlayerSettings.SetScriptingBackend(group, originalBackend);
            PlayerSettings.Android.targetArchitectures = originalArchitectures;
            PlayerSettings.Android.targetSdkVersion = originalSdk;
            EditorUserBuildSettings.buildAppBundle = originalBundle;
            EditorUserBuildSettings.development = originalDevelopment;
            PlayerSettings.Android.keystoreName = originalKeyPath;
            PlayerSettings.Android.keyaliasName = originalAlias;
            PlayerSettings.Android.keystorePass = originalStorePass;
            PlayerSettings.Android.keyaliasPass = originalKeyPass;
            PlayerSettings.Android.useCustomKeystore = originalCustom;
        }
    }
}
