using System.Collections.Generic;
using UnityEngine;

// Lightweight localization (English + native Greek). Static so any script
// or UI component can use it without scene wiring.
public static class L10n
{
    public enum Language { English = 0, Greek = 1 }

    const string PrefKey = "Language";

    public static System.Action OnLanguageChanged;

    public static Language Current
    {
        get
        {
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
        { "play_solo",               new[] { "Play Solo vs AI", "Παίξε Solo με AI" } },
        { "private_room",            new[] { "Private Room", "Ιδιωτικό δωμάτιο" } },
        { "home_solo_title",         new[] { "PLAY SOLO VS AI", "ΠΑΙΞΕ SOLO ΜΕ AI" } },
        { "home_solo_subtitle",      new[] { "PLAY NOW", "ΑΜΕΣΟ ΠΑΙΧΝΙΔΙ" } },
        { "home_private_title",      new[] { "PRIVATE ROOM", "ΙΔΙΩΤΙΚΟ ΔΩΜΑΤΙΟ" } },
        { "home_private_subtitle",   new[] { "PLAY WITH A FRIEND", "ΠΑΙΞΕ ΜΕ ΦΙΛΟ" } },
        { "home_daily_title",        new[] { "DAILY HUNT", "ΚΥΝΗΓΙ ΗΜΕΡΑΣ" } },
        { "home_daily_subtitle",     new[] {
            "NEW CHALLENGE EVERY DAY", "ΝΕΑ ΠΡΟΚΛΗΣΗ ΚΑΘΕ ΜΕΡΑ" } },
        { "home_tip_title",          new[] { "TIP:", "ΣΥΜΒΟΥΛΗ:" } },
        { "home_tip_body",           new[] {
            "Every guess narrows the range!",
            "Κάθε μαντεψιά μικραίνει το εύρος!" } },
        { "splash_loading",          new[] { "LOADING...", "ΦΟΡΤΩΣΗ..." } },
        { "back",                    new[] { "Back", "Πίσω" } },
        { "quit",                    new[] { "Quit", "Έξοδος" } },
        { "save",                    new[] { "Save", "Αποθήκευση" } },
        { "language",                new[] { "Language", "Γλώσσα" } },
        { "language_english",        new[] { "English", "English" } },
        { "language_greek",          new[] { "Ελληνικά", "Ελληνικά" } },
        { "music",                   new[] { "Music", "Μουσική" } },
        { "settings_title",          new[] { "Settings", "Ρυθμίσεις" } },
        { "settings_title_display",  new[] { "SETTINGS", "ΡΥΘΜΙΣΕΙΣ" } },
        { "settings_change",         new[] { "Change", "Αλλαγή" } },
        { "settings_change_display", new[] { "CHANGE", "ΑΛΛΑΓΗ" } },
        { "settings_save_display",   new[] { "SAVE", "ΑΠΟΘΗΚΕΥΣΗ" } },
        { "player_name",             new[] { "Your name", "Το όνομά σου" } },
        { "settings_player_name",    new[] { "PLAYER NAME", "ΟΝΟΜΑ ΠΑΙΚΤΗ" } },
        { "settings_language",       new[] { "LANGUAGE", "ΓΛΩΣΣΑ" } },
        { "settings_music",          new[] { "MUSIC", "ΜΟΥΣΙΚΗ" } },
        { "settings_ai_difficulty",  new[] { "AI DIFFICULTY", "ΔΥΣΚΟΛΙΑ AI" } },
        { "settings_ads_privacy",    new[] { "ADS PRIVACY", "ΑΠΟΡΡΗΤΟ ΔΙΑΦΗΜΙΣΕΩΝ" } },
        { "player_default",          new[] { "Player", "Παίκτης" } },

        // matchmaking
        { "find_challenger",         new[] { "Play now vs AI", "Παίξε Solo με AI" } },
        { "solo_search_title",       new[] { "Find opponent", "Βρες αντίπαλο" } },
        { "solo_ai_preparing",       new[] {
            "PREPARING AI OPPONENT", "ΠΡΟΕΤΟΙΜΑΣΙΑ AI ΑΝΤΙΠΑΛΟΥ" } },
        { "solo_ai_ready",           new[] {
            "AI OPPONENT READY!", "Ο AI ΑΝΤΙΠΑΛΟΣ ΕΙΝΑΙ ΕΤΟΙΜΟΣ!" } },
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
        { "result_page_title",       new[] { "RESULT!", "ΑΠΟΤΕΛΕΣΜΑ!" } },
        { "result_win_title",        new[] { "YOU WON!", "ΝΙΚΗΣΕΣ!" } },
        { "result_loss_title",       new[] { "YOU LOST!", "ΕΧΑΣΕΣ!" } },
        { "result_draw_title",       new[] { "DRAW!", "ΙΣΟΠΑΛΙΑ!" } },
        { "result_attempts",         new[] { "TRIES", "ΠΡΟΣΠΑΘΕΙΕΣ" } },
        { "result_attempts_short",   new[] { "TRIES", "ΠΡΟΣΠ." } },
        { "result_rematch_heading",  new[] { "FOR A REMATCH", "ΓΙΑ ΡΕΒΑΝΣ" } },
        { "result_reactions",        new[] { "SEND A REACTION", "ΣΤΕΙΛΕ ΑΝΤΙΔΡΑΣΗ" } },
        { "result_exit",             new[] { "EXIT", "ΕΞΟΔΟΣ" } },
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
        { "round_label",             new[] { "ROUND {0}/{1}", "ΓΥΡΟΣ {0}/{1}" } },
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
        { "private_room_title",      new[] { "Play with a friend", "Παίξε με φίλο" } },
        { "private_room_step",       new[] { "2. PLAY WITH A FRIEND", "2. ΠΑΙΞΕ ΜΕ ΦΙΛΟ" } },
        { "private_room_create_title", new[] { "CREATE A ROOM", "ΔΗΜΙΟΥΡΓΗΣΕ ΔΩΜΑΤΙΟ" } },
        { "private_room_create_action", new[] { "CREATE", "ΔΗΜΙΟΥΡΓΙΑ" } },
        { "private_room_create_hint",new[] { "Share the code with your friend", "Μοιράσου τον κωδικό με τον φίλο σου" } },
        { "private_room_join_title", new[] { "Join a room", "Συμμετοχή σε δωμάτιο" } },
        { "private_room_join_action", new[] { "JOIN!", "ΜΠΕΣ!" } },
        { "private_room_share",      new[] { "Share", "Μοιράσου" } },
        { "private_room_tip",        new[] {
            "Share the code with your friend to join the same room.",
            "Μοιράσου τον κωδικό με τον φίλο σου για να μπείτε στο ίδιο δωμάτιο." } },
        { "prebattle_title",         new[] { "Before the battle", "Πριν τη μάχη" } },
        { "prebattle_you",           new[] { "YOU", "ΕΣΥ" } },
        { "prebattle_opponent",      new[] { "OPPONENT", "ΑΝΤΙΠΑΛΟΣ" } },
        { "prebattle_found",         new[] { "FOUND", "ΒΡΕΘΗΚΕ" } },
        { "prebattle_waiting_short", new[] { "WAITING...", "ΑΝΑΜΟΝΗ..." } },
        { "prebattle_rule_title",    new[] { "RULE", "ΚΑΝΟΝΑΣ" } },
        { "versus",                  new[] { "VS", "VS" } },
        { "prebattle_rule",          new[] {
            "Guess the secret number 1–100 before your opponent.",
            "Μάντεψε τον μυστικό αριθμό 1–100 πριν τον αντίπαλό σου." } },
        { "prebattle_waiting",       new[] {
            "WAITING FOR OPPONENT...",
            "ΠΕΡΙΜΕΝΟΥΜΕ ΑΝΤΙΠΑΛΟ..." } },
        { "pvp_creating",            new[] { "Creating room...", "Δημιουργία δωματίου..." } },
        { "pvp_joining",             new[] { "Joining...", "Σύνδεση..." } },
        { "pvp_sending",             new[] { "Sending...", "Αποστολή..." } },
        { "pvp_wait_turn",           new[] { "Wait for your turn...", "Περίμενε τη σειρά σου..." } },
        { "pvp_opponent_left",       new[] { "Your opponent left the match.", "Ο αντίπαλός σου αποχώρησε." } },
        { "pvp_connection_lost",     new[] { "Connection lost. Try again later.", "Χάθηκε η σύνδεση. Δοκίμασε αργότερα." } },
        { "pvp_room_unavailable",    new[] { "This room is no longer available.", "Αυτό το δωμάτιο δεν είναι πλέον διαθέσιμο." } },
        { "pvp_terminal_connection_title", new[] { "CONNECTION LOST", "ΧΑΘΗΚΕ Η ΣΥΝΔΕΣΗ" } },
        { "pvp_terminal_room_title", new[] { "ROOM UNAVAILABLE", "ΜΗ ΔΙΑΘΕΣΙΜΟ ΔΩΜΑΤΙΟ" } },
        { "pvp_terminal_opponent_title", new[] { "OPPONENT LEFT", "Ο ΑΝΤΙΠΑΛΟΣ ΕΦΥΓΕ" } },
        { "pvp_invite_text",         new[] { "Duel me in HOL — Higher or Lower! My room code: {0}", "Έλα για μονομαχία στο HOL! Κωδικός δωματίου: {0}" } },
        { "hud_current_number",      new[] { "CURRENT NUMBER", "ΤΡΕΧΩΝ ΑΡΙΘΜΟΣ" } },
        { "round_label_open",        new[] { "ROUND {0}", "ΓΥΡΟΣ {0}" } },
        { "hud_history",             new[] { "HISTORY", "ΙΣΤΟΡΙΚΟ" } },
        { "hud_tip",                 new[] { "TIP", "ΣΥΜΒΟΥΛΗ" } },
        { "you",                     new[] { "You", "Εσύ" } },

        // disclosure / consent
        { "simulated_opponents",     new[] { "Solo starts right away against a computer challenger.", "Το Solo ξεκινά αμέσως με αντίπαλο τον υπολογιστή." } },
        { "consent_message",         new[] { "Allow ads and related device access? If you choose No, ads stay disabled.", "Να επιτρέπονται διαφημίσεις και η σχετική πρόσβαση στη συσκευή; Αν επιλέξεις Όχι, οι διαφημίσεις θα παραμείνουν απενεργοποιημένες." } },
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

        // daily hunt — one shared secret number per UTC day
        { "daily_hunt",              new[] { "Daily Hunt", "Καθημερινό Κυνήγι" } },
        { "daily_hunt_number",       new[] { "Daily Hunt #{0}", "Καθημερινό Κυνήγι #{0}" } },
        { "daily_intro",             new[] {
            "Everyone hunts the same secret number today. Find it in {0} guesses!",
            "Όλοι κυνηγούν τον ίδιο μυστικό αριθμό σήμερα. Βρες τον σε {0} προσπάθειες!" } },
        { "guesses_left",            new[] { "Guesses left: {0}", "Απομένουν προσπάθειες: {0}" } },
        { "daily_found",             new[] { "Found it in {0}/{1}!", "Τον βρήκες σε {0}/{1}!" } },
        { "daily_failed",            new[] { "Out of guesses! The number was {0}.", "Τέλος οι προσπάθειες! Ο αριθμός ήταν {0}." } },
        { "daily_come_back",         new[] { "New hunt tomorrow.", "Νέο κυνήγι αύριο." } },
        { "daily_streak",            new[] { "Daily streak: {0}", "Καθημερινό σερί: {0}" } },
        { "daily_streak_heading",    new[] { "DAILY STREAK", "ΗΜΕΡΗΣΙΟ ΣΕΡΙ" } },
        { "daily_challenge_title",   new[] { "DAILY CHALLENGE", "ΗΜΕΡΗΣΙΑ ΔΟΚΙΜΑΣΙΑ" } },
        { "daily_missions_heading",  new[] { "COMPLETE TODAY'S MISSIONS!", "ΟΛΟΚΛΗΡΩΣΕ ΤΙΣ ΣΗΜΕΡΙΝΕΣ ΑΠΟΣΤΟΛΕΣ!" } },
        { "daily_mission_win",       new[] { "WIN 1 MATCH", "ΚΕΡΔΙΣΕ 1 ΑΓΩΝΑ" } },
        { "daily_mission_correct",   new[] { "GET 3 CORRECT GUESSES", "ΠΕΤΥΧΕ 3 ΣΩΣΤΕΣ ΑΠΑΝΤΗΣΕΙΣ" } },
        { "daily_mission_share_room", new[] { "SHARE 1 ROOM", "ΜΟΙΡΑΣΟΥ ΕΝΑ ΔΩΜΑΤΙΟ" } },
        { "daily_all_missions_complete", new[] { "ALL MISSIONS COMPLETE!", "ΟΛΕΣ ΟΙ ΑΠΟΣΤΟΛΕΣ ΟΛΟΚΛΗΡΩΘΗΚΑΝ!" } },
        { "daily_missions_progress", new[] { "MISSIONS COMPLETE: {0}/{1}", "ΟΛΟΚΛΗΡΩΜΕΝΕΣ ΑΠΟΣΤΟΛΕΣ: {0}/{1}" } },
        { "daily_reward_heading",    new[] { "DAILY REWARD", "ΗΜΕΡΗΣΙΑ ΑΝΤΑΜΟΙΒΗ" } },
        { "daily_reset_label",      new[] { "RESET IN", "ΑΝΑΝΕΩΣΗ ΣΕ" } },
        { "daily_reset_in",         new[] { "RESET IN {0:00}:{1:00}:{2:00}", "ΑΝΑΝΕΩΣΗ ΣΕ {0:00}:{1:00}:{2:00}" } },
        { "daily_reward_collected", new[] { "REWARD COLLECTED", "Η ΑΝΤΑΜΟΙΒΗ ΔΟΘΗΚΕ" } },
        { "daily_reward_pending",   new[] { "COMPLETE ALL 3 MISSIONS", "ΟΛΟΚΛΗΡΩΣΕ ΚΑΙ ΤΙΣ 3 ΑΠΟΣΤΟΛΕΣ" } },
        { "daily_start",            new[] { "START!", "ΞΕΚΙΝΑ!" } },
        { "share_result",            new[] { "Share result", "Κοινοποίηση" } },
        { "share_copied",            new[] { "Copied! Paste it to a friend.", "Αντιγράφηκε! Στείλ'το σε φίλο." } },
        { "daily_share",             new[] { "HOL Daily Hunt #{0} — {1}\n{2}", "HOL Καθημερινό Κυνήγι #{0} — {1}\n{2}" } },
        { "second_chance",           new[] { "Watch an ad for +{0} guesses", "Δες διαφήμιση για +{0} προσπάθειες" } },

        // duel rules — rounds, last licks, the Lock
        { "lock",                    new[] { "LOCK", "ΚΛΕΙΔΩΜΑ" } },
        { "lock_hint",               new[] { "Lock a guess to win a tie — but miss and you forfeit your next turn.", "Κλείδωσε μια μαντεψιά για να κερδίσεις την ισοπαλία — αν αστοχήσεις όμως, χάνεις την επόμενη σειρά σου." } },
        { "lock_suggest",            new[] { "Only {0} left — lock it?", "Έμειναν μόνο {0} — κλείδωσέ το;" } },
        { "lock_spent",              new[] { "Lock used", "Το κλείδωμα χρησιμοποιήθηκε" } },
        { "lock_armed",              new[] { "LOCKED", "ΚΛΕΙΔΩΜΕΝΟ" } },
        { "lock_missed",             new[] { "Locked and missed — you forfeit your next turn.", "Κλείδωσες και αστόχησες — χάνεις την επόμενη σειρά σου." } },
        { "turn_forfeited",          new[] { "Turn forfeited", "Έχασες τη σειρά σου" } },
        { "opponent_forfeits",       new[] { "{0} forfeits this turn", "Ο/Η {0} χάνει αυτή τη σειρά" } },
        { "match_point",             new[] { "MATCH POINT — your last guess!", "ΜΑΤΣ ΠΟΪΝΤ — η τελευταία σου μαντεψιά!" } },
        { "match_point_yours",       new[] { "You found it! {0} gets one answering guess.", "Το βρήκες! Ο/Η {0} έχει μία απαντητική μαντεψιά." } },
        { "you_draw",                new[] { "DEAD HEAT!", "ΙΣΟΠΑΛΙΑ!" } },
        { "draw_in_guesses",         new[] { "You both found it in {0} guesses", "Και οι δύο το βρήκατε σε {0} προσπάθειες" } },
        { "draw_tip",                new[] { "Lock a guess next time to settle it.", "Την επόμενη φορά κλείδωσε μια μαντεψιά για να κριθεί." } },
        { "candidates_left",         new[] { "{0} numbers left", "Απομένουν {0} αριθμοί" } },

        // signals — a closed vocabulary, never free text
        { "signals",                 new[] { "Signals", "Σήματα" } },
        { "signal_luck",             new[] { "Good luck!", "Καλή τύχη!" } },
        { "signal_close",            new[] { "So close!", "Πολύ κοντά!" } },
        { "signal_ouch",             new[] { "Ouch!", "Άουτς!" } },
        { "signal_nice",             new[] { "Nice one!", "Μπράβο!" } },
        { "signal_your_turn",        new[] { "Your turn!", "Η σειρά σου!" } },
        { "signal_gg",               new[] { "Good game!", "Καλό παιχνίδι!" } },
        { "signal_limit",            new[] { "No signals left", "Δεν έχεις άλλα σήματα" } },
        { "signal_from",             new[] { "{0}: {1}", "{0}: {1}" } },

        // rematch — play again without re-sharing an invite code
        // ("rematch" itself already lives in the result block above)
        { "rematch_prompt",          new[] { "New secret number (1-100)", "Νέος μυστικός αριθμός (1-100)" } },
        { "rematch_waiting",         new[] { "Waiting for your opponent to accept", "Αναμονή να δεχτεί ο αντίπαλος" } },
        { "rematch_offered",         new[] { "{0} wants a rematch!", "Ο/Η {0} θέλει ρεβάνς!" } },
        { "rematch_closed",          new[] { "Your opponent has left.", "Ο αντίπαλός σου έφυγε." } },
    };
}
