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
    // Functional fallback colors only; current screen owners assign production sprites.
    static readonly Color Neutral = HolUiStateColors.SurfaceElevated;
    static readonly Color DarkLabel = new Color(0.10f, 0.09f, 0.18f);

    TMP_Text disclosurePlay;

    void Start()
    {
        StartCoroutine(WireNextFrame());
    }

    IEnumerator WireNextFrame()
    {
        yield return null; // let every other Start() finish first

        NormalizeReferencePanels();
        EnsureDailyStreak();
        WireDailyHunt();
        WireRematchButton();
        WireNumberInputSubmit();
        WireMatchmakingCancel();
        WireLanguageButtons();
        WireConsentSettings();
        WireDifficultyButtons();
        AddDisclosureLabels();
        LocalizeSceneTexts();
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

    // The hand-authored pages were historically scaled to 1.1 while runtime
    // coordinates and the CanvasScaler are authored in reference pixels. Reset
    // only full-screen page roots so every page shares one coordinate system.
    void NormalizeReferencePanels()
    {
        string[] pageNames = { "PanelGAME", "PanelPlay", "PanelSearching", "PanelSettings" };
        foreach (var rect in FindObjectsOfType<RectTransform>(true))
        {
            bool isPage = false;
            for (int i = 0; i < pageNames.Length; i++)
                if (rect.name == pageNames[i]) { isPage = true; break; }
            if (!isPage) continue;

            if (rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one)
                rect.localScale = Vector3.one;
        }
    }

    // DailyStreak is not placed in any scene; attach it here so the streak
    // hook actually counts (it self-registers in its own Start).
    void EnsureDailyStreak()
    {
        if (FindFirstObjectByType<DailyStreak>() == null)
            gameObject.AddComponent<DailyStreak>();
    }

    // The Daily Hunt panel and its menu entry, built the same zero-scene-edit
    // way as the PvP screens. Runs after PvpRuntimeUI's Start so the entry
    // button can seat itself right below the PvP one.
    void WireDailyHunt()
    {
        var canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null || FindFirstObjectByType<DailyHunt>() != null)
            return;

        DailyHunt.Attach(canvasGo.transform, FindFirstObjectByType<AdsManager>());
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
        ApplyWiringSprite(button, "mainmenu/mainmenu_cta_blue_9s");
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
            ApplyWiringSprite(button, "mainmenu/mainmenu_tip_frame_9s");
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
        ApplyWiringSprite(cancel, "mainmenu/mainmenu_cta_blue_9s");
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
            "EnglishButton", L10n.Get("language_english"),
            new Vector2(-130f, -560f), new Vector2(220f, 80f), Neutral);
        ApplyWiringSprite(englishButton, "mainmenu/mainmenu_tip_frame_9s");
        englishButton.onClick.AddListener(selector.SetEnglish);
        RuntimeUI.Localize(englishButton, "language_english");

        greekButton = RuntimeUI.CreateButton(menu.settingsPanel.transform,
            "GreekButton", L10n.Get("language_greek"),
            new Vector2(130f, -560f), new Vector2(220f, 80f), Neutral);
        ApplyWiringSprite(greekButton, "mainmenu/mainmenu_tip_frame_9s");
        greekButton.onClick.AddListener(selector.SetGreek);
        RuntimeUI.Localize(greekButton, "language_greek");

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
        if (button == null) return;
        ApplyWiringSprite(button, selected
            ? "mainmenu/mainmenu_cta_gold_9s"
            : "mainmenu/mainmenu_tip_frame_9s");

        var label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.color = selected ? DarkLabel : new Color(0.91f, 0.93f, 1f);
    }

    static void ApplyWiringSprite(Button button, string resource)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image == null) return;
        RuntimeUI.ApplyProductionSprite(image, resource, Image.Type.Sliced,
            false, 2f);
        image.raycastTarget = true;
        button.targetGraphic = image;
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

        RefreshDisclosure();
        L10n.OnLanguageChanged += RefreshDisclosure;
    }

    void RefreshDisclosure()
    {
        string text = L10n.Get("simulated_opponents");
        if (disclosurePlay != null)
            disclosurePlay.text = text;
    }

    void OnDestroy()
    {
        L10n.OnLanguageChanged -= RefreshDisclosure;
        L10n.OnLanguageChanged -= RefreshLanguageButtons;
        L10n.OnLanguageChanged -= RefreshLegacySceneTexts;
    }
}
