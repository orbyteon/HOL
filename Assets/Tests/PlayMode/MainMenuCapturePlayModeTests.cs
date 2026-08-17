using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class MainMenuCapturePlayModeTests
{
    [TestCase(true, true, "en", true)]
    [TestCase(true, true, "el", true)]
    [TestCase(false, true, "en", false)]
    [TestCase(true, false, "en", false)]
    [TestCase(true, true, null, false)]
    [TestCase(true, true, "", false)]
    [TestCase(true, true, "EN", false)]
    [TestCase(true, true, "fr", false)]
    public void CaptureRequiresDevelopmentAndroidAndSupportedIntent(
        bool isAndroid, bool isDevelopment, string extra, bool expected)
    {
        var method = CaptureType().GetMethod("ShouldCapture",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);

        bool actual = (bool)method.Invoke(null,
            new object[] { isAndroid, isDevelopment, extra });

        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase("en", "English")]
    [TestCase("el", "Greek")]
    public void ApplyingCaptureSeedsConsentAndRequestedLanguage(
        string extra, string expectedLanguage)
    {
        var prefs = SnapshotPrefs();
        var l10nType = RuntimeType("L10n");
        try
        {
            PlayerPrefs.DeleteKey("AdsConsent");
            InvokeApplyCapture(extra);

            Assert.That(PlayerPrefs.HasKey("AdsConsent"), Is.True);
            Assert.That(PlayerPrefs.GetInt("AdsConsent"), Is.Zero);
            Assert.That(CurrentLanguageName(l10nType), Is.EqualTo(expectedLanguage));
        }
        finally
        {
            RestorePrefs(prefs);
        }
    }

    [Test]
    public void ApplyingCaptureWithoutIntentHasNoEffect()
    {
        var prefs = SnapshotPrefs();
        var l10nType = RuntimeType("L10n");
        try
        {
            PlayerPrefs.SetInt("AdsConsent", 1);
            SetLanguage(l10nType, "Greek");

            InvokeApplyCapture(null);

            Assert.That(PlayerPrefs.GetInt("AdsConsent"), Is.EqualTo(1));
            Assert.That(CurrentLanguageName(l10nType), Is.EqualTo("Greek"));
        }
        finally
        {
            RestorePrefs(prefs);
        }
    }

    [UnityTest]
    public IEnumerator ReadyMarkerWaitsForReadyOwnedHomeAndUsesExactLanguage()
    {
        var ownerType = RuntimeType("MainMenuAuthoritativeVisuals");
        PropertyInfo isReady = ownerType.GetProperty("IsReady",
            BindingFlags.Instance | BindingFlags.Public);
        PropertyInfo ownsHome = ownerType.GetProperty("OwnsHome",
            BindingFlags.Instance | BindingFlags.Public);
        Assert.That(isReady, Is.Not.Null);
        Assert.That(ownsHome, Is.Not.Null);
        MethodInfo setReady = isReady.GetSetMethod(true);
        Assert.That(setReady, Is.Not.Null);

        var wait = CaptureType().GetMethod("WaitForReady",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(wait, Is.Not.Null);
        var routine = wait.Invoke(null, new object[] { "el" }) as IEnumerator;
        Assert.That(routine, Is.Not.Null);

        const string marker = "HOL_MAINMENU_CAPTURE_READY:el";
        int markerCount = 0;
        Application.LogCallback captureLog = (condition, stackTrace, type) =>
        {
            if (condition == marker)
                markerCount++;
        };

        Component previousOwner = FindComponent(ownerType);
        GameObject previousOwnerObject =
            previousOwner == null ? null : previousOwner.gameObject;
        bool previousOwnerWasActive =
            previousOwnerObject != null && previousOwnerObject.activeSelf;
        if (previousOwnerObject != null)
            previousOwnerObject.SetActive(false);

        GameObject ownerObject = new GameObject("TestMainMenuOwner");
        var owner = ownerObject.AddComponent(ownerType) as Behaviour;
        Assert.That(owner, Is.Not.Null);
        owner.enabled = false;
        Assert.That((bool)isReady.GetValue(owner, null), Is.False);

        GameObject menuObject = null;
        GameObject home = null;
        Application.logMessageReceived += captureLog;
        try
        {
            Assert.That(routine.MoveNext(), Is.True);
            yield return routine.Current;
            Assert.That(markerCount, Is.Zero,
                "Capture must not report ready while its owner is not ready.");

            var menuType = RuntimeType("MenuManager");
            menuObject = new GameObject("TestMenuManager");
            var menu = menuObject.AddComponent(menuType) as Component;
            Assert.That(menu, Is.Not.Null);
            home = new GameObject("TestMainMenuPanel");
            home.transform.SetParent(menuObject.transform, false);
            home.SetActive(false);

            var mainMenuPanel = menuType.GetField("mainMenuPanel",
                BindingFlags.Instance | BindingFlags.Public);
            var ownerMenu = ownerType.GetField("menu",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(mainMenuPanel, Is.Not.Null);
            Assert.That(ownerMenu, Is.Not.Null);
            mainMenuPanel.SetValue(menu, home);
            ownerMenu.SetValue(owner, menu);
            setReady.Invoke(owner, new object[] { true });

            Assert.That(routine.MoveNext(), Is.True);
            yield return routine.Current;
            Assert.That(markerCount, Is.Zero,
                "Capture must not report ready while Home is not owned.");

            home.SetActive(true);
            Assert.That((bool)ownsHome.GetValue(owner, null), Is.True);
            for (int frame = 0; frame < 10 && routine.MoveNext(); frame++)
                yield return routine.Current;

            Assert.That(markerCount, Is.EqualTo(1));
        }
        finally
        {
            Application.logMessageReceived -= captureLog;
            if (home != null)
                Object.Destroy(home);
            if (menuObject != null)
                Object.Destroy(menuObject);
            if (ownerObject != null)
                Object.Destroy(ownerObject);
            if (previousOwnerObject != null)
                previousOwnerObject.SetActive(previousOwnerWasActive);
        }
    }

    static System.Type CaptureType()
    {
        return RuntimeType("MainMenuCaptureBootstrap");
    }

    static System.Type RuntimeType(string name)
    {
        var type = System.Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "Missing runtime type: " + name);
        return type;
    }

    static void InvokeApplyCapture(string extra)
    {
        var method = CaptureType().GetMethod("ApplyCapture",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(null, new object[] { extra });
    }

    static string CurrentLanguageName(System.Type l10nType)
    {
        var current = l10nType.GetProperty("Current",
            BindingFlags.Static | BindingFlags.Public);
        Assert.That(current, Is.Not.Null);
        return current.GetValue(null, null).ToString();
    }

    static void SetLanguage(System.Type l10nType, string language)
    {
        var languageType = l10nType.GetNestedType("Language", BindingFlags.Public);
        var setLanguage = l10nType.GetMethod("SetLanguage",
            BindingFlags.Static | BindingFlags.Public);
        Assert.That(languageType, Is.Not.Null);
        Assert.That(setLanguage, Is.Not.Null);
        setLanguage.Invoke(null,
            new[] { System.Enum.Parse(languageType, language) });
    }

    static Component FindComponent(System.Type type)
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = root.GetComponentInChildren(type, true) as Component;
            if (found != null)
                return found;
        }
        return null;
    }

    struct PrefSnapshot
    {
        public string key;
        public bool had;
        public int value;
    }

    static PrefSnapshot[] SnapshotPrefs()
    {
        string[] keys = { "AdsConsent", "Language" };
        var snapshots = new PrefSnapshot[keys.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            snapshots[i] = new PrefSnapshot
            {
                key = keys[i],
                had = PlayerPrefs.HasKey(keys[i]),
                value = PlayerPrefs.GetInt(keys[i], 0)
            };
        }
        return snapshots;
    }

    static void RestorePrefs(PrefSnapshot[] snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            if (snapshot.had)
                PlayerPrefs.SetInt(snapshot.key, snapshot.value);
            else
                PlayerPrefs.DeleteKey(snapshot.key);
        }
        PlayerPrefs.Save();
    }
}
