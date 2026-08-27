// Unity-free fixed quick-chat protocol shared by the application boundary and
// the existing Unity localization adapter.
//
// Order is part of the wire contract: PlayFab sends only a positional id and
// each client resolves the matching localization key. Append only; never
// reorder or remove an existing entry without a separately approved protocol
// migration for old clients and CloudScript.
public static class PvpSignalProtocol
{
    public static readonly string[] Keys =
    {
        "signal_luck",      // 0
        "signal_close",     // 1
        "signal_ouch",      // 2
        "signal_nice",      // 3
        "signal_your_turn", // 4
        "signal_gg",        // 5
    };

    public static int Count { get { return Keys.Length; } }

    // Must remain aligned with SIGNAL_CAP_PER_SIDE in playfab/cloudscript.js.
    public const int CapPerSide = 12;

    public static bool IsValid(int id)
    {
        return id >= 0 && id < Keys.Length;
    }

    public static string Key(int id)
    {
        return IsValid(id) ? Keys[id] : "";
    }
}
