using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Final production construction owner for PvP match and result.
// Coordinates are adapted from PR #66 at 1080x1920, not a late reskin of
// PvpRuntimeUI. Only PvpGameController receives network state or sends actions.
[DisallowMultipleComponent]
public sealed class PvpDuelCartoonVisuals : MonoBehaviour
{
    public const string MatchRootName = "PvpDuelCartoonRoot";
    public const string ResultRootName = "PvpResultCartoonRoot";
    public const string BackgroundResource = "phase2a/hol_neon_reference_bg_r3";
    public const string PurpleFrameResource = "phase2a/hol_tip_frame_r2_9s";
    public const string BlueFrameResource = "phase2a/hol_cta_blue_r2_9s";
    public const string MagentaFrameResource = "phase2a/hol_cta_magenta_r2_9s";
    public const string GoldFrameResource = "phase2a/hol_cta_gold_r2_9s";
    const string OuterFrameResource = "mainmenu/mainmenu_outer_frame_reference_v1";
    const string LogoResource = "reference/hol_logo_exact";
    const string OpponentResource = "reference/opponent_purple_exact";
    static readonly Color White = new Color(0.985f, 0.975f, 1f, 1f);
    static readonly Color Ink = new Color(0.08f, 0.04f, 0.17f, 1f);
    static readonly Color Cyan = new Color(0.20f, 0.92f, 1f, 1f);
    static readonly Color Gold = new Color(1f, 0.82f, 0.22f, 1f);
    static readonly Color Muted = new Color(0.90f, 0.86f, 0.97f, 1f);
    static readonly Color DarkLabel = Ink;

    PvpGameController pvp;
    TMP_FontAsset displayFont;
    TMP_FontAsset bodyFont;
    readonly List<Image> profilePortraits = new List<Image>();
    readonly List<TMP_Text> nameLabels = new List<TMP_Text>();
    readonly List<TMP_Text> chipLabels = new List<TMP_Text>();
    TMP_Text resultStreak;
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
        if (!IsReady || Time.unscaledTime < nextIdentityRefresh) return;
        nextIdentityRefresh = Time.unscaledTime + 0.25f;
        RefreshIdentity();
    }

    void RefreshIdentity()
    {
        string player = PlayerPrefs.GetString("PlayerName", "");
        if (string.IsNullOrWhiteSpace(player)) player = L10n.Get("player_default");
        foreach (var label in nameLabels) if (label != null) label.text = player;
        foreach (var label in chipLabels)
            if (label != null) label.text = player + "\n" + L10n.Get("stats_streak") + " " + GameStats.CurrentStreak;
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
        AddSprite(safe, "PvpMatchLogo", LogoResource, new Vector2(0, 835), new Vector2(310, 155));
        var back = Back(safe, "LeaveButton", pvp.OnLeaveMatchPressed);
        pvp.leaveButton = back.gameObject;
        Chip(safe, "PvpMatchPlayerChip");

        var player = Frame(safe, "PvpPlayerCard", BlueFrameResource,
            new Vector2(-270, 610), new Vector2(470, 345));
        Profile(player.transform, "PvpPlayerCharacter", new Vector2(0, 25), new Vector2(170, 170));
        Label(player.transform, "PvpPlayerCaption", "you", 27, new Vector2(0, 118), new Vector2(390, 44));
        nameLabels.Add(Text(player.transform, "PvpPlayerName", "", 36,
            new Vector2(0, -70), new Vector2(410, 60)));
        var opponent = Frame(safe, "PvpOpponentCard", MagentaFrameResource,
            new Vector2(270, 610), new Vector2(470, 345));
        // Approved generic opponent artwork is decorative, not a fabricated
        // saved avatar. The current room protocol supplies names, not portraits.
        AddSprite(opponent.transform, "PvpOpponentCharacter", OpponentResource,
            new Vector2(0, 25), new Vector2(190, 160));
        Label(opponent.transform, "PvpOpponentCaption", "prebattle_opponent", 25,
            new Vector2(0, 118), new Vector2(400, 44));
        pvp.opponentNameText = Text(opponent.transform, "PvpOpponentName", "", 30,
            new Vector2(0, -70), new Vector2(410, 60));
        var versus = AddVectorSprite(safe, "PvpVsBurst", "reference/board_vs_burst_exact",
            new Vector2(0, 605), new Vector2(190, 190));
        Label(versus.transform, "PvpVersusLabel", "versus", 50, Vector2.zero, new Vector2(110, 80), Ink);

        var ribbon = Frame(safe, "PvpPromptRibbon", PurpleFrameResource,
            new Vector2(0, 370), new Vector2(900, 150));
        pvp.roundText = Text(ribbon.transform, "Round", "", 29,
            new Vector2(0, 32), new Vector2(760, 42));
        pvp.turnText = Text(ribbon.transform, "Turn", "", 35,
            new Vector2(0, -12), new Vector2(820, 82));
        pvp.resultText = Text(ribbon.transform, "Result", "", 34,
            Vector2.zero, new Vector2(820, 138));

        var interaction = Frame(safe, "PvpInteractionCard", PurpleFrameResource,
            new Vector2(-225, -255), new Vector2(610, 940));
        Label(interaction.transform, "PvpCurrentNumberHeading", "hud_current_number", 30,
            new Vector2(0, 405), new Vector2(520, 48));
        pvp.guessInput = Input(interaction.transform, "GuessInput", "number_placeholder",
            new Vector2(0, 315), new Vector2(500, 125), true);
        pvp.keypadRoot = Keypad(interaction.transform, pvp.guessInput);
        var lockButton = Button(interaction.transform, "LockButton", null, BlueFrameResource,
            new Vector2(-137, -399), new Vector2(250, 100), 26, White);
        lockButton.onClick.AddListener(pvp.OnLockTogglePressed);
        pvp.lockButton = lockButton.gameObject;
        pvp.lockButtonLabel = lockButton.GetComponentInChildren<TMP_Text>(true);
        var submit = Button(interaction.transform, "SubmitGuessButton", "pvp_guess", GoldFrameResource,
            new Vector2(137, -399), new Vector2(250, 100), 34, Ink);
        submit.onClick.AddListener(pvp.OnSubmitGuessPressed);
        pvp.guessButton = submit.gameObject;

        var rail = Frame(safe, "PvpOpponentRail", PurpleFrameResource,
            new Vector2(330, -255), new Vector2(350, 940));
        // The committed raster is the approved export of this gradient SVG.
        // A plain UI material cannot evaluate the SVG gradient atlas.
        var bubble = AddSprite(rail.transform, "PvpSignalBubble", "cartoon/cartoon_speech_bubble_raster",
            new Vector2(0, 340), new Vector2(310, 200)).gameObject;
        pvp.signalFeedText = Text(bubble.transform, "SignalFeed", "", 25,
            new Vector2(-18, 18), new Vector2(232, 140), Ink, false);
        var history = Frame(rail.transform, "PvpHistoryCard", PurpleFrameResource,
            new Vector2(0, 40), new Vector2(310, 370));
        Label(history.transform, "PvpHistoryTitle", "hud_history", 27,
            new Vector2(0, 155), new Vector2(270, 46));
        pvp.historyText = Text(history.transform, "History", "", 24,
            new Vector2(0, 35), new Vector2(266, 170), White, false);
        var scrollObject = Rect(history.transform, "HistoryScroll", new Vector2(0, -123), new Vector2(268, 90));
        var scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        var viewport = Rect(scrollObject.transform, "Viewport", Vector2.zero, new Vector2(268, 90));
        viewport.AddComponent<RectMask2D>();
        var hit = viewport.AddComponent<Image>();
        hit.color = Color.clear; // transparent input surface, never a substitute for artwork
        var earlier = Text(viewport.transform, "HistoryRail", "", 23,
            Vector2.zero, new Vector2(268, 90), Muted, false);
        earlier.alignment = TextAlignmentOptions.Top;
        earlier.rectTransform.anchorMin = new Vector2(0, 1);
        earlier.rectTransform.anchorMax = Vector2.one;
        earlier.rectTransform.pivot = new Vector2(0.5f, 1);
        earlier.rectTransform.sizeDelta = new Vector2(0, 90);
        earlier.rectTransform.anchoredPosition = Vector2.zero;
        earlier.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport.transform as RectTransform;
        scroll.content = earlier.rectTransform;
        var historyOwner = history.AddComponent<GuessHistoryRail>();
        historyOwner.source = pvp.historyText;
        historyOwner.target = earlier;
        pvp.historyRail = historyOwner;

        var tip = Frame(rail.transform, "PvpTipCard", PurpleFrameResource,
            new Vector2(0, -310), new Vector2(310, 300));
        Label(tip.transform, "PvpTipTitle", "hud_tip", 27,
            new Vector2(0, 115), new Vector2(260, 42), Gold);
        pvp.rangeText = Text(tip.transform, "Range", "", 25,
            new Vector2(0, -24), new Vector2(266, 208), Cyan, false);
        pvp.signalsRoot = SignalsPanel(safe, "Signals", new Vector2(0, -811));
        panel.SetActive(false);
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
        var root = Rect(parent, "Keypad", new Vector2(0, -28), new Vector2(560, 540));
        string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "←" };
        for (int i = 0; i < keys.Length; i++)
        {
            string key = keys[i];
            var button = Button(root.transform, "Key" + key, null, BlueFrameResource,
                new Vector2((i % 3 - 1) * 182, 195 - (i / 3) * 126), new Vector2(160, 104), 44, White);
            button.GetComponentInChildren<TMP_Text>().text = key;
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
            var button = Button(root.transform, name == "Signals" ? "Signal" + i : "ResultSignal" + i,
                Signals.Key(i), PurpleFrameResource,
                new Vector2((i % 3 - 1) * width / 3, i < 3 ? 43 : -43),
                new Vector2(width / 3 - 14, 78), 23, White);
            button.onClick.AddListener(() => pvp.OnSignalPressed(id));
        }
        return root;
    }

    void Chip(Transform parent, string name)
    {
        var chip = Frame(parent, name, "phase2a/hol_player_chip_r2_9s",
            new Vector2(350, 842), new Vector2(365, 118));
        Profile(chip.transform, name + "Avatar", new Vector2(-126, 0), new Vector2(94, 94));
        chipLabels.Add(Text(chip.transform, name + "Text", "", 25,
            new Vector2(48, 0), new Vector2(225, 90), White, false));
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
        var button = Button(parent, name, null, PurpleFrameResource,
            new Vector2(-478, 842), new Vector2(96, 96), 28, White);
        button.GetComponentInChildren<TMP_Text>().text = "";
        var icon = AddSprite(button.transform, name + "Icon", "phase2a/hol_chevron_r2",
            Vector2.zero, new Vector2(46, 58));
        icon.rectTransform.localScale = new Vector3(-1, 1, 1);
        button.onClick.AddListener(action);
        return button;
    }

    GameObject CreateScreen(Transform parent, string name, string visualName, out Transform safe)
    {
        var panel = RuntimeUI.CreateObject(name, parent);
        RuntimeUI.Stretch(panel);
        var visual = RuntimeUI.CreateObject(visualName, panel.transform);
        RuntimeUI.Stretch(visual);
        foreach (var layer in new[] {
            new[] { "Background", BackgroundResource },
            new[] { "Stars", "mainmenu/mainmenu_deco_stars" },
            new[] { "Confetti", "mainmenu/mainmenu_deco_confetti" } })
        {
            var image = AddSprite(visual.transform, visualName + layer[0], layer[1], Vector2.zero, new Vector2(1080, 1920));
            image.raycastTarget = layer[0] == "Background";
            var aspect = image.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio = 1080f / 1920f;
        }
        var outer = AddSprite(visual.transform, visualName + "OuterFrame", OuterFrameResource,
            Vector2.zero, new Vector2(1032, 1872));
        var safeObject = RuntimeUI.CreateObject(visualName + "SafeRoot", visual.transform);
        RuntimeUI.Stretch(safeObject);
        var canvas = panel.GetComponentInParent<Canvas>();
        if (canvas != null) ResponsiveSafeAreaRoot.Attach(
            safeObject.transform as RectTransform, canvas.transform as RectTransform, new Vector2(1080, 1920));
        safe = safeObject.transform;
        outer.transform.SetParent(safe, false);
        return panel;
    }

    static GameObject Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var go = RuntimeUI.CreateObject(name, parent);
        RuntimeUI.Center(go, position, size);
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
        RuntimeUI.AttachJuice(button);
        return button;
    }

    TMP_InputField Input(Transform parent, string name, string key, Vector2 position,
        Vector2 size, bool keypadOnly, int limit = 3,
        TMP_InputField.ContentType type = TMP_InputField.ContentType.IntegerNumber)
    {
        var field = RuntimeUI.CreateInputField(parent, name, L10n.Get(key), position, size, limit, type);
        RuntimeUI.ApplyProductionSprite(field.GetComponent<Image>(), PurpleFrameResource, Image.Type.Sliced, false, 2);
        FitSliceScale(field.GetComponent<Image>(), size);
        field.shouldHideSoftKeyboard = keypadOnly;
        field.shouldHideMobileInput = keypadOnly;
        field.keyboardType = type == TMP_InputField.ContentType.IntegerNumber
            ? TouchScreenKeyboardType.NumberPad : TouchScreenKeyboardType.Default;
        field.textComponent.font = displayFont;
        field.textComponent.fontSize = 44;
        field.textComponent.enableAutoSizing = true;
        field.textComponent.fontSizeMin = field.textComponent.fontSizeMax = 44;
        field.textComponent.overflowMode = TextOverflowModes.Overflow;
        field.textComponent.color = White;
        field.textComponent.alignment = TextAlignmentOptions.Midline;
        field.textComponent.richText = false;
        var placeholder = field.placeholder as TMP_Text;
        if (placeholder != null)
        {
            placeholder.font = bodyFont;
            placeholder.fontSize = 27;
            placeholder.enableAutoSizing = true;
            placeholder.fontSizeMin = placeholder.fontSizeMax = 27;
            placeholder.overflowMode = TextOverflowModes.Overflow;
            placeholder.color = Muted;
            placeholder.alignment = TextAlignmentOptions.Midline;
        }
        RuntimeUI.LocalizePlaceholder(field, key);
        return field;
    }
}
