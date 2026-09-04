using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class DailyHuntCartoonVisualsPlayModeTests
{
    struct SavedPreference
    {
        public bool Exists;
        public bool IsInteger;
        public int Integer;
        public string Text;
    }

    static readonly string[] DailyKeys =
    {
        "DailyHuntDay",
        "DailyHuntUsed",
        "DailyHuntTrail",
        "DailyHuntDone",
        "DailyHuntFound",
        "DailyHuntRevived",
        "DailyHuntMin",
        "DailyHuntMax",
        "DailyHuntStreak",
        "DailyHuntLastFound",
        "DailyHuntPendingRevive",
    };

    static readonly Vector2Int[] PortraitViewports =
    {
        new Vector2Int(720, 1280),
        new Vector2Int(1080, 1920),
        new Vector2Int(1080, 2400),
        new Vector2Int(1179, 2556),
    };

    int originalScreenWidth;
    int originalScreenHeight;
    bool originalFullScreen;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        originalScreenWidth = Screen.width;
        originalScreenHeight = Screen.height;
        originalFullScreen = Screen.fullScreen;
        SetLanguage("English");
        foreach (string key in DailyKeys)
            PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Screen.SetResolution(
            originalScreenWidth, originalScreenHeight, originalFullScreen);
        SetLanguage("English");
        foreach (string key in DailyKeys)
            PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        yield return null;
    }

    [UnityTest]
    public IEnumerator DailyHuntUsesApprovedCartoonCompositionAndRealGuessFlow()
    {
        Screen.SetResolution(1080, 1920, false);
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);

        Component hunt = null;
        Component visuals = null;
        for (int frame = 0; frame < 180; frame++)
        {
            hunt = FindInScene(RuntimeType("DailyHunt"));
            visuals = FindInScene(RuntimeType("DailyHuntVisuals"));
            if (hunt != null && visuals != null)
                break;
            yield return null;
        }

        Assert.That(hunt, Is.Not.Null);
        Assert.That(visuals, Is.Not.Null);
        Assert.That(GetProperty<bool>(visuals, "IsReady"), Is.True);
        Assert.That(CountInScene(RuntimeType("DailyHuntVisuals")), Is.EqualTo(1));
        Assert.That(
            Type.GetType("DailyHuntVisualFidelityPass, Assembly-CSharp"),
            Is.Null,
            "DailyHuntVisuals must remain the sole Daily Hunt visual/layout owner.");
        Assert.That(
            Type.GetType("DailyHuntVisualFidelityInstaller, Assembly-CSharp"),
            Is.Null,
            "Daily Hunt must not install a second runtime visual/layout writer.");

        Invoke(hunt, "Open");
        yield return new WaitForSecondsRealtime(0.36f);
        yield return null;
        Canvas.ForceUpdateCanvases();
        Assert.That(hunt.gameObject.activeInHierarchy, Is.True);

        Transform root = Find(hunt.transform, "DailyHuntVisualRoot");
        Assert.That(root, Is.Not.Null);
        Assert.That(Find(hunt.transform, "Card"), Is.Null,
            "Daily Hunt must not build a legacy presentation before its owner.");
        Assert.That(hunt.transform.childCount, Is.EqualTo(1),
            "DailyHuntVisuals must construct the only runtime hierarchy.");

        foreach (string name in new[]
        {
            "DailyBackground",
            "DailyStars",
            "DailyConfetti",
            "DailyOuterBezelBody",
            "DailyHuntSafeRoot",
            "CloseButton",
            "DailyPlayerChip",
            "DailyPlayerChipShell",
            "DailyPlayerAvatarRing",
            "DailyPlayerAvatarClip",
            "DailyPlayerAvatar",
            "DailyPlayerStar",
            "DailyPlayerName",
            "DailyPlayerWins",
            "DailyPlayerProgress",
            "DailyPlayerXpTrack",
            "DailyPlayerProgressFillTrack",
            "DailyLogo",
            "DailyTitleRibbon",
            "DailyRibbonTitle",
            "DailyMissionDashboard",
            "DailyMissionBoard",
            "DailyMissionCalendar",
            "DailyMissionHeading",
            "DailyMissionRow1",
            "DailyMissionRow2",
            "DailyMissionRow3",
            "DailyMissionCompletion",
            "DailyMissionRewardBoard",
            "DailyMissionRewardArtwork",
            "DailyMissionRewardChest",
            "DailyMissionRewardHeading",
            "DailyMissionReset",
            "DailyMissionRewardAmount",
            "DailyMissionStartButton",
            "DailyMissionPortal",
            "DailyMascotSix",
            "DailyMascotSeven",
        })
        {
            Assert.That(Find(root, name), Is.Not.Null,
                "Missing approved Daily Hunt object: " + name);
        }

        AssertRect(root, "CloseButton",
            new Vector2(-435f, 836f), new Vector2(155f, 155f));
        AssertRect(root, "DailyPlayerChip",
            new Vector2(335f, 827f), new Vector2(365f, 194f));
        AssertRect(root, "DailyLogo",
            new Vector2(-10f, 783f), new Vector2(396f, 295f));
        AssertRect(root, "DailyTitleRibbon",
            new Vector2(0f, 585f), new Vector2(1040f, 285f));
        AssertRect(root, "DailyMissionBoard",
            new Vector2(-1f, 119f), new Vector2(1036f, 874f));
        AssertRect(root, "DailyMissionRewardBoard",
            new Vector2(0f, -417f), new Vector2(1060f, 425f));
        AssertRect(root, "DailyMissionStartButton",
            new Vector2(0f, -771f), new Vector2(595f, 230f));
        AssertRect(root, "DailyMascotSix",
            new Vector2(-372f, -754f), new Vector2(322f, 375f));
        AssertRect(root, "DailyMascotSeven",
            new Vector2(363f, -748f), new Vector2(326f, 380f));

        // Human-approved 1080x1920 production geometry. These assertions
        // deliberately lock the internal component composition as well as the
        // outer containers so a later pass cannot regress the player-chip XP
        // readability or compress the mission/reward hierarchy while keeping
        // only the parent rectangles unchanged.
        AssertRect(root, "DailyPlayerChipShell",
            new Vector2(-3f, -9f), new Vector2(336f, 184f));
        AssertRect(root, "DailyPlayerAvatarRing",
            new Vector2(-120f, 5f), new Vector2(122f, 122f));
        AssertRect(root, "DailyPlayerAvatarClip",
            new Vector2(-120f, 5f), new Vector2(105f, 105f));
        AssertNormalizedAvatarFraming(
            Find(root, "DailyPlayerAvatar").GetComponent<Image>(),
            Find(root, "DailyPlayerAvatarClip") as RectTransform,
            "Daily initial profile portrait");
        AssertRect(root, "DailyPlayerName",
            new Vector2(45f, 53f), new Vector2(220f, 40f));
        AssertRect(root, "DailyPlayerStar",
            new Vector2(-9f, -4f), new Vector2(30f, 30f));
        AssertRect(root, "DailyPlayerWins",
            new Vector2(58f, 3f), new Vector2(120f, 38f));
        AssertRect(root, "DailyPlayerXpTrack",
            new Vector2(48f, -20f), new Vector2(150f, 24f));
        AssertRect(root, "DailyPlayerProgressFillTrack",
            new Vector2(-10f, -66f), new Vector2(270f, 34f));
        AssertRect(root, "DailyPlayerProgress",
            new Vector2(45f, -71f), new Vector2(176f, 36f));

        AssertRect(root, "DailyMissionCalendar",
            new Vector2(-290f, 7f), new Vector2(465f, 565f));
        AssertRect(root, "DailyMissionHeading",
            new Vector2(165f, 291f), new Vector2(470f, 106f));
        AssertRect(root, "DailyMissionRow1",
            new Vector2(190f, 160f), new Vector2(610f, 205f));
        AssertRect(root, "DailyMissionRow2",
            new Vector2(190f, 2f), new Vector2(610f, 205f));
        AssertRect(root, "DailyMissionRow3",
            new Vector2(190f, -161f), new Vector2(610f, 205f));
        AssertRect(root, "DailyMissionCompletion",
            new Vector2(50f, -294f), new Vector2(800f, 62f));
        AssertRect(root, "DailyMissionRewardChest",
            new Vector2(-271f, -16f), new Vector2(405f, 287f));
        AssertRect(root, "DailyMissionRewardHeading",
            new Vector2(176f, 123f), new Vector2(520f, 70f));
        AssertRect(root, "DailyMissionClock",
            new Vector2(0f, 8f), new Vector2(88f, 88f));
        AssertRect(root, "DailyMissionResetLabel",
            new Vector2(150f, 52f), new Vector2(330f, 44f));
        AssertRect(root, "DailyMissionReset",
            new Vector2(140f, 0f), new Vector2(330f, 56f));
        AssertRect(root, "DailyMissionRewardTrophy",
            new Vector2(40f, -110f), new Vector2(125f, 125f));
        AssertRect(root, "DailyMissionRewardAmount",
            new Vector2(178f, -110f), new Vector2(350f, 104f));
        AssertRect(root, "DailyMissionPortal",
            new Vector2(0f, -860f), new Vector2(1110f, 205f));

        AssertSprite(root, "DailyMissionCalendar",
            "dailyhunt/production/daily_calendar_target_production");
        AssertSprite(root, "DailyMissionRewardChest",
            "dailyhunt/production/daily_reward_chest_reference_v1");
        AssertSprite(root, "DailyPlayerChipShell",
            "dailyhunt/production/daily_player_chip_shell_v3");
        AssertSprite(root, "DailyPlayerAvatarRing",
            "dailyhunt/production/daily_player_avatar_ring_v1");
        AssertSprite(root, "DailyPlayerXpTrack",
            "dailyhunt/production/daily_player_xp_track_v2");
        AssertSprite(root, "DailyLogo", "reference/hol_logo_exact");
        Assert.That(Resources.Load<Sprite>("cartoon/cartoon_daily_calendar"), Is.Null,
            "The retired code-drawn calendar approximation must not return.");
        Assert.That(Resources.Load<Sprite>("cartoon/cartoon_reward_chest"), Is.Null,
            "The retired code-drawn reward approximation must not return.");

        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            Assert.That(IsAllowedProductionGraphic(graphic), Is.True,
                "Procedural Graphic found in Daily Hunt: " +
                graphic.GetType().Name + " / " + graphic.name);
            if (graphic is Image image && image.sprite != null)
                Assert.That(image.color.a, Is.GreaterThan(0f),
                    image.name + " hides approved production art completely.");
        }

        TMP_FontAsset displayFont = Resources.Load<TMP_FontAsset>(
            "dailyhunt/production/fonts/HOL Daily Display SDF");
        TMP_FontAsset bodyFont = Resources.Load<TMP_FontAsset>(
            "dailyhunt/production/fonts/HOL Daily Body SDF");
        Assert.That(displayFont, Is.Not.Null);
        Assert.That(bodyFont, Is.Not.Null);
        foreach (string name in new[]
        {
            "DailyPlayerName",
            "DailyPlayerWins",
            "DailyPlayerProgress",
            "DailyRibbonTitle",
            "DailyMissionHeading",
            "DailyMissionRewardHeading",
            "DailyMissionReset",
            "DailyMissionRewardAmount",
        })
        {
            TMP_Text text = Find(root, name).GetComponent<TMP_Text>();
            Assert.That(text.font, Is.SameAs(displayFont),
                name + " must use the approved HOL display font.");
        }
        Assert.That(
            Find(root, "DailyMissionProgress1").GetComponent<TMP_Text>().font,
            Is.SameAs(bodyFont));

        Assert.That(
            Find(root, "DailyRibbonTitle").GetComponent<TMP_Text>().text,
            Is.EqualTo(Localized("daily_challenge_title")));
        Assert.That(
            Find(root, "DailyMissionHeading").GetComponent<TMP_Text>().text,
            Is.EqualTo(Localized("daily_missions_heading")));
        Assert.That(
            Find(root, "DailyMissionRewardHeading").GetComponent<TMP_Text>().text,
            Is.EqualTo(Localized("daily_reward_heading")));

        foreach (Vector2Int viewport in PortraitViewports)
        {
            foreach (string language in new[] { "English", "Greek" })
            {
                SetLanguage(language);
                for (int frame = 0; frame < 2; frame++)
                    yield return null;
                ApplyResponsiveViewport(visuals, viewport);
                Canvas.ForceUpdateCanvases();
                AssertResponsiveViewport(root, viewport, language);
            }
        }

        SetLanguage("English");
        Screen.SetResolution(1080, 1920, false);
        for (int frame = 0; frame < 3; frame++)
            yield return null;
        ApplyResponsiveViewport(visuals, new Vector2Int(1080, 1920));
        Canvas.ForceUpdateCanvases();

        Find(root, "DailyMissionStartButton").GetComponent<Button>()
            .onClick.Invoke();
        yield return null;

        TMP_InputField input = Find(root, "GuessInput")
            .GetComponent<TMP_InputField>();
        Button submit = Find(root, "SubmitGuessButton").GetComponent<Button>();
        Button close = Find(root, "CloseButton").GetComponent<Button>();
        Assert.That(input, Is.Not.Null);
        Assert.That(submit, Is.Not.Null);
        Assert.That(close, Is.Not.Null);
        Assert.That(input.gameObject.activeInHierarchy, Is.True);
        Assert.That(submit.gameObject.activeInHierarchy, Is.True);

        int usedBefore = GetField<int>(hunt, "used");
        input.text = "50";
        submit.onClick.Invoke();
        yield return null;
        yield return null;
        int usedAfter = GetField<int>(hunt, "used");
        Assert.That(usedAfter, Is.EqualTo(usedBefore + 1),
            "The restyled Submit control lost the real Daily Hunt callback.");
        Assert.That(Find(root, "Status").GetComponent<TMP_Text>().text,
            Is.Not.Empty);

        string visibleTrail = Find(root, "Trail").GetComponent<TMP_Text>().text;
        Assert.That(visibleTrail, Does.Not.Contain("🎯"));
        Assert.That(visibleTrail, Does.Not.Contain("🔺"));
        Assert.That(visibleTrail, Does.Not.Contain("🔻"));
        Assert.That(visibleTrail, Does.Match("[↑↓•]"),
            "The visible trail must use glyphs covered by the production font chain.");

        SetField(hunt, "used", 4);
        SetField(hunt, "done", true);
        SetField(hunt, "found", true);
        Invoke(hunt, "Refresh");
        yield return null;
        Canvas.ForceUpdateCanvases();

        TMP_Text inputCaption = Find(root, "DailyInputCaption")
            .GetComponent<TMP_Text>();
        RectTransform statusFrame = Find(root, "DailyStatusFrame")
            as RectTransform;
        Button share = Find(root, "ShareButton").GetComponent<Button>();
        Assert.That(input.gameObject.activeInHierarchy, Is.False,
            "Completed Daily Hunt must hide the real numeric input.");
        Assert.That(inputCaption.gameObject.activeInHierarchy, Is.False,
            "Completed Daily Hunt must not leave a stale input caption.");
        Assert.That(share.gameObject.activeInHierarchy, Is.True,
            "Completed Daily Hunt must expose the real Share callback.");
        Assert.That(statusFrame.sizeDelta.y, Is.GreaterThan(200f),
            "The existing status panel must occupy the released input zone.");

        close.onClick.Invoke();
        yield return null;
        Assert.That(hunt.gameObject.activeSelf, Is.False,
            "The top-left Back control lost the real Close callback.");
    }

    [UnityTest]
    public IEnumerator DailyHeaderUsesCanonicalAvatarAndContainsEverySelection()
    {
        Type profile = RuntimeType("OnboardingProfile");
        string playerNameKey = Constant<string>(profile, "PlayerNameKey");
        string versionKey = Constant<string>(profile, "VersionKey");
        string genderKey = Constant<string>(profile, "GenderKey");
        string avatarKey = Constant<string>(profile, "AvatarKey");
        string ageKey = Constant<string>(profile, "AgeKey");
        string[] keys =
        {
            playerNameKey, versionKey, genderKey, avatarKey, ageKey,
        };
        SavedPreference[] saved = new SavedPreference[keys.Length];
        for (int index = 0; index < keys.Length; index++)
            saved[index] = CapturePreference(keys[index]);

        try
        {
            Assert.That(TryCommitProfile(6), Is.True);
            PlayerPrefs.Save();
            Screen.SetResolution(1080, 1920, false);
            yield return SceneManager.LoadSceneAsync(
                "MainMenu", LoadSceneMode.Single);

            Component hunt = null;
            Component visuals = null;
            for (int frame = 0; frame < 180; frame++)
            {
                hunt = FindInScene(RuntimeType("DailyHunt"));
                visuals = FindInScene(RuntimeType("DailyHuntVisuals"));
                if (hunt != null && visuals != null &&
                    GetProperty<bool>(visuals, "IsReady"))
                    break;
                yield return null;
            }
            Assert.That(hunt, Is.Not.Null);
            Assert.That(visuals, Is.Not.Null);
            Invoke(hunt, "Open");
            yield return null;
            Canvas.ForceUpdateCanvases();

            Transform root = Find(hunt.transform, "DailyHuntVisualRoot");
            Assert.That(root, Is.Not.Null);
            Image portrait = Find(root, "DailyPlayerAvatar")
                .GetComponent<Image>();
            RectTransform aperture = Find(root, "DailyPlayerAvatarClip")
                as RectTransform;
            RectTransform ring = Find(root, "DailyPlayerAvatarRing")
                as RectTransform;
            RectTransform chip = Find(root, "DailyPlayerChip")
                as RectTransform;
            Assert.That(aperture, Is.Not.Null, "Daily avatar aperture");
            Assert.That(ring, Is.Not.Null, "Daily avatar ring");
            Assert.That(chip, Is.Not.Null, "Daily player chip");
            Mask mask = aperture.GetComponent<Mask>();
            Assert.That(mask, Is.Not.Null,
                "Daily Hunt must clip the portrait to its circular aperture.");
            Assert.That(aperture.GetComponent<Image>().sprite, Is.Not.Null,
                "Daily Hunt must use the built-in circular mask sprite.");
            Assert.That(mask.showMaskGraphic, Is.False,
                "The aperture must not replace the approved ring artwork.");
            Assert.That(aperture.GetComponent<Image>().sprite,
                Is.SameAs(Resources.Load<Sprite>(
                    Constant<string>(
                        RuntimeType("PlayerProfileAvatarResolver"),
                        "CircularApertureResourcePath"))),
                "Daily Hunt must use the shared circular aperture sprite.");
            Assert.That(aperture.GetComponent<RectMask2D>(), Is.Null,
                "The old misaligned rectangular crop must not remain active.");
            Assert.That(portrait.rectTransform.parent, Is.SameAs(aperture));
            Assert.That(portrait.preserveAspect, Is.True);
            Assert.That(portrait.raycastTarget, Is.False);

            MethodInfo refresh = visuals.GetType().GetMethod(
                "RefreshPlayerChip",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(refresh, Is.Not.Null);
            MethodInfo valid = profile.GetMethod(
                "IsValidAvatar", BindingFlags.Public | BindingFlags.Static);
            int avatarCount = (int)RuntimeType("OnboardingAvatarCatalog")
                .GetProperty("Count", BindingFlags.Public | BindingFlags.Static)
                .GetValue(null, null);
            Sprite fallback = Resources.Load<Sprite>(
                "reference/player_cyan_exact");
            Assert.That(fallback, Is.Not.Null);

            for (int index = 0; index < avatarCount; index++)
            {
                PlayerPrefs.SetInt(avatarKey, index);
                refresh.Invoke(visuals, null);
                bool selectable = (bool)valid.Invoke(
                    null, new object[] { index });
                Sprite expected = selectable
                    ? CatalogAvatarSprite(index)
                    : fallback;
                Assert.That(portrait.sprite, Is.SameAs(expected),
                    "Daily Hunt canonical avatar " + index);
                Assert.That(PlayerPrefs.GetInt(avatarKey), Is.EqualTo(index),
                    "Refreshing Daily Hunt must never overwrite the saved avatar.");
                if (selectable)
                {
                    foreach (Vector2Int viewport in PortraitViewports)
                    {
                        ApplyResponsiveViewport(visuals, viewport);
                        refresh.Invoke(visuals, null);
                        Canvas.ForceUpdateCanvases();
                        string lane = "Daily avatar " + index + " " +
                            viewport.x + "x" + viewport.y;
                        Assert.That(portrait.sprite, Is.SameAs(expected),
                            lane + " changed identity.");
                        AssertNormalizedAvatarFraming(
                            portrait, aperture, lane);
                        Assert.That(PlayerPrefs.GetInt(avatarKey),
                            Is.EqualTo(index),
                            lane + " overwrote the saved avatar.");
                    }
                }
            }

            PlayerPrefs.DeleteKey(avatarKey);
            refresh.Invoke(visuals, null);
            Assert.That(portrait.sprite, Is.SameAs(fallback),
                "Missing avatar must use the approved cyan fallback.");
            Assert.That(PlayerPrefs.HasKey(avatarKey), Is.False,
                "Fallback framing must not create an avatar preference.");
            foreach (Vector2Int viewport in PortraitViewports)
            {
                ApplyResponsiveViewport(visuals, viewport);
                refresh.Invoke(visuals, null);
                Canvas.ForceUpdateCanvases();
                AssertNormalizedAvatarFraming(
                    portrait, aperture,
                    "Daily missing-value fallback " +
                    viewport.x + "x" + viewport.y);
            }
            foreach (int invalid in new[] { -1, avatarCount, int.MaxValue })
            {
                PlayerPrefs.SetInt(avatarKey, invalid);
                refresh.Invoke(visuals, null);
                Assert.That(portrait.sprite, Is.SameAs(fallback),
                    "Invalid avatar fallback " + invalid);
                Assert.That(PlayerPrefs.GetInt(avatarKey), Is.EqualTo(invalid),
                    "Fallback resolution must not rewrite legacy data.");
            }

            foreach (string language in new[] { "English", "Greek" })
            {
                SetLanguage(language);
                string longestName = language == "Greek"
                    ? "ΚΩΝΣΤΑΝΤΙΝΟΣ"
                    : "CONSTANTINOS";
                Assert.That(TryCommitProfile(6, longestName), Is.True);
                refresh.Invoke(visuals, null);
                foreach (Vector2Int viewport in PortraitViewports)
                {
                    ApplyResponsiveViewport(visuals, viewport);
                    refresh.Invoke(visuals, null);
                    Canvas.ForceUpdateCanvases();
                    string lane = language + " " +
                        viewport.x + "x" + viewport.y;
                    Assert.That(portrait.sprite,
                        Is.SameAs(CatalogAvatarSprite(6)),
                        lane + " changed the selected avatar.");
                    Assert.That(PlayerPrefs.GetInt(avatarKey), Is.EqualTo(6),
                        lane + " overwrote the selected avatar.");
                    AssertNormalizedAvatarFraming(
                        portrait, aperture, lane + " Daily avatar");
                    AssertRectInside(
                        aperture, ring, 8f,
                        lane + " Daily aperture inside ring");
                    AssertRectInside(
                        ring, chip, 1f,
                        lane + " Daily ring inside profile chip");
                    foreach (string textName in new[]
                    {
                        "DailyPlayerName",
                        "DailyPlayerWins",
                        "DailyPlayerProgress",
                    })
                    {
                        TMP_Text text = Find(root, textName)
                            .GetComponent<TMP_Text>();
                        text.ForceMeshUpdate();
                        Assert.That(text.isTextOverflowing, Is.False,
                            lane + " " + textName + " overflowed.");
                        Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(20f),
                            lane + " " + textName + " became unreadable.");
                        AssertRenderedTextInsideAndRightOfAperture(
                            text, chip, aperture, 1f,
                            lane + " " + textName);
                        AssertRectsDoNotOverlap(
                            aperture, text.rectTransform, chip, 1f,
                            lane + " avatar / " + textName);
                    }
                }
            }
        }
        finally
        {
            for (int index = 0; index < keys.Length; index++)
                RestorePreference(keys[index], saved[index]);
            PlayerPrefs.Save();
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

    static void AssertResponsiveViewport(
        Transform root,
        Vector2Int viewport,
        string language)
    {
        AssertApprovedResponsiveGeometry(root, viewport);

        foreach (string name in new[]
        {
            "DailyPlayerName",
            "DailyPlayerWins",
            "DailyPlayerProgress",
            "DailyRibbonTitle",
            "DailyMissionHeading",
            "DailyMissionLabel1",
            "DailyMissionLabel2",
            "DailyMissionLabel3",
            "DailyMissionCompletion",
            "DailyMissionRewardHeading",
            "DailyMissionReset",
            "DailyMissionRewardAmount",
        })
        {
            TMP_Text text = Find(root, name).GetComponent<TMP_Text>();
            text.ForceMeshUpdate();
            Assert.That(text.isTextOverflowing, Is.False,
                language + " / " + viewport + " / " + name + " overflowed.");
            Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(20f),
                language + " / " + viewport + " / " + name + " became unreadable.");
        }
    }

    static void ApplyResponsiveViewport(
        Component visuals,
        Vector2Int viewport)
    {
        MethodInfo method = visuals.GetType().GetMethod(
            "ApplyResponsiveLayoutForViewport",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(
            visuals,
            new object[] { viewport.x, viewport.y, true });
    }

    static void AssertApprovedResponsiveGeometry(
        Transform root,
        Vector2Int viewport)
    {
        float aspect = viewport.x > 0
            ? Mathf.Max(1, viewport.y) / (float)viewport.x
            : 1920f / 1080f;
        float tall = Mathf.InverseLerp(1.78f, 2.22f, aspect);

        AssertRect(root, "CloseButton",
            new Vector2(-435f, 836f + 165f * tall),
            new Vector2(155f, 155f));
        AssertRect(root, "DailyPlayerChip",
            new Vector2(335f, 827f + 165f * tall),
            new Vector2(365f, 194f));
        AssertRect(root, "DailyLogo",
            new Vector2(-10f, 783f + 110f * tall),
            new Vector2(396f, 295f));
        AssertRect(root, "DailyTitleRibbon",
            new Vector2(0f, 585f + 90f * tall),
            new Vector2(1040f, 285f));
        AssertRect(root, "DailyMissionBoard",
            new Vector2(-1f, 119f + 30f * tall),
            new Vector2(1036f, 874f));
        AssertRect(root, "DailyMissionRewardBoard",
            new Vector2(0f, -417f - 65f * tall),
            new Vector2(1060f, 425f));
        AssertRect(root, "DailyMissionPortal",
            new Vector2(0f, -860f - 240f * tall),
            new Vector2(1110f, 205f));
        AssertRect(root, "DailyMissionStartButton",
            new Vector2(0f, -771f - 185f * tall),
            new Vector2(595f, 230f));
        AssertRect(root, "DailyMascotSix",
            new Vector2(-372f, -754f - 165f * tall),
            new Vector2(322f, 375f));
        AssertRect(root, "DailyMascotSeven",
            new Vector2(363f, -748f - 165f * tall),
            new Vector2(326f, 380f));
    }

    static void AssertSprite(Transform root, string name, string resource)
    {
        Image image = Find(root, name).GetComponent<Image>();
        Sprite sprite = Resources.Load<Sprite>(resource);
        Assert.That(sprite, Is.Not.Null, resource);
        Assert.That(image.sprite, Is.SameAs(sprite), name);
        Assert.That(image.type, Is.EqualTo(Image.Type.Simple), name);
        Assert.That(image.color.a, Is.EqualTo(1f).Within(0.001f), name);
        Assert.That(image.raycastTarget, Is.False, name);
    }

    static void AssertRect(
        Transform root,
        string name,
        Vector2 position,
        Vector2 size)
    {
        RectTransform rect = Find(root, name) as RectTransform;
        Assert.That(rect, Is.Not.Null, name);
        Assert.That(Vector2.Distance(rect.anchoredPosition, position),
            Is.LessThan(1f), name + " position drifted.");
        Assert.That(Vector2.Distance(rect.sizeDelta, size),
            Is.LessThan(1f), name + " size drifted.");
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

    static void AssertNormalizedAvatarFraming(
        Image portrait,
        RectTransform aperture,
        string context)
    {
        PlayerProfileAvatarFramingTestAssertions.AssertLayout(
            portrait, aperture, context);
    }

    static void AssertRenderedTextInsideAndRightOfAperture(
        TMP_Text text,
        RectTransform chip,
        RectTransform aperture,
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
            chip, aperture);
        Assert.That(glyphs.xMin,
            Is.GreaterThanOrEqualTo(reserved.max.x + gap),
            context + " glyphs overlap the avatar aperture");
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

    static T Constant<T>(Type type, string name)
    {
        FieldInfo field = type.GetField(
            name, BindingFlags.Public | BindingFlags.Static);
        Assert.That(field, Is.Not.Null, name);
        return (T)field.GetRawConstantValue();
    }

    static bool TryCommitProfile(
        int avatarIndex,
        string playerName = "AvatarTester")
    {
        Type profile = RuntimeType("OnboardingProfile");
        Type gender = profile.GetNestedType(
            "GenderChoice", BindingFlags.Public);
        Type age = profile.GetNestedType(
            "AgeCategory", BindingFlags.Public);
        MethodInfo commit = profile.GetMethod(
            "TryCommit", BindingFlags.Public | BindingFlags.Static);
        return (bool)commit.Invoke(null, new object[]
        {
            playerName,
            Enum.ToObject(gender, 0),
            avatarIndex,
            Enum.ToObject(age, 2),
        });
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

    static string Localized(string key)
    {
        Type type = RuntimeType("L10n");
        MethodInfo method = type.GetMethod(
            "Get", BindingFlags.Public | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);
        return (string)method.Invoke(null, new object[] { key, new object[0] });
    }

    static void SetLanguage(string name)
    {
        Type type = RuntimeType("L10n");
        Type language = type.GetNestedType("Language", BindingFlags.Public);
        MethodInfo method = type.GetMethod(
            "SetLanguage", BindingFlags.Public | BindingFlags.Static);
        Assert.That(language, Is.Not.Null);
        Assert.That(method, Is.Not.Null);
        method.Invoke(null, new[] { Enum.Parse(language, name) });
    }

    static object Invoke(Component target, string name)
    {
        MethodInfo method = target.GetType().GetMethod(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, name);
        return method.Invoke(target, null);
    }

    static T GetField<T>(Component target, string name)
    {
        FieldInfo field = target.GetType().GetField(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        return (T)field.GetValue(target);
    }

    static void SetField<T>(Component target, string name, T value)
    {
        FieldInfo field = target.GetType().GetField(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        field.SetValue(target, value);
    }

    static T GetProperty<T>(Component target, string name)
    {
        PropertyInfo property = target.GetType().GetProperty(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, name);
        return (T)property.GetValue(target);
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
        Assert.That(type, Is.Not.Null, name);
        return type;
    }
}
