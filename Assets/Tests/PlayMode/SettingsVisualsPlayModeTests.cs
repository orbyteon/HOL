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
    public IEnumerator ApprovedSettingsCompositionReusesEveryProductionControl()
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
            for (int i = 0; i < 30; i++) yield return null;
            yield return new WaitForSecondsRealtime(0.35f);

            var menu = Object.FindObjectOfType(RuntimeType("MenuManager")) as Component;
            Assert.That(menu, Is.Not.Null);
            menu.SendMessage("OpenSettings", SendMessageOptions.RequireReceiver);
            for (int i = 0; i < 6; i++) yield return null;

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
            Assert.That(Find(root, "SettingsTitleStarLeft"), Is.Not.Null);
            Assert.That(Find(root, "SettingsTitleStarRight"), Is.Not.Null);
            Assert.That(Find(root, "SettingsReferenceShell"), Is.Not.Null);
            Assert.That(Find(root, "SettingsMascotSix"), Is.Not.Null);
            Assert.That(Find(root, "SettingsMascotSeven"), Is.Not.Null);
            Assert.That(Find(root, "SettingsPlayerChip"), Is.Not.Null);

            string[] rows =
            {
                "SettingsNameRow", "SettingsLanguageRow", "SettingsMusicRow",
                "SettingsDifficultyRow", "SettingsPrivacyRow"
            };
            foreach (string row in rows)
            {
                Transform found = Find(root, row);
                Assert.That(found, Is.Not.Null, "Missing Settings row: " + row);
                Assert.That(found.GetComponent(RuntimeType("SettingsSurfaceGraphic")),
                    Is.Not.Null, row + " must use the approved premium material.");
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
                Assert.That(image, Is.Not.Null, iconName + " must be a sprite Image.");
                Assert.That(image.sprite, Is.Not.Null,
                    iconName + " must use its premium 3D source asset.");
                Assert.That(Find(root, iconName).GetComponent(
                    RuntimeType("SettingsIconGraphic")), Is.Null,
                    iconName + " must not fall back to a procedural icon.");
            }

            Assert.That(CountNamed<Button>(settings.transform, "Buttonsave"), Is.EqualTo(1));
            Assert.That(CountNamed<Button>(settings.transform, "EnglishButton"), Is.EqualTo(1));
            Assert.That(CountNamed<Button>(settings.transform, "GreekButton"), Is.EqualTo(1));
            Assert.That(CountNamed<Toggle>(settings.transform, "Toggle"), Is.EqualTo(1));
            Assert.That(CountNamed<Button>(settings.transform, "AdsPrivacyButton"), Is.EqualTo(1));
            for (int i = 0; i < 4; i++)
                Assert.That(CountNamed<Button>(settings.transform, "Difficulty" + i),
                    Is.EqualTo(1));

            var saveButton = Find(root, "Buttonsave").GetComponent<Button>();
            AssertProductionButton(Find(root, "Buttonback").GetComponent<Button>(),
                "mainmenu_tip_frame_9s");
            AssertProductionButton(saveButton, "mainmenu_cta_blue_9s");
            AssertProductionButton(Find(root, "EnglishButton").GetComponent<Button>(),
                "mainmenu_cta_gold_9s");
            AssertProductionButton(Find(root, "GreekButton").GetComponent<Button>(),
                "mainmenu_tip_frame_9s");
            AssertProductionButton(Find(root, "AdsPrivacyButton").GetComponent<Button>(),
                "mainmenu_cta_blue_9s");
            for (int i = 0; i < 4; i++)
                AssertProductionButton(Find(root, "Difficulty" + i).GetComponent<Button>(),
                    i == 1 ? "mainmenu_cta_gold_9s" : "mainmenu_tip_frame_9s");

            Assert.That(saveButton.GetComponent(RuntimeType("SettingsButtonFeedback")),
                Is.Not.Null, "Production Settings buttons need tactile visual feedback.");
            var saveLabel = saveButton.GetComponentInChildren<TMP_Text>(true);
            var saveLabelRect = saveLabel.transform as RectTransform;
            Assert.That(saveLabelRect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(saveLabelRect.anchorMax, Is.EqualTo(Vector2.one));

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
                "The production name input and Save touch targets must not overlap.");

            AssertVerticalAlignment(root, "SettingsLanguageRowIcon",
                "SettingsLanguageRowLabel", 0.5f);
            AssertVerticalAlignment(root, "SettingsMusicRowIcon",
                "SettingsMusicRowLabel", 0.5f);
            AssertVerticalAlignment(root, "SettingsPrivacyRowIcon",
                "SettingsPrivacyRowLabel", 0.5f);
            Assert.That(((RectTransform)Find(root, "SettingsNameRowLabel"))
                .anchoredPosition.y, Is.EqualTo(38f).Within(0.5f));
            Assert.That(((RectTransform)Find(root, "SettingsDifficultyRowLabel"))
                .anchoredPosition.y, Is.EqualTo(38f).Within(0.5f));

            var inputImage = Find(root, "InputField (TMP)").GetComponent<Image>();
            Assert.That(inputImage.sprite.name, Is.EqualTo("mainmenu_tip_frame_9s"));
            Assert.That(inputImage.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(inputImage.pixelsPerUnitMultiplier, Is.EqualTo(2f));
            Assert.That(inputImage.color.a, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(Find(root, "SettingsInputSurface"), Is.Null,
                "The approved input frame must not be replaced procedurally.");

            var chipImage = Find(root, "SettingsPlayerChip").GetComponent<Image>();
            Assert.That(chipImage, Is.Not.Null);
            Assert.That(chipImage.sprite.name, Is.EqualTo("mainmenu_player_chip_frame_9s"));
            Assert.That(chipImage.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(chipImage.pixelsPerUnitMultiplier, Is.EqualTo(1f));
            Assert.That(chipImage.color.a, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(Find(root, "SettingsPlayerChip").GetComponent(
                RuntimeType("MainMenuPlayerChipGraphic")), Is.Null,
                "The approved player-chip artwork must remain the visible base.");

            AssertInsideParent(nameInputRect, 8f);
            AssertInsideParent(saveRect, 8f);
            AssertInsideParent(Find(root, "EnglishButton") as RectTransform, 8f);
            AssertInsideParent(Find(root, "GreekButton") as RectTransform, 8f);
            AssertInsideParent(Find(root, "Toggle") as RectTransform, 8f);
            AssertInsideParent(Find(root, "AdsPrivacyButton") as RectTransform, 8f);
            AssertRightBreathing(saveRect, 24f);
            AssertRightBreathing(Find(root, "EnglishButton") as RectTransform, 24f);
            AssertRightBreathing(Find(root, "GreekButton") as RectTransform, 24f);
            AssertRightBreathing(Find(root, "Toggle") as RectTransform, 24f);
            AssertRightBreathing(Find(root, "AdsPrivacyButton") as RectTransform, 24f);
            AssertHorizontalGapLocal(nameInputRect, saveRect, 16f);
            AssertHorizontalGapLocal(Find(root, "EnglishButton") as RectTransform,
                Find(root, "GreekButton") as RectTransform, 18f);
            AssertHorizontalGapLocal(Find(root, "SettingsDifficultyRowIcon")
                as RectTransform, Find(root, "Difficulty0") as RectTransform, 20f);
            for (int i = 0; i < 3; i++)
                AssertHorizontalGapLocal(Find(root, "Difficulty" + i)
                    as RectTransform, Find(root, "Difficulty" + (i + 1))
                    as RectTransform, 12f);
            for (int i = 0; i < rows.Length - 1; i++)
                AssertVerticalGapLocal(Find(root, rows[i]) as RectTransform,
                    Find(root, rows[i + 1]) as RectTransform, 18f);

            var chipName = Find(root, "PlayerName").GetComponent<TMP_Text>();
            Assert.That(chipName.text, Is.EqualTo("VisualTester"));

            var greek = Find(root, "GreekButton").GetComponent<Button>();
            greek.onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(PlayerPrefs.GetInt("Language", -1), Is.EqualTo(1),
                "The moved language control must retain its real callback.");
            Assert.That(saveLabel.text, Is.EqualTo("ΑΠΟΘΗΚΕΥΣΗ"));
            Assert.That(saveLabel.isTextOverflowing, Is.False,
                "The approved Greek Save label must fit its live production button.");
            AssertProductionButton(greek, "mainmenu_cta_gold_9s");
            AssertProductionButton(Find(root, "EnglishButton").GetComponent<Button>(),
                "mainmenu_tip_frame_9s");

            var adaptive = Find(root, "Difficulty3").GetComponent<Button>();
            adaptive.onClick.Invoke();
            yield return null;
            Assert.That(PlayerPrefs.GetInt("AIDifficulty", -1), Is.EqualTo(3),
                "The moved AI control must retain its real callback.");
            AssertProductionButton(adaptive, "mainmenu_cta_gold_9s");
            AssertProductionButton(Find(root, "Difficulty1").GetComponent<Button>(),
                "mainmenu_tip_frame_9s");

            var music = Find(root, "Toggle").GetComponent<Toggle>();
            music.isOn = false;
            yield return null;
            Assert.That(PlayerPrefs.GetInt("MusicOn", -1), Is.EqualTo(0),
                "The moved music control must retain its real callback.");

            var nameInput = Find(root, "InputField (TMP)").GetComponent<TMP_InputField>();
            nameInput.SetTextWithoutNotify("ContractTester");
            saveButton.onClick.Invoke();
            yield return null;
            Assert.That(PlayerPrefs.GetString("PlayerName", ""),
                Is.EqualTo("ContractTester"),
                "The production Save button must retain its existing callback.");

            var back = Find(root, "Buttonback").GetComponent<Button>();
            back.onClick.Invoke();
            yield return null;
            Assert.That(settings.activeSelf, Is.False,
                "Settings Back must retain MenuManager navigation.");
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
        Assert.That(image, Is.Not.Null, button.name + " needs its production Image.");
        Assert.That(image.sprite, Is.Not.Null, button.name + " needs an assigned sprite.");
        Assert.That(image.sprite.name, Is.EqualTo(expectedSprite), button.name);
        Assert.That(image.color.a, Is.EqualTo(1f).Within(0.0001f),
            button.name + " production artwork must be visible in the normal state.");
        Assert.That(image.type, Is.EqualTo(Image.Type.Sliced),
            button.name + " must preserve its approved nine-slice borders.");
        Assert.That(image.pixelsPerUnitMultiplier, Is.EqualTo(2f),
            button.name + " must render approved borders at the Settings scale.");
        Assert.That(button.targetGraphic, Is.SameAs(image),
            button.name + " must retain its original hit target.");

        Transform overlayTransform = Find(button.transform, "SettingsButtonStateOverlay");
        Assert.That(overlayTransform, Is.Not.Null,
            button.name + " needs an additive state overlay.");
        var overlay = overlayTransform.GetComponent<Image>();
        Assert.That(overlay, Is.Not.Null);
        Assert.That(overlay.sprite.name, Is.EqualTo(expectedSprite));
        Assert.That(overlay.type, Is.EqualTo(Image.Type.Sliced));
        Assert.That(overlay.pixelsPerUnitMultiplier, Is.EqualTo(2f));
        Assert.That(overlay.raycastTarget, Is.False);
        Assert.That(overlay.color.a, Is.EqualTo(0f).Within(0.0001f),
            button.name + " overlay must be invisible in the normal state.");

        foreach (var graphic in button.GetComponentsInChildren<Graphic>(true))
            Assert.That(graphic.GetType().Name, Is.Not.EqualTo("SettingsSurfaceGraphic"),
                button.name + " must not place a procedural replacement over its artwork.");
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
        var aRect = Rect.MinMaxRect(aCorners[0].x, aCorners[0].y,
            aCorners[2].x, aCorners[2].y);
        var bRect = Rect.MinMaxRect(bCorners[0].x, bCorners[0].y,
            bCorners[2].x, bCorners[2].y);
        return aRect.Overlaps(bRect);
    }

    static void AssertVerticalAlignment(Transform root, string firstName,
        string secondName, float tolerance)
    {
        var first = Find(root, firstName) as RectTransform;
        var second = Find(root, secondName) as RectTransform;
        Assert.That(first, Is.Not.Null);
        Assert.That(second, Is.Not.Null);
        Assert.That(second.anchoredPosition.y,
            Is.EqualTo(first.anchoredPosition.y).Within(tolerance),
            firstName + " and " + secondName + " must share one row baseline.");
    }

    static void AssertInsideParent(RectTransform child, float padding)
    {
        Assert.That(child, Is.Not.Null);
        var parent = child.parent as RectTransform;
        Assert.That(parent, Is.Not.Null);
        float left = child.anchoredPosition.x - child.rect.width * 0.5f;
        float right = child.anchoredPosition.x + child.rect.width * 0.5f;
        float bottom = child.anchoredPosition.y - child.rect.height * 0.5f;
        float top = child.anchoredPosition.y + child.rect.height * 0.5f;
        Assert.That(left, Is.GreaterThanOrEqualTo(parent.rect.xMin + padding),
            child.name + " needs left breathing room.");
        Assert.That(right, Is.LessThanOrEqualTo(parent.rect.xMax - padding),
            child.name + " needs right breathing room.");
        Assert.That(bottom, Is.GreaterThanOrEqualTo(parent.rect.yMin + padding),
            child.name + " needs bottom breathing room.");
        Assert.That(top, Is.LessThanOrEqualTo(parent.rect.yMax - padding),
            child.name + " needs top breathing room.");
    }

    static void AssertRightBreathing(RectTransform child, float minimum)
    {
        Assert.That(child, Is.Not.Null);
        var parent = child.parent as RectTransform;
        Assert.That(parent, Is.Not.Null);
        float right = child.anchoredPosition.x + child.rect.width * 0.5f;
        float gap = parent.rect.xMax - right;
        Assert.That(gap, Is.GreaterThanOrEqualTo(minimum),
            child.name + " needs extra right-side breathing room for its rim and glow.");
    }

    static void AssertHorizontalGapLocal(RectTransform first,
        RectTransform second, float minimum)
    {
        Assert.That(first, Is.Not.Null);
        Assert.That(second, Is.Not.Null);
        RectTransform left = first.anchoredPosition.x <= second.anchoredPosition.x
            ? first : second;
        RectTransform right = left == first ? second : first;
        float gap = right.anchoredPosition.x - right.rect.width * 0.5f -
            (left.anchoredPosition.x + left.rect.width * 0.5f);
        Assert.That(gap, Is.GreaterThanOrEqualTo(minimum),
            first.name + " and " + second.name + " need more breathing room.");
    }

    static void AssertVerticalGapLocal(RectTransform upper,
        RectTransform lower, float minimum)
    {
        Assert.That(upper, Is.Not.Null);
        Assert.That(lower, Is.Not.Null);
        if (upper.anchoredPosition.y < lower.anchoredPosition.y)
        {
            RectTransform swap = upper;
            upper = lower;
            lower = swap;
        }
        float gap = upper.anchoredPosition.y - upper.rect.height * 0.5f -
            (lower.anchoredPosition.y + lower.rect.height * 0.5f);
        Assert.That(gap, Is.GreaterThanOrEqualTo(minimum),
            upper.name + " and " + lower.name + " need vertical breathing room.");
    }

    static object Field(Component target, string name)
    {
        return target.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .GetValue(target);
    }

    static void InvokeInstaller(System.Type type)
    {
        var install = type.GetMethod("Install",
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
            var found = Find(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    static System.Type RuntimeType(string name)
    {
        var type = System.Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, "Missing runtime type: " + name);
        return type;
    }
}
