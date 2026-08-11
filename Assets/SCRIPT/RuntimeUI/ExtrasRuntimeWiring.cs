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
public class ExtrasRuntimeWiring : MonoBehaviour
{
    static readonly Color AccentBlue = new Color(0.20f, 0.50f, 0.90f);
    static readonly Color Neutral = new Color(0.25f, 0.25f, 0.30f);

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

        WireRematchButton();
        WireMatchmakingCancel();
        WireLanguageButtons();
        AddStatsLabel();
        AddDisclosureLabels();
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
            new Vector2(-130f, -560f), new Vector2(220f, 80f), AccentBlue);
        en.onClick.AddListener(selector.SetEnglish);

        var el = RuntimeUI.CreateButton(menu.settingsPanel.transform,
            "GreekButton", "Ελληνικά",
            new Vector2(130f, -560f), new Vector2(220f, 80f), AccentBlue);
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
            new Color(1f, 1f, 1f, 0.8f));

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
                new Color(1f, 1f, 1f, 0.6f));
        }

        var mm = FindObjectOfType<FakeMatchmaking>();
        if (mm != null && mm.searchingPanel != null)
        {
            disclosureSearch = RuntimeUI.CreateText(mm.searchingPanel.transform,
                "DisclosureLabel", "", 22,
                new Vector2(0f, -540f), new Vector2(760f, 70f),
                new Color(1f, 1f, 1f, 0.6f));
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
        GameEvents.OnMatchEnded -= OnMatchEnded;
    }
}
