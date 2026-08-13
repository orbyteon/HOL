// The complete vocabulary players can send each other.
//
// This is deliberately a fixed table rather than free-form chat. Only the index
// travels over the wire; each client renders the entry in its own reader's
// language, so a Greek player sees Greek even when the sender is playing in
// English. Nothing a player types ever reaches another player.
//
// That closed set is what keeps HOL out of user-generated-content territory:
// there is no text to moderate, no profanity filter to maintain, no reports or
// blocks to build, and nothing extra to declare on the Play Data Safety form
// beyond the room state the game already exchanges. The tone of the game is
// fixed at design time — every entry here is friendly or self-deprecating, so
// the set cannot be used to abuse an opponent.
//
// Order is part of the protocol: ids are positional and the server validates
// them against SIGNAL_COUNT in playfab/cloudscript.js. Append only, never
// reorder or remove — an older client would render the wrong line.
public static class Signals
{
    // Text only, no emoji: the project ships TextMesh Pro with the default
    // Liberation Sans atlas, which renders emoji as tofu. Adding a glyph column
    // means adding an emoji font fallback first.
    public static readonly string[] Table =
    {
        "signal_luck",      // 0
        "signal_close",     // 1
        "signal_ouch",      // 2
        "signal_nice",      // 3
        "signal_your_turn", // 4
        "signal_gg",        // 5
    };

    // Must equal SIGNAL_COUNT in playfab/cloudscript.js.
    public static int Count { get { return Table.Length; } }

    // Must equal SIGNAL_CAP_PER_SIDE in playfab/cloudscript.js. The client
    // stops offering signals at the cap so a player never taps into a rejection.
    public const int CapPerSide = 12;

    public static bool IsValid(int id)
    {
        return id >= 0 && id < Table.Length;
    }

    // The localization key for a signal, so runtime-built buttons can carry a
    // LocalizedText component and follow live language switching.
    public static string Key(int id)
    {
        return IsValid(id) ? Table[id] : "";
    }

    public static string Text(int id)
    {
        return IsValid(id) ? L10n.Get(Table[id]) : "";
    }
}
