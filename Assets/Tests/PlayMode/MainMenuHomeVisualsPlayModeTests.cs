using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class MainMenuHomeVisualsPlayModeTests
{
    const BindingFlags StaticFlags =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    static readonly Rect PromoInnerSafeRect =
        Rect.MinMaxRect(-260f, -68f, 260f, 68f);
    const float PromoTrophyRight = -189f;

    struct SavedPreference
    {
        public bool Exists;
        public bool IsInteger;
        public int Integer;
        public string Text;
    }

    [UnityTest]
    public IEnumerator HomeMatchesApprovedPlayHubHierarchyAndRemainsPlayable()
    {
        Screen.SetResolution(1080, 1920, false);
        InvokeInstaller("MainMenuHomeVisuals");
        InvokeInstaller("MainMenuPlayVisuals");
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);

        Component owner = null;
        for (int frame = 0; frame < 160; frame++)
        {
            owner = FindInScene(RuntimeType("MainMenuHomeVisuals"));
            if (owner != null &&
                GetProperty<bool>(owner, "IsReady") &&
                GetProperty<bool>(owner, "IsSettled"))
                break;
            yield return null;
        }

        Assert.That(owner, Is.Not.Null);
        Assert.That(GetProperty<bool>(owner, "IsReady"), Is.True);
        Assert.That(GetProperty<bool>(owner, "IsSettled"), Is.True);
        Assert.That(CountInScene(RuntimeType("MainMenuHomeVisuals")), Is.EqualTo(1),
            "Home must have exactly one presentation owner.");

        var canvas = owner.GetComponent<Canvas>();
        Assert.That(canvas, Is.Not.Null);
        Transform root = Find(canvas.transform, "HomeVisualRoot");
        Assert.That(root, Is.Not.Null);

        string[] required =
        {
            "HomeBackground",
            "HomeDecorations",
            "HomeOuterFrame",
            "HomeStars",
            "HomeConfetti",
            "HomeSafeAreaRoot",
            "HomeLogo",
            "HomeHeroBoy",
            "HomeHeroGirl",
            "HomeSpeechBubble",
            "HomeSpeechText",
            "HomePlayerChip",
            "HomePlayerAvatar",
            "HomeTrophyIcon",
            "HomePlayerChipText",
            "HomePlayerChipScore",
            "HomePlayTitle",
            "HomePlaySubtitle",
            "HomeDailyIcon",
            "HomeDailyTitle",
            "HomeDailySubtitle",
            "HomeDailyGift",
            "HomeDailyPromo",
            "HomePromoTrophy",
            "HomePortal",
            "HomeMascotSix",
            "HomeMascotSeven",
        };
        foreach (string name in required)
            Assert.That(Find(root, name), Is.Not.Null,
                "Missing approved Home object: " + name);

        foreach (string retired in new[]
        {
            "ExactReferenceBackdrop",
            "AttachmentReferenceBackdrop",
            "BoardHomeLogo",
            "HomeNeonBackdrop",
            "HomeArenaGrid",
            "HomeDecoStars",
            "HomeDecoLightning",
            "HomeDecoConfetti",
            "HomeDecoNumbers",
        })
        {
            Transform found = Find(canvas.transform, retired);
            Assert.That(found == null || !found.gameObject.activeInHierarchy, Is.True,
                "Retired Home presentation is visible: " + retired);
        }

        AssertSprite(root, "HomeBackground",
            "solo/production/solo_background_v1", Image.Type.Simple);
        AssertSprite(root, "HomeDecorations",
            "solo/production/solo_decorations_v1", Image.Type.Simple);
        AssertSprite(root, "HomeLogo",
            "reference/hol_logo_exact", Image.Type.Simple);
        AssertSprite(root, "HomeHeroBoy",
            "phase2a/hol_menu_boy_arms_crossed_r3", Image.Type.Simple);
        AssertSprite(root, "HomeHeroGirl",
            "phase2a/hol_menu_girl_forward_fist_r3", Image.Type.Simple);
        AssertSprite(root, "HomeSpeechBubble",
            "cartoon/cartoon_speech_bubble_raster", Image.Type.Sliced);
        AssertSprite(root, "HomeOuterFrame",
            "mainmenu/mainmenu_outer_frame_reference_v1", Image.Type.Simple);
        AssertSprite(root, "HomePortal",
            "dailyhunt/production/daily_floor_portal", Image.Type.Simple);
        AssertSprite(root, "HomeDailyGift",
            "mainmenu/mainmenu_daily_gift_reference_v1", Image.Type.Simple);

        RectTransform outerFrame = Find(root, "HomeOuterFrame") as RectTransform;
        Assert.That(outerFrame, Is.Not.Null);
        owner.GetType().GetMethod(
            "ApplyResponsiveLayoutForViewport", InstanceFlags)
            .Invoke(owner, new object[] { 1080, 1920, true });
        Assert.That(outerFrame.rect.height, Is.EqualTo(1920f).Within(0.01f));
        Assert.That(outerFrame.gameObject.activeInHierarchy, Is.False,
            "The retired chrome-bearing Home frame must stay invisible.");

        Button play = Find(root, "ButtonPlay").GetComponent<Button>();
        Button daily = Find(canvas.transform, "DailyHuntButton").GetComponent<Button>();
        Button settings = Find(canvas.transform, "Buttonsettings").GetComponent<Button>();
        Assert.That(play, Is.Not.Null);
        Assert.That(daily, Is.Not.Null);
        Assert.That(settings, Is.Not.Null);
        Assert.That(CountNamedButtons(canvas.transform, "ButtonPrivateRoom"), Is.Zero,
            "Home must not manufacture a duplicate private-room entry.");
        Assert.That(CountNamedButtons(canvas.transform, "ButtonPvP"), Is.EqualTo(1),
            "The one real PvP button is owned by the mode selector.");
        Assert.That(Find(root, "ButtonPvP"), Is.Null,
            "Private Room must not remain an equal Home CTA.");

        AssertProductionButton(
            play, "solo/production/solo_player_card_shell_v1");
        AssertProductionButton(
            daily, "solo/production/solo_opponent_card_shell_v1");

        Assert.That(PersistentMethods(play), Does.Contain("OnPlayPressed"));
        Assert.That(PersistentMethods(settings), Does.Contain("OpenSettings"));

        AssertReferenceComposition(root, play, daily);

        foreach (string titleName in new[]
        {
            "HomePlayTitle",
            "HomeDailyTitle",
        })
        {
            TMP_Text title = Find(root, titleName).GetComponent<TMP_Text>();
            Assert.That(title.font, Is.SameAs(Resources.Load<TMP_FontAsset>(
                "phase2a/fonts/HOL Menu Display SDF")));
            Assert.That(title.enableAutoSizing, Is.True, titleName);
            float minimum = titleName == "HomePlayTitle" ? 42f : 22f;
            Assert.That(title.fontSize, Is.GreaterThanOrEqualTo(minimum), titleName);
            Assert.That(title.overflowMode, Is.EqualTo(TextOverflowModes.Truncate));
            if (titleName == "HomePlayTitle")
                Assert.That((title.fontStyle & FontStyles.UpperCase) != 0,
                    "The dominant Home CTA must visibly read PLAY / ΠΑΙΞΕ.");
        }

        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            Assert.That(IsAllowedProductionGraphic(graphic), Is.True,
                "Procedural Graphic found on Home: " +
                graphic.GetType().Name + " / " + graphic.name);
            if (graphic is Image image && image.sprite != null)
                Assert.That(image.color.a, Is.GreaterThanOrEqualTo(0.99f),
                    image.name + " hides approved production art.");
        }

        PlayerPrefs.SetInt("StatWins", 12);
        PlayerPrefs.SetString("PlayerName", "Marinos");
        PlayerPrefs.Save();
        owner.GetType().GetMethod("RefreshChip", InstanceFlags).Invoke(owner, null);
        string chipText = Find(root, "HomePlayerChipText")
            .GetComponent<TMP_Text>().text;
        string chipScore = Find(root, "HomePlayerChipScore")
            .GetComponent<TMP_Text>().text;
        Assert.That(chipText, Is.EqualTo("Marinos"));
        Assert.That(chipScore, Is.EqualTo("12"));

        AssertLocalizedHomeCopy(root, 0,
            "Play", "Choose your game mode", "DAILY HUNT",
            "A new challenge every day, big rewards!");
        AssertLocalizedHomeCopy(root, 1,
            "Παίξε", "Διάλεξε τρόπο παιχνιδιού", "ΗΜΕΡΗΣΙΑ ΔΟΚΙΜΑΣΙΑ",
            "Πρόκληση κάθε μέρα, μεγάλα έπαθλα!");
        SetLanguage(0);

        Component hunt = FindInScene(RuntimeType("DailyHunt"));
        Assert.That(hunt, Is.Not.Null);
        Assert.That(hunt.gameObject.activeSelf, Is.False);
        daily.onClick.Invoke();
        yield return null;
        Assert.That(hunt.gameObject.activeSelf, Is.True,
            "Daily Hunt lost its real production callback.");
        hunt.SendMessage("Close", SendMessageOptions.RequireReceiver);
        yield return null;

        var menuManager = FindInScene(RuntimeType("MenuManager"));
        GameObject mainMenu = GetField<GameObject>(menuManager, "mainMenuPanel");
        GameObject panelPlay = GetField<GameObject>(menuManager, "panelPlay");
        GameObject searching = GetField<GameObject>(menuManager, "panelSearching");
        Component matchmaking = FindInScene(RuntimeType("FakeMatchmaking"));
        GameObject panelGame = GetField<GameObject>(matchmaking, "panelGame");
        Component controller = FindInScene(RuntimeType("PvpGameController"));
        GameObject pvpMenu = GetField<GameObject>(controller, "pvpMenuPanel");

        play.onClick.Invoke();
        yield return null;
        Assert.That(mainMenu.activeSelf, Is.False);
        Assert.That(panelPlay.activeSelf, Is.True,
            "PLAY must open the truthful two-choice mode selector.");
        Assert.That(searching.activeSelf, Is.False);
        Assert.That(panelGame.activeSelf, Is.False,
            "Opening the selector must not begin Solo.");
        Assert.That(pvpMenu.activeSelf, Is.False,
            "Opening the selector must not begin Private Room.");
    }

    [UnityTest]
    public IEnumerator HomeProfileAvatarUsesCanonicalOnboardingSelectionWithoutMovingLayout()
    {
        Type profile = RuntimeType("OnboardingProfile");
        string avatarKey = (string)profile.GetField("AvatarKey", StaticFlags)
            .GetRawConstantValue();
        string versionKey = (string)profile.GetField("VersionKey", StaticFlags)
            .GetRawConstantValue();
        int currentVersion = (int)profile.GetField("CurrentVersion", StaticFlags)
            .GetRawConstantValue();
        SavedPreference savedAvatar = CapturePreference(avatarKey);
        SavedPreference savedVersion = CapturePreference(versionKey);

        PlayerPrefs.SetInt(versionKey, currentVersion);
        PlayerPrefs.SetInt(avatarKey, 1);
        PlayerPrefs.Save();

        try
        {
            Screen.SetResolution(1080, 1920, false);
            InvokeInstaller("MainMenuHomeVisuals");
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);

            Component owner = null;
            for (int frame = 0; frame < 160; frame++)
            {
                owner = FindInScene(RuntimeType("MainMenuHomeVisuals"));
                if (owner != null &&
                    GetProperty<bool>(owner, "IsReady") &&
                    GetProperty<bool>(owner, "IsSettled"))
                    break;
                yield return null;
            }

            Assert.That(owner, Is.Not.Null);
            Transform root = Find(owner.transform, "HomeVisualRoot");
            Assert.That(root, Is.Not.Null);
            Transform chip = Find(root, "HomePlayerChip");
            Image portrait = Find(chip, "HomePlayerAvatar").GetComponent<Image>();
            Image ring = Find(chip, "HomePlayerAvatarRing").GetComponent<Image>();
            Sprite cyan = Resources.Load<Sprite>("reference/player_cyan_exact");
            MethodInfo refresh = owner.GetType().GetMethod("RefreshChip", InstanceFlags);
            MethodInfo applyViewport = owner.GetType().GetMethod(
                "ApplyResponsiveLayoutForViewport", InstanceFlags);
            MethodInfo isValid = profile.GetMethod("IsValidAvatar", StaticFlags);
            Type catalog = RuntimeType("OnboardingAvatarCatalog");
            int avatarCount = (int)catalog.GetProperty("Count", StaticFlags)
                .GetValue(null, null);

            Assert.That(portrait.sprite, Is.SameAs(CatalogAvatarSprite(1)));
            Sprite first = portrait.sprite;
            PlayerPrefs.SetInt(avatarKey, 6);
            refresh.Invoke(owner, null);
            Assert.That(portrait.sprite, Is.SameAs(CatalogAvatarSprite(6)));
            Assert.That(portrait.sprite, Is.Not.SameAs(first),
                "Distinct saved avatars must render distinct profile portraits.");

            for (int index = 0; index < avatarCount; index++)
            {
                PlayerPrefs.SetInt(avatarKey, index);
                refresh.Invoke(owner, null);
                bool valid = (bool)isValid.Invoke(null, new object[] { index });
                Sprite expected = valid ? CatalogAvatarSprite(index) : cyan;
                Assert.That(portrait.sprite, Is.SameAs(expected),
                    "Canonical avatar " + index);
            }

            PlayerPrefs.DeleteKey(avatarKey);
            refresh.Invoke(owner, null);
            Assert.That(portrait.sprite, Is.SameAs(cyan), "missing avatar fallback");
            PlayerPrefs.SetString(avatarKey, string.Empty);
            refresh.Invoke(owner, null);
            Assert.That(portrait.sprite, Is.SameAs(cyan), "empty avatar fallback");
            PlayerPrefs.SetString(avatarKey, "avatar_02_cap_boy");
            refresh.Invoke(owner, null);
            Assert.That(portrait.sprite, Is.SameAs(cyan), "legacy avatar fallback");
            foreach (int invalid in new[] { -1, avatarCount, int.MaxValue })
            {
                PlayerPrefs.SetInt(avatarKey, invalid);
                refresh.Invoke(owner, null);
                Assert.That(portrait.sprite, Is.SameAs(cyan),
                    "invalid avatar fallback " + invalid);
            }

            Assert.That(chip.GetComponentsInChildren<Selectable>(true), Is.Empty,
                "The display-only profile chip must not gain an interaction owner.");
            foreach (Graphic graphic in chip.GetComponentsInChildren<Graphic>(true))
                Assert.That(graphic.raycastTarget, Is.False,
                    graphic.name + " must remain raycast-transparent.");
            Assert.That(portrait.type, Is.EqualTo(Image.Type.Simple));
            Assert.That(portrait.preserveAspect, Is.True);
            Assert.That(portrait.color.a, Is.EqualTo(1f).Within(0.001f));
            Assert.That(ring.type, Is.EqualTo(Image.Type.Simple));
            Assert.That(ring.preserveAspect, Is.True);
            Assert.That(ring.color.a, Is.EqualTo(1f).Within(0.001f));
            foreach (RectTransform rect in new[]
            {
                ring.rectTransform,
                portrait.rectTransform,
            })
            {
                Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(rect.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(rect.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            }

            var viewports = new[]
            {
                new Vector2Int(720, 1280),
                new Vector2Int(1080, 1920),
                new Vector2Int(1080, 2400),
                new Vector2Int(1179, 2556),
            };
            string[] trackedNames =
            {
                "Buttonsettings", "HomePlayerChip", "HomePlayerAvatarRing",
                "HomePlayerAvatar", "HomeLogo", "HomeHeroBoy", "HomeHeroGirl",
                "HomeSpeechBubble", "ButtonPlay", "DailyHuntButton", "HomeDailyPromo",
                "HomePortal", "HomeMascotSix", "HomeMascotSeven",
            };
            foreach (Vector2Int viewport in viewports)
            {
                applyViewport.Invoke(owner, new object[]
                {
                    viewport.x, viewport.y, true,
                });
                string lane = viewport.x + "x" + viewport.y;
                RectTransform[] tracked = FindRects(root, trackedNames);

                PlayerPrefs.SetInt(avatarKey, 1);
                refresh.Invoke(owner, null);
                Vector4[] baseline = CaptureLayout(tracked);
                PlayerPrefs.SetInt(avatarKey, 6);
                refresh.Invoke(owner, null);
                AssertLayout(tracked, baseline, lane + " second avatar");
                PlayerPrefs.DeleteKey(avatarKey);
                refresh.Invoke(owner, null);
                AssertLayout(tracked, baseline, lane + " fallback avatar");

                float tall = Mathf.InverseLerp(
                    1.78f, 2.22f, viewport.y / (float)viewport.x);
                AssertRectTransform(tracked[0],
                    new Vector2(-454f, 838f + 45f * tall),
                    new Vector2(118f, 118f), lane + " settings");
                AssertRectTransform(tracked[1],
                    new Vector2(330f, 838f + 45f * tall),
                    new Vector2(370f, 150f), lane + " chip");
                AssertRectTransform(tracked[2], new Vector2(-126f, 0f),
                    new Vector2(108f, 108f), lane + " avatar ring");
                AssertRectTransform(tracked[3], new Vector2(-126f, 0f),
                    new Vector2(92f, 92f), lane + " avatar portrait");
                AssertRectTransform(tracked[4],
                    new Vector2(0f, 600f + 40f * tall),
                    new Vector2(512.3f, 304.11f), lane + " logo");
                AssertRectTransform(tracked[5],
                    new Vector2(-98f, 115f),
                    new Vector2(410f, 410f), lane + " hero boy");
                AssertRectTransform(tracked[6],
                    new Vector2(58f, 60f),
                    new Vector2(410f, 410f), lane + " hero girl");
                AssertRectTransform(tracked[7],
                    new Vector2(0f, 430f),
                    new Vector2(300f, 200f), lane + " speech bubble");
                AssertRectTransform(tracked[8],
                    new Vector2(-260f, -100f + 18f * tall),
                    new Vector2(560f, 1140f), lane + " play");
                AssertRectTransform(tracked[9],
                    new Vector2(260f, -100f + 18f * tall),
                    new Vector2(560f, 1140f), lane + " daily");
                AssertRectTransform(tracked[10],
                    new Vector2(0f, -748f - 18f * tall),
                    new Vector2(600f, 182f), lane + " promo");
                AssertRectTransform(tracked[11],
                    new Vector2(0f, -890f - 62f * tall),
                    new Vector2(610f, 165f), lane + " portal");
                AssertRectTransform(tracked[12],
                    new Vector2(-398f, -780f - 42f * tall),
                    new Vector2(230f, 255f), lane + " mascot six");
                AssertRectTransform(tracked[13],
                    new Vector2(398f, -780f - 42f * tall),
                    new Vector2(220f, 255f), lane + " mascot seven");
            }
        }
        finally
        {
            RestorePreference(avatarKey, savedAvatar);
            RestorePreference(versionKey, savedVersion);
            PlayerPrefs.Save();
        }
    }

    [UnityTest]
    public IEnumerator HomeLocalizedGlyphsStayInsideApprovedArtSafeAreas()
    {
        Screen.SetResolution(1080, 1920, false);
        InvokeInstaller("MainMenuHomeVisuals");
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);

        Component owner = null;
        for (int frame = 0; frame < 160; frame++)
        {
            owner = FindInScene(RuntimeType("MainMenuHomeVisuals"));
            if (owner != null &&
                GetProperty<bool>(owner, "IsReady") &&
                GetProperty<bool>(owner, "IsSettled"))
                break;
            yield return null;
        }

        Assert.That(owner, Is.Not.Null);
        Assert.That(GetProperty<bool>(owner, "IsReady"), Is.True);
        Assert.That(GetProperty<bool>(owner, "IsSettled"), Is.True);

        Canvas canvas = owner.GetComponent<Canvas>();
        Transform root = Find(canvas.transform, "HomeVisualRoot");
        RectTransform promo = Find(root, "HomeDailyPromo") as RectTransform;
        RectTransform trophy = Find(root, "HomePromoTrophy") as RectTransform;
        RectTransform playButton = Find(root, "ButtonPlay") as RectTransform;
        RectTransform dailyButton = Find(root, "DailyHuntButton") as RectTransform;
        RectTransform mascotSix = Find(root, "HomeMascotSix") as RectTransform;
        RectTransform mascotSeven = Find(root, "HomeMascotSeven") as RectTransform;
        TMP_Text playTitle = Find(root, "HomePlayTitle").GetComponent<TMP_Text>();
        TMP_Text playSubtitle = Find(root, "HomePlaySubtitle").GetComponent<TMP_Text>();
        TMP_Text dailyTitle = Find(root, "HomeDailyTitle").GetComponent<TMP_Text>();
        TMP_Text dailySubtitle = Find(root, "HomeDailySubtitle").GetComponent<TMP_Text>();
        TMP_Text promoTitle = Find(root, "HomePromoTitle").GetComponent<TMP_Text>();
        TMP_Text promoBody = Find(root, "HomePromoBody").GetComponent<TMP_Text>();

        var viewports = new[]
        {
            new Vector2Int(720, 1280),
            new Vector2Int(1080, 1920),
            new Vector2Int(1080, 2400),
            new Vector2Int(1179, 2556),
        };
        MethodInfo applyViewport = owner.GetType().GetMethod(
            "ApplyResponsiveLayoutForViewport", InstanceFlags);
        Assert.That(applyViewport, Is.Not.Null);

        for (int language = 0; language <= 1; language++)
        {
            SetLanguage(language);
            yield return null;

            foreach (Vector2Int viewport in viewports)
            {
                string lane = (language == 0 ? "EN " : "EL ") +
                              viewport.x + "x" + viewport.y;
                applyViewport.Invoke(
                    owner, new object[] { viewport.x, viewport.y, true });
                Canvas.ForceUpdateCanvases();
                playTitle.ForceMeshUpdate(true, true);
                playSubtitle.ForceMeshUpdate(true, true);
                dailyTitle.ForceMeshUpdate(true, true);
                dailySubtitle.ForceMeshUpdate(true, true);
                promoTitle.ForceMeshUpdate(true, true);
                promoBody.ForceMeshUpdate(true, true);

                foreach (TMP_Text text in new[]
                {
                    playTitle, playSubtitle, dailyTitle, dailySubtitle,
                })
                {
                    Assert.That(text.isTextOverflowing, Is.False,
                        lane + " " + text.name);
                }
                Assert.That(promoTitle.textInfo.lineCount, Is.EqualTo(1), lane);
                Assert.That(promoBody.textInfo.lineCount, Is.EqualTo(2), lane);
                Assert.That(promoTitle.isTextOverflowing, Is.False, lane);
                Assert.That(promoBody.isTextOverflowing, Is.False, lane);
                Assert.That(playTitle.fontSize, Is.GreaterThanOrEqualTo(50f), lane);
                Assert.That(playSubtitle.fontSize, Is.GreaterThanOrEqualTo(25f), lane);
                Assert.That(dailyTitle.fontSize, Is.GreaterThanOrEqualTo(27f), lane);
                Assert.That(dailySubtitle.fontSize, Is.GreaterThanOrEqualTo(24f), lane);
                Assert.That(promoTitle.fontSize, Is.GreaterThanOrEqualTo(22f), lane);
                Assert.That(promoBody.fontSize, Is.GreaterThanOrEqualTo(20f), lane);

                AssertApprovedCenteredTextRegion(
                    owner, dailyTitle, new Vector2(7f, 448.4f),
                    new Vector2(210f, 101.3f), new Vector2(210f, 133.3f),
                    0.01f, lane + " Daily title aperture");

                AssertContained(playButton.rect,
                    GlyphBoundsAll(playTitle, playButton), 22f,
                    lane + " PLAY title");
                AssertContained(playButton.rect,
                    GlyphBoundsAll(playSubtitle, playButton), 18f,
                    lane + " PLAY subtitle");
                AssertContained(dailyButton.rect,
                    GlyphBoundsAll(dailyTitle, dailyButton), 20f,
                    lane + " Daily title");
                AssertContained(dailyButton.rect,
                    GlyphBoundsAll(dailySubtitle, dailyButton), 16f,
                    lane + " Daily subtitle");

                Rect titleGlyphs = GlyphBounds(promoTitle, promo, 0);
                Rect rewardLine0 = GlyphBounds(promoBody, promo, 0);
                Rect rewardLine1 = GlyphBounds(promoBody, promo, 1);
                AssertInside(
                    PromoInnerSafeRect, titleGlyphs, lane + " promo title");
                AssertInside(
                    PromoInnerSafeRect, rewardLine0, lane + " promo reward");
                AssertInside(
                    PromoInnerSafeRect, rewardLine1, lane + " promo footer");
                Assert.That(
                    PromoInnerSafeRect.yMax - titleGlyphs.yMax,
                    Is.GreaterThanOrEqualTo(2f), lane + " promo top padding");
                Assert.That(
                    rewardLine1.yMin - PromoInnerSafeRect.yMin,
                    Is.GreaterThanOrEqualTo(6f), lane + " promo bottom padding");
                Assert.That(
                    titleGlyphs.yMin - rewardLine0.yMax,
                    Is.GreaterThanOrEqualTo(6f), lane + " promo title gap");
                Assert.That(
                    rewardLine0.yMin - rewardLine1.yMax,
                    Is.GreaterThanOrEqualTo(6f), lane + " promo line gap");

                Assert.That(
                    rewardLine1.xMin - PromoTrophyRight,
                    Is.GreaterThanOrEqualTo(8f), lane + " trophy/text gap");
                AssertRectTransform(
                    trophy, new Vector2(-220f, -18f),
                    new Vector2(62f, 62f), lane + " promo trophy");

                float aspect = viewport.y / (float)viewport.x;
                float tall = Mathf.InverseLerp(1.78f, 2.22f, aspect);
                AssertRectTransform(
                    mascotSix, new Vector2(-398f, -780f - 42f * tall),
                    new Vector2(230f, 255f), lane + " mascot 6");
                AssertRectTransform(
                    mascotSeven, new Vector2(398f, -780f - 42f * tall),
                    new Vector2(220f, 255f), lane + " mascot 7");
            }
        }

        SetLanguage(0);
    }

    static Rect GlyphBounds(
        TMP_Text text,
        RectTransform container,
        params int[] includedLines)
    {
        bool found = false;
        Vector2 minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        TMP_TextInfo info = text.textInfo;
        for (int index = 0; index < info.characterCount; index++)
        {
            TMP_CharacterInfo character = info.characterInfo[index];
            if (!character.isVisible || !Contains(includedLines, character.lineNumber))
                continue;

            Vector3 bottomLeft = container.InverseTransformPoint(
                text.rectTransform.TransformPoint(character.bottomLeft));
            Vector3 topRight = container.InverseTransformPoint(
                text.rectTransform.TransformPoint(character.topRight));
            minimum = Vector2.Min(minimum, bottomLeft);
            maximum = Vector2.Max(maximum, topRight);
            found = true;
        }

        Assert.That(found, Is.True, text.name + " has no visible glyphs.");
        return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
    }

    static Rect GlyphBoundsAll(TMP_Text text, RectTransform container)
    {
        int count = Mathf.Max(1, text.textInfo.lineCount);
        var lines = new int[count];
        for (int index = 0; index < count; index++)
            lines[index] = index;
        return GlyphBounds(text, container, lines);
    }

    static Rect Union(Rect first, Rect second)
    {
        return Rect.MinMaxRect(
            Mathf.Min(first.xMin, second.xMin),
            Mathf.Min(first.yMin, second.yMin),
            Mathf.Max(first.xMax, second.xMax),
            Mathf.Max(first.yMax, second.yMax));
    }

    static bool Contains(int[] values, int value)
    {
        foreach (int candidate in values)
        {
            if (candidate == value)
                return true;
        }
        return false;
    }

    static void AssertContained(
        Rect safe,
        Rect actual,
        float padding,
        string label)
    {
        Assert.That(actual.xMin, Is.GreaterThanOrEqualTo(safe.xMin + padding),
            label + " left padding");
        Assert.That(actual.xMax, Is.LessThanOrEqualTo(safe.xMax - padding),
            label + " right padding");
        Assert.That(actual.yMin, Is.GreaterThanOrEqualTo(safe.yMin + padding),
            label + " bottom padding");
        Assert.That(actual.yMax, Is.LessThanOrEqualTo(safe.yMax - padding),
            label + " top padding");
    }

    static void AssertInside(Rect safe, Rect actual, string label)
    {
        Assert.That(actual.xMin, Is.GreaterThanOrEqualTo(safe.xMin),
            label + " left containment");
        Assert.That(actual.xMax, Is.LessThanOrEqualTo(safe.xMax),
            label + " right containment");
        Assert.That(actual.yMin, Is.GreaterThanOrEqualTo(safe.yMin),
            label + " bottom containment");
        Assert.That(actual.yMax, Is.LessThanOrEqualTo(safe.yMax),
            label + " top containment");
    }

    // Fixed approved apertures are the expectation, not the runtime line box.
    // TMP offsets its line box to centre visible glyphs; keep checking its size
    // and the exact authored aperture, then independently check visible ink.
    internal static void AssertApprovedCenteredTextRegion(
        Component owner, TMP_Text text, Vector2 expectedCenter,
        Vector2 expectedSize, Vector2 expectedLineBoxSize,
        float geometryTolerance, string label)
    {
        PropertyInfo regionsProperty = owner.GetType().GetProperty(
            "CenteredTextRegions", InstanceFlags);
        Assert.That(regionsProperty, Is.Not.Null, label);
        Array regions = regionsProperty.GetValue(owner, null) as Array;
        Assert.That(regions, Is.Not.Null, label);
        object matchingRegion = null;
        int matches = 0;
        foreach (object region in regions)
        {
            if (region.GetType().GetField("Text", InstanceFlags)
                .GetValue(region) != (object)text) continue;
            matchingRegion = region;
            matches++;
        }
        Assert.That(matches, Is.EqualTo(1), label + " sole aperture");
        Rect actualSafe = (Rect)matchingRegion.GetType()
            .GetField("SafeRect", InstanceFlags).GetValue(matchingRegion);
        Rect expectedSafe = new Rect(expectedCenter - expectedSize * 0.5f,
            expectedSize);
        Assert.That(actualSafe.center.x,
            Is.EqualTo(expectedCenter.x).Within(geometryTolerance), label + " x");
        Assert.That(actualSafe.center.y,
            Is.EqualTo(expectedCenter.y).Within(geometryTolerance), label + " y");
        Assert.That(actualSafe.width,
            Is.EqualTo(expectedSize.x).Within(geometryTolerance), label + " width");
        Assert.That(actualSafe.height,
            Is.EqualTo(expectedSize.y).Within(geometryTolerance), label + " height");
        Assert.That(text.rectTransform.sizeDelta.x,
            Is.EqualTo(expectedLineBoxSize.x).Within(geometryTolerance),
            label + " line box width");
        Assert.That(text.rectTransform.sizeDelta.y,
            Is.EqualTo(expectedLineBoxSize.y).Within(geometryTolerance),
            label + " line box height");
        Assert.That(text.rectTransform.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(text.rectTransform.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(text.rectTransform.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(text.alignment, Is.EqualTo(TextAlignmentOptions.Center), label);
        Assert.That(text.isTextOverflowing, Is.False, label);
        Assert.That(text.isTextTruncated, Is.False, label);
        Rect glyphs = GlyphBoundsAll(text, text.rectTransform.parent as RectTransform);
        AssertInside(expectedSafe, glyphs, label + " visible glyphs");
        // Four reference pixels is the accepted 1080x1920 glyph-centering gate.
        Assert.That(glyphs.center.x, Is.EqualTo(expectedCenter.x).Within(4f),
            label + " glyph centre x");
        Assert.That(glyphs.center.y, Is.EqualTo(expectedCenter.y).Within(4f),
            label + " glyph centre y");
    }

    static void AssertRectTransform(
        RectTransform rect,
        Vector2 expectedPosition,
        Vector2 expectedSize,
        string label)
    {
        Assert.That(rect.anchoredPosition.x,
            Is.EqualTo(expectedPosition.x).Within(0.01f), label + " x");
        Assert.That(rect.anchoredPosition.y,
            Is.EqualTo(expectedPosition.y).Within(0.01f), label + " y");
        Assert.That(rect.sizeDelta.x,
            Is.EqualTo(expectedSize.x).Within(0.01f), label + " width");
        Assert.That(rect.sizeDelta.y,
            Is.EqualTo(expectedSize.y).Within(0.01f), label + " height");
    }

    static Sprite CatalogAvatarSprite(int index)
    {
        Type catalog = RuntimeType("OnboardingAvatarCatalog");
        object entry = catalog.GetMethod("Get", StaticFlags)
            .Invoke(null, new object[] { index });
        string resource = (string)entry.GetType().GetProperty("ResourcePath")
            .GetValue(entry, null);
        Sprite sprite = Resources.Load<Sprite>(resource);
        Assert.That(sprite, Is.Not.Null, resource);
        return sprite;
    }

    static SavedPreference CapturePreference(string key)
    {
        var saved = new SavedPreference
        {
            Exists = PlayerPrefs.HasKey(key),
        };
        if (!saved.Exists) return saved;

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
        if (!saved.Exists) return;
        if (saved.IsInteger)
            PlayerPrefs.SetInt(key, saved.Integer);
        else
            PlayerPrefs.SetString(key, saved.Text ?? string.Empty);
    }

    static RectTransform[] FindRects(Transform root, string[] names)
    {
        var rects = new RectTransform[names.Length];
        for (int index = 0; index < names.Length; index++)
        {
            rects[index] = Find(root, names[index]) as RectTransform;
            Assert.That(rects[index], Is.Not.Null, names[index]);
        }
        return rects;
    }

    static Vector4[] CaptureLayout(RectTransform[] rects)
    {
        var states = new Vector4[rects.Length];
        for (int index = 0; index < rects.Length; index++)
            states[index] = new Vector4(
                rects[index].anchoredPosition.x,
                rects[index].anchoredPosition.y,
                rects[index].sizeDelta.x,
                rects[index].sizeDelta.y);
        return states;
    }

    static void AssertLayout(
        RectTransform[] rects,
        Vector4[] expected,
        string label)
    {
        Assert.That(rects.Length, Is.EqualTo(expected.Length));
        for (int index = 0; index < rects.Length; index++)
        {
            Vector4 actual = new Vector4(
                rects[index].anchoredPosition.x,
                rects[index].anchoredPosition.y,
                rects[index].sizeDelta.x,
                rects[index].sizeDelta.y);
            Assert.That(actual.x, Is.EqualTo(expected[index].x).Within(0.01f),
                label + " " + rects[index].name + " x");
            Assert.That(actual.y, Is.EqualTo(expected[index].y).Within(0.01f),
                label + " " + rects[index].name + " y");
            Assert.That(actual.z, Is.EqualTo(expected[index].z).Within(0.01f),
                label + " " + rects[index].name + " width");
            Assert.That(actual.w, Is.EqualTo(expected[index].w).Within(0.01f),
                label + " " + rects[index].name + " height");
        }
    }

    static bool IsAllowedProductionGraphic(Graphic graphic)
    {
        if (graphic is Image || graphic is TMP_Text)
            return true;

        var subMesh = graphic as TMP_SubMeshUI;
        return subMesh != null &&
               subMesh.transform.parent != null &&
               subMesh.transform.parent.GetComponent<TMP_Text>() != null;
    }

    static void AssertSprite(
        Transform root,
        string objectName,
        string resource,
        Image.Type type)
    {
        Image image = Find(root, objectName).GetComponent<Image>();
        Sprite sprite = Resources.Load<Sprite>(resource);
        Assert.That(image, Is.Not.Null, objectName);
        Assert.That(sprite, Is.Not.Null, resource);
        Assert.That(image.sprite, Is.SameAs(sprite), objectName);
        Assert.That(image.type, Is.EqualTo(type), objectName);
        Assert.That(image.color.a, Is.EqualTo(1f).Within(0.001f), objectName);
        Assert.That(image.raycastTarget, Is.False, objectName);
    }

    static void AssertProductionButton(Button button, string resource)
    {
        Image hitImage = button.GetComponent<Image>();
        Sprite sprite = Resources.Load<Sprite>(resource);
        Assert.That(hitImage, Is.Not.Null, button.name);
        Assert.That(sprite, Is.Not.Null, resource);
        Assert.That(hitImage.sprite, Is.SameAs(sprite), button.name);
        Assert.That(hitImage.type, Is.EqualTo(Image.Type.Simple), button.name);
        Assert.That(hitImage.color.a, Is.EqualTo(1f).Within(0.001f), button.name);
        Assert.That(hitImage.raycastTarget, Is.True, button.name);
        Assert.That(button.targetGraphic, Is.SameAs(hitImage), button.name);
        Assert.That(button.transition, Is.EqualTo(Selectable.Transition.ColorTint));
        Assert.That(button.interactable, Is.True, button.name);
        Component juice = button.GetComponent("ButtonJuice");
        Assert.That(juice, Is.Not.Null, button.name + " press feedback");
        object pressedScale = juice.GetType().GetField("pressedScale").GetValue(juice);
        Assert.That((float)pressedScale, Is.EqualTo(0.92f).Within(0.001f),
            button.name + " press scale");
    }

    static void AssertReferenceComposition(
        Transform root,
        Button play,
        Button daily)
    {
        RectTransform logo = Find(root, "HomeLogo") as RectTransform;
        RectTransform boy = Find(root, "HomeHeroBoy") as RectTransform;
        RectTransform girl = Find(root, "HomeHeroGirl") as RectTransform;
        RectTransform promo = Find(root, "HomeDailyPromo") as RectTransform;
        RectTransform six = Find(root, "HomeMascotSix") as RectTransform;
        RectTransform seven = Find(root, "HomeMascotSeven") as RectTransform;

        Assert.That(logo.sizeDelta.x, Is.EqualTo(512.3f).Within(0.01f));
        Assert.That(boy.sizeDelta, Is.EqualTo(new Vector2(410f, 410f)));
        Assert.That(girl.sizeDelta, Is.EqualTo(new Vector2(410f, 410f)));
        Assert.That(boy.IsChildOf(play.transform), Is.True);
        Assert.That(girl.IsChildOf(play.transform), Is.True);
        Assert.That(promo.sizeDelta.x, Is.EqualTo(600f).Within(0.01f));
        Assert.That(six.anchoredPosition.x, Is.LessThan(-300f));
        Assert.That(seven.anchoredPosition.x, Is.GreaterThan(300f));

        RectTransform playHit = play.transform as RectTransform;
        RectTransform dailyHit = daily.transform as RectTransform;
        Assert.That(playHit.sizeDelta, Is.EqualTo(new Vector2(560f, 1140f)));
        Assert.That(dailyHit.sizeDelta, Is.EqualTo(new Vector2(560f, 1140f)));

        Vector4 playPadding = play.GetComponent<Image>().raycastPadding;
        Vector4 dailyPadding = daily.GetComponent<Image>().raycastPadding;
        Assert.That(playPadding, Is.EqualTo(new Vector4(40f, 0f, 40f, 0f)));
        Assert.That(dailyPadding, Is.EqualTo(playPadding));
        float playRight = playHit.anchoredPosition.x + playHit.sizeDelta.x * 0.5f - playPadding.z;
        float dailyLeft = dailyHit.anchoredPosition.x - dailyHit.sizeDelta.x * 0.5f + dailyPadding.x;
        Assert.That(playRight, Is.LessThan(dailyLeft),
            "PLAY and Daily Hunt must not have overlapping touch ownership.");
    }

    static void AssertLocalizedHomeCopy(
        Transform root,
        int language,
        string play,
        string playSubtitle,
        string daily,
        string dailySubtitle)
    {
        SetLanguage(language);
        Assert.That(Find(root, "HomePlayTitle").GetComponent<TMP_Text>().text,
            Is.EqualTo(play));
        Assert.That(Find(root, "HomePlaySubtitle").GetComponent<TMP_Text>().text,
            Is.EqualTo(playSubtitle));
        Assert.That(Find(root, "HomeDailyTitle").GetComponent<TMP_Text>().text,
            Is.EqualTo(daily));
        Assert.That(Find(root, "HomeDailySubtitle").GetComponent<TMP_Text>().text,
            Is.EqualTo(dailySubtitle));
    }

    static int CountNamedButtons(Transform root, string name)
    {
        int count = 0;
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            if (button.name == name)
                count++;
        }
        return count;
    }

    static string[] PersistentMethods(Button button)
    {
        int count = button.onClick.GetPersistentEventCount();
        string[] names = new string[count];
        for (int i = 0; i < count; i++)
            names[i] = button.onClick.GetPersistentMethodName(i);
        return names;
    }

    static T GetField<T>(Component component, string name) where T : class
    {
        FieldInfo field = component.GetType().GetField(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Missing field " + name);
        return field.GetValue(component) as T;
    }

    static T GetProperty<T>(Component component, string name)
    {
        PropertyInfo property = component.GetType().GetProperty(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, "Missing property " + name);
        return (T)property.GetValue(component);
    }

    static void SetLanguage(int value)
    {
        Type l10n = RuntimeType("L10n");
        Type language = l10n.GetNestedType("Language");
        object enumValue = Enum.ToObject(language, value);
        l10n.GetMethod("SetLanguage", BindingFlags.Static | BindingFlags.Public)
            .Invoke(null, new[] { enumValue });
    }

    static void InvokeInstaller(string typeName)
    {
        Type type = RuntimeType(typeName);
        MethodInfo method = type.GetMethod(
            "Install", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(null, null);
    }

    static Component FindInScene(Type type)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Component found = root.GetComponentInChildren(type, true) as Component;
            if (found != null) return found;
        }
        return null;
    }

    static int CountInScene(Type type)
    {
        int count = 0;
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            count += root.GetComponentsInChildren(type, true).Length;
        return count;
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
