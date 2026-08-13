using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    // Disclosure hook: show this line in-game / store listing.
    public const string SimulatedOpponentDisclosure = "Opponents are simulated by an on-device AI.";

    // AI difficulty (PlayerPrefs "AIDifficulty"): 0 = Easy, 1 = Normal, 2 = Hard,
    // 3 = Adaptive (tunes itself from the player's recent win rate, see
    // AdaptiveRandomChance). The value is the chance the AI guesses randomly
    // instead of taking the binary-search midpoint — higher means a weaker,
    // more human opponent.
    const string DifficultyPrefKey = "AIDifficulty";
    static readonly float[] DifficultyRandomChance = { 0.6f, 0.2f, 0f, -1f };

    // Solo mode (PlayerPrefs "GameMode"): 0 = Classic, 1 = Sudden Death
    // (find the number within the guess limit or lose), 2 = Time Attack
    // (a shared clock runs down only on your turn).
    public const string ModePrefKey = "GameMode";
    const int SuddenDeathGuessLimit = 7;
    const float TimeAttackSeconds = 45f;

    // Rival personas flavor HOW the AI errs; difficulty still decides how
    // often it errs. Cautious reads deliberately weaker, Calculator plays
    // the textbook interval — variety over strict symmetry.
    enum Persona { Calculator, Cautious, Chaotic, Intuitive }
    static readonly string[] PersonaKeys =
        { "persona_calculator", "persona_cautious", "persona_chaotic", "persona_intuitive" };

    public TMP_Text aiNumberText;
    public TMP_Text aiAnswerText;
    public TMP_Text turnText;
    public TMP_Text opponentNameText;

    // Optional UI (wire in Inspector): shows the player's narrowed guess
    // range and running guess histories for both sides.
    public TMP_Text rangeText;
    public TMP_Text playerHistoryText;
    public TMP_Text aiHistoryText;

    // Optional (runtime-wired): Sudden Death guesses left / Time Attack clock.
    public TMP_Text modeStatusText;

    public GameObject higherButton;
    public GameObject lowerButton;
    public GameObject correctButton;
    public GameObject stopGameButton;

    public AudioSource audioSource;
    public AudioClip winSound;
    public AudioClip loseSound;

    public NumberManager numberManager;
    public ConfettiBurst winConfetti;
    public AdsManager adsManager;

    public bool IsPlayerTurn => playerTurn && !gameFinished;
    public bool IsMatchOver => gameFinished;

    // The player's narrowed hunting interval, for UI that visualizes the
    // converging range (RangeBar). Fired from UpdateRangeText.
    public System.Action<int, int> OnPlayerRangeChanged;
    public int PlayerRangeMin => playerMin;
    public int PlayerRangeMax => playerMax;

    int min = 1;
    int max = 100;
    int aiGuess;

    int playerMin = 1;
    int playerMax = 100;

    bool playerTurn = false;
    bool gameFinished = false;
    bool firstAIGuess = true;
    bool matchStarted = false;

    int playerSecretNumber;
    int aiSecretNumber;
    int playerGuessCount;
    int aiGuessCount;

    Persona persona;
    int mode;
    float timeLeft;

    readonly System.Text.StringBuilder playerHistory = new System.Text.StringBuilder();
    readonly System.Text.StringBuilder aiHistory = new System.Text.StringBuilder();

    string currentOpponent;

    string[] fakeNames =
    {
        "Pierre", "Lucas", "Mathieu", "Marco", "Giovanni",
        "Luca", "Carlos", "Miguel", "Alejandro", "Javier",
        "Hans", "Lukas", "Erik", "Oliver", "Mateo",
        "Ivan", "Stefan", "Adrian", "Marek", "Bjorn",
        "Sven", "Kostas", "Andreas", "Nikos", "Konstantinos"
    };

    const string PendingForfeitKey = "PendingMatchForfeit";

    void Start()
    {
        ReconcilePendingStreakRestore();

        // A match that was live when the app went to background and never
        // came back was abandoned — settle it as the loss it is.
        if (PlayerPrefs.GetInt(PendingForfeitKey, 0) == 1)
        {
            PlayerPrefs.DeleteKey(PendingForfeitKey);
            GameStats.RecordLoss();
            PlayerPrefs.Save();
        }

        stopGameButton.SetActive(false);
        HideButtons();

        PickOpponent();

        if (turnText != null)
            turnText.text = L10n.Get("enter_your_number");
    }

    // A rival is a name plus a persona; both show in the header so the
    // variety is legible ("Nikos · the Chaotic"), not just felt.
    void PickOpponent()
    {
        currentOpponent = fakeNames[Random.Range(0, fakeNames.Length)];

        // The roll respects the difficulty promise: Hard never serves the
        // deliberately weak Cautious, Easy never serves the near-optimal
        // Calculator — otherwise an invisible persona roll could swing the
        // chosen difficulty by more than the setting itself.
        int difficulty = Mathf.Clamp(PlayerPrefs.GetInt(DifficultyPrefKey, 1), 0, 3);
        do
        {
            persona = (Persona)Random.Range(0, PersonaKeys.Length);
        }
        while ((difficulty == 2 && persona == Persona.Cautious)
            || (difficulty == 0 && persona == Persona.Calculator));

        RefreshOpponentLabel();
    }

    // Composed at runtime, so RuntimeUI.Localize can't own it — refreshed on
    // language change or the header keeps the old language until a rematch.
    void RefreshOpponentLabel()
    {
        if (opponentNameText != null && !string.IsNullOrEmpty(currentOpponent))
            opponentNameText.text = L10n.Get("opponent_label",
                currentOpponent + " · " + L10n.Get(PersonaKeys[(int)persona]));
    }

    void OnEnable()
    {
        L10n.OnLanguageChanged += RefreshOpponentLabel;
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= RefreshOpponentLabel;
    }

    bool pausedThisSession;

    void OnApplicationPause(bool paused)
    {
        // Ads and returns clear the marker; only a backgrounded live match
        // that never resumes converts to a loss on the next boot. The
        // pausedThisSession guard keeps Android's launch-time unpause call
        // from wiping a marker Start() hasn't reconciled yet.
        if (paused && matchStarted && !gameFinished)
        {
            pausedThisSession = true;
            PlayerPrefs.SetInt(PendingForfeitKey, 1);
            PlayerPrefs.Save();
        }
        else if (!paused && pausedThisSession)
        {
            PlayerPrefs.DeleteKey(PendingForfeitKey);
        }
    }

    public void SetPlayerNumber(int number)
    {
        playerSecretNumber = number;
    }

    public void StartGame()
    {
        CancelInvoke();

        min = 1;
        max = 100;
        playerMin = 1;
        playerMax = 100;

        gameFinished = false;
        playerTurn = false;
        firstAIGuess = true;
        playerGuessCount = 0;
        aiGuessCount = 0;

        mode = Mathf.Clamp(PlayerPrefs.GetInt(ModePrefKey, 0), 0, 2);
        timeLeft = TimeAttackSeconds;
        UpdateModeStatus();

        aiSecretNumber = Random.Range(1, 101);

        aiNumberText.text = "?";
        aiAnswerText.text = "";

        ResetHistory();
        UpdateRangeText();

        stopGameButton.SetActive(false);
        HideButtons();

        // The player always moves first. The duel is a symmetric race, so
        // whichever side moves first wins every evenly-played match — a
        // coin flip here made half of all flawless games unwinnable for a
        // reason invisible to the player. The house rival can afford it.
        turnText.text = L10n.Get("your_guess");
        playerTurn = true;
        matchStarted = true;
    }

    void HideButtons()
    {
        higherButton.SetActive(false);
        lowerButton.SetActive(false);
        correctButton.SetActive(false);
    }

    void AIGuess()
    {
        if (gameFinished) return;

        if (firstAIGuess)
        {
            // The Calculator opens at the textbook midpoint; everyone else
            // opens on instinct.
            aiGuess = persona == Persona.Calculator
                ? (min + max) / 2
                : Random.Range(min, max + 1);
            firstAIGuess = false;
        }
        else
        {
            int difficulty = Mathf.Clamp(PlayerPrefs.GetInt(DifficultyPrefKey, 1), 0, 3);
            float randomChance = difficulty == 3
                ? AdaptiveRandomChance()
                : DifficultyRandomChance[difficulty];

            aiGuess = Random.value < randomChance
                ? PersonaWildGuess()
                : PersonaFocusedGuess();
        }

        aiGuess = Mathf.Clamp(aiGuess, min, max);
        aiGuessCount++;

        aiNumberText.text = currentOpponent + ": " + aiGuess;
        AppendHistory(aiHistory, aiHistoryText, aiGuess);

        playerTurn = false;
        turnText.text = L10n.Get("answer_opponent", currentOpponent);
        HideButtons();

        if (playerSecretNumber > aiGuess)
            higherButton.SetActive(true);
        else if (playerSecretNumber < aiGuess)
            lowerButton.SetActive(true);
        else
            correctButton.SetActive(true);
    }

    public void Higher()
    {
        min = aiGuess + 1;
        AfterWrongAIGuess();
    }

    public void Lower()
    {
        max = aiGuess - 1;
        AfterWrongAIGuess();
    }

    void AfterWrongAIGuess()
    {
        HideButtons();

        // Sudden Death cuts both ways: the rival is on the same guess
        // budget, so an imperfect rival run is winnable pressure, not a
        // one-sided countdown on the player alone.
        if (mode == 1 && aiGuessCount >= SuddenDeathGuessLimit)
        {
            aiAnswerText.text = L10n.Get("opponent_out_of_guesses", currentOpponent);
            EndGame(true);
            return;
        }

        turnText.text = L10n.Get("your_guess");
        playerTurn = true;
        FocusGuessInput();
    }

    // Answering the opponent hands the turn back to the player — put the
    // caret straight into the guess field so typing can start immediately.
    void FocusGuessInput()
    {
        if (numberManager != null)
            numberManager.FocusInput();
    }

    // Focused guess: the persona's version of "playing well". Difficulty
    // decides how OFTEN the AI errs; the persona decides what its play and
    // its errors look like.
    int PersonaFocusedGuess()
    {
        int mid = (min + max) / 2;
        int range = max - min;
        switch (persona)
        {
            case Persona.Cautious:   return min + Mathf.Max(1, range / 4);
            case Persona.Chaotic:    return mid + Random.Range(-range / 5, range / 5 + 1);
            case Persona.Intuitive:  return mid + Random.Range(-2, 3);
            default:                 return mid; // Calculator
        }
    }

    // Wild guess: the persona's version of a lapse. Every lapse must cost
    // real information — a ±2 slip on a wide interval is still near-perfect
    // play, which made "Easy" Calculators as lethal as Hard ones.
    int PersonaWildGuess()
    {
        int mid = (min + max) / 2;
        int range = max - min;
        switch (persona)
        {
            case Persona.Calculator: return mid + Random.Range(-range / 4, range / 4 + 1);
            case Persona.Cautious:   return Random.value < 0.5f ? min : min + Mathf.Max(1, range / 3);
            case Persona.Intuitive:  return mid + Random.Range(-range / 4, range / 4 + 1);
            default:                 return Random.Range(min, max + 1); // Chaotic
        }
    }

    static float AdaptiveRandomChance()
    {
        // A real sample is required before the tuner reacts — two lucky
        // opening wins used to summon a near-perfect opponent by match 3.
        if (GameStats.RecentSamples < 5) return DifficultyRandomChance[1];

        float winRate = GameStats.RecentWinRate();
        if (winRate < 0f) return DifficultyRandomChance[1];

        // Continuous ramp between the forgiving and strong ends; the old
        // three-step table made the difficulty snap visibly between matches.
        return Mathf.Lerp(0.5f, 0.1f, Mathf.InverseLerp(0.3f, 0.7f, winRate));
    }

    public void Correct()
    {
        aiAnswerText.text = L10n.Get("opponent_found_number", currentOpponent);
        EndGame(false);
    }

    public bool PlayerGuess(int guess)
    {
        if (!playerTurn || gameFinished) return false;

        if (guess < playerMin || guess > playerMax)
        {
            aiAnswerText.text = L10n.Get("already_know_range", playerMin, playerMax);
            return false;
        }

        playerGuessCount++;
        UpdateModeStatus();

        string playerLabel = PlayerPrefs.GetString("PlayerName", "");
        if (string.IsNullOrEmpty(playerLabel))
            playerLabel = L10n.Get("you");

        aiAnswerText.text = playerLabel + ": " + guess;
        AppendHistory(playerHistory, playerHistoryText, guess);

        if (guess == aiSecretNumber)
        {
            aiAnswerText.text = playerLabel + ": " + guess + "\n" + L10n.Get("you_win");
            EndGame(true);
            return true;
        }

        if (guess < aiSecretNumber)
        {
            aiAnswerText.text = playerLabel + ": " + guess + "\n" + currentOpponent + ": " + L10n.Get("higher");
            if (guess + 1 > playerMin) playerMin = guess + 1;
        }
        else
        {
            aiAnswerText.text = playerLabel + ": " + guess + "\n" + currentOpponent + ": " + L10n.Get("lower");
            if (guess - 1 < playerMax) playerMax = guess - 1;
        }

        UpdateRangeText();

        // Sudden Death: the hunt ends when the guess budget does.
        if (mode == 1 && playerGuessCount >= SuddenDeathGuessLimit)
        {
            EndGame(false);
            return true;
        }

        playerTurn = false;

        turnText.text = L10n.Get("opponent_thinking", currentOpponent);
        Invoke(nameof(AIGuess), Random.Range(1.5f, 3.5f));
        return true;
    }

    void Update()
    {
        // Time Attack: one shared clock for the whole duel. Ticking only on
        // the player's turn meant a prompt typist could never lose to it —
        // the race premise needs the clock alive while the rival thinks too.
        if (mode != 2 || gameFinished || !matchStarted) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            UpdateModeStatus();
            EndGame(false);
            return;
        }
        UpdateModeStatus();
    }

    void UpdateModeStatus()
    {
        if (modeStatusText == null) return;

        if (mode == 1)
            modeStatusText.text = L10n.Get("guesses_left",
                Mathf.Max(0, SuddenDeathGuessLimit - playerGuessCount));
        else if (mode == 2)
            modeStatusText.text = L10n.Get("time_left", Mathf.CeilToInt(timeLeft));
        else
            modeStatusText.text = "";
    }

    void EndGame(bool playerWon)
    {
        gameFinished = true;

        HideButtons();
        stopGameButton.SetActive(true);

        if (numberManager != null)
            numberManager.CloseInput();

        if (playerWon)
        {
            GameStats.RecordWin(playerGuessCount);
            Haptics.Success();
            GameEvents.MatchEnded(true, playerGuessCount);

            turnText.text = L10n.Get("you_win") + "\n" + L10n.Get("won_in_guesses", playerGuessCount);
            // ≤7 is just competent binary search (and every Sudden Death win),
            // which made the game's only mastery callout carry no information.
            if (mode != 1 && playerGuessCount <= 5)
                turnText.text += "\n" + L10n.Get("perfect_game");
            if (audioSource != null && winSound != null)
                audioSource.PlayOneShot(winSound);
            if (winConfetti != null)
                winConfetti.Burst();
        }
        else
        {
            int streakBeforeLoss = GameStats.CurrentStreak;
            GameStats.RecordLoss();
            Haptics.Error();
            GameEvents.MatchEnded(false, 0);

            turnText.text = L10n.Get("you_lose") + "\n" + L10n.Get("number_was", aiSecretNumber);
            if (audioSource != null && loseSound != null)
                audioSource.PlayOneShot(loseSound);

            OfferStreakSave(streakBeforeLoss);
        }

        // Deferred to the rematch tap: firing here buried the result reveal,
        // the win celebration, and the streak-save offer under a full-screen
        // ad in the same frame.
        interstitialPending = adsManager != null && GameStats.Matches % 2 == 0
            && streakSaveButton == null;
    }

    bool interstitialPending;

    // Abandoning a live match counts as the loss it is. Without this,
    // backing out at the moment of certain defeat kept streaks and win rate
    // clean, skipped the interstitial cadence, and fed the adaptive AI a
    // fabricated win rate — which also made the rewarded streak-save
    // pointless. Quiet by design: no result screen, no ad, just the record.
    public void ForfeitIfLive()
    {
        if (!matchStarted || gameFinished) return;

        gameFinished = true;
        PlayerPrefs.DeleteKey(PendingForfeitKey); // settled here, not on boot
        GameStats.RecordLoss();
        GameEvents.MatchEnded(false, 0);
    }

    const int MinStreakToSave = 2;
    GameObject streakSaveButton;

    void ReconcilePendingStreakRestore()
    {
        int pending = PlayerPrefs.GetInt(AdsManager.PendingStreakRestoreKey, 0);
        if (pending <= 0) return;

        if (PlayerPrefs.GetInt(AdsManager.PendingRewardEarnedKey, 0) == 1)
        {
            GameStats.RestoreStreak(pending);
            GameEvents.StatsChanged();
        }

        PlayerPrefs.DeleteKey(AdsManager.PendingStreakRestoreKey);
        PlayerPrefs.DeleteKey(AdsManager.PendingRewardEarnedKey);
        PlayerPrefs.Save();
    }

    void OfferStreakSave(int streak)
    {
        if (streak < MinStreakToSave || adsManager == null || !adsManager.IsRewardedReady())
            return;
        if (stopGameButton == null) return;

        var btn = RuntimeUI.CreateButton(stopGameButton.transform.parent, "SaveStreakButton",
            L10n.Get("save_streak_ad", streak), Vector2.zero, new Vector2(560f, 90f),
            ConvergingLight.Gold, ConvergingLight.WithAlpha(ConvergingLight.PanelIndigo, 1f));

        var stopRect = (RectTransform)stopGameButton.transform;
        var rect = (RectTransform)btn.transform;
        rect.anchorMin = stopRect.anchorMin;
        rect.anchorMax = stopRect.anchorMax;
        rect.pivot = stopRect.pivot;
        rect.anchoredPosition = stopRect.anchoredPosition + new Vector2(0f, 120f);

        streakSaveButton = btn.gameObject;
        btn.onClick.AddListener(() =>
        {
            bool shown = adsManager.ShowRewardedAd(() =>
            {
                GameStats.RestoreStreak(streak);
                PlayerPrefs.DeleteKey(AdsManager.PendingStreakRestoreKey);
                PlayerPrefs.DeleteKey(AdsManager.PendingRewardEarnedKey);
                PlayerPrefs.Save();
                GameEvents.StatsChanged();
                if (streakSaveButton != null)
                {
                    Destroy(streakSaveButton);
                    streakSaveButton = null;
                }
            },
            () =>
            {
                PlayerPrefs.DeleteKey(AdsManager.PendingStreakRestoreKey);
                PlayerPrefs.Save();
                if (aiAnswerText != null)
                    aiAnswerText.text = L10n.Get("ad_not_ready");
            });

            if (shown)
            {
                PlayerPrefs.SetInt(AdsManager.PendingStreakRestoreKey, streak);
                PlayerPrefs.Save();
            }
        });
    }

    public void RestartMatch()
    {
        CancelInvoke();

        if (interstitialPending)
        {
            interstitialPending = false;
            if (adsManager != null)
                adsManager.ShowAd(null);
        }

        if (streakSaveButton != null)
        {
            Destroy(streakSaveButton);
            streakSaveButton = null;
        }

        gameFinished = false;
        playerTurn = false;
        matchStarted = false;

        // The number-entry screen must not carry last match's state: the
        // range bar froze on the old interval and Time Attack's label kept
        // the expired clock.
        playerMin = 1;
        playerMax = 100;
        UpdateRangeText();
        timeLeft = TimeAttackSeconds;
        UpdateModeStatus();

        PickOpponent();

        aiNumberText.text = "?";
        aiAnswerText.text = "";
        turnText.text = L10n.Get("enter_your_number");

        ResetHistory();

        stopGameButton.SetActive(false);
        HideButtons();

        if (numberManager != null)
            numberManager.ResetForNewMatch();
    }

    void ResetHistory()
    {
        playerHistory.Length = 0;
        aiHistory.Length = 0;

        if (playerHistoryText != null)
            playerHistoryText.text = "";
        if (aiHistoryText != null)
            aiHistoryText.text = "";
    }

    static void AppendHistory(System.Text.StringBuilder history, TMP_Text target, int guess)
    {
        if (history.Length > 0)
            history.Append("  ");
        history.Append(guess);

        if (target != null)
            target.text = history.ToString();
    }

    void UpdateRangeText()
    {
        if (rangeText != null)
            rangeText.text = L10n.Get("between_range", playerMin, playerMax);

        OnPlayerRangeChanged?.Invoke(playerMin, playerMax);
    }
}
