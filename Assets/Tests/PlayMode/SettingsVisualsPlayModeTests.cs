using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class SettingsVisualsPlayModeTests
{
    [UnityTest]
    public IEnumerator SettingsReusesRealControlsWithApprovedSpritesOnly()
    {
        bool hadLanguage = PlayerPrefs.HasKey("Language");
        int oldLanguage = PlayerPrefs.GetInt("Language", 0);
        bool hadDifficulty = PlayerPrefs.HasKey("AIDifficulty");
        int oldDifficulty = PlayerPrefs.GetInt("AIDifficulty", 1);
        bool hadMusic = PlayerPrefs.HasKey("MusicOn");
        int oldMusic = PlayerPrefs.GetInt("MusicOn", 1);
        bool hadName = PlayerPrefs.HasKey("PlayerName");
        string oldName = PlayerPrefs.GetString("PlayerName", "");

        try
        {
            PlayerPrefs.SetString("PlayerName", "VisualTester");
            PlayerPrefs.SetInt("Language", 0);
            PlayerPrefs.SetInt("AIDifficulty", 1);
            PlayerPrefs.SetInt("MusicOn", 1);
            PlayerPrefs.Save();

            InvokeInstaller(RuntimeType("SettingsVisuals"));
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            for (int i = 0; i < 36; i++) yield return null;

            var menu = UnityEngine.Object.FindObjectOfType(
                RuntimeType("MenuManager")) as Component;
            Assert.That(menu, Is.Not.Null);
            menu.SendMessage("OpenSettings", SendMessageOptions.RequireReceiver);
            for (int i = 0; i < 8; i++) yield return null;

            GameObject settings = (GameObject)Field(menu, "settingsPanel");
            GameObject main = (GameObject)Field(menu, "mainMenuPanel");
            Assert.That(settings.activeSelf, Is.True);
            Assert.That(main.activeSelf, Is.False);

            Transform root = Find(settings.transform, "SettingsVisualRoot");
            Transform safe = Find(root, "SettingsSafeRoot");
            Assert.That(root, Is.Not.Null);
            Assert.That(safe, Is.Not.Null);
            Assert.That(Find(root, "SettingsReferenceBackground"), Is.Not.Null);
            Assert.That(Find(root, "SettingsLogo"), Is.Not.Null);
            Assert.That(Find(root, "SettingsReferenceTitle"), Is.Not.Null);
            Assert.That(Find(root, "SettingsReferenceShell"), Is.Not.Null);
            Assert.That(Find(root, "SettingsMascotSix"), Is.Not.Null);
            Assert.That(Find(root, "SettingsMascotSeven"), Is.Not.Null);
            Assert.That(Find(root, "SettingsPlayerChip"), Is.Not.Null);
            Assert.That(Find(root, "SettingsTitleStarLeft"), Is.Null,
                "Settings title must not recreate procedural star decoration.");
            Assert.That(Find(root, "SettingsTitleStarRight"), Is.Null);

            foreach (string retiredType in new[]
            {
                "SettingsSurfaceGraphic",
                "SettingsIconGraphic",
                "SettingsToggleGraphic"
            })
            {
                Assert.That(Type.GetType(retiredType + ", Assembly-CSharp"), Is.Null,
                    "Retired procedural Settings type returned: " + retiredType);
            }

            string[] rows =
            {
                "SettingsNameRow", "SettingsLanguageRow", "SettingsMusicRow",
                "SettingsDifficultyRow", "SettingsPrivacyRow"
            };
            foreach (string rowName in rows)
            {
                var image = Find(root, rowName).GetComponent<Image>();
                Assert.That(image, Is.Not.Null, rowName);
                Assert.That(image.sprite.name, Is.EqualTo("mainmenu_tip_frame_9s"));
                Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
                Assert.That(image.color.a, Is.EqualTo(1f).Within(0.001f));
            }

            string[] rowIcons =
            {
                "SettingsNameRowIcon", "SettingsLanguageRowIcon",
                "SettingsMusicRowIcon", "SettingsDifficultyRowIcon",
                "SettingsPrivacyRowIcon"
            };
            foreach (string iconName in rowIcons)
            {
                var image = Find(root, iconName).GetComponent<Image>();
                Assert.That(image, Is.Not.Null, iconName);
                Assert.That(image.sprite, Is.Not.Null, iconName);
                Assert.That(image.color.a, Is.EqualTo(1f).Within(0.001f), iconName);
                Assert.That(image.preserveAspect, Is.True, iconName);
            }

            Assert.That(CountNamed<Button>(settings.transform, "Buttonsave"), Is.EqualTo(1));
            Assert.That(CountNamed<Button>(settings.transform, "EnglishButton"), Is.EqualTo(1));
            Assert.That(CountNamed<Button>(settings.transform, "GreekButton"), Is.EqualTo(1));
            Assert.That(CountNamed<Toggle>(settings.transform, "Toggle"), Is.EqualTo(1));
            Assert.That(CountNamed<Button>(settings.transform, "AdsPrivacyButton"), Is.EqualTo(1));
            for (int i = 0; i < 4; i++)
                Assert.That(CountNamed<Button>(settings.transform, "Difficulty" + i),
                    Is.EqualTo(1));

            var back = Find(root, "Buttonback").GetComponent<Button>();
            var save = Find(root, "Buttonsave").GetComponent<Button>();
            var english = Find(root, "EnglishButton").GetComponent<Button>();
            var greek = Find(root, "GreekButton").GetComponent<Button>();
            var privacy = Find(root, "AdsPrivacyButton").GetComponent<Button>();
            AssertProductionButton(back, "mainmenu_tip_frame_9s");
            AssertProductionButton(save, "mainmenu_cta_blue_9s");
            AssertProductionButton(english, "mainmenu_cta_gold_9s");
            AssertProductionButton(greek, "mainmenu_tip_frame_9s");
            AssertProductionButton(privacy, "mainmenu_cta_blue_9s");
            for (int i = 0; i < 4; i++)
                AssertProductionButton(
                    Find(root, "Difficulty" + i).GetComponent<Button>(),
                    i == 1 ? "mainmenu_cta_gold_9s" : "mainmenu_tip_frame_9s");

            Assert.That(save.GetComponent(RuntimeType("SettingsButtonFeedback")), Is.Not.Null);
            Assert.That(Find(save.transform, "SettingsButtonStateOverlay"), Is.Not.Null);

            AssertDescendant(Find(root, "InputField (TMP)"), "SettingsNameRow");
            AssertDescendant(Find(root, "Buttonsave"), "SettingsNameRow");
            AssertDescendant(Find(root, "EnglishButton"), "SettingsLanguageRow");
            AssertDescendant(Find(root, "GreekButton"), "SettingsLanguageRow");
            AssertDescendant(Find(root, "Toggle"), "SettingsMusicRow");
            AssertDescendant(Find(root, "Difficulty3"), "SettingsDifficultyRow");
            AssertDescendant(Find(root, "AdsPrivacyButton"), "SettingsPrivacyRow");
            AssertDescendant(Find(root, "Buttonback"), "SettingsSafeRoot");

            var nameInputRect = Find(root, "InputField (TMP)") as RectTransform;
            var saveRect = Find(root, "Buttonsave") as RectTransform;
            Assert.That(Overlaps(nameInputRect, saveRect), Is.False,
                "Name input and Save must not overlap.");

            var inputImage = Find(root, "InputField (TMP)").GetComponent<Image>();
            Assert.That(inputImage.sprite.name, Is.EqualTo("mainmenu_tip_frame_9s"));
            Assert.That(inputImage.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(inputImage.color.a, Is.EqualTo(1f).Within(0.001f));
            Assert.That(Find(root, "SettingsInputSurface"), Is.Null);

            var chipImage = Find(root, "SettingsPlayerChip").GetComponent<Image>();
            Assert.That(chipImage.sprite.name,
                Is.EqualTo("mainmenu_player_chip_frame_9s"));
            Assert.That(chipImage.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(chipImage.color.a, Is.EqualTo(1f).Within(0.001f));

            // Every normal-state production sprite is actually visible. State
            // overlays are the only intentionally transparent Images.
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == null || image.name == "SettingsButtonStateOverlay")
                    continue;
                Assert.That(image.color.a, Is.GreaterThanOrEqualTo(0.99f),
                    image.name + " hides approved Settings artwork.");
            }

            var chipName = Find(root, "PlayerName").GetComponent<TMP_Text>();
            Assert.That(chipName.text, Is.EqualTo("VisualTester"));

            greek.onClick.Invoke();
            yield return null;
            Assert.That(PlayerPrefs.GetInt("Language", -1), Is.EqualTo(1),
                "Moved language control lost its real callback.");
            var saveLabel = save.GetComponentInChildren<TMP_Text>(true);
            Assert.That(saveLabel.text, Is.EqualTo("ΑΠΟΘΗΚΕΥΣΗ"));
            AssertProductionButton(greek, "mainmenu_cta_gold_9s");
            AssertProductionButton(english, "mainmenu_tip_frame_9s");

            var adaptive = Find(root, "Difficulty3").GetComponent<Button>();
            adaptive.onClick.Invoke();
            yield return null;
            Assert.That(PlayerPrefs.GetInt("AIDifficulty", -1), Is.EqualTo(3),
                "Moved difficulty control lost its real callback.");
            AssertProductionButton(adaptive, "mainmenu_cta_gold_9s");

            var music = Find(root, "Toggle").GetComponent<Toggle>();
            music.isOn = false;
            yield return new WaitForSecondsRealtime(0.30f);
            Assert.That(PlayerPrefs.GetInt("MusicOn", -1), Is.EqualTo(0),
                "Moved music control lost its real callback.");
            Assert.That(music.GetComponent<Image>().sprite.name,
                Is.EqualTo("mainmenu_tip_frame_9s"));
            music.isOn = true;
            yield return new WaitForSecondsRealtime(0.30f);
            Assert.That(music.GetComponent<Image>().sprite.name,
                Is.EqualTo("mainmenu_cta_gold_9s"));

            var nameInput = Find(root, "InputField (TMP)")
                .GetComponent<TMP_InputField>();
            nameInput.SetTextWithoutNotify("ContractTester");
            save.onClick.Invoke();
            yield return null;
            Assert.That(PlayerPrefs.GetString("PlayerName", ""),
                Is.EqualTo("ContractTester"),
                "Production Save lost its existing callback.");

            back.onClick.Invoke();
            yield return null;
            Assert.That(settings.activeSelf, Is.False,
                "Settings Back lost MenuManager navigation.");
            Assert.That(main.activeSelf, Is.True);
        }
        finally
        {
            Restore("Language", hadLanguage, oldLanguage);
            Restore("AIDifficulty", hadDifficulty, oldDifficulty);
            Restore("MusicOn", hadMusic, oldMusic);
            if (hadName) PlayerPrefs.SetString("PlayerName", oldName);
            else PlayerPrefs.DeleteKey("PlayerName");
            PlayerPrefs.Save();
        }
    }

    static void Restore(string key, bool existed, int value)
    {
        if (existed) PlayerPrefs.SetInt(key, value);
        else PlayerPrefs.DeleteKey(key);
    }

    static void AssertProductionButton(Button button, string expectedSprite)
    {
        Assert.That(button, Is.Not.Null);
        var image = button.GetComponent<Image>();
        Assert.That(image, Is.Not.Null, button.name);
        Assert.That(image.sprite, Is.Not.Null, button.name);
        Assert.That(image.sprite.name, Is.EqualTo(expectedSprite), button.name);
        Assert.That(image.color.a, Is.EqualTo(1f).Within(0.001f), button.name);
        Assert.That(image.type, Is.EqualTo(Image.Type.Sliced), button.name);
        Assert.That(image.pixelsPerUnitMultiplier, Is.EqualTo(2f), button.name);
        Assert.That(button.targetGraphic, Is.SameAs(image), button.name);

        Transform overlayTransform = Find(button.transform, "SettingsButtonStateOverlay");
        Assert.That(overlayTransform, Is.Not.Null, button.name);
        var overlay = overlayTransform.GetComponent<Image>();
        Assert.That(overlay, Is.Not.Null);
        Assert.That(overlay.sprite, Is.SameAs(image.sprite));
        Assert.That(overlay.raycastTarget, Is.False);
        Assert.That(overlay.color.a, Is.EqualTo(0f).Within(0.001f));
    }

    static void AssertDescendant(Transform child, string ancestorName)
    {
        Assert.That(child, Is.Not.Null);
        Transform current = child.parent;
        while (current != null && current.name != ancestorName)
            current = current.parent;
        Assert.That(current, Is.Not.Null,
            child.name + " must remain under " + ancestorName + ".");
    }

    static int CountNamed<T>(Transform root, string name) where T : Component
    {
        int count = 0;
        foreach (var item in root.GetComponentsInChildren<T>(true))
            if (item.name == name) count++;
        return count;
    }

    static bool Overlaps(RectTransform a, RectTransform b)
    {
        var aCorners = new Vector3[4];
        var bCorners = new Vector3[4];
        a.GetWorldCorners(aCorners);
        b.GetWorldCorners(bCorners);
        Rect ar = Rect.MinMaxRect(aCorners[0].x, aCorners[0].y,
            aCorners[2].x, aCorners[2].y);
        Rect br = Rect.MinMaxRect(bCorners[0].x, bCorners[0].y,
            bCorners[2].x, bCorners[2].y);
        return ar.Overlaps(br);
    }

    static object Field(Component component, string name)
    {
        FieldInfo field = component.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        return field.GetValue(component);
    }

    static void InvokeInstaller(Type type)
    {
        MethodInfo install = type.GetMethod("Install",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(install, Is.Not.Null);
        install.Invoke(null, null);
    }

    static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = Find(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    static Type RuntimeType(string name)
    {
        Type type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "Missing runtime component: " + name);
        return type;
    }
}
