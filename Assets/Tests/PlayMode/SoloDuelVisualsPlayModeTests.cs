using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class SoloDuelVisualsPlayModeTests
{
    const BindingFlags StaticFlags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

    string[] dailyIntegerKeys;
    bool[] dailyIntegerExisted;
    int[] dailyIntegerValues;
    string dailyLastPlayDateKey;
    bool dailyLastPlayDateExisted;
    string dailyLastPlayDateValue;
    int originalScreenWidth;
    int originalScreenHeight;
    bool originalFullScreen;
    readonly List<string> activeGlyphViolations = new List<string>();

    [SetUp]
    public void CaptureFixtureState()
    {
        originalScreenWidth = Screen.width;
        originalScreenHeight = Screen.height;
        originalFullScreen = Screen.fullScreen;

        dailyIntegerKeys = new[]
        {
            RuntimeConstant<string>("DailyChallengeProgress", "DayKey"),
            RuntimeConstant<string>("DailyChallengeProgress", "WinsKey"),
            RuntimeConstant<string>(
                "DailyChallengeProgress", "CorrectGuessesKey"),
            RuntimeConstant<string>(
                "DailyChallengeProgress", "RoomsSharedKey"),
            RuntimeConstant<string>(
                "DailyChallengeProgress", "RewardClaimedKey"),
            RuntimeConstant<string>("DailyChallengeProgress", "PointsKey"),
            RuntimeConstant<string>("DailyStreak", "StreakKey"),
        };
        dailyIntegerExisted = dailyIntegerKeys
            .Select(PlayerPrefs.HasKey).ToArray();
        dailyIntegerValues = dailyIntegerKeys
            .Select(key => PlayerPrefs.GetInt(key, 0)).ToArray();

        dailyLastPlayDateKey = RuntimeConstant<string>(
            "DailyStreak", "LastPlayDateKey");
        dailyLastPlayDateExisted =
            PlayerPrefs.HasKey(dailyLastPlayDateKey);
        dailyLastPlayDateValue = PlayerPrefs.GetString(
            dailyLastPlayDateKey, string.Empty);
    }

    [UnityTearDown]
    public IEnumerator RestoreFixtureState()
    {
        // Quiesce the scene before restoring prefs. ExtrasRuntimeWiring creates
        // both daily trackers one frame after Start; leaving MainMenu alive can
        // otherwise rewrite the caller's state after restoration.
        Scene active = SceneManager.GetActiveScene();
        Scene quiescent = SceneManager.CreateScene(
            "SoloDuelVisualsQuiescent_" + Guid.NewGuid().ToString("N"));
        SceneManager.SetActiveScene(quiescent);
        if (active.IsValid() && active.isLoaded &&
            active.handle != quiescent.handle)
            yield return SceneManager.UnloadSceneAsync(active);
        yield return null;
#if UNITY_EDITOR
        FirstLaunchSoloEndToEndPlayModeTests
            .RestoreEditorWindowAfterSettlement();
#endif

        for (int index = 0; index < dailyIntegerKeys.Length; index++)
        {
            RestoreInt(
                dailyIntegerKeys[index],
                dailyIntegerExisted[index],
                dailyIntegerValues[index]);
        }
        if (dailyLastPlayDateExisted)
            PlayerPrefs.SetString(
                dailyLastPlayDateKey, dailyLastPlayDateValue);
        else
            PlayerPrefs.DeleteKey(dailyLastPlayDateKey);
        PlayerPrefs.Save();

        Screen.SetResolution(
            originalScreenWidth,
            originalScreenHeight,
            originalFullScreen);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ValidateSoloDuelRequiredViewportMatrix()
    {
        activeGlyphViolations.Clear();
        bool hadName = PlayerPrefs.HasKey("PlayerName");
        bool hadWins = PlayerPrefs.HasKey("StatWins");
        bool hadDifficulty = PlayerPrefs.HasKey("AIDifficulty");
        bool hadLanguage = PlayerPrefs.HasKey("Language");
        string profileVersionKey = OnboardingProfileConstant<string>("VersionKey");
        string avatarKey = OnboardingProfileConstant<string>("AvatarKey");
        bool hadProfileVersion = PlayerPrefs.HasKey(profileVersionKey);
        bool hadAvatar = PlayerPrefs.HasKey(avatarKey);
        string oldName = PlayerPrefs.GetString("PlayerName", string.Empty);
        int oldWins = PlayerPrefs.GetInt("StatWins", 0);
        int oldDifficulty = PlayerPrefs.GetInt("AIDifficulty", 1);
        int oldLanguage = PlayerPrefs.GetInt("Language", 0);
        int oldProfileVersion = PlayerPrefs.GetInt(profileVersionKey, 0);
        int oldAvatar = PlayerPrefs.GetInt(avatarKey, 0);

        try
        {
            PlayerPrefs.SetString("PlayerName", "ALEXANDERMAX");
            PlayerPrefs.SetInt("StatWins", 2450);
            PlayerPrefs.SetInt("AIDifficulty", 3);
            PlayerPrefs.SetInt("Language", 0);
            PlayerPrefs.DeleteKey(profileVersionKey);
            PlayerPrefs.DeleteKey(avatarKey);
            PlayerPrefs.Save();

            Screen.SetResolution(1080, 1920, false);
            yield return SceneManager.LoadSceneAsync(
                "MainMenu", LoadSceneMode.Single);

            Component layout = null;
            Component numberManager = null;
            for (int frame = 0; frame < 120; frame++)
            {
                layout = FindInScene(RuntimeType("SoloDuelVisuals"));
                numberManager = FindInScene(RuntimeType("NumberManager"));
                if (layout != null && numberManager != null)
                    break;
                yield return null;
            }

            Assert.That(layout, Is.Not.Null);
            Assert.That(numberManager, Is.Not.Null);
            numberManager.gameObject.SetActive(true);
            for (int frame = 0; frame < 8; frame++)
                yield return null;

            TMP_InputField input = GetField<TMP_InputField>(
                numberManager, "numberInput");
            Transform visualRoot = Find(
                numberManager.transform, "SoloDuelVisualRoot");
            Assert.That(visualRoot, Is.Not.Null);
            Canvas canvas = visualRoot.GetComponentInParent<Canvas>();
            Assert.That(canvas, Is.Not.Null);
            Transform safeRoot = Find(visualRoot, "SoloDuelSafeRoot");
            Assert.That(safeRoot, Is.Not.Null);
            Component safeAreaOwner = safeRoot.GetComponent(
                RuntimeType("ResponsiveSafeAreaRoot"));
            Assert.That(safeAreaOwner, Is.Not.Null);

            foreach (string language in new[] { "English", "Greek" })
            {
                bool greek = language == "Greek";
                SetLanguage(language);
                PlayerPrefs.SetString(
                    "PlayerName", greek ? "ΚΩΝΣΤΑΝΤΙΝΟΣ" : "ALEXANDERMAX");
                PlayerPrefs.Save();
                input.SetTextWithoutNotify(string.Empty);
                yield return null;
                yield return null;

                string locale = greek ? "el" : "en";
                foreach (Vector2Int viewport in new[]
                {
                    new Vector2Int(720, 1280),
                    new Vector2Int(1080, 1920),
                    new Vector2Int(1080, 2400),
                    new Vector2Int(1179, 2556),
                })
                {
                    yield return ValidateSoloStateMatrix(
                        canvas, safeAreaOwner, layout, numberManager,
                        viewport.x, viewport.y, locale,
                        greek ? "ΚΩΝΣΤΑΝΤΙΝΟΣ" : "KONSTANTINOS");
                }
            }
        }
        finally
        {
            RestoreString("PlayerName", hadName, oldName);
            RestoreInt("StatWins", hadWins, oldWins);
            RestoreInt("AIDifficulty", hadDifficulty, oldDifficulty);
            RestoreInt("Language", hadLanguage, oldLanguage);
            RestoreInt(
                profileVersionKey,
                hadProfileVersion,
                oldProfileVersion);
            RestoreInt(avatarKey, hadAvatar, oldAvatar);
            PlayerPrefs.Save();
        }

        if (activeGlyphViolations.Count > 0)
        {
            string report = "Solo active-glyph viewport matrix found " +
                activeGlyphViolations.Count + " violation(s):\n" +
                string.Join("\n", activeGlyphViolations.Select(
                    (violation, index) =>
                        $"{index + 1}. {violation}"));
            // A normal log preserves the complete diagnostic in Editor.log
            // even when the Test Runner details pane truncates long failures.
            Debug.Log(report);
            Assert.Fail(report);
        }
    }

    [UnityTest]
    public IEnumerator SoloBoardMatchesApprovedCartoonCompositionAndKeepsRealControls()
    {
        string profileVersionKey = OnboardingProfileConstant<string>("VersionKey");
        string avatarKey = OnboardingProfileConstant<string>("AvatarKey");
        bool hadProfileVersion = PlayerPrefs.HasKey(profileVersionKey);
        bool hadAvatar = PlayerPrefs.HasKey(avatarKey);
        int oldProfileVersion = PlayerPrefs.GetInt(profileVersionKey, 0);
        int oldAvatar = PlayerPrefs.GetInt(avatarKey, 0);

        try
        {
            PlayerPrefs.DeleteKey(profileVersionKey);
            PlayerPrefs.DeleteKey(avatarKey);
            PlayerPrefs.Save();

            Screen.SetResolution(1080, 1920, false);
            yield return SceneManager.LoadSceneAsync(
                "MainMenu", LoadSceneMode.Single);

        Component layout = null;
        Component numberManager = null;
        for (int frame = 0; frame < 120; frame++)
        {
            layout = FindInScene(RuntimeType("SoloDuelVisuals"));
            numberManager = FindInScene(RuntimeType("NumberManager"));
            if (layout != null && numberManager != null)
                break;
            yield return null;
        }

        Assert.That(layout, Is.Not.Null);
        Assert.That(numberManager, Is.Not.Null);
        numberManager.gameObject.SetActive(true);
        for (int frame = 0; frame < 6; frame++)
            yield return null;

        Assert.That(GetProperty<bool>(layout, "IsReady"), Is.True);
        Assert.That(CountInScene(RuntimeType("SoloDuelVisuals")), Is.EqualTo(1));

        Transform root = Find(numberManager.transform, "SoloDuelVisualRoot");
        Assert.That(root, Is.Not.Null);

        foreach (string name in new[]
        {
            "SoloDuelBackground",
            "SoloDuelDecorations",
            "SoloDuelSafeRoot",
            "DuelBack",
            "SoloDuelLogo",
            "SoloDuelPlayerChip",
            "SoloDuelChipAvatar",
            "SoloDuelChipTrophy",
            "SoloDuelChipText",
            "PlayerCard",
            "PlayerCharacter",
            "PlayerCaption",
            "PlayerName",
            "OpponentCard",
            "OpponentCharacter",
            "OpponentCaption",
            "OpponentCardTrophy",
            "OpponentDifficulty",
            "SoloVsBurst",
            "SoloVsOutline",
            "SoloVsLabel",
            "SoloPromptRibbon",
            "RoundLabel",
            "SoloDuelMascotSeven",
            "SoloDuelMascotThree",
            "SoloInteractionCard",
            "CurrentNumberHeading",
            "SoloOpponentRail",
            "SoloOpponentBubble",
            "SoloOpponentBubbleArtwork",
            "OpponentBubbleAvatar",
            "OpponentBubblePrompt",
            "OpponentReaction",
            "HistoryCard",
            "HistoryTitle",
            "HistoryRow1",
            "HistoryRow2",
            "HistoryRow3",
            "SoloTipCard",
            "SoloTipHeading",
            "SoloTipBulb",
            "SoloTipMascot",
            "NumberKeypad",
            "ButtonConfirm",
        })
        {
            Assert.That(Find(root, name), Is.Not.Null,
                "Missing approved Solo duel object: " + name);
        }

        Assert.That(root.GetSiblingIndex(),
            Is.EqualTo(root.parent.childCount - 1),
            "The single production owner must render above retired siblings.");
        Assert.That(Find(root, "SoloDuelOuterFrame"), Is.Null,
            "The canonical Solo reference has no full-screen arcade frame.");

        AssertSprite(root, "SoloDuelLogo", "reference/hol_logo_exact");
        AssertSprite(root, "PlayerCharacter", "reference/player_cyan_exact");
        AssertSprite(root, "OpponentCharacter", "reference/opponent_purple_exact");
        AssertSprite(root, "SoloVsBurst",
            "solo/production/solo_vs_burst_v2");
        AssertSprite(root, "PlayerCardTrophy",
            "solo/production/solo_trophy_v1");
        AssertSprite(root, "SoloDuelChipTrophy",
            "solo/production/solo_trophy_v1");
        AssertSprite(root, "SoloDuelMascotSeven", "reference/mascot_7_exact");
        AssertSprite(root, "SoloDuelMascotThree", "reference/mascot_3_exact");
        AssertSprite(root, "SoloTipMascot", "reference/mascot_7_exact");
        AssertSprite(root, "SoloTipBulb",
            "solo/production/solo_tip_bulb_v1");

        AssertSprite(root, "SoloDuelDecorations",
            "solo/production/solo_decorations_v1");
        AssertButtonSprite(root, "DuelBack",
            "solo/production/solo_back_button_v1");
        AssertSprite(root, "SoloDuelPlayerChip",
            "solo/production/solo_player_chip_v1");
        AssertSprite(root, "PlayerCard",
            "solo/production/solo_player_card_shell_v1");
        AssertSprite(root, "OpponentCard",
            "solo/production/solo_opponent_card_shell_v1");
        AssertSprite(root, "SoloPromptRibbon",
            "solo/production/solo_prompt_ribbon_v1");
        AssertSprite(root, "SoloInteractionCard",
            "solo/production/solo_interaction_board_v2");
        AssertSprite(root, "SoloDuelChipAvatar",
            "solo/production/solo_player_avatar_v1");
        AssertSprite(root, "OpponentBubbleAvatar",
            "solo/production/solo_opponent_medallion_v1");
        AssertSprite(root, "OpponentReaction",
            "solo/production/solo_reaction_emoji_v1");
        AssertSprite(root, "SoloOpponentBubbleArtwork",
            "solo/production/solo_opponent_speech_bubble_v2");
        Assert.That(Find(root, "SoloOpponentBubbleArtwork").localScale.x,
            Is.LessThan(0f),
            "The bubble tail must point right toward the opponent medallion.");
        AssertSprite(root, "SoloTipCard",
            "solo/production/solo_tip_board_v1");
        AssertButtonSprite(root, "ButtonConfirm",
            "solo/production/solo_primary_cta_v1");
        TMP_Text submitLabel = Find(root, "ButtonConfirm")
            .GetComponentInChildren<TMP_Text>(true);
        Assert.That(submitLabel.text == "SUBMIT!" || submitLabel.text == "ΣΤΕΙΛΕ!",
            Is.True,
            "The live localized submit label must remain EN/EL, not baked art.");
        Component submitLocalization = submitLabel.GetComponent(
            RuntimeType("LocalizedText"));
        Assert.That(submitLocalization, Is.Not.Null);
        Assert.That(GetField<string>(submitLocalization, "key"),
            Is.EqualTo("solo_submit"));

        Transform keypad = Find(root, "NumberKeypad");
        Button[] keypadButtons = keypad.GetComponentsInChildren<Button>(true);
        Assert.That(keypadButtons, Has.Length.EqualTo(12));
        foreach (Button key in keypadButtons)
        {
            Assert.That(key.GetComponent<Image>().sprite,
                Is.SameAs(Resources.Load<Sprite>(
                    "solo/production/solo_keypad_key_v1")));
            Assert.That(key.GetComponent<Image>().type,
                Is.EqualTo(Image.Type.Simple));
            Assert.That(key.GetComponent<RectTransform>().rect.width,
                Is.GreaterThanOrEqualTo(160f));
            Assert.That(key.GetComponent<RectTransform>().rect.height,
                Is.GreaterThanOrEqualTo(108f));
        }

        TMP_InputField input = GetField<TMP_InputField>(
            numberManager, "numberInput");
        Assert.That(input, Is.Not.Null);
        Assert.That(input.shouldHideMobileInput, Is.True);
        Assert.That(input.shouldHideSoftKeyboard, Is.True);
        Assert.That(GetProperty<Button>(layout, "SubmitControl"), Is.Not.Null);
        Assert.That(GetProperty<GameObject>(layout, "KeypadRoot"),
            Is.SameAs(keypad.gameObject));

        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            Assert.That(
                graphic is Image || graphic is TMP_Text ||
                graphic is TMP_SubMeshUI ||
                graphic.GetType().Name == "TMP_SelectionCaret",
                Is.True,
                "Procedural Graphic found in Solo duel: " +
                graphic.GetType().Name + " / " + graphic.name);
            if (graphic is Image image && image.sprite != null)
                Assert.That(image.color.a, Is.GreaterThanOrEqualTo(0.99f),
                    image.name + " hides approved production art.");
        }

        Component gameManager = FindInScene(RuntimeType("GameManager"));
        TMP_Text prompt = GetField<TMP_Text>(gameManager, "turnText");
        TMP_Text opponentIdentity = GetField<TMP_Text>(gameManager, "opponentNameText");
        TMP_Text range = GetField<TMP_Text>(gameManager, "rangeText");
        Assert.That(prompt.transform.IsChildOf(root), Is.True,
            "The real GameManager prompt must be seated in the approved ribbon.");
        Assert.That(opponentIdentity.transform.IsChildOf(
            Find(root, "OpponentCard")), Is.True,
            "The real opponent identity must be seated in the magenta card.");
        Assert.That(range.transform.IsChildOf(
            Find(root, "SoloTipCard")), Is.True,
            "The real range text must be seated in the contextual tip card.");
        RectTransform tipMascot = (RectTransform)Find(root, "SoloTipMascot");
        RectTransform tipPanel = range.rectTransform.parent as RectTransform;
        range.ForceMeshUpdate(true, true);
        Assert.That(
            TryRenderedGlyphRectIn(
                tipPanel, range, out Rect rangeGlyphs, out int glyphCount),
            Is.True,
            "Live range text must render before mascot exclusion is measured.");
        Assert.That(glyphCount, Is.GreaterThan(0));
        Bounds mascotBounds =
            RectTransformUtility.CalculateRelativeRectTransformBounds(
                tipPanel, tipMascot);
        Assert.That(mascotBounds.min.x - rangeGlyphs.xMax,
            Is.GreaterThanOrEqualTo(8f),
            "Live range glyphs must keep the required gap from the Tip mascot.");

        string[] answerNames =
        {
            "ButtonHIGHER", "ButtonCORRECT", "ButtonLOWER",
        };
        foreach (string answerName in answerNames)
        {
            Button answer = Find(numberManager.transform, answerName)
                .GetComponent<Button>();
            Assert.That(answer, Is.Not.Null);
            Assert.That(answer.targetGraphic, Is.SameAs(answer.GetComponent<Image>()));
        }

        Assert.That(numberManager.GetComponentsInChildren<Button>(true)
            .Count(button => button.name == "ButtonConfirm" &&
                             button.gameObject.activeInHierarchy),
            Is.EqualTo(1),
            "The cartoon board must expose one real submit control.");

        MethodInfo applyResponsive = layout.GetType().GetMethod(
            "ApplyResponsiveLayoutForViewport",
            BindingFlags.Instance | BindingFlags.Public);
        Assert.That(applyResponsive, Is.Not.Null);

        applyResponsive.Invoke(layout, new object[] { 1080f, 1920f });
        RectTransform logoRect = (RectTransform)Find(root, "SoloDuelLogo");
        RectTransform playerCardRect = (RectTransform)Find(root, "PlayerCard");
        RectTransform promptRect = (RectTransform)Find(root, "SoloPromptRibbon");
        RectTransform interactionRect =
            (RectTransform)Find(root, "SoloInteractionCard");
        RectTransform firstKey = (RectTransform)Find(root, "Key_1");
        RectTransform submitRect = (RectTransform)Find(root, "ButtonConfirm");
        float canonicalLogoY = logoRect.anchoredPosition.y;
        float canonicalCardY = playerCardRect.anchoredPosition.y;
        float canonicalBoardHeight = interactionRect.sizeDelta.y;
        float canonicalKeyHeight = firstKey.sizeDelta.y;
        float canonicalSubmitHeight = submitRect.sizeDelta.y;

        applyResponsive.Invoke(layout, new object[] { 1080f, 2400f });
        Assert.That(GetProperty<float>(layout, "CurrentTallBlend"),
            Is.EqualTo(1f).Within(0.001f));
        Assert.That(logoRect.anchoredPosition.y, Is.GreaterThan(canonicalLogoY),
            "Tall layout must move HOL into the otherwise dead top region.");
        Assert.That(playerCardRect.anchoredPosition.y, Is.GreaterThan(canonicalCardY),
            "Tall layout must keep the competitive header visually attached to HOL.");
        Assert.That(interactionRect.sizeDelta.y,
            Is.GreaterThan(canonicalBoardHeight),
            "Tall layout must expand the real interaction board, not center a 1920 block.");
        Assert.That(firstKey.sizeDelta.y, Is.GreaterThan(canonicalKeyHeight),
            "Tall layout must give keypad buttons the approved larger touch weight.");
        Assert.That(submitRect.sizeDelta.y, Is.GreaterThan(canonicalSubmitHeight),
            "Tall layout must preserve the gold CTA as the strongest action.");
        float promptBottom = promptRect.anchoredPosition.y -
                             promptRect.sizeDelta.y * 0.5f;
        float boardTop = interactionRect.anchoredPosition.y +
                         interactionRect.sizeDelta.y * 0.5f;
        float transparentRectOverlap = boardTop - promptBottom;
        Assert.That(transparentRectOverlap, Is.InRange(0f, 70f),
            "Tall reflow may overlap transparent sprite bounds only within the measured reference range.");
        float virtualHalfHeight = 1200f;
        float logoTop = logoRect.anchoredPosition.y +
                        logoRect.sizeDelta.y * 0.5f;
        float boardBottom = interactionRect.anchoredPosition.y -
                            interactionRect.sizeDelta.y * 0.5f;
        Assert.That(virtualHalfHeight - logoTop,
            Is.InRange(100f, 170f),
            "Tall reflow must keep the HOL header fully visible without a dead top zone.");
        Assert.That(boardBottom + virtualHalfHeight,
            Is.InRange(120f, 200f),
            "Tall reflow must preserve the measured lower breathing room without letterboxing.");

        // Restore canonical geometry so later assertions/tests never inherit a
        // synthetic tall viewport from this responsive behavior check.
        applyResponsive.Invoke(layout, new object[] { 1080f, 1920f });
        }
        finally
        {
            RestoreInt(
                profileVersionKey,
                hadProfileVersion,
                oldProfileVersion);
            RestoreInt(avatarKey, hadAvatar, oldAvatar);
            PlayerPrefs.Save();
        }

    }

    [UnityTest]
    public IEnumerator SoloOwnerContainsEdgeViewportsAndKeepsLocalizedPromptReadable()
    {
        bool hadLanguage = PlayerPrefs.HasKey("Language");
        int oldLanguage = PlayerPrefs.GetInt("Language", 0);

        try
        {
            Screen.SetResolution(1080, 1920, false);
            yield return SceneManager.LoadSceneAsync(
                "MainMenu", LoadSceneMode.Single);

            Component layout = null;
            Component numberManager = null;
            Component gameManager = null;
            for (int frame = 0; frame < 180; frame++)
            {
                layout = FindInScene(RuntimeType("SoloDuelVisuals"));
                numberManager = FindInScene(RuntimeType("NumberManager"));
                gameManager = FindInScene(RuntimeType("GameManager"));
                if (layout != null && numberManager != null &&
                    gameManager != null)
                    break;
                yield return null;
            }

            Assert.That(layout, Is.Not.Null);
            Assert.That(numberManager, Is.Not.Null);
            Assert.That(gameManager, Is.Not.Null);
            numberManager.gameObject.SetActive(true);
            for (int frame = 0; frame < 12; frame++)
                yield return null;

            Assert.That(GetProperty<bool>(layout, "IsReady"), Is.True);
            Transform root = Find(
                numberManager.transform, "SoloDuelVisualRoot");
            Transform safeRoot = Find(root, "SoloDuelSafeRoot");
            Assert.That(root, Is.Not.Null);
            Assert.That(safeRoot, Is.Not.Null);

            Component safeAreaOwner = safeRoot.GetComponent(
                RuntimeType("ResponsiveSafeAreaRoot"));
            Assert.That(safeAreaOwner, Is.Not.Null,
                "SoloDuelSafeRoot must retain the single safe-area owner.");
            MethodInfo applySafeArea = safeAreaOwner.GetType().GetMethod(
                "ApplyViewport", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo applyResponsive = layout.GetType().GetMethod(
                "ApplyResponsiveLayoutForViewport",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(applySafeArea, Is.Not.Null);
            Assert.That(applyResponsive, Is.Not.Null);

            TMP_Text phasePrompt = GetField<TMP_Text>(gameManager, "turnText");
            TMP_Text roundLabel = Find(root, "RoundLabel")
                .GetComponent<TMP_Text>();
            Assert.That(phasePrompt, Is.Not.Null);
            Assert.That(roundLabel, Is.Not.Null);

            string[] criticalBounds =
            {
                "DuelBack",
                "SoloDuelLogo",
                "SoloDuelPlayerChip",
                "SoloDuelChipAvatar",
                "PlayerCard",
                "OpponentCard",
                "SoloVsBurst",
                "SoloDuelMascotSeven",
                "SoloDuelMascotThree",
                "SoloPromptRibbon",
                // The approved interaction-board sprite contains transparent
                // glow bleed outside its visible bezel. Validate the owned
                // interactive content rather than treating transparent pixels
                // as clipped production UI.
                "CurrentNumberHeading",
                "CurrentRangeLabel",
                "CentralGuess",
                "CentralOutcome",
                "SoloContinueButton",
                "NumberKeypad",
                "ButtonConfirm",
                "LockButton",
                "SoloOpponentRail",
                "OpponentBubbleAvatar",
                "HistoryCard",
                "HistoryViewport",
                "SoloTipCard",
                "PlayerRangeLabel",
                "OpponentRangeLabel",
                "LockExplanation",
                "SoloTipMascot",
            };
            Vector2[] viewports =
            {
                new Vector2(720f, 1280f),
                new Vector2(1080f, 1920f),
                new Vector2(1080f, 2400f),
                new Vector2(1179f, 2556f),
            };

            string opponent = GetProperty<string>(
                gameManager, "CurrentOpponentName");
            MethodInfo begin = layout.GetType().GetMethod(
                "BeginNewMatch",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(begin, Is.Not.Null);
            begin.Invoke(layout, new object[] { opponent });
            PresentPhase(layout, "PlayerGuess", "YourGuess", 11);
            RecordPlayerMove(layout, 11, 42, "Higher", 43, 100);

            foreach (string language in new[] { "English", "Greek" })
            {
                SetLanguage(language);
                PresentPhase(layout, "PlayerGuess", "YourGuess", 11);
                string expectedPrompt = Localized(
                    "solo_player_turn", opponent);
                string expectedRound = Localized(
                    "round_label_open", 11);
                Assert.That(phasePrompt.text, Is.EqualTo(expectedPrompt),
                    language + " live Solo prompt must remain truthful.");
                Assert.That(roundLabel.text, Is.EqualTo(expectedRound),
                    language + " round 11 must use the unbounded round label.");
                Assert.That(roundLabel.text, Does.Contain("11"));
                Assert.That(roundLabel.text, Does.Not.Contain("/"),
                    "Solo rules do not impose a ten-round cap.");
                Assert.That(Find(root, "PlayerCard").GetComponent<CanvasGroup>().alpha,
                    Is.EqualTo(1f).Within(0.001f));
                Assert.That(Find(root, "OpponentCard").GetComponent<CanvasGroup>().alpha,
                    Is.LessThan(0.7f),
                    language + " opponent card must be visibly inactive.");
                Assert.That(Find(root, "PlayerActiveBadgeLabel")
                    .GetComponent<TMP_Text>().text,
                    Is.EqualTo(Localized("solo_player_active")));
                Assert.That(Find(root, "OpponentActiveBadgeLabel")
                    .GetComponent<TMP_Text>().text,
                    Is.EqualTo(Localized("solo_waiting")));

                Transform firstHistory = Find(root, "HistoryRow1");
                Assert.That(firstHistory, Is.Not.Null);
                string historyMeta = Find(firstHistory, "HistoryMeta")
                    .GetComponent<TMP_Text>().text;
                Assert.That(historyMeta,
                    Does.Contain(Localized("solo_you_header")));
                Assert.That(historyMeta, Does.Contain(opponent));
                Assert.That(historyMeta, Does.Contain(">"),
                    language + " history must name both actor and target.");

                foreach (Vector2 viewport in viewports)
                {
                    Rect[] safeAreas =
                    {
                        new Rect(Vector2.zero, viewport),
                        new Rect(
                            0f, viewport.y * 0.05f,
                            viewport.x, viewport.y * 0.87f),
                    };
                    Vector2 canvasSize = CanvasSize(viewport);
                    Rect canvasRect = new Rect(
                        canvasSize * -0.5f, canvasSize);

                    foreach (Rect safeArea in safeAreas)
                    {
                        applySafeArea.Invoke(safeAreaOwner, new object[]
                        {
                            new Rect(Vector2.zero, viewport),
                            safeArea,
                            canvasSize,
                        });
                        applyResponsive.Invoke(layout, new object[]
                        {
                            safeArea.width,
                            safeArea.height,
                        });
                        Canvas.ForceUpdateCanvases();

                        Rect safeRect = GetProperty<Rect>(
                            safeAreaOwner, "LastSafeRect");
                        string context = language + " / " + viewport +
                                         " / " + safeArea;
                        AssertContained(
                            canvasRect, safeRect,
                            context + " / safe area inside canvas");
                        foreach (string targetName in criticalBounds)
                        {
                            RectTransform target = Find(root, targetName)
                                as RectTransform;
                            Assert.That(target, Is.Not.Null,
                                "Missing critical Solo target " + targetName);
                            AssertContained(
                                safeRect,
                                BoundsInSafeRect(
                                    safeRoot as RectTransform,
                                    target,
                                    safeRect),
                                context + " / " + targetName);
                        }

                        Assert.That(phasePrompt.text,
                            Is.EqualTo(expectedPrompt));
                        Assert.That(roundLabel.text,
                            Is.EqualTo(expectedRound));
                        AssertRenderedTextWithinRect(
                            phasePrompt,
                            context + " / phase prompt");
                        AssertRenderedTextWithinRect(
                            roundLabel,
                            context + " / round label");
                        foreach (string textName in new[]
                        {
                            "SoloDuelChipText",
                            "PlayerCaption",
                            "PlayerName",
                            "PlayerWins",
                            "OpponentCaption",
                            "OpponentDifficulty",
                            "HistoryTitle",
                            "SoloTipHeading",
                        })
                        {
                            TMP_Text text = Find(root, textName)
                                ?.GetComponent<TMP_Text>();
                            Assert.That(text, Is.Not.Null,
                                context + " / " + textName);
                            AssertRenderedTextWithinRect(
                                text, context + " / " + textName);
                            if (textName == "OpponentDifficulty")
                            {
                                AssertRenderedTextHorizontalSafety(
                                    text, 1f,
                                    context + " / " + textName);
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            SetLanguage(oldLanguage == 1 ? "Greek" : "English");
            RestoreInt("Language", hadLanguage, oldLanguage);
            PlayerPrefs.Save();
        }
    }

    [UnityTest]
    public IEnumerator OneOwnerPreservesControllerControlsAndPhaseTruth()
    {
        Screen.SetResolution(1080, 1920, false);
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);

        Component layout = FindInScene(RuntimeType("SoloDuelVisuals"));
        Component numberManager = FindInScene(RuntimeType("NumberManager"));
        Component gameManager = FindInScene(RuntimeType("GameManager"));
        Assert.That(layout, Is.Not.Null);
        Assert.That(numberManager, Is.Not.Null);
        Assert.That(gameManager, Is.Not.Null);

        numberManager.gameObject.SetActive(true);
        for (int frame = 0; frame < 180; frame++)
        {
            if (GetProperty<bool>(layout, "IsReady") &&
                Find(numberManager.transform, "LockButton") != null)
                break;
            yield return null;
        }

        Transform root = Find(numberManager.transform, "SoloDuelVisualRoot");
        Transform safeRoot = Find(root, "SoloDuelSafeRoot");
        Transform interaction = Find(root, "SoloInteractionCard");
        Transform tip = Find(root, "SoloTipCard");
        Assert.That(root, Is.Not.Null);
        Assert.That(safeRoot, Is.Not.Null);
        Assert.That(interaction, Is.Not.Null);
        Assert.That(tip, Is.Not.Null);

        Assert.That(Type.GetType(
            "SoloDuelPresentationHardener, Assembly-CSharp"), Is.Null);
        Assert.That(Type.GetType(
            "SoloDuelVisualIntegrityGuard, Assembly-CSharp"), Is.Null);

        TMP_Text message = GetField<TMP_Text>(numberManager, "messageText");
        TMP_Text value = GetField<TMP_Text>(numberManager, "playerNumberText");
        TMP_InputField input = GetField<TMP_InputField>(numberManager, "numberInput");
        Assert.That(message.transform.IsChildOf(interaction), Is.True);

        GameObject stop = GetField<GameObject>(gameManager, "stopGameButton");
        GameObject higher = GetField<GameObject>(gameManager, "higherButton");
        GameObject correct = GetField<GameObject>(gameManager, "correctButton");
        GameObject lower = GetField<GameObject>(gameManager, "lowerButton");
        Assert.That(stop.transform.IsChildOf(safeRoot), Is.True);
        Transform lockButton = Find(root, "LockButton");
        Assert.That(lockButton, Is.Not.Null);
        Assert.That(lockButton.IsChildOf(interaction), Is.True,
            "Lock belongs beside Submit and must not cover strategy text.");
        Assert.That(higher.transform.IsChildOf(interaction), Is.True);
        Assert.That(correct.transform.IsChildOf(interaction), Is.True);
        Assert.That(lower.transform.IsChildOf(interaction), Is.True);

        GameObject legacyPlayer = GetField<GameObject>(
            numberManager, "playerGuessesPanel");
        GameObject legacyAi = GetField<GameObject>(
            numberManager, "aiGuessesPanel");
        legacyPlayer.SetActive(true);
        legacyAi.SetActive(true);
        PresentPhase(layout, "ChooseSecret", "EnterSecret");
        Assert.That(legacyPlayer.activeSelf, Is.False);
        Assert.That(legacyAi.activeSelf, Is.False);
        Assert.That(input.gameObject.activeSelf, Is.True);
        Assert.That(value.gameObject.activeSelf, Is.False);
        Assert.That(new[] { higher, correct, lower }.Any(x => x.activeSelf),
            Is.False);

        PresentPhase(layout, "AnswerOpponent", "AnswerOpponent");
        Assert.That(input.gameObject.activeSelf, Is.False);
        Assert.That(value.gameObject.activeSelf, Is.False,
            "The central factual surface replaces the ambiguous legacy value.");
        Assert.That(Find(root, "CentralGuess").gameObject.activeSelf, Is.True);
        higher.SetActive(true);
        yield return null;
        Assert.That(new[] { higher, correct, lower }.Any(x => x.activeSelf),
            Is.False,
            "Solo already knows the secret and presents automatic factual feedback.");

        PresentPhase(layout, "PlayerGuess", "YourGuess");
        Assert.That(new[] { higher, correct, lower }.Any(x => x.activeSelf),
            Is.False);
        Assert.That(input.gameObject.activeSelf, Is.True);
        Assert.That(value.gameObject.activeSelf, Is.False);
    }

    [UnityTest]
    public IEnumerator SoloChipUsesCanonicalAvatarContractAndTruthfulDifficulty()
    {
        string playerNameKey =
            OnboardingProfileConstant<string>("PlayerNameKey");
        string profileVersionKey = OnboardingProfileConstant<string>("VersionKey");
        string genderKey = OnboardingProfileConstant<string>("GenderKey");
        string avatarKey = OnboardingProfileConstant<string>("AvatarKey");
        string ageKey = OnboardingProfileConstant<string>("AgeKey");
        int currentProfileVersion =
            OnboardingProfileConstant<int>("CurrentVersion");
        string[] integerKeys =
        {
            profileVersionKey,
            genderKey,
            avatarKey,
            ageKey,
            "AIDifficulty",
            "Language",
        };
        bool[] integerExisted = integerKeys
            .Select(PlayerPrefs.HasKey).ToArray();
        int[] integerValues = integerKeys
            .Select(key => PlayerPrefs.GetInt(key, 0)).ToArray();
        bool playerNameExisted = PlayerPrefs.HasKey(playerNameKey);
        string playerNameValue = PlayerPrefs.GetString(
            playerNameKey, string.Empty);

        try
        {
            PlayerPrefs.DeleteKey(profileVersionKey);
            PlayerPrefs.DeleteKey(avatarKey);
            PlayerPrefs.Save();
            SetLanguage("English");
            Screen.SetResolution(1080, 1920, false);
            yield return SceneManager.LoadSceneAsync(
                "MainMenu", LoadSceneMode.Single);

            Component layout = null;
            yield return EnterSoloThroughProductionPath(
                readyLayout => layout = readyLayout);

            Assert.That(layout, Is.Not.Null);
            Transform root = Find(
                layout.transform, "SoloDuelVisualRoot");
            Assert.That(root, Is.Not.Null);
            Image avatar = Find(root, "SoloDuelChipAvatar")
                .GetComponent<Image>();
            Assert.That(avatar.raycastTarget, Is.False);
            Assert.That(avatar.GetComponentInParent<Button>(), Is.Null,
                "The Solo profile chip is display-only, not a deceptive control.");

            MethodInfo render = layout.GetType().GetMethod(
                "Render", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(render, Is.Not.Null);

            Sprite first = null;
            Sprite second = null;
            int avatarCount = OnboardingAvatarCount();
            int reloadedSelections = 0;
            for (int index = 0; index < avatarCount; index++)
            {
                if (!IsValidOnboardingAvatar(index))
                    continue;

                Assert.That(TryCommitOnboardingAvatar(index), Is.True,
                    "Canonical Onboarding must commit selectable avatar " +
                    index + ".");
                if (reloadedSelections < 2)
                {
                    yield return SceneManager.LoadSceneAsync(
                        "MainMenu", LoadSceneMode.Single);
                    layout = null;
                    yield return EnterSoloThroughProductionPath(
                        readyLayout => layout = readyLayout);

                    Assert.That(layout, Is.Not.Null,
                        "Solo owner did not return after avatar reload.");
                    root = Find(layout.transform, "SoloDuelVisualRoot");
                    Assert.That(root, Is.Not.Null);
                    avatar = Find(root, "SoloDuelChipAvatar")
                        .GetComponent<Image>();
                    render = layout.GetType().GetMethod(
                        "Render", BindingFlags.Instance |
                                  BindingFlags.NonPublic);
                    Assert.That(render, Is.Not.Null);
                    reloadedSelections++;
                }
                else
                {
                    render.Invoke(layout, null);
                }

                string resourcePath = OnboardingAvatarResourcePath(index);
                Sprite expected = Resources.Load<Sprite>(resourcePath);
                Assert.That(expected, Is.Not.Null, resourcePath);
                Assert.That(avatar.sprite, Is.SameAs(expected),
                    "Solo must resolve every selectable avatar through the canonical catalog.");
                if (first == null) first = expected;
                else if (second == null && expected != first) second = expected;
            }
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(reloadedSelections, Is.EqualTo(2),
                "Two distinct committed avatars must survive a real scene reload.");

            Sprite fallback = Resources.Load<Sprite>(
                "solo/production/solo_player_avatar_v1");
            Assert.That(fallback, Is.Not.Null);

            PlayerPrefs.DeleteKey(profileVersionKey);
            PlayerPrefs.DeleteKey(avatarKey);
            render.Invoke(layout, null);
            Assert.That(avatar.sprite, Is.SameAs(fallback),
                "Missing profile data must use the approved Solo fallback.");

            PlayerPrefs.SetInt(profileVersionKey, currentProfileVersion);
            PlayerPrefs.SetInt(avatarKey, int.MaxValue);
            render.Invoke(layout, null);
            Assert.That(avatar.sprite, Is.SameAs(fallback),
                "Invalid profile data must use the approved Solo fallback.");

            const int lockedAvatar = 11;
            Assert.That(avatarCount, Is.GreaterThan(lockedAvatar));
            Assert.That(IsValidOnboardingAvatar(lockedAvatar), Is.False,
                "Canonical avatar 11 must remain locked and unavailable.");
            Assert.That(TryCommitOnboardingAvatar(lockedAvatar), Is.False,
                "Canonical Onboarding must reject locked avatar 11.");
            PlayerPrefs.SetInt(profileVersionKey, currentProfileVersion);
            PlayerPrefs.SetInt(avatarKey, lockedAvatar);
            render.Invoke(layout, null);
            Assert.That(avatar.sprite, Is.SameAs(fallback),
                "Locked avatar 11 must use the approved Solo fallback.");

            TMP_Text difficulty = Find(root, "OpponentDifficulty")
                .GetComponent<TMP_Text>();
            string[] difficultyKeys = { "easy", "normal", "hard", "adaptive" };
            for (int value = 0; value < difficultyKeys.Length; value++)
            {
                PlayerPrefs.SetInt("AIDifficulty", value);
                render.Invoke(layout, null);
                Assert.That(difficulty.text,
                    Is.EqualTo("AI " + Localized(difficultyKeys[value])
                        .ToUpperInvariant()));
                Assert.That(difficulty.text.Any(char.IsDigit), Is.False,
                    "The opponent card must not fabricate a score or rating.");
                AssertRenderedTextWithinRect(
                    difficulty, "difficulty " + difficultyKeys[value]);
            }
        }
        finally
        {
            for (int i = 0; i < integerKeys.Length; i++)
            {
                RestoreInt(
                    integerKeys[i],
                    integerExisted[i],
                    integerValues[i]);
            }
            RestoreString(
                playerNameKey,
                playerNameExisted,
                playerNameValue);
            PlayerPrefs.Save();
        }
    }

    static IEnumerator EnterSoloThroughProductionPath(
        Action<Component> captureReadyLayout)
    {
#if UNITY_EDITOR
        FirstLaunchSoloEndToEndPlayModeTests
            .FocusGameViewForEndOfFrameSettlement();
#endif
        Component homeOwner = null;
        Button soloEntry = null;
        for (int frame = 0; frame < 600; frame++)
        {
            homeOwner = FindInScene(RuntimeType("MainMenuHomeVisuals"));
            if (homeOwner != null && homeOwner.gameObject.activeInHierarchy &&
                GetProperty<bool>(homeOwner, "IsReady") &&
                GetProperty<bool>(homeOwner, "IsSettled"))
            {
                Canvas homeCanvas = homeOwner.GetComponent<Canvas>();
                Transform entryTransform = homeCanvas == null
                    ? null
                    : Find(homeCanvas.transform, "ButtonPlay");
                soloEntry = entryTransform == null
                    ? null
                    : entryTransform.GetComponent<Button>();
                if (soloEntry != null &&
                    soloEntry.gameObject.activeInHierarchy &&
                    soloEntry.interactable)
                    break;
            }
#if UNITY_EDITOR
            FirstLaunchSoloEndToEndPlayModeTests
                .FocusGameViewForEndOfFrameSettlement();
#endif
            yield return null;
        }

        Assert.That(homeOwner, Is.Not.Null,
            "The production Home owner did not become ready.");
        Assert.That(soloEntry, Is.Not.Null,
            "The production PLAY SOLO entry is missing.");
        Assert.That(soloEntry.interactable, Is.True);
        string[] persistentMethods = Enumerable.Range(
                0, soloEntry.onClick.GetPersistentEventCount())
            .Select(soloEntry.onClick.GetPersistentMethodName)
            .ToArray();
        Assert.That(persistentMethods, Does.Contain("OnPlayPressed"),
            "The test must enter Solo through the production callback.");

        soloEntry.onClick.Invoke();

        Component layout = null;
        Component matchmaking = null;
        GameObject panelGame = null;
        Transform visualRoot = null;
        for (int frame = 0; frame < 600; frame++)
        {
            layout = FindInScene(RuntimeType("SoloDuelVisuals"));
            matchmaking = FindInScene(RuntimeType("FakeMatchmaking"));
            panelGame = matchmaking == null
                ? null
                : GetField<GameObject>(matchmaking, "panelGame");
            visualRoot = layout == null
                ? null
                : Find(layout.transform, "SoloDuelVisualRoot");
            bool ownerReady = layout != null &&
                layout.gameObject.activeInHierarchy &&
                GetProperty<bool>(layout, "IsReady") &&
                GetProperty<GameObject>(layout, "KeypadRoot") != null &&
                GetProperty<Button>(layout, "SubmitControl") != null;
            bool preparing = matchmaking != null &&
                GetProperty<bool>(matchmaking, "IsPreparing");
            if (panelGame != null && panelGame.activeInHierarchy &&
                ownerReady && visualRoot != null && !preparing)
                break;
            yield return null;
        }

        Assert.That(panelGame, Is.Not.Null,
            "The production Solo panel is missing.");
        Assert.That(panelGame.activeInHierarchy, Is.True,
            "PLAY SOLO did not activate the production Solo panel.");
        Assert.That(layout, Is.Not.Null,
            "The sole Solo presentation owner is missing.");
        Assert.That(layout.gameObject.activeInHierarchy, Is.True);
        Assert.That(GetProperty<bool>(layout, "IsReady"), Is.True,
            "Unity did not invoke Start and complete the Solo visual build.");
        Assert.That(visualRoot, Is.Not.Null,
            "The production Solo entry did not construct SoloDuelVisualRoot.");
        Assert.That(GetProperty<GameObject>(layout, "KeypadRoot"), Is.Not.Null);
        Assert.That(GetProperty<Button>(layout, "SubmitControl"), Is.Not.Null);
        Assert.That(matchmaking, Is.Not.Null);
        Assert.That(GetProperty<bool>(matchmaking, "IsPreparing"), Is.False,
            "Solo preparation did not finish through the real entry path.");
        captureReadyLayout(layout);
    }

    static void PresentPhase(
        Component layout,
        string phaseName,
        string promptName,
        int roundNumber = 1)
    {
        Type phaseType = RuntimeType("SoloBoardPhase");
        Type promptType = RuntimeType("SoloBoardPrompt");
        MethodInfo method = layout.GetType().GetMethod(
            "PresentPhase", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(method, Is.Not.Null);
        method.Invoke(layout, new[]
        {
            Enum.Parse(phaseType, phaseName),
            Enum.Parse(promptType, promptName),
            (object)roundNumber,
            1,
            100,
            0,
        });
    }

    static void RecordGuessResult(
        Component layout,
        int value,
        string outcomeName)
    {
        Type outcomeType = RuntimeType("SoloGuessOutcome");
        MethodInfo method = layout.GetType().GetMethod(
            "RecordPlayerGuessResult",
            BindingFlags.Instance | BindingFlags.Public);
        Assert.That(method, Is.Not.Null, "RecordPlayerGuessResult");
        method.Invoke(layout, new[]
        {
            (object)value,
            Enum.Parse(outcomeType, outcomeName),
        });
    }

    IEnumerator ValidateSoloStateMatrix(
        Canvas canvas,
        Component safeAreaOwner,
        Component layout,
        Component numberManager,
        int width,
        int height,
        string locale,
        string opponent)
    {
        object player = RuntimeEnum("SoloBoardActor", "Player");
        object ai = RuntimeEnum("SoloBoardActor", "Opponent");
        object higher = RuntimeEnum("DuelRules+Hint", "Higher");
        object lower = RuntimeEnum("DuelRules+Hint", "Lower");
        object correct = RuntimeEnum("DuelRules+Hint", "Correct");

        for (int difficulty = 0; difficulty < 4; difficulty++)
        {
            PlayerPrefs.SetInt("AIDifficulty", difficulty);
            InvokeLayout(layout, "BeginNewMatch", opponent);
            yield return ValidateSoloViewport(
                canvas, safeAreaOwner, layout, numberManager,
                width, height, locale + " Difficulty" + difficulty);
        }
        PlayerPrefs.SetInt("AIDifficulty", 3);
        InvokeLayout(layout, "BeginNewMatch", opponent);
        TMP_InputField liveInput = GetField<TMP_InputField>(
            numberManager, "numberInput");
        TMP_Text livePlaceholder = liveInput.placeholder as TMP_Text;
        Assert.That(livePlaceholder, Is.Not.Null);
        Assert.That(livePlaceholder.text,
            Is.EqualTo(Localized("solo_secret_domain")));
        Assert.That(
            Find(canvas.transform, "CurrentNumberHeading")
                .GetComponent<TMP_Text>().text,
            Is.EqualTo(Localized("solo_secret_action_heading")));
        Assert.That(
            Find(canvas.transform, "CurrentRangeLabel")
                .GetComponent<TMP_Text>().text,
            Is.EqualTo(Localized("solo_legal_domain")));
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " ChooseSecret");

        InvokeLayout(layout, "SetPlayerSecret", 73);
        InvokeLayout(layout, "RevealStarter",
            player, 1, 1, 100, 1, 100);
        InvokeLayout(layout, "UpdateLockState",
            true, false, false, false, 100);
        Assert.That(
            Find(canvas.transform, "CurrentNumberHeading")
                .gameObject.activeInHierarchy,
            Is.False,
            "The central ribbon, not the keypad card, owns starter feedback.");
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " PlayerStarts");

        InvokeLayout(layout, "BeginPlayerTurn",
            1, 1, 100, 1, 100, false);
        InvokeLayout(layout, "UpdateLockState",
            true, true, false, false, 100);
        Assert.That(
            Find(canvas.transform, "PlayerRangeLabel")
                .GetComponent<TMP_Text>().text,
            Is.EqualTo(Localized(
                "solo_range_player", 1, 100)));
        Assert.That(
            Find(canvas.transform, "OpponentRangeLabel")
                .GetComponent<TMP_Text>().text,
            Is.EqualTo(Localized(
                "solo_range_ai", 1, 100)));
        Assert.That(
            Find(canvas.transform, "CurrentNumberHeading")
                .GetComponent<TMP_Text>().text,
            Is.EqualTo(Localized(
                "solo_guess_target_heading", opponent)));
        Assert.That(
            Find(canvas.transform, "CurrentRangeLabel")
                .GetComponent<TMP_Text>().text,
            Is.EqualTo(Localized(
                "solo_strategic_legal_range", 1, 100)));
        Assert.That(livePlaceholder.text,
            Is.EqualTo(Localized("solo_input_range", 1, 100)));
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " PlayerGuess");
        InvokeLayout(layout, "UpdateLockState",
            true, true, true, false, 3);
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " LockArmed");
        InvokeLayout(layout, "UpdateLockState",
            true, true, false, false, 100);
        InvokeLayout(layout, "SetLeaveConfirmationVisible", true);
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " LeaveConfirmation");
        InvokeLayout(layout, "SetLeaveConfirmationVisible", false);

        // Boundary and mirrored-direction coverage proves the dynamic labels
        // hold the widest values without relying on the later happy-path data.
        InvokeLayout(layout, "RecordPlayerMove",
            1, 100, lower, false, 100,
            1, 99, 1, 100);
        InvokeLayout(layout, "UpdateLockState",
            true, false, false, false, 99);
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " PlayerLowerBoundary100");
        InvokeLayout(layout, "BeginOpponentThinking",
            1, 1, 99, 1, 100);
        InvokeLayout(layout, "RecordOpponentMove",
            1, 1, higher, false, 100,
            1, 99, 2, 100);
        InvokeLayout(layout, "RevealOpponentOutcome");
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " OpponentHigherBoundary1");

        InvokeLayout(layout, "BeginNewMatch", opponent);
        InvokeLayout(layout, "SetPlayerSecret", 73);
        InvokeLayout(layout, "RevealStarter",
            player, 1, 1, 100, 1, 100);
        InvokeLayout(layout, "BeginPlayerTurn",
            1, 1, 100, 1, 100, false);
        InvokeLayout(layout, "UpdateLockState",
            true, true, false, false, 100);

        InvokeLayout(layout, "RecordPlayerMove",
            1, 40, higher, false, 100,
            41, 100, 1, 100);
        InvokeLayout(layout, "UpdateLockState",
            true, false, false, false, 60);
        TMP_Text playerOutcome = Find(canvas.transform, "CentralOutcome")
            .GetComponent<TMP_Text>();
        Assert.That(playerOutcome.text,
            Does.Contain(Localized("solo_target_number_higher", opponent)));
        TMP_Text playerHandoff = GetField<TMP_Text>(layout, "phaseText");
        Assert.That(playerHandoff.text, Does.Contain("41"));
        Assert.That(playerHandoff.text, Does.Contain("100"));
        Assert.That(playerHandoff.text,
            Does.Contain(Localized("solo_opponent_turn_short", opponent)));
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " PlayerHigher");

        InvokeLayout(layout, "BeginOpponentThinking",
            1, 41, 100, 1, 100);
        TMP_Text thinkingRibbon = Find(canvas.transform, "CentralGuess")
            .GetComponent<TMP_Text>();
        TMP_Text thinkingBubble = GetField<TMP_Text>(layout, "opponentSpeechText");
        Assert.That(thinkingRibbon.text,
            Is.EqualTo(Localized("opponent_thinking", opponent)));
        Assert.That(thinkingBubble.text,
            Is.EqualTo(Localized("solo_ai_thinking_flavor")),
            "The speech bubble may carry flavour, not the authoritative live state.");
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " OpponentThinking");
        InvokeLayout(layout, "RecordOpponentMove",
            1, 60, lower, false, 100,
            41, 100, 1, 59);
        TMP_Text revealBubble = GetField<TMP_Text>(layout, "opponentGuessText");
        Assert.That(revealBubble.text,
            Is.EqualTo(Localized("solo_ai_bubble_guess", 60)));
        Assert.That(
            GetField<TMP_Text>(layout, "phaseText").text,
            Is.EqualTo(Localized("solo_ai_result_pending")),
            "The AI result must not appear during the guess-reveal beat.");
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " OpponentGuess");
        InvokeLayout(layout, "RevealOpponentOutcome");
        Assert.That(
            Find(canvas.transform, "SoloOpponentBubble")
                .gameObject.activeInHierarchy,
            Is.False,
            "Essential AI outcome and handoff copy belongs only to the ribbon.");
        TMP_Text aiOutcome = Find(canvas.transform, "CentralOutcome")
            .GetComponent<TMP_Text>();
        Assert.That(aiOutcome.text,
            Does.Contain(Localized("your_number_is_lower")));
        TMP_Text aiHandoff = GetField<TMP_Text>(layout, "phaseText");
        Assert.That(aiHandoff.text, Does.Contain("1"));
        Assert.That(aiHandoff.text, Does.Contain("59"));
        Assert.That(aiHandoff.text,
            Does.Contain(Localized("solo_your_turn_short")));
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " OpponentLower");

        InvokeLayout(layout, "BeginPlayerTurn",
            2, 41, 100, 1, 59, false);
        // Mirror GameManager.RefreshLockButton at the real turn handoff: the
        // rule-owned Lock becomes actionable again only after PlayerGuess is
        // active, while the factual AI summary remains pinned.
        InvokeLayout(layout, "UpdateLockState",
            true, true, false, false, 60);
        Assert.That(StateProperty(layout, "LatestAiHandoffPinned"),
            Is.EqualTo("True"));
        TMP_Text pinnedPrompt = GetField<TMP_Text>(layout, "phaseText");
        TMP_Text pinnedGuess = Find(canvas.transform, "CentralGuess")
            .GetComponent<TMP_Text>();
        TMP_Text pinnedOutcome = Find(canvas.transform, "CentralOutcome")
            .GetComponent<TMP_Text>();
        Assert.That(pinnedGuess.text,
            Does.Contain(Localized("solo_opponent_guessed", opponent, 60)));
        Assert.That(pinnedOutcome.text,
            Does.Contain(Localized("your_number_is_lower")));
        Assert.That(pinnedPrompt.text,
            Does.Contain(Localized("solo_your_turn_short")));
        Assert.That(
            Find(canvas.transform, "SoloOpponentBubble")
                .gameObject.activeInHierarchy,
            Is.False,
            "The retained AI result must not compete with the speech bubble.");
        Assert.That(livePlaceholder.text,
            Is.EqualTo(Localized("solo_input_range", 41, 100)));
        Assert.That(
            Find(canvas.transform, "CurrentNumberHeading")
                .GetComponent<TMP_Text>().text,
            Is.EqualTo(Localized(
                "solo_guess_target_heading", opponent)));
        Assert.That(
            Find(canvas.transform, "CurrentRangeLabel")
                .GetComponent<TMP_Text>().text,
            Is.EqualTo(Localized(
                "solo_strategic_legal_range", 41, 100)));
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " StickyAiHandoff");
        InvokeLayout(layout, "DismissLatestAiHandoff");
        InvokeLayout(layout, "RecordPlayerMove",
            2, 77, correct, false, 60,
            41, 100, 1, 59);
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " PlayerCorrect");
        InvokeLayout(layout, "BeginOpponentThinking",
            2, 41, 100, 1, 59);
        Assert.That(
            StateProperty(layout, "Prompt"),
            Is.EqualTo("MatchPointYours"));
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " AnsweringGuessNotice");

        InvokeLayout(layout, "BeginNewMatch", opponent);
        InvokeLayout(layout, "SetPlayerSecret", 73);
        InvokeLayout(layout, "RevealStarter",
            ai, 1, 1, 100, 1, 100);
        InvokeLayout(layout, "UpdateLockState",
            true, false, false, false, 100);
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " OpponentStarts");
        InvokeLayout(layout, "BeginOpponentThinking",
            1, 1, 100, 1, 100);
        InvokeLayout(layout, "RecordOpponentMove",
            1, 73, correct, false, 100,
            1, 100, 1, 100);
        InvokeLayout(layout, "RevealOpponentOutcome");
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " OpponentCorrect");
        InvokeLayout(layout, "ShowLastLicks", 1);
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " LastLicks");
        InvokeLayout(layout, "BeginPlayerTurn",
            1, 1, 100, 1, 100, true);
        Assert.That(StateProperty(layout, "LatestAiHandoffPinned"),
            Is.EqualTo("True"));
        TMP_Text lastLicksGuess = Find(canvas.transform, "CentralGuess")
            .GetComponent<TMP_Text>();
        TMP_Text lastLicksOutcome = Find(canvas.transform, "CentralOutcome")
            .GetComponent<TMP_Text>();
        Assert.That(lastLicksGuess.text,
            Does.Contain(Localized("solo_opponent_guessed", opponent, 73)));
        Assert.That(lastLicksOutcome.text,
            Does.Contain(Localized("your_number_is_correct")));
        InvokeLayout(layout, "UpdateLockState",
            true, true, false, false, 100);
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " LastLicksGuess");
        InvokeLayout(layout, "RecordPlayerMove",
            1, 40, lower, false, 100,
            1, 39, 1, 100);
        InvokeLayout(layout, "CompleteMatch",
            RuntimeEnum("DuelRules+Outcome", "GuestWins"),
            73, 77, 1, 1);
        Assert.That(
            Find(canvas.transform, "ResultReason").GetComponent<TMP_Text>().text,
            Is.EqualTo(Localized(
                "solo_result_only_correct", 1, opponent, 73)));
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " ResultLoss");

        InvokeLayout(layout, "BeginNewMatch", opponent);
        InvokeLayout(layout, "SetPlayerSecret", 73);
        InvokeLayout(layout, "RevealStarter",
            player, 1, 1, 100, 1, 100);
        InvokeLayout(layout, "BeginPlayerTurn",
            1, 1, 100, 1, 100, false);
        InvokeLayout(layout, "RecordPlayerMove",
            1, 40, higher, true, 100,
            41, 100, 1, 100);
        InvokeLayout(layout, "UpdateLockState",
            true, false, false, true, 60);
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " PlayerLockMiss");
        InvokeLayout(layout, "ShowLockForfeit", player, 1);
        Assert.That(StateProperty(layout, "ActiveActor"),
            Is.EqualTo("Opponent"));
        Assert.That(
            Find(canvas.transform, "CurrentNumberHeading")
                .gameObject.activeInHierarchy,
            Is.False,
            "The central ribbon, not the keypad card, owns Lock penalties.");
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " PlayerLockForfeit");

        InvokeLayout(layout, "BeginNewMatch", opponent);
        InvokeLayout(layout, "SetPlayerSecret", 73);
        InvokeLayout(layout, "RevealStarter",
            ai, 1, 1, 100, 1, 100);
        InvokeLayout(layout, "BeginOpponentThinking",
            1, 1, 100, 1, 100);
        InvokeLayout(layout, "RecordOpponentMove",
            1, 40, higher, true, 100,
            1, 100, 41, 100);
        InvokeLayout(layout, "RevealOpponentOutcome");
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " OpponentLockMiss");
        InvokeLayout(layout, "ShowLockForfeit", ai, 1);
        Assert.That(StateProperty(layout, "ActiveActor"),
            Is.EqualTo("Player"));
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " OpponentLockForfeit");

        InvokeLayout(layout, "BeginNewMatch", opponent);
        InvokeLayout(layout, "SetPlayerSecret", 73);
        InvokeLayout(layout, "RevealStarter",
            player, 1, 1, 100, 1, 100);
        InvokeLayout(layout, "BeginPlayerTurn",
            1, 1, 100, 1, 100, false);
        InvokeLayout(layout, "RecordPlayerMove",
            1, 77, correct, false, 100,
            1, 100, 1, 100);
        InvokeLayout(layout, "BeginOpponentThinking",
            1, 1, 100, 1, 100);
        InvokeLayout(layout, "RecordOpponentMove",
            1, 73, correct, false, 100,
            1, 100, 1, 100);
        InvokeLayout(layout, "RevealOpponentOutcome");
        InvokeLayout(layout, "CompleteMatch",
            RuntimeEnum("DuelRules+Outcome", "Draw"),
            73, 77, 1, 1);
        Assert.That(
            Find(canvas.transform, "ResultReason").GetComponent<TMP_Text>().text,
            Is.EqualTo(Localized("solo_result_exact_draw", 100)));
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " ResultDraw");

        InvokeLayout(layout, "BeginNewMatch", opponent);
        InvokeLayout(layout, "SetPlayerSecret", 73);
        InvokeLayout(layout, "RevealStarter",
            player, 1, 1, 100, 1, 100);
        InvokeLayout(layout, "BeginPlayerTurn",
            1, 1, 100, 1, 100, false);
        InvokeLayout(layout, "RecordPlayerMove",
            1, 77, correct, true, 100,
            1, 100, 1, 100);
        InvokeLayout(layout, "UpdateLockState",
            true, false, false, true, 1);
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " PlayerLockHit");
        InvokeLayout(layout, "BeginOpponentThinking",
            1, 1, 100, 1, 100);
        InvokeLayout(layout, "RecordOpponentMove",
            1, 73, correct, false, 100,
            1, 100, 1, 100);
        InvokeLayout(layout, "RevealOpponentOutcome");
        InvokeLayout(layout, "CompleteMatch",
            RuntimeEnum("DuelRules+Outcome", "HostWins"),
            73, 77, 1, 1);
        Assert.That(
            Find(canvas.transform, "ResultReason").GetComponent<TMP_Text>().text,
            Is.EqualTo(Localized(
                "solo_result_lock_tiebreak",
                Localized("solo_you_header"), 77)));
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " ResultLockWin");

        InvokeLayout(layout, "BeginNewMatch", opponent);
        InvokeLayout(layout, "SetPlayerSecret", 73);
        InvokeLayout(layout, "RevealStarter",
            player, 1, 1, 100, 1, 100);
        InvokeLayout(layout, "BeginPlayerTurn",
            1, 1, 100, 1, 100, false);
        InvokeLayout(layout, "RecordPlayerMove",
            1, 77, correct, false, 7,
            1, 100, 1, 100);
        InvokeLayout(layout, "BeginOpponentThinking",
            1, 1, 100, 1, 100);
        InvokeLayout(layout, "RecordOpponentMove",
            1, 73, correct, false, 12,
            1, 100, 1, 100);
        InvokeLayout(layout, "RevealOpponentOutcome");
        InvokeLayout(layout, "CompleteMatch",
            RuntimeEnum("DuelRules+Outcome", "HostWins"),
            73, 77, 1, 1);
        Assert.That(
            Find(canvas.transform, "ResultReason").GetComponent<TMP_Text>().text,
            Is.EqualTo(Localized(
                "solo_result_range_tiebreak",
                Localized("solo_you_header"), 77, 7, 12)));
        yield return ValidateSoloViewport(
            canvas, safeAreaOwner, layout, numberManager,
            width, height, locale + " ResultRangeWin");
    }

    static object InvokeLayout(
        Component layout,
        string methodName,
        params object[] arguments)
    {
        MethodInfo method = layout.GetType().GetMethod(
            methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(method, Is.Not.Null, methodName);
        object result = method.Invoke(layout, arguments);
        if (method.ReturnType == typeof(bool))
            Assert.That(result, Is.EqualTo(true), methodName);
        return result;
    }

    static object RuntimeEnum(string typeName, string value)
    {
        return Enum.Parse(RuntimeType(typeName), value);
    }

    static string StateProperty(Component layout, string name)
    {
        object state = GetProperty<object>(layout, "CurrentState");
        PropertyInfo property = state.GetType().GetProperty(
            name, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(property, Is.Not.Null, name);
        object value = property.GetValue(state);
        return value != null ? value.ToString() : string.Empty;
    }

    IEnumerator ValidateSoloViewport(
        Canvas canvas,
        Component safeAreaOwner,
        Component layout,
        Component numberManager,
        int width,
        int height,
        string locale)
    {
        yield return null;
        yield return null;
        Behaviour responsiveBehaviour = safeAreaOwner as Behaviour;
        bool responsiveWasEnabled = responsiveBehaviour != null &&
                                    responsiveBehaviour.enabled;
        Vector2 viewportSize = new Vector2(width, height);
        Vector2 canvasSize = CanvasSize(viewportSize);
        try
        {
            if (responsiveBehaviour != null)
                responsiveBehaviour.enabled = false;
            MethodInfo applyViewport = safeAreaOwner.GetType().GetMethod(
                "ApplyViewport", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(applyViewport, Is.Not.Null);
            applyViewport.Invoke(safeAreaOwner, new object[]
            {
                new Rect(Vector2.zero, viewportSize),
                new Rect(Vector2.zero, viewportSize),
                canvasSize,
            });
            MethodInfo applyResponsive = layout.GetType().GetMethod(
                "ApplyResponsiveLayoutForViewport",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(applyResponsive, Is.Not.Null);
            applyResponsive.Invoke(layout, new object[]
            {
                canvasSize.x,
                canvasSize.y,
            });
            Canvas.ForceUpdateCanvases();

            Transform visualRoot = Find(canvas.transform, "SoloDuelVisualRoot");
            Transform safeRoot = Find(visualRoot, "SoloDuelSafeRoot");
            Assert.That(visualRoot, Is.Not.Null);
            Assert.That(safeRoot, Is.Not.Null);
            Rect safeRect = GetProperty<Rect>(safeAreaOwner, "LastSafeRect");
            Rect canvasRect = new Rect(canvasSize * -0.5f, canvasSize);
            AssertContained(
                canvasRect, safeRect,
                $"{locale} {width}x{height} safe area");

            foreach (string targetName in new[]
            {
                "DuelBack",
                "SoloDuelLogo",
                "SoloDuelPlayerChip",
                "PlayerCard",
                "OpponentCard",
                "SoloPromptRibbon",
                "NumberKeypad",
                "ButtonConfirm",
                "LockButton",
                "SoloContinueButton",
                "HistoryCard",
                "HistoryViewport",
                "SoloTipCard",
                "CurrentRangeLabel",
                "PlayerRangeLabel",
                "OpponentRangeLabel",
                "LockExplanation",
            })
            {
                RectTransform target = Find(visualRoot, targetName)
                    as RectTransform;
                Assert.That(target, Is.Not.Null, targetName);
                AssertContained(
                    safeRect,
                    BoundsInSafeRect(
                        safeRoot as RectTransform, target, safeRect),
                    $"{locale} {width}x{height} {targetName}");
            }

            RectTransform keypad = Find(visualRoot, "NumberKeypad")
                as RectTransform;
            RectTransform submit = Find(visualRoot, "ButtonConfirm")
                as RectTransform;
            RectTransform lockButton = Find(visualRoot, "LockButton")
                as RectTransform;
            RectTransform continueButton = Find(
                visualRoot, "SoloContinueButton") as RectTransform;
            RectTransform history = Find(visualRoot, "HistoryCard")
                as RectTransform;
            RectTransform tip = Find(visualRoot, "SoloTipCard")
                as RectTransform;
            TMP_Text validation = GetField<TMP_Text>(
                numberManager, "messageText");
            string geometryContext = $"{locale} {width}x{height}";
            ForceFinalTextLayout(visualRoot);
            Assert.That(continueButton.gameObject.activeInHierarchy, Is.False,
                geometryContext + " routine phases need no permission button");
            AssertVerticalGap(
                BoundsInSafeRect(safeRoot as RectTransform,
                    validation.rectTransform, safeRect),
                BoundsInSafeRect(
                    safeRoot as RectTransform, keypad, safeRect),
                1f, geometryContext + " validation/keypad");
            if (submit.gameObject.activeInHierarchy &&
                lockButton.gameObject.activeInHierarchy)
            {
                AssertHorizontalGap(
                    BoundsInSafeRect(
                        safeRoot as RectTransform, submit, safeRect),
                    BoundsInSafeRect(
                        safeRoot as RectTransform, lockButton, safeRect),
                    1f, geometryContext + " Submit/Lock");
            }
            if (continueButton.gameObject.activeInHierarchy &&
                lockButton.gameObject.activeInHierarchy)
            {
                AssertHorizontalGap(
                    BoundsInSafeRect(
                        safeRoot as RectTransform,
                        continueButton, safeRect),
                    BoundsInSafeRect(
                        safeRoot as RectTransform,
                        lockButton, safeRect),
                    1f, geometryContext + " Continue/Lock");
            }
            AssertVerticalGap(
                BoundsInSafeRect(
                    safeRoot as RectTransform, history, safeRect),
                BoundsInSafeRect(
                    safeRoot as RectTransform, tip, safeRect),
                1f, geometryContext + " history/strategy");

            foreach (TMP_Text text in
                     visualRoot.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!IsRenderedText(text))
                    continue;
                CollectRenderedTextWithinRect(
                    activeGlyphViolations,
                    text, $"{locale} {width}x{height} {text.name}");
                CollectMissingGlyphs(
                    activeGlyphViolations,
                    text, $"{locale} {width}x{height} {text.name}");
                CollectMinimumReadableFont(
                    activeGlyphViolations,
                    text, $"{locale} {width}x{height} {text.name}");
                if (text.name == "OpponentDifficulty" ||
                    text.name == "HistoryOutcome")
                {
                    CollectRenderedTextHorizontalSafety(
                        activeGlyphViolations,
                        text, 1f,
                        $"{locale} {width}x{height} {text.name}");
                }
            }

            CollectOwnerAwareTypography(
                activeGlyphViolations,
                visualRoot,
                layout,
                numberManager,
                geometryContext);
            CollectConcurrentTextOverlaps(
                activeGlyphViolations,
                visualRoot,
                safeRoot as RectTransform,
                geometryContext);

            string phase = StateProperty(layout, "Phase");
            Button lockControl = lockButton.GetComponent<Button>();
            bool live = phase != "ChooseSecret" && phase != "MatchResult";
            Assert.That(lockButton.gameObject.activeInHierarchy,
                Is.EqualTo(live), geometryContext + " Lock visibility");
            Assert.That(lockControl.interactable,
                Is.EqualTo(phase == "PlayerGuess"),
                geometryContext + " Lock interaction");
            bool inputGuidance = phase == "ChooseSecret" ||
                                 phase == "PlayerGuess";
            Assert.That(
                Find(visualRoot, "CurrentNumberHeading")
                    .gameObject.activeInHierarchy,
                Is.EqualTo(inputGuidance),
                geometryContext + " keypad guidance ownership");
        }
        finally
        {
            if (responsiveBehaviour != null)
                responsiveBehaviour.enabled = responsiveWasEnabled;
        }
    }

    static void ForceFinalTextLayout(Transform visualRoot)
    {
        Canvas.ForceUpdateCanvases();
        foreach (TMP_Text text in
                 visualRoot.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.gameObject.activeInHierarchy)
                text.ForceMeshUpdate(true, true);
        }
        Canvas.ForceUpdateCanvases();
    }

    static void CollectOwnerAwareTypography(
        ICollection<string> violations,
        Transform visualRoot,
        Component layout,
        Component numberManager,
        string context)
    {
        RectTransform ribbon = RectNamed(visualRoot, "SoloPromptRibbon");
        RectTransform interaction = RectNamed(visualRoot, "SoloInteractionCard");
        RectTransform playerCard = RectNamed(visualRoot, "PlayerCard");
        RectTransform opponentCard = RectNamed(visualRoot, "OpponentCard");
        RectTransform bubble = RectNamed(visualRoot, "SoloOpponentBubble");
        RectTransform bubbleTextSafe = RectNamed(
            visualRoot, "OpponentBubbleTextSafeArea");
        RectTransform historyCard = RectNamed(visualRoot, "HistoryCard");
        RectTransform tip = RectNamed(visualRoot, "SoloTipCard");
        RectTransform chip = RectNamed(visualRoot, "SoloDuelPlayerChip");
        RectTransform result = RectNamed(visualRoot, "SoloResultDetail");

        TMP_Text round = TextNamed(visualRoot, "RoundLabel");
        TMP_Text action = TextNamed(visualRoot, "CentralGuess");
        TMP_Text outcome = TextNamed(visualRoot, "CentralOutcome");
        TMP_Text handoff = GetField<TMP_Text>(layout, "phaseText");
        CollectInsideMany(
            violations, ribbon, 8f, context + " ribbon",
            round, action, outcome, handoff);
        CollectVerticalSequence(
            violations, ribbon, 6f, context + " ribbon lanes",
            round, action, outcome, handoff);
        CollectMinimumReadableFont(
            violations, handoff, 18f, context + " ribbon handoff");

        TMP_Text heading = TextNamed(visualRoot, "CurrentNumberHeading");
        TMP_Text range = TextNamed(visualRoot, "CurrentRangeLabel");
        TMP_InputField input = GetField<TMP_InputField>(
            numberManager, "numberInput");
        TMP_Text inputValue = input != null ? input.textComponent : null;
        TMP_Text inputPlaceholder = input != null
            ? input.placeholder as TMP_Text
            : null;
        TMP_Text validation = GetField<TMP_Text>(numberManager, "messageText");
        CollectInsideMany(
            violations, interaction, 8f, context + " interaction",
            heading, range, validation);
        if (input != null)
        {
            RectTransform inputOwner = input.transform as RectTransform;
            CollectInsideMany(
                violations, inputOwner, 8f, context + " input",
                inputValue, inputPlaceholder);
        }
        CollectVerticalSequence(
            violations, interaction, 6f, context + " input guidance",
            heading, range, ActiveInputText(inputValue, inputPlaceholder));

        TMP_Text playerCaption = TextNamed(visualRoot, "PlayerCaption");
        TMP_Text playerBadge = TextNamed(visualRoot, "PlayerActiveBadgeLabel");
        TMP_Text playerName = TextNamed(visualRoot, "PlayerName");
        TMP_Text playerSecret = TextNamed(visualRoot, "PlayerSecretValue");
        TMP_Text playerLatest = TextNamed(visualRoot, "PlayerLatestGuess");
        TMP_Text playerWins = TextNamed(visualRoot, "PlayerWins");
        CollectInsideMany(
            violations, playerCard, 8f, context + " player-card",
            playerCaption, playerName, playerSecret, playerLatest, playerWins);
        CollectInsideMany(
            violations,
            RectNamed(visualRoot, "PlayerActiveBadge"),
            6f,
            context + " player-badge",
            playerBadge);
        CollectVerticalSequence(
            violations, playerCard, 6f, context + " player-card lanes",
            playerCaption, playerBadge, playerName,
            playerSecret, playerLatest, playerWins);
        CollectReservedExclusion(
            violations, playerWins,
            RectNamed(visualRoot, "PlayerCardTrophy"), playerCard, 8f,
            context + " player score/trophy");

        TMP_Text opponentCaption = TextNamed(visualRoot, "OpponentCaption");
        TMP_Text opponentBadge = TextNamed(
            visualRoot, "OpponentActiveBadgeLabel");
        TMP_Text opponentName = GetField<TMP_Text>(layout, "opponentIdentityText");
        TMP_Text opponentLatest = TextNamed(
            visualRoot, "OpponentLatestGuess");
        TMP_Text difficulty = TextNamed(visualRoot, "OpponentDifficulty");
        CollectInsideMany(
            violations, opponentCard, 8f, context + " opponent-card",
            opponentCaption, opponentName, opponentLatest, difficulty);
        CollectInsideMany(
            violations,
            RectNamed(visualRoot, "OpponentActiveBadge"),
            6f,
            context + " opponent-badge",
            opponentBadge);
        CollectVerticalSequence(
            violations, opponentCard, 6f, context + " opponent-card lanes",
            opponentCaption, opponentBadge, opponentName,
            opponentLatest, difficulty);
        CollectReservedExclusion(
            violations, difficulty,
            RectNamed(visualRoot, "OpponentCardTrophy"), opponentCard, 8f,
            context + " difficulty/trophy");
        CollectMinimumReadableFont(
            violations, opponentName, 35f, context + " opponent name");

        TMP_Text bubblePrompt = TextNamed(
            visualRoot, "OpponentBubblePrompt");
        TMP_Text bubbleGuess = GetField<TMP_Text>(layout, "opponentGuessText");
        TMP_Text bubbleSpeech = GetField<TMP_Text>(layout, "opponentSpeechText");
        TMP_Text[] bubbleLabels = { bubblePrompt, bubbleGuess, bubbleSpeech };
        CollectInsideMany(
            violations, bubbleTextSafe, 8f,
            context + " speech-bubble cream-content",
            bubbleLabels);
        int visibleBubbleLabels = bubbleLabels.Count(IsRenderedText);
        if (visibleBubbleLabels > 1)
        {
            violations.Add(
                context + " speech-bubble has " + visibleBubbleLabels +
                " competing text owners; expected at most one");
        }
        foreach (TMP_Text label in bubbleLabels)
        {
            CollectReservedExclusion(
                violations, label,
                RectNamed(visualRoot, "OpponentBubbleAvatar"), bubble, 8f,
                context + " speech/avatar reserve");
            CollectReservedExclusion(
                violations, label,
                RectNamed(visualRoot, "OpponentReaction"), bubble, 8f,
                context + " speech/emoji reserve");
        }

        TMP_Text historyTitle = TextNamed(visualRoot, "HistoryTitle");
        CollectInsideMany(
            violations, historyCard, 8f, context + " history-card",
            historyTitle);
        CollectReservedExclusion(
            violations, historyTitle,
            RectNamed(visualRoot, "HistoryTitleSparkleLeft"), historyCard, 8f,
            context + " history-title/left-sparkle");
        CollectReservedExclusion(
            violations, historyTitle,
            RectNamed(visualRoot, "HistoryTitleSparkleRight"), historyCard, 8f,
            context + " history-title/right-sparkle");
        foreach (Transform child in
                 RectNamed(visualRoot, "HistoryContent"))
        {
            if (!child.gameObject.activeInHierarchy ||
                !child.name.StartsWith("HistoryRow", StringComparison.Ordinal))
                continue;
            RectTransform row = child as RectTransform;
            TMP_Text meta = TextNamed(child, "HistoryMeta");
            TMP_Text number = TextNamed(child, "HistoryNumber");
            TMP_Text rowOutcome = TextNamed(child, "HistoryOutcome");
            TMP_Text newest = TextNamed(child, "HistoryNewest");
            CollectInsideMany(
                violations, row, 8f,
                context + " " + child.name,
                meta, number, rowOutcome, newest);
            CollectHorizontalTextGap(
                violations, meta, newest, row, 6f,
                context + " " + child.name + " meta/new");
            CollectVerticalTextGap(
                violations, meta, number, row, 6f,
                context + " " + child.name + " meta/number");
            CollectVerticalTextGap(
                violations, newest, rowOutcome, row, 6f,
                context + " " + child.name + " new/outcome");
            CollectHorizontalTextGap(
                violations, number, rowOutcome, row, 6f,
                context + " " + child.name + " number/outcome");
            CollectReservedExclusion(
                violations, rowOutcome,
                RectNamed(child, "HistoryIcon"), row, 8f,
                context + " " + child.name + " outcome/icon");
        }

        TMP_Text tipHeading = TextNamed(visualRoot, "SoloTipHeading");
        TMP_Text playerRange = TextNamed(visualRoot, "PlayerRangeLabel");
        TMP_Text aiRange = TextNamed(visualRoot, "OpponentRangeLabel");
        TMP_Text lockCopy = TextNamed(visualRoot, "LockExplanation");
        CollectInsideMany(
            violations, tip, 8f, context + " tip-card",
            tipHeading, playerRange, aiRange, lockCopy);
        CollectVerticalSequence(
            violations, tip, 6f, context + " tip lanes",
            tipHeading, playerRange, aiRange, lockCopy);
        foreach (TMP_Text label in new[]
                 { tipHeading, playerRange, aiRange, lockCopy })
        {
            CollectReservedExclusion(
                violations, label,
                RectNamed(visualRoot, "SoloTipMascot"), tip, 8f,
                context + " tip/mascot reserve");
            CollectReservedExclusion(
                violations, label,
                RectNamed(visualRoot, "SoloTipBulb"), tip, 8f,
                context + " tip/bulb reserve");
        }

        TMP_Text chipScore = TextNamed(visualRoot, "SoloDuelChipText");
        CollectInsideMany(
            violations, chip, 8f, context + " profile-chip", chipScore);
        CollectReservedExclusion(
            violations, chipScore,
            RectNamed(visualRoot, "SoloDuelChipTrophy"), chip, 8f,
            context + " chip score/trophy");
        CollectReservedExclusion(
            violations, chipScore,
            RectNamed(visualRoot, "SoloDuelChipAvatar"), chip, 8f,
            context + " chip score/avatar");

        if (result.gameObject.activeInHierarchy)
        {
            TMP_Text reason = TextNamed(visualRoot, "ResultReason");
            TMP_Text secrets = TextNamed(visualRoot, "ResultSecrets");
            TMP_Text guesses = TextNamed(visualRoot, "ResultLatestGuesses");
            TMP_Text turns = TextNamed(visualRoot, "ResultTurns");
            CollectInsideMany(
                violations, result, 8f, context + " result",
                reason, secrets, guesses, turns);
            CollectVerticalSequence(
                violations, result, 6f, context + " result lanes",
                reason, secrets, guesses, turns);
        }

        foreach (Button button in
                 visualRoot.GetComponentsInChildren<Button>(true))
        {
            if (!button.gameObject.activeInHierarchy)
                continue;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null || !label.gameObject.activeInHierarchy ||
                string.IsNullOrWhiteSpace(label.text))
                continue;
            CollectRenderedTextInsidePanel(
                violations, label, button.transform as RectTransform,
                new Vector4(8f, 8f, 8f, 8f),
                context + " " + button.name + " label");
        }
    }

    static RectTransform RectNamed(Transform root, string name)
    {
        Transform found = Find(root, name);
        Assert.That(found, Is.Not.Null, "Missing layout owner " + name);
        RectTransform rect = found as RectTransform;
        Assert.That(rect, Is.Not.Null, name + " must be a RectTransform");
        return rect;
    }

    static TMP_Text TextNamed(Transform root, string name)
    {
        Transform found = Find(root, name);
        Assert.That(found, Is.Not.Null, "Missing text owner " + name);
        TMP_Text text = found.GetComponent<TMP_Text>();
        Assert.That(text, Is.Not.Null, name + " must own TMP text");
        return text;
    }

    static TMP_Text ActiveInputText(TMP_Text value, TMP_Text placeholder)
    {
        return IsRenderedText(value) ? value : placeholder;
    }

    static bool IsRenderedText(TMP_Text text)
    {
        return text != null && text.enabled &&
               text.gameObject.activeInHierarchy &&
               !text.canvasRenderer.cull &&
               text.canvasRenderer.GetAlpha() > 0.001f &&
               HasRenderableText(text.text);
    }

    static bool HasRenderableText(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;
        return value.Any(character =>
            !char.IsWhiteSpace(character) &&
            char.GetUnicodeCategory(character) != UnicodeCategory.Format &&
            char.GetUnicodeCategory(character) != UnicodeCategory.Control);
    }

    static void CollectConcurrentTextOverlaps(
        ICollection<string> violations,
        Transform visualRoot,
        RectTransform referenceSpace,
        string context)
    {
        TMP_Text[] labels = visualRoot
            .GetComponentsInChildren<TMP_Text>(true)
            .Where(IsRenderedText)
            .ToArray();
        Transform modal = Find(visualRoot, "SoloLeaveConfirmation");
        bool modalVisible = modal != null && modal.gameObject.activeInHierarchy;
        for (int leftIndex = 0; leftIndex < labels.Length; leftIndex++)
        {
            TMP_Text left = labels[leftIndex];
            if (!TryRenderedGlyphRectsIn(
                    referenceSpace, left, out List<Rect> leftGlyphs))
                continue;
            Rect leftBounds = Union(leftGlyphs);
            for (int rightIndex = leftIndex + 1;
                 rightIndex < labels.Length;
                 rightIndex++)
            {
                TMP_Text right = labels[rightIndex];
                if (IsApprovedDecorativeTextLayer(left, right) ||
                    IsIntentionalModalLayerPair(
                        left, right, modal, modalVisible) ||
                    !TryRenderedGlyphRectsIn(
                        referenceSpace, right, out List<Rect> rightGlyphs))
                    continue;
                Rect rightBounds = Union(rightGlyphs);
                if (!leftBounds.Overlaps(rightBounds))
                    continue;
                if (!leftGlyphs.Any(leftGlyph =>
                        rightGlyphs.Any(rightGlyph =>
                            leftGlyph.Overlaps(rightGlyph))))
                    continue;
                violations.Add(
                    context + " active text overlap: " + left.name +
                    "/" + right.name + $"; left={leftBounds}, right={rightBounds}");
            }
        }
    }

    static bool IsApprovedDecorativeTextLayer(TMP_Text left, TMP_Text right)
    {
        return left.transform.parent == right.transform.parent &&
               ((left.name == "SoloVsOutline" && right.name == "SoloVsLabel") ||
                (left.name == "SoloVsLabel" && right.name == "SoloVsOutline"));
    }

    static bool IsIntentionalModalLayerPair(
        TMP_Text left,
        TMP_Text right,
        Transform modal,
        bool modalVisible)
    {
        if (!modalVisible)
            return false;
        bool leftIsModal = left.transform == modal ||
                           left.transform.IsChildOf(modal);
        bool rightIsModal = right.transform == modal ||
                            right.transform.IsChildOf(modal);
        return leftIsModal != rightIsModal;
    }

    static void CollectMissingGlyphs(
        ICollection<string> violations,
        TMP_Text text,
        string context)
    {
        if (text.font == null)
        {
            violations.Add(context + " has no TMP font asset");
            return;
        }

        var missing = new HashSet<int>();
        foreach (char character in text.text)
        {
            if (char.IsWhiteSpace(character) || char.IsControl(character) ||
                character == '\u200B' || char.IsSurrogate(character))
                continue;
            if (!text.font.HasCharacter(character, true, false))
                missing.Add(character);
        }
        if (missing.Count == 0)
            return;
        violations.Add(
            context + " missing TMP glyph(s): " +
            string.Join(", ", missing.OrderBy(value => value)
                .Select(value => $"U+{value:X4}")));
    }

    static void CollectMinimumReadableFont(
        ICollection<string> violations,
        TMP_Text text,
        string context)
    {
        float minimum;
        switch (text.name)
        {
            case "RoundLabel": minimum = 20f; break;
            case "CentralGuess": minimum = 23f; break;
            case "CentralOutcome": minimum = 21f; break;
            case "PhasePrompt": minimum = 18f; break;
            case "CurrentNumberHeading": minimum = 22f; break;
            case "CurrentRangeLabel": minimum = 19f; break;
            case "PlayerName":
            case "OpponentIdentity": minimum = 35f; break;
            case "PlayerLatestGuess":
            case "OpponentLatestGuess": minimum = 18f; break;
            case "OpponentDifficulty": minimum = 23f; break;
            case "HistoryMeta":
            case "HistoryNewest": minimum = 16f; break;
            case "HistoryOutcome": minimum = 19f; break;
            case "PlayerRangeLabel":
            case "OpponentRangeLabel": minimum = 17f; break;
            case "LockExplanation": minimum = 16f; break;
            case "ResultReason": minimum = 21f; break;
            default: return;
        }
        if (text.fontSize + 0.01f < minimum)
        {
            violations.Add(
                context + $" font expected >= {minimum:0.###} " +
                $"but was {text.fontSize:0.###}");
        }
    }

    static void CollectMinimumReadableFont(
        ICollection<string> violations,
        TMP_Text text,
        float minimum,
        string context)
    {
        if (!IsRenderedText(text))
            return;
        if (text.fontSize + 0.01f < minimum)
        {
            violations.Add(
                context + $" font expected >= {minimum:0.###} " +
                $"but was {text.fontSize:0.###}");
        }
    }

    static void CollectInsideMany(
        ICollection<string> violations,
        RectTransform owner,
        float padding,
        string context,
        params TMP_Text[] labels)
    {
        foreach (TMP_Text label in labels)
        {
            CollectRenderedTextInsidePanel(
                violations, label, owner,
                new Vector4(padding, padding, padding, padding),
                context + " " + (label != null ? label.name : "<missing>"));
        }
    }

    static void CollectVerticalSequence(
        ICollection<string> violations,
        RectTransform owner,
        float minimumGap,
        string context,
        params TMP_Text[] labels)
    {
        TMP_Text previous = null;
        foreach (TMP_Text label in labels)
        {
            if (!IsRenderedText(label))
                continue;
            if (previous != null)
            {
                CollectVerticalTextGap(
                    violations, previous, label, owner, minimumGap,
                    context + " " + previous.name + "/" + label.name);
            }
            previous = label;
        }
    }

    static void CollectVerticalTextGap(
        ICollection<string> violations,
        TMP_Text upper,
        TMP_Text lower,
        RectTransform owner,
        float minimumGap,
        string context)
    {
        if (!IsRenderedText(upper) || !IsRenderedText(lower))
            return;
        if (!TryRenderedGlyphRectIn(owner, upper, out Rect upperGlyphs, out _) ||
            !TryRenderedGlyphRectIn(owner, lower, out Rect lowerGlyphs, out _))
            return;
        float gap = upperGlyphs.yMin - lowerGlyphs.yMax;
        if (gap < minimumGap)
        {
            violations.Add(
                context + $" vertical gap expected >= {minimumGap:0.###} " +
                $"but was {gap:0.###}; upper={upperGlyphs}, lower={lowerGlyphs}");
        }
    }

    static void CollectHorizontalTextGap(
        ICollection<string> violations,
        TMP_Text left,
        TMP_Text right,
        RectTransform owner,
        float minimumGap,
        string context)
    {
        if (!IsRenderedText(left) || !IsRenderedText(right))
            return;
        if (!TryRenderedGlyphRectIn(owner, left, out Rect leftGlyphs, out _) ||
            !TryRenderedGlyphRectIn(owner, right, out Rect rightGlyphs, out _))
            return;
        float gap = rightGlyphs.xMin - leftGlyphs.xMax;
        if (gap < minimumGap)
        {
            violations.Add(
                context + $" horizontal gap expected >= {minimumGap:0.###} " +
                $"but was {gap:0.###}; left={leftGlyphs}, right={rightGlyphs}");
        }
    }

    static void CollectReservedExclusion(
        ICollection<string> violations,
        TMP_Text text,
        RectTransform reserved,
        RectTransform owner,
        float clearance,
        string context)
    {
        if (!IsRenderedText(text) || reserved == null ||
            !reserved.gameObject.activeInHierarchy)
            return;
        if (!TryRenderedGlyphRectsIn(owner, text, out List<Rect> glyphRects))
            return;
        Rect glyphs = Union(glyphRects);
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
            owner, reserved);
        Rect exclusion = new Rect(
            bounds.min.x - clearance,
            bounds.min.y - clearance,
            bounds.size.x + clearance * 2f,
            bounds.size.y + clearance * 2f);
        if (glyphRects.Any(glyph => glyph.Overlaps(exclusion)))
        {
            violations.Add(
                context + $" glyphs intersect {clearance:0.###}-unit " +
                $"reserved zone; glyphs={glyphs}, reserved={exclusion}");
        }
    }

    static void RestoreString(string key, bool existed, string value)
    {
        if (existed) PlayerPrefs.SetString(key, value);
        else PlayerPrefs.DeleteKey(key);
    }

    static void RestoreInt(string key, bool existed, int value)
    {
        if (existed) PlayerPrefs.SetInt(key, value);
        else PlayerPrefs.DeleteKey(key);
    }

    static Vector2 CanvasSize(Vector2 viewport)
    {
        MethodInfo method = RuntimeType("ResponsiveViewportGeometry")
            .GetMethod(
                "CanvasSizeForViewport",
                BindingFlags.Static | BindingFlags.Public);
        Assert.That(method, Is.Not.Null);
        return (Vector2)method.Invoke(null, new object[]
        {
            viewport,
            new Vector2(1080f, 1920f),
            0.5f,
        });
    }

    static void SetLanguage(string language)
    {
        Type l10n = RuntimeType("L10n");
        Type languageType = l10n.GetNestedType(
            "Language", BindingFlags.Public);
        MethodInfo method = l10n.GetMethod(
            "SetLanguage", BindingFlags.Static | BindingFlags.Public);
        Assert.That(languageType, Is.Not.Null);
        Assert.That(method, Is.Not.Null);
        method.Invoke(null, new[] { Enum.Parse(languageType, language) });
    }

    static string Localized(string key, params object[] arguments)
    {
        MethodInfo method = RuntimeType("L10n").GetMethod(
            "Get", BindingFlags.Static | BindingFlags.Public);
        Assert.That(method, Is.Not.Null);
        return (string)method.Invoke(null, new object[]
        {
            key,
            arguments ?? new object[0],
        });
    }

    static Rect BoundsInSafeRect(
        RectTransform safeRoot,
        RectTransform target,
        Rect safeRect)
    {
        Bounds localBounds =
            RectTransformUtility.CalculateRelativeRectTransformBounds(
                safeRoot, target);
        float scale = Mathf.Abs(safeRoot.localScale.x);
        Vector2 size = new Vector2(
            localBounds.size.x * scale,
            localBounds.size.y * scale);
        Vector2 center = safeRect.center + new Vector2(
            localBounds.center.x * scale,
            localBounds.center.y * scale);
        return new Rect(center - size * 0.5f, size);
    }

    static void AssertContained(Rect outer, Rect inner, string context)
    {
        const float tolerance = 0.5f;
        Assert.That(inner.xMin,
            Is.GreaterThanOrEqualTo(outer.xMin - tolerance), context);
        Assert.That(inner.xMax,
            Is.LessThanOrEqualTo(outer.xMax + tolerance), context);
        Assert.That(inner.yMin,
            Is.GreaterThanOrEqualTo(outer.yMin - tolerance), context);
        Assert.That(inner.yMax,
            Is.LessThanOrEqualTo(outer.yMax + tolerance), context);
    }

    static void AssertVerticalGap(
        Rect upper, Rect lower, float minimumGap, string context)
    {
        Assert.That(upper.yMin - lower.yMax,
            Is.GreaterThanOrEqualTo(minimumGap), context);
    }

    static void AssertHorizontalGap(
        Rect left, Rect right, float minimumGap, string context)
    {
        Assert.That(right.xMin - left.xMax,
            Is.GreaterThanOrEqualTo(minimumGap), context);
    }

    static void AssertRenderedTextWithinRect(
        TMP_Text text,
        string context)
    {
        text.ForceMeshUpdate();
        Assert.That(text.isTextOverflowing, Is.False,
            context + " reports overflow/truncation.");
        Bounds rendered = text.textBounds;
        Rect available = text.rectTransform.rect;
        string metrics = context +
            $" / font={text.fontSize:0.###}, max={text.fontSizeMax:0.###}, " +
            $"rect={available}, textBounds={rendered}";
        const float tolerance = 1f;
        Assert.That(rendered.min.x,
            Is.GreaterThanOrEqualTo(available.xMin - tolerance), metrics);
        Assert.That(rendered.max.x,
            Is.LessThanOrEqualTo(available.xMax + tolerance), metrics);
        Assert.That(rendered.min.y,
            Is.GreaterThanOrEqualTo(available.yMin - tolerance), metrics);
        Assert.That(rendered.max.y,
            Is.LessThanOrEqualTo(available.yMax + tolerance), metrics);
    }

    static void AssertRenderedTextHorizontalSafety(
        TMP_Text text,
        float safety,
        string context)
    {
        text.ForceMeshUpdate();
        Bounds rendered = text.textBounds;
        Rect available = text.rectTransform.rect;
        string metrics = context +
            $" / safety={safety:0.###}, rect={available}, textBounds={rendered}";
        Assert.That(rendered.min.x,
            Is.GreaterThanOrEqualTo(available.xMin + safety), metrics);
        Assert.That(rendered.max.x,
            Is.LessThanOrEqualTo(available.xMax - safety), metrics);
    }

    static void AssertRenderedTextInsidePanel(
        TMP_Text text,
        RectTransform panel,
        Vector4 padding,
        string context)
    {
        // An inactive phase can retain text in a reusable TMP label while its
        // parent panel is hidden. TMP reports an extreme sentinel textBounds
        // value because no glyphs are rendered in that state.
        // The label RectTransform is still covered by the viewport checks.
        if (!text.gameObject.activeInHierarchy ||
            !text.enabled ||
            string.IsNullOrWhiteSpace(text.text))
            return;

        Assert.That(
            TryRenderedGlyphRectIn(panel, text, out Rect glyphs, out int count),
            Is.True,
            context + " must render at least one visible glyph");
        Assert.That(count, Is.GreaterThan(0), context);
        Rect safe = panel.rect;
        safe.xMin += padding.x;
        safe.yMin += padding.y;
        safe.xMax -= padding.z;
        safe.yMax -= padding.w;
        AssertContained(safe, glyphs, context);
    }

    static void AssertRenderedTextLeftOf(
        TMP_Text text,
        RectTransform reserved,
        RectTransform panel,
        float minimumGap,
        string context)
    {
        if (!text.gameObject.activeInHierarchy ||
            !text.enabled ||
            string.IsNullOrWhiteSpace(text.text))
            return;

        Assert.That(
            TryRenderedGlyphRectIn(panel, text, out Rect glyphs, out int count),
            Is.True,
            context + " must render at least one visible glyph");
        Assert.That(count, Is.GreaterThan(0), context);
        Bounds reservedBounds =
            RectTransformUtility.CalculateRelativeRectTransformBounds(
                panel, reserved);
        float gap = reservedBounds.min.x - glyphs.xMax;
        Assert.That(gap, Is.GreaterThanOrEqualTo(minimumGap),
            context + $" / gap={gap:0.###}");
    }

    static bool TryRenderedGlyphRectIn(
        RectTransform owner,
        TMP_Text text,
        out Rect glyphs,
        out int visibleGlyphCount)
    {
        glyphs = default;
        visibleGlyphCount = 0;
        if (!TryRenderedGlyphRectsIn(owner, text, out List<Rect> rectangles))
            return false;

        visibleGlyphCount = rectangles.Count;
        glyphs = Union(rectangles);
        return true;
    }

    static bool TryRenderedGlyphRectsIn(
        RectTransform owner,
        TMP_Text text,
        out List<Rect> rectangles)
    {
        rectangles = new List<Rect>();
        if (owner == null || text == null)
            return false;

        text.ForceMeshUpdate(true, true);
        CalculateEffectExpansion(
            owner, text, out float left, out float bottom,
            out float right, out float top);
        TMP_TextInfo info = text.textInfo;
        for (int index = 0; index < info.characterCount; index++)
        {
            TMP_CharacterInfo character = info.characterInfo[index];
            if (!character.isVisible)
                continue;
            Vector2 minimum = new Vector2(
                float.PositiveInfinity, float.PositiveInfinity);
            Vector2 maximum = new Vector2(
                float.NegativeInfinity, float.NegativeInfinity);
            Vector3[] corners =
            {
                character.bottomLeft,
                character.topLeft,
                character.topRight,
                character.bottomRight,
            };
            foreach (Vector3 corner in corners)
            {
                Vector3 local = owner.InverseTransformPoint(
                    text.rectTransform.TransformPoint(corner));
                minimum = Vector2.Min(minimum, local);
                maximum = Vector2.Max(maximum, local);
            }
            rectangles.Add(Rect.MinMaxRect(
                minimum.x + left,
                minimum.y + bottom,
                maximum.x + right,
                maximum.y + top));
        }
        return rectangles.Count > 0;
    }

    static void CalculateEffectExpansion(
        RectTransform owner,
        TMP_Text text,
        out float left,
        out float bottom,
        out float right,
        out float top)
    {
        // TMP character quads already include the SDF material padding and
        // configured material outline. Expand only for live uGUI
        // Shadow/Outline components, whose duplicated geometry is outside the
        // TMP character quad.
        left = 0f;
        bottom = 0f;
        right = 0f;
        top = 0f;

        foreach (Shadow effect in text.GetComponents<Shadow>())
        {
            if (effect == null || !effect.enabled)
                continue;
            Vector2 distance = effect.effectDistance;
            Vector3 offset = owner.InverseTransformVector(
                text.rectTransform.TransformVector(
                    new Vector3(distance.x, distance.y, 0f)));
            if (effect is Outline)
            {
                float x = Mathf.Abs(offset.x);
                float y = Mathf.Abs(offset.y);
                left = Mathf.Min(left, -x);
                bottom = Mathf.Min(bottom, -y);
                right = Mathf.Max(right, x);
                top = Mathf.Max(top, y);
            }
            else
            {
                left = Mathf.Min(left, offset.x);
                bottom = Mathf.Min(bottom, offset.y);
                right = Mathf.Max(right, offset.x);
                top = Mathf.Max(top, offset.y);
            }
        }
    }

    static Rect Union(IReadOnlyList<Rect> rectangles)
    {
        Rect result = rectangles[0];
        for (int index = 1; index < rectangles.Count; index++)
        {
            Rect item = rectangles[index];
            result = Rect.MinMaxRect(
                Mathf.Min(result.xMin, item.xMin),
                Mathf.Min(result.yMin, item.yMin),
                Mathf.Max(result.xMax, item.xMax),
                Mathf.Max(result.yMax, item.yMax));
        }
        return result;
    }

    static void CollectRenderedTextWithinRect(
        ICollection<string> violations,
        TMP_Text text,
        string context)
    {
        text.ForceMeshUpdate(true, true);
        Rect available = text.rectTransform.rect;
        var reasons = new List<string>();
        if (text.isTextOverflowing)
            reasons.Add("overflow/truncation expected false but was true");
        bool hasGlyphs = TryRenderedGlyphRectIn(
            text.rectTransform, text, out Rect rendered, out int glyphCount);
        if (!hasGlyphs)
        {
            reasons.Add("non-empty active label rendered zero visible glyphs");
            AddGlyphViolation(
                violations, context, text, reasons,
                $"font={text.fontSize:0.###}, max={text.fontSizeMax:0.###}, " +
                $"rect={available}, visible={glyphCount}");
            return;
        }
        if (text.maxVisibleCharacters < text.textInfo.characterCount)
            reasons.Add("maxVisibleCharacters hides copy");
        if (text.maxVisibleWords < text.textInfo.wordCount)
            reasons.Add("maxVisibleWords hides copy");
        if (text.maxVisibleLines < text.textInfo.lineCount)
            reasons.Add("maxVisibleLines hides copy");
        const float tolerance = 1f;
        AddLowerBoundReason(
            reasons, "glyph min x", rendered.xMin,
            available.xMin - tolerance);
        AddUpperBoundReason(
            reasons, "glyph max x", rendered.xMax,
            available.xMax + tolerance);
        AddLowerBoundReason(
            reasons, "glyph min y", rendered.yMin,
            available.yMin - tolerance);
        AddUpperBoundReason(
            reasons, "glyph max y", rendered.yMax,
            available.yMax + tolerance);
        AddGlyphViolation(
            violations, context, text, reasons,
            $"font={text.fontSize:0.###}, max={text.fontSizeMax:0.###}, " +
            $"rect={available}, glyphs={rendered}, visible={glyphCount}");
    }

    static void CollectRenderedTextHorizontalSafety(
        ICollection<string> violations,
        TMP_Text text,
        float safety,
        string context)
    {
        text.ForceMeshUpdate(true, true);
        if (!TryRenderedGlyphRectIn(
                text.rectTransform, text, out Rect rendered, out int glyphCount))
        {
            violations.Add(context + " rendered zero visible glyphs");
            return;
        }
        Rect available = text.rectTransform.rect;
        var reasons = new List<string>();
        AddLowerBoundReason(
            reasons, "glyph min x", rendered.xMin,
            available.xMin + safety);
        AddUpperBoundReason(
            reasons, "glyph max x", rendered.xMax,
            available.xMax - safety);
        AddGlyphViolation(
            violations, context + " horizontal-safety", text, reasons,
            $"safety={safety:0.###}, rect={available}, " +
            $"glyphs={rendered}, visible={glyphCount}");
    }

    static void CollectRenderedTextInsidePanel(
        ICollection<string> violations,
        TMP_Text text,
        RectTransform panel,
        Vector4 padding,
        string context)
    {
        if (panel == null || !IsRenderedText(text))
            return;

        if (!TryRenderedGlyphRectIn(
                panel, text, out Rect glyphs, out int glyphCount))
        {
            violations.Add(context + " rendered zero visible glyphs");
            return;
        }
        Rect safe = panel.rect;
        safe.xMin += padding.x;
        safe.yMin += padding.y;
        safe.xMax -= padding.z;
        safe.yMax -= padding.w;
        var reasons = new List<string>();
        const float tolerance = 0.5f;
        AddLowerBoundReason(
            reasons, "panel glyph min x", glyphs.xMin,
            safe.xMin - tolerance);
        AddUpperBoundReason(
            reasons, "panel glyph max x", glyphs.xMax,
            safe.xMax + tolerance);
        AddLowerBoundReason(
            reasons, "panel glyph min y", glyphs.yMin,
            safe.yMin - tolerance);
        AddUpperBoundReason(
            reasons, "panel glyph max y", glyphs.yMax,
            safe.yMax + tolerance);
        AddGlyphViolation(
            violations, context, text, reasons,
            $"safe={safe}, glyphs={glyphs}, visible={glyphCount}");
    }

    static void CollectRenderedTextLeftOf(
        ICollection<string> violations,
        TMP_Text text,
        RectTransform reserved,
        RectTransform panel,
        float minimumGap,
        string context)
    {
        if (!IsRenderedText(text))
            return;

        if (!TryRenderedGlyphRectIn(
                panel, text, out Rect glyphs, out int glyphCount))
        {
            violations.Add(context + " rendered zero visible glyphs");
            return;
        }
        Bounds reservedBounds =
            RectTransformUtility.CalculateRelativeRectTransformBounds(
                panel, reserved);
        float gap = reservedBounds.min.x - glyphs.xMax;
        var reasons = new List<string>();
        AddLowerBoundReason(reasons, "mascot gap", gap, minimumGap);
        AddGlyphViolation(
            violations, context, text, reasons,
            $"minimumGap={minimumGap:0.###}, gap={gap:0.###}, " +
            $"glyphs={glyphs}, reserved={reservedBounds}, visible={glyphCount}");
    }

    static void AddLowerBoundReason(
        ICollection<string> reasons,
        string label,
        float actual,
        float minimum)
    {
        if (!(actual >= minimum))
            reasons.Add(
                $"{label} expected >= {minimum:0.###} but was {actual:0.###}");
    }

    static void AddUpperBoundReason(
        ICollection<string> reasons,
        string label,
        float actual,
        float maximum)
    {
        if (!(actual <= maximum))
            reasons.Add(
                $"{label} expected <= {maximum:0.###} but was {actual:0.###}");
    }

    static void AddGlyphViolation(
        ICollection<string> violations,
        string context,
        TMP_Text text,
        ICollection<string> reasons,
        string metrics)
    {
        if (reasons.Count == 0)
            return;
        string value = (text.text ?? string.Empty)
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
        violations.Add(
            context + " / " + string.Join("; ", reasons) +
            $" / text=\"{value}\" / {metrics}");
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

    static void AssertButtonSprite(
        Transform root, string name, string resource)
    {
        Button button = Find(root, name).GetComponent<Button>();
        Assert.That(button, Is.Not.Null, name);
        Image image = button.GetComponent<Image>();
        Sprite sprite = Resources.Load<Sprite>(resource);
        Assert.That(sprite, Is.Not.Null, resource);
        Assert.That(image.sprite, Is.SameAs(sprite), name);
        Assert.That(image.type, Is.EqualTo(Image.Type.Simple), name);
        Assert.That(image.color.a, Is.EqualTo(1f).Within(0.001f), name);
        Assert.That(image.raycastTarget, Is.True, name);
        Assert.That(button.targetGraphic, Is.SameAs(image), name);
    }

    static void AssertSlicedSprite(
        Transform root, string name, string resource)
    {
        Image image = Find(root, name).GetComponent<Image>();
        Sprite sprite = Resources.Load<Sprite>(resource);
        Assert.That(sprite, Is.Not.Null, resource);
        Assert.That(image.sprite, Is.SameAs(sprite), name);
        Assert.That(image.type, Is.EqualTo(Image.Type.Sliced), name);
        Assert.That(image.color.a, Is.EqualTo(1f).Within(0.001f), name);
        Assert.That(image.raycastTarget, Is.False, name);
    }

    static T GetField<T>(Component component, string name) where T : class
    {
        FieldInfo field = component.GetType().GetField(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        return field.GetValue(component) as T;
    }

    static T GetProperty<T>(Component component, string name)
    {
        PropertyInfo property = component.GetType().GetProperty(
            name, BindingFlags.Instance | BindingFlags.Public |
                  BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, name);
        return (T)property.GetValue(component);
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

    static T OnboardingProfileConstant<T>(string name)
    {
        return RuntimeConstant<T>("OnboardingProfile", name);
    }

    static T RuntimeConstant<T>(string typeName, string name)
    {
        FieldInfo field = RuntimeType(typeName).GetField(name, StaticFlags);
        Assert.That(field, Is.Not.Null,
            "Missing canonical " + typeName + " constant " + name + ".");
        return (T)field.GetValue(null);
    }

    static int OnboardingAvatarCount()
    {
        PropertyInfo count = RuntimeType("OnboardingAvatarCatalog")
            .GetProperty("Count", StaticFlags);
        Assert.That(count, Is.Not.Null,
            "Missing canonical Onboarding avatar count.");
        return (int)count.GetValue(null);
    }

    static bool IsValidOnboardingAvatar(int index)
    {
        MethodInfo valid = RuntimeType("OnboardingProfile")
            .GetMethod("IsValidAvatar", StaticFlags);
        Assert.That(valid, Is.Not.Null,
            "Missing canonical Onboarding avatar validator.");
        return (bool)valid.Invoke(null, new object[] { index });
    }

    static bool TryCommitOnboardingAvatar(int index)
    {
        Type profile = RuntimeType("OnboardingProfile");
        Type gender = profile.GetNestedType(
            "GenderChoice", BindingFlags.Public);
        Type age = profile.GetNestedType(
            "AgeCategory", BindingFlags.Public);
        MethodInfo commit = profile.GetMethod("TryCommit", StaticFlags);
        Assert.That(gender, Is.Not.Null,
            "Missing canonical Onboarding gender contract.");
        Assert.That(age, Is.Not.Null,
            "Missing canonical Onboarding age contract.");
        Assert.That(commit, Is.Not.Null,
            "Missing canonical Onboarding commit contract.");
        return (bool)commit.Invoke(null, new[]
        {
            (object)"AvatarTester",
            Enum.ToObject(gender, 0),
            index,
            Enum.ToObject(age, 2),
        });
    }

    static void RecordPlayerMove(
        Component layout,
        int roundNumber,
        int guess,
        string hintName,
        int newPlayerMin,
        int newPlayerMax)
    {
        MethodInfo method = layout.GetType().GetMethod(
            "RecordPlayerMove",
            BindingFlags.Instance | BindingFlags.Public);
        Assert.That(method, Is.Not.Null, "RecordPlayerMove");
        ParameterInfo[] parameters = method.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(9));
        Type hintType = parameters[2].ParameterType;
        Assert.That(hintType.IsEnum, Is.True,
            "RecordPlayerMove hint must remain the canonical enum contract.");
        object accepted = method.Invoke(layout, new object[]
        {
            roundNumber,
            guess,
            Enum.Parse(hintType, hintName),
            false,
            100,
            newPlayerMin,
            newPlayerMax,
            1,
            100,
        });
        Assert.That(accepted, Is.EqualTo(true));
    }

    static string OnboardingAvatarResourcePath(int index)
    {
        MethodInfo get = RuntimeType("OnboardingAvatarCatalog")
            .GetMethod("Get", StaticFlags);
        Assert.That(get, Is.Not.Null,
            "Missing canonical Onboarding avatar resolver.");
        object entry = get.Invoke(null, new object[] { index });
        PropertyInfo path = entry.GetType().GetProperty(
            "ResourcePath", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(path, Is.Not.Null,
            "Missing canonical Onboarding avatar resource path.");
        return (string)path.GetValue(entry);
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
        if (type == null)
            type = Type.GetType(name + ", HOL.Core");
        Assert.That(type, Is.Not.Null, name);
        return type;
    }
}
