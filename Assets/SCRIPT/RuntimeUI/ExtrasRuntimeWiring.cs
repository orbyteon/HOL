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
    // Converging Light palette (design/philosophy.md).
    static readonly Color AccentBlue = new Color(0.25f, 0.85f, 1f); // cyan seam
    static readonly Color Neutral = new Color(0.16f, 0.15f, 0.26f);   // indigo gray
    static readonly Color DarkLabel = new Color(0.10f, 0.09f, 0.18f);

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

        EnsureDailyStreak();
        WireRematchButton();
        WireMatchmakingCancel();
        WireLanguageButtons();
        WireConsentSettings();
        WireDifficultyButtons();
        AddStatsLabel();
        AddDisclosureLabels();
        LocalizeSceneTexts();
    }

    // --- 8. Scene-authored static labels ------------------------------------
    //
    // The scene's static labels were authored in English (with letter-spaced
    // styling like "S A V E" and a few typos like "Enter a nubmer") and have
    // no LocalizedText attached. Map them by normalized content (uppercase,
    // spaces and zero-width spaces stripped) onto L10n keys: TMP labels get a
    // live LocalizedText, legacy Text labels are refreshed on language change.

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
        { "ENTERANUBMER(1-100)", "enter_your_number" }, // scene typo
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
    }

    // --- 3b. Difficulty selector ---------------------------------------------
    //
    // GameManager supports Easy/Normal/Hard/Adaptive via the "AIDifficulty"
    // PlayerPrefs key (read per AI guess, so changes apply immediately) but
    // shipped with no UI. These buttons expose it in Settings; the active
    // choice is tinted gold.

    const string DifficultyPrefKey = "AIDifficulty"; // mirrors GameManager

    static readonly Color Gold = new Color(1f, 0.78f, 0.34f);

    readonly Button[] difficultyButtons = new Button[4];

    void WireDifficultyButtons()
    {
        var menu = FindObjectOfType<MenuManager>();
        if (menu == null || menu.settingsPanel == null)
            return;

        RuntimeUI.CreateText(menu.settingsPanel.transform, "DifficultyLabel",
            L10n.Get("difficulty"), 32,
            new Vector2(0f, -780f), new Vector2(400f, 50f));

        string[] keys = { "easy", "normal", "hard", "adaptive" };
        for (int i = 0; i < 4; i++)
        {
            int difficulty = i; // captured for the lambda
            var button = RuntimeUI.CreateButton(menu.settingsPanel.transform,
                "Difficulty" + i, L10n.Get(keys[i]),
                new Vector2(-300f + i * 200f, -860f), new Vector2(180f, 70f), Neutral);
            button.onClick.AddListener(() => SetDifficulty(difficulty));
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
        {
            if (difficultyButtons[i] == null)
                continue;

            bool selected = i == current;

            var image = difficultyButtons[i].GetComponent<Image>();
            if (image != null)
                image.color = selected ? Gold : Neutral;

            // Keep label contrast with the button tint.
            var label = difficultyButtons[i].GetComponentInChildren<Text>();
            if (label != null)
                label.color = selected ? DarkLabel : new Color(0.91f, 0.93f, 1f);
        }
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

        RuntimeUI.CreateText(menu.settingsPanel.transform, "LanguageLabel",
            L10n.Get("language"), 32,
            new Vector2(0f, -480f), new Vector2(400f, 50f));

        var en = RuntimeUI.CreateButton(menu.settingsPanel.transform,
            "EnglishButton", "English",
            new Vector2(-130f, -560f), new Vector2(220f, 80f), AccentBlue, DarkLabel);
        en.onClick.AddListener(selector.SetEnglish);

        var el = RuntimeUI.CreateButton(menu.settingsPanel.transform,
            "GreekButton", "Ελληνικά",
            new Vector2(130f, -560f), new Vector2(220f, 80f), AccentBlue, DarkLabel);
        el.onClick.AddListener(selector.SetGreek);
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
        L10n.OnLanguageChanged -= RefreshLegacySceneTexts;
        GameEvents.OnMatchEnded -= OnMatchEnded;
    }
}
