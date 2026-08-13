using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Zero-scene-edit wiring pass for features whose scene buttons were never
// hooked up (their target methods were added/renamed during code reviews).
// Runs one frame after Start so every other component has initialized.
//
//   1. End-game "stop" button  -> rewired to GameManager.RestartMatch,
//      relabeled "Rematch" (old StopGame no longer exists).
//   2. Matchmaking search panel -> gets a Cancel button (CancelSearch).
//   3. Settings panel -> gets EN/EL language buttons (LanguageSelector).
//   4. Main menu -> gets a stats label fed by GameStats.
//   5. Solo matchmaking panels -> "opponents are simulated" disclosure.
//   6. Settings -> "Ads privacy" button re-opens the consent dialog.
//   7. Attaches DailyStreak (it is placed in no scene) so streaks count.
//   8. Scene-authored English labels -> LocalizedText via content mapping.
//   9. Settings -> difficulty selector (Easy/Normal/Hard/Adaptive).
public class ExtrasRuntimeWiring : MonoBehaviour
{
    // Converging Light palette (design/philosophy.md), theme-routed.
    static Color Neutral => ConvergingLightFX.GhostSurface;
    static Color DarkLabel => ConvergingLightFX.DarkLabel;

    Text statsLabel;
    Text disclosurePlay;
    Text disclosureSearch;

    void Start()
    {
        StartCoroutine(WireNextFrame());
    }

    IEnumerator WireNextFrame()
    {
        yield return null; // let every other Start() finish first

        Analytics.Boot();
        DailyReminder.EnsureScheduled();
        EnsureDailyStreak();
        WireDailyHunt();
        WireRematchButton();
        WireNumberInputSubmit();
        WireMatchmakingCancel();
        WireLanguageButtons();
        WireConsentSettings();
        WireDifficultyButtons();
        WireThemeChips();
        WireModeChips();
        WireRangeAndHistories();
        WireHowToPlay();
        AddStatsLabel();
        AddDisclosureLabels();
        LocalizeSceneTexts();
    }

    // --- 14. Theme swatches ---------------------------------------------------
    //
    // Cosmetic palettes over the whole design system, unlocked through play.
    // The UI bakes palette colors at scene build, so applying a theme
    // reloads the scene; tapping a locked swatch explains its unlock.

    void WireThemeChips()
    {
        var menu = FindObjectOfType<MenuManager>();
        if (menu == null || menu.settingsPanel == null)
            return;

        var themeLabel = RuntimeUI.CreateText(menu.settingsPanel.transform, "ThemeLabel",
            L10n.Get("theme"), 32, new Vector2(0f, 330f), new Vector2(400f, 50f));
        RuntimeUI.Localize(themeLabel, "theme");

        var hint = RuntimeUI.CreateText(menu.settingsPanel.transform, "ThemeHint",
            "", 24, new Vector2(0f, 158f), new Vector2(760f, 44f),
            ConvergingLight.WithAlpha(ConvergingLight.NearWhite, 0.7f));

        for (int i = 0; i < Themes.All.Length; i++)
        {
            var theme = Themes.All[i];
            bool unlocked = Themes.IsUnlocked(theme);
            bool current = theme.id == Themes.CurrentId;

            var chip = RuntimeUI.CreateButton(menu.settingsPanel.transform,
                "Theme_" + theme.id, L10n.Get(theme.nameKey),
                new Vector2(-300f + i * 200f, 240f), new Vector2(184f, 84f),
                current ? theme.palette.gold : ConvergingLight.WithAlpha(theme.palette.panelIndigo, 1f),
                current ? DarkLabel : ConvergingLight.WithAlpha(theme.palette.nearWhite, unlocked ? 1f : 0.45f));
            RuntimeUI.Localize(chip, theme.nameKey);

            var captured = theme;
            chip.onClick.AddListener(() =>
            {
                if (!Themes.IsUnlocked(captured))
                {
                    hint.text = Themes.LockHint(captured);
                    return;
                }
                if (captured.id == Themes.CurrentId)
                    return;

                Themes.Apply(captured);
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            });
        }
    }

    // --- 13. Solo mode chips --------------------------------------------------
    //
    // Classic / Sudden Death / Time Attack, chosen on the challenger panel
    // and read by GameManager per match. Selection mirrors the difficulty
    // row (active chip gold).

    readonly Button[] modeChips = new Button[3];

    void WireModeChips()
    {
        var menu = FindObjectOfType<MenuManager>();
        if (menu == null || menu.panelPlay == null)
            return;

        var label = RuntimeUI.CreateText(menu.panelPlay.transform, "ModeLabel",
            L10n.Get("mode"), 30, new Vector2(0f, 80f), new Vector2(400f, 46f));
        RuntimeUI.Localize(label, "mode");

        string[] keys = { "mode_classic", "mode_sudden", "mode_timed" };
        for (int i = 0; i < modeChips.Length; i++)
        {
            int m = i; // captured for the lambda
            var chip = RuntimeUI.CreateButton(menu.panelPlay.transform, "ModeChip" + i,
                L10n.Get(keys[i]), new Vector2(-250f + i * 250f, 0f),
                new Vector2(230f, 78f), Neutral);
            RuntimeUI.Localize(chip, keys[i]);
            chip.onClick.AddListener(() => SetSoloMode(m));
            modeChips[i] = chip;
        }

        RefreshModeChips();
    }

    void SetSoloMode(int mode)
    {
        PlayerPrefs.SetInt(GameManager.ModePrefKey, mode);
        PlayerPrefs.Save();
        RefreshModeChips();
    }

    void RefreshModeChips()
    {
        int current = Mathf.Clamp(PlayerPrefs.GetInt(GameManager.ModePrefKey, 0), 0, 2);
        for (int i = 0; i < modeChips.Length; i++)
            TintSelectable(modeChips[i], i == current);
    }

    // --- 10. Converging range + guess histories ------------------------------
    //
    // GameManager has tracked the player's narrowed interval and both guess
    // histories since launch, but its optional UI fields were never wired in
    // the scene — the game's core mental model was invisible. Build the
    // RangeBar plus history labels on the game panel and hand them over.

    void WireRangeAndHistories()
    {
        var gm = FindObjectOfType<GameManager>();
        var mm = FindObjectOfType<FakeMatchmaking>();
        if (gm == null || mm == null || mm.panelGame == null)
            return;

        var panel = mm.panelGame.transform;

        RangeBar.Attach(panel, gm, new Vector2(0f, 300f));

        if (gm.rangeText == null)
        {
            gm.rangeText = RuntimeUI.CreateTmpText(panel, "RangeCaption", "", 26,
                new Vector2(0f, 238f), new Vector2(520f, 40f),
                ConvergingLight.WithAlpha(ConvergingLight.NearWhite, 0.6f));
        }

        if (gm.playerHistoryText == null)
        {
            gm.playerHistoryText = RuntimeUI.CreateTmpText(panel, "PlayerHistory", "", 28,
                new Vector2(-240f, 420f), new Vector2(460f, 60f),
                ConvergingLight.WithAlpha(ConvergingLight.NearWhite, 0.75f));
        }
        if (gm.aiHistoryText == null)
        {
            gm.aiHistoryText = RuntimeUI.CreateTmpText(panel, "AiHistory", "", 28,
                new Vector2(240f, 420f), new Vector2(460f, 60f),
                ConvergingLight.WithAlpha(ConvergingLight.NearWhite, 0.75f));
        }

        if (gm.modeStatusText == null)
        {
            gm.modeStatusText = RuntimeUI.CreateTmpText(panel, "ModeStatus", "", 30,
                new Vector2(0f, -420f), new Vector2(500f, 44f),
                ConvergingLight.WithAlpha(ConvergingLight.Gold, 0.9f));
        }
    }

    // --- 12. Daily Hunt -------------------------------------------------------
    //
    // The daily ritual: same seeded number for everyone, one attempt a day,
    // shareable result. DailyHunt builds its own panel; the entry button
    // lives on the menu below the PvP entry.

    void WireDailyHunt()
    {
        var menu = FindObjectOfType<MenuManager>();
        if (menu == null || menu.mainMenuPanel == null)
            return;

        var canvas = menu.mainMenuPanel.GetComponentInParent<Canvas>(true);
        if (canvas == null)
            return;

        DailyHunt.Attach(canvas.transform, menu.mainMenuPanel.transform,
            FindFirstObjectByType<AdsManager>());
    }

    // --- 11. How to play ------------------------------------------------------
    //
    // The game never explained itself. A rules card shows once on first
    // launch (after the consent choice exists, so modals never stack) and
    // stays reachable from a small "?" on the menu.

    const string HowToSeenKey = "HowToPlaySeen";
    GameObject howToPanel;

    void WireHowToPlay()
    {
        var menu = FindObjectOfType<MenuManager>();
        if (menu == null || menu.mainMenuPanel == null)
            return;

        var canvas = menu.mainMenuPanel.GetComponentInParent<Canvas>(true);
        if (canvas == null)
            return;

        BuildHowToPanel(canvas.transform);

        var help = RuntimeUI.CreateButton(menu.mainMenuPanel.transform, "HowToButton",
            "?", new Vector2(-460f, 850f), new Vector2(84f, 84f),
            ConvergingLightFX.GhostSurface);
        help.onClick.AddListener(() => howToPanel.SetActive(true));

        // First run: show the rules once, but never stack over the consent
        // dialog — if consent is still unanswered, wait for the next launch.
        if (PlayerPrefs.GetInt(HowToSeenKey, 0) == 0 && PlayerPrefs.HasKey("AdsConsent"))
            howToPanel.SetActive(true);
    }

    void BuildHowToPanel(Transform canvas)
    {
        howToPanel = RuntimeUI.FullscreenPanel(canvas, "HowToPanel",
            ConvergingLight.WithAlpha(ConvergingLight.ScrimIndigo, 0.9f));

        var card = RuntimeUI.CreateObject("Card", howToPanel.transform);
        ConvergingLight.Center(card, new Vector2(0f, 40f), new Vector2(900f, 1060f));
        var surface = card.AddComponent<Image>();
        surface.raycastTarget = true;
        ConvergingLightFX.StyleCard(surface, 0.985f);

        var title = RuntimeUI.CreateTmpText(card.transform, "Title",
            L10n.Get("how_to_play"), 46, new Vector2(0f, 440f), new Vector2(760f, 80f));
        ConvergingLightFX.StyleHeading(title, 4f);
        RuntimeUI.Localize(title, "how_to_play");

        ConvergingLightFX.AddSeamRule(card.transform, new Vector2(0f, 380f), 420f);

        var body = RuntimeUI.CreateTmpText(card.transform, "Body",
            L10n.Get("howto_body"), 32, new Vector2(0f, 20f), new Vector2(760f, 620f));
        body.alignment = TMPro.TextAlignmentOptions.Top;
        RuntimeUI.Localize(body, "howto_body");

        var gotIt = RuntimeUI.CreateButton(card.transform, "GotItButton",
            L10n.Get("got_it"), new Vector2(0f, -440f), new Vector2(400f, 92f),
            ConvergingLight.Gold, ConvergingLightFX.DarkLabel);
        RuntimeUI.Localize(gotIt, "got_it");
        gotIt.onClick.AddListener(() =>
        {
            PlayerPrefs.SetInt(HowToSeenKey, 1);
            PlayerPrefs.Save();
            howToPanel.SetActive(false);
        });

        howToPanel.AddComponent<PanelAnimator>();
        howToPanel.SetActive(false);
    }

    // --- 8. Scene-authored static labels ------------------------------------
    //
    // The scene's static labels were authored in English (with letter-spaced
    // styling like "S A V E"; a few historical typos have since been fixed in
    // the scene, and their normalized forms are kept below as fallbacks) and
    // have no LocalizedText attached. Map them by normalized content
    // (uppercase, spaces and zero-width spaces stripped) onto L10n keys: TMP
    // labels get a live LocalizedText, legacy Text labels are refreshed on
    // language change.

    static readonly System.Collections.Generic.Dictionary<string, string> SceneTextKeys =
        new System.Collections.Generic.Dictionary<string, string>
    {
        { "SEARCHCHALLENGER", "find_challenger" },
        { "SAVE", "save" },
        { "CORRECT", "correct" },
        { "CONFIRMNUMBER", "confirm" },
        { "BACK", "back" },
        { "HIGHER", "higher" },
        { "LOWER", "lower" },
        { "ENTERANUMBER(1-100)", "enter_your_number" },
        { "ENTERANUBMER(1-100)", "enter_your_number" }, // legacy scene typo (fixed in scene; kept as fallback)
        { "ENTERTEXT...", "number_placeholder" }, // number-input placeholder
        { "ENTERYOURNAME..", "player_name" },
        { "GUESSES:", "guesses" },
        { "GUSEEES:", "guesses" }, // scene typo
        { "YOURNUMBER?(1-100)", "your_number" },
        { "MUSIC", "music" },
        { "STOPGAME", "stop_game" },
    };

    readonly System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<Text, string>> legacySceneTexts =
        new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<Text, string>>();

    static string NormalizeSceneText(string s)
    {
        return (s ?? "").Replace(" ", "").Replace("\u200B", "").ToUpperInvariant();
    }

    void LocalizeSceneTexts()
    {
        foreach (var tmp in FindObjectsOfType<TMP_Text>(true))
        {
            string key;
            if (!SceneTextKeys.TryGetValue(NormalizeSceneText(tmp.text), out key))
                continue;
            if (tmp.GetComponent<LocalizedText>() != null)
                continue;

            var localized = tmp.gameObject.AddComponent<LocalizedText>();
            localized.key = key;
            localized.enabled = false; // force re-apply
            localized.enabled = true;
        }

        foreach (var legacy in FindObjectsOfType<Text>(true))
        {
            string key;
            if (SceneTextKeys.TryGetValue(NormalizeSceneText(legacy.text), out key))
            {
                legacy.text = L10n.Get(key);
                legacySceneTexts.Add(new System.Collections.Generic.KeyValuePair<Text, string>(legacy, key));
            }
        }

        if (legacySceneTexts.Count > 0)
            L10n.OnLanguageChanged += RefreshLegacySceneTexts;
    }

    void RefreshLegacySceneTexts()
    {
        foreach (var pair in legacySceneTexts)
            if (pair.Key != null)
                pair.Key.text = L10n.Get(pair.Value);
    }

    // DailyStreak is not placed in any scene; attach it here so the streak
    // hook actually counts (it self-registers in its own Start).
    void EnsureDailyStreak()
    {
        if (FindFirstObjectByType<DailyStreak>() == null)
            gameObject.AddComponent<DailyStreak>();
    }

    // "Ads privacy" button in Settings -> re-opens the consent dialog.
    void WireConsentSettings()
    {
        var menu = FindObjectOfType<MenuManager>();
        var consent = FindFirstObjectByType<ConsentManager>();
        if (menu == null || menu.settingsPanel == null || consent == null)
            return;

        var button = RuntimeUI.CreateButton(menu.settingsPanel.transform,
            "AdsPrivacyButton", L10n.Get("ads_privacy"),
            new Vector2(0f, -680f), new Vector2(360f, 80f), Neutral);
        button.onClick.AddListener(consent.ReopenConsent);
        RuntimeUI.Localize(button, "ads_privacy");
    }

    // --- 3b. Difficulty selector ---------------------------------------------
    //
    // GameManager supports Easy/Normal/Hard/Adaptive via the "AIDifficulty"
    // PlayerPrefs key (read per AI guess, so changes apply immediately) but
    // shipped with no UI. These buttons expose it in Settings; the active
    // choice is tinted gold.

    const string DifficultyPrefKey = "AIDifficulty"; // mirrors GameManager

    static Color Gold => ConvergingLight.Gold;

    readonly Button[] difficultyButtons = new Button[4];

    void WireDifficultyButtons()
    {
        var menu = FindObjectOfType<MenuManager>();
        if (menu == null || menu.settingsPanel == null)
            return;

        var difficultyLabel = RuntimeUI.CreateText(menu.settingsPanel.transform, "DifficultyLabel",
            L10n.Get("difficulty"), 32,
            new Vector2(0f, -780f), new Vector2(400f, 50f));
        RuntimeUI.Localize(difficultyLabel, "difficulty");

        string[] keys = { "easy", "normal", "hard", "adaptive" };
        for (int i = 0; i < 4; i++)
        {
            int difficulty = i; // captured for the lambda
            var button = RuntimeUI.CreateButton(menu.settingsPanel.transform,
                "Difficulty" + i, L10n.Get(keys[i]),
                new Vector2(-300f + i * 200f, -860f), new Vector2(180f, 70f), Neutral);
            button.onClick.AddListener(() => SetDifficulty(difficulty));
            RuntimeUI.Localize(button, keys[i]);
            difficultyButtons[i] = button;
        }

        RefreshDifficultyButtons();
    }

    void SetDifficulty(int difficulty)
    {
        PlayerPrefs.SetInt(DifficultyPrefKey, difficulty);
        PlayerPrefs.Save();
        RefreshDifficultyButtons();
    }

    void RefreshDifficultyButtons()
    {
        int current = Mathf.Clamp(PlayerPrefs.GetInt(DifficultyPrefKey, 1), 0, 3);
        for (int i = 0; i < difficultyButtons.Length; i++)
            TintSelectable(difficultyButtons[i], i == current);
    }

    // --- 1. Rematch -------------------------------------------------------

    void WireRematchButton()
    {
        var gm = FindObjectOfType<GameManager>();
        if (gm == null || gm.stopGameButton == null)
            return;

        var button = gm.stopGameButton.GetComponent<Button>();
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(gm.RestartMatch);

        // Relabel to "Rematch" and keep it following the language.
        var tmp = gm.stopGameButton.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
        {
            var localized = tmp.GetComponent<LocalizedText>();
            if (localized == null)
                localized = tmp.gameObject.AddComponent<LocalizedText>();
            localized.key = "rematch";
            localized.enabled = false; // force re-apply below
            localized.enabled = true;
        }
        else
        {
            var legacy = gm.stopGameButton.GetComponentInChildren<Text>(true);
            if (legacy != null)
                legacy.text = L10n.Get("rematch");
        }
    }

    // --- 1b. Keyboard submit --------------------------------------------------
    //
    // The soft keyboard's Done key (Enter in the editor) submits the number,
    // instead of forcing a reach for the Confirm button. SubmitNumber already
    // validates turn/range, so a mistimed submit just shows the usual message.

    void WireNumberInputSubmit()
    {
        // includeInactive: NumberManager sits on PanelGAME, which is inactive
        // until a match starts — a plain FindObjectOfType would miss it and
        // silently never wire the feature.
        var nm = FindObjectOfType<NumberManager>(true);
        if (nm == null || nm.numberInput == null)
            return;

        nm.numberInput.onSubmit.AddListener(_ => nm.SubmitNumber());

        // The field only ever takes 1-100: numeric soft keyboard, three
        // characters, no pasted letters reaching validation.
        nm.numberInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        nm.numberInput.characterLimit = 3;
    }

    // --- 2. Matchmaking cancel ---------------------------------------------

    void WireMatchmakingCancel()
    {
        var mm = FindObjectOfType<FakeMatchmaking>();
        if (mm == null || mm.searchingPanel == null)
            return;

        var cancel = RuntimeUI.CreateButton(mm.searchingPanel.transform,
            "CancelButton", L10n.Get("cancel"),
            new Vector2(0f, -420f), new Vector2(300f, 80f), Neutral);
        cancel.onClick.AddListener(mm.CancelSearch);
        RuntimeUI.Localize(cancel, "cancel");
    }

    // --- 3. Language buttons ------------------------------------------------

    void WireLanguageButtons()
    {
        var menu = FindObjectOfType<MenuManager>();
        if (menu == null || menu.settingsPanel == null)
            return;

        var selector = menu.settingsPanel.GetComponentInChildren<LanguageSelector>(true);
        if (selector == null)
            selector = menu.settingsPanel.AddComponent<LanguageSelector>();

        var languageLabel = RuntimeUI.CreateText(menu.settingsPanel.transform, "LanguageLabel",
            L10n.Get("language"), 32,
            new Vector2(0f, -480f), new Vector2(400f, 50f));
        RuntimeUI.Localize(languageLabel, "language");

        englishButton = RuntimeUI.CreateButton(menu.settingsPanel.transform,
            "EnglishButton", "English",
            new Vector2(-130f, -560f), new Vector2(220f, 80f), Neutral);
        englishButton.onClick.AddListener(selector.SetEnglish);

        greekButton = RuntimeUI.CreateButton(menu.settingsPanel.transform,
            "GreekButton", "Ελληνικά",
            new Vector2(130f, -560f), new Vector2(220f, 80f), Neutral);
        greekButton.onClick.AddListener(selector.SetGreek);

        RefreshLanguageButtons();
        L10n.OnLanguageChanged += RefreshLanguageButtons;
    }

    // Mirror the difficulty row: the active language is tinted gold so the
    // current choice is visible at a glance (before, both buttons looked
    // identical whichever language was on).
    Button englishButton;
    Button greekButton;

    void RefreshLanguageButtons()
    {
        bool english = L10n.Current == L10n.Language.English;
        TintSelectable(englishButton, english);
        TintSelectable(greekButton, !english);
    }

    static void TintSelectable(Button button, bool selected)
    {
        if (button == null)
            return;

        var image = button.GetComponent<Image>();
        if (image != null)
            image.color = selected ? Gold : Neutral;

        var label = button.GetComponentInChildren<Text>();
        if (label != null)
            label.color = selected ? DarkLabel : new Color(0.91f, 0.93f, 1f);
    }

    // --- 4. Stats label ------------------------------------------------------

    void AddStatsLabel()
    {
        var menu = FindObjectOfType<MenuManager>();
        if (menu == null || menu.mainMenuPanel == null)
            return;

        statsLabel = RuntimeUI.CreateText(menu.mainMenuPanel.transform,
            "StatsLabel", "", 28,
            new Vector2(0f, 820f), new Vector2(700f, 90f),
            new Color(0.91f, 0.93f, 1f, 0.8f));

        RefreshStats();
        L10n.OnLanguageChanged += RefreshStats;
        GameEvents.OnMatchEnded += OnMatchEnded;
    }

    void OnMatchEnded(bool playerWon, int guesses)
    {
        RefreshStats();
    }

    void RefreshStats()
    {
        string summary = L10n.Get("stats_wins") + ": " + GameStats.Wins +
            "   " + L10n.Get("stats_losses") + ": " + GameStats.Losses +
            "\n" + L10n.Get("stats_streak") + ": " + GameStats.CurrentStreak +
            "   " + L10n.Get("stats_best") + ": " + GameStats.BestStreak;

        // Fewest-guesses win: tracked since launch but never surfaced.
        if (GameStats.BestWinningGuesses > 0)
            summary += "\n" + L10n.Get("stats_fastest_win", GameStats.BestWinningGuesses);

        if (statsLabel != null)
            statsLabel.text = summary;
    }

    // --- 5. Simulated-opponent disclosure -----------------------------------
    //
    // The solo "Find challenger" flow uses a simulated on-device opponent;
    // honesty requires telling the player. (PvP Duel is real multiplayer and
    // lives on separate panels, so it gets no such label.)

    void AddDisclosureLabels()
    {
        var menu = FindObjectOfType<MenuManager>();
        if (menu != null && menu.panelPlay != null)
        {
            disclosurePlay = RuntimeUI.CreateText(menu.panelPlay.transform,
                "DisclosureLabel", "", 22,
                new Vector2(0f, -560f), new Vector2(760f, 70f),
                new Color(0.91f, 0.93f, 1f, 0.6f));
        }

        var mm = FindObjectOfType<FakeMatchmaking>();
        if (mm != null && mm.searchingPanel != null)
        {
            disclosureSearch = RuntimeUI.CreateText(mm.searchingPanel.transform,
                "DisclosureLabel", "", 22,
                new Vector2(0f, -540f), new Vector2(760f, 70f),
                new Color(0.91f, 0.93f, 1f, 0.6f));
        }

        RefreshDisclosure();
        L10n.OnLanguageChanged += RefreshDisclosure;
    }

    void RefreshDisclosure()
    {
        string text = L10n.Get("simulated_opponents");
        if (disclosurePlay != null)
            disclosurePlay.text = text;
        if (disclosureSearch != null)
            disclosureSearch.text = text;
    }

    void OnDestroy()
    {
        L10n.OnLanguageChanged -= RefreshStats;
        L10n.OnLanguageChanged -= RefreshDisclosure;
        L10n.OnLanguageChanged -= RefreshLanguageButtons;
        L10n.OnLanguageChanged -= RefreshLegacySceneTexts;
        GameEvents.OnMatchEnded -= OnMatchEnded;
    }
}
