using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Sole presentation owner for the Private Room landing screen.
//
// PvpRuntimeUI/PvpGameController remain responsible for networking, room state
// and callbacks. This component only seats the real controls inside the approved
// cartoon composition and assigns approved production sprites at alpha 1.
[DefaultExecutionOrder(2600)]
public sealed class PrivateRoomVisuals : MonoBehaviour
{
    public const string VisualRootName = "PrivateRoomVisualRoot";

    const string BackgroundResource = "phase2a/hol_neon_reference_bg_r3";
    const string LogoResource = "reference/hol_logo_exact";
    const string BoyResource = "reference/char_boy_exact";
    const string GirlResource = "reference/char_girl_exact";
    const string DoorResource = "reference/board_join_exact";
    const string MascotSixResource = "reference/mascot_6_exact";
    const string MascotSevenResource = "reference/mascot_7_exact";
    const string AvatarResource = "reference/player_cyan_exact";
    const string BackChevronResource = "phase2a/hol_chevron_r2";

    const string BlueFrameResource = "mainmenu/mainmenu_cta_blue_9s";
    const string GoldFrameResource = "mainmenu/mainmenu_cta_gold_9s";
    const string MagentaFrameResource = "phase2a/hol_cta_magenta_r2_9s";
    const string PurpleFrameResource = "mainmenu/mainmenu_tip_frame_9s";
    const string PlayerChipResource = "mainmenu/mainmenu_player_chip_frame_9s";
    const string TipIconResource = "mainmenu/mainmenu_icon_tip_bulb";
    const string StreakIconResource = "mainmenu/mainmenu_icon_streak";

    const string DisplayFontResource = "phase2a/fonts/HOL Menu Display SDF";
    const string BodyFontResource = "phase2a/fonts/HOL Menu Body SDF";

    const float ReferenceWidth = 1080f;
    const float ReferenceHeight = 1920f;

    static readonly Color NearWhite = new Color(0.98f, 0.97f, 1f, 1f);
    static readonly Color CyanText = new Color(0.19f, 0.90f, 1f, 1f);
    static readonly Color DarkInk = new Color(0.08f, 0.04f, 0.17f, 1f);

    PvpGameController pvp;
    RectTransform visualRoot;
    RectTransform safeRoot;
    TMP_FontAsset displayFont;
    TMP_FontAsset bodyFont;
    TMP_InputField landingCodeInput;
    TMP_Text playerNameText;
    TMP_Text streakText;
    Button createButton;
    Button joinButton;
    Button backButton;
    Button shareButton;
    bool built;
    float nextRefresh;

    public bool IsReady { get; private set; }

    IEnumerator Start()
    {
        for (int frame = 0; frame < 240 && !built; frame++)
        {
            pvp = GetComponent<PvpGameController>();
            if (pvp != null &&
                pvp.pvpMenuPanel != null &&
                pvp.createPanel != null &&
                pvp.joinPanel != null &&
                pvp.joinCodeInput != null)
            {
                Build();
                break;
            }
            yield return null;
        }

        if (!built)
            Debug.LogError("[PrivateRoomVisuals] PvP controls were not ready within 240 frames.");
    }

    void OnEnable()
    {
        L10n.OnLanguageChanged += RefreshCopy;
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= RefreshCopy;
    }

    void LateUpdate()
    {
        if (!built || pvp == null || pvp.pvpMenuPanel == null) return;
        if (!pvp.pvpMenuPanel.activeInHierarchy) return;
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.25f;
        RefreshPlayerChip();
    }

    void Build()
    {
        if (built || pvp == null || pvp.pvpMenuPanel == null) return;
        built = true;

        displayFont = Resources.Load<TMP_FontAsset>(DisplayFontResource);
        bodyFont = Resources.Load<TMP_FontAsset>(BodyFontResource);

        Sprite background = LoadRequired(BackgroundResource);
        Sprite logo = LoadRequired(LogoResource);
        Sprite boy = LoadRequired(BoyResource);
        Sprite girl = LoadRequired(GirlResource);
        Sprite door = LoadRequired(DoorResource);
        Sprite six = LoadRequired(MascotSixResource);
        Sprite seven = LoadRequired(MascotSevenResource);
        Sprite avatar = LoadRequired(AvatarResource);
        Sprite blue = LoadRequired(BlueFrameResource);
        Sprite gold = LoadRequired(GoldFrameResource);
        Sprite magenta = LoadRequired(MagentaFrameResource);
        Sprite purple = LoadRequired(PurpleFrameResource);
        Sprite chip = LoadRequired(PlayerChipResource);
        Sprite tip = LoadRequired(TipIconResource);
        Sprite chevron = LoadRequired(BackChevronResource);
        Sprite streakIcon = LoadRequired(StreakIconResource);

        IsReady = background != null && logo != null && boy != null &&
            girl != null && door != null && six != null && seven != null &&
            avatar != null && blue != null && gold != null && magenta != null &&
            purple != null && chip != null && tip != null && chevron != null &&
            streakIcon != null &&
            displayFont != null && bodyFont != null;
        if (!IsReady)
        {
            Debug.LogError("[PrivateRoomVisuals] Required production artwork/fonts are missing.");
            return;
        }

        Transform panel = pvp.pvpMenuPanel.transform;
        var panelImage = pvp.pvpMenuPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.enabled = false;
            panelImage.raycastTarget = false;
        }

        createButton = FindButton(panel, "CreateButton");
        joinButton = FindButton(panel, "JoinButton");
        backButton = FindBackButton(panel, createButton, joinButton);
        if (createButton == null || joinButton == null || backButton == null)
        {
            Debug.LogError("[PrivateRoomVisuals] Create/Join/Back controls are missing.");
            IsReady = false;
            return;
        }

        // These Button roots come from PvpRuntimeUI because the controller owns
        // their callbacks. Their historical child art/text is presentation only
        // and must not survive beneath this sole production owner.
        ClearButtonPresentation(createButton.transform);
        ClearButtonPresentation(joinButton.transform);
        ClearButtonPresentation(backButton.transform);

        HideLegacyPresentation(panel, createButton.transform, joinButton.transform,
            backButton.transform);

        visualRoot = EnsureRect(panel, VisualRootName);
        Stretch(visualRoot);
        visualRoot.SetAsFirstSibling();

        var bg = EnsureImage(visualRoot, "PrivateRoomBackground");
        Stretch(bg.rectTransform);
        ConfigureImage(bg, background, false, Image.Type.Simple);

        // One strong rounded board around the whole UI, matching the approved
        // reference's cabinet-like contained composition.
        var outer = EnsureImage(visualRoot, "PrivateRoomOuterFrame");
        ConfigureImage(outer, purple, false, Image.Type.Sliced);
        Place(outer.rectTransform, Vector2.zero, new Vector2(1030f, 1860f));
        outer.color = Color.white;

        safeRoot = EnsureRect(visualRoot, "PrivateRoomSafeRoot");
        Stretch(safeRoot);
        var canvas = pvp.pvpMenuPanel.GetComponentInParent<Canvas>();
        if (canvas != null)
            ResponsiveSafeAreaRoot.Attach(safeRoot, canvas.transform as RectTransform,
                new Vector2(ReferenceWidth, ReferenceHeight));

        BuildTopBar(chip, avatar, purple, chevron, streakIcon);

        var logoImage = EnsureImage(safeRoot, "PrivateRoomLogo");
        ConfigureImage(logoImage, logo, true, Image.Type.Simple);
        Place(logoImage.rectTransform, new Vector2(0f, 635f),
            new Vector2(600f, 320f));

        var titleRibbon = EnsureImage(safeRoot, "PrivateRoomTitleRibbon");
        ConfigureImage(titleRibbon, purple, false, Image.Type.Sliced);
        Place(titleRibbon.rectTransform, new Vector2(0f, 420f),
            new Vector2(870f, 145f));
        var title = EnsureText(titleRibbon.transform, "PrivateRoomTitle", 56f,
            displayFont, NearWhite, TextAlignmentOptions.Center);
        StretchText(title.rectTransform, 54f, 20f);
        RuntimeUI.Localize(title, "private_room_title");

        BuildCreateCard(blue, boy, girl);
        BuildJoinCard(magenta, gold, purple, door);
        BuildShareAndTip(purple, tip, six, seven);

        RefreshCopy();
        RefreshPlayerChip();
    }

    void BuildTopBar(Sprite chipSprite, Sprite avatar, Sprite pillSprite,
        Sprite chevron, Sprite streakIcon)
    {
        var step = EnsureImage(safeRoot, "PrivateRoomStepPill");
        ConfigureImage(step, pillSprite, false, Image.Type.Sliced);
        Place(step.rectTransform, new Vector2(-270f, 820f),
            new Vector2(330f, 90f));
        var stepText = EnsureText(step.transform, "PrivateRoomStepText", 28f,
            displayFont, NearWhite, TextAlignmentOptions.Center);
        StretchText(stepText.rectTransform, 18f, 12f);
        RuntimeUI.Localize(stepText, "private_room_step");

        var chip = EnsureImage(safeRoot, "PrivateRoomPlayerChip");
        ConfigureImage(chip, chipSprite, false, Image.Type.Sliced);
        Place(chip.rectTransform, new Vector2(350f, 820f),
            new Vector2(350f, 120f));

        var avatarImage = EnsureImage(chip.transform, "PrivateRoomPlayerAvatar");
        ConfigureImage(avatarImage, avatar, true, Image.Type.Simple);
        Place(avatarImage.rectTransform, new Vector2(-126f, 0f),
            new Vector2(82f, 82f));

        playerNameText = EnsureText(chip.transform, "PrivateRoomPlayerName", 31f,
            bodyFont, NearWhite, TextAlignmentOptions.Center);
        Place(playerNameText.rectTransform, new Vector2(38f, 22f),
            new Vector2(210f, 42f));
        playerNameText.fontStyle = FontStyles.Bold;

        streakText = EnsureText(chip.transform, "PrivateRoomStreak", 30f,
            bodyFont, new Color(1f, 0.82f, 0.24f, 1f),
            TextAlignmentOptions.Center);
        Place(streakText.rectTransform, new Vector2(55f, -27f),
            new Vector2(120f, 40f));
        var streakImage = EnsureImage(chip.transform, "PrivateRoomStreakIcon");
        ConfigureImage(streakImage, streakIcon, true, Image.Type.Simple);
        Place(streakImage.rectTransform, new Vector2(-20f, -27f),
            new Vector2(44f, 44f));

        Reparent(backButton.transform, safeRoot);
        Place((RectTransform)backButton.transform, new Vector2(-485f, 820f),
            new Vector2(86f, 86f));
        StyleButton(backButton, pillSprite, NearWhite, 1f, Image.Type.Sliced);
        HideButtonLabels(backButton.transform);
        var backIcon = EnsureImage(backButton.transform, "PrivateRoomBackIcon");
        ConfigureImage(backIcon, chevron, true, Image.Type.Simple);
        Place(backIcon.rectTransform, Vector2.zero, new Vector2(44f, 56f));
        backIcon.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
    }

    void BuildCreateCard(Sprite frame, Sprite boy, Sprite girl)
    {
        var card = EnsureImage(safeRoot, "PrivateRoomCreateCard");
        ConfigureImage(card, frame, false, Image.Type.Sliced);
        Place(card.rectTransform, new Vector2(0f, 105f),
            new Vector2(920f, 430f));

        var boyImage = EnsureImage(card.transform, "PrivateRoomCreateBoy");
        ConfigureImage(boyImage, boy, true, Image.Type.Simple);
        Place(boyImage.rectTransform, new Vector2(-285f, -5f),
            new Vector2(350f, 350f));

        var girlImage = EnsureImage(card.transform, "PrivateRoomCreateGirl");
        ConfigureImage(girlImage, girl, true, Image.Type.Simple);
        Place(girlImage.rectTransform, new Vector2(-120f, -10f),
            new Vector2(330f, 340f));

        var heading = EnsureText(card.transform, "PrivateRoomCreateHeading", 46f,
            displayFont, NearWhite, TextAlignmentOptions.Center);
        Place(heading.rectTransform, new Vector2(250f, 95f),
            new Vector2(390f, 110f));
        RuntimeUI.Localize(heading, "private_room_create_title");

        var hint = EnsureText(card.transform, "PrivateRoomCreateHint", 29f,
            bodyFont, NearWhite, TextAlignmentOptions.Center);
        Place(hint.rectTransform, new Vector2(250f, 8f),
            new Vector2(390f, 90f));
        RuntimeUI.Localize(hint, "private_room_create_hint");

        Reparent(createButton.transform, card.transform);
        Place((RectTransform)createButton.transform, new Vector2(250f, -112f),
            new Vector2(360f, 104f));
        StyleButton(createButton, frame, DarkInk, 1f, Image.Type.Sliced);
        ConfigureButtonLabel(createButton, "private_room_create_action", 38f, DarkInk);
    }

    void BuildJoinCard(Sprite magentaFrame, Sprite goldFrame,
        Sprite inputFrame, Sprite door)
    {
        var card = EnsureImage(safeRoot, "PrivateRoomJoinCard");
        ConfigureImage(card, magentaFrame, false, Image.Type.Sliced);
        Place(card.rectTransform, new Vector2(0f, -365f),
            new Vector2(920f, 400f));

        var doorImage = EnsureImage(card.transform, "PrivateRoomJoinDoor");
        ConfigureImage(doorImage, door, true, Image.Type.Simple);
        Place(doorImage.rectTransform, new Vector2(-305f, 0f),
            new Vector2(245f, 255f));

        var heading = EnsureText(card.transform, "PrivateRoomJoinHeading", 43f,
            displayFont, NearWhite, TextAlignmentOptions.Center);
        Place(heading.rectTransform, new Vector2(205f, 120f),
            new Vector2(500f, 90f));
        RuntimeUI.Localize(heading, "private_room_join_title");

        landingCodeInput = RuntimeUI.CreateInputField(card.transform,
            "PrivateRoomLandingCodeInput", L10n.Get("pvp_enter_code"),
            new Vector2(205f, 25f), new Vector2(430f, 88f), 5,
            TMP_InputField.ContentType.Standard);
        landingCodeInput.onValidateInput = (text, index, ch) =>
            char.ToUpperInvariant(ch);
        var inputImage = landingCodeInput.GetComponent<Image>();
        if (inputImage != null)
        {
            inputImage.sprite = inputFrame;
            inputImage.type = Image.Type.Sliced;
            inputImage.color = Color.white;
            inputImage.pixelsPerUnitMultiplier = 2f;
        }
        if (landingCodeInput.textComponent != null)
        {
            landingCodeInput.textComponent.font = displayFont;
            landingCodeInput.textComponent.fontSize = 38f;
            landingCodeInput.textComponent.fontStyle = FontStyles.Bold;
            landingCodeInput.textComponent.color = NearWhite;
            landingCodeInput.textComponent.alignment = TextAlignmentOptions.Center;
        }
        var placeholder = landingCodeInput.placeholder as TMP_Text;
        if (placeholder != null)
        {
            placeholder.font = bodyFont;
            placeholder.fontSize = 24f;
            placeholder.color = new Color(0.88f, 0.82f, 0.95f, 0.80f);
            placeholder.alignment = TextAlignmentOptions.Center;
        }
        RuntimeUI.LocalizePlaceholder(landingCodeInput, "pvp_enter_code");

        Reparent(joinButton.transform, card.transform);
        Place((RectTransform)joinButton.transform, new Vector2(205f, -100f),
            new Vector2(430f, 104f));
        StyleButton(joinButton, goldFrame, DarkInk, 1f, Image.Type.Sliced);
        ConfigureButtonLabel(joinButton, "private_room_join_action", 42f, DarkInk);
        joinButton.onClick.AddListener(CopyLandingCodeIntoJoinFlow);
    }

    void BuildShareAndTip(Sprite purpleFrame, Sprite tipIcon, Sprite six,
        Sprite seven)
    {
        var shareGo = RuntimeUI.CreateObject("PrivateRoomShareButton", safeRoot);
        Place((RectTransform)shareGo.transform, new Vector2(0f, -625f),
            new Vector2(420f, 96f));
        var shareImage = shareGo.AddComponent<Image>();
        ConfigureImage(shareImage, purpleFrame, false, Image.Type.Sliced);
        shareImage.raycastTarget = true;
        shareButton = shareGo.AddComponent<Button>();
        shareButton.targetGraphic = shareImage;
        RuntimeUI.AttachJuice(shareButton);
        var shareLabel = EnsureText(shareGo.transform, "PrivateRoomShareLabel", 36f,
            displayFont, NearWhite, TextAlignmentOptions.Center);
        StretchText(shareLabel.rectTransform, 28f, 14f);
        RuntimeUI.Localize(shareLabel, "private_room_share");
        shareButton.onClick.AddListener(ShareAvailableCode);

        var tipCard = EnsureImage(safeRoot, "PrivateRoomTipCard");
        ConfigureImage(tipCard, purpleFrame, false, Image.Type.Sliced);
        Place(tipCard.rectTransform, new Vector2(0f, -790f),
            new Vector2(760f, 170f));

        var bulb = EnsureImage(tipCard.transform, "PrivateRoomTipIcon");
        ConfigureImage(bulb, tipIcon, true, Image.Type.Simple);
        Place(bulb.rectTransform, new Vector2(-310f, 0f),
            new Vector2(82f, 82f));

        var tipText = EnsureText(tipCard.transform, "PrivateRoomTipText", 28f,
            bodyFont, NearWhite, TextAlignmentOptions.Left);
        Place(tipText.rectTransform, new Vector2(65f, 0f),
            new Vector2(560f, 118f));
        RuntimeUI.Localize(tipText, "private_room_tip");

        var sixImage = EnsureImage(safeRoot, "PrivateRoomMascotSix");
        ConfigureImage(sixImage, six, true, Image.Type.Simple);
        Place(sixImage.rectTransform, new Vector2(-430f, -805f),
            new Vector2(250f, 285f));

        var sevenImage = EnsureImage(safeRoot, "PrivateRoomMascotSeven");
        ConfigureImage(sevenImage, seven, true, Image.Type.Simple);
        Place(sevenImage.rectTransform, new Vector2(430f, -805f),
            new Vector2(250f, 285f));
    }

    void CopyLandingCodeIntoJoinFlow()
    {
        if (landingCodeInput == null || pvp == null || pvp.joinCodeInput == null)
            return;
        pvp.joinCodeInput.SetTextWithoutNotify(
            (landingCodeInput.text ?? string.Empty).Trim().ToUpperInvariant());
    }

    void ShareAvailableCode()
    {
        string code = string.Empty;
        if (pvp != null && pvp.roomCodeText != null)
        {
            string room = (pvp.roomCodeText.text ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(room) && room != "-----")
                code = room;
        }
        if (string.IsNullOrEmpty(code) && landingCodeInput != null)
            code = (landingCodeInput.text ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(code)) return;

        GUIUtility.systemCopyBuffer = code;
        if (pvp != null && pvp.roomCodeText != null &&
            pvp.roomCodeText.text == code)
            pvp.OnCopyInvitePressed();
    }

    void RefreshCopy()
    {
        if (!built) return;
        var stepText = DeepFind(safeRoot, "PrivateRoomStepText")
            ?.GetComponent<TMP_Text>();
        if (stepText != null)
            stepText.text = L10n.Get("private_room_step");
        RefreshPlayerChip();
    }

    void RefreshPlayerChip()
    {
        if (playerNameText == null || streakText == null) return;
        string player = PlayerPrefs.GetString("PlayerName", "");
        if (string.IsNullOrWhiteSpace(player)) player = L10n.Get("player_default");
        playerNameText.text = player;
        streakText.text = GameStats.CurrentStreak.ToString();
    }

    void ConfigureButtonLabel(Button button, string key, float size, Color color)
    {
        if (button == null) return;
        var label = DirectChild(button.transform, "PrivateRoomActionLabel")?.GetComponent<TMP_Text>();
        if (label == null)
        {
            label = EnsureText(button.transform, "PrivateRoomActionLabel", size, displayFont,
                color, TextAlignmentOptions.Center);
        }
        label.gameObject.SetActive(true);
        label.font = displayFont;
        label.fontSize = size;
        label.fontStyle = FontStyles.Bold;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = Mathf.Max(26f, size - 10f);
        label.fontSizeMax = size;
        label.enableWordWrapping = true;
        label.raycastTarget = false;
        StretchText(label.rectTransform, 24f, 14f);
        var localized = label.GetComponent<LocalizedText>();
        if (localized == null) RuntimeUI.Localize(label, key);
        else localized.key = key;
        label.text = L10n.Get(key);
    }

    static void StyleButton(Button button, Sprite sprite, Color labelColor,
        float pixelsPerUnit, Image.Type type)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image == null) image = button.gameObject.AddComponent<Image>();
        image.enabled = true;
        image.sprite = sprite;
        image.type = type;
        image.color = Color.white;
        image.preserveAspect = false;
        image.pixelsPerUnitMultiplier = pixelsPerUnit;
        image.raycastTarget = true;
        button.targetGraphic = image;

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.90f, 0.92f, 1f, 1f);
        colors.disabledColor = new Color(0.60f, 0.60f, 0.68f, 0.72f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.06f;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = colors;

        foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
            text.color = labelColor;
    }

    static void HideLegacyPresentation(Transform panel, params Transform[] keep)
    {
        for (int i = panel.childCount - 1; i >= 0; i--)
        {
            var child = panel.GetChild(i);
            bool preserve = false;
            for (int k = 0; k < keep.Length; k++)
                if (child == keep[k]) { preserve = true; break; }
            if (preserve || child.name == VisualRootName) continue;
            child.gameObject.SetActive(false);
        }
    }

    static Button FindButton(Transform root, string name)
    {
        var t = DeepFind(root, name);
        return t == null ? null : t.GetComponent<Button>();
    }

    static Button FindBackButton(Transform root, Button create, Button join)
    {
        foreach (var button in root.GetComponentsInChildren<Button>(true))
            if (button != create && button != join) return button;
        return null;
    }

    static void ClearButtonPresentation(Transform root)
    {
        if (root == null) return;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            var child = root.GetChild(i);
            child.gameObject.SetActive(false);
            child.SetParent(null, false);
            RuntimeUI.DestroyNow(child.gameObject);
        }
    }

    static void HideButtonLabels(Transform root)
    {
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            text.gameObject.SetActive(false);
        foreach (var text in root.GetComponentsInChildren<Text>(true))
            text.gameObject.SetActive(false);
    }

    static Sprite LoadRequired(string resource)
    {
        var sprite = Resources.Load<Sprite>(resource);
        if (sprite == null)
            Debug.LogError("[PrivateRoomVisuals] Missing Resources/" + resource + ".");
        return sprite;
    }

    static RectTransform EnsureRect(Transform parent, string name)
    {
        var existing = DirectChild(parent, name) as RectTransform;
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            return existing;
        }
        return (RectTransform)RuntimeUI.CreateObject(name, parent).transform;
    }

    static Image EnsureImage(Transform parent, string name)
    {
        var rect = EnsureRect(parent, name);
        var image = rect.GetComponent<Image>();
        if (image == null) image = rect.gameObject.AddComponent<Image>();
        return image;
    }

    static TMP_Text EnsureText(Transform parent, string name, float size,
        TMP_FontAsset font, Color color, TextAlignmentOptions alignment)
    {
        var rect = EnsureRect(parent, name);
        var text = rect.GetComponent<TextMeshProUGUI>();
        if (text == null) text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.enableAutoSizing = false;
        return text;
    }

    static void ConfigureImage(Image image, Sprite sprite, bool preserveAspect,
        Image.Type type)
    {
        image.enabled = true;
        image.sprite = sprite;
        image.type = type;
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    static void Reparent(Transform child, Transform parent)
    {
        if (child.parent != parent) child.SetParent(parent, false);
        child.gameObject.SetActive(true);
        child.SetAsLastSibling();
    }

    static void Place(RectTransform rect, Vector2 position, Vector2 size)
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
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    static void StretchText(RectTransform rect, float horizontalInset,
        float verticalInset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontalInset, verticalInset);
        rect.offsetMax = new Vector2(-horizontalInset, -verticalInset);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
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
            var found = DeepFind(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    static bool IsGreek => L10n.Current == L10n.Language.Greek;
}