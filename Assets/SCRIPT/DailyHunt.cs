using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// The daily ritual: one date-seeded secret number shared by every player,
// seven guesses, one attempt per day. The emoji-trail result copies to the
// clipboard for sharing, one rewarded ad revives a failed hunt with two
// extra guesses, and found days build their own completion streak. Pure
// client — the seed IS the UTC date — built entirely at runtime like the
// PvP panels. State persists after every guess, so an app kill mid-hunt
// resumes exactly where it stopped.
//
// Ported from the parked product-pass draft (#6) onto the Consumer First
// board. Deliberately not ported, so the feature ships without a server or
// package change: the CloudScript percentile ("you beat N% of today's
// hunters" — needs a submitDaily handler deployed first), the local
// notification reminder (needs the mobile-notifications package), and the
// draft's RangeBar visual (the status line's "Between X and Y" carries the
// same information).
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
    const string StreakPrefKey = "DailyHuntStreak";
    const string LastFoundKey = "DailyHuntLastFound";
    // Intent marker for the revive ad, holding the day it was requested
    // for. Pairs with AdsManager.PendingRewardEarnedKey exactly the way the
    // solo streak save's PendingStreakRestoreKey does — see
    // ReconcilePendingRevive for why both halves are needed.
    const string PendingReviveKey = "DailyHuntPendingRevive";

    // Day numbering epoch; #1 is 2026-01-01 UTC. The UTC anchor keeps the
    // day number stable across timezone travel — a local-day anchor replayed
    // or skipped whole days (and their streaks) on a long flight.
    static readonly DateTime Epoch = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    static readonly Color DarkLabel = new Color(0.10f, 0.09f, 0.18f);

    AdsManager ads;

    TMP_Text title;
    TMP_Text status;
    TMP_Text trailText;
    TMP_Text streakText;
    TMP_Text reviveLabel;
    TMP_InputField input;
    Button guessButton;
    Button reviveButton;
    Button shareButton;

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

    // Builds the hidden panel on the canvas and injects the menu entry
    // button right below the PvP entry, mirroring how PvpRuntimeUI places
    // its own button — no scene wiring anywhere.
    public static DailyHunt Attach(Transform canvas, AdsManager adsManager)
    {
        var panel = RuntimeUI.FullscreenPanel(canvas, "DailyHuntPanel",
            ConvergingLight.WithAlpha(ConsumerTokens.Background0, 0.92f));

        var hunt = panel.AddComponent<DailyHunt>();
        hunt.ads = adsManager;
        hunt.Build();

        panel.AddComponent<PanelAnimator>();
        panel.SetActive(false);

        var entry = RuntimeUI.CreateButton(canvas, "DailyHuntButton",
            L10n.Get("daily_hunt"), new Vector2(0f, -740f), new Vector2(460f, 90f),
            ConsumerTokens.Cyan, DarkLabel);
        RuntimeUI.Localize(entry, "daily_hunt");
        entry.onClick.AddListener(hunt.Open);

        // Sit right after the PvP entry so scene panels opened later still
        // render/raycast above the menu buttons.
        var pvpButton = GameObject.Find("ButtonPvP");
        if (pvpButton != null && pvpButton.transform.parent == canvas)
            entry.transform.SetSiblingIndex(pvpButton.transform.GetSiblingIndex() + 1);

        return hunt;
    }

    void Build()
    {
        NeonFrame.Frame(transform, "Card", new Vector2(0f, -10f),
            new Vector2(920f, 1340f), ConsumerTokens.Cyan, 0.985f, true,
            ConsumerTokens.Surface);

        title = RuntimeUI.CreateText(transform, "Title", "", 46,
            new Vector2(0f, 550f), new Vector2(780f, 80f), ConsumerTokens.Gold);

        status = RuntimeUI.CreateText(transform, "Status", "", 32,
            new Vector2(0f, 400f), new Vector2(780f, 150f));

        trailText = RuntimeUI.CreateText(transform, "Trail", "", 44,
            new Vector2(0f, 200f), new Vector2(800f, 80f), ConsumerTokens.Cyan);

        input = RuntimeUI.CreateInputField(transform, "GuessInput",
            L10n.Get("number_placeholder"), new Vector2(0f, 40f), new Vector2(420f, 96f));
        RuntimeUI.LocalizePlaceholder(input, "number_placeholder");
        input.onSubmit.AddListener(_ => SubmitGuess());

        // "Submit" in the name keeps this on the primary design sprite,
        // and gold marks it as the screen's one action that matters.
        guessButton = RuntimeUI.CreateButton(transform, "SubmitGuessButton",
            L10n.Get("pvp_guess"), new Vector2(0f, -110f), new Vector2(460f, 96f),
            ConsumerTokens.Gold, DarkLabel);
        RuntimeUI.Localize(guessButton, "pvp_guess");
        guessButton.onClick.AddListener(SubmitGuess);

        reviveButton = RuntimeUI.CreateButton(transform, "ReviveButton",
            L10n.Get("second_chance", ReviveGuesses), new Vector2(0f, -260f),
            new Vector2(620f, 96f), ConsumerTokens.Gold, DarkLabel);
        reviveLabel = reviveButton.GetComponentInChildren<TMP_Text>();
        reviveButton.onClick.AddListener(OnRevivePressed);

        shareButton = RuntimeUI.CreateButton(transform, "ShareButton",
            L10n.Get("share_result"), new Vector2(0f, -260f), new Vector2(460f, 96f),
            ConsumerTokens.Cyan, DarkLabel);
        RuntimeUI.Localize(shareButton, "share_result");
        shareButton.onClick.AddListener(OnSharePressed);

        streakText = RuntimeUI.CreateText(transform, "Streak", "", 28,
            new Vector2(0f, -410f), new Vector2(600f, 44f), ConsumerTokens.TextSecondary);

        var close = RuntimeUI.CreateButton(transform, "CloseButton",
            L10n.Get("back"), new Vector2(0f, -540f), new Vector2(300f, 84f),
            ConsumerTokens.SurfaceElevated);
        RuntimeUI.Localize(close, "back");
        close.onClick.AddListener(Close);
    }

    public void Open()
    {
        EnsureToday();
        gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        // Backing out of the revive offer settles the day as a loss —
        // leaving it unfinalized dropped the fail from the streak entirely.
        if (!done && used >= budget)
            FinalizeFail();
        gameObject.SetActive(false);
    }

    // Android back / gesture closes the hunt like its own Close button.
    // MenuManager treats Escape on the main menu as a no-op, so the modal
    // owns the press while it is open; Update only runs while active.
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    // ------------------------------------------------------------ state

    static int TodayNumber()
    {
        return (DateTime.UtcNow.Date - Epoch).Days + 1;
    }

    static int SecretFor(int dayNumber)
    {
        // Keyed hash instead of System.Random(day): the seeded-PRNG secret
        // was computable for every future day from the visible day number,
        // and its multiplied seed overflowed int in 2031.
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(
                System.Text.Encoding.UTF8.GetBytes("HOL-DAILY-2:" + dayNumber));
            uint v = (uint)(hash[0] | (hash[1] << 8) | (hash[2] << 16) | (hash[3] << 24));
            return (int)(v % 100u) + 1;
        }
    }

    void EnsureToday()
    {
        day = TodayNumber();

        int storedDay = PlayerPrefs.GetInt(DayKey, 0);
        if (day < storedDay)
        {
            // The device clock rolled backwards past a day already started.
            // Keep serving that furthest day — a reset here let a revealed
            // answer be replayed for a fresh 1-guess "win".
            day = storedDay;
        }
        secret = SecretFor(day);

        if (storedDay < day)
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

        // A missed day ends the streak now, not on the next find — the panel
        // used to advertise a streak the player no longer had, then appear to
        // steal it at the moment they won.
        if (PlayerPrefs.GetInt(LastFoundKey, -1) < day - 1
            && PlayerPrefs.GetInt(StreakPrefKey, 0) != 0)
        {
            PlayerPrefs.SetInt(StreakPrefKey, 0);
            PlayerPrefs.Save();
        }

        ReconcilePendingRevive();
    }

    // A revive earned right as the app died: AdsManager persists its earned
    // marker the moment LevelPlay reports the reward, but the callback that
    // applies it here never ran. Pairing that marker with this feature's own
    // intent marker — the same two-key handshake the solo streak save uses —
    // honors the reward once, on the day it was bought for, and consumes it
    // so it can never validate the other flow's restore instead.
    void ReconcilePendingRevive()
    {
        int pendingDay = PlayerPrefs.GetInt(PendingReviveKey, 0);
        if (pendingDay == 0) return;

        if (PlayerPrefs.GetInt(AdsManager.PendingRewardEarnedKey, 0) == 1)
        {
            if (pendingDay == day && !done && !revived)
            {
                revived = true;
                budget = GuessBudget + ReviveGuesses;
                Persist();
            }
            // Ours either way: a next-day open consumes the moot reward
            // rather than leaving it primed for a false streak restore.
            PlayerPrefs.DeleteKey(AdsManager.PendingRewardEarnedKey);
        }

        PlayerPrefs.DeleteKey(PendingReviveKey);
        PlayerPrefs.Save();
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
        PlayerPrefs.SetInt(StreakPrefKey, 0);
        Persist();
        Haptics.Error();
    }

    void UpdateStreakOnFound()
    {
        int last = PlayerPrefs.GetInt(LastFoundKey, -1);
        int streak = last == day - 1 ? PlayerPrefs.GetInt(StreakPrefKey, 0) + 1 : 1;
        PlayerPrefs.SetInt(StreakPrefKey, streak);
        PlayerPrefs.SetInt(LastFoundKey, day);
    }

    void OnRevivePressed()
    {
        if (ads == null) { FinalizeFail(); Refresh(); return; }

        // Intent first, like the streak save: if the process dies after the
        // reward lands but before the callback below runs, the next open
        // grants the revive from the persisted pair instead of losing it.
        PlayerPrefs.SetInt(PendingReviveKey, day);
        PlayerPrefs.Save();

        ads.ShowRewardedAd(() =>
        {
            revived = true;
            budget = GuessBudget + ReviveGuesses;
            Persist();
            // On success AdsManager leaves the earned marker for the
            // consumer to clear (FinishRewarded only deletes it when no
            // reward was granted). Left behind, it would pair with a later
            // abandoned streak-save attempt and fake a restore.
            PlayerPrefs.DeleteKey(PendingReviveKey);
            PlayerPrefs.DeleteKey(AdsManager.PendingRewardEarnedKey);
            PlayerPrefs.Save();
            Refresh();
        },
        () =>
        {
            PlayerPrefs.DeleteKey(PendingReviveKey);
            PlayerPrefs.Save();
            status.text = L10n.Get("ad_not_ready");
            FinalizeFail();
            Refresh();
        });
    }

    void OnSharePressed()
    {
        string score = (found ? used.ToString() : "✗") + "/" + budget;
        GUIUtility.systemCopyBuffer = L10n.Get("daily_share", day, score, trail);
        status.text = L10n.Get("share_copied");
    }

    // ------------------------------------------------------------ display

    // The stored trail keeps real emoji for the clipboard share, but the
    // bundled TMP font has no emoji glyphs — in-app they rendered as boxes.
    // Display swaps them for triangles/dot the font actually carries.
    static string DisplayTrail(string t)
    {
        return t.Replace("\U0001F53A", "▲")
                .Replace("\U0001F53B", "▼")
                .Replace("\U0001F3AF", "●");
    }

    void Refresh()
    {
        title.text = L10n.Get("daily_hunt_number", day);
        // Formatted label, so RuntimeUI.Localize can't cover it — re-resolve
        // here to follow live language switches like its sibling buttons.
        if (reviveLabel != null)
            reviveLabel.text = L10n.Get("second_chance", ReviveGuesses);
        streakText.text = L10n.Get("daily_streak", PlayerPrefs.GetInt(StreakPrefKey, 0));
        trailText.text = DisplayTrail(trail);

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
            string line = found
                ? L10n.Get("daily_found", used, budget)
                : L10n.Get("daily_failed", secret);
            status.text = line + "\n" + L10n.Get("daily_come_back");
        }
    }
}
