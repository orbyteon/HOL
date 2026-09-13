using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Sole construction/presentation owner for landing, Create, Join and Waiting.
// PvpRuntimeUI wires the real controls; PvpGameController owns every request,
// validation result and room transition. No artwork contains localized text.
[DisallowMultipleComponent]
[DefaultExecutionOrder(2600)]
public sealed class PrivateRoomVisuals : MonoBehaviour
{
    public const string VisualRootName = "PrivateRoomVisualRoot";
    public const string SafeRootName = "PrivateRoomSafeRoot";
    public const string BackgroundResource = "solo/production/solo_background_v1";
    const string LogoResource = "reference/hol_logo_exact";
    const string CreateCardResource = "reference/hol_private_create_card_v1";
    const string JoinCardResource = "reference/hol_private_join_card_v1";
    const string CreateActionResource = "phase2a/hol_cta_blue_r2_9s";
    const string BoardResource = "solo/production/solo_interaction_board_v2";
    const string RibbonResource = "solo/production/solo_prompt_ribbon_v1";
    const string PrimaryResource = "solo/production/solo_primary_cta_v1";
    const string InputResource = "solo/production/solo_input_field_v1";
    const string ChipResource = "solo/production/solo_player_chip_v1";
    const string BackResource = "solo/production/solo_back_button_v1";
    const string PurpleResource = "phase2a/hol_tip_frame_r2_9s";
    const string DisplayFontResource = "phase2a/fonts/HOL Menu Display SDF";
    const string BodyFontResource = "phase2a/fonts/HOL Menu Body SDF";
    static readonly Color White = new Color(.985f, .975f, 1f, 1f);
    static readonly Color Cyan = new Color(.20f, .94f, 1f, 1f);
    static readonly Color Muted = new Color(.75f, .78f, .92f, 1f);
    static readonly Color Ink = new Color(.09f, .05f, .16f, 1f);
    static readonly Color Gold = new Color(1f, .82f, .22f, 1f);

    readonly List<MainMenuCenteredTextRegion> centered = new List<MainMenuCenteredTextRegion>();
    readonly List<TMP_Text> names = new List<TMP_Text>();
    readonly List<TMP_Text> streaks = new List<TMP_Text>();
    readonly List<Image> portraits = new List<Image>();
    readonly List<PrebattleParts> forms = new List<PrebattleParts>();
    readonly List<HeaderParts> headers = new List<HeaderParts>();
    sealed class HeaderParts
    {
        internal RectTransform safe, chip, back, step, logo, ribbon;
        internal float height = -1f;
        internal string prefix;
    }
    PvpGameController pvp;
    TMP_FontAsset displayFont, bodyFont;
    TMP_InputField landingCodeInput;
    bool built, resourcesReady;
    float nextIdentityRefresh;
    public bool IsReady { get; private set; }

    public sealed class PrebattleParts
    {
        public GameObject panel, entryRoot, waitingRoot, confirm;
        public TMP_InputField secret, codeInput;
        public TMP_Text codeText, entryStatus, opponentStatus, status;
        public Button copy, share, back;
        internal bool createMode;
        internal TMP_Text playerName, opponentName;
        internal Image playerPortrait, opponentPortrait;
    }

    IEnumerator Start()
    {
        for (int frame = 0; frame < 240 && !built; frame++)
        {
            var controller = GetComponent<PvpGameController>();
            if (controller != null && controller.pvpMenuPanel != null &&
                controller.createPanel != null && controller.joinPanel != null)
            {
                Build(controller);
                yield break;
            }
            yield return null;
        }
        if (!built) Debug.LogError("[PrivateRoomVisuals] Real controller controls not ready.");
    }

    void OnEnable()
    {
        L10n.OnLanguageChanged -= RefreshCopy;
        L10n.OnLanguageChanged += RefreshCopy;
    }

    void OnDisable() { L10n.OnLanguageChanged -= RefreshCopy; }

    void LateUpdate()
    {
        if (!built) return;
        ApplyResponsiveLayout();
        if (Time.unscaledTime >= nextIdentityRefresh)
        {
            nextIdentityRefresh = Time.unscaledTime + .25f;
            RefreshIdentity();
        }
        RefreshAvailability();
        // The screen owner uses the existing pure glyph-geometry helper. It
        // never changes a font or another screen's layout to achieve centering.
        foreach (var region in centered) region.Apply();
    }

    void EnsureResources()
    {
        if (displayFont != null && bodyFont != null) return;
        displayFont = Resources.Load<TMP_FontAsset>(DisplayFontResource);
        bodyFont = Resources.Load<TMP_FontAsset>(BodyFontResource);
        resourcesReady = displayFont != null && bodyFont != null;
        foreach (string resource in new[] { BackgroundResource, LogoResource, CreateCardResource,
            JoinCardResource, CreateActionResource, BoardResource, RibbonResource, PrimaryResource, InputResource,
            ChipResource, BackResource, PurpleResource,
            "reference/mascot_6_exact", "reference/mascot_7_exact",
            "mainmenu/mainmenu_icon_streak", PlayerProfileAvatarResolver.CircularApertureResourcePath })
            resourcesReady &= Resources.Load<Sprite>(resource) != null;
        if (!resourcesReady) Debug.LogError("[PrivateRoomVisuals] Required Solo production artwork/font missing.");
    }

    public void Build(PvpGameController controller)
    {
        if (built) return;
        pvp = controller;
        EnsureResources();
        if (!resourcesReady || pvp == null || pvp.pvpMenuPanel == null) return;
        Transform panel = pvp.pvpMenuPanel.transform;
        var create = Find(panel, "CreateButton").GetComponent<Button>();
        var join = Find(panel, "JoinButton").GetComponent<Button>();
        var back = Find(panel, "BackButton").GetComponent<Button>();
        var oldBackground = panel.GetComponent<Image>();
        if (oldBackground != null) oldBackground.enabled = false;
        Transform safe = ScreenShell(pvp.pvpMenuPanel, VisualRootName, SafeRootName);
        Header(safe, "PrivateRoom", back);
        Title(safe, "PrivateRoom", "private_room_title");

        // Exact recovered artwork contains the high-five pair / illustrated
        // pink door on the left, and an empty live-content face on the right.
        // Preserve each PNG's native aspect; never stretch or 9-slice characters.
        var createCard = Sprite(safe, "PrivateRoomCreateCard", CreateCardResource,
            new Vector2(0, 173), new Vector2(960, 442.5743f), true);
        var joinCard = Sprite(safe, "PrivateRoomJoinCard", JoinCardResource,
            new Vector2(0, -297), new Vector2(960, 456.8073f), true);
        Copy(createCard.transform, "PrivateRoomCreateHeading", "private_room_create_title",
            42, new Rect(65, 42, 380, 120), White);
        // The localized all-caps card title omits Greek tonos without changing
        // the sentence-case form title or relying on TMP's invariant casing.
        Copy(joinCard.transform, "PrivateRoomJoinHeading", "private_room_join_card_title",
            42, new Rect(-5, 79, 420, 110), White);
        Copy(createCard.transform, "PrivateRoomCreateHint", "private_room_create_hint",
            31, new Rect(72, -53, 366, 84), Cyan, false);
        Copy(joinCard.transform, "PrivateRoomCodeCaption", "pvp_enter_code",
            25, new Rect(5, 36, 400, 36), White, false);
        landingCodeInput = Field(joinCard.transform, "PrivateRoomLandingCodeInput",
            "pvp_enter_code", new Vector2(208, -21), new Vector2(420, 94), true, 38);
        landingCodeInput.onValueChanged.AddListener(NormalizeLandingCode);
        SeatAction(create, createCard.transform, "private_room_create_action",
            new Vector2(255, -143), new Vector2(400, 110), 37, true, true);
        SeatAction(join, joinCard.transform, "private_room_join_action",
            new Vector2(208, -149), new Vector2(430, 110), 40, true);
        join.onClick.AddListener(CopyLandingCodeIntoJoinFlow);

        var tip = Slice(safe, "PrivateRoomTipCard", PurpleResource,
            new Vector2(0, -666), new Vector2(640, 230));
        Copy(tip.transform, "PrivateRoomTip", "private_room_tip",
            32, new Rect(-270, -65, 540, 130), White, false);
        Mascots(safe, "PrivateRoom");
        EnlargeIllustratedCard(createCard.rectTransform, 1.0625f);
        EnlargeIllustratedCard(joinCard.rectTransform, 1.0625f);
        built = true;
        IsReady = true;
        ApplyResponsiveLayout();
        RefreshCopy();
    }

    Transform ScreenShell(GameObject panel, string name, string safeName)
    {
        var visual = Rect(panel.transform, name, Vector2.zero, Vector2.zero);
        RuntimeUI.Stretch(visual.gameObject);
        visual.SetAsFirstSibling();
        // Use the approved Solo starfield itself, without Home's much larger
        // confetti overlays competing with room instructions or profile text.
        var image = Sprite(visual, name == VisualRootName ? "PrivateRoomBackground" : name + "Background",
            BackgroundResource, Vector2.zero, new Vector2(1080, 1920));
        image.raycastTarget = true; // Prevent clicks leaking into Home.
        PrivateRoomPortraitArtEnvelope.Attach(image);
        var safe = Rect(visual, safeName, Vector2.zero, Vector2.zero);
        RuntimeUI.Stretch(safe.gameObject);
        var canvas = panel.GetComponentInParent<Canvas>();
        if (canvas != null) ResponsiveSafeAreaRoot.Attach(safe,
            canvas.transform as RectTransform, new Vector2(1080, 1920));
        return safe;
    }

    void Header(Transform safe, string prefix, Button back)
    {
        SeatBack(back, safe, prefix);
        var step = Copy(safe, prefix + "StepText", "private_room_step",
            24, new Rect(-374, 807, 400, 74), White);
        var chip = Sprite(safe, prefix + "PlayerChip", ChipResource,
            MenuPortraitLayout.HeaderPosition(safe as RectTransform), MenuPortraitLayout.ChipSize);
        var portrait = MenuPortraitLayout.CreatePortrait(chip.transform, prefix + "PlayerAvatar");
        MenuPortraitLayout.PaintPortrait(portrait, PlayerProfileAvatarResolver.Resolve());
        portraits.Add(portrait);
        var playerName = Text(chip.transform, prefix + "PlayerName", "", 34,
            MenuPortraitLayout.NameFace, White);
        MenuPortraitLayout.StyleName(playerName);
        names.Add(playerName);
        streaks.Add(Text(chip.transform, prefix + "Streak", "", 32,
            MenuPortraitLayout.ScoreFace, Gold, false));
        Sprite(chip.transform, prefix + "StreakIcon", "mainmenu/mainmenu_icon_streak",
            new Vector2(-151, -32), new Vector2(34, 34), true);
        // The step label's own neutral holder moves with the header row; its
        // existing glyph-safe region remains local and is never counter-moved.
        var stepHolder = Rect(safe, prefix + "StepRegion", new Vector2(-174, 844), new Vector2(400, 74));
        step.transform.SetParent(stepHolder, false);
        centered.RemoveAll(region => region.Text == step);
        centered.Add(new MainMenuCenteredTextRegion(step, 0, 0, 400, 74));
        headers.Add(new HeaderParts { safe = safe as RectTransform, chip = chip.rectTransform,
            back = back.transform as RectTransform, step = stepHolder, prefix = prefix });
    }

    void Title(Transform safe, string prefix, string key)
    {
        var logo = Sprite(safe, prefix + "Logo", LogoResource,
            MenuPortraitLayout.LogoPosition(safe as RectTransform), MenuPortraitLayout.LogoSize, true);
        var ribbon = Sprite(safe, prefix + "TitleRibbon", RibbonResource,
            new Vector2(0, 493), new Vector2(938, 181));
        Copy(ribbon.transform, prefix + "Title", key, 45,
            new Rect(-355, -40, 710, 95), White);
        var header = headers.Find(item => item.safe == safe);
        header.logo = logo.rectTransform;
        header.ribbon = ribbon.rectTransform;
    }

    void Mascots(Transform safe, string prefix)
    {
        Sprite(safe, prefix + "MascotSix", "reference/mascot_6_exact",
            new Vector2(-428, -775), new Vector2(197, 235), true);
        Sprite(safe, prefix + "MascotSeven", "reference/mascot_7_exact",
            new Vector2(428, -775), new Vector2(197, 235), true);
    }

    public PrebattleParts BuildPrebattlePanel(string name, bool createMode)
    {
        EnsureResources();
        var parts = new PrebattleParts { createMode = createMode };
        parts.panel = RuntimeUI.CreateObject(name, transform);
        RuntimeUI.Stretch(parts.panel);
        Transform safe = ScreenShell(parts.panel, name + "Visuals", name + "VisualsSafeRoot");
        parts.back = NewButton(safe, "CancelButton", "cancel",
            new Vector2(0, -780), new Vector2(360, 108), 34, false);
        // Top-left Back and bottom Cancel both use the existing cancellation
        // callback, wired by PvpRuntimeUI, not a second room-lifecycle owner.
        var topBack = NewButton(safe, name + "TopBack", null,
            new Vector2(-468, 840), new Vector2(90, 90), 1, false);
        topBack.onClick.AddListener(() => parts.back.onClick.Invoke());
        Header(safe, name, topBack);
        Title(safe, name, createMode ? "private_room_create_title" : "private_room_join_title");
        var board = Sprite(safe, "PrebattleBoard", BoardResource,
            new Vector2(0, -165), new Vector2(960, 1280));
        // A non-raycasting image can still visually cover an earlier sibling.
        // Keep the board behind Header, the title ribbon and both Back controls.
        board.transform.SetAsFirstSibling();
        // Fixed, centered authoring containers are already inside our safe root.
        // A second full-screen root would register inputs with the generic page
        // layout and reapply their original positions on language/viewport change.
        parts.entryRoot = Rect(safe, "EntryState", Vector2.zero, new Vector2(1080, 1920)).gameObject;
        parts.waitingRoot = Rect(safe, "WaitingState", Vector2.zero, new Vector2(1080, 1920)).gameObject;
        Transform entry = parts.entryRoot.transform, waiting = parts.waitingRoot.transform;

        Copy(entry, "SecretPrivacy", "private_room_secret_privacy", 32,
            new Rect(-335, 224, 670, 132), White, false);
        if (!createMode)
        {
            Copy(entry, "RoomCodeCaption", "pvp_enter_code", 34,
                new Rect(-340, 146, 680, 55), Cyan);
            parts.codeInput = Field(entry, "CodeInput", "pvp_enter_code",
                new Vector2(0, 59), new Vector2(686, 134), true, 54);
        }
        float captionY = createMode ? 98 : -52;
        Copy(entry, "SecretCaption", "pvp_secret", 34,
            new Rect(-350, captionY - 32, 700, 64), Cyan);
        parts.secret = Field(entry, "SecretInput", "number_placeholder",
            new Vector2(0, createMode ? -25 : -158), new Vector2(686, 134), false, 60);
        Copy(entry, "SecretHelp", "private_room_secret_help", 28,
            new Rect(-335, createMode ? -200 : -315, 670, 74), Muted, false);
        parts.confirm = NewButton(entry, createMode ? "ConfirmCreateButton" : "ConfirmJoinButton",
            createMode ? "private_room_create_action" : "private_room_join_action",
            new Vector2(0, -405), new Vector2(730, 151), 52, true).gameObject;
        parts.entryStatus = Text(entry, "EntryStatus", "", 32,
            new Rect(-350, -641, 700, 151), White, false);

        // Seat both complete frames inside the board's visible inner aperture,
        // not its wider transparent image rect. Preserve text size and height.
        var you = Slice(waiting, "YouCard", PurpleResource,
            new Vector2(-200, 205), new Vector2(340, 252));
        var opponent = Slice(waiting, "OpponentCard", PurpleResource,
            new Vector2(200, 205), new Vector2(340, 252));
        Copy(you.transform, "YouCaption", "prebattle_you", 30,
            new Rect(-142, 62, 284, 52), Cyan);
        Copy(you.transform, "YouReady", "private_room_secret_ready", 31,
            new Rect(-144, -94, 288, 76), White, false);
        Copy(opponent.transform, "OpponentCaption", "prebattle_opponent", 30,
            new Rect(-142, 62, 284, 52), Cyan);
        parts.opponentStatus = Copy(opponent.transform, "Status", "prebattle_waiting_short", 31,
            new Rect(-144, -94, 288, 76), White, false);
        parts.playerPortrait = WaitingPortrait(you.transform, "WaitingPlayerAvatar");
        parts.opponentPortrait = WaitingPortrait(opponent.transform, "WaitingOpponentAvatar");
        parts.playerName = Text(you.transform, "WaitingPlayerName", "", 26,
            new Rect(-44, -16, 186, 68), White, false);
        parts.opponentName = Text(opponent.transform, "WaitingOpponentName", "", 26,
            new Rect(-44, -16, 186, 68), White, false);

        if (createMode)
        {
            Copy(waiting, "CodeCaption", "pvp_enter_code", 34,
                new Rect(-340, -22, 680, 55), Cyan);
            var code = Sprite(waiting, "RoomCodeFrame", InputResource,
                new Vector2(0, -114), new Vector2(730, 146));
            parts.codeText = Text(code.transform, "RoomCode", "-----", 67,
                new Rect(-265, -45, 530, 103), White);
            // One code/invitation region, primary native share and explicit copy fallback.
            var inviteActions = Rect(waiting, "ShareButton", new Vector2(0, -291), new Vector2(730, 151));
            parts.share = NewButton(inviteActions, "NativeShareButton", "pvp_share",
                new Vector2(-160, 0), new Vector2(410, 151), 36, true);
            parts.copy = NewButton(inviteActions, "CopyInviteButton", "pvp_copy_short",
                new Vector2(220, 0), new Vector2(290, 112), 30, false);
            Copy(waiting, "ShareHelp", "private_room_share_help", 28,
                new Rect(-335, -466, 670, 99), Muted, false);
        }
        else
        {
            Copy(waiting, "JoiningHint", "private_room_join_wait_hint", 34,
                new Rect(-335, -140, 670, 155), White, false);
        }
        var waitingPlate = Slice(waiting, "WaitingPlate", PurpleResource,
            new Vector2(0, -570), new Vector2(760, 164));
        parts.status = Text(waitingPlate.transform, "Status", "", 32,
            new Rect(-320, -48, 640, 108), White, false);
        Sprite(safe, name + "MascotSix", "reference/mascot_6_exact",
            new Vector2(-427, -784), new Vector2(123, 146), true);
        Sprite(safe, name + "MascotSeven", "reference/mascot_7_exact",
            new Vector2(427, -784), new Vector2(123, 146), true);
        forms.Add(parts);
        parts.waitingRoot.SetActive(false);
        ApplyResponsiveLayout();
        return parts;
    }

    void EnlargeIllustratedCard(RectTransform card, float factor)
    {
        // Preserve the recovered illustrations' aspect. Resize the actual card
        // and its live direct content rects, never a scaled Canvas/overlay.
        card.sizeDelta *= factor;
        foreach (RectTransform child in card)
        {
            child.anchoredPosition *= factor;
            child.sizeDelta *= factor;
        }
        foreach (var region in centered)
            if (region.Text != null && region.Text.transform.parent == card)
                region.ResizeFace(factor);
    }

    void ApplyResponsiveLayout()
    {
        foreach (var header in headers)
        {
            float height = MenuPortraitLayout.Height(header.safe);
            if (Mathf.Approximately(header.height, height)) continue;
            header.height = height;
            float top = MenuPortraitLayout.Top(header.safe);
            float extra = Mathf.Max(0f, height - 1920f);
            Place(header.chip, MenuPortraitLayout.HeaderPosition(header.safe), MenuPortraitLayout.ChipSize);
            Place(header.back, new Vector2(-454, top - 103), new Vector2(118, 118));
            Place(header.step, new Vector2(-174, top - 103), new Vector2(400, 74));
            if (header.logo != null) Place(header.logo, MenuPortraitLayout.LogoPosition(header.safe), MenuPortraitLayout.LogoSize);
            if (header.ribbon != null) Place(header.ribbon, new Vector2(0, top - 545), new Vector2(938, 181));
            bool landing = header.prefix == "PrivateRoom";
            if (landing)
            {
                var create = Find(header.safe, "PrivateRoomCreateCard") as RectTransform;
                var join = Find(header.safe, "PrivateRoomJoinCard") as RectTransform;
                if (create != null) create.anchoredPosition = new Vector2(0, top - 885);
                if (join != null) join.anchoredPosition = new Vector2(0, top - 1382);
                var tip = Find(header.safe, "PrivateRoomTipCard") as RectTransform;
                if (tip != null) tip.anchoredPosition = new Vector2(0, top - 1765 - .2f * extra);
            }
            else
            {
                var board = Find(header.safe, "PrebattleBoard") as RectTransform;
                if (board != null) Place(board, new Vector2(0, -252.5f),
                    new Vector2(1000, MenuPortraitLayout.Height(header.safe) - 765));
                foreach (string state in new[] { "EntryState", "WaitingState" })
                {
                    var content = Find(header.safe, state) as RectTransform;
                    if (content != null) content.anchoredPosition = Vector2.zero;
                }
                ReflowForm(header.safe, height - 765f);
                var cancel = Find(header.safe, "CancelButton") as RectTransform;
                if (cancel != null) cancel.anchoredPosition = new Vector2(0, -top + 70);
            }
            foreach (string suffix in new[] { "MascotSix", "MascotSeven" })
            {
                var mascot = Find(header.safe, header.prefix + suffix) as RectTransform;
                if (mascot == null) continue;
                if (!landing)
                {
                    Place(mascot, new Vector2(suffix == "MascotSix" ? -425 : 425, -top + 100),
                        new Vector2(165, 190));
                    continue;
                }

                // Keep complete sprite rectangles outside the 640px tip and
                // inside the 1080px safe width. The standard side gutters fit
                // the approved 220px mascots exactly. Grow only when the tall
                // viewport also has room below the tip, retaining the approved
                // maximum size without letting hair/hands cross either region.
                float tipBottom = top - 1765f - .2f * extra - 115f;
                float belowTip = tipBottom + top;
                float growth = Mathf.Min(60f * Mathf.Clamp01(extra / 420f),
                    Mathf.Max(0f, belowTip - 262f));
                var size = new Vector2(220f + growth, 260f + growth);
                float x = (growth > 0f ? 539f : 540f) - size.x * .5f;
                float y = top - 1775f - .85f * extra;
                if (growth > 0f) y = Mathf.Min(y, tipBottom - size.y * .5f - 1f);
                Place(mascot, new Vector2(suffix == "MascotSix" ? -x : x, y), size);
            }
        }
    }

    void ReflowForm(Transform safe, float boardHeight)
    {
        // The board PNG has transparent gutters and a thick rounded border.
        // Reflow real controls inside its central 78% x 80% face. Extra height
        // becomes deliberate spacing, never a stretched font or hidden copy.
        float top = -252.5f + .4f * boardHeight;
        float span = .8f * boardHeight;
        Transform entry = Find(safe, "EntryState"), waiting = Find(safe, "WaitingState");
        bool join = Find(entry, "CodeInput") != null;
        MoveFormRegion(entry, "SecretPrivacy", top - .09f * span);
        MoveFormRegion(entry, "RoomCodeCaption", top - .21f * span);
        MoveFormRegion(entry, "CodeInput", top - .315f * span);
        MoveFormRegion(entry, "SecretCaption", top - (join ? .427f : .30f) * span);
        MoveFormRegion(entry, "SecretInput", top - (join ? .525f : .42f) * span);
        MoveFormRegion(entry, "SecretHelp", top - (join ? .65f : .57f) * span);
        MoveFormRegion(entry, join ? "ConfirmJoinButton" : "ConfirmCreateButton", top - .79f * span);
        MoveFormRegion(entry, "EntryStatus", top - .915f * span);
        MoveFormRegion(waiting, "YouCard", top - .17f * span);
        MoveFormRegion(waiting, "OpponentCard", top - .17f * span);
        MoveFormRegion(waiting, "CodeCaption", top - .365f * span);
        MoveFormRegion(waiting, "RoomCodeFrame", top - .46f * span);
        MoveFormRegion(waiting, "ShareButton", top - .63f * span);
        MoveFormRegion(waiting, "ShareHelp", top - .755f * span);
        MoveFormRegion(waiting, "JoiningHint", top - .48f * span);
        MoveFormRegion(waiting, "WaitingPlate", top - .90f * span);
    }

    void MoveFormRegion(Transform content, string name, float y)
    {
        var rect = Find(content, name) as RectTransform;
        if (rect == null) return;
        var text = rect.GetComponent<TMP_Text>();
        var region = text == null ? null : centered.Find(item => item.Text == text);
        if (region != null) region.MoveFace(new Vector2(region.SafeRect.center.x, y));
        else rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
    }

    void RefreshCopy()
    {
        RefreshIdentity();
        RefreshAvailability();
        foreach (var region in centered) region.Apply();
    }

    Image WaitingPortrait(Transform parent, string name)
    {
        var aperture = Sprite(parent, name + "Aperture",
            PlayerProfileAvatarResolver.CircularApertureResourcePath,
            new Vector2(-105, 18), new Vector2(80, 80), true);
        aperture.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        return Sprite(aperture.transform, name, null, Vector2.zero, new Vector2(80, 80), true);
    }

    public void RefreshRoomIdentity() { RefreshIdentity(); }

    void RefreshIdentity()
    {
        string name = "", avatarId = "";
        bool accepted = pvp != null && pvp.TryGetPresentationIdentity(false, out name, out avatarId);
        if (!accepted) name = PlayerPrefs.GetString(OnboardingProfile.PlayerNameKey, "");
        if (string.IsNullOrWhiteSpace(name)) name = L10n.Get("player_default");
        foreach (var text in names) if (text != null) text.text = name;
        foreach (var text in streaks) if (text != null) text.text = GameStats.CurrentStreak.ToString();
        foreach (var portrait in portraits)
        {
            if (portrait == null) continue;
            portrait.sprite = accepted ? PlayerProfileAvatarResolver.ResolveId(avatarId) : PlayerProfileAvatarResolver.Resolve();
            PlayerProfileAvatarFraming.Apply(portrait, portrait.transform.parent as RectTransform);
        }
        string opponentName = "", opponentId = "";
        if (pvp != null) pvp.TryGetPresentationIdentity(true, out opponentName, out opponentId);
        foreach (var form in forms)
        {
            form.playerName.text = accepted ? name : "";
            form.opponentName.text = accepted ? opponentName : "";
            form.playerName.gameObject.SetActive(accepted && !string.IsNullOrWhiteSpace(name));
            form.opponentName.gameObject.SetActive(accepted && !string.IsNullOrWhiteSpace(opponentName));
            PaintWaitingPortrait(form.playerPortrait, avatarId, accepted);
            PaintWaitingPortrait(form.opponentPortrait, opponentId,
                accepted && !string.IsNullOrWhiteSpace(opponentName));
        }
    }

    static void PaintWaitingPortrait(Image portrait, string avatarId, bool present)
    {
        portrait.gameObject.SetActive(present);
        portrait.sprite = present ? PlayerProfileAvatarResolver.ResolveId(avatarId) : null;
        if (present) PlayerProfileAvatarFraming.Apply(portrait, portrait.transform.parent as RectTransform);
    }

    void RefreshAvailability()
    {
        foreach (var form in forms)
        {
            int secret;
            bool validSecret = int.TryParse(form.secret.text, out secret) && secret >= 1 && secret <= 100;
            bool validCode = form.createMode || ValidCode(form.codeInput.text);
            form.confirm.GetComponent<Button>().interactable = validSecret && validCode;
            if (form.copy != null)
                form.copy.interactable = pvp != null && pvp.CanShareInvite;
            if (form.share != null)
                form.share.interactable = pvp != null && pvp.CanShareInvite;
        }
    }

    static bool ValidCode(string code)
    {
        if (string.IsNullOrEmpty(code) || code.Trim().Length != 5) return false;
        foreach (char ch in code.Trim())
            if (!(ch >= 'A' && ch <= 'Z') && !(ch >= '0' && ch <= '9') &&
                !(ch >= 'a' && ch <= 'z')) return false;
        return true;
    }

    public static char ValidateRoomCodeCharacter(string text, int index, char ch)
    {
        char value = char.ToUpperInvariant(ch);
        return (value >= 'A' && value <= 'Z') || (value >= '0' && value <= '9') ? value : '\0';
    }

    void NormalizeLandingCode(string value)
    {
        string normalized = (value ?? "").Trim().ToUpperInvariant();
        if (normalized.Length > 5) normalized = normalized.Substring(0, 5);
        landingCodeInput.SetTextWithoutNotify(normalized);
    }

    void CopyLandingCodeIntoJoinFlow()
    {
        if (landingCodeInput != null && pvp != null && pvp.joinCodeInput != null)
            pvp.joinCodeInput.SetTextWithoutNotify((landingCodeInput.text ?? "").Trim().ToUpperInvariant());
    }

    TMP_InputField Field(Transform parent, string name, string key, Vector2 position,
        Vector2 size, bool code, float fontSize)
    {
        var field = RuntimeUI.CreateInputField(parent, name, L10n.Get(key), position, size,
            code ? 5 : 3, code ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.IntegerNumber);
        if (code)
        {
            field.characterLimit = 5;
            field.onValidateInput = ValidateRoomCodeCharacter;
        }
        else field.characterLimit = 3;
        var image = field.GetComponent<Image>();
        image.sprite = Resources.Load<Sprite>(InputResource);
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.raycastTarget = true;
        field.shouldHideSoftKeyboard = false;
        field.shouldHideMobileInput = false;
        field.keyboardType = code ? TouchScreenKeyboardType.ASCIICapable : TouchScreenKeyboardType.NumberPad;
        field.readOnly = false;
        StyleText(field.textComponent, fontSize, White, true);
        field.textComponent.enableWordWrapping = false;
        field.textComponent.alignment = TextAlignmentOptions.Center;
        var placeholder = field.placeholder as TMP_Text;
        if (placeholder != null)
        {
            StyleText(placeholder, code ? 28 : 33, Muted, false);
            placeholder.enableWordWrapping = false;
            placeholder.alignment = TextAlignmentOptions.Center;
        }
        // The usable field face is above the heavy lower shadow. Both the
        // editable text and placeholder live in the same padded viewport.
        var viewport = field.textViewport;
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(size.x * .10f, size.y * .19f);
        viewport.offsetMax = new Vector2(-size.x * .10f, -size.y * .12f);
        RuntimeUI.LocalizePlaceholder(field, key);
        return field;
    }

    void SeatBack(Button button, Transform parent, string prefix)
    {
        SeatAction(button, parent, null, new Vector2(-468, 840), new Vector2(90, 90), 1, false);
        var image = button.GetComponent<Image>();
        image.sprite = Resources.Load<Sprite>(BackResource);
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.name = button.name;
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.gameObject.SetActive(false);
    }

    Button NewButton(Transform parent, string name, string key,
        Vector2 position, Vector2 size, float fontSize, bool primary)
    {
        var go = Rect(parent, name, position, size).gameObject;
        var button = go.AddComponent<Button>();
        SeatAction(button, parent, key, position, size, fontSize, primary);
        return button;
    }

    void SeatAction(Button button, Transform parent, string key,
        Vector2 position, Vector2 size, float fontSize, bool primary, bool cyan = false)
    {
        button.transform.SetParent(parent, false);
        Place((RectTransform)button.transform, position, size);
        // Neutral RuntimeUI placeholder labels are not a competing production
        // screen. Keep their callbacks and replace only that button's text.
        foreach (var old in button.GetComponentsInChildren<TMP_Text>(true))
            old.gameObject.SetActive(false);
        var image = button.GetComponent<Image>();
        if (image == null) image = button.gameObject.AddComponent<Image>();
        image.sprite = Resources.Load<Sprite>(cyan ? CreateActionResource : primary ? PrimaryResource : PurpleResource);
        image.type = primary && !cyan ? Image.Type.Simple : Image.Type.Sliced;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = true;
        if (!primary || cyan) FitSlice(image, size);
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white;
        colors.pressedColor = new Color(.82f, .86f, .96f, 1);
        colors.disabledColor = new Color(.56f, .57f, .65f, .80f);
        colors.fadeDuration = .06f;
        button.colors = colors;
        // Measured Solo CTA face: between the two stars, clear of its lower
        // bevel. Rect is in the button's coordinates, not its full image box.
        var face = cyan
            ? new Rect(-size.x * .38f, -size.y * .22f, size.x * .76f, size.y * .50f)
            : primary
            ? new Rect(-size.x * .32f, -size.y * .29f, size.x * .64f, size.y * .65f)
            : new Rect(-size.x * .40f, -size.y * .25f, size.x * .80f, size.y * .60f);
        if (key != null) Copy(button.transform, "PrivateRoomActionLabel", key,
            fontSize, face, primary && !cyan ? Ink : White);
        RuntimeUI.AttachJuice(button);
    }

    TMP_Text Copy(Transform parent, string name, string key, float size,
        Rect face, Color color, bool display = true)
    {
        var label = Text(parent, name, L10n.Get(key), size, face, color, display);
        RuntimeUI.Localize(label, key);
        return label;
    }

    TMP_Text Text(Transform parent, string name, string value, float size,
        Rect face, Color color, bool display = true)
    {
        var label = Rect(parent, name, face.center, face.size).gameObject.AddComponent<TextMeshProUGUI>();
        StyleText(label, size, color, display);
        label.text = value;
        centered.Add(new MainMenuCenteredTextRegion(label, face.center.x, face.center.y, face.width, face.height));
        return label;
    }

    void StyleText(TMP_Text label, float size, Color color, bool display)
    {
        label.font = display ? displayFont : bodyFont;
        label.fontStyle = FontStyles.Bold;
        label.fontSize = size;
        label.enableAutoSizing = true;
        label.fontSizeMin = label.fontSizeMax = size;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Overflow;
        label.richText = false;
        label.raycastTarget = false;
        label.color = color;
        label.characterSpacing = label.wordSpacing = label.lineSpacing = 0;
        label.outlineColor = Ink;
        label.outlineWidth = color == Ink ? 0 : .14f;
    }

    Image Sprite(Transform parent, string name, string resource, Vector2 position,
        Vector2 size, bool preserveAspect = false)
    {
        var image = Rect(parent, name, position, size).gameObject.AddComponent<Image>();
        image.sprite = resource == null ? null : Resources.Load<Sprite>(resource);
        if (resource != null && image.sprite == null)
            Debug.LogError("[PrivateRoomVisuals] Missing resource " + resource);
        image.color = Color.white;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
        return image;
    }

    Image Slice(Transform parent, string name, string resource, Vector2 position, Vector2 size)
    {
        var image = Sprite(parent, name, resource, position, size);
        image.type = Image.Type.Sliced;
        FitSlice(image, size);
        return image;
    }

    static void FitSlice(Image image, Vector2 size)
    {
        if (image.sprite != null) image.pixelsPerUnitMultiplier = Mathf.Max(2f,
            image.sprite.rect.width / size.x, image.sprite.rect.height / size.y);
    }

    static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var rect = (RectTransform)RuntimeUI.CreateObject(name, parent).transform;
        Place(rect, position, size);
        return rect;
    }

    static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    static Transform Find(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            var found = Find(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
