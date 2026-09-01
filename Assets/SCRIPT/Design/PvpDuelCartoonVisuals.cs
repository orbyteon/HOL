using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Sole presentation owner for the live PvP duel and its authoritative result.
// PvpGameController, PvpBackend, GuessHistoryRail and the existing callback-
// bearing controls retain all state/network authority. This class only seats
// those controls inside the approved modular cartoon composition.
[DefaultExecutionOrder(2700)]
[DisallowMultipleComponent]
public sealed class PvpDuelCartoonVisuals : MonoBehaviour
{
    public const string MatchRootName = "PvpDuelCartoonRoot";
    public const string ResultRootName = "PvpResultCartoonRoot";

    const string BackgroundResource = "phase2a/hol_neon_reference_bg_r3";
    const string LogoResource = "reference/hol_logo_exact";
    const string PlayerResource = "reference/player_cyan_exact";
    const string OpponentResource = "reference/opponent_purple_exact";
    const string HeroResource = "reference/char_boy_exact";
    const string VsResource = "reference/board_vs_burst_exact";
    const string TrophyResource = "reference/board_trophy_exact";
    const string MascotSixResource = "reference/mascot_6_exact";
    const string MascotSevenResource = "reference/mascot_7_exact";
    const string MascotThreeResource = "reference/mascot_3_exact";
    const string SpeechBubbleResource = "cartoon/cartoon_speech_bubble";
    const string StarsResource = "mainmenu/mainmenu_deco_stars";
    const string ConfettiResource = "mainmenu/mainmenu_deco_confetti";
    const string BackChevronResource = "phase2a/hol_chevron_r2";
    const string ChipResource = "phase2a/hol_player_chip_r2_9s";
    const string BlueFrameResource = "phase2a/hol_cta_blue_r2_9s";
    const string MagentaFrameResource = "phase2a/hol_cta_magenta_r2_9s";
    const string GoldFrameResource = "phase2a/hol_cta_gold_r2_9s";
    const string PurpleFrameResource = "mainmenu/mainmenu_tip_frame_9s";
    const string DisplayFontResource = "phase2a/fonts/HOL Menu Display SDF";
    const string BodyFontResource = "phase2a/fonts/HOL Menu Body SDF";

    const float ReferenceWidth = 1080f;
    const float ReferenceHeight = 1920f;

    static readonly Color NearWhite = new Color(0.985f, 0.975f, 1f, 1f);
    static readonly Color Cyan = new Color(0.20f, 0.94f, 1f, 1f);
    static readonly Color Gold = new Color(1f, 0.80f, 0.20f, 1f);
    static readonly Color Muted = new Color(0.85f, 0.83f, 0.95f, 0.90f);
    static readonly Color Ink = new Color(0.08f, 0.04f, 0.17f, 1f);

    PvpGameController pvp;
    TMP_FontAsset displayFont;
    TMP_FontAsset bodyFont;
    TMP_Text matchPlayerName;
    TMP_Text matchChipText;
    TMP_Text resultStreakText;
    TMP_Text resultPlayerName;
    bool built;
    float nextRefresh;

    public bool IsReady { get; private set; }

    IEnumerator Start()
    {
        for (int frame = 0; frame < 300; frame++)
        {
            pvp = GetComponent<PvpGameController>();
            if (MatchControlsReady() && ResultControlsReady())
            {
                Build();
                yield break;
            }
            yield return null;
        }

        Debug.LogError(
            "[PvpDuelCartoonVisuals] PvP controls were not ready within 300 frames.");
    }

    void OnEnable()
    {
        L10n.OnLanguageChanged += RefreshDynamicCopy;
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= RefreshDynamicCopy;
    }

    void LateUpdate()
    {
        if (!built || Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.25f;
        RefreshDynamicCopy();
    }

    bool MatchControlsReady()
    {
        return pvp != null &&
               pvp.matchPanel != null &&
               pvp.guessInput != null &&
               pvp.guessButton != null &&
               pvp.keypadRoot != null &&
               pvp.leaveButton != null &&
               pvp.opponentNameText != null &&
               pvp.turnText != null &&
               pvp.roundText != null &&
               pvp.historyText != null &&
               pvp.historyRail != null &&
               pvp.rangeText != null &&
               pvp.lockButton != null &&
               pvp.signalFeedText != null;
    }

    bool ResultControlsReady()
    {
        return pvp != null &&
               pvp.resultPresentation != null &&
               pvp.resultPresentation.titleText != null &&
               pvp.resultPresentation.playerAttemptsText != null &&
               pvp.resultPresentation.opponentAttemptsText != null &&
               pvp.resultPresentation.revealedNumberText != null &&
               pvp.rematchButton != null &&
               pvp.rematchSecretInput != null &&
               pvp.resultExitButton != null;
    }

    void Build()
    {
        if (built) return;

        displayFont = Resources.Load<TMP_FontAsset>(DisplayFontResource);
        bodyFont = Resources.Load<TMP_FontAsset>(BodyFontResource);
        IsReady = displayFont != null && bodyFont != null;
        foreach (string resource in new[]
        {
            BackgroundResource, LogoResource, PlayerResource,
            OpponentResource, HeroResource, VsResource, TrophyResource,
            MascotSixResource, MascotSevenResource, MascotThreeResource,
            SpeechBubbleResource, StarsResource, ConfettiResource,
            BackChevronResource, ChipResource, BlueFrameResource,
            MagentaFrameResource, GoldFrameResource, PurpleFrameResource,
        })
        {
            if (Resources.Load<Sprite>(resource) == null)
            {
                Debug.LogError(
                    "[PvpDuelCartoonVisuals] Missing Resources/" + resource + ".");
                IsReady = false;
            }
        }

        if (!IsReady) return;

        BuildMatch();
        BuildResult();
        built = true;
        RefreshDynamicCopy();
    }

    void BuildMatch()
    {
        Transform panel = pvp.matchPanel.transform;
        DisableRootImage(pvp.matchPanel);

        RectTransform root = EnsureRect(panel, MatchRootName);
        Stretch(root);
        root.SetAsFirstSibling();
        BuildBackdrop(root, "PvpMatch");

        RectTransform safe = EnsureRect(root, "PvpMatchSafeRoot");
        Stretch(safe);
        AttachSafeArea(safe, panel);

        AddSprite(
            safe, "PvpMatchLogo", LogoResource,
            new Vector2(0f, 830f), new Vector2(310f, 155f));

        Button leave = pvp.leaveButton.GetComponent<Button>();
        if (leave != null)
        {
            Reparent(leave.transform, safe);
            Place(
                (RectTransform)leave.transform, new Vector2(-484f, 842f),
                new Vector2(90f, 90f));
            StyleButton(leave, PurpleFrameResource, NearWhite, 0f);
            HideButtonLabels(leave.transform);
            Image icon = AddSprite(
                leave.transform, "PvpLeaveBackIcon", BackChevronResource,
                Vector2.zero, new Vector2(46f, 58f));
            icon.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
        }

        GameObject chip = Frame(
            safe, "PvpMatchPlayerChip", ChipResource,
            new Vector2(350f, 842f), new Vector2(365f, 118f));
        AddSprite(
            chip.transform, "PvpMatchChipAvatar", PlayerResource,
            new Vector2(-126f, 0f), new Vector2(84f, 84f));
        AddSprite(
            chip.transform, "PvpMatchChipTrophy", TrophyResource,
            new Vector2(-18f, -28f), new Vector2(42f, 42f));
        matchChipText = BodyText(
            chip.transform, "PvpMatchChipText", "", 26f,
            new Vector2(55f, 0f), new Vector2(205f, 82f), NearWhite);

        GameObject playerCard = Frame(
            safe, "PvpPlayerCard", BlueFrameResource,
            new Vector2(-270f, 610f), new Vector2(470f, 345f));
        AddSprite(
            playerCard.transform, "PvpPlayerCharacter", PlayerResource,
            new Vector2(0f, 20f), new Vector2(310f, 265f));
        TMP_Text you = DisplayText(
            playerCard.transform, "PvpPlayerCaption", L10n.Get("you"),
            27f, new Vector2(0f, 136f), new Vector2(390f, 44f),
            NearWhite);
        RuntimeUI.Localize(you, "you");
        matchPlayerName = DisplayText(
            playerCard.transform, "PvpPlayerName", "", 38f,
            new Vector2(0f, -130f), new Vector2(410f, 58f), NearWhite);

        GameObject opponentCard = Frame(
            safe, "PvpOpponentCard", MagentaFrameResource,
            new Vector2(270f, 610f), new Vector2(470f, 345f));
        AddSprite(
            opponentCard.transform, "PvpOpponentCharacter", OpponentResource,
            new Vector2(0f, 20f), new Vector2(310f, 265f));
        TMP_Text opponentCaption = DisplayText(
            opponentCard.transform, "PvpOpponentCaption",
            L10n.Get("prebattle_opponent"), 25f,
            new Vector2(0f, 136f), new Vector2(400f, 44f), NearWhite);
        RuntimeUI.Localize(opponentCaption, "prebattle_opponent");
        Reparent(pvp.opponentNameText.transform, opponentCard.transform);
        Place(
            pvp.opponentNameText.rectTransform, new Vector2(0f, -130f),
            new Vector2(410f, 58f));
        StyleDisplay(pvp.opponentNameText, 26f, 35f, NearWhite);

        AddSprite(
            safe, "PvpVsBurst", VsResource,
            new Vector2(0f, 605f), new Vector2(190f, 190f));

        GameObject ribbon = Frame(
            safe, "PvpPromptRibbon", PurpleFrameResource,
            new Vector2(0f, 370f), new Vector2(900f, 150f));
        Reparent(pvp.roundText.transform, ribbon.transform);
        Place(
            pvp.roundText.rectTransform, new Vector2(0f, 42f),
            new Vector2(760f, 42f));
        StyleDisplay(pvp.roundText, 23f, 30f, NearWhite);
        Reparent(pvp.turnText.transform, ribbon.transform);
        Place(
            pvp.turnText.rectTransform, new Vector2(0f, -25f),
            new Vector2(820f, 78f));
        StyleDisplay(pvp.turnText, 29f, 40f, NearWhite);
        if (pvp.resultText != null)
        {
            Reparent(pvp.resultText.transform, ribbon.transform);
            Place(
                pvp.resultText.rectTransform, Vector2.zero,
                new Vector2(820f, 120f));
            StyleDisplay(pvp.resultText, 28f, 38f, NearWhite);
        }

        GameObject interaction = Frame(
            safe, "PvpInteractionCard", PurpleFrameResource,
            new Vector2(-225f, -255f), new Vector2(610f, 940f));
        TMP_Text current = DisplayText(
            interaction.transform, "PvpCurrentNumberHeading",
            L10n.Get("hud_current_number"), 30f,
            new Vector2(0f, 405f), new Vector2(520f, 48f), NearWhite);
        RuntimeUI.Localize(current, "hud_current_number");

        Reparent(pvp.guessInput.transform, interaction.transform);
        Place(
            (RectTransform)pvp.guessInput.transform,
            new Vector2(0f, 315f), new Vector2(500f, 125f));
        StyleInput(pvp.guessInput);

        Reparent(pvp.keypadRoot.transform, interaction.transform);
        Place(
            (RectTransform)pvp.keypadRoot.transform,
            new Vector2(0f, -25f), new Vector2(560f, 540f));
        LayoutKeypad(pvp.keypadRoot.transform);

        Button lockButton = pvp.lockButton.GetComponent<Button>();
        if (lockButton != null)
        {
            Reparent(lockButton.transform, interaction.transform);
            Place(
                (RectTransform)lockButton.transform,
                new Vector2(-135f, -335f), new Vector2(250f, 76f));
            StyleButton(lockButton, BlueFrameResource, Ink, 25f);
        }

        Button guessButton = pvp.guessButton.GetComponent<Button>();
        if (guessButton != null)
        {
            Reparent(guessButton.transform, interaction.transform);
            Place(
                (RectTransform)guessButton.transform,
                new Vector2(135f, -410f), new Vector2(270f, 104f));
            StyleButton(guessButton, GoldFrameResource, Ink, 39f);
        }

        GameObject rail = Frame(
            safe, "PvpOpponentRail", PurpleFrameResource,
            new Vector2(330f, -255f), new Vector2(350f, 940f));

        GameObject bubble = Frame(
            rail.transform, "PvpSignalBubble", SpeechBubbleResource,
            new Vector2(0f, 320f), new Vector2(310f, 230f));
        Reparent(pvp.signalFeedText.transform, bubble.transform);
        Place(
            pvp.signalFeedText.rectTransform, new Vector2(-30f, 5f),
            new Vector2(230f, 150f));
        StyleBody(pvp.signalFeedText, 20f, 28f, Ink);
        AddSprite(
            bubble.transform, "PvpSignalAvatar", OpponentResource,
            new Vector2(105f, -65f), new Vector2(85f, 85f));

        GameObject history = Frame(
            rail.transform, "PvpHistoryCard", PurpleFrameResource,
            new Vector2(0f, 35f), new Vector2(310f, 390f));
        TMP_Text historyTitle = DisplayText(
            history.transform, "PvpHistoryTitle", L10n.Get("hud_history"),
            27f, new Vector2(0f, 150f), new Vector2(270f, 46f),
            NearWhite);
        RuntimeUI.Localize(historyTitle, "hud_history");
        Reparent(pvp.historyText.transform, history.transform);
        Place(
            pvp.historyText.rectTransform, new Vector2(0f, 55f),
            new Vector2(270f, 100f));
        StyleBody(pvp.historyText, 20f, 28f, NearWhite);
        if (pvp.historyRail.target != null)
        {
            Reparent(pvp.historyRail.target.transform, history.transform);
            Place(
                pvp.historyRail.target.rectTransform,
                new Vector2(0f, -85f), new Vector2(270f, 150f));
            StyleBody(pvp.historyRail.target, 17f, 23f, Muted);
        }

        GameObject tip = Frame(
            rail.transform, "PvpTipCard", PurpleFrameResource,
            new Vector2(0f, -330f), new Vector2(310f, 220f));
        TMP_Text tipTitle = DisplayText(
            tip.transform, "PvpTipTitle", L10n.Get("hud_tip"), 27f,
            new Vector2(0f, 76f), new Vector2(260f, 42f), Gold);
        RuntimeUI.Localize(tipTitle, "hud_tip");
        Reparent(pvp.rangeText.transform, tip.transform);
        Place(
            pvp.rangeText.rectTransform, new Vector2(0f, -20f),
            new Vector2(270f, 125f));
        StyleBody(pvp.rangeText, 21f, 28f, Cyan);

        if (pvp.signalsRoot != null)
        {
            Reparent(pvp.signalsRoot.transform, safe);
            Place(
                (RectTransform)pvp.signalsRoot.transform,
                new Vector2(0f, -760f), new Vector2(960f, 150f));
            LayoutSignalButtons(pvp.signalsRoot.transform);
        }

        HideLegacyChildren(panel, root,
            pvp.resultPresentation.transform,
            pvp.terminalPresentation != null
                ? pvp.terminalPresentation.transform
                : null);
    }

    void BuildResult()
    {
        PvpResultPresentation result = pvp.resultPresentation;
        Transform host = result.transform;
        DisableRootImage(result.gameObject);

        RectTransform root = EnsureRect(host, ResultRootName);
        Stretch(root);
        BuildBackdrop(root, "PvpResult");

        RectTransform safe = EnsureRect(root, "PvpResultSafeRoot");
        Stretch(safe);
        AttachSafeArea(safe, host);

        AddSprite(
            safe, "PvpResultLogo", LogoResource,
            new Vector2(0f, 820f), new Vector2(330f, 165f));

        GameObject chip = Frame(
            safe, "PvpResultPlayerChip", ChipResource,
            new Vector2(350f, 842f), new Vector2(365f, 118f));
        AddSprite(
            chip.transform, "PvpResultChipAvatar", PlayerResource,
            new Vector2(-126f, 0f), new Vector2(84f, 84f));
        AddSprite(
            chip.transform, "PvpResultChipTrophy", TrophyResource,
            new Vector2(-18f, -28f), new Vector2(42f, 42f));
        resultPlayerName = BodyText(
            chip.transform, "PvpResultPlayerName", "", 26f,
            new Vector2(55f, 0f), new Vector2(205f, 82f), NearWhite);
        result.playerChipText = resultPlayerName;

        GameObject titleRibbon = Frame(
            safe, "PvpResultTitleRibbon", PurpleFrameResource,
            new Vector2(0f, 650f), new Vector2(900f, 160f));
        Reparent(result.titleText.transform, titleRibbon.transform);
        StretchText(result.titleText.rectTransform, 48f, 18f);
        StyleDisplay(result.titleText, 46f, 72f, NearWhite);

        AddSprite(
            safe, "PvpResultHero", HeroResource,
            new Vector2(-220f, 285f), new Vector2(520f, 540f));
        Image trophy = AddSprite(
            safe, "PvpResultTrophy", TrophyResource,
            new Vector2(65f, 345f), new Vector2(250f, 280f));
        result.trophy = trophy.gameObject;

        GameObject defeated = Frame(
            safe, "PvpResultOpponentCard", MagentaFrameResource,
            new Vector2(330f, 285f), new Vector2(330f, 420f));
        AddSprite(
            defeated.transform, "PvpResultOpponentCharacter",
            OpponentResource, new Vector2(0f, 50f),
            new Vector2(230f, 250f));
        TMP_Text opponentLabel = DisplayText(
            defeated.transform, "PvpResultOpponentLabel",
            L10n.Get("prebattle_opponent"), 25f,
            new Vector2(0f, 165f), new Vector2(280f, 42f), NearWhite);
        RuntimeUI.Localize(opponentLabel, "prebattle_opponent");
        TMP_Text opponentName = DisplayText(
            defeated.transform, "PvpResultOpponentName", "", 31f,
            new Vector2(0f, -145f), new Vector2(280f, 54f), NearWhite);
        opponentName.text = pvp.opponentNameText != null
            ? pvp.opponentNameText.text
            : string.Empty;

        GameObject stats = Frame(
            safe, "PvpResultStatsCard", PurpleFrameResource,
            new Vector2(0f, -100f), new Vector2(900f, 390f));
        BuildStatRow(
            stats.transform, "PlayerAttemptsRow", L10n.Get("you"),
            result.playerAttemptsText, new Vector2(0f, 125f), Cyan);
        BuildStatRow(
            stats.transform, "OpponentAttemptsRow",
            L10n.Get("prebattle_opponent"),
            result.opponentAttemptsText, new Vector2(0f, 35f),
            new Color(1f, 0.30f, 0.62f, 1f));
        Reparent(result.revealedNumberText.transform, stats.transform);
        Place(
            result.revealedNumberText.rectTransform,
            new Vector2(0f, -65f), new Vector2(780f, 62f));
        StyleDisplay(result.revealedNumberText, 24f, 32f, Gold);
        resultStreakText = BodyText(
            stats.transform, "PvpResultStreak", "", 26f,
            new Vector2(0f, -145f), new Vector2(780f, 52f), Muted);

        GameObject actions = Frame(
            safe, "PvpResultActions", PurpleFrameResource,
            new Vector2(0f, -500f), new Vector2(850f, 250f));
        Reparent(pvp.rematchSecretInput.transform, actions.transform);
        Place(
            (RectTransform)pvp.rematchSecretInput.transform,
            new Vector2(0f, 70f), new Vector2(700f, 70f));
        StyleInput(pvp.rematchSecretInput);

        Button rematch = pvp.rematchButton.GetComponent<Button>();
        if (rematch != null)
        {
            Reparent(rematch.transform, actions.transform);
            Place(
                (RectTransform)rematch.transform,
                new Vector2(-190f, -40f), new Vector2(340f, 84f));
            StyleButton(rematch, GoldFrameResource, Ink, 31f);
        }

        Button exit = pvp.resultExitButton.GetComponent<Button>();
        if (exit != null)
        {
            Reparent(exit.transform, actions.transform);
            Place(
                (RectTransform)exit.transform,
                new Vector2(190f, -40f), new Vector2(340f, 84f));
            StyleButton(exit, BlueFrameResource, Ink, 31f);
        }

        if (pvp.rematchStatusText != null)
        {
            Reparent(pvp.rematchStatusText.transform, actions.transform);
            Place(
                pvp.rematchStatusText.rectTransform,
                new Vector2(0f, -105f), new Vector2(740f, 38f));
            StyleBody(pvp.rematchStatusText, 18f, 23f, Muted);
        }

        if (pvp.resultSignalsRoot != null)
        {
            Reparent(pvp.resultSignalsRoot.transform, safe);
            Place(
                (RectTransform)pvp.resultSignalsRoot.transform,
                new Vector2(0f, -730f), new Vector2(900f, 120f));
            LayoutSignalButtons(pvp.resultSignalsRoot.transform);
        }
        if (pvp.resultSignalFeedText != null)
        {
            Reparent(pvp.resultSignalFeedText.transform, safe);
            Place(
                pvp.resultSignalFeedText.rectTransform,
                new Vector2(0f, -650f), new Vector2(780f, 50f));
            StyleBody(pvp.resultSignalFeedText, 19f, 25f, NearWhite);
        }

        AddSprite(
            safe, "PvpResultMascotSix", MascotSixResource,
            new Vector2(-430f, -805f), new Vector2(230f, 260f));
        AddSprite(
            safe, "PvpResultMascotSeven", MascotSevenResource,
            new Vector2(430f, -805f), new Vector2(230f, 260f));

        if (pvp.winConfetti != null)
        {
            Reparent(pvp.winConfetti.transform, root);
            pvp.winConfetti.transform.SetAsLastSibling();
        }

        HideLegacyChildren(host, root);
    }

    void BuildStatRow(
        Transform parent,
        string name,
        string label,
        TMP_Text value,
        Vector2 position,
        Color color)
    {
        GameObject row = Frame(
            parent, name, PurpleFrameResource,
            position, new Vector2(800f, 72f));
        TMP_Text caption = BodyText(
            row.transform, name + "Caption", label, 25f,
            new Vector2(-180f, 0f), new Vector2(390f, 46f), NearWhite);
        caption.alignment = TextAlignmentOptions.Left;
        Reparent(value.transform, row.transform);
        Place(
            value.rectTransform, new Vector2(270f, 0f),
            new Vector2(180f, 52f));
        StyleDisplay(value, 30f, 42f, color);
    }

    void RefreshDynamicCopy()
    {
        if (!built) return;

        string player = PlayerPrefs.GetString("PlayerName", "");
        if (string.IsNullOrWhiteSpace(player))
            player = L10n.Get("player_default");

        if (matchPlayerName != null)
            matchPlayerName.text = player;
        if (matchChipText != null)
        {
            matchChipText.text = "<b>" + player + "</b>\n<size=78%>" +
                                 L10n.Get("stats_wins") + ": " +
                                 GameStats.Wins + "</size>";
        }
        if (resultPlayerName != null)
        {
            resultPlayerName.text = "<b>" + player + "</b>\n<size=78%>" +
                                    L10n.Get("stats_wins") + ": " +
                                    GameStats.Wins + "</size>";
        }
        if (resultStreakText != null)
        {
            resultStreakText.text = L10n.Get("stats_streak") + ": " +
                                    GameStats.CurrentStreak;
        }
    }

    void BuildBackdrop(RectTransform root, string prefix)
    {
        Image background = EnsureImage(root, prefix + "Background");
        Stretch(background.rectTransform);
        ConfigureImage(
            background, BackgroundResource, false, Image.Type.Simple);
        background.raycastTarget = true;

        Image stars = EnsureImage(root, prefix + "Stars");
        Stretch(stars.rectTransform);
        ConfigureImage(stars, StarsResource, false, Image.Type.Simple);

        Image confetti = EnsureImage(root, prefix + "Confetti");
        Stretch(confetti.rectTransform);
        ConfigureImage(
            confetti, ConfettiResource, false, Image.Type.Simple);

        Image outer = EnsureImage(root, prefix + "OuterFrame");
        ConfigureImage(
            outer, PurpleFrameResource, false, Image.Type.Sliced);
        outer.pixelsPerUnitMultiplier = 2f;
        Place(outer.rectTransform, Vector2.zero, new Vector2(1032f, 1872f));
    }

    void LayoutKeypad(Transform keypad)
    {
        Button[] buttons = keypad.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            string key = button.name.Replace("Key", string.Empty);
            int index = KeyIndex(key);
            if (index < 0) continue;
            int column = index % 3;
            int row = index / 3;
            Place(
                (RectTransform)button.transform,
                new Vector2(-182f + column * 182f, 195f - row * 126f),
                new Vector2(160f, 104f));
            StyleButton(button, BlueFrameResource, NearWhite, 44f);
        }
    }

    static int KeyIndex(string key)
    {
        switch (key)
        {
            case "1": return 0;
            case "2": return 1;
            case "3": return 2;
            case "4": return 3;
            case "5": return 4;
            case "6": return 5;
            case "7": return 6;
            case "8": return 7;
            case "9": return 8;
            case "C": return 9;
            case "0": return 10;
            case "←":
            case "<": return 11;
            default: return -1;
        }
    }

    void LayoutSignalButtons(Transform root)
    {
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Place(
                (RectTransform)buttons[i].transform,
                new Vector2((i % 3 - 1) * 300f, i < 3 ? 34f : -34f),
                new Vector2(280f, 60f));
            StyleButton(buttons[i], PurpleFrameResource, NearWhite, 21f);
        }
    }

    static void StyleInput(TMP_InputField input)
    {
        if (input == null) return;
        Image image = input.GetComponent<Image>();
        if (image == null)
            image = input.gameObject.AddComponent<Image>();
        ConfigureImage(
            image, PurpleFrameResource, false, Image.Type.Sliced);
        image.pixelsPerUnitMultiplier = 2f;
        image.raycastTarget = true;
        if (input.textComponent != null)
        {
            input.textComponent.fontSize = 56f;
            input.textComponent.fontStyle = FontStyles.Bold;
            input.textComponent.color = NearWhite;
            input.textComponent.alignment = TextAlignmentOptions.Center;
        }
        TMP_Text placeholder = input.placeholder as TMP_Text;
        if (placeholder != null)
        {
            placeholder.fontSize = 28f;
            placeholder.color = Muted;
            placeholder.alignment = TextAlignmentOptions.Center;
        }
        input.shouldHideSoftKeyboard = true;
    }

    static void StyleButton(
        Button button,
        string resource,
        Color labelColor,
        float labelSize)
    {
        if (button == null) return;
        Image image = button.GetComponent<Image>();
        if (image == null)
            image = button.gameObject.AddComponent<Image>();
        ConfigureImage(image, resource, false, Image.Type.Sliced);
        image.pixelsPerUnitMultiplier = 2f;
        image.raycastTarget = true;
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.80f, 0.84f, 0.94f, 1f);
        colors.disabledColor = new Color(0.56f, 0.58f, 0.68f, 0.72f);
        colors.fadeDuration = 0.06f;
        colors.colorMultiplier = 1f;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = colors;
        RuntimeUI.AttachJuice(button);

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.gameObject.SetActive(labelSize > 0f);
            label.color = labelColor;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            if (labelSize > 0f)
            {
                label.enableAutoSizing = true;
                label.fontSizeMin = Mathf.Max(18f, labelSize - 8f);
                label.fontSizeMax = labelSize;
                label.overflowMode = TextOverflowModes.Overflow;
            }
        }
    }

    static void HideButtonLabels(Transform root)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            text.gameObject.SetActive(false);
        foreach (Text text in root.GetComponentsInChildren<Text>(true))
            text.gameObject.SetActive(false);
    }

    static void HideLegacyChildren(
        Transform parent,
        params Transform[] keep)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            bool preserve = false;
            for (int k = 0; k < keep.Length; k++)
            {
                if (keep[k] != null && child == keep[k])
                {
                    preserve = true;
                    break;
                }
            }
            if (!preserve)
                child.gameObject.SetActive(false);
        }
        foreach (Transform item in keep)
            if (item != null) item.gameObject.SetActive(item.gameObject.activeSelf);
    }

    static void DisableRootImage(GameObject root)
    {
        Image image = root != null ? root.GetComponent<Image>() : null;
        if (image == null) return;
        image.enabled = false;
        image.raycastTarget = false;
    }

    static void AttachSafeArea(RectTransform safe, Transform owner)
    {
        Canvas canvas = owner.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            ResponsiveSafeAreaRoot.Attach(
                safe, canvas.transform as RectTransform,
                new Vector2(ReferenceWidth, ReferenceHeight));
        }
    }

    GameObject Frame(
        Transform parent,
        string name,
        string resource,
        Vector2 position,
        Vector2 size)
    {
        RectTransform rect = EnsureRect(parent, name);
        Place(rect, position, size);
        Image image = rect.GetComponent<Image>();
        if (image == null)
            image = rect.gameObject.AddComponent<Image>();
        ConfigureImage(image, resource, false, Image.Type.Sliced);
        image.pixelsPerUnitMultiplier = 2f;
        return rect.gameObject;
    }

    Image AddSprite(
        Transform parent,
        string name,
        string resource,
        Vector2 position,
        Vector2 size)
    {
        RectTransform rect = EnsureRect(parent, name);
        Place(rect, position, size);
        Image image = rect.GetComponent<Image>();
        if (image == null)
            image = rect.gameObject.AddComponent<Image>();
        ConfigureImage(image, resource, true, Image.Type.Simple);
        return image;
    }

    TMP_Text DisplayText(
        Transform parent,
        string name,
        string value,
        float size,
        Vector2 position,
        Vector2 bounds,
        Color color)
    {
        TMP_Text text = EnsureText(
            parent, name, value, size, position, bounds, color);
        text.font = displayFont;
        StyleDisplay(text, Mathf.Max(18f, size - 9f), size + 1f, color);
        return text;
    }

    TMP_Text BodyText(
        Transform parent,
        string name,
        string value,
        float size,
        Vector2 position,
        Vector2 bounds,
        Color color)
    {
        TMP_Text text = EnsureText(
            parent, name, value, size, position, bounds, color);
        text.font = bodyFont;
        StyleBody(text, Mathf.Max(16f, size - 6f), size + 1f, color);
        return text;
    }

    static TMP_Text EnsureText(
        Transform parent,
        string name,
        string value,
        float size,
        Vector2 position,
        Vector2 bounds,
        Color color)
    {
        RectTransform rect = EnsureRect(parent, name);
        Place(rect, position, bounds);
        TMP_Text text = rect.GetComponent<TextMeshProUGUI>();
        if (text == null)
            text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    static void StyleDisplay(
        TMP_Text text,
        float minimum,
        float maximum,
        Color color)
    {
        if (text == null) return;
        text.color = color;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = minimum;
        text.fontSizeMax = maximum;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        Shadow shadow = text.GetComponent<Shadow>();
        if (shadow == null)
            shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.02f, 0.01f, 0.12f, 0.68f);
        shadow.effectDistance = new Vector2(2f, -3f);
        shadow.useGraphicAlpha = true;
    }

    static void StyleBody(
        TMP_Text text,
        float minimum,
        float maximum,
        Color color)
    {
        if (text == null) return;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = minimum;
        text.fontSizeMax = maximum;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
    }

    static void ConfigureImage(
        Image image,
        string resource,
        bool preserveAspect,
        Image.Type type)
    {
        Sprite sprite = Resources.Load<Sprite>(resource);
        if (sprite == null)
        {
            Debug.LogError(
                "[PvpDuelCartoonVisuals] Missing Resources/" + resource + ".");
            return;
        }
        image.enabled = true;
        image.sprite = sprite;
        image.type = type;
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    static RectTransform EnsureRect(Transform parent, string name)
    {
        Transform existing = DirectChild(parent, name);
        if (existing is RectTransform rect)
        {
            rect.gameObject.SetActive(true);
            return rect;
        }
        return (RectTransform)RuntimeUI.CreateObject(name, parent).transform;
    }

    static Image EnsureImage(Transform parent, string name)
    {
        RectTransform rect = EnsureRect(parent, name);
        Image image = rect.GetComponent<Image>();
        if (image == null)
            image = rect.gameObject.AddComponent<Image>();
        return image;
    }

    static void Reparent(Transform child, Transform parent)
    {
        if (child == null || parent == null) return;
        if (child.parent != parent)
            child.SetParent(parent, false);
        child.gameObject.SetActive(true);
        child.SetAsLastSibling();
    }

    static void Place(
        RectTransform rect,
        Vector2 position,
        Vector2 size)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    static void StretchText(
        RectTransform rect,
        float horizontal,
        float vertical)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontal, vertical);
        rect.offsetMax = new Vector2(-horizontal, -vertical);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    static Transform DirectChild(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name) return child;
        }
        return null;
    }
}
