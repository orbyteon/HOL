using System.Collections.Generic;
using UnityEngine;

// Lightweight localization (English + native Greek). Static so any script
// or UI component can use it without scene wiring.
//
// Usage:
//   string s = L10n.Get("play");                     // current language
//   string s = L10n.Get("opponent_thinking", name);  // formatted entry
//   L10n.SetLanguage(L10n.Language.Greek);           // persisted via PlayerPrefs
//
// UI wiring: put a LocalizedText on any TMP_Text and set its key.
public static class L10n
{
    public enum Language { English = 0, Greek = 1 }

    const string PrefKey = "Language";

    public static System.Action OnLanguageChanged;

    public static Language Current
    {
        get
        {
            // First launch (no explicit choice yet): follow the device
            // language, so Greek phones start in Greek.
            if (!PlayerPrefs.HasKey(PrefKey))
                return Application.systemLanguage == SystemLanguage.Greek
                    ? Language.Greek
                    : Language.English;
            return (Language)PlayerPrefs.GetInt(PrefKey, (int)Language.English);
        }
    }

    public static void SetLanguage(Language lang)
    {
        PlayerPrefs.SetInt(PrefKey, (int)lang);
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke();
    }

    public static string Get(string key, params object[] args)
    {
        string[] pair;
        if (!Table.TryGetValue(key, out pair))
        {
            Debug.LogWarning("L10n: missing key '" + key + "'");
            return key;
        }

        string s = pair[(int)Current];

        if (args != null && args.Length > 0)
            s = string.Format(s, args);

        return s;
    }

    // { English, Greek }
    static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
    {
        // main menu
        { "play",                    new[] { "Play", "Παίξε" } },
        { "back",                    new[] { "Back", "Πίσω" } },
        { "quit",                    new[] { "Quit", "Έξοδος" } },
        { "save",                    new[] { "Save", "Αποθήκευση" } },
        { "language",                new[] { "Language", "Γλώσσα" } },
        { "music",                   new[] { "Music", "Μουσική" } },
        { "player_name",             new[] { "Your name", "Το όνομά σου" } },

        // matchmaking
        { "find_challenger",         new[] { "Find challenger", "Βρες αντίπαλο" } },
        { "searching",               new[] { "Searching opponent", "Αναζήτηση αντιπάλου" } },
        { "opponent_found",          new[] { "Opponent found!", "Βρέθηκε αντίπαλος!" } },
        { "opponent_not_found",      new[] { "Opponent not found. Try again.", "Δεν βρέθηκε αντίπαλος. Δοκίμασε ξανά." } },
        { "cancel",                  new[] { "Cancel", "Ακύρωση" } },

        // game setup
        { "enter_your_number",       new[] { "Enter your number", "Βάλε τον αριθμό σου" } },
        { "number_placeholder",      new[] { "1-100", "1-100" } },
        { "confirm",                 new[] { "Confirm", "Επιβεβαίωση" } },
        { "invalid_number",          new[] { "Enter a valid number", "Βάλε έγκυρο αριθμό" } },
        { "number_out_of_range",     new[] { "Number must be between 1 and 100", "Ο αριθμός πρέπει να είναι από 1 έως 100" } },

        // duel
        { "opponent_label",          new[] { "Opponent: {0}", "Αντίπαλος: {0}" } },
        { "your_guess",              new[] { "Your guess", "Η μαντεψιά σου" } },
        { "opponent_thinking",       new[] { "{0} thinking...", "{0} σκέφτεται..." } },
        { "answer_opponent",         new[] { "Answer {0}", "Απάντησε στον/στην {0}" } },
        { "wait_your_turn",          new[] { "Wait for your turn...", "Περίμενε τη σειρά σου..." } },
        { "higher",                  new[] { "Higher", "Ψηλότερα" } },
        { "lower",                   new[] { "Lower", "Χαμηλότερα" } },
        { "correct",                 new[] { "Correct", "Σωστά" } },
        { "stop_game",               new[] { "Stop game", "Τέλος παιχνιδιού" } },
        { "back_again_to_leave",     new[] { "Press back again to leave the match", "Πάτησε ξανά πίσω για έξοδο από τον αγώνα" } },
        { "between_range",           new[] { "Between {0} and {1}", "Ανάμεσα σε {0} και {1}" } },
        { "already_know_range",      new[] { "You already know it's between {0} and {1}!", "Ξέρεις ήδη ότι είναι ανάμεσα σε {0} και {1}!" } },
        { "opponent_found_number",   new[] { "{0} found your number!", "{0} βρήκε τον αριθμό σου!" } },

        // result
        { "you_win",                 new[] { "YOU WIN!", "ΚΕΡΔΙΣΕΣ!" } },
        { "you_lose",                new[] { "YOU LOSE!", "ΕΧΑΣΕΣ!" } },
        { "won_in_guesses",          new[] { "In {0} guesses", "Σε {0} προσπάθειες" } },
        { "number_was",              new[] { "The number was {0}", "Ο αριθμός ήταν {0}" } },
        { "rematch",                 new[] { "Rematch", "Ρεβάνς" } },

        // stats
        { "stats_wins",              new[] { "Wins", "Νίκες" } },
        { "stats_losses",            new[] { "Losses", "Ήττες" } },
        { "stats_streak",            new[] { "Streak", "Σερί" } },
        { "stats_best",              new[] { "Best", "Ρεκόρ" } },
        { "stats_fastest_win",       new[] { "Fastest win: {0} guesses", "Ταχύτερη νίκη: {0} προσπάθειες" } },
        { "guesses",                 new[] { "Guesses:", "Προσπάθειες:" } },
        { "your_number",             new[] { "Your number? (1-100)", "Ο αριθμός σου; (1-100)" } },

        // pvp
        { "pvp_create_room",         new[] { "Create room", "Δημιουργία δωματίου" } },
        { "pvp_join_room",           new[] { "Join room", "Μπες σε δωμάτιο" } },
        { "pvp_waiting",             new[] { "Waiting for your challenger...", "Αναμονή για τον αντίπαλό σου..." } },
        { "pvp_invite_copied",       new[] { "Invite copied! Send it to a friend.", "Η πρόσκληση αντιγράφηκε! Στείλ'τη σε φίλο σου." } },
        { "pvp_room_not_found",      new[] { "Room not found. Check the code.", "Το δωμάτιο δεν βρέθηκε. Έλεγξε τον κωδικό." } },
        { "pvp_room_full",           new[] { "Room is already full.", "Το δωμάτιο είναι γεμάτο." } },
        { "pvp_network_error",       new[] { "Network hiccup — try again", "Πρόβλημα δικτύου — δοκίμασε ξανά" } },
        { "pvp_duel",                new[] { "PvP Duel", "Μονομαχία PvP" } },
        { "pvp_secret",              new[] { "Your secret number (1-100)", "Ο μυστικός σου αριθμός (1-100)" } },
        { "pvp_enter_code",          new[] { "Room code", "Κωδικός δωματίου" } },
        { "pvp_guess",               new[] { "Guess", "Μάντεψε" } },
        { "pvp_leave",               new[] { "Leave match", "Έξοδος από τον αγώνα" } },
        { "pvp_copy",                new[] { "Copy invite", "Αντιγραφή πρόσκλησης" } },
        { "pvp_creating",            new[] { "Creating room...", "Δημιουργία δωματίου..." } },
        { "pvp_joining",             new[] { "Joining...", "Σύνδεση..." } },
        { "pvp_sending",             new[] { "Sending...", "Αποστολή..." } },
        { "pvp_wait_turn",           new[] { "Wait for your turn...", "Περίμενε τη σειρά σου..." } },
        { "pvp_opponent_left",       new[] { "Your opponent left the match.", "Ο αντίπαλός σου αποχώρησε." } },
        { "pvp_connection_lost",     new[] { "Connection lost. Try again later.", "Χάθηκε η σύνδεση. Δοκίμασε αργότερα." } },
        { "pvp_invite_text",         new[] { "Duel me in HOL — Higher or Lower! My room code: {0}", "Έλα για μονομαχία στο HOL! Κωδικός δωματίου: {0}" } },
        { "you",                     new[] { "You", "Εσύ" } },

        // disclosure / consent
        { "simulated_opponents",     new[] { "Opponents are simulated by an on-device AI.", "Οι αντίπαλοι προσομοιώνονται από τεχνητή νοημοσύνη στη συσκευή." } },
        { "consent_message",         new[] { "This game shows ads. Allow personalized ads?", "Το παιχνίδι εμφανίζει διαφημίσεις. Να επιτρέπονται εξατομικευμένες;" } },
        { "yes",                     new[] { "Yes", "Ναι" } },
        { "no",                      new[] { "No", "Όχι" } },
        { "ads_privacy",             new[] { "Ads privacy", "Απόρρητο διαφημίσεων" } },
        { "difficulty",              new[] { "Difficulty", "Δυσκολία" } },
        { "easy",                    new[] { "Easy", "Εύκολο" } },
        { "normal",                  new[] { "Normal", "Κανονικό" } },
        { "hard",                    new[] { "Hard", "Δύσκολο" } },
        { "adaptive",                new[] { "Adaptive", "Προσαρμοστικό" } },
        { "splash_tagline",          new[] { "H I G H E R   O R   L O W E R", "Ψ Η Λ Ο Τ Ε Ρ Α   Ή   Χ Α Μ Η Λ Ο Τ Ε Ρ Α" } },
        { "update_required",         new[] { "A new version of HOL is available. Update to keep playing.", "Βγήκε νέα έκδοση του HOL. Ενημέρωσε για να συνεχίσεις να παίζεις." } },
        { "update_now",              new[] { "Update", "Ενημέρωση" } },
        { "save_streak_ad",          new[] { "Watch an ad to keep your {0}-win streak!", "Δες διαφήμιση για να κρατήσεις το σερί σου ({0})!" } },
        { "ad_not_ready",            new[] { "No ad available right now", "Δεν υπάρχει διαθέσιμη διαφήμιση αυτή τη στιγμή" } },
        { "perfect_game",            new[] { "PERFECT RUN!", "ΤΕΛΕΙΟ!" } },
    };
}
