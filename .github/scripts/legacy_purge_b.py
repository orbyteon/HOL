from pathlib import Path
import re


def require(cond, msg):
    if not cond:
        raise SystemExit(msg)


# RuntimeUI: exact-resource production-frame infrastructure.
runtime_path = Path("Assets/SCRIPT/RuntimeUI/RuntimeUI.cs")
runtime = runtime_path.read_text(encoding="utf-8")
marker = "    // Every runtime-built label is TextMesh Pro, same as the scene's TMP\n"
require(marker in runtime, "RuntimeUI insertion marker changed")
if "CreateProductionFrame(" not in runtime:
    helper = '''    // Neutral infrastructure for a caller-owned production frame. The
    // caller supplies the exact approved sprite path; RuntimeUI never chooses
    // a visual language or substitutes generated artwork.
    public static GameObject CreateProductionFrame(
        Transform parent, string name, Vector2 position, Vector2 size,
        string resourcePath, float pixelsPerUnitMultiplier = 2f)
    {
        var frame = CreateObject(name, parent);
        Center(frame, position, size);
        ClampToSafeArea((RectTransform)frame.transform, size, position);
        var image = frame.AddComponent<Image>();
        ApplyProductionSprite(image, resourcePath, Image.Type.Sliced, false,
            pixelsPerUnitMultiplier);
        image.raycastTarget = false;
        return frame;
    }

'''
    runtime = runtime.replace(marker, helper + marker, 1)
runtime_path.write_text(runtime, encoding="utf-8")


# PvP: one build path, current production art, no shared theme facade.
pvp_path = Path("Assets/SCRIPT/RuntimeUI/PvpRuntimeUI.cs")
pvp = pvp_path.read_text(encoding="utf-8")
pvp = pvp.replace("BuildPanelsLegacy(controller);", "BuildMatchPanel(controller);")
pvp = pvp.replace("void BuildPanelsLegacy(PvpGameController controller)",
                  "void BuildMatchPanel(PvpGameController controller)")

match_marker = '        // Match — laid out to the "HOL Consumer First" board'
method_head = "    void BuildMatchPanel(PvpGameController controller)\n    {\n"
require(method_head in pvp and match_marker in pvp,
        "PvpRuntimeUI match split markers changed")
start = pvp.index(method_head) + len(method_head)
match = pvp.index(match_marker, start)
pvp = pvp[:start] + '''        // Build only the live match surface here. Private Room landing and
        // prebattle controls are built once by ReplacePrivateRoomPanels and
        // presented by their dedicated current owner.
''' + pvp[match:]

wiring_pattern = re.compile(
    r"        // Wire the controller\.\n.*?        controller\.historyText = historyText;\n",
    re.S,
)
replacement = '''        // Wire match state only. Private Room fields are assigned exactly once
        // by ReplacePrivateRoomPanels below.
        controller.matchPanel = matchPanel;
        controller.guessInput = guessInput;
        controller.opponentNameText = opponentText;
        controller.turnText = turnText;
        controller.roundText = roundText;
        controller.historyText = historyText;
'''
pvp, n = wiring_pattern.subn(replacement, pvp, count=1)
require(n == 1, "PvpRuntimeUI legacy controller wiring block changed")

old_hooks = '''        // Button hooks.
        createBtn.onClick.AddListener(() => ShowOnly(controller, createPanel));
        joinBtn.onClick.AddListener(() => ShowOnly(controller, joinPanel));
        closeBtn.onClick.AddListener(controller.ClosePvpMenu);
        createGo.onClick.AddListener(controller.OnCreateRoomPressed);
        copyBtn.onClick.AddListener(controller.OnCopyInvitePressed);
        createBack.onClick.AddListener(controller.CancelRoomAndLeave);
        joinGo.onClick.AddListener(controller.OnJoinRoomPressed);
        joinBack.onClick.AddListener(controller.CancelRoomAndLeave);
        guessBtn.onClick.AddListener(controller.OnSubmitGuessPressed);
        lockBtn.onClick.AddListener(controller.OnLockTogglePressed);
        leaveBtn.onClick.AddListener(controller.OnLeaveMatchPressed);
'''
require(old_hooks in pvp, "PvpRuntimeUI button-hook block changed")
pvp = pvp.replace(old_hooks, '''        // Match button hooks.
        guessBtn.onClick.AddListener(controller.OnSubmitGuessPressed);
        lockBtn.onClick.AddListener(controller.OnLockTogglePressed);
        leaveBtn.onClick.AddListener(controller.OnLeaveMatchPressed);
''', 1)

soft_pattern = re.compile(
    r"        // Soft-keyboard Done .*?        guessInput\.onSubmit\.AddListener\(_ => controller\.OnSubmitGuessPressed\(\)\);\n",
    re.S,
)
pvp, n = soft_pattern.subn(
    '''        // Soft-keyboard Done submits the live guess flow.
        guessInput.onSubmit.AddListener(_ => controller.OnSubmitGuessPressed());
''',
    pvp,
    count=1,
)
require(n == 1, "PvpRuntimeUI submit-hook block changed")

old_hidden = '''        // All panels start hidden; OpenPvpMenu shows the menu panel.
        lockBtn.gameObject.SetActive(false);
        signalsRoot.SetActive(false);
        menuPanel.SetActive(false);
        createPanel.SetActive(false);
        joinPanel.SetActive(false);
        matchPanel.SetActive(false);
'''
require(old_hidden in pvp, "PvpRuntimeUI initial visibility block changed")
pvp = pvp.replace(old_hidden, '''        // Match starts hidden; OpenPvpMenu is owned by the current Private Room flow.
        lockBtn.gameObject.SetActive(false);
        signalsRoot.SetActive(false);
        matchPanel.SetActive(false);
''', 1)

landing_pattern = re.compile(
    r'        var menu = BuildPortraitPanel\(transform, "PvPMenuPanel"\);.*?'
    r'        var prebattleCreate = BuildPrebattlePanel\("PvPCreatePanel", true\);',
    re.S,
)
landing = '''        var menu = BuildPortraitPanel(transform, "PvPMenuPanel");
        var create = RuntimeUI.CreateButton(menu.transform, "CreateButton",
            L10n.Get("pvp_create_room"), Vector2.zero, new Vector2(360f, 104f),
            ConsumerTokens.Cyan, DarkLabel);
        var join = RuntimeUI.CreateButton(menu.transform, "JoinButton",
            L10n.Get("pvp_join_room"), Vector2.zero, new Vector2(430f, 104f),
            ConsumerTokens.Gold, DarkLabel);
        var back = RuntimeUI.CreateButton(menu.transform, "BackButton",
            L10n.Get("back"), Vector2.zero, new Vector2(86f, 86f),
            ConsumerTokens.SurfaceElevated);
        RuntimeUI.Localize(create, "pvp_create_room");
        RuntimeUI.Localize(join, "pvp_join_room");
        RuntimeUI.Localize(back, "back");

        var prebattleCreate = BuildPrebattlePanel("PvPCreatePanel", true);'''
pvp, n = landing_pattern.subn(landing, pvp, count=1)
require(n == 1, "PvpRuntimeUI Private Room landing block changed")

# Rewrite call sites before inserting helpers.
pvp = pvp.replace("RuntimeUI.FullscreenPanel(", "CreatePvpPanel(")
pvp = pvp.replace("RuntimeUI.CreateButton(", "CreatePvpButton(")
pvp = pvp.replace("RuntimeUI.CreateInputField(", "CreatePvpInput(")
pvp = pvp.replace("NeonFrame.Frame(", "PvpFrame(")
pvp = pvp.replace("ForceProceduralButton(", "StylePvpButton(")
pvp = re.sub(r"\n\s*NeonBackdrop\([^;]+\);", "", pvp)

helper_pattern = re.compile(
    r"    // CreateButton swaps in the wired design sprites.*?"
    r"    void BuildPanels\(PvpGameController controller\)",
    re.S,
)
pvp_helpers = '''    const string PvpBackgroundResource = "phase2a/hol_neon_reference_bg_r3";
    const string PvpPurpleFrameResource = "mainmenu/mainmenu_tip_frame_9s";
    const string PvpBlueFrameResource = "mainmenu/mainmenu_cta_blue_9s";
    const string PvpMagentaFrameResource = "phase2a/hol_cta_magenta_r2_9s";
    const string PvpGoldFrameResource = "mainmenu/mainmenu_cta_gold_9s";

    static GameObject CreatePvpPanel(Transform parent, string name, Color fallback)
    {
        var panel = RuntimeUI.FullscreenPanel(parent, name, fallback);
        var image = panel.GetComponent<Image>();
        RuntimeUI.ApplyProductionSprite(image, PvpBackgroundResource,
            Image.Type.Simple, false);
        image.raycastTarget = true;
        return panel;
    }

    static Button CreatePvpButton(Transform parent, string name, string label,
        Vector2 position, Vector2 size, Color color, Color? labelColor = null)
    {
        var button = RuntimeUI.CreateButton(parent, name, label, position, size,
            color, labelColor);
        StylePvpButton(button, color);
        return button;
    }

    static TMP_InputField CreatePvpInput(Transform parent, string name,
        string placeholder, Vector2 position, Vector2 size, int characterLimit = 3,
        TMP_InputField.ContentType contentType = TMP_InputField.ContentType.IntegerNumber)
    {
        var input = RuntimeUI.CreateInputField(parent, name, placeholder, position,
            size, characterLimit, contentType);
        var image = input.GetComponent<Image>();
        RuntimeUI.ApplyProductionSprite(image, PvpPurpleFrameResource,
            Image.Type.Sliced, false, 2f);
        if (input.textComponent != null)
            input.textComponent.color = ConsumerTokens.TextPrimary;
        var placeholderText = input.placeholder as TMP_Text;
        if (placeholderText != null)
            placeholderText.color = ConsumerTokens.WithAlpha(
                ConsumerTokens.TextSecondary, 0.82f);
        return input;
    }

    static GameObject PvpFrame(Transform parent, string name, Vector2 position,
        Vector2 size, Color accent, float fillAlpha = 0.85f, bool glow = true,
        Color? fillColor = null)
    {
        return RuntimeUI.CreateProductionFrame(parent, name, position, size,
            ResolvePvpFrameResource(accent, fillColor), 2f);
    }

    static void StylePvpButton(Button button, Color accent)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image == null) return;
        RuntimeUI.ApplyProductionSprite(image,
            ResolvePvpFrameResource(accent, null), Image.Type.Sliced, false, 2f);
        image.raycastTarget = true;
        button.targetGraphic = image;
    }

    static string ResolvePvpFrameResource(Color accent, Color? fillColor)
    {
        if (ColorDistance(accent, ConsumerTokens.Gold) < 0.35f)
            return PvpGoldFrameResource;
        if (ColorDistance(accent, ConsumerTokens.Magenta) < 0.42f ||
            (fillColor.HasValue &&
             ColorDistance(fillColor.Value, ConsumerTokens.CardPink) < 0.45f))
            return PvpMagentaFrameResource;
        if (ColorDistance(accent, ConsumerTokens.Cyan) < 0.48f ||
            ColorDistance(accent, ConsumerTokens.Blue) < 0.48f ||
            ColorDistance(accent, ConsumerTokens.KeyBlue) < 0.52f ||
            (fillColor.HasValue &&
             ColorDistance(fillColor.Value, ConsumerTokens.CardBlue) < 0.50f))
            return PvpBlueFrameResource;
        return PvpPurpleFrameResource;
    }

    static float ColorDistance(Color a, Color b)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return Mathf.Sqrt(dr * dr + dg * dg + db * db);
    }

    void BuildPanels(PvpGameController controller)'''
pvp, n = helper_pattern.subn(pvp_helpers, pvp, count=1)
require(n == 1, "PvpRuntimeUI legacy helper block changed")
pvp = pvp.replace(
    "    // Converging Light palette (design/philosophy.md): indigo depth, cyan and\n"
    "    // gold as the disciplined lights, near-white text. Gold is reserved for\n"
    "    // the single most important action on each screen (primary CTA).\n",
    "    // Dynamic state colors only. Production surfaces are approved sprites.\n",
)
pvp = pvp.replace(
    "Gold stays reserved for the primary action (design/philosophy.md)",
    "Gold marks the primary action",
)
pvp_path.write_text(pvp, encoding="utf-8")


# Consent current production frame/buttons.
consent_path = Path("Assets/SCRIPT/ConsentManager.cs")
consent = consent_path.read_text(encoding="utf-8")
consent, n = re.subn(
    r'        var card = NeonFrame\.Frame\(panel\.transform, "Card", Vector2\.zero,\n'
    r'            new Vector2\(600f, 420f\), ConsumerTokens\.Cyan, 0\.97f, true,\n'
    r'            ConsumerTokens\.Surface\);',
    '        var card = RuntimeUI.CreateProductionFrame(panel.transform, "Card", Vector2.zero,\n'
    '            new Vector2(600f, 420f), "mainmenu/mainmenu_tip_frame_9s", 2f);',
    consent,
    count=1,
)
require(n == 1, "Consent frame call changed")
old_img = '''        var image = go.AddComponent<Image>();
        image.sprite = RuntimeUI.RoundedRectSprite;
        image.type = Image.Type.Sliced;
        image.color = color;

        var button = go.AddComponent<Button>();
'''
require(old_img in consent, "Consent button surface block changed")
consent = consent.replace(old_img, '''        var image = go.AddComponent<Image>();
        string resource = name == "YesButton"
            ? "mainmenu/mainmenu_cta_blue_9s"
            : "mainmenu/mainmenu_tip_frame_9s";
        RuntimeUI.ApplyProductionSprite(image, resource, Image.Type.Sliced, false, 2f);

        var button = go.AddComponent<Button>();
''', 1)
consent_path.write_text(consent, encoding="utf-8")


# Force Update current production frame/buttons.
force_path = Path("Assets/SCRIPT/ForceUpdate.cs")
force = force_path.read_text(encoding="utf-8")
force, n = re.subn(
    r'        var card = NeonFrame\.Frame\(panel\.transform, "Card", Vector2\.zero,\n'
    r'            new Vector2\(640f, 560f\), ConsumerTokens\.Gold, 0\.97f, true,\n'
    r'            ConsumerTokens\.Surface\);',
    '        var card = RuntimeUI.CreateProductionFrame(panel.transform, "Card", Vector2.zero,\n'
    '            new Vector2(640f, 560f), "mainmenu/mainmenu_cta_gold_9s", 2f);',
    force,
    count=1,
)
require(n == 1, "ForceUpdate frame call changed")
update_anchor = '''        var update = RuntimeUI.CreateButton(card.transform, "ConfirmUpdateButton",
            L10n.Get("update_now"), new Vector2(0f, -110f), new Vector2(420f, 100f),
            ConsumerTokens.Gold, ConsumerTokens.WithAlpha(ConsumerTokens.Surface, 1f));
'''
require(update_anchor in force, "ForceUpdate primary button block changed")
force = force.replace(update_anchor, update_anchor + '''        RuntimeUI.ApplyProductionSprite(update.GetComponent<Image>(),
            "mainmenu/mainmenu_cta_gold_9s", Image.Type.Sliced, false, 2f);
''', 1)
quit_anchor = '''        var quit = RuntimeUI.CreateButton(card.transform, "QuitButton",
            L10n.Get("quit"), new Vector2(0f, -220f), new Vector2(420f, 100f),
            ConsumerTokens.SurfaceElevated);
'''
require(quit_anchor in force, "ForceUpdate quit button block changed")
force = force.replace(quit_anchor, quit_anchor + '''        RuntimeUI.ApplyProductionSprite(quit.GetComponent<Image>(),
            "mainmenu/mainmenu_tip_frame_9s", Image.Type.Sliced, false, 2f);
''', 1)
force_path.write_text(force, encoding="utf-8")


# Daily Hunt current production frame and entry art.
daily_path = Path("Assets/SCRIPT/DailyHunt.cs")
daily = daily_path.read_text(encoding="utf-8")
daily, n = re.subn(
    r'        NeonFrame\.Frame\(transform, "Card", new Vector2\(0f, -10f\),\n'
    r'            new Vector2\(920f, 1340f\), ConsumerTokens\.Cyan, 0\.985f, true,\n'
    r'            ConsumerTokens\.Surface\);',
    '        RuntimeUI.CreateProductionFrame(transform, "Card", new Vector2(0f, -10f),\n'
    '            new Vector2(920f, 1340f), "mainmenu/mainmenu_tip_frame_9s", 2f);',
    daily,
    count=1,
)
require(n == 1, "DailyHunt frame call changed")
entry_anchor = '''        var entry = RuntimeUI.CreateButton(canvas, "DailyHuntButton",
            L10n.Get("daily_hunt"), new Vector2(0f, -740f), new Vector2(460f, 90f),
            ConsumerTokens.Cyan, DarkLabel);
'''
require(entry_anchor in daily, "DailyHunt entry block changed")
daily = daily.replace(entry_anchor, entry_anchor + '''        RuntimeUI.ApplyProductionSprite(entry.GetComponent<Image>(),
            "mainmenu/mainmenu_cta_blue_9s", Image.Type.Sliced, false, 2f);
''', 1)
daily_path.write_text(daily, encoding="utf-8")


# Private Room ownership/fidelity cleanup.
pr_path = Path("Assets/SCRIPT/Design/PrivateRoomVisuals.cs")
pr = pr_path.read_text(encoding="utf-8")
pr = pr.replace(
    '    const string TipIconResource = "mainmenu/mainmenu_icon_tip_bulb";\n',
    '    const string TipIconResource = "mainmenu/mainmenu_icon_tip_bulb";\n'
    '    const string StreakIconResource = "mainmenu/mainmenu_icon_streak";\n',
    1,
)
pr = pr.replace(
    "        Sprite chevron = LoadRequired(BackChevronResource);\n",
    "        Sprite chevron = LoadRequired(BackChevronResource);\n"
    "        Sprite streakIcon = LoadRequired(StreakIconResource);\n",
    1,
)
pr = pr.replace(
    "            purple != null && chip != null && tip != null && chevron != null &&\n",
    "            purple != null && chip != null && tip != null && chevron != null &&\n"
    "            streakIcon != null &&\n",
    1,
)
pr = pr.replace(
    "        outer.color = new Color(0.80f, 0.68f, 1f, 0.70f);",
    "        outer.color = Color.white;",
    1,
)
pr = pr.replace(
    "        BuildTopBar(chip, avatar, purple, chevron);",
    "        BuildTopBar(chip, avatar, purple, chevron, streakIcon);",
    1,
)
pr = pr.replace(
    "    void BuildTopBar(Sprite chipSprite, Sprite avatar, Sprite pillSprite,\n        Sprite chevron)\n",
    "    void BuildTopBar(Sprite chipSprite, Sprite avatar, Sprite pillSprite,\n        Sprite chevron, Sprite streakIcon)\n",
    1,
)
old_step = '        stepText.text = IsGreek ? "2. ΠΑΙΞΕ ΜΕ ΦΙΛΟ" : "2. PLAY WITH A FRIEND";'
require(old_step in pr, "PrivateRoom step copy changed")
pr = pr.replace(old_step, '        RuntimeUI.Localize(stepText, "private_room_step");', 1)
streak_place = '''        Place(streakText.rectTransform, new Vector2(38f, -27f),
            new Vector2(210f, 40f));
'''
require(streak_place in pr, "PrivateRoom streak layout changed")
pr = pr.replace(streak_place, '''        Place(streakText.rectTransform, new Vector2(55f, -27f),
            new Vector2(120f, 40f));
        var streakImage = EnsureImage(chip.transform, "PrivateRoomStreakIcon");
        ConfigureImage(streakImage, streakIcon, true, Image.Type.Simple);
        Place(streakImage.rectTransform, new Vector2(-20f, -27f),
            new Vector2(44f, 44f));
''', 1)
old_refresh = '''        if (stepText != null)
            stepText.text = IsGreek ? "2. ΠΑΙΞΕ ΜΕ ΦΙΛΟ" : "2. PLAY WITH A FRIEND";
'''
require(old_refresh in pr, "PrivateRoom refresh step copy changed")
pr = pr.replace(old_refresh, '''        if (stepText != null)
            stepText.text = L10n.Get("private_room_step");
''', 1)
pr = pr.replace(
    '        streakText.text = "🔥 " + GameStats.CurrentStreak;',
    "        streakText.text = GameStats.CurrentStreak.ToString();",
    1,
)
label_line = "        var label = button.GetComponentInChildren<TMP_Text>(true);\n"
require(label_line in pr, "PrivateRoom ConfigureButtonLabel lookup changed")
pr = pr.replace(
    label_line,
    '        var label = DirectChild(button.transform, "PrivateRoomActionLabel")?.GetComponent<TMP_Text>();\n',
    1,
)
pr = pr.replace(
    '            label = EnsureText(button.transform, "Label", size, displayFont,\n',
    '            label = EnsureText(button.transform, "PrivateRoomActionLabel", size, displayFont,\n',
    1,
)
old_clear = '''    static void ClearButtonPresentation(Transform root)
    {
        if (root == null) return;
        for (int i = root.childCount - 1; i >= 0; i--)
            RuntimeUI.DestroyNow(root.GetChild(i).gameObject);
    }
'''
require(old_clear in pr, "PrivateRoom clear helper changed")
pr = pr.replace(old_clear, '''    static void ClearButtonPresentation(Transform root)
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
''', 1)
pr_path.write_text(pr, encoding="utf-8")


# Canonical EN/EL copy for the top-left step pill.
l10n_path = Path("Assets/SCRIPT/Localization/L10n.cs")
l10n = l10n_path.read_text(encoding="utf-8")
if '"private_room_step"' not in l10n:
    line_match = re.search(r'(?m)^(\s*\{\s*"private_room_title"[^\n]*\n)', l10n)
    require(line_match, "L10n private-room title line not found")
    insertion = line_match.group(1) + (
        '        { "private_room_step",       new[] { "2. PLAY WITH A FRIEND", '
        '"2. ΠΑΙΞΕ ΜΕ ΦΙΛΟ" } },\n'
    )
    l10n = l10n[:line_match.start()] + insertion + l10n[line_match.end():]
l10n_path.write_text(l10n, encoding="utf-8")


# Tests/docs: no deleted shim requirement, no reflection-install of removed owners.
exact_path = Path("Assets/Tests/EditMode/ExactReferenceAssetsTests.cs")
exact = exact_path.read_text(encoding="utf-8")
shim_pattern = re.compile(
    r"    \[Test\]\n    public void SerializedLegacyThemeShimHasNoRuntimeThemeLifecycle\(\)\n    \{.*?\n    \}\n",
    re.S,
)
exact, n = shim_pattern.subn('''    [Test]
    public void RuntimeUiInfrastructureExistsWithoutSerializedGlobalThemeOwner()
    {
        Assert.IsNotNull(RuntimeType("RuntimeUI"));
        Assert.IsNull(System.Type.GetType("DesignRuntimeWiring, Assembly-CSharp"));
    }
''', exact, count=1)
require(n == 1, "ExactReferenceAssets serialized-shim test changed")
exact_path.write_text(exact, encoding="utf-8")

splash_test_path = Path("Assets/Tests/PlayMode/SplashAuthoritativeVisualsPlayModeTests.cs")
splash_test = splash_test_path.read_text(encoding="utf-8")
for line in [
    '        InstallRuntimePresenter("ExactReferenceVisuals");\n',
    '        InstallRuntimePresenter("AttachmentReskinVisuals");\n',
    '        InstallRuntimePresenter("AttachmentReskinPolish");\n',
    '        InstallRuntimePresenter("AttachmentReskinCanvasBindings");\n',
    '        Assert.That(FindInScene(scene, RuntimeType("ExactReferenceVisuals")), Is.Null);\n',
    '        Assert.That(FindInScene(scene, RuntimeType("AttachmentReskinVisuals")), Is.Null);\n',
    '        Assert.That(FindInScene(scene, RuntimeType("AttachmentReskinPolish")), Is.Null);\n',
    '        Assert.That(FindInScene(scene, RuntimeType("AttachmentReskinCanvasBindings")), Is.Null);\n',
]:
    splash_test = splash_test.replace(line, "")
splash_test = re.sub(
    r"    static void InstallRuntimePresenter\(string typeName\)\n    \{.*?\n    \}\n\n",
    "",
    splash_test,
    count=1,
    flags=re.S,
)
splash_test_path.write_text(splash_test, encoding="utf-8")

screen_map = Path("Assets/newdesign/screen-map.md")
if screen_map.exists():
    text = screen_map.read_text(encoding="utf-8")
    text = text.replace(
        "`UIJuice/*`, `Haptics`, `DesignRuntimeWiring`",
        "`UIJuice/*`, `Haptics`, screen-specific `Design/*` owners",
    )
    screen_map.write_text(text, encoding="utf-8")


# Shared legacy frame facade is now obsolete.
neon = Path("Assets/SCRIPT/Design/NeonFrame.cs")
neon_meta = Path("Assets/SCRIPT/Design/NeonFrame.cs.meta")
require(neon.exists(), "NeonFrame already missing before phase B")
neon.unlink()
if neon_meta.exists():
    neon_meta.unlink()
