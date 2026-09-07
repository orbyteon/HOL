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
    struct SavedPreference
    {
        public bool Exists;
        public bool IsInteger;
        public int Integer;
        public string Text;
    }

    int originalScreenWidth;
    int originalScreenHeight;
    bool originalFullScreen;

    [SetUp]
    public void CaptureScreenState()
    {
        originalScreenWidth = Screen.width;
        originalScreenHeight = Screen.height;
        originalFullScreen = Screen.fullScreen;
    }

    [UnityTearDown]
    public IEnumerator RestoreScreenState()
    {
        Screen.SetResolution(
            originalScreenWidth, originalScreenHeight, originalFullScreen);
        yield return null;
    }

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
        string[] profileKeys =
        {
            "HOL.Onboarding.Version",
            "HOL.Onboarding.Gender",
            "HOL.Onboarding.Avatar",
            "HOL.Onboarding.AgeCategory",
        };
        SavedPreference[] savedProfile =
            new SavedPreference[profileKeys.Length];
        for (int index = 0; index < profileKeys.Length; index++)
            savedProfile[index] = CapturePreference(profileKeys[index]);

        try
        {
            PlayerPrefs.SetString("PlayerName", "VisualTester");
            PlayerPrefs.SetInt("Language", 0);
            PlayerPrefs.SetInt("AIDifficulty", 1);
            PlayerPrefs.SetInt("MusicOn", 1);
            PlayerPrefs.SetInt("HOL.Onboarding.Version", 1);
            PlayerPrefs.SetInt("HOL.Onboarding.Gender", 0);
            PlayerPrefs.SetInt("HOL.Onboarding.Avatar", 6);
            PlayerPrefs.SetInt("HOL.Onboarding.AgeCategory", 2);
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

            Image profileAvatar = Find(root, "PlayerAvatar").GetComponent<Image>();
            Assert.That(profileAvatar.sprite, Is.SameAs(CatalogAvatarSprite(6)),
                "Settings must display the canonical committed avatar.");
            Assert.That(profileAvatar.preserveAspect, Is.True);
            Assert.That(profileAvatar.raycastTarget, Is.False);
            RectTransform avatarRect = profileAvatar.rectTransform;
            Vector2 avatarPosition = avatarRect.anchoredPosition;
            Vector2 avatarSize = avatarRect.sizeDelta;
            PlayerPrefs.SetInt("HOL.Onboarding.Avatar", 1);
            InvokePrivate(
                root.GetComponentInParent(RuntimeType("SettingsVisuals")),
                "RefreshPresentation");
            Assert.That(profileAvatar.sprite, Is.SameAs(CatalogAvatarSprite(1)),
                "Settings must refresh from the shared avatar owner.");
            Assert.That(avatarRect.anchoredPosition, Is.EqualTo(avatarPosition));
            Assert.That(avatarRect.sizeDelta, Is.EqualTo(avatarSize));
            Assert.That(PlayerPrefs.GetInt("HOL.Onboarding.Avatar"), Is.EqualTo(1),
                "Settings must not rewrite the persisted avatar.");

            RectTransform chipRect = Find(root, "SettingsPlayerChip")
                as RectTransform;
            RectTransform safeRect = safe as RectTransform;
            Component safeAreaOwner = safe.GetComponent(
                RuntimeType("ResponsiveSafeAreaRoot"));
            Assert.That(safeAreaOwner, Is.Not.Null,
                "SettingsSafeRoot must retain the single safe-area owner.");
            MethodInfo applyViewport = safeAreaOwner.GetType().GetMethod(
                "ApplyViewport", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(applyViewport, Is.Not.Null);
            Behaviour responsiveBehaviour = safeAreaOwner as Behaviour;
            bool responsiveWasEnabled = responsiveBehaviour != null &&
                                        responsiveBehaviour.enabled;
            TMP_Text profileName = Find(root, "PlayerName")
                .GetComponent<TMP_Text>();
            TMP_Text profileStreak = Find(root, "Streak")
                .GetComponent<TMP_Text>();
            try
            {
                if (responsiveBehaviour != null)
                    responsiveBehaviour.enabled = false;
                foreach (Vector2Int viewport in new[]
                {
                    new Vector2Int(720, 1280),
                    new Vector2Int(1080, 1920),
                    new Vector2Int(1080, 2400),
                    new Vector2Int(1179, 2556),
                })
                {
                    Vector2 viewportSize = viewport;
                    Vector2 canvasSize = CanvasSize(viewportSize);
                    applyViewport.Invoke(safeAreaOwner, new object[]
                    {
                        new Rect(Vector2.zero, viewportSize),
                        new Rect(Vector2.zero, viewportSize),
                        canvasSize,
                    });
                    Canvas.ForceUpdateCanvases();
                    string lane = viewport.x + "x" + viewport.y;
                    Rect appliedSafeRect = Property<Rect>(
                        safeAreaOwner, "LastSafeRect");
                    AssertRectInside(
                        BoundsInSafeRect(safeRect, chipRect, appliedSafeRect),
                        appliedSafeRect, 1f,
                        lane + " Settings profile chip inside SafeArea");
                    AssertRectInside(avatarRect, chipRect, 1f,
                        lane + " Settings avatar inside profile frame");
                    AssertRectsDoNotOverlap(
                        avatarRect, profileName.rectTransform, chipRect, 1f,
                        lane + " Settings avatar / profile name");
                    AssertRectsDoNotOverlap(
                        avatarRect, profileStreak.rectTransform, chipRect, 1f,
                        lane + " Settings avatar / streak");
                    foreach (TMP_Text text in new[] { profileName, profileStreak })
                    {
                        text.ForceMeshUpdate();
                        Assert.That(text.isTextOverflowing, Is.False,
                            lane + " " + text.name + " overflowed.");
                        AssertRenderedTextInsideAndRightOfAvatar(
                            text, chipRect, avatarRect, 1f,
                            lane + " " + text.name);
                    }
                }
            }
            finally
            {
                if (responsiveBehaviour != null)
                    responsiveBehaviour.enabled = responsiveWasEnabled;
            }

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
            Assert.That(profileAvatar.sprite, Is.SameAs(CatalogAvatarSprite(1)),
                "Language changes must not replace the selected avatar.");

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
            for (int index = 0; index < profileKeys.Length; index++)
                RestorePreference(profileKeys[index], savedProfile[index]);
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

    static void AssertRectInside(
        RectTransform inner,
        RectTransform outer,
        float inset,
        string context)
    {
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
            outer, inner);
        Rect available = outer.rect;
        Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(available.xMin + inset),
            context + " left");
        Assert.That(bounds.max.x, Is.LessThanOrEqualTo(available.xMax - inset),
            context + " right");
        Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(available.yMin + inset),
            context + " bottom");
        Assert.That(bounds.max.y, Is.LessThanOrEqualTo(available.yMax - inset),
            context + " top");
    }

    static void AssertRectInside(
        Rect inner,
        Rect outer,
        float inset,
        string context)
    {
        Assert.That(inner.xMin, Is.GreaterThanOrEqualTo(outer.xMin + inset),
            context + " left");
        Assert.That(inner.xMax, Is.LessThanOrEqualTo(outer.xMax - inset),
            context + " right");
        Assert.That(inner.yMin, Is.GreaterThanOrEqualTo(outer.yMin + inset),
            context + " bottom");
        Assert.That(inner.yMax, Is.LessThanOrEqualTo(outer.yMax - inset),
            context + " top");
    }

    static Rect BoundsInSafeRect(
        RectTransform safeRoot,
        RectTransform target,
        Rect safeRect)
    {
        Bounds localBounds = RectTransformUtility
            .CalculateRelativeRectTransformBounds(safeRoot, target);
        float scale = Mathf.Abs(safeRoot.localScale.x);
        Vector2 size = new Vector2(
            localBounds.size.x * scale,
            localBounds.size.y * scale);
        Vector2 center = safeRect.center + new Vector2(
            localBounds.center.x * scale,
            localBounds.center.y * scale);
        return new Rect(center - size * 0.5f, size);
    }

    static Vector2 CanvasSize(Vector2 viewport)
    {
        MethodInfo method = RuntimeType("ResponsiveViewportGeometry")
            .GetMethod("CanvasSizeForViewport",
                BindingFlags.Static | BindingFlags.Public);
        Assert.That(method, Is.Not.Null);
        return (Vector2)method.Invoke(null, new object[]
        {
            viewport,
            new Vector2(1080f, 1920f),
            0.5f,
        });
    }

    static T Property<T>(Component component, string name)
    {
        PropertyInfo property = component.GetType().GetProperty(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, name);
        return (T)property.GetValue(component, null);
    }

    static void AssertRectsDoNotOverlap(
        RectTransform first,
        RectTransform second,
        RectTransform relativeTo,
        float gap,
        string context)
    {
        Bounds firstBounds = RectTransformUtility
            .CalculateRelativeRectTransformBounds(relativeTo, first);
        Bounds secondBounds = RectTransformUtility
            .CalculateRelativeRectTransformBounds(relativeTo, second);
        Rect expanded = new Rect(
            firstBounds.min.x - gap,
            firstBounds.min.y - gap,
            firstBounds.size.x + gap * 2f,
            firstBounds.size.y + gap * 2f);
        Rect other = new Rect(
            secondBounds.min.x,
            secondBounds.min.y,
            secondBounds.size.x,
            secondBounds.size.y);
        Assert.That(expanded.Overlaps(other), Is.False, context);
    }

    static void AssertRenderedTextInsideAndRightOfAvatar(
        TMP_Text text,
        RectTransform chip,
        RectTransform avatar,
        float gap,
        string context)
    {
        Assert.That(TryRenderedGlyphRectIn(chip, text, out Rect glyphs),
            Is.True, context + " must render visible glyphs.");
        Rect safe = chip.rect;
        Assert.That(glyphs.xMin, Is.GreaterThanOrEqualTo(safe.xMin + gap),
            context + " glyphs left chip");
        Assert.That(glyphs.xMax, Is.LessThanOrEqualTo(safe.xMax - gap),
            context + " glyphs right chip");
        Assert.That(glyphs.yMin, Is.GreaterThanOrEqualTo(safe.yMin + gap),
            context + " glyphs below chip");
        Assert.That(glyphs.yMax, Is.LessThanOrEqualTo(safe.yMax - gap),
            context + " glyphs above chip");
        Bounds reserved = RectTransformUtility.CalculateRelativeRectTransformBounds(
            chip, avatar);
        Assert.That(glyphs.xMin,
            Is.GreaterThanOrEqualTo(reserved.max.x + gap),
            context + " glyphs overlap the avatar");
    }

    static bool TryRenderedGlyphRectIn(
        RectTransform owner,
        TMP_Text text,
        out Rect glyphs)
    {
        glyphs = default;
        if (owner == null || text == null)
            return false;

        text.ForceMeshUpdate(true, true);
        bool found = false;
        Vector2 minimum = new Vector2(
            float.PositiveInfinity, float.PositiveInfinity);
        Vector2 maximum = new Vector2(
            float.NegativeInfinity, float.NegativeInfinity);
        TMP_TextInfo info = text.textInfo;
        for (int index = 0; index < info.characterCount; index++)
        {
            TMP_CharacterInfo character = info.characterInfo[index];
            if (!character.isVisible)
                continue;
            foreach (Vector3 corner in new[]
            {
                character.bottomLeft,
                character.topLeft,
                character.topRight,
                character.bottomRight,
            })
            {
                Vector3 local = owner.InverseTransformPoint(
                    text.rectTransform.TransformPoint(corner));
                minimum = Vector2.Min(minimum, local);
                maximum = Vector2.Max(maximum, local);
            }
            found = true;
        }
        if (found)
            glyphs = Rect.MinMaxRect(
                minimum.x, minimum.y, maximum.x, maximum.y);
        return found;
    }

    static SavedPreference CapturePreference(string key)
    {
        var saved = new SavedPreference
        {
            Exists = PlayerPrefs.HasKey(key),
        };
        if (!saved.Exists)
            return saved;

        const string sentinel = "<HOL_PLAYER_PREFS_TYPE_SENTINEL>";
        string text = PlayerPrefs.GetString(key, sentinel);
        saved.IsInteger = text == sentinel;
        if (saved.IsInteger)
            saved.Integer = PlayerPrefs.GetInt(key, 0);
        else
            saved.Text = text;
        return saved;
    }

    static void RestorePreference(string key, SavedPreference saved)
    {
        PlayerPrefs.DeleteKey(key);
        if (!saved.Exists)
            return;
        if (saved.IsInteger)
            PlayerPrefs.SetInt(key, saved.Integer);
        else
            PlayerPrefs.SetString(key, saved.Text ?? string.Empty);
    }

    static object InvokePrivate(Component component, string name)
    {
        Assert.That(component, Is.Not.Null, name + " owner");
        MethodInfo method = component.GetType().GetMethod(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, name);
        return method.Invoke(component, null);
    }

    static Sprite CatalogAvatarSprite(int index)
    {
        Type catalog = RuntimeType("OnboardingAvatarCatalog");
        object entry = catalog.GetMethod(
            "Get", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new object[] { index });
        string resource = (string)entry.GetType()
            .GetProperty("ResourcePath", BindingFlags.Public | BindingFlags.Instance)
            .GetValue(entry, null);
        Sprite sprite = Resources.Load<Sprite>(resource);
        Assert.That(sprite, Is.Not.Null, resource);
        return sprite;
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
