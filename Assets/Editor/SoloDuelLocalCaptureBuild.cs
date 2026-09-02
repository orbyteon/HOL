using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Builds the explicit Windows development player used for Solo visual
/// evidence. The capture driver lives in the PlayMode test assembly, so it is
/// present only when this method opts into IncludeTestAssemblies.
/// </summary>
public static class SoloDuelLocalCaptureBuild
{
    const string OutputEnvironment = "HOL_SOLO_WINDOWS_BUILD";
    const string AndroidOutputArgument = "-holSoloAndroidOutput";
    const string RequiredUnityVersion = "2022.3.62f3";
    const string CaptureScene = "Assets/Scenes/MainMenu.unity";
    const string CaptureExecutableName = "HOLSoloCapture.exe";
    const string ReceiptSuffix = ".hol-solo-build.json";

    static readonly BuildOptions CaptureBuildOptions =
        BuildOptions.Development | BuildOptions.IncludeTestAssemblies;

    [Serializable]
    sealed class FingerprintRecord
    {
        public int fileCount;
        public string sha256;
    }

    [Serializable]
    sealed class OutputFileRecord
    {
        public string path;
        public long length;
        public string sha256;
    }

    [Serializable]
    sealed class CaptureBuildReceipt
    {
        public int schemaVersion;
        public string kind;
        public string createdUtc;
        public string buildStartedUtc;
        public string buildCompletedUtc;
        public string unityVersion;
        public string editorExecutablePath;
        public string projectPath;
        public string scene;
        public string target;
        public int buildOptions;
        public bool developmentBuild;
        public bool includeTestAssemblies;
        public int unexpectedBuildOptions;
        public string scriptingDefines;
        public string companyName;
        public string productName;
        public bool outputDirectoryWasEmpty;
        public string executablePath;
        public long executableLength;
        public string executableSha256;
        public string outputDirectory;
        public int outputFileCount;
        public string outputManifestSha256;
        public OutputFileRecord[] outputFiles;
        public int sourceFileCount;
        public string sourceFingerprintSha256;
        public string buildResult;
        public int totalErrors;
        public int totalWarnings;
        public ulong totalSize;
        public string buildGuid;
    }

    public static void Build()
    {
        string output = Environment.GetEnvironmentVariable(OutputEnvironment);
        if (string.IsNullOrWhiteSpace(output))
            throw new InvalidOperationException(OutputEnvironment + " is not set.");

        BuildStandalone(output, false);
    }

    [MenuItem("HOL/Build Solo Duel Capture Player (GUI)...")]
    public static void BuildFromGui()
    {
        EnsureRequiredUnityVersion();

        string projectRoot = Path.GetFullPath(
            Path.GetDirectoryName(Application.dataPath) ??
            throw new InvalidOperationException(
                "Unable to resolve the project root."));
        DirectoryInfo projectDirectory = new DirectoryInfo(projectRoot);
        string initialDirectory = projectDirectory.Parent != null
            ? projectDirectory.Parent.FullName
            : projectRoot;

        string selectedDirectory = EditorUtility.OpenFolderPanel(
            "Select a new empty external Solo capture build directory",
            initialDirectory,
            string.Empty);
        if (string.IsNullOrWhiteSpace(selectedDirectory))
            return;

        selectedDirectory = Path.GetFullPath(selectedDirectory);
        EnsureExternalDirectory(selectedDirectory);
        if (Directory.GetFileSystemEntries(selectedDirectory).Length != 0)
        {
            throw new IOException(
                "The GUI capture build directory must be empty: " +
                selectedDirectory);
        }

        string output = Path.Combine(
            selectedDirectory, CaptureExecutableName);
        BuildStandalone(output, true);
        EditorUtility.RevealInFinder(output);
    }

    static void BuildStandalone(
        string rawOutput,
        bool requireEmptyDirectory)
    {
        EnsureRequiredUnityVersion();

        string output = Path.GetFullPath(rawOutput);
        string directory = Path.GetDirectoryName(output);
        if (string.IsNullOrWhiteSpace(directory))
            throw new InvalidOperationException("Capture build directory is invalid.");
        Directory.CreateDirectory(directory);

        if (requireEmptyDirectory &&
            Directory.GetFileSystemEntries(directory).Length != 0)
        {
            throw new IOException(
                "The GUI capture build directory is no longer empty: " +
                directory);
        }
        if (File.Exists(output))
            throw new IOException("Capture executable already exists: " + output);

        string receiptPath = GetReceiptPath(directory);
        if (File.Exists(receiptPath))
            throw new IOException("Capture build receipt already exists: " + receiptPath);

        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string sceneOnDisk = Path.Combine(
            projectRoot ?? throw new InvalidOperationException(
                "Unable to resolve the project root."),
            "Assets", "Scenes", "MainMenu.unity");
        if (!File.Exists(sceneOnDisk))
            throw new FileNotFoundException(
                "Solo capture scene is missing.", CaptureScene);

        string canonicalProjectRoot = Path.GetFullPath(projectRoot);
        EnsureOutsideProject(output, canonicalProjectRoot);
        FingerprintRecord sourceBefore = GetSourceFingerprint(
            canonicalProjectRoot);
        DateTime buildStartedUtc = DateTime.UtcNow;

        string originalCompanyName = PlayerSettings.companyName;
        string originalProductName = PlayerSettings.productName;
        BuildReport report;
        try
        {
            // The capture executable gets a separate Windows PlayerPrefs hive.
            // Even a crashed evidence process therefore cannot touch the real
            // HOL player's preferences on this workstation.
            PlayerSettings.companyName = "HOL QA";
            PlayerSettings.productName = "HOL Solo Capture";

            report = BuildPipeline.BuildPlayer(
                new BuildPlayerOptions
                {
                    scenes = new[] { CaptureScene },
                    locationPathName = output,
                    target = BuildTarget.StandaloneWindows64,
                    options = CaptureBuildOptions,
                });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Solo Duel local capture build failed: " +
                    report.summary.result);
            }

        }
        finally
        {
            PlayerSettings.companyName = originalCompanyName;
            PlayerSettings.productName = originalProductName;
        }

        if (!File.Exists(output))
            throw new FileNotFoundException(
                "Solo capture executable is missing after the build.",
                output);

        DateTime buildCompletedUtc = DateTime.UtcNow;
        FingerprintRecord sourceAfter = GetSourceFingerprint(
            canonicalProjectRoot);
        if (sourceAfter.fileCount != sourceBefore.fileCount ||
            !string.Equals(
                sourceAfter.sha256,
                sourceBefore.sha256,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Unity changed source inputs while building the Solo " +
                "capture player.");
        }

        OutputFileRecord[] outputFiles = GetOutputFiles(directory);
        string outputManifestSha256 = GetOutputManifestSha256(outputFiles);
        FileInfo executable = new FileInfo(output);
        CaptureBuildReceipt receipt = new CaptureBuildReceipt
        {
            schemaVersion = 1,
            kind = "hol-solo-gui-capture-build",
            createdUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            buildStartedUtc = buildStartedUtc.ToString(
                "o", CultureInfo.InvariantCulture),
            buildCompletedUtc = buildCompletedUtc.ToString(
                "o", CultureInfo.InvariantCulture),
            unityVersion = Application.unityVersion,
            editorExecutablePath = Path.GetFullPath(
                EditorApplication.applicationPath),
            projectPath = canonicalProjectRoot,
            scene = CaptureScene,
            target = BuildTarget.StandaloneWindows64.ToString(),
            buildOptions = (int)CaptureBuildOptions,
            developmentBuild =
                (CaptureBuildOptions & BuildOptions.Development) != 0,
            includeTestAssemblies =
                (CaptureBuildOptions & BuildOptions.IncludeTestAssemblies) != 0,
            unexpectedBuildOptions = (int)(CaptureBuildOptions &
                ~(BuildOptions.Development |
                  BuildOptions.IncludeTestAssemblies)),
            scriptingDefines = PlayerSettings.GetScriptingDefineSymbols(
                NamedBuildTarget.Standalone),
            companyName = "HOL QA",
            productName = "HOL Solo Capture",
            outputDirectoryWasEmpty = requireEmptyDirectory,
            executablePath = output,
            executableLength = executable.Length,
            executableSha256 = GetFileSha256(output),
            outputDirectory = directory,
            outputFileCount = outputFiles.Length,
            outputManifestSha256 = outputManifestSha256,
            outputFiles = outputFiles,
            sourceFileCount = sourceBefore.fileCount,
            sourceFingerprintSha256 = sourceBefore.sha256,
            buildResult = report.summary.result.ToString(),
            totalErrors = report.summary.totalErrors,
            totalWarnings = report.summary.totalWarnings,
            totalSize = report.summary.totalSize,
            buildGuid = report.summary.guid.ToString(),
        };
        WriteReceiptAtomically(receiptPath, receipt);

        Debug.Log(
            "HOL_SOLO_WINDOWS_BUILD_READY " + output +
            " RECEIPT " + receiptPath);
    }

    static void EnsureRequiredUnityVersion()
    {
        if (!string.Equals(
                Application.unityVersion,
                RequiredUnityVersion,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Solo capture requires Unity " + RequiredUnityVersion +
                "; found " + Application.unityVersion + ".");
        }
    }

    static void EnsureExternalDirectory(string directory)
    {
        string projectRoot = Path.GetFullPath(
            Path.GetDirectoryName(Application.dataPath) ??
            throw new InvalidOperationException(
                "Unable to resolve the project root."));
        EnsureOutsideProject(directory, projectRoot);
    }

    static void EnsureOutsideProject(string candidate, string projectRoot)
    {
        string projectPrefix = projectRoot.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string candidateFull = Path.GetFullPath(candidate);
        if (string.Equals(
                candidateFull.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar),
                projectRoot.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase) ||
            candidateFull.StartsWith(
                projectPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Solo capture output must be outside the Unity project.");
        }
    }

    static string GetReceiptPath(string outputDirectory)
    {
        DirectoryInfo directory = new DirectoryInfo(outputDirectory);
        string parent = directory.Parent != null
            ? directory.Parent.FullName
            : throw new InvalidOperationException(
                "Capture build directory cannot be a filesystem root.");
        return Path.Combine(parent, directory.Name + ReceiptSuffix);
    }

    static FingerprintRecord GetSourceFingerprint(string root)
    {
        HashSet<string> excludedDirectories = new HashSet<string>(
            new[]
            {
                ".git", ".vs", "Library", "Temp", "Logs", "obj",
                "artifacts", "Build", "Builds", "UserSettings",
                "MemoryCaptures", "Recordings",
            },
            StringComparer.OrdinalIgnoreCase);
        HashSet<string> excludedExtensions = new HashSet<string>(
            new[] { ".csproj", ".sln", ".user", ".pidb", ".booproj" },
            StringComparer.OrdinalIgnoreCase);
        string prefix = root.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        List<string> records = new List<string>();
        foreach (string file in Directory.GetFiles(
                     root, "*", SearchOption.AllDirectories))
        {
            string relative = file.Substring(prefix.Length).Replace('\\', '/');
            int separator = relative.IndexOf('/');
            string top = separator >= 0
                ? relative.Substring(0, separator)
                : relative;
            if (excludedDirectories.Contains(top) ||
                excludedExtensions.Contains(Path.GetExtension(file)))
            {
                continue;
            }

            FileInfo info = new FileInfo(file);
            records.Add(
                relative + "|" +
                info.Length.ToString(CultureInfo.InvariantCulture) + "|" +
                GetFileSha256(file));
        }

        records.Sort(StringComparer.Create(
            CultureInfo.GetCultureInfo("en-CY"), true));
        return new FingerprintRecord
        {
            fileCount = records.Count,
            sha256 = GetStringSha256(string.Join("\n", records.ToArray())),
        };
    }

    static OutputFileRecord[] GetOutputFiles(string root)
    {
        string prefix = root.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string[] files = Directory.GetFiles(
            root, "*", SearchOption.AllDirectories);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        List<OutputFileRecord> records = new List<OutputFileRecord>();
        foreach (string file in files)
        {
            FileInfo info = new FileInfo(file);
            records.Add(new OutputFileRecord
            {
                path = file.Substring(prefix.Length).Replace('\\', '/'),
                length = info.Length,
                sha256 = GetFileSha256(file),
            });
        }
        records.Sort((left, right) =>
            StringComparer.Ordinal.Compare(left.path, right.path));
        return records.ToArray();
    }

    static string GetOutputManifestSha256(OutputFileRecord[] records)
    {
        List<string> lines = new List<string>();
        foreach (OutputFileRecord record in records)
        {
            lines.Add(
                record.path + "|" +
                record.length.ToString(CultureInfo.InvariantCulture) + "|" +
                record.sha256);
        }
        return GetStringSha256(string.Join("\n", lines.ToArray()));
    }

    static string GetFileSha256(string path)
    {
        using (SHA256 algorithm = SHA256.Create())
        using (FileStream stream = File.OpenRead(path))
        {
            return ToLowerHex(algorithm.ComputeHash(stream));
        }
    }

    static string GetStringSha256(string value)
    {
        using (SHA256 algorithm = SHA256.Create())
        {
            return ToLowerHex(algorithm.ComputeHash(
                new UTF8Encoding(false).GetBytes(value)));
        }
    }

    static string ToLowerHex(byte[] bytes)
    {
        return BitConverter.ToString(bytes)
            .Replace("-", string.Empty)
            .ToLowerInvariant();
    }

    static void WriteReceiptAtomically(
        string receiptPath,
        CaptureBuildReceipt receipt)
    {
        string temporary = receiptPath + ".tmp";
        if (File.Exists(temporary))
            throw new IOException("Capture receipt temporary file exists: " + temporary);

        try
        {
            File.WriteAllText(
                temporary,
                JsonUtility.ToJson(receipt, true),
                new UTF8Encoding(false));
            File.Move(temporary, receiptPath);
        }
        catch
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
            throw;
        }
    }

    /// <summary>
    /// Explicit local Android compile/build smoke. The output must be supplied
    /// outside the repository. This lane intentionally excludes test
    /// assemblies, so the standalone-only capture fixture cannot enter an
    /// Android player.
    /// </summary>
    public static void BuildAndroidSmoke()
    {
        string rawOutput = ReadArgument(AndroidOutputArgument);
        if (string.IsNullOrWhiteSpace(rawOutput) ||
            !Path.IsPathRooted(rawOutput))
        {
            throw new InvalidOperationException(
                AndroidOutputArgument + " must name an explicit absolute APK path.");
        }

        string output = Path.GetFullPath(rawOutput);
        if (!string.Equals(
                Path.GetExtension(output), ".apk",
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Android smoke output must be an APK.");

        string projectRoot = Path.GetFullPath(
            Path.GetDirectoryName(Application.dataPath) ??
            throw new InvalidOperationException(
                "Unable to resolve the project root."));
        string projectPrefix = projectRoot.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (output.StartsWith(projectPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Android smoke output must be outside the Unity repository.");
        }
        if (File.Exists(output))
            throw new IOException("Android smoke output already exists: " + output);

        string directory = Path.GetDirectoryName(output);
        if (string.IsNullOrWhiteSpace(directory))
            throw new InvalidOperationException("Android smoke directory is invalid.");
        Directory.CreateDirectory(directory);

        BuildTarget originalTarget = EditorUserBuildSettings.activeBuildTarget;
        BuildTargetGroup originalGroup = BuildPipeline.GetBuildTargetGroup(
            originalTarget);
        bool originalBuildAppBundle = EditorUserBuildSettings.buildAppBundle;

        try
        {
            if (originalTarget != BuildTarget.Android &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Android, BuildTarget.Android))
            {
                throw new InvalidOperationException(
                    "Unable to switch to the Android build target.");
            }

            // BuildPlayerOptions carries Development locally. No PlayerSettings
            // value is changed; the one temporary Android editor flag is
            // restored in finally along with the active target.
            EditorUserBuildSettings.buildAppBundle = false;
            BuildReport report = BuildPipeline.BuildPlayer(
                new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/Scenes/MainMenu.unity" },
                    locationPathName = output,
                    target = BuildTarget.Android,
                    options = BuildOptions.Development,
                });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Solo Android smoke build failed: " +
                    report.summary.result);
            }

            Debug.Log("HOL_SOLO_ANDROID_SMOKE_READY " + output);
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = originalBuildAppBundle;
            if (originalTarget != BuildTarget.NoTarget &&
                EditorUserBuildSettings.activeBuildTarget != originalTarget)
            {
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                        originalGroup, originalTarget))
                {
                    Debug.LogError(
                        "HOL_SOLO_ANDROID_SMOKE_TARGET_RESTORE_FAILED " +
                        originalTarget);
                }
            }
        }
    }

    static string ReadArgument(string key)
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int index = 0; index + 1 < arguments.Length; index++)
        {
            if (string.Equals(
                    arguments[index], key,
                    StringComparison.OrdinalIgnoreCase))
                return arguments[index + 1];
        }
        return null;
    }
}
