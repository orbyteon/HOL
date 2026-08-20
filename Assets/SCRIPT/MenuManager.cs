using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject panelPlay;
    public GameObject panelSearching;

    public AdsManager adsManager;         // legacy reference; ads now show at match end (GameManager)
    public FakeMatchmaking matchmaking;   // optional: lets BackToMenu cancel a running search

    // Leaving mid-match must be deliberate: one stray back gesture used to
    // reload the scene and forfeit the whole match. The first press now
    // shows a hint; a second within this window exits. Once the match is
    // decided there is nothing left to forfeit, so back exits immediately.
    const float BackConfirmSeconds = 2f;
    float lastMatchBackTime = -10f;
    TMP_Text backHintLabel; // transient, built lazily on the game panel

    GameManager gameManager; // found once; used to detect the decided state

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        // Android back button / gesture. Escape is Unity's mapping for it.
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (settingsPanel != null && settingsPanel.activeSelf)
            BackToMenu();
        else if (panelSearching != null && panelSearching.activeSelf)
            BackToMenu(); // cancels the running search via matchmaking
        else if (matchmaking != null && matchmaking.panelGame != null
            && matchmaking.panelGame.activeSelf)
        {
            // Mid-match exit, same as the old stop button: reload the scene
            // for a clean state. Solo only — live PvP duels keep their
            // explicit Leave button so the room closes cleanly.
            // Checked BEFORE panelPlay: panelPlay stays active for the
            // whole match, so it must not shadow this branch.
            ConfirmMatchExit();
        }
        else if (panelPlay != null && panelPlay.activeSelf)
            BackToMenu();
        // On the main menu, back is a no-op — exit happens via the Quit
        // button so an accidental tap can't kill the app.
    }

    void ConfirmMatchExit()
    {
        // A decided match has nothing to forfeit — exit on the first press.
        bool matchLive = gameManager == null || !gameManager.IsMatchOver;

        if (!matchLive || Time.unscaledTime - lastMatchBackTime <= BackConfirmSeconds)
        {
            SceneManager.LoadScene("MainMenu");
            return;
        }

        lastMatchBackTime = Time.unscaledTime;
        ShowBackHint();
    }

    // Runtime solo-board Back uses exactly the same guarded path as Android's
    // system Back gesture. It must never bypass the live-match confirmation by
    // calling NumberManager.ExitToMenu directly.
    public void RequestSoloMatchExit()
    {
        ConfirmMatchExit();
    }

    void ShowBackHint()
    {
        if (backHintLabel == null)
        {
            backHintLabel = RuntimeUI.CreateText(matchmaking.panelGame.transform,
                "BackExitHint", "", 26, new Vector2(0f, -760f), new Vector2(820f, 60f),
                ConvergingLight.WithAlpha(ConsumerTokens.TextPrimary, 0.85f));
            backHintLabel.raycastTarget = false;
        }

        backHintLabel.text = L10n.Get("back_again_to_leave");
        backHintLabel.gameObject.SetActive(true);

        CancelInvoke(nameof(HideBackHint));
        Invoke(nameof(HideBackHint), BackConfirmSeconds);
    }

    void HideBackHint()
    {
        if (backHintLabel != null)
            backHintLabel.gameObject.SetActive(false);
    }

    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void BackToMenu()
    {
        // Never let a backgrounded search coroutine fire panelGame over the menu.
        if (matchmaking != null)
            matchmaking.CancelSearch();

        // Null-guarded like Update: a partially wired scene must not NRE out
        // of the back-navigation path.
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (panelPlay != null) panelPlay.SetActive(false);
        if (panelSearching != null) panelSearching.SetActive(false);

        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void OnPlayPressed()
    {
        // Ads moved to match end (GameManager.EndGame): gating every Play
        // press with an interstitial hurt retention and monetized the fake
        // "opponent not found" retry loop.
        OpenFindChallengerPanel();
    }

    void OpenFindChallengerPanel()
    {
        mainMenuPanel.SetActive(false);
        panelPlay.SetActive(true);
    }
}
