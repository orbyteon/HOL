using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Final production construction owner for PvP match and result.
// The approved current SoloDuelVisuals composition is the visual authority.
// This owner constructs it directly, with only room-specific data and signals
// adapted. Only PvpGameController receives network state or sends actions.
[DisallowMultipleComponent]
public sealed class PvpDuelCartoonVisuals : MonoBehaviour
{
    public const string MatchRootName = "PvpDuelCartoonRoot";
    public const string ResultRootName = "PvpResultCartoonRoot";
    public const string BackgroundResource = "solo/production/solo_background_v1";
    public const string PurpleFrameResource = "phase2a/hol_tip_frame_r2_9s";
    public const string BlueFrameResource = "mainmenu/mainmenu_cta_blue_9s";
    public const string MagentaFrameResource = "phase2a/hol_cta_magenta_r2_9s";
    public const string GoldFrameResource = "phase2a/hol_cta_gold_r2_9s";
    const string PlayerCardResource = "solo/production/solo_player_card_shell_v1";
    const string OpponentCardResource = "solo/production/solo_opponent_card_shell_v1";
    const string PromptResource = "solo/production/solo_prompt_ribbon_v1";
    const string InteractionResource = "solo/production/solo_interaction_board_v2";
    const string InputResource = "solo/production/solo_input_field_v1";
    const string KeyResource = "solo/production/solo_keypad_key_v1";
    const string PrimaryResource = "solo/production/solo_primary_cta_v1";
    const string HistoryResource = "solo/production/solo_history_board_v1";
    const string TipResource = "solo/production/solo_tip_board_v1";
    const string BubbleResource = "solo/production/solo_opponent_speech_bubble_v2";
    const string ChipResource = "solo/production/solo_player_chip_v1";
    const string BackResource = "solo/production/solo_back_button_v1";
    const string TrophyResource = "solo/production/solo_trophy_v1";
    const string VsResource = "solo/production/solo_vs_burst_v2";
    const string HigherResource = "solo/production/solo_history_high_v1";
    const string LowerResource = "solo/production/solo_history_low_v1";
    const string CorrectResource = "solo/production/solo_history_correct_v1";
    const string LogoResource = "reference/hol_logo_exact";
    const string OpponentResource = "reference/opponent_purple_exact";
    static readonly Color White = new Color(0.985f, 0.975f, 1f, 1f);
    static readonly Color Ink = new Color(0.09f, 0.05f, 0.16f, 1f);
    static readonly Color Cyan = new Color(0.20f, 0.94f, 1f, 1f);
    static readonly Color Gold = new Color(1f, 0.76f, 0.10f, 1f);
    static readonly Color Muted = new Color(0.75f, 0.78f, 0.92f, 1f);
    static readonly Color DarkLabel = Ink;

    PvpGameController pvp;
    TMP_FontAsset displayFont;
    TMP_FontAsset bodyFont;
    readonly List<Image> profilePortraits = new List<Image>();
    readonly List<TMP_Text> nameLabels = new List<TMP_Text>();
    readonly List<TMP_Text> chipLabels = new List<TMP_Text>();
    TMP_Text resultStreak;
    TMP_Text playerBadgeText, opponentBadgeText, playerLastGuess, opponentLastGuess;
    TMP_Text playerWins, opponentAttempts, currentHeading, currentRange, latestOutcome;
    TMP_Text lockHelp, lockSuggestion, signalPlaceholder;
    RectTransform historyViewport, historyContent;
    ScrollRect historyScroll;
    readonly List<HistoryRow> historyRows = new List<HistoryRow>();
    readonly Dictionary<string, RectTransform> namedRects = new Dictionary<string, RectTransform>();
    readonly List<ButtonFace> buttonFaces = new List<ButtonFace>();
    GameObject signalsDrawer;
    Button signalsToggle;
    float lastWidth = -1, lastHeight = -1, tallBlend;
    int paintedHistoryMatch = -1, paintedHistoryTotal = -1;

    sealed class HistoryRow
    {
        internal Image Art, Icon;
        internal TMP_Text Meta, Number, Outcome, Newest;
    }

    sealed class ButtonFace
    {
        internal Button Button;
        internal TMP_Text Label;
        internal bool Primary;
        internal Vector2 Size;
        internal MainMenuCenteredTextRegion Region;
    }
    float nextIdentityRefresh;
    public bool IsReady { get; private set; }

    public void Build(PvpGameController controller)
    {
        if (pvp != null) return;
        pvp = controller;
        displayFont = Resources.Load<TMP_FontAsset>("phase2a/fonts/HOL Menu Display SDF");
        bodyFont = Resources.Load<TMP_FontAsset>("phase2a/fonts/HOL Menu Body SDF");
        if (displayFont == null || bodyFont == null)
        {
            Debug.LogError("[PvpDuelCartoonVisuals] Required production fonts are missing.");
            return;
        }
        BuildMatch();
        BuildResult();
        BuildTerminal();
        RefreshIdentity();
        IsReady = true;
        ApplyResponsiveLayoutForViewport(Screen.safeArea.width, Screen.safeArea.height);
    }

    void OnEnable()
    {
        L10n.OnLanguageChanged -= RefreshIdentity;
        L10n.OnLanguageChanged += RefreshIdentity;
        RefreshIdentity();
    }

    void OnDisable() { L10n.OnLanguageChanged -= RefreshIdentity; }

    void LateUpdate()
    {
        if (!IsReady) return;
        if (Time.unscaledTime >= nextIdentityRefresh)
        {
            nextIdentityRefresh = Time.unscaledTime + 0.25f;
            RefreshIdentity();
        }
        ApplyResponsiveLayoutForViewport(Screen.safeArea.width, Screen.safeArea.height);
        RefreshMatchPresentation();
        CenterButtonFaces();
    }

    void RefreshIdentity()
    {
        string player = PlayerPrefs.GetString("PlayerName", "");
        if (string.IsNullOrWhiteSpace(player)) player = L10n.Get("player_default");
        foreach (var label in nameLabels) if (label != null) label.text = player;
        foreach (var label in chipLabels)
            if (label != null) label.text = GameStats.Wins.ToString();
        if (playerWins != null) playerWins.text = GameStats.Wins.ToString();
        Sprite selected = PlayerProfileAvatarResolver.Resolve();
        foreach (var portrait in profilePortraits)
        {
            if (portrait == null) continue;
            portrait.sprite = selected;
            PlayerProfileAvatarFraming.Apply(portrait, portrait.transform.parent as RectTransform);
        }
        if (resultStreak != null)
            resultStreak.text = L10n.Get("stats_streak") + ": " + GameStats.CurrentStreak;
    }

    void BuildMatch()
    {
        var panel = CreateScreen(transform, "PvPMatchPanel", MatchRootName, out Transform safe);
        pvp.matchPanel = panel;
        AddSprite(safe, "PvpMatchLogo", LogoResource, new Vector2(-46, 835), new Vector2(390, 229)).preserveAspect = false;
        pvp.leaveButton = Back(safe, "LeaveButton", pvp.OnLeaveMatchPressed).gameObject;
        Chip(safe, "PvpMatchPlayerChip");

        var player = Frame(safe, "PvpPlayerCard", PlayerCardResource,
            new Vector2(-276, 472), new Vector2(514, 620));
        // Like Solo, the large characters are approved decorative artwork.
        // Only the masked header portrait claims the saved player identity.
        AddSprite(player.transform, "PvpPlayerCharacter", "reference/player_cyan_exact",
            new Vector2(-52, 62), new Vector2(350, 350));
        Label(player.transform, "PvpPlayerCaption", "solo_you_header", 38,
            new Vector2(0, 248), new Vector2(320, 52));
        playerBadgeText = Badge(player.transform, "PvpPlayerActiveBadge", BlueFrameResource);
        var playerName = Text(player.transform, "PvpPlayerName", "", 50,
            new Vector2(0, -126), new Vector2(420, 60));
        NameTypography(playerName);
        nameLabels.Add(playerName);
        Label(player.transform, "PvpPlayerSecretReady", "private_room_secret_ready", 25,
            new Vector2(0, -178), new Vector2(390, 42), Gold);
        playerLastGuess = Text(player.transform, "PvpPlayerLatestGuess", "", 23,
            new Vector2(0, -218), new Vector2(408, 38), White, false);
        AddSprite(player.transform, "PvpPlayerTrophy", TrophyResource,
            new Vector2(-92, -243), new Vector2(48, 48));
        playerWins = Text(player.transform, "PvpPlayerWins", "", 38,
            new Vector2(36, -243), new Vector2(144, 64), Gold);

        var opponent = Frame(safe, "PvpOpponentCard", OpponentCardResource,
            new Vector2(282, 470), new Vector2(514, 620));
        AddSprite(opponent.transform, "PvpOpponentCharacter", OpponentResource,
            new Vector2(-20, 72), new Vector2(344, 344));
        Label(opponent.transform, "PvpOpponentCaption", "prebattle_opponent", 37,
            new Vector2(0, 248), new Vector2(320, 52));
        opponentBadgeText = Badge(opponent.transform, "PvpOpponentActiveBadge", MagentaFrameResource);
        pvp.opponentNameText = Text(opponent.transform, "PvpOpponentName", "", 50,
            new Vector2(0, -126), new Vector2(420, 60));
        NameTypography(pvp.opponentNameText);
        Label(opponent.transform, "PvpOpponentSecretPrivate", "pvp_secret_private", 25,
            new Vector2(0, -178), new Vector2(390, 42), Gold);
        opponentLastGuess = Text(opponent.transform, "PvpOpponentLatestGuess", "", 23,
            new Vector2(0, -218), new Vector2(408, 38), White, false);
        opponentAttempts = Text(opponent.transform, "PvpOpponentAttempts", "", 27,
            new Vector2(0, -243), new Vector2(400, 50), Gold);
        var versus = AddSprite(safe, "PvpVsBurst", VsResource,
            new Vector2(1, 508), new Vector2(338, 290));
        Text(versus.transform, "PvpVsOutline", "VS", 80, new Vector2(0, -2), new Vector2(168, 108), Ink);
        Text(versus.transform, "PvpVersusLabel", "VS", 66, new Vector2(0, 1), new Vector2(152, 94), Gold);
        AddSprite(safe, "PvpMascotSeven", "reference/mascot_7_exact",
            new Vector2(-426, 80), new Vector2(220, 245));
        AddSprite(safe, "PvpMascotThree", "reference/mascot_3_exact",
            new Vector2(420, 80), new Vector2(225, 250));

        var ribbon = Frame(safe, "PvpPromptRibbon", PromptResource,
            new Vector2(-18, 86), new Vector2(636, 181));
        pvp.roundText = Text(ribbon.transform, "Round", "", 24,
            new Vector2(0, 52), new Vector2(552, 42));
        pvp.historyText = Text(ribbon.transform, "History", "", 30,
            new Vector2(0, 22), new Vector2(592, 44));
        latestOutcome = Text(ribbon.transform, "LatestOutcome", "", 26,
            new Vector2(0, -8), new Vector2(592, 38), Gold);
        pvp.turnText = Text(ribbon.transform, "Turn", "", 24,
            new Vector2(0, -36), new Vector2(560, 52));
        pvp.resultText = Text(ribbon.transform, "Result", "", 28,
            Vector2.zero, new Vector2(580, 150));

        var interaction = Frame(safe, "PvpInteractionCard", InteractionResource,
            new Vector2(-189, -488), new Vector2(760, 1004));
        currentHeading = Text(interaction.transform, "PvpCurrentNumberHeading", "", 30,
            new Vector2(-9, 426), new Vector2(612, 46));
        currentHeading.margin = new Vector4(8, 0, 8, 0);
        currentHeading.fontSizeMin = 26;
        currentHeading.enableWordWrapping = false;
        currentRange = Text(interaction.transform, "PvpCurrentRange", "", 24,
            new Vector2(-9, 379), new Vector2(612, 38), Gold, false);
        pvp.guessInput = Input(interaction.transform, "GuessInput", "number_placeholder",
            new Vector2(-13, 285), new Vector2(520, 140), true);
        lockSuggestion = Text(interaction.transform, "PvpLockSuggestion", "", 24,
            new Vector2(-9, 179), new Vector2(612, 56), Gold, false);
        pvp.keypadRoot = Keypad(interaction.transform, pvp.guessInput);
        var submit = Button(interaction.transform, "SubmitGuessButton", "solo_submit", PrimaryResource,
            new Vector2(-150, -385), new Vector2(276, 94), 46, Ink);
        submit.onClick.AddListener(pvp.OnSubmitGuessPressed);
        pvp.guessButton = submit.gameObject;
        var lockButton = Button(interaction.transform, "LockButton", null, BlueFrameResource,
            new Vector2(150, -385), new Vector2(276, 94), 30, Ink);
        lockButton.onClick.AddListener(pvp.OnLockTogglePressed);
        pvp.lockButton = lockButton.gameObject;
        pvp.lockButtonLabel = lockButton.GetComponentInChildren<TMP_Text>(true);

        var rail = Rect(safe, "PvpOpponentRail", new Vector2(339, -488), new Vector2(362, 901));
        var bubble = Rect(rail.transform, "PvpSignalBubble", new Vector2(-45, 350), new Vector2(315, 220));
        var bubbleArt = AddSprite(bubble.transform, "PvpSignalBubbleArtwork", BubbleResource,
            Vector2.zero, new Vector2(315, 220));
        bubbleArt.preserveAspect = false;
        bubbleArt.rectTransform.localScale = new Vector3(-1, 1, 1);
        pvp.signalFeedText = Text(bubble.transform, "SignalFeed", "", 24,
            new Vector2(0, 8), new Vector2(218, 110), Ink);
        pvp.signalFeedText.outlineWidth = 0;
        signalPlaceholder = Label(bubble.transform, "SignalPlaceholder", "pvp_signal_idle", 24,
            new Vector2(0, 8), new Vector2(218, 110), Ink);
        signalPlaceholder.outlineWidth = 0;
        AddSprite(bubble.transform, "PvpSignalOpponentMedallion", "solo/production/solo_opponent_medallion_v1",
            new Vector2(160, -20), new Vector2(130, 130));

        BuildHistory(rail.transform);
        var tip = Frame(rail.transform, "PvpTipCard", TipResource,
            new Vector2(0, -330), new Vector2(390, 260));
        Label(tip.transform, "PvpTipTitle", "hud_tip", 27,
            new Vector2(-42, 91), new Vector2(230, 42), Gold);
        AddSprite(tip.transform, "PvpTipBulb", "solo/production/solo_tip_bulb_v1",
            new Vector2(-171, 95), new Vector2(34, 50));
        pvp.rangeText = Text(tip.transform, "Range", "", 23,
            new Vector2(0, 24), new Vector2(336, 82), Cyan, false);
        lockHelp = Text(tip.transform, "PvpLockHelp", "", 23,
            new Vector2(0, -63), new Vector2(336, 92), White, false);
        // Room-only quick chat uses an explicit drawer owned here. It never
        // takes space from Solo's keypad or replaces the accepted board shell.
        BuildMatchSignals(safe, rail.transform);
        panel.SetActive(false);
    }

    TMP_Text Badge(Transform parent, string name, string resource)
    {
        var badge = Frame(parent, name, resource, new Vector2(0, 202), new Vector2(226, 46));
        var text = Text(badge.transform, name + "Label", "", 24, Vector2.zero, new Vector2(210, 38));
        text.fontSizeMin = 20;
        return text;
    }

    static void NameTypography(TMP_Text text)
    {
        text.fontSizeMin = 36;
        text.fontSizeMax = 50;
        text.enableWordWrapping = false;
        text.margin = new Vector4(8, 0, 8, 0);
    }

    void BuildHistory(Transform rail)
    {
        var history = Frame(rail, "PvpHistoryCard", HistoryResource,
            new Vector2(0, 0), new Vector2(374, 400));
        Label(history.transform, "PvpHistoryTitle", "hud_history", 30,
            new Vector2(0, 174), new Vector2(300, 50));
        AddSprite(history.transform, "HistorySparkleLeft", "solo/production/solo_title_sparkle_v1",
            new Vector2(-150, 174), new Vector2(34, 40));
        AddSprite(history.transform, "HistorySparkleRight", "solo/production/solo_title_sparkle_v1",
            new Vector2(150, 174), new Vector2(34, 40));
        historyViewport = (RectTransform)Rect(history.transform, "HistoryViewport",
            new Vector2(0, -28), new Vector2(334, 310)).transform;
        historyViewport.gameObject.AddComponent<RectMask2D>();
        var hit = historyViewport.gameObject.AddComponent<Image>();
        hit.color = Color.clear;
        hit.raycastTarget = true;
        historyContent = (RectTransform)Rect(historyViewport, "HistoryContent", Vector2.zero, new Vector2(334, 410)).transform;
        historyContent.anchorMin = new Vector2(0, 1);
        historyContent.anchorMax = Vector2.one;
        historyContent.pivot = new Vector2(.5f, 1);
        historyScroll = history.AddComponent<ScrollRect>();
        historyScroll.viewport = historyViewport;
        historyScroll.content = historyContent;
        historyScroll.horizontal = false;
        historyScroll.vertical = true;
        historyScroll.movementType = ScrollRect.MovementType.Clamped;
        historyScroll.scrollSensitivity = 38;
        pvp.historyRail = history.AddComponent<GuessHistoryRail>();
        for (int i = 0; i < GuessHistoryRail.VisibleCapacity; i++)
        {
            var row = Frame(historyContent, "PvpHistoryRow" + i, LowerResource,
                new Vector2(0, -51 - i * 104), new Vector2(326, 102));
            var item = new HistoryRow { Art = row.GetComponent<Image>() };
            item.Meta = Text(row.transform, "HistoryMeta", "", 20,
                new Vector2(-24, 30), new Vector2(256, 34), White, false);
            item.Meta.alignment = TextAlignmentOptions.Left;
            item.Number = Text(row.transform, "HistoryNumber", "", 43,
                new Vector2(-112, -13), new Vector2(78, 68), Cyan);
            item.Outcome = Text(row.transform, "HistoryOutcome", "", 24,
                new Vector2(5, -12), new Vector2(170, 60));
            item.Outcome.fontSizeMin = 20;
            item.Outcome.margin = new Vector4(4, 0, 4, 0);
            item.Newest = Text(row.transform, "HistoryNewest", "", 17,
                new Vector2(120, 30), new Vector2(58, 30), Gold, false);
            item.Icon = AddSprite(row.transform, "HistoryIcon", "solo/production/solo_history_down_icon_v1",
                new Vector2(127, -13), new Vector2(48, 48));
            historyRows.Add(item);
            row.SetActive(false);
        }
    }

    void BuildMatchSignals(Transform safe, Transform rail)
    {
        pvp.signalsRoot = Rect(rail, "Signals", Vector2.zero, new Vector2(340, 901));
        signalsToggle = Button(pvp.signalsRoot.transform, "OpenSignals", "pvp_signals", PurpleFrameResource,
            new Vector2(-30, 236), new Vector2(240, 52), 25, White);
        signalsDrawer = Rect(safe, "PvpSignalsOverlay", Vector2.zero, new Vector2(1080, 2600));
        var blocker = signalsDrawer.AddComponent<Image>();
        blocker.color = new Color(0, 0, .04f, .65f);
        blocker.raycastTarget = true; // modal input barrier, not replacement artwork
        var drawerFace = Frame(signalsDrawer.transform, "PvpSignalsDrawer", InteractionResource,
            new Vector2(0, -300), new Vector2(820, 570));
        Label(drawerFace.transform, "SignalsTitle", "pvp_signals", 34,
            new Vector2(-45, 212), new Vector2(580, 58));
        var close = Button(drawerFace.transform, "CloseSignals", "pvp_signals_close", BlueFrameResource,
            new Vector2(0, -192), new Vector2(330, 90), 30, White);
        var options = SignalsPanel(drawerFace.transform, "MatchSignalChoices", new Vector2(0, 15), 660);
        foreach (var button in options.GetComponentsInChildren<Button>())
            button.onClick.AddListener(CloseSignals);
        signalsToggle.onClick.AddListener(() =>
        {
            if (!pvp.signalsRoot.activeInHierarchy || pvp.PresentationMatchOver) return;
            signalsDrawer.SetActive(true);
            signalsDrawer.transform.SetAsLastSibling();
        });
        close.onClick.AddListener(CloseSignals);
        signalsDrawer.SetActive(false);
    }

    void CloseSignals() { if (signalsDrawer != null) signalsDrawer.SetActive(false); }

    // The controller alone decides eligibility/suggestion. Keep its full
    // localized advice visible beside the number entry, not squeezed onto a
    // small button face. The button retains the exact armed/unarmed action.
    public void PresentLockCaption(bool armed, bool suggested, int candidates)
    {
        pvp.lockButtonLabel.text = L10n.Get(armed ? "lock_armed" : "lock");
        lockSuggestion.text = suggested && !armed ? L10n.Get("lock_suggest", candidates) : "";
    }

    public void RefreshMatchPresentation()
    {
        if (pvp == null || playerBadgeText == null) return;
        var state = pvp.PresentationState;
        if (state == null || pvp.PresentationMatchOver || !pvp.matchPanel.activeInHierarchy)
        {
            CloseSignals();
            return;
        }
        if (!pvp.signalsRoot.activeInHierarchy) CloseSignals();
        signalPlaceholder.gameObject.SetActive(string.IsNullOrWhiteSpace(pvp.signalFeedText.text));
        string me = pvp.client != null && pvp.client.IsHost ? "host" : "guest";
        string opponent = me == "host" ? state.guestName : state.hostName;
        bool myTurn = state.turn == me;
        playerBadgeText.text = L10n.Get(myTurn ? "solo_player_active" : "solo_waiting");
        opponentBadgeText.text = L10n.Get(myTurn ? "solo_waiting" : "pvp_opponent_active");
        opponentAttempts.text = L10n.Get("pvp_attempt_count", state.GuessCountFor(me == "host" ? "guest" : "host"));
        currentHeading.text = L10n.Get("solo_guess_target_heading", opponent);
        currentRange.text = L10n.Get("solo_strategic_legal_range", pvp.PlayerRangeMinimum, pvp.PlayerRangeMaximum);
        var placeholder = pvp.guessInput.placeholder as TMP_Text;
        if (placeholder != null) placeholder.text = L10n.Get("solo_input_range", pvp.PlayerRangeMinimum, pvp.PlayerRangeMaximum);

        // Do not overwrite the controller's first-use LOCK explanation. Give
        // that exact copy the whole secondary face, then return to range/help.
        bool intro = pvp.rangeText.text == L10n.Get("lock_hint");
        // The useful range already has its prominent, explicit owner above
        // the number input. Do not repeat it in the small LOCK help face.
        pvp.rangeText.gameObject.SetActive(intro);
        Place(pvp.rangeText.rectTransform, new Vector2(0, -8),
            new Vector2(Mathf.Lerp(336, 300, tallBlend), Mathf.Lerp(210, 236, tallBlend)));
        lockHelp.gameObject.SetActive(!intro);
        if (!pvp.lockButton.activeSelf) lockSuggestion.text = "";
        lockHelp.text = L10n.Get(pvp.PresentationLockArmed ? "solo_lock_locked"
            : state.LockUsedBy(me) ? "solo_lock_spent_reason" : "solo_lock_available");

        int count = pvp.historyRail.EventCount;
        int latestTotal = 0;
        PvpGuessHistoryEvent latest;
        if (pvp.historyRail.TryGetEvent(0, out latest))
        {
            latestTotal = latest.authoritativeTotal;
            string actor = latest.isMine ? L10n.Get("solo_you_header") : opponent;
            string target = latest.isMine ? opponent : L10n.Get("solo_you_header");
            pvp.historyText.text = L10n.Get("pvp_latest_action", actor, latest.value);
            latestOutcome.text = L10n.Get("pvp_target_" + latest.hint, target);
        }
        else
        {
            pvp.historyText.text = L10n.Get("pvp_match_objective");
            latestOutcome.text = "";
        }
        playerLastGuess.text = opponentLastGuess.text = "";
        for (int i = 0; i < count; i++)
        {
            PvpGuessHistoryEvent item;
            if (!pvp.historyRail.TryGetEvent(i, out item)) continue;
            var label = item.isMine ? playerLastGuess : opponentLastGuess;
            if (label.text.Length == 0) label.text = L10n.Get("solo_latest_guess", item.value);
        }
        for (int i = 0; i < historyRows.Count; i++)
        {
            var row = historyRows[i];
            PvpGuessHistoryEvent item;
            bool present = i < count && pvp.historyRail.TryGetEvent(count - 1 - i, out item);
            row.Art.gameObject.SetActive(present);
            if (!present) continue;
            pvp.historyRail.TryGetEvent(count - 1 - i, out item);
            string actor = item.isMine ? L10n.Get("solo_you_header") : opponent;
            string target = item.isMine ? opponent : L10n.Get("solo_you_header");
            // # is the side's real accepted guess ordinal, not a fabricated
            // round number when a LOCK penalty skipped a turn.
            row.Meta.text = "#" + item.ordinal + " • " + actor + " > " + target;
            row.Number.text = item.value.ToString();
            row.Outcome.text = L10n.Get("solo_history_" + item.hint) + (item.locked
                ? "\n" + L10n.Get(item.hint == "correct" ? "solo_lock_success_short" : "solo_lock_failed_short") : "");
            row.Newest.text = i == count - 1 ? L10n.Get("solo_history_newest") : "";
            string art = item.hint == "higher" ? HigherResource : item.hint == "correct" ? CorrectResource : LowerResource;
            row.Art.sprite = Resources.Load<Sprite>(art);
            row.Number.color = item.hint == "higher" ? new Color(1, .15f, .6f) : item.hint == "correct" ? Gold : Cyan;
            row.Icon.sprite = Resources.Load<Sprite>("solo/production/solo_history_" +
                (item.hint == "higher" ? "up" : item.hint == "correct" ? "correct" : "down") + "_icon_v1");
        }
        if (paintedHistoryMatch != pvp.historyRail.MatchIndex || paintedHistoryTotal != latestTotal)
        {
            paintedHistoryMatch = pvp.historyRail.MatchIndex;
            paintedHistoryTotal = latestTotal;
            LayoutHistory(tallBlend);
            Canvas.ForceUpdateCanvases();
            historyScroll.verticalNormalizedPosition = 0;
            CloseSignals();
        }
    }

    public void ApplyResponsiveLayoutForViewport(float width, float height)
    {
        if (!IsReady || (width == lastWidth && height == lastHeight)) return;
        lastWidth = width; lastHeight = height;
        tallBlend = Mathf.InverseLerp(1920f / 1080f, 1600f / 757f, height / Mathf.Max(1, width));
        Layout("LeaveButton", -453, 860, 132, 129, -448, 932, 120, 120);
        Layout("PvpMatchLogo", -46, 835, 390, 229, -10, 933, 390, 229);
        Layout("PvpMatchPlayerChip", 339, 860, 370, 150, 339, 932, 310, 132);
        Layout("PvpMatchPlayerChipAvatarAperture", 116, 0, 122, 122, 97, 0, 100, 100);
        Layout("PvpPlayerCard", -276, 472, 514, 620, -259, 586, 526, 606, .9f);
        Layout("PvpOpponentCard", 282, 470, 514, 620, 266, 584, 526, 606, .9f);
        Layout("PvpVsBurst", 1, 508, 338, 290, 1, 595, 306, 263);
        Layout("PvpMascotSeven", -426, 80, 220, 245, -420, 210, 200, 223);
        Layout("PvpMascotThree", 420, 80, 225, 250, 393, 210, 190, 212);
        Layout("PvpPromptRibbon", -18, 86, 636, 181, -18, 219, 636, 181);
        Layout("PvpInteractionCard", -189, -488, 760, 1004, -175, -437, 730, 1220);
        Layout("PvpOpponentRail", 339, -488, 362, 901, 321, -437, 340, 1220);
        Layout("PvpSignalBubble", -45, 350, 315, 220, -40, 457, 315, 220);
        Layout("OpenSignals", -30, 236, 240, 52, -25, 344, 240, 52);
        // The sole extra PvP control gets a clear gap above history. This is
        // the bounded deviation from Solo's otherwise identical right rail.
        Layout("PvpHistoryCard", 0, 0, 374, 400, 0, 20, 340, 570);
        Layout("PvpTipCard", 0, -330, 390, 260, 0, -425, 342, 320);
        Layout("PvpCurrentNumberHeading", -9, 409, 560, 46, -9, 518, 560, 46);
        Layout("PvpCurrentRange", -9, 372, 612, 38, -9, 481, 612, 38);
        Layout("GuessInput", -13, 285, 520, 140, -13, 385, 520, 150);
        Layout("PvpLockSuggestion", -9, 179, 612, 56, -9, 270, 612, 56);
        Layout("Keypad", -1, -80, 577, 440, -1, -65, 577, 600);
        string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "←" };
        for (int i = 0; i < keys.Length; i++)
            Layout("Key" + keys[i], (i % 3 - 1) * 203, 165 - i / 3 * 110, 186, 108,
                (i % 3 - 1) * 203, 225 - i / 3 * 150, 186, 132);
        Layout("SubmitGuessButton", -150, -385, 276, 94, -150, -455, 276, 120);
        Layout("LockButton", 150, -385, 276, 94, 150, -455, 276, 120);
        Layout("PvpTipTitle", -42, 75, 230, 42, -30, 103, 230, 42);
        Layout("PvpTipBulb", -171, 95, 34, 50, -143, 125, 34, 50);
        Layout("PvpLockHelp", 0, -8, 336, 210, 0, -8, 300, 236);
        LayoutHistory(tallBlend);
        RefreshIdentity();
        CenterButtonFaces();
    }

    void Layout(string name, float x, float y, float w, float h,
        float tx, float ty, float tw, float th, float tallScale = 1)
    {
        RectTransform rect;
        if (!namedRects.TryGetValue(name, out rect) || rect == null) return;
        Place(rect, Vector2.Lerp(new Vector2(x, y), new Vector2(tx, ty), tallBlend),
            Vector2.Lerp(new Vector2(w, h), new Vector2(tw, th), tallBlend));
        rect.localScale = Vector3.one * Mathf.Lerp(1, tallScale, tallBlend);
        var image = rect.GetComponent<Image>();
        if (image != null) FitSliceScale(image, rect.sizeDelta);
    }

    static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    void LayoutHistory(float blend)
    {
        if (historyViewport == null) return;
        float rowHeight = Mathf.Lerp(102, 120, blend);
        float viewportWidth = Mathf.Lerp(334, 314, blend);
        Place(historyViewport, new Vector2(0, -28), new Vector2(viewportWidth, Mathf.Lerp(310, 450, blend)));
        Layout("PvpHistoryTitle", 0, 164, 300, 50, 0, 245, 280, 50);
        Layout("HistorySparkleLeft", -150, 164, 34, 40, -139, 245, 34, 40);
        Layout("HistorySparkleRight", 150, 164, 34, 40, 139, 245, 34, 40);
        historyContent.sizeDelta = new Vector2(0, Mathf.Max(historyViewport.rect.height,
            pvp.historyRail.EventCount * (rowHeight + 6)));
        for (int i = 0; i < historyRows.Count; i++)
        {
            var rect = historyRows[i].Art.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
            Place(rect, new Vector2(0, -rowHeight * .5f - i * (rowHeight + 6)),
                new Vector2(viewportWidth - 8, rowHeight));
            // Retain Solo's readable number/outcome hierarchy. Actor/target
            // identities stay separate from the large guess and result.
            historyRows[i].Meta.rectTransform.anchoredPosition = new Vector2(-42, 30);
            historyRows[i].Meta.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(230, 220, blend), 34);
            historyRows[i].Meta.enableWordWrapping = false;
            historyRows[i].Meta.fontSizeMin = 17;
        }
    }

    void CenterButtonFaces()
    {
        foreach (var face in buttonFaces)
        {
            if (face.Button == null || face.Label == null) continue;
            Vector2 size = ((RectTransform)face.Button.transform).rect.size;
            if (face.Region == null || face.Size != size)
            {
                face.Size = size;
                // Native artwork's bright face sits above its lower shadow.
                float y = size.y * (face.Primary ? .035f : .02f);
                float width = size.x * (face.Primary ? .86f : .84f);
                float height = size.y * (face.Primary ? .65f : .70f);
                face.Region = new MainMenuCenteredTextRegion(face.Label, 0, y, width, height);
            }
            face.Region.Apply();
        }
    }

    void BuildResult()
    {
        var root = CreateScreen(pvp.matchPanel.transform, "ResultVisualRoot", ResultRootName, out Transform safe);
        AddSprite(safe, "PvpResultLogo", LogoResource, new Vector2(0, 820), new Vector2(330, 165));
        Chip(safe, "PvpResultPlayerChip");
        var titleRibbon = Frame(safe, "PvpResultTitleRibbon", PurpleFrameResource,
            new Vector2(0, 650), new Vector2(900, 160));
        var title = Text(titleRibbon.transform, "ResultTitle", "", 65,
            Vector2.zero, new Vector2(790, 112));
        AddSprite(safe, "PvpResultHero", "reference/char_boy_exact",
            new Vector2(-220, 285), new Vector2(520, 540));
        var trophy = AddVectorSprite(safe, "PvpResultTrophy", "reference/board_trophy_exact",
            new Vector2(65, 345), new Vector2(250, 280));
        var opponent = Frame(safe, "PvpResultOpponentCard", MagentaFrameResource,
            new Vector2(330, 285), new Vector2(330, 420));
        AddSprite(opponent.transform, "PvpResultOpponentCharacter", OpponentResource,
            new Vector2(0, 20), new Vector2(220, 225));
        Label(opponent.transform, "PvpResultOpponentLabel", "prebattle_opponent", 25,
            new Vector2(0, 165), new Vector2(280, 42));
        var opponentName = Text(opponent.transform, "PvpResultOpponentName", "", 30,
            new Vector2(0, -100), new Vector2(280, 65));

        var stats = Frame(safe, "PvpResultStatsCard", PurpleFrameResource,
            new Vector2(0, -100), new Vector2(900, 390));
        var mine = Stat(stats.transform, "PlayerAttemptsRow", "you", 125, Cyan);
        var theirs = Stat(stats.transform, "OpponentAttemptsRow", "prebattle_opponent", 35, new Color(1, .3f, .62f));
        var revealed = Text(stats.transform, "RevealedNumber", "", 31,
            new Vector2(0, -35), new Vector2(780, 62), Gold);
        resultStreak = Text(stats.transform, "PvpResultStreak", "", 26,
            new Vector2(0, -85), new Vector2(780, 52), Muted, false);

        var actions = Frame(safe, "PvpResultActions", PurpleFrameResource,
            new Vector2(0, -480), new Vector2(850, 270));
        // This field intentionally retains the native number keyboard. Unlike
        // live guessing, no replacement keypad owns rematch secret entry.
        pvp.rematchSecretInput = Input(actions.transform, "RematchSecret", "rematch_prompt",
            new Vector2(0, 68), new Vector2(740, 80), false);
        var rematch = Button(actions.transform, "ResultConfirmRematchButton", "rematch", GoldFrameResource,
            new Vector2(-190, -35), new Vector2(350, 88), 32, Ink);
        var exit = Button(actions.transform, "ResultExitButton", "result_exit", BlueFrameResource,
            new Vector2(190, -35), new Vector2(350, 88), 32, White);
        pvp.rematchStatusText = Text(actions.transform, "ResultRematchStatus", "", 25,
            new Vector2(0, -102), new Vector2(750, 46), Muted, false);
        pvp.rematchButton = rematch.gameObject;
        pvp.resultExitButton = exit.gameObject;
        rematch.onClick.AddListener(pvp.OnRematchPressed);
        pvp.rematchSecretInput.onSubmit.AddListener(_ => pvp.OnRematchPressed());
        exit.onClick.AddListener(pvp.OnLeaveMatchPressed);
        pvp.resultSignalFeedText = Text(safe, "ResultSignalFeed", "", 25,
            new Vector2(0, -665), new Vector2(780, 64), White, false);
        pvp.resultSignalsRoot = SignalsPanel(safe, "ResultSignals", new Vector2(0, -788), 720);
        AddSprite(safe, "PvpResultMascotSix", "reference/mascot_6_exact",
            new Vector2(-455, -817), new Vector2(155, 210));
        AddSprite(safe, "PvpResultMascotSeven", "reference/mascot_7_exact",
            new Vector2(455, -817), new Vector2(155, 210));

        var result = root.AddComponent<PvpResultPresentation>();
        result.titleText = title;
        result.playerAttemptsText = mine;
        result.opponentAttemptsText = theirs;
        result.revealedNumberText = revealed;
        result.opponentNameText = opponentName;
        result.trophy = trophy.gameObject;
        pvp.resultPresentation = result;
        var confettiRoot = Rect(root.transform, "ResultConfettiLayer", Vector2.zero, new Vector2(10, 10));
        pvp.winConfetti = confettiRoot.AddComponent<ConfettiBurst>();
        rematch.gameObject.SetActive(false);
        pvp.rematchSecretInput.gameObject.SetActive(false);
        pvp.resultSignalsRoot.SetActive(false);
        root.SetActive(false);
    }

    TMP_Text Stat(Transform parent, string name, string captionKey, float y, Color color)
    {
        var row = Frame(parent, name, PurpleFrameResource, new Vector2(0, y), new Vector2(800, 72));
        Label(row.transform, name + "Caption", captionKey, 28,
            new Vector2(-140, 0), new Vector2(470, 48));
        return Text(row.transform, name + "Value", "", 40,
            new Vector2(275, 0), new Vector2(175, 54), color);
    }

    void BuildTerminal()
    {
        var root = CreateScreen(pvp.matchPanel.transform, "PvpTerminalRoot", "PvpTerminalVisuals", out Transform safe);
        AddSprite(safe, "TerminalLogo", LogoResource, new Vector2(0, 590), new Vector2(480, 235));
        var card = Frame(safe, "TerminalCard", PurpleFrameResource, Vector2.zero, new Vector2(900, 650));
        var title = Text(card.transform, "Title", "", 48,
            new Vector2(0, 200), new Vector2(790, 115), Gold);
        var message = Text(card.transform, "Message", "", 34,
            new Vector2(0, 10), new Vector2(765, 210), White, false);
        var exit = Button(card.transform, "TerminalExitButton", "result_exit", BlueFrameResource,
            new Vector2(0, -208), new Vector2(450, 108), 37, White);
        exit.onClick.AddListener(pvp.OnLeaveMatchPressed);
        var terminal = gameObject.AddComponent<PvpTerminalPresentation>();
        terminal.terminalRoot = root;
        terminal.titleText = title;
        terminal.messageText = message;
        terminal.resultStatusText = pvp.rematchStatusText;
        terminal.terminalExitButton = exit.gameObject;
        terminal.resultExitButton = pvp.resultExitButton;
        pvp.terminalPresentation = terminal;
        root.SetActive(false);
    }

    GameObject Keypad(Transform parent, TMP_InputField input)
    {
        var root = Rect(parent, "Keypad", new Vector2(-1, -80), new Vector2(577, 440));
        string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "←" };
        for (int i = 0; i < keys.Length; i++)
        {
            string key = keys[i];
            var button = Button(root.transform, "Key" + key, null, KeyResource,
                new Vector2((i % 3 - 1) * 203, 165 - (i / 3) * 110), new Vector2(186, 108), 76, White);
            var label = button.GetComponentInChildren<TMP_Text>();
            label.text = key;
            if (key == "C" || key == "←")
            {
                label.gameObject.SetActive(false);
                AddSprite(button.transform, "KeyActionIcon", key == "C"
                    ? "solo/production/solo_key_clear_icon_v1" : "solo/production/solo_key_backspace_icon_v1",
                    Vector2.zero, new Vector2(96, 68));
            }
            button.onClick.AddListener(() =>
            {
                if (!input.IsInteractable() || !root.activeInHierarchy) return;
                string value = input.text ?? "";
                input.text = key == "C" ? "" : key == "←"
                    ? (value.Length > 0 ? value.Substring(0, value.Length - 1) : "")
                    : value.Length < 3 ? value + key : value;
            });
        }
        return root;
    }

    GameObject SignalsPanel(Transform parent, string name, Vector2 position, float width = 960)
    {
        var root = Rect(parent, name, position, new Vector2(width, 164));
        for (int i = 0; i < Signals.Count; i++)
        {
            int id = i;
            var button = Button(root.transform, name == "ResultSignals" ? "ResultSignal" + i : "Signal" + i,
                Signals.Key(i), PurpleFrameResource,
                new Vector2((i % 3 - 1) * width / 3, i < 3 ? 43 : -43),
                new Vector2(width / 3 - 14, 78), 23, White);
            button.onClick.AddListener(() => pvp.OnSignalPressed(id));
        }
        return root;
    }

    void Chip(Transform parent, string name)
    {
        var chip = Frame(parent, name, ChipResource,
            new Vector2(339, 860), new Vector2(370, 150));
        Profile(chip.transform, name + "Avatar", new Vector2(116, 0), new Vector2(122, 122));
        AddSprite(chip.transform, name + "Trophy", TrophyResource, new Vector2(-102, 0), new Vector2(52, 52));
        chipLabels.Add(Text(chip.transform, name + "Text", "", 36,
            new Vector2(-18, 0), new Vector2(172, 72)));
    }

    Image Profile(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var aperture = AddSprite(parent, name + "Aperture",
            PlayerProfileAvatarResolver.CircularApertureResourcePath, position, size);
        aperture.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        var portrait = AddSprite(aperture.transform, name,
            PlayerProfileAvatarResolver.FallbackResourcePath, Vector2.zero, size);
        portrait.sprite = PlayerProfileAvatarResolver.Resolve();
        profilePortraits.Add(portrait);
        PlayerProfileAvatarFraming.Apply(portrait, aperture.rectTransform);
        return portrait;
    }

    Button Back(Transform parent, string name, UnityEngine.Events.UnityAction action)
    {
        var button = Button(parent, name, null, BackResource,
            new Vector2(-453, 860), new Vector2(132, 129), 28, White);
        button.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);
        button.GetComponent<Image>().preserveAspect = true;
        button.onClick.AddListener(action);
        return button;
    }

    GameObject CreateScreen(Transform parent, string name, string visualName, out Transform safe)
    {
        var panel = RuntimeUI.CreateObject(name, parent);
        RuntimeUI.Stretch(panel);
        var visual = RuntimeUI.CreateObject(visualName, panel.transform);
        RuntimeUI.Stretch(visual);
        var background = AddSprite(visual.transform, visualName + "Background", BackgroundResource,
            Vector2.zero, new Vector2(1080, 1920));
        RuntimeUI.Stretch(background.gameObject);
        background.preserveAspect = false; // identical to Solo's full-bleed background
        background.raycastTarget = true;
        var safeObject = RuntimeUI.CreateObject(visualName + "SafeRoot", visual.transform);
        RuntimeUI.Stretch(safeObject);
        var canvas = panel.GetComponentInParent<Canvas>();
        if (canvas != null) ResponsiveSafeAreaRoot.Attach(
            safeObject.transform as RectTransform, canvas.transform as RectTransform, new Vector2(1080, 1920));
        safe = safeObject.transform;
        return panel;
    }

    GameObject Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var go = RuntimeUI.CreateObject(name, parent);
        RuntimeUI.Center(go, position, size);
        namedRects[name] = (RectTransform)go.transform;
        return go;
    }

    Image AddSprite(Transform parent, string name, string resource, Vector2 position, Vector2 size)
    {
        var image = Rect(parent, name, position, size).AddComponent<Image>();
        RuntimeUI.ApplyProductionSprite(image, resource, Image.Type.Simple, true);
        image.raycastTarget = false;
        return image;
    }

    Unity.VectorGraphics.SVGImage AddVectorSprite(Transform parent, string name,
        string resource, Vector2 position, Vector2 size)
    {
        // VectorSprite imports carry their artwork in the tessellated mesh
        // and vertex colors, not a rectangular raster texture. Render the
        // original mesh with Unity's vector UI component, never a lookalike.
        var image = Rect(parent, name, position, size).AddComponent<Unity.VectorGraphics.SVGImage>();
        image.sprite = Resources.Load<Sprite>(resource);
        if (image.sprite == null) Debug.LogError("[PvpDuelCartoonVisuals] Missing vector sprite: " + resource);
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    GameObject Frame(Transform parent, string name, string resource, Vector2 position, Vector2 size)
    {
        var image = AddSprite(parent, name, resource, position, size);
        image.type = resource.Contains("_9s") ? Image.Type.Sliced : Image.Type.Simple;
        image.preserveAspect = false;
        FitSliceScale(image, size);
        return image.gameObject;
    }

    static void FitSliceScale(Image image, Vector2 size)
    {
        if (image.sprite == null || image.type != Image.Type.Sliced) return;
        // A 2K button's native borders cannot fit in a 100px control at PPU=2:
        // Unity compresses both borders and collapses the central button face.
        // Scale its fixed corners to the actual authored control size first.
        image.pixelsPerUnitMultiplier = Mathf.Max(2f,
            image.sprite.rect.width / size.x, image.sprite.rect.height / size.y);
    }

    TMP_Text Text(Transform parent, string name, string value, float size,
        Vector2 position, Vector2 bounds, Color? color = null, bool display = true)
    {
        var label = Rect(parent, name, position, bounds).AddComponent<TextMeshProUGUI>();
        label.font = display ? displayFont : bodyFont;
        label.text = value;
        label.fontSize = size;
        // Pin this screen's readable type scale through shared safe-area
        // refreshes; never silently turn overflowing copy into ellipsis.
        label.enableAutoSizing = true;
        label.fontSizeMin = label.fontSizeMax = size;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Overflow;
        label.color = color ?? White;
        if (display)
        {
            label.fontStyle = FontStyles.Bold;
            label.outlineColor = Ink;
            label.outlineWidth = label.color == Ink ? 0 : .16f;
            var shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(.02f, .01f, .12f, .68f);
            shadow.effectDistance = new Vector2(3, -4);
            shadow.useGraphicAlpha = true;
        }
        label.alignment = TextAlignmentOptions.Midline;
        label.raycastTarget = false;
        label.richText = false; // remote names are text, never TMP markup
        return label;
    }

    TMP_Text Label(Transform parent, string name, string key, float size,
        Vector2 position, Vector2 bounds, Color? color = null)
    {
        var label = Text(parent, name, L10n.Get(key), size, position, bounds, color);
        RuntimeUI.Localize(label, key);
        return label;
    }

    Button Button(Transform parent, string name, string key, string resource,
        Vector2 position, Vector2 size, float fontSize, Color textColor)
    {
        var root = Frame(parent, name, resource, position, size);
        var image = root.GetComponent<Image>();
        image.raycastTarget = true;
        var button = root.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white;
        colors.pressedColor = new Color(.8f, .84f, .94f, 1);
        colors.disabledColor = new Color(.56f, .58f, .68f, .8f);
        colors.fadeDuration = .06f;
        button.colors = colors;
        var text = Text(root.transform, "Label", key == null ? "" : L10n.Get(key), fontSize,
            Vector2.zero, size - new Vector2(32, 20), textColor);
        if (key != null) RuntimeUI.Localize(text, key);
        buttonFaces.Add(new ButtonFace { Button = button, Label = text, Primary = resource == PrimaryResource });
        RuntimeUI.AttachJuice(button);
        return button;
    }

    TMP_InputField Input(Transform parent, string name, string key, Vector2 position,
        Vector2 size, bool keypadOnly, int limit = 3,
        TMP_InputField.ContentType type = TMP_InputField.ContentType.IntegerNumber)
    {
        var field = RuntimeUI.CreateInputField(parent, name, L10n.Get(key), position, size, limit, type);
        RuntimeUI.ApplyProductionSprite(field.GetComponent<Image>(), InputResource, Image.Type.Simple, false, 2);
        FitSliceScale(field.GetComponent<Image>(), size);
        field.shouldHideSoftKeyboard = keypadOnly;
        field.shouldHideMobileInput = keypadOnly;
        field.keyboardType = type == TMP_InputField.ContentType.IntegerNumber
            ? TouchScreenKeyboardType.NumberPad : TouchScreenKeyboardType.Default;
        field.textComponent.font = displayFont;
        field.textComponent.fontSize = keypadOnly ? 64 : 44;
        field.textComponent.fontStyle = FontStyles.Bold;
        field.textComponent.enableAutoSizing = true;
        field.textComponent.fontSizeMin = field.textComponent.fontSizeMax = keypadOnly ? 64 : 44;
        field.textComponent.overflowMode = TextOverflowModes.Overflow;
        field.textComponent.color = White;
        field.textComponent.alignment = TextAlignmentOptions.Midline;
        field.textComponent.richText = false;
        var placeholder = field.placeholder as TMP_Text;
        if (placeholder != null)
        {
            placeholder.font = keypadOnly ? displayFont : bodyFont;
            placeholder.fontStyle = keypadOnly ? FontStyles.Bold : FontStyles.Normal;
            placeholder.fontSize = keypadOnly ? 68 : 27;
            placeholder.enableAutoSizing = true;
            placeholder.fontSizeMin = keypadOnly ? 40 : 27;
            placeholder.fontSizeMax = keypadOnly ? 68 : 27;
            placeholder.overflowMode = TextOverflowModes.Overflow;
            placeholder.color = Muted;
            placeholder.alignment = TextAlignmentOptions.Midline;
        }
        if (!keypadOnly) RuntimeUI.LocalizePlaceholder(field, key);
        namedRects[name] = (RectTransform)field.transform;
        return field;
    }
}
