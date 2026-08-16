using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Applies the approved reference artwork as the literal presentation baseline.
// This is deliberately a skin over the existing controllers. It does not own
// game rules, matchmaking, room state, ads, statistics, or navigation.
public sealed class ExactReferenceVisuals : MonoBehaviour
{
    const string LogoResource = "reference/hol_logo_exact";
    const string PlayerResource = "reference/player_cyan_exact";
    const string OpponentResource = "reference/opponent_purple_exact";
    const string MascotSevenResource = "reference/mascot_7_exact";
    const string MascotThreeResource = "reference/mascot_3_exact";

    static readonly Color Depth = Hex(0x08, 0x06, 0x25);
    static readonly Color Surface = Hex(0x18, 0x0B, 0x48);
    static readonly Color SurfaceRaised = Hex(0x2B, 0x16, 0x72);
    static readonly Color Violet = Hex(0x76, 0x31, 0xE8);
    static readonly Color Cyan = Hex(0x00, 0xC8, 0xFF);
    static readonly Color Blue = Hex(0x08, 0x6E, 0xD9);
    static readonly Color Pink = Hex(0xF3, 0x28, 0x91);
    static readonly Color Gold = Hex(0xFF, 0xC4, 0x10);
    static readonly Color Orange = Hex(0xF4, 0x75, 0x0A);
    static readonly Color Success = Hex(0x62, 0xD6, 0x2D);
    static readonly Color NearWhite = Hex(0xF7, 0xF5, 0xFF);
    static readonly Color Muted = Hex(0xC7, 0xB9, 0xEA);
    static readonly Color Ink = Hex(0x16, 0x0D, 0x24);

    Sprite logo;
    Sprite playerPortrait;
    Sprite opponentPortrait;
    Sprite mascotSeven;
    Sprite mascotThree;
    float nextRefresh;
    int lastVisualSignature = int.MinValue;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        // Static scene callbacks survive the destruction caused by LoadScene(Single).
        // Re-register defensively so domain-reload-disabled play sessions cannot
        // accumulate duplicate callbacks.
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallForScene(scene);
    }

    static void InstallForScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;

        // Prefer the canvas that owns the menu controller's panel. This avoids
        // styling unrelated SDK/debug/world-space canvases that may exist in the
        // same scene. Splash and other scenes fall back to their first root,
        // screen-space canvas.
        Canvas canvas = null;
        var menu = FindInScene<MenuManager>(scene);
        if (menu != null && menu.mainMenuPanel != null)
            canvas = menu.mainMenuPanel.GetComponentInParent<Canvas>();

        if (!IsOwnedCanvas(canvas, scene))
        {
            canvas = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var candidate in root.GetComponentsInChildren<Canvas>(true))
                {
                    if (!IsOwnedCanvas(candidate, scene)) continue;
                    canvas = candidate;
                    break;
                }
                if (canvas != null) break;
            }
        }

        if (canvas != null && canvas.GetComponent<ExactReferenceVisuals>() == null)
            canvas.gameObject.AddComponent<ExactReferenceVisuals>();
    }

    static bool IsOwnedCanvas(Canvas canvas, Scene scene)
    {
        return canvas != null &&
               canvas.gameObject.scene == scene &&
               canvas.isRootCanvas &&
               canvas.renderMode != RenderMode.WorldSpace;
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

    void Awake()
    {
        // The earlier Converging Light pass is still scene-authored in the
        // existing project. Stop only the copy in this scene before Start so
        // unrelated additive scenes/canvases are never modified.
        var scene = gameObject.scene;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var legacy in root.GetComponentsInChildren<DesignRuntimeWiring>(true))
                legacy.enabled = false;

        logo = Resources.Load<Sprite>(LogoResource);
        playerPortrait = Resources.Load<Sprite>(PlayerResource);
        opponentPortrait = Resources.Load<Sprite>(OpponentResource);
        mascotSeven = Resources.Load<Sprite>(MascotSevenResource);
        mascotThree = Resources.Load<Sprite>(MascotThreeResource);
        if (logo == null)
            Debug.LogError("[ExactReferenceVisuals] Missing Resources/" + LogoResource +
                ". The approved HOL logo cannot render.");
        if (playerPortrait == null || opponentPortrait == null)
            Debug.LogError("[ExactReferenceVisuals] Approved character portraits are missing.");
        if (mascotSeven == null || mascotThree == null)
            Debug.LogError("[ExactReferenceVisuals] Approved number mascots are missing.");
    }

    void OnEnable()
    {
        L10n.OnLanguageChanged += ApplyAll;
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= ApplyAll;
    }

    IEnumerator Start()
    {
        // Apply once before the first rendered frame. The repeated passes below
        // still pick up UI surfaces that other Start methods build at runtime.
        ApplyAll();

        // Runtime-built PvP and Daily Hunt surfaces appear after their own
        // Start methods. Reapply through the first few frames, then refresh only
        // when this canvas' visual hierarchy changes.
        for (int i = 0; i < 4; i++)
        {
            yield return null;
            ApplyAll();
        }
        lastVisualSignature = VisualSignature();
    }

    void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.5f;

        int signature = VisualSignature();
        if (signature == lastVisualSignature) return;

        ApplyAll();
        // ApplyAll may itself create exact-reference children. Record the final
        // hierarchy so those intentional additions do not trigger another pass.
        lastVisualSignature = VisualSignature();
    }

    int VisualSignature()
    {
        unchecked
        {
            int signature = 17;
            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                signature = signature * 31 + child.GetInstanceID();
                signature = signature * 31 + (child.gameObject.activeSelf ? 1 : 0);
            }
            signature = signature * 31 + GetComponentsInChildren<Image>(true).Length;
            signature = signature * 31 + GetComponentsInChildren<Button>(true).Length;
            signature = signature * 31 + GetComponentsInChildren<TMP_Text>(true).Length;
            signature = signature * 31 + GetComponentsInChildren<TMP_InputField>(true).Length;
            return signature;
        }
    }

    void ApplyAll()
    {
        // ExactReferenceVisuals owns exactly one root canvas. Keep all name-based
        // styling underneath that canvas instead of mutating every Canvas in the
        // process, which protects SDK overlays, debug UI, and world-space UI.
        var root = transform;
        ApplyBackdrop(root);
        StylePanels(root);
        StyleInputs(root);
        StyleButtons(root);
        StyleText(root);
        ApplyCharacterCards(root);
        ApplyScreenLayouts(root);

        var menu = FindInScene<MenuManager>(gameObject.scene);
        if (menu != null && menu.mainMenuPanel != null &&
            menu.mainMenuPanel.GetComponentInParent<Canvas>() == GetComponent<Canvas>())
            BuildMainMenu(menu.mainMenuPanel.transform);
    }

    void ApplyBackdrop(Transform canvasRoot)
    {
        DisableDirectChild(canvasRoot, "BackdropDepth");
        DisableDirectChild(canvasRoot, "BackdropNumbers");

        var existing = DirectChild(canvasRoot, "ExactReferenceBackdrop");
        if (existing == null)
        {
            var go = RuntimeUI.CreateObject("ExactReferenceBackdrop", canvasRoot);
            RuntimeUI.Stretch(go);
            var image = go.AddComponent<Image>();
            image.sprite = ConvergingLight.VerticalGradient(Hex(0x07, 0x04, 0x1D), Hex(0x1A, 0x06, 0x43));
            image.color = Color.white;
            image.raycastTarget = false;
            go.transform.SetAsFirstSibling();
        }
    }

    void BuildMainMenu(Transform root)
    {
        // BACKROUND is the menu container as well as the legacy image. Keep its
        // children and navigation intact, but reveal the exact canvas backdrop.
        var legacyBackground = root.GetComponent<Image>();
        if (legacyBackground != null) legacyBackground.enabled = false;

        if (logo != null)
        {
            var logoImage = EnsureImage(root, "ExactHOLLogo");
            logoImage.sprite = logo;
            logoImage.color = Color.white;
            logoImage.preserveAspect = true;
            logoImage.raycastTarget = false;
            Place(logoImage.rectTransform, new Vector2(0f, 535f), new Vector2(650f, 360f));
        }

        var tagline = EnsureText(root, "ExactTagline");
        tagline.text = IsGreek
            ? "ΜΑΝΤΕΨΕ ΤΟΝ ΑΡΙΘΜΟ!"
            : "GUESS THE NUMBER!";
        tagline.fontSize = IsGreek ? 49f : 56f;
        tagline.fontStyle = FontStyles.Bold;
        tagline.color = NearWhite;
        tagline.alignment = TextAlignmentOptions.Center;
        Place(tagline.rectTransform, new Vector2(0f, 322f), new Vector2(900f, 90f));
        ConfigureResponsiveText(tagline, 30f);

        if (playerPortrait != null)
        {
            var playerHero = EnsureImage(root, "ExactPlayerHero");
            playerHero.sprite = playerPortrait;
            playerHero.preserveAspect = true;
            playerHero.raycastTarget = false;
            Place(playerHero.rectTransform, new Vector2(-335f, 205f), new Vector2(320f, 320f));
        }

        if (opponentPortrait != null)
        {
            var opponentHero = EnsureImage(root, "ExactOpponentHero");
            opponentHero.sprite = opponentPortrait;
            opponentHero.preserveAspect = true;
            opponentHero.raycastTarget = false;
            Place(opponentHero.rectTransform, new Vector2(335f, 205f), new Vector2(320f, 320f));
        }

        if (mascotSeven != null)
        {
            var seven = EnsureImage(root, "ExactMascotSeven");
            seven.sprite = mascotSeven;
            seven.preserveAspect = true;
            seven.raycastTarget = false;
            Place(seven.rectTransform, new Vector2(-142f, 188f), new Vector2(150f, 185f));
        }

        if (mascotThree != null)
        {
            var three = EnsureImage(root, "ExactMascotThree");
            three.sprite = mascotThree;
            three.preserveAspect = true;
            three.raycastTarget = false;
            Place(three.rectTransform, new Vector2(142f, 188f), new Vector2(150f, 185f));
        }

        var profile = EnsureImage(root, "ExactPlayerChip");
        profile.sprite = RuntimeUI.RoundedRectSprite;
        profile.type = Image.Type.Sliced;
        profile.color = new Color(0.03f, 0.20f, 0.48f, 0.94f);
        Place(profile.rectTransform, new Vector2(-305f, 825f), new Vector2(430f, 92f));
        EnsureOutline(profile.gameObject, Cyan, 3f);

        var profileText = EnsureText(profile.transform, "ExactPlayerChipText");
        string player = PlayerPrefs.GetString("PlayerName", L10n.Get("player_default"));
        string streak = L10n.Get("stats_streak").ToUpperInvariant();
        profileText.text = player.ToUpperInvariant() + "   " + streak + " " + GameStats.CurrentStreak;
        profileText.fontSize = 31f;
        profileText.fontStyle = FontStyles.Bold;
        profileText.color = NearWhite;
        RuntimeUI.Stretch(profileText.gameObject);
        ConfigureResponsiveText(profileText, 18f);

        AddConfetti(root);

        var play = FindButton("ButtonPlay");
        if (play != null)
        {
            Place((RectTransform)play.transform, new Vector2(0f, 80f), new Vector2(850f, 145f));
            StyleButton(play, Gold, Ink, true);
            SetButtonCopy(play,
                IsGreek ? "ΠΑΙΞΕ SOLO" : "PLAY SOLO",
                IsGreek ? "Νίκησε τον προσαρμοστικό αντίπαλο" : "Beat the adaptive opponent");
        }

        var room = FindButton("ButtonPvP");
        if (room != null)
        {
            Place((RectTransform)room.transform, new Vector2(0f, -125f), new Vector2(850f, 155f));
            StyleButton(room, Blue, NearWhite, false);
            SetButtonCopy(room,
                IsGreek ? "ΠΑΙΞΕ ΜΕ ΦΙΛΟ" : "PLAY WITH A FRIEND",
                IsGreek ? "Δημιούργησε ή μπες σε ιδιωτικό δωμάτιο" : "Create or join a private room");
        }

        var daily = FindButton("DailyHuntButton");
        if (daily != null)
        {
            Place((RectTransform)daily.transform, new Vector2(0f, -330f), new Vector2(850f, 155f));
            StyleButton(daily, Orange, NearWhite, false);
            SetButtonCopy(daily,
                IsGreek ? "ΚΑΘΗΜΕΡΙΝΟ ΚΥΝΗΓΙ" : "DAILY HUNT",
                IsGreek ? "Ένας κοινός αριθμός κάθε μέρα" : "One shared number every day");
            if (mascotSeven != null)
            {
                var image = EnsureImage(daily.transform, "ExactDailyMascot");
                image.sprite = mascotSeven;
                image.preserveAspect = true;
                image.raycastTarget = false;
                Place(image.rectTransform, new Vector2(335f, 0f), new Vector2(145f, 145f));
            }
        }

        var settings = FindButton("Buttonsettings");
        if (settings != null)
        {
            Place((RectTransform)settings.transform, new Vector2(440f, 825f), new Vector2(92f, 92f));
            StyleButton(settings, SurfaceRaised, NearWhite, false);
        }

        var quit = DeepFind(root, "ButtonQuit");
        if (quit != null) quit.gameObject.SetActive(false);
        var oldStats = DeepFind(root, "StatsLabel");
        if (oldStats != null) oldStats.gameObject.SetActive(false);
    }

    void ApplyCharacterCards(Transform root)
    {
        var playerCard = DeepFind(root, "PlayerCard");
        if (playerCard != null && playerPortrait != null)
        {
            var image = EnsureImage(playerCard, "ExactPlayerPortrait");
            image.sprite = playerPortrait;
            image.preserveAspect = true;
            image.raycastTarget = false;
            Place(image.rectTransform, Vector2.zero, new Vector2(250f, 250f));
            image.transform.SetAsFirstSibling();
        }

        var opponentCard = DeepFind(root, "OpponentCard");
        if (opponentCard != null && opponentPortrait != null)
        {
            var image = EnsureImage(opponentCard, "ExactOpponentPortrait");
            image.sprite = opponentPortrait;
            image.preserveAspect = true;
            image.raycastTarget = false;
            Place(image.rectTransform, Vector2.zero, new Vector2(250f, 250f));
            image.transform.SetAsFirstSibling();
        }
    }

    void ApplyScreenLayouts(Transform root)
    {
        LayoutSplash(root);
        LayoutPvpMenu(DeepFind(root, "PvPMenuPanel"));
        LayoutCreateRoom(DeepFind(root, "PvPCreatePanel"));
        LayoutJoinRoom(DeepFind(root, "PvPJoinPanel"));
        LayoutPvpMatch(DeepFind(root, "PvPMatchPanel"));
        LayoutDailyHunt(DeepFind(root, "DailyHuntPanel"));
        LayoutSearching(DeepFind(root, "PanelSearching"));
        LayoutSimpleScreen(DeepFind(root, "PanelPlay"), "ExactPlayLogo");
        LayoutSimpleScreen(DeepFind(root, "PanelSettings"), "ExactSettingsLogo");
        LayoutSimpleScreen(DeepFind(root, "PanelGAME"), "ExactSoloLogo");
        LayoutDialog(DeepFind(root, "ConsentPanel"), false);
        LayoutDialog(DeepFind(root, "ForceUpdatePanel"), true);
    }

    void LayoutSplash(Transform root)
    {
        if (root == null || FindInScene<SplashLoader>(gameObject.scene) == null) return;

        // The approved square reference is a clean logo, confetti and deep
        // purple field. Suppress the older number-field and seam treatment
        // while leaving SplashLoader and its gold progress line untouched.
        var oldBackground = DirectChild(root, "Panel");
        if (oldBackground != null)
        {
            var image = oldBackground.GetComponent<Image>();
            if (image != null) image.enabled = false;
        }

        string[] superseded =
        {
            "Image", "NumberField", "SeamBloom", "Seam", "Tagline"
        };
        for (int i = 0; i < superseded.Length; i++)
        {
            var item = DirectChild(root, superseded[i]);
            if (item != null) item.gameObject.SetActive(false);
        }

        AddConfetti(root);
        AddExactImage(root, "ExactSplashLogo", logo,
            new Vector2(0f, 70f), new Vector2(820f, 540f));
    }

    void LayoutPvpMenu(Transform panel)
    {
        if (panel == null) return;

        AddExactImage(panel, "ExactPvpMenuLogo", logo,
            new Vector2(0f, 690f), new Vector2(440f, 290f));
        AddExactImage(panel, "ExactPvpMenuSeven", mascotSeven,
            new Vector2(-400f, 80f), new Vector2(210f, 260f));
        AddExactImage(panel, "ExactPvpMenuThree", mascotThree,
            new Vector2(400f, 80f), new Vector2(210f, 260f));

        PlaceNamed(panel, "Title", new Vector2(0f, 390f), new Vector2(850f, 110f));
        PlaceNamed(panel, "CreateButton", new Vector2(0f, 130f), new Vector2(620f, 120f));
        PlaceNamed(panel, "JoinButton", new Vector2(0f, -30f), new Vector2(620f, 120f));
        PlaceNamed(panel, "CloseButton", new Vector2(0f, -250f), new Vector2(330f, 84f));
    }

    void LayoutCreateRoom(Transform panel)
    {
        if (panel == null) return;

        AddExactImage(panel, "ExactCreateLogo", logo,
            new Vector2(0f, 800f), new Vector2(330f, 215f));
        AddExactImage(panel, "ExactCreateSeven", mascotSeven,
            new Vector2(-430f, 0f), new Vector2(150f, 190f));
        AddExactImage(panel, "ExactCreateThree", mascotThree,
            new Vector2(430f, 0f), new Vector2(150f, 190f));

        PlaceNamed(panel, "Title", new Vector2(0f, 605f), new Vector2(850f, 90f));
        PlaceNamed(panel, "SecretInput", new Vector2(0f, 430f), new Vector2(500f, 92f));
        PlaceNamed(panel, "ConfirmCreateButton", new Vector2(0f, 305f), new Vector2(500f, 92f));
        PlaceNamed(panel, "RoomCodeFrame", new Vector2(0f, 55f), new Vector2(760f, 280f));
        PlaceNamed(panel, "CopyButton", new Vector2(0f, -170f), new Vector2(500f, 92f));
        PlaceNamed(panel, "StatusFrame", new Vector2(0f, -345f), new Vector2(860f, 150f));
        PlaceNamed(panel, "BackButton", new Vector2(0f, -560f), new Vector2(330f, 84f));
    }

    void LayoutJoinRoom(Transform panel)
    {
        if (panel == null) return;

        AddExactImage(panel, "ExactJoinLogo", logo,
            new Vector2(0f, 760f), new Vector2(360f, 235f));
        AddExactImage(panel, "ExactJoinPlayer", playerPortrait,
            new Vector2(-365f, 70f), new Vector2(250f, 285f));
        AddExactImage(panel, "ExactJoinOpponent", opponentPortrait,
            new Vector2(365f, 70f), new Vector2(250f, 285f));

        PlaceNamed(panel, "Title", new Vector2(0f, 520f), new Vector2(850f, 100f));
        PlaceNamed(panel, "JoinCard", new Vector2(0f, 90f), new Vector2(650f, 440f));
        PlaceNamed(panel, "CodeInput", new Vector2(0f, 250f), new Vector2(480f, 92f));
        PlaceNamed(panel, "SecretInput", new Vector2(0f, 115f), new Vector2(480f, 92f));
        PlaceNamed(panel, "ConfirmJoinButton", new Vector2(0f, -25f), new Vector2(480f, 92f));
        PlaceNamed(panel, "Status", new Vector2(0f, -235f), new Vector2(900f, 120f));
        PlaceNamed(panel, "BackButton", new Vector2(0f, -430f), new Vector2(330f, 84f));
    }

    void LayoutPvpMatch(Transform panel)
    {
        if (panel == null) return;

        AddExactImage(panel, "ExactMatchLogo", logo,
            new Vector2(0f, 865f), new Vector2(340f, 220f));
        AddExactImage(panel, "ExactMatchSeven", mascotSeven,
            new Vector2(-450f, 405f), new Vector2(105f, 135f));
        AddExactImage(panel, "ExactMatchThree", mascotThree,
            new Vector2(450f, 405f), new Vector2(105f, 135f));

        var playerCard = DeepFind(panel, "PlayerCard");
        var opponentCard = DeepFind(panel, "OpponentCard");
        if (playerCard != null)
        {
            Place((RectTransform)playerCard, new Vector2(-255f, 650f), new Vector2(460f, 350f));
            PlaceNamed(playerCard, "Caption", new Vector2(0f, 145f), new Vector2(420f, 42f));
            PlaceNamed(playerCard, "Name", new Vector2(0f, -132f), new Vector2(420f, 68f));
        }
        if (opponentCard != null)
        {
            Place((RectTransform)opponentCard, new Vector2(255f, 650f), new Vector2(460f, 350f));
            PlaceNamed(opponentCard, "Opponent", new Vector2(0f, -128f), new Vector2(420f, 82f));
        }

        PlaceNamed(panel, "VsBadge", new Vector2(0f, 650f), new Vector2(120f, 90f));
        PlaceNamed(panel, "PromptBanner", new Vector2(0f, 410f), new Vector2(900f, 170f));
        PlaceNamed(panel, "GuessCard", new Vector2(-255f, -160f), new Vector2(540f, 900f));
        PlaceNamed(panel, "SignalBubble", new Vector2(285f, 250f), new Vector2(470f, 170f));
        PlaceNamed(panel, "HistoryCard", new Vector2(285f, -35f), new Vector2(470f, 320f));
        PlaceNamed(panel, "TipCard", new Vector2(285f, -390f), new Vector2(470f, 320f));

        var playerImage = playerCard == null ? null : DeepFind(playerCard, "ExactPlayerPortrait");
        if (playerImage != null)
            Place((RectTransform)playerImage, new Vector2(0f, 15f), new Vector2(255f, 255f));
        var opponentImage = opponentCard == null ? null : DeepFind(opponentCard, "ExactOpponentPortrait");
        if (opponentImage != null)
            Place((RectTransform)opponentImage, new Vector2(0f, 15f), new Vector2(255f, 255f));
    }

    void LayoutDailyHunt(Transform panel)
    {
        if (panel == null) return;

        AddExactImage(panel, "ExactDailyLogo", logo,
            new Vector2(0f, 790f), new Vector2(360f, 235f));
        AddExactImage(panel, "ExactDailySeven", mascotSeven,
            new Vector2(-375f, 200f), new Vector2(190f, 240f));
        AddExactImage(panel, "ExactDailyThree", mascotThree,
            new Vector2(375f, 200f), new Vector2(190f, 240f));

        PlaceNamed(panel, "Card", new Vector2(0f, -10f), new Vector2(920f, 1460f));
        PlaceNamed(panel, "Title", new Vector2(0f, 590f), new Vector2(780f, 80f));
        PlaceNamed(panel, "Status", new Vector2(0f, 420f), new Vector2(720f, 150f));
        PlaceNamed(panel, "Trail", new Vector2(0f, 220f), new Vector2(760f, 90f));
    }

    void LayoutSearching(Transform panel)
    {
        if (panel == null) return;

        AddExactImage(panel, "ExactSearchingLogo", logo,
            new Vector2(0f, 700f), new Vector2(440f, 290f));
        AddExactImage(panel, "ExactSearchingPlayer", playerPortrait,
            new Vector2(-280f, 170f), new Vector2(380f, 440f));
        AddExactImage(panel, "ExactSearchingOpponent", opponentPortrait,
            new Vector2(280f, 170f), new Vector2(380f, 440f));
        var vs = EnsureText(panel, "ExactSearchingVs");
        vs.text = "VS";
        vs.fontSize = 72f;
        vs.fontStyle = FontStyles.Bold;
        vs.color = Gold;
        vs.alignment = TextAlignmentOptions.Center;
        Place(vs.rectTransform, new Vector2(0f, 175f), new Vector2(150f, 100f));
    }

    void LayoutSimpleScreen(Transform panel, string imageName)
    {
        if (panel == null) return;
        AddExactImage(panel, imageName, logo,
            new Vector2(0f, 800f), new Vector2(330f, 215f));
    }

    void LayoutDialog(Transform panel, bool blockingUpdate)
    {
        if (panel == null) return;
        var card = DeepFind(panel, "Card");
        if (card == null) return;

        Place((RectTransform)card, Vector2.zero,
            blockingUpdate ? new Vector2(700f, 700f) : new Vector2(680f, 590f));
        AddExactImage(card, blockingUpdate ? "ExactUpdateLogo" : "ExactConsentLogo", logo,
            new Vector2(0f, blockingUpdate ? 260f : 210f), new Vector2(270f, 175f));
        PlaceNamed(card, "Message", new Vector2(0f, blockingUpdate ? 70f : 25f),
            new Vector2(590f, blockingUpdate ? 190f : 170f));
        PlaceNamed(card, blockingUpdate ? "ConfirmUpdateButton" : "YesButton",
            new Vector2(0f, blockingUpdate ? -130f : -130f), new Vector2(460f, 96f));
        PlaceNamed(card, blockingUpdate ? "QuitButton" : "NoButton",
            new Vector2(0f, blockingUpdate ? -245f : -235f), new Vector2(460f, 96f));
    }

    void AddExactImage(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
    {
        if (parent == null || sprite == null) return;
        var image = EnsureImage(parent, name);
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        Place(image.rectTransform, position, size);
    }

    static void PlaceNamed(Transform root, string name, Vector2 position, Vector2 size)
    {
        if (root == null) return;
        var found = DeepFind(root, name);
        var rect = found as RectTransform;
        if (rect != null)
            Place(rect, position, size);
    }

    static void AddConfetti(Transform root)
    {
        if (DirectChild(root, "ExactConfetti") != null) return;
        var field = RuntimeUI.CreateObject("ExactConfetti", root);
        RuntimeUI.Stretch(field);
        var backdrop = DirectChild(root, "ExactReferenceBackdrop");
        field.transform.SetSiblingIndex(backdrop == null
            ? 0
            : Mathf.Min(backdrop.GetSiblingIndex() + 1, root.childCount - 1));

        Color[] colors = { Cyan, Pink, Gold, Violet, NearWhite };
        var rng = new System.Random(47031);
        for (int i = 0; i < 28; i++)
        {
            var bit = RuntimeUI.CreateObject("Confetti" + i, field.transform);
            var rect = (RectTransform)bit.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(rng.Next(8, 20), rng.Next(16, 38));
            rect.anchoredPosition = new Vector2(rng.Next(-500, 501), rng.Next(-800, 801));
            rect.localRotation = Quaternion.Euler(0f, 0f, rng.Next(0, 180));
            var image = bit.AddComponent<Image>();
            image.color = colors[i % colors.Length];
            image.raycastTarget = false;
        }
    }

    static void StylePanels(Transform root)
    {
        var exactBackdrop = DirectChild(root, "ExactReferenceBackdrop");
        var exactBackdropImage = exactBackdrop == null
            ? null
            : exactBackdrop.GetComponent<Image>();

        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            string name = image.transform.name;
            if (name.StartsWith("Exact") || name.StartsWith("Confetti"))
                continue;

            bool fullPanel = name.IndexOf("Panel", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                             image.transform.GetComponent<Button>() == null;
            bool card = name.IndexOf("Card", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("Frame", System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (fullPanel)
            {
                // RuntimeUI may have inherited a legacy scene sprite. Replace
                // it rather than tinting it, so none of the discarded visual
                // concept can remain visible on any full-screen surface.
                if (exactBackdropImage != null)
                {
                    image.sprite = exactBackdropImage.sprite;
                    image.type = Image.Type.Simple;
                    image.color = Color.white;
                }
                else
                    image.color = Depth;
            }
            else if (card)
            {
                image.sprite = RuntimeUI.RoundedRectSprite;
                image.type = Image.Type.Sliced;
                image.color = Surface;
                EnsureOutline(image.gameObject, Violet, 2f);
            }
        }
    }

    static void StyleInputs(Transform root)
    {
        foreach (var input in root.GetComponentsInChildren<TMP_InputField>(true))
        {
            var image = input.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = RuntimeUI.RoundedRectSprite;
                image.type = Image.Type.Sliced;
                image.color = SurfaceRaised;
                EnsureOutline(image.gameObject, Violet, 3f);
            }
            if (input.textComponent != null)
            {
                input.textComponent.color = NearWhite;
                input.textComponent.fontStyle = FontStyles.Bold;
                ConfigureResponsiveText(input.textComponent, 20f);
            }
            var placeholder = input.placeholder as TMP_Text;
            if (placeholder != null)
            {
                placeholder.color = Muted;
                ConfigureResponsiveText(placeholder, 18f);
            }
        }
    }

    static void StyleButtons(Transform root)
    {
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            string name = button.transform.name;
            if (name == "ButtonPlay" || name == "ButtonPvP" || name == "DailyHuntButton")
                continue;

            Color fill = SurfaceRaised;
            Color label = NearWhite;
            bool primary = false;

            if (Contains(name, "Higher")) fill = Cyan;
            else if (Contains(name, "Correct")) fill = Success;
            else if (Contains(name, "Lower")) fill = Pink;
            else if (Contains(name, "Create")) { fill = Gold; label = Ink; primary = true; }
            else if (Contains(name, "Join")) fill = Blue;
            else if (Contains(name, "Submit") || Contains(name, "Confirm") || Contains(name, "Start"))
            { fill = Gold; label = Ink; primary = true; }
            else if (Contains(name, "Share") || Contains(name, "Copy")) fill = Blue;
            else if (Contains(name, "Rematch")) { fill = Gold; label = Ink; primary = true; }
            else if (Contains(name, "Revive") || Contains(name, "Reward")) fill = Pink;
            else if (Contains(name, "Leave") || Contains(name, "Quit") || Contains(name, "Cancel")) fill = Pink;
            else if (name.StartsWith("Key_")) fill = Hex(0x39, 0x2C, 0xAE);
            else if (Contains(name, "Signal")) fill = Hex(0x2B, 0x1C, 0x69);

            StyleButton(button, fill, label, primary);
        }
    }

    static void StyleButton(Button button, Color fill, Color labelColor, bool primary)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = RuntimeUI.RoundedRectSprite;
            image.type = Image.Type.Sliced;
            image.color = fill;
            EnsureOutline(image.gameObject, primary ? Hex(0xFF, 0xE4, 0x68) : Violet, primary ? 4f : 2f);
            EnsureShadow(image.gameObject, primary ? Hex(0x9B, 0x4B, 0x00) : Hex(0x09, 0x02, 0x24), primary ? 10f : 7f);
        }

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.disabledColor = new Color(0.42f, 0.40f, 0.52f, 0.78f);
        button.colors = colors;

        foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
        {
            text.color = labelColor;
            text.fontStyle = FontStyles.Bold;
            ConfigureResponsiveText(text, 16f);
        }
    }

    static void StyleText(Transform root)
    {
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.GetComponentInParent<Button>() != null ||
                text.GetComponentInParent<TMP_InputField>() != null)
                continue;
            if (text.transform.name.StartsWith("Exact")) continue;
            text.color = NearWhite;
            ConfigureResponsiveText(text, 16f);
        }
    }

    static void SetButtonCopy(Button button, string title, string subtitle)
    {
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = title;
            label.fontSize = title.Length > 20 ? 34f : 43f;
            label.fontStyle = FontStyles.Bold;
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(760f, 64f);
            rect.anchoredPosition = new Vector2(0f, 23f);
            ConfigureResponsiveText(label, 22f);
        }

        var sub = EnsureText(button.transform, "ExactButtonSubtitle");
        sub.text = subtitle;
        sub.fontSize = IsGreek && subtitle.Length > 32 ? 21f : 24f;
        sub.fontStyle = FontStyles.Normal;
        sub.color = button.transform.name == "ButtonPlay" ? Ink : NearWhite;
        sub.alignment = TextAlignmentOptions.Center;
        Place(sub.rectTransform, new Vector2(0f, -34f), new Vector2(760f, 42f));
        ConfigureResponsiveText(sub, 16f);
    }

    static void ConfigureResponsiveText(TMP_Text text, float minimumSize)
    {
        if (text == null) return;

        // Greek copy is often wider than its English equivalent. Preserve the
        // approved geometry and hierarchy, then let TMP reduce only the font
        // size needed to keep either language inside the same control.
        if (!text.enableAutoSizing)
        {
            float configuredSize = Mathf.Max(text.fontSize, minimumSize);
            text.enableAutoSizing = true;
            text.fontSizeMax = configuredSize;
            text.fontSizeMin = Mathf.Min(configuredSize,
                Mathf.Max(minimumSize, configuredSize * 0.55f));
        }
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    static TMP_Text EnsureText(Transform parent, string name)
    {
        var existing = DirectChild(parent, name);
        if (existing != null)
        {
            var found = existing.GetComponent<TMP_Text>();
            if (found != null) return found;
        }
        return RuntimeUI.CreateText(parent, name, "", 30, Vector2.zero, new Vector2(100f, 50f), NearWhite);
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

    static void EnsureOutline(GameObject go, Color color, float distance)
    {
        var outline = go.GetComponent<Outline>();
        if (outline == null) outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(distance, distance);
        outline.useGraphicAlpha = true;
    }

    static void EnsureShadow(GameObject go, Color color, float distance)
    {
        var shadow = go.GetComponent<Shadow>();
        if (shadow == null) shadow = go.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = new Vector2(0f, -distance);
        shadow.useGraphicAlpha = true;
    }

    static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    Button FindButton(string name)
    {
        var hit = DeepFind(transform, name);
        return hit == null ? null : hit.GetComponent<Button>();
    }

    static Transform DirectChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name) return parent.GetChild(i);
        return null;
    }

    static void DisableDirectChild(Transform parent, string name)
    {
        var child = DirectChild(parent, name);
        if (child != null) child.gameObject.SetActive(false);
    }

    static Transform DeepFind(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var hit = DeepFind(root.GetChild(i), name);
            if (hit != null) return hit;
        }
        return null;
    }

    static bool Contains(string value, string part)
    {
        return value.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static bool IsGreek => L10n.Current == L10n.Language.Greek;

    static Color Hex(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }
}