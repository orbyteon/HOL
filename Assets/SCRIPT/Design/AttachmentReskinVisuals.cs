using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Reskins the existing HOL screens to the six-panel reference board without
// inventing any new product flow. Every interactive control used here already
// belongs to an existing controller; this class only changes presentation.
[DefaultExecutionOrder(1000)]
public sealed class AttachmentReskinVisuals : MonoBehaviour
{
    const string LogoResource = "reference/hol_logo_exact";
    const string PlayerResource = "reference/player_cyan_exact";
    const string OpponentResource = "reference/opponent_purple_exact";
    const string SevenResource = "reference/mascot_7_exact";
    const string ThreeResource = "reference/mascot_3_exact";

    static readonly Color Depth = Hex(0x07, 0x05, 0x20);
    static readonly Color Panel = Hex(0x14, 0x0A, 0x43);
    static readonly Color PanelDark = Hex(0x0D, 0x08, 0x32);
    static readonly Color Purple = Hex(0x72, 0x27, 0xD8);
    static readonly Color PurpleDark = Hex(0x43, 0x15, 0x95);
    static readonly Color Cyan = Hex(0x00, 0xBA, 0xF5);
    static readonly Color Blue = Hex(0x06, 0x70, 0xD8);
    static readonly Color Pink = Hex(0xE8, 0x24, 0x74);
    static readonly Color PinkDark = Hex(0x8F, 0x0E, 0x47);
    static readonly Color Gold = Hex(0xFF, 0xC2, 0x00);
    static readonly Color GoldDark = Hex(0xA9, 0x62, 0x00);
    static readonly Color White = Hex(0xFA, 0xF7, 0xFF);
    static readonly Color Muted = Hex(0xD1, 0xC5, 0xEE);
    static readonly Color Ink = Hex(0x22, 0x13, 0x09);

    Sprite logo;
    Sprite player;
    Sprite opponent;
    Sprite seven;
    Sprite three;

    float nextPass;
    int lastSignature = int.MinValue;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        if (scene.name == "SplashScene") return;

        Canvas canvas = null;
        var menu = FindInScene<MenuManager>(scene);
        if (menu != null && menu.mainMenuPanel != null)
            canvas = menu.mainMenuPanel.GetComponentInParent<Canvas>();

        if (canvas == null || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace)
        {
            canvas = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var candidate in root.GetComponentsInChildren<Canvas>(true))
                {
                    if (!candidate.isRootCanvas || candidate.renderMode == RenderMode.WorldSpace)
                        continue;
                    canvas = candidate;
                    break;
                }
                if (canvas != null) break;
            }
        }

        if (canvas != null && canvas.GetComponent<AttachmentReskinVisuals>() == null)
            canvas.gameObject.AddComponent<AttachmentReskinVisuals>();
    }

    void Awake()
    {
        logo = Resources.Load<Sprite>(LogoResource);
        player = Resources.Load<Sprite>(PlayerResource);
        opponent = Resources.Load<Sprite>(OpponentResource);
        seven = Resources.Load<Sprite>(SevenResource);
        three = Resources.Load<Sprite>(ThreeResource);
    }

    void OnEnable()
    {
        L10n.OnLanguageChanged += ApplyNow;
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= ApplyNow;
    }

    IEnumerator Start()
    {
        // Let all existing builders finish first. This layer is intentionally
        // last because it is presentation-only and must never replace wiring.
        for (int i = 0; i < 4; i++)
            yield return null;
        ApplyNow();
        lastSignature = Signature();
    }

    void LateUpdate()
    {
        if (Time.unscaledTime < nextPass) return;
        nextPass = Time.unscaledTime + 0.25f;

        int signature = Signature();
        if (signature == lastSignature) return;
        ApplyNow();
        lastSignature = Signature();
    }

    int Signature()
    {
        unchecked
        {
            int value = 23;
            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                value = value * 31 + child.GetInstanceID();
                value = value * 31 + (child.gameObject.activeSelf ? 1 : 0);
            }
            return value;
        }
    }

    void ApplyNow()
    {
        ApplyCanvasBackdrop();

        var menu = FindInScene<MenuManager>(gameObject.scene);
        if (menu != null)
        {
            if (gameObject.scene.name != "MainMenu")
                ApplyHome(menu);
            var settingsPanel = menu.settingsPanel == null
                ? null
                : menu.settingsPanel.transform;
            // SettingsVisuals is the authoritative owner once its production
            // hierarchy exists. A later generic pass must never replace those
            // approved sprites with procedural rounded rectangles.
            if (settingsPanel == null ||
                DeepFind(settingsPanel, "SettingsVisualRoot") == null)
                ApplySimplePanel(settingsPanel);
            if (gameObject.scene.name != "MainMenu")
                ApplySimplePanel(menu.panelPlay == null ? null : menu.panelPlay.transform);
            ApplySearching(menu.panelSearching == null ? null : menu.panelSearching.transform);
        }

        var pvp = FindInScene<PvpGameController>(gameObject.scene);
        if (pvp != null)
            ApplyPvp(pvp);

        var game = FindInScene<GameManager>(gameObject.scene);
        if (game != null)
            ApplySoloGame(game);

        // Daily Hunt, consent and force-update already have their real flows.
        // Keep them intact and only pull their existing controls into the same
        // reference-board surface/button language.
        ApplySimplePanel(DeepFind(transform, "DailyHuntPanel"));
        ApplySimplePanel(DeepFind(transform, "ConsentPanel"));
        ApplySimplePanel(DeepFind(transform, "ForceUpdatePanel"));
    }

    void ApplyCanvasBackdrop()
    {
        if (gameObject.scene.name == "MainMenu") return;
        var backdrop = DirectChild(transform, "AttachmentReferenceBackdrop");
        if (backdrop == null)
        {
            var go = RuntimeUI.CreateObject("AttachmentReferenceBackdrop", transform);
            RuntimeUI.Stretch(go);
            var image = go.AddComponent<Image>();
            image.color = Depth;
            image.raycastTarget = false;
            go.transform.SetAsFirstSibling();
            backdrop = go.transform;
        }

        var imageBackdrop = backdrop.GetComponent<Image>();
        if (imageBackdrop != null) imageBackdrop.color = Depth;
    }

    void ApplyHome(MenuManager menu)
    {
        if (menu.mainMenuPanel == null) return;
        var root = menu.mainMenuPanel.transform;
        if (!root.gameObject.activeInHierarchy) return;

        // The new reference home is logo + number mascots + one large primary
        // action + two secondary actions. Remove only presentation objects from
        // the older exact pass; the real menu buttons remain untouched.
        SetActive(DeepFind(root, "ExactTagline"), false);
        SetActive(DeepFind(root, "ExactPlayerHero"), false);
        SetActive(DeepFind(root, "ExactOpponentHero"), false);

        AddImage(root, "BoardHomeLogo", logo,
            new Vector2(0f, 620f), new Vector2(620f, 330f), true);
        AddImage(root, "BoardHomeSeven", seven,
            new Vector2(-350f, 210f), new Vector2(300f, 390f), true);
        AddImage(root, "BoardHomeThree", three,
            new Vector2(350f, 210f), new Vector2(300f, 390f), true);

        var oldSeven = DeepFind(root, "ExactMascotSeven");
        var oldThree = DeepFind(root, "ExactMascotThree");
        SetActive(oldSeven, false);
        SetActive(oldThree, false);

        BuildPlayerChip(root, new Vector2(310f, 820f));

        var play = FindButton(root, "ButtonPlay");
        if (play != null)
        {
            Place((RectTransform)play.transform, new Vector2(0f, 115f), new Vector2(540f, 185f));
            StyleButton(play, Gold, Ink, GoldDark, 5f);
            SetButtonCopy(play,
                L10n.Get("play").ToUpperInvariant() + "!",
                L10n.Get("find_challenger").ToUpperInvariant(),
                54f, 25f);
        }

        var friend = FindButton(root, "ButtonPvP");
        if (friend != null)
        {
            Place((RectTransform)friend.transform, new Vector2(-245f, -130f), new Vector2(450f, 165f));
            StyleButton(friend, Blue, White, Hex(0x00, 0xC3, 0xFF), 3f);
            SetButtonCopy(friend,
                IsGreek ? "ΠΑΙΞΕ\nΜΕ ΦΙΛΟ" : "PLAY WITH\nA FRIEND",
                "", 38f, 20f);
            EnsureIconText(friend.transform, "BoardFriendIcon", "☻☻",
                new Vector2(-155f, 0f), new Vector2(100f, 80f), 38f, White);
        }

        var daily = FindButton(root, "DailyHuntButton");
        if (daily != null)
        {
            Place((RectTransform)daily.transform, new Vector2(245f, -130f), new Vector2(450f, 165f));
            StyleButton(daily, Purple, White, Hex(0xA8, 0x58, 0xFF), 3f);
            SetButtonCopy(daily,
                L10n.Get("daily_hunt").ToUpperInvariant(),
                "", 36f, 20f);
            EnsureIconText(daily.transform, "BoardDailyIcon", "ϟ",
                new Vector2(-155f, 0f), new Vector2(90f, 90f), 58f, White);
        }

        var settings = FindButton(root, "Buttonsettings");
        if (settings != null)
        {
            Place((RectTransform)settings.transform, new Vector2(-455f, 820f), new Vector2(82f, 82f));
            StyleButton(settings, Panel, White, Purple, 2f);
        }

        BuildTipCard(root);
    }

    void BuildPlayerChip(Transform root, Vector2 position)
    {
        var chip = EnsureImage(root, "BoardPlayerChip");
        chip.sprite = RuntimeUI.RoundedRectSprite;
        chip.type = Image.Type.Sliced;
        chip.color = new Color(0.08f, 0.035f, 0.22f, 0.98f);
        chip.raycastTarget = false;
        Place(chip.rectTransform, position, new Vector2(360f, 92f));
        EnsureOutline(chip.gameObject, Purple, 2f);

        AddImage(chip.transform, "BoardPlayerChipAvatar", player,
            new Vector2(-138f, 0f), new Vector2(76f, 76f), true);

        var label = EnsureText(chip.transform, "BoardPlayerChipText");
        string name = PlayerPrefs.GetString("PlayerName", L10n.Get("player_default"));
        label.text = name.ToUpperInvariant() + "  •  " +
                     L10n.Get("stats_streak").ToUpperInvariant() + " " + GameStats.CurrentStreak;
        label.fontSize = 26f;
        label.fontStyle = FontStyles.Bold;
        label.color = White;
        label.alignment = TextAlignmentOptions.Center;
        Place(label.rectTransform, new Vector2(32f, 0f), new Vector2(260f, 70f));
        Responsive(label, 16f);
    }

    void BuildTipCard(Transform root)
    {
        var card = EnsureImage(root, "BoardHomeTipCard");
        card.sprite = RuntimeUI.RoundedRectSprite;
        card.type = Image.Type.Sliced;
        card.color = new Color(0.07f, 0.03f, 0.23f, 0.98f);
        card.raycastTarget = false;
        Place(card.rectTransform, new Vector2(0f, -420f), new Vector2(900f, 250f));
        EnsureOutline(card.gameObject, Purple, 2f);

        var title = EnsureText(card.transform, "BoardHomeTipTitle");
        title.text = L10n.Get("hud_tip").ToUpperInvariant() + ":";
        title.fontSize = 31f;
        title.fontStyle = FontStyles.Bold;
        title.color = Gold;
        title.alignment = TextAlignmentOptions.Left;
        Place(title.rectTransform, new Vector2(-245f, 62f), new Vector2(360f, 52f));

        var body = EnsureText(card.transform, "BoardHomeTipBody");
        body.text = L10n.Get("simulated_opponents");
        body.fontSize = 25f;
        body.color = White;
        body.alignment = TextAlignmentOptions.Left;
        Place(body.rectTransform, new Vector2(-95f, -22f), new Vector2(620f, 120f));
        Responsive(body, 17f);

        AddImage(card.transform, "BoardHomeTipMascot", seven,
            new Vector2(340f, -5f), new Vector2(210f, 230f), true);
    }

    void ApplyPvp(PvpGameController pvp)
    {
        if (pvp.pvpMenuPanel != null && pvp.pvpMenuPanel.activeInHierarchy)
            ApplyPvpMenu(pvp.pvpMenuPanel.transform);
        if (pvp.createPanel != null && pvp.createPanel.activeInHierarchy)
        {
            if (DeepFind(pvp.createPanel.transform, "YouCard") == null)
                ApplyCreatePanel(pvp);
        }
        if (pvp.joinPanel != null && pvp.joinPanel.activeInHierarchy)
        {
            if (DeepFind(pvp.joinPanel.transform, "YouCard") == null)
                ApplyJoinPanel(pvp);
        }
        if (pvp.matchPanel != null && pvp.matchPanel.activeInHierarchy)
            ApplyPvpMatch(pvp);
    }

    void ApplyPvpMenu(Transform panelRoot)
    {
        if (DeepFind(panelRoot, "TitleRibbon") != null)
            return;

        AddImage(panelRoot, "BoardPvpLogo", logo,
            new Vector2(0f, 705f), new Vector2(500f, 265f), true);

        var title = EnsureText(panelRoot, "BoardPvpTitle");
        title.text = L10n.Get("private_room_title").ToUpperInvariant();
        title.fontSize = 43f;
        title.fontStyle = FontStyles.Bold;
        title.color = White;
        title.alignment = TextAlignmentOptions.Center;
        Place(title.rectTransform, new Vector2(0f, 485f), new Vector2(720f, 75f));
        AddRibbon(panelRoot, "BoardPvpRibbon", new Vector2(0f, 485f), new Vector2(760f, 92f));
        title.transform.SetAsLastSibling();

        var create = FindButton(panelRoot, "CreateButton");
        if (create != null)
        {
            Place((RectTransform)create.transform, new Vector2(-245f, 60f), new Vector2(450f, 650f));
            StyleButton(create, Blue, White, Cyan, 3f);
            SetButtonCopy(create, L10n.Get("pvp_create_room").ToUpperInvariant(), "", 37f, 20f);
            MoveMainLabel(create, new Vector2(0f, -235f), new Vector2(400f, 100f));
            AddImage(create.transform, "BoardCreatePlayer", player,
                new Vector2(-70f, 55f), new Vector2(255f, 300f), true);
            AddImage(create.transform, "BoardCreateOpponent", opponent,
                new Vector2(95f, 35f), new Vector2(235f, 285f), true);
            EnsureIconText(create.transform, "BoardCreatePlus", "+",
                new Vector2(0f, 225f), new Vector2(85f, 85f), 60f, Blue, White);
        }

        var join = FindButton(panelRoot, "JoinButton");
        if (join != null)
        {
            Place((RectTransform)join.transform, new Vector2(245f, 60f), new Vector2(450f, 650f));
            StyleButton(join, Pink, White, Hex(0xFF, 0x55, 0x9B), 3f);
            SetButtonCopy(join, L10n.Get("pvp_join_room").ToUpperInvariant(), "", 37f, 20f);
            MoveMainLabel(join, new Vector2(0f, -235f), new Vector2(400f, 100f));
            AddImage(join.transform, "BoardJoinOpponent", opponent,
                new Vector2(0f, 55f), new Vector2(285f, 330f), true);
            EnsureIconText(join.transform, "BoardJoinDoor", "↪",
                new Vector2(0f, 225f), new Vector2(85f, 85f), 56f, PinkDark, White);
        }

        var close = FindButton(panelRoot, "CloseButton");
        if (close != null)
        {
            Place((RectTransform)close.transform, new Vector2(0f, -555f), new Vector2(330f, 82f));
            StyleButton(close, Panel, White, Purple, 2f);
        }

        AddImage(panelRoot, "BoardPvpSeven", seven,
            new Vector2(-410f, -610f), new Vector2(180f, 225f), true);
        AddImage(panelRoot, "BoardPvpThree", three,
            new Vector2(410f, -610f), new Vector2(180f, 225f), true);

        SetActive(DeepFind(panelRoot, "ExactPvpMenuLogo"), false);
        SetActive(DeepFind(panelRoot, "ExactPvpMenuSeven"), false);
        SetActive(DeepFind(panelRoot, "ExactPvpMenuThree"), false);
        SetActive(DeepFind(panelRoot, "Title"), false);
    }

    void ApplyCreatePanel(PvpGameController pvp)
    {
        var root = pvp.createPanel.transform;
        SetActive(DeepFind(root, "ExactCreateLogo"), false);
        SetActive(DeepFind(root, "ExactCreateSeven"), false);
        SetActive(DeepFind(root, "ExactCreateThree"), false);
        AddImage(root, "BoardCreateLogo", logo,
            new Vector2(0f, 735f), new Vector2(420f, 230f), true);
        AddImage(root, "BoardCreateSeven", seven,
            new Vector2(-405f, -500f), new Vector2(170f, 215f), true);
        AddImage(root, "BoardCreateThree", three,
            new Vector2(405f, -500f), new Vector2(170f, 215f), true);

        var card = EnsureImage(root, "BoardCreateCard");
        card.sprite = RuntimeUI.RoundedRectSprite;
        card.type = Image.Type.Sliced;
        card.color = new Color(0.02f, 0.36f, 0.72f, 0.92f);
        card.raycastTarget = false;
        Place(card.rectTransform, new Vector2(0f, 50f), new Vector2(760f, 940f));
        EnsureOutline(card.gameObject, Cyan, 3f);
        card.transform.SetAsFirstSibling();

        PlaceInput(pvp.createSecretInput, new Vector2(0f, 305f), new Vector2(500f, 100f), PurpleDark);
        if (pvp.roomCodeText != null)
        {
            pvp.roomCodeText.fontSize = 56f;
            pvp.roomCodeText.fontStyle = FontStyles.Bold;
            pvp.roomCodeText.color = Ink;
            pvp.roomCodeText.alignment = TextAlignmentOptions.Center;
            Place(pvp.roomCodeText.rectTransform, new Vector2(0f, 55f), new Vector2(520f, 105f));
            EnsureTextPlate(pvp.roomCodeText.transform.parent, "BoardRoomCodePlate",
                new Vector2(0f, 55f), new Vector2(560f, 120f));
            pvp.roomCodeText.transform.SetAsLastSibling();
        }

        var confirm = FindButton(root, "ConfirmCreateButton");
        if (confirm != null)
        {
            Place((RectTransform)confirm.transform, new Vector2(0f, 185f), new Vector2(500f, 96f));
            StyleButton(confirm, Gold, Ink, GoldDark, 4f);
        }
        var copy = FindButton(root, "CopyButton");
        if (copy != null)
        {
            Place((RectTransform)copy.transform, new Vector2(0f, -120f), new Vector2(500f, 96f));
            StyleButton(copy, Blue, White, Cyan, 3f);
        }
        if (pvp.createStatusText != null)
        {
            pvp.createStatusText.color = White;
            pvp.createStatusText.fontSize = 26f;
            Place(pvp.createStatusText.rectTransform, new Vector2(0f, -280f), new Vector2(640f, 120f));
            Responsive(pvp.createStatusText, 18f);
        }

        var back = FindButton(root, "BackButton");
        if (back != null)
        {
            Place((RectTransform)back.transform, new Vector2(0f, -650f), new Vector2(330f, 82f));
            StyleButton(back, Panel, White, Purple, 2f);
        }
    }

    void ApplyJoinPanel(PvpGameController pvp)
    {
        var root = pvp.joinPanel.transform;
        SetActive(DeepFind(root, "ExactJoinLogo"), false);
        SetActive(DeepFind(root, "ExactJoinPlayer"), false);
        SetActive(DeepFind(root, "ExactJoinOpponent"), false);
        AddImage(root, "BoardJoinLogo", logo,
            new Vector2(0f, 735f), new Vector2(420f, 230f), true);
        AddImage(root, "BoardJoinPlayer", player,
            new Vector2(-385f, -445f), new Vector2(185f, 225f), true);
        AddImage(root, "BoardJoinOpponent", opponent,
            new Vector2(385f, -445f), new Vector2(185f, 225f), true);

        var card = EnsureImage(root, "BoardJoinCard");
        card.sprite = RuntimeUI.RoundedRectSprite;
        card.type = Image.Type.Sliced;
        card.color = new Color(0.69f, 0.02f, 0.26f, 0.93f);
        card.raycastTarget = false;
        Place(card.rectTransform, new Vector2(0f, 55f), new Vector2(760f, 940f));
        EnsureOutline(card.gameObject, Pink, 3f);
        card.transform.SetAsFirstSibling();

        PlaceInput(pvp.joinCodeInput, new Vector2(0f, 265f), new Vector2(520f, 105f), PanelDark);
        PlaceInput(pvp.joinSecretInput, new Vector2(0f, 105f), new Vector2(520f, 105f), PanelDark);
        var confirm = FindButton(root, "ConfirmJoinButton");
        if (confirm != null)
        {
            Place((RectTransform)confirm.transform, new Vector2(0f, -65f), new Vector2(500f, 100f));
            StyleButton(confirm, Gold, Ink, GoldDark, 4f);
        }
        if (pvp.joinStatusText != null)
        {
            pvp.joinStatusText.color = White;
            pvp.joinStatusText.fontSize = 25f;
            Place(pvp.joinStatusText.rectTransform, new Vector2(0f, -260f), new Vector2(650f, 120f));
            Responsive(pvp.joinStatusText, 18f);
        }
        var back = FindButton(root, "BackButton");
        if (back != null)
        {
            Place((RectTransform)back.transform, new Vector2(0f, -650f), new Vector2(330f, 82f));
            StyleButton(back, Panel, White, Purple, 2f);
        }
    }

    void ApplySearching(Transform root)
    {
        if (root == null || !root.gameObject.activeInHierarchy) return;

        if (DeepFind(root, "SoloSearchVisualRoot") != null)
        {
            SetActive(DeepFind(root, "BoardSearchLogo"), false);
            SetActive(DeepFind(root, "BoardVsPlayerCard"), false);
            SetActive(DeepFind(root, "BoardVsOpponentCard"), false);
            SetActive(DeepFind(root, "BoardVsBadge"), false);
            SetActive(DeepFind(root, "BoardSearchRule"), false);
            return;
        }

        AddImage(root, "BoardSearchLogo", logo,
            new Vector2(0f, 715f), new Vector2(440f, 235f), true);
        BuildVersusCards(root, new Vector2(0f, 220f), 390f, 470f);

        var searching = FindInScene<FakeMatchmaking>(gameObject.scene);
        if (searching != null && searching.searchingText != null)
        {
            searching.searchingText.fontSize = 40f;
            searching.searchingText.fontStyle = FontStyles.Bold;
            searching.searchingText.color = White;
            searching.searchingText.alignment = TextAlignmentOptions.Center;
            Place(searching.searchingText.rectTransform, new Vector2(0f, -310f), new Vector2(800f, 120f));
            Responsive(searching.searchingText, 24f);
        }

        SetActive(DeepFind(root, "ExactSearchingLogo"), false);
        SetActive(DeepFind(root, "ExactSearchingPlayer"), false);
        SetActive(DeepFind(root, "ExactSearchingOpponent"), false);
        SetActive(DeepFind(root, "ExactSearchingVs"), false);
    }

    void ApplySoloGame(GameManager game)
    {
        if (game.stopGameButton == null) return;
        var root = game.stopGameButton.transform.parent;
        if (root == null || !root.gameObject.activeInHierarchy) return;

        if (game.IsMatchOver)
        {
            ApplySoloResult(game, root);
            return;
        }

        SetActive(DeepFind(root, "ExactSoloLogo"), false);
        SetActive(DeepFind(root, "BoardVsPlayerCard"), true);
        SetActive(DeepFind(root, "BoardVsOpponentCard"), true);
        SetActive(DeepFind(root, "BoardVsBadge"), true);
        if (game.aiAnswerText != null) game.aiAnswerText.color = White;
        if (game.playerHistoryText != null) game.playerHistoryText.color = White;
        if (game.aiHistoryText != null) game.aiHistoryText.color = White;
        AddImage(root, "BoardSoloLogo", logo,
            new Vector2(0f, 800f), new Vector2(350f, 190f), true);
        BuildVersusCards(root, new Vector2(0f, 505f), 360f, 310f);

        if (game.turnText != null)
        {
            game.turnText.fontSize = 36f;
            game.turnText.fontStyle = FontStyles.Bold;
            game.turnText.color = White;
            Place(game.turnText.rectTransform, new Vector2(0f, 280f), new Vector2(840f, 110f));
            Responsive(game.turnText, 22f);
        }
        if (game.aiAnswerText != null)
        {
            game.aiAnswerText.fontSize = 26f;
            game.aiAnswerText.color = White;
            Place(game.aiAnswerText.rectTransform, new Vector2(0f, 125f), new Vector2(820f, 150f));
            Responsive(game.aiAnswerText, 18f);
        }

        StyleExistingGameButtons(root);
    }

    void ApplySoloResult(GameManager game, Transform root)
    {
        SetActive(DeepFind(root, "BoardVsPlayerCard"), false);
        SetActive(DeepFind(root, "BoardVsOpponentCard"), false);
        SetActive(DeepFind(root, "BoardVsBadge"), false);
        if (game.aiAnswerText != null) game.aiAnswerText.color = Color.clear;
        if (game.playerHistoryText != null) game.playerHistoryText.color = Color.clear;
        if (game.aiHistoryText != null) game.aiHistoryText.color = Color.clear;
        AddImage(root, "BoardSoloResultLogo", logo,
            new Vector2(0f, 800f), new Vector2(350f, 190f), true);
        AddImage(root, "BoardSoloWinner", player,
            new Vector2(-270f, 315f), new Vector2(380f, 430f), true);

        if (game.turnText != null)
        {
            game.turnText.fontSize = 54f;
            game.turnText.fontStyle = FontStyles.Bold;
            game.turnText.color = Gold;
            game.turnText.alignment = TextAlignmentOptions.Center;
            Place(game.turnText.rectTransform, new Vector2(190f, 360f), new Vector2(520f, 250f));
            Responsive(game.turnText, 30f);
        }

        var stats = EnsureImage(root, "BoardSoloResultStats");
        stats.sprite = RuntimeUI.RoundedRectSprite;
        stats.type = Image.Type.Sliced;
        stats.color = Panel;
        stats.raycastTarget = false;
        Place(stats.rectTransform, new Vector2(190f, 70f), new Vector2(500f, 210f));
        EnsureOutline(stats.gameObject, Purple, 2f);

        var statsText = EnsureText(stats.transform, "BoardSoloResultStatsText");
        statsText.text = L10n.Get("stats_wins").ToUpperInvariant() + "  " + GameStats.Wins +
                         "     " + L10n.Get("stats_streak").ToUpperInvariant() + "  " + GameStats.CurrentStreak;
        statsText.fontSize = 28f;
        statsText.fontStyle = FontStyles.Bold;
        statsText.color = White;
        statsText.alignment = TextAlignmentOptions.Center;
        RuntimeUI.Stretch(statsText.gameObject);
        Responsive(statsText, 18f);

        var again = game.stopGameButton.GetComponent<Button>();
        if (again != null)
        {
            Place((RectTransform)again.transform, new Vector2(0f, -230f), new Vector2(520f, 115f));
            StyleButton(again, Blue, White, Cyan, 3f);
            SetButtonMainText(again, L10n.Get("rematch").ToUpperInvariant(), 38f);
        }

        var save = FindButton(root, "SaveStreakButton");
        if (save != null)
        {
            Place((RectTransform)save.transform, new Vector2(0f, -370f), new Vector2(620f, 105f));
            StyleButton(save, Gold, Ink, GoldDark, 4f);
        }
    }

    void ApplyPvpMatch(PvpGameController pvp)
    {
        var root = pvp.matchPanel.transform;
        bool result = pvp.resultText != null && !string.IsNullOrEmpty(pvp.resultText.text);
        var approvedResult = DeepFind(root, "ResultVisualRoot");
        if (result && approvedResult != null)
        {
            SetActive(DeepFind(root, "BoardPvpMatchLogo"), false);
            SetActive(DeepFind(root, "BoardVsPlayerCard"), false);
            SetActive(DeepFind(root, "BoardVsOpponentCard"), false);
            SetActive(DeepFind(root, "BoardVsBadge"), false);
            SetActive(DeepFind(root, "BoardVsBurstVector"), false);
            SetActive(DeepFind(root, "ExactMatchLogo"), false);
            SetActive(DeepFind(root, "ExactMatchSeven"), false);
            SetActive(DeepFind(root, "ExactMatchThree"), false);
            SetActive(DeepFind(root, "BoardPvpResultLogo"), false);
            SetActive(DeepFind(root, "BoardPvpResultPlayer"), false);
            SetActive(DeepFind(root, "BoardPvpResultStats"), false);
            SetActive(DeepFind(root, "BoardPvpTrophyVector"), false);
            approvedResult.SetAsLastSibling();
            return;
        }
        if (result)
        {
            ApplyPvpResult(pvp, root);
            return;
        }

        SetActive(DeepFind(root, "ExactMatchLogo"), false);
        SetActive(DeepFind(root, "ExactMatchSeven"), false);
        SetActive(DeepFind(root, "ExactMatchThree"), false);
        SetActive(DeepFind(root, "BoardVsPlayerCard"), true);
        SetActive(DeepFind(root, "BoardVsOpponentCard"), true);
        SetActive(DeepFind(root, "BoardVsBadge"), true);
        if (pvp.historyText != null) pvp.historyText.color = White;
        AddImage(root, "BoardPvpMatchLogo", logo,
            new Vector2(0f, 820f), new Vector2(330f, 180f), true);
        BuildVersusCards(root, new Vector2(0f, 575f), 360f, 320f);

        if (pvp.turnText != null)
        {
            pvp.turnText.fontSize = 34f;
            pvp.turnText.fontStyle = FontStyles.Bold;
            pvp.turnText.color = White;
            Place(pvp.turnText.rectTransform, new Vector2(0f, 325f), new Vector2(830f, 105f));
            Responsive(pvp.turnText, 21f);
        }
        if (pvp.guessInput != null)
            PlaceInput(pvp.guessInput, new Vector2(0f, 115f), new Vector2(470f, 98f), PanelDark);
        if (pvp.guessButton != null)
        {
            var button = pvp.guessButton.GetComponent<Button>();
            if (button != null)
            {
                Place((RectTransform)button.transform, new Vector2(0f, -10f), new Vector2(470f, 98f));
                StyleButton(button, Gold, Ink, GoldDark, 4f);
            }
        }
        if (pvp.historyText != null)
        {
            pvp.historyText.color = White;
            pvp.historyText.fontSize = 24f;
            Place(pvp.historyText.rectTransform, new Vector2(0f, -235f), new Vector2(760f, 180f));
            Responsive(pvp.historyText, 17f);
        }
        if (pvp.signalsRoot != null)
            StyleSignalButtons(pvp.signalsRoot.transform);
    }

    void ApplyPvpResult(PvpGameController pvp, Transform root)
    {
        SetActive(DeepFind(root, "BoardVsPlayerCard"), false);
        SetActive(DeepFind(root, "BoardVsOpponentCard"), false);
        SetActive(DeepFind(root, "BoardVsBadge"), false);
        if (pvp.historyText != null) pvp.historyText.color = Color.clear;
        AddImage(root, "BoardPvpResultLogo", logo,
            new Vector2(0f, 805f), new Vector2(330f, 180f), true);
        AddImage(root, "BoardPvpResultPlayer", player,
            new Vector2(-285f, 280f), new Vector2(390f, 430f), true);

        if (pvp.resultText != null)
        {
            pvp.resultText.fontSize = 52f;
            pvp.resultText.fontStyle = FontStyles.Bold;
            pvp.resultText.color = Gold;
            pvp.resultText.alignment = TextAlignmentOptions.Center;
            Place(pvp.resultText.rectTransform, new Vector2(210f, 340f), new Vector2(500f, 250f));
            Responsive(pvp.resultText, 28f);
        }

        var stats = EnsureImage(root, "BoardPvpResultStats");
        stats.sprite = RuntimeUI.RoundedRectSprite;
        stats.type = Image.Type.Sliced;
        stats.color = Panel;
        stats.raycastTarget = false;
        Place(stats.rectTransform, new Vector2(205f, 55f), new Vector2(500f, 210f));
        EnsureOutline(stats.gameObject, Purple, 2f);

        var statsText = EnsureText(stats.transform, "BoardPvpResultStatsText");
        statsText.text = L10n.Get("stats_wins").ToUpperInvariant() + "  " + GameStats.Wins +
                         "     " + L10n.Get("stats_streak").ToUpperInvariant() + "  " + GameStats.CurrentStreak;
        statsText.fontSize = 28f;
        statsText.fontStyle = FontStyles.Bold;
        statsText.color = White;
        statsText.alignment = TextAlignmentOptions.Center;
        RuntimeUI.Stretch(statsText.gameObject);
        Responsive(statsText, 18f);

        if (pvp.rematchButton != null)
        {
            var rematch = pvp.rematchButton.GetComponent<Button>();
            if (rematch != null)
            {
                Place((RectTransform)rematch.transform, new Vector2(0f, -250f), new Vector2(520f, 112f));
                StyleButton(rematch, Blue, White, Cyan, 3f);
                SetButtonMainText(rematch, L10n.Get("rematch").ToUpperInvariant(), 38f);
            }
        }
        if (pvp.rematchSecretInput != null && pvp.rematchSecretInput.gameObject.activeSelf)
            PlaceInput(pvp.rematchSecretInput, new Vector2(0f, -395f), new Vector2(500f, 98f), PanelDark);
        if (pvp.rematchStatusText != null)
        {
            pvp.rematchStatusText.color = White;
            pvp.rematchStatusText.fontSize = 24f;
            Place(pvp.rematchStatusText.rectTransform, new Vector2(0f, -515f), new Vector2(700f, 100f));
            Responsive(pvp.rematchStatusText, 17f);
        }
    }

    void BuildVersusCards(Transform root, Vector2 center, float cardWidth, float cardHeight)
    {
        float x = cardWidth * 0.58f;
        var left = EnsureImage(root, "BoardVsPlayerCard");
        left.gameObject.SetActive(true);
        left.sprite = RuntimeUI.RoundedRectSprite;
        left.type = Image.Type.Sliced;
        left.color = new Color(0.02f, 0.36f, 0.72f, 0.94f);
        left.raycastTarget = false;
        Place(left.rectTransform, center + new Vector2(-x, 0f), new Vector2(cardWidth, cardHeight));
        EnsureOutline(left.gameObject, Cyan, 3f);
        AddImage(left.transform, "BoardVsPlayer", player,
            new Vector2(0f, 10f), new Vector2(cardWidth * 0.70f, cardHeight * 0.78f), true);
        var you = EnsureText(left.transform, "BoardVsYou");
        you.text = L10n.Get("you").ToUpperInvariant();
        you.fontSize = 28f;
        you.fontStyle = FontStyles.Bold;
        you.color = White;
        you.alignment = TextAlignmentOptions.Center;
        Place(you.rectTransform, new Vector2(0f, cardHeight * 0.39f), new Vector2(cardWidth * 0.72f, 48f));

        var right = EnsureImage(root, "BoardVsOpponentCard");
        right.gameObject.SetActive(true);
        right.sprite = RuntimeUI.RoundedRectSprite;
        right.type = Image.Type.Sliced;
        right.color = new Color(0.67f, 0.02f, 0.28f, 0.94f);
        right.raycastTarget = false;
        Place(right.rectTransform, center + new Vector2(x, 0f), new Vector2(cardWidth, cardHeight));
        EnsureOutline(right.gameObject, Pink, 3f);
        AddImage(right.transform, "BoardVsOpponent", opponent,
            new Vector2(0f, 10f), new Vector2(cardWidth * 0.70f, cardHeight * 0.78f), true);
        var them = EnsureText(right.transform, "BoardVsThem");
        them.text = L10n.Get("prebattle_opponent").ToUpperInvariant();
        them.fontSize = 28f;
        them.fontStyle = FontStyles.Bold;
        them.color = White;
        them.alignment = TextAlignmentOptions.Center;
        Place(them.rectTransform, new Vector2(0f, cardHeight * 0.39f), new Vector2(cardWidth * 0.78f, 48f));

        var vs = EnsureText(root, "BoardVsBadge");
        vs.gameObject.SetActive(true);
        vs.text = L10n.Get("versus");
        vs.fontSize = 72f;
        vs.fontStyle = FontStyles.Bold;
        vs.color = Gold;
        vs.alignment = TextAlignmentOptions.Center;
        Place(vs.rectTransform, center, new Vector2(160f, 120f));
        EnsureOutline(vs.gameObject, Hex(0x4A, 0x10, 0x00), 4f);
        vs.transform.SetAsLastSibling();
    }

    void ApplySimplePanel(Transform root)
    {
        if (root == null || !root.gameObject.activeInHierarchy) return;

        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.transform.name.StartsWith("Board")) continue;
            string name = image.transform.name;
            if (Contains(name, "Card") || Contains(name, "Frame"))
            {
                image.sprite = RuntimeUI.RoundedRectSprite;
                image.type = Image.Type.Sliced;
                image.color = Panel;
                EnsureOutline(image.gameObject, Purple, 2f);
            }
        }

        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            if (button.transform.name.StartsWith("Board")) continue;
            if (Contains(button.transform.name, "Confirm") || Contains(button.transform.name, "Start"))
                StyleButton(button, Gold, Ink, GoldDark, 4f);
            else if (Contains(button.transform.name, "Back") || Contains(button.transform.name, "Close") || Contains(button.transform.name, "Cancel"))
                StyleButton(button, Panel, White, Purple, 2f);
            else
                StyleButton(button, PurpleDark, White, Purple, 2f);
        }
    }

    void StyleExistingGameButtons(Transform root)
    {
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            string name = button.transform.name;
            if (Contains(name, "Higher")) StyleButton(button, Blue, White, Cyan, 3f);
            else if (Contains(name, "Lower")) StyleButton(button, Pink, White, Hex(0xFF, 0x55, 0x9B), 3f);
            else if (Contains(name, "Correct") || Contains(name, "Confirm") || Contains(name, "Submit"))
                StyleButton(button, Gold, Ink, GoldDark, 4f);
            else if (Contains(name, "Lock")) StyleButton(button, Purple, White, Hex(0xA8, 0x58, 0xFF), 3f);
            else if (Contains(name, "Stop") || Contains(name, "Back")) StyleButton(button, Panel, White, Purple, 2f);
        }
    }

    void StyleSignalButtons(Transform root)
    {
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            StyleButton(button, PurpleDark, White, Purple, 2f);
            var rect = button.transform as RectTransform;
            if (rect != null)
                rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, 90f), Mathf.Max(rect.sizeDelta.y, 78f));
        }
    }

    static void PlaceInput(TMP_InputField input, Vector2 position, Vector2 size, Color fill)
    {
        if (input == null) return;
        Place(input.GetComponent<RectTransform>(), position, size);
        var image = input.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = RuntimeUI.RoundedRectSprite;
            image.type = Image.Type.Sliced;
            image.color = fill;
            EnsureOutline(image.gameObject, Purple, 2f);
        }
        if (input.textComponent != null)
        {
            input.textComponent.color = White;
            input.textComponent.fontStyle = FontStyles.Bold;
            Responsive(input.textComponent, 20f);
        }
        var placeholder = input.placeholder as TMP_Text;
        if (placeholder != null)
        {
            placeholder.color = Muted;
            Responsive(placeholder, 18f);
        }
    }

    static void EnsureTextPlate(Transform parent, string name, Vector2 position, Vector2 size)
    {
        if (parent == null) return;
        var plate = EnsureImage(parent, name);
        plate.sprite = RuntimeUI.RoundedRectSprite;
        plate.type = Image.Type.Sliced;
        plate.color = White;
        plate.raycastTarget = false;
        Place(plate.rectTransform, position, size);
        plate.transform.SetAsFirstSibling();
    }

    static void AddRibbon(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var ribbon = EnsureImage(parent, name);
        ribbon.sprite = RuntimeUI.RoundedRectSprite;
        ribbon.type = Image.Type.Sliced;
        ribbon.color = PurpleDark;
        ribbon.raycastTarget = false;
        Place(ribbon.rectTransform, position, size);
        EnsureOutline(ribbon.gameObject, Purple, 2f);
    }

    static void SetButtonCopy(Button button, string title, string subtitle,
        float titleSize, float subtitleSize)
    {
        if (button == null) return;
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = title;
            label.fontSize = titleSize;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            Place(label.rectTransform, string.IsNullOrEmpty(subtitle) ? Vector2.zero : new Vector2(0f, 28f),
                new Vector2(((RectTransform)button.transform).sizeDelta.x - 40f, string.IsNullOrEmpty(subtitle) ? 120f : 80f));
            Responsive(label, 20f);
        }

        var sub = EnsureText(button.transform, "BoardButtonSubtitle");
        sub.text = subtitle;
        sub.fontSize = subtitleSize;
        sub.fontStyle = FontStyles.Bold;
        sub.color = label == null ? White : label.color;
        sub.alignment = TextAlignmentOptions.Center;
        sub.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
        if (sub.gameObject.activeSelf)
        {
            Place(sub.rectTransform, new Vector2(0f, -48f),
                new Vector2(((RectTransform)button.transform).sizeDelta.x - 50f, 60f));
            Responsive(sub, 16f);
        }
    }

    static void SetButtonMainText(Button button, string copy, float size)
    {
        var label = button == null ? null : button.GetComponentInChildren<TMP_Text>(true);
        if (label == null) return;
        label.text = copy;
        label.fontSize = size;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        RuntimeUI.Stretch(label.gameObject);
        Responsive(label, 20f);
    }

    static void MoveMainLabel(Button button, Vector2 position, Vector2 size)
    {
        if (button == null) return;
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            Place(label.rectTransform, position, size);
            label.transform.SetAsLastSibling();
        }
    }

    static void StyleButton(Button button, Color fill, Color label, Color edge, float edgeSize)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = RuntimeUI.RoundedRectSprite;
            image.type = Image.Type.Sliced;
            image.color = fill;
            EnsureOutline(image.gameObject, edge, edgeSize);
            EnsureShadow(image.gameObject, new Color(0f, 0f, 0f, 0.72f), 9f);
        }

        foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.transform.name.StartsWith("Board")) continue;
            text.color = label;
            text.fontStyle = FontStyles.Bold;
            Responsive(text, 16f);
        }
    }

    static void EnsureIconText(Transform parent, string name, string glyph,
        Vector2 position, Vector2 size, float fontSize, Color color)
    {
        var text = EnsureText(parent, name);
        text.text = glyph;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        Place(text.rectTransform, position, size);
    }

    static void EnsureIconText(Transform parent, string name, string glyph,
        Vector2 position, Vector2 size, float fontSize, Color plateColor, Color glyphColor)
    {
        var plate = EnsureImage(parent, name + "Plate");
        plate.sprite = RuntimeUI.RoundedRectSprite;
        plate.type = Image.Type.Sliced;
        plate.color = plateColor;
        plate.raycastTarget = false;
        Place(plate.rectTransform, position, size);

        var text = EnsureText(parent, name);
        text.text = glyph;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = glyphColor;
        text.alignment = TextAlignmentOptions.Center;
        Place(text.rectTransform, position, size);
        text.transform.SetAsLastSibling();
    }

    static void AddImage(Transform parent, string name, Sprite sprite,
        Vector2 position, Vector2 size, bool preserveAspect)
    {
        if (parent == null || sprite == null) return;
        var image = EnsureImage(parent, name);
        image.gameObject.SetActive(true);
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
        Place(image.rectTransform, position, size);
        image.transform.SetAsFirstSibling();
    }

    static Image EnsureImage(Transform parent, string name)
    {
        var existing = DirectChild(parent, name);
        if (existing != null)
        {
            var found = existing.GetComponent<Image>();
            if (found != null) return found;
        }
        var go = RuntimeUI.CreateObject(name, parent);
        return go.AddComponent<Image>();
    }

    static TMP_Text EnsureText(Transform parent, string name)
    {
        var existing = DirectChild(parent, name);
        if (existing != null)
        {
            var found = existing.GetComponent<TMP_Text>();
            if (found != null) return found;
        }
        return RuntimeUI.CreateText(parent, name, "", 30, Vector2.zero,
            new Vector2(100f, 50f), White);
    }

    static Button FindButton(Transform root, string name)
    {
        var transform = DeepFind(root, name);
        return transform == null ? null : transform.GetComponent<Button>();
    }

    static Transform DirectChild(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name) return parent.GetChild(i);
        return null;
    }

    static Transform DeepFind(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var hit = DeepFind(root.GetChild(i), name);
            if (hit != null) return hit;
        }
        return null;
    }

    static T FindInScene<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }

    static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static void Responsive(TMP_Text text, float minimum)
    {
        if (text == null) return;
        float max = Mathf.Max(text.fontSize, minimum);
        text.enableAutoSizing = true;
        text.fontSizeMax = max;
        text.fontSizeMin = Mathf.Min(max, minimum);
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    static void EnsureOutline(GameObject go, Color color, float distance)
    {
        if (go == null) return;
        var outline = go.GetComponent<Outline>();
        if (outline == null) outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(distance, distance);
        outline.useGraphicAlpha = true;
    }

    static void EnsureShadow(GameObject go, Color color, float distance)
    {
        if (go == null) return;
        var shadow = go.GetComponent<Shadow>();
        if (shadow == null) shadow = go.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = new Vector2(0f, -distance);
        shadow.useGraphicAlpha = true;
    }

    static void SetActive(Transform transform, bool active)
    {
        if (transform != null && transform.gameObject.activeSelf != active)
            transform.gameObject.SetActive(active);
    }

    static bool Contains(string value, string part)
    {
        return value != null && value.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static bool IsGreek => L10n.Current == L10n.Language.Greek;

    static Color Hex(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }
}
