using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// The daily ritual: one date-seeded secret number shared by every player,
// seven guesses, one attempt per day. The emoji-trail result copies to the
// clipboard for sharing, one rewarded ad revives a failed hunt with two
// extra guesses, and found days build their own completion streak. Pure
// client — the seed IS the local date — built entirely at runtime like the
// PvP panels. State persists after every guess, so an app kill mid-hunt
// resumes exactly where it stopped.
public class DailyHunt : MonoBehaviour
{
    const int GuessBudget = 7;
    const int ReviveGuesses = 2;

    const string DayKey = "DailyHuntDay";
    const string UsedKey = "DailyHuntUsed";
    const string TrailKey = "DailyHuntTrail";
    const string DoneKey = "DailyHuntDone";
    const string FoundKey = "DailyHuntFound";
    const string RevivedKey = "DailyHuntRevived";
    const string MinKey = "DailyHuntMin";
    const string MaxKey = "DailyHuntMax";
    const string StreakKey = "DailyHuntStreak";
    const string LastFoundKey = "DailyHuntLastFound";

    // Day numbering epoch; #1 is 2026-01-01 in the device's local calendar
    // (matching DailyStreak's local-day semantics).
    static readonly DateTime Epoch = new DateTime(2026, 1, 1);

    AdsManager ads;

    TMP_Text title;
    TMP_Text status;
    TMP_Text trailText;
    TMP_Text streakText;
    TMP_InputField input;
    Button guessButton;
    Button reviveButton;
    Button shareButton;
    RangeBar rangeBar;

    int day;
    int secret;
    int used;
    int budget;
    bool done;
    bool found;
    bool revived;
    string trail = "";
    int min = 1;
    int max = 100;

    public static DailyHunt Attach(Transform canvas, Transform menuParent, AdsManager adsManager)
    {
        var panel = RuntimeUI.FullscreenPanel(canvas, "DailyHuntPanel",
            ConvergingLight.WithAlpha(ConvergingLight.ScrimIndigo, 0.9f));

        var hunt = panel.AddComponent<DailyHunt>();
        hunt.ads = adsManager;
        hunt.Build();

        panel.AddComponent<PanelAnimator>();
        panel.SetActive(false);

        var entry = RuntimeUI.CreateButton(menuParent, "DailyHuntButton",
            L10n.Get("daily_hunt"), new Vector2(0f, -740f), new Vector2(460f, 90f),
            ConvergingLight.Cyan, ConvergingLightFX.DarkLabel);
        RuntimeUI.Localize(entry, "daily_hunt");
        entry.onClick.AddListener(hunt.Open);

        return hunt;
    }

    void Build()
    {
        var card = RuntimeUI.CreateObject("Card", transform);
        ConvergingLight.Center(card, new Vector2(0f, -10f), new Vector2(920f, 1340f));
        var surface = card.AddComponent<Image>();
        surface.raycastTarget = true;
        ConvergingLightFX.StyleCard(surface, 0.985f);

        title = RuntimeUI.CreateTmpText(card.transform, "Title", "", 46,
            new Vector2(0f, 560f), new Vector2(780f, 80f));
        ConvergingLightFX.StyleHeading(title, 4f);

        ConvergingLightFX.AddSeamRule(card.transform, new Vector2(0f, 500f), 420f);

        status = RuntimeUI.CreateTmpText(card.transform, "Status", "", 32,
            new Vector2(0f, 408f), new Vector2(780f, 110f));

        rangeBar = RangeBar.Attach(card.transform, null, new Vector2(0f, 280f));

        trailText = RuntimeUI.CreateTmpText(card.transform, "Trail", "", 44,
            new Vector2(0f, 160f), new Vector2(800f, 80f));

        input = RuntimeUI.CreateInputField(card.transform, "GuessInput",
            L10n.Get("number_placeholder"), new Vector2(0f, 20f), new Vector2(420f, 96f));
        RuntimeUI.LocalizePlaceholder(input, "number_placeholder");
        input.onSubmit.AddListener(_ => SubmitGuess());

        guessButton = RuntimeUI.CreateButton(card.transform, "GuessButton",
            L10n.Get("pvp_guess"), new Vector2(0f, -120f), new Vector2(460f, 96f),
            ConvergingLight.Gold, ConvergingLightFX.DarkLabel);
        RuntimeUI.Localize(guessButton, "pvp_guess");
        guessButton.onClick.AddListener(SubmitGuess);

        reviveButton = RuntimeUI.CreateButton(card.transform, "ReviveButton",
            L10n.Get("second_chance", ReviveGuesses), new Vector2(0f, -280f),
            new Vector2(620f, 96f), ConvergingLight.Gold, ConvergingLightFX.DarkLabel);
        reviveButton.onClick.AddListener(OnRevivePressed);

        shareButton = RuntimeUI.CreateButton(card.transform, "ShareButton",
            L10n.Get("share_result"), new Vector2(0f, -280f), new Vector2(460f, 96f),
            ConvergingLight.Cyan, ConvergingLightFX.DarkLabel);
        RuntimeUI.Localize(shareButton, "share_result");
        shareButton.onClick.AddListener(OnSharePressed);

        streakText = RuntimeUI.CreateTmpText(card.transform, "Streak", "", 28,
            new Vector2(0f, -420f), new Vector2(600f, 44f),
            ConvergingLight.WithAlpha(ConvergingLight.NearWhite, 0.7f));

        var close = RuntimeUI.CreateButton(card.transform, "CloseButton",
            L10n.Get("back"), new Vector2(0f, -540f), new Vector2(300f, 84f),
            ConvergingLightFX.GhostSurface);
        RuntimeUI.Localize(close, "back");
        close.onClick.AddListener(() => gameObject.SetActive(false));
    }

    public void Open()
    {
        EnsureToday();
        gameObject.SetActive(true);
        Refresh();
    }

    // ------------------------------------------------------------ state

    static int TodayNumber()
    {
        return (DateTime.Now.Date - Epoch).Days + 1;
    }

    static int SecretFor(int dayNumber)
    {
        var rng = new System.Random(dayNumber * 1000003);
        rng.Next(); // decorrelate small consecutive seeds
        return rng.Next(1, 101);
    }

    void EnsureToday()
    {
        day = TodayNumber();
        secret = SecretFor(day);

        if (PlayerPrefs.GetInt(DayKey, 0) != day)
        {
            PlayerPrefs.SetInt(DayKey, day);
            PlayerPrefs.SetInt(UsedKey, 0);
            PlayerPrefs.SetString(TrailKey, "");
            PlayerPrefs.SetInt(DoneKey, 0);
            PlayerPrefs.SetInt(FoundKey, 0);
            PlayerPrefs.SetInt(RevivedKey, 0);
            PlayerPrefs.SetInt(MinKey, 1);
            PlayerPrefs.SetInt(MaxKey, 100);
            PlayerPrefs.Save();
        }

        used = PlayerPrefs.GetInt(UsedKey, 0);
        trail = PlayerPrefs.GetString(TrailKey, "");
        done = PlayerPrefs.GetInt(DoneKey, 0) == 1;
        found = PlayerPrefs.GetInt(FoundKey, 0) == 1;
        revived = PlayerPrefs.GetInt(RevivedKey, 0) == 1;
        min = PlayerPrefs.GetInt(MinKey, 1);
        max = PlayerPrefs.GetInt(MaxKey, 100);
        budget = GuessBudget + (revived ? ReviveGuesses : 0);
    }

    void Persist()
    {
        PlayerPrefs.SetInt(UsedKey, used);
        PlayerPrefs.SetString(TrailKey, trail);
        PlayerPrefs.SetInt(DoneKey, done ? 1 : 0);
        PlayerPrefs.SetInt(FoundKey, found ? 1 : 0);
        PlayerPrefs.SetInt(RevivedKey, revived ? 1 : 0);
        PlayerPrefs.SetInt(MinKey, min);
        PlayerPrefs.SetInt(MaxKey, max);
        PlayerPrefs.Save();
    }

    // ------------------------------------------------------------ play

    void SubmitGuess()
    {
        if (done || used >= budget) return;

        int guess;
        if (!int.TryParse(input.text, out guess) || guess < 1 || guess > 100)
        {
            status.text = L10n.Get("invalid_number");
            return;
        }
        input.text = "";

        used++;

        if (guess == secret)
        {
            trail += "\U0001F3AF"; // 🎯
            done = true;
            found = true;
            UpdateStreakOnFound();
            Persist();
            Haptics.Success();
            Analytics.DailyEnded(day, true, used, revived);
        }
        else
        {
            bool needHigher = guess < secret;
            trail += needHigher ? "\U0001F53A" : "\U0001F53B"; // 🔺 / 🔻
            if (needHigher && guess + 1 > min) min = guess + 1;
            if (!needHigher && guess - 1 < max) max = guess - 1;

            if (used >= budget && !CanOfferRevive())
                FinalizeFail();
            else
                Persist();
        }

        Refresh();
    }

    bool CanOfferRevive()
    {
        return !revived && ads != null && ads.IsRewardedReady();
    }

    void FinalizeFail()
    {
        done = true;
        found = false;
        PlayerPrefs.SetInt(StreakKey, 0);
        Persist();
        Haptics.Error();
        Analytics.DailyEnded(day, false, used, revived);
    }

    void UpdateStreakOnFound()
    {
        int last = PlayerPrefs.GetInt(LastFoundKey, -1);
        int streak = last == day - 1 ? PlayerPrefs.GetInt(StreakKey, 0) + 1 : 1;
        PlayerPrefs.SetInt(StreakKey, streak);
        PlayerPrefs.SetInt(LastFoundKey, day);
    }

    void OnRevivePressed()
    {
        if (ads == null) { FinalizeFail(); Refresh(); return; }

        bool shown = ads.ShowRewardedAd(() =>
        {
            revived = true;
            budget = GuessBudget + ReviveGuesses;
            Persist();
            Refresh();
        },
        () =>
        {
            status.text = L10n.Get("ad_not_ready");
            FinalizeFail();
            Refresh();
        });

        if (!shown) return; // onUnavailable already handled it
    }

    void OnSharePressed()
    {
        string score = (found ? used.ToString() : "✗") + "/" + budget;
        GUIUtility.systemCopyBuffer = L10n.Get("daily_share", day, score, trail);
        status.text = L10n.Get("share_copied");
    }

    // ------------------------------------------------------------ display

    void Refresh()
    {
        title.text = L10n.Get("daily_hunt_number", day);
        streakText.text = L10n.Get("daily_streak", PlayerPrefs.GetInt(StreakKey, 0));
        trailText.text = trail;
        rangeBar.SetRange(min, max, true);

        bool awaitingRevive = !done && used >= budget;
        bool playing = !done && !awaitingRevive;

        input.gameObject.SetActive(playing);
        guessButton.gameObject.SetActive(playing);
        reviveButton.gameObject.SetActive(awaitingRevive && CanOfferRevive());
        shareButton.gameObject.SetActive(done);

        if (awaitingRevive && !CanOfferRevive())
        {
            // The ad disappeared between the offer and this open — settle it.
            FinalizeFail();
            Refresh();
            return;
        }

        if (playing)
        {
            status.text = used == 0
                ? L10n.Get("daily_intro", GuessBudget)
                : L10n.Get("guesses_left", budget - used) + "\n" +
                  L10n.Get("between_range", min, max);
        }
        else if (awaitingRevive)
        {
            status.text = L10n.Get("guesses_left", 0);
        }
        else
        {
            status.text = (found
                ? L10n.Get("daily_found", used, budget)
                : L10n.Get("daily_failed", secret))
                + "\n" + L10n.Get("daily_come_back");
        }
    }
}
